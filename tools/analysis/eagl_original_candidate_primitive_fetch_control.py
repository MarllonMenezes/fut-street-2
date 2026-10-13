"""Join original render-chain primitives to candidate fetch primitives.

The comparison is deliberately structural: command counts/completeness, fetch
slot ids, state-assignment text and primitive-type flags are rejoined by the
raw object/layer/primitive key.  It does not assign a slot or stream to
position, UV, material, skinning, rigging, animation, morph, command or GPU
semantics.
"""

from __future__ import annotations

import argparse
import csv
import re
from pathlib import Path
from typing import Dict, List, Sequence, Tuple


STATUS = "verified-independent-original-candidate-primitive-fetch-control"
ORIGINAL_SEMANTICS = "not-claimed"
PROVENANCE = (
    "independent-primitive-key-join+original-render-chain/candidate-fetch-reparse/"
    "command-count-completeness+slot-id+state-text-controls;no-slot-semantic-promotion"
)

REPORT_FIELDS = (
    "model_archive",
    "model_object",
    "layer",
    "primitive",
    "original_command_count",
    "candidate_command_count",
    "original_command_bytes_sha256",
    "candidate_command_bytes_sha256",
    "original_command_stream_complete",
    "candidate_command_stream_complete",
    "original_fetch_slot_count",
    "candidate_fetch_slot_count",
    "original_fetch_slot_ids",
    "candidate_fetch_slot_ids",
    "original_complete_five_slot_fetch_sequence",
    "candidate_complete_five_slot_layout",
    "original_state_assignments",
    "candidate_state_names",
    "original_has_primitive_type_assignment",
    "candidate_primitive_type",
    "command_count_match",
    "command_bytes_sha256_match",
    "command_stream_complete_match",
    "fetch_slot_count_match",
    "fetch_slot_id_sequence_match",
    "complete_five_slot_sequence_match",
    "state_assignment_sequence_match",
    "primitive_type_flag_match",
    "candidate_slot_records_valid",
    "candidate_slot_records_unmaterialized_expected",
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
    "original_rows",
    "candidate_rows",
    "joined_rows",
    "missing_candidate_keys",
    "missing_original_keys",
    "command_count_match_rows",
    "command_bytes_sha256_match_rows",
    "command_stream_complete_match_rows",
    "fetch_slot_count_match_rows",
    "fetch_slot_id_sequence_match_rows",
    "complete_five_slot_sequence_match_rows",
    "state_assignment_sequence_match_rows",
    "primitive_type_flag_match_rows",
    "candidate_slot_records_valid_rows",
    "candidate_slot_records_unmaterialized_expected_rows",
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
        return int(row.get(field, ""))
    except ValueError:
        return None


def _tokens(value: str) -> Tuple[str, ...]:
    # Original state text uses semicolons and candidate state text uses pipes.
    return tuple(token.strip() for token in re.split(r"[;|]", value) if token.strip())


def _primitive_type_from_original(row: Dict[str, str]) -> str:
    for token in _tokens(row.get("state_assignments", "")):
        if token.startswith("SetPrimitiveType=EAGL::"):
            return token.split("=EAGL::", 1)[1]
    return ""


def _slot_ids_from_candidate(value: str) -> Tuple[str, ...]:
    ids = []
    for record in value.split("|"):
        match = re.search(r"(?:^|;)slot=(\d+)(?:;|$)", record)
        if match:
            ids.append(match.group(1))
    return tuple(ids)


def analyze(
    original_rows: Sequence[Dict[str, str]],
    candidate_rows: Sequence[Dict[str, str]],
) -> Tuple[List[Dict[str, str]], Dict[str, str]]:
    original_by_key = {_key(row): row for row in original_rows}
    candidate_by_key = {_key(row): row for row in candidate_rows}
    keys = sorted(set(original_by_key) & set(candidate_by_key))
    counters = {
        "command_count_match": 0,
        "command_bytes_sha256_match": 0,
        "command_stream_complete_match": 0,
        "fetch_slot_count_match": 0,
        "fetch_slot_id_sequence_match": 0,
        "complete_five_slot_sequence_match": 0,
        "state_assignment_sequence_match": 0,
        "primitive_type_flag_match": 0,
        "candidate_slot_records_valid": 0,
        "candidate_slot_records_unmaterialized_expected": 0,
        "original_input_valid": 0,
        "candidate_input_valid": 0,
    }
    report: List[Dict[str, str]] = []
    for key in keys:
        original = original_by_key[key]
        candidate = candidate_by_key[key]
        original_command_count = _int(original, "command_count")
        candidate_command_count = _int(candidate, "command_count")
        original_command_bytes_sha256 = original.get("command_bytes_sha256", "")
        candidate_command_bytes_sha256 = candidate.get("command_bytes_sha256", "")
        original_slot_count = _int(original, "fetch_slot_count")
        candidate_slot_count = _int(candidate, "fetch_slot_count")
        original_slot_ids = tuple(
            token.strip() for token in original.get("fetch_slot_ids", "").split(",") if token.strip()
        )
        candidate_slot_ids = _slot_ids_from_candidate(candidate.get("slot_records", ""))
        original_complete = _bool(original, "complete_five_slot_fetch_sequence")
        candidate_complete = _bool(candidate, "complete_five_slot_layout")
        original_assignments = _tokens(original.get("state_assignments", ""))
        candidate_assignments = _tokens(candidate.get("state_names", ""))
        original_type = _primitive_type_from_original(original)
        candidate_type = candidate.get("primitive_type", "")
        original_has_type = _bool(original, "has_primitive_type_assignment")
        candidate_has_type = bool(candidate_type and candidate_type != "none")
        candidate_slot_records_unmaterialized_expected = (
            candidate_slot_count == original_slot_count
            and not candidate_slot_ids
            and not candidate.get("slot_records", "").strip()
            and not original_complete
            and not candidate_complete
        )
        values = {
            "command_count_match": original_command_count is not None and original_command_count == candidate_command_count,
            "command_bytes_sha256_match": (
                len(original_command_bytes_sha256) == 64
                and len(candidate_command_bytes_sha256) == 64
                and original_command_bytes_sha256.upper() == candidate_command_bytes_sha256.upper()
            ),
            "command_stream_complete_match": _bool(original, "command_stream_complete") == _bool(candidate, "command_stream_complete"),
            "fetch_slot_count_match": original_slot_count is not None and original_slot_count == candidate_slot_count,
            "fetch_slot_id_sequence_match": original_slot_ids == candidate_slot_ids,
            "complete_five_slot_sequence_match": original_complete == candidate_complete,
            "state_assignment_sequence_match": original_assignments == candidate_assignments,
            "primitive_type_flag_match": original_has_type == candidate_has_type and (not original_has_type or original_type == candidate_type),
            "candidate_slot_records_valid": candidate_slot_count == len(candidate_slot_ids) and candidate.get("status") == "verified-bounded-candidate-fetch-record-association",
            "candidate_slot_records_unmaterialized_expected": candidate_slot_records_unmaterialized_expected and candidate.get("status") == "opaque-incomplete-fetch-record-association",
            "original_input_valid": original.get("status") == "verified-bounded-original-render-chain-structure" and original.get("original_semantics") == ORIGINAL_SEMANTICS,
            "candidate_input_valid": candidate.get("status") in {"verified-bounded-candidate-fetch-record-association", "opaque-incomplete-fetch-record-association"} and candidate.get("original_semantics") == ORIGINAL_SEMANTICS,
        }
        for field, value in values.items():
            counters[field] += int(value)
        valid = all(
            values[field]
            for field in values
            if field not in {
                "fetch_slot_id_sequence_match",
                "candidate_slot_records_valid",
                "candidate_slot_records_unmaterialized_expected",
            }
        ) and (
            values["fetch_slot_id_sequence_match"]
            or values["candidate_slot_records_unmaterialized_expected"]
        ) and (
            values["candidate_slot_records_valid"]
            or values["candidate_slot_records_unmaterialized_expected"]
        )
        report.append(
            {
                "model_archive": key[0],
                "model_object": key[1],
                "layer": key[2],
                "primitive": key[3],
                "original_command_count": original.get("command_count", ""),
                "candidate_command_count": candidate.get("command_count", ""),
                "original_command_bytes_sha256": original_command_bytes_sha256,
                "candidate_command_bytes_sha256": candidate_command_bytes_sha256,
                "original_command_stream_complete": original.get("command_stream_complete", ""),
                "candidate_command_stream_complete": candidate.get("command_stream_complete", ""),
                "original_fetch_slot_count": original.get("fetch_slot_count", ""),
                "candidate_fetch_slot_count": candidate.get("fetch_slot_count", ""),
                "original_fetch_slot_ids": original.get("fetch_slot_ids", ""),
                "candidate_fetch_slot_ids": ",".join(candidate_slot_ids),
                "original_complete_five_slot_fetch_sequence": str(original_complete),
                "candidate_complete_five_slot_layout": str(candidate_complete),
                "original_state_assignments": "|".join(original_assignments),
                "candidate_state_names": "|".join(candidate_assignments),
                "original_has_primitive_type_assignment": str(original_has_type),
                "candidate_primitive_type": candidate_type,
                **{field: str(value) for field, value in values.items()},
                "status": STATUS if valid else f"inconclusive-{STATUS}",
                "provenance": PROVENANCE,
                "original_semantics": ORIGINAL_SEMANTICS,
                "issue": "" if valid else "one or more structural primitive/fetch controls failed",
            }
        )

    complete = (
        len(original_rows) >= len(candidate_rows)
        and len(candidate_rows) == 218
        and len(keys) == 218
        and not (set(candidate_by_key) - set(original_by_key))
        and counters["command_count_match"] == 218
        and counters["command_bytes_sha256_match"] == 218
        and counters["command_stream_complete_match"] == 218
        and counters["fetch_slot_count_match"] == 218
        and counters["fetch_slot_id_sequence_match"] == 216
        and counters["complete_five_slot_sequence_match"] == 218
        and counters["state_assignment_sequence_match"] == 218
        and counters["primitive_type_flag_match"] == 218
        and counters["candidate_slot_records_valid"] == 216
        and counters["candidate_slot_records_unmaterialized_expected"] == 2
        and counters["original_input_valid"] == 218
        and counters["candidate_input_valid"] == 218
        and all(row["status"] == STATUS for row in report)
    )
    summary = {
        "original_source_report": "",
        "candidate_source_report": "",
        "original_rows": str(len(original_rows)),
        "candidate_rows": str(len(candidate_rows)),
        "joined_rows": str(len(keys)),
        "missing_candidate_keys": str(len(set(original_by_key) - set(candidate_by_key))),
        "missing_original_keys": str(len(set(candidate_by_key) - set(original_by_key))),
        **{f"{field}_rows": str(counters[field]) for field in counters},
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
    print("FIFA Street 2 independent original/candidate primitive fetch control")
    for field in SUMMARY_FIELDS:
        print(f"{field}: {summary[field]}")
    print(f"report: {args.report}")
    print(f"summary: {args.summary}")
    return 0 if summary["status"] == STATUS else 1


if __name__ == "__main__":
    raise SystemExit(main())
