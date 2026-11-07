'use strict';
const fs = require('node:fs/promises');
const path = require('node:path');
const crypto = require('node:crypto');
const net = require('node:net');
const tls = require('node:tls');
const { execFile } = require('node:child_process');
const { promisify } = require('node:util');
const { setTimeout: delay } = require('node:timers/promises');
const execute = promisify(execFile);

const hash = bytes => crypto.createHash('sha256').update(bytes).digest('hex');
const fields = (value, names) => value && typeof value === 'object' && !Array.isArray(value) &&
  Object.keys(value).sort().join(',') === [...names].sort().join(',');
const sourcePath = /^jetstream\/\$G\/streams\/KV_cosmic_cql_(?:public|foreign)\/(?:meta\.inf|meta\.sum|msgs\/(?:[1-9][0-9]*\.blk|index\.db))$/;

function captureBundle(text, descriptorText) {
  if (typeof text !== 'string' || Buffer.byteLength(text) > 32768 ||
      typeof descriptorText !== 'string' || Buffer.byteLength(descriptorText) > 2048)
    throw Error('A bounded qualified NATS source and descriptor are required.');
  const bundle = JSON.parse(text);
  const source = JSON.parse(descriptorText);
  if (JSON.stringify(bundle) !== text || !fields(bundle, ['schema', 'sourceSha256', 'files']) ||
      bundle.schema !== 1 || bundle.sourceSha256 !== hash(descriptorText) ||
      !fields(source, ['schema', 'endpoint', 'bucket', 'sourceOwner', 'ids']) || source.schema !== 1 ||
      source.endpoint !== 'tls://127.0.0.1:24222' || source.bucket !== 'cosmic_cql_public' ||
      !Array.isArray(source.ids) || source.ids.length !== 3 || new Set(source.ids).size !== 3 ||
      [source.sourceOwner, ...source.ids].some(id => typeof id !== 'string' ||
        !/^[a-f0-9]{8}-(?:[a-f0-9]{4}-){3}[a-f0-9]{12}$/.test(id) ||
        id === '00000000-0000-0000-0000-000000000000') ||
      !Array.isArray(bundle.files) || bundle.files.length < 6 || bundle.files.length > 32)
    throw Error('NATS source binding differs from its qualified catalogue.');
  let bytes = 0;
  const names = new Set();
  const files = bundle.files.map(file => {
    if (!fields(file, ['path', 'base64', 'sha256']) || typeof file.path !== 'string' ||
        !sourcePath.test(file.path) || names.has(file.path) || typeof file.base64 !== 'string' ||
        !/^[a-f0-9]{64}$/.test(file.sha256)) throw Error('Unexpected NATS native source entry.');
    names.add(file.path);
    const data = Buffer.from(file.base64, 'base64');
    bytes += data.length;
    if (data.toString('base64') !== file.base64 || hash(data) !== file.sha256 || bytes > 16777216)
      throw Error('NATS native source bytes differ from qualification.');
    return { path: file.path, data };
  });
  for (const bucket of ['public', 'foreign'])
    for (const file of ['meta.inf', 'meta.sum', 'msgs/1.blk'])
      if (!names.has(`jetstream/$G/streams/KV_cosmic_cql_${bucket}/${file}`))
        throw Error('The qualified NATS native store is incomplete.');
  return { digest: hash(text), sourceSha256: bundle.sourceSha256, files };
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
  if (path.resolve(directory) !== directory) throw Error('An absolute NATS source directory is required.');
  const parent = path.dirname(directory);
  if (await fs.realpath(parent) !== parent || !(await fs.lstat(parent)).isDirectory())
    throw Error('The NATS mount parent must be an existing real owned directory.');
  const ack = JSON.stringify({ schema: 1, version: '2.15.2', digest: bundle.digest,
    sourceSha256: bundle.sourceSha256 });
  const ackPath = path.join(directory, 'bootstrap.acknowledged.json');
  if (await inspect(directory)) {
    const stat = await inspect(ackPath);
    if (await fs.realpath(directory) !== directory || !(await fs.lstat(directory)).isDirectory() ||
        !stat?.isFile() || stat.isSymbolicLink() || stat.size > 4096 ||
        await fs.readFile(ackPath, 'utf8') !== ack)
      throw Error('Existing NATS source requires reconciliation before startup.');
    // Native JetStream owns working-file changes after the original placement.
    // Shipping SDK activation must verify every enrolled record on each start.
    for (const file of bundle.files) {
      const target = path.join(directory, 'store', file.path);
      if (await fs.realpath(target) !== target || !(await fs.lstat(target)).isFile())
        throw Error('An acknowledged NATS native source entry is missing or redirected.');
    }
    return;
  }
  await fs.mkdir(directory, { mode: 0o700 });
  await writeNew(path.join(directory, 'bootstrap.intent.json'), ack);
  for (const file of bundle.files) {
    const target = path.join(directory, 'store', file.path);
    await fs.mkdir(path.dirname(target), { recursive: true, mode: 0o700 });
    await writeNew(target, file.data);
  }
  await writeNew(ackPath, ack);
}

