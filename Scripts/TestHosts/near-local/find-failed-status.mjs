// Read-only discovery/reconciliation of an ORIGINAL failed transaction. Never signs or submits.
import { existsSync, readFileSync, writeFileSync } from "node:fs";
const root="/var/tmp/oasis-near-evidence";
const path=`${root}/failed-status-original.json`;
async function rpc(method,params){
  const response=await fetch("http://127.0.0.1:18830",{method:"POST",headers:{"content-type":"application/json"},
    body:JSON.stringify({jsonrpc:"2.0",id:"original-failed-status",method,params}),signal:AbortSignal.timeout(30000)});
  if(!response.ok)throw new Error(`NEAR HTTP ${response.status}`);
  const value=await response.json();
  if(value.error)throw new Error(`NEAR ${method} rejected: ${JSON.stringify(value.error)}`);
  return value.result;
}
const node=await rpc("status",[]);
if(node.chain_id!=="oasis-near-local" || node.sync_info.syncing)throw new Error("Wrong/syncing retained chain.");
async function verify(hash){
  const outcome=await rpc("tx",{tx_hash:hash,sender_account_id:"test.near",wait_until:"FINAL"});
  if(outcome.final_execution_status!=="FINAL" || outcome.transaction_outcome.id!==hash
      || outcome.transaction.signer_id!=="test.near" || outcome.transaction.receiver_id!=="test.near"
      || !outcome.status?.Failure)throw new Error("Original transaction is not proven failed/final and account-bound.");
  return outcome;
}
if(existsSync(path)){
  const original=JSON.parse(readFileSync(path,"utf8"));
  if(original.chain!==node.chain_id)throw new Error("Original chain differs; preserve evidence.");
  await verify(original.hash);
  console.log(JSON.stringify({hash:original.hash,reconciled:true,readOnly:true}));
}else{
  const visited=new Set();let found=false;
  const top=node.sync_info.latest_block_height-2;
  const floor=Math.max(node.sync_info.earliest_block_height,top-511);
  for(let height=top;height>=floor && !found;height--){
    const block=await rpc("block",{block_id:height});
    for(const header of block.chunks){
      if(visited.has(header.chunk_hash))continue;
      visited.add(header.chunk_hash);
      const chunk=await rpc("chunk",{chunk_id:header.chunk_hash});
      for(const transaction of chunk.transactions){
        if(transaction.signer_id!=="test.near" || transaction.receiver_id!=="test.near")continue;
        const outcome=await rpc("tx",{tx_hash:transaction.hash,sender_account_id:"test.near",wait_until:"FINAL"});
        if(!outcome.status?.Failure)continue;
        const verified=await verify(transaction.hash);
        writeFileSync(path,JSON.stringify({chain:node.chain_id,hash:transaction.hash,blockHeight:height,outcome:verified},null,2),{flag:"wx",mode:0o600});
        console.log(JSON.stringify({hash:transaction.hash,blockHeight:height,readOnly:true}));
        found=true;break;
      }
      if(found)break;
    }
  }
  if(!found)throw new Error("No failed original in bounded retained block window; no mutation attempted.");
}
