"""Independently enumerate models from fresh ISO and verify named programs.

No managed offsets drive source selection. Model/layer/descriptor keys are
decoded again from ELF. The runtime loader remains unproven.
"""
from __future__ import annotations

import argparse
import hashlib
import struct
from pathlib import Path

from list_xdvdfs import XdvdfsReader
from xbe_asset_nested_literal_audit import parse_big_index, parse_big_index_from_reader, expand_nested_archive
from eagl_indexed_draw_source_control import elf_sections, elf_relocations, slice_data
from eagl_material_parent_probe import load_material, SOURCE, PAYLOAD_HASH
from eagl_material_declaration_control import STATIC as MATERIAL_STATIC
from eagl_vertex_program_control import decode
from eagl_render_parameter_static_control import check_slice
from eagl_original_render_command_dispatch_control import EXPECTED_XBE_SHA256, _write_tsv
from xbe_render_cfg_audit import read_tsv, load_xbe
from parse_xbe import analyze_xbe, virtual_to_file_offset

DECL_HASH = "2205EC6F6DEB9CB77F0A6B23755DBB2C98C833355CABF8B4C61F5C8DD8468CBE"
PROGRAM_HASHES = {"Skin": "B37951FBB3188E7843A9492EF675B4CBEA9EBA75CAA412942746BF1212CDFB67",
    "Cloth": "D17E05FE970D37E5D67B7FD3649AFC9F8F58D27CFE4EBF92DEA0737943CD363B",
    "Eye": "307153C2F396F2FBAC94F855AF3BB6F82FD4CF1B9DFD6A264A90EE02B364B609"}
STATUS = "verified-static-model-declaration-and-named-program-reference"
SCOPE = "serialized-reference-to-exact-named-program-definition"
STATIC = {"material-create-from-parent-fields": MATERIAL_STATIC["material-create-from-parent-fields"],
    "runtime-parent-field-and-program-handle-inheritance": (0x1971E0, 0x19722D, {
        0x1971E6: "mov esi, dword ptr [edi + 0x2c]", 0x1971E9: "test esi, esi", 0x1971EB: "je 0x19721d",
        0x1971ED: "mov eax, dword ptr [edi + 0x24]", 0x1971F2: "jne 0x19721d",
        0x1971F4: "mov eax, dword ptr [esi + 0x10]", 0x1971FB: "mov dword ptr [edi + 0x10], eax",
        0x1971FE: "mov ecx, dword ptr [esi + 0x18]", 0x197201: "mov dword ptr [edi + 0x18], ecx",
        0x197209: "call 0x1970e0", 0x197211: "mov dword ptr [edi + 0x10], edx",
        0x197217: "mov dword ptr [edi + 0x18], eax", 0x197228: "jmp 0x1970e0"}),
    "runtime-parent-constructor-field-and-list": (0x197290, 0x1972D2, {
        0x197290: "mov eax, dword ptr [esp + 4]", 0x197295: "mov esi, ecx",
        0x197297: "mov dword ptr [esi + 0x1c], 0", 0x19729E: "mov dword ptr [esi + 0x28], 1",
        0x1972A5: "mov dword ptr [esi + 0x2c], eax", 0x1972A8: "mov ecx, dword ptr [eax + 0x10]",
        0x1972B0: "call 0x196e90", 0x1972C3: "mov dword ptr [esi + 0x1c], eax",
        0x1972C6: "mov dword ptr [0x3458bc], esi"}),
    "runtime-parent-copy-declaration-and-handles": (0x196E90, 0x196F18, {
        0x196E98: "mov edi, dword ptr [esi + 0x2c]", 0x196E9B: "mov eax, dword ptr [edi + 4]",
        0x196E9E: "mov dword ptr [esi + 4], eax", 0x196EA1: "mov ecx, dword ptr [edi + 8]",
        0x196EA4: "mov dword ptr [esi + 8], ecx", 0x196EA7: "mov edx, dword ptr [edi + 0xc]",
        0x196EAA: "mov dword ptr [esi + 0xc], edx", 0x196EAD: "mov eax, dword ptr [edi + 0x14]",
        0x196EB0: "mov dword ptr [esi + 0x14], eax", 0x196EB3: "mov ecx, dword ptr [edi + 0x24]",
        0x196EB6: "mov dword ptr [esi + 0x24], ecx", 0x196EB9: "mov edx, dword ptr [edi + 0x20]",
        0x196EBC: "mov dword ptr [esi + 0x20], edx", 0x196EBF: "mov eax, dword ptr [edi + 0x10]",
        0x196EC2: "mov dword ptr [esi + 0x10], eax", 0x196EC5: "mov ecx, dword ptr [edi + 0x18]",
        0x196EC8: "mov dword ptr [esi + 0x18], ecx", 0x196ECB: "mov edx, dword ptr [edi + 0x30]",
        0x196ECE: "mov dword ptr [esi + 0x30], edx", 0x196EF9: "mov dword ptr [esi], eax",
        0x196F0B: "call 0x223cdf"})}


