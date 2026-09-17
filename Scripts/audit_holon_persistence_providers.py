#!/usr/bin/env python3
"""Report every concrete SaveHolonAsync implementation and the identity signals it uses.

This is a source audit, not an integration test.  It deliberately does not certify a
remote database or blockchain without its configured integration environment.
"""
from __future__ import annotations
import argparse
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
METHOD = re.compile(r"(?:public|protected)\s+(?:override\s+)?async\s+Task\s*<\s*OASISResult\s*<\s*IHolon\s*>\s*>\s+SaveHolonAsync\s*\(", re.M)


def body_after(text: str, start: int) -> str:
    brace = text.find("{", start)
    if brace < 0:
        return ""
    depth = 0
    for index in range(brace, len(text)):
        if text[index] == "{": depth += 1
        elif text[index] == "}":
            depth -= 1
            if depth == 0:
                return text[brace:index + 1]
    return text[brace:]


def classify(body: str) -> tuple[str, str]:
    # Comments document the policy and must not be mistaken for executable lifecycle
    # branching by this static check.
    executable = re.sub(r"/\*.*?\*/", "", body, flags=re.S)
    executable = re.sub(r"//[^\n]*", "", executable)
    lower = executable.lower()
    has_id = ".id" in lower
    # CreatedDate assignments are allowed for audit stamping/preservation.  The bad
    # lifecycle decision is IsNewHolon (or a provider key) deciding whether to create.
    lifecycle_branch = bool(re.search(r"\bif\s*\([^\)]*(isnewholon|provideruniquestoragekey)", executable, re.I))
    if "notsupported" in lower or "not implemented" in lower:
        return "stub", "returns unsupported/not implemented"
    if not has_id:
        return "divergent", "does not reference public IHolon.Id"
    if lifecycle_branch:
        return "divergent", "branches on a lifecycle/provider-key field"
    if any(token in lower for token in ("uploadjson", "sendtransaction", "mint", "broadcast", "arweave")):
        return "append-only", "writes an immutable remote record; requires provider-specific versioning contract"
    if any(token in lower for token in ("upsert", "merge", "replaceone", "update", "writealltext", "repository.saveholon")):
        return "public-id write", "source uses Id as, or passes Id into, the storage write"
    return "review", "Id is present but write primitive needs manual integration review"


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--format", choices=("markdown", "tsv"), default="markdown")
    args = parser.parse_args()
    rows = []
    for area in (ROOT / "Providers" / "Storage", ROOT / "Providers" / "Blockchain"):
        for source in sorted(area.rglob("*.cs")):
            if ("TestProjects" in source.parts or "Commented" in source.name
                    or any(part in {"bin", "obj"} for part in source.parts)):
                continue
            text = source.read_text(encoding="utf-8-sig", errors="ignore")
            for match in METHOD.finditer(text):
                body = body_after(text, match.start())
                status, note = classify(body)
                rows.append((status, source.relative_to(ROOT).as_posix(), note))
    if args.format == "tsv":
        print("status\tfile\tnote")
        for row in rows: print("\t".join(row))
    else:
        print("| Status | SaveHolonAsync implementation | Static result |")
        print("|---|---|---|")
        for status, file, note in rows:
            print(f"| {status} | `{file}` | {note} |")
        from collections import Counter
        counts = Counter(row[0] for row in rows)
        print("\n" + ", ".join(f"{key}: {counts[key]}" for key in sorted(counts)))
    return 0

if __name__ == "__main__":
    raise SystemExit(main())
