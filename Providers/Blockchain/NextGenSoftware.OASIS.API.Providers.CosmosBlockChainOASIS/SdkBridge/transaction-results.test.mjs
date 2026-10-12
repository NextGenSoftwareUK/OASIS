import test from 'node:test';
import assert from 'node:assert/strict';
import { verifiedBankTransfer, verifiedContractExecution, verifiedUpload, verifiedInstantiation, verifiedNftCustodyReceipt, cosmosNativeFeeReceipt, bankTransferLegs, cosmosFailureMessage, cosmosErrorEnvelope, guardCosmosSigningClient, indexedHistorySnapshot } from './transaction-results.mjs';
import { SigningCosmWasmClient } from '@cosmjs/cosmwasm';
import { DirectSecp256k1Wallet } from '@cosmjs/proto-signing';
import { TxRaw, AuthInfo } from 'cosmjs-types/cosmos/tx/v1beta1/tx';
import { wasmTypes } from '@cosmjs/cosmwasm';
import { Registry } from '@cosmjs/proto-signing';
import { defaultRegistryTypes } from '@cosmjs/stargate';
import { toBech32 } from '@cosmjs/encoding';
import { TimeoutError } from '@cosmjs/stargate';
const hash = 'ab'.repeat(32);

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
  const client = { async signAndBroadcast(...args) { assert.deepEqual(args, ['sender', [], 'auto', 'memo']); return receipt; } };
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
  const tx = {hash:hash.toUpperCase(),height:42,txIndex:0,code:0,tx:new Uint8Array([1,2])};
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
  const tx = {hash:hash.toUpperCase(),height:42,txIndex:0,code:0,tx:new Uint8Array([1])};
  for (const changes of [{height:43},{height:0},{hash:'wrong'},{txIndex:-1},{code:-1}])
    await assert.rejects(indexedHistorySnapshot({getHeight:async()=>42,searchTx:async()=>[{...tx,...changes}]},address),/snapshot/);
  let count = 0;
  await assert.rejects(indexedHistorySnapshot({getHeight:async()=>42,
    searchTx:async()=>[++count===1?tx:{...tx,tx:new Uint8Array([2])}]},address),/disagree/);
});
function feeTx({ code = 0, granter = '', payer = '', amount = '9007199254740993', observedAmount = amount } = {}) {
  const address = toBech32('wasm', new Uint8Array(20).fill(1));
  const authInfoBytes = AuthInfo.encode(AuthInfo.fromPartial({ fee: { amount: [{ denom: 'stake', amount }], gasLimit: 50n, payer, granter } })).finish();
  return { hash: hash.toUpperCase(), height: 42, code, gasUsed: 30n, gasWanted: 50n,
    tx: TxRaw.encode(TxRaw.fromPartial({ bodyBytes: new Registry().encodeTxBody({ messages: [], memo: '' }), authInfoBytes })).finish(),
    events: [{ type: 'tx', attributes: [{ key: 'fee', value: observedAmount + 'stake' }, { key: 'fee_payer', value: granter || payer || address }] }] };
}
test('observed Cosmos fee deduction retains exact coins and charged failure without guessing payer', () => {
  const tx = feeTx({ code: 5 });
  const receipt = cosmosNativeFeeReceipt(tx, hash);
  assert.equal(receipt.success, false); assert.equal(receipt.executionCode, 5);
  assert.equal(receipt.fees[0].amount, '9007199254740993');
  assert.equal(receipt.gasUsed, '30'); assert.equal(receipt.basis, 'observed-ante-fee-deduction');
});
test('Cosmos observed fee granter remains the charged account', () => {
  const granter = toBech32('wasm', new Uint8Array(20).fill(2));
  const tx = feeTx({ granter });
  assert.equal(cosmosNativeFeeReceipt(tx, hash).payer, granter);
  const altered = structuredClone(tx); altered.events[0].attributes[1].value = toBech32('wasm', new Uint8Array(20).fill(3));
  assert.throws(() => cosmosNativeFeeReceipt(altered, hash));
});
test('missing ambiguous or mismatched observed fee evidence is never converted to zero', () => {
  const tx = feeTx();
  assert.throws(() => cosmosNativeFeeReceipt({ ...tx, events: [] }, hash));
  assert.throws(() => cosmosNativeFeeReceipt({ ...tx, events: [...tx.events, ...tx.events] }, hash));
  assert.throws(() => cosmosNativeFeeReceipt(feeTx({ observedAmount: '1' }), hash));
  assert.throws(() => cosmosNativeFeeReceipt({ ...tx, hash: 'fabricated' }, hash));
  for (const amount of ['01', '-1', '1.5']) assert.throws(() => cosmosNativeFeeReceipt(feeTx({ amount }), hash));
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
    return { hash, code: 0, height: 42, tx: Uint8Array.from([10, ...size, ...body, 18, 0]) };
  }
  const tx = receipt([message]);
  assert.equal(verifiedNftCustodyReceipt(tx, expected, registry), tx);
  for (const field of ['contractAddress', 'tokenId', 'custody', 'transactionHash'])
    assert.throws(() => verifiedNftCustodyReceipt(tx, { ...expected, [field]: 'other' }, registry));
  assert.throws(() => verifiedNftCustodyReceipt({ ...tx, code: 5 }, expected, registry));
  assert.throws(() => verifiedNftCustodyReceipt(null, expected, registry));
  assert.throws(() => verifiedNftCustodyReceipt(receipt([message, message]), expected, registry));
  assert.throws(() => verifiedNftCustodyReceipt(receipt([{ ...message, value: { ...message.value, sender: 'pool' } }]), expected, registry));
  assert.throws(() => verifiedNftCustodyReceipt(receipt([{ ...message, value: { ...message.value,
    msg: new TextEncoder().encode(JSON.stringify({ nft_transfer: { token_id: 'token', recipient: 'pool' } })),
  } }]), expected, registry));
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
