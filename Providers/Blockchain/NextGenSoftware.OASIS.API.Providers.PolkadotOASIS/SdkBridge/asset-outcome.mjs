import { verifyAssetPreparation } from './asset-operation.mjs';
import { readFinalizedExtrinsicContext, dispatchAndFeeReceipt } from './finalized-receipt.mjs';
import { validateOutcomeRange, readVerifiedOriginalRange } from './original-outcome.mjs';

export function exactAssetEffect(api, identity, own, dispatchSucceeded) {
  if (!dispatchSucceeded) return { observedUnits: null, assetEffectMatchesRequest: false };
  const expected = identity.action === 'mint' ? 'Issued' : identity.action === 'burn' ? 'Burned' : null;
  if (!expected) throw new Error('Unsupported original asset action.');
  const matching = own.filter(event => event.section === 'assets' && event.method === expected);
  if (matching.length !== 1) throw new Error('Exactly one original asset effect event is required.');
  const [id, account, amount] = matching[0].data;
  if (id.toString() !== identity.assetId || api.createType('AccountId32', account.toString()).toHex() !== identity.recipient)
    throw new Error('Asset effect event conflicts with the original asset/account.');
  const observedUnits = amount.toString();
  if (!/^(0|[1-9][0-9]*)$/.test(observedUnits) || BigInt(observedUnits) >= (1n << 128n))
    throw new Error('Invalid observed asset amount.');
  // FRAME burn is best-effort and may reap extra dust. Keep actual effects even when intent was not met.
  return { observedUnits, assetEffectMatchesRequest: observedUnits === identity.units };
}

export async function readVerifiedAssetReceipt(api, identity, request) {
  const context = await readFinalizedExtrinsicContext(api, request);
  const receipt = dispatchAndFeeReceipt(context, request);
  if (api.createType('AccountId32', receipt.payer).toHex() !== identity.sender)
    throw new Error('Asset transaction fee payer differs from the original signer.');
  return { ...receipt, ...exactAssetEffect(api, identity, context.own, receipt.dispatchSucceeded),
    action: identity.action, assetId: identity.assetId, recipient: identity.recipient, requestedUnits: identity.units,
    reconciliationRequired: true, feeCoverage: 'frame-transaction-fee-only-not-all-asset-native-outlay' };
}

export function validateAssetOutcomeRequest(request) {
  validateOutcomeRange({ fromBlockNumber: request.fromBlockNumber ?? '0', maximumBlocks: request.maximumBlocks });
}

export async function readOriginalAssetOutcome(api, request) {
  validateAssetOutcomeRequest(request);
  const original = structuredClone(request.preparation);
  const range = { fromBlockNumber: request.fromBlockNumber, maximumBlocks: request.maximumBlocks };
  const identity = await verifyAssetPreparation(api, original);
  if (range.fromBlockNumber == null) {
    const checkpoint = await api.rpc.chain.getHeader(identity.signingCheckpointHash);
    const canonical = await api.rpc.chain.getBlockHash(checkpoint.number);
    if (canonical.toHex() !== identity.signingCheckpointHash) throw new Error('Original asset checkpoint is not canonical.');
    range.fromBlockNumber = checkpoint.number.toString();
  }
  return readVerifiedOriginalRange(api, identity, range, undefined,
    (reader, identifiers) => readVerifiedAssetReceipt(reader, identity, identifiers));
}
