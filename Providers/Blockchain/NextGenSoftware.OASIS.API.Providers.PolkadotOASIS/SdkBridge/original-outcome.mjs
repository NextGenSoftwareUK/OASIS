import { verifyNativePreparation } from './verify-preparation.mjs';
import { readFinalizedReceipt } from './finalized-receipt.mjs';

const unsigned = /^(0|[1-9][0-9]*)$/;
const reviewedNativeExtensions = new Set(['CheckNonZeroSender', 'CheckSpecVersion', 'CheckTxVersion',
  'CheckGenesis', 'CheckMortality', 'CheckNonce', 'CheckWeight', 'ChargeTransactionPayment']);

export function requireReviewedNativeFeeExtensions(extensions) {
  if (!Array.isArray(extensions) || !extensions.includes('ChargeTransactionPayment')
      || extensions.some(extension => !reviewedNativeExtensions.has(extension)))
    throw new Error('Runtime signed fee extensions are not reviewed for complete native fee coverage.');
}

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

export async function readVerifiedOriginalRange(api, identity, request, finalizedCeiling, readReceipt = readFinalizedReceipt) {
  validateOutcomeRange(request);
  const { fromBlockNumber, maximumBlocks } = request;
  const originalIdentity = { transactionHash: identity.transactionHash, sender: identity.sender };
  const start = BigInt(fromBlockNumber);
  const finalizedHash = await api.rpc.chain.getFinalizedHead();
  const header = await api.rpc.chain.getHeader(finalizedHash);
  const observedTip = header.number.toBigInt();
  if (finalizedCeiling != null && (typeof finalizedCeiling !== 'bigint' || finalizedCeiling < 0n))
    throw new Error('Invalid internal finalized snapshot ceiling.');
  const tip = finalizedCeiling == null || observedTip < finalizedCeiling ? observedTip : finalizedCeiling;
  let next = start;
  let inspected = 0;
  while (next <= tip && inspected < maximumBlocks) {
    const hash = await api.rpc.chain.getBlockHash(next.toString());
    const block = await api.rpc.chain.getBlock(hash);
    if (block.block.extrinsics.some(extrinsic => extrinsic.hash.toHex() === originalIdentity.transactionHash)) {
      const receipt = await readReceipt(api, { blockHash: hash.toHex(), transactionHash: originalIdentity.transactionHash });
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

// Full read-only search through ONE finalized snapshot. Work is bounded by the owned
// process/caller deadline, never an arbitrary lookback silently advertised as complete.
export async function readOriginalNativeFee(api, preparation) {
  const original = structuredClone(preparation);
  const identity = await verifyNativePreparation(api, original);
  requireReviewedNativeFeeExtensions(api.registry.signedExtensions);
  return readVerifiedNativeFee(api, identity);
}

export async function readVerifiedNativeFee(api, identity) {
  const originalIdentity = { ...identity };
  const checkpoint = await api.rpc.chain.getHeader(originalIdentity.signingCheckpointHash);
  const canonical = await api.rpc.chain.getBlockHash(checkpoint.number);
  if (canonical.toHex() !== originalIdentity.signingCheckpointHash)
    throw new Error('Signed checkpoint is not on the canonical chain; original fee cannot be established.');
  const head = await api.rpc.chain.getFinalizedHead();
  const finalized = await api.rpc.chain.getHeader(head);
  const ceiling = finalized.number.toBigInt();
  let cursor = checkpoint.number.toBigInt();
  while (cursor <= ceiling) {
    const outcome = await readVerifiedOriginalRange(api, originalIdentity,
      { fromBlockNumber: cursor.toString(), maximumBlocks: 128 }, ceiling);
    if (outcome.found) return outcome.receipt;
    const next = BigInt(outcome.nextBlockNumber);
    if (next <= cursor) throw new Error('Finalized search did not advance; original fee remains unresolved.');
    cursor = next;
  }
  throw new Error('Original not present through the finalized snapshot; no zero fee, rejection or replay permission can be inferred.');
}
