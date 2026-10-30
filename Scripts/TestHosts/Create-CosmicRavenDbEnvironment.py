"""Create a single retained TLS/client-certificate RavenDB fixture, never reset it."""
from pathlib import Path
import argparse, json, os, pwd, ssl, subprocess, time, urllib.error, urllib.request, uuid

STATE = Path('/var/tmp/oasis-ravendb-cql')
IMAGE = 'docker.io/ravendb/ravendb@sha256:92bd726e9d0031f48c8203c9be660c9f9b361ff11b6b352a17ca4a861e366471'
NAME = 'oasis-ravendb-cql'
OWNER = pwd.getpwnam('dellams').pw_uid

def command(*args):
    return subprocess.check_output(['podman', *args], text=True).strip()

def durable(path, value, owner=OWNER):
    with path.open('x') as file:
        os.chmod(path, 0o600); json.dump(value, file); file.flush(); os.fsync(file.fileno())
    os.chown(path, owner, owner)

def openssl(*args):
    # No key material, passwords or certificate bodies are sent to console.
    subprocess.run(['openssl', *args], check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)

def main():
    assert os.geteuid() == 0
    parser = argparse.ArgumentParser()
    parser.add_argument('--resume', action='store_true')
    options = parser.parse_args()
    image = json.loads(command('image', 'inspect', IMAGE))[0]
    assert image['Id'] == '933a130ff10d66a6a28b8829c5f6f7af886162fc2425b4012a9f41649072816e'
    pending = STATE / 'environment.pending.json'
    if not options.resume:
        assert not STATE.exists(), 'Original source exists; explicit validated resume required'
        assert subprocess.run(['podman', 'container', 'exists', NAME]).returncode == 1
        STATE.mkdir(mode=0o700); os.chown(STATE, OWNER, OWNER)
        pending = STATE / 'environment.pending.json'
        durable(pending, {'image': IMAGE, 'name': NAME, 'port': 18003})
        for name in ['data', 'certs', 'config']:
            path = STATE / name; path.mkdir(mode=0o700); os.chown(path, 999, 999)
        old_umask = os.umask(0o077)
        try:
            openssl('req', '-x509', '-newkey', 'rsa:3072', '-nodes', '-days', '365',
                '-subj', '/CN=COSMIC RavenDB private demo CA',
                '-addext', 'basicConstraints=critical,CA:TRUE', '-addext', 'keyUsage=critical,keyCertSign,cRLSign', '-keyout', str(STATE / 'ca.key'), '-out', str(STATE / 'ca.pem'))
            openssl('req', '-new', '-newkey', 'rsa:3072', '-nodes', '-subj', '/CN=localhost',
                '-keyout', str(STATE / 'client.key'), '-out', str(STATE / 'server.csr'))
            extensions = STATE / 'extensions.cnf'
            extensions.write_text('subjectAltName=DNS:localhost,IP:127.0.0.1\nextendedKeyUsage=serverAuth,clientAuth\nkeyUsage=digitalSignature,keyEncipherment\nbasicConstraints=critical,CA:FALSE\n')
            openssl('x509', '-req', '-in', str(STATE / 'server.csr'), '-CA', str(STATE / 'ca.pem'),
                '-CAkey', str(STATE / 'ca.key'), '-CAcreateserial', '-days', '365', '-sha256',
                '-extfile', str(extensions), '-out', str(STATE / 'client.pem'))
            openssl('pkcs12', '-export', '-out', str(STATE / 'client.pfx'), '-inkey', str(STATE / 'client.key'),
                '-in', str(STATE / 'client.pem'), '-certfile', str(STATE / 'ca.pem'), '-passout', 'pass:')
            server_pfx = STATE / 'certs/server.pfx'
            server_pfx.write_bytes((STATE / 'client.pfx').read_bytes()); os.chown(server_pfx, 999, 999)
            for path in STATE.iterdir():
                if path.is_file(): os.chmod(path, 0o600); os.chown(path, OWNER, OWNER)
        finally: os.umask(old_umask)
        durable(STATE / 'config/settings.json', {
            'ServerUrl': 'https://0.0.0.0:8080', 'PublicServerUrl': 'https://localhost:18003',
            'ServerUrl.Tcp': 'tcp://0.0.0.0:38888', 'Setup.Mode': 'None',
            'Security.Certificate.Path': '/certs/server.pfx', 'DataDir': '/var/lib/ravendb/data',
            'License.Eula.Accepted': True
        }, 999)
        container = command('create', '--name', NAME, '--label', 'oasis.task=cosmic-cql',
            '--publish', '127.0.0.1:18003:8080', '--cpus', '2', '--memory', '2g', '--pids-limit', '256',
            '--volume', str(STATE / 'data') + ':/var/lib/ravendb/data',
            '--volume', str(STATE / 'certs') + ':/certs:ro',
            '--volume', str(STATE / 'config/settings.json') + ':/etc/ravendb/settings.json:ro',
            '--entrypoint', '/usr/lib/ravendb/server/Raven.Server', IMAGE, '--config-path', '/etc/ravendb/settings.json', '--non-interactive')
        durable(STATE / 'container.ack.json', {'id': container, 'imageId': image['Id']})
    else:
        assert pending.exists() and not (STATE / 'environment.json').exists()
        assert json.loads(pending.read_text()) == {'image': IMAGE, 'name': NAME, 'port': 18003}
        ack = json.loads((STATE / 'container.ack.json').read_text())
        container = ack['id']; native = json.loads(command('inspect', container))[0]
        assert native['Id'] == container and native['Image'] == ack['imageId'] == image['Id']
        assert native['State']['Status'] == 'exited' and native['State']['ExitCode'] == 0
        assert native['HostConfig']['PortBindings']['8080/tcp'] == [{'HostIp': '127.0.0.1', 'HostPort': '18003'}]
        assert any(m['Source'] == str(STATE / 'data') and m['Destination'] == '/var/lib/ravendb/data' for m in native['Mounts'])
        assert not (STATE / 'database.ack.json').exists(), 'Database admission already attempted; reconcile exact acknowledgement first'
    context = ssl.create_default_context(cafile=str(STATE / 'ca.pem'))
    context.load_cert_chain(str(STATE / 'client.pem'), str(STATE / 'client.key'))
    def request(path, method='GET', body=None, certificate=True):
        req = urllib.request.Request('https://localhost:18003' + path, method=method,
            data=json.dumps(body).encode() if body is not None else None, headers={'Content-Type': 'application/json'})
        trust = context if certificate else ssl.create_default_context(cafile=str(STATE / 'ca.pem'))
        with urllib.request.urlopen(req, context=trust, timeout=3) as response:
            return json.load(response)
    observations = []
    for phase in ['first', 'restart']:
        command('start', container)
        try:
            deadline = time.monotonic() + 60
            while time.monotonic() < deadline:
                assert json.loads(command('inspect', container))[0]['State']['Running'], 'Owned server exited; preserve logs/source'
                try:
                    version = request('/build/version'); break
                except urllib.error.URLError as error:
                    if isinstance(error.reason, ssl.SSLError): raise
                    time.sleep(.5)
                except ConnectionError: time.sleep(.5)
            else: raise RuntimeError('Owned TLS readiness deadline')
            if phase == 'first':
                assert request('/databases?start=0&pageSize=10&namesOnly=true')['Databases'] == [], 'Unexpected existing database'
                operation = str(uuid.uuid4())
                durable(STATE / 'database.pending.json', {'database': 'oasis', 'operationId': operation})
                acknowledgement = request('/admin/databases?name=oasis&replicationFactor=1&raft-request-id=' + operation, 'PUT', {'DatabaseName': 'oasis', 'Settings': {}})
                durable(STATE / 'database.ack.json', acknowledgement)
                (STATE / 'database.pending.json').unlink()
            stats = request('/databases/oasis/stats')
            assert stats['CountOfDocuments'] == 0
            try: request('/admin/databases?name=oasis', certificate=False)
            except urllib.error.HTTPError as error: assert error.code in [401, 403]
            else: raise AssertionError('Native administration allowed no client certificate')
            observations.append({'phase': phase, 'version': version, 'databaseId': stats['DatabaseId'], 'documents': 0})
        finally:
            command('stop', '--time', '30', container)
            status = json.loads(command('inspect', container))[0]['State']
            assert not status['Running'] and status['ExitCode'] == 0, 'Owned teardown incomplete'
    assert observations[0]['databaseId'] == observations[1]['databaseId']
    durable(STATE / 'environment.json', {'containerId': container, 'image': IMAGE, 'imageId': image['Id'], 'observations': observations})
    pending.unlink()
    print(json.dumps({'passed': True, 'containerId': container, 'imageId': image['Id'], 'imageBytes': image['Size'],
        'observations': observations, 'stateBytes': sum(p.stat().st_size for p in STATE.rglob('*') if p.is_file()), 'stopped': True}))

if __name__ == '__main__': main()
