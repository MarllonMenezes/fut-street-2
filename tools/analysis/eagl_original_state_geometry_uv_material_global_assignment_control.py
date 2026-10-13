"""Reconcile global state/geometry, candidate UV-role and material candidates.

This control intentionally enumerates only a structural cross-product.  The
geometry model count comes from the independent state/geometry assignment
control; the UV-role and material domains come from independent candidate
reports.  A singleton candidate slot or a singleton same-object asset source
is not promoted to UV/material semantics, and no state-to-draw ownership is
inferred.
"""

from __future__ import annotations

import argparse
import csv
from pathlib import Path
from typing import Dict, Iterable, List, Sequence, Tuple


STATUS = "verified-independent-original-state-geometry-uv-material-global-assignment-control"
SEMANTIC_BINDING = "not-proven"
ORIGINAL_SEMANTICS = "not-claimed"
PROVENANCE = (
    "independent-TSV-keyed-join+global-state-geometry-model-reparse+candidate-"
    "slot1-role-and-material-source-reconciliation+structural-cross-product;"
    "no-state-draw-or-UV-material-promotion"
)

REPORT_FIELDS = (
    "model_archive",
    "object_name",
    "geometry_assignment_models",
    "one_to_one_assignment_models",
    "nonempty_interval_count",
    "ambiguous_interval_count",
    "geometry_assignment_domain_size",
    "uv_joined_primitive_groups",
    "uv_complete_five_slot_groups",
    "uv_incomplete_five_slot_groups",
    "uv_slot1_near_unit_groups",
    "uv_other_slot_near_unit_groups",
    "uv_candidate_role_domain_size",
    "material_state_rows_joined",
    "material_state_rows_with_candidate_asset_source",
    "material_candidate_asset_valid",
    "material_candidate_domain_size",
    "structural_cross_product_model_count",
    "structural_cross_product_singleton",
    "global_input_status_valid",
    "unique_state_geometry_uv_material_binding_proven",
    "status",
    "semantic_binding",
    "original_semantics",
    "issue",
)

