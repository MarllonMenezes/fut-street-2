"""Check EAGL parameter layout against static retail XBE instruction evidence.

No instructions are executed. The managed parser's rows are reconciled with
the independent command-chain report, bounded handler slices and draw-call
packet constants. Runtime selector 0x345BCC remains unknown.
"""
from __future__ import annotations

import argparse
import hashlib
from collections import defaultdict
from pathlib import Path

from eagl_original_render_command_dispatch_control import (
    EXPECTED_XBE_SHA256, EXPECTED_XBE_SIZE, _read_dispatch_table,
    _validate_dispatch_slice, _write_tsv,
)
from parse_xbe import analyze_xbe, virtual_to_file_offset
from xbe_render_cfg_audit import decode_window, load_xbe, read_tsv

STATUS = "verified-static-eagl-parameter-and-indexed-draw-control"
DRAW_BINDING = "verified-static-command26-conditional-indexed-submission"
REFERENCE = "https://github.com/XboxDev/nxdk/blob/7e8d4fc3d00ca59e88298e912438ac215cf93392/lib/pbkit/nv_regs.h"

# (entry, exclusive end, command words, consumed parameter bytes, key instructions)
HANDLERS = {
    2: (0x1A3E40, 0x1A3E96, 2, 8, {0x1A3E4C: "mov eax, dword ptr [0x3458d0]", 0x1A3E5F: "mov dword ptr [0x3458d8], esi", 0x1A3E85: "add eax, 8", 0x1A3E8F: "mov dword ptr [0x3458d0], eax"}),
    4: (0x1A3EA0, 0x1A3EC1, 2, 0, {0x1A3EAF: "call 0x1a2920", 0x1A3EB7: "call 0x1a2950", 0x1A3EC0: "ret"}),
    5: (0x1A3ED0, 0x1A3F0A, 4, 8, {0x1A3EF4: "call 0x1a2970", 0x1A3EF9: "mov eax, dword ptr [0x3458d0]", 0x1A3F01: "add eax, 8", 0x1A3F04: "mov dword ptr [0x3458d0], eax"}),
    6: (0x1A3F10, 0x1A3F4A, 4, 8, {0x1A3F34: "call 0x1a2970", 0x1A3F39: "mov eax, dword ptr [0x3458d0]", 0x1A3F41: "add eax, 8", 0x1A3F44: "mov dword ptr [0x3458d0], eax"}),
    9: (0x1A3FD0, 0x1A400B, 4, 8, {0x1A3FD5: "mov eax, dword ptr [0x3458d4]", 0x1A3FDA: "mov ecx, dword ptr [eax + 4]", 0x1A3FDD: "imul ecx, dword ptr [0x345bcc]", 0x1A3FE4: "mov edx, dword ptr [0x3458d0]", 0x1A3FEA: "mov edx, dword ptr [edx + 4]", 0x1A3FED: "lea ecx, [ecx + edx - 0x14]", 0x1A3FF1: "mov edx, dword ptr [eax + 8]", 0x1A3FF4: "push ecx", 0x1A3FF5: "push edx", 0x1A3FF6: "call 0x1a29b0", 0x1A4003: "add dword ptr [0x3458d0], 8"}),
    14: (0x1A4010, 0x1A4045, 2, 8, {0x1A4024: "mov edx, dword ptr [0x3458d0]", 0x1A402A: "add ecx, dword ptr [edx + 4]", 0x1A402D: "push ecx", 0x1A402E: "push 0", 0x1A4030: "call 0x1a2a70", 0x1A403D: "add dword ptr [0x3458d0], 8"}),
    15: (0x1A4050, 0x1A40B5, 3, 8, {0x1A4086: "call 0x19ca10", 0x1A40A3: "call 0x19c130", 0x1A40AD: "add dword ptr [0x3458d0], 8"}),
    16: (0x1A40C0, 0x1A411B, 2, 8, {0x1A40DE: "call 0x19ace0", 0x1A4100: "call 0x19a960", 0x1A4108: "mov dword ptr [0x345c70], eax", 0x1A4112: "add dword ptr [0x3458d0], 8"}),
    20: (0x1A42C0, 0x1A4314, 3, 8, {0x1A42FA: "call 0x1a2970", 0x1A42FF: "mov eax, dword ptr [0x3458d0]", 0x1A4307: "add eax, 8", 0x1A430A: "mov dword ptr [0x3458d0], eax"}),
    26: (0x1A4660, 0x1A4699, 3, 0, {0x1A4677: "mov eax, dword ptr [0x3458d4]", 0x1A467C: "mov ecx, dword ptr [eax + 8]", 0x1A467F: "test ecx, ecx", 0x1A4681: "je 0x1a4694", 0x1A4683: "mov ecx, dword ptr [0x3458d8]", 0x1A4689: "push ecx", 0x1A468A: "push 0", 0x1A468C: "call 0x1a2b10", 0x1A4694: "jmp 0x1a0530"}),
    29: (0x1A4AA0, 0x1A4AF0, 6, 8, {0x1A4AD9: "call 0x1a4bf0", 0x1A4ADE: "mov eax, dword ptr [0x3458d0]", 0x1A4AE6: "add eax, 8", 0x1A4AE9: "mov dword ptr [0x3458d0], eax"}),
    31: (0x1A4760, 0x1A478E, 2, 8, {0x1A476F: "mov eax, dword ptr [0x3458d0]", 0x1A477F: "add eax, 8", 0x1A4788: "mov dword ptr [0x3458d0], eax"}),
}

