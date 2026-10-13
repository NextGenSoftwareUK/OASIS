import test from 'node:test';
import assert from 'node:assert/strict';
import { verifiedBankTransfer, verifiedContractExecution, verifiedUpload, verifiedInstantiation, verifiedNftCustodyReceipt, cosmosNativeFeeReceipt, bankTransferLegs, cosmosFailureMessage, cosmosErrorEnvelope, guardCosmosSigningClient, withCosmosNativeEvidence, indexedHistorySnapshot } from './transaction-results.mjs';
import { createHash } from 'node:crypto';
import { SigningCosmWasmClient } from '@cosmjs/cosmwasm';
import { DirectSecp256k1Wallet } from '@cosmjs/proto-signing';
import { TxRaw, AuthInfo } from 'cosmjs-types/cosmos/tx/v1beta1/tx';
import { wasmTypes } from '@cosmjs/cosmwasm';
import { Registry } from '@cosmjs/proto-signing';
import { defaultRegistryTypes } from '@cosmjs/stargate';
import { toBech32 } from '@cosmjs/encoding';
import { TimeoutError, StargateClient } from '@cosmjs/stargate';
import { captureCosmosDirectSigning, verifyCosmosPreparedIdentity } from './transaction-results.mjs';
import { SignDoc } from 'cosmjs-types/cosmos/tx/v1beta1/tx';
import { invoke } from './bridge.mjs';
test('official broadcast clears its polling timer when synchronous broadcast rejects', async () => {
  const client = new StargateClient(undefined, {});
  const failure = new Error('controlled precheck/transport rejection');
  client.broadcastTxSync = async () => { throw failure; };
  const timers = new Set();
  const set = globalThis.setTimeout, clear = globalThis.clearTimeout;
  globalThis.setTimeout = (...args) => { const timer=set(...args); timers.add(timer); return timer; };
  globalThis.clearTimeout = timer => { timers.delete(timer); return clear(timer); };
  try {
    await assert.rejects(client.broadcastTx(new Uint8Array([1]), 60000), error => error === failure);
    assert.equal(timers.size, 0, 'No orphan polling timer after rejected submission');
  } finally {
    for (const timer of timers) clear(timer);
    globalThis.setTimeout = set; globalThis.clearTimeout = clear;
  }
});
const hash = 'ab'.repeat(32);
test('direct capture rejects wallet-modified original chain or account context',async()=>{
  const doc=SignDoc.fromPartial({bodyBytes:new Uint8Array([1]),authInfoBytes:new Uint8Array([2]),chainId:'original-chain',accountNumber:7n});
  for(const changed of [{chainId:'foreign-chain'},{accountNumber:8n}]){
    const wallet={async signDirect(_,request){return {signed:{...request,...changed},signature:{signature:Buffer.alloc(64).toString('base64')}};}};
    const capture=captureCosmosDirectSigning(wallet);
    await assert.rejects(wallet.signDirect('sender',doc),/changed the requested original signing document/);
    assert.throws(()=>capture({transactionHash:'00'.repeat(32)}),/matching original direct SignDoc/);
  }
});
test('official signing waits for durable preparation and persistence failure never broadcasts', async () => {
  const wallet=await DirectSecp256k1Wallet.fromKey(new Uint8Array(32).fill(7),'wasm');
  const originalSigning=captureCosmosDirectSigning(wallet);
  const [{address}]=await wallet.getAccounts();
  const recipient=toBech32('wasm',new Uint8Array(20).fill(2));
  const client=await SigningCosmWasmClient.offline(wallet);
  client.getSequence=async()=>({accountNumber:7,sequence:3});
  client.getChainId=async()=>'controlled-chain';
  let broadcasts=0, prepared=0;
  client.broadcastTx=async()=>{broadcasts++;assert.fail('Unpersisted signed bytes must not reach broadcast');};
  const persistenceFailure=new Error('controlled journal unavailable');
  guardCosmosSigningClient(client,async evidence=>{
    assert.throws(()=>originalSigning({...evidence,transactionHash:'00'.repeat(32)}),/matching original direct SignDoc/);
    evidence=originalSigning(evidence);
    prepared++;assert.match(evidence.transactionHash,/^[0-9A-F]{64}$/);
    const signed=Buffer.from(evidence.signedTransactionBase64,'base64');
    assert.equal(createHash('sha256').update(signed).digest('hex').toUpperCase(),evidence.transactionHash);
    const raw=TxRaw.decode(signed);assert.equal(raw.signatures.length,1);assert.equal(raw.signatures[0].length,64);
    const doc=SignDoc.decode(Buffer.from(evidence.signDocBase64,'base64'));
    assert.equal(doc.accountNumber,7n);assert.equal(doc.chainId,'controlled-chain');
    assert.deepEqual(doc.bodyBytes,raw.bodyBytes);assert.deepEqual(doc.authInfoBytes,raw.authInfoBytes);
    const bound={...evidence,chainId:'controlled-chain',sender:address};
    assert.equal(verifyCosmosPreparedIdentity(bound).verified,true);
    assert.equal((await invoke({operation:'verifyNativePreparation',rpcEndpoint:'http://127.0.0.1:1',preparation:bound})).verified,true);
    await assert.rejects(invoke({operation:'verifyNativePreparation',preparation:{...bound,chainId:'wrong'}}));
    const extendedDoc=Buffer.concat([Buffer.from(evidence.signDocBase64,'base64'),Buffer.from([0x28,0x01])]);
    assert.throws(()=>verifyCosmosPreparedIdentity({...bound,signDocBase64:extendedDoc.toString('base64')}),/Canonical Cosmos/);
    for(const changed of [{chainId:'other'},{sender:recipient},{transactionHash:'00'.repeat(32)},{signDocBase64:'AQID'},{signedTransactionBase64:'!'}])
      assert.throws(()=>verifyCosmosPreparedIdentity({...bound,...changed}));
    const altered=TxRaw.decode(signed);altered.signatures[0][0]^=1;
    const alteredBytes=TxRaw.encode(altered).finish();
    assert.throws(()=>verifyCosmosPreparedIdentity({...bound,signedTransactionBase64:Buffer.from(alteredBytes).toString('base64'),transactionHash:createHash('sha256').update(alteredBytes).digest('hex').toUpperCase()}));
    assert.throws(()=>originalSigning(evidence),/matching original direct SignDoc/);
    assert.equal(Object.isFrozen(evidence),true);
    throw persistenceFailure;
  });
  await assert.rejects(withCosmosNativeEvidence(()=>client.signAndBroadcast(address,[{
    typeUrl:'/cosmos.bank.v1beta1.MsgSend',value:{fromAddress:address,toAddress:recipient,amount:[{denom:'stake',amount:'1'}]}
  }],{amount:[],gas:'100000'},'prepared persistence test')),error=>error===persistenceFailure);
  assert.equal(prepared,1);assert.equal(broadcasts,0);
});
test('prepared await cannot change the signed bytes bound to persisted identity', async () => {
  const bytes=new Uint8Array([1,2,3]), original=Uint8Array.from(bytes);
  const expected=createHash('sha256').update(original).digest('hex').toUpperCase();
  const client={async broadcastTx(sent){assert.deepEqual(sent,original);return {transactionHash:expected,height:42,code:0};},async signAndBroadcast(){}};
  guardCosmosSigningClient(client,async evidence=>{assert.equal(evidence.transactionHash,expected);bytes.fill(9);await Promise.resolve();});
  assert.equal((await client.broadcastTx(bytes)).transactionHash,expected);
});
test('prepared persistence is awaited before broadcast and rejection prevents native submission', async () => {
  const bytes=new Uint8Array([1,2,3]);
  const expected=createHash('sha256').update(bytes).digest('hex').toUpperCase();
  for(const reject of [false,true]) {
    const order=[];const failure=new Error('controlled durable persistence failure');
    const client={async broadcastTx(){order.push('broadcast');return {transactionHash:expected,height:42,code:0};},async signAndBroadcast(){}};
    guardCosmosSigningClient(client,async evidence=>{
      order.push('prepare');assert.deepEqual(evidence,{transactionHash:expected,signedTransactionBase64:Buffer.from(bytes).toString('base64')});
      await Promise.resolve();
      if(reject)throw failure;
      order.push('persisted');
    });
    if(reject) {
      await assert.rejects(withCosmosNativeEvidence(()=>client.broadcastTx(bytes)),error=>error===failure);
      assert.deepEqual(order,['prepare']);
      assert.equal(cosmosErrorEnvelope(failure).nativeMutation,undefined);
    } else {
      assert.equal((await client.broadcastTx(bytes)).transactionHash,expected);
      assert.deepEqual(order,['prepare','persisted','broadcast']);
    }
  }
});
for (const mode of ['success','poll-error','deadline']) {
 test(`official broadcast clears polling timers on ${mode}`, async () => {
  const client = new StargateClient(undefined, {});
  client.broadcastTxSync = async () => hash;
  const failure = new Error('controlled receipt lookup failure');
  client.getTx = async () => {
    if (mode === 'poll-error') throw failure;
    return mode === 'deadline' ? null : {code:0,height:42,txIndex:0,events:[],gasUsed:1n,gasWanted:1n};
  };
  const timers=new Set(), set=globalThis.setTimeout, clear=globalThis.clearTimeout;
  globalThis.setTimeout=(callback,delay,...args)=>{
    const timer=set(()=>{timers.delete(timer);callback(...args);},delay);
    timers.add(timer);return timer;
  };
  globalThis.clearTimeout=timer=>{timers.delete(timer);return clear(timer);};
  try {
    const operation=client.broadcastTx(new Uint8Array([1]),mode==='deadline'?5:60000,1);
    if(mode==='success') {
      const receipt=await operation;assert.equal(receipt.transactionHash,hash);assert.equal(receipt.height,42);
    } else await assert.rejects(operation,error=>mode==='poll-error'?error===failure:error instanceof TimeoutError && error.txId===hash);
    assert.equal(timers.size,0);
  } finally {
    for(const timer of timers)clear(timer);
    globalThis.setTimeout=set;globalThis.clearTimeout=clear;
  }
 });
}

