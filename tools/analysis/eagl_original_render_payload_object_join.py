"""Join original render-chain and __VB payload reports by raw object identity.

The join only establishes that two independently reported structural summaries
refer to the same ``.big``/``.ord`` object key. It does not associate a
primitive with a stream or assign position, UV, material, skinning, rigging,
animation, morph, or GPU semantics.
"""

from __future__ import annotations

import argparse
import csv
from pathlib import Path
from typing import Dict, List, Sequence, Tuple


STATUS = "verified-independent-original-render-payload-object-join"
ORIGINAL_SEMANTICS = "not-claimed"
RENDER_STATUS = "verified-bounded-original-render-chain-structure"
PAYLOAD_STATUS = "verified-bounded-original-numeric-payload-structure"
PROVENANCE = (
    "independent-TSV-object-key-join+render-chain-and-numeric-payload-counter-coherence;"
    "object-level-only;no-semantic-promotion"
)


REPORT_FIELDS = (
    "model_archive",
    "model_object",
    "render_primitive_rows",
    "render_command_count_total",
    "render_command_bytes_total",
    "render_complete_command_rows",
    "render_state_rows",
    "render_fetch_rows",
    "payload_validated_stream_count",
    "payload_finite_packed_float_profile_count",
    "payload_finite_float2_profile_count",
    "payload_declared_bounds_containment_count",
    "payload_distinct_range_count",
    "payload_overlap_pair_count",
    "identity_join_match",
    "render_status_valid",
    "payload_status_valid",
    "structural_counts_valid",
    "status",
    "original_semantics",
    "issue",
)

SUMMARY_FIELDS = (
    "render_source_report",
    "payload_source_report",
    "render_object_rows",
    "payload_object_rows",
    "joined_object_rows",
    "render_only_objects",
    "payload_only_objects",
    "render_primitive_rows",
    "payload_validated_stream_rows",
    "joined_identity_match_rows",
    "render_status_valid_rows",
    "payload_status_valid_rows",
    "structural_counts_valid_rows",
    "reversed_key_match_rows",
    "shifted_key_match_rows",
    "status",
    "provenance",
    "original_semantics",
)


def _int(row: Dict[str, str], field: str) -> int:
    try:
        return int(row[field], 0)
    except (KeyError, TypeError, ValueError) as exc:
        raise ValueError(f"invalid integer in {field}: {row.get(field)!r}") from exc


def _key(row: Dict[str, str]) -> Tuple[str, str]:
    key = (row.get("model_archive", ""), row.get("model_object", ""))
    if not all(key):
        raise ValueError("source row has an empty original object identity")
    return key


def _group(rows: Sequence[Dict[str, str]]) -> Dict[Tuple[str, str], List[Dict[str, str]]]:
    grouped: Dict[Tuple[str, str], List[Dict[str, str]]] = {}
    for row in rows:
        grouped.setdefault(_key(row), []).append(row)
    return grouped


def _read_status(rows: Sequence[Dict[str, str]], expected: str, label: str) -> bool:
    if not rows:
        raise ValueError(f"{label} has no rows")
    return all(row.get("status") == expected and row.get("original_semantics") == ORIGINAL_SEMANTICS for row in rows)


