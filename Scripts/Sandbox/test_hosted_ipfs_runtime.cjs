'use strict';
const test = require('node:test');
const assert = require('node:assert/strict');
const { EventEmitter } = require('node:events');
const fs = require('node:fs/promises');
const path = require('node:path');
const crypto = require('node:crypto');
const { captureBundle, bootstrap, supervise, waitForNative, assertSandbox } = require('../../Docker/host-sandbox-ipfs.cjs');
const hash = value => crypto.createHash('sha256').update(value).digest('hex');
const cid = character => 'Qm' + character.repeat(44);
function sample() {
  const bytes = Buffer.from('controlled capture only');
  return { schema: 1, catalogueCid: cid('a'), blocks: ['a', 'b'].map(character =>
    ({ cid: cid(character), sha256: hash(bytes), carBase64: bytes.toString('base64') })) };
}

test('native source provisioning uses the same authoritative sandbox DNA flag as WEB4', () => {
  assert.doesNotThrow(() => assertSandbox('{"OASIS":{"Sandbox":{"Enabled":true}}}'));
  for (const value of [undefined, '{}', '{"OASIS":{"Sandbox":{"Enabled":false}}}', '{"OASIS":{"Sandbox":{"Enabled":"true"}}}'])
    assert.throws(() => assertSandbox(value));
  assert.throws(() => assertSandbox('malformed-sensitive-DNA'), error => !error.message.includes('malformed-sensitive-DNA'));
});

test('capture owns a canonical bounded bundle and rejects damaged or ambiguous transfers', () => {
  const source = sample();
  const first = captureBundle(JSON.stringify(source));
  assert.equal(first.digest, captureBundle(JSON.stringify(source, null, 2)).digest);
  source.blocks[0].cid = cid('c');
  assert.equal(first.catalogueCid, cid('a'));
  for (const change of [
    value => { value.extra = true; },
    value => { value.blocks[0].extra = true; },
    value => { value.blocks[0].sha256 = '0'.repeat(64); },
    value => { value.blocks[0].carBase64 += '\n'; },
    value => { value.blocks[1] = value.blocks[0]; },
    value => { value.catalogueCid = cid('z'); },
    value => { value.blocks[0].cid = '../untrusted'; },
    value => { value.blocks = []; }
  ]) {
    const value = sample(); change(value);
    assert.throws(() => captureBundle(JSON.stringify(value)));
  }
  assert.throws(() => captureBundle(' '.repeat(131073)));
});

function processes(check = async () => {}) {
  const children = [];
  const signals = new EventEmitter();
  const launch = (command, args, options) => {
    const child = new EventEmitter();
    child.kills = [];
    child.kill = signal => { child.kills.push(signal); child.emit('exit', 0); };
    children.push({ command, args, options, child });
    return child;
  };
  const done = supervise({ launch, ready: { check, env: { IPFS_PATH: 'owned-repo' } }, signals, stopMilliseconds: 100 });
  return { children, signals, done };
}
const tick = () => new Promise(resolve => setImmediate(resolve));

test('API starts only after native readiness; native failure stops the API', async () => {
  let release;
  const ready = new Promise(resolve => { release = resolve; });
  const run = processes(() => ready);
  assert.deepEqual(run.children.map(value => value.command), ['/usr/local/bin/ipfs']);
  release(); await tick();
  assert.deepEqual(run.children.map(value => value.command), ['/usr/local/bin/ipfs', 'dotnet']);
  run.children[0].child.emit('exit', 0); // Even a clean, unexpected native exit loses the source.
  assert.equal(await run.done, 1);
  assert.deepEqual(run.children[1].child.kills, ['SIGTERM']);
});

test('readiness rejection tears down the owned native process without starting API', async () => {
  const run = processes(async () => { throw Error('native health failed'); });
  await assert.rejects(run.done, /native health failed/);
  assert.equal(run.children.length, 1);
  assert.deepEqual(run.children[0].child.kills, ['SIGTERM']);
});

test('API exit and host signals terminate only the owned native/API pair', async () => {
  for (const action of ['api-exit', 'SIGTERM', 'SIGINT']) {
    const run = processes(); await tick();
    if (action === 'api-exit') run.children[1].child.emit('exit', 7);
    else run.signals.emit(action);
    assert.equal(await run.done, action === 'api-exit' ? 7 : 0);
    assert.deepEqual(run.children[0].child.kills, ['SIGTERM']);
    assert.equal(run.signals.listenerCount('SIGTERM'), 0);
  }
});

