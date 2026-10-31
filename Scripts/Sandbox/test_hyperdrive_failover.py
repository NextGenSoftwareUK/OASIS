"""Acceptance controls for response-owned live failover evidence; no network calls."""
import importlib.util
import json
from pathlib import Path
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location("failover_gate", Path(__file__).with_name("Verify-HostedHyperdriveFailover.py"))
gate = importlib.util.module_from_spec(spec)
spec.loader.exec_module(gate)


class FailoverEvidenceTests(unittest.TestCase):
    def setUp(self):
        self.expected = {"id": "record", "name": "acknowledged", "version": 1, "createdByAvatarId": "owner"}
        self.route = {"RequestedProvider": "MongoDBOASIS", "InitialProvider": "MongoDBOASIS",
            "ServingProvider": "SQLLiteDBOASIS", "RequestedStrategy": "Auto", "Succeeded": True}

    def response(self, route=None):
        return {"isError": False, "result": dict(self.expected),
            "metaData": {"hyperDriveRoute": json.dumps(route or self.route)}}

    def test_accepts_each_directed_replica_route(self):
        for source in gate.PROVIDERS:
            for target in gate.PROVIDERS:
                if source == target:
                    continue
                route = dict(self.route, RequestedProvider=source, InitialProvider=source, ServingProvider=target)
                with self.subTest(source=source, target=target):
                    self.assertEqual(target, gate.verify_failover_response(self.response(route), self.expected, source)["ServingProvider"])

    def test_rejects_same_provider_despite_correct_record(self):
        route = dict(self.route, ServingProvider="MongoDBOASIS")
        with self.assertRaises(ValueError):
            gate.verify_failover_response(self.response(route), self.expected, "MongoDBOASIS")

    def test_rejects_missing_response_evidence(self):
        response = self.response()
        response["metaData"] = {}
        with self.assertRaises(ValueError):
            gate.verify_failover_response(response, self.expected, "MongoDBOASIS")

    def test_rejects_wrong_request_or_unenrolled_route(self):
        for key, value in [("RequestedProvider", "Default"), ("InitialProvider", "Neo4jOASIS"),
                ("ServingProvider", "SolanaOASIS"), ("RequestedStrategy", "Other"), ("Succeeded", False)]:
            with self.subTest(field=key), self.assertRaises(ValueError):
                gate.verify_failover_response(self.response(dict(self.route, **{key: value})), self.expected, "MongoDBOASIS")

    def test_rejects_error_or_changed_acknowledged_record(self):
        for key in self.expected:
            response = self.response()
            response["result"][key] = "changed"
            with self.subTest(field=key), self.assertRaises(ValueError):
                gate.verify_failover_response(response, self.expected, "MongoDBOASIS")
        response = self.response()
        response["isError"] = True
        with self.assertRaises(ValueError):
            gate.verify_failover_response(response, self.expected, "MongoDBOASIS")


    def test_foreign_setup_cannot_touch_original_solana_cohort(self):
        with patch("sys.argv", ["gate", "--prepare-foreign"]), patch.object(gate.flow, "session") as session:
            with self.assertRaises(RuntimeError):
                gate.main()
            session.assert_not_called()

    def test_expired_owner_prevents_foreign_visitor_creation(self):
        with patch("sys.argv", ["gate", "--cohort", "routing-v1", "--prepare-foreign"]), \
                patch.object(Path, "is_file", return_value=True), \
                patch.object(gate.flow, "session", side_effect=RuntimeError("expired")) as session:
            with self.assertRaises(RuntimeError):
                gate.main()
            session.assert_called_once_with(gate.ROOT / "LocalState/COSMIC/HostedHyperdriveRoutingCurrent", "owner", allow_create=False)

    def test_existing_gate_journal_stops_before_any_session_or_fault(self):
        with patch("sys.argv", ["gate", "--cohort", "routing-v1"]), \
                patch.object(Path, "exists", return_value=True), \
                patch.object(gate.flow, "session") as session, patch.object(gate.flow, "call") as request:
            with self.assertRaises(RuntimeError):
                gate.main()
            session.assert_not_called()
            request.assert_not_called()


if __name__ == "__main__":
    unittest.main()
