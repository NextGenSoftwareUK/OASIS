import test from 'node:test';import assert from 'node:assert/strict';import {reconcileFailedCreation} from './reconcile-hedera-store-deployment.mjs';
const state={sender:'0.0.1001',fileId:'0.0.2001',uploaded:11186,pending:{action:'contractCreate',transactionId:'0.0.1001@1700000000.000000001'}};
const record={transaction_id:'0.0.1001-1700000000-000000001',name:'CONTRACTCREATEINSTANCE',result:'INSUFFICIENT_TX_FEE',scheduled:false,nonce:0,parent_consensus_timestamp:null,entity_id:null,consensus_timestamp:'1700000001.000000001'};
test('confirmed failure clears only pending marker while preserving reusable upload and original evidence',()=>{
 const next=reconcileFailedCreation(state,{transactions:[record]});assert.equal(next.pending,undefined);
 assert.equal(next.fileId,state.fileId);assert.equal(next.uploaded,state.uploaded);assert.ok(state.pending);
 assert.equal(next.reconciledFailures[0].transactionId,state.pending.transactionId);
});
test('ambiguous, successful, foreign or created-contract records cannot authorize resubmission',()=>{
 for(const change of [{result:'SUCCESS'},{result:'UNKNOWN'},{transaction_id:'other'},{entity_id:'0.0.3001'},{scheduled:true},{nonce:1},{name:'CRYPTOTRANSFER'}])
  assert.throws(()=>reconcileFailedCreation(state,{transactions:[{...record,...change}]}));
 for(const transactions of [[],[record,record],null])assert.throws(()=>reconcileFailedCreation(state,{transactions}));
 assert.ok(state.pending);
});
