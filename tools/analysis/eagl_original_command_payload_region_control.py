"""Independently check raw command/payload interval separation per primitive.

This control joins render-chain command intervals to original topology payload
ranges by primitive identity. It reports half-open byte-interval relationships
only; no interval is assigned a field, vertex attribute, geometry, UV,
material, skinning, rigging, animation, morph, draw, command, or GPU meaning.
"""

from __future__ import annotations

import argparse
import csv
from collections import Counter
from pathlib import Path
from typing import Dict, List, Sequence, Tuple


STATUS = "verified-independent-original-command-payload-region-control"
ORIGINAL_SEMANTICS = "not-claimed"
RENDER_STATUS = "verified-bounded-original-render-chain-structure"
TOPOLOGY_STATUSES = {
    "verified-bounded-original-primitive-stream-topology",
    "inconclusive-original-primitive-stream-topology",
}
PROVENANCE = (
    "independent-TSV-primitive-key-join+half-open-command/payload-interval-checks;"
    "gap-histogram+non-overlap-control;no-semantic-promotion"
)
KEY_FIELDS = ("model_archive", "model_object", "layer", "primitive")
REPORT_FIELDS = (
    *KEY_FIELDS,
    "command_section_index",
    "command_stream_offset",
    "command_bytes",
    "topology_row_count",
    "topology_section_indices",
    "payload_interval_count",
    "payload_ranges_valid",
    "payload_intervals_nonoverlapping",
    "command_section_matches_topology",
    "command_interval_valid",
    "command_payload_nonoverlap",
    "command_after_payload_end",
    "command_payload_gap",
    "command_payload_gap_equals_0x10",
    "render_fetch_slot_ids",
    "render_status_valid",
    "topology_status_valid",
    "status",
    "original_semantics",
    "issue",
)
SUMMARY_FIELDS = (
    "render_source_report",
    "topology_source_report",
    "render_rows",
    "topology_rows",
    "joined_rows",
    "render_input_status_valid_rows",
    "topology_input_status_valid_rows",
    "topology_groups_with_five_rows",
    "payload_range_valid_rows",
    "payload_interval_nonoverlap_rows",
    "command_section_match_rows",
    "command_interval_valid_rows",
    "command_payload_nonoverlap_rows",
    "command_after_payload_end_rows",
    "command_payload_gap_equals_0x10_rows",
    "command_payload_gap_histogram",
    "incomplete_slot_two_rows",
    "missing_render_keys",
    "missing_topology_keys",
    "status",
    "provenance",
    "original_semantics",
)


def _key(row: Dict[str, str]) -> Tuple[str, str, str, str]:
    values = tuple(row.get(field, "") for field in KEY_FIELDS)
    if not all(values):
        raise ValueError(f"source row has an empty primitive identity: {row!r}")
    return values


def _int(row: Dict[str, str], field: str) -> int:
    try:
        return int(row[field], 0)
    except (KeyError, TypeError, ValueError) as exc:
        raise ValueError(f"invalid integer in {field}: {row.get(field)!r}") from exc


def _bool(row: Dict[str, str], field: str) -> bool:
    value = row.get(field)
    if value not in {"True", "False"}:
        raise ValueError(f"invalid boolean in {field}: {value!r}")
    return value == "True"


def _fetch_ids(value: str) -> List[int]:
    if not value:
        return []
    try:
        return [int(part, 0) for part in value.split(",") if part]
    except ValueError as exc:
        raise ValueError(f"invalid fetch-slot list: {value!r}") from exc


def _group(rows: Sequence[Dict[str, str]]) -> Dict[Tuple[str, str, str, str], List[Dict[str, str]]]:
    grouped: Dict[Tuple[str, str, str, str], List[Dict[str, str]]] = {}
    for row in rows:
        grouped.setdefault(_key(row), []).append(row)
    return grouped


