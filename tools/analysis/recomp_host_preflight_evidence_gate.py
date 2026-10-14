"""Verify the no-emulation host preflight contract.

The host preflight may read the supplied XBE and initialize the compatibility
layout, but it must stop before the translated entry point is called.  This
gate makes that boundary and the runtime bridge result machine-checkable.  It
does not launch the XBE and it is not a boot, gameplay, rendering, audio, or
fidelity test.
"""

from __future__ import annotations

import argparse
import json
import os
import subprocess
from pathlib import Path
from typing import Any, Callable, Sequence


FORBIDDEN_ENVIRONMENT = ("RECOMP_AC97_READY", "RECOMP_PB_EXEC", "RECOMP_VBLANK")
COMMON_MARKERS = (
    "Loaded 11/11 sections",
    "xbox_MemoryLayoutInit: complete",
    "Dynamic-call guards OK:",
    "no guest code executed.",
    "translated entry point resolved",
)
STATIC_MARKER = (
    "Static preflight OK: XBE memory layout initialized and translated entry "
    "point resolved; no guest code executed."
)
RUNTIME_MARKER = (
    "Runtime preflight OK: XBE memory, kernel bridge, dispatch and translated "
    "entry point resolved; no guest code executed."
)
BRIDGE_MARKER = "Kernel thunk bridge: 128/128 resolved (128 bridged, 0 stub)"


def verify_preflight_output(
    output: str, mode: str, returncode: int = 0
) -> dict[str, Any]:
    """Verify captured host output without executing a target in this function."""
    if mode not in ("static", "runtime"):
        raise ValueError(f"unsupported preflight mode: {mode!r}")
    if returncode != 0:
        raise ValueError(f"host preflight returned exit code {returncode}")
    missing = [marker for marker in COMMON_MARKERS if marker not in output]
    expected = RUNTIME_MARKER if mode == "runtime" else STATIC_MARKER
    if expected not in output:
        missing.append(expected)
    if mode == "runtime" and BRIDGE_MARKER not in output:
        missing.append(BRIDGE_MARKER)
    if "Calling translated entry" in output or "--launch" in output:
        raise ValueError("preflight output indicates guest launch")
    if missing:
        raise ValueError("host preflight evidence incomplete: " + "; ".join(missing))

    return {
        "status": f"verified-{mode}-host-preflight-contract",
        "mode": mode,
        "translated_entry_point": "resolved",
        "dynamic_call_guards": "zero-before-guest-call",
        "kernel_bridge": "128/128-resolved" if mode == "runtime" else "not-initialized",
        "guest_execution": "none",
        "original_semantics": "not-claimed",
    }


def run_preflight(
    executable: Path,
    xbe: Path,
    mode: str = "static",
    data_directory: Path | None = None,
    save_directory: Path | None = None,
    runner: Callable[..., subprocess.CompletedProcess[str]] = subprocess.run,
) -> dict[str, Any]:
    """Run only the host's explicit ``--preflight`` mode and verify its output."""
    if mode not in ("static", "runtime"):
        raise ValueError(f"unsupported preflight mode: {mode!r}")
    if not executable.is_file():
        raise ValueError(f"host executable not found: {executable}")
    if not xbe.is_file():
        raise ValueError(f"XBE input not found: {xbe}")
    if mode == "runtime" and (data_directory is None or save_directory is None):
        raise ValueError("runtime preflight requires data and save directories")
    if any(os.environ.get(name) is not None for name in FORBIDDEN_ENVIRONMENT):
        raise ValueError("emulator or synthetic-hardware environment is enabled")

    command: list[str] = [str(executable), "--preflight", str(xbe)]
    if mode == "runtime":
        command.extend((str(data_directory), str(save_directory)))
    completed = runner(
        command,
        capture_output=True,
        text=True,
        check=False,
    )
    output = (completed.stdout or "") + (completed.stderr or "")
    result = verify_preflight_output(output, mode, completed.returncode)
    result["command"] = command
    return result


def _parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--executable", type=Path, required=True)
    parser.add_argument("--xbe", type=Path, required=True)
    parser.add_argument("--mode", choices=("static", "runtime"), default="static")
    parser.add_argument("--data-directory", type=Path)
    parser.add_argument("--save-directory", type=Path)
    parser.add_argument("--format", choices=("json", "text"), default="text")
    return parser


def main() -> int:
    args = _parser().parse_args()
    result = run_preflight(
        args.executable,
        args.xbe,
        args.mode,
        args.data_directory,
        args.save_directory,
    )
    if args.format == "json":
        print(json.dumps(result, indent=2, sort_keys=True))
    else:
        for key, value in result.items():
            print(f"{key}={value}")
        print("recomp_host_preflight_evidence_gate: passed; guest_execution=none")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
