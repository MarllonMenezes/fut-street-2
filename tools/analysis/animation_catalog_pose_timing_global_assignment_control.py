"""Test whether animation catalog metadata reduces pose-bank domains.

This is an independent structural-negative control.  It reconciles catalog
timestamp rows, per-bank payload anchors, pose-bank rows and morph relocation
containment.  The reports have no pose-to-catalog key, so catalog timestamps
and anchors remain metadata only; they are never promoted to frame duration,
pose, rig, skinning, deformation or playback semantics.
"""

from __future__ import annotations

import argparse
import csv
from collections import Counter, defaultdict
from pathlib import Path
from typing import Dict, Iterable, List, Sequence


STATUS = "verified-independent-animation-catalog-pose-timing-global-assignment-control"
SEMANTIC_BINDING = "not-proven"
ORIGINAL_SEMANTICS = "not-claimed"
PROVENANCE = (
    "independent-TSV-reparse+catalog-timestamp-and-anchor-reconciliation+"
    "pose-bank-domain-cardinality+variant-morph-containment-control;no-"
    "timing-pose-rig-skinning-deformation-or-playback-promotion"
)

REPORT_FIELDS = (
    "nested_archive",
    "pose_palette_symbol",
    "normalized_name",
    "pose_catalog_join_key",
    "catalog_bank_count",
    "catalog_rows",
    "catalog_timestamp_valid_rows",
    "anchor_bank_rows",
    "anchor_complete_bank_rows",
    "anchor_count",
    "morph_layout_rows",
    "morph_parse_valid_rows",
    "morph_containing_layouts",
    "morph_containing_pairs",
    "morph_unique_containment_layouts",
    "morph_ambiguous_containment_layouts",
    "morph_exact_gap_set_pairs",
    "pose_bank_domain_size_before",
    "pose_bank_domain_size_after",
    "global_assignment_models_before",
    "global_assignment_models_after",
    "unique_pose_animation_binding_proven",
    "status",
    "semantic_binding",
    "original_semantics",
    "issue",
)

