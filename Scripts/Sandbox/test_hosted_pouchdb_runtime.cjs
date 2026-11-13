'use strict';
const test = require('node:test');
const assert = require('node:assert/strict');
const crypto = require('node:crypto');
const fs = require('node:fs');
const path = require('node:path');
const { EventEmitter } = require('node:events');
const { spawnSync } = require('node:child_process');
const { supervise } = require('../../Docker/host-sandbox-ipfs.cjs');
const { captureBundle, canonical, waitForNative, serviceConfiguration, run } = require('../../Docker/host-sandbox-pouchdb.cjs');
const hash = bytes => crypto.createHash('sha256').update(bytes).digest('hex');

function sample() {
  const actor = 'b05b1234-1000-4000-8000-000000000001';
  return { schema: 1, actor, database: 'oasis_holons', records: [1, 2, 3].map(number => {
    const id = 'b05b1234-2000-4000-8000-00000000000' + number;
    const bytes = Buffer.from(JSON.stringify({ Id: id, CreatedByAvatarId: actor, Name: 'PouchDB teaching record ' + number,
      Description: 'Synthetic public native document', IsPublic: true, MetaData: { DemoSource: 'PouchDB' }, Children: [] }));
    return { id, sha256: hash(bytes), jsonBase64: bytes.toString('base64') };
  }) };
}
function body(record, mutate) {
  const row = JSON.parse(Buffer.from(record.jsonBase64, 'base64').toString());
  const bytes = Buffer.from(typeof mutate === 'string' ? mutate : JSON.stringify(mutate(row) ?? row));
  record.jsonBase64 = bytes.toString('base64'); record.sha256 = hash(bytes);
}

test('PouchDB bundle binds three public projections to one stable actor/database/digest', () => {
  const value = sample();
  const first = captureBundle(JSON.stringify(value));
  assert.equal(first.digest, captureBundle(JSON.stringify(value, null, 2)).digest);
  assert.equal(first.records.length, 3);
  assert.ok(Object.isFrozen(first) && Object.isFrozen(first.records) && first.records.every(Object.isFrozen));
  assert.equal(canonical({ z: { b: 'two', a: 'one' }, a: [] }), '{"a":[],"z":{"a":"one","b":"two"}}');
});

const invalid = [
  ['wrong database', value => { value.database = 'foreign'; }],
  ['empty owner', value => { value.actor = '00000000-0000-0000-0000-000000000000'; }],
  ['extra config', value => { value.endpoint = 'https://foreign.invalid'; }],
  ['incomplete rows', value => { value.records.pop(); }],
  ['duplicate identity', value => { value.records[1] = value.records[0]; }],
  ['unsafe identity', value => { value.records[0].id = '../foreign'; }],
  ['changed transfer', value => { value.records[0].sha256 = '0'.repeat(64); }],
  ['extra transfer fields', value => { value.records[0].secret = 'controlled-only'; }],
  ['changed owner', value => { body(value.records[0], row => { row.CreatedByAvatarId = crypto.randomUUID(); }); }],
  ['changed identity', value => { body(value.records[0], row => { row.Id = crypto.randomUUID(); }); }],
  ['private record', value => { body(value.records[0], row => { row.IsPublic = false; }); }],
  ['native revision in packet', value => { body(value.records[0], row => { row._rev = '8-controlled'; }); }],
  ['child graph', value => { body(value.records[0], row => { row.Children = [{ Id: crypto.randomUUID() }]; }); }],
  ['numeric metadata', value => { body(value.records[0], row => { row.MetaData.number = 9007199254740992; }); }],
  ['oversized metadata', value => { body(value.records[0], row => { row.MetaData.value = 'x'.repeat(1025); }); }],
  ['unbounded description', value => { body(value.records[0], row => { row.Description = 'x'.repeat(1025); }); }]
];
for (const [name, mutate] of invalid) test('PouchDB source rejects ' + name + ' before native I/O', () => {
  const value = sample(); mutate(value);
  assert.throws(() => captureBundle(JSON.stringify(value)));
});

test('PouchDB rejects duplicate escaped/case-folded keys rather than taking the last value', () => {
  const value = sample();
  assert.throws(() => captureBundle(JSON.stringify(value).replace('"schema":1', '"schema":1,"sch\\u0065ma":1')), /Duplicate/);
  const row = JSON.parse(Buffer.from(value.records[0].jsonBase64, 'base64').toString());
  body(value.records[0], JSON.stringify(row).replace('"Name":', '"name":"duplicate","Name":'));
  assert.throws(() => captureBundle(JSON.stringify(value)), /Duplicate/);
});

