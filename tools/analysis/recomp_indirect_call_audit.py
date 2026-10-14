"""Classify indirect calls in the direct entry-point subgraph.

The audit joins generated ``RECOMP_ICALL_SAFE_AT`` sites with the static
function metadata and the XBE's read-only data.  Fixed kernel-thunk slots are
identified from the XBE thunk table; object/register targets remain dynamic.
No indirect target is inferred from a guessed vtable and the XBE is never
executed.
"""

from __future__ import annotations

import argparse
import json
import re
import struct
from collections import Counter, deque
from pathlib import Path
from typing import Any

try:
    from .recomp_entry_call_graph_audit import recover_direct_reachable
except ImportError:  # pragma: no cover - supports direct script execution
    from recomp_entry_call_graph_audit import recover_direct_reachable


FUNCTION_SYMBOL_RE = re.compile(r"^void\s+([A-Za-z_][A-Za-z0-9_]*)\(void\)")
REGISTER_ASSIGNMENT_RE = re.compile(
    r"^\s*(?P<register>eax|ebx|ecx|edx|esi|edi)\s*=\s*(?P<rhs>[^;]+);"
)
REGISTER_WRITE_RE = re.compile(
    r"\b(?P<register>eax|ebx|ecx|edx|esi|edi)\s*(?:\+\+|--|\+=|-=|&=|\|=)"
)
REGISTER_LITERAL_RE = re.compile(
    r"^(?P<value>0x[0-9A-Fa-f]{1,8}|[0-9]+)u?$"
)
REGISTER_NAME_RE = re.compile(r"^(?P<register>eax|ebx|ecx|edx|esi|edi)$")
REGISTER_ADD_RE = re.compile(
    r"^(?P<register>eax|ebx|ecx|edx|esi|edi)\s*\+\s*"
    r"(?P<value>0x[0-9A-Fa-f]{1,8}|[0-9]+)$"
)
INDIRECT_CALL_RE = re.compile(
    r"_icall_target\s*=\s*(?P<expression>[^;]+);.*?"
    r"RECOMP_ICALL_SAFE_AT\([^,]+,\s*[^,]+,\s*0x(?P<site>[0-9A-Fa-f]{8})u\)"
)
FIXED_SLOT_RE = re.compile(r"MEM32\(0x(?P<address>[0-9A-Fa-f]{1,8})\)")
FIXED_SLOT_WRITE_RE = re.compile(
    r"MEM32\(0x(?P<slot>[0-9A-Fa-f]{1,8})\)\s*=\s*"
    r"0x(?P<value>[0-9A-Fa-f]{1,8})u?;"
)
PRCB_BASE_LOAD_RE = re.compile(
    r"^\s*(?P<register>eax|ebx|ecx|edx|esi|edi)\s*=\s*"
    r"MEM32\(XBOX_FS_BASE \+ 0x20\);"
)
PRCB_CACHE_WRITE_RE = re.compile(
    r"MEM32\((?P<register>eax|ebx|ecx|edx|esi|edi)\s*\+\s*0x250\)\s*="
)
KERNEL_RESOLVER_CASE_RE = re.compile(
    r"^\s*case\s+(?P<ordinal>[0-9]+)\s*:\s*return\s+"
    r"\(ULONG_PTR\)(?P<target>[^;]+);"
)
DYNAMIC_OBJECT_FIELD_WRITE_RE = re.compile(
    r"MEM32\((?P<register>eax|ebx|ecx|edx|esi|edi)\s*\+\s*0x584\)\s*="
)
DYNAMIC_INDEXED_RECORD_WRITE_RE = re.compile(
    r"MEM32\((?P<base>eax|ebx|ecx|edx|esi|edi)\s*\+\s*"
    r"(?P<index>eax|ebx|ecx|edx|esi|edi)\s*\*\s*4\s*\+\s*8\)\s*="
)
DYNAMIC_ABSOLUTE_WRITE_RE = re.compile(
    r"MEM32\(0x(?P<address>[0-9A-Fa-f]{1,8})\)\s*="
)
DYNAMIC_REGISTER_WRITE_RE = re.compile(
    r"MEM32\((?P<register>eax|ebx|ecx|edx|esi|edi)\)\s*="
)
XBE_MAGIC = b"XBEH"
SECTION_SIZE = 0x38
KERNEL_XOR_KEYS = (0x5B6D40B6, 0xEFB1F152)


