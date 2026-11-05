"""Verify original RTDB emulator fixture/export restore without replaying unresolved writes."""
import argparse
from contextlib import contextmanager
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

parser = argparse.ArgumentParser()
parser.add_argument('--federated', action='store_true')
parser.add_argument('--recovery-laptop', action='store_true')
parser.add_argument('--initialize', action='store_true', help='Explicitly admit a new recovery fixture; never replay an existing identity')
parser.add_argument('--serve', action='store_true')
options = parser.parse_args()
if options.serve and not options.federated: raise RuntimeError('Serve requires federation')
if options.initialize and options.federated: raise RuntimeError('Initialize the native recovery checkpoint before federation')
ROOT = Path(__file__).resolve().parents[2]
STATE = ROOT / 'LocalState/COSMIC/FirebaseCql'
RESULTS = ROOT / 'TestResults/FirebaseCqlCurrent'
JAR = Path('C:/Users/david/.cache/firebase/emulators/firebase-database-emulator-v4.11.2.jar')
JAVA = Path('C:/Program Files/Microsoft/jdk-21.0.12.101-hotspot/bin/java.exe')
if options.initialize and not options.recovery_laptop:
    raise RuntimeError('Explicit initialization is confined to the separate recovery fixture')
if options.recovery_laptop:
    recovery = ROOT / 'LocalState/COSMIC/RecoveryLaptopFirebaseRtdb4112'
    STATE = recovery / 'FirebaseCql'
    RESULTS = recovery / ('run-' + uuid.uuid4().hex)
    JAR = recovery / 'firebase-database-emulator-v4.11.2.jar'
    JAVA = ROOT / 'LocalState/COSMIC/RecoveryLaptopSolr1010/jdk-21.0.13+9-jre/bin/java.exe'
    if options.initialize and STATE.exists():
        raise RuntimeError('Recovery fixture already exists; initialization cannot replay it')
    if not options.initialize and not (STATE / 'native-baseline.json').is_file():
        raise RuntimeError('Retained recovery checkpoint required; initialize a new source explicitly')
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

def report(name, seed, test="FirebaseNativeCqlTests", extra=None):
    env = dict(os.environ, COSMIC_FIREBASE_ROOT=str(STATE), COSMIC_FIREBASE_SEED='1' if seed else '0')
    env.update(extra or {})
    started = time.time()
    with (RESULTS / (name + '.log')).open('w') as output:
        subprocess.run(['dotnet', 'test', str(PROJECT), '--no-build', '--no-restore', '--filter', 'FullyQualifiedName~' + test,
                        '--results-directory', str(RESULTS), '--logger', 'trx;LogFileName=' + name + '.trx',
                        '/m:2', '/nr:false', '/p:UseSharedCompilation=false'], env=env, stdout=output, stderr=subprocess.STDOUT, check=True)
    path = RESULTS / (name + '.trx')
    assert path.stat().st_mtime >= started
    counts = ET.parse(path).find('.//{*}Counters').attrib
    assert counts['passed'] == counts['total'] == '1' and counts['notExecuted'] == '0'
    return hashlib.sha256(path.read_bytes()).hexdigest()

