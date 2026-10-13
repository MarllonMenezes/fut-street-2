"""Independently cross-check raw state/fetch population against exact adjacency.

This control joins only primitive identity keys from the original render-chain
report and the independent command/__VB adjacency report. It records
co-occurrence of state text, complete fetch topology and raw offset adjacency;
it does not assign geometry, UV, material, vertex-attribute, skinning, rigging,
animation, morph, command or GPU semantics.
"""

from __future__ import annotations

import argparse
import csv
from collections import Counter
from pathlib import Path
from typing import Dict, Iterable, List, Sequence, Tuple


STATUS = "verified-independent-original-state-fetch-adjacency-control"
RENDER_STATUS = "verified-bounded-original-render-chain-structure"
ADJACENCY_STATUS = "verified-independent-original-primitive-stream-command-descriptor-adjacency-control"
ADJACENCY_ROW_STATUS = "verified-bounded-original-primitive-stream-command-descriptor-adjacency"
ORIGINAL_SEMANTICS = "not-claimed"
PROVENANCE = (
    "independent-TSV-reparse+primitive-key-join+raw-state/fetch-population+"
    "exact-command-descriptor-adjacency-cooccurrence;no-semantic-promotion"
)

REPORT_FIELDS = (
    "model_archive",
    "model_object",
    "layer",
    "primitive",
    "state_relocation_present",
    "has_shapename_assignment",
    "has_primitive_type_assignment",
    "complete_five_slot_fetch_sequence",
    "state_fetch_overlap_reported",
    "state_shape_primitive_type_overlap_reported",
    "exact_adjacency_stream_count",
    "exact_adjacency_stream_categories",
    "exact_adjacency_stream_strides",
    "render_input_status",
    "adjacency_input_status",
    "status",
    "original_semantics",
    "issue",
)

SUMMARY_FIELDS = (
    "render_source_report",
    "adjacency_source_report",
    "render_rows",
    "adjacency_rows",
    "joined_rows",
    "render_input_status_valid_rows",
    "adjacency_input_status_valid_rows",
    "state_relocation_rows",
    "shapename_rows",
    "primitive_type_rows",
    "complete_five_slot_fetch_rows",
    "reported_state_fetch_overlap_rows",
    "recomputed_state_fetch_overlap_rows",
    "reported_state_shape_primitive_type_overlap_rows",
    "recomputed_state_shape_primitive_type_overlap_rows",
    "joined_exact_adjacency_rows",
    "joined_state_exact_adjacency_rows",
    "joined_state_fetch_exact_adjacency_rows",
    "joined_shapename_exact_adjacency_rows",
    "joined_primitive_type_exact_adjacency_rows",
    "exact_adjacency_category_histogram",
    "state_exact_adjacency_category_histogram",
    "status",
    "provenance",
    "original_semantics",
)


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


def _key(row: Dict[str, str]) -> Tuple[str, str, int, int]:
    archive = row.get("model_archive", "")
    object_name = row.get("model_object", "")
    if not archive or not object_name:
        raise ValueError("primitive row has empty object identity")
    return archive, object_name, _int(row, "layer"), _int(row, "primitive")


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


def _histogram(values: Iterable[int]) -> str:
    counts = Counter(values)
    return ",".join(f"{key}:{counts[key]}" for key in sorted(counts))


def _categories(value: str) -> List[int]:
    result: List[int] = []
    for item in value.split(","):
        if not item:
            continue
        _, category = item.split(":", 1)
        result.append(int(category, 0))
    return result


