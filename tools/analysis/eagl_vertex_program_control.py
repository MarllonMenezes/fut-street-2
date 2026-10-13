"""Fresh ISO + packed Kelvin bitfields control; static only, no execution.

Independent representation: a 128-bit integer, DWORD 3 least significant.
Operand activity is by opcode, not component liveness or a draw binding.
"""
from __future__ import annotations

import argparse
import hashlib
import struct
from pathlib import Path

from eagl_material_parent_probe import load_material, probe, SOURCE, PAYLOAD_HASH, TARGETS
from eagl_indexed_draw_source_control import elf_sections, elf_relocations, slice_data
from eagl_material_declaration_control import STATIC as MATERIAL_STATIC
from eagl_render_parameter_static_control import check_slice
from eagl_original_render_command_dispatch_control import EXPECTED_XBE_SHA256, _write_tsv
from xbe_render_cfg_audit import load_xbe, read_tsv
from parse_xbe import analyze_xbe, virtual_to_file_offset

PROGRAM_HASH = "B37951FBB3188E7843A9492EF675B4CBEA9EBA75CAA412942746BF1212CDFB67"
STATUS = "verified-bounded-vertex-program-opcode-operands"
SCOPE = "opcode-active-source-slots-not-component-liveness"
STATIC = dict(MATERIAL_STATIC, **{
    "vertex-program-unmodified-dword-upload": (0x291D70, 0x291DD8, {
        0x291D74: "mov ecx, dword ptr [esp + 0xc]", 0x291D80: "cmp ecx, 0x20",
        0x291D87: "mov ebx, 0x20", 0x291D8C: "mov esi, dword ptr [esp + 0x18]",
        0x291D92: "shl ecx, 0x12", 0x291D95: "add ecx, 0xb00",
        0x291D9B: "mov dword ptr [eax], ecx", 0x291D9D: "lea edx, [ebx*4]",
        0x291DAB: "lea edi, [eax + 4]", 0x291DAE: "rep movsd dword ptr es:[edi], dword ptr [esi]",
        0x291DBF: "add esi, edx", 0x291DC1: "sub ecx, ebx", 0x291DCF: "jne 0x291d80",
        0x291DD5: "ret 0xc"})})
MAC = ("NOP", "MOV", "MUL", "ADD", "MAD", "DP3", "DPH", "DP4", "DST", "MIN", "MAX", "SLT", "SGE", "ARL")
ILU = ("NOP", "MOV", "RCP", "RCC", "RSQ", "EXP", "LOG", "LIT")
# Public hardware contract: ADD's second operand is C, not B.
MAC_SLOTS = ("", "A", "AB", "AC", "ABC", "AB", "AB", "AB", "AB", "AB", "AB", "AB", "AB", "A")


