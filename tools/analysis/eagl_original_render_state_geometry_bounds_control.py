"""Independently join state/geometry partition and bounds/fetch controls.

The control checks object-level cardinality and the repeated bounds-consistent
first raw record across the candidate object set.  It deliberately preserves
state names, command shapes, categories and slots as opaque observations.  A
cardinality or bounds match is not promoted to a draw, vertex, UV, material,
skinning, rigging, animation, morph, or GPU binding.
"""

from __future__ import annotations

import argparse
import csv
from pathlib import Path
from typing import Dict, Iterable, List, Sequence, Tuple


STATUS = "verified-independent-original-render-state-geometry-bounds-structural-control"
PARTITION_STATUS = "verified-independent-original-state-geometry-partition-control"
BOUNDS_STATUS = "verified-independent-original-bounds-fetch-slot-join-control"
SEMANTIC_BINDING = "not-proven"
ORIGINAL_SEMANTICS = "not-claimed"

REPORT_FIELDS = (
    "model_archive",
    "model_object",
    "shape_name",
    "geometry_primitive_count",
    "state_primitive_count",
    "partition_rows",
    "bounds_rows",
    "bounds_primitive_groups",
    "complete_fetch_groups",
    "expected_incomplete_fetch_groups",
    "bounds_slot0_match_groups",
    "bounds_nonzero_match_groups",
    "partition_state_index_match",
    "interval_index_sequence_match",
    "interval_boundary_shape_match",
    "exact_command_shape_rows",
    "status",
    "semantic_binding",
    "original_semantics",
    "issue",
)

