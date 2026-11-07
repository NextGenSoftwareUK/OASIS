import argparse
import contextlib
from http.client import HTTPConnection
import json
import os
from pathlib import Path
import secrets
import signal
import socket
import subprocess
import time
import sys
import hashlib
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
parser.add_argument('--storage-probe-only', action='store_true', help='Diagnose one journalled native AOF write without Dapr or WEB4')
parser.add_argument('--provisioner', action='store_true', help='Qualify the shipping explicit Linux provisioner before two WEB4 recovery checks')
arguments = parser.parse_args()
DAPR = arguments.daprd.resolve(strict=True)
GARNET = arguments.garnet.resolve(strict=True)
CONTRACT = json.loads((ROOT / 'Docker/DaprRuntime/contract.json').read_text())
PORTS = tuple(CONTRACT['ports'][name] for name in ('http', 'grpc', 'internalGrpc', 'redis'))
TOKEN = secrets.token_hex(32)
ADMIN = secrets.token_hex(32)
READER = secrets.token_hex(32)
PREFIX = str(uuid.uuid4())[:-1]
KEY = 'oasis/holon/' + (PREFIX + 'e').replace('-', '')
PROCESSES = []
STORE = '/v1.0/state/' + CONTRACT['store']
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


def native_configuration(function, *values):
    script = "const fs=require('node:fs'),config=require(process.argv[1]);const input=JSON.parse(fs.readFileSync(0,'utf8'));process.stdout.write(JSON.stringify(config[input.function](...input.values)));"
    result = subprocess.run(['node', '-e', script, str(ROOT / 'Docker/DaprRuntime/configuration.cjs')],
        input=json.dumps({'function': function, 'values': values}).encode(), capture_output=True, timeout=10)
    if result.returncode:
        raise RuntimeError('Shared native configuration generation failed; no native source mutation')
    return json.loads(result.stdout)


@contextlib.contextmanager
def sidecar(name, username, password):
    require_free(PORTS[:3])
    component = RUN / 'components' / 'redis.yaml'
    component.write_text(native_configuration('component', password, username), encoding='utf-8')
    try:
        with launch([str(DAPR), *native_configuration('daprArguments', str(RUN))], name, os.environ | {'DAPR_API_TOKEN': TOKEN}) as process:
            await_ready(process, lambda: http('GET', '/v1.0/healthz')[0] == 204)
            yield
            if http('POST', '/v1.0/shutdown')[0] != 204 or process.wait(10) != 0:
                raise RuntimeError('Sidecar did not shut down cleanly')
    finally:
        component.unlink()


def garnet_authentication(username, secret):
    with socket.create_connection(('127.0.0.1', PORTS[3]), timeout=1) as connection:
        password = secret.encode()
        principal = username.encode()
        connection.sendall(b'*3\r\n$4\r\nAUTH\r\n$' + str(len(principal)).encode() + b'\r\n' + principal + b'\r\n$64\r\n' + password + b'\r\n')
        return connection.makefile('rb').readline(4096)


def garnet_ready(username='default', secret=ADMIN):
    return garnet_authentication(username, secret) == b'+OK\r\n'


require_runtime('daprd', DAPR, arguments.runtime_directory)
require_runtime('garnet', GARNET, arguments.runtime_directory)
require_free(PORTS)
(RUN / 'components').mkdir(parents=True)
acl = RUN / 'reader.acl'
acl.write_text(native_configuration('writerAcl', ADMIN, READER), encoding='utf-8')

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
    acl.write_text(native_configuration('readerAcl', READER) if recover else native_configuration('writerAcl', ADMIN, READER), encoding='utf-8')
    arguments = [str(GARNET), *native_configuration('garnetArguments', str(RUN), str(acl), 'recover' if recover else 'provision')]
    require_free(PORTS)
    with launch(arguments, 'garnet-' + stage) as process:
        await_ready(process, lambda: garnet_ready('reader', READER) if recover else garnet_ready())
        if recover:
            rejected = garnet_authentication('default', ADMIN)
            if not rejected.startswith(b'-') or not rejected.endswith(b'\r\n'):
                raise RuntimeError('The original provisioning writer still authenticates after recovery')
        yield
    require_free(PORTS)


