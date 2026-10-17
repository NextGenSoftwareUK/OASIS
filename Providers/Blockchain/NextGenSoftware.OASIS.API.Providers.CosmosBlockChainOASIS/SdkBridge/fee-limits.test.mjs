import test from 'node:test';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {DirectSecp256k1Wallet} from '@cosmjs/proto-signing';
import {SigningCosmWasmClient} from '@cosmjs/cosmwasm';
import {AuthInfo,SignDoc,TxRaw} from 'cosmjs-types/cosmos/tx/v1beta1/tx';
import {guardCosmosSigningClient,withCosmosNativeEvidence,CosmosNativeMutationReconciliationError,verifyCosmosPreparedIdentity,snapshotCosmosFeeLimits} from './transaction-results.mjs';
import {invoke} from './bridge.mjs';

async function signedFee(amount,tip=[]){
 const wallet=await DirectSecp256k1Wallet.fromKey(new Uint8Array(32).fill(7),'wasm');const [{address}]=await wallet.getAccounts();
 const client=await SigningCosmWasmClient.offline(wallet);
 const initial=await client.sign(address,[{typeUrl:'/cosmos.bank.v1beta1.MsgSend',value:{fromAddress:address,toAddress:address,amount:[{denom:'stake',amount:'1'}]}}],
  {amount:[],gas:'100000'},'',{accountNumber:7,sequence:3,chainId:'controlled-chain'});
 const auth=AuthInfo.decode(initial.authInfoBytes);auth.fee=amount===null?undefined:{...auth.fee,amount};
 if(tip.length)auth.tip={amount:tip,tipper:address};
 const doc=SignDoc.fromPartial({bodyBytes:initial.bodyBytes,authInfoBytes:AuthInfo.encode(auth).finish(),chainId:'controlled-chain',accountNumber:7n});
 const signed=await wallet.signDirect(address,doc);
 const bytes=TxRaw.encode({bodyBytes:signed.signed.bodyBytes,authInfoBytes:signed.signed.authInfoBytes,signatures:[Buffer.from(signed.signature.signature,'base64')]}).finish();
 return {bytes,evidence:{transactionHash:createHash('sha256').update(bytes).digest('hex').toUpperCase(),sender:address,chainId:'controlled-chain',signedTransactionBase64:Buffer.from(bytes).toString('base64'),signDocBase64:Buffer.from(SignDoc.encode(signed.signed).finish()).toString('base64')}};
}
function guarded(limits,prepared=true){
 const counts={persist:0,broadcast:0};const client={async broadcastTx(bytes){counts.broadcast++;return {transactionHash:createHash('sha256').update(bytes).digest('hex').toUpperCase(),height:42,code:0};},async signAndBroadcast(){throw new Error('Unused');}};
 guardCosmosSigningClient(client,prepared?async()=>{counts.persist++;}:undefined,limits);return {client,counts};
}

