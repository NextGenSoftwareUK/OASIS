"""One free Weaviate source: authenticated native HTTPS/gRPC TLS, GraphQL disabled."""
from pathlib import Path
import json, os, pwd, secrets, socket, ssl, subprocess, time
import urllib.error, urllib.request

STATE = Path('/var/tmp/oasis-weaviate-cql')
IMAGE = 'docker.io/semitechnologies/weaviate@sha256:7f435fea6cb3d1b7b053079dbd7e2e69535e3962f74c6f46d3f930478c3de42d'
IMAGE_ID = '40d942d2666fcfe35925f4508fa00d84e3a2d1916f33e4c312a5bd971b4ed988'
NAME = 'oasis-weaviate-cql'
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
        headers['Authorization'] = 'Bearer '+json.loads((STATE/'credentials.json').read_text())['apiKey']
    data = None
    if body is not None:
        headers['Content-Type'] = 'application/json'
        data = json.dumps(body).encode()
    with urllib.request.urlopen(urllib.request.Request('https://localhost:18080'+path, headers=headers, data=data),
            context=ssl.create_default_context(cafile=str(STATE/'ca.pem')), timeout=10) as response:
        raw = response.read()
        return json.loads(raw) if as_json else raw.decode()


def ready():
    deadline = time.monotonic()+180
    while time.monotonic() < deadline:
        assert json.loads(command('inspect', NAME))[0]['State']['Running'], 'Native service stopped before readiness'
        try:
            request('/v1/.well-known/ready', as_json=False)
            meta = request('/v1/meta')
            assert meta['version'] == '1.39.8' and meta['grpcMaxMessageSize'] == 4194304
            return {'version': meta['version'], 'grpcMaxMessageSize': meta['grpcMaxMessageSize']}
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
    for port in [18080, 15051]:
        with socket.socket() as probe:
            assert probe.connect_ex(('127.0.0.1', port)) != 0
    assert json.loads(command('image', 'inspect', IMAGE))[0]['Id'] == IMAGE_ID
    STATE.mkdir(mode=0o700)
    os.chown(STATE, OWNER, OWNER)
    durable('environment.pending.json', {'image': IMAGE, 'name': NAME, 'ports': [18080, 15051]})
    for name in ['data', 'config', 'config/tls']:
        (STATE/name).mkdir(mode=0o700)
        os.chown(STATE/name, 1000, 1000)
    durable('credentials.json', {'apiKey': secrets.token_hex(32)})
    old_umask = os.umask(0o077)
    def openssl(*args):
        subprocess.run(['openssl', *args], check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    try:
        openssl('req', '-x509', '-newkey', 'rsa:3072', '-nodes', '-days', '365',
            '-subj', '/CN=COSMIC Weaviate private CA', '-addext', 'basicConstraints=critical,CA:TRUE',
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
    env = {'AUTHENTICATION_ANONYMOUS_ACCESS_ENABLED': 'false', 'AUTHENTICATION_APIKEY_ENABLED': 'true',
        'AUTHENTICATION_APIKEY_ALLOWED_KEYS': json.loads((STATE/'credentials.json').read_text())['apiKey'],
        'AUTHENTICATION_APIKEY_USERS': 'oasis_test', 'AUTHORIZATION_RBAC_ENABLED': 'true',
        'AUTHORIZATION_RBAC_ROOT_USERS': 'oasis_test', 'DISABLE_GRAPHQL': 'true', 'DISABLE_TELEMETRY': 'true',
        'API_BASED_MODULES_DISABLED': 'true', 'DEFAULT_VECTORIZER_MODULE': 'none',
        'PERSISTENCE_DATA_PATH': '/var/lib/weaviate', 'CLUSTER_HOSTNAME': 'cosmic-weaviate',
        'RAFT_JOIN': 'cosmic-weaviate', 'RAFT_BOOTSTRAP_EXPECT': '1', 'GOMAXPROCS': '2', 'GOMEMLIMIT': '1GiB',
        'GRPC_CERT_FILE': '/etc/cosmic/tls/node.crt', 'GRPC_KEY_FILE': '/etc/cosmic/tls/node.key',
        'GRPC_MAX_MESSAGE_SIZE': '4194304', 'GRPC_PORT': '50051'}
    with (STATE/'service.env').open('x') as file:
        os.fchmod(file.fileno(), 0o600)
        file.write(''.join(k+'='+v+'\n' for k,v in env.items()))
    os.chown(STATE/'service.env', OWNER, OWNER)
    container = command('create', '--name', NAME, '--hostname', 'cosmic-weaviate', '--label', 'oasis.task=cosmic-cql',
        '--publish', '127.0.0.1:18080:8080', '--publish', '127.0.0.1:15051:50051',
        '--cpus', '2', '--memory', '2g', '--pids-limit', '512', '--log-opt', 'max-size=2097152',
        '--user', '1000:1000', '--env-file', str(STATE/'service.env'),
        '--volume', str(STATE/'data')+':/var/lib/weaviate',
        '--volume', str(STATE/'config')+':/etc/cosmic:ro', IMAGE,
        '--scheme=https', '--tls-port=8080', '--host=0.0.0.0',
        '--tls-certificate=/etc/cosmic/tls/node.crt', '--tls-key=/etc/cosmic/tls/node.key')
    durable('container.ack.json', {'id': container, 'imageId': IMAGE_ID})
    observations = []
    for phase in ['initial', 'restart']:
        command('start', NAME)
        try:
            identity = ready()
            assert request('/v1/schema').get('classes') in [None, []]
            nodes = request('/v1/nodes')['nodes']
            assert len(nodes) == 1 and nodes[0]['name'] == 'cosmic-weaviate' and nodes[0]['status'] == 'HEALTHY'
            try:
                request('/v1/schema', authenticated=False)
            except urllib.error.HTTPError as failure:
                assert failure.code in [401, 403]
            else:
                raise RuntimeError('Unauthenticated native schema access succeeded')
            try:
                request('/v1/graphql', {'query': '{ Get { OasisHolon { dataJson } } }'})
            except urllib.error.HTTPError as failure:
                assert failure.code == 422 and 'graphql api is disabled' in failure.read().decode().lower()
            else:
                raise RuntimeError('GraphQL must remain disabled for gRPC acceptance')
            if observations:
                assert identity == observations[0]
            durable(phase+'.ack.json', identity)
            observations.append(identity)
        finally:
            stop()
    durable('environment.json', {'container': container, 'image': IMAGE, 'identity': observations[0],
        'node': 'cosmic-weaviate', 'sourceDirectoryInode': (STATE/'data').stat().st_ino})
    (STATE/'environment.pending.json').rename(STATE/'environment.intent.ack.json')
    print(json.dumps({'passed': True, 'identity': observations[0], 'stopped': True,
        'nativeFiles': sorted(str(p.relative_to(STATE/'data')) for p in (STATE/'data').rglob('*') if p.is_file()),
        'stateBytes': sum(p.stat().st_size for p in STATE.rglob('*') if p.is_file())}))


if __name__ == '__main__':
    main()
