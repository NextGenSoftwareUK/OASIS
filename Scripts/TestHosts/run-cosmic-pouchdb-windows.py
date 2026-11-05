"""Reuse one pinned real PouchDB HTTP/LevelDB source. Caller holds build lease."""
import argparse
import hashlib
import json
import msvcrt
import os
from pathlib import Path
import socket
import subprocess
import sys
import time
import urllib.error
import urllib.request
import uuid
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
STATE = ROOT / 'LocalState/COSMIC/RecoveryLaptopPouchDBCql'
NODE = Path('C:/Program Files/nodejs/node.exe')
PROJECT = ROOT / 'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.PouchDBOASIS.IntegrationTests'


def durable(path, value):
    with path.open('x', encoding='utf-8') as output:
        json.dump(value, output, indent=2)
        output.flush()
        os.fsync(output.fileno())


def validate_admission(initialize):
    if initialize and any((STATE / name).exists() for name in ['source.json', 'bootstrap.intent.json', 'bootstrap.ack.json']):
        raise RuntimeError('Existing source/outcome; initialization cannot replay it')
    if not initialize and not all((STATE / name).is_file() for name in ['source.json', 'bootstrap.ack.json', 'native-baseline.json']):
        raise RuntimeError('Acknowledged retained source and baseline required')
    if (STATE / 'mongo.pending.json').exists():
        raise RuntimeError('Unresolved consumer outcome; no source replay')


def validate_closed(path):
    events = [json.loads(line) for line in path.read_text(encoding='utf-8').splitlines() if line.startswith('{')]
    closed = [event for event in events if event.get('stopped') is True]
    if len(closed) != 1 or closed[0].get('nativeDatabasesClosed') != 1:
        raise RuntimeError('Current native phase did not prove database closure')


class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, request, fp, code, message, headers, url):
        return None


OPENER = urllib.request.build_opener(urllib.request.ProxyHandler({}), NoRedirect())


def get(path):
    with OPENER.open('http://127.0.0.1:19600' + path, timeout=15) as response:
        raw = response.read(1024 * 1024 + 1)
        if len(raw) > 1024 * 1024:
            raise RuntimeError('Native observation exceeds budget')
        return raw


def observe_source():
    source_bytes = (STATE / 'source.json').read_bytes()
    ack = json.loads((STATE / 'bootstrap.ack.json').read_bytes())
    if hashlib.sha256(source_bytes).hexdigest() != ack['sourceSha256'].lower() or ack['createdRecords'] != 4:
        raise RuntimeError('Original source descriptor/admission differs')
    source = json.loads(source_bytes)
    hashes = {}
    for identity in source['LiveIds'] + [source['DeletedId']]:
        raw = get('/oasis_holons/' + identity + '?conflicts=true')
        row = json.loads(raw)
        receipt = json.loads((STATE / (identity.replace('-', '') + '.ack.json')).read_bytes())
        if row['_id'] != identity or row['Id'] != identity or row['CreatedByAvatarId'] != source['Actor']:
            raise RuntimeError('Native source identity/owner differs')
        if row['_rev'] != receipt['revision'] or not row['_rev'].startswith('1-') or row.get('_conflicts', []):
            raise RuntimeError('Original native revision differs or conflicts')
        if row['IsDeleted'] != (identity == source['DeletedId']):
            raise RuntimeError('Native deletion control differs')
        hashes[identity] = hashlib.sha256(raw).hexdigest()
    try:
        get('/oasis_holons/' + source['MissingId'])
    except urllib.error.HTTPError as error:
        if error.code != 404 or json.loads(error.read(65536)).get('reason') != 'missing':
            raise RuntimeError('Missing native control differs') from error
    else:
        raise RuntimeError('Original missing record is now present')
    return hashes


