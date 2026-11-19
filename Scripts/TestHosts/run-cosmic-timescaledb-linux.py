"""Run native TimescaleDB acceptance twice against one retained container/source."""
from pathlib import Path
import ssl, argparse, sys, base64, fcntl, hashlib, json, os, pwd, socket, subprocess, time, urllib.error, urllib.request
import xml.etree.ElementTree as ET
parser=argparse.ArgumentParser()
parser.add_argument('--federated',action='store_true');parser.add_argument('--serve',action='store_true')
options=parser.parse_args()
if options.serve and not options.federated:raise RuntimeError('Serve requires explicit federation')
sys.dont_write_bytecode=True
from cosmic_linux_mongo_service import owned_linux_mongo
ROOT=Path('/mnt/c/Source/OASIS-cosmic')
STATE=Path('/var/tmp/oasis-timescaledb-cql')
RESULTS=ROOT/'TestResults/TimescaleDbCqlCurrent'
DOTNET='/var/tmp/oasis-runtimes/dotnet-10.0.12/dotnet'
VSTEST='/mnt/c/Users/david/.nuget/packages/microsoft.testplatform.portable/18.11.0/tools/net8.0/vstest.console.dll'
DLL=ROOT/'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.TimescaleDBOASIS.IntegrationTests/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Providers.TimescaleDBOASIS.IntegrationTests.dll'
OWNER=pwd.getpwnam('dellams').pw_uid

def command(*args):return subprocess.check_output(['podman',*args],text=True).strip()
def inspect():
    value=json.loads(command('inspect','oasis-timescaledb-cql'))[0]
    original=json.loads((STATE/'environment.json').read_text())
    assert value['Id']==original['containerId']
    assert value['Image']==original['imageId']
    assert value['HostConfig']['PortBindings']['5432/tcp']==[{'HostIp':'127.0.0.1','HostPort':'15434'}]
    assert any(m['Source']==str(STATE/'data') and m['Destination']=='/var/lib/postgresql/data' for m in value['Mounts'])
    return value

def demote():
    os.setgroups([]);os.setgid(OWNER);os.setuid(OWNER)

def main():
    assert os.geteuid()==0,'Use existing rootful Podman owner'
    assert STATE.is_dir() and not list(STATE.glob('*.pending.json')),'Reconcile original pending outcome; never replay'
    assert inspect()['State']['Status']=='exited','Do not adopt a running service'
    config=json.loads((STATE/'credentials.json').read_text())
    connection='Host=localhost;Port=15434;Database=oasis;Username=oasis_test;Password='+config['password']+';SSL Mode=VerifyFull;Root Certificate='+str(STATE/'ca.pem')+';Search Path=cosmic_cql,pg_catalog;Pooling=false;Timeout=5;Command Timeout=15'
    def sql(query):
        return command('exec','--user','70','oasis-timescaledb-cql','psql','--no-psqlrc','-v','ON_ERROR_STOP=1','-U','oasis_test','-d','oasis','-At','-c',query)
    proofs=[]
    with (STATE/'runner.lock').open('a') as lock:
        fcntl.flock(lock,fcntl.LOCK_EX|fcntl.LOCK_NB)
        for phase in ['native-first','native-restart']:
            assert inspect()['State']['Status']=='exited'
            with socket.socket() as probe:assert probe.connect_ex(('127.0.0.1',15434))!=0
            command('start','oasis-timescaledb-cql')
            try:
                deadline=time.monotonic()+60
                while time.monotonic()<deadline:
                    assert inspect()['State']['Running'],'Owned service exited during readiness'
                    try:
                        if sql('SELECT 1')=='1':break
                    except subprocess.CalledProcessError:time.sleep(.25)
                else:raise RuntimeError('Native SQL readiness deadline')
                original=json.loads((STATE/'environment.json').read_text())['observations'][0]
                assert sql("SELECT extversion FROM pg_extension WHERE extname='timescaledb'")==original['extension']=='2.31.0'
                assert sql('SELECT system_identifier::TEXT FROM pg_control_system()')==original['systemIdentifier']
                assert sql("SELECT oid::TEXT FROM pg_database WHERE datname='oasis'")==original['databaseId']
                def report(name,filter_name='TimescaleDbCqlNativeTests',extra=None):
                    env={**os.environ,'DOTNET_ROOT':'/var/tmp/oasis-runtimes/dotnet-10.0.12','OASIS_TIMESCALE_CQL_STATE':str(STATE),'OASIS_TIMESCALE_CQL_CONNECTION':connection}
                    env.update(extra or {})
                    started=time.time()
                    with (RESULTS/(name+'.log')).open('w') as log:
                        subprocess.run([DOTNET,VSTEST,str(DLL),'--TestCaseFilter:FullyQualifiedName~'+filter_name,
                            '--ResultsDirectory:'+str(RESULTS),'--Logger:trx;LogFileName='+name+'.trx'],
                            env=env,cwd=RESULTS,preexec_fn=demote,stdout=log,stderr=subprocess.STDOUT,check=True)
                    trx=RESULTS/(name+'.trx');assert trx.stat().st_mtime>=started
                    counts=ET.parse(trx).find('.//{*}Counters').attrib
                    assert counts['passed']==counts['total']=='1' and counts['notExecuted']=='0'
                    proofs.append({'phase':name,'passed':1,'sha256':hashlib.sha256(trx.read_bytes()).hexdigest()})
                report(phase)
                if phase=='native-restart' and options.federated:
                    owner=Path('/var/tmp/oasis-cosmic-mongo/data').stat().st_uid
                    assert owner!=0
                    os.setresuid(owner,owner,0)
                    try:
                        with owned_linux_mongo(RESULTS) as mongo_env:
                            os.setresuid(0,0,0)
                            try:
                                report('mongo-init','LinuxMongoDemoEnvironmentTests',mongo_env)
                                report('mongo-federation','TimescaleDBMongoCqlNativeTests',
                                    {**mongo_env,'COSMIC_TIMESCALEDB_MONGO_SERVE':'1' if options.serve else '0'})
                                assert not (STATE/'mongo.pending.json').exists(),'Mongo fixture cleanup incomplete'
                            finally:os.setresuid(owner,owner,0)
                    finally:os.setresuid(0,0,0)
                    report('native-postpair')
            finally:
                command('stop','--time','20','oasis-timescaledb-cql')
                status=inspect()['State'];assert not status['Running'] and status['ExitCode']==0,'Owned teardown incomplete'
    assert not list(STATE.glob('*.pending.json'))
    print(json.dumps({'passed':True,'proofs':proofs,'stateBytes':sum(p.stat().st_size for p in STATE.rglob('*') if p.is_file()),'stopped':True}))

if __name__=='__main__':main()
