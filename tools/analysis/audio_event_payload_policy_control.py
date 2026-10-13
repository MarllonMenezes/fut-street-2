"""Independently compare bounded and all-candidate audio payload reports.

The reports are compared by exact event/bank candidate identity, decoder
status, bounded PCM metadata and exact-match counters. The control verifies
that the larger policy adds no hidden difference for this ISO; it does not
select an ABKC sample, bind an EVT name to gameplay, or claim playback
semantics.
"""

from __future__ import annotations

import argparse
import csv
from pathlib import Path
from typing import Dict, List, Sequence, Tuple


STATUS = "verified-independent-audio-event-payload-policy-control"
ORIGINAL_SEMANTICS = "not-claimed"
PROVENANCE = (
    "independent-TSV-keyed-reparse+bounded-policy-vs-all-policy-row/hash/"
    "decode-counter-comparison;no-event-sample-promotion"
)

REPORT_FIELDS = (
    "candidate_key",
    "bounded_status",
    "all_status",
    "bounded_decode_validated",
    "all_decode_validated",
    "bounded_wave_bytes",
    "all_wave_bytes",
    "bounded_pcm_sha256",
    "all_pcm_sha256",
    "bounded_exact_pcm_match_count",
    "all_exact_pcm_match_count",
    "identity_match",
    "status_match",
    "decode_match",
    "wave_bytes_match",
    "pcm_hash_match",
    "exact_match_count_match",
    "status",
    "original_semantics",
    "issue",
)

