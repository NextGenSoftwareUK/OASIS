// One fixed native SDK mint -> cross-wallet transfer -> burn sequence.
// A retained completion is read/verified, never executed again. Pending originals
// require explicit read-only reconciliation; partial sequences stay preserved.
import assert from 'node:assert/strict';
import { existsSync, readFileSync, openSync, writeFileSync, fsyncSync, closeSync, unlinkSync } from 'node:fs';
import { resolve } from 'node:path';
import { ApiPromise } from '@polkadot/api';
import { ReadOnlyHttpProvider } from './read-only-provider.mjs';
import { prepareNftMutation, verifyNftPreparation } from './psp34-contract-mutation.mjs';
import { readOriginalNftOutcome } from './psp34-contract-outcome.mjs';
import { originalSubmission, submitOriginalPreparation } from './original-submission.mjs';
import { openPinnedContractQuery } from './pinned-contract-query.mjs';
import { encodePsp34Message, decodePsp34Result } from './psp34-contract-codec.mjs';
import { readPsp34EscrowState } from './psp34-contract-read.mjs';

assert.equal(process.env.OASIS_POLKADOT_LOCAL_NATIVE, '1');
assert.equal(process.argv.length, 3);
const requestedMode = process.argv[2];
assert.ok(['--execute', '--reconcile', '--execute-escrow', '--reconcile-escrow'].includes(requestedMode));
const escrow = requestedMode.endsWith('-escrow');
const mode = requestedMode.startsWith('--execute') ? '--execute' : '--reconcile';
const folder = resolve(import.meta.dirname, '../../../../Docs/WIP/HyperDrive-Providers/NativeEvidence/Polkadot');
const prefix = escrow ? 'psp34-escrow-lifecycle' : 'psp34-mutation';
const pending = resolve(folder, prefix + '-pending.jsonl');
const completed = resolve(folder, prefix + '.completed.json');
if (mode === '--execute' && existsSync(pending)) throw new Error('Pending originals exist; reconcile read-only, never replay.');
if (mode === '--reconcile' && !existsSync(pending) && !existsSync(completed)) throw new Error('No original exists to reconcile.');
const fixture = JSON.parse(readFileSync(resolve(folder, escrow ? 'psp34-escrow-deployment.completed.json' : 'psp34-deployment.completed.json'), 'utf8'));
const alice = fixture.sender;
const bob = '0x8ee504148e75c34e8f051899b3c6e4241ff18dc1c9211260b6a6a434bedb485f';
assert.equal(alice, '0x1a4fee48c1ba1a48e8cd43782a8485d635aa91cfb82cbb477f0c1c576bc4031c');
const tokenId = { U128: escrow ? '18446744073709551620' : '18446744073709551617' };
const plans = [{ action: 'mint', sender: alice, to: alice, seed: '02', from: null,
  attributes: [['bmFtZQ==', 'UmV0YWluZWQgbmF0aXZlIE5GVA==']] },
...(escrow ? [
{ action: 'lock', sender: alice, to: fixture.address, seed: '02', from: alice },
{ action: 'unlock', sender: alice, to: alice, seed: '02', from: fixture.address }] : []),
{ action: 'transfer', sender: alice, to: bob, seed: '02', from: alice },
...(escrow ? [
{ action: 'lock', sender: bob, to: fixture.address, seed: '03', from: bob },
{ action: 'unlock', sender: bob, to: bob, seed: '03', from: fixture.address }] : []),
{ action: 'burn', sender: bob, seed: '03', from: bob }];
const durable = (path, value, append = false) => {
  const fd = openSync(path, append ? 'a' : 'wx', 0o600);
  try { writeFileSync(fd, JSON.stringify(value) + '\n'); fsyncSync(fd); } finally { closeSync(fd); }
};
const api = await ApiPromise.create({ provider: new ReadOnlyHttpProvider('http://127.0.0.1:19944'), noInitWarn: true });
try {
  assert.equal(api.genesisHash.toHex(), '0xe8cf5d7d636a08721efaa17acc52ed3d895fe2468dccb2520cc8e249ddb6b208');
  const prior = existsSync(completed) ? JSON.parse(readFileSync(completed, 'utf8')) : null;
  const retained = prior?.originals ?? (existsSync(pending)
    ? readFileSync(pending, 'utf8').trim().split('\n').map(line => JSON.parse(line)) : []);
  const originals = [], receipts = [];
  assert.ok(retained.length <= plans.length);
  const observe = async () => {
    if (escrow) {
      const state = await readPsp34EscrowState(api, { contractAddress: fixture.address, contractCodeHash: fixture.codeHash,
        origin: alice, tokenId });
      return { owner: state.owner, supply: state.totalSupply, beneficiary: state.escrowBeneficiary };
    }
    const view = await openPinnedContractQuery(api, { contractAddress: fixture.address, contractCodeHash: fixture.codeHash, origin: alice });
    const query = async (method, args) => decodePsp34Result(api.registry, method, await view.query(encodePsp34Message(api.registry, method, args)));
    const owner = await query('ownerOf', [tokenId]);
    return { owner: owner.isNone ? null : api.createType('AccountId32', owner.unwrap()).toHex(),
      supply: (await query('totalSupply', [])).toString() };
  };
  if (!retained.length) {
    assert.equal(mode, '--execute');assert.deepEqual(await observe(), { owner: null, supply: '0', ...(escrow ? { beneficiary: null } : {}) });
    // Both accounts already exist from retained native transfer tests. Never
    // top up, reset a ledger or create a replacement test wallet here.
    for (const account of [alice, bob]) {
      const balance = await api.query.system.account(account);
      assert.ok(balance.data.free.toBigInt() > 2000000000000n, 'Retained test signer needs reviewed funding; no automatic top-up.');
    }
  }
  for (let i = 0; i < plans.length; i++) {
    const plan = plans[i];let original = retained[i];
    if (!original) {
      if (mode === '--reconcile') break; // Do not finish a partial sequence by inventing new originals.
      const request = { action: plan.action, tokenId, contractAddress: fixture.address, contractCodeHash: fixture.codeHash,
        sender: plan.sender, recipient: fixture.address, privateKey: '0x' + plan.seed.repeat(32),
        ...(plan.to !== undefined ? { to: plan.to } : {}),
        ...(escrow && plan.from !== null ? { expectedFrom: plan.from } : {}),
        ...(plan.attributes !== undefined ? { attributes: plan.attributes } : {}) };
      original = await prepareNftMutation(api, request);
      await submitOriginalPreparation(api, original, value => durable(pending, value, true),
        originalSubmission('http://127.0.0.1:19944', original.signedTransactionBase64), verifyNftPreparation);
    }
    const identity = await verifyNftPreparation(api, original);
    assert.equal(identity.action, plan.action);assert.equal(identity.sender, plan.sender);
    assert.equal(identity.contractAddress, fixture.address);assert.deepEqual(identity.tokenId, tokenId);
    assert.equal(identity.to, plan.to);
    if (escrow && plan.from !== null) assert.equal(identity.expectedFrom, plan.from);
    assert.deepEqual(identity.attributes, plan.attributes);
    const deadline = Date.now() + 45000;let receipt;
    while (Date.now() < deadline) {
      const outcome = await readOriginalNftOutcome(api, { preparation: original, maximumBlocks: 32 });
      if (outcome.found) { receipt = outcome.receipt;break; }
      await new Promise(resolve => setTimeout(resolve, 500));
    }
    assert.equal(receipt?.dispatchSucceeded, true, 'Original failed/unresolved; preserve sequence, never replay.');
    assert.equal(receipt.nftEffectMatchesRequest, true);
    assert.equal(receipt.from, plan.from);assert.equal(receipt.to, plan.to ?? null);
    originals.push(original);receipts.push(receipt);
    if (!retained.length) {
      assert.deepEqual(await observe(),
        { owner: plan.action === 'burn' ? null : plan.to, supply: plan.action === 'burn' ? '0' : '1',
          ...(escrow ? { beneficiary: plan.action === 'lock' ? plan.sender : null } : {}) });
      if (escrow && plan.action === 'lock') {
        const foreign = plan.sender === alice ? bob : alice;
        await assert.rejects(() => prepareNftMutation(api, { action: 'unlock', tokenId,
          contractAddress: fixture.address, contractCodeHash: fixture.codeHash, sender: foreign,
          recipient: fixture.address, to: foreign, expectedFrom: fixture.address,
          privateKey: '0x' + (foreign === alice ? '02' : '03').repeat(32) }),
          /Native escrow beneficiary does not authorize this lock\/unlock intent/);
        // Preparation rejection never enters submitOriginalPreparation or appends a journal row.
        assert.equal(readFileSync(pending, 'utf8').trim().split('\n').length, originals.length);
      }
    }
  }
  if (originals.length !== plans.length) {
    console.log(JSON.stringify({ mode, verifiedOriginals: originals.length, partialSequencePreserved: true, newSubmissions: 0 }));
    throw new Error('Known partial sequence preserved; no replay or automatic continuation.');
  }
  assert.deepEqual(await observe(), { owner: null, supply: '0', ...(escrow ? { beneficiary: null } : {}) });
  const result = { contractAddress: fixture.address, codeHash: fixture.codeHash, tokenId, originals, receipts,
    nativeSdkLifecycleVerified: true, nativeBeneficiaryEscrowVerified: escrow,
    foreignAndStaleBeneficiaryRejectedBeforeSubmission: escrow && !retained.length,
    finalOwnerAbsent: true, finalSupply: '0', publicCSharpMutationVerified: false,
    fundingTransactions: 0, publicFunds: false };
  if (!prior) durable(completed, result);
  else assert.deepEqual(prior.originals.map(item => item.transactionHash), originals.map(item => item.transactionHash));
  if (existsSync(pending)) {
    const lines = readFileSync(pending, 'utf8').trim().split('\n').map(line => JSON.parse(line));
    assert.deepEqual(lines.map(item => item.transactionHash), originals.map(item => item.transactionHash));
    unlinkSync(pending);
  }
  console.log(JSON.stringify({ mode, contractAddress: fixture.address, verifiedOriginals: originals.length,
    fees: receipts.map(item => ({ action: item.action, actualFeeUnits: item.actualFeeUnits })),
    finalOwnerAbsent: true, finalSupply: '0', fundingTransactions: 0, publicCSharpMutationVerified: false }));
} finally { await api.disconnect(); }
