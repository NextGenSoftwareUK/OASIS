import { test } from 'node:test';
import assert from 'node:assert/strict';
import { invoke, execute, withNativeMutationEvidence, sdkErrorEnvelope, NativeMutationReconciliationError, publicationArtifacts, verifiedHistoryTransaction, nativeFeeReceipt, historyScanComplete } from './bridge.mjs';
import { Transaction } from '@mysten/sui/transactions';
import { toBase58 } from '@mysten/sui/utils';
import { verifyTransactionSignature } from '@mysten/sui/verify';
import { SuiGrpcClient } from '@mysten/sui/grpc';
import {verifyPreparedIdentity} from './bridge.mjs';

function offlineTransaction(signer) {
  const tx = new Transaction();
  tx.setSender(signer.toSuiAddress());
  tx.setGasPrice(1); tx.setGasBudget(1000);
  tx.setGasPayment([{ objectId: '0x'+'2'.repeat(64), version: '1', digest: toBase58(new Uint8Array(32).fill(1)) }]);
  return tx;
}
const originalDigest = await offlineTransaction(Ed25519Keypair.fromSecretKey(new Uint8Array(32).fill(7))).getDigest();
test('official Sui preparation verifier binds digest bytes signature and sender without network',async()=>{
 const signer=Ed25519Keypair.fromSecretKey(new Uint8Array(32).fill(7));
 const bytes=await offlineTransaction(signer).build();const {signature}=await signer.signTransaction(bytes);
 const evidence={transactionHash:originalDigest,sender:signer.toSuiAddress(),signature,signedTransactionBase64:Buffer.from(bytes).toString('base64')};
 assert.equal((await verifyPreparedIdentity(evidence)).verified,true);
 assert.equal((await invoke({operation:'verifyNativePreparation',preparation:evidence})).verified,true);
 for(const altered of [{transactionHash:'wrong'},{sender:Ed25519Keypair.generate().toSuiAddress()},{signature:'invalid'},{signedTransactionBase64:'AQID'},{signedTransactionBase64:evidence.signedTransactionBase64+' '}])
  await assert.rejects(verifyPreparedIdentity({...evidence,...altered}));
});
test('bridge entry point validates trusted preparation callback without treating wallet generation as submission', async () => {
  await assert.rejects(invoke({operation:'generateKey'},true), /awaited callback/);
  let preparations=0;
  const wallet=await invoke({operation:'generateKey'},async()=>{preparations++;});
  assert.ok(wallet.address);assert.equal(preparations,0);
});
test('concurrent Sui preparation callbacks remain bound to their own signed operation', async () => {
  const seen=await Promise.all([7,8].map(async seed=>{
    const signer=Ed25519Keypair.fromSecretKey(new Uint8Array(32).fill(seed));
    const expected=await offlineTransaction(signer).getDigest();let preparations=0;
    await withNativeMutationEvidence(()=>execute({async executeTransaction(request){
      await Promise.resolve();return {Transaction:{digest:await Transaction.from(request.transaction).getDigest(),status:{success:true}}};
    },async waitForTransaction(){}},offlineTransaction(signer),signer),async evidence=>{
      await Promise.resolve();preparations++;assert.equal(evidence.transactionHash,expected);assert.equal(evidence.sender,signer.toSuiAddress());
    });
    assert.equal(preparations,1);return expected;
  }));
  assert.notEqual(seen[0],seen[1]);
});
test('official Sui signing awaits prepared persistence and rejection prevents submission', async () => {
  const signer=Ed25519Keypair.fromSecretKey(new Uint8Array(32).fill(7));
  for(const reject of [false,true]) {
    const order=[],failure=new Error('controlled durable persistence failure');
    const client={async executeTransaction(request){
      order.push('submit');return {Transaction:{digest:await Transaction.from(request.transaction).getDigest(),status:{success:true}}};
    },async waitForTransaction(){}};
    const operation=withNativeMutationEvidence(()=>execute(client,offlineTransaction(signer),signer),async evidence=>{
      order.push('prepare');assert.equal(Object.isFrozen(evidence),true);
      const bytes=Buffer.from(evidence.signedTransactionBase64,'base64');
      assert.equal(await Transaction.from(bytes).getDigest(),evidence.transactionHash);
      assert.equal((await verifyTransactionSignature(bytes,evidence.signature)).toSuiAddress(),evidence.sender);
      await Promise.resolve();if(reject)throw failure;order.push('persisted');
    });
    if(reject){await assert.rejects(operation,error=>error===failure && !sdkErrorEnvelope(error).nativeMutation);assert.deepEqual(order,['prepare']);}
    else {await operation;assert.deepEqual(order,['prepare','persisted','submit']);}
  }
});

