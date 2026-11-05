"""Explicit new Windows teaching source; never impersonate the retained Linux source.

Caller must hold the provider build lease and build the integration project first.
"""
import argparse
import base64
import hashlib
import json
import msvcrt
import os
from pathlib import Path
import secrets
import socket
import subprocess
import sys
import time
import urllib.error
import urllib.request
import uuid
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
STATE = ROOT / 'LocalState/COSMIC/RecoveryLaptopSurrealDB335'
BINARY = STATE / 'surreal.exe'
SHA = 'fb859638bd875d658d483897d8d0c3eca6215e94a2946ff75693456b45e3bbbc'
PROJECT = ROOT / 'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.SurrealDBOASIS.IntegrationTests'


def durable(path, value):
    with path.open('x', encoding='utf-8') as output:
        json.dump(value, output, indent=2)
        output.flush()
        os.fsync(output.fileno())


def admit_source(state, initialize):
    source = state / 'windows-source'
    if initialize:
        if source.exists():
            raise RuntimeError('Existing Windows source/outcome; initialization cannot replay it')
        source.mkdir()
        durable(source / 'environment.pending.json', {'runtimeSha256': SHA, 'port': 18001})
        durable(source / 'credentials.json', {'username': 'cosmic_windows_demo', 'password': secrets.token_hex(32)})
        durable(source / 'environment.json', {'runtimeSha256': SHA, 'port': 18001, 'store': str(source / 'data')})
        (source / 'environment.pending.json').unlink()
    manifest = json.loads((source / 'environment.json').read_text(encoding='utf-8'))
    if manifest != {'runtimeSha256': SHA, 'port': 18001, 'store': str(source / 'data')}:
        raise RuntimeError('Original Windows environment differs')
    if list(source.glob('*.pending.json')):
        raise RuntimeError('Unresolved source outcome; preserve without replay')
    if not initialize and not all((source / name).is_file() for name in ['identity.json', 'native-baseline.json']):
        raise RuntimeError('Retained original identity/baseline required')
    return source


class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, request, fp, code, message, headers, url):
        return None


def reader_settings(source, provision, opener, root_credentials):
    credentials = source / 'reader.json'
    ack = source / 'reader.ack.json'
    pending = source / 'reader.pending.json'
    if provision:
        if any(path.exists() for path in [credentials, ack, pending]):
            raise RuntimeError('Existing reader/outcome; provisioning cannot replay')
        config = {'username': 'cosmic_windows_reader', 'password': secrets.token_hex(32)}
        durable(pending, {'username': config['username'], 'scope': 'oasis/oasis', 'role': 'VIEWER'})
        durable(credentials, config)
        request = urllib.request.Request('http://127.0.0.1:18001/sql', method='POST',
            data=("DEFINE USER cosmic_windows_reader ON DATABASE PASSWORD '" + config['password'] + "' ROLES VIEWER;").encode(),
            headers={'Authorization': 'Basic ' + base64.b64encode((root_credentials['username'] + ':' + root_credentials['password']).encode()).decode(),
                     'surreal-ns': 'oasis', 'surreal-db': 'oasis', 'Accept': 'application/json', 'Content-Type': 'text/plain'})
        with opener.open(request, timeout=15) as response:
            raw = response.read(65537)
            if len(raw) > 65536:
                raise RuntimeError('Reader acknowledgement exceeds budget')
        statements = json.loads(raw)
        if len(statements) != 1 or statements[0].get('status') != 'OK':
            raise RuntimeError('Native database reader provisioning failed; preserve pending outcome')
        durable(ack, {'username': config['username'], 'scope': 'oasis/oasis', 'role': 'VIEWER'})
        pending.unlink()
    if pending.exists() or not credentials.is_file() or not ack.is_file():
        raise RuntimeError('Acknowledged database reader required')
    expected = {'username': 'cosmic_windows_reader', 'scope': 'oasis/oasis', 'role': 'VIEWER'}
    if json.loads(ack.read_text(encoding='utf-8')) != expected:
        raise RuntimeError('Reader admission differs')
    config = json.loads(credentials.read_text(encoding='utf-8'))
    if config.get('username') != expected['username']:
        raise RuntimeError('Reader credential identity differs')
    return config


