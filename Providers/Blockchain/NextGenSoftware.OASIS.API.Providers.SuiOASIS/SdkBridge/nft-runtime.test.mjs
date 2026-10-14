import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync, openSync, writeSync, fsyncSync, closeSync, unlinkSync } from 'node:fs';
import { Ed25519Keypair } from '@mysten/sui/keypairs/ed25519';
import { requestSuiFromFaucetV2 } from '@mysten/sui/faucet';
import { invoke } from './bridge.mjs';
import { journaledMutation } from './fixture-submission.mjs';

const rpcEndpoint = process.env.OASIS_SUI_TEST_RPC;
const faucet = process.env.OASIS_SUI_TEST_FAUCET;
const deploymentPath = process.env.OASIS_SUI_TEST_DEPLOYMENT;
if (!rpcEndpoint || !faucet || !deploymentPath) throw new Error('Actual local RPC, faucet and deployment are required');
for (const endpoint of [rpcEndpoint, faucet])
  if (!['127.0.0.1', 'localhost', '[::1]'].includes(new URL(endpoint).hostname)) throw new Error('NFT evidence must use a local chain');

test('real NFT objects mint in quantity, transfer ownership, reject the old owner and burn', { timeout: 120000 }, async () => {
  const journalPath=process.env.OASIS_SUI_TEST_NFT_JOURNAL;
  if(!journalPath)throw new Error('Explicit retained NFT ownership journal required; no mint without durable preparation');
  const journal=openSync(journalPath,'wx');
  const record=value=>{writeSync(journal,JSON.stringify(value)+'\n');fsyncSync(journal);};
  let unknown=false,completed=false,primaryError;
  async function ownedInvoke(request){
    if(unknown)throw new Error('Original NFT outcome requires reconciliation before any further mutation');
    return journaledMutation(invoke,request,record,()=>{unknown=true;});
  }
  const remaining=new Set(),locked=new Set(),signers=new Map();
  let base;
  try {
  const deployment = JSON.parse(readFileSync(deploymentPath, 'utf8'));
  base = { ...deployment, rpcEndpoint, network: 'localnet' };
  // Public deterministic loopback-only fixture, reused; never a public-chain credential.
  const recipientKey=Ed25519Keypair.fromSecretKey(new Uint8Array(32).fill(8));
  const recipient={address:recipientKey.toSuiAddress(),privateKey:recipientKey.getSecretKey()};
  await requestSuiFromFaucetV2({ host: faucet, recipient: recipient.address });
  const owner = await invoke({ operation: 'restoreKey', privateKey: base.privateKey });
  const metadata = JSON.stringify({ Title: 'Living oak 🌳', Description: 'Complete on-chain metadata', MetaData: { park: 'real local chain' } });
  const minted = await ownedInvoke({ ...base, operation: 'nftMint', count: 2, metadata: [metadata, metadata], recipient: owner.address });
  for(const id of minted.tokenIds){remaining.add(id);signers.set(id,base.privateKey);}
  assert.equal(minted.tokenIds.length, 2);
  assert.equal(new Set(minted.tokenIds).size, 2);
    const [tokenId] = minted.tokenIds;
    const nft = await invoke({ ...base, operation: 'nftGet', tokenId });
    assert.equal(nft.metadata, metadata);
    assert.equal(nft.creator, owner.address);
    assert.equal(nft.owner.AddressOwner, owner.address);
    await ownedInvoke({ ...base, operation: 'nftLock', tokenId });
    locked.add(tokenId);
    await assert.rejects(ownedInvoke({ ...base, operation: 'nftSend', tokenId, recipient: recipient.address }), /not found|not exist|wrapped/i);
    await assert.rejects(ownedInvoke({ ...base, operation: 'nftUnlock', tokenId, privateKey: recipient.privateKey }), /No owned escrow/i);
    await ownedInvoke({ ...base, operation: 'nftUnlock', tokenId });
    locked.delete(tokenId);
    assert.equal((await invoke({ ...base, operation: 'nftGet', tokenId })).metadata, metadata);
    const moved = await ownedInvoke({ ...base, operation: 'nftSend', tokenId, fromWalletAddress: owner.address, recipient: recipient.address });
    signers.set(tokenId, recipient.privateKey);
    assert.equal((await invoke({ ...base, operation: 'transaction', transactionHash: moved.transactionHash })).status.success, true);
    assert.equal((await invoke({ ...base, operation: 'nftGet', tokenId })).owner.AddressOwner, recipient.address);
    const custodyRequest={...base,operation:'nftCustodyReceipt',tokenId,custody:recipient.address,transactionHash:moved.transactionHash};
    const custody=await invoke(custodyRequest);
    assert.equal(custody.objectId,tokenId);
    assert.ok(custody.version);assert.ok(custody.digest);
    await assert.rejects(invoke({...custodyRequest,custody:owner.address}),/does not prove this NFT entered custody/);
    await assert.rejects(ownedInvoke({ ...base, operation: 'nftBurn', tokenId }), /owner|owned|ownership/i);
    await ownedInvoke({ ...base, operation: 'nftBurn', tokenId, privateKey: recipient.privateKey });
    remaining.delete(tokenId);
    await assert.rejects(invoke({ ...base, operation: 'nftGet', tokenId }), /not found|not exist|deleted/i);
    await assert.rejects(invoke(custodyRequest),/not found|not exist|deleted|stale/i);
  } catch(error){primaryError=error;throw error;} finally {
    try{
    if(!unknown)for (const tokenId of remaining) {
      if (locked.has(tokenId)) await ownedInvoke({ ...base, operation: 'nftUnlock', tokenId, privateKey: signers.get(tokenId) });
      await ownedInvoke({ ...base, operation: 'nftBurn', tokenId, privateKey: signers.get(tokenId) });
      remaining.delete(tokenId);
    }
    completed=!unknown&&remaining.size===0;
    }catch(cleanupError){
      if(primaryError)throw new AggregateError([primaryError,cleanupError],'NFT fixture and owned cleanup both failed; journal retained');
      throw cleanupError;
    }finally{closeSync(journal);if(completed)unlinkSync(journalPath);}
  }
});
