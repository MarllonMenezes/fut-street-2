"""Join original SHAPENAME state tokens to same-trunk candidate asset rows.

The join is lexical and structural only.  It verifies that the original
state/geometry report and the candidate source-chain report agree on the
opaque object identity and on the observed shape spelling.  It does not turn
SHAPENAME into a texture/material binding, UV interpretation, vertex meaning,
GPU command, or original draw rule.
"""

from __future__ import annotations

import argparse
import csv
from pathlib import Path
from typing import Dict, List, Sequence, Tuple


STATUS = "verified-independent-original-shapename-candidate-asset-control"
ORIGINAL_SEMANTICS = "not-claimed"
PROVENANCE = (
    "independent-object-key-join+lexical-SHAPENAME/candidate-shape-name-"
    "reparse+same-trunk/source-pixel-controls;no-material-or-UV-promotion"
)

REPORT_FIELDS = (
    "model_archive",
    "model_object",
    "original_shape_names",
    "candidate_shape_name",
    "object_state_exact",
    "shapename_token_match",
    "candidate_same_trunk",
    "candidate_source_chain_valid",
    "candidate_shape_entry_exact",
    "candidate_decoded_pixels_match",
    "status",
    "provenance",
    "original_semantics",
    "issue",
)

SUMMARY_FIELDS = (
    "state_source_report",
    "candidate_source_report",
    "state_rows",
    "candidate_rows",
    "joined_rows",
    "missing_candidate_objects",
    "missing_state_objects",
    "state_input_valid_rows",
    "candidate_input_valid_rows",
    "object_state_exact_rows",
    "shapename_token_match_rows",
    "same_trunk_rows",
    "source_chain_valid_rows",
    "shape_entry_exact_rows",
    "decoded_pixel_match_rows",
    "status",
    "provenance",
    "original_semantics",
)


def _read(path: Path) -> List[Dict[str, str]]:
    with path.open("r", encoding="utf-8-sig", newline="") as handle:
        return list(csv.DictReader(handle, delimiter="\t"))


