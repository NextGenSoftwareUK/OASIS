import assert from 'node:assert/strict';
import { readFileSync, writeFileSync, existsSync } from 'node:fs';
import { resolve } from 'node:path';
import { ApiPromise } from '@polkadot/api';
import { ReadOnlyHttpProvider } from './read-only-provider.mjs';
import { readPsp34EscrowState } from './psp34-contract-read.mjs';
assert.equal(process.env.OASIS_POLKADOT_LOCAL_NATIVE, '1');assert.equal(process.argv.length, 2);
const folder = resolve(import.meta.dirname, '../../../../Docs/WIP/HyperDrive-Providers/NativeEvidence/Polkadot');
for (const name of ['psp34-escrow-deployment-pending.json', 'psp34-escrow-lifecycle-pending.jsonl'])
  assert.equal(existsSync(resolve(folder, name)), false, 'Unresolved originals prevent final-state certification.');
const deployed = JSON.parse(readFileSync(resolve(folder, 'psp34-escrow-deployment.completed.json'), 'utf8'));
assert.equal(deployed.nativeDeploymentVerified, true);
const api = await ApiPromise.create({ provider: new ReadOnlyHttpProvider('http://127.0.0.1:19944'), noInitWarn: true });
try {
  assert.equal(api.genesisHash.toHex(), '0xe8cf5d7d636a08721efaa17acc52ed3d895fe2468dccb2520cc8e249ddb6b208');
  const state = await readPsp34EscrowState(api, { contractAddress: deployed.address, contractCodeHash: deployed.codeHash,
    origin: deployed.sender, tokenId: { U128: '18446744073709551620' } });
  assert.equal(state.owner, null);assert.equal(state.escrowBeneficiary, null);assert.equal(state.totalSupply, '0');
  const proof = { ...state, submitted: false, signingKeysUsed: false, nativeBeneficiaryQueryVerified: true,
    scope: 'empty token/beneficiary and supply at this finalized snapshot; not lock/unlock lifecycle acceptance' };
  writeFileSync(resolve(folder, 'psp34-escrow-state-current.json'), JSON.stringify(proof, null, 2) + '\n');
  console.log(JSON.stringify(proof));
} finally { await api.disconnect(); }
