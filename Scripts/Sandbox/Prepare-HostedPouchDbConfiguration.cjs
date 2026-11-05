'use strict';

const fs = require('node:fs/promises');
const { createHash } = require('node:crypto');
const { captureBundle } = require('../../Docker/host-sandbox-pouchdb.cjs');

const provider = 'PouchDBOASIS';
const sourceKeys = ['OASIS_SANDBOX_POUCHDB', 'OASIS_SANDBOX_POUCHDB_BOOTSTRAP_JSON',
  'CQL_POUCHDB_QUERY_SOURCE_DIGEST', 'CQL_HOSTED_QUERY_EXAMPLES__PouchDBOASIS'];
const admissionKey = 'PREPARED_STORAGE_ADMISSION_JSON';
const catalogueKey = 'CQL_PUBLIC_READ_CATALOGUES_JSON';

function prepare(before, bundleText) {
  const bundle = captureBundle(bundleText);
  if (JSON.parse(before.OASIS_DNA_JSON)?.OASIS?.Sandbox?.Enabled !== true)
    throw Error('The existing WEB4 sandbox must be enabled in DNA.');
  if (sourceKeys.some(key => Object.hasOwn(before, key)))
    throw Error('Existing PouchDB source settings or presets require reconciliation.');

  const admission = JSON.parse(before[admissionKey]);
  const catalogue = JSON.parse(before[catalogueKey] ?? '{}');
  const examples = JSON.parse(before.CQL_HOSTED_QUERY_EXAMPLES_JSON ?? '{}');
  if (admission.queryOnlyProviders.includes(provider) || admission.ordinaryProviders.includes(provider) ||
      [...admission.nativeProviders, ...admission.queryTariffs].some(row => row.provider === provider) ||
      Object.hasOwn(catalogue, provider) || Object.hasOwn(examples, provider))
    throw Error('Existing PouchDB admission, grants or examples require reconciliation.');
  const reference = admission.queryTariffs.filter(row => row.provider === 'IPFSOASIS');
  if (reference.length !== 1 || reference[0].costUsd !== 0 || !reference[0].catalogueVersion)
    throw Error('One existing free IPFS query tariff is required.');

  const ids = bundle.records.map(row => row.id);
  const presets = [];
  for (const count of [3, 2]) {
    const suffix = count === 3 ? 'records' : 'subset';
    const selection = { orderBy: 'None', take: 25, includeMetadata: true,
      filters: [{ field: 'Id', scalar: 'Guid', operator: 'In', value: ids.slice(0, count) }] };
    presets.push({ id: 'public-' + suffix, label: 'PouchDB public teaching records (' + count + ')',
      request: { provider, maximumDurationMilliseconds: 30000, ...selection } });
    presets.push({ id: 'mongo-public-' + suffix, label: 'PouchDB + my private Mongo snapshots',
      operation: 'Federation', request: { scope: 'Named', providers: ['MongoDBOASIS', provider],
        source: selection, identityMode: 'PreserveCopies', maximumProviderAttempts: 2,
        maximumDurationMilliseconds: 30000, take: 25 } });
  }
  admission.queryOnlyProviders.push(provider);
  admission.queryTariffs.push({ ...reference[0], provider });
  catalogue[provider] = ids;
  const patch = {
    [sourceKeys[0]]: 'true',
    [sourceKeys[1]]: bundleText,
    [sourceKeys[2]]: bundle.digest,
    [sourceKeys[3]]: JSON.stringify(presets),
    [admissionKey]: JSON.stringify(admission),
    [catalogueKey]: JSON.stringify(catalogue)
  };
  for (const [key, value] of Object.entries(patch))
    if (Buffer.byteLength(value) > (key === sourceKeys[1] ? 131072 : 32768))
      throw Error('Variable byte budget exceeded: ' + key);
  return patch;
}

async function main() {
  const [baseline, source, qualification, output] = process.argv.slice(2);
  if (process.argv.length !== 6)
    throw Error('Supply a reviewed settings snapshot, source bundle, native qualification receipt and new output path.');
  const before = JSON.parse(await fs.readFile(baseline, 'utf8'));
  const bundleBytes = await fs.readFile(source);
  const proof = JSON.parse(await fs.readFile(qualification, 'utf8'));
  if (createHash('sha256').update(bundleBytes).digest('hex') !== proof.sourceBundleSha256?.toLowerCase() ||
      proof.proofs?.length !== 2)
    throw Error('Source bundle differs from the native qualification receipt.');
  const patch = prepare(before, new TextDecoder('utf-8', { fatal: true }).decode(bundleBytes));
  const file = await fs.open(output, 'wx', 0o600);
  try {
    await file.writeFile(JSON.stringify(patch));
    await file.sync();
  } finally {
    await file.close();
  }
}

module.exports = { prepare };
if (require.main === module) main().catch(error => {
  console.error(error instanceof SyntaxError ? 'Invalid JSON in the supplied settings or source.' : error.message);
  process.exitCode = 1;
});
