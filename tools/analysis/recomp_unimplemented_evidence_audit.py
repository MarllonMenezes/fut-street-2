"""Cross-check untranslated instruction evidence across recompilation outputs.

This audit is deliberately static.  It joins the ``unimplemented`` map in
XboxRecomp's ``summary.json`` with ``RECOMP_UNIMPL`` markers in generated C
and the matching instruction line in the recovered ASM listing.  It does not
execute the XBE, generated code, or any host runtime.
"""

from __future__ import annotations

import argparse
import json
import re
from pathlib import Path
from typing import Any


UNIMPLEMENTED_RE = re.compile(
    r"RECOMP_UNIMPL\(\s*\"(?P<text>[^\"]*)\"\s*,\s*"
    r"0x(?P<address>[0-9A-Fa-f]{8})u\s*\)"
)
ASM_ADDRESS_RE = re.compile(r"^\s*0x(?P<address>[0-9A-Fa-f]{8})(?:\s+|$)")


def _address(value: Any) -> int:
    if isinstance(value, int):
        return value
    return int(str(value), 0)


def _normalize_instruction(text: str) -> str:
    """Normalize whitespace/case without changing instruction semantics."""
    return re.sub(r"\s+", " ", text.strip().lower())


def recover_generated_sites(generated_dir: Path) -> dict[int, dict[str, Any]]:
    """Recover unique untranslated sites and their generated-C locations."""
    sites: dict[int, dict[str, Any]] = {}
    for source_path in sorted(generated_dir.rglob("recomp_*.c")):
        for line_number, line in enumerate(
            source_path.read_text(encoding="utf-8").splitlines(), 1
        ):
            for match in UNIMPLEMENTED_RE.finditer(line):
                address = int(match.group("address"), 16)
                text = match.group("text")
                row = sites.setdefault(
                    address,
                    {
                        "address": f"0x{address:08X}",
                        "generated_text": text,
                        "generated_sources": [],
                    },
                )
                if row["generated_text"] != text:
                    raise ValueError(
                        f"generated text mismatch at {address:#010x}: "
                        f"{row['generated_text']!r} != {text!r}"
                    )
                source = f"{source_path.name}:{line_number}"
                if source not in row["generated_sources"]:
                    row["generated_sources"].append(source)
    return sites


def recover_summary_sites(summary_path: Path) -> dict[int, str]:
    """Recover the mnemonic-to-address map emitted by XboxRecomp."""
    summary = json.loads(summary_path.read_text(encoding="utf-8"))
    raw_sites = summary.get("unimplemented")
    if not isinstance(raw_sites, dict):
        raise ValueError("summary.json has no object-valued 'unimplemented' map")

    sites: dict[int, str] = {}
    for mnemonic, addresses in raw_sites.items():
        if not isinstance(mnemonic, str) or not isinstance(addresses, list):
            raise ValueError("summary.json 'unimplemented' entries must be lists")
        for raw_address in addresses:
            address = _address(raw_address)
            previous = sites.setdefault(address, mnemonic)
            if previous != mnemonic:
                raise ValueError(
                    f"summary mnemonic mismatch at {address:#010x}: "
                    f"{previous!r} != {mnemonic!r}"
                )
    return sites


