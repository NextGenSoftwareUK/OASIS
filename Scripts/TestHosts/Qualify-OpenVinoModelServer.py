"""One cloud-only owned OVMS runtime qualification; no local image/model installation."""
import argparse,datetime,hashlib,json,os,re,shutil,subprocess,time,urllib.request,urllib.error
from pathlib import Path
parser=argparse.ArgumentParser(description=__doc__);verification=parser.add_mutually_exclusive_group();verification.add_argument('--verify-client',action='store_true');verification.add_argument('--verify-query',action='store_true');verification.add_argument('--verify-web4',action='store_true');options=parser.parse_args()
if os.environ.get('GITHUB_ACTIONS')!='true':
    raise RuntimeError('This qualifier runs only in the owned cloud job, never on the recovery laptop')
ROOT=Path(__file__).resolve().parents[2]
PINS=json.loads((ROOT/'Scripts/TestHosts/openvino-model-server-pins.json').read_bytes())
run=os.environ['GITHUB_RUN_ID'];attempt=os.environ['GITHUB_RUN_ATTEMPT']
assert re.fullmatch('[0-9]+',run) and re.fullmatch('[0-9]+',attempt)
label=run+'-'+attempt;name='oasis-ovms-cql-'+label
result=ROOT/'TestResults/OpenVinoRuntimeCurrent';result.mkdir(parents=True,exist_ok=False)
tmp=Path(os.environ['RUNNER_TEMP']).resolve();models=(tmp/name).resolve()
assert models.parent==tmp and not models.exists()
models.mkdir();(models/'owner.json').write_text(json.dumps({'run':run,'attempt':attempt}))
intent={'run':run,'attempt':attempt,'container':name,'image':PINS['image'],'model':PINS['modelName'],'versions':PINS['versions'],'readinessOnly':not(options.verify_client or options.verify_query or options.verify_web4),'qualification':'web4' if options.verify_web4 else 'query' if options.verify_query else 'client' if options.verify_client else 'runtime'}
(result/'intent.json').write_text(json.dumps(intent,indent=2))

def docker(*args,check=True,timeout=180):
    p=subprocess.run(['docker',*args],capture_output=True,text=True,timeout=timeout)
    if check and p.returncode:raise RuntimeError('Owned OVMS docker operation failed: '+p.stderr[-2000:])
    return p

class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self,*args):return None

sequence=0
def call(path,body=None):
    global sequence
    sequence+=1
    uri='http://127.0.0.1:28000'+path
    (result/(str(sequence)+'-intent.json')).write_text(json.dumps({'method':'GET' if body is None else 'POST','path':path,'body':body}))
    request=urllib.request.Request(uri,data=None if body is None else json.dumps(body).encode(),headers={'Accept':'application/json','Content-Type':'application/json'},method='GET' if body is None else 'POST')
    stage='connect';observed_status=None
    try:
        try:response=urllib.request.build_opener(NoRedirect).open(request,timeout=10)
        except urllib.error.HTTPError as error:response=error
        with response:
            observed_status=response.status;stage='body'
            raw=response.read(65537);status=response.status
    except (urllib.error.URLError,ConnectionError,TimeoutError) as error:
        reason=error.reason if isinstance(error,urllib.error.URLError) else error
        (result/(str(sequence)+'-transport.json')).write_text(json.dumps({'stage':stage,'observedHttpStatus':observed_status,'errorType':type(reason).__name__,'message':str(reason)}))
        return None,None
    (result/(str(sequence)+'-raw.json')).write_bytes(raw)
    (result/(str(sequence)+'-status.json')).write_text(json.dumps({'status':status,'bytes':len(raw),'sha256':hashlib.sha256(raw).hexdigest()}))
    assert len(raw)<=65536
    return status,raw

