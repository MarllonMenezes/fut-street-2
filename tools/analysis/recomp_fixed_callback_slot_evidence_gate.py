"""Gate the static evidence contract for the fixed callback slots.

The gate checks the generated-artifact inventory for the three fixed BSS
callback slots currently observed by the project.  It records the two slots
without a proven writer separately from the slot whose function initializer
is known.  It never executes the XBE, generated C, or host runtime.
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path
from typing import Any

try:
    from .recomp_indirect_call_audit import audit
except ImportError:  # pragma: no cover - supports direct script execution
    from recomp_indirect_call_audit import audit


EXPECTED_WRITER_COUNTS = {
    "0x00360A70": 0,
    "0x00360AA8": 0,
    "0x00360AB4": 1,
}
EXPECTED_CALL_SITES = {
    (
        "0x00360A70",
        "0x00274FC5",
        "runtime-initialized-fixed-slot",
    ),
    (
        "0x00360AA8",
        "0x00276E59",
        "runtime-initialized-fixed-slot",
    ),
    (
        "0x00360AB4",
        "0x00277532",
        "runtime-initialized-known-function-pointer",
    ),
}


def _require_equal(
    failures: list[str], key: str, actual: Any, expected: Any
) -> None:
    if actual != expected:
        failures.append(f"{key}: expected {expected!r}, got {actual!r}")


def verify_gate(result: dict[str, Any]) -> dict[str, Any]:
    """Verify the full-generated-C fixed-slot writer and call-site contract."""
    summary = result.get("summary")
    sites = result.get("sites")
    if not isinstance(summary, dict):
        raise ValueError("indirect-call audit result has no summary object")
    if not isinstance(sites, list):
        raise ValueError("indirect-call audit result has no sites list")

    failures: list[str] = []
    observed_call_sites = {
        (
            row.get("fixed_slot"),
            row.get("call_site"),
            row.get("classification"),
        )
        for row in sites
        if row.get("fixed_slot") in EXPECTED_WRITER_COUNTS
    }

    _require_equal(
        failures,
        "audit status",
        summary.get("status"),
        "verified-static-indirect-target-classification",
    )
    _require_equal(
        failures,
        "fixed-slot writer counts",
        summary.get("runtime_fixed_slot_all_writer_counts"),
        EXPECTED_WRITER_COUNTS,
    )
    _require_equal(
        failures,
        "fixed-slot call sites",
        observed_call_sites,
        EXPECTED_CALL_SITES,
    )
    _require_equal(
        failures,
        "runtime initialized fixed-slot sites",
        summary.get("runtime_initialized_fixed_slot_sites"),
        2,
    )
    _require_equal(
        failures,
        "runtime initialized known-function sites",
        summary.get("runtime_initialized_known_function_pointer_sites"),
        1,
    )
    _require_equal(
        failures,
        "fixed known-function pointer sites",
        summary.get("fixed_known_function_pointer_sites"),
        4,
    )

    if failures:
        raise ValueError("fixed callback slot evidence gate failed: " + "; ".join(failures))

    return {
        "status": "verified-fixed-callback-slot-evidence-contract",
        "original_semantics": "not-claimed",
        "fixed_callback_slots": 3,
        "slots_without_proven_generated_writer": 2,
        "known_function_pointer_slots": 1,
        "runtime_target_resolution": "not-claimed-for-two-bss-slots",
        "guest_execution": "none",
    }


def _parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--functions", type=Path, required=True)
    parser.add_argument("--generated", type=Path, required=True)
    parser.add_argument("--xbe", type=Path, required=True)
    parser.add_argument(
        "--kernel-source",
        type=Path,
        help="optional host kernel_thunks.c to retain resolver/ABI evidence",
    )
    parser.add_argument("--entry", type=lambda value: int(value, 0), default=0x0024D8BE)
    parser.add_argument("--format", choices=("json", "text"), default="text")
    return parser


def main() -> int:
    args = _parser().parse_args()
    result = audit(
        args.functions,
        args.generated,
        args.xbe,
        args.entry,
        args.kernel_source,
    )
    gate = verify_gate(result)
    if args.format == "json":
        print(json.dumps({"gate": gate, "audit": result}, indent=2, sort_keys=True))
    else:
        for key, value in gate.items():
            print(f"{key}={value}")
        print(
            "fixed_callback_slot_evidence_gate: verified without guest execution "
            "or runtime target resolution"
        )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
