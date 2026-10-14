"""Tests for conservative indirect-call classification."""

from __future__ import annotations

import json
import struct
import tempfile
import unittest
from pathlib import Path

from tools.analysis.recomp_indirect_call_audit import audit


class RecompIndirectCallAuditTests(unittest.TestCase):
    @staticmethod
    def _write_fixture_xbe(path: Path) -> None:
        data = bytearray(0x400)
        data[:4] = b"XBEH"
        base = 0x10000
        struct.pack_into("<I", data, 0x104, base)
        struct.pack_into("<I", data, 0x11C, 1)
        struct.pack_into("<I", data, 0x120, base + 0x200)
        thunk_base = 0x11010
        struct.pack_into("<I", data, 0x158, thunk_base ^ 0x5B6D40B6)
        struct.pack_into(
            "<IIIII", data, 0x200, 0x5, 0x11000, 0x100, 0x300, 0x100
        )
        struct.pack_into("<I", data, 0x310, 0x80000042)
        path.write_bytes(data)

    def test_classifies_kernel_slot_and_preserves_dynamic_target(self) -> None:
        functions = [
            {
                "start": "0x1000",
                "end": "0x1010",
                "name": "xbe_entry_point",
                "calls_to": ["0x1020"],
            },
            {"start": "0x1020", "end": "0x1030", "name": "sub_00001020", "calls_to": []},
        ]
        generated_source = """
void xbe_entry_point(void)
{
    { uint32_t _icall_target = MEM32(0x00011010); PUSH32(esp, 0x00001008u); RECOMP_ICALL_SAFE_AT(_icall_target, _icall_esp, 0x00001008u); }
    { uint32_t _icall_target = eax; PUSH32(esp, 0x00001009u); RECOMP_ICALL_SAFE_AT(_icall_target, _icall_esp, 0x00001009u); }
}
void sub_00001020(void)
{
}
"""
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            functions_path = root / "functions.json"
            generated = root / "generated"
            xbe_path = root / "default.xbe"
            generated.mkdir()
            functions_path.write_text(json.dumps(functions), encoding="utf-8")
            (generated / "recomp_0000.c").write_text(generated_source, encoding="utf-8")
            self._write_fixture_xbe(xbe_path)

            result = audit(functions_path, generated, xbe_path, 0x1000)

        summary = result["summary"]
        self.assertEqual(summary["directly_reachable_functions"], 2)
        self.assertEqual(summary["indirect_call_sites"], 2)
        self.assertEqual(summary["kernel_thunk_slot_sites"], 1)
        self.assertEqual(summary["dynamic_register_or_object_targets"], 1)
        self.assertEqual(summary["kernel_thunk_table_entries"], 1)
        self.assertEqual(result["sites"][0]["kernel_ordinal"], 0x42)


if __name__ == "__main__":
    unittest.main()
