"""Exercise the shipping curl arguments against owned loopback HTTP responses only."""
import hashlib
import http.server
import os
from pathlib import Path
import re
import shlex
import shutil
import subprocess
import tempfile
import threading
from types import SimpleNamespace
import unittest

ROOT = Path(__file__).resolve().parents[2]


class AssetDownloadPolicyTests(unittest.TestCase):
    def test_final_image_consumes_verified_public_release_assets(self):
        source = (ROOT / 'Docker/Dockerfile.web4').read_text(encoding='utf-8')
        stages = list(re.finditer(r'^FROM\s+(\S+)\s+AS\s+(\w+)\s*$', source, re.M | re.I))
        blocks = {stage.group(2): source[stage.end():stages[index + 1].start() if index + 1 < len(stages) else len(source)]
                  for index, stage in enumerate(stages)}
        bases = {stage.group(2): stage.group(1) for stage in stages}
        self.assertEqual(len(stages), len(blocks), 'Stage aliases must be unique.')
        reachable = set()
        pending = ['final']
        while pending:
            name = pending.pop()
            if name in reachable:
                continue
            reachable.add(name)
            if bases[name] in blocks:
                pending.append(bases[name])
            for dependency in re.findall(r'^COPY\s+--from=(\w+)\s', blocks[name], re.M):
                self.assertIn(dependency, blocks)
                pending.append(dependency)
        for role in ['provider_waku', 'provider_nats']:
            self.assertIn(role, reachable)
            self.arguments(role)
        self.assertFalse(any(bases[stage].startswith('ghcr.io/nextgensoftwareuk/oasis-web4')
                             for stage in reachable))

    def arguments(self, stage):
        dockerfile = (ROOT / 'Docker/Dockerfile.web4').read_text(encoding='utf-8')
        block = re.search(r'^FROM[^\n]* AS ' + stage + r'\n.*?(?=^FROM|\Z)', dockerfile, re.M | re.S).group()
        command = re.search(r'&& curl (.*?)&& echo', block, re.S).group(1)
        args = shlex.split(re.sub(r'\\\r?\n', ' ', command))
        self.assertEqual(args[0], '--disable', 'Ambient curlrc must not change the artifact contract.')
        self.assertEqual(args[args.index('--retry') + 1], '2')
        self.assertEqual(args[args.index('--retry-max-time') + 1], '180')
        self.assertEqual(args[args.index('--max-time') + 1], '90')
        self.assertNotIn('--retry-all-errors', args)
        self.assertNotIn('--insecure', args)
        self.assertNotIn('--silent', args)
        self.assertIn('--fail', args)
        expected = re.search(r"&& echo '([a-f0-9]{64})  [^']+' \| sha256sum --check", block).group(1)
        urls = [arg for arg in args if arg.startswith('https://')]
        self.assertEqual(len(urls), 1)
        self.assertRegex(urls[0], r'^https://api\.github\.com/repos/[^/]+/[^/]+/releases/assets/[0-9]+$')
        return args, urls[0], expected

    def transfer(self, stage, statuses, body=b'owned-control-asset'):
        args, source, expected = self.arguments(stage)
        observed = []

        class Replies(http.server.BaseHTTPRequestHandler):
            def do_GET(self):
                status = statuses[min(len(observed), len(statuses) - 1)]
                observed.append(status)
                self.send_response(status)
                self.send_header('Content-Length', str(len(body)))
                self.end_headers()
                self.wfile.write(body)

            def log_message(self, *unused):
                return  # Controlled fixture requests are asserted below, not application logs.

        server = http.server.ThreadingHTTPServer(('127.0.0.1', 0), Replies)
        thread = threading.Thread(target=server.serve_forever, daemon=True)
        thread.start()
        try:
            curl = Path('C:/Program Files/Git/mingw64/bin/curl.exe') if os.name == 'nt' else Path(shutil.which('curl') or '')
            self.assertTrue(curl.is_file(), 'The existing curl runtime is required; never install another here.')
            with tempfile.TemporaryDirectory(prefix='oasis-asset-policy-') as directory:
                output = Path(directory) / 'asset'
                args[args.index(source)] = f'http://127.0.0.1:{server.server_port}/asset'
                args[args.index('--output') + 1] = str(output)
                result = subprocess.run([str(curl), *args], capture_output=True, timeout=15,
                                        env=os.environ | {'LC_ALL': 'C', 'NO_PROXY': '127.0.0.1'})
                data = output.read_bytes() if output.exists() else b''
                # Execute the shipping checksum gate too. Only the controlled
                # source bytes and their corresponding pin are substituted.
                # Some curl versions emit error22 but return0 for a non-retried
                # HTTP failure when --retry is enabled; that must never make
                # the full build pipeline succeed.
                checksum = Path('C:/Program Files/Git/usr/bin/sha256sum.exe') if os.name == 'nt' else Path(shutil.which('sha256sum') or '')
                self.assertTrue(checksum.is_file())
                pin = hashlib.sha256(b'owned-control-asset').hexdigest()
                checked = subprocess.run([str(checksum), '--check', '--strict'],
                    input=f'{pin}  {output.as_posix()}\n'.encode(), capture_output=True, timeout=5)
                pipeline = SimpleNamespace(returncode=result.returncode or checked.returncode,
                    curl_returncode=result.returncode, stderr=result.stderr + checked.stderr)
                return pipeline, observed, data, expected
        finally:
            server.shutdown()
            server.server_close()
            thread.join(timeout=2)
            self.assertFalse(thread.is_alive(), 'Owned fixture must stop before releasing its directory.')

    def test_transient_errors_recover_on_third_attempt_and_are_observable(self):
        for stage in ['provider_waku', 'provider_nats']:
            with self.subTest(stage=stage):
                result, observed, data, _ = self.transfer(stage, [502, 503, 200])
                self.assertEqual(result.returncode, 0, result.stderr.decode())
                self.assertEqual(observed, [502, 503, 200])
                self.assertEqual(data, b'owned-control-asset')
                self.assertIn(b'502', result.stderr)
                self.assertIn(b'503', result.stderr)
                self.assertIn(b'retry', result.stderr.lower())

    def test_persistent_gateway_failure_stops_after_three_attempts(self):
        for stage in ['provider_waku', 'provider_nats']:
            with self.subTest(stage=stage):
                result, observed, _, _ = self.transfer(stage, [504])
                self.assertEqual(result.returncode, 22)
                self.assertEqual(observed, [504, 504, 504])

    def test_authentication_and_permission_errors_are_not_retried(self):
        for stage in ['provider_waku', 'provider_nats']:
            for status in [401, 403]:
                with self.subTest(stage=stage, status=status):
                    result, observed, _, _ = self.transfer(stage, [status])
                    self.assertNotEqual(result.returncode, 0)
                    self.assertEqual(observed, [status])
                    self.assertIn(str(status).encode(), result.stderr)

    def test_bad_bytes_do_not_trigger_transport_retry_or_match_pinned_checksum(self):
        for stage in ['provider_waku', 'provider_nats']:
            with self.subTest(stage=stage):
                result, observed, data, expected = self.transfer(stage, [200], b'corrupt')
                self.assertEqual(result.curl_returncode, 0)
                self.assertNotEqual(result.returncode, 0, 'Checksum gate must reject a successful HTTP response with bad bytes.')
                self.assertEqual(observed, [200])
                self.assertNotEqual(hashlib.sha256(data).hexdigest(), expected)


if __name__ == '__main__':
    unittest.main(verbosity=2)
