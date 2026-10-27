"""Synthetic contracts for adding a recovered callback to generated dispatch."""
import unittest
import struct
from types import SimpleNamespace

from tools.analysis.recomp_recover_observed_callback import (
    read_dispatch, register_callback, validate_contained_callback, validate_switch,
    validate_linear_leaf, validate_tail_jump_stub_gap, validate_int3_padding_gap,
    validate_slot_callback_gap, validate_padded_callback_gap, validate_data_reference,
)


def dispatch(rows, count=None):
    return ("/* Maps 2 Xbox VAs */\nstatic const recomp_entry_t g_recomp_table[] = {\n" +
            "".join(f"    {{ 0x{a:08X}u, (recomp_func_t){symbol} }},\n" for a, symbol in rows) +
            f"}};\nstatic const size_t g_recomp_table_size = {len(rows) if count is None else count};\n")


class RecoveryRegistrationTests(unittest.TestCase):
    def setUp(self):
        self.rows = [(0x1000, "sub_00001000"), (0x1200, "sub_00001200")]
        self.header = '#include "recomp_types.h"\nvoid sub_00001000(void);\nvoid sub_00001200(void);\n'

    def test_preserves_neighbours_and_registers_exact_start_only(self):
        text, header = register_callback(dispatch(self.rows), self.header, 0x1100)
        self.assertEqual(read_dispatch(text), [self.rows[0], (0x1100, "sub_00001100"), self.rows[1]])
        self.assertIn("Maps 3 Xbox VAs", text)
        self.assertIn("void sub_00001100(void);", header)
        self.assertNotIn(0x1101, dict(read_dispatch(text)))
        self.assertEqual(header.count("void sub_00001000(void);"), 1)

    def test_refuses_existing_start(self):
        with self.assertRaisesRegex(ValueError, "already registered"):
            register_callback(dispatch(self.rows), self.header, 0x1000)

    def test_refuses_unbounded_recovery(self):
        with self.assertRaisesRegex(ValueError, "following"):
            register_callback(dispatch(self.rows), self.header, 0x1300)

    def test_refuses_bad_size_duplicate_and_unsorted_dispatch(self):
        for rows, count in [(self.rows, 1), (self.rows * 2, 4), (self.rows[::-1], 2)]:
            with self.subTest(rows=rows, count=count), self.assertRaises(ValueError):
                read_dispatch(dispatch(rows, count))

    def test_refuses_unrecognized_header(self):
        with self.assertRaisesRegex(ValueError, "header"):
            register_callback(dispatch(self.rows), "", 0x1100)

    def test_refuses_orphan_declaration_without_duplicating_it(self):
        with self.assertRaisesRegex(ValueError, "already registered"):
            register_callback(dispatch(self.rows), self.header + "void sub_00001100(void);", 0x1100)


class DataCallbackTests(unittest.TestCase):
    def test_original_pointer_is_aligned_and_inside_noncode_rdata(self):
        data = bytes(8) + struct.pack('<I', 0x1000) + bytes(4)
        section = SimpleNamespace(name='.rdata', is_code=False, va=0x2000, raw_size=8, raw_addr=8)
        validate_data_reference(data, [section], 0x2000, 0x1000)
        for ref, target, candidate in [(0x2001,0x1000,section), (0x2000,0x1001,section),
                                       (0x2008,0x1000,section),
                                       (0x2000,0x1000,SimpleNamespace(name='.rdata',is_code=True,va=0x2000,raw_size=8,raw_addr=8))]:
            with self.subTest(ref=ref,target=target), self.assertRaises(ValueError):
                validate_data_reference(data,[candidate],ref,target)

    def test_padded_callback_rejects_bad_flow_padding_and_extent(self):
        body=bytes.fromhex('90c3cccc')
        self.assertEqual(len(validate_padded_callback_gap(body,0x1000,0x1002,0x1004,set(),1024)),2)
        for code, maximum in [(bytes.fromhex('90c390cc'),1024),
                              (bytes.fromhex('eb01c3cc'),1024), (body,3)]:
            with self.subTest(code=code), self.assertRaises(ValueError):
                validate_padded_callback_gap(code,0x1000,0x1002,0x1004,set(),maximum)


