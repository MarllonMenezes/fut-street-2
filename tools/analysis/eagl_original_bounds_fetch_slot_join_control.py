"""Join numeric stream rows to opaque primitive fetch-slot rows.

The join is keyed by raw archive/object/ordinal/category values.  It checks
that the bounds-consistent first category record is also fetch slot 0, while
preserving the two known incomplete slot cases.  No position, UV, material,
skinning, rigging, animation, morph, or GPU meaning is assigned.
"""

from __future__ import annotations

import argparse
import csv
from collections import defaultdict
from pathlib import Path
from typing import Dict, Iterable, List, Sequence, Tuple


STATUS = "verified-independent-original-bounds-fetch-slot-join-control"
NUMERIC_STATUS = "verified-bounded-original-numeric-stream-profile"
TOPOLOGY_STATUS = "verified-bounded-original-primitive-stream-topology"
TOPOLOGY_INCONCLUSIVE_STATUS = "inconclusive-original-primitive-stream-topology"
ORIGINAL_SEMANTICS = "not-claimed"
PROVENANCE = (
    "independent-TSV-keyed-join+opaque-category-ordinal-to-fetch-slot;"
    "bounds-consistency-only;no-semantic-promotion"
)

REPORT_FIELDS = [
    "model_archive",
    "model_object",
    "ordinal_key",
    "category_key",
    "numeric_row_count",
    "topology_row_count",
    "identity_join",
    "raw_category_order",
    "fetch_slot",
    "fetch_stride",
    "stream_stride",
    "fetch_stride_matches_stream_stride",
    "valid_payload_range",
    "declared_bounds_containment_float3",
    "first_raw_record",
    "first_raw_fetch_slot_zero",
    "status",
    "semantic_binding",
    "original_semantics",
    "issue",
]

SUMMARY_FIELDS = [
    "numeric_rows",
    "topology_rows",
    "joined_rows",
    "missing_numeric_keys",
    "missing_topology_keys",
    "object_ordinal_groups",
    "complete_five_stream_groups",
    "identity_join_rows",
    "numeric_input_status_valid_rows",
    "topology_input_status_valid_rows",
    "first_raw_fetch_slot_zero_groups",
    "first_raw_stride_match_groups",
    "first_raw_bounds_match_groups",
    "first_raw_no_bounds_match_groups",
    "later_raw_bounds_match_groups",
    "incomplete_fetch_slot_groups",
    "status",
    "semantic_binding",
    "provenance",
    "original_semantics",
]


def _read(path: Path) -> List[Dict[str, str]]:
    with path.open("r", encoding="utf-8-sig", newline="") as handle:
        return list(csv.DictReader(handle, delimiter="\t"))


def _integer(row: Dict[str, str], field: str) -> int:
    try:
        return int(row[field], 0)
    except (KeyError, TypeError, ValueError) as exc:
        raise ValueError(f"invalid integer field {field!r}") from exc


def _field(value: object) -> str:
    return str(value).replace("\t", " ").replace("\r", " ").replace("\n", " ")


