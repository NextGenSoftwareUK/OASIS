import test from 'node:test';
import assert from 'node:assert/strict';
import { verifiedBankTransfer, cosmosFailureMessage } from './transaction-results.mjs';
import { TimeoutError } from '@cosmjs/stargate';
const hash = 'ab'.repeat(32);
test('official SDK timeout retains identity without claiming failed or successful execution', () => {
  const message = cosmosFailureMessage(new TimeoutError('untrusted timeout message', hash));
  assert.ok(message.includes(hash)); assert.ok(message.includes('outcome is not verified'));
  assert.ok(message.includes('before retrying')); assert.ok(!message.includes('untrusted'));
});
test('ordinary errors with a txId property are not reclassified as verified SDK timeouts', () => {
  const ordinary = Object.assign(new Error('original validation error'), { txId: hash });
  assert.equal(cosmosFailureMessage(ordinary), 'original validation error');
});
test('bank transfer failure retains original committed hash and code without raw log', () => {
  assert.throws(() => verifiedBankTransfer({ code: 5, transactionHash: hash, rawLog: 'sensitive untrusted text' }), error =>
    error.message.includes(hash) && error.message.includes('code 5') && error.message.includes('before retrying') && !error.message.includes('sensitive'));
});
test('bank transfer accepts exact successful execution evidence unchanged', () => {
  const receipt = { code: 0, transactionHash: hash, height: 42 };
  assert.equal(verifiedBankTransfer(receipt), receipt);
});
for (const result of [null, { code: '0', transactionHash: hash }, { code: -1, transactionHash: hash }, { code: 0, transactionHash: 'fabricated' }])
  test(`bank transfer rejects malformed evidence ${JSON.stringify(result)}`, () => assert.throws(() => verifiedBankTransfer(result), /Reconcile submission/));