test('official SDK signing retains exact signed hash before transport and rejects substituted delivery identity', async () => {
  const wallet = await DirectSecp256k1Wallet.fromKey(new Uint8Array(32).fill(7), 'wasm');
  const [{address}] = await wallet.getAccounts();
  const recipient = toBech32('wasm',new Uint8Array(20).fill(2));
  for (const mode of ['transport','mismatch','mapping','failed']) {
    const client = await SigningCosmWasmClient.offline(wallet);
    client.getSequence = async () => ({accountNumber:7,sequence:3});
    client.getChainId = async () => 'controlled-chain';
    let submissions = 0, expected;
    const interruption = new Error('controlled transport interruption');
    client.broadcastTx = async bytes => {
      submissions++;
      const raw = TxRaw.decode(bytes);
      assert.equal(raw.signatures.length,1); assert.equal(raw.signatures[0].length,64);
      expected = createHash('sha256').update(bytes).digest('hex').toUpperCase();
      if(mode==='transport') throw interruption;
      return {transactionHash:mode==='mismatch'?'C'.repeat(64):expected,height:42,code:mode==='failed'?5:0};
    };
    guardCosmosSigningClient(client);
    await assert.rejects(withCosmosNativeEvidence(async () => {
      await client.signAndBroadcast(address,[{typeUrl:'/cosmos.bank.v1beta1.MsgSend',value:{fromAddress:address,toAddress:recipient,amount:[{denom:'stake',amount:'1'}]}}],{amount:[],gas:'100000'},'controlled memo');
      throw new Error('controlled post-execution mapping failure');
    }), error => {
      assert.deepEqual(cosmosErrorEnvelope(error).nativeMutation,{transactionHash:expected,executionSucceeded:mode==='mapping'?true:mode==='failed'?false:null});
      if(mode==='transport')assert.equal(error.cause,interruption);
      return true;
    });
    assert.equal(submissions,1);
  }
});

