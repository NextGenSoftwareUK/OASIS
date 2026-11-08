"""One cloud-only owned OVMS runtime qualification; no local image/model installation."""
import argparse,datetime,hashlib,json,os,re,shutil,subprocess,time,urllib.request,urllib.error
from pathlib import Path
parser=argparse.ArgumentParser(description=__doc__);verification=parser.add_mutually_exclusive_group();verification.add_argument('--verify-client',action='store_true');verification.add_argument('--verify-query',action='store_true');verification.add_argument('--verify-web4',action='store_true');verification.add_argument('--verify-package',action='store_true');options=parser.parse_args()
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
if options.verify_package:
    intent['qualification']='web4-base-package'
(result/'intent.json').write_text(json.dumps(intent,indent=2))

def docker(*args,check=True,timeout=180):
    p=subprocess.run(['docker',*args],capture_output=True,text=True,timeout=timeout)
    if check and p.returncode:raise RuntimeError('Owned OVMS docker operation failed: '+p.stderr[-2000:])
    return p

def capture_startup_diagnostics():
    """Observe the original process only; never restart, attach, or change mounts."""
    probes={
        'threads':('top',name,'-eo','pid,ppid,stat,wchan:32,comm'),
        'runtime-view':('exec','--user','0',name,'/bin/sh','-c',
            'readlink /proc/1/root; '
            'for p in /proc/1/root/proc /proc/1/root/proc/cpuinfo /proc/1/root/sys '
            '/proc/1/root/dev /proc/1/root/dev/null /proc/1/root/models/config.json; '
            'do ls -ld "$p" 2>&1; done; '
            'n=0; for t in /proc/1/task/*; do '
            'n=$((n+1)); [ "$n" -le 64 ] || break; printf "THREAD %s\\n" "$t"; '
            'for f in comm wchan syscall; do printf "%s: " "$f"; head -c 512 "$t/$f" 2>&1; printf "\\n"; done; done')
    }
    for key,args in probes.items():
        try:
            observed=docker(*args,check=False,timeout=10)
            record={'exitCode':observed.returncode,'stdout':observed.stdout[:32768],
                    'stderr':observed.stderr[:4096],'observationOnly':True}
        except subprocess.TimeoutExpired:
            record={'timeoutSeconds':10,'observationOnly':True}
        (result/('diagnostic-'+key+'.json')).write_text(json.dumps(record,indent=2))

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
    runtime_image=PINS['image']
    model_mount='/models'
    if options.verify_package:
        # Build the actual unchanged WEB4 base stage, then copy the entire official
        # userspace. Its Python runtime lives outside /ovms; preserve its ABI as
        # a complete root filesystem, isolated from the WEB4 loader environment.
        base_image='oasis-web4-base:'+label
        runtime_image='oasis-ovms-web4-package:'+label
        web4_dockerfile=ROOT/'Docker/Dockerfile.web4'
        docker('build','--target','base','--file',str(web4_dockerfile),'--tag',base_image,str(models),timeout=240)
        package_dockerfile=models/'Dockerfile.package'
        package_dockerfile.write_text(
            'FROM '+PINS['image']+' AS official\n'
            'FROM '+base_image+'\n'
            'COPY --from=official / /opt/ovms-rootfs/\n'
            'ENTRYPOINT ["/usr/sbin/chroot","/opt/ovms-rootfs","/usr/bin/env","LD_LIBRARY_PATH=/ovms/lib","PYTHONPATH=/ovms/lib/python:/ovms/python_deps","ESPEAK_DATA_PATH=/ovms/share/espeak-ng-data","/ovms/bin/ovms"]\n',encoding='utf-8')
        model_mount='/opt/ovms-rootfs/models'
        docker('build','--file',str(package_dockerfile),'--tag',runtime_image,str(models),timeout=240)
        package_info=json.loads(docker('image','inspect',runtime_image,timeout=20).stdout)[0]
        base_info=json.loads(docker('image','inspect',base_image,timeout=20).stdout)[0]
        (result/'package.json').write_text(json.dumps({
            'web4DockerfileSha256':hashlib.sha256(web4_dockerfile.read_bytes()).hexdigest(),
            'packageDockerfileSha256':hashlib.sha256(package_dockerfile.read_bytes()).hexdigest(),
            'baseImageId':base_info['Id'],'packageImageId':package_info['Id'],
            'officialImage':PINS['image'],'completeBundle':'official-rootfs','runtimeRoot':'/opt/ovms-rootfs',
            'additionalSystemPackages':[],'liveDeployment':False},indent=2))
    assert docker('container','inspect',name,check=False,timeout=20).returncode!=0
    docker('run','--detach','--name',name,'--label','oasis.cql.owner='+label,'--cpus','2','--memory','1g','--publish','127.0.0.1:28000:8000','--mount','type=bind,source='+str(models)+',target='+model_mount+',readonly',runtime_image,'--config_path','/models/config.json','--rest_port','8000','--port','9000',timeout=30)
    deadline=time.monotonic()+120
    # Explicit bounded startup readiness polling; model metadata/inference never retried.
    while True:
        status,raw=call('/v2/health/ready')
        if status==200:break
        inspected=json.loads(docker('container','inspect',name,timeout=20).stdout)[0]
        assert inspected['State']['Running'] and not inspected['State']['OOMKilled']
        if time.monotonic()>=deadline:
            capture_startup_diagnostics()
            raise RuntimeError('Owned OVMS startup deadline expired')
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
    proof['web4BasePackagePass']=options.verify_package
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
