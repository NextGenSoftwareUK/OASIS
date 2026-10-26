import { TypeRegistry } from '@polkadot/types/create';
import { validateExplicitSigner, withExplicitSigner, signOriginalCall, withOriginalFeeEstimate } from './native-transfer.mjs';
import { verifySignedPreparation } from './verify-preparation.mjs';
import { validatePinnedContractQuery, openPinnedContractQuery } from './pinned-contract-query.mjs';
import { readTokenEscrowDeposit } from './psp22-escrow-read.mjs';
import { encodeTokenEscrowMutation, decodeTokenEscrowMutation, decodeTokenEscrowResult } from './oasis-psp22-escrow-codec.mjs';

const protocol = 'oasis.polkadot.psp22-escrow.preparation.v1';
function data(registry, request) {
  if (request.action === 'unlock') {
    if (request.amountUnits !== undefined || request.to !== undefined)
      throw new Error('Unlock releases only the complete original deposit to its recorded owner.');
    return encodeTokenEscrowMutation(registry, { action: request.action, depositId: request.depositId });
  }
  return encodeTokenEscrowMutation(registry, request);
}
function token(request) {
  if (typeof request.tokenAddress !== 'string' || !/^0x[0-9a-f]{64}$/.test(request.tokenAddress)
      || request.tokenAddress === '0x' + '00'.repeat(32))
    throw new Error('Explicit canonical nonzero PSP22 token account required.');
}
export async function validateTokenEscrowMutation(request) {
  data(new TypeRegistry(), request); token(request);
  validatePinnedContractQuery({ ...request, origin: request.sender });
  for (const field of ['sender', 'recipient', 'contractAddress'])
    if (!/^0x[0-9a-f]{64}$/.test(request[field])) throw new Error('Canonical escrow account identities required.');
  if (request.recipient !== request.contractAddress || request.tokenAddress === request.contractAddress)
    throw new Error('Escrow signing recipient must be the reviewed escrow, distinct from the PSP22 token.');
  if (request.memo != null && request.memo !== '') throw new Error('Escrow ABI has no memo argument.');
  await validateExplicitSigner(request);
}

export async function prepareTokenEscrowMutation(api, request) {
  request = structuredClone(request);
  await validateTokenEscrowMutation(request);
  const observed = await readTokenEscrowDeposit(api, { ...request, origin: request.sender });
  if (request.action === 'lock' ? observed.deposit !== null : observed.deposit === null
      || observed.deposit.owner !== request.sender || observed.deposit.tokenAddress !== request.tokenAddress || observed.deposit.released)
    throw new Error('Original token deposit does not authorize this lock/unlock request.');
  if (!api.tx.contracts?.call) throw new Error('Runtime has no contracts.call operation.');
  const encoded = data(api.registry, request);
  return withExplicitSigner(api, request, async (pair, sender) => {
    const simulation = await api.call.contractsApi.call(sender, request.contractAddress, 0, null, null, encoded);
    if (simulation.result.isErr) throw new Error(`Native token escrow simulation failed: ${simulation.result.asErr.toString()}`);
    const result = simulation.result.asOk;
    decodeTokenEscrowResult(api.registry, 'mutation', result.data.toU8a(true));
    if (result.flags.get('bits').toNumber() !== 0) throw new Error('Token escrow simulation reverted.');
    const storageDepositLimit = simulation.storageDeposit.isCharge ? simulation.storageDeposit.asCharge.toString() : '0';
    const gasLimit = { refTime: simulation.gasRequired.refTime.toString(), proofSize: simulation.gasRequired.proofSize.toString() };
    const tx = api.tx.contracts.call(request.contractAddress, 0, gasLimit, storageDepositLimit, encoded);
    const intent = { action: request.action, depositId: request.depositId, tokenAddress: request.tokenAddress,
      ...(request.action === 'lock' ? { amountUnits: request.amountUnits } : {}),
      sender, contractAddress: request.contractAddress, contractCodeHash: request.contractCodeHash.toLowerCase(), gasLimit, storageDepositLimit };
    const prepared = await signOriginalCall(api, tx, pair, protocol, intent);
    await verifyTokenEscrowPreparation(api, prepared);
    return withOriginalFeeEstimate(api, prepared);
  });
}

