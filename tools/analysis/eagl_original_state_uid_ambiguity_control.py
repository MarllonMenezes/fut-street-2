"""Independently measure RUNTIME_ALLOC UID state/geometry ambiguity.

The UID is retained as an opaque token.  This control joins the 24 candidate
objects to the original render-chain report, extracts only the repeated
``RUNTIME_ALLOC::UID`` token, and measures how many SHAPENAME state rows share
each geometry row's UID.  Shared-token cardinality is not promoted to a
state-to-draw, geometry, UV, material, skinning, rigging, animation, morph, or
GPU binding.
"""

from __future__ import annotations

import argparse
import csv
import re
from collections import defaultdict
from pathlib import Path
from typing import Dict, Iterable, List, Sequence, Tuple


STATUS = "verified-independent-original-state-runtime-uid-ambiguity-control"
SEMANTIC_BINDING = "not-proven"
ORIGINAL_SEMANTICS = "not-claimed"
UID_RE = re.compile(r"(?:^|;)RUNTIME_ALLOC::UID=(\d+)(?:;|$)")
PROVENANCE = (
    "independent-TSV-object-key-join+opaque-RUNTIME_ALLOC-UID-reparse+"
    "geometry-to-state-candidate-cardinality+zero/one/multiple-control;"
    "no-state-draw-or-UV-material-promotion"
)

REPORT_FIELDS = (
    "model_archive",
    "model_object",
    "geometry_rows",
    "state_rows",
    "geometry_rows_with_valid_uid",
    "state_rows_with_valid_uid",
    "shared_uid_count",
    "geometry_rows_same_uid_state_candidates",
    "geometry_rows_zero_same_uid_state_candidates",
    "geometry_rows_one_same_uid_state_candidate",
    "geometry_rows_multiple_same_uid_state_candidates",
    "state_rows_in_shared_uid_domains",
    "max_state_candidates_per_geometry",
    "unique_geometry_state_binding_proven",
    "status",
    "semantic_binding",
    "original_semantics",
    "issue",
)

SUMMARY_FIELDS = (
    "candidate_source_report",
    "render_source_report",
    "candidate_object_rows",
    "render_scoped_rows",
    "joined_object_rows",
    "missing_candidate_objects",
    "missing_render_objects",
    "geometry_rows",
    "state_rows",
    "geometry_rows_with_valid_uid",
    "state_rows_with_valid_uid",
    "shared_uid_count",
    "geometry_rows_same_uid_state_candidates",
    "geometry_rows_zero_same_uid_state_candidates",
    "geometry_rows_one_same_uid_state_candidate",
    "geometry_rows_multiple_same_uid_state_candidates",
    "state_rows_in_shared_uid_domains",
    "objects_with_uid_ambiguity",
    "unique_geometry_state_binding_proven_rows",
    "candidate_input_status_valid_rows",
    "render_input_status_valid_rows",
    "valid_object_rows",
    "status",
    "semantic_binding",
    "provenance",
    "original_semantics",
)


def _read(path: Path) -> List[Dict[str, str]]:
    with path.open("r", encoding="utf-8-sig", newline="") as handle:
        return list(csv.DictReader(handle, delimiter="\t"))