SLICES = {
    "descriptor-caller": (0x19E9B0, 0x19E9BD, {0x19E9B0: "mov eax, dword ptr [esi]", 0x19E9B2: "mov ecx, dword ptr [eax]", 0x19E9B4: "push eax", 0x19E9B8: "call 0x197050"}),
    "dispatcher-entry": (0x197050, 0x1970AF, {0x19705D: "mov eax, dword ptr [esp + 8]", 0x197067: "mov esi, dword ptr [esi]", 0x197069: "add eax, 4", 0x19706C: "mov dword ptr [0x3458d4], esi", 0x197072: "mov dword ptr [0x3458d0], eax"}),
    "buffer-wrapper": (0x1A29B0, 0x1A29F2, {0x1A29B8: "mov ecx, dword ptr [edx]", 0x1A29D7: "mov ecx, dword ptr [edx + 8]", 0x1A29DA: "push ecx", 0x1A29E9: "push ecx", 0x1A29EA: "push eax", 0x1A29EB: "call 0x2923f0"}),
    "d3d-buffer-state": (0x2923F0, 0x29247C, {0x29240A: "lea esi, [eax + eax*2]", 0x29240D: "shl esi, 2", 0x292446: "mov eax, dword ptr [esp + 0x14]", 0x292452: "mov dword ptr [esi + 0x2a0e28], eax", 0x292458: "mov dword ptr [esi + 0x2a0e30], edi"}),
    "index-source-store": (0x1A2A70, 0x1A2A84, {0x1A2A70: "mov eax, dword ptr [esp + 8]", 0x1A2A78: "mov dword ptr [0x345d00], eax"}),
    "primitive-type-load": (0x19A960, 0x19A96D, {0x19A960: "mov eax, dword ptr [ecx]", 0x19A962: "mov ecx, dword ptr [esp + 4]", 0x19A966: "mov dword ptr [ecx], eax"}),
    "indexed-draw-wrapper": (0x1A2B10, 0x1A2B30, {0x1A2B10: "mov ecx, dword ptr [0x345d00]", 0x1A2B16: "mov eax, dword ptr [esp + 4]", 0x1A2B1A: "lea edx, [ecx + eax*2]", 0x1A2B1D: "mov eax, dword ptr [esp + 8]", 0x1A2B21: "mov ecx, dword ptr [0x345c70]", 0x1A2B27: "push edx", 0x1A2B28: "push eax", 0x1A2B29: "push ecx", 0x1A2B2A: "call 0x293fb0"}),
    "d3d-indexed-packets": (0x293FB0, 0x294207, {0x293FE3: "mov ecx, dword ptr [ebp + 8]", 0x293FE6: "mov edx, dword ptr [ebp + 0xc]", 0x293FEB: "mov dword ptr [ebx], 0x417fc", 0x293FF1: "mov dword ptr [ebx + 4], ecx", 0x294191: "mov ecx, eax", 0x294193: "shl ecx, 0x12", 0x294196: "add ecx, 0x40001800", 0x2941A1: "mov esi, dword ptr [ebp + 0x10]", 0x2941C0: "movzx eax, word ptr [ecx]", 0x2941C3: "mov dword ptr [ebx], 0x41808", 0x2941CF: "mov ecx, dword ptr [ebp - 8]", 0x2941D2: "mov dword ptr [ebx], 0x417fc", 0x2941D8: "mov dword ptr [ebx + 4], 0"}),
}


