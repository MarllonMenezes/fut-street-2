"""Independently reparse the raw command/payload identity control report.

This control validates candidate counts, bounded identity hashes and exact
command-payload ordinal lists from the C# audit. A zero exact-byte match is
preserved as a negative control and is not promoted to a semantic conclusion.
"""

from __future__ import annotations

import argparse
import csv
import re
from pathlib import Path
from typing import Dict, List, Sequence, Tuple


STATUS = "verified-independent-original-primitive-stream-command-payload-identity-control"
INPUT_STATUS = "verified-bounded-original-primitive-stream-command-payload-identity-control"
ORIGINAL_SEMANTICS = "not-claimed"
PROVENANCE = (
    "independent-TSV-reparse+raw-identity-hash/list-and-command-payload-ordinal-coherence;"
    "zero-exact-byte-match-preserved;no-semantic-promotion"
)
HASH_RE = re.compile(r"^[0-9A-Fa-f]{64}$")

REPORT_FIELDS = (
    "model_archive", "model_object", "layer", "primitive", "input_status_valid",
    "fetch_command_count", "candidate_ordinal_count", "raw_identity_candidate_count",
    "distinct_raw_identity_count", "duplicate_raw_identity_count",
    "command_payload_exact_candidate_count", "raw_identity_hash_count",
    "command_payload_exact_ordinal_count", "current_ordinal_present",
    "current_ordinal_raw_identity_unique", "current_ordinal_command_payload_exact",
    "current_ordinal_uniquely_discriminated_by_command_payload",
    "shifted_ordinal_command_payload_exact", "candidate_list_coherent", "status",
    "original_semantics", "issue",
)

