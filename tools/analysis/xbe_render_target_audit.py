"""Disassemble bounded windows for the strongest static render targets.

The target list comes from ``xbe-render-static-call-clusters.tsv``.  This
audit only records conservative x86 instruction-window facts: section mapping,
ret termination, prologue shape, direct/indirect calls, branches and absolute
memory operands.  It does not recover complete functions, execute the XBE,
decode GPU commands, or assign mesh, material, UV, skinning, rigging, morph or
render-state semantics.  The ISO is opened read-only.
"""

from __future__ import annotations

import argparse
import csv
import hashlib
from pathlib import Path
from typing import Iterable

try:
    from capstone import CS_ARCH_X86, CS_MODE_32, Cs
    from capstone.x86_const import X86_OP_IMM, X86_OP_MEM
except ImportError as exc:  # pragma: no cover
    raise SystemExit("Capstone não está instalado no ambiente de análise.") from exc

from list_xdvdfs import XdvdfsReader
from parse_xbe import analyze_xbe, virtual_to_file_offset


TARGET_FIELDS = (
    "target_virtual_address",
    "context_rows",
    "unique_render_literals",
    "render_literals",
    "file_offset",
    "section",
    "available_bytes",
    "instruction_count",
    "ret_found",
    "prologue_shape",
    "direct_call_count",
    "indirect_call_count",
    "conditional_branch_count",
    "absolute_memory_operand_count",
    "distinct_direct_call_targets",
    "first_instruction",
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
    "disassembled_targets",
    "ret_terminated_targets",
    "prologue_like_targets",
    "direct_call_count",
    "indirect_call_count",
    "conditional_branch_count",
    "absolute_memory_operand_count",
    "status",
    "provenance",
    "original_semantics",
)


def read_tsv(path: Path) -> list[dict[str, str]]:
    with path.open("r", encoding="utf-8", newline="") as handle:
        return list(csv.DictReader(handle, delimiter="\t"))


def _as_int(value: str | None) -> int | None:
    if not value:
        return None
    try:
        return int(value, 0)
    except ValueError:
        return None


def select_targets(rows: Iterable[dict[str, str]], limit: int) -> list[dict[str, str]]:
    """Select a stable bounded prefix of the pre-ranked cluster report."""

    selected: list[dict[str, str]] = []
    seen: set[int] = set()
    for row in rows:
        address = _as_int(row.get("call_target_virtual_address"))
        if address is None or address in seen:
            continue
        seen.add(address)
        selected.append(row)
        if len(selected) >= limit:
            break
    return selected


def load_xbe(image: Path, internal_path: str) -> bytes:
    reader = XdvdfsReader(image)
    try:
        entries = {entry.path: entry for entry in reader.entries()}
        entry = entries.get(internal_path)
        if entry is None or entry.is_directory or entry.image_offset is None:
            raise ValueError(f"internal file not found: {internal_path}")
        return reader._read_at(entry.image_offset, entry.size)
    finally:
        reader.close()


def _is_prologue(instructions: list) -> str:
    if not instructions:
        return "none"
    first = [instruction.mnemonic for instruction in instructions[:5]]
    if len(first) >= 2 and first[0] == "push" and first[1] == "mov":
        return "frame-pointer-like"
    if first[0] == "push" and "sub" in first:
        return "callee-save-stack-like"
    return "none"


