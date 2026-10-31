"""One retained free InfluxDB2 source with HTTPS/token auth and real restart."""
from pathlib import Path
import argparse, hashlib, json, os, pwd, secrets, socket, ssl, subprocess, time
import urllib.error, urllib.parse, urllib.request

STATE = Path('/var/tmp/oasis-influxdb-cql')
IMAGE = 'docker.io/library/influxdb@sha256:7ee90806d2b078a1d70a8ce3f454698997d992427f369ce7f2df11689de80644'
IMAGE_ID = '9ca9c0cb5b054022f64ce03607d4226c90bf9b5d38a09881283d0497ba9801a8'
NAME = 'oasis-influxdb-cql'
OWNER = pwd.getpwnam('dellams').pw_uid


def command(*args):
    return subprocess.check_output(['podman', *args], text=True).strip()


def durable(name, value):
    with (STATE/name).open('x') as file:
        os.fchmod(file.fileno(), 0o600)
        json.dump(value, file)
        file.flush()
        os.fsync(file.fileno())
    os.chown(STATE/name, OWNER, OWNER)


def request(path, body=None, authenticated=True):
    headers = {}
    if authenticated:
        credentials = json.loads((STATE/'credentials.json').read_text())
        headers['Authorization'] = 'Token '+credentials['token']
    data = None
    if body is not None:
        data = json.dumps(body).encode()
        headers['Content-Type'] = 'application/json'
    with urllib.request.urlopen(urllib.request.Request('https://localhost:18086'+path,
            data=data, headers=headers), context=ssl.create_default_context(cafile=str(STATE/'ca.pem')),
            timeout=15) as response:
        raw = response.read()
        return json.loads(raw) if raw else None


def ready():
    deadline = time.monotonic()+180
    while time.monotonic() < deadline:
        assert json.loads(command('inspect', NAME))[0]['State']['Running'], 'Server stopped before readiness'
        try:
            health = request('/health', authenticated=False)
            assert health['status'] == 'pass' and health['version'].lstrip('v').startswith('2.7.')
            return health
        except urllib.error.HTTPError:
            raise
        except (urllib.error.URLError, TimeoutError, ConnectionError):
            time.sleep(.5)
    raise RuntimeError('Native HTTPS readiness deadline')


def observe():
    health = ready()
    orgs = request('/api/v2/orgs?org=oasis-org')['orgs']
    assert len(orgs) == 1 and orgs[0]['name'] == 'oasis-org'
    buckets = request('/api/v2/buckets?name=oasis-bucket')['buckets']
    assert len(buckets) == 1 and buckets[0]['orgID'] == orgs[0]['id']
    assert all(rule['everySeconds'] == 0 for rule in buckets[0]['retentionRules']), 'Infinite retention required'
    assert request('/api/v2/setup', authenticated=False)['allowed'] is False
    try:
        request('/api/v2/buckets', authenticated=False)
    except urllib.error.HTTPError as failure:
        assert failure.code == 401
    else:
        raise RuntimeError('Unauthenticated bucket access succeeded')
    return {'version': health['version'], 'orgId': orgs[0]['id'], 'bucketId': buckets[0]['id'], 'retentionSeconds': 0}


def stop():
    # The pinned server registers os.Interrupt (SIGINT), not SIGTERM:
    # influxdb/v2.7.12/kit/signals/context.go. Use its actual graceful contract.
    # This Podman version exposes explicit signals through `kill`, not `stop`.
    command('kill', '--signal', 'SIGINT', NAME)
    deadline = time.monotonic()+60
    while True:
        current = json.loads(command('inspect', NAME))[0]['State']
        if not current['Running']:
            break
        assert time.monotonic() < deadline, 'Native graceful SIGINT shutdown deadline; no forced escalation'
        time.sleep(.25)
    assert not current['Running'] and not current['OOMKilled'] and not current['Error']
    assert current['ExitCode'] == 0, 'Preserve unclean owned service stop'


def verify_original():
    assert os.geteuid() == 0 and (STATE/'environment.pending.json').exists()
    assert (STATE/'initial.ack.json').exists() and not (STATE/'environment.json').exists()
    assert not list(STATE.glob('demo-seed*')), 'No business writes before environment admission'
    original = json.loads((STATE/'initial.ack.json').read_text())
    receipt = json.loads((STATE/'setup.ack.json').read_text())
    assert original['orgId'] == receipt['orgId'] and original['bucketId'] == receipt['bucketId']
    container = json.loads((STATE/'container.ack.json').read_text())
    current = json.loads(command('inspect', NAME))[0]
    assert current['Id'] == container['id'] and current['Image'] == container['imageId'] == IMAGE_ID
    assert not current['State']['OOMKilled']
    if current['State']['Running']:
        assert (STATE/'restart.ack.json').exists(), 'Only retained verified restart may finish its interrupted shutdown'
    else:
        if not (STATE/'initial-stop.ack.json').exists():
            durable('initial-stop.ack.json', {'exitCode': current['State']['ExitCode'],
                'reason': 'Pinned native signal handler uses SIGINT; original SIGTERM stop was abrupt'})
        command('start', NAME)
    try:
        assert observe() == original, 'Preserve original native org/bucket identities'
        if (STATE/'restart.ack.json').exists():
            assert json.loads((STATE/'restart.ack.json').read_text()) == original
        else:
            durable('restart.ack.json', original)
    finally:
        stop()
    durable('environment.json', {'container': container['id'], 'image': IMAGE, 'identity': original})
    for stem in ['environment', 'setup']:
        (STATE/(stem+'.pending.json')).rename(STATE/(stem+'.intent.ack.json'))
    print(json.dumps({'passed': True, 'identity': original, 'stopped': True,
        'stateBytes': sum(p.stat().st_size for p in STATE.rglob('*') if p.is_file())}))