def symbols(data, sections):
    result = []
    for s in sections:
        if s[1] != 2: continue
        if s[9] != 16 or s[5] % 16 or s[6] >= len(sections): raise ValueError("invalid symbol table")
        strings = sections[s[6]]
        for at in range(s[4], s[4] + s[5], 16):
            name, value, size, info, other, sec = struct.unpack_from("<IIIBBH", data, at)
            if name >= strings[5]: raise ValueError("invalid symbol string")
            end = data.find(b"\x00", strings[4] + name, strings[4] + strings[5])
            if end < 0: raise ValueError("unterminated symbol")
            result.append((data[strings[4] + name:end].decode(), sec, value))
    return result


def accessor(data, sections, relocs, sec):
    def word(at): return struct.unpack("<I", slice_data(data, sections, sec, at, 4))[0]
    def local(at):
        if relocs.get((sec, at)) != [(2, "")]: raise ValueError("nonunique/nonlocal material pointer")
        return word(at)
    def named(at):
        refs = relocs.get((sec, at), [])
        if len(refs) != 1 or refs[0][0] != 2 or not refs[0][1] or word(at): raise ValueError("inexact named reference")
        return refs[0][1]
    return word, local, named


def definitions(data):
    ss = elf_sections(data); rs = elf_relocations(data, ss); syms = symbols(data, ss); result = {}
    def exact(name):
        found = [s for s in syms if s[0] == name]
        if len(found) != 1 or found[0][1] == 0: raise ValueError("inexact definition")
        return found[0]
    for kind, program_hash in PROGRAM_HASHES.items():
        parent = "ParentRM_FifastCharacter" + kind
        vs, ps = f"FifastCharacter{kind}0__EAGLMicroCodeVS", f"FifastCharacter{kind}0__EAGLMicroCodePS"
        _, sec, at = exact("__RM:::" + parent); _, vs_sec, vs_at = exact(vs); _, ps_sec, ps_at = exact(ps)
        word, local, named = accessor(data, ss, rs, sec)
        if sec != vs_sec or sec != ps_sec or word(at + 4) != 1 or named(local(at + 12)) != vs or named(local(at + 20)) != ps:
            raise ValueError("parent source tables changed")
        declaration = slice_data(data, ss, sec, local(at + 8), 44)
        if hashlib.sha256(declaration).hexdigest().upper() != DECL_HASH: raise ValueError("source declaration changed")
        version, count = struct.unpack("<HH", slice_data(data, ss, sec, vs_at, 4))
        payload = slice_data(data, ss, sec, vs_at, 4 + 16 * count)
        if version != 0x2078 or count != (47 if kind == "Eye" else 46) or hashlib.sha256(payload).hexdigest().upper() != program_hash:
            raise ValueError("source program changed")
        decode(payload)
        result[vs] = dict(kind=kind, parent=parent, ps=ps, at=at, vs_at=vs_at, count=count, program_hash=program_hash)
    return result


