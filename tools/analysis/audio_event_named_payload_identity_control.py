"""Independently reconcile named EVT PCM with the exhaustive ABK candidate set.

The control joins the managed named-event runtime report, the independent named
decoder report and the exhaustive event/ABK PCM-correlation report. It verifies
that the same 17 named EVT payloads retain their decoded WAVE/PCM identity and
recomputes the event-to-ABK PCM hash cardinality. It does not select an ABKC
sample, bind an event to gameplay, or claim original playback semantics.
"""

from __future__ import annotations

import argparse
import csv
from pathlib import Path
from typing import Dict, List, Sequence, Tuple


STATUS = "verified-independent-audio-event-named-payload-identity-control"
DIRECT_SELECTION_BINDING = "not-proven"
ORIGINAL_SEMANTICS = "not-claimed"
PROVENANCE = (
    "independent-TSV-keyed-runtime/decode/correlation-reparse;"
    "named-EVT-WAVE/PCM-identity+exhaustive-ABK-hash-cardinality;"
    "no-event-sample-promotion"
)

REPORT_FIELDS = (
    "event_path",
    "event_name",
    "header_row",
    "runtime_wave_bytes",
    "correlation_wave_bytes",
    "runtime_pcm_bytes",
    "runtime_pcm_sha256",
    "correlation_pcm_sha256",
    "runtime_metadata_validated",
    "runtime_prepared",
    "runtime_wave_header_valid",
    "named_decode_validated",
    "correlation_decode_validated",
    "wave_bytes_match",
    "pcm_bytes_match",
    "pcm_sha256_match",
    "bank_pcm_hash_match_count",
    "status",
    "provenance",
    "direct_selection_binding",
    "original_semantics",
    "issue",
)

