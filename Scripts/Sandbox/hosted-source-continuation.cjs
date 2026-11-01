'use strict';
const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const names = ['initial-flow.progress.json', 'original-native-http.json',
  'original-settlement-state.json', 'failure-log-observation.json'];
const guid = value => typeof value === 'string' && /^[0-9a-f]{8}-(?:[0-9a-f]{4}-){3}[0-9a-f]{12}$/.test(value);
const same = (left, right) => JSON.stringify([...left].sort()) === JSON.stringify([...right].sort());
function requireCondition(value, message) { if (!value) throw Error('Continuation rejected: ' + message); }

// Recovery is an explicit evidence-backed state transition, never a retry of
// preparation or the failed usage operation. This function performs no I/O.
function validateContinuation(input, manifest, raw, acknowledgements, pending) {
  requireCondition(manifest.schema === 1 && manifest.completed === false && manifest.writesPermitted === false,
    'a pending read-only continuation is required');
  requireCondition(manifest.provider === input.source.provider && manifest.ownerId === input.ownerId &&
    guid(manifest.ownerId) && guid(manifest.failedOperationId), 'source/owner identity differs');
  requireCondition(Array.isArray(manifest.savedCopyIds) && manifest.savedCopyIds.length === 2 &&
    manifest.savedCopyIds.every(guid) && new Set(manifest.savedCopyIds).size === 2, 'two original copy identities required');
  const evidence = {};
  requireCondition(Object.keys(manifest.evidence ?? {}).length === names.length, 'exact evidence set required');
  for (const name of names) {
    const bytes = raw[name];
    const expected = manifest.evidence[name];
    requireCondition(Buffer.isBuffer(bytes) && bytes.length > 0 && bytes.length <= 131072 &&
      expected?.bytes === bytes.length && expected.sha256 === crypto.createHash('sha256').update(bytes).digest('hex'),
      'original evidence hash differs: ' + name);
    evidence[name] = JSON.parse(bytes.toString('utf8').replace(/^\uFEFF/, ''));
  }
  requireCondition(pending && typeof pending === 'object' && !Array.isArray(pending) &&
    Object.keys(pending).length === 0, 'unreconciled save intent exists');
  requireCondition(Array.isArray(acknowledgements) && acknowledgements.length === 2 &&
    acknowledgements.every(row => row.isSaved === true && row.ownerId === input.ownerId &&
      row.isPublic === false && row.version === 1 && guid(row.id) && guid(row.requestKey)) &&
    new Set(acknowledgements.map(row => row.requestKey)).size === 2 &&
    same(acknowledgements.map(row => row.id), manifest.savedCopyIds), 'original private save acknowledgements differ');
  const prior = evidence[names[0]];
  const progress = prior.progress;
  requireCondition(progress?.preparation?.saved === 2 && progress.preparation.existing === 0 &&
    Array.isArray(progress.preparation.keys) &&
    same(progress.preparation.keys, acknowledgements.map(row => row.requestKey)) &&
    !progress.repeated && !progress.replicas, 'original completed stages differ');
  requireCondition(progress.failedHttp?.route === 'data/query-cql' && progress.failedHttp.status === 503 &&
    progress.failedHttp.errorCode === 'SETTLEMENT_PENDING' && Array.isArray(prior.steps) &&
    prior.steps.at(-1)?.status === 503 && prior.steps.at(-1)?.errorCode === 'SETTLEMENT_PENDING' &&
    prior.steps.slice(0, -1).every(step => step.status === 200), 'failure is not the qualified settled read');
  const settled = evidence[names[2]];
  requireCondition(settled.operationId === manifest.failedOperationId && settled.state === 'settled' &&
    settled.observedOutcome === 'succeeded' && settled.endpoint === 'POST /api/data/query-cql' &&
    settled.receiptCount === 1 && typeof settled.settledAtUtc === 'string', 'original operation is not settled');
  const logs = evidence[names[3]];
  requireCondition(Array.isArray(logs) && logs.some(row => row.operations?.includes(manifest.failedOperationId)),
    'server failure identity is not linked to the settled operation');
  const native = evidence[names[1]];
  requireCondition(native.avatar?.id === input.ownerId && native.source?.provider === manifest.provider &&
    native.native?.length === 2, 'original native proof identity differs');
  for (const [index, count] of [[0, 3], [1, 2]]) {
    const selectedNative = native.native[index]?.result?.rows;
    const paired = progress[index === 0 ? 'five' : 'four']?.result?.rows;
    requireCondition(Array.isArray(selectedNative) && selectedNative.length === count &&
      Array.isArray(paired) && paired.length === count + 2, 'original selection counts differ');
    const copies = paired.filter(row => row.selected?.provider === 'MongoDBOASIS').map(row => row.selected.row);
    const publicRows = paired.filter(row => row.selected?.provider === manifest.provider).map(row => row.selected.row);
    requireCondition(copies.length === 2 && same(copies.map(row => row.id), manifest.savedCopyIds) &&
      copies.every(row => row.createdByAvatarId === input.ownerId && row.isPublic === false) &&
      publicRows.length === count && publicRows.every(row => row.isPublic === true) &&
      same(publicRows.map(row => row.id), selectedNative.map(row => row.id)), 'original copy provenance differs');
  }
  return { prior, ids: [...manifest.savedCopyIds], operationId: manifest.failedOperationId };
}

function assertContinuationRoute(continuation, route) {
  if (continuation && !['data/query-cql', 'data/query-cql/federation', 'data/query-cql/demo-identities'].includes(route))
    throw Error('Read-only continuation cannot send a mutation or unknown route.');
}

function loadContinuation(input) {
  const expected = path.resolve(input.state, 'settlement-continuation/continuation-source.json');
  requireCondition(path.resolve(input.resumeManifest) === expected, 'manifest must belong to the original cohort');
  const manifest = JSON.parse(fs.readFileSync(expected, 'utf8'));
  const raw = Object.fromEntries(names.map(name => [name, fs.readFileSync(path.join(path.dirname(expected), name))]));
  const acknowledgements = fs.readFileSync(path.join(input.state, 'save-acknowledgements.jsonl'), 'utf8')
    .trim().split(/\r?\n/).map(row => JSON.parse(row));
  const pending = JSON.parse(fs.readFileSync(path.join(input.state, 'preparation-intents.json'), 'utf8'));
  return validateContinuation(input, manifest, raw, acknowledgements, pending);
}
module.exports = { validateContinuation, loadContinuation, assertContinuationRoute };
