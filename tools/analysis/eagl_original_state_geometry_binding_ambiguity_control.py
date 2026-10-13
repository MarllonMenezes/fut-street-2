"""Independently measure the remaining original state-to-geometry ambiguity.

The three source reports describe the same original objects through pairwise
profiles, ordered state intervals, and object-level state/geometry counts.  This
control re-parses and reconciles those populations.  It verifies that state
coverage is complete while preserving the observed negative result: every
object has more state primitives than geometry primitives, no geometry-group
state exactness, and no exact command-shape pair.  It therefore proves neither
a unique state-to-draw mapping nor UV/material semantics.
"""

from __future__ import annotations

import argparse
import csv
from pathlib import Path
from typing import Dict, Iterable, List, Sequence, Tuple


STATUS = "verified-independent-original-state-geometry-binding-ambiguity-control"
SEMANTIC_BINDING = "not-proven"
ORIGINAL_SEMANTICS = "not-claimed"
ADJACENCY_STATUS = "verified-control"
PROFILE_STATUS = "opaque-profile-control"
ORDER_STATUS = "ordered-index-control"
PARTITION_STATUS = "verified-independent-original-state-geometry-partition-control"
PROVENANCE = (
    "independent-TSV-reparse+object-key-join+profile-cardinality+ordered-state-"
    "interval-reconciliation+negative-uniqueness-control;no-state-draw-or-"
    "UV-material-promotion"
)

REPORT_FIELDS = (
    "model_archive",
    "object_name",
    "shape_name",
    "geometry_primitive_count",
    "state_primitive_count",
    "state_count_greater_than_geometry_count",
    "same_primitive_index_pairs",
    "adjacent_primitive_pairs",
    "same_layer_pairs",
    "geometry_group_state_exact",
    "profile_rows",
    "expected_profile_rows",
    "profile_cardinality_reconciled",
    "exact_command_shape_rows",
    "order_rows",
    "expected_order_rows",
    "order_state_count",
    "order_state_count_reconciled",
    "order_state_index_set_reconciled",
    "unique_geometry_state_binding_proven",
    "status",
    "semantic_binding",
    "original_semantics",
    "issue",
)