def run(initialize, reader=False, provision_reader=False, paired=False, serve=False):
    if hashlib.sha256(BINARY.read_bytes()).hexdigest() != SHA:
        raise RuntimeError('Official runtime hash differs')
    with (STATE / 'runner.lock').open('a+b') as lock:
        lock.seek(0)
        msvcrt.locking(lock.fileno(), msvcrt.LK_NBLCK, 1)
        with socket.socket() as probe:
            if probe.connect_ex(('127.0.0.1', 18001)) == 0:
                raise RuntimeError('Existing native listener; no process adoption')
        source = admit_source(STATE, initialize)
        if paired:
            reader_settings(source, False, None, {})
            if (source / 'mongo.pending.json').exists() or (source / 'demo-ready.json').exists():
                raise RuntimeError('Unresolved previous consumer; preserve original evidence')
            with socket.socket() as probe:
                if probe.connect_ex(('127.0.0.1', 5140)) == 0:
                    raise RuntimeError('Browser listener occupied')
        config = json.loads((source / 'credentials.json').read_text(encoding='utf-8'))
        phase = ('interactive-' if serve else 'paired-' if paired else 'reader-admission-' if provision_reader else 'reader-' if reader else 'initial-' if initialize else 'retained-') + uuid.uuid4().hex
        reports = STATE / phase
        reports.mkdir()
        env = dict(os.environ, SURREAL_USER=config['username'], SURREAL_PASS=config['password'],
                   SURREAL_TELEMETRY_DISABLE='true', TOKIO_WORKER_THREADS='2', NO_COLOR='1')
        command = [str(BINARY), 'start', '--no-banner', '--bind', '127.0.0.1:18001',
                   '--deny-guests', 'surrealkv://' + (source / 'data').as_posix()]
        durable(reports / 'intent.json', {'command': command, 'initialize': initialize})
        startup = subprocess.STARTUPINFO()
        startup.dwFlags |= subprocess.STARTF_USESHOWWINDOW
        startup.wShowWindow = subprocess.SW_HIDE
        with (reports / 'server.log').open('xb') as log:
            node = subprocess.Popen(command, env=env, cwd=STATE, stdout=log, stderr=subprocess.STDOUT,
                                    startupinfo=startup, creationflags=subprocess.CREATE_NEW_CONSOLE)
            durable(reports / 'process.json', {'pid': node.pid})
            try:
                opener = urllib.request.build_opener(urllib.request.ProxyHandler({}), NoRedirect())
                deadline = time.monotonic() + 40
                while time.monotonic() < deadline:
                    if node.poll() is not None:
                        raise RuntimeError('Owned node exited before readiness; preserve original logs')
                    try:
                        with opener.open('http://127.0.0.1:18001/health', timeout=1) as response:
                            if response.status == 200:
                                break
                    except (ConnectionError, urllib.error.URLError):
                        time.sleep(.25)
                else:
                    raise RuntimeError('Native readiness deadline')
                test_env = dict(os.environ, COSMIC_SURREALDB_ROOT=str(source), COSMIC_SURREALDB_WINDOWS_REPOSITORY=str(ROOT))
                test_env['COSMIC_SURREALDB_MONGO_SERVE'] = '1' if serve else '0'
                if reader:
                    settings = reader_settings(source, provision_reader, opener, config)
                    identity = json.loads((source / 'identity.json').read_text(encoding='utf-8'))
                    values = {'ENDPOINT': 'http://127.0.0.1:18001', 'USERNAME': settings['username'],
                              'PASSWORD': settings['password'], 'NAMESPACE': 'oasis', 'DATABASE': 'oasis',
                              'RECORD_ID': identity['Ids'][0]}
                    test_env.update({'SURREALDB_CQL_READER_' + key: value for key, value in values.items()})
                with (reports / 'native.log').open('xb') as test_log:
                    subprocess.run(['dotnet', 'test', str(PROJECT), '--no-build', '--no-restore',
                        '--filter', 'FullyQualifiedName~' + ('SurrealDbWindowsMongoCqlNativeTests' if paired else 'SurrealDbQueryActivationNativeTests' if reader else 'SurrealDbNativeCqlTests'), '--results-directory', str(reports),
                        '--logger', 'trx;LogFileName=native.trx', '/m:2', '/nr:false', '/p:UseSharedCompilation=false'],
                        cwd=ROOT, env=test_env, stdout=test_log, stderr=subprocess.STDOUT, check=True)
                trx = reports / 'native.trx'
                counts = ET.parse(trx).find('.//{*}Counters').attrib
                if counts['passed'] != '1' or counts['total'] != '1' or counts['notExecuted'] != '0':
                    raise RuntimeError('Expected one real native pass and zero skips')
                if list(source.glob('*.pending.json')):
                    raise RuntimeError('Unresolved native fixture outcome')
                if (source / 'demo-ready.json').exists():
                    raise RuntimeError('Consumer shutdown incomplete')
                durable(reports / 'result.json', {'passed': 1, 'trxSha256': hashlib.sha256(trx.read_bytes()).hexdigest()})
            finally:
                if node.poll() is not None:
                    raise RuntimeError('Owned native process exited before explicit shutdown')
                result = subprocess.run([sys.executable, '-B', str(ROOT / 'Scripts/TestHosts/Signal-OwnedConsoleProcess.py'),
                                         '--pid', str(node.pid)], capture_output=True, text=True, check=True)
                receipt = json.loads(result.stdout)
                if receipt['targetPid'] != node.pid or receipt['ctrlCSent'] is not True:
                    raise RuntimeError('Owned console signal differs')
                code = node.wait(timeout=30)
                durable(reports / 'stopped.json', dict(receipt, exitCode=code))
                if code != 0:
                    raise RuntimeError('Native shutdown did not return success')
        print(json.dumps({'phase': phase, 'completed': True, 'stopped': True}))


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    mode = parser.add_mutually_exclusive_group(required=True)
    mode.add_argument('--initialize', action='store_true')
    mode.add_argument('--retained', action='store_true')
    mode.add_argument('--reader', action='store_true')
    mode.add_argument('--admit-reader', action='store_true')
    mode.add_argument('--paired', action='store_true')
    parser.add_argument('--serve', action='store_true')
    args = parser.parse_args()
    if args.serve and not args.paired:
        parser.error('Interactive serving requires the paired mode')
    run(args.initialize, args.reader or args.admit_reader, args.admit_reader, args.paired, args.serve)
