"""Independently check the raw record that is first by opaque category.

The source report contains only validated numeric stream metadata.  This
control groups rows by the opaque object/ordinal key and checks cardinality,
category ordering, stride, and declared-bounds containment.  It does not call
the first record a position, geometry, UV, material, or GPU binding.
"""

from __future__ import annotations

import argparse
import csv
from collections import Counter, defaultdict
from pathlib import Path
from typing import Dict, Iterable, List, Sequence, Tuple


STATUS = "verified-independent-original-bounds-consistent-record-control"
INPUT_STATUS = "verified-bounded-original-numeric-stream-profile"
ORIGINAL_SEMANTICS = "not-claimed"
PROVENANCE = (
    "independent-TSV-reparse+opaque-object-ordinal-grouping;"
    "first-category-record-bounds-consistency;no-semantic-promotion"
)

REPORT_FIELDS = [
    "model_archive",
    "model_object",
    "ordinal_key",
    "stream_row_count",
    "category_sequence",
    "category_sequence_valid",
    "all_stream_rows_valid",
    "first_raw_category",
    "first_raw_stride",
    "first_raw_bounds_match",
    "later_raw_bounds_match_count",
    "first_raw_stride12",
    "status",
    "semantic_binding",
    "original_semantics",
    "issue",
]

SUMMARY_FIELDS = [
    "source_report",
    "input_rows",
    "object_ordinal_groups",
    "complete_five_stream_groups",
    "category_sequence_valid_groups",
    "valid_stream_rows",
    "first_raw_stride12_groups",
    "first_raw_bounds_match_groups",
    "first_raw_no_bounds_match_groups",
    "later_raw_bounds_match_groups",
    "all_bounds_matches_are_first_raw",
    "first_raw_category_histogram",
    "status",
    "semantic_binding",
    "provenance",
    "original_semantics",
]


def _read(path: Path) -> List[Dict[str, str]]:
    with path.open("r", encoding="utf-8-sig", newline="") as handle:
        return list(csv.DictReader(handle, delimiter="\t"))


def _integer(row: Dict[str, str], field: str) -> int:
    try:
        return int(row[field], 0) if field.endswith("_key") else int(row[field])
    except (KeyError, TypeError, ValueError) as exc:
        raise ValueError(f"invalid integer field {field!r}") from exc


def _field(value: object) -> str:
    return str(value).replace("\t", " ").replace("\r", " ").replace("\n", " ")


def _histogram(values: Iterable[int]) -> str:
    counts = Counter(values)
    return ",".join(f"{key}={counts[key]}" for key in sorted(counts))


