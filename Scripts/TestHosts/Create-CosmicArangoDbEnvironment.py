"""Create one original authenticated ArangoDB source; never reset existing state."""
from pathlib import Path
import argparse, base64, json, os, pwd, secrets, subprocess, time, urllib.error, urllib.request

STATE = Path('/var/tmp/oasis-arangodb-cql')
IMAGE = 'docker.io/library/arangodb@sha256:ddaeda57726a6ff42186a3fea205a848585c92fd54814b892193925338f9e25c'
NAME = 'oasis-arangodb-cql'
OWNER = pwd.getpwnam('dellams').pw_uid

def command(*args):
    return subprocess.check_output(['podman', *args], text=True).strip()

def durable(path, value):
    with path.open('x') as file:
        os.chmod(path, 0o600); json.dump(value, file); file.flush(); os.fsync(file.fileno())
    os.chown(path, OWNER, OWNER)

def main():
    assert os.geteuid() == 0
    parser = argparse.ArgumentParser()
    parser.add_argument('--resume', action='store_true')
    options = parser.parse_args()
    image = json.loads(command('image', 'inspect', IMAGE))[0]
    pending = STATE / 'environment.pending.json'
    if not options.resume:
        assert not STATE.exists(), 'Original state exists; use explicit validated admission resume'
        assert subprocess.run(['podman', 'container', 'exists', NAME]).returncode == 1
        STATE.mkdir(mode=0o700); os.chown(STATE, OWNER, OWNER)
        pending = STATE / 'environment.pending.json'
        durable(pending, {'image': IMAGE, 'name': NAME, 'port': 18529})
        for name in ['data', 'apps']:
            (STATE / name).mkdir(mode=0o700)
        password = secrets.token_hex(32)
        durable(STATE / 'credentials.json', {'username': 'root', 'password': password})
        env_file = STATE / 'container.env'
        with env_file.open('x') as file:
            os.chmod(env_file, 0o600)
            file.write('ARANGO_ROOT_PASSWORD=' + password + '\n')
            file.flush(); os.fsync(file.fileno())
        os.chown(env_file, OWNER, OWNER)
        container = command('create', '--name', NAME, '--label', 'oasis.task=cosmic-cql',
            '--publish', '127.0.0.1:18529:8529', '--cpus', '2', '--memory', '2g', '--pids-limit', '256',
            '--env-file', str(env_file), '--volume', str(STATE / 'data') + ':/var/lib/arangodb3',
            '--volume', str(STATE / 'apps') + ':/var/lib/arangodb3-apps', IMAGE,
            'arangod', '--rocksdb.block-cache-size', '268435456', '--server.maximal-threads', '8')
        durable(STATE / 'container.ack.json', {'id': container, 'imageId': image['Id']})
    else:
        assert STATE.is_dir() and pending.exists() and not (STATE / 'environment.json').exists()
        intent = json.loads(pending.read_text())
        assert intent == {'image': IMAGE, 'name': NAME, 'port': 18529}
        ack = json.loads((STATE / 'container.ack.json').read_text())
        container = ack['id']
        native = json.loads(command('inspect', container))[0]
        assert native['Id'] == container and native['Image'] == ack['imageId'] == image['Id']
        assert native['State']['Status'] == 'exited' and native['State']['ExitCode'] == 0
        assert native['HostConfig']['PortBindings']['8529/tcp'] == [{'HostIp': '127.0.0.1', 'HostPort': '18529'}]
        assert any(m['Source'] == str(STATE / 'data') and m['Destination'] == '/var/lib/arangodb3' for m in native['Mounts'])
        credentials = STATE / 'credentials.json'
        assert credentials.stat().st_mode & 0o077 == 0
        password = json.loads(credentials.read_text())['password']
    authorization = 'Basic ' + base64.b64encode(('root:' + password).encode()).decode()
    def get(path, authenticated=True):
        request = urllib.request.Request('http://127.0.0.1:18529' + path,
            headers={'Authorization': authorization} if authenticated else {})
        with urllib.request.urlopen(request, timeout=2) as response:
            return json.load(response)
    observations = []
    for phase in ['first', 'restart']:
        command('start', container)
        try:
            for _ in range(120):
                assert json.loads(command('inspect', container))[0]['State']['Running'], 'Owned server exited; retain source/logs'
                try:
                    version = get('/_api/version')
                    if version.get('version') == '3.12.12': break
                except (ConnectionError, urllib.error.URLError): time.sleep(.25)
            else: raise RuntimeError('Owned native readiness deadline')
            native_id = json.loads((STATE / 'data/SERVER').read_text())['serverId']
            assert get('/_api/database')['result'] == ['_system'], 'Unexpected existing business database'
            try: get('/_api/database/current', False)
            except urllib.error.HTTPError as error: assert error.code == 401
            else: raise AssertionError('Protected native endpoint allowed anonymous access')
            observations.append({'phase': phase, 'version': version['version'], 'serverId': native_id, 'anonymous': 401})
        finally:
            command('stop', '--time', '30', container)
            status = json.loads(command('inspect', container))[0]['State']
            assert not status['Running'] and status['ExitCode'] == 0, 'Owned teardown incomplete'
    assert observations[0]['serverId'] == observations[1]['serverId']
    durable(STATE / 'environment.json', {'containerId': container, 'image': IMAGE, 'imageId': image['Id'], 'observations': observations})
    pending.unlink()
    print(json.dumps({'passed': True, 'containerId': container, 'imageId': image['Id'], 'imageBytes': image['Size'],
        'observations': observations, 'stateBytes': sum(p.stat().st_size for p in STATE.rglob('*') if p.is_file()), 'stopped': True}))

if __name__ == '__main__': main()
