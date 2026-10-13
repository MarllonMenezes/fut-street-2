"""Cross-reference XBE asset literals with entries inside read-only BIG archives.

The retail XBE often names an asset that is stored inside a compressed nested
``.big`` entry rather than as an XDVDFS file.  This audit follows only BIG
indexes and the bounded RefPack container needed to reach those indexes.  It
records textual/name evidence; it does not execute the XBE or assign any
literal to a loader, material, UV, audio cue, animation, rig, skinning path,
or GPU command.  The ISO is opened read-only and is never written.
"""

from __future__ import annotations

import argparse
import csv
import struct
from collections import defaultdict
from pathlib import Path

from list_xdvdfs import XdvdfsReader
from parse_xbe import analyze_xbe
from xbe_string_xref_audit import collect_literals, recover
from xbe_asset_literal_audit import is_asset_literal, normalize_path


MAX_REFPACK_OUTPUT = 64 * 1024 * 1024
MAX_NESTED_DEPTH = 8
MAX_NESTED_ARCHIVES = 10_000


def parse_big_index(data: bytes, archive_size: int | None = None) -> list[dict[str, int | str]]:
    """Parse a BIG4/BIGF index without interpreting its payload entries."""

    if len(data) < 16 or data[:4] not in (b"BIG4", b"BIGF"):
        raise ValueError("unsupported BIG header")
    available_size = len(data) if archive_size is None else archive_size
    if available_size < len(data):
        raise ValueError("BIG archive size is smaller than its index bytes")
    file_count = struct.unpack_from(">I", data, 8)[0]
    index_end = struct.unpack_from(">I", data, 12)[0]
    if index_end < 16 or index_end > len(data):
        raise ValueError("BIG index is outside the supplied bytes")

    entries: list[dict[str, int | str]] = []
    cursor = 16
    for index_number in range(file_count):
        if cursor + 8 > index_end:
            raise ValueError(f"BIG entry {index_number} is truncated")
        offset, size = struct.unpack_from(">II", data, cursor)
        name_end = data.find(b"\x00", cursor + 8, index_end)
        if name_end < 0:
            raise ValueError(f"BIG entry {index_number} has no terminator")
        name = data[cursor + 8 : name_end].decode("ascii", "replace")
        if offset < index_end or offset + size > available_size:
            raise ValueError(f"BIG entry {index_number} is outside the archive")
        entries.append({"name": name, "offset": offset, "size": size})
        cursor = name_end + 1
    return entries


def parse_big_index_from_reader(reader: XdvdfsReader, entry) -> list[dict[str, int | str]]:
    if entry.image_offset is None:
        raise ValueError("BIG entry has no image offset")
    header = reader._read_at(entry.image_offset, min(entry.size, 16))
    if len(header) < 16:
        raise ValueError("BIG header is truncated")
    index_end = struct.unpack_from(">I", header, 12)[0]
    return parse_big_index(reader._read_at(entry.image_offset, index_end), entry.size)


def decode_refpack(data: bytes) -> bytes:
    """Decode the bounded RefPack form already validated by the .NET reader."""

    if len(data) < 5 or data[1] != 0xFB or data[0] not in (0x10, 0x90):
        raise ValueError("unsupported RefPack header")
    header_size = 5 if data[0] == 0x10 else 6
    if data[0] == 0x10:
        declared_size = (data[2] << 16) | (data[3] << 8) | data[4]
    else:
        if len(data) < 6:
            raise ValueError("RefPack extended header is truncated")
        declared_size = struct.unpack_from(">I", data, 2)[0]
    if declared_size < 0 or declared_size > MAX_REFPACK_OUTPUT:
        raise ValueError("RefPack output exceeds the audit limit")

    output = bytearray(declared_size)
    source = header_size
    destination = 0
    stopped = False

    def require_input(position: int, count: int) -> None:
        if position < 0 or count < 0 or position > len(data) - count:
            raise ValueError("RefPack command exceeds input")

    def require_output(position: int, count: int) -> None:
        if position < 0 or count < 0 or position > len(output) - count:
            raise ValueError("RefPack command exceeds declared output")

    def copy_literals(count: int) -> None:
        nonlocal source, destination
        require_input(source, count)
        require_output(destination, count)
        output[destination : destination + count] = data[source : source + count]
        source += count
        destination += count

    def copy_history(offset: int, count: int) -> None:
        nonlocal destination
        if offset <= 0 or offset > destination:
            raise ValueError("RefPack back-reference is outside output history")
        require_output(destination, count)
        for _ in range(count):
            output[destination] = output[destination - offset]
            destination += 1

    while source < len(data):
        control = data[source]
        source += 1
        if control <= 0x7F:
            require_input(source, 1)
            b1 = data[source]
            source += 1
            copy_literals(control & 0x03)
            copy_history(((control & 0x60) << 3) + b1 + 1, ((control & 0x1C) >> 2) + 3)
        elif control <= 0xBF:
            require_input(source, 2)
            b1, b2 = data[source : source + 2]
            source += 2
            copy_literals((b1 & 0xC0) >> 6)
            copy_history(((b1 & 0x3F) << 8) + b2 + 1, (control & 0x3F) + 4)
        elif control <= 0xDF:
            require_input(source, 3)
            b1, b2, b3 = data[source : source + 3]
            source += 3
            copy_literals(control & 0x03)
            copy_history(((control & 0x10) << 12) + (b1 << 8) + b2 + 1, ((control & 0x0C) << 6) + b3 + 5)
        elif control <= 0xFB:
            copy_literals(((control & 0x1F) << 2) + 4)
        else:
            copy_literals(control & 0x03)
            stopped = True
            break

    if not stopped or destination != declared_size:
        raise ValueError("RefPack output or stop command is invalid")
    return bytes(output)


