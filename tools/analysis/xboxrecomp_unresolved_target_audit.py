"""Classify XboxRecomp unresolved call stubs against static XBE metadata.

The report is diagnostic only. A target inside a known function is not
automatically a function entry, and a target in an executable-flagged section
is not automatically code. This tool never loads or executes the XBE.
"""

from __future__ import annotations

import argparse
import json
import re
from collections import Counter
from pathlib import Path
from typing import Any


STUB_RE = re.compile(
    r"void sub_([0-9A-Fa-f]{8})\(void\)\s*\{\s*"
    r"g_esp\s*\+=\s*(\d+)\s*;\s*/\*\s*0x([0-9A-Fa-f]{8}):\s*"
    r"([^*]*)\*/\s*\}"
)


def _address(value: Any) -> int:
    if isinstance(value, int):
        return value
    return int(str(value), 0)


def parse_stub_targets(source: str) -> list[dict[str, Any]]:
    """Extract address and generated stack delta from XboxRecomp's C stubs."""
    targets = []
    for match in STUB_RE.finditer(source):
        address = int(match.group(1), 16)
        comment_address = int(match.group(3), 16)
        if address != comment_address:
            raise ValueError(
                f"stub symbol/address mismatch: {address:#010x} != "
                f"{comment_address:#010x}"
            )
        targets.append(
            {
                "address": address,
                "stack_delta": int(match.group(2)),
                "return_evidence": match.group(4).strip(),
            }
        )
    if not targets:
        raise ValueError("no unresolved-call stubs matched the expected format")
    addresses = [row["address"] for row in targets]
    if len(addresses) != len(set(addresses)):
        raise ValueError("duplicate unresolved-call stub addresses")
    return sorted(targets, key=lambda row: row["address"])


def classify_targets(
    targets: list[dict[str, Any]],
    functions: list[dict[str, Any]],
    sections: list[dict[str, Any]],
) -> list[dict[str, Any]]:
    """Join stubs to function intervals and the exact analyzed XBE sections."""
    function_rows = sorted(
        (
            _address(row["start"]),
            _address(row["end"]),
            str(row.get("name", "")),
        )
        for row in functions
    )
    section_rows = sorted(
        (
            _address(row.get("virtual_addr", row.get("virtual_address"))),
            _address(row.get("virtual_addr", row.get("virtual_address")))
            + _address(row["virtual_size"]),
            row,
        )
        for row in sections
    )

    result = []
    for target in targets:
        address = target["address"]
        containing_functions = [
            row for row in function_rows if row[0] <= address < row[1]
        ]
        containing_sections = [
            row for row in section_rows if row[0] <= address < row[1]
        ]
        if len(containing_sections) > 1:
            raise ValueError(f"overlapping XBE sections at {address:#010x}")

        section = containing_sections[0][2] if containing_sections else None
        parent = containing_functions[0] if len(containing_functions) == 1 else None
        if len(containing_functions) > 1:
            relation = "overlapping-function-ranges"
            parent_name = None
            parent_start = None
            parent_end = None
            next_start = None
        elif parent:
            relation = "function-start" if address == parent[0] else "function-interior"
            parent_name = parent[2]
            parent_start = parent[0]
            parent_end = parent[1]
        else:
            preceding = [row for row in function_rows if row[1] <= address]
            following = [row for row in function_rows if row[0] > address]
            relation = (
                "function-end"
                if preceding and preceding[-1][1] == address
                else "uncovered-address"
            )
            parent_name = preceding[-1][2] if preceding else None
            parent_start = preceding[-1][0] if preceding else None
            parent_end = preceding[-1][1] if preceding else None
            if following:
                next_start = following[0][0]
            else:
                next_start = None

        if parent and parent_end is not None and parent_end <= address:
            raise AssertionError("containing function must extend past target")
        if parent:
            next_start = None

        result.append(
            {
                **target,
                "section": section.get("name") if section else None,
                "section_executable_flag": section.get("executable") if section else None,
                "function_relation": relation,
                "parent_function": parent_name,
                "parent_start": parent_start,
                "parent_end": parent_end,
                "next_function_start": next_start,
                "overlapping_functions": [row[2] for row in containing_functions]
                if len(containing_functions) > 1 else [],
            }
        )
    return result


