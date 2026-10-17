"""Lift one evidenced callback in a function gap, preserving an existing generation.

Requires the project's patched XboxRecomp checkout and Capstone. Outputs are
private generated code/metadata, never inputs for source publication.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import re
import shutil
import struct
import sys
from pathlib import Path

ROW = re.compile(r"(?m)^[ \t]*\{ 0x([0-9A-Fa-f]+)u, \(recomp_func_t\)(\w+) \},")


def validate_switch(code: bytes, start: int, end: int, table: int, count: int):
    """Prove a trailing dword table guarded by CMP index,N / JA default."""
    import capstone
    if not 1 <= count <= 256 or not start < table or table + count * 4 != end:
        raise ValueError("Switch table must exactly fill the trailing range")
    decoder = capstone.Cs(capstone.CS_ARCH_X86, capstone.CS_MODE_32)
    decoder.detail = True
    instructions = list(decoder.disasm(code[:table - start], start))
    if (sum(i.size for i in instructions) != table - start
            or instructions[-1].mnemonic != "ret"):
        raise ValueError("Switch code must decode completely through RET")
    boundaries = {i.address for i in instructions}
    targets = list(struct.unpack(f"<{count}I", code[table - start:]))
    if any(t not in boundaries for t in targets):
        raise ValueError("Switch destination is not a code boundary")
    jumps = [i for i in instructions if i.mnemonic == "jmp" and i.operands[0].type == capstone.x86.X86_OP_MEM]
    if len(jumps) != 1:
        raise ValueError("Expected exactly one indexed switch")
    jump = jumps[0]
    mem = jump.operands[0].mem
    pos = instructions.index(jump)
    if (mem.base or mem.segment or not mem.index or mem.scale != 4
            or mem.disp != table or pos < 2):
        raise ValueError("Unexpected switch addressing")
    cmp, guard = instructions[pos - 2:pos]
    if (cmp.mnemonic != "cmp" or len(cmp.operands) != 2
            or cmp.operands[0].type != capstone.x86.X86_OP_REG
            or cmp.operands[0].reg != mem.index
            or cmp.operands[1].type != capstone.x86.X86_OP_IMM
            or cmp.operands[1].imm != count - 1
            or guard.mnemonic != "ja" or guard.operands[0].type != capstone.x86.X86_OP_IMM
            or guard.operands[0].imm not in boundaries):
        raise ValueError("Switch lacks the exact unsigned bound guard")
    bypass = {guard.address, jump.address}
    if (any(t in bypass for t in targets)
            or any(i.mnemonic.startswith('j') and i.operands[0].type == capstone.x86.X86_OP_IMM
                   and i.operands[0].imm in bypass for i in instructions)):
        raise ValueError("Control flow bypasses the switch bound check")
    return instructions, targets, jump.address


def validate_contained_callback(data: bytes, owner_file_va_delta: int, reference_file_va_delta: int,
                                owner_start: int,
                                owner_end: int, callback_start: int,
                                callback_end: int, expected_owner: int,
                                reference_start: int, reference_end: int):
    """Prove an interior entry and a literal reference in its known caller."""
    import capstone
    if owner_start != expected_owner:
        raise ValueError("Contained callback owner does not match the explicit owner")
    if not owner_start < callback_start < callback_end == owner_end:
        raise ValueError("Contained callback must start inside its owner and end at owner end")
    owner_off = owner_file_va_delta + owner_start
    owner_size = owner_end - owner_start
    code = data[owner_off:owner_off + owner_size]
    if len(code) != owner_size:
        raise ValueError("Owner bytes are incomplete")
    decoder = capstone.Cs(capstone.CS_ARCH_X86, capstone.CS_MODE_32)
    decoder.detail = True
    owner_instructions = list(decoder.disasm(code, owner_start))
    if sum(i.size for i in owner_instructions) != owner_size:
        raise ValueError("Owner does not decode completely")
    boundaries = {i.address for i in owner_instructions}
    if callback_start not in boundaries:
        raise ValueError("Callback start is not an owner instruction boundary")
    callback = [i for i in owner_instructions if i.address >= callback_start]
    if not callback or callback[0].address != callback_start or callback[-1].address + callback[-1].size != callback_end:
        raise ValueError("Callback end does not match the decoded owner boundary")
    if callback[-1].mnemonic not in ("ret", "jmp"):
        raise ValueError("Contained callback does not end in RET or JMP")
    ref_off = reference_file_va_delta + reference_start
    ref_size = reference_end - reference_start
    ref_code = data[ref_off:ref_off + ref_size]
    if len(ref_code) != ref_size:
        raise ValueError("Reference function bytes are incomplete")
    ref_instructions = list(decoder.disasm(ref_code, reference_start))
    if sum(i.size for i in ref_instructions) != ref_size:
        raise ValueError("Reference function does not decode completely")
    refs = []
    for ins in ref_instructions:
        for operand in ins.operands:
            if operand.type == capstone.x86.X86_OP_IMM and operand.imm & 0xFFFFFFFF == callback_start:
                refs.append(ins.address)
    if not refs:
        raise ValueError("Named reference function has no literal callback reference")
    return owner_instructions, refs


def validate_tail_jump_stub_gap(code: bytes, start: int, callback_end: int,
                               gap_end: int, registered: set[int]):
    """Prove a short gap consists only of direct registered JMP stubs."""
    import capstone
    if not start < callback_end < gap_end or len(code) != gap_end - start:
        raise ValueError("Tail-stub gap must end at a later known function boundary")
    decoder = capstone.Cs(capstone.CS_ARCH_X86, capstone.CS_MODE_32)
    decoder.detail = True
    instructions = list(decoder.disasm(code, start))
    if sum(i.size for i in instructions) != len(code):
        raise ValueError("Tail-stub gap does not decode completely")
    if not instructions or instructions[0].address != start:
        raise ValueError("Tail-stub entry is not the first instruction")
    first_end = instructions[0].address + instructions[0].size
    if first_end != callback_end:
        raise ValueError("Tail-stub end is not the first instruction boundary")
    targets = []
    for ins in instructions:
        if ins.mnemonic != "jmp" or ins.size != 5 or len(ins.operands) != 1 or ins.operands[0].type != capstone.x86.X86_OP_IMM:
            raise ValueError("Tail-stub gap must contain only direct near JMP instructions")
        target = ins.operands[0].imm & 0xFFFFFFFF
        if target not in registered:
            raise ValueError(f"Tail-stub destination {target:08X} is not registered")
        targets.append(target)
    return instructions, targets


def validate_linear_leaf(code: bytes, start: int, end: int):
    """Require a complete straight-line leaf ending in its only RET."""
    import capstone
    if len(code) != end - start or not code:
        raise ValueError("Linear leaf bytes do not match the requested range")
    decoder = capstone.Cs(capstone.CS_ARCH_X86, capstone.CS_MODE_32)
    decoder.detail = True
    instructions = list(decoder.disasm(code, start))
    if sum(i.size for i in instructions) != len(code):
        raise ValueError("Linear leaf does not decode completely")
    if (not instructions or instructions[-1].mnemonic != "ret"
            or instructions[-1].address + instructions[-1].size != end
            or sum(i.mnemonic == "ret" for i in instructions) != 1):
        raise ValueError("Linear leaf must end at its only RET")
    if any(i.mnemonic == "call" or i.mnemonic.startswith("j") for i in instructions):
        raise ValueError("Linear leaf cannot contain calls or branches")
    return instructions


def read_dispatch(text: str) -> list[tuple[int, str]]:
    rows = [(int(m[1], 16), m[2]) for m in ROW.finditer(text)]
    addresses = [a for a, _ in rows]
    if not rows or addresses != sorted(set(addresses)):
        raise ValueError("Dispatch must contain unique, ordered exact starts")
    size = re.search(r"static const size_t g_recomp_table_size = (\d+);", text)
    if size is None or int(size[1]) != len(rows):
        raise ValueError("Dispatch size constant disagrees with its entries")
    return rows


def register_callback(dispatch: str, header: str, start: int) -> tuple[str, str]:
    rows = read_dispatch(dispatch)
    symbol = f"sub_{start:08X}"
    if start in dict(rows) or re.search(rf"\b{symbol}\s*\(", header):
        raise ValueError("Callback already registered")
    following = next((m for m in ROW.finditer(dispatch) if int(m[1], 16) > start), None)
    if following is None:
        raise ValueError("Recovery requires a following known function")
    row = f"    {{ 0x{start:08X}u, (recomp_func_t){symbol} }},\n"
    dispatch = dispatch[:following.start()] + row + dispatch[following.start():]
    dispatch = dispatch.replace(
        f"static const size_t g_recomp_table_size = {len(rows)};",
        f"static const size_t g_recomp_table_size = {len(rows) + 1};", 1)
    dispatch = re.sub(r"Maps \d+ Xbox VAs", f"Maps {len(rows) + 1} Xbox VAs", dispatch)
    anchor = '#include "recomp_types.h"'
    if header.count(anchor) != 1:
        raise ValueError("Unrecognized generated declaration header")
    header = header.replace(anchor, anchor + f"\n\nvoid {symbol}(void);", 1)
    header = re.sub(r"\d+ functions, auto-generated", f"{len(rows) + 1} functions, auto-generated", header)
    read_dispatch(dispatch)
    return dispatch, header


def recover(args: argparse.Namespace) -> dict:
    import capstone

    if args.tail_jump_stub and args.linear_leaf_gap:
        raise ValueError("Tail-stub and linear-leaf recovery modes are mutually exclusive")
    output = args.output_dir.resolve()
    if output.drive.lower() != "d:" or output.exists():
        raise ValueError("Output must be a new directory on D:")
    if not 0 <= args.start < args.end <= 0x100000000:
        raise ValueError("Expected a nonempty 32-bit address range")
    data = args.xbe.read_bytes()
    digest = hashlib.sha256(data).hexdigest()
    if digest.lower() != args.xbe_sha256.lower():
        raise ValueError("Original XBE hash mismatch")
    trace = args.trace.read_text(encoding="utf-8-sig")
    if not re.search(rf"unresolved guest target 0x{args.start:08X}\b", trace, re.I):
        raise ValueError("Trace does not establish this unresolved callback")
    functions = json.loads(args.functions.read_text(encoding="utf-8-sig"))
    known = {int(f["start"], 16): f for f in functions}
    if args.start in known or args.end <= args.start:
        raise ValueError("Expected an omitted start with a nonempty recovery range")
    tail_gap_end = None
    linear_gap_end = None
    if args.tail_jump_stub:
        following = [address for address in known if address > args.start]
        if not following:
            raise ValueError("Tail-stub recovery requires a known following function boundary")
        tail_gap_end = min(following)
        if args.end >= tail_gap_end:
            raise ValueError("Tail-stub entry must end before the following function")
        if tail_gap_end - args.start > 64:
            raise ValueError("Tail-stub proof gap exceeds the 64-byte bound")
    elif args.linear_leaf_gap:
        following = [address for address in known if address > args.start]
        if not following:
            raise ValueError("Linear-leaf recovery requires a known following function boundary")
        linear_gap_end = min(following)
        if args.end >= linear_gap_end:
            raise ValueError("Linear-leaf entry must end before the following function")
    elif args.end not in known:
        raise ValueError("Expected an omitted start bounded by a known following function")
    overlaps = [f for f in functions
                if int(f["start"], 16) < args.end and int(f["end"], 16) > args.start]
    contained_proof = None
    if overlaps:
        if args.contained_owner is None or len(overlaps) != 1:
            raise ValueError("Requested range overlaps an already translated function")
        owner = overlaps[0]
        owner_start, owner_end = int(owner["start"], 16), int(owner["end"], 16)
        if (owner_start != args.contained_owner or args.end != owner_end
                or args.start <= owner_start):
            raise ValueError("Overlap is not contained in the explicitly named owner")
        reference = known.get(args.reference_function) if args.reference_function is not None else None
        if reference is None:
            raise ValueError("Contained callback requires an explicit known reference function")
        contained_proof = True
    elif args.contained_owner is not None or args.reference_function is not None:
        raise ValueError("Named contained-callback owner does not overlap the callback")
    dispatch = (args.generated_dir / "recomp_dispatch.c").read_text(encoding="utf-8")
    registered = dict(read_dispatch(dispatch))
    sys.path.insert(0, str(args.vendor_root.resolve()))
    from tools.recomp import config
    from tools.recomp.translator import BatchTranslator

    config.configure_from_xbe(str(args.xbe))
    if not config.is_code_address(args.start) or not config.is_code_address(args.end - 1):
        raise ValueError("Range is outside executable XBE sections")
    offset = config.va_to_file_offset(args.start)
    size = args.end - args.start
    if offset is None or config.va_to_file_offset(args.end - 1) != offset + size - 1:
        raise ValueError("Range is not contiguous in the original XBE")
    code = data[offset:offset + size]
    tail_stub_targets = []
    if args.tail_jump_stub:
        gap_offset = config.va_to_file_offset(tail_gap_end - 1)
        gap_size = tail_gap_end - args.start
        if gap_offset != offset + gap_size - 1:
            raise ValueError("Tail-stub gap is not contiguous in the original XBE")
        gap_code = data[offset:offset + gap_size]
        _, tail_stub_targets = validate_tail_jump_stub_gap(
            gap_code, args.start, args.end, tail_gap_end, set(registered))
    linear_leaf_proof = None
    if args.linear_leaf_gap:
        gap_offset = config.va_to_file_offset(linear_gap_end - 1)
        gap_size = linear_gap_end - args.start
        if gap_offset != offset + gap_size - 1:
            raise ValueError("Linear-leaf gap is not contiguous in the original XBE")
        gap_code = data[offset:offset + gap_size]
        first = validate_linear_leaf(gap_code[:size], args.start, args.end)
        neighbor = validate_linear_leaf(gap_code[size:], args.end, linear_gap_end)
        linear_leaf_proof = (first, neighbor)
    if contained_proof:
        owner = next(f for f in overlaps if int(f["start"], 16) == args.contained_owner)
        owner_offset = config.va_to_file_offset(args.contained_owner)
        reference = known[args.reference_function]
        reference_start, reference_end = (int(reference["start"], 16), int(reference["end"], 16))
        reference_offset = config.va_to_file_offset(reference_start)
        if owner_offset is None or reference_offset is None:
            raise ValueError("Owner or reference function is outside executable XBE sections")
        if config.va_to_file_offset(owner_end - 1) != owner_offset + owner_end - args.contained_owner - 1:
            raise ValueError("Owner is not contiguous in the original XBE")
        if config.va_to_file_offset(reference_end - 1) != reference_offset + reference_end - reference_start - 1:
            raise ValueError("Reference function is not contiguous in the original XBE")
        contained_proof = validate_contained_callback(
            data, owner_offset - args.contained_owner,
            reference_offset - reference_start,
            int(owner["start"], 16), int(owner["end"], 16),
            args.start, args.end, args.contained_owner,
            reference_start, reference_end)
    decoder = capstone.Cs(capstone.CS_ARCH_X86, capstone.CS_MODE_32)
    switch = args.switch_table
    targets, switch_jump = [], None
    if switch is not None:
        instructions, targets, switch_jump = validate_switch(
            code, args.start, args.end, switch, args.switch_count)
    else:
        if args.switch_count:
            raise ValueError("Switch count requires a table")
        instructions = list(decoder.disasm(code, args.start))
    if switch is None and (sum(i.size for i in instructions) != size or instructions[-1].mnemonic not in ("ret", "jmp")):
        raise ValueError("Incomplete decode or unproven function end")
    boundaries = {i.address for i in instructions}
    for ins in instructions:
        if ins.mnemonic == "call" or ins.mnemonic.startswith("j"):
            if not ins.op_str.startswith("0x"):
                if ins.address == switch_jump:
                    continue
                if (args.allow_indirect_calls and ins.mnemonic == "call"
                        and ins.op_str.startswith("dword ptr [")):
                    continue
                raise ValueError("Indirect control flow requires a separate recovery analysis")
            target = int(ins.op_str, 16)
            if args.start <= target < args.end:
                if target not in boundaries:
                    raise ValueError("Branch enters the middle of an instruction")
            elif target not in registered:
                raise ValueError(f"External destination {target:08X} is not registered")
            elif ins.mnemonic != "call" and ins.address != instructions[-1].address:
                raise ValueError("External branch before the final tail requires further analysis")
    symbol = f"sub_{args.start:08X}"
    following_boundary = tail_gap_end if args.tail_jump_stub else linear_gap_end
    boundary = known[following_boundary if following_boundary is not None else args.end]
    functions.append({"start": f"0x{args.start:08X}", "end": f"0x{args.end:08X}",
                      "size": size, "name": symbol, "section": boundary["section"],
                      "detection_method": ("observed_linear_leaf" if linear_leaf_proof else
                          "observed_tail_jump_stub" if args.tail_jump_stub else
                          "observed_contained_callback" if contained_proof else "observed_callback"), "confidence": 1.0,
                      "overlap_owner": f"0x{args.contained_owner:08X}" if contained_proof else None,
                      "reference_function": f"0x{args.reference_function:08X}" if contained_proof else None,
                      "literal_reference_sites": [f"0x{x:08X}" for x in contained_proof[1]] if contained_proof else [],
                      "tail_stub_gap_end": f"0x{tail_gap_end:08X}" if args.tail_jump_stub else None,
                      "tail_stub_gap_targets": [f"0x{x:08X}" for x in tail_stub_targets],
                      "linear_leaf_gap_end": f"0x{linear_gap_end:08X}" if linear_leaf_proof else None,
                      "linear_leaf_neighbor_end": f"0x{args.end + sum(i.size for i in linear_leaf_proof[1]):08X}" if linear_leaf_proof else None,
                      "num_instructions": len(instructions), "data_ranges":
                      [[switch, args.end]] if switch is not None else []})
    new_dispatch, new_header = register_callback(
        dispatch, (args.generated_dir / "recomp_funcs.h").read_text(encoding="utf-8"), args.start)
    output.mkdir(parents=True)
    metadata = output / "recovered_functions.json"
    metadata.write_text(json.dumps(sorted(functions, key=lambda f: int(f["start"], 16)), indent=2), encoding="utf-8")
    translator = BatchTranslator(str(args.xbe), str(metadata),
        labels_json_path=str(args.labels), identified_json_path=str(args.identified),
        abi_json_path=str(args.abi), output_dir=str(output), trace_functions={args.start},
        protected_function_starts={args.start})
    body = translator.translate_single(args.start)
    if not body or "RECOMP_UNIMPL" in body or f"void {symbol}(void)" not in body:
        raise ValueError("Callback did not translate completely")
    for source in args.generated_dir.iterdir():
        if source.is_file() and source.suffix in (".c", ".h"):
            shutil.copy2(source, output / source.name)
    (output / "recomp_dispatch.c").write_text(new_dispatch, encoding="utf-8")
    (output / "recomp_funcs.h").write_text(new_header, encoding="utf-8")
    (output / f"recomp_callback_{args.start:08X}.c").write_text(
        '#define RECOMP_GENERATED_CODE\n#include "recomp_funcs.h"\n' + body, encoding="utf-8")
    result = {"status": "generated-not-runtime-validated", "entry": f"0x{args.start:08X}",
              "end": f"0x{args.end:08X}", "size": size, "instructions": len(instructions),
              "xbe_sha256": digest, "body_sha256": hashlib.sha256(code).hexdigest(),
              "dispatch_entries": len(registered) + 1, "source_generation": str(args.generated_dir),
              "switch_table": switch, "switch_targets": targets,
              "contained_owner": f"0x{args.contained_owner:08X}" if contained_proof else None,
              "reference_function": f"0x{args.reference_function:08X}" if contained_proof else None,
              "literal_reference_sites": [f"0x{x:08X}" for x in contained_proof[1]] if contained_proof else [],
              "tail_stub_gap_end": f"0x{tail_gap_end:08X}" if args.tail_jump_stub else None,
              "tail_stub_gap_targets": [f"0x{x:08X}" for x in tail_stub_targets],
              "linear_leaf_gap_end": f"0x{linear_gap_end:08X}" if linear_leaf_proof else None,
              "linear_leaf_neighbor_end": f"0x{args.end + sum(i.size for i in linear_leaf_proof[1]):08X}" if linear_leaf_proof else None,
              "indirect_calls_preserved": args.allow_indirect_calls,
              "vendor_root": str(args.vendor_root), "trace": str(args.trace),
              "vendor_sources_sha256": {
                  name: hashlib.sha256((args.vendor_root / "tools" / "recomp" / name).read_bytes()).hexdigest()
                  for name in ("translator.py", "lifter.py", "disasm.py", "config.py")}}
    (output / "callback-recovery.json").write_text(json.dumps(result, indent=2), encoding="utf-8")
    return result


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ("vendor-root", "xbe", "functions", "labels", "identified", "abi", "generated-dir", "output-dir", "trace"):
        parser.add_argument(f"--{name}", type=Path, required=True)
    parser.add_argument("--xbe-sha256", required=True)
    parser.add_argument("--start", type=lambda x: int(x, 0), required=True)
    parser.add_argument("--end", type=lambda x: int(x, 0), required=True)
    parser.add_argument("--switch-table", type=lambda x: int(x, 0))
    parser.add_argument("--switch-count", type=int, default=0)
    parser.add_argument("--allow-indirect-calls", action="store_true")
    parser.add_argument("--contained-owner", type=lambda x: int(x, 0),
                        help="Explicit owner VA for a callback proven inside its range")
    parser.add_argument("--reference-function", type=lambda x: int(x, 0),
                        help="Known function containing a literal reference to the callback")
    parser.add_argument("--tail-jump-stub", action="store_true",
                        help="Prove a direct tail-jump entry within a fully decoded stub gap")
    parser.add_argument("--linear-leaf-gap", action="store_true",
                        help="Prove an observed straight-line RET body and one adjacent leaf through the next known boundary")
    args = parser.parse_args()
    print(json.dumps(recover(args), indent=2))


if __name__ == "__main__":
    main()
