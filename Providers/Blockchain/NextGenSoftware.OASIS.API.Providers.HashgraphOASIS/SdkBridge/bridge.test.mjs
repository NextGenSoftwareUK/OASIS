import test from 'node:test';
import assert from 'node:assert/strict';
import { execute, tokenUnits } from './bridge.mjs';

test('invalid receipt ID is rejected before opening a consensus client', async () => {
  await assert.rejects(execute('receipt', { network: 'testnet', transactionId: 'not-a-transaction-id' }));
});

test('mint rejects an invalid HTS asset ID before opening a consensus client', async () => {
  await assert.rejects(execute('tokenMint', { network: 'testnet', tokenId: 'not-an-hts-id' }));
});

for (const keyType of ['ed25519', 'ecdsa']) {
  test(`official SDK ${keyType} key generation and DER recovery`, async () => {
    const generated = await execute('generateKey', { keyType });
    const recovered = await execute('restoreKey', { privateKey: generated.privateKey, keyType: 'der' });
    assert.deepEqual(recovered, generated);
    assert.equal('accountId' in generated, false);
  });
}
test('raw key without explicit algorithm is rejected', async () => {
  await assert.rejects(execute('restoreKey', { privateKey: '1'.repeat(64), keyType: 'der' }));
});
test('fractional tinybars are rejected before network use', async () => {
  await assert.rejects(execute('transfer', { tinybars: '1.1' }), /exact int64/);
});
test('tinybar overflow is rejected before network use', async () => {
  await assert.rejects(execute('transfer', { tinybars: '9223372036854775808' }), /exact int64/);
});
test('token scaling uses ledger decimals without Number rounding', () => {
  assert.equal(tokenUnits('0.12345678', 8), 12345678n);
  assert.equal(tokenUnits('9007199254740993', 0), 9007199254740993n);
  assert.throws(() => tokenUnits('1.001', 2), /precision/);
  assert.throws(() => tokenUnits('9223372036854775808', 0), /int64/);
});
test('mismatched public key is rejected before opening a network client', async () => {
  const first = await execute('generateKey', { keyType: 'ed25519' });
  const second = await execute('generateKey', { keyType: 'ed25519' });
  await assert.rejects(execute('transfer', {
    sender: '0.0.1001', recipient: '0.0.1002', tinybars: '1', keyType: 'der',
    privateKey: first.privateKey, publicKey: second.publicKey
  }), /public key/);
});
test('transaction payer mismatch is rejected before opening a network client', async () => {
  const key = await execute('generateKey', { keyType: 'ed25519' });
  await assert.rejects(execute('transfer', {
    sender: '0.0.1001', recipient: '0.0.1002', tinybars: '1', keyType: 'der',
    privateKey: key.privateKey, transactionId: '0.0.1003@1700000000.000000001'
  }), /payer/);
});
