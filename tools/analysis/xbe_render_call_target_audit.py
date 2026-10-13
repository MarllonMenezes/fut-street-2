"""Inspect one-hop direct call targets reached by bounded render CFGs.

The input CFG slices are used only to collect direct immediate call targets.
Each distinct target is then disassembled in a small executable-section
window. Calls are deliberately not followed recursively, indirect transfers
remain unresolved, and no target is treated as a recovered function. The audit
is static, read-only and makes no mesh/material/UV/skinning/rigging/animation,
morph or GPU binding claim.
"""

from __future__ import annotations

import argparse
import csv
import hashlib
from pathlib import Path
from typing import Iterable

try:
    from capstone.x86_const import X86_OP_IMM
except ImportError as exc:  # pragma: no cover
    raise SystemExit("Capstone não está instalado no ambiente de análise.") from exc

from xbe_render_cfg_audit import decode_window, load_xbe, read_tsv, recover_cfg
from parse_xbe import analyze_xbe, virtual_to_file_offset


REPORT_FIELDS = (
    "call_target_virtual_address",
    "source_target_count",
    "source_target_addresses",
    "source_direct_call_occurrences",
    "section",
    "file_offset",
    "available_bytes",
    "instruction_count",
    "ret_found",
    "prologue_shape",
    "status",
    "original_semantics",
)

SUMMARY_FIELDS = (
    "image",
    "internal_path",
    "xbe_sha256",
    "xbe_size",
    "selection_policy",
    "source_render_target_windows",
    "source_windows_with_direct_calls",
    "unique_direct_call_targets",
    "mapped_executable_targets",
    "disassembled_targets",
    "ret_terminated_targets",
    "prologue_like_targets",
    "status",
    "provenance",
    "original_semantics",
)


def _as_int(value: str | None) -> int | None:
    if not value:
        return None
    try:
        return int(value, 0)
    except ValueError:
        return None


def aggregate_call_targets(
    data: bytes,
    result: dict,
    source_rows: list[dict[str, str]],
    source_max_bytes: int,
) -> tuple[dict[int, dict[str, object]], int]:
    targets: dict[int, dict[str, object]] = {}
    windows_with_direct_calls = 0
    for row in source_rows:
        address = _as_int(row.get("target_virtual_address"))
        if address is None:
            continue
        _, instructions = decode_window(data, result, address, source_max_bytes)
        cfg = recover_cfg(instructions, address)
        direct_targets = cfg["direct_call_targets"]
        if direct_targets:
            windows_with_direct_calls += 1
        for target in direct_targets:
            entry = targets.setdefault(target, {"source_addresses": set(), "occurrences": 0})
            entry["source_addresses"].add(address)
            entry["occurrences"] += sum(
                instruction.mnemonic.lower() == "call" and
                instruction.operands and
                instruction.operands[0].type == X86_OP_IMM and
                (instruction.operands[0].imm & 0xFFFFFFFF) == target
                for instruction in instructions
                if instruction.address in cfg["reachable"]
            )
    return targets, windows_with_direct_calls


def _prologue_shape(instructions: list) -> str:
    mnemonics = [instruction.mnemonic.lower() for instruction in instructions[:5]]
    if len(mnemonics) >= 2 and mnemonics[0] == "push" and mnemonics[1] == "mov":
        return "frame-pointer-like"
    if mnemonics and mnemonics[0] == "push" and "sub" in mnemonics:
        return "callee-save-stack-like"
    return "none"


def inspect_call_target(
    data: bytes,
    result: dict,
    target: int,
    source_addresses: set[int],
    occurrences: int,
    max_bytes: int,
) -> dict[str, str]:
    base = result["fields"]["base_address"]
    try:
        section, instructions = decode_window(data, result, target, max_bytes)
    except (StopIteration, ValueError):
        return {
            "call_target_virtual_address": f"0x{target:08X}",
            "source_target_count": str(len(source_addresses)),
            "source_target_addresses": "|".join(f"0x{value:08X}" for value in sorted(source_addresses)),
            "source_direct_call_occurrences": str(occurrences),
            "section": "",
            "file_offset": "",
            "available_bytes": "0",
            "instruction_count": "0",
            "ret_found": "False",
            "prologue_shape": "none",
            "status": "not-mapped-executable",
            "original_semantics": "not-claimed",
        }
    offset = virtual_to_file_offset(target, base, len(data), result["sections"])
    assert offset is not None
    available = min(max_bytes, section["file_address"] + section["file_size"] - offset)
    return {
        "call_target_virtual_address": f"0x{target:08X}",
        "source_target_count": str(len(source_addresses)),
        "source_target_addresses": "|".join(f"0x{value:08X}" for value in sorted(source_addresses)),
        "source_direct_call_occurrences": str(occurrences),
        "section": section["name"],
        "file_offset": f"0x{offset:08X}",
        "available_bytes": str(available),
        "instruction_count": str(len(instructions)),
        "ret_found": str(any(instruction.mnemonic.lower() in {"ret", "retf"} for instruction in instructions)),
        "prologue_shape": _prologue_shape(instructions),
        "status": "verified-bounded-static-call-target-slice",
        "original_semantics": "not-claimed",
    }


