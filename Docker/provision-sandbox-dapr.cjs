'use strict';
const fs = require('node:fs/promises');
const path = require('node:path');
const net = require('node:net');
const crypto = require('node:crypto');
const { spawn } = require('node:child_process');
const config = require('./DaprRuntime/configuration.cjs');
const { durableExclusive, seedOnce } = require('./DaprRuntime/seed.cjs');
const runtime = require('./host-sandbox-dapr.cjs');
const hash = value => crypto.createHash('sha256').update(value).digest('hex');

function review(plan) {
  if (plan?.schema !== 1 || typeof plan.prefix !== 'string' ||
      !/^[a-f0-9]{8}-(?:[a-f0-9]{4}-){3}[a-f0-9]{11}$/.test(plan.prefix) ||
      plan.prefix.replaceAll('-', '') === '0'.repeat(31)) throw Error('Explicit unique Dapr source plan required.');
  config.credential(plan.readerPassword); config.credential(plan.sidecarToken);
  if (plan.readerPassword === plan.sidecarToken) throw Error('Dapr reader and sidecar identities must differ.');
  return ['e', '1', '2', '3'].map((suffix, index) => ({
    id: plan.prefix + suffix,
    value: JSON.stringify({ id: plan.prefix + suffix, name: `Dapr public catalogue record ${index + 1}`,
      description: 'Owned native Dapr state for the WEB4 CQL demonstration.',
      createdByAvatarId: plan.prefix + '0', isPublic: true,
      deletedDate: suffix === '3' ? '2026-11-01T00:00:00Z' : '0001-01-01T00:00:00' })
  }));
}

function pendingSourceDescriptor(plan, creation, seed) {
  const records = review(plan);
  const payload = records.map(row => ({ key: 'oasis/holon/' + row.id.replaceAll('-', ''), value: row.value }));
  if (creation?.schema !== 1 || creation.prefix !== plan.prefix || creation.replayAllowed !== false ||
      seed?.prefix !== plan.prefix || seed.replayAllowed !== false ||
      !/^[a-f0-9]{8}-(?:[a-f0-9]{4}-){3}[a-f0-9]{12}$/.test(seed.operation || '') ||
      JSON.stringify(seed.keys) !== JSON.stringify(payload.map(row => row.key)) ||
      seed.payloadSha256 !== hash(JSON.stringify(payload))) throw Error('Original Dapr creation intent differs; recovery refused.');
  return records;
}

async function privateWrite(file, text, flag = 'wx') {
  const handle = await fs.open(file, flag, 0o600);
  try { await handle.writeFile(text); await handle.sync(); } finally { await handle.close(); }
}

async function storageSnapshot(directory) {
  const queue = [directory], rows = []; let entries = 0, bytes = 0;
  while (queue.length) {
    const current = queue.shift();
    if (await fs.realpath(current) !== current) throw Error('Original Dapr storage redirected.');
    for (const entry of await fs.readdir(current, { withFileTypes: true })) {
      if (++entries > 128 || entry.isSymbolicLink()) throw Error('Original Dapr storage boundary differs.');
      const file = path.join(current, entry.name);
      if (entry.isDirectory()) queue.push(file);
      else if (entry.isFile()) {
        const size = (await fs.stat(file)).size; bytes += size;
        if (bytes > 64 * 1024 * 1024) throw Error('Original Dapr storage exceeds inspection budget.');
        rows.push({ name: path.relative(directory, file), bytes: size, sha256: hash(await fs.readFile(file)) });
      } else throw Error('Unexpected original Dapr storage entry.');
    }
  }
  if (!rows.length || !bytes) throw Error('Original Dapr storage is absent; never reseed.');
  return rows.sort((a, b) => a.name.localeCompare(b.name));
}

