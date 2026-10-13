"""List files in an original Xbox/Xbox 360 XDVDFS image.

This tool is intentionally read-only. It does not extract, rewrite, patch, or
otherwise modify the input image. The directory record layout follows the
open-source extract-xiso implementation:

  0x00: left child offset, in 4-byte units, little-endian uint16
  0x02: right child offset, in 4-byte units, little-endian uint16
  0x04: file start sector, little-endian uint32
  0x08: file size, little-endian uint32
  0x0C: attributes, uint8
  0x0D: filename length, uint8
  0x0E: filename bytes

For XGD2 full-disc images the XDVDFS partition is normally at 0x0FD90000.
The value is detected from the XDVDFS volume marker rather than assumed.
"""

from __future__ import annotations

import argparse
import json
import struct
from dataclasses import asdict, dataclass
from pathlib import Path


SECTOR_SIZE = 2048
HEADER_OFFSET = 0x10000
MEDIA_MARKER = b"MICROSOFT*XBOX*MEDIA"
PARTITION_OFFSETS = (0, 0x0FD90000, 0x02080000, 0x18300000)
DIRECTORY_ATTRIBUTE = 0x10
PADDING_WORD = 0xFFFF


@dataclass(frozen=True)
class Volume:
    image: str
    image_size: int
    partition_offset: int
    root_sector: int
    root_size: int


@dataclass(frozen=True)
class Entry:
    path: str
    name: str
    is_directory: bool
    attributes: int
    size: int
    start_sector: int
    image_offset: int | None


class XdvdfsReader:
    def __init__(self, image: Path) -> None:
        self.image = image
        self.file = image.open("rb")
        self.file.seek(0, 2)
        self.image_size = self.file.tell()
        self.partition_offset, self.root_sector, self.root_size = self._read_volume()

    def close(self) -> None:
        self.file.close()

    def _read_at(self, offset: int, size: int) -> bytes:
        if offset < 0 or offset + size > self.image_size:
            raise ValueError(f"read outside image: 0x{offset:X}+{size}")
        self.file.seek(offset)
        data = self.file.read(size)
        if len(data) != size:
            raise ValueError(f"short read at 0x{offset:X}")
        return data

    def _read_volume(self) -> tuple[int, int, int]:
        for partition_offset in PARTITION_OFFSETS:
            marker_offset = partition_offset + HEADER_OFFSET
            if marker_offset + 28 > self.image_size:
                continue
            header = self._read_at(marker_offset, 28)
            if header[:20] != MEDIA_MARKER:
                continue
            root_sector, root_size = struct.unpack_from("<II", header, 20)
            root_offset = partition_offset + root_sector * SECTOR_SIZE
            if root_size == 0 or root_offset + root_size > self.image_size:
                raise ValueError(
                    f"invalid root directory: offset=0x{root_offset:X}, size={root_size}"
                )
            return partition_offset, root_sector, root_size
        raise ValueError("XDVDFS volume marker not found")

    def volume(self) -> Volume:
        return Volume(
            image=str(self.image),
            image_size=self.image_size,
            partition_offset=self.partition_offset,
            root_sector=self.root_sector,
            root_size=self.root_size,
        )

    def _read_entry(self, directory_offset: int, node_offset: int) -> tuple[int, int, Entry]:
        record_offset = directory_offset + node_offset
        fixed = self._read_at(record_offset, 14)
        left, right, start_sector, size, attributes, name_length = struct.unpack(
            "<HHIIBB", fixed
        )
        if name_length == 0 or name_length > 255:
            raise ValueError(
                f"invalid filename length {name_length} at 0x{record_offset:X}"
            )
        name_bytes = self._read_at(record_offset + 14, name_length)
        name = name_bytes.decode("ascii", errors="replace")
        entry = Entry(
            path=name,
            name=name,
            is_directory=bool(attributes & DIRECTORY_ATTRIBUTE),
            attributes=attributes,
            size=size,
            start_sector=start_sector,
            image_offset=(
                self.partition_offset + start_sector * SECTOR_SIZE
                if not (attributes & DIRECTORY_ATTRIBUTE)
                else None
            ),
        )
        return left * 4, right * 4, entry

    def _walk_directory_tree(self, directory_offset: int, directory_size: int) -> list[Entry]:
        entries: list[Entry] = []
        visited: set[int] = set()

        def walk(node_offset: int, depth: int) -> None:
            if node_offset in visited:
                return
            if node_offset < 0 or node_offset + 14 > directory_size:
                raise ValueError(
                    f"directory child outside table: offset=0x{node_offset:X}, size={directory_size}"
                )
            visited.add(node_offset)
            first_word = struct.unpack("<H", self._read_at(directory_offset + node_offset, 2))[0]
            if first_word == PADDING_WORD:
                return
            if depth > 4096:
                raise ValueError("directory tree depth limit exceeded")
            left, right, entry = self._read_entry(directory_offset, node_offset)
            if left:
                walk(left, depth + 1)
            entries.append(entry)
            if right:
                walk(right, depth + 1)

        walk(0, 0)
        return entries

    def entries(self) -> list[Entry]:
        root_offset = self.partition_offset + self.root_sector * SECTOR_SIZE
        root_entries = self._walk_directory_tree(root_offset, self.root_size)
        result: list[Entry] = []

        def descend(entries: list[Entry], prefix: str) -> None:
            for entry in entries:
                full_path = f"{prefix}{entry.name}"
                current = Entry(
                    path=full_path,
                    name=entry.name,
                    is_directory=entry.is_directory,
                    attributes=entry.attributes,
                    size=entry.size,
                    start_sector=entry.start_sector,
                    image_offset=entry.image_offset,
                )
                result.append(current)
                if entry.is_directory and entry.size:
                    directory_offset = self.partition_offset + entry.start_sector * SECTOR_SIZE
                    child_entries = self._walk_directory_tree(directory_offset, entry.size)
                    descend(child_entries, full_path + "/")

        descend(root_entries, "")
        return result


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("image", type=Path)
    parser.add_argument("--json", action="store_true", help="emit machine-readable JSON")
    args = parser.parse_args()

    reader = XdvdfsReader(args.image)
    try:
        entries = reader.entries()
        if args.json:
            print(json.dumps({"volume": asdict(reader.volume()), "entries": [asdict(e) for e in entries]}, indent=2))
        else:
            volume = reader.volume()
            print(f"image: {volume.image}")
            print(f"image_size: {volume.image_size}")
            print(f"partition_offset: 0x{volume.partition_offset:X}")
            print(f"root_sector: {volume.root_sector}")
            print(f"root_size: {volume.root_size}")
            print("entries:")
            for entry in entries:
                kind = "DIR " if entry.is_directory else "FILE"
                offset = "-" if entry.image_offset is None else f"0x{entry.image_offset:X}"
                print(
                    f"{kind} {entry.size:10d} bytes  sector={entry.start_sector:8d} "
                    f"offset={offset:>12}  {entry.path}"
                )
    finally:
        reader.close()
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
