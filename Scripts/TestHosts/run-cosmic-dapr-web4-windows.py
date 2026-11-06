import argparse
import contextlib
import hashlib
from http.client import HTTPConnection
import json
import os
from pathlib import Path
import secrets
import socket
import subprocess
import time
from urllib.parse import quote
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
parser = argparse.ArgumentParser(description='Run native Dapr CQL over the loopback WEB4 HTTP test host.')
parser.add_argument('--daprd', required=True, type=Path)
parser.add_argument('--garnet', required=True, type=Path)
parser.add_argument('--results-directory', required=True, type=Path)
arguments = parser.parse_args()
DAPR = arguments.daprd.resolve(strict=True)
GARNET = arguments.garnet.resolve(strict=True)
PORTS = (54861, 54862, 54863, 54864)
TOKEN = secrets.token_hex(32)
ADMIN = secrets.token_hex(32)
READER = secrets.token_hex(32)
KEY = 'oasis/holon/31f03d43c0084ad68dc631fd540b285e'
STORE = '/v1.0/state/cosmic-teaching'
RUN = arguments.results_directory.resolve(strict=True) / ('garnet-reader-' + secrets.token_hex(8))


def teaching_records():
    prefix = '31f03d43-c008-4ad6-8dc6-31fd540b285'
    return [{'key': 'oasis/holon/' + (prefix + suffix).replace('-', ''),
             'value': json.dumps({'id': prefix + suffix, 'isPublic': True, 'createdByAvatarId': prefix + '0',
                                  'deletedDate': '2026-11-01T00:00:00Z' if suffix == '3' else '0001-01-01T00:00:00'})}
            for suffix in ('e', '1', '2', '3')]


def require_free(ports):
    for port in ports:
        with socket.socket() as probe:
            probe.bind(('127.0.0.1', port))


def require_hash(path, expected):
    with path.open('rb') as file:
        if hashlib.file_digest(file, 'sha256').hexdigest() != expected:
            raise RuntimeError('Unexpected runtime executable digest')


@contextlib.contextmanager
def launch(args, name, env=None):
    with (RUN / f'{name}.log').open('wb') as log:
        process = subprocess.Popen(args, cwd=RUN, env=env, stdout=log, stderr=subprocess.STDOUT,
                                   creationflags=subprocess.CREATE_NO_WINDOW)
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
    raise RuntimeError('Owned fixture process did not become ready')


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


require_hash(DAPR, '1013629f8224e73d64aaa45a122e16c54638ccc07c3e9d5178f5e6d575a61897')
require_hash(GARNET, '0ce799cff60409b3cfd64a85fa19669474eef157d9b369ecc31c41e456c4f2a5')
require_free(PORTS)
(RUN / 'components').mkdir(parents=True)
acl = RUN / 'users.acl'
acl.write_text(f'user default on >{ADMIN} ~* +@all\n'
               f'user reader on >{READER} ~* +@read +@connection +command +info\n', encoding='utf-8')
try:
    with launch([str(GARNET), '--bind', '127.0.0.1', '--port', str(PORTS[3]), '--auth', 'ACL',
                 '--acl-file', str(acl), '--memory', '64m', '--lua'], 'garnet') as process:
        await_ready(process, garnet_ready)
        records = teaching_records()
        with sidecar('writer', 'default', ADMIN):
            if http('POST', STORE, records)[0] != 204:
                raise RuntimeError('Dapr seed failed')
        target = STORE + '/' + quote(KEY, safe='')
        with sidecar('reader', 'reader', READER):
            before = http('GET', target)
            if before[0] != 200 or not before[1] or json.loads(before[2]) != records[0]['value']:
                raise RuntimeError('Restricted Dapr read did not return the seeded record')
            denied = http('POST', STORE, [{'key': KEY, 'value': 'changed'}])
            if denied[0] != 500 or b'NOPERM' not in denied[2]:
                raise RuntimeError('Redis ACL denial was not verified')
            if http('GET', target) != before:
                raise RuntimeError('Value or ETag changed after the rejected write')
            project = ROOT / 'ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests/NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests.csproj'
            with (RUN / 'web4.log').open('wb') as log:
                subprocess.run(['dotnet', 'test', str(project), '--no-build', '--no-restore',
                                '--filter', 'FullyQualifiedName~CqlDaprNativeHttpTests', '--results-directory', str(RUN),
                                '--logger', 'trx;LogFileName=web4.trx'], cwd=RUN,
                               env=os.environ | {'DAPR_API_TOKEN': TOKEN, 'COSMIC_DAPR_NATIVE_HTTP': 'owned-loopback-54862'},
                               stdout=log, stderr=subprocess.STDOUT, timeout=75, check=True)
            counters = ET.parse(RUN / 'web4.trx').getroot().find('.//{*}Counters')
            if counters is None or counters.get('passed') != '1' or counters.get('total') != '1':
                raise RuntimeError('The single native WEB4 HTTP test must pass without skips')
    require_free(PORTS)
    result = 'PASS: Dapr state.redis reads through the Garnet reader ACL and rejects writes; value and ETag unchanged.\n'
    (RUN / 'result.log').write_text(result, encoding='utf-8')
    print(result, end='')
finally:
    acl.unlink()
    print(RUN)
