"""Query-only raw response retention before interpretation, no live requests."""
import base64
import hashlib
import importlib.util
import json
import io
from email.message import Message
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch, Mock

spec = importlib.util.spec_from_file_location("query_verification", Path(__file__).with_name("Verify-HostedQuerySource.py"))
verification = importlib.util.module_from_spec(spec)
spec.loader.exec_module(verification)


class ResponseEvidenceTests(unittest.TestCase):
    def test_localfile_receipt_recovers_bound_reports_without_opening_sessions(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            state = root / "LocalState/COSMIC/HostedQueryAcceptance-LocalFileOASIS"
            results = root / "TestResults/HostedCqlIntegrationCurrent"
            state.mkdir(parents=True)
            results.mkdir(parents=True)
            for label, identity in (("owner", "a"), ("foreign", "b")):
                (state / (label + ".acknowledged.json")).write_text(json.dumps({"id": identity}))
            native, paired, isolation = [results / ("LocalFileOASIS-" + suffix) for suffix in
                ("native-http.json", "client-http-flow.json", "isolation-http.json")]
            native.write_bytes(b"native")
            paired.write_bytes(b"paired")
            mount = {"deployment": "reviewed", "imageDigest": "sha256:" + "a" * 64}
            binding = {"ownerId": "a", "mount": mount, "priorEvidenceSha256": {
                path.name: hashlib.sha256(path.read_bytes()).hexdigest() for path in (native, paired)}}
            isolation.write_text(json.dumps({"avatar": {"id": "b"}, "localFileAcceptance": binding}))
            with (
                patch.object(verification, "ROOT", root),
                patch("sys.argv", ["verify", "LocalFileOASIS", "--recover-localfile-receipt"]),
                patch.object(verification, "session") as session,
                patch.object(verification, "call") as call,
                patch.object(verification, "localfile_review", return_value={"mountedReceipt": mount}),
            ):
                paired.write_bytes(b"changed")
                with self.assertRaisesRegex(RuntimeError, "paired report differs"):
                    verification.main()
                paired.write_bytes(b"paired")
                verification.main()
                session.assert_not_called()
                call.assert_not_called()
            receipt = json.loads((state / "cql-http-acceptance.acknowledged.json").read_bytes())
            self.assertEqual("reviewed", receipt["deployment"])
            self.assertEqual({"a", "b"}, {receipt["ownerId"], receipt["foreignId"]})
            self.assertEqual(3, len(receipt["evidenceSha256"]))

    def test_localfile_catalogue_guard_uses_reviewed_native_and_paired_ids(self):
        provider = "LocalFileOASIS"
        presets = []
        for suffix, ids in (("records", ["a", "b", "c"]), ("subset", ["a", "b"])):
            selection = {"filters": [{"field": "Id", "scalar": "Guid", "operator": "In", "value": ids}]}
            presets.extend([
                {"id": f"public-{suffix}", "request": dict(selection, provider=provider)},
                {"id": f"mongo-public-{suffix}", "operation": "Federation", "request": {"source": selection,
                     "providers": ["MongoDBOASIS", provider], "scope": "Named",
                     "identityMode": "PreserveCopies"}}
            ])
        review = {"presets": presets}
        source = {"examples": json.loads(json.dumps(presets))}
        source["examples"][0]["request"]["skip"] = 0
        verification.require_reviewed_presets("LocalFile", source, review)
        for index, nested, field, value in (
            (0, False, "skip", 1),
            (0, False, "take", 2),
            (0, False, "includeMetadata", True),
            (3, True, "orderBy", "Id"),
            (3, True, "descending", True),
            (3, False, "maximumProviderAttempts", 4),
        ):
            with self.subTest(field=field):
                drifted = {"examples": json.loads(json.dumps(presets))}
                request = drifted["examples"][index]["request"]
                (request["source"] if nested else request)[field] = value
                with self.assertRaisesRegex(RuntimeError, "differs from the reviewed rollout"):
                    verification.require_reviewed_presets("LocalFile", drifted, review)
        source["examples"].append(source["examples"][0])
        with self.assertRaisesRegex(RuntimeError, "preset IDs must be unique"):
            verification.require_reviewed_presets("LocalFile", source, review)
        for field, value in (("scope", "All"), ("identityMode", "Deduplicate")):
            with self.subTest(field=field):
                source = {"examples": json.loads(json.dumps(presets))}
                source["examples"][3]["request"][field] = value
                with self.assertRaisesRegex(RuntimeError, "mongo-public-subset differs"):
                    verification.require_reviewed_presets("LocalFile", source, review)
        source = {"examples": json.loads(json.dumps(presets))}
        source["examples"][3]["operation"] = "Rows"
        with self.assertRaisesRegex(RuntimeError, "mongo-public-subset differs"):
            verification.require_reviewed_presets("LocalFile", source, review)
        source = {"examples": json.loads(json.dumps(presets))}
        source["examples"][3]["request"]["source"]["filters"][0]["value"][1] = "different"
        with self.assertRaisesRegex(RuntimeError, "mongo-public-subset differs"):
            verification.require_reviewed_presets("LocalFile", source, review)

    def test_unavailable_localfile_does_not_create_a_cohort(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            with patch.object(verification, "ROOT", root), \
                 patch("sys.argv", ["verify", "LocalFileOASIS"]), \
                 patch.object(verification, "session", return_value=({}, "retained")) as session, \
                 patch.object(verification, "ready_source", side_effect=RuntimeError("unavailable")) as ready:
                with self.assertRaisesRegex(RuntimeError, "unavailable"):
                    verification.main()
            session.assert_called_once_with(root / "LocalState/COSMIC/HostedDemoRehearsalDiscovery",
                                            "visitor", allow_create=False)
            ready.assert_called_once_with("LocalFileOASIS", "retained", require_pair=True)
            self.assertEqual([], list(root.iterdir()))

    def test_localfile_completion_is_bound_to_distinct_visitors_and_the_current_mount(self):
        with tempfile.TemporaryDirectory() as directory:
            state = Path(directory)
            reports = [state / name for name in ("native.json", "paired.json", "privacy.json")]
            for report in reports:
                report.write_bytes(report.name.encode())
            evidence = {path.name: hashlib.sha256(path.read_bytes()).hexdigest() for path in reports}
            mount = {"deployment": "reviewed", "imageDigest": "sha256:" + "a" * 64}
            with patch.object(verification, "localfile_review", return_value={"mountedReceipt": mount}) as review:
                with self.assertRaisesRegex(RuntimeError, "distinct visitor"):
                    verification.acknowledge_localfile_http(state, "owner", "owner", mount, evidence)
                review.return_value = {"mountedReceipt": dict(mount, deployment="superseded")}
                with self.assertRaisesRegex(RuntimeError, "deployment changed"):
                    verification.acknowledge_localfile_http(state, "owner", "visitor", mount, evidence)
                receipt = state / "cql-http-acceptance.acknowledged.json"
                self.assertFalse(receipt.exists())
                review.return_value = {"mountedReceipt": mount}
                verification.acknowledge_localfile_http(state, "owner", "visitor", mount, evidence)
                original = receipt.read_bytes()
                self.assertEqual(evidence, json.loads(original)["evidenceSha256"])
                with self.assertRaises(FileExistsError):
                    verification.acknowledge_localfile_http(state, "owner", "visitor", mount, evidence)
                self.assertEqual(original, receipt.read_bytes())

    def test_localfile_mount_refusal_precedes_cohort_creation(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            with (
                patch.object(verification, "ROOT", root),
                patch("sys.argv", ["verify", "LocalFileOASIS"]),
                patch.object(verification, "session", return_value=({}, "retained")) as session,
                patch.object(verification, "ready_source", return_value={"examples": []}),
                patch.object(verification, "localfile_review", side_effect=RuntimeError("Missing mount receipt")) as review,
            ):
                with self.assertRaisesRegex(RuntimeError, "Missing mount receipt"):
                    verification.main()
            review.assert_called_once_with(require_mount=True)
            session.assert_called_once_with(root / "LocalState/COSMIC/HostedDemoRehearsalDiscovery",
                                            "visitor", allow_create=False)
            self.assertEqual([], list(root.iterdir()))

    def test_pouchdb_review_binds_presets_to_the_qualified_source(self):
        ids = ["11111111-1111-4111-8111-111111111111", "22222222-2222-4222-8222-222222222222",
               "33333333-3333-4333-8333-333333333333"]
        bundle = json.dumps({"records": [{"id": identity} for identity in ids]})
        digest = hashlib.sha256(bundle.encode()).hexdigest()
        presets = []
        for suffix, count in (("records", 3), ("subset", 2)):
            selection = {"filters": [{"field": "Id", "scalar": "Guid", "operator": "In", "value": ids[:count]}]}
            presets.extend([
                {"id": "public-" + suffix, "request": dict(selection, provider="PouchDBOASIS")},
                {"id": "mongo-public-" + suffix, "operation": "Federation", "request": {
                    "source": selection, "providers": ["MongoDBOASIS", "PouchDBOASIS"],
                    "scope": "Named", "identityMode": "PreserveCopies"}}])
        settings = {"OASIS_SANDBOX_POUCHDB": "true", "CQL_POUCHDB_QUERY_SOURCE_DIGEST": digest,
                    "OASIS_SANDBOX_POUCHDB_BOOTSTRAP_JSON": bundle,
                    "CQL_HOSTED_QUERY_EXAMPLES__PouchDBOASIS": json.dumps(presets)}
        with tempfile.TemporaryDirectory() as directory, patch.object(verification, "ROOT", Path(directory)):
            proof = Path(directory) / "LocalState/COSMIC/RecoveryLaptopPouchDBCql/hosted-preparation/native-source-qualified.json"
            proof.parent.mkdir(parents=True)
            proof.write_text(json.dumps({"sourceBundleSha256": digest}), encoding="utf-8")
            review = Path(directory) / "review.json"
            review.write_text(json.dumps(settings), encoding="utf-8")
            self.assertEqual({"presets": presets}, verification.pouchdb_review(review))
            settings["CQL_POUCHDB_QUERY_SOURCE_DIGEST"] = "0" * 64
            review.write_text(json.dumps(settings), encoding="utf-8")
            with self.assertRaisesRegex(RuntimeError, "qualified source"):
                verification.pouchdb_review(review)
            settings["CQL_POUCHDB_QUERY_SOURCE_DIGEST"] = digest
            presets = json.loads(json.dumps(presets))
            presets[3]["request"]["source"]["filters"][0]["value"] = ids[1:]
            settings["CQL_HOSTED_QUERY_EXAMPLES__PouchDBOASIS"] = json.dumps(presets)
            review.write_text(json.dumps(settings), encoding="utf-8")
            with self.assertRaisesRegex(RuntimeError, "mongo-public-subset differs"):
                verification.pouchdb_review(review)

    def test_pouchdb_discovery_mismatch_does_not_create_a_cohort(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            with (
                patch.object(verification, "ROOT", root),
                patch("sys.argv", ["verify", "PouchDBOASIS", "--pouchdb-reviewed-settings", "review.json"]),
                patch.object(verification, "pouchdb_review", return_value={"presets": []}),
                patch.object(verification, "session", return_value=({}, "retained")) as session,
                patch.object(verification, "ready_source", return_value={"examples": []}),
            ):
                with self.assertRaisesRegex(RuntimeError, "differs from the reviewed rollout"):
                    verification.main()
            session.assert_called_once_with(root / "LocalState/COSMIC/HostedDemoRehearsalDiscovery",
                                            "visitor", allow_create=False)
            self.assertEqual([], list(root.iterdir()))

    def test_foreign_visibility_rejects_replica_leak(self):
        providers = ["MongoDBOASIS", "SQLLiteDBOASIS", "Neo4jOASIS"]
        ids = ["11111111-1111-4111-8111-111111111111", "22222222-2222-4222-8222-222222222222"]
        empty = {"result": {"rows": []}}
        with patch.object(verification, "call", return_value=empty) as request:
            self.assertEqual(dict.fromkeys(providers, empty),
                             verification.verify_private_visibility("visitor", ids, providers))
            for provider, observed in zip(providers, request.call_args_list, strict=True):
                self.assertEqual(provider, observed.args[2]["provider"])
                self.assertEqual(ids, observed.args[2]["filters"][0]["value"])
        with patch.object(verification, "call", side_effect=[empty, {"result": {"rows": [{"id": ids[0]}]}}]):
            with self.assertRaisesRegex(RuntimeError, "SQLLiteDBOASIS"):
                verification.verify_private_visibility("visitor", ids, providers)

    def test_paired_readiness_rejects_an_unavailable_preset(self):
        provider = "LocalFileOASIS"
        examples = [{"id": key, "available": key != "mongo-public-subset"} for key in (
            "public-records", "public-subset", "mongo-public-records", "mongo-public-subset"
        )]
        source = {"provider": provider, "available": True, "federationAvailable": True, "examples": examples}
        with patch.object(verification, "call", return_value={"result": [source]}) as request:
            self.assertIs(source, verification.ready_source(provider, "test", require_pair=False))
            with self.assertRaisesRegex(RuntimeError, "missing required hosted query presets"):
                verification.ready_source(provider, "test", require_pair=True)
            source["federationAvailable"] = False
            self.assertIs(source, verification.ready_source(provider, "test", require_pair=False))
            with self.assertRaisesRegex(RuntimeError, "absent or unavailable"):
                verification.ready_source(provider, "test", require_pair=True)
            examples[0]["available"] = "false"
            with self.assertRaisesRegex(RuntimeError, "invalid hosted preset availability"):
                verification.ready_source(provider, "test", require_pair=False)
            examples[0]["available"] = True
            examples.append(dict(examples[0], available=False))
            with self.assertRaisesRegex(RuntimeError, "duplicate hosted preset IDs"):
                verification.ready_source(provider, "test", require_pair=False)
            request.return_value = {"result": [source, source]}
            with self.assertRaisesRegex(RuntimeError, "more than once"):
                verification.ready_source(provider, "test", require_pair=False)
            self.assertTrue(all(call.kwargs["method"] == "GET" for call in request.call_args_list))

    def test_native_selection_requires_exact_distinct_preset_ids(self):
        keys = [f"11111111-1111-4111-8111-11111111111{index}" for index in range(3)]
        examples = [{"request": {"filters": [
            {"field": "Id", "scalar": "Guid", "operator": "In", "value": keys[:count]}
        ]}} for count in (3, 2)]
        responses = [{"result": {"rows": [{"id": key} for key in keys[:count]]}} for count in (3, 2)]
        verification.require_native_selection(responses, examples)
        for replacement in (keys[0], "22222222-2222-4222-8222-222222222222"):
            with self.subTest(replacement=replacement):
                responses[0]["result"]["rows"][2]["id"] = replacement
                with self.assertRaisesRegex(RuntimeError, "IDs differ"):
                    verification.require_native_selection(responses, examples)

    def test_native_rows_require_exact_metadata_keys(self):
        for key in ("HyperlaneMessageId", "QuestDbSymbol", "D1Symbol"):
            with self.subTest(key=key):
                example = {"request": {"includeMetadata": True, "filters": [
                    {"metadataPath": [key], "scalar": "String", "operator": "In", "value": ["a", "b"]}
                ]}}
                rows = [{"metadata": {key: value}} for value in ("a", "b")]
                response = {"result": {"rows": rows}}
                verification.require_native_rows(response, example, 2)
                rows[1]["metadata"][key] = "c"
                with self.assertRaisesRegex(RuntimeError, "metadata keys differ"):
                    verification.require_native_rows(response, example, 2)

    def test_http_failure_is_captured_before_error_is_raised_without_retry(self):
        raw = b'{"isError":true,"errorCode":"CQL_QUERY_FAILED"}'
        class Response(io.BytesIO):
            status = 503
            headers = Message()
        response = Response(raw)
        response.headers['Access-Control-Allow-Origin'] = verification.BROWSER_ORIGIN
        response.headers['Access-Control-Allow-Credentials'] = 'true'
        opener = Mock(); opener.open.return_value = response
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory); target = root / 'TestResults/failed.json'; target.parent.mkdir()
            with patch.object(verification, 'ROOT', root), patch.object(verification.urllib.request, 'build_opener', return_value=opener):
                with self.assertRaisesRegex(RuntimeError, '503/CQL_QUERY_FAILED'):
                    verification.call('data/query-cql', token='inert-test-bearer', evidence_path=target)
            proof = json.loads(target.read_text(encoding='utf-8'))
            self.assertEqual(base64.b64decode(proof['bodyBase64']), raw)
            self.assertNotIn('inert-test-bearer', target.read_text(encoding='utf-8'))
            opener.open.assert_called_once()

    def test_error_or_invalid_json_bytes_are_retained_exactly_without_credentials(self):
        for status, raw in [(503, b'{"errorCode":"CQL_QUERY_FAILED"}'), (200, b'not-json\xff')]:
            with self.subTest(status=status), tempfile.TemporaryDirectory() as directory:
                root = Path(directory); target = root / "TestResults/response.json"; target.parent.mkdir()
                with patch.object(verification, "ROOT", root):
                    verification.capture_query_response(target, "data/query-cql", "operation-id", status, raw)
                proof = json.loads(target.read_text(encoding="utf-8"))
                self.assertEqual(base64.b64decode(proof["bodyBase64"]), raw)
                self.assertEqual(proof["status"], status)
                self.assertEqual(set(proof), {"route", "operationId", "status", "bodyBase64", "bodyTruncated"})

    def test_auth_routes_and_docs_destinations_reject(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            with patch.object(verification, "ROOT", root):
                for route, target in [("sandbox/session", root / "TestResults/x.json"),
                                      ("data/query-cql", root / "Docs/WIP/x.json")]:
                    with self.assertRaises(ValueError):
                        verification.capture_query_response(target, route, "op", 200, b'{}')
                with self.assertRaises(ValueError):
                    verification.call("sandbox/session", evidence_path=root / "TestResults/x.json")

    def test_existing_observation_cannot_be_overwritten(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory); target = root / "TestResults/x.json"; target.parent.mkdir()
            with patch.object(verification, "ROOT", root):
                verification.capture_query_response(target, "data/query-cql", "op", 200, b'{}')
                with self.assertRaises(FileExistsError):
                    verification.capture_query_response(target, "data/query-cql", "other-op", 503, b'other')
                with self.assertRaises(FileExistsError):
                    verification.call("data/query-cql", evidence_path=target)


if __name__ == "__main__":
    unittest.main()
