"""Run native ArangoDB acceptance twice against one retained container/source."""
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
STATE=Path('/var/tmp/oasis-arangodb-cql')
RESULTS=ROOT/'TestResults/ArangoDbCqlCurrent'
DOTNET='/var/tmp/oasis-runtimes/dotnet-10.0.12/dotnet'
VSTEST='/mnt/c/Users/david/.nuget/packages/microsoft.testplatform.portable/18.11.0/tools/net8.0/vstest.console.dll'
DLL=ROOT/'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.ArangoDBOASIS.IntegrationTests/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Providers.ArangoDBOASIS.IntegrationTests.dll'
OWNER=pwd.getpwnam('dellams').pw_uid

def command(*args):return subprocess.check_output(['podman',*args],text=True).strip()
def inspect():
    value=json.loads(command('inspect','oasis-arangodb-cql'))[0]
    original=json.loads((STATE/'environment.json').read_text())
    assert value['Id']==original['containerId']
    assert value['Image']==original['imageId']
    assert value['HostConfig']['PortBindings']['8529/tcp']==[{'HostIp':'127.0.0.1','HostPort':'18529'}]
    assert any(m['Source']==str(STATE/'data') and m['Destination']=='/var/lib/arangodb3' for m in value['Mounts'])
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
        request=urllib.request.Request('http://127.0.0.1:18529'+path,headers={'Authorization':header})
        with urllib.request.urlopen(request,timeout=2) as response:return json.load(response)
    proofs=[]
    with (STATE/'runner.lock').open('a') as lock:
        fcntl.flock(lock,fcntl.LOCK_EX|fcntl.LOCK_NB)
        for phase in ['native-first','native-restart']:
            assert inspect()['State']['Status']=='exited'
            with socket.socket() as probe:assert probe.connect_ex(('127.0.0.1',18529))!=0
            command('start','oasis-arangodb-cql')
            try:
                for _ in range(100):
                    assert inspect()['State']['Running'],'Owned service exited during readiness'
                    try:
                        if get('/_api/version')['version']=='3.12.12':break
                    except (ConnectionError,urllib.error.URLError):time.sleep(.25)
                else:raise RuntimeError('Native readiness deadline')
                assert json.loads((STATE/'data/SERVER').read_text())['serverId']==json.loads((STATE/'environment.json').read_text())['observations'][0]['serverId']
                def report(name,filter_name='ArangoDbNativeCqlTests',extra=None):
                    env={**os.environ,'DOTNET_ROOT':'/var/tmp/oasis-runtimes/dotnet-10.0.12','COSMIC_ARANGODB_ROOT':str(STATE)}
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
                                report('mongo-federation','ArangoDBMongoCqlNativeTests',
                                    {**mongo_env,'COSMIC_ARANGODB_MONGO_SERVE':'1' if options.serve else '0'})
                                assert not (STATE/'mongo.pending.json').exists(),'Mongo fixture cleanup incomplete'
                            finally:os.setresuid(owner,owner,0)
                    finally:os.setresuid(0,0,0)
                    report('native-postpair')
            finally:
                command('stop','--time','20','oasis-arangodb-cql')
                status=inspect()['State'];assert not status['Running'] and status['ExitCode']==0,'Owned teardown incomplete'
    assert not list(STATE.glob('*.pending.json'))
    print(json.dumps({'passed':True,'proofs':proofs,'stateBytes':sum(p.stat().st_size for p in STATE.rglob('*') if p.is_file()),'stopped':True}))

if __name__=='__main__':main()
