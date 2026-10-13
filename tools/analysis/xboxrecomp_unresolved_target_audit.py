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
    return {
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


def audit(stubs_path: Path, functions_path: Path, analysis_path: Path) -> dict[str, Any]:
    stubs = parse_stub_targets(stubs_path.read_text(encoding="utf-8"))
    functions = json.loads(functions_path.read_text(encoding="utf-8"))
    analysis = json.loads(analysis_path.read_text(encoding="utf-8"))
    rows = classify_targets(stubs, functions, analysis["sections"])
    return {"summary": summarize(rows), "targets": rows}


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--stubs-c", type=Path, required=True)
    parser.add_argument("--functions-json", type=Path, required=True)
    parser.add_argument("--xbe-analysis-json", type=Path, required=True)
    parser.add_argument("--details", action="store_true", help="include target VAs and parent ranges")
    parser.add_argument("--output-json", type=Path, help="write a local report (may contain game-derived addresses)")
    args = parser.parse_args()

    result = audit(args.stubs_c, args.functions_json, args.xbe_analysis_json)
    payload = result if args.details else {"summary": result["summary"]}
    serialized = json.dumps(payload, indent=2, sort_keys=True)
    if args.output_json:
        args.output_json.write_text(serialized + "\n", encoding="utf-8")
    print(serialized)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
