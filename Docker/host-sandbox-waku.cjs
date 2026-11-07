'use strict';
const fs = require('node:fs/promises');
const path = require('node:path');
const crypto = require('node:crypto');
const { setTimeout: delay } = require('node:timers/promises');

const version = '0.39.2';
const archiveName = 'store.sqlite3';
const endpoint = 'http://127.0.0.1:28645';
const connectionPending = new Set(['ECONNREFUSED', 'ECONNRESET', 'UND_ERR_SOCKET', 'UND_ERR_CONNECT_TIMEOUT']);
const hash = bytes => crypto.createHash('sha256').update(bytes).digest('hex');
const fields = (value, names) => value && typeof value === 'object' && !Array.isArray(value) &&
  Object.keys(value).sort().join(',') === [...names].sort().join(',');

function captureBundle(text, descriptorText) {
  if (typeof text !== 'string' || Buffer.byteLength(text) > 131072)
    throw Error('A bounded canonical Waku archive bundle is required.');
  const bundle = JSON.parse(text);
  // This operator-only wire format is produced by JSON.stringify. Round-tripping
  // also refuses duplicate keys before admitting any native file operation.
  if (JSON.stringify(bundle) !== text || !fields(bundle, ['schema', 'archiveBase64', 'receipt']) || bundle.schema !== 1)
    throw Error('The Waku archive bundle must use its canonical schema.');
  const receipt = bundle.receipt;
  if (!fields(receipt, ['archive', 'sha256', 'bytes', 'messageHashes']) ||
      receipt.archive !== 'store-qualified.sqlite3' || !Number.isInteger(receipt.bytes) ||
      receipt.bytes < 16 || receipt.bytes > 65536 || !/^[a-f0-9]{64}$/.test(receipt.sha256) ||
      !Array.isArray(receipt.messageHashes) || receipt.messageHashes.length !== 3 ||
      receipt.messageHashes.some(key => typeof key !== 'string' || !/^0x[a-f0-9]{64}$/.test(key)) ||
      new Set(receipt.messageHashes).size !== 3 || typeof bundle.archiveBase64 !== 'string')
    throw Error('A qualified standalone Waku archive and three distinct hashes are required.');
  const bytes = Buffer.from(bundle.archiveBase64, 'base64');
  if (bytes.length !== receipt.bytes || bytes.toString('base64') !== bundle.archiveBase64 ||
      hash(bytes) !== receipt.sha256 || !bytes.subarray(0, 16).equals(Buffer.from('SQLite format 3\0')))
    throw Error('The Waku archive differs from its qualification receipt.');
  if (typeof descriptorText !== 'string' || Buffer.byteLength(descriptorText) > 1024)
    throw Error('A bounded reviewed Waku descriptor is required.');
  const descriptor = JSON.parse(descriptorText);
  if (!fields(descriptor, ['sourceOwner', 'messageHashes']) ||
      typeof descriptor.sourceOwner !== 'string' ||
      !/^[a-f0-9]{8}-(?:[a-f0-9]{4}-){3}[a-f0-9]{12}$/.test(descriptor.sourceOwner) ||
      descriptor.sourceOwner === '00000000-0000-0000-0000-000000000000' ||
      JSON.stringify(descriptor.messageHashes) !== JSON.stringify(receipt.messageHashes))
    throw Error('The Waku descriptor must bind the complete qualified catalogue.');
  return { bytes, digest: hash(text), owner: descriptor.sourceOwner, hashes: [...receipt.messageHashes] };
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
  if (path.resolve(directory) !== directory) throw Error('An absolute Waku source directory is required.');
  const parent = path.dirname(directory);
  if (!(await inspect(parent))) {
    const ancestor = path.dirname(parent);
    if (await fs.realpath(ancestor) !== ancestor || !(await fs.lstat(ancestor)).isDirectory())
      throw Error('The Waku mount parent must be an existing real owned directory.');
    await fs.mkdir(parent, { mode: 0o700 });
  }
  if (await fs.realpath(parent) !== parent || !(await fs.lstat(parent)).isDirectory())
    throw Error('The Waku source parent must be an existing real owned directory.');
  const acknowledgement = { schema: 1, digest: bundle.digest, version, sourceOwner: bundle.owner, messageHashes: bundle.hashes };
  const acknowledgementPath = path.join(directory, 'bootstrap.acknowledged.json');
  const archivePath = path.join(directory, archiveName);
  if (await inspect(directory)) {
    if (await fs.realpath(directory) !== directory || !(await fs.lstat(directory)).isDirectory())
      throw Error('The existing Waku source directory differs.');
    const ackStat = await inspect(acknowledgementPath);
    const archiveStat = await inspect(archivePath);
    if (!ackStat?.isFile() || ackStat.isSymbolicLink() || ackStat.size > 4096 ||
        !archiveStat?.isFile() || archiveStat.isSymbolicLink() ||
        (await fs.readFile(acknowledgementPath, 'utf8')) !== JSON.stringify(acknowledgement))
      throw Error('Existing Waku source requires reconciliation before startup.');
    // The native process owns the working archive and SQLite sidecars after
    // provisioning. Readiness checks the original catalogue on every startup.
    return;
  }
  await fs.mkdir(directory, { mode: 0o700 });
  await writeNew(path.join(directory, 'bootstrap.intent.json'), JSON.stringify(acknowledgement));
  await writeNew(archivePath, bundle.bytes);
  await writeNew(acknowledgementPath, JSON.stringify(acknowledgement));
}