function directCall(api, decoded) {
  const call = decoded.method;
  if (call.section !== 'contracts' || call.method !== 'call' || call.args.length !== 5
      || call.args[1].toBigInt() !== 0n || call.args[3].isNone)
    throw new Error('Original is not a direct zero-value escrow contracts.call with a signed storage limit.');
  return { call, address: api.createType('AccountId32', call.args[0].toString()).toHex(),
    gasLimit: { refTime: call.args[2].refTime.toString(), proofSize: call.args[2].proofSize.toString() },
    storageDepositLimit: call.args[3].unwrap().toString() };
}

export async function verifyTokenEscrowPreparation(api, preparation) {
  preparation = structuredClone(preparation);
  if (preparation?.protocol !== protocol) throw new Error('Versioned token escrow preparation required.');
  const encoded = data(api.registry, preparation); token(preparation);
  const { decoded, sender, genesis, payload } = await verifySignedPreparation(api, preparation, protocol);
  validatePinnedContractQuery({ ...preparation, origin: sender });
  const original = directCall(api, decoded);
  if (original.address !== preparation.contractAddress || original.call.args[4].toHex() !== encoded
      || original.gasLimit.refTime !== preparation.gasLimit?.refTime
      || original.gasLimit.proofSize !== preparation.gasLimit?.proofSize
      || original.storageDepositLimit !== preparation.storageDepositLimit)
    throw new Error('Declared token escrow intent or execution limits differ from the signed original.');
  let amountUnits = preparation.amountUnits;
  if (preparation.action === 'unlock') {
    const observed = await readTokenEscrowDeposit(api, { ...preparation, origin: sender });
    // Released tombstones preserve immutable owner/token/amount for recovery;
    // verification must not demand that a previously submitted release is fresh.
    if (!observed.deposit || observed.deposit.owner !== sender || observed.deposit.tokenAddress !== preparation.tokenAddress)
      throw new Error('Signed release does not identify the original owned token deposit.');
    amountUnits = observed.deposit.amountUnits;
  } else await openPinnedContractQuery(api, { ...preparation, origin: sender });
  return { verified: true, transactionHash: decoded.hash.toHex(), sender, chainId: genesis,
    action: preparation.action, depositId: preparation.depositId, tokenAddress: preparation.tokenAddress, amountUnits,
    contractAddress: original.address, contractCodeHash: preparation.contractCodeHash.toLowerCase(),
    signingCheckpointHash: payload.blockHash, gasLimit: original.gasLimit, storageDepositLimit: original.storageDepositLimit,
    feeCoverage: 'identity-and-execution-limits-not-total-fee-cap' };
}

export async function recoverTokenEscrowPreparation(api, request) {
  request = structuredClone(request);
  if (typeof request.contractCodeHash !== 'string' || !/^0x[0-9a-f]{64}$/.test(request.contractCodeHash))
    throw new Error('Explicit reviewed escrow code hash required for recovery.');
  const original = request.preparation;
  const { decoded, sender, genesis } = await verifySignedPreparation(api, original, protocol);
  const call = directCall(api, decoded);
  const intent = decodeTokenEscrowMutation(api.registry, call.call.args[4].toHex());
  if (intent.action === 'unlock') {
    const observed = await readTokenEscrowDeposit(api, { contractAddress: call.address,
      contractCodeHash: request.contractCodeHash, origin: sender, depositId: intent.depositId });
    if (!observed.deposit || observed.deposit.owner !== sender) throw new Error('Original owned deposit cannot be recovered.');
    intent.tokenAddress = observed.deposit.tokenAddress;
  }
  const recovered = { protocol, chainId: genesis, sender, transactionHash: decoded.hash.toHex(), ...intent,
    signedTransactionBase64: original.signedTransactionBase64, signDocBase64: original.signDocBase64, signature: original.signature,
    contractAddress: call.address, contractCodeHash: request.contractCodeHash, gasLimit: call.gasLimit,
    storageDepositLimit: call.storageDepositLimit, recoveredFromSignedOriginal: true, reconciliationRequired: true };
  await verifyTokenEscrowPreparation(api, recovered);
  return recovered;
}
