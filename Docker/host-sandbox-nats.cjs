'use strict';
const fs = require('node:fs/promises');
const path = require('node:path');
const crypto = require('node:crypto');

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

module.exports = { captureBundle, prepare };
