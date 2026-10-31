import { canonicalBase64, verifySignedPreparation } from './verify-preparation.mjs';
import { validateExplicitSigner, withExplicitSigner, signOriginalCall, withOriginalFeeEstimate } from './native-transfer.mjs';
import { readRecordContract, validateRecordRead } from './record-contract-read.mjs';
import { registerRecordContractTypes, encodeRecordMessage, decodeRecordResult } from './record-contract-codec.mjs';

const protocol = 'oasis.polkadot.record-mutation.preparation.v1';
const unsigned = /^(0|[1-9][0-9]*)$/;
function bytes(value, maximum, empty = false) {
  return empty && value === '' ? new Uint8Array() : canonicalBase64(value, maximum);
}
export function recordMutationArguments(request) {
  if (!['put', 'delete'].includes(request.action)) throw new Error('Explicit record put/delete action required.');
  const key = bytes(request.keyBase64, 1024);
  const tag = bytes(request.operationTagBase64, 32);
  if (tag.length !== 32) throw new Error('Record operation tag must contain 32 bytes.');
  let expectation;
  if (request.expectation === 'Any' || request.expectation === 'Missing') {
    if (request.revision != null) throw new Error('Revision supplied without revision expectation.');
    expectation = request.expectation;
  } else if (request.expectation === 'Revision' && typeof request.revision === 'string'
      && request.revision.length <= 20 && unsigned.test(request.revision)
      && BigInt(request.revision) <= 18446744073709551615n) {
    expectation = { Revision: BigInt(request.revision) };
  } else throw new Error('Explicit valid record expectation/revision required.');
  if (request.action === 'delete') {
    if (request.valueBase64 != null) throw new Error('Delete cannot silently discard a supplied value.');
    return [key, expectation, tag];
  }
  return [key, bytes(request.valueBase64, 8192, true), expectation, tag];
}

export async function validateRecordMutation(request) {
  recordMutationArguments(request);
  validateRecordRead({ ...request, action: 'verify', origin: request.sender });
  if (request.recipient !== request.contractAddress) throw new Error('Explicit signer recipient must be the configured contract.');
  await validateExplicitSigner(request);
}

export async function prepareRecordMutation(api, request) {
  request = structuredClone(request);
  await validateRecordMutation(request);
  registerRecordContractTypes(api.registry);
  await readRecordContract(api, { ...request, action: 'verify', origin: request.sender });
  if (!api.tx.contracts?.call) throw new Error('Runtime has no contracts.call submission method.');
  const address = api.createType('AccountId32', request.contractAddress).toHex();
  const args = recordMutationArguments(request);
  const data = encodeRecordMessage(api.registry, request.action, args);
  return withExplicitSigner(api, request, async (pair, sender) => {
    const simulation = await api.call.contractsApi.call(sender, address, 0, null, null, data);
    if (simulation.result.isErr) throw new Error(`Native contract simulation failed: ${simulation.result.asErr.toString()}`);
    // Decode application rejection before signing. The runtime represents a
    // deliberate application Err as a REVERT result carrying the SCALE error.
    decodeRecordResult(api.registry, request.action, simulation.result.asOk.data.toU8a(true));
    if (simulation.result.asOk.flags.get('bits').toNumber() !== 0) throw new Error('Record simulation reverted.');
    const storageDepositLimit = simulation.storageDeposit.isCharge ? simulation.storageDeposit.asCharge.toString() : '0';
    const gasLimit = { refTime: simulation.gasRequired.refTime.toString(), proofSize: simulation.gasRequired.proofSize.toString() };
    const tx = api.tx.contracts.call(address, 0, gasLimit, storageDepositLimit, data);
    const intent = { sender, contractAddress: address, contractCodeHash: request.contractCodeHash.toLowerCase(),
      action: request.action, keyBase64: request.keyBase64, expectation: request.expectation,
      operationTagBase64: request.operationTagBase64, gasLimit, storageDepositLimit,
      ...(request.action === 'put' ? { valueBase64: request.valueBase64 } : {}),
      ...(request.expectation === 'Revision' ? { revision: request.revision } : {}) };
    return withOriginalFeeEstimate(api, await signOriginalCall(api, tx, pair, protocol, intent));
  });
}

export async function verifyRecordPreparation(api, preparation) {
  preparation = structuredClone(preparation);
  if (preparation?.protocol !== protocol) throw new Error('Versioned record preparation protocol required.');
  const args = recordMutationArguments(preparation);
  registerRecordContractTypes(api.registry);
  const { decoded, sender, genesis, payload } = await verifySignedPreparation(api, preparation, protocol);
  const call = decoded.method;
  if (call.section !== 'contracts' || call.method !== 'call' || call.args.length !== 5) {
    throw new Error('Signed original is not a direct contracts.call.');
  }
  const address = api.createType('AccountId32', call.args[0].toString()).toHex();
  if (address !== api.createType('AccountId32', preparation.contractAddress).toHex()
      || call.args[1].toBigInt() !== 0n
      || call.args[4].toHex() !== encodeRecordMessage(api.registry, preparation.action, args)
      || call.args[2].refTime.toString() !== preparation.gasLimit?.refTime
      || call.args[2].proofSize.toString() !== preparation.gasLimit?.proofSize
      || call.args[3].isNone || call.args[3].unwrap().toString() !== preparation.storageDepositLimit) {
    throw new Error('Declared record intent/gas/deposit limit differs from signed original.');
  }
  await readRecordContract(api, { ...preparation, action: 'verify', origin: sender });
  return { verified: true, transactionHash: decoded.hash.toHex(), sender, chainId: genesis,
    contractAddress: address, contractCodeHash: preparation.contractCodeHash, action: preparation.action,
    keyBase64: preparation.keyBase64, operationTagBase64: preparation.operationTagBase64,
    expectation: preparation.expectation, ...(preparation.revision != null ? { revision: preparation.revision } : {}),
    signingCheckpointHash: payload.blockHash, gasLimit: preparation.gasLimit,
    storageDepositLimit: preparation.storageDepositLimit, feeCoverage: 'identity-and-execution-limits-not-total-fee-cap' };
}
