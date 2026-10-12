import test from 'node:test';import assert from 'node:assert/strict';
import {PrivateKey,Transaction,AccountId} from '@hiero-ledger/sdk';
import {deploymentTransaction} from './deployment.mjs';
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
