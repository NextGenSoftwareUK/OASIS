"""Native CQL acceptance using the same original Community source across restart."""
from pathlib import Path
import fcntl, hashlib, json, os, pwd, runpy, socket, subprocess, time
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
STATE = Path('/var/tmp/oasis-couchbase-cql')
RESULTS = ROOT/'TestResults/CouchbaseCqlCurrent'
NAME = 'oasis-couchbase-cql'
OWNER = pwd.getpwnam('dellams').pw_uid
DOTNET = '/var/tmp/oasis-runtimes/dotnet-10.0.12/dotnet'
VSTEST = '/mnt/c/Users/david/.nuget/packages/microsoft.testplatform.portable/18.11.0/tools/net8.0/vstest.console.dll'
DLL = ROOT/'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.CouchbaseOASIS.IntegrationTests/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Providers.CouchbaseOASIS.IntegrationTests.dll'
setup = runpy.run_path(str(ROOT/'Scripts/TestHosts/Create-CosmicCouchbaseEnvironment.py'))
command = setup['command']
request = setup['request']


def inspect():
    current = json.loads(command('inspect', NAME))[0]
    receipt = json.loads((STATE/'container.ack.json').read_text())
    assert current['Id'] == receipt['id'] and current['Image'] == receipt['imageId']
    assert current['HostConfig']['PortBindings'] == {
        f'{p}/tcp': [{'HostIp': '127.0.0.1', 'HostPort': str(p)}] for p in [8091, 8093, 11210]}
    assert any(m['Source'] == str(STATE/'data') and m['Destination'] == '/opt/couchbase/var' for m in current['Mounts'])
    return current


def demote():
    os.setgroups([])
    os.setgid(OWNER)
    os.setuid(OWNER)


def main():
    assert os.geteuid() == 0
    original = json.loads((STATE/'environment.json').read_text())['identity']
    credentials = json.loads((STATE/'credentials.json').read_text())
    assert not list(STATE.glob('*.pending.json')), 'Reconcile unknown outcome without replay'
    proofs = []
    with (STATE/'runner.lock').open('a') as lock:
        fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
        for phase in ['native-first', 'native-restart']:
            assert inspect()['State']['Status'] == 'exited'
            for port in [8091, 8093, 11210]:
                with socket.socket() as probe:
                    assert probe.connect_ex(('127.0.0.1', port)) != 0
            command('start', NAME)
            try:
                pools = setup['ready'](credentials)
                assert pools['uuid'] == original['clusterUuid'] and pools['isEnterprise'] is False
                assert pools['implementationVersion'] == original['version']
                node = request('/nodes/self', credentials)
                assert node['otpNode'] == original['node']
                assert request('/pools/default/buckets/oasis', credentials)['uuid'] == original['bucketUuid']
                deadline = time.monotonic()+180
                while True:
                    bucket = request('/pools/default/buckets/oasis', credentials)
                    mapping = bucket.get('vBucketServerMap', {})
                    if bucket['nodes'] and all(n['status'] == 'healthy' for n in bucket['nodes']) and mapping.get('vBucketMap') and all(v[0] >= 0 for v in mapping['vBucketMap']):
                        break
                    assert time.monotonic() < deadline, 'Original bucket warmup deadline'
                    time.sleep(.5)
                env = {**os.environ, 'DOTNET_ROOT': '/var/tmp/oasis-runtimes/dotnet-10.0.12',
                    'OASIS_COUCHBASE_CQL_STATE': str(STATE)}
                started = time.time()
                with (RESULTS/(phase+'.log')).open('w') as log:
                    subprocess.run([DOTNET, VSTEST, str(DLL), '--TestCaseFilter:FullyQualifiedName~CouchbaseCqlNativeTests',
                        '--ResultsDirectory:'+str(RESULTS), '--Logger:trx;LogFileName='+phase+'.trx'],
                        cwd=RESULTS, env=env, preexec_fn=demote, stdout=log, stderr=subprocess.STDOUT, check=True)
                trx = RESULTS/(phase+'.trx')
                assert trx.stat().st_mtime >= started
                counts = ET.parse(trx).find('.//{*}Counters').attrib
                assert counts['passed'] == counts['total'] == '1' and counts['notExecuted'] == '0'
                proofs.append({'phase': phase, 'passed': 1, 'sha256': hashlib.sha256(trx.read_bytes()).hexdigest()})
            finally:
                setup['stop']()
                assert not inspect()['State']['Running']
    assert not list(STATE.glob('*.pending.json'))
    print(json.dumps({'passed': True, 'proofs': proofs, 'stopped': True,
        'stateBytes': sum(p.stat().st_size for p in STATE.rglob('*') if p.is_file())}))


if __name__ == '__main__':
    main()
