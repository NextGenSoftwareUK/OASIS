import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { requestSuiFromFaucetV2 } from '@mysten/sui/faucet';
import { invoke } from './bridge.mjs';

const rpcEndpoint = process.env.OASIS_SUI_TEST_RPC;
const faucet = process.env.OASIS_SUI_TEST_FAUCET;
const deploymentPath = process.env.OASIS_SUI_TEST_DEPLOYMENT;
if (!rpcEndpoint || !faucet || !deploymentPath) throw new Error('Actual local RPC, faucet and deployment are required');
for (const endpoint of [rpcEndpoint, faucet])
  if (!['127.0.0.1', 'localhost', '[::1]'].includes(new URL(endpoint).hostname)) throw new Error('NFT evidence must use a local chain');

test('real NFT objects mint in quantity, transfer ownership, reject the old owner and burn', { timeout: 120000 }, async () => {
  const deployment = JSON.parse(readFileSync(deploymentPath, 'utf8'));
  const base = { ...deployment, rpcEndpoint, network: 'localnet' };
  const recipient = await invoke({ operation: 'generateKey' });
  await requestSuiFromFaucetV2({ host: faucet, recipient: recipient.address });
  const owner = await invoke({ operation: 'restoreKey', privateKey: base.privateKey });
  const metadata = JSON.stringify({ Title: 'Living oak 🌳', Description: 'Complete on-chain metadata', MetaData: { park: 'real local chain' } });
  const minted = await invoke({ ...base, operation: 'nftMint', count: 2, metadata: [metadata, metadata], recipient: owner.address });
  assert.equal(minted.tokenIds.length, 2);
  assert.equal(new Set(minted.tokenIds).size, 2);
  const remaining = new Set(minted.tokenIds);
  const locked = new Set();
  const signers = new Map(minted.tokenIds.map(id => [id, base.privateKey]));
  try {
    const [tokenId] = minted.tokenIds;
    const nft = await invoke({ ...base, operation: 'nftGet', tokenId });
    assert.equal(nft.metadata, metadata);
    assert.equal(nft.creator, owner.address);
    assert.equal(nft.owner.AddressOwner, owner.address);
    await invoke({ ...base, operation: 'nftLock', tokenId });
    locked.add(tokenId);
    await assert.rejects(invoke({ ...base, operation: 'nftSend', tokenId, recipient: recipient.address }), /not found|not exist|wrapped/i);
    await assert.rejects(invoke({ ...base, operation: 'nftUnlock', tokenId, privateKey: recipient.privateKey }), /No owned escrow/i);
    await invoke({ ...base, operation: 'nftUnlock', tokenId });
    locked.delete(tokenId);
    assert.equal((await invoke({ ...base, operation: 'nftGet', tokenId })).metadata, metadata);
    const moved = await invoke({ ...base, operation: 'nftSend', tokenId, fromWalletAddress: owner.address, recipient: recipient.address });
    signers.set(tokenId, recipient.privateKey);
    assert.equal((await invoke({ ...base, operation: 'transaction', transactionHash: moved.transactionHash })).status.success, true);
    assert.equal((await invoke({ ...base, operation: 'nftGet', tokenId })).owner.AddressOwner, recipient.address);
    await assert.rejects(invoke({ ...base, operation: 'nftBurn', tokenId }), /owner|owned|ownership/i);
    await invoke({ ...base, operation: 'nftBurn', tokenId, privateKey: recipient.privateKey });
    remaining.delete(tokenId);
    await assert.rejects(invoke({ ...base, operation: 'nftGet', tokenId }), /not found|not exist|deleted/i);
  } finally {
    for (const tokenId of remaining) {
      if (locked.has(tokenId)) await invoke({ ...base, operation: 'nftUnlock', tokenId, privateKey: signers.get(tokenId) });
      await invoke({ ...base, operation: 'nftBurn', tokenId, privateKey: signers.get(tokenId) });
    }
  }
});
