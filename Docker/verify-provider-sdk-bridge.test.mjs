import {test} from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';
const verifier=fileURLToPath(new URL('./verify-provider-sdk-bridge.mjs',import.meta.url));
function fixture(source,dependencies={},files={}){
 const root=fs.mkdtempSync(path.join(os.tmpdir(),'oasis-bridge-verifier-'));
 try{
  fs.writeFileSync(path.join(root,'package.json'),JSON.stringify({name:'verification-fixture',dependencies}));
  fs.writeFileSync(path.join(root,'package-lock.json'),JSON.stringify({lockfileVersion:3,packages:{
   '':{dependencies},...Object.fromEntries(Object.entries(dependencies).map(([name,version])=>[`node_modules/${name}`,{version}]))}}));
  fs.writeFileSync(path.join(root,'bridge.mjs'),source);
  for(const [name,contents] of Object.entries(files)){
   const file=path.join(root,name);fs.mkdirSync(path.dirname(file),{recursive:true});fs.writeFileSync(file,contents);
  }
  return spawnSync(process.execPath,[verifier,root],{encoding:'utf8',timeout:10000});
 }finally{fs.rmSync(root,{recursive:true,force:true});}
}
test('accepts existing builtin imports without executing bridge operations',()=>{
 const result=fixture("import fs from 'node:fs'; throw new Error('Must not execute bridge entry point');");
 assert.equal(result.status,0,result.stderr);
});
test('rejects absent dynamic companion module',()=>{
 const result=fixture("async function operation(){await import('./missing.mjs');}");
 assert.notEqual(result.status,0);assert.match(result.stderr,/missing.mjs/);
});

test('rejects absent side-effect companion module',()=>{
 const result=fixture("import './missing-setup.mjs';");
 assert.notEqual(result.status,0);assert.match(result.stderr,/missing-setup.mjs/);
});

test('resolves side-effect SDK imports without executing bridge operations',()=>{
 const result=fixture("import 'side-effect-sdk'; throw new Error('Must not execute bridge');",{'side-effect-sdk':'1.0.0'},{
  'node_modules/side-effect-sdk/package.json':JSON.stringify({name:'side-effect-sdk',version:'1.0.0',type:'module',exports:'./index.mjs'}),
  'node_modules/side-effect-sdk/index.mjs':'export const ready=true;'
 });
 assert.equal(result.status,0,result.stderr);
});
test('rejects invalid module syntax',()=>{
 const result=fixture('export function broken( {');
 assert.notEqual(result.status,0);assert.match(result.stderr,/Invalid bridge module/);
});
test('rejects unused declared SDK',()=>{
 const result=fixture("import fs from 'node:fs';",{'unused-sdk':'1.0.0'});
 assert.notEqual(result.status,0);assert.match(result.stderr,/no verified bridge import/);
});
test('uses ESM conditional exports rather than CommonJS entry points',()=>{
 const result=fixture("import sdk from 'conditional-sdk';",{'conditional-sdk':'1.0.0'},{
  'node_modules/conditional-sdk/package.json':JSON.stringify({name:'conditional-sdk',version:'1.0.0',type:'module',exports:{import:'./esm.mjs',require:'./missing.cjs'}}),
  'node_modules/conditional-sdk/esm.mjs':'export default {};'
 });
 assert.equal(result.status,0,result.stderr);
});
test('rejects an importable SDK with a different installed version',()=>{
 const result=fixture("import sdk from 'versioned-sdk';",{'versioned-sdk':'1.0.0'},{
  'node_modules/versioned-sdk/package.json':JSON.stringify({name:'versioned-sdk',version:'2.0.0',type:'module',exports:'./index.mjs'}),
  'node_modules/versioned-sdk/index.mjs':'export default {};'
 });
 assert.notEqual(result.status,0);assert.match(result.stderr,/Installed SDK.*pinned version/);
});
test('rejects a lockfile that differs from the declared SDK pin',()=>{
 const result=fixture("import sdk from 'versioned-sdk';",{'versioned-sdk':'1.0.0'},{
  'package-lock.json':JSON.stringify({packages:{'':{dependencies:{'versioned-sdk':'1.0.0'}},'node_modules/versioned-sdk':{version:'2.0.0'}}}),
  'node_modules/versioned-sdk/package.json':JSON.stringify({name:'versioned-sdk',version:'1.0.0',type:'module',exports:'./index.mjs'}),
  'node_modules/versioned-sdk/index.mjs':'export default {};'
 });
 assert.notEqual(result.status,0);assert.match(result.stderr,/manifest\/lockfile version mismatch/);
});
