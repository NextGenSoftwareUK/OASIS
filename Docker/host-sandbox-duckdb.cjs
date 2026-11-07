'use strict';
const fs = require('node:fs/promises');
const path = require('node:path');
const crypto = require('node:crypto');
const { gunzipSync } = require('node:zlib');

const hash = value => crypto.createHash('sha256').update(value).digest('hex');
const connectionString = directory => `Data Source=${path.join(directory, 'source.duckdb')};access_mode=READ_ONLY`;

function captureBundle(text) {
  if (typeof text !== 'string' || Buffer.byteLength(text) > 32768)
    throw Error('A bounded qualified DuckDB source bundle is required.');
  const value = JSON.parse(text);
  if (!value || JSON.stringify(value) !== text ||
      Object.keys(value).sort().join(',') !== 'archiveGzipBase64,bytes,schema,sha256' ||
      value.schema !== 1 || !Number.isInteger(value.bytes) || value.bytes < 16 || value.bytes > 8388608 ||
      typeof value.sha256 !== 'string' || !/^[a-f0-9]{64}$/.test(value.sha256) ||
      typeof value.archiveGzipBase64 !== 'string')
    throw Error('The DuckDB source bundle must use its complete canonical schema.');
  const compressed = Buffer.from(value.archiveGzipBase64, 'base64');
  if (!compressed.length || compressed.toString('base64') !== value.archiveGzipBase64)
    throw Error('The DuckDB compressed source encoding differs.');
  const bytes = gunzipSync(compressed, { maxOutputLength: value.bytes });
  if (bytes.length !== value.bytes || hash(bytes) !== value.sha256)
    throw Error('The DuckDB source differs from its qualification.');
  return { bytes, sha256: value.sha256, digest: hash(text) };
}

async function inspect(file) {
  try { return await fs.lstat(file); }
  catch (error) { if (error.code === 'ENOENT') return null; throw error; }
}

async function writeNew(file, bytes) {
  const handle = await fs.open(file, 'wx', 0o600);
  try { await handle.writeFile(bytes); await handle.sync(); }
  finally { await handle.close(); }
}

async function prepare(bundle, directory) {
  if (path.resolve(directory) !== directory) throw Error('An absolute DuckDB source directory is required.');
  const parent = path.dirname(directory);
  if (!(await inspect(parent))) {
    const ancestor = path.dirname(parent);
    if (await fs.realpath(ancestor) !== ancestor || !(await fs.lstat(ancestor)).isDirectory())
      throw Error('The DuckDB mount parent must be an existing real owned directory.');
    await fs.mkdir(parent, { mode: 0o700 });
  }
  if (await fs.realpath(parent) !== parent || !(await fs.lstat(parent)).isDirectory())
    throw Error('The DuckDB source parent must be an existing real owned directory.');
  const source = path.join(directory, 'source.duckdb');
  const acknowledgement = JSON.stringify({ schema: 1, digest: bundle.digest, sha256: bundle.sha256, bytes: bundle.bytes.length });
  const acknowledgementPath = path.join(directory, 'bootstrap.acknowledged.json');
  if (await inspect(directory)) {
    if (await fs.realpath(directory) !== directory || !(await fs.lstat(directory)).isDirectory())
      throw Error('The existing DuckDB source directory differs.');
    const ackStat = await inspect(acknowledgementPath);
    const sourceStat = await inspect(source);
    if (!ackStat?.isFile() || ackStat.isSymbolicLink() || ackStat.size > 4096 ||
        !sourceStat?.isFile() || sourceStat.isSymbolicLink() || sourceStat.size !== bundle.bytes.length ||
        await inspect(source + '.wal') ||
        await fs.readFile(acknowledgementPath, 'utf8') !== acknowledgement ||
        hash(await fs.readFile(source)) !== bundle.sha256)
      throw Error('Existing DuckDB source requires reconciliation before startup.');
    return;
  }
  await fs.mkdir(directory, { mode: 0o700 });
  await writeNew(path.join(directory, 'bootstrap.intent.json'), acknowledgement);
  await writeNew(source, bundle.bytes);
  await writeNew(acknowledgementPath, acknowledgement);
}

async function prepareEnvironment(environment, directory = '/data/COSMIC/SandboxDuckDB') {
  if (environment.CQL_DUCKDB_QUERY_CONNECTION_STRING !== connectionString(directory))
    throw Error('The DuckDB source requires its exact owned path and read-only SDK connection.');
  await prepare(captureBundle(environment.OASIS_SANDBOX_DUCKDB_BOOTSTRAP_JSON), directory);
}

module.exports = { captureBundle, prepare, prepareEnvironment, connectionString };
