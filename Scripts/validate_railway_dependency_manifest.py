#!/usr/bin/env python3
"""Validate the single Railway WEB4-WEB10 dependency manifest.

The Dockerfiles must consume Docker/oasis-dependency-versions.env and must not
contain their own commit pins. With --require-gitlinks, each private dependency
pin must also equal the commit recorded by the parent repository.
"""

from __future__ import annotations

import argparse
import re
import subprocess
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
MANIFEST = ROOT / "Docker" / "oasis-dependency-versions.env"
DOCKERFILES = [ROOT / "Docker" / f"Dockerfile.web{number}" for number in range(4, 11)]
SHA_RE = re.compile(r"^[0-9a-f]{40}$")
GITLINKS = {
    "OASIS_API_CORE_COMMIT": "OASIS Architecture/NextGenSoftware.OASIS.API.Core",
    "OASIS_ONODE_CORE_COMMIT": "ONODE/NextGenSoftware.OASIS.API.ONODE.Core",
    "ONODE_MANAGER_COMMIT": "ONODE/ONODEManager",
    "STAR_ODK_COMMIT": "STAR ODK",
    "OASIS_WEB6_COMMIT": "WEB6",
    "OGENGINE_CLIENT_COMMIT": "OASIS Omniverse/OGEngineClient",
    "HOLONET_ORM_COMMIT": "HoloNET-ORM",
}
REQUIRED_COMMITS = {*GITLINKS, "LIBRARIES_COMMIT", "HOLOCHAIN_COMMIT"}


def load_manifest() -> dict[str, str]:
    values: dict[str, str] = {}
    for number, raw in enumerate(MANIFEST.read_text(encoding="utf-8").splitlines(), 1):
        line = raw.strip()
        if not line or line.startswith("#"):
            continue
        if "=" not in line:
            raise ValueError(f"{MANIFEST}:{number}: expected NAME=value")
        key, value = line.split("=", 1)
        if key in values:
            raise ValueError(f"{MANIFEST}:{number}: duplicate key {key}")
        values[key] = value
    return values


def gitlink(path: str) -> str:
    result = subprocess.run(
        ["git", "rev-parse", f"HEAD:{path}"],
        cwd=ROOT,
        text=True,
        capture_output=True,
    )
    if result.returncode:
        raise ValueError(f"Cannot resolve parent gitlink for {path}: {result.stderr.strip()}")
    return result.stdout.strip()


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--require-gitlinks",
        action="store_true",
        help="require private manifest pins to equal the parent repository gitlinks",
    )
    args = parser.parse_args()
    failures: list[str] = []

    try:
        values = load_manifest()
    except (OSError, ValueError) as exc:
        print(f"FAIL: {exc}")
        return 1

    missing = REQUIRED_COMMITS - values.keys()
    if missing:
        failures.append(f"manifest is missing: {', '.join(sorted(missing))}")
    for key in sorted(REQUIRED_COMMITS & values.keys()):
        if not SHA_RE.fullmatch(values[key]):
            failures.append(f"{key} must be a full lowercase 40-character commit SHA")

    for dockerfile in DOCKERFILES:
        text = dockerfile.read_text(encoding="utf-8")
        if "Docker/oasis-dependency-versions.env" not in text:
            failures.append(f"{dockerfile.name} does not copy the shared manifest")
        if "clone-pinned-oasis-dependencies.sh /tmp/oasis-dependency-versions.env" not in text:
            failures.append(f"{dockerfile.name} does not invoke the shared clone command")
        if re.search(r"^ARG [A-Z0-9_]+_COMMIT=", text, re.MULTILINE):
            failures.append(f"{dockerfile.name} contains a duplicated commit pin")

    if args.require_gitlinks:
        for key, path in GITLINKS.items():
            if key not in values:
                continue
            try:
                actual = gitlink(path)
            except ValueError as exc:
                failures.append(str(exc))
                continue
            if values[key] != actual:
                failures.append(
                    f"{key}={values[key]} but parent gitlink {path}={actual}; "
                    "advance the gitlink and manifest together"
                )

    if failures:
        print("Railway dependency policy failed:")
        for failure in failures:
            print(f"  - {failure}")
        return 1

    suffix = " and parent gitlinks" if args.require_gitlinks else ""
    print(f"Railway dependency manifest, seven Dockerfiles{suffix}: OK")
    return 0


if __name__ == "__main__":
    sys.exit(main())
