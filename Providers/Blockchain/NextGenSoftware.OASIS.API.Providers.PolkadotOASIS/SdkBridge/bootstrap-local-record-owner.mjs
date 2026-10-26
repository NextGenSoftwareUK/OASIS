// One reusable explicit-key-owner instance, sharing already uploaded code.
import assert from 'node:assert/strict';
import { ApiPromise, HttpProvider, Keyring } from '@polkadot/api';
import { cryptoWaitReady } from '@polkadot/util-crypto';
import { existsSync, readFileSync, openSync, writeSync, fsyncSync, closeSync, unlinkSync } from 'node:fs';
import { resolve } from 'node:path';
import { signOriginalCall } from './native-transfer.mjs';
import { verifySignedPreparation } from './verify-preparation.mjs';
import { originalSubmission, submitOriginalPreparation } from './original-submission.mjs';
import { readVerifiedOriginalRange } from './original-outcome.mjs';
import { readFinalizedReceipt, readFinalizedExtrinsicContext } from './finalized-receipt.mjs';

if (process.env.OASIS_POLKADOT_LOCAL_NATIVE !== '1') throw new Error('Explicit retained local-only opt-in required.');
const evidence = resolve(import.meta.dirname, '../../../../TestResults/PolkadotCurrent');
const pending = resolve(evidence, 'local-record-owner-pending.json');
const completed = resolve(evidence, 'local-record-owner.completed.json');
if (existsSync(pending)) throw new Error('Pending original owner deployment exists; reconcile read-only, never replay.');
const protocol = 'oasis.polkadot.local-record-owner.v1';
const codeHash = '0x7c102aaaaef5fec4e9f8f035c0542c7c6ca23f8c4e6b8de5e0498862debef0fa';
const durable = (path, value) => { const fd = openSync(path, 'wx', 0o600);
  try { writeSync(fd, JSON.stringify(value)); fsyncSync(fd); } finally { closeSync(fd); } };
const api = await ApiPromise.create({ provider: new HttpProvider('http://127.0.0.1:19944'), noInitWarn: true });
let pair;
try {
  assert.equal(api.genesisHash.toHex(), '0xe8cf5d7d636a08721efaa17acc52ed3d895fe2468dccb2520cc8e249ddb6b208');
  await cryptoWaitReady();
  pair = new Keyring({ type: 'sr25519' }).addFromSeed(new Uint8Array(32).fill(2)); // Existing public local fixture only.
  const sender = api.createType('AccountId32', pair.address).toHex();
  let preparation, receipt;
  if (existsSync(completed)) {
    const stored = JSON.parse(readFileSync(completed, 'utf8'));
    assert.equal(stored.codeHash, codeHash);
    preparation = stored.preparation;
    const verified = await verifySignedPreparation(api, preparation, protocol);
    assert.equal(verified.sender, sender);
    assert.equal(verified.decoded.method.section, 'contracts');
    assert.equal(verified.decoded.method.method, 'instantiate');
    assert.equal(verified.decoded.method.args[3].toHex(), codeHash);
    receipt = await readFinalizedReceipt(api, stored.receipt);
  } else {
    const salt = '0x6f617369732d7265636f7264732d73646b2d7631';
    const data = '0x00000001';
    const simulated = await api.call.contractsApi.instantiate(sender, 0, null, null, { Existing: codeHash }, data, salt);
    assert.equal(simulated.result.isOk, true, simulated.result.toString());
    assert.equal(simulated.result.asOk.result.flags.get('bits').toNumber(), 0);
    assert.equal((await api.query.contracts.contractInfoOf(simulated.result.asOk.accountId)).isNone, true,
      'Untracked owner instance exists; preserve and investigate.');
    const deposit = simulated.storageDeposit.isCharge ? simulated.storageDeposit.asCharge : 0;
    const tx = api.tx.contracts.instantiate(0, simulated.gasRequired, deposit, codeHash, data, salt);
    preparation = await signOriginalCall(api, tx, pair, protocol, { sender });
    const verify = async (api, value) => {
      const identity = await verifySignedPreparation(api, value, protocol);
      assert.equal(identity.sender, sender);
      assert.equal(identity.decoded.method.toHex(), tx.method.toHex());
      return { ...identity, transactionHash: identity.decoded.hash.toHex() };
    };
    await submitOriginalPreparation(api, preparation, original => durable(pending, { codeHash, preparation: original }),
      originalSubmission('http://127.0.0.1:19944', preparation.signedTransactionBase64), verify);
    const identity = await verify(api, preparation);
    const deadline = Date.now() + 45000;
    while (Date.now() < deadline) {
      const result = await readVerifiedOriginalRange(api, { sender, transactionHash: preparation.transactionHash },
        { fromBlockNumber: BigInt(identity.payload.blockNumber).toString(), maximumBlocks: 32 });
      if (result.found) { receipt = result.receipt; break; }
      await new Promise(resolve => setTimeout(resolve, 500));
    }
  }
  assert.equal(receipt?.dispatchSucceeded, true, 'Original pending/failed; never recreate or replay.');
  assert.equal(receipt.transactionHash, preparation.transactionHash);
  assert.equal(api.createType('AccountId32', receipt.payer).toHex(), sender);
  const { own } = await readFinalizedExtrinsicContext(api, receipt);
  const instances = own.filter(e => e.section === 'contracts' && e.method === 'Instantiated');
  assert.equal(instances.length, 1);
  assert.equal(api.createType('AccountId32', instances[0].data[0]).toHex(), sender);
  const address = instances[0].data[1].toString();
  const at = await api.at(receipt.blockHash);
  assert.equal((await at.query.contracts.contractInfoOf(address)).unwrap().codeHash.toHex(), codeHash);
  if (!existsSync(completed)) {
    durable(completed, { codeHash, address, sender, preparation, receipt });
    assert.equal(JSON.parse(readFileSync(pending, 'utf8')).preparation.transactionHash, preparation.transactionHash);
    unlinkSync(pending);
  }
  console.log(JSON.stringify({ address, codeHash, sharedUploadedCode: true, transactionHash: preparation.transactionHash,
    actualFeeUnits: receipt.actualFeeUnits, reusable: true, publicFunds: false }));
} finally { pair?.lock(); await api.disconnect(); }
