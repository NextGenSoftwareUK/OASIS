import argparse
import contextlib
from http.client import HTTPConnection
import json
import os
from pathlib import Path
import secrets
import socket
import subprocess
import time
import uuid
from urllib.parse import quote
import xml.etree.ElementTree as ET
from DaprNativeRuntimes import require_runtime, process_diagnostic

ROOT = Path(__file__).resolve().parents[2]
parser = argparse.ArgumentParser(description='Run native Dapr CQL over the loopback WEB4 HTTP test host.')
parser.add_argument('--daprd', required=True, type=Path)
parser.add_argument('--garnet', required=True, type=Path)
parser.add_argument('--results-directory', required=True, type=Path)
parser.add_argument('--runtime-directory', type=Path, help='Pinned Linux archives and extracted runtimes')
parser.add_argument('--startup-only', action='store_true', help='Diagnose backing-store startup without seed or HTTP test')
arguments = parser.parse_args()
DAPR = arguments.daprd.resolve(strict=True)
GARNET = arguments.garnet.resolve(strict=True)
PORTS = (54861, 54862, 54863, 54864)
TOKEN = secrets.token_hex(32)
ADMIN = secrets.token_hex(32)
READER = secrets.token_hex(32)
PREFIX = str(uuid.uuid4())[:-1]
KEY = 'oasis/holon/' + (PREFIX + 'e').replace('-', '')
PROCESSES = []
STORE = '/v1.0/state/cosmic-teaching'
RUN = arguments.results_directory.resolve(strict=True) / ('garnet-reader-' + secrets.token_hex(8))


def teaching_records():
    prefix = PREFIX
    return [{'key': 'oasis/holon/' + (prefix + suffix).replace('-', ''),
             'value': json.dumps({'id': prefix + suffix, 'isPublic': True, 'createdByAvatarId': prefix + '0',
                                  'deletedDate': '2026-11-01T00:00:00Z' if suffix == '3' else '0001-01-01T00:00:00'})}
            for suffix in ('e', '1', '2', '3')]


def require_free(ports):
    for port in ports:
        with socket.socket() as probe:
            probe.bind(('127.0.0.1', port))


@contextlib.contextmanager
def launch(args, name, env=None):
    with (RUN / f'{name}.log').open('wb') as log:
        process = subprocess.Popen(args, cwd=RUN, env=env, stdout=log, stderr=subprocess.STDOUT,
                                   creationflags=subprocess.CREATE_NO_WINDOW if os.name == 'nt' else 0)
        process.cosmic_log_path = RUN / f'{name}.log'
        PROCESSES.append(process)
        try:
            yield process
        finally:
            if process.poll() is None:
                process.terminate()
                process.wait(10)


def await_ready(process, check):
    deadline = time.monotonic() + 20
    while process.poll() is None and time.monotonic() < deadline:
        try:
            if check():
                return
        except (ConnectionError, TimeoutError):
            pass
        time.sleep(.2)
    diagnostic = process_diagnostic(process.cosmic_log_path, process.poll(), (TOKEN, ADMIN, READER))
    with (RUN / 'diagnostic.json').open('x') as output:
        json.dump(diagnostic, output)
    raise RuntimeError('Owned fixture process did not become ready: ' + json.dumps(diagnostic))


def http(method, path, body=None):
    connection = HTTPConnection('127.0.0.1', PORTS[0], timeout=3)
    try:
        connection.request(method, path, json.dumps(body) if body is not None else None,
                           {'dapr-api-token': TOKEN, 'Content-Type': 'application/json'})
        response = connection.getresponse()
        return response.status, response.getheader('ETag'), response.read(8192)
    finally:
        connection.close()


@contextlib.contextmanager
def sidecar(name, username, password):
    require_free(PORTS[:3])
    component = RUN / 'components' / 'redis.yaml'
    component.write_text(f'''apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: cosmic-teaching
spec:
  type: state.redis
  version: v1
  metadata:
  - name: redisHost
    value: "127.0.0.1:{PORTS[3]}"
  - name: redisUsername
    value: "{username}"
  - name: redisPassword
    value: "{password}"
scopes:
- cosmic-dapr-teaching
''', encoding='utf-8')
    try:
        with launch([str(DAPR), '--app-id', 'cosmic-dapr-teaching', '--dapr-listen-addresses', '127.0.0.1',
                     '--dapr-http-port', str(PORTS[0]), '--dapr-grpc-port', str(PORTS[1]),
                     '--dapr-internal-grpc-port', str(PORTS[2]), '--enable-metrics=false',
                     '--resources-path', str(component.parent)], name, os.environ | {'DAPR_API_TOKEN': TOKEN}) as process:
            await_ready(process, lambda: http('GET', '/v1.0/healthz')[0] == 204)
            yield
            if http('POST', '/v1.0/shutdown')[0] != 204 or process.wait(10) != 0:
                raise RuntimeError('Sidecar did not shut down cleanly')
    finally:
        component.unlink()


def garnet_ready():
    with socket.create_connection(('127.0.0.1', PORTS[3]), timeout=1) as connection:
        password = ADMIN.encode()
        connection.sendall(b'*3\r\n$4\r\nAUTH\r\n$7\r\ndefault\r\n$64\r\n' + password + b'\r\n')
        return connection.makefile('rb').readline() == b'+OK\r\n'


