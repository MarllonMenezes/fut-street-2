"""Independently reparse the complete data-universe EVT hash audit.

The single observed hit is retained as a structural fact only.  This control
does not infer that a VP6 payload is an audio asset or that the event name is
selected by the game.
"""

from __future__ import annotations

import argparse
import csv
from pathlib import Path


STATUS = "verified-independent-audio-event-data-asset-hash-universe-control"
PROVENANCE = (
    "independent-TSV-reparse+exact-event-hash-name+full-data-universe-row/"
    "cardinality+single-hit-context;no-selection-promotion"
)
DIRECT_SELECTION_BINDING = "not-proven"
ORIGINAL_SEMANTICS = "not-claimed"
EXPECTED_HIT_PATH = "data/movies/advoffn.vp6"
EXPECTED_HIT_DETAILS = "0x34214F6B:name=3panna_goal:count=1:offsets=0xA58D88"


def read_rows(path: Path) -> list[dict[str, str]]:
    with path.open("r", encoding="utf-8", newline="") as handle:
        return list(csv.DictReader(handle, delimiter="\t"))


def read_summary(path: Path) -> dict[str, str]:
    rows = read_rows(path)
    return {row.get("field", ""): row.get("value", "") for row in rows if row.get("field")}


def integer(summary: dict[str, str], key: str) -> int:
    try:
        return int(summary.get(key, ""))
    except ValueError:
        return -1


def write_rows(path: Path, row: dict[str, object]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    fields = list(row)
    with path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=fields, delimiter="\t", lineterminator="\n")
        writer.writeheader()
        writer.writerow(row)


def write_summary(path: Path, values: dict[str, object]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="") as handle:
        handle.write("field\tvalue\n")
        for key, value in values.items():
            handle.write(f"{key}\t{value}\n")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("report", type=Path)
    parser.add_argument("source_summary", type=Path)
    parser.add_argument("--report", dest="output_report", type=Path, required=True)
    parser.add_argument("--summary", dest="output_summary", type=Path, required=True)
    args = parser.parse_args()

    rows = read_rows(args.report)
    source = read_summary(args.source_summary)
    valid_rows = [row for row in rows if row.get("valid") == "True"]
    hit_rows = [row for row in rows if row.get("status") == "aligned-hash-hit"]
    unique_paths = {row.get("asset_path", "") for row in rows}
    recomputed_bytes = sum(int(row.get("asset_bytes", "0")) for row in valid_rows)
    exact_hit = (
        len(hit_rows) == 1
        and hit_rows[0].get("asset_path") == EXPECTED_HIT_PATH
        and hit_rows[0].get("hash_hit_details") == EXPECTED_HIT_DETAILS
        and hit_rows[0].get("valid") == "True"
    )
    source_valid = (
        source.get("asset_root") == "data"
        and source.get("event_path") == "data/audio/eventspa.evt"
        and source.get("status") == STATUS
        and integer(source, "candidate_count") == 17
        and integer(source, "scanned_asset_count") == 121
        and integer(source, "valid_asset_count") == 121
        and integer(source, "scanned_bytes") == 2_008_984_464
        and integer(source, "excluded_source_rows") == 1
        and integer(source, "assets_with_hits") == 1
        and integer(source, "asset_hash_hit_pairs") == 1
        and integer(source, "aligned_occurrences") == 1
    )
    valid = (
        source_valid
        and len(rows) == 121
        and len(valid_rows) == 121
        and len(unique_paths) == 121
        and recomputed_bytes == integer(source, "scanned_bytes")
        and exact_hit
    )
    hit_hash = "0x34214F6B" if exact_hit else ""
    hit_name = "3panna_goal" if exact_hit else ""
    hit_offset = "0xA58D88" if exact_hit else ""
    control_row = {
        "source_report": str(args.report),
        "source_summary": str(args.source_summary),
        "source_status": source.get("status", ""),
        "asset_root": source.get("asset_root", ""),
        "event_path": source.get("event_path", ""),
        "candidate_count": integer(source, "candidate_count"),
        "scanned_asset_count": integer(source, "scanned_asset_count"),
        "valid_asset_count": integer(source, "valid_asset_count"),
        "rows": len(rows),
        "valid_rows": len(valid_rows),
        "unique_paths": len(unique_paths),
        "scanned_bytes": integer(source, "scanned_bytes"),
        "recomputed_bytes": recomputed_bytes,
        "excluded_source_rows": integer(source, "excluded_source_rows"),
        "assets_with_hits": integer(source, "assets_with_hits"),
        "hit_rows": len(hit_rows),
        "asset_hash_hit_pairs": integer(source, "asset_hash_hit_pairs"),
        "aligned_occurrences": integer(source, "aligned_occurrences"),
        "hit_asset_path": EXPECTED_HIT_PATH if exact_hit else "",
        "hit_hash_word": hit_hash,
        "hit_event_name": hit_name,
        "hit_offset": hit_offset,
        "status": STATUS if valid else "inconclusive-" + STATUS,
        "provenance": PROVENANCE,
        "direct_selection_binding": DIRECT_SELECTION_BINDING,
        "original_semantics": ORIGINAL_SEMANTICS,
    }
    write_rows(args.output_report, control_row)
    write_summary(args.output_summary, control_row)
    print("FIFA Street 2 independent EVT data-universe hash control")
    print(f"rows: {len(rows)}")
    print(f"valid_rows: {len(valid_rows)}")
    print(f"unique_paths: {len(unique_paths)}")
    print(f"recomputed_bytes: {recomputed_bytes}")
    print(f"hit_rows: {len(hit_rows)}")
    print(f"hit_asset_path: {control_row['hit_asset_path']}")
    print(f"hit_hash_word: {hit_hash}")
    print(f"hit_event_name: {hit_name}")
    print(f"hit_offset: {hit_offset}")
    print(f"status: {control_row['status']}")
    print(f"provenance: {PROVENANCE}")
    print(f"direct_selection_binding: {DIRECT_SELECTION_BINDING}")
    print(f"original_semantics: {ORIGINAL_SEMANTICS}")
    print(f"report: {args.output_report}")
    print(f"summary: {args.output_summary}")
    return 0 if valid else 1


if __name__ == "__main__":
    raise SystemExit(main())
