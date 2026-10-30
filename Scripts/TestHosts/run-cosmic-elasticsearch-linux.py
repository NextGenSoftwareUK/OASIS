"""Bounded original Cassandra source acceptance, with an actual native restart."""
from pathlib import Path
import base64, ssl, urllib.request, urllib.error, argparse, sys, fcntl, hashlib, json, os, pwd, socket, subprocess, time
import xml.etree.ElementTree as ET

sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parents[2]
STATE = Path('/var/tmp/oasis-elasticsearch-cql')
RESULTS = ROOT/'TestResults/ElasticsearchCqlCurrent'
NAME = 'oasis-elasticsearch-cql'
OWNER = pwd.getpwnam('dellams').pw_uid
DOTNET = '/var/tmp/oasis-runtimes/dotnet-10.0.12/dotnet'
VSTEST = '/mnt/c/Users/david/.nuget/packages/microsoft.testplatform.portable/18.11.0/tools/net8.0/vstest.console.dll'
DLL = ROOT/'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.ElasticsearchOASIS.IntegrationTests/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Providers.ElasticsearchOASIS.IntegrationTests.dll'


def command(*args):
    return subprocess.check_output(['podman', *args], text=True).strip()


def inspect():
    record = json.loads(command('inspect', NAME))[0]
    original = json.loads((STATE/'container.ack.json').read_text())
    assert record['Id'] == original['id'] and record['Image'] == original['imageId']
    assert record['HostConfig']['PortBindings'] == {'9200/tcp': [{'HostIp': '127.0.0.1', 'HostPort': '19200'}]}
    assert any(m['Source'] == str(STATE/'data') and m['Destination'] == '/usr/share/elasticsearch/data' for m in record['Mounts'])
    return record


def demote():
    os.setgroups([])
    os.setgid(OWNER)
    os.setuid(OWNER)


def main():
    assert os.geteuid() == 0
    original = json.loads((STATE/'environment.json').read_text())['observations'][0]
    credentials = json.loads((STATE/'credentials.json').read_text())
    context = ssl.create_default_context(cafile=str(STATE/'ca.pem'))
    authorization = 'Basic '+base64.b64encode((credentials['username']+':'+credentials['password']).encode()).decode()
    def get(path):
        with urllib.request.urlopen(urllib.request.Request('https://localhost:19200'+path,
                headers={'Authorization': authorization}), context=context, timeout=5) as response:
            return json.load(response)
    assert not list(STATE.glob('*.pending.json')), 'Reconcile unknown outcome without replay'
    proofs = []
    with (STATE/'runner.lock').open('a') as lock:
        fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
        for phase in ['native-first', 'native-restart']:
            assert inspect()['State']['Status'] == 'exited'
            with socket.socket() as probe:
                assert probe.connect_ex(('127.0.0.1', 19200)) != 0
            command('start', NAME)
            try:
                deadline = time.monotonic()+180
                while time.monotonic() < deadline:
                    assert inspect()['State']['Running']
                    try:
                        identity = get('/')
                        license = get('/_license')['license']
                        health = get('/_cluster/health?wait_for_status=yellow&timeout=2s')
                        nodes = get('/_nodes/_local')['nodes']
                        assert identity['version']['number'] == original['version'] == '9.5.5'
                        assert identity['cluster_uuid'] == original['clusterUuid'] and list(nodes) == [original['nodeId']]
                        assert license['type'] == 'basic' and license['status'] == 'active'
                        if not health['timed_out'] and health['status'] in ['yellow', 'green']:
                            break
                    except (urllib.error.URLError, TimeoutError, ConnectionError):
                        time.sleep(.5)
                else:
                    raise RuntimeError('Original native identity/readiness deadline')
                def report(phase, test='ElasticsearchCqlNativeTests', extra=None):
                    env = {**os.environ, 'DOTNET_ROOT': '/var/tmp/oasis-runtimes/dotnet-10.0.12',
                        'OASIS_ELASTICSEARCH_CQL_STATE': str(STATE), 'SSL_CERT_FILE': str(STATE/'ca.pem'), **(extra or {})}
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
                report(phase)

            finally:
                command('stop', '--time', '60', NAME)
                status = inspect()['State']
                assert not status['Running'] and status['ExitCode'] == 143
                assert not status['OOMKilled'] and not status['Error'] and status['StoppedByUser']
    assert not list(STATE.glob('*.pending.json'))
    print(json.dumps({'passed': True, 'proofs': proofs, 'stopped': True, 'exitCode': 143,
        'stateBytes': sum(p.stat().st_size for p in STATE.rglob('*') if p.is_file())}))


if __name__ == '__main__':
    main()
