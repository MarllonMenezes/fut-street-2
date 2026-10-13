"""Independently cross-check opaque candidate attributes against textures.

The control joins the managed candidate attribute report with the independent
same-trunk candidate source-chain report.  It checks only finite, normalized
numeric ranges, vertex/value cardinality, positive texture dimensions, PAL8
metadata and decoded-pixel equality.  The slot and attribute names remain
opaque: this does not identify UVs, materials, texture sampling, original
render-state bindings or GPU semantics.
"""

from __future__ import annotations

import argparse
import csv
import math
import re
from pathlib import Path
from typing import Dict, Iterable, List, Sequence, Tuple


STATUS = "verified-independent-candidate-attribute-texture-compatibility-control"
ATTRIBUTE_STATUS = "complete-slot1-profile"
ASSET_STATUS = "verified-candidate-source-chain"
SEMANTIC_BINDING = "not-proven"
ORIGINAL_SEMANTICS = "not-claimed"

REPORT_FIELDS = (
    "model_archive",
    "model_object",
    "shape_name",
    "attribute_input_valid",
    "asset_input_valid",
    "texture_dimensions_valid",
    "texture_pal8",
    "decoded_pixel_match",
    "slot1_range_groups",
    "slot1_finite_range_groups",
    "slot1_normalized_range_groups",
    "slot1_value_count",
    "vertex_count",
    "slot1_value_count_match_vertex",
    "attribute_texture_compatibility",
    "status",
    "semantic_binding",
    "original_semantics",
    "issue",
)

SUMMARY_FIELDS = (
    "attribute_object_rows",
    "asset_object_rows",
    "joined_object_rows",
    "attribute_input_status_valid_rows",
    "asset_input_status_valid_rows",
    "texture_dimensions_valid_rows",
    "texture_pal8_rows",
    "decoded_pixel_match_rows",
    "slot1_range_rows",
    "slot1_finite_range_rows",
    "slot1_normalized_range_rows",
    "slot1_value_count_match_vertex_rows",
    "attribute_texture_compatibility_rows",
    "semantic_binding",
    "status",
    "provenance",
    "original_semantics",
)

