import importlib.util
import json
from pathlib import Path
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
