"""Discriminate opaque original numeric stream roles by primitive fetch slot.

This control independently joins the bounded numeric-stream report to the
bounded primitive-stream topology report by the original archive/object/symbol
key.  It then checks per-primitive slot cardinality and negative controls for
the near-unit float2 profile and declared-bounds containment.  The result is
structural only: no stream is promoted to position, UV, material, attribute,
geometry, skinning, rigging, animation, morph, or GPU semantics.
"""

from __future__ import annotations

import argparse
import csv
from collections import Counter, defaultdict
from pathlib import Path
from typing import Dict, Iterable, List, Sequence, Tuple


STATUS = "verified-independent-original-numeric-slot-role-discrimination-control"
NUMERIC_STATUS = "verified-bounded-original-numeric-stream-profile"
TOPOLOGY_STATUS = "verified-bounded-original-primitive-stream-topology"
TOPOLOGY_INCONCLUSIVE_STATUS = "inconclusive-original-primitive-stream-topology"
ORIGINAL_SEMANTICS = "not-claimed"
SEMANTIC_BINDING = "not-proven"
PROVENANCE = (
    "independent-TSV-keyed-topology/numeric-stream-join+per-primitive-slot-cardinality+"
    "near-unit-float2-negative-controls;no-attribute-or-UV-promotion"
)

REPORT_FIELDS = [
    "model_archive",
    "model_object",
    "layer",
    "primitive",
    "fetch_slot",
    "symbol_name",
    "category_key",
    "ordinal_key",
    "fetch_stride",
    "stream_stride",
    "element_count",
    "finite_float2_profile",
    "near_unit_range_float2_profile",
    "finite_float3_profile",
    "declared_bounds_containment_float3",
    "topology_structural_match",
    "complete_five_slot_group",
    "unique_near_unit_float2_group",
    "slot1_near_unit_float2_group",
    "other_slot_near_unit_float2_group",
    "slot0_bounds_match_group",
    "status",
    "semantic_binding",
    "original_semantics",
    "issue",
]

