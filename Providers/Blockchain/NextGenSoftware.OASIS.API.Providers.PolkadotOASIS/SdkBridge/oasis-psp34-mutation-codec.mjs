// Explicit oasis-psp34 v0.1.0 extensions. Mint/burn are NOT standard PSP34 ABI.
// Reuse standard token/account encoding; no signing, RPC or submission here.
import { encodePsp34Arguments, encodePsp34Message, decodePsp34Result } from './psp34-contract-codec.mjs';

function tokenBound(token) {
  if (token?.Bytes instanceof Uint8Array && token.Bytes.length > 1024)
    throw new Error('OASIS PSP34 byte identity exceeds1024bytes.');
}
function attributes(registry, values) {
  if (!Array.isArray(values) || values.length > 16) throw new Error('OASIS PSP34 requires at most16 attributes.');
  const keys = new Set();
  const normalized = values.map(pair => {
    if (!Array.isArray(pair) || pair.length !== 2 || !(pair[0] instanceof Uint8Array)
        || !(pair[1] instanceof Uint8Array) || pair[0].length === 0 || pair[0].length > 128 || pair[1].length > 8192)
      throw new Error('OASIS PSP34 attributes require nonempty byte keys<=128 and byte values<=8192.');
    const key = '0x' + Buffer.from(pair[0]).toString('hex');
    if (keys.has(key)) throw new Error('Duplicate OASIS PSP34 attribute key.');
    keys.add(key);
    return [key, '0x' + Buffer.from(pair[1]).toString('hex')];
  });
  return registry.createType('Vec<(Bytes,Bytes)>', normalized).toU8a();
}

export function encodeOasisPsp34Mutation(registry, action, tokenId, recipient, values) {
  tokenBound(tokenId);
  if (action === 'mint') {
    const prefix = encodePsp34Arguments(registry, ['account', 'id'], [recipient, tokenId]);
    return '0x00000010' + Buffer.concat([prefix, attributes(registry, values)]).toString('hex');
  }
  if (values !== undefined) throw new Error('Only mint may supply token attributes.');
  if (action === 'burn') {
    if (recipient !== undefined) throw new Error('Burn has no recipient.');
    return '0x00000011' + Buffer.from(encodePsp34Arguments(registry, ['id'], [tokenId])).toString('hex');
  }
  if (action === 'transfer') return encodePsp34Message(registry, 'transfer', [recipient, tokenId, new Uint8Array()]);
  throw new Error('Explicit OASIS PSP34 mint, transfer or burn required.');
}

export function decodeOasisPsp34MutationResult(registry, action, bytes) {
  if (!['mint', 'transfer', 'burn'].includes(action)) throw new Error('Unknown OASIS PSP34 mutation.');
  // All three return the same Result<(),PSP34Error> inside ink's language result.
  return decodePsp34Result(registry, 'transfer', bytes);
}
