"""Independently reparse the original EAGL state-token inventory.

This control validates literal vocabulary and row metadata only. It never
assigns a token to geometry, UV, material, skinning, rigging, animation,
morphs, or GPU behavior.
"""

from __future__ import annotations

import argparse
import csv
from pathlib import Path
from typing import Dict, Iterable, List, Sequence, Tuple


STATUS = "verified-independent-original-state-token-control"
ORIGINAL_SEMANTICS = "not-claimed"
INPUT_STATUS = "verified-bounded-original-render-chain-structure"

TEXTURE_TOKENS = ("texture", "tex", "sampler", "image", "diffuse", "albedo")
MATERIAL_TOKENS = ("material", "shader", "surface", "diffuse", "albedo")
UV_TOKENS = ("uv", "texcoord", "tex_coord", "texturecoordinate", "texture_coordinate")
VERTEX_TOKENS = ("vertex", "position", "normal", "tangent", "binormal", "color", "attribute")
DEFORMATION_TOKENS = ("skin", "weight", "bone", "rig", "morph", "deform", "palette")


def _int(row: Dict[str, str], key: str) -> int:
    return int(row[key])


def _names(row: Dict[str, str]) -> Tuple[str, ...]:
    return tuple(name for name in row["assignment_names"].split(",") if name)


def _hits(names: Iterable[str], tokens: Sequence[str]) -> int:
    text = ";".join(names).lower()
    return sum(token in text for token in tokens)


def _bool(value: str) -> bool:
    return value.lower() == "true"


def analyze_rows(rows: Sequence[Dict[str, str]]) -> Tuple[List[Dict[str, str]], Dict[str, str]]:
    report: List[Dict[str, str]] = []
    catalogs = set()
    texture_rows = material_rows = uv_rows = vertex_rows = deformation_rows = 0
    state_rows = shape_rows = clamp_u_rows = clamp_v_rows = primitive_type_rows = 0

    for row in rows:
        names = _names(row)
        catalogs.update(names)
        text_names = tuple(name.lower() for name in names)
        texture_hits = _hits(names, TEXTURE_TOKENS)
        material_hits = _hits(names, MATERIAL_TOKENS)
        uv_hits = _hits(names, UV_TOKENS)
        vertex_hits = _hits(names, VERTEX_TOKENS)
        deformation_hits = _hits(names, DEFORMATION_TOKENS)
        expected_shape = "SHAPENAME" in names
        expected_clamp_u = "XBOXEXTOBJ_SetClampU" in names
        expected_clamp_v = "XBOXEXTOBJ_SetClampV" in names
        expected_primitive_type = "SetPrimitiveType" in names
        assignment_match = _int(row, "assignment_count") == len(names)
        flags_match = (
            _bool(row["has_shapename_assignment"]) == expected_shape
            and _bool(row["has_clamp_u_assignment"]) == expected_clamp_u
            and _bool(row["has_clamp_v_assignment"]) == expected_clamp_v
            and _bool(row["has_primitive_type_assignment"]) == expected_primitive_type
        )
        input_status_valid = row["input_status"] == INPUT_STATUS
        negative_zero = all(
            value == 0
            for value in (texture_hits, material_hits, uv_hits, vertex_hits, deformation_hits)
        )
        row_status = (
            STATUS
            if assignment_match and flags_match and input_status_valid and negative_zero
            else "inconclusive-original-state-token-control"
        )
        state_rows += int(_int(row, "assignment_count") > 0)
        shape_rows += int(expected_shape)
        clamp_u_rows += int(expected_clamp_u)
        clamp_v_rows += int(expected_clamp_v)
        primitive_type_rows += int(expected_primitive_type)
        texture_rows += int(texture_hits > 0)
        material_rows += int(material_hits > 0)
        uv_rows += int(uv_hits > 0)
        vertex_rows += int(vertex_hits > 0)
        deformation_rows += int(deformation_hits > 0)
        report.append(
            {
                "model_archive": row["model_archive"],
                "model_object": row["model_object"],
                "layer": row["layer"],
                "primitive": row["primitive"],
                "assignment_count": row["assignment_count"],
                "assignment_name_reparse_match": str(assignment_match),
                "classification_flags_match": str(flags_match),
                "input_status_valid": str(input_status_valid),
                "negative_token_counts_zero": str(negative_zero),
                "texture_token_hits": str(texture_hits),
                "material_token_hits": str(material_hits),
                "uv_coordinate_token_hits": str(uv_hits),
                "vertex_attribute_token_hits": str(vertex_hits),
                "deformation_token_hits": str(deformation_hits),
                "status": row_status,
                "original_semantics": ORIGINAL_SEMANTICS,
            }
        )

    input_rows = len(rows)
    valid_rows = sum(item["status"] == STATUS for item in report)
    summary = {
        "source_report": "",
        "input_rows": str(input_rows),
        "state_rows": str(state_rows),
        "valid_input_rows": str(sum(row["input_status"] == INPUT_STATUS for row in rows)),
        "assignment_name_reparse_match_rows": str(
            sum(item["assignment_name_reparse_match"] == "True" for item in report)
        ),
        "classification_flags_match_rows": str(
            sum(item["classification_flags_match"] == "True" for item in report)
        ),
        "negative_token_counts_zero_rows": str(
            sum(item["negative_token_counts_zero"] == "True" for item in report)
        ),
        "valid_rows": str(valid_rows),
        "shapename_rows": str(shape_rows),
        "clamp_u_rows": str(clamp_u_rows),
        "clamp_v_rows": str(clamp_v_rows),
        "primitive_type_rows": str(primitive_type_rows),
        "texture_token_rows": str(texture_rows),
        "material_token_rows": str(material_rows),
        "uv_coordinate_token_rows": str(uv_rows),
        "vertex_attribute_token_rows": str(vertex_rows),
        "deformation_token_rows": str(deformation_rows),
        "assignment_name_catalog": ",".join(sorted(catalogs)),
        "status": STATUS if input_rows > 0 and valid_rows == input_rows else "inconclusive-original-state-token-control",
        "provenance": "independent-TSV-reparse+literal-vocabulary-coherence;original-state-negative-control;no-semantic-promotion",
        "original_semantics": ORIGINAL_SEMANTICS,
    }
    return report, summary


