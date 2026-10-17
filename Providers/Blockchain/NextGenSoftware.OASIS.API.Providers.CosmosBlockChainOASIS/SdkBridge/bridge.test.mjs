import { test } from "node:test";
import assert from "node:assert/strict";
import { spawnSync } from "node:child_process";
import { fileURLToPath } from "node:url";
import { fromBech32, toBech32 } from "@cosmjs/encoding";
import { DirectSecp256k1Wallet, decodeTxRaw } from "@cosmjs/proto-signing";
import { verifyCosmosPreparedIdentity } from './transaction-results.mjs';
import { invoke as invokeTrusted } from './bridge.mjs';
import { CosmWasmClient, SigningCosmWasmClient } from '@cosmjs/cosmwasm';
import { createHash } from 'node:crypto';

for(const mode of ['valid','admin','address','checksum','code-id','protocol'])test('escrow protocol pins immutable exact contract/code through SDK query boundary: '+mode,async()=>{
 const connect=CosmWasmClient.connect;let closed=0,queries=0;
 CosmWasmClient.connect=async()=>({
  getChainId:async()=> 'fixture-chain',
  getContract:async address=>({address:mode==='address'?'foreign':address,codeId:7,admin:mode==='admin'?'upgrade-owner':undefined}),
  getCodeDetails:async id=>({id:mode==='code-id'?8:id,checksum:(mode==='checksum'?'d':'c').repeat(64)}),
  queryContractSmart:async(address,message)=>{
   queries++;assert.equal(address,'fixture-contract');assert.deepEqual(message,{asset:{message:{token_escrow_protocol:{}}}});
   return mode==='protocol'?2:1;
  },
  disconnect:()=>{closed++;},
 });
 try{
  const call=invokeTrusted({operation:'tokenEscrowInfo',rpcEndpoint:'http://controlled',chainId:'fixture-chain',
   targetContractAddress:'fixture-contract',codeSha256:'c'.repeat(64)});
  if(mode==='valid')assert.deepEqual(await call,{contractAddress:'fixture-contract',codeSha256:'c'.repeat(64),protocol:1});
  else await assert.rejects(call,/Escrow|escrow/);
  assert.equal(queries,['valid','protocol'].includes(mode)?1:0);assert.equal(closed,1);
 }finally{CosmWasmClient.connect=connect;}
});

for(const action of ['token_transfer','token_mint'])for(const memo of ['Original seed 🌱 memo',''])test('contract execute preserves exact memo in real CosmJS signed bytes: '+action+'/'+JSON.stringify(memo),async()=>{
 const original=SigningCosmWasmClient.connectWithSigner;let prepared=0,broadcasts=0,closed=0;
 const asset={symbol:'fixture',recipient:toBech32('wasm',new Uint8Array(20).fill(3)),amount:'123000000'};
 if(action==='token_mint')asset.metadata_json='{}';
 const message={asset:{message:{[action]:asset}}};
 SigningCosmWasmClient.connectWithSigner=async(_endpoint,wallet,options)=>{
  const client=await SigningCosmWasmClient.offline(wallet,options);
  client.getChainId=async()=> 'fixture-chain';client.getSequence=async()=>({accountNumber:1,sequence:2});client.simulate=async()=>100000;
  client.broadcastTx=async bytes=>{
   assert.equal(prepared,1);broadcasts++;
   const raw=decodeTxRaw(bytes);assert.equal(raw.body.memo,memo);
   assert.equal(raw.body.messages.length,1);assert.equal(raw.body.messages[0].typeUrl,'/cosmwasm.wasm.v1.MsgExecuteContract');
   return {transactionHash:createHash('sha256').update(bytes).digest('hex').toUpperCase(),height:42,code:0,
    gasUsed:100000n,gasWanted:140000n,events:[],msgResponses:[],rawLog:'[]'};
  };
  client.disconnect=()=>{closed++;};return client;
 };
 try{
  const result=await invokeTrusted({operation:'execute',rpcEndpoint:'http://127.0.0.1:1',chainId:'fixture-chain',privateKey:'07'.repeat(32),
   addressPrefix:'wasm',gasPrice:'0.025stake',targetContractAddress:toBech32('wasm',new Uint8Array(32).fill(2)),message,memo},async evidence=>{
    const verified=await verifyCosmosPreparedIdentity(evidence);assert.equal(verified.verified,true);prepared++;
   });
  assert.equal(result.height,42);assert.equal(prepared,1);assert.equal(broadcasts,1);assert.equal(closed,1);
 }finally{SigningCosmWasmClient.connectWithSigner=original;}
});

