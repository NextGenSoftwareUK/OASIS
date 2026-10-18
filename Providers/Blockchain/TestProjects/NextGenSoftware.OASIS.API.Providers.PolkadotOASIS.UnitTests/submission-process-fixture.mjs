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
if (request.operation === 'prepareTransfer') {
  emit({ transactionHash: originalHash, chainId: chain, sender: 'sender',
    signedTransactionBase64: 'AQID', signature: 'fixture-signature',
    signDocBase64: Buffer.from(JSON.stringify({ signingPayload: { blockNumber: '0x01' } })).toString('base64') });
} else if (request.operation === 'verifyNativePreparation') {
  if (process.env.OASIS_POLKADOT_TEST_OUTCOME === 'cancel-verification')
    await waitForOwnedCancellation(String(process.pid) + '\n');
  else emit({ verified: true, transactionHash: originalHash, chainId: chain, sender: 'sender' });
} else if (request.operation === 'originalOutcome') {
  if (process.env.OASIS_POLKADOT_TEST_OUTCOME === 'wait-for-cancellation') {
    await waitForOwnedCancellation('1'); // Actual finality read entry, not a timing guess.
  } else if (process.env.OASIS_POLKADOT_TEST_OUTCOME === 'read-error') {
    process.stdout.write(JSON.stringify({ ok: false, error: 'controlled finalized read failure' }));
    process.exitCode = 1;
  } else emit({ found: true, reconciliationRequired: true, receipt: {
    transactionHash: originalHash, finalized: true,
    dispatchSucceeded: process.env.OASIS_POLKADOT_TEST_OUTCOME !== 'dispatch-failed',
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
