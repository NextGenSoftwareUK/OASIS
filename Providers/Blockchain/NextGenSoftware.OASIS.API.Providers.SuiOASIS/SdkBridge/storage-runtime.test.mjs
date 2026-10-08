import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync, writeFileSync, existsSync } from 'node:fs';
import { createHash, randomUUID } from 'node:crypto';
import { requestSuiFromFaucetV2 } from '@mysten/sui/faucet';
import { invoke } from './bridge.mjs';

const rpcEndpoint = process.env.OASIS_SUI_TEST_RPC;
const faucet = process.env.OASIS_SUI_TEST_FAUCET;
const compiledPath = process.env.OASIS_SUI_TEST_PACKAGE;
if (!rpcEndpoint || !faucet || !compiledPath) throw new Error('Real local RPC, faucet and compiled Move package settings are required');
for (const endpoint of [rpcEndpoint, faucet])
  if (!['127.0.0.1', 'localhost', '[::1]'].includes(new URL(endpoint).hostname)) throw new Error('Storage evidence must use a local chain');

test('published Move storage persists complete values, paginates and enforces real signer authorization', { timeout: 240000 }, async () => {
  const compiled = JSON.parse(readFileSync(compiledPath, 'utf8'));
  const digest = createHash('sha256').update(JSON.stringify(compiled)).digest('hex');
  const base = { rpcEndpoint, network: 'localnet' };
  base.chainId = (await invoke({ ...base, operation: 'probe' })).chainIdentifier;
  const cachePath = `${compiledPath}.deployment.json`;
  let deployment = existsSync(cachePath) ? JSON.parse(readFileSync(cachePath, 'utf8')) : null;
  if (!deployment || deployment.digest !== digest || deployment.chainId !== base.chainId) {
    const owner = await invoke({ operation: 'generateKey' });
    await requestSuiFromFaucetV2({ host: faucet, recipient: owner.address });
    const committed = await invoke({ ...base, operation: 'publish', privateKey: owner.privateKey,
      modules: compiled.modules, dependencies: compiled.dependencies });
    deployment = { ...committed, privateKey: owner.privateKey, digest, chainId: base.chainId };
    // Local-only synthetic signer, retained in the ignored fixed test directory for reuse.
    writeFileSync(cachePath, JSON.stringify(deployment));
  }
  Object.assign(base, deployment);
  const probe = await invoke({ ...base, operation: 'storageProbe' });
  assert.equal(probe.owner, (await invoke({ operation: 'restoreKey', privateKey: base.privateKey })).address);
  const prefix = `oasis/evidence/${randomUUID()}/`;
  const entries = Array.from({ length: 105 }, (_, i) => ({ key: `${prefix}${i}`, value: JSON.stringify({ id: i, detail: 'Complete Unicode record 🌳', karma: 123, xp: 456 }) }));
  try {
    assert.equal(await invoke({ ...base, operation: 'get', key: entries[0].key }), null);
    const committed = await invoke({ ...base, operation: 'putMany', entries });
    assert.equal((await invoke({ ...base, operation: 'transaction', transactionHash: committed.transactionHash })).status.success, true);
    assert.deepEqual(await invoke({ ...base, operation: 'list', prefix }), entries.map(entry => entry.key).sort());
    assert.equal(await invoke({ ...base, operation: 'get', key: entries[104].key }), entries[104].value);
    await invoke({ ...base, operation: 'put', key: entries[0].key, value: 'updated complete value' });
    assert.equal(await invoke({ ...base, operation: 'get', key: entries[0].key }), 'updated complete value');
    const outsider = await invoke({ operation: 'generateKey' });
    await requestSuiFromFaucetV2({ host: faucet, recipient: outsider.address });
    await assert.rejects(invoke({ ...base, operation: 'put', privateKey: outsider.privateKey,
      key: entries[0].key, value: 'unauthorized overwrite' }), /MoveAbort/);
    assert.equal(await invoke({ ...base, operation: 'get', key: entries[0].key }), 'updated complete value');
  } finally {
    await invoke({ ...base, operation: 'deleteMany', entries });
  }
  assert.deepEqual(await invoke({ ...base, operation: 'list', prefix }), []);
  assert.equal(await invoke({ ...base, operation: 'get', key: entries[0].key }), null);
  await invoke({ ...base, operation: 'delete', key: entries[0].key });
});
