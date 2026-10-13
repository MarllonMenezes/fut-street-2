"""Disassemble bounded internal call targets reached by the FIFA entry point."""

from __future__ import annotations

import argparse
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


def disassemble_target(data: bytes, result: dict, address: int, max_bytes: int) -> dict:
    base = result["fields"]["base_address"]
    offset = virtual_to_file_offset(address, base, len(data), result["sections"])
    if offset is None:
        raise ValueError(f"target 0x{address:X} does not map to XBE bytes")
    section = next(
        section
        for section in result["sections"]
        if section["virtual_address"] <= address < section["virtual_address"] + section["virtual_size"]
    )
    available = min(max_bytes, section["file_address"] + section["file_size"] - offset)
    decoder = Cs(CS_ARCH_X86, CS_MODE_32)
    decoder.detail = True
    instructions = []
    for instruction in decoder.disasm(data[offset : offset + available], address):
        item = {
            "address": instruction.address,
            "bytes": instruction.bytes.hex(" "),
            "mnemonic": instruction.mnemonic,
            "op_str": instruction.op_str,
        }
        if instruction.mnemonic in {"call", "jmp"} and instruction.operands:
            operand = instruction.operands[0]
            if operand.type == 2:
                item["target"] = operand.imm & 0xFFFFFFFF
        instructions.append(item)
        if instruction.mnemonic in {"ret", "retf"}:
            break
    return {"address": address, "file_offset": offset, "section": section["name"], "instructions": instructions}


def hx(value: int) -> str:
    return f"0x{value:08X}"


def render(report: dict) -> str:
    lines = [
        "# FIFA Street 2 — primeiros alvos internos do startup",
        "",
        "Rotinas alcançadas por chamadas relativas observadas na janela do entry point. "
        "Os limites foram interrompidos no primeiro `ret` identificado e continuam "
        "sendo uma classificação estática.",
        "",
    ]
    for function in report["functions"]:
        lines += [
            f"## Alvo `{hx(function['address'])}`",
            "",
            f"Offset no XBE: `{hx(function['file_offset'])}`; seção `{function['section']}`.",
            "",
            "| Endereço | Bytes | Instrução | Alvo |",
            "|---:|---|---|---:|",
        ]
        for item in function["instructions"]:
            target = hx(item["target"]) if "target" in item else ""
            text = f"{item['mnemonic']} {item['op_str']}".strip()
            lines.append(f"| `{hx(item['address'])}` | `{item['bytes']}` | `{text}` | `{target}` |")
        lines += ["", "---", ""]
    return "\n".join(lines)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("image", type=Path)
    parser.add_argument("--internal-path", default="default.xbe")
    parser.add_argument("--address", action="append", required=True, type=lambda value: int(value, 0))
    parser.add_argument("--max-bytes", type=int, default=192)
    parser.add_argument("--markdown", type=Path)
    args = parser.parse_args()
    result = analyze_xbe(args.image, args.internal_path)
    data = load_xbe(args.image, args.internal_path)
    report = {
        "image": str(args.image),
        "internal_path": args.internal_path,
        "functions": [disassemble_target(data, result, address, args.max_bytes) for address in args.address],
    }
    if args.markdown:
        args.markdown.write_text(render(report), encoding="utf-8")
    for function in report["functions"]:
        print(f"{hx(function['address'])}: {len(function['instructions'])} instructions")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
