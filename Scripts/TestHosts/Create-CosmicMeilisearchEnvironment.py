"""One retained free Meilisearch1.56.0 source, native TLS and API-key authentication."""
from pathlib import Path
import json, os, pwd, secrets, socket, ssl, subprocess, time
import urllib.error, urllib.request

STATE = Path('/var/tmp/oasis-meilisearch-cql')
IMAGE = 'docker.io/getmeili/meilisearch@sha256:3a2dda1ade2280eecb9b777fbe4e27dc82b458e4e335929884481b3850e7cd03'
IMAGE_ID = '6698884e04b6221ec084ac4567761ffd1dc7672f152a15ee823d5f82981f5a87'
NAME = 'oasis-meilisearch-cql'
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
    with urllib.request.urlopen(urllib.request.Request('https://localhost:17700'+path, headers=headers, data=data),
            context=ssl.create_default_context(cafile=str(STATE/'ca.pem')), timeout=10) as response:
        raw = response.read()
        return json.loads(raw) if as_json else raw.decode()


def ready():
    deadline = time.monotonic()+180
    while time.monotonic() < deadline:
        assert json.loads(command('inspect', NAME))[0]['State']['Running'], 'Native service stopped before readiness'
        try:
            assert request('/health')['status'] == 'available'
            meta = request('/version')
            assert meta['pkgVersion'] == '1.56.0'
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
    # v1.56.0 main.rs disables Actix signal handling; native SIGTERM exits143.
    # This is a signal-stop/reopen proof, not graceful-shutdown certification.
    assert current['ExitCode'] == 143 and current['StoppedByUser'], 'Unexpected native termination'


def main():
    assert os.geteuid() == 0 and not STATE.exists(), 'Preserve original source/unknown outcome'
    assert subprocess.run(['podman', 'container', 'exists', NAME]).returncode == 1
    for port in [17700]:
        with socket.socket() as probe:
            assert probe.connect_ex(('127.0.0.1', port)) != 0
    assert json.loads(command('image', 'inspect', IMAGE))[0]['Id'] == IMAGE_ID
    STATE.mkdir(mode=0o700)
    os.chown(STATE, OWNER, OWNER)
    durable('environment.pending.json', {'image': IMAGE, 'name': NAME, 'ports': [17700]})
    for name in ['data', 'config', 'config/tls']:
        (STATE/name).mkdir(mode=0o700)
        os.chown(STATE/name, 1000, 1000)
    durable('credentials.json', {'apiKey': secrets.token_hex(32)})
    old_umask = os.umask(0o077)
    def openssl(*args):
        subprocess.run(['openssl', *args], check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    try:
        openssl('req', '-x509', '-newkey', 'rsa:3072', '-nodes', '-days', '365',
            '-subj', '/CN=COSMIC Meilisearch private CA', '-addext', 'basicConstraints=critical,CA:TRUE',
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
    configuration = {
        'env': 'production', 'db_path': '/meili_data/data.ms', 'http_addr': '0.0.0.0:7700',
        'master_key': json.loads((STATE/'credentials.json').read_text())['apiKey'],
        'no_analytics': True, 'http_payload_size_limit': '4 MB', 'max_indexing_memory': '512 MiB',
        'max_indexing_threads': 2, 'log_level': 'WARN', 'dump_dir': '/meili_data/dumps',
        'snapshot_dir': '/meili_data/snapshots', 'schedule_snapshot': False,
        'ssl_cert_path': '/etc/cosmic/tls/node.crt', 'ssl_key_path': '/etc/cosmic/tls/node.key'}
    with (STATE/'config/meilisearch.toml').open('x') as file:
        os.fchmod(file.fileno(), 0o600)
        file.write(''.join(k+' = '+json.dumps(v)+'\n' for k,v in configuration.items()))
    os.chown(STATE/'config/meilisearch.toml', 1000, 1000)
    container = command('create', '--name', NAME, '--hostname', 'cosmic-meilisearch', '--label', 'oasis.task=cosmic-cql',
        '--publish', '127.0.0.1:17700:7700', '--cpus', '2', '--memory', '2g', '--pids-limit', '512',
        '--log-opt', 'max-size=2097152', '--user', '1000:1000',
        '--volume', str(STATE/'data')+':/meili_data', '--volume', str(STATE/'config')+':/etc/cosmic:ro',
        IMAGE, '/bin/meilisearch', '--config-file-path=/etc/cosmic/meilisearch.toml')
    durable('container.ack.json', {'id': container, 'imageId': IMAGE_ID})
    observations = []
    for phase in ['initial', 'restart']:
        command('start', NAME)
        try:
            identity = ready()
            assert request('/indexes')['results'] == []
            try:
                request('/indexes', authenticated=False)
            except urllib.error.HTTPError as failure:
                assert failure.code in [401, 403]
            else:
                raise RuntimeError('Unauthenticated native index access succeeded')
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


def verify_original_empty_restart():
    # Explicit reconciliation of the original acknowledged empty environment only.
    # No allocation, credential change, task replay or replacement source.
    assert os.geteuid() == 0 and (STATE/'environment.pending.json').exists()
    assert not (STATE/'environment.json').exists() and not (STATE/'restart.ack.json').exists()
    assert not (STATE/'demo-seed.json').exists() and not (STATE/'demo-seed.pending.json').exists()
    initial = json.loads((STATE/'initial.ack.json').read_text())
    receipt = json.loads((STATE/'container.ack.json').read_text())
    current = json.loads(command('inspect', NAME))[0]
    assert current['Id'] == receipt['id'] and current['Image'] == receipt['imageId'] == IMAGE_ID
    assert not current['State']['Running'] and current['State']['ExitCode'] == 143
    assert not current['State']['OOMKilled'] and not current['State']['Error']
    assert any(m['Source'] == str(STATE/'data') and m['Destination'] == '/meili_data' for m in current['Mounts'])
    inode = (STATE/'data').stat().st_ino
    durable('initial-stop-observation.ack.json', {'exitCode': 143, 'originalVerifierExpectedZero': True})
    command('start', NAME)
    try:
        identity = ready()
        assert identity == initial and (STATE/'data').stat().st_ino == inode
        assert request('/indexes')['results'] == [] and request('/tasks')['results'] == []
        try:
            request('/indexes', authenticated=False)
        except urllib.error.HTTPError as failure:
            assert failure.code in [401, 403]
        else:
            raise RuntimeError('Unauthenticated index access succeeded')
        durable('restart.ack.json', identity)
    finally:
        stop()
    durable('environment.json', {'container': receipt['id'], 'image': IMAGE, 'identity': initial,
        'sourceDirectoryInode': inode})
    (STATE/'environment.pending.json').rename(STATE/'environment.intent.ack.json')
    print(json.dumps({'passed': True, 'originalEmptySourceReopened': True, 'stopped': True,
        'nativeExitCode': 143, 'stateBytes': sum(p.stat().st_size for p in STATE.rglob('*') if p.is_file())}))


if __name__ == '__main__':
    import sys
    if sys.argv[1:] == ['--verify-original-empty-restart']:
        verify_original_empty_restart()
    else:
        assert not sys.argv[1:]
        main()
