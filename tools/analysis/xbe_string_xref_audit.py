"""Build a conservative, read-only XBE string-reference audit.

The audit reads ``default.xbe`` directly from the original XDVDFS image,
decodes executable sections as 32-bit x86, and records instructions whose
immediate or absolute-memory operand equals the address of an interesting
printable string.  The result is a static candidate map only: it does not
execute the XBE, recover source semantics, or assert an asset/material/GPU
binding.  The ISO and the embedded XBE are never written.
"""

from __future__ import annotations

import argparse
import bisect
import csv
import hashlib
import re
from collections import Counter
from pathlib import Path

try:
    from capstone import CS_ARCH_X86, CS_MODE_32, Cs, CsError
    from capstone.x86 import X86_OP_IMM, X86_OP_MEM
except ImportError as exc:  # pragma: no cover - environment dependency
    raise SystemExit("Capstone não está instalado no ambiente de análise.") from exc

from list_xdvdfs import XdvdfsReader
from parse_xbe import analyze_xbe


PRINTABLE_RUN = re.compile(rb"[\x20-\x7E]{4,}")
INTERESTING_LITERAL = re.compile(
    r"(?:\.(?:xsh|shpx|abk|ast|evt|bnk|big|bh|xbo)|"
    r"shapename|eaglanimation|d3d|dsound|audio|anim|model|texture|"
    r"vertex|mesh|skin|morph|render|shader|material|uv|stadium|ball|"
    r"apt)",
    re.IGNORECASE,
)


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


def literal_category(value: str) -> str | None:
    """Classify a literal by its text only; this is not decoded semantics."""

    if not INTERESTING_LITERAL.search(value):
        return None
    lowered = value.casefold()
    if re.search(r"\.(?:xsh|shpx|abk|ast|evt|bnk|big|bh|xbo)\b", lowered):
        return "asset-extension-literal"
    if any(token in lowered for token in ("d3d", "dsound", "eaglanimation")):
        return "api-or-runtime-token-literal"
    if any(token in lowered for token in ("audio", "anim", "model", "texture", "vertex", "mesh", "skin", "morph")):
        return "subsystem-token-literal"
    return "content-or-path-literal"


def collect_literals(data: bytes, sections: list[dict]) -> list[dict]:
    literals: list[dict] = []
    for section in sections:
        start = section["file_address"]
        end = start + section["file_size"]
        raw = data[start:end]
        for match in PRINTABLE_RUN.finditer(raw):
            text = match.group(0).decode("ascii", errors="replace")
            category = literal_category(text)
            if category is None:
                continue
            literals.append(
                {
                    "section": section["name"] or f"section-{section['index']}",
                    "section_index": section["index"],
                    "virtual_address": section["virtual_address"] + match.start(),
                    "file_offset": start + match.start(),
                    "text": text,
                    "category": category,
                }
            )
    return literals


def operand_targets(instruction) -> list[tuple[int, str]]:
    targets: list[tuple[int, str]] = []
    for operand in instruction.operands:
        if operand.type == X86_OP_IMM:
            targets.append((operand.imm & 0xFFFFFFFF, "immediate-pointer-candidate"))
        elif operand.type == X86_OP_MEM:
            memory = operand.mem
            if memory.base == 0 and memory.index == 0:
                targets.append((memory.disp & 0xFFFFFFFF, "absolute-memory-pointer-candidate"))
    return targets


def direct_call_target(instruction) -> int | None:
    """Return only a direct immediate call target, without naming its role."""

    if instruction.mnemonic != "call" or not instruction.operands:
        return None
    operand = instruction.operands[0]
    if operand.type != X86_OP_IMM:
        return None
    return operand.imm & 0xFFFFFFFF


