"""Cross-reference XBE asset-path literals with the read-only XDVDFS tree.

This audit records only textual evidence: a printable literal embedded in the
retail ``default.xbe`` is compared with paths present in the same ISO.  It
does not execute the XBE, recover function semantics, or assign a literal to
an asset loader, material, UV, audio cue, animation, rig, skinning path, or
GPU command.  The ISO is opened read-only and is never written.
"""

from __future__ import annotations

import argparse
import csv
import re
from collections import defaultdict
from pathlib import Path

from list_xdvdfs import XdvdfsReader
from xbe_string_xref_audit import collect_literals, recover
from parse_xbe import analyze_xbe


ASSET_LITERAL = re.compile(
    r"(?:\.(?:xsh|shpx|abk|ast|evt|bnk|big|bh|xbo|ord|orl|viv|gld|off|mus|mpf)|"
    r"(?:^|[/\\])(?:data|audio|aptanims|models|stadium|hud)[/\\])",
    re.IGNORECASE,
)


def normalize_path(value: str) -> str:
    """Normalize separators and case for textual path comparison only."""

    return re.sub(r"/+", "/", value.replace("\\", "/").strip("/")).casefold()


def is_asset_literal(value: str) -> bool:
    return bool(ASSET_LITERAL.search(value))


def match_literal_to_paths(literal: str, iso_paths: list[str]) -> tuple[str, list[str]]:
    """Return a conservative match kind and the matching ISO paths.

    A ``data/`` prefix is added only for comparison because retail literals
    sometimes omit the root directory.  Suffix matches are retained only
    when they are unique; ambiguous basenames are never promoted to a path.
    """

    normalized_literal = normalize_path(literal)
    normalized_paths = [(path, normalize_path(path)) for path in iso_paths]
    exact = [path for path, normalized in normalized_paths if normalized == normalized_literal]
    if exact:
        return "exact-xdvdfs-path", exact

    data_prefixed = normalized_literal
    if not data_prefixed.startswith("data/"):
        data_prefixed = f"data/{data_prefixed}"
    prefixed = [path for path, normalized in normalized_paths if normalized == data_prefixed]
    if prefixed:
        return "data-prefix-xdvdfs-path", prefixed

    suffix = f"/{normalized_literal}"
    suffix_matches = [path for path, normalized in normalized_paths if normalized.endswith(suffix)]
    if len(suffix_matches) == 1:
        return "unique-xdvdfs-suffix", suffix_matches
    if len(suffix_matches) > 1:
        return "ambiguous-xdvdfs-suffix", suffix_matches

    basename = normalized_literal.rsplit("/", 1)[-1]
    basename_matches = [
        path
        for path, normalized in normalized_paths
        if normalized.rsplit("/", 1)[-1] == basename
    ]
    if len(basename_matches) == 1:
        return "unique-xdvdfs-basename", basename_matches
    if len(basename_matches) > 1:
        return "ambiguous-xdvdfs-basename", basename_matches
    return "no-xdvdfs-match", []


def build_rows(report: dict, iso_paths: list[str]) -> list[dict]:
    """Build one row per unique interesting XBE literal."""

    literals = [literal for literal in collect_literals(
        load_xbe_bytes(report["image"], report["internal_path"]),
        analyze_xbe(Path(report["image"]), report["internal_path"])["sections"],
    ) if is_asset_literal(literal["text"])]
    xrefs_by_address: dict[int, int] = {}
    for xref in report.get("xrefs", []):
        address = xref["string_virtual_address"]
        xrefs_by_address[address] = xrefs_by_address.get(address, 0) + 1

    grouped: dict[tuple[str, str], list[dict]] = defaultdict(list)
    for literal in literals:
        grouped[(literal["text"].casefold(), literal["category"])].append(literal)

    rows: list[dict] = []
    for occurrences in grouped.values():
        representative = min(
            occurrences,
            key=lambda literal: (literal["virtual_address"], literal["file_offset"]),
        )
        match_kind, matches = match_literal_to_paths(representative["text"], iso_paths)
        rows.append(
            {
                "string_virtual_address": representative["virtual_address"],
                "string_file_offset": representative["file_offset"],
                "literal": representative["text"],
                "literal_category": representative["category"],
                "literal_occurrences": len(occurrences),
                "xref_count": sum(
                    xrefs_by_address.get(literal["virtual_address"], 0)
                    for literal in occurrences
                ),
                "match_kind": match_kind,
                "matched_paths": matches,
                "status": "candidate-xbe-literal-to-xdvdfs-path",
                "original_semantics": "not-claimed",
            }
        )
    return sorted(rows, key=lambda row: (row["literal"].casefold(), row["string_virtual_address"]))