def _write(path: Path, fields: Sequence[str], rows: Iterable[Dict[str, str]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(
            handle,
            fieldnames=list(fields),
            delimiter="\t",
            lineterminator="\n",
        )
        writer.writeheader()
        writer.writerows({field: row.get(field, "") for field in fields} for row in rows)


def _key(row: Dict[str, str]) -> Tuple[str, str]:
    key = (row.get("model_archive", ""), row.get("model_object", ""))
    if not all(key):
        raise ValueError(f"empty object identity: {key!r}")
    return key


def _uid(row: Dict[str, str]) -> str:
    match = UID_RE.search(row.get("state_assignments", ""))
    return match.group(1) if match else ""


def _valid_render(row: Dict[str, str]) -> bool:
    return (
        row.get("status") == "verified-bounded-original-render-chain-structure"
        and row.get("original_semantics") == ORIGINAL_SEMANTICS
    )


def _valid_candidate(row: Dict[str, str]) -> bool:
    return row.get("original_semantics") == ORIGINAL_SEMANTICS and row.get("status") in {
        "verified-bounded-candidate-fetch-record-association",
        "opaque-incomplete-fetch-record-association",
    }


def analyze(
    candidate_rows: Sequence[Dict[str, str]],
    render_rows: Sequence[Dict[str, str]],
) -> Tuple[List[Dict[str, str]], Dict[str, str]]:
    if not candidate_rows or not render_rows:
        raise ValueError("candidate and render reports must both contain rows")

    candidate_objects = {_key(row) for row in candidate_rows}
    candidate_by_object: Dict[Tuple[str, str], List[Dict[str, str]]] = defaultdict(list)
    render_by_object: Dict[Tuple[str, str], List[Dict[str, str]]] = defaultdict(list)
    for row in candidate_rows:
        candidate_by_object[_key(row)].append(row)
    for row in render_rows:
        key = _key(row)
        if key in candidate_objects:
            render_by_object[key].append(row)

    keys = sorted(candidate_objects | set(render_by_object))
    report: List[Dict[str, str]] = []
    counters = {
        "geometry_rows": 0,
        "state_rows": 0,
        "geometry_rows_with_valid_uid": 0,
        "state_rows_with_valid_uid": 0,
        "shared_uid_count": 0,
        "geometry_rows_same_uid_state_candidates": 0,
        "geometry_rows_zero_same_uid_state_candidates": 0,
        "geometry_rows_one_same_uid_state_candidate": 0,
        "geometry_rows_multiple_same_uid_state_candidates": 0,
        "state_rows_in_shared_uid_domains": 0,
        "objects_with_uid_ambiguity": 0,
        "unique_geometry_state_binding_proven_rows": 0,
        "candidate_input_status_valid_rows": 0,
        "render_input_status_valid_rows": 0,
        "valid_object_rows": 0,
    }

    for key in keys:
        candidate = candidate_by_object.get(key, [])
        render = render_by_object.get(key, [])
        geometry = [
            row for row in render if row.get("has_primitive_type_assignment") == "True"
        ]
        state = [
            row for row in render if row.get("has_shapename_assignment") == "True"
        ]
        states_by_uid: Dict[str, List[Dict[str, str]]] = defaultdict(list)
        geometries_by_uid: Dict[str, List[Dict[str, str]]] = defaultdict(list)
        for row in state:
            token = _uid(row)
            if token:
                states_by_uid[token].append(row)
        for row in geometry:
            token = _uid(row)
            if token:
                geometries_by_uid[token].append(row)

        candidates_per_geometry = []
        same_uid_geometry_rows = 0
        zero_rows = 0
        one_rows = 0
        multiple_rows = 0
        unique_proven = 0
        for row in geometry:
            token = _uid(row)
            count = len(states_by_uid.get(token, [])) if token else 0
            candidates_per_geometry.append(count)
            if count > 0:
                same_uid_geometry_rows += 1
            if count == 0:
                zero_rows += 1
            elif count == 1:
                one_rows += 1
            else:
                multiple_rows += 1
            # A UID match is only unique if the token selects one state and one
            # geometry in this object.  This is intentionally a negative test.
            unique_proven += int(count == 1 and token and len(geometries_by_uid[token]) == 1)

        shared_uids = set(states_by_uid) & set(geometries_by_uid)
        shared_state_rows = sum(len(states_by_uid[token]) for token in shared_uids)
        uid_ambiguity = bool(multiple_rows or zero_rows or unique_proven == 0 and geometry)
        input_candidate_valid = bool(candidate) and all(_valid_candidate(row) for row in candidate)
        input_render_valid = bool(render) and all(_valid_render(row) for row in render)
        valid = (
            key in candidate_objects
            and bool(render)
            and input_candidate_valid
            and input_render_valid
            and len(geometry) > 0
            and len(state) > 0
            and len(geometry) == len([row for row in render if row.get("has_primitive_type_assignment") == "True"])
            and all(count > 1 for count in candidates_per_geometry)
            and unique_proven == 0
            and bool(shared_uids)
        )

        counters["geometry_rows"] += len(geometry)
        counters["state_rows"] += len(state)
        counters["geometry_rows_with_valid_uid"] += sum(bool(_uid(row)) for row in geometry)
        counters["state_rows_with_valid_uid"] += sum(bool(_uid(row)) for row in state)
        counters["shared_uid_count"] += len(shared_uids)
        counters["geometry_rows_same_uid_state_candidates"] += same_uid_geometry_rows
        counters["geometry_rows_zero_same_uid_state_candidates"] += zero_rows
        counters["geometry_rows_one_same_uid_state_candidate"] += one_rows
        counters["geometry_rows_multiple_same_uid_state_candidates"] += multiple_rows
        counters["state_rows_in_shared_uid_domains"] += shared_state_rows
        counters["objects_with_uid_ambiguity"] += int(uid_ambiguity)
        counters["unique_geometry_state_binding_proven_rows"] += unique_proven
        counters["candidate_input_status_valid_rows"] += int(input_candidate_valid)
        counters["render_input_status_valid_rows"] += int(input_render_valid)
        counters["valid_object_rows"] += int(valid)
        report.append(
            {
                "model_archive": key[0],
                "model_object": key[1],
                "geometry_rows": str(len(geometry)),
                "state_rows": str(len(state)),
                "geometry_rows_with_valid_uid": str(sum(bool(_uid(row)) for row in geometry)),
                "state_rows_with_valid_uid": str(sum(bool(_uid(row)) for row in state)),
                "shared_uid_count": str(len(shared_uids)),
                "geometry_rows_same_uid_state_candidates": str(same_uid_geometry_rows),
                "geometry_rows_zero_same_uid_state_candidates": str(zero_rows),
                "geometry_rows_one_same_uid_state_candidate": str(one_rows),
                "geometry_rows_multiple_same_uid_state_candidates": str(multiple_rows),
                "state_rows_in_shared_uid_domains": str(shared_state_rows),
                "max_state_candidates_per_geometry": str(max(candidates_per_geometry, default=0)),
                "unique_geometry_state_binding_proven": str(unique_proven),
                "status": STATUS if valid else "inconclusive-" + STATUS,
                "semantic_binding": SEMANTIC_BINDING,
                "original_semantics": ORIGINAL_SEMANTICS,
                "issue": "" if valid else "UID cardinality or source reconciliation did not preserve the expected ambiguity",
            }
        )

    missing_candidate = set(render_by_object) - candidate_objects
    missing_render = candidate_objects - set(render_by_object)
    summary = {
        "candidate_source_report": "",
        "render_source_report": "",
        "candidate_object_rows": str(len(candidate_objects)),
        "render_scoped_rows": str(sum(len(rows) for rows in render_by_object.values())),
        "joined_object_rows": str(len(set(candidate_objects) & set(render_by_object))),
        "missing_candidate_objects": str(len(missing_candidate)),
        "missing_render_objects": str(len(missing_render)),
        **{key: str(value) for key, value in counters.items()},
        "status": STATUS if (
            len(candidate_objects) == 24
            and len(render_by_object) == 24
            and not missing_candidate
            and not missing_render
            and counters["geometry_rows"] == 38
            and counters["state_rows"] == 180
            and counters["geometry_rows_with_valid_uid"] == 38
            and counters["state_rows_with_valid_uid"] == 180
            and counters["shared_uid_count"] == 24
            and counters["geometry_rows_same_uid_state_candidates"] == 38
            and counters["geometry_rows_zero_same_uid_state_candidates"] == 0
            and counters["geometry_rows_one_same_uid_state_candidate"] == 0
            and counters["geometry_rows_multiple_same_uid_state_candidates"] == 38
            and counters["state_rows_in_shared_uid_domains"] == 180
            and counters["objects_with_uid_ambiguity"] == 24
            and counters["unique_geometry_state_binding_proven_rows"] == 0
            and counters["candidate_input_status_valid_rows"] == 24
            and counters["render_input_status_valid_rows"] == 24
            and counters["valid_object_rows"] == 24
            and all(row["status"] == STATUS for row in report)
        ) else "inconclusive-" + STATUS,
        "semantic_binding": SEMANTIC_BINDING,
        "provenance": PROVENANCE,
        "original_semantics": ORIGINAL_SEMANTICS,
    }
    return report, summary


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("candidate_report", type=Path)
    parser.add_argument("render_report", type=Path)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()
    report, summary = analyze(_read(args.candidate_report), _read(args.render_report))
    summary["candidate_source_report"] = str(args.candidate_report)
    summary["render_source_report"] = str(args.render_report)
    _write(args.report, REPORT_FIELDS, report)
    _write(args.summary, SUMMARY_FIELDS, [summary])
    print("FIFA Street 2 independent RUNTIME_ALLOC UID state/geometry ambiguity control")
    for field in SUMMARY_FIELDS:
        print(f"{field}: {summary[field]}")
    print(f"report: {args.report}")
    print(f"summary: {args.summary}")
    return 0 if summary["status"] == STATUS else 1


if __name__ == "__main__":
    raise SystemExit(main())
