"""Audit direct translation coverage from a statically recovered entry point.

This audit joins XboxRecomp's function metadata with generated C symbols.  It
follows only the ``calls_to`` edges already present in the metadata; indirect
calls, callbacks, imports, and runtime behavior are deliberately outside its
claim.  It never loads or executes the XBE.
"""

from __future__ import annotations

import argparse
import json
import re
from collections import deque
from pathlib import Path
from typing import Any


GENERATED_FUNCTION_RE = re.compile(r"\bvoid\s+sub_([0-9A-Fa-f]{8})\s*\(void\)\s*\{")
GENERATED_FUNCTION_NAME_RE = re.compile(
    r"\bvoid\s+(sub_[0-9A-Fa-f]{8})\s*\(void\)"
)
GENERATED_SYMBOL_RE = re.compile(r"\bvoid\s+([A-Za-z_][A-Za-z0-9_]*)\s*\(void\)\s*\{")
UNIMPLEMENTED_RE = re.compile(
    r"RECOMP_UNIMPL\(\s*\"(?P<text>[^\"]*)\"\s*,\s*"
    r"0x(?P<address>[0-9A-Fa-f]{8})u\s*\)"
)
FALLTHROUGH_RE = re.compile(
    r"/\*\s*fallthrough\s+0x(?P<target>[0-9A-Fa-f]{8})\s*\*/"
)


def _address(value: Any) -> int:
    if isinstance(value, int):
        return value
    return int(str(value), 0)


def _function_map(functions: list[dict[str, Any]]) -> dict[int, dict[str, Any]]:
    result: dict[int, dict[str, Any]] = {}
    for function in functions:
        start = _address(function["start"])
        if start in result:
            raise ValueError(f"duplicate function start: {start:#010x}")
        result[start] = function
    return result


def recover_direct_reachable(
    functions: list[dict[str, Any]], entry: int
) -> tuple[set[int], set[int]]:
    """Return direct-call reachable functions and direct targets not in metadata."""
    by_start = _function_map(functions)
    if entry not in by_start:
        raise ValueError(f"entry point is not present in functions metadata: {entry:#010x}")

    reachable: set[int] = set()
    external_targets: set[int] = set()
    pending: deque[int] = deque([entry])
    while pending:
        address = pending.popleft()
        if address in reachable:
            continue
        function = by_start.get(address)
        if function is None:
            external_targets.add(address)
            continue
        reachable.add(address)
        for raw_target in function.get("calls_to", []):
            target = _address(raw_target)
            if target in by_start:
                pending.append(target)
            else:
                external_targets.add(target)
    return reachable, external_targets


def recover_generated_functions(
    generated_dir: Path, functions: list[dict[str, Any]] | None = None
) -> set[int]:
    """Recover translated function symbols from generated C sources."""
    addresses: set[int] = set()
    generated_symbols: set[str] = set()
    for source_path in sorted(generated_dir.rglob("recomp_*.c")):
        source = source_path.read_text(encoding="utf-8")
        addresses.update(int(match.group(1), 16) for match in GENERATED_FUNCTION_RE.finditer(source))
        generated_symbols.update(match.group(1) for match in GENERATED_SYMBOL_RE.finditer(source))
    if functions:
        for function in functions:
            if str(function.get("name", "")) in generated_symbols:
                addresses.add(_address(function["start"]))
    if not addresses:
        raise ValueError(f"no generated function definitions found under {generated_dir}")
    return addresses


def recover_unimplemented_sites(generated_dir: Path) -> list[int]:
    """Recover untranslated instruction addresses from generated C sources."""
    return [
        int(row["address"], 16)
        for row in recover_unimplemented_site_evidence(generated_dir)
    ]


def recover_unimplemented_site_evidence(
    generated_dir: Path,
) -> list[dict[str, Any]]:
    """Recover unique untranslated sites with source and instruction text."""
    sites: dict[int, dict[str, Any]] = {}
    for source_path in sorted(generated_dir.rglob("recomp_*.c")):
        for line_number, line in enumerate(
            source_path.read_text(encoding="utf-8").splitlines(), 1
        ):
            for match in UNIMPLEMENTED_RE.finditer(line):
                address = int(match.group("address"), 16)
                row = sites.setdefault(
                    address,
                    {
                        "address": f"0x{address:08X}",
                        "text": match.group("text"),
                        "sources": [],
                    },
                )
                source = f"{source_path.name}:{line_number}"
                if source not in row["sources"]:
                    row["sources"].append(source)
    return [sites[address] for address in sorted(sites)]


def recover_generated_fallthrough_edges(
    generated_dir: Path,
    functions: list[dict[str, Any]] | None = None,
) -> list[dict[str, Any]]:
    """Recover explicit generated-C fallthrough edges.

    XboxRecomp can preserve a sequential transfer as a direct call-shaped
    helper followed by ``return``.  These edges are evidence from generated
    source only; they are not merged into the metadata ``calls_to`` graph
    because that would silently change the meaning of direct reachability.
    """
    known_functions = (
        set() if functions is None else {_address(row["start"]) for row in functions}
    )
    edges: list[dict[str, Any]] = []
    for source_path in sorted(generated_dir.rglob("recomp_*.c")):
        current_function: str | None = None
        current_address: int | None = None
        for line_number, line in enumerate(
            source_path.read_text(encoding="utf-8").splitlines(), 1
        ):
            symbol_match = GENERATED_FUNCTION_NAME_RE.search(line)
            if symbol_match is not None:
                current_function = symbol_match.group(1)
                current_address = int(current_function[4:], 16)
            match = FALLTHROUGH_RE.search(line)
            if match is None:
                continue
            target = int(match.group("target"), 16)
            edges.append(
                {
                    "source_function": (
                        None
                        if current_address is None
                        else f"0x{current_address:08X}"
                    ),
                    "source_function_name": current_function,
                    "target": f"0x{target:08X}",
                    "source": f"{source_path.name}:{line_number}",
                    "target_in_metadata": target in known_functions,
                }
            )
    return edges


