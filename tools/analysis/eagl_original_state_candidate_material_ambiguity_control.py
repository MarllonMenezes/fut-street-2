"""Measure original state-to-candidate-material ambiguity without promotion.

This control independently joins the bounded original render-chain rows, the
ordered state/geometry intervals, the original SHAPENAME-to-candidate asset
control and the candidate source-chain audit.  It verifies that state rows
carry a same-object candidate texture source and records how many geometry
primitives are adjacent to each state interval.  Adjacency and SHAPENAME are
not treated as a draw or material binding: the direct state-to-geometry and
state-to-material bindings remain explicitly unproven.
"""

from __future__ import annotations

import argparse
import csv
import re
from pathlib import Path
from typing import Dict, Iterable, List, Sequence, Set, Tuple


STATUS = "verified-independent-original-state-candidate-material-ambiguity-control"
ORDER_STATUS = "ordered-index-control"
RENDER_STATUS = "verified-bounded-original-render-chain-structure"
STATE_ASSET_STATUS = "verified-independent-original-shapename-candidate-asset-control"
CANDIDATE_STATUS = "verified-candidate-source-chain"
SEMANTIC_BINDING = "not-proven"
ORIGINAL_SEMANTICS = "not-claimed"
PROVENANCE = (
    "independent-TSV-object-join+ordered-state-interval-reparse+SHAPENAME-"
    "candidate-source-chain+adjacency-cardinality;no-state-draw-or-material-"
    "promotion"
)

OBJECT_FIELDS = ("model_archive", "model_object")
KEY_FIELDS = ("model_archive", "model_object", "layer", "primitive")

REPORT_FIELDS = (
    "model_archive",
    "model_object",
    "layer",
    "interval_index",
    "interval_kind",
    "previous_geometry_primitive",
    "next_geometry_primitive",
    "state_primitive_indices",
    "state_primitive_count",
    "adjacent_geometry_candidate_count",
    "state_rows_joined",
    "state_shape_token_match_rows",
    "state_rows_with_candidate_asset_source",
    "candidate_shape_name",
    "texture_width",
    "texture_height",
    "candidate_asset_valid",
    "state_to_geometry_unique_binding_proven",
    "state_to_material_unique_binding_proven",
    "status",
    "semantic_binding",
    "original_semantics",
    "issue",
)

