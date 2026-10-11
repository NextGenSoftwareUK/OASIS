import test from 'node:test';
import assert from 'node:assert/strict';
import { AccountId, Transaction, ContractExecuteTransaction, AccountAllowanceApproveTransaction } from '@hiero-ledger/sdk';
import { escrowAbi, escrowTransaction, escrowApprovalTransaction, tokenEscrowTransaction, tokenEscrowApprovalTransaction, verifyDepositorAccount, verifyEscrowRuntime } from './escrow.mjs';
import { keccak256 } from 'ethers';
import { execute } from './bridge.mjs';
import { PrivateKey } from '@hiero-ledger/sdk';
const request = {sender:'0.0.1001',contractId:'0.0.2001',tokenId:'0.0.3001',serial:'9007199254740993',
 gas:400000,maxFeeTinybars:'100000000',runtimeCodeHash:'0x'+'11'.repeat(32),transactionId:'0.0.1001@1700000000.000000001',
 approvalTransactionId:'0.0.1001@1700000000.000000002'};
for(const action of ['tokenEscrowLock','tokenEscrowUnlock']) test(`${action} dispatcher rejects missing EVM identity before network use`,async()=>{
 await assert.rejects(()=>execute(action,request),/real sender EVM address/);
});
for(const action of ['tokenEscrowLock','tokenEscrowUnlock']) test(`${action} dispatcher rejects signer mismatch before paid queries`,async()=>{
 const signer=PrivateKey.generateED25519(), other=PrivateKey.generateED25519();
 await assert.rejects(()=>execute(action,{...request,senderEvmAddress:'0x'+'12'.repeat(20),network:'testnet',privateKey:signer.toStringDer(),keyType:'der',publicKey:other.publicKey.toStringDer()}),/signing key mismatch/);
});
function roundTrip(tx) {tx.setNodeAccountIds([AccountId.fromString('0.0.3')]).freeze();return Transaction.fromBytes(tx.toBytes());}
for (const action of ['tokenEscrowLock','tokenEscrowUnlock']) test(`${action} uses exact contract ABI without an NFT serial`,()=>{
 const {serial,...tokenRequest}=request;
 const submission=tokenEscrowTransaction(action,tokenRequest);
 assert.equal(submission.maxAttempts,1);assert.equal(submission.regenerateTransactionId,false);
 const tx=roundTrip(submission), decoded=escrowAbi.parseTransaction({data:tx.functionParameters});
 assert.equal(decoded.name,action==='tokenEscrowLock'?'lockAll':'unlock');
 assert.equal(decoded.args.length,1);assert.equal(decoded.args[0].toLowerCase(),'0x0000000000000000000000000000000000000bb9');
 assert.equal(tx.contractId.toString(),request.contractId);assert.equal(tx.gas.toString(),String(request.gas));
 assert.equal(tx.transactionId.toString(),request.transactionId);assert.equal(tx.maxTransactionFee.toTinybars().toString(),request.maxFeeTinybars);
});
test('fungible allowance preserves int64 token units above Number precision in SDK wire bytes',()=>{
 const submission=tokenEscrowApprovalTransaction({...request,amountUnits:'9007199254740993'});
 assert.equal(submission.maxAttempts,1);assert.equal(submission.regenerateTransactionId,false);
 const tx=roundTrip(submission), allowance=tx.tokenApprovals[0];
 assert.equal(tx.tokenApprovals.length,1);assert.equal(allowance.amount.toString(),'9007199254740993');
 assert.equal(allowance.tokenId.toString(),request.tokenId);assert.equal(allowance.ownerAccountId.toString(),request.sender);
 assert.equal(allowance.spenderAccountId.toString(),request.contractId);assert.equal(tx.transactionId.toString(),request.approvalTransactionId);
});
test('fungible allowance rejects ambiguous or out-of-range amounts and reused approval IDs',()=>{
 for(const amountUnits of [undefined,1,'0','-1','1.5','9223372036854775808'])
  assert.throws(()=>tokenEscrowApprovalTransaction({...request,amountUnits}),/exact positive int64/);
 assert.throws(()=>tokenEscrowApprovalTransaction({...request,amountUnits:'1',approvalTransactionId:request.transactionId}),/distinct transaction ID/);
 assert.throws(()=>tokenEscrowTransaction('unknown',request),/Unknown token escrow/);
});
for (const action of ['nftEscrowLock','nftEscrowUnlock']) test(`${action} preserves exact serial, contract, fees and single submission in SDK wire bytes`,()=>{
 const submission=escrowTransaction(action,request);
 assert.equal(submission.maxAttempts,1);assert.equal(submission.regenerateTransactionId,false);
 const tx=roundTrip(submission);
 assert.ok(tx instanceof ContractExecuteTransaction);
 assert.equal(tx.contractId.toString(),request.contractId);
 assert.equal(tx.gas.toString(),String(request.gas));
 const decoded=escrowAbi.parseTransaction({data:tx.functionParameters});
 assert.equal(decoded.name,action==='nftEscrowLock'?'lockNFT':'unlockNFT');
 assert.equal(decoded.args[1],9007199254740993n);
 assert.equal(decoded.args[0].toLowerCase(),'0x0000000000000000000000000000000000000bb9');
 assert.equal(tx.maxTransactionFee.toTinybars().toString(),request.maxFeeTinybars);
});
test('approval is a single-serial native HTS allowance for the explicit owner and contract',()=>{
 const submission=escrowApprovalTransaction(request);
 assert.equal(submission.maxAttempts,1);assert.equal(submission.regenerateTransactionId,false);
 const tx=roundTrip(submission);
 assert.ok(tx instanceof AccountAllowanceApproveTransaction);
 assert.equal(tx.tokenNftApprovals.length,1);
 const allowance=tx.tokenNftApprovals[0];
 assert.equal(allowance.tokenId.toString(),request.tokenId);
 assert.equal(allowance.ownerAccountId.toString(),request.sender);
 assert.equal(allowance.spenderAccountId.toString(),request.contractId);
 assert.deepEqual(allowance.serialNumbers.map(n=>n.toString()),[request.serial]);
 assert.equal(tx.transactionId.toString(),request.approvalTransactionId);
});
for (const [field,value] of [['sender','0.0.0'],['contractId',''],['tokenId','bad'],['serial','0'],['serial','9223372036854775808'],
 ['gas',0],['gas',1.5],['maxFeeTinybars','0'],['maxFeeTinybars','9223372036854775808'],['transactionId','0.0.2002@1700000000.000000001']])
 test(`invalid ${field}=${value} is rejected before paid SDK execution`,()=>assert.throws(()=>escrowTransaction('nftEscrowLock',{...request,[field]:value})));
