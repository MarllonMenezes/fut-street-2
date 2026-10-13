"""Independent TSV reparse/control for the original opaque __VB association."""

from __future__ import annotations

import csv
from collections import defaultdict
from pathlib import Path
from typing import Iterable


STATUS = "verified-independent-original-opaque-vertex-record-association-control"
ORIGINAL_SEMANTICS = "not-claimed"
PROVENANCE = (
    "independent-TSV-reparse+row-local-stride-order-controls;"
    "original-opaque-association-only;no-semantic-promotion"
)


def _parse_keys(value: str) -> list[tuple[int, int, int]]:
    keys: list[tuple[int, int, int]] = []
    for record in value.split("|") if value else []:
        fields = {
            part.split("=", 1)[0]: part.split("=", 1)[1]
            for part in record.split(";")
            if "=" in part
        }
        if {"category", "ordinal", "stride"} <= fields.keys():
            keys.append(
                (
                    int(fields["category"]),
                    int(fields["ordinal"]),
                    int(fields["stride"]),
                )
            )
    return sorted(keys)


def _parse_strides(value: str) -> list[int]:
    return [int(item) for item in value.split(",") if item]


def analyze_rows(rows: Iterable[dict[str, str]]) -> tuple[list[dict[str, str]], dict[str, str]]:
    source_rows = list(rows)
    output_rows: list[dict[str, str]] = []
    exact_rows = 0
    input_unique_rows = 0
    reverse_rows = 0
    shifted_ordinal_rows = 0
    complete_rows = 0
    grouped: dict[tuple[str, str], list[dict[str, str]]] = defaultdict(list)
    for row in source_rows:
        grouped[(row.get("model_archive", ""), row.get("model_object", ""))].append(row)
        slots = _parse_strides(row.get("fetch_slot_strides", ""))
        keys = _parse_keys(row.get("record_keys", ""))
        primitive = int(row.get("primitive", "0"))
        input_unique = row.get("unique_opaque_sequence") == "True"
        input_unique_rows += int(input_unique)
        complete = len(slots) == 5
        complete_rows += int(complete)
        categories = [item[0] for item in keys]
        strides = [item[2] for item in keys]
        ordinals = [item[1] for item in keys]
        categories_consecutive = len(categories) == 5 and categories == list(
            range(categories[0], categories[0] + 5)
        ) if categories else False
        recomputed_exact = (
            len(slots) == 5
            and len(keys) == 5
            and categories_consecutive
            and all(ordinal == primitive for ordinal in ordinals)
            and strides == slots
        )
        reverse_match = len(slots) == 5 and list(reversed(strides)) == slots
        shifted_ordinal = (
            input_unique
            and len(keys) == 5
            and all(ordinal == primitive + 1 for ordinal in ordinals)
        )
        exact_rows += int(recomputed_exact)
        reverse_rows += int(reverse_match)
        shifted_ordinal_rows += int(shifted_ordinal)
        output_rows.append(
            {
                "model_archive": row.get("model_archive", ""),
                "model_object": row.get("model_object", ""),
                "layer": row.get("layer", ""),
                "primitive": row.get("primitive", ""),
                "recomputed_exact": str(recomputed_exact),
                "reverse_stride_match": str(reverse_match),
                "shifted_ordinal_match": str(shifted_ordinal),
                "input_unique_opaque_sequence": str(input_unique),
                "status": STATUS if recomputed_exact else "control-mismatch",
                "original_semantics": ORIGINAL_SEMANTICS,
            }
        )

    object_rows = len(grouped)
    summary = {
        "source_report": "",
        "input_rows": str(len(source_rows)),
        "object_rows": str(object_rows),
        "input_unique_rows": str(input_unique_rows),
        "recomputed_exact_rows": str(exact_rows),
        "reverse_stride_match_rows": str(reverse_rows),
        "shifted_ordinal_match_rows": str(shifted_ordinal_rows),
        "complete_five_slot_rows": str(complete_rows),
        "status": (
            STATUS
            if source_rows
            and exact_rows == input_unique_rows
            and reverse_rows == 0
            and shifted_ordinal_rows == 0
            else "inconclusive-independent-original-opaque-vertex-record-association-control"
        ),
        "provenance": PROVENANCE,
        "original_semantics": ORIGINAL_SEMANTICS,
    }
    return output_rows, summary


def write_report(path: Path, rows: Iterable[dict[str, str]]) -> None:
    rows = list(rows)
    path.parent.mkdir(parents=True, exist_ok=True)
    fields = [
        "model_archive",
        "model_object",
        "layer",
        "primitive",
        "recomputed_exact",
        "reverse_stride_match",
        "shifted_ordinal_match",
        "input_unique_opaque_sequence",
        "status",
        "original_semantics",
    ]
    with path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=fields, delimiter="\t", lineterminator="\n")
        writer.writeheader()
        writer.writerows(rows)


def write_summary(path: Path, summary: dict[str, str]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    fields = [
        "source_report",
        "input_rows",
        "object_rows",
        "input_unique_rows",
        "recomputed_exact_rows",
        "reverse_stride_match_rows",
        "shifted_ordinal_match_rows",
        "complete_five_slot_rows",
        "status",
        "provenance",
        "original_semantics",
    ]
    with path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=fields, delimiter="\t", lineterminator="\n")
        writer.writeheader()
        writer.writerow({field: summary.get(field, "") for field in fields})


def main() -> int:
    import argparse

    parser = argparse.ArgumentParser()
    parser.add_argument("source_report", type=Path)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()
    with args.source_report.open(encoding="utf-8", newline="") as handle:
        rows, summary = analyze_rows(csv.DictReader(handle, delimiter="\t"))
    summary["source_report"] = str(args.source_report)
    write_report(args.report, rows)
    write_summary(args.summary, summary)
    print("FIFA Street 2 independent original opaque __VB association control")
    for key, value in summary.items():
        print(f"{key}: {value}")
    print(f"report: {args.report}")
    print(f"summary: {args.summary}")
    return 0 if summary["status"] == STATUS else 1


if __name__ == "__main__":
    raise SystemExit(main())
