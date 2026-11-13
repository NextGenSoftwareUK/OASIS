"""Routing fixture protocol tests; no hosted calls or native services."""
from pathlib import Path
import importlib.util
import json
import tempfile
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location("routing_setup", Path(__file__).with_name("Prepare-HostedHyperdriveRouting.py"))
setup = importlib.util.module_from_spec(spec)
spec.loader.exec_module(setup)
OWNER = "11111111-1111-4111-8111-111111111111"


class RoutingFixtureTests(unittest.TestCase):
    def setUp(self):
        parent = ROOT / "TestResults/HostedCqlIntegrationCurrent"
        parent.mkdir(parents=True, exist_ok=True)
        self.temp = tempfile.TemporaryDirectory(prefix="owned-routing-protocol-", dir=parent)
        self.addCleanup(self.temp.cleanup)
        self.state = Path(self.temp.name)

    def saved(self, route, token, intent, **kwargs):
        self.assertEqual(route, "data/save-holon")
        self.assertEqual(kwargs["operation"], intent["operationId"])
        self.assertEqual(intent["expectedRevisions"], {intent["holon"]["id"]: None})
        self.assertEqual(intent["autoFailOverMode"], "OFF")
        self.assertEqual(intent["autoLoadBalanceMode"], "OFF")
        self.assertEqual(intent["autoReplicationMode"], "ON")
        self.assertTrue((self.state / "record-save.intent.json").is_file())
        return {"isSaved": True, "result": {**intent["holon"], "version": 1, "createdByAvatarId": OWNER}}

    def test_acknowledged_save_resumes_reads_without_a_second_write(self):
        with patch.object(setup.flow, "call", side_effect=self.saved) as request:
            intent, ack = setup.save_once(self.state, OWNER, "controlled-token")
            request.assert_called_once()
        with patch.object(setup.flow, "call") as request:
            self.assertEqual(setup.save_once(self.state, OWNER, "controlled-token"), (intent, ack))
            request.assert_not_called()

    def test_transport_uncertainty_retains_intent_and_blocks_resubmission(self):
        with patch.object(setup.flow, "call", side_effect=OSError("controlled uncertainty")) as request:
            with self.assertRaises(OSError): setup.save_once(self.state, OWNER, "controlled-token")
            request.assert_called_once()
        self.assertFalse((self.state / "record-save.acknowledged.json").exists())
        with patch.object(setup.flow, "call") as request:
            with self.assertRaises(RuntimeError): setup.save_once(self.state, OWNER, "controlled-token")
            request.assert_not_called()

    def test_incomplete_or_foreign_acknowledgement_is_not_published(self):
        for difference in ({"isSaved": False}, {"isWarning": True}, {"createdByAvatarId": "foreign"}, {"isPublic": True}, {"version": True}):
            with self.subTest(difference=difference):
                with tempfile.TemporaryDirectory(dir=self.state) as folder:
                    state = Path(folder)
                    def response(route, token, intent, **kwargs):
                        result = {"isSaved": True, "result": {**intent["holon"], "version": 1, "createdByAvatarId": OWNER}}
                        for key, value in difference.items():
                            (result if key in ("isSaved", "isWarning") else result["result"])[key] = value
                        return result
                    with patch.object(setup.flow, "call", side_effect=response):
                        with self.assertRaises(RuntimeError): setup.save_once(state, OWNER, "controlled-token")
                    self.assertTrue((state / "record-save.intent.json").exists())
                    self.assertFalse((state / "record-save.acknowledged.json").exists())

    def test_changed_retained_intent_or_owner_prevents_any_request(self):
        with patch.object(setup.flow, "call", side_effect=self.saved):
            intent, ack = setup.save_once(self.state, OWNER, "controlled-token")
        intent["holon"]["name"] = "changed"
        (self.state / "record-save.intent.json").write_text(json.dumps(intent))
        with patch.object(setup.flow, "call") as request:
            with self.assertRaises(RuntimeError): setup.save_once(self.state, OWNER, "controlled-token")
            request.assert_not_called()

    def test_exact_private_replicas_are_required_before_fixture_publication(self):
        with patch.object(setup.flow, "call", side_effect=self.saved):
            intent, ack = setup.save_once(self.state, OWNER, "controlled-token")
        def load(route, token, body):
            self.assertEqual(body["autoFailOverMode"], "OFF")
            self.assertEqual(body["autoLoadBalanceMode"], "OFF")
            return {"result": {**ack["expected"], "description": intent["holon"]["description"], "isPublic": False}}
        with patch.object(setup.flow, "call", side_effect=load) as request:
            result = setup.verify_replicas(intent, ack, "controlled-token")
            self.assertEqual({r["provider"] for r in result}, setup.gate.PROVIDERS)
            self.assertEqual(request.call_count, 3)
        with patch.object(setup.flow, "call", return_value={"result": {**ack["expected"], "isPublic": True}}):
            with self.assertRaises(RuntimeError): setup.verify_replicas(intent, ack, "controlled-token")

    def test_interrupted_replica_check_resumes_only_reads_and_publishes_durable_proof(self):
        (self.state / "owner.acknowledged.json").write_text(json.dumps({"id": OWNER}))
        missing = True
        saves = []
        def request(route, token, body=None, **kwargs):
            nonlocal missing
            if route == "sandbox/faults": return {"result": []}
            if route == "data/save-holon":
                saves.append(body["operationId"])
                return self.saved(route, token, body, **kwargs)
            if route != "data/load-holon": raise AssertionError(route)
            ack = json.loads((self.state / "record-save.acknowledged.json").read_text())
            intent = json.loads((self.state / "record-save.intent.json").read_text())
            if missing and body["providerType"] == "SQLLiteDBOASIS":
                return {"result": None}
            return {"result": {**ack["expected"], "description": intent["holon"]["description"], "isPublic": False}}
        with patch.object(setup.flow, "session", return_value=({"id": OWNER}, "controlled-token")), \
                patch.object(setup.flow, "call", side_effect=request):
            with self.assertRaises(RuntimeError): setup.prepare(self.state)
            self.assertFalse((self.state / "routing-record.acknowledged.json").exists())
            self.assertTrue((self.state / "record-save.acknowledged.json").is_file())
            missing = False
            self.assertEqual(setup.prepare(self.state)["replicas"], 3)
        self.assertEqual(len(saves), 1)
        document = json.loads((self.state / "routing-record.acknowledged.json").read_text())
        self.assertEqual(document["sourceEvidenceSha256"], setup.hashlib.sha256(
            (self.state / "replica-baseline.acknowledged.json").read_bytes()).hexdigest())
        setup.gate.reviewed_record(document, OWNER)

    def test_save_intent_without_original_owner_cannot_create_a_replacement(self):
        (self.state / "record-save.intent.json").write_text("{}")
        with patch.object(setup.flow, "session") as session:
            with self.assertRaises(RuntimeError): setup.prepare(self.state)
            session.assert_not_called()

    def test_completed_setup_never_creates_another_avatar(self):
        (self.state / "routing-record.acknowledged.json").write_text("{}")
        with patch.object(setup.flow, "session") as session:
            with self.assertRaises(RuntimeError): setup.prepare(self.state)
            session.assert_not_called()


if __name__ == "__main__":
    unittest.main()
