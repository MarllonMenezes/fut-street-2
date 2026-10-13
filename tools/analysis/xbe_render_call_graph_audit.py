"""Build a bounded interprocedural call graph from static render slices.

The graph starts from the direct call targets already reached by the bounded
render CFG audit and follows only direct immediate calls for a small, explicit
depth. Each node is decoded in a bounded executable-section window. The
result is a structural graph, not recovered function boundaries: the XBE is
never executed and no mesh/material/UV/skinning/rigging/animation/morph/GPU
binding or original semantic claim is made.
"""

from __future__ import annotations

import argparse
import csv
import hashlib
from collections import deque
from pathlib import Path
from typing import Iterable

from capstone.x86_const import X86_OP_IMM

from parse_xbe import analyze_xbe, virtual_to_file_offset
from xbe_render_call_target_audit import _as_int
from xbe_render_cfg_audit import decode_window, load_xbe, read_tsv, recover_cfg


NODE_FIELDS = (
    "call_target_virtual_address",
    "discovery_depth",
    "root_seed_addresses",
    "parent_addresses",
    "parent_edge_count",
    "section",
    "file_offset",
    "available_bytes",
    "instruction_count",
    "reachable_instruction_count",
    "reachable_ret_count",
    "direct_call_count",
    "unique_direct_call_target_count",
    "mapped_executable",
    "status",
    "original_semantics",
)

EDGE_FIELDS = (
    "parent_virtual_address",
    "child_virtual_address",
    "discovery_depth",
    "edge_occurrences",
    "parent_status",
    "child_discovery_status",
    "status",
    "original_semantics",
)

SUMMARY_FIELDS = (
    "image",
    "internal_path",
    "xbe_sha256",
    "xbe_size",
    "selection_policy",
    "seed_targets",
    "seed_mapped_targets",
    "max_depth",
    "max_nodes",
    "discovered_nodes",
    "mapped_executable_nodes",
    "disassembled_nodes",
    "ret_terminated_nodes",
    "nodes_with_direct_calls",
    "unique_edges",
    "total_edge_occurrences",
    "truncated_by_max_nodes",
    "status",
    "provenance",
    "original_semantics",
)


def _address(value: int) -> str:
    return f"0x{value:08X}"


def _direct_call_counts(instructions: list, reachable: set[int]) -> dict[int, int]:
    counts: dict[int, int] = {}
    for index in reachable:
        instruction = instructions[index]
        if instruction.mnemonic.lower() != "call":
            continue
        if not instruction.operands or instruction.operands[0].type != X86_OP_IMM:
            continue
        target = instruction.operands[0].imm & 0xFFFFFFFF
        counts[target] = counts.get(target, 0) + 1
    return counts


def _new_node(address: int, depth: int, root: int, parent: int | None) -> dict[str, object]:
    return {
        "address": address,
        "depth": depth,
        "roots": {root},
        "parents": {parent} if parent is not None else set(),
        "parent_edge_count": 0,
        "section": "",
        "file_offset": "",
        "available_bytes": 0,
        "instruction_count": 0,
        "reachable_instruction_count": 0,
        "reachable_ret_count": 0,
        "direct_call_count": 0,
        "children": set(),
        "mapped": False,
        "status": "not-mapped-executable",
        "expanded": False,
    }


def _node_row(node: dict[str, object]) -> dict[str, str]:
    return {
        "call_target_virtual_address": _address(int(node["address"])),
        "discovery_depth": str(node["depth"]),
        "root_seed_addresses": "|".join(_address(value) for value in sorted(node["roots"])),
        "parent_addresses": "|".join(_address(value) for value in sorted(node["parents"])),
        "parent_edge_count": str(node["parent_edge_count"]),
        "section": str(node["section"]),
        "file_offset": str(node["file_offset"]),
        "available_bytes": str(node["available_bytes"]),
        "instruction_count": str(node["instruction_count"]),
        "reachable_instruction_count": str(node["reachable_instruction_count"]),
        "reachable_ret_count": str(node["reachable_ret_count"]),
        "direct_call_count": str(node["direct_call_count"]),
        "unique_direct_call_target_count": str(len(node["children"])),
        "mapped_executable": str(node["mapped"]),
        "status": str(node["status"]),
        "original_semantics": "not-claimed",
    }


def _edge_row(
    parent: int,
    child: int,
    depth: int,
    occurrences: int,
    parent_status: str,
    child_status: str,
) -> dict[str, str]:
    return {
        "parent_virtual_address": _address(parent),
        "child_virtual_address": _address(child),
        "discovery_depth": str(depth),
        "edge_occurrences": str(occurrences),
        "parent_status": parent_status,
        "child_discovery_status": child_status,
        "status": "verified-bounded-static-direct-call-edge",
        "original_semantics": "not-claimed",
    }


