"""Admit/reuse one PocketBase source; never replay unresolved initialization or writes."""
import fcntl
import hashlib
import json
import os
from pathlib import Path
import secrets
import signal
import socket
import subprocess
import time
import urllib.request
import uuid
import xml.etree.ElementTree as ET

ROOT = Path('/mnt/c/Source/OASIS-cosmic')
STATE = Path('/var/tmp/oasis-pocketbase-cql')
RESULTS = ROOT / 'TestResults/PocketBaseCqlCurrent'
DAEMON = Path('/var/tmp/oasis-runtimes/pocketbase-0.40.6/pocketbase')
DOTNET = '/var/tmp/oasis-runtimes/dotnet-10.0.12/dotnet'
RUNNER = '/mnt/c/Users/david/.nuget/packages/microsoft.testplatform.portable/18.11.0/tools/net8.0/vstest.console.dll'
ASSEMBLY = ROOT / 'Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.PocketBaseOASIS.IntegrationTests/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Providers.PocketBaseOASIS.IntegrationTests.dll'
assert hashlib.sha256(DAEMON.read_bytes()).hexdigest() == '8e9805c9d92c50a4ff2bce97579d13e2ddfa138cab10cf6f9e8f6db8016ffa30'
STATE.mkdir(mode=0o700, exist_ok=True)
RESULTS.mkdir(parents=True, exist_ok=True)

def durable(path, data):
    fd = os.open(path, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o600)
    with os.fdopen(fd, 'w') as output:
        json.dump(data, output, indent=2); output.flush(); os.fsync(output.fileno())

def request(path, payload=None, token=None):
    headers = {'Content-Type': 'application/json'}
    if token: headers['Authorization'] = token
    data = None if payload is None else json.dumps(payload).encode()
    with urllib.request.urlopen(urllib.request.Request('http://127.0.0.1:18090' + path, data=data, headers=headers), timeout=15) as response:
        return json.load(response)

def authenticate():
    return request('/api/collections/_superusers/auth-with-password',
                   {'identity': 'cosmic-demo@example.invalid', 'password': (STATE / 'admin-password').read_text()})['token']

def baseline():
    page = request('/api/collections/oasis_holons/records?sort=oasis_id&perPage=100&skipTotal=false', token=authenticate())
    assert page['totalItems'] == len(page['items']) == 4
    identity = json.loads((STATE / 'identity.json').read_text())
    assert {v['oasis_id'] for v in page['items']} == set(identity['ids'])
    assert sum(v['is_deleted'] for v in page['items']) == 1
    # Exact immutable data_json/projections/native IDs, excluding changing auth state.
    return hashlib.sha256(json.dumps(page['items'], sort_keys=True, separators=(',', ':')).encode()).hexdigest()

def report(name, seed):
    env = dict(os.environ, DOTNET_ROOT='/var/tmp/oasis-runtimes/dotnet-10.0.12', COSMIC_POCKETBASE_ROOT=str(STATE), COSMIC_POCKETBASE_SEED='1' if seed else '0')
    started = time.time()
    with (RESULTS / (name + '.log')).open('w') as output:
        subprocess.run([DOTNET, RUNNER, str(ASSEMBLY), '--TestCaseFilter:FullyQualifiedName~PocketBaseNativeCqlTests',
                        '--ResultsDirectory:' + str(RESULTS), '--Logger:trx;LogFileName=' + name + '.trx'],
                       env=env, stdout=output, stderr=subprocess.STDOUT, check=True)
    path = RESULTS / (name + '.trx')
    assert path.stat().st_mtime >= started
    count = ET.parse(path).find('.//{*}Counters').attrib
    assert count['total'] == count['passed'] == '1' and count['notExecuted'] == '0'
    return hashlib.sha256(path.read_bytes()).hexdigest()

