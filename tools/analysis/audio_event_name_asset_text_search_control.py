"""Independently reconcile the exhaustive EVT-name text search.

The control re-parses the generated TSV, checks the complete 60 x 17
asset/name matrix and preserves the negative result. Text absence does not
select ABKC/AST/BNK samples and does not establish original playback.
"""

from __future__ import annotations

import argparse
import csv
from collections import Counter
from pathlib import Path
from typing import Dict, Iterable, List, Sequence


STATUS = "verified-independent-audio-event-name-asset-text-search-control"
SEMANTIC_BINDING = "not-proven"
DIRECT_SELECTION_BINDING = "not-proven"
ORIGINAL_SEMANTICS = "not-claimed"
PROVENANCE = (
    "independent-TSV-reparse+complete-audio-asset/name-matrix+zero-name-hit-"
    "reconciliation;no-sample-or-playback-promotion"
)

REPORT_FIELDS = (
    "asset_path",
    "asset_bytes",
    "event_name",
    "raw_occurrences",
    "first_offsets",
    "valid",
    "source_status",
    "recomputed_name_identity",
    "recomputed_count_identity",
    "status",
    "semantic_binding",
    "direct_selection_binding",
    "original_semantics",
    "issue",
)

SUMMARY_FIELDS = (
    "source_report",
    "source_summary",
    "input_rows",
    "distinct_assets",
    "distinct_names",
    "valid_rows",
    "no_hit_rows",
    "raw_occurrence_rows",
    "total_raw_occurrences",
    "duplicate_asset_name_keys",
    "source_summary_matches_recomputed",
    "expected_assets",
    "expected_names",
    "status",
    "semantic_binding",
    "provenance",
    "direct_selection_binding",
    "original_semantics",
)


def _read(path: Path) -> List[Dict[str, str]]:
    with path.open("r", encoding="utf-8-sig", newline="") as handle:
        return list(csv.DictReader(handle, delimiter="\t"))


