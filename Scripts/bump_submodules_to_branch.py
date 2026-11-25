#!/usr/bin/env python3
"""Move submodule pointers forward to their branch tips (the fix check_submodule_branches.py asks for).

    python3 Scripts/bump_submodules_to_branch.py Development          # stage the bumps
    python3 Scripts/bump_submodules_to_branch.py Development --check  # report only

For each submodule, OASIS <branch> must pin the tip of the submodule's branch from
check_submodule_branches.BRANCH_RULE. A pin that is merely behind that tip is staged with
`git update-index` (no submodule checkout needed). A pin that is NOT an ancestor of the tip
is left alone and reported: that is unreleased or rewritten work and needs a person.

Exit codes: 0 nothing left to fix (after staging, unless --check); 1 a pin needs a person
(or, with --check, anything is behind); 2 a remote could not be read.
"""

import os
import subprocess
import sys

# Never wait on a credential prompt: an unreadable private remote is reported, not hung on.
os.environ["GIT_TERMINAL_PROMPT"] = "0"
os.environ.setdefault("GCM_INTERACTIVE", "never")

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from check_submodule_branches import BRANCH_RULE, git, submodule_name_for, submodule_paths  # noqa: E402


def submodule_url(path):
    name = submodule_name_for(path)
    return git("config", "-f", ".gitmodules", f"submodule.{name}.url") if name else None


def github_api(path):
    """GET a GitHub REST path as JSON, or None. Uses GH_TOKEN / GITHUB_TOKEN when set (CI passes
    the submodule PAT), otherwise the gh CLI's login. No git networking: on Windows a credential
    manager can block a git remote call indefinitely, and a killed git leaves its helper holding
    the pipes open, so timeouts don't help."""
    import json
    import urllib.request

    token = os.environ.get("GH_TOKEN") or os.environ.get("GITHUB_TOKEN")
    try:
        if token:
            req = urllib.request.Request(f"https://api.github.com/{path}", headers={
                "Authorization": f"Bearer {token}", "Accept": "application/vnd.github+json"})
            with urllib.request.urlopen(req, timeout=60) as resp:
                return json.load(resp)
        out = subprocess.run(["gh", "api", path], capture_output=True, text=True, check=True,
                             timeout=60, stdin=subprocess.DEVNULL)
        return json.loads(out.stdout)
    except Exception:
        return None


def github_repo(url):
    import re

    m = re.match(r"^https://github\.com/([^/]+)/([^/]+?)(?:\.git)?/?$", url or "")
    return f"{m.group(1)}/{m.group(2)}" if m else None


def remote_tip(url, branch):
    repo = github_repo(url)
    data = github_api(f"repos/{repo}/branches/{branch}") if repo else None
    return (data or {}).get("commit", {}).get("sha")


def is_ancestor(url, older, newer):
    """True when `newer` is ahead of (or equal to) `older`, False if they diverged, None if unknown."""
    repo = github_repo(url)
    data = github_api(f"repos/{repo}/compare/{older}...{newer}") if repo else None
    status = (data or {}).get("status")
    if status in ("ahead", "identical"):
        return True
    if status in ("diverged", "behind"):
        return False
    return None


def main():
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    check_only = "--check" in sys.argv
    branch = args[0] if args else "Development"
    if branch not in BRANCH_RULE:
        print(f"Branch '{branch}' has no submodule pinning rule.")
        return 1
    expected = BRANCH_RULE[branch]

    bumped, behind, needs_person, unreadable = [], [], [], []
    for path in submodule_paths():
        pinned = git("rev-parse", f"HEAD:{path}")
        url = submodule_url(path)
        tip = remote_tip(url, expected) if url else None
        if not pinned or not tip:
            unreadable.append(path)
            print(f"  ??    {path}: could not read the pin or {url} {expected}")
            continue
        if pinned == tip:
            print(f"  ok    {path} -> {pinned[:9]}")
            continue

        ancestor = is_ancestor(url, pinned, tip)
        if ancestor is None:
            unreadable.append(path)
            print(f"  ??    {path}: could not compare {pinned[:9]} with {tip[:9]} on GitHub")
        elif not ancestor:
            needs_person.append(path)
            print(f"  FAIL  {path}: pinned {pinned[:9]} is NOT on {expected} ({tip[:9]}); not moving it")
        elif check_only:
            behind.append(path)
            print(f"  behind {path}: {pinned[:9]} -> {tip[:9]}")
        else:
            git("update-index", "--cacheinfo", f"160000,{tip},{path}")
            bumped.append(path)
            print(f"  bump  {path}: {pinned[:9]} -> {tip[:9]}")

    if bumped:
        print("\nStaged: " + ", ".join(bumped))
    if needs_person:
        print("\nNeeds a person (pin is off the branch): " + ", ".join(needs_person))
        return 1
    if unreadable:
        return 2
    return 1 if (check_only and behind) else 0


if __name__ == "__main__":
    sys.exit(main())