def analyze(
    numeric_rows: Sequence[Dict[str, str]],
    topology_rows: Sequence[Dict[str, str]],
) -> Tuple[List[Dict[str, str]], Dict[str, str]]:
    if not numeric_rows or not topology_rows:
        raise ValueError("numeric and topology reports must contain rows")

    numeric: dict[tuple[str, str, int, int], list[Dict[str, str]]] = defaultdict(list)
    topology: dict[tuple[str, str, int, int], list[Dict[str, str]]] = defaultdict(list)
    for row in numeric_rows:
        numeric[(row.get("model_archive", ""), row.get("model_object", ""), _integer(row, "ordinal_key"), _integer(row, "category_key"))].append(row)
    for row in topology_rows:
        topology[(row.get("model_archive", ""), row.get("model_object", ""), _integer(row, "primitive"), _integer(row, "category"))].append(row)

    numeric_keys = set(numeric)
    topology_keys = set(topology)
    joined_keys = sorted(numeric_keys & topology_keys)
    report: list[Dict[str, str]] = []
    identity_rows = 0
    numeric_status_valid_rows = 0
    topology_status_valid_rows = 0
    groups: dict = defaultdict(list)

    for key in sorted(numeric_keys | topology_keys):
        numeric_group = numeric.get(key, [])
        topology_group = topology.get(key, [])
        identity = len(numeric_group) == 1 and len(topology_group) == 1
        if identity:
            identity_rows += 1
        numeric_valid = identity and numeric_group[0].get("status") == NUMERIC_STATUS and numeric_group[0].get("original_semantics") == ORIGINAL_SEMANTICS
        topology_valid = identity and topology_group[0].get("status") in {TOPOLOGY_STATUS, TOPOLOGY_INCONCLUSIVE_STATUS} and topology_group[0].get("original_semantics") == ORIGINAL_SEMANTICS
        numeric_status_valid_rows += int(numeric_valid)
        topology_status_valid_rows += int(topology_valid)
        if not identity:
            numeric_row = numeric_group[0] if numeric_group else {}
            topology_row = topology_group[0] if topology_group else {}
        else:
            numeric_row = numeric_group[0]
            topology_row = topology_group[0]
        archive, object_name, ordinal, category = key
        category_order_key = (archive, object_name, ordinal)
        groups[category_order_key].append((key, numeric_row, topology_row))
        report.append(
            {
                "model_archive": archive,
                "model_object": object_name,
                "ordinal_key": str(ordinal),
                "category_key": str(category),
                "numeric_row_count": str(len(numeric_group)),
                "topology_row_count": str(len(topology_group)),
                "identity_join": str(identity),
                "raw_category_order": "",
                "fetch_slot": topology_row.get("fetch_slot", "-1"),
                "fetch_stride": topology_row.get("fetch_stride", "0"),
                "stream_stride": numeric_row.get("stream_stride", "0"),
                "fetch_stride_matches_stream_stride": topology_row.get("fetch_stride_matches_stream_stride", "False"),
                "valid_payload_range": topology_row.get("valid_payload_range", "False"),
                "declared_bounds_containment_float3": numeric_row.get("declared_bounds_containment_float3", "False"),
                "first_raw_record": "",
                "first_raw_fetch_slot_zero": "",
                "status": STATUS if identity and numeric_valid and topology_valid else "inconclusive-original-bounds-fetch-slot-join-control",
                "semantic_binding": "not-proven",
                "original_semantics": ORIGINAL_SEMANTICS,
                "issue": "" if identity and numeric_valid and topology_valid else "source key, status or cardinality requires review",
            }
        )

    group_count = len(groups)
    complete_groups = 0
    first_slot_zero_groups = 0
    first_stride_match_groups = 0
    first_bounds_match_groups = 0
    first_no_bounds_groups = 0
    later_bounds_groups = 0
    incomplete_fetch_slot_groups = 0
    report_by_key = {(row["model_archive"], row["model_object"], int(row["ordinal_key"]), int(row["category_key"])): row for row in report}
    for group_key, entries in groups.items():
        entries.sort(key=lambda item: item[0][3])
        complete = len(entries) == 5
        complete_groups += int(complete)
        if not complete:
            continue
        first_key, first_numeric, first_topology = entries[0]
        first_slot_zero = first_topology.get("fetch_slot") == "0"
        first_stride_match = first_topology.get("fetch_stride_matches_stream_stride") == "True"
        first_bounds_match = first_numeric.get("declared_bounds_containment_float3") == "True"
        later_bounds = any(entry[1].get("declared_bounds_containment_float3") == "True" for entry in entries[1:])
        first_slot_zero_groups += int(first_slot_zero)
        first_stride_match_groups += int(first_stride_match)
        first_bounds_match_groups += int(first_bounds_match)
        first_no_bounds_groups += int(not first_bounds_match)
        later_bounds_groups += int(later_bounds)
        incomplete_fetch_slot_groups += int(any(entry[2].get("fetch_slot") == "-1" for entry in entries))
        for index, (key, _, _) in enumerate(entries):
            output = report_by_key[key]
            output["raw_category_order"] = ",".join(str(item[0][3]) for item in entries)
            output["first_raw_record"] = str(index == 0)
            output["first_raw_fetch_slot_zero"] = str(first_slot_zero if index == 0 else False)

    status = (
        group_count > 0 and
        len(numeric_keys & topology_keys) == len(numeric_keys) == len(topology_keys) and
        identity_rows == len(numeric_rows) == len(topology_rows) and
        numeric_status_valid_rows == len(numeric_rows) and
        topology_status_valid_rows == len(topology_rows) and
        complete_groups == group_count and
        first_slot_zero_groups == group_count and
        first_stride_match_groups == group_count and
        first_bounds_match_groups > 0 and
        later_bounds_groups == 0 and
        incomplete_fetch_slot_groups >= 0
    )
    summary = {
        "numeric_rows": str(len(numeric_rows)),
        "topology_rows": str(len(topology_rows)),
        "joined_rows": str(len(joined_keys)),
        "missing_numeric_keys": str(len(topology_keys - numeric_keys)),
        "missing_topology_keys": str(len(numeric_keys - topology_keys)),
        "object_ordinal_groups": str(group_count),
        "complete_five_stream_groups": str(complete_groups),
        "identity_join_rows": str(identity_rows),
        "numeric_input_status_valid_rows": str(numeric_status_valid_rows),
        "topology_input_status_valid_rows": str(topology_status_valid_rows),
        "first_raw_fetch_slot_zero_groups": str(first_slot_zero_groups),
        "first_raw_stride_match_groups": str(first_stride_match_groups),
        "first_raw_bounds_match_groups": str(first_bounds_match_groups),
        "first_raw_no_bounds_match_groups": str(first_no_bounds_groups),
        "later_raw_bounds_match_groups": str(later_bounds_groups),
        "incomplete_fetch_slot_groups": str(incomplete_fetch_slot_groups),
        "status": STATUS if status else "inconclusive-original-bounds-fetch-slot-join-control",
        "semantic_binding": "not-proven",
        "provenance": PROVENANCE,
        "original_semantics": ORIGINAL_SEMANTICS,
    }
    return report, summary


def _write(path: Path, fields: Sequence[str], rows: Iterable[Dict[str, str]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=list(fields), delimiter="\t", lineterminator="\n")
        writer.writeheader()
        writer.writerows({field: _field(row.get(field, "")) for field in fields} for row in rows)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("numeric_report", type=Path)
    parser.add_argument("topology_report", type=Path)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()
    report, summary = analyze(_read(args.numeric_report), _read(args.topology_report))
    _write(args.report, REPORT_FIELDS, report)
    _write(args.summary, SUMMARY_FIELDS, [summary])
    print("FIFA Street 2 independent original bounds/fetch-slot join control")
    for key in (
        "status", "numeric_rows", "topology_rows", "joined_rows", "object_ordinal_groups",
        "first_raw_fetch_slot_zero_groups", "first_raw_stride_match_groups",
        "first_raw_bounds_match_groups", "first_raw_no_bounds_match_groups",
        "later_raw_bounds_match_groups", "incomplete_fetch_slot_groups", "semantic_binding",
    ):
        print(f"{key}: {summary[key]}")
    return 0 if summary["status"] == STATUS else 1


if __name__ == "__main__":
    raise SystemExit(main())
