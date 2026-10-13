"""Independently join state/geometry objects to the original payload objects.

This control checks only that the 24 objects covered by the state/geometry
reports exist in the independently validated render/payload object report and
that primitive/five-slot counters reconcile. It does not assign payload
records to positions, UVs, materials, vertex attributes, skinning, rigging,
animation, morphs, commands or GPU behavior.
"""

from __future__ import annotations

import argparse
import csv
from pathlib import Path
from typing import Dict, List, Sequence, Tuple


STATUS = "verified-independent-original-state-geometry-payload-join-control"
ADJACENCY_STATUS = "verified-control"
JOIN_STATUS = "verified-independent-original-render-payload-object-join"
ORIGINAL_SEMANTICS = "not-claimed"
PROVENANCE = (
    "independent-TSV-object-key-join+primitive-count-reconciliation+"
    "five-slot-payload-counter-control;no-semantic-promotion"
)

REPORT_FIELDS = (
    "model_archive",
    "object_name",
    "shape_name",
    "adjacency_input_status",
    "join_input_status",
    "identity_join_match",
    "render_status_valid",
    "payload_status_valid",
    "structural_counts_valid",
    "geometry_primitive_count",
    "state_primitive_count",
    "state_geometry_primitive_total",
    "render_primitive_rows",
    "render_complete_command_rows",
    "render_fetch_rows",
    "payload_validated_stream_count",
    "payload_distinct_range_count",
    "payload_overlap_pair_count",
    "primitive_count_match",
    "complete_command_count_match",
    "fetch_count_match",
    "five_slot_payload_count_match",
    "distinct_payload_range_count_match",
    "status",
    "original_semantics",
    "issue",
)

SUMMARY_FIELDS = (
    "adjacency_source_report",
    "join_source_report",
    "adjacency_rows",
    "join_rows",
    "joined_object_rows",
    "missing_join_objects",
    "adjacency_input_status_valid_rows",
    "join_input_status_valid_rows",
    "identity_join_match_rows",
    "render_status_valid_rows",
    "payload_status_valid_rows",
    "structural_counts_valid_rows",
    "primitive_count_match_rows",
    "complete_command_count_match_rows",
    "fetch_count_match_rows",
    "five_slot_payload_count_match_rows",
    "distinct_payload_range_count_match_rows",
    "payload_overlap_zero_rows",
    "status",
    "provenance",
    "original_semantics",
)


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


def _key(row: Dict[str, str]) -> Tuple[str, str]:
    key = (row.get("model_archive", ""), row.get("object_name", row.get("model_object", "")))
    if not all(key):
        raise ValueError(f"empty object identity: {key!r}")
    return key


