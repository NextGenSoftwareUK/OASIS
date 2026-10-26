import { test } from 'node:test';
import assert from 'node:assert/strict';
import { encodeAddress } from '@polkadot/util-crypto';
import { canonicalAccountIdentities } from './account-identities.mjs';

test('SDK preserves one AccountId32 across hex and SS58 presentations', async () => {
  const bytes = new Uint8Array(32).fill(17);
  const hex = '0x' + Buffer.from(bytes).toString('hex');
  assert.deepEqual(await canonicalAccountIdentities([hex, encodeAddress(bytes, 0), encodeAddress(bytes, 42), encodeAddress(bytes, 1000)]),
    [hex, hex, hex, hex]);
});

test('SDK rejects a damaged SS58 checksum rather than inventing an identity', async () => {
  const valid = encodeAddress(new Uint8Array(32).fill(17), 42);
  await assert.rejects(canonicalAccountIdentities([valid.slice(0, -1) + (valid.endsWith('1') ? '2' : '1')]));
});

test('bounded AccountId32 input rejects short accounts and malformed batches', async () => {
  for (const addresses of [[], null, Array(17).fill('x'), ['0x11'], [' 0x' + '11'.repeat(32)], [null]])
    await assert.rejects(canonicalAccountIdentities(addresses));
});
