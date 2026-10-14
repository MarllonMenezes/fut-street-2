"""Tests for the fixed callback slot evidence gate."""

from __future__ import annotations

import unittest

from tools.analysis.recomp_fixed_callback_slot_evidence_gate import verify_gate


def _result() -> dict:
    return {
        "summary": {
            "status": "verified-static-indirect-target-classification",
            "runtime_fixed_slot_all_writer_counts": {
                "0x00360A70": 0,
                "0x00360AA8": 0,
                "0x00360AB4": 1,
            },
            "runtime_initialized_fixed_slot_sites": 2,
            "runtime_initialized_known_function_pointer_sites": 1,
            "fixed_known_function_pointer_sites": 4,
        },
        "sites": [
            {
                "fixed_slot": "0x00360A70",
                "call_site": "0x00274FC5",
                "classification": "runtime-initialized-fixed-slot",
            },
            {
                "fixed_slot": "0x00360AA8",
                "call_site": "0x00276E59",
                "classification": "runtime-initialized-fixed-slot",
            },
            {
                "fixed_slot": "0x00360AB4",
                "call_site": "0x00277532",
                "classification": "runtime-initialized-known-function-pointer",
            },
        ],
    }


class FixedCallbackSlotEvidenceGateTests(unittest.TestCase):
    def test_accepts_current_slot_contract(self) -> None:
        gate = verify_gate(_result())

        self.assertEqual(gate["status"], "verified-fixed-callback-slot-evidence-contract")
        self.assertEqual(gate["slots_without_proven_generated_writer"], 2)

    def test_rejects_a_new_writer_for_an_unresolved_slot(self) -> None:
        result = _result()
        result["summary"]["runtime_fixed_slot_all_writer_counts"]["0x00360A70"] = 1

        with self.assertRaisesRegex(ValueError, "fixed-slot writer counts"):
            verify_gate(result)

    def test_rejects_call_site_classification_drift(self) -> None:
        result = _result()
        result["sites"][1]["classification"] = "fixed-known-function-pointer"

        with self.assertRaisesRegex(ValueError, "fixed-slot call sites"):
            verify_gate(result)


if __name__ == "__main__":
    unittest.main()
