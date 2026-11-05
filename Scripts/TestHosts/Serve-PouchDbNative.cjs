// Actual vendor HTTP routes over the pinned persistent PouchDB LevelDB adapter.
const fs = require('node:fs');
const path = require('node:path');
const { createRequire } = require('node:module');
const state = path.resolve(__dirname, '../../LocalState/COSMIC/RecoveryLaptopPouchDBCql');
const load = createRequire(path.join(state, 'package.json'));
async function main() {
  const PouchDB = load('pouchdb').defaults({ prefix: path.join(state, 'data') + path.sep, adapter: 'leveldb' });
  const middleware = load('express-pouchdb')(null, { mode: 'minimumForPouchDB' });
  const databases = new Set();
  middleware.dbWrapper.registerWrapper((name, db, next) => { databases.add(db); return next(); });
  await middleware.setPouchDB(PouchDB);
  const app = load('express')();
  app.use(middleware);
  const server = app.listen(19600, '127.0.0.1', () => console.log(JSON.stringify({ ready: true, pouchdb: PouchDB.version, adapter: 'leveldb', port: 19600 })));
  server.on('error', error => { console.error(error); process.exitCode = 1; });
  let stopping = false;
  process.on('SIGINT', async () => {
    if (stopping) return;
    stopping = true;
    try {
      await new Promise((resolve, reject) => server.close(error => error ? reject(error) : resolve()));
      for (const db of databases) await db.close();
      console.log(JSON.stringify({ stopped: true, nativeDatabasesClosed: databases.size }));
      process.exit(0);
    } catch (error) { console.error(error); process.exitCode = 1; }
  });
}
main().catch(error => { console.error(error); process.exitCode = 1; });
