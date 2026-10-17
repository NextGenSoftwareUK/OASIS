import test from 'node:test';
import assert from 'node:assert/strict';
import { AccountId, Transaction } from '@hiero-ledger/sdk';
import { tokenEscrowTransaction, executeTokenEscrow } from './escrow.mjs';
import { idBoundEscrowAbi, isIdBoundEscrow, validateDeposit, sameDeposit, verifyDepositReadback } from './id-bound-escrow.mjs';
import { tokenUnits } from './token-amount.mjs';

const request={sender:'0.0.1001',contractId:'0.0.2001',tokenId:'0.0.3001',gas:400000,maxFeeTinybars:'100000000',
 runtimeCodeHash:'0x'+'11'.repeat(32),transactionId:'0.0.1001@1700000000.000000001',
 approvalTransactionId:'0.0.1001@1700000000.000000002',escrowProtocol:'id-bound-v1',depositId:'0x'+'12'.repeat(32),amount:'90071992547409.93',amountUnits:'9007199254740993'};
for(const action of ['tokenEscrowLock','tokenEscrowUnlock'])test(`${action} per-deposit SDK serialization retains exact identity, units and original transaction`,()=>{
 const selected={...request,...(action==='tokenEscrowUnlock'?{amount:'0'}:{})};
 const tx=tokenEscrowTransaction(action,selected);
 assert.equal(tx.maxAttempts,1);assert.equal(tx.regenerateTransactionId,false);
 tx.setNodeAccountIds([AccountId.fromString('0.0.3')]).freeze();
 const decoded=Transaction.fromBytes(tx.toBytes()),call=idBoundEscrowAbi.parseTransaction({data:decoded.functionParameters});
 assert.equal(call.name,action==='tokenEscrowLock'?'lock':'unlock');assert.equal(call.args[0],request.depositId);
 if(action==='tokenEscrowLock'){
  assert.equal(call.args[1].toLowerCase(),'0x0000000000000000000000000000000000000bb9');
  assert.equal(call.args[2],9007199254740993n);assert.equal(tokenUnits(request.amount,2),call.args[2]);
 }
 assert.equal(decoded.contractId.toString(),request.contractId);assert.equal(decoded.transactionId.toString(),request.transactionId);
 assert.equal(decoded.maxTransactionFee.toTinybars().toString(),request.maxFeeTinybars);assert.equal(decoded.gas.toString(),'400000');
});
test('explicit unknown protocol never selects lock-all',()=>{
 assert.equal(isIdBoundEscrow({}),false);assert.equal(isIdBoundEscrow({escrowProtocol:'aggregate-v1'}),false);
 for(const escrowProtocol of [null,'','id-bound-v2','guess'])assert.throws(()=>tokenEscrowTransaction('tokenEscrowLock',{...request,escrowProtocol}),/protocol/);
 for(const payload of [{depositId:request.depositId},{amount:'1.23'}])assert.throws(()=>isIdBoundEscrow(payload),/cannot ignore/);
});
test('invalid identity fails before key resolution or paid client creation',async()=>{
 for(const depositId of [undefined,'','0x'+'00'.repeat(32),'0x12',12]){
  let keys=0,clients=0;
  await assert.rejects(()=>executeTokenEscrow('tokenEscrowLock',{...request,depositId},{restoreKey:()=>{keys++;},createClient:()=>{clients++;}}),/deposit identity/);
  assert.equal(keys,0);assert.equal(clients,0);
 }
});
test('nonexact lock amount, unsupported units and partial release are rejected',()=>{
 for(const amount of [undefined,1,'0','-1','1e3','NaN',''])assert.throws(()=>tokenEscrowTransaction('tokenEscrowLock',{...request,amount}));
 for(const amountUnits of [undefined,1,'0','-1','9223372036854775808'])assert.throws(()=>tokenEscrowTransaction('tokenEscrowLock',{...request,amountUnits}));
 assert.throws(()=>tokenEscrowTransaction('tokenEscrowUnlock',request),/whole identified deposit/);
 assert.throws(()=>tokenUnits('1.234',2),/precision/);assert.throws(()=>tokenUnits('92233720368547758.08',2),/int64/);
});
const owner='0x'+'11'.repeat(20),token='0x'+'22'.repeat(20),zero='0x'+'00'.repeat(20);
const empty={owner:zero,token:zero,amount:0n,released:false};
const owned={owner,token,amount:123n,released:false};
test('only empty unused deposit can lock and only exact owned deposit can release',()=>{
 validateDeposit(empty,owner,token,true);validateDeposit(owned,owner,token,false);
});
for(const [name,record,locking] of [
 ['used',owned,true],['empty',empty,false],['foreign',{...owned,owner:token},false],
 ['different-token',{...owned,token:owner},false],['released',{...owned,released:true},false],
 ['int64-overflow',{...owned,amount:9223372036854775808n},false],['dirty-empty',{...empty,amount:1n},true]
])test(`${name} deposit is rejected rather than changing custody semantics`,()=>assert.throws(()=>validateDeposit(record,owner,token,locking),/Deposit identity/));
test('readback requires original owner/token/amount/released state and snapshots compare all fields',()=>{
 verifyDepositReadback(owned,owner,token,123n,false);
 for(const record of [{...owned,owner:token},{...owned,token:owner},{...owned,amount:124n},{...owned,released:true}])
  assert.throws(()=>verifyDepositReadback(record,owner,token,123n,false),/readback/);
 assert.equal(sameDeposit([owner,token,123n,false],[owner,token,123n,false]),true);
 assert.equal(sameDeposit([owner,token,123n,false],[owner,token,124n,false]),false);
});
