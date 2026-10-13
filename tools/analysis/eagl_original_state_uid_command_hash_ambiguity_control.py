"""Reconcile runtime UIDs with command-stream hashes as a negative control.

This control is intentionally conservative.  It reparses the bounded original
render-chain and candidate-fetch TSVs, scopes them to the candidate object
keys, and asks whether adding the raw command-stream SHA-256 to the
RUNTIME_ALLOC::UID domain produces a unique geometry/state association.  It
does not assign meaning to the UID, command bytes, state, geometry, draw,
material, UV, skinning, rigging, animation, morph, or GPU behavior.
"""

from __future__ import annotations

import argparse
import csv
import re
from collections import defaultdict
from pathlib import Path
from typing import Dict, Iterable, List, Mapping, Sequence, Tuple


STATUS = "verified-independent-original-state-uid-command-hash-ambiguity-control"
SEMANTIC_BINDING = "not-proven"
ORIGINAL_SEMANTICS = "not-claimed"
PROVENANCE = (
    "independent-TSV-object-key-join+opaque-RUNTIME_ALLOC-UID-reparse+raw-command-SHA256-"
    "reparse+UID/command-hash-cardinality+zero/one/multiple-control;"
    "no-state-draw-or-UV-material-promotion"
)
UID_RE = re.compile(r"(?:^|;)RUNTIME_ALLOC::UID=(\d+)(?:;|$)")
KEY_FIELDS = ("model_archive", "model_object", "layer", "primitive")
OBJECT_FIELDS = ("model_archive", "model_object")
REPORT_FIELDS = [
    "model_archive",
    "model_object",
    "layer",
    "geometry_primitive",
    "geometry_uid",
    "geometry_command_bytes_sha256",
    "uid_state_candidate_count",
    "uid_state_candidate_primitives",
    "uid_state_command_hash_candidate_count",
    "uid_state_command_hash_candidate_primitives",
    "uid_state_candidates_unique",
    "uid_and_command_hash_candidates_unique",
    "status",
    "original_semantics",
    "issue",
]
SUMMARY_FIELDS = [
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
    "geometry_rows_with_uid_and_command_hash_candidates",
    "geometry_rows_with_zero_uid_and_command_hash_candidates",
    "geometry_rows_with_one_uid_and_command_hash_candidate",
    "geometry_rows_with_multiple_uid_and_command_hash_candidates",
    "state_rows_in_shared_uid_domains",
    "objects_with_uid_ambiguity",
    "objects_with_uid_and_command_hash_ambiguity",
    "unique_geometry_state_binding_proven_rows",
    "unique_geometry_state_binding_with_command_hash_proven_rows",
    "candidate_input_status_valid_rows",
    "render_input_status_valid_rows",
    "valid_object_rows",
    "status",
    "semantic_binding",
    "provenance",
    "original_semantics",
]


def read_tsv(path: Path) -> List[Dict[str, str]]:
    with path.open("r", encoding="utf-8-sig", newline="") as handle:
        return list(csv.DictReader(handle, delimiter="\t"))


def key(row: Mapping[str, str]) -> Tuple[str, ...]:
    return tuple(row.get(field, "") for field in KEY_FIELDS)


def object_key(row: Mapping[str, str]) -> Tuple[str, ...]:
    return tuple(row.get(field, "") for field in OBJECT_FIELDS)


def parse_uid(row: Mapping[str, str]) -> str:
    match = UID_RE.search(row.get("state_assignments", ""))
    return match.group(1) if match else ""


def truth(row: Mapping[str, str], field: str) -> bool:
    return row.get(field, "").strip().lower() == "true"


def status_ok(row: Mapping[str, str]) -> bool:
    return row.get("original_semantics", "") == ORIGINAL_SEMANTICS and bool(row.get("status"))


def write_tsv(path: Path, fields: Sequence[str], rows: Iterable[Mapping[str, object]]) -> None:
    with path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=list(fields), delimiter="\t", lineterminator="\n")
        writer.writeheader()
        for row in rows:
            writer.writerow({field: row.get(field, "") for field in fields})


