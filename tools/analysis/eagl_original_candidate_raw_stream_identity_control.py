"""Independently reparse candidate fetch rows and raw payload identity rows.

The control verifies object/primitive identity, five-slot coverage, metadata
equality, and SHA-256 formatting of the bounded raw __VB payload hashes. It
does not assign any slot or payload a vertex, UV, material, skinning, rig,
animation, morph, or GPU semantic.
"""

from __future__ import annotations

import argparse
import csv
import re
from pathlib import Path
from typing import Dict, Iterable, List, Sequence, Tuple


STATUS = "verified-independent-original-candidate-raw-stream-identity-control"
EXPECTED_UNMATERIALIZED_STATUS = "verified-expected-unmaterialized-candidate-fetch-control"
ORIGINAL_SEMANTICS = "not-claimed"
HEX64 = re.compile(r"^[0-9A-Fa-f]{64}$")

REPORT_FIELDS = (
    "model_archive",
    "model_object",
    "layer",
    "primitive",
    "candidate_complete_five_slot_layout",
    "candidate_slot_rows",
    "raw_valid_slot_rows",
    "raw_metadata_exact_rows",
    "raw_payload_hash_rows",
    "distinct_raw_payload_hashes",
    "slot1_raw_payload_sha256",
    "raw_payload_hashes",
    "candidate_identity",
    "status",
    "original_semantics",
    "issue",
)

SUMMARY_FIELDS = (
    "candidate_rows",
    "raw_rows",
    "candidate_object_rows",
    "raw_object_rows",
    "joined_rows",
    "candidate_complete_rows",
    "raw_complete_rows",
    "expected_unmaterialized_rows",
    "candidate_slot_rows",
    "raw_valid_slot_rows",
    "raw_metadata_exact_rows",
    "raw_payload_hash_rows",
    "raw_hash_format_rows",
    "raw_hash_distinctness_rows",
    "missing_candidate_keys",
    "missing_raw_keys",
    "status",
    "original_semantics",
)


def _read(path: Path) -> List[Dict[str, str]]:
    with path.open("r", encoding="utf-8-sig", newline="") as handle:
        return list(csv.DictReader(handle, delimiter="\t"))


