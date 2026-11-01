import base64
import importlib.util
import json
import os
from pathlib import Path
import tempfile
import time
import unittest
from unittest.mock import patch
import HostedSessionCredentials as credentials

OWNER = "00000000-0000-0000-0000-000000000001"
OTHER = "00000000-0000-0000-0000-000000000002"
COOKIE = "refreshToken=old%2Bvalue%3D; Path=/; Secure; HttpOnly; SameSite=Lax"
NEXT_COOKIE = "refreshToken=new%2Bvalue%3D; Path=/; Secure; HttpOnly; SameSite=Lax"

def token(owner=OWNER, seconds=900):
    payload = base64.urlsafe_b64encode(json.dumps({"sub": owner, "exp": int(time.time()) + seconds}).encode()).decode().rstrip("=")
    return "header." + payload + ".inert-signature"

def seal(value):
    return b"unit-sealed:" + value  # Inert fixtures only; live tooling uses Windows DPAPI.

def write_new(path, value):
    with path.open("xb") as stream:
        stream.write(value)

class SessionCredentialTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        parent = Path(__file__).resolve().parents[2] / "TestResults/SandboxSessionRenewalCurrent"
        parent.mkdir(parents=True, exist_ok=True)
        cls.temporary = tempfile.TemporaryDirectory(prefix="credential-unit-", dir=parent)
        cls.state = Path(cls.temporary.name).resolve()
        assert cls.state.parent == parent.resolve()

    @classmethod
    def tearDownClass(cls):
        assert cls.state.parent == (Path(__file__).resolve().parents[2] / "TestResults/SandboxSessionRenewalCurrent").resolve()
        cls.temporary.cleanup()

    def test_cookie_contract_preserves_encoded_wire_value(self):
        self.assertEqual(credentials.refresh_cookie([COOKIE]), "refreshToken=old%2Bvalue%3D")
        for headers in [[], [COOKIE, NEXT_COOKIE], [COOKIE.replace("Secure; ", "")],
                        [COOKIE.replace("HttpOnly; ", "")], [COOKIE.replace("SameSite=Lax", "SameSite=None")]]:
            with self.subTest(headers=headers):
                with self.assertRaises(RuntimeError): credentials.refresh_cookie(headers)

    def test_initial_bundle_requires_same_unexpired_owner(self):
        good = credentials.bundle(OWNER, token(), [COOKIE])
        self.assertEqual(credentials.validate(good, OWNER), good)
        for value in [token(OTHER), token(seconds=-1)]:
            with self.assertRaises(RuntimeError): credentials.bundle(OWNER, value, [COOKIE])

    def test_rotates_both_credentials_with_one_atomic_install(self):
        label = "atomic"
        original = credentials.bundle(OWNER, token(), [COOKIE])
        write_new(self.state / (label + ".session.dpapi"), seal(json.dumps(original).encode()))
        calls = []
        def transport(route, **options):
            calls.append((route, options["cookie_header"]))
            options["capture_auth"](200, b"inert-response", [NEXT_COOKIE])
            return {"isError": False, "result": {"id": OWNER, "jwtToken": token(seconds=1800)}}
        replace = os.replace
        with patch.object(credentials.os, "replace", wraps=replace) as atomic:
            updated = credentials.rotate(self.state, label, original, transport, seal, write_new)
            self.assertEqual(atomic.call_count, 1)
        installed = json.loads((self.state / (label + ".session.dpapi")).read_bytes()[len(b"unit-sealed:"):])
        self.assertEqual(installed, updated)
        self.assertEqual(updated["cookieHeader"], "refreshToken=new%2Bvalue%3D")
        self.assertEqual(calls, [("avatar/refresh-token", original["cookieHeader"])])
        credentials.no_pending_rotation(self.state, label)

    def test_unknown_outcome_blocks_a_second_refresh_attempt(self):
        original = credentials.bundle(OWNER, token(), [COOKIE]); calls = []
        def transport(*args, **kwargs):
            calls.append(True); raise TimeoutError("controlled lost response")
        with self.assertRaises(TimeoutError): credentials.rotate(self.state, "lost", original, transport, seal, write_new)
        with self.assertRaisesRegex(RuntimeError, "reconciliation"):
            credentials.rotate(self.state, "lost", original, transport, seal, write_new)
        self.assertEqual(len(calls), 1)

    def test_a_different_avatar_cannot_replace_the_bundle(self):
        original = credentials.bundle(OWNER, token(), [COOKIE]); path = self.state / "foreign.session.dpapi"
        before = seal(json.dumps(original).encode()); write_new(path, before)
        def transport(route, **options):
            options["capture_auth"](200, b"inert-response", [NEXT_COOKIE])
            return {"isError": False, "result": {"id": OTHER, "jwtToken": token(OTHER)}}
        with self.assertRaisesRegex(RuntimeError, "replace"):
            credentials.rotate(self.state, "foreign", original, transport, seal, write_new)
        self.assertEqual(path.read_bytes(), before)

    def test_response_without_rotation_is_not_installed(self):
        original = credentials.bundle(OWNER, token(), [COOKIE])
        def transport(route, **options):
            options["capture_auth"](200, b"inert-response", [COOKIE])
            return {"isError": False, "result": {"id": OWNER, "jwtToken": token(seconds=1800)}}
        with self.assertRaisesRegex(RuntimeError, "not rotated"):
            credentials.rotate(self.state, "unchanged", original, transport, seal, write_new)
        self.assertFalse((self.state / "unchanged.session.dpapi").exists())

    def test_missing_capture_cannot_publish_credentials(self):
        original = credentials.bundle(OWNER, token(), [COOKIE])
        def transport(route, **options):
            return {"isError": False, "result": {"id": OWNER, "jwtToken": token(seconds=1800)}}
        with self.assertRaisesRegex(RuntimeError, "acknowledgement"):
            credentials.rotate(self.state, "no-capture", original, transport, seal, write_new)
        self.assertFalse((self.state / "no-capture.session.dpapi").exists())

    def test_cookie_header_cannot_add_another_header_or_cookie(self):
        original = credentials.bundle(OWNER, token(), [COOKIE])
        for suffix in ["\r\nInjected: inert", ";another=inert"]:
            changed = {**original, "cookieHeader": original["cookieHeader"] + suffix}
            with self.assertRaises(RuntimeError): credentials.validate(changed, OWNER)

if __name__ == "__main__":
    unittest.main()