def _write(path: Path, fields: Sequence[str], rows: Sequence[Dict[str, str]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=list(fields), delimiter="\t", lineterminator="\n")
        writer.writeheader()
        writer.writerows({field: row.get(field, "") for field in fields} for row in rows)


def _bool(row: Dict[str, str], field: str) -> bool:
    return row.get(field, "").strip().lower() == "true"


def _key(row: Dict[str, str], archive: str, object_name: str) -> Tuple[str, str]:
    return row.get(archive, ""), row.get(object_name, "")


def _shape_tokens(value: str) -> List[str]:
    return [part for part in value.split("|") if part.startswith("SHAPENAME=")]


def analyze(
    state_rows: Sequence[Dict[str, str]],
    candidate_rows: Sequence[Dict[str, str]],
) -> Tuple[List[Dict[str, str]], Dict[str, str]]:
    state_by_key = {_key(row, "model_archive", "object_name"): row for row in state_rows}
    candidate_by_key = {
        _key(row, "model_archive_name", "model_object_name"): row for row in candidate_rows
    }
    keys = sorted(set(state_by_key) & set(candidate_by_key))
    report: List[Dict[str, str]] = []
    counters = {
        "object_state_exact": 0,
        "shapename_token_match": 0,
        "candidate_same_trunk": 0,
        "candidate_source_chain_valid": 0,
        "candidate_shape_entry_exact": 0,
        "candidate_decoded_pixels_match": 0,
    }
    for key in keys:
        state = state_by_key[key]
        candidate = candidate_by_key[key]
        candidate_shape = candidate.get("shape_name", "")
        shape_tokens = _shape_tokens(state.get("state_names", ""))
        shape_match = bool(candidate_shape) and any(
            token == f"SHAPENAME={candidate_shape},1" or token == f"SHAPENAME={candidate_shape}"
            for token in shape_tokens
        )
        object_state_exact = _bool(state, "object_state_exact")
        same_trunk = _bool(candidate, "same_trunk")
        source_chain_valid = candidate.get("status") == "verified-candidate-source-chain"
        shape_entry_exact = _bool(candidate, "shape_entry_exact")
        decoded_pixels_match = _bool(candidate, "decoded_pixels_match")
        values = {
            "object_state_exact": object_state_exact,
            "shapename_token_match": shape_match,
            "candidate_same_trunk": same_trunk,
            "candidate_source_chain_valid": source_chain_valid,
            "candidate_shape_entry_exact": shape_entry_exact,
            "candidate_decoded_pixels_match": decoded_pixels_match,
        }
        for field, value in values.items():
            counters[field] += int(value)
        valid = all(values.values()) and (
            state.get("status") == "verified-control" and
            candidate.get("original_semantics") == ORIGINAL_SEMANTICS
        )
        row_status = STATUS if valid else f"inconclusive-{STATUS}"
        issue = "" if valid else "one or more lexical/structural join controls failed"
        report.append({
            "model_archive": key[0],
            "model_object": key[1],
            "original_shape_names": "|".join(shape_tokens),
            "candidate_shape_name": candidate_shape,
            "object_state_exact": str(object_state_exact),
            "shapename_token_match": str(shape_match),
            "candidate_same_trunk": str(same_trunk),
            "candidate_source_chain_valid": str(source_chain_valid),
            "candidate_shape_entry_exact": str(shape_entry_exact),
            "candidate_decoded_pixels_match": str(decoded_pixels_match),
            "status": row_status,
            "provenance": PROVENANCE,
            "original_semantics": ORIGINAL_SEMANTICS,
            "issue": issue,
        })

    summary = {
        "state_source_report": "",
        "candidate_source_report": "",
        "state_rows": str(len(state_rows)),
        "candidate_rows": str(len(candidate_rows)),
        "joined_rows": str(len(keys)),
        "missing_candidate_objects": str(len(set(state_by_key) - set(candidate_by_key))),
        "missing_state_objects": str(len(set(candidate_by_key) - set(state_by_key))),
        "state_input_valid_rows": str(sum(row.get("status") == "verified-control" for row in state_rows)),
        "candidate_input_valid_rows": str(sum(row.get("status") == "verified-candidate-source-chain" and row.get("original_semantics") == ORIGINAL_SEMANTICS for row in candidate_rows)),
        "object_state_exact_rows": str(counters["object_state_exact"]),
        "shapename_token_match_rows": str(counters["shapename_token_match"]),
        "same_trunk_rows": str(counters["candidate_same_trunk"]),
        "source_chain_valid_rows": str(counters["candidate_source_chain_valid"]),
        "shape_entry_exact_rows": str(counters["candidate_shape_entry_exact"]),
        "decoded_pixel_match_rows": str(counters["candidate_decoded_pixels_match"]),
        "status": STATUS if (
            len(state_rows) == 24 and len(candidate_rows) == 24 and len(keys) == 24 and
            not (set(state_by_key) - set(candidate_by_key)) and
            not (set(candidate_by_key) - set(state_by_key)) and
            all(value == 24 for value in counters.values()) and
            all(row["status"] == STATUS for row in report) and
            all(row.get("original_semantics") == ORIGINAL_SEMANTICS for row in candidate_rows)
        ) else f"inconclusive-{STATUS}",
        "provenance": PROVENANCE,
        "original_semantics": ORIGINAL_SEMANTICS,
    }
    return report, summary


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("state_report", type=Path)
    parser.add_argument("candidate_report", type=Path)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()
    report, summary = analyze(_read(args.state_report), _read(args.candidate_report))
    summary["state_source_report"] = str(args.state_report)
    summary["candidate_source_report"] = str(args.candidate_report)
    _write(args.report, REPORT_FIELDS, report)
    _write(args.summary, SUMMARY_FIELDS, [summary])
    print("FIFA Street 2 independent original SHAPENAME/candidate asset control")
    for field in SUMMARY_FIELDS:
        print(f"{field}: {summary[field]}")
    print(f"report: {args.report}")
    print(f"summary: {args.summary}")
    return 0 if summary["status"] == STATUS else 1


if __name__ == "__main__":
    raise SystemExit(main())
