'use strict';
const assert = require('node:assert/strict');
const crypto = require('node:crypto');
const fs = require('node:fs/promises');
const path = require('node:path');

const providers = { GUNOASIS: 'SandboxGUN', OrbitDBOASIS: 'SandboxOrbitDB' };
const canonicalId = value => typeof value === 'string' &&
  /^[a-f0-9]{8}-(?:[a-f0-9]{4}-){3}[a-f0-9]{12}$/.test(value) &&
  value !== '00000000-0000-0000-0000-000000000000';

function review(plan) {
  if (!plan || Object.keys(plan).sort().join(',') !== 'actor,generation,ids,provider,schema' ||
      plan.schema !== 1 || !Object.hasOwn(providers, plan.provider) ||
      !canonicalId(plan.actor) || !canonicalId(plan.generation) ||
      !Array.isArray(plan.ids) || plan.ids.length !== 6 || !plan.ids.every(canonicalId) ||
      new Set([plan.actor, plan.generation, ...plan.ids]).size !== 8)
    throw Error('One canonical provider generation, actor and six distinct IDs are required.');
  return { schema: 1, provider: plan.provider, generation: plan.generation,
    actor: plan.actor, ids: [...plan.ids] };
}

const digest = plan => crypto.createHash('sha256').update(JSON.stringify(plan)).digest('hex');

function record(plan, position) {
  return { id: plan.ids[position], createdByAvatarId: plan.actor,
    name: ['Espresso', 'Latte', 'Tea', 'Private record', 'Deleted record'][position],
    description: 'COSMIC hosted ' + plan.provider + ' teaching source',
    isPublic: position !== 3, isDeleted: position === 4,
    children: [{ id: plan.ids[3], isPublic: false }] };
}

async function writeNew(file, value) {
  const handle = await fs.open(file, 'wx');
  try { await handle.writeFile(JSON.stringify(value)); await handle.sync(); }
  finally { await handle.close(); }
}

async function nativeStore(provider, directory, creating, address) {
  const runtime = provider === 'GUNOASIS' ? 'gun' : 'orbitdb';
  const { openTeachingStore } = await import('/opt/teaching/' + runtime + '/teaching-store.mjs');
  if (provider === 'GUNOASIS') {
    const store = await openTeachingStore(directory, creating);
    return {
      address: null,
      write: row => store.seed(row.id, row, row.isDeleted),
      read: async id => {
        const node = await store.read(id);
        if (node === undefined) return undefined;
        assert.equal(node?._?.['#'], 'oasis.holons/' + id);
        const row = JSON.parse(node.data_json);
        assert.equal(node.is_deleted, String(row.isDeleted));
        return row;
      }
    };
  }
  const store = await openTeachingStore(directory, creating ? undefined : address);
  return { address: store.db.address, write: row => store.db.put(row.id, row),
    read: id => store.db.get(id), close: store.close };
}

async function sourceDirectory(plan, root) {
  if (await fs.realpath(root) !== root) throw Error('The reviewed source parent must be a real directory.');
  return path.join(root, providers[plan.provider]);
}

async function createSource(input, { root = '/data/COSMIC', open = nativeStore } = {}) {
  const plan = review(input);
  const directory = await sourceDirectory(plan, root);
  // Exclusive creation owns the generation across processes. Existing or uncertain
  // sources require reconciliation, never another seed attempt.
  await fs.mkdir(directory);
  await writeNew(path.join(directory, 'source.intent.json'), plan);
  const store = await open(plan.provider, directory, true);
  let receipt;
  try {
    for (let i = 0; i < 5; i++) await store.write(record(plan, i));
    receipt = { schema: 1, planSha256: digest(plan), address: store.address };
  } finally { if (store.close) await store.close(); }
  await writeNew(path.join(directory, 'source.acknowledged.json'), receipt);
  return receipt;
}

async function verifySource(input, { root = '/data/COSMIC', open = nativeStore } = {}) {
  const plan = review(input);
  const directory = await sourceDirectory(plan, root);
  assert.equal(await fs.realpath(directory), directory);
  const original = JSON.parse(await fs.readFile(path.join(directory, 'source.intent.json'), 'utf8'));
  assert.deepEqual(original, plan);
  const acknowledgement = JSON.parse(await fs.readFile(path.join(directory, 'source.acknowledged.json'), 'utf8'));
  assert.equal(acknowledgement.planSha256, digest(plan));
  // Reserve verification before opening the native store. An interrupted
  // verification is retained for explicit diagnosis rather than replayed.
  await writeNew(path.join(directory, 'verification.intent.json'), { planSha256: digest(plan) });
  const store = await open(plan.provider, directory, false, acknowledgement.address);
  try {
    assert.equal(store.address, acknowledgement.address);
    for (let i = 0; i < 5; i++) assert.deepEqual(await store.read(plan.ids[i]), record(plan, i));
    assert.equal(await store.read(plan.ids[5]), undefined);
  } finally { if (store.close) await store.close(); }
  const proof = { schema: 1, planSha256: digest(plan), address: acknowledgement.address,
    publicRecords: 3, privateRecords: 1, deletedRecords: 1, missingRecords: 1 };
  await writeNew(path.join(directory, 'source.verified.json'), proof);
  return proof;
}

module.exports = { review, createSource, verifySource };
