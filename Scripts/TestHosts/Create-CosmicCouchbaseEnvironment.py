"""Create one free Community source; journal mutations and preserve unknown outcomes.

Runs as WSL root. Credentials never enter arguments or logs. This local demo uses
authenticated plaintext services published on host loopback only, not hosted TLS.
"""
from pathlib import Path
import argparse, base64, json, os, pwd, secrets, socket, subprocess, time
import urllib.error, urllib.parse, urllib.request

STATE = Path('/var/tmp/oasis-couchbase-cql')
IMAGE = 'docker.io/library/couchbase@sha256:f04bacbb63c6214109a25e9345bc09267ba7ad5b48505b4868261b51f9d1d45c'
IMAGE_ID = '0c21d4c459e1ed4c5f1c0f10640f033658600bee2050cec58e449c171734c41a'
NAME = 'oasis-couchbase-cql'
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


def request(path, credentials=None, form=None):
    headers = {}
    if credentials:
        headers['Authorization'] = 'Basic '+base64.b64encode(
            (credentials['username']+':'+credentials['password']).encode()).decode()
    body = urllib.parse.urlencode(form).encode() if form is not None else None
    with urllib.request.urlopen(urllib.request.Request('http://127.0.0.1:8091'+path,
            data=body, headers=headers), timeout=15) as response:
        data = response.read()
        return json.loads(data) if data else None


def ready(credentials=None):
    deadline = time.monotonic()+180
    while time.monotonic() < deadline:
        assert json.loads(command('inspect', NAME))[0]['State']['Running'], 'Server stopped before readiness'
        try:
            pools = request('/pools', credentials)
            if credentials is None or pools.get('pools'):
                return pools
        except (urllib.error.URLError, TimeoutError, ConnectionError):
            time.sleep(.5)
    raise RuntimeError('Couchbase readiness deadline')


def observe(credentials):
    pools = ready(credentials)
    assert pools['isEnterprise'] is False and pools['implementationVersion'].startswith('8.0.2-')
    deadline = time.monotonic()+180
    while True:
        pool = request('/pools/default', credentials)
        if len(pool['nodes']) == 1 and pool['nodes'][0]['status'] == 'healthy':
            break
        assert time.monotonic() < deadline, 'Native node health readiness deadline'
        time.sleep(.5)
    node = request('/nodes/self', credentials)
    bucket = request('/pools/default/buckets/oasis', credentials)
    if (STATE/'cluster-init.ack.json').exists():
        assert pools['uuid'] == json.loads((STATE/'cluster-init.ack.json').read_text())['clusterUuid']
    if (STATE/'bucket-create.ack.json').exists():
        assert bucket['uuid'] == json.loads((STATE/'bucket-create.ack.json').read_text())['uuid']
    assert len(pool['nodes']) == 1 and pool['nodes'][0]['status'] == 'healthy'
    assert sorted(node['services']) == ['index', 'kv', 'n1ql']
    assert bucket['name'] == 'oasis' and bucket['basicStats']['itemCount'] == 0
    try:
        request('/pools/default/buckets/oasis')
    except urllib.error.HTTPError as failure:
        assert failure.code == 401
    else:
        raise RuntimeError('Unauthenticated bucket access succeeded')
    return {'clusterUuid': pools['uuid'], 'node': node['otpNode'],
        'bucketUuid': bucket['uuid'], 'version': pools['implementationVersion'],
        'community': True, 'services': sorted(node['services'])}


def stop():
    command('stop', '--time', '60', NAME)
    state = json.loads(command('inspect', NAME))[0]['State']
    assert not state['Running'] and not state['OOMKilled'] and not state['Error']
    assert state['ExitCode'] in [0, 143], 'Unclean owned service stop; preserve original state'
    return state['ExitCode']


def close_acknowledged_intents():
    # Only acknowledge native mutations already proven by the retained receipt
    # and both original-source observations. This never performs native writes.
    environment = json.loads((STATE/'environment.json').read_text())
    identity = environment['identity']
    assert identity == json.loads((STATE/'initial.ack.json').read_text())
    assert identity == json.loads((STATE/'restart.ack.json').read_text())
    assert identity['clusterUuid'] == json.loads((STATE/'cluster-init.ack.json').read_text())['clusterUuid']
    assert identity['bucketUuid'] == json.loads((STATE/'bucket-create.ack.json').read_text())['uuid']
    record = json.loads(command('inspect', NAME))[0]
    assert record['Id'] == environment['container'] and record['Image'] == IMAGE_ID
    assert not record['State']['Running'] and not record['State']['OOMKilled']
    for stem in ['cluster-init', 'bucket-create']:
        pending = STATE/(stem+'.pending.json')
        acknowledged = STATE/(stem+'.intent.ack.json')
        assert pending.exists() and not acknowledged.exists()
        pending.rename(acknowledged)


