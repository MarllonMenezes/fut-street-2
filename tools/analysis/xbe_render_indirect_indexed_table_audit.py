"""Scan bounded candidate entries behind indexed indirect render transfers.

The indexed operand audit exposes a table base but does not know its length.
This script performs a bounded, read-only scan of that base, stopping after two
consecutive non-executable words or the explicit entry limit. It records raw
words and executable-section classification only; it does not call entries,
recover functions, or assign render, material, UV, skinning, rigging,
animation, morph or GPU semantics.
"""

from __future__ import annotations

import argparse
import csv
import hashlib
import struct
from pathlib import Path
from typing import Iterable

from parse_xbe import analyze_xbe, virtual_to_file_offset
from xbe_render_cfg_audit import load_xbe, read_tsv
from xbe_render_indirect_reference_audit import section_for_address


STATUS = "verified-bounded-static-render-indirect-indexed-table"
ORIGINAL_SEMANTICS = "not-claimed"

REPORT_FIELDS = (
    "table_base_virtual_address",
    "source_target_addresses",
    "source_instruction_addresses",
    "table_section",
    "table_file_offset",
    "scan_max_entries",
    "scanned_entries",
    "scan_capped",
    "terminated_after_non_executable_words",
    "readable_entry_count",
    "executable_entry_count",
    "non_executable_entry_count",
    "unreadable_entry_count",
    "first_non_executable_index",
    "contiguous_executable_prefix",
    "candidate_entry_addresses",
    "status",
    "original_semantics",
)

