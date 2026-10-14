"""Tests for the cross-report EAGL static render-chain gate."""

from __future__ import annotations

import unittest

from tools.analysis.eagl_static_render_chain_control import EXPECTED, verify_chain


def _synthetic_summaries() -> dict[str, dict[str, str]]:
    common = {"xbe_sha256": "5FABB5B763BE0CC8823E814EE343AC5BE6950305330F46C5170E08EAADEA910B"}
    result = {name: {**values, **common} for name, values in EXPECTED.items()}
    result["draw"].update({"draw_rows": "2364"})
    result["parameter"].update({"primitive_rows": "2364", "draw_commands": "2364", "draw_condition_nonzero": "2364"})
    result["model"].update({"model_rows": "2364", "declarations_matched": "2364"})
    return result


class EaglStaticRenderChainControlTests(unittest.TestCase):
    def test_verifies_cross_report_contract(self) -> None:
        gate = verify_chain(_synthetic_summaries())

        self.assertEqual(gate["status"], "verified-static-eagl-render-chain-control")
        self.assertEqual(gate["draw_rows"], 2364)
        self.assertEqual(gate["cross_report_joins"], 8)
        self.assertEqual(gate["full_rendering_binding"], "not-proven")

    def test_rejects_primitive_count_drift(self) -> None:
        summaries = _synthetic_summaries()
        summaries["model"]["model_rows"] = "2363"

        with self.assertRaisesRegex(ValueError, "parameter/model primitive join"):
            verify_chain(summaries)

    def test_rejects_xbe_identity_drift(self) -> None:
        summaries = _synthetic_summaries()
        summaries["material"]["xbe_sha256"] = "0" * 64

        with self.assertRaisesRegex(ValueError, "material XBE identity"):
            verify_chain(summaries)

    def test_rejects_semantic_promotion(self) -> None:
        summaries = _synthetic_summaries()
        summaries["draw"]["full_rendering_binding"] = "proven"

        with self.assertRaisesRegex(ValueError, "draw.full_rendering_binding"):
            verify_chain(summaries)


if __name__ == "__main__":
    unittest.main()