def main():
    assert os.geteuid() == 0 and not STATE.exists(), 'Preserve original source/unknown outcome'
    assert subprocess.run(['podman', 'container', 'exists', NAME]).returncode == 1
    with socket.socket() as probe:
        assert probe.connect_ex(('127.0.0.1', 18086)) != 0
    assert json.loads(command('image', 'inspect', IMAGE))[0]['Id'] == IMAGE_ID
    STATE.mkdir(mode=0o700)
    os.chown(STATE, OWNER, OWNER)
    durable('environment.pending.json', {'image': IMAGE, 'name': NAME, 'port': 18086})
    for name in ['data', 'config', 'config/tls']:
        (STATE/name).mkdir(mode=0o700)
        os.chown(STATE/name, 1000, 1000)
    credentials = {'username': 'oasis_test', 'password': secrets.token_hex(32),
        'token': secrets.token_urlsafe(48), 'org': 'oasis-org', 'bucket': 'oasis-bucket'}
    durable('credentials.json', credentials)
    old_umask = os.umask(0o077)
    def openssl(*args):
        subprocess.run(['openssl', *args], check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    try:
        openssl('req', '-x509', '-newkey', 'rsa:3072', '-nodes', '-days', '365',
            '-subj', '/CN=COSMIC InfluxDB private CA', '-addext', 'basicConstraints=critical,CA:TRUE',
            '-addext', 'keyUsage=critical,keyCertSign,cRLSign', '-keyout', str(STATE/'ca.key'), '-out', str(STATE/'ca.pem'))
        openssl('req', '-new', '-newkey', 'rsa:3072', '-nodes', '-subj', '/CN=localhost',
            '-keyout', str(STATE/'config/tls/node.key'), '-out', str(STATE/'node.csr'))
        (STATE/'extensions.cnf').write_text('subjectAltName=DNS:localhost,IP:127.0.0.1\n'
            'keyUsage=critical,digitalSignature,keyEncipherment\nextendedKeyUsage=serverAuth\nbasicConstraints=critical,CA:FALSE\n')
        openssl('x509', '-req', '-in', str(STATE/'node.csr'), '-CA', str(STATE/'ca.pem'),
            '-CAkey', str(STATE/'ca.key'), '-CAcreateserial', '-days', '365', '-sha256',
            '-extfile', str(STATE/'extensions.cnf'), '-out', str(STATE/'config/tls/node.crt'))
    finally:
        os.umask(old_umask)
    for name in ['ca.key', 'ca.pem', 'node.csr', 'ca.srl', 'extensions.cnf']:
        os.chmod(STATE/name, 0o600)
        os.chown(STATE/name, OWNER, OWNER)
    for path in (STATE/'config/tls').iterdir():
        os.chown(path, 1000, 1000)
    container = command('create', '--name', NAME, '--label', 'oasis.task=cosmic-cql',
        '--publish', '127.0.0.1:18086:8086', '--cpus', '2', '--memory', '2g',
        '--pids-limit', '512', '--log-opt', 'max-size=2097152', '--user', '1000:1000', '--stop-signal', 'SIGINT',
        '--env', 'GOMAXPROCS=2', '--entrypoint', '/usr/local/bin/influxd',
        '--volume', str(STATE/'data')+':/var/lib/influxdb2',
        '--volume', str(STATE/'config')+':/etc/influxdb2:ro', IMAGE,
        '--bolt-path=/var/lib/influxdb2/influxd.bolt', '--engine-path=/var/lib/influxdb2/engine',
        '--tls-cert=/etc/influxdb2/tls/node.crt', '--tls-key=/etc/influxdb2/tls/node.key',
        '--tls-min-version=1.2', '--reporting-disabled')
    durable('container.ack.json', {'id': container, 'imageId': IMAGE_ID})
    observations = []
    for phase in ['initial', 'restart']:
        command('start', NAME)
        try:
            ready()
            if phase == 'initial':
                assert request('/api/v2/setup', authenticated=False)['allowed'] is True
                durable('setup.pending.json', {'org': credentials['org'], 'bucket': credentials['bucket'], 'retentionSeconds': 0})
                response = request('/api/v2/setup', {**credentials, 'retentionPeriodSeconds': 0}, authenticated=False)
                assert response['auth']['token'] == credentials['token']
                durable('setup.ack.json', {'orgId': response['org']['id'], 'bucketId': response['bucket']['id'],
                    'userId': response['user']['id'], 'authId': response['auth']['id']})
            identity = observe()
            receipt = json.loads((STATE/'setup.ack.json').read_text())
            assert identity['orgId'] == receipt['orgId'] and identity['bucketId'] == receipt['bucketId']
            if observations:
                assert identity == observations[0]
            durable(phase+'.ack.json', identity)
            observations.append(identity)
        finally:
            stop()
    durable('environment.json', {'container': container, 'image': IMAGE, 'identity': observations[0]})
    for stem in ['environment', 'setup']:
        (STATE/(stem+'.pending.json')).rename(STATE/(stem+'.intent.ack.json'))
    print(json.dumps({'passed': True, 'identity': observations[0], 'stopped': True,
        'stateBytes': sum(p.stat().st_size for p in STATE.rglob('*') if p.is_file())}))


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--verify-original', action='store_true')
    if parser.parse_args().verify_original:
        verify_original()
    else:
        main()
