"""One retained free QuestDB10.1.0 source, authenticated loopback HTTP/PG; never reset."""
from pathlib import Path
import base64, json, os, pwd, secrets, socket, subprocess, time
import urllib.request, urllib.error, urllib.parse

STATE = Path('/var/tmp/oasis-questdb-cql')
NAME = 'oasis-questdb-cql'
IMAGE = 'docker.io/questdb/questdb@sha256:6f733a142ee7ca8623d65135b46ed6cf78aac1e9ab15d1493ad7df837721e2e4'
IMAGE_ID = 'e29d6aca1d1e92cd43f311088a9c2e9f6fd4a40858996e3c80f0b0b4f27e18de'
OWNER = pwd.getpwnam('dellams').pw_uid
TABLES = ['oasis_avatars', 'oasis_avatar_details', 'oasis_holons']


def command(*args):
    return subprocess.check_output(['podman', *args], text=True).strip()


def durable(name, value):
    with (STATE/name).open('x') as file:
        os.fchmod(file.fileno(), 0o600)
        json.dump(value, file)
        file.flush()
        os.fsync(file.fileno())
    os.chown(STATE/name, OWNER, OWNER)


def query(sql, authenticated=True):
    headers = {}
    if authenticated:
        auth = json.loads((STATE/'credentials.json').read_text())
        headers['Authorization'] = 'Basic '+base64.b64encode((auth['username']+':'+auth['password']).encode()).decode()
    url = 'http://127.0.0.1:19000/exec?'+urllib.parse.urlencode({'query': sql})
    with urllib.request.urlopen(urllib.request.Request(url, headers=headers), timeout=15) as response:
        result = json.loads(response.read())
        assert 'error' not in result, 'Native SQL error; preserve original outcome'
        return result


def ready():
    deadline = time.monotonic()+120
    while time.monotonic() < deadline:
        container = json.loads(command('inspect', NAME))[0]
        assert container['Image'] == IMAGE_ID and container['State']['Running'], 'Native service/identity differs before readiness'
        try:
            build = query('SELECT build();')
            assert '10.1.0' in str(build['dataset']), 'Pinned native release differs'
            return build['dataset']
        except (urllib.error.URLError, TimeoutError, ConnectionError):
            time.sleep(.5)
    raise RuntimeError('Authenticated native QuestDB readiness deadline')


def snapshot():
    result = {}
    for name in TABLES:
        table = query("SELECT id,table_name,designatedTimestamp,partitionBy,walEnabled,dedup,ttlValue,matView FROM tables() WHERE table_name='"+name+"';")
        assert len(table['dataset']) == 1
        row = table['dataset'][0]
        assert row[0] > 0 and row[1:] == [name,'ts','DAY',True,True,0,False]
        columns = query('SELECT "column",type,designated,upsertKey FROM table_columns(\''+name+'\');')
        assert columns['dataset'] == [['id','SYMBOL',False,True],['data','VARCHAR',False,False],['ts','TIMESTAMP',True,True]]
        result[name] = {'table': table['dataset'], 'columns': columns['dataset']}
    return result


def stop():
    current = json.loads(command('inspect', NAME))[0]
    if current['State']['Running']:
        command('stop', '--time', '60', NAME)
        current = json.loads(command('inspect', NAME))[0]
    state = current['State']
    assert not state['Running'] and state['ExitCode'] in [0,143] and not state['OOMKilled'], 'Preserve failed native shutdown'
    # JVM SIGTERM exits143 even after hooks complete. Scope evidence to this start,
    # so an older successful stop can never hide a later failure.
    logs = subprocess.check_output(['podman','logs','--since',state['StartedAt'],NAME],stderr=subprocess.STDOUT,text=True)
    assert 'QuestDB is shutdown.' in logs and 'component=engine from=STOPPING to=STOPPED' in logs
    assert 'pg-server closed' in logs and 'http-server closed' in logs
    assert 'shutdown deferred' not in logs and 'shutdown proceeding after lifecycle stop failure' not in logs


