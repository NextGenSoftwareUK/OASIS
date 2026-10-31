import test from 'node:test';
import assert from 'node:assert/strict';
import { PrivateKey, FileCreateTransaction, FileUpdateTransaction, FileDeleteTransaction, Transaction, TransactionId, AccountId } from '@hiero-ledger/sdk';
import { fileTransaction, execute } from './bridge.mjs';

test('file mutations use SDK file classes with explicit contents and identity', () => {
  const key = PrivateKey.generateED25519().publicKey;
  const create = fileTransaction('fileCreate', { contents: 'record' }, key);
  assert.ok(create instanceof FileCreateTransaction);
  assert.equal(Buffer.from(create.contents).toString(), 'record');
  const update = fileTransaction('fileUpdate', { fileId: '0.0.123', contents: 'changed' }, key);
  assert.equal(update.fileId.toString(), '0.0.123');
  assert.equal(Buffer.from(update.contents).toString(), 'changed');
  assert.ok(fileTransaction('fileDelete', { fileId: '0.0.123' }, key) instanceof FileDeleteTransaction);
});

test('file create retains contents and access key in serialized SDK transaction', () => {
  const key = PrivateKey.generateED25519().publicKey;
  const tx = fileTransaction('fileCreate', { contents: 'persistent record' }, key)
    .setTransactionId(TransactionId.fromString('0.0.1001@1700000000.000000001'))
    .setNodeAccountIds([AccountId.fromString('0.0.3')]).freeze();
  const decoded = Transaction.fromBytes(tx.toBytes());
  assert.equal(Buffer.from(decoded.contents).toString(), 'persistent record');
  assert.equal(decoded.keys[0].toStringDer(), key.toStringDer());
});

test('file signing-key mismatch is rejected before any paid query or submission', async () => {
  const key = PrivateKey.generateED25519();
  await assert.rejects(execute('fileRead', {
    privateKey: key.toStringDer(), keyType: 'der',
    publicKey: PrivateKey.generateED25519().publicKey.toStringDer(),
    sender: '0.0.1001', fileId: '0.0.123', network: 'testnet', maxFeeTinybars: '100000000'
  }), /does not match/);
});

test('file update preserves exact contents and file identity through SDK serialization', () => {
  const key = PrivateKey.generateED25519().publicKey;
  const tx = fileTransaction('fileUpdate', { contents: 'updated é record', fileId: '0.0.123' }, key)
    .setTransactionId(TransactionId.fromString('0.0.1001@1700000000.000000001'))
    .setNodeAccountIds([AccountId.fromString('0.0.3')]).freeze();
  const decoded = Transaction.fromBytes(tx.toBytes());
  assert.ok(decoded instanceof FileUpdateTransaction);
  assert.equal(decoded.fileId.toString(), '0.0.123');
  assert.equal(Buffer.from(decoded.contents).toString(), 'updated é record');
});

test('file payer mismatch and absent fee ceiling fail before opening a consensus client', async () => {
  const key = PrivateKey.generateED25519();
  const request = { privateKey: key.toStringDer(), keyType: 'der', sender: '0.0.1001',
    contents: 'record', network: 'testnet', transactionId: '0.0.1002@1700000000.000000001', maxFeeTinybars: '1' };
  await assert.rejects(execute('fileCreate', request), /payer mismatch/);
  request.transactionId = '0.0.1001@1700000000.000000001';
  delete request.maxFeeTinybars;
  await assert.rejects(execute('fileCreate', request), /fee ceiling/);
});

test('file payload bounds are UTF-8 bytes and do not silently truncate', () => {
  const key = PrivateKey.generateED25519().publicKey;
  assert.throws(() => fileTransaction('fileCreate', { contents: 'é'.repeat(1025) }, key));
  assert.throws(() => fileTransaction('fileCreate', {}, key));
  assert.throws(() => fileTransaction('fileUpdate', { fileId: 'invalid', contents: '' }, key));
});
