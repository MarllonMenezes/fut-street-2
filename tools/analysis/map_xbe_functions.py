"""Recover a conservative direct-call map from executable XBE sections."""

from __future__ import annotations

import argparse
from collections import Counter, defaultdict
from pathlib import Path

try:
    from capstone import CS_ARCH_X86, CS_MODE_32, Cs
except ImportError as exc:  # pragma: no cover
    raise SystemExit("Capstone não está instalado no ambiente de análise.") from exc

from list_xdvdfs import XdvdfsReader
from parse_xbe import analyze_xbe, virtual_to_file_offset


def load_xbe(image: Path, internal_path: str) -> bytes:
    reader = XdvdfsReader(image)
    try:
        entries = {entry.path: entry for entry in reader.entries()}
        entry = entries.get(internal_path)
        if entry is None or entry.is_directory or entry.image_offset is None:
            raise ValueError(f"internal file not found: {internal_path}")
        return reader._read_at(entry.image_offset, entry.size)
    finally:
        reader.close()


def in_exec(address: int, sections: list[dict]) -> bool:
    return any(
        section["executable"]
        and section["virtual_address"] <= address < section["virtual_address"] + section["virtual_size"]
        for section in sections
    )


def recover(image: Path, internal_path: str) -> dict:
    result = analyze_xbe(image, internal_path)
    data = load_xbe(image, internal_path)
    decoder = Cs(CS_ARCH_X86, CS_MODE_32)
    decoder.detail = True
    decoder.skipdata = True
    calls: list[dict] = []
    decoded_by_section = {}
    for section in result["sections"]:
        if not section["executable"]:
            continue
        raw = data[section["file_address"] : section["file_address"] + section["file_size"]]
        instructions = list(decoder.disasm(raw, section["virtual_address"]))
        decoded_by_section[section["name"]] = len(instructions)
        for instruction in instructions:
            if instruction.mnemonic != "call" or not instruction.operands:
                continue
            operand = instruction.operands[0]
            if operand.type != 2:
                continue
            target = operand.imm & 0xFFFFFFFF
            if in_exec(target, result["sections"]):
                calls.append({"source": instruction.address, "target": target})
    inbound = Counter(call["target"] for call in calls)
    entry = result["decoded_entry_points"]["retail_candidate"]
    selected = [entry, 0x0024EB3A, 0x00250CE2, 0x0024E7CC, 0x0024D373, 0x0024CCA7]
    return {
        "image": str(image),
        "internal_path": internal_path,
        "entry_point": entry,
        "decoded_instructions": decoded_by_section,
        "direct_calls": len(calls),
        "unique_targets": len(inbound),
        "top_targets": [{"address": address, "inbound": count} for address, count in inbound.most_common(80)],
        "selected_targets": [
            {"address": address, "inbound": inbound.get(address, 0)} for address in selected if in_exec(address, result["sections"])
        ],
    }


def hx(value: int) -> str:
    return f"0x{value:08X}"


def render(report: dict) -> str:
    lines = [
        "# FIFA Street 2 — mapa candidato de chamadas",
        "",
        "O mapa foi produzido decodificando as seções marcadas como executáveis "
        "como x86 32-bit e mantendo `call` diretos com destino executável. "
        "Como o XBE pode conter dados misturados ao código, os números e destinos "
        "são candidatos e ainda precisam de validação por fluxo básico.",
        "",
        f"- Entry point: `{hx(report['entry_point'])}`",
        f"- Instruções decodificadas por seção: `{report['decoded_instructions']}`",
        f"- Calls diretos aceitos: `{report['direct_calls']}`",
        f"- Destinos únicos: `{report['unique_targets']}`",
        "",
        "## Alvos prioritários",
        "",
        "| Endereço | Referências diretas |",
        "|---:|---:|",
    ]
    for item in report["selected_targets"]:
        lines.append(f"| `{hx(item['address'])}` | {item['inbound']} |")
    lines += ["", "## Destinos mais referenciados", "", "| Endereço | Referências |", "|---:|---:|"]
    for item in report["top_targets"]:
        lines.append(f"| `{hx(item['address'])}` | {item['inbound']} |")
    lines += ["", "Próximo refinamento: identificar limites por fluxo básico e referências a strings/formatos de asset.", ""]
    return "\n".join(lines)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("image", type=Path)
    parser.add_argument("--internal-path", default="default.xbe")
    parser.add_argument("--markdown", type=Path)
    args = parser.parse_args()
    report = recover(args.image, args.internal_path)
    if args.markdown:
        args.markdown.write_text(render(report), encoding="utf-8")
    print(f"direct calls: {report['direct_calls']}; unique targets: {report['unique_targets']}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
