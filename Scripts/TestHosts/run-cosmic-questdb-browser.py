"""Retained QuestDB symbol source and journalled local Mongo/browser consumer."""
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
import urllib.parse
import urllib.request
import uuid
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
CACHE = ROOT / 'LocalState/COSMIC/RecoveryLaptopQuestDB1010'
HOME = CACHE / 'questdb-10.1.0-rt-windows-x86-64'
STATE = CACHE / 'browser-source'
PROJECT = ROOT / 'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.QuestDBOASIS.IntegrationTests'


def write_once(path, value):
    with path.open('xb') as f:
        f.write(value if isinstance(value, bytes) else json.dumps(value, indent=2).encode())
        f.flush()
        os.fsync(f.fileno())


class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, *args):
        return None


OPENER = urllib.request.build_opener(urllib.request.ProxyHandler({}), NoRedirect())


def sql(statement):
    endpoint = 'http://127.0.0.1:29000/api/v1/sql/execute?query=' + urllib.parse.quote(statement, safe='')
    with OPENER.open(endpoint, timeout=15) as response:
        raw = response.read(65537)
    if len(raw) > 65536:
        raise RuntimeError('Native response exceeds budget')
    result = json.loads(raw)
    if 'error' in result:
        raise RuntimeError('QuestDB returned native SQL failure')
    return result


def observe():
    result = sql('select symbol from cql_demo_symbols order by symbol')
    if result.get('columns') != [{'name': 'symbol', 'type': 'SYMBOL'}] or result.get('dataset') != [['BTC-USDT'], ['ETH-USDT'], ['SOL-USDT']] or result.get('count') != 3:
        raise RuntimeError('Original symbol table schema or exact records differ')
    return hashlib.sha256(json.dumps({'columns': result['columns'], 'dataset': result['dataset'], 'count': result['count']}, sort_keys=True).encode()).hexdigest()


def verify_source_admission(state):
    admission = json.loads((state / 'source.admission.json').read_bytes())
    for name, field in [('source.json', 'sourceSha256'), ('conf/server.conf', 'configSha256')]:
        if hashlib.sha256((state / name).read_bytes()).hexdigest() != admission[field]:
            raise RuntimeError('Admitted QuestDB source identity or configuration changed')


