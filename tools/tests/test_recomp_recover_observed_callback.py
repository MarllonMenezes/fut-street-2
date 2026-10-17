"""Synthetic contracts for adding a recovered callback to generated dispatch."""
import unittest

from tools.analysis.recomp_recover_observed_callback import read_dispatch, register_callback


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


if __name__ == "__main__":
    unittest.main()
