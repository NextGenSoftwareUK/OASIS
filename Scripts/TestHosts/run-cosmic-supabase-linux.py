"""Own a reusable JWT-authenticated PostgREST data-plane fixture on retained PostgreSQL."""
import argparse
import sys
import base64
import fcntl
import hashlib
import hmac
import json
import os
from pathlib import Path
import secrets
import signal
import socket
import subprocess
import time
import urllib.error
import urllib.request
import uuid
import xml.etree.ElementTree as ET

parser = argparse.ArgumentParser()
parser.add_argument('--federated', action='store_true')
parser.add_argument('--serve', action='store_true')
options = parser.parse_args()
if options.serve and not options.federated: raise RuntimeError('Serve requires federation')
sys.dont_write_bytecode = True
from cosmic_linux_mongo_service import owned_linux_mongo
ROOT = Path('/mnt/c/Source/OASIS-cosmic')
STATE = Path('/var/tmp/oasis-supabase-cql')
RESULTS = ROOT / 'TestResults/SupabaseCqlCurrent'
DAEMON = Path('/var/tmp/oasis-runtimes/postgrest-16.5/postgrest')
DOTNET = '/var/tmp/oasis-runtimes/dotnet-10.0.12/dotnet'
RUNNER = '/mnt/c/Users/david/.nuget/packages/microsoft.testplatform.portable/18.11.0/tools/net8.0/vstest.console.dll'
ASSEMBLY = ROOT / 'Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.SupabaseOASIS.IntegrationTests/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Providers.SupabaseOASIS.IntegrationTests.dll'
assert os.geteuid() == 0, 'Use WSL root for the existing rootful Podman resource; do not clone it'
assert hashlib.sha256(DAEMON.read_bytes()).hexdigest() == 'cc218dc89e882ae2b828b3e8d08a1cffd29f4fa126a0869b038ac9e3a12c357d'
STATE.mkdir(mode=0o700, exist_ok=True)
RESULTS.mkdir(parents=True, exist_ok=True)

def durable(path, value):
    fd = os.open(path, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o600)
    with os.fdopen(fd, 'w') as output:
        output.write(value if isinstance(value, str) else json.dumps(value, indent=2))
        output.flush(); os.fsync(output.fileno())

