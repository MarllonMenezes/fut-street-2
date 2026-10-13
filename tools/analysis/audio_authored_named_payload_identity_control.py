"""Reconcile authored gameplay audio traces with named EVT PCM identities.

The control validates the managed clean-room route at the payload boundary:
each emitted gameplay route carries the actual WAVE/PCM identity produced by
the runtime, and that identity is compared with the independently generated
named EVT runtime report.  It does not infer an original gameplay event,
ABKC/sample selector, or original playback semantics.
"""

from __future__ import annotations

import argparse
import csv
from pathlib import Path
from typing import Dict, Iterable, List, Mapping, Sequence, Tuple


STATUS = "verified-independent-authored-audio-named-payload-identity-control"
DIRECT_SELECTION_BINDING = "not-proven"
ORIGINAL_SEMANTICS = "not-claimed"
PROVENANCE = (
    "independent-trace-payload-identity-reparse joined to named-event-runtime;"
    "WAVE/PCM-byte-and-SHA256-reconciliation;authored-candidate-only;"
    "no-original-event-or-sample-promotion"
)

REPORT_FIELDS = (
    "scenario",
    "sequence",
    "action",
    "candidate",
    "route",
    "trace_wave_bytes",
    "trace_pcm_bytes",
    "trace_pcm_sha256",
    "runtime_event_present",
    "runtime_prepared",
    "runtime_wave_header_valid",
    "runtime_wave_bytes",
    "runtime_pcm_bytes",
    "runtime_pcm_sha256",
    "wave_bytes_match",
    "pcm_bytes_match",
    "pcm_sha256_match",
    "status",
    "provenance",
    "direct_selection_binding",
    "original_semantics",
    "issue",
)

SUMMARY_FIELDS = (
    "coverage_source_report",
    "runtime_source_report",
    "scenario_rows",
    "verified_scenario_rows",
    "trace_rows",
    "trace_played_rows",
    "identity_rows",
    "runtime_rows",
    "runtime_prepared_route_rows",
    "runtime_wave_header_valid_route_rows",
    "wave_bytes_match_rows",
    "pcm_bytes_match_rows",
    "pcm_sha256_match_rows",
    "invalid_trace_identity_rows",
    "missing_runtime_candidates",
    "status",
    "provenance",
    "direct_selection_binding",
    "original_semantics",
)


def _read(path: Path) -> List[Dict[str, str]]:
    with path.open("r", encoding="utf-8-sig", newline="") as handle:
        return list(csv.DictReader(handle, delimiter="\t"))


def _write(path: Path, fields: Sequence[str], rows: Iterable[Mapping[str, object]]) -> None:
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


def _true(row: Mapping[str, str], field: str) -> bool:
    return row.get(field, "").strip().lower() == "true"


def _int_value(value: str) -> int | None:
    try:
        return int(value.strip())
    except (AttributeError, TypeError, ValueError):
        return None


