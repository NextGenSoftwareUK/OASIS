"""One free Solr10.1.0 standalone source: explicit canonical cores, TLS and Basic auth."""
from pathlib import Path
import base64, hashlib, json, os, pwd, secrets, shutil, socket, ssl, subprocess, time, uuid
import urllib.error, urllib.request

STATE = Path('/var/tmp/oasis-solr-cql')
IMAGE = 'docker.io/library/solr@sha256:0586be344eeb1eb5682e0bd1260001e004219ed3bd788d6e0d3620502c057f1c'
IMAGE_ID = '86975efef3b366376a29ad27083bcf34e34104a4b376fb009d2735184f75e060'
NAME = 'oasis-solr-cql'
CORES = ['oasis_avatars', 'oasis_avatar_details', 'oasis_holons']
OWNER = pwd.getpwnam('dellams').pw_uid


CANONICAL_SOLR_CONFIG = """<config>
  <luceneMatchVersion>LATEST</luceneMatchVersion>
  <directoryFactory name="DirectoryFactory" class="solr.NRTCachingDirectoryFactory"/>
  <schemaFactory class="ClassicIndexSchemaFactory"/>
  <updateHandler class="solr.DirectUpdateHandler2"><updateLog/></updateHandler>
  <requestHandler name="/select" class="solr.SearchHandler"><lst name="defaults"><str name="df">id</str></lst></requestHandler>
  <requestHandler name="/query" class="solr.SearchHandler"><lst name="defaults"><str name="df">id</str></lst></requestHandler>
  <requestHandler name="/get" class="solr.SearchHandler"><lst name="defaults"><str name="omitHeader">false</str></lst><arr name="components"><str>get</str></arr></requestHandler>
  <searchComponent name="get" class="solr.RealTimeGetComponent"/>
</config>
"""


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
        credentials = json.loads((STATE/'credentials.json').read_text())
        headers['Authorization'] = 'Basic '+base64.b64encode((credentials['username']+':'+credentials['password']).encode()).decode()
    data = None
    if body is not None:
        headers['Content-Type'] = 'application/json'
        data = json.dumps(body).encode()
    with urllib.request.urlopen(urllib.request.Request('https://localhost:18983'+path, headers=headers, data=data),
            context=ssl.create_default_context(cafile=str(STATE/'ca.pem')), timeout=10) as response:
        raw = response.read()
        return json.loads(raw) if as_json else raw.decode()


def ready():
    deadline = time.monotonic()+180
    while time.monotonic() < deadline:
        assert json.loads(command('inspect', NAME))[0]['State']['Running'], 'Native service stopped before readiness'
        try:
            meta = request('/solr/admin/info/system?wt=json')
            assert meta['mode'] == 'std' and meta['lucene']['solr-spec-version'] == '10.1.0'
            status = request('/solr/admin/cores?action=STATUS&wt=json')
            assert status.get('initFailures') == {}, 'Native core initialization failed'
            if set(status['status']) != set(CORES):
                time.sleep(.5)
                continue
            return {'version': meta['lucene']['solr-spec-version'], 'mode': meta['mode']}
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
    # Java's completed SIGTERM shutdown hooks retain signal exit143.
    assert current['ExitCode'] == 143 and current['StoppedByUser'], 'Unexpected native termination'


