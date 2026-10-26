import assert from 'node:assert/strict';
import { spawn } from 'node:child_process';
import net from 'node:net';
import { once } from 'node:events';
import { resolve } from 'node:path';
import { existsSync, readFileSync, openSync, writeSync, fsyncSync, closeSync, unlinkSync } from 'node:fs';

if (process.env.OASIS_POLKADOT_LOCAL_NATIVE !== '1') throw new Error('Retained local-only fixture opt-in required.');
const evidence = resolve(import.meta.dirname, '../../../../TestResults/PolkadotCurrent');
const pending = resolve(evidence, 'local-record-sdk-pending.json');
if (existsSync(pending)) throw new Error('Pending original SDK call exists; reconcile read-only, never replay.');
const owner = JSON.parse(readFileSync(resolve(evidence, 'local-record-owner.completed.json'), 'utf8'));
const base = { endpoint: 'http://127.0.0.1:19944',
  genesisHash: '0xe8cf5d7d636a08721efaa17acc52ed3d895fe2468dccb2520cc8e249ddb6b208',
  contractAddress: owner.address, contractCodeHash: owner.codeHash, origin: owner.sender };
const keyBase64 = Buffer.from('oasis:record-sdk-execution:v1').toString('base64');
const completedPath = label => resolve(evidence, `local-record-sdk.${label}.completed.json`);
let newlySubmitted = 0, durableAcknowledgements = 0, reusedOriginals = 0;
const durable = (path, value) => { const fd = openSync(path, 'wx', 0o600);
  try { writeSync(fd, JSON.stringify(value)); fsyncSync(fd); } finally { closeSync(fd); } };
async function invoke(fields, extra = {}) {
  const child = spawn(process.execPath, [resolve(import.meta.dirname, 'bridge.mjs')], {
    windowsHide: true, stdio: ['pipe', 'pipe', 'pipe'], env: { ...process.env, ...extra }
  });
  let output = '', diagnostics = '';
  child.stdout.setEncoding('utf8'); child.stderr.setEncoding('utf8');
  child.stdout.on('data', part => { output += part; if (output.length > 1048576) child.kill(); });
  child.stderr.on('data', part => { diagnostics = (diagnostics + part).slice(-4096); });
  const terminal = once(child, 'close'); // Join drained stdio before parsing the final envelope.
  const timer = setTimeout(() => child.kill(), 45000);
  child.stdin.end(JSON.stringify({ ...base, ...fields }));
  try {
    const [code] = await terminal;
    const response = JSON.parse(output);
    assert.equal(code, 0, response.error || diagnostics);
    assert.equal(response.ok, true, response.error);
    return response.result;
  } finally { clearTimeout(timer); }
}
async function outcome(preparation) {
  return invoke({ operation: 'recordOutcome', preparation, maximumBlocks: 32 });
}
async function execute(label, fields) {
  if (existsSync(completedPath(label))) {
    const stored = JSON.parse(readFileSync(completedPath(label), 'utf8'));
    const result = await outcome(stored.preparation);
    assert.equal(result.found, true);
    assert.equal(result.receipt.recordEffectMatchesRequest, true);
    reusedOriginals++;
    return result.receipt;
  }
  const preparation = await invoke({ operation: 'prepareRecordMutation', sender: owner.sender,
    recipient: owner.address, privateKey: '0x' + '02'.repeat(32), keyBase64, ...fields });
  const endpoint = process.platform === 'win32' ? '\\\\.\\pipe\\oasis-native-polkadot-record-current' : '/tmp/oasis-native-polkadot-record-current';
  let acknowledged = false;
  const server = net.createServer(socket => {
    let input = Buffer.alloc(0), handled = false;
    socket.on('data', chunk => {
      input = Buffer.concat([input, chunk]);
      if (input.length > 1048576) { socket.destroy(); return; }
      const newline = input.indexOf(10);
      if (newline < 0 || handled) return;
      handled = true;
      try {
        assert.equal(newline, input.length - 1);
        const envelope = JSON.parse(input.subarray(0, newline).toString('utf8'));
        assert.equal(envelope.protocol, 'oasis.native-preparation.v1');
        assert.equal(acknowledged, false);
        assert.deepEqual(envelope.evidence, preparation);
        durable(pending, { label, preparation: envelope.evidence });
        acknowledged = true;
        durableAcknowledgements++;
        socket.end(JSON.stringify({ protocol: 'oasis.native-preparation.v1', persisted: true,
          transactionHash: preparation.transactionHash }) + '\n');
      } catch { socket.destroy(); }
    });
  });
  server.listen(endpoint);
  await once(server, 'listening');
  try {
    const submitted = await invoke({ operation: 'submitRecordPreparation', preparation }, {
      OASIS_NATIVE_PREPARATION_PIPE: endpoint,
      OASIS_NATIVE_PREPARATION_MODULE: resolve(import.meta.dirname,
        '../../../Shared/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage/prepared-submission.mjs')
    });
    assert.equal(acknowledged, true);
    assert.equal(submitted.transactionHash, preparation.transactionHash);
    assert.equal(submitted.finalized, false); // Pool acceptance must never claim final success.
    newlySubmitted++;
  } finally { server.close(); await once(server, 'close'); }
  let result;
  const deadline = Date.now() + 45000;
  do {
    result = await outcome(preparation);
    if (result.found) break;
    await new Promise(resolve => setTimeout(resolve, 500));
  } while (Date.now() < deadline);
  assert.equal(result?.found, true, 'Unknown original outcome: preserve pending journal, never replay.');
  assert.equal(result.receipt.dispatchSucceeded, true);
  assert.equal(result.receipt.recordEffectMatchesRequest, true);
  durable(completedPath(label), { label, preparation, receipt: result.receipt });
  assert.equal(JSON.parse(readFileSync(pending, 'utf8')).preparation.transactionHash, preparation.transactionHash);
  unlinkSync(pending);
  return result.receipt;
}

const receipts = [];
if (existsSync(completedPath('missing-delete'))) {
  for (const label of ['create', 'delete', 'missing-delete']) receipts.push(await execute(label));
} else {
  receipts.push(await execute('create', { action: 'put', valueBase64: 'AQID', expectation: 'Missing',
    operationTagBase64: Buffer.alloc(32, 21).toString('base64') }));
  const revision = receipts[0].revision;
  receipts.push(await execute('delete', { action: 'delete', expectation: 'Revision', revision,
    operationTagBase64: Buffer.alloc(32, 22).toString('base64') }));
  receipts.push(await execute('missing-delete', { action: 'delete', expectation: 'Missing',
    operationTagBase64: Buffer.alloc(32, 23).toString('base64') }));
}
assert.equal(receipts[0].applied, true);
assert.equal(receipts[1].applied, true);
assert.equal(receipts[2].applied, false);
assert.equal((await invoke({ operation: 'recordRead', action: 'get', keyBase64 })).observation, null);
console.log(JSON.stringify({ shippingSdkFinalizedOriginals: receipts.length, newlySubmitted, durableAcknowledgements, reusedOriginals,
  actualTransactionFeeUnits: receipts.reduce((sum, row) => sum + BigInt(row.actualFeeUnits), 0n).toString(),
  missingDeleteProven: true, fixtureKeyCleaned: true, publicFunds: false }));
