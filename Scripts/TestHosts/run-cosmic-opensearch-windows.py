"""One journalled OpenSearch source, shipped JVM options, owned console shutdown."""
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
import xml.etree.ElementTree as ET

ROOT=Path(__file__).resolve().parents[2]
STATE=ROOT/'LocalState/COSMIC/RecoveryLaptopOpenSearch390'
INSTALL=STATE/'opensearch-3.9.0'
JAVA_STATE=ROOT/'LocalState/COSMIC/RecoveryLaptopSolr1010'
JAVA=JAVA_STATE/'jdk-21.0.13+9-jre/bin/java.exe'
ENDPOINT='http://127.0.0.1:19200'
CLUSTER='cosmic-cql-opensearch-recovery'
PROJECT=ROOT/'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.OpenSearchOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.OpenSearchOASIS.IntegrationTests.csproj'

class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self,*args):return None
OPENER=urllib.request.build_opener(urllib.request.ProxyHandler({}),NoRedirect())

def write_once(path,value):
    with path.open('xb') as f:
        f.write(value if isinstance(value,bytes) else json.dumps(value,indent=2).encode());f.flush();os.fsync(f.fileno())

def verify_runtimes():
    for directory,digest,prefix in [(STATE,'bbc42bc9d2a1a9aa164083270dec1e921aedc9eec2033e2f0b0a31c6199764b8',''),
        (JAVA_STATE,'7f8e68dd01566a36e0b0c441481b3fcc477a63fa153a973eee8b04486ce32520','jdk-21.0.13+9-jre/')]:
        raw=(directory/'runtime-manifest.json').read_bytes()
        if hashlib.sha256(raw).hexdigest()!=digest:raise RuntimeError('Pinned runtime manifest differs')
        for name,expected in json.loads(raw).items():
            if prefix and not name.startswith(prefix):continue
            path=(directory/name).resolve()
            if not path.is_relative_to(directory.resolve()) or hashlib.sha256(path.read_bytes()).hexdigest()!=expected:
                raise RuntimeError('Pinned runtime file differs: '+name)

def request(path,timeout=15):
    with OPENER.open(ENDPOINT+path,timeout=timeout) as response:
        raw=response.read(4*1024*1024+1)
    if len(raw)>4*1024*1024:raise RuntimeError('Native response exceeds budget')
    body=json.loads(raw)
    if 'error' in body:raise RuntimeError('Native OpenSearch returned an error')
    return body

def split_windows_options(text):
    shell=ctypes.WinDLL('shell32',use_last_error=True);kernel=ctypes.WinDLL('kernel32',use_last_error=True)
    shell.CommandLineToArgvW.argtypes=[wintypes.LPCWSTR,ctypes.POINTER(ctypes.c_int)];shell.CommandLineToArgvW.restype=ctypes.POINTER(wintypes.LPWSTR)
    kernel.LocalFree.argtypes=[wintypes.HLOCAL];kernel.LocalFree.restype=wintypes.HLOCAL
    count=ctypes.c_int();argv=shell.CommandLineToArgvW('java '+text,ctypes.byref(count))
    if not argv:raise ctypes.WinError(ctypes.get_last_error())
    try:return [argv[i] for i in range(1,count.value)]
    finally:kernel.LocalFree(argv)

def initialize_files():
    if (STATE/'source.json').exists() or (STATE/'config').exists() or (STATE/'data').exists():
        raise RuntimeError('Existing source/configuration requires reconciliation, never reseeding')
    shutil.copytree(INSTALL/'config',STATE/'config')
    (STATE/'config/opensearch.yml').write_text("cluster.name: "+CLUSTER+"\nnode.name: cosmic-owned-local\nnetwork.host: 127.0.0.1\nhttp.port: 19200\ntransport.port: 19300\ndiscovery.type: single-node\nplugins.security.disabled: true\naction.auto_create_index: false\npath.data: '"+(STATE/'data').as_posix()+"'\npath.logs: '"+(STATE/'logs').as_posix()+"'\n",encoding='utf-8')
    write_once(STATE/'source.json',dict(Endpoint=ENDPOINT,Actor=str(uuid.uuid4()),LiveIds=[str(uuid.uuid4()) for _ in range(3)],DeletedId=str(uuid.uuid4()),MissingId=str(uuid.uuid4())))

