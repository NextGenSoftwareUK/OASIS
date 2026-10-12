import { test } from 'node:test';
import assert from 'node:assert/strict';
import { invoke, execute, publicationArtifacts, verifiedHistoryTransaction, nativeFeeReceipt, historyScanComplete } from './bridge.mjs';
import { Transaction } from '@mysten/sui/transactions';

test('execution readback failure retains original digest and never resubmits', async () => {
  const signer = Ed25519Keypair.fromSecretKey(new Uint8Array(32).fill(7));
  const readbackError = new Error('controlled index transport failure');
  let submissions = 0, reads = 0;
  const client = {
    async signAndExecuteTransaction(request) {
      submissions++;
      assert.equal(request.signer, signer);
      assert.equal(request.include.effects, true);
      return { Transaction: { digest: 'original-executed-digest', status: { success: true } } };
    },
    async waitForTransaction(request) {
      reads++;
      assert.equal(request.digest, 'original-executed-digest');
      throw readbackError;
    }
  };
  await assert.rejects(execute(client, new Transaction(), signer), error => {
    assert.match(error.message, /original-executed-digest/);
    assert.match(error.message, /Reconcile this transaction before any retry/);
    assert.equal(error.cause, readbackError);
    return true;
  });
  assert.equal(submissions, 1);
  assert.equal(reads, 1);
});

test('failed execution retains original failure and does not wait or resubmit', async () => {
  const signer = Ed25519Keypair.fromSecretKey(new Uint8Array(32).fill(7));
  let submissions = 0;
  const client = {
    async signAndExecuteTransaction() {
      submissions++;
      return { FailedTransaction: { digest: 'original-failed-digest', status: { success: false, error: 'controlled Move abort' } } };
    },
    async waitForTransaction() { assert.fail('Failed execution must not enter success readback'); }
  };
  await assert.rejects(execute(client, new Transaction(), signer), /original-failed-digest.*controlled Move abort/);
  assert.equal(submissions, 1);
});

test('successful execution waits on and returns the original transaction once', async () => {
  const signer = Ed25519Keypair.fromSecretKey(new Uint8Array(32).fill(7));
  const transaction = { digest: 'original-success-digest', status: { success: true } };
  let submissions = 0, reads = 0;
  const client = {
    async signAndExecuteTransaction() { submissions++; return { Transaction: transaction }; },
    async waitForTransaction(request) { reads++; assert.equal(request.digest, transaction.digest); }
  };
  assert.equal(await execute(client, new Transaction(), signer), transaction);
  assert.equal(submissions, 1);
  assert.equal(reads, 1);
});

test('history completion requires index coverage of the fixed committed checkpoint', () => {
  for (const reason of [3, 5]) {
    assert.equal(historyScanComplete(reason, 42n, 42n), true);
    assert.throws(() => historyScanComplete(reason, 41n, 42n), /not covered/);
    assert.throws(() => historyScanComplete(reason, undefined, 42n), /not covered/);
  }
  for (const reason of [1, 2]) assert.equal(historyScanComplete(reason, 41n, 42n), false);
  for (const reason of [0, 4, 6, undefined])
    assert.throws(() => historyScanComplete(reason, 42n, 42n), /unexpected/);
});
import { Ed25519Keypair } from '@mysten/sui/keypairs/ed25519';

test('fee receipts retain exact charged failure, payer and signed rebate without double counting', () => {
  const payer = '0x' + 'a'.repeat(64);
  const gasUsed = { computationCost: '9007199254740993', storageCost: '7', storageRebate: '3', nonRefundableStorageFee: '2' };
  const tx = { digest: 'original', checkpoint: '42', timestampMs: 1, status: { success: false }, transaction: { gasData: { owner: payer } },
    effects: { transactionDigest: 'original', gasUsed } };
  const receipt = nativeFeeReceipt(tx, 'original');
  assert.equal(receipt.success, false); assert.equal(receipt.payer, payer);
  assert.equal(receipt.netUnits, '9007199254740997');
  assert.equal(nativeFeeReceipt({ ...tx, effects: { ...tx.effects, gasUsed: { ...gasUsed, computationCost: '1', storageCost: '2', storageRebate: '7' } } }, 'original').netUnits, '-4');
  assert.throws(() => nativeFeeReceipt(tx, 'other'));
  assert.throws(() => nativeFeeReceipt({ ...tx, transaction: { gasData: { owner: null } } }, 'original'));
  assert.throws(() => nativeFeeReceipt({ ...tx, checkpoint: null }, 'original'));
  for (const value of ['-1', '01', '1.5', '18446744073709551616', 7])
    assert.throws(() => nativeFeeReceipt({ ...tx, effects: { ...tx.effects, gasUsed: { ...gasUsed, storageCost: value } } }, 'original'));
});

