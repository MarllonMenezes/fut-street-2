"""Tests for the dynamic EAGL buffer selector writer/consumer control."""

from __future__ import annotations

import tempfile
import unittest
from pathlib import Path

from tools.analysis.eagl_dynamic_buffer_selector_control import (
    EXPECTED_CONSUMERS,
    EXPECTED_CONSUMER_CALLS,
    EXPECTED_CONSUMER_FUNCTIONS,
    classify_consumers,
    classify_writers,
    recover_consumer_rows,
    recover_writer_rows,
)


ASM = "\n".join(
    [
        "  0x0019E91B  890dcc5b3400            mov      dword ptr [0x00345BCC], ecx",
        "  0x0019F20F  c705cc5b340000000000    mov      dword ptr [0x00345BCC], 0",
        "  0x0019FCFF  a3cc5b3400              mov      dword ptr [0x00345BCC], eax",
        "  0x001A2FCE  a3cc5b3400              mov      dword ptr [0x00345BCC], eax",
        "  0x001A30FB  8915cc5b3400            mov      dword ptr [0x00345BCC], edx",
        "  0x001A3E08  0faf05cc5b3400          imul     eax, dword ptr [0x345bcc]",
        "  0x001A3E46  8b15cc5b3400            mov      edx, dword ptr [0x345bcc]",
    ]
)


