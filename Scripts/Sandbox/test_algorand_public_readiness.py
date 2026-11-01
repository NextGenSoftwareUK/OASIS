"""Current-state account boundary controls; no network or installations."""
import base64
import copy
import hashlib
import importlib.util
import json
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location("algorand_probe", Path(__file__).with_name("Probe-AlgorandPublicReadiness.py"))
probe = importlib.util.module_from_spec(spec)
spec.loader.exec_module(probe)
key = bytes(range(32))
ADDRESS = base64.b32encode(key + hashlib.new("sha512_256", key).digest()[-4:]).decode().rstrip("=")
ROW = {"current-round": 123, "account": {"address": ADDRESS, "round": 123, "status": "Offline",
       "amount": (1 << 64) - 1, "amount-without-pending-rewards": (1 << 64) - 2,
       "pending-rewards": 1, "rewards": 2, "min-balance": 100000}}


class AccountTests(unittest.TestCase):
    def test_exact_uint64_amount_not_float(self):
        result = probe.account(copy.deepcopy(ROW), ADDRESS)
        self.assertEqual(str((1 << 64) - 1), result["amount"])
        self.assertEqual("123", result["observedRound"])

    def test_checksum_and_canonical_address(self):
        self.assertEqual(ADDRESS, probe.address(ADDRESS))
        for value in (ADDRESS.lower(), ADDRESS + "=", "A" * 58, ADDRESS[:-1] + "A"):
            with self.subTest(value=value), self.assertRaises(ValueError):
                probe.address(value)

    def test_wrong_identity_rejected(self):
        other = bytes(32)
        other = base64.b32encode(other + hashlib.new("sha512_256", other).digest()[-4:]).decode().rstrip("=")
        with self.assertRaisesRegex(ValueError, "identity"):
            probe.account(ROW, other)

    def test_invalid_unsigned_fields_rejected(self):
        for field in ("amount", "round", "pending-rewards"):
            for value in (True, -1, 1 << 64, 1.5, "123"):
                row = copy.deepcopy(ROW)
                row["account"][field] = value
                with self.subTest(field=field, value=value), self.assertRaises(ValueError):
                    probe.account(row, ADDRESS)

    def test_inconsistent_rounds_rejected(self):
        row = copy.deepcopy(ROW)
        row["current-round"] += 1
        with self.assertRaisesRegex(ValueError, "rounds"):
            probe.account(row, ADDRESS)

    def test_deleted_unknown_status_bad_rewards_rejected(self):
        for field, value in (("deleted", True), ("status", "unknown"), ("pending-rewards", 2)):
            row = copy.deepcopy(ROW)
            row["account"][field] = value
            with self.subTest(field=field), self.assertRaises(ValueError):
                probe.account(row, ADDRESS)

    def test_both_network_endpoints_bound(self):
        versions = {"genesis_id": probe.NETWORK, "genesis_hash_b64": probe.GENESIS}
        genesis = {"genesis-id": probe.NETWORK, "genesis-hash": probe.GENESIS, "round": 0}
        probe.network(versions, genesis)
        for target, field, value in ((versions, "genesis_id", "testnet-v1.0"),
                                     (genesis, "genesis-hash", "wrong"), (genesis, "round", True)):
            left, right = copy.deepcopy(versions), copy.deepcopy(genesis)
            (left if target is versions else right)[field] = value
            with self.assertRaises(ValueError):
                probe.network(left, right)

    def test_duplicate_json_rejected(self):
        with self.assertRaises(ValueError):
            json.loads('{"round":1,"round":2}', object_pairs_hook=probe.unique)


if __name__ == "__main__":
    unittest.main()
