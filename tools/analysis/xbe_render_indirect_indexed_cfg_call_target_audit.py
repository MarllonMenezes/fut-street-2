"""Inspect direct-call destinations observed in indexed-table CFG slices.

The input CFG report contributes only the bounded, already-observed immediate
call destinations.  Each distinct destination is then mapped and disassembled
in another bounded executable-section window.  This does not follow calls,
recover a function boundary, or assign render/material/UV/skinning/rigging/
animation/morph/GPU semantics.
"""

from __future__ import annotations

import argparse
import csv
import hashlib
from pathlib import Path
from typing import Iterable

from parse_xbe import analyze_xbe, virtual_to_file_offset
from xbe_render_call_target_audit import _prologue_shape
from xbe_render_cfg_audit import decode_window, load_xbe, read_tsv


STATUS = "verified-bounded-static-render-indirect-indexed-cfg-call-target-slices"
ORIGINAL_SEMANTICS = "not-claimed"

REPORT_FIELDS = (
    "call_target_virtual_address",
    "source_cfg_row_count",
    "source_candidate_targets",
    "source_unique_target_mentions",
    "section",
    "file_offset",
    "available_bytes",
    "instruction_count",
    "ret_found",
    "prologue_shape",
    "status",
    "original_semantics",
)

SUMMARY_FIELDS = (
    "image",
    "internal_path",
    "xbe_sha256",
    "xbe_size",
    "selection_policy",
    "cfg_direct_call_occurrences",
    "cfg_unique_direct_call_targets_sum",
    "distinct_direct_call_targets",
    "mapped_executable_targets",
    "disassembled_targets",
    "ret_terminated_targets",
    "prologue_like_targets",
    "status",
    "provenance",
    "original_semantics",
)


def _as_int(value: str | None) -> int | None:
    if not value:
        return None
    try:
        return int(value, 0)
    except ValueError:
        return None


def _address(value: int) -> str:
    return f"0x{value & 0xFFFFFFFF:08X}"


def collect_call_targets(
    source_rows: list[dict[str, str]],
) -> dict[int, dict[str, object]]:
    """Collect distinct target mentions without inferring call multiplicity."""

    targets: dict[int, dict[str, object]] = {}
    for row in source_rows:
        candidate = row.get("candidate_target_virtual_address", "")
        for raw_target in row.get("direct_call_target_addresses", "").split("|"):
            target = _as_int(raw_target)
            if target is None:
                continue
            entry = targets.setdefault(
                target,
                {"source_candidates": set(), "source_row_count": 0},
            )
            entry["source_candidates"].add(candidate)
            entry["source_row_count"] += 1
    return targets


def inspect_call_target(
    data: bytes,
    result: dict,
    target: int,
    metadata: dict[str, object],
    max_bytes: int,
) -> dict[str, str]:
    base_row = {
        "call_target_virtual_address": _address(target),
        "source_cfg_row_count": str(metadata["source_row_count"]),
        "source_candidate_targets": "|".join(sorted(metadata["source_candidates"])),
        "source_unique_target_mentions": "1",
        "section": "",
        "file_offset": "",
        "available_bytes": "0",
        "instruction_count": "0",
        "ret_found": "False",
        "prologue_shape": "none",
        "status": "not-mapped-executable",
        "original_semantics": ORIGINAL_SEMANTICS,
    }
    try:
        section, instructions = decode_window(data, result, target, max_bytes)
    except (StopIteration, ValueError):
        return base_row
    offset = virtual_to_file_offset(
        target,
        result["fields"]["base_address"],
        len(data),
        result["sections"],
    )
    if offset is None:
        return base_row
    available = min(max_bytes, section["file_address"] + section["file_size"] - offset)
    base_row.update(
        {
            "section": section["name"],
            "file_offset": _address(offset),
            "available_bytes": str(available),
            "instruction_count": str(len(instructions)),
            "ret_found": str(any(
                instruction.mnemonic.lower() in {"ret", "retf"}
                for instruction in instructions
            )),
            "prologue_shape": _prologue_shape(instructions),
            "status": STATUS,
        }
    )
    return base_row


def build_summary(
    image: str,
    internal_path: str,
    xbe_sha256: str,
    xbe_size: int,
    cfg_summary: dict[str, str],
    targets: dict[int, dict[str, object]],
    target_rows: list[dict[str, str]],
    max_bytes: int,
) -> dict[str, str]:
    return {
        "image": image,
        "internal_path": internal_path,
        "xbe_sha256": xbe_sha256,
        "xbe_size": str(xbe_size),
        "selection_policy": f"direct-call-targets-from-bounded-indexed-CFG; max_bytes={max_bytes}; no-call-following",
        "cfg_direct_call_occurrences": cfg_summary.get("total_direct_calls", "0"),
        "cfg_unique_direct_call_targets_sum": cfg_summary.get("total_unique_direct_call_targets", "0"),
        "distinct_direct_call_targets": str(len(targets)),
        "mapped_executable_targets": str(sum(row["status"] == STATUS for row in target_rows)),
        "disassembled_targets": str(sum(row["instruction_count"] != "0" for row in target_rows)),
        "ret_terminated_targets": str(sum(row["ret_found"] == "True" for row in target_rows)),
        "prologue_like_targets": str(sum(row["prologue_shape"] != "none" for row in target_rows)),
        "status": STATUS,
        "provenance": "same-ISO-retail-XBE+bounded-indexed-CFG-direct-call-destinations;no-execution;no-call-following;no-function-boundary-claim;no-binding",
        "original_semantics": ORIGINAL_SEMANTICS,
    }


def write_tsv(path: Path, fields: tuple[str, ...], rows: Iterable[dict[str, str]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=fields, delimiter="\t", lineterminator="\n")
        writer.writeheader()
        writer.writerows({field: row.get(field, "") for field in fields} for row in rows)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("image", type=Path)
    parser.add_argument("cfg_report", type=Path)
    parser.add_argument("cfg_summary", type=Path)
    parser.add_argument("--internal-path", default="default.xbe")
    parser.add_argument("--max-bytes", type=int, default=256)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()
    if args.max_bytes <= 0:
        raise SystemExit("--max-bytes deve ser positivo")

    data = load_xbe(args.image, args.internal_path)
    result = analyze_xbe(args.image, args.internal_path)
    source_rows = read_tsv(args.cfg_report)
    cfg_summary = (read_tsv(args.cfg_summary) or [{}])[0]
    targets = collect_call_targets(source_rows)
    target_rows = [
        inspect_call_target(data, result, target, metadata, args.max_bytes)
        for target, metadata in sorted(targets.items())
    ]
    summary = build_summary(
        str(args.image),
        args.internal_path,
        hashlib.sha256(data).hexdigest().upper(),
        len(data),
        cfg_summary,
        targets,
        target_rows,
        args.max_bytes,
    )
    write_tsv(args.report, REPORT_FIELDS, target_rows)
    write_tsv(args.summary, SUMMARY_FIELDS, [summary])
    for key in SUMMARY_FIELDS:
        print(f"{key}: {summary[key]}")
    print(f"report: {args.report}")
    print(f"summary: {args.summary}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
