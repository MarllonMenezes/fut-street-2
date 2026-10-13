"""Compare two raw EAGL state-assignment populations without assigning semantics.

The source report contains bounded rows from the same-trunk original EAGL
object/descriptor chain.  This control compares only rows carrying the
observed SHAPENAME and SetPrimitiveType assignment labels.  It records
co-occurrence and repeated structural shapes per raw object, but it does not
interpret either label as geometry, material, UV, skinning, rigging,
animation, morph or GPU state.
"""

from __future__ import annotations

import argparse
import csv
from collections import defaultdict
from itertools import product
from pathlib import Path
from typing import Iterable


STATUS = "verified-bounded-original-state-population-control"
ORIGINAL_SEMANTICS = "not-claimed"
PROVENANCE = (
    "same-trunk-raw-EAGL-render-chain-report;population-only;"
    "candidate-mesh-not-used;no-semantic-promotion"
)

REPORT_FIELDS = (
    "model_archive",
    "model_object",
    "primitive_rows",
    "shapename_rows",
    "primitive_type_rows",
    "population_overlap_rows",
    "same_primitive_index_pairs",
    "adjacent_primitive_pairs",
    "same_layer_pairs",
    "exact_command_hash_pairs",
    "exact_boundary_shape_pairs",
    "complete_shapename_rows",
    "complete_primitive_type_rows",
    "status",
    "original_semantics",
    "issue",
)

SUMMARY_FIELDS = (
    "source_report",
    "object_rows",
    "primitive_rows",
    "shapename_rows",
    "primitive_type_rows",
    "population_overlap_rows",
    "same_primitive_index_pairs",
    "adjacent_primitive_pairs",
    "same_layer_pairs",
    "exact_command_hash_pairs",
    "exact_boundary_shape_pairs",
    "complete_shapename_rows",
    "complete_primitive_type_rows",
    "objects_with_rows",
    "status",
    "provenance",
    "original_semantics",
)


def _as_int(value: str, field: str) -> int:
    try:
        return int(value, 0)
    except (TypeError, ValueError) as exc:
        raise ValueError(f"invalid integer in {field}: {value!r}") from exc


def _as_bool(value: str, field: str) -> bool:
    if value == "True":
        return True
    if value == "False":
        return False
    raise ValueError(f"invalid boolean in {field}: {value!r}")


def _boundary_shape(row: dict[str, str]) -> tuple[str, str, str]:
    return (
        row.get("command_count", ""),
        row.get("command_bytes", ""),
        row.get("command_stream_complete", ""),
    )


def _pair_metrics(
    shapename_rows: list[dict[str, str]],
    primitive_type_rows: list[dict[str, str]],
) -> dict[str, int]:
    pairs = list(product(shapename_rows, primitive_type_rows))
    return {
        "same_primitive_index_pairs": sum(
            _as_int(left["primitive"], "primitive") == _as_int(right["primitive"], "primitive")
            for left, right in pairs
        ),
        "adjacent_primitive_pairs": sum(
            abs(
                _as_int(left["primitive"], "primitive")
                - _as_int(right["primitive"], "primitive")
            )
            == 1
            for left, right in pairs
        ),
        "same_layer_pairs": sum(
            _as_int(left["layer"], "layer") == _as_int(right["layer"], "layer")
            for left, right in pairs
        ),
        "exact_command_hash_pairs": sum(
            bool(left.get("command_bytes_sha256"))
            and left.get("command_bytes_sha256") == right.get("command_bytes_sha256")
            for left, right in pairs
        ),
        "exact_boundary_shape_pairs": sum(
            _boundary_shape(left) == _boundary_shape(right)
            for left, right in pairs
        ),
    }


