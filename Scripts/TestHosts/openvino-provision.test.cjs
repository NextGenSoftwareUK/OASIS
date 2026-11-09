'use strict';
const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const os = require('node:os');
const path = require('node:path');
const crypto = require('node:crypto');
const { provision } = require('../../Docker/provision-sandbox-openvino.cjs');
const { verifyModels, modelConfiguration } = require('../../Docker/host-sandbox-openvino.cjs');
const original = require('./openvino-model-server-pins.json');
async function fixture(t) {
  const parent = await fs.realpath(await fs.mkdtemp(path.join(os.tmpdir(), 'oasis-ovms-provision-')));
  t.after(async () => {
    assert.equal(path.dirname(parent), await fs.realpath(os.tmpdir()));
    assert(path.basename(parent).startsWith('oasis-ovms-provision-'));
    await fs.rm(parent, { recursive: true, force: true });
  });
  const pins = structuredClone(original);
  // Synthetic one-byte test data; no actual model or network download.
  for (const pin of Object.values(pins.files)) {
    pin.bytes = 1; pin.sha256 = crypto.createHash('sha256').update('x').digest('hex');
  }
  return { directory: path.join(parent, 'models'), pins };
}
test('builds three verified versions from two single-attempt pinned reads', async t => {
  const { directory, pins } = await fixture(t); const calls = [];
  await provision(directory, pins, async (uri, options) => {
    calls.push(uri); assert.equal(options.redirect, 'error'); return new Response('x');
  });
  assert.equal(calls.length, 2);
  assert(calls.every(uri => uri.includes('/' + pins.sourceCommit + '/src/test/dummy/1/')));
  await verifyModels(directory, modelConfiguration(pins), pins);
});
test('refuses existing target before downloading or overwriting', async t => {
  const { directory, pins } = await fixture(t); await fs.mkdir(directory); let calls = 0;
  await assert.rejects(provision(directory, pins, async () => { calls++; return new Response('x'); }), { code: 'EEXIST' });
  assert.equal(calls, 0);
});
test('wrong hash cannot produce an accepted configuration', async t => {
  const { directory, pins } = await fixture(t); let calls = 0;
  await assert.rejects(provision(directory, pins, async () => { calls++; return new Response('z'); }), /hash differs/);
  assert.equal(calls, 1);
  await assert.rejects(fs.stat(path.join(directory, 'config.json')), { code: 'ENOENT' });
});
test('oversized source fails before any model files are accepted', async t => {
  const { directory, pins } = await fixture(t);
  await assert.rejects(provision(directory, pins, async () => new Response('xx')), /byte bound/);
  assert.deepEqual(await fs.readdir(directory), []);
});
test('source HTTP failure is retained as failure without retry', async t => {
  const { directory, pins } = await fixture(t); let calls = 0;
  await assert.rejects(provision(directory, pins, async () => { calls++; return new Response('', { status: 503 }); }), /HTTP200/);
  assert.equal(calls, 1);
});
