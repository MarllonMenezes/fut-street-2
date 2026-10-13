"""Independently join render fetch-slot IDs to original primitive topology rows.

The join is keyed only by the original archive/object/layer/primitive identity.
It verifies row and slot-set coherence between two TSV reports. It does not
assign a field, vertex attribute, position, UV, material, skinning, rigging,
animation, morph, draw, command, or GPU meaning to any slot or stream.
"""

from __future__ import annotations

import argparse
import csv
from collections import Counter
from pathlib import Path
from typing import Dict, List, Sequence, Tuple


STATUS = "verified-independent-original-primitive-fetch-topology-control"
ORIGINAL_SEMANTICS = "not-claimed"
RENDER_STATUS = "verified-bounded-original-render-chain-structure"
TOPOLOGY_STATUS = "verified-bounded-original-primitive-stream-topology"
TOPOLOGY_INCONCLUSIVE_STATUS = "inconclusive-original-primitive-stream-topology"
PROVENANCE = (
    "independent-TSV-primitive-key-join+render-fetch-slot-id-set+"
    "original-topology-row-coherence;incomplete-slot-2-cases-preserved;"
    "no-semantic-promotion"
)

KEY_FIELDS = ("model_archive", "model_object", "layer", "primitive")
REPORT_FIELDS = (
    *KEY_FIELDS,
    "render_fetch_slot_count",
    "render_fetch_slot_ids",
    "render_complete_five_slot_fetch_sequence",
    "topology_row_count",
    "topology_valid_slot_count",
    "topology_valid_slot_ids",
    "topology_key_ordinal_match_rows",
    "topology_valid_payload_range_rows",
    "topology_structural_match_rows",
    "topology_complete_five_slot_sequence",
    "fetch_slot_count_match",
    "fetch_slot_id_set_match",
    "complete_five_slot_sequence_agreement",
    "render_status_valid",
    "topology_status_valid",
    "status",
    "original_semantics",
    "issue",
)
SUMMARY_FIELDS = (
    "render_source_report",
    "topology_source_report",
    "render_rows",
    "topology_rows",
    "joined_rows",
    "render_input_status_valid_rows",
    "topology_input_status_valid_rows",
    "topology_groups_with_five_rows",
    "topology_valid_slot_rows",
    "topology_key_ordinal_match_rows",
    "topology_valid_payload_range_rows",
    "topology_structural_match_rows",
    "render_fetch_slot_rows",
    "render_complete_five_slot_rows",
    "topology_complete_five_slot_rows",
    "fetch_slot_count_match_rows",
    "fetch_slot_id_set_match_rows",
    "complete_five_slot_sequence_agreement_rows",
    "incomplete_slot_two_rows",
    "missing_render_keys",
    "missing_topology_keys",
    "status",
    "provenance",
    "original_semantics",
)


def _key(row: Dict[str, str]) -> Tuple[str, str, str, str]:
    values = tuple(row.get(field, "") for field in KEY_FIELDS)
    if not all(values):
        raise ValueError(f"source row has an empty primitive identity: {row!r}")
    return values


def _int(row: Dict[str, str], field: str) -> int:
    try:
        return int(row[field], 0)
    except (KeyError, TypeError, ValueError) as exc:
        raise ValueError(f"invalid integer in {field}: {row.get(field)!r}") from exc


def _bool(row: Dict[str, str], field: str) -> bool:
    value = row.get(field)
    if value not in {"True", "False"}:
        raise ValueError(f"invalid boolean in {field}: {value!r}")
    return value == "True"


def _fetch_ids(value: str) -> List[int]:
    if not value:
        return []
    try:
        return [int(part, 0) for part in value.split(",") if part]
    except ValueError as exc:
        raise ValueError(f"invalid fetch-slot list: {value!r}") from exc


def _group(rows: Sequence[Dict[str, str]]) -> Dict[Tuple[str, str, str, str], List[Dict[str, str]]]:
    grouped: Dict[Tuple[str, str, str, str], List[Dict[str, str]]] = {}
    for row in rows:
        grouped.setdefault(_key(row), []).append(row)
    return grouped


