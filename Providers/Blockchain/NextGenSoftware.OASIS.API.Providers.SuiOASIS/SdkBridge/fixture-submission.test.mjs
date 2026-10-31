import test from 'node:test';
import assert from 'node:assert/strict';
import { journaledMutation } from './fixture-submission.mjs';

test('returned-record disk failure preserves prepared ownership and original errors', async () => {
  const disk = new Error('controlled result journal failure');
  const recovery = new Error('controlled recovery journal failure');
  const rows = []; let unknown = false, submitted = 0;
  await assert.rejects(journaledMutation(async (_, persist) => {
    await persist({ transactionHash: 'original' }); submitted++;
    return { transactionHash: 'original', tokenIds: ['owned'] };
  }, { operation: 'nftMint' }, row => {
    if (row.stage === 'returned') throw disk;
    if (row.stage === 'unresolved') throw recovery;
    rows.push(row);
  }, () => { unknown = true; }), error => error instanceof AggregateError
    && error.errors[0] === disk && error.errors[1] === recovery);
  assert.equal(unknown, true); assert.equal(submitted, 1);
  assert.equal(rows[0].evidence.transactionHash, 'original');
});

test('preparation persistence rejection never submits or invents a native outcome', async () => {
  const disk = new Error('controlled prebroadcast disk failure');
  let submitted = 0, unknown = false;
  await assert.rejects(journaledMutation(async (_, persist) => {
    await persist({ transactionHash: 'original' }); submitted++;
  }, { operation: 'tokenMint' }, () => { throw disk; }, () => { unknown = true; }), error => error === disk);
  assert.equal(submitted, 0); assert.equal(unknown, false);
});
