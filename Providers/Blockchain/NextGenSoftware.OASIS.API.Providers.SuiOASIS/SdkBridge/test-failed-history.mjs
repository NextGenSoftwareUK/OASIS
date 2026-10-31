// Read-only verification of the original retained failed submission. Never signs or submits.
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { spawn } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const journal = JSON.parse(await readFile(process.env.OASIS_SUI_FAILED_TRANSACTION_JOURNAL, 'utf8'));
assert.ok(journal.digest && journal.sender && journal.chainId, 'Original failure identity required');
const child = spawn(process.execPath, [fileURLToPath(new URL('./bridge.mjs', import.meta.url))], {
  stdio: ['pipe', 'pipe', 'pipe'], windowsHide: true,
});
let output = '', diagnostic = '';
child.stdout.on('data', chunk => output += chunk);
child.stderr.on('data', chunk => diagnostic += chunk);
const completed = new Promise((resolve, reject) => {
  child.once('error', reject);
  child.once('close', code => code === 0 ? resolve() : reject(new Error(`Bridge failed (${code}): ${diagnostic}`)));
});
const timer = setTimeout(() => child.kill(), 90000);
try {
  child.stdin.end(JSON.stringify({ operation: 'history', rpcEndpoint: 'http://127.0.0.1:9000',
    network: 'localnet', chainId: journal.chainId, walletAddress: journal.sender }));
  await completed;
  const envelope = JSON.parse(output);
  assert.equal(envelope.ok, true);
  const rows = envelope.result.filter(row => row.digest === journal.digest);
  assert.ok(rows.length > 0, 'Original failed transaction is absent from sender history');
  for (const row of rows) {
    assert.equal(row.success, false);
    assert.equal(row.sender, journal.sender);
    assert.equal(row.walletAddress, journal.sender);
    assert.match(row.amountUnits, /^-?(?:0|[1-9][0-9]*)$/);
    assert.ok(row.timestampMs !== undefined);
  }
  assert.ok(rows.some(row => BigInt(row.amountUnits) < 0n), 'Failed execution must retain real gas debit');
  console.log(JSON.stringify({ passed: true, digest: journal.digest, failedRows: rows.length,
    chargedGasRetained: true, mutation: false }));
} finally { clearTimeout(timer); }
