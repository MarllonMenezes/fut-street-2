"""Read-only XBE header analysis from an Xbox XDVDFS image.

The XBE is read directly from its internal XDVDFS extent. No game file is
written to disk and no signature, media flag, or executable byte is changed.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import struct
from pathlib import Path

from list_xdvdfs import XdvdfsReader


HEADER_SIZE = 0x178
SECTION_SIZE = 0x38
XBE_MAGIC = b"XBEH"
SECTION_EXECUTABLE = 0x00000004
SECTION_WRITABLE = 0x00000001
ENTRY_XOR_KEYS = {
    "retail_candidate": 0xA8FC57AB,
    "debug_candidate": 0x94859D4B,
}
KERNEL_XOR_KEYS = {
    "retail_candidate": 0x5B6D40B6,
    "debug_candidate": 0xEFB1F152,
}


def u32(data: bytes, offset: int) -> int:
    return struct.unpack_from("<I", data, offset)[0]


def utf16z(data: bytes) -> str:
    end = data.find(b"\x00\x00")
    if end >= 0:
        data = data[:end]
    if len(data) % 2:
        data = data[:-1]
    return data.decode("utf-16le", errors="replace").rstrip("\x00")


def ascii_z(data: bytes) -> str:
    end = data.find(b"\x00")
    return data[: end if end >= 0 else len(data)].decode("ascii", errors="replace")


def pointer_to_file_offset(pointer: int, base: int, image_size: int) -> int | None:
    if pointer == 0:
        return None
    offset = pointer - base
    if 0 <= offset < image_size:
        return offset
    return None


def pointer_string(data: bytes, pointer: int, base: int, image_size: int) -> str | None:
    offset = pointer_to_file_offset(pointer, base, image_size)
    if offset is None:
        return None
    return ascii_z(data[offset : min(offset + 512, image_size)])


def parse_library(data: bytes, pointer: int, base: int, image_size: int) -> dict | None:
    offset = pointer_to_file_offset(pointer, base, image_size)
    if offset is None or offset + 0x10 > image_size:
        return None
    name = ascii_z(data[offset : offset + 8])
    major, middle, minor, flags = struct.unpack_from("<hhhh", data, offset + 8)
    return {
        "file_offset": offset,
        "name": name,
        "major": major,
        "middle": middle,
        "minor": minor,
        "flags": flags & 0xFFFF,
    }


def section_entry(data: bytes, offset: int, base: int) -> dict:
    flags, virtual_address, virtual_size, file_address, file_size = struct.unpack_from(
        "<IIIII", data, offset
    )
    name_pointer, ref_count, head_ref, tail_ref = struct.unpack_from(
        "<IIII", data, offset + 0x14
    )
    digest = data[offset + 0x24 : offset + 0x38]
    name_offset = pointer_to_file_offset(name_pointer, base, len(data))
    name = None
    if name_offset is not None:
        name = ascii_z(data[name_offset : min(name_offset + 256, len(data))])
    raw_in_bounds = 0 <= file_address <= len(data) and file_address + file_size <= len(data)
    digest_match = None
    if raw_in_bounds:
        # xbedump's XBE validator hashes the little-endian file size followed
        # by the section bytes, not just the section bytes.
        digest_match = (
            hashlib.sha1(struct.pack("<I", file_size) + data[file_address : file_address + file_size]).digest()
            == digest
        )
    return {
        "header_offset": offset,
        "flags": flags,
        "executable": bool(flags & SECTION_EXECUTABLE),
        "writable": bool(flags & SECTION_WRITABLE),
        "virtual_address": virtual_address,
        "virtual_size": virtual_size,
        "file_address": file_address,
        "file_size": file_size,
        "name_pointer": name_pointer,
        "name_file_offset": name_offset,
        "name": name,
        "reference_count": ref_count,
        "head_reference_pointer": head_ref,
        "tail_reference_pointer": tail_ref,
        "sha1_stored": digest.hex(),
        "sha1_matches_file_data": digest_match,
    }


def virtual_to_file_offset(address: int, base: int, image_size: int, sections: list[dict]) -> int | None:
    """Map an XBE virtual address through the section table."""
    for section in sections:
        start = section["virtual_address"]
        end = start + section["virtual_size"]
        if start <= address < end:
            offset = section["file_address"] + (address - start)
            if 0 <= offset < image_size:
                return offset
    if base <= address < base + image_size:
        return address - base
    return None


def decode_kernel_thunks(
    data: bytes,
    encoded_pointer: int,
    base: int,
    sections: list[dict],
) -> dict:
    decoded = {
        label: encoded_pointer ^ key for label, key in KERNEL_XOR_KEYS.items()
    }
    result = {"decoded_addresses": decoded}
    for label, address in decoded.items():
        offset = virtual_to_file_offset(address, base, len(data), sections)
        result[label] = {"file_offset": offset, "ordinals": []}
        if offset is None:
            continue
        for index in range(4096):
            table_offset = offset + index * 4
            if table_offset + 4 > len(data):
                break
            value = u32(data, table_offset)
            if value == 0:
                break
            result[label]["ordinals"].append(
                {
                    "index": index,
                    "raw_value": value,
                    "ordinal": value & 0x7FFFFFFF,
                    "is_kernel_thunk": bool(value & 0x80000000),
                }
            )
    return result


def analyze_xbe(image: Path, internal_path: str = "default.xbe") -> dict:
    reader = XdvdfsReader(image)
    try:
        entries = {entry.path: entry for entry in reader.entries()}
        entry = entries.get(internal_path)
        if entry is None or entry.is_directory or entry.image_offset is None:
            raise ValueError(f"internal file not found: {internal_path}")
        data = reader._read_at(entry.image_offset, entry.size)
        if data[:4] != XBE_MAGIC:
            raise ValueError(f"internal file does not begin with XBEH: {internal_path}")

        fields = {
            "base_address": u32(data, 0x104),
            "header_size": u32(data, 0x108),
            "image_size": u32(data, 0x10C),
            "xbe_header_size": u32(data, 0x110),
            "timestamp": u32(data, 0x114),
            "certificate_pointer": u32(data, 0x118),
            "number_of_sections": u32(data, 0x11C),
            "section_headers_pointer": u32(data, 0x120),
            "init_flags": u32(data, 0x124),
            "entry_point_encoded": u32(data, 0x128),
            "tls_directory_pointer": u32(data, 0x12C),
            "stack_commit": u32(data, 0x130),
            "heap_reserve": u32(data, 0x134),
            "heap_commit": u32(data, 0x138),
            "pe_base_address": u32(data, 0x13C),
            "pe_image_size": u32(data, 0x140),
            "pe_checksum": u32(data, 0x144),
            "pe_timestamp": u32(data, 0x148),
            "debug_path_pointer": u32(data, 0x14C),
            "debug_filename_pointer": u32(data, 0x150),
            "debug_unicode_filename_pointer": u32(data, 0x154),
            "kernel_thunk_pointer_encoded": u32(data, 0x158),
            "debug_import_pointer": u32(data, 0x15C),
            "number_of_libraries": u32(data, 0x160),
            "library_versions_pointer": u32(data, 0x164),
            "kernel_library_pointer": u32(data, 0x168),
            "xapi_library_pointer": u32(data, 0x16C),
            "microsoft_logo_pointer": u32(data, 0x170),
            "microsoft_logo_size": u32(data, 0x174),
        }

        base = fields["base_address"]
        sections_offset = pointer_to_file_offset(
            fields["section_headers_pointer"], base, len(data)
        )
        sections = []
        if sections_offset is not None:
            for index in range(fields["number_of_sections"]):
                offset = sections_offset + index * SECTION_SIZE
                if offset + SECTION_SIZE > len(data):
                    break
                section = section_entry(data, offset, base)
                section["index"] = index
                sections.append(section)

        certificate_offset = pointer_to_file_offset(
            fields["certificate_pointer"], base, len(data)
        )
        certificate = None
        if certificate_offset is not None and certificate_offset + 0xB0 <= len(data):
            cert_size = u32(data, certificate_offset)
            title_id = u32(data, certificate_offset + 8)
            title_raw = data[certificate_offset + 0x0C : certificate_offset + 0x5C]
            alternate_title_ids = [
                u32(data, certificate_offset + 0x5C + index * 4) for index in range(16)
            ]
            certificate = {
                "file_offset": certificate_offset,
                "size": cert_size,
                "timestamp": u32(data, certificate_offset + 4),
                "title_id": title_id,
                "title_name": utf16z(title_raw),
                "alternate_title_ids": alternate_title_ids,
                "allowed_media_types": u32(data, certificate_offset + 0x9C),
                "game_region": u32(data, certificate_offset + 0xA0),
                "game_rating": u32(data, certificate_offset + 0xA4),
                "disk_number": u32(data, certificate_offset + 0xA8),
                "version": u32(data, certificate_offset + 0xAC),
            }
            if cert_size >= 0x1DC and certificate_offset + 0x1DC <= len(data):
                certificate["original_certificate_size"] = u32(data, certificate_offset + 0x1D0)
                certificate["online_service"] = u32(data, certificate_offset + 0x1D4)
                certificate["runtime_security_flags"] = u32(data, certificate_offset + 0x1D8)

        tls = None
        tls_offset = virtual_to_file_offset(
            fields["tls_directory_pointer"], base, len(data), sections
        )
        if tls_offset is not None and tls_offset + 0x1C <= len(data):
            tls = {
                "file_offset": tls_offset,
                "raw_start_address": u32(data, tls_offset),
                "raw_end_address": u32(data, tls_offset + 4),
                "tls_index_address": u32(data, tls_offset + 8),
                "tls_callbacks_address": u32(data, tls_offset + 0x0C),
                "size_of_zero_fill": u32(data, tls_offset + 0x10),
                "characteristics_raw": data[tls_offset + 0x14 : tls_offset + 0x1C].hex(),
            }

        decoded_entry_points = {
            label: fields["entry_point_encoded"] ^ key
            for label, key in ENTRY_XOR_KEYS.items()
        }
        executable_ranges = [
            (s["virtual_address"], s["virtual_address"] + s["virtual_size"])
            for s in sections
            if s["executable"]
        ]
        entry_validation = {
            label: any(start <= address < end for start, end in executable_ranges)
            for label, address in decoded_entry_points.items()
        }

        kernel_thunks = decode_kernel_thunks(
            data,
            fields["kernel_thunk_pointer_encoded"],
            base,
            sections,
        )

        library_versions = []
        library_offset = pointer_to_file_offset(
            fields["library_versions_pointer"], base, len(data)
        )
        if library_offset is not None:
            for index in range(fields["number_of_libraries"]):
                library = parse_library(
                    data, base + library_offset + index * 0x10, base, len(data)
                )
                if library is None:
                    break
                library["index"] = index
                library_versions.append(library)

        return {
            "image": str(image),
            "internal_path": internal_path,
            "internal_offset": entry.image_offset,
            "internal_size": entry.size,
            "magic": data[:4].decode("ascii"),
            "header_sha256": hashlib.sha256(data[:HEADER_SIZE]).hexdigest(),
            "fields": fields,
            "pointer_offsets": {
                "certificate": certificate_offset,
                "section_headers": sections_offset,
                "library_versions": library_offset,
            },
            "debug_strings": {
                "path": pointer_string(data, fields["debug_path_pointer"], base, len(data)),
                "filename": pointer_string(data, fields["debug_filename_pointer"], base, len(data)),
            },
            "certificate": certificate,
            "tls": tls,
            "decoded_entry_points": decoded_entry_points,
            "entry_point_in_executable_section": entry_validation,
            "kernel_thunks": kernel_thunks,
            "libraries": library_versions,
            "kernel_library": parse_library(
                data, fields["kernel_library_pointer"], base, len(data)
            ),
            "xapi_library": parse_library(data, fields["xapi_library_pointer"], base, len(data)),
            "sections": sections,
        }
    finally:
        reader.close()


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("image", type=Path)
    parser.add_argument("--internal-path", default="default.xbe")
    parser.add_argument("--json", action="store_true")
    args = parser.parse_args()
    result = analyze_xbe(args.image, args.internal_path)
    if args.json:
        print(json.dumps(result, indent=2))
    else:
        print(f"XBE: {result['internal_path']}")
        print(f"image offset: 0x{result['internal_offset']:X}")
        print(f"size: {result['internal_size']}")
        for key, value in result["fields"].items():
            print(f"{key}: {value if isinstance(value, str) else hex(value) if isinstance(value, int) else value}")
        print("certificate:", json.dumps(result["certificate"], ensure_ascii=False))
        print("entry points:", json.dumps(result["decoded_entry_points"]))
        print("entry validation:", json.dumps(result["entry_point_in_executable_section"]))
        print("libraries:", json.dumps(result["libraries"], ensure_ascii=False))
        print("sections:")
        for section in result["sections"]:
            print(json.dumps(section, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
