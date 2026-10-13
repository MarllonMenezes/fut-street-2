"""Inspect bounded code windows at candidates from an indexed table scan.

The indexed-table audit classifies raw words by executable-section membership,
but does not inspect their bytes. This companion audit disassembles a bounded
window at each distinct executable candidate and retains the table occurrence
indices. It is still only static observation: no entry is called, no function
boundary is recovered, and no render, material, UV, skinning, rigging,
animation, morph or GPU semantics are assigned.
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
from xbe_render_indirect_reference_audit import section_for_address


STATUS = "verified-bounded-static-render-indirect-indexed-target-slices"
ORIGINAL_SEMANTICS = "not-claimed"

REPORT_FIELDS = (
    "candidate_target_virtual_address",
    "table_entry_indices",
    "table_base_addresses",
    "entry_occurrence_count",
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
    "indexed_table_rows",
    "candidate_entry_occurrences",
    "distinct_candidate_targets",
    "mapped_executable_targets",
    "disassembled_candidate_targets",
    "ret_terminated_candidate_targets",
    "prologue_like_candidate_targets",
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


def _address(value: int | None) -> str:
    return "" if value is None else f"0x{value & 0xFFFFFFFF:08X}"


def collect_indexed_targets(rows: list[dict[str, str]]) -> dict[int, dict[str, object]]:
    """Expand bounded candidate address lists while preserving duplicates."""

    targets: dict[int, dict[str, object]] = {}
    table_rows = 0
    for row in rows:
        addresses = [
            _as_int(value)
            for value in row.get("candidate_entry_addresses", "").split("|")
        ]
        addresses = [value for value in addresses if value is not None]
        if addresses:
            table_rows += 1
        base = _as_int(row.get("table_base_virtual_address"))
        base_text = _address(base)
        for index, address in enumerate(addresses):
            entry = targets.setdefault(
                address,
                {
                    "table_entry_indices": set(),
                    "table_base_addresses": set(),
                    "entry_occurrence_count": 0,
                },
            )
            entry["table_entry_indices"].add(index)
            if base is not None:
                entry["table_base_addresses"].add(base_text)
            entry["entry_occurrence_count"] += 1
    return {"targets": targets, "table_rows": table_rows}


def inspect_indexed_target(
    data: bytes,
    result: dict,
    target: int,
    metadata: dict[str, object],
    max_bytes: int,
) -> dict[str, str]:
    section = section_for_address(result, target)
    base_row = {
        "candidate_target_virtual_address": _address(target),
        "table_entry_indices": "|".join(
            str(value) for value in sorted(metadata["table_entry_indices"])
        ),
        "table_base_addresses": "|".join(sorted(metadata["table_base_addresses"])),
        "entry_occurrence_count": str(metadata["entry_occurrence_count"]),
        "section": section["name"] if section else "",
        "file_offset": "",
        "available_bytes": "0",
        "instruction_count": "0",
        "ret_found": "False",
        "prologue_shape": "none",
        "status": "not-mapped-executable",
        "original_semantics": ORIGINAL_SEMANTICS,
    }
    if section is None or not section["executable"]:
        return base_row
    try:
        decoded_section, instructions = decode_window(data, result, target, max_bytes)
    except (StopIteration, ValueError):
        base_row["status"] = "mapped-executable-undecodable-window"
        return base_row
    offset = virtual_to_file_offset(
        target,
        result["fields"]["base_address"],
        len(data),
        result["sections"],
    )
    if offset is None:
        base_row["status"] = "mapped-executable-no-file-offset"
        return base_row
    available = min(max_bytes, decoded_section["file_address"] + decoded_section["file_size"] - offset)
    base_row.update(
        {
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


def write_tsv(path: Path, fields: tuple[str, ...], rows: Iterable[dict[str, str]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=fields, delimiter="\t", lineterminator="\n")
        writer.writeheader()
        writer.writerows({field: row.get(field, "") for field in fields} for row in rows)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("image", type=Path)
    parser.add_argument("indexed_table_report", type=Path)
    parser.add_argument("--internal-path", default="default.xbe")
    parser.add_argument("--max-bytes", type=int, default=256)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()
    if args.max_bytes <= 0:
        raise SystemExit("o limite de bytes deve ser positivo")

    data = load_xbe(args.image, args.internal_path)
    result = analyze_xbe(args.image, args.internal_path)
    table_rows = read_tsv(args.indexed_table_report)
    collected = collect_indexed_targets(table_rows)
    targets = collected["targets"]
    report_rows = [
        inspect_indexed_target(data, result, target, metadata, args.max_bytes)
        for target, metadata in sorted(targets.items())
    ]
    summary = {
        "image": str(args.image),
        "internal_path": args.internal_path,
        "xbe_sha256": hashlib.sha256(data).hexdigest().upper(),
        "xbe_size": str(len(data)),
        "selection_policy": f"bounded executable-section disassembly at indexed-table candidates; max_bytes={args.max_bytes}; no execution; no function-boundary claim",
        "indexed_table_rows": str(collected["table_rows"]),
        "candidate_entry_occurrences": str(sum(int(row["entry_occurrence_count"]) for row in report_rows)),
        "distinct_candidate_targets": str(len(targets)),
        "mapped_executable_targets": str(sum(bool(row["section"]) and row["status"] != "not-mapped-executable" for row in report_rows)),
        "disassembled_candidate_targets": str(sum(int(row["instruction_count"]) > 0 for row in report_rows)),
        "ret_terminated_candidate_targets": str(sum(row["ret_found"] == "True" for row in report_rows)),
        "prologue_like_candidate_targets": str(sum(row["prologue_shape"] != "none" for row in report_rows)),
        "status": STATUS,
        "provenance": "same-ISO-retail-XBE+bounded-disassembly-of-indexed-table-candidates;no-execution;no-call;no-function-boundary-claim;no-binding",
        "original_semantics": ORIGINAL_SEMANTICS,
    }
    write_tsv(args.report, REPORT_FIELDS, report_rows)
    write_tsv(args.summary, SUMMARY_FIELDS, [summary])
    print("FIFA Street 2 PC bounded indexed-table candidate-target audit")
    for key in SUMMARY_FIELDS:
        print(f"{key}: {summary[key]}")
    print(f"report: {args.report}")
    print(f"summary: {args.summary}")
    print("note: candidate target windows are bounded static observations; no entry was called or assigned semantics")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
