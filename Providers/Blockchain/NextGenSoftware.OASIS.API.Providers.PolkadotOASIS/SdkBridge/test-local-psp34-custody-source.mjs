// Read-only proof against the retained original public transfer, never a replay.
import assert from 'node:assert/strict';
import { readFileSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { createHash } from 'node:crypto';
import { ApiPromise } from '@polkadot/api';
import { ReadOnlyHttpProvider } from './read-only-provider.mjs';
import { readNftCustodyReceipt } from './psp34-contract-outcome.mjs';
assert.equal(process.env.OASIS_POLKADOT_LOCAL_NATIVE, '1');assert.equal(process.argv.length, 2);
const folder = resolve(import.meta.dirname, '../../../../Docs/WIP/HyperDrive-Providers/NativeEvidence/Polkadot');
const completed = JSON.parse(readFileSync(resolve(folder, 'psp34-public-mutation.completed.json'), 'utf8'));
const deployment = JSON.parse(readFileSync(resolve(folder, 'psp34-deployment.completed.json'), 'utf8'));
const original = completed.receipts.find(receipt => receipt.action === 'transfer');
assert.ok(original);assert.equal(original.contractAddress, deployment.address);
const api = await ApiPromise.create({ provider: new ReadOnlyHttpProvider('http://127.0.0.1:19944'), noInitWarn: true });
try {
  assert.equal(api.genesisHash.toHex(), '0xe8cf5d7d636a08721efaa17acc52ed3d895fe2468dccb2520cc8e249ddb6b208');
  const request = { transactionHash: original.transactionHash, blockHash: original.blockHash,
    contractAddress: original.contractAddress, contractCodeHash: deployment.codeHash,
    tokenId: original.tokenId, custody: original.to };
  const proof = await readNftCustodyReceipt(api, request);
  assert.equal(proof.custodyReceiptVerified, true);assert.equal(proof.from, original.from);
  assert.equal(proof.to, original.to);assert.deepEqual(proof.tokenId, original.tokenId);
  for (const altered of [{ custody: original.from }, { tokenId: { U8: '1' } },
    { contractCodeHash: '0x' + '01'.repeat(32) }]) await assert.rejects(readNftCustodyReceipt(api, { ...request, ...altered }));
  writeFileSync(resolve(folder, 'psp34-custody-source-native-current.json'), JSON.stringify({
    proof, alteredPoolTokenCodeRejected: true, submitted: false, signingKeysUsed: false,
    sourceSha256: Object.fromEntries(['psp34-contract-outcome.mjs', 'transaction-status.mjs', 'finalized-receipt.mjs'].map(name =>
      [name, createHash('sha256').update(readFileSync(resolve(import.meta.dirname, name))).digest('hex')])),
    scope: 'historical local custody entry only; does not prove present ownership or cross-chain authorization'
  }, null, 2) + '\n');
  console.log('PASS retained original custody receipt and three substitutions; no signing/submission.');
} finally { await api.disconnect(); }
