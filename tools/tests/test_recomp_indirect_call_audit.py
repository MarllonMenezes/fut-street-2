"""Tests for conservative indirect-call classification."""

from __future__ import annotations

import json
import struct
import tempfile
import unittest
from pathlib import Path

from tools.analysis.recomp_indirect_call_audit import (
    audit,
    recover_dynamic_target_writes,
    recover_fixed_slot_writes,
    recover_indirect_call_skip_branches,
    recover_kernel_bridge_cases,
    recover_kernel_implementation_evidence,
    recover_prcb_cache_writes,
    recover_runtime_fixed_slot_initializers,
)


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
    edi = MEM32(0x00011010);
    { uint32_t _icall_target = edi; PUSH32(esp, 0x0000100Au); RECOMP_ICALL_SAFE_AT(_icall_target, _icall_esp, 0x0000100Au); }
    eax = MEM32(XBOX_FS_BASE + 0x20);
    eax = MEM32(eax + 0x250);
    if (CMP_EQ(_fa, _fb)) goto loc_00001020;
    { uint32_t _icall_target = MEM32(eax + 0x14); PUSH32(esp, 0x0000100Bu); RECOMP_ICALL_SAFE_AT(_icall_target, _icall_esp, 0x0000100Bu); }
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
        self.assertEqual(summary["indirect_call_sites"], 4)
        self.assertEqual(summary["kernel_thunk_slot_sites"], 2)
        self.assertEqual(summary["linear_register_alias_sites"], 1)
        self.assertEqual(summary["dynamic_register_or_object_targets"], 1)
        self.assertEqual(summary["prcb_guarded_dynamic_callback_sites"], 1)
        self.assertEqual(summary["unresolved_dynamic_sites"], 2)
        self.assertEqual(summary["kernel_thunk_table_entries"], 1)
        self.assertEqual(result["sites"][0]["kernel_ordinal"], 0x42)
        self.assertEqual(result["sites"][2]["classification_source"], "linear-register-alias")

    def test_preserves_dynamic_memory_provenance(self) -> None:
        functions = [
            {
                "start": "0x1000",
                "end": "0x1010",
                "name": "xbe_entry_point",
                "calls_to": [],
            }
        ]
        generated_source = """
void xbe_entry_point(void)
{
    eax = 0x00011010;
    ecx = eax;
    { uint32_t _icall_target = MEM32(ecx); PUSH32(esp, 0x00001007u); RECOMP_ICALL_SAFE_AT(_icall_target, _icall_esp, 0x00001007u); }
    ecx = ecx + 4;
    { uint32_t _icall_target = MEM32(ecx); PUSH32(esp, 0x00001007u); RECOMP_ICALL_SAFE_AT(_icall_target, _icall_esp, 0x00001007u); }
    eax = MEM32(ecx + 0x584);
    { uint32_t _icall_target = eax; PUSH32(esp, 0x00001008u); RECOMP_ICALL_SAFE_AT(_icall_target, _icall_esp, 0x00001008u); }
    { uint32_t _icall_target = MEM32(ebx + esi * 4 + 8); PUSH32(esp, 0x00001009u); RECOMP_ICALL_SAFE_AT(_icall_target, _icall_esp, 0x00001009u); }
    { uint32_t _icall_target = MEM32(ecx); PUSH32(esp, 0x0000100Au); RECOMP_ICALL_SAFE_AT(_icall_target, _icall_esp, 0x0000100Au); }
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

        self.assertEqual(
            result["summary"]["dynamic_target_family_counts"],
            {
                "dynamic-memory-target": 3,
                "dynamic-object-field-target": 1,
                "dynamic-indexed-record-target": 1,
            },
        )
        self.assertEqual(result["sites"][0]["target_memory_address"], "0x00011010")
        self.assertEqual(result["sites"][0]["target_memory_raw_value"], "0x80000042")
        self.assertEqual(
            result["sites"][1]["target_memory_address_range"],
            "0x00011010-0x00011014",
        )
        self.assertEqual(
            result["sites"][1]["target_memory_raw_values"],
            {"0x00011010": "0x80000042", "0x00011014": "0x00000000"},
        )
        self.assertEqual(
            result["sites"][2]["target_source_expression"],
            "MEM32(ecx + 0x584)",
        )
        self.assertEqual(
            result["sites"][2]["classification"],
            "dynamic-object-field-target",
        )

    def test_resolves_proven_bss_slot_initializer(self) -> None:
        functions = [
            {
                "start": "0x1000",
                "end": "0x1010",
                "name": "xbe_entry_point",
                "calls_to": [],
            },
            {"start": "0x1020", "end": "0x1030", "name": "sub_00001020", "calls_to": []},
        ]
        generated_source = """
