"""Verify the static contract of kernel services used by the direct subgraph.

The gate joins the indirect-call audit with the supplied host kernel source.
It checks resolver/bridge coverage, source signatures, observed argument use,
and the exact service bodies that still contain explicit stubs or
``STATUS_NOT_IMPLEMENTED`` markers.  It does not claim Xbox semantics and
never executes the XBE or generated code.
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


EXPECTED_SUMMARY = {
    "status": "verified-static-indirect-target-classification",
    "kernel_thunk_slot_sites": 48,
    "kernel_resolver_status": "verified-explicit-case-coverage",
    "kernel_resolver_case_sites": 48,
    "kernel_resolver_missing_sites": 0,
    "kernel_bridge_status": "verified-explicit-case-coverage",
    "kernel_bridge_case_sites": 48,
    "kernel_bridge_missing_sites": 0,
    "kernel_bridge_body_targets": 19,
    "kernel_bridge_body_defined_targets": 19,
    "kernel_bridge_body_sites": 48,
    "kernel_bridge_body_missing_sites": 0,
    "kernel_bridge_body_stack_index_sites": 47,
    "kernel_bridge_body_stack_index_outside_sites": 0,
    "kernel_bridge_body_stack_index_gap_sites": 0,
    "kernel_bridge_body_return_disposition_missing_sites": 0,
    "kernel_bridge_delegation_targets": 19,
    "kernel_bridge_argument_use_targets": 19,
    "kernel_bridge_argument_use_outside_expected_targets": 0,
    "kernel_bridge_argument_use_missing_targets": 0,
    "kernel_implementation_status": "verified-source-definitions",
    "kernel_implementation_signature_status": (
        "verified-source-signature-parameter-counts"
    ),
    "kernel_implementation_signature_targets": 19,
    "kernel_implementation_signature_match_targets": 19,
    "kernel_implementation_signature_mismatch_targets": 0,
    "kernel_implementation_signature_match_sites": 48,
    "kernel_implementation_signature_mismatch_sites": 0,
    "kernel_implementation_targets": 19,
    "kernel_implementation_defined_targets": 19,
    "kernel_implementation_body_marker_target_count": 5,
    "kernel_implementation_explicit_stub_or_noop_target_count": 2,
    "kernel_implementation_status_not_implemented_token_target_count": 3,
    "kernel_symbolic_link_lifecycle_functions": 1,
    "kernel_symbolic_link_lifecycle_status": "open-query-close-order-observed",
    "kernel_symbolic_link_guest_path_sites": 1,
    "kernel_symbolic_link_host_query_target": r"\Device\CdRom0",
    "kernel_symbolic_link_host_bridge_status": (
        "bridge-path-and-handle-validation-observed"
    ),
    "kernel_file_info_class_status": "verified-observed-class-cases",
    "kernel_file_info_class_sites": 3,
    "kernel_file_info_class_covered_sites": 3,
    "kernel_file_info_class_missing_sites": 0,
}

EXPECTED_MARKER_TARGETS = {
    "xbox_MmPersistContiguousMemory",
    "xbox_NtOpenSymbolicLinkObject",
    "xbox_NtQueryInformationFile",
    "xbox_NtSetInformationFile",
    "xbox_RtlNtStatusToDosError",
}
EXPECTED_EXPLICIT_STUB_TARGETS = {
    "xbox_MmPersistContiguousMemory",
    "xbox_NtOpenSymbolicLinkObject",
}
EXPECTED_NOT_IMPLEMENTED_TARGETS = {
    "xbox_NtQueryInformationFile",
    "xbox_NtSetInformationFile",
    "xbox_RtlNtStatusToDosError",
}


def _call_signature(row: dict[str, Any]) -> tuple[Any, ...]:
    constants = row.get("constant_arguments", {})
    return (
        row.get("call_site"),
        row.get("kernel_ordinal"),
        row.get("target"),
        row.get("pushed_bytes"),
        tuple(sorted(constants.items())) if isinstance(constants, dict) else (),
    )


EXPECTED_EXPLICIT_STUB_CALLS = {
    (
        "0x0024D3F0",
        178,
        "xbox_MmPersistContiguousMemory",
        12,
        (("1", "0x00001000"), ("2", "0x00000001")),
    ),
    ("0x00250244", 203, "xbox_NtOpenSymbolicLinkObject", 8, ()),
}
EXPECTED_NOT_IMPLEMENTED_CALLS = {
    ("0x0024CCAB", 301, "xbox_RtlNtStatusToDosError", None, ()),
    ("0x0024D4D3", 301, "xbox_RtlNtStatusToDosError", None, ()),
    ("0x0024E41B", 211, "xbox_NtQueryInformationFile", None, ()),
    ("0x0024E441", 211, "xbox_NtQueryInformationFile", None, ()),
    ("0x0024E49E", 226, "xbox_NtSetInformationFile", None, ()),
    ("0x0025061E", 301, "xbox_RtlNtStatusToDosError", None, ()),
}


def _require_equal(
    failures: list[str], key: str, actual: Any, expected: Any
) -> None:
    if actual != expected:
        failures.append(f"{key}: expected {expected!r}, got {actual!r}")


def verify_gate(result: dict[str, Any]) -> dict[str, Any]:
    """Verify the exact static service contract and risk classification."""
    summary = result.get("summary")
    if not isinstance(summary, dict):
        raise ValueError("indirect-call audit result has no summary object")

    failures: list[str] = []
    for key, expected in EXPECTED_SUMMARY.items():
        _require_equal(failures, key, summary.get(key), expected)

    _require_equal(
        failures,
        "kernel implementation marker targets",
        set(summary.get("kernel_implementation_body_marker_targets", [])),
        EXPECTED_MARKER_TARGETS,
    )
    _require_equal(
        failures,
        "explicit stub targets",
        set(summary.get("kernel_implementation_explicit_stub_or_noop_targets", [])),
        EXPECTED_EXPLICIT_STUB_TARGETS,
    )
    _require_equal(
        failures,
        "STATUS_NOT_IMPLEMENTED targets",
        set(summary.get("kernel_implementation_status_not_implemented_token_targets", [])),
        EXPECTED_NOT_IMPLEMENTED_TARGETS,
    )

    explicit_calls = {
        _call_signature(row)
        for row in summary.get("kernel_implementation_explicit_stub_or_noop_call_sites", [])
    }
    not_implemented_calls = {
        _call_signature(row)
        for row in summary.get("kernel_implementation_status_not_implemented_call_sites", [])
    }
    _require_equal(failures, "explicit stub call sites", explicit_calls, EXPECTED_EXPLICIT_STUB_CALLS)
    _require_equal(
        failures,
        "STATUS_NOT_IMPLEMENTED call sites",
        not_implemented_calls,
        EXPECTED_NOT_IMPLEMENTED_CALLS,
    )
    _require_equal(
        failures,
        "missing implementation targets",
        summary.get("kernel_implementation_missing_targets"),
        [],
    )
    _require_equal(
        failures,
        "original semantics",
        summary.get("original_semantics"),
        "not-claimed",
    )

    if failures:
        raise ValueError("kernel service contract gate failed: " + "; ".join(failures))

    return {
        "status": "verified-static-kernel-service-contract",
        "kernel_sites": 48,
        "implementation_targets": 19,
        "explicit_stub_targets": 2,
        "status_not_implemented_targets": 3,
        "host_service_semantics": "not-proven",
        "original_semantics": "not-claimed",
        "guest_execution": "none",
    }


def _parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--functions", type=Path, required=True)
    parser.add_argument("--generated", type=Path, required=True)
    parser.add_argument("--xbe", type=Path, required=True)
    parser.add_argument("--kernel-source", type=Path, required=True)
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
            "kernel_service_contract_gate: verified static coverage and risk "
            "classification without guest execution or semantic promotion"
        )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
