"""Independently reparse the opaque primitive/__VB discrimination report.

This control checks the report's candidate-list coherence and preserves the
negative result: no nonzero fetch-command word occurred at an aligned word in
the validated candidate __VB record/descriptor or bounded payload prefix.
That absence is not promoted to a semantic conclusion.
"""

from __future__ import annotations

import argparse
import csv
from pathlib import Path
from typing import Dict, List, Sequence, Tuple


STATUS = "verified-independent-original-primitive-stream-opaque-discrimination-control"
INPUT_STATUS = "verified-bounded-original-primitive-stream-opaque-discrimination"
ORIGINAL_SEMANTICS = "not-claimed"
PROVENANCE = (
    "independent-TSV-reparse+candidate-list-and-opaque-word-overlap-coherence;"
    "zero-record-and-payload-prefix-overlap-preserved;no-semantic-promotion"
)

REPORT_FIELDS = (
    "model_archive", "model_object", "layer", "primitive", "input_status_valid",
    "fetch_word_count", "candidate_ordinal_count", "stride_matching_candidate_count",
    "metadata_matching_candidate_count", "payload_prefix_matching_candidate_count",
    "current_ordinal_present", "current_ordinal_metadata_match",
    "current_ordinal_payload_prefix_match", "current_ordinal_uniquely_discriminated_by_metadata",
    "current_ordinal_uniquely_discriminated_by_payload_prefix", "next_ordinal_stride_match",
    "next_ordinal_metadata_match", "next_ordinal_payload_prefix_match",
    "candidate_list_coherent", "status", "original_semantics", "issue",
)

