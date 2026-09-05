#!/usr/bin/env python3
"""Fail if any project on disk is missing from the solutions.

A .csproj that is in no .sln is never compiled by anything, so it can rot
indefinitely without a single error surfacing. That is exactly how ~200 provider
and test projects accumulated 1000+ compile errors unnoticed, and how SurrealDBOASIS
sat pinned to a NuGet version that had never been published.

Run locally:   python Scripts/check_solution_coverage.py
CI runs this before the full-solution build.

Projects that legitimately do not belong in a solution are listed in
Scripts/solution-coverage-ignore.txt, one glob-ish prefix or suffix per line.
"""
from __future__ import annotations

import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
IGNORE_FILE = os.path.join(ROOT, "Scripts", "solution-coverage-ignore.txt")

SKIP_DIRS = {".git", "bin", "obj", "node_modules", ".vs", "packages", ".claude"}

SOLUTIONS = [
    "The OASIS.sln",              # everything, including tests
    "The OASIS - NoTests.sln",    # production surface
]


def load_ignores() -> list[str]:
    if not os.path.exists(IGNORE_FILE):
        return []
    out = []
    for line in open(IGNORE_FILE, encoding="utf-8"):
        line = line.split("#", 1)[0].strip()
        if line:
            out.append(line.replace("/", os.sep).replace("\\", os.sep))
    return out


def ignored(rel: str, patterns: list[str]) -> bool:
    norm = rel.replace("/", os.sep).replace("\\", os.sep)
    return any(p in norm for p in patterns)


def projects_on_disk() -> dict[str, str]:
    found = {}
    for root, dirs, files in os.walk(ROOT):
        dirs[:] = [d for d in dirs if d not in SKIP_DIRS]
        for f in files:
            if f.endswith(".csproj"):
                rel = os.path.relpath(os.path.join(root, f), ROOT)
                found[rel] = f[: -len(".csproj")]
    return found


def projects_in_solution(sln: str) -> set[str]:
    path = os.path.join(ROOT, sln)
    if not os.path.exists(path):
        return set()
    src = open(path, encoding="utf-8-sig").read()
    out = set()
    for m in re.finditer(r'Project\("\{[^}]+\}"\) = "[^"]+", "([^"]+)", "\{[^}]+\}"', src):
        rel = m.group(1)
        if rel.lower().endswith(".csproj"):
            out.add(rel.replace("\\", os.sep).lower())
    return out


def main() -> int:
    patterns = load_ignores()
    disk = projects_on_disk()

    covered: set[str] = set()
    for sln in SOLUTIONS:
        covered |= projects_in_solution(sln)

    missing = sorted(
        rel for rel in disk
        if rel.lower() not in covered and not ignored(rel, patterns)
    )

    print(f"projects on disk        : {len(disk)}")
    print(f"covered by a solution   : {len(disk) - len(missing)}")
    print(f"explicitly ignored      : {len(patterns)} pattern(s)")
    print()

    if not missing:
        print("OK - every project is in a solution.")
        return 0

    print(f"FAIL - {len(missing)} project(s) are in no solution:\n")
    for rel in missing:
        print(f"  {rel}")
    print(
        "\nAdd them to the appropriate .sln, or add a pattern to"
        "\nScripts/solution-coverage-ignore.txt if they genuinely should not build."
    )
    return 1


if __name__ == "__main__":
    sys.exit(main())
