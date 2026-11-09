'use strict';

const fs = require('node:fs');
const { DatabaseSync } = require('node:sqlite');
const { captureBundle } = require('../../Docker/host-sandbox-waku.cjs');

const file = process.argv[2];
if (!file || !fs.lstatSync(file).isFile() || fs.realpathSync(file) !== file)
  throw Error('Supply the exact absolute path to the working archive.');

const bundle = captureBundle(process.env.OASIS_SANDBOX_WAKU_BOOTSTRAP_JSON,
  process.env.CQL_WAKU_STORE_QUERY_SOURCE_JSON);
const db = new DatabaseSync(file, { readOnly: true });
try {
  db.exec('PRAGMA query_only = ON; BEGIN');
  const integrity = db.prepare('PRAGMA quick_check(1)').get();
  if (integrity.quick_check !== 'ok') throw Error('Waku archive integrity check failed.');
  const find = db.prepare('SELECT 1 FROM Message WHERE messageHash = ?');
  const missing = bundle.hashes.filter(hash => !find.get(Buffer.from(hash.slice(2), 'hex')));
  const { rows } = db.prepare('SELECT COUNT(*) AS rows FROM Message').get();
  console.log(JSON.stringify({ rows, qualified: bundle.hashes.length, missing }));
} finally {
  db.close();
}
