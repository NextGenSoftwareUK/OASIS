import { TypeRegistry } from '@polkadot/types/create';
import { canonicalBase64, verifySignedPreparation } from './verify-preparation.mjs';
import { validateExplicitSigner, withExplicitSigner, signOriginalCall, withOriginalFeeEstimate } from './native-transfer.mjs';
import { validatePinnedContractQuery, openPinnedContractQuery } from './pinned-contract-query.mjs';
import { encodeOasisPsp34Mutation, decodeOasisPsp34MutationResult } from './oasis-psp34-mutation-codec.mjs';

const protocol = 'oasis.polkadot.psp34-mutation.preparation.v1';
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
  const id = nftTokenId(request.tokenId);
  let attributes;
  if (request.attributes !== undefined) {
    if (!Array.isArray(request.attributes)) throw new Error('Explicit metadata byte pairs required.');
    attributes = request.attributes.map(pair => {
      if (!Array.isArray(pair) || pair.length !== 2) throw new Error('Explicit metadata byte pairs required.');
      return [bytes(pair[0], 128), bytes(pair[1], 8192, true)];
    });
  }
  return encodeOasisPsp34Mutation(registry, request.action, id, request.to, attributes);
}

export async function validateNftMutation(request) {
  nftMutationData(new TypeRegistry(), request);
  validatePinnedContractQuery({ ...request, origin: request.sender });
  if (request.recipient !== request.contractAddress)
    throw new Error('Explicit signing recipient must be the reviewed NFT contract.');
  if (request.memo != null && request.memo !== '') throw new Error('NFT memo is not enabled; never silently discard it.');
  await validateExplicitSigner(request);
}

export async function prepareNftMutation(api, request) {
  request = structuredClone(request);
  await validateNftMutation(request);
  await openPinnedContractQuery(api, { ...request, origin: request.sender });
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
    const tx = api.tx.contracts.call(address, 0, gasLimit, storageDepositLimit, data);
    const intent = { sender, contractAddress: address, contractCodeHash: request.contractCodeHash.toLowerCase(),
      action: request.action, tokenId: request.tokenId, gasLimit, storageDepositLimit,
      ...(request.to !== undefined ? { to: request.to } : {}),
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
  const { decoded, sender, genesis, payload } = await verifySignedPreparation(api, preparation, protocol);
  validatePinnedContractQuery({ ...preparation, origin: sender });
  const call = decoded.method;
  if (call.section !== 'contracts' || call.method !== 'call' || call.args.length !== 5)
    throw new Error('Signed NFT original is not a direct contracts.call.');
  const address = api.createType('AccountId32', call.args[0].toString()).toHex();
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
    signingCheckpointHash: payload.blockHash, gasLimit: preparation.gasLimit,
    storageDepositLimit: preparation.storageDepositLimit,
    feeCoverage: 'identity-and-execution-limits-not-total-fee-cap' };
}
