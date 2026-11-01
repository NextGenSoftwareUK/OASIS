'use strict';
const { test } = require('node:test');
const assert = require('node:assert/strict');
const crypto = require('node:crypto');
const { validateContinuation, loadContinuation, assertContinuationRoute } = require('./hosted-source-continuation.cjs');
const id = number => '00000000-0000-0000-0000-' + String(number).padStart(12, '0');
function fixture() {
  const owner = id(1), copies = [id(2), id(3)], nativeIds = [id(4), id(5), id(6)], operation = id(9);
  const acknowledgements = copies.map((value, index) => ({ id: value, requestKey: id(10 + index),
    isSaved: true, ownerId: owner, isPublic: false, version: 1 }));
  const pair = count => ({ result: { rows: [
    ...nativeIds.slice(0, count).map(value => ({ selected: { provider: 'RadixOASIS', row: { id: value, isPublic: true } } })),
    ...copies.map(value => ({ selected: { provider: 'MongoDBOASIS', row: { id: value, createdByAvatarId: owner, isPublic: false } } }))
  ] } });
  const evidence = {
    'initial-flow.progress.json': { progress: { preparation: { saved: 2, existing: 0, keys: acknowledgements.map(row => row.requestKey) },
      five: pair(3), four: pair(2), failedHttp: { route: 'data/query-cql', status: 503, errorCode: 'SETTLEMENT_PENDING' } },
      steps: [{ status: 200 }, { status: 503, errorCode: 'SETTLEMENT_PENDING' }] },
    'original-native-http.json': { avatar: { id: owner }, source: { provider: 'RadixOASIS' },
      native: [3, 2].map(count => ({ result: { rows: nativeIds.slice(0, count).map(value => ({ id: value })) } })) },
    'original-settlement-state.json': { operationId: operation, state: 'settled', observedOutcome: 'succeeded',
      endpoint: 'POST /api/data/query-cql', receiptCount: 1, settledAtUtc: '2026-11-01T15:02:57.513Z' },
    'failure-log-observation.json': [{ operations: [operation] }]
  };
  const manifest = { schema: 1, completed: false, writesPermitted: false, provider: 'RadixOASIS',
    ownerId: owner, failedOperationId: operation, savedCopyIds: copies };
  const input = { ownerId: owner, source: { provider: 'RadixOASIS' } };
  function encode() {
    const raw = Object.fromEntries(Object.entries(evidence).map(([name, value]) => [name, Buffer.from(JSON.stringify(value))]));
    manifest.evidence = Object.fromEntries(Object.entries(raw).map(([name, bytes]) => [name, {
      bytes: bytes.length, sha256: crypto.createHash('sha256').update(bytes).digest('hex') }]));
    return raw;
  }
  return { input, manifest, evidence, acknowledgements, encode,
    check(pending = {}) { return validateContinuation(input, manifest, encode(), acknowledgements, pending); } };
}

test('qualified settled read preserves original stages and identities without changing input', () => {
  const f = fixture(), before = JSON.stringify(f.evidence);
  assert.deepEqual(f.check().ids, f.manifest.savedCopyIds);
  assert.equal(JSON.stringify(f.evidence), before);
});
test('changed evidence bytes fail before any HTTP or storage action', () => {
  const f = fixture(), raw = f.encode(); raw['initial-flow.progress.json'][0] ^= 1;
  assert.throws(() => validateContinuation(f.input, f.manifest, raw, f.acknowledgements, {}), /hash differs/);
});
test('another owner, provider or write-enabled manifest cannot resume', () => {
  for (const change of [f => f.input.ownerId = id(88), f => f.input.source.provider = 'SuiOASIS', f => f.manifest.writesPermitted = true]) {
    const f = fixture(); change(f); assert.throws(() => f.check(), /Continuation rejected/);
  }
});
test('pending save intents and incomplete/private acknowledgement changes are rejected', () => {
  assert.throws(() => fixture().check({ uncertain: 'save' }), /unreconciled/);
  for (const change of [a => a.pop(), a => a[0].isPublic = true, a => a[0].version = 2,
    a => a[0].ownerId = id(88), a => a[1].requestKey = a[0].requestKey]) {
    const f = fixture(); change(f.acknowledgements); assert.throws(() => f.check(), /Continuation rejected/);
  }
});
test('unsettled or unlinked operations cannot authorize continuation', () => {
  for (const change of [f => f.evidence['original-settlement-state.json'].state = 'pending',
    f => f.evidence['original-settlement-state.json'].operationId = id(88),
    f => f.evidence['failure-log-observation.json'] = []]) {
    const f = fixture(); change(f); assert.throws(() => f.check(), /Continuation rejected/);
  }
});
test('wrong completed counts, foreign copies and already completed reuse cannot resume', () => {
  for (const change of [f => f.evidence['initial-flow.progress.json'].progress.five.result.rows.pop(),
    f => f.evidence['initial-flow.progress.json'].progress.four.result.rows[2].selected.row.createdByAvatarId = id(88),
    f => f.evidence['initial-flow.progress.json'].progress.repeated = { existing: 2 }]) {
    const f = fixture(); change(f); assert.throws(() => f.check(), /Continuation rejected/);
  }
});
test('explicit continuation permits only query and pure identity routes', () => {
  for (const route of ['data/query-cql', 'data/query-cql/federation', 'data/query-cql/demo-identities'])
    assert.doesNotThrow(() => assertContinuationRoute({}, route));
  for (const route of ['data/save-holon', 'data/delete-holon', 'unknown'])
    assert.throws(() => assertContinuationRoute({}, route), /cannot send/);
  assert.doesNotThrow(() => assertContinuationRoute(null, 'data/save-holon'));
});
test('manifest outside the original cohort is rejected before file access', () => {
  assert.throws(() => loadContinuation({ state: '/original', resumeManifest: '/different/manifest.json' }), /original cohort/);
});