def _owners(functions: list[dict[str, Any]], address: int) -> list[int]:
    owners = []
    for function in functions:
        start = _address(function["start"])
        end = _address(function["end"])
        if start <= address < end:
            owners.append(start)
    return sorted(owners)


def audit(
    functions_path: Path,
    generated_dir: Path,
    entry: int = 0x0024D8BE,
) -> dict[str, Any]:
    """Build a conservative direct-call translation coverage audit."""
    functions = json.loads(functions_path.read_text(encoding="utf-8"))
    reachable, external_targets = recover_direct_reachable(functions, entry)
    generated = recover_generated_functions(generated_dir, functions)
    missing = sorted(reachable - generated)
    fallthrough_edges = recover_generated_fallthrough_edges(generated_dir, functions)
    direct_reachable_fallthrough_edges = [
        row
        for row in fallthrough_edges
        if row["source_function"] is not None
        and _address(row["source_function"]) in reachable
    ]
    fallthrough_targets_missing_from_metadata = [
        row["target"] for row in fallthrough_edges if not row["target_in_metadata"]
    ]
    fallthrough_targets_missing_from_generated = [
        row["target"]
        for row in fallthrough_edges
        if _address(row["target"]) not in generated
    ]

    unimplemented_rows = []
    for site_evidence in recover_unimplemented_site_evidence(generated_dir):
        site = int(str(site_evidence["address"]), 16)
        owners = _owners(functions, site)
        unimplemented_rows.append(
            {
                **site_evidence,
                "owners": [f"0x{owner:08X}" for owner in owners],
                "in_directly_reachable_function_scope": any(
                    owner in reachable for owner in owners
                ),
            }
        )

    summary = {
        "entry_point": f"0x{entry:08X}",
        "metadata_functions": len(functions),
        "directly_reachable_functions": len(reachable),
        "direct_external_call_targets": len(external_targets),
        "generated_function_definitions": len(generated),
        "directly_reachable_missing_translations": len(missing),
        "generated_fallthrough_edges": len(fallthrough_edges),
        "direct_reachable_fallthrough_edges": len(
            direct_reachable_fallthrough_edges
        ),
        "fallthrough_targets_missing_from_metadata": sorted(
            set(fallthrough_targets_missing_from_metadata)
        ),
        "fallthrough_targets_missing_from_generated": sorted(
            set(fallthrough_targets_missing_from_generated)
        ),
        "fallthrough_status": (
            "no-generated-fallthrough-edges-observed"
            if not fallthrough_edges
            else (
                "generated-fallthrough-target-coverage-incomplete"
                if fallthrough_targets_missing_from_metadata
                or fallthrough_targets_missing_from_generated
                else "generated-fallthrough-targets-covered"
            )
        ),
        "unimplemented_sites": len(unimplemented_rows),
        "unimplemented_sites_in_directly_reachable_function_scopes": sum(
            row["in_directly_reachable_function_scope"] for row in unimplemented_rows
        ),
        "unimplemented_direct_reachable_site_addresses": [
            row["address"]
            for row in unimplemented_rows
            if row["in_directly_reachable_function_scope"]
        ],
        "unimplemented_outside_direct_reachable_site_addresses": [
            row["address"]
            for row in unimplemented_rows
            if not row["in_directly_reachable_function_scope"]
        ],
        "status": (
            "verified-direct-call-translation-coverage"
            if not missing
            else "incomplete-direct-call-translation-coverage"
        ),
        "original_semantics": "not-claimed",
    }
    return {
        "summary": summary,
        "missing_direct_reachable_translations": [f"0x{address:08X}" for address in missing],
        "unimplemented_sites": unimplemented_rows,
        "fallthrough_edges": fallthrough_edges,
    }


def _parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--functions", type=Path, required=True, help="functions.json")
    parser.add_argument("--generated", type=Path, required=True, help="XboxRecomp generated C directory")
    parser.add_argument("--entry", type=lambda value: int(value, 0), default=0x0024D8BE)
    parser.add_argument("--format", choices=("json", "text"), default="text")
    return parser


def main() -> int:
    args = _parser().parse_args()
    result = audit(args.functions, args.generated, args.entry)
    if args.format == "json":
        print(json.dumps(result, indent=2, sort_keys=True))
    else:
        for key, value in result["summary"].items():
            print(f"{key}={value}")
        if result["missing_direct_reachable_translations"]:
            print("missing_direct_reachable_translations=" + ",".join(result["missing_direct_reachable_translations"]))
        for row in result["unimplemented_sites"]:
            print(
                "unimplemented_site={address} owners={owners} directly_reachable_scope={reachable} "
                "source={source} text={text}".format(
                    address=row["address"],
                    owners=",".join(row["owners"]) or "none",
                    reachable=row["in_directly_reachable_function_scope"],
                    source=",".join(row["sources"]),
                    text=row["text"],
                )
            )
    return 0 if not result["missing_direct_reachable_translations"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
