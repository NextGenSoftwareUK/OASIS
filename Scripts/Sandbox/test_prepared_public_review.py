"""No credentials, protected files, network calls or deployments."""
import copy
import importlib.util
import json
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location("review", Path(__file__).with_name("Review-PreparedPublicBatch.py"))
review = importlib.util.module_from_spec(spec)
spec.loader.exec_module(review)


class PublicReviewTests(unittest.TestCase):
    def fixture(self):
        candidate = {"OASIS_DNA_JSON": "inert-private-marker", "PREPARED_STORAGE_ADMISSION_JSON": "do-not-export"}
        examples = {}
        for provider, (key, fields) in review.PROFILE_KEYS.items():
            source = {field: "public-test-value" for field in fields}
            for field in ("rpcUrl", "restUrl"):
                if field in source:
                    source[field] = "https://public.example.invalid/"
            candidate[key] = json.dumps(source)
            examples[provider] = []
        candidate["CQL_HOSTED_QUERY_EXAMPLES_JSON"] = json.dumps(examples)
        return candidate

    def test_review_omits_private_settings_and_preserves_the_input(self):
        candidate = self.fixture()
        original = copy.deepcopy(candidate)
        result = review.public_review(candidate)
        self.assertEqual(candidate, original)
        self.assertEqual(set(result), {"profiles", "examples"})
        self.assertEqual(len(result["profiles"]), 7)
        self.assertNotIn("inert-private-marker", json.dumps(result))
        self.assertNotIn("do-not-export", json.dumps(result))

    def test_unexpected_source_credentials_cannot_enter_the_export(self):
        candidate = self.fixture()
        source = json.loads(candidate["CQL_NEAR_PUBLIC_SOURCE_JSON"])
        source["privateKey"] = "inert-secret-marker"
        candidate["CQL_NEAR_PUBLIC_SOURCE_JSON"] = json.dumps(source)
        with self.assertRaises(ValueError):
            review.public_review(candidate)

    def test_endpoint_credentials_and_token_paths_are_rejected(self):
        for url in ("https://user:password@example.invalid/", "https://example.invalid/?token=secret",
                    "https://example.invalid/#token", "https://example.invalid/private-token-path"):
            with self.subTest(url=url):
                candidate = self.fixture()
                source = json.loads(candidate["CQL_NEAR_PUBLIC_SOURCE_JSON"])
                source["rpcUrl"] = url
                candidate["CQL_NEAR_PUBLIC_SOURCE_JSON"] = json.dumps(source)
                with self.assertRaises(ValueError):
                    review.public_review(candidate)


if __name__ == "__main__":
    unittest.main()
