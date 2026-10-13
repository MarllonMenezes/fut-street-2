"""Record bounded call-site context for the indexed-CFG direct calls.

This audit retains the local instruction sequence around each reachable
immediate call observed in the indexed-table CFG slices.  It records repeated
shape only as observed bytes/instructions; it does not infer a wrapper, a
function boundary, an object layout, a renderer binding, or original meaning.
"""

from __future__ import annotations

import argparse
import csv
import hashlib
from pathlib import Path
from typing import Iterable

from capstone.x86_const import X86_OP_IMM, X86_OP_MEM, X86_REG_ECX

from parse_xbe import analyze_xbe
from xbe_render_cfg_audit import decode_window, load_xbe, read_tsv, recover_cfg


STATUS = "verified-bounded-static-render-indirect-indexed-call-sites"
ORIGINAL_SEMANTICS = "not-claimed"

REPORT_FIELDS = (
    "source_candidate_target_virtual_address",
    "call_instruction_virtual_address",
    "call_target_virtual_address",
    "context_instructions",
    "has_push_edx_before_lea",
    "has_lea_ecx_base_before_call",
    "lea_displacement",
    "has_ret_imm_4_after_call",
    "status",
    "original_semantics",
)

SUMMARY_FIELDS = (
    "image",
    "internal_path",
    "xbe_sha256",
    "xbe_size",
    "selection_policy",
    "source_cfg_candidates",
    "direct_call_occurrences",
    "distinct_call_targets",
    "candidate_targets_with_call_sites",
    "call_sites_with_push_edx_before_lea",
    "call_sites_with_lea_ecx_base_before_call",
    "call_sites_with_ret_imm_4_after_call",
    "distinct_lea_displacements",
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


def _address(value: int) -> str:
    return f"0x{value & 0xFFFFFFFF:08X}"


def _format_instruction(instruction) -> str:
    text = f"{instruction.mnemonic}"
    if instruction.op_str:
        text += f" {instruction.op_str}"
    return f"{instruction.address:08X}:{text}"


def _lea_ecx_displacement(instruction) -> int | None:
    if instruction.mnemonic.lower() != "lea" or len(instruction.operands) < 2:
        return None
    memory = instruction.operands[1]
    if memory.type != X86_OP_MEM or memory.mem.base != X86_REG_ECX:
        return None
    return memory.mem.disp


def collect_call_sites(
    data: bytes,
    result: dict,
    source_rows: list[dict[str, str]],
    max_bytes: int,
) -> list[dict[str, str]]:
    rows: list[dict[str, str]] = []
    for source_row in source_rows:
        source_address = _as_int(source_row.get("candidate_target_virtual_address"))
        if source_address is None:
            continue
        _, instructions = decode_window(data, result, source_address, max_bytes)
        cfg = recover_cfg(instructions, source_address)
        for index in sorted(cfg["reachable"]):
            instruction = instructions[index]
            if (
                instruction.mnemonic.lower() != "call" or
                not instruction.operands or
                instruction.operands[0].type != X86_OP_IMM
            ):
                continue
            call_target = instruction.operands[0].imm & 0xFFFFFFFF
            context_start = max(0, index - 3)
            context_end = min(len(instructions), index + 4)
            context = instructions[context_start:context_end]
            lea_displacement = _lea_ecx_displacement(instructions[index - 3]) if index >= 3 else None
            has_push_edx = (
                index >= 2 and
                instructions[index - 2].mnemonic.lower() == "push" and
                instructions[index - 2].op_str.lower() == "edx"
            )
            has_lea_ecx = lea_displacement is not None
            has_ret_imm_4 = any(
                candidate.mnemonic.lower() in {"ret", "retf"} and
                candidate.op_str.strip().lower() in {"4", "0x4"}
                for candidate in instructions[index + 1:index + 5]
            )
            rows.append({
                "source_candidate_target_virtual_address": _address(source_address),
                "call_instruction_virtual_address": _address(instruction.address),
                "call_target_virtual_address": _address(call_target),
                "context_instructions": "|".join(_format_instruction(item) for item in context),
                "has_push_edx_before_lea": str(has_push_edx),
                "has_lea_ecx_base_before_call": str(has_lea_ecx),
                "lea_displacement": "" if lea_displacement is None else _address(lea_displacement),
                "has_ret_imm_4_after_call": str(has_ret_imm_4),
                "status": STATUS,
                "original_semantics": ORIGINAL_SEMANTICS,
            })
    return rows


def build_summary(
    image: str,
    internal_path: str,
    xbe_sha256: str,
    xbe_size: int,
    source_rows: list[dict[str, str]],
    call_rows: list[dict[str, str]],
    max_bytes: int,
) -> dict[str, str]:
    return {
        "image": image,
        "internal_path": internal_path,
        "xbe_sha256": xbe_sha256,
        "xbe_size": str(xbe_size),
        "selection_policy": f"reachable immediate calls from bounded indexed-CFG slices; max_bytes={max_bytes}; local context only",
        "source_cfg_candidates": str(len(source_rows)),
        "direct_call_occurrences": str(len(call_rows)),
        "distinct_call_targets": str(len({row["call_target_virtual_address"] for row in call_rows})),
        "candidate_targets_with_call_sites": str(len({row["source_candidate_target_virtual_address"] for row in call_rows})),
        "call_sites_with_push_edx_before_lea": str(sum(row["has_push_edx_before_lea"] == "True" for row in call_rows)),
        "call_sites_with_lea_ecx_base_before_call": str(sum(row["has_lea_ecx_base_before_call"] == "True" for row in call_rows)),
        "call_sites_with_ret_imm_4_after_call": str(sum(row["has_ret_imm_4_after_call"] == "True" for row in call_rows)),
        "distinct_lea_displacements": str(len({row["lea_displacement"] for row in call_rows if row["lea_displacement"]})),
        "status": STATUS,
        "provenance": "same-ISO-retail-XBE+bounded-indexed-CFG-reachable-call-site-context;no-execution;no-call-following;no-function-boundary-claim;no-binding",
        "original_semantics": ORIGINAL_SEMANTICS,
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
    parser.add_argument("--max-bytes", type=int, default=256)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()
    if args.max_bytes <= 0:
        raise SystemExit("--max-bytes deve ser positivo")
    data = load_xbe(args.image, args.internal_path)
    result = analyze_xbe(args.image, args.internal_path)
    source_rows = read_tsv(args.cfg_report)
    call_rows = collect_call_sites(data, result, source_rows, args.max_bytes)
    summary = build_summary(
        str(args.image), args.internal_path, hashlib.sha256(data).hexdigest().upper(),
        len(data), source_rows, call_rows, args.max_bytes)
    write_tsv(args.report, REPORT_FIELDS, call_rows)
    write_tsv(args.summary, SUMMARY_FIELDS, [summary])
    for key in SUMMARY_FIELDS:
        print(f"{key}: {summary[key]}")
    print(f"report: {args.report}")
    print(f"summary: {args.summary}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