test('instantiate rejects inexact code IDs before opening any network client',async()=>{
 const original=SigningCosmWasmClient.connectWithSigner;let opened=0;
 SigningCosmWasmClient.connectWithSigner=async()=>{opened++;throw new Error('Must not connect');};
 try{
  for(const codeId of [0,-1,1.5,Number.MAX_SAFE_INTEGER+1,'2',null])
   await assert.rejects(invokeTrusted({operation:'instantiate',codeId}),/SDK-safe exact/);
  assert.equal(opened,0);
 }finally{SigningCosmWasmClient.connectWithSigner=original;}
});

test('actual status entry rejects substituted indexed bytes and closes read-only client', async () => {
 const original=CosmWasmClient.connect;
 const bytes=new Uint8Array([1,2,3]);
 const digest=createHash('sha256').update(bytes).digest('hex').toUpperCase();
 let result={tx:bytes,hash:digest,height:42,code:0},closed=0,prepared=0,lookups=0;
 CosmWasmClient.connect=async()=>({getChainId:async()=> 'fixture-chain',
  getTx:async hash=>{lookups++;assert.equal(hash,digest);return result;},disconnect:()=>{closed++;}});
 try{
  const input={operation:'transactionStatus',rpcEndpoint:'http://127.0.0.1:1',chainId:'fixture-chain',transactionHash:digest};
  const persist=async()=>{prepared++;};
  assert.deepEqual(await invokeTrusted(input,persist),{hash:digest,height:42,code:0});
  result={...result,tx:new Uint8Array([9])};
  await assert.rejects(invokeTrusted(input,persist),/exact committed/);
  result=null;assert.equal(await invokeTrusted(input,persist),null);
  await assert.rejects(invokeTrusted({...input,transactionHash:'invalid'},persist),/Exact Cosmos/);
  assert.equal(lookups,3);assert.equal(closed,4);assert.equal(prepared,0);
 }finally{CosmWasmClient.connect=original;}
});

test('actual instantiation entry applies default wallet prefix to returned address validation',async()=>{
 const original=SigningCosmWasmClient.connectWithSigner;
 let returnedPrefix='wasm',closed=0;
 SigningCosmWasmClient.connectWithSigner=async()=>({
  getChainId:async()=> 'fixture-chain',broadcastTx:async()=>{},signAndBroadcast:async()=>{},
  instantiate:async()=>({transactionHash:'a'.repeat(64),height:42,
   contractAddress:toBech32(returnedPrefix,new Uint8Array(32).fill(1))}),disconnect:()=>{closed++;}
 });
 try{
  const input={operation:'instantiate',rpcEndpoint:'http://127.0.0.1:1',chainId:'fixture-chain',
   privateKey:'07'.repeat(32),codeId:2,message:{},label:'controlled entry'};
  await assert.rejects(invokeTrusted(input),error=>error.nativeMutation?.transactionHash==='a'.repeat(64)&&error.nativeMutation.executionSucceeded===true);
  returnedPrefix='cosmos';const valid=await invokeTrusted(input);
  assert.equal(fromBech32(valid.contractAddress,90).prefix,'cosmos');
  returnedPrefix='wasm';const explicit=await invokeTrusted({...input,addressPrefix:'wasm'});
  assert.equal(fromBech32(explicit.contractAddress,90).prefix,'wasm');assert.equal(closed,3);
 }finally{SigningCosmWasmClient.connectWithSigner=original;}
});