def disassemble_target(data: bytes, result: dict, row: dict[str, str], max_bytes: int) -> dict[str, str]:
    address = _as_int(row.get("call_target_virtual_address"))
    if address is None:
        raise ValueError("render target has no virtual address")
    base = result["fields"]["base_address"]
    offset = virtual_to_file_offset(address, base, len(data), result["sections"])
    if offset is None:
        raise ValueError(f"target 0x{address:X} does not map to XBE bytes")
    section = next(
        section
        for section in result["sections"]
        if section["executable"] and section["virtual_address"] <= address < section["virtual_address"] + section["virtual_size"]
    )
    available = min(max_bytes, section["file_address"] + section["file_size"] - offset)
    decoder = Cs(CS_ARCH_X86, CS_MODE_32)
    decoder.detail = True
    instructions = []
    ret_found = False
    direct_targets: set[int] = set()
    direct_call_count = 0
    indirect_call_count = 0
    conditional_branch_count = 0
    absolute_memory_operand_count = 0
    for instruction in decoder.disasm(data[offset : offset + available], address):
        instructions.append(instruction)
        if instruction.mnemonic in {"ret", "retf"}:
            ret_found = True
            break
        if instruction.mnemonic == "call" and instruction.operands:
            operand = instruction.operands[0]
            if operand.type == X86_OP_IMM:
                direct_call_count += 1
                direct_targets.add(operand.imm & 0xFFFFFFFF)
            else:
                indirect_call_count += 1
        if instruction.mnemonic.startswith("j") and instruction.mnemonic not in {"jmp", "jrcxz"}:
            conditional_branch_count += 1
        for operand in instruction.operands:
            if operand.type == X86_OP_MEM and operand.mem.base == 0 and operand.mem.index == 0:
                absolute_memory_operand_count += 1

    first_instruction = ""
    if instructions:
        first_instruction = f"{instructions[0].mnemonic} {instructions[0].op_str}".strip()
    return {
        "target_virtual_address": f"0x{address:08X}",
        "context_rows": row.get("context_rows", ""),
        "unique_render_literals": row.get("unique_render_literals", ""),
        "render_literals": row.get("render_literals", ""),
        "file_offset": f"0x{offset:08X}",
        "section": section["name"],
        "available_bytes": str(available),
        "instruction_count": str(len(instructions)),
        "ret_found": str(ret_found),
        "prologue_shape": _is_prologue(instructions),
        "direct_call_count": str(direct_call_count),
        "indirect_call_count": str(indirect_call_count),
        "conditional_branch_count": str(conditional_branch_count),
        "absolute_memory_operand_count": str(absolute_memory_operand_count),
        "distinct_direct_call_targets": str(len(direct_targets)),
        "first_instruction": first_instruction,
        "status": "candidate-static-render-target-window",
        "original_semantics": "not-claimed",
    }


def build_summary(
    image: str,
    internal_path: str,
    xbe_sha256: str,
    xbe_size: int,
    selected_rows: list[dict[str, str]],
    target_rows: list[dict[str, str]],
    max_targets: int,
) -> dict[str, str]:
    return {
        "image": image,
        "internal_path": internal_path,
        "xbe_sha256": xbe_sha256,
        "xbe_size": str(xbe_size),
        "selection_policy": f"top_{max_targets}_render_call_clusters_by_unique_render_literals",
        "candidate_targets": str(len(selected_rows)),
        "disassembled_targets": str(len(target_rows)),
        "ret_terminated_targets": str(sum(row["ret_found"] == "True" for row in target_rows)),
        "prologue_like_targets": str(sum(row["prologue_shape"] != "none" for row in target_rows)),
        "direct_call_count": str(sum(int(row["direct_call_count"]) for row in target_rows)),
        "indirect_call_count": str(sum(int(row["indirect_call_count"]) for row in target_rows)),
        "conditional_branch_count": str(sum(int(row["conditional_branch_count"]) for row in target_rows)),
        "absolute_memory_operand_count": str(sum(int(row["absolute_memory_operand_count"]) for row in target_rows)),
        "status": "verified-static-render-target-windows",
        "provenance": "same-ISO-retail-XBE+bounded-x86-disassembly-of-ranked-render-context; no-execution; no-binding",
        "original_semantics": "not-claimed",
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
    parser.add_argument("cluster_report", type=Path)
    parser.add_argument("--internal-path", default="default.xbe")
    parser.add_argument("--max-targets", type=int, default=64)
    parser.add_argument("--max-bytes", type=int, default=384)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()
    if args.max_targets <= 0 or args.max_bytes <= 0:
        raise SystemExit("--max-targets e --max-bytes devem ser positivos")

    cluster_rows = read_tsv(args.cluster_report)
    selected_rows = select_targets(cluster_rows, args.max_targets)
    result = analyze_xbe(args.image, args.internal_path)
    data = load_xbe(args.image, args.internal_path)
    target_rows = [disassemble_target(data, result, row, args.max_bytes) for row in selected_rows]
    xbe_sha256 = hashlib.sha256(data).hexdigest()
    summary = build_summary(
        str(args.image),
        args.internal_path,
        xbe_sha256,
        len(data),
        selected_rows,
        target_rows,
        args.max_targets,
    )
    write_tsv(args.report, TARGET_FIELDS, target_rows)
    write_tsv(args.summary, SUMMARY_FIELDS, [summary])
    print(
        f"candidate_targets: {summary['candidate_targets']}; "
        f"disassembled_targets: {summary['disassembled_targets']}; "
        f"ret_terminated_targets: {summary['ret_terminated_targets']}; "
        f"prologue_like_targets: {summary['prologue_like_targets']}; "
        f"status: {summary['status']}"
    )
    print(f"report: {args.report}")
    print(f"summary: {args.summary}")
    print("original_semantics: not-claimed")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
