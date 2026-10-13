"""Join original SHAPENAME primitive population to candidate shape-state rows.

This is a bounded structural control.  It checks that the original state
report's per-object SHAPENAME primitive population agrees with the candidate
asset's independently parsed shape-state stream population.  Matching counts
do not decode primitive-to-vertex, UV, material, skinning, rigging, animation,
morph, command or GPU semantics.
"""

from __future__ import annotations

import argparse
import csv
from pathlib import Path
from typing import Dict, List, Sequence, Tuple


STATUS = "verified-independent-original-state-candidate-primitive-shape-control"
ORIGINAL_SEMANTICS = "not-claimed"
PROVENANCE = (
    "independent-object-key-join+original-SHAPENAME-primitive-population/"
    "candidate-shape-state-stream-count-reparse+same-trunk/source-pixel-controls;"
    "no-primitive-or-UV-promotion"
)

REPORT_FIELDS = (
    "model_archive",
    "model_object",
    "original_state_primitive_count",
    "original_shapename_token_count",
    "candidate_shape_state_count",
    "candidate_shape_state_streams_complete_count",
    "candidate_primitive_count",
    "state_count_matches_candidate_shape_state_count",
    "shapename_count_matches_candidate_shape_state_count",
    "candidate_shape_state_streams_complete",
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
    "state_primitive_count_match_rows",
    "shapename_count_match_rows",
    "candidate_shape_state_streams_complete_rows",
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
        writer = csv.DictWriter(
            handle,
            fieldnames=list(fields),
            delimiter="\t",
            lineterminator="\n",
        )
        writer.writeheader()
        writer.writerows({field: row.get(field, "") for field in fields} for row in rows)


def _bool(row: Dict[str, str], field: str) -> bool:
    return row.get(field, "").strip().lower() == "true"


def _int(row: Dict[str, str], field: str) -> int | None:
    try:
        return int(row.get(field, ""))
    except ValueError:
        return None


def _key(row: Dict[str, str], archive_field: str, object_field: str) -> Tuple[str, str]:
    return row.get(archive_field, ""), row.get(object_field, "")


def _shapename_token_count(value: str) -> int:
    return sum(part.startswith("SHAPENAME=") for part in value.split("|") if part)


def analyze(
    state_rows: Sequence[Dict[str, str]],
    candidate_rows: Sequence[Dict[str, str]],
) -> Tuple[List[Dict[str, str]], Dict[str, str]]:
    state_by_key = {_key(row, "model_archive", "object_name"): row for row in state_rows}
    candidate_by_key = {
        _key(row, "model_archive_name", "model_object_name"): row
        for row in candidate_rows
    }
    keys = sorted(set(state_by_key) & set(candidate_by_key))
    counters = {
        "state_primitive_count_match": 0,
        "shapename_count_match": 0,
        "candidate_shape_state_streams_complete": 0,
        "same_trunk": 0,
        "source_chain_valid": 0,
        "shape_entry_exact": 0,
        "decoded_pixel_match": 0,
    }
    report: List[Dict[str, str]] = []
    for key in keys:
        state = state_by_key[key]
        candidate = candidate_by_key[key]
        state_count = _int(state, "state_primitive_count")
        token_count = _shapename_token_count(state.get("state_names", ""))
        candidate_count = _int(candidate, "candidate_primitive_shape_state_count")
        candidate_complete_count = _int(
            candidate, "candidate_primitive_shape_state_streams_complete_count"
        )
        candidate_primitive_count = _int(candidate, "candidate_primitive_count")
        state_count_match = state_count is not None and state_count == candidate_count
        token_count_match = token_count == candidate_count
        candidate_complete = (
            candidate_count is not None
            and candidate_complete_count == candidate_count
        )
        same_trunk = _bool(candidate, "same_trunk")
        source_chain_valid = candidate.get("status") == "verified-candidate-source-chain"
        shape_entry_exact = _bool(candidate, "shape_entry_exact")
        decoded_pixels_match = _bool(candidate, "decoded_pixels_match")
        values = {
            "state_primitive_count_match": state_count_match,
            "shapename_count_match": token_count_match,
            "candidate_shape_state_streams_complete": candidate_complete,
            "same_trunk": same_trunk,
            "source_chain_valid": source_chain_valid,
            "shape_entry_exact": shape_entry_exact,
            "decoded_pixel_match": decoded_pixels_match,
        }
        for field, value in values.items():
            counters[field] += int(value)
        valid = all(values.values()) and (
            state.get("status") == "verified-control"
            and candidate.get("original_semantics") == ORIGINAL_SEMANTICS
        )
        row_status = STATUS if valid else f"inconclusive-{STATUS}"
        report.append(
            {
                "model_archive": key[0],
                "model_object": key[1],
                "original_state_primitive_count": "" if state_count is None else str(state_count),
                "original_shapename_token_count": str(token_count),
                "candidate_shape_state_count": "" if candidate_count is None else str(candidate_count),
                "candidate_shape_state_streams_complete_count": ""
                if candidate_complete_count is None
                else str(candidate_complete_count),
                "candidate_primitive_count": ""
                if candidate_primitive_count is None
                else str(candidate_primitive_count),
                "state_count_matches_candidate_shape_state_count": str(state_count_match),
                "shapename_count_matches_candidate_shape_state_count": str(token_count_match),
                "candidate_shape_state_streams_complete": str(candidate_complete),
                "candidate_same_trunk": str(same_trunk),
                "candidate_source_chain_valid": str(source_chain_valid),
                "candidate_shape_entry_exact": str(shape_entry_exact),
                "candidate_decoded_pixels_match": str(decoded_pixels_match),
                "status": row_status,
                "provenance": PROVENANCE,
                "original_semantics": ORIGINAL_SEMANTICS,
                "issue": "" if valid else "one or more structural population controls failed",
            }
        )

    complete = (
        len(state_rows) == 24
        and len(candidate_rows) == 24
        and len(keys) == 24
        and not (set(state_by_key) - set(candidate_by_key))
        and not (set(candidate_by_key) - set(state_by_key))
        and all(value == 24 for value in counters.values())
        and all(row["status"] == STATUS for row in report)
        and all(row.get("original_semantics") == ORIGINAL_SEMANTICS for row in candidate_rows)
    )
    summary = {
        "state_source_report": "",
        "candidate_source_report": "",
        "state_rows": str(len(state_rows)),
        "candidate_rows": str(len(candidate_rows)),
        "joined_rows": str(len(keys)),
        "missing_candidate_objects": str(len(set(state_by_key) - set(candidate_by_key))),
        "missing_state_objects": str(len(set(candidate_by_key) - set(state_by_key))),
        "state_input_valid_rows": str(sum(row.get("status") == "verified-control" for row in state_rows)),
        "candidate_input_valid_rows": str(
            sum(
                row.get("status") == "verified-candidate-source-chain"
                and row.get("original_semantics") == ORIGINAL_SEMANTICS
                for row in candidate_rows
            )
        ),
        "state_primitive_count_match_rows": str(counters["state_primitive_count_match"]),
        "shapename_count_match_rows": str(counters["shapename_count_match"]),
        "candidate_shape_state_streams_complete_rows": str(
            counters["candidate_shape_state_streams_complete"]
        ),
        "same_trunk_rows": str(counters["same_trunk"]),
        "source_chain_valid_rows": str(counters["source_chain_valid"]),
        "shape_entry_exact_rows": str(counters["shape_entry_exact"]),
        "decoded_pixel_match_rows": str(counters["decoded_pixel_match"]),
        "status": STATUS if complete else f"inconclusive-{STATUS}",
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
    print("FIFA Street 2 independent original state/candidate primitive shape control")
    for field in SUMMARY_FIELDS:
        print(f"{field}: {summary[field]}")
    print(f"report: {args.report}")
    print(f"summary: {args.summary}")
    return 0 if summary["status"] == STATUS else 1


if __name__ == "__main__":
    raise SystemExit(main())