def check_slice(data, meta, label, start, end, checks):
    section, code = decode_window(data, meta, start, end - start)
    if not code or code[-1].address + code[-1].size != end:
        raise ValueError(f"incomplete static decode: {label}")
    observed = {ins.address: f"{ins.mnemonic} {ins.op_str}".strip() for ins in code}
    for address, expected in checks.items():
        if observed.get(address) != expected:
            raise ValueError(f"static contract changed at {address:08X}: {observed.get(address)} != {expected}")
    return {"label": label, "start": f"0x{start:08X}", "end_exclusive": f"0x{end:08X}",
            "section": section["name"], "instruction_count": str(len(code)), "checked_sites": str(len(checks)),
            "slice_sha256": hashlib.sha256(b"".join(ins.bytes for ins in code)).hexdigest().upper()}


def reconcile(rows, chain):
    groups = defaultdict(list)
    key_fields = ("model_archive", "model_object", "layer", "primitive")
    def key(row): return tuple(row[f] for f in key_fields)
    for row in rows:
        if row["status"] != "verified-bounded-eagl-command-parameter-layout" or row["original_semantics"] != "not-claimed":
            raise ValueError("invalid/promoted parameter row")
        groups[key(row)].append(row)
    if len({key(row) for row in chain}) != len(chain) or set(groups) != {key(row) for row in chain}:
        raise ValueError("parameter/chain primitive identity mismatch")
    for primitive in chain:
        entries = groups[key(primitive)]
        ids = [int(token) for token in primitive["command_ids"].split(",")]
        cursor = int(primitive["global_parameters_offset"], 0)
        command_offset = int(primitive["command_stream_offset"], 0)
        if len(entries) != len(ids) or len(ids) != int(primitive["command_count"]):
            raise ValueError("command cardinality mismatch")
        for index, (row, command_id) in enumerate(zip(entries, ids)):
            _, _, words, consumed, _ = HANDLERS[command_id]
            if (int(row["command_index"]) != index or int(row["command_id"]) != command_id or
                int(row["section"]) != int(primitive["command_section_index"]) or
                int(row["consumed_bytes"]) != consumed or int(row["parameter_cursor"], 0) != cursor or
                int(row["command_offset"], 0) != command_offset):
                raise ValueError("parameter cursor / source command contract mismatch")
            cursor += consumed
            command_offset += words * 4
            if bool(row["parameter_word1"]) != bool(consumed) or bool(row["parameter_word0"]) != bool(consumed):
                raise ValueError("missing/spurious parameter pair")
            if row["address_scope"] == "external-or-unresolved-relocation" and (row["raw_target_words"] or row["target_symbols"] or row["vb_base_symbol"]):
                raise ValueError("external symbol treated as a local raw pointer")
            if command_id == 9 and (row["parameter_relocations"] != "2:" or not row["vb_base_symbol"].startswith("__VB") or
                                    row["command_stride"] != row["buffer_stride"]):
                raise ValueError("fetch does not match its exact local base record")
    fetches = [r for r in rows if r["command_id"] == "9"]
    draws = [r for r in rows if r["command_id"] == "26"]
    both = sum(any(r["command_id"] == "15" and "SHAPENAME=" in r["parameter_relocations"] for r in group) and
               any(r["command_id"] == "16" and r["parameter_relocations"].startswith("2:__EAGL::GeoPrimState:::") for r in group)
               for group in groups.values())
    return {"primitive_rows": len(groups), "command_rows": len(rows), "parameter_pairs": sum(bool(r["parameter_word1"]) for r in rows),
            "fetch_base_record_matches": len(fetches), "fetch_stride_matches": len(fetches),
            "fetch_runtime_selector_unresolved": sum(int(r["auxiliary"], 0) != 0 for r in fetches),
            "draw_commands": len(draws), "draw_condition_nonzero": sum(int(r["draw_condition"]) != 0 for r in draws),
            "tar_state_parameters": sum(r["command_id"] == "15" and r["parameter_relocations"].startswith("2:__EAGL::TAR:::") and "SHAPENAME=" in r["parameter_relocations"] for r in rows),
            "geo_state_parameters": sum(r["command_id"] == "16" and r["parameter_relocations"].startswith("2:__EAGL::GeoPrimState:::") for r in rows),
            "primitives_with_both_state_parameters": both}


