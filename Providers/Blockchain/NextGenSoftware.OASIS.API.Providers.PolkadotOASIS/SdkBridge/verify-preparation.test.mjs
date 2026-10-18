import { test } from 'node:test';
import assert from 'node:assert/strict';
import { canonicalBase64, signingDocument } from './verify-preparation.mjs';

test('canonical original bytes survive roundtrip', () => assert.equal(canonicalBase64('AQID').toString('hex'), '010203'));
for (const value of ['', 'AQID=', 'AQID\n', 'not base64', null, 'AA'])
  test(`rejects malformed base64 ${JSON.stringify(value)}`, () => assert.throws(() => canonicalBase64(value)));
test('rejects oversized original bytes before decoding', () => assert.throws(() => canonicalBase64('AQID', 2)));
test('versioned signing document preserves exact SDK payload', () => {
  const payload = { method: '0x0102', nonce: '0x00000000' };
  const encoded = signingDocument(payload);
  assert.deepEqual(JSON.parse(canonicalBase64(encoded).toString('utf8')), { protocol: 'oasis.polkadot.signing-document.v1', signingPayload: payload });
});
