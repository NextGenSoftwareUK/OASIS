/* Owned, write-once synthetic PouchDB source. Vendor PouchDB/express-pouchdb serve every data read. */
'use strict';
const fs = require('node:fs/promises');
const path = require('node:path');
const crypto = require('node:crypto');
const { createRequire } = require('node:module');
const { setTimeout: delay } = require('node:timers/promises');
const guid = /^[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}$/;
const empty = '00000000-0000-0000-0000-000000000000';
const hash = bytes => crypto.createHash('sha256').update(bytes).digest('hex');
const fields = (value, names) => value && typeof value === 'object' && !Array.isArray(value) &&
  Object.keys(value).sort().join(',') === [...names].sort().join(',');
const canonical = value => JSON.stringify(normalize(value));
function normalize(value) {
  if (Array.isArray(value)) return value.map(normalize);
  if (value && typeof value === 'object') return Object.fromEntries(Object.keys(value).sort().map(key => [key, normalize(value[key])]));
  return value;
}

function strictJson(text) {
  const value = JSON.parse(text);
  const stack = [];
  // JSON.parse owns syntax; this scan additionally rejects escaped/case-folded duplicate keys.
  for (const match of text.matchAll(/"(?:\\.|[^"\\])*"|[{}\[\],:]/g)) {
    const token = match[0], top = stack[stack.length - 1];
    if (token === '{') stack.push({ names: new Set(), key: true });
    else if (token === '[') stack.push({ names: null });
    else if (token === '}' || token === ']') stack.pop();
    else if (top?.names && token === ':') top.key = false;
    else if (top?.names && token === ',') top.key = true;
    else if (top?.names && top.key && token[0] === '"') {
      const name = JSON.parse(token).toLowerCase();
      if (top.names.has(name)) throw Error('Duplicate PouchDB source property.');
      top.names.add(name);
    }
  }
  return value;
}

function captureBundle(text) {
  if (typeof text !== 'string' || !text.length || Buffer.byteLength(text) > 131072) throw Error('Bounded PouchDB source bundle required.');
  const value = strictJson(text);
  if (!fields(value, ['schema', 'actor', 'database', 'records']) || value.schema !== 1 ||
      !guid.test(value.actor) || value.actor === empty || value.database !== 'oasis_holons' ||
      !Array.isArray(value.records) || value.records.length !== 3) throw Error('Three owned synthetic PouchDB records required.');
  const records = value.records.map(record => {
    if (!fields(record, ['id', 'sha256', 'jsonBase64']) || !guid.test(record.id) || record.id === empty ||
        typeof record.sha256 !== 'string' || !/^[a-f0-9]{64}$/.test(record.sha256) || typeof record.jsonBase64 !== 'string')
      throw Error('Invalid PouchDB source transfer.');
    const bytes = Buffer.from(record.jsonBase64, 'base64');
    if (!bytes.length || bytes.length > 32768 || bytes.toString('base64') !== record.jsonBase64 || hash(bytes) !== record.sha256)
      throw Error('PouchDB source transfer hash or encoding differs.');
    const row = strictJson(new TextDecoder('utf-8', { fatal: true }).decode(bytes));
    if (!fields(row, ['Id', 'CreatedByAvatarId', 'Name', 'Description', 'IsPublic', 'MetaData', 'Children']) ||
        row.Id !== record.id || row.CreatedByAvatarId !== value.actor || row.IsPublic !== true ||
        typeof row.Name !== 'string' || !row.Name.length || row.Name.length > 256 ||
        typeof row.Description !== 'string' || row.Description.length > 1024 ||
        !Array.isArray(row.Children) || row.Children.length !== 0 || !row.MetaData || typeof row.MetaData !== 'object' || Array.isArray(row.MetaData) ||
        Object.keys(row.MetaData).length > 16 || Object.entries(row.MetaData).some(([key, item]) => !key.length || key.length > 128 || typeof item !== 'string' || item.length > 1024))
      throw Error('PouchDB source must contain only the admitted public teaching projection.');
    return Object.freeze({ id: record.id, sha256: record.sha256, jsonBase64: record.jsonBase64 });
  });
  if (new Set(records.map(record => record.id)).size !== 3) throw Error('Duplicate PouchDB source identity.');
  const captured = { schema: 1, actor: value.actor, database: value.database, records: Object.freeze(records) };
  return Object.freeze({ ...captured, digest: hash(JSON.stringify(captured)) });
}