def _write(path: Path, fields: Sequence[str], rows: Iterable[Dict[str, str]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=list(fields), delimiter="\t", lineterminator="\n")
        writer.writeheader()
        writer.writerows({field: row.get(field, "") for field in fields} for row in rows)


def _key(row: Dict[str, str]) -> Tuple[str, str, str, str]:
    return (
        row.get("model_archive", ""),
        row.get("model_object", ""),
        row.get("layer", ""),
        row.get("primitive", ""),
    )


def _true(row: Dict[str, str], field: str) -> bool:
    return row.get(field, "").strip().lower() == "true"


def _int(row: Dict[str, str], field: str) -> int:
    return int(row.get(field, "0"))


def _candidate_slot_count(row: Dict[str, str]) -> int:
    value = row.get("slot_records", "")
    return len([item for item in value.split("|") if item])


def _valid_slot_count(row: Dict[str, str]) -> int:
    value = row.get("slot_records", "")
    return sum(item.endswith(";valid=True") for item in value.split("|") if item)


def _hash_pairs(value: str) -> List[Tuple[int, str]]:
    pairs: List[Tuple[int, str]] = []
    for token in value.split("|"):
        if not token:
            continue
        prefix, separator, digest = token.partition(":")
        if not separator or not prefix.startswith("slot="):
            continue
        try:
            slot = int(prefix[5:])
        except ValueError:
            continue
        pairs.append((slot, digest))
    return pairs


def analyze(
    candidate_rows: Sequence[Dict[str, str]],
    raw_rows: Sequence[Dict[str, str]],
) -> Tuple[List[Dict[str, str]], Dict[str, str]]:
    candidates = {_key(row): row for row in candidate_rows}
    raw = {_key(row): row for row in raw_rows}
    keys = sorted(set(candidates) | set(raw))
    report: List[Dict[str, str]] = []
    candidate_complete_rows = 0
    raw_complete_rows = 0
    expected_unmaterialized_rows = 0
    candidate_slot_rows = 0
    raw_valid_slot_rows = 0
    raw_metadata_exact_rows = 0
    raw_payload_hash_rows = 0
    raw_hash_format_rows = 0
    raw_hash_distinctness_rows = 0

    for key in keys:
        candidate = candidates.get(key)
        identity = raw.get(key)
        candidate_slots = _candidate_slot_count(candidate) if candidate else 0
        candidate_valid_slots = _valid_slot_count(candidate) if candidate else 0
        if candidate:
            candidate_slot_rows += candidate_slots
        if candidate and _true(candidate, "complete_five_slot_layout"):
            candidate_complete_rows += 1
        if identity and identity.get("status") == STATUS:
            raw_complete_rows += 1
        if identity and identity.get("status") == EXPECTED_UNMATERIALIZED_STATUS:
            expected_unmaterialized_rows += 1

        raw_valid = _int(identity, "raw_valid_slot_rows") if identity else 0
        raw_metadata = _int(identity, "raw_metadata_exact_rows") if identity else 0
        raw_hash_rows = _int(identity, "raw_payload_hash_rows") if identity else 0
        raw_valid_slot_rows += raw_valid
        raw_metadata_exact_rows += raw_metadata
        raw_payload_hash_rows += raw_hash_rows
        pairs = _hash_pairs(identity.get("raw_payload_hashes", "")) if identity else []
        format_ok = bool(identity) and (
            identity.get("status") == STATUS
            and
            raw_hash_rows == len(pairs) and
            all(slot in {0, 1, 2, 3, 4} and bool(HEX64.fullmatch(digest)) for slot, digest in pairs)
        )
        distinct_ok = bool(identity) and (
            identity.get("status") == STATUS
            and
            len({digest for _, digest in pairs}) == _int(identity, "distinct_raw_payload_hashes")
        )
        raw_hash_format_rows += int(format_ok)
        raw_hash_distinctness_rows += int(distinct_ok)
        if identity:
            raw_hash_rows_for_report = identity.get("raw_payload_hashes", "")
        else:
            raw_hash_rows_for_report = ""

        row_identity = bool(candidate and identity)
        row_valid = (
            row_identity
            and candidate_valid_slots == candidate_slots
            and (
                identity.get("status") == STATUS
                and candidate_slots == 5
                and raw_valid == raw_metadata == raw_hash_rows == 5
                and format_ok
                and distinct_ok
            or identity.get("status") == EXPECTED_UNMATERIALIZED_STATUS
                and candidate_slots == 0
                and _int(candidate, "fetch_slot_count") == 4
                and _int(candidate, "fetch_binding_count") == 0
                and not _true(candidate, "complete_five_slot_layout")
            )
            and identity.get("original_semantics") == ORIGINAL_SEMANTICS
            and candidate.get("original_semantics") == ORIGINAL_SEMANTICS
        )
        row_status = STATUS if row_valid else "inconclusive-" + STATUS
        report.append(
            {
                "model_archive": key[0],
                "model_object": key[1],
                "layer": key[2],
                "primitive": key[3],
                "candidate_complete_five_slot_layout": candidate.get("complete_five_slot_layout", "") if candidate else "",
                "candidate_slot_rows": str(candidate_slots),
                "raw_valid_slot_rows": str(raw_valid),
                "raw_metadata_exact_rows": str(raw_metadata),
                "raw_payload_hash_rows": str(raw_hash_rows),
                "distinct_raw_payload_hashes": identity.get("distinct_raw_payload_hashes", "") if identity else "",
                "slot1_raw_payload_sha256": identity.get("slot1_raw_payload_sha256", "") if identity else "",
                "raw_payload_hashes": raw_hash_rows_for_report,
                "candidate_identity": str(row_identity),
                "status": row_status,
                "original_semantics": ORIGINAL_SEMANTICS,
                "issue": "" if row_valid else "candidate and raw payload identity controls did not reconcile",
            }
        )

    summary = {
        "candidate_rows": str(len(candidate_rows)),
        "raw_rows": str(len(raw_rows)),
        "candidate_object_rows": str(len({(row.get("model_archive", ""), row.get("model_object", "")) for row in candidate_rows})),
        "raw_object_rows": str(len({(row.get("model_archive", ""), row.get("model_object", "")) for row in raw_rows})),
        "joined_rows": str(sum(row["candidate_identity"] == "True" for row in report)),
        "candidate_complete_rows": str(candidate_complete_rows),
        "raw_complete_rows": str(raw_complete_rows),
        "expected_unmaterialized_rows": str(expected_unmaterialized_rows),
        "candidate_slot_rows": str(candidate_slot_rows),
        "raw_valid_slot_rows": str(raw_valid_slot_rows),
        "raw_metadata_exact_rows": str(raw_metadata_exact_rows),
        "raw_payload_hash_rows": str(raw_payload_hash_rows),
        "raw_hash_format_rows": str(raw_hash_format_rows),
        "raw_hash_distinctness_rows": str(raw_hash_distinctness_rows),
        "missing_candidate_keys": str(sum(key not in candidates for key in keys)),
        "missing_raw_keys": str(sum(key not in raw for key in keys)),
        "status": STATUS if (
            len(candidate_rows) == len(raw_rows) == 218
            and len(candidates) == len(raw) == 218
            and len({(row.get("model_archive", ""), row.get("model_object", "")) for row in candidate_rows}) == 24
            and len({(row.get("model_archive", ""), row.get("model_object", "")) for row in raw_rows}) == 24
            and candidate_complete_rows == raw_complete_rows == 216
            and expected_unmaterialized_rows == 2
            and candidate_slot_rows == raw_valid_slot_rows == raw_metadata_exact_rows == raw_payload_hash_rows == 1080
            and raw_hash_format_rows == 216
            and raw_hash_distinctness_rows == 216
            and all(row["status"] == STATUS for row in report)
        ) else "inconclusive-" + STATUS,
        "original_semantics": ORIGINAL_SEMANTICS,
    }
    return report, summary


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("candidate_report", type=Path)
    parser.add_argument("raw_report", type=Path)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()
    report, summary = analyze(_read(args.candidate_report), _read(args.raw_report))
    _write(args.report, REPORT_FIELDS, report)
    _write(args.summary, SUMMARY_FIELDS, [summary])
    print("FIFA Street 2 independent candidate/raw __VB payload identity control")
    for field in SUMMARY_FIELDS:
        print(f"{field}: {summary[field]}")
    print(f"report: {args.report}")
    print(f"summary: {args.summary}")
    return 0 if summary["status"] == STATUS else 1


if __name__ == "__main__":
    raise SystemExit(main())