def assert_listener_owned(pid):
    powershell=os.environ['SystemRoot']+'/System32/WindowsPowerShell/v1.0/powershell.exe'
    query="$ErrorActionPreference='Stop'; @(Get-NetTCPConnection -State Listen -OwningProcess "+str(pid)+" | Select-Object LocalAddress,LocalPort,OwningProcess) | ConvertTo-Json -Compress"
    result=subprocess.run([powershell,'-NoProfile','-NonInteractive','-Command',query],stdout=subprocess.PIPE,stderr=subprocess.PIPE,creationflags=subprocess.CREATE_NO_WINDOW,timeout=15,check=True)
    rows=json.loads(result.stdout.decode('utf-8-sig'));rows=rows if isinstance(rows,list) else [rows]
    if not rows or any(row['OwningProcess']!=pid or row['LocalAddress'] not in ('127.0.0.1','::1') for row in rows) or not {19200,19300}.issubset({row['LocalPort'] for row in rows}):
        raise RuntimeError('Native listeners are not the exact owned loopback process')
    return rows

def readiness_health():
    try:
        health=request('/_cluster/health?wait_for_status=yellow&timeout=1s',timeout=3)
    except urllib.error.HTTPError as error:
        if error.code!=408:raise
        try:raw=error.read(65537)
        finally:error.close()
        if len(raw)>65536:raise RuntimeError('Native health timeout response exceeds budget')
        health=json.loads(raw)
        if health.get('timed_out') is not True:
            raise RuntimeError('Native408 did not prove an expired health wait')
    if health.get('cluster_name')!=CLUSTER or health.get('status') not in ('green','yellow','red') or not isinstance(health.get('timed_out'),bool):
        raise RuntimeError('Native health identity or state differs')
    return health


def wait_ready(process):
    until=time.monotonic()+120
    while True:
        if process.poll() is not None:raise RuntimeError('Owned OpenSearch exited before readiness')
        try:
            info=request('/',timeout=2)
            if info['version']['number']!='3.9.0' or info['version']['distribution']!='opensearch' or info['cluster_name']!=CLUSTER:
                raise RuntimeError('Native OpenSearch identity differs')
            health=readiness_health()
            if health['status'] in ('green','yellow') and not health['timed_out']:
                return {'identity':info,'health':health,'listeners':assert_listener_owned(process.pid)}
        except urllib.error.HTTPError as error:
            if error.code!=503:raise
            error.close()
        except (urllib.error.URLError,TimeoutError,ConnectionError):pass
        if time.monotonic()>=until:raise TimeoutError('Owned OpenSearch readiness deadline exceeded')
        time.sleep(.25)

