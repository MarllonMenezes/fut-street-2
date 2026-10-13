"""Enumerate global original state-to-geometry assignments without promotion.

The ordered interval report exposes only which geometry primitive is immediately
before and immediately after a non-empty state run.  This control treats those
boundary primitives as a structural candidate domain and enumerates every
assignment of state runs to that domain.  It deliberately does not assume that
the previous or next primitive owns the run, that every geometry primitive must
be referenced once, or that a state run is a draw call.  Singleton domains are
reported, but are never promoted to a state-to-draw binding because the domain
itself has no ownership semantics.  One-to-one counts are reported only as a
diagnostic because that cardinality is not evidenced by the source reports.
"""

from __future__ import annotations

import argparse
import csv
from pathlib import Path
from typing import Dict, Iterable, List, Sequence, Tuple


STATUS = "verified-independent-original-state-geometry-global-assignment-control"
SEMANTIC_BINDING = "not-proven"
ORIGINAL_SEMANTICS = "not-claimed"
ORDER_STATUS = "ordered-index-control"
PARTITION_STATUS = "verified-independent-original-state-geometry-partition-control"
PROVENANCE = (
    "independent-TSV-reparse+ordered-interval-boundary-domain+global-assignment-"
    "enumeration+unsupported-cardinality-diagnostic;no-state-draw-or-UV-material-"
    "promotion"
)

REPORT_FIELDS = (
    "model_archive",
    "object_name",
    "shape_name",
    "geometry_candidate_count",
    "geometry_candidate_indices",
    "order_rows",
    "nonempty_interval_count",
    "ambiguous_interval_rows",
    "interval_candidate_domains",
    "assignment_model_count",
    "one_to_one_assignment_model_count",
    "max_interval_domain_size",
    "order_state_count",
    "partition_state_count",
    "state_index_set_reconciled",
    "order_status_valid",
    "partition_status_valid",
    "unique_state_geometry_assignment_proven",
    "status",
    "semantic_binding",
    "original_semantics",
    "issue",
)