SUMMARY_FIELDS = (
    "source_report", "input_rows", "primitive_rows", "complete_fetch_rows",
    "candidate_ordinal_rows", "raw_identity_candidate_rows", "distinct_raw_identity_rows",
    "duplicate_raw_identity_rows", "command_payload_exact_candidate_rows",
    "current_ordinal_rows", "current_ordinal_raw_identity_unique_rows",
    "current_ordinal_command_payload_exact_rows",
    "current_ordinal_uniquely_discriminated_by_command_payload_rows",
    "shifted_ordinal_command_payload_exact_rows", "input_status_valid_rows",
    "candidate_list_coherent_rows", "status", "provenance", "original_semantics",
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
        raise ValueError("command/payload identity row has an empty object identity")
    return archive, object_name, _int(row, "layer"), _int(row, "primitive")


def _identity_hashes(row: Dict[str, str]) -> List[Tuple[int, str]]:
    value = row.get("raw_identity_hashes", "")
    if not value:
        return []
    result: List[Tuple[int, str]] = []
    for item in value.split("|"):
        if not item or ":" not in item:
            raise ValueError(f"invalid raw identity hash item: {item!r}")
        ordinal_text, digest = item.split(":", 1)
        try:
            ordinal = int(ordinal_text, 0)
        except ValueError as exc:
            raise ValueError(f"invalid raw identity ordinal: {ordinal_text!r}") from exc
        if not HASH_RE.fullmatch(digest):
            raise ValueError(f"invalid raw identity SHA-256: {digest!r}")
        result.append((ordinal, digest.upper()))
    if len({ordinal for ordinal, _ in result}) != len(result):
        raise ValueError("raw identity hash list contains duplicate ordinals")
    return result


def _ordinals(row: Dict[str, str], field: str) -> List[int]:
    value = row.get(field, "")
    if not value:
        return []
    try:
        return [int(item, 0) for item in value.split(",") if item]
    except ValueError as exc:
        raise ValueError(f"invalid ordinal list in {field}: {value!r}") from exc


def analyze_rows(rows: Sequence[Dict[str, str]]) -> Tuple[List[Dict[str, str]], Dict[str, str]]:
    if not rows:
        raise ValueError("command/payload identity report has no rows")
    required = {
        "model_archive", "model_object", "layer", "primitive", "fetch_command_count",
        "candidate_ordinal_count", "raw_identity_candidate_count", "distinct_raw_identity_count",
        "duplicate_raw_identity_count", "command_payload_exact_candidate_count",
        "raw_identity_hashes", "command_payload_exact_ordinals", "current_ordinal_present",
        "current_ordinal_raw_identity_unique", "current_ordinal_command_payload_exact",
        "current_ordinal_uniquely_discriminated_by_command_payload",
        "shifted_ordinal_command_payload_exact", "status", "original_semantics",
    }
    missing = required - set(rows[0])
    if missing:
        raise ValueError(f"command/payload identity report is missing columns: {sorted(missing)}")
    if not all(row.get("original_semantics") == ORIGINAL_SEMANTICS for row in rows):
        raise ValueError("command/payload identity input changed original_semantics")

    report: List[Dict[str, str]] = []
    complete_fetch_rows = 0
    candidate_rows = 0
    raw_identity_rows = 0
    distinct_identity_rows = 0
    duplicate_identity_rows = 0
    exact_rows = 0
    current_rows = 0
    current_unique_rows = 0
    current_exact_rows = 0
    current_unique_exact_rows = 0
    shifted_exact_rows = 0
    input_status_valid_rows = 0
    coherent_rows = 0

    for row in sorted(rows, key=_key):
        key = _key(row)
        fetch_count = _int(row, "fetch_command_count")
        candidate_count = _int(row, "candidate_ordinal_count")
        raw_count = _int(row, "raw_identity_candidate_count")
        distinct_count = _int(row, "distinct_raw_identity_count")
        duplicate_count = _int(row, "duplicate_raw_identity_count")
        exact_count = _int(row, "command_payload_exact_candidate_count")
        hashes = _identity_hashes(row)
        exact_ordinals = _ordinals(row, "command_payload_exact_ordinals")
        current_present = _bool(row, "current_ordinal_present")
        current_unique = _bool(row, "current_ordinal_raw_identity_unique")
        current_exact = _bool(row, "current_ordinal_command_payload_exact")
        current_unique_exact = _bool(row, "current_ordinal_uniquely_discriminated_by_command_payload")
        shifted_exact = _bool(row, "shifted_ordinal_command_payload_exact")
        hash_ordinals = {ordinal for ordinal, _ in hashes}
        hash_values = [digest for _, digest in hashes]
        current_digest = next((digest for ordinal, digest in hashes if ordinal == key[3]), None)
        computed_current_unique = current_digest is not None and hash_values.count(current_digest) == 1
        computed_current_exact = key[3] in exact_ordinals
        coherent = (
            candidate_count == raw_count == len(hashes)
            and distinct_count == len(set(hash_values))
            and duplicate_count == candidate_count - distinct_count
            and exact_count == len(exact_ordinals)
            and len(set(exact_ordinals)) == len(exact_ordinals)
            and set(exact_ordinals).issubset(hash_ordinals)
            and current_present == (key[3] in hash_ordinals)
            and current_unique == computed_current_unique
            and current_exact == computed_current_exact
            and current_unique_exact == (current_exact and exact_count == 1)
            and shifted_exact == ((key[3] + 1) in exact_ordinals)
        )
        if fetch_count == 5:
            complete_fetch_rows += 1
        candidate_rows += candidate_count
        raw_identity_rows += raw_count
        distinct_identity_rows += distinct_count
        duplicate_identity_rows += duplicate_count
        exact_rows += exact_count
        current_rows += int(current_present)
        current_unique_rows += int(current_unique)
        current_exact_rows += int(current_exact)
        current_unique_exact_rows += int(current_unique_exact)
        shifted_exact_rows += int(shifted_exact)
        input_status_valid_rows += int(row.get("status") in {INPUT_STATUS, "inconclusive-original-primitive-stream-command-payload-identity-control"})
        coherent_rows += int(coherent)
        report.append({
            "model_archive": key[0], "model_object": key[1], "layer": str(key[2]), "primitive": str(key[3]),
            "input_status_valid": str(row.get("status") == INPUT_STATUS),
            "fetch_command_count": str(fetch_count), "candidate_ordinal_count": str(candidate_count),
            "raw_identity_candidate_count": str(raw_count), "distinct_raw_identity_count": str(distinct_count),
            "duplicate_raw_identity_count": str(duplicate_count), "command_payload_exact_candidate_count": str(exact_count),
            "raw_identity_hash_count": str(len(hashes)), "command_payload_exact_ordinal_count": str(len(exact_ordinals)),
            "current_ordinal_present": str(current_present), "current_ordinal_raw_identity_unique": str(current_unique),
            "current_ordinal_command_payload_exact": str(current_exact),
            "current_ordinal_uniquely_discriminated_by_command_payload": str(current_unique_exact),
            "shifted_ordinal_command_payload_exact": str(shifted_exact),
            "candidate_list_coherent": str(coherent),
            "status": STATUS if coherent else "inconclusive-independent-original-primitive-stream-command-payload-identity-control",
            "original_semantics": ORIGINAL_SEMANTICS,
            "issue": "" if coherent else "candidate counts, hashes, exact ordinals or current flags are incoherent",
        })

    summary = {
        "source_report": "", "input_rows": str(len(rows)), "primitive_rows": str(len(rows)),
        "complete_fetch_rows": str(complete_fetch_rows), "candidate_ordinal_rows": str(candidate_rows),
        "raw_identity_candidate_rows": str(raw_identity_rows), "distinct_raw_identity_rows": str(distinct_identity_rows),
        "duplicate_raw_identity_rows": str(duplicate_identity_rows),
        "command_payload_exact_candidate_rows": str(exact_rows), "current_ordinal_rows": str(current_rows),
        "current_ordinal_raw_identity_unique_rows": str(current_unique_rows),
        "current_ordinal_command_payload_exact_rows": str(current_exact_rows),
        "current_ordinal_uniquely_discriminated_by_command_payload_rows": str(current_unique_exact_rows),
        "shifted_ordinal_command_payload_exact_rows": str(shifted_exact_rows),
        "input_status_valid_rows": str(input_status_valid_rows), "candidate_list_coherent_rows": str(coherent_rows),
        "status": STATUS if (
            len(rows) > 0 and complete_fetch_rows > 0 and coherent_rows == len(rows)
            and current_rows == len(rows) and current_unique_rows == len(rows)
            and exact_rows == 0 and current_exact_rows == 0
            and current_unique_exact_rows == 0 and shifted_exact_rows == 0
        ) else "inconclusive-independent-original-primitive-stream-command-payload-identity-control",
        "provenance": PROVENANCE, "original_semantics": ORIGINAL_SEMANTICS,
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
    print("FIFA Street 2 independent original primitive/__VB command-payload identity control")
    for field in SUMMARY_FIELDS:
        print(f"{field}: {summary[field]}")
    print(f"report: {args.report}")
    print(f"summary: {args.summary}")
    return 0 if summary["status"] == STATUS else 1


if __name__ == "__main__":
    raise SystemExit(main())