def main():
    assert os.geteuid() == 0 and not STATE.exists(), 'Preserve original source/unknown outcome'
    assert subprocess.run(['podman', 'container', 'exists', NAME]).returncode == 1
    for port in [18983]:
        with socket.socket() as probe:
            assert probe.connect_ex(('127.0.0.1', port)) != 0
    assert json.loads(command('image', 'inspect', IMAGE))[0]['Id'] == IMAGE_ID
    STATE.mkdir(mode=0o700)
    os.chown(STATE, OWNER, OWNER)
    durable('environment.pending.json', {'image': IMAGE, 'name': NAME, 'ports': [18983]})
    for name in ['data', 'data/data', 'data/logs', 'config', 'config/tls']:
        (STATE/name).mkdir(mode=0o700)
        os.chown(STATE/name, 8983, 8983)
    durable('credentials.json', {'username': 'cosmic', 'password': secrets.token_hex(32), 'keystorePassword': secrets.token_hex(24)})
    old_umask = os.umask(0o077)
    def openssl(*args):
        subprocess.run(['openssl', *args], check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    try:
        openssl('req', '-x509', '-newkey', 'rsa:3072', '-nodes', '-days', '365',
            '-subj', '/CN=COSMIC Solr private CA', '-addext', 'basicConstraints=critical,CA:TRUE',
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
        os.chown(path, 8983, 8983)
    credentials = json.loads((STATE/'credentials.json').read_text())
    salt = secrets.token_bytes(32)
    encoded = base64.b64encode(hashlib.sha256(hashlib.sha256(salt+credentials['password'].encode()).digest()).digest()).decode()+' '+base64.b64encode(salt).decode()
    home = STATE/'data/data'
    (home/'security.json').write_text(json.dumps({'authentication': {'class': 'solr.BasicAuthPlugin',
        'blockUnknown': True, 'forwardCredentials': False, 'credentials': {credentials['username']: encoded}},
        'authorization': {'class': 'solr.RuleBasedAuthorizationPlugin',
            'user-role': {credentials['username']: 'admin'}, 'permissions': [{'name': 'all', 'role': 'admin'}]}}))
    password_file = STATE/'config/tls/keystore-password.txt'
    password_file.write_text(credentials['keystorePassword'])
    openssl('pkcs12', '-export', '-inkey', str(STATE/'config/tls/node.key'), '-in', str(STATE/'config/tls/node.crt'),
        '-certfile', str(STATE/'ca.pem'), '-out', str(STATE/'config/tls/node.p12'), '-passout', 'file:'+str(password_file))
    root = Path(command('image', 'mount', IMAGE))
    core_ids = {}
    try:
        shutil.copyfile(root/'opt/solr/server/solr/solr.xml', home/'solr.xml')
        for name in CORES:
            core = home/name
            core.mkdir(mode=0o700)
            shutil.copytree(root/'opt/solr/server/solr/configsets/_default/conf', core/'conf')
            (core/'conf/solrconfig.xml').write_text(CANONICAL_SOLR_CONFIG)
            core_id = 'cosmic_'+uuid.uuid4().hex
            core_ids[name] = core_id
            fields = []
            def field(name, type, indexed, stored, dv):
                fields.append('<field name="'+name+'" type="'+type+'" indexed="'+str(indexed).lower()+'" stored="'+str(stored).lower()+'" docValues="'+str(dv).lower()+'" multiValued="false"/>')
            for field_name in ['id', 'name', 'username', 'email', 'parentHolonId']:
                field(field_name, 'string', True, True, False)
            field('data_json', 'string', False, True, False)
            field('holonType', 'pint', True, True, False)
            field('isDeleted', 'boolean', True, True, False)
            field('_version_', 'plong', False, False, True)
            schema = '<schema name="'+core_id+'" version="1.7"><uniqueKey>id</uniqueKey>'
            for field_type, implementation in [('string', 'solr.StrField'), ('pint', 'solr.IntPointField'), ('plong', 'solr.LongPointField'), ('boolean', 'solr.BoolField')]:
                schema += '<fieldType name="'+field_type+'" class="'+implementation+'"/>'
            schema += ''.join(fields)+'</schema>'
            (core/'conf/managed-schema.xml').write_text(schema)
            (core/'core.properties').write_text('name='+name+'\nconfig=solrconfig.xml\nschema=managed-schema.xml\n')
    finally:
        command('image', 'unmount', IMAGE)
    for folder in [STATE/'data', STATE/'config']:
        for path in [folder, *folder.rglob('*')]:
            os.chown(path, 8983, 8983)
            os.chmod(path, 0o700 if path.is_dir() else 0o600)
    env = {'SOLR_HOME': '/var/solr/data', 'SOLR_LOGS_DIR': '/var/solr/logs', 'SOLR_HEAP': '512m',
        'SOLR_PORT': '8983', 'SOLR_JETTY_HOST': '0.0.0.0', 'SOLR_SSL_ENABLED': 'true',
        'SOLR_SSL_KEY_STORE': '/etc/cosmic/tls/node.p12', 'SOLR_SSL_KEY_STORE_TYPE': 'PKCS12',
        'SOLR_SSL_KEY_STORE_PASSWORD': credentials['keystorePassword'],
        'SOLR_SSL_TRUST_STORE': '/etc/cosmic/tls/node.p12', 'SOLR_SSL_TRUST_STORE_TYPE': 'PKCS12',
        'SOLR_SSL_TRUST_STORE_PASSWORD': credentials['keystorePassword'],
        'SOLR_SSL_NEED_CLIENT_AUTH': 'false', 'SOLR_SSL_WANT_CLIENT_AUTH': 'false',
        'SOLR_SSL_CLIENT_HOSTNAME_VERIFICATION': 'true', 'SOLR_SSL_CHECK_PEER_NAME': 'true',
        'SOLR_OPTS': '-XX:ActiveProcessorCount=2'}
    with (STATE/'service.env').open('x') as file:
        os.fchmod(file.fileno(), 0o600)
        file.write(''.join(k+'='+v+'\n' for k,v in env.items()))
    os.chown(STATE/'service.env', OWNER, OWNER)
    durable('core-identities.ack.json', core_ids)
    container = command('create', '--name', NAME, '--hostname', 'cosmic-solr', '--label', 'oasis.task=cosmic-cql',
        '--publish', '127.0.0.1:18983:8983', '--cpus', '2', '--memory', '2g', '--pids-limit', '512',
        '--log-opt', 'max-size=2097152', '--user', '8983:8983', '--env-file', str(STATE/'service.env'),
        '--volume', str(STATE/'data')+':/var/solr', '--volume', str(STATE/'config')+':/etc/cosmic:ro',
        IMAGE, 'solr-foreground', '--user-managed')
    durable('container.ack.json', {'id': container, 'imageId': IMAGE_ID})
    observations = []
    for phase in ['initial', 'restart']:
        command('start', NAME)
        try:
            identity = ready()
            for name in CORES:
                schema = request('/solr/'+name+'/schema?wt=json')['schema']
                assert schema['name'] == core_ids[name]
                assert request('/solr/'+name+'/select?q=*:*&rows=0&wt=json')['response']['numFound'] == 0
            try:
                request('/solr/admin/cores?action=STATUS&wt=json', authenticated=False)
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
