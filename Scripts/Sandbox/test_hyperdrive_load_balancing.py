"""Controlled acceptance-verifier checks; no HTTP, credentials or native data."""
import importlib.util
import json
from pathlib import Path
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location("gate", Path(__file__).with_name("Verify-HostedHyperdriveLoadBalancing.py"))
gate = importlib.util.module_from_spec(spec)
spec.loader.exec_module(gate)
write_spec = importlib.util.spec_from_file_location("write_gate", Path(__file__).with_name("Verify-HostedHyperdriveWriteFault.py"))
write_gate = importlib.util.module_from_spec(write_spec)
write_spec.loader.exec_module(write_gate)


class RouteEvidenceTests(unittest.TestCase):
    def test_read_only_session_cannot_create_an_avatar_when_acknowledgement_is_missing(self):
        with patch.object(Path, "exists", return_value=False), patch.object(gate.flow, "call") as request, \
                patch.object(gate.flow, "write_new") as write:
            with self.assertRaises(RuntimeError):
                gate.flow.session(Path("controlled-unused-state"), "owner", allow_create=False)
            request.assert_not_called()
            write.assert_not_called()

    def test_read_only_expired_session_never_creates_or_sends_a_request(self):
        import base64
        claims = base64.urlsafe_b64encode(json.dumps({"exp": 1}).encode()).decode().rstrip("=")
        with patch.object(Path, "exists", return_value=True), patch.object(Path, "read_bytes", return_value=b"controlled"), \
                patch.object(gate.flow.verification, "protected_token", return_value=("header." + claims + ".signature").encode()), \
                patch.object(gate.flow, "call") as request, patch.object(gate.flow, "write_new") as write:
            with self.assertRaises(RuntimeError):
                gate.flow.session(Path("controlled-unused-state"), "owner", allow_create=False)
            request.assert_not_called()
            write.assert_not_called()

    def test_reviewed_fixture_binds_durable_record_to_acknowledged_owner(self):
        owner = "11111111-1111-4111-8111-111111111111"
        original = {"schema": 1, "sourceEvidenceSha256": "A" * 64, "expected": {
            "id": "22222222-2222-4222-8222-222222222222", "name": "Coffee", "version": 1,
            "createdByAvatarId": owner}}
        self.assertEqual(gate.reviewed_record(original, owner), original["expected"])
        for changed in ({"schema": 2}, {"sourceEvidenceSha256": "not-a-hash"},
                {"expected": {**original["expected"], "createdByAvatarId": "foreign"}},
                {"expected": {**original["expected"], "version": True}},
                {"expected": {**original["expected"], "id": "00000000-0000-0000-0000-000000000000"}}):
            with self.subTest(changed=changed), self.assertRaises(ValueError):
                gate.reviewed_record({**original, **changed}, owner)

    def test_only_explicit_unsaved_failure_outcomes_can_be_reviewed_for_resume(self):
        for code in write_gate.SAFE_REJECTIONS:
            outcome = {"isError": True, "isSaved": False, "returnedId": None, "errorCode": code}
            self.assertTrue(write_gate.rejected(outcome))
            for key, value in (("isError", False), ("isSaved", True), ("returnedId", "unexpected"), ("errorCode", "unknown")):
                self.assertFalse(write_gate.rejected({**outcome, key: value}))

    def test_write_transport_binds_original_operation_key_and_never_retries_unknown_outcome(self):
        calls = []
        def uncertain(request, **options):
            calls.append(request)
            raise OSError("controlled transport uncertainty")
        with patch.object(write_gate.failover.urllib.request, "urlopen", uncertain):
            with self.assertRaises(OSError):
                write_gate.failover.raw_request("controlled-token", "data/save-holon", {"operationId": "stable-write-key"})
        self.assertEqual(len(calls), 1)
        self.assertEqual(calls[0].get_header("Idempotency-key"), "stable-write-key")

    def test_explicit_and_default_requests_require_their_own_matching_route(self):
        expected = {"id": "owned", "name": "Coffee", "version": 1, "createdByAvatarId": "actor"}
        for requested in ("Default", "MongoDBOASIS"):
            route = {"RequestedProvider": requested, "InitialProvider": "MongoDBOASIS",
                "ServingProvider": "MongoDBOASIS", "Succeeded": True, "RequestedStrategy": "Auto"}
            response = {"result": expected, "metaData": {"hyperDriveRoute": json.dumps(route)}}
            self.assertEqual(gate.verify_response(response, expected, requested), route)

    def test_missing_shared_foreign_failed_or_different_provider_evidence_is_rejected(self):
        expected = {"id": "owned", "name": "Coffee", "version": 1, "createdByAvatarId": "actor"}
        for fault in ("missing", "shared", "foreign", "failed", "different", "unenrolled"):
            with self.subTest(fault=fault):
                route = {"RequestedProvider": "MongoDBOASIS", "InitialProvider": "MongoDBOASIS",
                    "ServingProvider": "MongoDBOASIS", "Succeeded": True, "RequestedStrategy": "Auto"}
                row = dict(expected)
                if fault == "foreign": row["createdByAvatarId"] = "other"
                if fault == "failed": route["Succeeded"] = False
                if fault == "different": route["ServingProvider"] = "Neo4jOASIS"
                if fault == "unenrolled": route["ServingProvider"] = "SolanaOASIS"
                metadata = {"hyperDriveRoute": json.dumps(route)} if fault not in ("missing", "shared") else {}
                response = {"result": row, "metaData": metadata, "lastProviderSelection": route}
                with self.assertRaises(ValueError): gate.verify_response(response, expected, "MongoDBOASIS")


if __name__ == "__main__":
    unittest.main()
