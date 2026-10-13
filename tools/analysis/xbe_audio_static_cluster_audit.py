"""Summarize conservative static audio evidence around retail-XBE literals.

The input reports are produced by ``xbe_string_xref_audit.py`` and the
textual asset cross-checks.  This tool groups only exact literal references
and nearby direct-call co-occurrence.  It does not recover functions, execute
the XBE, choose an EVT/ABKC sample, or assign gameplay/audio semantics.  All
inputs are read-only; the ISO is never opened for writing.
"""

from __future__ import annotations

import argparse
import csv
import re
from collections import defaultdict
from pathlib import Path
from typing import Iterable


AUDIO_LITERAL = re.compile(
    r"(?:audio|c[a-z]*audio|audio[a-z]*manager|audio[a-z]*trigger|"
    r"\.abk$|\.ast$|\.evt$|\.csi$|\.mus$|\.mpf$|\.off$|"
    r"djhdr|djdat|ambstrm|tunavars|xdbaudiosfx|beataudio|replay audio)",
    re.IGNORECASE,
)
AUDIO_EVENT_LITERAL = re.compile(r"\.evt$|event", re.IGNORECASE)


REPORT_FIELDS = (
    "string",
    "literal_category",
    "xref_count",
    "unique_xref_sources",
    "context_rows",
    "context_with_executable_calls",
    "distinct_executable_call_targets",
    "min_call_distance_bytes",
    "max_call_distance_bytes",
    "nested_match_kind",
    "nested_matches",
    "xdvdfs_match_kind",
    "xdvdfs_matches",
    "event_literal_xref_count",
    "status",
    "original_semantics",
)

CLUSTER_FIELDS = (
    "call_target_virtual_address",
    "context_rows",
    "unique_audio_xref_sources",
    "unique_audio_literals",
    "literal_categories",
    "audio_literals",
    "min_distance_bytes",
    "max_distance_bytes",
    "status",
    "original_semantics",
)

SUMMARY_FIELDS = (
    "image",
    "internal_path",
    "xbe_sha256",
    "xbe_size",
    "audio_xref_rows",
    "unique_audio_literals",
    "audio_context_rows",
    "audio_context_with_executable_calls",
    "distinct_executable_call_targets",
    "audio_targets_with_multiple_literals",
    "nested_audio_literal_matches",
    "xdvdfs_audio_literal_matches",
    "audio_event_literal_xref_rows",
    "status",
    "provenance",
    "original_semantics",
)


def is_audio_literal(value: str) -> bool:
    """Classify text only; this is not an audio binding."""

    return bool(AUDIO_LITERAL.search(value.strip()))


def is_audio_event_literal(value: str) -> bool:
    return bool(AUDIO_EVENT_LITERAL.search(value.strip()))


def read_tsv(path: Path) -> list[dict[str, str]]:
    with path.open("r", encoding="utf-8", newline="") as handle:
        return list(csv.DictReader(handle, delimiter="\t"))


def _as_int(value: str | None) -> int | None:
    if value is None or value == "":
        return None
    try:
        return int(value, 0)
    except ValueError:
        return None


def _join_values(values: Iterable[str]) -> str:
    return "|".join(sorted({value for value in values if value}, key=str.casefold))


def _asset_lookup(rows: Iterable[dict[str, str]], literal_field: str, match_field: str) -> dict[str, tuple[str, str]]:
    result: dict[str, tuple[str, str]] = {}
    for row in rows:
        literal = row.get(literal_field, "").strip().casefold()
        if not literal:
            continue
        result[literal] = (row.get("match_kind", ""), row.get(match_field, ""))
    return result


