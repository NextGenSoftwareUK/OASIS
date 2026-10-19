// Retained local native proof only. An unresolved journal is never reset/replayed.
import {readFileSync,openSync,writeFileSync,fsyncSync,closeSync,existsSync,renameSync} from 'node:fs';
import {randomUUID,randomBytes,createHash} from 'node:crypto';
import {Account,JsonRpcProvider,actions,teraToGas} from '../../../Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.NEAROASIS/SdkBridge/node_modules/near-api-js/lib/index.js';
const root='/var/tmp/oasis-near-evidence',accountId='test.near';
const pending=root+'/checked-record-proof.pending.json',complete=root+'/checked-record-proof.complete.json';
if(existsSync(pending)||existsSync(complete))throw new Error('Retained checked-record proof exists; review it instead of resubmitting.');
const provider=new JsonRpcProvider({url:'http://127.0.0.1:3030'});
if((await provider.viewNodeStatus()).chain_id!=='oasis-near-local')throw new Error('Only retained local chain permitted');
const code=await provider.viewContractCode({contractId:accountId});
if(typeof code.code_base64!=='string'||createHash('sha256').update(Buffer.from(code.code_base64,'base64')).digest('hex')!=='638719d44b0305bded6096a68230d505e1fa6b811e404df8c12e0fd22d6fcefe')throw new Error('Reviewed code differs');
if(await provider.callFunction({contractId:accountId,method:'record_write_protocol',args:{}})!==1)throw new Error('Checked protocol differs');
const signer=JSON.parse(readFileSync(root+'/ledger/validator_key.json','utf8'));
if(signer.account_id!==accountId)throw new Error('Retained signer differs');
const account=new Account(accountId,provider,signer.secret_key);
const id=randomUUID(),key='holon:'+id,archive='history:'+key+':0000000001';
// Opaque random native test values, NOT private-record encryption/codec acceptance evidence.
const values=Array.from({length:3},()=>randomBytes(48).toString('base64'));
const evidence={chain:'oasis-near-local',accountId,id,key,archive,state:'planned',receipts:[]};
function persist(flag='w'){const fd=openSync(pending,flag,0o600);try{writeFileSync(fd,JSON.stringify(evidence));fsyncSync(fd);}finally{closeSync(fd);}}
const get=key=>provider.callFunction({contractId:accountId,method:'get',args:{key}});
persist('wx');
async function call(label,method,args,success){
  evidence.state='before-'+label;persist();
  const outcome=await account.signAndSendTransaction({receiverId:accountId,actions:[actions.functionCall(method,args,teraToGas('100'),0n)],waitUntil:'FINAL',throwOnFailure:false});
  const hash=outcome.transaction_outcome?.id;
  if(!hash)throw new Error('Missing original native identity; preserve journal');
  const entry={label,hash,status:outcome.status,tokensBurnt:[outcome.transaction_outcome,...outcome.receipts_outcome].reduce((sum,row)=>sum+BigInt(row.outcome.tokens_burnt),0n).toString()};
  evidence.receipts.push(entry);evidence.state='acknowledged-'+label;persist();
  const read=await provider.viewTransactionStatus({txHash:hash,accountId,waitUntil:'NONE'});
  if(read.transaction_outcome?.id!==hash||read.final_execution_status!=='FINAL'||JSON.stringify(read.status)!==JSON.stringify(outcome.status))throw new Error('Original finalized receipt differs');
  if(outcome.final_execution_status!=='FINAL'||(success?!Object.hasOwn(outcome.status,'SuccessValue'):!outcome.status?.Failure))throw new Error('Unexpected native outcome');
}
async function unchanged(current,history){if(await get(key)!==current||await get(archive)!==history)throw new Error('Native record/archive changed unexpectedly');}
if(await get(key)!==null||await get(archive)!==null)throw new Error('Fixture collision; preserve original');
await call('create','put_record_checked',{key,expected_value:null,value:values[0]},true);await unchanged(values[0],null);
await call('stale','put_record_checked',{key,expected_value:'not-the-original',value:values[1],archive_key:archive,archive_value:values[0]},false);await unchanged(values[0],null);
await call('replace','put_record_checked',{key,expected_value:values[0],value:values[1],archive_key:archive,archive_value:values[0]},true);await unchanged(values[1],values[0]);
await call('archive-collision','put_record_checked',{key,expected_value:values[1],value:values[2],archive_key:archive,archive_value:values[1]},false);await unchanged(values[1],values[0]);
await call('create-collision','put_record_checked',{key,expected_value:null,value:values[2]},false);await unchanged(values[1],values[0]);
await call('cleanup-current','delete_record',{key},true);
await call('cleanup-archive','delete_record',{key:archive},true);await unchanged(null,null);
evidence.state='verified-fixtures-absent';persist();renameSync(pending,complete);
console.log(JSON.stringify(evidence));
