"""Gate the static evidence contract for the generated dynamic callbacks.

This is a project-specific regression gate over
``recomp_indirect_call_audit.audit``.  It checks that the full generated
artifact still has the seven known dynamic callback sites, complete local
source/stack/guard evidence, and the same conservative initialization
classification.  It deliberately does not resolve a callback target and
never executes the XBE, generated C, or a host runtime.
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


EXPECTED_DYNAMIC_CALL_SITES = {
    "0x0024D3B1",
    "0x0024D4C7",
    "0x00250612",
    "0x002512A4",
    "0x00252C7D",
    "0x00252CF1",
    "0x0026E75E",
}
EXPECTED_DYNAMIC_FAMILY_COUNTS = {
    "dynamic-memory-target": 1,
    "dynamic-object-field-target": 1,
    "dynamic-indexed-record-target": 1,
    "prcb-guarded-dynamic-callback": 4,
}
EXPECTED_INITIALIZATION_COUNTS = {
    "static-image-zero-guarded": 1,
    "host-fake-prcb-zeroed-guarded": 4,
    "runtime-object-field-initialization-unproven": 1,
    "runtime-indexed-record-initialization-unproven": 1,
}


def _require_equal(
    failures: list[str], key: str, actual: Any, expected: Any
) -> None:
    if actual != expected:
        failures.append(f"{key}: expected {expected!r}, got {actual!r}")


def verify_gate(result: dict[str, Any]) -> dict[str, Any]:
    """Verify the conservative full-artifact callback evidence contract."""
    summary = result.get("summary")
    if not isinstance(summary, dict):
        raise ValueError("indirect-call audit result has no summary object")

    failures: list[str] = []
    sites = result.get("sites")
    if not isinstance(sites, list):
        raise ValueError("indirect-call audit result has no sites list")

    dynamic_sites = {
        row.get("call_site")
        for row in sites
        if row.get("classification") in {
            "dynamic-memory-target",
            "dynamic-object-field-target",
            "dynamic-indexed-record-target",
            "prcb-guarded-dynamic-callback",
        }
    }

    _require_equal(
        failures,
        "audit status",
        summary.get("status"),
        "verified-static-indirect-target-classification",
    )
    _require_equal(
        failures,
        "dynamic call sites",
        dynamic_sites,
        EXPECTED_DYNAMIC_CALL_SITES,
    )
    _require_equal(
        failures,
        "unresolved dynamic sites",
        summary.get("unresolved_dynamic_sites"),
        len(EXPECTED_DYNAMIC_CALL_SITES),
    )
    _require_equal(
        failures,
        "dynamic target family counts",
        summary.get("dynamic_target_family_counts"),
        EXPECTED_DYNAMIC_FAMILY_COUNTS,
    )
    _require_equal(
        failures,
        "dynamic initialization status",
        summary.get("dynamic_callback_initialization_status"),
        "verified-conservative-dynamic-callback-initialization-classification",
    )
    _require_equal(
        failures,
        "dynamic initialization site count",
        summary.get("dynamic_callback_initialization_sites"),
        len(EXPECTED_DYNAMIC_CALL_SITES),
    )
    _require_equal(
        failures,
        "dynamic initialization class counts",
        summary.get("dynamic_callback_initialization_class_counts"),
        EXPECTED_INITIALIZATION_COUNTS,
    )
    _require_equal(
        failures,
        "runtime initialization unproven sites",
        summary.get("dynamic_callback_runtime_initialization_unproven_sites"),
        2,
    )
    _require_equal(
        failures,
        "unclassified initialization sites",
        summary.get("dynamic_callback_initialization_unclassified_sites"),
        0,
    )
    _require_equal(
        failures,
        "dynamic source provenance sites",
        summary.get("dynamic_source_provenance_sites"),
        len(EXPECTED_DYNAMIC_CALL_SITES),
    )
    _require_equal(
        failures,
        "complete dynamic source provenance sites",
        summary.get("dynamic_source_provenance_complete_sites"),
        len(EXPECTED_DYNAMIC_CALL_SITES),
    )
    _require_equal(
        failures,
        "missing dynamic source provenance sites",
        summary.get("dynamic_source_provenance_missing_sites"),
        0,
    )
    _require_equal(
        failures,
        "dynamic stack-shape sites",
        summary.get("dynamic_call_stack_shape_sites"),
        len(EXPECTED_DYNAMIC_CALL_SITES),
    )
    _require_equal(
        failures,
        "missing dynamic stack-shape sites",
        summary.get("dynamic_call_stack_shape_missing_sites"),
        0,
    )
    _require_equal(
        failures,
        "dynamic conditional guard sites",
        summary.get("dynamic_conditional_guard_sites"),
        len(EXPECTED_DYNAMIC_CALL_SITES),
    )
    _require_equal(
        failures,
        "missing dynamic conditional guard sites",
        summary.get("dynamic_conditional_guard_missing_sites"),
        0,
    )
    _require_equal(
        failures,
        "direct dynamic writer sites",
        summary.get("dynamic_target_direct_writer_sites"),
        0,
    )
    _require_equal(
        failures,
        "direct dynamic writer status",
        summary.get("dynamic_target_direct_writer_status"),
        "no-proven-direct-writer",
    )
    _require_equal(
        failures,
        "PRCB guarded callback sites",
        summary.get("prcb_guarded_dynamic_callback_sites"),
        4,
    )
    _require_equal(
        failures,
        "PRCB cache direct writer sites",
        summary.get("prcb_cache_direct_writer_sites"),
        0,
    )
    _require_equal(
        failures,
        "object callback producer status",
        summary.get("dynamic_object_callback_producer_status"),
        "verified-object-callback-parameter-boundary",
    )
    _require_equal(
        failures,
        "indexed callback producer status",
        summary.get("dynamic_indexed_callback_producer_status"),
        "verified-indexed-callback-frame-field-boundary",
    )
    _require_equal(
        failures,
        "indexed callback table status",
        summary.get("dynamic_indexed_callback_table_status"),
        "verified-static-indexed-table-root-provenance",
    )

    if failures:
        raise ValueError("dynamic callback evidence gate failed: " + "; ".join(failures))

    return {
        "status": "verified-dynamic-callback-evidence-contract",
        "original_semantics": "not-claimed",
        "dynamic_call_sites": len(dynamic_sites),
        "runtime_initialization_unproven_sites": 2,
        "direct_dynamic_writer_sites": 0,
        "dynamic_callback_targets_resolved": False,
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
            "dynamic_callback_evidence_gate: verified "
            "without guest execution or callback target resolution"
        )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
