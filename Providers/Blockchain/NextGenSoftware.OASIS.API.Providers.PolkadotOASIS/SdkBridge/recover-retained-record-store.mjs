// Read-only recovery of the EXISTING store identity after disposable test output
// was deleted. Never reconstructs a purported original preparation, signs,
// funds, uploads, redeploys or resets a ledger.
import assert from 'node:assert/strict';
import { ApiPromise } from '@polkadot/api';
import { ReadOnlyHttpProvider } from './read-only-provider.mjs';
import { readFinalizedExtrinsicContext, readFinalizedReceipt } from './finalized-receipt.mjs';
import { existsSync, readFileSync, mkdirSync, openSync, writeSync, fsyncSync, closeSync } from 'node:fs';
import { resolve } from 'node:path';
import { createHash } from 'node:crypto';

const genesis = '0xe8cf5d7d636a08721efaa17acc52ed3d895fe2468dccb2520cc8e249ddb6b208';
const deployment = '0x6afcebc9ffa0e24fa8e7028a4f3d0e4c4db5101e108cc810ac5372de58719c90';
const expectedCode = '0x7c102aaaaef5fec4e9f8f035c0542c7c6ca23f8c4e6b8de5e0498862debef0fa';
const expectedOwner = '0x1a4fee48c1ba1a48e8cd43782a8485d635aa91cfb82cbb477f0c1c576bc4031c';
const directory = resolve(import.meta.dirname, '../../../../Docs/WIP/HyperDrive-Providers/NativeEvidence/Polkadot');
const path = resolve(directory, 'reusable-record-store.json');
const api = await ApiPromise.create({ provider: new ReadOnlyHttpProvider('http://127.0.0.1:19944'), noInitWarn: true });
try {
  assert.equal(api.genesisHash.toHex(), genesis);
  const contract = api.createType('AccountId32', '5EJJyBw7E9voUitpEsv35Qja6ykUiZEwcpNMqrFH8dGvZaED').toHex();
  const finalized = await api.rpc.chain.getFinalizedHead();
  const tip = (await api.rpc.chain.getHeader(finalized)).number.toBigInt();
  assert.ok(tip <= 10000n, 'Explicit recovery scan bound exceeded; no partial-history success.');
  let blockHash, original;
  for (let number = 0n; number <= tip; number++) {
    const hash = await api.rpc.chain.getBlockHash(number.toString());
    const block = await api.rpc.chain.getBlock(hash);
    const found = block.block.extrinsics.filter(tx => tx.hash.toHex() === deployment);
    if (found.length) { assert.equal(found.length, 1); blockHash = hash.toHex(); original = found[0]; break; }
  }
  assert.ok(original, 'Known original deployment not found; preserve ledger, never redeploy.');
  assert.equal(original.isSigned, true);
  assert.equal(api.createType('AccountId32', original.signer.toString()).toHex(), expectedOwner);
  assert.equal(original.method.section, 'contracts'); assert.equal(original.method.method, 'instantiate');
  assert.equal(original.method.args[0].toBigInt(), 0n);
  assert.equal(original.method.args[3].toHex(), expectedCode);
  assert.equal(original.method.args[4].toHex(), '0x00000001');
  assert.equal(original.method.args[5].toHex(), '0x6f617369732d7265636f7264732d73646b2d7631');
  const identifiers = { blockHash, transactionHash: deployment };
  const context = await readFinalizedExtrinsicContext(api, identifiers);
  const receipt = await readFinalizedReceipt(api, identifiers);
  assert.equal(receipt.dispatchSucceeded, true);
  assert.equal(api.createType('AccountId32', receipt.payer).toHex(), expectedOwner);
  const instances = context.own.filter(event => event.section === 'contracts' && event.method === 'Instantiated');
  assert.equal(instances.length, 1);
  assert.equal(api.createType('AccountId32', instances[0].data[0]).toHex(), expectedOwner);
  assert.equal(api.createType('AccountId32', instances[0].data[1]).toHex(), contract);
  const at = await api.at(finalized);
  assert.equal((await at.query.contracts.contractInfoOf(contract)).unwrap().codeHash.toHex(), expectedCode);
  assert.equal(typeof at.query.contracts.pristineCode, 'function', 'Retained runtime must expose its original code bytes.');
  const wasm = (await at.query.contracts.pristineCode(expectedCode)).unwrap().toU8a(true);
  assert.equal(api.registry.hash(wasm).toHex(), expectedCode);
  const artifactSha = createHash('sha256').update(wasm).digest('hex');
  assert.equal(artifactSha, '16aafeccc5c9ee3d40b994bc9f9307a04ef1cee68ce5e9cbcc05d1ce94108ed3');
  const evidence = { protocol: 'oasis.polkadot.recovered-native-store.v1', genesisHash: genesis,
    contractAccountId: contract, contractCodeHash: expectedCode, ownerAccountId: expectedOwner,
    recoveredFromFinalizedChain: true, originalPreparationFileRecovered: false,
    deployedArtifact: { bytes: wasm.length, sha256: artifactSha, wasmBase64: Buffer.from(wasm).toString('base64') },
    originalDeployment: { ...identifiers, signedExtrinsicBase64: Buffer.from(original.toU8a()).toString('base64'), receipt,
      events: context.own.map(event => ({ section: event.section, method: event.method, data: event.data.toJSON() })) },
    submitted: false, redeployed: false };
  mkdirSync(directory, { recursive: true });
  if (existsSync(path)) {
    const retained = JSON.parse(readFileSync(path, 'utf8'));
    assert.deepEqual(retained, evidence,
      'Retained recovery evidence differs from independently observed finalized chain evidence; preserve both for review.');
  } else {
    const fd = openSync(path, 'wx', 0o600);
    try { writeSync(fd, JSON.stringify(evidence, null, 2) + '\n'); fsyncSync(fd); } finally { closeSync(fd); }
  }
  console.log(JSON.stringify({ path, contractAccountId: contract, ownerAccountId: expectedOwner,
    codeHash: expectedCode, originalDeployment: deployment, recoveredReadOnly: true, submitted: false, redeployed: false }));
} finally { await api.disconnect(); }
