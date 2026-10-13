"""Independently validate the opaque candidate slot symbol structure.

The control checks only the lexical category/ordinal relationship already
present in the candidate fetch records, then joins each row to the separate
raw-payload identity report.  It does not assign any slot a vertex, UV,
material, normal, weight, skinning, rig, animation, morph, or GPU meaning.
"""

from __future__ import annotations

import argparse
import csv
import re
from pathlib import Path
from typing import Dict, Iterable, List, Sequence, Tuple


STATUS = "verified-independent-original-candidate-slot-category-ordinal-control"
RAW_STATUS = "verified-independent-original-candidate-raw-stream-identity-control"
EXPECTED_UNMATERIALIZED_STATUS = "verified-expected-unmaterialized-candidate-fetch-control"
ORIGINAL_SEMANTICS = "not-claimed"
SYMBOL_PATTERN = re.compile(r"^__VB:::__GPvb_(\d+)_(\d+)_(.+)$")

REPORT_FIELDS = (
    "model_archive",
    "model_object",
    "layer",
    "primitive",
    "candidate_slot_rows",
    "symbol_format_rows",
    "category_exact_rows",
    "ordinal_exact_rows",
    "raw_identity_status",
    "status",
    "original_semantics",
    "issue",
)

SUMMARY_FIELDS = (
    "candidate_rows",
    "raw_identity_rows",
    "candidate_object_rows",
    "raw_identity_object_rows",
    "joined_rows",
    "candidate_complete_rows",
    "raw_identity_complete_rows",
    "expected_unmaterialized_rows",
    "candidate_slot_rows",
    "symbol_format_rows",
    "category_exact_rows",
    "ordinal_exact_rows",
    "complete_control_rows",
    "missing_candidate_keys",
    "missing_raw_identity_keys",
    "status",
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


def _key(row: Dict[str, str]) -> Tuple[str, str, str, str]:
    return (
        row.get("model_archive", ""),
        row.get("model_object", ""),
        row.get("layer", ""),
        row.get("primitive", ""),
    )


def _int(row: Dict[str, str], field: str) -> int:
    return int(row.get(field, "0"))


def _true(row: Dict[str, str], field: str) -> bool:
    return row.get(field, "").strip().lower() == "true"


def _slots(row: Dict[str, str]) -> List[Dict[str, str]]:
    output: List[Dict[str, str]] = []
    for record in row.get("slot_records", "").split("|"):
        if not record:
            continue
        fields: Dict[str, str] = {}
        for field in record.split(";"):
            name, separator, value = field.partition("=")
            if separator:
                fields[name] = value
        if fields:
            output.append(fields)
    return output


def _symbol_parts(value: str) -> Tuple[int, int] | None:
    match = SYMBOL_PATTERN.fullmatch(value)
    if not match:
        return None
    try:
        return int(match.group(1)), int(match.group(2))
    except ValueError:
        return None


def analyze(
    candidate_rows: Sequence[Dict[str, str]],
    raw_identity_rows: Sequence[Dict[str, str]],
) -> Tuple[List[Dict[str, str]], Dict[str, str]]:
    candidates = {_key(row): row for row in candidate_rows}
    raw_identity = {_key(row): row for row in raw_identity_rows}
    keys = sorted(set(candidates) | set(raw_identity))
    report: List[Dict[str, str]] = []
    candidate_complete_rows = 0
    raw_identity_complete_rows = 0
    expected_unmaterialized_rows = 0
    candidate_slot_rows = 0
    symbol_format_rows = 0
    category_exact_rows = 0
    ordinal_exact_rows = 0
    complete_control_rows = 0

    for key in keys:
        candidate = candidates.get(key)
        raw = raw_identity.get(key)
        slots = _slots(candidate) if candidate else []
        primitive = int(key[3]) if key[3].isdigit() else -1
        candidate_slot_count = len(slots)
        symbol_format_count = 0
        category_exact_count = 0
        ordinal_exact_count = 0
        for slot in slots:
            if slot.get("valid", "").strip().lower() != "true":
                continue
            parts = _symbol_parts(slot.get("stream", ""))
            if parts is None:
                continue
            symbol_format_count += 1
            category, ordinal = parts
            try:
                slot_index = int(slot.get("slot", "-1"))
            except ValueError:
                slot_index = -1
            category_exact_count += int(category == 10 + slot_index)
            ordinal_exact_count += int(ordinal == primitive)

        candidate_slot_rows += candidate_slot_count
        symbol_format_rows += symbol_format_count
        category_exact_rows += category_exact_count
        ordinal_exact_rows += ordinal_exact_count
        if candidate and _true(candidate, "complete_five_slot_layout"):
            candidate_complete_rows += 1
        if raw and raw.get("status") == RAW_STATUS:
            raw_identity_complete_rows += 1
        if raw and raw.get("status") == EXPECTED_UNMATERIALIZED_STATUS:
            expected_unmaterialized_rows += 1

        joined = candidate is not None and raw is not None
        complete = (
            joined
            and candidate is not None
            and raw is not None
            and candidate.get("original_semantics") == ORIGINAL_SEMANTICS
            and raw.get("original_semantics") == ORIGINAL_SEMANTICS
            and (
                (
                    raw.get("status") == RAW_STATUS
                    and candidate_slot_count == 5
                    and symbol_format_count == 5
                    and category_exact_count == 5
                    and ordinal_exact_count == 5
                    and _true(candidate, "complete_five_slot_layout")
                    and _int(candidate, "fetch_slot_count") == 5
                    and _int(candidate, "fetch_binding_count") == 5
                    and _int(raw, "raw_valid_slot_rows") == 5
                    and _int(raw, "raw_metadata_exact_rows") == 5
                    and _int(raw, "raw_payload_hash_rows") == 5
                )
                or (
                    raw.get("status") == EXPECTED_UNMATERIALIZED_STATUS
                    and candidate_slot_count == 0
                    and _int(candidate, "fetch_slot_count") == 4
                    and _int(candidate, "fetch_binding_count") == 0
                    and not _true(candidate, "complete_five_slot_layout")
                )
            )
        )
        complete_control_rows += int(complete)
        report.append(
            {
                "model_archive": key[0],
                "model_object": key[1],
                "layer": key[2],
                "primitive": key[3],
                "candidate_slot_rows": str(candidate_slot_count),
                "symbol_format_rows": str(symbol_format_count),
                "category_exact_rows": str(category_exact_count),
                "ordinal_exact_rows": str(ordinal_exact_count),
                "raw_identity_status": raw.get("status", "") if raw else "",
                "status": STATUS if complete else "inconclusive-" + STATUS,
                "original_semantics": ORIGINAL_SEMANTICS,
                "issue": "" if complete else "candidate slot symbol structure did not reconcile with raw identity control",
            }
        )

    candidate_object_keys = {(row.get("model_archive", ""), row.get("model_object", "")) for row in candidate_rows}
    raw_object_keys = {(row.get("model_archive", ""), row.get("model_object", "")) for row in raw_identity_rows}
    summary = {
        "candidate_rows": str(len(candidate_rows)),
        "raw_identity_rows": str(len(raw_identity_rows)),
        "candidate_object_rows": str(len(candidate_object_keys)),
        "raw_identity_object_rows": str(len(raw_object_keys)),
        "joined_rows": str(sum(key in candidates and key in raw_identity for key in keys)),
        "candidate_complete_rows": str(candidate_complete_rows),
        "raw_identity_complete_rows": str(raw_identity_complete_rows),
        "expected_unmaterialized_rows": str(expected_unmaterialized_rows),
        "candidate_slot_rows": str(candidate_slot_rows),
        "symbol_format_rows": str(symbol_format_rows),
        "category_exact_rows": str(category_exact_rows),
        "ordinal_exact_rows": str(ordinal_exact_rows),
        "complete_control_rows": str(complete_control_rows),
        "missing_candidate_keys": str(sum(key not in candidates for key in keys)),
        "missing_raw_identity_keys": str(sum(key not in raw_identity for key in keys)),
        "status": STATUS if (
            len(candidate_rows) == len(raw_identity_rows) == 218
            and len(candidates) == len(raw_identity) == 218
            and len(candidate_object_keys) == len(raw_object_keys) == 24
            and candidate_complete_rows == raw_identity_complete_rows == 216
            and expected_unmaterialized_rows == 2
            and candidate_slot_rows == symbol_format_rows == category_exact_rows == ordinal_exact_rows == 1080
            and complete_control_rows == 218
            and all(row["status"] == STATUS for row in report)
        ) else "inconclusive-" + STATUS,
        "original_semantics": ORIGINAL_SEMANTICS,
    }
    return report, summary


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("candidate_report", type=Path)
    parser.add_argument("raw_identity_report", type=Path)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()
    report, summary = analyze(_read(args.candidate_report), _read(args.raw_identity_report))
    _write(args.report, REPORT_FIELDS, report)
    _write(args.summary, SUMMARY_FIELDS, [summary])
    print("FIFA Street 2 independent opaque candidate slot category/ordinal control")
    for field in SUMMARY_FIELDS:
        print(f"{field}: {summary[field]}")
    print(f"report: {args.report}")
    print(f"summary: {args.summary}")
    return 0 if summary["status"] == STATUS else 1


if __name__ == "__main__":
    raise SystemExit(main())
