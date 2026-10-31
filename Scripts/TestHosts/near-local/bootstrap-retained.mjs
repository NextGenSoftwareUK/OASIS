// Explicit local-only bootstrap. Never reset, replace, or automatically replay a deployment.
import { createRequire } from "node:module";
import { readFileSync, realpathSync, existsSync, openSync, writeFileSync, fsyncSync, closeSync } from "node:fs";
import { createHash } from "node:crypto";

const require=createRequire(import.meta.url);
const sdk=createRequire(require.resolve("near-workspaces"))("near-api-js");
const root="/var/tmp/oasis-near-evidence";
const rpc="http://127.0.0.1:18830";
const account="test.near";
const chain="oasis-near-local";
const sha=bytes=>createHash("sha256").update(bytes).digest("hex");
const genesisHash="44cd89e1518e944f19aec5e2508550f33170ee9196158ec73012fd8e203a6713";
const wasmHash="6bc5b84cc31190a9c1eb89fefabf45925477cab4dfe46857cb11e6e2a9d7cb62";
const mode=process.argv[2];
if(!["--inspect","--deploy-once","--verify-original"].includes(mode) || process.argv.length!==3)
  throw new Error("Select --inspect, --verify-original or explicit --deploy-once; no automatic deployment.");
if(realpathSync(root)!==root)throw new Error("Unexpected retained environment path.");
const genesis=readFileSync(`${root}/ledger/genesis.json`);
const wasm=readFileSync(`${root}/build/oasis_storage.wasm`);
if(sha(genesis)!==genesisHash || sha(wasm)!==wasmHash)throw new Error("Reviewed genesis/bytecode differs.");
async function query(method,params){
  const response=await fetch(rpc,{method:"POST",headers:{"content-type":"application/json"},
    body:JSON.stringify({jsonrpc:"2.0",id:"retained-bootstrap",method,params}),signal:AbortSignal.timeout(30000)});
  if(!response.ok)throw new Error(`NEAR HTTP ${response.status}; no replay.`);
  const result=await response.json();
  if(result.error)throw new Error(`NEAR ${method} rejected: ${JSON.stringify(result.error)}`);
  return result.result;
}
function persistOnce(path,value){
  const fd=openSync(path,"wx",0o600);
  try{writeFileSync(fd,JSON.stringify(value,null,2));fsyncSync(fd);}finally{closeSync(fd);}
}
const status=await query("status",[]);
if(status.chain_id!==chain || status.sync_info.syncing)throw new Error("Wrong or syncing retained chain.");
const state=await query("query",{request_type:"view_account",account_id:account,finality:"final"});
const pending=`${root}/bootstrap-intent.json`;
console.log(JSON.stringify({chain,height:status.sync_info.latest_block_height,account,
  codeHash:state.code_hash,genesisSha256:genesisHash,wasmSha256:wasmHash,priorIntent:existsSync(pending)}));
if(mode==="--inspect")process.exit(0);
async function verifyOriginal(intent){
  if(intent.chain!==chain || intent.account!==account || intent.genesisSha256!==genesisHash || intent.wasmSha256!==wasmHash)
    throw new Error("Original intent identity differs; refusing reconciliation.");
  const final=await query("tx",{tx_hash:intent.transactionHash,sender_account_id:account,wait_until:"FINAL"});
  if(final.final_execution_status!=="FINAL" || final.transaction.hash!==intent.transactionHash ||
      !Object.hasOwn(final.status,"SuccessValue"))throw new Error("Original deployment is not proven FINAL success.");
  const code=await query("query",{request_type:"view_code",account_id:account,finality:"final"});
  if(sha(Buffer.from(code.code_base64,"base64"))!==wasmHash)throw new Error("Finalized deployed bytecode mismatch.");
  const entries=await query("query",{request_type:"call_function",account_id:account,
    method_name:"entries",args_base64:Buffer.from("{}").toString("base64"),finality:"final"});
  if(Buffer.from(entries.result).toString()!=="[]")throw new Error("Initialized contract not empty.");
  const evidence={transactionHash:intent.transactionHash,finalExecutionStatus:"FINAL",finalizedWasmSha256:wasmHash,entries:0};
  persistOnce(`${root}/bootstrap-finalized.json`,evidence);
  console.log(JSON.stringify(evidence));
}
if(mode==="--verify-original"){
  await verifyOriginal(JSON.parse(readFileSync(pending,"utf8")));
  process.exit(0);
}
if(existsSync(pending))throw new Error("Prior bootstrap intent exists: reconcile original hash read-only; NEVER replay.");
if(state.code_hash!=="11111111111111111111111111111111")throw new Error("Account already has code; refusing replacement.");
const stored=await query("query",{request_type:"view_state",account_id:account,prefix_base64:"",finality:"final"});
if(stored.values.length!==0)throw new Error("Account has state; refusing initialization.");
let key;
try{
  const signer=JSON.parse(readFileSync(`${root}/ledger/validator_key.json`,"utf8"));
  if(signer.account_id!==account)throw new Error("account mismatch");
  key=sdk.KeyPair.fromString(signer.secret_key);
}catch{throw new Error("Invalid retained signer; no replacement generated.");}
const access=await query("query",{request_type:"view_access_key",account_id:account,
  public_key:key.getPublicKey().toString(),finality:"final"});
if(access.permission!=="FullAccess")throw new Error("Bootstrap requires retained full-access test key.");
const signer=await sdk.InMemorySigner.fromKeyPair(chain,account,key);
const nonce=BigInt(access.nonce)+1n;
const actions=[sdk.transactions.deployContract(wasm),sdk.transactions.functionCall("init",
  {owner_id:account},100000000000000n,0n)];
const [hash,signed]=await sdk.transactions.signTransaction(account,nonce,actions,
  sdk.utils.serialize.base_decode(access.block_hash),signer,account,chain);
const transactionHash=sdk.utils.serialize.base_encode(hash);
// Durable original hash BEFORE the sole broadcast. Signed bytes/private keys are never logged.
persistOnce(pending,{chain,account,transactionHash,nonce:nonce.toString(),genesisSha256:genesisHash,
  wasmSha256:wasmHash,createdUtc:new Date().toISOString(),actions:["DeployContract","init"]});
console.log(JSON.stringify({transactionHash,stage:"intent-durable"}));
const outcome=await query("broadcast_tx_commit",[Buffer.from(signed.encode()).toString("base64")]);
persistOnce(`${root}/bootstrap-outcome.json`,outcome);
if(outcome.transaction.hash!==transactionHash || !Object.hasOwn(outcome.status,"SuccessValue"))
  throw new Error("Deployment did not prove success; original intent/outcome retained.");
await verifyOriginal(JSON.parse(readFileSync(pending,"utf8")));
