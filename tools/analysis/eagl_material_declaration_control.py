"""Fresh ISO/material decode and static consumer checks; no XBE execution.

The observed declaration fields and VS envelope are bounded data formats.
They do not prove attribute semantics, parent inheritance or shader behavior.
"""
from __future__ import annotations

import argparse
import hashlib
import struct
from pathlib import Path

from eagl_material_parent_probe import load_material, PAYLOAD_HASH, SOURCE, TARGETS
from eagl_indexed_draw_source_control import elf_sections, elf_relocations, slice_data
from eagl_original_render_command_dispatch_control import EXPECTED_XBE_SHA256, EXPECTED_XBE_SIZE, _write_tsv
from eagl_render_parameter_static_control import check_slice
from xbe_render_cfg_audit import load_xbe, read_tsv
from parse_xbe import analyze_xbe, virtual_to_file_offset

STATUS = "verified-independent-material-declaration-and-vs-envelope-control"
STATIC = {
    "material-create-from-parent-fields": (0x1970E0, 0x197195, {
        0x1970F3: "mov eax, dword ptr [esi + 4]", 0x197105: "mov dword ptr [esi + 0x10], eax",
        0x19711E: "mov dword ptr [esi + 0x18], eax", 0x197141: "mov edx, dword ptr [esi + 0xc]",
        0x197144: "mov ecx, dword ptr [edx + edi*4]", 0x197147: "mov edx, dword ptr [esi + 8]",
        0x19714A: "push ecx", 0x19714B: "push edx", 0x19714E: "call 0x1a3b30",
        0x19715E: "mov dword ptr [ecx + edi*4], eax"}),
    "vertex-create-wrapper": (0x1A3B30, 0x1A3B4B, {
        0x1A3B30: "mov eax, dword ptr [esp + 8]", 0x1A3B35: "push 0",
        0x1A3B39: "mov ecx, dword ptr [esp + 0xc]", 0x1A3B3D: "push esi",
        0x1A3B3E: "push eax", 0x1A3B3F: "push ecx", 0x1A3B40: "call 0x292080"}),
    "declaration-token-consumer": (0x291DE0, 0x291F79, {
        0x291DF3: "mov eax, dword ptr [ebx]", 0x291DF7: "and ecx, 0xe0000000",
        0x291E1A: "cmp ecx, 0x20000000", 0x291E20: "je 0x291f05",
        0x291E26: "test eax, 0x10000000", 0x291E43: "movzx esi, byte ptr [esp + 0x1a]",
        0x291E4C: "and eax, 0x1f", 0x291E56: "mov dword ptr [ecx + 0x18], ebp",
        0x291E59: "mov dword ptr [ecx + 0x14], edx", 0x291E5C: "mov dword ptr [ecx + 0x1c], esi",
        0x291EE4: "shr edx, 4", 0x291EE7: "and edx, 0xf",
        0x291EEA: "movzx eax, byte ptr [edx + 0x29c768]", 0x291EF1: "and esi, 0xf",
        0x291EF4: "movzx ecx, byte ptr [esi + 0x29c760]", 0x291EFB: "imul eax, ecx",
        0x291EFE: "add ebp, eax", 0x291F05: "and eax, 0xf", 0x291F0C: "xor ebp, ebp"}),
    "vertex-envelope-and-declaration-create": (0x292080, 0x2921BE, {
        0x29208C: "movzx eax, word ptr [ebp]", 0x292090: "movzx ecx, word ptr [ebp + 2]",
        0x2920A6: "shl eax, 4", 0x292126: "test ebp, ebp", 0x292128: "sete dl",
        0x29212C: "push edi", 0x29212D: "push esi", 0x29212E: "call 0x291de0",
        0x292143: "cmp ecx, 0x2078", 0x29215E: "or dword ptr [esi + 4], 8",
        0x292169: "shl eax, 2", 0x29216D: "add ebp, 4", 0x292170: "push ebp",
        0x292178: "call 0x291d70", 0x2921AF: "or esi, 1"}),
}


