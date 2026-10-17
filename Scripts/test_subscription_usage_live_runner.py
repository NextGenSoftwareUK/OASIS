"""Offline live-runner transport tests; uses local servers and dummy credentials only."""
import contextlib
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
import threading
import unittest
from unittest.mock import patch

import test_subscription_usage_live as runner


@contextlib.contextmanager
def serve(handler):
    server = ThreadingHTTPServer(("127.0.0.1", 0), handler)
    worker = threading.Thread(target=server.serve_forever, daemon=True)
    worker.start()
    try:
        yield f"http://127.0.0.1:{server.server_port}"
    finally:
        server.shutdown()
        server.server_close()
        worker.join()


class QuietHandler(BaseHTTPRequestHandler):
    def log_message(self, *args):
        pass


class LiveRunnerTransportTests(unittest.TestCase):
    def test_redirects_never_forward_credentials_or_repeat_mutations(self):
        sink_calls = []

        class Sink(QuietHandler):
            def do_GET(self):
                sink_calls.append(dict(self.headers))
                self.send_response(200)
                self.end_headers()

            do_POST = do_GET

        with serve(Sink) as destination:
            class Redirect(QuietHandler):
                def do_POST(self):
                    self.send_response(int(self.path.strip("/")))
                    self.send_header("Location", destination + "/capture")
                    self.end_headers()

            with serve(Redirect) as source:
                for status in (301, 302, 303, 307, 308):
                    with self.subTest(status=status), self.assertRaisesRegex(ValueError, "redirects are forbidden"):
                        runner.call(source, f"/{status}", {
                            "Authorization": "Bearer dummy-test-only",
                            "X-OASIS-Service-Key": "dummy-test-only"
                        }, {"operationId": "dummy-operation"})
        self.assertEqual([], sink_calls)

    def test_json_media_type_is_case_insensitive_for_success_and_errors(self):
        class Json(QuietHandler):
            def do_GET(self):
                self.send_response(int(self.path.strip("/")))
                self.send_header("content-type", "Application/JSON; charset=utf-8")
                self.end_headers()
                self.wfile.write(b'{"verified":true}')

        with serve(Json) as base:
            for status in (200, 409):
                with self.subTest(status=status):
                    payload, _ = runner.call(base, f"/{status}", {}, expected=status)
                    self.assertEqual({"verified": True}, payload)

    def test_every_call_validates_base_before_sending_credentials(self):
        invalid = (
            "http://example.test", "ftp://localhost", "https://name:password@example.test",
            "https://example.test?token=private", "https://example.test#fragment",
            "https://example.test:bad-port", "https://example.test\n", "https://example.test\\path", "https://"
        )
        with patch.object(runner.HTTP, "open") as network:
            for base in invalid:
                with self.subTest(base=base), self.assertRaises(ValueError):
                    runner.call(base, "/usage", {"Authorization": "Bearer dummy-test-only"})
            network.assert_not_called()

    def test_paths_cannot_change_origin_or_hide_fragments(self):
        with patch.object(runner.HTTP, "open") as network:
            for path in ("https://other.test", "//other.test/path", "/path#fragment", "/path\n", "/\\other.test"):
                with self.subTest(path=path), self.assertRaises(ValueError):
                    runner.call("https://example.test", path, {})
            network.assert_not_called()

    def test_https_and_loopback_urls_retain_the_configured_path(self):
        for base in ("https://example.test", "http://localhost:8000", "http://127.0.0.1:8000", "http://[::1]:8000"):
            with self.subTest(base=base):
                self.assertEqual(base + "/prefix/usage?month=2026-09", runner.request_url(base + "/prefix/", "/usage?month=2026-09"))


if __name__ == "__main__":
    unittest.main()
