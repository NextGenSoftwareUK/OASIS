"""GraphQL identity, precision and complete-selection controls; no network."""
import copy
import importlib.util
import json
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location("sui_probe", Path(__file__).with_name("Probe-SuiPublicReadiness.py"))
probe = importlib.util.module_from_spec(spec)
spec.loader.exec_module(probe)
RESPONSE = {"data": {"chainIdentifier": probe.GENESIS, "genesis": {"digest": probe.GENESIS},
            "view": {"sequenceNumber": (1 << 53) - 1, "digest": probe.GENESIS,
                     "query": {"r" + str(i): {"address": key, "version": (1 << 53) - 1, "digest": probe.GENESIS}
                               for i, key in enumerate(probe.OBJECTS)}}}}


class GraphqlTests(unittest.TestCase):
    def test_exact_native_domain_and_checkpoint(self):
        rows = probe.validate(RESPONSE, probe.OBJECTS)
        self.assertEqual(3, len(rows))
        self.assertEqual("9007199254740991", rows[0]["objectVersion"])
        self.assertEqual({rows[0]["checkpointSequence"]}, {row["checkpointSequence"] for row in rows})

    def test_bounded_parameterized_read_query(self):
        request = probe.request(probe.OBJECTS)
        self.assertTrue(request["query"].startswith("query("))
        self.assertIn("view:checkpoint", request["query"])
        self.assertEqual(list(request["variables"].values()), probe.OBJECTS)
        self.assertNotIn(probe.OBJECTS[0], request["query"])
        for keys in ([], probe.OBJECTS * 3, [probe.OBJECTS[0]] * 2, ["0x5"]):
            with self.subTest(keys=keys), self.assertRaises(ValueError):
                probe.request(keys)

    def test_partial_graphql_errors_rejected(self):
        for errors in ([{"message": "failed"}], []):
            changed = copy.deepcopy(RESPONSE)
            changed["errors"] = errors
            with self.assertRaises(ValueError):
                probe.validate(changed, probe.OBJECTS)

    def test_both_network_identities_required(self):
        for field in ("chainIdentifier", "genesis"):
            changed = copy.deepcopy(RESPONSE)
            changed["data"][field] = "1" * 32 if field == "chainIdentifier" else {"digest": "1" * 32}
            with self.subTest(field=field), self.assertRaises(ValueError):
                probe.validate(changed, probe.OBJECTS)

    def test_missing_extra_or_null_object_rejected(self):
        for fault in ("missing", "extra", "null", "identity"):
            changed = copy.deepcopy(RESPONSE)
            rows = changed["data"]["view"]["query"]
            if fault == "missing": del rows["r2"]
            if fault == "extra": rows["r3"] = rows["r0"]
            if fault == "null": rows["r2"] = None
            if fault == "identity": rows["r2"]["address"] = probe.OBJECTS[0]
            with self.subTest(fault=fault), self.assertRaises(ValueError):
                probe.validate(changed, probe.OBJECTS)

    def test_uint53_rejects_bool_float_overflow_string(self):
        for value in (True, -1, 1 << 53, 1.5, "123"):
            with self.subTest(value=value), self.assertRaises(ValueError):
                probe.uint53(value)

    def test_canonical_base58_width_and_addresses(self):
        for value in ("0" * 44, "1" * 31, "z" * 44, probe.GENESIS + "\n"):
            with self.subTest(value=value), self.assertRaises(ValueError):
                probe.digest(value)
        for value in ("0x5", "0X" + "0" * 64, "0x" + "A" * 64):
            with self.subTest(value=value), self.assertRaises(ValueError):
                probe.address(value)

    def test_duplicate_json_rejected(self):
        with self.assertRaises(ValueError):
            json.loads('{"data":{},"data":{}}', object_pairs_hook=probe.unique)


if __name__ == "__main__":
    unittest.main()
