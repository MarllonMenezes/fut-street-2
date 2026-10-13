"""Independently reconcile managed ABKC candidate selection with exhaustive rows.

This control compares the bounded batch decoder's first accepted sound candidate
with the corresponding row in the exhaustive payload report.  It verifies that
the clean-room decoder is internally consistent across two policies; it does
not identify an original EVT selector, event ID, sample meaning or playback
route.
"""

from __future__ import annotations

import argparse
import csv
from collections import defaultdict
from pathlib import Path
from typing import Dict, Iterable, List, Sequence, Tuple


STATUS = "verified-independent-audio-bank-decoder-selection-control"
DIRECT_SELECTION_BINDING = "not-proven"
ORIGINAL_SEMANTICS = "not-claimed"
PROVENANCE = (
    "independent-TSV-keyed-batch-vs-exhaustive-bank-row-join+"
    "selected-index/kind/wave-byte-reconciliation;no-original-selection-promotion"
)

REPORT_FIELDS = (
    "bank_path",
    "batch_source_kind",
    "batch_candidate_sound_count",
    "batch_attempt_count",
    "batch_selected_sound_index",
    "batch_decode_validated",
    "batch_wave_bytes",
    "exhaustive_bank_rows",
    "selected_identity_rows",
    "selected_kind_match_rows",
    "selected_wave_bytes_match_rows",
    "rejected_policy_match",
    "status",
    "direct_selection_binding",
    "original_semantics",
    "issue",
)

SUMMARY_FIELDS = (
    "batch_source_report",
    "exhaustive_source_report",
    "batch_rows",
    "exhaustive_bank_rows",
    "unique_batch_banks",
    "unique_exhaustive_banks",
    "batch_decoded_rows",
    "batch_rejected_rows",
    "selected_identity_rows",
    "selected_kind_match_rows",
    "selected_wave_bytes_match_rows",
    "rejected_policy_match_rows",
    "valid_control_rows",
    "status",
    "provenance",
    "direct_selection_binding",
    "original_semantics",
)


def read_tsv(path: Path) -> List[Dict[str, str]]:
    with path.open("r", encoding="utf-8-sig", newline="") as handle:
        return list(csv.DictReader(handle, delimiter="\t"))


def as_int(row: Dict[str, str], field: str) -> int:
    try:
        return int(row.get(field, ""), 0)
    except (TypeError, ValueError) as exc:
        raise ValueError(f"invalid integer in {field}: {row.get(field)!r}") from exc


def as_bool(row: Dict[str, str], field: str) -> bool:
    return row.get(field, "").strip().lower() == "true"


