"""One admitted MinIO store; create source once or read original records after restart."""
import argparse
import hashlib
import json
import msvcrt
import os
from pathlib import Path
import socket
import subprocess
import sys
import time
import urllib.error
import urllib.request
import uuid
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
STATE = ROOT / 'LocalState/COSMIC/RecoveryLaptopMinIO20250907'
BINARY = STATE / 'minio.exe'
SHA = 'af709e6ba68488404e85acdd22a3030d0f5e56a108d4b27d744f18ceb50861b4'
PROJECT = ROOT / 'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.MinIOOASIS.IntegrationTests'


def durable(path, value):
    with path.open('x', encoding='utf-8') as output:
        json.dump(value, output, indent=2)
        output.flush()
        os.fsync(output.fileno())


def validate_source_admission(initialize):
    if initialize and any((STATE / name).exists() for name in ['source.json', 'bootstrap.intent.json', 'bootstrap.ack.json']):
        raise RuntimeError('Existing source or outcome; initialization cannot replay')
    if not initialize and not all((STATE / name).is_file() for name in ['source.json', 'bootstrap.ack.json', 'bucket.ack.json']):
        raise RuntimeError('Acknowledged retained native source required')
    if (STATE / 'mongo.pending.json').exists():
        raise RuntimeError('Unresolved consumer outcome; preserve without replay')


def native_interrupted(path):
    events = [json.loads(line) for line in path.read_text(encoding='utf-8').splitlines() if line.startswith('{')]
    if sum(event.get('message') == 'Exiting on signal: INTERRUPT' for event in events) != 1:
        raise RuntimeError('Current native phase did not prove graceful interrupt shutdown')


def run(initialize):
    with (STATE / 'runner.lock').open('a+b') as lock:
        lock.seek(0)
        msvcrt.locking(lock.fileno(), msvcrt.LK_NBLCK, 1)
        validate_source_admission(initialize)
        if hashlib.sha256(BINARY.read_bytes()).hexdigest() != SHA:
            raise RuntimeError('Official runtime SHA differs')
        ready = json.loads((STATE / 'preflight-ready.json').read_bytes())
        stopped = json.loads((STATE / 'preflight-stopped.json').read_bytes())
        if ready != {'health': 200, 'anonymousRoot': 403, 'businessWrites': False} or stopped['exitCode'] != 0 or not stopped['ctrlCSent']:
            raise RuntimeError('Original native preflight admission differs')
        credentials_bytes = (STATE / 'credentials.json').read_bytes()
        credentials_hash = hashlib.sha256(credentials_bytes).hexdigest()
        credential_admission = STATE / 'credential.admission.json'
        if initialize:
            if credential_admission.exists():
                raise RuntimeError('Existing credential admission outcome; preserve')
            durable(credential_admission, {'sha256': credentials_hash})
        if json.loads(credential_admission.read_bytes()) != {'sha256': credentials_hash}:
            raise RuntimeError('Original teaching credential identity differs')
        if not (STATE / 'data').is_dir():
            raise RuntimeError('Original native store missing; do not recreate')
        for port in [19700, 19701]:
            with socket.socket() as probe:
                if probe.connect_ex(('127.0.0.1', port)) == 0:
                    raise RuntimeError('Existing listener; no process adoption')
        credentials = json.loads(credentials_bytes)
        env = dict(os.environ, MINIO_ROOT_USER=credentials['username'], MINIO_ROOT_PASSWORD=credentials['password'], MINIO_BROWSER='off', GOMAXPROCS='2')
        reports = STATE / (('initial-' if initialize else 'retained-') + uuid.uuid4().hex)
        reports.mkdir()
        command = [str(BINARY), 'server', str(STATE / 'data'), '--address', '127.0.0.1:19700', '--console-address', '127.0.0.1:19701', '--json']
        durable(reports / 'intent.json', {'command': command, 'sourceWritesRequested': initialize})
        startup = subprocess.STARTUPINFO()
        startup.dwFlags |= subprocess.STARTF_USESHOWWINDOW
        startup.wShowWindow = subprocess.SW_HIDE
        with (reports / 'server.log').open('xb') as log:
            node = subprocess.Popen(command, env=env, cwd=STATE, stdout=log, stderr=subprocess.STDOUT,
                                    startupinfo=startup, creationflags=subprocess.CREATE_NEW_CONSOLE)
            durable(reports / 'process.json', {'pid': node.pid})
            try:
                opener = urllib.request.build_opener(urllib.request.ProxyHandler({}))
                deadline = time.monotonic() + 40
                while time.monotonic() < deadline:
                    if node.poll() is not None:
                        raise RuntimeError('Owned node exited before readiness')
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
                if initialize:
                    identities = [str(uuid.uuid4()) for _ in range(5)]
                    durable(STATE / 'source.json', {'Endpoint': 'http://127.0.0.1:19700', 'Bucket': 'cosmic-cql-minio-recovery',
                        'Actor': str(uuid.uuid4()), 'LiveIds': identities[:3], 'DeletedId': identities[3], 'MissingId': identities[4]})
                test_env = dict(os.environ, COSMIC_MINIO_CQL_SOURCE=str(STATE / 'source.json'))
                def test(category):
                    with (reports / (category + '.log')).open('xb') as output:
                        subprocess.run(['dotnet', 'test', str(PROJECT), '--no-build', '--no-restore', '--filter', 'TestCategory=' + category,
                            '--results-directory', str(reports), '--logger', 'trx;LogFileName=' + category + '.trx', '/m:2', '/nr:false', '/p:UseSharedCompilation=false'],
                            env=test_env, cwd=ROOT, stdout=output, stderr=subprocess.STDOUT, check=True)
                    trx = reports / (category + '.trx')
                    counts = ET.parse(trx).find('.//{*}Counters').attrib
                    if counts['passed'] != '1' or counts['total'] != '1' or counts['notExecuted'] != '0':
                        raise RuntimeError('Require exactly one real native pass, zero skips')
                    return hashlib.sha256(trx.read_bytes()).hexdigest()
                if initialize:
                    durable(reports / 'bootstrap.json', {'trxSha256': test('MinioCqlBootstrap')})
                trx_hash = test('MinioCqlNative')
                if hashlib.sha256((STATE / 'credentials.json').read_bytes()).hexdigest() != credentials_hash:
                    raise RuntimeError('Native credential identity changed')
                durable(reports / 'result.json', {'passed': 1, 'trxSha256': trx_hash, 'sourceWritesRequested': initialize})
            finally:
                if node.poll() is not None:
                    raise RuntimeError('Native node exited before explicit shutdown')
                signal = subprocess.run([sys.executable, '-B', str(ROOT / 'Scripts/TestHosts/Signal-OwnedConsoleProcess.py'), '--pid', str(node.pid)], capture_output=True, text=True, check=True)
                receipt = json.loads(signal.stdout)
                code = node.wait(timeout=30)
                native_interrupted(reports / 'server.log')
                durable(reports / 'stopped.json', dict(receipt, exitCode=code, nativeInterrupted=True))
                if receipt['targetPid'] != node.pid or not receipt['ctrlCSent'] or code != 0:
                    raise RuntimeError('Native shutdown did not prove its contract')
        print(json.dumps({'completed': True, 'phase': reports.name}))


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    mode = parser.add_mutually_exclusive_group(required=True)
    mode.add_argument('--initialize', action='store_true')
    mode.add_argument('--retained', action='store_true')
    args = parser.parse_args()
    run(args.initialize)
