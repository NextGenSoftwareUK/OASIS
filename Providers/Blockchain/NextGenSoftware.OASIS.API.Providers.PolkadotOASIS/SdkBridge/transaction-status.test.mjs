import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readTransactionStatus } from './transaction-status.mjs';

const hash = n => '0x' + n.toString(16).padStart(64,'0');
const tx = hash(99);
const number = n => ({ toBigInt: () => BigInt(n), toString: () => String(n) });
function fixture({ failed = false, missing = false, duplicate = false, broken = false, events = true, canonical = true } = {}) {
  const header = n => ({ number: number(n), hash: { toHex: () => hash(n) }, parentHash: { toHex: () => hash(broken ? 0 : n-1) } });
  return {
    rpc: { chain: {
      getFinalizedHead: async () => ({ toHex: () => hash(2) }),
      getHeader: async h => header(Number(BigInt(typeof h === 'string' ? h : h.toHex()))),
      getBlockHash: async n => ({ toHex: () => hash(canonical ? Number(n.toString()) : 100) }),
      getBlock: async h => {
        const n = Number(BigInt(typeof h === 'string' ? h : h.toHex()));
        return { block: { header: header(n), extrinsics: n === 1 && !missing
          ? Array.from({length: duplicate ? 2 : 1}, () => ({ hash: { toHex: () => tx } })) : [] } };
      }
    } },
    at: async () => ({ query: { system: { events: async () => events ? [{
      phase: { isApplyExtrinsic: true, asApplyExtrinsic: { toNumber: () => 0 } },
      event: { section: 'system', method: failed ? 'ExtrinsicFailed' : 'ExtrinsicSuccess', data: ['actual failure'] }
    }] : [] } } })
  };
}
for (const explicit of [false,true]) for (const failed of [false,true])
  test(`exact finalized status explicit=${explicit} failed=${failed}`, async () => {
    const result = await readTransactionStatus(fixture({failed}), {transactionHash:tx, ...(explicit ? {blockHash:hash(1)} : {})});
    assert.equal(result.status, failed ? 'Failed' : 'Completed');
    assert.equal(result.dispatchSucceeded, !failed);
    assert.equal(result.blockHash, hash(1));
    assert.equal(result.extrinsicIndex, 0);
  });
for (const [name, options, pattern] of [
  ['absence', {missing:true}, /unresolved/], ['duplicates', {duplicate:true}, /Ambiguous/],
  ['broken ancestry', {broken:true}, /inconsistent/], ['missing dispatch', {events:false}, /terminal dispatch/],
  ['orphan', {canonical:false}, /canonical/]
]) test(`status rejects ${name}`, async () => assert.rejects(readTransactionStatus(fixture(options), {transactionHash:tx}),pattern));
test('invalid transaction identity fails before RPC', async () => assert.rejects(readTransactionStatus(null,{transactionHash:'bad'}), /exact transaction/));
test('pruned block remains an error', async () => {
  const api = fixture(); api.rpc.chain.getBlock = async () => {throw new Error('archive unavailable');};
  await assert.rejects(readTransactionStatus(api,{transactionHash:tx}), /archive unavailable/);
});
