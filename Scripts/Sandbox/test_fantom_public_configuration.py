"""Pure Fantom candidate tests: no native requests, Railway calls or credentials."""
import copy
import hashlib
import importlib.util
import json
from pathlib import Path
from types import SimpleNamespace
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location("fantom_publisher", Path(__file__).with_name("Publish-HostedFantomPublicConfiguration.py"))
publisher = importlib.util.module_from_spec(spec)
spec.loader.exec_module(publisher)


class FantomConfigurationTests(unittest.TestCase):
    def fixture(self):
        source = {"schema": 1, "rpcUrl": "https://rpcapi.fantom.network/", "chainId": "0xfa",
            "addresses": publisher.EXPECTED_KEYS.copy(), "sourceOwner": "00000000-0000-0000-0000-000000000123"}
        before = {"OASIS_DNA_JSON": json.dumps({"OASIS": {"Sandbox": {"Enabled": True},
            "StorageProviders": {"FantomOASIS": {"unchanged": "inert-test-marker"}}}}),
            "PREPARED_STORAGE_ADMISSION_JSON": json.dumps({"queryOnlyProviders": ["IPFSOASIS"],
                "ordinaryProviders": ["MongoDBOASIS"], "nativeProviders": [],
                "queryTariffs": [{"provider": "IPFSOASIS", "costUsd": 0, "catalogueVersion": "test-v1"}],
                "storageTariffs": [], "routes": [{"path": "unchanged"}]}),
            "CQL_HOSTED_QUERY_EXAMPLES_JSON": "{}", "UNRELATED": "unchanged"}
        return before, source

    def test_only_three_values_change_with_free_tariff_and_four_bounded_presets(self):
        before, source = self.fixture()
        original = copy.deepcopy(before)
        after = publisher.candidate(before, source)
        self.assertEqual(before, original)
        self.assertEqual({key for key in before.keys() | after.keys() if before.get(key) != after.get(key)}, set(publisher.KEYS))
        self.assertEqual(after["OASIS_DNA_JSON"], before["OASIS_DNA_JSON"])
        self.assertEqual(after["UNRELATED"], "unchanged")
        admission = json.loads(after[publisher.KEYS[1]])
        prior = json.loads(before[publisher.KEYS[1]])
        for key in ("ordinaryProviders", "nativeProviders", "storageTariffs", "routes"):
            self.assertEqual(admission[key], prior[key])
        self.assertEqual(admission["queryOnlyProviders"], ["IPFSOASIS", "FantomOASIS"])
        self.assertEqual(admission["queryTariffs"][-1]["costUsd"], 0)
        rows = json.loads(after[publisher.KEYS[2]])
        self.assertEqual(len(rows), 4)
        self.assertEqual([len(row["request"].get("source", row["request"])["filters"][0]["value"]) for row in rows], [3, 3, 2, 2])
        self.assertTrue(all(row["request"]["maximumDurationMilliseconds"] == 30000 for row in rows))
        self.assertEqual(json.loads(after[publisher.KEYS[0]]), source)

    def test_invalid_or_different_source_catalogue_refuses(self):
        for scenario in ("credential", "endpoint", "account", "key", "owner", "schema"):
            with self.subTest(scenario=scenario):
                before, source = self.fixture()
                if scenario in ("credential", "endpoint"):
                    source[scenario] = "forbidden-inert-value"
                elif scenario == "account": source["rpcUrl"] = "https://different.invalid/"
                elif scenario == "key": source["addresses"][0] = "different-object"
                elif scenario == "owner": source["sourceOwner"] = "00000000-0000-0000-0000-000000000000"
                else: source["schema"] = True
                with self.assertRaises(ValueError): publisher.candidate(before, source)

    def test_existing_enrollment_or_example_is_not_overwritten(self):
        for scenario in ("source", "ordinary", "query-only", "native", "tariff", "example", "shared-example"):
            with self.subTest(scenario=scenario):
                before, source = self.fixture()
                admission = json.loads(before[publisher.KEYS[1]])
                if scenario == "source": before[publisher.KEYS[0]] = "preserved"
                elif scenario == "ordinary": admission["ordinaryProviders"].append("FantomOASIS")
                elif scenario == "query-only": admission["queryOnlyProviders"].append("FantomOASIS")
                elif scenario == "native": admission["nativeProviders"].append({"provider": "FantomOASIS"})
                elif scenario == "tariff": admission["queryTariffs"].append({"provider": "FantomOASIS"})
                elif scenario == "example": before[publisher.KEYS[2]] = "[]"
                else: before["CQL_HOSTED_QUERY_EXAMPLES_JSON"] = '{"FantomOASIS":[]}'
                before[publisher.KEYS[1]] = json.dumps(admission)
                with self.assertRaises(ValueError): publisher.candidate(before, source)

    def test_no_paid_tariff_or_disabled_sandbox_is_admitted(self):
        for scenario in ("paid", "sandbox"):
            before, source = self.fixture()
            if scenario == "paid":
                admission = json.loads(before[publisher.KEYS[1]])
                admission["queryTariffs"][0]["costUsd"] = 0.01
                before[publisher.KEYS[1]] = json.dumps(admission)
            else:
                dna = json.loads(before["OASIS_DNA_JSON"])
                dna["OASIS"]["Sandbox"]["Enabled"] = False
                before["OASIS_DNA_JSON"] = json.dumps(dna)
            with self.assertRaises(ValueError): publisher.candidate(before, source)

    def test_encrypted_copies_require_an_explicit_queryable_metadata_policy(self):
        for algorithm in ("Rijndael256EncryptionEnabled", "QuantumEncryptionEnabled"):
            before, source = self.fixture()
            dna = json.loads(before["OASIS_DNA_JSON"])
            settings = {algorithm: True, "AdditionalQueryableKeys": ["anotherAppKey"]}
            dna["OASIS"]["Security"] = {"HolonDataEncryption": settings}
            before["OASIS_DNA_JSON"] = json.dumps(dna)
            with self.assertRaisesRegex(ValueError, "queryable policy"): publisher.candidate(before, source)
            settings["AdditionalQueryableKeys"].append("FantomAccountAddress")
            before["OASIS_DNA_JSON"] = json.dumps(dna)
            self.assertEqual(publisher.candidate(before, source)["OASIS_DNA_JSON"], before["OASIS_DNA_JSON"])

    def test_native_and_http_acknowledgements_keep_their_own_proof_fields(self):
        _, source = self.fixture()
        raw = json.dumps(source).encode()
        digest = hashlib.sha256(raw).hexdigest()
        native = {"schema": 1, "nativeQualified": True, "sourceSha256": digest,
            "nativeCounts": [3, 2], "actualConsoleCounts": [3, 2], "policy": "latest-canonical-at-read"}
        local = {"schema": 1, "web4LocalQualified": True, "sourceSha256": digest,
            "nativeCounts": [3, 2], "federationCounts": [5, 4], "policy": "latest-canonical-at-read",
            "foreignPrivate": 0, "foreignPublic": 3, "savedPrivateCopies": 2, "fixtureCleaned": True}
        with patch.object(publisher.Path, "read_bytes", return_value=raw), \
                patch.object(publisher.Path, "read_text", side_effect=[json.dumps(native), json.dumps(local)]):
            self.assertEqual(publisher.qualified_source(), source)

    def test_local_proof_cannot_change_policy_source_or_omit_copy_cleanup(self):
        _, source = self.fixture()
        raw = json.dumps(source).encode()
        digest = hashlib.sha256(raw).hexdigest()
        native = {"schema": 1, "nativeQualified": True, "sourceSha256": digest,
            "nativeCounts": [3, 2], "actualConsoleCounts": [3, 2], "policy": "latest-canonical-at-read"}
        for field, value in (("policy", "finalized"), ("sourceSha256", "different"),
                             ("fixtureCleaned", False), ("savedPrivateCopies", 0)):
            local = {"schema": 1, "web4LocalQualified": True, "sourceSha256": digest,
                "nativeCounts": [3, 2], "federationCounts": [5, 4], "policy": "latest-canonical-at-read",
                "foreignPrivate": 0, "foreignPublic": 3, "savedPrivateCopies": 2, "fixtureCleaned": True}
            local[field] = value
            with patch.object(publisher.Path, "read_bytes", return_value=raw), \
                    patch.object(publisher.Path, "read_text", side_effect=[json.dumps(native), json.dumps(local)]):
                with self.assertRaises(RuntimeError): publisher.qualified_source()

    def test_apply_requires_exact_release_before_shared_mutation_owner(self):
        before, source = self.fixture()
        after = publisher.candidate(before, source)
        with patch.object(publisher, "qualified_source", return_value=source), \
                patch.object(publisher.configuration, "protected_read", side_effect=[before, after]), \
                patch.object(publisher.configuration, "apply_values") as mutation, \
                patch.object(publisher, "module", return_value=SimpleNamespace(require_matching_release=lambda _: (_ for _ in ()).throw(RuntimeError("not deployed")))):
            with self.assertRaisesRegex(RuntimeError, "not deployed"): publisher.apply()
            mutation.assert_not_called()


if __name__ == "__main__":
    unittest.main()
