'use strict';
const test = require('node:test');
const assert = require('node:assert/strict');
const crypto = require('node:crypto');
const fs = require('node:fs/promises');
const os = require('node:os');
const path = require('node:path');
const { captureBundle, prepare, waitForNative, serviceConfiguration } = require('../../Docker/host-sandbox-waku.cjs');

const hashes = ['1', '2', '3'].map(value => '0x' + value.repeat(64));
const owner = 'b05b1234-1000-4000-8000-000000000001';
function sample() {
  const bytes = Buffer.concat([Buffer.from('SQLite format 3\0'), Buffer.alloc(64)]);
  return { schema: 1, archiveBase64: bytes.toString('base64'), receipt: {
    archive: 'store-qualified.sqlite3', bytes: bytes.length,
    sha256: crypto.createHash('sha256').update(bytes).digest('hex'), messageHashes: [...hashes]
  } };
}
function captured(value = sample(), descriptor = { sourceOwner: owner, messageHashes: hashes }) {
  return captureBundle(JSON.stringify(value), JSON.stringify(descriptor));
}
async function temporary(run) {
  const root = await fs.mkdtemp(path.join(os.tmpdir(), 'oasis-waku-control-'));
  try { await run(root); }
  finally {
    const resolved = await fs.realpath(root);
    if (path.dirname(resolved) !== await fs.realpath(os.tmpdir()) ||
        !path.basename(resolved).startsWith('oasis-waku-control-'))
      throw Error('Temporary Waku cleanup target differs from its owned directory.');
    await fs.rm(resolved, { recursive: true });
  }
}
const response = value => new Response(JSON.stringify(value), { status: 200 });
const catalogue = () => ({ statusCode: 200, messages: hashes.map(messageHash => ({ messageHash })) });

test('Waku bootstrap captures the complete qualified standalone archive and descriptor', () => {
  const bundle = captured();
  assert.deepEqual(bundle.hashes, hashes);
  assert.equal(bundle.owner, owner);
  assert.equal(bundle.bytes.length, sample().receipt.bytes);
});

test('Waku bootstrap rejects changed archive bytes before native I/O', () => {
  const value = sample();
  value.archiveBase64 = Buffer.alloc(value.receipt.bytes).toString('base64');
  assert.throws(() => captured(value), /differs/);
});

test('Waku bootstrap rejects a duplicate field in its canonical wire format', () => {
  const text = JSON.stringify(sample()).replace('"schema":1', '"schema":1,"schema":1');
  assert.throws(() => captureBundle(text, JSON.stringify({ sourceOwner: owner, messageHashes: hashes })), /canonical/);
});

test('Waku bootstrap rejects duplicate hashes and a partial or foreign descriptor', () => {
  const value = sample(); value.receipt.messageHashes[2] = hashes[0];
  assert.throws(() => captured(value), /distinct/);
  assert.throws(() => captured(sample(), { sourceOwner: owner, messageHashes: hashes.slice(0, 2) }), /complete/);
  assert.throws(() => captured(sample(), { sourceOwner: '00000000-0000-0000-0000-000000000000', messageHashes: hashes }), /complete/);
});

test('Waku native working archive is provisioned once and retained across native changes', async () => temporary(async root => {
  const directory = path.join(root, 'source'); const bundle = captured();
  await prepare(bundle, directory);
  const ack = await fs.readFile(path.join(directory, 'bootstrap.acknowledged.json'));
  await fs.appendFile(path.join(directory, 'store.sqlite3'), Buffer.from('native journal checkpoint'));
  const native = await fs.readFile(path.join(directory, 'store.sqlite3'));
  await prepare(bundle, directory);
  assert.deepEqual(await fs.readFile(path.join(directory, 'store.sqlite3')), native);
  assert.deepEqual(await fs.readFile(path.join(directory, 'bootstrap.acknowledged.json')), ack);
}));

test('Waku partial provisioning is refused without replacing its archive', async () => temporary(async root => {
  const directory = path.join(root, 'source'); await fs.mkdir(directory);
  await fs.writeFile(path.join(directory, 'store.sqlite3'), 'uncertain native state');
  await assert.rejects(prepare(captured(), directory), /reconciliation/);
  assert.equal(await fs.readFile(path.join(directory, 'store.sqlite3'), 'utf8'), 'uncertain native state');
}));

