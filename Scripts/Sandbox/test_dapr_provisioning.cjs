'use strict';
const test = require('node:test');
const assert = require('node:assert/strict');
const crypto = require('node:crypto');
const { review, provision, reviewCommand, pendingSourceDescriptor } = require('../../Docker/provision-sandbox-dapr.cjs');
const plan = { schema: 1, prefix: '12345678-1234-4234-8234-123456789ab', readerPassword: 'a'.repeat(64), sidecarToken: 'b'.repeat(64) };
test('creation plan provides three public records and a separate tombstone under one owner', () => {
  const rows = review(plan).map(row => JSON.parse(row.value));
  assert.equal(new Set(rows.map(row => row.id)).size, 4);
  assert.equal(rows.filter(row => row.deletedDate === '0001-01-01T00:00:00').length, 3);
  assert.ok(rows.every(row => row.createdByAvatarId === plan.prefix + '0' && row.isPublic));
});
test('invalid or shared credentials are refused before native work', async () => {
  for (const invalid of [{ ...plan, readerPassword: plan.sidecarToken }, { ...plan, readerPassword: 'injected\nACL' }, { ...plan, prefix: '../data' }])
    await assert.rejects(provision(invalid), /required|differ/);
});
test('Windows cannot create a hosted Linux source', { skip: process.platform !== 'win32' }, async () => {
  await assert.rejects(provision(plan), /requires Linux/);
});
test('creation command binds the reviewed sandbox deployment and refuses an active reader', () => {
  const args = ['12345678-1234-4234-8234-123456789abc', '12345678-1234-4234-8234-123456789abd'];
  const environment = { RAILWAY_DEPLOYMENT_ID: args[0], RAILWAY_REPLICA_ID: args[1], OASIS_DNA_JSON: '{"OASIS":{"Sandbox":{"Enabled":true}}}' };
  const text = JSON.stringify(plan);
  assert.deepEqual(reviewCommand(args, environment, text), plan);
  assert.throws(() => reviewCommand(args, { ...environment, RAILWAY_REPLICA_ID: args[0] }, text), /reviewed/);
  assert.throws(() => reviewCommand(args, { ...environment, OASIS_SANDBOX_DAPR: 'true' }, text), /disabled/);
  assert.throws(() => reviewCommand(args, { ...environment, OASIS_DNA_JSON: '{}' }, text), /Sandbox.Enabled/);
  assert.throws(() => reviewCommand(args, environment, ' '.repeat(8193)), /budget/);
});
test('pending inspection binds the original operation, exact keys and original serialized payload', () => {
  const rows = review(plan), payload = rows.map(row => ({ key: 'oasis/holon/' + row.id.replaceAll('-', ''), value: row.value }));
  const creation = { schema: 1, prefix: plan.prefix, replayAllowed: false };
  const seed = { prefix: plan.prefix, replayAllowed: false, operation: '12345678-1234-4234-8234-123456789abc',
    keys: payload.map(row => row.key), payloadSha256: crypto.createHash('sha256').update(JSON.stringify(payload)).digest('hex') };
  assert.deepEqual(pendingSourceDescriptor(plan, creation, seed), rows);
  for (const changed of [{ ...seed, replayAllowed: true }, { ...seed, payloadSha256: '0'.repeat(64) },
    { ...seed, keys: seed.keys.slice(1) }, { ...seed, prefix: 'foreign' }, { ...seed, operation: 'unknown' }])
    assert.throws(() => pendingSourceDescriptor(plan, creation, changed), /intent differs/);
  assert.throws(() => pendingSourceDescriptor(plan, { ...creation, prefix: 'foreign' }, seed), /intent differs/);
});
