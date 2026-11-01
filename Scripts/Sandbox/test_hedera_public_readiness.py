"""Boundary controls for mirror observations, without network dependencies."""
import copy
import importlib.util
import json
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location("hedera_probe", Path(__file__).with_name("Probe-HederaPublicReadiness.py"))
probe = importlib.util.module_from_spec(spec)
spec.loader.exec_module(probe)
ROW = {"account": "0.0.98", "deleted": False, "transactions": [],
       "balance": {"balance": (1 << 63) - 1, "timestamp": "1742896287.333154756"}}


class MirrorTests(unittest.TestCase):
    def test_exact_int64_and_old_observation_retained(self):
        row = probe.account(ROW, "0.0.98")
        self.assertEqual("9223372036854775807", row["balanceTinybars"])
        self.assertEqual("1742896287.333154756", row["balanceTimestamp"])
        self.assertEqual("LatestIndexed", row["consistency"])

    def test_canonical_numeric_account_id(self):
        for value in ("00.0.98", "0.0.0", "0.0.-1", "0.0.98-abcde", "0.0.18446744073709551616", "0.0.98\n"):
            with self.subTest(value=value), self.assertRaises(ValueError):
                probe.account_id(value)

    def test_tinybar_precision_and_domain(self):
        for value in (True, -1, 1 << 63, 1.5, "123"):
            with self.subTest(value=value), self.assertRaises(ValueError):
                probe.balance(value)

    def test_timestamp_exact_nine_digit_fraction(self):
        for value in (1.2, "1.2", "01.123456789", "1.1234567890", "18446744073709551616.000000000"):
            with self.subTest(value=value), self.assertRaises(ValueError):
                probe.timestamp(value)

    def test_mismatched_identity_rejected(self):
        with self.assertRaises(ValueError):
            probe.account(ROW, "0.0.2")

    def test_deleted_and_nonboolean_rejected(self):
        for value in (True, "false", 0):
            row = copy.deepcopy(ROW)
            row["deleted"] = value
            with self.subTest(value=value), self.assertRaises(ValueError):
                probe.account(row, "0.0.98")

    def test_hidden_transaction_scan_rejected(self):
        row = copy.deepcopy(ROW)
        row["transactions"] = [{}, {}]
        with self.assertRaises(ValueError):
            probe.account(row, "0.0.98")

    def test_duplicate_json_rejected(self):
        with self.assertRaises(ValueError):
            json.loads('{"account":"0.0.2","account":"0.0.3"}', object_pairs_hook=probe.unique)


if __name__ == "__main__":
    unittest.main()
