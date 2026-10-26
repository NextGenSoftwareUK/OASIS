// One retained native acceptance sequence. Partial originals are preserved and
// reconciled read-only, never replayed or automatically completed.
import assert from 'node:assert/strict';
import { existsSync, readFileSync, openSync, writeFileSync, fsyncSync, closeSync, unlinkSync } from 'node:fs';
import { resolve } from 'node:path';
import { createHash } from 'node:crypto';
import { ApiPromise } from '@polkadot/api';
import { ReadOnlyHttpProvider } from './read-only-provider.mjs';
import { withExplicitSigner, signOriginalCall } from './native-transfer.mjs';
import { verifySignedPreparation } from './verify-preparation.mjs';
import { originalSubmission, submitOriginalPreparation } from './original-submission.mjs';
import { readVerifiedOriginalRange } from './original-outcome.mjs';
import { readFinalizedExtrinsicContext } from './finalized-receipt.mjs';
import { decodeInkScaleResult } from './ink-scale-result.mjs';
import { nativePsp22Fixture, readNativeTestToken } from './native-psp22-test-token.mjs';
import { encodeTokenEscrowMutation, decodeTokenEscrowResult } from './oasis-psp22-escrow-codec.mjs';
import { prepareTokenEscrowMutation, verifyTokenEscrowPreparation, recoverTokenEscrowPreparation } from './psp22-escrow-mutation.mjs';
import { readOriginalTokenEscrowOutcome } from './psp22-escrow-outcome.mjs';
import { readTokenEscrowDeposit } from './psp22-escrow-read.mjs';

assert.equal(process.env.OASIS_POLKADOT_LOCAL_NATIVE, '1'); assert.equal(process.argv.length, 3);
const mode = process.argv[2]; assert.ok(['--execute', '--reconcile', '--approve-public', '--reconcile-public-approval'].includes(mode));
const approvalOnly = mode === '--approve-public' || mode === '--reconcile-public-approval';
const executing = mode === '--execute' || mode === '--approve-public';
const folder = resolve(import.meta.dirname, '../../../../Docs/WIP/HyperDrive-Providers/NativeEvidence/Polkadot');
const prefix = approvalOnly ? 'psp22-public-approval' : 'psp22-escrow-lifecycle';
const pending = resolve(folder, prefix + '-pending.jsonl');
const completed = resolve(folder, prefix + '.completed.json');
if (executing) assert.equal(existsSync(pending) || existsSync(completed), false, 'Existing originals require read-only reconciliation.');
else assert.equal(existsSync(pending) || existsSync(completed), true, 'No retained original exists to reconcile.');
const token = JSON.parse(readFileSync(resolve(folder, 'psp22-test-token-deployment.completed.json'), 'utf8'));
const escrow = JSON.parse(readFileSync(resolve(folder, 'psp22-escrow-deployment.completed.json'), 'utf8'));
const sender = token.sender, foreign = '0x8ee504148e75c34e8f051899b3c6e4241ff18dc1c9211260b6a6a434bedb485f';
assert.equal(sender, '0x1a4fee48c1ba1a48e8cd43782a8485d635aa91cfb82cbb477f0c1c576bc4031c');
const depositId = approvalOnly ? '0x' + createHash('sha256').update('e7b3473198cc497d9f115d5b8dd87fb1').digest('hex') : '0x' + '11'.repeat(32);
const amountUnits = '18446744073709551620';
// Deliberately failing native tests need room for nested writes before their
// outer revert. This local test-only limit is NOT a production fee allowance.
const failedCallStorageLimit = '2000000000000';
const protocol = 'oasis.polkadot.psp22-native-acceptance-call.v1';
const plans = approvalOnly ? [{ kind: 'approve' }] : [{ kind: 'approve' }, { kind: 'fault', fault: 2 }, { kind: 'failedLock' }, { kind: 'fault', fault: 0 },
  { kind: 'lock' }, { kind: 'fault', fault: 2 }, { kind: 'failedUnlock' }, { kind: 'fault', fault: 0 }, { kind: 'unlock' }];
