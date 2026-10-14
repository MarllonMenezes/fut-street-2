"""Tests for the no-emulation host preflight evidence gate."""

from __future__ import annotations

import subprocess
import unittest
from pathlib import Path

from tools.analysis.recomp_host_preflight_evidence_gate import (
    BRIDGE_MARKER,
    RUNTIME_MARKER,
    STATIC_MARKER,
    run_preflight,
    verify_preflight_output,
)


COMMON = (
    "Dynamic-call guards OK: PRCB cache=0; no guest code executed.\n"
    "Static preflight OK: XBE memory layout initialized and translated entry "
    "point resolved; no guest code executed.\n"
)


class RecompHostPreflightEvidenceGateTests(unittest.TestCase):
    def test_verifies_static_contract(self) -> None:
        result = verify_preflight_output(COMMON, "static")

        self.assertEqual(result["status"], "verified-static-host-preflight-contract")
        self.assertEqual(result["guest_execution"], "none")
        self.assertEqual(result["kernel_bridge"], "not-initialized")

    def test_verifies_runtime_bridge_contract(self) -> None:
        output = (
            "Dynamic-call guards OK: PRCB cache=0; no guest code executed.\n"
            f"{BRIDGE_MARKER}\n{RUNTIME_MARKER}\n"
        )
        result = verify_preflight_output(output, "runtime")

        self.assertEqual(result["status"], "verified-runtime-host-preflight-contract")
        self.assertEqual(result["kernel_bridge"], "128/128-resolved")

    def test_rejects_launch_evidence(self) -> None:
        with self.assertRaisesRegex(ValueError, "guest launch"):
            verify_preflight_output(COMMON + "Calling translated entry\n", "static")

    def test_rejects_missing_runtime_bridge(self) -> None:
        with self.assertRaisesRegex(ValueError, "Kernel thunk bridge"):
            verify_preflight_output(COMMON.replace(STATIC_MARKER, RUNTIME_MARKER), "runtime")

    def test_run_preflight_never_builds_a_launch_command(self) -> None:
        calls: list[list[str]] = []

        def fake_runner(command: list[str], **_: object) -> subprocess.CompletedProcess[str]:
            calls.append(command)
            return subprocess.CompletedProcess(command, 0, COMMON, "")

        result = run_preflight(
            Path(__file__),
            Path(__file__),
            runner=fake_runner,
        )

        self.assertEqual(result["status"], "verified-static-host-preflight-contract")
        self.assertEqual(len(calls), 1)
        self.assertIn("--preflight", calls[0])
        self.assertNotIn("--launch", calls[0])


if __name__ == "__main__":
    unittest.main()
