// One retained local collection. Explicit --deploy or read-only --reconcile.
// An uncertain original is never replaced, funded again or resubmitted.
import assert from 'node:assert/strict';
import { existsSync, readFileSync, openSync, writeFileSync, fsyncSync, closeSync, unlinkSync } from 'node:fs';
import { resolve } from 'node:path';
import { createHash } from 'node:crypto';
import { ApiPromise, HttpProvider, Keyring } from '@polkadot/api';
import { cryptoWaitReady, blake2AsHex } from '@polkadot/util-crypto';
import { encodeOasisPsp34Constructor } from './oasis-psp34-constructor.mjs';
import { signOriginalCall } from './native-transfer.mjs';
import { verifySignedPreparation } from './verify-preparation.mjs';
import { originalSubmission, submitOriginalPreparation } from './original-submission.mjs';
import { readVerifiedOriginalRange } from './original-outcome.mjs';
import { readFinalizedReceipt, readFinalizedExtrinsicContext } from './finalized-receipt.mjs';
import { readPsp34Collection } from './psp34-contract-read.mjs';

assert.equal(process.env.OASIS_POLKADOT_LOCAL_NATIVE, '1', 'Explicit retained local opt-in required.');
assert.equal(process.argv.length, 3);
const mode = process.argv[2];
assert.ok(['--deploy', '--reconcile'].includes(mode), 'Explicit deployment or same-original reconciliation required.');
const root = resolve(import.meta.dirname, '../../../..');
const evidence = resolve(root, 'Docs/WIP/HyperDrive-Providers/NativeEvidence/Polkadot');
const pending = resolve(evidence, 'psp34-deployment-pending.json');
const completed = resolve(evidence, 'psp34-deployment.completed.json');
if (mode === '--deploy' && existsSync(pending)) throw new Error('Pending original exists; reconcile only, never replay.');
if (mode === '--reconcile' && !existsSync(pending) && !existsSync(completed))
  throw new Error('No existing original to reconcile; this mode cannot deploy.');