test('Waku provisions its source parent inside an existing owned mount', async () => temporary(async root => {
  const directory = path.join(root, 'COSMIC', 'source');
  await prepare(captured(), directory);
  assert.deepEqual(await fs.readFile(path.join(directory, 'store.sqlite3')), captured().bytes);
  await assert.rejects(prepare(captured(), 'relative-source'), /absolute/);
}));

test('Waku concurrent provisioners have one exclusive source owner', async () => temporary(async root => {
  const directory = path.join(root, 'source');
  const results = await Promise.allSettled([prepare(captured(), directory), prepare(captured(), directory)]);
  assert.equal(results.filter(value => value.status === 'fulfilled').length, 1);
  assert.deepEqual(await fs.readFile(path.join(directory, 'store.sqlite3')), captured().bytes);
  assert.ok(await fs.stat(path.join(directory, 'bootstrap.acknowledged.json')));
}));

test('Waku acknowledged source rejects an operator owner change', async () => temporary(async root => {
  const directory = path.join(root, 'source'); await prepare(captured(), directory);
  const changed = captured(sample(), { sourceOwner: 'b05b1234-1000-4000-8000-000000000002', messageHashes: hashes });
  await assert.rejects(prepare(changed, directory), /reconciliation/);
}));

test('Waku readiness checks all three qualified identities in one bounded native call', async () => {
  let calls = 0;
  await waitForNative(captured(), new AbortController().signal, async (url, options) => {
    calls++;
    assert.equal(new URL(url).hostname, '127.0.0.1');
    assert.equal(new URL(url).searchParams.get('hashes'), hashes.join(','));
    assert.equal(options.redirect, 'error');
    return response(catalogue());
  });
  assert.equal(calls, 1);
});

test('Waku readiness rejects partial, duplicate, foreign and paginated results without retry', async () => {
  for (const mutate of [value => value.messages.pop(), value => value.messages[2] = value.messages[0],
    value => value.messages[2].messageHash = '0x' + '4'.repeat(64), value => value.paginationCursor = 'more']) {
    let calls = 0; const value = catalogue(); mutate(value);
    await assert.rejects(waitForNative(captured(), new AbortController().signal, async () => {
      calls++; return response(value);
    }), /exactly once/);
    assert.equal(calls, 1);
  }
});

test('Waku readiness rejects oversized bodies and malformed JSON', async () => {
  await assert.rejects(waitForNative(captured(), new AbortController().signal,
    async () => new Response(' '.repeat(65537))), /budget/);
  await assert.rejects(waitForNative(captured(), new AbortController().signal,
    async () => new Response('{')), SyntaxError);
});

test('Waku readiness only polls documented startup states and observes cancellation', async () => {
  const controller = new AbortController(); let calls = 0;
  await assert.rejects(waitForNative(captured(), controller.signal, async () => {
    calls++; controller.abort(Error('owned shutdown'));
    throw Object.assign(Error('not listening yet'), { cause: { code: 'ECONNREFUSED' } });
  }), /owned shutdown/);
  assert.equal(calls, 1);
  await assert.rejects(waitForNative(captured(), new AbortController().signal,
    async () => { throw Error('unrelated transport failure'); }), /unrelated/);
});

test('Waku service uses the native loopback Store with a credential-free child environment', async () => temporary(async root => {
  const environment = { OASIS_SANDBOX_WAKU: 'true', OASIS_SANDBOX_WAKU_BOOTSTRAP_JSON: JSON.stringify(sample()),
    CQL_WAKU_STORE_QUERY_SOURCE_JSON: JSON.stringify({ sourceOwner: owner, messageHashes: hashes }),
    PATH: '/native', OASIS_RECORD_ENCRYPTION_KEY: 'test-only-secret', PRIVATE_SUBMODULE_PAT: 'test-only-secret' };
  const service = await serviceConfiguration(environment, path.join(root, 'source'), '/native/waku');
  assert.equal(service.command, '/native/waku');
  assert.deepEqual(service.env, { PATH: '/native' });
  assert.ok(service.args.includes('--rest-address=127.0.0.1'));
  assert.ok(service.args.includes('--rest-admin=false'));
  assert.ok(service.args.includes('--store-message-db-url=sqlite://store.sqlite3'));
  await assert.rejects(serviceConfiguration({}), /Explicit/);
}));