test('PouchDB rejects noncanonical base64, invalid UTF8 and oversized envelopes', () => {
  let value = sample(); value.records[0].jsonBase64 += '\n';
  assert.throws(() => captureBundle(JSON.stringify(value)), /encoding/);
  value = sample(); const bytes = Buffer.from([0xff]);
  value.records[0].jsonBase64 = bytes.toString('base64'); value.records[0].sha256 = hash(bytes);
  assert.throws(() => captureBundle(JSON.stringify(value)));
  assert.throws(() => captureBundle(' '.repeat(131073)), /Bounded/);
});

module.exports = { sample };

function health(change = {}) {
  const { status = 200, digest = 'source', instance = 'instance', version = '9.0.0', body = { db_name: 'oasis_holons', doc_count: 3, adapter: 'leveldb' } } = change;
  return new Response(typeof body === 'string' ? body : JSON.stringify(body), { status,
    headers: { 'X-OASIS-PouchDB-Source': digest, 'X-OASIS-PouchDB-Instance': instance, 'X-OASIS-PouchDB-Version': version } });
}

test('PouchDB readiness verifies one owned native database without forwarding credentials', async () => {
  let attempts = 0;
  await waitForNative(new AbortController().signal, 'source', 'instance', async (uri, options) => {
    attempts++; assert.equal(uri, 'http://127.0.0.1:13800/oasis_holons'); assert.equal(options.method, 'GET');
    assert.equal(options.redirect, 'error'); assert.equal(options.headers, undefined);
    return health();
  });
  assert.equal(attempts, 1);
});

for (const [label, change] of [
  ['foreign source', { digest: 'foreign' }], ['foreign instance', { instance: 'other-process' }],
  ['wrong SDK version', { version: '8.0.0' }], ['unauthorized source', { status: 403 }],
  ['wrong database', { body: { db_name: 'foreign', doc_count: 3, adapter: 'leveldb' } }],
  ['incomplete source', { body: { db_name: 'oasis_holons', doc_count: 2, adapter: 'leveldb' } }],
  ['different adapter', { body: { db_name: 'oasis_holons', doc_count: 3, adapter: 'memory' } }],
  ['oversized response', { body: ' '.repeat(16385) }]
]) test('PouchDB readiness rejects ' + label + ' without retry', async () => {
  let attempts = 0;
  await assert.rejects(waitForNative(new AbortController().signal, 'source', 'instance', async () => { attempts++; return health(change); }));
  assert.equal(attempts, 1);
});

test('PouchDB startup polling is limited to explicit connection failures and observes cancellation', async () => {
  let attempts = 0;
  const refused = () => Object.assign(new TypeError('Connection refused'), { cause: { code: 'ECONNREFUSED' } });
  await waitForNative(new AbortController().signal, 'source', 'instance', async () => { if (++attempts === 1) throw refused(); return health(); });
  assert.equal(attempts, 2);
  const broken = Error('Programming error');
  await assert.rejects(waitForNative(new AbortController().signal, 'source', 'instance', async () => { throw broken; }), error => error === broken);
  const cancelled = new AbortController(); cancelled.abort();
  await assert.rejects(waitForNative(cancelled.signal, 'source', 'instance', async () => { throw Error('Must not dispatch'); }), { name: 'AbortError' });
});

test('PouchDB service configuration binds its child instance to the captured source without native I/O', () => {
  const packet = JSON.stringify(sample());
  const environment = { OASIS_SANDBOX_POUCHDB: 'true', OASIS_SANDBOX_POUCHDB_BOOTSTRAP_JSON: packet };
  const first = serviceConfiguration(environment), second = serviceConfiguration(environment);
  assert.equal(first.command, process.execPath); assert.match(first.args[0], /host-sandbox-pouchdb\.cjs$/);
  assert.equal(first.env.OASIS_SANDBOX_POUCHDB_BOOTSTRAP_JSON, packet);
  assert.match(first.env.OASIS_SANDBOX_POUCHDB_INSTANCE, /^[a-f0-9]{32}$/);
  assert.notEqual(first.env.OASIS_SANDBOX_POUCHDB_INSTANCE, second.env.OASIS_SANDBOX_POUCHDB_INSTANCE);
  for (const flag of [undefined, 'false', 'TRUE', '1']) assert.throws(() => serviceConfiguration({ ...environment, OASIS_SANDBOX_POUCHDB: flag }), /Explicit/);
});

