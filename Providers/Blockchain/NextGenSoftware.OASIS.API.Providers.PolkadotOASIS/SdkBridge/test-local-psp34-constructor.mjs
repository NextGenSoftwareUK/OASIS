// Read-only native constructor simulation on the ONE retained local ledger.
// Never upload, instantiate, fund, sign or submit from this entry point.
import assert from 'node:assert/strict';
import { readFileSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { createHash } from 'node:crypto';
import { ApiPromise, HttpProvider } from '@polkadot/api';
import { blake2AsHex } from '@polkadot/util-crypto';
import { encodeOasisPsp34Constructor } from './oasis-psp34-constructor.mjs';

assert.equal(process.env.OASIS_POLKADOT_LOCAL_NATIVE, '1', 'Explicit retained local opt-in required.');
assert.equal(process.argv.length, 2, 'Constructor simulation accepts no submission arguments.');
const root = resolve(import.meta.dirname, '../../../..');
const evidence = resolve(root, 'Docs/WIP/HyperDrive-Providers/NativeEvidence/Polkadot');
const cohort = JSON.parse(readFileSync(resolve(evidence, 'psp34-wasm-current.json'), 'utf8'));
const code = readFileSync(resolve(root, 'TestResults/PolkadotCurrent/record-contract-target/oasis-psp34.wasm'));
const sha = bytes => createHash('sha256').update(bytes).digest('hex');
assert.equal(sha(code), cohort.sha256);
assert.equal(blake2AsHex(code, 256), cohort.codeHash);
for (const [path, hash] of Object.entries(cohort.sources))
  assert.equal(sha(readFileSync(resolve(import.meta.dirname, '..', path))), hash, `Source cohort differs: ${path}`);
const api = await ApiPromise.create({ provider: new HttpProvider('http://127.0.0.1:19944'), noInitWarn: true });
try {
  assert.equal(api.genesisHash.toHex(), '0xe8cf5d7d636a08721efaa17acc52ed3d895fe2468dccb2520cc8e249ddb6b208');
  const properties = await api.rpc.system.properties();
  assert.equal(properties.tokenDecimals.unwrap()[0].toString(), '12');
  assert.equal(properties.tokenSymbol.unwrap()[0].toString(), 'UNIT');
  const sender = '0x1a4fee48c1ba1a48e8cd43782a8485d635aa91cfb82cbb477f0c1c576bc4031c';
  const fields = { name: 'OASIS retained PSP34 fixture', symbol: 'OASIS',
    description: 'Local native NFT acceptance; no public funds.', image: 'oasis://test/psp34' };
  const data = encodeOasisPsp34Constructor(api.registry,
    Object.fromEntries(Object.entries(fields).map(([key, value]) => [key, Buffer.from(value, 'utf8')])));
  const salt = '0x6f617369732d70737033342d7631';
  const finalized = await api.rpc.chain.getFinalizedHead();
  const at = await api.at(finalized);
  const result = await at.call.contractsApi.instantiate(sender, 0, null, null,
    { Upload: '0x' + code.toString('hex') }, data, salt);
  assert.equal(result.result.isOk, true, result.result.toString());
  const output = result.result.asOk;
  assert.equal(output.result.flags.get('bits').toNumber(), 0, 'Constructor reverted.');
  const address = api.createType('AccountId32', output.accountId).toHex();
  assert.equal((await at.query.contracts.contractInfoOf(address)).isNone, true,
    'Untracked fixture already exists; investigate instead of deploying again.');
  const proof = { genesisHash: api.genesisHash.toHex(), blockHash: finalized.toHex(),
    runtimeSpecVersion: api.runtimeVersion.specVersion.toString(), codeHash: cohort.codeHash,
    artifactSha256: cohort.sha256, address, sender, data, salt, fields,
    gasRequired: result.gasRequired.toJSON(), storageDeposit: result.storageDeposit.toJSON(),
    simulationPassed: true, nativeDeploymentVerified: false, signedOrSubmitted: false };
  writeFileSync(resolve(evidence, 'psp34-constructor-simulation-current.json'), JSON.stringify(proof, null, 2) + '\n');
  console.log(JSON.stringify(proof));
} finally { await api.disconnect(); }