def prepare_hosted_source(records, observations):
    password_file = RUN / 'reader-password'
    with password_file.open('x', encoding='utf-8') as output:
        output.write(READER)
    password_file.chmod(0o600)
    acl.write_text(native_configuration('readerAcl', READER), encoding='utf-8')
    acl.chmod(0o600)
    component = RUN / 'components/redis.yaml'
    with component.open('x', encoding='utf-8') as output:
        output.write(native_configuration('component', READER, 'reader'))
    component.chmod(0o600)
    source = {'schema': 1, 'sourceOwner': PREFIX + '0',
        'ids': [json.loads(row['value'])['id'] for row in records[:3]],
        'sidecarTokenSha256': hashlib.sha256(TOKEN.encode()).hexdigest(),
        'records': [{'id': json.loads(row['value'])['id'], 'value': row['value'], 'etag': observations[row['key']][1]} for row in records],
        'configurationSha256': {name: hashlib.sha256((RUN / name).read_bytes()).hexdigest()
            for name in ['reader.acl', 'components/redis.yaml']}}
    with (RUN / 'source.acknowledged.json').open('x', encoding='utf-8') as output:
        json.dump(source, output)


def verify_hosted_source():
    environment = {'OASIS_SANDBOX_DAPR': 'true', 'CQL_DAPR_QUERY_STORE': CONTRACT['store'],
        'CQL_DAPR_QUERY_GRPC_ENDPOINT': 'http://127.0.0.1:' + str(PORTS[1]) + '/',
        'CQL_DAPR_QUERY_PROBE_ID': PREFIX + 'e', 'CQL_DAPR_QUERY_SIDECAR_TOKEN': TOKEN,
        'CQL_PUBLIC_READ_CATALOGUES_JSON': json.dumps({'DaprOASIS': [PREFIX + suffix for suffix in ['e', '1', '2']]})}
    script = "const fs=require('node:fs'),runtime=require(process.argv[1]);const input=JSON.parse(fs.readFileSync(0,'utf8'));(async()=>{const services=await runtime.serviceConfigurations(input.environment,input.root);for(const service of services)await service.check(new AbortController().signal);process.stdout.write(JSON.stringify({recoveryServices:services.length,originalRecordsVerified:4,recoveryOnly:true}));})().catch(error=>{process.stderr.write(error.message);process.exitCode=1;});"
    result = subprocess.run(['node', '-e', script, str(ROOT / 'Docker/host-sandbox-dapr.cjs')],
        input=json.dumps({'root': str(RUN), 'environment': environment}).encode(), capture_output=True, timeout=40)
    if result.returncode:
        raise RuntimeError('Hosted recovery helper failed: ' + result.stderr.decode(errors='replace')[-2048:])
    proof = json.loads(result.stdout)
    if proof != {'recoveryServices': 2, 'originalRecordsVerified': 4, 'recoveryOnly': True}:
        raise RuntimeError('Hosted recovery helper proof differs')
    with (RUN / 'runtime-source-proof.json').open('x') as output:
        json.dump(proof, output)


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
        if stage == 'restart' and sys.platform == 'linux':
            verify_hosted_source()
        run_http(stage)
        return observations


def storage_probe():
    key = 'cosmic-native-diagnostic/' + str(uuid.uuid4())
    with (RUN / 'storage-probe.intent.json').open('x') as output:
        json.dump({'key': key, 'operation': 'one isolated HSET', 'replayAllowed': False}, output)
    def request(*parts):
        encoded = [part.encode() for part in parts]
        return b'*' + str(len(encoded)).encode() + b'\r\n' + b''.join(
            b'$' + str(len(part)).encode() + b'\r\n' + part + b'\r\n' for part in encoded)
    with socket.create_connection(('127.0.0.1', PORTS[3]), timeout=3) as connection:
        reader = connection.makefile('rb')
        connection.sendall(request('AUTH', 'default', ADMIN))
        if reader.readline(4096) != b'+OK\r\n':
            raise RuntimeError('Native storage diagnostic authentication failed')
        connection.sendall(request('HSET', key, 'value', 'owned-native-probe'))
        if reader.readline(4096) != b':1\r\n':
            raise RuntimeError('Native AOF HSET acknowledgement differs')
    with (RUN / 'storage-probe.acknowledged.json').open('x') as output:
        json.dump({'key': key, 'acknowledged': True}, output)


