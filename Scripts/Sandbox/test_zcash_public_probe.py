import importlib.util
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location("zec_probe", Path(__file__).with_name("Validate-ZcashPublicReadiness.py"))
probe = importlib.util.module_from_spec(spec)
spec.loader.exec_module(probe)


class ZcashReadinessTests(unittest.TestCase):
    def fixture(self):
        return {"data": [{"height": 0, "hash": probe.GENESIS, "timestamp": 1477641360, "coinbase_hex": "excluded"}],
                "meta": {"network": "mainnet", "freshness": {"status": "fresh"},
                         "units": {"authoritativeMonetary": "zatoshi", "authoritativeEncoding": "decimal-string", "zatoshiPerZec": "100000000"}}}

    def test_mainnet_genesis_and_metadata_only_projection(self):
        result = probe.validate(self.fixture(), [0], True)
        self.assertEqual("0", result["rows"][0]["height"])
        self.assertNotIn("coinbase_hex", result["rows"][0])
        self.assertFalse(result["proofVerified"])

    def test_wrong_network_genesis_units_and_unavailable_fail(self):
        for fault in ("network", "genesis", "units", "unavailable"):
            row = self.fixture()
            if fault == "network": row["meta"]["network"] = "testnet"
            if fault == "genesis": row["data"][0]["hash"] = "0" * 64
            if fault == "units": row["meta"]["units"]["zatoshiPerZec"] = 100000000
            if fault == "unavailable": row["meta"]["freshness"]["status"] = "unavailable"
            with self.subTest(fault=fault), self.assertRaises(ValueError): probe.validate(row, [0], True)

    def test_missing_duplicate_or_wrong_selected_identity_fails(self):
        for items in ([], self.fixture()["data"] * 2, [{"height": 1, "hash": probe.GENESIS, "timestamp": 1}]):
            value = self.fixture(); value["data"] = items
            with self.subTest(items=items), self.assertRaises(ValueError): probe.validate(value, [0])

    def test_error_bodies_private_payload_and_lossy_numbers_fail(self):
        with self.assertRaises(ValueError): probe.validate({"status": 503}, [0])
        for value in (True, 1.5, "0", -1, 2147483648):
            row = self.fixture(); row["data"][0]["height"] = value
            with self.subTest(value=value), self.assertRaises(ValueError): probe.validate(row, [0])
        row = self.fixture(); row["data"][0]["transactions"] = []
        with self.assertRaises(ValueError): probe.validate(row, [0])

    def test_three_and_two_record_sets_preserve_native_identity(self):
        value = self.fixture()
        value["data"] = [{"height": n, "hash": str(n) * 64, "timestamp": 1477641360 + n} for n in (1, 2, 3)]
        self.assertEqual(3, len(probe.validate(value, [1, 2, 3])["rows"]))
        value["data"] = value["data"][:2]
        self.assertEqual(2, len(probe.validate(value, [1, 2])["rows"]))

    def test_duplicate_json_rejected(self):
        with self.assertRaises(ValueError): probe.unique([("data", []), ("data", [])])

    def test_native_uint32_timestamp_preserved_beyond_signed_int32(self):
        value = self.fixture(); value["data"][0]["timestamp"] = 4294967295
        self.assertEqual("4294967295", probe.validate(value, [0])["rows"][0]["timestamp"])
        value["data"][0]["timestamp"] += 1
        with self.assertRaises(ValueError): probe.validate(value, [0])


if __name__ == "__main__":
    unittest.main()