const isSdk = plan => ['lock', 'unlock'].includes(plan.kind);
const isFailure = plan => ['failedLock', 'failedUnlock'].includes(plan.kind);
const isEscrow = plan => isSdk(plan) || isFailure(plan);
const mutation = (action, actor = sender) => ({ action, depositId, tokenAddress: token.address,
  ...(action === 'lock' ? { amountUnits } : {}), contractAddress: escrow.address, contractCodeHash: escrow.codeHash,
  sender: actor, recipient: escrow.address, privateKey: '0x' + (actor === sender ? '02' : '03').repeat(32) });
const durable = (path, value, append = false) => {
  const fd = openSync(path, append ? 'a' : 'wx', 0o600);
  try { writeFileSync(fd, JSON.stringify(value) + '\n'); fsyncSync(fd); } finally { closeSync(fd); }
};
const api = await ApiPromise.create({ provider: new ReadOnlyHttpProvider('http://127.0.0.1:19944'), noInitWarn: true });
try {
  assert.equal(api.genesisHash.toHex(), '0xe8cf5d7d636a08721efaa17acc52ed3d895fe2468dccb2520cc8e249ddb6b208');
  const prior = existsSync(completed) ? JSON.parse(readFileSync(completed, 'utf8')) : null;
  const retained = prior?.originals ?? (existsSync(pending) ? readFileSync(pending, 'utf8').trim().split('\n').map(line => JSON.parse(line)) : []);
  assert.ok(retained.length <= plans.length);
  const originals = [], receipts = [], observations = [];
  const observe = async () => ({ token: await readNativeTestToken(api, { contractAddress: token.address,
    contractCodeHash: token.codeHash, origin: sender, owner: sender, spender: escrow.address }),
    escrow: await readTokenEscrowDeposit(api, { contractAddress: escrow.address, contractCodeHash: escrow.codeHash, origin: sender, depositId }) });
  const assertState = (state, step) => {
    const held = step >= 4 && step < 8, released = step >= 8;
    assert.equal(state.token.totalSupply, nativePsp22Fixture.supply); assert.equal(state.token.decimals, 12);
    assert.equal(state.token.ownerBalance, (BigInt(nativePsp22Fixture.supply) - (held ? BigInt(amountUnits) : 0n)).toString());
    assert.equal(state.token.spenderBalance, held ? amountUnits : '0');
    assert.equal(state.token.allowance, step >= 0 && step < 4 ? amountUnits : '0');
    assert.deepEqual(state.escrow.deposit, held || released ? { owner: sender, tokenAddress: token.address, amountUnits, released } : null);
  };
  if (executing) {
    assertState(await observe(), -1);
    assert.ok((await api.query.system.account(sender)).data.free.toBigInt() > BigInt(failedCallStorageLimit),
      'Retained test account needs reviewed funding; never automatically top up.');
  }
  const callData = plan => {
    if (plan.kind === 'approve') return '0xb20f1bbd' + Buffer.from(api.registry.createType('(AccountId32,u128)', [escrow.address, amountUnits]).toU8a()).toString('hex');
    if (plan.kind === 'fault') return '0x00000030' + Buffer.from(api.registry.createType('u8', plan.fault).toU8a()).toString('hex');
    const action = ['lock', 'failedLock'].includes(plan.kind) ? 'lock' : 'unlock';
    return encodeTokenEscrowMutation(api.registry, action === 'lock'
      ? { action, depositId, tokenAddress: token.address, amountUnits } : { action, depositId });
  };
  const verify = async (connection, original, step) => {
    const plan = plans[step];
    const signed = await verifySignedPreparation(connection, original,
      isSdk(plan) ? 'oasis.polkadot.psp22-escrow.preparation.v1' : protocol);
    const call = signed.decoded.method;
    assert.equal(signed.sender, sender); assert.equal(call.section, 'contracts'); assert.equal(call.method, 'call');
    assert.equal(call.args.length, 5); assert.equal(call.args[1].toBigInt(), 0n); assert.equal(call.args[3].isSome, true);
    assert.equal(connection.createType('AccountId32', call.args[0].toString()).toHex(), isEscrow(plan) ? escrow.address : token.address);
    assert.equal(call.args[4].toHex(), callData(plan));
    if (isFailure(plan)) assert.equal(call.args[3].unwrap().toString(), failedCallStorageLimit);
    if (isSdk(plan)) await verifyTokenEscrowPreparation(connection, original);
    return { transactionHash: signed.decoded.hash.toHex(), sender, fromBlockNumber: BigInt(signed.payload.blockNumber).toString() };
  };
  for (let step = 0; step < plans.length; step++) {
    const plan = plans[step]; let original = retained[step];
    if (!original) {
      if (!executing) break;
      if (isSdk(plan)) original = await prepareTokenEscrowMutation(api, mutation(plan.kind));
      else {
        const address = isEscrow(plan) ? escrow.address : token.address;
        const data = callData(plan);
        const simulation = await api.call.contractsApi.call(sender, address, 0, null, null, data);
        assert.equal(simulation.result.isOk, true, simulation.result.toString());
        const result = simulation.result.asOk;
        if (isFailure(plan)) {
          assert.notEqual(result.flags.get('bits').toNumber(), 0);
          assert.throws(() => decodeTokenEscrowResult(api.registry, 'mutation', result.data.toU8a(true)), /TokenCallFailed/);
        } else {
          assert.equal(result.flags.get('bits').toNumber(), 0);
          api.registry.register({ NativeTestPsp22Error: { _enum: { Custom: 'String', InsufficientBalance: 'Null', InsufficientAllowance: 'Null',
            ZeroRecipientAddress: 'Null', ZeroSenderAddress: 'Null', SafeTransferCheckFailed: 'String' } },
            NativeTestPsp22LangError: { _enum: { CouldNotReadInput: 1 } } });
          decodeInkScaleResult(api.registry, 'Result<(),NativeTestPsp22Error>', 'NativeTestPsp22LangError', result.data.toU8a(true), 'Acceptance token');
        }
        const gas = { refTime: simulation.gasRequired.refTime.toString(), proofSize: simulation.gasRequired.proofSize.toString() };
        const storage = isFailure(plan) ? failedCallStorageLimit : simulation.storageDeposit.isCharge ? simulation.storageDeposit.asCharge.toString() : '0';
        original = await withExplicitSigner(api, { sender, recipient: address, privateKey: '0x' + '02'.repeat(32) }, pair => signOriginalCall(api,
          api.tx.contracts.call(address, 0, gas, storage, data), pair, protocol, { sender }));
      }
      await submitOriginalPreparation(api, original, value => durable(pending, value, true),
        originalSubmission('http://127.0.0.1:19944', original.signedTransactionBase64), (reader, value) => verify(reader, value, step));
    }
    const identity = await verify(api, original, step);
    let receipt; const deadline = Date.now() + 45000;
    while (Date.now() < deadline) {
      const outcome = await readVerifiedOriginalRange(api, identity, { fromBlockNumber: identity.fromBlockNumber, maximumBlocks: 32 });
      if (outcome.found) { receipt = outcome.receipt; break; }
      await new Promise(resolve => setTimeout(resolve, 500));
    }
    assert.equal(receipt?.dispatchSucceeded, !isFailure(plan), 'Original unresolved or unexpected outcome; preserve, never replay.');
    if (isFailure(plan)) {
      const context = await readFinalizedExtrinsicContext(api, receipt);
      const errors = context.own.filter(event => event.section === 'system' && event.method === 'ExtrinsicFailed');
      assert.equal(errors.length, 1); assert.equal(errors[0].data[0].isModule, true);
      const error = api.registry.findMetaError(errors[0].data[0].asModule);
      assert.equal(error.section, 'contracts'); assert.equal(error.name, 'ContractReverted',
        'Out-of-gas/storage-budget failures cannot count as the intended native rollback proof.');
    }
    const at = await api.at(receipt.blockHash), address = isEscrow(plan) ? escrow.address : token.address;
    assert.equal((await at.query.contracts.contractInfoOf(address)).unwrap().codeHash.toHex(), isEscrow(plan) ? escrow.codeHash : token.codeHash);
    originals.push(original); receipts.push(receipt);
    if (executing) {
      const state = await observe(); assertState(state, step); observations.push(state);
      if (step === 4) await assert.rejects(prepareTokenEscrowMutation(api, mutation('unlock', foreign)), /does not authorize/);
      if (step === 8) {
        await assert.rejects(prepareTokenEscrowMutation(api, mutation('unlock')), /does not authorize/);
        await assert.rejects(prepareTokenEscrowMutation(api, mutation('lock')), /does not authorize/);
      }
    }
  }
  assert.equal(originals.length, plans.length, 'Partial originals preserved; no automatic continuation or replay.');
  const sdkReceipts = [];
  for (const step of approvalOnly ? [] : [4, 8]) {
    const original = originals[step];
    // Deliberately discard unpersisted intent JSON: recover using only common
    // signed-original fields and the explicit reviewed code identity.
    const common = Object.fromEntries(['protocol', 'chainId', 'sender', 'transactionHash', 'signedTransactionBase64', 'signDocBase64', 'signature']
      .map(key => [key, original[key]]));
    const recovered = await recoverTokenEscrowPreparation(api, { contractCodeHash: escrow.codeHash, preparation: common });
    assert.equal(recovered.recoveredFromSignedOriginal, true);
    const outcome = await readOriginalTokenEscrowOutcome(api, { preparation: recovered, maximumBlocks: 32 });
    assert.equal(outcome.found, true); assert.equal(outcome.receipt.tokenEscrowEffectMatchesRequest, true);
    assert.equal(outcome.receipt.amountUnits, amountUnits); sdkReceipts.push(outcome.receipt);
  }
  const final = await observe();
  // A later public C# lock consumes the approval. Recovery proves the original
  // receipt without pretending historical allowance is the current state.
  if (!approvalOnly) assertState(final, 8);
  else if (executing) assertState(final, 0);
  const result = { token: token.address, escrow: escrow.address, depositId, amountUnits, originals, receipts, sdkReceipts,
    observations: prior?.observations ?? observations, final, positiveNativeCustodyVerified: !approvalOnly, nativeFailedBroadcastRollbackVerified: !approvalOnly,
    commonSignedFieldRecoveryVerified: !approvalOnly, syntheticLocalWallet: true, hostedAdmissionVerified: false, publicCSharpTokenMethodsVerified: false,
    publicFunds: false, fundingTransactions: 0, failedCallStorageLimitUnits: failedCallStorageLimit };
  if (!prior) durable(completed, result);
  else assert.deepEqual(prior.originals.map(item => item.transactionHash), originals.map(item => item.transactionHash));
  if (existsSync(pending)) {
    const rows = readFileSync(pending, 'utf8').trim().split('\n').map(line => JSON.parse(line));
    assert.deepEqual(rows.map(item => item.transactionHash), originals.map(item => item.transactionHash)); unlinkSync(pending);
  }
  console.log(JSON.stringify({ mode, verifiedOriginals: originals.length, expectedFailedOriginals: approvalOnly ? 0 : 2,
    actualFrameFeeUnits: receipts.reduce((sum, value) => sum + BigInt(value.actualFeeUnits), 0n).toString(),
    finalTokenOwnerBalance: final.token.ownerBalance, finalEscrowBalance: final.token.spenderBalance,
    finalAllowance: final.token.allowance, depositReleased: final.escrow.deposit?.released ?? false, hostedAdmissionVerified: false }));
} finally { await api.disconnect(); }
