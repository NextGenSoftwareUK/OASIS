import { test } from 'node:test';
import assert from 'node:assert/strict';
import { validateAssetRequest } from './asset-operation.mjs';

const valid = { action: 'mint', assetId: '424242', sender: 'explicit-signer', recipient: 'explicit-target',
  amount: '1.250001', privateKey: '0x' + '02'.repeat(32) };
for (const action of ['mint', 'burn']) test(`accept explicit asset ${action} intent before metadata lookup`, async () => {
  await validateAssetRequest({ ...valid, action });
});
for (const [name, fields] of [
  ['unknown action', { action: 'destroy' }], ['negative ID', { assetId: '-1' }],
  ['noncanonical ID', { assetId: '01' }], ['numeric ID object', { assetId: 1 }],
  ['oversized ID', { assetId: (1n << 128n).toString() }], ['empty signer', { sender: '' }],
  ['missing key', { privateKey: '' }], ['missing account', { recipient: '' }],
  ['nonpositive amount', { amount: '0' }], ['locale amount', { amount: '1,5' }],
  ['unimplemented memo', { memo: 'must not be discarded' }]
]) test(`reject asset ${name} before metadata/network`, async () => {
  await assert.rejects(validateAssetRequest({ ...valid, ...fields }));
  if (name === 'unimplemented memo') {
    await assert.rejects(validateAssetRequest({ ...valid, memo: 0 }));
    await assert.rejects(validateAssetRequest({ ...valid, memo: false }));
  }
});
