"""Enumerate original EVT-to-sample candidate domains without promotion.

This control joins the complete named EVT payload report, the independent
FIFAST2.csi name-overlap report and the parallel runtime join.  It records the
exact PCM identity domain, the weak textual CSI domain and the decoded bank
pool separately.  The bank pool is deliberately not multiplied into an
EVT-to-sample assignment: no source report supplies that relationship.  The
control therefore documents every available structural hypothesis while
keeping direct selection and original playback semantics unproven.
"""

from __future__ import annotations

import argparse
import csv
from pathlib import Path
from typing import Dict, Iterable, List, Sequence, Tuple


STATUS = "verified-independent-original-audio-event-sample-global-assignment-control"
SEMANTIC_BINDING = "not-proven"
DIRECT_SELECTION_BINDING = "not-proven"
ORIGINAL_SEMANTICS = "not-claimed"
PAYLOAD_EVENT_STATUS = "decoded-payload-no-match"
CSI_STATUS = "verified-independent-fifast2-csi-evt-name-ambiguity-control"
PARALLEL_STATUS = "verified-independent-audio-event-parallel-runtime-join-control"
NEGATIVE_STATUS = "verified-independent-audio-selection-negative-synthesis"
AUTHORED_STATUS = "verified-independent-authored-audio-candidate-route-control"
PROVENANCE = (
    "independent-keyed-reparse+exact-PCM-domain+weak-CSI-name-domain+decoded-"
    "bank-pool-enumeration+parallel-runtime-join;no-EVT-to-ABKC-promotion"
)

REPORT_FIELDS = (
    "event_name",
    "event_payload_status_valid",
    "event_pcm_exact_match_count",
    "event_exact_sample_identity_domain",
    "csi_input_status_valid",
    "csi_name_match_count",
    "weak_csi_name_domain",
    "parallel_input_status_valid",
    "decoded_bank_candidate_pool",
    "unconstrained_bank_pool",
    "exact_identity_model_count",
    "weak_name_only_model_count",
    "structural_cross_product_model_count",
    "unique_evt_abkc_sample_binding_proven",
    "status",
    "semantic_binding",
    "direct_selection_binding",
    "original_semantics",
    "issue",
)

