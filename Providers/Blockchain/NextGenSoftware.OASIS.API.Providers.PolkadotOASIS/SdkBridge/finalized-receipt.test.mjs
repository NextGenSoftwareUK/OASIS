import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFinalizedReceipt } from './finalized-receipt.mjs';

const blockHash = '0x' + 'a'.repeat(64);
const transactionHash = '0x' + 'b'.repeat(64);
const number = value => ({ toBigInt: () => BigInt(value), toString: () => String(value) });
function fixture({ final = 10, canonical = blockHash, duplicate = false, failed = false, fees = 1, tip = '2' } = {}) {
  const event = (section, method, data, index = 0) => ({
    phase: { isApplyExtrinsic: true, asApplyExtrinsic: { toNumber: () => index } },
    event: { section, method, data }
  });
  const events = [event('system', failed ? 'ExtrinsicFailed' : 'ExtrinsicSuccess', ['owned dispatch error'])];
  for (let i = 0; i < fees; i++) events.push(event('transactionPayment', 'TransactionFeePaid', ['payer', '12', tip]));
  // Another extrinsic's fee must never be attributed to this transaction.
  events.push(event('transactionPayment', 'TransactionFeePaid', ['other', '999', '0'], 1));
  return {
    rpc: { chain: {
      getFinalizedHead: async () => 'final',
      getHeader: async hash => ({ number: number(hash === 'final' ? final : 10) }),
      getBlockHash: async () => ({ toHex: () => canonical }),
      getBlock: async () => ({ block: { extrinsics: Array.from({ length: duplicate ? 2 : 1 }, () => ({ hash: { toHex: () => transactionHash } })) } })
    } },
    at: async () => ({ query: { system: { events: async () => events } } })
  };
}
test('exact finalized receipt includes tip once and excludes neighboring events', async () => {
  const result = await readFinalizedReceipt(fixture(), { blockHash, transactionHash });
  assert.equal(result.actualFeeUnits, '12');
  assert.equal(result.tipUnits, '2');
  assert.equal(result.dispatchSucceeded, true);
  assert.equal(result.finalized, true);
});
test('failed dispatch remains a paid finalized failure, not success', async () => {
  const result = await readFinalizedReceipt(fixture({ failed: true }), { blockHash, transactionHash });
  assert.equal(result.dispatchSucceeded, false);
  assert.equal(result.actualFeeUnits, '12');
  assert.equal(result.dispatchError, 'owned dispatch error');
});
for (const [name, options, pattern] of [
  ['unfinalized block', { final: 9 }, /not finalized/],
  ['orphan block', { canonical: '0x' + 'c'.repeat(64) }, /canonical/],
  ['ambiguous transaction', { duplicate: true }, /exactly once/],
  ['missing native fee', { fees: 0 }, /Exactly one native/],
  ['ambiguous native fee', { fees: 2 }, /Exactly one native/],
  ['invalid tip', { tip: '13' }, /Tip exceeds/]
]) test(`rejects ${name}`, async () => {
  await assert.rejects(readFinalizedReceipt(fixture(options), { blockHash, transactionHash }), pattern);
});
test('rejects missing transaction without claiming zero fees', async () => {
  await assert.rejects(readFinalizedReceipt(fixture(), { blockHash, transactionHash: '0x' + 'd'.repeat(64) }), /exactly once/);
});
test('requires explicit valid identities before RPC', async () => {
  await assert.rejects(readFinalizedReceipt(null, { blockHash: '', transactionHash }), /explicit block hash/);
});
