"""Cloud-only combined WEB4/OpenVINO startup with fresh isolated accounting data."""
import argparse, ipaddress, json, os, re, secrets, subprocess, tempfile, time
from pathlib import Path
from contextlib import ExitStack
from owned_web5_startup import OwnedApiBridge, OwnedWeb5Process, run_owned_browser
from owned_docker_resources import missing_resource


def configuration(root, mongo, marker, profile='full'):
    if profile not in ('full', 'tron-api', 'maplibre', 'cql-api'):
        raise ValueError('Explicit known startup profile required.')
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
    oasis['Email']['DisableAllEmails'] = True
    policy = {'requiresProviderMeasurement':True,'meterCategory':'cql.package-startup','reservedUnits':1,'fixedCostUsd':0,
              'estimatedCostUsd':0,'maximumNativeFeeCostUsd':0,'maximumProviderAttempts':1,
              'pricingCatalogueVersion':'package-startup-v1'}
    query_only = ['IntelOpenVINOOASIS'] if profile == 'full' else []
    admission = {'nativeProviders':[], 'ordinaryProviders':['MongoDBOASIS'],
        'queryOnlyProviders':query_only, 'storageTariffs':[],
        'queryTariffs':[{'provider':p,'costUsd':0,'catalogueVersion':'package-startup-v1'} for p in ['MongoDBOASIS'] + query_only],
        'routes':[{'method':'POST','path':'/api/data/query-cql','policy':policy}]}
    env = {'OASIS_DNA_JSON':json.dumps(dna,separators=(',',':')),
        'PREPARED_STORAGE_ADMISSION_JSON':json.dumps(admission,separators=(',',':')),
        'SUBSCRIPTION_USAGE_ENABLED':'true','SUBSCRIPTION_MONGODB_CONNECTION_STRING':mongo,
        'SUBSCRIPTION_MONGODB_DATABASE':marker+'_accounting',
        'SUBSCRIPTION_OUTBOX_MONGO_CONNECTION_STRING':mongo,'SUBSCRIPTION_OUTBOX_DATABASE':marker+'_outbox',
        'SUBSCRIPTION_LEDGER_INITIAL_STATE':'empty-new-installation',
        'PORT':'8080','ASPNETCORE_URLS':'http://+:8080'}
    if profile == 'full':
        env.update(OASIS_SANDBOX_OPENVINO='true', CQL_OPENVINO_MODEL_QUERY_SOURCE_OWNER='a5808c3e-7031-47e7-92de-48a659568f1b')
    else:
        env['OASIS_SANDBOX_OPENVINO'] = 'false'
    return env




def image_health_ready(state):
    test = state.get('Config', {}).get('Healthcheck', {}).get('Test', [])
    if not test or test[0] not in ('CMD', 'CMD-SHELL'):
        raise RuntimeError('Full image must declare an executable Docker healthcheck.')
    observed = state['State']
    if not observed['Running']:
        raise RuntimeError('Full image exited before Docker health readiness.')
    health = observed.get('Health', {})
    status = health.get('Status')
    if status == 'starting':
        return False
    if status != 'healthy' or not health.get('Log') or health['Log'][-1]['ExitCode'] != 0:
        raise RuntimeError('Full image Docker healthcheck failed or has no successful execution.')
    return True