def _address(value: Any) -> int:
    if isinstance(value, int):
        return value
    return int(str(value), 0)


def _u32(data: bytes, offset: int) -> int:
    return struct.unpack_from("<I", data, offset)[0]


def _section_for_address(sections: list[dict[str, int]], address: int) -> dict[str, int] | None:
    for section in sections:
        if section["virtual_address"] <= address < section["virtual_address"] + section["virtual_size"]:
            return section
    return None


def _file_offset(
    sections: list[dict[str, int]], image_size: int, address: int
) -> int | None:
    section = _section_for_address(sections, address)
    if section is not None:
        offset = section["file_address"] + address - section["virtual_address"]
        if section["file_address"] <= offset < section["file_address"] + section["file_size"]:
            return offset if offset < image_size else None
        return None
    return None


def load_xbe_layout(path: Path) -> dict[str, Any]:
    """Read the XBE section and kernel-thunk metadata without executing it."""
    data = path.read_bytes()
    if len(data) < 0x178 or data[:4] != XBE_MAGIC:
        raise ValueError(f"not an XBE image: {path}")

    base = _u32(data, 0x104)
    section_count = _u32(data, 0x11C)
    section_pointer = _u32(data, 0x120)
    section_offset = section_pointer - base
    if section_offset < 0 or section_offset + section_count * SECTION_SIZE > len(data):
        raise ValueError("XBE section table is outside the image")

    sections: list[dict[str, int]] = []
    for index in range(section_count):
        offset = section_offset + index * SECTION_SIZE
        flags, virtual_address, virtual_size, file_address, file_size = struct.unpack_from(
            "<IIIII", data, offset
        )
        sections.append(
            {
                "index": index,
                "flags": flags,
                "virtual_address": virtual_address,
                "virtual_size": virtual_size,
                "file_address": file_address,
                "file_size": file_size,
            }
        )

    encoded_thunk_pointer = _u32(data, 0x158)
    thunk_candidates = [encoded_thunk_pointer ^ key for key in KERNEL_XOR_KEYS]
    thunk_base = next(
        (
            candidate
            for candidate in thunk_candidates
            if _file_offset(sections, len(data), candidate) is not None
            and _u32(data, _file_offset(sections, len(data), candidate)) & 0x80000000
        ),
        None,
    )
    thunk_count = 0
    if thunk_base is not None:
        for index in range(4096):
            address = thunk_base + index * 4
            file_offset = _file_offset(sections, len(data), address)
            if file_offset is None or _u32(data, file_offset) == 0:
                break
            thunk_count += 1

    return {
        "data": data,
        "base_address": base,
        "sections": sections,
        "kernel_thunk_base": thunk_base,
        "kernel_thunk_count": thunk_count,
    }