def _write(path: Path, fields: Sequence[str], rows: Iterable[Dict[str, str]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(
            handle,
            fieldnames=list(fields),
            delimiter="\t",
            lineterminator="\n",
        )
        writer.writeheader()
        writer.writerows({field: row.get(field, "") for field in fields} for row in rows)


def _int(row: Dict[str, str], field: str) -> int:
    try:
        return int(row.get(field, ""), 0)
    except (TypeError, ValueError) as exc:
        raise ValueError(f"invalid integer in {field}: {row.get(field)!r}") from exc


def _bool(row: Dict[str, str], field: str) -> bool:
    value = row.get(field, "")
    if value not in {"True", "False"}:
        raise ValueError(f"invalid boolean in {field}: {value!r}")
    return value == "True"


def _summary_values(path: Path) -> Dict[str, str]:
    values: Dict[str, str] = {}
    with path.open("r", encoding="utf-8-sig", newline="") as handle:
        reader = csv.reader(handle, delimiter="\t")
        for row in reader:
            if len(row) == 2 and row[0] != "field":
                values[row[0]] = row[1]
    if not values:
        raise ValueError(f"summary is empty: {path}")
    return values


def analyze(
    rows: Sequence[Dict[str, str]],
    source_report: str,
    source_summary: str,
    summary_values: Dict[str, str],
) -> tuple[List[Dict[str, str]], Dict[str, str]]:
    expected_assets = 60
    expected_names = 17
    required_fields = {
        "asset_path",
        "asset_bytes",
        "event_name",
        "raw_occurrences",
        "first_offsets",
        "valid",
        "status",
        "direct_selection_binding",
        "original_semantics",
    }
    if rows and not required_fields.issubset(rows[0]):
        missing = sorted(required_fields.difference(rows[0]))
        raise ValueError(f"source report missing fields: {missing}")

    keys = [(row.get("asset_path", ""), row.get("event_name", "")) for row in rows]
    key_counts = Counter(keys)
    duplicate_keys = sum(count > 1 for count in key_counts.values())
    distinct_assets = {row.get("asset_path", "") for row in rows}
    distinct_names = {row.get("event_name", "") for row in rows}
    output: List[Dict[str, str]] = []
    for row in rows:
        valid = _bool(row, "valid")
        asset_bytes = _int(row, "asset_bytes")
        raw_occurrences = _int(row, "raw_occurrences")
        name_identity = bool(row.get("asset_path", "")) and bool(row.get("event_name", ""))
        count_identity = raw_occurrences >= 0 and asset_bytes >= 0
        row_valid = valid and name_identity and count_identity
        output.append(
            {
                "asset_path": row.get("asset_path", ""),
                "asset_bytes": row.get("asset_bytes", ""),
                "event_name": row.get("event_name", ""),
                "raw_occurrences": row.get("raw_occurrences", ""),
                "first_offsets": row.get("first_offsets", ""),
                "valid": row.get("valid", ""),
                "source_status": row.get("status", ""),
                "recomputed_name_identity": str(name_identity),
                "recomputed_count_identity": str(count_identity),
                "status": STATUS if row_valid else f"inconclusive-{STATUS}",
                "semantic_binding": SEMANTIC_BINDING,
                "direct_selection_binding": DIRECT_SELECTION_BINDING,
                "original_semantics": ORIGINAL_SEMANTICS,
                "issue": "no-exact-name-hit-in-audio-asset-universe",
            }
        )

    valid_rows = sum(row["status"] == STATUS for row in output)
    no_hit_rows = sum(
        row["status"] == STATUS and _int(row, "raw_occurrences") == 0 for row in output
    )
    raw_occurrence_rows = sum(
        row["status"] == STATUS and _int(row, "raw_occurrences") > 0 for row in output
    )
    total_raw_occurrences = sum(
        _int(row, "raw_occurrences") for row in output if row["status"] == STATUS
    )
    source_matches = (
        summary_values.get("candidate_count") == str(expected_names)
        and summary_values.get("scanned_asset_count") == str(expected_assets)
        and summary_values.get("valid_asset_count") == str(expected_assets)
        and summary_values.get("asset_name_hit_pairs") == "0"
        and summary_values.get("assets_with_name_hits") == "0"
        and summary_values.get("raw_occurrences") == "0"
        and summary_values.get("status") == "verified-independent-audio-event-name-asset-text-search-control"
    )
    common_valid = (
        len(rows) == expected_assets * expected_names
        and len(distinct_assets) == expected_assets
        and all(distinct_assets)
        and len(distinct_names) == expected_names
        and all(distinct_names)
        and valid_rows == len(rows)
        and no_hit_rows == len(rows)
        and raw_occurrence_rows == 0
        and total_raw_occurrences == 0
        and duplicate_keys == 0
        and source_matches
    )
    summary = {
        "source_report": source_report,
        "source_summary": source_summary,
        "input_rows": str(len(rows)),
        "distinct_assets": str(len(distinct_assets)),
        "distinct_names": str(len(distinct_names)),
        "valid_rows": str(valid_rows),
        "no_hit_rows": str(no_hit_rows),
        "raw_occurrence_rows": str(raw_occurrence_rows),
        "total_raw_occurrences": str(total_raw_occurrences),
        "duplicate_asset_name_keys": str(duplicate_keys),
        "source_summary_matches_recomputed": str(source_matches),
        "expected_assets": str(expected_assets),
        "expected_names": str(expected_names),
        "status": STATUS if common_valid else f"inconclusive-{STATUS}",
        "semantic_binding": SEMANTIC_BINDING,
        "provenance": PROVENANCE,
        "direct_selection_binding": DIRECT_SELECTION_BINDING,
        "original_semantics": ORIGINAL_SEMANTICS,
    }
    return output, summary


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("source_report", type=Path)
    parser.add_argument("source_summary", type=Path)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()
    output, summary = analyze(
        _read(args.source_report),
        str(args.source_report),
        str(args.source_summary),
        _summary_values(args.source_summary),
    )
    _write(args.report, REPORT_FIELDS, output)
    _write(args.summary, SUMMARY_FIELDS, [summary])
    if summary["status"] != STATUS:
        raise SystemExit(f"control failed: {summary['status']}")
    print(
        f"status={summary['status']} input_rows={summary['input_rows']} "
        f"distinct_assets={summary['distinct_assets']} distinct_names={summary['distinct_names']}"
    )


if __name__ == "__main__":
    main()
