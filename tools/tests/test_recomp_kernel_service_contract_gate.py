"""Tests for the static kernel-service contract gate."""

from __future__ import annotations

import unittest

from tools.analysis.recomp_kernel_service_contract_gate import (
    EXPECTED_EXPLICIT_STUB_CALLS,
    EXPECTED_EXPLICIT_STUB_TARGETS,
    EXPECTED_MARKER_TARGETS,
    EXPECTED_NOT_IMPLEMENTED_CALLS,
    EXPECTED_NOT_IMPLEMENTED_TARGETS,
    EXPECTED_SUMMARY,
    verify_gate,
)


def _synthetic_result() -> dict[str, object]:
    summary = dict(EXPECTED_SUMMARY)
    summary.update(
        {
            "original_semantics": "not-claimed",
            "kernel_implementation_body_marker_targets": sorted(EXPECTED_MARKER_TARGETS),
            "kernel_implementation_explicit_stub_or_noop_targets": sorted(
                EXPECTED_EXPLICIT_STUB_TARGETS
            ),
            "kernel_implementation_status_not_implemented_token_targets": sorted(
                EXPECTED_NOT_IMPLEMENTED_TARGETS
            ),
            "kernel_implementation_explicit_stub_or_noop_call_sites": [
                {
                    "call_site": call_site,
                    "kernel_ordinal": ordinal,
                    "target": target,
                    "pushed_bytes": pushed_bytes,
                    "constant_arguments": dict(constants),
                }
                for call_site, ordinal, target, pushed_bytes, constants in EXPECTED_EXPLICIT_STUB_CALLS
            ],
            "kernel_implementation_status_not_implemented_call_sites": [
                {
                    "call_site": call_site,
                    "kernel_ordinal": ordinal,
                    "target": target,
                    "constant_arguments": dict(constants),
                }
                for call_site, ordinal, target, pushed_bytes, constants in EXPECTED_NOT_IMPLEMENTED_CALLS
            ],
            "kernel_implementation_missing_targets": [],
        }
    )
    return {"summary": summary}


class RecompKernelServiceContractGateTests(unittest.TestCase):
    def test_verifies_static_service_contract(self) -> None:
        gate = verify_gate(_synthetic_result())

        self.assertEqual(gate["status"], "verified-static-kernel-service-contract")
        self.assertEqual(gate["kernel_sites"], 48)
        self.assertEqual(gate["implementation_targets"], 19)
        self.assertEqual(gate["explicit_stub_targets"], 2)
        self.assertEqual(gate["status_not_implemented_targets"], 3)
        self.assertEqual(gate["host_service_semantics"], "not-proven")

    def test_rejects_missing_bridge_case(self) -> None:
        result = _synthetic_result()
        result["summary"]["kernel_bridge_missing_sites"] = 1

        with self.assertRaisesRegex(ValueError, "kernel_bridge_missing_sites"):
            verify_gate(result)

    def test_rejects_risk_target_drift(self) -> None:
        result = _synthetic_result()
        result["summary"]["kernel_implementation_status_not_implemented_token_targets"] = [
            "xbox_NtQueryInformationFile"
        ]

        with self.assertRaisesRegex(ValueError, "STATUS_NOT_IMPLEMENTED targets"):
            verify_gate(result)

    def test_rejects_semantic_promotion(self) -> None:
        result = _synthetic_result()
        result["summary"]["original_semantics"] = "claimed"

        with self.assertRaisesRegex(ValueError, "original semantics"):
            verify_gate(result)


if __name__ == "__main__":
    unittest.main()
