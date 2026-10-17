"""Synthetic contracts for adding a recovered callback to generated dispatch."""
import unittest
import struct

from tools.analysis.recomp_recover_observed_callback import (
    read_dispatch, register_callback, validate_contained_callback, validate_switch,
    validate_linear_leaf, validate_tail_jump_stub_gap,
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
