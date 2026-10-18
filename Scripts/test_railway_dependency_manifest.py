"""Controlled preflight boundary tests; actual checkout checks run separately."""
import subprocess
import json
import unittest
from pathlib import Path
from unittest.mock import patch

from validate_railway_dependency_manifest import checkout_failures, restored_project_failures


class RestoreProvenanceTests(unittest.TestCase):
    def setUp(self):
        self.project = Path(__file__).absolute().parent / "fixture" / "Api.csproj"

    def check(self, value):
        with patch.object(Path, "read_text", return_value=json.dumps(value)):
            return restored_project_failures(self.project)

    def test_exact_logical_root_is_accepted(self):
        self.assertEqual([], self.check({"project": {"restore": {"projectPath": str(self.project)}}}))

    def test_other_checkout_is_rejected_even_if_junction_resolves_to_same_file(self):
        other = self.project.parent.parent / "other" / "Api.csproj"
        with patch.object(Path, "resolve", return_value=self.project):
            failures = self.check({"project": {"restore": {"projectPath": str(other)}}})
        self.assertIn("cached restore belongs to", failures[0])

    def test_normalized_logical_path_is_accepted(self):
        path = str(self.project.parent / "unused" / ".." / self.project.name)
        self.assertEqual([], self.check({"project": {"restore": {"projectPath": path}}}))

    def test_missing_root_is_rejected(self):
        self.assertIn("cannot verify", self.check({})[0])

    def test_relative_root_is_rejected(self):
        self.assertIn("absolute path", self.check({"project": {"restore": {"projectPath": "Api.csproj"}}})[0])

    def test_non_string_root_is_rejected(self):
        self.assertIn("cannot verify", self.check({"project": {"restore": {"projectPath": []}}})[0])

    def test_missing_assets_are_rejected(self):
        with patch.object(Path, "read_text", side_effect=FileNotFoundError("missing assets")):
            self.assertIn("cannot verify", restored_project_failures(self.project)[0])

    def test_malformed_assets_are_rejected(self):
        with patch.object(Path, "read_text", return_value="not json"):
            self.assertIn("cannot verify", restored_project_failures(self.project)[0])


class CheckoutPreflightTests(unittest.TestCase):
    def setUp(self):
        self.path = Path(__file__).resolve().parent
        self.pin = "a" * 40

    def result(self, stdout="", code=0):
        return subprocess.CompletedProcess([], code, stdout, "")

    def check(self, identity, status):
        with patch.object(Path, "exists", return_value=True), patch(
            "validate_railway_dependency_manifest.subprocess.run", side_effect=[identity, status]
        ):
            return checkout_failures(self.path, self.pin, "fixture")

    def test_missing_submodule_never_uses_parent_git_identity(self):
        with patch.object(Path, "exists", return_value=False), patch(
            "validate_railway_dependency_manifest.subprocess.run"
        ) as run:
            self.assertIn("not initialized", checkout_failures(self.path, self.pin, "fixture")[0])
            run.assert_not_called()

    def test_clean_exact_pin_is_accepted(self):
        self.assertEqual([], self.check(self.result(f"{self.path}\n{self.pin}\n"), self.result()))

    def test_different_pin_is_rejected(self):
        failures = self.check(self.result(f"{self.path}\n{'b' * 40}\n"), self.result())
        self.assertIn("differs", failures[0])

    def test_untracked_compilable_source_is_rejected(self):
        failures = self.check(self.result(f"{self.path}\n{self.pin}\n"), self.result("?? Extra.cs\n"))
        self.assertIn("tracked/untracked", failures[0])

    def test_failed_status_is_not_treated_as_clean(self):
        failures = self.check(self.result(f"{self.path}\n{self.pin}\n"), self.result(code=1))
        self.assertIn("cleanliness", failures[0])

    def test_parent_directory_identity_is_rejected(self):
        with patch.object(Path, "exists", return_value=True), patch(
            "validate_railway_dependency_manifest.subprocess.run",
            return_value=self.result(f"{self.path.parent}\n{self.pin}\n"),
        ):
            self.assertIn("own Git", checkout_failures(self.path, self.pin, "fixture")[0])


if __name__ == "__main__":
    unittest.main()
