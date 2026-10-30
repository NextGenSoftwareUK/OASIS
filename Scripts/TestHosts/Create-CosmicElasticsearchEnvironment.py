"""Create one original Basic-license Elasticsearch HTTPS source; never reset existing state."""
from pathlib import Path
import argparse, base64, hashlib, json, os, pwd, secrets, shutil, socket, ssl, subprocess, time, urllib.error, urllib.request

STATE = Path('/var/tmp/oasis-elasticsearch-cql')
IMAGE = 'docker.elastic.co/elasticsearch/elasticsearch@sha256:c40a1fb2bc6976821e5625d2329992a1e9c29df576e5955fdcba02421abf0d3c'
NAME = 'oasis-elasticsearch-cql'
OWNER = pwd.getpwnam('dellams').pw_uid


def command(*args):
    return subprocess.check_output(['podman', *args], text=True).strip()


def durable(path, value):
    with path.open('x') as file:
        os.fchmod(file.fileno(), 0o600)
        json.dump(value, file)
        file.flush()
        os.fsync(file.fileno())
    os.chown(path, OWNER, OWNER)


def private_text(path, value):
    with path.open('x') as file:
        os.fchmod(file.fileno(), 0o600)
        file.write(value)
        file.flush()
        os.fsync(file.fileno())
    os.chown(path, 1000, 1000)


def openssl(*args):
    subprocess.run(['openssl', *args], check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)


def main():
    assert os.geteuid() == 0 and not STATE.exists(), 'Preserve original source/unknown outcome; never recreate'
    assert subprocess.run(['podman', 'container', 'exists', NAME]).returncode == 1
    with socket.socket() as probe:
        assert probe.connect_ex(('127.0.0.1', 19200)) != 0
    image = json.loads(command('image', 'inspect', IMAGE))[0]
    assert image['Id'] == '952a43cd5ab721f7032b2dfd4dcc86a76ba9a791b4c34a223292cf26a832e040'
    STATE.mkdir(mode=0o700)
    os.chown(STATE, OWNER, OWNER)
    pending = STATE/'environment.pending.json'
    durable(pending, {'image': IMAGE, 'name': NAME, 'port': 19200, 'license': 'basic'})
    mount = Path(command('image', 'mount', IMAGE))
    try:
        shutil.copytree(mount/'usr/share/elasticsearch/config', STATE/'config')
    finally:
        command('image', 'unmount', IMAGE)
    for name in ['data', 'secrets', 'config/tls']:
        (STATE/name).mkdir(mode=0o700)
    password = secrets.token_hex(32)
    durable(STATE/'credentials.json', {'username': 'elastic', 'password': password})
    private_text(STATE/'secrets/password', password)
    old_umask = os.umask(0o077)
    try:
        openssl('req', '-x509', '-newkey', 'rsa:3072', '-nodes', '-days', '365',
            '-subj', '/CN=COSMIC Elasticsearch private CA', '-addext', 'basicConstraints=critical,CA:TRUE',
            '-addext', 'keyUsage=critical,keyCertSign,cRLSign', '-keyout', str(STATE/'ca.key'), '-out', str(STATE/'ca.pem'))
        openssl('req', '-new', '-newkey', 'rsa:3072', '-nodes', '-subj', '/CN=localhost',
            '-keyout', str(STATE/'config/tls/node.key'), '-out', str(STATE/'node.csr'))
        private_text(STATE/'extensions.cnf', 'subjectAltName=DNS:localhost,IP:127.0.0.1\n'
            'keyUsage=critical,digitalSignature,keyEncipherment\nextendedKeyUsage=serverAuth,clientAuth\nbasicConstraints=critical,CA:FALSE\n')
        openssl('x509', '-req', '-in', str(STATE/'node.csr'), '-CA', str(STATE/'ca.pem'),
            '-CAkey', str(STATE/'ca.key'), '-CAcreateserial', '-days', '365', '-sha256',
            '-extfile', str(STATE/'extensions.cnf'), '-out', str(STATE/'config/tls/node.crt'))
        private_text(STATE/'config/tls/ca.crt', (STATE/'ca.pem').read_text())
        for path in [STATE/'ca.key', STATE/'ca.pem', STATE/'node.csr', STATE/'ca.srl']:
            os.chmod(path, 0o600)
            os.chown(path, OWNER, OWNER)
    finally:
        os.umask(old_umask)
    settings = '''cluster.name: COSMIC-CQL-Elasticsearch
node.name: cosmic-cql-elasticsearch
network.host: 0.0.0.0
discovery.type: single-node
xpack.license.self_generated.type: basic
xpack.security.enabled: true
xpack.security.autoconfiguration.enabled: false
xpack.security.enrollment.enabled: false
xpack.security.http.ssl.enabled: true
xpack.security.http.ssl.key: tls/node.key
xpack.security.http.ssl.certificate: tls/node.crt
xpack.security.http.ssl.certificate_authorities: [tls/ca.crt]
xpack.security.transport.ssl.enabled: true
xpack.security.transport.ssl.key: tls/node.key
xpack.security.transport.ssl.certificate: tls/node.crt
xpack.security.transport.ssl.certificate_authorities: [tls/ca.crt]
xpack.security.transport.ssl.verification_mode: certificate
xpack.ml.enabled: false
ingest.geoip.downloader.enabled: false
'''
    (STATE/'config/elasticsearch.yml').write_text(settings)
    for name in ['config', 'data', 'secrets']:
        for root, dirs, files in os.walk(STATE/name):
            os.chown(root, 1000, 1000)
            for filename in files:
                os.chown(Path(root)/filename, 1000, 1000)
    container = command('create', '--name', NAME, '--label', 'oasis.task=cosmic-cql',
        '--publish', '127.0.0.1:19200:9200', '--cpus', '2', '--memory', '2g', '--pids-limit', '512',
        '--env', 'ES_JAVA_OPTS=-Xms512m -Xmx512m', '--env', 'ELASTIC_PASSWORD_FILE=/run/oasis-secrets/password',
        '--volume', str(STATE/'data')+':/usr/share/elasticsearch/data',
        '--volume', str(STATE/'config')+':/usr/share/elasticsearch/config',
        '--volume', str(STATE/'secrets')+':/run/oasis-secrets:ro', IMAGE)
    durable(STATE/'container.ack.json', {'id': container, 'imageId': image['Id']})
    verify_native(container, pending)


