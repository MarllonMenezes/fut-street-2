"""Independently join named EVT parallel ranges to managed runtime payload rows.

The control verifies that the exact event-name/header/data-range identity from
the parallel PCPAHDR/PCPADAT audit is preserved by the managed named-payload
decoder. It does not identify an ABKC sample, bind an EVT name to gameplay, or
claim original playback semantics.
"""

from __future__ import annotations

import argparse
import csv
from pathlib import Path
from typing import Dict, List, Sequence, Tuple


STATUS = "verified-independent-audio-event-parallel-runtime-join-control"
ORIGINAL_SEMANTICS = "not-claimed"
PROVENANCE = (
    "independent-TSV-keyed-event-name+header-row+data-offset+data-length-join;"
    "runtime-metadata/decode-control;no-event-sample-promotion"
)

REPORT_FIELDS = (
    "event_name",
    "parallel_header_row",
    "runtime_header_row",
    "parallel_data_offset",
    "runtime_data_offset",
    "parallel_data_length",
    "runtime_data_length",
    "header_row_match",
    "data_offset_match",
    "data_length_match",
    "candidate_metadata_validated",
    "runtime_prepared",
    "wave_header_valid",
    "parallel_status",
    "runtime_status",
    "status",
    "provenance",
    "original_semantics",
    "issue",
)