test('shutdown during startup cancels and observes native health before releasing ownership', async () => {
  let observed = false;
  const run = processes(signal => new Promise((_, reject) => signal.addEventListener('abort', () =>
    { observed = true; reject(Error('cancelled readiness')); }, { once: true })));
  run.signals.emit('SIGTERM');
  assert.equal(await run.done, 0);
  assert.equal(observed, true);
  assert.equal(run.children.length, 1);
});

for (const scenario of ['ready', 'foreign-node', 'wrong-version']) test('native health owns exact node identity: ' + scenario, async () => {
  const paths = [];
  const probe = async (url, options) => {
    paths.push(url);
    assert.equal(options.method, 'POST');
    assert.equal(options.redirect, 'error');
    return { ok: true, json: async () => url.endsWith('/version')
      ? { Version: scenario === 'wrong-version' ? '0.42.0' : '0.43.1' }
      : { ID: scenario === 'foreign-node' ? 'someone-else' : 'owned-node' } };
  };
  const work = waitForNative(new AbortController().signal, 'owned-node', probe);
  if (scenario === 'ready') await work;
  else await assert.rejects(work, scenario === 'foreign-node' ? /identity differs/ : /health check differs/);
  assert.equal(paths.length, scenario === 'wrong-version' ? 1 : 2);
});

test('native bootstrap imports the retained encrypted CARs once and preserves identity on repeat', {
  skip: process.platform !== 'win32' ? 'Explicit cached Windows Kubo native gate.' : false
}, async () => {
  const root = path.resolve(__dirname, '../..');
  const directory = path.join(root, 'TestResults/HostedIpfsBootstrapCurrent');
  // A fresh bootstrap cannot be tested against the retained user repository.
  // This one bounded disposable probe is removed only after every assertion passes.
  await assert.rejects(fs.stat(directory), { code: 'ENOENT' });
  await fs.mkdir(directory);
  const transfers = JSON.parse(await fs.readFile(path.join(root, 'LocalState/COSMIC/HostedIpfsSourceCurrent/source-transfer.json'), 'utf8'));
  const blocks = [];
  for (const entry of transfers) blocks.push({ cid: entry.Cid, sha256: entry.Sha256.toLowerCase(),
    carBase64: (await fs.readFile(entry.Path)).toString('base64') });
  const bundle = captureBundle(JSON.stringify({ schema: 1, catalogueCid: blocks[0].cid, blocks }));
  const binary = 'C:/Users/david/.cache/oasis-runtimes/kubo-0.43.1/ipfs.exe';
  const owned = path.join(directory, 'valid');
  const first = await bootstrap({ bundle, directory: owned, binary });
  const configBefore = await fs.readFile(path.join(first.repo, 'config'));
  const acknowledgementBefore = await fs.readFile(path.join(owned, 'bootstrap.acknowledged.json'));
  await bootstrap({ bundle, directory: owned, binary });
  assert.deepEqual(await fs.readFile(path.join(first.repo, 'config')), configBefore);
  assert.deepEqual(await fs.readFile(path.join(owned, 'bootstrap.acknowledged.json')), acknowledgementBefore);
  const unknown = path.join(directory, 'unknown');
  await fs.mkdir(unknown);
  await fs.writeFile(path.join(unknown, 'bootstrap.intent.json'), '{"ownedTestInterruption":true}');
  await assert.rejects(bootstrap({ bundle, directory: unknown, binary }), /reconcile/);
  assert.deepEqual((await fs.readdir(unknown)).sort(), ['bootstrap.intent.json']);
  const changed = captureBundle(JSON.stringify({ schema: 1, catalogueCid: blocks[0].cid, blocks: blocks.slice(0, 3) }));
  await assert.rejects(bootstrap({ bundle: changed, directory: owned, binary }), /explicit migration/);
  await fs.writeFile(path.join(root, 'TestResults/HostedCqlIntegrationCurrent/ipfs-runtime-native-proof.json'), JSON.stringify({
    blocks: blocks.length, digest: bundle.digest, repeatUnchanged: true, unknownStateRejected: true,
    changedSourceRejected: true, configSha256: hash(configBefore), portsStarted: 0, nativeVersion: '0.43.1'
  }, null, 2));
  assert.equal(await fs.realpath(directory), directory);
  assert.equal(path.dirname(directory), path.join(root, 'TestResults'));
  await fs.rm(directory, { recursive: true }); // Exact task-owned successful probe; no process was started.
});