def expand_nested_archive(data: bytes) -> bytes:
    if data[:4] in (b"BIG4", b"BIGF"):
        return data
    expanded = decode_refpack(data)
    if expanded[:4] not in (b"BIG4", b"BIGF"):
        raise ValueError("RefPack payload is not a BIG archive")
    return expanded


def collect_nested_entries(image: Path) -> tuple[list[str], dict[str, int]]:
    """Return display paths for valid nested BIG index entries."""

    reader = XdvdfsReader(image)
    paths: list[str] = []
    stats = {
        "top_level_big_archives": 0,
        "nested_archives_seen": 0,
        "nested_archives_validated": 0,
        "nested_archives_rejected": 0,
        "nested_entries": 0,
        "depth_limited_archives": 0,
    }

    def walk(container_path: str, archive_data: bytes, depth: int) -> None:
        try:
            entries = parse_big_index(archive_data)
        except ValueError:
            stats["nested_archives_rejected"] += 1
            return
        stats["nested_archives_validated"] += 1
        for item in entries:
            name = str(item["name"])
            path = f"{container_path}!{name}"
            paths.append(path)
            stats["nested_entries"] += 1
            if not name.casefold().endswith(".big"):
                continue
            stats["nested_archives_seen"] += 1
            if depth >= MAX_NESTED_DEPTH:
                stats["depth_limited_archives"] += 1
                continue
            try:
                child = archive_data[int(item["offset"]) : int(item["offset"]) + int(item["size"])]
                expanded = expand_nested_archive(child)
            except (ValueError, IndexError):
                stats["nested_archives_rejected"] += 1
                continue
            walk(path, expanded, depth + 1)

    try:
        for entry in reader.entries():
            if entry.is_directory or not entry.path.casefold().endswith(".big"):
                continue
            stats["top_level_big_archives"] += 1
            if stats["nested_archives_validated"] >= MAX_NESTED_ARCHIVES:
                break
            try:
                entries = parse_big_index_from_reader(reader, entry)
            except ValueError:
                continue
            stats["nested_archives_validated"] += 1
            for item in entries:
                name = str(item["name"])
                path = f"{entry.path}!{name}"
                paths.append(path)
                stats["nested_entries"] += 1
                if not name.casefold().endswith(".big"):
                    continue
                stats["nested_archives_seen"] += 1
                if stats["nested_archives_validated"] >= MAX_NESTED_ARCHIVES:
                    break
                try:
                    child_offset = entry.image_offset + int(item["offset"])
                    child = reader._read_at(child_offset, int(item["size"]))
                    expanded = expand_nested_archive(child)
                except (ValueError, IndexError):
                    stats["nested_archives_rejected"] += 1
                    continue
                walk(path, expanded, 1)
    finally:
        reader.close()
    return sorted(set(paths), key=str.casefold), stats


def match_literal_to_nested_paths(literal: str, nested_paths: list[str]) -> tuple[str, list[str]]:
    """Match by nested suffix or unique entry basename only."""

    normalized_literal = normalize_path(literal)
    normalized_paths = [(path, normalize_path(path.replace("!", "/"))) for path in nested_paths]
    suffixes = [
        path
        for path, normalized in normalized_paths
        if normalized.endswith(f"/{normalized_literal}")
        or normalized.endswith(f"!{normalized_literal}")
    ]
    if len(suffixes) == 1:
        return "unique-big-entry-suffix", suffixes
    if len(suffixes) > 1:
        return "ambiguous-big-entry-suffix", suffixes

    basename = normalized_literal.rsplit("/", 1)[-1]
    basenames = [
        path
        for path, normalized in normalized_paths
        if normalized.rsplit("/", 1)[-1] == basename
    ]
    if len(basenames) == 1:
        return "unique-big-entry-basename", basenames
    if len(basenames) > 1:
        return "ambiguous-big-entry-basename", basenames
    return "no-big-entry-match", []


