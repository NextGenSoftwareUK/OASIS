import fs from 'node:fs';
import path from 'node:path';
import {createRequire} from 'node:module';
import {fileURLToPath} from 'node:url';
import {createHash} from 'node:crypto';
import {indexedHistorySnapshot,cosmosNativeFeeReceipt} from '../../Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS/SdkBridge/transaction-results.mjs';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'../..');
const fixture=path.join(root,'TestResults/HyperDriveCosmos');
const bytes=fs.readFileSync(path.join(fixture,'prepared-native-pending.json'));
const journalHash=createHash('sha256').update(bytes).digest('hex').toUpperCase();
const assetProbe=journalHash==='BBACD101A5342D65430138C499FFE4839E3508D4176354222C8A9F398B42BB52';
if(!assetProbe&&journalHash!=='EF0CDE533070555A505BE561FDF287DEB5D10CE73BFD055952B27278DDAAEF5C')throw new Error('Independent original changed; new review required.');
const intent=JSON.parse(bytes);
if(assetProbe&&(intent.assetSymbol!=='PREP732dd8f8434048ddb739b963d3a74069'||intent.operationId!=='d96f0879-13dd-60c4-c8e8-baa23158e9cd'))throw new Error('Reviewed asset identity differs.');
const require=createRequire(path.join(root,'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS/SdkBridge/package.json'));
const {CosmWasmClient,wasmTypes}=require('@cosmjs/cosmwasm');
const {Registry,decodeTxRaw}=require('@cosmjs/proto-signing');
if(process.env.OASIS_COSMOS_TEST_RPC!=='http://127.0.0.1:26657'||intent.chain!=='oasis-cosmos-local'
 ||intent.contract!==process.env.OASIS_COSMOS_TEST_CONTRACT||!/^oasis\/independent-prepared-budget\/[0-9a-f]{32}$/.test(intent.key))throw new Error('Exact local isolated fixture required.');
const client=await CosmWasmClient.connect(process.env.OASIS_COSMOS_TEST_RPC);
try{
 if(await client.getChainId()!==intent.chain)throw new Error('Chain mismatch');
 const {owner}=await client.queryContractSmart(intent.contract,{config:{}});if(owner!==intent.sender)throw new Error('Original sender mismatch');
 const registry=new Registry(wasmTypes),matches=[];
 for(const tx of await indexedHistorySnapshot(client,owner))for(const encoded of decodeTxRaw(tx.tx).body.messages){
  if(encoded.typeUrl!=='/cosmwasm.wasm.v1.MsgExecuteContract')continue;
  const message=registry.decode(encoded);if(message.sender!==owner||message.contract!==intent.contract)continue;
  const payload=JSON.parse(new TextDecoder('utf-8',{fatal:true}).decode(message.msg));
  if(payload.put?.key===intent.key||payload.delete?.key===intent.key||assetProbe&&
    (payload.asset?.message?.token_mint?.symbol===intent.assetSymbol||payload.asset?.message?.token_burn?.symbol===intent.assetSymbol))
   matches.push({hash:tx.hash,height:tx.height,code:tx.code,payload,originalTransactionBase64:Buffer.from(tx.tx).toString('base64'),fee:cosmosNativeFeeReceipt(tx,tx.hash)});
 }
 const value=await client.queryContractSmart(intent.contract,{get:{key:intent.key}});
 const assetState=assetProbe?await client.queryContractSmart(intent.contract,{asset:{message:{token:{symbol:intent.assetSymbol}}}}):undefined;
 if(assetProbe){
  if(matches.length!==4||new Set(matches.map(tx=>tx.hash)).size!==4||matches.some(tx=>tx.code!==0))throw new Error('Exactly four distinct successful originals required; no replay permitted.');
  for(const kind of ['token_mint','token_burn'])if(matches.filter(tx=>tx.payload.asset?.message?.[kind]?.symbol===intent.assetSymbol).length!==1)throw new Error('Exact mint and burn originals required.');
  if(assetState.supply!=='0')throw new Error('Reviewed asset supply is not zero; inspect without mutation.');
 }
 const report={originalJournalSha256:journalHash,key:intent.key,assetSymbol:intent.assetSymbol,matches,value,assetState,readOnly:true,replacementAuthorized:false};
 fs.writeFileSync(path.join(fixture,assetProbe?'cosmos-independent-asset-originals-current.json':'cosmos-independent-original-current.json'),JSON.stringify(report,null,2));
 console.log(JSON.stringify({key:intent.key,assetSymbol:intent.assetSymbol,matches:matches.map(({hash,height,code,payload})=>({hash,height,code,payload})),value,assetState,readOnly:true,replacementAuthorized:false}));
}finally{client.disconnect();}
