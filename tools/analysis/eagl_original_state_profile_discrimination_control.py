"""Reparse state-profile signatures to measure state-to-geometry discrimination.

The bounded state/geometry profile report contains one row for every geometry
candidate and state primitive pair.  This control deduplicates the state
primitive identity, recomputes the profile tie count, and measures whether a
state command signature narrows the geometry candidate set.  It is deliberately
negative: a state primitive is only considered discriminative when its
observed profile selects exactly one geometry among multiple candidates and
the command shape is exact.  A single-geometry object is not evidence of a
state-to-geometry binding because there is no competing candidate.
"""

from __future__ import annotations

import argparse
import csv
from pathlib import Path
from typing import Dict, Iterable, List, Sequence, Tuple


STATUS = "verified-independent-original-state-profile-discrimination-control"
SEMANTIC_BINDING = "not-proven"
ORIGINAL_SEMANTICS = "not-claimed"
PROFILE_STATUS = "opaque-profile-control"
ADJACENCY_STATUS = "verified-control"
PROVENANCE = (
    "independent-TSV-reparse+state-identity-deduplication+profile-signature-"
    "tie-recompute+multi-geometry-candidate-negative-control;no-state-draw-"
    "or-UV-material-promotion"
)

REPORT_FIELDS = (
    "model_archive",
    "object_name",
    "geometry_candidate_count",
    "state_primitive_count",
    "profile_rows",
    "distinct_state_profile_signatures",
    "profile_tie_count_match_rows",
    "state_primitives_with_multiple_geometry_candidates",
    "state_primitives_with_single_geometry_candidate",
    "state_primitives_with_zero_geometry_candidates",
    "state_signature_groups_with_multiple_geometry_candidates",
    "exact_command_shape_rows",
    "unique_state_to_geometry_binding_proven_rows",
    "status",
    "semantic_binding",
    "original_semantics",
    "issue",
)

