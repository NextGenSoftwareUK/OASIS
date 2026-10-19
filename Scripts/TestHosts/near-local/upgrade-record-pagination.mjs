// One reviewed retained-local code upgrade. Never initializes, resets or retries a submission.
import {readFileSync,openSync,writeFileSync,fsyncSync,closeSync,existsSync,renameSync} from 'node:fs';
import {createHash} from 'node:crypto';
import {Account,JsonRpcProvider,actions} from '../../../Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.NEAROASIS/SdkBridge/node_modules/near-api-js/lib/index.js';
const root='/var/tmp/oasis-near-evidence',rpc='http://127.0.0.1:3030',accountId='test.near';
const pending=root+'/pagination-upgrade.pending.json',complete=root+'/pagination-upgrade.complete.json';
const oldHash='6bc5b84cc31190a9c1eb89fefabf45925477cab4dfe46857cb11e6e2a9d7cb62';
const newHash='da0eafd260e8345715f4099600526b3d4c3c9d3fc42241e6ef3b400683c8abec';
const sha=data=>createHash('sha256').update(data).digest('hex');
function persist(path,value,flag){const fd=openSync(path,flag,0o600);try{writeFileSync(fd,JSON.stringify(value));fsyncSync(fd);}finally{closeSync(fd);}}
async function query(params){const response=await fetch(rpc,{method:'POST',headers:{'content-type':'application/json'},body:JSON.stringify({jsonrpc:'2.0',id:'pagination-upgrade',method:'query',params})});if(!response.ok)throw new Error('Native query HTTP failure');const body=await response.json();if(body.error)throw new Error('Native query failed: '+JSON.stringify(body.error));return body.result;}
async function codeHash(){const result=await query({request_type:'view_code',account_id:accountId,finality:'final'});return sha(Buffer.from(result.code_base64,'base64'));}
async function stateDigest(){const result=await query({request_type:'view_state',account_id:accountId,prefix_base64:'',finality:'final'});if(!Array.isArray(result.values))throw new Error('Native state values missing');const rows=result.values.map(({key,value})=>[key,value]).sort((a,b)=>a[0].localeCompare(b[0]));return {count:rows.length,sha256:sha(JSON.stringify(rows))};}
if(existsSync(pending)||existsSync(complete))throw new Error('Original upgrade evidence exists; reconcile it, never resubmit.');
const wasm=readFileSync(root+'/build/oasis_storage.wasm');
if(sha(wasm)!==newHash)throw new Error('Reviewed new bytecode hash differs');
const provider=new JsonRpcProvider({url:rpc});
if((await provider.viewNodeStatus()).chain_id!=='oasis-near-local')throw new Error('Retained chain differs');
if(await codeHash()!==oldHash)throw new Error('Previous deployed bytecode differs; no signing');
const before=await stateDigest();
const signer=JSON.parse(readFileSync(root+'/ledger/validator_key.json','utf8'));
if(signer.account_id!==accountId)throw new Error('Retained signer account differs');
const evidence={chain:'oasis-near-local',accountId,oldHash,newHash,before,state:'before-code-only-submission'};
persist(pending,evidence,'wx');
const account=new Account(accountId,provider,signer.secret_key);
const outcome=await account.signAndSendTransaction({receiverId:accountId,actions:[actions.deployContract(wasm)],waitUntil:'FINAL'});
evidence.transactionHash=outcome.transaction_outcome?.id;
evidence.state='acknowledged';persist(pending,evidence,'w');
if(!evidence.transactionHash||outcome.final_execution_status!=='FINAL'||!outcome.status||!Object.hasOwn(outcome.status,'SuccessValue'))throw new Error('Upgrade not proven finalized-successful');
if(await codeHash()!==newHash)throw new Error('Independent deployed code hash differs');
const after=await stateDigest();
if(after.count!==before.count||after.sha256!==before.sha256)throw new Error('Contract data changed during code-only upgrade');
const page=await provider.callFunction({contractId:accountId,method:'entries_page',args:{offset:0,limit:1,prefix:''}});
if(!Number.isSafeInteger(page.total)||page.rows.length>1)throw new Error('New page ABI not independently readable');
Object.assign(evidence,{state:'verified',after,recordCount:page.total});persist(pending,evidence,'w');renameSync(pending,complete);
console.log(JSON.stringify(evidence));
