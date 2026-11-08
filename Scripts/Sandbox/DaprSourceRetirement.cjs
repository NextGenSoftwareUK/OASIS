'use strict';
const fs = require('node:fs/promises');
const path = require('node:path');
const crypto = require('node:crypto');
const net = require('node:net');
const hash = value => crypto.createHash('sha256').update(value).digest('hex');
const sourceRoot = '/data/COSMIC/SandboxDapr';
const archiveBase = '/data/COSMIC/RetiredDaprSources';

async function snapshot(directory) {
  const queue = [directory], rows = []; let entries = 0, bytes = 0;
  while (queue.length) {
    const current = queue.shift();
    if (await fs.realpath(current) !== current) throw Error('Retirement source is redirected.');
    for (const entry of await fs.readdir(current, { withFileTypes: true })) {
      if (++entries > 256 || entry.isSymbolicLink()) throw Error('Retirement inventory boundary differs.');
      const file = path.join(current, entry.name);
      if (entry.isDirectory()) queue.push(file);
      else if (entry.isFile()) {
        const size = (await fs.stat(file)).size; bytes += size;
        if (bytes > 64 * 1024 * 1024) throw Error('Retirement inventory exceeds budget.');
        rows.push({ name: path.relative(directory, file), bytes: size, sha256: hash(await fs.readFile(file)) });
      } else throw Error('Unexpected retirement source entry.');
    }
  }
  if (!rows.length || !bytes) throw Error('An existing original source is required.');
  return rows.sort((a, b) => a.name.localeCompare(b.name));
}

function requireUnadmitted(environment) {
  if (environment.OASIS_SANDBOX_DAPR && environment.OASIS_SANDBOX_DAPR !== 'false')
    throw Error('Dapr reader must remain disabled.');
  if (Object.keys(environment).some(key => key.startsWith('CQL_DAPR_QUERY_')))
    throw Error('An enrolled Dapr source cannot be retired by this teaching operation.');
  const grants = JSON.parse(environment.CQL_PUBLIC_READ_CATALOGUES_JSON || '{}');
  const admission = JSON.parse(environment.PREPARED_STORAGE_ADMISSION_JSON || '{}');
  const examples = JSON.parse(environment.CQL_HOSTED_QUERY_EXAMPLES_JSON || '{}');
  if (Object.hasOwn(grants, 'DaprOASIS') || Object.hasOwn(examples, 'DaprOASIS') || environment.CQL_HOSTED_QUERY_EXAMPLES__DaprOASIS ||
      ['ordinaryProviders', 'queryOnlyProviders'].some(key => (admission[key] || []).includes('DaprOASIS')) ||
      ['nativeProviders', 'queryTariffs'].some(key => (admission[key] || []).some(row => row.provider === 'DaprOASIS')))
    throw Error('Dapr must never have entered the current teaching admission.');
}

function reviewRetirement(plan, seed, inspection, store, expectedStoreHash) {
  if (!/^[a-f0-9]{64}$/.test(expectedStoreHash || '') || hash(JSON.stringify(store)) !== expectedStoreHash ||
      inspection?.schema !== 1 || inspection.prefix !== plan.prefix || inspection.originalOperation !== seed.operation ||
      inspection.businessWritesAllowed !== false || JSON.stringify(inspection.storage) !== JSON.stringify(store))
    throw Error('Exact failed original inspection and unchanged AOF are required.');
  return { schema: 1, originalOperation: seed.operation, sourceOwner: plan.prefix + '0',
    originalStorageSha256: expectedStoreHash, originalSaveResponseAcknowledged: false,
    originalSaveOutcome: 'unknown', priorGenerationAdmitted: false, businessWrites: 0 };
}

async function privateJson(file) {
  const stat = await fs.lstat(file);
  if (!stat.isFile() || stat.isSymbolicLink() || stat.size > 32768 || (stat.mode & 0o077))
    throw Error('Private original journal differs.');
  return JSON.parse(await fs.readFile(file, 'utf8'));
}

async function mustBeAbsent(file) {
  try { await fs.lstat(file); } catch (error) { if (error.code === 'ENOENT') return; throw error; }
  throw Error('Acknowledged or previously retired generation cannot be retired again.');
}

async function syncDirectory(directory) {
  const handle = await fs.open(directory, 'r');
  try { await handle.sync(); } finally { await handle.close(); }
}