def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--digest',required=True);p.add_argument('--profile',choices=['full','tron-api','maplibre','cql-api'],default='full');o=p.parse_args()
    assert os.environ.get('GITHUB_ACTIONS')=='true' and re.fullmatch('sha256:[a-f0-9]{64}',o.digest)
    root=Path(__file__).resolve().parents[2];out=root/'TestResults/Web4CombinedStartup';out.mkdir(parents=True,exist_ok=False)
    package=json.loads((root/'TestResults/Web4FullPackage/acknowledged.json').read_text())
    assert package['profile'] == o.profile, 'Package/startup profiles differ.'
    assert package['checks']['modelHashes'] is (o.profile == 'full'), 'Package/startup profiles differ.'
    if o.profile == 'tron-api':
        assert package['checks']['assemblies'] == 3 and package['checks']['tronReaderPackaged'] is True
    image='ghcr.io/nextgensoftwareuk/oasis-web4@'+o.digest;assert package['image']==image
    marker='oasis_startup_'+os.environ['GITHUB_RUN_ID']+'_'+os.environ['GITHUB_RUN_ATTEMPT']
    assert re.fullmatch('oasis_startup_[0-9]+_[0-9]+',marker)
    network=marker+'_net';mongo=marker+'_mongo';api=marker+'_api';created=[]
    processes=ExitStack();web5_process=None
    web5_app=os.environ.get('OASIS_STARTUP_WEB5_APP', '')
    popup_group=os.environ.get('OASIS_STARTUP_POPUP_GROUP', '') or 'none'
    if popup_group not in ('none', 'profile', 'web5-depth', 'web5-catalogue', 'developer', 'tools-followup', 'tools', 'web5-breadth', 'community', 'storage', 'hyperdrive', 'eggs', 'nft', 'data', 'data-crud', 'data-edit', 'files-write', 'map-route', 'messaging', 'messaging-read', 'chat', 'chat-directory', 'directory-message', 'clan', 'portal-breadth', 'chat-hyperdrive', 'data-messaging', 'nft-messaging-data'):
        raise ValueError('Unknown owned popup group.')
    popup_breadth=os.environ.get('OASIS_STARTUP_POPUP_BREADTH') == 'true'
    if popup_breadth and popup_group != 'none':
        raise ValueError('Select one new popup journey, not both.')
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
            mongo_uri='mongodb://mongo:27017/?replicaSet=rs0'
            if web5_app:
                selected_mongo=json.loads(docker('inspect', mongo).stdout)[0]
                assert selected_mongo['Config']['Labels']['oasis.startup.owner']==marker
                selected_ip=selected_mongo['NetworkSettings']['Networks'][network]['IPAddress']
                assert ipaddress.ip_address(selected_ip).version==4 and ipaddress.ip_address(selected_ip).is_private
                mongo_uri='mongodb://'+selected_ip+':27017/?replicaSet=rs0&directConnection=true'
            fixture_env=configuration(root,mongo_uri,marker,o.profile)
            if popup_breadth or popup_group != 'none':
                # This explicit new-image scenario needs collection authority under passive boot.
                admission=json.loads(fixture_env['PREPARED_STORAGE_ADMISSION_JSON'])
                admission['authoritativeProvider']='MongoDBOASIS'
                if popup_group in ('data-crud', 'data-edit', 'files-write', 'map-route', 'web5-depth'):
                    # Same free ordinary-operation tariff kinds as the admitted dev write profile.
                    catalogue=admission['routes'][0]['policy']['pricingCatalogueVersion']
                    admission['storageTariffs']=[{'provider':'MongoDBOASIS','operation':operation,'costUsd':0,'catalogueVersion':catalogue}
                        for operation in ('LoadHolon','SaveHolon','SaveHolons','LoadHolonByProviderKey','DeleteHolon','ReadConditionalHolonWriteOutcome','ReadHolonWritePreparation')]
                    write_policy=dict(admission['routes'][0]['policy'],meterCategory='api.request',maximumProviderAttempts=512)
                    admission['routes'] += [{'method':method,'path':path,'policy':dict(write_policy)}
                        for method,path in ((('POST','/api/map/routes'),) if popup_group == 'map-route' else
                            ((('POST','/api/files/upload-file'),) if popup_group == 'files-write' else
                             (('POST','/api/data/save-holon'),('DELETE','/api/data/delete-holon'))))]
                if popup_group == 'web5-depth':
                    admission['routes'].append({'method':'POST','path':'/api/oapps','policy':dict(write_policy)})
                if popup_group == 'profile':
                    catalogue=admission['routes'][0]['policy']['pricingCatalogueVersion']
                    admission['storageTariffs']=[{'provider':'MongoDBOASIS','operation':operation,'costUsd':0,'catalogueVersion':catalogue}
                        for operation in ('LoadHolon','SaveHolon','SaveHolons','LoadHolonByProviderKey','DeleteHolon','ReadConditionalHolonWriteOutcome','ReadHolonWritePreparation','LoadAvatar','ApplyAvatarProfile')]
                    admission['routes'].append({'method':'PATCH','path':'/api/avatar/profile','policy':dict(admission['routes'][0]['policy'],meterCategory='api.request',maximumProviderAttempts=512)})
                fixture_env['PREPARED_STORAGE_ADMISSION_JSON']=json.dumps(admission,separators=(',', ':'))
            if web5_app:
                fixture_env['SUBSCRIPTION_SERVICE_KEY_WEB5']=secrets.token_hex(32)
            envfile=Path(tmp)/'environment';envfile.write_text(''.join(k+'='+v+'\n' for k,v in fixture_env.items()));envfile.chmod(0o600)
            portal_root = os.environ.get('OASIS_STARTUP_PORTAL_ROOT', '')
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
        if o.profile in ('full', 'maplibre'):
            while not image_health_ready(json.loads(docker('inspect',api).stdout)[0]):
                if time.monotonic() > deadline:
                    raise RuntimeError('Full image Docker health readiness deadline.')
                time.sleep(2)
        auth_checked = os.environ.get('OASIS_STARTUP_AUTHENTICATION') == 'true'
        portal_checked = False
        if portal_root:
            assert auth_checked, 'Portal Startup browser requires explicit auth qualification.'
            api_state = json.loads(docker('inspect', api).stdout)[0]
            assert api_state['Config']['Labels']['oasis.startup.owner'] == marker
            private_ip = api_state['NetworkSettings']['Networks'][network]['IPAddress']
            parsed_ip = ipaddress.ip_address(private_ip)
            assert parsed_ip.version == 4 and parsed_ip.is_private, 'Owned internal API IPv4 required.'
            bridge=processes.enter_context(OwnedApiBridge(private_ip))
            browser_env=dict(os.environ, OASIS_STARTUP_API_URL=bridge.url)
            if web5_app:
                mongo_state=json.loads(docker('inspect', mongo).stdout)[0]
                assert mongo_state['Config']['Labels']['oasis.startup.owner']==marker
                mongo_ip=mongo_state['NetworkSettings']['Networks'][network]['IPAddress']
                assert ipaddress.ip_address(mongo_ip).version==4 and ipaddress.ip_address(mongo_ip).is_private
                direct_mongo='mongodb://'+mongo_ip+':27017/?replicaSet=rs0&directConnection=true'
                web5_dna=json.loads(fixture_env['OASIS_DNA_JSON'])
                web5_dna['OASIS']['StorageProviders']['MongoDBOASIS']['ConnectionString']=direct_mongo
                web5_environment=dict(fixture_env, WEB4_API_BASE_URL=bridge.url,
                    SUBSCRIPTION_MONGODB_CONNECTION_STRING=direct_mongo,
                    SUBSCRIPTION_OUTBOX_MONGO_CONNECTION_STRING=direct_mongo,
                    SUBSCRIPTION_OUTBOX_DATABASE=marker+'_web5_outbox')
                web5_admission=json.loads(web5_environment['PREPARED_STORAGE_ADMISSION_JSON'])
                web5_admission['authoritativeProvider']='MongoDBOASIS'
                if popup_group == 'web5-depth':
                    web5_admission['storageTariffs'].append({'provider':'MongoDBOASIS','operation':'LoadAvatar','costUsd':0,'catalogueVersion':web5_admission['routes'][0]['policy']['pricingCatalogueVersion']})
                web5_environment['PREPARED_STORAGE_ADMISSION_JSON']=json.dumps(web5_admission,separators=(',', ':'))
                web5_environment.pop('OASIS_DNA_JSON', None)
                web5_process=processes.enter_context(OwnedWeb5Process(web5_app, web5_dna, web5_environment))
                browser_env['OASIS_STARTUP_WEB5_URL']=web5_process.url
            browser = run_owned_browser(['node', str(root / 'Scripts/TestHosts/Fixtures/portal-startup-browser.cjs')], browser_env)
            popup_proof=re.search(r'PORTAL_STARTUP_POPUP_OUTCOMES:(\[[^\n]*\])', browser.stdout)
            if popup_proof:
                outcomes=json.loads(popup_proof.group(1))
                assert isinstance(outcomes,list) and 1 <= len(outcomes) <= 3
                for item in outcomes:
                    assert set(item) <= {'name','passed','stage','closePassed'}
                    assert item['name'] in ('Profile','OappMap','Launcher','Competition','Designer','Developer','HolonBrowser','Celestial','GlobalSearch','Activity','Social','Chain','Storage','Eggs','NFT','Data','DataWrite','FilesWrite','MapRoute','Messaging','MessagingRead','Chat','DirectoryMessage','Clan','HyperDrive') and isinstance(item['passed'],bool)
                    assert isinstance(item['closePassed'],bool)
                    assert re.fullmatch(r'[A-Za-z0-9 /-]{1,80}',item['stage'])
                portal_source=subprocess.check_output(['git','-C',portal_root,'rev-parse','HEAD'],text=True).strip()
                assert re.fullmatch(r'[a-f0-9]{40}',portal_source)
                (out/'portal-popup-outcomes.json').write_text(json.dumps({'source':package['source'],'image':image,'web5Source':subprocess.check_output(['git','rev-parse','HEAD'],cwd=root,text=True).strip() if web5_process else None,'portal':portal_source,'group':popup_group,'outcomes':outcomes},indent=2))
                print('PORTAL_STARTUP_POPUP_OUTCOMES:'+json.dumps(outcomes,separators=(',',':')),flush=True)
            if browser.returncode:
                facts = re.search(r'PORTAL_STARTUP_HTTP_FACTS:(\[[^\n]*\])', browser.stderr)
                if facts:
                    selected = json.loads(facts.group(1))
                    assert isinstance(selected, list) and len(selected) <= 64
                    assert all(set(item) <= {'path','status','isError','errorCode','json','routingMessage','resultShape','resultCount','routeShape','routePage','storageShape'} for item in selected)
                    for item in selected:
                        if 'storageShape' in item:
                            shape=item['storageShape']
                            assert set(shape)=={'applicationId','avatarPolicy','applicationPolicy','effectivePolicy','memoryCacheSupported'}
                            assert all(set(value)=={'present','type'} and isinstance(value['present'],bool) and value['type'] in ('undefined','null','object','string','boolean') for value in shape.values())
                        if 'routePage' in item:
                            assert set(item['routePage']) <= {'items','nextOffsetPresent','nextOffsetNull','nextOffsetInteger'}
                            assert all(isinstance(value,bool) for value in item['routePage'].values())
                        if 'routeShape' in item:
                            shape=item['routeShape']
                            assert set(shape) <= {'header','geometry','authority','revisions','headerIdMatches','headerPayload','geometryPayload','summaryPascal','summaryCamel','latitudePascal','latitudeCamel'}
                            for key,value in shape.items():
                                if key in ('headerPayload','geometryPayload'):
                                    assert value in ('undefined','string','object')
                                elif key == 'revisions':
                                    assert isinstance(value,int) and 0 <= value <= 2
                                else:
                                    assert isinstance(value,bool)
                    print('PORTAL_STARTUP_HTTP_FACTS:' + json.dumps(selected, separators=(',', ':')), flush=True)
                admission_failure=re.search(r'PORTAL_STARTUP_ADMISSION_FAILURE:(\{[^\n]*\})',browser.stderr)
                if admission_failure:
                    failure=json.loads(admission_failure.group(1))
                    assert set(failure)=={'stage','path','errorType','sourceLine'}
                    assert re.fullmatch(r'[A-Za-z0-9 /-]{1,80}',failure['stage'])
                    assert isinstance(failure['path'],str) and len(failure['path'])<=200 and failure['path'].startswith('/api/') and '?' not in failure['path']
                    assert failure['errorType'] in ('AssertionError','TypeError','Error') and isinstance(failure['sourceLine'],int)
                    print('PORTAL_STARTUP_ADMISSION_FAILURE:'+json.dumps(failure,separators=(',',':')),flush=True)
                storage_failure=re.search(r'PORTAL_STARTUP_STORAGE_FAILURE:(\{[^\n]*\})',browser.stderr)
                if storage_failure:
                    failure=json.loads(storage_failure.group(1))
                    assert set(failure)=={'stage','feedback'} and all(isinstance(value,str) and len(value)<=160 for value in failure.values())
                    print('PORTAL_STARTUP_STORAGE_FAILURE:'+json.dumps(failure,separators=(',',':')),flush=True)
                developer_failure=re.search(r'PORTAL_STARTUP_DEVELOPER_FAILURE:(\{[^\n]*\})',browser.stderr)
                if developer_failure:
                    failure=json.loads(developer_failure.group(1))
                    assert set(failure)=={'selected','modalVisible','loading','resourceCount','sourceLine'}
                    assert all(isinstance(failure[key],bool) for key in ('selected','modalVisible','loading'))
                    assert all(isinstance(failure[key],int) and failure[key]>=0 for key in ('resourceCount','sourceLine'))
                    print('PORTAL_STARTUP_DEVELOPER_FAILURE:'+json.dumps(failure,separators=(',',':')),flush=True)
                profile_failure=re.search(r'PORTAL_STARTUP_PROFILE_FAILURE:(\{[^\n]*\})',browser.stderr)
                if profile_failure:
                    failure=json.loads(profile_failure.group(1))
                    assert set(failure)<= {'status','inputDisabled','shape','errorType','sourceLine'}
                    assert isinstance(failure['status'],str) and len(failure['status'])<=320
                    assert isinstance(failure['inputDisabled'],bool)
                    assert failure['errorType'] in ('Error','TypeError','AssertionError','TimeoutError')
                    assert isinstance(failure['sourceLine'],int) and failure['sourceLine']>=0
                    if 'shape' in failure:
                        shape=failure['shape']
                        assert set(shape)=={'documentKeys','profileKeys','avatarRevisionZero','detailRevisionZero'}
                        assert set(shape['documentKeys'])<= {'profile','providerType'}
                        assert set(shape['profileKeys'])<= {'avatarId','title','firstName','lastName','username','email','address','avatarRevision','detailRevision'}
                        assert isinstance(shape['avatarRevisionZero'],bool) and isinstance(shape['detailRevisionZero'],bool)
                    print('PORTAL_STARTUP_PROFILE_FAILURE:'+json.dumps(failure,separators=(',',':')),flush=True)
                clan_failure=re.search(r'PORTAL_STARTUP_CLAN_FAILURE:(\{[^\n]*\})',browser.stderr)
                if clan_failure:
                    failure=json.loads(clan_failure.group(1))
                    assert set(failure)<= {'errorType','sourceLine','status','nameLength','buttonDisabled','activeTab','unavailable','operation','formOwner','formValid','invalidFields','bound','hit'}
                    assert isinstance(failure.get('status',''),str) and len(failure.get('status',''))<=320
                    print('PORTAL_STARTUP_CLAN_FAILURE:'+json.dumps(failure,separators=(',',':')),flush=True)
                safe_stage = re.search(r'PORTAL_STARTUP_BROWSER_FAILURE stage=([^;\n]+)', browser.stderr)
                raise RuntimeError('Owned portal Startup browser failed at ' + (safe_stage.group(1) if safe_stage else 'unreported stage'))
            assert 'PORTAL_STARTUP_BROWSER_PROOF:' in browser.stdout
            scenario = popup_group if popup_group != 'none' else ('WEB5 Games/OAPPs' if web5_process else ('Files/Wallet/Karma' if popup_breadth else 'dashboard/subscriptions/refresh'))
            print('PORTAL_STARTUP_BROWSER_PROOF: actual page sign-in/' + scenario + ' passed through isolated shipping Startup.', flush=True)
            portal_checked = True
        elif auth_checked:
            auth_script = root / 'Scripts/TestHosts/Fixtures/startup-authentication.cjs'
            docker('exec', api, 'node', '-e', auth_script.read_text(encoding='utf-8'), timeout=120)
            print('STARTUP_AUTH_PROOF: shipping registration/verification/password/session/subscription journey passed; owned Mongo only.', flush=True)
        processes.close()
        if web5_process and web5_process.graceful_exit!=0:
            raise RuntimeError('Owned WEB5 did not exit gracefully.')
        native="const x=require('/app/host-sandbox-openvino.cjs');x.verifyNative(require('/app/openvino-model-server-pins.json'),AbortSignal.timeout(10000)).then(()=>console.log('native-metadata-verified')).catch(e=>{console.error(e);process.exit(1)})"
        if o.profile == 'full':
            docker('exec',api,'node','-e',native,timeout=20)
        docker('stop','--time','20',api,timeout=30)
        state=json.loads(docker('inspect',api).stdout)[0];assert state['State']['ExitCode']==0 and not state['State']['OOMKilled']
        (out/'acknowledged.json').write_text(json.dumps({'source':package['source'],'image':image,'apiHealth':True,'dockerHealthVerified':o.profile in ('full', 'maplibre'),'nativeMetadata':o.profile == 'full','gracefulExit':0,'authentication':auth_checked,'portalBrowser':portal_checked,'portalWeb5Browser':bool(web5_process),'portalPopupBreadth':portal_checked and (popup_breadth or popup_group != 'none'),'portalPopupGroup':popup_group,'web5GracefulExit':web5_process.graceful_exit if web5_process else None,'internalNetwork':True,'liveDeployment':False,'cqlAcceptance':False},indent=2))
    finally:
        shutdown_error=None
        try:
            processes.close()
        except Exception as cause:
            shutdown_error=type(cause).__name__
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
        (out/'cleanup.json').write_text(json.dumps({'ownedContainersAbsent':True,'ownedNetworkAbsent':True,'ephemeralDatabaseVolumesRemoved':True,'web5ProcessStopped':web5_process is None or web5_process.process.poll() is not None,'web5DnaAbsent':web5_process is None or not web5_process.dna_path.exists()}))
        if shutdown_error:
            raise RuntimeError('Owned process cleanup failed ('+shutdown_error+').')

if __name__=='__main__':main()
