"""Focused reconciliation controls; no network, credentials or real fixtures."""
import copy
import hashlib
import importlib.util
import json
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location("eclipse_reconciliation", Path(__file__).with_name("Reconcile-HostedEclipseBaseline.py"))
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class EclipseReconciliationTests(unittest.TestCase):
    def fixture(self):
        before = {"OASIS_DNA_JSON": json.dumps({"OASIS": {"Sandbox": {"Enabled": True}}}),
                  "PREPARED_STORAGE_ADMISSION_JSON": json.dumps({"queryOnlyProviders": ["IPFSOASIS"],
                      "ordinaryProviders": ["MongoDBOASIS"], "nativeProviders": [], "storageTariffs": [],
                      "queryTariffs": [{"provider": "IPFSOASIS", "costUsd": 0, "catalogueVersion": "test-v1"}]}),
                  "CQL_HOSTED_QUERY_EXAMPLES_JSON": "{}", "UNRELATED": "retained"}
        eclipse = {"schema": 1, "rpcUrl": "https://mainnetbeta-rpc.eclipse.xyz/",
                   "addresses": module.eclipse.EXPECTED_KEYS.copy(), "genesisHash": module.eclipse.EXPECTED_GENESIS,
                   "sourceOwner": "00000000-0000-0000-0000-000000000123"}
        akash = {"schema": 1, "apiUrl": "https://console-api.akash.network/",
                 "providerOwners": module.akash.EXPECTED_KEYS.copy(),
                 "sourceOwner": "00000000-0000-0000-0000-000000000456"}
        original = module.eclipse.candidate(before, eclipse)
        acknowledged = module.akash.candidate(before, akash)
        acks = [{"key": key, "sha256": hashlib.sha256(acknowledged[key].encode()).hexdigest(),
                 "verified": True, "deploymentTriggered": False} for key in module.akash.KEYS]
        return [before, original, copy.deepcopy(before), acknowledged, copy.deepcopy(acknowledged), eclipse, akash, acks]

    def test_only_original_eclipse_changes_extend_acknowledged_akash_baseline(self):
        values = self.fixture()
        frozen = copy.deepcopy(values)
        result = module.reconcile(*values)
        self.assertEqual(values, frozen)
        self.assertEqual({k for k in result.keys() | values[4].keys() if result.get(k) != values[4].get(k)}, set(module.eclipse.KEYS))
        for key in (module.eclipse.KEYS[0], module.eclipse.KEYS[2]):
            self.assertEqual(result[key], values[1][key])
        for key in (module.akash.KEYS[0], module.akash.KEYS[2], "OASIS_DNA_JSON", "UNRELATED"):
            self.assertEqual(result[key], values[4][key])
        admission = json.loads(result[module.eclipse.KEYS[1]])
        self.assertEqual(admission["queryOnlyProviders"], ["IPFSOASIS", "AkashOASIS", "EclipseOASIS"])
        self.assertTrue(all(row["costUsd"] == 0 for row in admission["queryTariffs"]))

    def test_unacknowledged_drift_owner_price_and_proof_changes_reject(self):
        for fault in ("baseline", "old-candidate", "akash-candidate", "live-extra", "live-removed",
                      "live-dna", "live-permission", "eclipse-owner", "akash-owner", "paid-tariff",
                      "missing-ack", "ack-hash", "ack-not-verified", "ack-deploy"):
            with self.subTest(fault=fault):
                v = self.fixture()
                if fault == "baseline": v[2]["UNRELATED"] = "different"
                if fault == "old-candidate": v[1][module.eclipse.KEYS[0]] = "different"
                if fault == "akash-candidate": v[3][module.akash.KEYS[0]] = "different"
                if fault == "live-extra": v[4]["NEW"] = "unacknowledged"
                if fault == "live-removed": del v[4]["UNRELATED"]
                if fault == "live-dna": v[4]["OASIS_DNA_JSON"] = "{}"
                if fault == "live-permission": v[4][module.eclipse.KEYS[1]] = "{}"
                if fault == "eclipse-owner": v[5]["sourceOwner"] = "00000000-0000-0000-0000-000000000789"
                if fault == "akash-owner": v[6]["sourceOwner"] = "00000000-0000-0000-0000-000000000789"
                if fault == "paid-tariff":
                    admission = json.loads(v[0][module.eclipse.KEYS[1]])
                    admission["queryTariffs"][0]["costUsd"] = 1
                    v[0][module.eclipse.KEYS[1]] = json.dumps(admission)
                if fault == "missing-ack": v[7].pop()
                if fault == "ack-hash": v[7][0]["sha256"] = "wrong"
                if fault == "ack-not-verified": v[7][0]["verified"] = False
                if fault == "ack-deploy": v[7][0]["deploymentTriggered"] = True
                with self.assertRaises(ValueError): module.reconcile(*v)


if __name__ == "__main__":
    unittest.main()