def verify_original():
    assert os.geteuid() == 0 and (STATE/'environment.pending.json').exists()
    assert not (STATE/'environment.json').exists() and not (STATE/'initial.ack.json').exists()
    assert not list(STATE.glob('demo-seed*')), 'No business writes during environment recovery'
    record = json.loads(command('inspect', NAME))[0]
    receipt = json.loads((STATE/'container.ack.json').read_text())
    assert record['Id'] == receipt['id'] and record['Image'] == receipt['imageId'] == IMAGE_ID
    assert not record['State']['Running'] and not record['State']['OOMKilled']
    assert (STATE/'cluster-init.ack.json').exists() and (STATE/'bucket-create.ack.json').exists()
    credentials = json.loads((STATE/'credentials.json').read_text())
    observations = []
    exits = []
    for phase in ['initial', 'restart']:
        command('start', NAME)
        try:
            current = observe(credentials)
            if observations:
                assert current == observations[0]
            durable(phase+'.ack.json', current)
            observations.append(current)
        finally:
            exits.append(stop())
    durable('environment.json', {'container': receipt['id'], 'image': IMAGE,
        'identity': observations[0], 'stopExitCodes': exits})
    (STATE/'environment.pending.json').rename(STATE/'environment.intent.ack.json')
    close_acknowledged_intents()
    print(json.dumps({'passed': True, 'identity': observations[0], 'stopped': True,
        'stopExitCodes': exits, 'stateBytes': sum(p.stat().st_size for p in STATE.rglob('*') if p.is_file())}))


def main():
    assert os.geteuid() == 0 and not STATE.exists(), 'Preserve existing source/unknown outcome'
    assert subprocess.run(['podman', 'container', 'exists', NAME]).returncode == 1
    for port in [8091, 8093, 11210]:
        with socket.socket() as probe:
            assert probe.connect_ex(('127.0.0.1', port)) != 0, 'Native port occupied'
    image = json.loads(command('image', 'inspect', IMAGE))[0]
    assert image['Id'] == IMAGE_ID
    STATE.mkdir(mode=0o700)
    os.chown(STATE, OWNER, OWNER)
    durable('environment.pending.json', {'image': IMAGE, 'name': NAME, 'ports': [8091, 8093, 11210]})
    (STATE/'data').mkdir(mode=0o700)
    os.chown(STATE/'data', 1000, 1000)
    credentials = {'username': 'oasis_test', 'password': secrets.token_hex(32)}
    durable('credentials.json', credentials)
    container = command('create', '--name', NAME, '--label', 'oasis.task=cosmic-cql',
        '--publish', '127.0.0.1:8091:8091', '--publish', '127.0.0.1:8093:8093',
        '--publish', '127.0.0.1:11210:11210', '--cpus', '2', '--memory', '2g',
        '--pids-limit', '1024', '--volume', str(STATE/'data')+':/opt/couchbase/var', IMAGE)
    durable('container.ack.json', {'id': container, 'imageId': image['Id']})
    command('start', NAME)
    try:
        pools = ready()
        assert not pools['pools'] and pools['isEnterprise'] is False
        durable('cluster-init.pending.json', {'services': ['kv', 'n1ql', 'index'],
            'hostname': '127.0.0.1', 'memoryQuota': 256, 'indexMemoryQuota': 256,
            'indexerStorageMode': 'forestdb', 'community': True})
        # Mutate exactly once. A timeout leaves the intent for explicit readback,
        # never a second cluster-init request or replacement source.
        request('/clusterInit', form={**credentials, 'hostname': '127.0.0.1',
            'clusterName': 'COSMIC-CQL-Couchbase', 'services': 'kv,n1ql,index',
            'memoryQuota': 256, 'indexMemoryQuota': 256,
            'indexerStorageMode': 'forestdb', 'port': 'SAME'})
        pools = ready(credentials)
        durable('cluster-init.ack.json', {'clusterUuid': pools['uuid']})
        durable('bucket-create.pending.json', {'name': 'oasis', 'ramQuotaMB': 128, 'replicas': 0})
        request('/pools/default/buckets', credentials, {'name': 'oasis', 'ramQuotaMB': 128,
            'bucketType': 'couchbase', 'replicaNumber': 0, 'flushEnabled': 0})
        bucket = request('/pools/default/buckets/oasis', credentials)
        durable('bucket-create.ack.json', {'uuid': bucket['uuid'], 'name': bucket['name']})
        original = observe(credentials)
        durable('initial.ack.json', original)
    finally:
        first_exit = stop()
    command('start', NAME)
    try:
        restarted = observe(credentials)
        assert restarted == original, 'Original cluster/node/bucket changed across restart'
        durable('restart.ack.json', restarted)
    finally:
        second_exit = stop()
    durable('environment.json', {'container': container, 'image': IMAGE,
        'identity': original, 'stopExitCodes': [first_exit, second_exit]})
    (STATE/'environment.pending.json').rename(STATE/'environment.intent.ack.json')
    close_acknowledged_intents()
    print(json.dumps({'passed': True, 'identity': original, 'stopped': True,
        'stopExitCodes': [first_exit, second_exit],
        'stateBytes': sum(p.stat().st_size for p in STATE.rglob('*') if p.is_file())}))


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--verify-original', action='store_true')
    options = parser.parse_args()
    if options.verify_original:
        verify_original()
    else:
        main()