def analyze(rows: Sequence[Dict[str, str]]) -> Tuple[List[Dict[str, str]], Dict[str, str]]:
    if not rows:
        raise ValueError("numeric stream report has no rows")

    grouped: dict[tuple[str, str, int], list[Dict[str, str]]] = defaultdict(list)
    for row in rows:
        grouped[(row.get("model_archive", ""), row.get("model_object", ""), _integer(row, "ordinal_key"))].append(row)

    report: List[Dict[str, str]] = []
    valid_stream_rows = 0
    complete_groups = 0
    sequence_groups = 0
    first_stride12_groups = 0
    first_bounds_groups = 0
    first_no_bounds_groups = 0
    later_bounds_groups = 0
    first_categories: list[int] = []

    for key in sorted(grouped):
        archive, object_name, ordinal = key
        group = sorted(grouped[key], key=lambda row: (_integer(row, "category_key"), row.get("symbol_name", "")))
        complete = len(group) == 5
        if complete:
            complete_groups += 1
        category_values = [_integer(row, "category_key") for row in group]
        category_sequence_valid = complete and category_values == list(range(category_values[0], category_values[0] + 5))
        if category_sequence_valid:
            sequence_groups += 1
        all_valid = all(
            row.get("status") == INPUT_STATUS and
            row.get("original_semantics") == ORIGINAL_SEMANTICS and
            row.get("opaque_key_valid") == "True"
            for row in group
        )
        valid_stream_rows += sum(all_valid for _ in group)
        first = group[0] if group else {}
        first_category = _integer(first, "category_key") if first else -1
        first_stride = _integer(first, "stream_stride") if first else 0
        first_bounds = first.get("declared_bounds_containment_float3") == "True"
        later_bounds = sum(row.get("declared_bounds_containment_float3") == "True" for row in group[1:])
        first_stride12 = first_stride == 12
        first_categories.append(first_category)
        first_stride12_groups += int(complete and first_stride12)
        first_bounds_groups += int(complete and first_bounds)
        first_no_bounds_groups += int(complete and not first_bounds)
        later_bounds_groups += int(complete and later_bounds > 0)
        valid = complete and category_sequence_valid and all_valid and first_stride12
        report.append(
            {
                "model_archive": archive,
                "model_object": object_name,
                "ordinal_key": str(ordinal),
                "stream_row_count": str(len(group)),
                "category_sequence": ",".join(str(value) for value in category_values),
                "category_sequence_valid": str(category_sequence_valid),
                "all_stream_rows_valid": str(all_valid),
                "first_raw_category": str(first_category),
                "first_raw_stride": str(first_stride),
                "first_raw_bounds_match": str(first_bounds),
                "later_raw_bounds_match_count": str(later_bounds),
                "first_raw_stride12": str(first_stride12),
                "status": STATUS if valid else "inconclusive-original-bounds-consistent-record-control",
                "semantic_binding": "not-proven",
                "original_semantics": ORIGINAL_SEMANTICS,
                "issue": "" if valid else "group structure or raw numeric relationship requires review",
            }
        )

    summary = {
        "source_report": "",
        "input_rows": str(len(rows)),
        "object_ordinal_groups": str(len(grouped)),
        "complete_five_stream_groups": str(complete_groups),
        "category_sequence_valid_groups": str(sequence_groups),
        "valid_stream_rows": str(valid_stream_rows),
        "first_raw_stride12_groups": str(first_stride12_groups),
        "first_raw_bounds_match_groups": str(first_bounds_groups),
        "first_raw_no_bounds_match_groups": str(first_no_bounds_groups),
        "later_raw_bounds_match_groups": str(later_bounds_groups),
        "all_bounds_matches_are_first_raw": str(later_bounds_groups == 0 and first_bounds_groups > 0),
        "first_raw_category_histogram": _histogram(first_categories),
        "status": STATUS if (
            len(grouped) > 0 and
            complete_groups == len(grouped) and
            sequence_groups == len(grouped) and
            valid_stream_rows == len(rows) and
            first_stride12_groups == len(grouped) and
            later_bounds_groups == 0 and
            first_bounds_groups > 0
        ) else "inconclusive-original-bounds-consistent-record-control",
        "semantic_binding": "not-proven",
        "provenance": PROVENANCE,
        "original_semantics": ORIGINAL_SEMANTICS,
    }
    return report, summary


def _write(path: Path, fields: Sequence[str], rows: Iterable[Dict[str, str]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=list(fields), delimiter="\t", lineterminator="\n")
        writer.writeheader()
        writer.writerows({field: _field(row.get(field, "")) for field in fields} for row in rows)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("source_report", type=Path)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()
    report, summary = analyze(_read(args.source_report))
    summary["source_report"] = str(args.source_report)
    _write(args.report, REPORT_FIELDS, report)
    _write(args.summary, SUMMARY_FIELDS, [summary])
    print("FIFA Street 2 independent original bounds-consistent raw-record control")
    for key in (
        "status", "input_rows", "object_ordinal_groups", "complete_five_stream_groups",
        "first_raw_stride12_groups", "first_raw_bounds_match_groups",
        "first_raw_no_bounds_match_groups", "later_raw_bounds_match_groups",
        "first_raw_category_histogram", "semantic_binding",
    ):
        print(f"{key}: {summary[key]}")
    return 0 if summary["status"] == STATUS else 1


if __name__ == "__main__":
    raise SystemExit(main())
