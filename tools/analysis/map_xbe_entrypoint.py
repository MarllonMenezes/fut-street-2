"""Build a conservative, read-only map around an Xbox XBE entry point.

This is not a full disassembler. It records the retail entry point, nearby
bytes, direct relative call targets, and compiler-style prologue candidates
inside executable sections. The XBE is read directly from XDVDFS; the ISO and
the embedded executable are never rewritten.
"""

from __future__ import annotations

import argparse
import json
import struct
from pathlib import Path

from list_xdvdfs import XdvdfsReader
from parse_xbe import analyze_xbe, virtual_to_file_offset


ENTRY_RETAIL_KEY = 0xA8FC57AB
CALL_REL32 = 0xE8
JMP_REL32 = 0xE9


def signed_i32(value: int) -> int:
    return struct.unpack("<i", struct.pack("<I", value))[0]


def load_internal_xbe(image: Path, internal_path: str) -> bytes:
    reader = XdvdfsReader(image)
    try:
        entries = {entry.path: entry for entry in reader.entries()}
        entry = entries.get(internal_path)
        if entry is None or entry.is_directory or entry.image_offset is None:
            raise ValueError(f"internal file not found: {internal_path}")
        return reader._read_at(entry.image_offset, entry.size)
    finally:
        reader.close()


def executable_ranges(result: dict) -> list[tuple[int, int]]:
    return [
        (section["virtual_address"], section["virtual_address"] + section["virtual_size"])
        for section in result["sections"]
        if section["executable"]
    ]


def in_ranges(address: int, ranges: list[tuple[int, int]]) -> bool:
    return any(start <= address < end for start, end in ranges)


def direct_relative_transfers(
    data: bytes,
    result: dict,
    section: dict,
    only_from: tuple[int, int] | None = None,
) -> list[dict]:
    """Find E8/E9 rel32 transfers with targets inside executable sections."""
    start = section["file_address"]
    end = min(start + section["file_size"], len(data))
    if only_from is not None:
        virtual_start, virtual_end = only_from
        start = max(start, section["file_address"] + (virtual_start - section["virtual_address"]))
        end = min(end, section["file_address"] + (virtual_end - section["virtual_address"]))
    ranges = executable_ranges(result)
    found = []
    for offset in range(max(start, 0), max(start, end - 4)):
        opcode = data[offset]
        if opcode not in (CALL_REL32, JMP_REL32):
            continue
        displacement = signed_i32(u32(data, offset + 1))
        source = section["virtual_address"] + (offset - section["file_address"])
        target = source + 5 + displacement
        if in_ranges(target, ranges):
            found.append(
                {
                    "kind": "call" if opcode == CALL_REL32 else "jmp",
                    "source": source,
                    "target": target,
                    "file_offset": offset,
                    "displacement": displacement,
                }
            )
    return found


def u32(data: bytes, offset: int) -> int:
    return struct.unpack_from("<I", data, offset)[0]


def prologue_candidates(data: bytes, result: dict) -> list[dict]:
    """Find common MSVC x86 function prologues; candidates are not proof."""
    patterns = {
        b"\x55\x8B\xEC": "push ebp; mov ebp, esp",
        b"\x55\x8B\xEC\x83\xEC": "push ebp; mov ebp, esp; sub esp",
        b"\x53\x56\x57": "push ebx; push esi; push edi",
    }
    candidates = []
    for section in result["sections"]:
        if not section["executable"]:
            continue
        raw = data[section["file_address"] : section["file_address"] + section["file_size"]]
        for pattern, description in patterns.items():
            cursor = 0
            while True:
                found = raw.find(pattern, cursor)
                if found < 0:
                    break
                candidates.append(
                    {
                        "address": section["virtual_address"] + found,
                        "file_offset": section["file_address"] + found,
                        "pattern": pattern.hex(" "),
                        "description": description,
                        "section": section["name"],
                    }
                )
                cursor = found + 1
    candidates.sort(key=lambda item: item["address"])
    return candidates


