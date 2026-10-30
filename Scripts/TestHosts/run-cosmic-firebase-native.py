"""Verify original RTDB emulator fixture/export restore without replaying unresolved writes."""
import hashlib
import json
import msvcrt
import os
from pathlib import Path
import socket
import subprocess
import time
import urllib.error
import urllib.request
import uuid
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
STATE = ROOT / 'LocalState/COSMIC/FirebaseCql'
RESULTS = ROOT / 'TestResults/FirebaseCqlCurrent'
JAR = Path('C:/Users/david/.cache/firebase/emulators/firebase-database-emulator-v4.11.2.jar')
JAVA = Path('C:/Program Files/Microsoft/jdk-21.0.12.101-hotspot/bin/java.exe')
PROJECT = ROOT / 'Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.FirebaseOASIS.IntegrationTests'
assert hashlib.sha256(JAR.read_bytes()).hexdigest() == 'b70d99344caf17c98b6f910fa8f6edf32a7c016cb1035e8915f70d38901eb97f'
STATE.mkdir(parents=True, exist_ok=True)
RESULTS.mkdir(parents=True, exist_ok=True)

def durable(path, value):
    with path.open('x', encoding='utf-8') as output:
        json.dump(value, output, indent=2); output.flush(); os.fsync(output.fileno())

def request(path='/', method='GET', value=None, owner=True):
    url = 'http://127.0.0.1:19091' + path.rstrip('/') + '/.json' if path == '/' else 'http://127.0.0.1:19091' + path + '.json'
    url += '?ns=cosmic-cql-demo'
    headers = {'Content-Type': 'application/json'}
    if owner: headers['Authorization'] = 'Bearer owner'
    data = None if value is None else json.dumps(value, separators=(',', ':')).encode()
    with urllib.request.urlopen(urllib.request.Request(url, data=data, method=method, headers=headers), timeout=15) as response:
        return json.load(response)

def canonical(value): return json.dumps(value, sort_keys=True, separators=(',', ':')).encode()
def digest(value): return hashlib.sha256(canonical(value)).hexdigest()
def verify_source(value):
    assert set(value) == {'oasis_holons'}
    identity = json.loads((STATE / 'identity.json').read_text())
    records = value['oasis_holons']
    assert len(records) == 4 and set(records) == set(identity['ids'])
    assert all(key == row['id'] for key, row in records.items())
    assert sum(row.get('deletedDate', '0001-01-01T00:00:00') != '0001-01-01T00:00:00' for row in records.values()) == 1

def report(name, seed):
    env = dict(os.environ, COSMIC_FIREBASE_ROOT=str(STATE), COSMIC_FIREBASE_SEED='1' if seed else '0')
    started = time.time()
    with (RESULTS / (name + '.log')).open('w') as output:
        subprocess.run(['dotnet', 'test', str(PROJECT), '--no-build', '--no-restore', '--filter', 'FullyQualifiedName~FirebaseNativeCqlTests',
                        '--results-directory', str(RESULTS), '--logger', 'trx;LogFileName=' + name + '.trx',
                        '/m:2', '/nr:false', '/p:UseSharedCompilation=false'], env=env, stdout=output, stderr=subprocess.STDOUT, check=True)
    path = RESULTS / (name + '.trx')
    assert path.stat().st_mtime >= started
    counts = ET.parse(path).find('.//{*}Counters').attrib
    assert counts['passed'] == counts['total'] == '1' and counts['notExecuted'] == '0'
    return hashlib.sha256(path.read_bytes()).hexdigest()

