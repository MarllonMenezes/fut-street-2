"""Synthesize independent negative controls for original EVT sample selection.

This control joins already generated, independently bounded reports.  It makes
the remaining audio limitation explicit without converting a named EVT payload
or an authored route into an original event/sample binding.  It does not
execute the XBE, select an ABKC/AST/BNK sample, or claim playback semantics.
"""

from __future__ import annotations

import argparse
import csv
from pathlib import Path
from typing import Dict, Iterable, List, Sequence, Tuple


STATUS = "verified-independent-audio-selection-negative-synthesis"
DIRECT_SELECTION_BINDING = "not-proven"
ORIGINAL_SEMANTICS = "not-claimed"
PROVENANCE = (
    "independent-keyed-synthesis-of-EVT-index/XBE-word/payload-PCM/"
    "parallel-runtime/authored-route-controls;no-event-sample-promotion"
)

REPORT_FIELDS = (
    "source",
    "status",
    "valid",
    "observed",
    "expected",
    "evidence",
    "issue",
    "direct_selection_binding",
    "original_semantics",
)

SUMMARY_FIELDS = (
    "source_count",
    "valid_source_count",
    "index_candidate_count",
    "index_abk_occurrences",
    "index_header_occurrences",
    "index_data_occurrences",
    "xbe_candidate_count",
    "xbe_aligned_occurrences",
    "xbe_unaligned_occurrences",
    "payload_event_candidates",
    "payload_event_decoded",
    "payload_bank_count",
    "payload_bank_sound_candidates",
    "payload_bank_sound_decoded",
    "payload_exact_pcm_hash_pairs",
    "payload_event_hashes_with_match",
    "parallel_joined_rows",
    "authored_route_rows",
    "status",
    "provenance",
    "direct_selection_binding",
    "original_semantics",
)


def read_tsv(path: Path) -> List[Dict[str, str]]:
    with path.open("r", encoding="utf-8-sig", newline="") as handle:
        return list(csv.DictReader(handle, delimiter="\t"))


def read_key_value(path: Path) -> Dict[str, str]:
    rows = read_tsv(path)
    if not rows:
        return {}
    return {row.get("field", ""): row.get("value", "") for row in rows}


def read_first(path: Path) -> Dict[str, str]:
    rows = read_tsv(path)
    return rows[0] if rows else {}


def _int(values: Dict[str, str], field: str) -> int:
    try:
        return int(values.get(field, ""), 0)
    except (TypeError, ValueError) as exc:
        raise ValueError(f"invalid integer in {field}: {values.get(field)!r}") from exc


def _valid_source(
    source: str,
    values: Dict[str, str],
    checks: Iterable[Tuple[str, str]],
) -> Tuple[bool, str, str]:
    failures = [f"{field}={values.get(field, '')!r}, expected {expected!r}"
                for field, expected in checks
                if values.get(field, "") != expected]
    original_ok = values.get("original_semantics") == ORIGINAL_SEMANTICS
    if not original_ok:
        failures.append("original_semantics is not not-claimed")
    return (
        not failures,
        ", ".join(failures),
        f"{source}: status={values.get('status', '')}; "
        f"original_semantics={values.get('original_semantics', '')}",
    )