def decode(payload):
    if len(payload) < 4: raise ValueError("truncated envelope")
    version, count = struct.unpack_from("<HH", payload)
    if version != 0x2078 or not 0 < count <= 136 or len(payload) != 4 + 16 * count:
        raise ValueError("invalid envelope")
    rows = []
    for index, words in enumerate(struct.iter_unpack("<IIII", payload[4:])):
        packed = sum(word << (32 * (3 - n)) for n, word in enumerate(words))
        def bits(start, size): return (packed >> start) & ((1 << size) - 1)
        mac, ilu = bits(85, 4), bits(89, 3)
        if words[0] or packed >> 92 or mac >= len(MAC): raise ValueError("unknown reserved bits/opcode")
        if bool(bits(0, 1)) != (index == count - 1): raise ValueError("bad final marker")
        active = set(MAC_SLOTS[mac]) | ({"C"} if ilu else set())
        input_index, constant, relative = bits(73, 4), bits(77, 8), bits(1, 1)
        raw_sources = {name: bits(at, 15) for name, at in zip("ABC", (58, 43, 28))}
        sources, input_slots = {}, ""
        for name, raw in raw_sources.items():
            mux, temp, swizzle = raw & 3, (raw >> 2) & 15, (raw >> 6) & 255
            if name not in active:
                sources[name] = "inactive"
                continue
            if mux == 0 or (mux == 1 and temp > 12) or (mux == 3 and constant > 191):
                raise ValueError("invalid active source")
            register = {1: f"r[{temp}]", 2: f"v[{input_index}]",
                3: f"c[a0.x+{constant}]" if relative else f"c[{constant}]"}[mux]
            sources[name] = ("-" if raw & 0x4000 else "") + register + "." + "".join("xyzw"[(swizzle >> b) & 3] for b in (6, 4, 2, 0))
            if mux == 2: input_slots += name
        mac_mask, ilu_mask, temp = bits(24, 4), bits(16, 4), bits(20, 4)
        out_mask, bank, address, scalar = bits(12, 4), bits(11, 1), bits(3, 8), bits(2, 1)
        if (mac_mask and not mac) or (ilu_mask and not ilu) or ((mac_mask or ilu_mask) and temp > 11):
            raise ValueError("invalid temporary write")
        if out_mask and ((not ilu if scalar else not mac) or (bank and address not in (0, *range(3, 13))) or (not bank and address > 191)):
            raise ValueError("invalid output")
        row = {"source_path": SOURCE, "source_sha256": PAYLOAD_HASH,
            "vertex_program_sha256": hashlib.sha256(payload).hexdigest().upper(), "instruction_index": str(index)}
        row.update({f"word{n}": f"0x{word:08X}" for n, word in enumerate(words)})
        row.update(mac_opcode=str(mac), mac_name=MAC[mac], ilu_opcode=str(ilu), ilu_name=ILU[ilu],
            mac_source_slots=MAC_SLOTS[mac], ilu_source_slots="C" if ilu else "",
            input_index_field=str(input_index), constant_index_field=str(constant), relative_constant="true" if relative else "false")
        row.update({f"{n.lower()}_raw": f"0x{raw:08X}" for n, raw in raw_sources.items()})
        row.update({f"{n.lower()}_operand": source for n, source in sources.items()})
        row.update(active_input_slots=input_slots, mac_temporary_mask=str(mac_mask), ilu_temporary_mask=str(ilu_mask),
            raw_shared_temporary=str(temp), output_mask=str(out_mask), output_bank=str(bank), output_address=str(address),
            output_unit="ILU" if scalar else "MAC", final="true" if bits(0, 1) else "false", status=STATUS,
            operand_scope=SCOPE, parent_inheritance_binding="not-proven", full_rendering_binding="not-proven", original_semantics="not-claimed")
        rows.append(row)
    return rows