with (STATE / 'runner.lock').open('a+b') as lock:
    lock.seek(0); msvcrt.locking(lock.fileno(), msvcrt.LK_NBLCK, 1)
    if list(STATE.glob('*.pending')) or (STATE / 'active-process.json').exists():
        raise RuntimeError('Unresolved original outcome; preserve live source and inspect without replay')
    with socket.socket() as port: assert port.connect_ex(('127.0.0.1', 19091)) != 0
    fresh = not (STATE / 'identity.json').exists()
    if fresh:
        assert {p.name for p in STATE.iterdir()} == {'runner.lock'}
        durable(STATE / 'identity.json', {'actor': str(uuid.uuid4()), 'ids': [str(uuid.uuid4()) for _ in range(4)], 'missing': str(uuid.uuid4())})
    else:
        assert (STATE / 'checkpoint.json').exists() and (STATE / 'native-baseline.json').exists(), 'Original source checkpoint missing; no reseed'
    proofs = []
    for iteration in range(2):
        with (RESULTS / ('server-' + str(iteration) + '.log')).open('w') as output:
            process = subprocess.Popen([str(JAVA), '-Xms64m', '-Xmx256m', '-XX:ActiveProcessorCount=2', '-jar', str(JAR),
                                        '--host', '127.0.0.1', '--port', '19091', '--single_project_mode', 'Error'],
                                       cwd=RESULTS, stdout=output, stderr=subprocess.STDOUT, creationflags=subprocess.CREATE_NO_WINDOW)
            durable(STATE / 'active-process.json', {'pid': process.pid, 'jarSha256': hashlib.sha256(JAR.read_bytes()).hexdigest()})
            safe_to_stop = False
            try:
                deadline = time.monotonic() + 30
                while True:
                    if process.poll() is not None: raise RuntimeError('Owned emulator exited before readiness')
                    try:
                        assert request() is None
                        break
                    except OSError:
                        if time.monotonic() >= deadline: raise
                        time.sleep(.1)
                request('/.settings/rules', 'PUT', {'rules': {'.read': False, '.write': False}})
                try: request(owner=False)
                except urllib.error.HTTPError as error: assert error.code in [401, 403]
                else: raise RuntimeError('Public read unexpectedly admitted')
                if not fresh or iteration > 0:
                    checkpoint = json.loads((STATE / 'checkpoint.json').read_text())
                    verify_source(checkpoint)
                    assert digest(checkpoint) == json.loads((STATE / 'native-baseline.json').read_text())['sha256']
                    durable(STATE / 'restore.pending', {'sourceHash': digest(checkpoint), 'pid': process.pid, 'protocol': 'explicit emulator export import'})
                    restored = request('/', 'PUT', checkpoint)
                    assert digest(restored) == digest(checkpoint) and digest(request()) == digest(checkpoint)
                    (STATE / 'restore.pending').unlink()
                else: durable(STATE / 'seed.pending', {'records': 4})
                proofs.append(report('native' if iteration == 0 else 'restart', fresh and iteration == 0))
                observed = request(); verify_source(observed)
                source_hash = digest(observed)
                if fresh and iteration == 0:
                    durable(STATE / 'checkpoint.json', observed)
                    durable(STATE / 'native-baseline.json', {'sha256': source_hash, 'records': 4, 'live': 3, 'deleted': 1})
                    (STATE / 'seed.pending').rename(STATE / 'seed.ack')
                else: assert source_hash == json.loads((STATE / 'native-baseline.json').read_text())['sha256']
                safe_to_stop = True
            finally:
                if safe_to_stop:
                    # Emulator is volatile. Stop only after verifying its exact
                    # source checkpoint; this is not a clean-disk durability claim.
                    process.terminate(); process.wait(timeout=20)
                    (STATE / 'active-process.json').unlink()
                elif process.poll() is None:
                    print('PRESERVED live volatile emulator PID' + str(process.pid) + '; unresolved original source, do not kill/reseed')
    print(json.dumps({'nativePassed': 1, 'exportRestorePassed': 1, 'sourceHash': source_hash, 'trxHashes': proofs,
                      'stateBytes': sum(p.stat().st_size for p in STATE.rglob('*') if p.is_file()), 'serviceStoppedAfterCheckpoint': True}))