def analyze(
    adjacency_rows: Sequence[Dict[str, str]],
    join_rows: Sequence[Dict[str, str]],
) -> Tuple[List[Dict[str, str]], Dict[str, str]]:
    if not adjacency_rows or not join_rows:
        raise ValueError("adjacency and render/payload join reports must contain rows")

    adjacency_by_key = {_key(row): row for row in adjacency_rows}
    join_by_key = {_key(row): row for row in join_rows}
    keys = sorted(set(adjacency_by_key) & set(join_by_key))
    missing_join_objects = set(adjacency_by_key) - set(join_by_key)

    report: List[Dict[str, str]] = []
    identity_join_match_rows = 0
    render_status_valid_rows = 0
    payload_status_valid_rows = 0
    structural_counts_valid_rows = 0
    primitive_count_match_rows = 0
    complete_command_count_match_rows = 0
    fetch_count_match_rows = 0
    five_slot_payload_count_match_rows = 0
    distinct_payload_range_count_match_rows = 0
    payload_overlap_zero_rows = 0

    for key in keys:
        adjacency = adjacency_by_key[key]
        joined = join_by_key[key]
        geometry_count = _int(adjacency, "geometry_primitive_count")
        state_count = _int(adjacency, "state_primitive_count")
        primitive_total = geometry_count + state_count
        render_rows = _int(joined, "render_primitive_rows")
        render_complete = _int(joined, "render_complete_command_rows")
        render_fetch = _int(joined, "render_fetch_rows")
        payload_streams = _int(joined, "payload_validated_stream_count")
        payload_ranges = _int(joined, "payload_distinct_range_count")
        payload_overlap = _int(joined, "payload_overlap_pair_count")
        identity_match = _bool(joined, "identity_join_match")
        render_status_valid = _bool(joined, "render_status_valid")
        payload_status_valid = _bool(joined, "payload_status_valid")
        structural_valid = _bool(joined, "structural_counts_valid")
        primitive_match = render_rows == primitive_total
        complete_match = render_complete == render_rows
        fetch_match = render_fetch == render_rows
        five_slot_match = payload_streams == render_rows * 5
        distinct_range_match = payload_ranges == payload_streams
        overlap_zero = payload_overlap == 0
        row_status = STATUS if (
            adjacency.get("status") == ADJACENCY_STATUS
            and joined.get("status") == JOIN_STATUS
            and identity_match
            and render_status_valid
            and payload_status_valid
            and structural_valid
            and primitive_match
            and complete_match
            and fetch_match
            and five_slot_match
            and distinct_range_match
            and overlap_zero
        ) else "inconclusive-independent-original-state-geometry-payload-join-control"

        identity_join_match_rows += int(identity_match)
        render_status_valid_rows += int(render_status_valid)
        payload_status_valid_rows += int(payload_status_valid)
        structural_counts_valid_rows += int(structural_valid)
        primitive_count_match_rows += int(primitive_match)
        complete_command_count_match_rows += int(complete_match)
        fetch_count_match_rows += int(fetch_match)
        five_slot_payload_count_match_rows += int(five_slot_match)
        distinct_payload_range_count_match_rows += int(distinct_range_match)
        payload_overlap_zero_rows += int(overlap_zero)
        report.append(
            {
                "model_archive": key[0],
                "object_name": key[1],
                "shape_name": adjacency.get("shape_name", ""),
                "adjacency_input_status": adjacency.get("status", ""),
                "join_input_status": joined.get("status", ""),
                "identity_join_match": str(identity_match),
                "render_status_valid": str(render_status_valid),
                "payload_status_valid": str(payload_status_valid),
                "structural_counts_valid": str(structural_valid),
                "geometry_primitive_count": str(geometry_count),
                "state_primitive_count": str(state_count),
                "state_geometry_primitive_total": str(primitive_total),
                "render_primitive_rows": str(render_rows),
                "render_complete_command_rows": str(render_complete),
                "render_fetch_rows": str(render_fetch),
                "payload_validated_stream_count": str(payload_streams),
                "payload_distinct_range_count": str(payload_ranges),
                "payload_overlap_pair_count": str(payload_overlap),
                "primitive_count_match": str(primitive_match),
                "complete_command_count_match": str(complete_match),
                "fetch_count_match": str(fetch_match),
                "five_slot_payload_count_match": str(five_slot_match),
                "distinct_payload_range_count_match": str(distinct_range_match),
                "status": row_status,
                "original_semantics": ORIGINAL_SEMANTICS,
                "issue": "" if row_status == STATUS else "object join or structural counters require review",
            }
        )

    adjacency_input_status_valid_rows = sum(
        row.get("status") == ADJACENCY_STATUS for row in adjacency_rows
    )
    join_input_status_valid_rows = sum(row.get("status") == JOIN_STATUS for row in join_rows)
    summary = {
        "adjacency_source_report": "",
        "join_source_report": "",
        "adjacency_rows": str(len(adjacency_rows)),
        "join_rows": str(len(join_rows)),
        "joined_object_rows": str(len(keys)),
        "missing_join_objects": str(len(missing_join_objects)),
        "adjacency_input_status_valid_rows": str(adjacency_input_status_valid_rows),
        "join_input_status_valid_rows": str(join_input_status_valid_rows),
        "identity_join_match_rows": str(identity_join_match_rows),
        "render_status_valid_rows": str(render_status_valid_rows),
        "payload_status_valid_rows": str(payload_status_valid_rows),
        "structural_counts_valid_rows": str(structural_counts_valid_rows),
        "primitive_count_match_rows": str(primitive_count_match_rows),
        "complete_command_count_match_rows": str(complete_command_count_match_rows),
        "fetch_count_match_rows": str(fetch_count_match_rows),
        "five_slot_payload_count_match_rows": str(five_slot_payload_count_match_rows),
        "distinct_payload_range_count_match_rows": str(distinct_payload_range_count_match_rows),
        "payload_overlap_zero_rows": str(payload_overlap_zero_rows),
        "status": STATUS if (
            len(keys) == len(adjacency_rows) and
            adjacency_input_status_valid_rows == len(adjacency_rows) and
            join_input_status_valid_rows == len(join_rows) and
            not missing_join_objects and
            identity_join_match_rows == len(keys) and
            render_status_valid_rows == len(keys) and
            payload_status_valid_rows == len(keys) and
            structural_counts_valid_rows == len(keys) and
            primitive_count_match_rows == len(keys) and
            complete_command_count_match_rows == len(keys) and
            fetch_count_match_rows == len(keys) and
            five_slot_payload_count_match_rows == len(keys) and
            distinct_payload_range_count_match_rows == len(keys) and
            payload_overlap_zero_rows == len(keys) and
            all(row["status"] == STATUS for row in report)
        ) else "inconclusive-independent-original-state-geometry-payload-join-control",
        "provenance": PROVENANCE,
        "original_semantics": ORIGINAL_SEMANTICS,
    }
    return report, summary


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("adjacency_report", type=Path)
    parser.add_argument("join_report", type=Path)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()
    report, summary = analyze(_read(args.adjacency_report), _read(args.join_report))
    summary["adjacency_source_report"] = str(args.adjacency_report)
    summary["join_source_report"] = str(args.join_report)
    _write(args.report, REPORT_FIELDS, report)
    _write(args.summary, SUMMARY_FIELDS, [summary])
    print("FIFA Street 2 independent original state/geometry payload join control")
    for field in SUMMARY_FIELDS:
        print(f"{field}: {summary[field]}")
    print(f"report: {args.report}")
    print(f"summary: {args.summary}")
    return 0 if summary["status"] == STATUS else 1


if __name__ == "__main__":
    raise SystemExit(main())