def build_audio_literal_rows(
    xrefs: Iterable[dict[str, str]],
    context: Iterable[dict[str, str]],
    nested_rows: Iterable[dict[str, str]] = (),
    xdvdfs_rows: Iterable[dict[str, str]] = (),
) -> list[dict[str, str]]:
    """Build one row per unique audio-related XBE literal."""

    audio_xrefs = [row for row in xrefs if is_audio_literal(row.get("string", ""))]
    audio_context = [row for row in context if is_audio_literal(row.get("xref_string", ""))]
    nested = _asset_lookup(nested_rows, "literal", "matched_nested_entries")
    xdvdfs = _asset_lookup(xdvdfs_rows, "literal", "matched_paths")
    by_literal: dict[str, list[dict[str, str]]] = defaultdict(list)
    context_by_literal: dict[str, list[dict[str, str]]] = defaultdict(list)
    for row in audio_xrefs:
        by_literal[row.get("string", "")].append(row)
    for row in audio_context:
        context_by_literal[row.get("xref_string", "")].append(row)

    output: list[dict[str, str]] = []
    for literal in sorted(by_literal, key=str.casefold):
        xref_rows = by_literal[literal]
        context_rows = context_by_literal.get(literal, [])
        executable = [
            row
            for row in context_rows
            if row.get("target_is_executable", "").casefold() == "true"
            and row.get("call_target_virtual_address", "")
        ]
        distances = [
            value
            for value in (_as_int(row.get("distance_bytes")) for row in executable)
            if value is not None
        ]
        nested_kind, nested_matches = nested.get(literal.casefold(), ("no-nested-report-row", ""))
        xdvdfs_kind, xdvdfs_matches = xdvdfs.get(literal.casefold(), ("no-xdvdfs-report-row", ""))
        output.append(
            {
                "string": literal,
                "literal_category": _join_values(row.get("literal_category", "") for row in xref_rows),
                "xref_count": str(len(xref_rows)),
                "unique_xref_sources": str(len({row.get("source_virtual_address", "") for row in xref_rows})),
                "context_rows": str(len(context_rows)),
                "context_with_executable_calls": str(len(executable)),
                "distinct_executable_call_targets": str(len({row.get("call_target_virtual_address", "") for row in executable})),
                "min_call_distance_bytes": str(min(distances)) if distances else "",
                "max_call_distance_bytes": str(max(distances)) if distances else "",
                "nested_match_kind": nested_kind,
                "nested_matches": nested_matches,
                "xdvdfs_match_kind": xdvdfs_kind,
                "xdvdfs_matches": xdvdfs_matches,
                "event_literal_xref_count": str(sum(is_audio_event_literal(literal) for _ in xref_rows)),
                "status": "candidate-static-audio-literal-context",
                "original_semantics": "not-claimed",
            }
        )
    return output


def build_audio_clusters(context: Iterable[dict[str, str]]) -> list[dict[str, str]]:
    """Group nearby executable call targets by exact address only."""

    grouped: dict[str, list[dict[str, str]]] = defaultdict(list)
    for row in context:
        literal = row.get("xref_string", "")
        if not is_audio_literal(literal):
            continue
        if row.get("target_is_executable", "").casefold() != "true":
            continue
        target = row.get("call_target_virtual_address", "")
        if target:
            grouped[target].append(row)

    output: list[dict[str, str]] = []
    for target, rows in grouped.items():
        distances = [
            value
            for value in (_as_int(row.get("distance_bytes")) for row in rows)
            if value is not None
        ]
        output.append(
            {
                "call_target_virtual_address": target,
                "context_rows": str(len(rows)),
                "unique_audio_xref_sources": str(len({row.get("xref_source_virtual_address", "") for row in rows})),
                "unique_audio_literals": str(len({row.get("xref_string", "") for row in rows})),
                "literal_categories": _join_values(row.get("literal_category", "") for row in rows),
                "audio_literals": _join_values(row.get("xref_string", "") for row in rows),
                "min_distance_bytes": str(min(distances)) if distances else "",
                "max_distance_bytes": str(max(distances)) if distances else "",
                "status": "candidate-static-audio-call-target-cooccurrence",
                "original_semantics": "not-claimed",
            }
        )
    return sorted(
        output,
        key=lambda row: (-int(row["unique_audio_literals"]), -int(row["context_rows"]), row["call_target_virtual_address"]),
    )


