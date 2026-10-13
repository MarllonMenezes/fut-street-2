#!/usr/bin/env python3
"""Independently reparse the animation-bank catalog tail-range audit."""

from __future__ import annotations

import argparse
import csv
from pathlib import Path


STATUS = "verified-independent-animation-bank-catalog-tail-range-control"
SOURCE_STATUS = "verified-structural-animation-bank-tail-range"


def read_tsv(path: Path) -> list[dict[str, str]]:
    with path.open("r", encoding="utf-8", newline="") as handle:
        return list(csv.DictReader(handle, delimiter="\t"))


def write_summary(path: Path, values: dict[str, str]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.writer(handle, delimiter="\t", lineterminator="\n")
        writer.writerow(("field", "value"))
        for key, value in values.items():
            writer.writerow((key, value))


def as_int(row: dict[str, str], key: str) -> int:
    return int(row[key], 0)


def as_bool(row: dict[str, str], key: str) -> bool:
    return row[key].lower() == "true"


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("report", type=Path)
    parser.add_argument("source_summary", type=Path)
    parser.add_argument("--report", dest="control_report", type=Path, required=True)
    parser.add_argument("--summary", dest="control_summary", type=Path, required=True)
    args = parser.parse_args()

    report_rows = read_tsv(args.report)
    source_rows = read_tsv(args.source_summary)
    if len(source_rows) != 14 or source_rows[0].get("field") != "catalog_banks":
        raise ValueError("unexpected source summary shape")
    source = {row["field"]: row["value"] for row in source_rows}
    if not report_rows:
        raise ValueError("tail-range report is empty")

    required_columns = {
        "catalog_entry",
        "bank_name",
        "catalog_file_count",
        "elf_valid",
        "tail_header_shape_valid",
        "tail_entry_count_matches_catalog_double",
        "tail_range_within_data",
        "tail_range_aligned",
        "tail_range_length_matches_catalog_stride",
        "tail_range_relocation_count_matches_catalog_double",
        "tail_range_start",
        "tail_range_end",
        "tail_range_byte_count",
        "expected_catalog_stride_bytes",
        "status",
        "candidate_record_binding",
        "original_semantics",
    }
    if not required_columns.issubset(report_rows[0]):
        missing = sorted(required_columns.difference(report_rows[0]))
        raise ValueError(f"tail-range report is missing columns: {missing}")

    duplicate_catalog_entries = len(report_rows) - len({row["catalog_entry"] for row in report_rows})
    duplicate_bank_names = len(report_rows) - len({row["bank_name"] for row in report_rows})
    valid_rows = [row for row in report_rows if as_bool(row, "elf_valid")]
    shape_rows = [row for row in valid_rows if as_bool(row, "tail_header_shape_valid")]
    count_rows = [row for row in valid_rows if as_bool(row, "tail_entry_count_matches_catalog_double")]
    within_rows = [row for row in valid_rows if as_bool(row, "tail_range_within_data")]
    aligned_rows = [row for row in valid_rows if as_bool(row, "tail_range_aligned")]
    length_rows = [row for row in valid_rows if as_bool(row, "tail_range_length_matches_catalog_stride")]
    verified_rows = [
        row
        for row in valid_rows
        if row["status"] == SOURCE_STATUS
        and row["candidate_record_binding"] == "not-proven"
        and row["original_semantics"] == "not-claimed"
        and as_int(row, "tail_range_end") >= as_int(row, "tail_range_start")
        and as_int(row, "tail_range_byte_count")
        == as_int(row, "expected_catalog_stride_bytes")
    ]
    invalid_geometry_rows = [
        row
        for row in valid_rows
        if as_int(row, "tail_range_byte_count")
        != as_int(row, "expected_catalog_stride_bytes")
        or as_int(row, "tail_range_end") < as_int(row, "tail_range_start")
    ]
    no_relocation_rows = [
        row for row in valid_rows if int(row["relocations_in_tail_range"]) == 0
    ]
    catalog_rows = sum(int(row["catalog_file_count"]) for row in report_rows)
    source_matches = {
        "catalog_banks": source.get("catalog_banks"),
        "validated_banks": source.get("validated_banks"),
        "catalog_rows": source.get("catalog_rows"),
        "tail_header_shape_valid_rows": source.get("tail_header_shape_valid_rows"),
        "tail_entry_count_match_rows": source.get("tail_entry_count_match_rows"),
        "tail_range_within_data_rows": source.get("tail_range_within_data_rows"),
        "tail_range_aligned_rows": source.get("tail_range_aligned_rows"),
        "tail_range_length_match_rows": source.get("tail_range_length_match_rows"),
        "tail_range_relocation_count_match_rows": source.get("tail_range_relocation_count_match_rows"),
        "verified_structural_rows": source.get("verified_structural_rows"),
    }
    recomputed = {
        "catalog_banks": str(len(report_rows)),
        "validated_banks": str(len(valid_rows)),
        "catalog_rows": str(catalog_rows),
        "tail_header_shape_valid_rows": str(len(shape_rows)),
        "tail_entry_count_match_rows": str(len(count_rows)),
        "tail_range_within_data_rows": str(len(within_rows)),
        "tail_range_aligned_rows": str(len(aligned_rows)),
        "tail_range_length_match_rows": str(len(length_rows)),
        "tail_range_relocation_count_match_rows": str(
            sum(
                1
                for row in valid_rows
                if as_bool(row, "tail_range_relocation_count_matches_catalog_double")
            )
        ),
        "verified_structural_rows": str(len(verified_rows)),
    }
    summary_matches = all(source_matches[key] == value for key, value in recomputed.items())
    valid = (
        summary_matches
        and len(report_rows) == len(valid_rows)
        and len(report_rows) == len(verified_rows)
        and len(report_rows) == len(shape_rows)
        and len(report_rows) == len(count_rows)
        and len(report_rows) == len(within_rows)
        and len(report_rows) == len(aligned_rows)
        and len(report_rows) == len(length_rows)
        and recomputed["tail_range_relocation_count_match_rows"] == str(len(report_rows))
        and duplicate_catalog_entries == 0
        and duplicate_bank_names == 0
        and len(invalid_geometry_rows) == 0
        and len(no_relocation_rows) == 0
        and source.get("status") == SOURCE_STATUS
        and source.get("candidate_record_binding") == "not-proven"
        and source.get("original_semantics") == "not-claimed"
    )

    values = {
        "input_rows": str(len(report_rows)),
        "valid_input_rows": str(len(valid_rows)),
        "catalog_rows": str(catalog_rows),
        "duplicate_catalog_entries": str(duplicate_catalog_entries),
        "duplicate_bank_names": str(duplicate_bank_names),
        "header_shape_valid_rows": str(len(shape_rows)),
        "entry_count_match_rows": str(len(count_rows)),
        "range_within_data_rows": str(len(within_rows)),
        "range_aligned_rows": str(len(aligned_rows)),
        "range_length_match_rows": str(len(length_rows)),
        "range_relocation_count_match_rows": recomputed["tail_range_relocation_count_match_rows"],
        "no_relocation_rows": str(len(no_relocation_rows)),
        "invalid_geometry_rows": str(len(invalid_geometry_rows)),
        "verified_structural_rows": str(len(verified_rows)),
        "source_summary_matches_recomputed": str(summary_matches),
        "status": STATUS if valid else "invalid-independent-animation-bank-catalog-tail-range-control",
        "semantic_binding": "not-proven",
        "candidate_record_binding": "not-proven",
        "original_semantics": "not-claimed",
    }
    write_summary(args.control_summary, values)

    args.control_report.parent.mkdir(parents=True, exist_ok=True)
    with args.control_report.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.writer(handle, delimiter="\t", lineterminator="\n")
        writer.writerow(("catalog_entry", "bank_name", "valid", "status", "candidate_record_binding", "original_semantics"))
        for row in report_rows:
            row_valid = row in verified_rows and row["catalog_entry"] not in {""}
            writer.writerow((
                row["catalog_entry"],
                row["bank_name"],
                str(row_valid),
                row["status"],
                row["candidate_record_binding"],
                row["original_semantics"],
            ))

    print(
        f"status={values['status']} input_rows={len(report_rows)} "
        f"validated_rows={len(valid_rows)} verified_rows={len(verified_rows)}"
    )
    return 0 if valid else 1


if __name__ == "__main__":
    raise SystemExit(main())
