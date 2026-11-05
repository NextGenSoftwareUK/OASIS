"""Pinned owned ArcadeDB SQL source. Bootstrap is explicit and never replayed."""
import argparse
import base64
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import secrets
import shutil
import socket
import subprocess
import time
import urllib.error
import urllib.request
import uuid
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
STATE = ROOT / 'LocalState/COSMIC/RecoveryLaptopArcade26111'
INSTALL = STATE / 'arcadedb-26.11.1'
HOME = STATE / 'home'
JAVA_STATE = ROOT / 'LocalState/COSMIC/RecoveryLaptopSolr1010'
JAVA = JAVA_STATE / 'jdk-21.0.13+9-jre/bin/java.exe'
ENDPOINT = 'http://127.0.0.1:24800'
PROJECT = ROOT / 'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.ArcadeDBOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.ArcadeDBOASIS.IntegrationTests.csproj'

class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, *args): return None

OPENER = urllib.request.build_opener(urllib.request.ProxyHandler({}), NoRedirect())

def write_once(path, value):
    with path.open('xb') as output:
        output.write(value if isinstance(value,bytes) else json.dumps(value,indent=2).encode())
        output.flush()
        os.fsync(output.fileno())

def verify_runtimes():
    for directory, digest, prefix in [(STATE,'d37e2fec418708afb0e4b6e192dfd5a03bcc43d19612b99542724711c1b08c76',''),
        (JAVA_STATE,'7f8e68dd01566a36e0b0c441481b3fcc477a63fa153a973eee8b04486ce32520','jdk-21.0.13+9-jre/')]:
        raw = (directory / 'runtime-manifest.json').read_bytes()
        if hashlib.sha256(raw).hexdigest() != digest: raise RuntimeError('Pinned runtime manifest differs')
        for name, expected in json.loads(raw).items():
            if prefix and not name.startswith(prefix): continue
            path = (directory / name).resolve()
            if not path.is_relative_to(directory.resolve()) or hashlib.sha256(path.read_bytes()).hexdigest() != expected:
                raise RuntimeError('Pinned runtime file differs: ' + name)

def request(path, password, data=None, timeout=15):
    call = urllib.request.Request(ENDPOINT + path, data=None if data is None else json.dumps(data).encode(),
        headers={'Content-Type':'application/json','Authorization':'Basic ' + base64.b64encode(('root:' + password).encode()).decode()})
    with OPENER.open(call,timeout=timeout) as response:
        status = response.status
        raw = response.read(4*1024*1024+1)
    if len(raw)>4*1024*1024: raise RuntimeError('Native response exceeds budget')
    if status == 204: return None
    body = json.loads(raw)
    if 'error' in body or 'exception' in body: raise RuntimeError('ArcadeDB error response')
    return body