with (STATE / 'runner.lock').open('a') as lock:
    fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
    if list(STATE.glob('*.pending')) or (STATE / 'active-process.json').exists():
        raise RuntimeError('Unresolved original outcome; preserve and inspect without replay')
    with socket.socket() as check:
        assert check.connect_ex(('127.0.0.1', 18090)) != 0, 'Port belongs to an existing service'
    fresh = not (STATE / 'identity.json').exists()
    if fresh:
        assert {p.name for p in STATE.iterdir()} == {'runner.lock'}
        durable(STATE / 'setup.pending', {'version': '0.40.6'})
        durable(STATE / 'identity.json', {'actor': str(uuid.uuid4()), 'ids': [str(uuid.uuid4()) for _ in range(4)], 'missing': str(uuid.uuid4())})
        fd = os.open(STATE / 'admin-password', os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o600)
        with os.fdopen(fd, 'w') as output:
            output.write(secrets.token_urlsafe(36)); output.flush(); os.fsync(output.fileno())
        with (RESULTS / 'setup.log').open('w') as output:
            subprocess.run([str(DAEMON), 'superuser', 'create', 'cosmic-demo@example.invalid', (STATE / 'admin-password').read_text(), '--dir=' + str(STATE / 'pb_data')],
                           stdout=output, stderr=subprocess.STDOUT, check=True)
    else:
        assert (STATE / 'native-baseline.json').exists(), 'Original native verification incomplete; no reseed'
    proofs = []
    for iteration in range(2):
        with (RESULTS / ('server-' + str(iteration) + '.log')).open('w') as output:
            process = subprocess.Popen([str(DAEMON), 'serve', '--http=127.0.0.1:18090', '--dir=' + str(STATE / 'pb_data'),
                                        '--migrationsDir=' + str(STATE / 'pb_migrations')], stdout=output, stderr=subprocess.STDOUT)
            durable(STATE / 'active-process.json', {'pid': process.pid, 'runtime': str(DAEMON)})
            try:
                deadline = time.monotonic() + 20
                while True:
                    if process.poll() is not None: raise RuntimeError('Owned PocketBase exited before readiness')
                    try:
                        assert request('/api/health')['code'] == 200
                        break
                    except (OSError, ValueError):
                        if time.monotonic() >= deadline: raise
                        time.sleep(.1)
                if fresh and iteration == 0:
                    fields = [{'name': name, 'type': kind} for name, kind in [('oasis_id', 'text'), ('parent_holon_id', 'text'), ('holon_type', 'number'), ('is_deleted', 'bool'), ('data_json', 'text')]]
                    fields[-1]['max'] = 4000000
                    request('/api/collections', {'name': 'oasis_holons', 'type': 'base', 'fields': fields,
                            'indexes': ['CREATE UNIQUE INDEX idx_cosmic_oasis_id ON oasis_holons (oasis_id)']}, authenticate())
                    (STATE / 'setup.pending').rename(STATE / 'setup.ack')
                    durable(STATE / 'seed.pending', {'actor': json.loads((STATE / 'identity.json').read_text())['actor']})
                if not fresh or iteration > 0:
                    assert baseline() == json.loads((STATE / 'native-baseline.json').read_text())['sha256']
                proofs.append(report('native' if iteration == 0 else 'restart', fresh and iteration == 0))
                digest = baseline()
                if fresh and iteration == 0:
                    durable(STATE / 'native-baseline.json', {'sha256': digest, 'records': 4, 'live': 3, 'deleted': 1})
                    (STATE / 'seed.pending').rename(STATE / 'seed.ack')
                else:
                    assert digest == json.loads((STATE / 'native-baseline.json').read_text())['sha256']
            finally:
                if process.poll() is None:
                    process.send_signal(signal.SIGINT)
                    process.wait(timeout=20)
                assert process.returncode == 0, 'Unexpected native shutdown; preserve process receipt'
                (STATE / 'active-process.json').unlink()
    print(json.dumps({'nativePassed': 1, 'restartPassed': 1, 'records': 4, 'live': 3, 'deleted': 1,
                      'sourceHash': digest, 'trxHashes': proofs, 'stateBytes': sum(p.stat().st_size for p in STATE.rglob('*') if p.is_file()), 'serviceStopped': True}))
