import hashlib
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location(
    "d1_publisher", Path(__file__).with_name("Publish-HostedD1WorkerConfiguration.py"))
publisher = importlib.util.module_from_spec(spec)
spec.loader.exec_module(publisher)


class D1WorkerConfigurationTests(unittest.TestCase):
    def test_failed_https_qualification_retains_evidence_and_cannot_replay(self):
        probe_spec = importlib.util.spec_from_file_location(
            "d1_probe", Path(__file__).with_name("Probe-D1WorkerHostedReadiness.py"))
        probe = importlib.util.module_from_spec(probe_spec)
        probe_spec.loader.exec_module(probe)
        with tempfile.TemporaryDirectory() as directory:
            state = Path(directory)
            raw = json.dumps({"endpoint": "https://worker.example/symbols", "symbols": publisher.EXPECTED_KEYS}).encode()
            (state / "source.json").write_bytes(raw)
            with patch.object(probe, "STATE", state), patch("sys.argv", ["probe"]), \
                 patch.object(probe, "qualify", side_effect=TimeoutError("private diagnostic")) as qualify:
                with self.assertRaises(TimeoutError):
                    probe.main()
                with self.assertRaises(FileExistsError):
                    probe.main()
                qualify.assert_called_once()
            receipt = json.loads((state / "worker-https-qualification.failed.json").read_bytes())
            self.assertEqual({"schema": 1, "sourceSha256": hashlib.sha256(raw).hexdigest(),
                              "qualified": False, "failureType": "TimeoutError"}, receipt)
            self.assertFalse((state / "worker-https-qualified.acknowledged.json").exists())

    def test_rejects_nonpublic_or_ambiguous_worker_endpoints(self):
        for url in ("http://worker.example/symbols", "https://localhost./symbols",
                    "https://127.0.0.1/symbols", "https://[::1]/symbols",
                    "https://10.0.0.1/symbols", "https://worker.example/symbols?x=1"):
            with self.subTest(url=url), self.assertRaises(ValueError):
                publisher.validate_endpoint(url)
        publisher.validate_endpoint("https://worker.example/symbols")

    def test_free_query_only_candidate_preserves_writes_and_refuses_replacement(self):
        source = dict(schema=1,
                      endpoint="https://d1.example/symbols", symbols=publisher.EXPECTED_KEYS, sourceOwner="00000000-0000-0000-0000-000000000123")
        admission = dict(queryOnlyProviders=["IPFSOASIS"], ordinaryProviders=["MongoDBOASIS"],
                         nativeProviders=[], queryTariffs=[dict(provider="IPFSOASIS", costUsd=0)],
                         routes=["retained"], storageTariffs=["retained"])
        before = dict(OASIS_DNA_JSON='{"OASIS":{"Sandbox":{"Enabled":true}}}',
                      PREPARED_STORAGE_ADMISSION_JSON=json.dumps(admission))
        after = publisher.candidate(before, source)
        self.assertEqual(set(publisher.KEYS), {k for k in after if after[k] != before.get(k)})
        enrolled = json.loads(after[publisher.KEYS[1]])
        for key in ("routes", "storageTariffs", "ordinaryProviders", "nativeProviders"):
            self.assertEqual(admission[key], enrolled[key])
        self.assertEqual(["IPFSOASIS", "CloudflareD1OASIS"], enrolled["queryOnlyProviders"])
        self.assertEqual([3, 3, 2, 2], [
            len(row["request"].get("source", row["request"])["filters"][0]["value"])
            for row in json.loads(after[publisher.KEYS[2]])])
        with self.assertRaisesRegex(ValueError, "reconciliation"):
            publisher.candidate(after, source)

    def test_qualification_receipt_is_bound_to_exact_source_bytes(self):
        with tempfile.TemporaryDirectory() as directory, patch.object(publisher, "ROOT", Path(directory)):
            state = Path(directory) / "LocalState/COSMIC/D1WorkerHostedQualificationCurrent"
            state.mkdir(parents=True)
            source = dict(schema=1, endpoint="https://worker.example/symbols",
                          symbols=publisher.EXPECTED_KEYS,
                          sourceOwner="00000000-0000-0000-0000-000000000123")
            raw = json.dumps(source).encode()
            path = state / "source.json"
            path.write_bytes(raw)
            (state / "worker-https-qualified.acknowledged.json").write_text(json.dumps(dict(
                schema=1, sourceSha256=hashlib.sha256(raw).hexdigest(),
                fullSymbols=publisher.EXPECTED_KEYS, subsetSymbols=publisher.EXPECTED_KEYS[:2],
                duplicateStatus=400, unenrolledStatus=400, unboundedStatus=400, writeStatus=405)), encoding="utf-8")
            self.assertEqual(source, publisher.qualified_source())
            path.write_bytes(raw + b"\n")
            with self.assertRaisesRegex(RuntimeError, "Source-bound"):
                publisher.qualified_source()

    def test_prepare_requires_local_evidence_before_railway_access(self):
        with patch.object(publisher, "qualified_source", side_effect=RuntimeError("local proof missing")), \
             patch.object(publisher.configuration, "variables") as variables:
            with self.assertRaisesRegex(RuntimeError, "local proof missing"):
                publisher.prepare()
            variables.assert_not_called()

    def test_apply_refuses_mutation_until_reviewed_release_is_live(self):
        before, after = {}, {"reviewed": "candidate"}
        with patch.object(publisher.configuration, "protected_read", side_effect=[before, after]), \
             patch.object(publisher, "qualified_source", return_value={}), \
             patch.object(publisher, "candidate", return_value=after), \
             patch.object(publisher, "module") as release, \
             patch.object(publisher.configuration, "apply_values") as apply_values:
            gate = release.return_value.require_matching_release
            gate.side_effect = RuntimeError("expected release is not live")
            with self.assertRaisesRegex(RuntimeError, "expected release is not live"):
                publisher.apply()
            gate.assert_called_once_with(publisher.configuration)
            apply_values.assert_not_called()
