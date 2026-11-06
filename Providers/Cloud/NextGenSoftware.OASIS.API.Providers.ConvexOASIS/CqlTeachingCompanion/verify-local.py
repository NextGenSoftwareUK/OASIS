import argparse
import hashlib
import http.client
import json
import os
from pathlib import Path
import secrets
import shutil
import socket
import subprocess
import time
import uuid

parser = argparse.ArgumentParser(description='Verify the teaching companion on a disposable Windows Convex backend.')
parser.add_argument('--workspace', type=Path, required=True, help='Dedicated, disposable CLI project directory')
parser.add_argument('--backend', type=Path, required=True)
parser.add_argument('--node', type=Path, required=True)
args = parser.parse_args()
source = Path(__file__).resolve().parent
root = args.workspace.resolve(strict=True)
backend = args.backend.resolve(strict=True)
node = args.node.resolve(strict=True)
cli = (root / 'node_modules/convex/bin/main.js').resolve(strict=True)
if root == source or source in root.parents:
    parser.error('Use a separate disposable workspace')
for relative in ('package.json', 'package-lock.json', 'convex/http.ts', 'convex/schema.ts', 'convex/oasis/holons.ts'):
    target = root / relative
    target.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(source / relative, target)
with backend.open('rb') as file:
    if hashlib.file_digest(file, 'sha256').hexdigest() != '922c3b674cd2bb12209a0d509db1ab2342b4818f9c20572db794bc6601ef42d8':
        raise RuntimeError('Unexpected Convex backend digest')
ports = 54866, 54867
for port in ports:
    with socket.socket() as check:
        check.bind(('127.0.0.1', port))
fixture = root / 'native-fixture'
fixture.mkdir(exist_ok=True)
secret = secrets.token_hex(32)
reader = secrets.token_hex(32)
name = 'cosmic-convex-fixture'
identity = ['--instance-name', name, '--instance-secret', secret]
admin = subprocess.check_output([str(backend), 'keygen', 'admin-key', *identity], cwd=fixture,
                                creationflags=subprocess.CREATE_NO_WINDOW, text=True, timeout=10).strip()
if not admin.startswith(name + '|'):
    raise RuntimeError('Unexpected local Convex key format')
environment = {key: value for key, value in os.environ.items() if not key.startswith('CONVEX_')}
environment.update(CONVEX_SELF_HOSTED_URL=f'http://127.0.0.1:{ports[0]}',
                   CONVEX_SELF_HOSTED_ADMIN_KEY=admin, DO_NOT_TRACK='1')


def run_cli(*args, stdin=None):
    result = subprocess.run([str(node), str(cli), *args], cwd=root, env=environment, input=stdin,
                            stdout=subprocess.PIPE, stderr=subprocess.STDOUT, creationflags=subprocess.CREATE_NO_WINDOW,
                            text=True, timeout=60)
    output = result.stdout
    for value in (secret, reader, admin):
        output = output.replace(value, '[redacted]')
    with (fixture / 'cli.log').open('a', encoding='utf-8') as log:
        log.write(output)
    if result.returncode != 0:
        raise RuntimeError(f'Local Convex CLI step failed ({args[0]})')


def get(port, path, token=None):
    connection = http.client.HTTPConnection('127.0.0.1', port, timeout=2)
    try:
        connection.request('GET', path, headers={} if token is None else {'Authorization': 'Bearer ' + token})
        reply = connection.getresponse()
        return reply.status, reply.read(65536)
    finally:
        connection.close()


with (fixture / 'backend.log').open('wb') as log:
    process = subprocess.Popen([str(backend), '--interface', '127.0.0.1', '--port', str(ports[0]),
                                '--site-proxy-port', str(ports[1]), *identity, 'fixture.sqlite3'],
                               cwd=fixture, env=environment, stdout=log, stderr=subprocess.STDOUT,
                               creationflags=subprocess.CREATE_NO_WINDOW)
    try:
        deadline = time.monotonic() + 30
        while process.poll() is None and time.monotonic() < deadline:
            try:
                if get(ports[0], '/version')[0] == 200:
                    break
            except (ConnectionError, TimeoutError):
                pass
            time.sleep(.3)
        else:
            raise RuntimeError('Owned Convex backend did not become ready')

        run_cli('dev', '--once', '--typecheck', 'try', '--tail-logs', 'disable')
        run_cli('env', 'set', 'OASIS_CQL_READER_TOKEN', '--force', stdin=reader)
        ids = [str(uuid.uuid4()) for _ in range(4)]
        for index, identifier in enumerate(ids[:3]):
            document = {'id': identifier, 'isPublic': index != 1}
            run_cli('run', 'oasis/holons:seedTeachingHolon',
                    json.dumps({'id': identifier, 'isDeleted': index == 2, 'dataJson': json.dumps(document)}))
        route = '/oasis/cql/holon?id='
        public = get(ports[1], route + ids[0], reader)
        if public[0] != 200 or json.loads(public[1]) != {'id': ids[0], 'isPublic': True}:
            raise RuntimeError(f'Convex exact public read failed (HTTP {public[0]})')
        if get(ports[1], route + ids[0])[0] != 401 or get(ports[1], route + ids[1], reader)[0] != 403:
            raise RuntimeError('Convex authorization boundary failed')
        if get(ports[1], route + ids[0], 'invalid-token')[0] != 401:
            raise RuntimeError('Convex rejected-token boundary failed')
        for suffix in ('', 'not-a-guid', '00000000-0000-0000-0000-000000000000',
                       ids[0] + '&id=' + ids[1], ids[0] + '&limit=1'):
            if get(ports[1], route + suffix, reader)[0] != 400:
                raise RuntimeError('Convex exact-key request boundary failed')
        if any(get(ports[1], route + identifier, reader) != (200, b'null') for identifier in ids[2:]):
            raise RuntimeError('Convex deleted or absent record boundary failed')
        (fixture / 'result.log').write_text('PASS: indexed Convex public read, anonymous 401, private 403, tombstone and absent null.\n')
        print('PASS: native Convex public, private, tombstone and absent records.')
    finally:
        if process.poll() is None:
            process.terminate()
            process.wait(15)
