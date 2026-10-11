import test from 'node:test';import assert from 'node:assert/strict';
import {PrivateKey,Transaction,AccountId} from '@hiero-ledger/sdk';
import {deploymentTransaction} from './deployment.mjs';
test('NFT collection creation serializes real HTS type, keys, treasury and exact int64 supply',()=>{
 const key=PrivateKey.generateECDSA().publicKey;
 const tx=deploymentTransaction('nftCollectionCreate',{...request,name:'Oak seeds',symbol:'OAK',maxSupply:'9007199254740993'},key);
 assert.equal(tx.maxAttempts,1);assert.equal(tx.regenerateTransactionId,false);
 tx.setNodeAccountIds([AccountId.fromString('0.0.3')]).freeze();const decoded=Transaction.fromBytes(tx.toBytes());
 assert.equal(decoded.tokenType.toString(),'NON_FUNGIBLE_UNIQUE');
 assert.equal(decoded.treasuryAccountId.toString(),request.sender);
 assert.equal(decoded.maxSupply.toString(),'9007199254740993');
 assert.equal(decoded.adminKey.toString(),key.toString());assert.equal(decoded.supplyKey.toString(),key.toString());
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
