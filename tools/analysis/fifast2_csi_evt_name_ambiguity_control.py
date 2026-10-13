"""Measure exact token overlap between FIFAST2.csi names and EVT names.

This is an independent naming control only.  It does not infer that a CSI
name is an audio event, an ABKC/AST/BNK sample, a gameplay action, or a
playback route.  Exact token overlap is retained as ambiguous evidence when
more than one CSI name shares a token with an EVT candidate; zero overlap is
also retained instead of being treated as proof of absence.
"""

from __future__ import annotations

import argparse
import csv
import re
from pathlib import Path
from typing import Dict, Iterable, List, Sequence, Tuple


STATUS = "verified-independent-fifast2-csi-evt-name-ambiguity-control"
SEMANTIC_BINDING = "not-proven"
DIRECT_SELECTION_BINDING = "not-proven"
ORIGINAL_SEMANTICS = "not-claimed"
CSI_STATUS = "verified-structural-fifast2-csi-name-index-candidate"
EVT_STATUS = "verified-structural-named-parallel-range"
PROVENANCE = (
    "independent-TSV-reparse+deterministic-camelcase-tokenization+exact-token-"
    "overlap;ambiguous-name-control;no-event-sample-promotion"
)

REPORT_FIELDS = (
    "event_name",
    "event_tokens",
    "csi_name_match_count",
    "csi_name_matches",
    "zero_match",
    "single_match",
    "ambiguous_match",
    "event_input_status_valid",
    "csi_input_status_valid",
    "status",
    "semantic_binding",
    "direct_selection_binding",
    "original_semantics",
    "issue",
)