def recover_indirect_sites(
    generated_dir: Path,
    functions: list[dict[str, Any]],
    directly_reachable: set[int],
) -> list[dict[str, Any]]:
    """Extract indirect call expressions from directly reachable functions."""
    address_by_name = {
        str(function.get("name", "")): _address(function["start"])
        for function in functions
    }
    function_by_address = {
        _address(function["start"]): function for function in functions
    }
    sites: list[dict[str, Any]] = []
    for source_path in sorted(generated_dir.rglob("recomp_*.c")):
        current: int | None = None
        register_aliases: dict[str, int] = {}
        register_memory_sources: dict[str, str] = {}
        register_constants: dict[str, int] = {}
        register_constant_history: dict[str, set[int]] = {}
        for line_number, line in enumerate(
            source_path.read_text(encoding="utf-8").splitlines(), 1
        ):
            symbol_match = FUNCTION_SYMBOL_RE.match(line)
            if symbol_match:
                current = address_by_name.get(symbol_match.group(1))
                register_aliases = {}
                register_memory_sources = {}
                register_constants = {}
                register_constant_history = {}
                prcb_cache_sequence_pending = False
                prcb_cache_loaded = False
                prcb_cache_guard_checked = False
            if current is None or current not in directly_reachable:
                continue
            if "eax = MEM32(XBOX_FS_BASE + 0x20);" in line:
                prcb_cache_sequence_pending = True
                prcb_cache_loaded = False
                prcb_cache_guard_checked = False
            elif (
                prcb_cache_sequence_pending
                and "eax = MEM32(eax + 0x250);" in line
            ):
                prcb_cache_sequence_pending = False
                prcb_cache_loaded = True
            elif prcb_cache_loaded and "if (CMP_EQ(_fa, _fb))" in line:
                prcb_cache_guard_checked = True
            call_match = INDIRECT_CALL_RE.search(line)
            if call_match is not None:
                expression = call_match.group("expression").strip()
                row = {
                    "function": f"0x{current:08X}",
                    "function_name": str(function_by_address[current].get("name", "")),
                    "call_site": f"0x{int(call_match.group('site'), 16):08X}",
                    "target_expression": expression,
                    "source": f"{source_path.name}:{line_number}",
                }
                if expression in register_aliases:
                    row["aliased_fixed_slot"] = f"0x{register_aliases[expression]:08X}"
                    row["target_alias_register"] = expression
                if expression in register_memory_sources:
                    row["target_source_expression"] = register_memory_sources[expression]
                memory_register_match = re.fullmatch(
                    r"MEM32\((?P<register>eax|ebx|ecx|edx|esi|edi)\)",
                    expression,
                )
                if memory_register_match is not None:
                    register = memory_register_match.group("register")
                    if register in register_constants:
                        addresses = register_constant_history.get(
                            register, {register_constants[register]}
                        )
                        if len(addresses) == 1:
                            row["target_memory_address"] = (
                                f"0x{next(iter(addresses)):08X}"
                            )
                        else:
                            row["target_memory_address_range"] = (
                                f"0x{min(addresses):08X}-0x{max(addresses):08X}"
                            )
                if (
                    expression == "MEM32(eax + 0x14)"
                    and prcb_cache_guard_checked
                ):
                    row["prcb_cache_guarded"] = True
                sites.append(row)

            assignment_match = REGISTER_ASSIGNMENT_RE.match(line)
            if assignment_match:
                register = assignment_match.group("register")
                rhs = assignment_match.group("rhs").strip()
                fixed_match = FIXED_SLOT_RE.fullmatch(rhs)
                if fixed_match:
                    register_aliases[register] = int(fixed_match.group("address"), 16)
                    register_memory_sources.pop(register, None)
                elif rhs.startswith("MEM32(") and rhs.endswith(")"):
                    register_aliases.pop(register, None)
                    register_memory_sources[register] = rhs
                else:
                    register_aliases.pop(register, None)
                    register_memory_sources.pop(register, None)
                literal_match = REGISTER_LITERAL_RE.fullmatch(rhs)
                if literal_match is not None:
                    value_text = literal_match.group("value")
                    value = int(value_text, 0)
                    register_constants[register] = value
                    register_constant_history[register] = {value}
                else:
                    register_match = REGISTER_NAME_RE.fullmatch(rhs)
                    add_match = REGISTER_ADD_RE.fullmatch(rhs)
                    if (
                        register_match is not None
                        and register_match.group("register") in register_constants
                    ):
                        source_register = register_match.group("register")
                        register_constants[register] = register_constants[
                            source_register
                        ]
                        register_constant_history[register] = set(
                            register_constant_history.get(
                                source_register, {register_constants[register]}
                            )
                        )
                    elif (
                        add_match is not None
                        and add_match.group("register") in register_constants
                    ):
                        source_register = add_match.group("register")
                        delta = int(add_match.group("value"), 0)
                        source_history = set(
                            register_constant_history.get(
                                source_register, {register_constants[source_register]}
                            )
                        )
                        register_constants[register] = (
                            register_constants[source_register] + delta
                        )
                        register_constant_history[register] = source_history | {
                            value + delta for value in source_history
                        }
                    else:
                        register_constants.pop(register, None)
                        register_constant_history.pop(register, None)
            for write_match in REGISTER_WRITE_RE.finditer(line):
                register = write_match.group("register")
                register_aliases.pop(register, None)
                register_memory_sources.pop(register, None)
                register_constants.pop(register, None)
                register_constant_history.pop(register, None)
    if not sites:
        raise ValueError("no indirect calls found in directly reachable generated code")
    return sites