class SwitchRecoveryTests(unittest.TestCase):
    def setUp(self):
        self.code = bytes.fromhex('83f8017708ff24850e100000c3c3') + struct.pack('<2I', 0x100c, 0x100d)

    def test_bounded_table_preserves_both_arms(self):
        ins, targets, jump = validate_switch(self.code, 0x1000, 0x1016, 0x100e, 2)
        self.assertEqual(targets, [0x100c, 0x100d])
        self.assertEqual(jump, 0x1005)
        self.assertEqual(ins[-1].address, 0x100d)

    def test_rejects_wrong_bound_signed_guard_index_and_destination(self):
        for offset, value in [(2, 2), (3, 0x7f), (6, 0x20), (7, 0x8d), (14, 1)]:
            code = bytearray(self.code)
            code[offset] = value
            with self.subTest(offset=offset), self.assertRaises(ValueError):
                validate_switch(bytes(code), 0x1000, 0x1016, 0x100e, 2)

    def test_rejects_unaccounted_trailing_bytes(self):
        with self.assertRaises(ValueError):
            validate_switch(self.code + b'\0', 0x1000, 0x1017, 0x100e, 2)

    def test_rejects_table_arm_that_bypasses_guard(self):
        code = self.code[:14] + struct.pack('<2I', 0x1005, 0x100d)
        with self.assertRaisesRegex(ValueError, 'bypasses'):
            validate_switch(code, 0x1000, 0x1016, 0x100e, 2)


class ContainedCallbackTests(unittest.TestCase):
    def setUp(self):
        # push 0x1005; callback: nop; ret
        self.data = bytes.fromhex("680510000090c3")

    def test_requires_owner_boundary_end_and_literal_reference(self):
        ins, refs = validate_contained_callback(
            self.data, -0x1000, -0x1000, 0x1000, 0x1007, 0x1005, 0x1007,
            0x1000, 0x1000, 0x1007)
        self.assertEqual(ins[-2].address, 0x1005)
        self.assertEqual(refs, [0x1000])

    def test_rejects_missing_literal_reference(self):
        with self.assertRaisesRegex(ValueError, "literal callback reference"):
            validate_contained_callback(bytes.fromhex("909090c3"), -0x1000, -0x1000,
                                        0x1000, 0x1004, 0x1001, 0x1004,
                                        0x1000, 0x1000, 0x1004)

    def test_rejects_wrong_owner_and_non_boundary_start(self):
        with self.assertRaisesRegex(ValueError, "explicit owner"):
            validate_contained_callback(self.data, -0x1000, -0x1000, 0x1000, 0x1007,
                                        0x1005, 0x1007, 0x2000, 0x1000, 0x1007)
        with self.assertRaisesRegex(ValueError, "instruction boundary"):
            validate_contained_callback(self.data, -0x1000, -0x1000, 0x1000, 0x1007,
                                        0x1002, 0x1007, 0x1000, 0x1000, 0x1007)

    def test_rejects_non_owner_end_and_incomplete_owner_decode(self):
        with self.assertRaisesRegex(ValueError, "end at owner end"):
            validate_contained_callback(self.data, -0x1000, -0x1000, 0x1000, 0x1007,
                                        0x1005, 0x1006, 0x1000, 0x1000, 0x1007)
        with self.assertRaisesRegex(ValueError, "instruction boundary"):
            validate_contained_callback(bytes.fromhex("0f90c3"), -0x1000, -0x1000,
                                        0x1000, 0x1003,
                                        0x1001, 0x1003, 0x1000, 0x1000, 0x1003)


