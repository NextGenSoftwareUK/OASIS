"""Run native CouchDB acceptance twice against one retained container/source."""
from pathlib import Path
import argparse, sys, base64, fcntl, hashlib, json, os, pwd, socket, subprocess, time, urllib.error, urllib.request
import xml.etree.ElementTree as ET
parser=argparse.ArgumentParser()
parser.add_argument('--federated',action='store_true');parser.add_argument('--serve',action='store_true')
options=parser.parse_args()
if options.serve and not options.federated:raise RuntimeError('Serve requires explicit federation')
sys.dont_write_bytecode=True
from cosmic_linux_mongo_service import owned_linux_mongo
ROOT=Path('/mnt/c/Source/OASIS-cosmic')
STATE=Path('/var/tmp/oasis-couchdb-cql')
RESULTS=ROOT/'TestResults/CouchDbCqlCurrent'
DOTNET='/var/tmp/oasis-runtimes/dotnet-10.0.12/dotnet'
VSTEST='/mnt/c/Users/david/.nuget/packages/microsoft.testplatform.portable/18.11.0/tools/net8.0/vstest.console.dll'
DLL=ROOT/'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.CouchDBOASIS.IntegrationTests/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Providers.CouchDBOASIS.IntegrationTests.dll'
OWNER=pwd.getpwnam('dellams').pw_uid

def command(*args):return subprocess.check_output(['podman',*args],text=True).strip()
def inspect():
    value=json.loads(command('inspect','oasis-couchdb-cql'))[0]
    original=json.loads((STATE/'environment.json').read_text())
    assert value['Id']==original['containerId']
    assert value['Image']==original['imageId']
    assert value['HostConfig']['PortBindings']['5984/tcp']==[{'HostIp':'127.0.0.1','HostPort':'15984'}]
    assert any(m['Source']==str(STATE/'data') and m['Destination']=='/opt/couchdb/data' for m in value['Mounts'])
    return value

def demote():
    os.setgroups([]);os.setgid(OWNER);os.setuid(OWNER)

def main():
    assert os.geteuid()==0,'Use existing rootful Podman owner'
    assert STATE.is_dir() and not list(STATE.glob('*.pending.json')),'Reconcile original pending outcome; never replay'
    assert inspect()['State']['Status']=='exited','Do not adopt a running service'
    config=json.loads((STATE/'credentials.json').read_text())
    header='Basic '+base64.b64encode((config['username']+':'+config['password']).encode()).decode()
    def get(path):
        request=urllib.request.Request('http://127.0.0.1:15984'+path,headers={'Authorization':header})
        with urllib.request.urlopen(request,timeout=2) as response:return json.load(response)
    proofs=[]
    with (STATE/'runner.lock').open('a') as lock:
        fcntl.flock(lock,fcntl.LOCK_EX|fcntl.LOCK_NB)
        # Preserve completed integration evidence before starting the owned source.
        for name in ["native-first.trx", "native-restart.trx", "mongo-init.trx", "mongo-federation.trx", "native-postpair.trx"]:
            previous = RESULTS / name
            if previous.exists():
                count = ET.parse(previous).find(".//{*}Counters").attrib
                assert count["total"] == count["passed"] == "1" and count["notExecuted"] == "0", "Preserve unresolved prior report"
                archive = RESULTS / "Archive"
                archive.mkdir(exist_ok=True)
                destination = archive / (hashlib.sha256(previous.read_bytes()).hexdigest().upper() + "-" + name)
                assert not destination.exists(), "Prior report already archived; reconcile explicitly"
                previous.rename(destination)
        for phase in ['native-first','native-restart']:
            assert inspect()['State']['Status']=='exited'
            with socket.socket() as probe:assert probe.connect_ex(('127.0.0.1',15984))!=0
            command('start','oasis-couchdb-cql')
            try:
                for _ in range(100):
                    assert inspect()['State']['Running'],'Owned service exited during readiness'
                    try:
                        if get('/_up')['status']=='ok':break
                    except (ConnectionError,urllib.error.URLError):time.sleep(.25)
                else:raise RuntimeError('Native readiness deadline')
                assert get('/')['uuid']==json.loads((STATE/'environment.json').read_text())['observations'][0]['uuid']
                def report(name,filter_name='CouchDbNativeCqlTests',extra=None):
                    env={**os.environ,'DOTNET_ROOT':'/var/tmp/oasis-runtimes/dotnet-10.0.12','COSMIC_COUCHDB_ROOT':str(STATE)}
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
                                report('mongo-federation','CouchDBMongoCqlNativeTests',
                                    {**mongo_env,'COSMIC_COUCHDB_MONGO_SERVE':'1' if options.serve else '0'})
                                assert not (STATE/'mongo.pending.json').exists(),'Mongo fixture cleanup incomplete'
                            finally:os.setresuid(owner,owner,0)
                    finally:os.setresuid(0,0,0)
                    report('native-postpair')
            finally:
                command('stop','--time','20','oasis-couchdb-cql')
                status=inspect()['State'];assert not status['Running'] and status['ExitCode']==0,'Owned teardown incomplete'
    assert not list(STATE.glob('*.pending.json'))
    print(json.dumps({'passed':True,'proofs':proofs,'stateBytes':sum(p.stat().st_size for p in STATE.rglob('*') if p.is_file()),'stopped':True}))

if __name__=='__main__':main()
