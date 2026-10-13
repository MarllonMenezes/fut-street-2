"""Verify managed indexed sources against fresh read-only ISO payloads.

The XBE is disassembled, never executed. Original nested BIG/RefPack objects
are read independently of the managed decoder; no output C/runtime is used.
Only descriptor-local count/pointer/submission links are accepted.
"""
from __future__ import annotations

import argparse
import hashlib
import struct
from collections import defaultdict
from pathlib import Path

from list_xdvdfs import XdvdfsReader
from parse_xbe import analyze_xbe
from xbe_asset_nested_literal_audit import parse_big_index, parse_big_index_from_reader, expand_nested_archive
from xbe_render_cfg_audit import load_xbe, read_tsv
from eagl_original_render_command_dispatch_control import EXPECTED_XBE_SHA256, _write_tsv
from eagl_render_parameter_static_control import check_slice, SLICES

STATUS = "verified-independent-local-indexed-draw-source-control"
VERIFIED = "verified-local-indexed-draw-source"
KEY = ("model_archive", "model_object", "section", "layer", "primitive")
STATIC = {
    "count-address-to-draw-count": (0x1A3E40, 0x1A3E96, {
        0x1A3E46: "mov edx, dword ptr [0x345bcc]", 0x1A3E52: "mov esi, dword ptr [ecx + 4]",
        0x1A3E55: "imul esi, edx", 0x1A3E59: "mov edi, dword ptr [eax + 4]",
        0x1A3E5C: "mov esi, dword ptr [esi + edi]", 0x1A3E5F: "mov dword ptr [0x3458d8], esi"}),
    "index-parameter-address": (0x1A4010, 0x1A4045, {
        0x1A401A: "mov ecx, dword ptr [eax + 4]", 0x1A401D: "imul ecx, dword ptr [0x345bcc]",
        0x1A4024: "mov edx, dword ptr [0x3458d0]", 0x1A402A: "add ecx, dword ptr [edx + 4]",
        0x1A402D: "push ecx", 0x1A402E: "push 0", 0x1A4030: "call 0x1a2a70"}),
    **{name: SLICES[name] for name in ("index-source-store", "indexed-draw-wrapper", "d3d-indexed-packets")},
}


def elf_sections(data):
    if len(data) < 52 or data[:6] != b"\x7fELF\x01\x01":
        raise ValueError("unsupported split ELF")
    offset = struct.unpack_from("<I", data, 32)[0]
    stride, count = struct.unpack_from("<HH", data, 46)
    if stride < 40 or offset + stride * count > len(data):
        raise ValueError("section headers out of bounds")
    sections = [struct.unpack_from("<10I", data, offset + i * stride) for i in range(count)]
    for s in sections:
        if s[1] != 8 and s[4] + s[5] > len(data):
            raise ValueError("file-backed section out of bounds")
    return sections


def slice_data(data, sections, section, offset, count):
    if section <= 0 or section >= len(sections):
        raise ValueError("invalid data section")
    s = sections[section]
    if s[1] == 8 or offset < 0 or count < 0 or offset + count > s[5] or s[4] + offset + count > len(data):
        raise ValueError("local payload out of bounds")
    return data[s[4] + offset:s[4] + offset + count]


def elf_relocations(data, sections):
    result = defaultdict(list)
    for s in sections:
        if s[1] != 9:
            continue
        if s[9] != 8 or s[5] % 8 or s[6] >= len(sections) or s[7] >= len(sections):
            raise ValueError("unsupported relocation table")
        symbols = sections[s[6]]
        if symbols[1] != 2 or symbols[9] != 16 or symbols[6] >= len(sections):
            raise ValueError("unsupported symbol table")
        strings = sections[symbols[6]]
        for at in range(s[4], s[4] + s[5], 8):
            offset, info = struct.unpack_from("<II", data, at)
            symbol = info >> 8
            if symbol * 16 + 16 > symbols[5]:
                raise ValueError("relocation symbol outside table")
            name = struct.unpack_from("<I", data, symbols[4] + symbol * 16)[0]
            if name >= strings[5]:
                raise ValueError("symbol name outside string table")
            start = strings[4] + name
            end = data.find(b"\x00", start, strings[4] + strings[5])
            if end < 0:
                raise ValueError("unterminated relocation symbol")
            result[(s[7], offset)].append((info & 255, data[start:end].decode("utf-8")))
    return result


