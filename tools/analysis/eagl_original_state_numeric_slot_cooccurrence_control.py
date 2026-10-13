"""Reparse state/fetch and numeric slot-role cooccurrence by primitive.

The control joins two already bounded original reports by archive/object/layer/
primitive.  It checks cardinality and preserves the two incomplete fetch-slot
groups.  State labels and numeric slots remain opaque; no state-to-draw,
position, UV, material, attribute, skinning, rigging, animation, morph, or GPU
meaning is assigned.
"""

from __future__ import annotations

import argparse
import csv
from collections import Counter, defaultdict
from pathlib import Path
from typing import Dict, Iterable, List, Sequence, Tuple


STATUS = "verified-independent-original-state-numeric-slot-cooccurrence-control"
STATE_STATUS = "verified-independent-original-state-fetch-adjacency-control"
ROLE_STATUS = "verified-independent-original-numeric-slot-role-discrimination-control"
ORIGINAL_SEMANTICS = "not-claimed"
SEMANTIC_BINDING = "not-proven"
PROVENANCE = (
    "independent-TSV-keyed-state-fetch/numeric-slot-role-join+primitive-cardinality+"
    "incomplete-slot-preservation;no-state-to-draw-or-UV-material-promotion"
)

REPORT_FIELDS = [
    "model_archive",
    "model_object",
    "layer",
    "primitive",
    "state_relocation_present",
    "has_shapename_assignment",
    "has_primitive_type_assignment",
    "exact_adjacency_stream_count",
    "role_row_count",
    "complete_five_slot_group",
    "unique_near_unit_float2_group",
    "slot1_near_unit_float2_group",
    "other_slot_near_unit_float2_group",
    "status",
    "semantic_binding",
    "original_semantics",
    "issue",
]

SUMMARY_FIELDS = [
    "state_rows",
    "role_rows",
    "joined_rows",
    "missing_state_keys",
    "missing_role_keys",
    "duplicate_state_keys",
    "duplicate_role_keys",
    "state_input_status_valid_rows",
    "role_input_status_valid_rows",
    "primitive_groups",
    "groups_with_five_role_rows",
    "complete_five_slot_groups",
    "state_relocation_rows",
    "shapename_state_rows",
    "primitive_type_state_rows",
    "exact_adjacency_stream_rows",
    "complete_groups_with_shapename_state",
    "complete_groups_without_shapename_state",
    "complete_groups_with_primitive_type_state",
    "complete_groups_without_primitive_type_state",
    "complete_groups_with_unique_near_unit_float2",
    "complete_groups_with_slot1_near_unit_float2",
    "complete_groups_with_other_slot_near_unit_float2",
    "incomplete_five_slot_groups",
    "role_row_count_histogram",
    "status",
    "semantic_binding",
    "provenance",
    "original_semantics",
]


def _read(path: Path) -> List[Dict[str, str]]:
    with path.open("r", encoding="utf-8-sig", newline="") as handle:
        return list(csv.DictReader(handle, delimiter="\t"))


def _integer(row: Dict[str, str], field: str) -> int:
    try:
        return int(row[field], 0)
    except (KeyError, TypeError, ValueError) as exc:
        raise ValueError(f"invalid integer field {field!r}") from exc


def _field(value: object) -> str:
    return str(value).replace("\t", " ").replace("\r", " ").replace("\n", " ")


def _key(row: Dict[str, str]) -> tuple[str, str, str, int]:
    return (
        row.get("model_archive", ""),
        row.get("model_object", ""),
        row.get("layer", ""),
        _integer(row, "primitive"),
    )


def _histogram(values: Iterable[int]) -> str:
    counts = Counter(values)
    return ",".join(f"{key}={counts[key]}" for key in sorted(counts))


