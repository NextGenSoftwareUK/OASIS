import {test} from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';
const verifier=fileURLToPath(new URL('./verify-provider-sdk-bridge.mjs',import.meta.url));
function fixture(source,dependencies={}){
 const root=fs.mkdtempSync(path.join(os.tmpdir(),'oasis-bridge-verifier-'));
 try{
  fs.writeFileSync(path.join(root,'package.json'),JSON.stringify({name:'verification-fixture',dependencies}));
  fs.writeFileSync(path.join(root,'bridge.mjs'),source);
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
test('rejects invalid module syntax',()=>{
 const result=fixture('export function broken( {');
 assert.notEqual(result.status,0);assert.match(result.stderr,/Invalid bridge module/);
});
test('rejects unused declared SDK',()=>{
 const result=fixture("import fs from 'node:fs';",{'unused-sdk':'1.0.0'});
 assert.notEqual(result.status,0);assert.match(result.stderr,/no verified bridge import/);
});
