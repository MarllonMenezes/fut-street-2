"""Join candidate geometry groups to original slot-0 stream records.

The join compares only primitive keys and raw stream metadata.  It preserves
the candidate builder's source-slot choice as candidate evidence and does not
assign position, geometry, UV, material, normal, weight, skinning, rigging,
animation, morph, draw or GPU semantics to any stream.
"""

from __future__ import annotations

import argparse
import csv
from pathlib import Path
from typing import Dict, List, Sequence, Tuple


STATUS = "verified-independent-original-candidate-geometry-stream-control"
ORIGINAL_SEMANTICS = "not-claimed"
PROVENANCE = (
    "independent-primitive-key-join+original-slot-0-topology/candidate-geometry-group-reparse/"
    "symbol+stride+byte-length+element-count+strip-cardinality-controls;no-semantic-promotion"
)

REPORT_FIELDS = (
    "model_archive",
    "model_object",
    "layer",
    "primitive",
    "original_slot",
    "original_symbol_name",
    "candidate_source_slot",
    "candidate_stream_name",
    "original_stream_stride",
    "candidate_stream_stride",
    "original_byte_length",
    "candidate_byte_length",
    "original_element_count",
    "candidate_element_count",
    "candidate_vertex_count",
    "candidate_triangle_count",
    "original_complete_five_slot_fetch_sequence",
    "candidate_primitive_complete",
    "candidate_geometry_finite_and_indexed",
    "symbol_exact",
    "stream_stride_exact",
    "byte_length_exact",
    "element_count_exact",
    "vertex_count_exact",
    "triangle_count_exact",
    "original_input_valid",
    "candidate_input_valid",
    "status",
    "provenance",
    "original_semantics",
    "issue",
)

SUMMARY_FIELDS = (
    "original_source_report",
    "candidate_source_report",
    "original_slot_zero_rows",
    "candidate_rows",
    "joined_rows",
    "missing_candidate_keys",
    "missing_original_keys",
    "candidate_source_slot_zero_rows",
    "candidate_complete_rows",
    "candidate_finite_and_indexed_rows",
    "symbol_exact_rows",
    "stream_stride_exact_rows",
    "byte_length_exact_rows",
    "element_count_exact_rows",
    "vertex_count_exact_rows",
    "triangle_count_exact_rows",
    "original_input_valid_rows",
    "candidate_input_valid_rows",
    "status",
    "provenance",
    "original_semantics",
)


def _read(path: Path) -> List[Dict[str, str]]:
    with path.open("r", encoding="utf-8-sig", newline="") as handle:
        return list(csv.DictReader(handle, delimiter="\t"))


