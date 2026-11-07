'use strict';
const fs = require('node:fs/promises');
const path = require('node:path');
const crypto = require('node:crypto');
const net = require('node:net');
const { setTimeout: delay } = require('node:timers/promises');
const configuration = require('./DaprRuntime/configuration.cjs');
const { contract } = configuration;
const hash = value => crypto.createHash('sha256').update(value).digest('hex');
const guid = value => typeof value === 'string' && /^[a-f0-9]{8}-(?:[a-f0-9]{4}-){3}[a-f0-9]{12}$/.test(value) && value !== '00000000-0000-0000-0000-000000000000';

function sourceDescriptor(source, environment) {
  if (source?.schema !== 1 || !guid(source.sourceOwner) || !Array.isArray(source.ids) || source.ids.length !== 3 ||
      source.ids.some(id => !guid(id)) || new Set(source.ids).size !== 3 ||
      !Array.isArray(source.records) || source.records.length !== 4 ||
      environment.OASIS_SANDBOX_DAPR !== 'true' || environment.CQL_DAPR_QUERY_STORE !== contract.store ||
      environment.CQL_DAPR_QUERY_GRPC_ENDPOINT !== `http://127.0.0.1:${contract.ports.grpc}/` ||
      environment.CQL_DAPR_QUERY_PROBE_ID !== source.ids[0]) throw Error('Dapr source/reader binding differs.');
  configuration.credential(environment.CQL_DAPR_QUERY_SIDECAR_TOKEN);
  if (source.sidecarTokenSha256 !== hash(environment.CQL_DAPR_QUERY_SIDECAR_TOKEN)) throw Error('Dapr sidecar identity differs.');
  const grants = JSON.parse(environment.CQL_PUBLIC_READ_CATALOGUES_JSON || '{}').DaprOASIS;
  if (!Array.isArray(grants) || grants.length !== 3 || [...grants].sort().join(',') !== [...source.ids].sort().join(','))
    throw Error('Dapr public source grant differs.');
  const ids = new Set(); let deleted = 0;
  for (const record of source.records) {
    if (!guid(record.id) || ids.has(record.id) || typeof record.value !== 'string' || Buffer.byteLength(record.value) > 4096 ||
        typeof record.etag !== 'string' || !record.etag || record.etag.length > 256) throw Error('Dapr acknowledged record differs.');
    ids.add(record.id);
    const value = JSON.parse(record.value);
    if (value.id !== record.id || value.createdByAvatarId !== source.sourceOwner || value.isPublic !== true)
      throw Error('Dapr original record authority differs.');
    if (value.deletedDate === '0001-01-01T00:00:00') {
      if (!source.ids.includes(record.id)) throw Error('Dapr live record lies outside its catalogue.');
    } else {
      if (source.ids.includes(record.id) || !Number.isFinite(Date.parse(value.deletedDate))) throw Error('Dapr tombstone differs.');
      deleted++;
    }
  }
  if (deleted !== 1 || source.ids.some(id => !ids.has(id))) throw Error('Dapr original catalogue is incomplete.');
  return source;
}

async function readSource(environment, directory) {
  if (await fs.realpath(directory) !== directory || !(await fs.lstat(directory)).isDirectory()) throw Error('Dapr source root is redirected.');
  async function read(name, maximum, privateFile = false) {
    const file = path.join(directory, name); const stat = await fs.lstat(file);
    if (!stat.isFile() || stat.isSymbolicLink() || stat.size > maximum || await fs.realpath(file) !== file ||
        (privateFile && (stat.mode & 0o077))) throw Error('Dapr acknowledged source file differs.');
    return fs.readFile(file, 'utf8');
  }
  const source = sourceDescriptor(JSON.parse(await read('source.acknowledged.json', 32768)), environment);
  const password = configuration.credential(await read('reader-password', 64, true));
  const expected = { 'reader.acl': configuration.readerAcl(password), 'components/redis.yaml': configuration.component(password, 'reader') };
  for (const [name, value] of Object.entries(expected)) {
    if (await read(name, 4096, true) !== value || source.configurationSha256?.[name] !== hash(value))
      throw Error('Dapr restricted reader configuration changed.');
  }
  const queue = [path.join(directory, 'store')]; let files = 0; let bytes = 0; let entries = 0;
  while (queue.length) {
    const current = queue.shift();
    if (await fs.realpath(current) !== current || (await fs.lstat(current)).isSymbolicLink()) throw Error('Dapr native storage is redirected.');
    for (const entry of await fs.readdir(current, { withFileTypes: true })) {
      const file = path.join(current, entry.name); entries++;
      if (entry.isSymbolicLink()) throw Error('Dapr native storage contains a link.');
      if (entry.isDirectory()) queue.push(file);
      else if (entry.isFile()) { files++; bytes += (await fs.lstat(file)).size; }
      else throw Error('Unexpected Dapr native storage entry.');
      if (entries > 128 || bytes > 64 * 1024 * 1024) throw Error('Dapr native source exceeds its bounded budget.');
    }
  }
  if (!files || !bytes) throw Error('Original durable Dapr source missing; never reseed.');
  return { source, password };
}

