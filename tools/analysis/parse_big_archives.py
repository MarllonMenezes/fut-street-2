"""Read-only parser for the EA BIG4/BIGF index found in FIFA assets."""

from __future__ import annotations

import argparse
import struct
from pathlib import Path

from list_xdvdfs import Entry, XdvdfsReader


MAGICS = {b"BIG4", b"BIGF"}


def display_magic(data: bytes) -> str:
    if all(32 <= byte < 127 for byte in data):
        return data.decode("ascii")
    return "0x" + data.hex()


def be_u32(data: bytes, offset: int) -> int:
    return struct.unpack_from(">I", data, offset)[0]


def parse_archive(reader: XdvdfsReader, entry: Entry) -> dict:
    if entry.image_offset is None:
        raise ValueError("archive has no image offset")
    header = reader._read_at(entry.image_offset, min(entry.size, 16))
    if len(header) < 16 or header[:4] not in MAGICS:
        return {"path": entry.path, "size": entry.size, "magic": display_magic(header[:4]), "status": "unsupported magic"}

    magic = header[:4].decode("ascii")
    archive_size = struct.unpack_from("<I", header, 4)[0]
    file_count = be_u32(header, 8)
    index_end = be_u32(header, 12)
    if index_end < 16 or index_end > entry.size:
        return {"path": entry.path, "size": entry.size, "magic": magic, "status": "invalid index end"}

    index = reader._read_at(entry.image_offset, index_end)
    cursor = 16
    files = []
    error = None
    for index_number in range(file_count):
        if cursor + 8 > len(index):
            error = f"entry {index_number}: truncated offset/size"
            break
        file_offset = be_u32(index, cursor)
        file_size = be_u32(index, cursor + 4)
        name_end = index.find(b"\x00", cursor + 8, len(index))
        if name_end < 0:
            error = f"entry {index_number}: unterminated name"
            break
        name = index[cursor + 8 : name_end].decode("ascii", "replace")
        if file_offset < index_end or file_offset + file_size > entry.size:
            error = f"entry {index_number}: data range outside archive"
            break
        files.append({"name": name, "offset": file_offset, "size": file_size})
        cursor = name_end + 1

    return {
        "path": entry.path,
        "size": entry.size,
        "magic": magic,
        "archive_size": archive_size,
        "file_count": file_count,
        "index_end": index_end,
        "parsed_count": len(files),
        "status": "OK" if error is None and len(files) == file_count else error or "count mismatch",
        "files": files,
    }


def inspect(image: Path) -> list[dict]:
    reader = XdvdfsReader(image)
    try:
        entries = [entry for entry in reader.entries() if not entry.is_directory and entry.name.lower().endswith(".big")]
        return [parse_archive(reader, entry) for entry in sorted(entries, key=lambda item: item.path.lower())]
    finally:
        reader.close()


def render(reports: list[dict], image: Path) -> str:
    lines = [
        "# FIFA Street 2 — índices dos arquivos BIG",
        "",
        "Parser somente leitura. Foram lidos os headers e índices internos dos",
        "arquivos `.big`; nenhum conteúdo foi extraído ou regravado.",
        "",
        f"Imagem: `{image}`",
        "",
        "| Arquivo | Magic | Tamanho | Entradas | Índice | Status |",
        "|---|---|---:|---:|---:|---|",
    ]
    for report in reports:
        lines.append(
            f"| `{report['path']}` | `{report['magic']}` | {report['size']:,} | "
            f"{report.get('file_count', '—')} | {report.get('index_end', '—')} | `{report['status']}` |".replace(",", ".")
        )
    sample = next((report for report in reports if report["path"] == "data/anims.big"), None)
    if sample and sample.get("files"):
        lines += ["", "## Amostra: `data/anims.big`", "", "| Nome | Offset | Tamanho |", "|---|---:|---:|"]
        for item in sample["files"][:12]:
            lines.append(f"| `{item['name']}` | {item['offset']} | {item['size']} |")
    lines += [
        "",
        "A estrutura BIG4/BIGF e seus campos endian mistos foram tratados como índice; codecs e formatos dos arquivos internos continuam separados.",
        "Referência pública de layout: https://github.com/OpenSAGE/Docs/blob/master/file-formats/big/index.rst",
        "",
    ]
    return "\n".join(lines)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("image", type=Path)
    parser.add_argument("--archive", help="parse one internal archive instead of all .big files")
    parser.add_argument("--markdown", type=Path)
    args = parser.parse_args()
    if args.archive:
        reader = XdvdfsReader(args.image)
        try:
            entry = next(item for item in reader.entries() if item.path == args.archive)
            reports = [parse_archive(reader, entry)]
        finally:
            reader.close()
    else:
        reports = inspect(args.image)
    if args.markdown:
        args.markdown.write_text(render(reports, args.image), encoding="utf-8")
    for report in reports:
        print(f"{report['path']}: {report['status']}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