def analyze(
    index_values: Dict[str, str],
    xbe_values: Dict[str, str],
    payload_values: Dict[str, str],
    parallel_values: Dict[str, str],
    authored_values: Dict[str, str],
) -> Tuple[List[Dict[str, str]], Dict[str, str]]:
    source_specs = [
        (
            "evt-index",
            index_values,
            [
                ("status", "no-direct-selection-binding"),
                ("direct_selection_binding", DIRECT_SELECTION_BINDING),
                ("string_hash_family_candidates", "17"),
                ("abk_aligned_occurrences", "0"),
                ("pcpahdr_big_aligned_occurrences", "0"),
                ("pcpadat_big_aligned_occurrences", "0"),
            ],
            "17 candidate words; no aligned hits in ABK, PCPAHDR or PCPADAT",
        ),
        (
            "xbe-word-search",
            xbe_values,
            [
                ("status", "verified-structural-xbe-hash-search"),
                ("direct_selection_binding", DIRECT_SELECTION_BINDING),
                ("candidate_count", "17"),
                ("aligned_occurrences", "0"),
                ("unaligned_occurrences", "0"),
            ],
            "17 candidate words; no aligned or byte-search hits in default.xbe",
        ),
        (
            "payload-pcm-correlation",
            payload_values,
            [
                ("status", "verified-structural"),
                ("direct_selection_binding", DIRECT_SELECTION_BINDING),
                ("event_candidates", "17"),
                ("event_decoded", "17"),
                ("bank_count", "16"),
                ("exact_pcm_hash_pairs", "0"),
                ("event_hashes_with_match", "0"),
            ],
            "all named EVT payloads decoded; no exact PCM hash pair among bank candidates",
        ),
        (
            "parallel-runtime-join",
            parallel_values,
            [
                ("status", "verified-independent-audio-event-parallel-runtime-join-control"),
                ("original_semantics", ORIGINAL_SEMANTICS),
                ("joined_rows", "17"),
                ("missing_runtime_names", "0"),
                ("missing_parallel_names", "0"),
            ],
            "17 named EVT ranges preserved through managed payload preparation",
        ),
        (
            "authored-route-control",
            authored_values,
            [
                ("status", "verified-independent-authored-audio-candidate-route-control"),
                ("route_rows", "15"),
                ("missing_runtime_candidates", "0"),
                ("coverage_missing_policy_actions", "0"),
            ],
            "15 authored action-to-candidate routes replayed with prepared runtime candidates",
        ),
    ]
    report: List[Dict[str, str]] = []
    valid_count = 0
    for source, values, checks, evidence in source_specs:
        valid, issue, _ = _valid_source(source, values, checks)
        valid_count += int(valid)
        report.append({
            "source": source,
            "status": STATUS if valid else f"inconclusive-{STATUS}",
            "valid": str(valid),
            "observed": ";".join(f"{field}={values.get(field, '')}" for field, _ in checks),
            "expected": ";".join(f"{field}={expected}" for field, expected in checks),
            "evidence": evidence,
            "issue": issue,
            "direct_selection_binding": DIRECT_SELECTION_BINDING,
            "original_semantics": ORIGINAL_SEMANTICS,
        })

    all_valid = valid_count == len(source_specs)
    summary = {
        "source_count": str(len(source_specs)),
        "valid_source_count": str(valid_count),
        "index_candidate_count": index_values.get("string_hash_family_candidates", ""),
        "index_abk_occurrences": index_values.get("abk_aligned_occurrences", ""),
        "index_header_occurrences": index_values.get("pcpahdr_big_aligned_occurrences", ""),
        "index_data_occurrences": index_values.get("pcpadat_big_aligned_occurrences", ""),
        "xbe_candidate_count": xbe_values.get("candidate_count", ""),
        "xbe_aligned_occurrences": xbe_values.get("aligned_occurrences", ""),
        "xbe_unaligned_occurrences": xbe_values.get("unaligned_occurrences", ""),
        "payload_event_candidates": payload_values.get("event_candidates", ""),
        "payload_event_decoded": payload_values.get("event_decoded", ""),
        "payload_bank_count": payload_values.get("bank_count", ""),
        "payload_bank_sound_candidates": payload_values.get("bank_sound_candidates", ""),
        "payload_bank_sound_decoded": payload_values.get("bank_sound_decoded", ""),
        "payload_exact_pcm_hash_pairs": payload_values.get("exact_pcm_hash_pairs", ""),
        "payload_event_hashes_with_match": payload_values.get("event_hashes_with_match", ""),
        "parallel_joined_rows": parallel_values.get("joined_rows", ""),
        "authored_route_rows": authored_values.get("route_rows", ""),
        "status": STATUS if all_valid else f"inconclusive-{STATUS}",
        "provenance": PROVENANCE,
        "direct_selection_binding": DIRECT_SELECTION_BINDING,
        "original_semantics": ORIGINAL_SEMANTICS,
    }
    return report, summary


def write_tsv(path: Path, fields: Sequence[str], rows: Sequence[Dict[str, str]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=list(fields), delimiter="\t", lineterminator="\n")
        writer.writeheader()
        writer.writerows({field: row.get(field, "") for field in fields} for row in rows)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("index_summary", type=Path)
    parser.add_argument("xbe_summary", type=Path)
    parser.add_argument("payload_summary", type=Path)
    parser.add_argument("parallel_summary", type=Path)
    parser.add_argument("authored_summary", type=Path)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()
    report, summary = analyze(
        read_key_value(args.index_summary),
        read_key_value(args.xbe_summary),
        read_key_value(args.payload_summary),
        read_first(args.parallel_summary),
        read_first(args.authored_summary),
    )
    write_tsv(args.report, REPORT_FIELDS, report)
    write_tsv(args.summary, ("field", "value"), [{"field": field, "value": value} for field, value in summary.items()])
    print("FIFA Street 2 independent original audio-selection negative synthesis")
    for field in SUMMARY_FIELDS:
        print(f"{field}: {summary[field]}")
    print(f"report: {args.report}")
    print(f"summary: {args.summary}")
    return 0 if summary["status"] == STATUS else 1


if __name__ == "__main__":
    raise SystemExit(main())
