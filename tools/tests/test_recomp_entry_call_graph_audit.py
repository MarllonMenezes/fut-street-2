"""Tests for the conservative direct-call translation audit."""

from __future__ import annotations

import json
import tempfile
import unittest
from pathlib import Path

from tools.analysis.recomp_entry_call_graph_audit import audit


class RecompEntryCallGraphAuditTests(unittest.TestCase):
    def test_audit_distinguishes_direct_reachability_from_unimplemented_sites(self) -> None:
        functions = [
            {"start": "0x1000", "end": "0x1010", "calls_to": ["0x1020", "0xDEAD"]},
            {"start": "0x1020", "end": "0x1030", "calls_to": []},
            {"start": "0x2000", "end": "0x2010", "calls_to": []},
        ]
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            functions_path = root / "functions.json"
            generated = root / "generated"
            generated.mkdir()
            functions_path.write_text(json.dumps(functions), encoding="utf-8")
            (generated / "recomp_0000.c").write_text(
                "void sub_00001000(void) { RECOMP_UNIMPL(\"fixture\", 0x00001008u); }\n"
                "void sub_00001020(void) {}\n"
                "void sub_00002000(void) { RECOMP_UNIMPL(\"fixture\", 0x00002008u); }\n",
                encoding="utf-8",
            )

            result = audit(functions_path, generated, 0x1000)

        summary = result["summary"]
        self.assertEqual(summary["directly_reachable_functions"], 2)
        self.assertEqual(summary["direct_external_call_targets"], 1)
        self.assertEqual(summary["directly_reachable_missing_translations"], 0)
        self.assertEqual(summary["unimplemented_sites"], 2)
        self.assertEqual(
            summary["unimplemented_sites_in_directly_reachable_function_scopes"], 1
        )
        self.assertEqual(result["missing_direct_reachable_translations"], [])

    def test_missing_translated_direct_function_is_reported(self) -> None:
        functions = [
            {"start": "0x1000", "end": "0x1010", "calls_to": ["0x1020"]},
            {"start": "0x1020", "end": "0x1030", "calls_to": []},
        ]
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            functions_path = root / "functions.json"
            generated = root / "generated"
            generated.mkdir()
            functions_path.write_text(json.dumps(functions), encoding="utf-8")
            (generated / "recomp_0000.c").write_text(
                "void sub_00001000(void) {}\n", encoding="utf-8"
            )

            result = audit(functions_path, generated, 0x1000)

        self.assertEqual(
            result["missing_direct_reachable_translations"], ["0x00001020"]
        )
        self.assertEqual(
            result["summary"]["status"], "incomplete-direct-call-translation-coverage"
        )


if __name__ == "__main__":
    unittest.main()
