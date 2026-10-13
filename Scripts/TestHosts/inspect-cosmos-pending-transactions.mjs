import fs from 'node:fs';
import path from 'node:path';
import {createRequire} from 'node:module';
import {fileURLToPath} from 'node:url';
import {createHash,createHmac} from 'node:crypto';
import {indexedHistorySnapshot} from '../../Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS/SdkBridge/transaction-results.mjs';

const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'../..');
const require=createRequire(path.join(root,'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS/SdkBridge/package.json'));
const {CosmWasmClient,wasmTypes}=require('@cosmjs/cosmwasm');
const {Registry,decodeTxRaw}=require('@cosmjs/proto-signing');
const fixture=path.join(root,'TestResults/HyperDriveCosmos');
const intent=JSON.parse(fs.readFileSync(path.join(fixture,'replication-pending.json'),'utf8'));
const rpc=process.env.OASIS_COSMOS_TEST_RPC;
if(new URL(rpc).hostname!=='127.0.0.1'||intent.chain!=='oasis-cosmos-local'
  ||intent.contract!==process.env.OASIS_COSMOS_TEST_CONTRACT
  ||intent.encryptionKeyLabel!=='oasis-cosmos-local-storage-fixture-v1')
  throw new Error('Only the exact retained local pending Cosmos fixture may be inspected.');
const uuid=/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/;
if(!uuid.test(intent.avatarId)||!uuid.test(intent.holonId))throw new Error('Invalid owned record identities.');
const key=createHash('sha256').update(intent.encryptionKeyLabel).digest();
const indexKey=createHmac('sha256',key).update('OASIS private storage index v1').digest();
const logicalKeys=[`oasis/avatar/${intent.avatarId.replaceAll('-','')}`,
  `oasis/avatardetail/${intent.avatarId.replaceAll('-','')}`,`oasis/holon/${intent.holonId.replaceAll('-','')}`];
const physicalKeys=new Set(logicalKeys.map(value=>'oasis/private/v1/'+createHmac('sha256',indexKey).update(value).digest('hex')));
key.fill(0);indexKey.fill(0);
const client=await CosmWasmClient.connect(rpc);
try {
  if(await client.getChainId()!==intent.chain)throw new Error('Retained chain identity mismatch.');
  const {owner}=await client.queryContractSmart(intent.contract,{config:{}});
  const transactions=await indexedHistorySnapshot(client,owner);
  const registry=new Registry(wasmTypes);
  const matches=[];
  for(const tx of transactions){
    const raw=decodeTxRaw(tx.tx);
    for(let messageIndex=0;messageIndex<raw.body.messages.length;messageIndex++){
      const encoded=raw.body.messages[messageIndex];
      if(encoded.typeUrl!=='/cosmwasm.wasm.v1.MsgExecuteContract')continue;
      const message=registry.decode(encoded);
      if(message.sender!==owner||message.contract!==intent.contract)continue;
      const payload=JSON.parse(new TextDecoder('utf-8',{fatal:true}).decode(message.msg));
      for(const action of ['put','delete'])if(physicalKeys.has(payload[action]?.key))
        matches.push({hash:tx.hash,height:tx.height,messageIndex,action,code:tx.code,
          gasUsed:tx.gasUsed.toString(),gasWanted:tx.gasWanted.toString()});
    }
  }
  const report={chain:intent.chain,contract:intent.contract,indexedTransactionCount:transactions.length,matches,
    reconciliationComplete:false,explanation:'Positive exact-key transaction evidence only; absent matches do not prove non-submission. Pending intent remains untouched.'};
  fs.writeFileSync(path.join(fixture,'cosmos-pending-transactions-current.json'),JSON.stringify(report,null,2));
  console.log(JSON.stringify(report));
} finally {client.disconnect();}
