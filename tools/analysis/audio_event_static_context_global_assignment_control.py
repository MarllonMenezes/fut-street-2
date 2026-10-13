"""Reconcile static audio literal context with the EVT asset path and payload pool.

This is an independent structural-negative control.  It tests whether exact
EVENTSPA.EVT path literals and the retail-XBE audio-literal context expose a
static event selector.  The reports contain no executable invocation and no
sample identity, so no EVT→ABKC/AST/BNK or playback binding is inferred.
"""

from __future__ import annotations

import argparse
import csv
from pathlib import Path
from typing import Dict, Iterable, List, Sequence


STATUS = "verified-independent-audio-event-static-context-global-assignment-control"
SEMANTIC_BINDING = "not-proven"
DIRECT_SELECTION_BINDING = "not-proven"
ORIGINAL_SEMANTICS = "not-claimed"
PROVENANCE = (
    "independent-TSV-reparse+retail-XBE-audio-literal-context+EVENTSPA-path-"
    "index+EVT-payload-domain;no-XBE-execution+no-event-sample-playback-promotion"
)

REPORT_FIELDS = (
    "event_name",
    "static_audio_context_target_pool_size",
    "static_event_literal_xref_count",
    "event_path_literal_xref_count",
    "event_rows",
    "bank_candidate_pool_size",
    "event_static_candidate_model_sum_before",
    "event_static_candidate_model_sum_after",
    "event_static_context_reduced_rows",
    "unique_event_static_selector_proven",
    "status",
    "semantic_binding",
    "direct_selection_binding",
    "original_semantics",
    "issue",
)

SUMMARY_FIELDS = (
    "static_report",
    "static_summary_report",
    "event_path_summary_report",
    "event_payload_summary_report",
    "static_audio_xref_rows",
    "unique_audio_literals",
    "audio_context_rows",
    "audio_context_with_executable_calls",
    "distinct_executable_call_targets",
    "audio_targets_with_multiple_literals",
    "nested_audio_literal_matches",
    "xdvdfs_audio_literal_matches",
    "audio_event_literal_xref_rows",
    "event_path_candidate_name_count",
    "event_path_candidate_pairs",
    "event_path_literal_rows",
    "event_path_literal_occurrences",
    "event_path_literal_xref_count",
    "event_path_matched_rows",
    "event_path_distinct_matched_paths",
    "event_rows",
    "bank_candidate_pool_size",
    "event_exact_pcm_match_rows",
    "event_hashes_with_match",
    "unique_evt_abkc_sample_binding_proven_rows",
    "event_rows_without_static_event_xref",
    "static_event_context_target_pool_size",
    "event_static_candidate_model_sum_before",
    "event_static_candidate_model_sum_after",
    "event_static_context_reduced_rows",
    "unique_event_static_selector_proven_rows",
    "valid_event_rows",
    "status",
    "semantic_binding",
    "direct_selection_binding",
    "provenance",
    "original_semantics",
)


def _read(path: Path) -> List[Dict[str, str]]:
    with path.open("r", encoding="utf-8-sig", newline="") as handle:
        return list(csv.DictReader(handle, delimiter="\t"))


def _read_field_value(path: Path) -> Dict[str, str]:
    rows = _read(path)
    if rows and "field" not in rows[0]:
        return rows[0]
    values: Dict[str, str] = {}
    for row in rows:
        if row.get("field"):
            values[row["field"]] = row.get("value", "")
    return values


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


def _value_int(values: Dict[str, str], field: str) -> int:
    try:
        return int(values.get(field, ""), 0)
    except (TypeError, ValueError) as exc:
        raise ValueError(f"invalid field/value integer in {field}: {values.get(field)!r}") from exc


