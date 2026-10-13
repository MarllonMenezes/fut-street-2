"""Independently validate original geometry/draw classification markers.

This control joins the bounded original render-chain report with the bounded
candidate fetch report for the 24 same-trunk candidate objects.  It checks
only the observed structural partition: rows carrying the original
``SetPrimitiveType`` marker and a complete five-slot fetch sequence coincide
with candidate mesh groups, while SHAPENAME-only rows do not.  It does not
assign a state run to a draw, or interpret any slot as position, UV, material,
skinning, rigging, animation, morph, command or GPU state.
"""

from __future__ import annotations

import argparse
import csv
from pathlib import Path
from typing import Dict, Iterable, List, Sequence, Tuple


STATUS = "verified-independent-original-geometry-draw-classification-control"
RENDER_STATUS = "verified-bounded-original-render-chain-structure"
CANDIDATE_STATUS = "verified-bounded-candidate-fetch-record-association"
CANDIDATE_INCOMPLETE_STATUS = "opaque-incomplete-fetch-record-association"
SEMANTIC_BINDING = "not-proven"
ORIGINAL_SEMANTICS = "not-claimed"
PROVENANCE = (
    "independent-TSV-primitive-key-join+original-primitive-type/SHAPENAME-"
    "partition+candidate-mesh-group-reparse;classification-only;no-state-"
    "draw-or-UV-material-promotion"
)

KEY_FIELDS = ("model_archive", "model_object", "layer", "primitive")

REPORT_FIELDS = (
    *KEY_FIELDS,
    "original_geometry_marker",
    "original_state_marker",
    "original_complete_five_slot_fetch",
    "candidate_mesh_group_count",
    "candidate_geometry_marker",
    "candidate_primitive_type",
    "classification_match",
    "state_to_geometry_unique_binding_proven",
    "original_status_valid",
    "candidate_status_valid",
    "status",
    "semantic_binding",
    "original_semantics",
    "issue",
)