SUMMARY_FIELDS = (
    "order_source_report",
    "partition_source_report",
    "order_rows",
    "partition_rows",
    "joined_object_rows",
    "missing_partition_objects",
    "order_input_status_valid_rows",
    "partition_input_status_valid_rows",
    "nonempty_interval_rows",
    "ambiguous_interval_rows",
    "objects_with_multiple_assignment_models",
    "objects_with_unique_assignment_model",
    "objects_with_one_to_one_unique_model",
    "unconstrained_assignment_model_sum",
    "one_to_one_assignment_model_sum",
    "state_index_set_reconciled_rows",
    "unique_state_geometry_assignment_proven_rows",
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
    key = (row.get("model_archive", ""), row.get("object_name", ""))
    if not all(key):
        raise ValueError(f"empty object identity: {key!r}")
    return key


def _group(rows: Sequence[Dict[str, str]]) -> Dict[Tuple[str, str], List[Dict[str, str]]]:
    grouped: Dict[Tuple[str, str], List[Dict[str, str]]] = {}
    for row in rows:
        grouped.setdefault(_key(row), []).append(row)
    return grouped


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


def _optional_int(row: Dict[str, str], field: str) -> int | None:
    value = row.get(field, "").strip()
    if not value:
        return None
    try:
        return int(value, 0)
    except ValueError as exc:
        raise ValueError(f"invalid optional integer in {field}: {value!r}") from exc


def _state_indices(value: str) -> set[str]:
    return {part for part in value.split(",") if part}


def _injective_assignment_count(domains: Sequence[Tuple[int, ...]]) -> int:
    """Count injective assignments for diagnostics only."""

    count = 0

    def visit(index: int, used: set[int]) -> None:
        nonlocal count
        if index == len(domains):
            count += 1
            return
        for candidate in domains[index]:
            if candidate not in used:
                used.add(candidate)
                visit(index + 1, used)
                used.remove(candidate)

    visit(0, set())
    return count


def _domain(row: Dict[str, str]) -> Tuple[int, ...]:
    previous = _optional_int(row, "previous_geometry_primitive")
    following = _optional_int(row, "next_geometry_primitive")
    kind = row.get("interval_kind", "")
    if kind == "before-first-geometry":
        if following is None:
            raise ValueError("before-first interval has no next geometry")
        return (following,)
    if kind == "after-last-geometry":
        if previous is None:
            raise ValueError("after-last interval has no previous geometry")
        return (previous,)
    if kind == "between-geometry-candidates":
        if previous is None or following is None:
            raise ValueError("between interval has an incomplete boundary")
        return tuple(sorted({previous, following}))
    raise ValueError(f"unknown interval kind: {kind!r}")


def analyze(
    order_rows: Sequence[Dict[str, str]],
    partition_rows: Sequence[Dict[str, str]],
) -> Tuple[List[Dict[str, str]], Dict[str, str]]:
    if not order_rows or not partition_rows:
        raise ValueError("both source reports must contain rows")

    ordered = _group(order_rows)
    partitions = _group(partition_rows)
    keys = sorted(set(ordered) & set(partitions))
    report: List[Dict[str, str]] = []
    counters = {
        "nonempty_interval_rows": 0,
        "ambiguous_interval_rows": 0,
        "objects_with_multiple_assignment_models": 0,
        "objects_with_unique_assignment_model": 0,
        "objects_with_one_to_one_unique_model": 0,
        "unconstrained_assignment_model_sum": 0,
        "one_to_one_assignment_model_sum": 0,
        "state_index_set_reconciled_rows": 0,
        "unique_state_geometry_assignment_proven_rows": 0,
        "valid_object_rows": 0,
    }

    for key in keys:
        ordered_rows = sorted(ordered[key], key=lambda row: _int(row, "interval_index"))
        partition = partitions[key]
        if len(partition) != 1:
            raise ValueError(f"partition report is not one row per object: {key!r}")
        partition_row = partition[0]
        order_status_valid = all(row.get("status") == ORDER_STATUS for row in ordered_rows)
        partition_status_valid = partition_row.get("status") == PARTITION_STATUS
        state_count = sum(_int(row, "state_primitive_count") for row in ordered_rows)
        partition_state_count = _int(partition_row, "order_partitioned_state_count")
        state_index_set_reconciled = partition_row.get("state_index_set_match") == "True"
        nonempty = [row for row in ordered_rows if _int(row, "state_primitive_count") > 0]
        domains = [_domain(row) for row in nonempty]
        geometry_indices = sorted(
            {
                candidate
                for domain in domains
                for candidate in domain
            }
        )
        assignment_models = 1
        for domain in domains:
            assignment_models *= len(domain)
        one_to_one_models = _injective_assignment_count(domains)
        ambiguous_intervals = sum(len(domain) > 1 for domain in domains)
        max_domain_size = max((len(domain) for domain in domains), default=0)
        unique_assignment = assignment_models == 1
        unique_binding_proven = False
        valid = (
            len(ordered_rows) > 0
            and order_status_valid
            and partition_status_valid
            and partition_row.get("original_semantics") == ORIGINAL_SEMANTICS
            and state_count == partition_state_count
            and state_index_set_reconciled
            and all(domain for domain in domains)
            and assignment_models >= 1
            and not unique_binding_proven
        )
        counters["nonempty_interval_rows"] += len(nonempty)
        counters["ambiguous_interval_rows"] += ambiguous_intervals
        counters["objects_with_multiple_assignment_models"] += int(assignment_models > 1)
        counters["objects_with_unique_assignment_model"] += int(assignment_models == 1)
        counters["objects_with_one_to_one_unique_model"] += int(one_to_one_models == 1)
        counters["unconstrained_assignment_model_sum"] += assignment_models
        counters["one_to_one_assignment_model_sum"] += one_to_one_models
        counters["state_index_set_reconciled_rows"] += int(state_index_set_reconciled)
        counters["unique_state_geometry_assignment_proven_rows"] += int(unique_binding_proven)
        counters["valid_object_rows"] += int(valid)
        interval_domains = ";".join(
            f"{_int(row, 'interval_index')}:{','.join(str(candidate) for candidate in domain)}"
            for row, domain in zip(nonempty, domains)
        )
        report.append(
            {
                "model_archive": key[0],
                "object_name": key[1],
                "shape_name": ordered_rows[0].get("shape_name", ""),
                "geometry_candidate_count": ordered_rows[0].get("geometry_candidate_count_in_layer", ""),
                "geometry_candidate_indices": ",".join(str(candidate) for candidate in geometry_indices),
                "order_rows": str(len(ordered_rows)),
                "nonempty_interval_count": str(len(nonempty)),
                "ambiguous_interval_rows": str(ambiguous_intervals),
                "interval_candidate_domains": interval_domains,
                "assignment_model_count": str(assignment_models),
                "one_to_one_assignment_model_count": str(one_to_one_models),
                "max_interval_domain_size": str(max_domain_size),
                "order_state_count": str(state_count),
                "partition_state_count": str(partition_state_count),
                "state_index_set_reconciled": str(state_index_set_reconciled),
                "order_status_valid": str(order_status_valid),
                "partition_status_valid": str(partition_status_valid),
                "unique_state_geometry_assignment_proven": str(unique_binding_proven),
                "status": STATUS if valid else f"inconclusive-{STATUS}",
                "semantic_binding": SEMANTIC_BINDING,
                "original_semantics": ORIGINAL_SEMANTICS,
                "issue": "" if valid else "global state/geometry assignment domain was incomplete or unique",
            }
        )

    missing_partition = set(ordered) - set(partitions)
    summary = {
        "order_source_report": "",
        "partition_source_report": "",
        "order_rows": str(len(order_rows)),
        "partition_rows": str(len(partition_rows)),
        "joined_object_rows": str(len(keys)),
        "missing_partition_objects": str(len(missing_partition)),
        "order_input_status_valid_rows": str(sum(row.get("status") == ORDER_STATUS for row in order_rows)),
        "partition_input_status_valid_rows": str(sum(row.get("status") == PARTITION_STATUS for row in partition_rows)),
        **{field: str(value) for field, value in counters.items()},
        "status": STATUS if (
            len(keys) == 24
            and not missing_partition
            and sum(row.get("status") == ORDER_STATUS for row in order_rows) == len(order_rows)
            and sum(row.get("status") == PARTITION_STATUS for row in partition_rows) == len(partition_rows)
            and counters["state_index_set_reconciled_rows"] == len(keys)
            and counters["nonempty_interval_rows"] == 36
            and counters["ambiguous_interval_rows"] == 12
            and counters["objects_with_multiple_assignment_models"] == 12
            and counters["objects_with_unique_assignment_model"] == 12
            and counters["objects_with_one_to_one_unique_model"] == 24
            and counters["unique_state_geometry_assignment_proven_rows"] == 0
            and counters["valid_object_rows"] == len(keys)
            and all(row["status"] == STATUS for row in report)
        ) else f"inconclusive-{STATUS}",
        "semantic_binding": SEMANTIC_BINDING,
        "provenance": PROVENANCE,
        "original_semantics": ORIGINAL_SEMANTICS,
    }
    return report, summary


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("order_report", type=Path)
    parser.add_argument("partition_report", type=Path)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()
    report, summary = analyze(_read(args.order_report), _read(args.partition_report))
    summary["order_source_report"] = str(args.order_report)
    summary["partition_source_report"] = str(args.partition_report)
    _write(args.report, REPORT_FIELDS, report)
    _write(args.summary, SUMMARY_FIELDS, [summary])
    print("FIFA Street 2 independent original state/geometry global assignment control")
    for field in SUMMARY_FIELDS:
        print(f"{field}: {summary[field]}")
    print(f"report: {args.report}")
    print(f"summary: {args.summary}")
    return 0 if summary["status"] == STATUS else 1


if __name__ == "__main__":
    raise SystemExit(main())
