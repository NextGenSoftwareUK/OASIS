/* Native CSS provisioning uses its supported root-template initializer, not a replacement LDP server. */
'use strict';
const fs = require('node:fs/promises');
const path = require('node:path');
const crypto = require('node:crypto');
const { setTimeout: delay } = require('node:timers/promises');
const root = 'http://localhost:13000/';
const hash = bytes => crypto.createHash('sha256').update(bytes).digest('hex');
const fields = (value, names) => value && typeof value === 'object' && !Array.isArray(value) &&
  Object.keys(value).sort().join(',') === [...names].sort().join(',');
const acl = '@prefix acl: <http://www.w3.org/ns/auth/acl#>.\n' +
  '@prefix foaf: <http://xmlns.com/foaf/0.1/>.\n' +
  '<#public> a acl:Authorization; acl:agentClass foaf:Agent; acl:mode acl:Read; acl:accessTo <./>; acl:default <./>.\n';

function captureBundle(text) {
  if (typeof text !== 'string' || Buffer.byteLength(text) > 131072) throw Error('Bounded SOLID source bundle required.');
  const input = JSON.parse(text);
  if (!fields(input, ['schema', 'root', 'records']) || input.schema !== 1 || input.root !== root ||
      !Array.isArray(input.records) || input.records.length !== 3) throw Error('SOLID source requires the exact owned URI and three records.');
  const records = input.records.map(record => {
    if (!fields(record, ['id', 'sha256', 'turtleBase64']) || typeof record.id !== 'string' ||
        !/^[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}$/.test(record.id) ||
        record.id === '00000000-0000-0000-0000-000000000000' || typeof record.turtleBase64 !== 'string' ||
        typeof record.sha256 !== 'string' || !/^[a-f0-9]{64}$/.test(record.sha256)) throw Error('Invalid SOLID source record.');
    const bytes = Buffer.from(record.turtleBase64, 'base64');
    if (!bytes.length || bytes.length > 32768 || bytes.toString('base64') !== record.turtleBase64 || hash(bytes) !== record.sha256)
      throw Error('SOLID source record transfer differs.');
    // Strict UTF-8 decoding prevents a transfer from silently changing the encrypted envelope.
    new TextDecoder('utf-8', { fatal: true }).decode(bytes);
    return { id: record.id, sha256: record.sha256, turtleBase64: record.turtleBase64 };
  });
  if (new Set(records.map(record => record.id)).size !== records.length) throw Error('Duplicate SOLID resource.');
  const captured = { schema: 1, root, records };
  return { ...captured, digest: hash(JSON.stringify(captured)) };
}

async function exists(file) {
  try { await fs.lstat(file); return true; }
  catch (error) { if (error.code === 'ENOENT') return false; throw error; }
}

async function writeNew(file, value) {
  const handle = await fs.open(file, 'wx', 0o600);
  try { await handle.writeFile(value); await handle.sync(); } finally { await handle.close(); }
}

