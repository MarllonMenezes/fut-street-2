"""Independently classify numeric bounds-match cardinality per original object.

This control deliberately consumes only the already bounded numeric-payload TSV.
It reports whether declared-bounds containment is absent, unique, or multiple
at object level.  A unique count is not promoted to a position binding: the TSV
does not identify a primitive/slot semantic, and all original semantics remain
unclaimed.
"""

from __future__ import annotations

import argparse
import csv
from collections import Counter
from pathlib import Path
from typing import Dict, Iterable, List, Sequence, Tuple


STATUS = "verified-independent-original-numeric-stream-ambiguity-control"
INPUT_STATUS = "verified-bounded-original-numeric-payload-structure"
ORIGINAL_SEMANTICS = "not-claimed"
PROVENANCE = (
    "independent-TSV-reparse+object-level-bounds-match-cardinality;"
    "no-stream-attribute-promotion"
)

REPORT_FIELDS = [
    "model_archive",
    "model_object",
    "validated_stream_count",
    "finite_float3_profile_count",
    "declared_bounds_containment_float3_count",
    "bounds_match_cardinality",
    "object_level_unique_bounds_match",
    "semantic_binding",
    "status",
    "original_semantics",
    "issue",
]

SUMMARY_FIELDS = [
    "source_report",
    "input_rows",
    "valid_input_rows",
    "objects_with_no_bounds_match",
    "objects_with_unique_bounds_match",
    "objects_with_multiple_bounds_matches",
    "objects_with_non_unique_or_missing_match",
    "bounds_match_cardinality_histogram",
    "semantic_binding",
    "status",
    "provenance",
    "original_semantics",
]


def _read(path: Path) -> List[Dict[str, str]]:
    with path.open("r", encoding="utf-8-sig", newline="") as handle:
        return list(csv.DictReader(handle, delimiter="\t"))


def _integer(row: Dict[str, str], field: str) -> int:
    try:
        return int(row[field])
    except (KeyError, TypeError, ValueError) as exc:
        raise ValueError(f"invalid integer field {field!r}") from exc


def _field(value: object) -> str:
    text = str(value)
    return text.replace("\t", " ").replace("\r", " ").replace("\n", " ")


def _histogram(values: Iterable[int]) -> str:
    counts = Counter(values)
    return ",".join(f"{key}={counts[key]}" for key in sorted(counts))


def analyze(rows: Sequence[Dict[str, str]]) -> Tuple[List[Dict[str, str]], Dict[str, str]]:
    if not rows:
        raise ValueError("numeric payload report has no rows")

    report: List[Dict[str, str]] = []
    cardinalities: List[int] = []
    valid_input_rows = 0
    no_match = 0
    unique_match = 0
    multiple_match = 0

    for row in rows:
        stream_count = _integer(row, "validated_stream_count")
        float3_count = _integer(row, "finite_float3_profile_count")
        bounds_count = _integer(row, "declared_bounds_containment_float3_count")
        input_valid = (
            row.get("status") == INPUT_STATUS
            and row.get("original_semantics") == ORIGINAL_SEMANTICS
            and stream_count > 0
            and 0 <= bounds_count <= float3_count <= stream_count
        )
        if input_valid:
            valid_input_rows += 1
            cardinalities.append(bounds_count)
            if bounds_count == 0:
                no_match += 1
                classification = "none"
            elif bounds_count == 1:
                unique_match += 1
                classification = "unique"
            else:
                multiple_match += 1
                classification = "multiple"
            issue = ""
        else:
            classification = "invalid-input"
            issue = "source status or numeric cardinality is invalid"

        report.append(
            {
                "model_archive": row.get("model_archive", ""),
                "model_object": row.get("model_object", ""),
                "validated_stream_count": str(stream_count),
                "finite_float3_profile_count": str(float3_count),
                "declared_bounds_containment_float3_count": str(bounds_count),
                "bounds_match_cardinality": classification,
                "object_level_unique_bounds_match": str(input_valid and bounds_count == 1),
                "semantic_binding": "not-proven",
                "status": STATUS if input_valid else "inconclusive-original-numeric-stream-ambiguity-control",
                "original_semantics": ORIGINAL_SEMANTICS,
                "issue": issue,
            }
        )

    summary = {
        "source_report": "",
        "input_rows": str(len(rows)),
        "valid_input_rows": str(valid_input_rows),
        "objects_with_no_bounds_match": str(no_match),
        "objects_with_unique_bounds_match": str(unique_match),
        "objects_with_multiple_bounds_matches": str(multiple_match),
        "objects_with_non_unique_or_missing_match": str(no_match + multiple_match),
        "bounds_match_cardinality_histogram": _histogram(cardinalities),
        "semantic_binding": "not-proven",
        "status": STATUS if valid_input_rows == len(rows) else "inconclusive-original-numeric-stream-ambiguity-control",
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
    print("FIFA Street 2 independent original numeric stream ambiguity control")
    print(f"status: {summary['status']}")
    print(f"input_rows: {summary['input_rows']}")
    print(f"unique_bounds_match_rows: {summary['objects_with_unique_bounds_match']}")
    print(f"multiple_bounds_match_rows: {summary['objects_with_multiple_bounds_matches']}")
    print(f"no_bounds_match_rows: {summary['objects_with_no_bounds_match']}")
    print(f"semantic_binding: {summary['semantic_binding']}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
