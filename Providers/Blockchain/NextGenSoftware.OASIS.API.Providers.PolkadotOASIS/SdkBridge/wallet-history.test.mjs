import { test } from 'node:test';
import assert from 'node:assert/strict';
import { collectWalletBlock, readWalletHistory, validateHistoryRequest } from './wallet-history.mjs';

const hex = n => '0x' + n.toString(16).padStart(64, '0');
const api = { createType: (_, value) => ({ toHex: () => value }) };
const at = { query: { timestamp: { now: async () => '1700000000000' }, assets: { metadata: async () => ({ decimals: { toNumber: () => 6 } }) } } };
const tx = (signer = 'wallet') => ({ isSigned: true, signer, hash: { toHex: () => hex(10) }, method: { section: 'utility', method: 'batchAll' } });
const event = (section, method, data = [], index = 0) => ({ phase: { isApplyExtrinsic: true, asApplyExtrinsic: { toNumber: () => index } }, event: { section, method, data } });
const terminal = (success = true) => event('system', success ? 'ExtrinsicSuccess' : 'ExtrinsicFailed', success ? [] : ['ModuleError']);
const collect = (events, signer = 'wallet') => collectWalletBlock(api, at, { extrinsics: [tx(signer)] }, events, 'wallet', 12);

test('native history preserves units beyond Int64 and distinct batch event identities', async () => {
  const raw = (1n << 100n).toString();
  const rows = await collect([event('balances', 'Transfer', ['wallet', 'other', raw]), event('balances', 'Transfer', ['wallet', 'other', '2']), terminal()]);
  assert.equal(rows.length, 2); assert.equal(rows[0].rawAmount, raw); assert.equal(rows[0].decimals, 12);
  assert.notEqual(rows[0].eventIndex, rows[1].eventIndex); assert.equal(rows[0].amountUnit, 'native-base-unit');
});
test('incoming asset transfer is found without owning the extrinsic signer', async () => {
  const [row] = await collect([event('assets', 'Transferred', ['42', 'other', 'wallet', '250001']), terminal()], 'other');
  assert.equal(row.rawAmount, '250001'); assert.equal(row.decimals, 6); assert.equal(row.token, '42'); assert.equal(row.from, 'other');
});
test('mint never invents a debit from the issuing account', async () => {
  const [row] = await collect([event('assets', 'Issued', ['42', 'other', '100']), terminal()]);
  assert.equal(row.amountKind, 'not-interpreted'); assert.equal(row.rawAmount, null);
  const [credit] = await collect([event('assets', 'Issued', ['42', 'wallet', '100']), terminal()], 'other');
  assert.equal(credit.from, ''); assert.equal(credit.to, 'wallet'); assert.equal(credit.rawAmount, '100');
});
test('burn identifies the debited holder separately from the admin signer', async () => {
  const [row] = await collect([event('assets', 'Burned', ['42', 'wallet', '100']), terminal()], 'admin');
  assert.equal(row.from, 'wallet'); assert.equal(row.to, ''); assert.equal(row.signer, 'admin');
});
test('failed signed calls retain status without fabricated financial units', async () => {
  const [row] = await collect([terminal(false)]);
  assert.equal(row.dispatchSucceeded, false); assert.equal(row.dispatchError, 'ModuleError'); assert.equal(row.rawAmount, null);
});
test('actual events survive failed top-level dispatch without assuming rollback', async () => {
  const [row] = await collect([event('balances', 'Transfer', ['wallet', 'other', '3']), terminal(false)]);
  assert.equal(row.rawAmount, '3'); assert.equal(row.dispatchSucceeded, false);
});
test('unrelated signed transactions are not returned as wallet activity', async () => {
  assert.deepEqual(await collect([terminal()], 'other'), []);
});
for (const [name, events] of [
  ['missing terminal', [event('balances', 'Transfer', ['wallet', 'other', '1'])]],
  ['ambiguous terminal', [terminal(), terminal(false)]],
  ['negative units', [event('balances', 'Transfer', ['wallet', 'other', '-1']), terminal()]],
  ['overflow units', [event('balances', 'Transfer', ['wallet', 'other', (1n << 128n).toString()]), terminal()]],
  ['malformed event', [event('assets', 'Transferred', ['42', 'wallet', '1']), terminal()]]
]) test(`reject ${name} history, never return a partial list`, async () => assert.rejects(collect(events)));