test('ordinary SDK signing preparation failure performs no broadcast and does not manufacture native evidence', async () => {
  const wallet = await DirectSecp256k1Wallet.fromKey(new Uint8Array(32).fill(7),'wasm');
  const [{address}] = await wallet.getAccounts();
  const client = await SigningCosmWasmClient.offline(wallet);
  const preparation = new Error('controlled sequence lookup failure');
  client.getSequence = async () => {throw preparation;};
  let submissions = 0;
  client.broadcastTx = async () => {submissions++;assert.fail('No signed transaction exists');};
  guardCosmosSigningClient(client);
  await assert.rejects(withCosmosNativeEvidence(()=>client.signAndBroadcast(address,[],{amount:[],gas:'1'},'')),error=>error===preparation&&!cosmosErrorEnvelope(error).nativeMutation);
  assert.equal(submissions,0);
});

test('concurrent Cosmos evidence scopes remain isolated and ordinary preflight stays ordinary', async () => {
  const results = await Promise.all([7,8].map(async seed => {
    const wallet = await DirectSecp256k1Wallet.fromKey(new Uint8Array(32).fill(seed),'wasm');
    const [{address}] = await wallet.getAccounts();
    const client = await SigningCosmWasmClient.offline(wallet);
    client.getSequence=async()=>({accountNumber:7,sequence:3});client.getChainId=async()=>'controlled-chain';
    let expected;
    client.broadcastTx=async bytes=>{
      expected=createHash('sha256').update(bytes).digest('hex').toUpperCase();
      await Promise.resolve();return{transactionHash:expected,height:42,code:0};
    };
    guardCosmosSigningClient(client);
    try {
      await withCosmosNativeEvidence(async()=>{
        await client.signAndBroadcast(address,[],{amount:[],gas:'1'},'');
        await Promise.resolve();throw new Error('post-execution mapping failure');
      });
      assert.fail('Expected mapping failure');
    } catch(error){assert.equal(cosmosErrorEnvelope(error).nativeMutation.transactionHash,expected);return expected;}
  }));
  assert.notEqual(results[0],results[1]);
  const preflight=new Error('ordinary preflight');
  await assert.rejects(withCosmosNativeEvidence(async()=>{throw preflight;}),error=>error===preflight&&!cosmosErrorEnvelope(error).nativeMutation);
});

