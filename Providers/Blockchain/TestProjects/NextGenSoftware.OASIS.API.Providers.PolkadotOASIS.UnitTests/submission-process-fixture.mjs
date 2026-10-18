// Controlled process/pipe fixture ONLY. No SDK, RPC, signing or ledger writes.
import { readFileSync } from 'node:fs';
import { pathToFileURL } from 'node:url';
import net from 'node:net';
const request = JSON.parse(readFileSync(0, 'utf8'));
const originalHash = '0x' + 'b'.repeat(64);
const chain = '0x' + 'a'.repeat(64);
const emit = result => process.stdout.write(JSON.stringify({ ok: true, result }));
async function waitForOwnedCancellation(signal) {
  const socket = net.createConnection(process.env.OASIS_POLKADOT_TEST_FINALITY_PIPE);
  await new Promise((resolve, reject) => { socket.once('connect', resolve); socket.once('error', reject); });
  socket.write(signal);
  await new Promise(() => {});
}
if (request.operation === 'prepareTransfer' || request.operation === 'prepareAssetOperation') {
  const asset = request.operation === 'prepareAssetOperation';
  if (asset && (request.assetId !== '424242' || request.recipient !== 'sender' || request.amount !== '1.250001'))
    throw new Error('Controlled fixture expected the frozen typed asset intent.');
  emit({ transactionHash: originalHash, chainId: chain, sender: 'sender',
    ...(asset ? { action: request.action, assetId: request.assetId, recipient: request.recipient, units: '1250001' } : {}),
    signedTransactionBase64: 'AQID', signature: 'fixture-signature',
    signDocBase64: Buffer.from(JSON.stringify({ signingPayload: { blockNumber: '0x01' } })).toString('base64') });
} else if (request.operation === 'verifyNativePreparation' || request.operation === 'verifyAssetPreparation') {
  if (process.env.OASIS_POLKADOT_TEST_OUTCOME === 'cancel-verification')
    await waitForOwnedCancellation(String(process.pid) + '\n');
  else emit({ verified: true, transactionHash: originalHash, chainId: chain, sender: 'sender' });
} else if (request.operation === 'originalNativeFee') {
  const mode = process.env.OASIS_POLKADOT_TEST_OUTCOME;
  if (mode === 'fee-unresolved') {
    process.stdout.write(JSON.stringify({ ok: false, error: 'controlled unavailable original fee' })); process.exitCode = 1;
  } else emit({ transactionHash: mode === 'fee-wrong-hash' ? 'wrong' : originalHash,
    finalized: mode !== 'fee-unfinalized', dispatchSucceeded: mode !== 'fee-failed-dispatch', payer: 'sender',
    feeCoverage: 'finalized-native-transaction-payment-event-tip-included', actualFeeUnits: mode === 'fee-invalid-units' ? '-1' : '12' });
} else if (request.operation === 'originalOutcome' || request.operation === 'assetOutcome') {
  if (process.env.OASIS_POLKADOT_TEST_OUTCOME === 'wait-for-cancellation') {
    await waitForOwnedCancellation('1'); // Actual finality read entry, not a timing guess.
  } else if (process.env.OASIS_POLKADOT_TEST_OUTCOME === 'read-error') {
    process.stdout.write(JSON.stringify({ ok: false, error: 'controlled finalized read failure' }));
    process.exitCode = 1;
  } else emit({ found: true, reconciliationRequired: true, receipt: {
    transactionHash: originalHash, finalized: true,
    dispatchSucceeded: process.env.OASIS_POLKADOT_TEST_OUTCOME !== 'dispatch-failed',
    ...(request.operation === 'assetOutcome' ? {
      assetEffectMatchesRequest: process.env.OASIS_POLKADOT_TEST_OUTCOME !== 'effect-mismatch',
      observedUnits: process.env.OASIS_POLKADOT_TEST_OUTCOME === 'wrong-units' ? '1250000' : '1250001',
      requestedUnits: '1250001', assetId: request.preparation.assetId, recipient: request.preparation.recipient,
      action: process.env.OASIS_POLKADOT_TEST_OUTCOME === 'wrong-action' ? 'other' : request.preparation.action
    } : {}),
    dispatchError: 'controlled dispatch failure', actualFeeUnits: '12'
  } });
} else {
const { preparationClient } = await import(pathToFileURL(process.env.OASIS_NATIVE_PREPARATION_MODULE).href);
await preparationClient(process.env.OASIS_NATIVE_PREPARATION_PIPE)(request.preparation);
if (request.fixtureOutcome === 'error') {
  process.stdout.write(JSON.stringify({ ok: false, error: 'controlled response loss after acknowledgement' }));
  process.exitCode = 1;
} else {
  process.stdout.write(JSON.stringify({ ok: true, result: {
    transactionHash: request.fixtureOutcome === 'wrong-hash' ? 'wrong' : request.preparation.transactionHash,
    persisted: true, submitted: true, poolAccepted: true,
    finalized: request.fixtureOutcome === 'false-finality', reconciliationRequired: true
  } }));
}
}
