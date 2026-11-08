"""Cloud-only combined WEB4/OpenVINO startup with fresh isolated accounting data."""
import argparse, json, os, re, secrets, subprocess, tempfile, time
from pathlib import Path
from owned_docker_resources import missing_resource


def configuration(root, mongo, marker):
    dna = json.loads((root / 'OASIS Architecture/NextGenSoftware.OASIS.API.DNA/Default/OASIS_DNA.json').read_text(encoding='utf-8-sig'))
    oasis = dna['OASIS']; storage = oasis['StorageProviders']
    oasis['HyperDriveMode'] = 'OASISHyperDrive2'
    for key in list(storage):
        if key.startswith('Auto') and isinstance(storage[key], bool): storage[key] = False
        if key.startswith('Auto') and isinstance(storage[key], str): storage[key] = 'MongoDBOASIS'
    storage['OASISProviderBootType'] = 'Warm'
    storage['MongoDBOASIS'] = {'ConnectionString': mongo, 'DBName': marker + '_avatars'}
    hd = oasis['OASISHyperDriveConfig']
    for key in ['OfflineSyncEnabled','EnableHostedSync','EnableSyncHistoryCompaction','AutoFailoverEnabled','AutoReplicationEnabled','AutoLoadBalancingEnabled']: hd[key] = False
    for key in ['EnabledProviders','AutoFailoverProviders','AutoReplicationProviders','LoadBalancingProviders']: hd[key] = ['MongoDBOASIS']
    hd['ProviderConfigs'] = {}
    oasis['ONET']['EnableHyperDriveSyncHost'] = False
    oasis['ONET']['EnableMDNS'] = False
    oasis['ONET']['AutoRegisterOnBootstrap'] = False
    oasis['ONET']['BootstrapServers'] = []
    oasis['Security']['SecretKey'] = secrets.token_hex(64)
    oasis['Security']['RequireApiKey'] = False
    oasis['OfflineSessionGrants']['Enabled'] = False
    oasis['Sandbox']['Enabled'] = True
    oasis['Logging']['LogToFile'] = False
    policy = {'requiresProviderMeasurement':True,'meterCategory':'cql.package-startup','reservedUnits':1,'fixedCostUsd':0,
              'estimatedCostUsd':0,'maximumNativeFeeCostUsd':0,'maximumProviderAttempts':1,
              'pricingCatalogueVersion':'package-startup-v1'}
    admission = {'nativeProviders':[], 'ordinaryProviders':['MongoDBOASIS'],
        'queryOnlyProviders':['IntelOpenVINOOASIS'], 'storageTariffs':[],
        'queryTariffs':[{'provider':p,'costUsd':0,'catalogueVersion':'package-startup-v1'} for p in ['MongoDBOASIS','IntelOpenVINOOASIS']],
        'routes':[{'method':'POST','path':'/api/data/query-cql','policy':policy}]}
    return {'OASIS_DNA_JSON':json.dumps(dna,separators=(',',':')),
        'PREPARED_STORAGE_ADMISSION_JSON':json.dumps(admission,separators=(',',':')),
        'SUBSCRIPTION_USAGE_ENABLED':'true','SUBSCRIPTION_MONGODB_CONNECTION_STRING':mongo,
        'SUBSCRIPTION_MONGODB_DATABASE':marker+'_accounting',
        'SUBSCRIPTION_OUTBOX_MONGO_CONNECTION_STRING':mongo,'SUBSCRIPTION_OUTBOX_DATABASE':marker+'_outbox',
        'SUBSCRIPTION_LEDGER_INITIAL_STATE':'empty-new-installation',
        'OASIS_SANDBOX_OPENVINO':'true','CQL_OPENVINO_MODEL_QUERY_SOURCE_OWNER':'a5808c3e-7031-47e7-92de-48a659568f1b',
        'PORT':'8080','ASPNETCORE_URLS':'http://+:8080'}




