"""Build a bounded CFG for one-hop nested direct-call destinations.

The input is the bounded CFG report for direct-call destinations observed in
indexed CFG slices.  This audit inspects the direct-call destinations reported
inside those bounded windows, but does not execute code, follow another call,
recover a function boundary, or assign render/material/UV/skinning/rigging/
animation/morph/GPU semantics.
"""

from __future__ import annotations

import argparse
import csv
import hashlib
from pathlib import Path
from typing import Iterable

from parse_xbe import analyze_xbe, virtual_to_file_offset
from xbe_render_cfg_audit import decode_window, load_xbe, read_tsv, recover_cfg


STATUS = "verified-bounded-static-render-indirect-indexed-cfg-nested-call-target-cfg-slices"
ORIGINAL_SEMANTICS = "not-claimed"

REPORT_FIELDS = (
    "nested_call_target_virtual_address",
    "source_parent_target_addresses",
    "source_parent_target_rows",
    "source_parent_call_occurrences",
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
    "parent_target_rows",
    "parent_direct_call_occurrences",
    "distinct_nested_targets",
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
    "distinct_nested_direct_call_targets",
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


def collect_nested_targets(rows: list[dict[str, str]]) -> dict[int, dict[str, object]]:
    targets: dict[int, dict[str, object]] = {}
    for row in rows:
        parent = row.get("call_target_virtual_address", "")
        parent_target = _as_int(parent)
        if parent_target is None:
            continue
        addresses = [
            value for value in row.get("direct_call_target_addresses", "").split("|")
            if value
        ]
        for address in addresses:
            nested_target = _as_int(address)
            if nested_target is None:
                continue
            entry = targets.setdefault(
                nested_target,
                {"source_parents": set(), "source_rows": 0, "call_occurrences": 0},
            )
            entry["source_parents"].add(parent)
            entry["source_rows"] += 1
            entry["call_occurrences"] += 1
    return targets


def analyze_target(
    data: bytes,
    result: dict,
    target: int,
    metadata: dict[str, object],
    max_bytes: int,
) -> dict[str, str]:
    section, instructions = decode_window(data, result, target, max_bytes)
    offset = virtual_to_file_offset(
        target,
        result["fields"]["base_address"],
        len(data),
        result["sections"],
    )
    if offset is None:
        raise ValueError(f"target {_address(target)} does not map to XBE bytes")
    available = min(max_bytes, section["file_address"] + section["file_size"] - offset)
    cfg = recover_cfg(instructions, target)
    direct_targets = sorted(cfg["direct_call_targets"])
    return {
        "nested_call_target_virtual_address": _address(target),
        "source_parent_target_addresses": "|".join(sorted(metadata["source_parents"])),
        "source_parent_target_rows": str(metadata["source_rows"]),
        "source_parent_call_occurrences": str(metadata["call_occurrences"]),
        "section": section["name"],
        "file_offset": _address(offset),
        "available_bytes": str(available),
        "linear_instruction_count": str(len(instructions)),
        "reachable_instruction_count": str(len(cfg["reachable"])),
        "basic_block_count": str(cfg["basic_blocks"]),
        "reachable_ret_count": str(cfg["reachable_ret_count"]),
        "conditional_branch_count": str(cfg["conditional_branch_count"]),
        "unconditional_branch_count": str(cfg["unconditional_branch_count"]),
        "internal_branch_target_count": str(len(cfg["internal_branch_targets"])),
        "out_of_window_branch_target_count": str(cfg["out_of_window_branch_targets"]),
        "direct_call_count": str(cfg["direct_calls"]),
        "unique_direct_call_target_count": str(len(direct_targets)),
        "direct_call_target_addresses": "|".join(_address(value) for value in direct_targets),
        "indirect_control_transfer_count": str(cfg["indirect_control_transfers"]),
        "max_reachable_instruction_offset": str(cfg["max_reachable_instruction_offset"]),
        "status": STATUS,
        "original_semantics": ORIGINAL_SEMANTICS,
    }


def build_summary(
    image: str,
    internal_path: str,
    xbe_sha256: str,
    xbe_size: int,
    source_rows: list[dict[str, str]],
    target_rows: list[dict[str, str]],
    max_bytes: int,
) -> dict[str, str]:
    distinct_nested_calls = {
        address
        for row in target_rows
        for address in row.get("direct_call_target_addresses", "").split("|")
        if address
    }
    return {
        "image": image,
        "internal_path": internal_path,
        "xbe_sha256": xbe_sha256,
        "xbe_size": str(xbe_size),
        "selection_policy": f"bounded CFG over one-hop nested direct-call destinations; max_bytes={max_bytes}; calls not followed",
        "parent_target_rows": str(len(source_rows)),
        "parent_direct_call_occurrences": str(sum(int(row.get("direct_call_count", "0")) for row in source_rows)),
        "distinct_nested_targets": str(len(target_rows)),
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
        "distinct_nested_direct_call_targets": str(len(distinct_nested_calls)),
        "total_indirect_control_transfers": str(sum(int(row["indirect_control_transfer_count"]) for row in target_rows)),
        "status": STATUS,
        "provenance": "same-ISO-retail-XBE+bounded-CFG-over-one-hop-nested-direct-call-destinations;no-execution;no-call-following;no-function-boundary-claim;no-binding",
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
    parser.add_argument("parent_cfg_report", type=Path)
    parser.add_argument("--internal-path", default="default.xbe")
    parser.add_argument("--max-bytes", type=int, default=256)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()
    if args.max_bytes <= 0:
        raise SystemExit("--max-bytes deve ser positivo")
    data = load_xbe(args.image, args.internal_path)
    result = analyze_xbe(args.image, args.internal_path)
    source_rows = read_tsv(args.parent_cfg_report)
    targets = collect_nested_targets(source_rows)
    target_rows = [
        analyze_target(data, result, target, metadata, args.max_bytes)
        for target, metadata in sorted(targets.items())
    ]
    summary = build_summary(
        str(args.image), args.internal_path, hashlib.sha256(data).hexdigest().upper(),
        len(data), source_rows, target_rows, args.max_bytes)
    write_tsv(args.report, REPORT_FIELDS, target_rows)
    write_tsv(args.summary, SUMMARY_FIELDS, [summary])
    for key in SUMMARY_FIELDS:
        print(f"{key}: {summary[key]}")
    print(f"report: {args.report}")
    print(f"summary: {args.summary}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
