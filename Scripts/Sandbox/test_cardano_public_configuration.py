"""Pure candidate controls: no Railway, credentials, accounts or native RPC."""
import copy
import importlib.util
import json
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location("base_configuration", Path(__file__).with_name("Publish-HostedCardanoPublicConfiguration.py"))
publisher = importlib.util.module_from_spec(spec)
spec.loader.exec_module(publisher)


class CardanoConfigurationTests(unittest.TestCase):
    def fixture(self):
        source = {'schema': 1, 'restUrl': 'https://api.koios.rest/api/v1/', 'networkMagic': '764824073', 'blockHashes': ['fb9087c9f1408a7bbd7b022fd294ab565fec8dd3a8ef091567482722a1fa4e30', '60188a8dcb6db0d80628815be2cf626c4d17cb3e826cebfca84adaff93ad492a', 'c6646214a1f377aa461a0163c213fc6b86a559a2d6ebd647d54c4eb00aaab015'], 'sourceOwner': '00000000-0000-0000-0000-000000000123'}
        before = {"OASIS_DNA_JSON": json.dumps({"OASIS": {"Sandbox": {"Enabled": True},
            "StorageProviders": {"CardanoOASIS": {"PrivateKey": "inert-test-marker", "ConnectionString": "unchanged"}}}}),
            "PREPARED_STORAGE_ADMISSION_JSON": json.dumps({"queryOnlyProviders": ["IPFSOASIS"],
                "ordinaryProviders": ["MongoDBOASIS"], "nativeProviders": [],
                "queryTariffs": [{"provider": "IPFSOASIS", "costUsd": 0, "catalogueVersion": "test-v1"}],
                "storageTariffs": [], "routes": [{"path": "unchanged"}]}),
            "CQL_HOSTED_QUERY_EXAMPLES_JSON": "{}", "UNRELATED": "unchanged"}
        return before, source

    def test_preserves_wallet_dna_and_unrelated_admission_while_staging_four_presets(self):
        before, source = self.fixture()
        original = copy.deepcopy(before)
        after = publisher.candidate(before, source)
        self.assertEqual(before, original)
        self.assertEqual(after["OASIS_DNA_JSON"], before["OASIS_DNA_JSON"])
        self.assertEqual(after["UNRELATED"], "unchanged")
        admission = json.loads(after[publisher.KEYS[1]])
        old = json.loads(before[publisher.KEYS[1]])
        for key in ("nativeProviders", "ordinaryProviders", "storageTariffs", "routes"):
            self.assertEqual(admission[key], old[key])
        self.assertEqual(admission["queryOnlyProviders"], ["IPFSOASIS", "CardanoOASIS"])
        rows = json.loads(after[publisher.KEYS[2]])
        self.assertEqual(len(rows), 4)
        self.assertEqual([len(row["request"].get("source", row["request"])["filters"][0]["value"]) for row in rows], [3, 3, 2, 2])

    def test_encrypted_snapshot_query_key_requires_explicit_host_policy(self):
        for algorithm in ("Rijndael256EncryptionEnabled", "QuantumEncryptionEnabled"):
            with self.subTest(algorithm=algorithm):
                before, source = self.fixture()
                dna = json.loads(before["OASIS_DNA_JSON"])
                encryption = {algorithm: True, "AdditionalQueryableKeys": ["anotherAppKey"]}
                dna["OASIS"]["Security"] = {"HolonDataEncryption": encryption}
                before["OASIS_DNA_JSON"] = json.dumps(dna)
                with self.assertRaisesRegex(ValueError, "queryable policy"):
                    publisher.candidate(before, source)
                encryption["AdditionalQueryableKeys"].append("CardanoBlockHash")
                before["OASIS_DNA_JSON"] = json.dumps(dna)
                after = publisher.candidate(before, source)
                self.assertEqual(after["OASIS_DNA_JSON"], before["OASIS_DNA_JSON"])

    def test_rejects_credentials_wrong_catalogue_paid_tariff_and_existing_enrollment(self):
        for fault in ("credential", "endpoint", "chain", "resources", "owner", "schema", "paid", "existing", "ordinary", "native", "examples", "tariff", "history"):
            with self.subTest(fault=fault):
                before, source = self.fixture()
                admission = json.loads(before[publisher.KEYS[1]])
                if fault == "credential": source["credentials"] = "forbidden-test-marker"
                if fault == "endpoint": source["restUrl"] = "https://different.invalid/"
                if fault == "chain": source["networkMagic"] = "testnet"
                if fault == "resources": source["blockHashes"].reverse()
                if fault == "owner": source["sourceOwner"] = "00000000-0000-0000-0000-000000000000"
                if fault == "history": source["unapprovedConsistency"] = "Historical"
                if fault == "schema": source["schema"] = True
                if fault == "examples": before[publisher.KEYS[2]] = json.dumps({"CardanoOASIS": []})
                if fault == "tariff": admission["queryTariffs"].append({"provider": "CardanoOASIS", "costUsd": 0})
                if fault == "paid": admission["queryTariffs"][0]["costUsd"] = 1
                if fault == "existing": before[publisher.KEYS[0]] = "{}"
                if fault == "ordinary": admission["ordinaryProviders"].append("CardanoOASIS")
                if fault == "native": admission["nativeProviders"].append({"provider": "CardanoOASIS"})
                before[publisher.KEYS[1]] = json.dumps(admission)
                with self.assertRaises(ValueError): publisher.candidate(before, source)


if __name__ == "__main__":
    unittest.main()
