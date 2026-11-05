"""Qualify the pinned Windows Elasticsearch launcher and its owned native node."""
import argparse
import ctypes
from ctypes import wintypes
import hashlib
import json
import msvcrt
import os
from pathlib import Path
import shutil
import socket
import subprocess
import sys
import time
import urllib.error
import urllib.request
import uuid
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
STATE = ROOT / 'LocalState/COSMIC/RecoveryLaptopElasticsearch81924'
HOME = STATE / 'elasticsearch-8.19.24'
JAVA = HOME / 'jdk'
CLUSTER = 'cosmic-cql-elasticsearch-recovery'
ENDPOINT = 'http://127.0.0.1:19400'
PROJECT = ROOT / 'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.ElasticsearchOASIS.IntegrationTests'


def write_once(path, value):
    with path.open('xb') as f:
        f.write(value if isinstance(value, bytes) else json.dumps(value, indent=2).encode())
        f.flush()
        os.fsync(f.fileno())


def current_node_closed(path, offset):
    raw = path.read_bytes()
    if offset < 0 or len(raw) < offset:
        raise RuntimeError('Native log lineage changed during this run')
    current = raw[offset:].decode('utf-8')
    return '[cosmic-owned-local] stopped' in current and '[cosmic-owned-local] closed' in current


def run_test(category, label, env):
    results = STATE / label
    results.mkdir(exist_ok=False)
    with (results / 'test.log').open('xb') as output:
        subprocess.run(['dotnet', 'test', str(PROJECT), '--no-build', '--no-restore', '--filter',
                        'TestCategory=' + category, '/m:2', '/nr:false', '/p:UseSharedCompilation=false',
                        '--results-directory', str(results), '--logger', 'trx;LogFileName=native.trx'],
                       env=env, stdout=output, stderr=subprocess.STDOUT, check=True)
    trx = results / 'native.trx'
    counts = ET.parse(trx).find('.//{*}Counters').attrib
    if counts['total'] != '1' or counts['passed'] != '1' or counts['notExecuted'] != '0':
        raise RuntimeError('Exactly one native pass with zero skips required')
    return {'passed': 1, 'skipped': 0, 'trxSha256': hashlib.sha256(trx.read_bytes()).hexdigest()}


def expected_node_config():
    return (f'cluster.name: {CLUSTER}\nnode.name: cosmic-owned-local\n'
            'network.host: 127.0.0.1\nhttp.port: 19400\ntransport.port: 19500\n'
            'discovery.type: single-node\nxpack.security.enabled: false\n'
            'xpack.ml.enabled: false\ningest.geoip.downloader.enabled: false\n'
            'action.auto_create_index: false\n'
            f'path.data: "{(STATE / "data").as_posix()}"\n'
            f'path.logs: "{(STATE / "logs").as_posix()}"\n').encode()


def validate_source_health(health):
    if health.get('cluster_name') != CLUSTER or health.get('timed_out') is not False or health.get('status') != 'green':
        raise RuntimeError('Original native index has not reached its required ready state')
    if (health.get('number_of_nodes') != 1 or health.get('active_primary_shards') != 1 or
            health.get('active_shards') != 1 or health.get('unassigned_shards') != 0 or health.get('initializing_shards') != 0):
        raise RuntimeError('Original single-shard native source is not ready')


def request(path,timeout=15):
    with OPENER.open(ENDPOINT+path,timeout=timeout) as response:
        raw=response.read(4*1024*1024+1)
    if len(raw)>4*1024*1024:raise RuntimeError('Native response exceeds budget')
    body=json.loads(raw)
    if 'error' in body:raise RuntimeError('Native Elasticsearch returned an error')
    return body