def analyze_rows(
    render_rows: Sequence[Dict[str, str]],
    payload_rows: Sequence[Dict[str, str]],
) -> Tuple[List[Dict[str, str]], Dict[str, str]]:
    render_groups = _group(render_rows)
    payload_groups = _group(payload_rows)
    render_status_valid = _read_status(render_rows, RENDER_STATUS, "render report")
    payload_status_valid = _read_status(payload_rows, PAYLOAD_STATUS, "payload report")
    render_keys = set(render_groups)
    payload_keys = set(payload_groups)
    joined_keys = sorted(render_keys & payload_keys)
    render_only = render_keys - payload_keys
    payload_only = payload_keys - render_keys
    report: List[Dict[str, str]] = []
    for key in joined_keys:
        render_group = render_groups[key]
        payload_group = payload_groups[key]
        if len(payload_group) != 1:
            raise ValueError(f"payload report is not one row per object: {key!r}")
        payload = payload_group[0]
        render_complete = sum(row.get("command_stream_complete") == "True" for row in render_group)
        render_state = sum(row.get("state_relocation_present") == "True" for row in render_group)
        render_fetch = sum(row.get("fetch_slot_count", "0") != "0" for row in render_group)
        render_command_count = sum(_int(row, "command_count") for row in render_group)
        render_command_bytes = sum(_int(row, "command_bytes") for row in render_group)
        payload_streams = _int(payload, "validated_stream_count")
        payload_finite_packed = _int(payload, "finite_packed_float_profile_count")
        payload_finite_float2 = _int(payload, "finite_float2_profile_count")
        payload_bounds = _int(payload, "declared_bounds_containment_float3_count")
        payload_distinct_ranges = _int(payload, "distinct_payload_range_count")
        payload_overlap = _int(payload, "overlap_payload_pair_count")
        counts_valid = (
            len(render_group) > 0
            and render_complete == len(render_group)
            and render_state == len(render_group)
            and render_fetch > 0
            and payload_streams > 0
            and payload_finite_packed >= 0
            and payload_finite_float2 >= 0
            and payload_bounds >= 0
            and payload_distinct_ranges > 0
            and payload_overlap >= 0
        )
        row_status = STATUS if render_status_valid and payload_status_valid and counts_valid else "inconclusive-original-render-payload-object-join"
        report.append(
            {
                "model_archive": key[0],
                "model_object": key[1],
                "render_primitive_rows": str(len(render_group)),
                "render_command_count_total": str(render_command_count),
                "render_command_bytes_total": str(render_command_bytes),
                "render_complete_command_rows": str(render_complete),
                "render_state_rows": str(render_state),
                "render_fetch_rows": str(render_fetch),
                "payload_validated_stream_count": str(payload_streams),
                "payload_finite_packed_float_profile_count": str(payload_finite_packed),
                "payload_finite_float2_profile_count": str(payload_finite_float2),
                "payload_declared_bounds_containment_count": str(payload_bounds),
                "payload_distinct_range_count": str(payload_distinct_ranges),
                "payload_overlap_pair_count": str(payload_overlap),
                "identity_join_match": "True",
                "render_status_valid": str(render_status_valid),
                "payload_status_valid": str(payload_status_valid),
                "structural_counts_valid": str(counts_valid),
                "status": row_status,
                "original_semantics": ORIGINAL_SEMANTICS,
                "issue": "" if row_status == STATUS else "source status or structural counters require review",
            }
        )

    sorted_payload_keys = sorted(payload_keys)
    reversed_payload_keys = sorted((object_name, archive) for archive, object_name in payload_keys)
    reversed_key_match = sum(
        left == right for left, right in zip(sorted(render_keys), reversed_payload_keys)
    )
    shifted_key_match = sum(
        left == right for left, right in zip(sorted(render_keys), sorted_payload_keys[1:])
    )
    valid_rows = sum(row["status"] == STATUS for row in report)
    summary = {
        "render_source_report": "",
        "payload_source_report": "",
        "render_object_rows": str(len(render_keys)),
        "payload_object_rows": str(len(payload_keys)),
        "joined_object_rows": str(len(joined_keys)),
        "render_only_objects": str(len(render_only)),
        "payload_only_objects": str(len(payload_only)),
        "render_primitive_rows": str(sum(len(rows) for rows in render_groups.values())),
        "payload_validated_stream_rows": str(sum(_int(row, "validated_stream_count") for row in payload_rows)),
        "joined_identity_match_rows": str(sum(row["identity_join_match"] == "True" for row in report)),
        "render_status_valid_rows": str(sum(row["render_status_valid"] == "True" for row in report)),
        "payload_status_valid_rows": str(sum(row["payload_status_valid"] == "True" for row in report)),
        "structural_counts_valid_rows": str(sum(row["structural_counts_valid"] == "True" for row in report)),
        "reversed_key_match_rows": str(reversed_key_match),
        "shifted_key_match_rows": str(shifted_key_match),
        "status": STATUS if joined_keys and valid_rows == len(joined_keys) and not render_only and not payload_only else "inconclusive-original-render-payload-object-join",
        "provenance": PROVENANCE,
        "original_semantics": ORIGINAL_SEMANTICS,
    }
    if not joined_keys:
        raise ValueError("render and payload reports have no common object keys")
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
    parser.add_argument("payload_source_report", type=Path)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()
    report, summary = analyze_rows(_read(args.render_source_report), _read(args.payload_source_report))
    summary["render_source_report"] = str(args.render_source_report)
    summary["payload_source_report"] = str(args.payload_source_report)
    _write(args.report, REPORT_FIELDS, report)
    _write(args.summary, SUMMARY_FIELDS, [summary])
    print("FIFA Street 2 independent original render/payload object join")
    for key in SUMMARY_FIELDS:
        print(f"{key}: {summary[key]}")
    print(f"report: {args.report}")
    print(f"summary: {args.summary}")
    return 0 if summary["status"] == STATUS else 1


if __name__ == "__main__":
    raise SystemExit(main())
