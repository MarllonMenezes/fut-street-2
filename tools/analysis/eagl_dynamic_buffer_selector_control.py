"""Classify the proven writer and consumer shapes for the dynamic EAGL selector.

The control reads the recovered ASM listing and the read-only XBE identity. It
does not choose a buffer, execute a render path, or assign meaning to the
selector. Its purpose is to prevent the observed ``0x00345BCC`` domain from
being collapsed to a guessed constant and to keep its statically observed
consumer boundary explicit.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import re
from pathlib import Path
from typing import Any


EXPECTED_XBE_SHA256 = (
    "F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2"
)
SELECTOR = "0x00345BCC"
WRITER_RE = re.compile(
    r"^\s*0x(?P<address>[0-9A-Fa-f]{8})\s+[^\s]+\s+mov\s+"
    r"dword ptr \[0x(?P<selector>[0-9A-Fa-f]{1,8})\],\s*"
    r"(?P<value>.+?)\s*$",
    re.IGNORECASE,
)
CONSUMER_RE = re.compile(
    r"^\s*0x(?P<address>[0-9A-Fa-f]{8})\s+[^\s]+\s+"
    r"(?P<mnemonic>[a-z]+)\s+(?P<operands>.+?)\s*$",
    re.IGNORECASE,
)
SELECTOR_REFERENCE_RE = re.compile(
    r"\[0x(?P<selector>[0-9A-Fa-f]{1,8})\]",
    re.IGNORECASE,
)

EXPECTED_WRITERS = {
    "0x0019E91B": {
        "form": "signed-table-word",
        "value": "ecx",
        "evidence": "0x0019E916 movsx ecx, word ptr [esi + ebx*4 + 2]",
    },
    "0x0019F20F": {
        "form": "literal-zero",
        "value": "0",
        "evidence": "literal zero store",
    },
    "0x0019FCFF": {
        "form": "object-field-plus-0xC0",
        "value": "eax",
        "evidence": "0x0019FCF1 mov eax, dword ptr [ebx + 0xc0]",
    },
    "0x001A2FCE": {
        "form": "object-field-plus-0xC0",
        "value": "eax",
        "evidence": "0x001A2FC1 mov eax, dword ptr [ebx + 0xc0]",
    },
    "0x001A30FB": {
        "form": "object-field-plus-0xC0",
        "value": "edx",
        "evidence": "0x001A30F2 mov edx, dword ptr [esi + 0xc0]",
    },
}

EXPECTED_CONSUMERS = {
    **{
        address: "scaled-selector-consumer"
        for address in (
            "0x001A3E08",
            "0x001A3ED8",
            "0x001A3F18",
            "0x001A3F58",
            "0x001A3F98",
            "0x001A3FDD",
            "0x001A401D",
            "0x001A406F",
            "0x001A4094",
            "0x001A40CE",
            "0x001A40EC",
            "0x001A413A",
            "0x001A419A",
            "0x001A41FE",
            "0x001A4254",
            "0x001A42DE",
            "0x001A4334",
            "0x001A43A8",
            "0x001A4488",
            "0x001A44E1",
            "0x001A4548",
            "0x001A45A1",
            "0x001A46BE",
            "0x001A471E",
            "0x001A4768",
            "0x001A47AE",
            "0x001A480E",
            "0x001A4858",
            "0x001A4898",
            "0x001A48F8",
            "0x001A4AA8",
            "0x001A4AF8",
        )
    },
    "0x001A3E46": "loaded-selector-consumer",
}


def _normalize_value(value: str) -> str:
    return re.sub(r"\s+", " ", value.strip().lower())


def recover_writer_rows(asm_path: Path) -> list[dict[str, Any]]:
    """Recover every absolute writer to the selector from recovered ASM."""
    rows: list[dict[str, Any]] = []
    for line_number, line in enumerate(asm_path.read_text(encoding="utf-8").splitlines(), 1):
        match = WRITER_RE.match(line)
        if match is None:
            continue
        if int(match.group("selector"), 16) != 0x00345BCC:
            continue
        address = f"0x{int(match.group('address'), 16):08X}"
        value = _normalize_value(match.group("value"))
        rows.append(
            {
                "address": address,
                "value": value,
                "source": f"{asm_path.name}:{line_number}",
            }
        )
    return rows


def classify_writers(rows: list[dict[str, Any]]) -> list[dict[str, Any]]:
    """Join writer addresses to conservative, shape-only classifications."""
    expected_addresses = set(EXPECTED_WRITERS)
    observed_addresses = {row["address"] for row in rows}
    if observed_addresses != expected_addresses:
        raise ValueError(
            "selector writer-address mismatch: "
            f"expected={sorted(expected_addresses)}, "
            f"observed={sorted(observed_addresses)}"
        )

    classified: list[dict[str, Any]] = []
    for row in rows:
        expected = EXPECTED_WRITERS[row["address"]]
        if row["value"] != expected["value"]:
            raise ValueError(
                f"selector writer value mismatch at {row['address']}: "
                f"expected={expected['value']!r}, observed={row['value']!r}"
            )
        classified.append({**row, **expected})
    return sorted(classified, key=lambda row: int(row["address"], 16))


def recover_consumer_rows(asm_path: Path) -> list[dict[str, Any]]:
    """Recover every supported absolute read of the selector from ASM."""
    rows: list[dict[str, Any]] = []
    for line_number, line in enumerate(asm_path.read_text(encoding="utf-8").splitlines(), 1):
        match = CONSUMER_RE.match(line)
        if match is None:
            continue
        selector_match = SELECTOR_REFERENCE_RE.search(match.group("operands"))
        if selector_match is None:
            continue
        if int(selector_match.group("selector"), 16) != 0x00345BCC:
            continue
        if WRITER_RE.match(line) is not None:
            continue
        destination = match.group("operands").split(",", 1)[0].strip().split()[-1]
        rows.append(
            {
                "address": f"0x{int(match.group('address'), 16):08X}",
                "destination": destination.lower(),
                "mnemonic": match.group("mnemonic").lower(),
                "source": f"{asm_path.name}:{line_number}",
            }
        )
    return rows


def classify_consumers(rows: list[dict[str, Any]]) -> list[dict[str, Any]]:
    """Join consumer addresses to conservative selector-use classifications."""
    expected_addresses = set(EXPECTED_CONSUMERS)
    observed_addresses = {row["address"] for row in rows}
    if observed_addresses != expected_addresses:
        raise ValueError(
            "selector consumer-address mismatch: "
            f"expected={sorted(expected_addresses)}, "
            f"observed={sorted(observed_addresses)}"
        )

    classified: list[dict[str, Any]] = []
    for row in rows:
        expected_form = EXPECTED_CONSUMERS[row["address"]]
        expected_mnemonic = "imul" if expected_form == "scaled-selector-consumer" else "mov"
        if row["mnemonic"] != expected_mnemonic:
            raise ValueError(
                f"selector consumer mnemonic mismatch at {row['address']}: "
                f"expected={expected_mnemonic!r}, observed={row['mnemonic']!r}"
            )
        classified.append({**row, "form": expected_form})
    return sorted(classified, key=lambda row: int(row["address"], 16))


def audit(asm_path: Path, xbe_path: Path) -> dict[str, Any]:
    """Verify the selector writer domain against the current XBE identity."""
    xbe_sha256 = hashlib.sha256(xbe_path.read_bytes()).hexdigest().upper()
    if xbe_sha256 != EXPECTED_XBE_SHA256:
        raise ValueError(
            f"XBE SHA-256 mismatch: expected={EXPECTED_XBE_SHA256}, "
            f"observed={xbe_sha256}"
        )

    writer_rows = classify_writers(recover_writer_rows(asm_path))
    writer_form_counts: dict[str, int] = {}
    for row in writer_rows:
        writer_form_counts[row["form"]] = writer_form_counts.get(row["form"], 0) + 1
    consumer_rows = classify_consumers(recover_consumer_rows(asm_path))
    consumer_form_counts: dict[str, int] = {}
    for row in consumer_rows:
        consumer_form_counts[row["form"]] = consumer_form_counts.get(row["form"], 0) + 1

    return {
        "summary": {
            "status": "verified-static-eagl-buffer-selector-writer-consumer-domain",
            "selector": SELECTOR,
            "writer_sites": len(writer_rows),
            "writer_form_counts": writer_form_counts,
            "consumer_sites": len(consumer_rows),
            "consumer_form_counts": consumer_form_counts,
            "selector_read_sites": len(consumer_rows),
            "selector_usage": "scaled-index-and-one-loaded-index",
            "selector_constant": False,
            "runtime_selected_buffer": "not-proven",
            "rendering_binding": "not-proven",
            "original_semantics": "not-claimed",
            "xbe_sha256": xbe_sha256,
        },
        "writers": writer_rows,
        "consumers": consumer_rows,
    }


def _parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--asm", type=Path, required=True)
    parser.add_argument("--xbe", type=Path, required=True)
    parser.add_argument("--format", choices=("json", "text"), default="text")
    return parser


def main() -> int:
    args = _parser().parse_args()
    result = audit(args.asm, args.xbe)
    if args.format == "json":
        print(json.dumps(result, indent=2, sort_keys=True))
    else:
        for key, value in result["summary"].items():
            print(f"{key}={value}")
        for row in result["writers"]:
            print(
                "selector_writer={address} form={form} value={value} source={source}".format(
                    **row
                )
            )
        for row in result["consumers"]:
            print(
                "selector_consumer={address} form={form} mnemonic={mnemonic} "
                "destination={destination} source={source}".format(**row)
            )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