function supervised(check) {
  const source = serviceConfiguration({ OASIS_SANDBOX_POUCHDB: 'true', OASIS_SANDBOX_POUCHDB_BOOTSTRAP_JSON: JSON.stringify(sample()) });
  const signals = new EventEmitter(), children = [];
  const launch = (command, args) => {
    const child = new EventEmitter(); child.kills = [];
    child.kill = signal => { child.kills.push(signal); child.emit('exit', 0); };
    children.push({ command, args, child }); return child;
  };
  const done = supervise({ services: [{ ...source, check }], launch, signals, stopMilliseconds: 100 });
  return { signals, children, done };
}

test('PouchDB supervisor starts WEB4 only after source readiness and stops it on native exit', async () => {
  let release;
  const started = new Promise(resolve => { release = resolve; });
  const work = supervised(() => started);
  assert.equal(work.children.length, 1); assert.equal(work.children[0].command, process.execPath);
  assert.match(work.children[0].args[0], /host-sandbox-pouchdb\.cjs$/);
  release(); await new Promise(resolve => setImmediate(resolve));
  assert.equal(work.children[1].command, 'dotnet');
  work.children[0].child.emit('exit', 0);
  assert.equal(await work.done, 1); assert.deepEqual(work.children[1].child.kills, ['SIGTERM']);
});

test('PouchDB source failure prevents WEB4 startup and closes only its owned child', async () => {
  const work = supervised(async () => { throw Error('PouchDB source unavailable'); });
  await assert.rejects(work.done, /PouchDB source unavailable/);
  assert.equal(work.children.length, 1); assert.deepEqual(work.children[0].child.kills, ['SIGTERM']);
});

test('PouchDB startup cancellation observes readiness cancellation before shutdown', async () => {
  let observed = false;
  const work = supervised(signal => new Promise((_, reject) => signal.addEventListener('abort', () => {
    observed = true; reject(Error('Owned startup cancelled'));
  }, { once: true })));
  work.signals.emit('SIGTERM');
  assert.equal(await work.done, 0); assert.equal(observed, true); assert.equal(work.children.length, 1);
  assert.equal(work.signals.listenerCount('SIGTERM'), 0);
});

test('PouchDB container graph pins the complete native runtime without parent/extraneous packages', () => {
  const directory = path.resolve(__dirname, '../../Docker/PouchDbRuntime');
  const manifest = JSON.parse(fs.readFileSync(path.join(directory, 'package.json')));
  const lock = JSON.parse(fs.readFileSync(path.join(directory, 'package-lock.json')));
  assert.deepEqual(manifest.dependencies, { pouchdb: '9.0.0', 'express-pouchdb': '4.2.0', express: '4.21.2', 'pouchdb-all-dbs': '1.1.1' });
  assert.deepEqual(lock.packages[''].dependencies, manifest.dependencies);
  for (const [key, value] of Object.entries(lock.packages)) {
    if (!key) continue;
    assert.ok(key.startsWith('node_modules/')); assert.equal(value.extraneous, undefined);
    assert.match(value.integrity, /^sha(1|256|384|512)-/); assert.match(value.resolved, /^https:\/\//);
  }
});

test('PouchDB command rejects a non-sandbox host before opening any source', () => {
  const result = spawnSync(process.execPath, [path.resolve(__dirname, '../../Docker/host-sandbox-pouchdb.cjs')], {
    env: { ...process.env, OASIS_DNA_JSON: '{"OASIS":{"Sandbox":{"Enabled":false}}}', OASIS_SANDBOX_POUCHDB: 'true' }, encoding: 'utf8'
  });
  assert.equal(result.status, 1); assert.match(result.stderr, /requires OASIS.Sandbox.Enabled/);
});

test('PouchDB runtime rejects missing listener ownership before source preparation or signal registration', async () => {
  const signals = new EventEmitter();
  const directory = path.join(__dirname, 'must-not-create-pouch-source');
  assert.equal(fs.existsSync(directory), false);
  await assert.rejects(run({ bundle: captureBundle(JSON.stringify(sample())), directory,
    runtimeRoot: directory, instance: undefined, signals }), /listener instance required/);
  assert.equal(fs.existsSync(directory), false); assert.equal(signals.listenerCount('SIGTERM'), 0);
});
