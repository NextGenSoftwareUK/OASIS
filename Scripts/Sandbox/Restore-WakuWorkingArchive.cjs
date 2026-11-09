'use strict';

const fs = require('node:fs');
const path = require('node:path');
const { isDeepStrictEqual } = require('node:util');
const { DatabaseSync } = require('node:sqlite');
const { captureBundle } = require('../../Docker/host-sandbox-waku.cjs');

// Run in maintenance with the native Waku writer stopped.
const [working, original, backup, ...expectedMissing] = process.argv.slice(2);
for (const file of [working, original]) {
  if (!file || !fs.lstatSync(file).isFile() || fs.realpathSync(file) !== file)
    throw Error('Supply exact absolute paths to the working and qualified archives.');
}
if (working === original || !backup || path.resolve(backup) !== backup || !expectedMissing.length)
  throw Error('Supply a distinct qualified archive, absolute backup path and reviewed missing hashes.');

const bundle = captureBundle(process.env.OASIS_SANDBOX_WAKU_BOOTSTRAP_JSON,
  process.env.CQL_WAKU_STORE_QUERY_SOURCE_JSON);
if (fs.statSync(original).size !== bundle.bytes.length || !fs.readFileSync(original).equals(bundle.bytes))
  throw Error('The original archive differs from the protected bundle.');

const columns = 'messageHash, pubsubTopic, contentTopic, payload, version, timestamp, meta';
const select = 'SELECT ' + columns + ' FROM Message WHERE messageHash = ?';
const source = new DatabaseSync(original, { readOnly: true });
let target;
try {
  const catalogue = source.prepare('SELECT ' + columns + ' FROM Message');
  catalogue.setReadBigInts(true);
  const rows = catalogue.all();
  const identities = rows.map(row => '0x' + Buffer.from(row.messageHash).toString('hex'));
  if (!isDeepStrictEqual(identities.toSorted(), bundle.hashes.toSorted()))
    throw Error('The qualified archive identities differ from the protected bundle.');

  target = new DatabaseSync(working);
  if (target.prepare('PRAGMA integrity_check(1)').get().integrity_check !== 'ok')
    throw Error('The working archive failed integrity inspection.');
  const schema = 'SELECT type, name, sql FROM sqlite_schema ORDER BY type, name';
  if (!isDeepStrictEqual(source.prepare(schema).all(), target.prepare(schema).all()))
    throw Error('The working archive schema differs from the qualified archive.');
  fs.closeSync(fs.openSync(backup, 'wx', 0o600));
  target.prepare('VACUUM INTO ?').run(backup);
  target.exec('BEGIN IMMEDIATE');
  const find = target.prepare(select);
  find.setReadBigInts(true);
  const missing = [];
  for (const [index, row] of rows.entries()) {
    const observed = find.get(row.messageHash);
    if (!observed) missing.push(index);
    else if (!isDeepStrictEqual(row, observed)) throw Error('An existing qualified row differs.');
  }
  if (!isDeepStrictEqual(missing.map(index => identities[index]).toSorted(), expectedMissing.toSorted()))
    throw Error('The working archive differs from the reviewed missing set.');
  const insert = target.prepare('INSERT INTO Message (' + columns + ') VALUES (?, ?, ?, ?, ?, ?, ?)');
  for (const index of missing) insert.run(...Object.values(rows[index]));
  for (const row of rows) {
    if (!isDeepStrictEqual(row, find.get(row.messageHash))) throw Error('Restored row verification failed.');
  }
  target.exec('COMMIT');
  console.log(JSON.stringify({ restored: missing.length, qualified: rows.length, backup }));
} finally {
  target?.close();
  source.close();
}
