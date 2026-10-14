"""Tests for the conservative dynamic callback evidence gate."""

from __future__ import annotations

import unittest

from tools.analysis.recomp_dynamic_callback_evidence_gate import verify_gate


EXPECTED_SITES = {
    "0x0024D3B1",
    "0x0024D4C7",
    "0x00250612",
    "0x002512A4",
    "0x00252C7D",
    "0x00252CF1",
    "0x0026E75E",
}


def _result() -> dict:
    family_counts = {
        "dynamic-memory-target": 1,
        "dynamic-object-field-target": 1,
        "dynamic-indexed-record-target": 1,
        "prcb-guarded-dynamic-callback": 4,
    }
    initialization_counts = {
        "static-image-zero-guarded": 1,
        "host-fake-prcb-zeroed-guarded": 4,
        "runtime-object-field-initialization-unproven": 1,
        "runtime-indexed-record-initialization-unproven": 1,
    }
    summary = {
        "status": "verified-static-indirect-target-classification",
        "unresolved_dynamic_sites": 7,
        "dynamic_target_family_counts": family_counts,
        "dynamic_callback_initialization_status": (
            "verified-conservative-dynamic-callback-initialization-classification"
        ),
        "dynamic_callback_initialization_sites": 7,
        "dynamic_callback_initialization_class_counts": initialization_counts,
        "dynamic_callback_runtime_initialization_unproven_sites": 2,
        "dynamic_callback_initialization_unclassified_sites": 0,
        "dynamic_source_provenance_sites": 7,
        "dynamic_source_provenance_complete_sites": 7,
        "dynamic_source_provenance_missing_sites": 0,
        "dynamic_call_stack_shape_sites": 7,
        "dynamic_call_stack_shape_missing_sites": 0,
        "dynamic_conditional_guard_sites": 7,
        "dynamic_conditional_guard_missing_sites": 0,
        "dynamic_target_direct_writer_sites": 0,
        "dynamic_target_direct_writer_status": "no-proven-direct-writer",
        "prcb_guarded_dynamic_callback_sites": 4,
        "prcb_cache_direct_writer_sites": 0,
        "dynamic_object_callback_producer_status": (
            "verified-object-callback-parameter-boundary"
        ),
        "dynamic_indexed_callback_producer_status": (
            "verified-indexed-callback-frame-field-boundary"
        ),
        "dynamic_indexed_callback_table_status": (
            "verified-static-indexed-table-root-provenance"
        ),
    }
    classifications = [
        "dynamic-memory-target",
        "dynamic-object-field-target",
        "dynamic-indexed-record-target",
        "prcb-guarded-dynamic-callback",
        "prcb-guarded-dynamic-callback",
        "prcb-guarded-dynamic-callback",
        "prcb-guarded-dynamic-callback",
    ]
    return {
        "summary": summary,
        "sites": [
            {"call_site": site, "classification": classification}
            for site, classification in zip(sorted(EXPECTED_SITES), classifications)
        ],
    }


class DynamicCallbackEvidenceGateTests(unittest.TestCase):
    def test_accepts_current_conservative_contract(self) -> None:
        gate = verify_gate(_result())

        self.assertEqual(gate["status"], "verified-dynamic-callback-evidence-contract")
        self.assertEqual(gate["dynamic_call_sites"], 7)
        self.assertFalse(gate["dynamic_callback_targets_resolved"])

    def test_rejects_new_unclassified_site(self) -> None:
        result = _result()
        result["summary"]["dynamic_callback_initialization_unclassified_sites"] = 1

        with self.assertRaisesRegex(ValueError, "unclassified initialization sites"):
            verify_gate(result)

    def test_rejects_missing_callback_site(self) -> None:
        result = _result()
        result["sites"] = result["sites"][:-1]

        with self.assertRaisesRegex(ValueError, "dynamic call sites"):
            verify_gate(result)


if __name__ == "__main__":
    unittest.main()
