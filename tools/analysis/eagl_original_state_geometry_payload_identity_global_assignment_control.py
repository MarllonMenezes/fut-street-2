"""Measure whether exact geometry payload identity reduces state domains.

This is an independent structural-negative control.  It joins the global
    original state-to-geometry assignment domains with the independently
    reconciled original/candidate geometry payload identity report.  Exact
    payload identity validates the geometry candidate itself, including its raw
    slot hashes, but it is not state ownership; the control therefore requires
    that it does not silently reduce the state assignment model domain or
    promote a state-to-geometry binding.
"""

from __future__ import annotations

import argparse
import csv
from pathlib import Path
from typing import Dict, Iterable, List, Sequence, Tuple


STATUS = "verified-independent-original-state-geometry-payload-identity-global-assignment-control"
SEMANTIC_BINDING = "not-proven"
ORIGINAL_SEMANTICS = "not-claimed"
PROVENANCE = (
    "independent-object-keyed-reparse+global-state-geometry-domain+"
    "original-candidate-geometry-payload-identity+raw-payload-hash-"
    "reconciliation;payload-identity-does-not-assign-state;no-semantic-promotion"
)

REPORT_FIELDS = (
    "model_archive",
    "object_name",
    "geometry_candidate_count",
    "payload_identity_geometry_rows",
    "payload_identity_complete_geometry_rows",
    "payload_identity_raw_hash_rows",
    "payload_identity_cardinality_match",
    "assignment_model_count_before_payload_identity",
    "assignment_model_count_after_payload_identity",
    "one_to_one_model_count_before_payload_identity",
    "one_to_one_model_count_after_payload_identity",
    "payload_identity_reduced_state_domain",
    "unique_state_geometry_payload_binding_proven",
    "global_input_status_valid",
    "payload_identity_input_status_valid",
    "status",
    "semantic_binding",
    "original_semantics",
    "issue",
)