def build(
    candidate_path: Path,
    render_path: Path,
    report_path: Path,
    summary_path: Path,
) -> Dict[str, str]:
    candidate_rows = read_tsv(candidate_path)
    render_rows = read_tsv(render_path)
    candidate_by_object: Dict[Tuple[str, ...], List[Dict[str, str]]] = defaultdict(list)
    for row in candidate_rows:
        candidate_by_object[object_key(row)].append(row)
    candidate_objects = set(candidate_by_object)
    render_scoped = [row for row in render_rows if object_key(row) in candidate_objects]
    render_by_object: Dict[Tuple[str, ...], List[Dict[str, str]]] = defaultdict(list)
    for row in render_scoped:
        render_by_object[object_key(row)].append(row)

    joined_objects = sorted(candidate_objects & set(render_by_object))
    missing_candidate_objects = sorted(set(render_by_object) - candidate_objects)
    missing_render_objects = sorted(candidate_objects - set(render_by_object))

    geometry_rows: List[Dict[str, str]] = []
    state_rows: List[Dict[str, str]] = []
    for row in render_scoped:
        is_geometry = truth(row, "has_primitive_type_assignment") and not truth(row, "has_shapename_assignment")
        is_state = truth(row, "has_shapename_assignment") and not truth(row, "has_primitive_type_assignment")
        if is_geometry:
            geometry_rows.append(row)
        elif is_state:
            state_rows.append(row)

    state_by_uid: Dict[Tuple[Tuple[str, ...], str], List[Dict[str, str]]] = defaultdict(list)
    for row in state_rows:
        uid = parse_uid(row)
        if uid:
            state_by_uid[(object_key(row), uid)].append(row)

    geometry_rows_with_valid_uid = 0
    state_rows_with_valid_uid = sum(1 for row in state_rows if parse_uid(row))
    shared_domains = {
        domain for domain, rows in state_by_uid.items() if rows
    }
    for row in geometry_rows:
        if parse_uid(row):
            geometry_rows_with_valid_uid += 1
            shared_domains.add((object_key(row), parse_uid(row)))

    shared_uid_count = sum(
        1
        for domain in set((object_key(row), parse_uid(row)) for row in geometry_rows if parse_uid(row))
        if domain in state_by_uid
    )
    state_rows_in_shared_domains = sum(
        len(rows)
        for domain, rows in state_by_uid.items()
        if domain in {
            (object_key(row), parse_uid(row)) for row in geometry_rows if parse_uid(row)
        }
    )

    report_rows: List[Dict[str, object]] = []
    uid_zero = uid_one = uid_multiple = 0
    hash_zero = hash_one = hash_multiple = 0
    uid_shared = 0
    unique_uid = 0
    unique_hash = 0
    hash_candidates_total = 0
    object_uid_ambiguity: Dict[Tuple[str, ...], bool] = defaultdict(bool)
    object_hash_ambiguity: Dict[Tuple[str, ...], bool] = defaultdict(bool)

    for row in sorted(geometry_rows, key=key):
        uid = parse_uid(row)
        domain_rows = state_by_uid.get((object_key(row), uid), []) if uid else []
        hash_value = row.get("command_bytes_sha256", "").upper()
        hash_rows = [
            state
            for state in domain_rows
            if hash_value
            and state.get("command_bytes_sha256", "").upper() == hash_value
        ]
        uid_count = len(domain_rows)
        hash_count = len(hash_rows)
        if uid_count:
            uid_shared += 1
        if uid_count == 0:
            uid_zero += 1
        elif uid_count == 1:
            uid_one += 1
        else:
            uid_multiple += 1
            object_uid_ambiguity[object_key(row)] = True
        if hash_count == 0:
            hash_zero += 1
        elif hash_count == 1:
            hash_one += 1
        else:
            hash_multiple += 1
            object_hash_ambiguity[object_key(row)] = True
        hash_candidates_total += hash_count
        unique_uid += int(uid_count == 1)
        unique_hash += int(hash_count == 1)
        report_rows.append(
            {
                "model_archive": row.get("model_archive", ""),
                "model_object": row.get("model_object", ""),
                "layer": row.get("layer", ""),
                "geometry_primitive": row.get("primitive", ""),
                "geometry_uid": uid,
                "geometry_command_bytes_sha256": row.get("command_bytes_sha256", ""),
                "uid_state_candidate_count": uid_count,
                "uid_state_candidate_primitives": ",".join(state.get("primitive", "") for state in domain_rows),
                "uid_state_command_hash_candidate_count": hash_count,
                "uid_state_command_hash_candidate_primitives": ",".join(state.get("primitive", "") for state in hash_rows),
                "uid_state_candidates_unique": uid_count == 1,
                "uid_and_command_hash_candidates_unique": hash_count == 1,
                "status": STATUS,
                "original_semantics": ORIGINAL_SEMANTICS,
                "issue": "" if uid_count > 1 and hash_count == 0 else "command hash did not create the expected negative discriminator",
            }
        )

    candidate_valid = sum(1 for row in candidate_rows if status_ok(row))
    render_valid = sum(1 for row in render_scoped if status_ok(row))
    valid_objects = sum(
        1
        for obj in joined_objects
        if candidate_by_object[obj] and render_by_object[obj]
    )
    valid = (
        len(candidate_objects) == 24
        and len(render_scoped) == 218
        and len(joined_objects) == 24
        and not missing_candidate_objects
        and not missing_render_objects
        and len(geometry_rows) == 38
        and len(state_rows) == 180
        and geometry_rows_with_valid_uid == 38
        and state_rows_with_valid_uid == 180
        and shared_uid_count == 24
        and uid_shared == 38
        and uid_zero == 0
        and uid_one == 0
        and uid_multiple == 38
        and hash_candidates_total == 0
        and hash_zero == 38
        and hash_one == 0
        and hash_multiple == 0
        and unique_uid == 0
        and unique_hash == 0
        and candidate_valid == 218
        and render_valid == 218
        and valid_objects == 24
    )
    write_tsv(report_path, REPORT_FIELDS, report_rows)
    summary = {
        "candidate_source_report": str(candidate_path),
        "render_source_report": str(render_path),
        "candidate_object_rows": len(candidate_objects),
        "render_scoped_rows": len(render_scoped),
        "joined_object_rows": len(joined_objects),
        "missing_candidate_objects": len(missing_candidate_objects),
        "missing_render_objects": len(missing_render_objects),
        "geometry_rows": len(geometry_rows),
        "state_rows": len(state_rows),
        "geometry_rows_with_valid_uid": geometry_rows_with_valid_uid,
        "state_rows_with_valid_uid": state_rows_with_valid_uid,
        "shared_uid_count": shared_uid_count,
        "geometry_rows_same_uid_state_candidates": uid_shared,
        "geometry_rows_zero_same_uid_state_candidates": uid_zero,
        "geometry_rows_one_same_uid_state_candidate": uid_one,
        "geometry_rows_multiple_same_uid_state_candidates": uid_multiple,
        "geometry_rows_with_uid_and_command_hash_candidates": hash_candidates_total,
        "geometry_rows_with_zero_uid_and_command_hash_candidates": hash_zero,
        "geometry_rows_with_one_uid_and_command_hash_candidate": hash_one,
        "geometry_rows_with_multiple_uid_and_command_hash_candidates": hash_multiple,
        "state_rows_in_shared_uid_domains": state_rows_in_shared_domains,
        "objects_with_uid_ambiguity": sum(object_uid_ambiguity.values()),
        "objects_with_uid_and_command_hash_ambiguity": sum(object_hash_ambiguity.values()),
        "unique_geometry_state_binding_proven_rows": unique_uid,
        "unique_geometry_state_binding_with_command_hash_proven_rows": unique_hash,
        "candidate_input_status_valid_rows": candidate_valid,
        "render_input_status_valid_rows": render_valid,
        "valid_object_rows": valid_objects,
        "status": STATUS if valid else "inconclusive-" + STATUS,
        "semantic_binding": SEMANTIC_BINDING,
        "provenance": PROVENANCE,
        "original_semantics": ORIGINAL_SEMANTICS,
    }
    write_tsv(summary_path, SUMMARY_FIELDS, [summary])
    return {field: str(summary[field]) for field in SUMMARY_FIELDS}


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("candidate_fetch", type=Path)
    parser.add_argument("render_chain", type=Path)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()
    summary = build(args.candidate_fetch, args.render_chain, args.report, args.summary)
    print("FIFA Street 2 independent state UID/command hash ambiguity control")
    for field in SUMMARY_FIELDS:
        print(f"{field}: {summary[field]}")
    return 0 if summary["status"] == STATUS else 1


if __name__ == "__main__":
    raise SystemExit(main())
