"""Acceptance controls for response-owned live failover evidence; no network calls."""
import importlib.util
import json
from pathlib import Path
import unittest

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


if __name__ == "__main__":
    unittest.main()
