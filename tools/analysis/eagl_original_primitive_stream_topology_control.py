"""Independently reparse the original primitive/__VB topology report.

The control validates only the raw topology fields emitted by the managed
audit. It keeps a positive shifted-ordinal control visible: repeated stride
layouts can match another ordinal, so ordinal recurrence is not promoted to a
unique primitive binding or an attribute meaning.
"""

from __future__ import annotations

import argparse
import csv
from collections import defaultdict
from pathlib import Path
from typing import Dict, Iterable, List, Sequence, Tuple


STATUS = "verified-independent-original-primitive-stream-topology-control"
INPUT_STATUS = "verified-bounded-original-primitive-stream-topology"
ORIGINAL_SEMANTICS = "not-claimed"
PROVENANCE = (
    "independent-TSV-reparse+primitive-slot-and-__VB-range-controls;"
    "shifted-ordinal-ambiguity-preserved;no-semantic-promotion"
)

REPORT_FIELDS = (
    "model_archive",
    "model_object",
    "layer",
    "primitive",
    "input_rows",
    "category_sequence_valid_rows",
    "ordinal_match_rows",
    "slot_structure_valid_rows",
    "stride_match_rows",
    "valid_payload_range_rows",
    "structural_match_rows",
    "reversed_slot_match",
    "shifted_ordinal_match",
    "status",
    "original_semantics",
    "issue",
)

SUMMARY_FIELDS = (
    "source_report",
    "input_rows",
    "primitive_rows",
    "primitive_groups",
    "groups_with_five_rows",
    "groups_with_structural_match",
    "category_sequence_valid_rows",
    "ordinal_match_rows",
    "slot_structure_valid_rows",
    "stride_match_rows",
    "valid_payload_range_rows",
    "reversed_slot_match_rows",
    "shifted_ordinal_match_rows",
    "input_status_valid_rows",
    "status",
    "provenance",
    "original_semantics",
)


def _int(row: Dict[str, str], field: str) -> int:
    try:
        return int(row[field], 0)
    except (KeyError, TypeError, ValueError) as exc:
        raise ValueError(f"invalid integer in {field}: {row.get(field)!r}") from exc


def _bool(row: Dict[str, str], field: str) -> bool:
    value = row.get(field, "")
    if value not in {"True", "False"}:
        raise ValueError(f"invalid boolean in {field}: {value!r}")
    return value == "True"


def _key(row: Dict[str, str]) -> Tuple[str, str, int, int]:
    archive = row.get("model_archive", "")
    object_name = row.get("model_object", "")
    if not archive or not object_name:
        raise ValueError("topology row has an empty object identity")
    return archive, object_name, _int(row, "layer"), _int(row, "primitive")


def _group(rows: Sequence[Dict[str, str]]) -> Dict[Tuple[str, str, int, int], List[Dict[str, str]]]:
    grouped: Dict[Tuple[str, str, int, int], List[Dict[str, str]]] = defaultdict(list)
    for row in rows:
        grouped[_key(row)].append(row)
    return grouped


def _ordered(rows: Iterable[Dict[str, str]]) -> List[Dict[str, str]]:
    return sorted(rows, key=lambda row: (_int(row, "category"), row.get("symbol_name", "")))