test('approval cannot reuse the deposit transaction ID or a different payer',()=>{
 for(const approvalTransactionId of [request.transactionId,'0.0.2002@1700000000.000000001'])
  assert.throws(()=>escrowApprovalTransaction({...request,approvalTransactionId}),/distinct transaction ID/);
});
for(const action of ['nftEscrowLock','nftEscrowUnlock']) test(`${action} dispatcher rejects mismatched signer before funded queries`,async()=>{
 const signer=PrivateKey.generateED25519(), other=PrivateKey.generateED25519();
 await assert.rejects(()=>execute(action,{...request,network:'testnet',privateKey:signer.toStringDer(),keyType:'der',
  publicKey:other.publicKey.toStringDer()}),/signing key mismatch/);
});
test('lock dispatcher rejects reused approval ID before opening a funded client',async()=>{
 await assert.rejects(()=>execute('nftEscrowLock',{...request,approvalTransactionId:request.transactionId}),/distinct transaction ID/);
});
test('runtime code identity rejects empty or changed code without approving custody',()=>{
 const bytes=Uint8Array.from([0x60,0x00,0x60,0x00]);
 verifyEscrowRuntime(bytes,keccak256(bytes));
 assert.throws(()=>verifyEscrowRuntime(Uint8Array.from([0x60,0x01]),keccak256(bytes)),/runtime code/);
 assert.throws(()=>verifyEscrowRuntime(new Uint8Array(),keccak256(new Uint8Array())),/runtime code/);
});
test('EVM depositor must resolve through SDK alias query to the exact active numeric account',async()=>{
 const sender=AccountId.fromString('0.0.1001'), address='0x'+'12'.repeat(20);
 let queries=0;
 await verifyDepositorAccount(sender,address,async alias=>{
  queries++;assert.equal(alias.evmAddress.toString().toLowerCase(),address.slice(2));
  return {accountId:sender,isDeleted:false};
 });
 assert.equal(queries,1);
 await assert.rejects(()=>verifyDepositorAccount(sender,address,async()=>({accountId:AccountId.fromString('0.0.1002'),isDeleted:false})),/requesting account/);
 await assert.rejects(()=>verifyDepositorAccount(sender,address,async()=>({accountId:sender,isDeleted:true})),/requesting account/);
 await assert.rejects(()=>verifyDepositorAccount(sender,'0x'+'0'.repeat(40),async()=>{throw new Error('must not query');}),/valid EVM depositor/);
});