const cohort = JSON.parse(readFileSync(resolve(evidence, 'psp34-wasm-current.json'), 'utf8'));
const fixture = JSON.parse(readFileSync(resolve(evidence, 'psp34-constructor-simulation-current.json'), 'utf8'));
const code = readFileSync(resolve(root, 'TestResults/PolkadotCurrent/record-contract-target/oasis-psp34.wasm'));
const sha = bytes => createHash('sha256').update(bytes).digest('hex');
assert.equal(sha(code), cohort.sha256);
assert.equal(blake2AsHex(code, 256), cohort.codeHash);
assert.equal(fixture.codeHash, cohort.codeHash);
const durable = (path, value) => {
  const fd = openSync(path, 'wx', 0o600);
  try { writeFileSync(fd, JSON.stringify(value, null, 2) + '\n'); fsyncSync(fd); }
  finally { closeSync(fd); }
};
const protocol = 'oasis.polkadot.local-psp34-deployment.v1';
const api = await ApiPromise.create({ provider: new HttpProvider('http://127.0.0.1:19944'), noInitWarn: true });
let pair;
try {
  assert.equal(api.genesisHash.toHex(), '0xe8cf5d7d636a08721efaa17acc52ed3d895fe2468dccb2520cc8e249ddb6b208');
  const data = encodeOasisPsp34Constructor(api.registry,
    Object.fromEntries(Object.entries(fixture.fields).map(([key, value]) => [key, Buffer.from(value, 'utf8')])));
  assert.equal(data, fixture.data);
  const sender = fixture.sender;
  assert.equal(sender, '0x1a4fee48c1ba1a48e8cd43782a8485d635aa91cfb82cbb477f0c1c576bc4031c');
  let stored = existsSync(completed) ? JSON.parse(readFileSync(completed, 'utf8'))
    : existsSync(pending) ? JSON.parse(readFileSync(pending, 'utf8')) : null;
  const verify = async (connection, preparation) => {
    const identity = await verifySignedPreparation(connection, preparation, protocol);
    assert.equal(identity.sender, sender);
    assert.equal(identity.decoded.method.section, 'contracts');
    assert.equal(identity.decoded.method.method, 'instantiateWithCode');
    const expected = connection.tx.contracts.instantiateWithCode(0, stored.gasRequired,
      stored.storageDepositLimit, '0x' + code.toString('hex'), data, fixture.salt);
    assert.equal(identity.decoded.method.toHex(), expected.method.toHex());
    assert.equal(stored.codeHash, cohort.codeHash);
    assert.equal(stored.address, fixture.address);
    return { ...identity, transactionHash: identity.decoded.hash.toHex() };
  };
  if (!stored) {
    assert.equal(mode, '--deploy');
    assert.equal((await api.query.contracts.contractInfoOf(fixture.address)).isNone, true,
      'Untracked collection exists; investigate instead of deploying again.');
    const simulated = await api.call.contractsApi.instantiate(sender, 0, null, null,
      { Upload: '0x' + code.toString('hex') }, data, fixture.salt);
    assert.equal(simulated.result.isOk, true, simulated.result.toString());
    assert.equal(simulated.result.asOk.result.flags.get('bits').toNumber(), 0);
    assert.equal(api.createType('AccountId32', simulated.result.asOk.accountId).toHex(), fixture.address);
    stored = { codeHash: cohort.codeHash, artifactSha256: cohort.sha256, address: fixture.address,
      gasRequired: simulated.gasRequired.toJSON(),
      storageDepositLimit: simulated.storageDeposit.isCharge ? simulated.storageDeposit.asCharge.toString() : '0' };
    await cryptoWaitReady();
    pair = new Keyring({ type: 'sr25519' }).addFromSeed(new Uint8Array(32).fill(2)); // Public local fixture only.
    assert.equal(api.createType('AccountId32', pair.address).toHex(), sender);
    const tx = api.tx.contracts.instantiateWithCode(0, stored.gasRequired, stored.storageDepositLimit,
      '0x' + code.toString('hex'), data, fixture.salt);
    stored.preparation = await signOriginalCall(api, tx, pair, protocol, { sender });
    await submitOriginalPreparation(api, stored.preparation,
      original => durable(pending, { ...stored, preparation: original }),
      originalSubmission('http://127.0.0.1:19944', stored.preparation.signedTransactionBase64), verify);
  }
  const identity = await verify(api, stored.preparation);
  let receipt = stored.receipt ? await readFinalizedReceipt(api, stored.receipt) : null;
  const deadline = Date.now() + 45000;
  while (!receipt && Date.now() < deadline) {
    const result = await readVerifiedOriginalRange(api,
      { sender, transactionHash: stored.preparation.transactionHash },
      { fromBlockNumber: BigInt(identity.payload.blockNumber).toString(), maximumBlocks: 32 });
    if (result.found) receipt = result.receipt;
    else await new Promise(resolve => setTimeout(resolve, 500));
  }
  assert.equal(receipt?.dispatchSucceeded, true, 'Original unresolved/failed; preserve evidence, never replay.');
  assert.equal(receipt.transactionHash, stored.preparation.transactionHash);
  assert.equal(api.createType('AccountId32', receipt.payer).toHex(), sender);
  const { own } = await readFinalizedExtrinsicContext(api, receipt);
  const instances = own.filter(event => event.section === 'contracts' && event.method === 'Instantiated');
  assert.equal(instances.length, 1);
  assert.equal(api.createType('AccountId32', instances[0].data[0]).toHex(), sender);
  assert.equal(api.createType('AccountId32', instances[0].data[1]).toHex(), fixture.address);
  const at = await api.at(receipt.blockHash);
  assert.equal((await at.query.contracts.contractInfoOf(fixture.address)).unwrap().codeHash.toHex(), cohort.codeHash);
  const collection = await readPsp34Collection(api,
    { contractAddress: fixture.address, origin: sender, contractCodeHash: cohort.codeHash });
  assert.deepEqual(collection.attributes, fixture.fields);
  assert.deepEqual(collection.collectionId, { bytes: fixture.address });
  const result = { ...stored, sender, receipt, collection, nativeDeploymentVerified: true, publicFunds: false };
  if (!existsSync(completed)) durable(completed, result);
  else assert.equal(JSON.parse(readFileSync(completed, 'utf8')).preparation.transactionHash, receipt.transactionHash);
  if (existsSync(pending)) {
    assert.equal(JSON.parse(readFileSync(pending, 'utf8')).preparation.transactionHash, receipt.transactionHash);
    unlinkSync(pending); // Only after the SAME finalized original and flushed completion were verified.
  }
  console.log(JSON.stringify({ address: fixture.address, codeHash: cohort.codeHash,
    transactionHash: receipt.transactionHash, actualFeeUnits: receipt.actualFeeUnits,
    storageDepositLimit: stored.storageDepositLimit, nativeDeploymentVerified: true,
    sdkCollectionReadVerified: true, mode, reusable: true, publicFunds: false }));
} finally { pair?.lock(); await api.disconnect(); }