def load_xbe_bytes(image: str | Path, internal_path: str) -> bytes:
    reader = XdvdfsReader(Path(image))
    try:
        entries = {entry.path: entry for entry in reader.entries()}
        entry = entries.get(internal_path)
        if entry is None or entry.is_directory or entry.image_offset is None:
            raise ValueError(f"internal file not found: {internal_path}")
        return reader._read_at(entry.image_offset, entry.size)
    finally:
        reader.close()


def collect_iso_paths(image: Path) -> list[str]:
    reader = XdvdfsReader(image)
    try:
        return sorted(
            entry.path
            for entry in reader.entries()
            if not entry.is_directory
        )
    finally:
        reader.close()


FIELDS = (
    "image",
    "internal_path",
    "xbe_sha256",
    "xbe_size",
    "string_virtual_address",
    "string_file_offset",
    "literal",
    "literal_category",
    "literal_occurrences",
    "xref_count",
    "match_kind",
    "matched_paths",
    "status",
    "original_semantics",
)


def write_report(image: Path, internal_path: str, report: dict, rows: list[dict], output: Path) -> None:
    output.parent.mkdir(parents=True, exist_ok=True)
    with output.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=FIELDS, delimiter="\t", lineterminator="\n")
        writer.writeheader()
        for row in rows:
            writer.writerow(
                {
                    "image": str(image),
                    "internal_path": internal_path,
                    "xbe_sha256": report["xbe_sha256"],
                    "xbe_size": report["xbe_size"],
                    "string_virtual_address": f"0x{row['string_virtual_address']:08X}",
                    "string_file_offset": f"0x{row['string_file_offset']:X}",
                    "literal": row["literal"],
                    "literal_category": row["literal_category"],
                    "literal_occurrences": row["literal_occurrences"],
                    "xref_count": row["xref_count"],
                    "match_kind": row["match_kind"],
                    "matched_paths": "|".join(row["matched_paths"]),
                    "status": row["status"],
                    "original_semantics": row["original_semantics"],
                }
            )


def write_summary(image: Path, internal_path: str, report: dict, rows: list[dict], output: Path) -> None:
    counts: dict[str, int] = {}
    for row in rows:
        counts[row["match_kind"]] = counts.get(row["match_kind"], 0) + 1
    matched = sum(
        count for kind, count in counts.items()
        if kind != "no-xdvdfs-match"
    )
    values = [
        ("image", str(image)),
        ("internal_path", internal_path),
        ("xbe_sha256", report["xbe_sha256"]),
        ("xbe_size", report["xbe_size"]),
        ("unique_asset_literals", len(rows)),
        ("unique_asset_literals_with_any_path_match", matched),
        ("asset_literal_occurrences", sum(row["literal_occurrences"] for row in rows)),
        ("unique_exact_paths", counts.get("exact-xdvdfs-path", 0)),
        ("data_prefix_paths", counts.get("data-prefix-xdvdfs-path", 0)),
        ("unique_suffix_paths", counts.get("unique-xdvdfs-suffix", 0)),
        ("unique_basenames", counts.get("unique-xdvdfs-basename", 0)),
        ("ambiguous_suffixes", counts.get("ambiguous-xdvdfs-suffix", 0)),
        ("ambiguous_basenames", counts.get("ambiguous-xdvdfs-basename", 0)),
        ("unmatched_literals", counts.get("no-xdvdfs-match", 0)),
        ("status", "verified-textual-xbe-path-crosscheck"),
        ("provenance", "retail-XBE-literal+same-ISO-XDVDFS-tree; textual-only"),
        ("original_semantics", "not-claimed"),
    ]
    output.parent.mkdir(parents=True, exist_ok=True)
    with output.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.writer(handle, delimiter="\t", lineterminator="\n")
        writer.writerow(("metric", "value"))
        writer.writerows(values)
        for kind in sorted(counts):
            writer.writerow((f"match_kind:{kind}", counts[kind]))


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("image", type=Path)
    parser.add_argument("--internal-path", default="default.xbe")
    parser.add_argument("--report", type=Path)
    parser.add_argument("--summary", type=Path)
    args = parser.parse_args()

    report = recover(args.image, args.internal_path)
    rows = build_rows(report, collect_iso_paths(args.image))
    if args.report:
        write_report(args.image, args.internal_path, report, rows, args.report)
    if args.summary:
        write_summary(args.image, args.internal_path, report, rows, args.summary)
    matched = sum(row["match_kind"] != "no-xdvdfs-match" for row in rows)
    print(
        f"asset_literals: {len(rows)}; path_matches: {matched}; "
        "status: verified-textual-xbe-path-crosscheck"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
