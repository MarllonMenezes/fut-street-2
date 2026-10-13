"""Inventory indirect control transfers in bounded render CFG slices.

The audit revisits the already selected render targets and records only the
reachable x86 ``call``/jump instructions whose target is not an immediate
address. Register and memory operands are preserved as observed; the audit
does not dereference them, recover function boundaries, execute the XBE, or
assign mesh/material/UV/skinning/rigging/animation/morph/GPU semantics.
"""

from __future__ import annotations

import argparse
import csv
import hashlib
from pathlib import Path
from typing import Iterable

from capstone.x86_const import X86_OP_IMM, X86_OP_MEM, X86_OP_REG

from parse_xbe import analyze_xbe, virtual_to_file_offset
from xbe_render_cfg_audit import decode_window, load_xbe, read_tsv, recover_cfg


REPORT_FIELDS = (
    "source_target_virtual_address",
    "instruction_virtual_address",
    "section",
    "file_offset",
    "mnemonic",
    "operand_string",
    "operand_kind",
    "addressing_mode",
    "memory_base_register",
    "memory_index_register",
    "memory_scale",
    "memory_displacement",
    "instruction_bytes",
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
    "analyzed_targets",
    "targets_with_indirect_transfers",
    "total_indirect_transfers",
    "indirect_calls",
    "indirect_jumps",
    "register_operands",
    "memory_operands",
    "absolute_memory_operands",
    "distinct_operand_shapes",
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


def _signed_hex(value: int) -> str:
    return f"-0x{-value:X}" if value < 0 else f"0x{value:X}"


def _operand_details(instruction) -> tuple[str, str, str, str, str, str, str]:
    """Return kind/mode and raw x86 memory components without dereferencing."""

    if not instruction.operands:
        return "unknown", "unknown", "", "", "", "", ""
    operand = instruction.operands[0]
    if operand.type == X86_OP_REG:
        return "register", "register", instruction.reg_name(operand.reg), "", "", "", ""
    if operand.type == X86_OP_MEM:
        memory = operand.mem
        base = instruction.reg_name(memory.base) if memory.base else ""
        index = instruction.reg_name(memory.index) if memory.index else ""
        if base or index:
            mode = "base-index" if base and index else "base" if base else "index"
        else:
            mode = "absolute-memory"
        return (
            "memory",
            mode,
            base,
            index,
            str(memory.scale),
            _signed_hex(memory.disp),
            f"{base}|{index}|{memory.scale}|{memory.disp}",
        )
    if operand.type == X86_OP_IMM:
        return "immediate", "immediate", "", "", "", "", ""
    return "other", "other", "", "", "", "", ""


def classify_indirect_transfers(instructions: list, reachable: set[int]) -> list[dict[str, str]]:
    """Extract reachable indirect transfers from a decoded bounded window."""

    rows: list[dict[str, str]] = []
    for index in sorted(reachable):
        instruction = instructions[index]
        mnemonic = instruction.mnemonic.lower()
        if mnemonic != "call" and not mnemonic.startswith("j"):
            continue
        if not instruction.operands or instruction.operands[0].type == X86_OP_IMM:
            continue
        kind, mode, base, index_register, scale, displacement, shape = _operand_details(instruction)
        rows.append(
            {
                "instruction": instruction,
                "mnemonic": mnemonic,
                "operand_kind": kind,
                "addressing_mode": mode,
                "memory_base_register": base,
                "memory_index_register": index_register,
                "memory_scale": scale,
                "memory_displacement": displacement,
                "operand_shape": shape or f"{kind}|{mode}",
            }
        )
    return rows


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
    parser.add_argument("--internal-path", default="default.xbe")
    parser.add_argument("--max-bytes", type=int, default=2048)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()
    if args.max_bytes <= 0:
        raise SystemExit("max-bytes deve ser positivo")

    data = load_xbe(args.image, args.internal_path)
    analysis = analyze_xbe(args.image, args.internal_path)
    target_rows = read_tsv(args.cfg_report)
    report_rows: list[dict[str, str]] = []
    targets_with_indirect: set[int] = set()
    target_count = 0
    indirect_calls = 0
    indirect_jumps = 0
    register_operands = 0
    memory_operands = 0
    absolute_memory_operands = 0
    operand_shapes: set[str] = set()

    for target_row in target_rows:
        target = _as_int(target_row.get("target_virtual_address"))
        if target is None:
            continue
        target_count += 1
        try:
            section, instructions = decode_window(data, analysis, target, args.max_bytes)
        except (StopIteration, ValueError):
            continue
        cfg = recover_cfg(instructions, target)
        indirect_rows = classify_indirect_transfers(instructions, cfg["reachable"])
        if indirect_rows:
            targets_with_indirect.add(target)
        for item in indirect_rows:
            instruction = item["instruction"]
            kind = item["operand_kind"]
            mode = item["addressing_mode"]
            operand_shapes.add(item["operand_shape"])
            if instruction.mnemonic.lower() == "call":
                indirect_calls += 1
            else:
                indirect_jumps += 1
            if kind == "register":
                register_operands += 1
            elif kind == "memory":
                memory_operands += 1
                if mode == "absolute-memory":
                    absolute_memory_operands += 1
            file_offset = virtual_to_file_offset(
                instruction.address,
                analysis["fields"]["base_address"],
                len(data),
                analysis["sections"],
            )
            report_rows.append(
                {
                    "source_target_virtual_address": _address(target),
                    "instruction_virtual_address": _address(instruction.address),
                    "section": section["name"],
                    "file_offset": _address(file_offset),
                    "mnemonic": instruction.mnemonic,
                    "operand_string": instruction.op_str,
                    "operand_kind": kind,
                    "addressing_mode": mode,
                    "memory_base_register": item["memory_base_register"],
                    "memory_index_register": item["memory_index_register"],
                    "memory_scale": item["memory_scale"],
                    "memory_displacement": item["memory_displacement"],
                    "instruction_bytes": instruction.bytes.hex().upper(),
                    "status": "verified-bounded-static-indirect-transfer",
                    "original_semantics": "not-claimed",
                }
            )

    summary = {
        "image": str(args.image),
        "internal_path": args.internal_path,
        "xbe_sha256": hashlib.sha256(data).hexdigest().upper(),
        "xbe_size": str(len(data)),
        "selection_policy": "reachable indirect call/jump instructions in 64 bounded render CFG windows",
        "candidate_targets": str(len(target_rows)),
        "analyzed_targets": str(target_count),
        "targets_with_indirect_transfers": str(len(targets_with_indirect)),
        "total_indirect_transfers": str(len(report_rows)),
        "indirect_calls": str(indirect_calls),
        "indirect_jumps": str(indirect_jumps),
        "register_operands": str(register_operands),
        "memory_operands": str(memory_operands),
        "absolute_memory_operands": str(absolute_memory_operands),
        "distinct_operand_shapes": str(len(operand_shapes)),
        "status": "verified-static-render-indirect-transfer-inventory",
        "provenance": "same-ISO-retail-XBE+bounded-x86-disassembly-of-reachable-render-CFG;no-execution;no-dereference;no-binding",
        "original_semantics": "not-claimed",
    }
    write_tsv(args.report, REPORT_FIELDS, report_rows)
    write_tsv(args.summary, SUMMARY_FIELDS, [summary])
    print("FIFA Street 2 PC bounded render indirect-transfer audit")
    for key in SUMMARY_FIELDS:
        print(f"{key}: {summary[key]}")
    print(f"report: {args.report}")
    print(f"summary: {args.summary}")
    print("note: indirect operands are preserved as raw static observations; no target was dereferenced or assigned semantics")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