def verify_row(row, params, raw, companion):
    if row["original_semantics"] != "not-claimed":
        raise ValueError("semantic promotion")
    if row["object_sha256"] != hashlib.sha256(raw).hexdigest().upper():
        raise ValueError("managed object differs from independently extracted ISO bytes")
    if [int(p["command_index"]) for p in params] != list(range(len(params))):
        raise ValueError("command rows reordered or missing")
    before = [p for p in params if int(p["command_index"]) < int(row["draw_command"])]
    draws = [p for p in params if p["command_id"] == "26" and p["command_index"] == row["draw_command"]]
    if len(draws) != 1 or any(p["status"] != "verified-bounded-eagl-command-parameter-layout" for p in before + draws):
        raise ValueError("invalid draw or parameter layout")
    count = next((p for p in reversed(before) if p["command_id"] == "2"), None)
    indices = next((p for p in reversed(before) if p["command_id"] == "14"), None)
    if count is None or indices is None:
        if row["status"] != "unresolved-indexed-draw-source" or row["index_sha256"] or row["index_count"]:
            raise ValueError("inherited/missing source was fabricated")
        return 0
    sections = elf_sections(raw + companion)
    relocations = elf_relocations(raw + companion, sections)
    section = int(row["section"])
    for p, cursor_name, offset_name in ((count, "count_parameter_cursor", "count_offset"),
                                         (indices, "index_parameter_cursor", "index_offset")):
        if (p["parameter_relocations"] != "2:" or int(p["auxiliary"], 0) != 0 or
            row[cursor_name] != p["parameter_cursor"] or row[offset_name] != p["parameter_word1"]):
            raise ValueError("descriptor-local address contract mismatch")
        pair = slice_data(raw, sections, section, int(p["parameter_cursor"], 0), 8)
        if struct.unpack("<II", pair) != (int(p["parameter_word0"], 0), int(p["parameter_word1"], 0)):
            raise ValueError("parameter row differs from original pair")
        if relocations[(section, int(p["parameter_cursor"], 0) + 4)] != [(2, "")]:
            raise ValueError("original pointer lacks unique local relocation")
        command = slice_data(raw, sections, section, int(p["command_offset"], 0), 8)
        if struct.unpack("<II", command) != ((int(p["command_id"]) << 16) | 2, 0):
            raise ValueError("original count/index command differs")
    submission = slice_data(raw, sections, section, int(draws[0]["command_offset"], 0), 12)
    if struct.unpack("<III", submission) != (0x001A0003, int(draws[0]["auxiliary"], 0), int(draws[0]["draw_condition"])) or int(draws[0]["draw_condition"]) == 0:
        raise ValueError("original conditional submission differs")
    actual_count = struct.unpack("<I", slice_data(raw, sections, section, int(row["count_offset"], 0), 4))[0]
    if actual_count > 1_048_576 or int(row["index_count"]) != actual_count or int(row["index_bytes"]) != 2 * actual_count:
        raise ValueError("index count differs from original count target")
    index_offset = int(row["index_offset"], 0)
    if index_offset & 1:
        raise ValueError("unaligned indices")
    payload = slice_data(raw, sections, section, index_offset, actual_count * 2)
    for (target_section, at), _ in relocations.items():
        if target_section == section and (
            at < index_offset + len(payload) and at + 4 > index_offset or
            at < int(row["count_offset"], 0) + 4 and at + 4 > int(row["count_offset"], 0)):
            raise ValueError("count/index payload contains a relocation")
    values = [v[0] for v in struct.iter_unpack("<H", payload)]
    if (row["status"] != VERIFIED or row["index_sha256"] != hashlib.sha256(payload).hexdigest().upper() or
        row["minimum_index"] != (str(min(values)) if values else "") or
        row["maximum_index"] != (str(max(values)) if values else "")):
        raise ValueError("index decoder output differs from original bytes")
    return len(payload)


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("image", type=Path)
    p.add_argument("draws", type=Path)
    p.add_argument("parameters", type=Path)
    p.add_argument("--report", type=Path, required=True)
    p.add_argument("--summary", type=Path, required=True)
    args = p.parse_args()
    paths = [v.resolve() for v in (args.image, args.draws, args.parameters, args.report, args.summary)]
    if len(set(paths)) != len(paths):
        raise ValueError("all input/output paths must differ")
    xbe = load_xbe(args.image, "default.xbe")
    if hashlib.sha256(xbe).hexdigest().upper() != EXPECTED_XBE_SHA256:
        raise ValueError("retail XBE changed")
    meta = analyze_xbe(args.image, "default.xbe")
    proofs = [check_slice(xbe, meta, name, *contract) for name, contract in STATIC.items()]
    draws, parameters = read_tsv(args.draws), read_tsv(args.parameters)
    grouped = defaultdict(list)
    for r in parameters:
        grouped[tuple(r[k] for k in KEY)].append(r)
    if len({tuple(r[k] for k in KEY) + (r["draw_command"],) for r in draws}) != len(draws):
        raise ValueError("duplicate draw")
    expected_draws = {(tuple(r[k] for k in KEY), r["command_index"]) for r in parameters if r["command_id"] == "26"}
    if {(tuple(r[k] for k in KEY), r["draw_command"]) for r in draws} != expected_draws:
        raise ValueError("draw population mismatch")
    by_archive = defaultdict(list)
    for r in draws:
        by_archive[r["model_archive"]].append(r)
    bytes_verified = 0
    sample = None
    reader = XdvdfsReader(args.image)
    try:
        top = next(e for e in reader.entries() if e.path == "data/models.big")
        entries = {e["name"]: e for e in parse_big_index_from_reader(reader, top)}
        for archive, rows in by_archive.items():
            entry = entries[archive]
            nested = expand_nested_archive(reader._read_at(top.image_offset + int(entry["offset"]), int(entry["size"])))
            leaves = {e["name"]: e for e in parse_big_index(nested)}
            def leaf(name):
                e = leaves[name]
                return nested[int(e["offset"]):int(e["offset"]) + int(e["size"])]
            cache = {}
            for row in rows:
                name = row["model_object"]
                if name not in cache:
                    cache[name] = (leaf(name), leaf(str(Path(name).with_suffix(".orl"))))
                raw, companion = cache[name]
                params = grouped[tuple(row[k] for k in KEY)]
                bytes_verified += verify_row(row, params, raw, companion)
                if sample is None and row["status"] == VERIFIED:
                    sample = (row, params, raw, companion)
    finally:
        reader.close()
    # Negative controls operate only on in-memory row copies.
    if sample is None:
        raise ValueError("no local source for negative controls")
    negatives = 0
    for field, value in (("index_count", "4294967295"), ("index_offset", "0xFFFFFFFF"),
                         ("index_sha256", "0" * 64), ("count_parameter_cursor", "0xFFFFFFFF")):
        changed = dict(sample[0], **{field: value})
        try:
            verify_row(changed, *sample[1:])
        except ValueError:
            negatives += 1
        else:
            raise AssertionError("invalid indexed source accepted")
    summary = {
        "draw_rows": len(draws), "verified_local_sources": sum(r["status"] == VERIFIED for r in draws),
        "unresolved_sources": sum(r["status"] == "unresolved-indexed-draw-source" for r in draws),
        "rejected_sources": sum(r["status"] == "rejected-indexed-draw-source" for r in draws),
        "independent_index_bytes": bytes_verified, "static_spans": len(proofs),
        "static_sites": sum(int(r["checked_sites"]) for r in proofs), "negative_controls": negatives,
        "xbe_sha256": EXPECTED_XBE_SHA256,
        "draw_report_sha256": hashlib.sha256(args.draws.read_bytes()).hexdigest().upper(),
        "parameter_report_sha256": hashlib.sha256(args.parameters.read_bytes()).hexdigest().upper(),
        "source_binding": "descriptor-local-count-and-index-pointer-to-command26",
        "vertex_attributes": "not-proven", "runtime_selector": "unresolved",
        "full_rendering_binding": "not-proven", "original_semantics": "not-claimed", "status": STATUS,
    }
    _write_tsv(args.report, proofs[0].keys(), proofs)
    summary["static_report_sha256"] = hashlib.sha256(args.report.read_bytes()).hexdigest().upper()
    _write_tsv(args.summary, summary.keys(), [summary])
    for key, value in summary.items():
        print(f"{key}: {value}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