def run(initialize, paired=False, serve=False):
    with (STATE / 'runner.lock').open('a+b') as lock:
        lock.seek(0)
        msvcrt.locking(lock.fileno(), msvcrt.LK_NBLCK, 1)
        validate_admission(initialize)
        if paired:
            if (STATE / 'demo-ready.json').exists():
                raise RuntimeError('Unresolved previous browser session')
            with socket.socket() as probe:
                if probe.connect_ex(('127.0.0.1', 5141)) == 0:
                    raise RuntimeError('Browser listener occupied')
        manifest = (STATE / 'runtime-files.json').read_bytes()
        if hashlib.sha256(manifest).hexdigest() != '60f79bf3018a5dfb578097f4f3aab298fa02632f2dfbceb29d0162df4ac04684':
            raise RuntimeError('Admitted runtime manifest differs')
        for item in json.loads(manifest):
            target = (STATE / item['path']).resolve()
            if not target.is_relative_to((STATE / 'node_modules').resolve()) or target.stat().st_size != item['bytes'] or hashlib.sha256(target.read_bytes()).hexdigest() != item['sha256']:
                raise RuntimeError('Native runtime file differs')
        if hashlib.sha256(NODE.read_bytes()).hexdigest() != '0c3609dc301fb33d75ab933a527e7788cb3b2df15720326e55ac9c54c708466e':
            raise RuntimeError('Admitted Node runtime differs')
        with socket.socket() as probe:
            if probe.connect_ex(('127.0.0.1', 19600)) == 0:
                raise RuntimeError('Existing native listener; no process adoption')
        (STATE / 'data').mkdir(exist_ok=True)
        reports = STATE / (('interactive-' if serve else 'paired-' if paired else 'initial-' if initialize else 'retained-') + uuid.uuid4().hex)
        reports.mkdir()
        durable(reports / 'intent.json', {'initialize': initialize, 'port': 19600, 'adapter': 'leveldb'})
        startup = subprocess.STARTUPINFO()
        startup.dwFlags |= subprocess.STARTF_USESHOWWINDOW
        startup.wShowWindow = subprocess.SW_HIDE
        with (reports / 'server.log').open('xb') as log:
            node = subprocess.Popen([str(NODE), str(ROOT / 'Scripts/TestHosts/Serve-PouchDbNative.cjs')], cwd=STATE,
                stdout=log, stderr=subprocess.STDOUT, startupinfo=startup, creationflags=subprocess.CREATE_NEW_CONSOLE)
            durable(reports / 'process.json', {'pid': node.pid})
            try:
                deadline = time.monotonic() + 30
                while time.monotonic() < deadline:
                    if node.poll() is not None:
                        raise RuntimeError('Native process exited before readiness')
                    try:
                        info = json.loads(get('/'))
                        break
                    except (ConnectionError, urllib.error.URLError):
                        time.sleep(.25)
                else:
                    raise RuntimeError('Native readiness deadline')
                if info['version'] != '4.2.0' or info['express-pouchdb-mode'] != 'minimumForPouchDB' or 'leveldb' not in info['pouchdb-adapters']:
                    raise RuntimeError('Native HTTP product/adapters differ')
                durable(reports / 'ready.json', info)
                if initialize:
                    identities = [str(uuid.uuid4()) for _ in range(5)]
                    durable(STATE / 'source.json', {'Endpoint': 'http://127.0.0.1:19600', 'Actor': str(uuid.uuid4()),
                        'LiveIds': identities[:3], 'DeletedId': identities[3], 'MissingId': identities[4]})
                env = dict(os.environ, COSMIC_POUCHDB_CQL_SOURCE=str(STATE / 'source.json'), COSMIC_POUCHDB_CQL_ROOT=str(STATE))
                env['OASIS_POUCHDB_MONGO_SERVE'] = '1' if serve else '0'
                def test(category):
                    with (reports / (category + '.log')).open('xb') as output:
                        subprocess.run(['dotnet', 'test', str(PROJECT), '--no-build', '--no-restore', '--filter', 'TestCategory=' + category,
                            '--results-directory', str(reports), '--logger', 'trx;LogFileName=' + category + '.trx', '/m:2', '/nr:false', '/p:UseSharedCompilation=false'],
                            env=env, cwd=ROOT, stdout=output, stderr=subprocess.STDOUT, check=True)
                    trx = reports / (category + '.trx')
                    counts = ET.parse(trx).find('.//{*}Counters').attrib
                    if counts['passed'] != '1' or counts['total'] != '1' or counts['notExecuted'] != '0':
                        raise RuntimeError('Expected one real native pass, zero skips')
                    return hashlib.sha256(trx.read_bytes()).hexdigest()
                if initialize:
                    durable(reports / 'bootstrap.json', {'trxSha256': test('PouchDbCqlBootstrap')})
                    durable(STATE / 'native-baseline.json', observe_source())
                before = observe_source()
                if before != json.loads((STATE / 'native-baseline.json').read_bytes()):
                    raise RuntimeError('Original native payload changed')
                trx_hash = test('PouchDBMongoCqlNative' if paired else 'PouchDBCqlNative')
                if observe_source() != before:
                    raise RuntimeError('Native query changed original payload/revision')
                if (STATE / 'mongo.pending.json').exists() or (STATE / 'demo-ready.json').exists():
                    raise RuntimeError('Consumer fixture cleanup incomplete')
                durable(reports / 'result.json', {'passed': 1, 'trxSha256': trx_hash, 'originalDocumentsUnchanged': True})
            finally:
                if node.poll() is not None:
                    raise RuntimeError('Owned native process exited before explicit shutdown')
                signal = subprocess.run([sys.executable, '-B', str(ROOT / 'Scripts/TestHosts/Signal-OwnedConsoleProcess.py'), '--pid', str(node.pid)], capture_output=True, text=True, check=True)
                receipt = json.loads(signal.stdout)
                code = node.wait(timeout=30)
                validate_closed(reports / 'server.log')
                durable(reports / 'stopped.json', dict(receipt, exitCode=code, nativeDatabaseClosed=True))
                if receipt['targetPid'] != node.pid or receipt['ctrlCSent'] is not True or code != 0:
                    raise RuntimeError('Native shutdown differs')
        print(json.dumps({'completed': True, 'phase': reports.name}))


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    mode = parser.add_mutually_exclusive_group(required=True)
    mode.add_argument('--initialize', action='store_true')
    mode.add_argument('--retained', action='store_true')
    mode.add_argument('--paired', action='store_true')
    parser.add_argument('--serve', action='store_true')
    args = parser.parse_args()
    if args.serve and not args.paired:
        parser.error('Interactive serving requires paired mode')
    run(args.initialize, args.paired, args.serve)
