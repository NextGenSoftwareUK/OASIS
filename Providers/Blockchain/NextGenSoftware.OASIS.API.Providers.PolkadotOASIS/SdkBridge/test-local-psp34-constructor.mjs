// Read-only native constructor simulation on the ONE retained local ledger.
// Never upload, instantiate, fund, sign or submit from this entry point.
import assert from 'node:assert/strict';
import { readFileSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { createHash } from 'node:crypto';
import { ApiPromise } from '@polkadot/api';
import { ReadOnlyHttpProvider } from './read-only-provider.mjs';
import { blake2AsHex } from '@polkadot/util-crypto';
import { encodeOasisPsp34Constructor } from './oasis-psp34-constructor.mjs';
import { nativePsp22Fixture, nativeTestTokenConstructor } from './native-psp22-test-token.mjs';

assert.equal(process.env.OASIS_POLKADOT_LOCAL_NATIVE, '1', 'Explicit retained local opt-in required.');
assert.ok(process.argv.length === 2 || (process.argv.length === 3 && ['--escrow', '--token-escrow', '--psp22-test-token'].includes(process.argv[2])),
  'Constructor simulation accepts only explicit escrow variants, never submission arguments.');
const escrow = process.argv[2] === '--escrow';
const tokenEscrow = process.argv[2] === '--token-escrow';
const testToken = process.argv[2] === '--psp22-test-token';
const prefix = testToken ? nativePsp22Fixture.prefix : tokenEscrow ? 'psp22-escrow' : escrow ? 'psp34-escrow' : 'psp34';
const root = resolve(import.meta.dirname, '../../../..');
const evidence = resolve(root, 'Docs/WIP/HyperDrive-Providers/NativeEvidence/Polkadot');
const cohort = JSON.parse(readFileSync(resolve(evidence, `${prefix}-wasm-current.json`), 'utf8'));
const artifact = testToken ? nativePsp22Fixture.artifact : tokenEscrow ? 'oasis-token-escrow' : `oasis-${prefix}`;
const code = readFileSync(resolve(root, `TestResults/PolkadotCurrent/record-contract-target/${artifact}.wasm`));
const sha = bytes => createHash('sha256').update(bytes).digest('hex');
assert.equal(sha(code), cohort.sha256);
assert.equal(blake2AsHex(code, 256), cohort.codeHash);
for (const [path, hash] of Object.entries(cohort.sources))
  assert.equal(sha(readFileSync(resolve(import.meta.dirname, '..', path))), hash, `Source cohort differs: ${path}`);
const api = await ApiPromise.create({ provider: new ReadOnlyHttpProvider('http://127.0.0.1:19944'), noInitWarn: true });
try {
  assert.equal(api.genesisHash.toHex(), '0xe8cf5d7d636a08721efaa17acc52ed3d895fe2468dccb2520cc8e249ddb6b208');
  const properties = await api.rpc.system.properties();
  assert.equal(properties.tokenDecimals.unwrap()[0].toString(), '12');
  assert.equal(properties.tokenSymbol.unwrap()[0].toString(), 'UNIT');
  const sender = '0x1a4fee48c1ba1a48e8cd43782a8485d635aa91cfb82cbb477f0c1c576bc4031c';
  const fields = { name: escrow ? 'OASIS retained beneficiary escrow' : 'OASIS retained PSP34 fixture', symbol: 'OASIS',
    description: 'Local native NFT acceptance; no public funds.', image: 'oasis://test/psp34' };
  const data = testToken ? nativeTestTokenConstructor(api.registry) : tokenEscrow ? '0x00000001' : encodeOasisPsp34Constructor(api.registry,
    Object.fromEntries(Object.entries(fields).map(([key, value]) => [key, Buffer.from(value, 'utf8')])));
  const salt = testToken ? nativePsp22Fixture.salt : tokenEscrow ? '0x6f617369732d746f6b656e2d657363726f772d7632' : escrow ? '0x6f617369732d70737033342d657363726f772d7632' : '0x6f617369732d70737033342d7631';
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
    artifactSha256: cohort.sha256, address, sender, data, salt, fields: tokenEscrow || testToken ? null : fields,
    ...(testToken ? { initialSupply: nativePsp22Fixture.supply } : {}),
    gasRequired: result.gasRequired.toJSON(), storageDeposit: result.storageDeposit.toJSON(),
    simulationPassed: true, nativeDeploymentVerified: false, signedOrSubmitted: false };
  writeFileSync(resolve(evidence, `${prefix}-constructor-simulation-current.json`), JSON.stringify(proof, null, 2) + '\n');
  console.log(JSON.stringify(proof));
} finally { await api.disconnect(); }
