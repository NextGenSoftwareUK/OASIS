// ONE durable local-only Assets fixture. No public chain, environment reset or per-run asset IDs.
import assert from 'node:assert/strict';
import { ApiPromise, HttpProvider, Keyring } from '@polkadot/api';
import { cryptoWaitReady } from '@polkadot/util-crypto';
import { existsSync, readFileSync, openSync, writeSync, fsyncSync, closeSync, unlinkSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { signOriginalCall } from './native-transfer.mjs';
import { verifySignedPreparation } from './verify-preparation.mjs';
import { originalSubmission } from './original-submission.mjs';
import { readVerifiedOriginalRange, readOriginalNativeFee } from './original-outcome.mjs';
import { readFinalizedReceipt } from './finalized-receipt.mjs';

if (process.env.OASIS_POLKADOT_LOCAL_NATIVE !== '1') throw new Error('Explicit LOCAL-only opt-in required.');
const journal = resolve(process.env.OASIS_POLKADOT_LOCAL_ASSET_JOURNAL || '');
if (!process.env.OASIS_POLKADOT_LOCAL_ASSET_JOURNAL || dirname(journal) !== resolve('..', '..', '..', '..', 'TestResults', 'PolkadotCurrent'))
  throw new Error('Use the fixed existing PolkadotCurrent evidence directory.');
if (existsSync(journal)) throw new Error('Pending asset fixture original exists: reconcile read-only, NEVER replay.');
const completed = journal + '.completed.json';
const genesis = '0xe8cf5d7d636a08721efaa17acc52ed3d895fe2468dccb2520cc8e249ddb6b208';
const assetId = '424242';
const actor = '0x1a4fee48c1ba1a48e8cd43782a8485d635aa91cfb82cbb477f0c1c576bc4031c';
const protocol = 'oasis.polkadot.local-asset-fixture.v1';
const durable = (path, value) => { const fd = openSync(path, 'wx', 0o600);
  try { writeSync(fd, JSON.stringify(value)); fsyncSync(fd); } finally { closeSync(fd); } };
const api = await ApiPromise.create({ provider: new HttpProvider('http://127.0.0.1:19944'), noInitWarn: true });
try {
  assert.equal(api.genesisHash.toHex(), genesis);
  await cryptoWaitReady();
  const alice = new Keyring({ type: 'sr25519' }).addFromUri('//Alice'); // Public local genesis identity only.
  const tx = api.tx.utility.batchAll([
    api.tx.assets.create(assetId, alice.address, '1'),
    api.tx.assets.setMetadata(assetId, '0x' + Buffer.from('OASIS Fixture').toString('hex'), '0x4f4654', 6),
    api.tx.assets.mint(assetId, actor, '10000000'),
    api.tx.assets.setTeam(assetId, actor, actor, actor)
  ]);
  let receipt, preparation;
  if (existsSync(completed)) {
    const stored = JSON.parse(readFileSync(completed, 'utf8'));
    preparation = stored.preparation;
    const verified = await verifySignedPreparation(api, preparation, protocol);
    assert.equal(verified.decoded.method.toHex(), tx.method.toHex());
    receipt = await readFinalizedReceipt(api, { blockHash: stored.receipt.blockHash, transactionHash: preparation.transactionHash });
    assert.equal(receipt.dispatchSucceeded, true);
  } else {
    assert.equal((await api.query.assets.asset(assetId)).isNone, true, 'Existing untracked asset: preserve and investigate; never replace it.');
    preparation = await signOriginalCall(api, tx, alice, protocol, { sender: api.createType('AccountId32', alice.address).toHex() });
    const verified = await verifySignedPreparation(api, preparation, protocol);
    assert.equal(verified.decoded.method.toHex(), tx.method.toHex());
    durable(journal, preparation);
    assert.equal(await originalSubmission('http://127.0.0.1:19944', preparation.signedTransactionBase64)(), preparation.transactionHash);
    const start = BigInt(verified.payload.blockNumber).toString();
    const deadline = Date.now() + 30000;
    while (Date.now() < deadline) {
      const outcome = await readVerifiedOriginalRange(api, { sender: verified.sender, transactionHash: preparation.transactionHash },
        { fromBlockNumber: start, maximumBlocks: 16 });
      if (outcome.found) { receipt = outcome.receipt; break; }
      await new Promise(resolve => setTimeout(resolve, 500));
    }
    assert.equal(receipt?.dispatchSucceeded, true, 'Pending fixture: retain original and reconcile, never replay.');
    durable(completed, { preparation, receipt, assetId, actor });
    assert.equal(JSON.parse(readFileSync(journal, 'utf8')).transactionHash, preparation.transactionHash);
    unlinkSync(journal); // Exact confirmed original remains in flushed completed evidence.
  }
  const details = (await api.query.assets.asset(assetId)).unwrap();
  assert.equal(api.createType('AccountId32', details.issuer).toHex(), actor);
  assert.equal(api.createType('AccountId32', details.admin).toHex(), actor);
  assert.equal((await api.query.assets.metadata(assetId)).decimals.toNumber(), 6);
  // Native transfer fee reader MUST NOT accept arbitrary asset/deposit effects as reviewed native-only fees.
  await assert.rejects(readOriginalNativeFee(api, preparation));
  alice.lock();
  console.log(JSON.stringify({ assetId, decimals: 6, actor, transactionHash: preparation.transactionHash,
    finalized: true, publicFunds: false, nativeOnlyFeeReaderRejectsAssetFixture: true }));
} finally { await api.disconnect(); }
