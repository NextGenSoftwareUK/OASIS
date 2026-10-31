import { test } from 'node:test';
import assert from 'node:assert/strict';
import { requestSuiFromFaucetV2 } from '@mysten/sui/faucet';
import { invoke } from './bridge.mjs';

const rpcEndpoint = process.env.OASIS_SUI_TEST_RPC;
const faucet = process.env.OASIS_SUI_TEST_FAUCET;
if (!rpcEndpoint || !faucet) throw new Error('Real local Sui RPC and faucet settings are required; no skipped or mocked evidence');
for (const endpoint of [rpcEndpoint, faucet]) {
  if (!['127.0.0.1', 'localhost', '[::1]'].includes(new URL(endpoint).hostname))
    throw new Error('This evidence fixture is local-only and must not spend public-chain assets');
}

test('official SDK signs a native transfer and verifies committed balance deltas on real Sui', { timeout: 120000 }, async () => {
  const sender = await invoke({ operation: 'generateKey' });
  const recipient = await invoke({ operation: 'generateKey' });
  await requestSuiFromFaucetV2({ host: faucet, recipient: sender.address });
  const base = { rpcEndpoint, network: 'localnet', privateKey: sender.privateKey };
  const probe = await invoke({ ...base, operation: 'probe' });
  assert.ok(probe.chainIdentifier);
  base.chainId = probe.chainIdentifier;
  const balance = async address => BigInt((await invoke({ ...base, operation: 'balance', walletAddress: address })).balance);
  const senderBefore = await balance(sender.address);
  const recipientBefore = await balance(recipient.address);
  assert.ok(senderBefore > 1000000n, 'Faucet must fund real spendable gas');
  const committed = await invoke({ ...base, operation: 'transfer', fromWalletAddress: sender.address,
    toWalletAddress: recipient.address, amountUnits: '1000000' });
  assert.ok(committed.transactionHash);
  assert.equal((await balance(recipient.address)) - recipientBefore, 1000000n);
  const receipt = await invoke({ ...base, operation: 'transaction', transactionHash: committed.transactionHash });
  assert.equal(receipt.status.success, true);
  assert.ok(BigInt(receipt.gasUsed.computationCost) > 0n);
  // Sui gas smashing can rebate deleted gas coins, making net gas negative.
  const gas = BigInt(receipt.gasUsed.computationCost) + BigInt(receipt.gasUsed.storageCost) - BigInt(receipt.gasUsed.storageRebate);
  assert.equal(senderBefore - await balance(sender.address), 1000000n + gas);
  await assert.rejects(invoke({ ...base, operation: 'probe', chainId: 'wrong-genesis' }), /chain identifier mismatch/);
  await assert.rejects(invoke({ ...base, operation: 'transfer', fromWalletAddress: recipient.address,
    toWalletAddress: sender.address, amountUnits: '1' }), /does not own sender/);
});
