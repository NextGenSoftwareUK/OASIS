import io
import importlib.util
from pathlib import Path
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location("lens_probe", Path(__file__).with_name("Probe-LensPublicReadiness.py"))
probe = importlib.util.module_from_spec(spec)
spec.loader.exec_module(probe)


class LensQualificationTests(unittest.TestCase):
    def row(self, digit="a"):
        return {"address": "0x" + digit * 40, "owner": "0x" + "b" * 40,
                "username": {"value": "lens/demo"}, "metadata": {"name": "Coffee ☕", "bio": None}}

    def test_projection_preserves_nullable_data_and_normalizes_identity(self):
        row = self.row("A")
        parsed = probe.account(row)
        self.assertEqual("0x" + "a" * 40, parsed["address"])
        self.assertEqual("Coffee ☕", parsed["metadata"]["name"])
        row.update(username=None, metadata=None)
        self.assertIsNone(probe.account(row)["metadata"])

    def test_partial_graphql_data_is_never_success(self):
        for value in ({"data": {"accountsBulk": [self.row()]}, "errors": [{"message": "failed"}]},
                      {"data": None}, {}, {"data": {"accountsBulk": None}},
                      {"data": {"accountsBulk": []}}):
            with self.subTest(value=value), self.assertRaises(ValueError):
                probe.rows(value, "accountsBulk")

    def test_missing_or_malformed_selected_fields_fail(self):
        for field in self.row():
            row = self.row(); del row[field]
            with self.subTest(field=field), self.assertRaises(ValueError): probe.account(row)
        for field, value in (("owner", True), ("address", "0x12"), ("username", {}),
                             ("metadata", {"name": "x"}), ("metadata", {"name": 2, "bio": None})):
            row = self.row(); row[field] = value
            with self.subTest(field=field, value=value), self.assertRaises(ValueError): probe.account(row)

    def test_exact_bulk_key_set_and_duplicates(self):
        key = self.row()["address"]
        self.assertEqual(1, len(probe.rows({"data": {"accountsBulk": [self.row()]}}, "accountsBulk", [key])))
        for items, expected in (([self.row(), self.row()], [key]),
                                ([self.row()], [key, "0x" + "c" * 40]),
                                ([self.row("c")], [key])):
            with self.subTest(expected=expected), self.assertRaises(ValueError):
                probe.rows({"data": {"accountsBulk": items}}, "accountsBulk", expected)

    def test_discovery_bound_and_nullable_text_limits(self):
        with self.assertRaises(ValueError):
            probe.rows({"data": {"accounts": {"items": [self.row()] * 11}}}, "accounts")
        row = self.row(); row["metadata"]["bio"] = "☕" * 6000
        with self.assertRaises(ValueError): probe.account(row)

    def test_duplicate_json_and_redirect_rejected(self):
        with self.assertRaises(ValueError): probe.unique([("data", {}), ("data", {})])
        with self.assertRaises(ValueError): probe.NoRedirect().redirect_request(None)

    def test_byte_and_elapsed_budget_fail_before_acceptance(self):
        self.assertEqual(b"{}", probe.read_bounded(io.BytesIO(b"{}"), probe.time.monotonic()))
        with self.assertRaises(ValueError):
            probe.read_bounded(io.BytesIO(b"x" * (probe.BYTE_LIMIT + 1)), probe.time.monotonic())
        with patch.object(probe.time, "monotonic", return_value=16), self.assertRaises(ValueError):
            probe.read_bounded(io.BytesIO(b"{}"), 0)


if __name__ == "__main__":
    unittest.main()
