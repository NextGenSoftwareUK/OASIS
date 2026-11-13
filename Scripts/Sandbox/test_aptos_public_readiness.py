"""Account/version boundary tests, no network or installations."""
import copy
import importlib.util
import json
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location("aptos_probe", Path(__file__).with_name("Probe-AptosPublicReadiness.py"))
probe = importlib.util.module_from_spec(spec)
spec.loader.exec_module(probe)
HEADERS = {"x-aptos-chain-id": "1", "x-aptos-ledger-version": "18446744073709551615",
           "x-aptos-ledger-oldest-version": "0"}
BODY = {"sequence_number": "18446744073709551615", "authentication_key": "0x" + "a" * 64}


class AccountTests(unittest.TestCase):
    def test_exact_u64_and_requested_version(self):
        row = probe.account(BODY, HEADERS, probe.ACCOUNTS[0], (1 << 64) - 1)
        self.assertEqual("18446744073709551615", row["sequenceNumber"])
        self.assertEqual(row["sequenceNumber"], row["selectedLedgerVersion"])

    def test_noncanonical_unsigned_rejected(self):
        for value in (True, 123, -1, "-1", "01", "1.0", "18446744073709551616", "1\n"):
            with self.subTest(value=value), self.assertRaises(ValueError):
                probe.uint64(value)

    def test_canonical_address_and_authentication_key(self):
        for value in ("0x1", "0X" + "a" * 64, "0x" + "A" * 64, "0x" + "g" * 64):
            with self.subTest(value=value), self.assertRaises(ValueError):
                probe.hex32(value)

    def test_wrong_network_or_pruned_version_rejected(self):
        for key, value, selected in (("x-aptos-chain-id", "2", 100),
                                     ("x-aptos-ledger-oldest-version", "101", 100),
                                     ("x-aptos-ledger-version", "99", 100)):
            headers = dict(HEADERS)
            headers[key] = value
            with self.subTest(key=key), self.assertRaises(ValueError):
                probe.chain(headers, selected)

    def test_ledger_body_and_headers_must_agree(self):
        body = {"chain_id": 1, "ledger_version": HEADERS["x-aptos-ledger-version"], "oldest_ledger_version": "0"}
        self.assertEqual((1 << 64) - 1, probe.ledger(body, HEADERS))
        for key, value in (("chain_id", True), ("ledger_version", "100"), ("oldest_ledger_version", "1")):
            changed = dict(body)
            changed[key] = value
            with self.subTest(key=key), self.assertRaises(ValueError):
                probe.ledger(changed, HEADERS)

    def test_account_extra_fields_and_bad_native_values_rejected(self):
        for key, value in (("extra", "unexpected"), ("sequence_number", 1), ("authentication_key", "0x1")):
            changed = copy.deepcopy(BODY)
            changed[key] = value
            with self.subTest(key=key), self.assertRaises(ValueError):
                probe.account(changed, HEADERS, probe.ACCOUNTS[0], 100)

    def test_duplicate_json_rejected(self):
        with self.assertRaises(ValueError):
            json.loads('{"sequence_number":"1","sequence_number":"2"}', object_pairs_hook=probe.unique)


if __name__ == "__main__":
    unittest.main()
