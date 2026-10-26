// Final read-only state proof for the retained public API lifecycle.
import assert from 'node:assert/strict';
import { readFileSync, existsSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { ApiPromise } from '@polkadot/api';
import { ReadOnlyHttpProvider } from './read-only-provider.mjs';
import { openPinnedContractQuery } from './pinned-contract-query.mjs';
import { encodePsp34Message, decodePsp34Result } from './psp34-contract-codec.mjs';
import { nftTokenId } from './psp34-contract-mutation.mjs';
assert.equal(process.env.OASIS_POLKADOT_LOCAL_NATIVE, '1');assert.equal(process.argv.length, 2);
const folder = resolve(import.meta.dirname, '../../../../Docs/WIP/HyperDrive-Providers/NativeEvidence/Polkadot');
assert.equal(existsSync(resolve(folder, 'psp34-public-mutation-pending.jsonl')), false);
const deployment = JSON.parse(readFileSync(resolve(folder, 'psp34-deployment.completed.json'), 'utf8'));
const completed = JSON.parse(readFileSync(resolve(folder, 'psp34-public-mutation.completed.json'), 'utf8'));
const ids = [...new Set(completed.nativeTokenIds.map(value => JSON.stringify(value)))].map(value => JSON.parse(value));
assert.equal(ids.length, 2);
if (existsSync(resolve(folder, 'psp34-custody.completed.json'))) {
  assert.equal(existsSync(resolve(folder, 'psp34-custody-pending.jsonl')), false);
  const custody = JSON.parse(readFileSync(resolve(folder, 'psp34-custody.completed.json'), 'utf8'));
  assert.equal(custody.records.length, 4);
  const id = JSON.parse(custody.records[0].TokenId);
  assert.equal(ids.some(value => JSON.stringify(value) === JSON.stringify(id)), false);
  ids.push(id);
}
const api = await ApiPromise.create({ provider: new ReadOnlyHttpProvider('http://127.0.0.1:19944'), noInitWarn: true });
try {
  assert.equal(api.genesisHash.toHex(), '0xe8cf5d7d636a08721efaa17acc52ed3d895fe2468dccb2520cc8e249ddb6b208');
  const view = await openPinnedContractQuery(api, { contractAddress: deployment.address,
    contractCodeHash: deployment.codeHash, origin: deployment.sender });
  const query = async (method, args) => decodePsp34Result(api.registry, method, await view.query(encodePsp34Message(api.registry, method, args)));
  for (const id of ids) assert.equal((await query('ownerOf', [nftTokenId(id)])).isNone, true);
  assert.equal((await query('totalSupply', [])).toString(), '0');
  const proof = { ...view.identity, tokenIds: ids, allOwnersAbsent: true, totalSupply: '0', submitted: false };
  writeFileSync(resolve(folder, 'psp34-public-state-current.json'), JSON.stringify(proof, null, 2) + '\n');
  console.log(JSON.stringify(proof));
} finally { await api.disconnect(); }
