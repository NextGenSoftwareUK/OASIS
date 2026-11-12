'use strict';
const fs = require('node:fs/promises');
const path = require('node:path');
const crypto = require('node:crypto');
const assert = require('node:assert/strict');
const { spawn } = require('node:child_process');
const { captureBundle, waitForNative, nativeHash, canonical } = require('./host-sandbox-pouchdb.cjs');
const hash = value => crypto.createHash('sha256').update(value).digest('hex');

async function main() {
  if (process.platform !== 'linux' || process.argv.length !== 3) throw Error('Explicit Linux image SDK qualification required.');
  const runtimeRoot = path.resolve(process.argv[2]);
  assert.equal(runtimeRoot, '/opt/pouchdb');
  // Build-stage ownership only: never adopt an existing /data or mounted source.
  const directory = '/data';
  await assert.rejects(fs.lstat(directory), { code: 'ENOENT' });
  await fs.mkdir(directory, { mode: 0o700 });
  const owner = crypto.randomBytes(16).toString('hex');
  await fs.writeFile(path.join(directory, '.pouch-image-owner'), owner, { flag: 'wx' });
  const actor = crypto.randomUUID();
  const packet = { schema: 1, actor, database: 'oasis_holons', records: [1, 2, 3].map(number => {
    const id = crypto.randomUUID();
    const bytes = Buffer.from(JSON.stringify({ Id: id, CreatedByAvatarId: actor, Name: 'Native image teaching record ' + number,
      Description: 'Owned disposable image qualification', IsPublic: true, MetaData: { DemoSource: 'PouchDB image qualification' }, Children: [] }));
    return { id, sha256: hash(bytes), jsonBase64: bytes.toString('base64') };
  }) };
  const bundle = captureBundle(JSON.stringify(packet));
  const source = path.join(directory, 'COSMIC/SandboxPouchDB');
  const modulePath = path.join(__dirname, 'host-sandbox-pouchdb.cjs');
  const observed = [];
  let originalAck;
  for (let pass = 0; pass < 2; pass++) {
    const instance = crypto.randomBytes(16).toString('hex');
    const child = spawn(process.execPath, [modulePath], { stdio: ['ignore', 'pipe', 'pipe'], env: { ...process.env,
      OASIS_DNA_JSON: '{"OASIS":{"Sandbox":{"Enabled":true}}}', OASIS_SANDBOX_POUCHDB: 'true',
      OASIS_SANDBOX_POUCHDB_BOOTSTRAP_JSON: JSON.stringify(packet), OASIS_SANDBOX_POUCHDB_INSTANCE: instance } });
    let stdout = '', stderr = '', stopped, rejectReady, resolveReady, protocolError, stdoutBytes = 0;
    const ready = new Promise((resolve, reject) => { resolveReady = resolve; rejectReady = reject; });
    const exited = new Promise((resolve, reject) => { child.once('close', (code, signal) => resolve({ code, signal })); child.once('error', reject); });
    child.stdout.on('data', bytes => {
      stdoutBytes += bytes.length;
      if (stdoutBytes > 65536) { protocolError = Error('Native PouchDB child output exceeds budget.'); rejectReady(protocolError); child.kill('SIGTERM'); return; }
      stdout += bytes.toString();
      for (;;) {
        const end = stdout.indexOf('\n'); if (end < 0) break;
        const line = stdout.slice(0, end); stdout = stdout.slice(end + 1);
        try {
          const row = JSON.parse(line);
          if (row.ready) resolveReady(row);
          if (row.stopped) stopped = row;
        } catch (error) { protocolError = error; rejectReady(error); child.kill('SIGTERM'); }
      }
    });
    child.stderr.on('data', bytes => { stderr += bytes.toString(); if (stderr.length > 16384) stderr = stderr.slice(-16384); });
    let timer, stopTimer, terminalTimer, escalated = false, observation, failure;
    try {
      const online = await Promise.race([ready, exited.then(result => { throw Error('Native PouchDB exited before readiness: ' + JSON.stringify(result) + ' ' + stderr); }),
        new Promise((_, reject) => { timer = setTimeout(() => reject(Error('Native PouchDB image startup timed out.')), 15000); })]);
      clearTimeout(timer);
      assert.equal(online.pid, child.pid); assert.equal(online.digest, bundle.digest); assert.equal(online.instance, instance);
      assert.equal(online.origin, 'http://127.0.0.1:13800'); assert.equal(online.version, '9.0.0');
      await waitForNative(new AbortController().signal, bundle.digest, instance);
      await assert.rejects(waitForNative(new AbortController().signal, bundle.digest, '0'.repeat(32)), /identity differs/);
      const rows = [];
      for (const record of packet.records) {
        const response = await fetch(online.origin + '/oasis_holons/' + record.id + '?conflicts=true', { redirect: 'error', signal: AbortSignal.timeout(5000) });
        assert.equal(response.status, 200);
        const row = await response.json();
        const expected = { ...JSON.parse(Buffer.from(record.jsonBase64, 'base64').toString()), _id: record.id };
        assert.equal(nativeHash(row), hash(canonical(expected))); assert.match(row._rev, /^1-[a-f0-9]+$/);
        rows.push({ id: row._id, revision: row._rev, sha256: nativeHash(row) });
      }
      if (pass) assert.deepEqual(rows, observed[0].records);
      observation = { pid: child.pid, records: rows };
    } catch (error) { failure = error; }
    finally {
      clearTimeout(timer);
      child.kill('SIGTERM');
      stopTimer = setTimeout(() => { escalated = true; child.kill('SIGKILL'); }, 10000);
      try {
        const result = await Promise.race([exited, new Promise((_, reject) => {
          terminalTimer = setTimeout(() => reject(Error('Native PouchDB child remains unconfirmed after escalation.')), 20000);
        })]);
        assert.equal(escalated, false, 'Native PouchDB shutdown required escalation.');
        assert.equal(result.code, 0, stderr); assert.equal(result.signal, null);
        assert.ok(stopped); assert.equal(stopped.closedHandles, 2); assert.equal(stopped.digest, bundle.digest);
        if (protocolError) throw protocolError;
      } catch (error) { failure = failure ? new AggregateError([failure, error], 'Image qualification and native shutdown failed.') : error; }
      finally { clearTimeout(stopTimer); clearTimeout(terminalTimer); }
    }
    if (failure) throw failure;
    const ackBytes = await fs.readFile(path.join(source, 'bootstrap.acknowledged.json'));
    if (pass) assert.deepEqual(ackBytes, originalAck); else originalAck = ackBytes;
    observed.push(observation);
  }
  const sourceAck = JSON.parse(await fs.readFile(path.join(source, 'bootstrap.acknowledged.json'), 'utf8'));
  assert.equal(sourceAck.digest, bundle.digest); assert.equal(sourceAck.records.length, 3);
  // Every actual native child has exited and released LevelDB before exact owned cleanup.
  assert.equal(await fs.realpath(directory), '/data');
  assert.equal(await fs.readFile(path.join(directory, '.pouch-image-owner'), 'utf8'), owner);
  await fs.rm(directory, { recursive: true });
  console.log(JSON.stringify({ qualified: true, node: process.version, pouchdb: '9.0.0', adapter: 'leveldb', freshRecords: 3,
    retainedRecords: 3, unchangedOriginalRevisions: true, ownedChildSigtermExits: 2, nativeStoresClosedPerChild: 2, disposableSourceRemoved: true }));
}
main().catch(error => { console.error(error); process.exitCode = 1; });
