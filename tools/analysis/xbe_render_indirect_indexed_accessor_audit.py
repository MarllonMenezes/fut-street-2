"""Audit bounded structural shapes of entries from an indexed XBE table.

The indexed-table scan identifies executable words, but it does not record the
small instruction shape at each entry.  This companion audit records only the
observed instruction sequence up to the first bounded ``ret`` and immediate
operand text.  It is useful for distinguishing repeated accessors, fallback
stubs and call wrappers from larger code slices.

No entry is executed, no function boundary is asserted, and no field,
render, material, UV, skinning, rigging, animation, morph or GPU semantics are
assigned.  Names such as ``field`` in the report mean only an observed memory
operand displacement.
"""

from __future__ import annotations

import argparse
import csv
import hashlib
import re
from collections import Counter
from pathlib import Path
from typing import Iterable

from parse_xbe import analyze_xbe, virtual_to_file_offset
from xbe_render_cfg_audit import decode_window, load_xbe, read_tsv
from xbe_render_indirect_indexed_target_audit import collect_indexed_targets
from xbe_render_indirect_reference_audit import section_for_address


STATUS = "verified-bounded-static-render-indirect-indexed-accessor-shapes"
ORIGINAL_SEMANTICS = "not-claimed"

REPORT_FIELDS = (
    "candidate_target_virtual_address",
    "table_entry_indices",
    "entry_occurrence_count",
    "section",
    "file_offset",
    "available_bytes",
    "instruction_count_to_first_ret",
    "ret_found",
    "mnemonic_shape",
    "first_instruction",
    "first_memory_operand",
    "observed_memory_displacements",
    "direct_call_targets",
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
    "fallback_pop_pop_ret_shapes",
    "single_mov_memory_ret_shapes",
    "lea_push_call_ret_shapes",
    "other_shapes",
    "distinct_memory_displacements",
    "distinct_direct_call_targets",
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


def _first_ret_index(instructions: list[object]) -> int | None:
    for index, instruction in enumerate(instructions):
        if instruction.mnemonic.lower() in {"ret", "retf"}:
            return index
    return None


def _instruction_text(instruction: object) -> str:
    return f"0x{instruction.address:08X}:{instruction.mnemonic} {instruction.op_str}".strip()


def _memory_displacements(instructions: list[object]) -> list[str]:
    values: list[str] = []
    for instruction in instructions:
        for match in re.finditer(r"(?:\+\s*)?0x([0-9a-f]+)", instruction.op_str.lower()):
            values.append(f"0x{int(match.group(1), 16):08X}")
    return sorted(set(values))


def _direct_call_targets(instructions: list[object]) -> list[str]:
    values: list[str] = []
    for instruction in instructions:
        if instruction.mnemonic.lower() not in {"call", "jmp"}:
            continue
        operand = instruction.op_str.strip().lower()
        if operand.startswith("0x"):
            try:
                values.append(_address(int(operand, 16)))
            except ValueError:
                pass
    return sorted(set(values))


def _shape(mnemonics: list[str]) -> str:
    sequence = [mnemonic.lower() for mnemonic in mnemonics]
    if sequence[:3] == ["pop", "pop", "ret"]:
        return "fallback-pop-pop-ret"
    if sequence[:2] == ["mov", "pop"] and sequence[-1:] == ["ret"]:
        return "single-mov-memory-ret"
    if sequence[:5] == ["lea", "lea", "push", "push", "call"] and sequence[-1:] == ["ret"]:
        return "lea-lea-push-push-call-ret"
    return "other"


def inspect_target(
    data: bytes,
    result: dict,
    target: int,
    metadata: dict[str, object],
    max_bytes: int,
) -> dict[str, str]:
    row = {
        "candidate_target_virtual_address": _address(target),
        "table_entry_indices": "|".join(str(value) for value in sorted(metadata["table_entry_indices"])),
        "entry_occurrence_count": str(metadata["entry_occurrence_count"]),
        "section": "",
        "file_offset": "",
        "available_bytes": "0",
        "instruction_count_to_first_ret": "0",
        "ret_found": "False",
        "mnemonic_shape": "undecodable",
        "first_instruction": "",
        "first_memory_operand": "",
        "observed_memory_displacements": "",
        "direct_call_targets": "",
        "status": "not-mapped-executable",
        "original_semantics": ORIGINAL_SEMANTICS,
    }
    section = section_for_address(result, target)
    if section is None or not section["executable"]:
        return row
    try:
        decoded_section, instructions = decode_window(data, result, target, max_bytes)
    except (StopIteration, ValueError):
        row["status"] = "mapped-executable-undecodable-window"
        return row
    offset = virtual_to_file_offset(
        target,
        result["fields"]["base_address"],
        len(data),
        result["sections"],
    )
    if offset is None:
        row["status"] = "mapped-executable-no-file-offset"
        return row
    available = min(max_bytes, decoded_section["file_address"] + decoded_section["file_size"] - offset)
    ret_index = _first_ret_index(instructions)
    observed = instructions if ret_index is None else instructions[: ret_index + 1]
    sequence = [instruction.mnemonic for instruction in observed]
    memory_operands = [
        instruction.op_str
        for instruction in observed
        if "[" in instruction.op_str and "]" in instruction.op_str
    ]
    row.update(
        {
            "section": section["name"],
            "file_offset": _address(offset),
            "available_bytes": str(available),
            "instruction_count_to_first_ret": str(len(observed)),
            "ret_found": str(ret_index is not None),
            "mnemonic_shape": _shape(sequence),
            "first_instruction": _instruction_text(observed[0]) if observed else "",
            "first_memory_operand": memory_operands[0] if memory_operands else "",
            "observed_memory_displacements": "|".join(_memory_displacements(observed)),
            "direct_call_targets": "|".join(_direct_call_targets(observed)),
            "status": STATUS,
        }
    )
    return row


def write_tsv(path: Path, fields: Iterable[str], rows: Iterable[dict[str, str]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=list(fields), delimiter="\t", lineterminator="\n")
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
        raise SystemExit("--max-bytes deve ser positivo")

    data = load_xbe(args.image, args.internal_path)
    result = analyze_xbe(args.image, args.internal_path)
    indexed_rows = read_tsv(args.indexed_table_report)
    collected = collect_indexed_targets(indexed_rows)
    targets = collected["targets"]
    report_rows = [
        inspect_target(data, result, target, metadata, args.max_bytes)
        for target, metadata in sorted(targets.items())
    ]
    shapes = Counter(row["mnemonic_shape"] for row in report_rows)
    memory_displacements = {
        displacement
        for row in report_rows
        for displacement in row["observed_memory_displacements"].split("|")
        if displacement
    }
    direct_targets = {
        target
        for row in report_rows
        for target in row["direct_call_targets"].split("|")
        if target
    }
    summary = {
        "image": str(args.image),
        "internal_path": args.internal_path,
        "xbe_sha256": hashlib.sha256(data).hexdigest().upper(),
        "xbe_size": str(len(data)),
        "selection_policy": f"bounded instruction shape through first observed ret; max_bytes={args.max_bytes}; no execution; no function-boundary claim",
        "indexed_table_rows": str(collected["table_rows"]),
        "candidate_entry_occurrences": str(sum(int(row["entry_occurrence_count"]) for row in report_rows)),
        "distinct_candidate_targets": str(len(targets)),
        "mapped_executable_targets": str(sum(row["status"] == STATUS for row in report_rows)),
        "disassembled_candidate_targets": str(sum(row["instruction_count_to_first_ret"] != "0" for row in report_rows)),
        "ret_terminated_candidate_targets": str(sum(row["ret_found"] == "True" for row in report_rows)),
        "fallback_pop_pop_ret_shapes": str(shapes["fallback-pop-pop-ret"]),
        "single_mov_memory_ret_shapes": str(shapes["single-mov-memory-ret"]),
        "lea_push_call_ret_shapes": str(shapes["lea-lea-push-push-call-ret"]),
        "other_shapes": str(shapes["other"] + shapes["undecodable"]),
        "distinct_memory_displacements": str(len(memory_displacements)),
        "distinct_direct_call_targets": str(len(direct_targets)),
        "status": STATUS,
        "provenance": "same-ISO-retail-XBE+bounded-observation-of-indexed-table-entry-instruction-shapes;no-execution;no-function-boundary-claim;no-binding",
        "original_semantics": ORIGINAL_SEMANTICS,
    }
    write_tsv(args.report, REPORT_FIELDS, report_rows)
    write_tsv(args.summary, SUMMARY_FIELDS, [summary])
    print("FIFA Street 2 PC bounded indexed-table accessor-shape audit")
    for key in SUMMARY_FIELDS:
        print(f"{key}: {summary[key]}")
    print(f"report: {args.report}")
    print(f"summary: {args.summary}")
    print("note: instruction shapes and displacements remain opaque structural evidence; no semantics were assigned")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
