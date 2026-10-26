import { TypeRegistry } from '@polkadot/types/create';
import { canonicalBase64, verifySignedPreparation } from './verify-preparation.mjs';
import { validateExplicitSigner, withExplicitSigner, signOriginalCall, withOriginalFeeEstimate } from './native-transfer.mjs';
import { validatePinnedContractQuery, openPinnedContractQuery } from './pinned-contract-query.mjs';
import { encodeOasisPsp34Mutation, decodeOasisPsp34MutationResult, decodeOasisPsp34Mutation, encodeOasisPsp34EscrowQuery } from './oasis-psp34-mutation-codec.mjs';
import { encodePsp34Message, decodePsp34Result } from './psp34-contract-codec.mjs';

const protocol = 'oasis.polkadot.psp34-mutation.preparation.v1';
const ownerWitnessPrefix = Buffer.from('oasis:psp34:from:v1:', 'utf8');
export function encodeNftOwnerWitness(owner) {
  if (typeof owner !== 'string' || !/^0x[0-9a-f]{64}$/.test(owner)) throw new Error('Canonical NFT source owner required.');
  return '0x' + Buffer.concat([ownerWitnessPrefix, Buffer.from(owner.slice(2), 'hex')]).toString('hex');
}
export function unwrapNftCall(decoded) {
  let call = decoded.method;
  if (call.section !== 'utility') return { call };
  if (call.method !== 'batchAll' || call.args.length !== 1 || call.args[0].length !== 2)
    throw new Error('NFT source-owner witness requires exactly two atomic calls.');
  const [inner, remark] = call.args[0];
  if (inner.section !== 'contracts' || inner.method !== 'call') throw new Error('NFT witness must wrap one direct contract call.');
  if (remark.section !== 'system' || remark.method !== 'remarkWithEvent' || remark.args.length !== 1)
    throw new Error('NFT source-owner witness is not the reviewed atomic remark.');
  const bytes = Buffer.from(remark.args[0].toU8a(true));
  if (bytes.length !== ownerWitnessPrefix.length + 32 || !bytes.subarray(0, ownerWitnessPrefix.length).equals(ownerWitnessPrefix))
    throw new Error('NFT source-owner witness has an invalid domain or size.');
  return { call: inner, expectedFrom: '0x' + bytes.subarray(ownerWitnessPrefix.length).toString('hex') };
}
function validateExpectedOwner(request) {
  if (request.expectedFrom !== undefined && (request.action === 'mint'
      || typeof request.expectedFrom !== 'string' || !/^0x[0-9a-f]{64}$/.test(request.expectedFrom)))
    throw new Error('Expected NFT source owner requires a canonical account on transfer/burn only.');
}
function bytes(value, maximum, empty = false) {
  return empty && value === '' ? new Uint8Array() : canonicalBase64(value, maximum);
}
// JSON transports an explicit typed identity. Byte identities use canonical hex;
// numeric variants remain canonical decimal strings, never JS Number or GUIDs.
export function nftTokenId(value) {
  let id = value;
  if (id && Object.hasOwn(id, 'Bytes')) {
    if (typeof id.Bytes !== 'string' || !/^0x(?:[0-9a-f]{2})*$/.test(id.Bytes))
      throw new Error('PSP34 byte identity requires canonical lower-case hex.');
    id = { ...id, Bytes: Uint8Array.from(Buffer.from(id.Bytes.slice(2), 'hex')) };
  }
  return id;
}
export function nftMutationData(registry, request) {
  validateEscrowIntent(request, request.sender, request.contractAddress);
  const id = nftTokenId(request.tokenId);
  let attributes;
  if (request.attributes !== undefined) {
    if (!Array.isArray(request.attributes)) throw new Error('Explicit metadata byte pairs required.');
    attributes = request.attributes.map(pair => {
      if (!Array.isArray(pair) || pair.length !== 2) throw new Error('Explicit metadata byte pairs required.');
      return [bytes(pair[0], 128), bytes(pair[1], 8192, true)];
    });
  }
  return encodeOasisPsp34Mutation(registry, request.action, id,
    ['lock', 'unlock'].includes(request.action) ? undefined : request.to, attributes);
}

