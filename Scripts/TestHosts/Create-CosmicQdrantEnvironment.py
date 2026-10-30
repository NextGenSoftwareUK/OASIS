"""One original free standalone Qdrant source with HTTPS/API-key and restart."""
from pathlib import Path
import json, os, pwd, secrets, socket, ssl, subprocess, time
import urllib.error, urllib.request

STATE = Path('/var/tmp/oasis-qdrant-cql')
IMAGE = 'docker.io/qdrant/qdrant@sha256:26509b92c44ded1ad344e18a005383c20bb3fbf9dbf4d337265a230b2aa89e79'
IMAGE_ID = 'd2ea76deb2325eac1ef9548a2ee57b966121d93a810ced2d7c62387b2beb57bb'
NAME = 'oasis-qdrant-cql'
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


def request(path, authenticated=True):
    headers = {}
    if authenticated:
        headers['api-key'] = json.loads((STATE/'credentials.json').read_text())['apiKey']
    with urllib.request.urlopen(urllib.request.Request('https://localhost:16333'+path, headers=headers),
            context=ssl.create_default_context(cafile=str(STATE/'ca.pem')), timeout=10) as response:
        return json.load(response)


def ready():
    deadline = time.monotonic()+180
    while time.monotonic() < deadline:
        assert json.loads(command('inspect', NAME))[0]['State']['Running'], 'Native service stopped before readiness'
        try:
            identity = request('/')
            assert identity['version'] == '1.16.3'
            assert request('/collections')['status'] == 'ok'
            return {'version': identity['version'], 'commit': identity['commit']}
        except urllib.error.HTTPError:
            raise
        except (urllib.error.URLError, TimeoutError, ConnectionError):
            time.sleep(.5)
    raise RuntimeError('Authenticated HTTPS readiness deadline')


def stop():
    command('stop', '--time', '60', NAME)
    current = json.loads(command('inspect', NAME))[0]['State']
    assert not current['Running'] and not current['OOMKilled'] and not current['Error']
    assert current['ExitCode'] == 0, 'Preserve unclean native stop'


def main():
    assert os.geteuid() == 0 and not STATE.exists(), 'Preserve original source/unknown outcome'
    assert subprocess.run(['podman', 'container', 'exists', NAME]).returncode == 1
    with socket.socket() as probe:
        assert probe.connect_ex(('127.0.0.1', 16333)) != 0
    assert json.loads(command('image', 'inspect', IMAGE))[0]['Id'] == IMAGE_ID
    STATE.mkdir(mode=0o700)
    os.chown(STATE, OWNER, OWNER)
    durable('environment.pending.json', {'image': IMAGE, 'name': NAME, 'port': 16333})
    for name in ['data', 'snapshots', 'config', 'config/tls']:
        (STATE/name).mkdir(mode=0o700)
        os.chown(STATE/name, 1000, 1000)
    durable('credentials.json', {'apiKey': secrets.token_hex(32)})
    old_umask = os.umask(0o077)
    def openssl(*args):
        subprocess.run(['openssl', *args], check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    try:
        openssl('req', '-x509', '-newkey', 'rsa:3072', '-nodes', '-days', '365',
            '-subj', '/CN=COSMIC Qdrant private CA', '-addext', 'basicConstraints=critical,CA:TRUE',
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
    configuration = {'telemetry_disabled': True, 'cluster': {'enabled': False},
        'service': {'host': '0.0.0.0', 'http_port': 6333, 'grpc_port': None,
            'enable_tls': True, 'api_key': json.loads((STATE/'credentials.json').read_text())['apiKey'], 'max_workers': 2},
        'tls': {'cert': '/qdrant/config/tls/node.crt', 'key': '/qdrant/config/tls/node.key'},
        'storage': {'storage_path': '/qdrant/storage', 'snapshots_path': '/qdrant/snapshots',
            'performance': {'max_search_threads': 2, 'optimizer_cpu_budget': 1},
            'optimizers': {'max_optimization_threads': 1}}}
    # JSON flow syntax is also valid YAML; no extra YAML package/setup needed.
    with (STATE/'config/cosmic.yaml').open('x') as file:
        os.fchmod(file.fileno(), 0o600)
        json.dump(configuration, file)
    os.chown(STATE/'config/cosmic.yaml', 1000, 1000)
    container = command('create', '--name', NAME, '--label', 'oasis.task=cosmic-cql',
        '--publish', '127.0.0.1:16333:6333', '--cpus', '2', '--memory', '2g',
        '--pids-limit', '512', '--log-opt', 'max-size=2097152', '--user', '1000:1000',
        '--workdir', '/qdrant/storage', '--entrypoint', '/qdrant/qdrant',
        '--volume', str(STATE/'data')+':/qdrant/storage',
        '--volume', str(STATE/'snapshots')+':/qdrant/snapshots',
        '--volume', str(STATE/'config')+':/qdrant/config:ro', IMAGE,
        '--config-path', '/qdrant/config/cosmic.yaml')
    durable('container.ack.json', {'id': container, 'imageId': IMAGE_ID})
    observations = []
    for phase in ['initial', 'restart']:
        command('start', NAME)
        try:
            identity = ready()
            native_peer = json.loads((STATE/'data/raft_state.json').read_text())['this_peer_id']
            identity['peerId'] = str(native_peer)
            assert request('/collections')['result']['collections'] == []
            cluster = request('/cluster')['result']
            assert cluster['status'] == 'disabled'
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
    durable('environment.json', {'container': container, 'image': IMAGE,
        'identity': observations[0], 'standalone': True,
        'sourcePath': str(STATE/'data'), 'sourceDirectoryInode': (STATE/'data').stat().st_ino,
        'nativeFiles': sorted(str(p.relative_to(STATE/'data')) for p in (STATE/'data').rglob('*') if p.is_file())})
    durable('native-peer.ack.json', {'peerId': observations[0]['peerId'],
        'sourceDirectoryInode': (STATE/'data').stat().st_ino, 'beforeBusinessWrites': True})
    (STATE/'environment.pending.json').rename(STATE/'environment.intent.ack.json')
    print(json.dumps({'passed': True, 'identity': observations[0], 'stopped': True,
        'nativeFiles': sorted(str(p.relative_to(STATE/'data')) for p in (STATE/'data').rglob('*') if p.is_file()),
        'stateBytes': sum(p.stat().st_size for p in STATE.rglob('*') if p.is_file())}))


if __name__ == '__main__':
    main()