async function prepare({ bundle, directory, runtime = '/opt/solid' }) {
  const version = JSON.parse(await fs.readFile(path.join(runtime, 'package.json'), 'utf8')).version;
  if (version !== '7.1.8') throw Error('Pinned CSS version differs.');
  const ackPath = path.join(directory, 'bootstrap.acknowledged.json');
  const configPath = path.join(directory, 'source-config.json');
  const template = path.join(directory, 'template');
  const config = JSON.stringify({
    '@context': 'https://linkedsoftwaredependencies.org/bundles/npm/@solid/community-server/^7.0.0/components/context.jsonld',
    import: ['css:config/file-root.json'],
    '@graph': [{ '@id': 'urn:oasis:sandbox:root-template-override', '@type': 'Override',
      overrideInstance: { '@id': 'urn:solid-server:default:RootFolderGenerator' },
      overrideParameters: { '@type': 'StaticFolderGenerator', templateFolder: template } }]
  });
  let fresh = false;
  if (await exists(ackPath)) {
    const ack = JSON.parse(await fs.readFile(ackPath, 'utf8'));
    if (ack.schema !== 1 || ack.digest !== bundle.digest || ack.version !== version)
      throw Error('SOLID source acknowledgement differs; explicit migration required.');
  } else {
    if (await exists(directory)) throw Error('SOLID source exists without acknowledgement; reconcile before restarting.');
    await fs.mkdir(directory, { recursive: true, mode: 0o700 });
    // Persist before CSS can initialize its native storage. An interrupted start is never replayed automatically.
    await writeNew(path.join(directory, 'bootstrap.intent.json'), JSON.stringify({ schema: 1, digest: bundle.digest, version }));
    await fs.mkdir(path.join(template, 'base', 'oasis', 'holons'), { recursive: true, mode: 0o700 });
    await fs.mkdir(path.join(template, 'wac'), { mode: 0o700 });
    await writeNew(path.join(template, 'base', '.meta'), '<> a <http://www.w3.org/ns/pim/space#Storage>.\n');
    await writeNew(path.join(template, 'wac', '.acl'), acl);
    for (const record of bundle.records)
      await writeNew(path.join(template, 'base', 'oasis', 'holons', record.id + '.ttl'), Buffer.from(record.turtleBase64, 'base64'));
    await writeNew(configPath, config);
    fresh = true;
  }
  if (await fs.readFile(configPath, 'utf8') !== config || await fs.readFile(path.join(template, 'wac', '.acl'), 'utf8') !== acl)
    throw Error('SOLID provisioning configuration differs.');
  for (const record of bundle.records)
    if (hash(await fs.readFile(path.join(template, 'base', 'oasis', 'holons', record.id + '.ttl'))) !== record.sha256)
      throw Error('SOLID retained template differs.');
  const env = {};
  for (const key of ['PATH', 'HOME', 'USERPROFILE', 'SystemRoot', 'SYSTEMROOT', 'TEMP', 'TMP'])
    if (process.env[key]) env[key] = process.env[key];
  return { command: process.execPath, args: [path.join(runtime, 'bin', 'server.js'), '-c', configPath,
    '-f', path.join(directory, 'data'), '-b', root, '-p', '13000', '-l', 'warn'], env, cwd: runtime,
    fresh, ackPath, version, bundle };
}

function sameEnvelope(Parser, expected, actual, uri) {
  const triples = turtle => new Parser({ format: 'text/turtle', baseIRI: uri }).parse(turtle).map(quad =>
    JSON.stringify([quad.subject, quad.predicate, quad.object, quad.graph])).sort();
  const left = triples(expected); const right = triples(actual);
  return left.length === 2 && JSON.stringify(left) === JSON.stringify(right);
}

async function verify(source, signal, Parser, probe = fetch) {
  const options = () => ({ redirect: 'error', signal: AbortSignal.any([signal, AbortSignal.timeout(2000)]), headers: { Accept: 'text/turtle' } });
  const deadline = Date.now() + 20000;
  let rootResponse;
  // Startup readiness only. No native write or query is retried after the listener responds.
  while (Date.now() < deadline) {
    signal.throwIfAborted();
    try { rootResponse = await probe(root, options()); break; }
    catch { signal.throwIfAborted(); await delay(100, undefined, { signal }); }
  }
  if (!rootResponse?.ok) throw Error('Owned SOLID listener did not become ready.');
  const permissions = rootResponse.headers.get('WAC-Allow');
  await rootResponse.body?.cancel();
  if (!permissions || !/\bread\b/i.test(permissions) || /\b(write|append|control)\b/i.test(permissions))
    throw Error('SOLID public source permissions are not read-only.');
  for (const record of source.bundle.records) {
    const uri = root + 'oasis/holons/' + record.id + '.ttl';
    const response = await probe(uri, options());
    if (!response.ok) throw Error('SOLID native source record missing.');
    const bytes = [];
    let length = 0;
    for await (const chunk of response.body) {
      length += chunk.length;
      if (length > 65536) throw Error('SOLID native source response exceeds its budget.');
      bytes.push(chunk);
    }
    if (!sameEnvelope(Parser, Buffer.from(record.turtleBase64, 'base64').toString('utf8'), Buffer.concat(bytes).toString('utf8'), uri))
      throw Error('SOLID native encrypted envelope differs.');
  }
  signal.throwIfAborted();
  if (source.fresh) await writeNew(source.ackPath, JSON.stringify({ schema: 1, digest: source.bundle.digest, version: source.version }));
}

module.exports = { captureBundle, prepare, verify, sameEnvelope };
