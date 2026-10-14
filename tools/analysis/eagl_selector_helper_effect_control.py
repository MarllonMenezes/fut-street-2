"""Verify static effects of helpers reached by the EAGL selector consumers.

This control joins the already-bounded selector consumer call families to
small, observed instruction-shape contracts in their callees. It does not
name GPU state, execute the XBE, or infer the original meaning of any global.
"""

from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
from typing import Any

try:
    from .eagl_dynamic_buffer_selector_control import (
        EXPECTED_CONSUMER_CALLS,
        EXPECTED_XBE_SHA256,
        recover_function_blocks,
    )
except ImportError:  # pragma: no cover - direct script execution
    from eagl_dynamic_buffer_selector_control import (
        EXPECTED_CONSUMER_CALLS,
        EXPECTED_XBE_SHA256,
        recover_function_blocks,
    )


EXPECTED_HELPER_EFFECTS = {
    "sub_001A0530": {
        "effect": "read-global-and-return",
        "required_fragments": ("mov      eax, dword ptr [0x345d88]",),
    },
    "sub_001A2970": {
        "effect": "conditional-dispatch-with-state-return",
        "required_fragments": (
            "cmp      eax, 1",
            "cmp      eax, 4",
            "call     0x2921c0                       ; -> sub_002921c0",
            "call     0x292220                       ; -> sub_00292220",
            "call     0x2923b0                       ; -> sub_002923b0",
            "jmp      0x1a0530                       ; -> sub_001a0530",
        ),
    },
    "sub_001A29B0": {
        "effect": "indexed-pair-cache-update",
        "required_fragments": (
            "mov      dword ptr [eax*4 + 0x345c80]",
            "mov      dword ptr [eax*4 + 0x345cc0]",
            "call     0x2923f0                       ; -> sub_002923f0",
        ),
    },
    "sub_001A2A70": {
        "effect": "two-global-store",
        "required_fragments": (
            "mov      dword ptr [0x345d00]",
            "mov      dword ptr [0x345d08]",
        ),
    },
    "sub_001A2AB0": {
        "effect": "three-argument-forwarder",
        "required_fragments": (
            "push     eax",
            "push     ecx",
            "push     edx",
            "call     0x292bd0                       ; -> sub_00292bd0",
        ),
    },
    "sub_001A4B40": {
        "effect": "paired-lane-transform-and-state-helper",
        "required_fragments": (
            "call     0x254349                       ; -> sub_00254349",
            "call     0x1a2970                       ; -> sub_001a2970",
        ),
    },
    "sub_001A4BF0": {
        "effect": "paired-state-helper",
        "required_fragments": ("call     0x1a2970                       ; -> sub_001a2970",),
    },
    "sub_0019A960": {
        "effect": "single-load-and-store-helper",
        "required_fragments": (
            "mov      eax, dword ptr [ecx]",
            "mov      dword ptr [ecx], eax",
        ),
    },
    "sub_0019CA10": {
        "effect": "single-load-and-store-helper",
        "required_fragments": (
            "mov      eax, dword ptr [ecx]",
            "mov      dword ptr [eax], ecx",
        ),
    },
    "sub_0019C130": {
        "effect": "indexed-global-state-helper",
        "required_fragments": (
            "call     0x28fc70                       ; -> sub_0028fc70",
            "mov      dword ptr [eax*4 + 0x345ae0]",
            "mov      dword ptr [eax*4 + 0x345b00]",
        ),
    },
    "sub_0019ACE0": {
        "effect": "global-state-reset-and-helper",
        "required_fragments": (
            "call     0x28fc70                       ; -> sub_0028fc70",
            "mov      dword ptr [0x345ae0]",
            "mov      dword ptr [0x345ae4]",
            "mov      dword ptr [0x345ae8]",
            "mov      dword ptr [0x345aec]",
        ),
    },
    "sub_00254349": {
        "effect": "four-source-lanes-to-eight-destination-lanes",
        "required_fragments": (
            "movaps   xmm0, xmmword ptr [ecx]",
            "movaps   xmm2, xmmword ptr [ecx + 0x10]",
            "movaps   xmm3, xmmword ptr [ecx + 0x20]",
            "movaps   xmm5, xmmword ptr [ecx + 0x30]",
            "movlps   qword ptr [eax]",
            "movhps   qword ptr [eax + 0x38]",
        ),
    },
}


def _consumer_call_counts() -> dict[str, dict[str, int]]:
    counts: dict[str, dict[str, int]] = {
        name: {"consumer_functions": 0, "consumer_call_sites": 0}
        for name in EXPECTED_HELPER_EFFECTS
    }
    for calls in EXPECTED_CONSUMER_CALLS.values():
        for helper in calls:
            if helper not in counts:
                continue
            counts[helper]["consumer_functions"] += 1
            counts[helper]["consumer_call_sites"] += 1
    return counts


def verify_helper_effects(blocks: dict[str, dict[str, Any]]) -> list[dict[str, Any]]:
    """Verify each observed helper body against its bounded shape contract."""
    call_counts = _consumer_call_counts()
    rows: list[dict[str, Any]] = []
    for name, expected in EXPECTED_HELPER_EFFECTS.items():
        block = blocks.get(name)
        if block is None:
            raise ValueError(f"missing selector helper function: {name}")
        body = block["body_text"]
        missing = [fragment for fragment in expected["required_fragments"] if fragment not in body]
        if missing:
            raise ValueError(f"selector helper effect drift in {name}: missing={missing!r}")
        rows.append(
            {
                "helper": name,
                "effect": expected["effect"],
                "start": f"0x{block['start']:08X}",
                "end": f"0x{block['end']:08X}",
                "required_fragments": len(expected["required_fragments"]),
                **call_counts[name],
            }
        )
    return rows


def audit(asm_path: Path, xbe_path: Path) -> dict[str, Any]:
    """Verify helper effects and the selector-consumer linkage counts."""
    xbe_sha256 = hashlib.sha256(xbe_path.read_bytes()).hexdigest().upper()
    if xbe_sha256 != EXPECTED_XBE_SHA256:
        raise ValueError(
            f"XBE SHA-256 mismatch: expected={EXPECTED_XBE_SHA256}, observed={xbe_sha256}"
        )
    rows = verify_helper_effects(
        {block["name"]: block for block in recover_function_blocks(asm_path)}
    )
    return {
        "summary": {
            "status": "verified-static-eagl-selector-helper-effects",
            "helper_count": len(rows),
            "consumer_call_sites": sum(row["consumer_call_sites"] for row in rows),
            "consumer_helper_call_edges": sum(
                row["consumer_functions"] for row in rows
            ),
            "runtime_selected_buffer": "not-proven",
            "rendering_binding": "not-proven",
            "original_semantics": "not-claimed",
            "xbe_sha256": xbe_sha256,
        },
        "helpers": rows,
    }


def _parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--asm", type=Path, required=True)
    parser.add_argument("--xbe", type=Path, required=True)
    parser.add_argument("--format", choices=("json", "text"), default="text")
    return parser


def main() -> int:
    args = _parser().parse_args()
    result = audit(args.asm, args.xbe)
    if args.format == "json":
        print(json.dumps(result, indent=2, sort_keys=True))
    else:
        for key, value in result["summary"].items():
            print(f"{key}={value}")
        for row in result["helpers"]:
            print(
                "selector_helper={helper} effect={effect} start={start} end={end} "
                "consumer_functions={consumer_functions} consumer_call_sites={consumer_call_sites}".format(
                    **row
                )
            )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
