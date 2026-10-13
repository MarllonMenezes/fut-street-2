"""Independently bind observed EAGL command IDs to the static XBE dispatch table.

The control verifies the retail-XBE instruction slice that extracts the high
16-bit command index, performs the indexed indirect call, and advances the
stream by the low 16-bit word count. It then joins only complete original EAGL
command streams to the executable table prefix. Handler meaning and graphics
semantics are deliberately not inferred.
"""

from __future__ import annotations

import argparse
import csv
import hashlib
import struct
from collections import Counter, defaultdict
from pathlib import Path
from typing import Iterable

try:
    from capstone.x86_const import (
        X86_OP_IMM,
        X86_OP_MEM,
        X86_OP_REG,
        X86_REG_EAX,
        X86_REG_ECX,
        X86_REG_ESI,
        X86_REG_INVALID,
    )
except ImportError as exc:  # pragma: no cover
    raise SystemExit("Capstone não está instalado no ambiente de análise.") from exc

from parse_xbe import analyze_xbe, virtual_to_file_offset
from xbe_render_cfg_audit import decode_window, load_xbe, read_tsv
from xbe_render_indirect_reference_audit import section_for_address


EXPECTED_XBE_SHA256 = "5FABB5B763BE0CC8823E814EE343AC5BE6950305330F46C5170E08EAADEA910B"
EXPECTED_XBE_SIZE = 3_293_184
DISPATCH_SITE = 0x00197080
DISPATCH_TABLE_BASE = 0x0031A930
COMMAND_CURSOR_GLOBAL = 0x003458D4
MAX_TABLE_ENTRIES = 256

STATUS = "verified-static-eagl-command-id-dispatch-control"
DISPATCH_BINDING = "verified-static-command-header-index-to-handler-table-slot"
SEMANTIC_BINDING = "not-proven"
ORIGINAL_SEMANTICS = "not-claimed"
PROVENANCE = (
    "same-ISO-retail-XBE+complete-original-EAGL-command-streams+bounded-static-"
    "decoder-and-table;no-execution;no-function-boundary-claim;no-semantic-binding"
)

REPORT_FIELDS = (
    "command_id",
    "command_occurrences",
    "primitive_rows",
    "dispatch_table_index",
    "dispatch_table_virtual_address",
    "handler_virtual_address",
    "handler_section",
    "handler_executable",
    "handler_first_16_bytes_sha256",
    "dispatch_binding",
    "semantic_binding",
    "original_semantics",
)

SUMMARY_FIELDS = (
    "image",
    "internal_path",
    "xbe_sha256",
    "xbe_size",
    "eagl_report_sha256",
    "eagl_rows",
    "complete_command_stream_rows",
    "command_occurrences",
    "unique_observed_command_ids",
    "mapped_observed_command_ids",
    "executable_dispatch_table_prefix_entries",
    "distinct_dispatch_targets",
    "dispatch_table_base",
    "dispatch_table_file_offset",
    "first_non_executable_index",
    "first_non_executable_word",
    "handler_table_scan_capped",
    "dispatcher_instruction_addresses_validated",
    "status",
    "provenance",
    "dispatch_binding",
    "semantic_binding",
    "original_semantics",
)