class EaglDynamicBufferSelectorControlTests(unittest.TestCase):
    def test_classifies_all_writer_shapes(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            path = Path(temporary) / "text.asm"
            path.write_text(ASM, encoding="utf-8")
            rows = classify_writers(recover_writer_rows(path))

        self.assertEqual(len(rows), 5)
        self.assertEqual(
            {row["form"] for row in rows},
            {"signed-table-word", "literal-zero", "object-field-plus-0xC0"},
        )
        self.assertEqual(
            sum(row["form"] == "object-field-plus-0xC0" for row in rows), 3
        )

    def test_rejects_a_new_selector_writer(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            path = Path(temporary) / "text.asm"
            path.write_text(
                ASM
                + "\n  0x00123456  00000000            mov      dword ptr [0x00345BCC], eax\n",
                encoding="utf-8",
            )

            with self.assertRaisesRegex(ValueError, "writer-address mismatch"):
                classify_writers(recover_writer_rows(path))

    def test_rejects_value_shape_drift(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            path = Path(temporary) / "text.asm"
            path.write_text(ASM.replace(", ecx", ", eax"), encoding="utf-8")

            with self.assertRaisesRegex(ValueError, "writer value mismatch"):
                classify_writers(recover_writer_rows(path))

    def test_recovers_scaled_and_loaded_selector_consumers(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            path = Path(temporary) / "text.asm"
            path.write_text(ASM, encoding="utf-8")
            rows = recover_consumer_rows(path)

        self.assertEqual(
            {row["address"] for row in rows},
            {"0x001A3E08", "0x001A3E46"},
        )
        self.assertEqual({row["mnemonic"] for row in rows}, {"imul", "mov"})

    def test_classifies_the_complete_consumer_domain(self) -> None:
        rows = [
            {
                "address": address,
                "destination": "eax",
                "mnemonic": "imul"
                if form == "scaled-selector-consumer"
                else "mov",
                "source": "synthetic.asm:1",
            }
            for address, form in EXPECTED_CONSUMERS.items()
        ]

        classified = classify_consumers(rows)

        self.assertEqual(len(classified), 33)
        self.assertEqual(
            {row["form"] for row in classified},
            {"scaled-selector-consumer", "loaded-selector-consumer"},
        )
        self.assertEqual(
            sum(row["form"] == "scaled-selector-consumer" for row in classified),
            32,
        )

    def test_classifies_the_function_envelope_and_call_families(self) -> None:
        rows = []
        for address, form in EXPECTED_CONSUMERS.items():
            function = EXPECTED_CONSUMER_FUNCTIONS[address]
            rows.append(
                {
                    "address": address,
                    "destination": "eax",
                    "mnemonic": "imul"
                    if form == "scaled-selector-consumer"
                    else "mov",
                    "source": "synthetic.asm:1",
                    "function": function,
                    "function_calls": EXPECTED_CONSUMER_CALLS[function],
                    "function_references": ("0x3458d0", "0x3458d4"),
                }
            )

        classified = classify_consumers(rows, require_function_envelope=True)

        self.assertEqual(len({row["function"] for row in classified}), 31)
        self.assertEqual(
            sum(row["call_family"] == "no-direct-call" for row in classified),
            4,
        )

    def test_rejects_consumer_domain_drift(self) -> None:
        rows = [
            {
                "address": address,
                "destination": "eax",
                "mnemonic": "imul"
                if form == "scaled-selector-consumer"
                else "mov",
                "source": "synthetic.asm:1",
            }
            for address, form in EXPECTED_CONSUMERS.items()
        ]
        rows[-1]["address"] = "0x00123456"

        with self.assertRaisesRegex(ValueError, "consumer-address mismatch"):
            classify_consumers(rows)

    def test_rejects_consumer_mnemonic_drift(self) -> None:
        rows = [
            {
                "address": address,
                "destination": "eax",
                "mnemonic": "imul"
                if form == "scaled-selector-consumer"
                else "mov",
                "source": "synthetic.asm:1",
            }
            for address, form in EXPECTED_CONSUMERS.items()
        ]
        rows[-1]["mnemonic"] = "imul"

        with self.assertRaisesRegex(ValueError, "consumer mnemonic mismatch"):
            classify_consumers(rows)

    def test_rejects_function_envelope_drift(self) -> None:
        rows = []
        for address, form in EXPECTED_CONSUMERS.items():
            function = EXPECTED_CONSUMER_FUNCTIONS[address]
            rows.append(
                {
                    "address": address,
                    "destination": "eax",
                    "mnemonic": "imul"
                    if form == "scaled-selector-consumer"
                    else "mov",
                    "source": "synthetic.asm:1",
                    "function": function,
                    "function_calls": EXPECTED_CONSUMER_CALLS[function],
                    "function_references": ("0x3458d0", "0x3458d4"),
                }
            )
        rows[0]["function_calls"] = ("sub_001A0000",)

        with self.assertRaisesRegex(ValueError, "consumer-call-family mismatch"):
            classify_consumers(rows, require_function_envelope=True)

    def test_rejects_consumer_anchor_drift(self) -> None:
        rows = []
        for address, form in EXPECTED_CONSUMERS.items():
            function = EXPECTED_CONSUMER_FUNCTIONS[address]
            rows.append(
                {
                    "address": address,
                    "destination": "eax",
                    "mnemonic": "imul"
                    if form == "scaled-selector-consumer"
                    else "mov",
                    "source": "synthetic.asm:1",
                    "function": function,
                    "function_calls": EXPECTED_CONSUMER_CALLS[function],
                    "function_references": ("0x3458d0", "0x3458d4"),
                }
            )
        rows[0]["function_references"] = ("0x3458d0",)

        with self.assertRaisesRegex(ValueError, "consumer-anchor mismatch"):
            classify_consumers(rows, require_function_envelope=True)

    def test_does_not_silently_ignore_an_unsupported_selector_read(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            path = Path(temporary) / "text.asm"
            path.write_text(
                ASM.replace(
                    "0x001A3E08  0faf05cc5b3400          imul",
                    "0x001A3E08  0faf05cc5b3400          add",
                ),
                encoding="utf-8",
            )
            rows = recover_consumer_rows(path)

        self.assertEqual(
            {row["address"] for row in rows},
            {"0x001A3E08", "0x001A3E46"},
        )
        complete_rows = [
            {
                "address": address,
                "destination": "eax",
                "mnemonic": "imul"
                if form == "scaled-selector-consumer"
                else "mov",
                "source": "synthetic.asm:1",
            }
            for address, form in EXPECTED_CONSUMERS.items()
        ]
        complete_rows[0]["mnemonic"] = "add"
        with self.assertRaisesRegex(ValueError, "consumer mnemonic mismatch"):
            classify_consumers(complete_rows)


if __name__ == "__main__":
    unittest.main()
