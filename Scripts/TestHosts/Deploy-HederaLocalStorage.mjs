import assert from 'node:assert/strict';
import { readFileSync, writeFileSync } from 'node:fs';
import { createRequire } from 'node:module';
import { resolve } from 'node:path';
import { execute } from '../../Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.HashgraphOASIS/SdkBridge/bridge.mjs';

const fixture = process.argv[2];
assert.equal(process.env.GITHUB_ACTIONS, 'true');
assert.ok(fixture?.startsWith(resolve(process.env.RUNNER_TEMP) + '/oasis-hedera.'));
const compiler = createRequire(resolve(fixture, 'package.json'))('solc');
const requireSdk = createRequire(new URL(
  '../../Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.HashgraphOASIS/SdkBridge/package.json', import.meta.url));
const { TransactionId } = requireSdk('@hiero-ledger/sdk');
const source = readFileSync(new URL('../../Providers/Shared/Contracts/OASISKeyValueStore.sol', import.meta.url), 'utf8');
const compiled = JSON.parse(compiler.compile(JSON.stringify({
  language: 'Solidity',
  sources: { 'Store.sol': { content: source } },
  settings: { evmVersion: 'paris', outputSelection: { '*': { '*': ['evm.bytecode.object'] } } }
})));
assert.deepEqual((compiled.errors ?? []).filter(error => error.severity === 'error'), []);
const bytecode = compiled.contracts['Store.sol'].OASISKeyValueStore.evm.bytecode.object;
assert.match(bytecode, /^(?:[0-9a-f]{2})+$/);

// The official local-node genesis identity is valid only on this loopback fixture.
const request = {
  network: 'local',
  nodes: { '127.0.0.1:50211': '0.0.3' },
  sender: '0.0.2',
  keyType: 'der',
  privateKey: process.env.OASIS_HEDERA_LOCAL_SIGNING_KEY,
  gas: 4_000_000,
  maxFeeTinybars: '10000000000'
};

async function submit(action, data) {
  const transactionId = TransactionId.generate(request.sender).toString();
  const result = await execute(action, { ...request, ...data, transactionId });
  assert.equal(result.transactionId, transactionId);
  assert.equal(result.status, 'SUCCESS');
  return result;
}

const file = await submit('bytecodeFileCreate', { contents: bytecode.slice(0, 1024) });
assert.match(file.fileId, /^0\.0\.[1-9][0-9]*$/);
for (let offset = 1024; offset < bytecode.length; offset += 1024) {
  await submit('bytecodeFileAppend', { fileId: file.fileId, contents: bytecode.slice(offset, offset + 1024) });
}
const deployment = await submit('contractCreate', { fileId: file.fileId });
assert.match(deployment.contractId, /^0\.0\.[1-9][0-9]*$/);
const storage = { ...request, contractId: deployment.contractId };
const probe = await execute('storageProbe', storage);
assert.equal(probe.contractId, deployment.contractId);
assert.equal(probe.sender, request.sender);
assert.deepEqual(await execute('storageList', { ...storage, prefix: '' }), []);
const evidence = JSON.stringify({ contractId: deployment.contractId, deploymentTransaction: deployment.transactionId, emptyStorage: true });
writeFileSync(resolve(fixture, 'deployment.json'), evidence, { flag: 'wx' });
console.log(evidence);