class TailJumpStubTests(unittest.TestCase):
    def setUp(self):
        self.code = bytes.fromhex("e99ec7ffffe99bc8ffff")
        self.start = 0x240AFB

    def test_proves_each_direct_jump_and_observed_entry_boundary(self):
        ins, targets = validate_tail_jump_stub_gap(
            self.code, self.start, self.start + 5, self.start + 10,
            {0x23D29E, 0x23D3A0})
        self.assertEqual([i.address for i in ins], [self.start, self.start + 5])
        self.assertEqual(targets, [0x23D29E, 0x23D3A0])

    def test_rejects_wrong_entry_end_and_unregistered_destination(self):
        with self.assertRaisesRegex(ValueError, "first instruction boundary"):
            validate_tail_jump_stub_gap(self.code, self.start, self.start + 4,
                                        self.start + 10, {0x23D29E, 0x23D3A0})
        with self.assertRaisesRegex(ValueError, "not registered"):
            validate_tail_jump_stub_gap(self.code, self.start, self.start + 5,
                                        self.start + 10, {0x23D3A0})

    def test_rejects_indirect_non_jump_and_incomplete_gap(self):
        for code in (bytes.fromhex("ffe0e99bc8ffff"),
                     bytes.fromhex("9090909090e99bc8ffff"),
                     self.code[:-1]):
            with self.subTest(code=code), self.assertRaises(ValueError):
                validate_tail_jump_stub_gap(code, self.start, self.start + 5,
                                            self.start + 10,
                                            {0x23D29E, 0x23D3A0})


class Int3PaddingTests(unittest.TestCase):
    def setUp(self):
        self.body = bytes.fromhex("8b442404c3")
        self.reference = bytes.fromhex("6800100000c3")

    def test_accepts_complete_body_padding_and_exact_push_reference(self):
        instructions, sites = validate_int3_padding_gap(self.body+b"\xCC"*3,
            0x1000,0x1005,0x1008,self.reference,0x2000)
        self.assertEqual([i.mnemonic for i in instructions],["mov","ret"])
        self.assertEqual(sites,[0x2000])

    def test_accepts_terminal_direct_jump_without_translating_padding(self):
        instructions, _ = validate_int3_padding_gap(bytes.fromhex("e9fb0f0000cccccc"),
            0x1000,0x1005,0x1008,self.reference,0x2000)
        self.assertEqual(instructions[-1].mnemonic,"jmp")
        self.assertEqual(instructions[-1].op_str,"0x2000")

    def test_rejects_wrong_padding_incomplete_body_and_unproven_end(self):
        for code,end in ((self.body+b"\x90"*3,0x1005),
                         (self.body+b"\xCC"*2,0x1005),
                         (bytes.fromhex("8b44240fcccccccc"),0x1005),
                         (bytes.fromhex("9090909090cccccc"),0x1005),
                         (bytes.fromhex("cc909090c3cccccc"),0x1005)):
            with self.subTest(code=code),self.assertRaises(ValueError):
                validate_int3_padding_gap(code,0x1000,end,0x1008,self.reference,0x2000)

    def test_rejects_absent_duplicate_or_non_push_literal_reference(self):
        for reference in (b"\xC3",self.reference+self.reference,
                          bytes.fromhex("b800100000c3"),b"\x0F"):
            with self.subTest(reference=reference),self.assertRaises(ValueError):
                validate_int3_padding_gap(self.body+b"\xCC"*3,0x1000,0x1005,
                    0x1008,reference,0x2000)

    def test_rejects_zero_padding_excessive_padding_and_large_gap(self):
        for code,end,gap in ((self.body,0x1005,0x1005),
                             (self.body+b"\xCC"*16,0x1005,0x1015),
                             (b"\x90"*64+b"\xC3\xCC",0x1041,0x1042)):
            with self.subTest(gap=gap),self.assertRaises(ValueError):
                validate_int3_padding_gap(code,0x1000,end,gap,self.reference,0x2000)


