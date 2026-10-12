import { test } from 'node:test';
import assert from 'node:assert/strict';
import { invoke, publicationArtifacts, verifiedHistoryTransaction, nativeFeeReceipt } from './bridge.mjs';
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