SUMMARY_FIELDS = (
    "adjacency_source_report",
    "profile_source_report",
    "order_source_report",
    "partition_source_report",
    "adjacency_rows",
    "profile_rows",
    "order_rows",
    "partition_rows",
    "joined_object_rows",
    "missing_profile_objects",
    "missing_order_objects",
    "missing_partition_objects",
    "adjacency_input_status_valid_rows",
    "profile_input_status_valid_rows",
    "order_input_status_valid_rows",
    "partition_input_status_valid_rows",
    "profile_cardinality_reconciled_rows",
    "order_cardinality_reconciled_rows",
    "order_state_index_set_reconciled_rows",
    "state_count_greater_than_geometry_rows",
    "geometry_group_state_exact_rows",
    "exact_command_shape_rows",
    "unique_geometry_state_binding_proven_rows",
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


def _key(row: Dict[str, str], object_field: str = "object_name") -> Tuple[str, str]:
    key = (row.get("model_archive", ""), row.get(object_field, ""))
    if not all(key):
        raise ValueError(f"empty object identity: {key!r}")
    return key


def _group(rows: Sequence[Dict[str, str]], object_field: str = "object_name") -> Dict[Tuple[str, str], List[Dict[str, str]]]:
    grouped: Dict[Tuple[str, str], List[Dict[str, str]]] = {}
    for row in rows:
        grouped.setdefault(_key(row, object_field), []).append(row)
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


def _index_set(value: str) -> set[str]:
    return {part for part in value.split(",") if part}


def analyze(
    adjacency_rows: Sequence[Dict[str, str]],
    profile_rows: Sequence[Dict[str, str]],
    order_rows: Sequence[Dict[str, str]],
    partition_rows: Sequence[Dict[str, str]],
) -> Tuple[List[Dict[str, str]], Dict[str, str]]:
    if not all((adjacency_rows, profile_rows, order_rows, partition_rows)):
        raise ValueError("all source reports must contain rows")

    adjacency = _group(adjacency_rows)
    profiles = _group(profile_rows)
    ordered = _group(order_rows)
    partitions = _group(partition_rows)
    keys = sorted(set(adjacency) & set(profiles) & set(ordered) & set(partitions))
    report: List[Dict[str, str]] = []
    counters = {
        "profile_cardinality_reconciled": 0,
        "order_cardinality_reconciled": 0,
        "order_state_index_set_reconciled": 0,
        "state_count_greater_than_geometry": 0,
        "geometry_group_state_exact": 0,
        "exact_command_shape": 0,
        "unique_geometry_state_binding_proven": 0,
        "valid_object": 0,
    }

    for key in keys:
        left = adjacency[key]
        profile = profiles[key]
        order = ordered[key]
        partition = partitions[key]
        if len(left) != 1 or len(partition) != 1:
            raise ValueError(f"object-level report is not one row per object: {key!r}")
        adjacency_row = left[0]
        partition_row = partition[0]
        geometry_count = _int(adjacency_row, "geometry_primitive_count")
        state_count = _int(adjacency_row, "state_primitive_count")
        expected_profile_rows = geometry_count * state_count
        expected_order_rows = geometry_count + 1
        profile_cardinality = (
            len(profile) == expected_profile_rows
            and len({row.get("geometry_primitive", "") for row in profile}) == geometry_count
            and len({row.get("state_primitive", "") for row in profile}) == state_count
            and all(row.get("status") == PROFILE_STATUS for row in profile)
            and all(row.get("geometry_stream_complete") == "True" for row in profile)
            and all(row.get("state_stream_complete") == "True" for row in profile)
        )
        order_state_count = sum(_int(row, "state_primitive_count") for row in order)
        adjacency_state_indices = _index_set(adjacency_row.get("state_primitive_indices", ""))
        order_state_indices = set()
        for row in order:
            layer = row.get("layer", "")
            order_state_indices.update(
                f"{layer}:{primitive}"
                for primitive in _index_set(row.get("state_primitive_indices", ""))
            )
        order_cardinality = (
            len(order) == expected_order_rows
            and all(_int(row, "geometry_candidate_count_in_layer") == geometry_count for row in order)
            and all(row.get("status") == ORDER_STATUS for row in order)
            and order_state_count == state_count
        )
        order_index_set = order_state_indices == adjacency_state_indices
        exact_command_shapes = sum(
            row.get("exact_command_shape") == "True" for row in profile
        )
        geometry_group_exact = _bool(adjacency_row, "geometry_group_state_exact")
        state_greater_geometry = state_count > geometry_count
        unique_binding = geometry_group_exact and exact_command_shapes > 0
        input_valid = (
            adjacency_row.get("status") == ADJACENCY_STATUS
            and partition_row.get("status") == PARTITION_STATUS
            and partition_row.get("original_semantics") == ORIGINAL_SEMANTICS
            and all(row.get("original_semantics", ORIGINAL_SEMANTICS) == ORIGINAL_SEMANTICS for row in profile)
        )
        valid = (
            input_valid
            and _bool(adjacency_row, "object_state_exact")
            and not geometry_group_exact
            and profile_cardinality
            and order_cardinality
            and order_index_set
            and state_greater_geometry
            and exact_command_shapes == 0
            and not unique_binding
        )
        counters["profile_cardinality_reconciled"] += int(profile_cardinality)
        counters["order_cardinality_reconciled"] += int(order_cardinality)
        counters["order_state_index_set_reconciled"] += int(order_index_set)
        counters["state_count_greater_than_geometry"] += int(state_greater_geometry)
        counters["geometry_group_state_exact"] += int(geometry_group_exact)
        counters["exact_command_shape"] += exact_command_shapes
        counters["unique_geometry_state_binding_proven"] += int(unique_binding)
        counters["valid_object"] += int(valid)
        row_status = STATUS if valid else f"inconclusive-{STATUS}"
        report.append(
            {
                "model_archive": key[0],
                "object_name": key[1],
                "shape_name": adjacency_row.get("shape_name", ""),
                "geometry_primitive_count": str(geometry_count),
                "state_primitive_count": str(state_count),
                "state_count_greater_than_geometry_count": str(state_greater_geometry),
                "same_primitive_index_pairs": adjacency_row.get("same_primitive_index_pairs", ""),
                "adjacent_primitive_pairs": adjacency_row.get("adjacent_primitive_pairs", ""),
                "same_layer_pairs": adjacency_row.get("same_layer_pairs", ""),
                "geometry_group_state_exact": str(geometry_group_exact),
                "profile_rows": str(len(profile)),
                "expected_profile_rows": str(expected_profile_rows),
                "profile_cardinality_reconciled": str(profile_cardinality),
                "exact_command_shape_rows": str(exact_command_shapes),
                "order_rows": str(len(order)),
                "expected_order_rows": str(expected_order_rows),
                "order_state_count": str(order_state_count),
                "order_state_count_reconciled": str(order_cardinality),
                "order_state_index_set_reconciled": str(order_index_set),
                "unique_geometry_state_binding_proven": str(unique_binding),
                "status": row_status,
                "semantic_binding": SEMANTIC_BINDING,
                "original_semantics": ORIGINAL_SEMANTICS,
                "issue": "" if valid else "state/geometry ambiguity controls require review",
            }
        )

    missing_profile = set(adjacency) - set(profiles)
    missing_order = set(adjacency) - set(ordered)
    missing_partition = set(adjacency) - set(partitions)
    summary = {
        "adjacency_source_report": "",
        "profile_source_report": "",
        "order_source_report": "",
        "partition_source_report": "",
        "adjacency_rows": str(len(adjacency)),
        "profile_rows": str(len(profile_rows)),
        "order_rows": str(len(order_rows)),
        "partition_rows": str(len(partition_rows)),
        "joined_object_rows": str(len(keys)),
        "missing_profile_objects": str(len(missing_profile)),
        "missing_order_objects": str(len(missing_order)),
        "missing_partition_objects": str(len(missing_partition)),
        "adjacency_input_status_valid_rows": str(sum(row.get("status") == ADJACENCY_STATUS for row in adjacency_rows)),
        "profile_input_status_valid_rows": str(sum(row.get("status") == PROFILE_STATUS for row in profile_rows)),
        "order_input_status_valid_rows": str(sum(row.get("status") == ORDER_STATUS for row in order_rows)),
        "partition_input_status_valid_rows": str(sum(row.get("status") == PARTITION_STATUS for row in partition_rows)),
        "profile_cardinality_reconciled_rows": str(counters["profile_cardinality_reconciled"]),
        "order_cardinality_reconciled_rows": str(counters["order_cardinality_reconciled"]),
        "order_state_index_set_reconciled_rows": str(counters["order_state_index_set_reconciled"]),
        "state_count_greater_than_geometry_rows": str(counters["state_count_greater_than_geometry"]),
        "geometry_group_state_exact_rows": str(counters["geometry_group_state_exact"]),
        "exact_command_shape_rows": str(counters["exact_command_shape"]),
        "unique_geometry_state_binding_proven_rows": str(counters["unique_geometry_state_binding_proven"]),
        "valid_object_rows": str(counters["valid_object"]),
        "status": STATUS if (
            len(adjacency) == len(profiles) == len(ordered) == len(partitions) == len(keys) == 24
            and not missing_profile
            and not missing_order
            and not missing_partition
            and counters["profile_cardinality_reconciled"] == 24
            and counters["order_cardinality_reconciled"] == 24
            and counters["order_state_index_set_reconciled"] == 24
            and counters["state_count_greater_than_geometry"] == 24
            and counters["geometry_group_state_exact"] == 0
            and counters["exact_command_shape"] == 0
            and counters["unique_geometry_state_binding_proven"] == 0
            and counters["valid_object"] == 24
            and all(row["status"] == STATUS for row in report)
        ) else f"inconclusive-{STATUS}",
        "semantic_binding": SEMANTIC_BINDING,
        "provenance": PROVENANCE,
        "original_semantics": ORIGINAL_SEMANTICS,
    }
    return report, summary


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("adjacency_report", type=Path)
    parser.add_argument("profile_report", type=Path)
    parser.add_argument("order_report", type=Path)
    parser.add_argument("partition_report", type=Path)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()
    report, summary = analyze(
        _read(args.adjacency_report),
        _read(args.profile_report),
        _read(args.order_report),
        _read(args.partition_report),
    )
    summary["adjacency_source_report"] = str(args.adjacency_report)
    summary["profile_source_report"] = str(args.profile_report)
    summary["order_source_report"] = str(args.order_report)
    summary["partition_source_report"] = str(args.partition_report)
    _write(args.report, REPORT_FIELDS, report)
    _write(args.summary, SUMMARY_FIELDS, [summary])
    print("FIFA Street 2 independent original state/geometry binding ambiguity control")
    for field in SUMMARY_FIELDS:
        print(f"{field}: {summary[field]}")
    print(f"report: {args.report}")
    print(f"summary: {args.summary}")
    return 0 if summary["status"] == STATUS else 1


if __name__ == "__main__":
    raise SystemExit(main())
