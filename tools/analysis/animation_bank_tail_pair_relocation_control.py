"""Independently reparse the animation-bank tail pair-relocation audit.

The control validates only the repeated two-relocation layout and raw-word
cardinality. It does not join tail ordinals to catalog names or assign record,
frame, pose, rig, timing, skinning, morph, or playback semantics.
"""

from __future__ import annotations

import argparse
import csv
from pathlib import Path


STATUS = "verified-independent-animation-bank-tail-pair-relocation-control"
SOURCE_STATUS = "verified-structural-animation-bank-tail-pair-relocations"
BINDING = "not-proven"
ORIGINAL = "not-claimed"

REQUIRED_COLUMNS = {
    "catalog_entry",
    "bank_name",
    "catalog_file_count",
    "elf_valid",
    "pair_ordinal_rows",
    "pair_rows_with_two_relocations",
    "pair_rows_with_expected_offsets",
    "rows_with_unexpected_relocation_multiplicity",
    "first_raw_words_within_data",
    "second_raw_words_within_data",
    "first_raw_words_at_known_symbols",
    "second_raw_words_at_known_symbols",
    "distinct_raw_word_pairs",
    "raw_word_pair_sha256",
    "distinct_pair_symbol_signatures",
    "pair_symbol_signature_sha256",
    "status",
    "candidate_record_binding",
    "original_semantics",
}


def read_rows(path: Path) -> list[dict[str, str]]:
    with path.open("r", encoding="utf-8-sig", newline="") as handle:
        return list(csv.DictReader(handle, delimiter="\t"))


def read_summary(path: Path) -> dict[str, str]:
    rows = read_rows(path)
    values: dict[str, str] = {}
    for row in rows:
        if set(row) != {"field", "value"}:
            raise ValueError(f"invalid summary columns in {path}")
        values[row["field"]] = row["value"]
    return values


def integer(row: dict[str, str], name: str) -> int:
    return int(row[name], 10)


def summary_integer(summary: dict[str, str], name: str) -> int:
    return int(summary[name], 10)


def bool_text(value: str) -> bool:
    return value.lower() == "true"