def decoded_source(data, tables):
    if struct.unpack_from("<HH", data, 16) != (1, 8): raise ValueError("material ELF type/machine changed")
    sections = elf_sections(data)
    relocs = elf_relocations(data, sections)
    parent = TARGETS["__RM:::ParentRM_FifastCharacterSkin"]
    def word(at): return struct.unpack("<I", slice_data(data, sections, 1, at, 4))[0]
    def local(at):
        if relocs[(1, at)] != [(2, "")]: raise ValueError("parent local pointer changed")
        return word(at)
    declaration = local(parent + 8)
    program_table = local(parent + 12)
    if word(parent + 4) != 1 or relocs[(1, program_table)] != [(2, "FifastCharacterSkin0__EAGLMicroCodeVS")] or word(program_table):
        raise ValueError("named VS parent link changed")
    stream = None
    offset = 0
    elements = []
    for index in range(256):
        at = declaration + index * 4
        token = word(at)
        if relocs.get((1, at)): raise ValueError("relocation in declaration")
        if token == 0xFFFFFFFF:
            break
        family = token >> 29
        if family == 1:
            if token & 0x1FFFFFF0: raise ValueError("unsupported stream bits")
            stream, offset = token & 15, 0
        elif family == 2:
            if stream is None or token & 0x1F00FFE0: raise ValueError("unsupported register bits")
            register = token & 31
            fmt = (token >> 16) & 255
            if register > 15 or fmt not in (0x32, 0x22, 0x40, 0x15): raise ValueError("unsupported field")
            width = tables[fmt & 15] * tables[8 + (fmt >> 4)]
            elements.append((index, token, stream, register, fmt, offset, width))
            offset += width
        else: raise ValueError("unsupported token family")
    else: raise ValueError("no bounded terminator")
    if len(elements) != 5 or len({e[3] for e in elements}) != 5: raise ValueError("declaration population changed")
    declaration_bytes = (index + 1) * 4
    program = TARGETS["FifastCharacterSkin0__EAGLMicroCodeVS"]
    version, count = struct.unpack("<HH", slice_data(data, sections, 1, program, 4))
    if version != 0x2078 or count != 46: raise ValueError("vertex envelope changed")
    program_bytes = 4 + count * 16
    payload = slice_data(data, sections, 1, program, program_bytes)
    if any(s == 1 and at < program + program_bytes and at + 4 > program for s, at in relocs):
        raise ValueError("relocation in vertex program")
    common = {
        "source_path": SOURCE, "source_sha256": PAYLOAD_HASH, "parent_symbol": "__RM:::ParentRM_FifastCharacterSkin",
        "section": "1", "parent_offset": f"0x{parent:08X}", "declaration_offset": f"0x{declaration:08X}",
        "declaration_bytes": str(declaration_bytes),
        "declaration_sha256": hashlib.sha256(slice_data(data, sections, 1, declaration, declaration_bytes)).hexdigest().upper(),
        "vertex_program_offset": f"0x{program:08X}", "vertex_program_version": f"0x{version:08X}",
        "vertex_instruction_count": str(count), "vertex_program_bytes": str(program_bytes),
        "vertex_program_sha256": hashlib.sha256(payload).hexdigest().upper(),
        "status": "verified-bounded-material-declaration-and-vs-envelope", "attribute_semantics": "not-proven",
        "parent_inheritance_binding": "not-proven", "original_semantics": "not-claimed"}
    expected = []
    for i, token, stream, register, fmt, offset, width in elements:
        expected.append(dict(common, token_index=str(i), token=f"0x{token:08X}", stream=str(stream), register=str(register),
            raw_format=f"0x{fmt:08X}", byte_offset=str(offset), element_bytes=str(width)))
    return expected


def verify_rows(rows, expected):
    if rows != expected: raise ValueError("managed report differs from fresh independent material decode")


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("image", type=Path)
    p.add_argument("declaration", type=Path)
    p.add_argument("--report", type=Path, required=True)
    p.add_argument("--summary", type=Path, required=True)
    args = p.parse_args()
    paths = [a.resolve() for a in (args.image, args.declaration, args.report, args.summary)]
    if len(set(paths)) != len(paths): raise ValueError("inputs/outputs must differ")
    xbe = load_xbe(args.image, "default.xbe")
    if len(xbe) != EXPECTED_XBE_SIZE or hashlib.sha256(xbe).hexdigest().upper() != EXPECTED_XBE_SHA256:
        raise ValueError("XBE identity changed")
    meta = analyze_xbe(args.image, "default.xbe")
    proofs = [check_slice(xbe, meta, label, *contract) for label, contract in STATIC.items()]
    table_at = virtual_to_file_offset(0x29C760, meta["fields"]["base_address"], len(xbe), meta["sections"])
    tables = xbe[table_at:table_at + 16]
    if tables.hex() != "01020400010204000001020304000003": raise ValueError("size tables changed")
    expected = decoded_source(load_material(args.image), tables)
    rows = read_tsv(args.declaration)
    verify_rows(rows, expected)
    def reject(action):
        try: action()
        except ValueError: return
        raise AssertionError("negative control accepted changed evidence")
    for field, value in [("stream", "15"), ("register", "12"), ("element_bytes", "16"),
        ("vertex_program_bytes", "4"), ("parent_inheritance_binding", "proven"), ("source_sha256", "0" * 64)]:
        altered = [dict(r) for r in rows]
        altered[0][field] = value
        reject(lambda: verify_rows(altered, expected))
    changed = bytearray(xbe)
    at = virtual_to_file_offset(0x29212E, meta["fields"]["base_address"], len(xbe), meta["sections"])
    changed[at] = 0x90
    reject(lambda: check_slice(changed, meta, "changed-consumer", *STATIC["vertex-envelope-and-declaration-create"]))
    _write_tsv(args.report, proofs[0].keys(), proofs)
    summary = {"source_path": SOURCE, "source_sha256": PAYLOAD_HASH, "xbe_sha256": EXPECTED_XBE_SHA256,
        "declaration_elements": 5, "declaration_bytes": 44, "vertex_instruction_count": 46, "vertex_program_bytes": 740,
        "static_spans": len(proofs), "static_sites": sum(int(p["checked_sites"]) for p in proofs), "negative_controls": 7,
        "size_tables_address": "0x0029C760", "size_tables_sha256": hashlib.sha256(tables).hexdigest().upper(),
        "declaration_report_sha256": hashlib.sha256(args.declaration.read_bytes()).hexdigest().upper(),
        "static_report_sha256": hashlib.sha256(args.report.read_bytes()).hexdigest().upper(),
        "status": STATUS, "attribute_semantics": "not-proven", "parent_inheritance_binding": "not-proven",
        "full_rendering_binding": "not-proven", "original_semantics": "not-claimed"}
    _write_tsv(args.summary, summary.keys(), [summary])
    for key, value in summary.items(): print(f"{key}: {value}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
