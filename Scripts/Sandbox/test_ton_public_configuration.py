"""Pure candidate controls: no Railway, credentials, accounts or native RPC."""
import copy
import importlib.util
import json
from pathlib import Path
import unittest
import tempfile

spec = importlib.util.spec_from_file_location("base_configuration", Path(__file__).with_name("Publish-HostedTonPublicConfiguration.py"))
publisher = importlib.util.module_from_spec(spec)
spec.loader.exec_module(publisher)


class TonConfigurationTests(unittest.TestCase):
    def test_revision_names_cannot_escape_the_existing_state_directory(self):
        self.assertEqual(publisher.revision_prefix("oland"), "ton-public-duration-oland")
        for invalid in (None, "", "../oland", "a/b", "a\\b", "A", "a" * 17):
            with self.subTest(invalid=invalid), self.assertRaises(ValueError):
                publisher.revision_prefix(invalid)

    def test_revision_requires_original_snapshots_and_refuses_any_mutation_evidence(self):
        with tempfile.TemporaryDirectory(prefix="cosmic-ton-publisher-") as temporary:
            state = Path(temporary)
            self.assertEqual(state.resolve().parent, Path(tempfile.gettempdir()).resolve())
            print("Disposable revision control state:", state)
            with self.assertRaises(RuntimeError):
                publisher.require_unapplied_revision(state)
            for suffix in ("-before-variables.dpapi", "-candidate-variables.dpapi"):
                (state / (publisher.PREFIX + suffix)).write_bytes(b"inert-test-marker")
            publisher.require_unapplied_revision(state)
            for suffix in (".intent.json", ".acknowledged.json"):
                evidence = state / ("ton-public-duration-CQL_TON_PUBLIC_SOURCE_JSON" + suffix)
                evidence.write_bytes(b"inert-test-marker")
                with self.assertRaises(RuntimeError):
                    publisher.require_unapplied_revision(state)
                evidence.unlink()

    def fixture(self):
        source = {'schema': 1, 'restUrl': 'https://toncenter.com/api/v2/', 'globalId': -239, 'blocks': [{'fileHash': 'fm6uQx6+r+QPcjvxtYiKq2RYdJ14If7BcskBcBGpFAY=', 'seqno': 102765318, 'rootHash': 'Y9wDd3pZpST/McLYMsPERk6s7WO6+p19kJ36oNcBOc8='}, {'fileHash': '8rGzd/bbQwbuEk76O1OyobR3vZoBnR6GWVbx4AZkLRc=', 'seqno': 102765319, 'rootHash': 'gzkgCwmATjDXVh41RK0tjESsu0V+EG0ApXxQe6zsOP4='}, {'fileHash': 'Y2mAEz86fWZD6YToEZrMbPnTg9aB0ZTRqX+RFwJVjKk=', 'seqno': 102765320, 'rootHash': 'Xhj82aH+7pa4qHMEe2NCR2/O0gzJy/nAL/VrYi0cLTU='}], 'sourceOwner': '00000000-0000-0000-0000-000000000123'}
        before = {"OASIS_DNA_JSON": json.dumps({"OASIS": {"Sandbox": {"Enabled": True},
            "StorageProviders": {"TONOASIS": {"PrivateKey": "inert-test-marker", "ConnectionString": "unchanged"}}}}),
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
        self.assertEqual(admission["queryOnlyProviders"], ["IPFSOASIS", "TONOASIS"])
        rows = json.loads(after[publisher.KEYS[2]])
        self.assertEqual(len(rows), 4)
        self.assertTrue(all(row["request"]["maximumDurationMilliseconds"] == 30000 for row in rows))
        self.assertEqual([row["request"]["maximumDurationMilliseconds"] for row in rows if "source" in row["request"]], [30000, 30000])
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
                encryption["AdditionalQueryableKeys"].append("TonBlockRootHash")
                before["OASIS_DNA_JSON"] = json.dumps(dna)
                after = publisher.candidate(before, source)
                self.assertEqual(after["OASIS_DNA_JSON"], before["OASIS_DNA_JSON"])

    def test_rejects_credentials_wrong_catalogue_paid_tariff_and_existing_enrollment(self):
        for fault in ("credential", "endpoint", "chain", "resources", "owner", "schema", "paid", "existing", "ordinary", "native", "examples", "tariff", "history", "global-type", "sequence-type"):
            with self.subTest(fault=fault):
                before, source = self.fixture()
                admission = json.loads(before[publisher.KEYS[1]])
                if fault == "credential": source["credentials"] = "forbidden-test-marker"
                if fault == "endpoint": source["restUrl"] = "https://different.invalid/"
                if fault == "chain": source["globalId"] = -3
                if fault == "resources": source["blocks"].reverse()
                if fault == "owner": source["sourceOwner"] = "00000000-0000-0000-0000-000000000000"
                if fault == "global-type": source["globalId"] = -239.0
                if fault == "sequence-type": source["blocks"][0]["seqno"] = float(source["blocks"][0]["seqno"])
                if fault == "history": source["unapprovedConsistency"] = "Historical"
                if fault == "schema": source["schema"] = True
                if fault == "examples": before[publisher.KEYS[2]] = json.dumps({"TONOASIS": []})
                if fault == "tariff": admission["queryTariffs"].append({"provider": "TONOASIS", "costUsd": 0})
                if fault == "paid": admission["queryTariffs"][0]["costUsd"] = 1
                if fault == "existing": before[publisher.KEYS[0]] = "{}"
                if fault == "ordinary": admission["ordinaryProviders"].append("TONOASIS")
                if fault == "native": admission["nativeProviders"].append({"provider": "TONOASIS"})
                before[publisher.KEYS[1]] = json.dumps(admission)
                with self.assertRaises(ValueError): publisher.candidate(before, source)


if __name__ == "__main__":
    unittest.main()
