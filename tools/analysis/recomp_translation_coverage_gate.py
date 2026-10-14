"""Verify the static translation-coverage contract for the recompilation.

This gate joins the direct-entry call graph, the untranslated-site alignment,
and the bounded ASM context control.  It proves only that the generated
artifact covers the direct and tail-augmented static scopes and that the two
remaining untranslated instructions are outside those scopes.  It does not
execute the XBE, generated C, or any host runtime, and it does not assign
semantics to ``SGDT`` or ``OUT``.
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path
from typing import Any

try:
    from .recomp_entry_call_graph_audit import audit as audit_call_graph
    from .recomp_unimplemented_context_control import audit as audit_context
    from .recomp_unimplemented_evidence_audit import audit as audit_alignment
except ImportError:  # pragma: no cover - supports direct script execution
    from recomp_entry_call_graph_audit import audit as audit_call_graph
    from recomp_unimplemented_context_control import audit as audit_context
    from recomp_unimplemented_evidence_audit import audit as audit_alignment


EXPECTED_ENTRY_POINT = "0x0024D8BE"
EXPECTED_UNIMPLEMENTED_SITES = {
    ("0x00253FD8", "sgdt [esp + 6]"),
    ("0x00298030", "out dx, al"),
}
EXPECTED_CALL_GRAPH_SUMMARY = {
    "entry_point": EXPECTED_ENTRY_POINT,
    "metadata_functions": 15597,
    "directly_reachable_functions": 78,
    "direct_external_call_targets": 0,
    "generated_function_definitions": 15600,
    "directly_reachable_missing_translations": 0,
    "generated_direct_tail_edges": 1833,
    "direct_reachable_direct_tail_edges": 2,
    "tail_augmented_directly_reachable_functions": 80,
    "tail_augmented_new_functions": 2,
    "tail_augmented_external_targets": 0,
    "tail_augmented_missing_translations": 0,
    "generated_fallthrough_edges": 38,
    "direct_reachable_fallthrough_edges": 0,
    "fallthrough_targets_missing_from_metadata": [],
    "fallthrough_targets_missing_from_generated": [],
    "unimplemented_sites": 2,
    "unimplemented_sites_in_directly_reachable_function_scopes": 0,
    "unimplemented_sites_in_tail_augmented_function_scopes": 0,
    "unimplemented_direct_reachable_site_addresses": [],
    "unimplemented_outside_direct_reachable_site_addresses": [
        "0x00253FD8",
        "0x00298030",
    ],
    "unimplemented_tail_augmented_site_addresses": [],
    "unimplemented_outside_tail_augmented_scope_addresses": [
        "0x00253FD8",
        "0x00298030",
    ],
    "tail_augmented_unimplemented_status": (
        "no-unimplemented-sites-in-tail-augmented-scopes"
    ),
}


def _require_equal(
    failures: list[str], key: str, actual: Any, expected: Any
) -> None:
    if actual != expected:
        failures.append(f"{key}: expected {expected!r}, got {actual!r}")


def verify_gate(result: dict[str, Any]) -> dict[str, Any]:
    """Verify the joined static translation-coverage contract."""
    call_graph = result.get("call_graph")
    alignment = result.get("alignment")
    context = result.get("context")
    if not isinstance(call_graph, dict):
        raise ValueError("translation coverage result has no call_graph object")
    if not isinstance(alignment, dict):
        raise ValueError("translation coverage result has no alignment object")
    if not isinstance(context, dict):
        raise ValueError("translation coverage result has no context object")

    summary = call_graph.get("summary")
    alignment_summary = alignment.get("summary")
    context_summary = context.get("summary")
    if not isinstance(summary, dict):
        raise ValueError("call_graph result has no summary object")
    if not isinstance(alignment_summary, dict):
        raise ValueError("alignment result has no summary object")
    if not isinstance(context_summary, dict):
        raise ValueError("context result has no summary object")

    failures: list[str] = []
    for key, expected in EXPECTED_CALL_GRAPH_SUMMARY.items():
        _require_equal(failures, f"call graph {key}", summary.get(key), expected)

    _require_equal(
        failures,
        "call graph status",
        summary.get("status"),
        "verified-direct-call-translation-coverage",
    )
    _require_equal(
        failures,
        "alignment status",
        alignment_summary.get("status"),
        "verified-unimplemented-evidence-alignment",
    )
    _require_equal(failures, "alignment summary sites", alignment_summary.get("summary_sites"), 2)
    _require_equal(failures, "alignment generated sites", alignment_summary.get("generated_sites"), 2)
    _require_equal(failures, "alignment ASM sites", alignment_summary.get("asm_sites"), 2)
    _require_equal(
        failures,
        "context status",
        context_summary.get("status"),
        "verified-static-unimplemented-opcode-contexts",
    )
    _require_equal(failures, "context site count", context_summary.get("site_count"), 2)
    _require_equal(
        failures,
        "context lowering status",
        context_summary.get("lowering_status"),
        "2-sites-remain-unimplemented",
    )
    _require_equal(
        failures,
        "context hardware semantics",
        context_summary.get("hardware_semantics"),
        "not-proven",
    )

    alignment_sites = {
        (row.get("address"), row.get("instruction"))
        for row in alignment.get("sites", [])
    }
    context_sites = {
        (row.get("address"), row.get("instruction"))
        for row in context.get("sites", [])
    }
    _require_equal(failures, "aligned untranslated sites", alignment_sites, EXPECTED_UNIMPLEMENTED_SITES)
    _require_equal(failures, "context untranslated sites", context_sites, EXPECTED_UNIMPLEMENTED_SITES)

    if failures:
        raise ValueError("translation coverage gate failed: " + "; ".join(failures))

    return {
        "status": "verified-recompilation-translation-coverage-contract",
        "entry_point": EXPECTED_ENTRY_POINT,
        "directly_reachable_functions": 78,
        "tail_augmented_functions": 80,
        "direct_reachable_untranslated_sites": 0,
        "tail_augmented_untranslated_sites": 0,
        "untranslated_sites": 2,
        "hardware_semantics": "not-proven",
        "original_semantics": "not-claimed",
        "guest_execution": "none",
    }


def audit(
    functions_path: Path,
    generated_dir: Path,
    summary_path: Path,
    asm_dir: Path,
    entry: int = 0x0024D8BE,
) -> dict[str, Any]:
    """Run all three static audits and verify their joined contract."""
    result = {
        "call_graph": audit_call_graph(functions_path, generated_dir, entry),
        "alignment": audit_alignment(summary_path, generated_dir, asm_dir),
        "context": audit_context(asm_dir),
    }
    result["gate"] = verify_gate(result)
    return result


def _parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--functions", type=Path, required=True)
    parser.add_argument("--generated", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    parser.add_argument("--asm-dir", type=Path, required=True)
    parser.add_argument("--entry", type=lambda value: int(value, 0), default=0x0024D8BE)
    parser.add_argument("--format", choices=("json", "text"), default="text")
    return parser


def main() -> int:
    args = _parser().parse_args()
    result = audit(
        args.functions,
        args.generated,
        args.summary,
        args.asm_dir,
        args.entry,
    )
    if args.format == "json":
        print(json.dumps(result, indent=2, sort_keys=True))
    else:
        for key, value in result["gate"].items():
            print(f"{key}={value}")
        print(
            "recomp_translation_coverage_gate: verified static coverage "
            "without guest execution or opcode semantic promotion"
        )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
