import {existsSync,readFileSync,openSync,writeFileSync,fsyncSync,closeSync} from 'node:fs';
import {Account,actions,teraToGas} from 'near-api-js';

// A run-scoped negative fixture, not a replay/replacement of the historical original.
export async function prepareFailedStatusFixture(provider,accountId,privateKey,root) {
  if(root!=='/var/tmp/oasis-near-evidence')throw new Error('Only the retained fixture directory is authorized');
  const config=JSON.parse(readFileSync(root+'/ledger/config.json','utf8'));
  if(config.gc_num_epochs_to_keep<50)throw new Error('Native suite requires an explicitly configured verification retention window of50epochs; restore original5after stopping the node.');
  const path=root+'/failed-status-current.json';
  if(existsSync(path)){
    const prior=JSON.parse(readFileSync(path,'utf8'));
    if(prior.state!=='verified-failed')throw new Error('Unresolved failed-status fixture submission; reconcile original, never replay.');
    const history=openSync(root+'/failed-status-history.ndjson','a',0o600);
    try{writeFileSync(history,JSON.stringify(prior)+'\n');fsyncSync(history);}finally{closeSync(history);}
  }
  function persist(value){const fd=openSync(path,'w',0o600);try{writeFileSync(fd,JSON.stringify(value));fsyncSync(fd);}finally{closeSync(fd);}}
  const evidence={chain:'oasis-near-local',accountId,state:'before-negative-fixture-submission',createdAt:new Date().toISOString()};
  if((await provider.viewNodeStatus()).chain_id!==evidence.chain||accountId!=='test.near')throw new Error('Negative fixture requires retained local chain/account');
  persist(evidence);
  const account=new Account(accountId,provider,privateKey);
  const outcome=await account.signAndSendTransaction({receiverId:accountId,
    actions:[actions.functionCall('oasis_negative_fixture_missing_method',{},teraToGas('5'),0n)],waitUntil:'FINAL',throwOnFailure:false});
  evidence.hash=outcome.transaction_outcome?.id;evidence.state='acknowledged';persist(evidence);
  if(!evidence.hash||outcome.final_execution_status!=='FINAL'||!outcome.status?.Failure)throw new Error('Negative fixture did not finalize with native Failure');
  const read=await provider.viewTransactionStatus({txHash:evidence.hash,accountId,waitUntil:'NONE'});
  if(read.transaction_outcome?.id!==evidence.hash||read.final_execution_status!=='FINAL'||!read.status?.Failure)throw new Error('Independent native failed-outcome readback differs');
  Object.assign(evidence,{state:'verified-failed',blockHash:outcome.transaction_outcome.block_hash,failure:outcome.status.Failure});persist(evidence);
  return evidence;
}
