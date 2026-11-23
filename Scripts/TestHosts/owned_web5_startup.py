"""Owned cloud processes for real WEB4-to-WEB5 portal qualification; no simulated services."""
import ipaddress
import json
import os
from pathlib import Path
import re
import selectors
import signal
import socket
import socketserver
import subprocess
import threading
import time
import urllib.request


class OwnedApiBridge:
    """Expose only one inspected internal API address through a temporary loopback listener."""
    def __init__(self, private_ip):
        parsed = ipaddress.ip_address(private_ip)
        if parsed.version != 4 or not parsed.is_private:
            raise ValueError('An inspected owned internal IPv4 address is required.')
        self.destination = (private_ip, 8080)
        self.stopped = threading.Event()
        self.sockets = set()
        self.lock = threading.Lock()

    def __enter__(self):
        owner = self

        class Handler(socketserver.BaseRequestHandler):
            def handle(self):
                upstream = socket.create_connection(owner.destination, timeout=5)
                try:
                    with owner.lock:
                        owner.sockets.update((self.request, upstream))
                    with selectors.DefaultSelector() as selector:
                        selector.register(self.request, selectors.EVENT_READ, upstream)
                        selector.register(upstream, selectors.EVENT_READ, self.request)
                        while not owner.stopped.is_set():
                            for key, _ in selector.select(timeout=0.25):
                                data = key.fileobj.recv(65536)
                                if not data:
                                    return
                                key.data.sendall(data)
                except OSError:
                    if not owner.stopped.is_set():
                        raise
                finally:
                    with owner.lock:
                        owner.sockets.discard(self.request)
                        owner.sockets.discard(upstream)
                    upstream.close()

        self.server = socketserver.ThreadingTCPServer(('127.0.0.1', 0), Handler)
        self.thread = threading.Thread(target=self.server.serve_forever, name='owned-api-loopback')
        self.thread.start()
        self.url = 'http://127.0.0.1:' + str(self.server.server_address[1])
        return self

    def __exit__(self, *_):
        self.stopped.set()
        self.server.shutdown()
        with self.lock:
            active = list(self.sockets)
        for connection in active:
            connection.close()
        self.server.server_close()
        self.thread.join(timeout=5)
        if self.thread.is_alive():
            raise RuntimeError('Owned API bridge did not stop.')


class OwnedWeb5Process:
    """Run the published shipping entry point using the same owned DNA and real usage ledger."""
    def __init__(self, app_directory, dna, environment):
        if os.environ.get('GITHUB_ACTIONS') != 'true':
            raise RuntimeError('WEB5 Startup qualification is cloud-only.')
        supplied = Path(app_directory)
        self.directory = supplied.resolve(strict=True)
        expected = Path(os.environ['GITHUB_WORKSPACE']).resolve(strict=True) / 'portal-web5-owned' / 'app'
        if supplied.is_symlink() or self.directory != expected:
            raise ValueError('The exact owned WEB5 publish directory is required.')
        self.dll = self.directory / 'NextGenSoftware.OASIS.STAR.WebAPI.dll'
        if not self.dll.is_file():
            raise FileNotFoundError('Published shipping WEB5 entry point is missing.')
        self.dna = dna
        self.environment = dict(os.environ, **environment)
        self.environment.update({'PORT': '0', 'ASPNETCORE_ENVIRONMENT': 'Production',
                                 'Logging__LogLevel__Microsoft.Hosting.Lifetime': 'Information'})
        self.port = None
        self.process = None
        self.reader = None
        self.graceful_exit = None
        self.dna_path = self.directory / 'OASIS_DNA.json'
        if self.dna_path.is_symlink():
            raise ValueError('Owned DNA output must not be a symlink.')

    def __enter__(self):
        try:
            self.dna_path.write_text(json.dumps(self.dna, separators=(',', ':')), encoding='utf-8')
            self.dna_path.chmod(0o600)
            self.process = subprocess.Popen(['dotnet', str(self.dll)], cwd=self.directory,
                                            env=self.environment, stdout=subprocess.PIPE,
                                            stderr=subprocess.STDOUT, text=True, errors='replace')

            def read_port():
                # Drain logs without printing or retaining DNA, credentials or response contents.
                for line in self.process.stdout:
                    match = re.search(r'Now listening on: http://[^\s]+:([0-9]+)', line)
                    if match:
                        self.port = int(match.group(1))

            self.reader = threading.Thread(target=read_port, name='owned-web5-output')
            self.reader.start()
            deadline = time.monotonic() + 120
            while time.monotonic() < deadline:
                if self.process.poll() is not None:
                    raise RuntimeError('Owned WEB5 exited before HTTP readiness.')
                if self.port:
                    self.url = 'http://127.0.0.1:' + str(self.port)
                    request = urllib.request.Request(self.url + '/api/health',
                                                     headers={'X-Forwarded-Proto': 'https'})
                    with urllib.request.urlopen(request, timeout=5) as response:
                        health = json.load(response)
                    if health.get('status') != 'healthy' or health.get('service') != 'STAR API WebAPI':
                        raise RuntimeError('Owned WEB5 health identity differs.')
                    return self
                time.sleep(0.25)
            raise RuntimeError('Owned WEB5 Startup readiness deadline.')
        except BaseException:
            self.__exit__(None, None, None)
            raise

    def __exit__(self, *_):
        try:
            if self.process:
                if self.process.poll() is None:
                    self.process.terminate()
                try:
                    self.graceful_exit = self.process.wait(timeout=20)
                except subprocess.TimeoutExpired:
                    self.process.kill()
                    self.process.wait(timeout=5)
                    raise RuntimeError('Owned WEB5 required forced termination.')
                finally:
                    if self.reader:
                        self.reader.join(timeout=5)
                    self.process.stdout.close()
        finally:
            self.dna_path.unlink(missing_ok=True)


def run_owned_browser(command, environment, timeout=150):
    """Bound the owned browser process tree, including Chromium, on the cloud runner."""
    if os.environ.get('GITHUB_ACTIONS') != 'true' or os.name != 'posix':
        raise RuntimeError('Owned browser process-group execution requires the Linux cloud runner.')
    process = subprocess.Popen(command, env=environment, stdout=subprocess.PIPE,
                               stderr=subprocess.PIPE, text=True, start_new_session=True)
    try:
        stdout, stderr = process.communicate(timeout=timeout)
    except subprocess.TimeoutExpired:
        try:
            os.killpg(process.pid, signal.SIGTERM)
        except ProcessLookupError:
            # The owned group already ended during the deadline observation.
            process.wait(timeout=5)
        try:
            process.communicate(timeout=5)
        except subprocess.TimeoutExpired:
            try:
                os.killpg(process.pid, signal.SIGKILL)
            except ProcessLookupError:
                process.wait(timeout=5)
            process.communicate(timeout=5)
        raise RuntimeError('Owned portal browser exceeded its deadline; process group stopped.')
    return subprocess.CompletedProcess(command, process.returncode, stdout, stderr)
