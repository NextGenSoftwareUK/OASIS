"""Create one original authenticated Cassandra source; never replay/reset unknown outcomes."""
from pathlib import Path
import argparse, json, os, pwd, re, secrets, shutil, socket, subprocess, time

STATE = Path('/var/tmp/oasis-cassandra-cql')
IMAGE = 'docker.io/library/cassandra@sha256:57b251cede6ebb8402c9cfc8ee8931438086ce2640a5657e80ecebc9f4cf3bbd'
NAME = 'oasis-cassandra-cql'
OWNER = pwd.getpwnam('dellams').pw_uid


def command(*args):
    return subprocess.check_output(['podman', *args], text=True).strip()


def durable(path, value, owner=OWNER):
    with path.open('x') as output:
        os.fchmod(output.fileno(), 0o600)
        json.dump(value, output)
        output.flush()
        os.fsync(output.fileno())
    os.chown(path, owner, owner)


def stopped(container):
    status = json.loads(command('inspect', container))[0]['State']
    # The JVM runs its shutdown hooks and exits with SIGTERM's 128+15 status.
    assert not status['Running'] and status['ExitCode'] == 143
    assert not status['OOMKilled'] and not status['Error'] and status['StoppedByUser']


def verify_original():
    """Explicit read-only reconciliation after the initial exit-code verifier rejected SIGTERM."""
    assert os.geteuid() == 0
    for name in ['environment.pending.json', 'container.ack.json', 'role.ack.json',
                 'default-role.ack.json', 'initial.ack.json']:
        assert (STATE/name).is_file(), 'Original acknowledged setup required; no bootstrap replay'
    assert not (STATE/'environment.json').exists()
    original = json.loads((STATE/'initial.ack.json').read_text())
    receipt = json.loads((STATE/'container.ack.json').read_text())
    record = json.loads(command('inspect', NAME))[0]
    assert record['Id'] == receipt['id'] and record['Image'] == receipt['imageId']
    stopped(NAME)
    command('start', NAME)
    try:
        deadline = time.monotonic()+180
        while time.monotonic() < deadline:
            assert json.loads(command('inspect', NAME))[0]['State']['Running']
            response = subprocess.run(['podman', 'exec', '--user', '999', NAME, 'cqlsh',
                '--cqlshrc=/run/oasis-secrets/client', '--connect-timeout=5', '--request-timeout=10',
                '-e', 'SELECT host_id, cluster_name, release_version FROM system.local;', '127.0.0.1'],
                text=True, capture_output=True, timeout=20)
            if response.returncode == 0 and original['identity'] == response.stdout:
                break
            time.sleep(1)
        else:
            raise RuntimeError('Original identity/authenticated restart verification deadline')
        observation = {'phase': 'restart', 'identity': response.stdout}
        durable(STATE/'restart.ack.json', observation)
    finally:
        command('stop', '--time', '30', NAME)
        stopped(NAME)
    durable(STATE/'environment.json', {'image': IMAGE, 'container': record['Id'],
        'observations': [original, observation], 'initialVerifierRejectedExpectedSigterm': True})
    (STATE/'environment.pending.json').rename(STATE/'environment.intent.ack.json')
    print(json.dumps({'passed': True, 'originalPreserved': True, 'observations': [original, observation],
        'stopped': True, 'exitCode': 143,
        'stateBytes': sum(p.stat().st_size for p in STATE.rglob('*') if p.is_file())}))


