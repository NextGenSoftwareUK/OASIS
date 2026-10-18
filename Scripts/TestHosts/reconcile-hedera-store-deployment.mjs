import fs from 'node:fs';import path from 'node:path';import {fileURLToPath,pathToFileURL} from 'node:url';
import {saveDeploymentState} from './hedera-deployment-state.mjs';

export function reconcileFailedCreation(state,response) {
  if(state.pending?.action!=='contractCreate')throw new Error('Only the retained contract-creation failure is supported by this reconciliation.');
  const parts=/^([0-9]+\.[0-9]+\.[1-9][0-9]*)@([0-9]+)\.([0-9]{9})$/.exec(state.pending.transactionId??'');
  if(!parts||parts[1]!==state.sender)throw new Error('Pending payer identity mismatch.');
  const mirrorId=parts[1]+'-'+parts[2]+'-'+parts[3];
  const records=response.transactions;
  if(!Array.isArray(records)||records.length!==1)throw new Error('Reconciliation requires exactly one authoritative transaction.');
  const record=records[0];
  if(record.transaction_id!==mirrorId||record.name!=='CONTRACTCREATEINSTANCE'||record.result!=='INSUFFICIENT_TX_FEE'
    ||record.scheduled!==false||record.nonce!==0||record.parent_consensus_timestamp!==null||record.entity_id!==null)
    throw new Error('Receipt does not prove the expected terminal failed creation without a contract.');
  const next=structuredClone(state);
  next.reconciledFailures??=[];
  next.reconciledFailures.push({transactionId:state.pending.transactionId,status:record.result,consensusTimestamp:record.consensus_timestamp});
  delete next.pending;return next;
}

async function main(){
 const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'../..');
 const statePath=path.join(root,'TestResults/HederaRuntime/deployment-store.json');
 if(fs.lstatSync(statePath).isSymbolicLink())throw new Error('Refusing linked deployment evidence.');
 const leasePath=statePath+'.lock',lease=fs.openSync(leasePath,'wx');
 try {
  if(fs.existsSync(statePath+'.next'))throw new Error('Retained staged deployment evidence requires review before reconciliation.');
  const state=JSON.parse(fs.readFileSync(statePath,'utf8'));
  const match=/^(0\.0\.[1-9][0-9]*)@([0-9]+)\.([0-9]{9})$/.exec(state.pending?.transactionId??'');
  if(!match||match[1]!==state.sender)throw new Error('No valid pending testnet identity to reconcile.');
  const id=match[1]+'-'+match[2]+'-'+match[3];
  const response=await fetch('https://testnet.mirrornode.hedera.com/api/v1/transactions/'+id,{redirect:'error',signal:AbortSignal.timeout(20000)});
  if(!response.ok)throw new Error('Official mirror did not establish a receipt: HTTP '+response.status);
  const next=reconcileFailedCreation(state,await response.json());
  saveDeploymentState(statePath,next);
  console.log(JSON.stringify({reconciledTransaction:state.pending.transactionId,status:'INSUFFICIENT_TX_FEE',fileId:next.fileId,submittedNewTransaction:false}));
 } finally {fs.closeSync(lease);fs.unlinkSync(leasePath);}
}
if(process.argv[1]&&import.meta.url===pathToFileURL(process.argv[1]).href)await main();