def negative_controls(data, meta, rows, chain):
    """Mutation tests affect memory copies only, never the input image/reports."""
    def must_reject(action):
        try:
            action()
        except (ValueError, KeyError):
            return
        raise AssertionError("negative control unexpectedly accepted invalid evidence")

    mutations = [
        (0, "consumed_bytes", "8"),
        (0, "parameter_cursor", "0xFFFFFFFF"),
        (next(i for i, r in enumerate(rows) if r["command_id"] == "9"), "buffer_stride", "257"),
        (next(i for i, r in enumerate(rows) if r["address_scope"] == "external-or-unresolved-relocation"), "raw_target_words", "0x00000000"),
    ]
    for index, field, value in mutations:
        altered = list(rows)
        altered[index] = dict(rows[index], **{field: value})
        must_reject(lambda: reconcile(altered, chain))
    must_reject(lambda: reconcile(rows + [rows[0]], chain))
    changed = bytearray(data)
    offset = virtual_to_file_offset(0x1A468C, meta["fields"]["base_address"], len(data), meta["sections"])
    changed[offset] = 0x90  # remove the direct indexed-draw call in the memory copy
    start, end, _, _, checks = HANDLERS[26]
    must_reject(lambda: check_slice(changed, meta, "changed-command-26", start, end, checks))
    return len(mutations) + 2


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("image", type=Path)
    p.add_argument("parameters", type=Path)
    p.add_argument("chain", type=Path)
    p.add_argument("--report", type=Path, required=True)
    p.add_argument("--summary", type=Path, required=True)
    args = p.parse_args()
    inputs = {path.resolve() for path in (args.image, args.parameters, args.chain)}
    if args.report.resolve() in inputs or args.summary.resolve() in inputs or args.report.resolve() == args.summary.resolve():
        raise ValueError("outputs must differ from all inputs and from each other")
    data = load_xbe(args.image, "default.xbe")
    digest = hashlib.sha256(data).hexdigest().upper()
    if digest != EXPECTED_XBE_SHA256 or len(data) != EXPECTED_XBE_SIZE:
        raise ValueError("retail XBE identity mismatch")
    meta = analyze_xbe(args.image, "default.xbe")
    _validate_dispatch_slice(data, meta)
    _, table, _, _, _ = _read_dispatch_table(data, meta)
    proofs = []
    for command_id, (start, end, words, consumed, checks) in HANDLERS.items():
        if table[command_id][0] != start:
            raise ValueError("handler differs from the dispatch table")
        proof = check_slice(data, meta, f"command-{command_id}", start, end, checks)
        proof.update(command_words=str(words), parameter_bytes=str(consumed))
        proofs.append(proof)
    for label, (start, end, checks) in SLICES.items():
        proof = check_slice(data, meta, label, start, end, checks)
        proof.update(command_words="", parameter_bytes="")
        proofs.append(proof)
    parameter_rows, chain_rows = read_tsv(args.parameters), read_tsv(args.chain)
    summary = reconcile(parameter_rows, chain_rows)
    expected = {"primitive_rows": 2364, "command_rows": 36779, "parameter_pairs": 32051,
                "fetch_base_record_matches": 11818, "fetch_stride_matches": 11818,
                "fetch_runtime_selector_unresolved": 7092, "draw_commands": 2364,
                "draw_condition_nonzero": 2364, "tar_state_parameters": 2364,
                "geo_state_parameters": 1612, "primitives_with_both_state_parameters": 1612}
    if summary != expected:
        raise ValueError(f"retail asset population changed: {summary}")
    summary.update(xbe_sha256=digest,
                   parameter_report_sha256=hashlib.sha256(args.parameters.read_bytes()).hexdigest().upper(),
                   chain_report_sha256=hashlib.sha256(args.chain.read_bytes()).hexdigest().upper(),
                   static_spans_validated=len(proofs), static_sites_validated=sum(int(r["checked_sites"]) for r in proofs),
                   negative_controls_passed=negative_controls(data, meta, parameter_rows, chain_rows),
                   gpu_method_headers_verified=3, draw_opcode_binding=DRAW_BINDING,
                   runtime_selector="unresolved", vertex_attribute_binding="not-proven",
                   full_rendering_binding="not-proven", reference=REFERENCE,
                   status=STATUS, original_semantics="not-claimed")
    _write_tsv(args.report, proofs[0].keys(), proofs)
    summary["static_report_sha256"] = hashlib.sha256(args.report.read_bytes()).hexdigest().upper()
    _write_tsv(args.summary, summary.keys(), [summary])
    for name, value in summary.items():
        print(f"{name}: {value}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