def _read(path: Path) -> List[Dict[str, str]]:
    with path.open("r", encoding="utf-8-sig", newline="") as handle:
        return list(csv.DictReader(handle, delimiter="\t"))


def _write(path: Path, fieldnames: Sequence[str], rows: Sequence[Dict[str, str]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=fieldnames, delimiter="\t", lineterminator="\n")
        writer.writeheader()
        writer.writerows(rows)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("source_report", type=Path)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()
    source_rows = _read(args.source_report)
    report, summary = analyze_rows(source_rows)
    summary["source_report"] = str(args.source_report)
    _write(args.report, list(report[0]) if report else ["status"], report)
    summary_fields = [
        "source_report", "input_rows", "state_rows", "valid_input_rows",
        "assignment_name_reparse_match_rows", "classification_flags_match_rows",
        "negative_token_counts_zero_rows", "valid_rows", "shapename_rows",
        "clamp_u_rows", "clamp_v_rows", "primitive_type_rows", "texture_token_rows",
        "material_token_rows", "uv_coordinate_token_rows", "vertex_attribute_token_rows",
        "deformation_token_rows", "assignment_name_catalog", "status", "provenance",
        "original_semantics",
    ]
    _write(args.summary, summary_fields, [summary])
    print("FIFA Street 2 independent original EAGL state-token control")
    for key in (
        "input_rows", "state_rows", "valid_input_rows", "assignment_name_reparse_match_rows",
        "classification_flags_match_rows", "negative_token_counts_zero_rows", "valid_rows",
        "texture_token_rows", "material_token_rows", "uv_coordinate_token_rows",
        "vertex_attribute_token_rows", "deformation_token_rows", "status",
    ):
        print(f"{key}: {summary[key]}")
    return 0 if summary["status"] == STATUS else 1


if __name__ == "__main__":
    raise SystemExit(main())
