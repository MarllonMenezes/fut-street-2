"""Recover bounded static control-flow slices for ranked render targets.

This audit starts from the bounded render-target windows and decodes a larger
linear window only to build a conservative reachability graph over direct
conditional/unconditional branches. Direct calls are recorded as edges but are
not followed, because a call target does not prove a function boundary or
subsystem meaning. Indirect control flow is retained as unresolved. The
analysis never executes the XBE, never treats a slice as a recovered original
function, and never assigns mesh, material, UV, skinning, rigging, animation,
morph or GPU semantics.
"""

from __future__ import annotations

import argparse
import csv
import hashlib
from pathlib import Path
from typing import Iterable

try:
    from capstone import CS_ARCH_X86, CS_MODE_32, Cs
    from capstone.x86_const import X86_OP_IMM
except ImportError as exc:  # pragma: no cover
    raise SystemExit("Capstone não está instalado no ambiente de análise.") from exc

from list_xdvdfs import XdvdfsReader
from parse_xbe import analyze_xbe, virtual_to_file_offset


REPORT_FIELDS = (
    "target_virtual_address",
    "section",
    "file_offset",
    "available_bytes",
    "linear_instruction_count",
    "reachable_instruction_count",
    "basic_block_count",
    "reachable_ret_count",
    "conditional_branch_count",
    "unconditional_branch_count",
    "internal_branch_target_count",
    "out_of_window_branch_target_count",
    "direct_call_count",
    "unique_direct_call_target_count",
    "indirect_control_transfer_count",
    "max_reachable_instruction_offset",
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
    "targets_with_reachable_cfg",
    "targets_with_reachable_ret",
    "total_linear_instructions",
    "total_reachable_instructions",
    "total_basic_blocks",
    "total_conditional_branches",
    "total_unconditional_branches",
    "total_internal_branch_targets",
    "total_out_of_window_branch_targets",
    "total_direct_calls",
    "total_unique_direct_call_targets",
    "total_indirect_control_transfers",
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


def decode_window(data: bytes, result: dict, address: int, max_bytes: int) -> tuple[dict, list]:
    base = result["fields"]["base_address"]
    offset = virtual_to_file_offset(address, base, len(data), result["sections"])
    if offset is None:
        raise ValueError(f"target 0x{address:X} does not map to XBE bytes")
    section = next(
        section
        for section in result["sections"]
        if section["executable"]
        and section["virtual_address"] <= address < section["virtual_address"] + section["virtual_size"]
    )
    available = min(max_bytes, section["file_address"] + section["file_size"] - offset)
    decoder = Cs(CS_ARCH_X86, CS_MODE_32)
    decoder.detail = True
    instructions = list(decoder.disasm(data[offset : offset + available], address))
    return section, instructions


def _immediate_target(instruction) -> int | None:
    if not instruction.operands or instruction.operands[0].type != X86_OP_IMM:
        return None
    return instruction.operands[0].imm & 0xFFFFFFFF


def recover_cfg(instructions: list, start_address: int) -> dict[str, int | bool | set[int]]:
    """Build a conservative CFG over already-decoded instructions.

    Calls remain call edges only. Direct branch targets are followed when they
    land on an instruction in the same bounded window. Indirect transfers and
    branch targets outside that window remain unresolved counts.
    """

    by_address = {instruction.address: index for index, instruction in enumerate(instructions)}
    start_index = by_address.get(start_address)
    if start_index is None:
        return {
            "reachable": set(),
            "basic_blocks": 0,
            "reachable_ret_count": 0,
            "conditional_branch_count": 0,
            "unconditional_branch_count": 0,
            "internal_branch_targets": set(),
            "out_of_window_branch_targets": 0,
            "direct_calls": 0,
            "direct_call_targets": set(),
            "indirect_control_transfers": 0,
            "max_reachable_instruction_offset": 0,
        }

    reachable: set[int] = set()
    worklist = [start_index]
    internal_branch_targets: set[int] = set()
    direct_call_targets: set[int] = set()
    out_of_window_branch_targets = 0
    direct_calls = 0
    indirect_control_transfers = 0
    conditional_branch_count = 0
    unconditional_branch_count = 0

    while worklist:
        index = worklist.pop()
        if index in reachable or index < 0 or index >= len(instructions):
            continue
        reachable.add(index)
        instruction = instructions[index]
        mnemonic = instruction.mnemonic.lower()
        next_index = index + 1 if index + 1 < len(instructions) else None

        if mnemonic in {"ret", "retf", "iret", "ud2"}:
            continue

        if mnemonic == "call":
            target = _immediate_target(instruction)
            if target is None:
                indirect_control_transfers += 1
            else:
                direct_calls += 1
                direct_call_targets.add(target)
            if next_index is not None:
                worklist.append(next_index)
            continue

        is_loop_branch = mnemonic in {"loop", "loope", "loopne", "jecxz", "jcxz", "jrcxz"}
        is_jump = mnemonic.startswith("j") or is_loop_branch
        if is_jump:
            target = _immediate_target(instruction)
            is_unconditional = mnemonic == "jmp"
            if is_unconditional:
                unconditional_branch_count += 1
            else:
                conditional_branch_count += 1
            if target is None:
                indirect_control_transfers += 1
            else:
                target_index = by_address.get(target)
                if target_index is None:
                    out_of_window_branch_targets += 1
                else:
                    internal_branch_targets.add(target)
                    worklist.append(target_index)
            if not is_unconditional and next_index is not None:
                worklist.append(next_index)
            continue

        if next_index is not None:
            worklist.append(next_index)

    block_starts = {start_index}
    for index in sorted(reachable):
        instruction = instructions[index]
        mnemonic = instruction.mnemonic.lower()
        if index in reachable and index > start_index and (index - 1 not in reachable):
            block_starts.add(index)
        if mnemonic.startswith("j") or mnemonic in {"loop", "loope", "loopne", "jecxz", "jcxz", "jrcxz"}:
            if index + 1 in reachable:
                block_starts.add(index + 1)
            target = _immediate_target(instruction)
            target_index = by_address.get(target) if target is not None else None
            if target_index in reachable:
                block_starts.add(target_index)
        elif mnemonic == "call" and index + 1 in reachable:
            # Calls do not split the original function semantically, but the
            # post-call continuation is a useful deterministic block boundary.
            block_starts.add(index + 1)

    max_offset = 0
    if reachable:
        max_offset = max(instructions[index].address - start_address for index in reachable)
    return {
        "reachable": reachable,
        "basic_blocks": len(block_starts),
        "reachable_ret_count": sum(instructions[index].mnemonic.lower() in {"ret", "retf"} for index in reachable),
        "conditional_branch_count": conditional_branch_count,
        "unconditional_branch_count": unconditional_branch_count,
        "internal_branch_targets": internal_branch_targets,
        "out_of_window_branch_targets": out_of_window_branch_targets,
        "direct_calls": direct_calls,
        "direct_call_targets": direct_call_targets,
        "indirect_control_transfers": indirect_control_transfers,
        "max_reachable_instruction_offset": max_offset,
    }


def analyze_target(data: bytes, result: dict, row: dict[str, str], max_bytes: int) -> dict[str, str]:
    address = _as_int(row.get("target_virtual_address"))
    if address is None:
        raise ValueError("target report row has no virtual address")
    section, instructions = decode_window(data, result, address, max_bytes)
    base = result["fields"]["base_address"]
    offset = virtual_to_file_offset(address, base, len(data), result["sections"])
    assert offset is not None
    available = min(max_bytes, section["file_address"] + section["file_size"] - offset)
    cfg = recover_cfg(instructions, address)
    reachable = cfg["reachable"]
    status = "verified-bounded-static-cfg" if reachable else "not-recovered"
    return {
        "target_virtual_address": f"0x{address:08X}",
        "section": section["name"],
        "file_offset": f"0x{offset:08X}",
        "available_bytes": str(available),
        "linear_instruction_count": str(len(instructions)),
        "reachable_instruction_count": str(len(reachable)),
        "basic_block_count": str(cfg["basic_blocks"]),
        "reachable_ret_count": str(cfg["reachable_ret_count"]),
        "conditional_branch_count": str(cfg["conditional_branch_count"]),
        "unconditional_branch_count": str(cfg["unconditional_branch_count"]),
        "internal_branch_target_count": str(len(cfg["internal_branch_targets"])),
        "out_of_window_branch_target_count": str(cfg["out_of_window_branch_targets"]),
        "direct_call_count": str(cfg["direct_calls"]),
        "unique_direct_call_target_count": str(len(cfg["direct_call_targets"])),
        "indirect_control_transfer_count": str(cfg["indirect_control_transfers"]),
        "max_reachable_instruction_offset": str(cfg["max_reachable_instruction_offset"]),
        "status": status,
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
    max_bytes: int,
) -> dict[str, str]:
    return {
        "image": image,
        "internal_path": internal_path,
        "xbe_sha256": xbe_sha256,
        "xbe_size": str(xbe_size),
        "selection_policy": f"same_top_{max_targets}_render_target_windows; bounded_cfg_bytes={max_bytes}; direct_calls_not_followed",
        "candidate_targets": str(len(selected_rows)),
        "analyzed_targets": str(len(target_rows)),
        "targets_with_reachable_cfg": str(sum(row["reachable_instruction_count"] != "0" for row in target_rows)),
        "targets_with_reachable_ret": str(sum(int(row["reachable_ret_count"]) > 0 for row in target_rows)),
        "total_linear_instructions": str(sum(int(row["linear_instruction_count"]) for row in target_rows)),
        "total_reachable_instructions": str(sum(int(row["reachable_instruction_count"]) for row in target_rows)),
        "total_basic_blocks": str(sum(int(row["basic_block_count"]) for row in target_rows)),
        "total_conditional_branches": str(sum(int(row["conditional_branch_count"]) for row in target_rows)),
        "total_unconditional_branches": str(sum(int(row["unconditional_branch_count"]) for row in target_rows)),
        "total_internal_branch_targets": str(sum(int(row["internal_branch_target_count"]) for row in target_rows)),
        "total_out_of_window_branch_targets": str(sum(int(row["out_of_window_branch_target_count"]) for row in target_rows)),
        "total_direct_calls": str(sum(int(row["direct_call_count"]) for row in target_rows)),
        "total_unique_direct_call_targets": str(sum(int(row["unique_direct_call_target_count"]) for row in target_rows)),
        "total_indirect_control_transfers": str(sum(int(row["indirect_control_transfer_count"]) for row in target_rows)),
        "status": "verified-static-render-cfg-slices",
        "provenance": "same-ISO-retail-XBE+bounded-reachability-over-ranked-render-windows; no-execution; no-function-boundary-claim; no-binding",
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
    parser.add_argument("target_report", type=Path)
    parser.add_argument("--internal-path", default="default.xbe")
    parser.add_argument("--max-bytes", type=int, default=2048)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()
    if args.max_bytes <= 0:
        raise SystemExit("--max-bytes deve ser positivo")
    selected_rows = read_tsv(args.target_report)
    result = analyze_xbe(args.image, args.internal_path)
    data = load_xbe(args.image, args.internal_path)
    target_rows = [analyze_target(data, result, row, args.max_bytes) for row in selected_rows]
    summary = build_summary(
        str(args.image),
        args.internal_path,
        hashlib.sha256(data).hexdigest(),
        len(data),
        selected_rows,
        target_rows,
        len(selected_rows),
        args.max_bytes,
    )
    write_tsv(args.report, REPORT_FIELDS, target_rows)
    write_tsv(args.summary, SUMMARY_FIELDS, [summary])
    print(
        f"candidate_targets: {summary['candidate_targets']}; "
        f"analyzed_targets: {summary['analyzed_targets']}; "
        f"reachable_cfg_targets: {summary['targets_with_reachable_cfg']}; "
        f"reachable_ret_targets: {summary['targets_with_reachable_ret']}; "
        f"basic_blocks: {summary['total_basic_blocks']}; "
        f"status: {summary['status']}"
    )
    print(f"report: {args.report}")
    print(f"summary: {args.summary}")
    print("original_semantics: not-claimed")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
