import { readFileSync } from 'node:fs';
import { createHash } from 'node:crypto';
import { spawnSync } from 'node:child_process';

// No key reads, signer, broadcast, fixture creation, restore or contract deployment.
const endpoint='http://127.0.0.1:3030',contract='test.near';
const repo='/mnt/c/Source/OASIS',retained='/var/tmp/oasis-near-evidence';
const digest=value=>createHash('sha256').update(value).digest('hex');
async function rpc(method,params){
  const response=await fetch(endpoint,{method:'POST',headers:{'content-type':'application/json'},
    body:JSON.stringify({jsonrpc:'2.0',id:'interrupted-read-only',method,params}),signal:AbortSignal.timeout(15000)});
  if(!response.ok)throw new Error(`Read-only ${method} HTTP failure`);
  const body=await response.json();if(body.error||body.result===undefined)throw new Error(`Read-only ${method} RPC failure`);
  return body.result;
}
const status=await rpc('status',[]);
if(status.chain_id!=='oasis-near-local')throw new Error('Retained chain identity differs');
const code=await rpc('query',{request_type:'view_code',account_id:contract,finality:'final'});
const expectedCode=digest(readFileSync(`${retained}/build/oasis_storage.wasm`));
if(typeof code.code_base64!=='string'||digest(Buffer.from(code.code_base64,'base64'))!==expectedCode)
  throw new Error('Retained contract bytecode differs; no redeployment permitted');
const input={operation:'entries',rpcEndpoint:endpoint,networkId:'oasis-near-local',contractId:contract,prefix:''};
const child=spawnSync(process.execPath,[`${repo}/Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.NEAROASIS/SdkBridge/bridge.mjs`],
  {input:JSON.stringify(input),encoding:'utf8',timeout:120000,maxBuffer:70*1024*1024});
if(child.error||child.status!==0)throw new Error('Existing SDK read-only enumeration failed; no empty-state inference');
const envelope=JSON.parse(child.stdout);
if(envelope.ok!==true||!Array.isArray(envelope.result))throw new Error('Invalid existing SDK enumeration response');
const rows=envelope.result;
const fingerprints=rows.map(row=>{
  if(!Array.isArray(row)||row.length!==2||typeof row[0]!=='string'||typeof row[1]!=='string')throw new Error('Invalid record row');
  return [digest(row[0]),digest(row[1]),Buffer.byteLength(row[1])];
}).sort((left,right)=>left[0].localeCompare(right[0]));
const negative=JSON.parse(readFileSync(`${retained}/failed-status-current.json`,'utf8'));
const checked=JSON.parse(readFileSync(`${repo}/TestResults/NearRuntime/near-checked-sdk.complete.json`,'utf8'));
console.log(JSON.stringify({Mode:'read-only-interrupted-inspection',Chain:status.chain_id,Contract:contract,
  CodeSha256:expectedCode,FinalizedHeight:status.sync_info.latest_block_height,Rows:rows.length,
  InventorySha256:digest(JSON.stringify(fingerprints)),ValueBytes:fingerprints.reduce((sum,row)=>sum+row[2],0),
  NegativeJournalState:negative.state,CheckedJournalPhase:checked.phase,
  Caution:'Readable state inventory only; not proof of cleanup, original uncertain writes, full regression or native19PASS.'}));