SUMMARY_FIELDS = (
    "timestamp_source_report",
    "anchor_source_report",
    "pose_source_report",
    "morph_source_report",
    "catalog_bank_count",
    "catalog_rows",
    "catalog_timestamp_valid_rows",
    "anchor_bank_rows",
    "anchor_complete_bank_rows",
    "anchor_count",
    "pose_rows",
    "distinct_pose_archives",
    "distinct_pose_symbols",
    "distinct_normalized_names",
    "morph_layout_rows",
    "morph_parse_valid_rows",
    "morph_containing_layouts",
    "morph_containing_pairs",
    "morph_unique_containment_layouts",
    "morph_ambiguous_containment_layouts",
    "morph_exact_gap_set_pairs",
    "pose_rows_without_catalog_join_key",
    "pose_rows_without_morph_join_key",
    "pose_bank_domain_size_before",
    "pose_bank_domain_size_after",
    "global_assignment_models_before",
    "global_assignment_models_after",
    "pose_bank_domain_reduced_rows",
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


def _bank_names(row: Dict[str, str]) -> List[str]:
    value = row.get("containing_animation_banks", "")
    if not value:
        return []
    return [name for name in value.split(",") if name]


def analyze(
    timestamp_rows: Sequence[Dict[str, str]],
    anchor_rows: Sequence[Dict[str, str]],
    pose_rows: Sequence[Dict[str, str]],
    morph_rows: Sequence[Dict[str, str]],
    timestamp_source: str,
    anchor_source: str,
    pose_source: str,
    morph_source: str,
) -> tuple[List[Dict[str, str]], Dict[str, str]]:
    if not timestamp_rows or not anchor_rows or not pose_rows or not morph_rows:
        raise ValueError("all source reports must contain rows")

    timestamp_banks = {row.get("bank_name", "") for row in timestamp_rows}
    catalog_rows = sum(_int(row, "catalog_rows") for row in timestamp_rows)
    catalog_timestamp_valid_rows = sum(
        _int(row, "exact_timestamp_date_rows")
        for row in timestamp_rows
        if row.get("status") == "verified-structural"
        and row.get("display_date_parseable_rows") == row.get("exact_timestamp_date_rows")
        and row.get("timestamp_date_mismatch_rows") == "0"
        and row.get("invalid_timestamp_rows") == "0"
    )

    anchor_banks = {row.get("bank_name", "") for row in anchor_rows}
    anchor_complete_bank_rows = sum(
        row.get("status") == "verified-structural"
        and row.get("tagged_rows") == row.get("catalog_rows")
        and row.get("rows_with_two_anchors") == row.get("catalog_rows")
        and row.get("strictly_increasing") == "True"
        for row in anchor_rows
    )
    anchor_count = sum(_int(row, "anchor_count") for row in anchor_rows)

    pose_archives = {row.get("nested_archive", "") for row in pose_rows}
    pose_symbols = {row.get("pose_palette_symbol", "") for row in pose_rows}
    normalized_names = {row.get("normalized_name", "") for row in pose_rows}
    pose_join_keys = {
        row.get("matching_catalog_entries", "")
        for row in pose_rows
        if row.get("matching_catalog_entries", "")
    }

    morph_parse_valid_rows = sum(row.get("parse_valid") == "True" for row in morph_rows)
    morph_containing_layouts = sum(bool(_bank_names(row)) for row in morph_rows)
    morph_containing_pairs = sum(len(_bank_names(row)) for row in morph_rows)
    morph_unique_containment_layouts = sum(len(_bank_names(row)) == 1 for row in morph_rows)
    morph_ambiguous_containment_layouts = sum(len(_bank_names(row)) > 1 for row in morph_rows)
    morph_exact_gap_set_pairs = sum(_int(row, "exact_gap_set_bank_count") for row in morph_rows)

    catalog_bank_count = len(timestamp_banks)
    pose_rows_without_catalog_join_key = sum(not row.get("matching_catalog_entries", "") for row in pose_rows)
    pose_rows_without_morph_join_key = len(pose_rows)
    pose_bank_domain_size_before = catalog_bank_count
    # No pose row carries a catalog or morph key.  Containment is not an exact
    # identity relation, so it cannot safely reduce any pose-bank domain.
    pose_bank_domain_size_after = pose_bank_domain_size_before
    global_assignment_models_before = len(pose_rows) * pose_bank_domain_size_before
    global_assignment_models_after = len(pose_rows) * pose_bank_domain_size_after
    pose_bank_domain_reduced_rows = 0
    unique_binding = 0

    common_valid = (
        len(timestamp_banks) == len(anchor_banks) == 19
        and all(timestamp_banks)
        and all(anchor_banks)
        and timestamp_banks == anchor_banks
        and len(timestamp_rows) == 19
        and catalog_rows == 1584
        and catalog_timestamp_valid_rows == catalog_rows
        and len(anchor_rows) == 19
        and anchor_complete_bank_rows == len(anchor_rows)
        and anchor_count == 3168
        and len(pose_archives) == len(pose_rows) == 391
        and all(pose_archives)
        and len(pose_symbols) == 1
        and len(normalized_names) == 1
        and len(morph_rows) == 393
        and morph_parse_valid_rows == len(morph_rows)
        and morph_containing_layouts == 53
        and morph_containing_pairs == 159
        and morph_unique_containment_layouts == 8
        and morph_ambiguous_containment_layouts == 45
        and morph_exact_gap_set_pairs == 0
        and not pose_join_keys
        and pose_rows_without_catalog_join_key == len(pose_rows)
        and pose_rows_without_morph_join_key == len(pose_rows)
        and pose_bank_domain_size_before == 19
        and pose_bank_domain_size_after == 19
        and global_assignment_models_before == global_assignment_models_after == 7429
        and pose_bank_domain_reduced_rows == 0
        and unique_binding == 0
    )

    issue = (
        "catalog-timestamps-and-anchors-are-metadata-only;"
        "pose-has-no-catalog-or-morph-join-key;"
        "morph-containment-is-not-exact-bank-identity"
    )
    report: List[Dict[str, str]] = []
    for row in pose_rows:
        row_valid = common_valid and bool(row.get("nested_archive", ""))
        report.append(
            {
                "nested_archive": row.get("nested_archive", ""),
                "pose_palette_symbol": row.get("pose_palette_symbol", ""),
                "normalized_name": row.get("normalized_name", ""),
                "pose_catalog_join_key": row.get("matching_catalog_entries", ""),
                "catalog_bank_count": str(catalog_bank_count),
                "catalog_rows": str(catalog_rows),
                "catalog_timestamp_valid_rows": str(catalog_timestamp_valid_rows),
                "anchor_bank_rows": str(len(anchor_rows)),
                "anchor_complete_bank_rows": str(anchor_complete_bank_rows),
                "anchor_count": str(anchor_count),
                "morph_layout_rows": str(len(morph_rows)),
                "morph_parse_valid_rows": str(morph_parse_valid_rows),
                "morph_containing_layouts": str(morph_containing_layouts),
                "morph_containing_pairs": str(morph_containing_pairs),
                "morph_unique_containment_layouts": str(morph_unique_containment_layouts),
                "morph_ambiguous_containment_layouts": str(morph_ambiguous_containment_layouts),
                "morph_exact_gap_set_pairs": str(morph_exact_gap_set_pairs),
                "pose_bank_domain_size_before": str(pose_bank_domain_size_before),
                "pose_bank_domain_size_after": str(pose_bank_domain_size_after),
                "global_assignment_models_before": str(global_assignment_models_before),
                "global_assignment_models_after": str(global_assignment_models_after),
                "unique_pose_animation_binding_proven": str(bool(unique_binding)),
                "status": STATUS if row_valid else f"inconclusive-{STATUS}",
                "semantic_binding": SEMANTIC_BINDING,
                "original_semantics": ORIGINAL_SEMANTICS,
                "issue": issue,
            }
        )

    summary = {
        "timestamp_source_report": timestamp_source,
        "anchor_source_report": anchor_source,
        "pose_source_report": pose_source,
        "morph_source_report": morph_source,
        "catalog_bank_count": str(catalog_bank_count),
        "catalog_rows": str(catalog_rows),
        "catalog_timestamp_valid_rows": str(catalog_timestamp_valid_rows),
        "anchor_bank_rows": str(len(anchor_rows)),
        "anchor_complete_bank_rows": str(anchor_complete_bank_rows),
        "anchor_count": str(anchor_count),
        "pose_rows": str(len(pose_rows)),
        "distinct_pose_archives": str(len(pose_archives)),
        "distinct_pose_symbols": str(len(pose_symbols)),
        "distinct_normalized_names": str(len(normalized_names)),
        "morph_layout_rows": str(len(morph_rows)),
        "morph_parse_valid_rows": str(morph_parse_valid_rows),
        "morph_containing_layouts": str(morph_containing_layouts),
        "morph_containing_pairs": str(morph_containing_pairs),
        "morph_unique_containment_layouts": str(morph_unique_containment_layouts),
        "morph_ambiguous_containment_layouts": str(morph_ambiguous_containment_layouts),
        "morph_exact_gap_set_pairs": str(morph_exact_gap_set_pairs),
        "pose_rows_without_catalog_join_key": str(pose_rows_without_catalog_join_key),
        "pose_rows_without_morph_join_key": str(pose_rows_without_morph_join_key),
        "pose_bank_domain_size_before": str(pose_bank_domain_size_before),
        "pose_bank_domain_size_after": str(pose_bank_domain_size_after),
        "global_assignment_models_before": str(global_assignment_models_before),
        "global_assignment_models_after": str(global_assignment_models_after),
        "pose_bank_domain_reduced_rows": str(pose_bank_domain_reduced_rows),
        "unique_pose_animation_binding_proven_rows": str(unique_binding),
        "valid_pose_rows": str(len(pose_rows) if common_valid else 0),
        "status": STATUS if common_valid else f"inconclusive-{STATUS}",
        "semantic_binding": SEMANTIC_BINDING,
        "provenance": PROVENANCE,
        "original_semantics": ORIGINAL_SEMANTICS,
    }
    return report, summary


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("timestamp_report", type=Path)
    parser.add_argument("anchor_report", type=Path)
    parser.add_argument("pose_report", type=Path)
    parser.add_argument("morph_report", type=Path)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()

    report, summary = analyze(
        _read(args.timestamp_report),
        _read(args.anchor_report),
        _read(args.pose_report),
        _read(args.morph_report),
        str(args.timestamp_report),
        str(args.anchor_report),
        str(args.pose_report),
        str(args.morph_report),
    )
    _write(args.report, REPORT_FIELDS, report)
    _write(args.summary, SUMMARY_FIELDS, [summary])
    if summary["status"] != STATUS:
        raise SystemExit(f"control failed: {summary['status']}")
    print(
        f"status={summary['status']} pose_rows={summary['pose_rows']} "
        f"catalog_rows={summary['catalog_rows']} "
        f"morph_containing_pairs={summary['morph_containing_pairs']} "
        f"pose_bank_domain_reduced_rows={summary['pose_bank_domain_reduced_rows']}"
    )


if __name__ == "__main__":
    main()
