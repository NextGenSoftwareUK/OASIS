// One-time admission of a separate demo package on the retained local chain.
// Existing identities, signed attempts and deployments are never replaced.
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { createHash } from 'node:crypto';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');
const state = path.join(root, 'LocalState/COSMIC/SuiCql');
const originalPath = '\\\\wsl.localhost\\Ubuntu\\var\\tmp\\oasis-sui\\deployment.json';
const originalBytes = fs.readFileSync(originalPath);
const original = JSON.parse(originalBytes);
const packageBytes = fs.readFileSync(path.join(root, 'TestResults/SuiRuntime/storage-package.json'));
const compiled = JSON.parse(packageBytes);
const hash = bytes => createHash('sha256').update(bytes).digest('hex');
const exists = name => fs.existsSync(path.join(state, name));
const persist = (name, value) => {
  const fd = fs.openSync(path.join(state, name), 'wx', 0o600);
  try { fs.writeFileSync(fd, JSON.stringify(value)); fs.fsyncSync(fd); }
  finally { fs.closeSync(fd); }
};

fs.mkdirSync(state, { recursive: true });
if (['publication.pending.json', 'publication.receipt.json', 'demo-deployment.json', 'publication.admitted.json']
  .some(exists) || fs.readdirSync(state).some(name => name.startsWith('publication-prepared-')))
  throw new Error('Original publication evidence exists; preserve and reconcile, never replay.');
const bridge = path.join(root, 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.SuiOASIS/SdkBridge/bridge.mjs');
const { invoke, verifyPreparedIdentity } = await import(pathToFileURL(bridge).href);
let preparedCount = 0;
persist('publication.pending.json', {
  chainId: original.chainId, sender: original.address,
  originalDeploymentSha256: hash(originalBytes), compiledPackageSha256: hash(packageBytes),
  purpose: 'COSMIC CQL dedicated source; original Storage remains untouched',
});
try {
  const result = await invoke({ operation: 'publish', rpcEndpoint: 'http://127.0.0.1:9000',
    network: 'localnet', chainId: original.chainId, privateKey: original.privateKey,
    modules: compiled.modules, dependencies: compiled.dependencies, maximumGasBudgetMist: '100000000',
  }, async evidence => {
    await verifyPreparedIdentity(evidence);
    if (evidence.sender !== original.address || evidence.chainId !== original.chainId)
      throw new Error('Prepared publication differs from the retained publisher/chain.');
    persist(`publication-prepared-${preparedCount++}.json`, evidence);
  });
  if (preparedCount < 1 || preparedCount > 2)
    throw new Error('Unexpected publication preparation sequence; preserve original attempts.');
  persist('publication.receipt.json', result);
  persist('demo-deployment.json', { ...result, chainId: original.chainId,
    address: original.address, originalDeploymentSha256: hash(originalBytes),
    sourceKind: 'dedicated-cosmic-cql' });
  if (hash(fs.readFileSync(originalPath)) !== hash(originalBytes))
    throw new Error('Original deployment changed during independent source admission.');
  fs.renameSync(path.join(state, 'publication.pending.json'), path.join(state, 'publication.admitted.json'));
  process.stdout.write(JSON.stringify({ packageAddress: result.packageAddress,
    storageObjectId: result.storageObjectId, preparedCount }));
} catch (error) {
  persist('publication.failure.json', { message: error.message, nativeMutation: error.nativeMutation ?? null });
  throw error;
}