def command(args, data=None):
    result = subprocess.run(args, input=data, text=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    if result.returncode:
        # Do not echo SQL/connection configuration or generated secrets.
        raise RuntimeError('Owned command failed: ' + args[0] + '; inspect retained intent, no replay')
    return result.stdout.strip()

def inspect():
    value = json.loads(command(['podman', 'inspect', 'oasis-postgres']))[0]
    assert value['Id'] == 'cee58d44cc44aace8863ca53ffa955deed78a710a9800b750a484bb478206a69'
    assert value['Image'] == '79bd7c99e923138f136f8009d6bffa66e21e9d4fda5c0c561b00fc9c90cfe537'
    assert value['HostConfig']['PortBindings']['5432/tcp'] == [{'HostIp': '127.0.0.1', 'HostPort': '15432'}]
    assert any(v.get('Name') == 'oasis-postgres-data' and v['Destination'] == '/var/lib/postgresql/data' for v in value['Mounts'])
    return value

def sql(text, database='oasis_provider_test'):
    return command(['podman', 'exec', '-i', 'oasis-postgres', 'psql', '-X', '-qAt', '-v', 'ON_ERROR_STOP=1', '-U', 'oasis_test', '-d', database], text)

def start_database():
    assert inspect()['State']['Status'] == 'exited'
    command(['podman', 'start', 'oasis-postgres'])
    deadline = time.monotonic() + 30
    while True:
        value = inspect()
        if value['State'].get('Health', {}).get('Status') == 'healthy': break
        assert value['State']['Status'] == 'running'
        if time.monotonic() > deadline: raise RuntimeError('PostgreSQL readiness deadline; do not restart')
        time.sleep(.2)

def stop_database():
    assert inspect()['State']['Status'] == 'running'
    command(['podman', 'stop', '--time', '20', 'oasis-postgres'])
    assert inspect()['State']['Status'] == 'exited'

def request(path, token=None):
    headers = {} if token is None else {'Authorization': 'Bearer ' + token}
    with urllib.request.urlopen(urllib.request.Request('http://127.0.0.1:18091' + path, headers=headers), timeout=10) as response:
        return json.load(response)

def source_hash():
    text = sql("SELECT coalesce(json_agg(t ORDER BY id)::text, '[]') FROM public.oasis_holons t;", 'cosmic_supabase_demo')
    rows = json.loads(text)
    identity = json.loads((STATE / 'identity.json').read_text())
    assert len(rows) == 4 and {v['id'] for v in rows} == set(identity['ids'])
    assert sum(v['is_deleted'] for v in rows) == 1
    return hashlib.sha256(json.dumps(rows, sort_keys=True, separators=(',', ':')).encode()).hexdigest()

def report(name, seed, test="SupabaseNativeCqlTests", extra=None):
    env = dict(os.environ, DOTNET_ROOT='/var/tmp/oasis-runtimes/dotnet-10.0.12', COSMIC_SUPABASE_ROOT=str(STATE), COSMIC_SUPABASE_SEED='1' if seed else '0')
    env.update(extra or {})
    started = time.time()
    with (RESULTS / (name + '.log')).open('w') as output:
        subprocess.run([DOTNET, RUNNER, str(ASSEMBLY), '--TestCaseFilter:FullyQualifiedName~' + test,
                        '--ResultsDirectory:' + str(RESULTS), '--Logger:trx;LogFileName=' + name + '.trx'],
                       env=env, stdout=output, stderr=subprocess.STDOUT, check=True)
    path = RESULTS / (name + '.trx')
    assert path.stat().st_mtime >= started
    count = ET.parse(path).find('.//{*}Counters').attrib
    assert count['total'] == count['passed'] == '1' and count['notExecuted'] == '0'
    return hashlib.sha256(path.read_bytes()).hexdigest()

with (STATE / 'runner.lock').open('a') as lock:
    fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
    if list(STATE.glob('*.pending')) or (STATE / 'mongo.pending.json').exists() or (STATE / 'active-process.json').exists():
        raise RuntimeError('Unresolved original outcome; preserve without replay')
    with socket.socket() as check:
        assert check.connect_ex(('127.0.0.1', 18091)) != 0
    fresh = not (STATE / 'identity.json').exists()
    if not fresh: assert (STATE / 'native-baseline.json').exists(), 'Original verification incomplete; no reseed'
    assert inspect()['State']['Status'] == 'exited', 'Do not adopt another session service'
    proofs = []
    for iteration in range(2):
        start_database()
        try:
            if fresh and iteration == 0:
                assert sql("SELECT count(*) FROM pg_database WHERE datname='cosmic_supabase_demo';") == '0'
                assert sql("SELECT count(*) FROM pg_roles WHERE rolname IN ('cosmic_supabase_login','cosmic_supabase_api');") == '0'
                durable(STATE / 'setup.pending', {'database': 'cosmic_supabase_demo', 'roles': ['cosmic_supabase_login', 'cosmic_supabase_api']})
                durable(STATE / 'identity.json', {'actor': str(uuid.uuid4()), 'ids': [str(uuid.uuid4()) for _ in range(4)], 'missing': str(uuid.uuid4())})
                durable(STATE / 'database-password', secrets.token_urlsafe(36))
                durable(STATE / 'jwt-secret', secrets.token_urlsafe(48))
                def b64(value): return base64.urlsafe_b64encode(value).decode().rstrip('=')
                encoded = b64(b'{"alg":"HS256","typ":"JWT"}') + '.' + b64(json.dumps({'role': 'cosmic_supabase_api', 'exp': 2208988800}).encode())
                signature = hmac.new((STATE / 'jwt-secret').read_bytes(), encoded.encode(), hashlib.sha256).digest()
                durable(STATE / 'api-token', encoded + '.' + b64(signature))
                password = (STATE / 'database-password').read_text()
                sql("BEGIN; CREATE ROLE cosmic_supabase_api NOLOGIN; CREATE ROLE cosmic_supabase_login LOGIN NOINHERIT PASSWORD '" + password + "'; GRANT cosmic_supabase_api TO cosmic_supabase_login; COMMIT;")
                sql('CREATE DATABASE cosmic_supabase_demo OWNER oasis_test;')
                sql('''BEGIN;
                    REVOKE CONNECT ON DATABASE cosmic_supabase_demo FROM PUBLIC;
                    GRANT CONNECT ON DATABASE cosmic_supabase_demo TO cosmic_supabase_login;
                    REVOKE ALL ON SCHEMA public FROM PUBLIC;
                    GRANT USAGE ON SCHEMA public TO cosmic_supabase_api;
                    CREATE TABLE public.oasis_holons(id uuid PRIMARY KEY, parent_holon_id uuid, holon_type integer NOT NULL,
                        is_deleted boolean NOT NULL DEFAULT false, data_json text NOT NULL);
                    GRANT SELECT,INSERT,UPDATE ON public.oasis_holons TO cosmic_supabase_api;
                    COMMIT;''', 'cosmic_supabase_demo')
                (STATE / 'setup.pending').rename(STATE / 'setup.ack')
                durable(STATE / 'seed.pending', {'records': 4})
            else:
                assert source_hash() == json.loads((STATE / 'native-baseline.json').read_text())['sha256']
            database_bytes = int(sql("SELECT pg_database_size('cosmic_supabase_demo');"))
            env = dict(os.environ, PGRST_DB_URI='postgresql://cosmic_supabase_login:' + (STATE / 'database-password').read_text() + '@127.0.0.1:15432/cosmic_supabase_demo',
                       PGRST_DB_SCHEMAS='public', PGRST_DB_EXTRA_SEARCH_PATH='', PGRST_DB_CONFIG='false', PGRST_DB_POOL='2',
                       PGRST_SERVER_HOST='127.0.0.1', PGRST_SERVER_PORT='18091', PGRST_JWT_SECRET=(STATE / 'jwt-secret').read_text())
            with (RESULTS / ('server-' + str(iteration) + '.log')).open('w') as output:
                process = subprocess.Popen([str(DAEMON), '+RTS', '-N2', '-RTS'], env=env, stdout=output, stderr=subprocess.STDOUT)
                durable(STATE / 'active-process.json', {'pid': process.pid, 'runtime': str(DAEMON)})
                try:
                    token = (STATE / 'api-token').read_text()
                    deadline = time.monotonic() + 20
                    while True:
                        if process.poll() is not None: raise RuntimeError('PostgREST exited before readiness')
                        try:
                            assert request('/oasis_holons?limit=0', token) == []
                            break
                        except (OSError, ValueError):
                            if time.monotonic() >= deadline: raise
                            time.sleep(.1)
                    parts = token.split('.')
                    invalid = '.'.join(parts[:2]) + '.' + ('a' if parts[2][0] != 'a' else 'b') + parts[2][1:]
                    for rejected in [None, invalid]:
                        try: request('/oasis_holons?limit=0', rejected)
                        except urllib.error.HTTPError as error: assert error.code == 401
                        else: raise RuntimeError('Unauthenticated/invalid JWT unexpectedly admitted')
                    proofs.append(report('native' if iteration == 0 else 'restart', fresh and iteration == 0))
                    if iteration == 1 and options.federated:
                        # Launch/stop the reused Mongo as its original data owner,
                        # avoiding root-owned files in another user's retained store.
                        owner = Path('/var/tmp/oasis-cosmic-mongo/data').stat().st_uid
                        assert owner != 0
                        os.setresuid(owner, owner, 0)
                        try:
                            with owned_linux_mongo(RESULTS) as mongo_env:
                                os.setresuid(0, 0, 0)
                                try:
                                    proofs.append(report('mongo-init', False, 'LinuxMongoDemoEnvironmentTests', mongo_env))
                                    pair_env = dict(mongo_env, COSMIC_SUPABASE_MONGO_SERVE='1' if options.serve else '0')
                                    proofs.append(report('mongo-federation', False, 'SupabaseMongoCqlNativeTests', pair_env))
                                    assert not (STATE / 'mongo.pending.json').exists()
                                finally: os.setresuid(owner, owner, 0)
                        finally: os.setresuid(0, 0, 0)
                    digest = source_hash()
                    if fresh and iteration == 0:
                        durable(STATE / 'native-baseline.json', {'sha256': digest, 'records': 4, 'live': 3, 'deleted': 1})
                        (STATE / 'seed.pending').rename(STATE / 'seed.ack')
                    else: assert digest == json.loads((STATE / 'native-baseline.json').read_text())['sha256']
                finally:
                    if process.poll() is None:
                        process.send_signal(signal.SIGTERM); process.wait(timeout=20)
                    assert process.returncode in [0, -15], 'Unexpected REST shutdown; retain process receipt'
                    (STATE / 'active-process.json').unlink()
        finally:
            stop_database()
    print(json.dumps({'nativePassed': 1, 'restartPassed': 1, 'records': 4, 'sourceHash': digest, 'trxHashes': proofs,
                      'databaseBytes': database_bytes, 'stateBytes': sum(p.stat().st_size for p in STATE.rglob('*') if p.is_file()), 'servicesStopped': True}))