def recover_runtime_fixed_slot_initializers(
    generated_dir: Path,
    functions: list[dict[str, Any]],
    directly_reachable: set[int],
) -> dict[int, list[dict[str, Any]]]:
    """Collect direct constant writes to BSS-style callback slots.

    A write is evidence of an initializer, not a claim that every possible
    runtime path reaches it.  Callers decide whether the initialized value is
    safe to classify alongside the corresponding indirect site.
    """
    address_by_name = {
        str(function.get("name", "")): _address(function["start"])
        for function in functions
    }
    function_by_address = {
        _address(function["start"]): function for function in functions
    }
    initializers: dict[int, list[dict[str, Any]]] = {}
    for source_path in sorted(generated_dir.rglob("recomp_*.c")):
        current: int | None = None
        for line_number, line in enumerate(
            source_path.read_text(encoding="utf-8").splitlines(), 1
        ):
            symbol_match = FUNCTION_SYMBOL_RE.match(line)
            if symbol_match:
                current = address_by_name.get(symbol_match.group(1))
            if current is None or current not in directly_reachable:
                continue
            write_match = FIXED_SLOT_WRITE_RE.search(line)
            if write_match is None:
                continue
            slot = int(write_match.group("slot"), 16)
            value = int(write_match.group("value"), 16)
            initializers.setdefault(slot, []).append(
                {
                    "function": f"0x{current:08X}",
                    "function_name": str(function_by_address[current].get("name", "")),
                    "source": f"{source_path.name}:{line_number}",
                    "value": f"0x{value:08X}",
                }
            )
    return initializers


def recover_prcb_cache_writes(
    generated_dir: Path,
    functions: list[dict[str, Any]],
    directly_reachable: set[int],
) -> list[dict[str, Any]]:
    """Find direct-path writes to the PRCB cache after its FS-based load.

    The ``+0x250`` offset is reused by unrelated structures, so a matching
    write is counted only while a register is proven to contain
    ``MEM32(XBOX_FS_BASE + 0x20)`` (or a direct register copy of that value).
    A zero result means that no writer was proven in the generated functions;
    it does not claim that the Xbox kernel cannot populate the cache.
    """
    address_by_name = {
        str(function.get("name", "")): _address(function["start"])
        for function in functions
    }
    function_by_address = {
        _address(function["start"]): function for function in functions
    }
    writes: list[dict[str, Any]] = []
    for source_path in sorted(generated_dir.rglob("recomp_*.c")):
        current: int | None = None
        prcb_base_registers: set[str] = set()
        for line_number, line in enumerate(
            source_path.read_text(encoding="utf-8").splitlines(), 1
        ):
            symbol_match = FUNCTION_SYMBOL_RE.match(line)
            if symbol_match:
                current = address_by_name.get(symbol_match.group(1))
                prcb_base_registers = set()
            if current is None or current not in directly_reachable:
                continue

            write_match = PRCB_CACHE_WRITE_RE.search(line)
            if (
                write_match is not None
                and write_match.group("register") in prcb_base_registers
            ):
                writes.append(
                    {
                        "function": f"0x{current:08X}",
                        "function_name": str(
                            function_by_address[current].get("name", "")
                        ),
                        "source": f"{source_path.name}:{line_number}",
                        "register": write_match.group("register"),
                        "field": "FS:[0x20] + 0x250",
                    }
                )

            assignment_match = REGISTER_ASSIGNMENT_RE.match(line)
            if assignment_match:
                register = assignment_match.group("register")
                rhs = assignment_match.group("rhs").strip()
                load_match = PRCB_BASE_LOAD_RE.match(line)
                if load_match is not None:
                    prcb_base_registers.add(register)
                elif REGISTER_NAME_RE.fullmatch(rhs) and rhs in prcb_base_registers:
                    prcb_base_registers.add(register)
                else:
                    prcb_base_registers.discard(register)

            for register_match in REGISTER_WRITE_RE.finditer(line):
                prcb_base_registers.discard(register_match.group("register"))
    return writes