function probeRedis(password, signal) {
  return new Promise((resolve, reject) => {
    const socket = net.createConnection({ host: '127.0.0.1', port: contract.ports.redis });
    let data = ''; let settled = false;
    function finish(error) {
      if (settled) return; settled = true; signal.removeEventListener('abort', abort); socket.destroy();
      if (error) reject(error); else resolve();
    }
    const abort = () => finish(signal.reason || Error('Dapr startup cancelled.'));
    signal.addEventListener('abort', abort, { once: true });
    socket.setTimeout(1000, () => finish(Error('Dapr native reader handshake timed out.')));
    socket.once('error', finish);
    socket.once('end', () => finish(Error('Dapr native reader closed before acknowledgement.')));
    socket.once('close', () => finish(Error('Dapr native reader connection closed.')));
    socket.once('connect', () => socket.write(`*3\r\n$4\r\nAUTH\r\n$6\r\nreader\r\n$64\r\n${password}\r\n*1\r\n$4\r\nPING\r\n`));
    socket.on('data', chunk => {
      data += chunk.toString('utf8');
      if (data.length > 1024 || data.startsWith('-')) return finish(Error('Dapr native reader authentication failed.'));
      if (data === '+OK\r\n+PONG\r\n') finish();
      else if (data.split('\r\n').length > 3) finish(Error('Dapr native reader response differs.'));
    });
    if (signal.aborted) abort();
  });
}

async function startup(check, signal) {
  const deadline = Date.now() + 15000;
  // Bounded listener/health startup polling only; source reads and mutations are never retried.
  while (Date.now() < deadline) {
    signal.throwIfAborted();
    try { if (await check()) return; }
    catch (error) { if ((error.cause?.code || error.code) !== 'ECONNREFUSED') throw error; }
    await delay(100, undefined, { signal });
  }
  throw Error('Dapr native startup deadline expired.');
}

async function readRecord(record, token, signal) {
  signal.throwIfAborted();
  const key = encodeURIComponent('oasis/holon/' + record.id.replaceAll('-', ''));
  const response = await fetch(`http://127.0.0.1:${contract.ports.http}/v1.0/state/${contract.store}/${key}`, {
    headers: { 'dapr-api-token': token }, redirect: 'error', signal: AbortSignal.any([signal, AbortSignal.timeout(3000)]) });
  const etag = response.headers.get('etag');
  if (response.status !== 200 || !etag || etag.length > 256 || (record.etag !== undefined && etag !== record.etag))
    throw Error('Recovered Dapr record/ETag differs.');
  const chunks = []; let bytes = 0;
  for await (const chunk of response.body) {
    bytes += chunk.byteLength;
    if (bytes > 8192) throw Error('Recovered Dapr source exceeds its response budget.');
    chunks.push(Buffer.from(chunk));
  }
  if (JSON.parse(Buffer.concat(chunks).toString('utf8')) !== record.value) throw Error('Recovered Dapr source value differs.');
  return { id: record.id, value: record.value, etag };
}
async function observeRows(records, token, signal) {
  const observed = [];
  for (const record of records) observed.push(await readRecord(record, token, signal));
  return observed;
}
async function verifyRows(source, token, signal) {
  await observeRows(source.records, token, signal);
}
async function waitForRedis(password, signal) {
  await startup(async () => { await probeRedis(password, signal); return true; }, signal);
}
async function waitForSidecar(token, signal) {
  await startup(async () => {
    const response = await fetch(`http://127.0.0.1:${contract.ports.http}/v1.0/healthz`, {
      headers: { 'dapr-api-token': token }, redirect: 'error', signal: AbortSignal.any([signal, AbortSignal.timeout(1000)]) });
    if (response.status === 503) return false;
    if (response.status !== 204) throw Error('Dapr native health response differs.');
    return true;
  }, signal);
}

async function serviceConfigurations(environment, directory = '/data/COSMIC/SandboxDapr') {
  const { source, password } = await readSource(environment, directory);
  const nativeEnvironment = {};
  for (const key of ['PATH', 'HOME', 'TMPDIR', 'DOTNET_ROOT']) if (environment[key]) nativeEnvironment[key] = environment[key];
  return [
    { command: '/opt/garnet/GarnetServer', args: configuration.garnetArguments(directory, path.join(directory, 'reader.acl'), 'recover'),
      cwd: directory, env: nativeEnvironment, check: signal => waitForRedis(password, signal) },
    { command: '/opt/dapr/daprd', args: configuration.daprArguments(directory), cwd: directory,
      env: { ...nativeEnvironment, DAPR_API_TOKEN: environment.CQL_DAPR_QUERY_SIDECAR_TOKEN }, check: async signal => {
        await waitForSidecar(environment.CQL_DAPR_QUERY_SIDECAR_TOKEN, signal);
        await verifyRows(source, environment.CQL_DAPR_QUERY_SIDECAR_TOKEN, signal);
      } }
  ];
}
module.exports = { sourceDescriptor, readSource, probeRedis, observeRows, verifyRows, waitForRedis, waitForSidecar, serviceConfigurations };