require_runtime('daprd', DAPR, arguments.runtime_directory)
require_runtime('garnet', GARNET, arguments.runtime_directory)
require_free(PORTS)
(RUN / 'components').mkdir(parents=True)
acl = RUN / 'users.acl'
acl.write_text(f'user default on >{ADMIN} ~* +@all\n'
               f'user reader on >{READER} ~* +@read +@connection +command +info\n', encoding='utf-8')
def run_http(stage):
    output = RUN / stage
    output.mkdir()
    project = ROOT / 'ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests/NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests.csproj'
    with (output / 'web4.log').open('wb') as log:
        subprocess.run(['dotnet', 'test', str(project), '--no-build', '--no-restore',
                        '--filter', 'FullyQualifiedName~CqlDaprNativeHttpTests', '--results-directory', str(output),
                        '--logger', 'trx;LogFileName=web4.trx'], cwd=RUN,
                       env=os.environ | {'DAPR_API_TOKEN': TOKEN, 'COSMIC_DAPR_NATIVE_HTTP': 'owned-loopback-54862', 'COSMIC_DAPR_NATIVE_ID_PREFIX': PREFIX},
                       stdout=log, stderr=subprocess.STDOUT, timeout=75, check=True)
    counters = ET.parse(output / 'web4.trx').getroot().find('.//{*}Counters')
    if counters is None or counters.get('passed') != '1' or counters.get('total') != '1':
        raise RuntimeError('The single native WEB4 HTTP test must pass without skips')


@contextlib.contextmanager
def backing_store(stage, recover):
    arguments = [str(GARNET), '--bind', '127.0.0.1', '--port', str(PORTS[3]), '--auth', 'ACL',
        '--acl-file', str(acl), '--memory', '64m', '--page', '4k', '--lua', '--aof', '--aof-commit-freq', '0',
        '--aof-commit-wait', '--aof-memory', '16m', '--aof-page-size', '8k', '--aof-segment-size', '1m',
        '--checkpointdir', str(RUN / 'store')]
    if recover:
        arguments += ['--recover', '--fail-on-recovery-error']
    require_free(PORTS)
    with launch(arguments, 'garnet-' + stage) as process:
        await_ready(process, garnet_ready)
        yield
    require_free(PORTS)


def verify_reader(stage, records, expected=None):
    with sidecar('reader-' + stage, 'reader', READER):
        observations = {row['key']: http('GET', STORE + '/' + quote(row['key'], safe='')) for row in records}
        for row in records:
            response = observations[row['key']]
            if response[0] != 200 or not response[1] or json.loads(response[2]) != row['value']:
                raise RuntimeError('Restricted Dapr read differs from original record')
        if expected is not None and observations != expected:
            raise RuntimeError('Recovered native values or ETags differ; never reseed')
        denied = http('POST', STORE, [{'key': KEY, 'value': 'changed'}])
        if denied[0] != 500 or b'NOPERM' not in denied[2]:
            raise RuntimeError('Redis ACL denial was not verified')
        after = {row['key']: http('GET', STORE + '/' + quote(row['key'], safe='')) for row in records}
        if after != observations:
            raise RuntimeError('Value or ETag changed after rejected write')
        run_http(stage)
        return observations


try:
    (RUN / 'store').mkdir()
    if arguments.startup_only:
        with backing_store('startup', recover=False):
            pass
        (RUN / 'startup.json').write_text(json.dumps({'backingStoreStartup': True, 'seedExecuted': False}))
        print('PASS: backing-store startup only; no seed, sidecar or WEB4 query.')
        raise SystemExit(0)
    records = teaching_records()
    with backing_store('initial', recover=False):
        with sidecar('writer', 'default', ADMIN):
            with (RUN / 'seed.intent.json').open('x') as intent:
                json.dump({'operation': str(uuid.uuid4()), 'prefix': PREFIX, 'keys': [row['key'] for row in records],
                           'source': str(RUN / 'store'), 'replayAllowed': False}, intent)
            if http('POST', STORE, records)[0] != 204:
                raise RuntimeError('Dapr seed failed; original intent requires reconciliation')
            with (RUN / 'seed.acknowledged.json').open('x') as ack:
                json.dump({'prefix': PREFIX, 'acknowledged': True, 'count': len(records)}, ack)
        original = verify_reader('initial', records)
    files = [file for file in (RUN / 'store').rglob('*') if file.is_file()]
    if not files or not any(file.stat().st_size for file in files):
        raise RuntimeError('Acknowledged native source has no durable files')
    # No writer sidecar or seed call exists in this recovery phase.
    with backing_store('restart', recover=True):
        verify_reader('restart', records, expected=original)
    require_free(PORTS)
    result = 'PASS: Dapr restricted reads, denied writes and WEB4 native queries passed before/after backing-store restart; original values and ETags unchanged; seed executed once.\n'
    (RUN / 'result.log').write_text(result, encoding='utf-8')
    print(result, end='')
except Exception as error:
    diagnostics = [process_diagnostic(process.cosmic_log_path, process.poll(), (TOKEN, ADMIN, READER))
                   for process in PROCESSES]
    with (RUN / 'failure-diagnostics.json').open('x') as output:
        json.dump({'errorType': type(error).__name__, 'seedIntent': (RUN / 'seed.intent.json').exists(),
                   'seedAcknowledged': (RUN / 'seed.acknowledged.json').exists(), 'prefix': PREFIX,
                   'processes': diagnostics}, output)
    print(json.dumps({'nativeFailureDiagnostics': diagnostics}))
    raise
finally:
    acl.unlink()
    print(RUN)
