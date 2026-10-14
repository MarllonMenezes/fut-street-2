"""Verify the bounded static context of the remaining untranslated opcodes.

This control does not assign hardware or GDT semantics.  It only checks that
the two untranslated sites retain the exact owner function and nearby
instruction shapes observed in the recovered ASM listing.
"""

from __future__ import annotations

import argparse
import json
import re
from pathlib import Path
from typing import Any


FUNCTION_RE = re.compile(r"^; Function: (?P<name>sub_[0-9A-Fa-f]+)\s*$")
RANGE_RE = re.compile(
    r"^; Start: 0x(?P<start>[0-9A-Fa-f]{8})\s+"
    r"End: 0x(?P<end>[0-9A-Fa-f]{8})\s+Size:"
)
ADDRESS_RE = re.compile(r"^\s*0x(?P<address>[0-9A-Fa-f]{8})\s+(?P<body>.*)$")


EXPECTED_UNIMPLEMENTED_CONTEXTS = {
    0x00298030: {
        "instruction": "out dx, al",
        "function": "sub_00297F04",
        "context": "fixed-port-byte-write-before-two-indirect-calls",
        "required_fragments": (
            "mov dx, 0x80c0",
            "mov al, bl",
            "out dx, al",
            "lea eax, [ebp - 8]",
            "call edi",
            "mov dword ptr [esi + 0xa0], ebx",
        ),
    },
    0x00253FD8: {
        "instruction": "sgdt [esp + 6]",
        "function": "sub_00253FD0",
        "context": "descriptor-store-around-guest-interrupt-mask-and-far-jump",
        "required_fragments": (
            "mov ecx, dword ptr [esp + 4]",
            "mov edx, dword ptr [esp + 8]",
            "sgdt [esp + 6]",
            "mov eax, dword ptr [esp + 8]",
            "cli",
            "xchg dword ptr [eax + 8], ecx",
            "xchg dword ptr [eax + 0xc], edx",
            "sti",
            "ljmp 8:0x253ff0",
        ),
    },
}


def _finish_block(
    blocks: dict[str, dict[str, Any]], current: dict[str, Any] | None
) -> None:
    if current is None:
        return
    name = current["name"]
    if current["start"] is None or current["end"] is None:
        raise ValueError(f"function range missing for {name}")
    if name in blocks:
        raise ValueError(f"duplicate function body: {name}")
    current["body_text"] = "\n".join(current["body_lines"])
    blocks[name] = current


def recover_function_blocks(asm_dir: Path) -> dict[str, dict[str, Any]]:
    """Recover function ranges and normalized instruction lines from ASM."""
    blocks: dict[str, dict[str, Any]] = {}
    for asm_path in sorted(asm_dir.rglob("*.asm")):
        current: dict[str, Any] | None = None
        for line_number, raw_line in enumerate(
            asm_path.read_text(encoding="utf-8").splitlines(), 1
        ):
            function_match = FUNCTION_RE.match(raw_line)
            if function_match:
                _finish_block(blocks, current)
                current = {
                    "name": function_match.group("name"),
                    "start": None,
                    "end": None,
                    "body_lines": [],
                    "instructions": [],
                    "source": asm_path.name,
                }
                continue
            if current is None:
                continue

            range_match = RANGE_RE.match(raw_line)
            if range_match:
                current["start"] = int(range_match.group("start"), 16)
                current["end"] = int(range_match.group("end"), 16)

            normalized = " ".join(raw_line.strip().lower().split())
            if normalized:
                current["body_lines"].append(normalized)

            address_match = ADDRESS_RE.match(raw_line)
            if address_match:
                current["instructions"].append(
                    {
                        "address": int(address_match.group("address"), 16),
                        "text": normalized,
                        "source": f"{asm_path.name}:{line_number}",
                    }
                )

            if raw_line.strip() == "; end of function":
                _finish_block(blocks, current)
                current = None

        _finish_block(blocks, current)
    return blocks


def _owner_for_address(
    blocks: dict[str, dict[str, Any]], address: int
) -> dict[str, Any]:
    owners = [
        block
        for block in blocks.values()
        if block["start"] <= address < block["end"]
    ]
    if len(owners) != 1:
        raise ValueError(
            f"expected one owner for 0x{address:08X}, found "
            f"{[block['name'] for block in owners]!r}"
        )
    return owners[0]


def verify_contexts(blocks: dict[str, dict[str, Any]]) -> list[dict[str, Any]]:
    """Verify exact site ownership, instruction text, and local shapes."""
    rows: list[dict[str, Any]] = []
    for address, expected in EXPECTED_UNIMPLEMENTED_CONTEXTS.items():
        owner = _owner_for_address(blocks, address)
        if owner["name"] != expected["function"]:
            raise ValueError(
                f"unimplemented context owner drift at 0x{address:08X}: "
                f"expected={expected['function']} observed={owner['name']}"
            )

        site_rows = [
            row for row in owner["instructions"] if row["address"] == address
        ]
        if len(site_rows) != 1:
            raise ValueError(
                f"expected one instruction at 0x{address:08X}, "
                f"found={len(site_rows)}"
            )
        if expected["instruction"] not in site_rows[0]["text"]:
            raise ValueError(
                f"unimplemented instruction drift at 0x{address:08X}: "
                f"expected={expected['instruction']!r} "
                f"observed={site_rows[0]['text']!r}"
            )

        missing = [
            fragment
            for fragment in expected["required_fragments"]
            if fragment not in owner["body_text"]
        ]
        if missing:
            raise ValueError(
                f"unimplemented context drift at 0x{address:08X}: "
                f"missing={missing!r}"
            )
        rows.append(
            {
                "address": f"0x{address:08X}",
                "context": expected["context"],
                "function": owner["name"],
                "required_fragments": len(expected["required_fragments"]),
                "source": site_rows[0]["source"],
                "instruction": expected["instruction"],
            }
        )
    return rows


def audit(asm_dir: Path) -> dict[str, Any]:
    """Audit the two remaining untranslated sites without assigning semantics."""
    rows = verify_contexts(recover_function_blocks(asm_dir))
    return {
        "summary": {
            "status": "verified-static-unimplemented-opcode-contexts",
            "site_count": len(rows),
            "hardware_semantics": "not-proven",
            "original_semantics": "not-claimed",
            "lowering_status": "2-sites-remain-unimplemented",
        },
        "sites": rows,
    }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--asm-dir", type=Path, required=True)
    parser.add_argument("--format", choices=("json", "text"), default="text")
    args = parser.parse_args()
    result = audit(args.asm_dir)
    if args.format == "json":
        print(json.dumps(result, indent=2, sort_keys=True))
    else:
        for key, value in result["summary"].items():
            print(f"{key}={value}")
        for row in result["sites"]:
            print(
                "unimplemented_context={address} function={function} "
                "instruction={instruction} context={context} source={source}".format(
                    **row
                )
            )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
