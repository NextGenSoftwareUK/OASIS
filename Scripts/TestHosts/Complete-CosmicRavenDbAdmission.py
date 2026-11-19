"""Reconcile original database creation by native read-back, without replaying a write."""
from pathlib import Path
import json, os, ssl, subprocess, time, urllib.error, urllib.request

STATE = Path('/var/tmp/oasis-ravendb-cql')
assert STATE.resolve() == STATE and os.geteuid() == 0
assert not (STATE / 'environment.json').exists() and not (STATE / 'database.ack.json').exists()
original = json.loads((STATE / 'database.pending.json').read_text())
assert original['database'] == 'oasis' and original['operationId']
ack = json.loads((STATE / 'container.ack.json').read_text())
def command(*args): return subprocess.check_output(['podman', *args], text=True).strip()
def inspect():
    native = json.loads(command('inspect', ack['id']))[0]
    assert native['Id'] == ack['id'] and native['Image'] == ack['imageId'] == '933a130ff10d66a6a28b8829c5f6f7af886162fc2425b4012a9f41649072816e'
    assert native['HostConfig']['PortBindings']['8080/tcp'] == [{'HostIp': '127.0.0.1', 'HostPort': '18003'}]
    assert any(m['Source'] == str(STATE / 'data') and m['Destination'] == '/var/lib/ravendb/data' for m in native['Mounts'])
    return native
assert inspect()['State']['Status'] == 'exited' and inspect()['State']['ExitCode'] == 0
context = ssl.create_default_context(cafile=str(STATE / 'ca.pem'))
context.load_cert_chain(str(STATE / 'client.pem'), str(STATE / 'client.key'))
def get(path, certificate=True):
    trust = context if certificate else ssl.create_default_context(cafile=str(STATE / 'ca.pem'))
    with urllib.request.urlopen('https://localhost:18003' + path, context=trust, timeout=3) as response:
        return json.load(response)
observations = []
for phase in ['readback', 'restart']:
    command('start', ack['id'])
    try:
        deadline = time.monotonic() + 60
        while time.monotonic() < deadline:
            assert inspect()['State']['Running']
            try:
                version = get('/build/version'); break
            except urllib.error.URLError as error:
                if isinstance(error.reason, ssl.SSLError): raise
                time.sleep(.5)
        else: raise RuntimeError('Native read-back readiness deadline')
        assert version['FullVersion'] == '7.2.7'
        assert get('/databases?start=0&pageSize=10&namesOnly=true')['Databases'] == ['oasis']
        record = get('/admin/databases?name=oasis')
        assert record['DatabaseName'] == original['database'] and record['Settings'] == {} and not record['Encrypted']
        assert record['Topology']['Members'] == ['A'] and record['Topology']['ReplicationFactor'] == 1
        assert record['Topology']['Stamp']['Index'] == 3, 'Unexpected later database definition; refuse adoption'
        stats = get('/databases/oasis/stats')
        assert stats['CountOfDocuments'] == stats['CountOfRevisionDocuments'] == stats['CountOfTombstones'] == 0
        assert stats['LastDocEtag'] == 0 and stats['DatabaseId']
        node = get('/cluster/node-info')
        try: get('/admin/databases?name=oasis', False)
        except urllib.error.HTTPError as error: assert error.code in [401, 403]
        else: raise AssertionError('Administration accepted no client certificate')
        observations.append({'phase': phase, 'version': version['FullVersion'], 'databaseId': stats['DatabaseId'],
            'serverId': node['ServerId'], 'topologyId': node['TopologyId'], 'documents': 0, 'anonymousRejected': True})
    finally:
        command('stop', '--time', '30', ack['id'])
        assert not inspect()['State']['Running'] and inspect()['State']['ExitCode'] == 0
for field in ['databaseId', 'serverId', 'topologyId']:
    assert observations[0][field] == observations[1][field]
def durable(path, value):
    with path.open('x') as file:
        os.fchmod(file.fileno(), 0o600); json.dump(value, file); file.flush(); os.fsync(file.fileno())
    os.chown(path, STATE.stat().st_uid, STATE.stat().st_gid)
durable(STATE / 'database.ack.json', {'acknowledgement': 'NativeReadBackAfterHttp500',
    'originalOperationId': original['operationId'], 'databaseId': observations[0]['databaseId'], 'databaseDefinitionIndex': 3,
    'noRequestReplayed': True})
environment = {'containerId': ack['id'], 'imageId': ack['imageId'],
    'image': 'docker.io/ravendb/ravendb@sha256:92bd726e9d0031f48c8203c9be660c9f9b361ff11b6b352a17ca4a861e366471',
    'observations': observations}
durable(STATE / 'environment.json', environment)
(STATE / 'database.pending.json').unlink(); (STATE / 'environment.pending.json').unlink()
print(json.dumps({'passed': True, **environment, 'noRequestReplayed': True, 'stopped': True,
    'stateBytes': sum(p.stat().st_size for p in STATE.rglob('*') if p.is_file())}))
