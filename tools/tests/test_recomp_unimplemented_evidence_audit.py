"""Tests for cross-artifact untranslated-instruction evidence auditing."""

from __future__ import annotations

import json
import tempfile
import unittest
from pathlib import Path

from tools.analysis.recomp_unimplemented_evidence_audit import audit


class RecompUnimplementedEvidenceAuditTests(unittest.TestCase):
    def test_aligns_summary_generated_c_and_asm(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            generated = root / "generated"
            asm = root / "asm"
            generated.mkdir()
            asm.mkdir()
            (root / "summary.json").write_text(
                json.dumps({"unimplemented": {"out": [0x298030], "sgdt": [0x253FD8]}}),
                encoding="utf-8",
            )
            (generated / "recomp_0000.c").write_text(
                'RECOMP_UNIMPL("out dx, al", 0x00298030u);\n'
                'RECOMP_UNIMPL("sgdt [esp + 6]", 0x00253FD8u);\n',
                encoding="utf-8",
            )
            (asm / "D3D.asm").write_text(
                "  0x00298030  ee                      out      dx, al\n",
                encoding="utf-8",
            )
            (asm / "KERNEL.asm").write_text(
                "  0x00253FD8  0f0114                  sgdt     [esp + 6]\n",
                encoding="utf-8",
            )

            result = audit(root / "summary.json", generated, asm)

        self.assertEqual(
            result["summary"]["status"],
            "verified-unimplemented-evidence-alignment",
        )
        self.assertEqual(result["summary"]["summary_sites"], 2)
        self.assertEqual(result["sites"][0]["instruction"], "sgdt [esp + 6]")
        self.assertEqual(result["sites"][1]["instruction"], "out dx, al")

    def test_rejects_summary_site_missing_from_generated_c(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            generated = root / "generated"
            asm = root / "asm"
            generated.mkdir()
            asm.mkdir()
            (root / "summary.json").write_text(
                json.dumps({"unimplemented": {"out": [0x298030]}}),
                encoding="utf-8",
            )

            with self.assertRaisesRegex(ValueError, "summary/generated"):
                audit(root / "summary.json", generated, asm)

    def test_rejects_generated_text_mismatch_with_asm(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            generated = root / "generated"
            asm = root / "asm"
            generated.mkdir()
            asm.mkdir()
            (root / "summary.json").write_text(
                json.dumps({"unimplemented": {"out": [0x298030]}}),
                encoding="utf-8",
            )
            (generated / "recomp_0000.c").write_text(
                'RECOMP_UNIMPL("out dx, bl", 0x00298030u);\n',
                encoding="utf-8",
            )
            (asm / "D3D.asm").write_text(
                "  0x00298030  ee                      out      dx, al\n",
                encoding="utf-8",
            )

            with self.assertRaisesRegex(ValueError, "generated/ASM"):
                audit(root / "summary.json", generated, asm)


if __name__ == "__main__":
    unittest.main()