SUMMARY_FIELDS = (
    "global_source_report",
    "uv_source_report",
    "material_source_report",
    "global_rows",
    "uv_rows",
    "material_rows",
    "joined_object_rows",
    "missing_uv_objects",
    "missing_material_objects",
    "global_input_status_valid_rows",
    "uv_candidate_attribute_valid_rows",
    "uv_candidate_raster_influence_valid_rows",
    "uv_complete_group_rows",
    "uv_incomplete_group_rows",
    "uv_slot1_near_unit_group_rows",
    "uv_other_slot_near_unit_group_rows",
    "material_input_status_valid_rows",
    "material_candidate_asset_valid_rows",
    "material_state_candidate_source_complete_rows",
    "objects_with_multiple_geometry_assignment_models",
    "objects_with_single_geometry_assignment_model",
    "objects_with_single_candidate_uv_role_domain",
    "objects_with_single_candidate_material_domain",
    "structural_cross_product_model_sum",
    "objects_with_multiple_structural_cross_product_models",
    "objects_with_single_structural_cross_product_model",
    "unique_state_geometry_uv_material_binding_proven_rows",
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


def _key(row: Dict[str, str]) -> Tuple[str, str]:
    key = (row.get("model_archive", ""), row.get("object_name", row.get("model_object", "")))
    if not all(key):
        raise ValueError(f"empty object identity: {key!r}")
    return key


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
    uv_rows: Sequence[Dict[str, str]],
    material_rows: Sequence[Dict[str, str]],
) -> Tuple[List[Dict[str, str]], Dict[str, str]]:
    if not global_rows or not uv_rows or not material_rows:
        raise ValueError("all three source reports must contain rows")

    global_by_key = {_key(row): row for row in global_rows}
    uv_by_key = {_key(row): row for row in uv_rows}
    material_groups: Dict[Tuple[str, str], List[Dict[str, str]]] = {}
    for row in material_rows:
        material_groups.setdefault(_key(row), []).append(row)
    if len(global_by_key) != len(global_rows):
        raise ValueError("global source has duplicate object keys")
    if len(uv_by_key) != len(uv_rows):
        raise ValueError("UV source has duplicate object keys")

    keys = sorted(set(global_by_key) & set(uv_by_key) & set(material_groups))
    missing_uv = set(global_by_key) - set(uv_by_key)
    missing_material = set(global_by_key) - set(material_groups)

    rows: List[Dict[str, str]] = []
    counters = {
        "global_input_status_valid_rows": 0,
        "uv_candidate_attribute_valid_rows": 0,
        "uv_candidate_raster_influence_valid_rows": 0,
        "uv_complete_group_rows": 0,
        "uv_incomplete_group_rows": 0,
        "uv_slot1_near_unit_group_rows": 0,
        "uv_other_slot_near_unit_group_rows": 0,
        "material_input_status_valid_rows": 0,
        "material_candidate_asset_valid_rows": 0,
        "material_state_candidate_source_complete_rows": 0,
        "objects_with_multiple_geometry_assignment_models": 0,
        "objects_with_single_geometry_assignment_model": 0,
        "objects_with_single_candidate_uv_role_domain": 0,
        "objects_with_single_candidate_material_domain": 0,
        "structural_cross_product_model_sum": 0,
        "objects_with_multiple_structural_cross_product_models": 0,
        "objects_with_single_structural_cross_product_model": 0,
        "unique_state_geometry_uv_material_binding_proven_rows": 0,
    }

    for key in keys:
        global_row = global_by_key[key]
        uv_row = uv_by_key[key]
        material_object_rows = material_groups[key]
        material_row = material_object_rows[0]

        geometry_models = _int(global_row, "assignment_model_count")
        one_to_one_models = _int(global_row, "one_to_one_assignment_model_count")
        if geometry_models <= 0 or one_to_one_models <= 0:
            raise ValueError(f"non-positive geometry model count for {key!r}")

        global_valid = (
            global_row.get("status") == "verified-independent-original-state-geometry-global-assignment-control"
            and global_row.get("semantic_binding") == SEMANTIC_BINDING
            and global_row.get("original_semantics") == ORIGINAL_SEMANTICS
            and _bool(global_row, "order_status_valid")
            and _bool(global_row, "partition_status_valid")
            and _bool(global_row, "state_index_set_reconciled")
            and not _bool(global_row, "unique_state_geometry_assignment_proven")
        )
        uv_valid = (
            uv_row.get("status") == "verified-independent-original-state-slot1-candidate-uv-ambiguity-control"
            and uv_row.get("semantic_binding") == SEMANTIC_BINDING
            and uv_row.get("original_semantics") == ORIGINAL_SEMANTICS
            and _bool(uv_row, "candidate_attribute_texture_valid")
            and _bool(uv_row, "candidate_raster_influence_valid")
        )
        material_valid = all(
            row.get("status") == "verified-independent-original-state-candidate-material-ambiguity-control"
            and row.get("semantic_binding") == SEMANTIC_BINDING
            and row.get("original_semantics") == ORIGINAL_SEMANTICS
            and _bool(row, "candidate_asset_valid")
            for row in material_object_rows
        )

        complete_groups = _int(uv_row, "complete_five_slot_groups")
        incomplete_groups = _int(uv_row, "incomplete_five_slot_groups")
        slot1_groups = _int(uv_row, "complete_groups_slot1_near_unit_groups")
        other_slot_groups = _int(uv_row, "complete_groups_other_slot_near_unit_groups")
        joined_groups = _int(uv_row, "joined_primitive_groups")
        state_rows = sum(_int(row, "state_rows_joined") for row in material_object_rows)
        state_source_rows = sum(
            _int(row, "state_rows_with_candidate_asset_source") for row in material_object_rows
        )

        # This is a candidate-role domain only.  It never means UV semantics.
        uv_role_domain = int(
            uv_valid
            and complete_groups > 0
            and slot1_groups == complete_groups
            and other_slot_groups == 0
        )
        # The object-level same-trunk source is one candidate asset, but it is
        # lexical/source-chain evidence only, never a material binding.
        material_domain = int(material_valid and state_rows > 0 and state_source_rows == state_rows)
        structural_models = geometry_models * uv_role_domain * material_domain
        structural_singleton = structural_models == 1
        unique_binding_proven = False
        valid = global_valid and uv_valid and material_valid and uv_role_domain == 1 and material_domain == 1

        counters["global_input_status_valid_rows"] += int(global_valid)
        counters["uv_candidate_attribute_valid_rows"] += int(uv_valid)
        counters["uv_candidate_raster_influence_valid_rows"] += int(
            uv_valid and _bool(uv_row, "candidate_raster_influence_valid")
        )
        counters["uv_complete_group_rows"] += complete_groups
        counters["uv_incomplete_group_rows"] += incomplete_groups
        counters["uv_slot1_near_unit_group_rows"] += slot1_groups
        counters["uv_other_slot_near_unit_group_rows"] += other_slot_groups
        counters["material_input_status_valid_rows"] += int(material_valid)
        counters["material_candidate_asset_valid_rows"] += int(material_valid)
        counters["material_state_candidate_source_complete_rows"] += int(
            material_valid and state_rows > 0 and state_source_rows == state_rows
        )
        counters["objects_with_multiple_geometry_assignment_models"] += int(geometry_models > 1)
        counters["objects_with_single_geometry_assignment_model"] += int(geometry_models == 1)
        counters["objects_with_single_candidate_uv_role_domain"] += int(uv_role_domain == 1)
        counters["objects_with_single_candidate_material_domain"] += int(material_domain == 1)
        counters["structural_cross_product_model_sum"] += structural_models
        counters["objects_with_multiple_structural_cross_product_models"] += int(structural_models > 1)
        counters["objects_with_single_structural_cross_product_model"] += int(structural_singleton)
        counters["unique_state_geometry_uv_material_binding_proven_rows"] += int(unique_binding_proven)

        rows.append(
            {
                "model_archive": key[0],
                "object_name": key[1],
                "geometry_assignment_models": str(geometry_models),
                "one_to_one_assignment_models": str(one_to_one_models),
                "nonempty_interval_count": global_row.get("nonempty_interval_count", ""),
                "ambiguous_interval_count": global_row.get("ambiguous_interval_rows", ""),
                "geometry_assignment_domain_size": global_row.get("max_interval_domain_size", ""),
                "uv_joined_primitive_groups": str(joined_groups),
                "uv_complete_five_slot_groups": str(complete_groups),
                "uv_incomplete_five_slot_groups": str(incomplete_groups),
                "uv_slot1_near_unit_groups": str(slot1_groups),
                "uv_other_slot_near_unit_groups": str(other_slot_groups),
                "uv_candidate_role_domain_size": str(uv_role_domain),
                "material_state_rows_joined": str(state_rows),
                "material_state_rows_with_candidate_asset_source": str(state_source_rows),
                "material_candidate_asset_valid": str(material_valid),
                "material_candidate_domain_size": str(material_domain),
                "structural_cross_product_model_count": str(structural_models),
                "structural_cross_product_singleton": str(structural_singleton),
                "global_input_status_valid": str(global_valid),
                "unique_state_geometry_uv_material_binding_proven": str(unique_binding_proven),
                "status": STATUS if valid else "inconclusive-independent-original-state-geometry-uv-material-global-assignment-control",
                "semantic_binding": SEMANTIC_BINDING,
                "original_semantics": ORIGINAL_SEMANTICS,
                "issue": "" if valid else "source status or candidate structural domain is incomplete",
            }
        )

    summary = {
        "global_source_report": "",
        "uv_source_report": "",
        "material_source_report": "",
        "global_rows": str(len(global_rows)),
        "uv_rows": str(len(uv_rows)),
        "material_rows": str(len(material_rows)),
        "joined_object_rows": str(len(keys)),
        "missing_uv_objects": str(len(missing_uv)),
        "missing_material_objects": str(len(missing_material)),
        **{key: str(value) for key, value in counters.items()},
        "valid_object_rows": str(sum(row["status"] == STATUS for row in rows)),
        "status": STATUS if (
            len(keys) == len(global_rows)
            and len(keys) == len(uv_rows)
            and len(keys) == len(material_groups)
            and not missing_uv
            and not missing_material
            and counters["global_input_status_valid_rows"] == len(keys)
            and counters["uv_candidate_attribute_valid_rows"] == len(keys)
            and counters["uv_candidate_raster_influence_valid_rows"] == len(keys)
            and counters["material_input_status_valid_rows"] == len(keys)
            and counters["material_candidate_asset_valid_rows"] == len(keys)
            and counters["material_state_candidate_source_complete_rows"] == len(keys)
            and counters["objects_with_multiple_geometry_assignment_models"] == 12
            and counters["objects_with_single_geometry_assignment_model"] == 12
            and counters["objects_with_single_candidate_uv_role_domain"] == len(keys)
            and counters["objects_with_single_candidate_material_domain"] == len(keys)
            and counters["structural_cross_product_model_sum"] == 36
            and counters["objects_with_multiple_structural_cross_product_models"] == 12
            and counters["objects_with_single_structural_cross_product_model"] == 12
            and counters["unique_state_geometry_uv_material_binding_proven_rows"] == 0
            and sum(row["status"] == STATUS for row in rows) == len(keys)
        ) else "inconclusive-independent-original-state-geometry-uv-material-global-assignment-control",
        "semantic_binding": SEMANTIC_BINDING,
        "provenance": PROVENANCE,
        "original_semantics": ORIGINAL_SEMANTICS,
    }
    return rows, summary


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("global_source", type=Path)
    parser.add_argument("uv_source", type=Path)
    parser.add_argument("material_source", type=Path)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()

    global_rows = _read(args.global_source)
    uv_rows = _read(args.uv_source)
    material_rows = _read(args.material_source)
    report, summary = analyze(global_rows, uv_rows, material_rows)
    summary["global_source_report"] = str(args.global_source)
    summary["uv_source_report"] = str(args.uv_source)
    summary["material_source_report"] = str(args.material_source)
    _write(args.report, REPORT_FIELDS, report)
    _write(args.summary, SUMMARY_FIELDS, [summary])
    for field in SUMMARY_FIELDS:
        print(f"{field}: {summary[field]}")
    print(f"report: {args.report}")
    print(f"summary: {args.summary}")
    return 0 if summary["status"] == STATUS else 1


if __name__ == "__main__":
    raise SystemExit(main())
