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
FIXED_SLOT_ANY_WRITE_RE = re.compile(
    r"MEM32\(0x(?P<slot>[0-9A-Fa-f]{1,8})\)\s*=\s*"
    r"(?P<rhs>[^;]+);"
)
FIXED_REGISTER_WRITE_RE = re.compile(
    r"MEM32\((?P<register>eax|ebx|ecx|edx|esi|edi)\)\s*=\s*"
    r"(?P<rhs>[^;]+);"
)
PRCB_BASE_LOAD_RE = re.compile(
    r"^\s*(?P<register>eax|ebx|ecx|edx|esi|edi)\s*=\s*"
    r"MEM32\(XBOX_FS_BASE \+ 0x20\);"
)
PRCB_CACHE_WRITE_RE = re.compile(
    r"MEM32\((?P<register>eax|ebx|ecx|edx|esi|edi)\s*\+\s*0x250\)\s*="
)
PRCB_CACHE_ADDRESS_ALIAS_RE = re.compile(
    r"^(?P<base>eax|ebx|ecx|edx|esi|edi)\s*\+\s*0x250$"
)
KERNEL_RESOLVER_CASE_RE = re.compile(
    r"^\s*case\s+(?P<ordinal>[0-9]+)\s*:\s*return\s+"
    r"\(ULONG_PTR\)(?P<target>[^;]+);"
)
KERNEL_FUNCTION_DEFINITION_RE = re.compile(
    r"(?ms)^\s*(?:(?:static|inline|__declspec\([^)]*\))\s+)*"
    r"(?:[A-Za-z_][A-Za-z0-9_]*\s+)+"
    r"(?P<name>xbox_[A-Za-z0-9_]+)\s*\([^;{}]*\)\s*\{"
)
KERNEL_IMPLEMENTATION_STUB_MARKER_RE = re.compile(
    r"(?i)\b(?:stub(?:bed)?|no-op)\b"
)
KERNEL_IMPLEMENTATION_STATUS_MARKER_RE = re.compile(
    r"\bSTATUS_NOT_IMPLEMENTED\b"
)
SYMBOLIC_LINK_OBJECT_NAME_ASSIGNMENT_RE = re.compile(
    r"MEM32\([^;]*\+\s*-16\)\s*=\s*"
    r"0x(?P<address>[0-9A-Fa-f]{1,8})u?;"
)
SYMBOLIC_LINK_HOST_TARGET_RE = re.compile(
    r'\bconst\s+char\s*\*\s*target\s*=\s*"(?P<target>[^"]+)";'
)
KERNEL_BRIDGE_FUNCTION_RE = re.compile(
    r"^\s*static\s+bridge_func_t\s+bridge_for_ordinal\s*\(ULONG\s+ordinal\)\s*$"
)
KERNEL_BRIDGE_CASE_RE = re.compile(
    r"^\s*case\s+(?P<ordinal>[0-9]+)\s*:\s*return\s+"
    r"(?P<target>bridge_[^;]+);"
)
KERNEL_STDCALL_FUNCTION_RE = re.compile(
    r"^\s*static\s+int\s+stdcall_args_for_ordinal\s*\(ULONG\s+ordinal\s*\)\s*$"
)
KERNEL_STDCALL_CASE_RE = re.compile(
    r"^\s*case\s+(?P<ordinal>[0-9]+)\s*:\s*return\s+"
    r"(?P<bytes>[0-9]+);"
)
PUSH32_RE = re.compile(r"^\s*PUSH32\(esp,\s*(?P<operand>.*)\);\s*$")
KERNEL_FILE_ENUM_RE = re.compile(
    r"^\s*(?P<name>XboxFile[A-Za-z0-9_]+)\s*=\s*"
    r"(?P<value>0x[0-9A-Fa-f]+|[0-9]+)\s*,?\s*$"
)
KERNEL_FILE_FUNCTION_RE = re.compile(
    r"(?ms)^\s*(?:[A-Za-z_][A-Za-z0-9_]*\s+)*"
    r"(?P<name>xbox_Nt(?:Query|Set)InformationFile)\s*"
    r"\([^;{}]*\)\s*\{"
)
KERNEL_FILE_CASE_RE = re.compile(
    r"\bcase\s+(?P<name>XboxFile[A-Za-z0-9_]+)\s*:"
)
CONSTANT_OPERAND_RE = re.compile(r"^(?P<value>0x[0-9A-Fa-f]+|[0-9]+)u?$")
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
DYNAMIC_WRITER_ASSIGNMENT_RE = re.compile(r"=\s*(?P<value>[^;]+);")
DYNAMIC_OBJECT_FIELD_ALIAS_RE = re.compile(
    r"^(?P<base>eax|ebx|ecx|edx|esi|edi)\s*\+\s*0x584$"
)
DYNAMIC_INDEXED_RECORD_ALIAS_RE = re.compile(
    r"^(?P<base>eax|ebx|ecx|edx|esi|edi)\s*\+\s*"
    r"(?P<index>eax|ebx|ecx|edx|esi|edi)\s*\*\s*4\s*\+\s*8$"
)
LOCAL_BRANCH_RE = re.compile(
    r"^\s*if\s*\((?P<condition>.+)\)\s+goto\s+(?P<label>loc_[0-9A-Fa-f]+);"
)
LOCAL_LABEL_RE = re.compile(r"^\s*(?P<label>loc_[0-9A-Fa-f]+):\s*;")
XBE_MAGIC = b"XBEH"
SECTION_SIZE = 0x38
KERNEL_XOR_KEYS = (0x5B6D40B6, 0xEFB1F152)


def _address(value: Any) -> int:
    if isinstance(value, int):
        return value
    return int(str(value), 0)


def _u32(data: bytes, offset: int) -> int:
    return struct.unpack_from("<I", data, offset)[0]


