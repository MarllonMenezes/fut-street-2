"""Reconcile pose-bank, animation-bank and morph-layout evidence.

This is an independent structural-negative control.  It checks that the
pose-bank index, animation-bank ELF profiles and morph relocation profiles
cover the expected populations, while preserving the unresolved result:
the generic pose symbol does not name an animation bank and morph offsets
only produce containment, not an exact bank identity.  No pose, frame, rig,
timing or skinning semantics are inferred.
"""

from __future__ import annotations

import argparse
import csv
from pathlib import Path
from typing import Dict, Iterable, List, Sequence


STATUS = "verified-independent-animation-pose-bank-ambiguity-control"
SEMANTIC_BINDING = "not-proven"
ORIGINAL_SEMANTICS = "not-claimed"
PROVENANCE = (
    "independent-TSV-reparse+population-reconciliation+symbol-metadata-negative-"
    "control+morph-containment-vs-exact-gap-control;no-pose-frame-rig-timing-"
    "or-skinning-promotion"
)

REPORT_FIELDS = (
    "nested_archive",
    "pose_palette_symbol",
    "normalized_name",
    "symbol_table_section_index",
    "symbol_index",
    "symbol_section_index",
    "symbol_value",
    "symbol_size",
    "symbol_info",
    "matches_animation_bank_name",
    "matches_animation_symbol_metadata",
    "animation_bank_rows",
    "animation_bank_valid_rows",
    "morph_layout_rows",
    "morph_parse_valid_rows",
    "morph_containing_pairs",
    "morph_exact_gap_set_layouts",
    "morph_exact_gap_set_pairs",
    "morph_exact_gap_histogram_layouts",
    "morph_exact_gap_histogram_pairs",
    "unique_pose_animation_binding_proven",
    "status",
    "semantic_binding",
    "original_semantics",
    "issue",
)

