'use strict';
const test = require('node:test');
const assert = require('node:assert/strict');
const { review, provision } = require('../../Docker/provision-sandbox-dapr.cjs');
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