def run():
    parser = argparse.ArgumentParser()
    parser.add_argument('--initialize', action='store_true')
    parser.add_argument('--serve', action='store_true')
    args = parser.parse_args()
    if args.initialize and args.serve:
        raise RuntimeError('Initialize and accept the native source before interactive use')
    raw = (CACHE / 'browser-runtime-manifest.json').read_bytes()
    if hashlib.sha256(raw).hexdigest() != '3e53e13800098a9b341d83449d608a391978136f2d49bb52a71e2900ed8b0bff':
        raise RuntimeError('Previously admitted runtime inventory differs')
    for name, digest in json.loads(raw).items():
        path = (CACHE / name).resolve()
        if not path.is_relative_to(HOME.resolve()) or hashlib.sha256(path.read_bytes()).hexdigest() != digest:
            raise RuntimeError('Retained QuestDB runtime changed')
    if args.initialize:
        STATE.mkdir(exist_ok=False)
        (STATE / 'conf').mkdir()
        write_once(STATE / 'conf/server.conf', b'http.bind.to=127.0.0.1:29000\nhttp.min.enabled=false\npg.enabled=false\nline.tcp.enabled=false\nline.udp.enabled=false\ntelemetry.enabled=false\nshared.worker.count=2\nwal.apply.worker.count=1\n')
        write_once(STATE / 'source.json', {'schema': 1, 'actor': str(uuid.uuid4()), 'symbols': ['BTC-USDT', 'ETH-USDT', 'SOL-USDT']})
        write_once(STATE / 'source.admission.json', {
            'sourceSha256': hashlib.sha256((STATE / 'source.json').read_bytes()).hexdigest(),
            'configSha256': hashlib.sha256((STATE / 'conf/server.conf').read_bytes()).hexdigest()})
    elif not (STATE / 'bootstrap.ack.json').is_file():
        raise RuntimeError('Accepted retained browser source required; no automatic initialization')
    verify_source_admission(STATE)
    with (STATE / 'runner.lock').open('a+b') as lock:
        lock.seek(0)
        msvcrt.locking(lock.fileno(), msvcrt.LK_NBLCK, 1)
        if (STATE / 'active-process.json').exists() or (STATE / 'mongo.pending.json').exists():
            raise RuntimeError('Unresolved original process or Mongo outcome; preserve without replay')
        for port in [29000, 5134]:
            with socket.socket() as s:
                if s.connect_ex(('127.0.0.1', port)) == 0:
                    raise RuntimeError('Required port occupied; no process adoption')
        phase = ('initial-' if args.initialize else 'interactive-' if args.serve else 'paired-') + uuid.uuid4().hex
        results = CACHE / phase
        results.mkdir()
        startup = subprocess.STARTUPINFO()
        startup.dwFlags |= subprocess.STARTF_USESHOWWINDOW
        startup.wShowWindow = 0
        command = [str(HOME / 'bin/java.exe'), '-Xms64m', '-Xmx256m', '-XX:ActiveProcessorCount=2',
                   '--enable-native-access=io.questdb', '--sun-misc-unsafe-memory-access=allow',
                   '--add-opens=java.base/java.lang=io.questdb', '--add-opens=java.base/java.lang.reflect=io.questdb',
                   '--add-opens=java.base/java.nio=io.questdb', '--add-opens=java.base/java.time.zone=io.questdb',
                   '--add-exports=java.base/jdk.internal.vm=io.questdb',
                   '-m', 'io.questdb/io.questdb.ServerMain', '-d', str(STATE), '-f']
        with (results / 'server.log').open('xb') as output:
            process = subprocess.Popen(command, cwd=CACHE, stdout=output, stderr=subprocess.STDOUT,
                                       startupinfo=startup, creationflags=subprocess.CREATE_NEW_CONSOLE)
            write_once(STATE / 'active-process.json', {'pid': process.pid, 'phase': phase, 'ownedHiddenConsole': True})
            try:
                deadline = time.monotonic() + 40
                while True:
                    if process.poll() is not None:
                        raise RuntimeError('Owned QuestDB exited before readiness')
                    try:
                        build = sql('select build()')
                        break
                    except urllib.error.HTTPError:
                        raise
                    except OSError:
                        if time.monotonic() >= deadline:
                            raise
                        time.sleep(.2)
                if '10.1.0' not in json.dumps(build):
                    raise RuntimeError('Native QuestDB build identity differs')
                write_once(results / 'native-build.json', build)
                if args.initialize:
                    write_once(STATE / 'bootstrap.intent.json', {'maximumRows': 3, 'table': 'cql_demo_symbols'})
                    write_once(STATE / 'create.ack.json', sql('CREATE TABLE cql_demo_symbols (symbol SYMBOL)'))
                    write_once(STATE / 'insert.intent.json', {'symbols': ['BTC-USDT', 'ETH-USDT', 'SOL-USDT']})
                    write_once(STATE / 'insert.ack.json', sql("INSERT INTO cql_demo_symbols VALUES ('BTC-USDT'), ('ETH-USDT'), ('SOL-USDT')"))
                    write_once(STATE / 'bootstrap.ack.json', {'sourceHash': observe(), 'records': 3})
                before = observe()
                if before != json.loads((STATE / 'bootstrap.ack.json').read_bytes())['sourceHash']:
                    raise RuntimeError('Acknowledged source differs')
                env = dict(os.environ, COSMIC_QUESTDB_BROWSER_SOURCE=str(STATE), COSMIC_QUESTDB_BROWSER_SERVE='1' if args.serve else '0')
                with (results / 'consumer.log').open('xb') as log:
                    subprocess.run(['dotnet', 'test', str(PROJECT), '--no-build', '--no-restore', '--filter',
                                    'TestCategory=QuestDbBrowserNative', '/m:2', '/nr:false', '/p:UseSharedCompilation=false',
                                    '--results-directory', str(results), '--logger', 'trx;LogFileName=consumer.trx'],
                                   env=env, stdout=log, stderr=subprocess.STDOUT, check=True)
                trx = results / 'consumer.trx'
                counters = ET.parse(trx).find('.//{*}Counters').attrib
                if counters['total'] != '1' or counters['passed'] != '1' or counters['notExecuted'] != '0':
                    raise RuntimeError('Exactly one passed consumer with no skips required')
                if observe() != before or (STATE / 'mongo.pending.json').exists() or (STATE / 'demo-ready.json').exists():
                    raise RuntimeError('Source preservation or consumer cleanup incomplete')
                verify_source_admission(STATE)
                write_once(results / 'acceptance.json', {'passed': 1, 'sourceUnchanged': True, 'sourceHash': before,
                           'trxSha256': hashlib.sha256(trx.read_bytes()).hexdigest(), 'phase': phase})
            finally:
                if process.poll() is None:
                    signal = subprocess.run([sys.executable, '-B', str(ROOT / 'Scripts/TestHosts/Signal-OwnedConsoleProcess.py'),
                                             '--pid', str(process.pid)], capture_output=True, timeout=15)
                    write_once(results / 'signal.log', signal.stdout + signal.stderr)
                    if signal.returncode:
                        raise RuntimeError('Owned QuestDB signal failed; preserve PID')
                    receipt = json.loads(signal.stdout)
                    process.wait(timeout=45)
                    output.flush()
                    log = (results / 'server.log').read_text(encoding='utf-8')
                    closed = 'QuestDB is shutdown.' in log and 'shutdown is incomplete' not in log
                    write_once(results / 'stopped.json', dict(receipt, exitCode=process.returncode, nativeClosed=closed))
                    if process.returncode not in (0, 130) or not closed or not receipt['ctrlCSent']:
                        raise RuntimeError('Native QuestDB shutdown incomplete')
                    (STATE / 'active-process.json').unlink()
                else:
                    raise RuntimeError('Native node exited unexpectedly; preserve original process marker')
        print(json.dumps({'phase': phase, 'results': str(results), 'passed': True}))


if __name__ == '__main__':
    run()
