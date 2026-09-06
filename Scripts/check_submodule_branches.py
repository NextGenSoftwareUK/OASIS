#!/usr/bin/env python3
"""Enforce the submodule branch rule.

    OASIS master      pins every submodule at that submodule's  main
    OASIS Development pins every submodule at that submodule's  Development

Master deploys to Azure, so a master pinned at a Development-only commit ships
unreleased code. That happened before: master was pinned to submodule commits that
did not exist on those repos' main branches, and one submodule pointed at an empty
"Initial commit" so a fresh clone produced no source at all.

Run with no arguments to check the branch currently checked out, or pass a ref to
check any commit (useful for testing and for auditing history):

    python3 Scripts/check_submodule_branches.py
    python3 Scripts/check_submodule_branches.py origin/master

A raw SHA carries no branch name, so name the rule explicitly to audit one:

    python3 Scripts/check_submodule_branches.py 71be1c048 Development

Exit code 0 means every pointer is on the right branch; 1 means at least one is not.
Prints a line per submodule either way so CI logs show what was verified.
"""

import re
import subprocess
import sys

# OASIS branch -> the submodule branch its pointers must be on.
BRANCH_RULE = {
    "master": "main",
    "main": "main",
    "Development": "Development",
}


def git(*args, cwd=None):
    """Run git, returning stripped stdout, or None if the command failed."""
    try:
        out = subprocess.run(
            ["git", *args],
            cwd=cwd,
            capture_output=True,
            text=True,
            check=True,
        )
        return out.stdout.strip()
    except (subprocess.CalledProcessError, FileNotFoundError):
        return None


def submodule_paths():
    """Paths from .gitmodules, in declaration order. Handles paths containing spaces."""
    raw = git("config", "-f", ".gitmodules", "--get-regexp", r"^submodule\..*\.path$")
    if not raw:
        return []
    paths = []
    for line in raw.splitlines():
        # "submodule.<name with spaces>.path <path with spaces>" - split on the key only.
        m = re.match(r"^submodule\..*\.path\s+(.*)$", line)
        if m:
            paths.append(m.group(1))
    return paths


def declared_branch(path):
    """The branch = value .gitmodules records for `path`, or None if unset."""
    raw = git("config", "-f", ".gitmodules", "--get-regexp", r"^submodule\..*\.branch$")
    if not raw:
        return None
    for line in raw.splitlines():
        m = re.match(r"^submodule\.(.*)\.branch\s+(\S+)$", line)
        if m and m.group(1) == submodule_name_for(path):
            return m.group(2)
    return None


def submodule_name_for(path):
    """The .gitmodules section name whose path is `path`."""
    raw = git("config", "-f", ".gitmodules", "--get-regexp", r"^submodule\..*\.path$")
    for line in (raw or "").splitlines():
        m = re.match(r"^submodule\.(.*)\.path\s+(.*)$", line)
        if m and m.group(2) == path:
            return m.group(1)
    return None


def current_branch():
    name = git("rev-parse", "--abbrev-ref", "HEAD")
    if name and name != "HEAD":
        return name
    # Detached (CI often is): fall back to the ref CI reports.
    import os

    ref = os.environ.get("GITHUB_REF_NAME")
    return ref or name


def resolve(ref, path):
    """The commit the superproject records for `path`, or None."""
    return git("rev-parse", f"{ref}:{path}")


def is_initialised(path):
    """True if `path` is really the submodule's own working tree.

    Git run inside an empty submodule directory walks up and silently answers from the
    superproject, so a plain "--git-dir succeeded" test passes for an uninitialised
    submodule and every subsequent query then compares against the wrong repository.
    Comparing the reported toplevel against the path itself is what actually settles it.
    """
    import os

    top = git("rev-parse", "--show-toplevel", cwd=path)
    if not top:
        return False
    return os.path.realpath(top) == os.path.realpath(path)


