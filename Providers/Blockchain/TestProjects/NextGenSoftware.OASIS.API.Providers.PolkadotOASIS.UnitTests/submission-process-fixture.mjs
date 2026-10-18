// Controlled process/pipe fixture ONLY. No SDK, RPC, signing or ledger writes.
import { readFileSync } from 'node:fs';
import { pathToFileURL } from 'node:url';
const request = JSON.parse(readFileSync(0, 'utf8'));
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
