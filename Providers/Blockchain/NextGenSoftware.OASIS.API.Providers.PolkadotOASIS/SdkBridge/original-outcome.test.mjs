import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readVerifiedOriginalRange, validateOutcomeRange } from './original-outcome.mjs';

const tx = '0x' + 'b'.repeat(64);
const block = '0x' + 'a'.repeat(64);
const identity = { transactionHash: tx, sender: 'signer' };
const numeric = value => ({ toBigInt: () => BigInt(value), toString: () => String(value) });
function fixture({ foundAt = -1, payer = 'signer', failed = false } = {}) {
  const calls = [];
  const event = (section, method, data) => ({ phase: { isApplyExtrinsic: true, asApplyExtrinsic: { toNumber: () => 0 } }, event: { section, method, data } });
  return {
    calls,
    rpc: { chain: {
      getFinalizedHead: async () => 'tip',
      getHeader: async hash => ({ number: numeric(hash === 'tip' ? 12 : foundAt) }),
      getBlockHash: async height => { calls.push(Number(height.toString())); return { toHex: () => block, height: Number(height.toString()) }; },
      getBlock: async hash => ({ block: { extrinsics: hash.height === foundAt || typeof hash === 'string'
        ? [{ hash: { toHex: () => tx } }] : [] } })
    } },
    createType: (type, address) => ({ toHex: () => address }),
    at: async () => ({ query: { system: { events: async () => [
      event('system', failed ? 'ExtrinsicFailed' : 'ExtrinsicSuccess', ['dispatch error']),
      event('transactionPayment', 'TransactionFeePaid', [payer, '12', '2'])
    ] } } })
  };
}
test('bounded range returns an exact resumable cursor, not rejection or replay permission', async () => {
  const api = fixture();
  const result = await readVerifiedOriginalRange(api, identity, { fromBlockNumber: '10', maximumBlocks: 2 });
  assert.deepEqual(api.calls, [10, 11]);
  assert.equal(result.nextBlockNumber, '12');
  assert.equal(result.found, false);
  assert.equal(result.reconciliationRequired, true);
  assert.equal(result.searchedThroughSnapshot, false);
});
test('range reaching finalized snapshot is still an unknown original outcome', async () => {
  const result = await readVerifiedOriginalRange(fixture(), identity, { fromBlockNumber: '12', maximumBlocks: 2 });
  assert.equal(result.nextBlockNumber, '13');
  assert.equal(result.searchedThroughSnapshot, true);
  assert.equal(result.finalized, false);
});
test('future cursor performs no block lookup and cannot claim finality', async () => {
  const api = fixture();
  const result = await readVerifiedOriginalRange(api, identity, { fromBlockNumber: '13', maximumBlocks: 1 });
  assert.equal(api.calls.length, 0);
  assert.equal(result.inspectedBlocks, 0);
  assert.equal(result.found, false);
});
test('caller mutation cannot enlarge a captured scan or replace its original identity', async () => {
  const api = fixture();
  const range = { fromBlockNumber: '10', maximumBlocks: 1 };
  const original = { ...identity };
  const pending = readVerifiedOriginalRange(api, original, range);
  range.maximumBlocks = 128;
  original.transactionHash = 'replacement';
  const result = await pending;
  assert.deepEqual(api.calls, [10]);
  assert.equal(result.transactionHash, tx);
});
for (const failed of [false, true]) test(`found original binds fee payer and preserves dispatch outcome ${failed}`, async () => {
  const result = await readVerifiedOriginalRange(fixture({ foundAt: 11, failed }), identity, { fromBlockNumber: '10', maximumBlocks: 2 });
  assert.equal(result.found, true);
  assert.equal(result.receipt.dispatchSucceeded, !failed);
  assert.equal(result.receipt.actualFeeUnits, '12');
  assert.equal(result.reconciliationRequired, true); // Not a billing settlement result.
});
test('different fee payer cannot settle the signed original', async () => {
  await assert.rejects(readVerifiedOriginalRange(fixture({ foundAt: 10, payer: 'other' }), identity,
    { fromBlockNumber: '10', maximumBlocks: 1 }), /payer differs/);
});
for (const range of [
  { fromBlockNumber: '-1', maximumBlocks: 1 }, { fromBlockNumber: '01', maximumBlocks: 1 },
  { fromBlockNumber: '18446744073709551616', maximumBlocks: 1 }, { fromBlockNumber: 1, maximumBlocks: 1 },
  { fromBlockNumber: '1', maximumBlocks: 0 }, { fromBlockNumber: '1', maximumBlocks: 129 },
  { fromBlockNumber: '1', maximumBlocks: 1.5 }
]) test(`reject invalid range ${JSON.stringify(range)} before RPC`, () => assert.throws(() => validateOutcomeRange(range)));