SUMMARY_FIELDS = (
    "pose_source_report",
    "animation_source_report",
    "morph_source_report",
    "pose_rows",
    "distinct_pose_archives",
    "distinct_pose_symbols",
    "distinct_normalized_names",
    "animation_bank_rows",
    "distinct_animation_banks",
    "animation_bank_valid_rows",
    "animation_bank_symbol_metadata_rows",
    "morph_layout_rows",
    "morph_parse_valid_rows",
    "morph_containing_pairs",
    "morph_exact_gap_set_layouts",
    "morph_exact_gap_set_pairs",
    "morph_exact_gap_histogram_layouts",
    "morph_exact_gap_histogram_pairs",
    "pose_name_match_rows",
    "pose_symbol_metadata_match_rows",
    "unique_pose_animation_binding_proven_rows",
    "valid_pose_rows",
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
    pose_rows: Sequence[Dict[str, str]],
    animation_rows: Sequence[Dict[str, str]],
    morph_rows: Sequence[Dict[str, str]],
    pose_source: str,
    animation_source: str,
    morph_source: str,
) -> tuple[List[Dict[str, str]], Dict[str, str]]:
    if not pose_rows or not animation_rows or not morph_rows:
        raise ValueError("all source reports must contain rows")

    distinct_pose_archives = {row.get("nested_archive", "") for row in pose_rows}
    distinct_pose_symbols = {row.get("pose_palette_symbol", "") for row in pose_rows}
    distinct_normalized_names = {row.get("normalized_name", "") for row in pose_rows}
    distinct_animation_banks = {row.get("bank_name", "") for row in animation_rows}

    animation_bank_valid_rows = sum(
        row.get("elf_valid") == "True" for row in animation_rows
    )
    animation_bank_symbol_metadata_rows = sum(
        bool(row.get("animation_bank_symbol_metadata", ""))
        and row.get("animation_bank_symbol_metadata", "").startswith(
            f"__AnimationBank:::{row.get('bank_name', '')};"
        )
        for row in animation_rows
    )
    morph_parse_valid_rows = sum(
        row.get("parse_valid") == "True" for row in morph_rows
    )
    morph_containing_pairs = sum(
        _int(row, "containing_animation_bank_count") for row in morph_rows
    )
    morph_exact_gap_set_layouts = sum(
        _int(row, "exact_gap_set_bank_count") > 0 for row in morph_rows
    )
    morph_exact_gap_set_pairs = sum(
        _int(row, "exact_gap_set_bank_count") for row in morph_rows
    )
    morph_exact_gap_histogram_layouts = sum(
        _int(row, "exact_gap_histogram_bank_count") > 0 for row in morph_rows
    )
    morph_exact_gap_histogram_pairs = sum(
        _int(row, "exact_gap_histogram_bank_count") for row in morph_rows
    )
    pose_name_match_rows = sum(
        _bool(row, "matches_animation_bank_name") for row in pose_rows
    )
    pose_symbol_metadata_match_rows = sum(
        _bool(row, "matches_animation_symbol_metadata") for row in pose_rows
    )
    unique_binding_proven = (
        pose_name_match_rows > 0
        or pose_symbol_metadata_match_rows > 0
        or morph_exact_gap_set_pairs > 0
        or morph_exact_gap_histogram_pairs > 0
    )

    common_valid = (
        len(distinct_pose_archives) == len(pose_rows)
        and all(distinct_pose_archives)
        and len(distinct_pose_symbols) == 1
        and all(distinct_pose_symbols)
        and len(distinct_normalized_names) == 1
        and distinct_normalized_names == {"PosePaletteBank"}
        and len(distinct_animation_banks) == len(animation_rows)
        and all(distinct_animation_banks)
        and animation_bank_valid_rows == len(animation_rows)
        and animation_bank_symbol_metadata_rows == len(animation_rows)
        and morph_parse_valid_rows == len(morph_rows)
        and morph_exact_gap_set_layouts == 0
        and morph_exact_gap_set_pairs == 0
        and morph_exact_gap_histogram_layouts == 0
        and morph_exact_gap_histogram_pairs == 0
        and not unique_binding_proven
    )

    report: List[Dict[str, str]] = []
    for row in pose_rows:
        row_valid = common_valid and row.get("nested_archive", "") != ""
        report.append(
            {
                "nested_archive": row.get("nested_archive", ""),
                "pose_palette_symbol": row.get("pose_palette_symbol", ""),
                "normalized_name": row.get("normalized_name", ""),
                "symbol_table_section_index": row.get("symbol_table_section_index", ""),
                "symbol_index": row.get("symbol_index", ""),
                "symbol_section_index": row.get("symbol_section_index", ""),
                "symbol_value": row.get("symbol_value", ""),
                "symbol_size": row.get("symbol_size", ""),
                "symbol_info": row.get("symbol_info", ""),
                "matches_animation_bank_name": row.get("matches_animation_bank_name", ""),
                "matches_animation_symbol_metadata": row.get(
                    "matches_animation_symbol_metadata", ""
                ),
                "animation_bank_rows": str(len(animation_rows)),
                "animation_bank_valid_rows": str(animation_bank_valid_rows),
                "morph_layout_rows": str(len(morph_rows)),
                "morph_parse_valid_rows": str(morph_parse_valid_rows),
                "morph_containing_pairs": str(morph_containing_pairs),
                "morph_exact_gap_set_layouts": str(morph_exact_gap_set_layouts),
                "morph_exact_gap_set_pairs": str(morph_exact_gap_set_pairs),
                "morph_exact_gap_histogram_layouts": str(
                    morph_exact_gap_histogram_layouts
                ),
                "morph_exact_gap_histogram_pairs": str(
                    morph_exact_gap_histogram_pairs
                ),
                "unique_pose_animation_binding_proven": str(unique_binding_proven),
                "status": STATUS if row_valid else f"inconclusive-{STATUS}",
                "semantic_binding": SEMANTIC_BINDING,
                "original_semantics": ORIGINAL_SEMANTICS,
                "issue": (
                    "generic-pose-symbol-does-not-identify-animation-bank;"
                    "morph-containment-is-not-exact-bank-identity"
                ),
            }
        )

    summary = {
        "pose_source_report": pose_source,
        "animation_source_report": animation_source,
        "morph_source_report": morph_source,
        "pose_rows": str(len(pose_rows)),
        "distinct_pose_archives": str(len(distinct_pose_archives)),
        "distinct_pose_symbols": str(len(distinct_pose_symbols)),
        "distinct_normalized_names": str(len(distinct_normalized_names)),
        "animation_bank_rows": str(len(animation_rows)),
        "distinct_animation_banks": str(len(distinct_animation_banks)),
        "animation_bank_valid_rows": str(animation_bank_valid_rows),
        "animation_bank_symbol_metadata_rows": str(animation_bank_symbol_metadata_rows),
        "morph_layout_rows": str(len(morph_rows)),
        "morph_parse_valid_rows": str(morph_parse_valid_rows),
        "morph_containing_pairs": str(morph_containing_pairs),
        "morph_exact_gap_set_layouts": str(morph_exact_gap_set_layouts),
        "morph_exact_gap_set_pairs": str(morph_exact_gap_set_pairs),
        "morph_exact_gap_histogram_layouts": str(morph_exact_gap_histogram_layouts),
        "morph_exact_gap_histogram_pairs": str(morph_exact_gap_histogram_pairs),
        "pose_name_match_rows": str(pose_name_match_rows),
        "pose_symbol_metadata_match_rows": str(pose_symbol_metadata_match_rows),
        "unique_pose_animation_binding_proven_rows": str(int(unique_binding_proven)),
        "valid_pose_rows": str(len(pose_rows) if common_valid else 0),
        "status": STATUS if common_valid else f"inconclusive-{STATUS}",
        "semantic_binding": SEMANTIC_BINDING,
        "provenance": PROVENANCE,
        "original_semantics": ORIGINAL_SEMANTICS,
    }
    return report, summary


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("pose_report", type=Path)
    parser.add_argument("animation_report", type=Path)
    parser.add_argument("morph_report", type=Path)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()

    report, summary = analyze(
        _read(args.pose_report),
        _read(args.animation_report),
        _read(args.morph_report),
        str(args.pose_report),
        str(args.animation_report),
        str(args.morph_report),
    )
    _write(args.report, REPORT_FIELDS, report)
    _write(args.summary, SUMMARY_FIELDS, [summary])
    if summary["status"] != STATUS:
        raise SystemExit(f"control failed: {summary['status']}")
    print(
        f"status={summary['status']} pose_rows={summary['pose_rows']} "
        f"animation_bank_rows={summary['animation_bank_rows']} "
        f"morph_layout_rows={summary['morph_layout_rows']} "
        f"morph_containing_pairs={summary['morph_containing_pairs']}"
    )


if __name__ == "__main__":
    main()
