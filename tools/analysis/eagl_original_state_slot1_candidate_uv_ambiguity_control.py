"""Reparse state/fetch, slot-role and candidate raster evidence together.

This control measures whether original state rows that carry SHAPENAME and
clamp tokens are uniquely distinguished by the independently revalidated
near-unit float2 slot role, while also checking candidate texture/raster
compatibility for the same object.  It deliberately does not call slot 1 UV,
does not assign a state run to a draw, and does not promote candidate texture
influence to original material semantics.
"""

from __future__ import annotations

import argparse
import csv
from collections import defaultdict
from pathlib import Path
from typing import Iterable, Sequence


STATUS = "verified-independent-original-state-slot1-candidate-uv-ambiguity-control"
SEMANTIC_BINDING = "not-proven"
ORIGINAL_SEMANTICS = "not-claimed"
STATE_STATUS = "verified-independent-original-state-fetch-adjacency-control"
ROLE_STATUS = "verified-independent-original-numeric-slot-role-discrimination-control"
ATTRIBUTE_STATUS = "verified-independent-candidate-attribute-texture-compatibility-control"
INFLUENCE_STATUS = "verified-candidate-raster-influence"
PROFILE_STATUS = "verified-independent-original-state-profile-discrimination-control"
PROVENANCE = (
    "independent-TSV-reparse+primitive-key-state/fetch-to-slot-role-join+"
    "object-key-candidate-texture/raster-join+state-geometry-uniqueness-"
    "negative-control;no-UV-material-or-state-draw-promotion"
)

REPORT_FIELDS = (
    "model_archive",
    "model_object",
    "state_rows",
    "numeric_slot_rows",
    "joined_primitive_groups",
    "complete_five_slot_groups",
    "incomplete_five_slot_groups",
    "shapename_state_groups",
    "complete_shapename_state_groups",
    "complete_shapename_slot1_near_unit_groups",
    "complete_shapename_other_slot_near_unit_groups",
    "complete_groups_slot1_near_unit_groups",
    "complete_groups_other_slot_near_unit_groups",
    "candidate_attribute_texture_valid",
    "candidate_raster_influence_valid",
    "unique_state_to_geometry_binding_proven_rows",
    "status",
    "semantic_binding",
    "original_semantics",
    "issue",
)

SUMMARY_FIELDS = (
    "state_source_report",
    "role_source_report",
    "attribute_source_report",
    "influence_source_report",
    "profile_source_report",
    "state_rows",
    "role_rows",
    "joined_primitive_groups",
    "candidate_joined_primitive_groups",
    "missing_state_keys",
    "missing_role_keys",
    "duplicate_state_keys",
    "duplicate_role_keys",
    "complete_five_slot_groups",
    "incomplete_five_slot_groups",
    "shapename_state_groups",
    "complete_shapename_state_groups",
    "complete_shapename_slot1_near_unit_groups",
    "complete_shapename_other_slot_near_unit_groups",
    "complete_groups_slot1_near_unit_groups",
    "complete_groups_other_slot_near_unit_groups",
    "candidate_object_rows",
    "candidate_attribute_texture_valid_rows",
    "candidate_raster_influence_valid_rows",
    "unique_state_to_geometry_binding_proven_rows",
    "valid_object_rows",
    "status",
    "semantic_binding",
    "provenance",
    "original_semantics",
)


def read_tsv(path: Path) -> tuple[tuple[str, ...], list[dict[str, str]]]:
    with path.open("r", encoding="utf-8-sig", newline="") as handle:
        reader = csv.DictReader(handle, delimiter="\t")
        if reader.fieldnames is None:
            raise ValueError(f"missing TSV header: {path}")
        return tuple(reader.fieldnames), list(reader)


def write_tsv(path: Path, fields: Sequence[str], rows: Iterable[dict[str, str]]) -> None:
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


def integer(row: dict[str, str], field: str) -> int:
    try:
        return int(row.get(field, ""), 0)
    except (TypeError, ValueError) as exc:
        raise ValueError(f"invalid integer {field}={row.get(field)!r}") from exc


def boolean(row: dict[str, str], field: str) -> bool:
    value = row.get(field, "")
    if value not in {"True", "False"}:
        raise ValueError(f"invalid boolean {field}={value!r}")
    return value == "True"