SUMMARY_FIELDS = (
    "csi_source_report",
    "evt_source_report",
    "csi_rows",
    "evt_rows",
    "csi_valid_rows",
    "evt_valid_rows",
    "evt_unique_names",
    "evt_rows_with_zero_csi_name_matches",
    "evt_rows_with_single_csi_name_match",
    "evt_rows_with_ambiguous_csi_name_matches",
    "evt_rows_with_any_csi_name_match",
    "exact_event_name_overlap_rows",
    "unique_name_binding_proven_rows",
    "status",
    "semantic_binding",
    "direct_selection_binding",
    "provenance",
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


def _tokens(value: str) -> Tuple[str, ...]:
    # Preserve acronym boundaries (GBShot -> gb, shot) before ordinary
    # camel-case boundaries.  EVT snake_case names are handled by the same
    # final alphanumeric-token extraction.
    value = re.sub(r"([A-Z]+)([A-Z][a-z])", r"\1 \2", value)
    value = re.sub(r"(?<=[a-z0-9])(?=[A-Z])", " ", value)
    return tuple(re.findall(r"[a-z0-9]+", value.lower()))


def analyze(
    csi_rows: Sequence[Dict[str, str]],
    csi_summary: Dict[str, str],
    evt_rows: Sequence[Dict[str, str]],
) -> Tuple[List[Dict[str, str]], Dict[str, str]]:
    if not csi_rows or not evt_rows:
        raise ValueError("CSI and EVT reports must contain rows")
    if any(not row.get("name") for row in csi_rows):
        raise ValueError("CSI name index contains an empty name")
    if any(not row.get("event_name") for row in evt_rows):
        raise ValueError("EVT range report contains an empty event name")
    if len({row["name"] for row in csi_rows}) != len(csi_rows):
        raise ValueError("CSI name index contains duplicate names")
    if len({row["event_name"] for row in evt_rows}) != len(evt_rows):
        raise ValueError("EVT range report contains duplicate event names")

    csi_status_valid = (
        csi_summary.get("status") == CSI_STATUS
        and csi_summary.get("original_semantics") == ORIGINAL_SEMANTICS
        and csi_summary.get("direct_selection_binding") == DIRECT_SELECTION_BINDING
        and csi_summary.get("candidate_record_count") == str(len(csi_rows))
        and csi_summary.get("valid_record_count") == str(len(csi_rows))
    )
    csi_tokens = {row["name"]: set(_tokens(row["name"])) for row in csi_rows}
    report: List[Dict[str, str]] = []
    zero_matches = 0
    single_matches = 0
    ambiguous_matches = 0
    any_matches = 0
    exact_overlap = 0
    valid_evt_rows = 0

    for event in sorted(evt_rows, key=lambda row: row["event_name"]):
        event_name = event["event_name"]
        event_tokens = set(_tokens(event_name))
        matches = sorted(
            name for name, name_tokens in csi_tokens.items()
            if event_tokens.intersection(name_tokens)
        )
        count = len(matches)
        zero = count == 0
        single = count == 1
        ambiguous = count > 1
        event_valid = (
            event.get("status") == EVT_STATUS
            and event.get("original_semantics") == ORIGINAL_SEMANTICS
            and event.get("direct_selection_binding") == DIRECT_SELECTION_BINDING
        )
        row_valid = csi_status_valid and event_valid and not (
            single and event_name in matches
        )
        zero_matches += int(zero)
        single_matches += int(single)
        ambiguous_matches += int(ambiguous)
        any_matches += int(count > 0)
        exact_overlap += int(event_name in matches)
        valid_evt_rows += int(event_valid)
        report.append({
            "event_name": event_name,
            "event_tokens": ",".join(sorted(event_tokens)),
            "csi_name_match_count": str(count),
            "csi_name_matches": ",".join(matches),
            "zero_match": str(zero),
            "single_match": str(single),
            "ambiguous_match": str(ambiguous),
            "event_input_status_valid": str(event_valid),
            "csi_input_status_valid": str(csi_status_valid),
            "status": STATUS if row_valid else f"inconclusive-{STATUS}",
            "semantic_binding": SEMANTIC_BINDING,
            "direct_selection_binding": DIRECT_SELECTION_BINDING,
            "original_semantics": ORIGINAL_SEMANTICS,
            "issue": "" if row_valid else "name inputs or negative uniqueness policy failed",
        })

    summary = {
        "csi_source_report": "",
        "evt_source_report": "",
        "csi_rows": str(len(csi_rows)),
        "evt_rows": str(len(evt_rows)),
        "csi_valid_rows": str(len(csi_rows) if csi_status_valid else 0),
        "evt_valid_rows": str(valid_evt_rows),
        "evt_unique_names": str(len({row["event_name"] for row in evt_rows})),
        "evt_rows_with_zero_csi_name_matches": str(zero_matches),
        "evt_rows_with_single_csi_name_match": str(single_matches),
        "evt_rows_with_ambiguous_csi_name_matches": str(ambiguous_matches),
        "evt_rows_with_any_csi_name_match": str(any_matches),
        "exact_event_name_overlap_rows": str(exact_overlap),
        # A name overlap is never sufficient to prove the event/sample
        # binding.  This must remain zero even for a single textual match.
        "unique_name_binding_proven_rows": "0",
        "status": STATUS if (
            len(csi_rows) == 100
            and len(evt_rows) == 17
            and csi_status_valid
            and valid_evt_rows == len(evt_rows)
            and zero_matches == 10
            and single_matches == 3
            and ambiguous_matches == 4
            and any_matches == 7
            and exact_overlap == 0
            and all(row["status"] == STATUS for row in report)
        ) else f"inconclusive-{STATUS}",
        "semantic_binding": SEMANTIC_BINDING,
        "direct_selection_binding": DIRECT_SELECTION_BINDING,
        "provenance": PROVENANCE,
        "original_semantics": ORIGINAL_SEMANTICS,
    }
    return report, summary


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("csi_report", type=Path)
    parser.add_argument("csi_summary", type=Path)
    parser.add_argument("evt_report", type=Path)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()
    report, summary = analyze(
        _read(args.csi_report),
        _key_value(_read(args.csi_summary)),
        _read(args.evt_report),
    )
    summary["csi_source_report"] = str(args.csi_report)
    summary["evt_source_report"] = str(args.evt_report)
    _write(args.report, REPORT_FIELDS, report)
    _write(args.summary, SUMMARY_FIELDS, [{field: summary[field] for field in SUMMARY_FIELDS}])
    print("FIFA Street 2 independent FIFAST2.csi-to-EVT name ambiguity control")
    for field in SUMMARY_FIELDS:
        print(f"{field}: {summary[field]}")
    print(f"report: {args.report}")
    print(f"summary: {args.summary}")
    return 0 if summary["status"] == STATUS else 1


if __name__ == "__main__":
    raise SystemExit(main())