def object_rows(archive, name, data, companion, defs):
    combined = data + companion
    ss = elf_sections(combined); rs = elf_relocations(combined, ss)
    result = []
    for model, sec, model_at in symbols(combined, ss):
        if not model.startswith("__Model:::") or not sec: continue
        word, local, named = accessor(data, ss, rs, sec)
        layer_count, layers_at = word(model_at + 0x9C), word(model_at + 0xCC)
        if not 0 < layer_count <= 128: raise ValueError("invalid layer count")
        old = word(layers_at) == 0xA0000000
        cursor = layers_at + (12 if old else 4)
        for layer in range(layer_count):
            if old: cursor += 8
            count = word(cursor)
            if count > 1024: raise ValueError("invalid primitive count")
            table, stride = cursor + 4, 8 if old else 4
            for primitive in range(count):
                descriptor = local(table + primitive * stride + (4 if old else 0))
                at = local(descriptor)
                raw = slice_data(data, ss, sec, at, 52); words = struct.unpack("<13I", raw)
                command = local(at)
                if words[1] != 1 or words[4] or words[6] or words[9] or words[10] != 0xFFFFFFFF: raise ValueError("model material fields changed")
                # Declaration identity is exact, not a float/profile coincidence.
                decl = local(at + 8); declaration = slice_data(data, ss, sec, decl, 44)
                if hashlib.sha256(declaration).hexdigest().upper() != DECL_HASH: raise ValueError("model declaration differs")
                vs_table, ps_table = local(at + 12), local(at + 20)
                vs, ps, parent1c, parent2c = named(vs_table), named(ps_table), named(at + 28), named(at + 44)
                if vs not in defs: raise ValueError("unknown named program")
                d = defs[vs]
                if ps != d["ps"] or parent1c != parent2c or parent2c != d["parent"]: raise ValueError("inconsistent program/parent references")
                result.append({"model_archive": archive, "model_object": name, "object_sha256": hashlib.sha256(data).hexdigest().upper(),
                    "section": str(sec), "layer": str(layer), "primitive": str(primitive), "render_method_offset": f"0x{at:08X}",
                    "render_method_bytes": "52", "raw_words": ",".join(f"0x{w:08X}" for w in words), "command_offset": f"0x{command:08X}",
                    "declaration_offset": f"0x{decl:08X}", "declaration_bytes": "44", "declaration_sha256": DECL_HASH,
                    "declaration_elements": "5", "vertex_table_offset": f"0x{vs_table:08X}", "pixel_table_offset": f"0x{ps_table:08X}",
                    "parent_1c_reference": parent1c, "parent_2c_reference": parent2c, "vertex_program_reference": vs,
                    "pixel_program_reference": ps, "material_kind": d["kind"], "definition_source": SOURCE, "definition_source_sha256": PAYLOAD_HASH,
                    "definition_parent_offset": f"0x{d['at']:08X}", "definition_vertex_offset": f"0x{d['vs_at']:08X}",
                    "definition_vertex_bytes": str(4 + 16 * d["count"]), "definition_vertex_instruction_count": str(d["count"]),
                    "definition_vertex_sha256": d["program_hash"], "status": STATUS, "reference_scope": SCOPE,
                    "runtime_loader_resolution": "not-proven", "selected_buffers": "not-proven", "full_rendering_binding": "not-proven", "original_semantics": "not-claimed"})
            cursor = table + count * stride
    return result


