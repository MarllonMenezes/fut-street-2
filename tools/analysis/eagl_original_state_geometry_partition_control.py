"""Independently cross-check the original state/geometry partition reports.

This control reparses the previously emitted adjacency, opaque-profile and
ordered-interval TSVs. It checks only object-key joins, primitive cardinality,
state-index coverage and interval partitioning. It deliberately does not
assign a state run to a mesh group, material, UV, skinning, rigging,
animation, morph, command or GPU meaning.
"""

from __future__ import annotations

import argparse
import csv
from pathlib import Path
from typing import Dict, Iterable, List, Sequence, Set, Tuple


STATUS = "verified-independent-original-state-geometry-partition-control"
ADJACENCY_STATUS = "verified-control"
PROFILE_STATUS = "opaque-profile-control"
ORDER_STATUS = "ordered-index-control"
ORIGINAL_SEMANTICS = "not-claimed"
PROVENANCE = (
    "independent-TSV-reparse+object-key-join+primitive-cardinality+"
    "state-index-coverage+ordered-interval-partition+negative-profile-control;"
    "no-semantic-promotion"
)

REPORT_FIELDS = (
    "model_archive",
    "object_name",
    "shape_name",
    "adjacency_input_status",
    "profile_input_status",
    "order_input_status",
    "object_state_exact",
    "geometry_group_state_exact",
    "geometry_primitive_count",
    "profile_geometry_pair_count",
    "profile_geometry_identity_count",
    "state_primitive_count",
    "profile_state_identity_count",
    "profile_row_count",
    "order_row_count",
    "order_partitioned_state_count",
    "adjacency_state_index_count",
    "order_state_index_count",
    "state_index_set_match",
    "interval_index_sequence_match",
    "interval_boundary_shape_match",
    "profile_streams_complete",
    "profile_exact_command_shape_count",
    "status",
    "original_semantics",
    "issue",
)