SUMMARY_FIELDS = (
    "parallel_source_report",
    "runtime_source_report",
    "parallel_rows",
    "runtime_rows",
    "joined_rows",
    "missing_runtime_names",
    "missing_parallel_names",
    "parallel_unique_event_names",
    "runtime_unique_event_names",
    "header_row_match_rows",
    "data_offset_match_rows",
    "data_length_match_rows",
    "candidate_metadata_validated_rows",
    "runtime_prepared_rows",
    "wave_header_valid_rows",
    "parallel_input_status_rows",
    "runtime_input_status_rows",
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


def _key(row: Dict[str, str]) -> str:
    return row.get("event_name", "")


def _bool(row: Dict[str, str], field: str) -> bool:
    return row.get(field, "").strip().lower() == "true"


def analyze(
    parallel_rows: Sequence[Dict[str, str]], runtime_rows: Sequence[Dict[str, str]]
) -> Tuple[List[Dict[str, str]], Dict[str, str]]:
    if not parallel_rows or not runtime_rows:
        raise ValueError("both parallel-range and runtime reports must contain rows")

    parallel = {_key(row): row for row in parallel_rows if _key(row)}
    runtime = {_key(row): row for row in runtime_rows if _key(row)}
    names = sorted(set(parallel) & set(runtime))
    report: List[Dict[str, str]] = []
    counters = {field: 0 for field in (
        "header_row_match", "data_offset_match", "data_length_match",
        "candidate_metadata_validated", "runtime_prepared", "wave_header_valid",
    )}
    for name in names:
        left = parallel[name]
        right = runtime[name]
        header_row_match = left.get("header_row") == right.get("header_row")
        data_offset_match = left.get("data_offset") == right.get("data_offset")
        data_length_match = left.get("data_length") == right.get("data_length")
        candidate_metadata_validated = _bool(right, "candidate_metadata_validated")
        runtime_prepared = _bool(right, "runtime_prepared")
        wave_header_valid = _bool(right, "wave_header_valid")
        values = {
            "header_row_match": header_row_match,
            "data_offset_match": data_offset_match,
            "data_length_match": data_length_match,
            "candidate_metadata_validated": candidate_metadata_validated,
            "runtime_prepared": runtime_prepared,
            "wave_header_valid": wave_header_valid,
        }
        for field, value in values.items():
            counters[field] += int(value)
        row_status = STATUS if (
            header_row_match and data_offset_match and data_length_match
            and candidate_metadata_validated and runtime_prepared and wave_header_valid
            and left.get("status") == "verified-structural-named-parallel-range"
            and right.get("status") == "verified-runtime-named-payload"
            and left.get("original_semantics") == ORIGINAL_SEMANTICS
            and right.get("original_semantics") == ORIGINAL_SEMANTICS
        ) else "inconclusive-independent-audio-event-parallel-runtime-join-control"
        report.append({
            "event_name": name,
            "parallel_header_row": left.get("header_row", ""),
            "runtime_header_row": right.get("header_row", ""),
            "parallel_data_offset": left.get("data_offset", ""),
            "runtime_data_offset": right.get("data_offset", ""),
            "parallel_data_length": left.get("data_length", ""),
            "runtime_data_length": right.get("data_length", ""),
            "header_row_match": str(header_row_match),
            "data_offset_match": str(data_offset_match),
            "data_length_match": str(data_length_match),
            "candidate_metadata_validated": str(candidate_metadata_validated),
            "runtime_prepared": str(runtime_prepared),
            "wave_header_valid": str(wave_header_valid),
            "parallel_status": left.get("status", ""),
            "runtime_status": right.get("status", ""),
            "status": row_status,
            "provenance": PROVENANCE,
            "original_semantics": ORIGINAL_SEMANTICS,
            "issue": "" if row_status == STATUS else "parallel range and runtime payload controls differ",
        })

    summary = {
        "parallel_source_report": "",
        "runtime_source_report": "",
        "parallel_rows": str(len(parallel_rows)),
        "runtime_rows": str(len(runtime_rows)),
        "joined_rows": str(len(names)),
        "missing_runtime_names": str(len(set(parallel) - set(runtime))),
        "missing_parallel_names": str(len(set(runtime) - set(parallel))),
        "parallel_unique_event_names": str(len(parallel)),
        "runtime_unique_event_names": str(len(runtime)),
        "header_row_match_rows": str(counters["header_row_match"]),
        "data_offset_match_rows": str(counters["data_offset_match"]),
        "data_length_match_rows": str(counters["data_length_match"]),
        "candidate_metadata_validated_rows": str(counters["candidate_metadata_validated"]),
        "runtime_prepared_rows": str(counters["runtime_prepared"]),
        "wave_header_valid_rows": str(counters["wave_header_valid"]),
        "parallel_input_status_rows": str(sum(row.get("status") == "verified-structural-named-parallel-range" for row in parallel_rows)),
        "runtime_input_status_rows": str(sum(row.get("status") == "verified-runtime-named-payload" for row in runtime_rows)),
        "status": STATUS if (
            len(parallel_rows) == len(runtime_rows) == len(names) == 17
            and len(parallel) == len(runtime) == 17
            and not (set(parallel) - set(runtime))
            and not (set(runtime) - set(parallel))
            and all(counters[field] == 17 for field in counters)
            and all(row["status"] == STATUS for row in report)
            and all(row.get("original_semantics") == ORIGINAL_SEMANTICS for row in parallel_rows)
            and all(row.get("original_semantics") == ORIGINAL_SEMANTICS for row in runtime_rows)
        ) else "inconclusive-independent-audio-event-parallel-runtime-join-control",
        "provenance": PROVENANCE,
        "original_semantics": ORIGINAL_SEMANTICS,
    }
    return report, summary


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("parallel_report", type=Path)
    parser.add_argument("runtime_report", type=Path)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()
    report, summary = analyze(_read(args.parallel_report), _read(args.runtime_report))
    summary["parallel_source_report"] = str(args.parallel_report)
    summary["runtime_source_report"] = str(args.runtime_report)
    _write(args.report, REPORT_FIELDS, report)
    _write(args.summary, SUMMARY_FIELDS, [summary])
    print("FIFA Street 2 independent audio parallel/runtime join control")
    for field in SUMMARY_FIELDS:
        print(f"{field}: {summary[field]}")
    print(f"report: {args.report}")
    print(f"summary: {args.summary}")
    return 0 if summary["status"] == STATUS else 1


if __name__ == "__main__":
    raise SystemExit(main())
