"""Reparse the original/candidate geometry stream join with raw payload hashes.

The control is intentionally narrower than a renderer semantic claim.  It
joins the 38 candidate geometry groups to the original five-slot topology,
candidate fetch records and independently revalidated candidate payload hashes.
It proves byte identity for the observed stream records only; it does not
assign position, UV, material, draw, GPU, skinning, rigging, animation or
morph semantics.
"""

from __future__ import annotations

import argparse
import csv
from collections import defaultdict
from pathlib import Path
from typing import Dict, Iterable, List, Sequence, Tuple


STATUS = "verified-independent-original-candidate-geometry-payload-identity-control"
ORIGINAL_SEMANTICS = "not-claimed"
PROVENANCE = (
    "independent-TSV-keyed-geometry-join+original-five-slot-topology+candidate-fetch-metadata+"
    "candidate-raw-payload-SHA256;byte-identity-only;no-semantic-promotion"
)

KEY = Tuple[str, str, str, str]

REPORT_FIELDS = (
    "model_archive", "model_object", "layer", "primitive",
    "geometry_source_slot", "geometry_stream_name", "original_slot_rows",
    "candidate_slot_rows", "raw_hash_rows", "symbol_exact_rows",
    "metadata_exact_rows", "payload_hash_exact_rows", "original_input_valid",
    "candidate_input_valid", "geometry_input_valid", "raw_input_valid",
    "complete_identity", "status", "provenance", "original_semantics", "issue",
)

SUMMARY_FIELDS = (
    "topology_source_report", "candidate_fetch_source_report",
    "candidate_geometry_source_report", "raw_identity_source_report",
    "geometry_rows", "candidate_geometry_rows", "joined_rows",
    "missing_candidate_geometry_keys", "missing_candidate_fetch_keys",
    "missing_raw_identity_keys", "original_slot_rows", "candidate_slot_rows",
    "raw_hash_rows", "symbol_exact_rows", "metadata_exact_rows",
    "payload_hash_exact_rows", "complete_identity_rows",
    "original_input_valid_rows", "candidate_input_valid_rows",
    "geometry_input_valid_rows", "raw_input_valid_rows", "status",
    "provenance", "original_semantics",
)


def _read(path: Path) -> List[Dict[str, str]]:
    with path.open("r", encoding="utf-8-sig", newline="") as handle:
        return list(csv.DictReader(handle, delimiter="\t"))


