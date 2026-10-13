"""Join original state-token rows to candidate attribute/raster reports.

This control verifies object-key identity and the independent structural fields
already emitted by the three source audits.  It deliberately does not assign
the candidate slot-1 values to UVs, materials, geometry, skinning, rigging,
animation, morphs, commands, or GPU behavior, and it does not claim semantic
absence from opaque original data.
"""

from __future__ import annotations

import argparse
import csv
from pathlib import Path
from typing import Dict, Iterable, List, Sequence, Tuple


STATUS = "verified-independent-original-state-candidate-attribute-control"
ORIGINAL_SEMANTICS = "not-claimed"
PROVENANCE = (
    "independent-object-key-join+state-token-negative-reparse+candidate-attribute/"
    "raster-structural-crosscheck;no-slot1-or-raster-semantic-promotion"
)

REPORT_FIELDS = (
    "model_archive",
    "model_object",
    "state_rows",
    "state_valid_rows",
    "state_negative_token_rows",
    "attribute_present",
    "attribute_status",
    "attribute_complete",
    "attribute_finite",
    "attribute_near_unit",
    "influence_present",
    "influence_status",
    "influence_geometry_finite_and_indexed",
    "influence_texture_influences_raster",
    "influence_mesh_shape_state_exact",
    "identity_join",
    "status",
    "provenance",
    "original_semantics",
    "issue",
)

