import assert from 'node:assert/strict';
import { ApiPromise } from '@polkadot/api';
import { ReadOnlyHttpProvider } from './read-only-provider.mjs';
import { prepareNativeTransfer } from './native-transfer.mjs';
import { verifyNativePreparation } from './verify-preparation.mjs';

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
  console.log(JSON.stringify({ network: 'Polkadot', submitted: false, originalVerified: true,
    maximumUtf8MemoBytes: 256, rejectedTamperCases: mutations.length, alteredEstimateNotFeeAuthorization: true }));
} finally { await api.disconnect(); }
