// One owned native LevelDB qualification store, retained for reuse.
const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const { createRequire } = require('node:module');
const root = path.resolve(__dirname, '../..');
const state = path.join(root, 'LocalState/COSMIC/RecoveryLaptopPouchDBCql');
const load = createRequire(path.join(state, 'package.json'));
function durable(name, value) {
  const fd = fs.openSync(path.join(state, name), 'wx');
  try { fs.writeFileSync(fd, JSON.stringify(value, null, 2)); fs.fsyncSync(fd); }
  finally { fs.closeSync(fd); }
}
async function main() {
  const PouchDB = load('pouchdb');
  if (PouchDB.version !== '9.0.0' || load('express-pouchdb/package.json').version !== '4.2.0' ||
      load('express/package.json').version !== '4.21.2') throw new Error('Pinned native package identity differs');
  const store = path.join(state, 'runtime-probe');
  if (fs.existsSync(store) || fs.existsSync(path.join(state, 'runtime-probe.intent.json')))
    throw new Error('Existing qualification outcome; inspect and reuse, never replay');
  durable('runtime-probe.intent.json', { store, version: PouchDB.version, adapter: 'leveldb' });
  let db = new PouchDB(store, { adapter: 'leveldb' });
  const first = await db.info();
  const identity = await db.id();
  if (first.adapter !== 'leveldb' || first.doc_count !== 0 || first.update_seq !== 0) throw new Error('Native empty LevelDB qualification differs');
  await db.close();
  db = new PouchDB(store, { adapter: 'leveldb' });
  const reopened = await db.info();
  if (await db.id() !== identity || reopened.adapter !== 'leveldb' || reopened.doc_count !== 0 || reopened.update_seq !== 0)
    throw new Error('Native LevelDB identity changed through reopen');
  await db.close();
  const files = [];
  function scan(directory) {
    for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
      const target = path.join(directory, entry.name);
      if (entry.isSymbolicLink()) throw new Error('Unexpected linked runtime file');
      if (entry.isDirectory()) scan(target);
      else files.push({ path: path.relative(state, target).replaceAll('\\', '/'), bytes: fs.statSync(target).size,
        sha256: crypto.createHash('sha256').update(fs.readFileSync(target)).digest('hex') });
    }
  }
  scan(path.join(state, 'node_modules'));
  files.sort((a, b) => a.path.localeCompare(b.path, 'en'));
  durable('runtime-files.json', files);
  const receipt = { node: process.version, nodePath: process.execPath, pouchdb: PouchDB.version,
    expressPouchdb: '4.2.0', adapter: 'leveldb', nativeStoreReopen: true, documentCount: 0,
    runtimeFiles: files.length, runtimeBytes: files.reduce((n, item) => n + item.bytes, 0),
    manifestSha256: crypto.createHash('sha256').update(fs.readFileSync(path.join(state, 'runtime-files.json'))).digest('hex'),
    lockSha256: crypto.createHash('sha256').update(fs.readFileSync(path.join(state, 'package-lock.json'))).digest('hex') };
  durable('runtime.json', receipt);
  console.log(JSON.stringify(receipt));
}
main().catch(error => { console.error(error); process.exitCode = 1; });
