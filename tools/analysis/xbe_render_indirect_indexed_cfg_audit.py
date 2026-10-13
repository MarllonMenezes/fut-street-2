"""Build bounded CFG slices for candidates from an indexed render table.

The indexed-target audit records bounded executable windows but deliberately
does not follow their direct branches.  This companion audit applies the same
bounded reachability routine used by the render CFG audit to each distinct
indexed-table candidate.  Calls are recorded but not followed, indirect
transfers remain unresolved, and no slice is promoted to a recovered function
or a render/material/UV/skinning/rigging/animation/morph/GPU binding.
"""

from __future__ import annotations

import argparse
import csv
import hashlib
from pathlib import Path
from typing import Iterable

from parse_xbe import analyze_xbe, virtual_to_file_offset
from xbe_render_cfg_audit import decode_window, load_xbe, read_tsv, recover_cfg


STATUS = "verified-bounded-static-render-indirect-indexed-cfg-slices"
ORIGINAL_SEMANTICS = "not-claimed"

REPORT_FIELDS = (
    "candidate_target_virtual_address",
    "table_entry_indices",
    "entry_occurrence_count",
    "section",
    "file_offset",
    "available_bytes",
    "linear_instruction_count",
    "reachable_instruction_count",
    "basic_block_count",
    "reachable_ret_count",
    "conditional_branch_count",
    "unconditional_branch_count",
    "internal_branch_target_count",
    "out_of_window_branch_target_count",
    "direct_call_count",
    "unique_direct_call_target_count",
    "direct_call_target_addresses",
    "indirect_control_transfer_count",
    "max_reachable_instruction_offset",
    "status",
    "original_semantics",
)

