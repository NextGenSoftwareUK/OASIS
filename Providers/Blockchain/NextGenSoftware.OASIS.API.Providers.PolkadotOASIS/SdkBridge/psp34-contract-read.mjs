import { openPinnedContractQuery } from './pinned-contract-query.mjs';
import { encodePsp34Message, decodePsp34Result } from './psp34-contract-codec.mjs';
import { encodeOasisPsp34EscrowQuery } from './oasis-psp34-mutation-codec.mjs';
import { nftTokenId } from './psp34-contract-mutation.mjs';

// Explicit v0.2 capability: owner, active beneficiary and supply at one snapshot.
// An older contract cannot silently fall back to owner/minter metadata.
export async function readPsp34EscrowState(api, request) {
  request = structuredClone(request);
  const id = nftTokenId(request.tokenId);
  const ownerCall = encodePsp34Message(api.registry, 'ownerOf', [id]);
  const beneficiaryCall = encodeOasisPsp34EscrowQuery(api.registry, id);
  const supplyCall = encodePsp34Message(api.registry, 'totalSupply', []);
  const view = await openPinnedContractQuery(api, request);
  const owner = decodePsp34Result(api.registry, 'ownerOf',
    await view.query(ownerCall));
  const beneficiary = decodePsp34Result(api.registry, 'ownerOf',
    await view.query(beneficiaryCall));
  const supply = decodePsp34Result(api.registry, 'totalSupply',
    await view.query(supplyCall));
  const account = value => value.isNone ? null : api.createType('AccountId32', value.unwrap()).toHex();
  return { ...view.identity, tokenId: request.tokenId, owner: account(owner),
    escrowBeneficiary: account(beneficiary), totalSupply: supply.toString() };
}

// Public collection metadata has no implicit token ID. Every attribute and the
// collection identity are observed at the SAME finalized code-pinned snapshot.
export async function readPsp34Collection(api, request) {
  const view = await openPinnedContractQuery(api, structuredClone(request));
  const query = async (method, args) => decodePsp34Result(api.registry, method,
    await view.query(encodePsp34Message(api.registry, method, args)));
  const collection = await query('collectionId', []);
  const json = collection.toJSON();
  // Codec normalization deliberately accepts explicit enum variants only.
  // Registry JSON uses lower-case names; keep the SCALE variant and exact value.
  const variant = ['U8', 'U16', 'U32', 'U64', 'U128', 'Bytes'][collection.index];
  const value = collection.value;
  const id = { [variant]: variant === 'Bytes' ? value.toU8a(true) : value.toString() };
  const attributes = {};
  for (const key of ['name', 'symbol', 'description', 'image']) {
    const result = await query('getAttribute', [id, Buffer.from(key, 'utf8')]);
    attributes[key] = result.isNone ? null : new TextDecoder('utf-8', { fatal: true }).decode(result.unwrap().toU8a(true));
  }
  return { ...view.identity, collectionId: json, attributes };
}
