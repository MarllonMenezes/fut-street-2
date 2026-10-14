"""Verify the cross-report contract for the recovered EAGL render chain.

This gate joins four independently produced, static reports: indexed draw
sources, render parameters, material declarations and model/material
references.  Matching counts and identities reduce the chance that one
report silently drifts from another, but they do not prove runtime selection,
GPU effects, or original rendering semantics.  It never executes the XBE.
"""

from __future__ import annotations

import argparse
import csv
import json
from pathlib import Path
from typing import Any


EXPECTED_XBE_SHA256 = (
    "5FABB5B763BE0CC8823E814EE343AC5BE6950305330F46C5170E08EAADEA910B"
)

EXPECTED = {
    "draw": {
        "draw_rows": "2364",
        "verified_local_sources": "2363",
        "unresolved_sources": "1",
        "rejected_sources": "0",
        "independent_index_bytes": "1739618",
        "static_spans": "5",
        "static_sites": "37",
        "negative_controls": "4",
        "draw_report_sha256": "1EBF623119A7C53B883A9820C26EAE1F3614BF884C135F154980B4AD72CB0CBA",
        "parameter_report_sha256": "30FB26CA3856FC0741695C58F2BD1313482BAF4E6F40BDB16C98CEF5B11DE8D6",
        "source_binding": "descriptor-local-count-and-index-pointer-to-command26",
        "vertex_attributes": "not-proven",
        "runtime_selector": "unresolved",
        "full_rendering_binding": "not-proven",
        "original_semantics": "not-claimed",
        "status": "verified-independent-local-indexed-draw-source-control",
        "static_report_sha256": "819D371B56C2CC8DE4B0B9110EB87E1C58D8BC48D1E491F05655C0B9CAF1489F",
    },
    "parameter": {
        "primitive_rows": "2364",
        "command_rows": "36779",
        "parameter_pairs": "32051",
        "fetch_base_record_matches": "11818",
        "fetch_stride_matches": "11818",
        "fetch_runtime_selector_unresolved": "7092",
        "draw_commands": "2364",
        "draw_condition_nonzero": "2364",
        "tar_state_parameters": "2364",
        "geo_state_parameters": "1612",
        "primitives_with_both_state_parameters": "1612",
        "parameter_report_sha256": "30FB26CA3856FC0741695C58F2BD1313482BAF4E6F40BDB16C98CEF5B11DE8D6",
        "chain_report_sha256": "A8ED71E43FA835682D59B5A7B3BE5750B7371EE888B9058714395DB85394859E",
        "static_spans_validated": "20",
        "static_sites_validated": "106",
        "negative_controls_passed": "6",
        "gpu_method_headers_verified": "3",
        "draw_opcode_binding": "verified-static-command26-conditional-indexed-submission",
        "runtime_selector": "unresolved",
        "vertex_attribute_binding": "not-proven",
        "full_rendering_binding": "not-proven",
        "reference": "https://github.com/XboxDev/nxdk/blob/7e8d4fc3d00ca59e88298e912438ac215cf93392/lib/pbkit/nv_regs.h",
        "status": "verified-static-eagl-parameter-and-indexed-draw-control",
        "original_semantics": "not-claimed",
        "static_report_sha256": "2F3AA7F3750481249B36417EBBD5E90B4009EA85C488551A1FE79C5EB960291B",
    },
    "material": {
        "source_path": "data/initstaz.big!initstay.big!mat.o",
        "source_sha256": "75DBC11557C6F3E4A32D6D88372476398B3A4C435E3907C2902E38579FB29430",
        "declaration_elements": "5",
        "declaration_bytes": "44",
        "vertex_instruction_count": "46",
        "vertex_program_bytes": "740",
        "static_spans": "4",
        "static_sites": "51",
        "negative_controls": "7",
        "size_tables_address": "0x0029C760",
        "size_tables_sha256": "2F6CF9E3BD99EB8EEB79080DD0867A4E0A68D14B7A3220A07BA704F8882FBB3D",
        "declaration_report_sha256": "D439A519D72E2F9718EAD9A312CD8C3997C3A60A43EC9398014F966F79488F6B",
        "static_report_sha256": "70AA5EDE3857520172410B6260EC7671095FAA565027593BAD2882236B1BF286",
        "status": "verified-independent-material-declaration-and-vs-envelope-control",
        "attribute_semantics": "not-proven",
        "parent_inheritance_binding": "not-proven",
        "full_rendering_binding": "not-proven",
        "original_semantics": "not-claimed",
    },
    "model": {
        "model_rows": "2364",
        "model_objects": "883",
        "material_definitions": "3",
        "declarations_matched": "2364",
        "parent_1c_2c_same_reference_rows": "2364",
        "skin_rows": "1416",
        "cloth_rows": "594",
        "eye_rows": "354",
        "source_path": "data/initstaz.big!initstay.big!mat.o",
        "source_sha256": "75DBC11557C6F3E4A32D6D88372476398B3A4C435E3907C2902E38579FB29430",
        "declaration_sha256": "2205EC6F6DEB9CB77F0A6B23755DBB2C98C833355CABF8B4C61F5C8DD8468CBE",
        "report_sha256": "A8A6BC0A6D79258586F40B740DF327ADBCD5C5B15D518135B79DFDEAF120DC50",
        "static_report_sha256": "11DDCA2A39CEEE01C9609AF75FF99123A4DE6260D8640233A42F45AA062627DC",
        "static_spans": "4",
        "static_sites": "53",
        "negative_controls": "16",
        "status": "verified-independent-model-material-reference-control",
        "reference_scope": "serialized-reference-to-exact-named-program-definition",
        "runtime_loader_resolution": "not-proven",
        "selected_buffers": "not-proven",
        "full_rendering_binding": "not-proven",
        "original_semantics": "not-claimed",
    },
}


