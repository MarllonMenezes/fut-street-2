"""Compare original SHAPENAME primitive indices with candidate parser indices.

This is an independent structural control for per-object index/layer/order
reconciliation.  Exact sequence agreement does not decode a state-to-vertex,
UV, material, skinning, rigging, animation, morph, command or GPU binding.
"""

from __future__ import annotations

import argparse
import csv
from pathlib import Path
from typing import Dict, List, Sequence, Tuple


STATUS = "verified-independent-original-candidate-shapename-primitive-index-control"
ORIGINAL_SEMANTICS = "not-claimed"
PROVENANCE = (
    "independent-object-key-join+original-SHAPENAME-layer:primitive-sequence/"
    "candidate-SHAPENAME-layer/index-reparse+candidate-stream-completeness;"
    "no-primitive-or-UV-promotion"
)

REPORT_FIELDS = (
    "model_archive",
    "model_object",
    "shape_name",
    "original_state_primitive_indices",
    "candidate_shape_state_layers",
    "candidate_shape_state_indices",
    "original_candidate_sequence_match",
    "original_candidate_set_match",
    "candidate_shape_state_count_match",
    "candidate_shape_state_streams_complete",
    "original_input_valid",
    "candidate_input_valid",
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
    "sequence_match_rows",
    "set_match_rows",
    "count_match_rows",
    "complete_candidate_rows",
    "original_input_valid_rows",
    "candidate_input_valid_rows",
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
            handle, fieldnames=list(fields), delimiter="\t", lineterminator="\n"
        )
        writer.writeheader()
        writer.writerows({field: row.get(field, "") for field in fields} for row in rows)


def _key(row: Dict[str, str], archive_field: str, object_field: str) -> Tuple[str, str]:
    return row.get(archive_field, ""), row.get(object_field, "")


def _bool(row: Dict[str, str], field: str) -> bool:
    return row.get(field, "").strip().lower() == "true"


def _index_sequence(value: str) -> Tuple[Tuple[int, int], ...]:
    result = []
    for token in value.split(","):
        token = token.strip()
        if not token:
            continue
        layer, primitive = token.split(":", 1)
        result.append((int(layer), int(primitive)))
    return tuple(result)


def _candidate_sequence(row: Dict[str, str]) -> Tuple[Tuple[int, int], ...]:
    layers = [int(value) for value in row.get("candidate_shape_state_layers", "").split(",") if value]
    indices = [int(value) for value in row.get("candidate_shape_state_indices", "").split(",") if value]
    if len(layers) != len(indices):
        return tuple()
    return tuple(zip(layers, indices))


def analyze(
    state_rows: Sequence[Dict[str, str]],
    candidate_rows: Sequence[Dict[str, str]],
) -> Tuple[List[Dict[str, str]], Dict[str, str]]:
    state_by_key = {_key(row, "model_archive", "object_name"): row for row in state_rows}
    candidate_by_key = {
        _key(row, "model_archive", "model_object"): row for row in candidate_rows
    }
    keys = sorted(set(state_by_key) & set(candidate_by_key))
    counters = {
        "sequence_match": 0,
        "set_match": 0,
        "count_match": 0,
        "complete_candidate": 0,
        "original_input_valid": 0,
        "candidate_input_valid": 0,
    }
    report: List[Dict[str, str]] = []
    for key in keys:
        state = state_by_key[key]
        candidate = candidate_by_key[key]
        original_sequence = _index_sequence(state.get("state_primitive_indices", ""))
        candidate_sequence = _candidate_sequence(candidate)
        original_valid = state.get("status") == "verified-control"
        candidate_valid = (
            candidate.get("status") == "verified-candidate-shape-state-index-control"
            and candidate.get("original_semantics") == ORIGINAL_SEMANTICS
        )
        sequence_match = original_sequence == candidate_sequence
        set_match = set(original_sequence) == set(candidate_sequence)
        count_match = len(original_sequence) == int(candidate.get("candidate_shape_state_count", "-1"))
        complete_candidate = _bool(candidate, "candidate_shape_state_streams_complete")
        values = {
            "sequence_match": sequence_match,
            "set_match": set_match,
            "count_match": count_match,
            "complete_candidate": complete_candidate,
            "original_input_valid": original_valid,
            "candidate_input_valid": candidate_valid,
        }
        for field, value in values.items():
            counters[field] += int(value)
        valid = all(values.values())
        report.append(
            {
                "model_archive": key[0],
                "model_object": key[1],
                "shape_name": state.get("shape_name", candidate.get("shape_name", "")),
                "original_state_primitive_indices": state.get("state_primitive_indices", ""),
                "candidate_shape_state_layers": candidate.get("candidate_shape_state_layers", ""),
                "candidate_shape_state_indices": candidate.get("candidate_shape_state_indices", ""),
                "original_candidate_sequence_match": str(sequence_match),
                "original_candidate_set_match": str(set_match),
                "candidate_shape_state_count_match": str(count_match),
                "candidate_shape_state_streams_complete": str(complete_candidate),
                "original_input_valid": str(original_valid),
                "candidate_input_valid": str(candidate_valid),
                "status": STATUS if valid else f"inconclusive-{STATUS}",
                "provenance": PROVENANCE,
                "original_semantics": ORIGINAL_SEMANTICS,
                "issue": "" if valid else "one or more structural index controls failed",
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
    )
    summary = {
        "state_source_report": "",
        "candidate_source_report": "",
        "state_rows": str(len(state_rows)),
        "candidate_rows": str(len(candidate_rows)),
        "joined_rows": str(len(keys)),
        "missing_candidate_objects": str(len(set(state_by_key) - set(candidate_by_key))),
        "missing_state_objects": str(len(set(candidate_by_key) - set(state_by_key))),
        "sequence_match_rows": str(counters["sequence_match"]),
        "set_match_rows": str(counters["set_match"]),
        "count_match_rows": str(counters["count_match"]),
        "complete_candidate_rows": str(counters["complete_candidate"]),
        "original_input_valid_rows": str(counters["original_input_valid"]),
        "candidate_input_valid_rows": str(counters["candidate_input_valid"]),
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
    print("FIFA Street 2 independent original/candidate SHAPENAME primitive index control")
    for field in SUMMARY_FIELDS:
        print(f"{field}: {summary[field]}")
    print(f"report: {args.report}")
    print(f"summary: {args.summary}")
    return 0 if summary["status"] == STATUS else 1


if __name__ == "__main__":
    raise SystemExit(main())
