import { verifyNftPreparation, nftTokenId } from './psp34-contract-mutation.mjs';
import { encodePsp34Arguments } from './psp34-contract-codec.mjs';
import { canonicalBase64 } from './verify-preparation.mjs';
import { readFinalizedExtrinsicContext, dispatchAndFeeReceipt } from './finalized-receipt.mjs';
import { validateOutcomeRange, readVerifiedOriginalRange } from './original-outcome.mjs';

function exact(registry, type, raw) {
  const value = registry.createType(type, raw);
  if (!Buffer.from(value.toU8a()).equals(Buffer.from(raw))) throw new Error('Noncanonical NFT event bytes.');
  return value;
}

// The reviewed code emits mint AttributeSet events in request order, then one
// Transfer; transfer/burn emit one Transfer. Never treat dispatch alone as effect.
export function exactNftEffect(api, identity, own, dispatchSucceeded) {
  if (!['mint', 'transfer', 'burn'].includes(identity.action)) throw new Error('Unknown NFT effect action.');
  if (!dispatchSucceeded) return { applied: false, nftEffectMatchesRequest: false, from: null, to: null };
  const encodedId = encodePsp34Arguments(api.registry, ['id'], [nftTokenId(identity.tokenId)]);
  const events = own.filter(event => event.section === 'contracts' && event.method === 'ContractEmitted'
    && api.createType('AccountId32', event.data[0].toString()).toHex() === identity.contractAddress);
  const attributes = identity.action === 'mint' ? identity.attributes : [];
  if (!Array.isArray(attributes) || events.length !== attributes.length + 1)
    throw new Error('Exact NFT attribute/transfer acknowledgement count differs.');
  for (let i = 0; i < attributes.length; i++) {
    const decoded = exact(api.registry, '(OasisPsp34Id,Bytes,Bytes)', events[i].data[1].toU8a(true));
    const [key, value] = attributes[i];
    if (!Buffer.from(decoded[0].toU8a()).equals(encodedId)
        || !Buffer.from(decoded[1].toU8a(true)).equals(canonicalBase64(key, 128))
        || !Buffer.from(decoded[2].toU8a(true)).equals(value === '' ? Buffer.alloc(0) : canonicalBase64(value, 8192)))
      throw new Error('NFT metadata acknowledgement differs from signed token/key/value.');
  }
  const transfer = exact(api.registry, '(Option<AccountId32>,Option<AccountId32>,OasisPsp34Id)',
    events[events.length - 1].data[1].toU8a(true));
  const from = transfer[0].isNone ? null : api.createType('AccountId32', transfer[0].unwrap()).toHex();
  const to = transfer[1].isNone ? null : api.createType('AccountId32', transfer[1].unwrap()).toHex();
  if (!Buffer.from(transfer[2].toU8a()).equals(encodedId)
      || (identity.action === 'mint' ? from !== null : from === null)
      || (identity.action === 'burn' ? to !== null : to !== identity.to))
    throw new Error('NFT transfer acknowledgement differs from signed action/token/recipient.');
  // PSP34 transfer/burn sign caller+token, not a from-account argument. Return
  // actual native owner for the API to compare with its frozen requested owner;
  // an approved operator may legitimately differ from the token's owner.
  return { applied: true, nftEffectMatchesRequest: true, from, to };
}

export async function readVerifiedNftReceipt(api, identity, request) {
  const context = await readFinalizedExtrinsicContext(api, request);
  const receipt = dispatchAndFeeReceipt(context, request);
  if (api.createType('AccountId32', receipt.payer).toHex() !== identity.sender)
    throw new Error('NFT native fee payer differs from original signer.');
  if (receipt.dispatchSucceeded) {
    const at = await api.at(receipt.blockHash);
    const info = await at.query.contracts.contractInfoOf(identity.contractAddress);
    if (info.isNone || info.unwrap().codeHash.toHex().toLowerCase() !== identity.contractCodeHash.toLowerCase())
      throw new Error('NFT original receipt does not prove the reviewed contract code.');
  }
  const effect = exactNftEffect(api, identity, context.own, receipt.dispatchSucceeded);
  return { ...receipt, ...effect, action: identity.action, tokenId: identity.tokenId,
    contractAddress: identity.contractAddress, reconciliationRequired: true,
    feeCoverage: 'frame-transaction-fee-only-storage-deposit-effects-separate' };
}

export function validateNftOutcome(request) {
  validateOutcomeRange({ fromBlockNumber: request.fromBlockNumber ?? '0', maximumBlocks: request.maximumBlocks });
}

export async function readOriginalNftOutcome(api, request) {
  request = structuredClone(request);
  validateNftOutcome(request);
  const identity = await verifyNftPreparation(api, request.preparation);
  let fromBlockNumber = request.fromBlockNumber;
  if (fromBlockNumber == null) {
    const checkpoint = await api.rpc.chain.getHeader(identity.signingCheckpointHash);
    if ((await api.rpc.chain.getBlockHash(checkpoint.number)).toHex() !== identity.signingCheckpointHash)
      throw new Error('Original NFT signed checkpoint is not canonical.');
    fromBlockNumber = checkpoint.number.toString();
  }
  return readVerifiedOriginalRange(api, identity, { fromBlockNumber, maximumBlocks: request.maximumBlocks }, undefined,
    (reader, identifiers) => readVerifiedNftReceipt(reader, identity, identifiers));
}