SUMMARY_FIELDS = (
    "render_source_report",
    "order_source_report",
    "state_asset_source_report",
    "candidate_asset_source_report",
    "candidate_object_rows",
    "render_scoped_rows",
    "order_rows",
    "order_state_rows",
    "original_geometry_rows",
    "original_state_rows",
    "nonempty_state_interval_rows",
    "intervals_with_two_adjacent_geometry_candidates",
    "intervals_with_one_adjacent_geometry_candidate",
    "intervals_with_zero_adjacent_geometry_candidates",
    "state_rows_joined",
    "state_rows_with_shape_token_match",
    "state_rows_with_candidate_asset_source",
    "candidate_asset_valid_rows",
    "object_asset_join_rows",
    "objects_with_complete_ambiguity_control",
    "original_input_status_valid_rows",
    "order_input_status_valid_rows",
    "state_asset_input_status_valid_rows",
    "candidate_asset_input_status_valid_rows",
    "state_to_geometry_unique_binding_proven_rows",
    "state_to_material_unique_binding_proven_rows",
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


def _object_key(row: Dict[str, str]) -> Tuple[str, str]:
    values = (
        row.get("model_archive", "") or row.get("model_archive_name", ""),
        row.get("model_object", "")
        or row.get("object_name", "")
        or row.get("model_object_name", ""),
    )
    if not all(values):
        raise ValueError(f"empty object identity: {values!r}")
    return values


def _key(row: Dict[str, str]) -> Tuple[str, str, str, str]:
    values = tuple(row.get(field, "") for field in KEY_FIELDS)
    if not all(values):
        raise ValueError(f"empty primitive identity: {values!r}")
    return values


def _bool(row: Dict[str, str], field: str) -> bool:
    value = row.get(field, "")
    if value not in {"True", "False"}:
        raise ValueError(f"invalid boolean in {field}: {value!r}")
    return value == "True"


def _int(row: Dict[str, str], field: str) -> int:
    try:
        return int(row.get(field, ""), 0)
    except (TypeError, ValueError) as exc:
        raise ValueError(f"invalid integer in {field}: {row.get(field)!r}") from exc


def _split_indices(value: str, default_layer: str) -> List[Tuple[str, str]]:
    if not value:
        return []
    result: List[Tuple[str, str]] = []
    for item in value.split(","):
        parts = item.split(":")
        if len(parts) == 1 and item:
            result.append((default_layer, item))
            continue
        if len(parts) != 2 or not all(parts):
            raise ValueError(f"invalid state primitive index: {item!r}")
        result.append((parts[0], parts[1]))
    return result


def _shape_token(row: Dict[str, str]) -> str:
    assignments = row.get("state_assignments", "")
    matches = re.findall(r"(?:^|;)SHAPENAME=([^;]+)", assignments)
    if len(matches) != 1:
        raise ValueError(f"expected one SHAPENAME assignment: {assignments!r}")
    return matches[0]


def _texture_dimensions(row: Dict[str, str]) -> Tuple[int, int]:
    width = _int(row, "texture_width")
    height = _int(row, "texture_height")
    if width <= 0 or height <= 0:
        raise ValueError(f"invalid texture dimensions: {width}x{height}")
    return width, height


def analyze(
    render_rows: Sequence[Dict[str, str]],
    order_rows: Sequence[Dict[str, str]],
    state_asset_rows: Sequence[Dict[str, str]],
    candidate_asset_rows: Sequence[Dict[str, str]],
) -> Tuple[List[Dict[str, str]], Dict[str, str]]:
    if not all((render_rows, order_rows, state_asset_rows, candidate_asset_rows)):
        raise ValueError("all four source reports must contain rows")

    candidate_objects = {_object_key(row) for row in candidate_asset_rows}
    scoped_render = [row for row in render_rows if _object_key(row) in candidate_objects]
    scoped_order = [row for row in order_rows if _object_key(row) in candidate_objects]
    state_asset_by_object = {_object_key(row): row for row in state_asset_rows}
    candidate_asset_by_object = {_object_key(row): row for row in candidate_asset_rows}
    if len(state_asset_by_object) != len(state_asset_rows):
        raise ValueError("state asset source has duplicate object keys")
    if len(candidate_asset_by_object) != len(candidate_asset_rows):
        raise ValueError("candidate asset source has duplicate object keys")

    render_by_key: Dict[Tuple[str, str, str, str], Dict[str, str]] = {}
    geometry_by_object_layer: Dict[Tuple[str, str, str], Set[str]] = {}
    state_rows_by_object: Dict[Tuple[str, str], Dict[Tuple[str, str], Dict[str, str]]] = {}
    original_geometry_rows = 0
    original_state_rows = 0
    original_input_valid_rows = 0
    for row in scoped_render:
        key = _key(row)
        if key in render_by_key:
            raise ValueError(f"duplicate render primitive key: {key!r}")
        render_by_key[key] = row
        valid = (
            row.get("status") == RENDER_STATUS
            and row.get("original_semantics") == ORIGINAL_SEMANTICS
        )
        original_input_valid_rows += int(valid)
        primitive_type = _bool(row, "has_primitive_type_assignment")
        shapename = _bool(row, "has_shapename_assignment")
        complete_fetch = _bool(row, "complete_five_slot_fetch_sequence")
        if primitive_type and shapename:
            raise ValueError(f"overlapping state/geometry marker: {key!r}")
        object_key = _object_key(row)
        layer_key = (object_key[0], object_key[1], row["layer"])
        if primitive_type and complete_fetch:
            geometry_by_object_layer.setdefault(layer_key, set()).add(row["primitive"])
            original_geometry_rows += 1
        if shapename and not primitive_type:
            state_rows_by_object.setdefault(object_key, {})[(row["layer"], row["primitive"])] = row
            original_state_rows += 1

    order_input_valid_rows = 0
    state_asset_input_valid_rows = 0
    candidate_asset_input_valid_rows = 0
    for row in scoped_order:
        order_input_valid_rows += int(row.get("status") == ORDER_STATUS)
    for row in state_asset_rows:
        state_asset_input_valid_rows += int(
            row.get("status") == STATE_ASSET_STATUS
            and row.get("original_semantics") == ORIGINAL_SEMANTICS
        )
    for row in candidate_asset_rows:
        candidate_asset_input_valid_rows += int(
            row.get("status") == CANDIDATE_STATUS
            and row.get("candidate_provenance", "").startswith("same-trunk-source-chain")
            and _bool(row, "same_trunk")
            and _bool(row, "shape_entry_exact")
            and _bool(row, "decoded_pixels_match")
            and row.get("original_semantics") == ORIGINAL_SEMANTICS
        )

    counters = {
        "nonempty_state_interval_rows": 0,
        "intervals_with_two_adjacent_geometry_candidates": 0,
        "intervals_with_one_adjacent_geometry_candidate": 0,
        "intervals_with_zero_adjacent_geometry_candidates": 0,
        "order_state_rows": 0,
        "state_rows_joined": 0,
        "state_rows_with_shape_token_match": 0,
        "state_rows_with_candidate_asset_source": 0,
        "candidate_asset_valid_rows": 0,
        "object_asset_join_rows": 0,
        "objects_with_complete_ambiguity_control": 0,
        "state_to_geometry_unique_binding_proven_rows": 0,
        "state_to_material_unique_binding_proven_rows": 0,
    }
    report: List[Dict[str, str]] = []
    object_rows: Dict[Tuple[str, str], List[Dict[str, str]]] = {}
    object_order_state_counts: Dict[Tuple[str, str], int] = {}

    for object_key in sorted(candidate_objects):
        state_asset = state_asset_by_object.get(object_key)
        candidate_asset = candidate_asset_by_object.get(object_key)
        if state_asset is None or candidate_asset is None:
            raise ValueError(f"missing object asset join: {object_key!r}")
        counters["object_asset_join_rows"] += 1
        candidate_asset_valid = (
            candidate_asset.get("status") == CANDIDATE_STATUS
            and candidate_asset.get("candidate_provenance", "").startswith("same-trunk-source-chain")
            and _bool(candidate_asset, "same_trunk")
            and _bool(candidate_asset, "shape_entry_exact")
            and _bool(candidate_asset, "decoded_pixels_match")
            and candidate_asset.get("original_semantics") == ORIGINAL_SEMANTICS
        )
        counters["candidate_asset_valid_rows"] += int(candidate_asset_valid)
        candidate_shape_name = candidate_asset.get("shape_name", "")
        texture_width, texture_height = _texture_dimensions(candidate_asset)
        object_state_rows = state_rows_by_object.get(object_key, {})

        object_order = [row for row in scoped_order if _object_key(row) == object_key]
        object_order_state_counts[object_key] = sum(
            _int(row, "state_primitive_count") for row in object_order
        )
        object_report_rows: List[Dict[str, str]] = []
        for order in sorted(object_order, key=lambda row: _int(row, "interval_index")):
            indices = _split_indices(order.get("state_primitive_indices", ""), order.get("layer", ""))
            state_count = _int(order, "state_primitive_count")
            if state_count != len(indices):
                raise ValueError(f"state count/index mismatch: {object_key!r}")
            counters["order_state_rows"] += state_count
            nonempty = state_count > 0
            counters["nonempty_state_interval_rows"] += int(nonempty)
            previous = order.get("previous_geometry_primitive", "")
            next_geometry = order.get("next_geometry_primitive", "")
            adjacent = {value for value in (previous, next_geometry) if value}
            adjacent_count = len(adjacent)
            if nonempty:
                if adjacent_count == 2:
                    counters["intervals_with_two_adjacent_geometry_candidates"] += 1
                elif adjacent_count == 1:
                    counters["intervals_with_one_adjacent_geometry_candidate"] += 1
                else:
                    counters["intervals_with_zero_adjacent_geometry_candidates"] += 1

            joined = 0
            shape_matches = 0
            source_rows = 0
            issues: List[str] = []
            for layer, primitive in indices:
                state = object_state_rows.get((layer, primitive))
                if state is None:
                    issues.append(f"missing-state:{layer}:{primitive}")
                    continue
                joined += 1
                counters["state_rows_joined"] += 1
                if state_asset.get("object_state_exact") != "True":
                    issues.append("state-asset-object-identity-not-exact")
                if _shape_token(state) == f"{candidate_shape_name},1":
                    shape_matches += 1
                    counters["state_rows_with_shape_token_match"] += 1
                else:
                    issues.append(f"state-shapename-mismatch:{_shape_token(state)}")
                if candidate_asset_valid:
                    source_rows += 1
                    counters["state_rows_with_candidate_asset_source"] += 1
            valid = (
                order.get("status") == ORDER_STATUS
                and candidate_asset_valid
                and state_asset.get("status") == STATE_ASSET_STATUS
                and state_asset.get("original_semantics") == ORIGINAL_SEMANTICS
                and joined == state_count
                and shape_matches == state_count
            )
            object_report_rows.append(
                {
                    "model_archive": object_key[0],
                    "model_object": object_key[1],
                    "layer": order.get("layer", ""),
                    "interval_index": order.get("interval_index", ""),
                    "interval_kind": order.get("interval_kind", ""),
                    "previous_geometry_primitive": previous,
                    "next_geometry_primitive": next_geometry,
                    "state_primitive_indices": order.get("state_primitive_indices", ""),
                    "state_primitive_count": str(state_count),
                    "adjacent_geometry_candidate_count": str(adjacent_count),
                    "state_rows_joined": str(joined),
                    "state_shape_token_match_rows": str(shape_matches),
                    "state_rows_with_candidate_asset_source": str(source_rows),
                    "candidate_shape_name": candidate_shape_name,
                    "texture_width": str(texture_width),
                    "texture_height": str(texture_height),
                    "candidate_asset_valid": str(candidate_asset_valid),
                    "state_to_geometry_unique_binding_proven": "False",
                    "state_to_material_unique_binding_proven": "False",
                    "status": STATUS if valid else "inconclusive-" + STATUS,
                    "semantic_binding": SEMANTIC_BINDING,
                    "original_semantics": ORIGINAL_SEMANTICS,
                    "issue": ";".join(issues),
                }
            )
        object_rows[object_key] = object_report_rows

    for object_key, rows in object_rows.items():
        order_state_count = object_order_state_counts[object_key]
        state_rows = len(state_rows_by_object.get(object_key, {}))
        object_complete = (
            order_state_count == state_rows
            and all(row["status"] == STATUS for row in rows)
            and any(int(row["state_primitive_count"]) > 0 for row in rows)
        )
        counters["objects_with_complete_ambiguity_control"] += int(object_complete)
        report.extend(rows)

    if counters["order_state_rows"] != original_state_rows:
        raise ValueError(
            f"order/render state population mismatch: {counters['order_state_rows']} != {original_state_rows}"
        )

    summary = {
        "render_source_report": "",
        "order_source_report": "",
        "state_asset_source_report": "",
        "candidate_asset_source_report": "",
        "candidate_object_rows": str(len(candidate_objects)),
        "render_scoped_rows": str(len(scoped_render)),
        "order_rows": str(len(scoped_order)),
        "order_state_rows": str(counters["order_state_rows"]),
        "original_geometry_rows": str(original_geometry_rows),
        "original_state_rows": str(original_state_rows),
        "nonempty_state_interval_rows": str(counters["nonempty_state_interval_rows"]),
        "intervals_with_two_adjacent_geometry_candidates": str(counters["intervals_with_two_adjacent_geometry_candidates"]),
        "intervals_with_one_adjacent_geometry_candidate": str(counters["intervals_with_one_adjacent_geometry_candidate"]),
        "intervals_with_zero_adjacent_geometry_candidates": str(counters["intervals_with_zero_adjacent_geometry_candidates"]),
        "state_rows_joined": str(counters["state_rows_joined"]),
        "state_rows_with_shape_token_match": str(counters["state_rows_with_shape_token_match"]),
        "state_rows_with_candidate_asset_source": str(counters["state_rows_with_candidate_asset_source"]),
        "candidate_asset_valid_rows": str(counters["candidate_asset_valid_rows"]),
        "object_asset_join_rows": str(counters["object_asset_join_rows"]),
        "objects_with_complete_ambiguity_control": str(counters["objects_with_complete_ambiguity_control"]),
        "original_input_status_valid_rows": str(original_input_valid_rows),
        "order_input_status_valid_rows": str(order_input_valid_rows),
        "state_asset_input_status_valid_rows": str(state_asset_input_valid_rows),
        "candidate_asset_input_status_valid_rows": str(candidate_asset_input_valid_rows),
        "state_to_geometry_unique_binding_proven_rows": str(counters["state_to_geometry_unique_binding_proven_rows"]),
        "state_to_material_unique_binding_proven_rows": str(counters["state_to_material_unique_binding_proven_rows"]),
        "status": STATUS,
        "semantic_binding": SEMANTIC_BINDING,
        "provenance": PROVENANCE,
        "original_semantics": ORIGINAL_SEMANTICS,
    }
    return report, summary


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("render_chain", type=Path)
    parser.add_argument("order_audit", type=Path)
    parser.add_argument("state_asset_control", type=Path)
    parser.add_argument("candidate_asset_audit", type=Path)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()

    report, summary = analyze(
        _read(args.render_chain),
        _read(args.order_audit),
        _read(args.state_asset_control),
        _read(args.candidate_asset_audit),
    )
    summary["render_source_report"] = str(args.render_chain)
    summary["order_source_report"] = str(args.order_audit)
    summary["state_asset_source_report"] = str(args.state_asset_control)
    summary["candidate_asset_source_report"] = str(args.candidate_asset_audit)
    _write(args.report, REPORT_FIELDS, report)
    _write(args.summary, SUMMARY_FIELDS, [summary])
    print(
        "status={status} candidate_object_rows={objects} state_rows={states} "
        "nonempty_intervals={intervals} two_adjacent={two} one_adjacent={one} "
        "shape_matches={matches}".format(
            status=summary["status"],
            objects=summary["candidate_object_rows"],
            states=summary["original_state_rows"],
            intervals=summary["nonempty_state_interval_rows"],
            two=summary["intervals_with_two_adjacent_geometry_candidates"],
            one=summary["intervals_with_one_adjacent_geometry_candidate"],
            matches=summary["state_rows_with_shape_token_match"],
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