test('official CosmJS contract methods preserve failed delivery before plain error conversion and submit once', async () => {
  const wallet = await DirectSecp256k1Wallet.fromKey(new Uint8Array(32).fill(7), 'wasm');
  const [{ address }] = await wallet.getAccounts();
  const contract = toBech32('wasm', new Uint8Array(32).fill(2));
  for (const operation of ['execute', 'upload', 'instantiate']) {
    const client = await SigningCosmWasmClient.offline(wallet);
    let submissions = 0;
    client.signAndBroadcast = async function(sender, messages) {
      assert.equal(this, client);
      assert.equal(sender, address);
      assert.equal(messages.length, 1);
      assert.equal(messages[0].typeUrl, operation === 'execute' ? '/cosmwasm.wasm.v1.MsgExecuteContract'
        : operation === 'upload' ? '/cosmwasm.wasm.v1.MsgStoreCode' : '/cosmwasm.wasm.v1.MsgInstantiateContract');
      submissions++;
      return { transactionHash: hash, height: 42, code: 5, rawLog: 'untrusted sensitive log' };
    };
    guardCosmosSigningClient(client);
    const fee = { amount: [], gas: '1' };
    const submit = operation === 'execute' ? () => client.execute(address, contract, { put: { key: 'owned', value: 'value' } }, fee)
      : operation === 'upload' ? () => client.upload(address, new Uint8Array([0,97,115,109,1,0,0,0]), fee)
      : () => client.instantiate(address, 1, {}, 'controlled', fee);
    await assert.rejects(submit(), error => {
      assert.deepEqual(cosmosErrorEnvelope(error).nativeMutation, { transactionHash: hash, executionSucceeded: false });
      assert.ok(!error.message.includes('sensitive'));
      return true;
    });
    assert.equal(submissions, 1);
  }
});