SUMMARY_FIELDS = (
    "image",
    "internal_path",
    "xbe_sha256",
    "xbe_size",
    "selection_policy",
    "candidate_targets",
    "analyzed_targets",
    "targets_with_reachable_cfg",
    "targets_with_reachable_ret",
    "total_linear_instructions",
    "total_reachable_instructions",
    "total_basic_blocks",
    "total_conditional_branches",
    "total_unconditional_branches",
    "total_internal_branch_targets",
    "total_out_of_window_branch_targets",
    "total_direct_calls",
    "total_unique_direct_call_targets",
    "distinct_direct_call_targets",
    "total_indirect_control_transfers",
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


def analyze_indexed_target(
    data: bytes,
    result: dict,
    row: dict[str, str],
    max_bytes: int,
) -> dict[str, str]:
    """Return only bounded CFG facts for one indexed-table candidate."""

    address = _as_int(row.get("candidate_target_virtual_address"))
    if address is None:
        raise ValueError("indexed target report row has no virtual address")
    section, instructions = decode_window(data, result, address, max_bytes)
    offset = virtual_to_file_offset(
        address,
        result["fields"]["base_address"],
        len(data),
        result["sections"],
    )
    if offset is None:
        raise ValueError(f"target {_address(address)} does not map to XBE bytes")
    available = min(max_bytes, section["file_address"] + section["file_size"] - offset)
    cfg = recover_cfg(instructions, address)
    reachable = cfg["reachable"]
    status = STATUS if reachable else "not-recovered"
    direct_call_targets = sorted(cfg["direct_call_targets"])
    return {
        "candidate_target_virtual_address": _address(address),
        "table_entry_indices": row.get("table_entry_indices", ""),
        "entry_occurrence_count": row.get("entry_occurrence_count", ""),
        "section": section["name"],
        "file_offset": _address(offset),
        "available_bytes": str(available),
        "linear_instruction_count": str(len(instructions)),
        "reachable_instruction_count": str(len(reachable)),
        "basic_block_count": str(cfg["basic_blocks"]),
        "reachable_ret_count": str(cfg["reachable_ret_count"]),
        "conditional_branch_count": str(cfg["conditional_branch_count"]),
        "unconditional_branch_count": str(cfg["unconditional_branch_count"]),
        "internal_branch_target_count": str(len(cfg["internal_branch_targets"])),
        "out_of_window_branch_target_count": str(cfg["out_of_window_branch_targets"]),
        "direct_call_count": str(cfg["direct_calls"]),
        "unique_direct_call_target_count": str(len(direct_call_targets)),
        "direct_call_target_addresses": "|".join(_address(value) for value in direct_call_targets),
        "indirect_control_transfer_count": str(cfg["indirect_control_transfers"]),
        "max_reachable_instruction_offset": str(cfg["max_reachable_instruction_offset"]),
        "status": status,
        "original_semantics": ORIGINAL_SEMANTICS,
    }


def build_summary(
    image: str,
    internal_path: str,
    xbe_sha256: str,
    xbe_size: int,
    selected_rows: list[dict[str, str]],
    target_rows: list[dict[str, str]],
    max_bytes: int,
) -> dict[str, str]:
    return {
        "image": image,
        "internal_path": internal_path,
        "xbe_sha256": xbe_sha256,
        "xbe_size": str(xbe_size),
        "selection_policy": f"bounded CFG over indexed-table candidate windows; max_bytes={max_bytes}; direct calls not followed",
        "candidate_targets": str(len(selected_rows)),
        "analyzed_targets": str(len(target_rows)),
        "targets_with_reachable_cfg": str(sum(int(row["reachable_instruction_count"]) > 0 for row in target_rows)),
        "targets_with_reachable_ret": str(sum(int(row["reachable_ret_count"]) > 0 for row in target_rows)),
        "total_linear_instructions": str(sum(int(row["linear_instruction_count"]) for row in target_rows)),
        "total_reachable_instructions": str(sum(int(row["reachable_instruction_count"]) for row in target_rows)),
        "total_basic_blocks": str(sum(int(row["basic_block_count"]) for row in target_rows)),
        "total_conditional_branches": str(sum(int(row["conditional_branch_count"]) for row in target_rows)),
        "total_unconditional_branches": str(sum(int(row["unconditional_branch_count"]) for row in target_rows)),
        "total_internal_branch_targets": str(sum(int(row["internal_branch_target_count"]) for row in target_rows)),
        "total_out_of_window_branch_targets": str(sum(int(row["out_of_window_branch_target_count"]) for row in target_rows)),
        "total_direct_calls": str(sum(int(row["direct_call_count"]) for row in target_rows)),
        "total_unique_direct_call_targets": str(sum(int(row["unique_direct_call_target_count"]) for row in target_rows)),
        "distinct_direct_call_targets": str(len({
            address
            for row in target_rows
            for address in row.get("direct_call_target_addresses", "").split("|")
            if address
        })),
        "total_indirect_control_transfers": str(sum(int(row["indirect_control_transfer_count"]) for row in target_rows)),
        "status": STATUS,
        "provenance": "same-ISO-retail-XBE+bounded-reachability-over-indexed-table-candidates;no-execution;no-call-following;no-function-boundary-claim;no-binding",
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
    parser.add_argument("indexed_target_report", type=Path)
    parser.add_argument("--internal-path", default="default.xbe")
    parser.add_argument("--max-bytes", type=int, default=256)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()
    if args.max_bytes <= 0:
        raise SystemExit("--max-bytes deve ser positivo")

    selected_rows = read_tsv(args.indexed_target_report)
    result = analyze_xbe(args.image, args.internal_path)
    data = load_xbe(args.image, args.internal_path)
    target_rows = [
        analyze_indexed_target(data, result, row, args.max_bytes)
        for row in selected_rows
    ]
    summary = build_summary(
        str(args.image),
        args.internal_path,
        hashlib.sha256(data).hexdigest().upper(),
        len(data),
        selected_rows,
        target_rows,
        args.max_bytes,
    )
    write_tsv(args.report, REPORT_FIELDS, target_rows)
    write_tsv(args.summary, SUMMARY_FIELDS, [summary])
    for key in SUMMARY_FIELDS:
        print(f"{key}: {summary[key]}")
    print(f"report: {args.report}")
    print(f"summary: {args.summary}")
    print("note: CFG slices are bounded static observations; calls and indirect transfers remain unresolved")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
