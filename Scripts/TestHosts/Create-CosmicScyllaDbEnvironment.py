"""Create one original authenticated ScyllaDB source; never replay/reset unknown outcomes."""
from pathlib import Path
import argparse, json, os, pwd, re, secrets, shutil, socket, subprocess, time

STATE = Path('/var/tmp/oasis-scylladb-cql')
IMAGE = 'docker.io/scylladb/scylla@sha256:2a3cf99b55d793a41e19b4d8afa4084744bc05517f4b4a211483c8d2abe4ef3a'
NAME = 'oasis-scylladb-cql'
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
    assert not status['Running'] and status['ExitCode'] == 0
    assert not status['OOMKilled'] and not status['Error'] and status['StoppedByUser']


def main():
    assert os.geteuid() == 0
    assert not STATE.exists(), 'Original source or unknown outcome exists; reconcile without recreation'
    assert subprocess.run(['podman', 'container', 'exists', NAME]).returncode == 1
    with socket.socket() as probe:
        assert probe.connect_ex(('127.0.0.1', 19043)) != 0
    image = json.loads(command('image', 'inspect', IMAGE))[0]
    assert image['Id'] == 'ae6d996ec593faef2a0cbdce559de45f8fbaa397309b23c4f461d73047d40bcf'
    STATE.mkdir(mode=0o700)
    os.chown(STATE, OWNER, OWNER)
    pending = STATE/'environment.pending.json'
    durable(pending, {'image': IMAGE, 'name': NAME, 'port': 19043})
    for directory in ['data', 'secrets']:
        (STATE/directory).mkdir(mode=0o700)
    password = secrets.token_hex(32)
    durable(STATE/'credentials.json', {'username': 'oasis_test', 'password': password})
    for name, user, secret in [('bootstrap', 'cassandra', 'cassandra'), ('client', 'oasis_test', password)]:
        path = STATE/'secrets'/name
        path.write_text('[authentication]\nusername = '+user+'\npassword = '+secret+'\n')
        os.chmod(path, 0o600)
    for directory in ['data', 'secrets']:
        for root, dirs, files in os.walk(STATE/directory):
            os.chown(root, 998, 1000)
            for name in files:
                os.chown(Path(root)/name, 998, 1000)
    container = command('create', '--name', NAME, '--label', 'oasis.task=cosmic-cql',
        '--publish', '127.0.0.1:19043:9042', '--cpus', '1', '--memory', '2g', '--pids-limit', '512',
        '--volume', str(STATE/'data')+':/var/lib/scylla',
        '--volume', str(STATE/'secrets')+':/run/oasis-secrets:ro', IMAGE,
        '--smp', '1', '--memory', '1G', '--developer-mode', '1', '--overprovisioned', '1',
        '--io-setup', '0', '--disable-version-check', '--authenticator', 'PasswordAuthenticator',
        '--authorizer', 'CassandraAuthorizer', '--cluster-name', 'COSMIC CQL ScyllaDB')
    durable(STATE/'container.ack.json', {'id': container, 'imageId': image['Id']})

    def query(cql, account='client'):
        completed = subprocess.run(['podman', 'exec', '-i', '--user', '0', container,
            'cqlsh', '--cqlshrc=/run/oasis-secrets/'+account, '--request-timeout=15', '127.0.0.1'],
            input=cql+'\n', text=True, capture_output=True, timeout=25)
        if completed.returncode != 0 or re.search(r'(Error|Exception|Unauthorized|InvalidRequest):', completed.stdout+completed.stderr):
            raise RuntimeError('ScyllaDB native query not acknowledged')
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
            version = command('exec', container, 'scylla', '--version')
            assert version.startswith('2026.3')
            observation['productVersion'] = version
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
    main()