test('post-execution mapping errors retain verified original evidence across the whole operation', async () => {
  const signer = Ed25519Keypair.fromSecretKey(new Uint8Array(32).fill(7));
  const mappingError = new Error('controlled post-execution mapping failure');
  await assert.rejects(withNativeMutationEvidence(async () => {
    await execute({ async executeTransaction() { return { Transaction: { digest: originalDigest, status: { success: true } } }; },
      async waitForTransaction() {} }, offlineTransaction(signer), signer);
    throw mappingError;
  }), error => {
    assert.deepEqual(sdkErrorEnvelope(error).nativeMutation, { transactionHash: originalDigest, executionSucceeded: true });
    assert.equal(error.cause, mappingError);
    return true;
  });
});

test('concurrent operation contexts cannot overwrite each other or reclassify preflight errors', async () => {
  const outcomes = await Promise.all([7,8].map(async seed => {
    const signer = Ed25519Keypair.fromSecretKey(new Uint8Array(32).fill(seed));
    const digest = await offlineTransaction(signer).getDigest();
    try {
      await withNativeMutationEvidence(async () => {
        await execute({ async executeTransaction(request) {
          await Promise.resolve();
          return { Transaction: { digest: await Transaction.from(request.transaction).getDigest(), status: { success: true } } };
        }, async waitForTransaction() {} }, offlineTransaction(signer), signer);
        await Promise.resolve(); throw new Error('post-execution failure');
      });
      assert.fail('Expected mapping failure');
    } catch (error) { assert.equal(sdkErrorEnvelope(error).nativeMutation.transactionHash, digest); return digest; }
  }));
  assert.notEqual(outcomes[0], outcomes[1]);
  const preflight = new Error('ordinary preflight failure');
  await assert.rejects(withNativeMutationEvidence(async () => { throw preflight; }), error => error === preflight && !sdkErrorEnvelope(error).nativeMutation);
});

test('submission transport interruption retains digest of the exact signed bytes and never resubmits', async () => {
  const key = Ed25519Keypair.fromSecretKey(new Uint8Array(32).fill(7));
  let signatures = 0, submissions = 0;
  const transportError = new Error('controlled submission transport interruption');
  const signer = { toSuiAddress: () => key.toSuiAddress(), async signTransaction(bytes) { signatures++; return key.signTransaction(bytes); } };
  await assert.rejects(execute({
    async executeTransaction(request) {
      submissions++;
      assert.equal(await Transaction.from(request.transaction).getDigest(), originalDigest);
      assert.equal((await verifyTransactionSignature(request.transaction, request.signatures[0])).toSuiAddress(), key.toSuiAddress());
      throw transportError;
    },
    async waitForTransaction() { assert.fail('Unknown submission cannot enter successful readback'); }
  }, offlineTransaction(key), signer), error => {
    assert.deepEqual(sdkErrorEnvelope(error).nativeMutation, { transactionHash: originalDigest, executionSucceeded: null });
    assert.equal(error.cause, transportError);
    return true;
  });
  assert.equal(signatures, 1); assert.equal(submissions, 1);
});

test('real official gRPC transport failure retains original signed identity without a node or ledger mutation', { timeout: 10000 }, async () => {
  const signer = Ed25519Keypair.fromSecretKey(new Uint8Array(32).fill(7));
  const client = new SuiGrpcClient({ network: 'localnet', baseUrl: 'http://127.0.0.1:1' });
  await assert.rejects(execute(client, offlineTransaction(signer), signer), error => {
    assert.deepEqual(sdkErrorEnvelope(error).nativeMutation, { transactionHash: originalDigest, executionSucceeded: null });
    assert.ok(error.cause instanceof Error);
    return true;
  });
});

test('build and signing failures remain pre-submission errors with zero broadcasts', async () => {
  const key = Ed25519Keypair.fromSecretKey(new Uint8Array(32).fill(7));
  let submissions = 0;
  const client = { async executeTransaction() { submissions++; assert.fail('No prepared signature exists'); } };
  await assert.rejects(execute(client, new Transaction(), key), error => !sdkErrorEnvelope(error).nativeMutation);
  const signingError = new Error('controlled signing failure');
  await assert.rejects(execute(client, offlineTransaction(key), { toSuiAddress: () => key.toSuiAddress(), async signTransaction() { throw signingError; } }), error => error === signingError);
  assert.equal(submissions, 0);
});

