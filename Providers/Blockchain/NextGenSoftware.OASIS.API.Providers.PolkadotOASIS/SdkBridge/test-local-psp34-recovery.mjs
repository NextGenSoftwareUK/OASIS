// Read-only recovery using ONLY the durable owner's common signed fields.
import assert from 'node:assert/strict';
import { readFileSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { createHash } from 'node:crypto';
import { ApiPromise } from '@polkadot/api';
import { ReadOnlyHttpProvider } from './read-only-provider.mjs';
import { recoverNftPreparation } from './psp34-contract-mutation.mjs';
import { readOriginalNftOutcome } from './psp34-contract-outcome.mjs';
assert.equal(process.env.OASIS_POLKADOT_LOCAL_NATIVE, '1');assert.equal(process.argv.length, 2);
const folder = resolve(import.meta.dirname, '../../../../Docs/WIP/HyperDrive-Providers/NativeEvidence/Polkadot');
const fixture = JSON.parse(readFileSync(resolve(folder, 'psp34-mutation.completed.json'), 'utf8'));
const api = await ApiPromise.create({ provider: new ReadOnlyHttpProvider('http://127.0.0.1:19944'), noInitWarn: true });
try {
  assert.equal(api.genesisHash.toHex(), '0xe8cf5d7d636a08721efaa17acc52ed3d895fe2468dccb2520cc8e249ddb6b208');
  const proofs = [];
  for (let i = 0; i < fixture.originals.length; i++) {
    const original = fixture.originals[i];
    const minimum = Object.fromEntries(['chainId', 'sender', 'transactionHash', 'signedTransactionBase64', 'signDocBase64', 'signature']
      .map(key => [key, original[key]]));
    const recovered = await recoverNftPreparation(api, { preparation: minimum, contractCodeHash: fixture.codeHash });
    for (const field of ['action', 'tokenId', 'to', 'attributes', 'gasLimit', 'storageDepositLimit', 'transactionHash'])
      assert.deepEqual(recovered[field], original[field], `Recovered ${field} differs from signed intent.`);
    assert.equal(recovered.sourceOwnerPreconditionRetained, false);assert.equal(recovered.reconciliationRequired, true);
    assert.equal(Object.hasOwn(recovered, 'expectedFrom'), false);
    const outcome = await readOriginalNftOutcome(api, { preparation: recovered, maximumBlocks: 32 });
    assert.equal(outcome.found, true);assert.equal(outcome.receipt.nftEffectMatchesRequest, true);
    assert.equal(outcome.receipt.actualFeeUnits, fixture.receipts[i].actualFeeUnits);
    assert.equal(outcome.receipt.from, fixture.receipts[i].from);assert.equal(outcome.receipt.to, fixture.receipts[i].to);
    const alteredSidecar = await recoverNftPreparation(api,
      { preparation: { ...minimum, action: 'untrusted', tokenId: { U8: '0' } }, contractCodeHash: fixture.codeHash });
    assert.equal(alteredSidecar.action, original.action);assert.deepEqual(alteredSidecar.tokenId, original.tokenId);
    await assert.rejects(recoverNftPreparation(api, { preparation: minimum, contractCodeHash: '0x' + '01'.repeat(32) }), /code differs/);
    await assert.rejects(recoverNftPreparation(api, { preparation: { ...minimum, transactionHash: '0x' + '01'.repeat(32) },
      contractCodeHash: fixture.codeHash }), /transaction hash/);
    proofs.push({ transactionHash: original.transactionHash, action: original.action, actualFeeUnits: outcome.receipt.actualFeeUnits,
      minimumFieldsOnly: true, finalizedEffectsVerified: true, sourceOwnerPreconditionRetained: false });
  }
  const result = { proofs, submitted: false, signingKeysUsed: false, newEnvironment: false,
    sourceSha256: Object.fromEntries(['psp34-contract-mutation.mjs', 'psp34-contract-outcome.mjs', 'oasis-psp34-mutation-codec.mjs', 'test-local-psp34-recovery.mjs']
      .map(name => [name, createHash('sha256').update(readFileSync(resolve(import.meta.dirname, name))).digest('hex')])) };
  writeFileSync(resolve(folder, 'psp34-recovery-native-current.json'), JSON.stringify(result, null, 2) + '\n');
  console.log(JSON.stringify(result));
} finally { await api.disconnect(); }