def build_call_context(xrefs: list[dict], calls: list[dict], window: int = 128) -> list[dict]:
    """Pair each string xref with nearby direct calls as a local byte context."""

    calls_by_section: dict[str, list[dict]] = {}
    addresses_by_section: dict[str, list[int]] = {}
    for call in calls:
        calls_by_section.setdefault(call["source_section"], []).append(call)
    for section, section_calls in calls_by_section.items():
        section_calls.sort(key=lambda call: call["source_virtual_address"])
        addresses_by_section[section] = [call["source_virtual_address"] for call in section_calls]

    rows: list[dict] = []
    for xref in xrefs:
        section_calls = calls_by_section.get(xref["source_section"], [])
        addresses = addresses_by_section.get(xref["source_section"], [])
        source = xref["source_virtual_address"]
        left = bisect.bisect_left(addresses, source - window)
        right = bisect.bisect_right(addresses, source + window)
        nearby = section_calls[left:right]
        nearby.sort(key=lambda call: (abs(call["source_virtual_address"] - xref["source_virtual_address"]), call["source_virtual_address"]))
        if not nearby:
            nearby = [
                {
                    "source_section": xref["source_section"],
                    "source_virtual_address": None,
                    "source_file_offset": None,
                    "instruction_bytes": "",
                    "target_virtual_address": None,
                    "distance_bytes": None,
                    "target_is_executable": None,
                }
            ]
        for call in nearby:
            rows.append(
                {
                    "source_section": xref["source_section"],
                    "xref_source_virtual_address": xref["source_virtual_address"],
                    "xref_string_virtual_address": xref["string_virtual_address"],
                    "xref_string": xref["string"],
                    "literal_category": xref["literal_category"],
                    "call_source_virtual_address": call["source_virtual_address"],
                    "call_source_file_offset": call["source_file_offset"],
                    "call_instruction_bytes": call["instruction_bytes"],
                    "call_target_virtual_address": call["target_virtual_address"],
                    "distance_bytes": (
                        None
                        if call["source_virtual_address"] is None
                        else abs(call["source_virtual_address"] - xref["source_virtual_address"])
                    ),
                    "target_is_executable": call["target_is_executable"],
                    "status": "candidate-local-x86-call-context",
                    "original_semantics": "not-claimed",
                }
            )
    return rows


def build_call_clusters(call_context: list[dict]) -> list[dict]:
    """Group local call-context rows by target address only.

    This is a co-occurrence report, not function recovery. A target can appear
    in several unrelated code regions because the input window is local but no
    function boundaries or call semantics are asserted.
    """

    grouped: dict[int, list[dict]] = {}
    for row in call_context:
        target = row.get("call_target_virtual_address")
        if target is None or not row.get("target_is_executable"):
            continue
        grouped.setdefault(target, []).append(row)

    clusters: list[dict] = []
    for target, rows in grouped.items():
        distances = [row["distance_bytes"] for row in rows if row.get("distance_bytes") is not None]
        strings = sorted({row["xref_string"] for row in rows})
        categories = sorted({row["literal_category"] for row in rows})
        xref_sources = sorted({row["xref_source_virtual_address"] for row in rows})
        clusters.append(
            {
                "call_target_virtual_address": target,
                "target_is_executable": True,
                "context_rows": len(rows),
                "unique_xref_source_instructions": len(xref_sources),
                "unique_xref_strings": len(strings),
                "literal_categories": categories,
                "min_distance_bytes": min(distances) if distances else None,
                "max_distance_bytes": max(distances) if distances else None,
                "representative_strings": strings,
                "status": "candidate-local-call-target-cooccurrence",
                "original_semantics": "not-claimed",
            }
        )

    return sorted(
        clusters,
        key=lambda row: (-row["unique_xref_strings"], -row["context_rows"], row["call_target_virtual_address"]),
    )


def recover(image: Path, internal_path: str) -> dict:
    result = analyze_xbe(image, internal_path)
    data = load_xbe(image, internal_path)
    literals = collect_literals(data, result["sections"])
    by_address = {literal["virtual_address"]: literal for literal in literals}

    decoder = Cs(CS_ARCH_X86, CS_MODE_32)
    decoder.detail = True
    decoder.skipdata = True
    xrefs: list[dict] = []
    decoded_by_section: dict[str, int] = {}
    seen: set[tuple[int, int, str]] = set()
    calls: list[dict] = []
    for section in result["sections"]:
        if not section["executable"]:
            continue
        start = section["file_address"]
        end = start + section["file_size"]
        raw = data[start:end]
        section_name = section["name"] or f"section-{section['index']}"
        instructions = list(decoder.disasm(raw, section["virtual_address"]))
        decoded_by_section[section_name] = len(instructions)
        for instruction in instructions:
            if instruction.mnemonic == ".byte":
                continue
            try:
                targets = operand_targets(instruction)
            except CsError:
                # Capstone's skipdata pseudo-instructions deliberately have
                # no operand information. They remain decoded-count input,
                # but cannot produce a string-reference candidate.
                continue
            call_target = direct_call_target(instruction)
            if call_target is not None:
                calls.append(
                    {
                        "source_section": section_name,
                        "source_virtual_address": instruction.address,
                        "source_file_offset": start + (instruction.address - section["virtual_address"]),
                        "instruction_bytes": instruction.bytes.hex(),
                        "target_virtual_address": call_target,
                        "distance_bytes": None,
                        "target_is_executable": any(
                            candidate["executable"]
                            and candidate["virtual_address"] <= call_target < candidate["virtual_address"] + candidate["virtual_size"]
                            for candidate in result["sections"]
                        ),
                    }
                )
            for target, reference_kind in targets:
                literal = by_address.get(target)
                if literal is None:
                    continue
                key = (instruction.address, target, reference_kind)
                if key in seen:
                    continue
                seen.add(key)
                xrefs.append(
                    {
                        "source_section": section_name,
                        "source_virtual_address": instruction.address,
                        "source_file_offset": start + (instruction.address - section["virtual_address"]),
                        "instruction_bytes": instruction.bytes.hex(),
                        "mnemonic": instruction.mnemonic,
                        "operands": instruction.op_str,
                        "reference_kind": reference_kind,
                        "string_section": literal["section"],
                        "string_virtual_address": literal["virtual_address"],
                        "string_file_offset": literal["file_offset"],
                        "string": literal["text"],
                        "literal_category": literal["category"],
                        "status": "candidate-x86-reference",
                        "original_semantics": "not-claimed",
                    }
                )

    return {
        "image": str(image),
        "internal_path": internal_path,
        "xbe_sha256": hashlib.sha256(data).hexdigest(),
        "xbe_size": len(data),
        "interesting_literals": len(literals),
        "decoded_instructions": decoded_by_section,
        "direct_calls": calls,
        "xrefs": sorted(
            xrefs,
            key=lambda row: (row["source_virtual_address"], row["string_virtual_address"]),
        ),
    }


