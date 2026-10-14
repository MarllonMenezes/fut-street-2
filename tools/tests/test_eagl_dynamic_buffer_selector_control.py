"""Tests for the dynamic EAGL buffer selector writer control."""

from __future__ import annotations

import tempfile
import unittest
from pathlib import Path

from tools.analysis.eagl_dynamic_buffer_selector_control import (
    classify_writers,
    recover_writer_rows,
)


ASM = "\n".join(
    [
        "  0x0019E91B  890dcc5b3400            mov      dword ptr [0x00345BCC], ecx",
        "  0x0019F20F  c705cc5b340000000000    mov      dword ptr [0x00345BCC], 0",
        "  0x0019FCFF  a3cc5b3400              mov      dword ptr [0x00345BCC], eax",
        "  0x001A2FCE  a3cc5b3400              mov      dword ptr [0x00345BCC], eax",
        "  0x001A30FB  8915cc5b3400            mov      dword ptr [0x00345BCC], edx",
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


if __name__ == "__main__":
    unittest.main()