def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--digest',required=True);o=p.parse_args()
    assert os.environ.get('GITHUB_ACTIONS')=='true' and re.fullmatch('sha256:[a-f0-9]{64}',o.digest)
    root=Path(__file__).resolve().parents[2];out=root/'TestResults/Web4CombinedStartup';out.mkdir(parents=True,exist_ok=False)
    package=json.loads((root/'TestResults/Web4FullPackage/acknowledged.json').read_text())
    image='ghcr.io/nextgensoftwareuk/oasis-web4@'+o.digest;assert package['image']==image
    marker='oasis_startup_'+os.environ['GITHUB_RUN_ID']+'_'+os.environ['GITHUB_RUN_ATTEMPT']
    assert re.fullmatch('oasis_startup_[0-9]+_[0-9]+',marker)
    network=marker+'_net';mongo=marker+'_mongo';api=marker+'_api';created=[]
    def docker(*args,check=True,timeout=60):
        r=subprocess.run(['docker',*args],capture_output=True,text=True,timeout=timeout)
        if check and r.returncode: raise RuntimeError('Owned startup operation failed: '+r.stderr[-1500:])
        return r
    def absent(kind,name):
        r=docker(kind,'inspect',name,check=False)
        if missing_resource(kind,name,r.returncode,r.stderr): return True
        if r.returncode != 0: raise RuntimeError('Cannot prove owned '+kind+' absence: '+r.stderr[-1000:])
        return False
    for kind,name in [('network',network),('container',mongo),('container',api)]: assert absent(kind,name)
    try:
        docker('network','create','--internal','--label','oasis.startup.owner='+marker,network);created.append(('network',network))
        docker('create','--name',mongo,'--label','oasis.startup.owner='+marker,'--network',network,
            '--network-alias','mongo','--memory','1g','mongo:8.0.15','--replSet','rs0','--bind_ip_all',timeout=180);created.append(('container',mongo));docker('start',mongo)
        deadline=time.monotonic()+60
        while True:
            r=docker('exec',mongo,'mongosh','--quiet','--eval','db.adminCommand({ping:1}).ok',check=False,timeout=10)
            if r.returncode==0: break
            if not re.search(r'ECONNREFUSED|connection refused',r.stdout+r.stderr,re.I):
                raise RuntimeError('Mongo ping failed outside the startup connection-refusal contract.')
            if time.monotonic()>deadline: raise RuntimeError('Owned Mongo readiness deadline.')
            time.sleep(1)
        docker('exec',mongo,'mongosh','--quiet','--eval','rs.initiate({_id:"rs0",members:[{_id:0,host:"mongo:27017"}]})')
        deadline=time.monotonic()+60
        while True:
            r=docker('exec',mongo,'mongosh','--quiet','--eval','if(!db.hello().isWritablePrimary)quit(2)',check=False,timeout=10)
            if r.returncode==0: break
            if r.returncode!=2: raise RuntimeError('Mongo primary check failed outside the not-yet-primary contract.')
            if time.monotonic()>deadline: raise RuntimeError('Owned primary readiness deadline.')
            time.sleep(1)
        with tempfile.TemporaryDirectory(prefix='oasis-startup-',dir=os.environ['RUNNER_TEMP']) as tmp:
            envfile=Path(tmp)/'environment';envfile.write_text(''.join(k+'='+v+'\n' for k,v in configuration(root,'mongodb://mongo:27017/?replicaSet=rs0',marker).items()));envfile.chmod(0o600)
            docker('create','--name',api,'--label','oasis.startup.owner='+marker,'--network',network,
                '--memory','3g','--cpus','2','--env-file',str(envfile),image);created.append(('container',api));docker('start',api)
        probe = r"""
(async()=>{
let r;try{r=await fetch('http://127.0.0.1:8080/api/health',{signal:AbortSignal.timeout(3000)});}
catch(e){if(e.cause?.code==='ECONNREFUSED')process.exit(2);throw e;}
if(r.status===503)process.exit(2);
if(!r.ok)throw Error('API health HTTP '+r.status);
const b=await r.json();if(b.status!=='healthy'||b.service!=='OASIS API ONODE WebAPI')throw Error('API health identity differs');
console.log(JSON.stringify(b));
})().catch(e=>{console.error(e);process.exit(1)});
"""
        deadline=time.monotonic()+180
        while True:
            state=json.loads(docker('inspect',api).stdout)[0]
            if not state['State']['Running']: raise RuntimeError('Combined API process exited before readiness.')
            r=docker('exec',api,'node','-e',probe,check=False,timeout=8)
            if r.returncode==0: break
            if r.returncode!=2:
                (out/'failed-health-probe.json').write_text(json.dumps({'exitCode':r.returncode,'stdout':r.stdout[-2000:],'stderr':r.stderr[-2000:]}))
                raise RuntimeError('API health failed outside connection-refusal/503 readiness contract; exit '+str(r.returncode)+': '+r.stderr[-1500:])
            if time.monotonic()>deadline: raise RuntimeError('Combined API readiness deadline.')
            time.sleep(2)
        native="const x=require('/app/host-sandbox-openvino.cjs');x.verifyNative(require('/app/openvino-model-server-pins.json'),AbortSignal.timeout(10000)).then(()=>console.log('native-metadata-verified')).catch(e=>{console.error(e);process.exit(1)})"
        docker('exec',api,'node','-e',native,timeout=20)
        docker('stop','--time','20',api,timeout=30)
        state=json.loads(docker('inspect',api).stdout)[0];assert state['State']['ExitCode']==0 and not state['State']['OOMKilled']
        (out/'acknowledged.json').write_text(json.dumps({'source':package['source'],'image':image,'apiHealth':True,'nativeMetadata':True,'gracefulExit':0,'internalNetwork':True,'liveDeployment':False,'cqlAcceptance':False},indent=2))
    finally:
        if ('container',api) in created:
            observed=json.loads(docker('inspect',api).stdout)[0]
            assert observed['Config']['Labels']['oasis.startup.owner']==marker
            (out/'process-state.json').write_text(json.dumps(observed['State'],indent=2))
            logs=docker('logs','--tail','250',api,check=False)
            log=logs.stdout+'\n'+logs.stderr
            # Never retain configuration or generated signing material in diagnostic output.
            safe = [line for line in log.splitlines() if not re.search(r'(?i)secret|password|token|key|connectionstring|[{}]',line)]
            (out/'process.log').write_text('\n'.join(safe)[-32768:])
        for kind,name in reversed(created):
            state=json.loads(docker(kind,'inspect',name).stdout)[0]
            labels=state['Labels'] if kind=='network' else state['Config']['Labels'];assert labels['oasis.startup.owner']==marker
            if kind=='container':
                if state['State']['Running']: docker('stop','--time','20',name,timeout=30)
                volumes=[mount['Name'] for mount in state.get('Mounts',[]) if mount['Type']=='volume']
                docker('rm','--volumes',name)
                for volume in volumes:
                    assert absent('volume',volume), 'Owned anonymous database volume remains.'
            else: docker('network','rm',name)
            assert absent(kind,name)
        (out/'cleanup.json').write_text(json.dumps({'ownedContainersAbsent':True,'ownedNetworkAbsent':True,'ephemeralDatabaseVolumesRemoved':True}))

if __name__=='__main__':main()