RANGE_RE = re.compile(
    r"count=(?P<count>\d+);min=(?P<minx>[^,;]+),(?P<miny>[^;]+);"
    r"max=(?P<maxx>[^,;]+),(?P<maxy>[^|]+)"
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


def _key(
    row: Dict[str, str],
    archive_field: str = "model_archive",
    object_field: str = "model_object",
) -> Tuple[str, str, str]:
    return (
        row.get(archive_field, ""),
        row.get(object_field, ""),
        row.get("shape_name", ""),
    )


def _int(row: Dict[str, str], field: str) -> int:
    return int(row.get(field, "0"))


def _true(row: Dict[str, str], field: str) -> bool:
    return row.get(field, "").strip().lower() == "true"


def _parse_ranges(signature: str) -> Tuple[int, int, int, int]:
    parts = [part for part in signature.split("|") if part]
    finite = 0
    normalized = 0
    total_values = 0
    valid = 0
    for part in parts:
        match = RANGE_RE.search(part)
        if match is None:
            continue
        try:
            count = int(match.group("count"))
            values = [
                float(match.group("minx")),
                float(match.group("miny")),
                float(match.group("maxx")),
                float(match.group("maxy")),
            ]
        except ValueError:
            continue
        valid += 1
        total_values += count
        if all(math.isfinite(value) for value in values):
            finite += 1
        if (
            all(math.isfinite(value) and 0.0 <= value <= 1.0 for value in values)
            and values[0] <= values[2]
            and values[1] <= values[3]
        ):
            normalized += 1
    return valid, finite, normalized, total_values


def analyze(
    attribute_rows: Sequence[Dict[str, str]],
    asset_rows: Sequence[Dict[str, str]],
) -> Tuple[List[Dict[str, str]], Dict[str, str]]:
    attributes = {_key(row): row for row in attribute_rows}
    assets = {
        _key(row, "model_archive_name", "model_object_name"): row
        for row in asset_rows
    }
    keys = sorted(set(attributes) | set(assets))
    report: List[Dict[str, str]] = []
    attribute_valid_rows = 0
    asset_valid_rows = 0
    texture_dimensions_valid_rows = 0
    texture_pal8_rows = 0
    decoded_pixel_match_rows = 0
    slot1_range_rows = 0
    slot1_finite_range_rows = 0
    slot1_normalized_range_rows = 0
    value_count_match_rows = 0
    compatibility_rows = 0

    for key in keys:
        attribute = attributes.get(key)
        asset = assets.get(key)
        attribute_valid = bool(
            attribute
            and attribute.get("status") == ATTRIBUTE_STATUS
            and _int(attribute, "groups") > 0
            and _int(attribute, "vertices") > 0
            and _int(attribute, "slot1_complete_groups") == _int(attribute, "groups")
            and _int(attribute, "slot1_finite_groups") == _int(attribute, "groups")
            and _int(attribute, "slot1_near_unit_groups") == _int(attribute, "groups")
        )
        asset_valid = bool(
            asset
            and asset.get("status") == ASSET_STATUS
            and _true(asset, "candidate_bounds_valid")
            and _true(asset, "candidate_geometry_finite_and_indexed")
            and _true(asset, "shape_payload_valid")
            and _true(asset, "shape_entry_exact")
            and _true(asset, "decoded_pixels_match")
        )
        if attribute_valid:
            attribute_valid_rows += 1
        if asset_valid:
            asset_valid_rows += 1

        dimensions_valid = bool(
            asset
            and _int(asset, "texture_width") > 0
            and _int(asset, "texture_height") > 0
        )
        pal8 = bool(asset and asset.get("pixel_format") == "PAL8")
        decoded_match = bool(asset and _true(asset, "decoded_pixels_match"))
        if dimensions_valid:
            texture_dimensions_valid_rows += 1
        if pal8:
            texture_pal8_rows += 1
        if decoded_match:
            decoded_pixel_match_rows += 1

        range_groups = finite_groups = normalized_groups = value_count = 0
        if attribute:
            range_groups, finite_groups, normalized_groups, value_count = _parse_ranges(
                attribute.get("slot1_range_signature", "")
            )
        range_ok = bool(attribute and range_groups == _int(attribute, "groups"))
        finite_ok = bool(attribute and range_ok and finite_groups == range_groups)
        normalized_ok = bool(attribute and finite_ok and normalized_groups == range_groups)
        if range_ok:
            slot1_range_rows += 1
        if finite_ok:
            slot1_finite_range_rows += 1
        if normalized_ok:
            slot1_normalized_range_rows += 1
        vertex_count = _int(attribute, "vertices") if attribute else 0
        value_count_match = bool(attribute and value_count == vertex_count)
        if value_count_match:
            value_count_match_rows += 1

        compatibility = bool(
            attribute
            and asset
            and attribute_valid
            and asset_valid
            and dimensions_valid
            and pal8
            and decoded_match
            and normalized_ok
            and value_count_match
        )
        if compatibility:
            compatibility_rows += 1
        report.append(
            {
                "model_archive": key[0],
                "model_object": key[1],
                "shape_name": key[2],
                "attribute_input_valid": str(attribute_valid),
                "asset_input_valid": str(asset_valid),
                "texture_dimensions_valid": str(dimensions_valid),
                "texture_pal8": str(pal8),
                "decoded_pixel_match": str(decoded_match),
                "slot1_range_groups": str(range_groups),
                "slot1_finite_range_groups": str(finite_groups),
                "slot1_normalized_range_groups": str(normalized_groups),
                "slot1_value_count": str(value_count),
                "vertex_count": str(vertex_count),
                "slot1_value_count_match_vertex": str(value_count_match),
                "attribute_texture_compatibility": str(compatibility),
                "status": STATUS if compatibility else "inconclusive-" + STATUS,
                "semantic_binding": SEMANTIC_BINDING,
                "original_semantics": ORIGINAL_SEMANTICS,
                "issue": "" if compatibility else "candidate attribute/texture structural compatibility did not reconcile",
            }
        )

    joined_keys = set(attributes) & set(assets)
    provenance = (
        "independent-TSV-object-key-join+opaque-attribute-range+candidate-texture-dimension/"
        "decoded-pixel-control;no-UV-or-material-semantic-promotion"
    )
    summary = {
        "attribute_object_rows": str(len(attribute_rows)),
        "asset_object_rows": str(len(asset_rows)),
        "joined_object_rows": str(len(joined_keys)),
        "attribute_input_status_valid_rows": str(attribute_valid_rows),
        "asset_input_status_valid_rows": str(asset_valid_rows),
        "texture_dimensions_valid_rows": str(texture_dimensions_valid_rows),
        "texture_pal8_rows": str(texture_pal8_rows),
        "decoded_pixel_match_rows": str(decoded_pixel_match_rows),
        "slot1_range_rows": str(slot1_range_rows),
        "slot1_finite_range_rows": str(slot1_finite_range_rows),
        "slot1_normalized_range_rows": str(slot1_normalized_range_rows),
        "slot1_value_count_match_vertex_rows": str(value_count_match_rows),
        "attribute_texture_compatibility_rows": str(compatibility_rows),
        "semantic_binding": SEMANTIC_BINDING,
        "status": STATUS if (
            len(attribute_rows) == 24
            and len(asset_rows) == 24
            and len(joined_keys) == 24
            and attribute_valid_rows == asset_valid_rows == 24
            and texture_dimensions_valid_rows == texture_pal8_rows == decoded_pixel_match_rows == 24
            and slot1_range_rows == slot1_finite_range_rows == slot1_normalized_range_rows == 24
            and value_count_match_rows == compatibility_rows == 24
            and all(row["status"] == STATUS for row in report)
        ) else "inconclusive-" + STATUS,
        "provenance": provenance,
        "original_semantics": ORIGINAL_SEMANTICS,
    }
    return report, summary


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--attribute-report", type=Path, required=True)
    parser.add_argument("--asset-report", type=Path, required=True)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()

    report, summary = analyze(_read(args.attribute_report), _read(args.asset_report))
    _write(args.report, REPORT_FIELDS, report)
    _write(args.summary, SUMMARY_FIELDS, [summary])
    for field in SUMMARY_FIELDS:
        print(f"{field}: {summary[field]}")
    print(f"report: {args.report}")
    print(f"summary: {args.summary}")
    return 0 if summary["status"] == STATUS else 1


if __name__ == "__main__":
    raise SystemExit(main())
