import test from 'node:test';import assert from 'node:assert/strict';
import {PrivateKey,Transaction,AccountId,Client,TransferTransaction,TransactionId} from '@hiero-ledger/sdk';
import {deploymentTransaction,executeDeployment} from './deployment.mjs';
import {execute,persistSignedSubmission} from './bridge.mjs';
import {verifyPreparedIdentity} from './native-preparation.mjs';

test('original signed hashes bind every SDK node variant and distinguish payloads sharing an ID',async()=>{
 const key=PrivateKey.generateED25519(),id='0.0.1001@1700000000.000000001';
 let previous;
 for(const amount of [1,2]){
  const signed=await new TransferTransaction().setTransactionId(TransactionId.fromString(id))
   .setNodeAccountIds([AccountId.fromString('0.0.3'),AccountId.fromString('0.0.4')])
   .addHbarTransfer('0.0.1001',-amount).addHbarTransfer('0.0.1002',amount).freeze().sign(key);
  const evidence={transactionHash:id,sender:'0.0.1001',signedTransactionBase64:Buffer.from(signed.toBytes()).toString('base64')};
  const verified=await verifyPreparedIdentity(evidence,key.publicKey.toStringDer());
  const expected=[...(await signed.getTransactionHashPerNode()).values()].map(hash=>Buffer.from(hash).toString('hex'));
  assert.deepEqual(verified.signedTransactionHashes,expected);assert.equal(expected.length,2);
  for(const hash of expected){assert.match(hash,/^[0-9a-f]{96}$/);if(previous)assert.equal(previous.includes(hash),false);}
  previous=expected;
 }
});
test('prepared dispatcher refuses invalid callback and unwired actions before network use',async()=>{
 await assert.rejects(execute('contractCreate',{},true),/awaited callback/);
 let calls=0;
 await assert.rejects(execute('storageGet',{},async()=>{calls++;}),/not yet supported/);
 assert.equal(calls,0);
});
test('real SDK signed deployment waits for persistence and rejection prevents submission',async()=>{
 const key=PrivateKey.generateECDSA(),client=Client.forNetwork({'127.0.0.1:1':'0.0.3'});
 let calls=0,closed=false;const close=client.close.bind(client);client.close=()=>{closed=true;close();};
 const failure=new Error('controlled persistence rejected');
 await assert.rejects(executeDeployment('contractCreate',{...request,network:'controlled-local'}, {
  createClient:async()=>client,restoreKey:()=>key,persistPreparedSubmission:async evidence=>{
   calls++;assert.equal(evidence.transactionHash,request.transactionId);assert.equal(evidence.sender,request.sender);
   const original=Transaction.fromBytes(Buffer.from(evidence.signedTransactionBase64,'base64'));
   assert.equal(original.transactionId.toString(),request.transactionId);assert.equal(Object.isFrozen(evidence),true);
   assert.equal((await verifyPreparedIdentity(evidence,key.publicKey.toStringDer())).verified,true);
   assert.equal((await execute('verifyNativePreparation',{preparation:evidence,expectedPublicKey:key.publicKey.toStringDer()})).verified,true);
   await assert.rejects(execute('verifyNativePreparation',{preparation:evidence,expectedPublicKey:PrivateKey.generateECDSA().publicKey.toStringDer()}),/signature mismatch/);
   await assert.rejects(verifyPreparedIdentity(evidence,PrivateKey.generateECDSA().publicKey.toStringDer()),/signature mismatch/);
   await assert.rejects(verifyPreparedIdentity({...evidence,sender:'0.0.1002'},key.publicKey.toStringDer()),/payer mismatch/);
   await assert.rejects(verifyPreparedIdentity({...evidence,transactionHash:'0.0.1001@1700000001.000000001'},key.publicKey.toStringDer()),/identity or payer mismatch/);
   let persisted=0;
   await assert.rejects(persistSignedSubmission(original,{...request,network:'controlled-local'},async prepared=>{
    persisted++;assert.equal(prepared.signedTransactionBase64,evidence.signedTransactionBase64);throw failure;
   }),error=>error===failure);
   assert.equal(persisted,1);
   await assert.rejects(persistSignedSubmission(original,{...request,transactionId:'0.0.1001@1700000001.000000001'},async()=>assert.fail('Foreign original must never persist')),/identity mismatch/);
   throw failure;
  }
 }),error=>error===failure);
 assert.equal(calls,1);assert.equal(closed,true);
});
for (const action of ['bytecodeFileCreate','bytecodeFileAppend','contractCreate','nftCollectionCreate','tokenCollectionDelete','nftMetadataUpdate']) {
 test(`${action} constructor refuses absent original identity`, () => {
  const key=PrivateKey.generateECDSA().publicKey;
  const valid={...request,name:'Oak',symbol:'OAK',maxSupply:'10',tokenId:'0.0.3001',serials:['1'],metadata:'Oak'};
  assert.doesNotThrow(()=>deploymentTransaction(action,valid,key));
  for(const transactionId of [undefined,null,'',' '])
   assert.throws(()=>deploymentTransaction(action,{...valid,transactionId},key));
 });
}
test('collection and NFT metadata reject malformed Unicode rather than replace it',()=>{
 const key=PrivateKey.generateECDSA().publicKey;
 for(const malformed of ['\ud800','\udfff']){
  assert.throws(()=>deploymentTransaction('nftCollectionCreate',{...request,name:malformed,symbol:'OAK',maxSupply:'10'},key));
  assert.throws(()=>deploymentTransaction('nftMetadataUpdate',{...request,tokenId:'0.0.2001',serials:['1'],metadata:malformed},key));
 }
});
test('NFT metadata update uses native SDK wire fields with exact serials and UTF-8',()=>{
 const key=PrivateKey.generateECDSA().publicKey;
 const input={...request,tokenId:'0.0.2001',serials:['9007199254740993'],metadata:'Oak 🌳'};
 const tx=deploymentTransaction('nftMetadataUpdate',input,key);
 tx.setNodeAccountIds([AccountId.fromString('0.0.3')]).freeze();const decoded=Transaction.fromBytes(tx.toBytes());
 assert.equal(decoded._tokenId.toString(),input.tokenId);assert.equal(decoded._serialNumbers[0].toString(),input.serials[0]);
 assert.equal(Buffer.from(decoded._metadata).toString('utf8'),input.metadata);
 for(const serials of [[],['0'],['1','1'],['9223372036854775808']])
  assert.throws(()=>deploymentTransaction('nftMetadataUpdate',{...input,serials},key));
 assert.throws(()=>deploymentTransaction('nftMetadataUpdate',{...input,metadata:'🌳'.repeat(26)},key));
});
test('collection royalty serializes a real HTS fee with issuer collector and no fallback fee',()=>{
 const key=PrivateKey.generateECDSA().publicKey;
 const tx=deploymentTransaction('nftCollectionCreate',{...request,name:'Oak',symbol:'OAK',maxSupply:'10',royaltyPercentage:7},key);
 tx.setNodeAccountIds([AccountId.fromString('0.0.3')]).freeze();const decoded=Transaction.fromBytes(tx.toBytes());
 assert.equal(decoded.customFees.length,1);
 const fee=decoded.customFees[0];assert.equal(fee.numerator.toString(),'7');assert.equal(fee.denominator.toString(),'100');
 assert.equal(fee.feeCollectorAccountId.toString(),request.sender);assert.equal(fee.fallbackFee,null);
 for(const royaltyPercentage of [-1,101,0.5,'7'])
  assert.throws(()=>deploymentTransaction('nftCollectionCreate',{...request,name:'Oak',symbol:'OAK',maxSupply:'10',royaltyPercentage},key));
});
test('collection deletion encodes only explicit native token ID with original payer and fee',()=>{
 const key=PrivateKey.generateECDSA().publicKey;
 const tx=deploymentTransaction('tokenCollectionDelete',{...request,tokenId:'0.0.2001'},key);
 assert.equal(tx.maxAttempts,1);assert.equal(tx.regenerateTransactionId,false);
 tx.setNodeAccountIds([AccountId.fromString('0.0.3')]).freeze();const decoded=Transaction.fromBytes(tx.toBytes());
 assert.equal(decoded.tokenId.toString(),'0.0.2001');assert.equal(decoded.transactionId.toString(),request.transactionId);
 assert.equal(decoded.maxTransactionFee.toTinybars().toString(),request.maxFeeTinybars);
 for(const tokenId of [undefined,'0.0.0','0.0.2001/1','fixture'])
  assert.throws(()=>deploymentTransaction('tokenCollectionDelete',{...request,tokenId},key));
});
test('NFT collection creation serializes real HTS type, keys, treasury and exact int64 supply',()=>{
 const key=PrivateKey.generateECDSA().publicKey;
 const tx=deploymentTransaction('nftCollectionCreate',{...request,name:'Oak seeds',symbol:'OAK',maxSupply:'9007199254740993'},key);
 assert.equal(tx.maxAttempts,1);assert.equal(tx.regenerateTransactionId,false);
 tx.setNodeAccountIds([AccountId.fromString('0.0.3')]).freeze();const decoded=Transaction.fromBytes(tx.toBytes());
 assert.equal(decoded.tokenType.toString(),'NON_FUNGIBLE_UNIQUE');
 assert.equal(decoded.treasuryAccountId.toString(),request.sender);
 assert.equal(decoded.maxSupply.toString(),'9007199254740993');
 assert.equal(decoded.adminKey.toString(),key.toString());assert.equal(decoded.supplyKey.toString(),key.toString());
 assert.equal(decoded.metadataKey.toString(),key.toString());
 assert.equal(decoded.transactionId.toString(),request.transactionId);
});
test('NFT collection invalid supply and UTF-8 metadata reject before signing',()=>{
 const key=PrivateKey.generateECDSA().publicKey,collection={...request,name:'Oak',symbol:'OAK',maxSupply:'10'};
 for(const maxSupply of ['0','-1','1.5','9223372036854775808',undefined])
  assert.throws(()=>deploymentTransaction('nftCollectionCreate',{...collection,maxSupply},key));
 for(const name of ['', ' ', '🌳'.repeat(26)])
  assert.throws(()=>deploymentTransaction('nftCollectionCreate',{...collection,name},key));
});
const request={sender:'0.0.1001',transactionId:'0.0.1001@1700000000.000000001',maxFeeTinybars:'100000000',gas:2000000,fileId:'0.0.2001',contents:'60006000'};
for(const action of ['bytecodeFileCreate','bytecodeFileAppend','contractCreate']) test(`${action} preserves explicit payer, fees and native SDK payload`,()=>{
 const key=PrivateKey.generateECDSA().publicKey,tx=deploymentTransaction(action,request,key);
 assert.equal(tx.maxAttempts,1);assert.equal(tx.regenerateTransactionId,false);
 tx.setNodeAccountIds([AccountId.fromString('0.0.3')]).freeze();const decoded=Transaction.fromBytes(tx.toBytes());
 assert.equal(decoded.transactionId.toString(),request.transactionId);assert.equal(decoded.maxTransactionFee.toTinybars().toString(),request.maxFeeTinybars);
 if(action==='contractCreate'){assert.equal(decoded.bytecodeFileId.toString(),request.fileId);assert.equal(decoded.gas.toString(),String(request.gas));}
 else assert.equal(Buffer.from(decoded.contents).toString(),request.contents);
});
test('deployment rejects missing limits, ambiguous bytecode and mismatched payer before signing',()=>{
 const key=PrivateKey.generateECDSA().publicKey;
 for(const contents of ['', '0x6000','abc','gg','00'.repeat(513)]) assert.throws(()=>deploymentTransaction('bytecodeFileCreate',{...request,contents},key));
 for(const maxFeeTinybars of [undefined,'0','9223372036854775808']) assert.throws(()=>deploymentTransaction('contractCreate',{...request,maxFeeTinybars},key));
 assert.throws(()=>deploymentTransaction('contractCreate',{...request,gas:0},key));
 assert.throws(()=>deploymentTransaction('contractCreate',{...request,transactionId:'0.0.1002@1700000000.000000001'},key));
});