test('history requires original digest and explicit outcome while preserving failed execution', () => {
  const tx = {digest:'original',status:{success:false},transaction:{sender:'0x'+'1'.repeat(64)},
    checkpoint:'42',timestampMs:1,effects:{transactionDigest:'original'},balanceChanges:[]};
  assert.equal(verifiedHistoryTransaction(tx,'original'),tx);
  assert.throws(() => verifiedHistoryTransaction(tx,'different'), /mismatched/);
  for (const checkpoint of [null, undefined, '-1', '1.5', '01', '18446744073709551616'])
    assert.throws(() => verifiedHistoryTransaction({...tx,checkpoint},'original'), /incomplete/);
  for (const timestampMs of [0, -1, 1.5, Number.MAX_SAFE_INTEGER + 1])
    assert.throws(() => verifiedHistoryTransaction({...tx,timestampMs},'original'), /incomplete/);
  assert.throws(() => verifiedHistoryTransaction({...tx,effects:{transactionDigest:'different'}},'original'), /incomplete/);
  for (const status of [undefined,{}, {success:'false'}])
    assert.throws(() => verifiedHistoryTransaction({...tx,status},'original'), /incomplete/);
  for (const timestampMs of [null,undefined])
    assert.throws(() => verifiedHistoryTransaction({...tx,timestampMs},'original'), /incomplete/);
});

test('publication parses Move objects without treating package as a struct tag', () => {
  const packageId = '0x' + 'a'.repeat(64);
  const receipt = { digest: 'synthetic-parser-receipt', effects: { changedObjects: [
    { objectId: packageId, outputState: 'PackageWrite', idOperation: 'Created' } ] }, objectTypes: {
    [packageId]: 'package', storage: `${packageId}::storage::Storage`,
    currency: `${packageId}::token::Currency`, registry: `0x2::coin_registry::Currency<${packageId}::token::TOKEN>` } };
  assert.equal(publicationArtifacts(receipt).packageAddress, packageId);
  assert.equal(publicationArtifacts(receipt).registrationId, 'registry');
  delete receipt.objectTypes.storage;
  assert.throws(() => publicationArtifacts(receipt), /required OASIS objects/);
});

test('official SDK generates a recoverable canonical Sui wallet', async () => {
  const generated = await invoke({ operation: 'generateKey' });
  assert.match(generated.privateKey, /^suiprivkey1/);
  assert.match(generated.address, /^0x[0-9a-f]{64}$/);
  const restored = await invoke({ operation: 'restoreKey', privateKey: generated.privateKey });
  assert.deepEqual(restored, generated);
  const key = Ed25519Keypair.fromSecretKey(generated.privateKey);
  const data = new TextEncoder().encode('OASIS actual Sui signature evidence');
  assert.equal(await key.getPublicKey().verify(data, await key.sign(data)), true);
});

test('invalid credentials fail without manufacturing an address', async () => {
  await assert.rejects(invoke({ operation: 'restoreKey', privateKey: 'mainnet' }));
});

test('BIP39 account recovery uses the SDK Sui derivation path', async () => {
  const account = await invoke({ operation: 'createAccount' });
  assert.equal(account.seedPhrase.split(' ').length, 24);
  const restored = await invoke({ operation: 'restoreAccount', seedPhrase: account.seedPhrase });
  assert.equal(restored.address, account.address);
  assert.equal(restored.privateKey, account.privateKey);
  await assert.rejects(invoke({ operation: 'restoreAccount', seedPhrase: 'not a mnemonic' }));
});

test('unreachable gRPC cannot produce a successful probe', async () => {
  await assert.rejects(invoke({ operation: 'probe', rpcEndpoint: 'http://127.0.0.1:1', network: 'localnet' }));
});

test('transfer rejects the wrong signer and invalid u64 MIST before contacting a node', async () => {
  const key = await invoke({ operation: 'generateKey' });
  const other = await invoke({ operation: 'generateKey' });
  const request = { operation: 'transfer', privateKey: key.privateKey, fromWalletAddress: key.address,
    toWalletAddress: other.address, rpcEndpoint: 'http://127.0.0.1:1', network: 'localnet', amountUnits: '1' };
  await assert.rejects(invoke({ ...request, fromWalletAddress: other.address }), /does not own/);
  await assert.rejects(invoke({ ...request, amountUnits: '0' }), /Positive integral/);
  await assert.rejects(invoke({ ...request, amountUnits: '1.1' }), /Positive integral/);
  await assert.rejects(invoke({ ...request, amountUnits: '18446744073709551616' }), /exceeds u64/);
});