function validateEscrowIntent(request, sender, contract) {
  if (!['lock', 'unlock'].includes(request.action)) return;
  const to = request.action === 'lock' ? contract : sender;
  const from = request.action === 'lock' ? sender : contract;
  if (request.to !== to || request.expectedFrom !== from)
    throw new Error('Native escrow requires its deterministic beneficiary/contract destination and signed source-owner witness.');
}

export async function validateNftMutation(request) {
  nftMutationData(new TypeRegistry(), request);
  validateExpectedOwner(request);
  validatePinnedContractQuery({ ...request, origin: request.sender });
  if (request.recipient !== request.contractAddress)
    throw new Error('Explicit signing recipient must be the reviewed NFT contract.');
  if (request.memo != null && request.memo !== '') throw new Error('NFT memo is not enabled; never silently discard it.');
  await validateExplicitSigner(request);
}

export async function prepareNftMutation(api, request) {
  request = structuredClone(request);
  await validateNftMutation(request);
  const view = await openPinnedContractQuery(api, { ...request, origin: request.sender });
  if (request.expectedFrom !== undefined) {
    const owner = decodePsp34Result(api.registry, 'ownerOf',
      await view.query(encodePsp34Message(api.registry, 'ownerOf', [nftTokenId(request.tokenId)])));
    if (owner.isNone || api.createType('AccountId32', owner.unwrap()).toHex() !== request.expectedFrom)
      throw new Error('Observed NFT owner differs from the frozen requested source owner.');
  }
  if (['lock', 'unlock'].includes(request.action)) {
    const beneficiary = decodePsp34Result(api.registry, 'ownerOf',
      await view.query(encodeOasisPsp34EscrowQuery(api.registry, nftTokenId(request.tokenId))));
    if (request.action === 'lock' ? !beneficiary.isNone :
      beneficiary.isNone || api.createType('AccountId32', beneficiary.unwrap()).toHex() !== request.sender)
      throw new Error('Native escrow beneficiary does not authorize this lock/unlock intent.');
  }
  if (!api.tx.contracts?.call) throw new Error('Runtime has no contracts.call operation.');
  const address = api.createType('AccountId32', request.contractAddress).toHex();
  const data = nftMutationData(api.registry, request);
  return withExplicitSigner(api, request, async (pair, sender) => {
    const simulation = await api.call.contractsApi.call(sender, address, 0, null, null, data);
    if (simulation.result.isErr) throw new Error(`Native NFT simulation failed: ${simulation.result.asErr.toString()}`);
    decodeOasisPsp34MutationResult(api.registry, request.action, simulation.result.asOk.data.toU8a(true));
    if (simulation.result.asOk.flags.get('bits').toNumber() !== 0) throw new Error('NFT simulation reverted.');
    const storageDepositLimit = simulation.storageDeposit.isCharge ? simulation.storageDeposit.asCharge.toString() : '0';
    const gasLimit = { refTime: simulation.gasRequired.refTime.toString(), proofSize: simulation.gasRequired.proofSize.toString() };
    let tx = api.tx.contracts.call(address, 0, gasLimit, storageDepositLimit, data);
    if (request.expectedFrom !== undefined) {
      if (!api.tx.utility?.batchAll || !api.tx.system?.remarkWithEvent)
        throw new Error('Runtime cannot atomically preserve the requested NFT source owner.');
      tx = api.tx.utility.batchAll([tx, api.tx.system.remarkWithEvent(encodeNftOwnerWitness(request.expectedFrom))]);
    }
    const intent = { sender, contractAddress: address, contractCodeHash: request.contractCodeHash.toLowerCase(),
      action: request.action, tokenId: request.tokenId, gasLimit, storageDepositLimit,
      ...(request.to !== undefined ? { to: request.to } : {}),
      ...(request.expectedFrom !== undefined ? { expectedFrom: request.expectedFrom } : {}),
      ...(request.attributes !== undefined ? { attributes: request.attributes } : {}) };
    const prepared = await signOriginalCall(api, tx, pair, protocol, intent);
    await verifyNftPreparation(api, prepared);
    return withOriginalFeeEstimate(api, prepared);
  });
}

