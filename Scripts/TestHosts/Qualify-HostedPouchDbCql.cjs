// Inner retained-source runner. The caller owns the shared provider build lease.
'use strict';
const fs = require('node:fs/promises');
const path = require('node:path');
const crypto = require('node:crypto');
const assert = require('node:assert/strict');
const { EventEmitter } = require('node:events');
const { execFile } = require('node:child_process');
const { promisify } = require('node:util');
const { captureBundle, run, waitForNative } = require('../../Docker/host-sandbox-pouchdb.cjs');
const execute = promisify(execFile);
const hash = bytes => crypto.createHash('sha256').update(bytes).digest('hex');
const repo = path.resolve(__dirname, '../..');
const runtimeRoot = path.join(repo, 'LocalState/COSMIC/RecoveryLaptopPouchDBCql');
const root = path.join(runtimeRoot, 'hosted-preparation');

async function writeNew(file, value) {
  const handle = await fs.open(file, 'wx', 0o600);
  try { await handle.writeFile(value); await handle.sync(); } finally { await handle.close(); }
}

async function main() {
  const args = process.argv.slice(2);
  assert.ok(args.length === 0 || (args.length === 1 && args[0] === '--paired'), 'Expected no arguments or --paired.');
  const paired = args.length === 1;
  const runtime = JSON.parse(await fs.readFile(path.join(runtimeRoot, 'runtime.json'), 'utf8'));
  assert.equal(process.version, runtime.node);
  assert.equal(path.resolve(process.execPath).toLowerCase(), path.resolve(runtime.nodePath).toLowerCase());
  const source = path.join(root, 'source');
  const sourceAck = path.join(source, 'bootstrap.acknowledged.json');
  const bundleBytes = await fs.readFile(path.join(root, 'bundle.json'));
  const bundle = captureBundle(bundleBytes.toString('utf8'));
  const original = JSON.parse(await fs.readFile(path.join(root, 'native-source-qualified.json'), 'utf8'));
  assert.equal(hash(bundleBytes), original.sourceBundleSha256.toLowerCase());
  assert.equal(hash(await fs.readFile(sourceAck)), original.originalSourceAckSha256.toLowerCase());
  const baseline = original.proofs.at(-1);
  assert.match(baseline.phase, /^retained-[a-f0-9]{32}$/);
  const proof = path.join(root, baseline.phase, 'ack.json');
  assert.equal(hash(await fs.readFile(proof)), baseline.ackSha256.toLowerCase());
  const core = (await execute('git', ['-C', path.join(repo, 'OASIS Architecture/NextGenSoftware.OASIS.API.Core'), 'rev-parse', 'HEAD'])).stdout.trim();
  assert.match(core, /^[a-f0-9]{40}$/);
  const phase = path.join(root, (paired ? 'hosted-paired-' : 'hosted-cql-') + crypto.randomUUID().replaceAll('-', ''));
  await fs.mkdir(phase);
  await writeNew(path.join(phase, 'intent.json'), JSON.stringify({ pid: process.pid, core, digest: bundle.digest,
    originalProofSha256: baseline.ackSha256.toLowerCase(), readOnly: true, paired, startedUtc: new Date().toISOString() }));
  const signals = new EventEmitter(), events = [];
  let ready;
  const online = new Promise(resolve => { ready = resolve; });
  const instance = crypto.randomBytes(16).toString('hex');
  const work = run({ bundle, directory: source, runtimeRoot, instance, signals, log: row => { events.push(row); if (row.ready) ready(row); } });
  let timer, failure;
  const reportPath = path.join(phase, 'native-cql.trx');
  try {
    const listener = await Promise.race([online, work.then(() => { throw Error('Native source ended before CQL readiness.'); }),
      new Promise((_, reject) => { timer = setTimeout(() => reject(Error('Native PouchDB source startup timed out.')), 15000); })]);
    clearTimeout(timer);
    assert.equal(listener.origin, 'http://127.0.0.1:13800');
    await waitForNative(new AbortController().signal, bundle.digest, instance);
    const descriptor = path.join(phase, 'source.json');
    await writeNew(descriptor, JSON.stringify({ Endpoint: listener.origin, Digest: bundle.digest, Actor: bundle.actor,
      LiveIds: bundle.records.map(row => row.id), MissingId: crypto.randomUUID(), OriginalProofPath: proof,
      OriginalProofSha256: baseline.ackSha256.toLowerCase(), SourceAckPath: sourceAck, SourceAckSha256: original.originalSourceAckSha256.toLowerCase() }));
    const project = 'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.PouchDBOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.PouchDBOASIS.IntegrationTests.csproj';
    let output;
    try {
      const test = paired ? 'NativeHostedPouchDBMongoFederationRunsSdkAndHttpWithProviderProvenance' : 'PouchHostedCqlNativeTests';
      output = await execute('dotnet', ['test', project, '--no-build', '--filter', 'FullyQualifiedName~' + test,
        '/m:2', '/nr:false', '/p:UseSharedCompilation=false', '--results-directory', phase, '--logger', 'trx;LogFileName=native-cql.trx'],
      { cwd: repo, env: { ...process.env, COSMIC_POUCHDB_HOSTED_CQL_SOURCE: descriptor }, maxBuffer: 1024 * 1024 });
    } catch (error) {
      await writeNew(path.join(phase, 'native-cql.log'), (error.stdout ?? '') + (error.stderr ?? ''));
      throw error;
    }
    await writeNew(path.join(phase, 'native-cql.log'), output.stdout + output.stderr);
    const report = await fs.readFile(reportPath, 'utf8');
    const counters = [...report.matchAll(/<Counters\b([^<>]*?)\/>/g)];
    assert.equal(counters.length, 1);
    const values = Object.fromEntries([...counters[0][1].matchAll(/\b(\w+)="([^"]*)"/g)].map(row => [row[1], row[2]]));
    assert.deepEqual([values.total, values.passed, values.failed, values.notExecuted], ['1', '1', '0', '0']);
    if (paired) {
      const cleanup = JSON.parse(await fs.readFile(path.join(phase, 'mongo-cleanup.ack.json'), 'utf8'));
      assert.equal(cleanup.database, 'cosmic_cql_mongo_pouchdb_hosted');
      assert.equal(cleanup.actor, bundle.actor);
      assert.equal(cleanup.dropped, true);
      assert.equal(new Set(cleanup.ids).size, 3);
      await assert.rejects(fs.stat(path.join(root, 'mongo.pending.json')), { code: 'ENOENT' });
    }
    assert.equal(hash(await fs.readFile(sourceAck)), original.originalSourceAckSha256.toLowerCase());
  } catch (error) { failure = error; }
  finally {
    clearTimeout(timer); signals.emit('SIGTERM');
    try { assert.equal(await work, 0); } catch (error) { failure = failure ? new AggregateError([failure, error]) : error; }
  }
  if (failure) {
    await writeNew(path.join(phase, 'failure.json'), JSON.stringify({ message: failure.message, stack: failure.stack, events, preserveSource: true }));
    throw failure;
  }
  assert.equal(events.length, 2); assert.equal(events[1].closedHandles, 2);
  await writeNew(path.join(phase, 'ack.json'), JSON.stringify({ pid: process.pid, core, paired, passed: 1, skipped: 0,
    trxSha256: hash(await fs.readFile(reportPath)), sourceWrites: 0, events, finishedUtc: new Date().toISOString() }));
  console.log(JSON.stringify({ passed: true, phase, sourceWrites: 0, nativeStoresClosed: 2 }));
}
main().catch(error => { console.error(error); process.exitCode = 1; });