SUMMARY_FIELDS = (
    "partition_object_rows",
    "bounds_object_rows",
    "joined_object_rows",
    "partition_input_status_valid_rows",
    "bounds_input_status_valid_rows",
    "object_cardinality_match_rows",
    "bounds_group_rows",
    "complete_fetch_group_rows",
    "expected_incomplete_fetch_group_rows",
    "bounds_slot0_match_rows",
    "bounds_nonzero_match_rows",
    "exact_command_shape_rows",
    "complete_control_rows",
    "semantic_binding",
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
        writer = csv.DictWriter(
            handle,
            fieldnames=list(fields),
            delimiter="\t",
            lineterminator="\n",
        )
        writer.writeheader()
        writer.writerows({field: row.get(field, "") for field in fields} for row in rows)


def _object_key(row: Dict[str, str]) -> Tuple[str, str]:
    return row.get("model_archive", ""), row.get("model_object", row.get("object_name", ""))


def _int(row: Dict[str, str], field: str) -> int:
    return int(row.get(field, "0"))


def _true(row: Dict[str, str], field: str) -> bool:
    return row.get(field, "").strip().lower() == "true"


def analyze(
    partition_rows: Sequence[Dict[str, str]],
    bounds_rows: Sequence[Dict[str, str]],
) -> Tuple[List[Dict[str, str]], Dict[str, str]]:
    partitions = {_object_key(row): row for row in partition_rows}
    bounds_by_object: Dict[Tuple[str, str], List[Dict[str, str]]] = {}
    for row in bounds_rows:
        bounds_by_object.setdefault(_object_key(row), []).append(row)

    # Scope the synthesis to the 24 object rows already selected by the
    # state/geometry candidate control.  Unrelated original objects remain
    # outside this candidate-object join rather than becoming failures.
    keys = sorted(partitions)
    report: List[Dict[str, str]] = []
    partition_valid = 0
    bounds_valid = 0
    cardinality_matches = 0
    bounds_group_rows = 0
    complete_fetch_group_rows = 0
    expected_incomplete_fetch_group_rows = 0
    bounds_slot0_matches = 0
    bounds_nonzero_matches = 0
    exact_command_shape_rows = 0
    complete_control_rows = 0

    for key in keys:
        partition = partitions.get(key)
        object_bounds = bounds_by_object.get(key, [])
        groups: Dict[str, List[Dict[str, str]]] = {}
        for row in object_bounds:
            groups.setdefault(row.get("ordinal_key", ""), []).append(row)
        partition_ok = bool(
            partition
            and partition.get("status") == PARTITION_STATUS
            and partition.get("original_semantics") == ORIGINAL_SEMANTICS
            and _true(partition, "object_state_exact")
            and _true(partition, "state_index_set_match")
            and _true(partition, "interval_index_sequence_match")
            and _true(partition, "interval_boundary_shape_match")
            and _true(partition, "profile_streams_complete")
        )
        if partition_ok:
            partition_valid += 1

        bounds_input_ok = bool(object_bounds) and all(
            row.get("status") == BOUNDS_STATUS and row.get("original_semantics") == ORIGINAL_SEMANTICS
            for row in object_bounds
        )
        bounds_ok = bounds_input_ok
        complete_group_count = 0
        expected_incomplete_group_count = 0
        for group in groups.values():
            ordered = sorted(group, key=lambda item: _int(item, "category_key"))
            categories = [row.get("category_key", "") for row in ordered]
            slots = [row.get("fetch_slot", "") for row in ordered]
            bounds_shape_ok = (
                len(group) == 5
                and categories == ["10", "11", "12", "13", "14"]
                and sum(_true(row, "declared_bounds_containment_float3") for row in group) == 1
                and _true(ordered[0], "declared_bounds_containment_float3")
            )
            complete_group = bounds_shape_ok and slots == ["0", "1", "2", "3", "4"]
            expected_incomplete_group = bounds_shape_ok and slots == ["0", "1", "3", "4", "-1"]
            complete_group_count += int(complete_group)
            expected_incomplete_group_count += int(expected_incomplete_group)
            bounds_ok = bounds_ok and (complete_group or expected_incomplete_group)
        if bounds_input_ok:
            bounds_valid += 1

        geometry_count = _int(partition, "geometry_primitive_count") if partition else 0
        state_count = _int(partition, "state_primitive_count") if partition else 0
        group_count = len(groups)
        cardinality_ok = bool(partition and group_count == geometry_count + state_count)
        cardinality_matches += int(cardinality_ok)
        bounds_group_rows += group_count
        complete_fetch_group_rows += complete_group_count
        expected_incomplete_fetch_group_rows += expected_incomplete_group_count
        slot0_match_count = sum(
            int(any(
                _int(row, "fetch_slot") == 0 and _true(row, "declared_bounds_containment_float3")
                for row in group
            ))
            for group in groups.values()
        )
        nonzero_match_count = sum(
            int(any(
                _int(row, "fetch_slot") != 0 and _true(row, "declared_bounds_containment_float3")
                for row in group
            ))
            for group in groups.values()
        )
        bounds_slot0_matches += slot0_match_count
        bounds_nonzero_matches += nonzero_match_count
        exact_shape_count = _int(partition, "profile_exact_command_shape_count") if partition else 0
        exact_command_shape_rows += exact_shape_count
        complete = bool(
            partition
            and object_bounds
            and partition_ok
            and bounds_ok
            and cardinality_ok
            and slot0_match_count == group_count
            and nonzero_match_count == 0
            and exact_shape_count == 0
        )
        complete_control_rows += int(complete)
        report.append(
            {
                "model_archive": key[0],
                "model_object": key[1],
                "shape_name": partition.get("shape_name", "") if partition else "",
                "geometry_primitive_count": str(geometry_count),
                "state_primitive_count": str(state_count),
                "partition_rows": str(int(partition is not None)),
                "bounds_rows": str(len(object_bounds)),
                "bounds_primitive_groups": str(group_count),
                "complete_fetch_groups": str(complete_group_count),
                "expected_incomplete_fetch_groups": str(expected_incomplete_group_count),
                "bounds_slot0_match_groups": str(slot0_match_count),
                "bounds_nonzero_match_groups": str(nonzero_match_count),
                "partition_state_index_match": partition.get("state_index_set_match", "") if partition else "",
                "interval_index_sequence_match": partition.get("interval_index_sequence_match", "") if partition else "",
                "interval_boundary_shape_match": partition.get("interval_boundary_shape_match", "") if partition else "",
                "exact_command_shape_rows": str(exact_shape_count),
                "status": STATUS if complete else "inconclusive-" + STATUS,
                "semantic_binding": SEMANTIC_BINDING,
                "original_semantics": ORIGINAL_SEMANTICS,
                "issue": "" if complete else "state/geometry partition and bounds/fetch control did not reconcile",
            }
        )

    partition_object_keys = set(partitions)
    bounds_object_keys = set(bounds_by_object)
    joined_object_keys = partition_object_keys & bounds_object_keys
    provenance = "independent-TSV-object-key-join+state-geometry-interval-partition+bounds-first-raw-fetch-slot-control;no-semantic-promotion"
    summary = {
        "partition_object_rows": str(len(partition_rows)),
        "bounds_object_rows": str(len(joined_object_keys)),
        "joined_object_rows": str(len(joined_object_keys)),
        "partition_input_status_valid_rows": str(partition_valid),
        "bounds_input_status_valid_rows": str(bounds_valid),
        "object_cardinality_match_rows": str(cardinality_matches),
            "bounds_group_rows": str(bounds_group_rows),
            "complete_fetch_group_rows": str(complete_fetch_group_rows),
            "expected_incomplete_fetch_group_rows": str(expected_incomplete_fetch_group_rows),
            "bounds_slot0_match_rows": str(bounds_slot0_matches),
        "bounds_nonzero_match_rows": str(bounds_nonzero_matches),
        "exact_command_shape_rows": str(exact_command_shape_rows),
        "complete_control_rows": str(complete_control_rows),
        "semantic_binding": SEMANTIC_BINDING,
        "status": STATUS if (
            len(partition_rows) == 24
            and len(joined_object_keys) == 24
            and partition_valid == bounds_valid == cardinality_matches == complete_control_rows == 24
            and bounds_group_rows == bounds_slot0_matches == 218
            and complete_fetch_group_rows == 216
            and expected_incomplete_fetch_group_rows == 2
            and bounds_nonzero_matches == 0
            and exact_command_shape_rows == 0
            and all(row["status"] == STATUS for row in report)
        ) else "inconclusive-" + STATUS,
        "provenance": provenance,
        "original_semantics": ORIGINAL_SEMANTICS,
    }
    return report, summary


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("partition_report", type=Path)
    parser.add_argument("bounds_report", type=Path)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()
    report, summary = analyze(_read(args.partition_report), _read(args.bounds_report))
    _write(args.report, REPORT_FIELDS, report)
    _write(args.summary, SUMMARY_FIELDS, [summary])
    print("FIFA Street 2 independent render state/geometry/bounds structural control")
    for field in SUMMARY_FIELDS:
        print(f"{field}: {summary[field]}")
    print(f"report: {args.report}")
    print(f"summary: {args.summary}")
    return 0 if summary["status"] == STATUS else 1


if __name__ == "__main__":
    raise SystemExit(main())
