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
INDIRECT_TAIL_SWITCH_HEADER_RE = re.compile(
    r"^\s*\{\s*uint32_t\s+_jt\s*=\s*MEM32\((?P<expression>[^)]+)\);"
    r"\s*/\*\s*switch:\s*(?P<entries>[0-9]+)\s+entries,\s*"
    r"(?P<targets>[0-9]+)\s+targets\s*\*/"
)
INDIRECT_TAIL_SWITCH_TARGET_RE = re.compile(
    r"^\s*if\s*\(_jt\s*==\s*0x(?P<value>[0-9A-Fa-f]{1,8})u\)\s+"
    r"goto\s+(?P<label>loc_[0-9A-Fa-f]+);"
)
INDIRECT_TAIL_RE = re.compile(r"\bRECOMP_ITAIL\((?P<target>[^)]+)\)")
INDIRECT_TAIL_TABLE_EXPRESSION_RE = re.compile(
    r"^(?P<index>[A-Za-z_][A-Za-z0-9_]*)\s*\*\s*4\s*\+\s*"
    r"0x(?P<base>[0-9A-Fa-f]{1,8})$"
)
DIRECT_ABI_CALL_RE = re.compile(
    r"RECOMP_ABI_CALL\(0x(?P<target>[0-9A-Fa-f]{8})u,\s*"
    r"(?P<name>[A-Za-z_][A-Za-z0-9_]*)\)"
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
PRCB_HOST_FAKE_ADDRESS_RE = re.compile(
    r"#define\s+FAKE_PRCB_VA\s+0x(?P<address>[0-9A-Fa-f]+)"
)
PRCB_HOST_ZERO_BLOCK_RE = re.compile(
    r"memset\(XBOX_VA\(FAKE_PRCB_VA\),\s*0,\s*0x(?P<size>[0-9A-Fa-f]+)\)"
)
PRCB_HOST_FS_POINTER_RE = re.compile(
    r"MEM32_INIT\(XBOX_FS_BASE\s*\+\s*0x20,\s*FAKE_PRCB_VA\)"
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
KERNEL_FUNCTION_SIGNATURE_RE = re.compile(
    r"(?ms)^\s*(?:(?:static|inline|__declspec\([^)]*\))\s+)*"
    r"(?:[A-Za-z_][A-Za-z0-9_]*\s+)+"
    r"(?P<name>xbox_[A-Za-z0-9_]+)\s*"
    r"\((?P<parameters>[^;{}]*)\)\s*\{"
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
SYMBOLIC_LINK_BRIDGE_PATH_RE = re.compile(
    r'\bstrcmp\(path,\s*"(?P<path>[^"]+)"\)\s*!=\s*0'
)
KERNEL_BRIDGE_FUNCTION_RE = re.compile(
    r"^\s*static\s+bridge_func_t\s+bridge_for_ordinal\s*\(ULONG\s+ordinal\)\s*$"
)
KERNEL_BRIDGE_CASE_RE = re.compile(
    r"^\s*case\s+(?P<ordinal>[0-9]+)\s*:\s*return\s+"
    r"(?P<target>bridge_[^;]+);"
)
KERNEL_BRIDGE_FUNCTION_DEF_RE = re.compile(
    r"^\s*static\s+void\s+(?P<name>bridge_[A-Za-z0-9_]+)\s*\(void\)\s*$"
)
KERNEL_BRIDGE_STACK_ARG_RE = re.compile(r"\bSTACK_ARG\((?P<index>[0-9]+)\)")
KERNEL_BRIDGE_HOST_CALL_RE = re.compile(
    r"\b(?P<name>xbox_[A-Za-z0-9_]+)\s*\("
)
KERNEL_BRIDGE_TERMINAL_HOST_CALLS = {"xbox_HalReturnToFirmware"}
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
DYNAMIC_CALLBACK_OBJECT_LOAD_RE = re.compile(
    r"^\s*(?P<register>eax|ebx|ecx|edx|esi|edi)\s*=\s*"
    r"MEM32\(esp\s*\+\s*0x20\);"
)
DYNAMIC_CALLBACK_FIELD_LOAD_RE = re.compile(
    r"^\s*(?P<register>eax|ebx|ecx|edx|esi|edi)\s*=\s*"
    r"MEM32\((?P<object>eax|ebx|ecx|edx|esi|edi)\s*\+\s*"
    r"(?P<offset>0x[0-9A-Fa-f]+|[0-9]+)\);"
)
DYNAMIC_CALLBACK_OBJECT_TARGET_RE = re.compile(
    r"^MEM32\((?:eax|ebx|ecx|edx|esi|edi)\s*\+\s*"
    r"(?P<offset>0x[0-9A-Fa-f]+|[0-9]+)\)$"
)
LOCAL_MEMORY_WRITE_RE = re.compile(
    r"^\s*MEM32\((?P<base>eax|ebx|ecx|edx|esi|edi|ebp)\s*\+\s*"
    r"(?P<offset>-?0x[0-9A-Fa-f]+|-?[0-9]+)\)\s*=\s*"
    r"(?P<rhs>[^;]+);"
)
DYNAMIC_WRITER_ASSIGNMENT_RE = re.compile(r"=\s*(?P<value>[^;]+);")
DYNAMIC_OBJECT_FIELD_ALIAS_RE = re.compile(
    r"^(?P<base>eax|ebx|ecx|edx|esi|edi)\s*\+\s*0x584$"
)
DYNAMIC_INDEXED_RECORD_ALIAS_RE = re.compile(
    r"^(?P<base>eax|ebx|ecx|edx|esi|edi)\s*\+\s*"
    r"(?P<index>eax|ebx|ecx|edx|esi|edi)\s*\*\s*4\s*\+\s*8$"
)
INDEXED_CALLBACK_TABLE_BASE = 0x00361360
INDEXED_CALLBACK_TABLE_CAPACITY_SLOT = 0x0036135C
INDEXED_CALLBACK_TABLE_INIT_FUNCTION = 0x002772E2
INDEXED_CALLBACK_TABLE_RECORD_STRIDE = 0x28
INDEXED_CALLBACK_TABLE_INIT_BYTES = 0x500
INDEXED_CALLBACK_TABLE_FLAGGED_BYTES = 0x78
INDEXED_CALLBACK_ROOT_MANAGER_FUNCTION = 0x00277AB1
INDEXED_CALLBACK_CONSUMER_FUNCTION = 0x0026E70A
INDEXED_CALLBACK_CONSUMER_CALL_SITE = 0x0026E75E
INDEXED_CALLBACK_CONSUMER_OBJECT_TABLE_OFFSET = 0x8
INDEXED_CALLBACK_CONSUMER_OBJECT_INDEX_OFFSET = 0xC
INDEXED_CALLBACK_CONSUMER_RECORD_STRIDE = 0xC
INDEXED_CALLBACK_CONSUMER_RECORD_GUARD_OFFSET = 0x4
INDEXED_CALLBACK_CONSUMER_RECORD_TARGET_OFFSET = 0x8
INDEXED_CALLBACK_FRAME_HELPER_FUNCTION = 0x002710F4
INDEXED_CALLBACK_FRAME_LOCAL_OFFSET = -0x10
INDEXED_CALLBACK_TABLE_ROOT_ACCESS_RE = re.compile(
    r"MEM32\([^;\n]*\+\s*0x361360\)"
)
INDEXED_CALLBACK_FRAME_LOCAL_ASSIGNMENT_RE = re.compile(
    r"^\s*(?P<register>eax|ebx|ecx|edx|esi|edi)\s*=\s*"
    r"ebp\s*\+\s*(?P<offset>-0x10|-16);"
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


def recover_indirect_tail_switch_provenance(
    generated_dir: Path,
    functions: list[dict[str, Any]],
    directly_reachable: set[int],
    xbe: dict[str, Any],
) -> list[dict[str, Any]]:
    """Verify XBE values for generated ``RECOMP_ITAIL`` switch fallbacks.

    This is deliberately limited to the generated pattern that compares a
    table-loaded ``_jt`` value against local labels before falling back to
    ``RECOMP_ITAIL(_jt)``.  It proves that the read-only XBE table values match
    the translated switch arms; it does not prove that a runtime index is
    valid, that the tail path executes, or that arbitrary runtime targets are
    safe.
    """
    address_by_name = {
        str(function.get("name", "")): _address(function["start"])
        for function in functions
    }
    function_by_address = {
        _address(function["start"]): function for function in functions
    }
    rows: list[dict[str, Any]] = []
    for source_path in sorted(generated_dir.rglob("recomp_*.c")):
        lines = source_path.read_text(encoding="utf-8").splitlines()
        current: int | None = None
        function_start_line = 0
        for line_index, line in enumerate(lines):
            symbol_match = FUNCTION_SYMBOL_RE.match(line)
            if symbol_match:
                current = address_by_name.get(symbol_match.group(1))
                function_start_line = line_index
            if current is None or current not in directly_reachable:
                continue
            tail_match = INDIRECT_TAIL_RE.search(line)
            if tail_match is None:
                continue

            row: dict[str, Any] = {
                "function": f"0x{current:08X}",
                "function_name": str(function_by_address[current].get("name", "")),
                "tail_target_expression": tail_match.group("target").strip(),
                "source": f"{source_path.name}:{line_index + 1}",
                "status": "indirect-tail-no-static-switch-provenance",
            }
            header_index: int | None = None
            header_match: re.Match[str] | None = None
            for candidate in range(line_index - 1, function_start_line - 1, -1):
                if "}" in lines[candidate]:
                    break
                candidate_match = INDIRECT_TAIL_SWITCH_HEADER_RE.match(lines[candidate])
                if candidate_match is not None:
                    header_index = candidate
                    header_match = candidate_match
                    break
            if header_match is None or header_index is None:
                rows.append(row)
                continue

            expression = header_match.group("expression").strip()
            declared_entries = int(header_match.group("entries"), 10)
            declared_targets = int(header_match.group("targets"), 10)
            target_values = [
                int(match.group("value"), 16)
                for candidate in lines[header_index + 1 : line_index + 1]
                if (match := INDIRECT_TAIL_SWITCH_TARGET_RE.match(candidate))
            ]
            target_labels = [
                match.group("label")
                for candidate in lines[header_index + 1 : line_index + 1]
                if (match := INDIRECT_TAIL_SWITCH_TARGET_RE.match(candidate))
            ]
            row.update(
                {
                    "switch_table_expression": expression,
                    "switch_declared_entry_count": declared_entries,
                    "switch_declared_target_count": declared_targets,
                    "switch_target_values": [f"0x{value:08X}" for value in target_values],
                    "switch_target_labels": target_labels,
                }
            )
            table_match = INDIRECT_TAIL_TABLE_EXPRESSION_RE.fullmatch(expression)
            if table_match is None:
                row["status"] = "switch-table-expression-unresolved"
                rows.append(row)
                continue

            table_base = int(table_match.group("base"), 16)
            row["switch_table_base"] = f"0x{table_base:08X}"
            row["switch_table_index_expression"] = table_match.group("index")
            data = xbe["data"]
            sections = xbe["sections"]
            table_values: list[int] = []
            table_read_failure = False
            for index in range(declared_entries):
                table_address = table_base + index * 4
                file_offset = _file_offset(sections, len(data), table_address)
                if file_offset is None or file_offset + 4 > len(data):
                    table_read_failure = True
                    break
                table_values.append(_u32(data, file_offset))
            row["switch_table_values"] = [
                f"0x{value:08X}" for value in table_values
            ]
            if table_read_failure:
                row["status"] = "switch-table-read-unresolved"
            elif (
                declared_entries == declared_targets
                and len(target_values) == declared_targets
                and sorted(table_values) == sorted(target_values)
            ):
                row["status"] = "static-table-entries-match-switch-targets"
            elif len(table_values) == declared_entries and all(
                value in set(target_values) for value in table_values
            ):
                row["status"] = "static-table-values-match-local-switch-branches"
            else:
                row["status"] = "switch-target-set-mismatch"
            row["switch_table_unexpected_values"] = [
                f"0x{value:08X}"
                for value in table_values
                if value not in set(target_values)
            ]
            row["switch_targets_missing_from_table"] = [
                f"0x{value:08X}"
                for value in target_values
                if value not in set(table_values)
            ]
            rows.append(row)
    return rows


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


def recover_indexed_callback_table_provenance(
    generated_dir: Path,
    functions: list[dict[str, Any]],
    directly_reachable: set[int] | None,
) -> dict[str, list[dict[str, Any]]]:
    """Inventory the BSS table root used by the indexed callback family.

    The generated code uses ``0x00361360`` as a table of pointers and stores
    its current byte capacity at ``0x0036135C``.  This helper records absolute
    root/capacity writes and the bounded ``index * 4 + root`` alias shape used
    for record writes.  It does not infer record identity or callback targets.
    """
    address_by_name = {
        str(function.get("name", "")): _address(function["start"])
        for function in functions
    }
    function_by_address = {
        _address(function["start"]): function for function in functions
    }
    table_text = f"0x{INDEXED_CALLBACK_TABLE_BASE:X}"
    capacity_text = f"0x{INDEXED_CALLBACK_TABLE_CAPACITY_SLOT:X}"
    indexed_alias_re = re.compile(
        r"^\s*(?P<register>eax|ebx|ecx|edx|esi|edi)\s*=\s*"
        r"(?P<index>eax|ebx|ecx|edx|esi|edi)\s*\*\s*4\s*\+\s*"
        + re.escape(table_text)
        + r";"
    )
    rows: dict[str, list[dict[str, Any]]] = {
        "table_root_writes": [],
        "capacity_writes": [],
        "indexed_record_writes": [],
    }

    def add_row(
        category: str,
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
        rows[category].append(row)

    for source_path in sorted(generated_dir.rglob("recomp_*.c")):
        current: int | None = None
        aliases: set[str] = set()
        for line_number, line in enumerate(
            source_path.read_text(encoding="utf-8").splitlines(), 1
        ):
            symbol_match = FUNCTION_SYMBOL_RE.match(line)
            if symbol_match:
                current = address_by_name.get(symbol_match.group(1))
                aliases = set()
            if current is None or (
                directly_reachable is not None
                and current not in directly_reachable
            ):
                continue

            root_write = FIXED_SLOT_ANY_WRITE_RE.search(line)
            if root_write is not None:
                slot = int(root_write.group("slot"), 16)
                if slot == INDEXED_CALLBACK_TABLE_BASE:
                    constant = FIXED_SLOT_WRITE_RE.search(line)
                    add_row(
                        "table_root_writes",
                        source_path,
                        line_number,
                        current,
                        "absolute-constant" if constant is not None else "absolute",
                        line.strip(),
                        None
                        if constant is None
                        else f"0x{int(constant.group('value'), 16):08X}",
                    )
                elif slot == INDEXED_CALLBACK_TABLE_CAPACITY_SLOT:
                    constant = FIXED_SLOT_WRITE_RE.search(line)
                    add_row(
                        "capacity_writes",
                        source_path,
                        line_number,
                        current,
                        "absolute-constant" if constant is not None else "absolute",
                        line.strip(),
                        None
                        if constant is None
                        else f"0x{int(constant.group('value'), 16):08X}",
                    )

            register_write = FIXED_REGISTER_WRITE_RE.search(line)
            if register_write is not None and register_write.group("register") in aliases:
                add_row(
                    "indexed_record_writes",
                    source_path,
                    line_number,
                    current,
                    "indexed-record-alias",
                    line.strip(),
                    register_write.group("rhs").strip(),
                )

            alias_match = indexed_alias_re.match(line)
            if alias_match is not None:
                aliases.add(alias_match.group("register"))
            else:
                assignment_match = REGISTER_ASSIGNMENT_RE.match(line)
                if assignment_match is not None:
                    aliases.discard(assignment_match.group("register"))
            for register_match in REGISTER_WRITE_RE.finditer(line):
                aliases.discard(register_match.group("register"))
    return rows


def recover_indexed_callback_table_initialization_layout(
    generated_dir: Path,
) -> dict[str, Any]:
    """Recover the bounded initialization shape of the indexed BSS table.

    This recognizes the local initializer at ``sub_002772E2``: it publishes
    the allocation at ``0x00361360``, records capacity ``0x20``, clears the
    dword at root-record ``+8`` across a ``0x500``-byte span with stride
    ``0x28``, and then applies the observed ``0xC1`` byte pattern to the first
    ``0x78`` bytes of record starts.  It is a source-shape audit only; it does
    not prove allocator success, loop execution, record identity at a caller,
    or a runtime callback target.
    """
    function_name = f"sub_{INDEXED_CALLBACK_TABLE_INIT_FUNCTION:08X}"
    for source_path in sorted(generated_dir.rglob("recomp_*.c")):
        lines = source_path.read_text(encoding="utf-8").splitlines()
        current_name = ""
        function_lines: list[tuple[int, str]] = []

        def inspect_function() -> dict[str, Any] | None:
            if current_name != function_name or not function_lines:
                return None

            def find(pattern: str) -> dict[str, str] | None:
                expression_re = re.compile(pattern)
                for line_number, line in function_lines:
                    match = expression_re.search(line)
                    if match is not None:
                        return {
                            "source": f"{source_path.name}:{line_number}",
                            "expression": line.strip(),
                            **match.groupdict(),
                        }
                return None

            root_write = find(
                r"MEM32\(0x00361360\)\s*=\s*(?P<value>[^;]+);"
            ) or find(r"MEM32\(0x361360\)\s*=\s*(?P<value>[^;]+);")
            capacity_write = find(
                r"MEM32\(0x0036135C\)\s*=\s*0x(?P<value>[0-9A-Fa-f]+)u?;"
            ) or find(
                r"MEM32\(0x36135C\)\s*=\s*0x(?P<value>[0-9A-Fa-f]+)u?;"
            )
            sentinel_write = find(
                r"MEM32\(eax\)\s*=\s*MEM32\(eax\)\s*\|\s*"
                r"0xFFFFFFFFu;"
            )
            callback_field_zero = find(
                r"MEM32\(eax\s*\+\s*8\)\s*=\s*"
                r"MEM32\(eax\s*\+\s*8\)\s*&\s*0;"
            )
            flag_zero = find(r"MEM8\(eax\s*\+\s*4\)\s*=\s*0;")
            flag_ten = find(r"MEM8\(eax\s*\+\s*5\)\s*=\s*0xA;")
            stride_increment = find(
                r"eax\s*=\s*eax\s*\+\s*0x28;"
            )
            span_increment = find(
                r"ecx\s*=\s*ecx\s*\+\s*0x500;"
            )
            root_reload = find(r"ecx\s*=\s*MEM32\(0x361360\);")
            flagged_write = find(r"MEM8\(ecx\s*\+\s*4\)\s*=\s*0xC1;")
            flagged_limit = find(r"0x78")
            required = (
                root_write is not None
                and capacity_write is not None
                and sentinel_write is not None
                and callback_field_zero is not None
                and flag_zero is not None
                and flag_ten is not None
                and stride_increment is not None
                and span_increment is not None
                and root_reload is not None
                and flagged_write is not None
                and flagged_limit is not None
                and int(capacity_write["value"], 16) == 0x20
            )
            return {
                "status": (
                    "verified-bounded-indexed-table-initializer-shape"
                    if required
                    else "incomplete-indexed-table-initializer-shape"
                ),
                "source_file": source_path.name,
                "function": f"0x{INDEXED_CALLBACK_TABLE_INIT_FUNCTION:08X}",
                "table_root": f"0x{INDEXED_CALLBACK_TABLE_BASE:08X}",
                "capacity_slot": f"0x{INDEXED_CALLBACK_TABLE_CAPACITY_SLOT:08X}",
                "capacity_records": (
                    None
                    if capacity_write is None
                    else int(capacity_write["value"], 16)
                ),
                "record_stride_bytes": INDEXED_CALLBACK_TABLE_RECORD_STRIDE,
                "initialization_span_bytes": INDEXED_CALLBACK_TABLE_INIT_BYTES,
                "initialization_record_count": (
                    INDEXED_CALLBACK_TABLE_INIT_BYTES
                    // INDEXED_CALLBACK_TABLE_RECORD_STRIDE
                ),
                "root_record_zeroed_field_offset": "0x8",
                "root_record_zeroed_field_status": (
                    "zero-write-shape-observed"
                    if callback_field_zero is not None
                    else "not-observed"
                ),
                "flagged_prefix_bytes": INDEXED_CALLBACK_TABLE_FLAGGED_BYTES,
                "flagged_prefix_record_count": (
                    INDEXED_CALLBACK_TABLE_FLAGGED_BYTES
                    // INDEXED_CALLBACK_TABLE_RECORD_STRIDE
                ),
                "flagged_byte_offset": "0x4",
                "flagged_byte_value": "0xC1",
                "evidence": {
                    "root_write": root_write or {},
                    "capacity_write": capacity_write or {},
                    "sentinel_write": sentinel_write or {},
                    "callback_field_zero": callback_field_zero or {},
                    "flag_zero": flag_zero or {},
                    "flag_ten": flag_ten or {},
                    "stride_increment": stride_increment or {},
                    "span_increment": span_increment or {},
                    "root_reload": root_reload or {},
                    "flagged_write": flagged_write or {},
                    "flagged_limit": flagged_limit or {},
                },
            }

        for line_number, line in enumerate(lines, 1):
            symbol_match = FUNCTION_SYMBOL_RE.match(line)
            if symbol_match is not None:
                result = inspect_function()
                if result is not None:
                    return result
                current_name = symbol_match.group(1)
                function_lines = [(line_number, line)]
                continue
            if current_name:
                function_lines.append((line_number, line))
        result = inspect_function()
        if result is not None:
            return result
    return {
        "status": "indexed-table-initializer-function-not-observed",
        "function": f"0x{INDEXED_CALLBACK_TABLE_INIT_FUNCTION:08X}",
        "table_root": f"0x{INDEXED_CALLBACK_TABLE_BASE:08X}",
        "capacity_slot": f"0x{INDEXED_CALLBACK_TABLE_CAPACITY_SLOT:08X}",
        "evidence": {},
    }


def recover_dynamic_indexed_callback_consumer_layout(
    generated_dir: Path,
    functions: list[dict[str, Any]],
    classified: list[dict[str, Any]],
) -> dict[str, Any]:
    """Recover the bounded layout used by the indexed callback consumer.

    This is intentionally separate from ``recover_indexed_callback_table_provenance``.
    The BSS root at ``0x00361360`` is an allocation/record family, while the
    consumer at ``0x0026E70A`` obtains its container from an object field.  The
    generated source does not, by itself, prove that those pointers alias.  The
    helper therefore records the consumer's object-field loads, index scaling,
    guard field and target field without merging the two structures or naming a
    runtime callback.
    """
    consumer_rows = [
        row
        for row in classified
        if row.get("classification") == "dynamic-indexed-record-target"
        and _address(row.get("function", 0)) == INDEXED_CALLBACK_CONSUMER_FUNCTION
    ]
    if not consumer_rows:
        return {
            "status": "no-indexed-callback-consumer-observed",
            "function": f"0x{INDEXED_CALLBACK_CONSUMER_FUNCTION:08X}",
            "call_sites": [],
            "table_root_link_status": "not-assessed",
        }

    function_by_name = {
        str(function.get("name", "")): function
        for function in functions
        if _address(function.get("start", 0)) == INDEXED_CALLBACK_CONSUMER_FUNCTION
    }
    target_names = {
        str(row.get("function_name", ""))
        for row in consumer_rows
        if row.get("function_name")
    }
    if not target_names:
        target_names = set(function_by_name)

    call_sites = sorted(
        {
            _address(row["call_site"])
            for row in consumer_rows
            if row.get("call_site") is not None
        }
    )
    object_loads: list[dict[str, Any]] = []
    container_field_loads: list[dict[str, Any]] = []
    index_field_loads: list[dict[str, Any]] = []
    index_scale_rows: list[dict[str, Any]] = []
    guard_field_rows: list[dict[str, Any]] = []
    target_rows: list[dict[str, Any]] = []
    source_function = ""

    scale_re = re.compile(
        r"^\s*(?P<register>eax|ebx|ecx|edx|esi|edi)\s*=\s*"
        r"(?P=register)\s*\+\s*(?P=register)\s*\*\s*2;"
    )
    guard_re = re.compile(
        r"MEM32\(ebx\s*\+\s*esi\s*\*\s*4\s*\+\s*0x4\)"
    )

    for source_path in sorted(generated_dir.rglob("recomp_*.c")):
        lines = source_path.read_text(encoding="utf-8").splitlines()
        current_name = ""
        current_address: int | None = None
        function_lines: list[tuple[int, str]] = []

        def inspect_function() -> None:
            nonlocal source_function
            if current_name not in target_names or not function_lines:
                return
            source_function = source_path.name
            object_registers: set[str] = set()
            for line_number, body_line in function_lines:
                object_load = DYNAMIC_CALLBACK_OBJECT_LOAD_RE.match(body_line)
                if object_load is not None:
                    object_registers.add(object_load.group("register"))
                    object_loads.append(
                        {
                            "source": f"{source_path.name}:{line_number}",
                            "expression": body_line.strip(),
                            "register": object_load.group("register"),
                        }
                    )

                field_load = DYNAMIC_CALLBACK_FIELD_LOAD_RE.match(body_line)
                if field_load is not None and field_load.group("object") in object_registers:
                    offset = int(field_load.group("offset"), 0)
                    if offset == INDEXED_CALLBACK_CONSUMER_OBJECT_TABLE_OFFSET:
                        container_field_loads.append(
                            {
                                "source": f"{source_path.name}:{line_number}",
                                "expression": body_line.strip(),
                                "object_register": field_load.group("object"),
                                "loaded_register": field_load.group("register"),
                                "field_offset": f"0x{offset:X}",
                            }
                        )
                    elif offset == INDEXED_CALLBACK_CONSUMER_OBJECT_INDEX_OFFSET:
                        index_field_loads.append(
                            {
                                "source": f"{source_path.name}:{line_number}",
                                "expression": body_line.strip(),
                                "object_register": field_load.group("object"),
                                "loaded_register": field_load.group("register"),
                                "field_offset": f"0x{offset:X}",
                            }
                        )

                scale_match = scale_re.match(body_line)
                if scale_match is not None:
                    index_scale_rows.append(
                        {
                            "source": f"{source_path.name}:{line_number}",
                            "expression": body_line.strip(),
                            "register": scale_match.group("register"),
                            "multiplier": 3,
                        }
                    )
                if guard_re.search(body_line) is not None:
                    guard_field_rows.append(
                        {
                            "source": f"{source_path.name}:{line_number}",
                            "expression": body_line.strip(),
                            "field_offset": f"0x{INDEXED_CALLBACK_CONSUMER_RECORD_GUARD_OFFSET:X}",
                        }
                    )
                call = INDIRECT_CALL_RE.search(body_line)
                if call is not None and int(call.group("site"), 16) in call_sites:
                    target_rows.append(
                        {
                            "source": f"{source_path.name}:{line_number}",
                            "expression": body_line.strip(),
                            "call_site": f"0x{int(call.group('site'), 16):08X}",
                            "target_expression": call.group("expression").strip(),
                        }
                    )

        for line_number, line in enumerate(lines, 1):
            symbol_match = FUNCTION_SYMBOL_RE.match(line)
            if symbol_match is not None:
                inspect_function()
                current_name = symbol_match.group(1)
                current_address = (
                    _address(function_by_name[current_name]["start"])
                    if current_name in function_by_name
                    else None
                )
                function_lines = [(line_number, line)]
                continue
            if current_address == INDEXED_CALLBACK_CONSUMER_FUNCTION:
                function_lines.append((line_number, line))
        inspect_function()

    required = (
        bool(object_loads)
        and bool(container_field_loads)
        and bool(index_field_loads)
        and bool(index_scale_rows)
        and bool(target_rows)
    )
    return {
        "status": (
            "verified-indexed-callback-consumer-layout"
            if required
            else "incomplete-indexed-callback-consumer-layout"
        ),
        "function": f"0x{INDEXED_CALLBACK_CONSUMER_FUNCTION:08X}",
        "function_name": sorted(target_names)[0] if target_names else "",
        "source_file": source_function,
        "call_sites": [f"0x{site:08X}" for site in call_sites],
        "object_loads": object_loads,
        "container_field_loads": container_field_loads,
        "index_field_loads": index_field_loads,
        "index_scale": index_scale_rows,
        "guard_field_reads": guard_field_rows,
        "target_reads": target_rows,
        "record_stride_bytes": INDEXED_CALLBACK_CONSUMER_RECORD_STRIDE,
        "record_guard_field_offset": f"0x{INDEXED_CALLBACK_CONSUMER_RECORD_GUARD_OFFSET:X}",
        "record_target_field_offset": f"0x{INDEXED_CALLBACK_CONSUMER_RECORD_TARGET_OFFSET:X}",
        "record_target_shape": "MEM32(ebx + esi * 4 + 8)",
        "table_root_link_status": "not-proven-from-consumer-body",
    }


def recover_indexed_callback_root_handoff_provenance(
    generated_dir: Path,
) -> dict[str, Any]:
    """Compare the root-record path with the consumer's local frame object.

    The bounded pattern in ``sub_00277AB1`` reads a record from the indexed
    root and later calls ``sub_0026E70A`` with ``ebp - 0x10`` after the frame
    helper.  This records both shapes and any direct frame write in between;
    absence of that write does not exclude helper-mediated aliasing or runtime
    object identity.
    """
    function_name = f"sub_{INDEXED_CALLBACK_ROOT_MANAGER_FUNCTION:08X}"
    root_loads: list[dict[str, Any]] = []
    record_addresses: list[dict[str, Any]] = []
    record_field_accesses: list[dict[str, Any]] = []
    frame_helper_calls: list[dict[str, Any]] = []
    frame_assignments: list[dict[str, Any]] = []
    consumer_calls: list[dict[str, Any]] = []
    direct_frame_writes: list[dict[str, Any]] = []
    source_file = ""

    root_re = re.compile(r"MEM32\([^;]*\*\s*4\s*\+\s*0x361360\)")
    record_re = re.compile(
        r"^\s*esi\s*=\s*ecx\s*\+\s*eax\s*\*\s*8;"
    )
    record_field_re = re.compile(
        r"MEM32\(esi\s*\+\s*(?:0x8|8|0xC|12)\)"
    )
    frame_re = re.compile(
        r"^\s*(?P<register>eax|ebx|ecx|edx|esi|edi)\s*=\s*"
        r"ebp\s*\+\s*(?:-0x10|-16);"
    )
    direct_frame_write_re = re.compile(
        r"MEM32\(ebp\s*\+\s*(?:-0x10|-16)\)\s*="
    )

    for source_path in sorted(generated_dir.rglob("recomp_*.c")):
        lines = source_path.read_text(encoding="utf-8").splitlines()
        in_target = False
        for line_number, line in enumerate(lines, 1):
            symbol_match = FUNCTION_SYMBOL_RE.match(line)
            if symbol_match is not None:
                in_target = symbol_match.group(1) == function_name
                if in_target:
                    source_file = source_path.name
                continue
            if not in_target:
                continue
            source = f"{source_path.name}:{line_number}"
            stripped = line.strip()
            if root_re.search(line) is not None:
                root_loads.append({"source": source, "expression": stripped})
            if record_re.match(line) is not None:
                record_addresses.append({"source": source, "expression": stripped})
            if record_field_re.search(line) is not None:
                record_field_accesses.append(
                    {"source": source, "expression": stripped}
                )
            if "0x002710F4u, sub_002710F4" in line:
                frame_helper_calls.append(
                    {"source": source, "expression": stripped}
                )
            frame_match = frame_re.match(line)
            if frame_match is not None:
                frame_assignments.append(
                    {
                        "source": source,
                        "expression": stripped,
                        "register": frame_match.group("register"),
                    }
                )
            if "0x0026E70Au, sub_0026E70A" in line:
                consumer_calls.append(
                    {"source": source, "expression": stripped}
                )
            if direct_frame_write_re.search(line) is not None:
                direct_frame_writes.append(
                    {"source": source, "expression": stripped}
                )

    required = (
        bool(root_loads)
        and bool(record_addresses)
        and bool(record_field_accesses)
        and bool(frame_helper_calls)
        and bool(frame_assignments)
        and bool(consumer_calls)
    )
    if not source_file:
        status = "root-manager-function-not-observed"
    elif not required:
        status = "incomplete-root-record-frame-handoff-shape"
    elif direct_frame_writes:
        status = "direct-frame-write-observed-in-root-manager"
    else:
        status = "verified-bounded-root-record-frame-separation"
    return {
        "status": status,
        "function": f"0x{INDEXED_CALLBACK_ROOT_MANAGER_FUNCTION:08X}",
        "function_name": function_name,
        "source_file": source_file,
        "table_root": f"0x{INDEXED_CALLBACK_TABLE_BASE:08X}",
        "root_record_stride_bytes": INDEXED_CALLBACK_TABLE_RECORD_STRIDE,
        "consumer_function": f"0x{INDEXED_CALLBACK_CONSUMER_FUNCTION:08X}",
        "frame_local_offset": f"0x{abs(INDEXED_CALLBACK_FRAME_LOCAL_OFFSET):X}",
        "root_loads": root_loads,
        "record_address_shapes": record_addresses,
        "record_field_accesses": record_field_accesses,
        "frame_helper_calls": frame_helper_calls,
        "frame_assignments": frame_assignments,
        "consumer_calls": consumer_calls,
        "direct_frame_writes": direct_frame_writes,
    }


def recover_indexed_callback_frame_seed_provenance(
    generated_dir: Path,
    functions: list[dict[str, Any]],
    caller_provenance: list[dict[str, Any]],
    xbe: dict[str, Any],
) -> dict[str, Any]:
    """Recover static table-slot candidates from frame-helper stack seeds.

    For callers that both invoke ``sub_002710F4`` and pass ``ebp - 0x10`` to
    ``sub_0026E70A``, the helper's two preceding pushes are recorded as the
    frame-size and frame-seed shapes.  When the latest local frame-index
    assignment is constant, the corresponding guard and target dwords are
    read from the supplied XBE image.  A zero guard remains a skipped or
    runtime-dependent callback, not a resolved call.
    """
    target_name = f"0x{INDEXED_CALLBACK_CONSUMER_FUNCTION:08X}"
    known_functions = {_address(row["start"]) for row in functions}
    source_cache: dict[str, list[str]] = {}
    rows: list[dict[str, Any]] = []
    push_re = re.compile(r"^\s*PUSH32\(esp,\s*(?P<value>[^)]+)\);")
    frame_index_re = re.compile(
        r"^\s*MEM32\(ebp\s*\+\s*(?:-0x4|-4)\)\s*=\s*"
        r"(?P<value>[^;]+);"
    )

    for caller in caller_provenance:
        if _address(caller.get("target", 0)) != INDEXED_CALLBACK_CONSUMER_FUNCTION:
            continue
        object_push = (caller.get("pushes") or [{}])[-1]
        object_expression = str(object_push.get("operand", "")).strip()
        source_assignment = object_push.get("source_assignment")
        if isinstance(source_assignment, dict):
            object_expression = str(
                source_assignment.get("rhs", object_expression)
            ).strip()
        if re.sub(r"\s+", "", object_expression).lower() not in {
            "ebp+-0x10",
            "ebp+-16",
        }:
            continue

        source_text, separator, line_text = str(caller.get("source", "")).rpartition(":")
        source_path = generated_dir / source_text if separator else None
        if source_path is None or not line_text.isdigit() or not source_path.exists():
            continue
        lines = source_cache.setdefault(
            source_text, source_path.read_text(encoding="utf-8").splitlines()
        )
        call_index = int(line_text) - 1
        if not 0 <= call_index < len(lines):
            continue
        function_start = 0
        function_name = ""
        for index in range(call_index, -1, -1):
            symbol_match = FUNCTION_SYMBOL_RE.match(lines[index])
            if symbol_match is not None:
                function_start = index
                function_name = symbol_match.group(1)
                break
        helper_call_index = None
        for index in range(call_index - 1, function_start - 1, -1):
            if "0x002710F4u, sub_002710F4" in lines[index]:
                helper_call_index = index
                break
        if helper_call_index is None:
            continue

        pushes: list[dict[str, Any]] = []
        for index in range(helper_call_index - 1, function_start - 1, -1):
            push_match = push_re.match(lines[index])
            if push_match is None:
                if pushes:
                    break
                continue
            pushes.append(
                {
                    "source": f"{source_text}:{index + 1}",
                    "expression": lines[index].strip(),
                    "value": push_match.group("value").strip(),
                }
            )
            if len(pushes) == 2:
                break
        pushes.reverse()
        if len(pushes) != 2:
            continue
        frame_size_expression = pushes[0]["value"]
        frame_seed_expression = pushes[1]["value"]
        seed_match = re.fullmatch(
            r"0x(?P<value>[0-9A-Fa-f]{1,8})u?", frame_seed_expression
        )
        if seed_match is None:
            continue
        frame_seed = int(seed_match.group("value"), 16)

        register_constants: dict[str, int] = {}
        frame_index: int | None = None
        frame_index_assignment: dict[str, Any] | None = None
        for index in range(helper_call_index + 1, call_index):
            line = lines[index]
            assignment_match = REGISTER_ASSIGNMENT_RE.match(line)
            if assignment_match is not None:
                register = assignment_match.group("register")
                rhs = assignment_match.group("rhs").strip()
                literal_match = REGISTER_LITERAL_RE.fullmatch(rhs)
                if literal_match is not None:
                    register_constants[register] = int(
                        literal_match.group("value"), 0
                    )
                else:
                    register_constants.pop(register, None)
            index_match = frame_index_re.match(line)
            if index_match is None:
                continue
            value = index_match.group("value").strip()
            literal_match = REGISTER_LITERAL_RE.fullmatch(value)
            if literal_match is not None:
                frame_index = int(literal_match.group("value"), 0)
            elif REGISTER_NAME_RE.fullmatch(value) and value in register_constants:
                frame_index = register_constants[value]
            else:
                frame_index = None
            frame_index_assignment = {
                "source": f"{source_text}:{index + 1}",
                "expression": line.strip(),
                "value": value,
            }
        row: dict[str, Any] = {
            "caller": caller.get("caller"),
            "caller_name": caller.get("caller_name", function_name),
            "source": caller.get("source"),
            "frame_size_expression": frame_size_expression,
            "frame_seed_expression": frame_seed_expression,
            "frame_seed": f"0x{frame_seed:08X}",
            "frame_seed_pushes": pushes,
            "frame_index": frame_index,
            "frame_index_assignment": frame_index_assignment,
            "consumer": target_name,
        }
        if frame_index is None:
            row["status"] = "frame-seed-observed-index-unresolved"
            rows.append(row)
            continue
        base = (
            frame_seed
            + frame_index * INDEXED_CALLBACK_CONSUMER_RECORD_STRIDE
        )
        guard_offset = _file_offset(xbe["sections"], len(xbe["data"]), base + 4)
        target_offset = _file_offset(xbe["sections"], len(xbe["data"]), base + 8)
        if guard_offset is None or target_offset is None:
            row["status"] = "frame-seed-table-slot-outside-xbe"
            rows.append(row)
            continue
        guard = _u32(xbe["data"], guard_offset)
        target = _u32(xbe["data"], target_offset)
        row["table_slot_base"] = f"0x{base:08X}"
        row["guard_address"] = f"0x{base + 4:08X}"
        row["guard_raw_value"] = f"0x{guard:08X}"
        row["target_address"] = f"0x{base + 8:08X}"
        row["target_raw_value"] = f"0x{target:08X}"
        row["target_is_known_function_start"] = target in known_functions
        row["status"] = (
            "static-slot-candidate-zero-guard"
            if guard == 0 and target != 0
            else (
                "static-slot-candidate-nonzero-guard"
                if target != 0
                else "static-slot-empty"
            )
        )
        rows.append(row)

    return {
        "status": (
            "static-frame-seed-slot-candidates-observed"
            if rows
            else "no-static-frame-seed-slot-candidates-observed"
        ),
        "consumer_function": target_name,
        "sites": rows,
        "site_count": len(rows),
        "zero_guard_sites": sum(
            row.get("status") == "static-slot-candidate-zero-guard"
            for row in rows
        ),
        "known_function_target_sites": sum(
            bool(row.get("target_is_known_function_start")) for row in rows
        ),
    }


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
    directly_reachable: set[int] | None,
) -> list[dict[str, Any]]:
    """Find PRCB-cache writes after an FS-based load.

    The ``+0x250`` offset is reused by unrelated structures, so a matching
    write is counted only while a register is proven to contain
    ``MEM32(XBOX_FS_BASE + 0x20)`` (or a direct register copy of that value).
    When ``directly_reachable`` is provided, the result is limited to that
    subgraph; ``None`` scans every named generated function. A zero result
    means that no writer was proven in the selected generated functions; it
    does not claim that the Xbox kernel cannot populate the cache.
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
            if current is None or (
                directly_reachable is not None
                and current not in directly_reachable
            ):
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


def recover_prcb_host_layout_evidence(source_root: Path) -> dict[str, Any]:
    """Verify the pinned host layout used to suppress the PRCB callbacks.

    The host runtime must point ``FS:[0x20]`` at a zeroed block large enough to
    cover ``+0x250`` before any guest instruction is considered.  This helper
    checks the source shape only; it does not execute the runtime or claim
    that a real Xbox kernel has the same initialization behavior.
    """
    source_path = source_root / "xbox_memory_layout.c"
    evidence: dict[str, Any] = {
        "status": "missing-xbox-memory-layout-source",
        "source": None,
        "fake_prcb_va": None,
        "zero_block_bytes": None,
        "fake_prcb_define_source": None,
        "zero_block_source": None,
        "fs_pointer_assignment_source": None,
    }
    if not source_path.exists():
        return evidence

    text = source_path.read_text(encoding="utf-8")
    fake_match = PRCB_HOST_FAKE_ADDRESS_RE.search(text)
    zero_match = PRCB_HOST_ZERO_BLOCK_RE.search(text)
    fs_match = PRCB_HOST_FS_POINTER_RE.search(text)
    evidence["source"] = source_path.name
    if fake_match is not None:
        evidence["fake_prcb_va"] = f"0x{int(fake_match.group('address'), 16):08X}"
        evidence["fake_prcb_define_source"] = (
            f"{source_path.name}:{text.count(chr(10), 0, fake_match.start()) + 1}"
        )
    if zero_match is not None:
        evidence["zero_block_bytes"] = int(zero_match.group("size"), 16)
        evidence["zero_block_source"] = (
            f"{source_path.name}:{text.count(chr(10), 0, zero_match.start()) + 1}"
        )
    if fs_match is not None:
        evidence["fs_pointer_assignment_source"] = (
            f"{source_path.name}:{text.count(chr(10), 0, fs_match.start()) + 1}"
        )
    if (
        fake_match is not None
        and zero_match is not None
        and int(zero_match.group("size"), 16) >= 0x254
        and fs_match is not None
    ):
        evidence["status"] = "verified-zeroed-fake-prcb-cache-layout"
    else:
        evidence["status"] = "incomplete-fake-prcb-cache-layout-evidence"
    return evidence


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


def _count_c_parameters(parameters: str) -> int:
    """Count top-level C parameters in a source signature."""
    normalized = re.sub(r"/\*.*?\*/|//[^\r\n]*", "", parameters, flags=re.S)
    if not normalized.strip() or normalized.strip() == "void":
        return 0
    depth = 0
    count = 1
    for character in normalized:
        if character in "([{":
            depth += 1
        elif character in ")]}":
            depth = max(0, depth - 1)
        elif character == "," and depth == 0:
            count += 1
    return count


def recover_kernel_implementation_signature_evidence(
    source_root: Path,
    target_expected_parameter_counts: dict[str, int],
) -> dict[str, dict[str, Any]]:
    """Compare host definition parameter counts with bridge expectations.

    This is source-signature evidence only.  Matching counts do not prove
    parameter types, pointer translation, calling-convention details, Xbox
    semantics, or side effects.
    """
    evidence = {
        target: {
            "status": "missing-source-definition",
            "sources": [],
            "parameter_counts": [],
            "expected_parameter_count": expected_count,
        }
        for target, expected_count in target_expected_parameter_counts.items()
    }
    for source_path in sorted(source_root.rglob("*.c")):
        text = source_path.read_text(encoding="utf-8")
        relative_source = source_path.relative_to(source_root).as_posix()
        for match in KERNEL_FUNCTION_SIGNATURE_RE.finditer(text):
            target = match.group("name")
            item = evidence.get(target)
            if item is None:
                continue
            source = f"{relative_source}:{text.count(chr(10), 0, match.start()) + 1}"
            if source not in item["sources"]:
                item["sources"].append(source)
            parameter_count = _count_c_parameters(match.group("parameters"))
            if parameter_count not in item["parameter_counts"]:
                item["parameter_counts"].append(parameter_count)
    for item in evidence.values():
        item["sources"].sort()
        item["parameter_counts"].sort()
        if not item["sources"]:
            item["status"] = "missing-source-definition"
        elif item["expected_parameter_count"] in item["parameter_counts"]:
            item["status"] = "parameter-count-match"
        else:
            item["status"] = "parameter-count-mismatch"
    return evidence


def _kernel_return_expression_kind(expression: str | None) -> str:
    if expression is None or not expression.strip():
        return "bare-return"
    normalized = " ".join(expression.strip().split())
    if "STATUS_NOT_IMPLEMENTED" in normalized:
        return "status-not-implemented"
    if normalized == "STATUS_SUCCESS" or normalized in {"0", "0u", "0UL"}:
        return "status-success-or-zero"
    if normalized.startswith("STATUS_"):
        return "status-symbol"
    if re.fullmatch(r"(?:0x[0-9A-Fa-f]+|[0-9]+)(?:u|U|ul|UL)?", normalized):
        return "constant"
    if "xbox_" in normalized or "bridge_" in normalized:
        return "delegated-expression"
    return "expression"


def recover_kernel_return_contract_evidence(
    resolver_source: Path,
    resolver_cases: dict[int, str],
) -> dict[str, dict[str, Any]]:
    """Collect textual return expressions for resolver-selected host targets.

    This is source-shape evidence only.  It does not prove that a return value
    has Xbox-compatible status bits, pointer ownership, side effects, or ABI.
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
            "returns": [],
        }
        for target in target_names
    }
    source_root = resolver_source.parent
    return_re = re.compile(
        r"(?m)^\s*return(?:\s+(?P<expression>[^;\r\n{}]+))?;"
    )
    for source_path in sorted(source_root.rglob("*.c")):
        text = source_path.read_text(encoding="utf-8")
        for match in KERNEL_FUNCTION_DEFINITION_RE.finditer(text):
            target = match.group("name")
            if target not in evidence:
                continue
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
            relative_source = source_path.relative_to(source_root).as_posix()
            function_line = text.count("\n", 0, match.start()) + 1
            source_text = f"{relative_source}:{function_line}"
            if source_text not in evidence[target]["sources"]:
                evidence[target]["sources"].append(source_text)
            evidence[target]["status"] = "source-definition-found"
            for return_match in return_re.finditer(
                text, body_start, body_end + 1
            ):
                expression = return_match.group("expression")
                return_source = (
                    f"{relative_source}:"
                    f"{text.count(chr(10), 0, return_match.start()) + 1}"
                )
                row = {
                    "source": return_source,
                    "expression": (
                        None if expression is None else " ".join(expression.split())
                    ),
                    "kind": _kernel_return_expression_kind(expression),
                }
                if row not in evidence[target]["returns"]:
                    evidence[target]["returns"].append(row)
    for item in evidence.values():
        item["sources"].sort()
        item["returns"].sort(key=lambda row: (row["source"], row["kind"]))
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


def recover_kernel_bridge_body_provenance(
    source_path: Path,
    bridge_cases: dict[int, str],
    used_ordinals: set[int],
) -> dict[str, dict[str, Any]]:
    """Summarize ABI-facing shapes in bridge bodies used by the XBE path.

    The report records stack-argument indices, ``g_eax`` assignments, and
    guest-pointer/memory access macros.  These are source observations only;
    they do not establish Xbox-compatible parameter types or behavior.
    """
    target_names = {
        bridge_cases[ordinal]
        for ordinal in used_ordinals
        if ordinal in bridge_cases
    }
    evidence: dict[str, dict[str, Any]] = {
        target: {
            "status": "missing-source-body",
            "source": None,
            "stack_arg_indices": [],
            "g_eax_assignment_sites": [],
            "explicit_no_op_markers": [],
            "host_call_targets": [],
            "return_disposition": "missing-source-body",
            "pointer_translation_sites": 0,
            "guest_memory_access_sites": 0,
            "body_line_count": 0,
        }
        for target in target_names
    }
    lines = source_path.read_text(encoding="utf-8").splitlines()
    index = 0
    while index < len(lines):
        match = KERNEL_BRIDGE_FUNCTION_DEF_RE.match(lines[index])
        if match is None:
            index += 1
            continue
        target = match.group("name")
        body_lines: list[tuple[int, str]] = []
        depth = 0
        entered_body = False
        body_index = index
        while body_index < len(lines):
            body_line = lines[body_index]
            body_lines.append((body_index + 1, body_line))
            depth += body_line.count("{") - body_line.count("}")
            if depth > 0:
                entered_body = True
            if entered_body and depth == 0:
                break
            body_index += 1
        if target in evidence and entered_body and depth == 0:
            stack_args = {
                int(arg_match.group("index"))
                for _, body_line in body_lines
                for arg_match in KERNEL_BRIDGE_STACK_ARG_RE.finditer(body_line)
            }
            g_eax_sources = [
                f"{source_path.name}:{line_number}"
                for line_number, body_line in body_lines
                if re.search(r"\bg_eax\s*=", body_line) is not None
            ]
            explicit_no_op_markers = [
                f"{source_path.name}:{line_number}"
                for line_number, body_line in body_lines
                if re.search(r"\b(?:no[- ]op|stub)\b", body_line, re.IGNORECASE)
                is not None
            ]
            host_call_targets = sorted(
                {
                    call_match.group("name")
                    for _, body_line in body_lines
                    for call_match in KERNEL_BRIDGE_HOST_CALL_RE.finditer(body_line)
                }
            )
            item = evidence[target]
            item["status"] = "source-body-found"
            item["source"] = f"{source_path.name}:{index + 1}"
            item["stack_arg_indices"] = sorted(stack_args)
            item["g_eax_assignment_sites"] = g_eax_sources
            item["explicit_no_op_markers"] = explicit_no_op_markers
            item["host_call_targets"] = host_call_targets
            item["return_disposition"] = (
                "explicit-g-eax-assignment"
                if g_eax_sources
                else (
                    "terminal-host-call-no-g-eax"
                    if any(
                        call in KERNEL_BRIDGE_TERMINAL_HOST_CALLS
                        for call in host_call_targets
                    )
                    else "missing-g-eax-and-no-terminal-call"
                )
            )
            item["pointer_translation_sites"] = sum(
                len(re.findall(r"\bXBOX_TO_NATIVE\s*\(", body_line))
                for _, body_line in body_lines
            )
            item["guest_memory_access_sites"] = sum(
                len(re.findall(r"\bBRIDGE_MEM32\s*\(", body_line))
                for _, body_line in body_lines
            )
            item["body_line_count"] = len(body_lines)
        index = max(body_index + 1, index + 1)
    return evidence


def classify_kernel_bridge_implementation_delegation(
    resolver_target: str | None,
    host_call_targets: list[str],
    host_implementation_body_markers: list[dict[str, Any]],
) -> str:
    """Classify whether a bridge delegates or adapts its resolver target."""
    if resolver_target is not None and resolver_target in host_call_targets:
        return "direct-implementation-delegation"
    if host_call_targets:
        return "bridge-local-adaptation"
    if any(
        marker["kind"] == "explicit-stub-or-no-op"
        for marker in host_implementation_body_markers
    ):
        return "bridge-local-adaptation-over-host-stub"
    return "bridge-local-no-direct-host-call"


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


def classify_kernel_bridge_argument_use(
    expected_arg_count: int | None,
    stack_arg_indices: list[int],
    explicit_stub: bool = False,
) -> str:
    """Classify which expected caller arguments a bridge reads.

    ``STACK_ARG`` use is only source-shape evidence.  A partial read may be
    intentional when the bridge supplies host-side defaults, and does not
    establish Xbox parameter types or semantics.
    """
    if expected_arg_count is None:
        return "missing-expected-count"
    if any(index < 0 or index >= expected_arg_count for index in stack_arg_indices):
        return "stack-arg-index-outside-expected-count"
    if expected_arg_count == 0:
        return "zero-argument-bridge" if not stack_arg_indices else "stack-arg-index-outside-expected-count"
    if not stack_arg_indices:
        return (
            "explicit-stub-without-argument-read"
            if explicit_stub
            else "no-observed-argument-read"
        )
    if len(set(stack_arg_indices)) == expected_arg_count:
        return "all-expected-arguments-read"
    return "partial-expected-arguments-read"


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


def recover_dynamic_indexed_writer_root_link_provenance(
    generated_dir: Path,
    writers: list[dict[str, Any]],
) -> dict[str, Any]:
    """Search indexed writer bodies for a direct ``0x00361360`` reference.

    This is a deliberately bounded negative/positive check.  It groups the
    existing ``base + index * 4 + 8`` writer shapes by generated function and
    records literal references to the indexed BSS root in the same function
    body.  Absence means only that this local body scan found no direct root
    expression; argument propagation, indirect calls, and runtime aliasing
    remain outside the check.
    """
    indexed_writers = [
        writer
        for writer in writers
        if writer.get("source") and writer.get("function")
    ]
    if not indexed_writers:
        return {
            "status": "no-indexed-writer-sites-observed",
            "writer_sites": 0,
            "writer_functions": 0,
            "writer_sites_with_root_reference": 0,
            "writer_functions_with_root_reference": 0,
            "functions": [],
        }

    source_paths = {
        path.name: path for path in generated_dir.rglob("recomp_*.c")
    }
    function_rows: dict[tuple[int, str], dict[str, Any]] = {}
    unresolved_sites = 0
    for writer in indexed_writers:
        source_text, separator, line_text = str(writer["source"]).rpartition(":")
        source_path = source_paths.get(source_text)
        if not separator or not line_text.isdigit() or source_path is None:
            unresolved_sites += 1
            continue
        lines = source_path.read_text(encoding="utf-8").splitlines()
        writer_index = int(line_text) - 1
        if not 0 <= writer_index < len(lines):
            unresolved_sites += 1
            continue
        function_start = 0
        current_name = ""
        for index in range(writer_index, -1, -1):
            symbol_match = FUNCTION_SYMBOL_RE.match(lines[index])
            if symbol_match is not None:
                function_start = index
                current_name = symbol_match.group(1)
                break
        function_end = len(lines)
        for index in range(function_start + 1, len(lines)):
            if FUNCTION_SYMBOL_RE.match(lines[index]) is not None:
                function_end = index
                break
        root_references = [
            {
                "source": f"{source_text}:{index + 1}",
                "expression": lines[index].strip(),
            }
            for index in range(function_start, function_end)
            if re.search(r"0x(?:00361360|361360)\b", lines[index]) is not None
        ]
        key = (_address(writer["function"]), current_name)
        row = function_rows.setdefault(
            key,
            {
                "function": f"0x{key[0]:08X}",
                "function_name": current_name,
                "source_file": source_text,
                "writer_sites": 0,
                "root_references": root_references,
            },
        )
        row["writer_sites"] += 1
        if root_references and not row["root_references"]:
            row["root_references"] = root_references

    rows = sorted(
        function_rows.values(),
        key=lambda row: (_address(row["function"]), row["function_name"]),
    )
    writer_sites_with_root_reference = sum(
        row["writer_sites"] for row in rows if row["root_references"]
    )
    writer_functions_with_root_reference = sum(
        bool(row["root_references"]) for row in rows
    )
    return {
        "status": (
            "incomplete-indexed-writer-root-link-provenance"
            if unresolved_sites
            else (
                "root-reference-shape-observed-in-writer-function-bodies"
                if writer_functions_with_root_reference
                else "verified-bounded-no-root-reference-in-writer-function-bodies"
            )
        ),
        "writer_sites": len(indexed_writers),
        "writer_functions": len(rows),
        "writer_sites_with_root_reference": writer_sites_with_root_reference,
        "writer_functions_with_root_reference": writer_functions_with_root_reference,
        "unresolved_sites": unresolved_sites,
        "functions": rows,
    }


def recover_dynamic_object_field_writer_context(
    generated_dir: Path,
    writers: list[dict[str, Any]],
) -> list[dict[str, Any]]:
    """Recover immediate register sources for ``object + 0x584`` writers.

    The result is deliberately a local, post-call window.  It records the
    nearest assignments to the base and value registers after the previous
    direct call in the same generated function.  This can expose a repeated
    construction shape, but it does not prove path dominance, object identity,
    allocation semantics, or that the value is a callable target.
    """
    source_paths = {
        path.name: path for path in generated_dir.rglob("recomp_*.c")
    }
    contexts: list[dict[str, Any]] = []

    def nearest_assignment(
        lines: list[str], register: str, start: int, lower_bound: int
    ) -> dict[str, Any] | None:
        for index in range(start, lower_bound - 1, -1):
            match = REGISTER_ASSIGNMENT_RE.match(lines[index])
            if match is None or match.group("register") != register:
                continue
            return {
                "source": f"{source_name}:{index + 1}",
                "expression": lines[index].strip(),
                "register": register,
                "rhs": match.group("rhs").strip(),
            }
        return None

    for writer in writers:
        source_text = str(writer.get("source", ""))
        source_name, separator, line_text = source_text.rpartition(":")
        if not separator or not line_text.isdigit():
            contexts.append(
                {
                    "writer": writer,
                    "status": "writer-source-unresolved",
                }
            )
            continue
        source_path = source_paths.get(source_name)
        if source_path is None:
            contexts.append(
                {
                    "writer": writer,
                    "status": "writer-source-file-unresolved",
                }
            )
            continue

        lines = source_path.read_text(encoding="utf-8").splitlines()
        writer_index = int(line_text) - 1
        if writer_index < 0 or writer_index >= len(lines):
            contexts.append(
                {
                    "writer": writer,
                    "status": "writer-source-line-unresolved",
                }
            )
            continue

        function_start = 0
        for index in range(writer_index, -1, -1):
            if FUNCTION_SYMBOL_RE.match(lines[index]) is not None:
                function_start = index
                break

        field_write = DYNAMIC_OBJECT_FIELD_WRITE_RE.search(lines[writer_index])
        value_match = DYNAMIC_WRITER_ASSIGNMENT_RE.search(lines[writer_index])
        if field_write is None or value_match is None:
            contexts.append(
                {
                    "writer": writer,
                    "status": "object-field-write-shape-unresolved",
                }
            )
            continue

        last_direct_call = function_start
        for index in range(function_start, writer_index):
            if "RECOMP_ABI_CALL(" in lines[index]:
                last_direct_call = index

        base_register = field_write.group("register")
        value_expression = value_match.group("value").strip()
        value_register_match = REGISTER_NAME_RE.fullmatch(value_expression)
        value_register = (
            value_register_match.group("register")
            if value_register_match is not None
            else None
        )
        base_source = nearest_assignment(
            lines, base_register, writer_index - 1, last_direct_call + 1
        )
        value_source = (
            nearest_assignment(
                lines, value_register, writer_index - 1, last_direct_call + 1
            )
            if value_register is not None
            else None
        )
        if base_source is not None and value_source is not None:
            status = "post-call-register-sources-observed"
        elif base_source is not None or value_source is not None:
            status = "partial-post-call-register-sources-observed"
        else:
            status = "post-call-register-sources-unresolved"

        base_rhs = base_source["rhs"] if base_source is not None else None
        value_rhs = value_source["rhs"] if value_source is not None else None
        construction_shape = (
            "local-object-base-and-value-load-shape"
            if base_rhs == "MEM32(ebp + -28)"
            and value_rhs == "MEM32(ebp + -84)"
            else "not-observed"
        )
        contexts.append(
            {
                "writer": writer,
                "status": status,
                "base_register": base_register,
                "value_expression": value_expression,
                "value_register": value_register,
                "base_source": base_source,
                "value_source": value_source,
                "base_source_kind": (
                    _dynamic_writer_value_kind(base_rhs)
                    if base_rhs is not None
                    else "unresolved"
                ),
                "value_source_kind": (
                    _dynamic_writer_value_kind(value_rhs)
                    if value_rhs is not None
                    else "unresolved"
                ),
                "construction_shape": construction_shape,
            }
        )
    return contexts


def recover_dynamic_object_field_writer_overlap_provenance(
    writers: list[dict[str, Any]],
    functions: list[dict[str, Any]],
) -> dict[str, Any]:
    """Group object-field writer sites by overlapping function metadata.

    The disassembly can expose tail-jump aliases or interior labels as
    function entries. This report keeps every writer site, then groups the
    corresponding function intervals when they overlap. It is a metadata
    de-duplication aid only: overlapping intervals do not prove that entries
    are aliases at runtime or that any writer reaches the callback object.
    """
    function_by_address = {
        _address(function["start"]): function for function in functions
    }
    writer_by_function: dict[int, list[dict[str, Any]]] = {}
    for writer in writers:
        if not writer.get("function"):
            continue
        address = _address(writer["function"])
        writer_by_function.setdefault(address, []).append(writer)

    function_rows: list[dict[str, Any]] = []
    unresolved_functions = 0
    for address, site_rows in sorted(writer_by_function.items()):
        metadata = function_by_address.get(address)
        if metadata is None or metadata.get("end") is None:
            unresolved_functions += 1
            function_rows.append(
                {
                    "function": f"0x{address:08X}",
                    "function_name": str(site_rows[0].get("function_name", "")),
                    "metadata_status": "function-interval-unresolved",
                    "writer_sites": site_rows,
                }
            )
            continue
        start = address
        end = _address(metadata["end"])
        function_rows.append(
            {
                "function": f"0x{address:08X}",
                "function_name": str(metadata.get("name", "")),
                "interval_start": f"0x{start:08X}",
                "interval_end": f"0x{end:08X}",
                "size": metadata.get("size"),
                "detection_method": metadata.get("detection_method"),
                "metadata_status": "function-interval-observed",
                "writer_sites": site_rows,
            }
        )

    indexed_rows = [
        row
        for row in function_rows
        if row.get("metadata_status") == "function-interval-observed"
    ]
    adjacency: dict[int, set[int]] = {
        index: set() for index in range(len(indexed_rows))
    }
    for left_index, left in enumerate(indexed_rows):
        left_start = _address(left["interval_start"])
        left_end = _address(left["interval_end"])
        for right_index in range(left_index + 1, len(indexed_rows)):
            right = indexed_rows[right_index]
            right_start = _address(right["interval_start"])
            right_end = _address(right["interval_end"])
            if left_start < right_end and right_start < left_end:
                adjacency[left_index].add(right_index)
                adjacency[right_index].add(left_index)

    components: list[list[int]] = []
    seen: set[int] = set()
    for index in range(len(indexed_rows)):
        if index in seen:
            continue
        pending = [index]
        seen.add(index)
        component: list[int] = []
        while pending:
            current = pending.pop()
            component.append(current)
            for neighbor in sorted(adjacency[current]):
                if neighbor not in seen:
                    seen.add(neighbor)
                    pending.append(neighbor)
        components.append(sorted(component))

    overlap_groups: list[dict[str, Any]] = []
    overlap_group_by_index: dict[int, int] = {}
    overlap_components = [component for component in components if len(component) > 1]
    for group_id, component in enumerate(overlap_components):
        for index in component:
            overlap_group_by_index[index] = group_id
        members = [indexed_rows[index] for index in component]
        overlap_groups.append(
            {
                "group": group_id,
                "members": [
                    {
                        "function": member["function"],
                        "function_name": member["function_name"],
                        "interval_start": member["interval_start"],
                        "interval_end": member["interval_end"],
                        "detection_method": member["detection_method"],
                        "writer_site_count": len(member["writer_sites"]),
                    }
                    for member in members
                ],
                "union_start": f"0x{min(_address(member['interval_start']) for member in members):08X}",
                "union_end": f"0x{max(_address(member['interval_end']) for member in members):08X}",
            }
        )

    for index, row in enumerate(indexed_rows):
        row["overlap_group"] = overlap_group_by_index.get(index)

    overlapping_writer_functions = sum(
        len(component) for component in overlap_components
    )
    return {
        "status": (
            "incomplete-writer-function-overlap-provenance"
            if unresolved_functions
            else (
                "overlapping-writer-function-intervals-observed"
                if overlap_groups
                else "verified-no-overlapping-writer-function-intervals"
            )
        ),
        "writer_sites": len(writers),
        "writer_functions": len(writer_by_function),
        "metadata_resolved_writer_functions": len(indexed_rows),
        "overlap_group_count": len(overlap_groups),
        "overlapping_writer_functions": overlapping_writer_functions,
        "unresolved_function_intervals": unresolved_functions,
        "overlap_groups": overlap_groups,
        "functions": function_rows,
    }


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


def recover_dynamic_source_provenance(
    generated_dir: Path,
    classified: list[dict[str, Any]],
) -> dict[str, dict[str, Any]]:
    """Recover bounded linear assignments feeding unresolved call operands.

    This records only assignments observed before each indirect-call site in
    the generated function.  It deliberately does not perform CFG-sensitive
    aliasing, object identity, callback resolution, or ABI inference.  The
    small recursive walk exists only to retain a nearby self-reassignment
    chain such as ``eax = MEM32(eax + 0x250)``.
    """
    dynamic_classes = {
        "dynamic-register-or-object-target",
        "dynamic-memory-target",
        "dynamic-object-field-target",
        "dynamic-indexed-record-target",
        "prcb-guarded-dynamic-callback",
    }
    targets = {
        _address(row["call_site"]): row
        for row in classified
        if row.get("classification") in dynamic_classes
    }
    if not targets:
        return {}

    register_names = "eax|ebx|ecx|edx|esi|edi"
    register_token_re = re.compile(rf"\b(?:{register_names})\b")
    call_site_re = re.compile(
        r"RECOMP_ICALL_SAFE_AT\([^,]+,\s*[^,]+,\s*"
        r"0x(?P<site>[0-9A-Fa-f]{8})u\)"
    )
    address_by_name = {
        str(row.get("function_name", "")): _address(row["function"])
        for row in classified
        if row.get("function_name")
    }
    evidence: dict[str, dict[str, Any]] = {}

    for source_path in sorted(generated_dir.rglob("recomp_*.c")):
        current_function_name = ""
        current_function_address: int | None = None
        assignments: dict[str, list[dict[str, Any]]] = {}
        lines = source_path.read_text(encoding="utf-8").splitlines()
        for line_number, line in enumerate(lines, 1):
            symbol_match = FUNCTION_SYMBOL_RE.match(line)
            if symbol_match:
                current_function_name = symbol_match.group(1)
                current_function_address = address_by_name.get(current_function_name)
                assignments = {}
            if current_function_address is None:
                continue

            call_match = call_site_re.search(line)
            if call_match is not None:
                call_site = int(call_match.group("site"), 16)
                row = targets.get(call_site)
                if row is not None and str(row["function"]) == f"0x{current_function_address:08X}":
                    target_expression = str(row["target_expression"])
                    target_registers = sorted(set(register_token_re.findall(target_expression)))
                    selected: list[dict[str, Any]] = []
                    selected_keys: set[tuple[str, int]] = set()

                    def add_register(
                        register: str, before_line: int, depth: int = 0
                    ) -> None:
                        if depth > 4:
                            return
                        history = assignments.get(register, [])
                        if not history:
                            return
                        eligible = [
                            index
                            for index, item in enumerate(history)
                            if int(item["line_number"]) < before_line
                        ]
                        if not eligible:
                            return
                        index = eligible[-1]
                        key = (register, index)
                        if key in selected_keys:
                            return
                        selected_keys.add(key)
                        assignment = history[index]
                        selected.append(assignment)
                        rhs = str(assignment["rhs"])
                        rhs_registers = register_token_re.findall(rhs)
                        for dependency in sorted(set(rhs_registers)):
                            add_register(
                                dependency,
                                int(assignment["line_number"]),
                                depth + 1,
                            )

                    for register in target_registers:
                        add_register(register, line_number)

                    covered_registers = {item["register"] for item in selected}
                    covered_target_registers = covered_registers & set(target_registers)
                    evidence[row["call_site"]] = {
                        "call_site": row["call_site"],
                        "function": row["function"],
                        "function_name": row.get("function_name", current_function_name),
                        "classification": row["classification"],
                        "target_expression": target_expression,
                        "target_registers": target_registers,
                        "covered_target_registers": sorted(covered_target_registers),
                        "source_registers": sorted(covered_registers),
                        "status": (
                            "local-linear-source-assignments-observed"
                            if set(target_registers) <= covered_registers
                            else "local-linear-source-assignments-incomplete"
                        ),
                        "assignments": selected,
                    }

            assignment_match = REGISTER_ASSIGNMENT_RE.match(line)
            if assignment_match is not None:
                register = assignment_match.group("register")
                assignments.setdefault(register, []).append(
                    {
                        "register": register,
                        "rhs": assignment_match.group("rhs").strip(),
                        "expression": line.strip(),
                        "line_number": line_number,
                        "source": f"{source_path.name}:{line_number}",
                    }
                )
    return evidence


def recover_prcb_callback_cache_provenance(
    classified: list[dict[str, Any]],
    dynamic_source_provenance: dict[str, dict[str, Any]],
) -> dict[str, dict[str, Any]]:
    """Summarize the bounded source shape for PRCB-backed callbacks.

    The report joins the already recovered linear assignments with the
    classified target expression.  It records the observed
    ``FS:[0x20] -> +0x250 -> +0x14`` shape and the local guard flag, but it
    deliberately does not resolve the callback value or claim kernel/runtime
    behavior.
    """
    cache_load_re = re.compile(
        r"^(?P<register>eax|ebx|ecx|edx|esi|edi)\s*=\s*"
        r"MEM32\((?P<base>eax|ebx|ecx|edx|esi|edi)\s*\+\s*0x250\);$"
    )
    callback_field_re = re.compile(
        r"^MEM32\((?P<base>eax|ebx|ecx|edx|esi|edi)\s*\+\s*0x14\)$"
    )
    evidence: dict[str, dict[str, Any]] = {}
    for row in classified:
        if row.get("classification") != "prcb-guarded-dynamic-callback":
            continue
        call_site = str(row["call_site"])
        provenance = dynamic_source_provenance.get(call_site)
        assignments = [] if provenance is None else provenance.get("assignments", [])
        fs_assignment = next(
            (
                assignment
                for assignment in assignments
                if PRCB_BASE_LOAD_RE.match(str(assignment.get("expression", "")))
            ),
            None,
        )
        cache_assignment = None
        if fs_assignment is not None:
            fs_register = str(fs_assignment["register"])
            for assignment in assignments:
                cache_match = cache_load_re.fullmatch(
                    str(assignment.get("expression", ""))
                )
                if cache_match is not None and cache_match.group("base") == fs_register:
                    cache_assignment = assignment
                    break
        callback_match = callback_field_re.fullmatch(
            str(row.get("target_expression", ""))
        )
        cache_register = (
            None if cache_assignment is None else str(cache_assignment["register"])
        )
        callback_register = (
            None if callback_match is None else callback_match.group("base")
        )
        complete = (
            fs_assignment is not None
            and cache_assignment is not None
            and callback_match is not None
            and cache_register == callback_register
        )
        evidence[call_site] = {
            "call_site": call_site,
            "function": row["function"],
            "function_name": row.get("function_name", ""),
            "classification": row["classification"],
            "source_expression": "MEM32(XBOX_FS_BASE + 0x20)",
            "source_register": (
                None if fs_assignment is None else str(fs_assignment["register"])
            ),
            "cache_offset": "0x250",
            "cache_register": cache_register,
            "callback_field_offset": "0x14",
            "callback_register": callback_register,
            "target_expression": row.get("target_expression", ""),
            "guarded": bool(row.get("prcb_cache_guarded")),
            "source_assignment": fs_assignment,
            "cache_assignment": cache_assignment,
            "status": (
                "prcb-cache-callback-field-shape-observed"
                if complete
                else "incomplete-prcb-cache-callback-field-shape"
            ),
        }
    return evidence


def classify_dynamic_callback_initialization(
    classified: list[dict[str, Any]],
    prcb_host_layout_evidence: dict[str, Any] | None,
    indexed_callback_frame_seed_provenance: dict[str, Any] | None,
    indexed_callback_root_link_provenance: dict[str, Any] | None,
    dynamic_target_outside_direct_writers: dict[str, list[dict[str, Any]]] | None = None,
) -> dict[str, Any]:
    """Classify bounded initial-state evidence for unresolved callbacks.

    This is deliberately not a target resolver.  A zero value in the input
    image, or a zeroed host cache, is recorded as a conservative quiescence
    observation only.  Object and indexed-record callbacks remain
    runtime-initialization-unproven even when their surrounding data layout is
    understood.
    """
    dynamic_classes = {
        "dynamic-memory-target",
        "dynamic-object-field-target",
        "dynamic-indexed-record-target",
        "prcb-guarded-dynamic-callback",
        "dynamic-register-or-object-target",
    }
    outside_writers = dynamic_target_outside_direct_writers or {}
    frame_seed = indexed_callback_frame_seed_provenance or {}
    root_link = indexed_callback_root_link_provenance or {}
    host_prcb = prcb_host_layout_evidence or {}
    required_prcb_zero_block_bytes = 0x250 + 0x14 + 4
    host_prcb_zeroed = (
        host_prcb.get("status") == "verified-zeroed-fake-prcb-cache-layout"
        and host_prcb.get("fake_prcb_va") is not None
        and int(host_prcb.get("zero_block_bytes") or 0) >= required_prcb_zero_block_bytes
        and host_prcb.get("fs_pointer_assignment_source") is not None
    )

    site_rows: list[dict[str, Any]] = []
    for row in classified:
        classification = row.get("classification")
        if classification not in dynamic_classes:
            continue

        evidence: dict[str, Any] = {}
        if classification == "dynamic-memory-target":
            raw_values = row.get("target_memory_raw_values", {})
            all_zero = bool(raw_values) and all(
                value == "0x00000000" for value in raw_values.values()
            )
            guarded = "conditional_nonzero_guard_evidence" in row
            no_direct_writer = row.get("direct_writer_status") == (
                "no-proven-direct-writer"
            )
            evidence = {
                "target_memory_raw_values": raw_values,
                "nonzero_guard_observed": guarded,
                "direct_writer_status": row.get("direct_writer_status"),
                "outside_direct_writer_candidates": len(
                    outside_writers.get("dynamic-memory-target", [])
                ),
            }
            status = (
                "static-image-zero-guarded"
                if all_zero and guarded and no_direct_writer
                else "runtime-memory-initialization-unproven"
            )
        elif classification == "prcb-guarded-dynamic-callback":
            provenance = row.get("prcb_callback_cache_provenance", {})
            shape_observed = provenance.get("status") == (
                "prcb-cache-callback-field-shape-observed"
            )
            guarded = bool(provenance.get("guarded")) or (
                "conditional_skip_evidence" in row
            )
            evidence = {
                "host_layout_status": host_prcb.get("status"),
                "host_fake_prcb_va": host_prcb.get("fake_prcb_va"),
                "host_zero_block_bytes": host_prcb.get("zero_block_bytes"),
                "required_zero_block_bytes": required_prcb_zero_block_bytes,
                "callback_field_offset": provenance.get("callback_field_offset"),
                "cache_offset": provenance.get("cache_offset"),
                "callback_field_shape_status": provenance.get("status"),
                "guarded": guarded,
                "xbox_runtime_value_proven": False,
            }
            status = (
                "host-fake-prcb-zeroed-guarded"
                if host_prcb_zeroed and shape_observed and guarded
                else "runtime-prcb-state-unproven"
            )
        elif classification == "dynamic-object-field-target":
            evidence = {
                "target_source_expression": row.get("target_source_expression"),
                "direct_writer_status": row.get("direct_writer_status"),
                "outside_direct_writer_candidates": len(
                    outside_writers.get("dynamic-object-field-target", [])
                ),
            }
            status = "runtime-object-field-initialization-unproven"
        elif classification == "dynamic-indexed-record-target":
            evidence = {
                "frame_seed_status": frame_seed.get("status"),
                "frame_seed_sites": frame_seed.get("site_count", 0),
                "frame_seed_zero_guard_sites": frame_seed.get(
                    "zero_guard_sites", 0
                ),
                "frame_seed_known_function_target_sites": frame_seed.get(
                    "known_function_target_sites", 0
                ),
                "root_link_status": root_link.get("status"),
                "outside_direct_writer_candidates": len(
                    outside_writers.get("dynamic-indexed-record-target", [])
                ),
            }
            status = "runtime-indexed-record-initialization-unproven"
        else:
            evidence = {
                "target_expression": row.get("target_expression"),
                "direct_writer_status": row.get("direct_writer_status"),
            }
            status = "dynamic-initialization-unclassified"

        site_rows.append(
            {
                "call_site": row["call_site"],
                "function": row["function"],
                "function_name": row.get("function_name", ""),
                "classification": classification,
                "status": status,
                "evidence": evidence,
            }
        )

    class_counts = Counter(row["status"] for row in site_rows)
    unclassified_sites = class_counts["dynamic-initialization-unclassified"]
    return {
        "status": (
            "verified-conservative-dynamic-callback-initialization-classification"
            if site_rows and not unclassified_sites
            else "incomplete-conservative-dynamic-callback-initialization-classification"
        ),
        "site_count": len(site_rows),
        "static_image_zero_guard_sites": class_counts[
            "static-image-zero-guarded"
        ],
        "host_prcb_zero_guard_sites": class_counts[
            "host-fake-prcb-zeroed-guarded"
        ],
        "runtime_initialization_unproven_sites": sum(
            count
            for status, count in class_counts.items()
            if status.endswith("initialization-unproven")
            or status == "runtime-prcb-state-unproven"
        ),
        "unclassified_sites": unclassified_sites,
        "class_counts": dict(class_counts),
        "semantics": "not-claimed",
        "sites": site_rows,
    }


def recover_dynamic_callback_caller_provenance(
    generated_dir: Path,
    classified: list[dict[str, Any]],
    functions: list[dict[str, Any]],
    directly_reachable: set[int] | None,
) -> list[dict[str, Any]]:
    """Recover direct-call push shapes into functions containing dynamic sites.

    This is caller-side stack evidence only.  It does not assign parameter
    types, identify an object class, or prove that a pushed pointer reaches a
    particular callback table at runtime.
    """
    dynamic_function_addresses = {
        _address(row["function"])
        for row in classified
        if row.get("classification")
        in {
            "dynamic-register-or-object-target",
            "dynamic-memory-target",
            "dynamic-object-field-target",
            "dynamic-indexed-record-target",
            "prcb-guarded-dynamic-callback",
        }
    }
    if not dynamic_function_addresses:
        return []
    function_addresses = {
        str(function.get("name", "")): _address(function["start"])
        for function in functions
    }
    target_names = {
        address: next(
            (
                str(row.get("function_name", ""))
                for row in classified
                if _address(row["function"]) == address
            ),
            "",
        )
        for address in dynamic_function_addresses
    }
    results: list[dict[str, Any]] = []

    for source_path in sorted(generated_dir.rglob("recomp_*.c")):
        current_name = ""
        current_address: int | None = None
        function_start_line = 0
        register_assignments: dict[str, list[dict[str, Any]]] = {}
        lines = source_path.read_text(encoding="utf-8").splitlines()
        for line_number, line in enumerate(lines, 1):
            symbol_match = FUNCTION_SYMBOL_RE.match(line)
            if symbol_match:
                current_name = symbol_match.group(1)
                current_address = function_addresses.get(current_name)
                function_start_line = line_number
                register_assignments = {}
            if current_address is None:
                continue

            call_match = DIRECT_ABI_CALL_RE.search(line)
            if call_match is not None:
                target_address = int(call_match.group("target"), 16)
                if (
                    target_address in dynamic_function_addresses
                    and (
                        directly_reachable is None
                        or current_address in directly_reachable
                    )
                ):
                    pushes: list[dict[str, Any]] = []
                    for prior_index in range(line_number - 2, function_start_line - 2, -1):
                        prior_line = lines[prior_index]
                        if LOCAL_LABEL_RE.match(prior_line) is not None:
                            break
                        if (
                            DIRECT_ABI_CALL_RE.search(prior_line) is not None
                            or INDIRECT_CALL_RE.search(prior_line) is not None
                        ):
                            break
                        push_match = PUSH32_RE.match(prior_line)
                        if push_match is None:
                            continue
                        operand = push_match.group("operand").strip()
                        push: dict[str, Any] = {
                            "source": f"{source_path.name}:{prior_index + 1}",
                            "operand": operand,
                        }
                        if REGISTER_NAME_RE.fullmatch(operand) is not None:
                            history = register_assignments.get(operand, [])
                            if history:
                                push["source_assignment"] = history[-1]
                        pushes.append(push)
                    pushes.reverse()
                    results.append(
                        {
                            "caller": f"0x{current_address:08X}",
                            "caller_name": current_name,
                            "source": f"{source_path.name}:{line_number}",
                            "target": f"0x{target_address:08X}",
                            "target_name": target_names[target_address],
                            "pushes": pushes,
                            "status": "direct-call-push-shape-observed",
                        }
                    )

            assignment_match = REGISTER_ASSIGNMENT_RE.match(line)
            if assignment_match is not None:
                register = assignment_match.group("register")
                register_assignments.setdefault(register, []).append(
                    {
                        "register": register,
                        "rhs": assignment_match.group("rhs").strip(),
                        "expression": line.strip(),
                        "source": f"{source_path.name}:{line_number}",
                    }
                )
    return results


def recover_dynamic_indexed_callback_frame_provenance(
    generated_dir: Path,
    classified: list[dict[str, Any]],
    caller_provenance: list[dict[str, Any]],
) -> dict[str, Any]:
    """Prove the frame-local object shape passed to the indexed consumer.

    The two direct callers in the current entry-point subgraph pass ``ebp -
    0x10`` to ``sub_0026E70A`` after calling ``sub_002710F4``.  The helper
    writes that same frame address to ``XBOX_FS_BASE``.  This records the
    bounded shape without treating the frame object as identical to the
    separate indexed-table root at ``0x00361360`` or resolving its callback.
    """
    indexed_rows = [
        row
        for row in classified
        if row.get("classification") == "dynamic-indexed-record-target"
        and _address(row.get("function", 0)) == INDEXED_CALLBACK_CONSUMER_FUNCTION
    ]
    if not indexed_rows or not caller_provenance:
        return {
            "status": "no-indexed-callback-frame-provenance-observed",
            "consumer_function": f"0x{INDEXED_CALLBACK_CONSUMER_FUNCTION:08X}",
            "frame_helper_function": f"0x{INDEXED_CALLBACK_FRAME_HELPER_FUNCTION:08X}",
            "sites": [],
            "table_root_link_status": "not-assessed",
        }

    source_cache: dict[str, list[str]] = {}
    helper_evidence: dict[str, Any] | None = None
    helper_name = f"sub_{INDEXED_CALLBACK_FRAME_HELPER_FUNCTION:08X}"
    helper_call_text = (
        f"0x{INDEXED_CALLBACK_FRAME_HELPER_FUNCTION:08X}u, {helper_name}"
    )

    for source_path in sorted(generated_dir.rglob("recomp_*.c")):
        lines = source_path.read_text(encoding="utf-8").splitlines()
        current_name = ""
        function_lines: list[tuple[int, str]] = []

        def inspect_helper() -> None:
            nonlocal helper_evidence
            if current_name != helper_name or not function_lines:
                return
            fs_read = next(
                (
                    (line_number, line.strip())
                    for line_number, line in function_lines
                    if re.search(r"=\s*MEM32\(XBOX_FS_BASE\);", line)
                ),
                None,
            )
            frame_assignment = next(
                (
                    (line_number, line.strip(), match.group("register"))
                    for line_number, line in function_lines
                    for match in [INDEXED_CALLBACK_FRAME_LOCAL_ASSIGNMENT_RE.match(line)]
                    if match is not None
                ),
                None,
            )
            frame_write = None
            if frame_assignment is not None:
                register = frame_assignment[2]
                frame_write = next(
                    (
                        (line_number, line.strip())
                        for line_number, line in function_lines
                        if re.search(
                            rf"MEM32\(XBOX_FS_BASE\)\s*=\s*{register};",
                            line,
                        )
                    ),
                    None,
                )
            helper_evidence = {
                "source_file": source_path.name,
                "function": f"0x{INDEXED_CALLBACK_FRAME_HELPER_FUNCTION:08X}",
                "fs_base_read": (
                    None
                    if fs_read is None
                    else {
                        "source": f"{source_path.name}:{fs_read[0]}",
                        "expression": fs_read[1],
                    }
                ),
                "frame_assignment": (
                    None
                    if frame_assignment is None
                    else {
                        "source": f"{source_path.name}:{frame_assignment[0]}",
                        "expression": frame_assignment[1],
                        "register": frame_assignment[2],
                        "offset": f"0x{abs(INDEXED_CALLBACK_FRAME_LOCAL_OFFSET):X}",
                    }
                ),
                "fs_base_write": (
                    None
                    if frame_write is None
                    else {
                        "source": f"{source_path.name}:{frame_write[0]}",
                        "expression": frame_write[1],
                    }
                ),
            }
            helper_evidence["status"] = (
                "verified-helper-publishes-ebp-minus-0x10-to-fs"
                if fs_read is not None
                and frame_assignment is not None
                and frame_write is not None
                else "incomplete-indexed-callback-frame-helper-shape"
            )

        for line_number, line in enumerate(lines, 1):
            symbol_match = FUNCTION_SYMBOL_RE.match(line)
            if symbol_match is not None:
                inspect_helper()
                current_name = symbol_match.group(1)
                function_lines = [(line_number, line)]
                continue
            if current_name:
                function_lines.append((line_number, line))
        inspect_helper()
        if helper_evidence is not None and helper_evidence["status"].startswith(
            "verified-"
        ):
            break

    target_addresses = {
        _address(row["function"])
        for row in indexed_rows
    }
    source_rows: list[dict[str, Any]] = []
    for caller in caller_provenance:
        if _address(caller.get("target", 0)) not in target_addresses:
            continue
        source_text, separator, line_text = str(caller.get("source", "")).rpartition(":")
        if not separator or not line_text.isdigit():
            continue
        source_path = generated_dir / source_text
        if not source_path.exists():
            continue
        lines = source_cache.setdefault(
            source_text, source_path.read_text(encoding="utf-8").splitlines()
        )
        call_index = int(line_text) - 1
        if not 0 <= call_index < len(lines):
            continue
        function_start = 0
        for index in range(call_index, -1, -1):
            if FUNCTION_SYMBOL_RE.match(lines[index]) is not None:
                function_start = index
                break

        helper_calls = [
            {
                "source": f"{source_text}:{index + 1}",
                "expression": lines[index].strip(),
            }
            for index in range(function_start, call_index)
            if helper_call_text in lines[index]
        ]
        frame_assignments = [
            {
                "source": f"{source_text}:{index + 1}",
                "expression": lines[index].strip(),
                "register": match.group("register"),
                "offset": f"0x{abs(INDEXED_CALLBACK_FRAME_LOCAL_OFFSET):X}",
            }
            for index in range(function_start, call_index)
            for match in [INDEXED_CALLBACK_FRAME_LOCAL_ASSIGNMENT_RE.match(lines[index])]
            if match is not None
        ]
        object_push = (caller.get("pushes") or [{}])[-1]
        object_register = str(object_push.get("operand", "")).strip()
        matched_frame_assignment = next(
            (
                assignment
                for assignment in frame_assignments
                if assignment["register"] == object_register
            ),
            None,
        )
        root_accesses = [
            {
                "source": f"{source_text}:{index + 1}",
                "expression": lines[index].strip(),
            }
            for index in range(function_start, call_index)
            if INDEXED_CALLBACK_TABLE_ROOT_ACCESS_RE.search(lines[index]) is not None
        ]
        direct_frame_writes = [
            {
                "source": f"{source_text}:{index + 1}",
                "expression": lines[index].strip(),
            }
            for index in range(function_start, call_index)
            if re.search(r"MEM32\(ebp\s*\+\s*-16\)\s*=", lines[index])
        ]
        if matched_frame_assignment is None:
            continue
        status = (
            "published-seh-frame-object"
            if helper_calls and helper_evidence is not None
            and helper_evidence.get("status", "").startswith("verified-")
            else "frame-local-object-without-verified-helper"
        )
        source_rows.append(
            {
                "caller": caller.get("caller"),
                "caller_name": caller.get("caller_name", ""),
                "source": caller.get("source"),
                "target": caller.get("target"),
                "target_name": caller.get("target_name", ""),
                "object_push": object_push,
                "object_pointer_expression": "ebp + -16",
                "frame_assignment": matched_frame_assignment,
                "frame_helper_calls": helper_calls,
                "indexed_table_root_accesses": root_accesses,
                "direct_frame_writes": direct_frame_writes,
                "status": status,
            }
        )

    verified_sites = [
        row for row in source_rows if row["status"] == "published-seh-frame-object"
    ]
    return {
        "status": (
            "verified-indexed-callback-frame-provenance"
            if helper_evidence is not None
            and helper_evidence.get("status")
            == "verified-helper-publishes-ebp-minus-0x10-to-fs"
            and source_rows
            and len(verified_sites) == len(source_rows)
            else "incomplete-indexed-callback-frame-provenance"
        ),
        "consumer_function": f"0x{INDEXED_CALLBACK_CONSUMER_FUNCTION:08X}",
        "frame_helper_function": f"0x{INDEXED_CALLBACK_FRAME_HELPER_FUNCTION:08X}",
        "frame_local_offset": f"0x{abs(INDEXED_CALLBACK_FRAME_LOCAL_OFFSET):X}",
        "helper": helper_evidence or {},
        "sites": source_rows,
        "verified_sites": len(verified_sites),
        "table_root_link_status": "not-proven-frame-object-to-indexed-table-root",
    }


def recover_dynamic_indexed_callback_object_provenance(
    generated_dir: Path,
    classified: list[dict[str, Any]],
    caller_provenance: list[dict[str, Any]],
) -> dict[str, Any]:
    """Inventory every observed object argument into the indexed consumer.

    The frame-specific helper above deliberately records only the stronger
    ``ebp - 0x10`` shape.  This companion inventory keeps the remaining
    caller shapes visible instead of dropping them: the current generated C
    also passes a register loaded from a caller stack argument and a register
    whose latest local assignment is zero.  These are source shapes only; no
    object identity, branch reachability, or callback target is inferred.
    """
    frame_provenance = recover_dynamic_indexed_callback_frame_provenance(
        generated_dir, classified, caller_provenance
    )
    if not caller_provenance:
        return {
            "status": "no-indexed-callback-object-provenance-observed",
            "consumer_function": f"0x{INDEXED_CALLBACK_CONSUMER_FUNCTION:08X}",
            "sites": [],
            "object_shape_status_counts": {},
            "table_root_link_status": "not-assessed",
        }

    indexed_function_addresses = {
        _address(row["function"])
        for row in classified
        if row.get("classification") == "dynamic-indexed-record-target"
        and _address(row.get("function", 0)) == INDEXED_CALLBACK_CONSUMER_FUNCTION
    }
    indexed_callers = [
        caller
        for caller in caller_provenance
        if _address(caller.get("target", 0)) in indexed_function_addresses
    ]
    frame_by_source = {
        str(row.get("source")): row
        for row in frame_provenance.get("sites", [])
    }
    rows: list[dict[str, Any]] = []
    for caller in caller_provenance:
        if _address(caller.get("target", 0)) not in indexed_function_addresses:
            continue
        source = str(caller.get("source", ""))
        frame_row = frame_by_source.get(source)
        if frame_row is not None:
            row = dict(frame_row)
            row["object_shape_status"] = (
                "published-seh-frame-object"
                if row.get("status") == "published-seh-frame-object"
                else "inherited-frame-local-object"
            )
            rows.append(row)
            continue

        pushes = caller.get("pushes") or []
        if not pushes:
            continue
        object_push = pushes[-1]
        operand = str(object_push.get("operand", "")).strip()
        source_assignment = object_push.get("source_assignment")
        object_expression = operand
        if isinstance(source_assignment, dict):
            object_expression = str(
                source_assignment.get("rhs", object_expression)
            ).strip()
        normalized = re.sub(r"\s+", "", object_expression).lower()
        if normalized in {"0", "0u", "0x0", "0x0u"}:
            shape_status = "zero-object-register"
        elif normalized == "mem32(ebp+0xc)":
            shape_status = "caller-stack-object-register"
        elif REGISTER_NAME_RE.fullmatch(operand) is not None and not isinstance(
            source_assignment, dict
        ):
            shape_status = "register-object-without-source-assignment"
        else:
            shape_status = "object-expression-unresolved"
        rows.append(
            {
                "caller": caller.get("caller"),
                "caller_name": caller.get("caller_name", ""),
                "source": source,
                "target": caller.get("target"),
                "target_name": caller.get("target_name", ""),
                "object_push": object_push,
                "object_pointer_expression": object_expression,
                "object_shape_status": shape_status,
            }
        )

    status_counts = dict(Counter(row["object_shape_status"] for row in rows))
    frame_helper_backed_sites = status_counts.get("published-seh-frame-object", 0)
    frame_object_sites = frame_helper_backed_sites + status_counts.get(
        "inherited-frame-local-object", 0
    )
    recognized_statuses = {
        "published-seh-frame-object",
        "inherited-frame-local-object",
        "caller-stack-object-register",
        "zero-object-register",
    }
    verified = (
        bool(rows)
        and frame_provenance.get("status")
        in {
            "verified-indexed-callback-frame-provenance",
            "incomplete-indexed-callback-frame-provenance",
        }
        and frame_provenance.get("helper", {}).get("status")
        == "verified-helper-publishes-ebp-minus-0x10-to-fs"
        and len(rows) == len(indexed_callers)
        and set(status_counts).issubset(recognized_statuses)
    )
    return {
        "status": (
            "verified-indexed-callback-object-provenance"
            if verified
            else "incomplete-indexed-callback-object-provenance"
        ),
        "consumer_function": f"0x{INDEXED_CALLBACK_CONSUMER_FUNCTION:08X}",
        "frame_helper_function": f"0x{INDEXED_CALLBACK_FRAME_HELPER_FUNCTION:08X}",
        "frame_provenance": frame_provenance,
        "sites": rows,
        "site_count": len(rows),
        "frame_mapped_sites": frame_helper_backed_sites,
        "frame_object_sites": frame_object_sites,
        "frame_helper_backed_sites": frame_helper_backed_sites,
        "object_shape_status_counts": status_counts,
        "table_root_link_status": (
            "not-proven-frame-object-to-indexed-table-root"
            if frame_object_sites
            else "not-assessed"
        ),
    }


def recover_fixed_callback_consumer_provenance(
    generated_dir: Path,
    classified: list[dict[str, Any]],
    functions: list[dict[str, Any]],
) -> list[dict[str, Any]]:
    """Inventory callers of the two unresolved fixed callback consumers.

    The fixed slots are still unresolved BSS-backed callback values.  This
    helper reuses the bounded direct-call push analysis for their consumer
    functions and adds the slot/read site that led to each consumer.  It does
    not infer the callback implementation, parameter types, or runtime path.
    """
    unresolved_fixed_rows = [
        row
        for row in classified
        if row.get("classification") == "runtime-initialized-fixed-slot"
        and row.get("fixed_slot") in {"0x00360A70", "0x00360AA8"}
    ]
    if not unresolved_fixed_rows:
        return []
    synthetic_rows = [
        {
            "function": row["function"],
            "function_name": row.get("function_name", ""),
            "classification": "dynamic-memory-target",
        }
        for row in unresolved_fixed_rows
    ]
    target_callers = recover_dynamic_callback_caller_provenance(
        generated_dir, synthetic_rows, functions, None
    )
    fixed_by_function = {
        _address(row["function"]): row for row in unresolved_fixed_rows
    }
    results: list[dict[str, Any]] = []
    for caller in target_callers:
        consumer = fixed_by_function.get(_address(caller["target"]))
        if consumer is None:
            continue
        results.append(
            {
                "fixed_slot": consumer["fixed_slot"],
                "consumer_function": consumer["function"],
                "consumer_function_name": consumer.get("function_name", ""),
                "consumer_call_site": consumer["call_site"],
                "consumer_target_expression": consumer["target_expression"],
                "caller": caller["caller"],
                "caller_name": caller["caller_name"],
                "source": caller["source"],
                "target": caller["target"],
                "target_name": caller["target_name"],
                "pushes": caller["pushes"],
                "status": "fixed-callback-consumer-push-shape-observed",
            }
        )
    return results


def recover_callback_return_use_provenance(
    generated_dir: Path,
    classified: list[dict[str, Any]],
    target_classes: set[str] | None = None,
) -> dict[str, dict[str, Any]]:
    """Record the bounded first use of unresolved callback return values.

    The scan stops at the next call, branch transfer, or return and only
    reports the generated C shape immediately following the indirect call.
    It does not infer a C type, signedness, ABI, or callback implementation.
    """
    callback_classes = target_classes or {
        "dynamic-register-or-object-target",
        "dynamic-memory-target",
        "dynamic-object-field-target",
        "dynamic-indexed-record-target",
        "prcb-guarded-dynamic-callback",
        "runtime-initialized-fixed-slot",
    }
    targets = {
        str(row["call_site"]): row
        for row in classified
        if row.get("classification") in callback_classes
    }
    if not targets:
        return {}
    results: dict[str, dict[str, Any]] = {}
    for source_path in sorted(generated_dir.rglob("recomp_*.c")):
        lines = source_path.read_text(encoding="utf-8").splitlines()
        current_name = ""
        for line_number, line in enumerate(lines, 1):
            symbol_match = FUNCTION_SYMBOL_RE.match(line)
            if symbol_match:
                current_name = symbol_match.group(1)
            site_match = re.search(
                r"RECOMP_ICALL_SAFE_AT\([^,]+,\s*[^,]+,\s*"
                r"0x(?P<site>[0-9A-Fa-f]{8})u\)",
                line,
            )
            if site_match is None:
                continue
            call_site = f"0x{int(site_match.group('site'), 16):08X}"
            row = targets.get(call_site)
            if row is None:
                continue

            status = "callback-return-not-observed-before-control-transfer"
            first_effect: dict[str, Any] | None = None
            return_read_sources: list[str] = []
            return_compare_source: str | None = None
            return_was_read = False
            for follow_line_number in range(line_number + 1, len(lines) + 1):
                follow = lines[follow_line_number - 1]
                stripped = follow.strip()
                if FUNCTION_SYMBOL_RE.match(follow):
                    break
                if not stripped or stripped == "}" or LOCAL_LABEL_RE.match(follow):
                    continue
                if (
                    DIRECT_ABI_CALL_RE.search(follow) is not None
                    or INDIRECT_CALL_RE.search(follow) is not None
                    or stripped.startswith("goto ")
                    or "return" in stripped
                ):
                    break
                assignment = REGISTER_ASSIGNMENT_RE.match(follow)
                if assignment is not None and assignment.group("register") == "eax":
                    first_effect = {
                        "kind": "return-overwritten-before-observation",
                        "source": f"{source_path.name}:{follow_line_number}",
                        "expression": stripped,
                    }
                    status = "callback-return-overwritten-before-observation"
                    break
                if (
                    "(eax)" in follow
                    or "LO8(eax)" in follow
                    or re.search(r"PUSH32\(esp,\s*eax\)", follow) is not None
                    or re.search(r"=\s*eax;", follow) is not None
                ):
                    return_was_read = True
                    return_read_sources.append(
                        f"{source_path.name}:{follow_line_number}"
                    )
                    if first_effect is None:
                        first_effect = {
                            "kind": "return-value-read",
                            "source": f"{source_path.name}:{follow_line_number}",
                            "expression": stripped,
                        }
                if return_was_read and re.match(r"^\s*if\s*\((?:CMP_|TEST_)", follow):
                    return_compare_source = (
                        f"{source_path.name}:{follow_line_number}"
                    )
                    status = "callback-return-compared-after-call"
                    break
                if return_was_read and len(return_read_sources) >= 6:
                    status = "callback-return-read-after-call"
                    break
            if status == "callback-return-not-observed-before-control-transfer" and first_effect:
                status = "callback-return-read-after-call"
            results[call_site] = {
                "call_site": call_site,
                "function": row["function"],
                "function_name": row.get("function_name", current_name),
                "classification": row["classification"],
                "target_expression": row.get("target_expression", ""),
                "status": status,
                "first_effect": first_effect,
                "return_read_sources": return_read_sources,
                "return_compare_source": return_compare_source,
            }
    return results


def recover_kernel_return_use_provenance(
    generated_dir: Path,
    classified: list[dict[str, Any]],
) -> dict[str, dict[str, Any]]:
    """Record the bounded first use of kernel-thunk return values."""
    return recover_callback_return_use_provenance(
        generated_dir, classified, {"kernel-thunk-slot"}
    )


def recover_dynamic_callback_container_field_provenance(
    generated_dir: Path,
    classified: list[dict[str, Any]],
    caller_provenance: list[dict[str, Any]],
) -> list[dict[str, Any]]:
    """Recover bounded caller-local writes for an indexed callback container.

    The indexed callback currently loads its table and index from object fields
    at ``+8`` and ``+0xC``.  This helper first records those load shapes in the
    callback body, then maps them onto a caller-provided pointer only when the
    last pushed object argument has a simple ``register + constant`` shape.
    It reports writes observed before the direct call; it does not prove CFG
    dominance, object identity, field types, or an indirect callback target.
    """
    indexed_rows = [
        row
        for row in classified
        if row.get("classification") == "dynamic-indexed-record-target"
    ]
    if not indexed_rows or not caller_provenance:
        return []

    target_rows: dict[int, list[dict[str, Any]]] = {}
    for row in indexed_rows:
        target_rows.setdefault(_address(row["function"]), []).append(row)

    target_names = {
        address: str(rows[0].get("function_name", ""))
        for address, rows in target_rows.items()
    }
    target_address_by_name = {
        name: address for address, name in target_names.items() if name
    }
    target_layouts: dict[int, dict[int, list[dict[str, Any]]]] = {}
    for source_path in sorted(generated_dir.rglob("recomp_*.c")):
        lines = source_path.read_text(encoding="utf-8").splitlines()
        current_name = ""
        current_address: int | None = None
        function_lines: list[tuple[int, str]] = []

        def flush_function() -> None:
            if current_address not in target_rows or not function_lines:
                return
            object_registers: set[str] = set()
            field_loads: dict[int, list[dict[str, Any]]] = {}
            target_sites = {
                _address(row["call_site"]): row
                for row in target_rows[current_address]
            }
            for local_index, (line_number, body_line) in enumerate(function_lines):
                object_load = DYNAMIC_CALLBACK_OBJECT_LOAD_RE.match(body_line)
                if object_load is not None:
                    object_registers.add(object_load.group("register"))
                field_load = DYNAMIC_CALLBACK_FIELD_LOAD_RE.match(body_line)
                if field_load is None or field_load.group("object") not in object_registers:
                    continue
                offset = int(field_load.group("offset"), 0)
                if offset not in {0x8, 0xC}:
                    continue
                field_loads.setdefault(offset, []).append(
                    {
                        "source": f"{source_path.name}:{line_number}",
                        "expression": body_line.strip(),
                        "object_register": field_load.group("object"),
                        "loaded_register": field_load.group("register"),
                        "local_index": local_index,
                    }
                )
            for call_site in target_sites:
                matching_lines = [
                    index
                    for index, (_, body_line) in enumerate(function_lines)
                    if f"0x{call_site:08X}u" in body_line
                    and INDIRECT_CALL_RE.search(body_line) is not None
                ]
                if not matching_lines:
                    continue
                call_index = matching_lines[-1]
                target_layouts[current_address] = {
                    offset: [
                        {
                            key: value
                            for key, value in item.items()
                            if key != "local_index"
                        }
                        for item in loads
                        if item["local_index"] <= call_index
                    ]
                    for offset, loads in field_loads.items()
                    if any(item["local_index"] <= call_index for item in loads)
                }
                if target_layouts[current_address]:
                    return

        for line_number, line in enumerate(lines, 1):
            symbol_match = FUNCTION_SYMBOL_RE.match(line)
            if symbol_match:
                flush_function()
                current_name = symbol_match.group(1)
                current_address = target_address_by_name.get(current_name)
                function_lines = [(line_number, line)]
                continue
            if current_name:
                function_lines.append((line_number, line))
        flush_function()

    pointer_expression_re = re.compile(
        r"^(?P<base>eax|ebx|ecx|edx|esi|edi|ebp)"
        r"(?:\s*\+\s*(?P<offset>-?0x[0-9A-Fa-f]+|-?[0-9]+))?$"
    )
    results: list[dict[str, Any]] = []
    source_cache: dict[str, list[str]] = {}
    for caller in caller_provenance:
        target_address = _address(caller["target"])
        layout = target_layouts.get(target_address)
        if not layout:
            continue
        source_text, line_text = str(caller["source"]).split(":", 1)
        call_line_number = int(line_text)
        if source_text not in source_cache:
            source_cache[source_text] = (
                generated_dir / source_text
            ).read_text(encoding="utf-8").splitlines()
        lines = source_cache[source_text]
        call_index = call_line_number - 1
        function_start = 0
        for index in range(call_index, -1, -1):
            if FUNCTION_SYMBOL_RE.match(lines[index]) is not None:
                function_start = index
                break

        pushes = caller.get("pushes", [])
        if not pushes:
            continue
        object_push = pushes[-1]
        object_expression = str(object_push.get("operand", "")).strip()
        source_assignment = object_push.get("source_assignment")
        if isinstance(source_assignment, dict):
            object_expression = str(source_assignment.get("rhs", object_expression)).strip()
        pointer_match = pointer_expression_re.fullmatch(object_expression)
        field_evidence: list[dict[str, Any]] = []
        if pointer_match is None:
            for field_offset in sorted(layout):
                field_evidence.append(
                    {
                        "field_offset": f"0x{field_offset:X}",
                        "target_loads": layout[field_offset],
                        "writes": [],
                        "status": "object-pointer-expression-unresolved",
                    }
                )
            results.append(
                {
                    "caller": caller["caller"],
                    "caller_name": caller["caller_name"],
                    "source": caller["source"],
                    "target": caller["target"],
                    "target_name": target_names.get(target_address, caller.get("target_name", "")),
                    "object_push": object_push,
                    "object_pointer_expression": object_expression,
                    "container_fields": field_evidence,
                    "status": "object-pointer-expression-unresolved",
                }
            )
            continue

        pointer_base = pointer_match.group("base")
        pointer_delta = (
            int(pointer_match.group("offset"), 0)
            if pointer_match.group("offset") is not None
            else 0
        )
        for field_offset in sorted(layout):
            expected_offset = pointer_delta + field_offset
            writes: list[dict[str, Any]] = []
            for index in range(function_start, call_index):
                write_match = LOCAL_MEMORY_WRITE_RE.match(lines[index])
                if write_match is None:
                    continue
                if write_match.group("base") != pointer_base:
                    continue
                if int(write_match.group("offset"), 0) != expected_offset:
                    continue
                rhs = write_match.group("rhs").strip()
                writes.append(
                    {
                        "source": f"{source_text}:{index + 1}",
                        "expression": lines[index].strip(),
                        "rhs": rhs,
                        "value_kind": _dynamic_writer_value_kind(rhs),
                    }
                )
            field_evidence.append(
                {
                    "field_offset": f"0x{field_offset:X}",
                    "address_expression": (
                        f"{pointer_base} + {expected_offset}"
                    ),
                    "target_loads": layout[field_offset],
                    "writes": writes,
                    "status": (
                        "caller-local-field-write-shape-observed"
                        if writes
                        else "no-proven-caller-local-field-write"
                    ),
                }
            )
        results.append(
            {
                "caller": caller["caller"],
                "caller_name": caller["caller_name"],
                "source": caller["source"],
                "target": caller["target"],
                "target_name": target_names.get(target_address, caller.get("target_name", "")),
                "object_push": object_push,
                "object_pointer_expression": object_expression,
                "container_fields": field_evidence,
                "status": (
                    "caller-local-field-write-shape-observed"
                    if any(field["writes"] for field in field_evidence)
                    else "no-proven-caller-local-field-write"
                ),
            }
        )
    return results


def recover_dynamic_callback_object_field_provenance(
    generated_dir: Path,
    classified: list[dict[str, Any]],
    caller_provenance: list[dict[str, Any]],
) -> list[dict[str, Any]]:
    """Recover writes to fields used by dynamic object-field callbacks.

    For a direct caller, the last pushed register is recorded as the possible
    first cdecl argument, matching the observed generated call shape.  Only
    exact ``MEM32(register + constant) = rhs`` writes before that call are
    reported; this does not prove argument identity, path dominance, or the
    callback target stored in the field.
    """
    target_fields: dict[int, set[int]] = {}
    target_names: dict[int, str] = {}
    for row in classified:
        if row.get("classification") != "dynamic-object-field-target":
            continue
        target_match = DYNAMIC_CALLBACK_OBJECT_TARGET_RE.fullmatch(
            str(
                row.get(
                    "target_source_expression",
                    row.get("target_expression", ""),
                )
            )
        )
        if target_match is None:
            continue
        target_address = _address(row["function"])
        target_fields.setdefault(target_address, set()).add(
            int(target_match.group("offset"), 0)
        )
        target_names[target_address] = str(row.get("function_name", ""))
    if not target_fields or not caller_provenance:
        return []

    results: list[dict[str, Any]] = []
    source_cache: dict[str, list[str]] = {}
    for caller in caller_provenance:
        target_address = _address(caller["target"])
        field_offsets = target_fields.get(target_address)
        if not field_offsets:
            continue
        source_text, line_text = str(caller["source"]).split(":", 1)
        call_index = int(line_text) - 1
        if source_text not in source_cache:
            source_cache[source_text] = (
                generated_dir / source_text
            ).read_text(encoding="utf-8").splitlines()
        lines = source_cache[source_text]
        function_start = 0
        for index in range(call_index, -1, -1):
            if FUNCTION_SYMBOL_RE.match(lines[index]) is not None:
                function_start = index
                break

        pushes = caller.get("pushes", [])
        if not pushes:
            continue
        object_push = pushes[-1]
        object_register = str(object_push.get("operand", "")).strip()
        if REGISTER_NAME_RE.fullmatch(object_register) is None:
            object_register = ""

        fields: list[dict[str, Any]] = []
        for field_offset in sorted(field_offsets):
            writes: list[dict[str, Any]] = []
            if object_register:
                for index in range(function_start, call_index):
                    write_match = LOCAL_MEMORY_WRITE_RE.match(lines[index])
                    if write_match is None:
                        continue
                    if write_match.group("base") != object_register:
                        continue
                    if int(write_match.group("offset"), 0) != field_offset:
                        continue
                    rhs = write_match.group("rhs").strip()
                    writes.append(
                        {
                            "source": f"{source_text}:{index + 1}",
                            "expression": lines[index].strip(),
                            "rhs": rhs,
                            "value_kind": _dynamic_writer_value_kind(rhs),
                        }
                    )
            fields.append(
                {
                    "field_offset": f"0x{field_offset:X}",
                    "address_expression": (
                        f"{object_register} + 0x{field_offset:X}"
                        if object_register
                        else None
                    ),
                    "writes": writes,
                    "status": (
                        "caller-object-field-write-shape-observed"
                        if writes
                        else (
                            "no-proven-caller-object-field-write"
                            if object_register
                            else "last-pushed-object-register-unresolved"
                        )
                    ),
                }
            )
        results.append(
            {
                "caller": caller["caller"],
                "caller_name": caller["caller_name"],
                "source": caller["source"],
                "target": caller["target"],
                "target_name": target_names.get(target_address, caller.get("target_name", "")),
                "object_push": object_push,
                "object_register": object_register or None,
                "fields": fields,
                "status": (
                    "caller-object-field-write-shape-observed"
                    if any(field["writes"] for field in fields)
                    else "caller-object-field-shape-observed"
                ),
            }
        )
    return results


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


def recover_symbolic_link_host_bridge_evidence(
    kernel_source: Path,
) -> dict[str, Any] | None:
    """Recover narrow path/handle validation in the host bridge.

    This records source-level evidence for the observed ``\\??\\D:`` link
    only. It does not establish generic Object Manager behavior or runtime
    execution of the bridge.
    """
    source_path = kernel_source.parent / "kernel_bridge.c"
    if not source_path.exists():
        return None
    text = source_path.read_text(encoding="utf-8")
    open_marker = "static void bridge_NtOpenSymbolicLinkObject(void)"
    query_marker = "static void bridge_NtQuerySymbolicLinkObject(void)"
    open_start = text.find(open_marker)
    query_start = text.find(query_marker)
    if open_start < 0 or query_start < 0 or query_start <= open_start:
        return None
    next_function = text.find("\nstatic void ", query_start + len(query_marker))
    query_end = len(text) if next_function < 0 else next_function
    open_text = text[open_start:query_start]
    query_text = text[query_start:query_end]
    path_match = SYMBOLIC_LINK_BRIDGE_PATH_RE.search(open_text)
    token_match = re.search(
        r"#define\s+BRIDGE_SYMBOLIC_LINK_HANDLE\s+"
        r"(?P<token>0x[0-9A-Fa-f]+)u?",
        text,
    )
    target_match = SYMBOLIC_LINK_HOST_TARGET_RE.search(query_text)
    if (
        path_match is None
        or token_match is None
        or target_match is None
        or "bridge_get_xbox_path" not in open_text
        or "handle != BRIDGE_SYMBOLIC_LINK_HANDLE" not in query_text
        or "BRIDGE_MEM32(handle_va) = BRIDGE_SYMBOLIC_LINK_HANDLE" not in open_text
    ):
        return None
    target_text = target_match.group("target").replace("\\\\", "\\")
    return {
        "status": "bridge-validates-observed-path-and-handle",
        "observed_guest_path": path_match.group("path").replace("\\\\", "\\"),
        "synthetic_handle": token_match.group("token").upper(),
        "target_text": target_text,
        "open_source": (
            f"{source_path.relative_to(kernel_source.parent).as_posix()}:"
            f"{text.count(chr(10), 0, open_start) + 1}"
        ),
        "query_source": (
            f"{source_path.relative_to(kernel_source.parent).as_posix()}:"
            f"{text.count(chr(10), 0, query_start) + 1}"
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
    all_prcb_cache_writes = recover_prcb_cache_writes(
        generated_dir, functions, None
    )
    kernel_resolver_cases = (
        None
        if kernel_source is None
        else recover_kernel_resolver_cases(kernel_source)
    )
    prcb_host_layout_evidence = (
        None
        if kernel_source is None
        else recover_prcb_host_layout_evidence(kernel_source.parent)
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
    dynamic_indexed_writer_root_link_provenance = (
        recover_dynamic_indexed_writer_root_link_provenance(
            generated_dir,
            all_dynamic_target_writers.get("dynamic-indexed-record-target", []),
        )
    )
    dynamic_object_field_writer_context = (
        recover_dynamic_object_field_writer_context(
            generated_dir,
            all_dynamic_target_writers.get("dynamic-object-field-target", []),
        )
    )
    dynamic_object_field_writer_overlap_provenance = (
        recover_dynamic_object_field_writer_overlap_provenance(
            all_dynamic_target_writers.get("dynamic-object-field-target", []),
            functions,
        )
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
    indexed_callback_table_provenance = recover_indexed_callback_table_provenance(
        generated_dir, functions, directly_reachable
    )
    all_indexed_callback_table_provenance = recover_indexed_callback_table_provenance(
        generated_dir, functions, None
    )
    indexed_callback_table_initialization = (
        recover_indexed_callback_table_initialization_layout(generated_dir)
    )
    indexed_callback_root_handoff_provenance = (
        recover_indexed_callback_root_handoff_provenance(generated_dir)
    )
    xbe = load_xbe_layout(xbe_path)
    indirect_tail_switch_provenance = recover_indirect_tail_switch_provenance(
        generated_dir, functions, directly_reachable, xbe
    )
    classified = classify_sites(
        sites,
        xbe,
        {_address(function["start"]) for function in functions},
        runtime_initializers,
        kernel_resolver_cases,
        dynamic_target_writers,
    )
    dynamic_indexed_callback_consumer_layout = (
        recover_dynamic_indexed_callback_consumer_layout(
            generated_dir, functions, classified
        )
    )
    dynamic_source_provenance = recover_dynamic_source_provenance(
        generated_dir, classified
    )
    prcb_callback_cache_provenance = recover_prcb_callback_cache_provenance(
        classified, dynamic_source_provenance
    )
    callback_return_use_provenance = recover_callback_return_use_provenance(
        generated_dir, classified
    )
    kernel_return_use_provenance = recover_kernel_return_use_provenance(
        generated_dir, classified
    )
    dynamic_callback_caller_provenance = recover_dynamic_callback_caller_provenance(
        generated_dir, classified, functions, directly_reachable
    )
    dynamic_indexed_callback_frame_provenance = (
        recover_dynamic_indexed_callback_frame_provenance(
            generated_dir, classified, dynamic_callback_caller_provenance
        )
    )
    dynamic_callback_all_caller_provenance = (
        recover_dynamic_callback_caller_provenance(
            generated_dir, classified, functions, None
        )
    )
    indexed_callback_frame_seed_provenance = (
        recover_indexed_callback_frame_seed_provenance(
            generated_dir,
            functions,
            dynamic_callback_all_caller_provenance,
            xbe,
        )
    )
    dynamic_callback_outside_direct_caller_provenance = [
        row
        for row in dynamic_callback_all_caller_provenance
        if _address(row["caller"]) not in directly_reachable
    ]
    dynamic_indexed_callback_object_provenance = (
        recover_dynamic_indexed_callback_object_provenance(
            generated_dir, classified, dynamic_callback_all_caller_provenance
        )
    )
    fixed_callback_consumer_provenance = (
        recover_fixed_callback_consumer_provenance(
            generated_dir, classified, functions
        )
    )
    fixed_callback_consumer_direct_provenance = [
        row
        for row in fixed_callback_consumer_provenance
        if _address(row["caller"]) in directly_reachable
    ]
    fixed_callback_consumer_outside_direct_provenance = [
        row
        for row in fixed_callback_consumer_provenance
        if _address(row["caller"]) not in directly_reachable
    ]
    dynamic_callback_container_field_provenance = (
        recover_dynamic_callback_container_field_provenance(
            generated_dir, classified, dynamic_callback_caller_provenance
        )
    )
    dynamic_callback_container_field_all_provenance = (
        recover_dynamic_callback_container_field_provenance(
            generated_dir, classified, dynamic_callback_all_caller_provenance
        )
    )
    dynamic_callback_container_field_outside_direct_provenance = [
        row
        for row in dynamic_callback_container_field_all_provenance
        if _address(row["caller"]) not in directly_reachable
    ]
    dynamic_callback_object_field_provenance = (
        recover_dynamic_callback_object_field_provenance(
            generated_dir, classified, dynamic_callback_caller_provenance
        )
    )
    for row in classified:
        provenance = dynamic_source_provenance.get(str(row["call_site"]))
        if provenance is not None:
            row["dynamic_source_provenance"] = provenance
        prcb_provenance = prcb_callback_cache_provenance.get(str(row["call_site"]))
        if prcb_provenance is not None:
            row["prcb_callback_cache_provenance"] = prcb_provenance
        return_provenance = callback_return_use_provenance.get(
            str(row["call_site"])
        )
        if return_provenance is not None:
            row["callback_return_use_provenance"] = return_provenance
        kernel_return_provenance = kernel_return_use_provenance.get(
            str(row["call_site"])
        )
        if kernel_return_provenance is not None:
            row["kernel_return_use_provenance"] = kernel_return_provenance
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
    used_kernel_ordinals = {
        int(row["kernel_ordinal"])
        for row in classified
        if row["classification"] == "kernel-thunk-slot"
    }
    kernel_bridge_body_provenance = (
        None
        if kernel_source is None or kernel_bridge_cases is None
        else recover_kernel_bridge_body_provenance(
            kernel_source.parent / "kernel_bridge.c",
            kernel_bridge_cases,
            used_kernel_ordinals,
        )
    )
    kernel_expected_parameter_counts = (
        {}
        if kernel_resolver_cases is None or kernel_stdcall_arg_bytes is None
        else {
            target: kernel_stdcall_arg_bytes[ordinal] // 4
            for ordinal, target in used_kernel_cases.items()
            if ordinal in kernel_stdcall_arg_bytes
        }
    )
    kernel_implementation_signature_evidence = (
        None
        if kernel_source is None
        else recover_kernel_implementation_signature_evidence(
            kernel_source.parent, kernel_expected_parameter_counts
        )
    )
    kernel_implementation_evidence = (
        None
        if kernel_source is None
        else recover_kernel_implementation_evidence(
            kernel_source, used_kernel_cases
        )
    )
    kernel_return_contract_evidence = (
        None
        if kernel_source is None
        else recover_kernel_return_contract_evidence(
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
        if kernel_implementation_signature_evidence is not None and resolver_target:
            signature = kernel_implementation_signature_evidence.get(resolver_target)
            if signature is not None:
                row["host_implementation_signature"] = signature
                row["host_implementation_signature_status"] = signature[
                    "status"
                ]
        if kernel_return_contract_evidence is not None and resolver_target:
            contract = kernel_return_contract_evidence.get(resolver_target)
            if contract is not None:
                row["host_return_contract"] = contract
        ordinal = row.get("kernel_ordinal")
        bridge_target = None
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
        if kernel_bridge_body_provenance is not None and bridge_target is not None:
            bridge_body = kernel_bridge_body_provenance.get(bridge_target)
            row["host_bridge_body_shape"] = bridge_body
            if bridge_body is None:
                row["kernel_bridge_body_status"] = "missing-source-body"
            else:
                row["kernel_bridge_body_status"] = bridge_body["status"]
                row["kernel_bridge_body_return_disposition"] = bridge_body[
                    "return_disposition"
                ]
                row["kernel_bridge_implementation_delegation_status"] = (
                    classify_kernel_bridge_implementation_delegation(
                        resolver_target,
                        bridge_body["host_call_targets"],
                        row.get("host_implementation_body_markers", []),
                    )
                )
                expected_bytes = (
                    None
                    if kernel_stdcall_arg_bytes is None
                    else kernel_stdcall_arg_bytes.get(int(ordinal))
                )
                observed = kernel_call_argument_counts.get(str(row["call_site"]))
                expected_arg_count = (
                    None if expected_bytes is None else expected_bytes // 4
                )
                stack_arg_indices = bridge_body["stack_arg_indices"]
                if bridge_body["status"] != "source-body-found":
                    row["kernel_bridge_body_stack_arg_status"] = (
                        "missing-source-body"
                    )
                elif not stack_arg_indices:
                    row["kernel_bridge_body_stack_arg_status"] = (
                        "no-stack-arg-read-and-zero-arg-call"
                        if expected_arg_count == 0
                        else (
                            "no-stack-arg-read-explicit-host-stub"
                            if bridge_body["explicit_no_op_markers"]
                            and any(
                                marker["kind"] == "explicit-stub-or-no-op"
                                for marker in row.get(
                                    "host_implementation_body_markers", []
                                )
                            )
                            else "no-stack-arg-read"
                        )
                    )
                elif expected_arg_count is None:
                    row["kernel_bridge_body_stack_arg_status"] = (
                        "missing-expected-count"
                    )
                elif max(stack_arg_indices) >= expected_arg_count:
                    row["kernel_bridge_body_stack_arg_status"] = (
                        "stack-arg-index-outside-expected-count"
                    )
                elif observed is None:
                    row["kernel_bridge_body_stack_arg_status"] = (
                        "within-expected-count-observed-call-missing"
                    )
                elif max(stack_arg_indices) >= observed["push_count"]:
                    row["kernel_bridge_body_stack_arg_status"] = (
                        "stack-arg-index-outside-observed-window"
                    )
                else:
                    row["kernel_bridge_body_stack_arg_status"] = (
                        "indices-within-expected-and-observed-window"
                    )
                row["kernel_bridge_argument_use_status"] = (
                    "missing-source-body"
                    if bridge_body["status"] != "source-body-found"
                    else classify_kernel_bridge_argument_use(
                        expected_arg_count,
                        stack_arg_indices,
                        explicit_stub=bool(
                            bridge_body["explicit_no_op_markers"]
                            and any(
                                marker["kind"] == "explicit-stub-or-no-op"
                                for marker in row.get(
                                    "host_implementation_body_markers", []
                                )
                            )
                        ),
                    )
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
    dynamic_callback_initialization = classify_dynamic_callback_initialization(
        classified,
        prcb_host_layout_evidence,
        indexed_callback_frame_seed_provenance,
        dynamic_indexed_writer_root_link_provenance,
        dynamic_target_outside_direct_writers,
    )
    dynamic_callback_initialization_by_site = {
        item["call_site"]: item
        for item in dynamic_callback_initialization["sites"]
    }
    for row in classified:
        initialization = dynamic_callback_initialization_by_site.get(
            row["call_site"]
        )
        if initialization is not None:
            row["dynamic_initialization_status"] = initialization["status"]
            row["dynamic_initialization_evidence"] = initialization["evidence"]
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
    bridge_body_sites = [
        row
        for row in kernel_slots
        if row.get("kernel_bridge_body_status") == "source-body-found"
    ]
    bridge_body_missing_sites = [
        row
        for row in kernel_slots
        if row.get("kernel_bridge_body_status") != "source-body-found"
    ]
    bridge_body_stack_index_sites = [
        row
        for row in kernel_slots
        if row.get("kernel_bridge_body_stack_arg_status")
        == "indices-within-expected-and-observed-window"
    ]
    bridge_body_stack_index_outside_sites = [
        row
        for row in kernel_slots
        if row.get("kernel_bridge_body_stack_arg_status")
        in {
            "stack-arg-index-outside-expected-count",
            "stack-arg-index-outside-observed-window",
        }
    ]
    bridge_body_stack_index_gap_sites = [
        row
        for row in kernel_slots
        if row.get("kernel_bridge_body_stack_arg_status")
        not in {
            "indices-within-expected-and-observed-window",
            "no-stack-arg-read-and-zero-arg-call",
            "no-stack-arg-read-explicit-host-stub",
        }
    ]
    bridge_body_explicit_no_arg_stub_sites = [
        row
        for row in kernel_slots
        if row.get("kernel_bridge_body_stack_arg_status")
        == "no-stack-arg-read-explicit-host-stub"
    ]
    bridge_body_return_disposition_missing_sites = [
        row
        for row in kernel_slots
        if row.get("kernel_bridge_body_return_disposition")
        == "missing-g-eax-and-no-terminal-call"
    ]
    bridge_delegation_target_statuses: dict[str, set[str]] = {}
    for row in kernel_slots:
        bridge_target = row.get("host_bridge_target")
        delegation_status = row.get("kernel_bridge_implementation_delegation_status")
        if bridge_target is None or delegation_status is None:
            continue
        bridge_delegation_target_statuses.setdefault(bridge_target, set()).add(
            delegation_status
        )
    bridge_delegation_inconsistent_targets = [
        target
        for target, statuses in bridge_delegation_target_statuses.items()
        if len(statuses) != 1
    ]
    bridge_delegation_target_counts = Counter(
        next(iter(statuses))
        for statuses in bridge_delegation_target_statuses.values()
        if len(statuses) == 1
    )
    bridge_argument_use_target_statuses: dict[str, set[str]] = {}
    for row in kernel_slots:
        bridge_target = row.get("host_bridge_target")
        argument_use_status = row.get("kernel_bridge_argument_use_status")
        if bridge_target is None or argument_use_status is None:
            continue
        bridge_argument_use_target_statuses.setdefault(bridge_target, set()).add(
            argument_use_status
        )
    bridge_argument_use_inconsistent_targets = [
        target
        for target, statuses in bridge_argument_use_target_statuses.items()
        if len(statuses) != 1
    ]
    bridge_argument_use_target_counts = Counter(
        next(iter(statuses))
        for statuses in bridge_argument_use_target_statuses.values()
        if len(statuses) == 1
    )
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
    kernel_implementation_signature_match_rows = [
        row
        for row in kernel_slots
        if row.get("host_implementation_signature_status")
        == "parameter-count-match"
    ]
    kernel_implementation_signature_mismatch_rows = [
        row
        for row in kernel_slots
        if row.get("host_implementation_signature_status")
        == "parameter-count-mismatch"
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
    symbolic_link_host_bridge_evidence = (
        None
        if kernel_source is None
        else recover_symbolic_link_host_bridge_evidence(kernel_source)
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
    dynamic_provenance_rows = [
        row
        for row in dynamic_stack_rows
        if "dynamic_source_provenance" in row
    ]
    dynamic_provenance_complete_rows = [
        row
        for row in dynamic_provenance_rows
        if row["dynamic_source_provenance"]["status"]
        == "local-linear-source-assignments-observed"
    ]
    aliased_sites = [row for row in classified if row.get("classification_source") == "linear-register-alias"]
    verified_indirect_tail_statuses = {
        "static-table-entries-match-switch-targets",
        "static-table-values-match-local-switch-branches",
    }
    indirect_tail_switch_sites = [
        row
        for row in indirect_tail_switch_provenance
        if "switch_table_expression" in row
    ]
    summary = {
        "entry_point": f"0x{entry:08X}",
        "directly_reachable_functions": len(directly_reachable),
        "direct_external_call_targets": len(external_targets),
        "indirect_call_sites": len(classified),
        "functions_with_indirect_calls": len({row["function"] for row in classified}),
        "indirect_tail_sites": len(indirect_tail_switch_provenance),
        "indirect_tail_switch_sites": len(indirect_tail_switch_sites),
        "indirect_tail_switch_verified_sites": sum(
            row["status"] in verified_indirect_tail_statuses
            for row in indirect_tail_switch_sites
        ),
        "indirect_tail_switch_unresolved_sites": sum(
            row["status"] not in verified_indirect_tail_statuses
            for row in indirect_tail_switch_sites
        ),
        "indirect_tail_switch_mismatched_sites": sum(
            row["status"] == "switch-target-set-mismatch"
            for row in indirect_tail_switch_sites
        ),
        "indirect_tail_switch_status": (
            "no-indirect-tail-sites"
            if not indirect_tail_switch_provenance
            else (
                "verified-static-tail-switch-table-provenance"
                if len(indirect_tail_switch_sites)
                == len(indirect_tail_switch_provenance)
                and all(
                    row["status"] in verified_indirect_tail_statuses
                    for row in indirect_tail_switch_provenance
                )
                else "incomplete-static-tail-switch-table-provenance"
            )
        ),
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
        "kernel_bridge_body_status": (
            "not-supplied"
            if kernel_bridge_body_provenance is None
            else (
                "verified-source-body-and-stack-index-coverage"
                if len(bridge_body_sites) == len(kernel_slots)
                and not bridge_body_stack_index_gap_sites
                and not bridge_body_explicit_no_arg_stub_sites
                else (
                    "verified-source-body-stack-index-coverage-with-explicit-stub-exception"
                    if len(bridge_body_sites) == len(kernel_slots)
                    and not bridge_body_stack_index_gap_sites
                    else (
                        "verified-source-body-with-stack-index-gap"
                        if len(bridge_body_sites) == len(kernel_slots)
                        else "source-body-or-stack-index-gap"
                    )
                )
            )
        ),
        "kernel_bridge_body_targets": (
            None
            if kernel_bridge_body_provenance is None
            else len(kernel_bridge_body_provenance)
        ),
        "kernel_bridge_body_defined_targets": (
            None
            if kernel_bridge_body_provenance is None
            else sum(
                item["status"] == "source-body-found"
                for item in kernel_bridge_body_provenance.values()
            )
        ),
        "kernel_bridge_body_sites": (
            None
            if kernel_bridge_body_provenance is None
            else len(bridge_body_sites)
        ),
        "kernel_bridge_body_missing_sites": (
            None
            if kernel_bridge_body_provenance is None
            else len(bridge_body_missing_sites)
        ),
        "kernel_bridge_body_stack_index_sites": (
            None
            if kernel_bridge_body_provenance is None
            else len(bridge_body_stack_index_sites)
        ),
        "kernel_bridge_body_stack_index_outside_sites": (
            None
            if kernel_bridge_body_provenance is None
            else len(bridge_body_stack_index_outside_sites)
        ),
        "kernel_bridge_body_stack_index_gap_sites": (
            None
            if kernel_bridge_body_provenance is None
            else len(bridge_body_stack_index_gap_sites)
        ),
        "kernel_bridge_body_explicit_no_arg_stub_sites": (
            None
            if kernel_bridge_body_provenance is None
            else len(bridge_body_explicit_no_arg_stub_sites)
        ),
        "kernel_bridge_body_return_disposition_status": (
            "not-supplied"
            if kernel_bridge_body_provenance is None
            else (
                "verified-source-return-disposition-coverage"
                if len(bridge_body_sites) == len(kernel_slots)
                and not bridge_body_return_disposition_missing_sites
                else "missing-source-return-disposition"
            )
        ),
        "kernel_bridge_body_explicit_g_eax_return_sites": (
            None
            if kernel_bridge_body_provenance is None
            else sum(
                item["return_disposition"] == "explicit-g-eax-assignment"
                for item in kernel_bridge_body_provenance.values()
            )
        ),
        "kernel_bridge_body_terminal_host_return_sites": (
            None
            if kernel_bridge_body_provenance is None
            else sum(
                item["return_disposition"] == "terminal-host-call-no-g-eax"
                for item in kernel_bridge_body_provenance.values()
            )
        ),
        "kernel_bridge_body_return_disposition_missing_sites": (
            None
            if kernel_bridge_body_provenance is None
            else len(bridge_body_return_disposition_missing_sites)
        ),
        "kernel_bridge_delegation_status": (
            "not-supplied"
            if kernel_bridge_body_provenance is None
            else (
                "verified-source-bridge-delegation-classification"
                if len(bridge_delegation_inconsistent_targets) == 0
                and len(bridge_delegation_target_statuses)
                == len(kernel_bridge_body_provenance)
                else "incomplete-or-inconsistent-bridge-delegation-classification"
            )
        ),
        "kernel_bridge_delegation_targets": (
            None
            if kernel_bridge_body_provenance is None
            else len(bridge_delegation_target_statuses)
        ),
        "kernel_bridge_direct_delegation_targets": (
            None
            if kernel_bridge_body_provenance is None
            else bridge_delegation_target_counts[
                "direct-implementation-delegation"
            ]
        ),
        "kernel_bridge_local_adaptation_targets": (
            None
            if kernel_bridge_body_provenance is None
            else bridge_delegation_target_counts["bridge-local-adaptation"]
        ),
        "kernel_bridge_local_no_direct_host_call_targets": (
            None
            if kernel_bridge_body_provenance is None
            else bridge_delegation_target_counts[
                "bridge-local-no-direct-host-call"
            ]
        ),
        "kernel_bridge_adaptation_over_host_stub_targets": (
            None
            if kernel_bridge_body_provenance is None
            else bridge_delegation_target_counts[
                "bridge-local-adaptation-over-host-stub"
            ]
        ),
        "kernel_bridge_argument_use_status": (
            "not-supplied"
            if kernel_bridge_body_provenance is None
            else (
                "verified-source-argument-read-classification"
                if len(bridge_argument_use_inconsistent_targets) == 0
                and len(bridge_argument_use_target_statuses)
                == len(kernel_bridge_body_provenance)
                else "incomplete-or-inconsistent-argument-read-classification"
            )
        ),
        "kernel_bridge_argument_use_targets": (
            None
            if kernel_bridge_body_provenance is None
            else len(bridge_argument_use_target_statuses)
        ),
        "kernel_bridge_all_expected_arguments_read_targets": (
            None
            if kernel_bridge_body_provenance is None
            else bridge_argument_use_target_counts[
                "all-expected-arguments-read"
            ]
        ),
        "kernel_bridge_partial_expected_arguments_read_targets": (
            None
            if kernel_bridge_body_provenance is None
            else bridge_argument_use_target_counts[
                "partial-expected-arguments-read"
            ]
        ),
        "kernel_bridge_zero_argument_targets": (
            None
            if kernel_bridge_body_provenance is None
            else bridge_argument_use_target_counts["zero-argument-bridge"]
        ),
        "kernel_bridge_explicit_stub_without_argument_read_targets": (
            None
            if kernel_bridge_body_provenance is None
            else bridge_argument_use_target_counts[
                "explicit-stub-without-argument-read"
            ]
        ),
        "kernel_bridge_argument_use_outside_expected_targets": (
            None
            if kernel_bridge_body_provenance is None
            else bridge_argument_use_target_counts[
                "stack-arg-index-outside-expected-count"
            ]
        ),
        "kernel_bridge_argument_use_missing_targets": (
            None
            if kernel_bridge_body_provenance is None
            else bridge_argument_use_target_counts["missing-source-body"]
            + bridge_argument_use_target_counts["missing-expected-count"]
        ),
        "kernel_bridge_body_g_eax_assignment_targets": (
            None
            if kernel_bridge_body_provenance is None
            else sum(
                bool(item["g_eax_assignment_sites"])
                for item in kernel_bridge_body_provenance.values()
            )
        ),
        "kernel_bridge_body_pointer_translation_targets": (
            None
            if kernel_bridge_body_provenance is None
            else sum(
                item["pointer_translation_sites"] > 0
                for item in kernel_bridge_body_provenance.values()
            )
        ),
        "kernel_bridge_body_guest_memory_access_targets": (
            None
            if kernel_bridge_body_provenance is None
            else sum(
                item["guest_memory_access_sites"] > 0
                for item in kernel_bridge_body_provenance.values()
            )
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
        "kernel_implementation_signature_status": (
            "not-supplied"
            if kernel_implementation_signature_evidence is None
            else (
                "verified-source-signature-parameter-counts"
                if all(
                    item["status"] == "parameter-count-match"
                    for item in kernel_implementation_signature_evidence.values()
                )
                else "source-signature-parameter-count-mismatch-or-incomplete"
            )
        ),
        "kernel_implementation_signature_targets": (
            None
            if kernel_implementation_signature_evidence is None
            else len(kernel_implementation_signature_evidence)
        ),
        "kernel_implementation_signature_match_targets": (
            None
            if kernel_implementation_signature_evidence is None
            else sum(
                item["status"] == "parameter-count-match"
                for item in kernel_implementation_signature_evidence.values()
            )
        ),
        "kernel_implementation_signature_mismatch_targets": (
            None
            if kernel_implementation_signature_evidence is None
            else sum(
                item["status"] == "parameter-count-mismatch"
                for item in kernel_implementation_signature_evidence.values()
            )
        ),
        "kernel_implementation_signature_match_sites": (
            None
            if kernel_implementation_signature_evidence is None
            else len(kernel_implementation_signature_match_rows)
        ),
        "kernel_implementation_signature_mismatch_sites": (
            None
            if kernel_implementation_signature_evidence is None
            else len(kernel_implementation_signature_mismatch_rows)
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
        "kernel_return_contract_status": (
            "not-supplied"
            if kernel_return_contract_evidence is None
            else (
                "verified-source-return-shapes"
                if all(
                    item["status"] == "source-definition-found"
                    for item in kernel_return_contract_evidence.values()
                )
                else "missing-source-return-shapes"
            )
        ),
        "kernel_return_contract_targets": (
            None
            if kernel_return_contract_evidence is None
            else len(kernel_return_contract_evidence)
        ),
        "kernel_return_contract_defined_targets": (
            None
            if kernel_return_contract_evidence is None
            else sum(
                item["status"] == "source-definition-found"
                for item in kernel_return_contract_evidence.values()
            )
        ),
        "kernel_return_contract_sites": (
            None
            if kernel_return_contract_evidence is None
            else sum(
                len(item["returns"])
                for item in kernel_return_contract_evidence.values()
            )
        ),
        "kernel_return_contract_kind_counts": (
            None
            if kernel_return_contract_evidence is None
            else dict(
                Counter(
                    return_row["kind"]
                    for item in kernel_return_contract_evidence.values()
                    for return_row in item["returns"]
                )
            )
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
        "kernel_symbolic_link_host_bridge_status": (
            "not-supplied"
            if kernel_source is None
            else (
                "bridge-path-and-handle-validation-observed"
                if symbolic_link_host_bridge_evidence is not None
                else "bridge-validation-not-observed"
            )
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
        "dynamic_callback_initialization_status": dynamic_callback_initialization[
            "status"
        ],
        "dynamic_callback_initialization_sites": dynamic_callback_initialization[
            "site_count"
        ],
        "dynamic_callback_static_image_zero_guard_sites": (
            dynamic_callback_initialization["static_image_zero_guard_sites"]
        ),
        "dynamic_callback_host_prcb_zero_guard_sites": (
            dynamic_callback_initialization["host_prcb_zero_guard_sites"]
        ),
        "dynamic_callback_runtime_initialization_unproven_sites": (
            dynamic_callback_initialization["runtime_initialization_unproven_sites"]
        ),
        "dynamic_callback_initialization_unclassified_sites": (
            dynamic_callback_initialization["unclassified_sites"]
        ),
        "dynamic_callback_initialization_class_counts": (
            dynamic_callback_initialization["class_counts"]
        ),
        "dynamic_callback_initialization_semantics": (
            dynamic_callback_initialization["semantics"]
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
        "dynamic_indexed_writer_root_link_status": (
            dynamic_indexed_writer_root_link_provenance.get(
                "status", "not-assessed"
            )
        ),
        "dynamic_indexed_writer_root_link_sites": (
            dynamic_indexed_writer_root_link_provenance.get("writer_sites", 0)
        ),
        "dynamic_indexed_writer_root_link_functions": (
            dynamic_indexed_writer_root_link_provenance.get(
                "writer_functions", 0
            )
        ),
        "dynamic_indexed_writer_root_link_sites_with_root_reference": (
            dynamic_indexed_writer_root_link_provenance.get(
                "writer_sites_with_root_reference", 0
            )
        ),
        "dynamic_indexed_writer_root_link_functions_with_root_reference": (
            dynamic_indexed_writer_root_link_provenance.get(
                "writer_functions_with_root_reference", 0
            )
        ),
        "dynamic_object_field_writer_context_sites": len(
            dynamic_object_field_writer_context
        ),
        "dynamic_object_field_writer_context_status_counts": dict(
            Counter(row["status"] for row in dynamic_object_field_writer_context)
        ),
        "dynamic_object_field_writer_context_construction_shape_counts": dict(
            Counter(
                row.get("construction_shape", "not-observed")
                for row in dynamic_object_field_writer_context
            )
        ),
        "dynamic_object_field_writer_context_value_source_kinds": dict(
            Counter(
                row.get("value_source_kind", "unresolved")
                for row in dynamic_object_field_writer_context
            )
        ),
        "runtime_fixed_slot_all_writer_counts": {
            slot: len(rows)
            for slot, rows in runtime_fixed_slot_writer_inventory.items()
        },
        "dynamic_indexed_callback_table_status": (
            "verified-static-indexed-table-root-provenance"
            if all_indexed_callback_table_provenance["table_root_writes"]
            and all_indexed_callback_table_provenance["capacity_writes"]
            else "incomplete-static-indexed-table-root-provenance"
        ),
        "dynamic_indexed_callback_table_base": f"0x{INDEXED_CALLBACK_TABLE_BASE:08X}",
        "dynamic_indexed_callback_table_capacity_slot": (
            f"0x{INDEXED_CALLBACK_TABLE_CAPACITY_SLOT:08X}"
        ),
        "dynamic_indexed_callback_table_direct_root_write_sites": len(
            indexed_callback_table_provenance["table_root_writes"]
        ),
        "dynamic_indexed_callback_table_all_root_write_sites": len(
            all_indexed_callback_table_provenance["table_root_writes"]
        ),
        "dynamic_indexed_callback_table_direct_capacity_write_sites": len(
            indexed_callback_table_provenance["capacity_writes"]
        ),
        "dynamic_indexed_callback_table_all_capacity_write_sites": len(
            all_indexed_callback_table_provenance["capacity_writes"]
        ),
        "dynamic_indexed_callback_table_direct_record_write_sites": len(
            indexed_callback_table_provenance["indexed_record_writes"]
        ),
        "dynamic_indexed_callback_table_all_record_write_sites": len(
            all_indexed_callback_table_provenance["indexed_record_writes"]
        ),
        "dynamic_indexed_callback_root_handoff_status": (
            indexed_callback_root_handoff_provenance.get(
                "status", "not-assessed"
            )
        ),
        "dynamic_indexed_callback_root_handoff_function": (
            indexed_callback_root_handoff_provenance.get("function")
        ),
        "dynamic_indexed_callback_root_handoff_direct_frame_writes": len(
            indexed_callback_root_handoff_provenance.get(
                "direct_frame_writes", []
            )
        ),
        "dynamic_indexed_callback_table_initialization_status": (
            indexed_callback_table_initialization.get(
                "status", "not-assessed"
            )
        ),
        "dynamic_indexed_callback_table_initialization_capacity_records": (
            indexed_callback_table_initialization.get("capacity_records")
        ),
        "dynamic_indexed_callback_table_initialization_record_stride_bytes": (
            indexed_callback_table_initialization.get("record_stride_bytes")
        ),
        "dynamic_indexed_callback_table_initialization_record_count": (
            indexed_callback_table_initialization.get(
                "initialization_record_count"
            )
        ),
        "dynamic_indexed_callback_table_initialization_zeroed_field_status": (
            indexed_callback_table_initialization.get(
                "root_record_zeroed_field_status", "not-assessed"
            )
        ),
        "dynamic_indexed_callback_table_initialization_flagged_prefix_record_count": (
            indexed_callback_table_initialization.get(
                "flagged_prefix_record_count"
            )
        ),
        "dynamic_indexed_callback_consumer_layout_status": (
            dynamic_indexed_callback_consumer_layout.get(
                "status", "no-indexed-callback-consumer-observed"
            )
        ),
        "dynamic_indexed_callback_consumer_layout_call_sites": len(
            dynamic_indexed_callback_consumer_layout.get("call_sites", [])
        ),
        "dynamic_indexed_callback_consumer_layout_record_stride_bytes": (
            dynamic_indexed_callback_consumer_layout.get("record_stride_bytes")
        ),
        "dynamic_indexed_callback_consumer_layout_table_root_link_status": (
            dynamic_indexed_callback_consumer_layout.get(
                "table_root_link_status", "not-assessed"
            )
        ),
        "dynamic_indexed_callback_frame_provenance_status": (
            dynamic_indexed_callback_frame_provenance.get(
                "status", "not-assessed"
            )
        ),
        "dynamic_indexed_callback_frame_provenance_sites": len(
            dynamic_indexed_callback_frame_provenance.get("sites", [])
        ),
        "dynamic_indexed_callback_frame_provenance_verified_sites": (
            dynamic_indexed_callback_frame_provenance.get("verified_sites", 0)
        ),
        "dynamic_indexed_callback_frame_provenance_table_root_link_status": (
            dynamic_indexed_callback_frame_provenance.get(
                "table_root_link_status", "not-assessed"
            )
        ),
        "dynamic_indexed_callback_frame_seed_status": (
            indexed_callback_frame_seed_provenance.get(
                "status", "not-assessed"
            )
        ),
        "dynamic_indexed_callback_frame_seed_sites": (
            indexed_callback_frame_seed_provenance.get("site_count", 0)
        ),
        "dynamic_indexed_callback_frame_seed_zero_guard_sites": (
            indexed_callback_frame_seed_provenance.get("zero_guard_sites", 0)
        ),
        "dynamic_indexed_callback_frame_seed_known_target_sites": (
            indexed_callback_frame_seed_provenance.get(
                "known_function_target_sites", 0
            )
        ),
        "dynamic_indexed_callback_object_provenance_status": (
            dynamic_indexed_callback_object_provenance.get(
                "status", "not-assessed"
            )
        ),
        "dynamic_indexed_callback_object_provenance_sites": (
            dynamic_indexed_callback_object_provenance.get("site_count", 0)
        ),
        "dynamic_indexed_callback_object_provenance_frame_mapped_sites": (
            dynamic_indexed_callback_object_provenance.get(
                "frame_mapped_sites", 0
            )
        ),
        "dynamic_indexed_callback_object_provenance_frame_object_sites": (
            dynamic_indexed_callback_object_provenance.get(
                "frame_object_sites", 0
            )
        ),
        "dynamic_indexed_callback_object_provenance_frame_helper_backed_sites": (
            dynamic_indexed_callback_object_provenance.get(
                "frame_helper_backed_sites", 0
            )
        ),
        "dynamic_indexed_callback_object_provenance_shape_counts": (
            dynamic_indexed_callback_object_provenance.get(
                "object_shape_status_counts", {}
            )
        ),
        "dynamic_indexed_callback_object_provenance_table_root_link_status": (
            dynamic_indexed_callback_object_provenance.get(
                "table_root_link_status", "not-assessed"
            )
        ),
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
        "dynamic_source_provenance_status": (
            "complete-local-linear-assignment-evidence"
            if len(dynamic_provenance_complete_rows) == len(dynamic_stack_rows)
            else "incomplete-local-linear-assignment-evidence"
        ),
        "dynamic_source_provenance_sites": len(dynamic_provenance_rows),
        "dynamic_source_provenance_complete_sites": len(
            dynamic_provenance_complete_rows
        ),
        "dynamic_source_provenance_missing_sites": (
            len(dynamic_stack_rows) - len(dynamic_provenance_rows)
        ),
        "prcb_callback_cache_provenance_sites": len(
            prcb_callback_cache_provenance
        ),
        "prcb_callback_cache_provenance_complete_sites": sum(
            row["status"] == "prcb-cache-callback-field-shape-observed"
            for row in prcb_callback_cache_provenance.values()
        ),
        "prcb_callback_cache_provenance_missing_sites": (
            classifications["prcb-guarded-dynamic-callback"]
            - len(prcb_callback_cache_provenance)
        ),
        "prcb_callback_cache_provenance_status": (
            "prcb-cache-callback-field-shape-observed"
            if (
                len(prcb_callback_cache_provenance)
                == classifications["prcb-guarded-dynamic-callback"]
                and all(
                    row["status"] == "prcb-cache-callback-field-shape-observed"
                    for row in prcb_callback_cache_provenance.values()
                )
            )
            else "incomplete-prcb-cache-callback-field-shape"
        ),
        "prcb_host_layout_status": (
            "not-supplied"
            if prcb_host_layout_evidence is None
            else prcb_host_layout_evidence["status"]
        ),
        "prcb_host_layout_fake_prcb_va": (
            None
            if prcb_host_layout_evidence is None
            else prcb_host_layout_evidence["fake_prcb_va"]
        ),
        "prcb_host_layout_zero_block_bytes": (
            None
            if prcb_host_layout_evidence is None
            else prcb_host_layout_evidence["zero_block_bytes"]
        ),
        "callback_return_use_provenance_sites": len(
            callback_return_use_provenance
        ),
        "callback_return_use_compared_sites": sum(
            row["status"] == "callback-return-compared-after-call"
            for row in callback_return_use_provenance.values()
        ),
        "callback_return_use_read_sites": sum(
            row["status"] == "callback-return-read-after-call"
            for row in callback_return_use_provenance.values()
        ),
        "callback_return_use_overwritten_sites": sum(
            row["status"] == "callback-return-overwritten-before-observation"
            for row in callback_return_use_provenance.values()
        ),
        "callback_return_use_unobserved_sites": sum(
            row["status"] == "callback-return-not-observed-before-control-transfer"
            for row in callback_return_use_provenance.values()
        ),
        "callback_return_use_status": (
            "bounded-first-post-call-use-observed"
            if callback_return_use_provenance
            else "no-callback-return-use-observed"
        ),
        "kernel_return_use_provenance_sites": len(kernel_return_use_provenance),
        "kernel_return_use_compared_sites": sum(
            row["status"] == "callback-return-compared-after-call"
            for row in kernel_return_use_provenance.values()
        ),
        "kernel_return_use_read_sites": sum(
            row["status"] == "callback-return-read-after-call"
            for row in kernel_return_use_provenance.values()
        ),
        "kernel_return_use_overwritten_sites": sum(
            row["status"] == "callback-return-overwritten-before-observation"
            for row in kernel_return_use_provenance.values()
        ),
        "kernel_return_use_unobserved_sites": sum(
            row["status"] == "callback-return-not-observed-before-control-transfer"
            for row in kernel_return_use_provenance.values()
        ),
        "kernel_return_use_status": (
            "bounded-first-post-call-use-observed"
            if kernel_return_use_provenance
            else "no-kernel-return-use-observed"
        ),
        "dynamic_callback_caller_provenance_sites": len(
            dynamic_callback_caller_provenance
        ),
        "dynamic_callback_caller_provenance_status": (
            "direct-call-push-shape-observed"
            if dynamic_callback_caller_provenance
            else "no-direct-caller-push-shape-observed"
        ),
        "dynamic_callback_all_caller_provenance_sites": len(
            dynamic_callback_all_caller_provenance
        ),
        "dynamic_callback_outside_direct_caller_provenance_sites": len(
            dynamic_callback_outside_direct_caller_provenance
        ),
        "dynamic_callback_all_caller_provenance_status": (
            "all-generated-direct-call-shapes-observed"
            if dynamic_callback_all_caller_provenance
            else "no-generated-direct-call-shapes-observed"
        ),
        "dynamic_callback_all_caller_counts": dict(
            Counter(
                row["target_name"]
                for row in dynamic_callback_all_caller_provenance
            )
        ),
        "dynamic_callback_outside_direct_caller_counts": dict(
            Counter(
                row["target_name"]
                for row in dynamic_callback_outside_direct_caller_provenance
            )
        ),
        "fixed_callback_consumer_provenance_sites": len(
            fixed_callback_consumer_provenance
        ),
        "fixed_callback_consumer_direct_provenance_sites": len(
            fixed_callback_consumer_direct_provenance
        ),
        "fixed_callback_consumer_outside_direct_provenance_sites": len(
            fixed_callback_consumer_outside_direct_provenance
        ),
        "fixed_callback_consumer_provenance_status": (
            "fixed-bss-consumer-push-shape-observed"
            if fixed_callback_consumer_provenance
            else "no-fixed-bss-consumer-push-shape-observed"
        ),
        "fixed_callback_consumer_counts": dict(
            Counter(
                row["fixed_slot"] for row in fixed_callback_consumer_provenance
            )
        ),
        "fixed_callback_consumer_direct_counts": dict(
            Counter(
                row["fixed_slot"]
                for row in fixed_callback_consumer_direct_provenance
            )
        ),
        "fixed_callback_consumer_outside_direct_counts": dict(
            Counter(
                row["fixed_slot"]
                for row in fixed_callback_consumer_outside_direct_provenance
            )
        ),
        "dynamic_callback_container_field_provenance_sites": len(
            dynamic_callback_container_field_provenance
        ),
        "dynamic_callback_container_field_write_sites": sum(
            len(field["writes"])
            for row in dynamic_callback_container_field_provenance
            for field in row["container_fields"]
        ),
        "dynamic_callback_container_field_provenance_status": (
            "caller-local-field-write-shape-observed"
            if dynamic_callback_container_field_provenance
            else "no-indexed-callback-container-provenance"
        ),
        "dynamic_callback_container_field_all_provenance_sites": len(
            dynamic_callback_container_field_all_provenance
        ),
        "dynamic_callback_container_field_all_write_sites": sum(
            len(field["writes"])
            for row in dynamic_callback_container_field_all_provenance
            for field in row["container_fields"]
        ),
        "dynamic_callback_container_field_outside_direct_provenance_sites": len(
            dynamic_callback_container_field_outside_direct_provenance
        ),
        "dynamic_callback_container_field_outside_direct_write_sites": sum(
            len(field["writes"])
            for row in dynamic_callback_container_field_outside_direct_provenance
            for field in row["container_fields"]
        ),
        "dynamic_callback_object_field_provenance_sites": len(
            dynamic_callback_object_field_provenance
        ),
        "dynamic_callback_object_field_write_sites": sum(
            len(field["writes"])
            for row in dynamic_callback_object_field_provenance
            for field in row["fields"]
        ),
        "dynamic_callback_object_field_provenance_status": (
            "caller-object-field-write-shape-observed"
            if any(
                field["writes"]
                for row in dynamic_callback_object_field_provenance
                for field in row["fields"]
            )
            else (
                "caller-object-field-shape-observed"
                if dynamic_callback_object_field_provenance
                else "no-object-field-callback-provenance"
            )
        ),
        "dynamic_object_field_writer_overlap_status": (
            dynamic_object_field_writer_overlap_provenance.get(
                "status", "not-assessed"
            )
        ),
        "dynamic_object_field_writer_overlap_groups": (
            dynamic_object_field_writer_overlap_provenance.get(
                "overlap_group_count", 0
            )
        ),
        "dynamic_object_field_writer_overlap_functions": (
            dynamic_object_field_writer_overlap_provenance.get(
                "overlapping_writer_functions", 0
            )
        ),
        "dynamic_object_field_writer_metadata_functions": (
            dynamic_object_field_writer_overlap_provenance.get(
                "metadata_resolved_writer_functions", 0
            )
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
        "prcb_cache_all_writer_sites": len(all_prcb_cache_writes),
        "prcb_cache_all_writer_status": (
            "writer-observed-in-generated-functions"
            if all_prcb_cache_writes
            else "no-proven-writer-in-generated-functions"
        ),
        "prcb_cache_outside_direct_writer_sites": max(
            0, len(all_prcb_cache_writes) - len(prcb_cache_writes)
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
        "prcb_cache_all_writes": all_prcb_cache_writes,
        "dynamic_target_outside_direct_writers": dynamic_target_outside_direct_writers,
        "dynamic_indexed_writer_root_link_provenance": (
            dynamic_indexed_writer_root_link_provenance
        ),
        "dynamic_object_field_writer_context": dynamic_object_field_writer_context,
        "dynamic_object_field_writer_overlap_provenance": (
            dynamic_object_field_writer_overlap_provenance
        ),
        "dynamic_source_provenance": dynamic_source_provenance,
        "dynamic_callback_initialization": dynamic_callback_initialization,
        "prcb_callback_cache_provenance": prcb_callback_cache_provenance,
        "callback_return_use_provenance": callback_return_use_provenance,
        "kernel_return_use_provenance": kernel_return_use_provenance,
        "dynamic_callback_caller_provenance": dynamic_callback_caller_provenance,
        "dynamic_callback_all_caller_provenance": (
            dynamic_callback_all_caller_provenance
        ),
        "dynamic_callback_outside_direct_caller_provenance": (
            dynamic_callback_outside_direct_caller_provenance
        ),
        "fixed_callback_consumer_provenance": fixed_callback_consumer_provenance,
        "fixed_callback_consumer_direct_provenance": (
            fixed_callback_consumer_direct_provenance
        ),
        "fixed_callback_consumer_outside_direct_provenance": (
            fixed_callback_consumer_outside_direct_provenance
        ),
        "dynamic_callback_container_field_provenance": (
            dynamic_callback_container_field_provenance
        ),
        "dynamic_callback_container_field_all_provenance": (
            dynamic_callback_container_field_all_provenance
        ),
        "dynamic_callback_container_field_outside_direct_provenance": (
            dynamic_callback_container_field_outside_direct_provenance
        ),
        "dynamic_callback_object_field_provenance": (
            dynamic_callback_object_field_provenance
        ),
        "indirect_tail_switch_provenance": indirect_tail_switch_provenance,
        "runtime_fixed_slot_writer_inventory": runtime_fixed_slot_writer_inventory,
        "dynamic_indexed_callback_table_provenance": indexed_callback_table_provenance,
        "dynamic_indexed_callback_table_all_provenance": (
            all_indexed_callback_table_provenance
        ),
        "dynamic_indexed_callback_table_initialization": (
            indexed_callback_table_initialization
        ),
        "dynamic_indexed_callback_root_handoff_provenance": (
            indexed_callback_root_handoff_provenance
        ),
        "dynamic_indexed_callback_consumer_layout": (
            dynamic_indexed_callback_consumer_layout
        ),
        "dynamic_indexed_callback_frame_provenance": (
            dynamic_indexed_callback_frame_provenance
        ),
        "dynamic_indexed_callback_frame_seed_provenance": (
            indexed_callback_frame_seed_provenance
        ),
        "dynamic_indexed_callback_object_provenance": (
            dynamic_indexed_callback_object_provenance
        ),
        "conditional_skip_branches": skip_branch_evidence,
        "conditional_nonzero_guards": nonzero_guard_evidence,
        "kernel_implementation_evidence": kernel_implementation_evidence or {},
        "kernel_implementation_signature_evidence": (
            kernel_implementation_signature_evidence or {}
        ),
        "kernel_return_contract_evidence": kernel_return_contract_evidence or {},
        "kernel_file_information_evidence": kernel_file_information_evidence or {},
        "kernel_bridge_cases": kernel_bridge_cases or {},
        "kernel_bridge_body_provenance": kernel_bridge_body_provenance or {},
        "kernel_bridge_delegation_evidence": {
            target: {"statuses": sorted(statuses)}
            for target, statuses in bridge_delegation_target_statuses.items()
        },
        "kernel_bridge_argument_use_evidence": {
            target: {"statuses": sorted(statuses)}
            for target, statuses in bridge_argument_use_target_statuses.items()
        },
        "kernel_call_argument_counts": kernel_call_argument_counts,
        "kernel_constant_arguments": kernel_constant_arguments,
        "kernel_register_constant_arguments": kernel_register_constant_arguments,
        "kernel_symbolic_link_lifecycle_evidence": symbolic_link_lifecycle_evidence,
        "kernel_symbolic_link_guest_path_evidence": symbolic_link_guest_path_evidence,
        "kernel_symbolic_link_host_query_target": symbolic_link_host_query_target or {},
        "kernel_symbolic_link_host_bridge_evidence": (
            symbolic_link_host_bridge_evidence or {}
        ),
        "prcb_host_layout_evidence": prcb_host_layout_evidence or {},
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
