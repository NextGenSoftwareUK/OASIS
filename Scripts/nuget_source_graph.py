"""Compare committed package input graphs; never infer source from publication dates."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import xml.etree.ElementTree as ET
import re
from functools import lru_cache


class Graph:
    def __init__(self, root, commit, *, external_pins=None, canonical=False):
        self.root = Path(root).resolve()
        self.commit = commit
        self.inputs = {}
        self.visited = set()
        self.external_pins = external_pins
        self.canonical = canonical

    def identity(self, root):
        return normalize_url(self.git(root, "remote", "get-url", "origin"))

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
            if self.external_pins is not None:
                for folder, dependency in self.external_pins.items():
                    external = Path(folder).resolve()
                    if path.is_relative_to(external):
                        return dependency["root"], dependency["commit"], path.relative_to(external).as_posix()
                raise ValueError(f"Unrecorded standalone dependency: {path}")
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
        name = self.identity(root) if self.canonical else root.name
        self.inputs[f"repository:{name}"] = self.git(root, "rev-parse", f"{commit}^{{tree}}")
        return root, commit, relative

    def record(self, path, required=False):
        root, commit, relative = self.location(path)
        if relative in ("", "."):
            key = f"repository:{self.identity(root)}" if self.canonical else str(root)
            self.inputs[key] = self.git(root, "rev-parse", f"{commit}^{{tree}}")
            return root, commit, relative, True
        entry = self.git(root, "ls-tree", commit, "--", relative)
        if entry.startswith("160000 commit "):
            # The directory itself can be a gitlink (for projects at a child
            # repository root). Compare content, not merge/commit identity.
            child_root, child_commit, child_relative = self.child_location(
                root / relative, entry.split()[2], "")
            return child_root, child_commit, child_relative, True
        if not entry and required:
            raise ValueError(f"Input absent from committed source: {path}")
        key = (f"{self.identity(root)}:{relative}" if self.canonical else
               str(Path(path).resolve().relative_to(self.root.parent)).replace("\\", "/"))
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
            if not ancestor.is_relative_to(root) and not ancestor.is_relative_to(self.root):
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


def normalize_url(url):
    return url.rstrip("/").removesuffix(".git").lower()


def resolve_baseline(item, receipts):
    """Validate an immutable registry identity against its recorded publication graph."""
    root = Path(item["root"]).resolve()
    package, version = item["packageId"], item["previousVersion"]
    published = item["publishedRepository"]
    commit, url = published.get("commit", ""), normalize_url(published.get("url", ""))
    if not re.fullmatch(r"[0-9a-fA-F]{40}", commit):
        raise ValueError(f"Missing repository commit: {package} {version}")
    receipt = receipts.get((package.lower(), version))
    if receipt:
        if receipt.get("kind") == "standalone":
            if commit != receipt["ownerCommit"] or url != normalize_url(receipt["ownerUrl"]):
                raise ValueError(f"Catalog provenance contradicts standalone receipt: {package} {version}")
            return receipt
        baseline = receipt["sourceCommit"]
        graph = Graph(root, baseline)
        owner_root, owner_commit, owner_project = graph.location(root / item["project"])
        owner_url = normalize_url(graph.git(owner_root, "remote", "get-url", "origin"))
        correction = receipt.get("metadataCorrection")
        corrected = correction and commit == correction["recordedCommit"] and url == normalize_url(correction["recordedUrl"]) and owner_url == normalize_url(correction["actualOwner"])
        if commit not in (baseline, owner_commit):
            raise ValueError(f"Catalog provenance contradicts reviewed publication: {package} {version}")
        if url not in (owner_url, normalize_url("https://github.com/NextGenSoftwareUK/OASIS")) and not corrected:
            # A reviewed global publication records its parent graph. Verify that
            # any inherited stale URL was actually declared by that exact source,
            # not an arbitrary foreign registry identity. Never infer a parent
            # graph from a child commit or URL alone.
            source_xml = ET.fromstring(graph.git(owner_root, "show", f"{owner_commit}:{owner_project}").lstrip("\ufeff"))
            ids = {node.text for node in source_xml.iter() if node.tag.rsplit("}", 1)[-1] == "PackageId"}
            urls = {normalize_url(node.text or "") for node in source_xml.iter() if node.tag.rsplit("}", 1)[-1] == "RepositoryUrl"}
            if package not in ids or url not in urls:
                raise ValueError(f"Catalog metadata differs from reviewed published source: {package} {version}")
        return baseline
    if url != normalize_url("https://github.com/NextGenSoftwareUK/OASIS"):
        raise ValueError(f"Component publication requires a reviewed parent dependency-graph receipt: {package} {version}")
    Graph(root, commit).git(root, "cat-file", "-e", f"{commit}^{{commit}}")
    return commit


def load_receipts(root):
    root = Path(root)
    data = json.loads((root / "Scripts/nuget-publication-provenance.json").read_text(encoding="utf-8-sig"))
    bulk = data["bulk"]
    inventory = (root / bulk["inventory"]).read_text(encoding="utf-8-sig")
    entries = re.findall(r"https://www\.nuget\.org/packages/([^/\s]+)/([0-9]+\.[0-9]+\.[0-9]+)\)", inventory)
    if len(entries) != bulk["expectedCount"] or len(set(entries)) != len(entries):
        raise ValueError("Reviewed bulk publication inventory coverage is invalid")
    receipts = {(package.lower(), version): bulk.copy() for package, version in entries}
    for receipt in data["packages"]:
        receipts[(receipt["packageId"].lower(), receipt["version"])] = receipt
    for correction in data["metadataCorrections"]:
        receipts[(correction["packageId"].lower(), correction["version"])]["metadataCorrection"] = correction
    return receipts


def compare_publication(item, receipts):
    baseline = resolve_baseline(item, receipts)
    if not isinstance(baseline, dict):
        return compare(item["root"], baseline, item["current"], item["project"])
    root = Path(item["root"]).resolve()
    current = Graph(root, item["current"], canonical=True)
    owner_root, _, owner_project = current.location(root / item["project"])
    if current.identity(owner_root) != normalize_url(baseline["ownerUrl"]):
        raise ValueError("Standalone receipt owner differs from current project owner")
    pins = {}
    for folder, dependency in baseline["dependencies"].items():
        if folder not in ("holochain-client-csharp", "NextGenSoftware-Libraries"):
            raise ValueError("Unknown standalone dependency location")
        path = root.parent / folder
        if normalize_url(Graph.git(path, "remote", "get-url", "origin")) != normalize_url(dependency["url"]):
            raise ValueError("Standalone dependency repository mismatch")
        if not re.fullmatch(r"[0-9a-f]{40}", dependency["commit"]):
            raise ValueError("Invalid standalone dependency commit")
        pins[str(path)] = {"root": path, "commit": dependency["commit"]}
    # In the producer ORM is nested under the Client checkout. Its ../Logos,
    # README and other sibling inputs belong to that exact Client tree, not OASIS.
    if baseline.get("ownerParent") != "holochain-client-csharp":
        raise ValueError("Standalone owner layout must be recorded explicitly")
    client = root.parent / baseline["ownerParent"]
    pins[str(owner_root.parent)] = {"root": client,
                                    "commit": baseline["dependencies"][baseline["ownerParent"]]["commit"]}
    old = Graph(owner_root, baseline["ownerCommit"], external_pins=pins, canonical=True)
    old_hash = old.fingerprint(owner_project)
    new_hash = current.fingerprint(item["project"])
    return {"changed": old_hash != new_hash, "baselineFingerprint": old_hash,
            "sourceFingerprint": new_hash, "baselineCommit": baseline["ownerCommit"],
            "baselineKind": "standalone", "producerRun": baseline["run"],
            "changedInputs": sorted(key for key in old.inputs.keys() | current.inputs.keys()
                                    if old.inputs.get(key) != current.inputs.get(key))}


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--batch", action="store_true")
    for name in ("root", "baseline", "current", "project"):
        parser.add_argument(f"--{name}")
    args = parser.parse_args()
    if args.batch:
        import sys
        requests = json.load(sys.stdin)
        receipts = load_receipts(requests[0]["root"]) if requests else {}
        print(json.dumps({item["packageId"]: compare_publication(item, receipts) for item in requests}))
    else:
        print(json.dumps(compare(args.root, args.baseline, args.current, args.project)))
