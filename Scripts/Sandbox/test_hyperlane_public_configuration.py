import importlib.util
import json
from pathlib import Path
from tempfile import TemporaryDirectory
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location(
    "hyperlane_publisher", Path(__file__).with_name("Publish-HostedHyperlanePublicConfiguration.py"))
publisher = importlib.util.module_from_spec(spec)
spec.loader.exec_module(publisher)


class HyperlaneConfigurationTests(unittest.TestCase):
    def test_free_query_only_candidate_preserves_writes_and_refuses_replacement(self):
        source = dict(schema=1, apiUrl="https://explorer4.hasura.app/v1/graphql",
                      messageIds=publisher.EXPECTED_KEYS, sourceOwner="00000000-0000-0000-0000-000000000123")
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
        self.assertEqual(["IPFSOASIS", "HyperlaneOASIS"], enrolled["queryOnlyProviders"])
        self.assertEqual([3, 3, 2, 2], [
            len(row["request"].get("source", row["request"])["filters"][0]["value"])
            for row in json.loads(after[publisher.KEYS[2]])])
        with self.assertRaisesRegex(ValueError, "reconciliation"):
            publisher.candidate(after, source)

    def test_prepare_requires_local_evidence_before_railway_access(self):
        with patch.object(publisher, "qualified_source", side_effect=RuntimeError("local proof missing")), \
             patch.object(publisher.configuration, "variables") as variables:
            with self.assertRaisesRegex(RuntimeError, "local proof missing"):
                publisher.prepare()
            variables.assert_not_called()

    def test_apply_refuses_mutation_until_reviewed_release_is_live(self):
        before, after = {}, {"reviewed": "candidate"}
        with patch.dict(publisher.os.environ, {"COSMIC_CQL_REVIEWED_RELEASE_REF": "refs/heads/reviewed"}), \
             patch.object(publisher.configuration, "protected_read", side_effect=[before, after]), \
             patch.object(publisher, "qualified_source", return_value={}), \
             patch.object(publisher, "candidate", return_value=after), \
             patch.object(publisher, "module") as release, \
             patch.object(publisher.configuration, "apply_values") as apply_values:
            gate = release.return_value.require_matching_release
            gate.side_effect = RuntimeError("expected release is not live")
            with self.assertRaisesRegex(RuntimeError, "expected release is not live"):
                publisher.apply()
            gate.assert_called_once_with(publisher.configuration, release_ref="refs/heads/reviewed")
            apply_values.assert_not_called()

    def test_prepare_resumes_after_candidate_snapshot_without_overwriting_evidence(self):
        before = {"OASIS_DNA_JSON": "{}"}
        after = dict(before, **{publisher.KEYS[2]: "[]"})
        with TemporaryDirectory() as directory:
            root = Path(directory)
            state = root / "state"
            state.mkdir()
            for suffix in ("before", "candidate"):
                (state / f"{publisher.PREFIX}-{suffix}-variables.dpapi").write_bytes(b"preserved")
            with patch.object(publisher, "ROOT", root), \
                 patch.object(publisher.configuration, "STATE", state), \
                 patch.object(publisher, "qualified_source", return_value={}), \
                 patch.object(publisher.configuration, "variables", return_value=before), \
                 patch.object(publisher, "candidate", return_value=after), \
                 patch.object(publisher.configuration, "protected_read", side_effect=[before, after] * 2):
                publisher.prepare()
                publisher.prepare()
            review = root / "TestResults/HyperlanePublicSourceCurrent/prepared-review.json"
            self.assertEqual(publisher.KEYS, json.loads(review.read_bytes())["changedKeys"])
            self.assertEqual([b"preserved"] * 2, [path.read_bytes() for path in state.glob("*.dpapi")])
