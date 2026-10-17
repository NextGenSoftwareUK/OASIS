import {test} from 'node:test';
import assert from 'node:assert/strict';
import {bcs} from '@mysten/sui/bcs';
import {ObjectError} from '@mysten/sui/client';
import {Ed25519Keypair} from '@mysten/sui/keypairs/ed25519';
import {normalizeSuiAddress,toBase58} from '@mysten/sui/utils';
import {Transaction,Inputs} from '@mysten/sui/transactions';
import {verifyTransactionSignature} from '@mysten/sui/verify';
import {execute,withNativeMutationEvidence,sdkErrorEnvelope} from './bridge.mjs';
import {Escrow,Deposit,readTokenDeposit,executeTokenEscrow} from './token-escrow.mjs';

const address=n=>normalizeSuiAddress('0x'+n);
const key=Ed25519Keypair.fromSecretKey(new Uint8Array(32).fill(7));
const base={packageAddress:address('1'),escrowObjectId:address('2'),depositId:'a'.repeat(64),
 privateKey:key.getSecretKey(),fromWalletAddress:key.toSuiAddress(),amountUnits:'123'};
function fixture(mode='valid',initial=null){
 let record=initial;let submits=0;
 const client={
  getObject:async()=>({object:{objectId:base.escrowObjectId,type:`${base.packageAddress}::token_escrow::Escrow`,
   owner:{$kind:mode==='private'?'AddressOwner':'Shared'},content:Escrow.serialize({id:base.escrowObjectId,
    protocol:mode==='protocol'?'2':'1',deposits:{id:address('3'),size:'0'}}).toBytes()}}),
  core:{getDynamicField:async args=>{
   assert.equal(args.parentId,address('3'));assert.equal(args.name.type,'vector<u8>');
   assert.deepEqual(bcs.vector(bcs.u8()).parse(args.name.bcs),[...Buffer.from(base.depositId,'hex')]);
   if(!record)throw new ObjectError('NOT_FOUND','controlled absent',{reason:'notFound'});
   return {dynamicField:{value:{type:mode==='type'?'0x1::wrong::Deposit':`${base.packageAddress}::token_escrow::Deposit`,
    bcs:Deposit.serialize({...record,funds:mode==='funds'?'0':record.funds}).toBytes()}}};
  }},
  listCoins:async()=>({objects:[{objectId:address('4'),balance:mode==='insufficient'?'1':'999'}],hasNextPage:false}),
 };
 const execute=async(_client,tx,signer)=>{
  assert.equal(signer.toSuiAddress(),key.toSuiAddress());submits++;
  const data=tx.getData();const call=data.commands.find(item=>item.$kind==='MoveCall').MoveCall;
  assert.equal(call.package,base.packageAddress);assert.equal(call.module,'token_escrow');
  const locking=call.function==='lock';
  if(locking){
   assert.equal(data.commands[0].$kind,'SplitCoins');
   const amountInput=data.inputs[data.commands[0].SplitCoins.amounts[0].Input];
   assert.equal(bcs.u64().parse(Buffer.from(amountInput.Pure.bytes,'base64')),'123');
   assert.equal(data.commands.some(item=>item.$kind==='TransferObjects'),false);
  }else{
   assert.equal(call.function,'release');assert.equal(data.commands.at(-1).$kind,'TransferObjects');
   const ownerInput=data.inputs[data.commands.at(-1).TransferObjects.address.Input];
   assert.equal(bcs.Address.parse(Buffer.from(ownerInput.Pure.bytes,'base64')),key.toSuiAddress());
  }
  record={owner:mode==='readback' ? address('9'):key.toSuiAddress(),amount:'123',released:!locking,funds:locking?'123':'0'};
  return {digest:'controlled-original'};
 };
 return {client,execute,pages:async fetch=>(await fetch(null)).objects,count:()=>submits};
}