async function readCatalogue(response) {
  const reader = response.body.getReader();
  const chunks = [];
  let bytes = 0;
  try {
    for (;;) {
      const item = await reader.read();
      if (item.done) break;
      bytes += item.value.length;
      if (bytes > 65536) { await reader.cancel(); throw Error('Waku readiness exceeds its response budget.'); }
      chunks.push(item.value);
    }
  } finally { reader.releaseLock(); }
  return JSON.parse(new TextDecoder('utf-8', { fatal: true }).decode(Buffer.concat(chunks)));
}

async function waitForNative(bundle, signal, probe = fetch) {
  const deadline = Date.now() + 20000;
  const url = endpoint + '/store/v3/messages?includeData=true&pageSize=4&hashes=' + bundle.hashes.join(',');
  while (Date.now() < deadline) {
    signal.throwIfAborted();
    const timeout = AbortSignal.timeout(1000);
    let response;
    try { response = await probe(url, { redirect: 'error', signal: AbortSignal.any([signal, timeout]) }); }
    catch (error) {
      signal.throwIfAborted();
      if (!timeout.aborted && !connectionPending.has(error.cause?.code)) throw error;
      await delay(100, undefined, { signal });
      continue;
    }
    if (response.status === 503) {
      await response.body?.cancel();
      await delay(100, undefined, { signal });
      continue;
    }
    if (response.status !== 200) throw Error('Waku readiness returned an unexpected HTTP status.');
    const result = await readCatalogue(response);
    const hashes = result?.messages?.map(row => row.messageHash);
    if (result.statusCode !== 200 || result.paginationCursor || !Array.isArray(hashes) ||
        hashes.length !== 3 || new Set(hashes).size !== 3 || hashes.some(key => !bundle.hashes.includes(key)))
      throw Error('Waku readiness must return every qualified message exactly once.');
    // The shipping C# activation then validates each message's topics, exact
    // Int64 timestamp and native SHA-256 before exposing its CQL profile.
    return;
  }
  throw Error('Waku native startup deadline expired.');
}

async function serviceConfiguration(environment, directory = '/data/COSMIC/SandboxWaku', binary = '/opt/waku/logosdeliverynode') {
  if (environment.OASIS_SANDBOX_WAKU !== 'true') throw Error('Explicit Waku sandbox runtime flag required.');
  const bundle = captureBundle(environment.OASIS_SANDBOX_WAKU_BOOTSTRAP_JSON, environment.CQL_WAKU_STORE_QUERY_SOURCE_JSON);
  await prepare(bundle, directory);
  const nativeEnvironment = {};
  for (const key of ['PATH', 'HOME', 'TMPDIR']) if (environment[key]) nativeEnvironment[key] = environment[key];
  return {
    command: binary, cwd: directory, env: nativeEnvironment,
    args: ['--listen-address=127.0.0.1', '--tcp-port=28600', '--rest=true', '--rest-address=127.0.0.1',
      '--rest-port=28645', '--rest-admin=false', '--cluster-id=123', '--shard=1', '--relay=true',
      '--discv5-discovery=false', '--peer-exchange=false', '--rln-relay=false', '--store=true',
      '--store-message-db-url=sqlite://' + archiveName],
    check: signal => waitForNative(bundle, signal)
  };
}

module.exports = { captureBundle, prepare, waitForNative, serviceConfiguration };
