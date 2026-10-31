// Explicit retained local-node test, not part of offline npm test.
import assert from 'node:assert/strict';
import { readFile, writeFile } from 'node:fs/promises';
import { spawn } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import { DirectSecp256k1HdWallet, DirectSecp256k1Wallet } from '@cosmjs/proto-signing';
import { SigningStargateClient, GasPrice } from '@cosmjs/stargate';
import { fromHex } from '@cosmjs/encoding';
import { verifiedBankTransfer } from './transaction-results.mjs';

const journalPath = process.env.OASIS_COSMOS_MULTISEND_JOURNAL;
assert.ok(journalPath, 'Set an existing reusable directory journal path; never discard an uncertain intent');
const rpc = 'http://127.0.0.1:26657';
const chain = 'oasis-cosmos-local';
const owner = await DirectSecp256k1HdWallet.fromMnemonic(`${'abandon '.repeat(23)}art`, { prefix: 'wasm' });
const recipientWallet = await DirectSecp256k1Wallet.fromKey(fromHex(`${'0'.repeat(63)}2`), 'wasm');
const sender = (await owner.getAccounts())[0].address;
const recipient = (await recipientWallet.getAccounts())[0].address;
const client = await SigningStargateClient.connectWithSigner(rpc, owner, {
  gasPrice: GasPrice.fromString('0.025stake'), broadcastTimeoutMs: 60000, broadcastPollIntervalMs: 500,
});
try {
  assert.equal(await client.getChainId(), chain, 'Never submit this fixture on another chain');
  let journal;
  try { journal = JSON.parse(await readFile(journalPath, 'utf8')); }
  catch (error) { if (error.code !== 'ENOENT') throw error; }
  if (!journal) {
    journal = { chain, sender, recipient, before: (await client.getBalance(recipient, 'stake')).amount, state: 'intent' };
    // Persist exclusive intent before any mutation. Unknown outcomes cannot be automatically resubmitted.
    await writeFile(journalPath, JSON.stringify(journal), { flag: 'wx' });
    const result = await client.signAndBroadcast(sender, [{
      typeUrl: '/cosmos.bank.v1beta1.MsgMultiSend', value: {
        inputs: [{ address: sender, coins: [{ denom: 'stake', amount: '5' }] }],
        outputs: [
          { address: recipient, coins: [{ denom: 'stake', amount: '2' }] },
          { address: recipient, coins: [{ denom: 'stake', amount: '3' }] },
        ],
      },
    }], 'auto', 'OASIS retained multi-send history fixture');
    journal.hash = result.transactionHash;
    journal.code = result.code;
    journal.state = 'receipt';
    await writeFile(journalPath, JSON.stringify(journal));
    verifiedBankTransfer(result);
    assert.equal(BigInt((await client.getBalance(recipient, 'stake')).amount) - BigInt(journal.before), 5n);
  }
  assert.equal(journal.chain, chain);
  assert.equal(journal.sender, sender);
  assert.equal(journal.recipient, recipient);
  assert.equal(journal.state, 'receipt', 'Unresolved original intent: reconcile before retrying, do not delete journal');
  assert.equal(journal.code, 0);
  const receipt = await client.getTx(journal.hash);
  assert.ok(receipt);
  assert.equal(receipt.hash, journal.hash);
  assert.equal(receipt.code, 0);
  for (const walletAddress of [sender, recipient]) {
    const process = spawn(globalThis.process.execPath, [fileURLToPath(new URL('./bridge.mjs', import.meta.url))], { stdio: ['pipe', 'pipe', 'pipe'], windowsHide: true });
    let output = '', diagnostic = '';
    process.stdout.on('data', chunk => output += chunk);
    process.stderr.on('data', chunk => diagnostic += chunk);
    const completed = new Promise((resolve, reject) => {
      process.once('error', reject);
      process.once('close', code => code === 0 ? resolve() : reject(new Error(`History bridge failed (${code}): ${diagnostic}`)));
    });
    const timeout = setTimeout(() => process.kill(), 90000);
    try {
      process.stdin.end(JSON.stringify({ operation: 'transactions', rpcEndpoint: rpc, chainId: chain, walletAddress, denom: 'stake' }));
      await completed;
    } finally { clearTimeout(timeout); }
    const envelope = JSON.parse(output);
    assert.equal(envelope.ok, true);
    const rows = envelope.result.filter(row => row.hash === journal.hash);
    assert.equal(rows.length, 2);
    assert.deepEqual(rows.map(row => row.amountUnits).sort(), ['2', '3']);
    assert.deepEqual(rows.map(row => row.transferIndex).sort(), [0, 1]);
    for (const row of rows) {
      assert.equal(row.from, sender); assert.equal(row.to, recipient);
      assert.equal(row.operation, '/cosmos.bank.v1beta1.MsgMultiSend'); assert.equal(row.code, 0);
    }
  }
  console.log(JSON.stringify({ passed: true, hash: journal.hash, outputs: 2, replay: 'read-only receipt/history' }));
} finally { client.disconnect(); }