def _write_tsv(path: Path, fields: Iterable[str], rows: Iterable[dict[str, str]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(
            handle,
            fieldnames=list(fields),
            delimiter="\t",
            lineterminator="\n",
        )
        writer.writeheader()
        writer.writerows(rows)


def _require_register(instruction, operand_index: int, register: int) -> None:
    operand = instruction.operands[operand_index]
    if operand.type != X86_OP_REG or operand.reg != register:
        raise ValueError(
            f"unexpected operand at 0x{instruction.address:08X}: "
            f"{instruction.mnemonic} {instruction.op_str}"
        )


def _require_immediate(instruction, operand_index: int, value: int) -> None:
    operand = instruction.operands[operand_index]
    if operand.type != X86_OP_IMM or (operand.imm & 0xFFFFFFFF) != value:
        raise ValueError(
            f"unexpected immediate at 0x{instruction.address:08X}: "
            f"{instruction.mnemonic} {instruction.op_str}"
        )


def _require_absolute_memory(instruction, operand_index: int, address: int) -> None:
    operand = instruction.operands[operand_index]
    if operand.type != X86_OP_MEM:
        raise ValueError(f"expected memory operand at 0x{instruction.address:08X}")
    memory = operand.mem
    if (
        memory.base != X86_REG_INVALID
        or memory.index != X86_REG_INVALID
        or memory.disp != address
    ):
        raise ValueError(
            f"unexpected absolute address at 0x{instruction.address:08X}: "
            f"{instruction.mnemonic} {instruction.op_str}"
        )


def _validate_dispatch_slice(data: bytes, xbe: dict) -> int:
    _, decoded = decode_window(data, xbe, DISPATCH_SITE, 0x40)
    by_address = {instruction.address: instruction for instruction in decoded}
    expected = {
        0x00197080: "mov",
        0x00197082: "shr",
        0x00197085: "and",
        0x0019708B: "call",
        0x00197092: "mov",
        0x00197098: "lea",
        0x0019709B: "mov",
    }
    if any(
        address not in by_address or by_address[address].mnemonic.lower() != mnemonic
        for address, mnemonic in expected.items()
    ):
        raise ValueError("retail XBE does not match the bounded EAGL dispatch instruction slice")

    _require_register(by_address[0x00197080], 0, X86_REG_ESI)
    _require_register(by_address[0x00197080], 1, X86_REG_EAX)

    _require_register(by_address[0x00197082], 0, X86_REG_EAX)
    _require_immediate(by_address[0x00197082], 1, 0x10)

    _require_register(by_address[0x00197085], 0, X86_REG_ESI)
    _require_immediate(by_address[0x00197085], 1, 0xFFFF)

    dispatch_call = by_address[0x0019708B]
    call_operand = dispatch_call.operands[0]
    if call_operand.type != X86_OP_MEM:
        raise ValueError("indexed dispatcher call is not an indirect memory call")
    call_memory = call_operand.mem
    if (
        call_memory.base != X86_REG_INVALID
        or call_memory.index != X86_REG_EAX
        or call_memory.scale != 4
        or call_memory.disp != DISPATCH_TABLE_BASE
    ):
        raise ValueError("indexed dispatcher call no longer references the expected table base/index")

    _require_register(by_address[0x00197092], 0, X86_REG_ECX)
    _require_absolute_memory(by_address[0x00197092], 1, COMMAND_CURSOR_GLOBAL)

    pointer_advance = by_address[0x00197098]
    _require_register(pointer_advance, 0, X86_REG_EAX)
    advance_operand = pointer_advance.operands[1]
    if advance_operand.type != X86_OP_MEM:
        raise ValueError("command cursor advance is not a memory-based LEA")
    advance_memory = advance_operand.mem
    if (
        advance_memory.base != X86_REG_ECX
        or advance_memory.index != X86_REG_ESI
        or advance_memory.scale != 4
        or advance_memory.disp != 0
    ):
        raise ValueError("command cursor advance no longer scales the low-16-bit field by four")

    _require_absolute_memory(by_address[0x0019709B], 0, COMMAND_CURSOR_GLOBAL)
    _require_register(by_address[0x0019709B], 1, X86_REG_EAX)
    return len(expected)


def _read_dispatch_table(data: bytes, xbe: dict) -> tuple[int, dict[int, tuple[int, str, bytes]], int, int, int]:
    table_section = section_for_address(xbe, DISPATCH_TABLE_BASE)
    table_offset = virtual_to_file_offset(
        DISPATCH_TABLE_BASE,
        xbe["fields"]["base_address"],
        len(data),
        xbe["sections"],
    )
    if table_section is None or table_offset is None:
        raise ValueError("dispatch table base is not mapped to the retail XBE")

    entries: dict[int, tuple[int, str, bytes]] = {}
    boundary_index = -1
    boundary_word = 0
    for index in range(MAX_TABLE_ENTRIES):
        virtual_address = DISPATCH_TABLE_BASE + index * 4
        raw_offset = virtual_to_file_offset(
            virtual_address,
            xbe["fields"]["base_address"],
            len(data),
            xbe["sections"],
        )
        if raw_offset is None or raw_offset + 4 > len(data):
            raise ValueError(f"dispatch table word {index} is not readable")
        target = struct.unpack_from("<I", data, raw_offset)[0]
        target_section = section_for_address(xbe, target)
        if target_section is None or not target_section["executable"]:
            boundary_index = index
            boundary_word = target
            break

        target_offset = virtual_to_file_offset(
            target,
            xbe["fields"]["base_address"],
            len(data),
            xbe["sections"],
        )
        if target_offset is None or target_offset + 16 > len(data):
            raise ValueError(f"dispatch target {index} has no bounded 16-byte raw window")
        entries[index] = (
            target,
            target_section["name"],
            data[target_offset : target_offset + 16],
        )

    if boundary_index < 0:
        raise ValueError(f"dispatch-table scan reached its {MAX_TABLE_ENTRIES}-entry cap")
    return boundary_index, entries, boundary_word, table_offset, len({item[0] for item in entries.values()})


def _parse_commands(path: Path) -> tuple[list[dict[str, str]], Counter[int], dict[int, set[tuple[str, str, str, str]]], int]:
    rows = read_tsv(path)
    required = {
        "model_archive",
        "model_object",
        "layer",
        "primitive",
        "command_count",
        "command_stream_complete",
        "status",
        "original_semantics",
        "command_ids",
    }
    if not rows or not required.issubset(rows[0]):
        raise ValueError("original EAGL render-chain TSV is empty or lacks command_ids fields")

    occurrences: Counter[int] = Counter()
    primitive_rows: dict[int, set[tuple[str, str, str, str]]] = defaultdict(set)
    identities: set[tuple[str, str, str, str]] = set()
    complete_rows = 0
    for row in rows:
        identity = (
            row["model_archive"],
            row["model_object"],
            row["layer"],
            row["primitive"],
        )
        if identity in identities:
            raise ValueError(f"duplicate original primitive identity: {identity}")
        identities.add(identity)
        if (
            row["command_stream_complete"].lower() != "true"
            or row["status"] != "verified-bounded-original-render-chain-structure"
            or row["original_semantics"] != ORIGINAL_SEMANTICS
        ):
            raise ValueError(f"incomplete or semantically promoted EAGL row: {identity}")
        complete_rows += 1

        tokens = row["command_ids"].split(",") if row["command_ids"] else []
        if not tokens or len(tokens) != int(row["command_count"]):
            raise ValueError(f"command ID cardinality mismatch: {identity}")
        row_ids: set[int] = set()
        for token in tokens:
            if not token.isdecimal():
                raise ValueError(f"non-decimal command ID {token!r} in {identity}")
            command_id = int(token, 10)
            if not 0 <= command_id <= 0xFFFF:
                raise ValueError(f"command ID outside uint16 range in {identity}")
            occurrences[command_id] += 1
            row_ids.add(command_id)
        for command_id in row_ids:
            primitive_rows[command_id].add(identity)
    return rows, occurrences, primitive_rows, complete_rows


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("image", type=Path)
    parser.add_argument("command_report", type=Path)
    parser.add_argument("--internal-path", default="default.xbe")
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()

    xbe_data = load_xbe(args.image, args.internal_path)
    xbe_sha256 = hashlib.sha256(xbe_data).hexdigest().upper()
    if len(xbe_data) != EXPECTED_XBE_SIZE or xbe_sha256 != EXPECTED_XBE_SHA256:
        raise ValueError("XBE size/hash differs from the pinned FIFA Street 2 retail input")
    xbe = analyze_xbe(args.image, args.internal_path)
    validated_instruction_count = _validate_dispatch_slice(xbe_data, xbe)
    boundary_index, table_entries, boundary_word, table_offset, distinct_targets = _read_dispatch_table(
        xbe_data,
        xbe,
    )

    render_rows, occurrences, primitive_rows, complete_rows = _parse_commands(args.command_report)
    if complete_rows != len(render_rows):
        raise ValueError("not every original EAGL command stream is complete")
    unmapped_ids = sorted(set(occurrences) - set(table_entries))
    if unmapped_ids:
        raise ValueError(f"observed EAGL command IDs have no executable table slot: {unmapped_ids}")

    report_rows = []
    for command_id in sorted(occurrences):
        target, section_name, target_window = table_entries[command_id]
        report_rows.append(
            {
                "command_id": str(command_id),
                "command_occurrences": str(occurrences[command_id]),
                "primitive_rows": str(len(primitive_rows[command_id])),
                "dispatch_table_index": str(command_id),
                "dispatch_table_virtual_address": f"0x{DISPATCH_TABLE_BASE + command_id * 4:08X}",
                "handler_virtual_address": f"0x{target:08X}",
                "handler_section": section_name,
                "handler_executable": "True",
                "handler_first_16_bytes_sha256": hashlib.sha256(target_window).hexdigest().upper(),
                "dispatch_binding": DISPATCH_BINDING,
                "semantic_binding": SEMANTIC_BINDING,
                "original_semantics": ORIGINAL_SEMANTICS,
            }
        )

    summary = {
        "image": str(args.image),
        "internal_path": args.internal_path,
        "xbe_sha256": xbe_sha256,
        "xbe_size": str(len(xbe_data)),
        "eagl_report_sha256": hashlib.sha256(args.command_report.read_bytes()).hexdigest().upper(),
        "eagl_rows": str(len(render_rows)),
        "complete_command_stream_rows": str(complete_rows),
        "command_occurrences": str(sum(occurrences.values())),
        "unique_observed_command_ids": str(len(occurrences)),
        "mapped_observed_command_ids": str(len(occurrences)),
        "executable_dispatch_table_prefix_entries": str(boundary_index),
        "distinct_dispatch_targets": str(distinct_targets),
        "dispatch_table_base": f"0x{DISPATCH_TABLE_BASE:08X}",
        "dispatch_table_file_offset": f"0x{table_offset:08X}",
        "first_non_executable_index": str(boundary_index),
        "first_non_executable_word": f"0x{boundary_word:08X}",
        "handler_table_scan_capped": "False",
        "dispatcher_instruction_addresses_validated": str(validated_instruction_count),
        "status": STATUS,
        "provenance": PROVENANCE,
        "dispatch_binding": DISPATCH_BINDING,
        "semantic_binding": SEMANTIC_BINDING,
        "original_semantics": ORIGINAL_SEMANTICS,
    }

    _write_tsv(args.report, REPORT_FIELDS, report_rows)
    _write_tsv(args.summary, SUMMARY_FIELDS, [summary])
    print(f"status: {STATUS}")
    print(f"xbe_sha256: {xbe_sha256}")
    print(f"dispatcher_instructions_validated: {validated_instruction_count}")
    print(f"executable_table_prefix_entries: {boundary_index}")
    print(f"first_non_executable_entry: index={boundary_index} value=0x{boundary_word:08X}")
    print(f"eagl_rows_complete: {complete_rows}/{len(render_rows)}")
    print(f"command_occurrences: {sum(occurrences.values())}")
    print(f"unique_observed_command_ids: {len(occurrences)}")
    print(f"mapped_observed_command_ids: {len(occurrences)}/{len(occurrences)}")
    print(f"dispatch_binding: {DISPATCH_BINDING}")
    print(f"semantic_binding: {SEMANTIC_BINDING}")
    print(f"original_semantics: {ORIGINAL_SEMANTICS}")
    print(f"report: {args.report}")
    print(f"summary: {args.summary}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