def main():
    assert not STATE.exists(), 'Retain existing source and receipts; do not provision over it'
    assert json.loads(command('image', 'inspect', IMAGE_ID))[0]['Id'] == IMAGE_ID
    for port in [19000,18812]:
        with socket.socket() as probe: probe.bind(('127.0.0.1',port))
    STATE.mkdir(mode=0o700)
    os.chown(STATE,OWNER,OWNER)
    (STATE/'data').mkdir(mode=0o700)
    os.chown(STATE/'data',10001,10001)
    durable('credentials.json', {'username':'cosmic','password':secrets.token_hex(32)})
    auth = json.loads((STATE/'credentials.json').read_text())
    env = {'QDB_HTTP_USER':auth['username'],'QDB_HTTP_PASSWORD':auth['password'],
        'QDB_PG_USER':auth['username'],'QDB_PG_PASSWORD':auth['password'],
        'QDB_HTTP_MIN_ENABLED':'false','QDB_LINE_TCP_ENABLED':'false',
        'QDB_TELEMETRY_ENABLED':'false','QDB_TELEMETRY_DISABLE_COMPLETELY':'true',
        'QDB_SHARED_WORKER_COUNT':'2','QDB_WAL_APPLY_WORKER_COUNT':'1',
        'QDB_HTTP_WORKER_COUNT':'1','QDB_PG_WORKER_COUNT':'1',
        'JVM_PREPEND':'-Xms128m -Xmx512m -XX:ActiveProcessorCount=2'}
    with (STATE/'service.env').open('x') as file:
        os.fchmod(file.fileno(),0o600)
        file.write(''.join(k+'='+v+'\n' for k,v in env.items()))
    os.chown(STATE/'service.env',OWNER,OWNER)
    durable('environment.pending.json', {'image':IMAGE,'source':str(STATE),'ports':[19000,18812],'scope':'authenticated loopback plaintext local'})
    container = command('create','--name',NAME,'--cpus','2','--memory','2g','--pids-limit','512',
        '--log-opt','max-size=2mb','--env-file',str(STATE/'service.env'),
        '-p','127.0.0.1:19000:9000','-p','127.0.0.1:18812:8812',
        '-v',str(STATE/'data')+':/var/lib/questdb',IMAGE)
    durable('container.ack.json',{'id':container,'imageId':IMAGE_ID})
    command('start',NAME)
    try:
        build = ready()
        assert query('SELECT table_name FROM tables();')['dataset'] == []
        try:
            query('SELECT build();',authenticated=False)
            raise AssertionError('Unauthenticated native SQL access succeeded')
        except urllib.error.HTTPError as failure:
            assert failure.code in [401,403]
        for name in TABLES:
            sql = 'CREATE TABLE '+name+' (id SYMBOL,data VARCHAR,ts TIMESTAMP) TIMESTAMP(ts) PARTITION BY DAY WAL DEDUP UPSERT KEYS(ts,id);'
            durable(name+'.intent.json',{'sql':sql})
            acknowledgement = query(sql)
            assert acknowledgement == {'ddl':'OK'}
            durable(name+'.ack.json',acknowledgement)
        original = snapshot()
        durable('initial.ack.json',{'build':build,'schema':original})
    finally:
        stop()
    command('start',NAME)
    try:
        assert ready() == build
        assert snapshot() == original
        durable('restart.ack.json',{'build':build,'schema':original})
    finally:
        stop()
    durable('environment.json',{'container':container,'image':IMAGE,'version':'10.1.0',
        'sourceDirectoryInode':(STATE/'data').stat().st_ino,'http':'http://127.0.0.1:19000','pgPort':18812})
    (STATE/'environment.pending.json').rename(STATE/'environment.intent.ack.json')
    print(json.dumps({'passed':True,'stopped':True,'tableIds':{k:v['table'][0][0] for k,v in original.items()},
        'stateBytes':sum(f.stat().st_size for f in STATE.rglob('*') if f.is_file())}))


if __name__ == '__main__':
    main()