def analyze(
    render_rows: Sequence[Dict[str, str]],
    adjacency_rows: Sequence[Dict[str, str]],
) -> Tuple[List[Dict[str, str]], Dict[str, str]]:
    if not render_rows or not adjacency_rows:
        raise ValueError("render-chain and adjacency reports must both contain rows")

    render_required = {
        "model_archive",
        "model_object",
        "layer",
        "primitive",
        "state_relocation_present",
        "has_shapename_assignment",
        "has_primitive_type_assignment",
        "complete_five_slot_fetch_sequence",
        "state_fetch_overlap",
        "state_shapename_primitive_type_overlap",
        "status",
        "original_semantics",
    }
    adjacency_required = {
        "model_archive",
        "model_object",
        "layer",
        "primitive",
        "exact_adjacency_stream_count",
        "exact_adjacency_stream_categories",
        "exact_adjacency_stream_strides",
        "render_input_status",
        "status",
        "original_semantics",
    }
    missing_render = render_required - set(render_rows[0])
    missing_adjacency = adjacency_required - set(adjacency_rows[0])
    if missing_render:
        raise ValueError(f"render-chain report is missing columns: {sorted(missing_render)}")
    if missing_adjacency:
        raise ValueError(f"adjacency report is missing columns: {sorted(missing_adjacency)}")

    render_by_key = {_key(row): row for row in render_rows}
    adjacency_by_key = {_key(row): row for row in adjacency_rows}
    keys = sorted(set(render_by_key) & set(adjacency_by_key))
    render_input_status_valid_rows = sum(
        row.get("status") == RENDER_STATUS for row in render_rows
    )
    adjacency_input_status_valid_rows = sum(
        row.get("status") == ADJACENCY_ROW_STATUS for row in adjacency_rows
    )

    report: List[Dict[str, str]] = []
    state_relocation_rows = 0
    shapename_rows = 0
    primitive_type_rows = 0
    complete_five_slot_fetch_rows = 0
    reported_state_fetch_overlap_rows = 0
    recomputed_state_fetch_overlap_rows = 0
    reported_state_shape_primitive_type_overlap_rows = 0
    recomputed_state_shape_primitive_type_overlap_rows = 0
    joined_exact_adjacency_rows = 0
    joined_state_exact_adjacency_rows = 0
    joined_state_fetch_exact_adjacency_rows = 0
    joined_shapename_exact_adjacency_rows = 0
    joined_primitive_type_exact_adjacency_rows = 0
    all_exact_categories: List[int] = []
    state_exact_categories: List[int] = []

    for key in keys:
        render = render_by_key[key]
        adjacency = adjacency_by_key[key]
        state_relocation = _bool(render, "state_relocation_present")
        shapename = _bool(render, "has_shapename_assignment")
        primitive_type = _bool(render, "has_primitive_type_assignment")
        complete_fetch = _bool(render, "complete_five_slot_fetch_sequence")
        reported_state_fetch = _bool(render, "state_fetch_overlap")
        reported_state_shape_primitive_type = _bool(
            render, "state_shapename_primitive_type_overlap"
        )
        exact_count = _int(adjacency, "exact_adjacency_stream_count")
        exact_categories = _categories(adjacency.get("exact_adjacency_stream_categories", ""))
        exact = exact_count > 0
        recomputed_state_fetch = shapename and complete_fetch
        recomputed_state_shape_primitive_type = shapename and primitive_type

        state_relocation_rows += int(state_relocation)
        shapename_rows += int(shapename)
        primitive_type_rows += int(primitive_type)
        complete_five_slot_fetch_rows += int(complete_fetch)
        reported_state_fetch_overlap_rows += int(reported_state_fetch)
        recomputed_state_fetch_overlap_rows += int(recomputed_state_fetch)
        reported_state_shape_primitive_type_overlap_rows += int(
            reported_state_shape_primitive_type
        )
        recomputed_state_shape_primitive_type_overlap_rows += int(
            recomputed_state_shape_primitive_type
        )
        joined_exact_adjacency_rows += int(exact)
        joined_state_exact_adjacency_rows += int(state_relocation and exact)
        joined_state_fetch_exact_adjacency_rows += int(state_relocation and complete_fetch and exact)
        joined_shapename_exact_adjacency_rows += int(shapename and exact)
        joined_primitive_type_exact_adjacency_rows += int(primitive_type and exact)
        all_exact_categories.extend(exact_categories)
        if state_relocation:
            state_exact_categories.extend(exact_categories)

        row_status = STATUS if (
            render.get("original_semantics") == ORIGINAL_SEMANTICS
            and adjacency.get("original_semantics") == ORIGINAL_SEMANTICS
            and render.get("status") == RENDER_STATUS
            and adjacency.get("status") == ADJACENCY_ROW_STATUS
            and exact
        ) else "inconclusive-independent-original-state-fetch-adjacency-control"
        report.append(
            {
                "model_archive": key[0],
                "model_object": key[1],
                "layer": str(key[2]),
                "primitive": str(key[3]),
                "state_relocation_present": str(state_relocation),
                "has_shapename_assignment": str(shapename),
                "has_primitive_type_assignment": str(primitive_type),
                "complete_five_slot_fetch_sequence": str(complete_fetch),
                "state_fetch_overlap_reported": str(reported_state_fetch),
                "state_shape_primitive_type_overlap_reported": str(
                    reported_state_shape_primitive_type
                ),
                "exact_adjacency_stream_count": str(exact_count),
                "exact_adjacency_stream_categories": adjacency.get(
                    "exact_adjacency_stream_categories", ""
                ),
                "exact_adjacency_stream_strides": adjacency.get(
                    "exact_adjacency_stream_strides", ""
                ),
                "render_input_status": render.get("status", ""),
                "adjacency_input_status": adjacency.get("status", ""),
                "status": row_status,
                "original_semantics": ORIGINAL_SEMANTICS,
                "issue": "" if row_status == STATUS else "joined raw inputs were incomplete",
            }
        )

    summary = {
        "render_source_report": "",
        "adjacency_source_report": "",
        "render_rows": str(len(render_rows)),
        "adjacency_rows": str(len(adjacency_rows)),
        "joined_rows": str(len(keys)),
        "render_input_status_valid_rows": str(render_input_status_valid_rows),
        "adjacency_input_status_valid_rows": str(adjacency_input_status_valid_rows),
        "state_relocation_rows": str(state_relocation_rows),
        "shapename_rows": str(shapename_rows),
        "primitive_type_rows": str(primitive_type_rows),
        "complete_five_slot_fetch_rows": str(complete_five_slot_fetch_rows),
        "reported_state_fetch_overlap_rows": str(reported_state_fetch_overlap_rows),
        "recomputed_state_fetch_overlap_rows": str(recomputed_state_fetch_overlap_rows),
        "reported_state_shape_primitive_type_overlap_rows": str(
            reported_state_shape_primitive_type_overlap_rows
        ),
        "recomputed_state_shape_primitive_type_overlap_rows": str(
            recomputed_state_shape_primitive_type_overlap_rows
        ),
        "joined_exact_adjacency_rows": str(joined_exact_adjacency_rows),
        "joined_state_exact_adjacency_rows": str(joined_state_exact_adjacency_rows),
        "joined_state_fetch_exact_adjacency_rows": str(joined_state_fetch_exact_adjacency_rows),
        "joined_shapename_exact_adjacency_rows": str(joined_shapename_exact_adjacency_rows),
        "joined_primitive_type_exact_adjacency_rows": str(
            joined_primitive_type_exact_adjacency_rows
        ),
        "exact_adjacency_category_histogram": _histogram(all_exact_categories),
        "state_exact_adjacency_category_histogram": _histogram(state_exact_categories),
        "status": STATUS if (
            len(keys) == len(render_rows) == len(adjacency_rows)
            and render_input_status_valid_rows == len(render_rows)
            and adjacency_input_status_valid_rows == len(adjacency_rows)
            and joined_exact_adjacency_rows == len(keys)
            and reported_state_fetch_overlap_rows == recomputed_state_fetch_overlap_rows
            and reported_state_shape_primitive_type_overlap_rows
            == recomputed_state_shape_primitive_type_overlap_rows
            and all(row.get("original_semantics") == ORIGINAL_SEMANTICS for row in report)
        ) else "inconclusive-independent-original-state-fetch-adjacency-control",
        "provenance": PROVENANCE,
        "original_semantics": ORIGINAL_SEMANTICS,
    }
    return report, summary


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("render_report", type=Path)
    parser.add_argument("adjacency_report", type=Path)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()
    report, summary = analyze(_read(args.render_report), _read(args.adjacency_report))
    summary["render_source_report"] = str(args.render_report)
    summary["adjacency_source_report"] = str(args.adjacency_report)
    _write(args.report, REPORT_FIELDS, report)
    _write(args.summary, SUMMARY_FIELDS, [summary])
    print("FIFA Street 2 independent original state/fetch adjacency control")
    for field in SUMMARY_FIELDS:
        print(f"{field}: {summary[field]}")
    print(f"report: {args.report}")
    print(f"summary: {args.summary}")
    return 0 if summary["status"] == STATUS else 1


if __name__ == "__main__":
    raise SystemExit(main())