def read_summary(path: Path) -> dict[str, str]:
    """Read one one-row TSV summary and reject malformed evidence."""
    with path.open("r", encoding="utf-8", newline="") as handle:
        rows = list(csv.DictReader(handle, delimiter="\t"))
    if len(rows) != 1:
        raise ValueError(f"summary must contain exactly one data row: {path}")
    row = rows[0]
    if not row or any(key is None or value is None for key, value in row.items()):
        raise ValueError(f"summary contains an empty header/value: {path}")
    return {str(key): str(value) for key, value in row.items()}


def _require_equal(failures: list[str], key: str, actual: Any, expected: Any) -> None:
    if actual != expected:
        failures.append(f"{key}: expected {expected!r}, got {actual!r}")


def verify_chain(summaries: dict[str, dict[str, str]]) -> dict[str, Any]:
    """Verify exact report identities, counts, joins and conservative caveats."""
    failures: list[str] = []
    for name, expected in EXPECTED.items():
        observed = summaries.get(name)
        if observed is None:
            failures.append(f"missing summary: {name}")
            continue
        for key, value in expected.items():
            _require_equal(failures, f"{name}.{key}", observed.get(key), value)

    draw = summaries.get("draw", {})
    parameter = summaries.get("parameter", {})
    material = summaries.get("material", {})
    model = summaries.get("model", {})
    _require_equal(failures, "draw/parameter XBE identity", draw.get("xbe_sha256"), EXPECTED_XBE_SHA256)
    _require_equal(failures, "parameter XBE identity", parameter.get("xbe_sha256"), EXPECTED_XBE_SHA256)
    _require_equal(failures, "material XBE identity", material.get("xbe_sha256"), EXPECTED_XBE_SHA256)
    _require_equal(failures, "model XBE identity", model.get("xbe_sha256"), EXPECTED_XBE_SHA256)
    _require_equal(failures, "draw/parameter primitive join", draw.get("draw_rows"), parameter.get("primitive_rows"))
    _require_equal(failures, "parameter/model primitive join", parameter.get("primitive_rows"), model.get("model_rows"))
    _require_equal(failures, "material/model source join", material.get("source_sha256"), model.get("source_sha256"))
    _require_equal(failures, "model declaration identity", model.get("declaration_sha256"), "2205EC6F6DEB9CB77F0A6B23755DBB2C98C833355CABF8B4C61F5C8DD8468CBE")
    _require_equal(failures, "model material-kind partition", sum(int(model.get(key, "-1")) for key in ("skin_rows", "cloth_rows", "eye_rows")), int(model.get("model_rows", "-1")))
    _require_equal(failures, "model declaration coverage", model.get("declarations_matched"), model.get("model_rows"))
    _require_equal(failures, "parameter draw coverage", parameter.get("draw_commands"), parameter.get("primitive_rows"))
    _require_equal(failures, "parameter nonzero coverage", parameter.get("draw_condition_nonzero"), parameter.get("draw_commands"))

    if failures:
        raise ValueError("EAGL static render-chain gate failed: " + "; ".join(failures))

    return {
        "status": "verified-static-eagl-render-chain-control",
        "draw_rows": 2364,
        "model_rows": 2364,
        "material_definitions": 3,
        "declaration_elements": 5,
        "vertex_instruction_count": 46,
        "cross_report_joins": 8,
        "runtime_selector": "unresolved",
        "vertex_attribute_binding": "not-proven",
        "selected_buffers": "not-proven",
        "full_rendering_binding": "not-proven",
        "original_semantics": "not-claimed",
        "guest_execution": "none",
    }


def _parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--draw-summary", type=Path, required=True)
    parser.add_argument("--parameter-summary", type=Path, required=True)
    parser.add_argument("--material-summary", type=Path, required=True)
    parser.add_argument("--model-summary", type=Path, required=True)
    parser.add_argument("--format", choices=("json", "text"), default="text")
    return parser


def main() -> int:
    args = _parser().parse_args()
    summaries = {
        "draw": read_summary(args.draw_summary),
        "parameter": read_summary(args.parameter_summary),
        "material": read_summary(args.material_summary),
        "model": read_summary(args.model_summary),
    }
    result = verify_chain(summaries)
    if args.format == "json":
        print(json.dumps(result, indent=2, sort_keys=True))
    else:
        for key, value in result.items():
            print(f"{key}={value}")
        print("eagl_static_render_chain_control: verified static report joins without guest execution")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