async function inspectPendingSource(plan, directory = '/data/COSMIC/SandboxDapr', binaries = { garnet: '/opt/garnet/GarnetServer', daprd: '/opt/dapr/daprd' }) {
  review(plan);
  if (process.platform !== 'linux' || await fs.realpath(directory) !== directory) throw Error('Original Linux source required.');
  if (JSON.stringify(config.contract.linuxStorageArguments) !== JSON.stringify(['--device-type', 'Native', '--device-io-backend', 'Uring']))
    throw Error('Qualified explicit native io_uring recovery contract required.');
  async function original(name) {
    const file = path.join(directory, name), stat = await fs.lstat(file);
    if (!stat.isFile() || stat.isSymbolicLink() || stat.size > 32768 || (stat.mode & 0o077)) throw Error('Original private Dapr journal differs.');
    return JSON.parse(await fs.readFile(file, 'utf8'));
  }
  for (const name of ['seed.acknowledged.json', 'source.acknowledged.json']) {
    try { await fs.lstat(path.join(directory, name)); }
    catch (error) { if (error.code === 'ENOENT') continue; throw error; }
    throw Error('Dapr source already acknowledged; retain original evidence.');
  }
  const creation = await original('creation.intent.json'), seed = await original('seed.intent.json');
  const records = pendingSourceDescriptor(plan, creation, seed);
  const before = await storageSnapshot(path.join(directory, 'store'));
  await requireFreePorts();
  const inspection = path.join(directory, 'inspection');
  await fs.mkdir(inspection, { mode: 0o700 });
  await durableExclusive(path.join(inspection, 'inspection.intent.json'), { schema: 1, prefix: plan.prefix, originalOperation: seed.operation, storage: before, businessWritesAllowed: false });
  await fs.mkdir(path.join(inspection, 'components'), { mode: 0o700 });
  await privateWrite(path.join(inspection, 'reader.acl'), config.readerAcl(plan.readerPassword));
  await privateWrite(path.join(inspection, 'components/redis.yaml'), config.component(plan.readerPassword, 'reader'));
  let observations;
  for (const name of ['first', 'second']) {
    await phase(inspection, 'inspect-' + name, plan.sidecarToken, async (start, signal) => {
      await start(binaries.garnet, config.garnetArguments(directory, path.join(inspection, 'reader.acl'), 'recover'), s => runtime.waitForRedis(plan.readerPassword, s));
      await start(binaries.daprd, config.daprArguments(inspection), s => runtime.waitForSidecar(plan.sidecarToken, s));
      if (observations) await runtime.verifyRows({ records: observations }, plan.sidecarToken, signal);
      else observations = await runtime.observeRows(records, plan.sidecarToken, signal);
    });
    await requireFreePorts();
    if (JSON.stringify(await storageSnapshot(path.join(directory, 'store'))) !== JSON.stringify(before))
      throw Error('Original AOF bytes changed during recovery inspection; preserve evidence.');
  }
  const proof = { schema: 1, originalOperation: seed.operation, sourceOwner: plan.prefix + '0', records: observations,
    recoveries: 2, originalStorage: before, businessWrites: 0, originalSaveResponseAcknowledged: false };
  await durableExclusive(path.join(inspection, 'inspection.acknowledged.json'), proof);
  return proof;
}

async function requireFreePorts() {
  for (const port of Object.values(config.contract.ports)) {
    const listener = net.createServer();
    await new Promise((resolve, reject) => { listener.once('error', reject); listener.listen(port, '127.0.0.1', resolve); });
    await new Promise((resolve, reject) => listener.close(error => error ? reject(error) : resolve()));
  }
}