def observe_source():
    source=json.loads((STATE/'source.json').read_bytes());hashes=[]
    for id in source['LiveIds']+[source['DeletedId']]:
        native=request('/oasis-holons/_doc/'+id+'?realtime=true&_source=true')
        if native.get('_index')!='oasis-holons' or native.get('_id')!=id or native.get('found') is not True or native.get('_version')!=1:
            raise RuntimeError('Original Elasticsearch identity/version differs')
        intent=json.loads((STATE/('bootstrap-'+id+'.intent.json')).read_bytes())
        ack=json.loads((STATE/('bootstrap-'+id+'.ack.json')).read_bytes())
        source_hash=hashlib.sha256((STATE/'source.json').read_bytes()).hexdigest()
        if intent['sourceSha256'].lower()!=source_hash or ack['sourceSha256'].lower()!=source_hash or ack['version']!=1:
            raise RuntimeError('Original document intent/ACK differs')
        with OPENER.open(ENDPOINT+'/oasis-holons/_source/'+id+'?realtime=true',timeout=15) as response:raw=response.read(1024*1024+1)
        if len(raw)>1024*1024 or hashlib.sha256(raw).hexdigest()!=intent['payloadSha256'].lower():
            raise RuntimeError('Original acknowledged canonical payload differs')
        hashes.append(hashlib.sha256(raw).hexdigest())
    try:
        request('/oasis-holons/_doc/'+source['MissingId']+'?realtime=true&_source=true')
    except urllib.error.HTTPError as error:
        if error.code!=404:raise
        try:raw=error.read(16385)
        finally:error.close()
        if len(raw)>16384:raise RuntimeError('Missing-control response exceeds budget')
        absent=json.loads(raw)
        if absent.get('_index')!='oasis-holons' or absent.get('_id')!=source['MissingId'] or absent.get('found') is not False or '_source' in absent or 'error' in absent:
            raise RuntimeError('Native response did not prove original absent control')
    else:raise RuntimeError('Original absent control is present')
    return hashes



def verify_runtime():
    raw = (STATE / 'runtime-files.json').read_bytes()
    if hashlib.sha256(raw).hexdigest() != 'a62eac761035e14e3327308430105c9aa7a803a2ecb6cf9862cae972aaa3d568':
        raise RuntimeError('Pinned Elasticsearch manifest differs')
    for item in json.loads(raw):
        p = (STATE / item['path']).resolve()
        if not p.is_relative_to(HOME.resolve()) or hashlib.sha256(p.read_bytes()).hexdigest() != item['sha256']:
            raise RuntimeError('Pinned Elasticsearch runtime differs: ' + item['path'])
    raw = (STATE / 'bundled-jdk-manifest.json').read_bytes()
    if hashlib.sha256(raw).hexdigest() != '89bbce38a7fecebd6294288887796e0816a3b990f3c029c3412f48ea4ebd6519':
        raise RuntimeError('Pinned bundled JDK manifest differs')
    for name, digest in json.loads(raw).items():
        path = (STATE / name).resolve()
        if not path.is_relative_to(JAVA.resolve()) or hashlib.sha256(path.read_bytes()).hexdigest() != digest:
            raise RuntimeError('Pinned bundled JDK runtime differs')


class NativeProcess:
    def __init__(self, pid):
        self.pid = pid
        self.kernel = ctypes.WinDLL('kernel32', use_last_error=True)
        self.kernel.OpenProcess.argtypes = [wintypes.DWORD, wintypes.BOOL, wintypes.DWORD]
        self.kernel.OpenProcess.restype = wintypes.HANDLE
        self.kernel.GetExitCodeProcess.argtypes = [wintypes.HANDLE, ctypes.POINTER(wintypes.DWORD)]
        self.kernel.GetExitCodeProcess.restype = wintypes.BOOL
        self.kernel.WaitForSingleObject.argtypes = [wintypes.HANDLE, wintypes.DWORD]
        self.kernel.WaitForSingleObject.restype = wintypes.DWORD
        self.kernel.CloseHandle.argtypes = [wintypes.HANDLE]
        self.kernel.CloseHandle.restype = wintypes.BOOL
        self.handle = self.kernel.OpenProcess(0x100000 | 0x1000, False, pid)
        if not self.handle:
            raise ctypes.WinError(ctypes.get_last_error())

    def wait(self, milliseconds):
        result = self.kernel.WaitForSingleObject(self.handle, milliseconds)
        if result == 258:
            return None
        if result != 0:
            raise ctypes.WinError(ctypes.get_last_error())
        code = wintypes.DWORD()
        if not self.kernel.GetExitCodeProcess(self.handle, ctypes.byref(code)):
            raise ctypes.WinError(ctypes.get_last_error())
        return code.value

    def close(self):
        if not self.kernel.CloseHandle(self.handle):
            raise ctypes.WinError(ctypes.get_last_error())