def analyze_rows(source_rows: Iterable[dict[str, str]]) -> tuple[list[dict[str, str]], dict[str, str]]:
    groups: dict[tuple[str, str], list[dict[str, str]]] = defaultdict(list)
    primitive_rows = 0
    shapename_rows = 0
    primitive_type_rows = 0
    population_overlap_rows = 0
    complete_shapename_rows = 0
    complete_primitive_type_rows = 0

    for row in source_rows:
        if row.get("status") != "verified-bounded-original-render-chain-structure":
            raise ValueError(f"source row is not verified raw structure: {row.get('status')!r}")
        if row.get("original_semantics") != ORIGINAL_SEMANTICS:
            raise ValueError("source row has claimed original semantics")
        archive = row.get("model_archive", "")
        object_name = row.get("model_object", "")
        if not archive or not object_name:
            raise ValueError("source row has an empty raw object identity")
        _as_int(row.get("layer", ""), "layer")
        _as_int(row.get("primitive", ""), "primitive")
        shapename = _as_bool(row.get("has_shapename_assignment", ""), "has_shapename_assignment")
        primitive_type = _as_bool(
            row.get("has_primitive_type_assignment", ""),
            "has_primitive_type_assignment",
        )
        complete = _as_bool(row.get("command_stream_complete", ""), "command_stream_complete")
        primitive_rows += 1
        shapename_rows += shapename
        primitive_type_rows += primitive_type
        population_overlap_rows += shapename and primitive_type
        complete_shapename_rows += shapename and complete
        complete_primitive_type_rows += primitive_type and complete
        groups[(archive, object_name)].append(row)

    report_rows: list[dict[str, str]] = []
    totals = {
        "same_primitive_index_pairs": 0,
        "adjacent_primitive_pairs": 0,
        "same_layer_pairs": 0,
        "exact_command_hash_pairs": 0,
        "exact_boundary_shape_pairs": 0,
    }
    for (archive, object_name), rows in sorted(groups.items()):
        shapes = [row for row in rows if row["has_shapename_assignment"] == "True"]
        primitive_types = [
            row for row in rows if row["has_primitive_type_assignment"] == "True"
        ]
        overlap = sum(
            row["has_shapename_assignment"] == "True"
            and row["has_primitive_type_assignment"] == "True"
            for row in rows
        )
        metrics = _pair_metrics(shapes, primitive_types)
        for key in totals:
            totals[key] += metrics[key]
        report_rows.append(
            {
                "model_archive": archive,
                "model_object": object_name,
                "primitive_rows": str(len(rows)),
                "shapename_rows": str(len(shapes)),
                "primitive_type_rows": str(len(primitive_types)),
                "population_overlap_rows": str(overlap),
                **{key: str(value) for key, value in metrics.items()},
                "complete_shapename_rows": str(
                    sum(
                        row["has_shapename_assignment"] == "True"
                        and row["command_stream_complete"] == "True"
                        for row in rows
                    )
                ),
                "complete_primitive_type_rows": str(
                    sum(
                        row["has_primitive_type_assignment"] == "True"
                        and row["command_stream_complete"] == "True"
                        for row in rows
                    )
                ),
                "status": STATUS,
                "original_semantics": ORIGINAL_SEMANTICS,
                "issue": "",
            }
        )

    summary = {
        "source_report": "",
        "object_rows": str(len(report_rows)),
        "primitive_rows": str(primitive_rows),
        "shapename_rows": str(shapename_rows),
        "primitive_type_rows": str(primitive_type_rows),
        "population_overlap_rows": str(population_overlap_rows),
        **{key: str(value) for key, value in totals.items()},
        "complete_shapename_rows": str(complete_shapename_rows),
        "complete_primitive_type_rows": str(complete_primitive_type_rows),
        "objects_with_rows": str(len(groups)),
        "status": STATUS,
        "provenance": PROVENANCE,
        "original_semantics": ORIGINAL_SEMANTICS,
    }
    if not report_rows or population_overlap_rows != 0:
        raise ValueError("raw state populations are missing or overlap")
    return report_rows, summary


def write_tsv(path: Path, fields: Iterable[str], rows: Iterable[dict[str, str]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=list(fields), delimiter="\t", lineterminator="\n")
        writer.writeheader()
        writer.writerows({field: row.get(field, "") for field in fields} for row in rows)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source_report", type=Path)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()

    with args.source_report.open("r", encoding="utf-8", newline="") as handle:
        source_rows = list(csv.DictReader(handle, delimiter="\t"))
    report_rows, summary = analyze_rows(source_rows)
    summary["source_report"] = str(args.source_report)
    write_tsv(args.report, REPORT_FIELDS, report_rows)
    write_tsv(args.summary, SUMMARY_FIELDS, [summary])
    print("FIFA Street 2 raw EAGL state-population control")
    for key in SUMMARY_FIELDS:
        print(f"{key}: {summary[key]}")
    print(f"report: {args.report}")
    print(f"summary: {args.summary}")
    print("note: SHAPENAME and SetPrimitiveType remain observed labels only; no binding was assigned")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