def analyze(
    state_rows: Sequence[Dict[str, str]],
    role_rows: Sequence[Dict[str, str]],
) -> Tuple[List[Dict[str, str]], Dict[str, str]]:
    if not state_rows or not role_rows:
        raise ValueError("state and role reports must contain rows")

    state: dict[tuple[str, str, str, int], List[Dict[str, str]]] = defaultdict(list)
    role: dict[tuple[str, str, str, int], List[Dict[str, str]]] = defaultdict(list)
    for row in state_rows:
        state[_key(row)].append(row)
    for row in role_rows:
        role[_key(row)].append(row)

    state_keys = set(state)
    role_keys = set(role)
    joined_keys = state_keys & role_keys
    duplicate_state_keys = sum(len(rows) > 1 for rows in state.values())
    duplicate_role_keys = sum(len(rows) > 5 for rows in role.values())
    state_status_valid_rows = sum(
        len(rows) == 1 and rows[0].get("status") == STATE_STATUS and rows[0].get("original_semantics") == ORIGINAL_SEMANTICS
        for rows in state.values()
    )
    role_status_valid_rows = sum(
        row.get("status") == ROLE_STATUS and row.get("original_semantics") == ORIGINAL_SEMANTICS
        for row in role_rows
    )

    complete_groups = 0
    groups_with_five_rows = 0
    state_relocation_rows = 0
    shapename_state_rows = 0
    primitive_type_state_rows = 0
    exact_adjacency_stream_rows = 0
    complete_with_shapename = 0
    complete_without_shapename = 0
    complete_with_primitive_type = 0
    complete_without_primitive_type = 0
    complete_unique_near = 0
    complete_slot1_near = 0
    complete_other_near = 0
    incomplete_groups = 0
    row_counts: List[int] = []
    report: List[Dict[str, str]] = []

    for key in sorted(joined_keys):
        state_group = state[key]
        role_group = role[key]
        state_row = state_group[0] if len(state_group) == 1 else {}
        roles = sorted(role_group, key=lambda row: _integer(row, "fetch_slot"))
        row_count = len(roles)
        row_counts.append(row_count)
        groups_with_five_rows += int(row_count == 5)
        complete = row_count == 5 and { _integer(row, "fetch_slot") for row in roles } == {0, 1, 2, 3, 4}
        complete_groups += int(complete)
        incomplete_groups += int(not complete)
        state_relocation = state_row.get("state_relocation_present") == "True"
        has_shapename = state_row.get("has_shapename_assignment") == "True"
        has_primitive_type = state_row.get("has_primitive_type_assignment") == "True"
        exact_adjacency = _integer(state_row, "exact_adjacency_stream_count") == 1 if state_row else False
        state_relocation_rows += int(state_relocation)
        shapename_state_rows += int(has_shapename)
        primitive_type_state_rows += int(has_primitive_type)
        exact_adjacency_stream_rows += int(exact_adjacency)
        near_rows = [row for row in roles if row.get("near_unit_range_float2_profile") == "True"]
        unique_near = complete and len(near_rows) == 1
        slot1_near = unique_near and _integer(near_rows[0], "fetch_slot") == 1
        other_near = complete and any(_integer(row, "fetch_slot") != 1 for row in near_rows)
        complete_with_shapename += int(complete and has_shapename)
        complete_without_shapename += int(complete and not has_shapename)
        complete_with_primitive_type += int(complete and has_primitive_type)
        complete_without_primitive_type += int(complete and not has_primitive_type)
        complete_unique_near += int(unique_near)
        complete_slot1_near += int(slot1_near)
        complete_other_near += int(other_near)
        archive, object_name, layer, primitive = key
        report.append(
            {
                "model_archive": archive,
                "model_object": object_name,
                "layer": layer,
                "primitive": str(primitive),
                "state_relocation_present": str(state_relocation),
                "has_shapename_assignment": str(has_shapename),
                "has_primitive_type_assignment": str(has_primitive_type),
                "exact_adjacency_stream_count": state_row.get("exact_adjacency_stream_count", "0"),
                "role_row_count": str(row_count),
                "complete_five_slot_group": str(complete),
                "unique_near_unit_float2_group": str(unique_near),
                "slot1_near_unit_float2_group": str(slot1_near),
                "other_slot_near_unit_float2_group": str(other_near),
                "status": STATUS,
                "semantic_binding": SEMANTIC_BINDING,
                "original_semantics": ORIGINAL_SEMANTICS,
                "issue": "" if complete else "known incomplete raw fetch-slot sequence retained",
            }
        )

    status_ok = (
        len(joined_keys) == len(state_rows) == len(set(role_keys))
        and not (state_keys - role_keys)
        and not (role_keys - state_keys)
        and duplicate_state_keys == 0
        and duplicate_role_keys == 0
        and state_status_valid_rows == len(state_rows)
        and role_status_valid_rows == len(role_rows)
        and len(joined_keys) == 2364
        and groups_with_five_rows == 2364
        and complete_groups == 2362
        and state_relocation_rows == 2364
        and shapename_state_rows == 752
        and primitive_type_state_rows == 1612
        and exact_adjacency_stream_rows == 2364
        and complete_unique_near == 2362
        and complete_slot1_near == 2362
        and complete_other_near == 0
        and incomplete_groups == 2
    )
    summary = {
        "state_rows": str(len(state_rows)),
        "role_rows": str(len(role_rows)),
        "joined_rows": str(len(joined_keys)),
        "missing_state_keys": str(len(role_keys - state_keys)),
        "missing_role_keys": str(len(state_keys - role_keys)),
        "duplicate_state_keys": str(duplicate_state_keys),
        "duplicate_role_keys": str(duplicate_role_keys),
        "state_input_status_valid_rows": str(state_status_valid_rows),
        "role_input_status_valid_rows": str(role_status_valid_rows),
        "primitive_groups": str(len(joined_keys)),
        "groups_with_five_role_rows": str(groups_with_five_rows),
        "complete_five_slot_groups": str(complete_groups),
        "state_relocation_rows": str(state_relocation_rows),
        "shapename_state_rows": str(shapename_state_rows),
        "primitive_type_state_rows": str(primitive_type_state_rows),
        "exact_adjacency_stream_rows": str(exact_adjacency_stream_rows),
        "complete_groups_with_shapename_state": str(complete_with_shapename),
        "complete_groups_without_shapename_state": str(complete_without_shapename),
        "complete_groups_with_primitive_type_state": str(complete_with_primitive_type),
        "complete_groups_without_primitive_type_state": str(complete_without_primitive_type),
        "complete_groups_with_unique_near_unit_float2": str(complete_unique_near),
        "complete_groups_with_slot1_near_unit_float2": str(complete_slot1_near),
        "complete_groups_with_other_slot_near_unit_float2": str(complete_other_near),
        "incomplete_five_slot_groups": str(incomplete_groups),
        "role_row_count_histogram": _histogram(row_counts),
        "status": STATUS if status_ok else "inconclusive-original-state-numeric-slot-cooccurrence-control",
        "semantic_binding": SEMANTIC_BINDING,
        "provenance": PROVENANCE,
        "original_semantics": ORIGINAL_SEMANTICS,
    }
    return report, summary


def _write(path: Path, fields: Sequence[str], rows: Iterable[Dict[str, str]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=list(fields), delimiter="\t", lineterminator="\n")
        writer.writeheader()
        writer.writerows({field: _field(row.get(field, "")) for field in fields} for row in rows)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("state_report", type=Path)
    parser.add_argument("role_report", type=Path)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()
    report, summary = analyze(_read(args.state_report), _read(args.role_report))
    _write(args.report, REPORT_FIELDS, report)
    _write(args.summary, SUMMARY_FIELDS, [summary])
    print("FIFA Street 2 independent original state/numeric slot cooccurrence control")
    for key in (
        "status", "state_rows", "role_rows", "joined_rows", "primitive_groups",
        "complete_five_slot_groups", "shapename_state_rows", "primitive_type_state_rows",
        "complete_groups_with_unique_near_unit_float2", "complete_groups_with_slot1_near_unit_float2",
        "complete_groups_with_other_slot_near_unit_float2", "incomplete_five_slot_groups",
        "semantic_binding",
    ):
        print(f"{key}: {summary[key]}")
    return 0 if summary["status"] == STATUS else 1


if __name__ == "__main__":
    raise SystemExit(main())
