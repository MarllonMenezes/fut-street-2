"""Read-only, bounded probe of the newly located material parent ELF.

Symbol names and raw windows are retained as evidence. This is not a shader
decoder, linker, XBE runner, GPU simulator or proof of vertex attributes.
"""
from __future__ import annotations

import argparse
import hashlib
import struct
from pathlib import Path

from list_xdvdfs import XdvdfsReader
from xbe_asset_nested_literal_audit import parse_big_index, expand_nested_archive
from eagl_indexed_draw_source_control import elf_sections, elf_relocations, slice_data
from eagl_original_render_command_dispatch_control import _write_tsv

PAYLOAD_HASH = "75DBC11557C6F3E4A32D6D88372476398B3A4C435E3907C2902E38579FB29430"
SOURCE = "data/initstaz.big!initstay.big!mat.o"
TARGETS = {
    "__RM:::ParentRM_FifastCharacterSkin": 0x1458,
    "FifastCharacterSkin0__EAGLMicroCodeVS": 0x1490,
    "FifastCharacterSkin0__EAGLMicroCodePS": 0x1780,
}


def load_material(image):
    reader = XdvdfsReader(image)
    try:
        root = next(e for e in reader.entries() if e.path == "data/initstaz.big")
        top = reader._read_at(root.image_offset, root.size)
        entry = next(e for e in parse_big_index(top) if e["name"] == "initstay.big")
        nested = expand_nested_archive(top[entry["offset"]:entry["offset"] + entry["size"]])
        entry = next(e for e in parse_big_index(nested) if e["name"] == "mat.o")
        data = nested[entry["offset"]:entry["offset"] + entry["size"]]
    finally:
        reader.close()
    if len(data) != 38970 or hashlib.sha256(data).hexdigest().upper() != PAYLOAD_HASH:
        raise ValueError("material parent payload identity changed")
    return data


def probe(image):
    data = load_material(image)
    sections = elf_sections(data)
    relocations = elf_relocations(data, sections)
    symbols = []
    for s in sections:
        if s[1] != 2:
            continue
        if s[9] != 16 or s[5] % 16 or s[6] >= len(sections):
            raise ValueError("unsupported symbol table")
        strings = sections[s[6]]
        for at in range(s[4], s[4] + s[5], 16):
            name, value, size, info, other, section = struct.unpack_from("<IIIBBH", data, at)
            if name >= strings[5]:
                raise ValueError("symbol string outside table")
            end = data.find(b"\x00", strings[4] + name, strings[4] + strings[5])
            if end < 0:
                raise ValueError("unterminated symbol name")
            text = data[strings[4] + name:end].decode("utf-8")
            if text in TARGETS:
                symbols.append((text, value, size, section))
    if len(symbols) != 3 or {s[0] for s in symbols} != set(TARGETS):
        raise ValueError("material parent targets missing or duplicated")
    rows = []
    for name, value, size, section in sorted(symbols):
        if section != 1 or value != TARGETS[name]:
            raise ValueError("material parent symbol changed")
        window = slice_data(data, sections, section, value, 56)
        rows.append({
            "source_path": SOURCE, "source_sha256": PAYLOAD_HASH, "symbol": name,
            "section": section, "symbol_offset": f"0x{value:08X}", "declared_symbol_size": size,
            "bounded_window_bytes": len(window),
            "raw_words": ",".join(f"0x{w[0]:08X}" for w in struct.iter_unpack("<I", window)),
            "window_sha256": hashlib.sha256(window).hexdigest().upper(),
            "window_relocations": "|".join(f"+0x{offset-value:X}:{kind}:{target}"
                for (target_section, offset), values in sorted(relocations.items())
                if target_section == section and value <= offset < value + len(window)
                for kind, target in values),
            "status": "verified-bounded-material-parent-symbol-probe",
            "shader_layout_binding": "not-proven", "original_semantics": "not-claimed",
        })
    return rows


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("image", type=Path)
    p.add_argument("--report", type=Path, required=True)
    p.add_argument("--summary", type=Path, required=True)
    args = p.parse_args()
    if len({p.resolve() for p in (args.image, args.report, args.summary)}) != 3:
        raise ValueError("input image and reports must differ")
    rows = probe(args.image)
    _write_tsv(args.report, rows[0].keys(), rows)
    summary = {"source_path": SOURCE, "source_bytes": 38970, "source_sha256": PAYLOAD_HASH,
               "symbol_rows": len(rows), "bounded_window_bytes": sum(r["bounded_window_bytes"] for r in rows),
               "report_sha256": hashlib.sha256(args.report.read_bytes()).hexdigest().upper(),
               "status": "verified-bounded-material-parent-symbol-probe",
               "shader_layout_binding": "not-proven", "original_semantics": "not-claimed"}
    _write_tsv(args.summary, summary.keys(), [summary])
    for key, value in summary.items():
        print(f"{key}: {value}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
