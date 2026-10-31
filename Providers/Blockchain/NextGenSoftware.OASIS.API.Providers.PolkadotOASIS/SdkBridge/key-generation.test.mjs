import { test } from 'node:test';
import assert from 'node:assert/strict';
import { Keyring } from '@polkadot/api';
import { decodeAddress, encodeAddress, signatureVerify } from '@polkadot/util-crypto';
import { generateKeyPair, validateGenerationRequest, createAccount, restoreAccount, validateRestorationMaterial } from './key-generation.mjs';

const api = prefix => ({ registry: { chainSS58: prefix }, genesisHash: { toHex: () => '0x' + 'a'.repeat(64) },
  createType: (_, address) => ({ toHex: () => '0x' + Buffer.from(decodeAddress(address)).toString('hex') }) });

test('SDK BIP39 account restores the identical sr25519 identity from mnemonic and raw seed', async () => {
  const created = await createAccount(api(42));
  let mnemonic, raw;
  try {
    assert.equal(created.seedPhrase.split(' ').length, 24);
    mnemonic = await restoreAccount(api(42), created.seedPhrase);
    raw = await restoreAccount(api(42), created.privateKey);
    for (const restored of [mnemonic, raw]) {
      assert.equal(restored.publicKey, created.publicKey);
      assert.equal(restored.address, created.address);
      assert.ok(restored.privateKey === created.privateKey, 'Restored seed mismatch (values withheld).');
    }
  } finally {
    created.privateKey = null; created.seedPhrase = null;
    if (mnemonic) mnemonic.privateKey = null;
    if (raw) raw.privateKey = null;
  }
});

for (const material of [null, '', 'arbitrary text', '0x01', 'ab'.repeat(33), 'notabip39word '.repeat(12).trim()])
  test(`invalid recovery material rejected without guessed key derivation (${typeof material}/${material?.length ?? 0})`, async () => {
    await assert.rejects(validateRestorationMaterial(material), /required|valid BIP39/);
  });
for (const prefix of [0, 42, 1000, 16383]) test(`real SDK sr25519 generation, SS58 and independent signatures for format ${prefix}`, async () => {
  const generated = await generateKeyPair(api(prefix));
  let pair; const seed = Buffer.from(generated.privateKey.slice(2), 'hex');
  try {
    assert.ok(/^0x[0-9a-f]{64}$/.test(generated.privateKey), 'Private seed format invalid (value withheld).');
    assert.equal(generated.cryptoType, 'sr25519'); assert.equal(generated.ss58Format, prefix);
    assert.equal(encodeAddress(decodeAddress(generated.address), prefix), generated.address);
    pair = new Keyring({ type: 'sr25519', ss58Format: prefix }).addFromSeed(seed);
    assert.equal('0x' + Buffer.from(pair.publicKey).toString('hex'), generated.publicKey);
    assert.equal(pair.address, generated.address);
    const message = Buffer.from('OASIS generated key proof');
    const signature = pair.sign(message);
    assert.equal(signatureVerify(message, signature, generated.publicKey).isValid, true);
    assert.equal(signatureVerify(Buffer.from('different proof'), signature, generated.publicKey).isValid, false);
  } finally { seed.fill(0); pair?.lock(); generated.privateKey = null; }
});
test('fresh RNG samples are distinct without printing private material', async () => {
  const first = await generateKeyPair(api(42)), second = await generateKeyPair(api(42));
  try { assert.ok(first.privateKey !== second.privateKey, 'Distinct fresh seed samples expected (values withheld).');
    assert.notEqual(first.publicKey, second.publicKey); }
  finally { first.privateKey = null; second.privateKey = null; }
});
for (const prefix of [undefined, null, -1, 46, 47, 16384, 1.5, '42'])
  test(`no guessed network prefix ${String(prefix)}`, async () => assert.rejects(generateKeyPair(api(prefix)), /authoritative/));
for (const field of ['privateKey', 'mnemonic', 'publicKey', 'address'])
  test(`generation is not import/recovery of ${field}`, () => assert.throws(() => validateGenerationRequest({ [field]: '' }), /not wallet recovery/));
test('codec/address mismatch cannot produce a successful generated wallet', async () => {
  const invalid = { ...api(42), createType: () => ({ toHex: () => '0x' + '0'.repeat(64) }) };
  await assert.rejects(generateKeyPair(invalid), /does not identify/);
});