test('execution receipts for another digest cannot replace original signed identity or establish outcome', async () => {
  const signer = Ed25519Keypair.fromSecretKey(new Uint8Array(32).fill(7));
  for (const response of [{ Transaction: { digest: 'another-digest', status: { success: true } } }, { FailedTransaction: { digest: 'another-digest', status: { success: false } } }])
    await assert.rejects(execute({ async executeTransaction() { return response; } }, offlineTransaction(signer), signer), error => {
      assert.deepEqual(sdkErrorEnvelope(error).nativeMutation, { transactionHash: originalDigest, executionSucceeded: null });
      return true;
    });
});

test('execution readback failure retains original digest and never resubmits', async () => {
  const signer = Ed25519Keypair.fromSecretKey(new Uint8Array(32).fill(7));
  const readbackError = new Error('controlled index transport failure');
  let submissions = 0, reads = 0;
  const client = {
    async executeTransaction(request) {
      submissions++;
      const publicKey = await verifyTransactionSignature(request.transaction, request.signatures[0]);
      assert.equal(publicKey.toSuiAddress(), signer.toSuiAddress());
      assert.equal(await Transaction.from(request.transaction).getDigest(), originalDigest);
      assert.equal(request.include.effects, true);
      return { Transaction: { digest: originalDigest, status: { success: true } } };
    },
    async waitForTransaction(request) {
      reads++;
      assert.equal(request.digest, originalDigest);
      throw readbackError;
    }
  };
  await assert.rejects(execute(client, offlineTransaction(signer), signer), error => {
    assert.ok(error.message.includes(originalDigest));
    assert.match(error.message, /Reconcile this transaction before any retry/);
    assert.equal(error.cause, readbackError);
    assert.deepEqual(sdkErrorEnvelope(error).nativeMutation, { transactionHash: originalDigest, executionSucceeded: true });
    return true;
  });
  assert.equal(submissions, 1);
  assert.equal(reads, 1);
});

test('failed execution retains original failure and does not wait or resubmit', async () => {
  const signer = Ed25519Keypair.fromSecretKey(new Uint8Array(32).fill(7));
  let submissions = 0;
  const client = {
    async executeTransaction() {
      submissions++;
      return { FailedTransaction: { digest: originalDigest, status: { success: false, error: 'controlled Move abort' } } };
    },
    async waitForTransaction() { assert.fail('Failed execution must not enter success readback'); }
  };
  await assert.rejects(execute(client, offlineTransaction(signer), signer), error => {
    assert.ok(error.message.includes(originalDigest)); assert.match(error.message, /controlled Move abort/);
    assert.deepEqual(sdkErrorEnvelope(error).nativeMutation, { transactionHash: originalDigest, executionSucceeded: false });
    return true;
  });
  assert.equal(submissions, 1);
});

test('successful execution waits on and returns the original transaction once', async () => {
  const signer = Ed25519Keypair.fromSecretKey(new Uint8Array(32).fill(7));
  const transaction = { digest: originalDigest, status: { success: true } };
  let submissions = 0, reads = 0;
  const client = {
    async executeTransaction() { submissions++; return { Transaction: transaction }; },
    async waitForTransaction(request) { reads++; assert.equal(request.digest, transaction.digest); }
  };
  assert.equal(await execute(client, offlineTransaction(signer), signer), transaction);
  assert.equal(submissions, 1);
  assert.equal(reads, 1);
});

test('native error envelopes retain explicit outcomes without inventing missing transaction identity', () => {
  for (const outcome of [true, false, null]) {
    const error = new NativeMutationReconciliationError('original error', undefined, outcome);
    const envelope = JSON.parse(JSON.stringify(sdkErrorEnvelope(error)));
    assert.equal(envelope.error, 'original error');
    assert.deepEqual(envelope.nativeMutation, { transactionHash: null, executionSucceeded: outcome });
  }
  assert.deepEqual(sdkErrorEnvelope(new Error('preflight error')), { ok: false, error: 'preflight error' });
});

