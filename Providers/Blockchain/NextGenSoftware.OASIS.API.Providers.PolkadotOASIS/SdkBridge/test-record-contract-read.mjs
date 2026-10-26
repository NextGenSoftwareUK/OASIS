import { test } from 'node:test';
import assert from 'node:assert/strict';
import { TypeRegistry } from '@polkadot/types/create';
import { registerRecordContractTypes } from './record-contract-codec.mjs';
import { readRecordContract, validateRecordRead } from './record-contract-read.mjs';

const address = '0x' + '11'.repeat(32), origin = '0x' + '22'.repeat(32), codeHash = '0x' + '33'.repeat(32);
const request = { action: 'list', contractAddress: address, origin, contractCodeHash: codeHash, keyBase64: '' };
function fixture({ missing = false, wrongCode = false, rows = [], editPage } = {}) {
  const registry = new TypeRegistry(); registerRecordContractTypes(registry);
  const counts = { snapshots: 0, heads: 0, calls: 0 };
  const finalized = { toHex: () => '0x' + '44'.repeat(32) };
  const at = {
    query: { contracts: { contractInfoOf: async () => ({ isNone: missing,
      unwrap: () => ({ codeHash: { toHex: () => wrongCode ? address : codeHash } }) }) } },
    call: { contractsApi: { call: async (_origin, _address, value, gas, deposit, input) => {
      counts.calls++;
      assert.equal(value, 0); assert.equal(gas, null); assert.equal(deposit, null);
      const bytes = Buffer.from(input.slice(10), 'hex');
      let type, result;
      if (input.startsWith('0x00000002')) {
        type = 'Option<OasisRecordCell>';
        const key = registry.createType('Bytes', bytes).toHex();
        result = rows.includes(key) ? { value: '0xab', revision: '9007199254740993' } : null;
      } else {
        type = 'OasisRecordPage';
        const args = registry.createType('(Bytes,u64,u8)', bytes);
        const prefix = args[0].toHex().slice(2), start = args[1].toNumber(), limit = args[2].toNumber();
        result = { generation: 27, keys: rows.slice(start, start + limit).filter(key => key.slice(2).startsWith(prefix)),
          next: start + limit < rows.length ? start + limit : null };
        if (editPage) result = editPage(result, start);
      }
      const data = registry.createType(`Result<Result<${type},OasisRecordError>,OasisRecordLangError>`, { Ok: { Ok: result } });
      return { result: { isErr: false, asOk: { flags: { get: () => registry.createType('u32', 0) },
        data: registry.createType('Bytes', data.toHex()) } } };
    } } }
  };
  return { counts, api: { registry, createType: (...args) => registry.createType(...args),
    rpc: { chain: { getFinalizedHead: async () => { counts.heads++; return finalized; } } },
    at: async hash => { counts.snapshots++; assert.equal(hash, finalized); return at; } } };
}

test('all enumeration pages use one finalized code-verified snapshot', async () => {
  const rows = Array.from({ length: 19 }, (_, i) => '0x' + Buffer.from(`record:${i}`).toString('hex'));
  const f = fixture({ rows });
  const actual = await readRecordContract(f.api, request);
  assert.deepEqual(actual.keysBase64, rows.map(hex => Buffer.from(hex.slice(2), 'hex').toString('base64')));
  assert.equal(actual.generation, '27');
  assert.deepEqual(f.counts, { snapshots: 1, heads: 1, calls: 3 });
});

test('point observation preserves bytes and u64 revision beyond JS safe integer', async () => {
  const f = fixture({ rows: ['0x010203'] });
  const result = await readRecordContract(f.api, { ...request, action: 'get', keyBase64: 'AQID' });
  assert.deepEqual(result.observation, { valueBase64: 'qw==', revision: '9007199254740993' });
  assert.equal((await readRecordContract(f.api, { ...request, action: 'get', keyBase64: 'AQ==' })).observation, null);
});

test('missing contract and wrong artifact never become missing rows', async () => {
  for (const settings of [{ missing: true }, { wrongCode: true }]) {
    const f = fixture(settings);
    await assert.rejects(readRecordContract(f.api, request), /does not exist|differs/);
    assert.equal(f.counts.calls, 0);
  }
});

test('rejects stalled cursors, changed generation and duplicate/out-of-prefix keys', async () => {
  for (const editPage of [
    page => ({ ...page, next: 0 }),
    (page, start) => ({ ...page, generation: start ? 28 : 27 }),
    page => ({ ...page, keys: ['0x01', '0x01'] }),
    page => ({ ...page, keys: ['0xff'] })
  ]) {
    const f = fixture({ rows: Array.from({ length: 9 }, (_, i) => '0x01' + i.toString(16).padStart(2, '0')), editPage });
    await assert.rejects(readRecordContract(f.api, { ...request, keyBase64: 'AQ==' }), /cursor|changed|duplicate|prefix/);
  }
});

test('verify performs native code and query round trips without enumeration', async () => {
  const f = fixture({ rows: Array.from({ length: 19 }, (_, i) => '0x01' + i.toString(16).padStart(2, '0')) });
  assert.equal((await readRecordContract(f.api, { ...request, action: 'verify' })).verified, true);
  assert.equal(f.counts.calls, 1);
  assert.throws(() => validateRecordRead({ ...request, action: 'get', keyBase64: '' }), /base64/);
  assert.throws(() => validateRecordRead({ ...request, contractCodeHash: null }), /pinned/);
});