function serverConfiguration(directory, password) {
  if (typeof password !== 'string' || !/^[A-Za-z0-9_-]{43}$/.test(password))
    throw Error('The owned NATS reader requires its explicit bounded password.');
  return { host: '127.0.0.1', port: 24222, server_name: 'cosmic-cql-owned',
    jetstream: { store_dir: path.join(directory, 'store'), max_memory_store: 16777216, max_file_store: 33554432 },
    tls: { cert_file: path.join(directory, 'ca.pem'), key_file: path.join(directory, 'server.key') },
    authorization: { users: [{ user: 'cql_reader', password, permissions: {
      publish: { allow: ['$JS.API.INFO', '$JS.API.STREAM.INFO.KV_cosmic_cql_public',
        '$JS.API.DIRECT.GET.KV_cosmic_cql_public', '$JS.API.DIRECT.GET.KV_cosmic_cql_public.>',
        '$JS.API.STREAM.MSG.GET.KV_cosmic_cql_public'] }, subscribe: { allow: ['_INBOX.>'] } } }] } };
}

async function prepareTls(directory, password, openssl = 'openssl') {
  const configuration = JSON.stringify(serverConfiguration(directory, password));
  if (await fs.realpath(directory) !== directory || !(await fs.lstat(directory)).isDirectory())
    throw Error('The NATS TLS directory must be the real owned source directory.');
  const files = ['ca.pem', 'server.key', 'server.conf'];
  const ackPath = path.join(directory, 'tls.acknowledged.json');
  const ackStat = await inspect(ackPath);
  if (!ackStat) {
    if (await inspect(path.join(directory, 'tls.intent.json')) ||
        (await Promise.all(files.map(file => inspect(path.join(directory, file))))).some(Boolean))
      throw Error('Partial NATS TLS provisioning requires reconciliation.');
    await writeNew(path.join(directory, 'tls.intent.json'), JSON.stringify({ schema: 1, configurationSha256: hash(configuration) }));
    await execute(openssl, ['req', '-x509', '-newkey', 'rsa:2048', '-sha256', '-nodes',
      '-keyout', path.join(directory, 'server.key'), '-out', path.join(directory, 'ca.pem'),
      '-days', '3650', '-subj', '/CN=localhost', '-addext', 'subjectAltName=IP:127.0.0.1,DNS:localhost'],
    { timeout: 30000, maxBuffer: 65536, windowsHide: true });
    await fs.chmod(path.join(directory, 'server.key'), 0o600);
    await writeNew(path.join(directory, 'server.conf'), configuration);
    const hashes = {};
    for (const file of files) hashes[file] = hash(await fs.readFile(path.join(directory, file)));
    await writeNew(ackPath, JSON.stringify({ schema: 1, hashes }));
  } else if (!ackStat.isFile() || ackStat.isSymbolicLink() || ackStat.size > 4096) {
    throw Error('NATS TLS acknowledgement differs.');
  }
  const ack = JSON.parse(await fs.readFile(ackPath, 'utf8'));
  if (!fields(ack, ['schema', 'hashes']) || ack.schema !== 1 || !fields(ack.hashes, files))
    throw Error('NATS TLS acknowledgement schema differs.');
  for (const file of files) {
    const target = path.join(directory, file);
    const stat = await fs.lstat(target);
    if (!stat.isFile() || stat.isSymbolicLink() || stat.size > 16384 ||
        hash(await fs.readFile(target)) !== ack.hashes[file])
      throw Error('Persistent NATS TLS identity differs; explicit reconciliation required.');
  }
  if (await fs.readFile(path.join(directory, 'server.conf'), 'utf8') !== configuration)
    throw Error('Persistent NATS reader configuration differs; no automatic rotation permitted.');
  const ca = await fs.readFile(path.join(directory, 'ca.pem'));
  const certificate = new crypto.X509Certificate(ca);
  const publicKey = crypto.createPublicKey(await fs.readFile(path.join(directory, 'server.key')));
  if (!certificate.checkIP('127.0.0.1') || !certificate.checkHost('localhost') ||
      Date.parse(certificate.validFrom) > Date.now() || Date.parse(certificate.validTo) < Date.now() + 86400000 ||
      !certificate.verify(publicKey)) throw Error('NATS TLS certificate identity or validity differs.');
  return ca;
}