@contextmanager
def owned_windows_mongo():
    daemon = Path('C:/Program Files/MongoDB/Server/8.3/bin/mongod.exe')
    data = ROOT / 'LocalState/COSMIC/mongo-data'
    if not options.recovery_laptop:
        assert (data / 'WiredTiger').is_file()
        assert hashlib.sha256(daemon.read_bytes()).hexdigest() == 'fb5035767717c9d00ca1eada0a63be28a1758c6108dc614cd95c41465a98aabe'
    if options.recovery_laptop:
        receipt = json.loads((ROOT / 'LocalState/COSMIC/RecoveryLaptopMongo82/replica.ready.json').read_text(encoding='utf-8-sig'))
        daemon = Path('C:/Program Files/MongoDB/Server/8.2/bin/mongod.exe')
        data = ROOT / 'LocalState/COSMIC/RecoveryLaptopMongo82/data'
        assert receipt['runtime'] == daemon.as_posix() and receipt['address'] == '127.0.0.1:27029' and receipt['replica'] == 'oasisRecoveryRs'
        assert hashlib.sha256(daemon.read_bytes()).hexdigest().upper() == receipt['sha256'].upper()
        assert (data / 'WiredTiger').is_file()
    with socket.socket() as port: assert port.connect_ex(('127.0.0.1', 27029)) != 0
    path = RESULTS / 'mongo-service.log'
    path.write_text('')
    startup = subprocess.STARTUPINFO()
    startup.dwFlags |= subprocess.STARTF_USESHOWWINDOW
    startup.wShowWindow = subprocess.SW_HIDE
    with (RESULTS / 'mongo-console.log').open('w') as output:
        process = subprocess.Popen([str(daemon), '--dbpath', str(data), '--bind_ip', '127.0.0.1', '--port', '27029',
                                    '--replSet', 'oasisRecoveryRs', '--wiredTigerCacheSizeGB', '0.25', '--oplogSize', '32',
                                    '--logpath', str(path), '--logappend'], stdout=output, stderr=subprocess.STDOUT,
                                   creationflags=subprocess.CREATE_NEW_CONSOLE, startupinfo=startup)
        try:
            deadline = time.monotonic() + 40
            while True:
                if process.poll() is not None: raise RuntimeError('Owned Mongo exited before primary readiness')
                if 'Transition to primary complete' in path.read_text(errors='strict'): break
                if time.monotonic() >= deadline: raise RuntimeError('Mongo primary readiness deadline; no restart')
                time.sleep(.2)
            yield dict(OASIS_MONGO_REPLICA_SET_CONNECTION='mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000',
                       COSMIC_FIREBASE_MONGO_SERVE='1' if options.serve else '0')
        finally:
            if process.poll() is None:
                signal = subprocess.run(['python', '-B', str(ROOT / 'Scripts/TestHosts/Signal-OwnedConsoleProcess.py'), '--pid', str(process.pid)], capture_output=True, text=True, check=True)
                receipt = json.loads(signal.stdout)
                assert receipt['ctrlCSent'] and receipt['targetPid'] == process.pid
                process.wait(timeout=30)
                assert process.returncode == 12, 'Mongo Windows console shutdown must use native exit 12'
                events = [json.loads(line) for line in path.read_text(encoding='utf-8').splitlines()[-15:]]
                assert any(e.get('id') == 4795901 for e in events) and any(e.get('id') == 8423404 for e in events)
                assert any(e.get('id') == 23138 and e.get('attr', {}).get('exitCode') == 12 for e in events)
                durable(RESULTS / 'mongo-stopped.json', dict(receipt, exitCode=12, storageClosed=True, shutdownComplete=True))

with (STATE / 'runner.lock').open('a+b') as lock:
    lock.seek(0); msvcrt.locking(lock.fileno(), msvcrt.LK_NBLCK, 1)
    if list(STATE.glob('*.pending')) or (STATE / 'mongo.pending.json').exists() or (STATE / 'active-process.json').exists():
        raise RuntimeError('Unresolved original outcome; preserve live source and inspect without replay')
    with socket.socket() as port: assert port.connect_ex(('127.0.0.1', 19091)) != 0
    # Preserve completed integration evidence before starting the owned source.
    for name in ["native.trx", "restart.trx", "mongo-federation.trx"]:
        previous = RESULTS / name
        if previous.exists():
            count = ET.parse(previous).find(".//{*}Counters").attrib
            assert count["total"] == count["passed"] == "1" and count["notExecuted"] == "0", "Preserve unresolved prior report"
            archive = RESULTS / "Archive"
            archive.mkdir(exist_ok=True)
            destination = archive / (hashlib.sha256(previous.read_bytes()).hexdigest().upper() + "-" + name)
            assert not destination.exists(), "Prior report already archived; reconcile explicitly"
            previous.rename(destination)
    fresh = not (STATE / 'identity.json').exists()
    if fresh:
        if options.recovery_laptop and not options.initialize:
            raise RuntimeError('Recovery source creation requires explicit initialization')
        assert {p.name for p in STATE.iterdir()} == {'runner.lock'}
        durable(STATE / 'identity.json', {'actor': str(uuid.uuid4()), 'ids': [str(uuid.uuid4()) for _ in range(4)], 'missing': str(uuid.uuid4())})
    else:
        assert (STATE / 'checkpoint.json').exists() and (STATE / 'native-baseline.json').exists(), 'Original source checkpoint missing; no reseed'
    proofs = []
    paired_recovery = options.recovery_laptop and options.federated
    if paired_recovery and fresh:
        raise RuntimeError('Recovery federation requires a previously accepted native checkpoint')
    for iteration in range(1 if paired_recovery else 2):
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
                if not paired_recovery:
                    proofs.append(report('native' if iteration == 0 else 'restart', fresh and iteration == 0))
                if options.federated and (iteration == 1 or paired_recovery):
                    with owned_windows_mongo() as mongo_env:
                        proofs.append(report('mongo-federation', False, 'FirebaseMongoCqlNativeTests', mongo_env))
                        assert not (STATE / 'mongo.pending.json').exists()
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
    result = {'nativePassed': 0 if paired_recovery else 1, 'managerMongoHttpPassed': 1 if options.federated else 0, 'exportRestorePassed': 1, 'sourceHash': source_hash, 'trxHashes': proofs,
                      'stateBytes': sum(p.stat().st_size for p in STATE.rglob('*') if p.is_file()), 'serviceStoppedAfterCheckpoint': True, 'recoveryLaptop': options.recovery_laptop, 'results': str(RESULTS)}
    durable(RESULTS / 'acceptance.json', result)
    print(json.dumps(result))
