import importlib.util
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location("aztec_probe", Path(__file__).with_name("Probe-AztecPublicReadiness.py"))
probe = importlib.util.module_from_spec(spec)
spec.loader.exec_module(probe)


class AztecProbeTests(unittest.TestCase):
    def identity(self):
        return {"nodeVersion": "6.0.0", "l1ChainId": 1, "rollupVersion": probe.ROLLUP_VERSION,
                "realProofs": True, "l1ContractAddresses": {"rollupAddress": probe.ROLLUP, "registryAddress": probe.REGISTRY}}

    def record(self):
        return {"number": 42, "hash": "0x" + "1" * 64, "checkpointNumber": 4, "indexWithinCheckpoint": 2,
                "archive": {"root": "0x" + "2" * 64, "nextAvailableLeafIndex": 43},
                "header": {"globalVariables": {"blockNumber": 42},
                           "lastArchive": {"root": "0x" + "3" * 64, "nextAvailableLeafIndex": 42}}}

    def test_pinned_mainnet_without_independent_proof_claim(self):
        result = probe.identity(self.identity())
        self.assertFalse(result["proofVerified"])
        self.assertEqual(probe.ROLLUP, result["rollupAddress"])

    def test_wrong_network_release_or_rollup_fails(self):
        for field, value in (("l1ChainId", True), ("l1ChainId", 11155111), ("nodeVersion", "5.2.0"),
                             ("rollupVersion", probe.ROLLUP_VERSION + 1), ("realProofs", False)):
            row = self.identity(); row[field] = value
            with self.subTest(field=field), self.assertRaises(ValueError): probe.identity(row)
        row = self.identity(); row["l1ContractAddresses"]["rollupAddress"] = "0x" + "0" * 40
        with self.assertRaises(ValueError): probe.identity(row)

    def test_project_only_metadata_and_preserve_exact_numbers(self):
        parsed = probe.block(self.record(), 42)
        self.assertEqual("42", parsed["number"])
        self.assertNotIn("header", parsed)
        self.assertEqual(9007199254740991, probe.integer(9007199254740991))

    def test_bodies_wrong_numbers_missing_fields_and_hashes_fail(self):
        for fault in ("body", "outer", "header", "hash", "missing"):
            row = self.record()
            if fault == "body": row["body"] = {"txEffects": []}
            if fault == "outer": row["number"] = 41
            if fault == "header": row["header"]["globalVariables"]["blockNumber"] = 41
            if fault == "hash": row["hash"] = "0xAB"
            if fault == "missing": del row["archive"]
            with self.subTest(fault=fault), self.assertRaises(ValueError): probe.block(row, 42)

    def test_rpc_errors_wrong_id_and_duplicate_json_fail(self):
        for row in ({"jsonrpc": "2.0", "id": True, "result": {}},
                    {"jsonrpc": "2.0", "id": 2, "result": {}},
                    {"jsonrpc": "2.0", "id": 1, "result": {}, "error": {}}, {}):
            with self.subTest(row=row), self.assertRaises(ValueError): probe.envelope(row, 1)
        with self.assertRaises(ValueError): probe.unique([("id", 1), ("id", 2)])

    def test_fractional_boolean_or_unsafe_integer_rejected(self):
        for value in (True, 42.0, -1, 9007199254740992, "42"):
            with self.subTest(value=value), self.assertRaises(ValueError): probe.integer(value)


if __name__ == "__main__":
    unittest.main()