async function exists(file) {
  try { return await fs.lstat(file); }
  catch (error) { if (error.code === 'ENOENT') return null; throw error; }
}
async function writeNew(file, value) {
  const handle = await fs.open(file, 'wx', 0o600);
  try { await handle.writeFile(value); await handle.sync(); } finally { await handle.close(); }
}
async function regular(file) {
  const info = await fs.lstat(file);
  if (!info.isFile() || info.isSymbolicLink()) throw Error('Owned PouchDB source file differs.');
}
function expectedDocument(record) {
  return { ...strictJson(new TextDecoder('utf-8', { fatal: true }).decode(Buffer.from(record.jsonBase64, 'base64'))), _id: record.id };
}
function nativeHash(document) {
  const { _rev, ...content } = document;
  return hash(canonical(content));
}

async function verify(prepared, receipts) {
  if (!Array.isArray(receipts) || receipts.length !== 3 || new Set(receipts.map(row => row.id)).size !== 3)
    throw Error('PouchDB source receipts are incomplete.');
  const observed = [];
  for (const record of prepared.bundle.records) {
    const receipt = receipts.find(row => row.id === record.id);
    if (!fields(receipt, ['id', 'payloadSha256', 'nativeSha256', 'revision']) || receipt.payloadSha256 !== record.sha256 ||
        receipt.nativeSha256 !== hash(canonical(expectedDocument(record))) || !/^1-[a-f0-9]+$/.test(receipt.revision))
      throw Error('Original PouchDB write acknowledgement differs.');
    const row = await prepared.db.get(record.id, { conflicts: true });
    if (row._id !== record.id || row._rev !== receipt.revision || row._conflicts || nativeHash(row) !== receipt.nativeSha256)
      throw Error('Native PouchDB source differs from original acknowledgement.');
    observed.push({ id: record.id, revision: row._rev, nativeSha256: nativeHash(row) });
  }
  const all = await prepared.db.allDocs({ limit: 4 });
  if (all.total_rows !== 3 || all.rows.length !== 3 ||
      canonical(all.rows.map(row => row.id).sort()) !== canonical(prepared.bundle.records.map(row => row.id).sort()))
    throw Error('PouchDB teaching database contains a different record set.');
  return observed;
}