def main():
    assert os.geteuid() == 0
    assert not STATE.exists(), 'Original source or unknown outcome exists; reconcile without recreation'
    assert subprocess.run(['podman', 'container', 'exists', NAME]).returncode == 1
    with socket.socket() as probe:
        assert probe.connect_ex(('127.0.0.1', 19042)) != 0
    image = json.loads(command('image', 'inspect', IMAGE))[0]
    assert image['Id'] == '40516ee152ef61655ef539671a2f59d3bb9ae115a2c8dd4992be227b3ae83b4f'
    STATE.mkdir(mode=0o700)
    os.chown(STATE, OWNER, OWNER)
    pending = STATE/'environment.pending.json'
    durable(pending, {'image': IMAGE, 'name': NAME, 'port': 19042})
    mount = Path(command('image', 'mount', IMAGE))
    try:
        shutil.copytree(mount/'etc/cassandra', STATE/'config')
    finally:
        command('image', 'unmount', IMAGE)
    config = STATE/'config/cassandra.yaml'
    text = config.read_text()
    for key, old, new in [('authenticator', 'AllowAllAuthenticator', 'PasswordAuthenticator'),
                          ('authorizer', 'AllowAllAuthorizer', 'CassandraAuthorizer')]:
        assert text.count(key+': '+old+'\n') == 1
        text = text.replace(key+': '+old+'\n', key+': '+new+'\n')
    config.write_text(text)
    for directory in ['data', 'secrets']:
        (STATE/directory).mkdir(mode=0o700)
    password = secrets.token_hex(32)
    durable(STATE/'credentials.json', {'username': 'oasis_test', 'password': password})
    for name, user, secret in [('bootstrap', 'cassandra', 'cassandra'), ('client', 'oasis_test', password)]:
        path = STATE/'secrets'/name
        path.write_text('[authentication]\nusername = '+user+'\npassword = '+secret+'\n')
        os.chmod(path, 0o600)
    for directory in ['config', 'data', 'secrets']:
        for root, dirs, files in os.walk(STATE/directory):
            os.chown(root, 999, 999)
            for name in files:
                os.chown(Path(root)/name, 999, 999)
    container = command('create', '--name', NAME, '--label', 'oasis.task=cosmic-cql',
        '--publish', '127.0.0.1:19042:9042', '--cpus', '2', '--memory', '2g', '--pids-limit', '256',
        '--env', 'CASSANDRA_CLUSTER_NAME=COSMIC CQL Cassandra', '--env', 'MAX_HEAP_SIZE=512M',
        '--env', 'HEAP_NEWSIZE=128M', '--volume', str(STATE/'data')+':/var/lib/cassandra',
        '--volume', str(STATE/'config')+':/etc/cassandra',
        '--volume', str(STATE/'secrets')+':/run/oasis-secrets:ro', IMAGE)
    durable(STATE/'container.ack.json', {'id': container, 'imageId': image['Id']})

    def query(cql, account='client'):
        completed = subprocess.run(['podman', 'exec', '-i', '--user', '999', container,
            'cqlsh', '--cqlshrc=/run/oasis-secrets/'+account, '--request-timeout=15', '127.0.0.1'],
            input=cql+'\n', text=True, capture_output=True, timeout=25)
        if completed.returncode != 0 or re.search(r'(Error|Exception|Unauthorized|InvalidRequest):', completed.stdout+completed.stderr):
            raise RuntimeError('Cassandra native query not acknowledged')
        return completed.stdout

    observations = []
    for phase in ['initial', 'restart']:
        command('start', container)
        try:
            deadline = time.monotonic()+180
            account = 'bootstrap' if phase == 'initial' else 'client'
            while time.monotonic() < deadline:
                assert json.loads(command('inspect', container))[0]['State']['Running']
                try:
                    identity = query('SELECT host_id, cluster_name, release_version FROM system.local;', account)
                    if re.search(r'[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}', identity):
                        break
                except (RuntimeError, subprocess.TimeoutExpired):
                    time.sleep(1)
            else:
                raise RuntimeError('Native authenticated readiness deadline')
            if phase == 'initial':
                role_pending = STATE/'role.pending.json'
                durable(role_pending, {'role': 'oasis_test', 'action': 'create-superuser'})
                query("CREATE ROLE oasis_test WITH PASSWORD='"+password+"' AND LOGIN=true AND SUPERUSER=true;", 'bootstrap')
                query('SELECT host_id FROM system.local;')
                role_pending.rename(STATE/'role.ack.json')
                drop_pending = STATE/'default-role.pending.json'
                durable(drop_pending, {'role': 'cassandra', 'action': 'drop-default'})
                query('DROP ROLE cassandra;')
                drop_pending.rename(STATE/'default-role.ack.json')
            observation = {'phase': phase, 'identity': query('SELECT host_id, cluster_name, release_version FROM system.local;')}
            assert '4.1.12' in observation['identity']
            if observations:
                assert observation['identity'] == observations[0]['identity']
            observations.append(observation)
            durable(STATE/(phase+'.ack.json'), observation)
        finally:
            command('stop', '--time', '30', container)
            stopped(container)
    durable(STATE/'environment.json', {'image': IMAGE, 'container': container, 'observations': observations})
    pending.unlink()
    print(json.dumps({'passed': True, 'observations': observations, 'stopped': True,
        'stateBytes': sum(p.stat().st_size for p in STATE.rglob('*') if p.is_file())}))


if __name__ == '__main__':
    options = argparse.ArgumentParser()
    options.add_argument('--verify-original', action='store_true')
    arguments = options.parse_args()
    if arguments.verify_original:
        verify_original()
    else:
        main()
