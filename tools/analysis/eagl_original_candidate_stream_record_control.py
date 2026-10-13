"""Join original __VB stream records to candidate fetch slot records.

The join is limited to exact primitive/slot keys and raw record metadata.  It
does not assign a stream to position, UV, material, skinning, rigging,
animation, morph, draw or GPU semantics.
"""

from __future__ import annotations

import argparse
import csv
import re
from pathlib import Path
from typing import Dict, List, Sequence, Tuple


STATUS = "verified-independent-original-candidate-stream-record-control"
ORIGINAL_SEMANTICS = "not-claimed"
PROVENANCE = (
    "independent-primitive-slot-key-join+original-__VB-reparse/candidate-slot-record-reparse/"
    "symbol+aux+stride+byte-length+element-count-controls;no-slot-semantic-promotion"
)

REPORT_FIELDS = (
    "model_archive",
    "model_object",
    "layer",
    "primitive",
    "original_stream_rows",
    "candidate_fetch_slot_count",
    "candidate_slot_records",
    "candidate_complete_five_slot_layout",
    "original_complete_five_slot_fetch_sequence",
    "joined_materialized_slot_rows",
    "symbol_exact_rows",
    "auxiliary_exact_rows",
    "fetch_stride_exact_rows",
    "stream_stride_exact_rows",
    "byte_length_exact_rows",
    "element_count_exact_rows",
    "record_exact_rows",
    "candidate_slot_records_unmaterialized_expected",
    "original_input_valid",
    "candidate_input_valid",
    "status",
    "provenance",
    "original_semantics",
    "issue",
)

SUMMARY_FIELDS = (
    "original_source_report",
    "candidate_source_report",
    "original_rows",
    "original_primitive_rows",
    "candidate_rows",
    "joined_primitive_rows",
    "missing_candidate_keys",
    "missing_original_keys",
    "candidate_complete_primitive_rows",
    "candidate_incomplete_primitive_rows",
    "candidate_slot_rows_materialized",
    "joined_materialized_slot_rows",
    "symbol_exact_rows",
    "auxiliary_exact_rows",
    "fetch_stride_exact_rows",
    "stream_stride_exact_rows",
    "byte_length_exact_rows",
    "element_count_exact_rows",
    "record_exact_rows",
    "candidate_slot_records_unmaterialized_expected_rows",
    "original_input_valid_rows",
    "candidate_input_valid_rows",
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
            handle, fieldnames=list(fields), delimiter="\t", lineterminator="\n"
        )
        writer.writeheader()
        writer.writerows({field: row.get(field, "") for field in fields} for row in rows)


def _key(row: Dict[str, str]) -> Tuple[str, str, str, str]:
    return (
        row.get("model_archive", ""),
        row.get("model_object", row.get("object_name", "")),
        row.get("layer", ""),
        row.get("primitive", ""),
    )


def _bool(row: Dict[str, str], field: str) -> bool:
    return row.get(field, "").strip().lower() == "true"


def _int(value: str) -> int | None:
    try:
        return int(value, 0)
    except (TypeError, ValueError):
        return None


def _candidate_records(value: str) -> List[Dict[str, str]]:
    records: List[Dict[str, str]] = []
    for raw in value.split("|") if value else []:
        record = {
            name: match.group(1)
            for name in (
                "slot",
                "aux",
                "command_stride",
                "stream_stride",
                "stream_bytes",
                "elements",
                "stream",
                "valid",
            )
            if (match := re.search(rf"(?:^|;){name}=([^;]+)(?:;|$)", raw))
        }
        if record:
            records.append(record)
    return records


def _candidate_status_valid(row: Dict[str, str]) -> bool:
    return row.get("status") in {
        "verified-bounded-candidate-fetch-record-association",
        "opaque-incomplete-fetch-record-association",
    } and row.get("original_semantics") == ORIGINAL_SEMANTICS


