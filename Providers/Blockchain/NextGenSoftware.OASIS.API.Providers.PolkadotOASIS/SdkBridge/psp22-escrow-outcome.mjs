import { blake2AsHex } from '@polkadot/util-crypto';
import { verifyTokenEscrowPreparation } from './psp22-escrow-mutation.mjs';
import { readFinalizedExtrinsicContext, dispatchAndFeeReceipt } from './finalized-receipt.mjs';
import { validateOutcomeRange, readVerifiedOriginalRange } from './original-outcome.mjs';

export function tokenEscrowEventSignature(action) {
  if (!['lock', 'unlock'].includes(action)) throw new Error('Explicit token escrow event action required.');
  // ink5 derives this topic from event name and whitespace-free Rust field
  // type spellings. Locked and Unlocked intentionally have identical payloads.
  const name = action === 'lock' ? 'Locked' : 'Unlocked';
  return blake2AsHex(`${name}([u8;32],AccountId,AccountId,u128)`, 256);
}
export function exactTokenEscrowEffect(api, identity, records, dispatchSucceeded) {
  const signature = tokenEscrowEventSignature(identity.action);
  if (!dispatchSucceeded) return { applied: false, tokenEscrowEffectMatchesRequest: false };
  if (!Array.isArray(records)) throw new Error('Original event records including signature topics required.');
  const matches = records.filter(({ event }) => event.section === 'contracts' && event.method === 'ContractEmitted'
    && api.createType('AccountId32', event.data[0].toString()).toHex() === identity.contractAddress);
  if (matches.length !== 1) throw new Error('Exactly one original escrow acknowledgement required.');
  const record = matches[0];
  if (record.topics.length !== 3 || record.topics[0].toHex() !== signature
      || record.topics[1].toHex() !== identity.depositId || record.topics[2].toHex() !== identity.sender)
    throw new Error('Escrow event signature or indexed deposit/owner differs from signed intent.');
  const raw = record.event.data[1].toU8a(true);
  const decoded = api.registry.createType('(H256,AccountId32,AccountId32,u128)', raw);
  if (!Buffer.from(decoded.toU8a()).equals(Buffer.from(raw)) || decoded[0].toHex() !== identity.depositId
      || decoded[1].toHex() !== identity.sender || decoded[2].toHex() !== identity.tokenAddress
      || decoded[3].toString() !== identity.amountUnits)
    throw new Error('Escrow acknowledgement differs from original deposit/owner/token/amount.');
  return { applied: true, tokenEscrowEffectMatchesRequest: true, released: identity.action === 'unlock' };
}
export async function readVerifiedTokenEscrowReceipt(api, identity, request) {
  const context = await readFinalizedExtrinsicContext(api, request);
  const receipt = dispatchAndFeeReceipt(context, request);
  if (api.createType('AccountId32', receipt.payer).toHex() !== identity.sender)
    throw new Error('Escrow native fee payer differs from original signer.');
  if (receipt.dispatchSucceeded) {
    const at = await api.at(receipt.blockHash);
    const info = await at.query.contracts.contractInfoOf(identity.contractAddress);
    if (info.isNone || info.unwrap().codeHash.toHex().toLowerCase() !== identity.contractCodeHash)
      throw new Error('Original token receipt does not prove the reviewed escrow code.');
  }
  return { ...receipt, ...exactTokenEscrowEffect(api, identity, context.ownRecords, receipt.dispatchSucceeded),
    action: identity.action, depositId: identity.depositId, tokenAddress: identity.tokenAddress, amountUnits: identity.amountUnits,
    contractAddress: identity.contractAddress, reconciliationRequired: true,
    feeCoverage: 'frame-transaction-fee-only-storage-deposit-effects-separate' };
}
export async function readOriginalTokenEscrowOutcome(api, request) {
  request = structuredClone(request);
  validateOutcomeRange({ fromBlockNumber: request.fromBlockNumber ?? '0', maximumBlocks: request.maximumBlocks });
  const identity = await verifyTokenEscrowPreparation(api, request.preparation);
  let fromBlockNumber = request.fromBlockNumber;
  if (fromBlockNumber == null) {
    const checkpoint = await api.rpc.chain.getHeader(identity.signingCheckpointHash);
    if ((await api.rpc.chain.getBlockHash(checkpoint.number)).toHex() !== identity.signingCheckpointHash)
      throw new Error('Original token escrow signing checkpoint is not canonical.');
    fromBlockNumber = checkpoint.number.toString();
  }
  return readVerifiedOriginalRange(api, identity, { fromBlockNumber, maximumBlocks: request.maximumBlocks }, undefined,
    (reader, identifiers) => readVerifiedTokenEscrowReceipt(reader, identity, identifiers));
}
