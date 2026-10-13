"""Independently reparse command-chain and __VB topology TSV reports.

The control uses only section-relative command-stream and descriptor offsets.
The observed 0x0C adjacency is preserved as raw structure; it is not assigned
to a field, vertex attribute, draw operation, material, UV, skinning, rigging,
animation, morph or GPU semantic.
"""

from __future__ import annotations

import argparse
import csv
from collections import defaultdict
from pathlib import Path
from typing import Dict, Iterable, List, Sequence, Tuple


STATUS = "verified-independent-original-primitive-stream-command-descriptor-adjacency-control"
INPUT_STATUS = "verified-bounded-original-primitive-stream-command-descriptor-adjacency"
RENDER_STATUS = "verified-bounded-original-render-chain-structure"
TOPOLOGY_STATUS = "verified-bounded-original-primitive-stream-topology"
ORIGINAL_SEMANTICS = "not-claimed"
COMMAND_DESCRIPTOR_DELTA = 0x0C
PROVENANCE = (
    "independent-TSV-reparse+same-section-command-stream/descriptor-offset-join;"
    "exact-observed-byte-adjacency;shifted-ordinal-negative-control;no-semantic-promotion"
)

REPORT_FIELDS = (
    "model_archive",
    "model_object",
    "layer",
    "primitive",
    "command_section_index",
    "command_stream_offset",
    "candidate_ordinal_count",
    "identity_candidate_count",
    "identity_candidate_ordinals",
    "identity_candidate_categories",
    "exact_adjacency_stream_count",
    "exact_adjacency_stream_categories",
    "exact_adjacency_stream_strides",
    "current_ordinal_present",
    "current_ordinal_identity_match",
    "current_ordinal_uniquely_identified",
    "shifted_ordinal_identity_match",
    "render_input_status",
    "topology_input_status",
    "status",
    "original_semantics",
    "issue",
)

SUMMARY_FIELDS = (
    "render_source_report",
    "topology_source_report",
    "command_descriptor_delta",
    "render_rows",
    "topology_rows",
    "primitive_rows",
    "complete_command_rows",
    "candidate_ordinal_rows",
    "identity_candidate_rows",
    "current_ordinal_rows",
    "current_ordinal_identity_rows",
    "unique_current_ordinal_identity_rows",
    "shifted_ordinal_identity_rows",
    "same_section_identity_rows",
    "exact_adjacency_stream_rows",
    "exact_adjacency_category_histogram",
    "exact_adjacency_stride_histogram",
    "render_input_status_valid_rows",
    "topology_input_status_valid_rows",
    "status",
    "provenance",
    "original_semantics",
)


def _int(row: Dict[str, str], field: str) -> int:
    value = row.get(field, "")
    try:
        return int(value, 0)
    except (TypeError, ValueError) as exc:
        raise ValueError(f"invalid integer in {field}: {value!r}") from exc


def _bool(row: Dict[str, str], field: str) -> bool:
    value = row.get(field, "")
    if value not in {"True", "False"}:
        raise ValueError(f"invalid boolean in {field}: {value!r}")
    return value == "True"


def _primitive_key(row: Dict[str, str]) -> Tuple[str, str, int, int]:
    archive = row.get("model_archive", "")
    object_name = row.get("model_object", "")
    if not archive or not object_name:
        raise ValueError("primitive row has empty object identity")
    return archive, object_name, _int(row, "layer"), _int(row, "primitive")


def _object_key(row: Dict[str, str]) -> Tuple[str, str, int]:
    archive = row.get("model_archive", "")
    object_name = row.get("model_object", "")
    if not archive or not object_name:
        raise ValueError("object row has empty object identity")
    return archive, object_name, _int(row, "layer")


def _ordered(rows: Iterable[Dict[str, str]]) -> List[Dict[str, str]]:
    return sorted(rows, key=lambda row: (_int(row, "category"), row.get("symbol_name", "")))


def _read(path: Path) -> List[Dict[str, str]]:
    with path.open("r", encoding="utf-8-sig", newline="") as handle:
        return list(csv.DictReader(handle, delimiter="\t"))


