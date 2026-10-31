// Controlled verifier tests: actual client modules, memory-only transport/files.
// Native provider acceptance is reported by the separate Oracle/Mongo SDK/HTTP gate.
'use strict';
const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const modulePath = path.resolve(__dirname, '../../../Web4Site/cql-demo-preparation.js');
const program = fs.readFileSync(path.join(__dirname, 'Verify-HostedSourceFlow.cjs'), 'utf8');

for (const byId of [false, true]) test(`verifier uses ${byId ? 'native ID unions' : 'protocol keys'} and checks both replicas`, async () => {
  const owner = 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa';
  const nativeIds = ['44444444-4444-4444-8444-444444444441',
    '44444444-4444-4444-8444-444444444442', '44444444-4444-4444-8444-444444444443'];
  const copyIds = ['11111111-1111-4111-8111-111111111111', '22222222-2222-4222-8222-222222222222'];
  const provider = byId ? 'OracleDBOASIS' : 'ActivityPubOASIS';
  const keys = byId ? nativeIds : ['101', '102', '103'];
  const source = { provider, available: true, federationAvailable: true };
  function example(count) {
    return { operation: 'Federation', request: { scope: 'Named', providers: ['MongoDBOASIS', provider],
      source: { orderBy: 'None', take: 8, filters: [{ ...(byId ? { field: 'Id', scalar: 'Guid' }
        : { metadataPath: ['NativeKey'], scalar: 'String' }), operator: 'In', value: keys.slice(0, count) }] },
      identityMode: 'PreserveCopies', maximumProviderAttempts: 2, take: 8 } };
  }
  const input = { source, base: 'https://controlled.invalid/api/', token: 'controlled-test-only', ownerId: owner,
    state: 'memory-state', report: 'memory-report', preparationModule: modulePath,
    pairedThree: example(3), pairedTwo: example(2) };
  const files = new Map(); const handles = new Map(); let descriptor = 0;
  const memory = {
    existsSync: name => files.has(name),
    readFileSync: name => name === 0 ? JSON.stringify(input) : files.get(name),
    openSync(name, flag) {
      if (flag === 'wx' && files.has(name)) throw Error('Existing exclusive intent.');
      if (!files.has(name)) files.set(name, '');
      handles.set(++descriptor, { name, flag }); return descriptor;
    },
    writeFileSync(name, value, options) {
      if (typeof name === 'number') {
        const handle = handles.get(name);
        files.set(handle.name, (handle.flag === 'a' ? files.get(handle.name) : '') + value); return;
      }
      if (options?.flag === 'wx' && files.has(name)) throw Error('Existing exclusive proof.');
      files.set(name, value);
    },
    fsyncSync() {}, closeSync() {},
    renameSync(from, to) { files.set(to, files.get(from)); files.delete(from); }
  };
  const native = nativeIds.map((id, i) => ({ id, name: 'Native ' + keys[i], description: 'Source record',
    metadata: byId ? {} : { NativeKey: keys[i] } }));
  const copies = new Map(); const calls = []; let saves = 0;
  function selected(rows, body) {
    const filter = body.filters[0];
    const values = Array.isArray(filter.value) ? filter.value : [filter.value];
    return rows.filter(row => values.includes(filter.field === 'Id' ? row.id : row.metadata.NativeKey));
  }
  const fetch = async (url, options) => {
    const route = url.replace(input.base, ''); const body = JSON.parse(options.body);
    calls.push({ route, body }); let result;
    if (route.endsWith('demo-identities')) result = { result: { ownerId: owner, provider,
      records: body.keys.map(nativeKey => ({ id: copyIds[keys.indexOf(nativeKey)], nativeKey, metadata: {
        'oasis.native.provider': provider, 'oasis.native.source': 'cosmic-cql-demo', 'oasis.native.key': nativeKey
      } })) } };
    else if (route === 'data/save-holon') {
      saves++;
      assert.equal(copies.has(body.holon.id), false);
      const row = { id: body.holon.id, name: body.holon.name, description: body.holon.description,
        metadata: body.holon.metaData, version: 1, createdByAvatarId: owner, isPublic: false };
      copies.set(row.id, row); result = { isSaved: true, result: row };
    } else if (route.endsWith('/federation')) {
      result = { result: { rows: selected([...native, ...copies.values()], body.source) } };
    } else if (route === 'data/query-cql') {
      result = { result: { rows: selected(body.provider === provider ? native : [...copies.values()], body) } };
    } else throw Error('Unexpected route ' + route);
    return { ok: true, status: 200, headers: { get: () => 'controlled-operation' }, text: async () => JSON.stringify(result) };
  };
  const errors = []; const process = { exitCode: 0 };
  await vm.runInNewContext(program, { require: name => name === 'node:fs' ? memory : require(name),
    fetch, AbortSignal, setTimeout, process, console: { log() {}, error: value => errors.push(value) } });
  assert.equal(process.exitCode, 0, errors.join('\n'));
  const proof = JSON.parse(files.get(input.report));
  assert.equal(saves, 2); assert.equal(proof.repeated.saved, 0); assert.equal(proof.repeated.existing, 2);
  assert.equal(proof.five.result.rows.length, 5); assert.equal(proof.four.result.rows.length, 4);
  assert.equal(proof.replicas.SQLLiteDBOASIS.result.rows.length, 2);
  assert.equal(proof.replicas.Neo4jOASIS.result.rows.length, 2);
  const pairs = calls.filter(call => call.route.endsWith('/federation'));
  assert.deepEqual(pairs.map(call => call.body.source.filters[0].value.length), byId ? [5, 4] : [3, 2]);
  const maps = calls.map((call, index) => call.route.endsWith('demo-identities') ? index : -1).filter(index => index >= 0);
  assert.equal(maps.length, 3);
  assert.equal(maps[1] < calls.indexOf(pairs[0]), byId);
  assert.equal(proof.browserAcceptance, false);
});
