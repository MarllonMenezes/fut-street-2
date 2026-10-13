"""Reconcile global original state/geometry models with candidate render groups.

This control compares the already enumerated original state-to-geometry model
domain with the same-object candidate mesh/source-chain and candidate runtime
reports.  It preserves the important negative result that object-state
agreement is exact while candidate render-group state agreement is zero.  It
does not select a draw, mesh, UV, material or GPU binding.
"""

from __future__ import annotations

import argparse
import csv
from pathlib import Path
from typing import Dict, Iterable, List, Sequence, Tuple


STATUS = "verified-independent-original-state-geometry-candidate-render-global-assignment-control"
SEMANTIC_BINDING = "not-proven"
ORIGINAL_SEMANTICS = "not-claimed"
PROVENANCE = (
    "independent-object-keyed-reparse+global-state-geometry-model-domain+candidate-"
    "group/source-chain+candidate-runtime-state-crosscheck;no-draw-or-material-promotion"
)

REPORT_FIELDS = (
    "model_archive",
    "object_name",
    "shape_name",
    "geometry_candidate_count",
    "candidate_geometry_groups",
    "assignment_model_count",
    "one_to_one_assignment_model_count",
    "object_state_exact",
    "render_group_state",
    "geometry_candidate_cardinality_match",
    "global_input_status_valid",
    "runtime_input_status_valid",
    "candidate_input_status_valid",
    "payload_join_input_status_valid",
    "candidate_source_chain_valid",
    "payload_object_join_valid",
    "unique_state_geometry_candidate_render_binding_proven",
    "status",
    "semantic_binding",
    "original_semantics",
    "issue",
)

