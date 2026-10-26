// One finalized code-pinned view for any reviewed pallet-contracts ABI.
// Callers own ABI encoding/decoding. No signing, submission, retry or fallback.
export function validatePinnedContractQuery(request) {
  for (const field of ['contractAddress', 'origin', 'contractCodeHash']) {
    if (typeof request[field] !== 'string' || !/^0x[0-9a-fA-F]{64}$/.test(request[field]))
      throw new Error('Explicit canonical contract/origin/code identity required.');
  }
}
export async function openPinnedContractQuery(api, request) {
  validatePinnedContractQuery(request);
  const address = api.createType('AccountId32', request.contractAddress).toHex();
  const origin = api.createType('AccountId32', request.origin).toHex();
  const codeHash = request.contractCodeHash.toLowerCase();
  const finalized = await api.rpc.chain.getFinalizedHead();
  const at = await api.at(finalized);
  if (!at.query.contracts?.contractInfoOf || !at.call.contractsApi?.call)
    throw new Error('Pinned runtime does not expose pallet-contracts storage and call simulation.');
  const info = await at.query.contracts.contractInfoOf(address);
  if (info.isNone) throw new Error('Configured contract does not exist at finalized snapshot.');
  if (info.unwrap().codeHash.toHex().toLowerCase() !== codeHash)
    throw new Error('Contract code differs from the explicitly pinned artifact.');
  return {
    identity: Object.freeze({ blockHash: finalized.toHex(), contractAddress: address, codeHash }),
    query: async input => {
      if (typeof input !== 'string' || !/^0x(?:[0-9a-fA-F]{2}){4,}$/.test(input))
        throw new Error('Reviewed contract selector and canonical input bytes required.');
      const response = await at.call.contractsApi.call(origin, address, 0, null, null, input);
      if (response.result.isErr) throw new Error(`Native contract read failed: ${response.result.asErr.toString()}`);
      const result = response.result.asOk;
      if (result.flags.get('bits').toNumber() !== 0) throw new Error('Contract read reverted or returned unsupported flags.');
      return result.data.toU8a(true);
    }
  };
}