def _write(path: Path, fields: Sequence[str], rows: Iterable[Dict[str, str]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(
            handle, fieldnames=list(fields), delimiter="\t", lineterminator="\n"
        )
        writer.writeheader()
        writer.writerows({field: row.get(field, "") for field in fields} for row in rows)


def _key(row: Dict[str, str]) -> KEY:
    return (
        row.get("model_archive", ""),
        row.get("model_object", row.get("object_name", "")),
        row.get("layer", ""),
        row.get("primitive", ""),
    )


def _int(row: Dict[str, str], field: str) -> int:
    try:
        return int(row.get(field, ""), 0)
    except (TypeError, ValueError) as exc:
        raise ValueError(f"invalid integer in {field}: {row.get(field)!r}") from exc


def _true(row: Dict[str, str], field: str) -> bool:
    return row.get(field, "").strip().lower() == "true"


def _hex(value: str) -> str:
    return value.strip().lower().removeprefix("0x")


def _parse_slot_records(value: str) -> Dict[int, Dict[str, str]]:
    output: Dict[int, Dict[str, str]] = {}
    for record in value.split("|"):
        if not record:
            continue
        fields: Dict[str, str] = {}
        for part in record.split(";"):
            if "=" in part:
                name, field_value = part.split("=", 1)
                fields[name] = field_value
        if "slot" not in fields:
            continue
        slot = int(fields["slot"], 0)
        output[slot] = fields
    return output


def _parse_hashes(value: str) -> Dict[int, str]:
    output: Dict[int, str] = {}
    for record in value.split("|"):
        if not record or ":" not in record:
            continue
        slot_text, digest = record.split(":", 1)
        if slot_text.startswith("slot="):
            output[int(slot_text[5:], 0)] = _hex(digest)
    return output


def analyze(
    topology_rows: Sequence[Dict[str, str]],
    candidate_fetch_rows: Sequence[Dict[str, str]],
    candidate_geometry_rows: Sequence[Dict[str, str]],
    raw_identity_rows: Sequence[Dict[str, str]],
) -> Tuple[List[Dict[str, str]], Dict[str, str]]:
    if not topology_rows or not candidate_fetch_rows or not candidate_geometry_rows or not raw_identity_rows:
        raise ValueError("all four source reports must contain rows")

    topology_by_key: Dict[KEY, List[Dict[str, str]]] = defaultdict(list)
    for row in topology_rows:
        topology_by_key[_key(row)].append(row)
    fetch_by_key = {_key(row): row for row in candidate_fetch_rows}
    geometry_by_key = {_key(row): row for row in candidate_geometry_rows}
    raw_by_key = {_key(row): row for row in raw_identity_rows}
    keys = sorted(geometry_by_key)

    counters = defaultdict(int)
    report: List[Dict[str, str]] = []
    for key in keys:
        geometry = geometry_by_key[key]
        topology = sorted(topology_by_key.get(key, []), key=lambda row: _int(row, "fetch_slot"))
        fetch = fetch_by_key.get(key)
        raw = raw_by_key.get(key)
        candidate_slots = _parse_slot_records(fetch.get("slot_records", "") if fetch else "")
        raw_hashes = _parse_hashes(raw.get("raw_payload_hashes", "") if raw else "")

        original_valid = (
            len(topology) == 5 and
            [(_int(row, "fetch_slot"), _int(row, "category")) for row in topology] ==
            [(slot, _int(topology[0], "category") + slot) for slot in range(5)] and
            all(
                row.get("status") == "verified-bounded-original-primitive-stream-topology" and
                _true(row, "structural_match") and _true(row, "valid_payload_range") and
                len(row.get("payload_sha256", "")) == 64
                for row in topology
            )
        )
        candidate_valid = bool(fetch) and (
            fetch.get("status") == "verified-bounded-candidate-fetch-record-association" and
            _true(fetch, "complete_five_slot_layout") and
            _int(fetch, "fetch_slot_count") == 5 and
            _int(fetch, "fetch_binding_count") == 5 and
            set(candidate_slots) == set(range(5)) and
            all(candidate_slots[slot].get("valid", "").lower() == "true" for slot in range(5))
        )
        geometry_valid = (
            geometry.get("status") == "verified-bounded-candidate-geometry-stream-audit" and
            _int(geometry, "candidate_source_slot") == 0 and
            _true(geometry, "candidate_primitive_complete") and
            _true(geometry, "candidate_geometry_finite_and_indexed")
        )
        raw_valid = bool(raw) and (
            raw.get("status") == "verified-independent-original-candidate-raw-stream-identity-control" and
            _int(raw, "raw_valid_slot_rows") == 5 and
            _int(raw, "raw_metadata_exact_rows") == 5 and
            _int(raw, "raw_payload_hash_rows") == 5 and
            len(raw_hashes) == 5
        )

        symbol_exact = 0
        metadata_exact = 0
        payload_exact = 0
        for slot in range(5):
            original = topology[slot] if len(topology) == 5 else {}
            candidate = candidate_slots.get(slot, {})
            if original.get("symbol_name", "") == candidate.get("stream", "") and original:
                symbol_exact += 1
            if (
                original and candidate and
                _int(original, "stream_stride") == int(candidate.get("stream_stride", "-1"), 0) and
                _int(original, "byte_length") == int(candidate.get("stream_bytes", "-1"), 0) and
                _int(original, "element_count") == int(candidate.get("elements", "-1"), 0)
            ):
                metadata_exact += 1
            if _hex(original.get("payload_sha256", "")) == raw_hashes.get(slot, "") and len(original.get("payload_sha256", "")) == 64:
                payload_exact += 1

        complete = (
            original_valid and candidate_valid and geometry_valid and raw_valid and
            symbol_exact == 5 and metadata_exact == 5 and payload_exact == 5
        )
        counters["original_slot_rows"] += len(topology)
        counters["candidate_slot_rows"] += len(candidate_slots)
        counters["raw_hash_rows"] += len(raw_hashes)
        counters["symbol_exact_rows"] += symbol_exact
        counters["metadata_exact_rows"] += metadata_exact
        counters["payload_hash_exact_rows"] += payload_exact
        counters["original_input_valid_rows"] += int(original_valid)
        counters["candidate_input_valid_rows"] += int(candidate_valid)
        counters["geometry_input_valid_rows"] += int(geometry_valid)
        counters["raw_input_valid_rows"] += int(raw_valid)
        counters["complete_identity_rows"] += int(complete)
        report.append({
            "model_archive": key[0], "model_object": key[1], "layer": key[2],
            "primitive": key[3], "geometry_source_slot": geometry.get("candidate_source_slot", ""),
            "geometry_stream_name": geometry.get("candidate_stream_name", ""),
            "original_slot_rows": str(len(topology)), "candidate_slot_rows": str(len(candidate_slots)),
            "raw_hash_rows": str(len(raw_hashes)), "symbol_exact_rows": str(symbol_exact),
            "metadata_exact_rows": str(metadata_exact), "payload_hash_exact_rows": str(payload_exact),
            "original_input_valid": str(original_valid), "candidate_input_valid": str(candidate_valid),
            "geometry_input_valid": str(geometry_valid), "raw_input_valid": str(raw_valid),
            "complete_identity": str(complete), "status": STATUS if complete else "inconclusive-" + STATUS,
            "provenance": PROVENANCE, "original_semantics": ORIGINAL_SEMANTICS,
            "issue": "" if complete else "geometry group did not rejoin all five original/candidate payload identities",
        })

    missing_fetch = sorted(set(keys) - set(fetch_by_key))
    missing_raw = sorted(set(keys) - set(raw_by_key))
    summary = {
        "topology_source_report": "eagl-original-primitive-stream-topology.tsv",
        "candidate_fetch_source_report": "eagl-candidate-fetch-association.tsv",
        "candidate_geometry_source_report": "eagl-candidate-geometry-stream.tsv",
        "raw_identity_source_report": "eagl-original-candidate-raw-stream-identity.tsv",
        "geometry_rows": str(len(keys)), "candidate_geometry_rows": str(len(candidate_geometry_rows)),
        "joined_rows": str(len(report)), "missing_candidate_geometry_keys": "0",
        "missing_candidate_fetch_keys": str(len(missing_fetch)), "missing_raw_identity_keys": str(len(missing_raw)),
        **{name: str(counters[name]) for name in (
            "original_slot_rows", "candidate_slot_rows", "raw_hash_rows", "symbol_exact_rows",
            "metadata_exact_rows", "payload_hash_exact_rows", "complete_identity_rows",
            "original_input_valid_rows", "candidate_input_valid_rows", "geometry_input_valid_rows",
            "raw_input_valid_rows",
        )},
        "status": STATUS if len(report) == 38 and counters["complete_identity_rows"] == 38 and not missing_fetch and not missing_raw else "incomplete-" + STATUS,
        "provenance": PROVENANCE, "original_semantics": ORIGINAL_SEMANTICS,
    }
    return report, summary


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("topology")
    parser.add_argument("candidate_fetch")
    parser.add_argument("candidate_geometry")
    parser.add_argument("raw_identity")
    parser.add_argument("--report", required=True)
    parser.add_argument("--summary", required=True)
    args = parser.parse_args()
    report, summary = analyze(
        _read(Path(args.topology)), _read(Path(args.candidate_fetch)),
        _read(Path(args.candidate_geometry)), _read(Path(args.raw_identity)),
    )
    _write(Path(args.report), REPORT_FIELDS, report)
    _write(Path(args.summary), SUMMARY_FIELDS, [summary])
    print(f"status: {summary['status']}")
    print(f"geometry_rows: {summary['geometry_rows']}")
    print(f"complete_identity_rows: {summary['complete_identity_rows']}")
    print(f"payload_hash_exact_rows: {summary['payload_hash_exact_rows']}")
    return 0 if summary["status"] == STATUS else 1


if __name__ == "__main__":
    raise SystemExit(main())