def run_test(category, phase, env):
    results = ROOT / 'TestResults/ArcadeDBCqlNativeCurrent'
    results.mkdir(exist_ok=True)
    target = results / (phase + '.trx')
    if target.exists(): raise RuntimeError('Refusing to overwrite native evidence')
    result = subprocess.run(['dotnet','test',str(PROJECT),'--no-build','--no-restore','--filter','TestCategory=' + category,
        '--logger','trx;LogFileName=' + target.name,'--results-directory',str(results),'/m:2','/nr:false','/p:UseSharedCompilation=false','-v:q'],env=env)
    if result.returncode: raise RuntimeError('ArcadeDB native ' + category + ' failed; preserve original intent')
    ns = {'t':'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
    counters = ET.parse(target).getroot().find('t:ResultSummary/t:Counters',ns).attrib
    if any(counters[k] != value for k,value in [('total','1'),('passed','1'),('failed','0'),('notExecuted','0')]):
        raise RuntimeError('Native test counters differ')
    return dict(counters=counters,trxSha256=hashlib.sha256(target.read_bytes()).hexdigest())

def observe_source(password):
    source = json.loads((STATE / 'source.json').read_bytes())
    hashes = []
    for id in source['LiveIds'] + [source['DeletedId'],source['MissingId']]:
        body = request('/api/v1/query/oasis',password,{'language':'sql',
            'command':'SELECT oasisId, doc FROM Holon WHERE oasisId = :id LIMIT 2','params':{'id':id}})
        rows = body['result']
        if id == source['MissingId']:
            if rows != []: raise RuntimeError('Original absent control is present')
            continue
        if len(rows)!=1 or rows[0]['oasisId']!=id: raise RuntimeError('Original native identity differs')
        raw = rows[0]['doc'].encode()
        if len(raw)>1024*1024: raise RuntimeError('Original stored document exceeds budget')
        row = json.loads(raw)
        expected = {'Id':id,'Name':'ArcadeDB CQL teaching ' + uuid.UUID(id).hex,
            'CreatedByAvatarId':source['Actor'],'ParentHolonId':source['Actor'],'IsPublic':False,'IsActive':True,
            'DeletedDate':'2026-11-05T00:00:00Z' if id == source['DeletedId'] else '0001-01-01T00:00:00'}
        if any(row.get(key)!=value for key,value in expected.items()):
            raise RuntimeError('Original acknowledged teaching payload differs')
        hashes.append(hashlib.sha256(raw).hexdigest())
    return hashes


def wait_until_ready(process, password):
    until = time.monotonic() + 60
    while True:
        if process.poll() is not None: raise RuntimeError('Owned ArcadeDB exited before readiness')
        try:
            if request('/api/v1/ready',password,timeout=2) is not None:
                raise RuntimeError('Native readiness did not return the documented204 response')
            return
        except urllib.error.HTTPError as error:
            if error.code != 503: raise
            error.close()
        except (urllib.error.URLError,TimeoutError,ConnectionError): pass
        if time.monotonic() >= until: raise TimeoutError('ArcadeDB readiness deadline exceeded')
        time.sleep(.25)


def run():
    parser = argparse.ArgumentParser()
    parser.add_argument('--initialize',action='store_true')
    parser.add_argument('--paired',action='store_true')
    parser.add_argument('--serve-run-id')
    args = parser.parse_args()
    if args.paired and args.initialize: raise RuntimeError('Pairing requires the retained acknowledged source')
    if args.serve_run_id and (not args.paired or len(args.serve_run_id)!=32 or any(c not in '0123456789abcdef' for c in args.serve_run_id)):
        raise RuntimeError('Interactive pairing requires an explicit GuidN run identity')
    if (STATE / 'mongo.pending.json').exists(): raise RuntimeError('Unfinished Mongo fixture requires reconciliation')
    with socket.socket() as port: port.bind(('127.0.0.1',24800))
    verify_runtimes()
    spec = importlib.util.spec_from_file_location('protection',ROOT / 'Scripts/Sandbox/Verify-HostedCqlDemo.py')
    protection = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(protection)
    if args.initialize:
        if HOME.exists() or (STATE / 'source.json').exists(): raise RuntimeError('Existing owned source must be reconciled; never reseed')
        HOME.mkdir()
        shutil.copytree(INSTALL / 'config',HOME / 'config')
        source = dict(Endpoint=ENDPOINT,Actor=str(uuid.uuid4()),LiveIds=[str(uuid.uuid4()) for _ in range(3)],DeletedId=str(uuid.uuid4()),MissingId=str(uuid.uuid4()))
        write_once(STATE / 'source.json',source)
        password = secrets.token_urlsafe(32)
        write_once(STATE / 'root-password.dpapi',protection.protected_token(password.encode()))
    else:
        if not (STATE / 'bootstrap.ack.json').exists(): raise RuntimeError('Acknowledged retained source required')
        password = protection.protected_token((STATE / 'root-password.dpapi').read_bytes(),decrypt=True).decode()
    raw = (STATE / 'source.json').read_bytes()
    source_hash = hashlib.sha256(raw).hexdigest()
    if json.loads(raw)['Endpoint'] != ENDPOINT: raise RuntimeError('Fixed loopback source required')
    if not args.initialize:
        ack = json.loads((STATE / 'bootstrap.ack.json').read_bytes())
        if ack['sourceSha256'].lower() != source_hash or ack['savedRecords'] != 4:
            raise RuntimeError('Original retained bootstrap identity differs')
    phase = ('interactive-' + args.serve_run_id if args.serve_run_id else 'paired') if args.paired else ('initial' if args.initialize else 'retained-' + uuid.uuid4().hex)
    write_once(STATE / (phase + '-run.intent.json'),dict(sourceSha256=source_hash,bootstrapThisRun=args.initialize))
    env = os.environ.copy()
    env['arcadedb.server.rootPassword'] = password
    env['COSMIC_ARCADEDB_CQL_SOURCE'] = str(STATE / 'source.json')
    env['COSMIC_ARCADEDB_CQL_PASSWORD'] = password
    env['OASIS_ARCADEDB_MONGO_SERVE'] = '1' if args.serve_run_id else '0'
    command = [str(JAVA),'-Xms64m','-Xmx256m','--enable-native-access=ALL-UNNAMED',
        '-Darcadedb.profile=low-ram','-Darcadedb.server.rootPath=' + str(HOME),
        '-Darcadedb.server.httpIncomingHost=127.0.0.1','-Darcadedb.server.httpIncomingPort=24800',
        '-Darcadedb.ha.enabled=false','-Darcadedb.server.plugins=',
        '-Djava.util.logging.config.file=' + str(HOME / 'config/arcadedb-log.properties'),
        '-cp',str(INSTALL / 'lib/*'),'com.arcadedb.server.ArcadeDBServer']
    with (STATE / (phase + '-server.log')).open('xb') as log:
        process = subprocess.Popen(command,cwd=HOME,env=env,stdin=subprocess.DEVNULL,stdout=log,stderr=subprocess.STDOUT,creationflags=subprocess.CREATE_NO_WINDOW)
        write_once(STATE / (phase + '-process.json'),dict(pid=process.pid,command=command))
        ready = False
        try:
            wait_until_ready(process,password)
            ready = True
            if args.initialize:
                result = run_test('ArcadeDBCqlBootstrap','bootstrap',env)
                write_once(STATE / 'bootstrap-run.result.json',result)
            ack = json.loads((STATE / 'bootstrap.ack.json').read_bytes())
            if ack['sourceSha256'].lower() != source_hash or ack['savedRecords'] != 4: raise RuntimeError('Original acknowledged bootstrap source differs')
            before = observe_source(password)
            result = run_test('ArcadeDBMongoCqlNative' if args.paired else 'ArcadeDBCqlNative',phase,env)
            if observe_source(password) != before: raise RuntimeError('Original ArcadeDB source changed during demo')
            if args.paired and ((STATE / 'mongo.pending.json').exists() or (STATE / 'demo-ready.json').exists()):
                raise RuntimeError('Paired fixture cleanup is incomplete')
            write_once(STATE / (phase + '-run.result.json'),dict(sourceSha256=source_hash,bootstrapThisRun=args.initialize,originalDocumentsUnchanged=True,**result))
        finally:
            if process.poll() is None:
                if ready:
                    response = request('/api/v1/server',password,{'command':'shutdown'})
                    if response.get('result') != 'ok': raise RuntimeError('Graceful native shutdown was not acknowledged')
                else:
                    process.terminate()  # Failed pre-ready owned process, no test/bootstrap was dispatched.
                process.wait(timeout=30)
            write_once(STATE / (phase + '-stopped.json'),dict(pid=process.pid,exitCode=process.returncode,graceful=ready))

if __name__ == '__main__': run()