def _dynamic_writer_value_kind(value: str) -> str:
    value = value.strip()
    if REGISTER_LITERAL_RE.fullmatch(value) is not None:
        return "constant"
    if REGISTER_NAME_RE.fullmatch(value) is not None:
        return "register"
    if value.startswith("MEM32(") and value.endswith(")"):
        return "memory-read"
    return "computed-expression"


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
    directly_reachable: set[int] | None,
) -> dict[int, list[dict[str, Any]]]:
    """Collect constant writes to BSS-style callback slots.

    A write is evidence of an initializer, not a claim that every possible
    runtime path reaches it.  ``directly_reachable=None`` inventories all
    generated functions; callers decide whether the initialized value is safe
    to classify alongside the corresponding indirect site.
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
            if current is None or (
                directly_reachable is not None
                and current not in directly_reachable
            ):
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


def recover_fixed_slot_writes(
    generated_dir: Path,
    functions: list[dict[str, Any]],
    directly_reachable: set[int] | None,
    tracked_slots: set[int],
) -> dict[int, list[dict[str, Any]]]:
    """Inventory absolute and register-derived writes to selected slots.

    This is broader than the constant-initializer helper: it also recognizes
    ``register = slot`` followed by ``MEM32(register) = ...``.  The result is
    still syntactic provenance only and does not prove that a runtime path
    reaches the writer.
    """
    address_by_name = {
        str(function.get("name", "")): _address(function["start"])
        for function in functions
    }
    function_by_address = {
        _address(function["start"]): function for function in functions
    }
    writes: dict[int, list[dict[str, Any]]] = {}

    def add_write(
        slot: int,
        source_path: Path,
        line_number: int,
        current: int,
        kind: str,
        expression: str,
        value: str | None = None,
    ) -> None:
        row: dict[str, Any] = {
            "function": f"0x{current:08X}",
            "function_name": str(function_by_address[current].get("name", "")),
            "source": f"{source_path.name}:{line_number}",
            "kind": kind,
            "expression": expression,
        }
        if value is not None:
            row["value"] = value
        writes.setdefault(slot, []).append(row)

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
            if current is None or (
                directly_reachable is not None
                and current not in directly_reachable
            ):
                continue

            absolute_match = FIXED_SLOT_ANY_WRITE_RE.search(line)
            if absolute_match is not None:
                slot = int(absolute_match.group("slot"), 16)
                if slot in tracked_slots:
                    constant_match = FIXED_SLOT_WRITE_RE.search(line)
                    value = (
                        None
                        if constant_match is None
                        else f"0x{int(constant_match.group('value'), 16):08X}"
                    )
                    add_write(
                        slot,
                        source_path,
                        line_number,
                        current,
                        "absolute-constant" if value is not None else "absolute",
                        line.strip(),
                        value,
                    )

            register_match = FIXED_REGISTER_WRITE_RE.search(line)
            if register_match is not None:
                register = register_match.group("register")
                slot = register_constants.get(register)
                if slot in tracked_slots:
                    add_write(
                        slot,
                        source_path,
                        line_number,
                        current,
                        "register-derived",
                        line.strip(),
                        register_match.group("rhs").strip(),
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
    return writes


def recover_indirect_call_skip_branches(
    generated_dir: Path,
    sites: list[dict[str, Any]],
    window: int = 32,
) -> dict[str, dict[str, Any]]:
    """Find nearby local branches whose target is after an indirect call.

    This is deliberately weaker than proving a null guard: it records only
    that generated control flow has a conditional jump before the call to a
    local label after the call.  The condition may depend on other state, so
    callers must not use this as a resolved-target claim.
    """
    source_paths = {
        path.name: path for path in generated_dir.rglob("recomp_*.c")
    }
    evidence: dict[str, dict[str, Any]] = {}
    for site in sites:
        source_text = str(site.get("source", ""))
        source_name, separator, line_text = source_text.rpartition(":")
        if not separator or not line_text.isdigit():
            continue
        source_path = source_paths.get(source_name)
        if source_path is None:
            continue
        lines = source_path.read_text(encoding="utf-8").splitlines()
        call_index = int(line_text) - 1
        if not 0 <= call_index < len(lines):
            continue

        function_start = 0
        for index in range(call_index, -1, -1):
            if FUNCTION_SYMBOL_RE.match(lines[index]):
                function_start = index
                break
        label_lines = {}
        for index in range(call_index + 1, len(lines)):
            label_match = LOCAL_LABEL_RE.match(lines[index])
            if label_match is not None:
                label_lines[label_match.group("label")] = index

        candidates: list[tuple[int, int, str, str]] = []
        first_index = max(function_start, call_index - window)
        for index in range(first_index, call_index):
            branch_match = LOCAL_BRANCH_RE.match(lines[index])
            if branch_match is None:
                continue
            label = branch_match.group("label")
            target_index = label_lines.get(label)
            if target_index is None or target_index <= call_index:
                continue
            candidates.append(
                (
                    index,
                    target_index,
                    branch_match.group("condition"),
                    label,
                )
            )
        if not candidates:
            continue
        branch_index, target_index, condition, label = max(
            candidates, key=lambda item: item[0]
        )
        call_site = str(site["call_site"])
        evidence[call_site] = {
            "kind": "conditional-branch-skips-call",
            "branch_source": f"{source_path.name}:{branch_index + 1}",
            "condition": condition,
            "target_label": label,
            "target_source": f"{source_path.name}:{target_index + 1}",
        }
    return evidence


def recover_indirect_call_nonzero_guards(
    generated_dir: Path,
    sites: list[dict[str, Any]],
    window: int = 32,
) -> dict[str, dict[str, Any]]:
    """Find a local non-zero branch that gates an indirect call.

    The generated form is intentionally narrow: a nearby ``CMP_NE`` branch
    must target a label immediately before the call, and the same bounded
    block must mention the call's recovered target expression.  This records
    control-flow evidence only; it does not prove the slot is always zero or
    that the target has valid ABI/semantics when populated.
    """
    source_paths = {
        path.name: path for path in generated_dir.rglob("recomp_*.c")
    }
    evidence: dict[str, dict[str, Any]] = {}
    for site in sites:
        source_text = str(site.get("source", ""))
        source_name, separator, line_text = source_text.rpartition(":")
        if not separator or not line_text.isdigit():
            continue
        source_path = source_paths.get(source_name)
        if source_path is None:
            continue
        lines = source_path.read_text(encoding="utf-8").splitlines()
        call_index = int(line_text) - 1
        if not 0 <= call_index < len(lines):
            continue
        function_start = 0
        for index in range(call_index, -1, -1):
            if FUNCTION_SYMBOL_RE.match(lines[index]):
                function_start = index
                break
        label_lines: dict[str, int] = {}
        for index in range(function_start, call_index + 1):
            label_match = LOCAL_LABEL_RE.match(lines[index])
            if label_match is not None:
                label_lines[label_match.group("label")] = index

        first_index = max(function_start, call_index - window)
        for index in range(first_index, call_index):
            branch_match = LOCAL_BRANCH_RE.match(lines[index])
            if branch_match is None or "CMP_NE" not in branch_match.group("condition"):
                continue
            label = branch_match.group("label")
            target_index = label_lines.get(label)
            if target_index is None or target_index <= index or target_index > call_index:
                continue
            target_block = lines[target_index : call_index + 1]
            if not any("RECOMP_ICALL_SAFE_AT" in line for line in target_block):
                continue
            target_expression = str(site.get("target_expression", ""))
            guard_context = lines[max(function_start, index - 6) : index + 1]
            if target_expression and not any(
                target_expression in line for line in guard_context
            ):
                continue
            call_site = str(site["call_site"])
            evidence[call_site] = {
                "kind": "nonzero-guarded-indirect-call",
                "branch_source": f"{source_path.name}:{index + 1}",
                "condition": branch_match.group("condition"),
                "target_label": label,
                "target_source": f"{source_path.name}:{target_index + 1}",
                "target_expression": target_expression,
            }
            break
    return evidence


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
        prcb_cache_address_aliases: dict[str, str] = {}
        for line_number, line in enumerate(
            source_path.read_text(encoding="utf-8").splitlines(), 1
        ):
            symbol_match = FUNCTION_SYMBOL_RE.match(line)
            if symbol_match:
                current = address_by_name.get(symbol_match.group(1))
                prcb_base_registers = set()
                prcb_cache_address_aliases = {}
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
            register_write_match = DYNAMIC_REGISTER_WRITE_RE.search(line)
            if register_write_match is not None:
                alias_register = register_write_match.group("register")
                if alias_register in prcb_cache_address_aliases:
                    writes.append(
                        {
                            "function": f"0x{current:08X}",
                            "function_name": str(
                                function_by_address[current].get("name", "")
                            ),
                            "source": f"{source_path.name}:{line_number}",
                            "register": alias_register,
                            "address_expression": prcb_cache_address_aliases[
                                alias_register
                            ],
                            "field": "FS:[0x20] + 0x250",
                        }
                    )

            assignment_match = REGISTER_ASSIGNMENT_RE.match(line)
            if assignment_match:
                register = assignment_match.group("register")
                rhs = assignment_match.group("rhs").strip()
                load_match = PRCB_BASE_LOAD_RE.match(line)
                add_match = PRCB_CACHE_ADDRESS_ALIAS_RE.fullmatch(rhs)
                if load_match is not None:
                    prcb_base_registers.add(register)
                    prcb_cache_address_aliases.pop(register, None)
                elif REGISTER_NAME_RE.fullmatch(rhs) and rhs in prcb_base_registers:
                    prcb_base_registers.add(register)
                    prcb_cache_address_aliases.pop(register, None)
                elif (
                    add_match is not None
                    and add_match.group("base") in prcb_base_registers
                ):
                    prcb_base_registers.discard(register)
                    prcb_cache_address_aliases[register] = rhs
                else:
                    prcb_base_registers.discard(register)
                    prcb_cache_address_aliases.pop(register, None)

            for register_match in REGISTER_WRITE_RE.finditer(line):
                prcb_base_registers.discard(register_match.group("register"))
                prcb_cache_address_aliases.pop(register_match.group("register"), None)
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


def recover_kernel_implementation_evidence(
    resolver_source: Path,
    resolver_cases: dict[int, str],
) -> dict[str, dict[str, Any]]:
    """Locate source definitions for functions returned by the kernel resolver.

    A definition is only source-presence evidence.  It does not prove ABI,
    Xbox-compatible behavior, side effects, or that a body marked as a stub is
    sufficient for the game's call site.  The resolver source's directory is
    treated as the root because the implementations live beside
    ``kernel_thunks.c`` in the pinned XboxRecomp source tree.
    """
    target_names = sorted(
        {
            target.strip().lstrip("&")
            for target in resolver_cases.values()
            if target.strip().lstrip("&").startswith("xbox_")
        }
    )
    evidence = {
        target: {
            "status": "missing-source-definition",
            "sources": [],
            "body_markers": [],
        }
        for target in target_names
    }
    source_root = resolver_source.parent
    for source_path in sorted(source_root.rglob("*.c")):
        text = source_path.read_text(encoding="utf-8")
        for match in KERNEL_FUNCTION_DEFINITION_RE.finditer(text):
            target = match.group("name")
            if target not in evidence:
                continue
            line_number = text.count("\n", 0, match.start()) + 1
            relative_source = source_path.relative_to(source_root).as_posix()
            evidence[target]["sources"].append(
                f"{relative_source}:{line_number}"
            )
            evidence[target]["status"] = "source-definition-found"
            body_start = match.end() - 1
            depth = 0
            body_end: int | None = None
            for index in range(body_start, len(text)):
                character = text[index]
                if character == "{":
                    depth += 1
                elif character == "}":
                    depth -= 1
                    if depth == 0:
                        body_end = index
                        break
            if body_end is None:
                continue
            for marker_re, marker_kind in (
                (
                    KERNEL_IMPLEMENTATION_STUB_MARKER_RE,
                    "explicit-stub-or-no-op",
                ),
                (
                    KERNEL_IMPLEMENTATION_STATUS_MARKER_RE,
                    "status-not-implemented-token",
                ),
            ):
                for marker_match in marker_re.finditer(
                    text, body_start, body_end + 1
                ):
                    marker = marker_match.group(0)
                    marker_source = {
                        "kind": marker_kind,
                        "marker": marker,
                        "source": (
                            f"{relative_source}:"
                            f"{text.count(chr(10), 0, marker_match.start()) + 1}"
                        ),
                    }
                    if marker_source not in evidence[target]["body_markers"]:
                        evidence[target]["body_markers"].append(marker_source)
    return evidence


def recover_kernel_file_information_evidence(
    source_root: Path,
) -> dict[str, Any]:
    """Recover enum values and implemented file-information switch cases.

    This is source-coverage evidence for the host implementations selected by
    the observed call sites.  It does not prove that the host structures,
    handles, status codes, or side effects are fully Xbox-compatible.
    """
    enum_values: dict[str, int] = {}
    enum_path = source_root / "kernel.h"
    if enum_path.exists():
        for line in enum_path.read_text(encoding="utf-8").splitlines():
            match = KERNEL_FILE_ENUM_RE.match(line)
            if match is not None:
                enum_values[match.group("name")] = int(match.group("value"), 0)

    functions: dict[str, dict[str, Any]] = {
        name: {
            "status": "missing-source-cases",
            "case_names": [],
            "case_values": {},
            "sources": [],
        }
        for name in (
            "xbox_NtQueryInformationFile",
            "xbox_NtSetInformationFile",
        )
    }
    source_path = source_root / "kernel_file.c"
    if not source_path.exists():
        return {
            "status": "missing-kernel-file-source",
            "enum_values": enum_values,
            "functions": functions,
        }

    source_text = source_path.read_text(encoding="utf-8")
    for function_match in KERNEL_FILE_FUNCTION_RE.finditer(source_text):
        function_name = function_match.group("name")
        body_start = function_match.end() - 1
        depth = 0
        body_end: int | None = None
        for index in range(body_start, len(source_text)):
            character = source_text[index]
            if character == "{":
                depth += 1
            elif character == "}":
                depth -= 1
                if depth == 0:
                    body_end = index
                    break
        if body_end is None:
            continue

        evidence = functions[function_name]
        function_line = source_text.count("\n", 0, function_match.start()) + 1
        relative_source = source_path.relative_to(source_root).as_posix()
        evidence["sources"].append(f"{relative_source}:{function_line}")
        for case_match in KERNEL_FILE_CASE_RE.finditer(
            source_text, function_match.end(), body_end
        ):
            case_name = case_match.group("name")
            if case_name not in enum_values:
                continue
            if case_name not in evidence["case_names"]:
                evidence["case_names"].append(case_name)
            evidence["case_values"][case_name] = enum_values[case_name]
        if evidence["case_names"]:
            evidence["status"] = "source-cases-found"

    for evidence in functions.values():
        evidence["case_names"].sort()
        evidence["sources"] = sorted(set(evidence["sources"]))
    return {
        "status": "source-cases-found"
        if all(item["status"] == "source-cases-found" for item in functions.values())
        else "missing-source-cases",
        "enum_values": enum_values,
        "functions": functions,
    }


def recover_kernel_bridge_cases(source_path: Path) -> dict[int, str]:
    """Read ordinal-to-bridge wrappers from ``kernel_bridge.c``.

    This is separate from the typed service resolver: the bridge is the ABI
    adapter that reads guest stack/register state and translates guest pointers
    before calling the host implementation.  A bridge case still does not
    prove the service's Xbox semantics.
    """
    lines = source_path.read_text(encoding="utf-8").splitlines()
    in_bridge = False
    entered_body = False
    depth = 0
    cases: dict[int, str] = {}
    for line in lines:
        if not in_bridge:
            if KERNEL_BRIDGE_FUNCTION_RE.match(line) is None:
                continue
            in_bridge = True
        depth += line.count("{") - line.count("}")
        if depth > 0:
            entered_body = True
        match = KERNEL_BRIDGE_CASE_RE.match(line)
        if match is not None:
            ordinal = int(match.group("ordinal"), 10)
            target = match.group("target").strip()
            previous = cases.get(ordinal)
            if previous is not None and previous != target:
                raise ValueError(
                    f"conflicting kernel bridge cases for ordinal {ordinal} "
                    f"at {source_path}"
                )
            cases[ordinal] = target
        if entered_body and depth == 0:
            break
    return cases


def recover_kernel_stdcall_arg_bytes(source_path: Path) -> dict[int, int]:
    """Read the bridge's expected caller-stack byte count per ordinal."""
    lines = source_path.read_text(encoding="utf-8").splitlines()
    in_function = False
    entered_body = False
    depth = 0
    cases: dict[int, int] = {}
    for line in lines:
        if not in_function:
            if KERNEL_STDCALL_FUNCTION_RE.match(line) is None:
                continue
            in_function = True
        depth += line.count("{") - line.count("}")
        if depth > 0:
            entered_body = True
        match = KERNEL_STDCALL_CASE_RE.match(line)
        if match is not None:
            ordinal = int(match.group("ordinal"), 10)
            value = int(match.group("bytes"), 10)
            previous = cases.get(ordinal)
            if previous is not None and previous != value:
                raise ValueError(
                    f"conflicting stdcall argument counts for ordinal {ordinal} "
                    f"at {source_path}"
                )
            cases[ordinal] = value
        if entered_body and depth == 0:
            break
    return cases


