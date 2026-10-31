import {test} from 'node:test';
import assert from 'node:assert/strict';
import {readWalletHistory} from './bridge.mjs';

const address='0x'+'1'.repeat(64);
function fixture({height=100n,lowest=0n,checkpoint='42',pages}={}){
  const queries=[];let infos=0;
  const frames=pages??[[{transaction:{digest:'original'}},{end:{reason:3},watermark:{checkpoint:height}}]];
  const client={ledgerService:{getServiceInfo:async()=>{infos++;return {response:{checkpointHeight:height,lowestAvailableCheckpoint:lowest}};},
    listTransactions:query=>{queries.push(query);return {responses:(async function*(){yield* frames[queries.length-1];})()};}},
    getTransaction:async({digest})=>({Transaction:{digest,checkpoint,timestampMs:1,status:{success:true},transaction:{sender:address},
      effects:{transactionDigest:digest,changedObjects:[]},balanceChanges:[]}})};
  return {client,queries,get infos(){return infos;}};
}

test('Sui requested history range supports available pruned intervals with inclusive bounds',async()=>{
  const mock=fixture({lowest:30n,pages:[[{transaction:{digest:'original'}},{end:{reason:3},watermark:{checkpoint:45n}}]]});
  const records=await readWalletHistory(mock.client,{walletAddress:address,fromBlock:'40',toBlock:'45'});
  assert.equal(records.length,1);assert.equal(mock.queries[0].startCheckpoint,40n);assert.equal(mock.queries[0].endCheckpoint,46n);
});
test('Sui requested history range retains u64 precision beyond JavaScript safe integers',async()=>{
  const mock=fixture({height:9007199254741000n,checkpoint:'9007199254740993'});
  await readWalletHistory(mock.client,{walletAddress:address,fromBlock:'9007199254740993',toBlock:'9007199254740994'});
  assert.equal(mock.queries[0].startCheckpoint,9007199254740993n);assert.equal(mock.queries[0].endCheckpoint,9007199254740995n);
});
test('Sui requested history range rejects malformed or reversed input before ledger queries',async()=>{
  for(const range of [{fromBlock:1},{fromBlock:'01'},{toBlock:'-1'},{toBlock:'18446744073709551616'},{fromBlock:'3',toBlock:'2'}]){
    const mock=fixture();await assert.rejects(readWalletHistory(mock.client,{walletAddress:address,...range}),/range/);assert.equal(mock.infos,0);
  }
});
test('Sui requested history range rejects out-of-bounds original receipt checkpoints',async()=>{
  for(const checkpoint of ['39','46']){
    const mock=fixture({checkpoint});
    await assert.rejects(readWalletHistory(mock.client,{walletAddress:address,fromBlock:'40',toBlock:'45'}),/checkpoint/);
  }
});
test('Sui requested history range beyond committed tip needs no index scan',async()=>{
  const mock=fixture();assert.deepEqual(await readWalletHistory(mock.client,{walletAddress:address,fromBlock:'101'}),[]);assert.equal(mock.queries.length,0);
});
test('Sui requested history range includes genesis checkpoint zero',async()=>{
  const mock=fixture({checkpoint:'0',pages:[[{transaction:{digest:'original'}},{end:{reason:3},watermark:{checkpoint:0n}}]]});
  assert.equal((await readWalletHistory(mock.client,{walletAddress:address,fromBlock:'0',toBlock:'0'})).length,1);
  assert.equal(mock.queries[0].startCheckpoint,0n);assert.equal(mock.queries[0].endCheckpoint,1n);
});
test('Sui requested history range remains fixed across pagination and verifies terminal index coverage',async()=>{
  const cursor=new Uint8Array([1]);
  const mock=fixture({pages:[[{end:{reason:1},watermark:{checkpoint:41n,cursor}}],[{transaction:{digest:'original'}},{end:{reason:3},watermark:{checkpoint:45n}}]]});
  assert.equal((await readWalletHistory(mock.client,{walletAddress:address,fromBlock:'40',toBlock:'45'})).length,1);
  assert.equal(mock.queries.length,2);assert.equal(mock.queries[1].options.after,cursor);
  for(const query of mock.queries){assert.equal(query.startCheckpoint,40n);assert.equal(query.endCheckpoint,46n);}
  const behind=fixture({pages:[[{end:{reason:5},watermark:{checkpoint:44n}}]]});
  await assert.rejects(readWalletHistory(behind.client,{walletAddress:address,fromBlock:'40',toBlock:'45'}),/not covered/);
});
test('Sui requested history range refuses unavailable pruning and exclusive-u64 overflow',async()=>{
  const pruned=fixture({lowest:41n});await assert.rejects(readWalletHistory(pruned.client,{walletAddress:address,fromBlock:'40'}),/prun/);assert.equal(pruned.queries.length,0);
  const overflow=fixture({height:18446744073709551615n});
  await assert.rejects(readWalletHistory(overflow.client,{walletAddress:address}),/exclusive/);assert.equal(overflow.queries.length,0);
});
test('Sui requested history range validates exact service availability before scanning',async()=>{
  for(const response of [{checkpointHeight:'100',lowestAvailableCheckpoint:0n},{checkpointHeight:100n},
    {checkpointHeight:100n,lowestAvailableCheckpoint:101n},{checkpointHeight:-1n,lowestAvailableCheckpoint:0n},
    {checkpointHeight:18446744073709551616n,lowestAvailableCheckpoint:0n}]){
    const mock=fixture();mock.client.ledgerService.getServiceInfo=async()=>({response});
    await assert.rejects(readWalletHistory(mock.client,{walletAddress:address}),/availability/);assert.equal(mock.queries.length,0);
  }
});