def primitive_key(row: dict[str, str]) -> tuple[str, str, str, str]:
    key = (
        row.get("model_archive", ""),
        row.get("model_object", ""),
        row.get("layer", ""),
        row.get("primitive", ""),
    )
    if not all(key):
        raise ValueError(f"empty primitive key: {key!r}")
    return key


def object_key(row: dict[str, str]) -> tuple[str, str]:
    key = (
        row.get("model_archive", ""),
        row.get("model_object", "") or row.get("object_name", ""),
    )
    if not all(key):
        raise ValueError(f"empty object key: {key!r}")
    return key


def checked_header(header: tuple[str, ...], required: Sequence[str], path: Path) -> None:
    missing = [field for field in required if field not in header]
    if missing:
        raise ValueError(f"{path} missing fields: {', '.join(missing)}")


def analyze(
    state_rows: list[dict[str, str]],
    role_rows: list[dict[str, str]],
    attribute_rows: list[dict[str, str]],
    influence_rows: list[dict[str, str]],
    profile_rows: list[dict[str, str]],
) -> tuple[list[dict[str, str]], dict[str, str]]:
    state_by_key: dict[tuple[str, str, str, str], dict[str, str]] = {}
    duplicate_state_keys = 0
    for row in state_rows:
        key = primitive_key(row)
        if key in state_by_key:
            duplicate_state_keys += 1
        state_by_key[key] = row

    role_by_key: dict[tuple[str, str, str, str], list[dict[str, str]]] = defaultdict(list)
    for row in role_rows:
        role_by_key[primitive_key(row)].append(row)

    duplicate_role_keys = sum(max(0, len(rows) - 5) for rows in role_by_key.values())
    joined_keys = sorted(set(state_by_key) & set(role_by_key))
    missing_state_keys = len(set(role_by_key) - set(state_by_key))
    missing_role_keys = len(set(state_by_key) - set(role_by_key))

    attribute_by_object: dict[tuple[str, str], dict[str, str]] = {}
    for row in attribute_rows:
        key = object_key(row)
        if key in attribute_by_object:
            raise ValueError(f"duplicate candidate attribute object: {key!r}")
        attribute_by_object[key] = row

    influence_by_object: dict[tuple[str, str], dict[str, str]] = {}
    for row in influence_rows:
        if row.get("kind") != "candidate":
            continue
        key = object_key(row)
        if key in influence_by_object:
            raise ValueError(f"duplicate candidate influence object: {key!r}")
        influence_by_object[key] = row

    profile_by_object: dict[tuple[str, str], dict[str, str]] = {}
    for row in profile_rows:
        key = object_key(row)
        if key in profile_by_object:
            raise ValueError(f"duplicate state-profile object: {key!r}")
        profile_by_object[key] = row

    groups_by_object: dict[tuple[str, str], list[tuple[dict[str, str], list[dict[str, str]]]]] = defaultdict(list)
    for key in joined_keys:
        groups_by_object[key[:2]].append((state_by_key[key], role_by_key[key]))

    report: list[dict[str, str]] = []
    total = defaultdict(int)
    valid_objects = 0
    for key in sorted(attribute_by_object):
        groups = groups_by_object.get(key, [])
        complete = 0
        shapename = 0
        complete_shapename = 0
        complete_shapename_slot1 = 0
        complete_shapename_other = 0
        complete_slot1 = 0
        complete_other = 0
        numeric_slot_count = 0
        object_issue = ""

        for state, roles in groups:
            numeric_slot_count += len(roles)
            state_status_ok = state.get("status") == STATE_STATUS
            role_status_ok = all(row.get("status") == ROLE_STATUS for row in roles)
            complete_group = (
                len(roles) == 5
                and state.get("complete_five_slot_fetch_sequence") == "True"
                and all(boolean(row, "complete_five_slot_group") for row in roles)
            )
            has_shape = boolean(state, "has_shapename_assignment")
            slot1 = any(boolean(row, "slot1_near_unit_float2_group") for row in roles)
            other = any(boolean(row, "other_slot_near_unit_float2_group") for row in roles)
            complete += int(complete_group)
            shapename += int(has_shape)
            complete_shapename += int(complete_group and has_shape)
            complete_shapename_slot1 += int(complete_group and has_shape and slot1)
            complete_shapename_other += int(complete_group and has_shape and other)
            complete_slot1 += int(complete_group and slot1)
            complete_other += int(complete_group and other)
            if not state_status_ok or not role_status_ok:
                object_issue = "input status mismatch"

        attribute = attribute_by_object[key]
        influence = influence_by_object.get(key)
        profile = profile_by_object.get(key)
        attribute_valid = (
            attribute.get("status") == ATTRIBUTE_STATUS
            and boolean(attribute, "attribute_texture_compatibility")
        )
        influence_valid = (
            influence is not None
            and influence.get("status") == INFLUENCE_STATUS
            and boolean(influence, "candidate_texture_influences_raster")
        )
        unique_geometry_rows = integer(profile, "unique_state_to_geometry_binding_proven_rows") if profile else -1
        valid = (
            len(groups) > 0
            and all(len(roles) in {0, 5} for _, roles in groups)
            and attribute_valid
            and influence_valid
            and profile is not None
            and profile.get("status") == PROFILE_STATUS
            and unique_geometry_rows == 0
            and complete_shapename_other == 0
        )
        if not valid and not object_issue:
            object_issue = "state/slot/candidate intersection controls require review"
        valid_objects += int(valid)
        values = {
            "model_archive": key[0],
            "model_object": key[1],
            "state_rows": str(len(groups)),
            "numeric_slot_rows": str(numeric_slot_count),
            "joined_primitive_groups": str(len(groups)),
            "complete_five_slot_groups": str(complete),
            "incomplete_five_slot_groups": str(len(groups) - complete),
            "shapename_state_groups": str(shapename),
            "complete_shapename_state_groups": str(complete_shapename),
            "complete_shapename_slot1_near_unit_groups": str(complete_shapename_slot1),
            "complete_shapename_other_slot_near_unit_groups": str(complete_shapename_other),
            "complete_groups_slot1_near_unit_groups": str(complete_slot1),
            "complete_groups_other_slot_near_unit_groups": str(complete_other),
            "candidate_attribute_texture_valid": str(attribute_valid),
            "candidate_raster_influence_valid": str(influence_valid),
            "unique_state_to_geometry_binding_proven_rows": str(unique_geometry_rows),
            "status": STATUS if valid else f"inconclusive-{STATUS}",
            "semantic_binding": SEMANTIC_BINDING,
            "original_semantics": ORIGINAL_SEMANTICS,
            "issue": object_issue,
        }
        report.append(values)
        for field in (
            "state_rows",
            "numeric_slot_rows",
            "joined_primitive_groups",
            "complete_five_slot_groups",
            "incomplete_five_slot_groups",
            "shapename_state_groups",
            "complete_shapename_state_groups",
            "complete_shapename_slot1_near_unit_groups",
            "complete_shapename_other_slot_near_unit_groups",
            "complete_groups_slot1_near_unit_groups",
            "complete_groups_other_slot_near_unit_groups",
            "unique_state_to_geometry_binding_proven_rows",
        ):
            total[field] += int(values[field])

    candidate_object_rows = len(attribute_by_object)
    attribute_valid_rows = sum(
        row["candidate_attribute_texture_valid"] == "True" for row in report
    )
    influence_valid_rows = sum(
        row["candidate_raster_influence_valid"] == "True" for row in report
    )
    summary = {
        "state_source_report": "",
        "role_source_report": "",
        "attribute_source_report": "",
        "influence_source_report": "",
        "profile_source_report": "",
        "state_rows": str(len(state_rows)),
        "role_rows": str(len(role_rows)),
        "joined_primitive_groups": str(len(joined_keys)),
        "candidate_joined_primitive_groups": str(total["joined_primitive_groups"]),
        "missing_state_keys": str(missing_state_keys),
        "missing_role_keys": str(missing_role_keys),
        "duplicate_state_keys": str(duplicate_state_keys),
        "duplicate_role_keys": str(duplicate_role_keys),
        "complete_five_slot_groups": str(total["complete_five_slot_groups"]),
        "incomplete_five_slot_groups": str(total["incomplete_five_slot_groups"]),
        "shapename_state_groups": str(total["shapename_state_groups"]),
        "complete_shapename_state_groups": str(total["complete_shapename_state_groups"]),
        "complete_shapename_slot1_near_unit_groups": str(total["complete_shapename_slot1_near_unit_groups"]),
        "complete_shapename_other_slot_near_unit_groups": str(total["complete_shapename_other_slot_near_unit_groups"]),
        "complete_groups_slot1_near_unit_groups": str(total["complete_groups_slot1_near_unit_groups"]),
        "complete_groups_other_slot_near_unit_groups": str(total["complete_groups_other_slot_near_unit_groups"]),
        "candidate_object_rows": str(candidate_object_rows),
        "candidate_attribute_texture_valid_rows": str(attribute_valid_rows),
        "candidate_raster_influence_valid_rows": str(influence_valid_rows),
        "unique_state_to_geometry_binding_proven_rows": str(total["unique_state_to_geometry_binding_proven_rows"]),
        "valid_object_rows": str(valid_objects),
        "status": STATUS if (
            len(state_rows) == 2364
            and len(role_rows) == 11820
            and len(joined_keys) == 2364
            and missing_state_keys == 0
            and missing_role_keys == 0
            and duplicate_state_keys == 0
            and duplicate_role_keys == 0
            and total["joined_primitive_groups"] == 218
            and total["complete_five_slot_groups"] == 216
            and total["incomplete_five_slot_groups"] == 2
            and total["shapename_state_groups"] == 180
            and total["complete_shapename_state_groups"] == 178
            and total["complete_shapename_slot1_near_unit_groups"] == 178
            and total["complete_shapename_other_slot_near_unit_groups"] == 0
            and total["complete_groups_slot1_near_unit_groups"] == 216
            and total["complete_groups_other_slot_near_unit_groups"] == 0
            and candidate_object_rows == 24
            and attribute_valid_rows == 24
            and influence_valid_rows == 24
            and total["unique_state_to_geometry_binding_proven_rows"] == 0
            and valid_objects == 24
            and all(row["status"] == STATUS for row in report)
        ) else f"inconclusive-{STATUS}",
        "semantic_binding": SEMANTIC_BINDING,
        "provenance": PROVENANCE,
        "original_semantics": ORIGINAL_SEMANTICS,
    }
    return report, summary


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("state_report", type=Path)
    parser.add_argument("role_report", type=Path)
    parser.add_argument("attribute_report", type=Path)
    parser.add_argument("influence_report", type=Path)
    parser.add_argument("profile_report", type=Path)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()

    state_header, state_rows = read_tsv(args.state_report)
    role_header, role_rows = read_tsv(args.role_report)
    attribute_header, attribute_rows = read_tsv(args.attribute_report)
    influence_header, influence_rows = read_tsv(args.influence_report)
    profile_header, profile_rows = read_tsv(args.profile_report)
    checked_header(
        state_header,
        ("model_archive", "model_object", "layer", "primitive", "status", "has_shapename_assignment", "complete_five_slot_fetch_sequence"),
        args.state_report,
    )
    checked_header(
        role_header,
        ("model_archive", "model_object", "layer", "primitive", "fetch_slot", "status", "complete_five_slot_group", "slot1_near_unit_float2_group", "other_slot_near_unit_float2_group"),
        args.role_report,
    )
    checked_header(
        attribute_header,
        ("model_archive", "model_object", "status", "attribute_texture_compatibility"),
        args.attribute_report,
    )
    checked_header(
        influence_header,
        ("kind", "model_archive", "model_object", "status", "candidate_texture_influences_raster"),
        args.influence_report,
    )
    checked_header(
        profile_header,
        ("model_archive", "object_name", "status", "unique_state_to_geometry_binding_proven_rows"),
        args.profile_report,
    )
    report, summary = analyze(state_rows, role_rows, attribute_rows, influence_rows, profile_rows)
    summary["state_source_report"] = str(args.state_report)
    summary["role_source_report"] = str(args.role_report)
    summary["attribute_source_report"] = str(args.attribute_report)
    summary["influence_source_report"] = str(args.influence_report)
    summary["profile_source_report"] = str(args.profile_report)
    write_tsv(args.report, REPORT_FIELDS, report)
    write_tsv(args.summary, SUMMARY_FIELDS, [summary])
    print("FIFA Street 2 independent state/slot1 candidate UV ambiguity control")
    for field in SUMMARY_FIELDS:
        print(f"{field}: {summary[field]}")
    print(f"report: {args.report}")
    print(f"summary: {args.summary}")
    return 0 if summary["status"] == STATUS else 1


if __name__ == "__main__":
    raise SystemExit(main())
