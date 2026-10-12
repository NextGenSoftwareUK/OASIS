import test from 'node:test';
import assert from 'node:assert/strict';
import { verifiedBankTransfer, verifiedContractExecution, verifiedUpload, verifiedInstantiation, cosmosFailureMessage } from './transaction-results.mjs';
import { toBech32 } from '@cosmjs/encoding';
import { TimeoutError } from '@cosmjs/stargate';
const hash = 'ab'.repeat(32);
test('upload requires exact positive code ID and retains original digest on invalid identity', () => {
  const result = { transactionHash:hash,height:42,codeId:7 };
  assert.equal(verifiedUpload(result),result);
  for (const codeId of [undefined,0,-1,'7',1.5,Number.MAX_SAFE_INTEGER+1])
    assert.throws(() => verifiedUpload({...result,codeId}), error => error.message.includes(hash));
});
test('instantiation validates actual bech32 checksum and configured prefix without losing receipt', () => {
  const result = {transactionHash:hash,height:42,contractAddress:toBech32('wasm',new Uint8Array(32).fill(1))};
  assert.equal(verifiedInstantiation(result,'wasm'),result);
  assert.throws(() => verifiedInstantiation(result,'cosmos'), error => error.message.includes(hash));
  for (const contractAddress of [undefined,'fabricated',result.contractAddress.slice(0,-1)+'!'])
    assert.throws(() => verifiedInstantiation({...result,contractAddress},'wasm'), error => error.message.includes(hash));
});
test('contract execution preserves verified SDK hash and committed height', () => {
  const result = { transactionHash: hash, height: 42 };
  assert.equal(verifiedContractExecution(result), result);
});
for (const height of [undefined, 0, -1, '42', 1.5, Number.MAX_SAFE_INTEGER + 1])
  test(`contract execution rejects invalid height ${height} while retaining hash`, () =>
    assert.throws(() => verifiedContractExecution({transactionHash:hash,height}), error => error.message.includes(hash) && error.message.includes('before retrying')));
test('contract execution rejects missing or malformed hash', () => {
  for (const result of [null, {height:42}, {transactionHash:'fabricated',height:42}])
    assert.throws(() => verifiedContractExecution(result), /Reconcile submission/);
});
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
