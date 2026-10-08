import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync, writeFileSync, existsSync } from 'node:fs';
import { requestSuiFromFaucetV2 } from '@mysten/sui/faucet';
import { invoke } from './bridge.mjs';

const rpcEndpoint = process.env.OASIS_SUI_TEST_RPC;
const faucet = process.env.OASIS_SUI_TEST_FAUCET;
const deploymentPath = process.env.OASIS_SUI_TEST_DEPLOYMENT;
if (!rpcEndpoint || !faucet || !deploymentPath) throw new Error('Real local RPC, faucet and deployment are required');
for (const endpoint of [rpcEndpoint, faucet])
  if (!['127.0.0.1', 'localhost', '[::1]'].includes(new URL(endpoint).hostname)) throw new Error('Token tests cannot spend public assets');

test('standard Sui coin registration, issuer mint, transfer, escrow lock/unlock and holder burn commit real state', { timeout: 180000 }, async () => {
  const base = { ...JSON.parse(readFileSync(deploymentPath, 'utf8')), rpcEndpoint, network: 'localnet' };
  const issuer = await invoke({ operation: 'restoreKey', privateKey: base.privateKey });
  const walletPath = `${deploymentPath}.token-wallet.json`;
  let cached = existsSync(walletPath) ? JSON.parse(readFileSync(walletPath, 'utf8')) : null;
  if (!cached || cached.chainId !== base.chainId) {
    cached = { chainId: base.chainId, wallet: await invoke({ operation: 'generateKey' }) };
    writeFileSync(walletPath, JSON.stringify(cached), { mode: 0o600 });
  }
  const recipient = cached.wallet;
  if (BigInt((await invoke({ ...base, operation: 'balance', walletAddress: recipient.address })).balance) < 100000000n)
    await requestSuiFromFaucetV2({ host: faucet, recipient: recipient.address });
  const owned = { ...base, privateKey: recipient.privateKey, walletAddress: recipient.address };
  // The fixture issuer is synthetic and exclusively owned by this local test deployment.
  // Remove its leftovers from an interrupted prior run before measuring this run's supply delta.
  if (BigInt(await invoke({ ...base, operation: 'tokenBalance', walletAddress: issuer.address })) > 0n)
    await invoke({ ...base, operation: 'tokenBurn' });
  if (BigInt(await invoke({ ...owned, operation: 'tokenLockedBalance' })) > 0n)
    await invoke({ ...owned, operation: 'tokenUnlock' });
  if (BigInt(await invoke({ ...owned, operation: 'tokenBalance' })) > 0n)
    await invoke({ ...owned, operation: 'tokenBurn' });
  const initial = BigInt((await invoke({ ...base, operation: 'tokenProbe' })).treasury.supply);
  const metadata = await invoke({ ...base, operation: 'tokenMetadata' });
  assert.equal(metadata.symbol, 'OASIS');
  assert.equal(metadata.decimals, 9);
  try {
    const minted = await invoke({ ...base, operation: 'tokenMint', recipient: issuer.address, amountUnits: '1000' });
    assert.equal((await invoke({ ...base, operation: 'transaction', transactionHash: minted.transactionHash })).status.success, true);
    await assert.rejects(invoke({ ...owned, operation: 'tokenMint', recipient: recipient.address, amountUnits: '1' }), /MoveAbort/);
    await invoke({ ...base, operation: 'tokenSend', recipient: recipient.address, amountUnits: '400' });
    assert.equal(await invoke({ ...owned, operation: 'tokenBalance' }), '400');
    await invoke({ ...owned, operation: 'tokenLock' });
    assert.equal(await invoke({ ...owned, operation: 'tokenBalance' }), '0');
    assert.equal(await invoke({ ...owned, operation: 'tokenLockedBalance' }), '400');
    await assert.rejects(invoke({ ...owned, operation: 'tokenSend', recipient: issuer.address, amountUnits: '1' }), /No spendable/);
    await invoke({ ...owned, operation: 'tokenUnlock' });
    assert.equal(await invoke({ ...owned, operation: 'tokenBalance' }), '400');
    assert.equal(await invoke({ ...owned, operation: 'tokenLockedBalance' }), '0');
    await invoke({ ...owned, operation: 'tokenBurn' });
    assert.equal(await invoke({ ...owned, operation: 'tokenBalance' }), '0');
    assert.equal(BigInt((await invoke({ ...base, operation: 'tokenProbe' })).treasury.supply), initial + 600n);
  } finally {
    // Inspect authoritative balances to clean up this fixture, never mask a failed chain call.
    for (const wallet of [{ ...base, walletAddress: issuer.address }, owned]) {
      if (BigInt(await invoke({ ...wallet, operation: 'tokenLockedBalance' })) > 0n)
        await invoke({ ...wallet, operation: 'tokenUnlock' });
      if (BigInt(await invoke({ ...wallet, operation: 'tokenBalance' })) > 0n)
        await invoke({ ...wallet, operation: 'tokenBurn' });
    }
  }
  assert.equal(BigInt((await invoke({ ...base, operation: 'tokenProbe' })).treasury.supply), initial);
});
