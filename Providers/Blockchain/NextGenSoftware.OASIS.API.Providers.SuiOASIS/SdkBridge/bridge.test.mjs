import { test } from 'node:test';
import assert from 'node:assert/strict';
import { invoke } from './bridge.mjs';
import { Ed25519Keypair } from '@mysten/sui/keypairs/ed25519';

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
