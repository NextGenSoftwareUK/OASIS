import { test } from 'node:test';
import assert from 'node:assert/strict';
import { nativeUnits, validateTransferRequest } from './native-transfer.mjs';
import { ReadOnlyHttpProvider } from './read-only-provider.mjs';

for (const [amount, decimals, expected] of [
  ['1', 10, '10000000000'], ['1.0000000001', 10, '10000000001'], ['0.0000000001', 10, '1'],
  ['1.000', 0, '1'], ['1', 12, '1000000000000'], ['18446744073709551616', 0, '18446744073709551616'],
  ['340282366920938463463374607431768211455', 0, '340282366920938463463374607431768211455']
]) test(`exact native units ${amount}/${decimals}`, () => assert.equal(nativeUnits(amount, decimals), expected));

for (const [amount, decimals] of [
  ['0', 10], ['-1', 10], ['+1', 10], ['01', 10], ['1e3', 10], ['1,000', 10],
  [' 1', 10], ['1 ', 10], ['.1', 10], ['0.00000000001', 10], ['1.1', 0],
  ['340282366920938463463374607431768211456', 0], ['1', -1], ['1', 39]
]) test(`rejects inexact or malformed units ${amount}/${decimals}`, () => assert.throws(() => nativeUnits(amount, decimals)));

const valid = { sender: 'sender', recipient: 'recipient', amount: '10.125', privateKey: '0x' + '01'.repeat(32) };
test('accepts explicit seed and decimal intent without assuming network precision', async () => {
  await validateTransferRequest(valid);
});
for (const [name, changes] of [
  ['missing signer', { privateKey: '' }], ['malformed seed', { privateKey: 'owned-secret' }],
  ['two signers', { mnemonic: 'not even a mnemonic' }], ['invalid mnemonic', { privateKey: '', mnemonic: 'invalid' }],
  ['missing sender', { sender: '' }], ['missing recipient', { recipient: '' }],
  ['asset token ambiguity', { tokenAddress: 'asset1' }], ['asset identity ambiguity', { tokenId: 'asset-id' }], ['non-positive amount', { amount: '0.00' }],
  ['non-string memo', { memo: 123 }], ['oversized UTF8 memo', { memo: '🌳'.repeat(65) }]
]) test(`rejects ${name} before runtime lookup`, async () => {
  await assert.rejects(validateTransferRequest({ ...valid, ...changes }));
});

for (const method of ['author_submitExtrinsic', 'author_submitAndWatchExtrinsic', 'transaction_v1_broadcast', 'transactionWatch_v1_submitAndWatch', 'unknown']) {
  test(`preparation transport cannot execute ${method}`, async () => {
    const provider = new ReadOnlyHttpProvider('http://127.0.0.1:1');
    try { await assert.rejects(provider.send(method, []), /rejects non-read-only/); }
    finally { await provider.disconnect(); }
  });
}
