'use strict';
const net = require('node:net');
const { EventEmitter } = require('node:events');
const test = require('node:test');
const assert = require('node:assert/strict');
const crypto = require('node:crypto');
const path = require('node:path');
const os = require('node:os');
const fs = require('node:fs/promises');
const runtime = require('../../Docker/host-sandbox-dapr.cjs');
test('unexpected native handshake diagnostics redact the reader credential and close its socket', async t => {
  const password = 'd'.repeat(64), socket = new EventEmitter(); let closed = false;
  socket.setTimeout = () => socket;
  socket.destroy = () => { closed = true; };
  socket.write = () => socket.emit('data', Buffer.from('+OK\r\n$64\r\n' + password + '\r\n'));
  t.mock.method(net, 'createConnection', () => { process.nextTick(() => socket.emit('connect')); return socket; });
  await assert.rejects(runtime.probeRedis(password, new AbortController().signal), error => {
    assert.match(error.message, /response differs/);
    assert.ok(error.message.includes('[redacted]'));
    assert.ok(!error.message.includes(password));
    return true;
  });
  assert.equal(closed, true);
});
function fixture() {
  const owner = crypto.randomUUID(); const ids = Array.from({ length: 4 }, () => crypto.randomUUID());
  const token = 'a'.repeat(64);
  const source = { schema: 1, sourceOwner: owner, ids: ids.slice(0, 3),
    sidecarTokenSha256: crypto.createHash('sha256').update(token).digest('hex'),
    records: ids.map((id, index) => ({ id, etag: String(index + 1), value: JSON.stringify({ id,
      createdByAvatarId: owner, isPublic: true, deletedDate: index === 3 ? '2026-11-01T00:00:00Z' : '0001-01-01T00:00:00' }) })) };
  const environment = { OASIS_SANDBOX_DAPR: 'true', CQL_DAPR_QUERY_STORE: 'cosmic-teaching',
    CQL_DAPR_QUERY_GRPC_ENDPOINT: 'http://127.0.0.1:54862/', CQL_DAPR_QUERY_PROBE_ID: ids[0],
    CQL_DAPR_QUERY_SIDECAR_TOKEN: token, CQL_PUBLIC_READ_CATALOGUES_JSON: JSON.stringify({ DaprOASIS: ids.slice(0, 3) }) };
  return { source, environment, token };
}
test('source binding requires its original owner, catalogue and sidecar identity', () => {
  const { source, environment } = fixture();
  assert.equal(runtime.sourceDescriptor(source, environment), source);
  for (const patch of [{ CQL_DAPR_QUERY_STORE: 'other' }, { CQL_DAPR_QUERY_GRPC_ENDPOINT: 'http://example.invalid/' },
    { CQL_DAPR_QUERY_PROBE_ID: crypto.randomUUID() }, { CQL_DAPR_QUERY_SIDECAR_TOKEN: 'b'.repeat(64) },
    { CQL_PUBLIC_READ_CATALOGUES_JSON: '{}' }, { OASIS_SANDBOX_DAPR: 'false' }])
    assert.throws(() => runtime.sourceDescriptor(source, { ...environment, ...patch }));
});
test('duplicate, foreign or missing original records cannot enter recovery', () => {
  const { source, environment } = fixture();
  for (const patch of [{ records: source.records.slice(1) }, { records: [source.records[0], ...source.records.slice(0, 3)] },
    { sourceOwner: crypto.randomUUID() }, { ids: [source.ids[0], source.ids[0], source.ids[1]] }])
    assert.throws(() => runtime.sourceDescriptor({ ...source, ...patch }, environment));
});
test('missing original source is refused without creating or bootstrapping it', async () => {
  const { environment } = fixture(); const directory = path.join(os.tmpdir(), 'cosmic-dapr-not-created-' + crypto.randomUUID());
  await assert.rejects(runtime.serviceConfigurations(environment, directory));
  await assert.rejects(fs.access(directory));
});
test('recovery verifies exact values and ETags once through authenticated loopback reads', async t => {
  const { source, token } = fixture(); const original = global.fetch; const calls = [];
  t.after(() => { global.fetch = original; });
  global.fetch = async (url, options) => {
    calls.push({ url, options }); const row = source.records[calls.length - 1];
    return new Response(JSON.stringify(row.value), { status: 200, headers: { ETag: row.etag } });
  };
  await runtime.verifyRows(source, token, new AbortController().signal);
  assert.equal(calls.length, 4);
  for (const call of calls) {
    assert.equal(new URL(call.url).hostname, '127.0.0.1');
    assert.equal(call.options.headers['dapr-api-token'], token);
    assert.equal(call.options.redirect, 'error');
  }
});
test('a changed ETag stops immediately without another source request', async t => {
  const { source, token } = fixture(); const original = global.fetch; let calls = 0;
  t.after(() => { global.fetch = original; });
  global.fetch = async () => { calls++; return new Response(JSON.stringify(source.records[0].value), { headers: { ETag: 'changed' } }); };
  await assert.rejects(runtime.verifyRows(source, token, new AbortController().signal), /ETag/);
  assert.equal(calls, 1);
});
test('native response budget is enforced while streaming, without a query retry', async t => {
  const { source, token } = fixture(); const original = global.fetch; let calls = 0;
  t.after(() => { global.fetch = original; });
  global.fetch = async () => { calls++; return new Response('x'.repeat(8193), { headers: { ETag: source.records[0].etag } }); };
  await assert.rejects(runtime.verifyRows(source, token, new AbortController().signal), /budget/);
  assert.equal(calls, 1);
});
test('cancelled startup cannot issue a source read', async t => {
  const { source, token } = fixture(); const original = global.fetch; let calls = 0;
  t.after(() => { global.fetch = original; }); global.fetch = async () => { calls++; };
  const controller = new AbortController(); controller.abort(Error('cancelled'));
  await assert.rejects(runtime.verifyRows(source, token, controller.signal), /cancelled/);
  assert.equal(calls, 0);
});
