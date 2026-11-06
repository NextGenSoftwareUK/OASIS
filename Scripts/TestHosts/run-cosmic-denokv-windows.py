import argparse
import hashlib
import http.client
import json
import os
from pathlib import Path
import secrets
import socket
import subprocess
import time
import xml.etree.ElementTree as ET

root = Path(__file__).resolve().parents[2]
parser = argparse.ArgumentParser(description='Verify the native Deno KV CQL fixture on Windows.')
parser.add_argument('--denokv', required=True, type=Path)
parser.add_argument('--results-directory', required=True, type=Path)
arguments = parser.parse_args()
runtime = arguments.denokv.resolve(strict=True)
with runtime.open('rb') as binary:
    if hashlib.file_digest(binary, 'sha256').hexdigest() != '60ab2492e9fcea10db590c825c9242f9475600a6a6f729a6bd53d6032813a6b6':
        raise RuntimeError('Unexpected denokv executable hash')
port = 54865
with socket.socket() as check:
    check.bind(('127.0.0.1', port))
token = secrets.token_hex(32)
result = arguments.results_directory.resolve(strict=True) / ('standalone-native-' + secrets.token_hex(8))
result.mkdir()
with (result / 'server.log').open('wb') as log:
    process = subprocess.Popen([str(runtime), '--sqlite-path', ':memory:', 'serve', '--addr', f'127.0.0.1:{port}'],
                               cwd=result, env=os.environ | {'DENO_KV_ACCESS_TOKEN': token},
                               stdout=log, stderr=subprocess.STDOUT, creationflags=subprocess.CREATE_NO_WINDOW)
    try:
        deadline = time.monotonic() + 10
        while process.poll() is None and time.monotonic() < deadline:
            connection = http.client.HTTPConnection('127.0.0.1', port, timeout=1)
            try:
                connection.request('POST', '/', '{"supportedVersions":[2,3]}',
                                   {'Content-Type': 'application/json', 'Authorization': 'Bearer ' + token})
                reply = connection.getresponse()
                if reply.status != 200:
                    raise RuntimeError(f'KV Connect metadata HTTP {reply.status}')
                response = json.loads(reply.read(16 * 1024))
                evidence = {'metadataContentType': reply.getheader('Content-Type'),
                            'fields': sorted(response), **{key: response[key]
                            for key in ('version', 'uuid', 'databaseId', 'endpoints') if key in response}}
                key = b'\x02oasis/holon/31f03d43c0084ad68dc631fd540b285e\x00'
                end = key + b'\x00'
                read_range = bytes((10, len(key))) + key + bytes((18, len(end))) + end + b'\x18\x01'
                connection.request('POST', '/v2/snapshot_read', bytes((10, len(read_range))) + read_range,
                                   {'Content-Type': 'application/x-protobuf', 'Authorization': 'Bearer ' + response['token'],
                                    'x-denokv-version': str(response['version']), 'x-denokv-database-id': response['databaseId']})
                snapshot = connection.getresponse()
                payload = snapshot.read(64)
                if snapshot.status != 200 or payload != b'\x0a\x00\x20\x01\x40\x01':
                    raise RuntimeError(f'Unexpected native empty snapshot (HTTP {snapshot.status})')
                evidence['snapshotContentType'] = snapshot.getheader('Content-Type')
                evidence['emptySnapshotBytes'] = payload.hex()
                (result / 'metadata.json').write_text(json.dumps(evidence, indent=2), encoding='utf-8')
                print(json.dumps(evidence))
                break
            except (ConnectionError, TimeoutError):
                time.sleep(.2)
            finally:
                connection.close()
        else:
            raise RuntimeError('Owned denokv did not become ready')
        project = root / 'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.EdgeStorage.ProtocolTests/NextGenSoftware.OASIS.API.Providers.EdgeStorage.ProtocolTests.csproj'
        with (result / 'tests.log').open('wb') as test_log:
            subprocess.run(['dotnet', 'test', str(project), '--no-build', '--no-restore', '--filter',
                            'FullyQualifiedName~DenoStandaloneCqlTests|FullyQualifiedName~DenoHostedQueryProtocolTests|FullyQualifiedName~DenoKvConnectProtocolTests',
                            '--results-directory', str(result), '--logger', 'trx;LogFileName=standalone.trx'],
                           cwd=result, env=os.environ | {'COSMIC_DENO_KV_NATIVE_TOKEN': token},
                           stdout=test_log, stderr=subprocess.STDOUT, timeout=70, check=True)
        counters = ET.parse(result / 'standalone.trx').getroot().find('.//{*}Counters')
        if counters is None or counters.get('passed') != '15' or counters.get('total') != '15':
            raise RuntimeError('Expected 15 Deno checks to pass without skips')
    finally:
        if process.poll() is None:
            process.terminate()
            process.wait(10)
with socket.socket() as check:
    check.bind(('127.0.0.1', port))
print(result)