def verify_original():
    assert os.geteuid() == 0 and (STATE/'environment.pending.json').exists()
    assert not (STATE/'environment.json').exists() and not list(STATE.glob('demo-seed*'))
    receipt = json.loads((STATE/'container.ack.json').read_text())
    record = json.loads(command('inspect', NAME))[0]
    assert record['Id'] == receipt['id'] and record['Image'] == receipt['imageId']
    assert not record['State']['Running'] and not record['State']['OOMKilled']
    # Preserve and compare the original node/cluster identity recorded by its own
    # first startup, before any business index or SDK write was attempted.
    raw = command('logs', NAME)
    identities = set()
    for line in raw.splitlines():
        try:
            value = json.loads(line)
        except json.JSONDecodeError:
            continue
        cluster = value.get('elasticsearch.cluster.uuid')
        node = value.get('elasticsearch.node.id')
        if cluster and cluster != '_na_' and node:
            identities.add((cluster, node))
    assert len(identities) == 1, 'Original native identity ambiguous; never replace its baseline'
    cluster, node = next(iter(identities))
    durable(STATE/'original-startup-observation.ack.json', {'clusterUuid': cluster, 'nodeId': node,
        'caSha256': hashlib.sha256((STATE/'ca.pem').read_bytes()).hexdigest(),
        'source': 'original container structured startup log', 'initialStopExitCode': record['State']['ExitCode']})
    verify_native(NAME, STATE/'environment.pending.json', (cluster, node))


def verify_native(container, pending, expected=None):
    password = json.loads((STATE/'credentials.json').read_text())['password']
    context = ssl.create_default_context(cafile=str(STATE/'ca.pem'))
    authorization = 'Basic '+base64.b64encode(('elastic:'+password).encode()).decode()

    def get(path, authenticated=True):
        headers = {'Authorization': authorization} if authenticated else {}
        with urllib.request.urlopen(urllib.request.Request('https://localhost:19200'+path, headers=headers),
                context=context, timeout=5) as response:
            return json.load(response)

    observations = []
    for phase in ['initial', 'restart']:
        command('start', container)
        try:
            deadline = time.monotonic()+180
            while time.monotonic() < deadline:
                assert json.loads(command('inspect', container))[0]['State']['Running'], 'Owned service stopped before readiness'
                try:
                    identity = get('/')
                    license = get('/_license')['license']
                    health = get('/_cluster/health?wait_for_status=yellow&timeout=2s')
                    if identity.get('version', {}).get('number') == '9.5.5' and license['type'] == 'basic' and license['status'] == 'active' and health['status'] in ['yellow', 'green'] and not health['timed_out']:
                        break
                except (urllib.error.URLError, TimeoutError, ConnectionError):
                    time.sleep(.5)
            else:
                raise RuntimeError('Authenticated HTTPS readiness deadline')
            try:
                get('/', authenticated=False)
            except urllib.error.HTTPError as failure:
                assert failure.code == 401
            else:
                raise RuntimeError('Unauthenticated native access unexpectedly succeeded')
            license = get('/_license')['license']
            assert license['type'] == 'basic' and license['status'] == 'active'
            assert get('/_cat/indices/oasis-*?format=json') == [], 'No business indices before original source writes'
            nodes = get('/_nodes/_local')['nodes']
            assert len(nodes) == 1
            observation = {'phase': phase, 'clusterUuid': identity['cluster_uuid'], 'nodeId': next(iter(nodes)),
                'version': identity['version']['number'], 'license': license['type']}
            assert observation['clusterUuid'] != '_na_'
            if expected:
                assert (observation['clusterUuid'], observation['nodeId']) == expected
            if observations:
                assert {k: v for k, v in observation.items() if k != 'phase'} == {
                    k: v for k, v in observations[0].items() if k != 'phase'}
            observations.append(observation)
            durable(STATE/(phase+'.ack.json'), observation)
        finally:
            command('stop', '--time', '60', container)
            status = json.loads(command('inspect', container))[0]['State']
            assert not status['Running'] and status['ExitCode'] == 143 and not status['OOMKilled'] and not status['Error']
    durable(STATE/'environment.json', {'image': IMAGE, 'container': container, 'observations': observations})
    pending.rename(STATE/'environment.intent.ack.json')
    print(json.dumps({'passed': True, 'observations': observations, 'stopped': True, 'exitCode': 143,
        'stateBytes': sum(p.stat().st_size for p in STATE.rglob('*') if p.is_file())}))


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--verify-original', action='store_true')
    options = parser.parse_args()
    if options.verify_original:
        verify_original()
    else:
        main()