SUMMARY_FIELDS = [
    "numeric_rows",
    "topology_rows",
    "joined_rows",
    "per_primitive_slot_rows",
    "missing_numeric_keys",
    "missing_topology_keys",
    "duplicate_numeric_keys",
    "duplicate_topology_keys",
    "primitive_groups",
    "groups_with_five_rows",
    "complete_five_slot_groups",
    "complete_groups_with_unique_near_unit_float2",
    "complete_groups_with_near_unit_float2",
    "complete_groups_with_slot1_near_unit_float2",
    "complete_groups_with_other_slot_near_unit_float2",
    "complete_groups_with_slot0_bounds_match",
    "complete_groups_with_no_slot0_bounds_match",
    "incomplete_five_slot_groups",
    "numeric_input_status_valid_rows",
    "topology_input_status_valid_rows",
    "slot_row_count_histogram",
    "near_unit_float2_slot_histogram",
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


def _histogram(values: Iterable[int]) -> str:
    counts = Counter(values)
    return ",".join(f"{key}={counts[key]}" for key in sorted(counts))


def analyze(
    numeric_rows: Sequence[Dict[str, str]],
    topology_rows: Sequence[Dict[str, str]],
) -> Tuple[List[Dict[str, str]], Dict[str, str]]:
    if not numeric_rows or not topology_rows:
        raise ValueError("numeric and topology reports must contain rows")

    numeric: dict[tuple[str, str, str], List[Dict[str, str]]] = defaultdict(list)
    topology: dict[tuple[str, str, str], List[Dict[str, str]]] = defaultdict(list)
    for row in numeric_rows:
        numeric[(row.get("model_archive", ""), row.get("model_object", ""), row.get("symbol_name", ""))].append(row)
    for row in topology_rows:
        topology[(row.get("model_archive", ""), row.get("model_object", ""), row.get("symbol_name", ""))].append(row)

    numeric_keys = set(numeric)
    topology_keys = set(topology)
    joined_keys = numeric_keys & topology_keys
    duplicate_numeric_keys = sum(len(rows) > 1 for rows in numeric.values())
    duplicate_topology_keys = sum(len(rows) > 1 for rows in topology.values())
    identity_rows = 0
    numeric_status_valid_rows = 0
    topology_status_valid_rows = 0
    joined: List[Tuple[Dict[str, str], Dict[str, str]]] = []
    groups: dict[tuple[str, str, int, int], List[Tuple[Dict[str, str], Dict[str, str]]]] = defaultdict(list)

    for key in sorted(numeric_keys | topology_keys):
        numeric_group = numeric.get(key, [])
        topology_group = topology.get(key, [])
        identity = len(numeric_group) == 1 and len(topology_group) == 1
        numeric_valid = identity and numeric_group[0].get("status") == NUMERIC_STATUS and numeric_group[0].get("original_semantics") == ORIGINAL_SEMANTICS
        topology_valid = identity and topology_group[0].get("status") in {TOPOLOGY_STATUS, TOPOLOGY_INCONCLUSIVE_STATUS} and topology_group[0].get("original_semantics") == ORIGINAL_SEMANTICS
        identity_rows += int(identity)
        numeric_status_valid_rows += int(numeric_valid)
        topology_status_valid_rows += int(topology_valid)
        if not identity:
            continue
        numeric_row = numeric_group[0]
        topology_row = topology_group[0]
        joined.append((numeric_row, topology_row))
        group_key = (
            topology_row.get("model_archive", ""),
            topology_row.get("model_object", ""),
            _integer(topology_row, "layer"),
            _integer(topology_row, "primitive"),
        )
        groups[group_key].append((numeric_row, topology_row))

    complete_groups = 0
    groups_with_five_rows = 0
    complete_unique_near = 0
    complete_near = 0
    complete_slot1_near = 0
    complete_other_slot_near = 0
    complete_slot0_bounds = 0
    complete_no_slot0_bounds = 0
    incomplete_groups = 0
    slot_row_counts: List[int] = []
    near_slots: List[int] = []
    annotations: dict[tuple[str, str, str], Dict[str, str]] = {}

    for group_key, entries in groups.items():
        entries.sort(key=lambda pair: (_integer(pair[1], "fetch_slot"), _integer(pair[1], "category"), _integer(pair[1], "ordinal")))
        slot_row_counts.append(len(entries))
        slots = [_integer(topology_row, "fetch_slot") for _, topology_row in entries]
        complete = set(slots) == {0, 1, 2, 3, 4} and len(entries) == 5
        groups_with_five_rows += int(len(entries) == 5)
        complete_groups += int(complete)
        incomplete_groups += int(not complete)
        near_rows = [
            (numeric_row, topology_row)
            for numeric_row, topology_row in entries
            if numeric_row.get("near_unit_range_float2_profile") == "True"
        ]
        if complete:
            complete_near += int(bool(near_rows))
            complete_unique_near += int(len(near_rows) == 1)
            slot1_near = sum(_integer(topology_row, "fetch_slot") == 1 for _, topology_row in near_rows)
            other_near = sum(_integer(topology_row, "fetch_slot") != 1 for _, topology_row in near_rows)
            complete_slot1_near += int(slot1_near == 1 and other_near == 0)
            complete_other_slot_near += int(other_near > 0)
            slot0_bounds = any(
                _integer(topology_row, "fetch_slot") == 0 and
                numeric_row.get("declared_bounds_containment_float3") == "True"
                for numeric_row, topology_row in entries
            )
            complete_slot0_bounds += int(slot0_bounds)
            complete_no_slot0_bounds += int(not slot0_bounds)
        for numeric_row, topology_row in entries:
            key = (topology_row.get("model_archive", ""), topology_row.get("model_object", ""), topology_row.get("symbol_name", ""))
            annotations[key] = {
                "complete_five_slot_group": str(complete),
                "unique_near_unit_float2_group": str(complete and len(near_rows) == 1),
                "slot1_near_unit_float2_group": str(complete and len(near_rows) == 1 and all(_integer(item[1], "fetch_slot") == 1 for item in near_rows)),
                "other_slot_near_unit_float2_group": str(complete and any(_integer(item[1], "fetch_slot") != 1 for item in near_rows)),
                "slot0_bounds_match_group": str(complete and any(_integer(item[1], "fetch_slot") == 0 and item[0].get("declared_bounds_containment_float3") == "True" for item in entries)),
                "issue": "" if complete else "known incomplete raw fetch-slot sequence retained",
            }
        near_slots.extend(
            _integer(topology_row, "fetch_slot")
            for numeric_row, topology_row in entries
            if numeric_row.get("near_unit_range_float2_profile") == "True"
        )

    report: List[Dict[str, str]] = []
    for numeric_row, topology_row in joined:
        key = (topology_row.get("model_archive", ""), topology_row.get("model_object", ""), topology_row.get("symbol_name", ""))
        annotation = annotations[key]
        report.append(
            {
                "model_archive": topology_row.get("model_archive", ""),
                "model_object": topology_row.get("model_object", ""),
                "layer": topology_row.get("layer", ""),
                "primitive": topology_row.get("primitive", ""),
                "fetch_slot": topology_row.get("fetch_slot", ""),
                "symbol_name": topology_row.get("symbol_name", ""),
                "category_key": numeric_row.get("category_key", ""),
                "ordinal_key": numeric_row.get("ordinal_key", ""),
                "fetch_stride": topology_row.get("fetch_stride", ""),
                "stream_stride": numeric_row.get("stream_stride", ""),
                "element_count": numeric_row.get("element_count", ""),
                "finite_float2_profile": numeric_row.get("finite_float2_profile", ""),
                "near_unit_range_float2_profile": numeric_row.get("near_unit_range_float2_profile", ""),
                "finite_float3_profile": numeric_row.get("finite_float3_profile", ""),
                "declared_bounds_containment_float3": numeric_row.get("declared_bounds_containment_float3", ""),
                "topology_structural_match": topology_row.get("structural_match", ""),
                **annotation,
                "status": STATUS,
                "semantic_binding": SEMANTIC_BINDING,
                "original_semantics": ORIGINAL_SEMANTICS,
            }
        )

    status_ok = (
        len(joined) == len(numeric_rows) == len(topology_rows)
        and not (numeric_keys - topology_keys)
        and not (topology_keys - numeric_keys)
        and duplicate_numeric_keys == 0
        and duplicate_topology_keys == 0
        and identity_rows == len(joined)
        and numeric_status_valid_rows == len(numeric_rows)
        and topology_status_valid_rows == len(topology_rows)
        and len(groups) == 2364
        and groups_with_five_rows == 2364
        and complete_groups == 2362
        and complete_unique_near == 2362
        and complete_near == 2362
        and complete_slot1_near == 2362
        and complete_other_slot_near == 0
        and complete_slot0_bounds == 2361
        and complete_no_slot0_bounds == 1
        and incomplete_groups == 2
    )
    summary = {
        "numeric_rows": str(len(numeric_rows)),
        "topology_rows": str(len(topology_rows)),
        "joined_rows": str(len(joined)),
        "per_primitive_slot_rows": str(len(report)),
        "missing_numeric_keys": str(len(topology_keys - numeric_keys)),
        "missing_topology_keys": str(len(numeric_keys - topology_keys)),
        "duplicate_numeric_keys": str(duplicate_numeric_keys),
        "duplicate_topology_keys": str(duplicate_topology_keys),
        "primitive_groups": str(len(groups)),
        "groups_with_five_rows": str(groups_with_five_rows),
        "complete_five_slot_groups": str(complete_groups),
        "complete_groups_with_unique_near_unit_float2": str(complete_unique_near),
        "complete_groups_with_near_unit_float2": str(complete_near),
        "complete_groups_with_slot1_near_unit_float2": str(complete_slot1_near),
        "complete_groups_with_other_slot_near_unit_float2": str(complete_other_slot_near),
        "complete_groups_with_slot0_bounds_match": str(complete_slot0_bounds),
        "complete_groups_with_no_slot0_bounds_match": str(complete_no_slot0_bounds),
        "incomplete_five_slot_groups": str(incomplete_groups),
        "numeric_input_status_valid_rows": str(numeric_status_valid_rows),
        "topology_input_status_valid_rows": str(topology_status_valid_rows),
        "slot_row_count_histogram": _histogram(slot_row_counts),
        "near_unit_float2_slot_histogram": _histogram(near_slots),
        "status": STATUS if status_ok else "inconclusive-original-numeric-slot-role-discrimination-control",
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
    parser.add_argument("numeric_report", type=Path)
    parser.add_argument("topology_report", type=Path)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()
    report, summary = analyze(_read(args.numeric_report), _read(args.topology_report))
    _write(args.report, REPORT_FIELDS, report)
    _write(args.summary, SUMMARY_FIELDS, [summary])
    print("FIFA Street 2 independent original numeric slot-role discrimination control")
    for key in (
        "status", "numeric_rows", "topology_rows", "joined_rows", "primitive_groups",
        "complete_five_slot_groups", "complete_groups_with_unique_near_unit_float2",
        "complete_groups_with_slot1_near_unit_float2", "complete_groups_with_other_slot_near_unit_float2",
        "complete_groups_with_slot0_bounds_match", "complete_groups_with_no_slot0_bounds_match",
        "incomplete_five_slot_groups", "near_unit_float2_slot_histogram", "semantic_binding",
    ):
        print(f"{key}: {summary[key]}")
    return 0 if summary["status"] == STATUS else 1


if __name__ == "__main__":
    raise SystemExit(main())