def _status_valid(rows: Sequence[Dict[str, str]], expected: str | set[str], label: str) -> bool:
    if not rows:
        raise ValueError(f"{label} has no rows")
    allowed = {expected} if isinstance(expected, str) else expected
    return all(
        row.get("status") in allowed and row.get("original_semantics") == ORIGINAL_SEMANTICS
        for row in rows
    )


def _nonoverlapping(intervals: Sequence[Tuple[int, int]]) -> bool:
    ordered = sorted(intervals)
    return all(left[1] <= right[0] for left, right in zip(ordered, ordered[1:]))


def analyze_rows(
    render_rows: Sequence[Dict[str, str]],
    topology_rows: Sequence[Dict[str, str]],
) -> Tuple[List[Dict[str, str]], Dict[str, str]]:
    render_groups = _group(render_rows)
    topology_groups = _group(topology_rows)
    render_status_valid = _status_valid(render_rows, RENDER_STATUS, "render report")
    topology_status_valid = _status_valid(topology_rows, TOPOLOGY_STATUSES, "topology report")
    render_keys = set(render_groups)
    topology_keys = set(topology_groups)
    joined_keys = sorted(render_keys & topology_keys)
    missing_render_keys = sorted(topology_keys - render_keys)
    missing_topology_keys = sorted(render_keys - topology_keys)
    report: List[Dict[str, str]] = []
    gap_histogram: Counter[int] = Counter()
    incomplete_slot_two_rows = 0

    for key in joined_keys:
        render_group = render_groups[key]
        topology_group = topology_groups[key]
        if len(render_group) != 1:
            raise ValueError(f"render report is not one row per primitive: {key!r}")
        if len(topology_group) != 5:
            raise ValueError(f"topology report does not have five rows per primitive: {key!r}")

        render = render_group[0]
        command_section = _int(render, "command_section_index")
        command_start = _int(render, "command_stream_offset")
        command_bytes = _int(render, "command_bytes")
        intervals = [
            (_int(row, "data_offset"), _int(row, "data_offset") + _int(row, "byte_length"))
            for row in topology_group
        ]
        section_indices = sorted({_int(row, "section_index") for row in topology_group})
        payload_ranges_valid = all(_bool(row, "valid_payload_range") for row in topology_group)
        payload_intervals_nonoverlapping = _nonoverlapping(intervals)
        command_section_matches = all(index == command_section for index in section_indices)
        command_interval_valid = command_start >= 0 and command_bytes > 0
        command_end = command_start + command_bytes
        command_payload_nonoverlap = command_interval_valid and all(
            command_end <= start or end <= command_start
            for start, end in intervals
        )
        max_payload_end = max(end for _, end in intervals)
        command_after_payload_end = command_interval_valid and command_start >= max_payload_end
        gap = command_start - max_payload_end
        gap_histogram[gap] += 1
        gap_equals_0x10 = gap == 0x10
        complete = _fetch_ids(render.get("fetch_slot_ids", "")) == [0, 1, 2, 3, 4]
        incomplete_slot_two = not complete and _fetch_ids(render.get("fetch_slot_ids", "")) == [0, 1, 3, 4]
        if incomplete_slot_two:
            incomplete_slot_two_rows += 1
        row_valid = (
            render_status_valid
            and topology_status_valid
            and len(topology_group) == 5
            and payload_ranges_valid
            and payload_intervals_nonoverlapping
            and command_section_matches
            and command_interval_valid
            and command_payload_nonoverlap
            and command_after_payload_end
        )
        row_status = STATUS if row_valid else "inconclusive-original-command-payload-region-control"
        report.append(
            {
                **dict(zip(KEY_FIELDS, key)),
                "command_section_index": str(command_section),
                "command_stream_offset": f"0x{command_start:X}",
                "command_bytes": str(command_bytes),
                "topology_row_count": str(len(topology_group)),
                "topology_section_indices": ",".join(str(index) for index in section_indices),
                "payload_interval_count": str(len(intervals)),
                "payload_ranges_valid": str(payload_ranges_valid),
                "payload_intervals_nonoverlapping": str(payload_intervals_nonoverlapping),
                "command_section_matches_topology": str(command_section_matches),
                "command_interval_valid": str(command_interval_valid),
                "command_payload_nonoverlap": str(command_payload_nonoverlap),
                "command_after_payload_end": str(command_after_payload_end),
                "command_payload_gap": f"0x{gap:X}",
                "command_payload_gap_equals_0x10": str(gap_equals_0x10),
                "render_fetch_slot_ids": render.get("fetch_slot_ids", ""),
                "render_status_valid": str(render_status_valid),
                "topology_status_valid": str(topology_status_valid),
                "status": row_status,
                "original_semantics": ORIGINAL_SEMANTICS,
                "issue": "" if row_valid else "source status or command/payload interval relationship requires review",
            }
        )

    summary = {
        "render_source_report": "",
        "topology_source_report": "",
        "render_rows": str(len(render_rows)),
        "topology_rows": str(len(topology_rows)),
        "joined_rows": str(len(joined_keys)),
        "render_input_status_valid_rows": str(len(render_rows) if render_status_valid else 0),
        "topology_input_status_valid_rows": str(len(topology_rows) if topology_status_valid else 0),
        "topology_groups_with_five_rows": str(sum(len(topology_groups[key]) == 5 for key in joined_keys)),
        "payload_range_valid_rows": str(sum(row["payload_ranges_valid"] == "True" for row in report)),
        "payload_interval_nonoverlap_rows": str(sum(row["payload_intervals_nonoverlapping"] == "True" for row in report)),
        "command_section_match_rows": str(sum(row["command_section_matches_topology"] == "True" for row in report)),
        "command_interval_valid_rows": str(sum(row["command_interval_valid"] == "True" for row in report)),
        "command_payload_nonoverlap_rows": str(sum(row["command_payload_nonoverlap"] == "True" for row in report)),
        "command_after_payload_end_rows": str(sum(row["command_after_payload_end"] == "True" for row in report)),
        "command_payload_gap_equals_0x10_rows": str(sum(row["command_payload_gap_equals_0x10"] == "True" for row in report)),
        "command_payload_gap_histogram": ",".join(
            f"{gap}:{count}" for gap, count in sorted(gap_histogram.items())
        ),
        "incomplete_slot_two_rows": str(incomplete_slot_two_rows),
        "missing_render_keys": str(len(missing_render_keys)),
        "missing_topology_keys": str(len(missing_topology_keys)),
        "status": (
            STATUS
            if joined_keys
            and len(joined_keys) == len(render_keys) == len(topology_keys)
            and all(row["status"] == STATUS for row in report)
            else "inconclusive-original-command-payload-region-control"
        ),
        "provenance": PROVENANCE,
        "original_semantics": ORIGINAL_SEMANTICS,
    }
    return report, summary


def _read(path: Path) -> List[Dict[str, str]]:
    with path.open("r", encoding="utf-8-sig", newline="") as handle:
        return list(csv.DictReader(handle, delimiter="\t"))


def _write(path: Path, fields: Sequence[str], rows: Sequence[Dict[str, str]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=list(fields), delimiter="\t", lineterminator="\n")
        writer.writeheader()
        writer.writerows({field: row.get(field, "") for field in fields} for row in rows)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("render_source_report", type=Path)
    parser.add_argument("topology_source_report", type=Path)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()
    report, summary = analyze_rows(_read(args.render_source_report), _read(args.topology_source_report))
    summary["render_source_report"] = str(args.render_source_report)
    summary["topology_source_report"] = str(args.topology_source_report)
    _write(args.report, REPORT_FIELDS, report)
    _write(args.summary, SUMMARY_FIELDS, [summary])
    print("FIFA Street 2 independent original command/payload region control")
    for field in SUMMARY_FIELDS:
        print(f"{field}: {summary[field]}")
    print(f"report: {args.report}")
    print(f"summary: {args.summary}")
    return 0 if summary["status"] == STATUS else 1


if __name__ == "__main__":
    raise SystemExit(main())