test('signing boundary retains successful real SDK response and ordinary preflight errors unchanged', async () => {
  const receipt = { transactionHash: hash, height: 42, code: 0 };
  const client = { async broadcastTx() { assert.fail('Receipt-only fixture does not broadcast'); }, async signAndBroadcast(...args) { assert.deepEqual(args, ['sender', [], 'auto', 'memo']); return receipt; } };
  guardCosmosSigningClient(client);
  assert.equal(await client.signAndBroadcast('sender', [], 'auto', 'memo'), receipt);
  assert.deepEqual(cosmosErrorEnvelope(new Error('ordinary validation error')), { ok: false, error: 'ordinary validation error' });
});

test('native timeout and malformed delivery evidence never manufacture execution status or identity', () => {
  assert.deepEqual(cosmosErrorEnvelope(new TimeoutError('untrusted text', hash)).nativeMutation, { transactionHash: hash, executionSucceeded: null });
  assert.deepEqual(cosmosErrorEnvelope(new TimeoutError('untrusted text', 'invalid')).nativeMutation, { transactionHash: null, executionSucceeded: null });
  assert.ok(!cosmosErrorEnvelope(new TimeoutError('untrusted text', 'invalid')).error.includes('untrusted'));
  for (const receipt of [{ code: 5, transactionHash: hash }, { code: '0', transactionHash: hash, height: 42 }, { code: 4294967296, transactionHash: hash, height: 42 }])
    assert.throws(() => verifiedBankTransfer(receipt), error => cosmosErrorEnvelope(error).nativeMutation.executionSucceeded === null);
});
test('Cosmos history binds both indexes to one committed height and reconciles overlapping identities', async () => {
  const address = toBech32('wasm', new Uint8Array(20).fill(1));
  const bytes = new Uint8Array([1,2]);
  const tx = {hash:createHash('sha256').update(bytes).digest('hex').toUpperCase(),height:42,txIndex:0,code:0,tx:bytes};
  const queries = [];
  const client = {getHeight:async()=>42,searchTx:async query=>{queries.push(query);return [tx];}};
  assert.deepEqual(await indexedHistorySnapshot(client,address),[tx]);
  assert.equal(queries.length,2);
  assert.ok(queries.every(query=>query.endsWith(' AND tx.height<=42')));
  assert.ok(queries[0].startsWith('transfer.recipient='));
  assert.ok(queries[1].startsWith('message.sender='));
});
test('Cosmos history rejects out-of-snapshot and conflicting indexed receipts', async () => {
  const address = toBech32('wasm', new Uint8Array(20).fill(1));
  const bytes = new Uint8Array([1]);
  const tx = {hash:createHash('sha256').update(bytes).digest('hex').toUpperCase(),height:42,txIndex:0,code:0,tx:bytes};
  for (const changes of [{height:43},{height:0},{hash:'wrong'},{hash:'AB'.repeat(32)},{tx:new Uint8Array([2])},{txIndex:-1},{code:-1}])
    await assert.rejects(indexedHistorySnapshot({getHeight:async()=>42,searchTx:async()=>[{...tx,...changes}]},address),/snapshot/);
  let count = 0;
  await assert.rejects(indexedHistorySnapshot({getHeight:async()=>42,
    searchTx:async()=>[++count===1?tx:{...tx,code:5}]},address),/disagree/);
});
function feeTx({ code = 0, granter = '', payer = '', amount = '9007199254740993', observedAmount = amount } = {}) {
  const address = toBech32('wasm', new Uint8Array(20).fill(1));
  const authInfoBytes = AuthInfo.encode(AuthInfo.fromPartial({ fee: { amount: [{ denom: 'stake', amount }], gasLimit: 50n, payer, granter } })).finish();
  const tx = TxRaw.encode(TxRaw.fromPartial({ bodyBytes: new Registry().encodeTxBody({ messages: [], memo: '' }), authInfoBytes })).finish();
  return { hash: createHash('sha256').update(tx).digest('hex').toUpperCase(), height: 42, code, gasUsed: 30n, gasWanted: 50n,
    tx,
    events: [{ type: 'tx', attributes: [{ key: 'fee', value: observedAmount + 'stake' }, { key: 'fee_payer', value: granter || payer || address }] }] };
}
test('observed Cosmos fee deduction retains exact coins and charged failure without guessing payer', () => {
  const tx = feeTx({ code: 5 });
  const hash = tx.hash;
  const receipt = cosmosNativeFeeReceipt(tx, hash);
  assert.equal(receipt.success, false); assert.equal(receipt.executionCode, 5);
  assert.equal(receipt.fees[0].amount, '9007199254740993');
  assert.equal(receipt.gasUsed, '30'); assert.equal(receipt.basis, 'observed-ante-fee-deduction');
});

