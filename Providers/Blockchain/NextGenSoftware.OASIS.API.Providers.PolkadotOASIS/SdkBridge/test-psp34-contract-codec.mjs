import { test } from 'node:test';
import assert from 'node:assert/strict';
import { TypeRegistry } from '@polkadot/types/create';
import { encodePsp34Message, decodePsp34Result } from './psp34-contract-codec.mjs';

const account = '0x' + 'ab'.repeat(32);
for (const [variant, value, encoded] of [
  ['U8', '255', '00ff'], ['U16', '65535', '01ffff'], ['U32', '4294967295', '02ffffffff'],
  ['U64', '18446744073709551615', '03' + 'ff'.repeat(8)],
  ['U128', '340282366920938463463374607431768211455', '04' + 'ff'.repeat(16)],
  ['Bytes', new Uint8Array([1, 2, 255]), '050c0102ff']
]) test(`published PSP34 owner selector and exact ${variant} token identity`, () => {
  const registry = new TypeRegistry();
  assert.equal(encodePsp34Message(registry, 'ownerOf', [{ [variant]: value }]), '0x1168624d' + encoded);
});
test('published transfer selector and binary tuple do not truncate multi-byte values', () => {
  const actual = encodePsp34Message(new TypeRegistry(), 'transfer', [account, { Bytes: new Uint8Array([1, 2, 3]) }, new Uint8Array([4, 5])]);
  assert.equal(actual, '0x3128d61b' + 'ab'.repeat(32) + '050c010203080405');
});
test('optional approval identity keeps collection-wide None distinct from an empty byte identity', () => {
  const registry = new TypeRegistry();
  assert.equal(encodePsp34Message(registry, 'approve', [account, null, false]), '0x1932a8b0' + 'ab'.repeat(32) + '0000');
  assert.equal(encodePsp34Message(registry, 'approve', [account, { Bytes: new Uint8Array() }, true]),
    '0x1932a8b0' + 'ab'.repeat(32) + '01050001');
});
test('rejects ambiguous, out-of-range, imprecise and noncanonical token identities', () => {
  for (const value of [{ U8: '256' }, { U128: '340282366920938463463374607431768211456' },
    { U64: 9007199254740993 }, { U8: '01' }, { U8: '-1' }, { U8: '1', U16: '1' }, {},
    { Unknown: '1' }, { Bytes: '0x0102' }, null])
    assert.throws(() => encodePsp34Message(new TypeRegistry(), 'ownerOf', [value]));
});
test('rejects inferred addresses, missing arguments and truthy approval values', () => {
  const registry = new TypeRegistry();
  assert.throws(() => encodePsp34Message(registry, 'balanceOf', ['5A-not-a-canonical-account']));
  assert.throws(() => encodePsp34Message(registry, 'collectionId', [null]));
  assert.throws(() => encodePsp34Message(registry, 'approve', [account, null, 1]));
  assert.throws(() => encodePsp34Message(registry, 'inventedMint', []));
});
test('decodes absent owner and exact u128 supply without conversion to Number', () => {
  const registry = new TypeRegistry();
  assert.equal(decodePsp34Result(registry, 'ownerOf', new Uint8Array([0, 0])).isNone, true);
  const max = new Uint8Array([0, ...Array(16).fill(255)]);
  assert.equal(decodePsp34Result(registry, 'totalSupply', max).toString(), '340282366920938463463374607431768211455');
});
test('language/application errors and trailing bytes never become successful acknowledgements', () => {
  const registry = new TypeRegistry();
  assert.throws(() => decodePsp34Result(registry, 'ownerOf', new Uint8Array([1, 1])), /language failure/);
  assert.throws(() => decodePsp34Result(registry, 'transfer', new Uint8Array([0, 1, 2])), /application failure/);
  assert.throws(() => decodePsp34Result(registry, 'ownerOf', new Uint8Array([0, 0, 0])), /trailing/);
  assert.throws(() => decodePsp34Result(registry, 'ownerOf', new Uint8Array()));
  assert.equal(decodePsp34Result(registry, 'transfer', new Uint8Array([0, 0])).isOk, true);
});