def build_map(image: Path, internal_path: str, window: int) -> dict:
    result = analyze_xbe(image, internal_path)
    data = load_internal_xbe(image, internal_path)
    base = result["fields"]["base_address"]
    entry = result["decoded_entry_points"]["retail_candidate"]
    entry_offset = virtual_to_file_offset(entry, base, len(data), result["sections"])
    if entry_offset is None:
        raise ValueError(f"entry point 0x{entry:X} does not map to XBE bytes")

    half = max(window // 2, 1)
    window_start = max(0, entry_offset - half)
    window_end = min(len(data), entry_offset + half)
    entry_section = next(
        section
        for section in result["sections"]
        if section["virtual_address"] <= entry < section["virtual_address"] + section["virtual_size"]
    )
    entry_window_virtual = (
        entry_section["virtual_address"] + (window_start - entry_section["file_address"]),
        entry_section["virtual_address"] + (window_end - entry_section["file_address"]),
    )
    transfers = direct_relative_transfers(data, result, entry_section, entry_window_virtual)
    section_transfers = direct_relative_transfers(data, result, entry_section)
    targets = sorted({item["target"] for item in section_transfers})
    return {
        "image": str(image),
        "internal_path": internal_path,
        "entry_point": entry,
        "entry_file_offset": entry_offset,
        "entry_section": entry_section["name"],
        "entry_window": {
            "file_start": window_start,
            "file_end": window_end,
            "virtual_start": entry_window_virtual[0],
            "virtual_end": entry_window_virtual[1],
            "hex": data[window_start:window_end].hex(" "),
        },
        "entry_window_transfers": transfers,
        "entry_section_transfer_count": len(section_transfers),
        "entry_section_unique_targets": targets,
        "prologue_candidates": prologue_candidates(data, result),
    }


def markdown(report: dict) -> str:
    def hx(value: int) -> str:
        return f"0x{value:08X}"

    lines = [
        "# FIFA Street 2 — mapa inicial do entry point",
        "",
        "Este artefato é uma triagem estática somente leitura. Ele não substitui",
        "um disassembler completo: chamadas relativas e prólogos são candidatos",
        "e precisam ser confirmados por decodificação de instruções e fluxo de controle.",
        "",
        f"- Executável: `{report['internal_path']}` dentro de `{report['image']}`",
        f"- Entry point retail: `{hx(report['entry_point'])}`",
        f"- Offset no XBE: `{hx(report['entry_file_offset'])}`",
        f"- Seção: `{report['entry_section']}`",
        "",
        "## Janela de bytes",
        "",
        f"Virtual: `{hx(report['entry_window']['virtual_start'])}`–`{hx(report['entry_window']['virtual_end'])}`",
        f"Arquivo: `{hx(report['entry_window']['file_start'])}`–`{hx(report['entry_window']['file_end'])}`",
        "",
        "```text",
        report["entry_window"]["hex"],
        "```",
        "## Transferências relativas próximas ao entry point",
        "",
    ]
    if report["entry_window_transfers"]:
        lines.append("| Tipo | Origem | Destino | Offset |")
        lines.append("|---|---:|---:|---:|")
        for item in report["entry_window_transfers"]:
            lines.append(
                f"| {item['kind']} | `{hx(item['source'])}` | `{hx(item['target'])}` | `{hx(item['file_offset'])}` |"
            )
    else:
        lines.append("Nenhuma transferência relativa para uma seção executável foi encontrada nesta janela.")
    lines += [
        "",
        "## Triagem da seção `.text`",
        "",
        f"Foram encontrados {report['entry_section_transfer_count']} candidatos `call/jmp rel32` na seção do entry point, "
        f"apontando para {len(report['entry_section_unique_targets'])} destinos únicos dentro de seções executáveis.",
        "",
        "## Próxima etapa",
        "",
        "Confirmar esses candidatos com um disassembler x86 e construir o primeiro mapa de funções: "
        "startup, inicialização de arquivos, loop principal e chamadas para XAPILIB/D3D/DSOUND.",
        "Os prólogos abaixo são apenas indícios e não foram usados como prova de limites de função.",
        "",
        f"Total de candidatos de prólogo: {len(report['prologue_candidates'])}.",
        "",
    ]
    return "\n".join(lines)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("image", type=Path)
    parser.add_argument("--internal-path", default="default.xbe")
    parser.add_argument("--window", type=int, default=256)
    parser.add_argument("--json", action="store_true")
    parser.add_argument("--markdown", type=Path)
    args = parser.parse_args()
    report = build_map(args.image, args.internal_path, args.window)
    if args.markdown:
        args.markdown.write_text(markdown(report), encoding="utf-8")
    if args.json:
        print(json.dumps(report, indent=2))
    else:
        print(f"entry point: 0x{report['entry_point']:08X}")
        print(f"file offset: 0x{report['entry_file_offset']:08X}")
        print(f"section: {report['entry_section']}")
        print(f"nearby transfers: {len(report['entry_window_transfers'])}")
        print(f"section transfers: {report['entry_section_transfer_count']}")
        print(f"prologue candidates: {len(report['prologue_candidates'])}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
