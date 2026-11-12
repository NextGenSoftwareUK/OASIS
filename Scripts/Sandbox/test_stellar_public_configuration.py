"""Pure candidate controls: no Railway, credentials, accounts or native RPC."""
import copy
import importlib.util
import json
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location("base_configuration", Path(__file__).with_name("Publish-HostedStellarPublicConfiguration.py"))
publisher = importlib.util.module_from_spec(spec)
spec.loader.exec_module(publisher)


class StellarConfigurationTests(unittest.TestCase):
    def fixture(self):
        source = {'schema': 1, 'horizonUrl': 'https://horizon.stellar.org/', 'networkPassphrase': 'Public Global Stellar Network ; September 2015', 'ledgers': [{'sequence': 65247993, 'hash': 'c8deae88416e676c30358bef81cfbb7aa78546a7713d3c252653591e3c3340ce'}, {'sequence': 65247992, 'hash': '4cc75f196c333bcc325fc90d1a42ea39209513e7ad1696c1866573d90a74d768'}, {'sequence': 65247991, 'hash': 'd898b8941c4cb2d0c03d5cf5fac6fb18b52888102fdb3283c26b779a039054d9'}], 'sourceOwner': '00000000-0000-0000-0000-000000000123'}
        before = {"OASIS_DNA_JSON": json.dumps({"OASIS": {"Sandbox": {"Enabled": True},
            "StorageProviders": {"StellarOASIS": {"PrivateKey": "inert-test-marker", "ConnectionString": "unchanged"}}}}),
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
        self.assertEqual(admission["queryOnlyProviders"], ["IPFSOASIS", "StellarOASIS"])
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
                encryption["AdditionalQueryableKeys"].append("StellarLedgerSequence")
                before["OASIS_DNA_JSON"] = json.dumps(dna)
                after = publisher.candidate(before, source)
                self.assertEqual(after["OASIS_DNA_JSON"], before["OASIS_DNA_JSON"])

    def test_rejects_credentials_wrong_catalogue_paid_tariff_and_existing_enrollment(self):
        for fault in ("credential", "endpoint", "chain", "addresses", "owner", "schema", "paid", "existing", "ordinary", "native", "examples", "tariff"):
            with self.subTest(fault=fault):
                before, source = self.fixture()
                admission = json.loads(before[publisher.KEYS[1]])
                if fault == "credential": source["credentials"] = "forbidden-test-marker"
                if fault == "endpoint": source["horizonUrl"] = "https://different.invalid/"
                if fault == "chain": source["networkPassphrase"] = "test-network"
                if fault == "addresses": source["ledgers"].reverse()
                if fault == "owner": source["sourceOwner"] = "00000000-0000-0000-0000-000000000000"
                if fault == "schema": source["schema"] = True
                if fault == "examples": before[publisher.KEYS[2]] = json.dumps({"StellarOASIS": []})
                if fault == "tariff": admission["queryTariffs"].append({"provider": "StellarOASIS", "costUsd": 0})
                if fault == "paid": admission["queryTariffs"][0]["costUsd"] = 1
                if fault == "existing": before[publisher.KEYS[0]] = "{}"
                if fault == "ordinary": admission["ordinaryProviders"].append("StellarOASIS")
                if fault == "native": admission["nativeProviders"].append({"provider": "StellarOASIS"})
                before[publisher.KEYS[1]] = json.dumps(admission)
                with self.assertRaises(ValueError): publisher.candidate(before, source)


if __name__ == "__main__":
    unittest.main()
