// Explicit oasis-psp34 extensions. Native beneficiary escrow requires v0.2.0.
// Mint/burn/lock/unlock are NOT standard PSP34 ABI.
// Reuse standard token/account encoding; no signing, RPC or submission here.
import { encodePsp34Arguments, encodePsp34Message, decodePsp34Result, registerPsp34Types } from './psp34-contract-codec.mjs';

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
  if (['burn', 'lock', 'unlock'].includes(action)) {
    if (recipient !== undefined) throw new Error('Native burn/escrow call has no arbitrary recipient.');
    const selector = { burn: '00000011', lock: '00000012', unlock: '00000013' }[action];
    return '0x' + selector + Buffer.from(encodePsp34Arguments(registry, ['id'], [tokenId])).toString('hex');
  }
  if (action === 'transfer') return encodePsp34Message(registry, 'transfer', [recipient, tokenId, new Uint8Array()]);
  throw new Error('Explicit OASIS PSP34 mutation required.');
}

export function encodeOasisPsp34EscrowQuery(registry, tokenId) {
  tokenBound(tokenId);
  return '0x00000014' + Buffer.from(encodePsp34Arguments(registry, ['id'], [tokenId])).toString('hex');
}

export function decodeOasisPsp34MutationResult(registry, action, bytes) {
  if (!['mint', 'transfer', 'burn', 'lock', 'unlock'].includes(action)) throw new Error('Unknown OASIS PSP34 mutation.');
  // Every mutation returns Result<(),PSP34Error> inside ink's language result.
  return decodePsp34Result(registry, 'transfer', bytes);
}

// Recover intent from the EXACT signed call, not an unpersisted JSON sidecar.
export function decodeOasisPsp34Mutation(registry, input) {
  if (typeof input !== 'string' || !/^0x(?:[0-9a-f]{2}){4,}$/.test(input)) throw new Error('Canonical NFT call bytes required.');
  const specification = { '00000010': ['mint', '(AccountId32,OasisPsp34Id,Vec<(Bytes,Bytes)>)'],
    '3128d61b': ['transfer', '(AccountId32,OasisPsp34Id,Bytes)'], '00000011': ['burn', 'OasisPsp34Id'],
    '00000012': ['lock', 'OasisPsp34Id'], '00000013': ['unlock', 'OasisPsp34Id'] }[input.slice(2, 10)];
  if (!specification) throw new Error('Signed call is not a reviewed NFT mutation selector.');
  registerPsp34Types(registry);
  const [action, type] = specification;
  const raw = Buffer.from(input.slice(10), 'hex'), args = registry.createType(type, raw);
  if (!Buffer.from(args.toU8a()).equals(raw)) throw new Error('Noncanonical NFT call or trailing data.');
  const identityOnly = ['burn', 'lock', 'unlock'].includes(action);
  const id = identityOnly ? args : args[1];
  const variant = ['U8', 'U16', 'U32', 'U64', 'U128', 'Bytes'][id.index];
  const tokenId = { [variant]: variant === 'Bytes' ? id.value.toHex() : id.value.toString() };
  if (action === 'transfer' && args[2].toU8a(true).length !== 0)
    throw new Error('Reviewed NFT transfer recovery cannot discard receiver callback data.');
  const to = identityOnly ? undefined : args[0].toHex();
  const attributes = action === 'mint' ? [...args[2]].map(pair =>
    [Buffer.from(pair[0].toU8a(true)).toString('base64'), Buffer.from(pair[1].toU8a(true)).toString('base64')]) : undefined;
  const typed = variant === 'Bytes' ? { Bytes: Uint8Array.from(Buffer.from(tokenId.Bytes.slice(2), 'hex')) } : tokenId;
  const binary = attributes?.map(([key, value]) => [Uint8Array.from(Buffer.from(key, 'base64')), Uint8Array.from(Buffer.from(value, 'base64'))]);
  if (encodeOasisPsp34Mutation(registry, action, typed, to, binary) !== input)
    throw new Error('Recovered NFT intent does not round-trip the reviewed ABI.');
  return { action, tokenId, ...(to !== undefined ? { to } : {}), ...(attributes !== undefined ? { attributes } : {}) };
}