SUMMARY_FIELDS = (
    "original_source_report",
    "candidate_source_report",
    "candidate_object_rows",
    "original_scoped_rows",
    "candidate_rows",
    "joined_rows",
    "missing_original_keys",
    "missing_candidate_keys",
    "joined_object_rows",
    "objects_with_complete_classification",
    "original_input_status_valid_rows",
    "candidate_input_status_valid_rows",
    "original_geometry_marker_rows",
    "original_state_marker_rows",
    "candidate_geometry_marker_rows",
    "candidate_state_marker_rows",
    "classification_match_rows",
    "state_count_greater_than_geometry_rows",
    "state_to_geometry_unique_binding_proven_rows",
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


def _key(row: Dict[str, str]) -> Tuple[str, str, str, str]:
    values = tuple(row.get(field, "") for field in KEY_FIELDS)
    if not all(values):
        raise ValueError(f"empty primitive identity: {values!r}")
    return values


def _object_key(row: Dict[str, str]) -> Tuple[str, str]:
    return row.get("model_archive", ""), row.get("model_object", "")


def _group(rows: Sequence[Dict[str, str]]) -> Dict[Tuple[str, str, str, str], List[Dict[str, str]]]:
    grouped: Dict[Tuple[str, str, str, str], List[Dict[str, str]]] = {}
    for row in rows:
        grouped.setdefault(_key(row), []).append(row)
    return grouped


def _bool(row: Dict[str, str], field: str) -> bool:
    value = row.get(field, "")
    if value not in {"True", "False"}:
        raise ValueError(f"invalid boolean in {field}: {value!r}")
    return value == "True"


def _int(row: Dict[str, str], field: str) -> int:
    try:
        return int(row.get(field, ""), 0)
    except (TypeError, ValueError) as exc:
        raise ValueError(f"invalid integer in {field}: {row.get(field)!r}") from exc


def analyze(
    original_rows: Sequence[Dict[str, str]],
    candidate_rows: Sequence[Dict[str, str]],
) -> Tuple[List[Dict[str, str]], Dict[str, str]]:
    if not original_rows or not candidate_rows:
        raise ValueError("both source reports must contain rows")

    original_by_key = _group(original_rows)
    candidate_by_key = _group(candidate_rows)
    candidate_object_keys = {_object_key(row) for row in candidate_rows}
    scoped_original = [
        row for row in original_rows if _object_key(row) in candidate_object_keys
    ]
    scoped_original_by_key = _group(scoped_original)
    original_keys = set(scoped_original_by_key)
    candidate_keys = set(candidate_by_key)
    joined_keys = sorted(original_keys & candidate_keys)

    report: List[Dict[str, str]] = []
    object_rows: Dict[Tuple[str, str], List[Dict[str, str]]] = {}
    counters = {
        "original_geometry_marker": 0,
        "original_state_marker": 0,
        "candidate_geometry_marker": 0,
        "candidate_state_marker": 0,
        "classification_match": 0,
        "state_to_geometry_unique_binding_proven": 0,
    }

    for key in joined_keys:
        original_group = scoped_original_by_key[key]
        candidate_group = candidate_by_key[key]
        if len(original_group) != 1 or len(candidate_group) != 1:
            raise ValueError(f"primitive key is not unique: {key!r}")
        original = original_group[0]
        candidate = candidate_group[0]
        original_status_valid = (
            original.get("status") == RENDER_STATUS
            and original.get("original_semantics") == ORIGINAL_SEMANTICS
        )
        candidate_status_valid = (
            candidate.get("status") in {CANDIDATE_STATUS, CANDIDATE_INCOMPLETE_STATUS}
            and candidate.get("original_semantics") == ORIGINAL_SEMANTICS
        )
        primitive_type = _bool(original, "has_primitive_type_assignment")
        shapename = _bool(original, "has_shapename_assignment")
        complete_fetch = _bool(original, "complete_five_slot_fetch_sequence")
        if primitive_type and shapename:
            raise ValueError(f"original marker overlap: {key!r}")
        original_geometry = primitive_type and complete_fetch
        original_state = shapename and not primitive_type
        mesh_group_count = _int(candidate, "mesh_group_count")
        candidate_geometry = mesh_group_count > 0
        candidate_state = not candidate_geometry
        classification_match = original_geometry == candidate_geometry
        unique_binding = False
        counters["original_geometry_marker"] += int(original_geometry)
        counters["original_state_marker"] += int(original_state)
        counters["candidate_geometry_marker"] += int(candidate_geometry)
        counters["candidate_state_marker"] += int(candidate_state)
        counters["classification_match"] += int(classification_match)
        counters["state_to_geometry_unique_binding_proven"] += int(unique_binding)
        object_rows.setdefault(_object_key(original), []).append(
            {
                "original_geometry": str(original_geometry),
                "original_state": str(original_state),
                "candidate_geometry": str(candidate_geometry),
            }
        )
        valid = (
            original_status_valid
            and candidate_status_valid
            and (original_geometry or original_state)
            and (candidate_geometry or candidate_state)
            and classification_match
        )
        report.append(
            {
                **dict(zip(KEY_FIELDS, key)),
                "original_geometry_marker": str(original_geometry),
                "original_state_marker": str(original_state),
                "original_complete_five_slot_fetch": str(complete_fetch),
                "candidate_mesh_group_count": str(mesh_group_count),
                "candidate_geometry_marker": str(candidate_geometry),
                "candidate_primitive_type": candidate.get("primitive_type", ""),
                "classification_match": str(classification_match),
                "state_to_geometry_unique_binding_proven": str(unique_binding),
                "original_status_valid": str(original_status_valid),
                "candidate_status_valid": str(candidate_status_valid),
                "status": STATUS if valid else "inconclusive-" + STATUS,
                "semantic_binding": SEMANTIC_BINDING,
                "original_semantics": ORIGINAL_SEMANTICS,
                "issue": "" if valid else "original/candidate structural classification did not reconcile",
            }
        )

    complete_object_rows = 0
    state_count_greater_than_geometry_rows = 0
    for rows in object_rows.values():
        geometry_count = sum(row["original_geometry"] == "True" for row in rows)
        state_count = sum(row["original_state"] == "True" for row in rows)
        complete_object_rows += int(
            geometry_count > 0
            and state_count > geometry_count
            and all(row["original_geometry"] == row["candidate_geometry"] for row in rows)
        )
        state_count_greater_than_geometry_rows += int(state_count > geometry_count)

    original_input_valid_rows = sum(
        row.get("status") == RENDER_STATUS
        and row.get("original_semantics") == ORIGINAL_SEMANTICS
        for row in scoped_original
    )
    candidate_input_valid_rows = sum(
        row.get("status") in {CANDIDATE_STATUS, CANDIDATE_INCOMPLETE_STATUS}
        and row.get("original_semantics") == ORIGINAL_SEMANTICS
        for row in candidate_rows
    )
    all_rows_valid = all(row["status"] == STATUS for row in report)
    summary = {
        "original_source_report": "",
        "candidate_source_report": "",
        "candidate_object_rows": str(len(candidate_object_keys)),
        "original_scoped_rows": str(len(scoped_original)),
        "candidate_rows": str(len(candidate_rows)),
        "joined_rows": str(len(joined_keys)),
        "missing_original_keys": str(len(candidate_keys - original_keys)),
        "missing_candidate_keys": str(len(original_keys - candidate_keys)),
        "joined_object_rows": str(len(object_rows)),
        "objects_with_complete_classification": str(complete_object_rows),
        "original_input_status_valid_rows": str(original_input_valid_rows),
        "candidate_input_status_valid_rows": str(candidate_input_valid_rows),
        "original_geometry_marker_rows": str(counters["original_geometry_marker"]),
        "original_state_marker_rows": str(counters["original_state_marker"]),
        "candidate_geometry_marker_rows": str(counters["candidate_geometry_marker"]),
        "candidate_state_marker_rows": str(counters["candidate_state_marker"]),
        "classification_match_rows": str(counters["classification_match"]),
        "state_count_greater_than_geometry_rows": str(state_count_greater_than_geometry_rows),
        "state_to_geometry_unique_binding_proven_rows": str(
            counters["state_to_geometry_unique_binding_proven"]
        ),
        "status": (
            STATUS
            if len(candidate_object_keys) == 24
            and len(scoped_original) == 218
            and len(candidate_rows) == 218
            and len(joined_keys) == 218
            and len(object_rows) == 24
            and complete_object_rows == 24
            and original_input_valid_rows == 218
            and candidate_input_valid_rows == 218
            and counters["original_geometry_marker"] == 38
            and counters["original_state_marker"] == 180
            and counters["candidate_geometry_marker"] == 38
            and counters["candidate_state_marker"] == 180
            and counters["classification_match"] == 218
            and state_count_greater_than_geometry_rows == 24
            and counters["state_to_geometry_unique_binding_proven"] == 0
            and all_rows_valid
            else "inconclusive-" + STATUS
        ),
        "semantic_binding": SEMANTIC_BINDING,
        "provenance": PROVENANCE,
        "original_semantics": ORIGINAL_SEMANTICS,
    }
    return report, summary


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("original_report", type=Path)
    parser.add_argument("candidate_report", type=Path)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()
    report, summary = analyze(_read(args.original_report), _read(args.candidate_report))
    summary["original_source_report"] = str(args.original_report)
    summary["candidate_source_report"] = str(args.candidate_report)
    _write(args.report, REPORT_FIELDS, report)
    _write(args.summary, SUMMARY_FIELDS, [summary])
    print(
        "status="
        f"{summary['status']} candidate_object_rows={summary['candidate_object_rows']} "
        f"joined_rows={summary['joined_rows']} geometry_rows={summary['original_geometry_marker_rows']} "
        f"state_rows={summary['original_state_marker_rows']}"
    )
    print(f"report: {args.report}")
    print(f"summary: {args.summary}")
    return 0 if summary["status"] == STATUS else 1


if __name__ == "__main__":
    raise SystemExit(main())