async function phase(directory, name, token, operation) {
  const children = []; const abort = new AbortController();
  async function start(command, args, ready) {
    const log = await fs.open(path.join(directory, `${name}-${children.length}.log`), 'wx', 0o600);
    let child;
    try { child = spawn(command, args, { cwd: directory, env: {
      PATH: process.env.PATH, HOME: process.env.HOME, DOTNET_ROOT: process.env.DOTNET_ROOT,
      DAPR_API_TOKEN: token }, stdio: ['ignore', log.fd, log.fd] }); }
    finally { await log.close(); }
    const owned = { child, stopping: false };
    owned.done = new Promise(resolve => {
      child.once('error', () => { if (!owned.stopping) abort.abort(Error('Dapr native process failed to start.')); });
      child.once('close', () => { if (!owned.stopping) abort.abort(Error('Dapr native process exited unexpectedly.')); resolve(); });
    });
    children.push(owned);
    await ready(abort.signal);
  }
  async function deadline(promise, milliseconds) {
    let timer;
    try { await Promise.race([promise, new Promise((_, reject) => { timer = setTimeout(() => reject(Error('Dapr native shutdown deadline expired.')), milliseconds); })]); }
    finally { clearTimeout(timer); }
  }
  try { return await operation(start, abort.signal); }
  finally {
    abort.abort(Error('Dapr native phase ended.'));
    const errors = [];
    for (const owned of children.reverse()) {
      owned.stopping = true; owned.child.kill('SIGTERM');
      try { await deadline(owned.done, 10000); }
      catch (error) {
        errors.push(error); owned.child.kill('SIGKILL');
        try { await deadline(owned.done, 5000); } catch (terminal) { errors.push(terminal); }
      }
    }
    if (errors.length) throw new AggregateError(errors, 'Dapr owned native shutdown failed.');
  }
}

