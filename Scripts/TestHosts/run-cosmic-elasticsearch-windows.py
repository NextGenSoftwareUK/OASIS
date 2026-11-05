"""Qualify the pinned Windows Elasticsearch launcher and its owned native node."""
import argparse
import ctypes
from ctypes import wintypes
import hashlib
import json
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

ROOT = Path(__file__).resolve().parents[2]
STATE = ROOT / 'LocalState/COSMIC/RecoveryLaptopElasticsearch81924'
HOME = STATE / 'elasticsearch-8.19.24'
JAVA = HOME / 'jdk'
CLUSTER = 'cosmic-cql-elasticsearch-recovery'


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


def run():
    parser = argparse.ArgumentParser()
    parser.add_argument('--preflight', action='store_true', required=True)
    parser.parse_args()
    verify_runtime()
    for port in [19400, 19500]:
        with socket.socket() as s:
            if s.connect_ex(('127.0.0.1', port)) == 0:
                raise RuntimeError('Native port occupied; no process adoption')
    config = STATE / 'config'
    if not config.exists():
        config.mkdir()
        for name in ['jvm.options', 'log4j2.properties']:
            shutil.copyfile(HOME / 'config' / name, config / name)
        write_once(config / 'elasticsearch.yml', (
            f'cluster.name: {CLUSTER}\nnode.name: cosmic-owned-local\n'
            'network.host: 127.0.0.1\nhttp.port: 19400\ntransport.port: 19500\n'
            'discovery.type: single-node\nxpack.security.enabled: false\n'
            'xpack.ml.enabled: false\ningest.geoip.downloader.enabled: false\n'
            'action.auto_create_index: false\n'
            f'path.data: "{(STATE / "data").as_posix()}"\n'
            f'path.logs: "{(STATE / "logs").as_posix()}"\n').encode())
    phase = 'preflight-' + uuid.uuid4().hex
    pidfile = STATE / (phase + '.pid')
    (STATE / 'tmp').mkdir(exist_ok=True)
    env = dict(os.environ, ES_JAVA_HOME=str(JAVA), ES_PATH_CONF=str(config),
               ES_TMPDIR=str(STATE / 'tmp'),
               ES_JAVA_OPTS='-Xms128m -Xmx512m -XX:ActiveProcessorCount=2')
    for key in ['JAVA_TOOL_OPTIONS', 'JDK_JAVA_OPTIONS', '_JAVA_OPTIONS']:
        env.pop(key, None)
    startup = subprocess.STARTUPINFO()
    startup.dwFlags |= subprocess.STARTF_USESHOWWINDOW
    startup.wShowWindow = 0
    command = ['cmd.exe', '/d', '/c', str(HOME / 'bin/elasticsearch.bat'), '-d', '-p', str(pidfile)]
    server_log = STATE / 'logs' / (CLUSTER + '.log')
    log_offset = server_log.stat().st_size if server_log.exists() else 0
    write_once(STATE / (phase + '-intent.json'), {'command': command, 'sourceWrites': False})
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
        def observe(path):
            with opener.open('http://127.0.0.1:19400' + path, timeout=15) as response:
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
    print(json.dumps({'preflightPassed': True, 'phase': phase, 'sourceWrites': False, **capacity}))


if __name__ == '__main__':
    run()
