"""One retained loopback Convex test backend; no download, reset or secret output."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import secrets
import signal
import socket
import subprocess
import time

ROOT = Path('/root/oasis-runtimes/convex')
BINARY = ROOT / 'convex-local-backend'
OWNER = ROOT / 'owner.json'
STATE = ROOT / 'state'
SECRET = ROOT / 'credentials/instance-secret'
ADMIN = ROOT / 'credentials/admin-key'
EXPECTED = '044eedfe569b1550d5003683aec7fceb4dcee3f82abf00ab9baf1c10d3ca0b4c'


def identity(pid):
    proc = Path('/proc') / str(pid)
    try:
        if (proc / 'exe').resolve() != BINARY:
            return None
        return (proc / 'stat').read_text().rsplit(')', 1)[1].split()[19]
    except FileNotFoundError:
        return None


def current():
    if not OWNER.exists():
        return None
    owned = json.loads(OWNER.read_text())
    if identity(owned['pid']) == owned['startTicks']:
        return owned
    return None


def publish(owned):
    pending = OWNER.with_suffix('.next')
    if pending.exists():
        raise RuntimeError('Preserve unresolved owner publication before another operation')
    with pending.open('x') as output:
        json.dump(owned, output, indent=2)
        output.flush()
        os.fsync(output.fileno())
    pending.replace(OWNER)


def start():
    existing = current()
    if existing:
        return existing
    for port in (13210, 13211):
        with socket.socket() as probe:
            probe.settimeout(0.2)
            if probe.connect_ex(('127.0.0.1', port)) == 0:
                raise RuntimeError('An unregistered listener already owns a Convex test port')
    if hashlib.sha256(BINARY.read_bytes()).hexdigest() != EXPECTED:
        raise RuntimeError('Retained Convex binary changed')
    STATE.mkdir(exist_ok=True)
    SECRET.parent.mkdir(exist_ok=True, mode=0o700)
    if not SECRET.exists():
        with SECRET.open('x') as output:
            output.write(secrets.token_hex(32))
        SECRET.chmod(0o600)
    root_secret = SECRET.read_text().strip()
    if len(root_secret) != 64 or bytes.fromhex(root_secret).hex() != root_secret:
        raise RuntimeError('Preserve invalid instance-secret file for review')
    if not ADMIN.exists():
        generated = subprocess.run([str(BINARY), 'keygen', 'admin-key', '--instance-name', 'oasis-convex',
            '--instance-secret', root_secret], check=True, capture_output=True, text=True).stdout.strip()
        if not generated.startswith('oasis-convex|'):
            raise RuntimeError('Admin-key generation returned an unexpected identity')
        with ADMIN.open('x') as output:
            output.write(generated)
        ADMIN.chmod(0o600)
    log = ROOT / 'backend.log'
    with log.open('ab') as output:
        log.chmod(0o600)
        process = subprocess.Popen([str(BINARY), '--interface', '127.0.0.1', '--port', '13210',
            '--site-proxy-port', '13211', '--convex-origin', 'http://127.0.0.1:13210',
            '--convex-site', 'http://127.0.0.1:13211', '--instance-name', 'oasis-convex',
            '--instance-secret', root_secret, '--disable-beacon', '--redact-logs-to-client',
            '--local-storage', str(STATE / 'storage'), str(STATE / 'convex.sqlite3')],
            cwd=STATE, stdin=subprocess.DEVNULL, stdout=output, stderr=subprocess.STDOUT, start_new_session=True)
    owned = {'pid': process.pid, 'startTicks': identity(process.pid), 'binary': str(BINARY),
        'sha256': EXPECTED, 'ports': [13210, 13211], 'interface': '127.0.0.1', 'state': str(STATE)}
    if owned['startTicks'] is None:
        raise RuntimeError('Backend exited before ownership could be recorded; inspect protected log')
    publish(owned)
    return owned


def stop():
    owned = current()
    if not owned:
        return {'active': False, 'action': 'no matching owned process'}
    os.kill(owned['pid'], signal.SIGTERM)
    deadline = time.monotonic() + 10
    while identity(owned['pid']) is not None and time.monotonic() < deadline:
        time.sleep(0.1)
    if identity(owned['pid']) is not None:
        raise RuntimeError('Owned backend did not terminate; no automatic SIGKILL')
    return {'active': False, 'action': 'owned process terminated; state/credentials retained'}


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('action', choices=['start', 'status', 'stop'])
    action = parser.parse_args().action
    result = start() if action == 'start' else stop() if action == 'stop' else {'active': current()}
    print(json.dumps(result))
