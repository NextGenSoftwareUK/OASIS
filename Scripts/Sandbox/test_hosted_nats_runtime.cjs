'use strict';
const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const os = require('node:os');
const path = require('node:path');
const crypto = require('node:crypto');
const { captureBundle, prepare, prepareTls, serverConfiguration, waitForNative, serviceConfiguration } = require('../../Docker/host-sandbox-nats.cjs');
const hash = data => crypto.createHash('sha256').update(data).digest('hex');
const descriptor = JSON.stringify({ schema: 1, endpoint: 'tls://127.0.0.1:24222',
  bucket: 'cosmic_cql_public', sourceOwner: 'b3ec9989-1733-450f-aba3-1015f6712c33',
  ids: ['d9679006-14cd-49d9-8b28-de6b5f776af8', '639ce059-c192-4e04-9d13-85ae0a52efc6',
    '7f7911d3-bb0c-40ae-8647-2a3a34a1aeb5'] });
function wire() {
  return { schema: 1, sourceSha256: hash(descriptor), files: ['public', 'foreign'].flatMap(bucket =>
    ['meta.inf', 'meta.sum', 'msgs/1.blk'].map(name => ({
      path: `jetstream/$G/streams/KV_cosmic_cql_${bucket}/${name}`,
      base64: Buffer.from(name).toString('base64'), sha256: hash(name) }))) };
}
test('qualified native bytes and descriptor are bound before placement', () => {
  assert.equal(captureBundle(JSON.stringify(wire()), descriptor).files.length, 6);
});
for (const [name, mutate] of [
  ['traversal', bundle => { bundle.files[0].path = '../server.conf'; }],
  ['duplicate native path', bundle => { bundle.files[1].path = bundle.files[0].path; }],
  ['altered bytes', bundle => { bundle.files[0].base64 = 'eA=='; }],
  ['noncanonical encoding', bundle => { bundle.files[0].base64 += '\n'; }],
  ['descriptor mismatch', bundle => { bundle.sourceSha256 = '0'.repeat(64); }],
  ['missing native stream', bundle => { bundle.files.pop(); }],
  ['unknown bundle field', bundle => { bundle.reseed = true; }],
]) test(`reject ${name}`, () => {
  const bundle = wire(); mutate(bundle);
  assert.throws(() => captureBundle(JSON.stringify(bundle), descriptor));
});
test('reject duplicate JSON keys before accepting native bytes', () => {
  const text = JSON.stringify(wire()).replace('"schema":1', '"schema":1,"schema":1');
  assert.throws(() => captureBundle(text, descriptor));
});
async function fixture(t) {
  const parent = await fs.realpath(await fs.mkdtemp(path.join(os.tmpdir(), 'oasis-nats-owned-test-')));
  t.after(async () => {
    assert.equal(await fs.realpath(parent), parent);
    assert.ok(path.basename(parent).startsWith('oasis-nats-owned-test-'));
    await fs.rm(parent, { recursive: true });
  });
  return path.join(parent, 'source');
}
test('first placement is exclusive and acknowledged restart preserves native changes', async t => {
  const directory = await fixture(t);
  const bundle = captureBundle(JSON.stringify(wire()), descriptor);
  await prepare(bundle, directory);
  const file = path.join(directory, 'store', bundle.files[0].path);
  await fs.writeFile(file, 'native-owned working change');
  await prepare(bundle, directory);
  assert.equal(await fs.readFile(file, 'utf8'), 'native-owned working change');
});
test('partial placement is never reseeded', async t => {
  const directory = await fixture(t);
  await fs.mkdir(directory);
  await fs.writeFile(path.join(directory, 'bootstrap.intent.json'), '{}');
  await assert.rejects(prepare(captureBundle(JSON.stringify(wire()), descriptor), directory), /reconciliation/);
  assert.deepEqual(await fs.readdir(directory), ['bootstrap.intent.json']);
});
test('acknowledged source cannot be replaced by a different bundle', async t => {
  const directory = await fixture(t);
  await prepare(captureBundle(JSON.stringify(wire()), descriptor), directory);
  const other = wire(); other.files[0].base64 = 'eA=='; other.files[0].sha256 = hash('x');
  await assert.rejects(prepare(captureBundle(JSON.stringify(other), descriptor), directory), /reconciliation/);
});
test('missing acknowledged native file stops restart', async t => {
  const directory = await fixture(t);
  const bundle = captureBundle(JSON.stringify(wire()), descriptor);
  await prepare(bundle, directory);
  await fs.unlink(path.join(directory, 'store', bundle.files[0].path));
  await assert.rejects(prepare(bundle, directory));
});
test('native configuration exposes only the restricted reader on loopback', () => {
  const config = serverConfiguration('/data/COSMIC/SandboxNATS', 'x'.repeat(43));
  assert.equal(config.host, '127.0.0.1');
  assert.equal(config.authorization.users.length, 1);
  const reader = config.authorization.users[0];
  assert.equal(reader.user, 'cql_reader');
  assert.deepEqual(reader.permissions.subscribe.allow, ['_INBOX.>']);
  assert.ok(reader.permissions.publish.allow.every(subject => !subject.includes('foreign') && !subject.startsWith('$KV.')));
});
test('invalid credentials and missing explicit flag stop before placement', async () => {
  assert.throws(() => serverConfiguration('/data/COSMIC/SandboxNATS', 'short'));
  await assert.rejects(serviceConfiguration({}), /explicit flag/);
});
test('unknown partial TLS state cannot regenerate credentials or certificate', async t => {
  const directory = await fixture(t);
  await fs.mkdir(directory);
  await fs.writeFile(path.join(directory, 'tls.intent.json'), '{}');
  await assert.rejects(prepareTls(directory, 'x'.repeat(43)), /reconciliation/);
  assert.deepEqual(await fs.readdir(directory), ['tls.intent.json']);
});
test('readiness does not retry authentication or certificate errors', async () => {
  const error = Object.assign(Error('certificate rejected'), { code: 'CERT_HAS_EXPIRED' });
  let calls = 0;
  await assert.rejects(waitForNative(Buffer.alloc(0), 'x'.repeat(43), new AbortController().signal,
    async () => { calls++; throw error; }), error);
  assert.equal(calls, 1);
});
test('startup polling retries only a refused connection', async () => {
  let calls = 0;
  await waitForNative(Buffer.alloc(0), 'x'.repeat(43), new AbortController().signal,
    async () => { if (++calls === 1) throw Object.assign(Error('not listening'), { code: 'ECONNREFUSED' }); });
  assert.equal(calls, 2);
});
test('persistent real TLS identity survives restart and rejects credential changes', async t => {
  const openssl = process.platform === 'win32' ? 'C:/Program Files/Git/usr/bin/openssl.exe' : 'openssl';
  const directory = await fixture(t);
  await fs.mkdir(directory);
  const ca = await prepareTls(directory, 'x'.repeat(43), openssl);
  const key = await fs.readFile(path.join(directory, 'server.key'));
  assert.deepEqual(await prepareTls(directory, 'x'.repeat(43), openssl), ca);
  assert.deepEqual(await fs.readFile(path.join(directory, 'server.key')), key);
  await assert.rejects(prepareTls(directory, 'y'.repeat(43), openssl), /configuration differs/);
  assert.deepEqual(await fs.readFile(path.join(directory, 'ca.pem')), ca);
});