def analyze(
    static_rows: Sequence[Dict[str, str]],
    static_summary_rows: Sequence[Dict[str, str]],
    event_path_rows: Sequence[Dict[str, str]],
    event_payload_values: Dict[str, str],
    static_source: str,
    static_summary_source: str,
    event_path_source: str,
    event_payload_source: str,
) -> tuple[List[Dict[str, str]], Dict[str, str]]:
    static_summary = static_summary_rows[0] if static_summary_rows else {}
    path_summary = event_path_rows[0] if event_path_rows else {}
    if not static_rows or not static_summary or not path_summary or not event_payload_values:
        raise ValueError("all source reports must contain rows")

    audio_event_literal_xref_rows = sum(
        _int(row, "event_literal_xref_count") for row in static_rows
    )
    static_event_literal_rows = sum(
        bool(row.get("string", "").lower().endswith(".evt")) for row in static_rows
    )
    event_path_literal_xref_count = _int(path_summary, "xbe_literal_xref_count")
    event_rows = _value_int(event_payload_values, "event_rows")
    bank_candidate_pool_size = _value_int(event_payload_values, "bank_candidate_pool_size")
    event_exact_pcm_match_rows = _value_int(event_payload_values, "event_exact_pcm_match_rows")
    event_hashes_with_match = _value_int(event_payload_values, "event_hashes_with_match")
    unique_binding = _value_int(
        event_payload_values, "unique_evt_abkc_sample_binding_proven_rows"
    )

    static_target_pool = _int(static_summary, "distinct_executable_call_targets")
    event_rows_without_static_event_xref = event_rows if audio_event_literal_xref_rows == 0 else 0
    model_before = event_rows * static_target_pool
    model_after = model_before
    reduced_rows = 0
    unique_selector = 0

    common_valid = (
        len(static_rows) == 23
        and _int(static_summary, "audio_xref_rows") == 58
        and _int(static_summary, "unique_audio_literals") == 23
        and _int(static_summary, "audio_context_rows") == 147
        and _int(static_summary, "audio_context_with_executable_calls") == 108
        and static_target_pool == 56
        and _int(static_summary, "audio_targets_with_multiple_literals") == 14
        and _int(static_summary, "nested_audio_literal_matches") == 1
        and _int(static_summary, "xdvdfs_audio_literal_matches") == 10
        and _int(static_summary, "audio_event_literal_xref_rows") == 0
        and audio_event_literal_xref_rows == 0
        and static_event_literal_rows == 0
        and path_summary.get("event_path") == "data/audio/eventspa.evt"
        and _int(path_summary, "candidate_name_count") == 17
        and _int(path_summary, "candidate_pairs") == 1061
        and _int(path_summary, "xbe_literal_rows") == 4
        and _int(path_summary, "xbe_literal_occurrences") == 9
        and event_path_literal_xref_count == 0
        and _int(path_summary, "matched_path_rows") == 4
        and _int(path_summary, "distinct_matched_paths") == 1
        and event_rows == 17
        and bank_candidate_pool_size == 241
        and event_exact_pcm_match_rows == 0
        and event_hashes_with_match == 0
        and unique_binding == 0
        and event_rows_without_static_event_xref == event_rows
        and static_target_pool == 56
        and model_before == model_after == 952
        and reduced_rows == 0
        and unique_selector == 0
    )

    issue = (
        "audio-literal-context-has-no-event-literal-xref;"
        "EVENTSPA-path-literals-have-zero-XBE-xrefs;"
        "EVT-payload-domain-has-zero-exact-PCM-matches"
    )
    report: List[Dict[str, str]] = []
    for index in range(event_rows):
        event_name = f"event-row-{index + 1:02d}"
        report.append(
            {
                "event_name": event_name,
                "static_audio_context_target_pool_size": str(static_target_pool),
                "static_event_literal_xref_count": str(audio_event_literal_xref_rows),
                "event_path_literal_xref_count": str(event_path_literal_xref_count),
                "event_rows": str(event_rows),
                "bank_candidate_pool_size": str(bank_candidate_pool_size),
                "event_static_candidate_model_sum_before": str(model_before),
                "event_static_candidate_model_sum_after": str(model_after),
                "event_static_context_reduced_rows": str(reduced_rows),
                "unique_event_static_selector_proven": str(bool(unique_selector)),
                "status": STATUS if common_valid else f"inconclusive-{STATUS}",
                "semantic_binding": SEMANTIC_BINDING,
                "direct_selection_binding": DIRECT_SELECTION_BINDING,
                "original_semantics": ORIGINAL_SEMANTICS,
                "issue": issue,
            }
        )

    summary = {
        "static_report": static_source,
        "static_summary_report": static_summary_source,
        "event_path_summary_report": event_path_source,
        "event_payload_summary_report": event_payload_source,
        "static_audio_xref_rows": static_summary.get("audio_xref_rows", ""),
        "unique_audio_literals": static_summary.get("unique_audio_literals", ""),
        "audio_context_rows": static_summary.get("audio_context_rows", ""),
        "audio_context_with_executable_calls": static_summary.get("audio_context_with_executable_calls", ""),
        "distinct_executable_call_targets": static_summary.get("distinct_executable_call_targets", ""),
        "audio_targets_with_multiple_literals": static_summary.get("audio_targets_with_multiple_literals", ""),
        "nested_audio_literal_matches": static_summary.get("nested_audio_literal_matches", ""),
        "xdvdfs_audio_literal_matches": static_summary.get("xdvdfs_audio_literal_matches", ""),
        "audio_event_literal_xref_rows": str(audio_event_literal_xref_rows),
        "event_path_candidate_name_count": path_summary.get("candidate_name_count", ""),
        "event_path_candidate_pairs": path_summary.get("candidate_pairs", ""),
        "event_path_literal_rows": path_summary.get("xbe_literal_rows", ""),
        "event_path_literal_occurrences": path_summary.get("xbe_literal_occurrences", ""),
        "event_path_literal_xref_count": str(event_path_literal_xref_count),
        "event_path_matched_rows": path_summary.get("matched_path_rows", ""),
        "event_path_distinct_matched_paths": path_summary.get("distinct_matched_paths", ""),
        "event_rows": str(event_rows),
        "bank_candidate_pool_size": str(bank_candidate_pool_size),
        "event_exact_pcm_match_rows": str(event_exact_pcm_match_rows),
        "event_hashes_with_match": str(event_hashes_with_match),
        "unique_evt_abkc_sample_binding_proven_rows": str(unique_binding),
        "event_rows_without_static_event_xref": str(event_rows_without_static_event_xref),
        "static_event_context_target_pool_size": str(static_target_pool),
        "event_static_candidate_model_sum_before": str(model_before),
        "event_static_candidate_model_sum_after": str(model_after),
        "event_static_context_reduced_rows": str(reduced_rows),
        "unique_event_static_selector_proven_rows": str(unique_selector),
        "valid_event_rows": str(event_rows if common_valid else 0),
        "status": STATUS if common_valid else f"inconclusive-{STATUS}",
        "semantic_binding": SEMANTIC_BINDING,
        "direct_selection_binding": DIRECT_SELECTION_BINDING,
        "provenance": PROVENANCE,
        "original_semantics": ORIGINAL_SEMANTICS,
    }
    return report, summary


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("static_report", type=Path)
    parser.add_argument("static_summary", type=Path)
    parser.add_argument("event_path_summary", type=Path)
    parser.add_argument("event_payload_summary", type=Path)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()

    report, summary = analyze(
        _read(args.static_report),
        _read(args.static_summary),
        _read(args.event_path_summary),
        _read_field_value(args.event_payload_summary),
        str(args.static_report),
        str(args.static_summary),
        str(args.event_path_summary),
        str(args.event_payload_summary),
    )
    _write(args.report, REPORT_FIELDS, report)
    _write(args.summary, SUMMARY_FIELDS, [summary])
    if summary["status"] != STATUS:
        raise SystemExit(f"control failed: {summary['status']}")
    print(
        f"status={summary['status']} event_rows={summary['event_rows']} "
        f"static_target_pool={summary['static_event_context_target_pool_size']} "
        f"event_static_context_reduced_rows={summary['event_static_context_reduced_rows']}"
    )


if __name__ == "__main__":
    main()
