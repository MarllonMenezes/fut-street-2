"""Independently cross-check command-descriptor identity and numeric slot roles.

The control joins the bounded original command-descriptor report with the
numeric slot-role report by primitive identity.  It verifies that the command
descriptor's current ordinal is uniquely identified while preserving the two
incomplete numeric groups.  No slot, stream, geometry, UV, material, draw,
skinning, rigging, animation, morph, or GPU meaning is assigned.
"""

from __future__ import annotations

import argparse
import csv
from collections import Counter, defaultdict
from pathlib import Path
from typing import Dict, Iterable, List, Sequence, Tuple


STATUS = "verified-independent-original-command-slot-role-cooccurrence-control"
DESCRIPTOR_STATUS = "verified-bounded-original-primitive-stream-command-descriptor-adjacency"
ROLE_STATUS = "verified-independent-original-numeric-slot-role-discrimination-control"
ORIGINAL_SEMANTICS = "not-claimed"
SEMANTIC_BINDING = "not-proven"
PROVENANCE = (
    "independent-TSV-keyed-command-descriptor/numeric-slot-role-join+current-ordinal-"
    "identity+incomplete-slot-preservation;no-slot-or-draw-semantic-promotion"
)

REPORT_FIELDS = [
    "model_archive",
    "model_object",
    "layer",
    "primitive",
    "descriptor_candidate_ordinal_count",
    "descriptor_identity_candidate_count",
    "descriptor_current_ordinal_present",
    "descriptor_current_ordinal_identity_match",
    "descriptor_current_ordinal_uniquely_identified",
    "descriptor_shifted_ordinal_identity_match",
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
    "descriptor_rows",
    "role_rows",
    "joined_rows",
    "missing_descriptor_keys",
    "missing_role_keys",
    "duplicate_descriptor_keys",
    "duplicate_role_keys",
    "descriptor_input_status_valid_rows",
    "role_input_status_valid_rows",
    "current_ordinal_present_rows",
    "current_ordinal_identity_match_rows",
    "current_ordinal_uniquely_identified_rows",
    "shifted_ordinal_identity_match_rows",
    "primitive_groups",
    "groups_with_five_role_rows",
    "complete_five_slot_groups",
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


def _boolean(row: Dict[str, str], field: str) -> bool:
    value = row.get(field, "")
    if value not in {"True", "False"}:
        raise ValueError(f"invalid boolean field {field!r}: {value!r}")
    return value == "True"


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
    descriptor_rows: Sequence[Dict[str, str]],
    role_rows: Sequence[Dict[str, str]],
) -> Tuple[List[Dict[str, str]], Dict[str, str]]:
    if not descriptor_rows or not role_rows:
        raise ValueError("descriptor and role reports must contain rows")

    descriptor: dict[tuple[str, str, str, int], List[Dict[str, str]]] = defaultdict(list)
    role: dict[tuple[str, str, str, int], List[Dict[str, str]]] = defaultdict(list)
    for row in descriptor_rows:
        descriptor[_key(row)].append(row)
    for row in role_rows:
        role[_key(row)].append(row)

    descriptor_keys = set(descriptor)
    role_keys = set(role)
    joined_keys = descriptor_keys & role_keys
    duplicate_descriptor_keys = sum(len(rows) > 1 for rows in descriptor.values())
    duplicate_role_keys = sum(len(rows) > 5 for rows in role.values())
    descriptor_status_valid_rows = sum(
        len(rows) == 1 and rows[0].get("status") == DESCRIPTOR_STATUS and
        rows[0].get("original_semantics") == ORIGINAL_SEMANTICS
        for rows in descriptor.values()
    )
    role_status_valid_rows = sum(
        row.get("status") == ROLE_STATUS and row.get("original_semantics") == ORIGINAL_SEMANTICS
        for row in role_rows
    )

    current_present_rows = 0
    current_identity_rows = 0
    current_unique_rows = 0
    shifted_identity_rows = 0
    groups_with_five = 0
    complete_groups = 0
    complete_unique_near = 0
    complete_slot1_near = 0
    complete_other_near = 0
    incomplete_groups = 0
    row_counts: List[int] = []
    report: List[Dict[str, str]] = []

    for key in sorted(joined_keys):
        descriptors = descriptor[key]
        roles = sorted(role[key], key=lambda row: _integer(row, "fetch_slot"))
        descriptor_row = descriptors[0] if len(descriptors) == 1 else {}
        row_count = len(roles)
        row_counts.append(row_count)
        groups_with_five += int(row_count == 5)
        complete = row_count == 5 and {_integer(row, "fetch_slot") for row in roles} == {0, 1, 2, 3, 4}
        complete_groups += int(complete)
        incomplete_groups += int(not complete)
        present = _boolean(descriptor_row, "current_ordinal_present") if descriptor_row else False
        identity = _boolean(descriptor_row, "current_ordinal_identity_match") if descriptor_row else False
        unique = _boolean(descriptor_row, "current_ordinal_uniquely_identified") if descriptor_row else False
        shifted = _boolean(descriptor_row, "shifted_ordinal_identity_match") if descriptor_row else False
        current_present_rows += int(present)
        current_identity_rows += int(identity)
        current_unique_rows += int(unique)
        shifted_identity_rows += int(shifted)
        near_rows = [row for row in roles if row.get("near_unit_range_float2_profile") == "True"]
        unique_near = complete and len(near_rows) == 1
        slot1_near = unique_near and _integer(near_rows[0], "fetch_slot") == 1
        other_near = complete and any(_integer(row, "fetch_slot") != 1 for row in near_rows)
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
                "descriptor_candidate_ordinal_count": descriptor_row.get("candidate_ordinal_count", "0"),
                "descriptor_identity_candidate_count": descriptor_row.get("identity_candidate_count", "0"),
                "descriptor_current_ordinal_present": str(present),
                "descriptor_current_ordinal_identity_match": str(identity),
                "descriptor_current_ordinal_uniquely_identified": str(unique),
                "descriptor_shifted_ordinal_identity_match": str(shifted),
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
        len(joined_keys) == len(descriptor_rows) == len(set(role_keys))
        and not (descriptor_keys - role_keys)
        and not (role_keys - descriptor_keys)
        and duplicate_descriptor_keys == 0
        and duplicate_role_keys == 0
        and descriptor_status_valid_rows == len(descriptor_rows)
        and role_status_valid_rows == len(role_rows)
        and len(joined_keys) == 2364
        and current_present_rows == 2364
        and current_identity_rows == 2364
        and current_unique_rows == 2364
        and shifted_identity_rows == 0
        and groups_with_five == 2364
        and complete_groups == 2362
        and complete_unique_near == 2362
        and complete_slot1_near == 2362
        and complete_other_near == 0
        and incomplete_groups == 2
    )
    summary = {
        "descriptor_rows": str(len(descriptor_rows)),
        "role_rows": str(len(role_rows)),
        "joined_rows": str(len(joined_keys)),
        "missing_descriptor_keys": str(len(role_keys - descriptor_keys)),
        "missing_role_keys": str(len(descriptor_keys - role_keys)),
        "duplicate_descriptor_keys": str(duplicate_descriptor_keys),
        "duplicate_role_keys": str(duplicate_role_keys),
        "descriptor_input_status_valid_rows": str(descriptor_status_valid_rows),
        "role_input_status_valid_rows": str(role_status_valid_rows),
        "current_ordinal_present_rows": str(current_present_rows),
        "current_ordinal_identity_match_rows": str(current_identity_rows),
        "current_ordinal_uniquely_identified_rows": str(current_unique_rows),
        "shifted_ordinal_identity_match_rows": str(shifted_identity_rows),
        "primitive_groups": str(len(joined_keys)),
        "groups_with_five_role_rows": str(groups_with_five),
        "complete_five_slot_groups": str(complete_groups),
        "complete_groups_with_unique_near_unit_float2": str(complete_unique_near),
        "complete_groups_with_slot1_near_unit_float2": str(complete_slot1_near),
        "complete_groups_with_other_slot_near_unit_float2": str(complete_other_near),
        "incomplete_five_slot_groups": str(incomplete_groups),
        "role_row_count_histogram": _histogram(row_counts),
        "status": STATUS if status_ok else "inconclusive-original-command-slot-role-cooccurrence-control",
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
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("descriptor_report", type=Path)
    parser.add_argument("role_report", type=Path)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()
    report, summary = analyze(_read(args.descriptor_report), _read(args.role_report))
    _write(args.report, REPORT_FIELDS, report)
    _write(args.summary, SUMMARY_FIELDS, [summary])
    print("FIFA Street 2 independent original command/slot-role cooccurrence control")
    for key in (
        "status", "descriptor_rows", "role_rows", "joined_rows", "primitive_groups",
        "current_ordinal_uniquely_identified_rows", "shifted_ordinal_identity_match_rows",
        "complete_five_slot_groups", "complete_groups_with_unique_near_unit_float2",
        "complete_groups_with_slot1_near_unit_float2", "complete_groups_with_other_slot_near_unit_float2",
        "incomplete_five_slot_groups", "semantic_binding",
    ):
        print(f"{key}: {summary[key]}")
    return 0 if summary["status"] == STATUS else 1


if __name__ == "__main__":
    raise SystemExit(main())