def _write(path: Path, fields: Sequence[str], rows: Sequence[Dict[str, str]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(
            handle, fieldnames=list(fields), delimiter="\t", lineterminator="\n"
        )
        writer.writeheader()
        writer.writerows({field: row.get(field, "") for field in fields} for row in rows)


def _key(row: Dict[str, str]) -> Tuple[str, str, str, str]:
    return (
        row.get("model_archive", ""),
        row.get("model_object", row.get("object_name", "")),
        row.get("layer", ""),
        row.get("primitive", ""),
    )


def _bool(row: Dict[str, str], field: str) -> bool:
    return row.get(field, "").strip().lower() == "true"


def _int(row: Dict[str, str], field: str) -> int | None:
    try:
        return int(row.get(field, ""), 0)
    except (TypeError, ValueError):
        return None


def analyze(
    original_rows: Sequence[Dict[str, str]],
    candidate_rows: Sequence[Dict[str, str]],
) -> Tuple[List[Dict[str, str]], Dict[str, str]]:
    original_by_key = {
        _key(row): row
        for row in original_rows
        if _int(row, "fetch_slot") == 0
    }
    candidate_by_key = {
        _key(row): row
        for row in candidate_rows
        if _int(row, "candidate_source_slot") == 0
    }
    keys = sorted(set(original_by_key) & set(candidate_by_key))
    counters = {
        "candidate_source_slot_zero_rows": 0,
        "candidate_complete_rows": 0,
        "candidate_finite_and_indexed_rows": 0,
        "symbol_exact_rows": 0,
        "stream_stride_exact_rows": 0,
        "byte_length_exact_rows": 0,
        "element_count_exact_rows": 0,
        "vertex_count_exact_rows": 0,
        "triangle_count_exact_rows": 0,
        "original_input_valid_rows": 0,
        "candidate_input_valid_rows": 0,
    }
    report: List[Dict[str, str]] = []
    for key in keys:
        original = original_by_key[key]
        candidate = candidate_by_key[key]
        original_element_count = _int(original, "element_count")
        candidate_element_count = _int(candidate, "candidate_stream_element_count")
        candidate_vertex_count = _int(candidate, "candidate_vertex_count")
        candidate_triangle_count = _int(candidate, "candidate_triangle_count")
        checks = {
            "symbol_exact": candidate.get("candidate_stream_name", "")
            == original.get("symbol_name", ""),
            "stream_stride_exact": _int(candidate, "candidate_stream_stride")
            == _int(original, "stream_stride"),
            "byte_length_exact": _int(candidate, "candidate_stream_byte_length")
            == _int(original, "byte_length"),
            "element_count_exact": candidate_element_count == original_element_count,
            "vertex_count_exact": candidate_vertex_count == original_element_count,
            "triangle_count_exact": candidate_triangle_count == (
                original_element_count - 2 if original_element_count is not None else None
            ),
        }
        for field, value in checks.items():
            counters[f"{field}_rows"] += int(value)
        candidate_source_slot_zero = _int(candidate, "candidate_source_slot") == 0
        candidate_complete = _bool(candidate, "candidate_primitive_complete")
        candidate_finite = _bool(candidate, "candidate_geometry_finite_and_indexed")
        original_valid = (
            original.get("status")
            in {
                "verified-bounded-original-primitive-stream-topology",
                "inconclusive-original-primitive-stream-topology",
            }
            and original.get("original_semantics") == ORIGINAL_SEMANTICS
            and original.get("fetch_slot") == "0"
        )
        candidate_valid = (
            candidate.get("status") == "verified-bounded-candidate-geometry-stream-audit"
            and candidate.get("original_semantics") == ORIGINAL_SEMANTICS
        )
        counters["candidate_source_slot_zero_rows"] += int(candidate_source_slot_zero)
        counters["candidate_complete_rows"] += int(candidate_complete)
        counters["candidate_finite_and_indexed_rows"] += int(candidate_finite)
        counters["original_input_valid_rows"] += int(original_valid)
        counters["candidate_input_valid_rows"] += int(candidate_valid)
        valid = (
            all(checks.values())
            and candidate_source_slot_zero
            and candidate_complete
            and candidate_finite
            and original_valid
            and candidate_valid
        )
        report.append(
            {
                "model_archive": key[0],
                "model_object": key[1],
                "layer": key[2],
                "primitive": key[3],
                "original_slot": original.get("fetch_slot", ""),
                "original_symbol_name": original.get("symbol_name", ""),
                "candidate_source_slot": candidate.get("candidate_source_slot", ""),
                "candidate_stream_name": candidate.get("candidate_stream_name", ""),
                "original_stream_stride": original.get("stream_stride", ""),
                "candidate_stream_stride": candidate.get("candidate_stream_stride", ""),
                "original_byte_length": original.get("byte_length", ""),
                "candidate_byte_length": candidate.get("candidate_stream_byte_length", ""),
                "original_element_count": original.get("element_count", ""),
                "candidate_element_count": candidate.get("candidate_stream_element_count", ""),
                "candidate_vertex_count": candidate.get("candidate_vertex_count", ""),
                "candidate_triangle_count": candidate.get("candidate_triangle_count", ""),
                "original_complete_five_slot_fetch_sequence": original.get(
                    "complete_five_slot_fetch_sequence", ""
                ),
                "candidate_primitive_complete": candidate.get(
                    "candidate_primitive_complete", ""
                ),
                "candidate_geometry_finite_and_indexed": candidate.get(
                    "candidate_geometry_finite_and_indexed", ""
                ),
                **{field: str(value) for field, value in checks.items()},
                "original_input_valid": str(original_valid),
                "candidate_input_valid": str(candidate_valid),
                "status": STATUS if valid else f"inconclusive-{STATUS}",
                "provenance": PROVENANCE,
                "original_semantics": ORIGINAL_SEMANTICS,
                "issue": "" if valid else "one or more structural candidate/original geometry-stream controls failed",
            }
        )

    complete = (
        len(original_by_key) == 2364
        and len(candidate_rows) == 38
        and len(candidate_by_key) == 38
        and len(keys) == 38
        and not (set(candidate_by_key) - set(original_by_key))
        and counters["candidate_source_slot_zero_rows"] == 38
        and counters["candidate_complete_rows"] == 38
        and counters["candidate_finite_and_indexed_rows"] == 38
        and counters["symbol_exact_rows"] == 38
        and counters["stream_stride_exact_rows"] == 38
        and counters["byte_length_exact_rows"] == 38
        and counters["element_count_exact_rows"] == 38
        and counters["vertex_count_exact_rows"] == 38
        and counters["triangle_count_exact_rows"] == 38
        and counters["original_input_valid_rows"] == 38
        and counters["candidate_input_valid_rows"] == 38
        and all(row["status"] == STATUS for row in report)
    )
    summary = {
        "original_source_report": "",
        "candidate_source_report": "",
        "original_slot_zero_rows": str(len(original_by_key)),
        "candidate_rows": str(len(candidate_rows)),
        "joined_rows": str(len(keys)),
        "missing_candidate_keys": str(len(set(original_by_key) - set(candidate_by_key))),
        "missing_original_keys": str(len(set(candidate_by_key) - set(original_by_key))),
        **{field: str(value) for field, value in counters.items()},
        "status": STATUS if complete else f"inconclusive-{STATUS}",
        "provenance": PROVENANCE,
        "original_semantics": ORIGINAL_SEMANTICS,
    }
    return report, summary


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("original_report", type=Path)
    parser.add_argument("candidate_report", type=Path)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()
    report, summary = analyze(_read(args.original_report), _read(args.candidate_report))
    summary["original_source_report"] = str(args.original_report)
    summary["candidate_source_report"] = str(args.candidate_report)
    _write(args.report, REPORT_FIELDS, report)
    _write(args.summary, SUMMARY_FIELDS, [summary])
    print("FIFA Street 2 independent original/candidate geometry-stream control")
    for field in SUMMARY_FIELDS:
        print(f"{field}: {summary[field]}")
    print(f"report: {args.report}")
    print(f"summary: {args.summary}")
    return 0 if summary["status"] == STATUS else 1


if __name__ == "__main__":
    raise SystemExit(main())