def _write(path: Path, fields: Sequence[str], rows: Sequence[Dict[str, str]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=list(fields), delimiter="\t", lineterminator="\n")
        writer.writeheader()
        writer.writerows({field: row.get(field, "") for field in fields} for row in rows)


def analyze(
    render_rows: Sequence[Dict[str, str]],
    topology_rows: Sequence[Dict[str, str]],
) -> Tuple[List[Dict[str, str]], Dict[str, str]]:
    if not render_rows or not topology_rows:
        raise ValueError("render-chain and topology reports must both contain rows")

    render_required = {
        "model_archive",
        "model_object",
        "layer",
        "primitive",
        "command_section_index",
        "command_stream_offset",
        "command_stream_complete",
        "status",
        "original_semantics",
    }
    topology_required = {
        "model_archive",
        "model_object",
        "layer",
        "primitive",
        "section_index",
        "descriptor_offset",
        "category",
        "ordinal",
        "stream_stride",
        "status",
        "original_semantics",
    }
    missing_render = render_required - set(render_rows[0])
    missing_topology = topology_required - set(topology_rows[0])
    if missing_render:
        raise ValueError(f"render-chain report is missing columns: {sorted(missing_render)}")
    if missing_topology:
        raise ValueError(f"topology report is missing columns: {sorted(missing_topology)}")
    if any(row.get("original_semantics") != ORIGINAL_SEMANTICS for row in render_rows):
        raise ValueError("render-chain input changed original_semantics")
    if any(row.get("original_semantics") != ORIGINAL_SEMANTICS for row in topology_rows):
        raise ValueError("topology input changed original_semantics")

    topology_by_object: Dict[
        Tuple[str, str, int], Dict[int, List[Dict[str, str]]]
    ] = defaultdict(lambda: defaultdict(list))
    for row in topology_rows:
        topology_by_object[_object_key(row)][_int(row, "ordinal")].append(row)

    report: List[Dict[str, str]] = []
    current_rows = 0
    identity_rows = 0
    unique_rows = 0
    shifted_rows = 0
    same_section_rows = 0
    complete_command_rows = 0
    exact_adjacency_stream_rows = 0
    exact_adjacency_category_histogram: Dict[int, int] = defaultdict(int)
    exact_adjacency_stride_histogram: Dict[int, int] = defaultdict(int)

    for render in sorted(render_rows, key=_primitive_key):
        key = _primitive_key(render)
        command_section = _int(render, "command_section_index")
        command_offset = _int(render, "command_stream_offset")
        complete_command = _bool(render, "command_stream_complete") and command_offset > 0
        complete_command_rows += int(complete_command)
        candidates: List[Tuple[int, List[Dict[str, str]]]] = []
        object_key = _object_key(render)
        object_candidates = topology_by_object.get(object_key, {})
        for ordinal, rows in sorted(object_candidates.items()):
            ordered = _ordered(rows)
            categories = [_int(row, "category") for row in ordered]
            same_section = all(_int(row, "section_index") == command_section for row in ordered)
            category_sequence = bool(ordered) and len(ordered) == 5 and all(
                category == categories[0] + index
                for index, category in enumerate(categories)
            )
            adjacency = any(
                command_offset >= _int(row, "descriptor_offset")
                and command_offset - _int(row, "descriptor_offset") == COMMAND_DESCRIPTOR_DELTA
                for row in ordered
            )
            if same_section and category_sequence and adjacency:
                candidates.append((ordinal, ordered))
        current_ordinal = _int(render, "primitive")
        current_present = current_ordinal in object_candidates
        current_match = any(ordinal == current_ordinal for ordinal, _ in candidates)
        shifted_match = any(ordinal == current_ordinal + 1 for ordinal, _ in candidates)
        exact_matches = [
            (ordinal, row)
            for ordinal, rows in candidates
            for row in rows
            if command_offset >= _int(row, "descriptor_offset")
            and command_offset - _int(row, "descriptor_offset") == COMMAND_DESCRIPTOR_DELTA
        ]
        exact_adjacency_stream_rows += len(exact_matches)
        for _, row in exact_matches:
            exact_adjacency_category_histogram[_int(row, "category")] += 1
            exact_adjacency_stride_histogram[_int(row, "stream_stride")] += 1
        if current_present:
            current_rows += 1
        if current_match:
            identity_rows += 1
            same_section_rows += 1
        if current_match and len(candidates) == 1:
            unique_rows += 1
        if shifted_match:
            shifted_rows += 1

        candidate_categories = "|".join(
            f"{ordinal}:{','.join(str(_int(row, 'category')) for row in rows)}"
            for ordinal, rows in candidates
        )
        exact_categories = ",".join(
            f"{ordinal}:{_int(row, 'category')}" for ordinal, row in exact_matches
        )
        exact_strides = ",".join(
            f"{ordinal}:{_int(row, 'stream_stride')}" for ordinal, row in exact_matches
        )
        row_status = INPUT_STATUS if complete_command and current_match else (
            "inconclusive-original-primitive-stream-command-descriptor-adjacency"
        )
        report.append(
            {
                "model_archive": key[0],
                "model_object": key[1],
                "layer": str(key[2]),
                "primitive": str(key[3]),
                "command_section_index": str(command_section),
                "command_stream_offset": f"0x{command_offset:X}",
                "candidate_ordinal_count": str(len(object_candidates)),
                "identity_candidate_count": str(len(candidates)),
                "identity_candidate_ordinals": ",".join(str(ordinal) for ordinal, _ in candidates),
                "identity_candidate_categories": candidate_categories,
                "exact_adjacency_stream_count": str(len(exact_matches)),
                "exact_adjacency_stream_categories": exact_categories,
                "exact_adjacency_stream_strides": exact_strides,
                "current_ordinal_present": str(current_present),
                "current_ordinal_identity_match": str(current_match),
                "current_ordinal_uniquely_identified": str(current_match and len(candidates) == 1),
                "shifted_ordinal_identity_match": str(shifted_match),
                "render_input_status": render.get("status", ""),
                "topology_input_status": ";".join(sorted({row.get("status", "") for row in object_candidates.get(current_ordinal, [])})),
                "status": row_status,
                "original_semantics": ORIGINAL_SEMANTICS,
                "issue": "" if row_status == INPUT_STATUS else "command stream or exact same-section descriptor adjacency was absent",
            }
        )

    summary = {
        "render_source_report": "",
        "topology_source_report": "",
        "command_descriptor_delta": str(COMMAND_DESCRIPTOR_DELTA),
        "render_rows": str(len(render_rows)),
        "topology_rows": str(len(topology_rows)),
        "primitive_rows": str(len(report)),
        "complete_command_rows": str(complete_command_rows),
        "candidate_ordinal_rows": str(sum(int(row["candidate_ordinal_count"]) for row in report)),
        "identity_candidate_rows": str(sum(int(row["identity_candidate_count"]) for row in report)),
        "current_ordinal_rows": str(current_rows),
        "current_ordinal_identity_rows": str(identity_rows),
        "unique_current_ordinal_identity_rows": str(unique_rows),
        "shifted_ordinal_identity_rows": str(shifted_rows),
        "same_section_identity_rows": str(same_section_rows),
        "exact_adjacency_stream_rows": str(exact_adjacency_stream_rows),
        "exact_adjacency_category_histogram": ",".join(
            f"{category}:{count}"
            for category, count in sorted(exact_adjacency_category_histogram.items())
        ),
        "exact_adjacency_stride_histogram": ",".join(
            f"{stride}:{count}"
            for stride, count in sorted(exact_adjacency_stride_histogram.items())
        ),
        "render_input_status_valid_rows": str(sum(row.get("status") == RENDER_STATUS for row in render_rows)),
        "topology_input_status_valid_rows": str(sum(row.get("status") == TOPOLOGY_STATUS for row in topology_rows)),
        "status": STATUS if (
            report
            and complete_command_rows == len(report)
            and current_rows == len(report)
            and identity_rows == len(report)
            and unique_rows == len(report)
            and shifted_rows == 0
            and all(row.get("original_semantics") == ORIGINAL_SEMANTICS for row in report)
        ) else "inconclusive-independent-original-primitive-stream-command-descriptor-adjacency-control",
        "provenance": PROVENANCE,
        "original_semantics": ORIGINAL_SEMANTICS,
    }
    return report, summary


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("render_report", type=Path)
    parser.add_argument("topology_report", type=Path)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()
    report, summary = analyze(_read(args.render_report), _read(args.topology_report))
    summary["render_source_report"] = str(args.render_report)
    summary["topology_source_report"] = str(args.topology_report)
    _write(args.report, REPORT_FIELDS, report)
    _write(args.summary, SUMMARY_FIELDS, [summary])
    print("FIFA Street 2 independent original primitive/__VB command-descriptor adjacency control")
    for field in SUMMARY_FIELDS:
        print(f"{field}: {summary[field]}")
    print(f"report: {args.report}")
    print(f"summary: {args.summary}")
    return 0 if summary["status"] == STATUS else 1


if __name__ == "__main__":
    raise SystemExit(main())
