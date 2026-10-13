"""Independently reconcile named EVT rows with the parallel PCP ranges.

This control separates two observations that are easy to conflate: the
named EVT string anchors and the exact pcpahdr/pcpadat ranges selected by the
existing bounded structural audit.  It re-parses the TSV outputs, checks the
positive 16-bit/row-range result and the independent negative 32-bit raw-word
search, and preserves the result as EVT-to-PCP-range evidence only.  It does
not select an ABKC/AST sample or claim original playback semantics.
"""

from __future__ import annotations

import argparse
import csv
from pathlib import Path
from typing import Dict, Iterable, List, Sequence


STATUS = "verified-independent-audio-event-evt-pcp-range-discrimination-control"
DIRECT_SELECTION_BINDING = "not-proven"
ORIGINAL_SEMANTICS = "not-claimed"
PROVENANCE = (
    "independent-TSV-reparse+named-EVT-parallel-row-reconciliation+"
    "32-bit-raw-negative-control;EVT-to-PCP-range-only;no-sample-promotion"
)

REPORT_FIELDS = (
    "event_name",
    "event_string_offset",
    "header_row",
    "header_offset",
    "header_length",
    "data_offset",
    "data_length",
    "event_occurrence_count",
    "event_string_anchor_valid",
    "header_range_valid",
    "data_range_valid",
    "parallel_row_index_valid",
    "data_starts_with_schl",
    "unique_event_name",
    "unique_event_string_offset",
    "unique_header_row",
    "parallel_status_valid",
    "index_candidate_names_contains_event",
    "index_32bit_abk_zero",
    "index_32bit_pcpahdr_zero",
    "index_32bit_pcpadat_zero",
    "status",
    "direct_selection_binding",
    "original_semantics",
    "issue",
)

SUMMARY_FIELDS = (
    "parallel_source_report",
    "index_source_report",
    "runtime_join_source_report",
    "parallel_rows",
    "valid_named_range_rows",
    "event_string_anchor_rows",
    "header_range_rows",
    "data_range_rows",
    "parallel_row_index_rows",
    "data_schl_rows",
    "unique_event_name_rows",
    "unique_event_string_offset_rows",
    "unique_header_row_rows",
    "parallel_status_valid_rows",
    "index_candidate_count",
    "index_distinct_string_offsets",
    "index_distinct_following_words",
    "index_string_hash_family_candidates",
    "index_candidate_names_contains_event_rows",
    "index_32bit_abk_zero",
    "index_32bit_pcpahdr_zero",
    "index_32bit_pcpadat_zero",
    "runtime_joined_rows",
    "runtime_missing_names",
    "status",
    "provenance",
    "direct_selection_binding",
    "original_semantics",
)


def _read_rows(path: Path) -> List[Dict[str, str]]:
    with path.open("r", encoding="utf-8-sig", newline="") as handle:
        return list(csv.DictReader(handle, delimiter="\t"))


def _read_key_value(path: Path) -> Dict[str, str]:
    values: Dict[str, str] = {}
    with path.open("r", encoding="utf-8-sig", newline="") as handle:
        for row in csv.DictReader(handle, delimiter="\t"):
            if row.get("field"):
                values[row["field"]] = row.get("value", "")
    return values


def _int(row: Dict[str, str], key: str, default: int = -1) -> int:
    try:
        return int(row.get(key, ""), 0)
    except (TypeError, ValueError):
        return default


def _bool(row: Dict[str, str], key: str) -> bool:
    return row.get(key, "").strip().lower() == "true"


def _csv_bool(value: str) -> str:
    return "True" if value else "False"


