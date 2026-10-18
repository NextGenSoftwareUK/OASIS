import { test } from 'node:test';
import assert from 'node:assert/strict';
import { exactAssetEffect } from './asset-outcome.mjs';

const api = { createType: (type, value) => ({ toHex: () => value }) };
const identity = { action: 'burn', assetId: '42', recipient: 'account', units: '100' };
const event = (amount = '100', id = '42', account = 'account', method = 'Burned') => ({ section: 'assets', method, data: [id, account, amount] });
for (const amount of ['100', '99', '101', '0']) test(`retain observed burn ${amount} without inventing exact success`, () => {
  const effect = exactAssetEffect(api, identity, [event(amount)], true);
  assert.equal(effect.observedUnits, amount);
  assert.equal(effect.assetEffectMatchesRequest, amount === '100');
});
test('mint requires Issued rather than a different asset event', () => {
  assert.equal(exactAssetEffect(api, { ...identity, action: 'mint' }, [event('100', '42', 'account', 'Issued')], true).assetEffectMatchesRequest, true);
  assert.throws(() => exactAssetEffect(api, { ...identity, action: 'mint' }, [event()], true));
});
test('failed dispatch does not invent asset effect or lose failure state', () => {
  assert.deepEqual(exactAssetEffect(api, identity, [], false), { observedUnits: null, assetEffectMatchesRequest: false });
});
for (const [name, events] of [
  ['missing', []], ['ambiguous', [event(), event()]], ['wrong asset', [event('100', '43')]],
  ['wrong account', [event('100', '42', 'other')]], ['negative amount', [event('-1')]],
  ['noncanonical amount', [event('0100')]], ['overflow amount', [event((1n << 128n).toString())]]
]) test(`reject ${name} asset effect`, () => assert.throws(() => exactAssetEffect(api, identity, events, true)));

const transfer = { ...identity, action: 'transfer', sender: 'source' };
const moved = (source = 'source', target = 'account', amount = '100') => ({ section: 'assets', method: 'Transferred', data: ['42', source, target, amount] });
for (const amount of ['100', '99', '101', '0']) test(`retain exact observed asset transfer ${amount}`, () => {
  const result = exactAssetEffect(api, transfer, [moved('source', 'account', amount)], true);
  assert.equal(result.observedUnits, amount);
  assert.equal(result.assetEffectMatchesRequest, amount === '100');
});
for (const [name, events] of [
  ['wrong source', [moved('other')]], ['wrong target', [moved('source', 'other')]],
  ['missing transfer', []], ['ambiguous transfers', [moved(), moved()]],
  ['burn masquerading as transfer', [event()]], ['wrong event shape', [{ ...moved(), data: ['42', 'account', '100'] }]]
]) test(`reject transfer ${name}`, () => assert.throws(() => exactAssetEffect(api, transfer, events, true)));