def recover_kernel_call_argument_counts(
    generated_dir: Path,
    sites: list[dict[str, Any]],
    window: int = 64,
) -> dict[str, dict[str, int]]:
    """Estimate guest PUSH32 argument counts before kernel calls.

    Generated functions may save callee-saved registers immediately after the
    indirect-call block opens.  An initial push of ``ebx``, ``esi`` or ``edi``
    is excluded only when a matching pop appears in the same function's
    epilogue.  This remains stack-shape evidence, not a proof of argument
    types or ABI.
    """
    source_paths = {
        path.name: path for path in generated_dir.rglob("recomp_*.c")
    }
    counts: dict[str, dict[str, int]] = {}
    for site in sites:
        source_text = str(site.get("source", ""))
        source_name, separator, line_text = source_text.rpartition(":")
        if not separator or not line_text.isdigit():
            continue
        source_path = source_paths.get(source_name)
        if source_path is None:
            continue
        lines = source_path.read_text(encoding="utf-8").splitlines()
        call_index = int(line_text) - 1
        if not 0 <= call_index < len(lines):
            continue
        start = max(0, call_index - window)
        for index in range(call_index, start - 1, -1):
            if "uint32_t _icall_esp = g_esp;" in lines[index]:
                start = index + 1
                break
        else:
            continue
        body_lines = lines[start:call_index]
        function_start = 0
        for index in range(call_index, -1, -1):
            if FUNCTION_SYMBOL_RE.match(lines[index]):
                function_start = index
                break
        function_end = len(lines)
        for index in range(call_index + 1, len(lines)):
            if FUNCTION_SYMBOL_RE.match(lines[index]):
                function_end = index
                break
        epilogue_lines = lines[call_index + 1 : function_end]
        prior_lines = lines[function_start:start]
        first_non_push = next(
            (
                index
                for index, body_line in enumerate(body_lines)
                if "PUSH32(esp," not in body_line
            ),
            len(body_lines),
        )
        saved_registers = {
            register
            for body_line in body_lines[:first_non_push]
            for register in ("ebx", "esi", "edi")
            if re.fullmatch(
                rf"\s*PUSH32\(esp,\s*{register}\);\s*", body_line
            )
            and not any(
                re.fullmatch(
                    rf"\s*PUSH32\(esp,\s*{register}\);\s*", prior_line
                )
                for prior_line in prior_lines
            )
            and any(
                re.fullmatch(
                    rf"\s*POP32\(esp,\s*{register}\);.*", epilogue_line
                )
                for epilogue_line in epilogue_lines
            )
        }
        push_count = sum(
            "PUSH32(esp," in body_line
            and not any(
                index < first_non_push
                and body_line.strip() == f"PUSH32(esp, {register});"
                for register in saved_registers
            )
            for index, body_line in enumerate(body_lines)
        )
        counts[str(site["call_site"])] = {
            "push_count": push_count,
            "pushed_bytes": push_count * 4,
        }
    return counts


