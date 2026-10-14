"""Tests for the joined static translation-coverage gate."""

from __future__ import annotations

import copy
import unittest

from tools.analysis.recomp_translation_coverage_gate import (
    EXPECTED_CALL_GRAPH_SUMMARY,
    EXPECTED_UNIMPLEMENTED_SITES,
    verify_gate,
)


def _result() -> dict:
    call_graph_summary = dict(EXPECTED_CALL_GRAPH_SUMMARY)
    call_graph_summary.update(
        {
            "status": "verified-direct-call-translation-coverage",
            "original_semantics": "not-claimed",
        }
    )
    sites = [
        {"address": address, "instruction": instruction}
        for address, instruction in sorted(EXPECTED_UNIMPLEMENTED_SITES)
    ]
    return {
        "call_graph": {"summary": call_graph_summary},
        "alignment": {
            "summary": {
                "status": "verified-unimplemented-evidence-alignment",
                "summary_sites": 2,
                "generated_sites": 2,
                "asm_sites": 2,
            },
            "sites": sites,
        },
        "context": {
            "summary": {
                "status": "verified-static-unimplemented-opcode-contexts",
                "site_count": 2,
                "hardware_semantics": "not-proven",
                "lowering_status": "2-sites-remain-unimplemented",
            },
            "sites": sites,
        },
    }


class RecompTranslationCoverageGateTests(unittest.TestCase):
    def test_accepts_current_joined_contract(self) -> None:
        gate = verify_gate(_result())

        self.assertEqual(
            gate["status"], "verified-recompilation-translation-coverage-contract"
        )
        self.assertEqual(gate["direct_reachable_untranslated_sites"], 0)

    def test_rejects_an_untranslated_site_in_direct_scope(self) -> None:
        result = _result()
        result["call_graph"]["summary"][
            "unimplemented_sites_in_directly_reachable_function_scopes"
        ] = 1

        with self.assertRaisesRegex(ValueError, "directly_reachable"):
            verify_gate(result)

    def test_rejects_alignment_drift(self) -> None:
        result = copy.deepcopy(_result())
        result["alignment"]["sites"][0]["instruction"] = "out dx, bl"

        with self.assertRaisesRegex(ValueError, "aligned untranslated sites"):
            verify_gate(result)


if __name__ == "__main__":
    unittest.main()
