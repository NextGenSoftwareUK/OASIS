'use strict';
const fs = require('node:fs/promises');
const path = require('node:path');
const crypto = require('node:crypto');
const assert = require('node:assert/strict');
const helper = require('../../Docker/provision-sandbox-dapr.cjs');
const { contract } = require('../../Docker/DaprRuntime/configuration.cjs');

(async () => {
  if (process.platform !== 'linux' || process.argv.length !== 5) throw Error('Explicit Linux native executables and existing evidence root required.');
  const binaries = { garnet: await fs.realpath(process.argv[2]), daprd: await fs.realpath(process.argv[3]) };
  const evidenceRoot = await fs.realpath(process.argv[4]);
  const source = path.join(evidenceRoot, 'pending-' + crypto.randomUUID());
  const plan = { schema: 1, prefix: crypto.randomUUID().slice(0, -1),
    readerPassword: crypto.randomBytes(32).toString('hex'), sidecarToken: crypto.randomBytes(32).toString('hex') };
  const nativeFetch = globalThis.fetch;
  const endpoint = `http://127.0.0.1:${contract.ports.http}/v1.0/state/${contract.store}`;
  let saves = 0, lostResponseInjected = false, inspectionMutations = 0;
  // The real native service receives and acknowledges the only save. Only its
  // response delivery to the journal owner is fault-injected; storage is real.
  globalThis.fetch = async (url, options) => {
    if (options?.method === 'POST' && url === endpoint) {
      if (++saves !== 1) throw Error('Unexpected repeated save during failure fixture.');
      const response = await nativeFetch(url, options);
      assert.equal(response.status, 204);
      lostResponseInjected = true;
      throw Error('Controlled loss of original native save response');
    }
    return nativeFetch(url, options);
  };
  try {
    await assert.rejects(helper.provision(plan, source, binaries), /Controlled loss of original native save response/);
    assert.equal(lostResponseInjected, true);
    for (const name of ['seed.acknowledged.json', 'source.acknowledged.json'])
      await assert.rejects(fs.stat(path.join(source, name)), { code: 'ENOENT' });
    let inspectionReadFailureInjected = false;
    globalThis.fetch = (url, options) => {
      const method = options?.method ?? 'GET';
      if (method !== 'GET') { inspectionMutations++; throw Error('Recovery inspector attempted a mutation.'); }
      if (!inspectionReadFailureInjected && method === 'GET' && String(url).includes('/v1.0/state/')) {
        inspectionReadFailureInjected = true;
        throw Error('Controlled failure of first inspection read');
      }
      return nativeFetch(url, options);
    };
    await assert.rejects(helper.inspectPendingSource(plan, source, binaries), /Controlled failure of first inspection read/,
      'The first real inspection GET must reach the controlled read-failure boundary');
    assert.equal(inspectionReadFailureInjected, true);
    const priorIntent = await fs.readFile(path.join(source, 'inspection/inspection.intent.json'));
    const priorHash = crypto.createHash('sha256').update(priorIntent).digest('hex');
    const proof = await helper.inspectCorrectedDeviceSource(plan, priorHash, source, binaries);
    assert.deepEqual(await fs.readFile(path.join(source, 'inspection-device-correction/prior-inspection/inspection.intent.json')), priorIntent);
    for (const name of ['inspect-first-0.log', 'inspect-first-1.log'])
      assert.ok((await fs.stat(path.join(source, 'inspection-device-correction/prior-inspection', name))).isFile());
    const correction = JSON.parse(await fs.readFile(path.join(source, 'inspection-device-correction/correction.intent.json'), 'utf8'));
    assert.equal(correction.priorInspectionIntentSha256, priorHash);
    await assert.rejects(helper.inspectCorrectedDeviceSource(plan, priorHash, source, binaries), /Acknowledged/);
    assert.equal(proof.records.length, 4);
    assert.equal(proof.recoveries, 2);
    assert.equal(proof.businessWrites, 0);
    assert.equal(proof.originalSaveResponseAcknowledged, false);
    assert.deepEqual(proof.records.map(row => row.value), helper.review(plan).map(row => row.value));
    assert.equal(saves, 1); assert.equal(inspectionMutations, 0);
    for (const name of ['seed.acknowledged.json', 'source.acknowledged.json'])
      await assert.rejects(fs.stat(path.join(source, name)), { code: 'ENOENT' });
    const accepted = await helper.acknowledgeRecoveredSource(plan, source);
    assert.equal(accepted.reconciliation.originalSaveResponseAcknowledged, false);
    assert.equal(accepted.reconciliation.businessWrites, 0);
    const environment = { OASIS_SANDBOX_DAPR: 'true', CQL_DAPR_QUERY_STORE: contract.store,
      CQL_DAPR_QUERY_GRPC_ENDPOINT: `http://127.0.0.1:${contract.ports.grpc}/`,
      CQL_DAPR_QUERY_PROBE_ID: accepted.ids[0], CQL_DAPR_QUERY_SIDECAR_TOKEN: plan.sidecarToken,
      CQL_PUBLIC_READ_CATALOGUES_JSON: JSON.stringify({ DaprOASIS: accepted.ids }) };
    const acknowledged = await require('../../Docker/host-sandbox-dapr.cjs').readSource(environment, source);
    assert.equal(acknowledged.source.reconciliation.inspectionSha256, accepted.reconciliation.inspectionSha256);
    assert.equal(JSON.parse(await fs.readFile(path.join(source, 'seed.reconciled.json'), 'utf8')).outcome, 'applied');
    await assert.rejects(fs.stat(path.join(source, 'seed.acknowledged.json')), { code: 'ENOENT' });
    await assert.rejects(helper.acknowledgeRecoveredSource(plan, source), /already acknowledged/);
    const result = { schema: 1, nativeSaveRequests: saves, responseLossInjected: true,
      originalRecordsVerified: proof.records.length, recoveries: proof.recoveries,
      inspectionMutations, originalAofUnchanged: true, originalSaveResponseAcknowledged: false,
      fabricatedSaveAcknowledgement: false, recoveredSourceMetadataAccepted: true, nativeProcessesStopped: true,
      failedInspectionRetained: true, correctedDeviceInspectionVerified: true };
    const file = await fs.open(path.join(source, 'pending-recovery-native-proof.json'), 'wx', 0o600);
    try { await file.writeFile(JSON.stringify(result)); await file.sync(); } finally { await file.close(); }
    process.stdout.write(JSON.stringify(result) + '\n');
  } finally { globalThis.fetch = nativeFetch; }
})().catch(error => { console.error(error.message); process.exitCode = 1; });