SUMMARY_FIELDS = (
    "source_report", "input_rows", "primitive_rows", "complete_fetch_rows",
    "candidate_ordinal_rows", "stride_matching_candidate_rows",
    "metadata_matching_candidate_rows", "payload_prefix_matching_candidate_rows",
    "current_ordinal_rows", "current_ordinal_metadata_matching_rows",
    "current_ordinal_payload_prefix_matching_rows", "uniquely_discriminated_by_metadata_rows",
    "uniquely_discriminated_by_payload_prefix_rows", "shifted_stride_candidate_rows",
    "shifted_metadata_matching_rows", "shifted_payload_prefix_matching_rows",
    "input_status_valid_rows", "candidate_list_coherent_rows", "status", "provenance",
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


def _ordinals(row: Dict[str, str], field: str) -> List[int]:
    value = row.get(field, "")
    if not value:
        return []
    try:
        return [int(item, 0) for item in value.split(",") if item]
    except ValueError as exc:
        raise ValueError(f"invalid ordinal list in {field}: {value!r}") from exc


def _key(row: Dict[str, str]) -> Tuple[str, str, int, int]:
    archive = row.get("model_archive", "")
    object_name = row.get("model_object", "")
    if not archive or not object_name:
        raise ValueError("opaque discrimination row has an empty object identity")
    return archive, object_name, _int(row, "layer"), _int(row, "primitive")


def analyze_rows(rows: Sequence[Dict[str, str]]) -> Tuple[List[Dict[str, str]], Dict[str, str]]:
    if not rows:
        raise ValueError("opaque discrimination report has no rows")
    required = {
        "model_archive", "model_object", "layer", "primitive", "fetch_word_count",
        "candidate_ordinal_count", "stride_matching_candidate_count",
        "metadata_matching_candidate_count", "payload_prefix_matching_candidate_count",
        "stride_matching_ordinals", "metadata_matching_ordinals", "payload_prefix_matching_ordinals",
        "current_ordinal_present", "current_ordinal_metadata_match",
        "current_ordinal_payload_prefix_match", "current_ordinal_uniquely_discriminated_by_metadata",
        "current_ordinal_uniquely_discriminated_by_payload_prefix", "status", "original_semantics",
    }
    missing = required - set(rows[0])
    if missing:
        raise ValueError(f"opaque discrimination report is missing columns: {sorted(missing)}")
    if not all(row.get("original_semantics") == ORIGINAL_SEMANTICS for row in rows):
        raise ValueError("opaque discrimination input changed original_semantics")

    report: List[Dict[str, str]] = []
    complete_fetch_rows = 0
    candidate_rows = 0
    stride_rows = 0
    metadata_rows = 0
    payload_rows = 0
    current_rows = 0
    current_metadata_rows = 0
    current_payload_rows = 0
    metadata_unique_rows = 0
    payload_unique_rows = 0
    shifted_stride_rows = 0
    shifted_metadata_rows = 0
    shifted_payload_rows = 0
    input_status_valid_rows = 0
    coherent_rows = 0

    for row in sorted(rows, key=_key):
        key = _key(row)
        fetch_word_count = _int(row, "fetch_word_count")
        candidate_count = _int(row, "candidate_ordinal_count")
        stride_count = _int(row, "stride_matching_candidate_count")
        metadata_count = _int(row, "metadata_matching_candidate_count")
        payload_count = _int(row, "payload_prefix_matching_candidate_count")
        stride_ordinals = _ordinals(row, "stride_matching_ordinals")
        metadata_ordinals = _ordinals(row, "metadata_matching_ordinals")
        payload_ordinals = _ordinals(row, "payload_prefix_matching_ordinals")
        current_present = _bool(row, "current_ordinal_present")
        current_metadata = _bool(row, "current_ordinal_metadata_match")
        current_payload = _bool(row, "current_ordinal_payload_prefix_match")
        metadata_unique = _bool(row, "current_ordinal_uniquely_discriminated_by_metadata")
        payload_unique = _bool(row, "current_ordinal_uniquely_discriminated_by_payload_prefix")

        complete_fetch = fetch_word_count == 5
        next_ordinal = key[3] + 1
        next_stride = next_ordinal in stride_ordinals
        next_metadata = next_ordinal in metadata_ordinals
        next_payload = next_ordinal in payload_ordinals
        coherent = (
            candidate_count >= stride_count >= metadata_count
            and stride_count >= payload_count
            and len(stride_ordinals) == stride_count
            and len(metadata_ordinals) == metadata_count
            and len(payload_ordinals) == payload_count
            and set(metadata_ordinals).issubset(stride_ordinals)
            and set(payload_ordinals).issubset(stride_ordinals)
            and current_present
            and (current_metadata == (key[3] in metadata_ordinals))
            and (current_payload == (key[3] in payload_ordinals))
            and (metadata_unique == (current_metadata and metadata_count == 1))
            and (payload_unique == (current_payload and payload_count == 1))
        )
        if complete_fetch:
            complete_fetch_rows += 1
        candidate_rows += candidate_count
        stride_rows += stride_count
        metadata_rows += metadata_count
        payload_rows += payload_count
        current_rows += int(current_present)
        current_metadata_rows += int(current_metadata)
        current_payload_rows += int(current_payload)
        metadata_unique_rows += int(metadata_unique)
        payload_unique_rows += int(payload_unique)
        shifted_stride_rows += int(next_stride)
        shifted_metadata_rows += int(next_metadata)
        shifted_payload_rows += int(next_payload)
        input_status_valid_rows += int(row.get("status") in {INPUT_STATUS, "inconclusive-original-primitive-stream-opaque-discrimination"})
        coherent_rows += int(coherent)
        report.append(
            {
                "model_archive": key[0], "model_object": key[1], "layer": str(key[2]), "primitive": str(key[3]),
                "input_status_valid": str(row.get("status") == INPUT_STATUS),
                "fetch_word_count": str(fetch_word_count), "candidate_ordinal_count": str(candidate_count),
                "stride_matching_candidate_count": str(stride_count),
                "metadata_matching_candidate_count": str(metadata_count),
                "payload_prefix_matching_candidate_count": str(payload_count),
                "current_ordinal_present": str(current_present),
                "current_ordinal_metadata_match": str(current_metadata),
                "current_ordinal_payload_prefix_match": str(current_payload),
                "current_ordinal_uniquely_discriminated_by_metadata": str(metadata_unique),
                "current_ordinal_uniquely_discriminated_by_payload_prefix": str(payload_unique),
                "next_ordinal_stride_match": str(next_stride),
                "next_ordinal_metadata_match": str(next_metadata),
                "next_ordinal_payload_prefix_match": str(next_payload),
                "candidate_list_coherent": str(coherent),
                "status": STATUS if coherent else "inconclusive-independent-original-primitive-stream-opaque-discrimination-control",
                "original_semantics": ORIGINAL_SEMANTICS,
                "issue": "" if coherent else "candidate counts, ordinal lists or current flags are incoherent",
            }
        )

    summary = {
        "source_report": "",
        "input_rows": str(len(rows)),
        "primitive_rows": str(len(rows)),
        "complete_fetch_rows": str(complete_fetch_rows),
        "candidate_ordinal_rows": str(candidate_rows),
        "stride_matching_candidate_rows": str(stride_rows),
        "metadata_matching_candidate_rows": str(metadata_rows),
        "payload_prefix_matching_candidate_rows": str(payload_rows),
        "current_ordinal_rows": str(current_rows),
        "current_ordinal_metadata_matching_rows": str(current_metadata_rows),
        "current_ordinal_payload_prefix_matching_rows": str(current_payload_rows),
        "uniquely_discriminated_by_metadata_rows": str(metadata_unique_rows),
        "uniquely_discriminated_by_payload_prefix_rows": str(payload_unique_rows),
        "shifted_stride_candidate_rows": str(shifted_stride_rows),
        "shifted_metadata_matching_rows": str(shifted_metadata_rows),
        "shifted_payload_prefix_matching_rows": str(shifted_payload_rows),
        "input_status_valid_rows": str(input_status_valid_rows),
        "candidate_list_coherent_rows": str(coherent_rows),
        "status": STATUS if (
            len(rows) > 0 and complete_fetch_rows > 0 and coherent_rows == len(rows)
            and current_rows == len(rows) and metadata_rows == 0 and payload_rows == 0
            and shifted_stride_rows > 0 and shifted_metadata_rows == 0 and shifted_payload_rows == 0
        ) else "inconclusive-independent-original-primitive-stream-opaque-discrimination-control",
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
    print("FIFA Street 2 independent original primitive/__VB opaque discrimination control")
    for field in SUMMARY_FIELDS:
        print(f"{field}: {summary[field]}")
    print(f"report: {args.report}")
    print(f"summary: {args.summary}")
    return 0 if summary["status"] == STATUS else 1


if __name__ == "__main__":
    raise SystemExit(main())
