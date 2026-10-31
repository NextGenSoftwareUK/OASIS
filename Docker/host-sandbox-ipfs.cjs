/* Explicit sandbox deployment provisioning and process ownership. No business-write retries. */
'use strict';
const fs = require('node:fs/promises');
const path = require('node:path');
const crypto = require('node:crypto');
const { execFile, spawn } = require('node:child_process');
const { promisify } = require('node:util');
const { setTimeout: delay } = require('node:timers/promises');
const execute = promisify(execFile);
const sha256 = bytes => crypto.createHash('sha256').update(bytes).digest('hex');
const rootKeys = (value, expected) => value && typeof value === 'object' && !Array.isArray(value) &&
  Object.keys(value).sort().join(',') === [...expected].sort().join(',');

function captureBundle(text) {
  if (typeof text !== 'string' || Buffer.byteLength(text) > 131072) throw Error('IPFS bootstrap bundle is missing or too large.');
  const value = JSON.parse(text);
  if (!rootKeys(value, ['schema', 'catalogueCid', 'blocks']) || value.schema !== 1 ||
      !Array.isArray(value.blocks) || value.blocks.length < 2 || value.blocks.length > 9) throw Error('Invalid IPFS bootstrap bundle.');
  const blocks = value.blocks.map(block => {
    if (!rootKeys(block, ['cid', 'sha256', 'carBase64']) || typeof block.cid !== 'string' ||
        !/^[A-Za-z0-9]{40,128}$/.test(block.cid) || typeof block.carBase64 !== 'string' ||
        !/^[a-f0-9]{64}$/.test(block.sha256)) throw Error('Invalid IPFS bootstrap block.');
    const bytes = Buffer.from(block.carBase64, 'base64');
    if (!bytes.length || bytes.length > 32768 || bytes.toString('base64') !== block.carBase64 || sha256(bytes) !== block.sha256)
      throw Error('IPFS bootstrap block hash or encoding differs.');
    return { cid: block.cid, sha256: block.sha256, carBase64: block.carBase64 };
  });
  if (new Set(blocks.map(block => block.cid)).size !== blocks.length || value.catalogueCid !== blocks[0].cid)
    throw Error('IPFS bootstrap source identity differs.');
  const captured = { schema: 1, catalogueCid: value.catalogueCid, blocks };
  return { ...captured, digest: sha256(JSON.stringify(captured)) };
}

async function exists(file) {
  try { await fs.lstat(file); return true; }
  catch (error) { if (error.code === 'ENOENT') return false; throw error; }
}

function nativeEnvironment(repo) {
  const result = { IPFS_PATH: repo, IPFS_LOGGING: 'error' };
  for (const key of ['PATH', 'HOME', 'USERPROFILE', 'SystemRoot', 'SYSTEMROOT', 'TEMP', 'TMP'])
    if (process.env[key]) result[key] = process.env[key];
  return result; // The daemon/CLI do not need WEB4 credentials or the record encryption key.
}

async function bootstrap({ bundle, directory, binary }) {
  const repo = path.join(directory, 'repo');
  async function cli(...args) {
    try { return (await execute(binary, args, { env: nativeEnvironment(repo), windowsHide: true,
      timeout: 20000, maxBuffer: 4194304 })).stdout; }
    catch (error) { throw new Error(`IPFS bootstrap native ${args[0]} failed; preserve the original intent.`, { cause: error }); }
  }
  const version = JSON.parse(await cli('version', '--enc=json'));
  if (version.Version !== '0.43.1') throw Error('Pinned IPFS runtime version differs.');
  const ackPath = path.join(directory, 'bootstrap.acknowledged.json');
  if (await exists(ackPath)) {
    const ack = JSON.parse(await fs.readFile(ackPath, 'utf8'));
    if (ack.schema !== 1 || ack.digest !== bundle.digest || ack.version !== version.Version)
      throw Error('Existing IPFS bootstrap acknowledgement differs; explicit migration required.');
  } else {
    // A previous interruption is an unknown outcome. Never initialize over it or
    // replace its node identity, and never repeat its import automatically.
    if (await exists(directory)) throw Error('IPFS bootstrap state exists without acknowledgement; reconcile it before continuing.');
    await fs.mkdir(directory, { recursive: true, mode: 0o700 });
    const intent = { schema: 1, digest: bundle.digest, cids: bundle.blocks.map(block => block.cid), version: version.Version };
    const writeNew = async (file, value) => {
      const handle = await fs.open(file, 'wx', 0o600);
      try { await handle.writeFile(JSON.stringify(value)); await handle.sync(); } finally { await handle.close(); }
    };
    await writeNew(path.join(directory, 'bootstrap.intent.json'), intent);
    await cli('init', '--empty-repo', '--profile=server');
    const configPath = path.join(repo, 'config');
    const config = JSON.parse(await fs.readFile(configPath, 'utf8'));
    config.Addresses.API = '/ip4/127.0.0.1/tcp/5001';
    config.Addresses.Gateway = '';
    config.Addresses.Swarm = [];
    config.Bootstrap = [];
    config.Discovery.MDNS.Enabled = false;
    await fs.writeFile(configPath, JSON.stringify(config), { mode: 0o600 });
    for (const [index, block] of bundle.blocks.entries()) {
      const car = path.join(directory, `source-${index}.car`);
      await fs.writeFile(car, Buffer.from(block.carBase64, 'base64'), { flag: 'wx', mode: 0o600 });
      await cli('dag', 'import', '--pin-roots=true', car);
      await cli('cat', block.cid); // Offline native verification; no gateway/network substitute.
    }
    await writeNew(ackPath, intent);
  }
  const config = JSON.parse(await fs.readFile(path.join(repo, 'config'), 'utf8'));
  if (config.Addresses.API !== '/ip4/127.0.0.1/tcp/5001' || config.Addresses.Gateway !== '' || config.Addresses.Swarm.length)
    throw Error('Owned IPFS listener configuration differs.');
  for (const block of bundle.blocks) await cli('cat', block.cid);
  if (typeof config.Identity?.PeerID !== 'string' || !config.Identity.PeerID) throw Error('Owned IPFS node identity is missing.');
  return { repo, peerId: config.Identity.PeerID, nativeEnv: nativeEnvironment(repo) };
}

