// Native simulation only. Transport cannot submit; no new original is signed.
import assert from 'node:assert/strict';
import { readFileSync, writeFileSync, existsSync } from 'node:fs';
import { resolve } from 'node:path';
import { ApiPromise } from '@polkadot/api';
import { ReadOnlyHttpProvider } from './read-only-provider.mjs';
import { readNativeTestToken } from './native-psp22-test-token.mjs';
import { readTokenEscrowDeposit } from './psp22-escrow-read.mjs';
import { prepareTokenEscrowMutation } from './psp22-escrow-mutation.mjs';
assert.equal(process.env.OASIS_POLKADOT_LOCAL_NATIVE, '1'); assert.equal(process.argv.length, 2);
const folder = resolve(import.meta.dirname, '../../../../Docs/WIP/HyperDrive-Providers/NativeEvidence/Polkadot');
for (const name of ['psp22-test-token-deployment-pending.json', 'psp22-escrow-deployment-pending.json', 'psp22-escrow-lifecycle-pending.jsonl'])
  assert.equal(existsSync(resolve(folder, name)), false, 'Unresolved original must be reconciled first.');
const token = JSON.parse(readFileSync(resolve(folder, 'psp22-test-token-deployment.completed.json'), 'utf8'));
const escrow = JSON.parse(readFileSync(resolve(folder, 'psp22-escrow-deployment.completed.json'), 'utf8'));
const api = await ApiPromise.create({ provider: new ReadOnlyHttpProvider('http://127.0.0.1:19944'), noInitWarn: true });
try {
  assert.equal(api.genesisHash.toHex(), '0xe8cf5d7d636a08721efaa17acc52ed3d895fe2468dccb2520cc8e249ddb6b208');
  const query = { contractAddress: token.address, contractCodeHash: token.codeHash, origin: token.sender,
    owner: token.sender, spender: escrow.address };
  const before = await readNativeTestToken(api, query);
  assert.equal(before.allowance, '0'); assert.equal(before.spenderBalance, '0');
  const request = { action: 'lock', contractAddress: escrow.address, contractCodeHash: escrow.codeHash,
    sender: token.sender, recipient: escrow.address, tokenAddress: token.address,
    depositId: '0x' + '11'.repeat(32), amountUnits: '18446744073709551620', privateKey: '0x' + '02'.repeat(32) };
  let error;
  try { await prepareTokenEscrowMutation(api, request); } catch (failure) { error = failure; }
  const after = await readNativeTestToken(api, query);
  assert.deepEqual(after, before);
  const deposit = await readTokenEscrowDeposit(api, { ...request, origin: token.sender });
  assert.equal(deposit.deposit, null);
  const structuredErrorVerified = error instanceof Error && error.message.includes('Escrow application failure: TokenCallFailed');
  const proof = { before, after, deposit, error: error?.message, structuredErrorVerified,
    submitted: false, signedOriginalCreated: false, publicFunds: false,
    scope: 'native insufficient-allowance simulation and return ABI; not persisted native rollback acceptance' };
  writeFileSync(resolve(folder, 'psp22-escrow-preparation-native-current.json'), JSON.stringify(proof, null, 2) + '\n');
  console.log(JSON.stringify(proof));
  assert.equal(structuredErrorVerified, true, 'Native simulation must return the reviewed structured escrow application error.');
} finally { await api.disconnect(); }