SUMMARY_FIELDS = (
    "state_source_report",
    "attribute_source_report",
    "influence_source_report",
    "candidate_object_rows",
    "state_object_rows",
    "influence_object_rows",
    "joined_object_rows",
    "missing_state_objects",
    "missing_attribute_objects",
    "missing_influence_objects",
    "state_rows_joined",
    "state_valid_rows",
    "state_negative_token_rows",
    "attribute_complete_rows",
    "attribute_finite_rows",
    "attribute_near_unit_rows",
    "influence_geometry_rows",
    "influence_status_rows",
    "influence_texture_rows",
    "influence_mesh_shape_state_exact_rows",
    "status",
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


def _key(row: Dict[str, str]) -> Tuple[str, str]:
    return row.get("model_archive", ""), row.get("model_object", "")


def _true(row: Dict[str, str], field: str) -> bool:
    return row.get(field, "").strip().lower() == "true"


def _state_negative(row: Dict[str, str]) -> bool:
    return all(
        row.get(field, "") == "0"
        for field in (
            "texture_token_hits",
            "material_token_hits",
            "uv_coordinate_token_hits",
            "vertex_attribute_token_hits",
            "deformation_token_hits",
        )
    )


def analyze(
    state_rows: Sequence[Dict[str, str]],
    attribute_rows: Sequence[Dict[str, str]],
    influence_rows: Sequence[Dict[str, str]],
) -> Tuple[List[Dict[str, str]], Dict[str, str]]:
    attributes = {_key(row): row for row in attribute_rows}
    influences = {
        _key(row): row
        for row in influence_rows
        if row.get("kind", "").strip().lower() == "candidate"
    }
    state_by_key: Dict[Tuple[str, str], List[Dict[str, str]]] = {}
    for row in state_rows:
        state_by_key.setdefault(_key(row), []).append(row)

    candidate_keys = sorted(set(attributes) | set(influences))
    report: List[Dict[str, str]] = []
    counters = {
        "state_valid_rows": 0,
        "state_negative_token_rows": 0,
        "attribute_complete_rows": 0,
        "attribute_finite_rows": 0,
        "attribute_near_unit_rows": 0,
        "influence_geometry_rows": 0,
        "influence_status_rows": 0,
        "influence_texture_rows": 0,
        "influence_mesh_shape_state_exact_rows": 0,
    }

    for key in candidate_keys:
        archive, obj = key
        original = state_by_key.get(key, [])
        attribute = attributes.get(key)
        influence = influences.get(key)
        state_valid = [
            row
            for row in original
            if row.get("status") == "verified-bounded-original-state-token-negative-control"
        ]
        state_negative = [row for row in original if _state_negative(row)]
        attribute_complete = attribute is not None and attribute.get("status") == "complete-slot1-profile" and int(attribute.get("slot1_complete_groups", "0")) > 0
        attribute_finite = attribute is not None and int(attribute.get("slot1_finite_groups", "0")) > 0
        attribute_near_unit = attribute is not None and int(attribute.get("slot1_near_unit_groups", "0")) > 0
        influence_geometry = influence is not None and _true(influence, "geometry_finite_and_indexed")
        influence_status = influence is not None and influence.get("status") in {
            "verified-candidate-raster-influence",
            "observed-candidate-raster-unchanged",
        }
        influence_texture = influence is not None and _true(influence, "candidate_texture_influences_raster")
        influence_mesh_exact = influence is not None and _true(influence, "mesh_shape_state_exact")
        identity_join = bool(original and attribute is not None and influence is not None)
        row_valid = (
            identity_join
            and len(state_valid) == len(original)
            and len(state_negative) == len(original)
            and attribute_complete
            and attribute_finite
            and attribute_near_unit
            and influence_geometry
            and influence_status
            and not influence_mesh_exact
            and original
            and influence.get("original_semantics") == ORIGINAL_SEMANTICS
            and all(row.get("original_semantics") == ORIGINAL_SEMANTICS for row in original)
        )
        counters["state_valid_rows"] += len(state_valid)
        counters["state_negative_token_rows"] += len(state_negative)
        counters["attribute_complete_rows"] += int(attribute_complete)
        counters["attribute_finite_rows"] += int(attribute_finite)
        counters["attribute_near_unit_rows"] += int(attribute_near_unit)
        counters["influence_geometry_rows"] += int(influence_geometry)
        counters["influence_status_rows"] += int(influence_status)
        counters["influence_texture_rows"] += int(influence_texture)
        counters["influence_mesh_shape_state_exact_rows"] += int(influence_mesh_exact)
        row_status = STATUS if row_valid else "inconclusive-original-state-candidate-attribute-control"
        report.append(
            {
                "model_archive": archive,
                "model_object": obj,
                "state_rows": str(len(original)),
                "state_valid_rows": str(len(state_valid)),
                "state_negative_token_rows": str(len(state_negative)),
                "attribute_present": str(attribute is not None),
                "attribute_status": attribute.get("status", "") if attribute else "",
                "attribute_complete": str(attribute_complete),
                "attribute_finite": str(attribute_finite),
                "attribute_near_unit": str(attribute_near_unit),
                "influence_present": str(influence is not None),
                "influence_status": influence.get("status", "") if influence else "",
                "influence_geometry_finite_and_indexed": str(influence_geometry),
                "influence_texture_influences_raster": str(influence_texture),
                "influence_mesh_shape_state_exact": str(influence_mesh_exact),
                "identity_join": str(identity_join),
                "status": row_status,
                "provenance": PROVENANCE,
                "original_semantics": ORIGINAL_SEMANTICS,
                "issue": "" if row_status == STATUS else "state, attribute and influence controls did not reconcile",
            }
        )

    state_object_keys = set(state_by_key)
    summary = {
        "state_source_report": "",
        "attribute_source_report": "",
        "influence_source_report": "",
        "candidate_object_rows": str(len(candidate_keys)),
        "state_object_rows": str(len(state_object_keys)),
        "influence_object_rows": str(len(influences)),
        "joined_object_rows": str(sum(1 for row in report if row["identity_join"] == "True")),
        "missing_state_objects": str(sum(1 for key in candidate_keys if key not in state_by_key)),
        "missing_attribute_objects": str(sum(1 for key in candidate_keys if key not in attributes)),
        "missing_influence_objects": str(sum(1 for key in candidate_keys if key not in influences)),
        "state_rows_joined": str(sum(int(row["state_rows"]) for row in report)),
        "state_valid_rows": str(counters["state_valid_rows"]),
        "state_negative_token_rows": str(counters["state_negative_token_rows"]),
        "attribute_complete_rows": str(counters["attribute_complete_rows"]),
        "attribute_finite_rows": str(counters["attribute_finite_rows"]),
        "attribute_near_unit_rows": str(counters["attribute_near_unit_rows"]),
        "influence_geometry_rows": str(counters["influence_geometry_rows"]),
        "influence_status_rows": str(counters["influence_status_rows"]),
        "influence_texture_rows": str(counters["influence_texture_rows"]),
        "influence_mesh_shape_state_exact_rows": str(counters["influence_mesh_shape_state_exact_rows"]),
        "status": STATUS if (
            len(candidate_keys) == 24
            and len(attributes) == len(influences) == len(candidate_keys)
            and len(state_object_keys) >= len(candidate_keys)
            and int(sum(1 for key in candidate_keys if key in state_by_key)) == len(candidate_keys)
            and all(int(row["state_rows"]) > 0 for row in report)
            and counters["state_valid_rows"] == counters["state_negative_token_rows"] == sum(int(row["state_rows"]) for row in report)
            and counters["attribute_complete_rows"] == counters["attribute_finite_rows"] == counters["attribute_near_unit_rows"] == len(candidate_keys)
            and counters["influence_geometry_rows"] == counters["influence_status_rows"] == len(candidate_keys)
            and counters["influence_mesh_shape_state_exact_rows"] == 0
            and all(row["status"] == STATUS for row in report)
        ) else "inconclusive-original-state-candidate-attribute-control",
        "provenance": PROVENANCE,
        "original_semantics": ORIGINAL_SEMANTICS,
    }
    return report, summary


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("state_report", type=Path)
    parser.add_argument("attribute_report", type=Path)
    parser.add_argument("influence_report", type=Path)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()
    report, summary = analyze(_read(args.state_report), _read(args.attribute_report), _read(args.influence_report))
    summary["state_source_report"] = str(args.state_report)
    summary["attribute_source_report"] = str(args.attribute_report)
    summary["influence_source_report"] = str(args.influence_report)
    _write(args.report, REPORT_FIELDS, report)
    _write(args.summary, SUMMARY_FIELDS, [summary])
    print("FIFA Street 2 independent original state/candidate attribute control")
    for field in SUMMARY_FIELDS:
        print(f"{field}: {summary[field]}")
    print(f"report: {args.report}")
    print(f"summary: {args.summary}")
    return 0 if summary["status"] == STATUS else 1


if __name__ == "__main__":
    raise SystemExit(main())
