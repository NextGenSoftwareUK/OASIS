"""Offline Tableland version-contract controls; no native requests."""
import importlib.util
import json
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location("tableland_probe",
    Path(__file__).with_name("Probe-TablelandPublicReadiness.py"))
probe = importlib.util.module_from_spec(spec)
spec.loader.exec_module(probe)


class TablelandProbeTests(unittest.TestCase):
    def valid(self):
        return {key: "reported-value" for key in
            ("git_commit", "git_branch", "git_state", "git_summary", "build_date", "binary_version")}

    def test_documented_version_whitelists_fields(self):
        value = self.valid()
        value["unrelated"] = "excluded"
        self.assertEqual(probe.parse_version(json.dumps(value).encode()), self.valid())

    def test_error_or_incomplete_body_is_not_readiness(self):
        for value in ({"error": "unavailable"}, [], self.valid() | {"git_commit": None}):
            with self.subTest(value=value), self.assertRaises(ValueError):
                probe.parse_version(json.dumps(value).encode())

    def test_duplicate_fields_are_rejected(self):
        raw = json.dumps(self.valid())[:-1] + ',"git_commit":"duplicate"}'
        with self.assertRaises(ValueError):
            probe.parse_version(raw.encode())

    def test_body_limit_and_redirect_policy(self):
        with self.assertRaises(ValueError):
            probe.parse_version(b" " * (probe.LIMIT + 1))
        self.assertIsNone(probe.NoRedirect().redirect_request(None, None, 302, "", {}, "https://other.invalid/"))


if __name__ == "__main__":
    unittest.main()