SUMMARY_FIELDS = (
    "payload_source_report",
    "csi_source_report",
    "parallel_source_report",
    "negative_synthesis_source_report",
    "authored_route_source_report",
    "event_rows",
    "bank_rows",
    "decoded_event_rows",
    "decoded_bank_rows",
    "event_input_status_valid_rows",
    "csi_input_status_valid_rows",
    "parallel_input_status_valid_rows",
    "negative_synthesis_valid_source_count",
    "authored_route_rows",
    "bank_candidate_pool_size",
    "event_exact_pcm_match_rows",
    "event_hashes_with_match",
    "evt_rows_with_zero_exact_sample_models",
    "evt_rows_with_single_weak_name_models",
    "evt_rows_with_multiple_weak_name_models",
    "evt_rows_with_zero_weak_name_models",
    "weak_name_domain_sum",
    "exact_identity_model_sum",
    "structural_cross_product_model_sum",
    "unique_evt_abkc_sample_binding_proven_rows",
    "valid_event_rows",
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


def _key_value(rows: Sequence[Dict[str, str]]) -> Dict[str, str]:
    if not rows:
        return {}
    if "field" not in rows[0] or "value" not in rows[0]:
        raise ValueError("expected field/value summary TSV")
    return {row.get("field", ""): row.get("value", "") for row in rows}


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


def analyze(
    payload_rows: Sequence[Dict[str, str]],
    csi_rows: Sequence[Dict[str, str]],
    parallel_rows: Sequence[Dict[str, str]],
    negative_summary: Dict[str, str],
    authored_summary: Dict[str, str],
) -> Tuple[List[Dict[str, str]], Dict[str, str]]:
    events = [row for row in payload_rows if row.get("kind") == "event"]
    banks = [row for row in payload_rows if row.get("kind") == "bank"]
    if not events or not banks or not csi_rows or not parallel_rows:
        raise ValueError("payload, CSI and parallel reports must contain rows")

    payload_by_name = {row.get("name", ""): row for row in events}
    csi_by_name = {row.get("event_name", ""): row for row in csi_rows}
    parallel_by_name = {row.get("event_name", ""): row for row in parallel_rows}
    if len(payload_by_name) != len(events):
        raise ValueError("payload report contains duplicate event names")
    if len(csi_by_name) != len(csi_rows):
        raise ValueError("CSI ambiguity report contains duplicate event names")
    if len(parallel_by_name) != len(parallel_rows):
        raise ValueError("parallel join report contains duplicate event names")

    decoded_events = [row for row in events if _bool(row, "decode_validated")]
    decoded_banks = [row for row in banks if _bool(row, "decode_validated")]
    bank_candidate_pool = len(decoded_banks)
    negative_valid = (
        negative_summary.get("status") == NEGATIVE_STATUS
        and negative_summary.get("direct_selection_binding") == DIRECT_SELECTION_BINDING
        and negative_summary.get("original_semantics") == ORIGINAL_SEMANTICS
    )
    authored_valid = (
        authored_summary.get("status") == AUTHORED_STATUS
        and authored_summary.get("route_rows") == "15"
        and authored_summary.get("missing_runtime_candidates") == "0"
        and authored_summary.get("coverage_missing_policy_actions") == "0"
        and authored_summary.get("original_semantics") == ORIGINAL_SEMANTICS
    )

    report: List[Dict[str, str]] = []
    counters = {
        "event_input_status_valid_rows": 0,
        "csi_input_status_valid_rows": 0,
        "parallel_input_status_valid_rows": 0,
        "event_exact_pcm_match_rows": 0,
        "event_hashes_with_match": 0,
        "evt_rows_with_zero_exact_sample_models": 0,
        "evt_rows_with_single_weak_name_models": 0,
        "evt_rows_with_multiple_weak_name_models": 0,
        "evt_rows_with_zero_weak_name_models": 0,
        "weak_name_domain_sum": 0,
        "exact_identity_model_sum": 0,
        "structural_cross_product_model_sum": 0,
        "unique_evt_abkc_sample_binding_proven_rows": 0,
        "valid_event_rows": 0,
    }

    for event_name in sorted(payload_by_name):
        event = payload_by_name[event_name]
        csi = csi_by_name.get(event_name, {})
        parallel = parallel_by_name.get(event_name, {})
        event_valid = (
            event.get("status") == PAYLOAD_EVENT_STATUS
            and _bool(event, "decode_validated")
            and _int(event, "candidate_count") == 1
            and _int(event, "exact_pcm_match_count") == 0
        )
        csi_valid = (
            csi.get("status") == CSI_STATUS
            and csi.get("direct_selection_binding") == DIRECT_SELECTION_BINDING
            and csi.get("original_semantics") == ORIGINAL_SEMANTICS
        )
        parallel_valid = (
            parallel.get("status") == PARALLEL_STATUS
            and parallel.get("original_semantics") == ORIGINAL_SEMANTICS
            and parallel.get("header_row_match") == "True"
            and parallel.get("data_offset_match") == "True"
            and parallel.get("data_length_match") == "True"
            and parallel.get("runtime_prepared") == "True"
            and parallel.get("wave_header_valid") == "True"
        )
        exact_matches = _int(event, "exact_pcm_match_count")
        weak_name_models = _int(csi, "csi_name_match_count") if csi else 0
        exact_identity_models = exact_matches
        # A weak CSI name and an exact PCM identity are independent domains;
        # their product is intentionally zero for this source corpus.  The
        # 241 decoded bank rows remain an unassigned pool, not an event map.
        structural_product = exact_identity_models * weak_name_models
        row_valid = event_valid and csi_valid and parallel_valid and negative_valid and authored_valid

        counters["event_input_status_valid_rows"] += int(event_valid)
        counters["csi_input_status_valid_rows"] += int(csi_valid)
        counters["parallel_input_status_valid_rows"] += int(parallel_valid)
        counters["event_exact_pcm_match_rows"] += int(exact_matches > 0)
        counters["event_hashes_with_match"] += int(exact_matches > 0)
        counters["evt_rows_with_zero_exact_sample_models"] += int(exact_identity_models == 0)
        counters["evt_rows_with_single_weak_name_models"] += int(weak_name_models == 1)
        counters["evt_rows_with_multiple_weak_name_models"] += int(weak_name_models > 1)
        counters["evt_rows_with_zero_weak_name_models"] += int(weak_name_models == 0)
        counters["weak_name_domain_sum"] += weak_name_models
        counters["exact_identity_model_sum"] += exact_identity_models
        counters["structural_cross_product_model_sum"] += structural_product
        counters["unique_evt_abkc_sample_binding_proven_rows"] += int(structural_product == 1)
        counters["valid_event_rows"] += int(row_valid)

        issue_parts = []
        if not event_valid:
            issue_parts.append("event payload identity/decode control failed")
        if not csi_valid:
            issue_parts.append("CSI weak-name control failed")
        if not parallel_valid:
            issue_parts.append("parallel runtime join failed")
        if not negative_valid:
            issue_parts.append("negative synthesis input failed")
        if not authored_valid:
            issue_parts.append("authored route input failed")
        report.append({
            "event_name": event_name,
            "event_payload_status_valid": str(event_valid),
            "event_pcm_exact_match_count": str(exact_matches),
            "event_exact_sample_identity_domain": str(exact_identity_models),
            "csi_input_status_valid": str(csi_valid),
            "csi_name_match_count": str(weak_name_models),
            "weak_csi_name_domain": str(weak_name_models),
            "parallel_input_status_valid": str(parallel_valid),
            "decoded_bank_candidate_pool": str(bank_candidate_pool),
            "unconstrained_bank_pool": "True",
            "exact_identity_model_count": str(exact_identity_models),
            "weak_name_only_model_count": str(weak_name_models),
            "structural_cross_product_model_count": str(structural_product),
            "unique_evt_abkc_sample_binding_proven": "False",
            "status": STATUS if row_valid else f"inconclusive-{STATUS}",
            "semantic_binding": SEMANTIC_BINDING,
            "direct_selection_binding": DIRECT_SELECTION_BINDING,
            "original_semantics": ORIGINAL_SEMANTICS,
            "issue": "; ".join(issue_parts),
        })

    all_valid = (
        len(events) == 17
        and len(banks) == 242
        and len(decoded_events) == 17
        and len(decoded_banks) == 241
        and counters["event_input_status_valid_rows"] == 17
        and counters["csi_input_status_valid_rows"] == 17
        and counters["parallel_input_status_valid_rows"] == 17
        and negative_summary.get("valid_source_count") == "5"
        and authored_summary.get("route_rows") == "15"
        and counters["event_exact_pcm_match_rows"] == 0
        and counters["event_hashes_with_match"] == 0
        and counters["evt_rows_with_zero_exact_sample_models"] == 17
        and counters["evt_rows_with_single_weak_name_models"] == 3
        and counters["evt_rows_with_multiple_weak_name_models"] == 4
        and counters["evt_rows_with_zero_weak_name_models"] == 10
        and counters["unique_evt_abkc_sample_binding_proven_rows"] == 0
        and counters["valid_event_rows"] == 17
        and all(row["status"] == STATUS for row in report)
    )
    summary = {
        "payload_source_report": "",
        "csi_source_report": "",
        "parallel_source_report": "",
        "negative_synthesis_source_report": "",
        "authored_route_source_report": "",
        "event_rows": str(len(events)),
        "bank_rows": str(len(banks)),
        "decoded_event_rows": str(len(decoded_events)),
        "decoded_bank_rows": str(len(decoded_banks)),
        "event_input_status_valid_rows": str(counters["event_input_status_valid_rows"]),
        "csi_input_status_valid_rows": str(counters["csi_input_status_valid_rows"]),
        "parallel_input_status_valid_rows": str(counters["parallel_input_status_valid_rows"]),
        "negative_synthesis_valid_source_count": negative_summary.get("valid_source_count", ""),
        "authored_route_rows": authored_summary.get("route_rows", ""),
        "bank_candidate_pool_size": str(bank_candidate_pool),
        "event_exact_pcm_match_rows": str(counters["event_exact_pcm_match_rows"]),
        "event_hashes_with_match": str(counters["event_hashes_with_match"]),
        "evt_rows_with_zero_exact_sample_models": str(counters["evt_rows_with_zero_exact_sample_models"]),
        "evt_rows_with_single_weak_name_models": str(counters["evt_rows_with_single_weak_name_models"]),
        "evt_rows_with_multiple_weak_name_models": str(counters["evt_rows_with_multiple_weak_name_models"]),
        "evt_rows_with_zero_weak_name_models": str(counters["evt_rows_with_zero_weak_name_models"]),
        "weak_name_domain_sum": str(counters["weak_name_domain_sum"]),
        "exact_identity_model_sum": str(counters["exact_identity_model_sum"]),
        "structural_cross_product_model_sum": str(counters["structural_cross_product_model_sum"]),
        "unique_evt_abkc_sample_binding_proven_rows": str(counters["unique_evt_abkc_sample_binding_proven_rows"]),
        "valid_event_rows": str(counters["valid_event_rows"]),
        "status": STATUS if all_valid else f"inconclusive-{STATUS}",
        "semantic_binding": SEMANTIC_BINDING,
        "provenance": PROVENANCE,
        "direct_selection_binding": DIRECT_SELECTION_BINDING,
        "original_semantics": ORIGINAL_SEMANTICS,
    }
    return report, summary


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("payload_report", type=Path)
    parser.add_argument("csi_report", type=Path)
    parser.add_argument("parallel_report", type=Path)
    parser.add_argument("negative_summary", type=Path)
    parser.add_argument("authored_summary", type=Path)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()
    report, summary = analyze(
        _read(args.payload_report),
        _read(args.csi_report),
        _read(args.parallel_report),
        _key_value(_read(args.negative_summary)),
        _read(args.authored_summary)[0] if _read(args.authored_summary) else {},
    )
    summary["payload_source_report"] = str(args.payload_report)
    summary["csi_source_report"] = str(args.csi_report)
    summary["parallel_source_report"] = str(args.parallel_report)
    summary["negative_synthesis_source_report"] = str(args.negative_summary)
    summary["authored_route_source_report"] = str(args.authored_summary)
    _write(args.report, REPORT_FIELDS, report)
    _write(args.summary, SUMMARY_FIELDS, [{field: summary[field] for field in SUMMARY_FIELDS}])
    print("FIFA Street 2 independent original audio EVT-to-sample global assignment control")
    for field in SUMMARY_FIELDS:
        print(f"{field}: {summary[field]}")
    print(f"report: {args.report}")
    print(f"summary: {args.summary}")
    return 0 if summary["status"] == STATUS else 1


if __name__ == "__main__":
    raise SystemExit(main())
