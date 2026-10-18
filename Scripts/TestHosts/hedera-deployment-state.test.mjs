import {test} from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import {saveDeploymentState} from './hedera-deployment-state.mjs';
import {spawn} from 'node:child_process';
import {once} from 'node:events';

function fixture(run){
 const root=fs.mkdtempSync(path.join(os.tmpdir(),'oasis-hedera-journal-'));
 try{run(path.join(root,'deployment.json'));}
 finally{fs.rmSync(root,{recursive:true,force:true});}
}
test('creates and atomically replaces complete deployment intent',()=>fixture(statePath=>{
 const pending={sender:'0.0.1001',pending:{transactionId:'0.0.1001@1700000000.000000001'}};
 saveDeploymentState(statePath,pending);
 assert.deepEqual(JSON.parse(fs.readFileSync(statePath)),pending);
 const completed={...pending,pending:undefined,contractId:'0.0.2001'};
 saveDeploymentState(statePath,completed);
 assert.equal(JSON.parse(fs.readFileSync(statePath)).contractId,'0.0.2001');
 assert.equal(fs.existsSync(statePath+'.next'),false);
}));
test('leftover staging evidence fences overwrite and preserves original intent',()=>fixture(statePath=>{
 const original={pending:{transactionId:'original'}};
 saveDeploymentState(statePath,original);
 fs.writeFileSync(statePath+'.next','partial evidence');
 assert.throws(()=>saveDeploymentState(statePath,{pending:{transactionId:'replacement'}}),/EEXIST/);
 assert.deepEqual(JSON.parse(fs.readFileSync(statePath)),original);
 assert.equal(fs.readFileSync(statePath+'.next','utf8'),'partial evidence');
}));

test('actual process interruption before rename preserves original and fences the flushed next intent',async()=>{
 const root=fs.mkdtempSync(path.join(os.tmpdir(),'oasis-hedera-journal-crash-'));
 const statePath=path.join(root,'deployment.json');
 let child;
 try{
  const original={pending:{transactionId:'original'}};saveDeploymentState(statePath,original);
  const moduleUrl=new URL('./hedera-deployment-state.mjs',import.meta.url).href;
  const script=`import fs from 'node:fs';import {saveDeploymentState} from ${JSON.stringify(moduleUrl)};
   fs.renameSync=()=>{process.stdout.write('FLUSHED\\n');Atomics.wait(new Int32Array(new SharedArrayBuffer(4)),0,0);};
   saveDeploymentState(process.argv[1],{pending:{transactionId:'next-intent'}});`;
  child=spawn(process.execPath,['--input-type=module','-e',script,statePath],{stdio:['ignore','pipe','pipe']});
  const exit=once(child,'exit');
  await new Promise((resolve,reject)=>{
   const deadline=setTimeout(()=>reject(new Error('Child did not reach flushed journal boundary')),5000);
   child.once('error',error=>{clearTimeout(deadline);reject(error);});
   child.once('exit',()=>{clearTimeout(deadline);reject(new Error('Child exited before crash boundary'));});
   child.stdout.on('data',chunk=>{if(chunk.toString().includes('FLUSHED')){clearTimeout(deadline);resolve();}});
  });
  assert.equal(child.kill('SIGKILL'),true);await exit;
  assert.deepEqual(JSON.parse(fs.readFileSync(statePath)),original);
  assert.equal(JSON.parse(fs.readFileSync(statePath+'.next')).pending.transactionId,'next-intent');
  assert.throws(()=>saveDeploymentState(statePath,{pending:{transactionId:'replacement'}}),/EEXIST/);
 }finally{
  if(child&&child.exitCode===null&&child.signalCode===null){const exit=once(child,'exit');child.kill('SIGKILL');await exit;}
  fs.rmSync(root,{recursive:true,force:true});
 }
});
