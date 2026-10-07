#!/usr/bin/env python3
"""Synchronize Railway's private dependency pins with parent gitlinks."""

from __future__ import annotations

import argparse
import re
import subprocess
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
MANIFEST = ROOT / "Docker" / "oasis-dependency-versions.env"
GITLINKS = {
    "OASIS_API_CORE_COMMIT": "OASIS Architecture/NextGenSoftware.OASIS.API.Core",
    "OASIS_ONODE_CORE_COMMIT": "ONODE/NextGenSoftware.OASIS.API.ONODE.Core",
    "ONODE_MANAGER_COMMIT": "ONODE/ONODEManager",
    "STAR_ODK_COMMIT": "STAR ODK",
    "OASIS_WEB6_COMMIT": "WEB6",
    "OGENGINE_CLIENT_COMMIT": "OASIS Omniverse/OGEngineClient",
    "HOLONET_ORM_COMMIT": "HoloNET-ORM",
}


def gitlink_commit(path: str) -> str:
    # Resolve the commit recorded by the parent tree, not `git -C <path> HEAD`.
    # An uninitialized submodule directory has no .git metadata, so Git walks up
    # to the parent repository and the latter command silently returns the
    # parent's HEAD instead of the gitlink SHA.
    result = subprocess.run(
        ["git", "rev-parse", f"HEAD:{path}"],
        cwd=ROOT,
        text=True,
        capture_output=True,
    )
    if result.returncode:
        raise RuntimeError(f"Cannot resolve parent gitlink {path}: {result.stderr.strip()}")
    commit = result.stdout.strip()
    if not re.fullmatch(r"[0-9a-f]{40}", commit):
        raise RuntimeError(f"Gitlink {path} returned an invalid commit: {commit}")
    return commit


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--check",
        action="store_true",
        help="report drift without changing the manifest",
    )
    args = parser.parse_args()

    original = MANIFEST.read_text(encoding="utf-8")
    updated = original
    changes: list[str] = []

    try:
        for key, path in GITLINKS.items():
            commit = gitlink_commit(path)
            pattern = re.compile(rf"(?m)^{re.escape(key)}=([0-9a-f]{{40}})$")
            match = pattern.search(updated)
            if not match:
                raise RuntimeError(f"Manifest is missing a valid {key}=<40-character SHA> entry")
            if match.group(1) != commit:
                changes.append(f"{key}: {match.group(1)} -> {commit}")
                updated = pattern.sub(f"{key}={commit}", updated, count=1)
    except (OSError, RuntimeError) as exc:
        print(f"FAIL: {exc}")
        return 1

    if not changes:
        print("Railway manifest already matches the parent repository gitlinks.")
        return 0

    print("Railway manifest drift:")
    for change in changes:
        print(f"  - {change}")

    if args.check:
        return 1

    MANIFEST.write_text(updated, encoding="utf-8")
    print(f"Updated {MANIFEST.relative_to(ROOT)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
