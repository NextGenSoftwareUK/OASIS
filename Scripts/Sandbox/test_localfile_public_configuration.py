"""LocalFile rollout invariants; synthetic bytes, no network or service mutation."""
import base64
import copy
import hashlib
import importlib.util
import json
from pathlib import Path
from tempfile import TemporaryDirectory
from types import SimpleNamespace
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location("localfile_publisher", Path(__file__).with_name("Publish-HostedLocalFileConfiguration.py"))
publisher = importlib.util.module_from_spec(spec)
spec.loader.exec_module(publisher)


class LocalFileConfigurationTests(unittest.TestCase):
    def fixture(self):
        records, payloads = [], []
        for number in range(1, 4):
            raw = ("synthetic-record-" + str(number)).encode()
            row = {"id": str(publisher.uuid.UUID(int=number)), "sha256": hashlib.sha256(raw).hexdigest()}
            records.append(row)
            payloads.append({**row, "contentBase64": base64.b64encode(raw).decode()})
        source = {"schema": 1, "directory": "/data/cql-local-file-demo",
                  "sourceOwner": str(publisher.uuid.UUID(int=99)), "records": records}
        bootstrap = {"schema": 1, "records": payloads}
        before = {"OASIS_DNA_JSON": '{"OASIS":{"Sandbox":{"Enabled":true}}}',
                  "PREPARED_STORAGE_ADMISSION_JSON": json.dumps({"queryOnlyProviders": ["IPFSOASIS"],
                      "ordinaryProviders": ["MongoDBOASIS"], "nativeProviders": [],
                      "queryTariffs": [{"provider": "IPFSOASIS", "costUsd": 0}],
                      "routes": ["preserved"], "storageTariffs": ["preserved"]}),
                  "UNRELATED": "preserved"}
        return before, source, bootstrap

    def test_only_five_values_change_and_no_write_route_is_added(self):
        before, source, bundle = self.fixture()
        original = copy.deepcopy(before)
        after = publisher.candidate(before, source, bundle)
        self.assertEqual(before, original)
        self.assertEqual({k for k in before.keys() | after.keys() if before.get(k) != after.get(k)}, set(publisher.KEYS))
        prior = json.loads(before[publisher.KEYS[2]])
        admission = json.loads(after[publisher.KEYS[2]])
        for key in ("ordinaryProviders", "nativeProviders", "routes", "storageTariffs"):
            self.assertEqual(prior[key], admission[key])
        self.assertEqual(admission["queryTariffs"][-1]["costUsd"], 0)
        examples = json.loads(after[publisher.KEYS[4]])
        self.assertEqual(len(examples), 4)
        self.assertEqual([len(e["request"].get("source", e["request"])["filters"][0]["value"]) for e in examples], [3, 3, 2, 2])
        self.assertEqual(json.loads(after[publisher.KEYS[3]])["LocalFileOASIS"], [r["id"] for r in source["records"]])

    def test_corrupt_incomplete_duplicate_and_foreign_bundle_refuse(self):
        for scenario in ("bytes", "missing", "duplicate", "foreign", "hash"):
            with self.subTest(scenario=scenario):
                before, source, bundle = self.fixture()
                if scenario == "bytes": bundle["records"][0]["contentBase64"] = "Y29ycnVwdA=="
                elif scenario == "missing": bundle["records"].pop()
                elif scenario == "duplicate": bundle["records"][1] = bundle["records"][0]
                elif scenario == "foreign": bundle["records"][0]["id"] = str(publisher.uuid.UUID(int=100))
                else: bundle["records"][0]["sha256"] = "0" * 64
                with self.assertRaises(ValueError): publisher.candidate(before, source, bundle)

    def test_existing_configuration_is_never_replaced(self):
        for key in (publisher.KEYS[0], publisher.KEYS[1], publisher.KEYS[4]):
            before, source, bundle = self.fixture()
            before[key] = "preserved"
            with self.assertRaises(ValueError): publisher.candidate(before, source, bundle)

    def test_no_paid_or_non_sandbox_or_different_directory_configuration(self):
        for scenario in ("paid", "dna", "directory", "credential"):
            before, source, bundle = self.fixture()
            if scenario == "paid":
                admission = json.loads(before[publisher.KEYS[2]])
                admission["queryTariffs"][0]["costUsd"] = 0.01
                before[publisher.KEYS[2]] = json.dumps(admission)
            elif scenario == "dna": before["OASIS_DNA_JSON"] = '{"OASIS":{"Sandbox":{"Enabled":false}}}'
            elif scenario == "directory": source["directory"] = "/data/unrelated"
            else: source["credential"] = "forbidden"
            with self.assertRaises(ValueError): publisher.candidate(before, source, bundle)

    def test_prepare_creates_fresh_output_directories_without_mutation(self):
        before, source, bundle = self.fixture()
        with TemporaryDirectory() as directory:
            root = Path(directory)
            state = root / "LocalState" / "rollout"
            with patch.object(publisher, "ROOT", root), \
                 patch.object(publisher.configuration, "STATE", state), \
                 patch.object(publisher, "qualified_source", return_value=(source, bundle)), \
                 patch.object(publisher.configuration, "variables", return_value=before), \
                 patch.object(publisher.configuration.verification, "protected_token", return_value=b"sealed"), \
                 patch.object(publisher.configuration, "apply_values") as mutation:
                publisher.prepare()
            self.assertEqual(2, len(list(state.glob("*.dpapi"))))
            review = root / "TestResults/HostedLocalFileSourceCurrent/prepared-review.json"
            self.assertEqual(publisher.KEYS, json.loads(review.read_bytes())["changedKeys"])
            mutation.assert_not_called()

    def test_apply_cannot_mutate_before_exact_release_gate(self):
        before, source, bundle = self.fixture()
        after = publisher.candidate(before, source, bundle)
        def refuse(_):
            raise RuntimeError("not deployed")
        with patch.object(publisher, "qualified_source", return_value=(source, bundle)), \
             patch.object(publisher.configuration, "protected_read", side_effect=[before, after]), \
             patch.object(publisher.configuration, "apply_values") as mutation, \
             patch.object(publisher, "module", return_value=SimpleNamespace(require_matching_release=refuse)):
            with self.assertRaisesRegex(RuntimeError, "not deployed"): publisher.apply()
            mutation.assert_not_called()


    def test_qualification_rejects_modified_retained_bundle(self):
        _, source, bundle = self.fixture()
        with TemporaryDirectory() as directory:
            root = Path(directory)
            dataset = root / "LocalState/COSMIC/HostedLocalFilePublicDatasetCurrent"
            evidence = root / "LocalState/COSMIC/HostedLocalFileSourceCurrent"
            dataset.mkdir(parents=True)
            evidence.mkdir(parents=True)
            raw = json.dumps(source).encode()
            payload = json.dumps(bundle).encode()
            hashes = {
                "sourceConfigSha256": publisher.hashlib.sha256(raw).hexdigest(),
                "bootstrapSha256": publisher.hashlib.sha256(payload).hexdigest(),
            }
            (dataset / "source-config.json").write_bytes(raw)
            bootstrap = dataset / "bootstrap.json"
            bootstrap.write_bytes(payload)
            (dataset / "export.acknowledged.json").write_text(json.dumps(
                dict(hashes, sdkFull=3, sdkSubset=2, nativeSourceUnchanged=True)))
            (evidence / "source-qualified.json").write_text(json.dumps(
                dict(hashes, sdkCounts=[3, 2], shippingBootstrapAdmitted=True)))
            with patch.object(publisher, "ROOT", root):
                self.assertEqual((source, bundle), publisher.qualified_source())
                bootstrap.write_bytes(payload + b" ")
                with self.assertRaisesRegex(RuntimeError, "source-bound"):
                    publisher.qualified_source()

if __name__ == "__main__":
    unittest.main()
