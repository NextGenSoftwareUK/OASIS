"""Offline security checks;no account,HTTP,DPAPI credential or native source is used."""
from pathlib import Path
import base64
import importlib.util
import json
import tempfile
import types
import unittest
import HostedSessionCredentials as credentials

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location("signin", Path(__file__).with_name("Prepare-CqlRecoveryOperatorSession.py"))
signin = importlib.util.module_from_spec(spec)
spec.loader.exec_module(signin)


class OperatorSignInTests(unittest.TestCase):
    def setUp(self):
        parent = ROOT / "TestResults/CqlSourceRecoveryCurrent/operator-signin-tests"
        parent.mkdir(exist_ok=True)
        self.temp = tempfile.TemporaryDirectory(dir=parent)
        self.addCleanup(self.temp.cleanup)
        self.state = Path(self.temp.name) / "state"
        self.owner = "00000000-0000-0000-0000-000000000123"
        payload = base64.urlsafe_b64encode(json.dumps({"sub": self.owner, "exp": 9999999999}).encode()).decode().rstrip("=")
        self.token = "synthetic." + payload + ".not-a-signature"
        self.calls = []
        self.cookies = ["refreshToken=synthetic-cookie; Path=/; Secure; HttpOnly; SameSite=Lax"]

        def call(route, **options):
            self.calls.append(route)
            options["capture_auth"](200, b"synthetic-auth-response", self.cookies)
            return {"result": {"id": self.owner, "jwtToken": self.token}}

        self.query = types.SimpleNamespace(call=call, session_credentials=credentials,
            verification=types.SimpleNamespace(protected_token=lambda _: b"opaque-test-protected-bytes"))
        self.good_role = lambda *_: {"status": 409, "errorCode": "CQL_SOURCE_HOST_INSTANCE_CONFLICT"}

    def test_existing_wizard_acknowledgement_requires_server_authority_and_never_writes_plain_credentials(self):
        signin.prepare(self.state, "synthetic-user", "synthetic-password", self.query, self.good_role)
        ack = json.loads((self.state / "operator.acknowledged.json").read_text())
        self.assertTrue(ack["wizardAuthorityVerified"])
        self.assertEqual(ack["id"], self.owner)
        for path in self.state.iterdir():
            self.assertNotIn(b"synthetic-password", path.read_bytes())
            self.assertNotIn(self.token.encode(), path.read_bytes())
            self.assertNotIn(b"synthetic-cookie", path.read_bytes())
        self.assertEqual(self.calls, ["avatar/authenticate"])

    def test_non_wizard_does_not_get_operator_acknowledgement_or_role_promotion(self):
        with self.assertRaises(RuntimeError):
            signin.prepare(self.state, "synthetic-user", "synthetic-password", self.query,
                lambda *_: {"status": 403, "errorCode": "CQL_OPERATOR_REQUIRED"})
        self.assertFalse((self.state / "operator.acknowledged.json").exists())
        self.assertTrue((self.state / "operator.auth-response.dpapi").exists())
        self.assertEqual(self.calls, ["avatar/authenticate"])

    def test_existing_or_unknown_attempt_prevents_another_signin(self):
        self.state.mkdir()
        (self.state / "operator.intent.json").write_text("{}")
        with self.assertRaises(RuntimeError):
            signin.prepare(self.state, "synthetic-user", "synthetic-password", self.query, self.good_role)
        self.assertEqual(self.calls, [])

    def test_missing_secure_refresh_cookie_preserves_response_without_ready_session(self):
        self.cookies = []
        with self.assertRaises(RuntimeError):
            signin.prepare(self.state, "synthetic-user", "synthetic-password", self.query, self.good_role)
        self.assertTrue((self.state / "operator.auth-response.dpapi").exists())
        self.assertFalse((self.state / "operator.session.dpapi").exists())


if __name__ == "__main__":
    unittest.main()
