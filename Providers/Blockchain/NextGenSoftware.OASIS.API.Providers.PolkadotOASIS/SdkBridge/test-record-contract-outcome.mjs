import { test } from 'node:test';
import assert from 'node:assert/strict';
import { TypeRegistry } from '@polkadot/types/create';
import { exactRecordEffect, validateRecordOutcome } from './record-contract-outcome.mjs';
import { spawnSync } from 'node:child_process';
import { resolve } from 'node:path';
import { submitOriginalPreparation } from './original-submission.mjs';
const registry = new TypeRegistry();
const api = { registry, createType: (...args) => registry.createType(...args) };
const address = '0x' + '11'.repeat(32);
const tag = Buffer.alloc(32, 2);
const identity = { contractAddress: address, action: 'put', keyBase64: 'AQID',
  operationTagBase64: tag.toString('base64'), expectation: 'Revision', revision: '7' };
function event({ owner = address, operation = tag, key = Buffer.from([1, 2, 3]), revision = 9n, deleted = false, suffix = '' } = {}) {
  const encoded = registry.createType('([u8;32],Bytes,u64,bool)', [
    '0x' + operation.toString('hex'), '0x' + key.toString('hex'), revision, deleted]).toHex();
  return { section: 'contracts', method: 'ContractEmitted', data: [registry.createType('AccountId32', owner),
    registry.createType('Bytes', encoded + suffix)] };
}
test('exact original event proves applied record and native monotonic revision', () => {
  assert.deepEqual(exactRecordEffect(api, identity, [event()], true),
    { applied: true, recordEffectMatchesRequest: true, revision: '9' });
});
test('wrong tag/key/deletion flag/code-address/stale revision cannot acknowledge original', () => {
  for (const variant of [ { operation: Buffer.alloc(32, 3) }, { key: Buffer.from([4]) }, { deleted: true },
    { owner: '0x' + '22'.repeat(32) }, { revision: 7n }, { revision: 0n }, { suffix: '00' } ]) {
    assert.throws(() => exactRecordEffect(api, identity, [event(variant)], true));
  }
  assert.throws(() => exactRecordEffect(api, identity, [event(), event()], true));
});
test('failed dispatch preserves known non-success, not applied acknowledgement', () => {
  assert.deepEqual(exactRecordEffect(api, identity, [], false),
    { applied: false, recordEffectMatchesRequest: false, revision: null });
});
test('only non-revision missing delete may request separate absence proof', () => {
  assert.equal(exactRecordEffect(api, { ...identity, action: 'delete', expectation: 'Missing' }, [], true), null);
  assert.throws(() => exactRecordEffect(api, { ...identity, action: 'delete' }, [], true));
  assert.throws(() => exactRecordEffect(api, identity, [], true));
});
test('outcome lookup requires explicit bounded scan', () => {
  validateRecordOutcome({ maximumBlocks: 32 });
  assert.throws(() => validateRecordOutcome({ maximumBlocks: 0 }));
  assert.throws(() => validateRecordOutcome({ maximumBlocks: 129 }));
});
test('shipping record submission rejects missing durable transport before RPC', () => {
  const env = { ...process.env };
  delete env.OASIS_NATIVE_PREPARATION_PIPE;
  delete env.OASIS_NATIVE_PREPARATION_MODULE;
  const child = spawnSync(process.execPath, [resolve(import.meta.dirname, 'bridge.mjs')], {
    env, input: JSON.stringify({ operation: 'submitRecordPreparation', endpoint: 'http://127.0.0.1:1' }),
    encoding: 'utf8', timeout: 20000, windowsHide: true
  });
  assert.equal(child.error, undefined);
  assert.equal(child.status, 1);
  const result = JSON.parse(child.stdout);
  assert.equal(result.ok, false);
  assert.match(result.error, /request validation.*Owned persistence transport/);
});
test('failed persistence never receives original submission capability', async () => {
  let submitted = 0;
  const failure = new Error('durable owner rejected evidence');
  await assert.rejects(submitOriginalPreparation({}, { transactionHash: 'original' },
    async () => { throw failure; }, async () => { submitted++; }, async () => ({ transactionHash: 'original' })),
    error => error === failure);
  assert.equal(submitted, 0);
});