SUMMARY_FIELDS = (
    "global_source_report",
    "runtime_source_report",
    "candidate_source_report",
    "payload_join_source_report",
    "global_rows",
    "runtime_rows",
    "candidate_rows",
    "payload_join_rows",
    "joined_object_rows",
    "missing_runtime_objects",
    "missing_candidate_objects",
    "missing_payload_join_objects",
    "global_input_status_valid_rows",
    "runtime_input_status_valid_rows",
    "candidate_input_status_valid_rows",
    "payload_join_input_status_valid_rows",
    "geometry_candidate_count_sum",
    "candidate_geometry_group_sum",
    "geometry_candidate_cardinality_match_rows",
    "object_state_exact_rows",
    "render_group_state_match_rows",
    "candidate_source_chain_valid_rows",
    "payload_object_join_valid_rows",
    "objects_with_multiple_geometry_assignment_models",
    "objects_with_single_geometry_assignment_models",
    "unconstrained_assignment_model_sum",
    "one_to_one_assignment_model_sum",
    "unique_state_geometry_candidate_render_binding_proven_rows",
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
        writer = csv.DictWriter(handle, fieldnames=list(fields), delimiter="\t", lineterminator="\n")
        writer.writeheader()
        writer.writerows({field: row.get(field, "") for field in fields} for row in rows)


def _key(row: Dict[str, str], archive: str, obj: str, shape: str) -> Tuple[str, str, str]:
    value = (row.get(archive, ""), row.get(obj, ""), row.get(shape, ""))
    if not all(value):
        raise ValueError(f"empty object identity: {value!r}")
    return value


def _index(rows: Sequence[Dict[str, str]], archive: str, obj: str, shape: str) -> Dict[Tuple[str, str, str], Dict[str, str]]:
    indexed: Dict[Tuple[str, str, str], Dict[str, str]] = {}
    for row in rows:
        key = _key(row, archive, obj, shape)
        if key in indexed:
            raise ValueError(f"duplicate object identity: {key!r}")
        indexed[key] = row
    return indexed


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
    global_rows: Sequence[Dict[str, str]],
    runtime_rows: Sequence[Dict[str, str]],
    candidate_rows: Sequence[Dict[str, str]],
    payload_rows: Sequence[Dict[str, str]],
) -> Tuple[List[Dict[str, str]], Dict[str, str]]:
    if not global_rows or not runtime_rows or not candidate_rows or not payload_rows:
        raise ValueError("all source reports must contain rows")
    global_index = _index(global_rows, "model_archive", "object_name", "shape_name")
    runtime_index = _index(runtime_rows, "model_archive", "model_object", "shape_name")
    candidate_index = _index(candidate_rows, "model_archive_name", "model_object_name", "shape_name")
    payload_index = _index(payload_rows, "model_archive", "object_name", "shape_name")
    keys = sorted(set(global_index) & set(runtime_index) & set(candidate_index) & set(payload_index))

    report: List[Dict[str, str]] = []
    counters = {
        "global_input_status_valid_rows": 0,
        "runtime_input_status_valid_rows": 0,
        "candidate_input_status_valid_rows": 0,
        "payload_join_input_status_valid_rows": 0,
        "geometry_candidate_count_sum": 0,
        "candidate_geometry_group_sum": 0,
        "geometry_candidate_cardinality_match_rows": 0,
        "object_state_exact_rows": 0,
        "render_group_state_match_rows": 0,
        "candidate_source_chain_valid_rows": 0,
        "payload_object_join_valid_rows": 0,
        "objects_with_multiple_geometry_assignment_models": 0,
        "objects_with_single_geometry_assignment_models": 0,
        "unconstrained_assignment_model_sum": 0,
        "one_to_one_assignment_model_sum": 0,
        "unique_state_geometry_candidate_render_binding_proven_rows": 0,
        "valid_object_rows": 0,
    }
    for key in keys:
        original = global_index[key]
        runtime = runtime_index[key]
        candidate = candidate_index[key]
        payload = payload_index[key]
        geometry_count = _int(original, "geometry_candidate_count")
        candidate_groups = _int(candidate, "candidate_geometry_groups")
        assignment_models = _int(original, "assignment_model_count")
        one_to_one_models = _int(original, "one_to_one_assignment_model_count")
        global_valid = (
            original.get("status") == "verified-independent-original-state-geometry-global-assignment-control"
            and original.get("semantic_binding") == SEMANTIC_BINDING
            and original.get("original_semantics") == ORIGINAL_SEMANTICS
            and _bool(original, "state_index_set_reconciled")
        )
        runtime_valid = (
            runtime.get("status") == "verified-candidate"
            and runtime.get("object_state") == "exact"
        )
        candidate_valid = (
            candidate.get("status") == "verified-candidate-source-chain"
            and _bool(candidate, "same_trunk")
            and _bool(candidate, "candidate_bounds_valid")
            and _bool(candidate, "candidate_geometry_streams_complete")
            and _bool(candidate, "candidate_shape_state_streams_complete")
            and _bool(candidate, "candidate_geometry_finite_and_indexed")
            and candidate.get("original_semantics") == ORIGINAL_SEMANTICS
        )
        payload_valid = (
            payload.get("status") == "verified-independent-original-state-geometry-payload-join-control"
            and _bool(payload, "identity_join_match")
            and _bool(payload, "structural_counts_valid")
            and _bool(payload, "primitive_count_match")
            and _bool(payload, "distinct_payload_range_count_match")
            and payload.get("original_semantics") == ORIGINAL_SEMANTICS
        )
        cardinality_match = geometry_count == candidate_groups
        object_state_exact = runtime.get("object_state") == "exact"
        render_group_state_match = runtime.get("render_group_state") == "exact"
        unique_proven = False
        row_valid = global_valid and runtime_valid and candidate_valid and payload_valid and cardinality_match and not render_group_state_match
        counters["global_input_status_valid_rows"] += int(global_valid)
        counters["runtime_input_status_valid_rows"] += int(runtime_valid)
        counters["candidate_input_status_valid_rows"] += int(candidate_valid)
        counters["payload_join_input_status_valid_rows"] += int(payload_valid)
        counters["geometry_candidate_count_sum"] += geometry_count
        counters["candidate_geometry_group_sum"] += candidate_groups
        counters["geometry_candidate_cardinality_match_rows"] += int(cardinality_match)
        counters["object_state_exact_rows"] += int(object_state_exact)
        counters["render_group_state_match_rows"] += int(render_group_state_match)
        counters["candidate_source_chain_valid_rows"] += int(candidate_valid)
        counters["payload_object_join_valid_rows"] += int(payload_valid)
        counters["objects_with_multiple_geometry_assignment_models"] += int(assignment_models > 1)
        counters["objects_with_single_geometry_assignment_models"] += int(assignment_models == 1)
        counters["unconstrained_assignment_model_sum"] += assignment_models
        counters["one_to_one_assignment_model_sum"] += one_to_one_models
        counters["unique_state_geometry_candidate_render_binding_proven_rows"] += int(unique_proven)
        counters["valid_object_rows"] += int(row_valid)
        issues = []
        if not global_valid:
            issues.append("global state/geometry input invalid")
        if not runtime_valid:
            issues.append("candidate runtime input invalid")
        if not candidate_valid:
            issues.append("candidate source-chain input invalid")
        if not payload_valid:
            issues.append("payload join input invalid")
        if not cardinality_match:
            issues.append("geometry/group cardinality mismatch")
        report.append({
            "model_archive": key[0],
            "object_name": key[1],
            "shape_name": key[2],
            "geometry_candidate_count": str(geometry_count),
            "candidate_geometry_groups": str(candidate_groups),
            "assignment_model_count": str(assignment_models),
            "one_to_one_assignment_model_count": str(one_to_one_models),
            "object_state_exact": str(object_state_exact),
            "render_group_state": runtime.get("render_group_state", ""),
            "geometry_candidate_cardinality_match": str(cardinality_match),
            "global_input_status_valid": str(global_valid),
            "runtime_input_status_valid": str(runtime_valid),
            "candidate_input_status_valid": str(candidate_valid),
            "payload_join_input_status_valid": str(payload_valid),
            "candidate_source_chain_valid": str(candidate_valid),
            "payload_object_join_valid": str(payload_valid),
            "unique_state_geometry_candidate_render_binding_proven": str(unique_proven),
            "status": STATUS if row_valid else f"inconclusive-{STATUS}",
            "semantic_binding": SEMANTIC_BINDING,
            "original_semantics": ORIGINAL_SEMANTICS,
            "issue": "; ".join(issues),
        })

    all_valid = (
        len(global_rows) == 24
        and len(runtime_rows) == 24
        and len(candidate_rows) == 24
        and len(payload_rows) == 24
        and len(keys) == 24
        and counters["global_input_status_valid_rows"] == 24
        and counters["runtime_input_status_valid_rows"] == 24
        and counters["candidate_input_status_valid_rows"] == 24
        and counters["payload_join_input_status_valid_rows"] == 24
        and counters["geometry_candidate_count_sum"] == 38
        and counters["candidate_geometry_group_sum"] == 38
        and counters["geometry_candidate_cardinality_match_rows"] == 24
        and counters["object_state_exact_rows"] == 24
        and counters["render_group_state_match_rows"] == 0
        and counters["objects_with_multiple_geometry_assignment_models"] == 12
        and counters["objects_with_single_geometry_assignment_models"] == 12
        and counters["unconstrained_assignment_model_sum"] == 36
        and counters["one_to_one_assignment_model_sum"] == 24
        and counters["unique_state_geometry_candidate_render_binding_proven_rows"] == 0
        and counters["valid_object_rows"] == 24
        and all(row["status"] == STATUS for row in report)
    )
    summary = {
        "global_source_report": "",
        "runtime_source_report": "",
        "candidate_source_report": "",
        "payload_join_source_report": "",
        "global_rows": str(len(global_rows)),
        "runtime_rows": str(len(runtime_rows)),
        "candidate_rows": str(len(candidate_rows)),
        "payload_join_rows": str(len(payload_rows)),
        "joined_object_rows": str(len(keys)),
        "missing_runtime_objects": str(len(set(global_index) - set(runtime_index))),
        "missing_candidate_objects": str(len(set(global_index) - set(candidate_index))),
        "missing_payload_join_objects": str(len(set(global_index) - set(payload_index))),
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
    parser.add_argument("runtime_report", type=Path)
    parser.add_argument("candidate_report", type=Path)
    parser.add_argument("payload_join_report", type=Path)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()
    report, summary = analyze(
        _read(args.global_report),
        _read(args.runtime_report),
        _read(args.candidate_report),
        _read(args.payload_join_report),
    )
    summary["global_source_report"] = str(args.global_report)
    summary["runtime_source_report"] = str(args.runtime_report)
    summary["candidate_source_report"] = str(args.candidate_report)
    summary["payload_join_source_report"] = str(args.payload_join_report)
    _write(args.report, REPORT_FIELDS, report)
    _write(args.summary, SUMMARY_FIELDS, [{field: summary[field] for field in SUMMARY_FIELDS}])
    print("FIFA Street 2 independent original state/geometry candidate-render global assignment control")
    for field in SUMMARY_FIELDS:
        print(f"{field}: {summary[field]}")
    print(f"report: {args.report}")
    print(f"summary: {args.summary}")
    return 0 if summary["status"] == STATUS else 1


if __name__ == "__main__":
    raise SystemExit(main())