def verify(rows, expected):
    def key(r): return tuple(r[f] for f in ("model_archive", "model_object", "section", "layer", "primitive"))
    if len({key(r) for r in rows}) != len(rows) or {key(r): r for r in rows} != {key(r): r for r in expected}:
        raise ValueError("model report differs from independent source universe")


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("image", type=Path); p.add_argument("models", type=Path)
    p.add_argument("--report", type=Path, required=True); p.add_argument("--summary", type=Path, required=True)
    a = p.parse_args()
    if len({v.resolve() for v in (a.image, a.models, a.report, a.summary)}) != 4: raise ValueError("input/output alias")
    defs = definitions(load_material(a.image)); expected = []; objects = 0; sample = None
    reader = XdvdfsReader(a.image)
    try:
        top = next(e for e in reader.entries() if e.path == "data/models.big")
        entries = parse_big_index_from_reader(reader, top)
        stems = {Path(e["name"]).stem.casefold() for e in entries if Path(e["name"]).suffix.lower() == ".xsh"}
        packages = sorted((e for e in entries if Path(e["name"]).suffix.lower() == ".big" and Path(e["name"]).stem.casefold() in stems), key=lambda e: e["name"].casefold())
        for entry in packages:
            archive = expand_nested_archive(reader._read_at(top.image_offset + entry["offset"], entry["size"]))
            leaves = parse_big_index(archive); lookup = {e["name"].casefold(): e for e in leaves}
            def leaf(name):
                e = lookup[name.casefold()]; return archive[e["offset"]:e["offset"] + e["size"]]
            for obj in leaves:
                if Path(obj["name"]).suffix.lower() != ".ord": continue
                data, companion = leaf(obj["name"]), leaf(str(Path(obj["name"]).with_suffix(".orl")))
                rows = object_rows(entry["name"], obj["name"], data, companion, defs)
                expected.extend(rows); objects += 1
                if sample is None and rows: sample = (entry["name"], obj["name"], data, companion)
    finally: reader.close()
    if len(expected) != 2364 or objects != 883: raise ValueError("fresh model universe changed")
    verify(read_tsv(a.models), expected)
    negatives = 0
    def reject(action):
        nonlocal negatives
        try: action()
        except ValueError: negatives += 1; return
        raise AssertionError("negative control accepted changed evidence")
    for field in ("declaration_sha256", "vertex_program_reference", "pixel_program_reference", "parent_2c_reference",
        "raw_words", "definition_vertex_sha256", "runtime_loader_resolution", "selected_buffers", "full_rendering_binding", "original_semantics"):
        changed = [dict(r) for r in expected]; changed[0][field] = "changed-or-promoted"
        reject(lambda: verify(changed, expected))
    reject(lambda: verify(expected[:-1], expected)); reject(lambda: verify(expected + expected[:1], expected))
    archive, name, data, companion = sample
    row = expected[0]; ss = elf_sections(data + companion); sec = int(row["section"])
    for offset in (int(row["render_method_offset"], 0) + 4, int(row["render_method_offset"], 0) + 44, int(row["declaration_offset"], 0)):
        changed = bytearray(data); changed[ss[sec][4] + offset] ^= 1
        reject(lambda: object_rows(archive, name, changed, companion, defs))
    xbe = load_xbe(a.image, "default.xbe")
    if hashlib.sha256(xbe).hexdigest().upper() != EXPECTED_XBE_SHA256: raise ValueError("XBE identity changed")
    meta = analyze_xbe(a.image, "default.xbe")
    proofs = [check_slice(xbe, meta, label, *contract) for label, contract in STATIC.items()]
    changed = bytearray(xbe); at = virtual_to_file_offset(0x1971E6, meta["fields"]["base_address"], len(xbe), meta["sections"]); changed[at] = 0x90
    reject(lambda: check_slice(changed, meta, "changed-runtime-field", *STATIC["runtime-parent-field-and-program-handle-inheritance"]))
    _write_tsv(a.report, proofs[0].keys(), proofs)
    result = {"model_rows": len(expected), "model_objects": objects, "material_definitions": len(defs), "declarations_matched": len(expected),
        "parent_1c_2c_same_reference_rows": len(expected), **{f"{k.lower()}_rows": sum(r["material_kind"] == k for r in expected) for k in PROGRAM_HASHES},
        "source_path": SOURCE, "source_sha256": PAYLOAD_HASH, "declaration_sha256": DECL_HASH,
        "report_sha256": hashlib.sha256(a.models.read_bytes()).hexdigest().upper(), "static_report_sha256": hashlib.sha256(a.report.read_bytes()).hexdigest().upper(),
        "xbe_sha256": EXPECTED_XBE_SHA256, "static_spans": len(proofs), "static_sites": sum(int(r["checked_sites"]) for r in proofs),
        "negative_controls": negatives, "status": "verified-independent-model-material-reference-control", "reference_scope": SCOPE,
        "runtime_loader_resolution": "not-proven", "selected_buffers": "not-proven", "full_rendering_binding": "not-proven", "original_semantics": "not-claimed"}
    _write_tsv(a.summary, result.keys(), [result])
    for k, v in result.items(): print(f"{k}: {v}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