try:
    if arguments.provisioner:
        if sys.platform != 'linux':
            raise RuntimeError('Shipping provisioner qualification requires Linux')
        script = "const fs=require('node:fs'),p=require(process.argv[1]);const i=JSON.parse(fs.readFileSync(0,'utf8'));p.provision(i.plan,i.directory,i.binaries).then(result=>process.stdout.write(JSON.stringify(result))).catch(error=>{process.stderr.write(error.message);process.exitCode=1;});"
        source_root = RUN / 'source'
        payload = json.dumps({'plan': {'schema': 1, 'prefix': PREFIX, 'readerPassword': READER, 'sidecarToken': TOKEN},
            'directory': str(source_root), 'binaries': {'garnet': str(GARNET), 'daprd': str(DAPR)}}).encode()
        with subprocess.Popen(['node', '-e', script, str(ROOT / 'Docker/provision-sandbox-dapr.cjs')],
                stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE, start_new_session=True) as creator:
            try:
                stdout, stderr = creator.communicate(payload, timeout=120)
            except subprocess.TimeoutExpired:
                # This isolated process group contains only this fixture's creator and its native children.
                os.killpg(creator.pid, signal.SIGTERM)
                try:
                    creator.communicate(timeout=10)
                except subprocess.TimeoutExpired:
                    os.killpg(creator.pid, signal.SIGKILL)
                    creator.communicate(timeout=5)
                raise RuntimeError('Shipping provisioner deadline expired; preserve original source intent, never resend')
            if creator.returncode:
                raise RuntimeError('Shipping provisioner failed: ' + stderr.decode(errors='replace')[-2048:])
        receipt = json.loads(stdout)
        if receipt.get('recoveryVerified') is not True or receipt.get('records') != 4:
            raise RuntimeError('Shipping provisioner acknowledgement differs')
        RUN = source_root
        acl = RUN / 'reader.acl'
        source = json.loads((RUN / 'source.acknowledged.json').read_text(encoding='utf-8'))
        records = [{'key': 'oasis/holon/' + row['id'].replace('-', ''), 'value': row['value']} for row in source['records']]
        with backing_store('first-recovery', recover=True):
            original = verify_reader('initial', records)
        with backing_store('second-recovery', recover=True):
            verify_reader('restart', records, expected=original)
        require_free(PORTS)
        result = 'PASS: shipping Dapr provisioner created once, verified durable reader-only recovery; two WEB4 recoveries preserved original values and ETags.\n'
        (RUN / 'result.log').write_text(result, encoding='utf-8')
        print(result, end='')
        raise SystemExit(0)
    (RUN / 'store').mkdir()
    if arguments.storage_probe_only:
        with backing_store('storage-probe', recover=False):
            storage_probe()
        print('PASS: one isolated native AOF HSET acknowledged; no Dapr seed or WEB4 query.')
        raise SystemExit(0)
    if arguments.startup_only:
        with backing_store('startup', recover=False):
            pass
        (RUN / 'startup.json').write_text(json.dumps({'backingStoreStartup': True, 'seedExecuted': False}))
        print('PASS: backing-store startup only; no seed, sidecar or WEB4 query.')
        raise SystemExit(0)
    records = teaching_records()
    with backing_store('initial', recover=False):
        with sidecar('writer', 'default', ADMIN):
            script = "const fs=require('node:fs'),seed=require(process.argv[1]);const input=JSON.parse(fs.readFileSync(0,'utf8'));seed.seedOnce(input.directory,input.prefix,input.records,input.token,new AbortController().signal).catch(()=>{process.stderr.write('Dapr source creation failed; reconcile original intent, never resend.');process.exitCode=1;});"
            subprocess.run(['node', '-e', script, str(ROOT / 'Docker/DaprRuntime/seed.cjs')],
                input=json.dumps({'directory': str(RUN), 'prefix': PREFIX, 'records': records, 'token': TOKEN}).encode(),
                capture_output=True, check=True, timeout=15)
        original = verify_reader('initial', records)
    files = [file for file in (RUN / 'store').rglob('*') if file.is_file()]
    if not files or not any(file.stat().st_size for file in files):
        raise RuntimeError('Acknowledged native source has no durable files')
    if sys.platform == 'linux':
        prepare_hosted_source(records, original)
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