def extract_asset_literals(image: Path, internal_path: str) -> list[dict]:
    report = recover(image, internal_path)
    # recover() already records the image and XBE metadata; use its parser
    # sections to keep the literal source identical to the existing audit.
    from xbe_asset_literal_audit import load_xbe_bytes

    literals = collect_literals(
        load_xbe_bytes(report["image"], report["internal_path"]),
        analyze_xbe(Path(report["image"]), report["internal_path"])["sections"],
    )
    grouped: dict[tuple[str, str], list[dict]] = defaultdict(list)
    for literal in literals:
        if is_asset_literal(literal["text"]):
            grouped[(literal["text"].casefold(), literal["category"])].append(literal)
    rows = []
    for occurrences in grouped.values():
        representative = min(occurrences, key=lambda item: (item["virtual_address"], item["file_offset"]))
        rows.append(
            {
                "string_virtual_address": representative["virtual_address"],
                "string_file_offset": representative["file_offset"],
                "literal": representative["text"],
                "literal_category": representative["category"],
                "literal_occurrences": len(occurrences),
            }
        )
    return sorted(rows, key=lambda row: (row["literal"].casefold(), row["string_virtual_address"]))


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
    "match_kind",
    "matched_nested_entries",
    "status",
    "original_semantics",
)


def write_report(image: Path, internal_path: str, rows: list[dict], nested_paths: list[str], output: Path) -> dict[str, int]:
    xbe_report = recover(image, internal_path)
    counts: dict[str, int] = {}
    output.parent.mkdir(parents=True, exist_ok=True)
    with output.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=FIELDS, delimiter="\t", lineterminator="\n")
        writer.writeheader()
        for row in rows:
            kind, matches = match_literal_to_nested_paths(row["literal"], nested_paths)
            counts[kind] = counts.get(kind, 0) + 1
            writer.writerow(
                {
                    "image": str(image),
                    "internal_path": internal_path,
                    "xbe_sha256": xbe_report["xbe_sha256"],
                    "xbe_size": xbe_report["xbe_size"],
                    "string_virtual_address": f"0x{row['string_virtual_address']:08X}",
                    "string_file_offset": f"0x{row['string_file_offset']:X}",
                    "literal": row["literal"],
                    "literal_category": row["literal_category"],
                    "literal_occurrences": row["literal_occurrences"],
                    "match_kind": kind,
                    "matched_nested_entries": "|".join(matches),
                    "status": "candidate-xbe-literal-to-nested-big-entry",
                    "original_semantics": "not-claimed",
                }
            )
    return counts


def write_summary(
    image: Path,
    internal_path: str,
    rows: list[dict],
    nested_paths: list[str],
    stats: dict[str, int],
    counts: dict[str, int],
    output: Path,
) -> None:
    matched = sum(value for kind, value in counts.items() if kind != "no-big-entry-match")
    values = [
        ("image", str(image)),
        ("internal_path", internal_path),
        ("xbe_sha256", recover(image, internal_path)["xbe_sha256"]),
        ("unique_asset_literals", len(rows)),
        ("nested_paths_indexed", len(nested_paths)),
        ("unique_asset_literals_with_any_nested_match", matched),
        ("unmatched_literals", counts.get("no-big-entry-match", 0)),
        ("status", "verified-textual-xbe-nested-big-entry-crosscheck"),
        ("provenance", "retail-XBE-literal+same-ISO-BIG-index+bounded-RefPack; textual-only"),
        ("original_semantics", "not-claimed"),
    ]
    values.extend((f"archive:{key}", value) for key, value in stats.items())
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

    rows = extract_asset_literals(args.image, args.internal_path)
    nested_paths, stats = collect_nested_entries(args.image)
    counts = write_report(args.image, args.internal_path, rows, nested_paths, args.report) if args.report else {
        kind: sum(1 for row in rows if match_literal_to_nested_paths(row["literal"], nested_paths)[0] == kind)
        for kind in {match_literal_to_nested_paths(row["literal"], nested_paths)[0] for row in rows}
    }
    if args.summary:
        write_summary(args.image, args.internal_path, rows, nested_paths, stats, counts, args.summary)
    matched = sum(value for kind, value in counts.items() if kind != "no-big-entry-match")
    print(
        f"asset_literals: {len(rows)}; nested_paths: {len(nested_paths)}; "
        f"nested_matches: {matched}; status: verified-textual-xbe-nested-big-entry-crosscheck"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