test('fee evidence preserves exact uint256 boundary and explicit zero without accepting overflow',()=>{
 for(const amount of ['0',((1n<<256n)-1n).toString()]){
  const tx=feeTx({amount});
  assert.equal(cosmosNativeFeeReceipt(tx,tx.hash).fees[0].amount,amount);
 }
 const overflow=feeTx({amount:(1n<<256n).toString()});
 assert.throws(()=>cosmosNativeFeeReceipt(overflow,overflow.hash),/Malformed exact Cosmos fee coin/);
});

test('duplicate observed fee denominations never become billable or zero evidence',()=>{
 const tx=feeTx({amount:'1'});
 tx.events[0].attributes[0].value='1stake,1stake';
 assert.throws(()=>cosmosNativeFeeReceipt(tx,tx.hash),/Duplicate Cosmos fee denominations/);
});
test('Cosmos observed fee granter remains the charged account', () => {
  const granter = toBech32('wasm', new Uint8Array(20).fill(2));
  const tx = feeTx({ granter });
  const hash = tx.hash;
  assert.equal(cosmosNativeFeeReceipt(tx, hash).payer, granter);
  const altered = structuredClone(tx); altered.events[0].attributes[1].value = toBech32('wasm', new Uint8Array(20).fill(3));
  assert.throws(() => cosmosNativeFeeReceipt(altered, hash));
});
test('missing ambiguous or mismatched observed fee evidence is never converted to zero', () => {
  const tx = feeTx();
  const hash = tx.hash;
  assert.throws(() => cosmosNativeFeeReceipt({ ...tx, events: [] }, hash));
  assert.throws(() => cosmosNativeFeeReceipt({ ...tx, events: [...tx.events, ...tx.events] }, hash));
  assert.throws(() => cosmosNativeFeeReceipt(feeTx({ observedAmount: '1' }), hash));
  assert.throws(() => cosmosNativeFeeReceipt({ ...tx, hash: 'fabricated' }, hash));
  for (const amount of ['01', '-1', '1.5']) {
    const malformed = feeTx({ amount });
    assert.throws(() => cosmosNativeFeeReceipt(malformed, malformed.hash));
  }
});
test('Cosmos fee attribution rejects valid-format substituted hashes and signed bytes', () => {
  const tx = feeTx();
  assert.doesNotThrow(() => cosmosNativeFeeReceipt(tx, tx.hash));
  assert.throws(() => cosmosNativeFeeReceipt({...tx,hash:'AB'.repeat(32)}, 'AB'.repeat(32)), /signed transaction bytes/);
  assert.throws(() => cosmosNativeFeeReceipt({...tx,tx:feeTx({amount:'1'}).tx}, tx.hash), /signed transaction bytes/);
});
test('custody proof binds exact successful receipt, collection, token and pool', () => {
  const registry = new Registry([...defaultRegistryTypes, ...wasmTypes]);
  const expected = { transactionHash: hash, contractAddress: 'collection', tokenId: 'token', custody: 'pool' };
  const message = { typeUrl: '/cosmwasm.wasm.v1.MsgExecuteContract', value: {
    sender: 'owner', contract: 'collection', msg: new TextEncoder().encode(JSON.stringify({ asset: { message: { nft_transfer: { token_id: 'token', recipient: 'pool' } } } })), funds: [],
  } };
  function receipt(messages) {
    const body = registry.encodeTxBody({ messages, memo: '' });
    let length = body.length;
    const size = [];
    do { const byte = length & 127; length >>>= 7; size.push(byte | (length ? 128 : 0)); } while (length);
    // TxRaw fixture: official encoded TxBody plus empty AuthInfo; no signing or broadcast.
    const tx = Uint8Array.from([10, ...size, ...body, 18, 0]);
    return { hash:createHash('sha256').update(tx).digest('hex').toUpperCase(), code: 0, height: 42, tx };
  }
  const tx = receipt([message]);
  expected.transactionHash = tx.hash;
  assert.equal(verifiedNftCustodyReceipt(tx, expected, registry), tx);
  assert.throws(() => verifiedNftCustodyReceipt({...tx,hash:'AB'.repeat(32)}, {...expected,transactionHash:'AB'.repeat(32)}, registry));
  assert.throws(() => verifiedNftCustodyReceipt({...tx,tx:receipt([]).tx}, expected, registry));
  for (const field of ['contractAddress', 'tokenId', 'custody', 'transactionHash'])
    assert.throws(() => verifiedNftCustodyReceipt(tx, { ...expected, [field]: 'other' }, registry));
  assert.throws(() => verifiedNftCustodyReceipt({ ...tx, code: 5 }, expected, registry));
  assert.throws(() => verifiedNftCustodyReceipt(null, expected, registry));
  function rejectMessages(messages) {
    const invalid=receipt(messages);
    assert.throws(() => verifiedNftCustodyReceipt(invalid, {...expected,transactionHash:invalid.hash}, registry));
  }
  rejectMessages([message, message]);
  rejectMessages([{ ...message, value: { ...message.value, sender: 'pool' } }]);
  rejectMessages([{ ...message, value: { ...message.value,
    msg: new TextEncoder().encode(JSON.stringify({ nft_transfer: { token_id: 'token', recipient: 'pool' } })),
  } }]);
});
test('official protobuf multi-send decoding preserves each actual output and exact units', () => {
  const registry = new Registry(defaultRegistryTypes);
  const typeUrl = '/cosmos.bank.v1beta1.MsgMultiSend';
  const value = {inputs:[{address:'sender',coins:[{denom:'uatom',amount:'9007199254740994'}]}],outputs:[
    {address:'one',coins:[{denom:'uatom',amount:'9007199254740993'}]},
    {address:'two',coins:[{denom:'uatom',amount:'1'}]}]};
  const legs = bankTransferLegs(registry.encodeAsAny({typeUrl,value}),registry);
  assert.deepEqual(legs.map(leg => [leg.from,leg.to,leg.transferIndex,leg.coins[0].amount]),
    [['sender','one',0,'9007199254740993'],['sender','two',1,'1']]);
  assert.throws(() => bankTransferLegs(registry.encodeAsAny({typeUrl,value:{...value,inputs:[...value.inputs,...value.inputs]}}),registry), /ambiguous/);
});
test('upload requires exact positive code ID and retains original digest on invalid identity', () => {
  const result = { transactionHash:hash,height:42,codeId:7 };
  assert.equal(verifiedUpload(result),result);
  for (const codeId of [undefined,0,-1,'7',1.5,Number.MAX_SAFE_INTEGER+1])
    assert.throws(() => verifiedUpload({...result,codeId}), error => error.message.includes(hash));
});
test('instantiation validates actual bech32 checksum and configured prefix without losing receipt', () => {
  const result = {transactionHash:hash,height:42,contractAddress:toBech32('wasm',new Uint8Array(32).fill(1))};
  assert.equal(verifiedInstantiation(result,'wasm'),result);
  assert.throws(() => verifiedInstantiation(result,'cosmos'), error => error.message.includes(hash));
  for (const contractAddress of [undefined,'fabricated',result.contractAddress.slice(0,-1)+'!'])
    assert.throws(() => verifiedInstantiation({...result,contractAddress},'wasm'), error => error.message.includes(hash));
});
test('contract execution preserves verified SDK hash and committed height', () => {
  const result = { transactionHash: hash, height: 42 };
  assert.equal(verifiedContractExecution(result), result);
});
for (const height of [undefined, 0, -1, '42', 1.5, Number.MAX_SAFE_INTEGER + 1])
  test(`contract execution rejects invalid height ${height} while retaining hash`, () =>
    assert.throws(() => verifiedContractExecution({transactionHash:hash,height}), error => error.message.includes(hash) && error.message.includes('before retrying')));