async function supervise({ launch = spawn, ready, signals = process, stopMilliseconds = 10000 }) {
  const children = [];
  let interrupted = false;
  let stop;
  let health;
  const healthAbort = new AbortController();
  const stopping = new Promise(resolve => { stop = resolve; });
  const signal = () => { interrupted = true; stop(); healthAbort.abort(); };
  signals.on('SIGTERM', signal); signals.on('SIGINT', signal);
  function child(command, args, options) {
    const process = launch(command, args, options);
    const owned = { process, finished: false };
    owned.done = new Promise(resolve => {
      const finish = code => { if (!owned.finished) { owned.finished = true; resolve(code); } };
      process.once('error', () => finish(1));
      process.once('exit', code => finish(code ?? 1));
    });
    children.push(owned);
    return owned;
  }
  try {
    const native = child('/usr/local/bin/ipfs', ['daemon', '--offline', '--migrate=false'], { stdio: 'inherit', env: ready.env });
    health = ready.check(healthAbort.signal);
    const started = await Promise.race([health.then(() => 'ready'), native.done.then(() => 'exit'), stopping.then(() => 'stop')]);
    if (started !== 'ready') return interrupted ? 0 : 1;
    const api = child('dotnet', ['NextGenSoftware.OASIS.API.ONODE.WebAPI.dll'], { stdio: 'inherit' });
    const outcome = await Promise.race([api.done.then(code => ({ code })), native.done.then(() => ({ code: 1 })), stopping.then(() => ({ code: 0 }))]);
    return outcome.code;
  } finally {
    healthAbort.abort();
    // Startup rejection is owned by the race above; observe cancelled health
    // work before releasing the process owner, rather than detaching a poller.
    if (health) await Promise.allSettled([health]);
    signals.removeListener('SIGTERM', signal); signals.removeListener('SIGINT', signal);
    for (const owned of children) if (!owned.finished) owned.process.kill('SIGTERM');
    const all = Promise.all(children.map(owned => owned.done));
    let timer;
    await Promise.race([all, new Promise(resolve => { timer = setTimeout(resolve, stopMilliseconds); })]);
    clearTimeout(timer);
    for (const owned of children) if (!owned.finished) owned.process.kill('SIGKILL');
    // Do not leave an unobserved child after escalation.
    let terminalTimer;
    try { await Promise.race([all, new Promise((_, reject) => { terminalTimer = setTimeout(() => reject(Error('Owned IPFS/API process remains unconfirmed after stop.')), stopMilliseconds); })]); }
    finally { clearTimeout(terminalTimer); }
  }
}

async function waitForNative(signal, peerId, probe = fetch) {
  const deadline = Date.now() + 15000;
  // Bounded startup health polling only. No query, import or business save retry.
  while (Date.now() < deadline) {
    let response;
    signal.throwIfAborted();
    const options = { method: 'POST', redirect: 'error', signal: AbortSignal.any([signal, AbortSignal.timeout(1000)]) };
    try { response = await probe('http://127.0.0.1:5001/api/v0/version', options); }
    catch (error) { signal.throwIfAborted(); await delay(100, undefined, { signal }); continue; }
    if (!response.ok || (await response.json()).Version !== '0.43.1') throw Error('Owned native IPFS health check differs.');
    const identity = await probe('http://127.0.0.1:5001/api/v0/id', options);
    if (!identity.ok || (await identity.json()).ID !== peerId) throw Error('Native IPFS node identity differs; refusing another daemon.');
    return;
  }
  throw Error('Owned native IPFS readiness deadline expired.');
}

function assertSandbox(text) {
  let dna;
  try { dna = JSON.parse(text); }
  catch { throw Error('Sandbox DNA could not be parsed.'); }
  if (dna?.OASIS?.Sandbox?.Enabled !== true) throw Error('Owned IPFS demo runtime requires OASIS.Sandbox.Enabled.');
}

async function main() {
  assertSandbox(process.env.OASIS_DNA_JSON);
  const bundle = captureBundle(process.env.OASIS_SANDBOX_IPFS_BOOTSTRAP_JSON);
  const native = await bootstrap({ bundle, directory: '/data/COSMIC/SandboxIPFS', binary: '/usr/local/bin/ipfs' });
  return supervise({ ready: { env: native.nativeEnv, check: signal => waitForNative(signal, native.peerId) } });
}

module.exports = { captureBundle, bootstrap, supervise, waitForNative, assertSandbox };
if (require.main === module) main().then(code => { process.exitCode = code; }, error => { console.error(error.message); process.exitCode = 1; });