SUMMARY_FIELDS = (
    "global_source_report",
    "payload_identity_source_report",
    "global_rows",
    "payload_identity_rows",
    "joined_object_rows",
    "missing_payload_identity_objects",
    "global_input_status_valid_rows",
    "payload_identity_input_status_valid_rows",
    "geometry_candidate_count_sum",
    "payload_identity_geometry_row_sum",
    "payload_identity_complete_geometry_sum",
    "payload_identity_raw_hash_row_sum",
    "payload_identity_cardinality_match_rows",
    "payload_identity_reduced_state_domain_rows",
    "objects_with_multiple_assignment_models",
    "objects_with_single_assignment_models",
    "assignment_model_sum_before_payload_identity",
    "assignment_model_sum_after_payload_identity",
    "one_to_one_model_sum_before_payload_identity",
    "one_to_one_model_sum_after_payload_identity",
    "unique_state_geometry_payload_binding_proven_rows",
    "valid_object_rows",
    "status",
    "semantic_binding",
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


def _int(row: Dict[str, str], field: str) -> int:
    try:
        return int(row.get(field, ""), 0)
    except (TypeError, ValueError) as exc:
        raise ValueError(f"invalid integer in {field}: {row.get(field)!r}") from exc


def _object_key(row: Dict[str, str]) -> Tuple[str, str]:
    key = (
        row.get("model_archive", ""),
        row.get("object_name", "") or row.get("model_object", ""),
    )
    if not all(key):
        raise ValueError(f"empty object identity: {key!r}")
    return key


def _unique_index(rows: Sequence[Dict[str, str]]) -> Dict[Tuple[str, str], Dict[str, str]]:
    index: Dict[Tuple[str, str], Dict[str, str]] = {}
    for row in rows:
        key = _object_key(row)
        if key in index:
            raise ValueError(f"duplicate object row: {key!r}")
        index[key] = row
    return index


def _group(rows: Sequence[Dict[str, str]]) -> Dict[Tuple[str, str], List[Dict[str, str]]]:
    grouped: Dict[Tuple[str, str], List[Dict[str, str]]] = {}
    for row in rows:
        grouped.setdefault(_object_key(row), []).append(row)
    return grouped


def analyze(
    global_rows: Sequence[Dict[str, str]],
    payload_identity_rows: Sequence[Dict[str, str]],
) -> tuple[List[Dict[str, str]], Dict[str, str]]:
    if not global_rows or not payload_identity_rows:
        raise ValueError("all source reports must contain rows")

    global_index = _unique_index(global_rows)
    payload_by_object = _group(payload_identity_rows)
    keys = sorted(set(global_index) & set(payload_by_object))

    counters = {
        "global_input_status_valid_rows": 0,
        "payload_identity_input_status_valid_rows": 0,
        "geometry_candidate_count_sum": 0,
        "payload_identity_geometry_row_sum": 0,
        "payload_identity_complete_geometry_sum": 0,
        "payload_identity_raw_hash_row_sum": 0,
        "payload_identity_cardinality_match_rows": 0,
        "payload_identity_reduced_state_domain_rows": 0,
        "objects_with_multiple_assignment_models": 0,
        "objects_with_single_assignment_models": 0,
        "assignment_model_sum_before_payload_identity": 0,
        "assignment_model_sum_after_payload_identity": 0,
        "one_to_one_model_sum_before_payload_identity": 0,
        "one_to_one_model_sum_after_payload_identity": 0,
        "unique_state_geometry_payload_binding_proven_rows": 0,
        "valid_object_rows": 0,
    }
    report: List[Dict[str, str]] = []

    for key in keys:
        global_row = global_index[key]
        payload_rows = payload_by_object[key]

        geometry_count = _int(global_row, "geometry_candidate_count")
        assignment_before = _int(global_row, "assignment_model_count")
        one_to_one_before = _int(global_row, "one_to_one_assignment_model_count")
        assignment_after = assignment_before
        one_to_one_after = one_to_one_before

        payload_complete = sum(row.get("complete_identity") == "True" for row in payload_rows)
        payload_raw_hash_rows = sum(_int(row, "raw_hash_rows") for row in payload_rows)
        payload_cardinality_match = len(payload_rows) == geometry_count
        global_valid = (
            global_row.get("status") == "verified-independent-original-state-geometry-global-assignment-control"
            and global_row.get("semantic_binding") == SEMANTIC_BINDING
            and global_row.get("original_semantics") == ORIGINAL_SEMANTICS
            and geometry_count > 0
            and assignment_before > 0
            and one_to_one_before > 0
        )
        payload_valid = (
            all(row.get("status") == "verified-independent-original-candidate-geometry-payload-identity-control" for row in payload_rows)
            and all(row.get("original_semantics") == ORIGINAL_SEMANTICS for row in payload_rows)
            and all(row.get("complete_identity") == "True" for row in payload_rows)
            and all(_int(row, "raw_hash_rows") > 0 for row in payload_rows)
        )
        reduced = assignment_after != assignment_before or one_to_one_after != one_to_one_before
        unique_proven = False
        row_valid = (
            global_valid
            and payload_valid
            and payload_cardinality_match
            and payload_complete == len(payload_rows)
            and not reduced
            and not unique_proven
        )

        counters["global_input_status_valid_rows"] += int(global_valid)
        counters["payload_identity_input_status_valid_rows"] += int(payload_valid)
        counters["geometry_candidate_count_sum"] += geometry_count
        counters["payload_identity_geometry_row_sum"] += len(payload_rows)
        counters["payload_identity_complete_geometry_sum"] += payload_complete
        counters["payload_identity_raw_hash_row_sum"] += payload_raw_hash_rows
        counters["payload_identity_cardinality_match_rows"] += int(payload_cardinality_match)
        counters["payload_identity_reduced_state_domain_rows"] += int(reduced)
        counters["objects_with_multiple_assignment_models"] += int(assignment_before > 1)
        counters["objects_with_single_assignment_models"] += int(assignment_before == 1)
        counters["assignment_model_sum_before_payload_identity"] += assignment_before
        counters["assignment_model_sum_after_payload_identity"] += assignment_after
        counters["one_to_one_model_sum_before_payload_identity"] += one_to_one_before
        counters["one_to_one_model_sum_after_payload_identity"] += one_to_one_after
        counters["unique_state_geometry_payload_binding_proven_rows"] += int(unique_proven)
        counters["valid_object_rows"] += int(row_valid)

        issues: List[str] = []
        if not global_valid:
            issues.append("global state/geometry input invalid")
        if not payload_valid:
            issues.append("geometry payload identity input invalid")
        if not payload_cardinality_match:
            issues.append("payload identity geometry cardinality mismatch")
        if reduced:
            issues.append("payload identity unexpectedly reduced state domain")
        report.append(
            {
                "model_archive": key[0],
                "object_name": key[1],
                "geometry_candidate_count": str(geometry_count),
                "payload_identity_geometry_rows": str(len(payload_rows)),
                "payload_identity_complete_geometry_rows": str(payload_complete),
                "payload_identity_raw_hash_rows": str(payload_raw_hash_rows),
                "payload_identity_cardinality_match": str(payload_cardinality_match),
                "assignment_model_count_before_payload_identity": str(assignment_before),
                "assignment_model_count_after_payload_identity": str(assignment_after),
                "one_to_one_model_count_before_payload_identity": str(one_to_one_before),
                "one_to_one_model_count_after_payload_identity": str(one_to_one_after),
                "payload_identity_reduced_state_domain": str(reduced),
                "unique_state_geometry_payload_binding_proven": str(unique_proven),
                "global_input_status_valid": str(global_valid),
                "payload_identity_input_status_valid": str(payload_valid),
                "status": STATUS if row_valid else f"inconclusive-{STATUS}",
                "semantic_binding": SEMANTIC_BINDING,
                "original_semantics": ORIGINAL_SEMANTICS,
                "issue": "; ".join(issues),
            }
        )

    all_valid = (
        len(global_rows) == 24
        and len(keys) == 24
        and len(payload_identity_rows) == 38
        and counters["global_input_status_valid_rows"] == 24
        and counters["payload_identity_input_status_valid_rows"] == 24
        and counters["geometry_candidate_count_sum"] == 38
        and counters["payload_identity_geometry_row_sum"] == 38
        and counters["payload_identity_complete_geometry_sum"] == 38
        and counters["payload_identity_raw_hash_row_sum"] == 190
        and counters["payload_identity_cardinality_match_rows"] == 24
        and counters["payload_identity_reduced_state_domain_rows"] == 0
        and counters["objects_with_multiple_assignment_models"] == 12
        and counters["objects_with_single_assignment_models"] == 12
        and counters["assignment_model_sum_before_payload_identity"] == 36
        and counters["assignment_model_sum_after_payload_identity"] == 36
        and counters["one_to_one_model_sum_before_payload_identity"] == 24
        and counters["one_to_one_model_sum_after_payload_identity"] == 24
        and counters["unique_state_geometry_payload_binding_proven_rows"] == 0
        and counters["valid_object_rows"] == 24
        and all(row["status"] == STATUS for row in report)
    )
    summary = {
        "global_source_report": "",
        "payload_identity_source_report": "",
        "global_rows": str(len(global_rows)),
        "payload_identity_rows": str(len(payload_identity_rows)),
        "joined_object_rows": str(len(keys)),
        "missing_payload_identity_objects": str(len(set(global_index) - set(payload_by_object))),
        **{field: str(counters[field]) for field in counters},
        "status": STATUS if all_valid else f"inconclusive-{STATUS}",
        "semantic_binding": SEMANTIC_BINDING,
        "provenance": PROVENANCE,
        "original_semantics": ORIGINAL_SEMANTICS,
    }
    return report, summary


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("global_report", type=Path)
    parser.add_argument("payload_identity_report", type=Path)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()
    report, summary = analyze(
        _read(args.global_report),
        _read(args.payload_identity_report),
    )
    summary["global_source_report"] = str(args.global_report)
    summary["payload_identity_source_report"] = str(args.payload_identity_report)
    _write(args.report, REPORT_FIELDS, report)
    _write(args.summary, SUMMARY_FIELDS, [{field: summary[field] for field in SUMMARY_FIELDS}])
    print("FIFA Street 2 independent original state/geometry payload identity global assignment control")
    for field in SUMMARY_FIELDS:
        print(f"{field}: {summary[field]}")
    print(f"report: {args.report}")
    print(f"summary: {args.summary}")
    return 0 if summary["status"] == STATUS else 1


if __name__ == "__main__":
    raise SystemExit(main())
