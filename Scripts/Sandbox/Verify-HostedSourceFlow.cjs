/* Execute the published preparation module against real WEB4 HTTP APIs.
   This is an HTTP/client-module gate, not a browser rendering acceptance test. */
'use strict';
const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');

async function main() {
  const input = JSON.parse(fs.readFileSync(0, 'utf8')); // Bearer stays in process memory/stdin only.
  const preparation = require(input.preparationModule);
  const { unwrap } = require(path.join(path.dirname(input.preparationModule), 'cql-playground.js'));
  const journal = path.join(input.state, 'preparation-intents.json');
  if (fs.existsSync(journal + '.new')) throw Error('Interrupted storage replacement needs reconciliation.');
  const entries = fs.existsSync(journal) ? JSON.parse(fs.readFileSync(journal, 'utf8')) : {};
  function persist() {
    const handle = fs.openSync(journal + '.new', 'wx', 0o600);
    try { fs.writeFileSync(handle, JSON.stringify(entries)); fs.fsyncSync(handle); } finally { fs.closeSync(handle); }
    fs.renameSync(journal + '.new', journal);
  }
  const storage = {
    get length() { return Object.keys(entries).length; },
    key: index => Object.keys(entries)[index] ?? null,
    getItem: key => entries[key] ?? null,
    setItem(key, value) { entries[key] = value; persist(); },
    removeItem(key) { delete entries[key]; persist(); }
  };
  const steps = [];
  const mongo = new Map();
  const progress = {};
  function observe(phase, value) {
    progress[phase] = value;
    fs.writeFileSync(input.report + '.progress.json', JSON.stringify({ progress, steps, browserAcceptance: false }, null, 2));
  }
  async function request(route, body) {
    const response = await fetch(input.base + route, { method: 'POST', redirect: 'error',
      signal: AbortSignal.timeout(45000), headers: { Accept: 'application/json', 'Content-Type': 'application/json',
        Authorization: 'Bearer ' + input.token, 'Idempotency-Key': body?.operationId ?? crypto.randomUUID() },
      body: JSON.stringify(body) });
    const text = await response.text();
    const value = unwrap(text.length ? JSON.parse(text) : null);
    steps.push({ route, status: response.status, operationId: response.headers.get('X-OASIS-Operation-Id'),
      errorCode: value?.errorCode ?? value?.code ?? null });
    if (!response.ok || !value || value.isError) {
      observe('failedHttp', { route, status: response.status, errorCode: value?.errorCode ?? value?.code ?? null,
        retryAfter: response.headers.get('Retry-After') });
      throw Error(route + ' failed: HTTP ' + response.status + '/' + (value?.errorCode ?? value?.code ?? 'error'));
    }
    if (route === 'data/save-holon') {
      const acknowledgement = { requestKey: body.operationId, isSaved: value.isSaved === true,
        id: value.result?.id, ownerId: value.result?.createdByAvatarId, isPublic: value.result?.isPublic,
        version: value.result?.version, operationId: response.headers.get('X-OASIS-Operation-Id') };
      const handle = fs.openSync(path.join(input.state, 'save-acknowledgements.jsonl'), 'a', 0o600);
      try { fs.writeFileSync(handle, JSON.stringify(acknowledgement) + '\n'); fs.fsyncSync(handle); } finally { fs.closeSync(handle); }
    }
    if (route === 'data/query-cql' && body.provider === 'MongoDBOASIS')
      for (const row of value.result?.rows ?? []) mongo.set(row.id, row);
    return value;
  }
  function rows(value, expected) {
    if (!Array.isArray(value?.result?.rows) || value.result.rows.length !== expected)
      throw Error('Complete native/federated result count differs from ' + expected + '.');
    return value.result.rows;
  }
  const plan = preparation.recipe(input.source, input.pairedThree);
  if (!plan) throw Error('Configured source does not support the published private-snapshot recipe.');
  const options = { recipe: plan, request, storage, base: input.base.replace(/\/api\/$/, ''),
    isCurrent: () => true, randomUUID: () => crypto.randomUUID() };
  const prepared = await preparation.prepare(options);
  observe('preparation', prepared);
  const mappedIds = async () => {
    const mapped = await request('data/query-cql/demo-identities', { provider: input.source.provider, keys: plan.keys });
    return mapped.result.records.map(record => record.id);
  };
  let ids = plan.selectionKind === 'Id' ? await mappedIds() : null;
  const five = await request('data/query-cql/federation',
    plan.selectionKind === 'Id' ? prepared.pairedRequest : input.pairedThree.request);
  rows(five, 5);
  observe('five', five);
  const four = await request('data/query-cql/federation', plan.selectionKind === 'Id'
    ? preparation.composeIdPair(preparation.recipe(input.source, input.pairedTwo), ids) : input.pairedTwo.request);
  rows(four, 4);
  observe('four', four);
  const repeated = await preparation.prepare(options);
  if (repeated.saved !== 0 || repeated.existing !== 2) throw Error('Repeated setup must preserve both private snapshots.');
  observe('repeated', repeated);

  // Preserve the existing protocol gate's request order; ID recipes need the
  // mapped identities earlier to compose their actor-specific query predicate.
  if (ids === null) ids = await mappedIds();
  const replicas = {};
  const stable = value => JSON.stringify(value, (_, entry) => entry && typeof entry === 'object' && !Array.isArray(entry)
    ? Object.fromEntries(Object.entries(entry).sort(([left], [right]) => left.localeCompare(right))) : entry);
  for (const provider of ['SQLLiteDBOASIS', 'Neo4jOASIS']) {
    let response;
    // Bounded observation of asynchronous replication, not a repeated save.
    // At most three reads per target, one second apart, within this five-second gate.
    const deadline = Date.now() + 5000;
    for (let attempt = 0; attempt < 3; attempt++) {
      response = await request('data/query-cql', { provider, orderBy: 'Id', take: 2, includeMetadata: true,
        filters: [{ field: 'Id', scalar: 'Guid', operator: 'In', value: ids }] });
      if (response.result?.rows?.length === 2 || attempt === 2 || Date.now() >= deadline) break;
      await new Promise(resolve => setTimeout(resolve, 1000));
    }
    rows(response, 2);
    for (const row of response.result.rows) {
      const original = mongo.get(row.id);
      if (!ids.includes(row.id) || !original || row.createdByAvatarId !== input.ownerId || row.isPublic !== false ||
          typeof row.version !== 'number' || row.version < 1 ||
          ['name', 'description', 'version', 'createdByAvatarId', 'isPublic', 'metadata'].some(field => stable(row[field]) !== stable(original[field])))
        throw Error(provider + ' replica differs from the acknowledged private Mongo snapshot.');
    }
    replicas[provider] = response;
    observe('replicas', replicas);
  }
  const proof = { provider: input.source.provider, prepared, repeated, ids, five, four, replicas, mongoRows: [...mongo.values()], steps,
    browserAcceptance: false, clientModule: input.preparationModule };
  fs.writeFileSync(input.report, JSON.stringify(proof, null, 2), { flag: 'wx' });
  console.log(JSON.stringify({ provider: proof.provider, prepared, repeated, nativeAndMongo: [5, 4],
    replicaCounts: [2, 2], browserAcceptance: false }));
}
main().catch(error => { console.error(error.message); process.exitCode = 1; });