async function prepare({ bundle, directory, runtimeRoot = '/opt/pouchdb' }) {
  const captured = captureBundle(JSON.stringify({ schema: bundle.schema, actor: bundle.actor, database: bundle.database, records: bundle.records }));
  if (captured.digest !== bundle.digest) throw Error('Captured PouchDB bundle changed.');
  bundle = captured;
  const load = createRequire(path.join(path.resolve(runtimeRoot), 'package.json'));
  for (const [name, version] of [['pouchdb', '9.0.0'], ['express-pouchdb', '4.2.0'], ['express', '4.21.2'], ['pouchdb-all-dbs', '1.1.1']])
    if (load(name + '/package.json').version !== version) throw Error('Pinned PouchDB runtime differs.');
  directory = path.resolve(directory);
  const ackFile = path.join(directory, 'bootstrap.acknowledged.json');
  const info = await exists(directory);
  if (info && (!info.isDirectory() || info.isSymbolicLink())) throw Error('Owned PouchDB directory differs.');
  const retained = !!info;
  let ack;
  if (retained) {
    await regular(ackFile);
    ack = strictJson(await fs.readFile(ackFile, 'utf8'));
    if (!fields(ack, ['schema', 'digest', 'version', 'records']) || ack.schema !== 1 || ack.digest !== bundle.digest || ack.version !== '9.0.0')
      throw Error('PouchDB acknowledgement differs; explicit migration/reconciliation required.');
    const data = path.join(directory, 'data');
    const dataInfo = await fs.lstat(data);
    if (!dataInfo.isDirectory() || dataInfo.isSymbolicLink()) throw Error('PouchDB native directory differs.');
    const storeInfo = await fs.lstat(path.join(data, bundle.database));
    if (!storeInfo.isDirectory() || storeInfo.isSymbolicLink()) throw Error('PouchDB native store differs.');
    // An acknowledged missing native store must not be recreated by the SDK constructor.
    const current = path.join(data, bundle.database, 'CURRENT');
    await regular(current);
    const manifest = (await fs.readFile(current, 'utf8')).trim();
    if (!/^MANIFEST-[0-9]+$/.test(manifest)) throw Error('PouchDB native CURRENT file differs.');
    await regular(path.join(data, bundle.database, manifest));
    if (await fs.readFile(path.join(directory, 'source-config.json'), 'utf8') !== JSON.stringify(bundle))
      throw Error('Original PouchDB source configuration differs.');
  } else {
    await fs.mkdir(directory, { mode: 0o700 });
    await writeNew(path.join(directory, 'bootstrap.intent.json'), JSON.stringify({ schema: 1, digest: bundle.digest, version: '9.0.0' }));
    await writeNew(path.join(directory, 'source-config.json'), JSON.stringify(bundle));
    await fs.mkdir(path.join(directory, 'data'), { mode: 0o700 });
  }
  const handles = new Set();
  const NativePouch = load('pouchdb').defaults({ prefix: path.join(directory, 'data') + path.sep, adapter: 'leveldb' });
  // Include middleware metadata stores, not only HTTP document handles, in native shutdown ownership.
  class OwnedPouch extends NativePouch {
    constructor(...args) { super(...args); handles.add(this); this.once('closed', () => handles.delete(this)); }
  }
  // The vendor HTTP layer resolves databases through this SDK registry. Install
  // its supported plugin before opening the source, so created events are owned.
  load('pouchdb-all-dbs')(OwnedPouch);
  // Bootstrap and vendor routes must share the vendor's single cached SDK handle.
  // Closing independent wrappers over the same LevelDB store violates its close contract.
  const db = await load('express-pouchdb/lib/create-or-delete-dbs').getOrCreateDB(OwnedPouch, bundle.database);
  const prepared = { bundle, directory, PouchDB: OwnedPouch, db, handles, load, receipts: [] };
  try {
    if (!retained) {
      const initial = await db.info();
      if (initial.doc_count !== 0 || initial.update_seq !== 0) throw Error('New PouchDB teaching store was not empty.');
      for (const record of bundle.records) {
        const nativeSha256 = hash(canonical(expectedDocument(record)));
        await writeNew(path.join(directory, record.id + '.intent.json'), JSON.stringify({ id: record.id, payloadSha256: record.sha256, nativeSha256 }));
        const result = await db.put(expectedDocument(record));
        if (result.ok !== true || result.id !== record.id || !/^1-[a-f0-9]+$/.test(result.rev)) throw Error('Native PouchDB put acknowledgement differs.');
        const receipt = { id: record.id, payloadSha256: record.sha256, nativeSha256, revision: result.rev };
        await writeNew(path.join(directory, record.id + '.acknowledged.json'), JSON.stringify(receipt));
        prepared.receipts.push(receipt);
      }
      await verify(prepared, prepared.receipts);
      ack = { schema: 1, digest: bundle.digest, version: '9.0.0', records: prepared.receipts };
      await writeNew(ackFile, JSON.stringify(ack));
    } else {
      for (const record of bundle.records) {
        const receipt = strictJson(await fs.readFile(path.join(directory, record.id + '.acknowledged.json'), 'utf8'));
        prepared.receipts.push(receipt);
      }
      if (canonical(ack.records) !== canonical(prepared.receipts)) throw Error('PouchDB global and per-record acknowledgements differ.');
      await verify(prepared, prepared.receipts);
    }
    const registered = await OwnedPouch.allDbs();
    if (registered.length !== 1 || registered[0] !== bundle.database)
      throw Error('Vendor PouchDB database registry differs from the owned source.');
    return prepared;
  } catch (error) {
    try { await close(prepared); }
    catch (cleanup) { throw new AggregateError([error, cleanup], 'PouchDB source preparation and native close failed.'); }
    throw error;
  }
}

async function close(prepared) {
  const handles = [...prepared.handles];
  const outcomes = await Promise.allSettled(handles.map(db => db.close()));
  const failures = outcomes.filter(row => row.status === 'rejected').map(row => row.reason);
  if (failures.length) throw new AggregateError(failures, 'Native PouchDB handles did not close cleanly.');
  return handles.length;
}

