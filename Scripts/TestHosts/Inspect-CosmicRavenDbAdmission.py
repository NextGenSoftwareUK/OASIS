"""Read-only reconciliation of the original server after an unacknowledged database request."""
from pathlib import Path
import json, ssl, subprocess, time, urllib.error, urllib.request

state = Path('/var/tmp/oasis-ravendb-cql')
ack = json.loads((state / 'container.ack.json').read_text())
def command(*args): return subprocess.check_output(['podman', *args], text=True).strip()
native = json.loads(command('inspect', ack['id']))[0]
assert native['Image'] == ack['imageId'] and native['State']['Status'] == 'exited' and native['State']['ExitCode'] == 0
assert (state / 'database.pending.json').exists() and not (state / 'database.ack.json').exists()
context = ssl.create_default_context(cafile=str(state / 'ca.pem'))
context.load_cert_chain(str(state / 'client.pem'), str(state / 'client.key'))
def get(path):
    try:
        with urllib.request.urlopen('https://localhost:18003' + path, context=context, timeout=3) as response:
            return json.load(response)
    except urllib.error.HTTPError as error:
        payload = json.load(error)
        return {'httpStatus': error.code, 'message': str(payload.get('Message', payload.get('Error', '')))[:1500]}
command('start', ack['id'])
try:
    deadline = time.monotonic() + 60
    while time.monotonic() < deadline:
        assert json.loads(command('inspect', ack['id']))[0]['State']['Running']
        try:
            version = get('/build/version'); break
        except urllib.error.URLError as error:
            if isinstance(error.reason, ssl.SSLError): raise
            time.sleep(.5)
    else: raise RuntimeError('Read-only readiness deadline')
    node = get('/cluster/node-info')
    record = get('/admin/databases?name=oasis')
    print(json.dumps({'version': version, 'databases': get('/databases?start=0&pageSize=10&namesOnly=true'),
        'node': {key: node.get(key) for key in ['NodeTag', 'TopologyId', 'ServerId', 'CurrentState']},
        'record': {key: record.get(key) for key in ['DatabaseName', 'Topology', 'Encrypted', 'Settings', 'httpStatus', 'message']},
        'stats': get('/databases/oasis/stats'), 'licenseType': get('/license/status').get('Type')}))

finally:
    command('stop', '--time', '30', ack['id'])
    terminal = json.loads(command('inspect', ack['id']))[0]['State']
    assert not terminal['Running'] and terminal['ExitCode'] == 0
