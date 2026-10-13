"""Inspect bounded code slices at words found by indirect render references.

The preceding reference-word audit reads four-byte values at base-less
absolute memory operands and an indexed-table base.  This audit only
disassembles a bounded executable-section window beginning at each distinct
raw word from that report.  It does not execute the word, recover a function
boundary, follow calls, or assign rendering, material, UV, skinning, rigging,
animation, morph or GPU meaning.
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


REPORT_FIELDS = (
    "candidate_target_virtual_address",
    "reference_row_count",
    "reference_addresses",
    "source_instruction_addresses",
    "raw_word",
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
    "reference_rows",
    "distinct_raw_words",
    "executable_raw_words",
    "disassembled_candidate_words",
    "ret_terminated_candidate_words",
    "prologue_like_candidate_words",
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


def collect_reference_words(rows: list[dict[str, str]]) -> dict[int, dict[str, set[int] | int]]:
    """Group raw words without interpreting them as callable targets."""

    words: dict[int, dict[str, set[int] | int]] = {}
    for row in rows:
        raw_word = _as_int(row.get("raw_word"))
        if raw_word is None:
            continue
        entry = words.setdefault(
            raw_word,
            {
                "reference_addresses": set(),
                "source_instruction_addresses": set(),
                "reference_row_count": 0,
            },
        )
        reference_address = _as_int(row.get("memory_reference_virtual_address"))
        instruction_address = _as_int(row.get("instruction_virtual_address"))
        if reference_address is not None:
            entry["reference_addresses"].add(reference_address)
        if instruction_address is not None:
            entry["source_instruction_addresses"].add(instruction_address)
        entry["reference_row_count"] += 1
    return words


def _address(value: int | None) -> str:
    return "" if value is None else f"0x{value & 0xFFFFFFFF:08X}"


def inspect_reference_word(
    data: bytes,
    result: dict,
    raw_word: int,
    metadata: dict[str, set[int] | int],
    max_bytes: int,
) -> dict[str, str]:
    section = section_for_address(result, raw_word)
    base_row = {
        "candidate_target_virtual_address": _address(raw_word),
        "reference_row_count": str(metadata["reference_row_count"]),
        "reference_addresses": "|".join(
            _address(value) for value in sorted(metadata["reference_addresses"])
        ),
        "source_instruction_addresses": "|".join(
            _address(value) for value in sorted(metadata["source_instruction_addresses"])
        ),
        "raw_word": _address(raw_word),
        "section": section["name"] if section else "",
        "file_offset": "",
        "available_bytes": "0",
        "instruction_count": "0",
        "ret_found": "False",
        "prologue_shape": "none",
        "status": "not-mapped-executable",
        "original_semantics": "not-claimed",
    }
    if section is None or not section["executable"]:
        return base_row
    try:
        decoded_section, instructions = decode_window(data, result, raw_word, max_bytes)
    except (StopIteration, ValueError):
        base_row["status"] = "mapped-executable-undecodable-window"
        return base_row
    offset = virtual_to_file_offset(
        raw_word,
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
            "status": "verified-static-render-indirect-reference-word-slice",
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
    parser.add_argument("reference_report", type=Path)
    parser.add_argument("--internal-path", default="default.xbe")
    parser.add_argument("--max-bytes", type=int, default=256)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()
    if args.max_bytes <= 0:
        raise SystemExit("o limite de bytes deve ser positivo")

    data = load_xbe(args.image, args.internal_path)
    result = analyze_xbe(args.image, args.internal_path)
    reference_rows = read_tsv(args.reference_report)
    words = collect_reference_words(reference_rows)
    report_rows = [
        inspect_reference_word(data, result, raw_word, metadata, args.max_bytes)
        for raw_word, metadata in sorted(words.items())
    ]
    summary = {
        "image": str(args.image),
        "internal_path": args.internal_path,
        "xbe_sha256": hashlib.sha256(data).hexdigest().upper(),
        "xbe_size": str(len(data)),
        "selection_policy": f"bounded executable-section disassembly at raw words from reference report; max_bytes={args.max_bytes}; no execution",
        "reference_rows": str(len(reference_rows)),
        "distinct_raw_words": str(len(words)),
        "executable_raw_words": str(sum(bool(row["section"]) and row["status"] != "not-mapped-executable" for row in report_rows)),
        "disassembled_candidate_words": str(sum(int(row["instruction_count"]) > 0 for row in report_rows)),
        "ret_terminated_candidate_words": str(sum(row["ret_found"] == "True" for row in report_rows)),
        "prologue_like_candidate_words": str(sum(row["prologue_shape"] != "none" for row in report_rows)),
        "status": "verified-static-render-indirect-reference-word-slices",
        "provenance": "same-ISO-retail-XBE+bounded-disassembly-of-raw-words-from-reference-audit;no-execution;no-call;no-function-boundary-claim;no-binding",
        "original_semantics": "not-claimed",
    }
    write_tsv(args.report, REPORT_FIELDS, report_rows)
    write_tsv(args.summary, SUMMARY_FIELDS, [summary])
    print("FIFA Street 2 PC bounded indirect-reference word-slice audit")
    for key in SUMMARY_FIELDS:
        print(f"{key}: {summary[key]}")
    print(f"report: {args.report}")
    print(f"summary: {args.summary}")
    print("note: slices are candidate static observations only; no word was executed or assigned semantics")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