for (const fields of [{ address: '' }, { fromBlock: '-1' }, { fromBlock: '01' }, { fromBlock: 1 },
  { fromBlock: (1n << 64n).toString() }, { toBlock: '128' }, { fromBlock: '2', toBlock: '1' }])
  test(`reject invalid history range before RPC ${JSON.stringify(fields)}`, () => assert.throws(() => validateHistoryRequest({ address: 'wallet', ...fields })));

function fixture(change = {}) {
  let reads = 0;
  const rpc = {
    getFinalizedHead: async () => hex(2), getHeader: async () => ({ number: { toBigInt: () => 2n } }),
    getBlockHash: async n => hex(Number(n)),
    getBlock: async blockHash => { reads++; const n = BigInt(blockHash.toHex()); return { block: {
      header: { number: { toBigInt: () => n }, hash: { toHex: () => hex(Number(n)) }, parentHash: { toHex: () => hex(Number(n) - 1) } },
      extrinsics: [tx()] } }; }
  };
  const chain = { ...rpc, ...change };
  const wrapped = { ...chain };
  wrapped.getFinalizedHead = async () => { const value = await chain.getFinalizedHead(); return { toHex: () => value }; };
  wrapped.getBlockHash = async n => { const value = await chain.getBlockHash(n); return { toHex: () => value }; };
  return { api: { ...api, genesisHash: { toHex: () => hex(99) }, rpc: { chain: wrapped },
    at: async () => ({ query: { ...at.query, system: { events: async () => [terminal()] } } }) }, reads: () => reads };
}
test('finalized inclusive range and canonical anchors return complete selected range', async () => {
  const f = fixture(); const result = await readWalletHistory(f.api, { address: 'wallet', fromBlock: '1', toBlock: '2' }, 12);
  assert.equal(result.rows.length, 2); assert.equal(result.fromBlock, '1'); assert.equal(result.toBlock, '2'); assert.equal(f.reads(), 2);
});
test('future and excessive implicit ranges fail instead of silently clipping', async () => {
  const f = fixture(); await assert.rejects(readWalletHistory(f.api, { address: 'wallet', fromBlock: '3' }, 12));
  const large = fixture({ getHeader: async () => ({ number: { toBigInt: () => 500n } }) });
  await assert.rejects(readWalletHistory(large.api, { address: 'wallet' }, 12)); assert.equal(large.reads(), 0);
});
test('archive errors remain failures, never successful empty history', async () => {
  const f = fixture({ getBlock: async () => { throw new Error('pruned'); } });
  await assert.rejects(readWalletHistory(f.api, { address: 'wallet', fromBlock: '1', toBlock: '1' }, 12), /pruned/);
});
test('caller mutation across awaits cannot change the captured wallet/range', async () => {
  const request = { address: 'wallet', fromBlock: '1', toBlock: '1' };
  const f = fixture({ getFinalizedHead: async () => { request.address = 'changed'; request.fromBlock = '99'; return hex(2); } });
  const result = await readWalletHistory(f.api, request, 12); assert.equal(result.address, 'wallet'); assert.equal(result.fromBlock, '1');
});
test('broken parent linkage rejects the whole range after a valid first block', async () => {
  const f = fixture(), original = f.api.rpc.chain.getBlock;
  f.api.rpc.chain.getBlock = async hash => { const block = await original(hash); block.block.header.parentHash = { toHex: () => hex(99) }; return block; };
  await assert.rejects(readWalletHistory(f.api, { address: 'wallet', fromBlock: '1', toBlock: '2' }, 12), /parent linkage/);
});
test('changed selected anchor cannot yield partial successful history', async () => {
  const f = fixture(), original = f.api.rpc.chain.getBlockHash; let calls = 0;
  f.api.rpc.chain.getBlockHash = async n => String(n) === '1' && ++calls > 1 ? { toHex: () => hex(99) } : original(n);
  await assert.rejects(readWalletHistory(f.api, { address: 'wallet', fromBlock: '1', toBlock: '1' }, 12), /anchor changed/);
});
test('changed finalized snapshot anchor is also rejected', async () => {
  const f = fixture(), original = f.api.rpc.chain.getBlockHash;
  f.api.rpc.chain.getBlockHash = async n => String(n) === '2' ? { toHex: () => hex(99) } : original(n);
  await assert.rejects(readWalletHistory(f.api, { address: 'wallet', fromBlock: '1', toBlock: '1' }, 12), /anchor changed/);
});
test('invalid authoritative timestamp is not replaced with current time', async () => {
  const bad = { query: { ...at.query, timestamp: { now: async () => '-1' } } };
  await assert.rejects(collectWalletBlock(api, bad, { extrinsics: [tx()] }, [terminal()], 'wallet', 12), /timestamp/);
});
