"""Create one original authenticated ScyllaDB source; never replay/reset unknown outcomes."""
from pathlib import Path
import argparse, hashlib, shlex, json, os, pwd, re, secrets, shutil, socket, subprocess, time

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


def repair_argument_boundary(container):
    record = json.loads(command('inspect', container))[0]
    receipt = json.loads((STATE/'container.ack.json').read_text())
    assert record['Id'] == receipt['id'] and record['Image'] == receipt['imageId']
    assert not record['State']['Running']
    mount = Path(command('mount', container))
    try:
        path = mount/'scyllasetup.py'
        before = path.read_text()
        old = 'args += ["--cluster-name %s" % self._clusterName]'
        rejected = 'args += ["--cluster-name %s" % shlex.quote(self._clusterName)]'
        original = STATE/'vendor-scyllasetup.original.py'
        patch_receipt = STATE/'vendor-argument-patch.ack.json'
        if patch_receipt.exists():
            receipt = json.loads(patch_receipt.read_text())
            assert receipt['afterSha256'] == hashlib.sha256(before.encode()).hexdigest()
            assert before.count(rejected) == 1 and original.exists()
            saved = STATE/'vendor-scyllasetup.quoting-rejected.py'
            with saved.open('x') as output:
                output.write(before)
            os.chmod(saved, 0o600)
            os.chown(saved, OWNER, OWNER)
            patch_receipt.rename(STATE/'vendor-argument-quote-rejected.ack.json')
            before = before.replace('import shlex\n', '').replace(rejected, old)
        else:
            assert before.count(old) == 1 and not original.exists()
            with original.open('x') as output:
                output.write(before)
                output.flush()
                os.fsync(output.fileno())
            os.chmod(original, 0o600)
            os.chown(original, OWNER, OWNER)
        # The shell consumer word-splits the whole Docker argument string without
        # interpreting embedded quotes. Use the vendor-supported structured config
        # channel for this scalar instead of emitting another lossy argument string.
        replacement = '\n'.join([
            'with open("/etc/scylla/scylla.yaml") as stream:',
            '                configuration = yaml.safe_load(stream)',
            '            configuration["cluster_name"] = self._clusterName',
            '            with open("/etc/scylla/scylla.yaml", "w") as stream:',
            '                yaml.safe_dump(configuration, stream)'])
        after = before.replace(old, replacement)
        compile(after, 'scyllasetup.py', 'exec')
        path.write_text(after)
        durable(patch_receipt, {
            'container': record['Id'], 'beforeSha256': hashlib.sha256(before.encode()).hexdigest(),
            'afterSha256': hashlib.sha256(after.encode()).hexdigest(),
            'scope': '/scyllasetup.py cluster-name YAML serialization only'})
    finally:
        command('unmount', container)


def resume_original():
    assert os.geteuid() == 0 and (STATE/'environment.pending.json').exists()
    assert not (STATE/'environment.json').exists()
    assert not list(STATE.glob('role*')) and not list(STATE.glob('initial*'))
    assert not list(STATE.glob('demo-seed*')) and not (STATE/'source-snapshot.json').exists()
    files = sorted(str(p.relative_to(STATE/'data')) for p in (STATE/'data').rglob('*') if p.is_file())
    assert files == ['.cassandra/cqlshrc'], 'Only original prebootstrap setup is resumable; never reset native data'
    stopped(NAME)
    repair_argument_boundary(NAME)
    password = json.loads((STATE/'credentials.json').read_text())['password']
    verify_native(NAME, password, STATE/'environment.pending.json')


def resume_native_auth_bootstrap():
    assert os.geteuid() == 0 and (STATE/'environment.pending.json').exists()
    assert not (STATE/'environment.json').exists()
    assert not list(STATE.glob('role*')) and not list(STATE.glob('initial*'))
    assert not list(STATE.glob('demo-seed*')) and not (STATE/'source-snapshot.json').exists()
    record = json.loads(command('inspect', NAME))[0]
    receipt = json.loads((STATE/'container.ack.json').read_text())
    assert record['Id'] == receipt['id'] and record['Image'] == receipt['imageId']
    assert (STATE/'vendor-argument-patch.ack.json').exists()
    stopped(NAME)
    password = json.loads((STATE/'credentials.json').read_text())['password']
    verify_native(NAME, password, STATE/'environment.pending.json')


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
        '--publish', '127.0.0.1:19043:9042', '--cpus', '1', '--memory', '3g', '--pids-limit', '512',
        '--volume', str(STATE/'data')+':/var/lib/scylla',
        '--volume', str(STATE/'secrets')+':/run/oasis-secrets:ro', IMAGE,
        '--smp', '1', '--memory', '1G', '--developer-mode', '1', '--overprovisioned', '1',
        '--io-setup', '0', '--disable-version-check', '--authenticator', 'PasswordAuthenticator',
        '--authorizer', 'CassandraAuthorizer', '--cluster-name', 'COSMIC CQL ScyllaDB')
    durable(STATE/'container.ack.json', {'id': container, 'imageId': image['Id']})

    repair_argument_boundary(container)
    verify_native(container, password, pending)


def verify_native(container, password, pending):
    def query(cql, account='client'):
        arguments = ['podman', 'exec', '-i', '--user', '0', container, 'cqlsh',
            '--connect-timeout=5', '--request-timeout=15']
        if account == 'maintenance':
            arguments += ['/var/lib/scylla/cql.m']
        else:
            arguments += ['--cqlshrc=/run/oasis-secrets/client',
                json.loads(command('inspect', container))[0]['NetworkSettings']['IPAddress']]
        completed = subprocess.run(arguments, input=cql+'\n', text=True, capture_output=True, timeout=25)
        if completed.returncode != 0 or re.search(r'(Error|Exception|Unauthorized|InvalidRequest):', completed.stdout+completed.stderr):
            raise RuntimeError('ScyllaDB native query not acknowledged')
        return completed.stdout

    observations = []
    for phase in ['initial', 'restart']:
        command('start', container)
        try:
            deadline = time.monotonic()+180
            account = 'maintenance' if phase == 'initial' else 'client'
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
                # ScyllaDB2026.2+ has no default superuser. Use its documented private
                # maintenance socket to provision the original administrator once.
                roles = query('LIST ROLES;', 'maintenance')
                schemas = query("SELECT keyspace_name FROM system_schema.keyspaces WHERE keyspace_name='oasis';", 'maintenance')
                assert '(0 rows)' in roles and '(0 rows)' in schemas, 'Never adopt an existing role/business source'
                durable(STATE/'initial-native.prebootstrap.ack.json', {'identity': identity, 'roles': roles, 'businessKeyspace': schemas})
                role_pending = STATE/'role.pending.json'
                durable(role_pending, {'role': 'oasis_test', 'action': 'create-superuser-maintenance-socket'})
                query("CREATE ROLE oasis_test WITH PASSWORD='"+password+"' AND LOGIN=true AND SUPERUSER=true;", 'maintenance')
                query('SELECT host_id FROM system.local;')
                role_pending.rename(STATE/'role.ack.json')
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
    parser = argparse.ArgumentParser()
    parser.add_argument('--resume-original-prebootstrap', action='store_true')
    parser.add_argument('--resume-native-auth-bootstrap', action='store_true')
    options = parser.parse_args()
    assert not (options.resume_original_prebootstrap and options.resume_native_auth_bootstrap)
    if options.resume_native_auth_bootstrap:
        resume_native_auth_bootstrap()
    elif options.resume_original_prebootstrap:
        resume_original()
    else:
        main()
