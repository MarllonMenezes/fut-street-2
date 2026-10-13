"""Independently reparse the bounded raw EVT/ABKC/AST identity audit.

The control validates row cardinalities, SHA-256 formatting, SCHl flags and
the preserved zero full-payload-match negative result. It does not turn raw
byte equality into event/sample/playback semantics.
"""

from __future__ import annotations

import argparse
import csv
import re
from pathlib import Path
from typing import Dict, List, Sequence, Tuple


STATUS = "verified-independent-audio-event-abk-raw-identity-control"
DIRECT_SELECTION_BINDING = "not-proven"
ORIGINAL_SEMANTICS = "not-claimed"
PROVENANCE = (
    "independent-TSV-reparse+event/AST-row-cardinality+SHA256-format+SCHl-"
    "flag+zero-full-raw-match-negative-control;no-selection-promotion"
)
SHA256_RE = re.compile(r"^[0-9A-Fa-f]{64}$")


def read_tsv(path: Path) -> List[Dict[str, str]]:
    with path.open("r", encoding="utf-8-sig", newline="") as handle:
        return list(csv.DictReader(handle, delimiter="\t"))


def read_key_value(path: Path) -> Dict[str, str]:
    rows = read_tsv(path)
    return {row.get("field", ""): row.get("value", "") for row in rows}


def as_int(values: Dict[str, str], field: str) -> int:
    try:
        return int(values.get(field, ""), 0)
    except (TypeError, ValueError) as exc:
        raise ValueError(f"invalid integer in {field}: {values.get(field)!r}") from exc