function listenerConfiguration(port, instance) {
  if (!Number.isInteger(port) || port < 0 || port > 65535 || typeof instance !== 'string' || !/^[a-f0-9]{32}$/.test(instance))
    throw Error('Explicit owned PouchDB listener instance required.');
}

async function serve(prepared, { port = 13800, instance } = {}) {
  listenerConfiguration(port, instance);
  const app = prepared.load('express')();
  app.disable('x-powered-by');
  app.use((request, response, next) => {
    response.set('X-OASIS-PouchDB-Source', prepared.bundle.digest);
    response.set('X-OASIS-PouchDB-Instance', instance);
    response.set('X-OASIS-PouchDB-Version', '9.0.0');
    if (request.method !== 'GET') return response.status(405).json({ error: 'read_only', reason: 'Synthetic teaching source admits native reads only.' });
    const uri = new URL(request.originalUrl, 'http://127.0.0.1');
    const base = '/' + prepared.bundle.database;
    const document = uri.pathname.startsWith(base + '/') && guid.test(uri.pathname.slice(base.length + 1));
    if (uri.pathname !== base && !document) return response.status(404).json({ error: 'not_found', reason: 'unsupported read route' });
    if ([...uri.searchParams].length > (document ? 1 : 0) || [...uri.searchParams].some(([key, value]) => key !== 'conflicts' || value !== 'true'))
      return response.status(400).json({ error: 'bad_request', reason: 'Unsupported teaching read option.' });
    next();
  });
  const middleware = prepared.load('express-pouchdb')(null, { mode: 'minimumForPouchDB' });
  await middleware.setPouchDB(prepared.PouchDB);
  app.use(middleware);
  const server = await new Promise((resolve, reject) => {
    const listener = app.listen(port, '127.0.0.1', () => resolve(listener));
    listener.once('error', reject);
  });
  const terminal = new Promise(resolve => {
    server.once('error', error => resolve({ error }));
    server.once('close', () => resolve({ closed: true }));
  });
  return {
    origin: 'http://127.0.0.1:' + server.address().port,
    terminal,
    async stop() {
      const errors = []; let handles = 0;
      try {
        const closed = new Promise((resolve, reject) => server.close(error => error ? reject(error) : resolve()));
        server.closeIdleConnections();
        await closed;
      } catch (error) { errors.push(error); }
      try { await middleware.daemonManager.stop(prepared.PouchDB); } catch (error) { errors.push(error); }
      try { await verify(prepared, prepared.receipts); } catch (error) { errors.push(error); }
      try { handles = await close(prepared); } catch (error) { errors.push(error); }
      if (errors.length) throw new AggregateError(errors, 'Owned PouchDB shutdown did not complete cleanly.');
      return handles;
    }
  };
}

async function run({ bundle, directory, runtimeRoot, instance, port = 13800, signals = process, log = value => console.log(JSON.stringify(value)) }) {
  listenerConfiguration(port, instance);
  let stop, interrupted = false, prepared, server, failure, closedHandles = 0;
  const stopping = new Promise(resolve => { stop = resolve; });
  const signal = () => { interrupted = true; stop({ stopped: true }); };
  signals.on('SIGTERM', signal); signals.on('SIGINT', signal);
  try {
    // Await the owning initialization even when shutdown is requested mid-startup.
    prepared = await prepare({ bundle, directory, runtimeRoot });
    if (!interrupted) {
      server = await serve(prepared, { port, instance });
      log({ ready: true, pid: process.pid, origin: server.origin, digest: bundle.digest, instance, version: '9.0.0' });
      const outcome = await Promise.race([stopping, server.terminal]);
      if (outcome.error) throw outcome.error;
      if (outcome.closed && !interrupted) throw Error('Owned PouchDB listener stopped unexpectedly.');
    }
  } catch (error) { failure = error; }
  finally {
    signals.removeListener('SIGTERM', signal); signals.removeListener('SIGINT', signal);
    try {
      if (server) closedHandles = await server.stop();
      else if (prepared) closedHandles = await close(prepared);
    } catch (error) { failure = failure ? new AggregateError([failure, error], 'PouchDB runtime and shutdown failed.') : error; }
  }
  if (failure) throw failure;
  log({ stopped: true, pid: process.pid, digest: bundle.digest, closedHandles });
  return 0;
}

