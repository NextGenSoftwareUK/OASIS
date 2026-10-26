// One reviewed retained-local code upgrade. Never initializes, resets or retries a submission.
import {readFileSync,openSync,writeFileSync,fsyncSync,closeSync,existsSync} from 'node:fs';
import {createHash} from 'node:crypto';
import {Account,JsonRpcProvider} from '../../../Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.NEAROASIS/SdkBridge/node_modules/near-api-js/lib/index.js';
import {prepareNearDeployment,submitNearDeployment,exactNearSubmission,recoverNearDeployment,verifyNearDeployment} from '../../../Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.NEAROASIS/SdkBridge/native-preparation.mjs';
const root='/var/tmp/oasis-near-evidence',rpc='http://127.0.0.1:3030',accountId='test.near';
const upgrades={
  pagination:{artifact:'oasis_storage.wasm',oldHash:'6bc5b84cc31190a9c1eb89fefabf45925477cab4dfe46857cb11e6e2a9d7cb62',newHash:'da0eafd260e8345715f4099600526b3d4c3c9d3fc42241e6ef3b400683c8abec'},
  'checked-record':{artifact:'oasis_storage.wasm',oldHash:'da0eafd260e8345715f4099600526b3d4c3c9d3fc42241e6ef3b400683c8abec',newHash:'638719d44b0305bded6096a68230d505e1fa6b811e404df8c12e0fd22d6fcefe'},
};
upgrades['token-escrow']={artifact:'oasis_token_escrow_candidate.wasm',oldHash:'638719d44b0305bded6096a68230d505e1fa6b811e404df8c12e0fd22d6fcefe',newHash:'3390da66ecaa7c09e6ebdd897a7139ae2be53644deb69ccbe953bf944c6c5b31'};
upgrades['nft-escrow']={artifact:'oasis_nft_escrow_candidate.wasm',oldHash:'3390da66ecaa7c09e6ebdd897a7139ae2be53644deb69ccbe953bf944c6c5b31',newHash:'86e566d1e51bdac93aa0f47a511bb581ab33f29f939cb4b6ac5a7fd0595945dd'};
const selected=process.argv[2]??'pagination',reconcile=process.argv[3]==='reconcile';
if(process.argv.length>4||process.argv[3]!==undefined&&!reconcile)throw new Error('Choose explicit reconcile or one initial submission.');
if(!Object.hasOwn(upgrades,selected))throw new Error('Only a pinned reviewed local upgrade is permitted');
const {oldHash,newHash,artifact}=upgrades[selected];
const pending=root+'/'+selected+'-upgrade.pending.json',complete=root+'/'+selected+'-upgrade.complete.json';
const sha=data=>createHash('sha256').update(data).digest('hex');
function persist(path,value,flag){const fd=openSync(path,flag,0o600);try{writeFileSync(fd,JSON.stringify(value));fsyncSync(fd);}finally{closeSync(fd);}const directory=openSync(root,'r');try{fsyncSync(directory);}finally{closeSync(directory);}}
async function query(params){const response=await fetch(rpc,{method:'POST',headers:{'content-type':'application/json'},body:JSON.stringify({jsonrpc:'2.0',id:'pagination-upgrade',method:'query',params})});if(!response.ok)throw new Error('Native query HTTP failure');const body=await response.json();if(body.error)throw new Error('Native query failed: '+JSON.stringify(body.error));return body.result;}
async function codeHash(){const result=await query({request_type:'view_code',account_id:accountId,finality:'final'});return sha(Buffer.from(result.code_base64,'base64'));}
async function stateDigest(){const result=await query({request_type:'view_state',account_id:accountId,prefix_base64:'',finality:'final'});if(!Array.isArray(result.values))throw new Error('Native state values missing');const rows=result.values.map(({key,value})=>[key,value]).sort((a,b)=>a[0].localeCompare(b[0]));return {count:rows.length,sha256:sha(JSON.stringify(rows))};}
if(existsSync(complete)||!reconcile&&existsSync(pending)||reconcile&&!existsSync(pending))throw new Error('Upgrade evidence requires explicit original reconciliation; never resubmit.');
const wasm=readFileSync(root+'/build/'+artifact);
if(sha(wasm)!==newHash)throw new Error('Reviewed new bytecode hash differs');
const provider=new JsonRpcProvider({url:rpc});
if((await provider.viewNodeStatus()).chain_id!=='oasis-near-local')throw new Error('Retained chain differs');
let evidence,outcome;
if(reconcile){
  evidence=JSON.parse(readFileSync(pending,'utf8'));
  if(evidence.chain!=='oasis-near-local'||evidence.accountId!==accountId||evidence.oldHash!==oldHash||evidence.newHash!==newHash||!evidence.original)
    throw new Error('Retained original upgrade evidence differs or predates durable preparation; preserve for review.');
  const original=await recoverNearDeployment(evidence.original,evidence.chain);
  const identity=await verifyNearDeployment(provider,original,evidence.chain,{kind:'deployContract',recipient:accountId,codeBytes:wasm.length,codeSha256:newHash});
  if(identity.transactionHash!==evidence.transactionHash)throw new Error('Retained upgrade identity differs.');
  // Read-only reconciliation: no signer, nonce creation or submission capability.
  outcome=await provider.txStatus(evidence.transactionHash,accountId,'FINAL');
}else{
  if(await codeHash()!==oldHash)throw new Error('Previous deployed bytecode differs; no signing');
  const before=await stateDigest();
  const signer=JSON.parse(readFileSync(root+'/ledger/validator_key.json','utf8'));
  if(signer.account_id!==accountId)throw new Error('Retained signer account differs');
  const account=new Account(accountId,provider,signer.secret_key);
  const original=await prepareNearDeployment(account,{chainId:'oasis-near-local',sender:accountId,recipient:accountId,kind:'deployContract',codeBytes:wasm.length,codeSha256:newHash},wasm);
  evidence={chain:'oasis-near-local',accountId,oldHash,newHash,before,transactionHash:original.transactionHash,original,state:'signed-original-persisted'};
  const submitted=await submitNearDeployment(provider,original,evidence.chain,async()=>persist(pending,evidence,'wx'),exactNearSubmission(rpc,original.signedTransactionBase64));
  outcome=submitted.outcome;
}
if(outcome.transaction_outcome?.id!==evidence.transactionHash||outcome.final_execution_status!=='FINAL'||!outcome.status||!Object.hasOwn(outcome.status,'SuccessValue'))throw new Error('Upgrade not proven finalized-successful');
if(await codeHash()!==newHash)throw new Error('Independent deployed code hash differs');
const after=await stateDigest();
if(after.count!==evidence.before.count||after.sha256!==evidence.before.sha256)throw new Error('Contract data changed during code-only upgrade');
const page=await provider.callFunction({contractId:accountId,method:'entries_page',args:{offset:0,limit:1,prefix:''}});
if(!Number.isSafeInteger(page.total)||page.rows.length>1)throw new Error('New page ABI not independently readable');
if(selected==='checked-record'&&await provider.callFunction({contractId:accountId,method:'record_write_protocol',args:{}})!==1)throw new Error('Checked record ABI capability differs');
if(selected==='token-escrow'&&await provider.callFunction({contractId:accountId,method:'token_escrow_protocol',args:{}})!==1)throw new Error('Native token custody ABI differs');
if(selected==='nft-escrow'&&await provider.callFunction({contractId:accountId,method:'nft_escrow_protocol',args:{}})!==1)throw new Error('Native NFT custody ABI differs');
// Preserve immutable signed-original file; publish complete proof exclusively.
Object.assign(evidence,{state:'verified',after,recordCount:page.total,outcome});persist(complete,evidence,'wx');
console.log(JSON.stringify({state:evidence.state,transactionHash:evidence.transactionHash,oldHash,newHash,before:evidence.before,after}));
