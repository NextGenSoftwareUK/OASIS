"""One retained free Milvus2.6.26 source: embedded etcd/local storage/RocksMQ and native TLS."""
from pathlib import Path
import json, os, pwd, secrets, socket, ssl, subprocess, time
import urllib.error, urllib.request

STATE = Path('/var/tmp/oasis-milvus-cql')
IMAGE = 'docker.io/milvusdb/milvus@sha256:e6066d0afc4eeab69b7c9269e49be2b9ae54c740a21c05f230c4e5743c646ee3'
IMAGE_ID = '42a136cd8ee294bf880feb0a8caa92214cba8b6b5613f86848d93dc712659eec'
NAME = 'oasis-milvus-cql'
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
        headers['Authorization'] = 'Bearer '+'root:'+json.loads((STATE/'credentials.json').read_text())['password']
    data = None
    if body is not None:
        headers['Content-Type'] = 'application/json'
        data = json.dumps(body).encode()
    with urllib.request.urlopen(urllib.request.Request('https://localhost:19531'+path, headers=headers, data=data),
            context=ssl.create_default_context(cafile=str(STATE/'ca.pem')), timeout=10) as response:
        raw = response.read()
        return json.loads(raw) if as_json else raw.decode()


def ready():
    deadline = time.monotonic()+180
    while time.monotonic() < deadline:
        assert json.loads(command('inspect', NAME))[0]['State']['Running'], 'Native service stopped before readiness'
        try:
            meta = request('/v2/vectordb/collections/list', {})
            if meta.get('code') != 0:
                time.sleep(.5)
                continue
            native = json.loads(command('exec', NAME, 'curl', '-sfS', '--max-time', '5', '-X', 'POST',
                'http://127.0.0.1:2379/v3/maintenance/status', '-d', '{}'))
            header = native['header']
            assert int(header['cluster_id']) > 0 and int(header['member_id']) > 0
            return {'version': '2.6.26', 'clusterId': header['cluster_id'], 'memberId': header['member_id']}
        except urllib.error.HTTPError as failure:
            if failure.code != 503:
                raise
        except (urllib.error.URLError, TimeoutError, ConnectionError):
            pass
        time.sleep(.5)
    raise RuntimeError('Native authenticated HTTPS/schema readiness deadline')


def stop():
    prior = json.loads(command('inspect', NAME))[0]['State']
    if not prior['Running']:
        assert prior['ExitCode'] == 0 and not prior['OOMKilled'] and not prior['Error'], 'Preserve failed native exit; do not relabel it with stop'
        return
    command('stop', '--time', '60', NAME)
    current = json.loads(command('inspect', NAME))[0]['State']
    assert not current['Running'] and not current['OOMKilled'] and not current['Error']
    assert current['ExitCode'] == 0, 'Preserve unclean native stop'


def create_container():
    return command('create', '--name', NAME, '--hostname', 'cosmic-milvus', '--label', 'oasis.task=cosmic-cql',
        '--publish', '127.0.0.1:19531:19530', '--cpus', '2', '--memory', '4g', '--pids-limit', '1024',
        '--log-opt', 'max-size=2097152', '--user', '0:0', '--env-file', str(STATE/'service.env'),
        '--volume', str(STATE/'data')+':/var/lib/milvus', '--volume', str(STATE/'config')+':/etc/cosmic:ro',
        '--volume', str(STATE/'config/user.yaml')+':/milvus/configs/user.yaml:ro', IMAGE, 'milvus', 'run', 'standalone')


