"""One retained free Typesense30.2 source with native TLS and API-key authentication."""
from pathlib import Path
import json, os, pwd, secrets, socket, ssl, subprocess, time
import urllib.error, urllib.request

STATE = Path('/var/tmp/oasis-typesense-cql')
IMAGE = 'docker.io/typesense/typesense@sha256:b405d4443bdb9254fab4792b7fd9ee95d969f3b5485975917a3a3f84274eba27'
IMAGE_ID = 'f0a039399e56fece46120da49b83253d56c79bd6842add7879ef82ca38d9f9bf'
NAME = 'oasis-typesense-cql'
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


def request(path, body=None, authenticated=True, as_json=True):
    headers = {}
    if authenticated:
        headers['X-TYPESENSE-API-KEY'] = json.loads((STATE/'credentials.json').read_text())['apiKey']
    data = None
    if body is not None:
        headers['Content-Type'] = 'application/json'
        data = json.dumps(body).encode()
    with urllib.request.urlopen(urllib.request.Request('https://localhost:18108'+path, headers=headers, data=data),
            context=ssl.create_default_context(cafile=str(STATE/'ca.pem')), timeout=10) as response:
        raw = response.read()
        return json.loads(raw) if as_json else raw.decode()


def ready():
    deadline = time.monotonic()+180
    while time.monotonic() < deadline:
        assert json.loads(command('inspect', NAME))[0]['State']['Running'], 'Native service stopped before readiness'
        try:
            assert request('/health')['ok']
            meta = request('/debug')
            assert meta['version'] == '30.2' and meta['state'] == 1
            return meta
        except urllib.error.HTTPError as failure:
            if failure.code != 503:
                raise
        except (urllib.error.URLError, TimeoutError, ConnectionError):
            pass
        time.sleep(.5)
    raise RuntimeError('Native authenticated HTTPS/schema readiness deadline')


def stop():
    command('stop', '--time', '60', NAME)
    current = json.loads(command('inspect', NAME))[0]['State']
    assert not current['Running'] and not current['OOMKilled'] and not current['Error']
    assert current['ExitCode'] == 0, 'Preserve unclean native stop'


def main():
    assert os.geteuid() == 0 and not STATE.exists(), 'Preserve original source/unknown outcome'
    assert subprocess.run(['podman', 'container', 'exists', NAME]).returncode == 1
    for port in [18108]:
        with socket.socket() as probe:
            assert probe.connect_ex(('127.0.0.1', port)) != 0
    assert json.loads(command('image', 'inspect', IMAGE))[0]['Id'] == IMAGE_ID
    STATE.mkdir(mode=0o700)
    os.chown(STATE, OWNER, OWNER)
    durable('environment.pending.json', {'image': IMAGE, 'name': NAME, 'ports': [18108]})
    for name in ['data', 'config', 'config/tls']:
        (STATE/name).mkdir(mode=0o700)
        os.chown(STATE/name, 1000, 1000)
    durable('credentials.json', {'apiKey': secrets.token_hex(32)})
    old_umask = os.umask(0o077)
    def openssl(*args):
        subprocess.run(['openssl', *args], check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    try:
        openssl('req', '-x509', '-newkey', 'rsa:3072', '-nodes', '-days', '365',
            '-subj', '/CN=COSMIC Typesense private CA', '-addext', 'basicConstraints=critical,CA:TRUE',
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
    env = {'TYPESENSE_API_KEY': json.loads((STATE/'credentials.json').read_text())['apiKey'],
        'TYPESENSE_DATA_DIR': '/typesense-data', 'TYPESENSE_API_ADDRESS': '0.0.0.0', 'TYPESENSE_API_PORT': '8108',
        'TYPESENSE_SSL_CERTIFICATE': '/etc/cosmic/tls/node.crt', 'TYPESENSE_SSL_CERTIFICATE_KEY': '/etc/cosmic/tls/node.key',
        'TYPESENSE_PEERING_ADDRESS': '127.0.0.1', 'TYPESENSE_ENABLE_SEARCH_ANALYTICS': 'false',
        'TYPESENSE_THREAD_POOL_SIZE': '8', 'TYPESENSE_NUM_COLLECTIONS_PARALLEL_LOAD': '2',
        'TYPESENSE_NUM_DOCUMENTS_PARALLEL_LOAD': '2', 'TYPESENSE_MAX_INDEXING_CONCURRENCY': '2',
        'TYPESENSE_LOG_SLOW_SEARCHES_TIME_MS': '-1'}
    with (STATE/'service.env').open('x') as file:
        os.fchmod(file.fileno(), 0o600)
        file.write(''.join(k+'='+v+'\n' for k,v in env.items()))
    os.chown(STATE/'service.env', OWNER, OWNER)
    container = command('create', '--name', NAME, '--hostname', 'cosmic-typesense', '--label', 'oasis.task=cosmic-cql',
        '--publish', '127.0.0.1:18108:8108', '--cpus', '2', '--memory', '2g', '--pids-limit', '512',
        '--log-opt', 'max-size=2097152', '--user', '1000:1000', '--env-file', str(STATE/'service.env'),
        '--volume', str(STATE/'data')+':/typesense-data', '--volume', str(STATE/'config')+':/etc/cosmic:ro', IMAGE)
    durable('container.ack.json', {'id': container, 'imageId': IMAGE_ID})
    observations = []
    for phase in ['initial', 'restart']:
        command('start', NAME)
        try:
            identity = ready()
            assert request('/collections') == []
            try:
                request('/collections', authenticated=False)
            except urllib.error.HTTPError as failure:
                assert failure.code in [401, 403]
            else:
                raise RuntimeError('Unauthenticated collection access succeeded')
            if observations:
                assert identity == observations[0]
            durable(phase+'.ack.json', identity)
            observations.append(identity)
        finally:
            stop()
    durable('environment.json', {'container': container, 'image': IMAGE, 'identity': observations[0],
        'sourceDirectoryInode': (STATE/'data').stat().st_ino})
    (STATE/'environment.pending.json').rename(STATE/'environment.intent.ack.json')
    print(json.dumps({'passed': True, 'identity': observations[0], 'stopped': True,
        'nativeFiles': sorted(str(p.relative_to(STATE/'data')) for p in (STATE/'data').rglob('*') if p.is_file()),
        'stateBytes': sum(p.stat().st_size for p in STATE.rglob('*') if p.is_file())}))


if __name__ == '__main__':
    main()