def analyze(
    original_rows: Sequence[Dict[str, str]],
    candidate_rows: Sequence[Dict[str, str]],
) -> Tuple[List[Dict[str, str]], Dict[str, str]]:
    original_by_key: Dict[Tuple[str, str, str, str], List[Dict[str, str]]] = {}
    for row in original_rows:
        # The original topology report keeps a sentinel row for each incomplete
        # primitive. It is evidence of incompleteness, not a fetch slot.
        fetch_slot = _int(row.get("fetch_slot", ""))
        if fetch_slot is not None and fetch_slot >= 0:
            original_by_key.setdefault(_key(row), []).append(row)
    candidate_by_key = {_key(row): row for row in candidate_rows}
    keys = sorted(set(original_by_key) & set(candidate_by_key))
    counters = {
        "joined_materialized_slot_rows": 0,
        "symbol_exact_rows": 0,
        "auxiliary_exact_rows": 0,
        "fetch_stride_exact_rows": 0,
        "stream_stride_exact_rows": 0,
        "byte_length_exact_rows": 0,
        "element_count_exact_rows": 0,
        "record_exact_rows": 0,
        "candidate_slot_records_unmaterialized_expected": 0,
        "original_input_valid": 0,
        "candidate_input_valid": 0,
    }
    report: List[Dict[str, str]] = []
    for key in keys:
        original = original_by_key[key]
        candidate = candidate_by_key[key]
        candidate_records = _candidate_records(candidate.get("slot_records", ""))
        candidate_by_slot = {_int(record.get("slot", "")): record for record in candidate_records}
        original_by_slot = {_int(row.get("fetch_slot", "")): row for row in original}
        expected_unmaterialized = (
            not candidate_records
            and _int(candidate.get("fetch_slot_count", "")) == len(original)
            and not _bool(candidate, "complete_five_slot_layout")
            and not _bool(original[0], "complete_five_slot_fetch_sequence")
            and candidate.get("status") == "opaque-incomplete-fetch-record-association"
        )
        local = {
            "joined_materialized_slot_rows": 0,
            "symbol_exact_rows": 0,
            "auxiliary_exact_rows": 0,
            "fetch_stride_exact_rows": 0,
            "stream_stride_exact_rows": 0,
            "byte_length_exact_rows": 0,
            "element_count_exact_rows": 0,
            "record_exact_rows": 0,
        }
        for slot, candidate_record in candidate_by_slot.items():
            source = original_by_slot.get(slot)
            if source is None:
                continue
            local["joined_materialized_slot_rows"] += 1
            checks = {
                "symbol_exact_rows": candidate_record.get("stream", "") == source.get("symbol_name", ""),
                "auxiliary_exact_rows": _int(candidate_record.get("aux", "")) == _int(source.get("fetch_auxiliary_value", "")),
                "fetch_stride_exact_rows": _int(candidate_record.get("command_stride", "")) == _int(source.get("fetch_stride", "")),
                "stream_stride_exact_rows": _int(candidate_record.get("stream_stride", "")) == _int(source.get("stream_stride", "")),
                "byte_length_exact_rows": _int(candidate_record.get("stream_bytes", "")) == _int(source.get("byte_length", "")),
                "element_count_exact_rows": _int(candidate_record.get("elements", "")) == _int(source.get("element_count", "")),
            }
            for field, value in checks.items():
                local[field] += int(value)
            local["record_exact_rows"] += int(all(checks.values()) and candidate_record.get("valid") == "True")
        for field, value in local.items():
            counters[field] += value
        counters["candidate_slot_records_unmaterialized_expected"] += int(expected_unmaterialized)
        original_valid = all(
            row.get("status") in {
                "verified-bounded-original-primitive-stream-topology",
                "inconclusive-original-primitive-stream-topology",
            }
            and row.get("original_semantics") == ORIGINAL_SEMANTICS
            for row in original
        )
        candidate_valid = _candidate_status_valid(candidate)
        counters["original_input_valid"] += int(original_valid)
        counters["candidate_input_valid"] += int(candidate_valid)
        primitive_valid = (
            local["record_exact_rows"] == len(candidate_records) == len(original)
            if candidate_records
            else expected_unmaterialized
        ) and original_valid and candidate_valid
        report.append(
            {
                "model_archive": key[0],
                "model_object": key[1],
                "layer": key[2],
                "primitive": key[3],
                "original_stream_rows": str(len(original)),
                "candidate_fetch_slot_count": candidate.get("fetch_slot_count", ""),
                "candidate_slot_records": str(len(candidate_records)),
                "candidate_complete_five_slot_layout": candidate.get("complete_five_slot_layout", ""),
                "original_complete_five_slot_fetch_sequence": original[0].get("complete_five_slot_fetch_sequence", ""),
                "joined_materialized_slot_rows": str(local["joined_materialized_slot_rows"]),
                "symbol_exact_rows": str(local["symbol_exact_rows"]),
                "auxiliary_exact_rows": str(local["auxiliary_exact_rows"]),
                "fetch_stride_exact_rows": str(local["fetch_stride_exact_rows"]),
                "stream_stride_exact_rows": str(local["stream_stride_exact_rows"]),
                "byte_length_exact_rows": str(local["byte_length_exact_rows"]),
                "element_count_exact_rows": str(local["element_count_exact_rows"]),
                "record_exact_rows": str(local["record_exact_rows"]),
                "candidate_slot_records_unmaterialized_expected": str(expected_unmaterialized),
                "original_input_valid": str(original_valid),
                "candidate_input_valid": str(candidate_valid),
                "status": STATUS if primitive_valid else f"inconclusive-{STATUS}",
                "provenance": PROVENANCE,
                "original_semantics": ORIGINAL_SEMANTICS,
                "issue": "" if primitive_valid else "one or more structural stream-record controls failed",
            }
        )
    original_primitive_rows = len(original_by_key)
    candidate_complete_primitive_rows = sum(
        int(_bool(row, "complete_five_slot_layout")) for row in candidate_rows
    )
    complete = (
        len(original_rows) == 11820
        and original_primitive_rows == 2364
        and len(candidate_rows) == 218
        and len(keys) == 218
        and not (set(candidate_by_key) - set(original_by_key))
        and candidate_complete_primitive_rows == 216
        and counters["candidate_slot_records_unmaterialized_expected"] == 2
        and counters["candidate_input_valid"] == 218
        and counters["original_input_valid"] == 218
        and counters["joined_materialized_slot_rows"] == 1080
        and counters["symbol_exact_rows"] == 1080
        and counters["auxiliary_exact_rows"] == 1080
        and counters["fetch_stride_exact_rows"] == 1080
        and counters["stream_stride_exact_rows"] == 1080
        and counters["byte_length_exact_rows"] == 1080
        and counters["element_count_exact_rows"] == 1080
        and counters["record_exact_rows"] == 1080
        and all(row["status"] == STATUS for row in report)
    )
    summary = {
        "original_source_report": "",
        "candidate_source_report": "",
        "original_rows": str(len(original_rows)),
        "original_primitive_rows": str(original_primitive_rows),
        "candidate_rows": str(len(candidate_rows)),
        "joined_primitive_rows": str(len(keys)),
        "missing_candidate_keys": str(len(set(original_by_key) - set(candidate_by_key))),
        "missing_original_keys": str(len(set(candidate_by_key) - set(original_by_key))),
        "candidate_complete_primitive_rows": str(candidate_complete_primitive_rows),
        "candidate_incomplete_primitive_rows": str(len(candidate_rows) - candidate_complete_primitive_rows),
        "candidate_slot_rows_materialized": str(sum(len(_candidate_records(row.get("slot_records", ""))) for row in candidate_rows)),
        **{field: str(counters[field]) for field in counters if field not in {"candidate_slot_records_unmaterialized_expected", "original_input_valid", "candidate_input_valid"}},
        "candidate_slot_records_unmaterialized_expected_rows": str(counters["candidate_slot_records_unmaterialized_expected"]),
        "original_input_valid_rows": str(counters["original_input_valid"]),
        "candidate_input_valid_rows": str(counters["candidate_input_valid"]),
        "status": STATUS if complete else f"inconclusive-{STATUS}",
        "provenance": PROVENANCE,
        "original_semantics": ORIGINAL_SEMANTICS,
    }
    return report, summary


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("original_report", type=Path)
    parser.add_argument("candidate_report", type=Path)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()
    report, summary = analyze(_read(args.original_report), _read(args.candidate_report))
    summary["original_source_report"] = str(args.original_report)
    summary["candidate_source_report"] = str(args.candidate_report)
    _write(args.report, REPORT_FIELDS, report)
    _write(args.summary, SUMMARY_FIELDS, [summary])
    print("FIFA Street 2 independent original/candidate stream-record control")
    for field in SUMMARY_FIELDS:
        print(f"{field}: {summary[field]}")
    print(f"report: {args.report}")
    print(f"summary: {args.summary}")
    return 0 if summary["status"] == STATUS else 1


if __name__ == "__main__":
    raise SystemExit(main())
