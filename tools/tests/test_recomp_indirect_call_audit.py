"""Tests for conservative indirect-call classification."""

from __future__ import annotations

import json
import struct
import tempfile
import unittest
from pathlib import Path

from tools.analysis.recomp_indirect_call_audit import (
    audit,
    recover_dynamic_callback_caller_provenance,
    recover_dynamic_callback_container_field_provenance,
    recover_dynamic_callback_object_field_provenance,
    recover_dynamic_source_provenance,
    recover_dynamic_target_writes,
    recover_fixed_callback_consumer_provenance,
    recover_fixed_slot_writes,
    recover_indirect_call_nonzero_guards,
    recover_indirect_call_skip_branches,
    recover_kernel_bridge_cases,
    recover_kernel_call_argument_counts,
    recover_kernel_constant_arguments,
    recover_kernel_register_constant_arguments,
    recover_kernel_file_information_evidence,
    recover_kernel_implementation_evidence,
    recover_prcb_cache_writes,
    recover_prcb_callback_cache_provenance,
    recover_runtime_fixed_slot_initializers,
    recover_symbolic_link_open_query_close_sequences,
    recover_symbolic_link_guest_paths,
    recover_symbolic_link_host_bridge_evidence,
    recover_symbolic_link_host_query_target,
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
        self.assertEqual(summary["prcb_callback_cache_provenance_sites"], 1)
        self.assertEqual(summary["prcb_callback_cache_provenance_complete_sites"], 1)
        self.assertEqual(
            summary["prcb_callback_cache_provenance_status"],
            "prcb-cache-callback-field-shape-observed",
        )
        self.assertEqual(summary["unresolved_dynamic_sites"], 2)
        self.assertEqual(summary["dynamic_call_stack_shape_sites"], 0)
        self.assertEqual(summary["dynamic_call_stack_shape_missing_sites"], 2)
        self.assertEqual(
            summary["dynamic_call_stack_shape_status"],
            "incomplete-push-count-evidence",
        )
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

    def test_recovers_bounded_dynamic_source_assignments(self) -> None:
        generated_source = """
void xbe_entry_point(void)
{
    eax = 0x00011010;
    ecx = eax;
    { uint32_t _icall_target = MEM32(ecx); PUSH32(esp, 0x00001007u); RECOMP_ICALL_SAFE_AT(_icall_target, _icall_esp, 0x00001007u); }
    eax = MEM32(XBOX_FS_BASE + 0x20);
    eax = MEM32(eax + 0x250);
    { uint32_t _icall_target = MEM32(eax + 0x14); PUSH32(esp, 0x00001008u); RECOMP_ICALL_SAFE_AT(_icall_target, _icall_esp, 0x00001008u); }
    eax = MEM32(esp + 0x20);
    ebx = MEM32(eax + 8);
    esi = MEM32(eax + 0xC);
    esi = esi + esi * 2;
    { uint32_t _icall_target = MEM32(ebx + esi * 4 + 8); PUSH32(esp, 0x00001009u); RECOMP_ICALL_SAFE_AT(_icall_target, _icall_esp, 0x00001009u); }
}
"""
        rows = [
            {
                "call_site": "0x00001007",
                "function": "0x00001000",
                "function_name": "xbe_entry_point",
                "classification": "dynamic-memory-target",
                "target_expression": "MEM32(ecx)",
            },
            {
                "call_site": "0x00001008",
                "function": "0x00001000",
                "function_name": "xbe_entry_point",
                "classification": "prcb-guarded-dynamic-callback",
                "target_expression": "MEM32(eax + 0x14)",
            },
            {
                "call_site": "0x00001009",
                "function": "0x00001000",
                "function_name": "xbe_entry_point",
                "classification": "dynamic-indexed-record-target",
                "target_expression": "MEM32(ebx + esi * 4 + 8)",
            },
        ]
        with tempfile.TemporaryDirectory() as temporary:
            generated = Path(temporary) / "generated"
            generated.mkdir()
            (generated / "recomp_0000.c").write_text(
                generated_source, encoding="utf-8"
            )
            evidence = recover_dynamic_source_provenance(generated, rows)

        self.assertEqual(
            evidence["0x00001007"]["status"],
            "local-linear-source-assignments-observed",
        )
        self.assertIn(
            "eax = 0x00011010;",
            {item["expression"] for item in evidence["0x00001007"]["assignments"]},
        )
        self.assertEqual(
            evidence["0x00001008"]["covered_target_registers"], ["eax"]
        )
        self.assertEqual(
            evidence["0x00001009"]["covered_target_registers"], ["ebx", "esi"]
        )
        self.assertIn(
            "eax = MEM32(esp + 0x20);",
            {item["expression"] for item in evidence["0x00001009"]["assignments"]},
        )

    def test_recovers_prcb_callback_cache_field_shape(self) -> None:
        classified = [
            {
                "call_site": "0x0000100B",
                "function": "0x00001000",
                "function_name": "xbe_entry_point",
                "classification": "prcb-guarded-dynamic-callback",
                "target_expression": "MEM32(eax + 0x14)",
                "prcb_cache_guarded": True,
            }
        ]
        source_provenance = {
            "0x0000100B": {
                "assignments": [
                    {
                        "register": "eax",
                        "expression": "eax = MEM32(eax + 0x250);",
                    },
                    {
                        "register": "eax",
                        "expression": "eax = MEM32(XBOX_FS_BASE + 0x20);",
                    },
                ]
            }
        }

        evidence = recover_prcb_callback_cache_provenance(
            classified, source_provenance
        )

        row = evidence["0x0000100B"]
        self.assertEqual(row["source_register"], "eax")
        self.assertEqual(row["cache_offset"], "0x250")
        self.assertEqual(row["callback_field_offset"], "0x14")
        self.assertEqual(row["cache_register"], "eax")
        self.assertEqual(row["callback_register"], "eax")
        self.assertTrue(row["guarded"])
        self.assertEqual(row["status"], "prcb-cache-callback-field-shape-observed")

    def test_recovers_direct_caller_push_shape(self) -> None:
        functions = [
            {
                "start": "0x1000",
                "end": "0x1010",
                "name": "sub_00001000",
                "calls_to": [],
            },
            {
                "start": "0x2000",
                "end": "0x2010",
                "name": "sub_00002000",
                "calls_to": ["0x1000"],
            },
        ]
        generated_source = """
void sub_00002000(void)
{
    eax = ebp + -16;
loc_00002004: ;
    PUSH32(esp, 0xFFFFFFFFu);
    PUSH32(esp, eax);
    PUSH32(esp, 0x0000200Au); RECOMP_ABI_CALL(0x00001000u, sub_00001000); /* call */
}
void sub_00001000(void)
{
    { uint32_t _icall_target = MEM32(ebx + esi * 4 + 8); PUSH32(esp, 0x0000100Fu); RECOMP_ICALL_SAFE_AT(_icall_target, _icall_esp, 0x0000100Fu); }
}
"""
        rows = [
            {
                "function": "0x00001000",
                "function_name": "sub_00001000",
                "classification": "dynamic-indexed-record-target",
            }
        ]
        with tempfile.TemporaryDirectory() as temporary:
            generated = Path(temporary) / "generated"
            generated.mkdir()
            (generated / "recomp_0000.c").write_text(
                generated_source, encoding="utf-8"
            )
            evidence = recover_dynamic_callback_caller_provenance(
                generated, rows, functions, {0x1000, 0x2000}
            )

        self.assertEqual(len(evidence), 1)
        self.assertEqual(evidence[0]["caller_name"], "sub_00002000")
        self.assertEqual(
            [item["operand"] for item in evidence[0]["pushes"]],
            ["0xFFFFFFFFu", "eax"],
        )
        self.assertEqual(
            evidence[0]["pushes"][1]["source_assignment"]["rhs"],
            "ebp + -16",
        )

    def test_recovers_fixed_callback_consumer_push_shape(self) -> None:
        functions = [
            {
                "start": "0x1000",
                "end": "0x1010",
                "name": "sub_00001000",
                "calls_to": [],
            },
            {
                "start": "0x2000",
                "end": "0x2010",
                "name": "sub_00002000",
                "calls_to": ["0x1000"],
            },
        ]
        generated_source = """
void sub_00001000(void)
{
    eax = MEM32(0x360A70);
    if (CMP_EQ(_fa, _fb)) goto loc_0000100F;
    { uint32_t _icall_target = eax; PUSH32(esp, 0x0000100Fu); RECOMP_ICALL_SAFE_AT(_icall_target, _icall_esp, 0x0000100Fu); }
loc_0000100F: ;
}
void sub_00002000(void)
{
    PUSH32(esp, ebx);
    PUSH32(esp, 0x0000200Au); RECOMP_ABI_CALL(0x00001000u, sub_00001000); /* call */
}
"""
        classified = [
            {
                "call_site": "0x0000100F",
                "function": "0x00001000",
                "function_name": "sub_00001000",
                "classification": "runtime-initialized-fixed-slot",
                "fixed_slot": "0x00360A70",
                "target_expression": "eax",
            }
        ]
        with tempfile.TemporaryDirectory() as temporary:
            generated = Path(temporary) / "generated"
            generated.mkdir()
            (generated / "recomp_0000.c").write_text(
                generated_source, encoding="utf-8"
            )
            evidence = recover_fixed_callback_consumer_provenance(
                generated, classified, functions
            )

        self.assertEqual(len(evidence), 1)
        self.assertEqual(evidence[0]["fixed_slot"], "0x00360A70")
        self.assertEqual(evidence[0]["consumer_call_site"], "0x0000100F")
        self.assertEqual(evidence[0]["caller_name"], "sub_00002000")
        self.assertEqual([item["operand"] for item in evidence[0]["pushes"]], ["ebx"])
        self.assertEqual(
            evidence[0]["status"], "fixed-callback-consumer-push-shape-observed"
        )

    def test_recovers_indexed_callback_container_field_shapes(self) -> None:
        functions = [
            {
                "start": "0x1000",
                "end": "0x1010",
                "name": "sub_00001000",
                "calls_to": [],
            },
            {
                "start": "0x2000",
                "end": "0x2010",
                "name": "sub_00002000",
                "calls_to": ["0x1000"],
            },
        ]
        generated_source = """
void sub_00001000(void)
{
    eax = MEM32(esp + 0x20);
    ebx = MEM32(eax + 8);
    esi = MEM32(eax + 0xC);
    { uint32_t _icall_target = MEM32(ebx + esi * 4 + 8); PUSH32(esp, 0x0000100Fu); RECOMP_ICALL_SAFE_AT(_icall_target, _icall_esp, 0x0000100Fu); }
}
void sub_00002000(void)
{
    eax = ebp + -16;
    MEM32(ebp + -4) = ebx;
    PUSH32(esp, 0xFFFFFFFFu);
    PUSH32(esp, eax);
    PUSH32(esp, 0x0000200Au); RECOMP_ABI_CALL(0x00001000u, sub_00001000); /* call */
}
"""
        rows = [
            {
                "call_site": "0x0000100F",
                "function": "0x00001000",
                "function_name": "sub_00001000",
                "classification": "dynamic-indexed-record-target",
                "target_expression": "MEM32(ebx + esi * 4 + 8)",
            }
        ]
        with tempfile.TemporaryDirectory() as temporary:
            generated = Path(temporary) / "generated"
            generated.mkdir()
            (generated / "recomp_0000.c").write_text(
                generated_source, encoding="utf-8"
            )
            callers = recover_dynamic_callback_caller_provenance(
                generated, rows, functions, {0x1000, 0x2000}
            )
            all_callers = recover_dynamic_callback_caller_provenance(
                generated, rows, functions, None
            )
            evidence = recover_dynamic_callback_container_field_provenance(
                generated, rows, callers
            )

        self.assertEqual(len(evidence), 1)
        self.assertEqual(evidence[0]["object_pointer_expression"], "ebp + -16")
        fields = {row["field_offset"]: row for row in evidence[0]["container_fields"]}
        self.assertEqual(fields["0x8"]["status"], "no-proven-caller-local-field-write")
        self.assertEqual(
            fields["0xC"]["status"],
            "caller-local-field-write-shape-observed",
        )
        self.assertEqual(fields["0xC"]["writes"][0]["rhs"], "ebx")
        self.assertEqual(len(all_callers), 1)

    def test_recovers_dynamic_object_field_writer_shape(self) -> None:
        functions = [
            {
                "start": "0x1000",
                "end": "0x1010",
                "name": "sub_00001000",
                "calls_to": [],
            },
            {
                "start": "0x2000",
                "end": "0x2010",
                "name": "sub_00002000",
                "calls_to": ["0x1000"],
            },
        ]
        generated_source = """
void sub_00001000(void)
{
    ecx = MEM32(ebp + 8);
    { uint32_t _icall_target = MEM32(ecx + 0x584); PUSH32(esp, 0x0000100Fu); RECOMP_ICALL_SAFE_AT(_icall_target, _icall_esp, 0x0000100Fu); }
}
void sub_00002000(void)
{
    edi = MEM32(ebp + 8);
    MEM32(edi + 0x584) = edx;
    PUSH32(esp, edi);
    PUSH32(esp, 0x0000200Au); RECOMP_ABI_CALL(0x00001000u, sub_00001000); /* call */
}
"""
        rows = [
            {
                "call_site": "0x0000100F",
                "function": "0x00001000",
                "function_name": "sub_00001000",
                "classification": "dynamic-object-field-target",
                "target_expression": "MEM32(ecx + 0x584)",
            }
        ]
        with tempfile.TemporaryDirectory() as temporary:
            generated = Path(temporary) / "generated"
            generated.mkdir()
            (generated / "recomp_0000.c").write_text(
                generated_source, encoding="utf-8"
            )
            callers = recover_dynamic_callback_caller_provenance(
                generated, rows, functions, {0x1000, 0x2000}
            )
            evidence = recover_dynamic_callback_object_field_provenance(
                generated, rows, callers
            )

        self.assertEqual(len(evidence), 1)
        self.assertEqual(evidence[0]["object_register"], "edi")
        self.assertEqual(
            evidence[0]["fields"][0]["status"],
            "caller-object-field-write-shape-observed",
        )
        self.assertEqual(evidence[0]["fields"][0]["writes"][0]["rhs"], "edx")

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
    edx = esi + 0x250;
    MEM32(edx) = ebp;
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

        self.assertEqual(len(writes), 2)
        self.assertEqual(writes[0]["register"], "esi")
        self.assertEqual(writes[0]["field"], "FS:[0x20] + 0x250")
        self.assertEqual(writes[1]["register"], "edx")
        self.assertEqual(writes[1]["address_expression"], "esi + 0x250")

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
                "    return STATUS_SUCCESS; /* no-op */\n"
                "}\n",
                encoding="utf-8",
            )

            cases = {66: "xbox_IoCreateFile", 67: "xbox_Missing"}
            evidence = recover_kernel_implementation_evidence(resolver, cases)

        self.assertEqual(evidence["xbox_IoCreateFile"]["status"], "source-definition-found")
        self.assertEqual(evidence["xbox_IoCreateFile"]["sources"], ["kernel_file.c:1"])
        self.assertEqual(
            evidence["xbox_IoCreateFile"]["body_markers"],
            [
                {
                    "kind": "explicit-stub-or-no-op",
                    "marker": "no-op",
                    "source": "kernel_file.c:4",
                }
            ],
        )
        self.assertEqual(evidence["xbox_Missing"]["status"], "missing-source-definition")
        self.assertEqual(evidence["xbox_Missing"]["body_markers"], [])

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

    def test_counts_kernel_call_push_arguments(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            generated = Path(temporary) / "generated"
            generated.mkdir()
            (generated / "recomp_0000.c").write_text(
                "void sub_00001000(void)\n"
                "{\n"
                "    { uint32_t _icall_esp = g_esp;\n"
                "    PUSH32(esp, eax);\n"
                "    PUSH32(esp, ecx);\n"
                "    { uint32_t _icall_target = MEM32(0x2A9000); "
                "PUSH32(esp, 0x00001008u); "
                "RECOMP_ICALL_SAFE_AT(_icall_target, _icall_esp, 0x00001008u); }\n"
                "    }\n"
                "}\n",
                encoding="utf-8",
            )

            counts = recover_kernel_call_argument_counts(
                generated,
                [{"call_site": "0x00001008", "source": "recomp_0000.c:6"}],
            )

        self.assertEqual(counts["0x00001008"], {"push_count": 2, "pushed_bytes": 8})

    def test_recovers_constant_kernel_stack_arguments(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            generated = Path(temporary) / "generated"
            generated.mkdir()
            (generated / "recomp_0000.c").write_text(
                "void sub_00001000(void)\n"
                "{\n"
                "    { uint32_t _icall_esp = g_esp;\n"
                "    PUSH32(esp, 0x22);\n"
                "    PUSH32(esp, 0x38u);\n"
                "    PUSH32(esp, eax);\n"
                "    PUSH32(esp, ecx);\n"
                "    PUSH32(esp, edx);\n"
                "    { uint32_t _icall_target = MEM32(0x2A9000); "
                "PUSH32(esp, 0x00001008u); "
                "RECOMP_ICALL_SAFE_AT(_icall_target, _icall_esp, 0x00001008u); }\n"
                "    }\n"
                "}\n",
                encoding="utf-8",
            )

            constants = recover_kernel_constant_arguments(
                generated,
                [{"call_site": "0x00001008", "source": "recomp_0000.c:9"}],
            )

        self.assertEqual(
            constants["0x00001008"],
            {"3": "0x00000038", "4": "0x00000022"},
        )

    def test_recovers_register_constant_kernel_stack_arguments(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            generated = Path(temporary) / "generated"
            generated.mkdir()
            (generated / "recomp_0000.c").write_text(
                "void sub_00001000(void)\n"
                "{\n"
                "    esi = 0x00001000u;\n"
                "    { uint32_t _icall_esp = g_esp;\n"
                "    PUSH32(esp, eax);\n"
                "    PUSH32(esp, esi);\n"
                "    PUSH32(esp, 0x22u);\n"
                "    { uint32_t _icall_target = MEM32(0x2A9000); "
                "PUSH32(esp, 0x00001008u); "
                "RECOMP_ICALL_SAFE_AT(_icall_target, _icall_esp, 0x00001008u); }\n"
                "    }\n"
                "}\n",
                encoding="utf-8",
            )

            constants = recover_kernel_register_constant_arguments(
                generated,
                [{"call_site": "0x00001008", "source": "recomp_0000.c:8"}],
            )

        self.assertEqual(
            constants["0x00001008"],
            {"1": "0x00001000"},
        )

    def test_recovers_kernel_file_information_cases(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            (root / "kernel.h").write_text(
                "typedef enum _XBOX_FILE_INFORMATION_CLASS {\n"
                "    XboxFilePositionInformation = 14,\n"
                "    XboxFileNetworkOpenInformation = 34,\n"
                "} XBOX_FILE_INFORMATION_CLASS;\n",
                encoding="utf-8",
            )
            (root / "kernel_file.c").write_text(
                "NTSTATUS __stdcall xbox_NtQueryInformationFile(\n"
                "    PVOID a)\n"
                "{\n"
                "    switch (a) {\n"
                "    case XboxFilePositionInformation:\n"
                "        return STATUS_SUCCESS;\n"
                "    case XboxFileNetworkOpenInformation:\n"
                "        return STATUS_SUCCESS;\n"
                "    default: return STATUS_NOT_IMPLEMENTED;\n"
                "    }\n"
                "}\n"
                "NTSTATUS __stdcall xbox_NtSetInformationFile(PVOID a)\n"
                "{\n"
                "    switch (a) {\n"
                "    case XboxFilePositionInformation:\n"
                "        return STATUS_SUCCESS;\n"
                "    default: return STATUS_NOT_IMPLEMENTED;\n"
                "    }\n"
                "}\n",
                encoding="utf-8",
            )

            evidence = recover_kernel_file_information_evidence(root)

        self.assertEqual(evidence["status"], "source-cases-found")
        self.assertEqual(
            evidence["enum_values"]["XboxFileNetworkOpenInformation"],
            34,
        )
        self.assertEqual(
            evidence["functions"]["xbox_NtQueryInformationFile"]["case_values"],
            {
                "XboxFileNetworkOpenInformation": 34,
                "XboxFilePositionInformation": 14,
            },
        )
        self.assertEqual(
            evidence["functions"]["xbox_NtSetInformationFile"]["case_names"],
            ["XboxFilePositionInformation"],
        )

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
        self.assertEqual(
            {
                row["assigned_value_kind"]
                for row in writers["dynamic-object-field-target"]
            },
            {"register"},
        )
        self.assertEqual(
            writers["dynamic-indexed-record-target"][0]["assigned_value_expression"],
            "eax",
        )
        self.assertEqual(len(all_writers["dynamic-object-field-target"]), 2)
        self.assertEqual(len(all_writers["dynamic-indexed-record-target"]), 1)

    def test_tracks_register_derived_dynamic_writer_shapes(self) -> None:
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
    eax = ecx + 0x584;
    MEM32(eax) = edx;
    edi = ebx + esi * 4 + 8;
    MEM32(edi) = edx;
    eax = MEM32(ecx + 0x584);
    MEM32(eax) = edx;
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
                [],
            )

        self.assertEqual(
            [row["expression"] for row in writers["dynamic-object-field-target"]],
            ["MEM32(eax) = edx;"],
        )
        self.assertEqual(
            writers["dynamic-object-field-target"][0]["address_expression"],
            "ecx + 0x584",
        )
        self.assertEqual(
            [row["expression"] for row in writers["dynamic-indexed-record-target"]],
            ["MEM32(edi) = edx;"],
        )
        self.assertEqual(
            writers["dynamic-indexed-record-target"][0]["address_expression"],
            "ebx + esi * 4 + 8",
        )

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

    def test_recovers_nonconstant_absolute_fixed_slot_write(self) -> None:
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
    MEM32(0x00011180) = eax;
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
        self.assertEqual(writes[0x11180][0]["kind"], "absolute")
        self.assertNotIn("value", writes[0x11180][0])

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

    def test_records_nonzero_guard_for_indirect_call(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            generated = Path(temporary) / "generated"
            generated.mkdir()
            (generated / "recomp_0000.c").write_text(
                "void sub_00001000(void)\n"
                "{\n"
                "loc_00001000: ;\n"
                "    _fa = (uint32_t)(MEM32(ecx)) & 0xFFFFFFFFu;\n"
                "    _fb = (uint32_t)(0) & 0xFFFFFFFFu;\n"
                "    if (CMP_NE(_fa, _fb)) goto loc_00001006;\n"
                "    goto loc_0000100A;\n"
                "loc_00001006: ;\n"
                "    { uint32_t _icall_target = MEM32(ecx); "
                "RECOMP_ICALL_SAFE_AT(_icall_target, _icall_esp, 0x00001008u); }\n"
                "loc_0000100A: ;\n"
                "}\n",
                encoding="utf-8",
            )

            evidence = recover_indirect_call_nonzero_guards(
                generated,
                [
                    {
                        "call_site": "0x00001008",
                        "source": "recomp_0000.c:9",
                        "target_expression": "MEM32(ecx)",
                    }
                ],
            )

        self.assertEqual(
            evidence["0x00001008"]["kind"],
            "nonzero-guarded-indirect-call",
        )
        self.assertEqual(
            evidence["0x00001008"]["target_label"],
            "loc_00001006",
        )

    def test_records_symbolic_link_open_query_close_order(self) -> None:
        rows = [
            {
                "function": "0x00001000",
                "function_name": "sub_00001000",
                "classification": "kernel-thunk-slot",
                "kernel_ordinal": ordinal,
                "call_site": call_site,
                "source": source,
                "host_resolver_target": target,
            }
            for ordinal, call_site, source, target in (
                (203, "0x00001010", "recomp_0000.c:10", "xbox_NtOpenSymbolicLinkObject"),
                (215, "0x00001020", "recomp_0000.c:20", "xbox_NtQuerySymbolicLinkObject"),
                (187, "0x00001030", "recomp_0000.c:30", "xbox_NtClose"),
            )
        ]

        evidence = recover_symbolic_link_open_query_close_sequences(rows)

        self.assertEqual(len(evidence), 1)
        self.assertEqual(evidence[0]["status"], "open-query-close-order-observed")
        self.assertEqual(evidence[0]["ordinals"], [203, 215, 187])

    def test_recovers_symbolic_link_guest_path_and_host_query_target(self) -> None:
        data = bytearray(0x400)
        struct.pack_into("<HHI", data, 0x20, 6, 7, 0x10040)
        data[0x40 : 0x40 + 6] = b"\\??\\D:"
        xbe = {
            "data": bytes(data),
            "sections": [
                {
                    "virtual_address": 0x10000,
                    "virtual_size": len(data),
                    "file_address": 0,
                    "file_size": len(data),
                }
            ],
        }
        rows = [
            {
                "classification": "kernel-thunk-slot",
                "kernel_ordinal": 203,
                "call_site": "0x00001008",
                "source": "recomp_0000.c:6",
            }
        ]
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            generated = root / "generated"
            generated.mkdir()
            (generated / "recomp_0000.c").write_text(
                "void sub_00001000(void)\n"
                "{\n"
                "    eax = 0;\n"
                "    MEM32(ebp + -16) = 0x00010020;\n"
                "    PUSH32(esp, eax);\n"
                "    RECOMP_ICALL_SAFE_AT(_icall_target, _icall_esp, 0x00001008u);\n"
                "}\n",
                encoding="utf-8",
            )
            kernel_source = root / "kernel_thunks.c"
            kernel_source.write_text("", encoding="utf-8")
            (root / "kernel_file.c").write_text(
                'const char* target = "\\\\Device\\\\CdRom0";\n',
                encoding="utf-8",
            )
            (root / "kernel_bridge.c").write_text(
                "#define BRIDGE_SYMBOLIC_LINK_HANDLE 0xDEAD0001u\n"
                "static void bridge_NtOpenSymbolicLinkObject(void) {\n"
                "const char *path = bridge_get_xbox_path(obj);\n"
                'if (strcmp(path, "\\\\??\\\\D:") != 0) return;\n'
                "BRIDGE_MEM32(handle_va) = BRIDGE_SYMBOLIC_LINK_HANDLE;\n"
                "}\n"
                "static void bridge_NtQuerySymbolicLinkObject(void) {\n"
                "uint32_t handle = STACK_ARG(0);\n"
                'const char* target = "\\\\Device\\\\CdRom0";\n'
                "if (handle != BRIDGE_SYMBOLIC_LINK_HANDLE) return;\n"
                "}\n",
                encoding="utf-8",
            )

            guest_evidence = recover_symbolic_link_guest_paths(
                generated, xbe, rows
            )
            host_evidence = recover_symbolic_link_host_query_target(kernel_source)
            bridge_evidence = recover_symbolic_link_host_bridge_evidence(kernel_source)

        self.assertEqual(guest_evidence["0x00001008"]["path_text"], "\\??\\D:")
        self.assertEqual(guest_evidence["0x00001008"]["object_name_address"], "0x00010020")
        self.assertEqual(host_evidence["target_text"], "\\Device\\CdRom0")
        self.assertEqual(
            bridge_evidence["status"],
            "bridge-validates-observed-path-and-handle",
        )
        self.assertEqual(bridge_evidence["observed_guest_path"], "\\??\\D:")


if __name__ == "__main__":
    unittest.main()