async function archiveSource(directory, destination, proof, durableExclusive) {
  if (process.platform !== 'linux' || !path.isAbsolute(directory) || !path.isAbsolute(destination) ||
      await fs.realpath(directory) !== directory || await fs.realpath(path.dirname(destination)) !== path.dirname(destination))
    throw Error('Canonical existing Linux source and archive parent required.');
  const before = await snapshot(directory);
  await fs.mkdir(destination, { mode: 0o700 });
  const intent = { ...proof, sourceFiles: before, sourceFilesSha256: hash(JSON.stringify(before)) };
  await durableExclusive(path.join(destination, 'retirement.intent.json'), intent);
  const retained = path.join(destination, 'source');
  await fs.rename(directory, retained);
  await syncDirectory(path.dirname(directory)); await syncDirectory(destination);
  const after = await snapshot(retained);
  if (JSON.stringify(before) !== JSON.stringify(after)) throw Error('Retained source bytes differ; never create a replacement.');
  const receipt = { ...proof, retiredPath: retained, files: after.length, sourceFilesSha256: intent.sourceFilesSha256 };
  await durableExclusive(path.join(destination, 'retirement.acknowledged.json'), receipt);
  return receipt;
}

async function main() {
  if (process.platform !== 'linux' || process.argv.length !== 4) throw Error('Exact Linux deployment/instance retirement required.');
  let input = '';
  for await (const chunk of process.stdin) { input += chunk.toString('utf8'); if (Buffer.byteLength(input) > 16384) throw Error('Retirement input exceeds budget.'); }
  const value = JSON.parse(input), text = Buffer.from(value.planBase64, 'base64').toString('utf8');
  for (const [file, expected] of Object.entries(value.implementation))
    if (hash(await fs.readFile('/app/' + file)) !== expected) throw Error('Reviewed runtime implementation differs.');
  const helper = require('/app/provision-sandbox-dapr.cjs');
  const { durableExclusive } = require('/app/DaprRuntime/seed.cjs');
  const config = require('/app/DaprRuntime/configuration.cjs');
  const plan = helper.reviewCommand(process.argv.slice(2), process.env, text);
  requireUnadmitted(process.env);
  if (hash(text) !== value.planSha256 || await fs.realpath(sourceRoot) !== sourceRoot)
    throw Error('Original plan/source ownership differs.');
  for (const name of ['seed.acknowledged.json', 'source.acknowledged.json', 'seed.reconciled.json', 'inspection/inspection.acknowledged.json'])
    await mustBeAbsent(path.join(sourceRoot, name));
  const creation = await privateJson(path.join(sourceRoot, 'creation.intent.json'));
  const seed = await privateJson(path.join(sourceRoot, 'seed.intent.json'));
  helper.pendingSourceDescriptor(plan, creation, seed);
  if (seed.operation !== value.originalOperation) throw Error('Reviewed original operation differs.');
  const inspection = await privateJson(path.join(sourceRoot, 'inspection/inspection.intent.json'));
  const correction = await privateJson(path.join(sourceRoot, 'inspection-device-correction/correction.intent.json'));
  if (correction.originalOperation !== seed.operation || correction.originalStorageSha256 !== value.storeSha256)
    throw Error('Original failed device correction differs.');
  const store = await snapshot(path.join(sourceRoot, 'store'));
  const proof = reviewRetirement(plan, seed, inspection, store, value.storeSha256);
  for (const port of Object.values(config.contract.ports)) {
    const listener = net.createServer();
    await new Promise((resolve, reject) => { listener.once('error', reject); listener.listen(port, '127.0.0.1', resolve); });
    await new Promise((resolve, reject) => listener.close(error => error ? reject(error) : resolve()));
  }
  for (const pid of (await fs.readdir('/proc')).filter(name => /^\d+$/.test(name))) {
    let cwd;
    try { cwd = await fs.readlink('/proc/' + pid + '/cwd'); }
    catch (error) { if (['ENOENT', 'ESRCH', 'EACCES'].includes(error.code)) continue; throw error; }
    if (cwd === sourceRoot || cwd.startsWith(sourceRoot + '/')) throw Error('Original source still has an owned process.');
  }
  if (!/^[a-f0-9]{8}-(?:[a-f0-9]{4}-){3}[a-f0-9]{12}$/.test(seed.operation)) throw Error('Original operation required.');
  try { await fs.mkdir(archiveBase, { mode: 0o700 }); }
  catch (error) { if (error.code !== 'EEXIST') throw error; }
  if (await fs.realpath(archiveBase) !== archiveBase || (await fs.stat(archiveBase)).mode & 0o077)
    throw Error('Private canonical archive parent required.');
  const receipt = await archiveSource(sourceRoot, path.join(archiveBase, seed.operation), proof, durableExclusive);
  process.stdout.write('COSMIC_DAPR_RETIREMENT ' + JSON.stringify({ ...receipt,
    deployment: process.env.RAILWAY_DEPLOYMENT_ID, instance: process.env.RAILWAY_REPLICA_ID }) + '\n');
}

module.exports = { snapshot, requireUnadmitted, reviewRetirement, archiveSource };
if (process.argv[1] === 'retire-hosted-dapr') main().catch(error => { console.error(error.message); process.exitCode = 1; });
