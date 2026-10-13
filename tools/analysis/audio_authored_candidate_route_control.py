"""Independently join authored audio traces to named candidate EVT payloads.

The control reconstructs only the clean-room action->candidate routes that were
actually emitted and replayed by authored traces.  It does not infer an
original event table, ABKC sample, gameplay rule, or playback binding.
"""

from __future__ import annotations

import argparse
import csv
from pathlib import Path
from typing import Dict, Iterable, List, Mapping, Sequence, Tuple


STATUS = "verified-independent-authored-audio-candidate-route-control"
ORIGINAL_SEMANTICS = "not-claimed"
PROVENANCE = (
    "independent-trace-action+candidate+route-reparse joined to named-event-runtime;"
    "authored-route-only;no-original-event-or-sample-promotion"
)

REPORT_FIELDS = (
    "action",
    "candidate",
    "trace_occurrences",
    "trace_played_occurrences",
    "trace_route_values",
    "runtime_event_present",
    "runtime_prepared",
    "runtime_wave_header_valid",
    "coverage_scenarios",
    "status",
    "provenance",
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
    "route_rows",
    "unique_action_rows",
    "unique_candidate_rows",
    "runtime_rows",
    "runtime_prepared_route_rows",
    "runtime_wave_header_valid_route_rows",
    "missing_runtime_candidates",
    "coverage_missing_policy_actions",
    "status",
    "provenance",
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


def _true(row: Mapping[str, str], field: str) -> bool:
    return row.get(field, "").strip().lower() == "true"


def _split(value: str) -> List[str]:
    return [part for part in value.split(",") if part]


def analyze(
    coverage_rows: Sequence[Dict[str, str]],
    runtime_rows: Sequence[Dict[str, str]],
    trace_rows_by_path: Mapping[str, Sequence[Dict[str, str]]],
) -> Tuple[List[Dict[str, str]], Dict[str, str]]:
    scenarios = [row for row in coverage_rows if row.get("kind") == "scenario"]
    runtime = {row.get("event_name", ""): row for row in runtime_rows if row.get("event_name")}
    route_data: Dict[Tuple[str, str], Dict[str, object]] = {}
    trace_rows = 0
    trace_played_rows = 0
    verified_scenario_rows = 0
    for coverage in scenarios:
        if _true(coverage, "trace_valid") and _true(coverage, "replay_matched"):
            verified_scenario_rows += 1
        path = coverage.get("trace_path", "")
        for trace in trace_rows_by_path.get(path, ()):
            action = trace.get("action", "")
            candidate = trace.get("candidate", "")
            if not action or not candidate:
                continue
            trace_rows += 1
            played = trace.get("route") == "played-clean-room-candidate"
            trace_played_rows += int(played)
            key = (action, candidate)
            entry = route_data.setdefault(key, {"occurrences": 0, "played": 0, "scenarios": set(), "routes": set()})
            entry["occurrences"] = int(entry["occurrences"]) + 1
            entry["played"] = int(entry["played"]) + int(played)
            entry["scenarios"].add(coverage.get("name", ""))
            entry["routes"].add(trace.get("route", ""))

    report: List[Dict[str, str]] = []
    runtime_prepared = runtime_wave_valid = 0
    missing_runtime = 0
    for action, candidate in sorted(route_data):
        entry = route_data[(action, candidate)]
        event = runtime.get(candidate)
        event_present = event is not None
        prepared = event_present and _true(event, "runtime_prepared")
        wave_valid = event_present and _true(event, "wave_header_valid")
        if prepared:
            runtime_prepared += 1
        if wave_valid:
            runtime_wave_valid += 1
        if not event_present:
            missing_runtime += 1
        valid = (
            int(entry["occurrences"]) > 0
            and int(entry["played"]) == int(entry["occurrences"])
            and event_present
            and prepared
            and wave_valid
            and event.get("status") == "verified-runtime-named-payload"
            and event.get("original_semantics") == ORIGINAL_SEMANTICS
        )
        row_status = STATUS if valid else "inconclusive-authored-audio-candidate-route-control"
        report.append({
            "action": action,
            "candidate": candidate,
            "trace_occurrences": str(entry["occurrences"]),
            "trace_played_occurrences": str(entry["played"]),
            "trace_route_values": ",".join(sorted(entry["routes"])),
            "runtime_event_present": str(event_present),
            "runtime_prepared": str(prepared),
            "runtime_wave_header_valid": str(wave_valid),
            "coverage_scenarios": ",".join(sorted(entry["scenarios"])),
            "status": row_status,
            "provenance": PROVENANCE,
            "original_semantics": ORIGINAL_SEMANTICS,
            "issue": "" if row_status == STATUS else "trace route and named runtime candidate did not reconcile",
        })

    missing_actions = set()
    summary_row = next((row for row in coverage_rows if row.get("kind") == "summary"), {})
    missing_actions.update(_split(summary_row.get("missing_policy_actions", "")))
    summary = {
        "coverage_source_report": "",
        "runtime_source_report": "",
        "scenario_rows": str(len(scenarios)),
        "verified_scenario_rows": str(verified_scenario_rows),
        "trace_rows": str(trace_rows),
        "trace_played_rows": str(trace_played_rows),
        "route_rows": str(len(report)),
        "unique_action_rows": str(len({action for action, _ in route_data})),
        "unique_candidate_rows": str(len({candidate for _, candidate in route_data})),
        "runtime_rows": str(len(runtime)),
        "runtime_prepared_route_rows": str(runtime_prepared),
        "runtime_wave_header_valid_route_rows": str(runtime_wave_valid),
        "missing_runtime_candidates": str(missing_runtime),
        "coverage_missing_policy_actions": str(len(missing_actions)),
        "status": STATUS if (
            len(scenarios) == 10
            and verified_scenario_rows == len(scenarios)
            and trace_rows == trace_played_rows == 54
            and len(report) == 15
            and len({action for action, _ in route_data}) == 15
            and len({candidate for _, candidate in route_data}) == 8
            and len(runtime) == 17
            and runtime_prepared == runtime_wave_valid == len(report)
            and missing_runtime == 0
            and not missing_actions
            and all(row["status"] == STATUS for row in report)
        ) else "inconclusive-authored-audio-candidate-route-control",
        "provenance": PROVENANCE,
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
    print("FIFA Street 2 independent authored audio candidate-route control")
    for field in SUMMARY_FIELDS:
        print(f"{field}: {summary[field]}")
    print(f"report: {args.report}")
    print(f"summary: {args.summary}")
    return 0 if summary["status"] == STATUS else 1


if __name__ == "__main__":
    raise SystemExit(main())
