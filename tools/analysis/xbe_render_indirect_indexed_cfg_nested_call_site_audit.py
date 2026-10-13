"""Record local context for nested calls inside the indexed CFG destination.

The report filters the bounded call-site inventory to direct calls whose target
was observed inside the indexed CFG destination slice.  It is a byte/instruction
observation only: no function boundary, object layout, renderer binding, or
original semantics is inferred.
"""

from __future__ import annotations

import argparse
import csv
import hashlib
from pathlib import Path
from typing import Iterable

from parse_xbe import analyze_xbe
from xbe_render_cfg_audit import load_xbe, read_tsv
from xbe_render_indirect_indexed_cfg_call_site_audit import collect_call_sites


STATUS = "verified-bounded-static-render-indirect-indexed-nested-call-sites"
ORIGINAL_SEMANTICS = "not-claimed"

REPORT_FIELDS = (
    "source_parent_target_virtual_address",
    "call_instruction_virtual_address",
    "nested_call_target_virtual_address",
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
    "parent_cfg_target_rows",
    "nested_call_target_rows",
    "distinct_nested_call_targets",
    "nested_call_sites_with_push_edx_before_lea",
    "nested_call_sites_with_lea_ecx_base_before_call",
    "nested_call_sites_with_ret_imm_4_after_call",
    "distinct_nested_lea_displacements",
    "status",
    "provenance",
    "original_semantics",
)


def _nested_targets(parent_rows: list[dict[str, str]]) -> set[str]:
    return {
        value
        for row in parent_rows
        for value in row.get("direct_call_target_addresses", "").split("|")
        if value
    }


def filter_nested_call_sites(
    parent_rows: list[dict[str, str]],
    call_rows: list[dict[str, str]],
) -> list[dict[str, str]]:
    targets = _nested_targets(parent_rows)
    result: list[dict[str, str]] = []
    for row in call_rows:
        if row.get("call_target_virtual_address", "") not in targets:
            continue
        result.append({
            "source_parent_target_virtual_address": row.get("source_candidate_target_virtual_address", ""),
            "call_instruction_virtual_address": row.get("call_instruction_virtual_address", ""),
            "nested_call_target_virtual_address": row.get("call_target_virtual_address", ""),
            "context_instructions": row.get("context_instructions", ""),
            "has_push_edx_before_lea": row.get("has_push_edx_before_lea", ""),
            "has_lea_ecx_base_before_call": row.get("has_lea_ecx_base_before_call", ""),
            "lea_displacement": row.get("lea_displacement", ""),
            "has_ret_imm_4_after_call": row.get("has_ret_imm_4_after_call", ""),
            "status": STATUS,
            "original_semantics": ORIGINAL_SEMANTICS,
        })
    return result


def build_summary(
    image: str,
    internal_path: str,
    xbe_sha256: str,
    xbe_size: int,
    parent_rows: list[dict[str, str]],
    nested_rows: list[dict[str, str]],
    max_bytes: int,
) -> dict[str, str]:
    return {
        "image": image,
        "internal_path": internal_path,
        "xbe_sha256": xbe_sha256,
        "xbe_size": str(xbe_size),
        "selection_policy": f"bounded local context for one-hop nested direct calls; max_bytes={max_bytes}; no semantic promotion",
        "parent_cfg_target_rows": str(len(parent_rows)),
        "nested_call_target_rows": str(len(nested_rows)),
        "distinct_nested_call_targets": str(len({row["nested_call_target_virtual_address"] for row in nested_rows})),
        "nested_call_sites_with_push_edx_before_lea": str(sum(row["has_push_edx_before_lea"] == "True" for row in nested_rows)),
        "nested_call_sites_with_lea_ecx_base_before_call": str(sum(row["has_lea_ecx_base_before_call"] == "True" for row in nested_rows)),
        "nested_call_sites_with_ret_imm_4_after_call": str(sum(row["has_ret_imm_4_after_call"] == "True" for row in nested_rows)),
        "distinct_nested_lea_displacements": str(len({row["lea_displacement"] for row in nested_rows if row["lea_displacement"]})),
        "status": STATUS,
        "provenance": "same-ISO-retail-XBE+bounded-one-hop-nested-call-site-context;no-execution;no-call-following;no-function-boundary-claim;no-binding",
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
    parent_rows = read_tsv(args.parent_cfg_report)
    source_rows = [
        {"candidate_target_virtual_address": row.get("call_target_virtual_address", "")}
        for row in parent_rows
    ]
    call_rows = collect_call_sites(data, result, source_rows, args.max_bytes)
    nested_rows = filter_nested_call_sites(parent_rows, call_rows)
    summary = build_summary(
        str(args.image), args.internal_path, hashlib.sha256(data).hexdigest().upper(),
        len(data), parent_rows, nested_rows, args.max_bytes)
    write_tsv(args.report, REPORT_FIELDS, nested_rows)
    write_tsv(args.summary, SUMMARY_FIELDS, [summary])
    for key in SUMMARY_FIELDS:
        print(f"{key}: {summary[key]}")
    print(f"report: {args.report}")
    print(f"summary: {args.summary}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
