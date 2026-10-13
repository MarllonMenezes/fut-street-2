"""Inventory bytes reached by XBE-to-nested-BIG textual matches.

This is a read-only provenance audit.  It follows the exact nested paths from
``xbe-asset-nested-literal-audit.tsv``, records payload size/hash and bounded
container headers, and does not execute the XBE or assign loader, mesh,
material, UV, audio, event, animation, rigging, skinning, morph, or GPU
semantics.  The source ISO is never written.
"""

from __future__ import annotations

import argparse
import csv
import hashlib
from collections import Counter
from pathlib import Path

from list_xdvdfs import XdvdfsReader
from xbe_asset_nested_literal_audit import (
    decode_refpack,
    expand_nested_archive,
    parse_big_index,
)


REPORT_FIELDS = (
    "literal",
    "literal_category",
    "match_kind",
    "nested_path",
    "container_depth",
    "payload_size",
    "payload_sha256",
    "raw_header_hex",
    "payload_format",
    "expanded_size",
    "expanded_header_hex",
    "status",
    "original_semantics",
)

SUMMARY_FIELDS = (
    "image",
    "internal_path",
    "xbe_sha256",
    "matched_literal_rows",
    "inventory_payload_rows",
    "unique_payload_hashes",
    "unique_nested_paths",
    "formats",
    "refpack_wrapped_rows",
    "elf_rows",
    "status",
    "provenance",
    "original_semantics",
)


