import importlib.util
import json
import os
from pathlib import Path
import tempfile
import unittest
import uuid
from unittest.mock import patch

spec = importlib.util.spec_from_file_location("exact_key_configuration",
    Path(__file__).with_name("Prepare-HostedExactKeyConfiguration.py"))
configuration = importlib.util.module_from_spec(spec)
spec.loader.exec_module(configuration)


class ExactKeyConfigurationTests(unittest.TestCase):
    def test_candidate_preserves_existing_providers_and_refuses_replacement(self):
        self.check_candidate("QdrantOASIS")

    def test_weaviate_candidate_preserves_existing_providers(self):
        self.check_candidate("WeaviateOASIS")

    def test_tigris_candidate_preserves_existing_providers(self):
        self.check_candidate("TigrisOASIS")

    def test_redis_family_candidates_preserve_existing_providers(self):
        for provider in ("GarnetOASIS", "KeyDBOASIS"):
            with self.subTest(provider=provider):
                self.check_candidate(provider)

    def check_candidate(self, provider):
        partition = "CQL_HOSTED_QUERY_EXAMPLES__" + provider
        baseline = {
            "OASIS_DNA_JSON": '{"OASIS":{"Sandbox":{"Enabled":true}}}',
            configuration.ADMISSION: json.dumps({
                "ordinaryProviders": ["MongoDBOASIS"], "nativeProviders": [],
                "queryOnlyProviders": ["IPFSOASIS"],
                "queryTariffs": [{"provider": "IPFSOASIS", "costUsd": 0, "tariffVersion": "demo-read-v1"}],
            }),
            configuration.GRANTS: json.dumps({"IPFSOASIS": ["retained"]}),
            configuration.EXAMPLES: json.dumps({"IPFSOASIS": ["retained"]}),
            "UNRELATED_SETTING": "retained",
        }
        with tempfile.TemporaryDirectory() as directory:
            descriptor = Path(directory) / "catalogue.json"
            actor, first, second, third, deleted, missing = [str(uuid.UUID(int=i)) for i in range(1, 7)]
            catalogue = {"Actor": actor, "LiveIds": [first, second, third], "DeletedId": deleted, "MissingId": missing}
            if provider == "TigrisOASIS":
                catalogue.update(Endpoint="https://t3.storage.dev", Bucket="teaching", Originals=[
                    {"Id": identifier, "Sha256": "a" * 64, "ETag": "controlled-etag"}
                    for identifier in (first, second, third, deleted)])
                for invalid in (catalogue | {"Originals": catalogue["Originals"][:-1]},
                                catalogue | {"Endpoint": "https://t3.storage.dev.other.example"}):
                    descriptor.write_text(json.dumps(invalid))
                    with self.assertRaisesRegex(RuntimeError, "reviewed teaching catalogue"):
                        configuration.verifier.reviewed_exact_key_source(provider, descriptor)
            descriptor.write_text(json.dumps(catalogue))
            review = configuration.verifier.reviewed_exact_key_source(provider, descriptor)
            if provider in ("TigrisOASIS", "WeaviateOASIS", "GarnetOASIS", "KeyDBOASIS"):
                with self.assertRaisesRegex(ValueError, "do not match"):
                    configuration.candidate(baseline, provider, review)
                if provider == "TigrisOASIS":
                    baseline.update(CQL_TIGRIS_QUERY_ORIGIN="https://t3.storage.dev",
                                    CQL_TIGRIS_QUERY_BUCKET="teaching", CQL_TIGRIS_QUERY_PROBE_ID=first)
                    with self.assertRaisesRegex(ValueError, "do not match"):
                        configuration.candidate(baseline | {"CQL_TIGRIS_QUERY_BUCKET": "foreign"}, provider, review)
                else:
                    product = provider.removesuffix("OASIS").upper()
                    baseline[f"CQL_{product}_QUERY_PROBE_ID"] = first
            after = configuration.candidate(baseline, provider, review)
            self.assertEqual({configuration.ADMISSION, configuration.GRANTS, partition},
                             {key for key, value in after.items() if baseline.get(key) != value})
            admission = json.loads(after[configuration.ADMISSION])
            self.assertEqual(["IPFSOASIS", provider], admission["queryOnlyProviders"])
            self.assertEqual(dict(admission["queryTariffs"][0], provider=provider), admission["queryTariffs"][1])
            self.assertEqual({"IPFSOASIS": ["retained"], provider: [first, second, third]},
                             json.loads(after[configuration.GRANTS]))
            self.assertEqual(review["presets"], json.loads(after[partition]))
            for changed in (after, baseline | {partition: "[]"},
                            baseline | {configuration.EXAMPLES: json.dumps({provider: []})}):
                with self.assertRaisesRegex(ValueError, "reconciliation"):
                    configuration.candidate(changed, provider, review)
            priced = json.loads(baseline[configuration.ADMISSION])
            priced["queryTariffs"][0]["costUsd"] = 1
            with self.assertRaisesRegex(ValueError, "zero-cost"):
                configuration.candidate(baseline | {configuration.ADMISSION: json.dumps(priced)}, provider, review)
            if os.name == "nt":
                protect = configuration.configuration.verification.protected_token
                original = Path(directory) / "baseline.dpapi"
                output = Path(directory) / "candidate.dpapi"
                original.write_bytes(protect(json.dumps(baseline).encode()))
                with patch("sys.argv", ["prepare", provider, "--catalogue", str(descriptor),
                                        "--baseline", str(original), "--output", str(output)]):
                    configuration.main()
                    with self.assertRaises(FileExistsError):
                        configuration.main()
                self.assertEqual(after, json.loads(protect(output.read_bytes(), decrypt=True)))