def build_graph(
    data: bytes,
    result: dict,
    seed_rows: list[dict[str, str]],
    max_depth: int,
    max_bytes: int,
    max_nodes: int,
) -> tuple[dict[int, dict[str, object]], dict[tuple[int, int], dict[str, object]], bool]:
    nodes: dict[int, dict[str, object]] = {}
    edges: dict[tuple[int, int], dict[str, object]] = {}
    queue: deque[int] = deque()
    for row in seed_rows:
        seed = _as_int(row.get("call_target_virtual_address"))
        if seed is None:
            continue
        node = nodes.get(seed)
        if node is None:
            node = _new_node(seed, 0, seed, None)
            nodes[seed] = node
            queue.append(seed)
        else:
            node["roots"].add(seed)

    truncated = False
    while queue:
        address = queue.popleft()
        node = nodes[address]
        if node["expanded"]:
            continue
        node["expanded"] = True
        try:
            section, instructions = decode_window(data, result, address, max_bytes)
        except (StopIteration, ValueError):
            continue

        base = result["fields"]["base_address"]
        offset = virtual_to_file_offset(address, base, len(data), result["sections"])
        if offset is None:
            continue
        available = min(max_bytes, section["file_address"] + section["file_size"] - offset)
        cfg = recover_cfg(instructions, address)
        reachable = cfg["reachable"]
        call_counts = _direct_call_counts(instructions, reachable)
        node["section"] = section["name"]
        node["file_offset"] = _address(offset)
        node["available_bytes"] = available
        node["instruction_count"] = len(instructions)
        node["reachable_instruction_count"] = len(reachable)
        node["reachable_ret_count"] = cfg["reachable_ret_count"]
        node["direct_call_count"] = cfg["direct_calls"]
        node["mapped"] = True
        node["status"] = "verified-bounded-static-call-graph-node"

        for child, occurrence_count in sorted(call_counts.items()):
            node["children"].add(child)
            edge_key = (address, child)
            edge = edges.setdefault(
                edge_key,
                {"depth": int(node["depth"]) + 1, "occurrences": 0},
            )
            edge["occurrences"] += occurrence_count
            child_node = nodes.get(child)
            if child_node is None:
                if int(node["depth"]) >= max_depth:
                    continue
                if len(nodes) >= max_nodes:
                    truncated = True
                    continue
                child_node = _new_node(child, int(node["depth"]) + 1, min(node["roots"]), address)
                nodes[child] = child_node
                queue.append(child)
            else:
                child_node["roots"].update(node["roots"])
                child_node["parents"].add(address)
            child_node["parent_edge_count"] += occurrence_count

    return nodes, edges, truncated


def write_tsv(path: Path, fields: tuple[str, ...], rows: Iterable[dict[str, str]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=fields, delimiter="\t", lineterminator="\n")
        writer.writeheader()
        writer.writerows({field: row.get(field, "") for field in fields} for row in rows)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("image", type=Path)
    parser.add_argument("call_target_report", type=Path)
    parser.add_argument("--internal-path", default="default.xbe")
    parser.add_argument("--max-depth", type=int, default=2)
    parser.add_argument("--max-bytes", type=int, default=256)
    parser.add_argument("--max-nodes", type=int, default=512)
    parser.add_argument("--node-report", type=Path, required=True)
    parser.add_argument("--edge-report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()
    if args.max_depth < 0 or args.max_bytes <= 0 or args.max_nodes <= 0:
        raise SystemExit("depth deve ser não-negativo e limites devem ser positivos")

    seed_rows = read_tsv(args.call_target_report)
    result = analyze_xbe(args.image, args.internal_path)
    data = load_xbe(args.image, args.internal_path)
    nodes, edges, truncated = build_graph(
        data, result, seed_rows, args.max_depth, args.max_bytes, args.max_nodes)
    node_rows = [_node_row(nodes[address]) for address in sorted(nodes)]
    edge_rows = [
        _edge_row(
            parent,
            child,
            int(edge["depth"]),
            int(edge["occurrences"]),
            str(nodes[parent]["status"]),
            str(nodes[child]["status"]) if child in nodes else "not-discovered-by-bound",
        )
        for (parent, child), edge in sorted(edges.items())
    ]
    summary = {
        "image": str(args.image),
        "internal_path": args.internal_path,
        "xbe_sha256": hashlib.sha256(data).hexdigest(),
        "xbe_size": str(len(data)),
        "selection_policy": f"seeded-by-call-target-report; max_depth={args.max_depth}; max_bytes={args.max_bytes}; max_nodes={args.max_nodes}",
        "seed_targets": str(sum(_as_int(row.get("call_target_virtual_address")) is not None for row in seed_rows)),
        "seed_mapped_targets": str(sum(
            nodes[address]["mapped"]
            for address in nodes
            if nodes[address]["depth"] == 0
        )),
        "max_depth": str(args.max_depth),
        "max_nodes": str(args.max_nodes),
        "discovered_nodes": str(len(nodes)),
        "mapped_executable_nodes": str(sum(node["mapped"] for node in nodes.values())),
        "disassembled_nodes": str(sum(node["instruction_count"] != 0 for node in nodes.values())),
        "ret_terminated_nodes": str(sum(node["reachable_ret_count"] != 0 for node in nodes.values())),
        "nodes_with_direct_calls": str(sum(node["direct_call_count"] != 0 for node in nodes.values())),
        "unique_edges": str(len(edges)),
        "total_edge_occurrences": str(sum(int(edge["occurrences"]) for edge in edges.values())),
        "truncated_by_max_nodes": str(truncated),
        "status": "verified-static-bounded-render-call-graph",
        "provenance": "same-ISO-retail-XBE+bounded-direct-call-graph-seeded-by-render-CFG-targets; no-execution; no-function-boundary-claim; no-binding",
        "original_semantics": "not-claimed",
    }
    write_tsv(args.node_report, NODE_FIELDS, node_rows)
    write_tsv(args.edge_report, EDGE_FIELDS, edge_rows)
    write_tsv(args.summary, SUMMARY_FIELDS, [summary])
    print(
        f"seed_targets: {summary['seed_targets']}; discovered_nodes: {summary['discovered_nodes']}; "
        f"mapped_executable_nodes: {summary['mapped_executable_nodes']}; unique_edges: {summary['unique_edges']}; "
        f"ret_terminated_nodes: {summary['ret_terminated_nodes']}; status: {summary['status']}"
    )
    print(f"node_report: {args.node_report}")
    print(f"edge_report: {args.edge_report}")
    print(f"summary: {args.summary}")
    print("original_semantics: not-claimed")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