class SlotCallbackTests(unittest.TestCase):
    def setUp(self):
        # TEST/JZ select either a registered tail jump or a later RET.
        self.body=bytes.fromhex("85c07405e9f70f0000c3")
        self.reference=bytes.fromhex("c7812402000000100000c3")

    def proof(self,body=None,reference=None,slot=0x224,registered=None):
        body=self.body if body is None else body
        return validate_slot_callback_gap(body+b"\xCC"*3,0x1000,0x1000+len(body),
            0x1003+len(body),self.reference if reference is None else reference,
            0x3000,slot,{0x2000} if registered is None else registered)

    def test_accepts_two_exits_and_exact_slot_reference(self):
        instructions,sites=self.proof()
        self.assertEqual(instructions[-1].mnemonic,"ret")
        self.assertEqual(sites,[0x3000])

    def test_rejects_unregistered_tail_conditional_external_and_indirect_flow(self):
        for body,registered in ((self.body,set()),
                (bytes.fromhex("0f84fa0f0000c3"),{0x2000}),
                (bytes.fromhex("ffd0c3"),{0x2000}),
                (bytes.fromhex("ffe0c3"),{0x2000}),
                (bytes.fromhex("e800000000c3"),{0x2000})):
            with self.subTest(body=body),self.assertRaises(ValueError):
                self.proof(body=body,registered=registered)

    def test_rejects_branch_interior_and_unreachable_embedded_bytes(self):
        for body in (bytes.fromhex("ebff90c3"),bytes.fromhex("c390c3"),
                     bytes.fromhex("eb0190c3"),bytes.fromhex("ccc3")):
            with self.subTest(body=body),self.assertRaises(ValueError):
                self.proof(body=body)

    def test_rejects_wrong_slot_absolute_indexed_and_duplicate_writes(self):
        for reference,slot in ((self.reference,0x228),(self.reference,0x225),
                (bytes.fromhex("c7052402000000100000c3"),0x224),
                (bytes.fromhex("c784812402000000100000c3"),0x224),
                (self.reference+self.reference,0x224),
                (bytes.fromhex("c7812402000001100000c3"),0x224)):
            with self.subTest(reference=reference,slot=slot),self.assertRaises(ValueError):
                self.proof(reference=reference,slot=slot)

    def test_rejects_non_int3_padding_and_oversized_gap(self):
        with self.assertRaises(ValueError):
            validate_slot_callback_gap(self.body+b"\x90"*3,0x1000,0x100A,0x100D,
                self.reference,0x3000,0x224,{0x2000})
        with self.assertRaises(ValueError):
            self.proof(body=b"\x90"*512+b"\xC3")

    def test_memory_calls_require_explicit_opt_in_and_preserve_flow(self):
        body=bytes.fromhex("ff17ff912c020000c3")
        args=(body+b"\xCC"*3,0x1000,0x1000+len(body),0x1003+len(body),
              self.reference,0x3000,0x224,set())
        with self.assertRaises(ValueError):
            validate_slot_callback_gap(*args)
        instructions,_=validate_slot_callback_gap(*args,allow_indirect_calls=True)
        self.assertEqual([i.mnemonic for i in instructions],["call","call","ret"])
        for body in (bytes.fromhex("ffd0c3"),bytes.fromhex("ff27c3")):
            with self.subTest(body=body),self.assertRaises(ValueError):
                validate_slot_callback_gap(body+b"\xCC"*3,0x1000,0x1000+len(body),
                    0x1003+len(body),self.reference,0x3000,0x224,set(),True)

    def test_larger_callback_remains_bounded_to_512_bytes(self):
        body=b"\x90"*300+b"\xC3"
        instructions,_=validate_slot_callback_gap(body+b"\xCC"*3,0x1000,0x112D,0x1130,
            self.reference,0x3000,0x224,set())
        self.assertEqual(len(instructions),301)


class LinearLeafTests(unittest.TestCase):
    def test_accepts_complete_leaf_with_one_terminal_ret(self):
        instructions = validate_linear_leaf(bytes.fromhex("535bc3"), 0x1000, 0x1003)
        self.assertEqual([i.mnemonic for i in instructions], ["push", "pop", "ret"])

    def test_rejects_internal_ret_branches_calls_incomplete_and_wrong_end(self):
        for code, end in ((bytes.fromhex("c390c3"), 0x1003),
                          (bytes.fromhex("eb00c3"), 0x1003),
                          (bytes.fromhex("e800000000c3"), 0x1006),
                          (bytes.fromhex("0f"), 0x1001),
                          (bytes.fromhex("535bc3"), 0x1004)):
            with self.subTest(code=code, end=end), self.assertRaises(ValueError):
                validate_linear_leaf(code, 0x1000, end)


if __name__ == "__main__":
    unittest.main()
