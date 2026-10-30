"""Verify original Chroma CQL records across restart; reuse the retained source."""
from pathlib import Path
import argparse, fcntl, hashlib, json, os, pwd, runpy, subprocess, time
import xml.etree.ElementTree as ET
from cosmic_linux_mongo_service import owned_linux_mongo
parser = argparse.ArgumentParser()
parser.add_argument('--federated', action='store_true')
parser.add_argument('--serve', action='store_true')
options = parser.parse_args()
assert not options.serve or options.federated

ROOT = Path(__file__).resolve().parents[2]
STATE = Path('/var/tmp/oasis-chroma-cql')
RESULTS = ROOT/'TestResults/ChromaCqlCurrent'
DOTNET = '/var/tmp/oasis-runtimes/dotnet-10.0.12/dotnet'
VSTEST = '/mnt/c/Users/david/.nuget/packages/microsoft.testplatform.portable/18.11.0/tools/net8.0/vstest.console.dll'
DLL = ROOT/'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.ChromaOASIS.IntegrationTests/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Providers.ChromaOASIS.IntegrationTests.dll'
OWNER = pwd.getpwnam('dellams').pw_uid
setup = runpy.run_path(str(ROOT/'Scripts/TestHosts/Create-CosmicChromaEnvironment.py'))


def demote():
    os.setgroups([])
    os.setgid(OWNER)
    os.setuid(OWNER)


def main():
    assert os.geteuid() == 0
    environment = json.loads((STATE/'environment.json').read_text())
    receipt = json.loads((STATE/'container.ack.json').read_text())
    assert not list(STATE.glob('*.pending.json')), 'Preserve unknown outcomes; never replay'
    with (STATE/'runner.lock').open('a') as lock:
        fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
        for phase in ['native-first', 'native-restart']:
            container = json.loads(setup['command']('inspect', setup['NAME']))[0]
            assert container['Id'] == receipt['id'] and container['Image'] == receipt['imageId'] == setup['IMAGE_ID']
            assert not container['State']['Running'] and container['State']['ExitCode'] == 0
            assert container['HostConfig']['PortBindings'] == {'8000/tcp': [{'HostIp': '127.0.0.1', 'HostPort': '18002'}]}
            assert any(m['Source'] == str(STATE/'data') and m['Destination'] == '/data' for m in container['Mounts'])
            assert (STATE/'data').stat().st_ino == environment['sourceDirectoryInode']
            setup['command']('start', setup['NAME'])
            try:
                setup['ready']()
                def report(label, test="ChromaCqlNativeTests", extra=None):
                    started = time.time()
                    with (RESULTS/(label+'.log')).open('w') as log:
                        subprocess.run([DOTNET, VSTEST, str(DLL), '--TestCaseFilter:FullyQualifiedName~'+test,
                            '--ResultsDirectory:'+str(RESULTS), '--Logger:trx;LogFileName='+label+'.trx'], cwd=RESULTS,
                            env={**os.environ, 'DOTNET_ROOT': '/var/tmp/oasis-runtimes/dotnet-10.0.12', 'OASIS_CHROMA_CQL_STATE': str(STATE), **(extra or {})},
                            preexec_fn=demote, stdout=log, stderr=subprocess.STDOUT, check=True)
                    trx = RESULTS/(label+'.trx')
                    assert trx.stat().st_mtime >= started
                    counts = ET.parse(trx).find('.//{*}Counters').attrib
                    assert counts['passed'] == '1' and counts['failed'] == '0' and counts['notExecuted'] == '0'
                    print(json.dumps({'phase': label, 'counts': counts, 'sha256': hashlib.sha256(trx.read_bytes()).hexdigest()}), flush=True)
                report(phase)
                if phase == 'native-restart' and options.federated:
                    owner = (Path('/var/tmp/oasis-cosmic-mongo/data')).stat().st_uid
                    assert owner != 0
                    os.setresuid(owner, owner, 0)
                    try:
                        with owned_linux_mongo(RESULTS) as mongo_env:
                            os.setresuid(0, 0, 0)
                            try:
                                report('mongo-init', 'LinuxMongoDemoEnvironmentTests', mongo_env)
                                report('mongo-federation', 'ChromaMongoCqlNativeTests',
                                    {**mongo_env, 'COSMIC_CHROMA_MONGO_SERVE': '1' if options.serve else '0'})
                                assert not (STATE/'mongo.pending.json').exists()
                            finally:
                                os.setresuid(owner, owner, 0)
                    finally:
                        os.setresuid(0, 0, 0)
                    report('native-postpair')
            finally:
                setup['stop']()


if __name__ == '__main__':
    main()
