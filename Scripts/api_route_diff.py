#!/usr/bin/env python3
"""Lists the HTTP routes an API added or removed between a commit and HEAD, read from its ASP.NET controllers.

  api_route_diff.py --git-root <repo> --since <commit> --dir <controllers dir relative to the repo>

Prints {"removed": [...], "added": [...]} as JSON. A route is "VERB template", lower-cased, with parameter names dropped, so renaming
{id} to {avatarId} is not a change. A verb or path change shows up as one removed plus one added route. Used by
Scripts/Invoke-OASISGlobalRelease.ps1 to raise (never lower) an API's version bump: a removed route is a breaking change.
"""
import argparse, json, re, subprocess, sys

CLASS_RE = re.compile(r"\bclass\s+(\w+?)(Controller)?\b")
ROUTE_RE = re.compile(r'^\s*\[Route\("([^"]*)"\)\]')
HTTP_RE = re.compile(r'^\s*\[Http(Get|Post|Put|Delete|Patch)(?:\("([^"]*)"\))?[,\]\)]')


def git(root, *args):
    r = subprocess.run(["git", "-C", root, *args], capture_output=True)
    if r.returncode != 0: sys.exit(f"git {' '.join(args)} failed: {r.stderr.decode('utf-8', 'replace').strip()}")
    return r.stdout.decode("utf-8", "replace")


def norm(path): return re.sub(r"\{[^}/]*\}", "{}", "/".join(p for p in path.lower().split("/") if p))


def routes(root, ref, directory):
    out = set()
    listing = subprocess.run(["git", "-C", root, "ls-tree", "-r", "--name-only", ref, "--", directory], capture_output=True)
    for name in listing.stdout.decode("utf-8", "replace").splitlines():
        if not name.endswith(".cs"): continue
        prefix, pending, in_class = "", None, False
        for line in git(root, "show", f"{ref}:{name}").splitlines():
            if not in_class:
                m = ROUTE_RE.match(line)
                if m: pending = m.group(1)
                c = CLASS_RE.search(line)
                if c and "Controller" in line:
                    in_class = True
                    prefix = (pending or "").replace("[controller]", c.group(1).lower())
                continue
            h = HTTP_RE.match(line)
            if h:
                template = h.group(2) or ""
                out.add(f"{h.group(1).upper()} {norm(template[2:] if template.startswith('~/') else prefix + '/' + template)}")
    return out


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--git-root", required=True)
    ap.add_argument("--since", required=True)
    ap.add_argument("--dir", required=True)
    a = ap.parse_args()
    before, after = routes(a.git_root, a.since, a.dir), routes(a.git_root, "HEAD", a.dir)
    if not before:  # no controllers at the release point: nothing to compare against
        print(json.dumps({"removed": [], "added": []}))
        return
    print(json.dumps({"removed": sorted(before - after), "added": sorted(after - before)}))


if __name__ == "__main__":
    main()
