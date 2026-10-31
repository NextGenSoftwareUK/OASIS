'use strict';
const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const path = require('node:path');
const crypto = require('node:crypto');
const { EventEmitter } = require('node:events');
const { captureBundle, prepare, verify } = require('../../Docker/host-sandbox-solid.cjs');
const { supervise } = require('../../Docker/host-sandbox-ipfs.cjs');
const hash = bytes => crypto.createHash('sha256').update(bytes).digest('hex');
const root = path.resolve(__dirname, '../../TestResults/HostedSolidBootstrapCurrent');

function sample() {
  return { schema: 1, root: 'http://localhost:13000/', records: [1, 2, 3].map(number => {
    const id = 'bf89482b-470c-45aa-b750-b19fa6d8e9e' + number;
    const body = Buffer.from('controlled unit-test envelope ' + number);
    return { id, sha256: hash(body), turtleBase64: body.toString('base64') };
  }) };
}

test('SOLID source transfer binds three unique records to one exact URI without credentials', () => {
  const source = sample();
  const first = captureBundle(JSON.stringify(source));
  assert.equal(first.digest, captureBundle(JSON.stringify(source, null, 2)).digest);
  for (const change of [
    input => { input.root = 'http://foreign.invalid/'; },
    input => { input.records.pop(); },
    input => { input.records[1] = input.records[0]; },
    input => { input.extra = 'private-secret'; },
    input => { input.records[0].id = '../escape'; },
    input => { input.records[0].sha256 = '0'.repeat(64); },
    input => { input.records[0].turtleBase64 += '\n'; },
    input => { input.records[0].id = '00000000-0000-0000-0000-000000000000'; }
  ]) {
    const input = sample(); change(input);
    assert.throws(() => captureBundle(JSON.stringify(input)));
  }
  assert.throws(() => captureBundle(' '.repeat(131073)));
});

test('native template preparation is intent-fenced, read-only from creation and never replays an interrupted initializer', async () => {
  await assert.rejects(fs.stat(root), { code: 'ENOENT' });
  await fs.mkdir(root, { recursive: true });
  const runtime = path.join(root, 'runtime-contract');
  await fs.mkdir(runtime);
  await fs.writeFile(path.join(runtime, 'package.json'), '{"version":"7.1.8"}');
  const directory = path.join(root, 'source');
  const bundle = captureBundle(JSON.stringify(sample()));
  const first = await prepare({ bundle, directory, runtime });
  assert.equal(first.fresh, true);
  assert.equal(first.env.OASIS_DNA_JSON, undefined);
  assert.equal(first.env.OASIS_SOLID_STORAGE_KEY, undefined);
  assert.equal(first.env.OASIS_SANDBOX_SOLID_BOOTSTRAP_JSON, undefined);
  assert.deepEqual(first.args.slice(-6), ['-b', bundle.root, '-p', '13000', '-l', 'warn']);
  const permissions = await fs.readFile(path.join(directory, 'template/wac/.acl'), 'utf8');
  assert.match(permissions, /acl:mode acl:Read;/);
  assert.doesNotMatch(permissions, /acl:(Write|Append|Control)/);
  for (const record of bundle.records)
    assert.equal(hash(await fs.readFile(path.join(directory, 'template/base/oasis/holons', record.id + '.ttl'))), record.sha256);
  await assert.rejects(prepare({ bundle, directory, runtime }), /without acknowledgement/);

  // Test the acknowledgement boundary independently from native RDF parsing.
  // This controlled parser is not a native Pod, and does not establish a live-source result.
  class ControlledParser { parse(value) { return [{ subject: value, predicate: 1 }, { subject: value, predicate: 2 }]; } }
  const requests = [];
  const probe = async uri => {
    requests.push(uri);
    const record = bundle.records.find(record => uri.endsWith(record.id + '.ttl'));
    return new Response(record ? Buffer.from(record.turtleBase64, 'base64') : '',
      { headers: { 'WAC-Allow': 'user="read",public="read"' } });
  };
  const signal = new AbortController().signal;
  await assert.rejects(verify(first, signal, ControlledParser,
    async () => new Response('', { headers: { 'WAC-Allow': 'user="read,write"' } })), /not read-only/);
  await assert.rejects(fs.stat(first.ackPath), { code: 'ENOENT' });
  await verify(first, signal, ControlledParser, probe);
  assert.equal(requests.length, 4);
  const ack = await fs.readFile(first.ackPath);
  const repeated = await prepare({ bundle, directory, runtime });
  assert.equal(repeated.fresh, false);
  await verify(repeated, signal, ControlledParser, probe);
  assert.deepEqual(await fs.readFile(first.ackPath), ack);
  const changed = sample(); changed.records[0].id = 'af89482b-470c-45aa-b750-b19fa6d8e9e1';
  await assert.rejects(prepare({ bundle: captureBundle(JSON.stringify(changed)), directory, runtime }), /explicit migration/);
  await fs.writeFile(path.join(directory, 'template/wac/.acl'), permissions + '# changed');
  await assert.rejects(prepare({ bundle, directory, runtime }), /configuration differs/);

  // Only this bounded successful synthetic probe is disposable; no server ran.
  assert.equal(await fs.realpath(root), root);
  assert.equal(path.dirname(root), path.resolve(__dirname, '../../TestResults'));
  await fs.rm(root, { recursive: true });
});

test('one process owner gates API on both native sources and tears down every child when either source exits', async () => {
  const children = [];
  const signals = new EventEmitter();
  let release;
  const secondReady = new Promise(resolve => { release = resolve; });
  const launch = command => {
    const child = new EventEmitter();
    child.kills = [];
    child.kill = signal => { child.kills.push(signal); child.emit('exit', 0); };
    children.push({ command, child });
    return child;
  };
  const done = supervise({ launch, signals, stopMilliseconds: 100,
    services: [{ command: 'native-ipfs', args: [], check: async () => {} },
      { command: 'native-solid', args: [], check: () => secondReady }] });
  await new Promise(resolve => setImmediate(resolve));
  assert.deepEqual(children.map(child => child.command), ['native-ipfs', 'native-solid']);
  release();
  await new Promise(resolve => setImmediate(resolve));
  assert.deepEqual(children.map(child => child.command), ['native-ipfs', 'native-solid', 'dotnet']);
  children[1].child.emit('exit', 0);
  assert.equal(await done, 1);
  assert.deepEqual(children[0].child.kills, ['SIGTERM']);
  assert.deepEqual(children[2].child.kills, ['SIGTERM']);
  assert.equal(signals.listenerCount('SIGTERM'), 0);
});

test('an earlier source exit cancels later startup work and prevents API startup', async () => {
  const children = [];
  let cancelled = false;
  const launch = command => {
    const child = new EventEmitter();
    child.kill = () => child.emit('exit', 0);
    children.push({ command, child });
    return child;
  };
  const done = supervise({ launch, signals: new EventEmitter(), stopMilliseconds: 100,
    services: [{ command: 'ipfs', args: [], check: async () => {} },
      { command: 'solid', args: [], check: signal => new Promise((_, reject) =>
        signal.addEventListener('abort', () => { cancelled = true; reject(Error('cancelled')); }, { once: true })) }] });
  await new Promise(resolve => setImmediate(resolve));
  children[0].child.emit('exit', 1);
  assert.equal(await done, 1);
  assert.equal(cancelled, true);
  assert.deepEqual(children.map(child => child.command), ['ipfs', 'solid']);
});