SUMMARY_FIELDS = (
    "runtime_source_report",
    "named_decode_source_report",
    "correlation_source_report",
    "runtime_rows",
    "named_decode_rows",
    "correlation_event_rows",
    "correlation_bank_rows",
    "correlation_bank_decoded_rows",
    "joined_event_rows",
    "missing_runtime_events",
    "missing_named_decode_events",
    "missing_correlation_events",
    "runtime_metadata_validated_rows",
    "runtime_prepared_rows",
    "runtime_wave_header_valid_rows",
    "named_decode_validated_rows",
    "correlation_decode_validated_rows",
    "wave_bytes_match_rows",
    "pcm_bytes_match_rows",
    "pcm_sha256_match_rows",
    "event_hashes_with_bank_match",
    "exact_pcm_hash_pairs",
    "unique_event_names",
    "status",
    "provenance",
    "direct_selection_binding",
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


def _bool(row: Dict[str, str], field: str) -> bool:
    return row.get(field, "").strip().lower() == "true"


def _key_runtime(row: Dict[str, str]) -> Tuple[str, str, str]:
    return (row.get("event_path", ""), row.get("event_name", ""), row.get("header_row", ""))


def _key_decode(row: Dict[str, str]) -> Tuple[str, str, str]:
    return (row.get("event_path", ""), row.get("event_name", ""), row.get("header_row", ""))


def _key_correlation(row: Dict[str, str]) -> Tuple[str, str, str]:
    return (
        row.get("source_path", ""),
        row.get("name", ""),
        row.get("row_or_sound_index", ""),
    )


def analyze(
    runtime_rows: Sequence[Dict[str, str]],
    named_decode_rows: Sequence[Dict[str, str]],
    correlation_rows: Sequence[Dict[str, str]],
) -> Tuple[List[Dict[str, str]], Dict[str, str]]:
    if not runtime_rows or not named_decode_rows or not correlation_rows:
        raise ValueError("all audio identity reports must contain rows")

    runtime = {_key_runtime(row): row for row in runtime_rows if all(_key_runtime(row))}
    named_decode = {
        _key_decode(row): row for row in named_decode_rows if all(_key_decode(row))
    }
    correlation_events = {
        _key_correlation(row): row
        for row in correlation_rows
        if row.get("kind") == "event" and all(_key_correlation(row))
    }
    correlation_banks = [row for row in correlation_rows if row.get("kind") == "bank"]
    decoded_banks = [
        row
        for row in correlation_banks
        if _bool(row, "decode_validated") and row.get("pcm_sha256", "").strip()
    ]
    bank_hashes = {
        row.get("pcm_sha256", "").strip().upper()
        for row in decoded_banks
        if row.get("pcm_sha256", "").strip()
    }

    runtime_keys = set(runtime)
    decode_keys = set(named_decode)
    correlation_keys = set(correlation_events)
    keys = sorted(runtime_keys & decode_keys & correlation_keys)
    counters = {
        field: 0
        for field in (
            "runtime_metadata_validated",
            "runtime_prepared",
            "runtime_wave_header_valid",
            "named_decode_validated",
            "correlation_decode_validated",
            "wave_bytes_match",
            "pcm_bytes_match",
            "pcm_sha256_match",
        )
    }
    report: List[Dict[str, str]] = []
    event_hashes_with_bank_match = 0

    for key in keys:
        runtime_row = runtime[key]
        decode_row = named_decode[key]
        correlation_row = correlation_events[key]
        runtime_wave_bytes = runtime_row.get("wave_bytes", "")
        correlation_wave_bytes = correlation_row.get("wave_bytes", "")
        runtime_pcm_bytes = runtime_row.get("pcm_bytes", "")
        runtime_pcm_sha256 = runtime_row.get("pcm_sha256", "").strip().upper()
        correlation_pcm_sha256 = correlation_row.get("pcm_sha256", "").strip().upper()
        wave_bytes_match = runtime_wave_bytes == correlation_wave_bytes
        pcm_bytes_match = runtime_pcm_bytes.isdigit() and correlation_wave_bytes.isdigit() and (
            int(runtime_pcm_bytes) + 44 == int(correlation_wave_bytes)
        )
        pcm_sha256_match = bool(runtime_pcm_sha256) and runtime_pcm_sha256 == correlation_pcm_sha256
        bank_match_count = sum(1 for value in bank_hashes if value == runtime_pcm_sha256)
        if bank_match_count:
            event_hashes_with_bank_match += 1

        values = {
            "runtime_metadata_validated": _bool(runtime_row, "candidate_metadata_validated"),
            "runtime_prepared": _bool(runtime_row, "runtime_prepared"),
            "runtime_wave_header_valid": _bool(runtime_row, "wave_header_valid"),
            "named_decode_validated": _bool(decode_row, "decode_validated"),
            "correlation_decode_validated": _bool(correlation_row, "decode_validated"),
            "wave_bytes_match": wave_bytes_match,
            "pcm_bytes_match": pcm_bytes_match,
            "pcm_sha256_match": pcm_sha256_match,
        }
        for field, value in values.items():
            counters[field] += int(value)

        row_ok = (
            all(values.values())
            and bank_match_count == 0
            and runtime_row.get("status") == "verified-runtime-named-payload"
            and decode_row.get("decode_validated") == "True"
            and correlation_row.get("status") == "decoded-payload-no-match"
            and runtime_row.get("original_semantics") == ORIGINAL_SEMANTICS
            and correlation_row.get("issue", "") == ""
        )
        report.append(
            {
                "event_path": key[0],
                "event_name": key[1],
                "header_row": key[2],
                "runtime_wave_bytes": runtime_wave_bytes,
                "correlation_wave_bytes": correlation_wave_bytes,
                "runtime_pcm_bytes": runtime_pcm_bytes,
                "runtime_pcm_sha256": runtime_pcm_sha256,
                "correlation_pcm_sha256": correlation_pcm_sha256,
                "runtime_metadata_validated": str(values["runtime_metadata_validated"]),
                "runtime_prepared": str(values["runtime_prepared"]),
                "runtime_wave_header_valid": str(values["runtime_wave_header_valid"]),
                "named_decode_validated": str(values["named_decode_validated"]),
                "correlation_decode_validated": str(values["correlation_decode_validated"]),
                "wave_bytes_match": str(wave_bytes_match),
                "pcm_bytes_match": str(pcm_bytes_match),
                "pcm_sha256_match": str(pcm_sha256_match),
                "bank_pcm_hash_match_count": str(bank_match_count),
                "status": STATUS if row_ok else "inconclusive-independent-audio-event-named-payload-identity-control",
                "provenance": PROVENANCE,
                "direct_selection_binding": DIRECT_SELECTION_BINDING,
                "original_semantics": ORIGINAL_SEMANTICS,
                "issue": "" if row_ok else "named payload identity or ABK negative control differs",
            }
        )

    expected_event_count = 17
    summary = {
        "runtime_source_report": "",
        "named_decode_source_report": "",
        "correlation_source_report": "",
        "runtime_rows": str(len(runtime_rows)),
        "named_decode_rows": str(len(named_decode_rows)),
        "correlation_event_rows": str(len(correlation_events)),
        "correlation_bank_rows": str(len(correlation_banks)),
        "correlation_bank_decoded_rows": str(len(decoded_banks)),
        "joined_event_rows": str(len(keys)),
        "missing_runtime_events": str(len(decode_keys & correlation_keys - runtime_keys)),
        "missing_named_decode_events": str(len(runtime_keys & correlation_keys - decode_keys)),
        "missing_correlation_events": str(len(runtime_keys & decode_keys - correlation_keys)),
        "runtime_metadata_validated_rows": str(counters["runtime_metadata_validated"]),
        "runtime_prepared_rows": str(counters["runtime_prepared"]),
        "runtime_wave_header_valid_rows": str(counters["runtime_wave_header_valid"]),
        "named_decode_validated_rows": str(counters["named_decode_validated"]),
        "correlation_decode_validated_rows": str(counters["correlation_decode_validated"]),
        "wave_bytes_match_rows": str(counters["wave_bytes_match"]),
        "pcm_bytes_match_rows": str(counters["pcm_bytes_match"]),
        "pcm_sha256_match_rows": str(counters["pcm_sha256_match"]),
        "event_hashes_with_bank_match": str(event_hashes_with_bank_match),
        "exact_pcm_hash_pairs": str(event_hashes_with_bank_match),
        "unique_event_names": str(len({key[1] for key in keys})),
        "status": STATUS
        if (
            len(runtime_rows) == len(named_decode_rows) == expected_event_count
            and len(correlation_events) == expected_event_count
            and len(correlation_banks) == 242
            and len(decoded_banks) == 241
            and len(keys) == expected_event_count
            and all(value == expected_event_count for value in counters.values())
            and event_hashes_with_bank_match == 0
            and all(row["status"] == STATUS for row in report)
        )
        else "inconclusive-independent-audio-event-named-payload-identity-control",
        "provenance": PROVENANCE,
        "direct_selection_binding": DIRECT_SELECTION_BINDING,
        "original_semantics": ORIGINAL_SEMANTICS,
    }
    return report, summary


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("runtime_report", type=Path)
    parser.add_argument("named_decode_report", type=Path)
    parser.add_argument("correlation_report", type=Path)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()
    report, summary = analyze(
        _read(args.runtime_report),
        _read(args.named_decode_report),
        _read(args.correlation_report),
    )
    summary["runtime_source_report"] = str(args.runtime_report)
    summary["named_decode_source_report"] = str(args.named_decode_report)
    summary["correlation_source_report"] = str(args.correlation_report)
    _write(args.report, REPORT_FIELDS, report)
    _write(args.summary, SUMMARY_FIELDS, [summary])
    print("FIFA Street 2 independent named EVT payload identity control")
    for field in SUMMARY_FIELDS:
        print(f"{field}: {summary[field]}")
    print(f"report: {args.report}")
    print(f"summary: {args.summary}")
    return 0 if summary["status"] == STATUS else 1


if __name__ == "__main__":
    raise SystemExit(main())