def main():
    assert os.geteuid() == 0 and not STATE.exists(), 'Preserve original source/unknown outcome'
    assert subprocess.run(['podman', 'container', 'exists', NAME]).returncode == 1
    for port in [19531]:
        with socket.socket() as probe:
            assert probe.connect_ex(('127.0.0.1', port)) != 0
    assert json.loads(command('image', 'inspect', IMAGE))[0]['Id'] == IMAGE_ID
    STATE.mkdir(mode=0o700)
    os.chown(STATE, OWNER, OWNER)
    durable('environment.pending.json', {'image': IMAGE, 'name': NAME, 'ports': [19531]})
    for name in ['data', 'config', 'config/tls']:
        (STATE/name).mkdir(mode=0o700)
        os.chown(STATE/name, 1000, 1000)
    durable('credentials.json', {'username': 'root', 'password': secrets.token_hex(32)})
    old_umask = os.umask(0o077)
    def openssl(*args):
        subprocess.run(['openssl', *args], check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    try:
        openssl('req', '-x509', '-newkey', 'rsa:3072', '-nodes', '-days', '365',
            '-subj', '/CN=COSMIC Milvus private CA', '-addext', 'basicConstraints=critical,CA:TRUE',
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
    config = {
        'etcd': {'rootPath': 'cosmic-cql', 'use': {'embed': True}, 'data': {'dir': '/var/lib/milvus/etcd'},
            'endpoints': '127.0.0.1:2379', 'log': {'level': 'warn', 'path': 'stdout'}},
        'common': {'storageType': 'local', 'gracefulStopTimeout': 45,
            'security': {'authorizationEnabled': True, 'defaultRootPassword': json.loads((STATE/'credentials.json').read_text())['password'],
                'enablePublicPrivilege': False, 'managementMetricsOnly': True, 'tlsMode': 1},
            'threadCoreCoefficient': {'maxThreadsSize': 16}},
        'tls': {'serverPemPath': '/etc/cosmic/tls/node.crt', 'serverKeyPath': '/etc/cosmic/tls/node.key', 'caPemPath': '/etc/cosmic/ca.pem'},
        'mq': {'type': 'rocksmq'}, 'rocksmq': {'path': '/var/lib/milvus/rdb_data', 'lrucacheratio': .01},
        'localStorage': {'path': '/var/lib/milvus/data'},
        # Native TLS requires distinct HTTP/gRPC listeners; publish only HTTPS19530.
        'proxy': {'port': 19532, 'http': {'port': 19530, 'enabled': True, 'enableV1': False, 'enablePprof': False, 'enableWebUI': False}},
        'rootCoord': {'dmlChannelNum': 4}, 'log': {'level': 'warn', 'file': {'rootPath': ''}}}
    (STATE/'config/user.yaml').write_text(json.dumps(config))
    (STATE/'config/embedEtcd.yaml').write_text(json.dumps({'listen-client-urls': 'http://127.0.0.1:2379',
        'advertise-client-urls': 'http://127.0.0.1:2379', 'quota-backend-bytes': 134217728,
        'auto-compaction-mode': 'revision', 'auto-compaction-retention': '1000'}))
    import shutil
    shutil.copyfile(STATE/'ca.pem', STATE/'config/ca.pem')
    for file in (STATE/'config').rglob('*'):
        if file.is_file():
            os.chmod(file, 0o600)
            os.chown(file, 1000, 1000)
    env = {'ETCD_USE_EMBED': 'true', 'ETCD_DATA_DIR': '/var/lib/milvus/etcd',
        'ETCD_CONFIG_PATH': '/etc/cosmic/embedEtcd.yaml', 'COMMON_STORAGETYPE': 'local',
        'DEPLOY_MODE': 'STANDALONE', 'GOMAXPROCS': '2', 'GOMEMLIMIT': '2GiB', 'OMP_NUM_THREADS': '2'}
    with (STATE/'service.env').open('x') as file:
        os.fchmod(file.fileno(), 0o600)
        file.write(''.join(k+'='+v+'\n' for k,v in env.items()))
    os.chown(STATE/'service.env', OWNER, OWNER)
    container = create_container()
    durable('container.ack.json', {'id': container, 'imageId': IMAGE_ID})
    observations = []
    for phase in ['initial', 'restart']:
        command('start', NAME)
        try:
            identity = ready()
            assert request('/v2/vectordb/collections/list', {})['data'] == []
            try:
                unauthorized = request('/v2/vectordb/collections/list', {}, authenticated=False)
                assert unauthorized['code'] != 0, 'Unauthenticated native collection access succeeded'
            except urllib.error.HTTPError as failure:
                assert failure.code in [401, 403]
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
