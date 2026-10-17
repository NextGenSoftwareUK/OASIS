// Explicit retained local-chain acceptance only; excluded from offline npm test.
import {test} from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync,existsSync,openSync,writeSync,fsyncSync,closeSync,renameSync,unlinkSync} from 'node:fs';
import {spawnSync} from 'node:child_process';
import {createHash} from 'node:crypto';
import {invoke} from './bridge.mjs';
import {journaledMutation} from './fixture-submission.mjs';

const rpcEndpoint=process.env.OASIS_SUI_TEST_RPC;
const deploymentPath=process.env.OASIS_SUI_TEST_DEPLOYMENT;
const journalPath=process.env.OASIS_SUI_TEST_ESCROW_JOURNAL;
const cachePath=process.env.OASIS_SUI_TEST_ESCROW_CACHE;
if(!rpcEndpoint||!deploymentPath||!journalPath||!cachePath)throw new Error('Explicit retained local escrow settings required');
if(!new Set(['127.0.0.1','localhost','[::1]']).has(new URL(rpcEndpoint).hostname))throw new Error('Public-chain spending forbidden');

test('retained local Sui exact deposit custody commits original prepared transactions and actual fees',{timeout:240000},async()=>{
 assert.equal(existsSync(journalPath),false,'Preserve original pending ownership; do not replay');
 const originalDeploymentBytes=readFileSync(deploymentPath);
 const original=JSON.parse(originalDeploymentBytes);
 const node={rpcEndpoint,network:'localnet',chainId:original.chainId};
 assert.equal((await invoke({...node,operation:'probe'})).chainIdentifier,original.chainId);
 const compiled=spawnSync('wsl.exe',['-d','Ubuntu','--','/var/tmp/oasis-runtimes/sui-1.81.1/sui','move','build',
  '--path','/mnt/c/Source/OASIS-hyperdrive-v2-gaps/Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.SuiOASIS/contracts',
  '--build-env','mainnet','--dump-bytecode-as-base64','--no-tree-shaking'],{encoding:'utf8',timeout:90000,maxBuffer:4*1024*1024});
 assert.ifError(compiled.error);assert.equal(compiled.status,0,compiled.stderr);
 const artifact=JSON.parse(compiled.stdout);
 assert.ok(Array.isArray(artifact.modules)&&artifact.modules.length>0&&Array.isArray(artifact.dependencies));
 const compiledSha256=createHash('sha256').update(JSON.stringify(artifact)).digest('hex');
 const owner=await invoke({operation:'restoreKey',privateKey:original.privateKey});
 const other=JSON.parse(readFileSync(deploymentPath+'.token-wallet.json','utf8'));
 assert.equal(other.chainId,original.chainId);
 const stranger=await invoke({operation:'restoreKey',privateKey:other.wallet.privateKey});
 assert.notEqual(stranger.address,owner.address);
 const depositId=createHash('sha256').update('11111111222233334444555555555555').digest('hex');
 let cached=existsSync(cachePath)?JSON.parse(readFileSync(cachePath,'utf8')):null;
 if(cached){
  assert.equal(cached.chainId,original.chainId);assert.equal(cached.compiledSha256,compiledSha256,'Reviewed package changed; never auto-publish another');
  assert.equal(cached.completed,true,'Incomplete retained deployment requires original-attempt reconciliation');
 }
 const verifyFinal=async deployment=>{
  const base={...node,...deployment,privateKey:original.privateKey};
  const record=await invoke({...base,operation:'tokenEscrowGet',depositId});
  assert.equal(record.owner,owner.address);assert.equal(record.amount,'123');assert.equal(record.released,true);assert.equal(record.funds,'0');
  assert.equal(await invoke({...base,operation:'tokenBalance',walletAddress:owner.address}),'0');
  assert.equal((await invoke({...base,operation:'tokenProbe'})).treasury.supply,'0');
 };
 if(cached){await verifyFinal(cached);console.log('Retained escrow deployment reverified read-only; no new publication or mutation.');return;}
 let journal=openSync(journalPath,'wx'),unknown=false,complete=false;
 const record=value=>{writeSync(journal,JSON.stringify(value)+'\n');fsyncSync(journal);};
 const mutate=request=>{
  if(unknown)throw new Error('Original mutation outcome unresolved; no further write permitted');
  return journaledMutation(invoke,request,record,()=>{unknown=true;});
 };
 const saveCache=value=>{
  const temp=cachePath+'.tmp';const file=openSync(temp,'wx');
  try{writeSync(file,JSON.stringify(value));fsyncSync(file);}finally{closeSync(file);}
  renameSync(temp,cachePath);
 };
 try{
  record({stage:'intent',chainId:original.chainId,compiledSha256,depositId,creator:owner.address});
  const published=await mutate({...node,operation:'publish',privateKey:original.privateKey,modules:artifact.modules,dependencies:artifact.dependencies});
  const base={...node,...published,privateKey:original.privateKey};
  cached={chainId:original.chainId,compiledSha256,...published,completed:false};saveCache(cached);
  const created=await mutate({...base,operation:'tokenEscrowCreate',fromWalletAddress:owner.address});
  Object.assign(base,{escrowObjectId:created.escrowObjectId});
  Object.assign(cached,{escrowObjectId:created.escrowObjectId,creationHash:created.transactionHash});saveCache(cached);
  assert.equal(await invoke({...base,operation:'tokenEscrowGet',depositId}),null);
  assert.equal((await invoke({...base,operation:'tokenProbe'})).treasury.supply,'0');
  await mutate({...base,operation:'tokenMint',recipient:owner.address,amountUnits:'1000'});
  const locked=await mutate({...base,operation:'tokenEscrowLock',fromWalletAddress:owner.address,depositId,amountUnits:'123'});
  assert.equal(locked.amountUnits,'123');assert.equal(locked.released,false);
  assert.equal(await invoke({...base,operation:'tokenBalance',walletAddress:owner.address}),'877');
  const active=await invoke({...base,operation:'tokenEscrowGet',depositId});assert.equal(active.funds,'123');
  await assert.rejects(mutate({...base,privateKey:other.wallet.privateKey,operation:'tokenEscrowRelease',fromWalletAddress:stranger.address,depositId}),/owned deposit/);
  const released=await mutate({...base,operation:'tokenEscrowRelease',fromWalletAddress:owner.address,depositId});
  assert.equal(released.amountUnits,'123');assert.equal(released.released,true);
  assert.equal(await invoke({...base,operation:'tokenBalance',walletAddress:owner.address}),'1000');
  await assert.rejects(mutate({...base,operation:'tokenEscrowLock',fromWalletAddress:owner.address,depositId,amountUnits:'1'}),/already used/);
  await assert.rejects(mutate({...base,operation:'tokenEscrowRelease',fromWalletAddress:owner.address,depositId}),/owned deposit/);
  await mutate({...base,operation:'tokenBurn',amountUnits:'1000'});
  await verifyFinal(base);
  const hashes=[...new Set(readFileSync(journalPath,'utf8').trim().split('\n').map(line=>JSON.parse(line))
   .filter(entry=>entry.stage==='prepared').map(entry=>entry.evidence.transactionHash))];
  const fees=[];
  for(const transactionHash of hashes){
   const receipt=await invoke({...node,operation:'nativeFeeReceipt',transactionHash});
   assert.equal(receipt.transactionHash,transactionHash);assert.equal(receipt.payer,owner.address);assert.equal(receipt.success,true);
   fees.push({transactionHash,netMIST:receipt.netUnits,components:receipt.components});
  }
  assert.ok(hashes.length>=6,'Every native stage must retain its original signed evidence');
  Object.assign(cached,{completed:true,depositId,fees});saveCache(cached);
  assert.equal(JSON.parse(readFileSync(cachePath,'utf8')).completed,true);
  assert.deepEqual(readFileSync(deploymentPath),originalDeploymentBytes,'Original storage deployment must remain unchanged');
  complete=true;console.log(JSON.stringify({escrowObjectId:created.escrowObjectId,packageAddress:published.packageAddress,originalNativeStages:hashes.length,fees}));
 }finally{
  closeSync(journal);if(complete)unlinkSync(journalPath);
  // Any failure preserves signed evidence, partial cache and balances; no automatic cleanup write/replay.
 }
});
