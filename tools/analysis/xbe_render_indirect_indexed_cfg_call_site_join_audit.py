"""Join indexed-table occurrences with bounded CFG call-site context.

The join preserves the distinction between call-site rows and table-entry
occurrences.  It is a structural consistency report only; it does not infer a
function boundary, object layout, field meaning, renderer binding, or original
semantics.
"""

from __future__ import annotations

import argparse
import csv
import hashlib
from pathlib import Path
from typing import Iterable

from xbe_render_cfg_audit import load_xbe, read_tsv


STATUS = "verified-bounded-static-render-indirect-indexed-call-site-join"
ORIGINAL_SEMANTICS = "not-claimed"

REPORT_FIELDS = (
    "table_base_addresses",
    "table_entry_indices",
    "entry_occurrence_count",
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
    "indexed_table_target_rows",
    "distinct_table_bases",
    "call_site_rows",
    "joined_call_site_rows",
    "distinct_call_targets",
    "distinct_table_entry_indices",
    "table_entry_occurrences_for_joined_call_sites",
    "duplicate_table_occurrences_over_call_sites",
    "call_sites_with_complete_local_shape",
    "status",
    "provenance",
    "original_semantics",
)


def join_rows(
    target_rows: list[dict[str, str]],
    call_site_rows: list[dict[str, str]],
) -> list[dict[str, str]]:
    targets = {
        row.get("candidate_target_virtual_address", ""): row
        for row in target_rows
    }
    joined: list[dict[str, str]] = []
    for call in call_site_rows:
        target = targets.get(call.get("source_candidate_target_virtual_address", ""))
        if target is None:
            continue
        joined.append({
            "table_base_addresses": target.get("table_base_addresses", ""),
            "table_entry_indices": target.get("table_entry_indices", ""),
            "entry_occurrence_count": target.get("entry_occurrence_count", ""),
            "source_candidate_target_virtual_address": call.get("source_candidate_target_virtual_address", ""),
            "call_instruction_virtual_address": call.get("call_instruction_virtual_address", ""),
            "call_target_virtual_address": call.get("call_target_virtual_address", ""),
            "context_instructions": call.get("context_instructions", ""),
            "has_push_edx_before_lea": call.get("has_push_edx_before_lea", ""),
            "has_lea_ecx_base_before_call": call.get("has_lea_ecx_base_before_call", ""),
            "lea_displacement": call.get("lea_displacement", ""),
            "has_ret_imm_4_after_call": call.get("has_ret_imm_4_after_call", ""),
            "status": STATUS,
            "original_semantics": ORIGINAL_SEMANTICS,
        })
    return joined


def _split_values(rows: list[dict[str, str]], field: str) -> set[str]:
    return {
        value
        for row in rows
        for value in row.get(field, "").split("|")
        if value
    }


def build_summary(
    image: str,
    internal_path: str,
    xbe_sha256: str,
    xbe_size: int,
    target_rows: list[dict[str, str]],
    call_site_rows: list[dict[str, str]],
    joined_rows: list[dict[str, str]],
) -> dict[str, str]:
    distinct_indices = _split_values(joined_rows, "table_entry_indices")
    entry_occurrences = sum(
        len(row.get("table_entry_indices", "").split("|"))
        for row in joined_rows
        if row.get("table_entry_indices")
    )
    complete_shape = sum(
        row.get("has_push_edx_before_lea") == "True" and
        row.get("has_lea_ecx_base_before_call") == "True" and
        row.get("has_ret_imm_4_after_call") == "True"
        for row in joined_rows
    )
    return {
        "image": image,
        "internal_path": internal_path,
        "xbe_sha256": xbe_sha256,
        "xbe_size": str(xbe_size),
        "selection_policy": "bounded join of indexed-table target occurrences with reachable CFG call-site context; no semantic promotion",
        "indexed_table_target_rows": str(len(target_rows)),
        "distinct_table_bases": str(len(_split_values(target_rows, "table_base_addresses"))),
        "call_site_rows": str(len(call_site_rows)),
        "joined_call_site_rows": str(len(joined_rows)),
        "distinct_call_targets": str(len(_split_values(joined_rows, "call_target_virtual_address"))),
        "distinct_table_entry_indices": str(len(distinct_indices)),
        "table_entry_occurrences_for_joined_call_sites": str(entry_occurrences),
        "duplicate_table_occurrences_over_call_sites": str(max(0, entry_occurrences - len(joined_rows))),
        "call_sites_with_complete_local_shape": str(complete_shape),
        "status": STATUS,
        "provenance": "same-ISO-retail-XBE+bounded-indexed-table-target-and-CFG-call-site-join;no-execution;no-call-following;no-function-boundary-claim;no-binding",
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
    parser.add_argument("call_site_report", type=Path)
    parser.add_argument("--internal-path", default="default.xbe")
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()
    data = load_xbe(args.image, args.internal_path)
    target_rows = read_tsv(args.indexed_target_report)
    call_site_rows = read_tsv(args.call_site_report)
    joined_rows = join_rows(target_rows, call_site_rows)
    summary = build_summary(
        str(args.image), args.internal_path, hashlib.sha256(data).hexdigest().upper(),
        len(data), target_rows, call_site_rows, joined_rows)
    write_tsv(args.report, REPORT_FIELDS, joined_rows)
    write_tsv(args.summary, SUMMARY_FIELDS, [summary])
    for key in SUMMARY_FIELDS:
        print(f"{key}: {summary[key]}")
    print(f"report: {args.report}")
    print(f"summary: {args.summary}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