class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, *args):
        return None


OPENER = urllib.request.build_opener(urllib.request.ProxyHandler({}), NoRedirect())


def run():
    parser = argparse.ArgumentParser()
    mode = parser.add_mutually_exclusive_group(required=True)
    mode.add_argument('--preflight', action='store_true')
    mode.add_argument('--initialize', action='store_true')
    mode.add_argument('--retained', action='store_true')
    mode.add_argument('--paired', action='store_true')
    parser.add_argument('--serve-run-id')
    args = parser.parse_args()
    if args.serve_run_id and (not args.paired or len(args.serve_run_id) != 32 or uuid.UUID(hex=args.serve_run_id).hex != args.serve_run_id):
        raise RuntimeError('An explicit paired interactive run ID is required')
    with (STATE / 'runner.lock').open('a+b') as lock:
        lock.seek(0)
        msvcrt.locking(lock.fileno(), msvcrt.LK_NBLCK, 1)
        run_owned(args)


def run_owned(args):
    if args.initialize and any((STATE / name).exists() for name in ['source.json', 'bootstrap.intent.json', 'bootstrap.ack.json']):
        raise RuntimeError('Existing source or bootstrap outcome; initialization cannot replay it')
    if (args.retained or getattr(args, 'paired', False)) and not (STATE / 'bootstrap.ack.json').is_file():
        raise RuntimeError('Acknowledged retained native source required')
    verify_runtime()
    if (STATE / 'mongo.pending.json').exists():
        raise RuntimeError('Unresolved original Mongo outcome; preserve without replay')
    for port in ([19400, 19500, 5139] if getattr(args, 'paired', False) else [19400, 19500]):
        with socket.socket() as s:
            if s.connect_ex(('127.0.0.1', port)) == 0:
                raise RuntimeError('Native port occupied; no process adoption')
    config = STATE / 'config'
    if not config.exists():
        config.mkdir()
        for name in ['jvm.options', 'log4j2.properties']:
            shutil.copyfile(HOME / 'config' / name, config / name)
        write_once(config / 'elasticsearch.yml', expected_node_config())
    if (config / 'elasticsearch.yml').read_bytes() != expected_node_config():
        raise RuntimeError('Owned loopback/configuration contract differs; no node startup')
    if args.retained or getattr(args, 'paired', False):
        admission = json.loads((STATE / 'source.admission.json').read_bytes())
        if admission['sourceSha256'] != hashlib.sha256((STATE / 'source.json').read_bytes()).hexdigest():
            raise RuntimeError('Original source descriptor differs; no node startup')
        if admission['configSha256'] != hashlib.sha256((config / 'elasticsearch.yml').read_bytes()).hexdigest():
            raise RuntimeError('Original source configuration differs; no node startup')
    phase = ('interactive-' + args.serve_run_id) if args.serve_run_id else ('preflight-' if args.preflight else 'initial-' if args.initialize else 'paired-' if args.paired else 'retained-') + uuid.uuid4().hex
    pidfile = STATE / (phase + '.pid')
    (STATE / 'tmp').mkdir(exist_ok=True)
    env = dict(os.environ, ES_JAVA_HOME=str(JAVA), ES_PATH_CONF=str(config),
               ES_TMPDIR=str(STATE / 'tmp'),
               ES_JAVA_OPTS='-Xms128m -Xmx512m -XX:ActiveProcessorCount=2')
    for key in ['JAVA_TOOL_OPTIONS', 'JDK_JAVA_OPTIONS', '_JAVA_OPTIONS', 'COSMIC_ELASTICSEARCH_CQL_USERNAME', 'COSMIC_ELASTICSEARCH_CQL_PASSWORD']:
        env.pop(key, None)
    startup = subprocess.STARTUPINFO()
    startup.dwFlags |= subprocess.STARTF_USESHOWWINDOW
    startup.wShowWindow = 0
    command = ['cmd.exe', '/d', '/c', str(HOME / 'bin/elasticsearch.bat'), '-d', '-p', str(pidfile)]
    server_log = STATE / 'logs' / (CLUSTER + '.log')
    log_offset = server_log.stat().st_size if server_log.exists() else 0
    write_once(STATE / (phase + '-intent.json'), {'command': command, 'sourceWritesRequested': args.initialize})
    with (STATE / (phase + '-launcher.log')).open('xb') as log:
        launcher = subprocess.Popen(command, env=env, cwd=HOME, stdout=log, stderr=subprocess.STDOUT,
                                    startupinfo=startup, creationflags=subprocess.CREATE_NEW_CONSOLE)
        write_once(STATE / (phase + '-launcher.json'), {'pid': launcher.pid, 'ownedHiddenConsole': True})
        code = launcher.wait(timeout=120)
        if code != 0:
            raise RuntimeError('Shipped daemon launcher failed; preserve original output')
    native = NativeProcess(int(pidfile.read_text().strip()))
    write_once(STATE / (phase + '-process.json'), {'pid': native.pid, 'pidfile': str(pidfile)})
    try:
        opener = urllib.request.build_opener(urllib.request.ProxyHandler({}), NoRedirect())
        with opener.open('http://127.0.0.1:19400/', timeout=15) as r:
            if r.headers.get('X-Elastic-Product') != 'Elasticsearch':
                raise RuntimeError('Native product header differs')
            raw = r.read(65537)
        if len(raw) > 65536:
            raise RuntimeError('Native root response exceeds budget')
        root = json.loads(raw)
        if root['cluster_name'] != CLUSTER or root['version']['number'] != '8.19.24':
            raise RuntimeError('Native version or cluster differs')
        write_once(STATE / (phase + '-ready.json'), root)
        def observe(path, timeout=15):
            with opener.open('http://127.0.0.1:19400' + path, timeout=timeout) as response:
                raw = response.read(4 * 1024 * 1024 + 1)
            if len(raw) > 4 * 1024 * 1024:
                raise RuntimeError('Native observation exceeds budget')
            return json.loads(raw)
        settings = observe('/_cluster/settings?include_defaults=true&flat_settings=true')
        key = 'cluster.routing.allocation.disk.watermark.high'
        high = settings['transient'].get(key, settings['persistent'].get(key, settings['defaults'][key]))
        if high != '90%':
            raise RuntimeError('Expected native default high disk watermark differs')
        nodes = observe('/_nodes/stats/fs')['nodes']
        if len(nodes) != 1:
            raise RuntimeError('Expected one owned local node')
        fs = next(iter(nodes.values()))['fs']['total']
        capacity = {'highWatermark': high, 'totalBytes': fs['total_in_bytes'],
                    'availableBytes': fs['available_in_bytes'],
                    'minimumAvailableBytes': (fs['total_in_bytes'] + 9) // 10}
        capacity['newIndexCapacityReady'] = capacity['availableBytes'] >= capacity['minimumAvailableBytes']
        write_once(STATE / (phase + '-capacity.json'), capacity)
        if not args.preflight:
            if args.initialize:
                if not capacity['newIndexCapacityReady']:
                    raise RuntimeError('Native default disk capacity gate remains closed; no source IDs or records created')
                ids = [str(uuid.uuid4()) for _ in range(5)]
                write_once(STATE / 'source.json', {'Endpoint': 'http://127.0.0.1:19400', 'Actor': str(uuid.uuid4()),
                           'LiveIds': ids[:3], 'DeletedId': ids[3], 'MissingId': ids[4]})
                write_once(STATE / 'source.admission.json', {
                    'sourceSha256': hashlib.sha256((STATE / 'source.json').read_bytes()).hexdigest(),
                    'configSha256': hashlib.sha256((config / 'elasticsearch.yml').read_bytes()).hexdigest()})
            admission = json.loads((STATE / 'source.admission.json').read_bytes())
            source_hash = hashlib.sha256((STATE / 'source.json').read_bytes()).hexdigest()
            if admission['sourceSha256'] != source_hash or admission['configSha256'] != hashlib.sha256((config / 'elasticsearch.yml').read_bytes()).hexdigest():
                raise RuntimeError('Original native source identity or configuration differs')
            env['COSMIC_ELASTICSEARCH_CQL_SOURCE'] = str(STATE / 'source.json')
            if args.initialize:
                write_once(STATE / (phase + '-bootstrap.json'), run_test('ElasticsearchCqlBootstrap', phase + '-bootstrap', env))
            ack = json.loads((STATE / 'bootstrap.ack.json').read_bytes())
            if ack['sourceSha256'].lower() != source_hash or ack['createdRecords'] != 4:
                raise RuntimeError('Original native bootstrap acknowledgement differs')
            health = observe('/_cluster/health/oasis-holons?wait_for_status=green&wait_for_no_initializing_shards=true&timeout=30s', timeout=35)
            validate_source_health(health)
            write_once(STATE / (phase + '-source-ready.json'), health)
            before = observe_source()
            env['OASIS_ELASTICSEARCH_MONGO_SERVE'] = '1' if args.serve_run_id else '0'
            result = run_test('ElasticsearchMongoCqlNative' if args.paired else 'ElasticsearchCqlNative', phase + '-query', env)
            if observe_source() != before:
                raise RuntimeError('Original native documents changed during consumer operation')
            if args.paired and ((STATE / 'mongo.pending.json').exists() or (STATE / 'demo-ready.json').exists()):
                raise RuntimeError('Paired fixture cleanup incomplete')
            if hashlib.sha256((STATE / 'source.json').read_bytes()).hexdigest() != source_hash:
                raise RuntimeError('Native source descriptor changed during qualification')
            write_once(STATE / (phase + '-result.json'), dict(result, sourceSha256=source_hash,
                       bootstrapThisRun=args.initialize, originalDocumentsUnchanged=True))
    finally:
        try:
            signal = subprocess.run([sys.executable, '-B', str(ROOT / 'Scripts/TestHosts/Signal-OwnedConsoleProcess.py'),
                                     '--pid', str(native.pid)], capture_output=True, timeout=15)
            write_once(STATE / (phase + '-signal.log'), signal.stdout + signal.stderr)
            if signal.returncode:
                raise RuntimeError('Owned native console signal failed; preserve native PID')
            receipt = json.loads(signal.stdout)
            code = native.wait(45000)
            closed = current_node_closed(server_log, log_offset)
            write_once(STATE / (phase + '-stopped.json'), dict(receipt, exitCode=code, nodeClosed=closed))
            if not receipt['ctrlCSent'] or code not in (0, 130) or not closed:
                raise RuntimeError('Native node did not prove complete shutdown')
        finally:
            native.close()
    print(json.dumps({'completed': True, 'phase': phase, 'sourceWritesRequested': args.initialize, **capacity}))


if __name__ == '__main__':
    run()
