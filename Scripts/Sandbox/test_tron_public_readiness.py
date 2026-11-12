"""Pure TRON identity, completeness and precision controls; no network."""
import importlib.util
import json
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location("tron_probe", Path(__file__).with_name("Probe-TronPublicReadiness.py"))
probe = importlib.util.module_from_spec(spec)
spec.loader.exec_module(probe)


def header(height=1):
    return {"blockID": format(height, "016x") + "a" * 48,
            "block_header": {"raw_data": {"number": height, "timestamp": (1 << 63) - 1,
                "parentHash": probe.GENESIS, "txTrieRoot": "0" * 64,
                "witness_address": "41" + "a" * 40}}}


class TronReadinessTests(unittest.TestCase):
    def test_exact_precision_and_read_only_selection(self):
        row = probe.validate(header(), 1)
        self.assertEqual("9223372036854775807", row["timestampMilliseconds"])
        self.assertEqual("1", row["height"])
        self.assertEqual({"id_or_num": "1", "detail": False}, probe.request(1))

    def test_invalid_heights_rejected(self):
        for value in (True, -1, 1 << 63, 1.0, "1", None):
            with self.subTest(value=value), self.assertRaises(ValueError):
                probe.request(value)

    def test_genesis_zero_omission_is_explicit_and_network_bound(self):
        genesis = {"blockID": probe.GENESIS, "block_header": {"raw_data": {}}}
        self.assertEqual("0", probe.validate(genesis, 0)["height"])
        genesis["block_header"]["raw_data"]["number"] = 0
        self.assertEqual(probe.GENESIS, probe.validate(genesis, 0)["blockId"])
        for value in (True, 1, "0"):
            genesis["block_header"]["raw_data"]["number"] = value
            with self.subTest(value=value), self.assertRaises(ValueError):
                probe.validate(genesis, 0)
        genesis["blockID"] = "0" * 64
        with self.assertRaises(ValueError):
            probe.validate(genesis, 0)

    def test_ordinary_missing_fields_are_not_defaulted(self):
        for key in ("number", "timestamp", "parentHash", "txTrieRoot", "witness_address"):
            changed = header()
            del changed["block_header"]["raw_data"][key]
            with self.subTest(key=key), self.assertRaises(KeyError):
                probe.validate(changed, 1)

    def test_error_and_transaction_bodies_fail_closed(self):
        for key, value in (("Error", "failed"), ("Error", ""), ("transactions", [])):
            changed = header()
            changed[key] = value
            with self.subTest(key=key), self.assertRaises(ValueError):
                probe.validate(changed, 1)

    def test_request_header_and_block_id_must_agree(self):
        for target in ("id", "number"):
            changed = header()
            if target == "id":
                changed["blockID"] = "0000000000000002" + "a" * 48
            else:
                changed["block_header"]["raw_data"]["number"] = 2
            with self.subTest(target=target), self.assertRaises(ValueError):
                probe.validate(changed, 1)

    def test_numeric_native_domains_reject_coercion(self):
        for key in ("number", "timestamp"):
            for value in (True, -1, 1 << 63, 1.5, "1"):
                changed = header()
                changed["block_header"]["raw_data"][key] = value
                with self.subTest(key=key, value=value), self.assertRaises(ValueError):
                    probe.validate(changed, 1)

    def test_hash_and_witness_encodings(self):
        for key in ("parentHash", "txTrieRoot", "witness_address"):
            for value in (None, "bad", "A" * 64, "0x" + "a" * 64):
                changed = header()
                changed["block_header"]["raw_data"][key] = value
                with self.subTest(key=key, value=value), self.assertRaises(ValueError):
                    probe.validate(changed, 1)

    def test_duplicate_json_is_rejected_at_every_depth(self):
        for value in ('{"blockID":1,"blockID":2}', '{"block_header":{"number":1,"number":2}}'):
            with self.assertRaises(ValueError):
                json.loads(value, object_pairs_hook=probe.unique)


if __name__ == "__main__":
    unittest.main()
