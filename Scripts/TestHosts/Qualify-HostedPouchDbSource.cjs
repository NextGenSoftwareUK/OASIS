'use strict';
const fs = require('node:fs/promises');
const path = require('node:path');
const crypto = require('node:crypto');
const assert = require('node:assert/strict');
const { captureBundle, prepare, verify, close, serve, nativeHash } = require('../../Docker/host-sandbox-pouchdb.cjs');
const hash = bytes => crypto.createHash('sha256').update(bytes).digest('hex');
const runtimeRoot = path.resolve(__dirname, '../../LocalState/COSMIC/RecoveryLaptopPouchDBCql');
const root = path.join(runtimeRoot, 'hosted-preparation');

function errorInfo(error) {
  return { message: error.message, stack: error.stack,
    ...(error.errors ? { errors: error.errors.map(errorInfo) } : {}),
    ...(error.cause ? { cause: errorInfo(error.cause) } : {}) };
}

async function writeNew(file, value) {
  const handle = await fs.open(file, 'wx', 0o600);
  try { await handle.writeFile(JSON.stringify(value)); await handle.sync(); } finally { await handle.close(); }
}

async function main() {
  const mode = process.argv[2];
  if (process.argv.length !== 3 || !['--initialize', '--retained'].includes(mode)) throw Error('Explicit initialize or retained source qualification required.');
  const runtime = JSON.parse(await fs.readFile(path.join(runtimeRoot, 'runtime.json'), 'utf8'));
  assert.equal(process.version, runtime.node);
  assert.equal(path.resolve(process.execPath).toLowerCase(), path.resolve(runtime.nodePath).toLowerCase());
  const bundlePath = path.join(root, 'bundle.json');
  if (mode === '--initialize') {
    await fs.mkdir(root, { mode: 0o700 });
    const actor = crypto.randomUUID();
    const records = ['First', 'Second', 'Third'].map(label => {
      const id = crypto.randomUUID();
      const bytes = Buffer.from(JSON.stringify({ Id: id, CreatedByAvatarId: actor, Name: label + ' native PouchDB teaching record',
        Description: 'Synthetic public source for WEB4 deployment qualification; hosting remains pending.', IsPublic: true,
        MetaData: { DemoSource: 'COSMIC CQL PouchDB', DatasetVisibility: 'SyntheticPublic' }, Children: [] }));
      return { id, sha256: hash(bytes), jsonBase64: bytes.toString('base64') };
    });
    await writeNew(bundlePath, { schema: 1, actor, database: 'oasis_holons', records });
  } else {
    await fs.access(path.join(root, 'source', 'bootstrap.acknowledged.json'));
  }
  const bundleBytes = await fs.readFile(bundlePath);
  const bundle = captureBundle(bundleBytes.toString('utf8'));
  const phase = path.join(root, (mode === '--initialize' ? 'initial-' : 'retained-') + crypto.randomUUID().replaceAll('-', ''));
  await fs.mkdir(phase, { mode: 0o700 });
  await writeNew(path.join(phase, 'intent.json'), { mode, pid: process.pid, bundleSha256: hash(bundleBytes), digest: bundle.digest, node: process.version, startedUtc: new Date().toISOString() });
  let prepared, server, observation, closedHandles = 0, failure;
  try {
    const unacknowledged = path.join(phase, 'guard-unacknowledged');
    await fs.mkdir(unacknowledged);
    await assert.rejects(prepare({ bundle, directory: unacknowledged, runtimeRoot }), /ENOENT/);
    assert.deepEqual(await fs.readdir(unacknowledged), []);
    const changed = path.join(phase, 'guard-changed-ack');
    await fs.mkdir(changed);
    const wrong = { schema: 1, digest: '0'.repeat(64), version: '9.0.0', records: [] };
    await writeNew(path.join(changed, 'bootstrap.acknowledged.json'), wrong);
    await assert.rejects(prepare({ bundle, directory: changed, runtimeRoot }), /acknowledgement differs/);
    assert.deepEqual(await fs.readdir(changed), ['bootstrap.acknowledged.json']);
    const missingStore = path.join(phase, 'guard-missing-store');
    await fs.mkdir(path.join(missingStore, 'data', 'oasis_holons'), { recursive: true });
    await writeNew(path.join(missingStore, 'bootstrap.acknowledged.json'), { ...wrong, digest: bundle.digest });
    await assert.rejects(prepare({ bundle, directory: missingStore, runtimeRoot }), /ENOENT/);
    assert.deepEqual(await fs.readdir(path.join(missingStore, 'data', 'oasis_holons')), []);
    prepared = await prepare({ bundle, directory: path.join(root, 'source'), runtimeRoot });
    const before = await verify(prepared, prepared.receipts);
    const instance = crypto.randomUUID().replaceAll('-', '');
    server = await serve(prepared, { port: 0, instance });
    await writeNew(path.join(phase, 'listener.json'), { pid: process.pid, origin: server.origin, instance, digest: bundle.digest });
    let reads = 0;
    async function read(route, options = {}) {
      const response = await fetch(server.origin + route, { redirect: 'error', signal: AbortSignal.timeout(15000), ...options });
      assert.equal(response.headers.get('X-OASIS-PouchDB-Source'), bundle.digest);
      assert.equal(response.headers.get('X-OASIS-PouchDB-Instance'), instance);
      assert.equal(response.headers.get('X-OASIS-PouchDB-Version'), '9.0.0');
      const bytes = Buffer.from(await response.arrayBuffer());
      assert.ok(bytes.length < 65536);
      await writeNew(path.join(phase, 'http-' + (++reads) + '.json'), { route, method: options.method ?? 'GET', status: response.status, bodyBase64: bytes.toString('base64'), sha256: hash(bytes) });
      return { status: response.status, row: JSON.parse(bytes.toString('utf8')), sha256: hash(bytes) };
    }
    const info = await read('/oasis_holons');
    assert.equal(info.status, 200); assert.equal(info.row.db_name, 'oasis_holons'); assert.equal(info.row.doc_count, 3);
    const rows = [];
    for (const record of bundle.records) {
      const result = await read('/oasis_holons/' + record.id + '?conflicts=true');
      assert.equal(result.status, 200);
      const receipt = prepared.receipts.find(row => row.id === record.id);
      assert.equal(result.row._rev, receipt.revision);
      assert.equal(nativeHash(result.row), receipt.nativeSha256);
      rows.push({ id: record.id, revision: result.row._rev, nativeSha256: nativeHash(result.row), httpSha256: result.sha256 });
    }
    const missing = await read('/oasis_holons/' + crypto.randomUUID());
    assert.equal(missing.status, 404); assert.equal(missing.row.error, 'not_found'); assert.equal(missing.row.reason, 'missing');
    assert.equal((await read('/oasis_holons/' + bundle.records[0].id, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: '{}' })).status, 405);
    assert.equal((await read('/oasis_holons/_bulk_docs', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: '{"docs":[]}' })).status, 405);
    assert.equal((await read('/_all_dbs')).status, 404);
    assert.equal((await read('/oasis_holons/_all_docs')).status, 404);
    assert.equal((await read('/oasis_holons/' + bundle.records[0].id + '?rev=unadmitted')).status, 400);
    assert.deepEqual(await verify(prepared, prepared.receipts), before);
    assert.equal(hash(await fs.readFile(bundlePath)), hash(bundleBytes));
    observation = { records: rows, nativeDocumentCount: 3, deniedWrites: 2, deniedUnboundedRoutes: 2, sourceGuards: 3, unchangedOriginals: true, origin: server.origin };
  } catch (error) { failure = error; }
  finally {
    try {
      if (server) closedHandles = await server.stop();
      else if (prepared) closedHandles = await close(prepared);
    } catch (error) { failure = failure ? new AggregateError([failure, error], 'Qualification and native close failed.') : error; }
  }
  if (failure) {
    await writeNew(path.join(phase, 'failure.json'), { ...errorInfo(failure), preserveSource: true, closedHandles, finishedUtc: new Date().toISOString() });
    throw failure;
  }
  await writeNew(path.join(phase, 'ack.json'), { ...observation, bundleSha256: hash(bundleBytes), digest: bundle.digest, closedHandles, finishedUtc: new Date().toISOString() });
  console.log(JSON.stringify({ passed: true, phase, mode, records: 3, closedHandles, sourceWrites: mode === '--initialize' ? 3 : 0 }));
}
main().catch(error => { console.error(error.stack); process.exitCode = 1; });
