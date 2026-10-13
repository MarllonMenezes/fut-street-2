"""Join direct-call targets from indexed CFG slices with render target sets.

The indexed CFG audit records direct call occurrences but does not follow the
called address.  This audit performs only bounded address-set intersections
against the existing one-hop render call-target, render-window, and bounded
render call-graph reports.  It is structural evidence only: non-overlap is a
negative control, overlap would still not prove a function boundary or
render/material/UV/skinning/rigging/animation/morph/GPU semantics.
"""

from __future__ import annotations

import argparse
import csv
from pathlib import Path
from typing import Iterable

from xbe_render_cfg_audit import read_tsv


ORIGINAL_SEMANTICS = "not-claimed"

REPORT_FIELDS = (
    "cfg_direct_call_target_virtual_address",
    "cfg_direct_call_occurrence_count",
    "in_render_direct_call_target_set",
    "in_render_window_target_set",
    "in_render_call_graph_node_set",
    "status",
    "original_semantics",
)

SUMMARY_FIELDS = (
    "image",
    "internal_path",
    "xbe_sha256",
    "xbe_size",
    "selection_policy",
    "cfg_direct_call_targets",
    "render_direct_call_targets",
    "render_window_targets",
    "render_call_graph_nodes",
    "cfg_targets_in_render_direct_call_targets",
    "cfg_targets_in_render_window_targets",
    "cfg_targets_in_render_call_graph_nodes",
    "status",
    "provenance",
    "original_semantics",
)


def _address_set(rows: list[dict[str, str]], field: str) -> set[str]:
    return {row[field].upper() for row in rows if row.get(field)}


def _cfg_direct_call_targets(rows: list[dict[str, str]]) -> dict[str, int]:
    counts: dict[str, int] = {}
    for row in rows:
        for address in row.get("direct_call_target_addresses", "").split("|"):
            if address:
                normalized = address.upper()
                counts[normalized] = counts.get(normalized, 0) + 1
    return counts


def build_join(
    cfg_rows: list[dict[str, str]],
    render_direct_call_rows: list[dict[str, str]],
    render_window_rows: list[dict[str, str]],
    call_graph_rows: list[dict[str, str]],
) -> tuple[list[dict[str, str]], dict[str, int | str]]:
    cfg_targets = _cfg_direct_call_targets(cfg_rows)
    render_direct_calls = _address_set(render_direct_call_rows, "call_target_virtual_address")
    render_windows = _address_set(render_window_rows, "target_virtual_address")
    call_graph_nodes = _address_set(call_graph_rows, "call_target_virtual_address")
    overlaps = {
        "render_direct": set(cfg_targets) & render_direct_calls,
        "render_window": set(cfg_targets) & render_windows,
        "call_graph": set(cfg_targets) & call_graph_nodes,
    }
    status = "verified-structural-negative" if not any(overlaps.values()) else "verified-structural"
    rows = [
        {
            "cfg_direct_call_target_virtual_address": address,
            "cfg_direct_call_occurrence_count": str(cfg_targets[address]),
            "in_render_direct_call_target_set": str(address in render_direct_calls),
            "in_render_window_target_set": str(address in render_windows),
            "in_render_call_graph_node_set": str(address in call_graph_nodes),
            "status": status,
            "original_semantics": ORIGINAL_SEMANTICS,
        }
        for address in sorted(cfg_targets)
    ]
    summary: dict[str, int | str] = {
        "cfg_direct_call_targets": len(cfg_targets),
        "render_direct_call_targets": len(render_direct_calls),
        "render_window_targets": len(render_windows),
        "render_call_graph_nodes": len(call_graph_nodes),
        "cfg_targets_in_render_direct_call_targets": len(overlaps["render_direct"]),
        "cfg_targets_in_render_window_targets": len(overlaps["render_window"]),
        "cfg_targets_in_render_call_graph_nodes": len(overlaps["call_graph"]),
        "status": status,
    }
    return rows, summary


def write_tsv(path: Path, fields: tuple[str, ...], rows: Iterable[dict[str, str]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=fields, delimiter="\t", lineterminator="\n")
        writer.writeheader()
        writer.writerows({field: row.get(field, "") for field in fields} for row in rows)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("cfg_report", type=Path)
    parser.add_argument("render_direct_call_report", type=Path)
    parser.add_argument("render_window_report", type=Path)
    parser.add_argument("call_graph_node_report", type=Path)
    parser.add_argument("--image", required=True)
    parser.add_argument("--internal-path", default="default.xbe")
    parser.add_argument("--xbe-sha256", required=True)
    parser.add_argument("--xbe-size", required=True)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()

    report_rows, counts = build_join(
        read_tsv(args.cfg_report),
        read_tsv(args.render_direct_call_report),
        read_tsv(args.render_window_report),
        read_tsv(args.call_graph_node_report),
    )
    summary = {
        "image": args.image,
        "internal_path": args.internal_path,
        "xbe_sha256": args.xbe_sha256.upper(),
        "xbe_size": args.xbe_size,
        "selection_policy": "bounded address-set intersection of indexed CFG direct-call targets with render reports; no call following; no binding",
        **{key: str(value) for key, value in counts.items()},
        "provenance": "same-ISO-retail-XBE+bounded-indexed-CFG-call-target-set-intersection;no-execution;no-call-following;no-function-boundary-claim;no-binding",
        "original_semantics": ORIGINAL_SEMANTICS,
    }
    write_tsv(args.report, REPORT_FIELDS, report_rows)
    write_tsv(args.summary, SUMMARY_FIELDS, [summary])
    print("FIFA Street 2 PC indexed-CFG direct-call set-intersection audit")
    for key in SUMMARY_FIELDS:
        print(f"{key}: {summary[key]}")
    print(f"report: {args.report}")
    print(f"summary: {args.summary}")
    print("note: set intersections are structural controls only; no target was called or assigned semantics")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