def _format_from_header(payload: bytes) -> str:
    if payload.startswith(b"BIG4") or payload.startswith(b"BIGF"):
        return "big-index"
    if payload.startswith(b"\x7fELF"):
        return "elf32-little-endian-candidate"
    if payload.startswith(b"BILG"):
        return "bilg-candidate"
    if payload.startswith(b"SHPX") or payload.startswith(b"G358") or payload.startswith(b"G359"):
        return "shpx-candidate"
    if payload.startswith(b"\x00.symtab") or b".strtab" in payload[:32]:
        return "elf-companion-sidecar-candidate"
    printable = sum(32 <= value <= 126 or value in (9, 10, 13) for value in payload[:256])
    if payload and printable >= min(64, len(payload[:256]) * 3 // 4):
        return "printable-payload-candidate"
    return "opaque-payload"


def _bounded_hex(payload: bytes, count: int = 16) -> str:
    return payload[:count].hex()


def read_tsv(path: Path) -> list[dict[str, str]]:
    with path.open("r", encoding="utf-8", newline="") as handle:
        return list(csv.DictReader(handle, delimiter="\t"))


def _split_nested_path(path: str) -> list[str]:
    parts = [part for part in path.split("!") if part]
    if len(parts) < 2:
        raise ValueError(f"nested path has no leaf entry: {path}")
    return parts


def _read_top_archive(reader: XdvdfsReader, path: str) -> bytes:
    entries = {entry.path.casefold(): entry for entry in reader.entries()}
    entry = entries.get(path.casefold())
    if entry is None or entry.is_directory or entry.image_offset is None:
        raise FileNotFoundError(path)
    return reader._read_at(entry.image_offset, entry.size)


def read_nested_payload(reader: XdvdfsReader, nested_path: str) -> bytes:
    parts = _split_nested_path(nested_path)
    archive_data = _read_top_archive(reader, parts[0])
    for index, name in enumerate(parts[1:]):
        entries = parse_big_index(archive_data)
        entry = next((candidate for candidate in entries if str(candidate["name"]).casefold() == name.casefold()), None)
        if entry is None:
            raise FileNotFoundError(nested_path)
        payload = archive_data[int(entry["offset"]) : int(entry["offset"]) + int(entry["size"])]
        if index == len(parts[1:]) - 1:
            return payload
        archive_data = expand_nested_archive(payload)
    raise AssertionError("nested path traversal did not return a payload")


def inspect_payload(reader: XdvdfsReader, nested_path: str) -> dict[str, str]:
    payload = read_nested_payload(reader, nested_path)
    payload_format = _format_from_header(payload)
    expanded_size = ""
    expanded_header = ""
    if payload[:2] in (b"\x10\xfb", b"\x90\xfb"):
        expanded = decode_refpack(payload)
        expanded_size = str(len(expanded))
        expanded_header = _bounded_hex(expanded)
        payload_format = f"refpack-wrapped-{_format_from_header(expanded)}"
    return {
        "container_depth": str(nested_path.count("!")),
        "payload_size": str(len(payload)),
        "payload_sha256": hashlib.sha256(payload).hexdigest(),
        "raw_header_hex": _bounded_hex(payload),
        "payload_format": payload_format,
        "expanded_size": expanded_size,
        "expanded_header_hex": expanded_header,
        "status": "verified-nested-payload-inventory",
        "original_semantics": "not-claimed",
    }


def build_rows(image: Path, nested_rows: list[dict[str, str]]) -> list[dict[str, str]]:
    output: list[dict[str, str]] = []
    reader = XdvdfsReader(image)
    try:
        for row in nested_rows:
            matches = [value for value in row.get("matched_nested_entries", "").split("|") if value]
            for nested_path in matches:
                try:
                    details = inspect_payload(reader, nested_path)
                except (ValueError, FileNotFoundError, IndexError) as exc:
                    details = {
                        "container_depth": str(nested_path.count("!")),
                        "payload_size": "",
                        "payload_sha256": "",
                        "raw_header_hex": "",
                        "payload_format": f"rejected:{type(exc).__name__}",
                        "expanded_size": "",
                        "expanded_header_hex": "",
                        "status": "rejected-nested-payload-inventory",
                        "original_semantics": "not-claimed",
                    }
                output.append(
                    {
                        "literal": row.get("literal", ""),
                        "literal_category": row.get("literal_category", ""),
                        "match_kind": row.get("match_kind", ""),
                        "nested_path": nested_path,
                        **details,
                    }
                )
    finally:
        reader.close()
    return sorted(output, key=lambda row: (row["literal"].casefold(), row["nested_path"].casefold()))


def build_summary(image: Path, nested_rows: list[dict[str, str]], payload_rows: list[dict[str, str]]) -> dict[str, str]:
    first = next(iter(nested_rows), {})
    format_counts = Counter(row["payload_format"] for row in payload_rows)
    formats = "|".join(f"{name}={count}" for name, count in sorted(format_counts.items()))
    valid_rows = [row for row in payload_rows if row["payload_sha256"]]
    return {
        "image": str(image),
        "internal_path": first.get("internal_path", ""),
        "xbe_sha256": first.get("xbe_sha256", ""),
        "matched_literal_rows": str(sum(bool(row.get("matched_nested_entries", "")) for row in nested_rows)),
        "inventory_payload_rows": str(len(payload_rows)),
        "unique_payload_hashes": str(len({row["payload_sha256"] for row in valid_rows})),
        "unique_nested_paths": str(len({row["nested_path"] for row in payload_rows})),
        "formats": formats,
        "refpack_wrapped_rows": str(sum(row["payload_format"].startswith("refpack-wrapped-") for row in payload_rows)),
        "elf_rows": str(sum(row["payload_format"] == "elf32-little-endian-candidate" for row in payload_rows)),
        "status": "verified-nested-payload-inventory",
        "provenance": "same-ISO-nested-BIG-index+bounded-RefPack+payload-hash; structural-only",
        "original_semantics": "not-claimed",
    }


def write_tsv(path: Path, fields: tuple[str, ...], rows: list[dict[str, str]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=fields, delimiter="\t", lineterminator="\n")
        writer.writeheader()
        writer.writerows({field: row.get(field, "") for field in fields} for row in rows)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("image", type=Path)
    parser.add_argument("nested_report", type=Path)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()
    nested_rows = read_tsv(args.nested_report)
    payload_rows = build_rows(args.image, nested_rows)
    summary = build_summary(args.image, nested_rows, payload_rows)
    write_tsv(args.report, REPORT_FIELDS, payload_rows)
    write_tsv(args.summary, SUMMARY_FIELDS, [summary])
    print(
        f"inventory_payload_rows: {summary['inventory_payload_rows']}; "
        f"unique_payload_hashes: {summary['unique_payload_hashes']}; "
        f"formats: {summary['formats']}; status: {summary['status']}"
    )
    print(f"report: {args.report}")
    print(f"summary: {args.summary}")
    print("original_semantics: not-claimed")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
