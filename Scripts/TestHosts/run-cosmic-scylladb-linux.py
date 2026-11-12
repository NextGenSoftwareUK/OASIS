"""Bounded original ScyllaDb source acceptance, with an actual native restart."""
from pathlib import Path
import argparse, sys, fcntl, hashlib, json, os, pwd, socket, subprocess, time
import xml.etree.ElementTree as ET

sys.dont_write_bytecode = True
from cosmic_linux_mongo_service import owned_linux_mongo
parser = argparse.ArgumentParser()
parser.add_argument('--resume-original-prewrite', action='store_true')
parser.add_argument('--federated', action='store_true')
parser.add_argument('--serve', action='store_true')
options = parser.parse_args()
assert not options.serve or options.federated
ROOT = Path(__file__).resolve().parents[2]
STATE = Path('/var/tmp/oasis-scylladb-cql')
RESULTS = ROOT/'TestResults/ScyllaDbCqlCurrent'
NAME = 'oasis-scylladb-cql'
OWNER = pwd.getpwnam('dellams').pw_uid
DOTNET = '/var/tmp/oasis-runtimes/dotnet-10.0.12/dotnet'
VSTEST = '/mnt/c/Users/david/.nuget/packages/microsoft.testplatform.portable/18.11.0/tools/net8.0/vstest.console.dll'
DLL = ROOT/'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.ScyllaDBOASIS.IntegrationTests/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Providers.ScyllaDBOASIS.IntegrationTests.dll'


def command(*args):
    return subprocess.check_output(['podman', *args], text=True).strip()


def inspect():
    record = json.loads(command('inspect', NAME))[0]
    original = json.loads((STATE/'container.ack.json').read_text())
    assert record['Id'] == original['id'] and record['Image'] == original['imageId']
    assert record['HostConfig']['PortBindings'] == {'9042/tcp': [{'HostIp': '127.0.0.1', 'HostPort': '19043'}]}
    assert any(m['Source'] == str(STATE/'data') and m['Destination'] == '/var/lib/scylla' for m in record['Mounts'])
    return record


def demote():
    os.setgroups([])
    os.setgid(OWNER)
    os.setuid(OWNER)


def main():
    assert os.geteuid() == 0
    original = json.loads((STATE/'environment.json').read_text())['observations'][0]['identity']
    pending = sorted(p.name for p in STATE.glob('*.pending.json'))
    assert not pending or options.resume_original_prewrite and pending == ['demo-seed.pending.json'], 'Reconcile unknown outcome without replay'
    proofs = []
    with (STATE/'runner.lock').open('a') as lock:
        fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
        for phase in ['native-first', 'native-restart']:
            assert inspect()['State']['Status'] == 'exited'
            with socket.socket() as probe:
                assert probe.connect_ex(('127.0.0.1', 19043)) != 0
            command('start', NAME)
            try:
                deadline = time.monotonic()+180
                while time.monotonic() < deadline:
                    assert inspect()['State']['Running']
                    response = subprocess.run(['podman', 'exec', '--user', '0', NAME, 'cqlsh',
                        '--cqlshrc=/run/oasis-secrets/client', '--connect-timeout=5', '--request-timeout=10',
                        '-e', 'SELECT host_id, cluster_name, release_version FROM system.local;', inspect()['NetworkSettings']['IPAddress']],
                        text=True, capture_output=True, timeout=20)
                    if response.returncode == 0 and response.stdout == original:
                        break
                    time.sleep(1)
                else:
                    raise RuntimeError('Original native identity/readiness deadline')
                def report(phase, test='ScyllaDbCqlNativeTests', extra=None):
                    env = {**os.environ, 'DOTNET_ROOT': '/var/tmp/oasis-runtimes/dotnet-10.0.12',
                        'OASIS_SCYLLADB_CQL_STATE': str(STATE), **(extra or {})}
                    started = time.time()
                    with (RESULTS/(phase+'.log')).open('w') as log:
                        subprocess.run([DOTNET, VSTEST, str(DLL), '--TestCaseFilter:FullyQualifiedName~'+test,
                            '--ResultsDirectory:'+str(RESULTS), '--Logger:trx;LogFileName='+phase+'.trx'],
                            env=env, cwd=RESULTS, preexec_fn=demote, stdout=log, stderr=subprocess.STDOUT, check=True)
                    trx = RESULTS/(phase+'.trx')
                    assert trx.stat().st_mtime >= started
                    counts = ET.parse(trx).find('.//{*}Counters').attrib
                    assert counts['passed'] == counts['total'] == '1' and counts['notExecuted'] == '0'
                    proofs.append({'phase': phase, 'passed': 1, 'sha256': hashlib.sha256(trx.read_bytes()).hexdigest()})
                report(phase, extra={'OASIS_SCYLLADB_RESUME_PREWRITE': '1'} if options.resume_original_prewrite and phase == 'native-first' else {})

                if phase == 'native-restart' and options.federated:
                    owner = Path('/var/tmp/oasis-cosmic-mongo/data').stat().st_uid
                    assert owner != 0
                    os.setresuid(owner, owner, 0)
                    try:
                        with owned_linux_mongo(RESULTS) as mongo_env:
                            os.setresuid(0, 0, 0)
                            try:
                                report('mongo-init', 'LinuxMongoDemoEnvironmentTests', mongo_env)
                                report('mongo-federation', 'ScyllaDBMongoCqlNativeTests',
                                    {**mongo_env, 'COSMIC_SCYLLADB_MONGO_SERVE': '1' if options.serve else '0'})
                                assert not (STATE/'mongo.pending.json').exists()
                            finally:
                                os.setresuid(owner, owner, 0)
                    finally:
                        os.setresuid(0, 0, 0)
                    report('native-postpair')

            finally:
                command('stop', '--time', '30', NAME)
                status = inspect()['State']
                assert not status['Running'] and status['ExitCode'] == 0
                assert not status['OOMKilled'] and not status['Error'] and status['StoppedByUser']
    assert not list(STATE.glob('*.pending.json'))
    print(json.dumps({'passed': True, 'proofs': proofs, 'stopped': True, 'exitCode': 0,
        'stateBytes': sum(p.stat().st_size for p in STATE.rglob('*') if p.is_file())}))


if __name__ == '__main__':
    main()
