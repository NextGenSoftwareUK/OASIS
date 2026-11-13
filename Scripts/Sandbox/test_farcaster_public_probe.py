import copy
import importlib.util
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location("farcaster_probe", Path(__file__).with_name("Probe-FarcasterPublicReadiness.py"))
probe = importlib.util.module_from_spec(spec)
spec.loader.exec_module(probe)


class FarcasterProbeTests(unittest.TestCase):
    def fixture(self):
        return {"data": {"fid": 1, "type": "MESSAGE_TYPE_USER_DATA_ADD", "network": "FARCASTER_NETWORK_MAINNET",
                         "timestamp": 4294967295, "userDataBody": {"type": "USER_DATA_TYPE_DISPLAY", "value": "Public ☕"}}}

    def test_exact_native_identity_and_uint32_preserved_without_signature_claim(self):
        row = probe.validate(self.fixture(), 1)
        self.assertEqual("4294967295", row["timestamp"])
        self.assertEqual("1", row["fid"])
        self.assertFalse(row["signatureVerified"])

    def test_wrong_identity_types_and_partial_data_rejected(self):
        for field, value in (("fid", 2), ("fid", True), ("fid", 1.0), ("timestamp", -1),
                             ("timestamp", 4294967296), ("timestamp", True),
                             ("network", "FARCASTER_NETWORK_TESTNET"), ("type", "MESSAGE_TYPE_CAST_ADD")):
            with self.subTest(field=field, value=value):
                row = copy.deepcopy(self.fixture()); row["data"][field] = value
                with self.assertRaises(ValueError): probe.validate(row, 1)
        with self.assertRaises(ValueError): probe.validate({}, 1)

    def test_different_user_data_kind_or_unbounded_text_rejected(self):
        for body in ({"type": "USER_DATA_TYPE_BIO", "value": "bio"},
                     {"type": "USER_DATA_TYPE_DISPLAY", "value": None},
                     {"type": "USER_DATA_TYPE_DISPLAY", "value": "x" * 4097}):
            row = self.fixture(); row["data"]["userDataBody"] = body
            with self.assertRaises(ValueError): probe.validate(row, 1)

    def test_duplicate_json_fields_rejected(self):
        with self.assertRaises(ValueError): probe.unique([("data", {}), ("data", {})])


if __name__ == "__main__":
    unittest.main()