def recover_kernel_constant_arguments(
    generated_dir: Path,
    sites: list[dict[str, Any]],
    window: int = 64,
) -> dict[str, dict[str, str]]:
    """Recover constant guest stack arguments before indirect kernel calls.

    The generated code pushes arguments from last to first.  Reversing the
    observed push sequence maps the constants to the bridge's ``STACK_ARG``
    indices.  This is only a constant-value observation; it does not prove
    types, evaluation on every path, ABI fidelity, or service semantics.
    """
    source_paths = {
        path.name: path for path in generated_dir.rglob("recomp_*.c")
    }
    constants: dict[str, dict[str, str]] = {}
    for site in sites:
        source_text = str(site.get("source", ""))
        source_name, separator, line_text = source_text.rpartition(":")
        if not separator or not line_text.isdigit():
            continue
        source_path = source_paths.get(source_name)
        if source_path is None:
            continue
        lines = source_path.read_text(encoding="utf-8").splitlines()
        call_index = int(line_text) - 1
        if not 0 <= call_index < len(lines):
            continue
        start = max(0, call_index - window)
        for index in range(call_index, start - 1, -1):
            if "uint32_t _icall_esp = g_esp;" in lines[index]:
                start = index + 1
                break
        else:
            continue
        body_lines = lines[start:call_index]
        function_start = 0
        for index in range(call_index, -1, -1):
            if FUNCTION_SYMBOL_RE.match(lines[index]):
                function_start = index
                break
        function_end = len(lines)
        for index in range(call_index + 1, len(lines)):
            if FUNCTION_SYMBOL_RE.match(lines[index]):
                function_end = index
                break
        epilogue_lines = lines[call_index + 1 : function_end]
        prior_lines = lines[function_start:start]
        first_non_push = next(
            (
                index
                for index, body_line in enumerate(body_lines)
                if "PUSH32(esp," not in body_line
            ),
            len(body_lines),
        )
        saved_registers = {
            register
            for body_line in body_lines[:first_non_push]
            for register in ("ebx", "esi", "edi")
            if re.fullmatch(
                rf"\s*PUSH32\(esp,\s*{register}\);\s*", body_line
            )
            and not any(
                re.fullmatch(
                    rf"\s*PUSH32\(esp,\s*{register}\);\s*", prior_line
                )
                for prior_line in prior_lines
            )
            and any(
                re.fullmatch(
                    rf"\s*POP32\(esp,\s*{register}\);.*", epilogue_line
                )
                for epilogue_line in epilogue_lines
            )
        }
        pushed_operands: list[str] = []
        for index, body_line in enumerate(body_lines):
            match = PUSH32_RE.match(body_line)
            if match is None:
                continue
            operand = match.group("operand").strip()
            if (
                index < first_non_push
                and operand in saved_registers
            ):
                continue
            pushed_operands.append(operand)
        argument_constants: dict[str, str] = {}
        for argument_index, operand in enumerate(reversed(pushed_operands)):
            match = CONSTANT_OPERAND_RE.fullmatch(operand)
            if match is None:
                continue
            value = int(match.group("value"), 0)
            argument_constants[str(argument_index)] = f"0x{value:08X}"
        constants[str(site["call_site"])] = argument_constants
    return constants


