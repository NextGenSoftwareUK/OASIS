'use strict';
const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const os = require('node:os');
const path = require('node:path');
const { seedOnce } = require('../../Docker/DaprRuntime/seed.cjs');
const prefix = '12345678-1234-4234-8234-123456789ab';
const records = ['e', '1', '2', '3'].map(suffix => ({
  key: 'oasis/holon/' + (prefix + suffix).replaceAll('-', ''),
  value: JSON.stringify({ id: prefix + suffix, createdByAvatarId: prefix + '0', isPublic: true,
    deletedDate: suffix === '3' ? '2026-11-01T00:00:00Z' : '0001-01-01T00:00:00' })
}));
for (const outcome of ['acknowledged', 'unknown']) test(`a ${outcome} source cannot be submitted twice`, async t => {
  const root = await fs.realpath(await fs.mkdtemp(path.join(os.tmpdir(), 'cosmic-dapr-seed-')));
  t.after(() => fs.rm(root, { recursive: true }));
  let requests = 0;
  t.mock.method(globalThis, 'fetch', async (url, request) => {
    requests++;
    const intent = JSON.parse(await fs.readFile(path.join(root, 'seed.intent.json'), 'utf8'));
    assert.equal(intent.replayAllowed, false);
    assert.equal(JSON.parse(request.body).length, 4);
    assert.equal(request.redirect, 'error');
    if (outcome === 'unknown') throw Error('Connection lost after send');
    return new Response(null, { status: 204 });
  });
  const create = () => seedOnce(root, prefix, records, 'a'.repeat(64), new AbortController().signal);
  if (outcome === 'unknown') await assert.rejects(create, /Connection lost/); else await create();
  await assert.rejects(create, { code: 'EEXIST' });
  assert.equal(requests, 1);
  if (outcome === 'unknown') await assert.rejects(fs.stat(path.join(root, 'seed.acknowledged.json')), { code: 'ENOENT' });
});
test('foreign authority fails before writing an intent or sending a request', async t => {
  t.mock.method(globalThis, 'fetch', () => assert.fail('No source request allowed'));
  const rows = structuredClone(records); rows[0].value = rows[0].value.replace(prefix + '0', prefix + '9');
  await assert.rejects(seedOnce('/unused', prefix, rows, 'a'.repeat(64), new AbortController().signal), /authority/);
});
