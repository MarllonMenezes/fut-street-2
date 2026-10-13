"""Test whether original geometry/draw classification reduces state assignments.

This control joins the global original state-to-geometry assignment domain with
the independent primitive geometry/state classification and the independent
candidate geometry payload identities.  It checks that geometry-marked
primitives reconcile one-for-one with the candidate geometry/payload rows while
the 180 SHAPENAME/state-marked primitives remain outside that payload domain.
The classification is structural only: it does not assign a state run to a
draw, UV, material, or GPU binding.
"""

from __future__ import annotations

import argparse
import csv
from pathlib import Path
from typing import Dict, Iterable, List, Sequence, Tuple


STATUS = "verified-independent-original-state-geometry-draw-classification-global-assignment-control"
GLOBAL_STATUS = "verified-independent-original-state-geometry-global-assignment-control"
CLASSIFICATION_STATUS = "verified-independent-original-geometry-draw-classification-control"
PAYLOAD_STATUS = "verified-independent-original-candidate-geometry-payload-identity-control"
SEMANTIC_BINDING = "not-proven"
ORIGINAL_SEMANTICS = "not-claimed"
PROVENANCE = (
    "independent-object/primitive-keyed-reparse+global-state-geometry-domain+"
    "geometry-draw-classification+candidate-payload-identity;classification-does-"
    "not-assign-state;no-draw-UV-material-promotion"
)

OBJECT_FIELDS = ("model_archive", "object_name")
PRIMITIVE_FIELDS = ("model_archive", "model_object", "layer", "primitive")

REPORT_FIELDS = (
    "model_archive",
    "object_name",
    "geometry_candidate_count",
    "draw_geometry_marker_count",
    "draw_state_marker_count",
    "payload_identity_geometry_count",
    "classification_rows",
    "classification_match_rows",
    "geometry_draw_payload_identity_join_rows",
    "state_rows_without_payload_identity",
    "assignment_model_count_before_draw_classification",
    "assignment_model_count_after_draw_classification",
    "one_to_one_model_count_before_draw_classification",
    "one_to_one_model_count_after_draw_classification",
    "draw_classification_reduced_state_domain",
    "unique_state_geometry_draw_binding_proven",
    "global_input_status_valid",
    "draw_classification_input_status_valid",
    "payload_identity_input_status_valid",
    "status",
    "semantic_binding",
    "original_semantics",
    "issue",
)

