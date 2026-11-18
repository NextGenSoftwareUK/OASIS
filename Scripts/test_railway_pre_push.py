#!/usr/bin/env python3
"""Exercise the real pre-push hook against owned local Git repositories; no network or provider access."""
from __future__ import annotations
import argparse
import io
import os
import re
import runpy
import shlex
import shutil
import subprocess
import sys
import tarfile
import tempfile
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

class ArtifactPrePushTests(unittest.TestCase):
    hook_source: str
    source_ref: str

    @classmethod
    def setUpClass(cls):
        names = ["Scripts/validate_railway_dependency_manifest.py",
                 "Scripts/sync_railway_dependency_manifest.py",
                 "Docker/oasis-dependency-versions.env",
                 *[f"Docker/Dockerfile.web{n}" for n in range(4, 11)]]
        archive = subprocess.check_output(["git", "-C", str(ROOT), "archive", "--format=tar",
                                           cls.source_ref, *names])
        cls.temporary = tempfile.TemporaryDirectory(prefix="cosmic-prepush-fixture-")
        cls.base = Path(cls.temporary.name).resolve()
        assert cls.base.parent == Path(tempfile.gettempdir()).resolve()
        assert cls.base.name.startswith("cosmic-prepush-fixture-")
        cls.addClassCleanup(cls.temporary.cleanup)
        cls.repo = cls.base / "source"
        cls.remote = cls.base / "remote.git"
        cls.repo.mkdir()
        cls.git("init", "-b", "fixture")
        cls.git("config", "user.name", "OASIS fixture")
        cls.git("config", "user.email", "fixture@example.invalid")
        cls.git("config", "commit.gpgsign", "false")
        cls.git("config", "core.autocrlf", "false")
        cls.git("config", "core.fsmonitor", "false")
        cls.git("config", "core.hooksPath", ".githooks")
        cls.git("commit", "--allow-empty", "-m", "Legacy tree without deployment inputs")
        cls.legacy = cls.git("rev-parse", "HEAD")
        with tarfile.open(fileobj=io.BytesIO(archive)) as bundle:
            for name in names:
                target = cls.repo / name
                target.parent.mkdir(parents=True, exist_ok=True)
                target.write_bytes(bundle.extractfile(name).read())
        hook = cls.repo / ".githooks/pre-push"
        hook.parent.mkdir()
        hook.write_text(cls.hook_source, encoding="utf-8", newline="\n")
        hook.chmod(0o755)
        cls.manifest = cls.repo / "Docker/oasis-dependency-versions.env"
        cls.original = cls.manifest.read_text(encoding="utf-8")
        cls.corrupt = re.sub(r"(?m)^OASIS_API_CORE_COMMIT=[0-9a-f]{40}$",
                            "OASIS_API_CORE_COMMIT=" + "f" * 40, cls.original, count=1)
        assert cls.corrupt != cls.original
        cls.git("add", ".")
        cls.git("update-index", "--chmod=+x", ".githooks/pre-push")
        mapping = runpy.run_path(str(cls.repo / "Scripts/validate_railway_dependency_manifest.py"))["GITLINKS"]
        for key, path in mapping.items():
            sha = re.search(r"(?m)^" + re.escape(key) + r"=([0-9a-f]{40})$", cls.original).group(1)
            cls.git("update-index", "--add", "--cacheinfo", "160000," + sha + "," + path)
        cls.git("commit", "-m", "Consistent deployment inputs")
        cls.good = cls.git("rev-parse", "HEAD")
        cls.git("tag", "-a", "qualified", "-m", "Qualified artifact", cls.good)
        cls.manifest.write_text(cls.corrupt, encoding="utf-8", newline="\n")
        cls.git("add", "Docker/oasis-dependency-versions.env")
        cls.git("commit", "-m", "Inconsistent deployment inputs")
        cls.bad = cls.git("rev-parse", "HEAD")
        subprocess.run(["git", "init", "--bare", str(cls.remote)], check=True, capture_output=True)

    @classmethod
    def git(cls, *args):
        result = subprocess.run(["git", "-C", str(cls.repo), *args], text=True,
                                capture_output=True, env=dict(os.environ, PYTHONDONTWRITEBYTECODE="1"))
        if result.returncode:
            raise AssertionError("Local fixture Git command failed: " + result.stderr[-2000:])
        return result.stdout.strip()

    def setUp(self):
        self.git("update-ref", "HEAD", self.bad)
        self.manifest.write_text(self.corrupt, encoding="utf-8", newline="\n")

    def push(self, *refs):
        return subprocess.run(["git", "-C", str(self.repo), "push", str(self.remote), *refs],
                              text=True, capture_output=True,
                              env=dict(os.environ, PYTHONDONTWRITEBYTECODE="1"))

    def invoke_without_python(self, oid, deletion=False):
        tools = self.base / "git-only-path"
        tools.mkdir(exist_ok=True)
        binary = shutil.which("git")
        shell = shutil.which("sh")
        if shell is None:
            shell_path = Path(binary).parent.parent / "bin/sh.exe"
            self.assertTrue(shell_path.is_file(), "The installed Git shell is required.")
            shell = str(shell_path)
        shim = tools / "git"
        shim.write_text("#!/bin/sh\nexec " + shlex.quote(binary.replace("\\", "/")) + " \"$@\"\n",
                        encoding="utf-8", newline="\n")
        shim.chmod(0o755)
        target = "refs/heads/no-python"
        local = "0000000000000000000000000000000000000000" if deletion else oid
        line = target + " " + local + " " + target + " " + "0" * 40 + "\n"
        return subprocess.run([shell, "-c", self.hook_source], cwd=self.repo, input=line,
                              text=True, capture_output=True,
                              env=dict(os.environ, PATH=str(tools), PYTHONDONTWRITEBYTECODE="1"))

    def test_no_python_deletion_remains_outside_deployment_policy(self):
        result = self.invoke_without_python(self.good, deletion=True)
        self.assertEqual(0, result.returncode, result.stdout + result.stderr)

    def test_no_python_legacy_tree_remains_outside_deployment_policy(self):
        result = self.invoke_without_python(self.legacy)
        self.assertEqual(0, result.returncode, result.stdout + result.stderr)

    def test_no_python_deployment_snapshot_is_refused(self):
        result = self.invoke_without_python(self.good)
        self.assertNotEqual(0, result.returncode)
        self.assertIn("Python is required", result.stderr)

    def test_valid_snapshot_with_invalid_current_head_and_manifest(self):
        result = self.push(self.good + ":refs/heads/valid-artifact")
        self.assertEqual(0, result.returncode, result.stdout + result.stderr)

    def test_invalid_snapshot_with_valid_current_head_and_manifest(self):
        self.git("update-ref", "HEAD", self.good)
        self.manifest.write_text(self.original, encoding="utf-8", newline="\n")
        result = self.push(self.bad + ":refs/heads/invalid-artifact")
        self.assertNotEqual(0, result.returncode)
        self.assertIn("dependency verification failed", result.stdout + result.stderr)

    def test_every_ref_is_checked_before_any_transfer(self):
        result = self.push(self.good + ":refs/heads/batch-good", self.bad + ":refs/heads/batch-bad")
        self.assertNotEqual(0, result.returncode)
        refs = subprocess.run(["git", "--git-dir=" + str(self.remote), "show-ref", "--verify",
                               "refs/heads/batch-good"], capture_output=True)
        self.assertNotEqual(0, refs.returncode)

    def test_deletion_has_no_deployment_snapshot_to_verify(self):
        created = self.push(self.good + ":refs/heads/delete-me")
        self.assertEqual(0, created.returncode, created.stderr)
        deleted = self.push(":refs/heads/delete-me")
        self.assertEqual(0, deleted.returncode, deleted.stderr)

    def test_annotated_tag_validates_its_commit(self):
        result = self.push("refs/tags/qualified:refs/tags/qualified")
        self.assertEqual(0, result.returncode, result.stdout + result.stderr)

    def test_legacy_tree_without_deployment_inputs(self):
        result = self.push(self.legacy + ":refs/heads/legacy")
        self.assertEqual(0, result.returncode, result.stdout + result.stderr)

def run(hook_source: str, source_ref: str) -> int:
    ArtifactPrePushTests.hook_source = hook_source
    ArtifactPrePushTests.source_ref = source_ref
    suite = unittest.defaultTestLoader.loadTestsFromTestCase(ArtifactPrePushTests)
    result = unittest.TextTestRunner(verbosity=2).run(suite)
    root = getattr(ArtifactPrePushTests, "base", None)
    if root is not None:
        print("PREPUSH_FIXTURE_PATH=" + str(root))
        print("PREPUSH_FIXTURE_CLEANUP_VERIFIED=" + str(not root.exists()).lower())
        if root.exists():
            return 1
    return 0 if result.wasSuccessful() else 1

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--source-ref", default="HEAD")
    options = parser.parse_args()
    sys.exit(run((ROOT / ".githooks/pre-push").read_text(encoding="utf-8"), options.source_ref))