SUMMARY_FIELDS = (
    "image",
    "internal_path",
    "xbe_sha256",
    "xbe_size",
    "selection_policy",
    "indexed_rows",
    "distinct_table_bases",
    "tables_scan_capped",
    "tables_terminated_after_non_executable_words",
    "readable_table_words",
    "executable_table_words",
    "non_executable_table_words",
    "unreadable_table_words",
    "distinct_executable_words",
    "tables_with_contiguous_executable_prefix",
    "max_contiguous_executable_prefix",
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


def _indexed_table_bases(rows: list[dict[str, str]]) -> dict[int, dict[str, set[int] | int]]:
    bases: dict[int, dict[str, set[int] | int]] = {}
    for row in rows:
        if row.get("addressing_mode") != "index":
            continue
        if row.get("memory_base_register"):
            continue
        displacement = _as_int(row.get("memory_displacement"))
        if displacement is None:
            continue
        entry = bases.setdefault(
            displacement & 0xFFFFFFFF,
            {
                "source_target_addresses": set(),
                "source_instruction_addresses": set(),
                "row_count": 0,
            },
        )
        target = _as_int(row.get("source_target_virtual_address"))
        instruction = _as_int(row.get("instruction_virtual_address"))
        if target is not None:
            entry["source_target_addresses"].add(target)
        if instruction is not None:
            entry["source_instruction_addresses"].add(instruction)
        entry["row_count"] += 1
    return bases


def _read_word(data: bytes, result: dict, address: int) -> tuple[int | None, int | None, dict | None]:
    section = section_for_address(result, address)
    if section is None:
        return None, None, None
    offset = virtual_to_file_offset(
        address,
        result["fields"]["base_address"],
        len(data),
        result["sections"],
    )
    if offset is None or offset < 0 or offset + 4 > len(data):
        return None, offset, section
    return struct.unpack_from("<I", data, offset)[0], offset, section


def scan_table(
    data: bytes,
    result: dict,
    base: int,
    metadata: dict[str, set[int] | int],
    max_entries: int,
) -> dict[str, str]:
    table_section = section_for_address(result, base)
    table_offset = virtual_to_file_offset(
        base,
        result["fields"]["base_address"],
        len(data),
        result["sections"],
    )
    readable = 0
    executable = 0
    non_executable = 0
    unreadable = 0
    first_non_executable = None
    contiguous_prefix = 0
    candidate_addresses: list[str] = []
    consecutive_non_executable = 0
    scanned = 0

    for index in range(max_entries):
        scanned += 1
        raw_word, _, _ = _read_word(data, result, base + index * 4)
        if raw_word is None:
            unreadable += 1
            consecutive_non_executable += 1
        else:
            readable += 1
            raw_section = section_for_address(result, raw_word)
            if raw_section is not None and raw_section["executable"]:
                executable += 1
                consecutive_non_executable = 0
                if contiguous_prefix == index:
                    contiguous_prefix += 1
                candidate_addresses.append(_address(raw_word))
            else:
                non_executable += 1
                consecutive_non_executable += 1
                if first_non_executable is None:
                    first_non_executable = index

        if consecutive_non_executable >= 2:
            break

    return {
        "table_base_virtual_address": _address(base),
        "source_target_addresses": "|".join(
            _address(value) for value in sorted(metadata["source_target_addresses"])
        ),
        "source_instruction_addresses": "|".join(
            _address(value) for value in sorted(metadata["source_instruction_addresses"])
        ),
        "table_section": table_section["name"] if table_section else "",
        "table_file_offset": _address(table_offset),
        "scan_max_entries": str(max_entries),
        "scanned_entries": str(scanned),
        "scan_capped": str(scanned == max_entries and first_non_executable is None),
        "terminated_after_non_executable_words": str(consecutive_non_executable >= 2 and scanned < max_entries),
        "readable_entry_count": str(readable),
        "executable_entry_count": str(executable),
        "non_executable_entry_count": str(non_executable),
        "unreadable_entry_count": str(unreadable),
        "first_non_executable_index": "" if first_non_executable is None else str(first_non_executable),
        "contiguous_executable_prefix": str(contiguous_prefix),
        "candidate_entry_addresses": "|".join(candidate_addresses),
        "status": STATUS if table_section is not None and scanned > 0 else "unmapped-indexed-table-base",
        "original_semantics": ORIGINAL_SEMANTICS,
    }


def write_tsv(path: Path, fields: Iterable[str], rows: Iterable[dict[str, str]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=list(fields), delimiter="\t", lineterminator="\n")
        writer.writeheader()
        writer.writerows(rows)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("image", type=Path)
    parser.add_argument("indirect_report", type=Path)
    parser.add_argument("--internal-path", default="default.xbe")
    parser.add_argument("--max-entries", type=int, default=256)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()
    if args.max_entries <= 0:
        raise SystemExit("o limite de entradas deve ser positivo")

    data = load_xbe(args.image, args.internal_path)
    result = analyze_xbe(args.image, args.internal_path)
    indirect_rows = read_tsv(args.indirect_report)
    bases = _indexed_table_bases(indirect_rows)
    report_rows = [
        scan_table(data, result, base, metadata, args.max_entries)
        for base, metadata in sorted(bases.items())
    ]
    executable_words = {
        address
        for row in report_rows
        for address in row["candidate_entry_addresses"].split("|")
        if address
    }
    summary = {
        "image": str(args.image),
        "internal_path": args.internal_path,
        "xbe_sha256": hashlib.sha256(data).hexdigest().upper(),
        "xbe_size": str(len(data)),
        "selection_policy": f"bounded indexed-table scan; max_entries={args.max_entries}; stop_after_two_non_executable_words; no execution",
        "indexed_rows": str(sum(int(metadata["row_count"]) for metadata in bases.values())),
        "distinct_table_bases": str(len(bases)),
        "tables_scan_capped": str(sum(row["scan_capped"] == "True" for row in report_rows)),
        "tables_terminated_after_non_executable_words": str(
            sum(row["terminated_after_non_executable_words"] == "True" for row in report_rows)
        ),
        "readable_table_words": str(sum(int(row["readable_entry_count"]) for row in report_rows)),
        "executable_table_words": str(sum(int(row["executable_entry_count"]) for row in report_rows)),
        "non_executable_table_words": str(sum(int(row["non_executable_entry_count"]) for row in report_rows)),
        "unreadable_table_words": str(sum(int(row["unreadable_entry_count"]) for row in report_rows)),
        "distinct_executable_words": str(len(executable_words)),
        "tables_with_contiguous_executable_prefix": str(
            sum(int(row["contiguous_executable_prefix"]) > 0 for row in report_rows)
        ),
        "max_contiguous_executable_prefix": str(
            max((int(row["contiguous_executable_prefix"]) for row in report_rows), default=0)
        ),
        "status": STATUS,
        "provenance": "same-ISO-retail-XBE+bounded-read-at-indexed-indirect-table-base;no-execution;no-call;no-function-boundary-claim;no-binding",
        "original_semantics": ORIGINAL_SEMANTICS,
    }
    write_tsv(args.report, REPORT_FIELDS, report_rows)
    write_tsv(args.summary, SUMMARY_FIELDS, [summary])
    print("FIFA Street 2 PC bounded indexed indirect-table audit")
    for key in SUMMARY_FIELDS:
        print(f"{key}: {summary[key]}")
    print(f"report: {args.report}")
    print(f"summary: {args.summary}")
    print("note: table words remain raw bounded candidates; no entry was called or assigned semantics")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
