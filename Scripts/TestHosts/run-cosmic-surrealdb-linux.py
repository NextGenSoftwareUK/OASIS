"""Reuse one original authenticated loopback SurrealKV store across native restarts."""
from pathlib import Path
import argparse, fcntl, hashlib, json, os, secrets, signal, socket, subprocess, time
from cosmic_linux_mongo_service import owned_linux_mongo
import urllib.error, urllib.request
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
STATE = Path('/var/tmp/oasis-surrealdb-cql')
RUNTIME = Path('/var/tmp/oasis-runtimes/surrealdb-3.3.5/surreal')
HASH = 'bd4ffffbf3daedb503fc876fa6d531578383ee10ed9b62649b9fa401b9375052'
RESULTS = ROOT / 'TestResults/SurrealDbCqlCurrent'
DOTNET = '/var/tmp/oasis-runtimes/dotnet-10.0.12/dotnet'
VSTEST = '/mnt/c/Users/david/.nuget/packages/microsoft.testplatform.portable/18.11.0/tools/net8.0/vstest.console.dll'
DLL = ROOT / 'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.SurrealDBOASIS.IntegrationTests/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Providers.SurrealDBOASIS.IntegrationTests.dll'

def durable(path, value):
    with path.open('x') as file:
        os.chmod(path, 0o600)
        json.dump(value, file)
        file.flush(); os.fsync(file.fileno())

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--federated", action="store_true")
    parser.add_argument("--serve", action="store_true")
    options = parser.parse_args()
    assert not options.serve or options.federated
    assert os.geteuid() != 0, 'Use the existing non-root runtime owner'
    assert hashlib.sha256(RUNTIME.read_bytes()).hexdigest() == HASH
    if not STATE.exists():
        STATE.mkdir(mode=0o700)
        durable(STATE / 'environment.pending.json', {'runtimeSha256': HASH, 'port': 18001})
        durable(STATE / 'credentials.json', {'username': 'cosmic_demo', 'password': secrets.token_hex(32)})
        durable(STATE / 'environment.json', {'runtimeSha256': HASH, 'port': 18001, 'store': str(STATE / 'data')})
        (STATE / 'environment.pending.json').unlink()
    assert STATE.stat().st_uid == os.getuid() and STATE.stat().st_mode & 0o077 == 0
    assert not (STATE / 'environment.pending.json').exists(), 'Unknown environment outcome; do not recreate'
    manifest = json.loads((STATE / 'environment.json').read_text())
    assert manifest == {'runtimeSha256': HASH, 'port': 18001, 'store': str(STATE / 'data')}
    credentials = STATE / 'credentials.json'
    assert credentials.stat().st_uid == os.getuid() and credentials.stat().st_mode & 0o077 == 0
    config = json.loads(credentials.read_text())
    RESULTS.mkdir(parents=True, exist_ok=True)
    proofs = []
    with (STATE / 'runner.lock').open('a') as lock:
        fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
        for phase in ['native-first', 'native-restart']:
            with socket.socket() as probe:
                assert probe.connect_ex(('127.0.0.1', 18001)) != 0, 'Existing listener is not owned by this run'
            env = {**os.environ, 'SURREAL_USER': config['username'], 'SURREAL_PASS': config['password'],
                   'SURREAL_TELEMETRY_DISABLE': 'true', 'TOKIO_WORKER_THREADS': '2'}
            with (RESULTS / (phase + '-server.log')).open('w') as log:
                node = subprocess.Popen([str(RUNTIME), 'start', '--no-banner', '--bind', '127.0.0.1:18001',
                    '--deny-guests', 'surrealkv://' + str(STATE / 'data')], env=env, stdout=log, stderr=subprocess.STDOUT)
                try:
                    for _ in range(120):
                        assert node.poll() is None, 'Owned server exited; preserve original source/logs'
                        try:
                            with urllib.request.urlopen('http://127.0.0.1:18001/health', timeout=1) as response:
                                if response.status == 200: break
                        except (ConnectionError, urllib.error.URLError): time.sleep(.25)
                    else: raise RuntimeError('Native readiness deadline')
                    def report(name, test_class='SurrealDbNativeCqlTests', extra=None):
                        test_env = {**os.environ, 'DOTNET_ROOT': str(Path(DOTNET).parent), 'COSMIC_SURREALDB_ROOT': str(STATE)}
                        test_env.update(extra or {})
                        started = time.time()
                        with (RESULTS / (name + '.log')).open('w') as test_log:
                            subprocess.run([DOTNET, VSTEST, str(DLL), '--TestCaseFilter:FullyQualifiedName~' + test_class,
                                '--ResultsDirectory:' + str(RESULTS), '--Logger:trx;LogFileName=' + name + '.trx'],
                                env=test_env, cwd=RESULTS, stdout=test_log, stderr=subprocess.STDOUT, check=True)
                        trx = RESULTS / (name + '.trx'); assert trx.stat().st_mtime >= started
                        counts = ET.parse(trx).find('.//{*}Counters').attrib
                        assert counts['passed'] == counts['total'] == '1' and counts['notExecuted'] == '0'
                        proofs.append({'phase': name, 'passed': 1, 'sha256': hashlib.sha256(trx.read_bytes()).hexdigest()})
                    report(phase)
                    if phase == 'native-restart' and options.federated:
                        with owned_linux_mongo(RESULTS) as mongo_env:
                            report('mongo-init', 'LinuxMongoDemoEnvironmentTests', mongo_env)
                            report('mongo-federation', 'SurrealDBMongoCqlNativeTests',
                                {**mongo_env, 'COSMIC_SURREALDB_MONGO_SERVE': '1' if options.serve else '0'})
                            assert not (STATE / 'mongo.pending.json').exists(), 'Mongo fixture cleanup unresolved'
                        report('native-postpair')

                finally:
                    if node.poll() is None: node.send_signal(signal.SIGINT)
                    assert node.wait(timeout=30) == 0, 'Owned shutdown incomplete; preserve source'
    assert not list(STATE.glob('*.pending.json')), 'Original acknowledgement remains unresolved'
    print(json.dumps({'passed': True, 'proofs': proofs, 'stopped': True,
        'stateBytes': sum(p.stat().st_size for p in STATE.rglob('*') if p.is_file())}))

if __name__ == '__main__': main()