SUMMARY_FIELDS = (
    "bounded_source_report",
    "all_source_report",
    "bounded_rows",
    "all_rows",
    "joined_rows",
    "bounded_event_rows",
    "all_event_rows",
    "bounded_bank_rows",
    "all_bank_rows",
    "bounded_input_status_rows",
    "all_input_status_rows",
    "identity_match_rows",
    "status_match_rows",
    "decode_match_rows",
    "wave_bytes_match_rows",
    "pcm_hash_match_rows",
    "exact_match_count_match_rows",
    "exact_pcm_match_rows",
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
        writer = csv.DictWriter(handle, fieldnames=list(fields), delimiter="\t", lineterminator="\n")
        writer.writeheader()
        writer.writerows({field: row.get(field, "") for field in fields} for row in rows)


def _key(row: Dict[str, str]) -> Tuple[str, str, str, str, str, str]:
    return tuple(row.get(field, "") for field in (
        "kind", "source_path", "name", "row_or_sound_index", "data_offset", "data_length"
    ))


def _int(row: Dict[str, str], field: str) -> int:
    try:
        return int(row.get(field, ""), 0)
    except (TypeError, ValueError) as exc:
        raise ValueError(f"invalid integer in {field}: {row.get(field)!r}") from exc


def analyze(
    bounded_rows: Sequence[Dict[str, str]], all_rows: Sequence[Dict[str, str]]
) -> Tuple[List[Dict[str, str]], Dict[str, str]]:
    if not bounded_rows or not all_rows:
        raise ValueError("both audio payload reports must contain rows")
    bounded = {_key(row): row for row in bounded_rows}
    all_candidates = {_key(row): row for row in all_rows}
    keys = sorted(set(bounded) & set(all_candidates))

    report: List[Dict[str, str]] = []
    counters = {field: 0 for field in (
        "identity_match", "status_match", "decode_match", "wave_bytes_match",
        "pcm_hash_match", "exact_match_count_match"
    )}
    exact_pcm_match_rows = 0
    for key in keys:
        left = bounded[key]
        right = all_candidates[key]
        identity_match = key in all_candidates
        status_match = left.get("status") == right.get("status")
        decode_match = left.get("decode_validated") == right.get("decode_validated")
        wave_bytes_match = left.get("wave_bytes") == right.get("wave_bytes")
        pcm_hash_match = left.get("pcm_sha256") == right.get("pcm_sha256")
        exact_count_match = _int(left, "exact_pcm_match_count") == _int(right, "exact_pcm_match_count")
        exact_pcm_match_rows += int(_int(right, "exact_pcm_match_count") > 0)
        for name, value in (
            ("identity_match", identity_match), ("status_match", status_match),
            ("decode_match", decode_match), ("wave_bytes_match", wave_bytes_match),
            ("pcm_hash_match", pcm_hash_match), ("exact_match_count_match", exact_count_match)
        ):
            counters[name] += int(value)
        row_status = STATUS if (
            identity_match and status_match and decode_match and wave_bytes_match
            and pcm_hash_match and exact_count_match
        ) else "inconclusive-independent-audio-event-payload-policy-control"
        report.append({
            "candidate_key": "|".join(key),
            "bounded_status": left.get("status", ""),
            "all_status": right.get("status", ""),
            "bounded_decode_validated": left.get("decode_validated", ""),
            "all_decode_validated": right.get("decode_validated", ""),
            "bounded_wave_bytes": left.get("wave_bytes", ""),
            "all_wave_bytes": right.get("wave_bytes", ""),
            "bounded_pcm_sha256": left.get("pcm_sha256", ""),
            "all_pcm_sha256": right.get("pcm_sha256", ""),
            "bounded_exact_pcm_match_count": left.get("exact_pcm_match_count", ""),
            "all_exact_pcm_match_count": right.get("exact_pcm_match_count", ""),
            "identity_match": str(identity_match),
            "status_match": str(status_match),
            "decode_match": str(decode_match),
            "wave_bytes_match": str(wave_bytes_match),
            "pcm_hash_match": str(pcm_hash_match),
            "exact_match_count_match": str(exact_count_match),
            "status": row_status,
            "original_semantics": ORIGINAL_SEMANTICS,
            "issue": "" if row_status == STATUS else "bounded and all candidate rows differ",
        })

    bounded_event_rows = sum(row.get("kind") == "event" for row in bounded_rows)
    all_event_rows = sum(row.get("kind") == "event" for row in all_rows)
    bounded_bank_rows = sum(row.get("kind") == "bank" for row in bounded_rows)
    all_bank_rows = sum(row.get("kind") == "bank" for row in all_rows)
    summary = {
        "bounded_source_report": "",
        "all_source_report": "",
        "bounded_rows": str(len(bounded_rows)),
        "all_rows": str(len(all_rows)),
        "joined_rows": str(len(keys)),
        "bounded_event_rows": str(bounded_event_rows),
        "all_event_rows": str(all_event_rows),
        "bounded_bank_rows": str(bounded_bank_rows),
        "all_bank_rows": str(all_bank_rows),
        "bounded_input_status_rows": str(sum(bool(row.get("status")) for row in bounded_rows)),
        "all_input_status_rows": str(sum(bool(row.get("status")) for row in all_rows)),
        "identity_match_rows": str(counters["identity_match"]),
        "status_match_rows": str(counters["status_match"]),
        "decode_match_rows": str(counters["decode_match"]),
        "wave_bytes_match_rows": str(counters["wave_bytes_match"]),
        "pcm_hash_match_rows": str(counters["pcm_hash_match"]),
        "exact_match_count_match_rows": str(counters["exact_match_count_match"]),
        "exact_pcm_match_rows": str(exact_pcm_match_rows),
        "status": STATUS if (
            len(bounded_rows) == len(all_rows) == len(keys)
            and bounded_event_rows == all_event_rows == 17
            and bounded_bank_rows == all_bank_rows == 242
            and counters["identity_match"] == len(keys)
            and counters["status_match"] == len(keys)
            and counters["decode_match"] == len(keys)
            and counters["wave_bytes_match"] == len(keys)
            and counters["pcm_hash_match"] == len(keys)
            and counters["exact_match_count_match"] == len(keys)
            and exact_pcm_match_rows == 0
            and all(row["status"] == STATUS for row in report)
        ) else "inconclusive-independent-audio-event-payload-policy-control",
        "provenance": PROVENANCE,
        "original_semantics": ORIGINAL_SEMANTICS,
    }
    return report, summary


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("bounded_report", type=Path)
    parser.add_argument("all_report", type=Path)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()
    report, summary = analyze(_read(args.bounded_report), _read(args.all_report))
    summary["bounded_source_report"] = str(args.bounded_report)
    summary["all_source_report"] = str(args.all_report)
    _write(args.report, REPORT_FIELDS, report)
    _write(args.summary, SUMMARY_FIELDS, [summary])
    print("FIFA Street 2 independent audio payload policy control")
    for field in SUMMARY_FIELDS:
        print(f"{field}: {summary[field]}")
    print(f"report: {args.report}")
    print(f"summary: {args.summary}")
    return 0 if summary["status"] == STATUS else 1


if __name__ == "__main__":
    raise SystemExit(main())
