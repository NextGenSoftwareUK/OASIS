import copy
import importlib.util
import json
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location("reconcile", Path(__file__).with_name("Prepare-XrplFreeEndpointReconciliation.py"))
reconcile = importlib.util.module_from_spec(spec)
spec.loader.exec_module(reconcile)


class EndpointTests(unittest.TestCase):
    def fixture(self):
        source = {"schema": 1, "rpcUrl": "https://xrplcluster.com/", "networkId": 0,
                  "ledgerHashes": ["A" * 64, "B" * 64, "C" * 64],
                  "sourceOwner": "00000000-0000-0000-0000-000000000123"}
        before = {reconcile.KEY: json.dumps(source), "OASIS_DNA_JSON": "unchanged-wallet-dna",
                  "CQL_HOSTED_QUERY_EXAMPLES__XRPLOASIS": "unchanged-presets",
                  "PREPARED_STORAGE_ADMISSION_JSON": json.dumps({"queryOnlyProviders": ["XRPLOASIS"],
                    "ordinaryProviders": ["MongoDBOASIS"], "nativeProviders": [],
                    "queryTariffs": [{"provider": "XRPLOASIS", "costUsd": 0}]})}
        replacement = dict(source, rpcUrl="https://s2.ripple.com:51234/")
        return before, replacement

    def test_only_endpoint_changes_without_mutating_inputs(self):
        before, replacement = self.fixture()
        snapshot = copy.deepcopy((before, replacement))
        after = reconcile.candidate(before, replacement)
        self.assertEqual(snapshot, (before, replacement))
        self.assertEqual({reconcile.KEY}, {k for k in after if before[k] != after[k]})
        self.assertEqual(replacement, json.loads(after[reconcile.KEY]))

    def test_rejects_identity_schema_or_unqualified_endpoint_changes(self):
        for key, value in (("sourceOwner", "00000000-0000-0000-0000-000000000456"),
                           ("ledgerHashes", ["D" * 64]), ("networkId", 1), ("networkId", False),
                           ("schema", True), ("rpcUrl", "https://different.invalid/"), ("privateKey", "forbidden")):
            before, replacement = self.fixture()
            replacement[key] = value
            with self.subTest(key=key, value=value), self.assertRaises(ValueError):
                reconcile.candidate(before, replacement)

    def test_rejects_paid_or_overlapping_admission(self):
        for fault in ("paid", "ordinary", "native", "missing"):
            before, replacement = self.fixture()
            admission = json.loads(before["PREPARED_STORAGE_ADMISSION_JSON"])
            if fault == "paid": admission["queryTariffs"][0]["costUsd"] = 1
            if fault == "ordinary": admission["ordinaryProviders"].append("XRPLOASIS")
            if fault == "native": admission["nativeProviders"].append({"provider": "XRPLOASIS"})
            if fault == "missing": admission["queryOnlyProviders"] = []
            before["PREPARED_STORAGE_ADMISSION_JSON"] = json.dumps(admission)
            with self.subTest(fault=fault), self.assertRaises(ValueError):
                reconcile.candidate(before, replacement)


if __name__ == "__main__":
    unittest.main()