def run_test(category,phase,env):
    results=ROOT/'TestResults/OpenSearchCqlNativeCurrent';results.mkdir(exist_ok=True)
    target=results/(phase+'.trx')
    if target.exists():raise RuntimeError('Refusing to overwrite native evidence')
    outcome=subprocess.run(['dotnet','test',str(PROJECT),'--no-build','--no-restore','--filter','TestCategory='+category,
        '--logger','trx;LogFileName='+target.name,'--results-directory',str(results),'/m:2','/nr:false','/p:UseSharedCompilation=false','-v:q'],env=env)
    if outcome.returncode:raise RuntimeError('Native '+category+' failed; preserve original dispatched work')
    ns={'t':'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
    counters=ET.parse(target).getroot().find('t:ResultSummary/t:Counters',ns).attrib
    if any(counters[k]!=v for k,v in [('total','1'),('passed','1'),('failed','0'),('notExecuted','0')]):raise RuntimeError('Native test counters differ')
    return dict(counters=counters,trxSha256=hashlib.sha256(target.read_bytes()).hexdigest())

def run():
    parser=argparse.ArgumentParser();parser.add_argument('--initialize',action='store_true');args=parser.parse_args()
    for number in [19200,19300]:
        with socket.socket() as port:port.bind(('127.0.0.1',number))
    verify_runtimes()
    if not json.loads((STATE/'console-v2.result.json').read_bytes())['passed']:raise RuntimeError('Owned console shutdown qualification required')
    if args.initialize:initialize_files()
    elif not (STATE/'bootstrap.ack.json').exists():raise RuntimeError('Acknowledged retained source required')
    raw=(STATE/'source.json').read_bytes();source_hash=hashlib.sha256(raw).hexdigest()
    if json.loads(raw)['Endpoint']!=ENDPOINT:raise RuntimeError('Fixed loopback source required')
    if not args.initialize:
        ack=json.loads((STATE/'bootstrap.ack.json').read_bytes())
        if ack['sourceSha256'].lower()!=source_hash or ack['createdRecords']!=4:raise RuntimeError('Original acknowledged source differs')
    phase='initial' if args.initialize else 'retained-'+uuid.uuid4().hex
    write_once(STATE/(phase+'-run.intent.json'),dict(sourceSha256=source_hash,bootstrapThisRun=args.initialize))
    (STATE/'tmp').mkdir(exist_ok=True);(STATE/'logs').mkdir(exist_ok=True)
    env=os.environ.copy()
    for key in ['JAVA_TOOL_OPTIONS','JDK_JAVA_OPTIONS','_JAVA_OPTIONS','COSMIC_OPENSEARCH_CQL_USERNAME','COSMIC_OPENSEARCH_CQL_PASSWORD']:env.pop(key,None)
    env.update(OPENSEARCH_HOME=str(INSTALL),OPENSEARCH_PATH_CONF=str(STATE/'config'),OPENSEARCH_TMPDIR=str(STATE/'tmp'),
        OPENSEARCH_JAVA_OPTS='-Xms128m -Xmx512m -XX:ActiveProcessorCount=2',COSMIC_OPENSEARCH_CQL_SOURCE=str(STATE/'source.json'))
    parsed=subprocess.run([str(JAVA),'-cp',str(INSTALL/'lib/*'),'org.opensearch.tools.launchers.JvmOptionsParser',str(STATE/'config')],cwd=INSTALL,env=env,stdout=subprocess.PIPE,stderr=subprocess.PIPE,creationflags=subprocess.CREATE_NO_WINDOW,timeout=30,check=True)
    write_once(STATE/(phase+'-options.log'),parsed.stdout+parsed.stderr)
    command=[str(JAVA)]+split_windows_options(parsed.stdout.decode().strip())+['-Dopensearch','-Dopensearch.path.home='+str(INSTALL),
        '-Dopensearch.path.conf='+str(STATE/'config'),'-Dopensearch.distribution.type=zip','-Dopensearch.bundled_jdk=true','-cp',str(INSTALL/'lib/*'),'org.opensearch.bootstrap.OpenSearch']
    si=subprocess.STARTUPINFO();si.dwFlags|=subprocess.STARTF_USESHOWWINDOW;si.wShowWindow=0
    with (STATE/(phase+'-server.log')).open('xb') as log:
        process=subprocess.Popen(command,cwd=INSTALL,env=env,stdin=subprocess.DEVNULL,stdout=log,stderr=subprocess.STDOUT,startupinfo=si,creationflags=subprocess.CREATE_NEW_CONSOLE)
        write_once(STATE/(phase+'-process.json'),dict(pid=process.pid,command=command,ownedHiddenConsole=True))
        try:
            write_once(STATE/(phase+'-ready.json'),wait_ready(process))
            if args.initialize:write_once(STATE/'bootstrap-run.result.json',run_test('OpenSearchCqlBootstrap','bootstrap',env))
            ack=json.loads((STATE/'bootstrap.ack.json').read_bytes())
            if ack['sourceSha256'].lower()!=source_hash or ack['createdRecords']!=4:raise RuntimeError('Native bootstrap acknowledgement differs')
            result=run_test('OpenSearchCqlNative',phase,env)
            write_once(STATE/(phase+'-run.result.json'),dict(sourceSha256=source_hash,bootstrapThisRun=args.initialize,**result))
        finally:
            signal_sent=False
            if process.poll() is None:
                stopped=subprocess.run([sys.executable,str(ROOT/'Scripts/TestHosts/Signal-OwnedConsoleProcess.py'),'--pid',str(process.pid)],stdout=subprocess.PIPE,stderr=subprocess.STDOUT,creationflags=subprocess.CREATE_NO_WINDOW,timeout=15)
                write_once(STATE/(phase+'-signal.log'),stopped.stdout)
                if stopped.returncode:raise RuntimeError('Owned native console signal failed; preserve PID for reconciliation')
                signal_sent=json.loads(stopped.stdout)['ctrlCSent']
                process.wait(timeout=45)
            log.flush()
            server_log=(STATE/(phase+'-server.log')).read_text(encoding='utf-8')
            node_closed='[cosmic-owned-local] stopped' in server_log and '[cosmic-owned-local] closed' in server_log
            write_once(STATE/(phase+'-stopped.json'),dict(pid=process.pid,exitCode=process.returncode,ctrlCSent=signal_sent,nodeClosed=node_closed))
            if not signal_sent or not node_closed or process.returncode not in (0,130):
                raise RuntimeError('Owned OpenSearch did not prove normal node shutdown')

if __name__=='__main__':run()