def recover_kernel_register_constant_arguments(
    generated_dir: Path,
    sites: list[dict[str, Any]],
    window: int = 64,
) -> dict[str, dict[str, str]]:
    """Recover constants pushed through a tracked register assignment.

    This is deliberately separate from literal-push recovery. It recognizes
    only a register assigned a literal before its push in the bounded function
    window and call block; it does not prove path coverage, types, or ABI
    fidelity.
    """
    source_paths = {
        path.name: path for path in generated_dir.rglob("recomp_*.c")
    }
    constants: dict[str, dict[str, str]] = {}
    for site in sites:
        source_text = str(site.get("source", ""))
        source_name, separator, line_text = source_text.rpartition(":")
        if not separator or not line_text.isdigit():
            continue
        source_path = source_paths.get(source_name)
        if source_path is None:
            continue
        lines = source_path.read_text(encoding="utf-8").splitlines()
        call_index = int(line_text) - 1
        if not 0 <= call_index < len(lines):
            continue
        start = max(0, call_index - window)
        for index in range(call_index, start - 1, -1):
            if "uint32_t _icall_esp = g_esp;" in lines[index]:
                start = index + 1
                break
        else:
            continue
        body_lines = lines[start:call_index]
        function_start = 0
        for index in range(call_index, -1, -1):
            if FUNCTION_SYMBOL_RE.match(lines[index]):
                function_start = index
                break
        function_end = len(lines)
        for index in range(call_index + 1, len(lines)):
            if FUNCTION_SYMBOL_RE.match(lines[index]):
                function_end = index
                break
        epilogue_lines = lines[call_index + 1 : function_end]
        prior_lines = lines[function_start:start]
        first_non_push = next(
            (
                index
                for index, body_line in enumerate(body_lines)
                if "PUSH32(esp," not in body_line
            ),
            len(body_lines),
        )
        saved_registers = {
            register
            for body_line in body_lines[:first_non_push]
            for register in ("ebx", "esi", "edi")
            if re.fullmatch(
                rf"\s*PUSH32\(esp,\s*{register}\);\s*", body_line
            )
            and not any(
                re.fullmatch(
                    rf"\s*PUSH32\(esp,\s*{register}\);\s*", prior_line
                )
                for prior_line in prior_lines
            )
            and any(
                re.fullmatch(
                    rf"\s*POP32\(esp,\s*{register}\);.*", epilogue_line
                )
                for epilogue_line in epilogue_lines
            )
        }
        register_constants: dict[str, int] = {}
        conditional_checkpoint: dict[str, int] | None = None
        context_start = max(function_start, call_index - window)
        for context_line in lines[context_start:start]:
            if re.search(
                r"\bif\s*\([^\n]*\)\s+goto\s+loc_[0-9A-Fa-f]+;",
                context_line,
            ):
                conditional_checkpoint = register_constants.copy()
                continue
            if re.fullmatch(
                r"\s*goto\s+loc_[0-9A-Fa-f]+;\s*", context_line
            ):
                if conditional_checkpoint is not None:
                    register_constants = conditional_checkpoint.copy()
                else:
                    register_constants.clear()
                continue
            assignment = REGISTER_ASSIGNMENT_RE.match(context_line)
            if assignment is not None:
                register = assignment.group("register")
                literal = REGISTER_LITERAL_RE.fullmatch(
                    assignment.group("rhs").strip()
                )
                if literal is None:
                    register_constants.pop(register, None)
                else:
                    register_constants[register] = int(
                        literal.group("value"), 0
                    )
                continue
            for register in REGISTER_WRITE_RE.findall(context_line):
                register_constants.pop(register, None)
        pushed_register_constants: list[int | None] = []
        for index, body_line in enumerate(body_lines):
            if re.search(
                r"\bif\s*\([^\n]*\)\s+goto\s+loc_[0-9A-Fa-f]+;",
                body_line,
            ):
                conditional_checkpoint = register_constants.copy()
                continue
            if re.fullmatch(
                r"\s*goto\s+loc_[0-9A-Fa-f]+;\s*", body_line
            ):
                if conditional_checkpoint is not None:
                    register_constants = conditional_checkpoint.copy()
                else:
                    register_constants.clear()
                continue
            match = PUSH32_RE.match(body_line)
            if match is not None:
                operand = match.group("operand").strip()
                if not (
                    index < first_non_push and operand in saved_registers
                ):
                    pushed_register_constants.append(
                        register_constants.get(operand)
                    )
                continue
            assignment = REGISTER_ASSIGNMENT_RE.match(body_line)
            if assignment is not None:
                register = assignment.group("register")
                literal = REGISTER_LITERAL_RE.fullmatch(
                    assignment.group("rhs").strip()
                )
                if literal is None:
                    register_constants.pop(register, None)
                else:
                    register_constants[register] = int(
                        literal.group("value"), 0
                    )
                continue
            for register in REGISTER_WRITE_RE.findall(body_line):
                register_constants.pop(register, None)
        argument_constants: dict[str, str] = {}
        for argument_index, value in enumerate(reversed(pushed_register_constants)):
            if value is not None:
                argument_constants[str(argument_index)] = f"0x{value:08X}"
        constants[str(site["call_site"])] = argument_constants
    return constants


