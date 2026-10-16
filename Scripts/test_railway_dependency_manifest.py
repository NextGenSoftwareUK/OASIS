"""Controlled preflight boundary tests; actual checkout checks run separately."""
import subprocess
import unittest
from pathlib import Path
from unittest.mock import patch

from validate_railway_dependency_manifest import checkout_failures


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