function probeReader(ca, password, signal) {
  return new Promise((resolve, reject) => {
    let socket = net.connect({ host: '127.0.0.1', port: 24222 });
    let buffer = '';
    const timer = setTimeout(() => finish(Error('NATS TLS readiness deadline expired.')), 3000);
    const abort = () => finish(signal.reason || Error('NATS readiness cancelled.'));
    const finish = error => { clearTimeout(timer); signal.removeEventListener('abort', abort);
      socket.destroy(); error ? reject(error) : resolve(); };
    signal.addEventListener('abort', abort, { once: true });
    socket.once('error', finish);
    socket.on('data', function info(chunk) {
      buffer += chunk.toString('utf8');
      if (buffer.length > 8192) return finish(Error('NATS readiness INFO exceeds its budget.'));
      if (!buffer.includes('\r\n')) return;
      socket.removeListener('data', info);
      let metadata;
      try {
        if (!buffer.startsWith('INFO ')) throw Error('Missing NATS INFO.');
        metadata = JSON.parse(buffer.slice(5, buffer.indexOf('\r\n')));
        if (metadata.version !== '2.15.2' || metadata.server_name !== 'cosmic-cql-owned' ||
            metadata.tls_required !== true || metadata.auth_required !== true)
          throw Error('Owned NATS native listener identity differs.');
      } catch (error) { return finish(error); }
      socket.removeListener('error', finish);
      socket = tls.connect({ socket, ca, servername: 'localhost', rejectUnauthorized: true });
      socket.once('error', finish);
      buffer = '';
      socket.once('secureConnect', () => socket.write('CONNECT ' + JSON.stringify({
        user: 'cql_reader', pass: password, protocol: 1, verbose: false, pedantic: true,
        tls_required: true }) + '\r\nPING\r\n'));
      socket.on('data', chunk => {
        buffer += chunk.toString('utf8');
        if (buffer.length > 8192 || buffer.includes('-ERR')) return finish(Error('NATS restricted reader readiness failed.'));
        if (buffer.split('\r\n').includes('PONG')) finish();
      });
    });
    if (signal.aborted) abort();
  });
}
async function waitForNative(ca, password, signal, probe = probeReader) {
  const deadline = Date.now() + 20000;
  while (Date.now() < deadline) {
    signal.throwIfAborted();
    try { await probe(ca, password, signal); return; }
    catch (error) {
      if (error.code !== 'ECONNREFUSED') throw error;
      await delay(100, undefined, { signal });
    }
  }
  throw Error('Owned NATS listener startup deadline expired.');
}
async function serviceConfiguration(environment, directory = '/data/COSMIC/SandboxNATS', binary = '/usr/local/bin/nats-server') {
  if (environment.OASIS_SANDBOX_NATS !== 'true' || environment.CQL_NATS_PUBLIC_SOURCE_USERNAME !== 'cql_reader')
    throw Error('The owned NATS runtime requires its explicit flag and reader.');
  const password = environment.CQL_NATS_PUBLIC_SOURCE_PASSWORD;
  serverConfiguration(directory, password); // Validate before any source placement.
  const bundle = captureBundle(environment.OASIS_SANDBOX_NATS_BOOTSTRAP_JSON, environment.CQL_NATS_PUBLIC_SOURCE_JSON);
  await prepare(bundle, directory);
  const ca = await prepareTls(directory, password);
  const nativeEnvironment = {};
  for (const key of ['PATH', 'HOME', 'TMPDIR']) if (environment[key]) nativeEnvironment[key] = environment[key];
  return { command: binary, args: ['-c', path.join(directory, 'server.conf')],
    cwd: directory, env: nativeEnvironment, check: signal => waitForNative(ca, password, signal) };
}

module.exports = { captureBundle, prepare, serverConfiguration, prepareTls, probeReader, waitForNative, serviceConfiguration };