def recover_kernel_resolver_cases(source_path: Path) -> dict[int, str]:
    """Read explicit ordinal cases from the host kernel resolver source.

    This proves only that the host resolver has a pointer expression for an
    ordinal.  It does not prove that the pointed-to service has Xbox-compatible
    behavior, ABI, side effects, or hardware semantics.
    """
    cases: dict[int, str] = {}
    for line_number, line in enumerate(
        source_path.read_text(encoding="utf-8").splitlines(), 1
    ):
        match = KERNEL_RESOLVER_CASE_RE.match(line)
        if match is None:
            continue
        ordinal = int(match.group("ordinal"), 10)
        target = match.group("target").strip()
        previous = cases.get(ordinal)
        if previous is not None and previous != target:
            raise ValueError(
                f"conflicting kernel resolver cases for ordinal {ordinal} "
                f"at {source_path}:{line_number}"
            )
        cases[ordinal] = target
    return cases


def recover_dynamic_target_writes(
    generated_dir: Path,
    functions: list[dict[str, Any]],
    directly_reachable: set[int],
    memory_ranges: list[tuple[int, int]],
) -> dict[str, list[dict[str, Any]]]:
    """Inventory direct-path writes matching unresolved callback sources.

    The result records shape-level writer evidence only.  It does not prove
    that a writer reaches the same object or record instance as a callback
    site.  Register-derived writes to the absolute dynamic-memory range are
    included only when the register has a tracked constant value in that
    range; generic ``+0x584`` and indexed-record shapes are kept separate.
    """
    address_by_name = {
        str(function.get("name", "")): _address(function["start"])
        for function in functions
    }
    function_by_address = {
        _address(function["start"]): function for function in functions
    }
    writers: dict[str, list[dict[str, Any]]] = {
        "dynamic-object-field-target": [],
        "dynamic-indexed-record-target": [],
    }
    if memory_ranges:
        writers["dynamic-memory-target"] = []

    def in_memory_range(address: int) -> bool:
        return any(start <= address <= end for start, end in memory_ranges)

    def add_writer(
        family: str,
        source_path: Path,
        line_number: int,
        current: int,
        expression: str,
    ) -> None:
        writers[family].append(
            {
                "function": f"0x{current:08X}",
                "function_name": str(function_by_address[current].get("name", "")),
                "source": f"{source_path.name}:{line_number}",
                "expression": expression,
            }
        )

    for source_path in sorted(generated_dir.rglob("recomp_*.c")):
        current: int | None = None
        register_constants: dict[str, int] = {}
        for line_number, line in enumerate(
            source_path.read_text(encoding="utf-8").splitlines(), 1
        ):
            symbol_match = FUNCTION_SYMBOL_RE.match(line)
            if symbol_match:
                current = address_by_name.get(symbol_match.group(1))
                register_constants = {}
            if current is None or current not in directly_reachable:
                continue

            object_match = DYNAMIC_OBJECT_FIELD_WRITE_RE.search(line)
            if object_match is not None:
                add_writer(
                    "dynamic-object-field-target",
                    source_path,
                    line_number,
                    current,
                    line.strip(),
                )
            indexed_match = DYNAMIC_INDEXED_RECORD_WRITE_RE.search(line)
            if indexed_match is not None:
                add_writer(
                    "dynamic-indexed-record-target",
                    source_path,
                    line_number,
                    current,
                    line.strip(),
                )
            absolute_match = DYNAMIC_ABSOLUTE_WRITE_RE.search(line)
            if absolute_match is not None and in_memory_range(
                int(absolute_match.group("address"), 16)
            ):
                add_writer(
                    "dynamic-memory-target",
                    source_path,
                    line_number,
                    current,
                    line.strip(),
                )
            register_write_match = DYNAMIC_REGISTER_WRITE_RE.search(line)
            if (
                register_write_match is not None
                and register_write_match.group("register") in register_constants
                and in_memory_range(
                    register_constants[register_write_match.group("register")]
                )
            ):
                add_writer(
                    "dynamic-memory-target",
                    source_path,
                    line_number,
                    current,
                    line.strip(),
                )

            assignment_match = REGISTER_ASSIGNMENT_RE.match(line)
            if assignment_match:
                register = assignment_match.group("register")
                rhs = assignment_match.group("rhs").strip()
                literal_match = REGISTER_LITERAL_RE.fullmatch(rhs)
                register_match = REGISTER_NAME_RE.fullmatch(rhs)
                add_match = REGISTER_ADD_RE.fullmatch(rhs)
                if literal_match is not None:
                    register_constants[register] = int(
                        literal_match.group("value"), 0
                    )
                elif (
                    register_match is not None
                    and register_match.group("register") in register_constants
                ):
                    register_constants[register] = register_constants[
                        register_match.group("register")
                    ]
                elif (
                    add_match is not None
                    and add_match.group("register") in register_constants
                ):
                    register_constants[register] = register_constants[
                        add_match.group("register")
                    ] + int(add_match.group("value"), 0)
                else:
                    register_constants.pop(register, None)
            for write_match in REGISTER_WRITE_RE.finditer(line):
                register_constants.pop(write_match.group("register"), None)
    return writers


