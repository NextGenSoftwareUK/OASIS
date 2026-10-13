"""Compare committed package input graphs; never infer source from publication dates."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import xml.etree.ElementTree as ET
from functools import lru_cache


class Graph:
    def __init__(self, root, commit):
        self.root = Path(root).resolve()
        self.commit = commit
        self.inputs = {}
        self.visited = set()

    @staticmethod
    @lru_cache(maxsize=50000)
    def git(root, *args):
        result = subprocess.run(["git", "-C", str(root), *args], capture_output=True, text=True, encoding="utf-8")
        if result.returncode:
            raise ValueError(result.stderr.strip())
        return result.stdout.strip()

    def location(self, path):
        path = Path(path).resolve()
        try:
            relative = path.relative_to(self.root).as_posix()
        except ValueError:
            manifest = self.git(self.root, "show", f"{self.commit}:Docker/oasis-dependency-versions.env")
            pins = dict(line.split("=", 1) for line in manifest.splitlines() if "=" in line and not line.startswith("#"))
            for folder, key in (("NextGenSoftware-Libraries", "LIBRARIES_COMMIT"), ("holochain-client-csharp", "HOLOCHAIN_COMMIT")):
                external = self.root.parent / folder
                if path.is_relative_to(external):
                    return external, pins[key], path.relative_to(external).as_posix()
            raise ValueError(f"Unpinned external input: {path}")
        root, commit = self.root, self.commit
        parts = relative.split("/")
        for index in range(1, len(parts)):
            prefix = "/".join(parts[:index])
            entry = self.git(root, "ls-tree", commit, "--", prefix)
            if entry.startswith("160000 commit "):
                child = root / prefix
                return self.child_location(child, entry.split()[2], "/".join(parts[index:]))
        return root, commit, relative

    def child_location(self, root, commit, relative):
        # A child repository's complete tree is conservatively included. This
        # covers child imports and conditional dependency wiring without guessing.
        self.inputs[f"repository:{root.name}"] = self.git(root, "rev-parse", f"{commit}^{{tree}}")
        return root, commit, relative

    def record(self, path, required=False):
        root, commit, relative = self.location(path)
        if relative in ("", "."):
            self.inputs[str(root)] = self.git(root, "rev-parse", f"{commit}^{{tree}}")
            return root, commit, relative, True
        entry = self.git(root, "ls-tree", commit, "--", relative)
        if not entry and required:
            raise ValueError(f"Input absent from committed source: {path}")
        key = str(Path(path).resolve().relative_to(self.root.parent)).replace("\\", "/")
        self.inputs[key] = entry.split("\t", 1)[0] if entry else "absent"
        return root, commit, relative, bool(entry)

    def project(self, path):
        path = Path(path).resolve()
        if path in self.visited:
            return
        self.visited.add(path)
        root, commit, relative, exists = self.record(path)
        if not exists:
            return
        self.record(path.parent, required=True)
        for ancestor in (path.parent, *path.parent.parents):
            if not ancestor.is_relative_to(root):
                break
            for name in ("Directory.Build.props", "Directory.Build.targets", "Directory.Packages.props", "NuGet.Config", "nuget.config", "global.json"):
                self.record(ancestor / name)
        try:
            xml = ET.fromstring(self.git(root, "show", f"{commit}:{relative}").lstrip("\ufeff"))
        except ET.ParseError as error:
            raise ValueError(f"Invalid project XML at {root} {commit}:{relative}: {error}") from error
        for node in xml.iter():
            kind = node.tag.rsplit("}", 1)[-1]
            include = node.get("Include") or (node.get("Project") if kind == "Import" else None)
            if not include or kind not in ("ProjectReference", "Import", "Compile", "Content", "None", "EmbeddedResource"):
                continue
            # Local globs are covered by the whole project directory tree.
            normalized = include.replace("\\", "/")
            if "$" in normalized or "*" in normalized or ";" in normalized:
                if ".." in normalized or kind in ("Import", "ProjectReference"):
                    raise ValueError(f"Cannot prove dynamic external input: {path}: {include}")
                continue
            target = (path.parent / normalized).resolve()
            if kind in ("ProjectReference", "Import"):
                self.project(target)
            else:
                self.record(target)

    def fingerprint(self, project):
        self.project(self.root / project)
        return hashlib.sha256(json.dumps(self.inputs, sort_keys=True).encode()).hexdigest()


def compare(root, baseline, current, project):
    old, new = Graph(root, baseline), Graph(root, current)
    old_hash, new_hash = old.fingerprint(project), new.fingerprint(project)
    return {"changed": old_hash != new_hash, "baselineFingerprint": old_hash,
            "sourceFingerprint": new_hash, "baselineCommit": baseline,
            "changedInputs": sorted(key for key in old.inputs.keys() | new.inputs.keys()
                                    if old.inputs.get(key) != new.inputs.get(key))}


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--batch", action="store_true")
    for name in ("root", "baseline", "current", "project"):
        parser.add_argument(f"--{name}")
    args = parser.parse_args()
    if args.batch:
        import sys
        requests = json.load(sys.stdin)
        print(json.dumps({item["packageId"]: compare(item["root"], item["baseline"], item["current"], item["project"]) for item in requests}))
    else:
        print(json.dumps(compare(args.root, args.baseline, args.current, args.project)))
