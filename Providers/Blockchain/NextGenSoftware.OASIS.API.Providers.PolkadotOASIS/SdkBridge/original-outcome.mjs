import { verifyNativePreparation } from './verify-preparation.mjs';
import { readFinalizedReceipt } from './finalized-receipt.mjs';

const unsigned = /^(0|[1-9][0-9]*)$/;

export function validateOutcomeRange(request) {
  if (typeof request.fromBlockNumber !== 'string' || request.fromBlockNumber.length > 20 || !unsigned.test(request.fromBlockNumber)
      || BigInt(request.fromBlockNumber) >= (1n << 64n))
    throw new Error('An explicit canonical unsigned starting block number is required.');
  if (!Number.isInteger(request.maximumBlocks) || request.maximumBlocks < 1 || request.maximumBlocks > 128)
    throw new Error('A bounded finalized scan of one to 128 blocks is required.');
}

// Read-only, resumable recovery of the SAME signed original. Absence in a bounded range
// never proves rejection/expiry or authorizes another submission.
export async function readOriginalOutcome(api, request) {
  validateOutcomeRange(request);
  const original = structuredClone(request.preparation);
  const range = { fromBlockNumber: request.fromBlockNumber, maximumBlocks: request.maximumBlocks };
  const identity = await verifyNativePreparation(api, original);
  return readVerifiedOriginalRange(api, identity, range);
}

export async function readVerifiedOriginalRange(api, identity, request) {
  validateOutcomeRange(request);
  const { fromBlockNumber, maximumBlocks } = request;
  const originalIdentity = { transactionHash: identity.transactionHash, sender: identity.sender };
  const start = BigInt(fromBlockNumber);
  const finalizedHash = await api.rpc.chain.getFinalizedHead();
  const header = await api.rpc.chain.getHeader(finalizedHash);
  const tip = header.number.toBigInt();
  let next = start;
  let inspected = 0;
  while (next <= tip && inspected < maximumBlocks) {
    const hash = await api.rpc.chain.getBlockHash(next.toString());
    const block = await api.rpc.chain.getBlock(hash);
    if (block.block.extrinsics.some(extrinsic => extrinsic.hash.toHex() === originalIdentity.transactionHash)) {
      const receipt = await readFinalizedReceipt(api, { blockHash: hash.toHex(), transactionHash: originalIdentity.transactionHash });
      if (api.createType('AccountId32', receipt.payer).toHex() !== originalIdentity.sender)
        throw new Error('Final native fee payer differs from the verified original signer.');
      return { found: true, reconciliationRequired: true, receipt };
    }
    inspected++;
    next++;
  }
  return { found: false, transactionHash: originalIdentity.transactionHash, finalized: false,
    reconciliationRequired: true, fromBlockNumber: start.toString(),
    finalizedSnapshotBlock: tip.toString(), nextBlockNumber: next.toString(), inspectedBlocks: inspected,
    searchedThroughSnapshot: next > tip };
}