export async function verifyNftPreparation(api, preparation) {
  preparation = structuredClone(preparation);
  if (preparation?.protocol !== protocol) throw new Error('Versioned PSP34 mutation preparation required.');
  const data = nftMutationData(api.registry, preparation);
  validateExpectedOwner(preparation);
  const { decoded, sender, genesis, payload } = await verifySignedPreparation(api, preparation, protocol);
  validatePinnedContractQuery({ ...preparation, origin: sender });
  const { call, expectedFrom } = unwrapNftCall(decoded);
  if (call.section !== 'contracts' || call.method !== 'call' || call.args.length !== 5)
    throw new Error('Signed NFT original is not a direct contracts.call.');
  if (preparation.expectedFrom !== expectedFrom) throw new Error('Declared NFT source owner differs from its signed atomic witness.');
  const address = api.createType('AccountId32', call.args[0].toString()).toHex();
  validateEscrowIntent(preparation, sender, address);
  if (address !== preparation.contractAddress || call.args[1].toBigInt() !== 0n || call.args[4].toHex() !== data
      || call.args[2].refTime.toString() !== preparation.gasLimit?.refTime
      || call.args[2].proofSize.toString() !== preparation.gasLimit?.proofSize
      || call.args[3].isNone || call.args[3].unwrap().toString() !== preparation.storageDepositLimit)
    throw new Error('Declared NFT action/token/account/metadata/gas/deposit differs from signed original.');
  await openPinnedContractQuery(api, { ...preparation, origin: sender });
  return { verified: true, transactionHash: decoded.hash.toHex(), sender, chainId: genesis,
    contractAddress: address, contractCodeHash: preparation.contractCodeHash.toLowerCase(),
    action: preparation.action, tokenId: preparation.tokenId,
    ...(preparation.to !== undefined ? { to: preparation.to } : {}),
    ...(preparation.attributes !== undefined ? { attributes: preparation.attributes } : {}),
    ...(expectedFrom !== undefined ? { expectedFrom } : {}),
    signingCheckpointHash: payload.blockHash, gasLimit: preparation.gasLimit,
    storageDepositLimit: preparation.storageDepositLimit,
    feeCoverage: 'identity-and-execution-limits-not-total-fee-cap' };
}

export function validateNftRecovery(request) {
  if (typeof request.contractCodeHash !== 'string' || !/^0x[0-9a-f]{64}$/.test(request.contractCodeHash))
    throw new Error('Explicit reviewed NFT implementation code hash required for recovery.');
}

export async function recoverNftPreparation(api, request) {
  request = structuredClone(request);validateNftRecovery(request);
  const original = request.preparation;
  const { decoded, sender, genesis } = await verifySignedPreparation(api, original, protocol);
  const { call, expectedFrom } = unwrapNftCall(decoded);
  if (call.section !== 'contracts' || call.method !== 'call' || call.args.length !== 5
      || call.args[1].toBigInt() !== 0n || call.args[3].isNone)
    throw new Error('Retained original is not a zero-value NFT contracts.call with a signed deposit limit.');
  const intent = decodeOasisPsp34Mutation(api.registry, call.args[4].toHex());
  if (['lock', 'unlock'].includes(intent.action))
    intent.to = intent.action === 'lock' ? api.createType('AccountId32', call.args[0].toString()).toHex() : sender;
  const prepared = { protocol, chainId: genesis, sender, transactionHash: decoded.hash.toHex(),
    signedTransactionBase64: original.signedTransactionBase64, signDocBase64: original.signDocBase64,
    signature: original.signature, contractAddress: api.createType('AccountId32', call.args[0].toString()).toHex(),
    contractCodeHash: request.contractCodeHash, ...intent,
    recoveredFromSignedOriginal: true, reconciliationRequired: true, sourceOwnerPreconditionRetained: expectedFrom !== undefined,
    ...(expectedFrom !== undefined ? { expectedFrom } : {}),
    gasLimit: { refTime: call.args[2].refTime.toString(), proofSize: call.args[2].proofSize.toString() },
    storageDepositLimit: call.args[3].unwrap().toString() };
  await verifyNftPreparation(api, prepared);
  // Direct PSP34 calls have no source-owner argument. Only a verified signed
  // atomic witness preserves that precondition; never invent it for older calls.
  return prepared;
}
