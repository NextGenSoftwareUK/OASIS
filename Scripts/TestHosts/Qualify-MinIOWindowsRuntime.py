"""One official Windows MinIO runtime preflight; no bucket or Holon writes."""
from pathlib import Path
import hashlib
import json
import os
import secrets
import socket
import subprocess
import sys
import time
import urllib.error
import urllib.request

ROOT = Path(__file__).resolve().parents[2]
STATE = ROOT / 'LocalState/COSMIC/RecoveryLaptopMinIO20250907'
BINARY = STATE / 'minio.exe'
SHA = 'af709e6ba68488404e85acdd22a3030d0f5e56a108d4b27d744f18ceb50861b4'


def durable(path, value):
    with path.open('x', encoding='utf-8') as output:
        json.dump(value, output, indent=2)
        output.flush()
        os.fsync(output.fileno())


def main():
    if (STATE / 'preflight.intent.json').exists():
        raise RuntimeError('Existing runtime preflight/outcome; inspect/reuse without replay')
    if hashlib.sha256(BINARY.read_bytes()).hexdigest() != SHA:
        raise RuntimeError('Official native executable differs')
    for port in [19700, 19701]:
        with socket.socket() as probe:
            if probe.connect_ex(('127.0.0.1', port)) == 0:
                raise RuntimeError('Existing listener; no process adoption')
    if (STATE / 'credentials.json').exists() or (STATE / 'data').exists():
        raise RuntimeError('Existing native environment; no overwrite')
    credentials = {'username': 'cosmic_recovery_minio', 'password': secrets.token_hex(32)}
    durable(STATE / 'credentials.json', credentials)
    durable(STATE / 'preflight.intent.json', {'runtimeSha256': SHA, 'endpoint': 'http://127.0.0.1:19700', 'businessWrites': False})
    env = dict(os.environ, MINIO_ROOT_USER=credentials['username'], MINIO_ROOT_PASSWORD=credentials['password'],
               MINIO_BROWSER='off', GOMAXPROCS='2')
    startup = subprocess.STARTUPINFO()
    startup.dwFlags |= subprocess.STARTF_USESHOWWINDOW
    startup.wShowWindow = subprocess.SW_HIDE
    log_path = STATE / 'preflight-server.log'
    with log_path.open('xb') as log:
        node = subprocess.Popen([str(BINARY), 'server', str(STATE / 'data'), '--address', '127.0.0.1:19700',
                                 '--console-address', '127.0.0.1:19701', '--json'], env=env, cwd=STATE,
                                stdout=log, stderr=subprocess.STDOUT, startupinfo=startup, creationflags=subprocess.CREATE_NEW_CONSOLE)
        durable(STATE / 'preflight-process.json', {'pid': node.pid})
        try:
            opener = urllib.request.build_opener(urllib.request.ProxyHandler({}))
            deadline = time.monotonic() + 40
            while time.monotonic() < deadline:
                if node.poll() is not None:
                    raise RuntimeError('Owned native runtime exited before health')
                try:
                    with opener.open('http://127.0.0.1:19700/minio/health/live', timeout=1) as response:
                        if response.status == 200:
                            break
                except urllib.error.HTTPError as error:
                    if error.code != 503:
                        raise
                    time.sleep(.25)
                except (ConnectionError, urllib.error.URLError):
                    time.sleep(.25)
            else:
                raise RuntimeError('Native health readiness deadline')
            try:
                opener.open('http://127.0.0.1:19700/', timeout=5)
            except urllib.error.HTTPError as error:
                if error.code != 403 or b'AccessDenied' not in error.read(65536):
                    raise RuntimeError('Native anonymous-access denial differs') from error
            else:
                raise RuntimeError('Native object endpoint unexpectedly admitted anonymous listing')
            durable(STATE / 'preflight-ready.json', {'health': 200, 'anonymousRoot': 403, 'businessWrites': False})
        finally:
            if node.poll() is not None:
                raise RuntimeError('Native process exited before explicit shutdown')
            signal = subprocess.run([sys.executable, '-B', str(ROOT / 'Scripts/TestHosts/Signal-OwnedConsoleProcess.py'), '--pid', str(node.pid)],
                                    capture_output=True, text=True, check=True)
            receipt = json.loads(signal.stdout)
            code = node.wait(timeout=30)
            durable(STATE / 'preflight-stopped.json', dict(receipt, exitCode=code))
            if receipt['targetPid'] != node.pid or receipt['ctrlCSent'] is not True or code != 0:
                raise RuntimeError('Native shutdown did not prove its contract')
    print(json.dumps({'nativeRuntimeQualified': True, 'businessWrites': False, 'stopped': True}))


if __name__ == '__main__':
    main()
