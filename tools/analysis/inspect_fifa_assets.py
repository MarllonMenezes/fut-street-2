"""Inventory FIFA Street 2 asset signatures without extracting or rewriting data."""

from __future__ import annotations

import argparse
from collections import Counter, defaultdict
from pathlib import Path

from list_xdvdfs import XdvdfsReader


EXTENSIONS = (".big", ".abk", ".ast", ".vp6", ".xsh", ".viv", ".bh", ".off", ".evt", ".xbx", ".csi", ".mpf", ".mus", ".pmb")


def printable_magic(data: bytes) -> str:
    return "".join(chr(byte) if 32 <= byte < 127 else "." for byte in data[:4])


def inspect(image: Path) -> dict:
    reader = XdvdfsReader(image)
    try:
        grouped: dict[str, list[dict]] = defaultdict(list)
        for entry in reader.entries():
            suffix = Path(entry.name).suffix.lower()
            if entry.is_directory or entry.image_offset is None or suffix not in EXTENSIONS:
                continue
            sample_size = min(64, entry.size)
            sample = reader._read_at(entry.image_offset, sample_size)
            grouped[suffix].append(
                {
                    "path": entry.path,
                    "size": entry.size,
                    "magic": printable_magic(sample),
                    "header_hex": sample.hex(" "),
                }
            )
        return {"image": str(image), "groups": grouped}
    finally:
        reader.close()


def render(report: dict) -> str:
    lines = [
        "# FIFA Street 2 — assinaturas dos assets",
        "",
        "Inventário somente leitura. Os dados foram amostrados diretamente do",
        "XDVDFS; nenhum asset foi extraído ou regravado.",
        "",
        f"Imagem: `{report['image']}`",
        "",
    ]
    for suffix in sorted(report["groups"]):
        entries = report["groups"][suffix]
        magic_counts = Counter(entry["magic"] for entry in entries)
        lines += [
            f"## `{suffix}` — {len(entries)} arquivos",
            "",
            "Magics observados: " + ", ".join(f"`{magic}` ({count})" for magic, count in magic_counts.most_common()),
            "",
            "| Arquivo | Tamanho | Magic | Primeiros bytes |",
            "|---|---:|---|---|",
        ]
        for entry in sorted(entries, key=lambda item: item["path"])[:12]:
            lines.append(
                f"| `{entry['path']}` | {entry['size']:,} | `{entry['magic']}` | `{entry['header_hex']}` |".replace(",", ".")
            )
        if len(entries) > 12:
            lines.append(f"| … | … | … | mais {len(entries) - 12} arquivos não listados individualmente |")
        lines += ["", "Interpretação: assinatura registrada; formato interno ainda não decodificado.", ""]
    return "\n".join(lines)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("image", type=Path)
    parser.add_argument("--markdown", type=Path)
    args = parser.parse_args()
    report = inspect(args.image)
    if args.markdown:
        args.markdown.write_text(render(report), encoding="utf-8")
    for suffix, entries in sorted(report["groups"].items()):
        print(f"{suffix}: {len(entries)} files; magics={sorted({entry['magic'] for entry in entries})}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
