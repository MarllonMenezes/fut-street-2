"""Join bounded render-window and indirect-reference reports without semantics."""

from __future__ import annotations

import argparse
import csv
from collections import defaultdict
from pathlib import Path


STATUS = "verified-static-render-indirect-window-join"
ORIGINAL_SEMANTICS = "not-claimed"


def read_tsv(path: str | Path) -> list[dict[str, str]]:
    with Path(path).open("r", encoding="utf-8", newline="") as handle:
        return list(csv.DictReader(handle, delimiter="\t"))


def parse_hex(value: str) -> int:
    return int(value, 0)


def _raw_word_key(value: str) -> str:
    return f"0x{parse_hex(value):08X}"


def build_join(
    target_rows: list[dict[str, str]],
    transfer_rows: list[dict[str, str]],
    reference_rows: list[dict[str, str]],
    slice_rows: list[dict[str, str]],
) -> tuple[list[dict[str, str]], dict[str, str]]:
    """Join only exact observed virtual addresses and raw-word values."""
    transfers_by_target: dict[str, list[dict[str, str]]] = defaultdict(list)
    for row in transfer_rows:
        transfers_by_target[row["source_target_virtual_address"]].append(row)

    references_by_target: dict[str, list[dict[str, str]]] = defaultdict(list)
    for row in reference_rows:
        references_by_target[row["source_target_virtual_address"]].append(row)

    transfer_instruction_keys = {
        (row["source_target_virtual_address"], row["instruction_virtual_address"])
        for row in transfer_rows
    }

    slices_by_word = {row["candidate_target_virtual_address"]: row for row in slice_rows}

    joined: list[dict[str, str]] = []
    for target in target_rows:
        address = target["target_virtual_address"]
        transfers = transfers_by_target.get(address, [])
        references = references_by_target.get(address, [])
        exact_instruction_overlap = [
            row
            for row in references
            if (address, row["instruction_virtual_address"]) in transfer_instruction_keys
        ]
        raw_words = sorted(
            {_raw_word_key(row["raw_word"]) for row in references},
            key=parse_hex,
        )
        slices = [slices_by_word[word] for word in raw_words if word in slices_by_word]
        joined.append(
            {
                "target_virtual_address": address,
                "target_context_rows": target.get("context_rows", ""),
                "indirect_transfer_count": str(len(transfers)),
                "indirect_call_count": str(
                    sum(row.get("mnemonic", "").lower() == "call" for row in transfers)
                ),
                "indirect_jump_count": str(
                    sum(row.get("mnemonic", "").lower() != "call" for row in transfers)
                ),
                "reference_row_count": str(len(references)),
                "transfer_reference_instruction_overlap_count": str(len(exact_instruction_overlap)),
                "distinct_raw_word_count": str(len(raw_words)),
                "raw_words": ",".join(raw_words),
                "disassembled_raw_word_count": str(len(slices)),
                "ret_terminated_raw_word_count": str(
                    sum(row.get("ret_found", "False") == "True" for row in slices)
                ),
                "prologue_like_raw_word_count": str(
                    sum(row.get("prologue_shape", "none") != "none" for row in slices)
                ),
                "status": STATUS,
                "original_semantics": ORIGINAL_SEMANTICS,
            }
        )

    summary = {
        "target_windows": str(len(target_rows)),
        "targets_with_indirect_transfers": str(
            sum(bool(transfers_by_target.get(row["target_virtual_address"])) for row in target_rows)
        ),
        "total_indirect_transfers": str(len(transfer_rows)),
        "indirect_calls": str(sum(row.get("mnemonic", "").lower() == "call" for row in transfer_rows)),
        "indirect_jumps": str(sum(row.get("mnemonic", "").lower() != "call" for row in transfer_rows)),
        "targets_with_references": str(
            sum(bool(references_by_target.get(row["target_virtual_address"])) for row in target_rows)
        ),
        "reference_rows": str(len(reference_rows)),
        "targets_with_transfer_reference_instruction_overlap": str(
            sum(
                any(
                    (row["source_target_virtual_address"], row["instruction_virtual_address"])
                    in transfer_instruction_keys
                    for row in references_by_target.get(target["target_virtual_address"], [])
                )
                for target in target_rows
            )
        ),
        "exact_transfer_reference_instruction_rows": str(
            sum(
                sum(
                    (row["source_target_virtual_address"], row["instruction_virtual_address"])
                    in transfer_instruction_keys
                    for row in references
                )
                for references in references_by_target.values()
            )
        ),
        "distinct_raw_words": str(len({_raw_word_key(row["raw_word"]) for row in reference_rows})),
        "executable_raw_words": str(len(slice_rows)),
        "disassembled_candidate_words": str(len(slice_rows)),
        "ret_terminated_candidate_words": str(
            sum(row.get("ret_found", "False") == "True" for row in slice_rows)
        ),
        "prologue_like_candidate_words": str(
            sum(row.get("prologue_shape", "none") != "none" for row in slice_rows)
        ),
        "status": STATUS,
        "original_semantics": ORIGINAL_SEMANTICS,
    }
    return joined, summary


def write_tsv(path: str | Path, fieldnames: list[str], rows: list[dict[str, str]]) -> None:
    with Path(path).open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=fieldnames, delimiter="\t", lineterminator="\n")
        writer.writeheader()
        writer.writerows(rows)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("target_report")
    parser.add_argument("transfer_report")
    parser.add_argument("reference_report")
    parser.add_argument("slice_report")
    parser.add_argument("--report", required=True)
    parser.add_argument("--summary", required=True)
    args = parser.parse_args()

    joined, summary = build_join(
        read_tsv(args.target_report),
        read_tsv(args.transfer_report),
        read_tsv(args.reference_report),
        read_tsv(args.slice_report),
    )
    write_tsv(args.report, list(joined[0]), joined)
    write_tsv(args.summary, list(summary), [summary])

    print("FIFA Street 2 PC bounded render indirect-window join audit")
    for key in (
        "target_windows",
        "targets_with_indirect_transfers",
        "total_indirect_transfers",
        "indirect_calls",
        "indirect_jumps",
        "targets_with_references",
        "reference_rows",
        "targets_with_transfer_reference_instruction_overlap",
        "exact_transfer_reference_instruction_rows",
        "distinct_raw_words",
        "executable_raw_words",
        "disassembled_candidate_words",
        "ret_terminated_candidate_words",
        "prologue_like_candidate_words",
    ):
        print(f"{key}: {summary[key]}")
    print(f"status: {STATUS}")
    print(f"original_semantics: {ORIGINAL_SEMANTICS}")
    print(f"report: {Path(args.report).resolve()}")
    print(f"summary: {Path(args.summary).resolve()}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
