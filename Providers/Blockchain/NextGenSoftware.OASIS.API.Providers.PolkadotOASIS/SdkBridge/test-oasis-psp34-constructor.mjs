import { test } from 'node:test';
import assert from 'node:assert/strict';
import { TypeRegistry } from '@polkadot/types/create';
import { encodeOasisPsp34Constructor } from './oasis-psp34-constructor.mjs';
const metadata = () => ({ name: new Uint8Array([65, 66]), symbol: new Uint8Array([84]),
  description: new Uint8Array(), image: new Uint8Array([255, 0]) });
test('explicit constructor selector preserves the four independent binary fields', () => {
  assert.equal(encodeOasisPsp34Constructor(new TypeRegistry(), metadata()), '0x0000000108414204540008ff00');
});
test('empty optional fields stay empty Bytes, not absent options or guessed defaults', () => {
  const empty = Object.fromEntries(['name', 'symbol', 'description', 'image'].map(key => [key, new Uint8Array()]));
  assert.equal(encodeOasisPsp34Constructor(new TypeRegistry(), empty), '0x0000000100000000');
});
test('byte bounds match the contract and large compact lengths round-trip without truncation', () => {
  const registry = new TypeRegistry(), input = metadata();input.name = new Uint8Array(8192).fill(171);
  const encoded = encodeOasisPsp34Constructor(registry, input);
  const decoded = registry.createType('(Bytes,Bytes,Bytes,Bytes)', Buffer.from(encoded.slice(10), 'hex'));
  assert.deepEqual(decoded[0].toU8a(true), input.name);
  assert.deepEqual(decoded[3].toU8a(true), input.image);
  input.name = new Uint8Array(8193);
  assert.throws(() => encodeOasisPsp34Constructor(registry, input), /8192/);
});
test('missing/extra fields and implicit string/array encodings fail before native I/O', () => {
  for (const change of [input => delete input.image, input => input.owner = 'invented-minter',
    input => input.name = 'Tree', input => input.name = [1, 2], input => input.image = null]) {
    const input = metadata();change(input);
    assert.throws(() => encodeOasisPsp34Constructor(new TypeRegistry(), input));
  }
});
