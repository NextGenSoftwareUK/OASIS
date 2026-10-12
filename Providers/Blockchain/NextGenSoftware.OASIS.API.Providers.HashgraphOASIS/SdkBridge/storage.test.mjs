import test from 'node:test';
import assert from 'node:assert/strict';
import { Interface } from 'ethers';
import { storageReadCall, storageTransaction, validateStoragePage, execute } from './bridge.mjs';
import { PrivateKey, ContractExecuteTransaction, Transaction, TransactionId, AccountId } from '@hiero-ledger/sdk';

const abi = new Interface([
  'function put(string key,string value)',
  'function remove(string key)',
  'function get(string key) view returns(bool,string)',
  'function revision() view returns(uint256)',
  'function keyPage(uint256,uint256,uint256) view returns(string[],uint256,uint256)'
]);

test('storage mutations retain contract, gas and exact ABI payload through SDK serialization', () => {
  for (const [action, name] of [['storagePut', 'put'], ['storageDelete', 'remove']]) {
    const tx = storageTransaction(action, { contractId: '0.0.123', key: 'avatar/é', value: '', gas: 100000 })
      .setTransactionId(TransactionId.fromString('0.0.1001@1700000000.000000001'))
      .setNodeAccountIds([AccountId.fromString('0.0.3')]).freeze();
    const decoded = Transaction.fromBytes(tx.toBytes());
    assert.ok(decoded instanceof ContractExecuteTransaction);
    assert.equal(decoded.contractId.toString(), '0.0.123');
    assert.equal(decoded.gas.toString(), '100000');
    const args = abi.decodeFunctionData(name, decoded.functionParameters);
    assert.equal(args[0], 'avatar/é');
    if (name === 'put') assert.equal(args[1], '');
  }
});

test('storage mutations reject absent values, empty keys and invalid gas', () => {
  const request = { contractId: '0.0.123', key: 'key', gas: 100000 };
  assert.throws(() => storageTransaction('storagePut', request), /value/);
  assert.throws(() => storageTransaction('storageDelete', { ...request, key: '' }), /nonempty/);
  assert.throws(() => storageTransaction('storageDelete', { ...request, gas: 1.5 }), /gas/);
});

test('signed storage writes preserve payer identity, fee ceiling and disabled ID regeneration', async () => {
  const key = PrivateKey.generateED25519();
  const id = TransactionId.fromString('0.0.1001@1700000000.000000001');
  const tx = storageTransaction('storagePut', { contractId: '0.0.123', key: 'key', value: 'é record', gas: 100000 })
    .setTransactionId(id).setMaxTransactionFee(1).setMaxAttempts(1).setRegenerateTransactionId(false)
    .setNodeAccountIds([AccountId.fromString('0.0.3')]).freeze();
  await tx.sign(key);
  assert.equal(tx.maxAttempts, 1);
  assert.equal(tx.regenerateTransactionId, false);
  const decoded = Transaction.fromBytes(tx.toBytes());
  assert.equal(decoded.transactionId.toString(), id.toString());
  assert.equal(decoded.maxTransactionFee.toTinybars().toString(), '100000000');
  assert.equal(abi.decodeFunctionData('put', decoded.functionParameters)[1], 'é record');
  assert.equal(decoded.getSignatures().size, 1);
});

test('storage signing-key mismatch fails before opening a funded client', async () => {
  const key = PrivateKey.generateED25519();
  await assert.rejects(execute('storageDelete', {
    contractId: '0.0.123', key: 'key', gas: 100000,
    privateKey: key.toStringDer(), keyType: 'der',
    publicKey: PrivateKey.generateED25519().publicKey.toStringDer()
  }), /signing key mismatch/);
});

test('storage payer and fee validation occurs before funded network use', async () => {
  const key = PrivateKey.generateED25519();
  const request = { contractId: '0.0.123', key: 'key', value: 'record', gas: 100000,
    privateKey: key.toStringDer(), keyType: 'der', sender: '0.0.1001', network: 'testnet',
    transactionId: '0.0.1002@1700000000.000000001', maxFeeTinybars: '1' };
  await assert.rejects(execute('storagePut', request), /payer mismatch/);
  request.transactionId = '0.0.1001@1700000000.000000001';
  delete request.maxFeeTinybars;
  await assert.rejects(execute('storagePut', request), /fee ceiling/);
});

test('pagination rejects nonprogressing, changed-total and out-of-range results', () => {
  assert.throws(() => validateStoragePage([[], 0n, 1n], 0n, null));
  assert.throws(() => validateStoragePage([['key'], 1n, 2n], 0n, 3n));
  assert.throws(() => validateStoragePage([['key'], 2n, 1n], 0n, null));
  assert.doesNotThrow(() => validateStoragePage([[], 0n, 0n], 0n, null));
});

test('storage get encodes the exact Unicode key through standard ABI', () => {
  assert.equal(abi.decodeFunctionData('get', storageReadCall('get', ['avatar/é']))[0], 'avatar/é');
});

test('storage pagination retains revisions and cursors above Number precision', () => {
  const revision = 9007199254740993n;
  const decoded = abi.decodeFunctionData('keyPage', storageReadCall('keyPage', [revision, revision, 256]));
  assert.equal(decoded[0], revision);
  assert.equal(decoded[1], revision);
  assert.equal(decoded[2], 256n);
});

test('storage read rejects mutation selectors rather than guessing an ABI', () => {
  assert.throws(() => storageReadCall('put', ['key', 'value']), /Unknown storage read/);
});

test('storage query configuration fails before a funded consensus client is opened', async () => {
  await assert.rejects(execute('storageGet', { contractId: 'invalid' }));
  await assert.rejects(execute('storageGet', { contractId: '0.0.123', key: 'key', gas: 0 }), /gas limit/);
  await assert.rejects(execute('storageList', { contractId: '0.0.123', prefix: '', gas: 100000 }), /fee ceiling/);
});
test('storage owner probe uses the deployed immutable-owner ABI',()=>{
 const ownerAbi=new Interface(['function owner() view returns(address)']);
 assert.equal(ownerAbi.parseTransaction({data:storageReadCall('owner',[])}).name,'owner');
});
test('storage probe requires bounded consensus query configuration',async()=>{
 await assert.rejects(execute('storageProbe',{contractId:'0.0.123',gas:0}),/gas limit/);
 await assert.rejects(execute('storageProbe',{contractId:'0.0.123',gas:100000}),/fee ceiling/);
});
