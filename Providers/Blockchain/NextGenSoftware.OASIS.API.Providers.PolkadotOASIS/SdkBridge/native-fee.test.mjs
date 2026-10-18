import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readVerifiedNativeFee, requireReviewedNativeFeeExtensions } from './original-outcome.mjs';

const tx = '0x' + 'f'.repeat(64);
const hash = height => '0x' + BigInt(height).toString(16).padStart(64, '0');
const numeric = height => ({ toBigInt: () => BigInt(height), toString: () => String(height) });
function fixture({ foundAt = 130, tip = 140, wrongCheckpoint = false, failed = false, payer = 'sender' } = {}) {
  const visited = [];
  const event = (section, method, data) => ({ phase: { isApplyExtrinsic: true, asApplyExtrinsic: { toNumber: () => 0 } }, event: { section, method, data } });
  const api = {
    visited,
    createType: (type, value) => ({ toHex: () => value }),
    rpc: { chain: {
      getFinalizedHead: async () => hash(tip),
      getHeader: async value => ({ number: numeric(BigInt(value.toHex ? value.toHex() : value)) }),
      getBlockHash: async value => ({ toHex: () => wrongCheckpoint && BigInt(value.toString()) === 0n ? hash(1) : hash(value.toString()) }),
      getBlock: async value => {
        const height = Number(BigInt(value.toHex ? value.toHex() : value));
        visited.push(height);
        return { block: { extrinsics: height === foundAt ? [{ hash: { toHex: () => tx } }] : [] } };
      }
    } },
    at: async () => ({ query: { system: { events: async () => [
      event('system', failed ? 'ExtrinsicFailed' : 'ExtrinsicSuccess', ['dispatch failed']),
      event('transactionPayment', 'TransactionFeePaid', [payer, '12', '2'])
    ] } } })
  };
  return api;
}
const identity = { transactionHash: tx, sender: 'sender', signingCheckpointHash: hash(0) };
test('fee search advances past first128block page to exact original', async () => {
  const api = fixture();
  const receipt = await readVerifiedNativeFee(api, identity);
  assert.equal(receipt.blockNumber, '130');
  assert.equal(receipt.actualFeeUnits, '12');
  assert.equal(receipt.dispatchSucceeded, true);
  assert.ok(api.visited.includes(128));
});
test('failed finalized original still returns its actual native fee', async () => {
  const receipt = await readVerifiedNativeFee(fixture({ foundAt: 1, failed: true }), identity);
  assert.equal(receipt.dispatchSucceeded, false);
  assert.equal(receipt.actualFeeUnits, '12');
});
test('caller cannot replace original fee identity across asynchronous reads', async () => {
  const mutable = { ...identity };
  const pending = readVerifiedNativeFee(fixture({ foundAt: 1 }), mutable);
  mutable.signingCheckpointHash = hash(9);
  mutable.transactionHash = hash(99);
  assert.equal((await pending).transactionHash, tx);
});
test('absent original is unresolved, never a synthetic zero fee', async () => {
  await assert.rejects(readVerifiedNativeFee(fixture({ foundAt: -1, tip: 2 }), identity), /no zero fee/);
});
test('fee lookup terminates at its captured snapshot instead of chasing a moving head', async () => {
  const api = fixture({ foundAt: -1, tip: 2 });
  let observations = 0;
  api.rpc.chain.getFinalizedHead = async () => hash(2 + observations++ * 100);
  await assert.rejects(readVerifiedNativeFee(api, identity), /no zero fee/);
  assert.deepEqual(api.visited, [0, 1, 2]);
});
test('orphan signing checkpoint cannot determine a canonical fee', async () => {
  await assert.rejects(readVerifiedNativeFee(fixture({ wrongCheckpoint: true }), identity), /checkpoint/);
});
test('fee payer must match the verified original signer', async () => {
  await assert.rejects(readVerifiedNativeFee(fixture({ foundAt: 1, payer: 'other' }), identity), /payer differs/);
});
test('unavailable RPC history cannot become absence or zero', async () => {
  const api = fixture();
  const failure = new Error('pruned checkpoint history');
  api.rpc.chain.getHeader = async () => { throw failure; };
  await assert.rejects(readVerifiedNativeFee(api, identity), error => error === failure);
});
test('native fee context requires reviewed standard payment extension', () => {
  requireReviewedNativeFeeExtensions(['CheckGenesis', 'CheckNonce', 'ChargeTransactionPayment']);
  for (const extensions of [[], ['CheckGenesis'], ['ChargeAssetTxPayment'], ['ChargeTransactionPayment', 'UnknownFeeExtension']])
    assert.throws(() => requireReviewedNativeFeeExtensions(extensions), /not reviewed/);
});