void xbe_entry_point(void)
{
    MEM32(0x00011180) = 0x00001020;
    { uint32_t _icall_target = MEM32(0x00011180); PUSH32(esp, 0x00001008u); RECOMP_ICALL_SAFE_AT(_icall_target, _icall_esp, 0x00001008u); }
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
            xbe = bytearray(xbe_path.read_bytes())
            struct.pack_into("<I", xbe, 0x208, 0x200)
            xbe_path.write_bytes(xbe)

            result = audit(functions_path, generated, xbe_path, 0x1000)

        self.assertEqual(
            result["sites"][0]["classification"],
            "runtime-initialized-known-function-pointer",
        )
        self.assertEqual(result["sites"][0]["target_function"], "0x00001020")
        self.assertEqual(
            result["summary"]["runtime_initialized_known_function_pointer_sites"],
            1,
        )

    def test_distinguishes_prcb_cache_write_from_reused_offset(self) -> None:
        functions = [
            {
                "start": "0x1000",
                "end": "0x1010",
                "name": "xbe_entry_point",
                "calls_to": [],
            }
        ]
        generated_source = """
void xbe_entry_point(void)
{
    esi = MEM32(XBOX_FS_BASE + 0x20);
    MEM32(esi + 0x250) = eax;
    edi = MEM32(ebx + 0x250);
    MEM32(edi + 0x250) = ecx;
}
"""
        with tempfile.TemporaryDirectory() as temporary:
            generated = Path(temporary) / "generated"
            generated.mkdir()
            (generated / "recomp_0000.c").write_text(
                generated_source, encoding="utf-8"
            )
            writes = recover_prcb_cache_writes(generated, functions, {0x1000})

        self.assertEqual(len(writes), 1)
        self.assertEqual(writes[0]["register"], "esi")
        self.assertEqual(writes[0]["field"], "FS:[0x20] + 0x250")

    def test_audits_explicit_host_kernel_resolver_case(self) -> None:
        functions = [
            {
                "start": "0x1000",
                "end": "0x1010",
                "name": "xbe_entry_point",
                "calls_to": [],
            }
        ]
        generated_source = """
void xbe_entry_point(void)
{
    { uint32_t _icall_target = MEM32(0x00011010); PUSH32(esp, 0x00001008u); RECOMP_ICALL_SAFE_AT(_icall_target, _icall_esp, 0x00001008u); }
}
"""
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            generated = root / "generated"
            generated.mkdir()
            functions_path = root / "functions.json"
            xbe_path = root / "default.xbe"
            kernel_source = root / "kernel_thunks.c"
            functions_path.write_text(json.dumps(functions), encoding="utf-8")
            (generated / "recomp_0000.c").write_text(
                generated_source, encoding="utf-8"
            )
            self._write_fixture_xbe(xbe_path)
            kernel_source.write_text(
                "case 66: return (ULONG_PTR)xbox_IoCreateFile;\n",
                encoding="utf-8",
            )

            result = audit(
                functions_path,
                generated,
                xbe_path,
                0x1000,
                kernel_source,
            )

        self.assertEqual(
            result["summary"]["kernel_resolver_status"],
            "verified-explicit-case-coverage",
        )
        self.assertEqual(result["summary"]["kernel_resolver_case_sites"], 1)
        self.assertEqual(result["summary"]["kernel_resolver_missing_sites"], 0)
        self.assertEqual(result["sites"][0]["host_resolver_status"], "explicit-case")
        self.assertEqual(
            result["sites"][0]["host_resolver_target"],
            "xbox_IoCreateFile",
        )

    def test_distinguishes_kernel_source_definition_from_missing_definition(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            resolver = root / "kernel_thunks.c"
            resolver.write_text(
                "case 66: return (ULONG_PTR)xbox_IoCreateFile;\n"
                "case 67: return (ULONG_PTR)xbox_Missing;\n",
                encoding="utf-8",
            )
            (root / "kernel_file.c").write_text(
                "NTSTATUS __stdcall xbox_IoCreateFile(\n"
                "    PVOID a)\n"
                "{\n"
                "    return STATUS_SUCCESS;\n"
                "}\n",
                encoding="utf-8",
            )

            cases = {66: "xbox_IoCreateFile", 67: "xbox_Missing"}
            evidence = recover_kernel_implementation_evidence(resolver, cases)

        self.assertEqual(evidence["xbox_IoCreateFile"]["status"], "source-definition-found")
        self.assertEqual(evidence["xbox_IoCreateFile"]["sources"], ["kernel_file.c:1"])
        self.assertEqual(evidence["xbox_Missing"]["status"], "missing-source-definition")

    def test_reads_only_kernel_bridge_resolver_cases(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            source = Path(temporary) / "kernel_bridge.c"
            source.write_text(
                "static int stdcall_args_for_ordinal(ULONG ordinal)\n"
                "{\n"
                "    switch (ordinal) {\n"
                "    case 66: return 8;\n"
                "    default: return -1;\n"
                "    }\n"
                "}\n"
                "static bridge_func_t bridge_for_ordinal(ULONG ordinal)\n"
                "{\n"
                "    switch (ordinal) {\n"
                "    case 66: return bridge_IoCreateFile;\n"
                "    default: return NULL;\n"
                "    }\n"
                "}\n",
                encoding="utf-8",
            )

            cases = recover_kernel_bridge_cases(source)

        self.assertEqual(cases, {66: "bridge_IoCreateFile"})

    def test_keeps_dynamic_writer_shapes_separate(self) -> None:
        functions = [
            {
                "start": "0x1000",
                "end": "0x1010",
                "name": "xbe_entry_point",
                "calls_to": [],
            }
        ]
        generated_source = """
void xbe_entry_point(void)
{
    eax = 0x00011010;
    MEM32(eax) = ebx;
    MEM32(ecx + 0x584) = eax;
    MEM32(ebx + esi * 4 + 8) = eax;
    MEM32(edi + 0x584) = eax;
}
"""
        with tempfile.TemporaryDirectory() as temporary:
            generated = Path(temporary) / "generated"
            generated.mkdir()
            (generated / "recomp_0000.c").write_text(
                generated_source, encoding="utf-8"
            )
            writers = recover_dynamic_target_writes(
                generated,
                functions,
                {0x1000},
                [(0x11010, 0x11010)],
            )
            all_writers = recover_dynamic_target_writes(
                generated,
                functions,
                None,
                [],
            )

        self.assertEqual(len(writers["dynamic-memory-target"]), 1)
        self.assertEqual(len(writers["dynamic-object-field-target"]), 2)
        self.assertEqual(len(writers["dynamic-indexed-record-target"]), 1)
        self.assertEqual(len(all_writers["dynamic-object-field-target"]), 2)
        self.assertEqual(len(all_writers["dynamic-indexed-record-target"]), 1)

    def test_can_inventory_fixed_slot_writers_outside_direct_path(self) -> None:
        functions = [
            {
                "start": "0x1000",
                "end": "0x1010",
                "name": "xbe_entry_point",
                "calls_to": [],
            },
            {
                "start": "0x1020",
                "end": "0x1030",
                "name": "sub_00001020",
                "calls_to": [],
            },
        ]
        generated_source = """
void xbe_entry_point(void)
{
    MEM32(0x00011180) = 0x00001020;
}
void sub_00001020(void)
{
    MEM32(0x00011180) = 0x00001020;
}
"""
        with tempfile.TemporaryDirectory() as temporary:
            generated = Path(temporary) / "generated"
            generated.mkdir()
            (generated / "recomp_0000.c").write_text(
                generated_source, encoding="utf-8"
            )
            all_writers = recover_runtime_fixed_slot_initializers(
                generated, functions, None
            )
            direct_writers = recover_runtime_fixed_slot_initializers(
                generated, functions, {0x1000}
            )

        self.assertEqual(len(all_writers[0x11180]), 2)
        self.assertEqual(len(direct_writers[0x11180]), 1)

    def test_recovers_register_derived_fixed_slot_write(self) -> None:
        functions = [
            {
                "start": "0x1000",
                "end": "0x1010",
                "name": "xbe_entry_point",
                "calls_to": [],
            }
        ]
        generated_source = """
void xbe_entry_point(void)
{
    eax = 0x00011180;
    MEM32(eax) = edx;
}
"""
        with tempfile.TemporaryDirectory() as temporary:
            generated = Path(temporary) / "generated"
            generated.mkdir()
            (generated / "recomp_0000.c").write_text(
                generated_source, encoding="utf-8"
            )
            writes = recover_fixed_slot_writes(
                generated,
                functions,
                {0x1000},
                {0x11180},
            )

        self.assertEqual(len(writes[0x11180]), 1)
        self.assertEqual(writes[0x11180][0]["kind"], "register-derived")

    def test_records_branch_that_skips_indirect_call(self) -> None:
        functions = [
            {
                "start": "0x1000",
                "end": "0x1010",
                "name": "xbe_entry_point",
                "calls_to": [],
            }
        ]
        generated_source = """
void xbe_entry_point(void)
{
    if (CMP_EQ(_fa, _fb)) goto loc_00001010;
    { uint32_t _icall_target = eax; PUSH32(esp, 0x00001008u); RECOMP_ICALL_SAFE_AT(_icall_target, _icall_esp, 0x00001008u); }
loc_00001010: ;
}
"""
        with tempfile.TemporaryDirectory() as temporary:
            generated = Path(temporary) / "generated"
            generated.mkdir()
            (generated / "recomp_0000.c").write_text(
                generated_source, encoding="utf-8"
            )
            evidence = recover_indirect_call_skip_branches(
                generated,
                [
                    {
                        "call_site": "0x1008",
                        "source": "recomp_0000.c:5",
                    }
                ],
            )

        self.assertEqual(evidence["0x1008"]["kind"], "conditional-branch-skips-call")
        self.assertEqual(evidence["0x1008"]["target_label"], "loc_00001010")


if __name__ == "__main__":
    unittest.main()