def control(source_report: Path, source_summary: Path) -> tuple[list[dict[str, str]], dict[str, str]]:
    rows = read_rows(source_report)
    summary = read_summary(source_summary)
    if not rows:
        raise ValueError("source report is empty")
    if not REQUIRED_COLUMNS.issubset(rows[0]):
        missing = sorted(REQUIRED_COLUMNS - set(rows[0]))
        raise ValueError(f"source report is missing columns: {missing}")

    output_rows: list[dict[str, str]] = []
    for row in rows:
        count = integer(row, "catalog_file_count")
        valid = (
            bool_text(row["elf_valid"])
            and integer(row, "pair_ordinal_rows") == count
            and integer(row, "pair_rows_with_two_relocations") == count
            and integer(row, "pair_rows_with_expected_offsets") == count
            and integer(row, "rows_with_unexpected_relocation_multiplicity") == 0
            and integer(row, "first_raw_words_within_data") == count
            and integer(row, "second_raw_words_within_data") == count
            and integer(row, "first_raw_words_at_known_symbols") == 0
            and integer(row, "second_raw_words_at_known_symbols") == 0
            and integer(row, "distinct_raw_word_pairs") == count
            and integer(row, "distinct_pair_symbol_signatures") == 1
            and row["status"] == SOURCE_STATUS
            and row["candidate_record_binding"] == BINDING
            and row["original_semantics"] == ORIGINAL
        )
        output_rows.append(
            {
                "catalog_entry": row["catalog_entry"],
                "bank_name": row["bank_name"],
                "catalog_file_count": str(count),
                "pair_layout_valid": str(valid),
                "raw_word_cardinality_valid": str(
                    integer(row, "distinct_raw_word_pairs") == count
                ),
                "opaque_symbol_signature_valid": str(
                    integer(row, "distinct_pair_symbol_signatures") == 1
                ),
                "source_status": row["status"],
                "status": STATUS if valid else "incomplete-independent-animation-bank-tail-pair-relocation-control",
                "candidate_record_binding": BINDING,
                "original_semantics": ORIGINAL,
                "issue": "" if valid else "source structural counters did not reconcile",
            }
        )

    expected_rows = summary_integer(summary, "catalog_banks")
    expected_catalog_rows = summary_integer(summary, "catalog_rows")
    summary_matches = (
        summary.get("status") == SOURCE_STATUS
        and summary.get("candidate_record_binding") == BINDING
        and summary.get("original_semantics") == ORIGINAL
        and len(rows) == expected_rows
        and summary_integer(summary, "validated_banks") == expected_rows
        and sum(integer(row, "catalog_file_count") for row in rows) == expected_catalog_rows
        and summary_integer(summary, "pair_ordinal_rows") == expected_catalog_rows
        and summary_integer(summary, "pair_rows_with_two_relocations") == expected_catalog_rows
        and summary_integer(summary, "pair_rows_with_expected_offsets") == expected_catalog_rows
        and summary_integer(summary, "rows_with_unexpected_relocation_multiplicity") == 0
        and summary_integer(summary, "first_raw_words_within_data") == expected_catalog_rows
        and summary_integer(summary, "second_raw_words_within_data") == expected_catalog_rows
        and summary_integer(summary, "first_raw_words_at_known_symbols") == 0
        and summary_integer(summary, "second_raw_words_at_known_symbols") == 0
        and summary_integer(summary, "distinct_raw_word_pairs") == expected_catalog_rows
        and summary_integer(summary, "banks_with_complete_pair_layout") == expected_rows
    )
    valid_rows = sum(row["status"] == STATUS for row in output_rows)
    output_summary = {
        "input_rows": str(len(rows)),
        "valid_input_rows": str(sum(bool_text(row["elf_valid"]) for row in rows)),
        "catalog_rows": str(sum(integer(row, "catalog_file_count") for row in rows)),
        "pair_ordinal_rows": str(sum(integer(row, "pair_ordinal_rows") for row in rows)),
        "pair_rows_with_two_relocations": str(
            sum(integer(row, "pair_rows_with_two_relocations") for row in rows)
        ),
        "pair_rows_with_expected_offsets": str(
            sum(integer(row, "pair_rows_with_expected_offsets") for row in rows)
        ),
        "rows_with_unexpected_relocation_multiplicity": str(
            sum(integer(row, "rows_with_unexpected_relocation_multiplicity") for row in rows)
        ),
        "first_raw_words_within_data": str(
            sum(integer(row, "first_raw_words_within_data") for row in rows)
        ),
        "second_raw_words_within_data": str(
            sum(integer(row, "second_raw_words_within_data") for row in rows)
        ),
        "first_raw_words_at_known_symbols": str(
            sum(integer(row, "first_raw_words_at_known_symbols") for row in rows)
        ),
        "second_raw_words_at_known_symbols": str(
            sum(integer(row, "second_raw_words_at_known_symbols") for row in rows)
        ),
        "distinct_raw_word_pairs": str(
            sum(integer(row, "distinct_raw_word_pairs") for row in rows)
        ),
        "banks_with_complete_pair_layout": str(valid_rows),
        "source_summary_matches_recomputed": str(summary_matches),
        "status": STATUS if summary_matches and valid_rows == len(rows) else "incomplete-" + STATUS,
        "semantic_binding": BINDING,
        "candidate_record_binding": BINDING,
        "original_semantics": ORIGINAL,
    }
    return output_rows, output_summary


def write_report(path: Path, rows: list[dict[str, str]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    fields = [
        "catalog_entry",
        "bank_name",
        "catalog_file_count",
        "pair_layout_valid",
        "raw_word_cardinality_valid",
        "opaque_symbol_signature_valid",
        "source_status",
        "status",
        "candidate_record_binding",
        "original_semantics",
        "issue",
    ]
    with path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=fields, delimiter="\t", lineterminator="\n")
        writer.writeheader()
        writer.writerows(rows)


def write_summary(path: Path, summary: dict[str, str]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.writer(handle, delimiter="\t", lineterminator="\n")
        writer.writerow(["field", "value"])
        writer.writerows(summary.items())


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("source_report", type=Path)
    parser.add_argument("source_summary", type=Path)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()
    rows, summary = control(args.source_report, args.source_summary)
    write_report(args.report, rows)
    write_summary(args.summary, summary)
    print(
        f"status={summary['status']} input_rows={summary['input_rows']} "
        f"valid_rows={summary['valid_input_rows']} catalog_rows={summary['catalog_rows']} "
        f"pair_rows={summary['pair_rows_with_expected_offsets']}"
    )
    return 0 if summary["status"] == STATUS else 1


if __name__ == "__main__":
    raise SystemExit(main())
