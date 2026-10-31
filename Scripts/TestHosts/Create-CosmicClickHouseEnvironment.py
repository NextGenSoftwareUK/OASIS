"""One original free ClickHouse source, authenticated HTTPS and real restart."""
from pathlib import Path
import argparse, base64, hashlib, json, os, pwd, secrets, shutil, socket, ssl, subprocess, time
import urllib.error, urllib.parse, urllib.request

STATE = Path('/var/tmp/oasis-clickhouse-cql')
IMAGE = 'docker.io/clickhouse/clickhouse-server@sha256:0805f9f529673d2a68d1fb50dfef90f30f1710bb1b62d4c3f1d1a99d37459ecc'
IMAGE_ID = '21ecc2f2ad3382c8dcf0ce708ba21347f5b597a31ec2e64238dbcf50208742d6'
NAME = 'oasis-clickhouse-cql'
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


def query(sql, authenticated=True):
    credentials = json.loads((STATE/'credentials.json').read_text())
    headers = {}
    if authenticated:
        headers['Authorization'] = 'Basic '+base64.b64encode(
            (credentials['username']+':'+credentials['password']).encode()).decode()
    with urllib.request.urlopen(urllib.request.Request('https://localhost:18443/?'+
            urllib.parse.urlencode({'query': sql}), headers=headers),
            context=ssl.create_default_context(cafile=str(STATE/'ca.pem')), timeout=10) as response:
        return response.read().decode().strip()


def ready():
    deadline = time.monotonic()+180
    while time.monotonic() < deadline:
        assert json.loads(command('inspect', NAME))[0]['State']['Running'], 'Server stopped before readiness'
        try:
            row = query('SELECT version(), toString(serverUUID()) FORMAT JSONEachRow')
            return json.loads(row)
        except urllib.error.HTTPError:
            raise
        except (urllib.error.URLError, TimeoutError, ConnectionError):
            time.sleep(.5)
    raise RuntimeError('Authenticated HTTPS readiness deadline')


def stop():
    command('stop', '--time', '60', NAME)
    status = json.loads(command('inspect', NAME))[0]['State']
    assert not status['Running'] and not status['OOMKilled'] and not status['Error']
    assert status['ExitCode'] == 0, 'Preserve unclean native stop'


def main(resume_before_config=False):
    assert os.geteuid() == 0
    if resume_before_config:
        assert sorted(p.name for p in STATE.iterdir()) == ['environment.pending.json']
        original = json.loads((STATE/'environment.pending.json').read_text())
        assert original == {'image': IMAGE, 'name': NAME, 'httpsPort': 18443}
    else:
        assert not STATE.exists(), 'Preserve original source/unknown outcome'
    assert subprocess.run(['podman', 'container', 'exists', NAME]).returncode == 1
    with socket.socket() as probe:
        assert probe.connect_ex(('127.0.0.1', 18443)) != 0
    assert json.loads(command('image', 'inspect', IMAGE))[0]['Id'] == IMAGE_ID
    if not resume_before_config:
        STATE.mkdir(mode=0o700)
        os.chown(STATE, OWNER, OWNER)
        durable('environment.pending.json', {'image': IMAGE, 'name': NAME, 'httpsPort': 18443})
    mount = Path(command('image', 'mount', IMAGE))
    try:
        server = mount/'usr/bin/clickhouse-server'
        target = mount/str(server.readlink()).lstrip('/') if server.is_symlink() else server
        assert target.is_file(), 'Resolve vendor executable symlink inside image, not host filesystem'
        shutil.copytree(mount/'etc/clickhouse-server', STATE/'config')
    finally:
        command('image', 'unmount', IMAGE)
    (STATE/'data').mkdir(mode=0o700)
    (STATE/'logs').mkdir(mode=0o700)
    (STATE/'config/tls').mkdir(mode=0o700)
    password = secrets.token_hex(32)
    durable('credentials.json', {'username': 'oasis_test', 'password': password})
    old_umask = os.umask(0o077)
    def openssl(*args):
        subprocess.run(['openssl', *args], check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    try:
        openssl('req', '-x509', '-newkey', 'rsa:3072', '-nodes', '-days', '365',
            '-subj', '/CN=COSMIC ClickHouse private CA', '-addext', 'basicConstraints=critical,CA:TRUE',
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
    (STATE/'config/config.d/cosmic.xml').write_text('''<clickhouse>
<http_port remove="1"/><tcp_port remove="1"/><https_port>8443</https_port>
<listen_host replace="1">0.0.0.0</listen_host>
<max_server_memory_usage>1073741824</max_server_memory_usage>
<background_schedule_pool_size>8</background_schedule_pool_size>
<max_thread_pool_size>256</max_thread_pool_size>
<openSSL><server><certificateFile>/etc/clickhouse-server/tls/node.crt</certificateFile>
<privateKeyFile>/etc/clickhouse-server/tls/node.key</privateKeyFile></server></openSSL>
</clickhouse>''')
    digest = hashlib.sha256(password.encode()).hexdigest()
    (STATE/'config/users.d/cosmic.xml').write_text('''<clickhouse><users><default remove="1"/>
<oasis_test><password_sha256_hex>'''+digest+'''</password_sha256_hex>
<networks><ip>::/0</ip></networks><profile>default</profile><quota>default</quota>
<access_management>1</access_management></oasis_test></users>
<profiles><default><max_threads>2</max_threads></default></profiles></clickhouse>''')
    for name in ['config', 'data', 'logs']:
        for root, dirs, files in os.walk(STATE/name):
            os.chown(root, 101, 101)
            for filename in files:
                os.chown(Path(root)/filename, 101, 101)
    container = command('create', '--name', NAME, '--label', 'oasis.task=cosmic-cql',
        '--publish', '127.0.0.1:18443:8443', '--cpus', '2', '--memory', '2g',
        '--pids-limit', '512', '--ulimit', 'nofile=262144:262144', '--user', '101:101',
        '--entrypoint', '/usr/bin/clickhouse-server',
        '--volume', str(STATE/'data')+':/var/lib/clickhouse',
        '--volume', str(STATE/'config')+':/etc/clickhouse-server',
        '--volume', str(STATE/'logs')+':/var/log/clickhouse-server', IMAGE,
        '--config-file=/etc/clickhouse-server/config.xml')
    durable('container.ack.json', {'id': container, 'imageId': IMAGE_ID})
    observations = []
    for phase in ['initial', 'restart']:
        command('start', NAME)
        try:
            identity = ready()
            assert identity['version()'].startswith('26.8.')
            assert query("SELECT count() FROM system.databases WHERE name='oasis'") == '0'
            try:
                query('SELECT 1', authenticated=False)
            except urllib.error.HTTPError as failure:
                assert failure.code in [401, 403]
            else:
                raise RuntimeError('Unauthenticated query unexpectedly succeeded')
            if observations:
                assert identity == observations[0]
            durable(phase+'.ack.json', identity)
            observations.append(identity)
        finally:
            stop()
    durable('environment.json', {'container': container, 'image': IMAGE, 'identity': observations[0]})
    (STATE/'environment.pending.json').rename(STATE/'environment.intent.ack.json')
    print(json.dumps({'passed': True, 'identity': observations[0], 'stopped': True,
        'stateBytes': sum(p.stat().st_size for p in STATE.rglob('*') if p.is_file())}))


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--resume-before-config', action='store_true')
    main(parser.parse_args().resume_before_config)