test('contract execution rejects missing or malformed hash', () => {
  for (const result of [null, {height:42}, {transactionHash:'fabricated',height:42}])
    assert.throws(() => verifiedContractExecution(result), /Reconcile submission/);
});
test('official SDK timeout retains identity without claiming failed or successful execution', () => {
  const message = cosmosFailureMessage(new TimeoutError('untrusted timeout message', hash));
  assert.ok(message.includes(hash)); assert.ok(message.includes('outcome is not verified'));
  assert.ok(message.includes('before retrying')); assert.ok(!message.includes('untrusted'));
});
test('ordinary errors with a txId property are not reclassified as verified SDK timeouts', () => {
  const ordinary = Object.assign(new Error('original validation error'), { txId: hash });
  assert.equal(cosmosFailureMessage(ordinary), 'original validation error');
});
test('bank transfer failure retains original committed hash and code without raw log', () => {
  assert.throws(() => verifiedBankTransfer({ code: 5, transactionHash: hash, rawLog: 'sensitive untrusted text' }), error =>
    error.message.includes(hash) && error.message.includes('code 5') && error.message.includes('before retrying') && !error.message.includes('sensitive'));
});
test('bank transfer accepts exact successful execution evidence unchanged', () => {
  const receipt = { code: 0, transactionHash: hash, height: 42 };
  assert.equal(verifiedBankTransfer(receipt), receipt);
});
for (const height of [undefined,0,-1,'42',1.5,Number.MAX_SAFE_INTEGER+1])
  test(`bank transfer successful code still requires committed height ${height}`, () =>
    assert.throws(() => verifiedBankTransfer({code:0,transactionHash:hash,height}), error => error.message.includes(hash) && error.message.includes('before retrying')));
for (const result of [null, { code: '0', transactionHash: hash }, { code: -1, transactionHash: hash }, { code: 0, transactionHash: 'fabricated' }])
  test(`bank transfer rejects malformed evidence ${JSON.stringify(result)}`, () => assert.throws(() => verifiedBankTransfer(result), /Reconcile submission/));
