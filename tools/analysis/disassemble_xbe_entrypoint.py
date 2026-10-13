"""Disassemble a bounded XBE entry-point window with Capstone.

Capstone is an analysis dependency only; this script reads ``default.xbe``
directly from the XDVDFS image and never writes the ISO or the embedded XBE.
Install Capstone separately if the script is run outside the prepared analysis
environment.
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path

try:
    from capstone import CS_ARCH_X86, CS_MODE_32, Cs
except ImportError as exc:  # pragma: no cover - depends on local tooling
    raise SystemExit(
        "Capstone não está instalado. Instale-o apenas no ambiente de análise "
        "ou execute o mapa heurístico map_xbe_entrypoint.py."
    ) from exc

from list_xdvdfs import XdvdfsReader
from parse_xbe import analyze_xbe, virtual_to_file_offset


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


def build_report(image: Path, internal_path: str, window: int) -> dict:
    result = analyze_xbe(image, internal_path)
    data = load_internal_xbe(image, internal_path)
    base = result["fields"]["base_address"]
    address = result["decoded_entry_points"]["retail_candidate"]
    offset = virtual_to_file_offset(address, base, len(data), result["sections"])
    if offset is None:
        raise ValueError(f"entry point 0x{address:X} does not map to bytes")
    # Start exactly at the known entry point. Starting at an arbitrary byte
    # before it could make a variable-length x86 decoder appear misaligned.
    start = offset
    end = min(len(data), offset + window)
    section = next(
        section
        for section in result["sections"]
        if section["virtual_address"] <= address < section["virtual_address"] + section["virtual_size"]
    )
    virtual_start = address
    decoder = Cs(CS_ARCH_X86, CS_MODE_32)
    decoder.detail = True
    instructions = []
    for instruction in decoder.disasm(data[start:end], virtual_start):
        item = {
            "address": instruction.address,
            "bytes": instruction.bytes.hex(" "),
            "mnemonic": instruction.mnemonic,
            "op_str": instruction.op_str,
        }
        if instruction.mnemonic in {"call", "jmp"} and instruction.operands:
            operand = instruction.operands[0]
            if operand.type == 2:  # X86_OP_IMM
                item["target"] = operand.imm & 0xFFFFFFFF
        instructions.append(item)
    return {
        "image": str(image),
        "internal_path": internal_path,
        "entry_point": address,
        "entry_file_offset": offset,
        "section": section["name"],
        "window": {"file_start": start, "file_end": end, "virtual_start": virtual_start},
        "instructions": instructions,
    }


def hx(value: int) -> str:
    return f"0x{value:08X}"


def render_markdown(report: dict) -> str:
    lines = [
        "# FIFA Street 2 — disassembly inicial do entry point",
        "",
        "Disassembly x86 de uma janela limitada a partir do entry point retail. "
        "O XBE foi lido diretamente do XDVDFS; a imagem original não foi alterada.",
        "",
        f"- Executável: `{report['internal_path']}`",
        f"- Entry point: `{hx(report['entry_point'])}`",
        f"- Offset no XBE: `{hx(report['entry_file_offset'])}`",
        f"- Seção: `{report['section']}`",
        f"- Janela: `{hx(report['window']['virtual_start'])}`–`{hx(report['window']['virtual_start'] + (report['window']['file_end'] - report['window']['file_start']))}`",
        "",
        "| Endereço | Bytes | Instrução | Alvo |",
        "|---:|---|---|---:|",
    ]
    for item in report["instructions"]:
        target = hx(item["target"]) if "target" in item else ""
        text = f"{item['mnemonic']} {item['op_str']}".strip()
        lines.append(f"| `{hx(item['address'])}` | `{item['bytes']}` | `{text}` | `{target}` |")
    lines += [
        "",
        "## Leitura inicial",
        "",
        "O entry point começa com carregamentos e cálculos de estado de runtime, "
        "chama rotinas internas e termina dentro da janela limitada. O prólogo "
        "`push ebp; mov ebp, esp` observado depois pertence à rotina seguinte, "
        "em `0x0024D930`. Os alvos são endereços do próprio `.text`; ainda não "
        "são nomes de funções sem análise de fluxo e de referências cruzadas.",
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
    report = build_report(args.image, args.internal_path, args.window)
    if args.markdown:
        args.markdown.write_text(render_markdown(report), encoding="utf-8")
    if args.json:
        print(json.dumps(report, indent=2))
    else:
        print(f"entry point: {hx(report['entry_point'])}")
        print(f"instructions: {len(report['instructions'])}")
        for instruction in report["instructions"]:
            target = f" -> {hx(instruction['target'])}" if "target" in instruction else ""
            print(f"{hx(instruction['address'])}: {instruction['mnemonic']} {instruction['op_str']}{target}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
