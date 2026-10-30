"""Create one original secured OpenSearch HTTPS source; never reset existing state."""
from pathlib import Path
import bcrypt, yaml, re, argparse, base64, hashlib, json, os, pwd, secrets, shutil, socket, ssl, subprocess, time, urllib.error, urllib.request

STATE = Path('/var/tmp/oasis-opensearch-cql')
IMAGE = 'docker.io/opensearchproject/opensearch@sha256:5c20af6b5cb472a74ba99f663bee3dd03afbeced14b3897b220a87fc75278445'
NAME = 'oasis-opensearch-cql'
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
        assert probe.connect_ex(('127.0.0.1', 19201)) != 0
    image = json.loads(command('image', 'inspect', IMAGE))[0]
    assert image['Id'] == 'f546dde21473d6d15f4fe4e76e10a5da3fc09f6a0799721642344f9abab0737d'
    STATE.mkdir(mode=0o700)
    os.chown(STATE, OWNER, OWNER)
    pending = STATE/'environment.pending.json'
    durable(pending, {'image': IMAGE, 'name': NAME, 'port': 19201, 'securityPlugin': True})
    mount = Path(command('image', 'mount', IMAGE))
    try:
        shutil.copytree(mount/'usr/share/opensearch/config', STATE/'config')
    finally:
        command('image', 'unmount', IMAGE)
    for name in ['data', 'secrets', 'config/tls']:
        (STATE/name).mkdir(mode=0o700)
    password = secrets.token_hex(32)
    durable(STATE/'credentials.json', {'username': 'oasis_test', 'password': password})
    private_text(STATE/'secrets/password', password)
    old_umask = os.umask(0o077)
    try:
        openssl('req', '-x509', '-newkey', 'rsa:3072', '-nodes', '-days', '365',
            '-subj', '/CN=COSMIC OpenSearch private CA', '-addext', 'basicConstraints=critical,CA:TRUE',
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
    def security_file(name, value):
        path = STATE/'config/opensearch-security'/name
        path.write_text(yaml.safe_dump(value, sort_keys=False))
        os.chmod(path, 0o600)
    security_file('internal_users.yml', {
        '_meta': {'type': 'internalusers', 'config_version': 2},
        'oasis_test': {'hash': bcrypt.hashpw(password.encode(), bcrypt.gensalt(rounds=12)).decode(),
            'reserved': True, 'backend_roles': ['cosmic_admin'], 'description': 'Owned COSMIC native fixture'}})
    security_file('roles_mapping.yml', {'_meta': {'type': 'rolesmapping', 'config_version': 2},
        'all_access': {'reserved': False, 'backend_roles': ['cosmic_admin']}})
    security_file('config.yml', {'_meta': {'type': 'config', 'config_version': 2}, 'config': {'dynamic': {
        'http': {'anonymous_auth_enabled': False}, 'authc': {'basic_internal_auth_domain': {
            'http_enabled': True, 'transport_enabled': True, 'order': 0,
            'http_authenticator': {'type': 'basic', 'challenge': True}, 'authentication_backend': {'type': 'intern'}}}}}})
    settings = {
        'cluster.name': 'COSMIC-CQL-OpenSearch', 'node.name': 'cosmic-cql-opensearch',
        'network.host': '127.0.0.1', 'http.host': '0.0.0.0', 'transport.host': '127.0.0.1',
        'discovery.type': 'single-node', 'plugins.security.ssl.http.enabled': True,
        'plugins.security.ssl.http.pemkey_filepath': 'tls/node.key',
        'plugins.security.ssl.http.pemcert_filepath': 'tls/node.crt',
        'plugins.security.ssl.http.pemtrustedcas_filepath': 'tls/ca.crt',
        'plugins.security.ssl.transport.pemkey_filepath': 'tls/node.key',
        'plugins.security.ssl.transport.pemcert_filepath': 'tls/node.crt',
        'plugins.security.ssl.transport.pemtrustedcas_filepath': 'tls/ca.crt',
        'transport.ssl.enforce_hostname_verification': True,
        'plugins.security.nodes_dn': ['CN=localhost'],
        'plugins.security.authcz.admin_dn': ['CN=cosmic-cql-admin'],
        'plugins.security.allow_default_init_securityindex': True,
        'plugins.security.allow_unsafe_democertificates': False,
        'plugins.security.restapi.roles_enabled': ['all_access']}
    (STATE/'config/opensearch.yml').write_text(yaml.safe_dump(settings, sort_keys=False))
    for name in ['config', 'data', 'secrets']:
        for root, dirs, files in os.walk(STATE/name):
            os.chown(root, 1000, 1000)
            for filename in files:
                os.chown(Path(root)/filename, 1000, 1000)
    container = command('create', '--name', NAME, '--label', 'oasis.task=cosmic-cql',
        '--publish', '127.0.0.1:19201:9200', '--cpus', '2', '--memory', '2g', '--pids-limit', '512',
        '--env', 'OPENSEARCH_JAVA_OPTS=-Xms512m -Xmx512m', '--env', 'DISABLE_INSTALL_DEMO_CONFIG=true',
        '--env', 'DISABLE_PERFORMANCE_ANALYZER_AGENT_CLI=true',
        '--volume', str(STATE/'data')+':/usr/share/opensearch/data',
        '--volume', str(STATE/'config')+':/usr/share/opensearch/config',
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
    clusters = set(re.findall(r'cluster UUID set to \[([^\]]+)\]', raw))
    nodes = set(re.findall(r'node ID \[([^\]]+)\]', raw))
    assert len(clusters) == len(nodes) == 1, 'Original startup identity ambiguous; never replace baseline'
    cluster, node = next(iter(clusters)), next(iter(nodes))
    durable(STATE/'original-startup-observation.ack.json', {'clusterUuid': cluster, 'nodeId': node,
        'caSha256': hashlib.sha256((STATE/'ca.pem').read_bytes()).hexdigest(),
        'source': 'original OpenSearch startup node ID / cluster UUID log records', 'initialStopExitCode': record['State']['ExitCode']})
    verify_native(NAME, STATE/'environment.pending.json', (cluster, node))


def verify_native(container, pending, expected=None):
    password = json.loads((STATE/'credentials.json').read_text())['password']
    context = ssl.create_default_context(cafile=str(STATE/'ca.pem'))
    authorization = 'Basic '+base64.b64encode(('oasis_test:'+password).encode()).decode()

    def get(path, authenticated=True):
        headers = {'Authorization': authorization} if authenticated else {}
        with urllib.request.urlopen(urllib.request.Request('https://localhost:19201'+path, headers=headers),
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
                    auth = get('/_plugins/_security/authinfo')
                    health = get('/_cluster/health?wait_for_status=yellow&timeout=2s')
                    if identity.get('version', {}).get('number') == '3.9.0' and auth['user_name'] == 'oasis_test' and 'all_access' in auth['roles'] and health['status'] in ['yellow', 'green'] and not health['timed_out']:
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
            auth = get('/_plugins/_security/authinfo')
            assert auth['user_name'] == 'oasis_test' and 'all_access' in auth['roles']
            assert get('/_cat/indices/oasis_*?format=json') == [], 'No business indices before original source writes'
            nodes = get('/_nodes/_local')['nodes']
            assert len(nodes) == 1
            observation = {'phase': phase, 'clusterUuid': identity['cluster_uuid'], 'nodeId': next(iter(nodes)),
                'version': identity['version']['number'], 'distribution': identity['version']['distribution'], 'user': auth['user_name']}
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
