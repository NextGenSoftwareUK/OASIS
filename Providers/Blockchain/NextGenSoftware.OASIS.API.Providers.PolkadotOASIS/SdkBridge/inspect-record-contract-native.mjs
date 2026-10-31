// Read-only local runtime simulation. Never signs/submits/uploads code.
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { ApiPromise, HttpProvider } from '@polkadot/api';
import { registerRecordContractTypes, encodeRecordMessage } from './record-contract-codec.mjs';

const api = await ApiPromise.create({ provider: new HttpProvider('http://127.0.0.1:19944'), noInitWarn: true });
try {
  assert.equal(api.genesisHash.toHex(), '0xe8cf5d7d636a08721efaa17acc52ed3d895fe2468dccb2520cc8e249ddb6b208');
  assert.equal(typeof api.call.contractsApi?.instantiate, 'function', 'This runtime must expose pallet-contracts simulation.');
  registerRecordContractTypes(api.registry);
  const artifact = readFileSync(resolve(import.meta.dirname, '../../../../TestResults/PolkadotCurrent/record-contract-target/oasis-records.wasm'));
  const origin = '0xd43593c715fdd31c61141abd04a99fd6822c8558854ccde39a5684e7a56da27d'; // public local Alice
  const outcome = await api.call.contractsApi.instantiate(origin, 0, null, null,
    { Upload: '0x' + artifact.toString('hex') }, encodeRecordMessage(api.registry, 'new', []), '0x');
  if (outcome.result.isErr) {
    const error = outcome.result.asErr;
    const detail = error.isModule ? api.registry.findMetaError(error.asModule) : null;
    throw new Error(`Native instantiation simulation rejected artifact: ${detail ? `${detail.section}.${detail.name}: ${detail.docs.join(' ')}` : error.toString()}; ${outcome.debugMessage.toUtf8().slice(0, 2048)}`);
  }
  const result = outcome.result.asOk;
  assert.equal(result.result.flags.get('bits').toNumber(), 0, 'Constructor returned revert/unknown flags.');
  console.log(JSON.stringify({ genesis: api.genesisHash.toHex(), specVersion: api.runtimeVersion.specVersion.toString(),
    codeHash: api.registry.hash(artifact).toHex(), simulatedAddress: result.accountId.toString(),
    gasRequired: outcome.gasRequired.toJSON(), storageDeposit: outcome.storageDeposit.toJSON(),
    nativeSimulationPassed: true, submitted: false, deployed: false }));
} finally { await api.disconnect(); }
