import assert from 'node:assert/strict';
import { test } from 'node:test';
import { TypeRegistry } from '@polkadot/types/create';
import { registerRecordContractTypes, encodeRecordMessage, decodeRecordResult } from './record-contract-codec.mjs';

function registry() { const r = new TypeRegistry(); registerRecordContractTypes(r); return r; }

test('selectors and SCALE arguments match explicit Rust ABI vectors', () => {
  const r = registry();
  assert.equal(encodeRecordMessage(r, 'new', []), '0x00000001');
  assert.equal(encodeRecordMessage(r, 'get', [Uint8Array.of(0xab)]), '0x0000000204ab');
  assert.equal(encodeRecordMessage(r, 'get', [Uint8Array.of(1, 2, 3)]), '0x000000020c010203');
  assert.equal(encodeRecordMessage(r, 'put', [Uint8Array.of(0xab), Uint8Array.of(0xcd), { Revision: 258n }, new Uint8Array(32)]),
    '0x0000000304ab04cd020201000000000000' + '00'.repeat(32));
  assert.equal(encodeRecordMessage(r, 'delete', [Uint8Array.of(0xab), 'Missing', new Uint8Array(32)]),
    '0x0000000404ab01' + '00'.repeat(32));
  assert.equal(encodeRecordMessage(r, 'keys', [new Uint8Array(), 0n, 8]), '0x0000000500000000000000000008');
});

test('rejects invalid admission before constructing calls', () => {
  const r = registry();
  assert.throws(() => encodeRecordMessage(r, 'get', [new Uint8Array()]), /key length/);
  assert.throws(() => encodeRecordMessage(r, 'get', ['0xab']), /Uint8Array/);
  assert.throws(() => encodeRecordMessage(r, 'put', [Uint8Array.of(1), new Uint8Array(8193), 'Any', new Uint8Array(32)]), /8192/);
  assert.throws(() => encodeRecordMessage(r, 'delete', [Uint8Array.of(1), { Revision: 9007199254740992 }, new Uint8Array(32)]), /expectation/);
  assert.throws(() => encodeRecordMessage(r, 'delete', [Uint8Array.of(1), 'Any', new Uint8Array(31)]), /32 bytes/);
  assert.throws(() => encodeRecordMessage(r, 'keys', [new Uint8Array(), 0, 8]), /page bounds/);
  assert.throws(() => encodeRecordMessage(r, 'keys', [new Uint8Array(), 0n, 9]), /page bounds/);
});

test('nested ink and application acknowledgements preserve exact revision', () => {
  const r = registry();
  assert.equal(decodeRecordResult(r, 'put', '0x00000201000000000000').toString(), '258');
  assert.equal(decodeRecordResult(r, 'delete', '0x000000').isNone, true);
  const cell = decodeRecordResult(r, 'get', '0x00000104ab0201000000000000').unwrap();
  assert.equal(cell.value.toHex(), '0xab');
  assert.equal(cell.revision.toString(), '258');
});

test('rejects application conflicts, language errors and malformed output', () => {
  const r = registry();
  assert.throws(() => decodeRecordResult(r, 'put', '0x000103'), e => e.contractError === 'Conflict');
  assert.throws(() => decodeRecordResult(r, 'put', '0x0101'), /language error/);
  assert.throws(() => decodeRecordResult(r, 'put', '0x0000020100000000000000'), /trailing/);
  assert.throws(() => decodeRecordResult(r, 'put', new Uint8Array()), /nonempty/);
  assert.throws(() => encodeRecordMessage(r, 'constructor', []), /Unknown/);
});
