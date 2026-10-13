"""Join indexed-table candidate targets against existing render target sets.

This is a bounded negative-control audit. It compares address sets only: the
candidate words from the indexed-table target audit are intersected with the
one-hop direct-call targets and with the ranked render-window targets. It does
not infer that non-overlap means unrelated behavior, and it assigns no
function, render, material, UV, skinning, rigging, animation, morph or GPU
semantics.
"""

from __future__ import annotations

import argparse
import csv
import hashlib
from pathlib import Path
from typing import Iterable

from xbe_render_cfg_audit import read_tsv


STATUS = "verified-structural-negative"
ORIGINAL_SEMANTICS = "not-claimed"

REPORT_FIELDS = (
    "candidate_target_virtual_address",
    "in_direct_call_target_set",
    "in_render_window_target_set",
    "status",
    "original_semantics",
)

SUMMARY_FIELDS = (
    "image",
    "internal_path",
    "xbe_sha256",
    "xbe_size",
    "selection_policy",
    "candidate_targets",
    "direct_call_targets",
    "render_window_targets",
    "candidate_targets_in_direct_call_targets",
    "candidate_targets_in_render_window_targets",
    "status",
    "provenance",
    "original_semantics",
)


def _address_set(rows: list[dict[str, str]], field: str) -> set[str]:
    return {row[field].upper() for row in rows if row.get(field)}


def build_join(
    candidate_rows: list[dict[str, str]],
    direct_call_rows: list[dict[str, str]],
    render_window_rows: list[dict[str, str]],
) -> tuple[list[dict[str, str]], dict[str, int]]:
    candidates = _address_set(candidate_rows, "candidate_target_virtual_address")
    direct_calls = _address_set(direct_call_rows, "call_target_virtual_address")
    render_windows = _address_set(render_window_rows, "target_virtual_address")
    rows = [
        {
            "candidate_target_virtual_address": address,
            "in_direct_call_target_set": str(address in direct_calls),
            "in_render_window_target_set": str(address in render_windows),
            "status": STATUS,
            "original_semantics": ORIGINAL_SEMANTICS,
        }
        for address in sorted(candidates)
    ]
    summary = {
        "candidate_targets": len(candidates),
        "direct_call_targets": len(direct_calls),
        "render_window_targets": len(render_windows),
        "candidate_targets_in_direct_call_targets": len(candidates & direct_calls),
        "candidate_targets_in_render_window_targets": len(candidates & render_windows),
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
    parser.add_argument("indexed_target_report", type=Path)
    parser.add_argument("direct_call_report", type=Path)
    parser.add_argument("render_window_report", type=Path)
    parser.add_argument("--image", required=True)
    parser.add_argument("--internal-path", default="default.xbe")
    parser.add_argument("--xbe-sha256", required=True)
    parser.add_argument("--xbe-size", required=True)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()

    candidate_rows = read_tsv(args.indexed_target_report)
    direct_call_rows = read_tsv(args.direct_call_report)
    render_window_rows = read_tsv(args.render_window_report)
    report_rows, counts = build_join(candidate_rows, direct_call_rows, render_window_rows)
    summary = {
        "image": args.image,
        "internal_path": args.internal_path,
        "xbe_sha256": args.xbe_sha256.upper(),
        "xbe_size": args.xbe_size,
        "selection_policy": "set intersection of bounded indexed-table candidates with direct-call and render-window reports; no binding",
        **{key: str(value) for key, value in counts.items()},
        "status": STATUS,
        "provenance": "same-ISO-retail-XBE+bounded-address-set-intersection;no-execution;no-call;no-function-boundary-claim;no-binding",
        "original_semantics": ORIGINAL_SEMANTICS,
    }
    write_tsv(args.report, REPORT_FIELDS, report_rows)
    write_tsv(args.summary, SUMMARY_FIELDS, [summary])
    print("FIFA Street 2 PC indexed indirect-target set-intersection audit")
    for key in SUMMARY_FIELDS:
        print(f"{key}: {summary[key]}")
    print(f"report: {args.report}")
    print(f"summary: {args.summary}")
    print("note: non-overlap is a negative control only; no behavioral or subsystem conclusion is assigned")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