def _validate_status(rows: Sequence[Dict[str, str]], expected: Sequence[str], label: str) -> bool:
    if not rows:
        raise ValueError(f"{label} has no rows")
    return all(
        row.get("status") in expected and row.get("original_semantics") == ORIGINAL_SEMANTICS
        for row in rows
    )


def analyze_rows(
    render_rows: Sequence[Dict[str, str]],
    topology_rows: Sequence[Dict[str, str]],
) -> Tuple[List[Dict[str, str]], Dict[str, str]]:
    render_groups = _group(render_rows)
    topology_groups = _group(topology_rows)
    render_status_valid = _validate_status(render_rows, (RENDER_STATUS,), "render report")
    topology_status_valid = _validate_status(
        topology_rows,
        (TOPOLOGY_STATUS, TOPOLOGY_INCONCLUSIVE_STATUS),
        "topology report",
    )
    render_keys = set(render_groups)
    topology_keys = set(topology_groups)
    joined_keys = sorted(render_keys & topology_keys)
    missing_render_keys = sorted(topology_keys - render_keys)
    missing_topology_keys = sorted(render_keys - topology_keys)
    report: List[Dict[str, str]] = []
    for key in joined_keys:
        render_group = render_groups[key]
        topology_group = topology_groups[key]
        if len(render_group) != 1:
            raise ValueError(f"render report is not one row per primitive: {key!r}")
        render = render_group[0]
        if len(topology_group) != 5:
            raise ValueError(f"topology report does not have five rows per primitive: {key!r}")

        render_ids = _fetch_ids(render.get("fetch_slot_ids", ""))
        topology_valid_rows = [
            row for row in topology_group
            if _int(row, "fetch_slot") >= 0
        ]
        topology_ids = sorted(_int(row, "fetch_slot") for row in topology_valid_rows)
        render_ids_sorted = sorted(render_ids)
        topology_key_ordinal_match_rows = sum(
            _bool(row, "key_ordinal_matches_primitive") for row in topology_group
        )
        topology_valid_payload_range_rows = sum(
            _bool(row, "valid_payload_range") for row in topology_group
        )
        topology_structural_match_rows = sum(
            _bool(row, "structural_match") for row in topology_group
        )
        topology_complete = (
            len(topology_valid_rows) == 5
            and topology_ids == [0, 1, 2, 3, 4]
            and topology_structural_match_rows == 5
        )
        render_complete = _bool(render, "complete_five_slot_fetch_sequence")
        fetch_slot_count_match = len(render_ids) == len(topology_valid_rows) == _int(render, "fetch_slot_count")
        fetch_slot_id_set_match = render_ids_sorted == topology_ids
        complete_agreement = render_complete == topology_complete
        row_valid = (
            render_status_valid
            and topology_status_valid
            and len(topology_group) == 5
            and topology_key_ordinal_match_rows == 5
            and topology_valid_payload_range_rows == 5
            and fetch_slot_count_match
            and fetch_slot_id_set_match
            and complete_agreement
        )
        status = STATUS if row_valid else "inconclusive-original-primitive-fetch-topology-control"
        report.append(
            {
                **dict(zip(KEY_FIELDS, key)),
                "render_fetch_slot_count": str(_int(render, "fetch_slot_count")),
                "render_fetch_slot_ids": ",".join(str(value) for value in render_ids_sorted),
                "render_complete_five_slot_fetch_sequence": str(render_complete),
                "topology_row_count": str(len(topology_group)),
                "topology_valid_slot_count": str(len(topology_valid_rows)),
                "topology_valid_slot_ids": ",".join(str(value) for value in topology_ids),
                "topology_key_ordinal_match_rows": str(topology_key_ordinal_match_rows),
                "topology_valid_payload_range_rows": str(topology_valid_payload_range_rows),
                "topology_structural_match_rows": str(topology_structural_match_rows),
                "topology_complete_five_slot_sequence": str(topology_complete),
                "fetch_slot_count_match": str(fetch_slot_count_match),
                "fetch_slot_id_set_match": str(fetch_slot_id_set_match),
                "complete_five_slot_sequence_agreement": str(complete_agreement),
                "render_status_valid": str(render_status_valid),
                "topology_status_valid": str(topology_status_valid),
                "status": status,
                "original_semantics": ORIGINAL_SEMANTICS,
                "issue": "" if row_valid else "source status, primitive identity or fetch-slot topology requires review",
            }
        )

    incomplete_slot_two_rows = sum(
        row["render_fetch_slot_ids"] == "0,1,3,4"
        and row["render_complete_five_slot_fetch_sequence"] == "False"
        for row in report
    )
    summary = {
        "render_source_report": "",
        "topology_source_report": "",
        "render_rows": str(len(render_rows)),
        "topology_rows": str(len(topology_rows)),
        "joined_rows": str(len(joined_keys)),
        "render_input_status_valid_rows": str(len(render_rows) if render_status_valid else 0),
        "topology_input_status_valid_rows": str(len(topology_rows) if topology_status_valid else 0),
        "topology_groups_with_five_rows": str(sum(len(topology_groups[key]) == 5 for key in joined_keys)),
        "topology_valid_slot_rows": str(sum(int(row["topology_valid_slot_count"]) for row in report)),
        "topology_key_ordinal_match_rows": str(sum(int(row["topology_key_ordinal_match_rows"]) for row in report)),
        "topology_valid_payload_range_rows": str(sum(int(row["topology_valid_payload_range_rows"]) for row in report)),
        "topology_structural_match_rows": str(sum(int(row["topology_structural_match_rows"]) for row in report)),
        "render_fetch_slot_rows": str(sum(_int(row, "fetch_slot_count") for row in render_rows)),
        "render_complete_five_slot_rows": str(sum(_bool(row, "complete_five_slot_fetch_sequence") for row in render_rows)),
        "topology_complete_five_slot_rows": str(sum(row["topology_complete_five_slot_sequence"] == "True" for row in report)),
        "fetch_slot_count_match_rows": str(sum(row["fetch_slot_count_match"] == "True" for row in report)),
        "fetch_slot_id_set_match_rows": str(sum(row["fetch_slot_id_set_match"] == "True" for row in report)),
        "complete_five_slot_sequence_agreement_rows": str(sum(row["complete_five_slot_sequence_agreement"] == "True" for row in report)),
        "incomplete_slot_two_rows": str(incomplete_slot_two_rows),
        "missing_render_keys": str(len(missing_render_keys)),
        "missing_topology_keys": str(len(missing_topology_keys)),
        "status": (
            STATUS
            if joined_keys
            and len(joined_keys) == len(render_keys) == len(topology_keys)
            and all(row["status"] == STATUS for row in report)
            else "inconclusive-original-primitive-fetch-topology-control"
        ),
        "provenance": PROVENANCE,
        "original_semantics": ORIGINAL_SEMANTICS,
    }
    return report, summary


def _read(path: Path) -> List[Dict[str, str]]:
    with path.open("r", encoding="utf-8-sig", newline="") as handle:
        return list(csv.DictReader(handle, delimiter="\t"))


def _write(path: Path, fields: Sequence[str], rows: Sequence[Dict[str, str]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=list(fields), delimiter="\t", lineterminator="\n")
        writer.writeheader()
        writer.writerows({field: row.get(field, "") for field in fields} for row in rows)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("render_source_report", type=Path)
    parser.add_argument("topology_source_report", type=Path)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()
    report, summary = analyze_rows(_read(args.render_source_report), _read(args.topology_source_report))
    summary["render_source_report"] = str(args.render_source_report)
    summary["topology_source_report"] = str(args.topology_source_report)
    _write(args.report, REPORT_FIELDS, report)
    _write(args.summary, SUMMARY_FIELDS, [summary])
    print("FIFA Street 2 independent original primitive/fetch topology control")
    for field in SUMMARY_FIELDS:
        print(f"{field}: {summary[field]}")
    print(f"report: {args.report}")
    print(f"summary: {args.summary}")
    return 0 if summary["status"] == STATUS else 1


if __name__ == "__main__":
    raise SystemExit(main())