XREF_FIELDS = (
    "image",
    "internal_path",
    "xbe_sha256",
    "xbe_size",
    "source_section",
    "source_virtual_address",
    "source_file_offset",
    "instruction_bytes",
    "mnemonic",
    "operands",
    "reference_kind",
    "string_section",
    "string_virtual_address",
    "string_file_offset",
    "string",
    "literal_category",
    "status",
    "original_semantics",
)

CALL_CONTEXT_FIELDS = (
    "image",
    "internal_path",
    "xbe_sha256",
    "xbe_size",
    "source_section",
    "xref_source_virtual_address",
    "xref_string_virtual_address",
    "xref_string",
    "literal_category",
    "call_source_virtual_address",
    "call_source_file_offset",
    "call_instruction_bytes",
    "call_target_virtual_address",
    "distance_bytes",
    "target_is_executable",
    "status",
    "original_semantics",
)

CALL_CLUSTER_FIELDS = (
    "image",
    "internal_path",
    "xbe_sha256",
    "xbe_size",
    "call_target_virtual_address",
    "target_is_executable",
    "context_rows",
    "unique_xref_source_instructions",
    "unique_xref_strings",
    "literal_categories",
    "min_distance_bytes",
    "max_distance_bytes",
    "representative_strings",
    "status",
    "original_semantics",
)


def write_xrefs(report: dict, output: Path) -> None:
    output.parent.mkdir(parents=True, exist_ok=True)
    with output.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=XREF_FIELDS, delimiter="\t", lineterminator="\n")
        writer.writeheader()
        for row in report["xrefs"]:
            writer.writerow(
                {
                    "image": report["image"],
                    "internal_path": report["internal_path"],
                    "xbe_sha256": report["xbe_sha256"],
                    "xbe_size": report["xbe_size"],
                    **row,
                    "source_virtual_address": f"0x{row['source_virtual_address']:08X}",
                    "source_file_offset": f"0x{row['source_file_offset']:X}",
                    "string_virtual_address": f"0x{row['string_virtual_address']:08X}",
                    "string_file_offset": f"0x{row['string_file_offset']:X}",
                }
            )


def write_call_context(report: dict, output: Path) -> None:
    rows = build_call_context(report["xrefs"], report["direct_calls"])
    output.parent.mkdir(parents=True, exist_ok=True)
    with output.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=CALL_CONTEXT_FIELDS, delimiter="\t", lineterminator="\n")
        writer.writeheader()
        for row in rows:
            writer.writerow(
                {
                    "image": report["image"],
                    "internal_path": report["internal_path"],
                    "xbe_sha256": report["xbe_sha256"],
                    "xbe_size": report["xbe_size"],
                    **row,
                    "xref_source_virtual_address": f"0x{row['xref_source_virtual_address']:08X}",
                    "xref_string_virtual_address": f"0x{row['xref_string_virtual_address']:08X}",
                    "call_source_virtual_address": (
                        "" if row["call_source_virtual_address"] is None else f"0x{row['call_source_virtual_address']:08X}"
                    ),
                    "call_source_file_offset": (
                        "" if row["call_source_file_offset"] is None else f"0x{row['call_source_file_offset']:X}"
                    ),
                    "call_target_virtual_address": (
                        "" if row["call_target_virtual_address"] is None else f"0x{row['call_target_virtual_address']:08X}"
                    ),
                }
            )