test('missing execution digest remains unreconciled rather than becoming an ordinary retryable error', async () => {
  const signer = Ed25519Keypair.fromSecretKey(new Uint8Array(32).fill(7));
  let submissions = 0;
  await assert.rejects(execute({
    async executeTransaction() { submissions++; return {}; },
    async waitForTransaction() { assert.fail('No digest is available for readback'); }
  }, offlineTransaction(signer), signer), error => {
    assert.deepEqual(sdkErrorEnvelope(error).nativeMutation, { transactionHash: originalDigest, executionSucceeded: null });
    return true;
  });
  assert.equal(submissions, 1);
});

test('execution response does not manufacture success or failure from missing status', async () => {
  const signer = Ed25519Keypair.fromSecretKey(new Uint8Array(32).fill(7));
  for (const [response, outcome] of [
    [{ FailedTransaction: { digest: originalDigest } }, null],
    [{ Transaction: { digest: originalDigest, status: { success: false } } }, false],
    [{ Transaction: { digest: originalDigest } }, null],
    [{ Transaction: { digest: originalDigest, status: { success: 'true' } } }, null]
  ]) {
    let submissions = 0;
    await assert.rejects(execute({
      async executeTransaction() { submissions++; return response; },
      async waitForTransaction() { assert.fail('Unverified execution cannot enter successful readback'); }
    }, offlineTransaction(signer), signer), error => {
      assert.deepEqual(sdkErrorEnvelope(error).nativeMutation, { transactionHash: originalDigest, executionSucceeded: outcome });
      return true;
    });
    assert.equal(submissions, 1);
  }
});

test('history completion requires index coverage of the fixed committed checkpoint', () => {
  for (const reason of [3, 5]) {
    assert.equal(historyScanComplete(reason, 42n, 42n), true);
    assert.throws(() => historyScanComplete(reason, 41n, 42n), /not covered/);
    assert.throws(() => historyScanComplete(reason, undefined, 42n), /not covered/);
  }
  for (const reason of [1, 2]) assert.equal(historyScanComplete(reason, 41n, 42n), false);
  for (const reason of [0, 4, 6, undefined])
    assert.throws(() => historyScanComplete(reason, 42n, 42n), /unexpected/);
});
import { Ed25519Keypair } from '@mysten/sui/keypairs/ed25519';

test('fee receipts retain exact charged failure, payer and signed rebate without double counting', () => {
  const payer = '0x' + 'a'.repeat(64);
  const gasUsed = { computationCost: '9007199254740993', storageCost: '7', storageRebate: '3', nonRefundableStorageFee: '2' };
  const tx = { digest: 'original', checkpoint: '42', timestampMs: 1, status: { success: false }, transaction: { gasData: { owner: payer } },
    effects: { transactionDigest: 'original', gasUsed } };
  const receipt = nativeFeeReceipt(tx, 'original');
  assert.equal(receipt.success, false); assert.equal(receipt.payer, payer);
  assert.equal(receipt.netUnits, '9007199254740997');
  assert.equal(nativeFeeReceipt({ ...tx, effects: { ...tx.effects, gasUsed: { ...gasUsed, computationCost: '1', storageCost: '2', storageRebate: '7' } } }, 'original').netUnits, '-4');
  assert.throws(() => nativeFeeReceipt(tx, 'other'));
  assert.throws(() => nativeFeeReceipt({ ...tx, transaction: { gasData: { owner: null } } }, 'original'));
  assert.throws(() => nativeFeeReceipt({ ...tx, checkpoint: null }, 'original'));
  for (const value of ['-1', '01', '1.5', '18446744073709551616', 7])
    assert.throws(() => nativeFeeReceipt({ ...tx, effects: { ...tx.effects, gasUsed: { ...gasUsed, storageCost: value } } }, 'original'));
});

test('history requires original digest and explicit outcome while preserving failed execution', () => {
  const tx = {digest:'original',status:{success:false},transaction:{sender:'0x'+'1'.repeat(64)},
    checkpoint:'42',timestampMs:1,effects:{transactionDigest:'original'},balanceChanges:[]};
  assert.equal(verifiedHistoryTransaction(tx,'original'),tx);
  assert.throws(() => verifiedHistoryTransaction(tx,'different'), /mismatched/);
  for (const checkpoint of [null, undefined, '-1', '1.5', '01', '18446744073709551616'])
    assert.throws(() => verifiedHistoryTransaction({...tx,checkpoint},'original'), /incomplete/);
  for (const timestampMs of [0, -1, 1.5, Number.MAX_SAFE_INTEGER + 1])
    assert.throws(() => verifiedHistoryTransaction({...tx,timestampMs},'original'), /incomplete/);
  assert.throws(() => verifiedHistoryTransaction({...tx,effects:{transactionDigest:'different'}},'original'), /incomplete/);
  for (const status of [undefined,{}, {success:'false'}])
    assert.throws(() => verifiedHistoryTransaction({...tx,status},'original'), /incomplete/);
  for (const timestampMs of [null,undefined])
    assert.throws(() => verifiedHistoryTransaction({...tx,timestampMs},'original'), /incomplete/);
});