def recover_dynamic_target_writes(
    generated_dir: Path,
    functions: list[dict[str, Any]],
    directly_reachable: set[int] | None,
    memory_ranges: list[tuple[int, int]],
) -> dict[str, list[dict[str, Any]]]:
    """Inventory writes matching unresolved callback sources.

    The result records shape-level writer evidence only.  It does not prove
    that a writer reaches the same object or record instance as a callback
    site.  Register-derived writes to the absolute dynamic-memory range are
    included only when the register has a tracked constant value in that
    range; generic ``+0x584`` and indexed-record shapes are kept separate.
    A bounded symbolic alias is also tracked when code computes one of those
    two address shapes into a register and then writes ``MEM32(register)``.
    Passing ``directly_reachable=None`` inventories all generated functions.
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
        address_expression: str | None = None,
    ) -> None:
        row = {
            "function": f"0x{current:08X}",
            "function_name": str(function_by_address[current].get("name", "")),
            "source": f"{source_path.name}:{line_number}",
            "expression": expression,
        }
        value_match = DYNAMIC_WRITER_ASSIGNMENT_RE.search(expression)
        if value_match is not None:
            value = value_match.group("value").strip()
            row["assigned_value_expression"] = value
            row["assigned_value_kind"] = _dynamic_writer_value_kind(value)
        if address_expression is not None:
            row["address_expression"] = address_expression
        writers[family].append(row)

    for source_path in sorted(generated_dir.rglob("recomp_*.c")):
        current: int | None = None
        register_constants: dict[str, int] = {}
        address_aliases: dict[str, tuple[str, str]] = {}
        for line_number, line in enumerate(
            source_path.read_text(encoding="utf-8").splitlines(), 1
        ):
            symbol_match = FUNCTION_SYMBOL_RE.match(line)
            if symbol_match:
                current = address_by_name.get(symbol_match.group(1))
                register_constants = {}
                address_aliases = {}
            if current is None or (
                directly_reachable is not None
                and current not in directly_reachable
            ):
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
            if register_write_match is not None:
                alias = address_aliases.get(register_write_match.group("register"))
                if alias is not None:
                    add_writer(
                        alias[0],
                        source_path,
                        line_number,
                        current,
                        line.strip(),
                        alias[1],
                    )

            assignment_match = REGISTER_ASSIGNMENT_RE.match(line)
            if assignment_match:
                register = assignment_match.group("register")
                rhs = assignment_match.group("rhs").strip()
                object_alias_match = DYNAMIC_OBJECT_FIELD_ALIAS_RE.fullmatch(rhs)
                indexed_alias_match = DYNAMIC_INDEXED_RECORD_ALIAS_RE.fullmatch(rhs)
                literal_match = REGISTER_LITERAL_RE.fullmatch(rhs)
                register_match = REGISTER_NAME_RE.fullmatch(rhs)
                add_match = REGISTER_ADD_RE.fullmatch(rhs)
                if object_alias_match is not None:
                    address_aliases[register] = (
                        "dynamic-object-field-target",
                        rhs,
                    )
                    register_constants.pop(register, None)
                elif indexed_alias_match is not None:
                    address_aliases[register] = (
                        "dynamic-indexed-record-target",
                        rhs,
                    )
                    register_constants.pop(register, None)
                else:
                    address_aliases.pop(register, None)
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
                address_aliases.pop(write_match.group("register"), None)
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


def recover_symbolic_link_open_query_close_sequences(
    rows: list[dict[str, Any]],
) -> list[dict[str, Any]]:
    """Recover local ordinal order for symbolic-link helper calls.

    This is only call-order evidence. It does not prove that the handles
    passed between calls are identical or that the host shim matches Xbox
    object, path, buffer, and lifetime semantics.
    """
    by_function: dict[str, list[dict[str, Any]]] = {}
    for row in rows:
        if row.get("classification") != "kernel-thunk-slot":
            continue
        if int(row.get("kernel_ordinal", -1)) not in (187, 203, 215):
            continue
        by_function.setdefault(str(row["function"]), []).append(row)

    evidence: list[dict[str, Any]] = []
    for function, function_rows in sorted(by_function.items()):
        ordered = sorted(function_rows, key=lambda row: _address(row["call_site"]))
        ordinals = [int(row["kernel_ordinal"]) for row in ordered]
        if ordinals != [203, 215, 187]:
            continue
        evidence.append(
            {
                "function": function,
                "function_name": ordered[0].get("function_name", ""),
                "status": "open-query-close-order-observed",
                "ordinals": ordinals,
                "sites": [
                    {
                        "call_site": row["call_site"],
                        "source": row["source"],
                        "kernel_ordinal": row["kernel_ordinal"],
                        "target": row.get("host_resolver_target"),
                    }
                    for row in ordered
                ],
            }
        )
    return evidence


def recover_symbolic_link_guest_paths(
    generated_dir: Path,
    xbe: dict[str, Any],
    rows: list[dict[str, Any]],
    window: int = 80,
) -> dict[str, dict[str, Any]]:
    """Recover constant XBOX_ANSI_STRING paths before ordinal 203 calls.

    The result is static guest-memory and generated-source evidence only. It
    does not prove that the runtime passes the same pointer on every path or
    that the host symbolic-link implementation has Xbox-compatible lifetime
    semantics.
    """
    source_paths = {
        path.name: path for path in generated_dir.rglob("recomp_*.c")
    }
    evidence: dict[str, dict[str, Any]] = {}
    for row in rows:
        if row.get("classification") != "kernel-thunk-slot":
            continue
        if int(row.get("kernel_ordinal", -1)) != 203:
            continue
        source_text = str(row["source"])
        source_name, line_text = source_text.rsplit(":", 1)
        source_path = source_paths.get(source_name)
        if source_path is None:
            continue
        lines = source_path.read_text(encoding="utf-8").splitlines()
        line_number = int(line_text)
        matches = [
            match
            for line in lines[max(0, line_number - window - 1) : line_number - 1]
            for match in SYMBOLIC_LINK_OBJECT_NAME_ASSIGNMENT_RE.finditer(line)
        ]
        if not matches:
            continue
        object_name_address = int(matches[-1].group("address"), 16)
        object_name_offset = _file_offset(
            xbe["sections"], len(xbe["data"]), object_name_address
        )
        if object_name_offset is None or object_name_offset + 8 > len(xbe["data"]):
            continue
        length, maximum_length, buffer_address = struct.unpack_from(
            "<HHI", xbe["data"], object_name_offset
        )
        if length > maximum_length or length > 0x1000:
            continue
        buffer_offset = _file_offset(
            xbe["sections"], len(xbe["data"]), buffer_address
        )
        if buffer_offset is None or buffer_offset + length > len(xbe["data"]):
            continue
        path_text = xbe["data"][buffer_offset : buffer_offset + length].decode(
            "latin-1"
        )
        evidence[str(row["call_site"])] = {
            "status": "guest-ansi-string-recovered",
            "call_site": row["call_site"],
            "source": row["source"],
            "object_name_address": f"0x{object_name_address:08X}",
            "ansi_length": length,
            "ansi_maximum_length": maximum_length,
            "ansi_buffer_address": f"0x{buffer_address:08X}",
            "path_text": path_text,
        }
    return evidence


def recover_symbolic_link_host_query_target(
    kernel_source: Path,
) -> dict[str, Any] | None:
    """Recover the literal target returned by the host query shim."""
    source_path = kernel_source.parent / "kernel_file.c"
    if not source_path.exists():
        return None
    text = source_path.read_text(encoding="utf-8")
    match = SYMBOLIC_LINK_HOST_TARGET_RE.search(text)
    if match is None:
        return None
    target_text = match.group("target").replace("\\\\", "\\")
    return {
        "status": "host-query-target-literal-observed",
        "target_text": target_text,
        "source": (
            f"{source_path.relative_to(kernel_source.parent).as_posix()}:"
            f"{text.count(chr(10), 0, match.start()) + 1}"
        ),
    }


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
    all_runtime_initializers = recover_runtime_fixed_slot_initializers(
        generated_dir, functions, None
    )
    prcb_cache_writes = recover_prcb_cache_writes(
        generated_dir, functions, directly_reachable
    )
    kernel_resolver_cases = (
        None
        if kernel_source is None
        else recover_kernel_resolver_cases(kernel_source)
    )
    kernel_bridge_cases = (
        None
        if kernel_source is None or not (kernel_source.parent / "kernel_bridge.c").exists()
        else recover_kernel_bridge_cases(kernel_source.parent / "kernel_bridge.c")
    )
    kernel_stdcall_arg_bytes = (
        None
        if kernel_source is None or not (kernel_source.parent / "kernel_bridge.c").exists()
        else recover_kernel_stdcall_arg_bytes(kernel_source.parent / "kernel_bridge.c")
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
    all_dynamic_target_writers = recover_dynamic_target_writes(
        generated_dir,
        functions,
        None,
        dynamic_memory_ranges,
    )
    dynamic_target_outside_direct_writers = {
        family: [
            row
            for row in rows
            if _address(row["function"]) not in directly_reachable
        ]
        for family, rows in all_dynamic_target_writers.items()
    }
    tracked_bss_slots = (0x00360A70, 0x00360AA8, 0x00360AB4)
    all_fixed_slot_writes = recover_fixed_slot_writes(
        generated_dir,
        functions,
        None,
        set(tracked_bss_slots),
    )
    runtime_fixed_slot_writer_inventory = {
        f"0x{slot:08X}": all_fixed_slot_writes.get(slot, [])
        for slot in tracked_bss_slots
    }
    xbe = load_xbe_layout(xbe_path)
    classified = classify_sites(
        sites,
        xbe,
        {_address(function["start"]) for function in functions},
        runtime_initializers,
        kernel_resolver_cases,
        dynamic_target_writers,
    )
    used_kernel_cases = (
        {}
        if kernel_resolver_cases is None
        else {
            int(row["kernel_ordinal"]): kernel_resolver_cases[int(row["kernel_ordinal"])]
            for row in classified
            if row["classification"] == "kernel-thunk-slot"
            and int(row["kernel_ordinal"]) in kernel_resolver_cases
        }
    )
    kernel_implementation_evidence = (
        None
        if kernel_source is None
        else recover_kernel_implementation_evidence(
            kernel_source, used_kernel_cases
        )
    )
    kernel_file_information_evidence = (
        None
        if kernel_source is None
        else recover_kernel_file_information_evidence(kernel_source.parent)
    )
    kernel_call_argument_counts = recover_kernel_call_argument_counts(
        generated_dir, classified
    )
    kernel_constant_arguments = recover_kernel_constant_arguments(
        generated_dir, classified
    )
    kernel_register_constant_arguments = recover_kernel_register_constant_arguments(
        generated_dir, classified
    )
    skip_branch_evidence = recover_indirect_call_skip_branches(
        generated_dir, classified
    )
    nonzero_guard_evidence = recover_indirect_call_nonzero_guards(
        generated_dir, classified
    )
    kernel_file_operations = {
        211: "xbox_NtQueryInformationFile",
        226: "xbox_NtSetInformationFile",
    }
    for row in classified:
        evidence = skip_branch_evidence.get(str(row["call_site"]))
        if evidence is not None:
            row["conditional_skip_evidence"] = evidence
        nonzero_guard = nonzero_guard_evidence.get(str(row["call_site"]))
        if nonzero_guard is not None:
            row["conditional_nonzero_guard_evidence"] = nonzero_guard
        observed_call_stack = kernel_call_argument_counts.get(str(row["call_site"]))
        if observed_call_stack is not None:
            row["indirect_call_observed_push_count"] = observed_call_stack[
                "push_count"
            ]
            row["indirect_call_observed_pushed_bytes"] = observed_call_stack[
                "pushed_bytes"
            ]
        resolver_target = row.get("host_resolver_target")
        if kernel_implementation_evidence is not None and resolver_target:
            implementation = kernel_implementation_evidence.get(resolver_target)
            if implementation is None:
                row["host_implementation_status"] = "resolver-target-not-a-function"
            else:
                row["host_implementation_status"] = implementation["status"]
                row["host_implementation_sources"] = implementation["sources"]
                row["host_implementation_body_markers"] = implementation[
                    "body_markers"
                ]
        ordinal = row.get("kernel_ordinal")
        if kernel_bridge_cases is not None and ordinal is not None:
            bridge_target = kernel_bridge_cases.get(int(ordinal))
            row["host_bridge_status"] = (
                "explicit-case" if bridge_target is not None else "missing-case"
            )
            if bridge_target is not None:
                row["host_bridge_target"] = bridge_target
        if kernel_stdcall_arg_bytes is not None and ordinal is not None:
            expected_bytes = kernel_stdcall_arg_bytes.get(int(ordinal))
            observed = kernel_call_argument_counts.get(str(row["call_site"]))
            if expected_bytes is None:
                row["kernel_abi_stack_status"] = "missing-expected-count"
            elif observed is None:
                row["kernel_abi_stack_status"] = "missing-call-push-evidence"
            else:
                row["kernel_abi_expected_pushed_bytes"] = expected_bytes
                row["kernel_abi_observed_pushed_bytes"] = observed["pushed_bytes"]
                row["kernel_abi_stack_status"] = (
                    "argument-count-match"
                    if expected_bytes == observed["pushed_bytes"]
                    else "argument-count-mismatch"
                )
        if kernel_file_information_evidence is not None and ordinal is not None:
            file_operation = kernel_file_operations.get(int(ordinal))
            if file_operation is not None:
                row["kernel_file_info_class_argument_index"] = 4
                observed_constants = kernel_constant_arguments.get(
                    str(row["call_site"]), {}
                )
                class_text = observed_constants.get("4")
                function_evidence = kernel_file_information_evidence[
                    "functions"
                ].get(file_operation)
                row["kernel_file_info_host_function"] = file_operation
                if class_text is None:
                    row["kernel_file_info_class_status"] = (
                        "missing-constant-argument-evidence"
                    )
                    continue
                class_value = int(class_text, 0)
                row["kernel_file_info_class_value"] = class_value
                row["kernel_file_info_class_value_text"] = class_text
                enum_values = kernel_file_information_evidence["enum_values"]
                class_names = [
                    name for name, value in enum_values.items() if value == class_value
                ]
                if class_names:
                    row["kernel_file_info_class_name"] = class_names[0]
                if function_evidence is None:
                    row["kernel_file_info_class_status"] = "missing-host-case"
                else:
                    row["kernel_file_info_host_case_names"] = function_evidence[
                        "case_names"
                    ]
                    row["kernel_file_info_class_status"] = (
                        "host-case-observed"
                        if class_names
                        and class_names[0] in function_evidence["case_names"]
                        else "missing-host-case"
                    )
        observed_register_constants = kernel_register_constant_arguments.get(
            str(row["call_site"]), {}
        )
        if observed_register_constants:
            row["kernel_register_constant_arguments"] = observed_register_constants
    classifications = Counter(row["classification"] for row in classified)
    fixed_slots = [row for row in classified if "fixed_slot" in row]
    kernel_slots = [row for row in classified if row["classification"] == "kernel-thunk-slot"]
    resolver_case_sites = [
        row for row in kernel_slots if row.get("host_resolver_status") == "explicit-case"
    ]
    resolver_missing_sites = [
        row for row in kernel_slots if row.get("host_resolver_status") == "missing-case"
    ]
    bridge_case_sites = [
        row for row in kernel_slots if row.get("host_bridge_status") == "explicit-case"
    ]
    bridge_missing_sites = [
        row for row in kernel_slots if row.get("host_bridge_status") == "missing-case"
    ]
    abi_count_sites = [
        row for row in kernel_slots if row.get("kernel_abi_stack_status") == "argument-count-match"
    ]
    abi_mismatch_sites = [
        row
        for row in kernel_slots
        if row.get("kernel_abi_stack_status") == "argument-count-mismatch"
    ]
    kernel_implementation_explicit_stub_rows = [
        row
        for row in kernel_slots
        if any(
            marker["kind"] == "explicit-stub-or-no-op"
            for marker in row.get("host_implementation_body_markers", [])
        )
    ]
    kernel_implementation_status_marker_rows = [
        row
        for row in kernel_slots
        if any(
            marker["kind"] == "status-not-implemented-token"
            for marker in row.get("host_implementation_body_markers", [])
        )
    ]
    symbolic_link_lifecycle_evidence = (
        recover_symbolic_link_open_query_close_sequences(classified)
    )
    symbolic_link_guest_path_evidence = recover_symbolic_link_guest_paths(
        generated_dir, xbe, classified
    )
    symbolic_link_host_query_target = (
        None
        if kernel_source is None
        else recover_symbolic_link_host_query_target(kernel_source)
    )
    kernel_file_info_rows = [
        row
        for row in kernel_slots
        if int(row.get("kernel_ordinal", -1)) in (211, 226)
    ]
    kernel_file_info_covered_rows = [
        row
        for row in kernel_file_info_rows
        if row.get("kernel_file_info_class_status") == "host-case-observed"
    ]
    kernel_file_info_missing_rows = [
        row
        for row in kernel_file_info_rows
        if row.get("kernel_file_info_class_status") != "host-case-observed"
    ]
    dynamic_writer_counts = {
        family: len(rows)
        for family, rows in dynamic_target_writers.items()
    }
    dynamic_outside_writer_counts = {
        family: len(rows)
        for family, rows in dynamic_target_outside_direct_writers.items()
    }
    dynamic_writer_value_kinds = {
        family: dict(Counter(row["assigned_value_kind"] for row in rows))
        for family, rows in dynamic_target_writers.items()
    }
    dynamic_outside_writer_value_kinds = {
        family: dict(Counter(row["assigned_value_kind"] for row in rows))
        for family, rows in dynamic_target_outside_direct_writers.items()
    }
    dynamic_classifications = {
        "dynamic-register-or-object-target",
        "dynamic-memory-target",
        "dynamic-object-field-target",
        "dynamic-indexed-record-target",
        "prcb-guarded-dynamic-callback",
    }
    dynamic_skip_branch_sites = [
        row
        for row in classified
        if row["classification"] in dynamic_classifications
        and "conditional_skip_evidence" in row
    ]
    dynamic_nonzero_guard_sites = [
        row
        for row in classified
        if row["classification"] in dynamic_classifications
        and "conditional_nonzero_guard_evidence" in row
    ]
    dynamic_stack_rows = [
        row for row in classified if row["classification"] in dynamic_classifications
    ]
    dynamic_conditional_guard_sites = [
        row
        for row in dynamic_stack_rows
        if (
            "conditional_skip_evidence" in row
            or "conditional_nonzero_guard_evidence" in row
        )
    ]
    dynamic_stack_observed_rows = [
        row for row in dynamic_stack_rows if "indirect_call_observed_pushed_bytes" in row
    ]
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
        "kernel_bridge_status": (
            "not-supplied"
            if kernel_source is None
            else (
                "bridge-source-not-found"
                if kernel_bridge_cases is None
                else "verified-explicit-case-coverage"
            )
        ),
        "kernel_bridge_case_sites": (
            None if kernel_bridge_cases is None else len(bridge_case_sites)
        ),
        "kernel_bridge_missing_sites": (
            None if kernel_bridge_cases is None else len(bridge_missing_sites)
        ),
        "kernel_abi_stack_status": (
            "not-supplied"
            if kernel_stdcall_arg_bytes is None
            else (
                "verified-argument-count-coverage"
                if len(abi_count_sites) == len(kernel_slots)
                and not abi_mismatch_sites
                else "argument-count-mismatch-or-incomplete"
            )
        ),
        "kernel_abi_argument_count_sites": (
            None if kernel_stdcall_arg_bytes is None else len(abi_count_sites)
        ),
        "kernel_abi_argument_count_mismatch_sites": (
            None if kernel_stdcall_arg_bytes is None else len(abi_mismatch_sites)
        ),
        "kernel_implementation_status": (
            "not-supplied"
            if kernel_implementation_evidence is None
            else (
                "verified-source-definitions"
                if all(
                    item["status"] == "source-definition-found"
                    for item in kernel_implementation_evidence.values()
                )
                else "missing-source-definitions"
            )
        ),
        "kernel_implementation_targets": (
            None
            if kernel_implementation_evidence is None
            else len(kernel_implementation_evidence)
        ),
        "kernel_implementation_defined_targets": (
            None
            if kernel_implementation_evidence is None
            else sum(
                item["status"] == "source-definition-found"
                for item in kernel_implementation_evidence.values()
            )
        ),
        "kernel_implementation_body_marker_targets": (
            None
            if kernel_implementation_evidence is None
            else sorted(
                target
                for target, item in kernel_implementation_evidence.items()
                if item["body_markers"]
            )
        ),
        "kernel_implementation_body_marker_target_count": (
            None
            if kernel_implementation_evidence is None
            else sum(
                bool(item["body_markers"])
                for item in kernel_implementation_evidence.values()
            )
        ),
        "kernel_implementation_explicit_stub_or_noop_target_count": (
            None
            if kernel_implementation_evidence is None
            else sum(
                any(
                    marker["kind"] == "explicit-stub-or-no-op"
                    for marker in item["body_markers"]
                )
                for item in kernel_implementation_evidence.values()
            )
        ),
        "kernel_implementation_explicit_stub_or_noop_targets": (
            None
            if kernel_implementation_evidence is None
            else sorted(
                target
                for target, item in kernel_implementation_evidence.items()
                if any(
                    marker["kind"] == "explicit-stub-or-no-op"
                    for marker in item["body_markers"]
                )
            )
        ),
        "kernel_implementation_explicit_stub_or_noop_call_sites": (
            None
            if kernel_implementation_evidence is None
            else [
                {
                    "call_site": row["call_site"],
                    "source": row["source"],
                    "kernel_ordinal": row["kernel_ordinal"],
                    "target": row.get("host_resolver_target"),
                    "pushed_bytes": row.get("indirect_call_observed_pushed_bytes"),
                    "constant_arguments": {
                        **kernel_constant_arguments.get(str(row["call_site"]), {}),
                        **kernel_register_constant_arguments.get(
                            str(row["call_site"]), {}
                        ),
                    },
                }
                for row in kernel_implementation_explicit_stub_rows
            ]
        ),
        "kernel_implementation_status_not_implemented_token_target_count": (
            None
            if kernel_implementation_evidence is None
            else sum(
                any(
                    marker["kind"] == "status-not-implemented-token"
                    for marker in item["body_markers"]
                )
                for item in kernel_implementation_evidence.values()
            )
        ),
        "kernel_implementation_status_not_implemented_token_targets": (
            None
            if kernel_implementation_evidence is None
            else sorted(
                target
                for target, item in kernel_implementation_evidence.items()
                if any(
                    marker["kind"] == "status-not-implemented-token"
                    for marker in item["body_markers"]
                )
            )
        ),
        "kernel_implementation_status_not_implemented_call_sites": (
            None
            if kernel_implementation_evidence is None
            else [
                {
                    "call_site": row["call_site"],
                    "source": row["source"],
                    "kernel_ordinal": row["kernel_ordinal"],
                    "target": row.get("host_resolver_target"),
                }
                for row in kernel_implementation_status_marker_rows
            ]
        ),
        "kernel_symbolic_link_lifecycle_functions": len(
            symbolic_link_lifecycle_evidence
        ),
        "kernel_symbolic_link_lifecycle_status": (
            "open-query-close-order-observed"
            if symbolic_link_lifecycle_evidence
            else "no-open-query-close-order-observed"
        ),
        "kernel_symbolic_link_guest_path_sites": len(
            symbolic_link_guest_path_evidence
        ),
        "kernel_symbolic_link_guest_path_texts": sorted(
            {
                item["path_text"]
                for item in symbolic_link_guest_path_evidence.values()
            }
        ),
        "kernel_symbolic_link_host_query_target": (
            None
            if symbolic_link_host_query_target is None
            else symbolic_link_host_query_target["target_text"]
        ),
        "kernel_implementation_missing_targets": (
            None
            if kernel_implementation_evidence is None
            else sorted(
                target
                for target, item in kernel_implementation_evidence.items()
                if item["status"] != "source-definition-found"
            )
        ),
        "kernel_file_info_class_status": (
            "not-supplied"
            if kernel_file_information_evidence is None
            else (
                "verified-observed-class-cases"
                if kernel_file_info_rows and not kernel_file_info_missing_rows
                else "missing-observed-class-case-evidence"
            )
        ),
        "kernel_file_info_class_sites": (
            None
            if kernel_file_information_evidence is None
            else len(kernel_file_info_rows)
        ),
        "kernel_file_info_class_covered_sites": (
            None
            if kernel_file_information_evidence is None
            else len(kernel_file_info_covered_rows)
        ),
        "kernel_file_info_class_missing_sites": (
            None
            if kernel_file_information_evidence is None
            else len(kernel_file_info_missing_rows)
        ),
        "dynamic_target_direct_writer_sites": sum(dynamic_writer_counts.values()),
        "dynamic_target_direct_writer_counts": dynamic_writer_counts,
        "dynamic_target_direct_writer_value_kinds": dynamic_writer_value_kinds,
        "dynamic_target_direct_writer_status": (
            "direct-writer-shape-observed"
            if sum(dynamic_writer_counts.values())
            else "no-proven-direct-writer"
        ),
        "dynamic_target_outside_direct_writer_sites": sum(
            dynamic_outside_writer_counts.values()
        ),
        "dynamic_target_outside_direct_writer_counts": dynamic_outside_writer_counts,
        "dynamic_target_outside_direct_writer_value_kinds": (
            dynamic_outside_writer_value_kinds
        ),
        "dynamic_target_outside_direct_writer_status": (
            "candidate-shapes-outside-direct-subgraph"
            if sum(dynamic_outside_writer_counts.values())
            else "none-observed"
        ),
        "runtime_fixed_slot_all_writer_counts": {
            slot: len(rows)
            for slot, rows in runtime_fixed_slot_writer_inventory.items()
        },
        "conditional_skip_branch_sites": len(skip_branch_evidence),
        "dynamic_conditional_skip_branch_sites": len(dynamic_skip_branch_sites),
        "dynamic_conditional_nonzero_guard_sites": len(
            dynamic_nonzero_guard_sites
        ),
        "dynamic_conditional_guard_sites": len(dynamic_conditional_guard_sites),
        "dynamic_conditional_guard_missing_sites": (
            len(dynamic_stack_rows) - len(dynamic_conditional_guard_sites)
        ),
        "dynamic_call_stack_shape_status": (
            "observed-push-counts-not-abi-proof"
            if len(dynamic_stack_observed_rows) == len(dynamic_stack_rows)
            else "incomplete-push-count-evidence"
        ),
        "dynamic_call_stack_shape_sites": len(dynamic_stack_observed_rows),
        "dynamic_call_stack_shape_missing_sites": (
            len(dynamic_stack_rows) - len(dynamic_stack_observed_rows)
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
        "dynamic_target_outside_direct_writers": dynamic_target_outside_direct_writers,
        "runtime_fixed_slot_writer_inventory": runtime_fixed_slot_writer_inventory,
        "conditional_skip_branches": skip_branch_evidence,
        "conditional_nonzero_guards": nonzero_guard_evidence,
        "kernel_implementation_evidence": kernel_implementation_evidence or {},
        "kernel_file_information_evidence": kernel_file_information_evidence or {},
        "kernel_bridge_cases": kernel_bridge_cases or {},
        "kernel_call_argument_counts": kernel_call_argument_counts,
        "kernel_constant_arguments": kernel_constant_arguments,
        "kernel_register_constant_arguments": kernel_register_constant_arguments,
        "kernel_symbolic_link_lifecycle_evidence": symbolic_link_lifecycle_evidence,
        "kernel_symbolic_link_guest_path_evidence": symbolic_link_guest_path_evidence,
        "kernel_symbolic_link_host_query_target": symbolic_link_host_query_target or {},
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