def submodule_branch_head(path, branch):
    """Tip of `branch` in the submodule, preferring the remote ref."""
    for ref in (f"origin/{branch}", branch):
        sha = git("rev-parse", ref, cwd=path)
        if sha:
            return sha, ref
    return None, None


def main():
    # Optional ref argument: check that commit instead of the working tree's HEAD.
    ref = sys.argv[1] if len(sys.argv) > 1 else "HEAD"
    if ref == "HEAD":
        branch = current_branch()
    else:
        # Derive the rule from the ref name, e.g. "origin/master" -> "master".
        # A raw SHA has no branch name, so allow it to be named as a second argument.
        branch = sys.argv[2] if len(sys.argv) > 2 else ref.rsplit("/", 1)[-1]
    if branch not in BRANCH_RULE:
        print(f"Branch '{branch}' has no submodule pinning rule - nothing to check.")
        print(f"Rules are defined for: {', '.join(sorted(BRANCH_RULE))}")
        return 0

    expected_branch = BRANCH_RULE[branch]
    paths = submodule_paths()
    if not paths:
        print("No submodules found in .gitmodules.")
        return 0

    print(f"Branch '{branch}' must pin every submodule at its '{expected_branch}' branch.\n")

    failures = []
    for path in paths:
        pinned = resolve(ref, path)
        if pinned is None:
            failures.append((path, "not recorded in this commit"))
            print(f"  FAIL  {path}\n        not recorded in this commit")
            continue

        # An uninitialised submodule cannot be checked, and must not be silently
        # mis-reported: git run inside an empty submodule directory walks up and answers
        # from the superproject, comparing the pointer against the wrong repository.
        if not is_initialised(path):
            failures.append((path, "not initialised"))
            print(f"  FAIL  {path}")
            print("        submodule not initialised - run 'git submodule update --init'"
                  " (CI: checkout with submodules: recursive)")
            continue

        target, ref_used = submodule_branch_head(path, expected_branch)
        if target is None:
            failures.append((path, f"submodule has no '{expected_branch}' branch"))
            print(f"  FAIL  {path}\n        submodule has no '{expected_branch}' branch")
            continue

        if pinned == target:
            print(f"  ok    {path}  -> {pinned[:9]} ({ref_used})")
            declared = declared_branch(path)
            if declared is not None and declared != expected_branch:
                # The pointer is right today, but .gitmodules would send
                # "git submodule update --remote" to the wrong branch tomorrow.
                failures.append((path, "declared branch"))
                print(f"  FAIL  {path}")
                print(f"        .gitmodules says branch = {declared}, expected "
                      f"{expected_branch} - 'git submodule update --remote' would drift")
        else:
            # Say whether the pin is merely behind, or off the branch entirely.
            on_branch = subprocess.run(
                ["git", "merge-base", "--is-ancestor", pinned, target],
                cwd=path,
                capture_output=True,
            ).returncode == 0
            detail = (
                f"pinned {pinned[:9]} is behind {ref_used} {target[:9]} - bump the pointer"
                if on_branch
                else f"pinned {pinned[:9]} is NOT on {ref_used} - this ships unreleased code"
            )
            failures.append((path, detail))
            print(f"  FAIL  {path}\n        {detail}")

    print()
    if failures:
        uninit = [q for q, d in failures if d == "not initialised"]
        if uninit:
            print(f"{len(uninit)} submodule(s) were not initialised, so the rule "
                  "could not be checked for them. Initialise them and re-run.")
        other = [f for f in failures if f[1] != "not initialised"]
        if not other:
            return 1
        print(f"{len(other)} submodule pointer(s) violate the rule for '{branch}'.")
        print("To fix, for each submodule listed:")
        print(f"  1. make sure the work is on the submodule's '{expected_branch}' branch")
        print(f"  2. in the submodule:  git checkout {expected_branch} && git pull")
        print( "  3. in the superproject:  git add <submodule path> && git commit")
        return 1

    print(f"All {len(paths)} submodule pointers are on '{expected_branch}'.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
