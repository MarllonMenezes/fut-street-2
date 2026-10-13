"""Join nested-BIG literal matches with the existing static XBE xref audit.

This is a provenance join over read-only TSV evidence produced from the same
retail ISO. It reports exact literal co-occurrence and nearby direct-call
context only. It does not recover functions, execute the XBE, or assign a
loader, material, UV, audio, animation, rig, skinning, morph, or GPU meaning.
"""

from __future__ import annotations

import argparse
import csv
from collections import defaultdict
from pathlib import Path


def read_tsv(path: Path) -> list[dict[str, str]]:
    with path.open("r", encoding="utf-8", newline="") as handle:
        return list(csv.DictReader(handle, delimiter="\t"))


def join_rows(
    nested_rows: list[dict[str, str]],
    xref_rows: list[dict[str, str]],
    context_rows: list[dict[str, str]],
) -> list[dict[str, str]]:
    xrefs: dict[str, list[dict[str, str]]] = defaultdict(list)
    contexts: dict[str, list[dict[str, str]]] = defaultdict(list)
    for row in xref_rows:
        xrefs[row["string"].casefold()].append(row)
    for row in context_rows:
        if row.get("call_source_virtual_address"):
            contexts[row["xref_string"].casefold()].append(row)

    joined: list[dict[str, str]] = []
    for row in nested_rows:
        if row["match_kind"] == "no-big-entry-match":
            continue
        literal = row["literal"]
        literal_xrefs = xrefs[literal.casefold()]
        literal_context = contexts[literal.casefold()]
        executable_targets = sorted({
            context["call_target_virtual_address"]
            for context in literal_context
            if context.get("target_is_executable", "").casefold() == "true"
        })
        joined.append(
            {
                "image": row["image"],
                "internal_path": row["internal_path"],
                "xbe_sha256": row["xbe_sha256"],
                "xbe_size": row["xbe_size"],
                "literal": literal,
                "literal_category": row["literal_category"],
                "literal_occurrences": row["literal_occurrences"],
                "nested_match_kind": row["match_kind"],
                "matched_nested_entries": row["matched_nested_entries"],
                "xbe_xref_count": str(len(literal_xrefs)),
                "unique_xref_source_instructions": str(len({
                    item["source_virtual_address"] for item in literal_xrefs
                })),
                "nearby_call_context_rows": str(len(literal_context)),
                "nearby_executable_call_targets": str(len(executable_targets)),
                "nearby_executable_call_target_vas": "|".join(executable_targets),
                "status": "candidate-nested-entry-plus-x86-xref-join",
                "original_semantics": "not-claimed",
            }
        )
    return sorted(joined, key=lambda row: row["literal"].casefold())


FIELDS = (
    "image", "internal_path", "xbe_sha256", "xbe_size", "literal",
    "literal_category", "literal_occurrences", "nested_match_kind",
    "matched_nested_entries", "xbe_xref_count", "unique_xref_source_instructions",
    "nearby_call_context_rows", "nearby_executable_call_targets",
    "nearby_executable_call_target_vas", "status", "original_semantics",
)


def write_report(rows: list[dict[str, str]], output: Path) -> None:
    output.parent.mkdir(parents=True, exist_ok=True)
    with output.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=FIELDS, delimiter="\t", lineterminator="\n")
        writer.writeheader()
        writer.writerows(rows)


def write_summary(rows: list[dict[str, str]], nested_path: Path, xref_path: Path, context_path: Path, output: Path) -> None:
    values = [
        ("nested_report", str(nested_path)),
        ("xref_report", str(xref_path)),
        ("call_context_report", str(context_path)),
        ("joined_nested_literals", len(rows)),
        ("joined_literals_with_xrefs", sum(int(row["xbe_xref_count"]) > 0 for row in rows)),
        ("joined_literals_with_nearby_call_context", sum(int(row["nearby_call_context_rows"]) > 0 for row in rows)),
        ("total_xrefs_for_joined_literals", sum(int(row["xbe_xref_count"]) for row in rows)),
        ("total_nearby_call_context_rows", sum(int(row["nearby_call_context_rows"]) for row in rows)),
        ("status", "verified-static-nested-entry-xref-join"),
        ("provenance", "same-ISO-nested-BIG-index+retail-XBE-static-xref+local-call-context"),
        ("original_semantics", "not-claimed"),
    ]
    output.parent.mkdir(parents=True, exist_ok=True)
    with output.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.writer(handle, delimiter="\t", lineterminator="\n")
        writer.writerow(("metric", "value"))
        writer.writerows(values)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("nested_report", type=Path)
    parser.add_argument("xref_report", type=Path)
    parser.add_argument("context_report", type=Path)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--summary", type=Path, required=True)
    args = parser.parse_args()
    rows = join_rows(
        read_tsv(args.nested_report),
        read_tsv(args.xref_report),
        read_tsv(args.context_report),
    )
    write_report(rows, args.report)
    write_summary(rows, args.nested_report, args.xref_report, args.context_report, args.summary)
    print(
        f"joined_nested_literals: {len(rows)}; "
        f"with_xrefs: {sum(int(row['xbe_xref_count']) > 0 for row in rows)}; "
        "status: verified-static-nested-entry-xref-join"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
