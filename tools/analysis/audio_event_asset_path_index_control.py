"""Cross-check the XBE event-asset path literals against the EVT index report.

This is a conservative provenance control.  It joins only the textual
EVENTSPA.EVT literals found in the retail XBE asset-path report with the
independently produced event string-index evidence.  It does not execute the
XBE, recover a loader, select an ABKC/AST/BNK sample, or assign gameplay or
playback semantics.  The source ISO is never opened for writing.
"""

from __future__ import annotations

import argparse
import csv
from pathlib import Path


STATUS = "verified-independent-audio-event-asset-path-index-control"
DIRECT_SELECTION_BINDING = "not-proven"
ORIGINAL_SEMANTICS = "not-claimed"

REPORT_FIELDS = (
    "event_path",
    "candidate_name_count",
    "candidate_pairs",
    "xbe_literal_rows",
    "xbe_literal_occurrences",
    "xbe_literal_xref_count",
    "matched_path_rows",
    "distinct_matched_paths",
    "matched_paths",
    "status",
    "direct_selection_binding",
    "original_semantics",
    "issue",
)

SUMMARY_FIELDS = (
    "event_path",
    "candidate_name_count",
    "candidate_pairs",
    "xbe_literal_rows",
    "xbe_literal_occurrences",
    "xbe_literal_xref_count",
    "matched_path_rows",
    "distinct_matched_paths",
    "status",
    "provenance",
    "direct_selection_binding",
    "original_semantics",
)


def read_tsv(path: Path) -> list[dict[str, str]]:
    with path.open("r", encoding="utf-8", newline="") as handle:
        return list(csv.DictReader(handle, delimiter="\t"))


def read_key_value_tsv(path: Path) -> dict[str, str]:
    rows = read_tsv(path)
    return {row.get("field", ""): row.get("value", "") for row in rows}


def _int(value: str, field: str) -> int:
    try:
        return int(value, 0)
    except (TypeError, ValueError) as exc:
        raise ValueError(f"invalid integer in {field}: {value!r}") from exc


def _split_paths(value: str) -> list[str]:
    return [part for part in value.split("|") if part]


def build_control(
    asset_rows: list[dict[str, str]],
    index_values: dict[str, str],
) -> tuple[list[dict[str, str]], dict[str, str]]:
    event_path = index_values.get("event_path", "").strip()
    expected_basename = Path(event_path).name.casefold()
    selected = [
        row
        for row in asset_rows
        if Path(row.get("literal", "").replace("\\", "/")).name.casefold()
        == expected_basename
    ]
    matched_paths = sorted(
        {
            path
            for row in selected
            for path in _split_paths(row.get("matched_paths", ""))
        },
        key=str.casefold,
    )
    literal_occurrences = sum(_int(row.get("literal_occurrences", "0"), "literal_occurrences") for row in selected)
    literal_xrefs = sum(_int(row.get("xref_count", "0"), "xref_count") for row in selected)
    candidate_names = [
        name for name in index_values.get("candidate_names", "").split(",") if name
    ]
    matched_rows = sum(
        bool(_split_paths(row.get("matched_paths", ""))) for row in selected
    )
    issue = ""
    status = STATUS
    if not event_path:
        status = "inconclusive-" + STATUS
        issue = "index report has no event_path"
    elif not selected:
        status = "inconclusive-" + STATUS
        issue = "no XBE literal row matched the EVT basename"
    elif matched_rows != len(selected) or len(matched_paths) != 1:
        status = "inconclusive-" + STATUS
        issue = "XBE literals do not converge to one unique ISO path"
    elif index_values.get("status") not in {"no-direct-selection-binding", ""}:
        status = "inconclusive-" + STATUS
        issue = "index report status is not the expected structural status"

    report_row = {
        "event_path": event_path,
        "candidate_name_count": str(len(candidate_names)),
        "candidate_pairs": index_values.get("candidate_pairs", ""),
        "xbe_literal_rows": str(len(selected)),
        "xbe_literal_occurrences": str(literal_occurrences),
        "xbe_literal_xref_count": str(literal_xrefs),
        "matched_path_rows": str(matched_rows),
        "distinct_matched_paths": str(len(matched_paths)),
        "matched_paths": "|".join(matched_paths),
        "status": status,
        "direct_selection_binding": DIRECT_SELECTION_BINDING,
        "original_semantics": ORIGINAL_SEMANTICS,
        "issue": issue,
    }
    summary = {
        "event_path": event_path,
        "candidate_name_count": str(len(candidate_names)),
        "candidate_pairs": index_values.get("candidate_pairs", ""),
        "xbe_literal_rows": str(len(selected)),
        "xbe_literal_occurrences": str(literal_occurrences),
        "xbe_literal_xref_count": str(literal_xrefs),
        "matched_path_rows": str(matched_rows),
        "distinct_matched_paths": str(len(matched_paths)),
        "status": status,
        "provenance": (
            "independent-XBE-asset-literal-report+EVT-string-index-report;"
            "textual-path-only;no-execution;no-selection"
        ),
        "direct_selection_binding": DIRECT_SELECTION_BINDING,
        "original_semantics": ORIGINAL_SEMANTICS,
    }
    return [report_row], summary


def write_tsv(path: Path, fields: tuple[str, ...], rows: list[dict[str, str]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=fields, delimiter="\t", lineterminator="\n")
        writer.writeheader()
        writer.writerows({field: row.get(field, "") for field in fields} for row in rows)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("asset_report", type=Path)
    parser.add_argument("index_summary", type=Path)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()

    report_rows, summary = build_control(
        read_tsv(args.asset_report),
        read_key_value_tsv(args.index_summary),
    )
    write_tsv(args.report, REPORT_FIELDS, report_rows)
    write_tsv(args.summary, SUMMARY_FIELDS, [summary])
    print(
        f"event_path: {summary['event_path']}; "
        f"candidate_name_count: {summary['candidate_name_count']}; "
        f"xbe_literal_rows: {summary['xbe_literal_rows']}; "
        f"distinct_matched_paths: {summary['distinct_matched_paths']}; "
        f"status: {summary['status']}"
    )
    print(f"report: {args.report}")
    print(f"summary: {args.summary}")
    print("direct_selection_binding: not-proven")
    print("original_semantics: not-claimed")
    return 0 if summary["status"] == STATUS else 1


if __name__ == "__main__":
    raise SystemExit(main())