def _write(path: Path, fields: Sequence[str], rows: Iterable[Dict[str, str]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(
            handle,
            fieldnames=list(fields),
            delimiter="\t",
            lineterminator="\n",
            extrasaction="ignore",
        )
        writer.writeheader()
        writer.writerows(rows)


def _source_status_valid(row: Dict[str, str]) -> bool:
    return (
        row.get("status") == "verified-structural-named-parallel-range"
        and row.get("direct_selection_binding") == DIRECT_SELECTION_BINDING
        and row.get("original_semantics") == ORIGINAL_SEMANTICS
    )


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("parallel_report", type=Path)
    parser.add_argument("index_summary", type=Path)
    parser.add_argument("runtime_join_summary", type=Path)
    parser.add_argument("--report", required=True, type=Path)
    parser.add_argument("--summary", required=True, type=Path)
    args = parser.parse_args()

    parallel_rows = _read_rows(args.parallel_report)
    index = _read_key_value(args.index_summary)
    runtime_rows = _read_rows(args.runtime_join_summary)
    runtime = runtime_rows[0] if runtime_rows else {}

    event_names = {row.get("event_name", "") for row in parallel_rows}
    index_names = {
        value.strip()
        for value in index.get("candidate_names", "").split(",")
        if value.strip()
    }
    index_candidate_count = _int(index, "string_hash_family_candidates", -1)
    index_distinct_offsets = _int(index, "distinct_string_offsets", -1)
    index_distinct_following = _int(index, "distinct_following_words", -1)
    index_abk_zero = _int(index, "abk_aligned_occurrences", -1) == 0
    index_pcpahdr_zero = _int(index, "pcpahdr_big_aligned_occurrences", -1) == 0
    index_pcpadat_zero = _int(index, "pcpadat_big_aligned_occurrences", -1) == 0

    report: List[Dict[str, str]] = []
    counters = {
        "valid_named_range_rows": 0,
        "event_string_anchor_rows": 0,
        "header_range_rows": 0,
        "data_range_rows": 0,
        "parallel_row_index_rows": 0,
        "data_schl_rows": 0,
        "unique_event_name_rows": 0,
        "unique_event_string_offset_rows": 0,
        "unique_header_row_rows": 0,
        "parallel_status_valid_rows": 0,
        "index_candidate_names_contains_event": 0,
    }

    for row in parallel_rows:
        source_status_valid = _source_status_valid(row)
        event_name = row.get("event_name", "")
        in_index = event_name in index_names
        flags = {
            "event_string_anchor_valid": _bool(row, "event_string_anchor_valid"),
            "header_range_valid": _bool(row, "header_range_valid"),
            "data_range_valid": _bool(row, "data_range_valid"),
            "parallel_row_index_valid": _bool(row, "parallel_row_index_valid"),
            "data_starts_with_schl": _bool(row, "data_starts_with_schl"),
            "unique_event_name": _bool(row, "unique_event_name"),
            "unique_event_string_offset": _bool(row, "unique_event_string_offset"),
            "unique_header_row": _bool(row, "unique_header_row"),
        }
        counter_keys = {
            "event_string_anchor_valid": "event_string_anchor_rows",
            "header_range_valid": "header_range_rows",
            "data_range_valid": "data_range_rows",
            "parallel_row_index_valid": "parallel_row_index_rows",
            "data_starts_with_schl": "data_schl_rows",
            "unique_event_name": "unique_event_name_rows",
            "unique_event_string_offset": "unique_event_string_offset_rows",
            "unique_header_row": "unique_header_row_rows",
        }
        for key, value in flags.items():
            if value:
                counter_key = counter_keys[key]
                counters[counter_key] += 1
        if source_status_valid:
            counters["parallel_status_valid_rows"] += 1
        if in_index:
            counters["index_candidate_names_contains_event"] += 1

        valid = (
            source_status_valid
            and all(flags.values())
            and _int(row, "event_occurrence_count") > 0
            and _int(row, "header_offset") >= 0
            and _int(row, "header_length") > 0
            and _int(row, "data_offset") >= 0
            and _int(row, "data_length") > 0
            and in_index
        )
        if valid:
            counters["valid_named_range_rows"] += 1
        issue = "" if valid else "named EVT/PCP range reconciliation failed"
        report.append(
            {
                "event_name": event_name,
                "event_string_offset": row.get("event_string_offset", ""),
                "header_row": row.get("header_row", ""),
                "header_offset": row.get("header_offset", ""),
                "header_length": row.get("header_length", ""),
                "data_offset": row.get("data_offset", ""),
                "data_length": row.get("data_length", ""),
                "event_occurrence_count": row.get("event_occurrence_count", ""),
                **{key: _csv_bool(value) for key, value in flags.items()},
                "parallel_status_valid": _csv_bool(source_status_valid),
                "index_candidate_names_contains_event": _csv_bool(in_index),
                "index_32bit_abk_zero": _csv_bool(index_abk_zero),
                "index_32bit_pcpahdr_zero": _csv_bool(index_pcpahdr_zero),
                "index_32bit_pcpadat_zero": _csv_bool(index_pcpadat_zero),
                "status": STATUS if valid else "inconclusive",
                "direct_selection_binding": DIRECT_SELECTION_BINDING,
                "original_semantics": ORIGINAL_SEMANTICS,
                "issue": issue,
            }
        )

    runtime_joined = _int(runtime, "joined_rows", 0)
    runtime_missing = _int(runtime, "missing_runtime_names", 0) + _int(
        runtime, "missing_parallel_names", 0
    )
    index_valid = (
        index.get("status") == "no-direct-selection-binding"
        and index.get("direct_selection_binding") == DIRECT_SELECTION_BINDING
        and index.get("original_semantics") == ORIGINAL_SEMANTICS
        and index_candidate_count == len(parallel_rows)
        and index_distinct_offsets == len(parallel_rows)
        and index_distinct_following == len(parallel_rows)
        and index_abk_zero
        and index_pcpahdr_zero
        and index_pcpadat_zero
    )
    runtime_valid = (
        runtime.get("status") == "verified-independent-audio-event-parallel-runtime-join-control"
        and runtime_joined == len(parallel_rows)
        and runtime_missing == 0
    )
    valid = (
        len(parallel_rows) == 17
        and counters["valid_named_range_rows"] == len(parallel_rows)
        and counters["index_candidate_names_contains_event"] == len(parallel_rows)
        and index_valid
        and runtime_valid
    )
    summary = {
        "parallel_source_report": args.parallel_report.name,
        "index_source_report": args.index_summary.name,
        "runtime_join_source_report": args.runtime_join_summary.name,
        "parallel_rows": str(len(parallel_rows)),
        **{key: str(value) for key, value in counters.items() if key != "index_candidate_names_contains_event"},
        "index_candidate_count": str(index_candidate_count),
        "index_distinct_string_offsets": str(index_distinct_offsets),
        "index_distinct_following_words": str(index_distinct_following),
        "index_string_hash_family_candidates": str(index_candidate_count),
        "index_candidate_names_contains_event_rows": str(
            counters["index_candidate_names_contains_event"]
        ),
        "index_32bit_abk_zero": _csv_bool(index_abk_zero),
        "index_32bit_pcpahdr_zero": _csv_bool(index_pcpahdr_zero),
        "index_32bit_pcpadat_zero": _csv_bool(index_pcpadat_zero),
        "runtime_joined_rows": str(runtime_joined),
        "runtime_missing_names": str(runtime_missing),
        "status": STATUS if valid else "inconclusive",
        "provenance": PROVENANCE,
        "direct_selection_binding": DIRECT_SELECTION_BINDING,
        "original_semantics": ORIGINAL_SEMANTICS,
    }
    _write(args.report, REPORT_FIELDS, report)
    _write(args.summary, SUMMARY_FIELDS, [summary])

    print(f"status: {summary['status']}")
    print(f"parallel_rows: {len(parallel_rows)}")
    print(f"valid_named_range_rows: {counters['valid_named_range_rows']}")
    print(f"index_candidate_names_contains_event_rows: {counters['index_candidate_names_contains_event']}")
    print(f"index_32bit_raw_negative_control: {index_valid}")
    print(f"runtime_joined_rows: {runtime_joined}")
    return 0 if valid else 1


if __name__ == "__main__":
    raise SystemExit(main())
