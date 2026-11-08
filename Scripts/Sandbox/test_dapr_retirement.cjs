'use strict';
const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const os = require('node:os');
const path = require('node:path');
const crypto = require('node:crypto');
const { snapshot, requireUnadmitted, reviewRetirement, archiveSource } = require('./DaprSourceRetirement.cjs');
const { durableExclusive } = require('../../Docker/DaprRuntime/seed.cjs');
const hash = value => crypto.createHash('sha256').update(value).digest('hex');
const operation = '12345678-1234-4234-8234-123456789abc';

test('retirement refuses runtime settings, grants, tariffs, presets and active readers', () => {
  requireUnadmitted({ OASIS_SANDBOX_DAPR: 'false' });
  for (const environment of [
    { OASIS_SANDBOX_DAPR: 'true' }, { CQL_DAPR_QUERY_STORE: '' },
    { CQL_PUBLIC_READ_CATALOGUES_JSON: '{"DaprOASIS":[]}' },
    { CQL_HOSTED_QUERY_EXAMPLES_JSON: '{"DaprOASIS":[]}' },
    { CQL_HOSTED_QUERY_EXAMPLES__DaprOASIS: '[]' },
    { PREPARED_STORAGE_ADMISSION_JSON: '{"queryOnlyProviders":["DaprOASIS"]}' },
    { PREPARED_STORAGE_ADMISSION_JSON: '{"nativeProviders":[{"provider":"DaprOASIS"}]}' },
    { PREPARED_STORAGE_ADMISSION_JSON: '{"queryTariffs":[{"provider":"DaprOASIS"}]}' }])
    assert.throws(() => requireUnadmitted(environment), /disabled|enrolled|admission/);
});

test('retirement preserves unknown outcome and binds original operation and AOF bytes', () => {
  const plan = { prefix: '12345678-1234-4234-8234-123456789ab' }, seed = { operation };
  const store = [{ name: 'AOF/aof.log.0', bytes: 2048, sha256: 'a'.repeat(64) }];
  const inspection = { schema: 1, prefix: plan.prefix, originalOperation: operation, businessWritesAllowed: false, storage: store };
  const expected = hash(JSON.stringify(store));
  const proof = reviewRetirement(plan, seed, inspection, store, expected);
  assert.equal(proof.originalSaveOutcome, 'unknown');
  assert.equal(proof.originalSaveResponseAcknowledged, false);
  assert.equal(proof.businessWrites, 0);
  for (const changed of [{ ...inspection, prefix: 'foreign' }, { ...inspection, originalOperation: 'other' },
    { ...inspection, businessWritesAllowed: true }, { ...inspection, storage: [] }])
    assert.throws(() => reviewRetirement(plan, seed, changed, store, expected), /unchanged AOF/);
  assert.throws(() => reviewRetirement(plan, seed, inspection, [{ ...store[0], bytes: 512 }], expected), /unchanged AOF/);
});

async function fixture(t) {
  const root = await fs.mkdtemp(path.join(os.tmpdir(), 'cosmic-dapr-retirement-'));
  t.after(() => fs.rm(root, { recursive: true, force: true }));
  const source = path.join(root, 'source'), archive = path.join(root, 'archive');
  await fs.mkdir(path.join(source, 'store/AOF'), { recursive: true, mode: 0o700 });
  await fs.mkdir(path.join(source, 'inspection'), { mode: 0o700 });
  await fs.mkdir(archive, { mode: 0o700 });
  await fs.writeFile(path.join(source, 'store/AOF/aof.log.0'), Buffer.from([0, 1, 2, 255]), { mode: 0o600 });
  await fs.writeFile(path.join(source, 'inspection/failure.log'), 'retained failure', { mode: 0o600 });
  return { root, source, destination: path.join(archive, operation) };
}

test('inventory refuses redirected files before any archival', { skip: process.platform !== 'linux' }, async t => {
  const { source, root } = await fixture(t);
  await fs.writeFile(path.join(root, 'foreign'), 'foreign');
  await fs.symlink(path.join(root, 'foreign'), path.join(source, 'redirect'));
  await assert.rejects(snapshot(source), /boundary/);
});

test('archive preserves every original byte and never fabricates source/save acknowledgement', { skip: process.platform !== 'linux' }, async t => {
  const { source, destination } = await fixture(t), before = await snapshot(source);
  const proof = { originalOperation: operation, originalSaveOutcome: 'unknown', businessWrites: 0, originalSaveResponseAcknowledged: false };
  const receipt = await archiveSource(source, destination, proof, durableExclusive);
  assert.deepEqual(await snapshot(path.join(destination, 'source')), before);
  assert.equal(receipt.sourceFilesSha256, hash(JSON.stringify(before)));
  assert.equal(receipt.originalSaveOutcome, 'unknown');
  for (const name of ['seed.acknowledged.json', 'source.acknowledged.json'])
    await assert.rejects(fs.stat(path.join(destination, 'source', name)), { code: 'ENOENT' });
  await assert.rejects(fs.stat(source), { code: 'ENOENT' });
  await assert.rejects(archiveSource(source, destination, proof, durableExclusive), { code: 'ENOENT' });
});

test('existing archive stops before moving the original source', { skip: process.platform !== 'linux' }, async t => {
  const { source, destination } = await fixture(t), before = await snapshot(source);
  await fs.mkdir(destination);
  await assert.rejects(archiveSource(source, destination, {}, durableExclusive), { code: 'EEXIST' });
  assert.deepEqual(await snapshot(source), before);
});

test('changed source bytes cannot earn an archive acknowledgement', { skip: process.platform !== 'linux' }, async t => {
  const { source, destination } = await fixture(t);
  const changingJournal = async (file, value) => {
    await durableExclusive(file, value);
    await fs.writeFile(path.join(source, 'inspection/failure.log'), 'changed during control');
  };
  await assert.rejects(archiveSource(source, destination, {}, changingJournal), /Retained source bytes differ/);
  await assert.rejects(fs.stat(path.join(destination, 'retirement.acknowledged.json')), { code: 'ENOENT' });
});