test('exact lock uses real SDK split/Move commands and validates original BCS record',async()=>{
 const f=fixture();const result=await executeTokenEscrow(f.client,{...base,operation:'tokenEscrowLock'},f.execute,f.pages);
 assert.equal(result.amountUnits,'123');assert.equal(result.released,false);assert.equal(f.count(),1);
});
test('release transfers only original owned deposit coin to its owner',async()=>{
 const f=fixture('valid',{owner:key.toSuiAddress(),amount:'123',released:false,funds:'123'});
 const result=await executeTokenEscrow(f.client,{...base,operation:'tokenEscrowRelease'},f.execute,f.pages);
 assert.equal(result.amountUnits,'123');assert.equal(result.released,true);assert.equal(f.count(),1);
});
for(const mode of ['private','protocol','insufficient'])test('preflight rejects before submission: '+mode,async()=>{
 const f=fixture(mode);await assert.rejects(executeTokenEscrow(f.client,{...base,operation:'tokenEscrowLock'},f.execute,f.pages));
 assert.equal(f.count(),0);
});
for(const mode of ['type','funds'])test('exact deposit record rejects malformed evidence: '+mode,async()=>{
 const f=fixture(mode,{owner:key.toSuiAddress(),amount:'123',released:false,funds:'123'});
 await assert.rejects(readTokenDeposit(f.client,base));assert.equal(f.count(),0);
});
for(const released of [false,true])test('used ID cannot be replaced, released='+released,async()=>{
 const f=fixture('valid',{owner:key.toSuiAddress(),amount:'123',released,funds:released?'0':'123'});
 await assert.rejects(executeTokenEscrow(f.client,{...base,operation:'tokenEscrowLock'},f.execute,f.pages),/already used/);
 assert.equal(f.count(),0);
});
test('foreign owner cannot release',async()=>{
 const f=fixture('valid',{owner:address('9'),amount:'123',released:false,funds:'123'});
 await assert.rejects(executeTokenEscrow(f.client,{...base,operation:'tokenEscrowRelease'},f.execute,f.pages),/owned/);
 assert.equal(f.count(),0);
});
test('postreceipt mismatch fails after exactly one controlled submission',async()=>{
 const f=fixture('readback');await assert.rejects(executeTokenEscrow(f.client,{...base,operation:'tokenEscrowLock'},f.execute,f.pages),/readback/);
 assert.equal(f.count(),1);
});

// Resolve only controlled object references/gas metadata, never replace the
// generated commands or SDK build/signing/submission owner.
function resolveOffline(tx){
 const data=tx.getData();
 data.inputs=data.inputs.map(input=>input.UnresolvedObject
  ?input.UnresolvedObject.objectId===base.escrowObjectId
   ?Inputs.SharedObjectRef({objectId:base.escrowObjectId,initialSharedVersion:'1',mutable:true})
   :Inputs.ObjectRef({objectId:input.UnresolvedObject.objectId,version:'1',digest:toBase58(new Uint8Array(32).fill(1))})
  :input);
 const resolved=Transaction.from(JSON.stringify(data));
 resolved.setGasPrice(1);resolved.setGasBudget(1000);
 resolved.setGasPayment([{objectId:address('5'),version:'1',digest:toBase58(new Uint8Array(32).fill(2))}]);
 return resolved;
}

for(const locking of [false,true])for(const mode of ['success','reject-preparation','readback'])
 test('actual signed escrow bytes preserve original graph/preparation/evidence: '+(locking?'lock':'release')+'/'+mode,async()=>{
  const initial=locking?null:{owner:key.toSuiAddress(),amount:'123',released:false,funds:'123'};
  const f=fixture(mode==='readback'?'readback':'valid',initial);
  const order=[];let original;let submissions=0;
  const client={
   ...f.client,
   executeTransaction:async request=>{
    assert.deepEqual(order,['prepared','persisted']);submissions++;order.push('submit');
    const transaction=Transaction.from(request.transaction);const digest=await transaction.getDigest();
    assert.equal(digest,original);
    assert.equal((await verifyTransactionSignature(request.transaction,request.signatures[0])).toSuiAddress(),key.toSuiAddress());
    // Existing graph verifier checks exact split or release+owner transfer on decoded signed bytes.
    await f.execute(client,transaction,key);
    return {Transaction:{digest,status:{success:true}}};
   },
   waitForTransaction:async request=>{assert.equal(request.digest,original);order.push('receipt');},
  };
  const operation=withNativeMutationEvidence(()=>executeTokenEscrow(client,{...base,
   operation:locking?'tokenEscrowLock':'tokenEscrowRelease'},
   (nativeClient,tx,signer)=>execute(nativeClient,resolveOffline(tx),signer),f.pages),async evidence=>{
    order.push('prepared');const bytes=Buffer.from(evidence.signedTransactionBase64,'base64');
    original=await Transaction.from(bytes).getDigest();assert.equal(original,evidence.transactionHash);
    assert.equal((await verifyTransactionSignature(bytes,evidence.signature)).toSuiAddress(),key.toSuiAddress());
    if(mode==='reject-preparation')throw new Error('controlled durable admission rejection');
    order.push('persisted');
   });
  if(mode==='success'){
   const reply=await operation;assert.equal(reply.transactionHash,original);assert.equal(reply.released,!locking);
  }else await assert.rejects(operation,error=>{
   if(mode==='reject-preparation')assert.equal(sdkErrorEnvelope(error).nativeMutation,undefined);
   else assert.deepEqual(sdkErrorEnvelope(error).nativeMutation,{transactionHash:original,executionSucceeded:true});
   return true;
  });
  assert.equal(submissions,mode==='reject-preparation'?0:1);assert.equal(f.count(),submissions);
 });
