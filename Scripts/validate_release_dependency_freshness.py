#!/usr/bin/env python3
"""Reject stale release inputs; never rewrite pins during a release."""
import subprocess
from validate_railway_dependency_manifest import load_manifest


def validate(values, remote_head):
    failures = []
    for prefix in ("HOLOCHAIN", "LIBRARIES"):
        expected = remote_head(values[f"{prefix}_REPO"])
        actual = values[f"{prefix}_COMMIT"]
        if actual != expected:
            failures.append(f"{prefix}_COMMIT={actual} is stale; main={expected}. "
                            "Review and advance the manifest, then validate the new candidate. "
                            "Do not reset another contributor's pins or change source mid-release.")
    return failures


def remote_head(repo):
    result = subprocess.run(["git", "ls-remote", "--exit-code", repo, "refs/heads/main"],
                            check=True, capture_output=True, text=True)
    return result.stdout.split()[0]


if __name__ == "__main__":
    failures = validate(load_manifest(), remote_head)
    if failures:
        raise SystemExit("\n".join(failures))
    print("Release dependencies match current upstream main; retain these exact pins for this run.")