def write_call_clusters(report: dict, output: Path) -> None:
    call_context = build_call_context(report["xrefs"], report["direct_calls"])
    clusters = build_call_clusters(call_context)
    output.parent.mkdir(parents=True, exist_ok=True)
    with output.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=CALL_CLUSTER_FIELDS, delimiter="\t", lineterminator="\n")
        writer.writeheader()
        for row in clusters:
            writer.writerow(
                {
                    "image": report["image"],
                    "internal_path": report["internal_path"],
                    "xbe_sha256": report["xbe_sha256"],
                    "xbe_size": report["xbe_size"],
                    "call_target_virtual_address": f"0x{row['call_target_virtual_address']:08X}",
                    "target_is_executable": row["target_is_executable"],
                    "context_rows": row["context_rows"],
                    "unique_xref_source_instructions": row["unique_xref_source_instructions"],
                    "unique_xref_strings": row["unique_xref_strings"],
                    "literal_categories": "|".join(row["literal_categories"]),
                    "min_distance_bytes": "" if row["min_distance_bytes"] is None else row["min_distance_bytes"],
                    "max_distance_bytes": "" if row["max_distance_bytes"] is None else row["max_distance_bytes"],
                    "representative_strings": "|".join(
                        value.replace("\t", " ").replace("\r", " ").replace("\n", " ")
                        for value in row["representative_strings"]
                    ),
                    "status": row["status"],
                    "original_semantics": row["original_semantics"],
                }
            )


def write_summary(report: dict, output: Path) -> None:
    category_counts = Counter(row["literal_category"] for row in report["xrefs"])
    kind_counts = Counter(row["reference_kind"] for row in report["xrefs"])
    call_context = build_call_context(report["xrefs"], report["direct_calls"])
    call_clusters = build_call_clusters(call_context)
    context_xrefs = {
        row["xref_source_virtual_address"]
        for row in call_context
        if row["call_source_virtual_address"] is not None
    }
    executable_call_targets = {
        row["call_target_virtual_address"]
        for row in call_context
        if row["call_target_virtual_address"] is not None and row["target_is_executable"]
    }
    rows = [
        ("image", report["image"]),
        ("internal_path", report["internal_path"]),
        ("xbe_sha256", report["xbe_sha256"]),
        ("xbe_size", report["xbe_size"]),
        ("interesting_literals", report["interesting_literals"]),
        ("decoded_instructions", sum(report["decoded_instructions"].values())),
        ("direct_call_instructions", len(report["direct_calls"])),
        ("xref_count", len(report["xrefs"])),
        ("unique_source_instructions", len({row["source_virtual_address"] for row in report["xrefs"]})),
        ("xref_context_rows", len(call_context)),
        ("xrefs_with_nearby_direct_call", len(context_xrefs)),
        ("unique_nearby_executable_call_targets", len(executable_call_targets)),
        ("call_target_clusters", len(call_clusters)),
        (
            "call_target_clusters_with_multiple_strings",
            sum(row["unique_xref_strings"] >= 2 for row in call_clusters),
        ),
        ("call_context_window_bytes", 128),
        ("call_cluster_status", "candidate-local-call-target-cooccurrence-only"),
        ("call_context_status", "candidate-local-x86-call-context-only"),
        ("status", "candidate-x86-reference-only"),
        ("original_semantics", "not-claimed"),
    ]
    rows.extend((f"literal_category:{key}", value) for key, value in sorted(category_counts.items()))
    rows.extend((f"reference_kind:{key}", value) for key, value in sorted(kind_counts.items()))
    output.parent.mkdir(parents=True, exist_ok=True)
    with output.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.writer(handle, delimiter="\t", lineterminator="\n")
        writer.writerow(("metric", "value"))
        writer.writerows(rows)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("image", type=Path)
    parser.add_argument("--internal-path", default="default.xbe")
    parser.add_argument("--report", type=Path)
    parser.add_argument("--summary", type=Path)
    parser.add_argument("--call-context", type=Path)
    parser.add_argument("--call-clusters", type=Path)
    args = parser.parse_args()
    report = recover(args.image, args.internal_path)
    if args.report:
        write_xrefs(report, args.report)
    if args.summary:
        write_summary(report, args.summary)
    if args.call_context:
        write_call_context(report, args.call_context)
    if args.call_clusters:
        write_call_clusters(report, args.call_clusters)
    print(
        f"interesting_literals: {report['interesting_literals']}; "
        f"xrefs: {len(report['xrefs'])}; direct_calls: {len(report['direct_calls'])}; "
        f"status: candidate-x86-reference-only"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
