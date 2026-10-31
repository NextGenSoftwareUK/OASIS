// Local fixture funding ONLY. Never use these public development keys on a public/value-bearing chain.
import assert from 'node:assert/strict';
import { ApiPromise, Keyring } from '@polkadot/api';
import { cryptoWaitReady } from '@polkadot/util-crypto';
import { openSync, writeSync, fsyncSync, closeSync, existsSync, readFileSync, unlinkSync } from 'node:fs';
import { resolve, dirname } from 'node:path';
import { signingDocument, verifyNativePreparation } from './verify-preparation.mjs';
import { originalSubmission } from './original-submission.mjs';
import { readOriginalOutcome } from './original-outcome.mjs';
import { readFinalizedReceipt } from './finalized-receipt.mjs';

if (process.env.OASIS_POLKADOT_LOCAL_NATIVE !== '1') throw new Error('Explicit local-native fixture opt-in required.');
const genesis = '0xe8cf5d7d636a08721efaa17acc52ed3d895fe2468dccb2520cc8e249ddb6b208';
const journal = resolve(process.env.OASIS_POLKADOT_LOCAL_BOOTSTRAP_JOURNAL || '');
if (!process.env.OASIS_POLKADOT_LOCAL_BOOTSTRAP_JOURNAL || dirname(journal) !== resolve('..', '..', '..', '..', 'TestResults', 'PolkadotCurrent'))
  throw new Error('Use the fixed existing PolkadotCurrent evidence directory.');
const completed = journal + '.completed.json';
if (existsSync(journal)) throw new Error('Pending original bootstrap exists; reconcile it read-only, NEVER submit again.');
const durable = (path, value) => {
  const fd = openSync(path, 'wx', 0o600);
  try { writeSync(fd, JSON.stringify(value)); fsyncSync(fd); } finally { closeSync(fd); }
};
const api = await ApiPromise.create({ provider: new (await import('@polkadot/api')).HttpProvider('http://127.0.0.1:19944'), noInitWarn: true });
try {
  assert.equal(api.genesisHash.toHex(), genesis);
  const properties = await api.rpc.system.properties();
  assert.equal(properties.tokenDecimals.unwrap()[0].toNumber(), 12);
  await cryptoWaitReady();
  const keyring = new Keyring({ type: 'sr25519' });
  const recipient = api.createType('AccountId32', keyring.addFromSeed(new Uint8Array(32).fill(2)).address).toHex();
  if (existsSync(completed)) {
    const retained = JSON.parse(readFileSync(completed, 'utf8'));
    assert.equal(retained.preparation.chainId, genesis);
    assert.equal(retained.preparation.recipient, recipient);
    assert.equal(retained.receipt.dispatchSucceeded, true);
    const verified = await verifyNativePreparation(api, retained.preparation);
    const liveReceipt = await readFinalizedReceipt(api, { blockHash: retained.receipt.blockHash,
      transactionHash: retained.preparation.transactionHash });
    assert.equal(liveReceipt.dispatchSucceeded, true);
    assert.equal(liveReceipt.actualFeeUnits, retained.receipt.actualFeeUnits);
    assert.equal(api.createType('AccountId32', liveReceipt.payer).toHex(), verified.sender);
    console.log(JSON.stringify({ reused: true, transactionHash: retained.preparation.transactionHash, recipient }));
  } else {
    const alice = keyring.addFromUri('//Alice'); // Official local-dev genesis key, not an operator credential.
    const tx = api.tx.balances.transferKeepAlive(recipient, '100000000000000'); // Explicit100local UNIT at12decimals.
    let payload, signature;
    await tx.signAsync(alice.address, { tip: 0, signer: { signPayload: async supplied => {
      payload = structuredClone(supplied);
      signature = api.registry.createType('ExtrinsicPayload', supplied, { version: supplied.version }).sign(alice).signature;
      return { id: 0, signature };
    } } });
    const preparation = { protocol: 'oasis.polkadot.native-transfer.preparation.v2', chainId: genesis,
      sender: api.createType('AccountId32', alice.address).toHex(), recipient, units: '100000000000000',
      transactionHash: tx.hash.toHex(), signedTransactionBase64: Buffer.from(tx.toU8a()).toString('base64'),
      signDocBase64: signingDocument(payload), signature };
    await verifyNativePreparation(api, preparation);
    durable(journal, preparation); // No broadcast until original evidence is fsync'd.
    const accepted = await originalSubmission('http://127.0.0.1:19944', preparation.signedTransactionBase64)();
    assert.equal(accepted, preparation.transactionHash);
    const start = BigInt(payload.blockNumber).toString();
    const deadline = Date.now() + 30000;
    let receipt;
    while (Date.now() < deadline) {
      const outcome = await readOriginalOutcome(api, { preparation, fromBlockNumber: start, maximumBlocks: 16 });
      if (outcome.found) { receipt = outcome.receipt; break; }
      await new Promise(resolve => setTimeout(resolve, 500)); // Read-only finality polling, no resubmission.
    }
    assert.equal(receipt?.dispatchSucceeded, true, 'Original bootstrap remains pending; reconcile, never replay.');
    durable(completed, { preparation, receipt });
    assert.equal(JSON.parse(readFileSync(journal, 'utf8')).transactionHash, preparation.transactionHash);
    unlinkSync(journal); // Exact confirmed local original is now retained in completed evidence.
    console.log(JSON.stringify({ reused: false, recipient, receipt }));
  }
} finally { await api.disconnect(); }