test('actual probe derives SDK signer but uses only readonly client and never prepares submission',async()=>{
 const wallet=await DirectSecp256k1Wallet.fromKey(new Uint8Array(32).fill(7),'wasm');
 const [{address}]=await wallet.getAccounts();
 const originalRead=CosmWasmClient.connect,originalSigned=SigningCosmWasmClient.connectWithSigner;
 let signed=0,prepared=0,closed=0;
 CosmWasmClient.connect=async()=>({getChainId:async()=> 'fixture-chain',getContract:async()=>({address:'fixture-contract'}),queryContractSmart:async()=>({owner:address}),getHeight:async()=>42,disconnect:()=>{closed++;}});
 SigningCosmWasmClient.connectWithSigner=async()=>{signed++;throw new Error('Probe must not open signing client');};
 try{
  const input={operation:'probe',rpcEndpoint:'http://127.0.0.1:1',chainId:'fixture-chain',contractAddress:'fixture-contract',addressPrefix:'wasm',privateKey:'07'.repeat(32)};
  const result=await invokeTrusted(input,async()=>{prepared++;});assert.equal(result.config.owner,address);
  await assert.rejects(invokeTrusted({...input,privateKey:'08'.repeat(32)}),/owner does not match/);
  assert.equal(signed,0);assert.equal(prepared,0);assert.equal(closed,2);
 }finally{CosmWasmClient.connect=originalRead;SigningCosmWasmClient.connectWithSigner=originalSigned;}
});

test('trusted entry point validates callback and does not prepare key recovery as a submission', async () => {
  await assert.rejects(invokeTrusted({operation:'generateKey'},true),/awaited callback/);
  let prepared=0;
  const wallet=await invokeTrusted({operation:'restoreKey',privateKey:'07'.repeat(32),addressPrefix:'wasm'},async()=>{prepared++;});
  assert.equal(fromBech32(wallet.address,90).prefix,'wasm');assert.equal(prepared,0);
});

test("raw signing keys recover exact official SDK identity with the requested prefix", async () => {
  const wallet = await DirectSecp256k1Wallet.fromKey(new Uint8Array(32).fill(7), "wasm");
  const [account] = await wallet.getAccounts();
  for (const privateKey of ["07".repeat(32), "0x" + "07".repeat(32)]) {
    const restored = invoke({ operation: "restoreKey", privateKey, addressPrefix: "wasm" });
    assert.equal(restored.ok, true);
    assert.equal(restored.result.address, account.address);
    assert.equal(restored.result.publicKey, Buffer.from(account.pubkey).toString("hex"));
    assert.equal(fromBech32(restored.result.address, 90).prefix, "wasm");
  }
});

const bridge = fileURLToPath(new URL("bridge.mjs", import.meta.url));
function invoke(request) {
  const process = spawnSync(globalThis.process.execPath, [bridge], {
    // Match CosmosSdkBackend's two-minute process deadline, including SDK startup.
    input: JSON.stringify(request), encoding: "utf8", timeout: 120000,
  });
  assert.ifError(process.error);
  return { status: process.status, ...JSON.parse(process.stdout) };
}

test("official SDK creates a valid recoverable Cosmos wallet", () => {
  const generated = invoke({ operation: "generateKey", addressPrefix: "wasm" });
  assert.equal(generated.status, 0);
  assert.equal(generated.ok, true);
  assert.equal(generated.result.mnemonic.split(" ").length, 24);
  // Cosmos account addresses fit the Bech32 standard's 90-character limit.
  assert.equal(fromBech32(generated.result.address, 90).prefix, "wasm");
  assert.equal(fromBech32(generated.result.address, 90).data.length, 20);
  assert.equal(generated.result.publicKey.length, 66);
  const restored = invoke({ operation: "restoreKey", privateKey: generated.result.mnemonic, addressPrefix: "wasm" });
  assert.equal(restored.result.address, generated.result.address);
  assert.equal(restored.result.publicKey, generated.result.publicKey);
});

test("invalid signing key fails instead of inventing a wallet", () => {
  const response = invoke({ operation: "restoreKey", privateKey: "not-a-key" });
  assert.equal(response.status, 1);
  assert.equal(response.ok, false);
  assert.equal(typeof response.error, "string");
});

test("unreachable RPC propagates failure, not synthetic activation", () => {
  const response = invoke({ operation: "probe", rpcEndpoint: "http://127.0.0.1:1", chainId: "oasis-test", contractAddress: "invalid" });
  assert.equal(response.status, 1);
  assert.equal(response.ok, false);
});
test('storage activation authority is explicit and fail closed without paid mutation', async()=>{
 const {verifyStorageAuthority}=await import('./bridge.mjs');
 assert.doesNotThrow(()=>verifyStorageAuthority({owner:'wasm1owner'},'wasm1owner'));
 for(const [config,sender] of [[null,'wasm1owner'],[{},'wasm1owner'],[{owner:'other'},'wasm1owner'],[{owner:''},''],[{owner:'wasm1owner'},null]])
  assert.throws(()=>verifyStorageAuthority(config,sender),/owner does not match/);
});