SUMMARY_FIELDS = (
    "profile_source_report",
    "adjacency_source_report",
    "profile_rows",
    "profile_objects",
    "geometry_candidate_rows",
    "state_primitive_rows",
    "distinct_object_state_profile_signatures",
    "profile_tie_count_match_rows",
    "state_primitives_with_multiple_geometry_candidates",
    "state_primitives_with_single_geometry_candidate",
    "state_primitives_with_zero_geometry_candidates",
    "state_signature_groups_with_multiple_geometry_candidates",
    "exact_command_shape_rows",
    "unique_state_to_geometry_binding_proven_rows",
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


def _int(row: Dict[str, str], field: str) -> int:
    try:
        return int(row.get(field, ""), 0)
    except (TypeError, ValueError) as exc:
        raise ValueError(f"invalid integer in {field}: {row.get(field)!r}") from exc


def _bool(row: Dict[str, str], field: str) -> bool:
    value = row.get(field, "")
    if value not in {"True", "False"}:
        raise ValueError(f"invalid boolean in {field}: {value!r}")
    return value == "True"


def _object_key(row: Dict[str, str]) -> Tuple[str, str]:
    key = (row.get("model_archive", ""), row.get("object_name", ""))
    if not all(key):
        raise ValueError(f"empty object identity: {key!r}")
    return key


def _state_key(row: Dict[str, str]) -> Tuple[str, str, str, str]:
    return (
        row["model_archive"],
        row["object_name"],
        row["state_layer"],
        row["state_primitive"],
    )


def analyze(
    profile_rows: Sequence[Dict[str, str]],
    adjacency_rows: Sequence[Dict[str, str]],
) -> Tuple[List[Dict[str, str]], Dict[str, str]]:
    if not profile_rows or not adjacency_rows:
        raise ValueError("profile and adjacency reports must contain rows")

    profiles_by_object: Dict[Tuple[str, str], List[Dict[str, str]]] = {}
    adjacency_by_object: Dict[Tuple[str, str], Dict[str, str]] = {}
    for row in profile_rows:
        profiles_by_object.setdefault(_object_key(row), []).append(row)
    for row in adjacency_rows:
        key = _object_key(row)
        if key in adjacency_by_object:
            raise ValueError(f"duplicate adjacency object: {key!r}")
        adjacency_by_object[key] = row

    keys = sorted(set(profiles_by_object) & set(adjacency_by_object))
    report: List[Dict[str, str]] = []
    counters = {
        "profile_tie_count_match": 0,
        "state_primitives": 0,
        "ambiguous_state_primitives": 0,
        "single_geometry_state_primitives": 0,
        "zero_geometry_state_primitives": 0,
        "multi_geometry_signature_groups": 0,
        "exact_command_shape": 0,
        "unique_binding": 0,
        "valid_object": 0,
    }

    for key in keys:
        rows = profiles_by_object[key]
        adjacency = adjacency_by_object[key]
        geometry_ids = {row["geometry_primitive"] for row in rows}
        state_to_geometries: Dict[Tuple[str, str, str, str], set[str]] = {}
        state_to_signature: Dict[Tuple[str, str, str, str], str] = {}
        signature_to_states: Dict[str, set[Tuple[str, str, str, str]]] = {}
        signature_to_geometries: Dict[str, set[str]] = {}
        tie_matches = 0
        exact_rows = 0

        for row in rows:
            if row.get("status") != PROFILE_STATUS:
                raise ValueError(f"invalid profile status for {key!r}")
            state_key = _state_key(row)
            signature = row.get("state_command_shape_sha256", "")
            if not signature:
                raise ValueError(f"empty state profile signature for {state_key!r}")
            state_to_geometries.setdefault(state_key, set()).add(row["geometry_primitive"])
            prior_signature = state_to_signature.setdefault(state_key, signature)
            if prior_signature != signature:
                raise ValueError(f"state signature changed across geometry rows: {state_key!r}")
            signature_to_states.setdefault(signature, set()).add(state_key)
            signature_to_geometries.setdefault(signature, set()).add(row["geometry_primitive"])

            expected_tie_count = len(signature_to_states[signature])
            tie_matches += int(_int(row, "state_profile_tie_count") == expected_tie_count)
            exact_rows += int(_bool(row, "exact_command_shape"))

        # Re-evaluate tie counts after all rows are loaded; this prevents a
        # row-order-dependent result when a signature first appears late.
        tie_matches = sum(
            _int(row, "state_profile_tie_count")
            == len(signature_to_states[row["state_command_shape_sha256"]])
            for row in rows
        )
        ambiguous = sum(len(geometries) > 1 for geometries in state_to_geometries.values())
        single = sum(len(geometries) == 1 for geometries in state_to_geometries.values())
        zero = sum(len(geometries) == 0 for geometries in state_to_geometries.values())
        multi_signature_groups = sum(
            len(geometries) > 1 for geometries in signature_to_geometries.values()
        )
        unique_binding = sum(
            len(geometries) == 1 and len(geometry_ids) > 1
            for geometries in state_to_geometries.values()
        )

        expected_geometry_count = _int(adjacency, "geometry_primitive_count")
        expected_state_count = _int(adjacency, "state_primitive_count")
        valid = (
            adjacency.get("status") == ADJACENCY_STATUS
            and len(geometry_ids) == expected_geometry_count
            and len(state_to_geometries) == expected_state_count
            and len(rows) == expected_geometry_count * expected_state_count
            and all(row.get("geometry_stream_complete") == "True" for row in rows)
            and all(row.get("state_stream_complete") == "True" for row in rows)
            and tie_matches == len(rows)
            and exact_rows == 0
            and unique_binding == 0
        )
        counters["profile_tie_count_match"] += tie_matches
        counters["state_primitives"] += len(state_to_geometries)
        counters["ambiguous_state_primitives"] += ambiguous
        counters["single_geometry_state_primitives"] += single
        counters["zero_geometry_state_primitives"] += zero
        counters["multi_geometry_signature_groups"] += multi_signature_groups
        counters["exact_command_shape"] += exact_rows
        counters["unique_binding"] += unique_binding
        counters["valid_object"] += int(valid)
        report.append(
            {
                "model_archive": key[0],
                "object_name": key[1],
                "geometry_candidate_count": str(len(geometry_ids)),
                "state_primitive_count": str(len(state_to_geometries)),
                "profile_rows": str(len(rows)),
                "distinct_state_profile_signatures": str(len(signature_to_states)),
                "profile_tie_count_match_rows": str(tie_matches),
                "state_primitives_with_multiple_geometry_candidates": str(ambiguous),
                "state_primitives_with_single_geometry_candidate": str(single),
                "state_primitives_with_zero_geometry_candidates": str(zero),
                "state_signature_groups_with_multiple_geometry_candidates": str(multi_signature_groups),
                "exact_command_shape_rows": str(exact_rows),
                "unique_state_to_geometry_binding_proven_rows": str(unique_binding),
                "status": STATUS if valid else f"inconclusive-{STATUS}",
                "semantic_binding": SEMANTIC_BINDING,
                "original_semantics": ORIGINAL_SEMANTICS,
                "issue": "" if valid else "state profile discrimination controls require review",
            }
        )

    expected_objects = len(adjacency_by_object)
    summary = {
        "profile_source_report": "",
        "adjacency_source_report": "",
        "profile_rows": str(len(profile_rows)),
        "profile_objects": str(len(profiles_by_object)),
        "geometry_candidate_rows": str(sum(len({row["geometry_primitive"] for row in rows}) for rows in profiles_by_object.values())),
        "state_primitive_rows": str(counters["state_primitives"]),
        "distinct_object_state_profile_signatures": str(sum(len({row["state_command_shape_sha256"] for row in rows}) for rows in profiles_by_object.values())),
        "profile_tie_count_match_rows": str(counters["profile_tie_count_match"]),
        "state_primitives_with_multiple_geometry_candidates": str(counters["ambiguous_state_primitives"]),
        "state_primitives_with_single_geometry_candidate": str(counters["single_geometry_state_primitives"]),
        "state_primitives_with_zero_geometry_candidates": str(counters["zero_geometry_state_primitives"]),
        "state_signature_groups_with_multiple_geometry_candidates": str(counters["multi_geometry_signature_groups"]),
        "exact_command_shape_rows": str(counters["exact_command_shape"]),
        "unique_state_to_geometry_binding_proven_rows": str(counters["unique_binding"]),
        "valid_object_rows": str(counters["valid_object"]),
        "status": STATUS if (
            len(keys) == expected_objects
            and len(keys) == 24
            and len(profile_rows) == 269
            and counters["state_primitives"] == 180
            and counters["ambiguous_state_primitives"] == 89
            and counters["single_geometry_state_primitives"] == 91
            and counters["zero_geometry_state_primitives"] == 0
            and counters["profile_tie_count_match"] == 269
            and counters["exact_command_shape"] == 0
            and counters["unique_binding"] == 0
            and counters["valid_object"] == 24
            and all(row["status"] == STATUS for row in report)
        ) else f"inconclusive-{STATUS}",
        "semantic_binding": SEMANTIC_BINDING,
        "provenance": PROVENANCE,
        "original_semantics": ORIGINAL_SEMANTICS,
    }
    return report, summary


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("profile_report", type=Path)
    parser.add_argument("adjacency_report", type=Path)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()
    report, summary = analyze(_read(args.profile_report), _read(args.adjacency_report))
    summary["profile_source_report"] = str(args.profile_report)
    summary["adjacency_source_report"] = str(args.adjacency_report)
    _write(args.report, REPORT_FIELDS, report)
    _write(args.summary, SUMMARY_FIELDS, [summary])
    print("FIFA Street 2 independent original state-profile discrimination control")
    for field in SUMMARY_FIELDS:
        print(f"{field}: {summary[field]}")
    print(f"report: {args.report}")
    print(f"summary: {args.summary}")
    return 0 if summary["status"] == STATUS else 1


if __name__ == "__main__":
    raise SystemExit(main())
