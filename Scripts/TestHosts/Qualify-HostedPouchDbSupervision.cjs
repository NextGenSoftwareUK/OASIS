'use strict';
const fs = require('node:fs/promises');
const path = require('node:path');
const crypto = require('node:crypto');
const assert = require('node:assert/strict');
const { EventEmitter } = require('node:events');
const { captureBundle, run, prepare, serve, close, waitForNative } = require('../../Docker/host-sandbox-pouchdb.cjs');
const hash = bytes => crypto.createHash('sha256').update(bytes).digest('hex');
const runtimeRoot = path.resolve(__dirname, '../../LocalState/COSMIC/RecoveryLaptopPouchDBCql');
const root = path.join(runtimeRoot, 'hosted-preparation');

async function writeNew(file, value) {
  const handle = await fs.open(file, 'wx', 0o600);
  try { await handle.writeFile(JSON.stringify(value)); await handle.sync(); } finally { await handle.close(); }
}

async function main() {
  const mode = process.argv[2];
  if (process.argv.length !== 3 || !['--graceful', '--validation-failure'].includes(mode)) throw Error('Explicit retained supervision mode required.');
  const runtime = JSON.parse(await fs.readFile(path.join(runtimeRoot, 'runtime.json'), 'utf8'));
  assert.equal(process.version, runtime.node);
  const source = path.join(root, 'source');
  const ackPath = path.join(source, 'bootstrap.acknowledged.json');
  const original = await fs.readFile(ackPath);
  const bundle = captureBundle(await fs.readFile(path.join(root, 'bundle.json'), 'utf8'));
  const phase = path.join(root, 'supervision-' + crypto.randomUUID().replaceAll('-', ''));
  await fs.mkdir(phase);
  await writeNew(path.join(phase, 'intent.json'), { mode, pid: process.pid, digest: bundle.digest, originalAckSha256: hash(original), startedUtc: new Date().toISOString() });
  const instance = crypto.randomBytes(16).toString('hex');
  let receipt;
  try {
    if (mode === '--graceful') {
      const signals = new EventEmitter(), events = [];
      let ready;
      const online = new Promise(resolve => { ready = resolve; });
      const work = run({ bundle, directory: source, runtimeRoot, instance, signals, log: row => { events.push(row); if (row.ready) ready(row); } });
      let timer, failure;
      try {
        const listener = await Promise.race([online, work.then(() => { throw Error('Runtime ended before ready.'); }),
          new Promise((_, reject) => { timer = setTimeout(() => reject(Error('Retained runtime readiness timed out.')), 15000); })]);
        clearTimeout(timer);
        assert.equal(listener.pid, process.pid); assert.equal(listener.instance, instance);
        assert.equal(listener.origin, 'http://127.0.0.1:13800');
        await waitForNative(new AbortController().signal, bundle.digest, instance);
        await assert.rejects(waitForNative(new AbortController().signal, bundle.digest, '0'.repeat(32)), /identity differs/);
      } catch (error) { failure = error; }
      finally {
        clearTimeout(timer); signals.emit('SIGTERM');
        try { assert.equal(await work, 0); } catch (error) { failure = failure ? new AggregateError([failure, error]) : error; }
      }
      if (failure) throw failure;
      assert.equal(events.length, 2); assert.equal(events[1].stopped, true); assert.equal(events[1].closedHandles, 2);
      assert.equal(signals.listenerCount('SIGTERM'), 0); assert.equal(signals.listenerCount('SIGINT'), 0);
      receipt = { simulatedSignal: 'SIGTERM', nativeStoresClosed: 2, foreignInstanceRejected: true, events };
    } else {
      const prepared = await prepare({ bundle, directory: source, runtimeRoot });
      let server;
      try {
        server = await serve(prepared, { port: 0, instance });
        prepared.receipts[0].nativeSha256 = '0'.repeat(64); // In-memory validation failure; disk/source receipts remain untouched.
        await assert.rejects(server.stop(), error => error instanceof AggregateError && error.errors.length === 1 &&
          /Original PouchDB write acknowledgement differs/.test(error.errors[0].message));
        assert.equal(prepared.handles.size, 0);
      } finally {
        if (!server) await close(prepared);
      }
      receipt = { expectedValidationFailure: true, nativeHandlesRemaining: 0, diskReceiptsUntouched: true };
    }
    assert.equal(hash(await fs.readFile(ackPath)), hash(original));
    await writeNew(path.join(phase, 'ack.json'), { ...receipt, pid: process.pid, mode, digest: bundle.digest, originalAckSha256: hash(original), sourceWrites: 0, finishedUtc: new Date().toISOString() });
    console.log(JSON.stringify({ passed: true, phase, mode, sourceWrites: 0 }));
  } catch (error) {
    await writeNew(path.join(phase, 'failure.json'), { message: error.message, stack: error.stack, preserveSource: true });
    throw error;
  }
}
main().catch(error => { console.error(error); process.exitCode = 1; });
