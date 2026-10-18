// Real LOCAL-only SDK pipeline with create-only/fsync owner. Not hosted avatar-wallet/Mongo/API proof.
import assert from 'node:assert/strict';
import { ApiPromise } from '@polkadot/api';
import { ReadOnlyHttpProvider } from './read-only-provider.mjs';
import { prepareAssetOperation, verifyAssetPreparation } from './asset-operation.mjs';
import { readOriginalAssetOutcome } from './asset-outcome.mjs';
import { submitOriginalPreparation, originalSubmission } from './original-submission.mjs';
import { openSync, writeSync, fsyncSync, closeSync, existsSync, readFileSync, unlinkSync } from 'node:fs';
import { resolve, dirname } from 'node:path';

if (process.env.OASIS_POLKADOT_LOCAL_NATIVE !== '1' || !process.env.OASIS_POLKADOT_LOCAL_ASSET_MUTATION_JOURNAL)
  throw new Error('Explicit LOCAL-only opt-in and fixed journal required.');
const journal = resolve(process.env.OASIS_POLKADOT_LOCAL_ASSET_MUTATION_JOURNAL);
if (dirname(journal) !== resolve('..', '..', '..', '..', 'TestResults', 'PolkadotCurrent') || existsSync(journal))
  throw new Error('Use existing evidence directory; preserve/reconcile any pending original, never replay.');
const durable = (path, value, mode = 'wx') => { const fd = openSync(path, mode, 0o600);
  try { writeSync(fd, JSON.stringify(value)); fsyncSync(fd); } finally { closeSync(fd); } };
const api = await ApiPromise.create({ provider: new ReadOnlyHttpProvider('http://127.0.0.1:19944'), noInitWarn: true });
try {
  assert.equal(api.genesisHash.toHex(), '0xe8cf5d7d636a08721efaa17acc52ed3d895fe2468dccb2520cc8e249ddb6b208');
  const actor = '0x1a4fee48c1ba1a48e8cd43782a8485d635aa91cfb82cbb477f0c1c576bc4031c';
  for (const [action, amount, units] of [['mint', '1.250001', '1250001'], ['burn', '0.500001', '500001']]) {
    const before = BigInt((await api.query.assets.account('424242', actor)).unwrap().balance.toString());
    const supplyBefore = BigInt((await api.query.assets.asset('424242')).unwrap().supply.toString());
    const original = await prepareAssetOperation(api, { action, amount, assetId: '424242', sender: actor, recipient: actor,
      privateKey: '0x' + '02'.repeat(32) }); // Public LOCAL-only fixture key, never public funds.
    assert.equal(original.units, units);
    let persisted = 0;
    const accepted = await submitOriginalPreparation(api, original, async evidence => {
      const verified = await verifyAssetPreparation(api, evidence);
      assert.equal(verified.action, action);
      assert.equal(verified.assetId, '424242');
      durable(journal, evidence);
      persisted++;
    }, originalSubmission('http://127.0.0.1:19944', original.signedTransactionBase64), verifyAssetPreparation);
    assert.equal(persisted, 1);
    assert.equal(accepted.finalized, false);
    const deadline = Date.now() + 30000;
    let receipt;
    while (Date.now() < deadline) {
      const outcome = await readOriginalAssetOutcome(api, { preparation: original, maximumBlocks: 16 });
      if (outcome.found) { receipt = outcome.receipt; break; }
      await new Promise(resolve => setTimeout(resolve, 500)); // Read-only polling, never submit twice.
    }
    assert.equal(receipt?.dispatchSucceeded, true, 'Retain pending original for reconciliation.');
    assert.equal(receipt.assetEffectMatchesRequest, true, 'Known ledger effect differs; reconcile, never replay.');
    assert.equal(receipt.observedUnits, units);
    const delta = action === 'mint' ? BigInt(units) : -BigInt(units);
    assert.equal(BigInt((await api.query.assets.account('424242', actor)).unwrap().balance.toString()) - before, delta);
    assert.equal(BigInt((await api.query.assets.asset('424242')).unwrap().supply.toString()) - supplyBefore, delta);
    assert.equal(JSON.parse(readFileSync(journal, 'utf8')).transactionHash, original.transactionHash);
    durable(journal + `.${action}.completed.json`, { original, receipt, balanceDelta: delta.toString() }, 'w');
    unlinkSync(journal); // Only exact confirmed original, now retained in flushed completed record.
    console.log(JSON.stringify({ action, transactionHash: original.transactionHash, observedUnits: units,
      exactFinalizedEffectAndSupply: true, feeCoverage: receipt.feeCoverage, publicFunds: false }));
  }
} finally { await api.disconnect(); }
