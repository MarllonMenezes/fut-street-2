"""Tests for static EAGL selector helper-effect contracts."""

from __future__ import annotations

import unittest

from tools.analysis.eagl_selector_helper_effect_control import (
    EXPECTED_HELPER_EFFECTS,
    verify_helper_effects,
)


def _synthetic_blocks() -> dict[str, dict[str, object]]:
    return {
        name: {
            "start": 0x1000 + index * 0x10,
            "end": 0x1000 + index * 0x10 + 8,
            "body_text": "\n".join(expected["required_fragments"]),
        }
        for index, (name, expected) in enumerate(EXPECTED_HELPER_EFFECTS.items())
    }


class EaglSelectorHelperEffectControlTests(unittest.TestCase):
    def test_verifies_all_helper_effect_contracts(self) -> None:
        rows = verify_helper_effects(_synthetic_blocks())

        self.assertEqual(len(rows), 12)
        self.assertEqual(sum(row["consumer_call_sites"] for row in rows), 40)
        self.assertEqual(
            {row["effect"] for row in rows},
            {expected["effect"] for expected in EXPECTED_HELPER_EFFECTS.values()},
        )

    def test_rejects_missing_effect_fragment(self) -> None:
        blocks = _synthetic_blocks()
        blocks["sub_001A2A70"]["body_text"] = "mov dword ptr [0x345d00]"

        with self.assertRaisesRegex(ValueError, "effect drift"):
            verify_helper_effects(blocks)

    def test_rejects_missing_helper_function(self) -> None:
        blocks = _synthetic_blocks()
        del blocks["sub_00254349"]

        with self.assertRaisesRegex(ValueError, "missing selector helper function"):
            verify_helper_effects(blocks)


if __name__ == "__main__":
    unittest.main()