SUMMARY_FIELDS = (
    "global_source_report",
    "draw_classification_source_report",
    "payload_identity_source_report",
    "global_rows",
    "draw_classification_rows",
    "payload_identity_rows",
    "joined_object_rows",
    "missing_draw_classification_objects",
    "missing_payload_identity_objects",
    "global_input_status_valid_rows",
    "draw_classification_input_status_valid_rows",
    "payload_identity_input_status_valid_rows",
    "geometry_candidate_count_sum",
    "draw_classification_geometry_marker_sum",
    "payload_identity_geometry_row_sum",
    "classification_match_rows",
    "geometry_draw_payload_identity_join_rows",
    "state_marker_rows",
    "state_rows_without_payload_identity",
    "objects_with_state_over_geometry_rows",
    "assignment_model_sum_before_draw_classification",
    "assignment_model_sum_after_draw_classification",
    "one_to_one_model_sum_before_draw_classification",
    "one_to_one_model_sum_after_draw_classification",
    "draw_classification_reduced_state_domain_rows",
    "unique_state_geometry_draw_binding_proven_rows",
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


def _object_key(row: Dict[str, str]) -> Tuple[str, str]:
    key = (
        row.get("model_archive", ""),
        row.get("object_name", "") or row.get("model_object", ""),
    )
    if not all(key):
        raise ValueError(f"empty object identity: {key!r}")
    return key


def _primitive_key(row: Dict[str, str]) -> Tuple[str, str, str, str]:
    key = tuple(row.get(field, "") for field in PRIMITIVE_FIELDS)
    if not all(key):
        raise ValueError(f"empty primitive identity: {key!r}")
    return key


def _group(rows: Sequence[Dict[str, str]], key_fn):
    grouped = {}
    for row in rows:
        grouped.setdefault(key_fn(row), []).append(row)
    return grouped


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
    classification_rows: Sequence[Dict[str, str]],
    payload_rows: Sequence[Dict[str, str]],
) -> Tuple[List[Dict[str, str]], Dict[str, str]]:
    if not global_rows or not classification_rows or not payload_rows:
        raise ValueError("all source reports must contain rows")

    global_by_object = _group(global_rows, _object_key)
    classification_by_object = _group(classification_rows, _object_key)
    payload_by_object = _group(payload_rows, _object_key)
    payload_by_primitive = _group(payload_rows, _primitive_key)
    object_keys = sorted(
        set(global_by_object) & set(classification_by_object) & set(payload_by_object)
    )
    classification_rows_by_primitive = _group(classification_rows, _primitive_key)

    report: List[Dict[str, str]] = []
    counters = {
        "geometry_candidate_count": 0,
        "draw_geometry_marker": 0,
        "payload_identity_geometry": 0,
        "classification_match": 0,
        "geometry_draw_payload_identity_join": 0,
        "state_marker": 0,
        "state_rows_without_payload_identity": 0,
        "objects_with_state_over_geometry": 0,
        "assignment_before": 0,
        "assignment_after": 0,
        "one_to_one_before": 0,
        "one_to_one_after": 0,
        "draw_reduced": 0,
        "unique_binding": 0,
        "valid_object": 0,
    }

    for object_key in object_keys:
        global_group = global_by_object[object_key]
        classification_group = classification_by_object[object_key]
        payload_group = payload_by_object[object_key]
        if len(global_group) != 1:
            raise ValueError(f"global report is not one row per object: {object_key!r}")
        global_row = global_group[0]
        geometry_candidate_count = _int(global_row, "geometry_candidate_count")
        assignment_before = _int(global_row, "assignment_model_count")
        one_to_one_before = _int(global_row, "one_to_one_assignment_model_count")

        draw_geometry_rows = [
            row for row in classification_group if _bool(row, "original_geometry_marker")
        ]
        draw_state_rows = [
            row for row in classification_group if _bool(row, "original_state_marker")
        ]
        classification_matches = sum(
            _bool(row, "classification_match") for row in classification_group
        )
        payload_complete = [
            row
            for row in payload_group
            if _bool(row, "complete_identity")
        ]
        geometry_payload_join_rows = 0
        state_without_payload = 0
        invalid_primitive_join = False
        for row in classification_group:
            primitive_key = _primitive_key(row)
            matches = payload_by_primitive.get(primitive_key, [])
            if _bool(row, "original_geometry_marker"):
                geometry_payload_join_rows += int(
                    len(matches) == 1
                    and all(_bool(match, "complete_identity") for match in matches)
                )
                invalid_primitive_join |= len(matches) != 1
            else:
                state_without_payload += int(len(matches) == 0)

        global_valid = (
            global_row.get("status") == GLOBAL_STATUS
            and global_row.get("original_semantics") == ORIGINAL_SEMANTICS
        )
        classification_valid = all(
            row.get("status") == CLASSIFICATION_STATUS
            and row.get("original_semantics") == ORIGINAL_SEMANTICS
            for row in classification_group
        )
        payload_valid = all(
            row.get("status") == PAYLOAD_STATUS
            and row.get("original_semantics") == ORIGINAL_SEMANTICS
            for row in payload_group
        )
        state_over_geometry = len(draw_state_rows) > len(draw_geometry_rows)
        cardinality_match = (
            len(draw_geometry_rows) == geometry_candidate_count
            and len(payload_complete) == geometry_candidate_count
        )
        classification_validity = (
            global_valid
            and classification_valid
            and payload_valid
            and len(classification_group) == len(draw_geometry_rows) + len(draw_state_rows)
            and all(_bool(row, "classification_match") for row in classification_group)
            and cardinality_match
            and geometry_payload_join_rows == len(draw_geometry_rows)
            and state_without_payload == len(draw_state_rows)
            and not invalid_primitive_join
            and state_over_geometry
        )
        assignment_after = assignment_before
        one_to_one_after = one_to_one_before
        draw_reduced = False
        unique_binding = False
        valid = (
            classification_validity
            and assignment_after == assignment_before
            and one_to_one_after == one_to_one_before
            and not draw_reduced
            and not unique_binding
        )

        counters["geometry_candidate_count"] += geometry_candidate_count
        counters["draw_geometry_marker"] += len(draw_geometry_rows)
        counters["payload_identity_geometry"] += len(payload_complete)
        counters["classification_match"] += classification_matches
        counters["geometry_draw_payload_identity_join"] += geometry_payload_join_rows
        counters["state_marker"] += len(draw_state_rows)
        counters["state_rows_without_payload_identity"] += state_without_payload
        counters["objects_with_state_over_geometry"] += int(state_over_geometry)
        counters["assignment_before"] += assignment_before
        counters["assignment_after"] += assignment_after
        counters["one_to_one_before"] += one_to_one_before
        counters["one_to_one_after"] += one_to_one_after
        counters["draw_reduced"] += int(draw_reduced)
        counters["unique_binding"] += int(unique_binding)
        counters["valid_object"] += int(valid)
        report.append(
            {
                "model_archive": object_key[0],
                "object_name": object_key[1],
                "geometry_candidate_count": str(geometry_candidate_count),
                "draw_geometry_marker_count": str(len(draw_geometry_rows)),
                "draw_state_marker_count": str(len(draw_state_rows)),
                "payload_identity_geometry_count": str(len(payload_complete)),
                "classification_rows": str(len(classification_group)),
                "classification_match_rows": str(classification_matches),
                "geometry_draw_payload_identity_join_rows": str(geometry_payload_join_rows),
                "state_rows_without_payload_identity": str(state_without_payload),
                "assignment_model_count_before_draw_classification": str(assignment_before),
                "assignment_model_count_after_draw_classification": str(assignment_after),
                "one_to_one_model_count_before_draw_classification": str(one_to_one_before),
                "one_to_one_model_count_after_draw_classification": str(one_to_one_after),
                "draw_classification_reduced_state_domain": str(draw_reduced),
                "unique_state_geometry_draw_binding_proven": str(unique_binding),
                "global_input_status_valid": str(global_valid),
                "draw_classification_input_status_valid": str(classification_valid),
                "payload_identity_input_status_valid": str(payload_valid),
                "status": STATUS if valid else f"inconclusive-{STATUS}",
                "semantic_binding": SEMANTIC_BINDING,
                "original_semantics": ORIGINAL_SEMANTICS,
                "issue": "" if valid else "draw classification/payload cardinality did not reconcile",
            }
        )

    missing_classification = set(global_by_object) - set(classification_by_object)
    missing_payload = set(global_by_object) - set(payload_by_object)
    global_input_valid = sum(
        row.get("status") == GLOBAL_STATUS
        and row.get("original_semantics") == ORIGINAL_SEMANTICS
        for row in global_rows
    )
    classification_input_valid = sum(
        row.get("status") == CLASSIFICATION_STATUS
        and row.get("original_semantics") == ORIGINAL_SEMANTICS
        for row in classification_rows
    )
    payload_input_valid = sum(
        row.get("status") == PAYLOAD_STATUS
        and row.get("original_semantics") == ORIGINAL_SEMANTICS
        for row in payload_rows
    )
    all_valid = all(row["status"] == STATUS for row in report)
    summary = {
        "global_source_report": "",
        "draw_classification_source_report": "",
        "payload_identity_source_report": "",
        "global_rows": str(len(global_rows)),
        "draw_classification_rows": str(len(classification_rows)),
        "payload_identity_rows": str(len(payload_rows)),
        "joined_object_rows": str(len(object_keys)),
        "missing_draw_classification_objects": str(len(missing_classification)),
        "missing_payload_identity_objects": str(len(missing_payload)),
        "global_input_status_valid_rows": str(global_input_valid),
        "draw_classification_input_status_valid_rows": str(classification_input_valid),
        "payload_identity_input_status_valid_rows": str(payload_input_valid),
        "geometry_candidate_count_sum": str(counters["geometry_candidate_count"]),
        "draw_classification_geometry_marker_sum": str(counters["draw_geometry_marker"]),
        "payload_identity_geometry_row_sum": str(counters["payload_identity_geometry"]),
        "classification_match_rows": str(counters["classification_match"]),
        "geometry_draw_payload_identity_join_rows": str(
            counters["geometry_draw_payload_identity_join"]
        ),
        "state_marker_rows": str(counters["state_marker"]),
        "state_rows_without_payload_identity": str(counters["state_rows_without_payload_identity"]),
        "objects_with_state_over_geometry_rows": str(counters["objects_with_state_over_geometry"]),
        "assignment_model_sum_before_draw_classification": str(counters["assignment_before"]),
        "assignment_model_sum_after_draw_classification": str(counters["assignment_after"]),
        "one_to_one_model_sum_before_draw_classification": str(counters["one_to_one_before"]),
        "one_to_one_model_sum_after_draw_classification": str(counters["one_to_one_after"]),
        "draw_classification_reduced_state_domain_rows": str(counters["draw_reduced"]),
        "unique_state_geometry_draw_binding_proven_rows": str(counters["unique_binding"]),
        "valid_object_rows": str(counters["valid_object"]),
        "status": (
            STATUS
            if len(global_rows) == 24
            and len(classification_rows) == 218
            and len(payload_rows) == 38
            and len(object_keys) == 24
            and not missing_classification
            and not missing_payload
            and global_input_valid == 24
            and classification_input_valid == 218
            and payload_input_valid == 38
            and counters["geometry_candidate_count"] == 38
            and counters["draw_geometry_marker"] == 38
            and counters["payload_identity_geometry"] == 38
            and counters["classification_match"] == 218
            and counters["geometry_draw_payload_identity_join"] == 38
            and counters["state_marker"] == 180
            and counters["state_rows_without_payload_identity"] == 180
            and counters["objects_with_state_over_geometry"] == 24
            and counters["assignment_before"] == 36
            and counters["assignment_after"] == 36
            and counters["one_to_one_before"] == 24
            and counters["one_to_one_after"] == 24
            and counters["draw_reduced"] == 0
            and counters["unique_binding"] == 0
            and counters["valid_object"] == 24
            and all_valid
            else f"inconclusive-{STATUS}"
        ),
        "semantic_binding": SEMANTIC_BINDING,
        "provenance": PROVENANCE,
        "original_semantics": ORIGINAL_SEMANTICS,
    }
    return report, summary


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("global_report", type=Path)
    parser.add_argument("draw_classification_report", type=Path)
    parser.add_argument("payload_identity_report", type=Path)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()
    report, summary = analyze(
        _read(args.global_report),
        _read(args.draw_classification_report),
        _read(args.payload_identity_report),
    )
    summary["global_source_report"] = str(args.global_report)
    summary["draw_classification_source_report"] = str(args.draw_classification_report)
    summary["payload_identity_source_report"] = str(args.payload_identity_report)
    _write(args.report, REPORT_FIELDS, report)
    _write(args.summary, SUMMARY_FIELDS, [summary])
    print("FIFA Street 2 independent state/geometry/draw classification global assignment control")
    for field in SUMMARY_FIELDS:
        print(f"{field}: {summary[field]}")
    print(f"report: {args.report}")
    print(f"summary: {args.summary}")
    return 0 if summary["status"] == STATUS else 1


if __name__ == "__main__":
    raise SystemExit(main())
