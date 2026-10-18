import assert from 'node:assert/strict';
import { ApiPromise } from '@polkadot/api';
import { ReadOnlyHttpProvider } from './read-only-provider.mjs';
import { prepareNativeTransfer } from './native-transfer.mjs';
import { verifyNativePreparation } from './verify-preparation.mjs';
import { submitOriginalPreparation } from './original-submission.mjs';

if (process.env.OASIS_POLKADOT_READONLY_NATIVE !== '1') throw new Error('Explicit read-only native opt-in required.');
const genesis = '0x91b171bb158e2d3848fa23a9f1c25182fb8e20313b2c1eb49219da7a70ce90c3';
const api = await ApiPromise.create({ provider: new ReadOnlyHttpProvider('https://rpc.polkadot.io'), noInitWarn: true });
try {
  assert.equal(api.genesisHash.toHex(), genesis);
  // Public unfunded TEST-ONLY seed, never an operator credential. The transport cannot submit.
  const original = await prepareNativeTransfer(api, {
    sender: '0x189dac29296d31814dc8c56cf3d36a0543372bba7538fa322a4aebfebc39e056',
    recipient: '0xd43593c715fdd31c61141abd04a99fd6822c8558854ccde39a5684e7a56da27d',
    privateKey: '0x' + '01'.repeat(32), amount: '1.0000000001', memo: '🌳'.repeat(64)
  }, 10);
  const verified = await verifyNativePreparation(api, original);
  assert.equal(verified.units, '10000000001');
  assert.equal(Buffer.byteLength(verified.memo, 'utf8'), 256);
  const document = () => JSON.parse(Buffer.from(original.signDocBase64, 'base64').toString('utf8'));
  const changedDocument = mutate => { const value = document(); mutate(value.signingPayload); return Buffer.from(JSON.stringify(value)).toString('base64'); };
  const changedBytes = Buffer.from(original.signedTransactionBase64, 'base64'); changedBytes[changedBytes.length - 1] ^= 1;
  const mutations = [
    ['hash', { transactionHash: '0x' + '00'.repeat(32) }], ['sender', { sender: original.recipient }],
    ['chain', { chainId: '0x' + '00'.repeat(32) }], ['genesis alias', { genesisHash: '0x' + '00'.repeat(32) }],
    ['recipient', { recipient: original.sender }], ['units', { units: '999' }],
    ['noncanonical base64', { signedTransactionBase64: original.signedTransactionBase64 + '=' }],
    ['signed bytes', { signedTransactionBase64: changedBytes.toString('base64') }],
    ['signature', { signature: '0x01' + '00'.repeat(64) }],
    ['document whitespace', { signDocBase64: Buffer.from(' ' + JSON.stringify(document())).toString('base64') }],
    ['document genesis', { signDocBase64: changedDocument(p => { p.genesisHash = '0x' + '00'.repeat(32); }) }],
    ['document nonce', { signDocBase64: changedDocument(p => { p.nonce = '0x00000001'; }) }],
    ['document call', { signDocBase64: changedDocument(p => { p.method = '0x0000'; }) }],
    ['runtime version', { signDocBase64: changedDocument(p => { p.specVersion = '0xffffffff'; }) }],
    ['signed extensions', { signDocBase64: changedDocument(p => { p.signedExtensions.reverse(); }) }]
  ];
  for (const [name, fields] of mutations) await assert.rejects(verifyNativePreparation(api, { ...original, ...fields }), undefined, name);
  const estimateChanged = await verifyNativePreparation(api, { ...original, partialFeeUnits: '0' });
  assert.equal(estimateChanged.feeCoverage, 'identity-only-not-a-fee-cap-or-receipt');
  // Real SDK cryptographic validation, controlled owner/submission capabilities: NO public broadcast.
  let submissions = 0;
  const ownerFailure = new Error('controlled durable owner rejection');
  await assert.rejects(submitOriginalPreparation(api, original, async () => { throw ownerFailure; }, async () => { submissions++; }),
    error => error === ownerFailure);
  assert.equal(submissions, 0);
  const order = [];
  const accepted = await submitOriginalPreparation(api, original,
    async evidence => { assert.equal(evidence.signedTransactionBase64, original.signedTransactionBase64); order.push('persisted'); },
    async () => { order.push('submit'); return original.transactionHash; });
  assert.deepEqual(order, ['persisted', 'submit']);
  assert.equal(accepted.finalized, false);
  assert.equal(accepted.reconciliationRequired, true);
  const lostResponse = new Error('controlled lost submission response');
  await assert.rejects(submitOriginalPreparation(api, original, async () => {}, async () => { throw lostResponse; }),
    error => error.reconciliationRequired === true && error.originalTransactionId === original.transactionHash && error.cause === lostResponse);
  await assert.rejects(submitOriginalPreparation(api, original, async () => {}, async () => '0x' + '00'.repeat(32)),
    error => error.reconciliationRequired === true && error.originalTransactionId === original.transactionHash);
  const mutable = structuredClone(original);
  const captured = submitOriginalPreparation(api, mutable, async evidence => {
    assert.equal(evidence.transactionHash, original.transactionHash);
    assert.equal(evidence.signedTransactionBase64, original.signedTransactionBase64);
  }, async () => original.transactionHash);
  mutable.transactionHash = '0x' + '00'.repeat(32);
  mutable.signedTransactionBase64 = 'AQID';
  assert.equal((await captured).transactionHash, original.transactionHash);
  console.log(JSON.stringify({ network: 'Polkadot', submitted: false, originalVerified: true,
    maximumUtf8MemoBytes: 256, rejectedTamperCases: mutations.length, alteredEstimateNotFeeAuthorization: true,
    controlledSubmissionCases: 5, publicBroadcasts: 0 }));
} finally { await api.disconnect(); }
