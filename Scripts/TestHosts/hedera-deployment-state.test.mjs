import {test} from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import {saveDeploymentState} from './hedera-deployment-state.mjs';

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
