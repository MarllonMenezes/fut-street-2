"""Tests for bounded contexts around untranslated instructions."""

from __future__ import annotations

import unittest

from tools.analysis.recomp_unimplemented_context_control import (
    EXPECTED_UNIMPLEMENTED_CONTEXTS,
    verify_contexts,
)


def _synthetic_blocks() -> dict[str, dict[str, object]]:
    blocks: dict[str, dict[str, object]] = {}
    for index, (address, expected) in enumerate(
        EXPECTED_UNIMPLEMENTED_CONTEXTS.items()
    ):
        start = address - 0x20
        end = address + 0x20
        instruction = {
            "address": address,
            "text": f"0x{address:08x} bytes {expected['instruction']}",
            "source": "synthetic.asm:1",
        }
        blocks[expected["function"]] = {
            "name": expected["function"],
            "start": start,
            "end": end,
            "instructions": [instruction],
            "body_text": "\n".join(expected["required_fragments"]),
            "source": f"synthetic-{index}.asm",
        }
    return blocks


class RecompUnimplementedContextControlTests(unittest.TestCase):
    def test_verifies_both_unimplemented_contexts(self) -> None:
        rows = verify_contexts(_synthetic_blocks())

        self.assertEqual(len(rows), 2)
        self.assertEqual(
            {row["function"] for row in rows},
            {expected["function"] for expected in EXPECTED_UNIMPLEMENTED_CONTEXTS.values()},
        )
        self.assertEqual(
            {row["context"] for row in rows},
            {expected["context"] for expected in EXPECTED_UNIMPLEMENTED_CONTEXTS.values()},
        )

    def test_rejects_owner_drift(self) -> None:
        blocks = _synthetic_blocks()
        blocks["sub_00297F04"]["name"] = "sub_00BAD000"

        with self.assertRaisesRegex(ValueError, "owner drift"):
            verify_contexts(blocks)

    def test_rejects_context_fragment_drift(self) -> None:
        blocks = _synthetic_blocks()
        blocks["sub_00253FD0"]["body_text"] = "sgdt [esp + 6]"

        with self.assertRaisesRegex(ValueError, "context drift"):
            verify_contexts(blocks)

    def test_rejects_instruction_text_drift(self) -> None:
        blocks = _synthetic_blocks()
        blocks["sub_00297F04"]["instructions"][0]["text"] = (
            "0x00298030 bytes out dx, bl"
        )

        with self.assertRaisesRegex(ValueError, "instruction drift"):
            verify_contexts(blocks)


if __name__ == "__main__":
    unittest.main()