def build_summary(
    image: str,
    internal_path: str,
    xbe_sha256: str,
    xbe_size: int,
    source_rows: list[dict[str, str]],
    targets: dict[int, dict[str, object]],
    target_rows: list[dict[str, str]],
    windows_with_direct_calls: int,
    max_bytes: int,
) -> dict[str, str]:
    return {
        "image": image,
        "internal_path": internal_path,
        "xbe_sha256": xbe_sha256,
        "xbe_size": str(xbe_size),
        "selection_policy": f"direct-call-targets-from-bounded-render-cfg; max_bytes={max_bytes}; no-recursive-following",
        "source_render_target_windows": str(len(source_rows)),
        "source_windows_with_direct_calls": str(windows_with_direct_calls),
        "unique_direct_call_targets": str(len(targets)),
        "mapped_executable_targets": str(sum(row["status"] != "not-mapped-executable" for row in target_rows)),
        "disassembled_targets": str(sum(row["instruction_count"] != "0" for row in target_rows)),
        "ret_terminated_targets": str(sum(row["ret_found"] == "True" for row in target_rows)),
        "prologue_like_targets": str(sum(row["prologue_shape"] != "none" for row in target_rows)),
        "status": "verified-static-render-call-target-slices",
        "provenance": "same-ISO-retail-XBE+one-hop-direct-call-targets-from-bounded-render-CFG; no-execution; no-function-boundary-claim; no-binding",
        "original_semantics": "not-claimed",
    }


def write_tsv(path: Path, fields: tuple[str, ...], rows: Iterable[dict[str, str]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=fields, delimiter="\t", lineterminator="\n")
        writer.writeheader()
        writer.writerows({field: row.get(field, "") for field in fields} for row in rows)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("image", type=Path)
    parser.add_argument("cfg_report", type=Path)
    parser.add_argument("--internal-path", default="default.xbe")
    parser.add_argument("--source-max-bytes", type=int, default=2048)
    parser.add_argument("--max-bytes", type=int, default=256)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()
    if args.source_max_bytes <= 0 or args.max_bytes <= 0:
        raise SystemExit("os limites de bytes devem ser positivos")
    source_rows = read_tsv(args.cfg_report)
    result = analyze_xbe(args.image, args.internal_path)
    data = load_xbe(args.image, args.internal_path)
    targets, windows_with_direct_calls = aggregate_call_targets(
        data, result, source_rows, args.source_max_bytes)
    target_rows = [
        inspect_call_target(
            data,
            result,
            target,
            values["source_addresses"],
            values["occurrences"],
            args.max_bytes,
        )
        for target, values in sorted(targets.items())
    ]
    summary = build_summary(
        str(args.image),
        args.internal_path,
        hashlib.sha256(data).hexdigest(),
        len(data),
        source_rows,
        targets,
        target_rows,
        windows_with_direct_calls,
        args.max_bytes,
    )
    write_tsv(args.report, REPORT_FIELDS, target_rows)
    write_tsv(args.summary, SUMMARY_FIELDS, [summary])
    print(
        f"source_windows: {summary['source_render_target_windows']}; "
        f"unique_direct_call_targets: {summary['unique_direct_call_targets']}; "
        f"disassembled_targets: {summary['disassembled_targets']}; "
        f"ret_terminated_targets: {summary['ret_terminated_targets']}; "
        f"status: {summary['status']}"
    )
    print(f"report: {args.report}")
    print(f"summary: {args.summary}")
    print("original_semantics: not-claimed")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