test('publication parses Move objects without treating package as a struct tag', () => {
  const packageId = '0x' + 'a'.repeat(64);
  const receipt = { digest: 'synthetic-parser-receipt', effects: { changedObjects: [
    { objectId: packageId, outputState: 'PackageWrite', idOperation: 'Created' } ] }, objectTypes: {
    [packageId]: 'package', storage: `${packageId}::storage::Storage`,
    currency: `${packageId}::token::Currency`, registry: `0x2::coin_registry::Currency<${packageId}::token::TOKEN>` } };
  assert.equal(publicationArtifacts(receipt).packageAddress, packageId);
  assert.equal(publicationArtifacts(receipt).registrationId, 'registry');
  delete receipt.objectTypes.storage;
  assert.throws(() => publicationArtifacts(receipt), /required OASIS objects/);
});

test('official SDK generates a recoverable canonical Sui wallet', async () => {
  const generated = await invoke({ operation: 'generateKey' });
  assert.match(generated.privateKey, /^suiprivkey1/);
  assert.match(generated.address, /^0x[0-9a-f]{64}$/);
  const restored = await invoke({ operation: 'restoreKey', privateKey: generated.privateKey });
  assert.deepEqual(restored, generated);
  const key = Ed25519Keypair.fromSecretKey(generated.privateKey);
  const data = new TextEncoder().encode('OASIS actual Sui signature evidence');
  assert.equal(await key.getPublicKey().verify(data, await key.sign(data)), true);
});

test('invalid credentials fail without manufacturing an address', async () => {
  await assert.rejects(invoke({ operation: 'restoreKey', privateKey: 'mainnet' }));
});

test('BIP39 account recovery uses the SDK Sui derivation path', async () => {
  const account = await invoke({ operation: 'createAccount' });
  assert.equal(account.seedPhrase.split(' ').length, 24);
  const restored = await invoke({ operation: 'restoreAccount', seedPhrase: account.seedPhrase });
  assert.equal(restored.address, account.address);
  assert.equal(restored.privateKey, account.privateKey);
  await assert.rejects(invoke({ operation: 'restoreAccount', seedPhrase: 'not a mnemonic' }));
});

test('unreachable gRPC cannot produce a successful probe', async () => {
  await assert.rejects(invoke({ operation: 'probe', rpcEndpoint: 'http://127.0.0.1:1', network: 'localnet' }));
});

test('transfer rejects the wrong signer and invalid u64 MIST before contacting a node', async () => {
  const key = await invoke({ operation: 'generateKey' });
  const other = await invoke({ operation: 'generateKey' });
  const request = { operation: 'transfer', privateKey: key.privateKey, fromWalletAddress: key.address,
    toWalletAddress: other.address, rpcEndpoint: 'http://127.0.0.1:1', network: 'localnet', amountUnits: '1' };
  await assert.rejects(invoke({ ...request, fromWalletAddress: other.address }), /does not own/);
  await assert.rejects(invoke({ ...request, amountUnits: '0' }), /Positive integral/);
  await assert.rejects(invoke({ ...request, amountUnits: '1.1' }), /Positive integral/);
  await assert.rejects(invoke({ ...request, amountUnits: '18446744073709551616' }), /exceeds u64/);
});
test('storage activation checks exact owner without paid mutation',async()=>{
 const {verifyStorageAuthority}=await import('./bridge.mjs');
 const owner='0x'+'1'.repeat(64),other='0x'+'2'.repeat(64);
 assert.doesNotThrow(()=>verifyStorageAuthority(owner,owner));
 for(const [actual,sender] of [[owner,other],[null,owner],[owner,null],['bad',owner],[owner,'bad']])
  assert.throws(()=>verifyStorageAuthority(actual,sender),/owner does not match/);
});
