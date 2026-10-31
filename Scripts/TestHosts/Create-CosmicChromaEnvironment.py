"""Provision exactly one retained free Chroma1.5.9 loopback source; never reset/reseed."""
from pathlib import Path
import json, os, pwd, socket, subprocess, time
import urllib.request, urllib.error

STATE = Path('/var/tmp/oasis-chroma-cql')
NAME = 'oasis-chroma-cql'
IMAGE = 'docker.io/chromadb/chroma@sha256:abcce7c335e2dab9f11ef629296f7309b09cb19ae4b34da32ac7e34ff5773140'
IMAGE_ID = 'a221f3391b5eabc5b0b293b65958a28f10d7015be67e4d8cc33a5e4e60e6f3ca'
OWNER = pwd.getpwnam('dellams').pw_uid
BASE = 'http://127.0.0.1:18002'
COLLECTIONS = '/api/v2/tenants/default_tenant/databases/default_database/collections'


def command(*args):
    return subprocess.check_output(['podman', *args], text=True).strip()


def durable(name, value):
    with (STATE/name).open('x') as file:
        os.fchmod(file.fileno(), 0o600)
        json.dump(value, file)
        file.flush()
        os.fsync(file.fileno())
    os.chown(STATE/name, OWNER, OWNER)


def request(path, body=None):
    data = None if body is None else json.dumps(body).encode()
    headers = {} if data is None else {'Content-Type': 'application/json'}
    with urllib.request.urlopen(urllib.request.Request(BASE+path, data=data, headers=headers), timeout=15) as response:
        raw = response.read()
        return json.loads(raw) if raw else None


def ready():
    deadline = time.monotonic()+120
    while time.monotonic() < deadline:
        container = json.loads(command('inspect', NAME))[0]
        assert container['Image'] == IMAGE_ID and container['State']['Running'], 'Native service/identity differs before readiness'
        try:
            # Pinned Rust frontend hard-codes1.0.0; image digest owns release identity.
            assert request('/api/v2/version') == '1.0.0'
            assert request('/api/v2/heartbeat')['nanosecond heartbeat'] > 0
            return
        except (urllib.error.URLError, TimeoutError, ConnectionError):
            time.sleep(.5)
    raise RuntimeError('Native Chroma readiness deadline')


def stop():
    state = json.loads(command('inspect', NAME))[0]['State']
    if state['Running']:
        command('stop', '--time', '60', NAME)
        state = json.loads(command('inspect', NAME))[0]['State']
    assert not state['Running'] and state['ExitCode'] == 0, 'Preserve unclean native shutdown'


def main():
    assert not STATE.exists(), 'Retain existing source and inspect its receipts; do not reprovision'
    image = json.loads(command('image', 'inspect', IMAGE_ID))[0]
    assert image['Id'] == IMAGE_ID
    with socket.socket() as probe:
        probe.bind(('127.0.0.1', 18002))
    STATE.mkdir(mode=0o700)
    os.chown(STATE, OWNER, OWNER)
    (STATE/'data').mkdir(mode=0o700)
    config = STATE/'config.yaml'
    config.write_text(json.dumps({'persist_path': '/data', 'max_payload_size_bytes': 4194304,
        'frontend': {'allow_reset': False}}))
    os.chmod(config, 0o600)
    durable('environment.pending.json', {'image': IMAGE, 'source': str(STATE), 'port': 18002, 'scope': 'loopback plaintext local only'})
    container = command('create', '--name', NAME, '--cpus', '2', '--memory', '2g', '--pids-limit', '512',
        '--log-opt', 'max-size=2mb', '-p', '127.0.0.1:18002:8000',
        '-e', 'ANONYMIZED_TELEMETRY=false', '-e', 'CHROMA_ANONYMIZED_TELEMETRY=false',
        '-v', str(STATE/'data')+':/data', '-v', str(config)+':/config.yaml:ro', IMAGE)
    durable('container.ack.json', {'id': container, 'imageId': IMAGE_ID})
    command('start', NAME)
    try:
        ready()
        assert request(COLLECTIONS) == []
        for name in ['oasis_avatars', 'oasis_avatar_details', 'oasis_holons']:
            durable(name+'.intent.json', {'name': name})
            collection = request(COLLECTIONS, {'name': name, 'get_or_create': False,
                'metadata': {'cosmic_storage_schema': 'chroma-doc-v1', 'cosmic_embedding_dimension': 1}})
            assert collection['name'] == name and collection['dimension'] is None
            durable(name+'.ack.json', collection)
        durable('initial.ack.json', {'release': '1.5.9', 'nativeApiVersion': '1.0.0', 'collections': request(COLLECTIONS)})
    finally:
        stop()
    command('start', NAME)
    try:
        ready()
        original = json.loads((STATE/'initial.ack.json').read_text())['collections']
        actual = request(COLLECTIONS)
        assert sorted(original, key=lambda c:c['id']) == sorted(actual, key=lambda c:c['id'])
        durable('restart.ack.json', {'release': '1.5.9', 'nativeApiVersion': '1.0.0', 'collections': actual})
    finally:
        stop()
    durable('environment.json', {'container': container, 'image': IMAGE, 'sourceDirectoryInode': (STATE/'data').stat().st_ino,
        'release': '1.5.9', 'nativeApiVersion': '1.0.0', 'url': BASE})
    (STATE/'environment.pending.json').rename(STATE/'environment.intent.ack.json')
    print(json.dumps({'passed': True, 'stopped': True, 'collections': [c['id'] for c in actual],
        'stateBytes': sum(f.stat().st_size for f in STATE.rglob('*') if f.is_file())}))


if __name__ == '__main__':
    main()
