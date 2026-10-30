"""Create one original certificate-authenticated single-node development source."""
from pathlib import Path
import argparse, re, csv, hashlib, io, json, os, signal, socket, subprocess, time

STATE = Path('/var/tmp/oasis-cockroachdb-cql')
RUNTIME = Path('/var/tmp/oasis-runtimes/cockroach-26.2.9/cockroach')
HASH = '7e2a7a869f52c083343dcefc01852a3f34f15292c0a4cc63895182296d2d778f'
RESULTS = Path(__file__).resolve().parents[2] / 'TestResults/CockroachDbCqlCurrent'

def durable(path, value):
    with path.open('x') as file:
        os.fchmod(file.fileno(), 0o600); json.dump(value, file); file.flush(); os.fsync(file.fileno())

def sql(statement, database='defaultdb'):
    return subprocess.check_output([str(RUNTIME), 'sql', '--host=localhost:26259', '--user=root',
        '--certs-dir=' + str(STATE / 'certs'), '--database=' + database,
        '--format=tsv', '--execute=' + statement], text=True, stderr=subprocess.PIPE)

def one(statement, database='defaultdb'):
    rows = list(csv.reader(io.StringIO(sql(statement, database)), delimiter='\t'))
    assert len(rows) == 2 and len(rows[1]) == 1, 'Unexpected native scalar acknowledgement'
    return rows[1][0]

def main():
    assert os.getuid() != 0
    assert hashlib.sha256(RUNTIME.read_bytes()).hexdigest() == HASH
    parser = argparse.ArgumentParser()
    parser.add_argument('--resume', action='store_true')
    options = parser.parse_args()
    pending = STATE / 'environment.pending.json'
    if not options.resume:
        assert not STATE.exists(), 'Original state exists; explicit validated resume required'
        STATE.mkdir(mode=0o700); (STATE / 'certs').mkdir(mode=0o700)
        pending = STATE / 'environment.pending.json'
        durable(pending, {'runtimeSha256': HASH, 'sqlPort': 26259, 'httpPort': 18004})
        for arguments in [
            ['create-ca'], ['create-node', 'localhost', '127.0.0.1'], ['create-client', 'root']
        ]:
            subprocess.run([str(RUNTIME), 'cert', *arguments, '--certs-dir=' + str(STATE / 'certs'),
                '--ca-key=' + str(STATE / 'ca.key')], check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    else:
        assert STATE.resolve()==STATE and STATE.stat().st_uid==os.getuid()
        assert pending.exists() and not (STATE/'environment.json').exists()
        assert json.loads(pending.read_text())=={'runtimeSha256': HASH, 'sqlPort': 26259, 'httpPort': 18004}
        assert not (STATE/'database.pending.json').exists() and not (STATE/'database.ack.json').exists()
        assert all((STATE/'certs'/name).is_file() for name in ['ca.crt','node.crt','node.key','client.root.crt','client.root.key'])
    RESULTS.mkdir(parents=True, exist_ok=True)
    observations = []
    for phase in ['first', 'restart']:
        for port in [26259, 18004]:
            with socket.socket() as probe: assert probe.connect_ex(('127.0.0.1', port)) != 0
        with (RESULTS / (phase + ('-resume' if options.resume else '') + '-environment.log')).open('w') as log:
            node = subprocess.Popen([str(RUNTIME), 'start-single-node', '--certs-dir=' + str(STATE / 'certs'),
                '--store=' + str(STATE / 'data'), '--listen-addr=127.0.0.1:26259', '--advertise-addr=localhost:26259',
                '--http-addr=127.0.0.1:18004', '--cache=128MiB', '--max-sql-memory=128MiB',
                '--log=' + json.dumps({'file-defaults': {'dir': str(RESULTS / 'server-logs')}})],
                env={**os.environ, 'COCKROACH_SKIP_ENABLING_DIAGNOSTIC_REPORTING': 'true'}, stdout=log, stderr=subprocess.STDOUT)
            try:
                deadline = time.monotonic() + 30
                last_error = None
                while time.monotonic() < deadline:
                    assert node.poll() is None, 'Owned server exited; preserve source/logs'
                    try:
                        assert one('SELECT 1')=='1'
                        break
                    except subprocess.CalledProcessError as error:
                        last_error = error.stderr
                        time.sleep(.25)
                else: raise RuntimeError('Native SQL readiness deadline: '+str(last_error))
                native_ids=re.findall(r'^clusterID:\s+([0-9a-f-]+)',Path(log.name).read_text(),re.MULTILINE)
                assert len(native_ids)==1,'Owned startup must publish exactly one native cluster identity'
                cluster=native_ids[0]
                if options.resume:
                    initial=re.findall(r'^clusterID:\s+([0-9a-f-]+)',(RESULTS/'first-environment.log').read_text(),re.MULTILINE)
                    assert len(initial)==1 and cluster==initial[0], 'Original cluster identity changed'
                if phase == 'first':
                    assert one("SELECT count(*) FROM pg_database WHERE datname='oasis'") == '0'
                    sql('SET CLUSTER SETTING diagnostics.reporting.enabled=false')
                    durable(STATE / 'database.pending.json', {'database': 'oasis', 'clusterId': cluster})
                    sql('CREATE DATABASE oasis')
                    durable(STATE / 'database.ack.json', {'database': 'oasis', 'clusterId': cluster})
                    (STATE / 'database.pending.json').unlink()
                database_id = one("SELECT oid::STRING FROM pg_database WHERE datname='oasis'")
                assert one("SELECT count(*) FROM information_schema.tables WHERE table_schema='public'", 'oasis') == '0'
                observations.append({'phase': phase, 'clusterId': cluster, 'databaseId': database_id,
                    'version': one('SELECT version()'), 'userTables': 0})
            finally:
                if node.poll() is None: node.send_signal(signal.SIGTERM)
                assert node.wait(timeout=45) == 0, 'Owned teardown incomplete; preserve data'
    assert observations[0]['clusterId'] == observations[1]['clusterId']
    assert observations[0]['databaseId'] == observations[1]['databaseId']
    durable(STATE / 'environment.json', {'runtimeSha256': HASH, 'observations': observations})
    pending.unlink()
    print(json.dumps({'passed': True, 'observations': observations, 'stopped': True,
        'stateBytes': sum(p.stat().st_size for p in STATE.rglob('*') if p.is_file())}))

if __name__ == '__main__': main()
