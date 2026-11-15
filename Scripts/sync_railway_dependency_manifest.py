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


def gitlink_commit(path: str, source: str) -> str:
    # HEAD/index modes resolve the parent gitlink and are safe for uninitialized
    # submodules. Working-tree mode is explicit and is only used after a workflow
    # has initialized and advanced every submodule checkout.
    if source == "working-tree":
        command = ["git", "-C", path, "rev-parse", "HEAD"]
    else:
        tree = "HEAD" if source == "head" else ""
        command = ["git", "rev-parse", f"{tree}:{path}"]

    result = subprocess.run(
        command,
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


def candidate_gitlinks(commit_ref: str) -> dict[str, str]:
    result = subprocess.run(
        ["git", "ls-tree", "-z", "--full-tree", commit_ref, "--", *GITLINKS.values()],
        cwd=ROOT,
        encoding="utf-8",
        capture_output=True,
    )
    if result.returncode:
        raise RuntimeError(f"Cannot read candidate gitlinks: {result.stderr.strip()}")

    pins = {}
    for entry in result.stdout.split("\0"):
        if not entry:
            continue
        fields, path = entry.split("\t", 1)
        mode, object_type, object_id = fields.split()
        if mode != "160000" or object_type != "commit" or not re.fullmatch(r"[0-9a-f]{40}", object_id):
            raise RuntimeError(f"Candidate path {path} is not a valid gitlink")
        pins[path] = object_id
    missing = set(GITLINKS.values()) - pins.keys()
    if missing:
        raise RuntimeError(f"Candidate missing gitlinks: {', '.join(sorted(missing))}")
    return pins


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--check",
        action="store_true",
        help="report drift without changing the manifest",
    )
    parser.add_argument(
        "--source",
        choices=("head", "index", "working-tree"),
        default="head",
        help=("where to resolve gitlinks: committed HEAD, the staged index, or "
              "initialized submodule working trees"),
    )
    parser.add_argument(
        "--check-ref",
        metavar="COMMIT",
        help="verify the manifest and gitlinks stored in a commit without touching the checkout",
    )
    args = parser.parse_args()
    if args.check_ref and not re.fullmatch(r"[0-9a-f]{40}", args.check_ref):
        parser.error("--check-ref requires a full commit SHA")
    if args.check_ref and args.source != "head":
        parser.error("--check-ref cannot be combined with an index or working-tree source")

    changes: list[str] = []

    try:
        if args.check_ref:
            entry = f"{args.check_ref}:{MANIFEST.relative_to(ROOT).as_posix()}"
            result = subprocess.run(["git", "show", entry], cwd=ROOT, encoding="utf-8", capture_output=True)
            if result.returncode:
                raise RuntimeError(f"Cannot read the candidate Railway manifest: {result.stderr.strip()}")
            updated = result.stdout
        else:
            updated = MANIFEST.read_text(encoding="utf-8")

        pins = candidate_gitlinks(args.check_ref) if args.check_ref else None
        for key, path in GITLINKS.items():
            commit = pins[path] if pins is not None else gitlink_commit(path, args.source)
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

    if args.check or args.check_ref:
        return 1

    MANIFEST.write_text(updated, encoding="utf-8")
    print(f"Updated {MANIFEST.relative_to(ROOT)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