def summary(rows, report_hash):
    return {"source_path": SOURCE, "source_sha256": PAYLOAD_HASH, "vertex_program_sha256": PROGRAM_HASH,
        "instruction_count": str(len(rows)),
        "active_input_registers": ",".join(map(str, sorted({int(r["input_index_field"]) for r in rows if r["active_input_slots"]}))),
        "active_input_slot_references": str(sum(len(r["active_input_slots"]) for r in rows)),
        "relative_constant_instructions": str(sum(r["relative_constant"] == "true" and any(r[f"{s}_operand"].lstrip("-").startswith("c[") for s in "abc") for r in rows)),
        "active_output_addresses": ",".join(map(str, sorted({int(r["output_address"]) for r in rows if int(r["output_mask"]) and r["output_bank"] == "1"}))),
        "output_write_instructions": str(sum(int(r["output_mask"]) != 0 for r in rows)),
        "paired_instructions": str(sum(r["mac_opcode"] != "0" and r["ilu_opcode"] != "0" for r in rows)),
        "report_sha256": report_hash, "status": STATUS, "operand_scope": SCOPE,
        "parent_inheritance_binding": "not-proven", "full_rendering_binding": "not-proven", "original_semantics": "not-claimed"}


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("image", type=Path)
    p.add_argument("program_report", type=Path)
    p.add_argument("--managed-summary", type=Path, required=True)
    p.add_argument("--report", type=Path, required=True)
    p.add_argument("--summary", type=Path, required=True)
    args = p.parse_args()
    paths = [a.resolve() for a in (args.image, args.program_report, args.managed_summary, args.report, args.summary)]
    if len(set(paths)) != 5: raise ValueError("input/output paths must differ")
    data = load_material(args.image)
    probe(args.image)
    sections, start = elf_sections(data), TARGETS["FifastCharacterSkin0__EAGLMicroCodeVS"]
    payload = slice_data(data, sections, 1, start, 740)
    if hashlib.sha256(payload).hexdigest().upper() != PROGRAM_HASH: raise ValueError("program identity changed")
    if any(s == 1 and at < start + 740 and at + 4 > start for s, at in elf_relocations(data, sections)):
        raise ValueError("relocation in program")
    expected = decode(payload)
    def verify(rows):
        if rows != expected: raise ValueError("managed report differs from independent fresh decode")
    verify(read_tsv(args.program_report))
    report_hash = hashlib.sha256(args.program_report.read_bytes()).hexdigest().upper()
    wanted_summary = summary(expected, report_hash)
    if read_tsv(args.managed_summary) != [wanted_summary]: raise ValueError("managed summary differs")
    negatives = 0
    def reject(action):
        nonlocal negatives
        try: action()
        except ValueError: negatives += 1; return
        raise AssertionError("negative control accepted changed evidence")
    for field, value in (("mac_source_slots", "ABC"), ("active_input_slots", "ABC"), ("c_operand", "v[2].xyzw"),
                         ("a_raw", "0xFFFFFFFF"), ("word2", "0x00000000"), ("final", "true"),
                         ("parent_inheritance_binding", "proven"), ("full_rendering_binding", "proven"), ("original_semantics", "claimed")):
        changed = [dict(r) for r in expected]
        changed[0][field] = value
        reject(lambda: verify(changed))
    for at, value in ((0, 0), (2, 0), (4, 1), (11, 255), (16, payload[16] | 1), (len(payload) - 4, payload[-4] & ~1)):
        changed = bytearray(payload); changed[at] = value
        reject(lambda: decode(changed))
    reject(lambda: decode(payload[:-1]))
    reject(lambda: decode(payload + b"\x00"))
    xbe = load_xbe(args.image, "default.xbe")
    if hashlib.sha256(xbe).hexdigest().upper() != EXPECTED_XBE_SHA256: raise ValueError("XBE identity changed")
    meta = analyze_xbe(args.image, "default.xbe")
    proofs = [check_slice(xbe, meta, label, *contract) for label, contract in STATIC.items()]
    changed = bytearray(xbe)
    at = virtual_to_file_offset(0x291DAE, meta["fields"]["base_address"], len(xbe), meta["sections"])
    changed[at] = 0x90
    reject(lambda: check_slice(changed, meta, "changed-upload", *STATIC["vertex-program-unmodified-dword-upload"]))
    _write_tsv(args.report, proofs[0].keys(), proofs)
    result = dict(wanted_summary)
    result.update(status="verified-independent-vertex-program-opcode-operands-control", static_spans=str(len(proofs)),
        static_sites=str(sum(int(r["checked_sites"]) for r in proofs)), negative_controls=str(negatives),
        xbe_sha256=EXPECTED_XBE_SHA256, static_report_sha256=hashlib.sha256(args.report.read_bytes()).hexdigest().upper(),
        managed_summary_sha256=hashlib.sha256(args.managed_summary.read_bytes()).hexdigest().upper())
    _write_tsv(args.summary, result.keys(), [result])
    for k, v in result.items(): print(f"{k}: {v}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