test('explicit Cosmos fee maps reject malformed config before operation or wallet creation',async()=>{
 for(const limits of [null,[],{}, {stake:'0'},{stake:'01'},{stake:'-1'},{stake:1},{stake:'1.0'},{'bad denom':'1'},Object.fromEntries(Array.from({length:33},(_,i)=>['coin'+i,'1']))]){
  assert.throws(()=>snapshotCosmosFeeLimits(limits));
  await assert.rejects(invoke({operation:'put',maximumFeeUnitsByDenomination:limits}),/Cosmos.*fee|Cosmos.*denomination/);
 }
});
test('signed Cosmos declarations must fit every approved denomination before preparation or broadcast',async()=>{
 for(const prepared of [false,true])for(const [coins,limits] of [
  [[{denom:'stake',amount:'6'}],{stake:'5'}],[[{denom:'other',amount:'1'}],{stake:'5'}],
  [[{denom:'stake',amount:'1'},{denom:'stake',amount:'1'}],{stake:'5'}],[[{denom:'stake',amount:'01'}],{stake:'5'}],
  [null,{stake:'5'}]]){
   const {bytes}=await signedFee(coins);const {client,counts}=guarded(limits,prepared);
   await assert.rejects(withCosmosNativeEvidence(()=>client.broadcastTx(bytes)),error=>!(error instanceof CosmosNativeMutationReconciliationError));
   assert.deepEqual(counts,{persist:0,broadcast:0});
 }
});
test('signed fee and tip are summed rather than independently allowed against the same ceiling',async()=>{
 const {bytes,evidence}=await signedFee([{denom:'stake',amount:'3'}],[{denom:'stake',amount:'3'}]);
 assert.equal(verifyCosmosPreparedIdentity(evidence).verified,true);
 const rejected=guarded({stake:'5'});await assert.rejects(rejected.client.broadcastTx(bytes),/fee and tip exceed/);
 assert.deepEqual(rejected.counts,{persist:0,broadcast:0});
 const accepted=guarded({stake:'6'});await accepted.client.broadcastTx(bytes);assert.deepEqual(accepted.counts,{persist:1,broadcast:1});
});
test('exact multi-denomination and large integer fee ceilings retain precision',async()=>{
 const large='9007199254740993';
 const {bytes,evidence}=await signedFee([{denom:'stake',amount:large},{denom:'uatom',amount:'7'}]);
 assert.equal(verifyCosmosPreparedIdentity(evidence).verified,true);
 const {client,counts}=guarded({stake:large,uatom:'7'});await client.broadcastTx(bytes);assert.deepEqual(counts,{persist:1,broadcast:1});
 const lower=guarded({stake:'9007199254740992',uatom:'7'});await assert.rejects(lower.client.broadcastTx(bytes),/exceed/);
});
test('Cosmos fee limits snapshot cannot be raised or lowered by later caller mutation',async()=>{
 const limits={stake:'5'};const {client,counts}=guarded(limits);limits.stake='999';
 await assert.rejects(client.broadcastTx((await signedFee([{denom:'stake',amount:'6'}])).bytes),/exceed/);
 limits.stake='1';await client.broadcastTx((await signedFee([{denom:'stake',amount:'5'}])).bytes);
 assert.deepEqual(counts,{persist:1,broadcast:1});
});
test('later Cosmos ceiling rejection retains earlier original effect and prevents ordinary retry',async()=>{
 const first=await signedFee([{denom:'stake',amount:'1'}]),second=await signedFee([{denom:'stake',amount:'6'}]);
 const {client,counts}=guarded({stake:'5'});
 await assert.rejects(withCosmosNativeEvidence(async()=>{await client.broadcastTx(first.bytes);await client.broadcastTx(second.bytes);}),
  error=>error instanceof CosmosNativeMutationReconciliationError&&error.nativeMutation.transactionHash===first.evidence.transactionHash&&error.nativeMutation.executionSucceeded===true);
 assert.deepEqual(counts,{persist:1,broadcast:1});
});
test('historical Cosmos identity verification is not new spending approval under a current lower cap',async()=>{
 const {evidence}=await signedFee([{denom:'stake',amount:'9'}]);
 const identity=await invoke({operation:'verifyNativePreparation',preparation:evidence,maximumFeeUnitsByDenomination:{stake:'1'}});
 assert.equal(identity.verified,true);assert.equal(identity.transactionHash,evidence.transactionHash);
});

test('verified fee evidence derives exact sorted fee plus tip totals only from original signed bytes',async()=>{
 const {evidence}=await signedFee([{denom:'uatom',amount:'9007199254740993'},{denom:'stake',amount:'3'}],
  [{denom:'uatom',amount:'7'},{denom:'stake',amount:'4'}]);
 const verified=verifyCosmosPreparedIdentity({...evidence,declaredFeeAndTip:[{denom:'stake',amount:'0'}]});
 assert.deepEqual(verified.declaredFeeAndTip,[{denom:'stake',amount:'7'},{denom:'uatom',amount:'9007199254741000'}]);
 assert.ok(Object.isFrozen(verified.declaredFeeAndTip));assert.ok(verified.declaredFeeAndTip.every(Object.isFrozen));
});

test('signed identity verification and admission share rejection of ambiguous or malformed fee coins',async()=>{
 for(const coins of [[{denom:'stake',amount:'01'}],[{denom:'stake',amount:'-1'}],
  [{denom:'bad denom',amount:'1'}],[{denom:'stake',amount:'1'},{denom:'stake',amount:'2'}]]){
  const {evidence,bytes}=await signedFee(coins);
  assert.throws(()=>verifyCosmosPreparedIdentity(evidence),/denomination\/amount/);
  const {client,counts}=guarded({stake:'100'});
  await assert.rejects(client.broadcastTx(bytes));assert.deepEqual(counts,{persist:0,broadcast:0});
 }
});

test('empty and explicitly zero signed fee declarations remain distinct without invented denominations',async()=>{
 assert.deepEqual(verifyCosmosPreparedIdentity((await signedFee([])).evidence).declaredFeeAndTip,[]);
 assert.deepEqual(verifyCosmosPreparedIdentity((await signedFee([{denom:'stake',amount:'0'}])).evidence).declaredFeeAndTip,[{denom:'stake',amount:'0'}]);
});

test('combined fee and tip denomination count is bounded rather than each array alone',async()=>{
 const fee=Array.from({length:17},(_,i)=>({denom:'fee'+i,amount:'1'}));
 const tip=Array.from({length:16},(_,i)=>({denom:'tip'+i,amount:'1'}));
 const {evidence,bytes}=await signedFee(fee,tip);
 assert.throws(()=>verifyCosmosPreparedIdentity(evidence),/combined Cosmos fee denominations/);
 const {client,counts}=guarded({stake:'1'});
 await assert.rejects(client.broadcastTx(bytes),/combined Cosmos fee denominations/);
 assert.deepEqual(counts,{persist:0,broadcast:0});
});
