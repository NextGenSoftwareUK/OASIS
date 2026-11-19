"""Native CQL acceptance using the same original Typesense source across restart."""
from pathlib import Path
import argparse, fcntl, hashlib, json, os, pwd, runpy, socket, subprocess, sys, time
import xml.etree.ElementTree as ET
sys.dont_write_bytecode = True
from cosmic_linux_mongo_service import owned_linux_mongo
parser = argparse.ArgumentParser()
parser.add_argument('--federated', action='store_true')
parser.add_argument('--serve', action='store_true')
options = parser.parse_args()
assert not options.serve or options.federated

ROOT = Path(__file__).resolve().parents[2]
STATE = Path('/var/tmp/oasis-typesense-cql')
RESULTS = ROOT/'TestResults/TypesenseCqlCurrent'
NAME = 'oasis-typesense-cql'
OWNER = pwd.getpwnam('dellams').pw_uid
DOTNET = '/var/tmp/oasis-runtimes/dotnet-10.0.12/dotnet'
VSTEST = '/mnt/c/Users/david/.nuget/packages/microsoft.testplatform.portable/18.11.0/tools/net8.0/vstest.console.dll'
DLL = ROOT/'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.TypesenseOASIS.IntegrationTests/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Providers.TypesenseOASIS.IntegrationTests.dll'
setup = runpy.run_path(str(ROOT/'Scripts/TestHosts/Create-CosmicTypesenseEnvironment.py'))
command = setup['command']


def inspect():
    current = json.loads(command('inspect', NAME))[0]
    receipt = json.loads((STATE/'container.ack.json').read_text())
    assert current['Id'] == receipt['id'] and current['Image'] == receipt['imageId']
    assert current['HostConfig']['PortBindings'] == {
        '8108/tcp': [{'HostIp': '127.0.0.1', 'HostPort': '18108'}]}
    assert any(m['Source'] == str(STATE/'data') and m['Destination'] == '/typesense-data' for m in current['Mounts'])
    return current


def demote():
    os.setgroups([])
    os.setgid(OWNER)
    os.setuid(OWNER)


def main():
    assert os.geteuid() == 0
    original = json.loads((STATE/'environment.json').read_text())['identity']
    assert not list(STATE.glob('*.pending.json')), 'Reconcile unknown outcome without replay'
    proofs = []
    with (STATE/'runner.lock').open('a') as lock:
        fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
        for phase in ['native-first', 'native-restart']:
            assert inspect()['State']['Status'] == 'exited'
            for port in [18108]:
                with socket.socket() as probe:
                    assert probe.connect_ex(('127.0.0.1', port)) != 0
            command('start', NAME)
            try:
                assert setup['ready']() == original
                environment = json.loads((STATE/'environment.json').read_text())
                assert (STATE/'data').stat().st_ino == environment['sourceDirectoryInode']
                stores = {name: (STATE/'data'/name/'IDENTITY').read_text().strip() for name in ['db', 'meta']}
                assert all(stores.values())
                store_receipt = STATE/'native-store-identity.ack.json'
                if store_receipt.exists():
                    original_stores = json.loads(store_receipt.read_text())
                    # v30.2 raft_server.cpp:start reloads the derived db from the
                    # retained Raft source when no snapshot exists. Only meta ID
                    # is a stable source identity; preserve original db observation.
                    assert stores['meta'] == original_stores['meta'], 'Original metadata store identity changed'
                    if stores['db'] != original_stores['db'] and not (STATE/'native-db-replay-observation.ack.json').exists():
                        setup['durable']('native-db-replay-observation.ack.json', {
                            'original': original_stores, 'reopened': stores,
                            'reason': 'Typesense30.2 native Raft replay recreates derived db; original receipt retained'})
                else:
                    assert not (STATE/'demo-seed.json').exists() and not (STATE/'demo-seed.pending.json').exists()
                    assert setup['request']('/collections') == []
                    setup['durable']('native-store-identity.ack.json', stores)
                def report(phase, test='TypesenseCqlNativeTests', extra=None):
                    env = {**os.environ, 'DOTNET_ROOT': '/var/tmp/oasis-runtimes/dotnet-10.0.12',
                        'OASIS_TYPESENSE_CQL_STATE': str(STATE), 'SSL_CERT_FILE': str(STATE/'ca.pem'), **(extra or {})}
                    started = time.time()
                    with (RESULTS/(phase+'.log')).open('w') as log:
                        subprocess.run([DOTNET, VSTEST, str(DLL), '--TestCaseFilter:FullyQualifiedName~'+test,
                        '--ResultsDirectory:'+str(RESULTS), '--Logger:trx;LogFileName='+phase+'.trx'],
                        cwd=RESULTS, env=env, preexec_fn=demote, stdout=log, stderr=subprocess.STDOUT, check=True)
                    trx = RESULTS/(phase+'.trx')
                    assert trx.stat().st_mtime >= started
                    counts = ET.parse(trx).find('.//{*}Counters').attrib
                    assert counts['passed'] == counts['total'] == '1' and counts['notExecuted'] == '0'
                    proofs.append({'phase': phase, 'passed': 1, 'sha256': hashlib.sha256(trx.read_bytes()).hexdigest()})
                report(phase)
                if phase == 'native-restart' and options.federated:
                    owner = Path('/var/tmp/oasis-cosmic-mongo/data').stat().st_uid
                    assert owner != 0
                    os.setresuid(owner, owner, 0)
                    try:
                        with owned_linux_mongo(RESULTS) as mongo_env:
                            os.setresuid(0, 0, 0)
                            try:
                                report('mongo-init', 'LinuxMongoDemoEnvironmentTests', mongo_env)
                                report('mongo-federation', 'TypesenseMongoCqlNativeTests',
                                    {**mongo_env, 'COSMIC_TYPESENSE_MONGO_SERVE': '1' if options.serve else '0'})
                                assert not (STATE/'mongo.pending.json').exists()
                            finally:
                                os.setresuid(owner, owner, 0)
                    finally:
                        os.setresuid(0, 0, 0)
                    report('native-postpair')
            finally:
                setup['stop']()
                assert not inspect()['State']['Running']
    assert not list(STATE.glob('*.pending.json'))
    print(json.dumps({'passed': True, 'proofs': proofs, 'stopped': True,
        'stateBytes': sum(p.stat().st_size for p in STATE.rglob('*') if p.is_file())}))


if __name__ == '__main__':
    main()