async function waitForNative(signal, digest, instance, probe = fetch) {
  const deadline = Date.now() + 15000;
  while (Date.now() < deadline) {
    signal.throwIfAborted();
    const request = new AbortController();
    const abort = () => request.abort(signal.reason);
    signal.addEventListener('abort', abort, { once: true });
    let timedOut = false;
    const timer = setTimeout(() => { timedOut = true; request.abort(Error('PouchDB readiness request timed out.')); }, 1000);
    let response;
    try {
      response = await probe('http://127.0.0.1:13800/oasis_holons', { method: 'GET', redirect: 'error', signal: request.signal });
      if (!response.ok || response.headers.get('X-OASIS-PouchDB-Source') !== digest ||
          response.headers.get('X-OASIS-PouchDB-Instance') !== instance || response.headers.get('X-OASIS-PouchDB-Version') !== '9.0.0')
        throw Error('Owned PouchDB readiness identity differs.');
      const reader = response.body.getReader(); let count = 0; const chunks = [];
      try {
        for (;;) {
          const item = await reader.read(); if (item.done) break;
          count += item.value.length;
          if (count > 16384) { await reader.cancel(); throw Error('PouchDB readiness response exceeds its budget.'); }
          chunks.push(Buffer.from(item.value));
        }
      } finally { reader.releaseLock(); }
      const info = strictJson(Buffer.concat(chunks).toString('utf8'));
      if (info.db_name !== 'oasis_holons' || info.doc_count !== 3 || info.adapter !== 'leveldb')
        throw Error('Owned PouchDB native database identity differs.');
      return;
    } catch (error) {
      signal.throwIfAborted();
      // Only an unconnected/timed-out startup health request may be polled again.
      if (response || (!timedOut && !['ECONNREFUSED', 'ECONNRESET', 'UND_ERR_CONNECT_TIMEOUT', 'UND_ERR_SOCKET'].includes(error.cause?.code))) throw error;
    } finally {
      clearTimeout(timer); signal.removeEventListener('abort', abort); request.abort();
    }
    await delay(100, undefined, { signal });
  }
  throw Error('Owned PouchDB readiness deadline expired.');
}

function serviceConfiguration(environment) {
  if (environment.OASIS_SANDBOX_POUCHDB !== 'true') throw Error('Explicit PouchDB sandbox runtime flag required.');
  const bundle = captureBundle(environment.OASIS_SANDBOX_POUCHDB_BOOTSTRAP_JSON);
  const instance = crypto.randomBytes(16).toString('hex');
  return { command: process.execPath, args: [path.join(__dirname, 'host-sandbox-pouchdb.cjs')],
    env: { ...environment, OASIS_SANDBOX_POUCHDB_INSTANCE: instance },
    check: signal => waitForNative(signal, bundle.digest, instance) };
}

module.exports = { captureBundle, prepare, verify, close, serve, canonical, nativeHash, run, waitForNative, serviceConfiguration };
if (require.main === module) {
  Promise.resolve().then(async () => {
    require('./host-sandbox-ipfs.cjs').assertSandbox(process.env.OASIS_DNA_JSON);
    if (process.env.OASIS_SANDBOX_POUCHDB !== 'true') throw Error('Explicit PouchDB sandbox runtime flag required.');
    const bundle = captureBundle(process.env.OASIS_SANDBOX_POUCHDB_BOOTSTRAP_JSON);
    listenerConfiguration(13800, process.env.OASIS_SANDBOX_POUCHDB_INSTANCE);
    const data = await fs.lstat('/data');
    if (!data.isDirectory() || data.isSymbolicLink()) throw Error('Explicit owned sandbox data root required.');
    const parent = await exists('/data/COSMIC');
    if (parent && (!parent.isDirectory() || parent.isSymbolicLink())) throw Error('Owned sandbox source parent differs.');
    if (!parent) await fs.mkdir('/data/COSMIC', { mode: 0o700 });
    return run({ bundle,
      directory: '/data/COSMIC/SandboxPouchDB', runtimeRoot: '/opt/pouchdb', instance: process.env.OASIS_SANDBOX_POUCHDB_INSTANCE });
  }).then(code => { process.exitCode = code; }, error => { console.error(error); process.exitCode = 1; });
}
