import { test } from 'node:test';
import assert from 'node:assert/strict';
import { recordMutationArguments, validateRecordMutation } from './record-contract-mutation.mjs';

const base = { action: 'put', keyBase64: 'AQID', valueBase64: '', expectation: 'Missing',
  operationTagBase64: Buffer.alloc(32, 1).toString('base64'),
  contractAddress: '0x' + '11'.repeat(32), contractCodeHash: '0x' + '22'.repeat(32),
  sender: '0x' + '33'.repeat(32), recipient: '0x' + '11'.repeat(32), privateKey: '0x' + '02'.repeat(32) };
test('exact binary mutation admits empty value and u64 expectation without precision loss', async () => {
  await validateRecordMutation(base);
  const args = recordMutationArguments({ ...base, expectation: 'Revision', revision: '9007199254740993' });
  assert.deepEqual([...args[0]], [1, 2, 3]);
  assert.equal(args[1].length, 0);
  assert.equal(args[2].Revision, 9007199254740993n);
});
test('rejects absent, noncanonical and oversized binary input before native I/O', () => {
  for (const patch of [{ keyBase64: '' }, { keyBase64: 'AQ' }, { valueBase64: null },
    { valueBase64: Buffer.alloc(8193).toString('base64') }, { operationTagBase64: 'AQ==' }]) {
    assert.throws(() => recordMutationArguments({ ...base, ...patch }));
  }
});
test('rejects ambiguous/unbounded revision and discarded delete value', () => {
  for (const patch of [{ revision: '1' }, { expectation: null },
    { expectation: 'Revision', revision: '01' }, { expectation: 'Revision', revision: 1 },
    { expectation: 'Revision', revision: '18446744073709551616' }, { action: 'delete' }]) {
    assert.throws(() => recordMutationArguments({ ...base, ...patch }));
  }
});
test('requires matching explicit contract recipient and signing material', async () => {
  await assert.rejects(validateRecordMutation({ ...base, recipient: base.sender }), /configured contract/);
  await assert.rejects(validateRecordMutation({ ...base, privateKey: '' }), /exactly one/);
  await assert.rejects(validateRecordMutation({ ...base, contractCodeHash: null }), /pinned/);
});