def _dynamic_target_family(site: dict[str, Any]) -> str:
    """Name the conservative data-flow shape, without resolving its value."""
    if site.get("prcb_cache_guarded"):
        return "prcb-guarded-dynamic-callback"
    source_expression = site.get(
        "target_source_expression", site["target_expression"]
    )
    if source_expression == "MEM32(ecx + 0x584)":
        return "dynamic-object-field-target"
    if source_expression == "MEM32(ebx + esi * 4 + 8)":
        return "dynamic-indexed-record-target"
    if source_expression.startswith("MEM32("):
        return "dynamic-memory-target"
    return "dynamic-register-or-object-target"


def classify_sites(
    sites: list[dict[str, Any]],
    xbe: dict[str, Any],
    function_starts: set[int],
    runtime_initializers: dict[int, list[dict[str, Any]]] | None = None,
    kernel_resolver_cases: dict[int, str] | None = None,
    dynamic_target_writers: dict[str, list[dict[str, Any]]] | None = None,
) -> list[dict[str, Any]]:
    """Classify each site without resolving dynamic object or register calls."""
    data: bytes = xbe["data"]
    sections: list[dict[str, int]] = xbe["sections"]
    thunk_base = xbe["kernel_thunk_base"]
    thunk_count = xbe["kernel_thunk_count"]
    runtime_initializers = runtime_initializers or {}
    resolver_audited = kernel_resolver_cases is not None
    kernel_resolver_cases = kernel_resolver_cases or {}
    dynamic_target_writers = dynamic_target_writers or {}
    rows: list[dict[str, Any]] = []
    for site in sites:
        row = dict(site)
        expression = site["target_expression"]
        if site.get("prcb_cache_guarded"):
            row["classification"] = "prcb-guarded-dynamic-callback"
            rows.append(row)
            continue
        fixed_match = FIXED_SLOT_RE.fullmatch(expression)
        aliased_slot = site.get("aliased_fixed_slot")
        if fixed_match is None and aliased_slot is None:
            row["classification"] = _dynamic_target_family(site)
            writer_sites = dynamic_target_writers.get(row["classification"])
            if writer_sites is not None:
                row["direct_writer_status"] = (
                    "direct-writer-shape-observed"
                    if writer_sites
                    else "no-proven-direct-writer"
                )
                if writer_sites:
                    row["direct_writer_sites"] = writer_sites
            if "target_memory_address" in row:
                target_address = int(str(row["target_memory_address"]), 0)
                target_offset = _file_offset(sections, len(data), target_address)
                row["target_memory_raw_value"] = (
                    None
                    if target_offset is None
                    else f"0x{_u32(data, target_offset):08X}"
                )
            elif "target_memory_address_range" in row:
                start_text, end_text = str(row["target_memory_address_range"]).split("-")
                start_address = int(start_text, 0)
                end_address = int(end_text, 0)
                raw_values: dict[str, str | None] = {}
                for address in range(start_address, end_address + 1, 4):
                    target_offset = _file_offset(sections, len(data), address)
                    raw_values[f"0x{address:08X}"] = (
                        None
                        if target_offset is None
                        else f"0x{_u32(data, target_offset):08X}"
                    )
                row["target_memory_raw_values"] = raw_values
            rows.append(row)
            continue

        slot = int(fixed_match.group("address"), 16) if fixed_match else int(str(aliased_slot), 0)
        row["fixed_slot"] = f"0x{slot:08X}"
        if fixed_match is None:
            row["classification_source"] = "linear-register-alias"
        file_offset = _file_offset(sections, len(data), slot)
        raw_value = _u32(data, file_offset) if file_offset is not None else None
        row["raw_value"] = None if raw_value is None else f"0x{raw_value:08X}"
        if (
            thunk_base is not None
            and thunk_base <= slot < thunk_base + thunk_count * 4
            and raw_value is not None
            and raw_value & 0x80000000
        ):
            row["classification"] = "kernel-thunk-slot"
            row["kernel_ordinal"] = raw_value & 0x7FFFFFFF
            if resolver_audited:
                resolver_target = kernel_resolver_cases.get(row["kernel_ordinal"])
                row["host_resolver_status"] = (
                    "explicit-case" if resolver_target is not None else "missing-case"
                )
                if resolver_target is not None:
                    row["host_resolver_target"] = resolver_target
        elif raw_value is None:
            initializers = runtime_initializers.get(slot, [])
            initializer_values = {int(str(item["value"]), 0) for item in initializers}
            if (
                len(initializer_values) == 1
                and next(iter(initializer_values)) in function_starts
            ):
                target = next(iter(initializer_values))
                row["classification"] = "runtime-initialized-known-function-pointer"
                row["target_function"] = f"0x{target:08X}"
                row["runtime_initializer_sites"] = initializers
            else:
                row["classification"] = "runtime-initialized-fixed-slot"
        elif raw_value in function_starts:
            row["classification"] = "fixed-known-function-pointer"
            row["target_function"] = f"0x{raw_value:08X}"
        else:
            row["classification"] = "fixed-data-pointer-not-proven-function"
        rows.append(row)
    return rows