def build_summary(
    xrefs: Iterable[dict[str, str]],
    context: Iterable[dict[str, str]],
    literal_rows: list[dict[str, str]],
    cluster_rows: list[dict[str, str]],
    image: str,
    internal_path: str,
) -> dict[str, str]:
    audio_xrefs = [row for row in xrefs if is_audio_literal(row.get("string", ""))]
    audio_context = [row for row in context if is_audio_literal(row.get("xref_string", ""))]
    nested_matches = sum(
        row["nested_match_kind"] not in {"no-nested-report-row", "no-big-entry-match"}
        for row in literal_rows
    )
    xdvdfs_matches = sum(
        row["xdvdfs_match_kind"] not in {"no-xdvdfs-report-row", "no-xdvdfs-path-match"}
        for row in literal_rows
    )
    first = next(iter(audio_xrefs), {})
    return {
        "image": image,
        "internal_path": internal_path,
        "xbe_sha256": first.get("xbe_sha256", ""),
        "xbe_size": first.get("xbe_size", ""),
        "audio_xref_rows": str(len(audio_xrefs)),
        "unique_audio_literals": str(len(literal_rows)),
        "audio_context_rows": str(len(audio_context)),
        "audio_context_with_executable_calls": str(sum(
            row.get("target_is_executable", "").casefold() == "true"
            and bool(row.get("call_target_virtual_address", ""))
            for row in audio_context
        )),
        "distinct_executable_call_targets": str(len({
            row.get("call_target_virtual_address", "")
            for row in audio_context
            if row.get("target_is_executable", "").casefold() == "true"
            and row.get("call_target_virtual_address", "")
        })),
        "audio_targets_with_multiple_literals": str(sum(int(row["unique_audio_literals"]) > 1 for row in cluster_rows)),
        "nested_audio_literal_matches": str(nested_matches),
        "xdvdfs_audio_literal_matches": str(xdvdfs_matches),
        "audio_event_literal_xref_rows": str(sum(is_audio_event_literal(row.get("string", "")) for row in audio_xrefs)),
        "status": "verified-static-audio-literal-context",
        "provenance": "retail-XBE-static-xref+local-call-context+same-ISO-textual-crosschecks; no-execution",
        "original_semantics": "not-claimed",
    }


def write_tsv(path: Path, fields: tuple[str, ...], rows: Iterable[dict[str, str]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=fields, delimiter="\t", lineterminator="\n")
        writer.writeheader()
        writer.writerows({field: row.get(field, "") for field in fields} for row in rows)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("xref_report", type=Path)
    parser.add_argument("context_report", type=Path)
    parser.add_argument("--nested-report", type=Path)
    parser.add_argument("--xdvdfs-report", type=Path)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--cluster-report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()

    xrefs = read_tsv(args.xref_report)
    context = read_tsv(args.context_report)
    nested = read_tsv(args.nested_report) if args.nested_report else []
    xdvdfs = read_tsv(args.xdvdfs_report) if args.xdvdfs_report else []
    literal_rows = build_audio_literal_rows(xrefs, context, nested, xdvdfs)
    cluster_rows = build_audio_clusters(context)
    first = next(iter(xrefs), {})
    summary = build_summary(
        xrefs,
        context,
        literal_rows,
        cluster_rows,
        first.get("image", ""),
        first.get("internal_path", ""),
    )
    write_tsv(args.report, REPORT_FIELDS, literal_rows)
    write_tsv(args.cluster_report, CLUSTER_FIELDS, cluster_rows)
    write_tsv(args.summary, SUMMARY_FIELDS, [summary])
    print(
        f"audio_xref_rows: {summary['audio_xref_rows']}; "
        f"unique_audio_literals: {summary['unique_audio_literals']}; "
        f"audio_context_with_executable_calls: {summary['audio_context_with_executable_calls']}; "
        f"distinct_executable_call_targets: {summary['distinct_executable_call_targets']}; "
        f"status: {summary['status']}"
    )
    print(f"report: {args.report}")
    print(f"cluster_report: {args.cluster_report}")
    print(f"summary: {args.summary}")
    print("original_semantics: not-claimed")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