def summarize(rows: list[dict[str, Any]]) -> dict[str, Any]:
    summary = {
        "unresolved_targets": len(rows),
        "by_section": dict(sorted(Counter(row["section"] or "unmapped" for row in rows).items())),
        "by_function_relation": dict(
            sorted(Counter(row["function_relation"] for row in rows).items())
        ),
        "generated_stack_delta": dict(
            sorted(Counter(str(row["stack_delta"]) for row in rows).items())
        ),
        "return_metadata": dict(
            sorted(
                Counter(
                    "recognized-ret-n" if row["return_evidence"].startswith("ret ")
                    else "not-detected"
                    for row in rows
                ).items()
            )
        ),
    }
    if rows and "xref_types" in rows[0]:
        summary["direct_xrefs_by_type"] = dict(
            sorted(
                Counter(
                    xref_type
                    for row in rows
                    for xref_type, count in row["xref_types"].items()
                    for _ in range(count)
                ).items()
            )
        )
        summary["targets_with_direct_xrefs"] = sum(
            bool(row["xref_types"]) for row in rows
        )
        summary["targets_with_branch_xrefs_only"] = sum(
            bool(row["xref_types"])
            and set(row["xref_types"]).issubset({"jump", "cond_jump"})
            for row in rows
        )
        summary["branch_xrefs_to_target_inside_unique_source_function"] = sum(
            row["branch_xrefs_to_target_inside_unique_source_function"]
            for row in rows
        )
        summary["branch_xrefs_from_overlapping_source_ranges"] = sum(
            row["branch_xrefs_from_overlapping_source_ranges"] for row in rows
        )
    return summary


def join_direct_xrefs(
    rows: list[dict[str, Any]],
    functions: list[dict[str, Any]],
    xrefs: list[dict[str, Any]],
) -> list[dict[str, Any]]:
    """Attach direct xref counts without inferring function or binding semantics.

    A branch whose destination lies inside the unique function interval of its
    source is a useful coalescence candidate, not proof that the detected
    function boundary is wrong (shared tails and alternate entries are possible).
    """
    function_rows = [
        (_address(row["start"]), _address(row["end"])) for row in functions
    ]
    refs_by_target: dict[int, list[dict[str, Any]]] = {}
    for xref in xrefs:
        refs_by_target.setdefault(_address(xref["to"]), []).append(xref)

    joined = []
    for row in rows:
        refs = refs_by_target.get(row["address"], [])
        types = Counter(str(ref.get("type", "unknown")) for ref in refs)
        local_branch_count = 0
        ambiguous_branch_count = 0
        for ref in refs:
            if ref.get("type") not in {"jump", "cond_jump"}:
                continue
            source = _address(ref["from"])
            source_owners = [
                (start, end) for start, end in function_rows if start <= source < end
            ]
            if (
                len(source_owners) == 1
                and source_owners[0][0] <= row["address"] < source_owners[0][1]
            ):
                local_branch_count += 1
            elif (
                len(source_owners) > 1
                and any(start <= row["address"] < end for start, end in source_owners)
            ):
                ambiguous_branch_count += 1
        joined.append(
            {
                **row,
                "xref_types": dict(sorted(types.items())),
                "direct_xref_count": len(refs),
                "branch_xrefs_to_target_inside_unique_source_function": local_branch_count,
                "branch_xrefs_from_overlapping_source_ranges": ambiguous_branch_count,
            }
        )
    return joined


def audit(
    stubs_path: Path,
    functions_path: Path,
    analysis_path: Path,
    xrefs_path: Path | None = None,
) -> dict[str, Any]:
    stubs = parse_stub_targets(stubs_path.read_text(encoding="utf-8"))
    functions = json.loads(functions_path.read_text(encoding="utf-8"))
    analysis = json.loads(analysis_path.read_text(encoding="utf-8"))
    rows = classify_targets(stubs, functions, analysis["sections"])
    if xrefs_path:
        xrefs = json.loads(xrefs_path.read_text(encoding="utf-8"))
        rows = join_direct_xrefs(rows, functions, xrefs)
    return {"summary": summarize(rows), "targets": rows}


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--stubs-c", type=Path, required=True)
    parser.add_argument("--functions-json", type=Path, required=True)
    parser.add_argument("--xbe-analysis-json", type=Path, required=True)
    parser.add_argument(
        "--xrefs-json", type=Path,
        help="optional static xrefs JSON; branch matches are candidates, not proofs",
    )
    parser.add_argument("--details", action="store_true", help="include target VAs and parent ranges")
    parser.add_argument("--output-json", type=Path, help="write a local report (may contain game-derived addresses)")
    args = parser.parse_args()

    result = audit(args.stubs_c, args.functions_json, args.xbe_analysis_json, args.xrefs_json)
    payload = result if args.details else {"summary": result["summary"]}
    serialized = json.dumps(payload, indent=2, sort_keys=True)
    if args.output_json:
        args.output_json.write_text(serialized + "\n", encoding="utf-8")
    print(serialized)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