def audit(
    functions_path: Path,
    generated_dir: Path,
    xbe_path: Path,
    entry: int = 0x0024D8BE,
    kernel_source: Path | None = None,
) -> dict[str, Any]:
    functions = json.loads(functions_path.read_text(encoding="utf-8"))
    directly_reachable, external_targets = recover_direct_reachable(functions, entry)
    sites = recover_indirect_sites(generated_dir, functions, directly_reachable)
    runtime_initializers = recover_runtime_fixed_slot_initializers(
        generated_dir, functions, directly_reachable
    )
    prcb_cache_writes = recover_prcb_cache_writes(
        generated_dir, functions, directly_reachable
    )
    kernel_resolver_cases = (
        None
        if kernel_source is None
        else recover_kernel_resolver_cases(kernel_source)
    )
    dynamic_memory_ranges = []
    for site in sites:
        range_text = site.get("target_memory_address_range")
        if range_text is None:
            continue
        start_text, end_text = str(range_text).split("-")
        dynamic_memory_ranges.append((int(start_text, 0), int(end_text, 0)))
    dynamic_target_writers = recover_dynamic_target_writes(
        generated_dir,
        functions,
        directly_reachable,
        dynamic_memory_ranges,
    )
    xbe = load_xbe_layout(xbe_path)
    classified = classify_sites(
        sites,
        xbe,
        {_address(function["start"]) for function in functions},
        runtime_initializers,
        kernel_resolver_cases,
        dynamic_target_writers,
    )
    classifications = Counter(row["classification"] for row in classified)
    fixed_slots = [row for row in classified if "fixed_slot" in row]
    kernel_slots = [row for row in classified if row["classification"] == "kernel-thunk-slot"]
    resolver_case_sites = [
        row for row in kernel_slots if row.get("host_resolver_status") == "explicit-case"
    ]
    resolver_missing_sites = [
        row for row in kernel_slots if row.get("host_resolver_status") == "missing-case"
    ]
    dynamic_writer_counts = {
        family: len(rows)
        for family, rows in dynamic_target_writers.items()
    }
    aliased_sites = [row for row in classified if row.get("classification_source") == "linear-register-alias"]
    summary = {
        "entry_point": f"0x{entry:08X}",
        "directly_reachable_functions": len(directly_reachable),
        "direct_external_call_targets": len(external_targets),
        "indirect_call_sites": len(classified),
        "functions_with_indirect_calls": len({row["function"] for row in classified}),
        "fixed_slot_sites": len(fixed_slots),
        "unique_fixed_slots": len({row["fixed_slot"] for row in fixed_slots}),
        "linear_register_alias_sites": len(aliased_sites),
        "kernel_thunk_slot_sites": len(kernel_slots),
        "unique_kernel_thunk_slots": len({row["fixed_slot"] for row in kernel_slots}),
        "kernel_resolver_status": (
            "not-supplied"
            if kernel_resolver_cases is None
            else "verified-explicit-case-coverage"
        ),
        "kernel_resolver_case_sites": (
            None if kernel_resolver_cases is None else len(resolver_case_sites)
        ),
        "kernel_resolver_missing_sites": (
            None if kernel_resolver_cases is None else len(resolver_missing_sites)
        ),
        "dynamic_target_direct_writer_sites": sum(dynamic_writer_counts.values()),
        "dynamic_target_direct_writer_counts": dynamic_writer_counts,
        "dynamic_target_direct_writer_status": (
            "direct-writer-shape-observed"
            if sum(dynamic_writer_counts.values())
            else "no-proven-direct-writer"
        ),
        "dynamic_register_or_object_targets": sum(
            classifications[name]
            for name in (
                "dynamic-register-or-object-target",
                "dynamic-memory-target",
                "dynamic-object-field-target",
                "dynamic-indexed-record-target",
            )
        ),
        "dynamic_target_family_counts": {
            name: classifications[name]
            for name in (
                "dynamic-memory-target",
                "dynamic-object-field-target",
                "dynamic-indexed-record-target",
                "dynamic-register-or-object-target",
                "prcb-guarded-dynamic-callback",
            )
            if classifications[name]
        },
        "prcb_guarded_dynamic_callback_sites": classifications[
            "prcb-guarded-dynamic-callback"
        ],
        "prcb_cache_direct_writer_sites": len(prcb_cache_writes),
        "prcb_cache_direct_writer_status": (
            "direct-writer-observed"
            if prcb_cache_writes
            else "no-proven-direct-writer"
        ),
        "unresolved_dynamic_sites": classifications[
            "dynamic-register-or-object-target"
        ]
        + classifications["dynamic-memory-target"]
        + classifications["dynamic-object-field-target"]
        + classifications["dynamic-indexed-record-target"]
        + classifications["prcb-guarded-dynamic-callback"],
        "fixed_known_function_pointer_sites": classifications[
            "fixed-known-function-pointer"
        ],
        "runtime_initialized_fixed_slot_sites": classifications[
            "runtime-initialized-fixed-slot"
        ],
        "runtime_initialized_known_function_pointer_sites": classifications[
            "runtime-initialized-known-function-pointer"
        ],
        "fixed_data_pointer_not_proven_function_sites": classifications[
            "fixed-data-pointer-not-proven-function"
        ],
        "kernel_thunk_table_base": (
            None
            if xbe["kernel_thunk_base"] is None
            else f"0x{xbe['kernel_thunk_base']:08X}"
        ),
        "kernel_thunk_table_entries": xbe["kernel_thunk_count"],
        "status": "verified-static-indirect-target-classification",
        "original_semantics": "not-claimed",
    }
    return {
        "summary": summary,
        "sites": classified,
        "prcb_cache_writes": prcb_cache_writes,
    }


def _parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--functions", type=Path, required=True)
    parser.add_argument("--generated", type=Path, required=True)
    parser.add_argument("--xbe", type=Path, required=True)
    parser.add_argument(
        "--kernel-source",
        type=Path,
        help="optional host kernel_thunks.c to audit explicit ordinal cases",
    )
    parser.add_argument("--entry", type=lambda value: int(value, 0), default=0x0024D8BE)
    parser.add_argument("--format", choices=("json", "text"), default="text")
    return parser


def main() -> int:
    args = _parser().parse_args()
    result = audit(
        args.functions,
        args.generated,
        args.xbe,
        args.entry,
        args.kernel_source,
    )
    if args.format == "json":
        print(json.dumps(result, indent=2, sort_keys=True))
    else:
        for key, value in result["summary"].items():
            print(f"{key}={value}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