SUMMARY_FIELDS = (
    "adjacency_source_report",
    "profile_source_report",
    "order_source_report",
    "adjacency_rows",
    "profile_rows",
    "order_rows",
    "joined_object_rows",
    "adjacency_input_status_valid_rows",
    "profile_input_status_valid_rows",
    "order_input_status_valid_rows",
    "object_state_exact_rows",
    "geometry_group_state_exact_rows",
    "profile_geometry_pair_rows",
    "profile_state_pair_rows",
    "profile_rows_reconciled",
    "order_partitioned_states",
    "adjacency_states",
    "order_states",
    "state_index_set_match_rows",
    "interval_index_sequence_match_rows",
    "interval_boundary_shape_match_rows",
    "complete_profile_stream_rows",
    "exact_command_shape_rows",
    "exact_command_shape_expected_zero",
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


def _int(row: Dict[str, str], field: str) -> int:
    value = row.get(field, "")
    try:
        return int(value, 0)
    except (TypeError, ValueError) as exc:
        raise ValueError(f"invalid integer in {field}: {value!r}") from exc


def _bool(row: Dict[str, str], field: str) -> bool:
    value = row.get(field, "")
    if value not in {"True", "False"}:
        raise ValueError(f"invalid boolean in {field}: {value!r}")
    return value == "True"


def _key(row: Dict[str, str]) -> Tuple[str, str, str]:
    key = (row.get("model_archive", ""), row.get("object_name", ""), row.get("shape_name", ""))
    if not all(key):
        raise ValueError(f"empty object identity: {key!r}")
    return key


def _split_indices(value: str, *, allow_layer_prefix: bool = False) -> Set[int]:
    result: Set[int] = set()
    for item in value.split(","):
        item = item.strip()
        if not item:
            continue
        if allow_layer_prefix and ":" in item:
            layer, primitive = item.split(":", 1)
            if int(layer, 0) != 0:
                raise ValueError(f"unexpected non-zero layer in primitive index: {item!r}")
            item = primitive
        result.add(int(item, 0))
    return result


def _group(rows: Iterable[Dict[str, str]]) -> Dict[Tuple[str, str, str], List[Dict[str, str]]]:
    grouped: Dict[Tuple[str, str, str], List[Dict[str, str]]] = {}
    for row in rows:
        grouped.setdefault(_key(row), []).append(row)
    return grouped


def _interval_boundary_shape(
    adjacency: Dict[str, str], order_rows: Sequence[Dict[str, str]]
) -> bool:
    if not order_rows:
        return False
    geometry_indices = sorted(
        _split_indices(adjacency.get("geometry_primitive_indices", ""), allow_layer_prefix=True)
    )
    if not geometry_indices or len(order_rows) != len(geometry_indices) + 1:
        return False
    ordered = sorted(order_rows, key=lambda row: _int(row, "interval_index"))
    for expected, row in enumerate(ordered):
        if _int(row, "interval_index") != expected:
            return False
        previous = row.get("previous_geometry_primitive", "")
        next_value = row.get("next_geometry_primitive", "")
        if expected == 0:
            if row.get("interval_kind") != "before-first-geometry" or previous:
                return False
            if next_value != str(geometry_indices[0]):
                return False
        elif expected == len(geometry_indices):
            if row.get("interval_kind") != "after-last-geometry" or next_value:
                return False
            if previous != str(geometry_indices[-1]):
                return False
        else:
            if row.get("interval_kind") != "between-geometry-candidates":
                return False
            if previous != str(geometry_indices[expected - 1]) or next_value != str(geometry_indices[expected]):
                return False
    return True


def analyze(
    adjacency_rows: Sequence[Dict[str, str]],
    profile_rows: Sequence[Dict[str, str]],
    order_rows: Sequence[Dict[str, str]],
) -> Tuple[List[Dict[str, str]], Dict[str, str]]:
    if not adjacency_rows or not profile_rows or not order_rows:
        raise ValueError("all three source reports must contain rows")

    adjacency_by_key = _group(adjacency_rows)
    profile_by_key = _group(profile_rows)
    order_by_key = _group(order_rows)
    keys = sorted(set(adjacency_by_key) & set(profile_by_key) & set(order_by_key))
    if any(len(rows) != 1 for rows in adjacency_by_key.values()):
        raise ValueError("adjacency report contains duplicate object keys")

    report: List[Dict[str, str]] = []
    object_state_exact_rows = 0
    geometry_group_state_exact_rows = 0
    profile_geometry_pair_rows = 0
    profile_state_pair_rows = 0
    profile_rows_reconciled = 0
    order_partitioned_states = 0
    adjacency_states = 0
    order_states = 0
    state_index_set_match_rows = 0
    interval_index_sequence_match_rows = 0
    interval_boundary_shape_match_rows = 0
    complete_profile_stream_rows = 0
    exact_command_shape_rows = 0

    for key in keys:
        adjacency = adjacency_by_key[key][0]
        profiles = profile_by_key[key]
        intervals = order_by_key[key]
        geometry_count = _int(adjacency, "geometry_primitive_count")
        state_count = _int(adjacency, "state_primitive_count")
        adjacency_geometry_indices = _split_indices(
            adjacency.get("geometry_primitive_indices", ""), allow_layer_prefix=True
        )
        adjacency_state_indices = _split_indices(
            adjacency.get("state_primitive_indices", ""), allow_layer_prefix=True
        )
        profile_geometry_pairs = {
            (_int(row, "geometry_layer"), _int(row, "geometry_primitive")) for row in profiles
        }
        profile_state_pairs = {
            (_int(row, "state_layer"), _int(row, "state_primitive")) for row in profiles
        }
        profile_geometry_indices = {primitive for _, primitive in profile_geometry_pairs}
        profile_state_indices = {primitive for _, primitive in profile_state_pairs}
        interval_indices = [_int(row, "interval_index") for row in intervals]
        interval_states = {
            int(value, 0)
            for row in intervals
            for value in row.get("state_primitive_indices", "").split(",")
            if value.strip()
        }
        profile_streams_complete = all(
            _bool(row, "geometry_stream_complete") and _bool(row, "state_stream_complete")
            for row in profiles
        )
        exact_command_shapes = sum(_bool(row, "exact_command_shape") for row in profiles)
        order_partitioned = sum(_int(row, "state_primitive_count") for row in intervals)
        state_set_match = interval_states == adjacency_state_indices
        interval_sequence_match = sorted(interval_indices) == list(range(len(intervals)))
        boundary_shape_match = _interval_boundary_shape(adjacency, intervals)
        profile_pairs_match = (
            len(profile_geometry_pairs) == geometry_count
            and profile_geometry_indices == adjacency_geometry_indices
            and len(profile_state_pairs) == state_count
            and profile_state_indices == adjacency_state_indices
            and len(profiles) == geometry_count * state_count
        )
        row_status = STATUS if (
            adjacency.get("status") == ADJACENCY_STATUS
            and all(row.get("status") == PROFILE_STATUS for row in profiles)
            and all(row.get("status") == ORDER_STATUS for row in intervals)
            and _bool(adjacency, "object_state_exact")
            and not _bool(adjacency, "geometry_group_state_exact")
            and profile_pairs_match
            and order_partitioned == state_count
            and state_set_match
            and interval_sequence_match
            and boundary_shape_match
            and profile_streams_complete
            and exact_command_shapes == 0
        ) else "inconclusive-independent-original-state-geometry-partition-control"

        object_state_exact_rows += int(_bool(adjacency, "object_state_exact"))
        geometry_group_state_exact_rows += int(_bool(adjacency, "geometry_group_state_exact"))
        profile_geometry_pair_rows += len(profile_geometry_pairs)
        profile_state_pair_rows += len(profile_state_pairs)
        profile_rows_reconciled += int(profile_pairs_match)
        order_partitioned_states += order_partitioned
        adjacency_states += state_count
        order_states += order_partitioned
        state_index_set_match_rows += int(state_set_match)
        interval_index_sequence_match_rows += int(interval_sequence_match)
        interval_boundary_shape_match_rows += int(boundary_shape_match)
        complete_profile_stream_rows += sum(
            _bool(row, "geometry_stream_complete") and _bool(row, "state_stream_complete")
            for row in profiles
        )
        exact_command_shape_rows += exact_command_shapes
        report.append(
            {
                "model_archive": key[0],
                "object_name": key[1],
                "shape_name": key[2],
                "adjacency_input_status": adjacency.get("status", ""),
                "profile_input_status": ",".join(sorted({row.get("status", "") for row in profiles})),
                "order_input_status": ",".join(sorted({row.get("status", "") for row in intervals})),
                "object_state_exact": str(_bool(adjacency, "object_state_exact")),
                "geometry_group_state_exact": str(_bool(adjacency, "geometry_group_state_exact")),
                "geometry_primitive_count": str(geometry_count),
                "profile_geometry_pair_count": str(len(profile_geometry_pairs)),
                "profile_geometry_identity_count": str(len(profile_geometry_indices)),
                "state_primitive_count": str(state_count),
                "profile_state_identity_count": str(len(profile_state_indices)),
                "profile_row_count": str(len(profiles)),
                "order_row_count": str(len(intervals)),
                "order_partitioned_state_count": str(order_partitioned),
                "adjacency_state_index_count": str(len(adjacency_state_indices)),
                "order_state_index_count": str(len(interval_states)),
                "state_index_set_match": str(state_set_match),
                "interval_index_sequence_match": str(interval_sequence_match),
                "interval_boundary_shape_match": str(boundary_shape_match),
                "profile_streams_complete": str(profile_streams_complete),
                "profile_exact_command_shape_count": str(exact_command_shapes),
                "status": row_status,
                "original_semantics": ORIGINAL_SEMANTICS,
                "issue": "" if row_status == STATUS else "source reports were not structurally reconciled",
            }
        )

    adjacency_input_status_valid_rows = sum(
        row.get("status") == ADJACENCY_STATUS for row in adjacency_rows
    )
    profile_input_status_valid_rows = sum(
        row.get("status") == PROFILE_STATUS for row in profile_rows
    )
    order_input_status_valid_rows = sum(row.get("status") == ORDER_STATUS for row in order_rows)
    summary = {
        "adjacency_source_report": "",
        "profile_source_report": "",
        "order_source_report": "",
        "adjacency_rows": str(len(adjacency_rows)),
        "profile_rows": str(len(profile_rows)),
        "order_rows": str(len(order_rows)),
        "joined_object_rows": str(len(keys)),
        "adjacency_input_status_valid_rows": str(adjacency_input_status_valid_rows),
        "profile_input_status_valid_rows": str(profile_input_status_valid_rows),
        "order_input_status_valid_rows": str(order_input_status_valid_rows),
        "object_state_exact_rows": str(object_state_exact_rows),
        "geometry_group_state_exact_rows": str(geometry_group_state_exact_rows),
        "profile_geometry_pair_rows": str(profile_geometry_pair_rows),
        "profile_state_pair_rows": str(profile_state_pair_rows),
        "profile_rows_reconciled": str(profile_rows_reconciled),
        "order_partitioned_states": str(order_partitioned_states),
        "adjacency_states": str(adjacency_states),
        "order_states": str(order_states),
        "state_index_set_match_rows": str(state_index_set_match_rows),
        "interval_index_sequence_match_rows": str(interval_index_sequence_match_rows),
        "interval_boundary_shape_match_rows": str(interval_boundary_shape_match_rows),
        "complete_profile_stream_rows": str(complete_profile_stream_rows),
        "exact_command_shape_rows": str(exact_command_shape_rows),
        "exact_command_shape_expected_zero": str(exact_command_shape_rows == 0),
        "status": STATUS if (
            len(keys) == len(adjacency_by_key) == len(profile_by_key) == len(order_by_key)
            and adjacency_input_status_valid_rows == len(adjacency_rows)
            and profile_input_status_valid_rows == len(profile_rows)
            and order_input_status_valid_rows == len(order_rows)
            and object_state_exact_rows == len(keys)
            and geometry_group_state_exact_rows == 0
            and profile_rows_reconciled == len(keys)
            and order_partitioned_states == adjacency_states == order_states
            and state_index_set_match_rows == len(keys)
            and interval_index_sequence_match_rows == len(keys)
            and interval_boundary_shape_match_rows == len(keys)
            and complete_profile_stream_rows == len(profile_rows)
            and exact_command_shape_rows == 0
            and all(row["status"] == STATUS for row in report)
        ) else "inconclusive-independent-original-state-geometry-partition-control",
        "provenance": PROVENANCE,
        "original_semantics": ORIGINAL_SEMANTICS,
    }
    return report, summary


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("adjacency_report", type=Path)
    parser.add_argument("profile_report", type=Path)
    parser.add_argument("order_report", type=Path)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()
    report, summary = analyze(
        _read(args.adjacency_report), _read(args.profile_report), _read(args.order_report)
    )
    summary["adjacency_source_report"] = str(args.adjacency_report)
    summary["profile_source_report"] = str(args.profile_report)
    summary["order_source_report"] = str(args.order_report)
    _write(args.report, REPORT_FIELDS, report)
    _write(args.summary, SUMMARY_FIELDS, [summary])
    print("FIFA Street 2 independent original state/geometry partition control")
    for field in SUMMARY_FIELDS:
        print(f"{field}: {summary[field]}")
    print(f"report: {args.report}")
    print(f"summary: {args.summary}")
    return 0 if summary["status"] == STATUS else 1


if __name__ == "__main__":
    raise SystemExit(main())
