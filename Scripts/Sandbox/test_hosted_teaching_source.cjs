'use strict';
const assert = require('node:assert/strict');
const crypto = require('node:crypto');
const fs = require('node:fs/promises');
const path = require('node:path');
const test = require('node:test');
const { review, createSource, verifySource } = require('./hosted-teaching-source.cjs');

function plan(provider = 'GUNOASIS') {
  return { schema: 1, provider, generation: crypto.randomUUID(), actor: crypto.randomUUID(),
    ids: Array.from({ length: 6 }, () => crypto.randomUUID()) };
}

async function fixture(t) {
  const base = path.resolve('TestResults/TeachingBootstrapCurrent');
  await fs.mkdir(base, { recursive: true });
  const root = await fs.mkdtemp(path.join(base, 'owned-'));
  t.after(async () => {
    assert.equal(path.dirname(root), base);
    await fs.rm(root, { recursive: true });
  });
  const rows = new Map();
  let writes = 0;
  const open = async () => ({ address: null,
    write: async row => { writes++; rows.set(row.id, structuredClone(row)); },
    read: async id => rows.get(id) });
  return { root, open, rows, count: () => writes };
}

test('invalid or aliased identities fail before provisioning', () => {
  const original = plan();
  assert.throws(() => review({ ...original, provider: 'unreviewed' }));
  assert.throws(() => review({ ...original, actor: original.ids[0] }));
  assert.throws(() => review({ ...original, ids: original.ids.slice(0, 3) }));
  assert.throws(() => review({ ...original, extra: true }));
});

test('native writes follow the durable intent and independent verification checks all record states', async t => {
  const f = await fixture(t);
  const input = plan();
  const open = async (...args) => {
    assert.deepEqual(JSON.parse(await fs.readFile(path.join(f.root, 'SandboxGUN/source.intent.json'))), input);
    return f.open(...args);
  };
  const receipt = await createSource(input, { ...f, open });
  assert.equal(f.count(), 5);
  const proof = await verifySource(input, f);
  assert.equal(proof.planSha256, receipt.planSha256);
  assert.equal(proof.publicRecords, 3);
  assert.equal(proof.privateRecords, 1);
  assert.equal(proof.deletedRecords, 1);
  assert.equal(proof.missingRecords, 1);
  await assert.rejects(verifySource(input, f), { code: 'EEXIST' });
  assert.equal(f.count(), 5);
});

test('an acknowledged source cannot be seeded again', async t => {
  const f = await fixture(t);
  const input = plan();
  await createSource(input, f);
  await assert.rejects(createSource(input, f), { code: 'EEXIST' });
  assert.equal(f.count(), 5);
});

test('native uncertainty preserves intent and refuses another write attempt', async t => {
  const f = await fixture(t);
  const input = plan();
  let writes = 0;
  const open = async () => ({ address: null, write: async () => {
    if (++writes === 2) throw Error('simulated uncertain native outcome');
  } });
  await assert.rejects(createSource(input, { ...f, open }), /uncertain native outcome/);
  assert.deepEqual(JSON.parse(await fs.readFile(path.join(f.root, 'SandboxGUN/source.intent.json'))), input);
  await assert.rejects(fs.stat(path.join(f.root, 'SandboxGUN/source.acknowledged.json')), { code: 'ENOENT' });
  await assert.rejects(createSource(input, { ...f, open }), { code: 'EEXIST' });
  assert.equal(writes, 2);
});

test('concurrent creators admit only one native writer', async t => {
  const f = await fixture(t);
  const outcomes = await Promise.allSettled([createSource(plan(), f), createSource(plan(), f)]);
  assert.equal(outcomes.filter(row => row.status === 'fulfilled').length, 1);
  assert.equal(outcomes.filter(row => row.status === 'rejected').length, 1);
  assert.equal(f.count(), 5);
});

test('changed source identities are rejected before opening the reader', async t => {
  const f = await fixture(t);
  const input = plan('OrbitDBOASIS');
  await createSource(input, f);
  let opened = false;
  await assert.rejects(verifySource({ ...input, actor: crypto.randomUUID() }, {
    ...f, open: async () => { opened = true; return f.open(); }
  }));
  assert.equal(opened, false);
});
