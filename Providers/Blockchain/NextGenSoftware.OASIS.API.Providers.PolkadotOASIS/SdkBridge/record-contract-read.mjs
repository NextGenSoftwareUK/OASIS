import { canonicalBase64 } from './verify-preparation.mjs';
import { registerRecordContractTypes, encodeRecordMessage, decodeRecordResult } from './record-contract-codec.mjs';

export function validateRecordRead(request) {
  if (!['get', 'list', 'verify'].includes(request.action)) throw new Error('Unknown contract read action.');
  if (typeof request.contractAddress !== 'string' || !request.contractAddress.trim()
      || typeof request.origin !== 'string' || !request.origin.trim()
      || !/^0x[0-9a-f]{64}$/i.test(request.contractCodeHash)) {
    throw new Error('Explicit contract address, read origin and pinned code hash are required.');
  }
  if (request.action !== 'verify') {
    const bytes = request.action === 'list' && request.keyBase64 === ''
      ? new Uint8Array() : canonicalBase64(request.keyBase64, 1024);
    if (bytes.length > 1024 || (request.action === 'get' && bytes.length === 0)) throw new Error('Invalid record key.');
    return bytes;
  }
  return new Uint8Array();
}

// One immutable finalized observation for the entire enumeration, including
// code identity and all cursor pages. Never mix latest-state pages or retry a
// failed page into a different snapshot.
export async function readRecordContract(api, request) {
  request = structuredClone(request);
  const key = validateRecordRead(request);
  registerRecordContractTypes(api.registry);
  const address = api.createType('AccountId32', request.contractAddress).toHex();
  const origin = api.createType('AccountId32', request.origin).toHex();
  const finalized = await api.rpc.chain.getFinalizedHead();
  const at = await api.at(finalized);
  if (!at.query.contracts?.contractInfoOf || !at.call.contractsApi?.call) {
    throw new Error('Pinned runtime does not expose pallet-contracts storage and call simulation.');
  }
  const info = await at.query.contracts.contractInfoOf(address);
  if (info.isNone) throw new Error('Configured record contract does not exist at finalized snapshot.');
  if (info.unwrap().codeHash.toHex().toLowerCase() !== request.contractCodeHash.toLowerCase()) {
    throw new Error('Record contract code differs from the explicitly pinned artifact.');
  }
  async function query(name, args) {
    const response = await at.call.contractsApi.call(origin, address, 0, null, null,
      encodeRecordMessage(api.registry, name, args));
    if (response.result.isErr) throw new Error(`Native contract read failed: ${response.result.asErr.toString()}`);
    const result = response.result.asOk;
    if (result.flags.get('bits').toNumber() !== 0) throw new Error('Contract read reverted or returned unsupported flags.');
    return decodeRecordResult(api.registry, name, result.data.toU8a(true));
  }
  const identity = { blockHash: finalized.toHex(), contractAddress: address, codeHash: request.contractCodeHash.toLowerCase() };
  if (request.action === 'get') {
    const cell = await query('get', [key]);
    return { ...identity, observation: cell.isNone ? null : {
      valueBase64: Buffer.from(cell.unwrap().value.toU8a(true)).toString('base64'),
      revision: cell.unwrap().revision.toString()
    } };
  }
  let cursor = 0n, generation;
  const keysBase64 = [], seen = new Set();
  do {
    const page = await query('keys', [key, cursor, 8]);
    const observed = page.generation.toString();
    if (generation != null && generation !== observed) throw new Error('Contract enumeration changed within pinned snapshot.');
    generation = observed;
    for (const entry of page.get('keys')) {
      const bytes = Buffer.from(entry.toU8a(true));
      if (bytes.length === 0 || bytes.length > 1024 || !bytes.subarray(0, key.length).equals(Buffer.from(key))) {
        throw new Error('Contract returned an invalid or out-of-prefix key.');
      }
      const encoded = bytes.toString('base64');
      if (seen.has(encoded)) throw new Error('Contract returned a duplicate key.');
      seen.add(encoded);
      keysBase64.push(encoded);
    }
    const next = page.get('next');
    if (next.isNone || request.action === 'verify') break;
    const position = next.unwrap().toBigInt();
    if (position <= cursor || position > cursor + 8n) throw new Error('Contract cursor did not advance within bounded page.');
    cursor = position;
  } while (true);
  return request.action === 'verify'
    ? { ...identity, verified: true }
    : { ...identity, generation, keysBase64 };
}
