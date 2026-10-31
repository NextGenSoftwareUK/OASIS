"""Pure candidate controls: no Railway, credentials, accounts or native RPC."""
import copy
import importlib.util
import json
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location("base_configuration", Path(__file__).with_name("Publish-HostedLineaPublicConfiguration.py"))
publisher = importlib.util.module_from_spec(spec)
spec.loader.exec_module(publisher)


class LineaConfigurationTests(unittest.TestCase):
    def fixture(self):
        source = {"schema": 1, "rpcUrl": "https://mainnet.linea.io/", "chainId": "0xe708",
            "addresses": ["0x" + format(value, "040x") for value in (1, 2, 3)],
            "sourceOwner": "00000000-0000-0000-0000-000000000123"}
        before = {"OASIS_DNA_JSON": json.dumps({"OASIS": {"Sandbox": {"Enabled": True},
            "StorageProviders": {"LineaOASIS": {"PrivateKey": "inert-test-marker", "ConnectionString": "unchanged"}}}}),
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
        self.assertEqual(admission["queryOnlyProviders"], ["IPFSOASIS", "LineaOASIS"])
        rows = json.loads(after[publisher.KEYS[2]])["LineaOASIS"]
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
                encryption["AdditionalQueryableKeys"].append("LineaAccountAddress")
                before["OASIS_DNA_JSON"] = json.dumps(dna)
                after = publisher.candidate(before, source)
                self.assertEqual(after["OASIS_DNA_JSON"], before["OASIS_DNA_JSON"])

    def test_rejects_credentials_wrong_catalogue_paid_tariff_and_existing_enrollment(self):
        for fault in ("credential", "endpoint", "chain", "addresses", "owner", "schema", "paid", "existing", "ordinary", "native", "examples", "tariff"):
            with self.subTest(fault=fault):
                before, source = self.fixture()
                admission = json.loads(before[publisher.KEYS[1]])
                if fault == "credential": source["credentials"] = "forbidden-test-marker"
                if fault == "endpoint": source["rpcUrl"] = "https://different.invalid/"
                if fault == "chain": source["chainId"] = "0x2"
                if fault == "addresses": source["addresses"].reverse()
                if fault == "owner": source["sourceOwner"] = "00000000-0000-0000-0000-000000000000"
                if fault == "schema": source["schema"] = True
                if fault == "examples": before[publisher.KEYS[2]] = json.dumps({"LineaOASIS": []})
                if fault == "tariff": admission["queryTariffs"].append({"provider": "LineaOASIS", "costUsd": 0})
                if fault == "paid": admission["queryTariffs"][0]["costUsd"] = 1
                if fault == "existing": before[publisher.KEYS[0]] = "{}"
                if fault == "ordinary": admission["ordinaryProviders"].append("LineaOASIS")
                if fault == "native": admission["nativeProviders"].append({"provider": "LineaOASIS"})
                before[publisher.KEYS[1]] = json.dumps(admission)
                with self.assertRaises(ValueError): publisher.candidate(before, source)


if __name__ == "__main__":
    unittest.main()
