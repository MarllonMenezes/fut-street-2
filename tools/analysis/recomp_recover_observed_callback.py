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
import sys
from pathlib import Path

ROW = re.compile(r"(?m)^[ \t]*\{ 0x([0-9A-Fa-f]+)u, \(recomp_func_t\)(\w+) \},")


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
    if args.start in known or args.end not in known or args.end <= args.start:
        raise ValueError("Expected an omitted start bounded by a known following function")
    if any(int(f["start"], 16) < args.end and int(f["end"], 16) > args.start for f in functions):
        raise ValueError("Requested range overlaps an already translated function")
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
    decoder = capstone.Cs(capstone.CS_ARCH_X86, capstone.CS_MODE_32)
    instructions = list(decoder.disasm(code, args.start))
    if sum(i.size for i in instructions) != size or instructions[-1].mnemonic not in ("ret", "jmp"):
        raise ValueError("Incomplete decode or unproven function end")
    boundaries = {i.address for i in instructions}
    for ins in instructions:
        if ins.mnemonic == "call" or ins.mnemonic.startswith("j"):
            if not ins.op_str.startswith("0x"):
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
    functions.append({"start": f"0x{args.start:08X}", "end": f"0x{args.end:08X}",
                      "size": size, "name": symbol, "section": known[args.end]["section"],
                      "detection_method": "observed_callback", "confidence": 1.0,
                      "num_instructions": len(instructions), "data_ranges": []})
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
    args = parser.parse_args()
    print(json.dumps(recover(args), indent=2))


if __name__ == "__main__":
    main()
