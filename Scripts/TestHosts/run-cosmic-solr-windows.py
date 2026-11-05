"""One retained loopback Solr source; explicit first seed, then read-only tests."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil
import socket
import subprocess
import time
import urllib.request
import uuid
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
STATE = ROOT / 'LocalState/COSMIC/RecoveryLaptopSolr1010'
INSTALL = STATE / 'solr-10.1.0-slim'
JAVA = STATE / 'jdk-21.0.13+9-jre/bin/java.exe'
HOME = STATE / 'home'
ENDPOINT = 'http://127.0.0.1:18883'
class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, request, fp, code, message, headers, newurl):
        return None

OPENER = urllib.request.build_opener(urllib.request.ProxyHandler({}), NoRedirect())

def verify_runtime():
    manifest = (STATE / 'runtime-manifest.json').read_bytes()
    if hashlib.sha256(manifest).hexdigest() != '7f8e68dd01566a36e0b0c441481b3fcc477a63fa153a973eee8b04486ce32520':
        raise RuntimeError('Pinned runtime manifest differs')
    for relative, expected in json.loads(manifest).items():
        target = (STATE / relative).resolve()
        if not target.is_relative_to(STATE.resolve()) or hashlib.sha256(target.read_bytes()).hexdigest() != expected:
            raise RuntimeError('Pinned runtime file differs: ' + relative)


def write_once(path, value):
    with path.open('x', encoding='utf-8') as output:
        json.dump(value, output, indent=2)
        output.flush()
        os.fsync(output.fileno())

def call(path, data=None, timeout=15):
    request = urllib.request.Request(ENDPOINT + path,
        data=None if data is None else json.dumps(data).encode(),
        headers={'Content-Type': 'application/json'})
    with OPENER.open(request, timeout=timeout) as response:
        raw = response.read(4 * 1024 * 1024 + 1)
    if len(raw) > 4 * 1024 * 1024:
        raise RuntimeError('Solr response exceeds budget')
    value = json.loads(raw)
    if value.get('responseHeader', {}).get('status') != 0:
        raise RuntimeError('Solr response status unsuccessful')
    return value

def assert_port_free(port):
    with socket.socket() as listener:
        listener.bind(('127.0.0.1', port))

def initialize_files():
    if HOME.exists() or (STATE / 'source.json').exists():
        raise RuntimeError('Existing source requires reconciliation, never automatic reseeding')
    HOME.mkdir()
    shutil.copyfile(INSTALL / 'server/solr/solr.xml', HOME / 'solr.xml')
    conf = HOME / 'oasis_holons/conf'
    shutil.copytree(INSTALL / 'server/solr/configsets/_default/conf', conf)
    (HOME / 'oasis_holons/core.properties').write_text('name=oasis_holons\n')
    schema_path = conf / 'managed-schema.xml'
    schema = ET.parse(schema_path)
    for name, kind in [('name', 'string'), ('description', 'string'),
        ('createdByAvatarId', 'string'), ('parentHolonId', 'string'),
        ('deletedDate', 'pdate'), ('isPublic', 'boolean'), ('isActive', 'boolean')]:
        if any(field.get('name') == name for field in schema.getroot().findall('field')):
            raise RuntimeError('Unexpected existing fixture schema field')
        ET.SubElement(schema.getroot(), 'field', name=name, type=kind,
            indexed='true', stored='true', multiValued='false')
    schema.write(schema_path, encoding='utf-8', xml_declaration=True)
    source = dict(Endpoint=ENDPOINT, Actor=str(uuid.uuid4()),
        LiveIds=[str(uuid.uuid4()) for _ in range(3)],
        DeletedId=str(uuid.uuid4()), MissingId=str(uuid.uuid4()))
    write_once(STATE / 'source.json', source)
    rows = [dict(id=item, name='Solr teaching record ' + str(index + 1),
        description='Owned local native fixture', isPublic=False, isActive=True,
        createdByAvatarId=source['Actor'], parentHolonId=source['Actor'],
        deletedDate='2026-11-05T00:00:00Z' if item == source['DeletedId'] else '0001-01-01T00:00:00Z')
        for index, item in enumerate(source['LiveIds'] + [source['DeletedId']])]
    write_once(STATE / 'seed.intent.json', dict(rows=rows, sourceSha256=hashlib.sha256((STATE / 'source.json').read_bytes()).hexdigest()))


def observe_source():
    source = json.loads((STATE / 'source.json').read_text())
    seed = json.loads((STATE / 'seed.intent.json').read_text())
    if seed['sourceSha256'] != hashlib.sha256((STATE / 'source.json').read_bytes()).hexdigest():
        raise RuntimeError('Original source metadata differs')
    acknowledgement = json.loads((STATE / 'seed.ack.json').read_text())
    if acknowledgement['intentSha256'] != hashlib.sha256((STATE / 'seed.intent.json').read_bytes()).hexdigest():
        raise RuntimeError('Original acknowledged seed intent differs')
    hashes = []
    for expected in seed['rows']:
        native = call('/solr/oasis_holons/get?id=' + expected['id'] + '&wt=json&omitHeader=false')['doc']
        if native is None or any(native.get(key) != value for key, value in expected.items()):
            raise RuntimeError('Original Solr document differs from the acknowledged teaching record')
        hashes.append(hashlib.sha256(json.dumps(native, sort_keys=True).encode()).hexdigest())
    if call('/solr/oasis_holons/get?id=' + source['MissingId'] + '&wt=json&omitHeader=false')['doc'] is not None:
        raise RuntimeError('Original Solr missing control is present')
    return hashes


def wait_until_ready(process):
    until = time.monotonic() + 60
    while True:
        if process.poll() is not None:
            raise RuntimeError('Owned Solr exited before readiness')
        try:
            status = call('/solr/oasis_holons/schema/uniquekey?wt=json', timeout=2)
            if status.get('uniqueKey') != 'id':
                raise RuntimeError('Native Solr unique key differs')
            break
        except urllib.error.HTTPError as error:
            if error.code != 503:
                raise
            error.close()
        except (urllib.error.URLError, ConnectionError, TimeoutError):
            pass  # Bound readiness polling only; no writes or tests replayed.
        if time.monotonic() >= until:
            raise TimeoutError('Owned Solr did not become ready')
        time.sleep(0.25)


def run():
    parser = argparse.ArgumentParser()
    modes = parser.add_mutually_exclusive_group()
    modes.add_argument('--initialize', action='store_true')
    modes.add_argument('--resume-unseeded', action='store_true')
    parser.add_argument('--paired', action='store_true')
    parser.add_argument('--serve-run-id')
    args = parser.parse_args()
    if args.paired and (args.initialize or args.resume_unseeded):
        raise RuntimeError('Pairing uses only the acknowledged existing Solr source')
    if args.serve_run_id and (not args.paired or len(args.serve_run_id) != 32 or any(c not in '0123456789abcdef' for c in args.serve_run_id)):
        raise RuntimeError('Serve run requires paired mode and an explicit GuidN run identity')
    if (STATE / 'mongo.pending.json').exists():
        raise RuntimeError('Unfinished Mongo fixture requires reconciliation')
    for port in (18883, 17883):
        assert_port_free(port)
    if args.initialize:
        initialize_files()
    elif args.resume_unseeded:
        if (STATE / 'seed.ack.json').exists() or (STATE / 'seed-dispatch.intent.json').exists() or not (STATE / 'seed.intent.json').exists():
            raise RuntimeError('Explicit unseeded recovery requires an original seed intent and no ACK')
        stopped = json.loads((STATE / 'initial-stopped.json').read_text())
        if stopped['exitCode'] != 0 or (ROOT / 'TestResults/SolrCqlNativeCurrent/initial.trx').exists():
            raise RuntimeError('Original pre-test startup failure is not reconciled')
    elif not (STATE / 'seed.ack.json').exists():
        raise RuntimeError('No acknowledged source; retained reads cannot seed')
    verify_runtime()
    phase = ('interactive-' + args.serve_run_id if args.serve_run_id else 'paired') if args.paired else ('initial' if args.initialize else ('unseeded-resume-' if args.resume_unseeded else 'retained-') + uuid.uuid4().hex)
    intent = STATE / (phase + '-run.intent.json')
    write_once(intent, dict(phase=phase, seededThisRun=args.initialize or args.resume_unseeded))
    logs = STATE / 'logs'
    logs.mkdir(exist_ok=True)
    temporary = STATE / 'tmp'
    temporary.mkdir(exist_ok=True)
    command = [str(JAVA), '-Xms128m', '-Xmx256m', '--enable-native-access=ALL-UNNAMED',
        '-Djetty.host=127.0.0.1', '-Dsolr.port.listen=18883',
        '-DSTOP.PORT=17883', '-DSTOP.KEY=cosmic-owned-loopback',
        '-Dsolr.solr.home=' + str(HOME), '-Dsolr.install.dir=' + str(INSTALL),
        '-Djetty.home=' + str(INSTALL / 'server'), '-Dsolr.logs.dir=' + str(logs),
        '-Dlog4j.configurationFile=' + str(INSTALL / 'server/resources/log4j2.xml'),
        '-Djava.io.tmpdir=' + str(temporary), '-jar', 'start.jar', '--module=http']
    with (STATE / (phase + '-server.log')).open('xb') as log:
        process = subprocess.Popen(command, cwd=INSTALL / 'server', stdout=log,
            stderr=subprocess.STDOUT, creationflags=subprocess.CREATE_NO_WINDOW)
        write_once(STATE / (phase + '-process.json'), dict(pid=process.pid, command=command))
        try:
            wait_until_ready(process)
            if args.initialize or args.resume_unseeded:
                source = json.loads((STATE / 'source.json').read_text())
                seed = json.loads((STATE / 'seed.intent.json').read_text())
                if seed['sourceSha256'] != hashlib.sha256((STATE / 'source.json').read_bytes()).hexdigest():
                    raise RuntimeError('Source identity changed since seed intent')
                for row in seed['rows']:
                    observed = call('/solr/oasis_holons/get?id=' + row['id'] + '&wt=json&omitHeader=false')
                    if observed['doc'] is not None:
                        raise RuntimeError('Source is not empty: reconcile without replaying seed')
                write_once(STATE / 'seed-dispatch.intent.json', dict(phase=phase, originalIntentSha256=hashlib.sha256((STATE / 'seed.intent.json').read_bytes()).hexdigest()))
                response = call('/solr/oasis_holons/update/json/docs?commit=true&wt=json', seed['rows'])
                write_once(STATE / 'seed.ack.json', dict(response=response,
                    intentSha256=hashlib.sha256((STATE / 'seed.intent.json').read_bytes()).hexdigest()))
            before = observe_source()
            env = os.environ.copy()
            env['COSMIC_SOLR_CQL_SOURCE'] = str(STATE / 'source.json')
            env.pop('COSMIC_SOLR_CQL_USERNAME', None)
            env.pop('COSMIC_SOLR_CQL_PASSWORD', None)
            env['OASIS_SOLR_MONGO_SERVE'] = '1' if args.serve_run_id else '0'
            results = ROOT / 'TestResults/SolrCqlNativeCurrent'
            results.mkdir(exist_ok=True)
            name = phase + '.trx'
            target = results / name
            if target.exists():
                raise RuntimeError('Refusing to overwrite native evidence')
            outcome = subprocess.run(['dotnet', 'test', str(ROOT / 'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.SolrOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.SolrOASIS.IntegrationTests.csproj'),
                '--no-build', '--no-restore', '--filter', 'TestCategory=' + ('SolrMongoCqlNative' if args.paired else 'SolrCqlNative'),
                '--logger', 'trx;LogFileName=' + name, '--results-directory', str(results),
                '/m:2', '/nr:false', '/p:UseSharedCompilation=false', '-v:q'], env=env)
            if outcome.returncode:
                raise RuntimeError('Native Solr CQL acceptance failed')
            ns = {'t':'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
            counters = ET.parse(target).getroot().find('t:ResultSummary/t:Counters', ns).attrib
            if any(counters[key] != expected for key, expected in
                [('total','1'), ('passed','1'), ('failed','0'), ('notExecuted','0')]):
                raise RuntimeError('Native test counters differ')
            if observe_source() != before:
                raise RuntimeError('Retained Solr source changed during query qualification')
            if args.paired and ((STATE / 'mongo.pending.json').exists() or (STATE / 'demo-ready.json').exists()):
                raise RuntimeError('Paired fixture cleanup is incomplete')
            write_once(STATE / (phase + '-run.result.json'), dict(counters=counters, sourceDocumentsUnchanged=True,
                trxSha256=hashlib.sha256(target.read_bytes()).hexdigest(), seededThisRun=args.initialize or args.resume_unseeded))
        finally:
            if process.poll() is None:
                stop = subprocess.run([str(JAVA), '-Djetty.home=' + str(INSTALL / 'server'),
                    '-jar', str(INSTALL / 'server/start.jar'), 'STOP.PORT=17883',
                    'STOP.KEY=cosmic-owned-loopback', '--stop'], cwd=INSTALL / 'server',
                    stdout=log, stderr=subprocess.STDOUT, creationflags=subprocess.CREATE_NO_WINDOW, timeout=30)
                if stop.returncode:
                    raise RuntimeError('Owned Solr graceful stop failed')
                process.wait(timeout=30)
            write_once(STATE / (phase + '-stopped.json'), dict(pid=process.pid, exitCode=process.returncode))

if __name__ == '__main__':
    run()
