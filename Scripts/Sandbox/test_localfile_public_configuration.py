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

    def test_prepare_reconciles_a_matching_partial_snapshot(self):
        before, source, bundle = self.fixture()
        with TemporaryDirectory() as directory:
            root = Path(directory)
            state = root / "rollout"
            state.mkdir()
            original = state / (publisher.PREFIX + "-before-variables.dpapi")
            original.write_bytes(b"original sealed baseline")
            with patch.object(publisher, "ROOT", root), \
                 patch.object(publisher.configuration, "STATE", state), \
                 patch.object(publisher, "qualified_source", return_value=(source, bundle)), \
                 patch.object(publisher.configuration, "variables", return_value=before), \
                 patch.object(publisher.configuration, "protected_read", return_value=before) as read, \
                 patch.object(publisher.configuration.verification, "protected_token", return_value=b"sealed"):
                publisher.prepare()
                self.assertEqual(b"original sealed baseline", original.read_bytes())
                self.assertEqual(2, len(list(state.glob("*.dpapi"))))
                read.return_value = {}
                with self.assertRaisesRegex(RuntimeError, "preparation differs"):
                    publisher.prepare()

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


    def test_preflight_checks_every_instance_and_apply_refuses_multi_instance_bootstrap(self):
        before, source, bundle = self.fixture()
        after = publisher.candidate(before, source, bundle)
        instances = ["11111111-1111-1111-1111-111111111111", "22222222-2222-2222-2222-222222222222"]
        writable = b"42 37 8:1 / /data rw,relatime - ext4 /dev/disk rw\n"
        for second in (writable.replace(b"/data", b"/data/child"),
                       writable.replace(b"rw", b"ro", 1),
                       writable.replace(b"/dev/disk rw", b"/dev/disk ro"), writable):
            with patch.dict(publisher.os.environ, {"COSMIC_CQL_REVIEWED_INSTANCE_IDS": ",".join(instances)}), \
                 patch.object(publisher, "qualified_source", return_value=(source, bundle)), \
                 patch.object(publisher.configuration, "protected_read", side_effect=[before, after]), \
                 patch.object(publisher.configuration, "apply_values") as mutation, \
                 patch.object(publisher, "verify_release", return_value="reviewed-deployment"), \
                 patch.object(publisher.subprocess, "run", side_effect=[
                     SimpleNamespace(returncode=0, stdout=writable),
                     SimpleNamespace(returncode=0, stdout=second)]) as ssh:
                if second == writable:
                    publisher.verify_mount()
                else:
                    with self.assertRaisesRegex(RuntimeError, "writable /data"):
                        publisher.verify_mount()
                for call, instance in zip(ssh.call_args_list, instances, strict=True):
                    self.assertEqual(["-d", instance, "--", "cat", "/proc/self/mountinfo"], call.args[0][-5:])
                with self.assertRaisesRegex(RuntimeError, "requires one reviewed active instance"):
                    publisher.apply()
                self.assertEqual(2, ssh.call_count)
                mutation.assert_not_called()

    def test_apply_stages_settings_after_single_instance_mount_is_verified(self):
        before, source, bundle = self.fixture()
        after = publisher.candidate(before, source, bundle)
        instance = "11111111-1111-1111-1111-111111111111"
        with patch.dict(publisher.os.environ, {"COSMIC_CQL_REVIEWED_INSTANCE_IDS": instance}), \
             patch.object(publisher, "qualified_source", return_value=(source, bundle)), \
             patch.object(publisher.configuration, "protected_read", side_effect=[before, after] * 2), \
             patch.object(publisher.configuration, "apply_values") as mutation, \
             patch.object(publisher, "verify_release", side_effect=["first", "first", "first", "second"]) as release, \
             patch.object(publisher.subprocess, "run", return_value=SimpleNamespace(
                 returncode=0, stdout=b"42 37 8:1 / /data rw,relatime - ext4 /dev/disk rw\n")) as ssh:
            publisher.apply()
            self.assertEqual(2, release.call_count)
            ssh.assert_called_once()
            mutation.assert_called_once_with(publisher.PREFIX, publisher.KEYS, reviewed_snapshots=(before, after))
            mutation.reset_mock()
            with self.assertRaisesRegex(RuntimeError, "deployment changed"):
                publisher.apply()
            mutation.assert_not_called()

    def test_verify_settings_rejects_drift_without_writing(self):
        before, source, bundle = self.fixture()
        after = publisher.candidate(before, source, bundle)
        with TemporaryDirectory() as directory, \
             patch.object(publisher.configuration, "STATE", Path(directory)), \
             patch.dict(publisher.os.environ, {"COSMIC_CQL_SETTINGS_DEPLOYMENT_ID": "deployment"}), \
             patch.object(publisher, "require_data_mount") as mount, \
             patch.object(publisher, "verify_release", return_value="deployment") as release, \
             patch.object(publisher.configuration, "protected_read", return_value=after), \
             patch.object(publisher.configuration, "variables", return_value=after) as variables, \
             patch.object(publisher.configuration, "apply_values") as mutation:
            with self.assertRaisesRegex(RuntimeError, "Missing reviewed LocalFile settings acknowledgement"):
                publisher.verify_settings()
            variables.assert_not_called()
            for key in publisher.KEYS:
                ack = Path(directory) / f"{publisher.PREFIX}-{key}.acknowledged.json"
                ack.write_text(json.dumps(dict(key=key, verified=True, deploymentTriggered=False,
                    sha256=publisher.hashlib.sha256(after[key].encode()).hexdigest())))
            self.assertEqual("deployment", publisher.verify_settings())
            mount.reset_mock()
            release.return_value = "earlier-deployment-of-same-image"
            with self.assertRaisesRegex(RuntimeError, "not the reviewed LocalFile settings deployment"):
                publisher.verify_settings()
            mount.assert_not_called()
            release.return_value = "deployment"
            variables.return_value = dict(after, **{publisher.KEYS[0]: "different"})
            with self.assertRaisesRegex(RuntimeError, "settings differ"):
                publisher.verify_settings()
            variables.return_value = dict(after, UNREVIEWED_SETTING="changed")
            with self.assertRaisesRegex(RuntimeError, "Other Development WEB4 settings changed"):
                publisher.verify_settings()
            mutation.assert_not_called()

    def test_verify_records_binds_all_bytes_to_one_reviewed_deployment(self):
        before, source, bundle = self.fixture()
        candidate = publisher.candidate(before, source, bundle)
        instance = "11111111-1111-4111-8111-111111111111"
        paths = [f"/data/cql-local-file-demo/Holons/{row['id']}.json" for row in source["records"]]
        payloads = [base64.b64decode(row["contentBase64"]) for row in bundle["records"]]
        for issue in (None, "path", "digest", "oversize", "deployment"):
            with self.subTest(issue=issue):
                observations = [response for path, body in zip(paths, payloads, strict=True)
                                for response in (path.encode() + b"\n", body)]
                if issue == "path":
                    observations[0] = b"/tmp/foreign.json\n"
                elif issue == "digest":
                    observations[1] = b"changed"
                elif issue == "oversize":
                    observations[1] = b"x" * 32769
                with (
                    patch.dict(publisher.os.environ, {"COSMIC_CQL_REVIEWED_INSTANCE_IDS": instance}),
                    patch.object(publisher, "qualified_source", return_value=(source, bundle)),
                    patch.object(publisher.configuration, "protected_read", return_value=candidate),
                    patch.object(publisher, "verify_settings", side_effect=["reviewed", "changed" if issue == "deployment" else "reviewed"]),
                    patch.object(publisher.subprocess, "run", side_effect=[
                        SimpleNamespace(returncode=0, stdout=value) for value in observations]) as ssh,
                ):
                    if issue is None:
                        self.assertEqual(("reviewed", source), publisher.verify_records())
                        self.assertEqual(6, ssh.call_count)
                        for index, path in enumerate(paths):
                            self.assertEqual(["-d", instance, "--", "realpath", "-e", path], ssh.call_args_list[2 * index].args[0][-6:])
                            self.assertEqual(["-d", instance, "--", "head", "-c", "32769", path], ssh.call_args_list[2 * index + 1].args[0][-7:])
                    else:
                        with self.assertRaisesRegex(RuntimeError, "outside|qualified digest|deployment changed"):
                            publisher.verify_records()

    def test_mounted_receipt_refuses_missing_modified_and_superseded_evidence(self):
        _, source, bundle = self.fixture()
        deployment = "11111111-1111-4111-8111-111111111111"
        env = {"COSMIC_CQL_SETTINGS_DEPLOYMENT_ID": deployment,
               "COSMIC_CQL_REVIEWED_INSTANCE_IDS": "22222222-2222-4222-8222-222222222222",
               "COSMIC_CQL_EXPECTED_DEPLOYMENT_IMAGE_DIGEST": "sha256:" + "a" * 64,
               "COSMIC_CQL_EXPECTED_DEPLOYMENT_COMMIT": "b" * 40}
        with TemporaryDirectory() as directory:
            root = Path(directory)
            (root / "TestResults/HostedLocalFileSourceCurrent").mkdir(parents=True)
            with (
                patch.object(publisher, "ROOT", root),
                patch.dict(publisher.os.environ, env),
                patch.object(publisher, "qualified_source", side_effect=AssertionError("Receipt must use the verified source.")),
                patch.object(publisher, "verify_release", return_value=deployment) as release,
            ):
                with self.assertRaisesRegex(RuntimeError, "Verify mounted"):
                    publisher.require_mounted_record_receipt(source)
                publisher.write_mounted_record_receipt(deployment, source)
                publisher.write_mounted_record_receipt(deployment, source)
                self.assertEqual(deployment, publisher.require_mounted_record_receipt(source)["deployment"])
                with self.assertRaisesRegex(RuntimeError, "reviewed source"):
                    publisher.require_mounted_record_receipt(dict(source, records=[]))
                release.return_value = "superseded"
                with self.assertRaisesRegex(RuntimeError, "live deployment"):
                    publisher.require_mounted_record_receipt(source)

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