def recover_asm_site(
    asm_dir: Path,
    address: int,
    mnemonic: str,
) -> str:
    """Find and return the normalized recovered-ASM instruction at ``address``."""
    address_prefix = f"0x{address:08X}"
    instruction_re = re.compile(
        rf"\b{re.escape(mnemonic)}\b(?P<operands>.*?)(?:\s+;.*)?$",
        re.IGNORECASE,
    )
    matches: list[str] = []
    for asm_path in sorted(asm_dir.rglob("*.asm")):
        for line in asm_path.read_text(encoding="utf-8").splitlines():
            address_match = ASM_ADDRESS_RE.match(line)
            if address_match is None:
                continue
            if f"0x{int(address_match.group('address'), 16):08X}" != address_prefix:
                continue
            instruction_match = instruction_re.search(line)
            if instruction_match is None:
                continue
            operands = instruction_match.group("operands").strip()
            text = mnemonic if not operands else f"{mnemonic} {operands}"
            matches.append(_normalize_instruction(text))

    if not matches:
        raise ValueError(
            f"no recovered ASM instruction for {address_prefix} {mnemonic}"
        )
    unique_matches = sorted(set(matches))
    if len(unique_matches) != 1:
        raise ValueError(
            f"conflicting recovered ASM instructions for {address_prefix}: "
            + ", ".join(unique_matches)
        )
    return unique_matches[0]


def audit(
    summary_path: Path,
    generated_dir: Path,
    asm_dir: Path,
) -> dict[str, Any]:
    """Verify summary, generated-C, and recovered-ASM site agreement."""
    summary_sites = recover_summary_sites(summary_path)
    generated_sites = recover_generated_sites(generated_dir)

    summary_addresses = set(summary_sites)
    generated_addresses = set(generated_sites)
    if summary_addresses != generated_addresses:
        missing_from_generated = sorted(summary_addresses - generated_addresses)
        missing_from_summary = sorted(generated_addresses - summary_addresses)
        raise ValueError(
            "summary/generated untranslated-site mismatch: "
            f"missing_from_generated={[f'{a:#010x}' for a in missing_from_generated]}, "
            f"missing_from_summary={[f'{a:#010x}' for a in missing_from_summary]}"
        )

    rows: list[dict[str, Any]] = []
    for address in sorted(summary_sites):
        mnemonic = summary_sites[address]
        generated_text = generated_sites[address]["generated_text"]
        generated_normalized = _normalize_instruction(generated_text)
        generated_mnemonic = generated_normalized.split(" ", 1)[0]
        if generated_mnemonic != mnemonic.lower():
            raise ValueError(
                f"summary/generated mnemonic mismatch at {address:#010x}: "
                f"{mnemonic!r} != {generated_text!r}"
            )

        asm_text = recover_asm_site(asm_dir, address, mnemonic)
        if generated_normalized != asm_text:
            raise ValueError(
                f"generated/ASM instruction mismatch at {address:#010x}: "
                f"{generated_text!r} != {asm_text!r}"
            )

        rows.append(
            {
                "address": f"0x{address:08X}",
                "mnemonic": mnemonic.lower(),
                "instruction": generated_normalized,
                "generated_sources": generated_sites[address]["generated_sources"],
            }
        )

    return {
        "summary": {
            "summary_sites": len(summary_sites),
            "generated_sites": len(generated_sites),
            "asm_sites": len(rows),
            "status": "verified-unimplemented-evidence-alignment",
            "original_semantics": "not-claimed",
        },
        "sites": rows,
    }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--summary", type=Path, required=True, help="recompiler summary.json")
    parser.add_argument("--generated", type=Path, required=True, help="generated C directory")
    parser.add_argument("--asm-dir", type=Path, required=True, help="recovered ASM directory")
    parser.add_argument("--format", choices=("json", "text"), default="text")
    parser.add_argument("--output-json", type=Path, help="write a local JSON report")
    args = parser.parse_args()

    result = audit(args.summary, args.generated, args.asm_dir)
    serialized = json.dumps(result, indent=2, sort_keys=True)
    if args.output_json:
        args.output_json.write_text(serialized + "\n", encoding="utf-8")
    if args.format == "json":
        print(serialized)
    else:
        for key, value in result["summary"].items():
            print(f"{key}={value}")
        for row in result["sites"]:
            print(
                "unimplemented_site={address} instruction={instruction} "
                "source={source}".format(
                    address=row["address"],
                    instruction=row["instruction"],
                    source=",".join(row["generated_sources"]),
                )
            )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
