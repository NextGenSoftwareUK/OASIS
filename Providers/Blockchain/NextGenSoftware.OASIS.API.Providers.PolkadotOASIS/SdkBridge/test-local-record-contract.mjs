// One retained local-only contract. Original signed calls are journaled before
// submission; a pending original fences all new writes, including after timeout.
import assert from 'node:assert/strict';
import { ApiPromise, HttpProvider, Keyring } from '@polkadot/api';
import { cryptoWaitReady } from '@polkadot/util-crypto';
import { existsSync, readFileSync, openSync, writeSync, fsyncSync, closeSync, unlinkSync } from 'node:fs';
import { resolve } from 'node:path';
import { createHash } from 'node:crypto';
import { signOriginalCall } from './native-transfer.mjs';
import { verifySignedPreparation } from './verify-preparation.mjs';
import { originalSubmission } from './original-submission.mjs';
import { readVerifiedOriginalRange } from './original-outcome.mjs';
import { readFinalizedReceipt, readFinalizedExtrinsicContext } from './finalized-receipt.mjs';
import { registerRecordContractTypes, encodeRecordMessage, decodeRecordResult } from './record-contract-codec.mjs';

if (process.env.OASIS_POLKADOT_LOCAL_NATIVE !== '1') throw new Error('Explicit retained local-only opt-in required.');
const evidence = resolve(import.meta.dirname, '../../../../TestResults/PolkadotCurrent');
const pending = resolve(evidence, 'local-record-contract-pending.json');
if (existsSync(pending)) throw new Error('Pending record original exists. Reconcile read-only; never replay.');
const protocol = 'oasis.polkadot.local-record-contract.v1';
const inspectOnly = process.argv.includes('--inspect-only');
if (inspectOnly && !existsSync(resolve(evidence, 'local-record-contract.deploy.completed.json'))) {
  throw new Error('Read-only inspection requires existing finalized deployment evidence.');
}
const durable = (path, value) => {
  const fd = openSync(path, 'wx', 0o600);
  try { writeSync(fd, JSON.stringify(value)); fsyncSync(fd); } finally { closeSync(fd); }
};
const completedPath = label => resolve(evidence, `local-record-contract.${label}.completed.json`);
const api = await ApiPromise.create({ provider: new HttpProvider('http://127.0.0.1:19944'), noInitWarn: true });
let alice;
try {
  assert.equal(api.genesisHash.toHex(), '0xe8cf5d7d636a08721efaa17acc52ed3d895fe2468dccb2520cc8e249ddb6b208');
  assert.equal((await api.rpc.system.properties()).tokenDecimals.unwrap()[0].toNumber(), 12);
  await cryptoWaitReady();
  alice = new Keyring({ type: 'sr25519' }).addFromUri('//Alice'); // Public local genesis identity only.
  const sender = api.createType('AccountId32', alice.address).toHex();
  registerRecordContractTypes(api.registry);
  const artifact = readFileSync(resolve(evidence, 'record-contract-target/oasis-records.wasm'));
  assert.equal(createHash('sha256').update(artifact).digest('hex'), '16aafeccc5c9ee3d40b994bc9f9307a04ef1cee68ce5e9cbcc05d1ce94108ed3');
  const code = '0x' + artifact.toString('hex');
  const codeHash = api.registry.hash(artifact).toHex();
  const salt = '0x6f617369732d7265636f7264732d7631';
  const constructor = encodeRecordMessage(api.registry, 'new', []);
  const labels = ['deploy', 'create', 'update', 'delete', 'recreate', 'cleanup'];
  const receipts = [];

  async function verifyRetained(label) {
    const stored = JSON.parse(readFileSync(completedPath(label), 'utf8'));
    assert.equal(stored.codeHash, codeHash);
    assert.equal(stored.label, label);
    const verified = await verifySignedPreparation(api, stored.preparation, protocol);
    assert.equal(verified.sender, sender);
    const receipt = await readFinalizedReceipt(api, { blockHash: stored.receipt.blockHash,
      transactionHash: stored.preparation.transactionHash });
    assert.equal(receipt.dispatchSucceeded, true);
    assert.equal(receipt.actualFeeUnits, stored.receipt.actualFeeUnits);
    receipts.push(receipt);
    return stored;
  }

  async function execute(label, tx, validate) {
    if (existsSync(completedPath(label))) {
      const stored = await verifyRetained(label);
      const verified = await verifySignedPreparation(api, stored.preparation, protocol);
      assert.equal(verified.decoded.method.toHex(), tx.method.toHex());
      await validate(stored.receipt);
      return stored.receipt;
    }
    const preparation = await signOriginalCall(api, tx, alice, protocol, { sender });
    const verified = await verifySignedPreparation(api, preparation, protocol);
    assert.equal(verified.decoded.method.toHex(), tx.method.toHex());
    durable(pending, { label, codeHash, preparation });
    assert.equal(await originalSubmission('http://127.0.0.1:19944', preparation.signedTransactionBase64)(), preparation.transactionHash);
    let receipt;
    const deadline = Date.now() + 45000;
    while (Date.now() < deadline) {
      const found = await readVerifiedOriginalRange(api,
        { sender, transactionHash: preparation.transactionHash },
        { fromBlockNumber: BigInt(verified.payload.blockNumber).toString(), maximumBlocks: 32 });
      if (found.found) { receipt = found.receipt; break; }
      await new Promise(resolve => setTimeout(resolve, 500));
    }
    assert.equal(receipt?.dispatchSucceeded, true, 'Original outcome pending/failed: preserve journal, never replay.');
    await validate(receipt);
    durable(completedPath(label), { label, codeHash, preparation, receipt });
    assert.equal(JSON.parse(readFileSync(pending, 'utf8')).preparation.transactionHash, preparation.transactionHash);
    unlinkSync(pending); // Exact finalized original is preserved in flushed completion evidence.
    receipts.push(receipt);
    return receipt;
  }

  let address;
  async function inspectDeploy(receipt) {
    const { own } = await readFinalizedExtrinsicContext(api, receipt);
    const instantiated = own.filter(e => e.section === 'contracts' && e.method === 'Instantiated');
    assert.equal(instantiated.length, 1);
    assert.equal(api.createType('AccountId32', instantiated[0].data[0]).toHex(), sender);
    address = instantiated[0].data[1].toString();
    const at = await api.at(receipt.blockHash);
    assert.equal((await at.query.contracts.contractInfoOf(address)).unwrap().codeHash.toHex(), codeHash);
  }

  if (existsSync(completedPath('cleanup'))) {
    for (const label of labels) await verifyRetained(label);
    await inspectDeploy(receipts[0]);
  } else {
    if (existsSync(completedPath('deploy'))) {
      const stored = await verifyRetained('deploy');
      await inspectDeploy(stored.receipt);
    } else {
      const simulation = await api.call.contractsApi.instantiate(sender, 0, null, null,
        { Upload: code }, constructor, salt);
      assert.equal(simulation.result.isOk, true, simulation.result.toString());
      assert.equal(simulation.result.asOk.result.flags.get('bits').toNumber(), 0);
      const predicted = simulation.result.asOk.accountId;
      assert.equal((await api.query.contracts.contractInfoOf(predicted)).isNone, true, 'Untracked contract exists; preserve it.');
      const deposit = simulation.storageDeposit.isCharge ? simulation.storageDeposit.asCharge : 0;
      const tx = api.tx.contracts.instantiateWithCode(0, simulation.gasRequired, deposit, code, constructor, salt);
      await execute('deploy', tx, inspectDeploy);
    }
  }

  const key = Uint8Array.from(Buffer.from('oasis:record-contract:fixture:v1'));
  async function query(name, args, origin = sender) {
    const finalized = await api.rpc.chain.getFinalizedHead();
    const at = await api.at(finalized);
    const response = await at.call.contractsApi.call(origin, address, 0, null, null,
      encodeRecordMessage(api.registry, name, args));
    assert.equal(response.result.isOk, true, response.result.toString());
    assert.equal(response.result.asOk.flags.get('bits').toNumber(), 0);
    return decodeRecordResult(api.registry, name, response.result.asOk.data.toU8a(true));
  }

  if (inspectOnly) {
    const cell = await query('get', [key]);
    console.log(JSON.stringify({ inspectOnly: true, address, codeHash, cell: cell.toJSON(),
      page: (await query('keys', [key, 0n, 8])).toJSON(), submitted: false }));
  } else {

  if (!existsSync(completedPath('cleanup'))) {
    async function mutate(label, name, args, revision, deleted) {
      const data = encodeRecordMessage(api.registry, name, args);
      const validate = async receipt => {
        const { own } = await readFinalizedExtrinsicContext(api, receipt);
        const events = own.filter(e => e.section === 'contracts' && e.method === 'ContractEmitted'
          && api.createType('AccountId32', e.data[0]).eq(api.createType('AccountId32', address)));
        assert.equal(events.length, 1);
        const expected = api.registry.createType('([u8;32],Bytes,u64,bool)', [
          '0x' + Buffer.from(args.at(-1)).toString('hex'), '0x' + Buffer.from(key).toString('hex'), revision, deleted]);
        assert.equal(events[0].data[1].toHex(), '0x' + Buffer.from(expected.toU8a()).toString('hex'));
      };
      if (existsSync(completedPath(label))) {
        const stored = await verifyRetained(label);
        const { decoded } = await verifySignedPreparation(api, stored.preparation, protocol);
        assert.equal(decoded.method.section, 'contracts');
        assert.equal(decoded.method.method, 'call');
        assert.equal(decoded.method.args.length, 5);
        assert.equal(api.createType('AccountId32', decoded.method.args[0].toString()).toHex(), api.createType('AccountId32', address).toHex());
        assert.equal(decoded.method.args[1].toBigInt(), 0n);
        assert.equal(decoded.method.args[4].toHex(), data);
        await validate(stored.receipt);
        return;
      }
      const simulated = await api.call.contractsApi.call(sender, address, 0, null, null, data);
      assert.equal(simulated.result.isOk, true, simulated.result.toString());
      assert.equal(simulated.result.asOk.flags.get('bits').toNumber(), 0);
      decodeRecordResult(api.registry, name, simulated.result.asOk.data.toU8a(true));
      const deposit = simulated.storageDeposit.isCharge ? simulated.storageDeposit.asCharge : 0;
      const tx = api.tx.contracts.call(address, 0, simulated.gasRequired, deposit, data);
      await execute(label, tx, validate);
    }
    if (!existsSync(completedPath('create'))) assert.equal((await query('get', [key])).isNone, true);
    await mutate('create', 'put', [key, Uint8Array.of(0x11), 'Missing', new Uint8Array(32).fill(1)], 1n, false);
    if (!existsSync(completedPath('update'))) assert.equal((await query('get', [key])).unwrap().value.toHex(), '0x11');
    if (!existsSync(completedPath('delete'))) {
      const page = await query('keys', [key, 0n, 8]);
      assert.equal(page.get('keys').length, 1);
      assert.equal(page.get('next').isNone, true);
    }
    await mutate('update', 'put', [key, Uint8Array.of(0x22), { Revision: 1n }, new Uint8Array(32).fill(2)], 2n, false);
    await mutate('delete', 'delete', [key, { Revision: 2n }, new Uint8Array(32).fill(3)], 3n, true);
    await mutate('recreate', 'put', [key, Uint8Array.of(0x33), 'Missing', new Uint8Array(32).fill(4)], 4n, false);
    assert.equal((await query('get', [key])).unwrap().revision.toString(), '4');
    await mutate('cleanup', 'delete', [key, { Revision: 4n }, new Uint8Array(32).fill(5)], 5n, true);
  }
  assert.equal((await query('get', [key])).isNone, true);
  const finalPage = await query('keys', [new Uint8Array(), 0n, 8]);
  assert.equal(finalPage.get('keys').length, 0);
  assert.equal(finalPage.generation.toString(), '5');
  const bob = '0x8eaf04151687736326c9fea17e25fc5287613693c912909cb226aa4794f26a48';
  for (const [origin, name, args, expected] of [
    [bob, 'put', [key, Uint8Array.of(1), 'Missing', new Uint8Array(32).fill(6)], 'Unauthorized'],
    [sender, 'delete', [key, { Revision: 1n }, new Uint8Array(32).fill(7)], 'Conflict']
  ]) {
    const response = await api.call.contractsApi.call(origin, address, 0, null, null,
      encodeRecordMessage(api.registry, name, args));
    assert.equal(response.result.isOk, true);
    assert.equal(response.result.asOk.flags.get('bits').toNumber(), 1);
    assert.throws(() => decodeRecordResult(api.registry, name, response.result.asOk.data.toU8a(true)),
      error => error.contractError === expected);
  }
  assert.equal((await query('get', [key])).isNone, true);
  assert.equal((await query('keys', [new Uint8Array(), 0n, 8])).generation.toString(), '5');
  console.log(JSON.stringify({ address, codeHash, finalizedOriginals: receipts.length,
    actualTransactionFeeUnits: receipts.reduce((sum, receipt) => sum + BigInt(receipt.actualFeeUnits), 0n).toString(),
    readOnlyRejectionChecks: 2, localTestOnly: true, keysCleaned: true, retainedContract: true, publicFunds: false }));
  }
} finally { alice?.lock(); await api.disconnect(); }