def analyze_rows(rows: Sequence[Dict[str, str]]) -> Tuple[List[Dict[str, str]], Dict[str, str]]:
    if not rows:
        raise ValueError("topology report has no rows")
    required = {
        "model_archive", "model_object", "layer", "primitive", "fetch_slot",
        "fetch_stride", "category", "ordinal", "symbol_name", "stream_stride",
        "element_count", "category_sequence_valid", "key_ordinal_matches_primitive",
        "fetch_slot_structure_valid", "fetch_stride_matches_stream_stride",
        "valid_payload_range", "structural_match", "status", "original_semantics",
    }
    missing = required - set(rows[0])
    if missing:
        raise ValueError(f"topology report is missing columns: {sorted(missing)}")
    if not all(row.get("original_semantics") == ORIGINAL_SEMANTICS for row in rows):
        raise ValueError("topology input changed original_semantics")

    grouped = _group(rows)
    object_groups: Dict[Tuple[str, str], Dict[int, List[Dict[str, str]]]] = defaultdict(lambda: defaultdict(list))
    for row in rows:
        object_groups[(row["model_archive"], row["model_object"])][_int(row, "ordinal")].append(row)

    report: List[Dict[str, str]] = []
    reversed_rows = 0
    shifted_rows = 0
    for key, group in sorted(grouped.items()):
        ordered = _ordered(group)
        category_sequence = all(
            _int(row, "category") == _int(ordered[0], "category") + index
            for index, row in enumerate(ordered)
        ) if ordered else False
        category_sequence_rows = sum(_bool(row, "category_sequence_valid") for row in ordered)
        ordinal_rows = sum(_bool(row, "key_ordinal_matches_primitive") for row in ordered)
        slot_rows = sum(_bool(row, "fetch_slot_structure_valid") for row in ordered)
        stride_rows = sum(_bool(row, "fetch_stride_matches_stream_stride") for row in ordered)
        range_rows = sum(_bool(row, "valid_payload_range") for row in ordered)
        structural_rows = sum(_bool(row, "structural_match") for row in ordered)
        fetch_strides = [_int(row, "fetch_stride") for row in ordered]
        stream_strides = [_int(row, "stream_stride") for row in ordered]
        reversed_match = len(ordered) == 5 and fetch_strides == list(reversed(stream_strides))
        if reversed_match:
            reversed_rows += 1

        object_key = (key[0], key[1])
        next_ordinal = key[3] + 1
        shifted_candidates = _ordered(object_groups[object_key].get(next_ordinal, [])) if next_ordinal < 2**32 else []
        shifted_match = len(ordered) == 5 and len(shifted_candidates) == 5 and fetch_strides == [
            _int(row, "stream_stride") for row in shifted_candidates
        ]
        if shifted_match:
            shifted_rows += 1

        group_status = INPUT_STATUS if structural_rows == len(ordered) and len(ordered) == 5 else "inconclusive-original-primitive-stream-topology"
        issue = "" if group_status == INPUT_STATUS else "raw topology group is incomplete or stride/slot controls did not agree"
        report.append(
            {
                "model_archive": key[0],
                "model_object": key[1],
                "layer": str(key[2]),
                "primitive": str(key[3]),
                "input_rows": str(len(ordered)),
                "category_sequence_valid_rows": str(category_sequence_rows),
                "ordinal_match_rows": str(ordinal_rows),
                "slot_structure_valid_rows": str(slot_rows),
                "stride_match_rows": str(stride_rows),
                "valid_payload_range_rows": str(range_rows),
                "structural_match_rows": str(structural_rows),
                "reversed_slot_match": str(reversed_match),
                "shifted_ordinal_match": str(shifted_match),
                "status": group_status,
                "original_semantics": ORIGINAL_SEMANTICS,
                "issue": issue,
            }
        )

    summary = {
        "source_report": "",
        "input_rows": str(len(rows)),
        "primitive_rows": str(len(grouped)),
        "primitive_groups": str(len(grouped)),
        "groups_with_five_rows": str(sum(len(group) == 5 for group in grouped.values())),
        "groups_with_structural_match": str(sum(row["status"] == INPUT_STATUS for row in report)),
        "category_sequence_valid_rows": str(sum(_bool(row, "category_sequence_valid") for row in rows)),
        "ordinal_match_rows": str(sum(_bool(row, "key_ordinal_matches_primitive") for row in rows)),
        "slot_structure_valid_rows": str(sum(_bool(row, "fetch_slot_structure_valid") for row in rows)),
        "stride_match_rows": str(sum(_bool(row, "fetch_stride_matches_stream_stride") for row in rows)),
        "valid_payload_range_rows": str(sum(_bool(row, "valid_payload_range") for row in rows)),
        "reversed_slot_match_rows": str(reversed_rows),
        "shifted_ordinal_match_rows": str(shifted_rows),
        "input_status_valid_rows": str(sum(row.get("status") in {INPUT_STATUS, "inconclusive-original-primitive-stream-topology"} for row in rows)),
        "status": STATUS if len(grouped) > 0 and all(len(group) == 5 for group in grouped.values()) and shifted_rows > 0 and reversed_rows == 0 else "inconclusive-independent-original-primitive-stream-topology-control",
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
    parser.add_argument("source_report", type=Path)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()
    report, summary = analyze_rows(_read(args.source_report))
    summary["source_report"] = str(args.source_report)
    _write(args.report, REPORT_FIELDS, report)
    _write(args.summary, SUMMARY_FIELDS, [summary])
    print("FIFA Street 2 independent original primitive/__VB topology control")
    for field in SUMMARY_FIELDS:
        print(f"{field}: {summary[field]}")
    print(f"report: {args.report}")
    print(f"summary: {args.summary}")
    return 0 if summary["status"] == STATUS else 1


if __name__ == "__main__":
    raise SystemExit(main())
