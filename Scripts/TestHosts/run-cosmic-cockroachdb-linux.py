"""Verify original native SQL/CQL records across two owned secured process starts."""
from pathlib import Path
import csv, fcntl, hashlib, io, json, os, re, signal, socket, subprocess, time
import xml.etree.ElementTree as ET

ROOT=Path(__file__).resolve().parents[2]
STATE=Path('/var/tmp/oasis-cockroachdb-cql')
RUNTIME=Path('/var/tmp/oasis-runtimes/cockroach-26.2.9/cockroach')
HASH='7e2a7a869f52c083343dcefc01852a3f34f15292c0a4cc63895182296d2d778f'
RESULTS=ROOT/'TestResults/CockroachDbCqlCurrent'
DLL=ROOT/'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.CockroachDBOASIS.IntegrationTests/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Providers.CockroachDBOASIS.IntegrationTests.dll'
DOTNET='/var/tmp/oasis-runtimes/dotnet-10.0.12/dotnet'
VSTEST='/mnt/c/Users/david/.nuget/packages/microsoft.testplatform.portable/18.11.0/tools/net8.0/vstest.console.dll'

def one(statement):
    output=subprocess.check_output([str(RUNTIME),'sql','--host=localhost:26259','--user=root',
        '--certs-dir='+str(STATE/'certs'),'--format=tsv','--execute='+statement],text=True,stderr=subprocess.PIPE)
    rows=list(csv.reader(io.StringIO(output),delimiter='\t'))
    assert len(rows)==2 and len(rows[1])==1
    return rows[1][0]

def main():
    assert os.getuid()!=0 and STATE.stat().st_uid==os.getuid()
    assert hashlib.sha256(RUNTIME.read_bytes()).hexdigest()==HASH
    assert not list(STATE.glob('*.pending.json')),'Reconcile original pending outcome, never replay'
    original=json.loads((STATE/'environment.json').read_text())['observations'][0]
    connection='Host=localhost;Port=26259;Database=oasis;Username=root;SSL Mode=VerifyFull;Root Certificate='+str(STATE/'certs/ca.crt')+';SSL Certificate='+str(STATE/'certs/client.root.crt')+';SSL Key='+str(STATE/'certs/client.root.key')+';Search Path=cosmic_cql,pg_catalog;Pooling=false;Timeout=5;Command Timeout=15'
    proofs=[]
    with (STATE/'runner.lock').open('a') as lock:
        fcntl.flock(lock,fcntl.LOCK_EX|fcntl.LOCK_NB)
        for phase in ['native-first','native-restart']:
            for port in [26259,18004]:
                with socket.socket() as probe:assert probe.connect_ex(('127.0.0.1',port))!=0
            with (RESULTS/(phase+'-server.log')).open('w') as log:
                node=subprocess.Popen([str(RUNTIME),'start-single-node','--certs-dir='+str(STATE/'certs'),
                    '--store='+str(STATE/'data'),'--listen-addr=127.0.0.1:26259','--advertise-addr=localhost:26259',
                    '--http-addr=127.0.0.1:18004','--cache=128MiB','--max-sql-memory=128MiB',
                    '--log='+json.dumps({'file-defaults':{'dir':str(RESULTS/'server-logs')}})],
                    env={**os.environ,'COCKROACH_SKIP_ENABLING_DIAGNOSTIC_REPORTING':'true'},stdout=log,stderr=subprocess.STDOUT)
                try:
                    deadline=time.monotonic()+30
                    last_error=None
                    while time.monotonic()<deadline:
                        assert node.poll() is None,'Owned native server exited; retain source'
                        try:
                            assert one('SELECT 1')=='1';break
                        except subprocess.CalledProcessError as error:last_error=error.stderr;time.sleep(.25)
                    else:raise RuntimeError('SQL readiness deadline: '+str(last_error))
                    ids=re.findall(r'^clusterID:\s+([0-9a-f-]+)',Path(log.name).read_text(),re.MULTILINE)
                    assert ids==[original['clusterId']]
                    assert one("SELECT oid::STRING FROM pg_database WHERE datname='oasis'")==original['databaseId']
                    started=time.time()
                    with (RESULTS/(phase+'.log')).open('w') as output:
                        subprocess.run([DOTNET,VSTEST,str(DLL),'--TestCaseFilter:FullyQualifiedName~CockroachDbCqlNativeTests',
                            '--ResultsDirectory:'+str(RESULTS),'--Logger:trx;LogFileName='+phase+'.trx'],
                            env={**os.environ,'DOTNET_ROOT':str(Path(DOTNET).parent),'OASIS_COCKROACH_CQL_CONNECTION':connection,
                                'OASIS_COCKROACH_CQL_STATE':str(STATE)},cwd=RESULTS,stdout=output,stderr=subprocess.STDOUT,check=True)
                    report=RESULTS/(phase+'.trx');assert report.stat().st_mtime>=started
                    counts=ET.parse(report).find('.//{*}Counters').attrib
                    assert counts['total']==counts['passed']=='1' and counts['notExecuted']=='0'
                    proofs.append({'phase':phase,'passed':1,'sha256':hashlib.sha256(report.read_bytes()).hexdigest()})
                finally:
                    if node.poll() is None:node.send_signal(signal.SIGTERM)
                    assert node.wait(timeout=45)==0,'Owned shutdown incomplete; retain source'
    assert not list(STATE.glob('*.pending.json'))
    print(json.dumps({'passed':True,'proofs':proofs,'stopped':True,
        'stateBytes':sum(p.stat().st_size for p in STATE.rglob('*') if p.is_file())}))

if __name__=='__main__':main()