def analyze(rows: List[Dict[str, str]], summary: Dict[str, str]) -> Tuple[List[Dict[str, str]], Dict[str, str]]:
    failures: List[str] = []
    event_rows = [row for row in rows if row.get("kind") == "event"]
    ast_rows = [row for row in rows if row.get("kind") == "ast-range"]
    valid_rows = [row for row in rows if row.get("raw_sha256")]
    event_valid = [row for row in event_rows if row.get("raw_sha256")]
    ast_valid = [row for row in ast_rows if row.get("raw_sha256")]

    expected_events = as_int(summary, "event_candidates")
    expected_ast_ranges = as_int(summary, "bank_ast_range_rows")
    if summary.get("status") != "verified-bounded-audio-event-abk-raw-identity-negative-control":
        failures.append(f"unexpected main status: {summary.get('status', '')!r}")
    if summary.get("direct_selection_binding") != DIRECT_SELECTION_BINDING:
        failures.append("main direct_selection_binding is not not-proven")
    if summary.get("original_semantics") != ORIGINAL_SEMANTICS:
        failures.append("main original_semantics is not not-claimed")
    if len(event_rows) != expected_events:
        failures.append(f"event rows {len(event_rows)} != {expected_events}")
    if len(event_valid) != as_int(summary, "event_raw_rows"):
        failures.append("event_raw_rows disagrees with report")
    if len(ast_rows) != expected_ast_ranges:
        failures.append(f"AST rows {len(ast_rows)} != {expected_ast_ranges}")
    if len(ast_valid) != expected_ast_ranges:
        failures.append("not every AST range has a raw hash")
    if as_int(summary, "rejected_rows") != 0:
        failures.append("main audit rejected rows")
    if as_int(summary, "full_raw_hash_pairs") != 0:
        failures.append("full raw payload match is not the expected negative result")
    if as_int(summary, "event_hashes_with_full_raw_match") != 0:
        failures.append("event full raw hash match count is not zero")
    if as_int(summary, "bank_ast_schl_rows") != len([row for row in ast_valid if row.get("schl_signature") == "True"]):
        failures.append("bank_ast_schl_rows disagrees with SCHl flags")
    if any(not SHA256_RE.fullmatch(row.get("raw_sha256", "")) for row in valid_rows):
        failures.append("a raw_sha256 value is not a 64-character SHA-256")
    if any(not SHA256_RE.fullmatch(row.get("raw_prefix_sha256", "")) for row in valid_rows):
        failures.append("a raw_prefix_sha256 value is not a 64-character SHA-256")
    if any(row.get("status") == "rejected" for row in rows):
        failures.append("report contains rejected rows")
    if len({row.get("raw_sha256") for row in event_valid}) != as_int(summary, "event_distinct_raw_hashes"):
        failures.append("event_distinct_raw_hashes disagrees with report")
    recomputed_pairs = sum(int(row.get("full_hash_match_count", "0"), 0) for row in event_rows)
    if recomputed_pairs != as_int(summary, "full_raw_hash_pairs"):
        failures.append("full_raw_hash_pairs disagrees with event rows")
    recomputed_event_matches = sum(
        int(row.get("full_hash_match_count", "0"), 0) > 0 for row in event_rows
    )
    if recomputed_event_matches != as_int(summary, "event_hashes_with_full_raw_match"):
        failures.append("event_hashes_with_full_raw_match disagrees with event rows")

    valid = not failures
    report = [{
        "source": "raw-identity-report",
        "status": STATUS if valid else f"inconclusive-{STATUS}",
        "valid": str(valid),
        "observed": (
            f"event_rows={len(event_rows)};event_valid={len(event_valid)};"
            f"ast_rows={len(ast_rows)};ast_valid={len(ast_valid)};"
            f"full_raw_hash_pairs={summary.get('full_raw_hash_pairs', '')};"
            f"rejected_rows={summary.get('rejected_rows', '')}"
        ),
        "expected": (
            f"event_rows={expected_events};event_valid={expected_events};"
            f"ast_rows={expected_ast_ranges};ast_valid={expected_ast_ranges};"
            "full_raw_hash_pairs=0;rejected_rows=0"
        ),
        "evidence": (
            "independent row/key/hash/SCHl reparse of named EVT ranges and the "
            "bounded ABKC/AST range population"
        ),
        "issue": "; ".join(failures),
        "direct_selection_binding": DIRECT_SELECTION_BINDING,
        "original_semantics": ORIGINAL_SEMANTICS,
    }]
    control_summary = {
        "input_rows": str(len(rows)),
        "event_rows": str(len(event_rows)),
        "event_valid_rows": str(len(event_valid)),
        "ast_rows": str(len(ast_rows)),
        "ast_valid_rows": str(len(ast_valid)),
        "event_sha256_rows": str(sum(bool(SHA256_RE.fullmatch(row.get("raw_sha256", ""))) for row in event_valid)),
        "ast_sha256_rows": str(sum(bool(SHA256_RE.fullmatch(row.get("raw_sha256", ""))) for row in ast_valid)),
        "event_distinct_raw_hashes": str(len({row.get("raw_sha256") for row in event_valid})),
        "full_raw_hash_pairs": summary.get("full_raw_hash_pairs", ""),
        "event_hashes_with_full_raw_match": summary.get("event_hashes_with_full_raw_match", ""),
        "rejected_rows": summary.get("rejected_rows", ""),
        "status": STATUS if valid else f"inconclusive-{STATUS}",
        "provenance": PROVENANCE,
        "direct_selection_binding": DIRECT_SELECTION_BINDING,
        "original_semantics": ORIGINAL_SEMANTICS,
    }
    return report, control_summary


def write_tsv(path: Path, fields: Sequence[str], rows: Sequence[Dict[str, str]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=list(fields), delimiter="\t", lineterminator="\n")
        writer.writeheader()
        writer.writerows({field: row.get(field, "") for field in fields} for row in rows)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("report", type=Path)
    parser.add_argument("summary", type=Path)
    parser.add_argument("--report", dest="output_report", type=Path, required=True)
    parser.add_argument("--summary", dest="output_summary", type=Path, required=True)
    args = parser.parse_args()
    report, control_summary = analyze(read_tsv(args.report), read_key_value(args.summary))
    write_tsv(
        args.output_report,
        ("source", "status", "valid", "observed", "expected", "evidence", "issue", "direct_selection_binding", "original_semantics"),
        report,
    )
    write_tsv(
        args.output_summary,
        ("field", "value"),
        [{"field": field, "value": value} for field, value in control_summary.items()],
    )
    for field, value in control_summary.items():
        print(f"{field}: {value}")
    print(f"report: {args.output_report}")
    print(f"summary: {args.output_summary}")
    return 0 if control_summary["status"] == STATUS else 1


if __name__ == "__main__":
    raise SystemExit(main())