def analyze(
    batch_rows: Sequence[Dict[str, str]],
    exhaustive_rows: Sequence[Dict[str, str]],
) -> Tuple[List[Dict[str, str]], Dict[str, str]]:
    if not batch_rows:
        raise ValueError("batch decoder report must contain rows")
    bank_rows = [row for row in exhaustive_rows if row.get("kind") == "bank"]
    exhaustive_by_bank: Dict[str, List[Dict[str, str]]] = defaultdict(list)
    for row in bank_rows:
        exhaustive_by_bank[row.get("source_path", "")].append(row)

    report: List[Dict[str, str]] = []
    decoded_rows = 0
    rejected_rows = 0
    selected_identity_rows = 0
    selected_kind_match_rows = 0
    selected_wave_bytes_match_rows = 0
    rejected_policy_match_rows = 0

    for batch in batch_rows:
        bank_path = batch.get("bank_path", "")
        candidates = exhaustive_by_bank.get(bank_path, [])
        batch_decoded = as_bool(batch, "decode_validated")
        if batch_decoded:
            decoded_rows += 1
            selected_index = batch.get("selected_sound_index", "")
            selected = [
                row for row in candidates
                if row.get("row_or_sound_index") == selected_index
                and as_bool(row, "decode_validated")
            ]
            identity = len(selected) == 1
            kind_match = identity and selected[0].get("name") == batch.get("source_kind")
            wave_match = identity and selected[0].get("wave_bytes") == batch.get("wave_bytes")
            selected_identity_rows += int(identity)
            selected_kind_match_rows += int(kind_match)
            selected_wave_bytes_match_rows += int(wave_match)
            valid = identity and kind_match and wave_match and as_int(batch, "attempt_count") > 0
            issue = "" if valid else "selected batch candidate did not reconcile to one exhaustive decoded row"
        else:
            rejected_rows += 1
            selected_index = batch.get("selected_sound_index", "")
            rejected_policy = (
                as_int(batch, "candidate_sound_count") == 0
                and as_int(batch, "attempt_count") == 0
                and not any(as_bool(row, "decode_validated") for row in candidates)
                and selected_index in {"", "0"}
                and as_int(batch, "wave_bytes") == 0
            )
            rejected_policy_match_rows += int(rejected_policy)
            valid = rejected_policy
            issue = "" if valid else "rejected batch policy did not reconcile to an empty exhaustive candidate set"
            identity = False
            kind_match = False
            wave_match = False

        report.append({
            "bank_path": bank_path,
            "batch_source_kind": batch.get("source_kind", ""),
            "batch_candidate_sound_count": batch.get("candidate_sound_count", ""),
            "batch_attempt_count": batch.get("attempt_count", ""),
            "batch_selected_sound_index": batch.get("selected_sound_index", ""),
            "batch_decode_validated": str(batch_decoded),
            "batch_wave_bytes": batch.get("wave_bytes", ""),
            "exhaustive_bank_rows": str(len(candidates)),
            "selected_identity_rows": str(int(identity)),
            "selected_kind_match_rows": str(int(kind_match)),
            "selected_wave_bytes_match_rows": str(int(wave_match)),
            "rejected_policy_match": str(int(not batch_decoded and valid)),
            "status": STATUS if valid else f"inconclusive-{STATUS}",
            "direct_selection_binding": DIRECT_SELECTION_BINDING,
            "original_semantics": ORIGINAL_SEMANTICS,
            "issue": issue,
        })

    valid_rows = sum(row["status"] == STATUS for row in report)
    summary = {
        "batch_source_report": "",
        "exhaustive_source_report": "",
        "batch_rows": str(len(batch_rows)),
        "exhaustive_bank_rows": str(len(bank_rows)),
        "unique_batch_banks": str(len({row.get("bank_path", "") for row in batch_rows})),
        "unique_exhaustive_banks": str(len(exhaustive_by_bank)),
        "batch_decoded_rows": str(decoded_rows),
        "batch_rejected_rows": str(rejected_rows),
        "selected_identity_rows": str(selected_identity_rows),
        "selected_kind_match_rows": str(selected_kind_match_rows),
        "selected_wave_bytes_match_rows": str(selected_wave_bytes_match_rows),
        "rejected_policy_match_rows": str(rejected_policy_match_rows),
        "valid_control_rows": str(valid_rows),
        "status": STATUS if (
            len(batch_rows) == 16
            and len(bank_rows) == 242
            and len(exhaustive_by_bank) == 16
            and decoded_rows == 15
            and rejected_rows == 1
            and selected_identity_rows == 15
            and selected_kind_match_rows == 15
            and selected_wave_bytes_match_rows == 15
            and rejected_policy_match_rows == 1
            and valid_rows == 16
        ) else f"inconclusive-{STATUS}",
        "provenance": PROVENANCE,
        "direct_selection_binding": DIRECT_SELECTION_BINDING,
        "original_semantics": ORIGINAL_SEMANTICS,
    }
    return report, summary


def write_tsv(path: Path, fields: Iterable[str], rows: Iterable[Dict[str, str]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=list(fields), delimiter="\t", lineterminator="\n")
        writer.writeheader()
        writer.writerows({field: row.get(field, "") for field in writer.fieldnames} for row in rows)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("batch_report", type=Path)
    parser.add_argument("exhaustive_report", type=Path)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()
    report, summary = analyze(read_tsv(args.batch_report), read_tsv(args.exhaustive_report))
    summary["batch_source_report"] = str(args.batch_report)
    summary["exhaustive_source_report"] = str(args.exhaustive_report)
    write_tsv(args.report, REPORT_FIELDS, report)
    write_tsv(args.summary, SUMMARY_FIELDS, [summary])
    print("FIFA Street 2 independent audio bank decoder-selection control")
    for field in SUMMARY_FIELDS:
        print(f"{field}: {summary[field]}")
    print(f"report: {args.report}")
    print(f"summary: {args.summary}")
    return 0 if summary["status"] == STATUS else 1


if __name__ == "__main__":
    raise SystemExit(main())
