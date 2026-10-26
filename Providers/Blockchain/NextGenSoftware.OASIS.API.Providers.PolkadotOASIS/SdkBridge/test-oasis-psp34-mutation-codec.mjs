import { test } from 'node:test';
import assert from 'node:assert/strict';
import { TypeRegistry } from '@polkadot/types/create';
import { encodeOasisPsp34Mutation, decodeOasisPsp34MutationResult } from './oasis-psp34-mutation-codec.mjs';
const account = '0x' + 'ab'.repeat(32);
test('mint extension has exact account, variant and independent metadata byte encoding', () => {
  assert.equal(encodeOasisPsp34Mutation(new TypeRegistry(), 'mint', { U8: '7' }, account,
    [[new Uint8Array([65]), new Uint8Array([0, 255])]]),
    '0x00000010' + 'ab'.repeat(32) + '00070404410800ff');
});
test('burn extension preserves u128 identity and transfer retains the standard selector', () => {
  assert.equal(encodeOasisPsp34Mutation(new TypeRegistry(), 'burn', { U128: '340282366920938463463374607431768211455' }),
    '0x0000001104' + 'ff'.repeat(16));
  assert.equal(encodeOasisPsp34Mutation(new TypeRegistry(), 'transfer', { Bytes: new Uint8Array([1, 2]) }, account),
    '0x3128d61b' + 'ab'.repeat(32) + '0508010200');
});
test('extension bounds match the retained contract without string coercion or silent attribute loss', () => {
  const registry = new TypeRegistry();
  for (const values of [undefined, [[new Uint8Array(), new Uint8Array()]], [['name', 'value']],
    [[new Uint8Array(129), new Uint8Array()]], [[new Uint8Array([1]), new Uint8Array(8193)]],
    Array.from({ length: 17 }, (_, i) => [new Uint8Array([i]), new Uint8Array()]),
    [[new Uint8Array([1]), new Uint8Array()], [new Uint8Array([1]), new Uint8Array([2])]]])
    assert.throws(() => encodeOasisPsp34Mutation(registry, 'mint', { U8: '1' }, account, values));
  assert.throws(() => encodeOasisPsp34Mutation(registry, 'mint', { Bytes: new Uint8Array(1025) }, account, []));
  assert.throws(() => encodeOasisPsp34Mutation(registry, 'transfer', { U8: '1' }, account, []));
  assert.throws(() => encodeOasisPsp34Mutation(registry, 'burn', { U8: '1' }, account));
  assert.throws(() => encodeOasisPsp34Mutation(registry, 'invented', { U8: '1' }));
});
test('native application/language errors and trailing data cannot acknowledge a mutation', () => {
  const registry = new TypeRegistry();
  for (const action of ['mint', 'transfer', 'burn']) {
    assert.equal(decodeOasisPsp34MutationResult(registry, action, new Uint8Array([0, 0])).isOk, true);
    assert.throws(() => decodeOasisPsp34MutationResult(registry, action, new Uint8Array([0, 1, 2])), /application failure/);
    assert.throws(() => decodeOasisPsp34MutationResult(registry, action, new Uint8Array([1, 1])), /language failure/);
    assert.throws(() => decodeOasisPsp34MutationResult(registry, action, new Uint8Array([0, 0, 0])), /trailing/);
  }
});
