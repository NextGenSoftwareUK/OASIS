import { test } from 'node:test';
import assert from 'node:assert/strict';
import elliptic from 'elliptic';
import { SHA3 } from 'sha3';
import { normalizeAddress, signMessage, validateEndpoint } from './bridge.mjs';

test('Flow addresses are 8 bytes, normalized with a 0x prefix', () => {
  assert.equal(normalizeAddress('F8D6E0586B0A20C7'), '0xf8d6e0586b0a20c7');
  assert.equal(normalizeAddress('0xf8d6e0586b0a20c7'), '0xf8d6e0586b0a20c7');
  assert.throws(() => normalizeAddress('0x1234'));
});

test('Only http(s) access node endpoints are accepted', () => {
  assert.equal(validateEndpoint('http://127.0.0.1:8888/'), 'http://127.0.0.1:8888');
  assert.throws(() => validateEndpoint('file:///etc/passwd'));
});

for (const [algorithm, curveName] of [['ECDSA_P256', 'p256'], ['ECDSA_secp256k1', 'secp256k1']]) {
  test(`${algorithm} signatures are SHA3-256 r||s and verify against the public key`, () => {
    const curve = new elliptic.ec(curveName);
    const key = curve.genKeyPair();
    const privateKey = key.getPrivate('hex').padStart(64, '0');
    const message = Buffer.from('FLOW-V0.0-transaction payload').toString('hex');
    const signature = signMessage(privateKey, message, algorithm);
    assert.match(signature, /^[0-9a-f]{128}$/);
    const hash = new SHA3(256);
    hash.update(Buffer.from(message, 'hex'));
    assert.ok(curve.keyFromPublic(key.getPublic()).verify(hash.digest(), { r: signature.slice(0, 64), s: signature.slice(64) }));
  });
}

test('Malformed signer keys are refused', () => {
  assert.throws(() => signMessage('abcd', '00'));
  assert.throws(() => signMessage('11'.repeat(32), '00', 'RSA'));
});
