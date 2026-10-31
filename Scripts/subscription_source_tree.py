"""Explicit immutable-source input for structural subscription validators; no checkout or download."""
import argparse
from pathlib import Path
import re
import subprocess

ROOT = Path(__file__).resolve().parents[1]


class SourceTree:
    def __init__(self, root=ROOT, repositories=None, pins=None):
        self.root = Path(root)
        self.repositories = repositories or {}
        self.pins = pins or {}
        self.inventory = {}
        for module, repository in self.repositories.items():
            commit = self.pins[module]
            if not re.fullmatch(r"[0-9a-f]{40}", commit):
                raise ValueError(f"{module}: requires a full immutable commit SHA")
            kind = self._git(repository, "cat-file", "-t", commit).strip()
            if kind != "commit":
                raise ValueError(f"{module}: pinned object is not a commit")
            recorded = self._git(self.root, "rev-parse", f"HEAD:{module}").strip()
            if recorded != commit:
                raise ValueError(f"{module}: manifest and committed parent gitlink differ")
            self.inventory[module] = set(self._git(repository, "ls-tree", "-r", "-t", "--name-only", commit).splitlines())

    @staticmethod
    def _git(repository, *args):
        result = subprocess.run(["git", "-C", str(repository), *args], capture_output=True, encoding="utf-8", check=False)
        if result.returncode:
            raise ValueError(f"Git source lookup failed: {' '.join(args)}")
        return result.stdout

    def _snapshot(self, path):
        relative = Path(path).relative_to(self.root)
        module = relative.parts[0]
        if module not in self.repositories:
            return None
        suffix = Path(*relative.parts[1:]).as_posix()
        return self.repositories[module], f"{self.pins[module]}:{suffix}"

    def exists(self, path):
        snapshot = self._snapshot(path)
        if snapshot is None:
            return Path(path).exists()
        _, spec = snapshot
        module = Path(path).relative_to(self.root).parts[0]
        return spec.split(":", 1)[1] in self.inventory[module]

    def read(self, path):
        snapshot = self._snapshot(path)
        if snapshot is None:
            return Path(path).read_text(encoding="utf-8")
        repository, spec = snapshot
        return self._git(repository, "show", spec)

    def cs_files(self, directory):
        snapshot = self._snapshot(directory)
        if snapshot is None:
            return list(Path(directory).rglob("*.cs"))
        repository, spec = snapshot
        commit, suffix = spec.split(":", 1)
        module = Path(directory).relative_to(self.root).parts[0]
        names = self._git(repository, "ls-tree", "-r", "--name-only", commit, "--", suffix).splitlines()
        return [self.root / module / name for name in names if name.endswith(".cs")]


def source_tree_from_args():
    parser = argparse.ArgumentParser(description="Structural source validation, not runtime or deployment verification")
    parser.add_argument("--pinned-star-repository", type=Path)
    parser.add_argument("--pinned-web6-repository", type=Path)
    options = parser.parse_args()
    if bool(options.pinned_star_repository) != bool(options.pinned_web6_repository):
        parser.error("Provide both existing pinned-source repositories; no implicit source fallback is allowed")
    if not options.pinned_star_repository:
        print("Source input: parent worktree and its dependency checkouts")
        return SourceTree()
    values = {}
    for line in (ROOT / "Docker/oasis-dependency-versions.env").read_text(encoding="utf-8").splitlines():
        if line and not line.startswith("#") and "=" in line:
            key, value = line.split("=", 1)
            values[key] = value
    tree = SourceTree(repositories={"STAR ODK": options.pinned_star_repository, "WEB6": options.pinned_web6_repository},
                      pins={"STAR ODK": values["STAR_ODK_COMMIT"], "WEB6": values["OASIS_WEB6_COMMIT"]})
    print("Source input: immutable manifest/gitlink STAR and WEB6 commits from existing Git objects; other source from parent worktree")
    return tree
