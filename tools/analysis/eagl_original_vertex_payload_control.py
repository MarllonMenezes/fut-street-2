"""Independent TSV reparse/control for original numeric __VB payload evidence."""

from __future__ import annotations

import csv
from collections import defaultdict
from pathlib import Path
from typing import Iterable


STATUS = "verified-independent-original-numeric-payload-control"
ORIGINAL_SEMANTICS = "not-claimed"
PROVENANCE = (
    "independent-TSV-reparse+numeric-signature-count-coherence;"
    "original-payload-structure-only;no-semantic-promotion"
)


def _integer(row: dict[str, str], field: str) -> int:
    return int(row.get(field, "0"))


def _numeric_signature(row: dict[str, str]) -> dict[str, int] | None:
    fields: dict[str, int] = {}
    for item in row.get("numeric_signature", "").split(";"):
        if "=" not in item:
            continue
        key, value = item.split("=", 1)
        if key in {"streams", "finite_packed", "bounds_matching_stride12"}:
            try:
                fields[key] = int(value)
            except ValueError:
                return None
    return fields if len(fields) == 3 else None


def analyze_rows(rows: Iterable[dict[str, str]]) -> tuple[list[dict[str, str]], dict[str, str]]:
    source_rows = list(rows)
    grouped: dict[tuple[str, str], list[dict[str, str]]] = defaultdict(list)
    output_rows: list[dict[str, str]] = []
    signature_matches = 0
    range_metadata_valid = 0
    status_rows = 0
    for row in source_rows:
        grouped[(row.get("model_archive", ""), row.get("model_object", ""))].append(row)
        streams = _integer(row, "validated_stream_count")
        finite_packed = _integer(row, "finite_packed_float_profile_count")
        finite_float2 = _integer(row, "finite_float2_profile_count")
        near_unit_float2 = _integer(row, "near_unit_range_float2_profile_count")
        finite_float3 = _integer(row, "finite_float3_profile_count")
        bounds_containment = _integer(row, "declared_bounds_containment_float3_count")
        distinct_ranges = _integer(row, "distinct_payload_range_count")
        overlap_pairs = _integer(row, "overlap_payload_pair_count")
        signature = _numeric_signature(row)
        signature_match = bool(
            signature
            and signature["streams"] == streams
            and signature["finite_packed"] == finite_packed
            and signature["bounds_matching_stride12"] == bounds_containment
        )
        metadata_valid = bool(
            streams > 0
            and 0 <= finite_packed <= streams
            and 0 <= finite_float2 <= streams
            and 0 <= near_unit_float2 <= finite_float2
            and 0 <= finite_float3 <= streams
            and 0 <= bounds_containment <= finite_float3
            and 0 < distinct_ranges <= streams
            and overlap_pairs >= 0
        )
        status_valid = row.get("status") == "verified-bounded-original-numeric-payload-structure"
        signature_matches += int(signature_match)
        range_metadata_valid += int(metadata_valid)
        status_rows += int(status_valid)
        output_rows.append(
            {
                "model_archive": row.get("model_archive", ""),
                "model_object": row.get("model_object", ""),
                "validated_stream_count": str(streams),
                "numeric_signature_match": str(signature_match),
                "payload_range_metadata_valid": str(metadata_valid),
                "input_status_valid": str(status_valid),
                "status": STATUS if signature_match and metadata_valid and status_valid else "control-mismatch",
                "original_semantics": ORIGINAL_SEMANTICS,
            }
        )

    input_rows = len(source_rows)
    object_rows = len(grouped)
    summary = {
        "source_report": "",
        "input_rows": str(input_rows),
        "object_rows": str(object_rows),
        "numeric_signature_match_rows": str(signature_matches),
        "payload_range_metadata_valid_rows": str(range_metadata_valid),
        "input_status_valid_rows": str(status_rows),
        "status": (
            STATUS
            if input_rows > 0
            and object_rows > 0
            and signature_matches == input_rows
            and range_metadata_valid == input_rows
            and status_rows == input_rows
            else "inconclusive-independent-original-numeric-payload-control"
        ),
        "provenance": PROVENANCE,
        "original_semantics": ORIGINAL_SEMANTICS,
    }
    return output_rows, summary


def write_report(path: Path, rows: Iterable[dict[str, str]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    fields = [
        "model_archive",
        "model_object",
        "validated_stream_count",
        "numeric_signature_match",
        "payload_range_metadata_valid",
        "input_status_valid",
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
        "numeric_signature_match_rows",
        "payload_range_metadata_valid_rows",
        "input_status_valid_rows",
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
    print("FIFA Street 2 independent original numeric __VB payload control")
    for key, value in summary.items():
        print(f"{key}: {value}")
    print(f"report: {args.report}")
    print(f"summary: {args.summary}")
    return 0 if summary["status"] == STATUS else 1


if __name__ == "__main__":
    raise SystemExit(main())
