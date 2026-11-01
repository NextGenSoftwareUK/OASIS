"""Pure MultiversX network, membership, checksum and precision controls."""
import copy
import importlib.util
import json
from pathlib import Path
from urllib.parse import parse_qs, urlsplit
import unittest

spec = importlib.util.spec_from_file_location("elrond_probe", Path(__file__).with_name("Probe-ElrondPublicReadiness.py"))
probe = importlib.util.module_from_spec(spec)
spec.loader.exec_module(probe)
KEYS = ["erd1qqqqqqqqqqqqqqqpqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqplllst77y4l",
        "erd1rf4hv70arudgzus0ymnnsnc4pml0jkywg2xjvzslg0mz4nn2tg7q7k0t6p",
        "erd18mv2z6r2ksn4rfmm52tmhkc6x5tz6achmynvxftq4ay927029qqqmqpzfw"]


def rows():
    return [{"address": key, "balance": "13901850058901293848051868", "nonce": (1 << 53) - 1,
             "shard": 4294967295 if i == 0 else 0} for i, key in enumerate(KEYS)]


class ElrondReadinessTests(unittest.TestCase):
    def test_native_filter_and_complete_selection(self):
        query = parse_qs(urlsplit(probe.request_path(KEYS)).query)
        self.assertEqual([",".join(KEYS)], query["addresses"])
        self.assertEqual(["3"], query["size"])
        self.assertEqual(["address,balance,nonce,shard"], query["fields"])
        result = probe.validate(list(reversed(rows())), KEYS)
        self.assertEqual(KEYS, [row["address"] for row in result])
        self.assertEqual("9007199254740991", result[0]["nonce"])
        self.assertEqual("13901850058901293848051868", result[0]["balanceBaseUnits"])
        self.assertEqual("4294967295", result[0]["shard"])

    def test_network_denomination_is_not_display_precision(self):
        config = {"id": "mainnet", "chainId": "1", "egldDenomination": "18", "decimals": "4"}
        probe.validate_network(config)
        for field, value in (("id", "devnet"), ("chainId", "D"), ("chainId", 1), ("egldDenomination", "4")):
            changed = copy.deepcopy(config)
            changed[field] = value
            with self.subTest(field=field, value=value), self.assertRaises(ValueError):
                probe.validate_network(changed)

    def test_canonical_checksum_required(self):
        for key in KEYS:
            self.assertEqual(key, probe.address(key))
        invalid = [KEYS[0].upper(), "bad", KEYS[0][:-1] + "q", KEYS[0].replace("erd", "xyz", 1)]
        for key in invalid:
            with self.subTest(key=key), self.assertRaises(ValueError):
                probe.address(key)

    def test_missing_duplicate_and_extra_identity_fail(self):
        for fault in ("missing", "duplicate", "extra", "null"):
            changed = rows()
            if fault == "missing": changed.pop()
            if fault == "duplicate": changed[2] = copy.deepcopy(changed[0])
            if fault == "extra": changed.append(copy.deepcopy(changed[0]))
            if fault == "null": changed = None
            with self.subTest(fault=fault), self.assertRaises(ValueError):
                probe.validate(changed, KEYS)

    def test_balance_strings_are_never_coerced_or_rounded(self):
        for value in (1, 1.5, True, "-1", "01", "1e18", "1.0", "", "1" * 4097):
            changed = rows()
            changed[0]["balance"] = value
            with self.subTest(value=str(value)[:40]), self.assertRaises(ValueError):
                probe.validate(changed, KEYS)

    def test_nonce_safe_domain_is_explicit(self):
        for value in (-1, True, 1.5, "1", 1 << 53):
            changed = rows()
            changed[0]["nonce"] = value
            with self.subTest(value=value), self.assertRaises(ValueError):
                probe.validate(changed, KEYS)

    def test_shard_and_metachain_domain(self):
        for value in (-1, True, 1.0, "0", 3, 4294967296):
            changed = rows()
            changed[0]["shard"] = value
            with self.subTest(value=value), self.assertRaises(ValueError):
                probe.validate(changed, KEYS)

    def test_request_is_bounded_and_unique(self):
        for keys in ([], KEYS * 3, [KEYS[0]] * 2, ["bad"]):
            with self.subTest(keys=keys), self.assertRaises(ValueError):
                probe.request_path(keys)

    def test_duplicate_json_properties_rejected(self):
        for value in ('{"chainId":"1","chainId":"D"}', '[{"balance":"0","balance":"1"}]'):
            with self.assertRaises(ValueError):
                json.loads(value, object_pairs_hook=probe.unique)


if __name__ == "__main__":
    unittest.main()
