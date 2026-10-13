"""Summarize conservative static render-context evidence around retail-XBE literals.

The input reports are produced by ``xbe_string_xref_audit.py``.  This audit
groups only exact render/geometry-related literals and nearby direct-call
co-occurrence.  It does not recover functions, execute the XBE, decode a GPU
command stream, or assign mesh, material, UV, skinning, rigging, morph or
render-state semantics.  All inputs are read-only; the ISO is never opened for
writing.
"""

from __future__ import annotations

import argparse
import csv
import re
from collections import defaultdict
from pathlib import Path
from typing import Iterable


RENDER_LITERAL_PATTERNS: tuple[tuple[str, re.Pattern[str]], ...] = (
    (
        "d3d-or-render-api-token",
        re.compile(
            r"(?:D3DTexture|D3D|RenderRegion|RenderEnvironment|RenderProperties|"
            r"RenderTime|Texture|Vertex|Shader)",
            re.IGNORECASE,
        ),
    ),
    (
        "eagl-geometry-token",
        re.compile(
            r"(?:EAGL::|SHAPENAME|SHPX|SUVW\w*|QSUV\w*|geom(?:etry|mesh)|"
            r"mesh|model|\.ord$|\.orl$|\.xsh$|skin%|skin|collbody)",
            re.IGNORECASE,
        ),
    ),
    (
        "render-script-token",
        re.compile(r"(?:cRenderProperties|cScriptRenderText|ResetRenderTime)", re.IGNORECASE),
    ),
    (
        "render-source-path-token",
        re.compile(r"(?:source[/\\].*(?:render|geom)|geomlib|render/)", re.IGNORECASE),
    ),
)


REPORT_FIELDS = (
    "string",
    "literal_category",
    "render_token_categories",
    "xref_count",
    "unique_xref_sources",
    "context_rows",
    "context_with_executable_calls",
    "distinct_executable_call_targets",
    "min_call_distance_bytes",
    "max_call_distance_bytes",
    "status",
    "original_semantics",
)

CLUSTER_FIELDS = (
    "call_target_virtual_address",
    "context_rows",
    "unique_render_xref_sources",
    "unique_render_literals",
    "literal_categories",
    "render_literals",
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
    "render_xref_rows",
    "unique_render_literals",
    "render_context_rows",
    "render_context_with_executable_calls",
    "distinct_executable_call_targets",
    "render_targets_with_multiple_literals",
    "category_counts",
    "status",
    "provenance",
    "original_semantics",
)


def classify_render_literal(value: str) -> tuple[str, ...]:
    """Classify text only; this is not a render binding."""

    stripped = value.strip()
    return tuple(name for name, pattern in RENDER_LITERAL_PATTERNS if pattern.search(stripped))


def is_render_literal(value: str) -> bool:
    return bool(classify_render_literal(value))


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


def build_render_literal_rows(
    xrefs: Iterable[dict[str, str]],
    context: Iterable[dict[str, str]],
) -> list[dict[str, str]]:
    """Build one row per unique render/geometry-related XBE literal."""

    render_xrefs = [row for row in xrefs if is_render_literal(row.get("string", ""))]
    render_context = [row for row in context if is_render_literal(row.get("xref_string", ""))]
    by_literal: dict[str, list[dict[str, str]]] = defaultdict(list)
    context_by_literal: dict[str, list[dict[str, str]]] = defaultdict(list)
    for row in render_xrefs:
        by_literal[row.get("string", "")].append(row)
    for row in render_context:
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
        output.append(
            {
                "string": literal,
                "literal_category": _join_values(row.get("literal_category", "") for row in xref_rows),
                "render_token_categories": _join_values(classify_render_literal(literal)),
                "xref_count": str(len(xref_rows)),
                "unique_xref_sources": str(len({row.get("source_virtual_address", "") for row in xref_rows})),
                "context_rows": str(len(context_rows)),
                "context_with_executable_calls": str(len(executable)),
                "distinct_executable_call_targets": str(len({row.get("call_target_virtual_address", "") for row in executable})),
                "min_call_distance_bytes": str(min(distances)) if distances else "",
                "max_call_distance_bytes": str(max(distances)) if distances else "",
                "status": "candidate-static-render-literal-context",
                "original_semantics": "not-claimed",
            }
        )
    return output


def build_render_clusters(context: Iterable[dict[str, str]]) -> list[dict[str, str]]:
    """Group nearby executable call targets by exact address only."""

    grouped: dict[str, list[dict[str, str]]] = defaultdict(list)
    for row in context:
        if not is_render_literal(row.get("xref_string", "")):
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
                "unique_render_xref_sources": str(len({row.get("xref_source_virtual_address", "") for row in rows})),
                "unique_render_literals": str(len({row.get("xref_string", "") for row in rows})),
                "literal_categories": _join_values(row.get("literal_category", "") for row in rows),
                "render_literals": _join_values(row.get("xref_string", "") for row in rows),
                "min_distance_bytes": str(min(distances)) if distances else "",
                "max_distance_bytes": str(max(distances)) if distances else "",
                "status": "candidate-static-render-call-target-cooccurrence",
                "original_semantics": "not-claimed",
            }
        )
    return sorted(
        output,
        key=lambda row: (-int(row["unique_render_literals"]), -int(row["context_rows"]), row["call_target_virtual_address"]),
    )


def build_summary(
    xrefs: Iterable[dict[str, str]],
    context: Iterable[dict[str, str]],
    literal_rows: list[dict[str, str]],
    cluster_rows: list[dict[str, str]],
    image: str,
    internal_path: str,
) -> dict[str, str]:
    render_xrefs = [row for row in xrefs if is_render_literal(row.get("string", ""))]
    render_context = [row for row in context if is_render_literal(row.get("xref_string", ""))]
    category_counts: dict[str, int] = defaultdict(int)
    for row in literal_rows:
        for category in row["render_token_categories"].split("|"):
            if category:
                category_counts[category] += 1
    first = next(iter(render_xrefs), {})
    return {
        "image": image,
        "internal_path": internal_path,
        "xbe_sha256": first.get("xbe_sha256", ""),
        "xbe_size": first.get("xbe_size", ""),
        "render_xref_rows": str(len(render_xrefs)),
        "unique_render_literals": str(len(literal_rows)),
        "render_context_rows": str(len(render_context)),
        "render_context_with_executable_calls": str(sum(
            row.get("target_is_executable", "").casefold() == "true"
            and bool(row.get("call_target_virtual_address", ""))
            for row in render_context
        )),
        "distinct_executable_call_targets": str(len({
            row.get("call_target_virtual_address", "")
            for row in render_context
            if row.get("target_is_executable", "").casefold() == "true"
            and row.get("call_target_virtual_address", "")
        })),
        "render_targets_with_multiple_literals": str(sum(int(row["unique_render_literals"]) > 1 for row in cluster_rows)),
        "category_counts": _join_values(f"{key}:{category_counts[key]}" for key in sorted(category_counts)),
        "status": "verified-static-render-literal-context",
        "provenance": "retail-XBE-static-xref+local-call-context; no-execution; no-binding",
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
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--cluster-report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()

    xrefs = read_tsv(args.xref_report)
    context = read_tsv(args.context_report)
    literal_rows = build_render_literal_rows(xrefs, context)
    cluster_rows = build_render_clusters(context)
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
        f"render_xref_rows: {summary['render_xref_rows']}; "
        f"unique_render_literals: {summary['unique_render_literals']}; "
        f"render_context_with_executable_calls: {summary['render_context_with_executable_calls']}; "
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
