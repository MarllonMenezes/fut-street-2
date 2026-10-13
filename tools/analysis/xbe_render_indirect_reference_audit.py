"""Audit bounded words referenced by absolute indirect render operands.

The preceding indirect-transfer audit deliberately leaves register and
indexed targets unresolved. This audit reads only the four bytes at an
absolute memory operand (and the first word at a base-less indexed-table
candidate), classifies the raw word against XBE section ranges, and stops
there. It does not call the word, recover a function boundary, or assign
rendering, material, UV, skinning, rigging, animation, morph or GPU meaning.
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


REPORT_FIELDS = (
    "source_target_virtual_address",
    "instruction_virtual_address",
    "operand_string",
    "addressing_mode",
    "memory_base_register",
    "memory_index_register",
    "memory_scale",
    "memory_displacement",
    "memory_reference_virtual_address",
    "memory_reference_file_offset",
    "raw_word",
    "raw_word_section",
    "raw_word_is_executable",
    "reference_read_kind",
    "resolution_status",
    "status",
    "original_semantics",
)

SUMMARY_FIELDS = (
    "image",
    "internal_path",
    "xbe_sha256",
    "xbe_size",
    "selection_policy",
    "indirect_rows",
    "reference_rows",
    "absolute_memory_rows",
    "indexed_table_base_rows",
    "bounded_reference_reads",
    "reference_words_valid",
    "reference_words_in_executable_sections",
    "distinct_reference_addresses",
    "distinct_raw_words",
    "status",
    "provenance",
    "original_semantics",
)


def _address(value: int | None) -> str:
    return "" if value is None else f"0x{value & 0xFFFFFFFF:08X}"


def _as_int(value: str | None) -> int | None:
    if not value:
        return None
    try:
        return int(value, 0)
    except ValueError:
        return None


def reference_address(row: dict[str, str]) -> tuple[int | None, str]:
    """Return only statically readable base-less memory references."""

    mode = row.get("addressing_mode", "")
    base = row.get("memory_base_register", "")
    index = row.get("memory_index_register", "")
    displacement = _as_int(row.get("memory_displacement"))
    if displacement is None or base:
        return None, "not-applicable-nonabsolute"
    if mode == "absolute-memory":
        return displacement & 0xFFFFFFFF, "absolute-memory-word"
    if mode == "index" and index:
        return displacement & 0xFFFFFFFF, "indexed-table-base-first-word"
    return None, "not-applicable-nonabsolute"


def section_for_address(result: dict, address: int) -> dict | None:
    for section in result["sections"]:
        start = section["virtual_address"]
        end = start + max(section["virtual_size"], section["file_size"])
        if start <= address < end:
            return section
    return None


def read_reference_word(data: bytes, result: dict, address: int) -> tuple[int | None, int | None, dict | None]:
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


def write_tsv(path: Path, fields: tuple[str, ...], rows: Iterable[dict[str, str]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=fields, delimiter="\t", lineterminator="\n")
        writer.writeheader()
        writer.writerows({field: row.get(field, "") for field in fields} for row in rows)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("image", type=Path)
    parser.add_argument("indirect_report", type=Path)
    parser.add_argument("--internal-path", default="default.xbe")
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()

    data = load_xbe(args.image, args.internal_path)
    result = analyze_xbe(args.image, args.internal_path)
    indirect_rows = read_tsv(args.indirect_report)
    report_rows: list[dict[str, str]] = []
    reference_addresses: set[int] = set()
    raw_words: set[int] = set()
    absolute_rows = 0
    indexed_rows = 0
    valid_reads = 0
    executable_words = 0

    for row in indirect_rows:
        address, read_kind = reference_address(row)
        if address is None:
            continue
        if read_kind == "absolute-memory-word":
            absolute_rows += 1
        else:
            indexed_rows += 1
        reference_addresses.add(address)
        raw_word, file_offset, reference_section = read_reference_word(data, result, address)
        word_section = section_for_address(result, raw_word) if raw_word is not None else None
        if raw_word is not None:
            valid_reads += 1
            raw_words.add(raw_word)
            if word_section is not None and word_section["executable"]:
                executable_words += 1
        report_rows.append(
            {
                "source_target_virtual_address": row.get("source_target_virtual_address", ""),
                "instruction_virtual_address": row.get("instruction_virtual_address", ""),
                "operand_string": row.get("operand_string", ""),
                "addressing_mode": row.get("addressing_mode", ""),
                "memory_base_register": row.get("memory_base_register", ""),
                "memory_index_register": row.get("memory_index_register", ""),
                "memory_scale": row.get("memory_scale", ""),
                "memory_displacement": row.get("memory_displacement", ""),
                "memory_reference_virtual_address": _address(address),
                "memory_reference_file_offset": _address(file_offset),
                "raw_word": _address(raw_word),
                "raw_word_section": word_section["name"] if word_section else "",
                "raw_word_is_executable": str(bool(word_section and word_section["executable"])),
                "reference_read_kind": read_kind,
                "resolution_status": "bounded-image-word-readable" if raw_word is not None else "reference-unreadable",
                "status": "verified-static-render-indirect-reference-word",
                "original_semantics": "not-claimed",
            }
        )

    summary = {
        "image": str(args.image),
        "internal_path": args.internal_path,
        "xbe_sha256": hashlib.sha256(data).hexdigest().upper(),
        "xbe_size": str(len(data)),
        "selection_policy": "bounded four-byte reads at base-less absolute indirect render references; no target dereference",
        "indirect_rows": str(len(indirect_rows)),
        "reference_rows": str(len(report_rows)),
        "absolute_memory_rows": str(absolute_rows),
        "indexed_table_base_rows": str(indexed_rows),
        "bounded_reference_reads": str(len(report_rows)),
        "reference_words_valid": str(valid_reads),
        "reference_words_in_executable_sections": str(executable_words),
        "distinct_reference_addresses": str(len(reference_addresses)),
        "distinct_raw_words": str(len(raw_words)),
        "status": "verified-static-render-indirect-reference-words",
        "provenance": "same-ISO-retail-XBE+bounded-read-at-observed-indirect-memory-references;no-execution;no-call;no-binding",
        "original_semantics": "not-claimed",
    }
    write_tsv(args.report, REPORT_FIELDS, report_rows)
    write_tsv(args.summary, SUMMARY_FIELDS, [summary])
    print("FIFA Street 2 PC bounded render indirect-reference word audit")
    for key in SUMMARY_FIELDS:
        print(f"{key}: {summary[key]}")
    print(f"report: {args.report}")
    print(f"summary: {args.summary}")
    print("note: raw words are candidate address observations only; no word was called or assigned semantics")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
