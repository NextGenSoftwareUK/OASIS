'use strict';
const test = require('node:test');
const assert = require('node:assert/strict');
const crypto = require('node:crypto');
const fs = require('node:fs/promises');
const os = require('node:os');
const path = require('node:path');
const { gzipSync } = require('node:zlib');
const { EventEmitter } = require('node:events');
const { captureBundle, prepare, prepareEnvironment, connectionString } = require('../../Docker/host-sandbox-duckdb.cjs');
const { supervise } = require('../../Docker/host-sandbox-ipfs.cjs');

const bytes = Buffer.from('controlled immutable source bytes; native SDK qualification is separate');
const hash = value => crypto.createHash('sha256').update(value).digest('hex');
const sample = () => ({ schema: 1, archiveGzipBase64: gzipSync(bytes).toString('base64'), bytes: bytes.length, sha256: hash(bytes) });
const capture = value => captureBundle(JSON.stringify(value ?? sample()));
async function temporary(run) {
  const root = await fs.mkdtemp(path.join(os.tmpdir(), 'oasis-duckdb-control-'));
  try { await run(root); }
  finally {
    const resolved = await fs.realpath(root);
    if (path.dirname(resolved) !== await fs.realpath(os.tmpdir()) ||
        !path.basename(resolved).startsWith('oasis-duckdb-control-'))
      throw Error('DuckDB control cleanup target differs.');
    await fs.rm(resolved, { recursive: true });
  }
}

test('qualified compressed bytes round-trip with a complete source digest', () => {
  assert.deepEqual(capture().bytes, bytes);
  assert.equal(capture().sha256, hash(bytes));
});
for (const [name, mutate] of [
  ['unknown fields', value => { value.endpoint = 'https://foreign.example'; }],
  ['schema drift', value => { value.schema = 2; }],
  ['digest drift', value => { value.sha256 = '0'.repeat(64); }],
  ['short output budget', value => { value.bytes--; }],
  ['long output length', value => { value.bytes++; }],
  ['unbounded output', value => { value.bytes = 8388609; }],
  ['noncanonical base64', value => { value.archiveGzipBase64 += '\n'; }],
  ['invalid gzip', value => { value.archiveGzipBase64 = bytes.toString('base64'); }],
]) test(`bundle refuses ${name}`, () => { const value = sample(); mutate(value); assert.throws(() => capture(value)); });

test('bundle refuses duplicate keys, extra whitespace and total byte overflow', () => {
  const text = JSON.stringify(sample());
  assert.throws(() => captureBundle(text.replace('"schema":1', '"schema":1,"schema":1')));
  assert.throws(() => captureBundle(text + ' '));
  assert.throws(() => captureBundle(' '.repeat(32769)));
});

test('initial provisioning and acknowledged restart preserve the same source and receipts', async () => temporary(async root => {
  const directory = path.join(root, 'owned');
  const bundle = capture();
  await prepare(bundle, directory);
  const source = await fs.readFile(path.join(directory, 'source.duckdb'));
  const acknowledgement = await fs.readFile(path.join(directory, 'bootstrap.acknowledged.json'));
  await prepare(bundle, directory);
  assert.deepEqual(await fs.readFile(path.join(directory, 'source.duckdb')), source);
  assert.deepEqual(await fs.readFile(path.join(directory, 'bootstrap.acknowledged.json')), acknowledgement);
  assert.deepEqual(source, bytes);
}));

test('an unacknowledged source is preserved and never adopted or overwritten', async () => temporary(async root => {
  const directory = path.join(root, 'owned'); await fs.mkdir(directory);
  await fs.writeFile(path.join(directory, 'source.duckdb'), bytes);
  await assert.rejects(prepare(capture(), directory), /reconciliation/);
  assert.deepEqual(await fs.readFile(path.join(directory, 'source.duckdb')), bytes);
  assert.deepEqual(await fs.readdir(directory), ['source.duckdb']);
}));

test('changed native bytes are refused without repairing the source', async () => temporary(async root => {
  const directory = path.join(root, 'owned'); const bundle = capture(); await prepare(bundle, directory);
  const changed = Buffer.from(bytes); changed[0] ^= 1;
  await fs.writeFile(path.join(directory, 'source.duckdb'), changed);
  await assert.rejects(prepare(bundle, directory), /reconciliation/);
  assert.deepEqual(await fs.readFile(path.join(directory, 'source.duckdb')), changed);
}));

test('a WAL requires explicit reconciliation and is never removed', async () => temporary(async root => {
  const directory = path.join(root, 'owned'); const bundle = capture(); await prepare(bundle, directory);
  const wal = path.join(directory, 'source.duckdb.wal'); await fs.writeFile(wal, 'unresolved native state');
  await assert.rejects(prepare(bundle, directory), /reconciliation/);
  assert.equal(await fs.readFile(wal, 'utf8'), 'unresolved native state');
}));

test('foreign or writable SDK connections fail before creating any source directory', async () => temporary(async root => {
  const directory = path.join(root, 'owned');
  for (const connection of ['Data Source=foreign;access_mode=READ_ONLY', connectionString(directory).replace('READ_ONLY', 'READ_WRITE')])
    await assert.rejects(prepareEnvironment({ CQL_DUCKDB_QUERY_CONNECTION_STRING: connection,
      OASIS_SANDBOX_DUCKDB_BOOTSTRAP_JSON: JSON.stringify(sample()) }, directory), /exact owned path/);
  assert.deepEqual(await fs.readdir(root), []);
  await prepareEnvironment({ CQL_DUCKDB_QUERY_CONNECTION_STRING: connectionString(directory),
    OASIS_SANDBOX_DUCKDB_BOOTSTRAP_JSON: JSON.stringify(sample()) }, directory);
  assert.deepEqual(await fs.readFile(path.join(directory, 'source.duckdb')), bytes);
}));

test('an in-process DuckDB source supervises the API without creating a native daemon', async () => {
  const commands = [];
  const signals = new EventEmitter();
  const result = await supervise({ services: [], signals, launch(command, args) {
    commands.push({ command, args });
    const child = new EventEmitter(); child.kill = () => { throw Error('Completed API should not be killed'); };
    queueMicrotask(() => child.emit('exit', 0));
    return child;
  } });
  assert.equal(result, 0);
  assert.deepEqual(commands, [{ command: 'dotnet', args: ['NextGenSoftware.OASIS.API.ONODE.WebAPI.dll'] }]);
  assert.equal(signals.listenerCount('SIGTERM'), 0);
  assert.equal(signals.listenerCount('SIGINT'), 0);
});