async function provision(plan, directory = '/data/COSMIC/SandboxDapr', binaries = { garnet: '/opt/garnet/GarnetServer', daprd: '/opt/dapr/daprd' }) {
  const records = review(plan);
  if (process.platform !== 'linux') throw Error('Dapr hosted source provisioning requires Linux.');
  if (!path.isAbsolute(directory) || await fs.realpath(path.dirname(directory)) !== path.dirname(directory))
    throw Error('Dapr source parent must exist without redirection.');
  for (const binary of [binaries.garnet, binaries.daprd]) {
    if (typeof binary !== 'string' || !path.isAbsolute(binary) || await fs.realpath(binary) !== binary || !(await fs.stat(binary)).isFile())
      throw Error('Exact native Dapr runtime executable required.');
    await fs.access(binary, 1);
  }
  await requireFreePorts();
  // Exclusive source ownership: an interrupted or existing source is never reseeded.
  await fs.mkdir(directory, { mode: 0o700 });
  await durableExclusive(path.join(directory, 'creation.intent.json'), { schema: 1, prefix: plan.prefix, replayAllowed: false });
  await fs.mkdir(path.join(directory, 'store'), { mode: 0o700 });
  await fs.mkdir(path.join(directory, 'components'), { mode: 0o700 });
  const admin = crypto.randomBytes(32).toString('hex');
  const writerAcl = path.join(directory, 'writer.acl');
  const component = path.join(directory, 'components/redis.yaml');
  await privateWrite(writerAcl, config.writerAcl(admin, plan.readerPassword));
  await privateWrite(component, config.component(admin, 'default'));
  const observations = await phase(directory, 'provision', plan.sidecarToken, async (start, signal) => {
    await start(binaries.garnet, config.garnetArguments(directory, writerAcl, 'provision'), s => runtime.waitForRedis(plan.readerPassword, s));
    await start(binaries.daprd, config.daprArguments(directory), s => runtime.waitForSidecar(plan.sidecarToken, s));
    await seedOnce(directory, plan.prefix, records.map(row => ({ key: 'oasis/holon/' + row.id.replaceAll('-', ''), value: row.value })), plan.sidecarToken, signal);
    return runtime.observeRows(records, plan.sidecarToken, signal);
  });
  const aclText = config.readerAcl(plan.readerPassword), componentText = config.component(plan.readerPassword, 'reader');
  await privateWrite(path.join(directory, 'reader-password'), plan.readerPassword);
  await privateWrite(path.join(directory, 'reader.acl'), aclText);
  await privateWrite(component, componentText, 'w');
  await fs.unlink(writerAcl); // Both owned provisioning processes have stopped.
  const source = { schema: 1, sourceOwner: plan.prefix + '0', ids: records.slice(0, 3).map(row => row.id),
    sidecarTokenSha256: hash(plan.sidecarToken), records: observations,
    configurationSha256: { 'reader.acl': hash(aclText), 'components/redis.yaml': hash(componentText) } };
  await phase(directory, 'recovery', plan.sidecarToken, async (start, signal) => {
    await start(binaries.garnet, config.garnetArguments(directory, path.join(directory, 'reader.acl'), 'recover'), s => runtime.waitForRedis(plan.readerPassword, s));
    await start(binaries.daprd, config.daprArguments(directory), s => runtime.waitForSidecar(plan.sidecarToken, s));
    await runtime.verifyRows(source, plan.sidecarToken, signal);
    const denied = await fetch(`http://127.0.0.1:${config.contract.ports.http}/v1.0/state/${config.contract.store}`, {
      method: 'POST', headers: { 'content-type': 'application/json', 'dapr-api-token': plan.sidecarToken },
      body: JSON.stringify([{ key: 'oasis/holon/' + records[0].id.replaceAll('-', ''), value: 'changed' }]),
      redirect: 'error', signal: AbortSignal.any([signal, AbortSignal.timeout(3000)])
    });
    let text = '';
    for await (const chunk of denied.body) {
      text += Buffer.from(chunk).toString('utf8');
      if (Buffer.byteLength(text) > 8192) throw Error('Dapr denied-write response exceeds its budget.');
    }
    if (denied.status !== 500 || !text.includes('NOPERM')) throw Error('Dapr reader mutation was not denied.');
    await runtime.verifyRows(source, plan.sidecarToken, signal);
  });
  await requireFreePorts();
  await durableExclusive(path.join(directory, 'source.acknowledged.json'), source);
  return { schema: 1, sourceOwner: source.sourceOwner, ids: source.ids, records: source.records.length, recoveryVerified: true };
}
function reviewCommand(args, environment, text) {
  const guid = /^[a-f0-9]{8}-(?:[a-f0-9]{4}-){3}[a-f0-9]{12}$/;
  if (args.length !== 2 || args.some(value => !guid.test(value)) ||
      environment.RAILWAY_DEPLOYMENT_ID !== args[0] || environment.RAILWAY_REPLICA_ID !== args[1])
    throw Error('Exact reviewed deployment and replica required for Dapr creation.');
  require('./host-sandbox-ipfs.cjs').assertSandbox(environment.OASIS_DNA_JSON);
  if (environment.OASIS_SANDBOX_DAPR !== undefined && environment.OASIS_SANDBOX_DAPR !== 'false')
    throw Error('Dapr readers must remain disabled during source creation.');
  if (Buffer.byteLength(text) > 8192) throw Error('Dapr creation plan exceeds its budget.');
  const plan = JSON.parse(text); review(plan);
  return plan;
}
module.exports = { review, provision, reviewCommand, pendingSourceDescriptor, inspectPendingSource };
if (require.main === module) (async () => {
  let text = '';
  for await (const chunk of process.stdin) {
    text += chunk.toString('utf8');
    if (Buffer.byteLength(text) > 8192) throw Error('Dapr creation plan exceeds its budget.');
  }
  const plan = reviewCommand(process.argv.slice(2), process.env, text);
  const receipt = await provision(plan);
  process.stdout.write('COSMIC_DAPR_PROOF ' + JSON.stringify({ ...receipt,
    planSha256: hash(text), deployment: process.env.RAILWAY_DEPLOYMENT_ID, instance: process.env.RAILWAY_REPLICA_ID }) + '\n');
})().catch(error => { console.error(error.message); process.exitCode = 1; });
