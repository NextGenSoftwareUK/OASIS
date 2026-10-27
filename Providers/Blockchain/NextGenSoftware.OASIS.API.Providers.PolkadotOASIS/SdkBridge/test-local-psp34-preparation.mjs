// Native read/sign/verify preparation only; no author_* capability or submission.
import assert from 'node:assert/strict';
import { readFileSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { createHash } from 'node:crypto';
import { ApiPromise } from '@polkadot/api';
import { ReadOnlyHttpProvider } from './read-only-provider.mjs';
import { prepareNftMutation, verifyNftPreparation } from './psp34-contract-mutation.mjs';
import { openPinnedContractQuery } from './pinned-contract-query.mjs';
import { encodePsp34Message, decodePsp34Result } from './psp34-contract-codec.mjs';

assert.equal(process.env.OASIS_POLKADOT_LOCAL_NATIVE, '1', 'Explicit retained local opt-in required.');
assert.equal(process.argv.length, 2);
const folder = resolve(import.meta.dirname, '../../../../Docs/WIP/HyperDrive-Providers/NativeEvidence/Polkadot');
const fixture = JSON.parse(readFileSync(resolve(folder, 'psp34-deployment.completed.json'), 'utf8'));
const api = await ApiPromise.create({ provider: new ReadOnlyHttpProvider('http://127.0.0.1:19944'), noInitWarn: true });
try {
  assert.equal(api.genesisHash.toHex(), '0xe8cf5d7d636a08721efaa17acc52ed3d895fe2468dccb2520cc8e249ddb6b208');
  const tokenId = { U128: '18446744073709551617' };
  const request = { action: 'mint', contractAddress: fixture.address, contractCodeHash: fixture.codeHash,
    tokenId, to: fixture.sender, attributes: [['bmFtZQ==', 'UmV0YWluZWQgbmF0aXZlIE5GVA==']],
    sender: fixture.sender, recipient: fixture.address, privateKey: '0x' + '02'.repeat(32) }; // Existing public local fixture only.
  const owner = async () => {
    const view = await openPinnedContractQuery(api, { ...request, origin: fixture.sender });
    return decodePsp34Result(api.registry, 'ownerOf', await view.query(encodePsp34Message(api.registry, 'ownerOf', [tokenId])));
  };
  assert.equal((await owner()).isNone, true, 'Do not reuse a token from an untracked native mutation.');
  const prepared = await prepareNftMutation(api, request);
  const verified = await verifyNftPreparation(api, prepared);
  assert.equal(verified.action, 'mint');assert.equal(verified.sender, fixture.sender);
  assert.equal(verified.contractAddress, fixture.address);assert.deepEqual(verified.tokenId, tokenId);
  assert.equal(prepared.submitted, false);
  assert.match(prepared.partialFeeUnits, /^(0|[1-9][0-9]*)$/);
  assert.equal(Object.hasOwn(prepared, 'privateKey'), false);
  let rejected = 0;
  for (const change of [value => value.tokenId = { U128: '18446744073709551618' },
    value => value.to = '0x' + '01'.repeat(32), value => value.attributes = [],
    value => value.action = 'burn', value => value.gasLimit.refTime = '1',
    value => value.storageDepositLimit = '0', value => value.contractCodeHash = '0x' + '01'.repeat(32)]) {
    const candidate = structuredClone(prepared);change(candidate);
    await assert.rejects(verifyNftPreparation(api, candidate));rejected++;
  }
  assert.equal((await owner()).isNone, true, 'Preparation cannot mint a token.');
  const proof = { genesisHash: api.genesisHash.toHex(), contractAddress: fixture.address, codeHash: fixture.codeHash,
    tokenId, preparedTransactionHash: prepared.transactionHash, partialFeeUnits: prepared.partialFeeUnits,
    gasLimit: prepared.gasLimit, storageDepositLimit: prepared.storageDepositLimit,
    tamperedIntentsRejected: rejected, signedPreparationVerified: true,
    submitted: false, tokenStillAbsent: true, publicMutationApiWired: false,
    feeCoverage: 'estimate-and-execution-limits-not-total-fee-cap',
    sourceSha256: Object.fromEntries(['psp34-contract-mutation.mjs', 'oasis-psp34-mutation-codec.mjs', 'psp34-contract-codec.mjs', 'test-local-psp34-preparation.mjs']
      .map(name => [name, createHash('sha256').update(readFileSync(resolve(import.meta.dirname, name))).digest('hex')])) };
  writeFileSync(resolve(folder, 'psp34-preparation-native-current.json'), JSON.stringify(proof, null, 2) + '\n');
  console.log(JSON.stringify(proof));
} finally { await api.disconnect(); }