try:
    files={}
    for file,pin in PINS['files'].items():
        url='https://raw.githubusercontent.com/openvinotoolkit/model_server/'+PINS['sourceCommit']+'/src/test/dummy/1/'+file
        with urllib.request.build_opener(NoRedirect).open(url,timeout=25) as response:
            assert response.status==200
            raw=response.read(pin['bytes']+1)
        assert len(raw)==pin['bytes'] and hashlib.sha256(raw).hexdigest()==pin['sha256']
        files[file]=raw
    for version in PINS['versions']:
        folder=models/PINS['modelName']/version;folder.mkdir(parents=True)
        for file,raw in files.items():(folder/file).write_bytes(raw)
    config={'model_config_list':[{'config':{'name':PINS['modelName'],'base_path':'/models/'+PINS['modelName'],'model_version_policy':{'all':{}},'target_device':'CPU'}}]}
    (models/'config.json').write_text(json.dumps(config))
    docker('pull',PINS['image'])
    assert docker('container','inspect',name,check=False,timeout=20).returncode!=0
    docker('run','--detach','--name',name,'--label','oasis.cql.owner='+label,'--cpus','2','--memory','1g','--publish','127.0.0.1:28000:8000','--mount','type=bind,source='+str(models)+',target=/models,readonly',PINS['image'],'--config_path','/models/config.json','--rest_port','8000','--port','9000',timeout=30)
    deadline=time.monotonic()+120
    # Explicit bounded startup readiness polling; model metadata/inference never retried.
    while True:
        status,raw=call('/v2/health/ready')
        if status==200:break
        inspected=json.loads(docker('container','inspect',name,timeout=20).stdout)[0]
        assert inspected['State']['Running'] and not inspected['State']['OOMKilled']
        if time.monotonic()>=deadline:raise RuntimeError('Owned OVMS startup deadline expired')
        time.sleep(1)
    status,raw=call('/v2');assert status==200
    server=json.loads(raw);assert server['name']=='OpenVINO Model Server' and server['version']==PINS['serverVersion']
    metadata=[]
    for version in PINS['versions']:
        path='/v2/models/'+PINS['modelName']+'/versions/'+version
        status,raw=call(path+'/ready');assert status==200
        status,raw=call(path);assert status==200
        record=json.loads(raw);assert record['name']==PINS['modelName'] and version in record['versions']
        assert record['inputs']==[PINS['input']] and record['outputs']==[PINS['output']]
        metadata.append(record)
        request={'id':'oasis-'+label+'-'+version,'inputs':[PINS['input']|{'data':list(range(10))}]}
        status,raw=call(path+'/infer',request);assert status==200
        inference=json.loads(raw)
        assert inference['model_name']==PINS['modelName'] and inference['model_version']==version
        assert inference['id']==request['id'] and inference['outputs']==[PINS['output']|{'data':list(range(1,11))}]
    if options.verify_client:
        env=dict(os.environ,COSMIC_OPENVINO_CLIENT_NATIVE='owned-loopback-28000',COSMIC_OPENVINO_PIN_FILE=str(ROOT/'Scripts/TestHosts/openvino-model-server-pins.json'))
        subprocess.run(['dotnet','test','Providers/Other/TestProjects/NextGenSoftware.OASIS.API.Providers.IntelOpenVINOOASIS.UnitTests','--no-build','--no-restore','/m:2','/nr:false','/p:UseSharedCompilation=false','--filter','TestCategory=OpenVinoClientNative','--results-directory','TestResults/OpenVinoClientCurrent','--logger','trx;LogFileName=native.trx'],cwd=ROOT,env=env,check=True,timeout=120)
    if options.verify_query:
        env=dict(os.environ,COSMIC_OPENVINO_MODEL_NATIVE='owned-loopback-28000')
        subprocess.run(['dotnet','test','Providers/Other/TestProjects/NextGenSoftware.OASIS.API.Providers.IntelOpenVINOOASIS.UnitTests','--no-build','--no-restore','/m:2','/nr:false','/p:UseSharedCompilation=false','--filter','TestCategory=OpenVinoModelNative','--results-directory','TestResults/OpenVinoQueryCurrent','--logger','trx;LogFileName=native.trx'],cwd=ROOT,env=env,check=True,timeout=120)
    if options.verify_web4:
        env=dict(os.environ,COSMIC_OPENVINO_MODEL_NATIVE_HTTP='owned-loopback',COSMIC_REVIEWED_PUBLIC_MONGO_NATIVE='owned-local-27029')
        subprocess.run(['dotnet','test','ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests','--no-build','--no-restore','/m:2','/nr:false','/p:UseSharedCompilation=false','--filter','FullyQualifiedName~OpenVinoModelNativeHttp','--results-directory','TestResults/OpenVinoWeb4Current','--logger','trx;LogFileName=web4.trx'],cwd=ROOT,env=env,check=True,timeout=180)
    proof=intent|{'server':server,'metadata':metadata,'inferences':3,'probeRequests':sequence,'nativeApiPass':True,'dotnetClientPass':options.verify_client,'cqlPass':options.verify_query,'web4Pass':options.verify_web4,'browserPass':False,'hostedPass':False}
    (result/'acknowledged.json').write_text(json.dumps(proof,indent=2))
finally:
    observation=docker('container','inspect',name,check=False,timeout=20)
    if observation.returncode==0:
        inspected=json.loads(observation.stdout)[0]
        assert inspected['Config']['Labels']['oasis.cql.owner']==label
        logs=docker('logs',name,check=False,timeout=20)
        (result/'server.log').write_text(logs.stdout+logs.stderr)
        docker('stop','--time','15',name,timeout=30)
        stopped=json.loads(docker('container','inspect',name,timeout=20).stdout)[0]
        assert not stopped['State']['Running'] and not stopped['State']['OOMKilled']
        docker('rm','--volumes',name,timeout=20)
        absence=docker('container','inspect',name,check=False,timeout=20)
        assert absence.returncode!=0 and 'no such' in absence.stderr.lower()
    else:
        assert 'no such' in observation.stderr.lower(), 'Container outcome is uncertain; preserve model directory'
    assert models.resolve().parent==tmp and not models.is_symlink()
    assert json.loads((models/'owner.json').read_bytes())=={'run':run,'attempt':attempt}
    shutil.rmtree(models)
    (result/'cleanup.json').write_text(json.dumps({'ownedProcessesStopped':True,'ownedContainerAbsent':True,'ownedModelDirectoryAbsent':not models.exists(),'cloudImageOnly':True}))
print('Owned OpenVINO runtime and three real versioned model/inference checks pass; compact evidence retained.')
