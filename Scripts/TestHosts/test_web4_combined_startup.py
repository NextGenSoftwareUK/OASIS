"""Lock the actual cloud startup health probe's readiness/error boundary."""
import ast
import os
from pathlib import Path
import subprocess
import unittest


class StartupProbeTests(unittest.TestCase):
    def test_tron_api_profile_enrolls_only_isolated_mongo_and_no_native_reader(self):
        import runpy
        import json
        root = Path(__file__).resolve().parents[2]
        create = runpy.run_path(str(Path(__file__).with_name('qualify_web4_combined_startup.py')))['configuration']
        env = create(root, 'mongodb://mongo:27017/?replicaSet=rs0', 'oasis_startup_1_1', 'tron-api')
        admission = json.loads(env['PREPARED_STORAGE_ADMISSION_JSON'])
        self.assertEqual(['MongoDBOASIS'], admission['ordinaryProviders'])
        self.assertEqual([], admission['queryOnlyProviders'])
        self.assertEqual(['MongoDBOASIS'], [row['provider'] for row in admission['queryTariffs']])
        self.assertEqual('false', env['OASIS_SANDBOX_OPENVINO'])
        for key in ['CQL_OPENVINO_MODEL_QUERY_SOURCE_OWNER',
                    'CQL_TRON_PUBLIC_HEADER_QUERY_SOURCE_JSON', 'COSMIC_TRON_PUBLIC_READER_KEY']:
            self.assertNotIn(key, env)
        script = "const fs=require('node:fs');require(process.argv[1]).assertSandbox(fs.readFileSync(0,'utf8'));"
        result = subprocess.run([os.environ.get('NODE_BINARY', 'node'), '-e', script,
            str(root/'Docker/host-sandbox-ipfs.cjs')], input=env['OASIS_DNA_JSON'],
            text=True, capture_output=True, timeout=8)
        self.assertEqual(0, result.returncode, result.stderr)

    def test_unknown_startup_profile_is_rejected(self):
        import runpy
        create = runpy.run_path(str(Path(__file__).with_name('qualify_web4_combined_startup.py')))['configuration']
        with self.assertRaises(ValueError):
            create(Path(__file__).resolve().parents[2], 'unused', 'unused', 'unknown')

    def test_actual_package_probe_requires_reader_metadata_and_full_model_checks(self):
        import json
        tree = ast.parse(Path(__file__).with_name('qualify_web4_full_package.py').read_text(encoding='utf-8-sig'))
        probe = next(n.value.value for n in ast.walk(tree) if isinstance(n, ast.Assign)
                     and any(isinstance(t, ast.Name) and t.id == 'probe' for t in n.targets))
        harness = r"""
const vm=require('node:vm'),crypto=require('node:crypto'),assert=require('node:assert/strict');
const o=JSON.parse(process.argv[1]),helper=Buffer.from('controlled-helper'),model=Buffer.from('controlled-model');
const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
const expected={'/app/host-sandbox-ipfs.cjs':hash(helper)};
let result,modelReads=0;
const fs={readFileSync:p=>{
 if(p==='/app/host-sandbox-ipfs.cjs')return helper;
 if(p==='/app/NextGenSoftware.OASIS.API.Providers.TRONOASIS.dll'){
   if(o.missingFile)throw Error('controlled missing reader assembly');
   return Buffer.from('TronPublicHeaderMetadataSource '+(o.missingMethod?'':'FromAuthenticatedJson'));
 }
 if(p==='/app/NextGenSoftware.OASIS.API.Providers.UrbitOASIS.dll'){
   if(o.missingUrbit)throw Error('controlled missing Urbit assembly');
   return Buffer.from('UrbitContactsV1Client ReadBookPageAsync '+(o.missingUrbitQuery?'':'UrbitContactBookQuerySource'));
 }
 if(p==='/app/NextGenSoftware.OASIS.API.ONODE.WebAPI.dll')return Buffer.from(o.missingUrbitRegistration?'':'CqlUrbitReadSourceRegistration AddUrbitReadSource');
 if(p==='/app/NextGenSoftware.OASIS.API.ONODE.WebAPI.deps.json'){
   const name='NextGenSoftware.OASIS.API.Providers.UrbitOASIS',key=name+'/2.0.0';
   const target={[key]:{runtime:{[name+'.dll']:{}}},'NextGenSoftware.OASIS.API.ONODE.WebAPI/2.0.0':{dependencies:{[name]:'2.0.0'}}};
   if(o.missingUrbitRuntime)delete target[key].runtime[name+'.dll'];
   if(o.missingUrbitDependency)delete target['NextGenSoftware.OASIS.API.ONODE.WebAPI/2.0.0'].dependencies[name];
   return JSON.stringify({runtimeTarget:{name:'net10.0'},targets:{'net10.0':target},libraries:{[key]:{type:o.urbitNotProject?'package':'project'}}});
 }
 if(p.startsWith('/models/')){modelReads++;return model;}
 throw Error('Unexpected controlled file '+p);
},statSync:()=>({size:1}),realpathSync:()=>'/opt/ovms-rootfs/ovms'};
const pins={modelName:'controlled',versions:['1','2','3'],files:{'graph':{bytes:model.length,sha256:hash(model)}}};
vm.runInNewContext(PROBE,{Buffer,require:p=>{
 if(p==='node:fs')return fs;if(p==='node:crypto')return crypto;if(p==='node:assert/strict')return assert;
 if(p==='/app/openvino-model-server-pins.json')return pins;throw Error('Unexpected controlled module '+p);
},process:{argv:['node',JSON.stringify(expected),JSON.stringify(o.reader),o.profile,JSON.stringify(o.urbit===true)]},console:{log:s=>result=JSON.parse(s)}});
assert.equal(modelReads,o.profile==='full'?3:0);
console.log(JSON.stringify(result));
""".replace('PROBE', json.dumps(probe))
        cases = [({'profile':'full','reader':False}, 0, 6),
                 ({'profile':'full','reader':True}, 0, 7),
                 ({'profile':'maplibre','reader':False}, 0, 3),
                 ({'profile':'tron-api','reader':True}, 0, 3),
                 ({'profile':'tron-api','reader':True,'missingMethod':True}, 1, None),
                 ({'profile':'tron-api','reader':True,'missingFile':True}, 1, None),
                 ({'profile':'tron-api','reader':False}, 1, None),
                 ({'profile':'full','reader':False,'urbit':True}, 0, 7),
                 ({'profile':'full','reader':True,'urbit':True}, 0, 8),
                 ({'profile':'tron-api','reader':True,'urbit':True}, 1, None)]
        cases += [({'profile':'full','reader':False,'urbit':True,key:True},1,None)
                  for key in ['missingUrbit','missingUrbitQuery','missingUrbitRegistration',
                              'missingUrbitRuntime','missingUrbitDependency','urbitNotProject']]
        for options, status, count in cases:
            with self.subTest(options=options):
                result = subprocess.run([os.environ.get('NODE_BINARY','node'), '-e', harness,
                    json.dumps(options)], text=True, capture_output=True, timeout=8)
                self.assertEqual(status, result.returncode, result.stderr)
                if status == 0:
                    proof = json.loads(result.stdout)
                    self.assertEqual(count, proof['assemblies'])
                    self.assertEqual(options.get('urbit',False), proof['urbitReaderPackaged'])
                    self.assertEqual(options['profile']=='full', proof['modelHashes'])

    def test_shipped_health_command_uses_published_runtime_and_rejects_bad_http(self):
        import json
        import re
        dockerfile = Path(__file__).resolve().parents[2] / 'Docker/Dockerfile.web4'
        match = re.search(r'HEALTHCHECK[^\n]+\n\s*CMD (\[.*\])', dockerfile.read_text(encoding='utf-8-sig'))
        self.assertIsNotNone(match)
        command = json.loads(match.group(1))
        self.assertEqual(['node', '-e'], command[:2])
        self.assertEqual(3, len(command))
        for label, response, expected in [
            ('healthy', 'return {status:200}', 0),
            ('unavailable', 'return {status:503}', 1),
            ('redirect', 'return {status:302}', 1),
            ('connection-error', "throw Error('controlled connection failure')", 1),
        ]:
            with self.subTest(label=label):
                harness = "globalThis.fetch=async(u,o)=>{if(u!=='http://127.0.0.1:8080/api/health'||o.redirect!=='error'||!(o.signal instanceof AbortSignal))throw Error('wrong health contract');" + response + '};\n' + command[2]
                result = subprocess.run([os.environ.get('NODE_BINARY','node'),'-e',harness],
                    capture_output=True,text=True,timeout=5)
                self.assertEqual(expected,result.returncode,result.stderr)

    def test_full_startup_requires_actual_docker_health_success(self):
        import runpy
        ready = runpy.run_path(str(Path(__file__).with_name('qualify_web4_combined_startup.py')))['image_health_ready']
        def state(status='healthy', code=0):
            return {'Config':{'Healthcheck':{'Test':['CMD','node','-e','probe']}},
                    'State':{'Running':True,'Health':{'Status':status,'Log':[{'ExitCode':code}]}}}
        self.assertTrue(ready(state()))
        self.assertFalse(ready(state('starting',1)))
        bad = [state('unhealthy',1),state('healthy',1),state('missing',0)]
        missing = state(); del missing['Config']['Healthcheck']; bad.append(missing)
        disabled = state(); disabled['Config']['Healthcheck']['Test']=['NONE']; bad.append(disabled)
        stopped = state(); stopped['State']['Running']=False; bad.append(stopped)
        empty = state(); empty['State']['Health']['Log']=[]; bad.append(empty)
        missing_curl = state('unhealthy',1)
        missing_curl['State']['Health']['Log'][-1]['Output']='/bin/sh: 1: curl: not found'
        bad.append(missing_curl)
        for observed in bad:
            with self.subTest(observed=observed):
                with self.assertRaises(RuntimeError): ready(observed)

    def test_generated_dna_satisfies_actual_native_supervisor_guard(self):
        import runpy
        root=Path(__file__).resolve().parents[2]
        create=runpy.run_path(str(Path(__file__).with_name('qualify_web4_combined_startup.py')))['configuration']
        env=create(root,'mongodb://mongo:27017/?replicaSet=rs0','oasis_startup_1_1')
        import json
        self.assertEqual('OASISHyperDrive2',json.loads(env['OASIS_DNA_JSON'])['OASIS']['HyperDriveMode'])
        admission=json.loads(env['PREPARED_STORAGE_ADMISSION_JSON'])
        policy=admission['routes'][0]['policy']
        self.assertTrue(policy['requiresProviderMeasurement'])
        self.assertEqual(0,policy['maximumNativeFeeCostUsd'])
        self.assertEqual(1,policy['maximumProviderAttempts'])
        self.assertGreater(policy['reservedUnits'],0)
        for tariff in admission['queryTariffs']:
            self.assertEqual(policy['pricingCatalogueVersion'],tariff['catalogueVersion'])
            self.assertLessEqual(tariff['costUsd']*policy['maximumProviderAttempts'],policy['estimatedCostUsd'])
        script="const fs=require('node:fs');require(process.argv[1]).assertSandbox(fs.readFileSync(0,'utf8'));"
        result=subprocess.run([os.environ.get('NODE_BINARY','node'),'-e',script,str(root/'Docker/host-sandbox-ipfs.cjs')],
                              input=env['OASIS_DNA_JSON'],text=True,capture_output=True,timeout=8)
        self.assertEqual(0,result.returncode,result.stderr)

    def test_typed_docker_absence_excludes_daemon_and_permission_failures(self):
        import runpy
        missing = runpy.run_path(str(Path(__file__).with_name('qualify_web4_combined_startup.py')))['missing_resource']
        for kind, message in [('network','Error response from daemon: network owned not found'),
                              ('container','Error: No such container: owned'),
                              ('volume','Error response from daemon: get owned: no such volume')]:
            with self.subTest(kind=kind):
                self.assertTrue(missing(kind,'owned',1,message))
                self.assertFalse(missing(kind,'other',1,message))
                self.assertFalse(missing(kind,'owned',125,message))
                self.assertFalse(missing(kind,'owned',1,'permission denied'))
                self.assertFalse(missing(kind,'owned',1,'Cannot connect to the Docker daemon'))

    def test_actual_probe_classifies_readiness_without_masking_errors(self):
        tree = ast.parse(Path(__file__).with_name('qualify_web4_combined_startup.py').read_text(encoding='utf-8-sig'))
        probe = next(n.value.value for n in ast.walk(tree) if isinstance(n, ast.Assign)
                     and any(isinstance(t, ast.Name) and t.id == 'probe' for t in n.targets))
        cases = [
            ('healthy', "return {ok:true,status:200,json:async()=>({status:'healthy',service:'OASIS API ONODE WebAPI'})}", 0),
            ('wrong-service', "return {ok:true,status:200,json:async()=>({status:'healthy',service:'other'})}", 1),
            ('forbidden', "return {ok:false,status:403}", 1),
            ('warming', "return {ok:false,status:503}", 2),
            ('connection-refused', "throw Object.assign(Error('connecting'),{cause:{code:'ECONNREFUSED'}})", 2),
            ('unexpected-network-error', "throw Object.assign(Error('network'),{cause:{code:'ENOTFOUND'}})", 1),
            ('invalid-json', "return {ok:true,status:200,json:async()=>{throw SyntaxError('invalid')}}", 1),
        ]
        for label, response, expected in cases:
            with self.subTest(label=label):
                script = 'globalThis.fetch=async()=>{' + response + '};\n' + probe
                result = subprocess.run([os.environ.get('NODE_BINARY', 'node'), '-e', script], capture_output=True, text=True, timeout=8)
                self.assertEqual(expected, result.returncode, result.stderr)


if __name__ == '__main__':
    unittest.main()
