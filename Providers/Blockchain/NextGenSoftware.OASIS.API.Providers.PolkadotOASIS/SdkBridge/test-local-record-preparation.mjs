import { test } from 'node:test';
import assert from 'node:assert/strict';
import { ApiPromise, Keyring } from '@polkadot/api';
import { cryptoWaitReady } from '@polkadot/util-crypto';
import { ReadOnlyHttpProvider } from './read-only-provider.mjs';
import { signOriginalCall } from './native-transfer.mjs';
import { encodeRecordMessage, registerRecordContractTypes } from './record-contract-codec.mjs';
import { verifyRecordPreparation, prepareRecordMutation } from './record-contract-mutation.mjs';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { spawnSync } from 'node:child_process';

if (process.env.OASIS_POLKADOT_LOCAL_NATIVE !== '1') throw new Error('Retained local-only fixture opt-in required; no skips.');
const contractAddress = '5ESNt37LSXvXuyWxfWai8aghbkCP8QgDaykAcgsN94eY9cx6';
const contractCodeHash = '0x7c102aaaaef5fec4e9f8f035c0542c7c6ca23f8c4e6b8de5e0498862debef0fa';
test('real signed record intent and limits verify; altered intent is rejected without submission', async () => {
  const api = await ApiPromise.create({ provider: new ReadOnlyHttpProvider('http://127.0.0.1:19944'), noInitWarn: true });
  let alice;
  try {
    assert.equal(api.genesisHash.toHex(), '0xe8cf5d7d636a08721efaa17acc52ed3d895fe2468dccb2520cc8e249ddb6b208');
    await cryptoWaitReady();
    alice = new Keyring({ type: 'sr25519' }).addFromUri('//Alice');
    registerRecordContractTypes(api.registry);
    const sender = api.createType('AccountId32', alice.address).toHex();
    const address = api.createType('AccountId32', contractAddress).toHex();
    const key = Uint8Array.from(Buffer.from('oasis:record-preparation:v1'));
    const tag = new Uint8Array(32).fill(9);
    const data = encodeRecordMessage(api.registry, 'put', [key, Uint8Array.of(1), 'Missing', tag]);
    const simulation = await api.call.contractsApi.call(sender, address, 0, null, null, data);
    assert.equal(simulation.result.isOk, true);
    assert.equal(simulation.result.asOk.flags.get('bits').toNumber(), 0);
    const gasLimit = { refTime: simulation.gasRequired.refTime.toString(), proofSize: simulation.gasRequired.proofSize.toString() };
    const storageDepositLimit = simulation.storageDeposit.asCharge.toString();
    const intent = { sender, contractAddress: address, contractCodeHash, action: 'put',
      keyBase64: Buffer.from(key).toString('base64'), valueBase64: 'AQ==', expectation: 'Missing',
      operationTagBase64: Buffer.from(tag).toString('base64'), gasLimit, storageDepositLimit };
    const original = await signOriginalCall(api, api.tx.contracts.call(address, 0, gasLimit, storageDepositLimit, data),
      alice, 'oasis.polkadot.record-mutation.preparation.v1', intent);
    const verified = await verifyRecordPreparation(api, original);
    assert.equal(verified.transactionHash, original.transactionHash);
    assert.equal(verified.verified, true);
    for (const patch of [
      { keyBase64: 'Ag==' }, { valueBase64: 'Ag==' }, { expectation: 'Any' },
      { operationTagBase64: Buffer.alloc(32, 8).toString('base64') },
      { gasLimit: { ...gasLimit, refTime: '1' } }, { storageDepositLimit: '0' },
      { contractAddress: sender }, { contractCodeHash: '0x' + '00'.repeat(32) }, { protocol: null }
    ]) await assert.rejects(verifyRecordPreparation(api, { ...original, ...patch }));
    // No signing-key extraction or private SDK hooks to turn public //Alice
    // into production seed material. Verify the normal explicit signer path
    // rejects a real non-owner during simulation, before signing/submission.
    const pair = new Keyring({ type: 'sr25519' }).addFromSeed(new Uint8Array(32).fill(2));
    try {
      await assert.rejects(prepareRecordMutation(api, { ...intent, sender: pair.address, recipient: contractAddress,
        contractAddress, privateKey: '0x' + '02'.repeat(32) }), error => error.contractError === 'Unauthorized');
    } finally { pair.lock(); }
    console.log(JSON.stringify({ realSignatureVerified: true, tamperChecks: 9, nonOwnerRejected: true,
      submitted: false, signedOriginalRetainedInMemoryOnly: true }));
  } finally { alice?.lock(); await api.disconnect(); }
});

test('shipping bridge prepares and verifies ordinary explicit-owner call without submitting', () => {
  const owner = JSON.parse(readFileSync(resolve(import.meta.dirname,
    '../../../../TestResults/PolkadotCurrent/local-record-owner.completed.json'), 'utf8'));
  const request = { endpoint: 'http://127.0.0.1:19944',
    genesisHash: '0xe8cf5d7d636a08721efaa17acc52ed3d895fe2468dccb2520cc8e249ddb6b208',
    operation: 'prepareRecordMutation', action: 'put', sender: owner.sender,
    contractAddress: owner.address, recipient: owner.address, contractCodeHash,
    privateKey: '0x' + '02'.repeat(32), keyBase64: Buffer.from('oasis:record-production-preparation:v1').toString('base64'),
    valueBase64: 'AQID', expectation: 'Missing', operationTagBase64: Buffer.alloc(32, 10).toString('base64') };
  function invoke(input) {
    const child = spawnSync(process.execPath, [resolve(import.meta.dirname, 'bridge.mjs')], {
      input: JSON.stringify(input), encoding: 'utf8', windowsHide: true, timeout: 45000, maxBuffer: 1024 * 1024
    });
    assert.equal(child.error, undefined);
    const response = JSON.parse(child.stdout);
    assert.equal(child.status, 0, response.error);
    assert.equal(response.ok, true, response.error);
    return response.result;
  }
  const original = invoke(request);
  assert.equal(original.submitted, false);
  assert.equal(original.sender, owner.sender);
  assert.equal(Object.hasOwn(original, 'privateKey'), false);
  assert.match(original.partialFeeUnits, /^(0|[1-9][0-9]*)$/);
  const verified = invoke({ endpoint: request.endpoint, genesisHash: request.genesisHash,
    operation: 'verifyRecordPreparation', preparation: original });
  assert.equal(verified.verified, true);
  assert.equal(verified.transactionHash, original.transactionHash);
  assert.equal(verified.contractAddress, original.contractAddress);
  assert.equal(verified.feeCoverage, 'identity-and-execution-limits-not-total-fee-cap');
});
