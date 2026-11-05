"""Query-only raw response retention before interpretation, no live requests."""
import base64
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