def analyze(
    coverage_rows: Sequence[Dict[str, str]],
    runtime_rows: Sequence[Dict[str, str]],
    trace_rows_by_path: Mapping[str, Sequence[Dict[str, str]]],
) -> Tuple[List[Dict[str, str]], Dict[str, str]]:
    scenarios = [row for row in coverage_rows if row.get("kind") == "scenario"]
    runtime = {
        row.get("event_name", ""): row
        for row in runtime_rows
        if row.get("event_name")
    }
    report: List[Dict[str, str]] = []
    trace_rows = 0
    trace_played_rows = 0
    identity_rows = 0
    invalid_trace_identity_rows = 0
    missing_runtime_candidates = 0
    wave_bytes_match_rows = 0
    pcm_bytes_match_rows = 0
    pcm_sha256_match_rows = 0
    runtime_prepared_candidates = set()
    runtime_wave_valid_candidates = set()
    verified_scenario_rows = 0

    for coverage in scenarios:
        if _true(coverage, "trace_valid") and _true(coverage, "replay_matched"):
            verified_scenario_rows += 1
        scenario = coverage.get("name", "")
        for trace in trace_rows_by_path.get(coverage.get("trace_path", ""), ()):
            if not trace.get("action") and not trace.get("candidate"):
                continue
            trace_rows += 1
            candidate = trace.get("candidate", "")
            route = trace.get("route", "")
            played = route == "played-clean-room-candidate"
            trace_played_rows += int(played)
            if not candidate:
                invalid_trace_identity_rows += 1
                report.append(
                    {
                        "scenario": scenario,
                        "sequence": trace.get("sequence", ""),
                        "action": trace.get("action", ""),
                        "candidate": candidate,
                        "route": route,
                        "status": "inconclusive-authored-audio-named-payload-identity-control",
                        "provenance": PROVENANCE,
                        "direct_selection_binding": DIRECT_SELECTION_BINDING,
                        "original_semantics": ORIGINAL_SEMANTICS,
                        "issue": "trace row has no candidate payload identity",
                    }
                )
                continue

            runtime_row = runtime.get(candidate)
            runtime_present = runtime_row is not None
            if not runtime_present:
                missing_runtime_candidates += 1
            elif _true(runtime_row, "runtime_prepared"):
                runtime_prepared_candidates.add(candidate)
            if runtime_present and _true(runtime_row, "wave_header_valid"):
                runtime_wave_valid_candidates.add(candidate)

            trace_wave_bytes = _int_value(trace.get("wave_bytes", ""))
            trace_pcm_bytes = _int_value(trace.get("pcm_bytes", ""))
            trace_pcm_sha256 = trace.get("pcm_sha256", "").strip().upper()
            runtime_wave_bytes = _int_value(runtime_row.get("wave_bytes", "")) if runtime_row else None
            runtime_pcm_bytes = _int_value(runtime_row.get("pcm_bytes", "")) if runtime_row else None
            runtime_pcm_sha256 = runtime_row.get("pcm_sha256", "").strip().upper() if runtime_row else ""
            valid_trace_identity = (
                trace_wave_bytes is not None
                and trace_pcm_bytes is not None
                and trace_wave_bytes >= 44
                and trace_pcm_bytes >= 0
                and len(trace_pcm_sha256) == 64
            )
            if not valid_trace_identity:
                invalid_trace_identity_rows += 1
            identity_rows += 1

            wave_match = valid_trace_identity and runtime_wave_bytes == trace_wave_bytes
            pcm_match = valid_trace_identity and runtime_pcm_bytes == trace_pcm_bytes
            sha_match = valid_trace_identity and runtime_pcm_sha256 == trace_pcm_sha256
            wave_bytes_match_rows += int(wave_match)
            pcm_bytes_match_rows += int(pcm_match)
            pcm_sha256_match_rows += int(sha_match)
            valid = (
                played
                and valid_trace_identity
                and runtime_present
                and _true(runtime_row, "runtime_prepared")
                and _true(runtime_row, "wave_header_valid")
                and runtime_row.get("status") == "verified-runtime-named-payload"
                and wave_match
                and pcm_match
                and sha_match
                and runtime_row.get("original_semantics") == ORIGINAL_SEMANTICS
            )
            report.append(
                {
                    "scenario": scenario,
                    "sequence": trace.get("sequence", ""),
                    "action": trace.get("action", ""),
                    "candidate": candidate,
                    "route": route,
                    "trace_wave_bytes": trace.get("wave_bytes", ""),
                    "trace_pcm_bytes": trace.get("pcm_bytes", ""),
                    "trace_pcm_sha256": trace.get("pcm_sha256", ""),
                    "runtime_event_present": str(runtime_present),
                    "runtime_prepared": str(runtime_present and _true(runtime_row, "runtime_prepared")),
                    "runtime_wave_header_valid": str(runtime_present and _true(runtime_row, "wave_header_valid")),
                    "runtime_wave_bytes": "" if runtime_wave_bytes is None else str(runtime_wave_bytes),
                    "runtime_pcm_bytes": "" if runtime_pcm_bytes is None else str(runtime_pcm_bytes),
                    "runtime_pcm_sha256": runtime_pcm_sha256,
                    "wave_bytes_match": str(wave_match),
                    "pcm_bytes_match": str(pcm_match),
                    "pcm_sha256_match": str(sha_match),
                    "status": STATUS if valid else "inconclusive-authored-audio-named-payload-identity-control",
                    "provenance": PROVENANCE,
                    "direct_selection_binding": DIRECT_SELECTION_BINDING,
                    "original_semantics": ORIGINAL_SEMANTICS,
                    "issue": "" if valid else "trace payload identity did not reconcile with named runtime candidate",
                }
            )

    all_valid = (
        len(scenarios) == 10
        and verified_scenario_rows == len(scenarios)
        and trace_rows == trace_played_rows == 54
        and identity_rows == 54
        and invalid_trace_identity_rows == 0
        and len(runtime) == 17
        and len(runtime_prepared_candidates) == len(runtime_wave_valid_candidates) == 8
        and missing_runtime_candidates == 0
        and wave_bytes_match_rows == pcm_bytes_match_rows == pcm_sha256_match_rows == identity_rows
        and len(report) == identity_rows
        and all(row["status"] == STATUS for row in report)
    )
    summary = {
        "coverage_source_report": "",
        "runtime_source_report": "",
        "scenario_rows": str(len(scenarios)),
        "verified_scenario_rows": str(verified_scenario_rows),
        "trace_rows": str(trace_rows),
        "trace_played_rows": str(trace_played_rows),
        "identity_rows": str(identity_rows),
        "runtime_rows": str(len(runtime)),
        "runtime_prepared_route_rows": str(len(runtime_prepared_candidates)),
        "runtime_wave_header_valid_route_rows": str(len(runtime_wave_valid_candidates)),
        "wave_bytes_match_rows": str(wave_bytes_match_rows),
        "pcm_bytes_match_rows": str(pcm_bytes_match_rows),
        "pcm_sha256_match_rows": str(pcm_sha256_match_rows),
        "invalid_trace_identity_rows": str(invalid_trace_identity_rows),
        "missing_runtime_candidates": str(missing_runtime_candidates),
        "status": STATUS if all_valid else "inconclusive-authored-audio-named-payload-identity-control",
        "provenance": PROVENANCE,
        "direct_selection_binding": DIRECT_SELECTION_BINDING,
        "original_semantics": ORIGINAL_SEMANTICS,
    }
    return report, summary


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("coverage_report", type=Path)
    parser.add_argument("runtime_report", type=Path)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()

    coverage_rows = _read(args.coverage_report)
    trace_rows_by_path = {
        row["trace_path"]: _read(Path(row["trace_path"]))
        for row in coverage_rows
        if row.get("kind") == "scenario" and row.get("trace_path")
    }
    report, summary = analyze(coverage_rows, _read(args.runtime_report), trace_rows_by_path)
    summary["coverage_source_report"] = str(args.coverage_report)
    summary["runtime_source_report"] = str(args.runtime_report)
    _write(args.report, REPORT_FIELDS, report)
    _write(args.summary, SUMMARY_FIELDS, [summary])
    print("FIFA Street 2 independent authored named-payload identity control")
    for field in SUMMARY_FIELDS:
        print(f"{field}: {summary[field]}")
    print(f"report: {args.report}")
    print(f"summary: {args.summary}")
    return 0 if summary["status"] == STATUS else 1


if __name__ == "__main__":
    raise SystemExit(main())
