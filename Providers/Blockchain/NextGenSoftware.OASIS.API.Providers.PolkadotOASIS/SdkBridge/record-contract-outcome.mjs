import { verifyRecordPreparation } from './record-contract-mutation.mjs';
import { canonicalBase64 } from './verify-preparation.mjs';
import { encodeRecordMessage, decodeRecordResult } from './record-contract-codec.mjs';
import { readFinalizedExtrinsicContext, dispatchAndFeeReceipt } from './finalized-receipt.mjs';
import { validateOutcomeRange, readVerifiedOriginalRange } from './original-outcome.mjs';

export function exactRecordEffect(api, identity, own, dispatchSucceeded) {
  if (!['put', 'delete'].includes(identity.action)) throw new Error('Unsupported record action.');
  if (!dispatchSucceeded) return { applied: false, recordEffectMatchesRequest: false, revision: null };
  const events = own.filter(event => event.section === 'contracts' && event.method === 'ContractEmitted'
    && api.createType('AccountId32', event.data[0].toString()).toHex() === identity.contractAddress);
  if (events.length === 0 && identity.action === 'delete' && identity.expectation !== 'Revision') return null; // Requires native absence proof below.
  if (events.length !== 1) throw new Error('Exactly one original record Mutation acknowledgement is required.');
  const raw = events[0].data[1].toU8a(true);
  const decoded = api.registry.createType('([u8;32],Bytes,u64,bool)', raw);
  if (!Buffer.from(decoded.toU8a()).equals(Buffer.from(raw))) throw new Error('Noncanonical record event.');
  const tag = canonicalBase64(identity.operationTagBase64, 32);
  const key = canonicalBase64(identity.keyBase64, 1024);
  if (!Buffer.from(decoded[0].toU8a()).equals(tag) || !Buffer.from(decoded[1].toU8a(true)).equals(key)
      || decoded[3].isTrue !== (identity.action === 'delete')) throw new Error('Native record event differs from original signed intent.');
  const revision = decoded[2].toBigInt();
  if (revision === 0n || (identity.expectation === 'Revision' && revision <= BigInt(identity.revision))) {
    throw new Error('Native record acknowledgement has an invalid/stale revision.');
  }
  return { applied: true, recordEffectMatchesRequest: true, revision: revision.toString() };
}

export async function readVerifiedRecordReceipt(api, identity, request) {
  const context = await readFinalizedExtrinsicContext(api, request);
  const receipt = dispatchAndFeeReceipt(context, request);
  if (api.createType('AccountId32', receipt.payer).toHex() !== identity.sender) throw new Error('Record fee payer differs from original signer.');
  const at = await api.at(receipt.blockHash);
  if (receipt.dispatchSucceeded) {
    const info = await at.query.contracts.contractInfoOf(identity.contractAddress);
    if (info.isNone || info.unwrap().codeHash.toHex().toLowerCase() !== identity.contractCodeHash.toLowerCase()) {
      throw new Error('Original receipt does not prove the pinned record contract code.');
    }
  }
  let effect = exactRecordEffect(api, identity, context.own, receipt.dispatchSucceeded);
  if (effect === null) {
    const result = await at.call.contractsApi.call(identity.sender, identity.contractAddress, 0, null, null,
      encodeRecordMessage(api.registry, 'get', [canonicalBase64(identity.keyBase64, 1024)]));
    if (result.result.isErr || result.result.asOk.flags.get('bits').toNumber() !== 0) throw new Error('Missing-delete absence proof failed.');
    if (!decodeRecordResult(api.registry, 'get', result.result.asOk.data.toU8a(true)).isNone) {
      throw new Error('Delete has no acknowledgement and the finalized key is present; preserve original for reconciliation.');
    }
    effect = { applied: false, recordEffectMatchesRequest: true, revision: null };
  }
  return { ...receipt, ...effect, action: identity.action, contractAddress: identity.contractAddress,
    reconciliationRequired: true, feeCoverage: 'frame-transaction-fee-only-storage-deposit-effects-separate' };
}

export function validateRecordOutcome(request) {
  validateOutcomeRange({ fromBlockNumber: request.fromBlockNumber ?? '0', maximumBlocks: request.maximumBlocks });
}

export async function readOriginalRecordOutcome(api, request) {
  validateRecordOutcome(request);
  const identity = await verifyRecordPreparation(api, request.preparation);
  let fromBlockNumber = request.fromBlockNumber;
  if (fromBlockNumber == null) {
    const checkpoint = await api.rpc.chain.getHeader(identity.signingCheckpointHash);
    if ((await api.rpc.chain.getBlockHash(checkpoint.number)).toHex() !== identity.signingCheckpointHash) {
      throw new Error('Original record checkpoint is not canonical.');
    }
    fromBlockNumber = checkpoint.number.toString();
  }
  return readVerifiedOriginalRange(api, identity, { fromBlockNumber, maximumBlocks: request.maximumBlocks }, undefined,
    (reader, identifiers) => readVerifiedRecordReceipt(reader, identity, identifiers));
}
