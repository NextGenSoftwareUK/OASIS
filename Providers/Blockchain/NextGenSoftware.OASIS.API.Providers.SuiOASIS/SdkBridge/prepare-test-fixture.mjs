import { readFile, writeFile, stat } from 'node:fs/promises';
import { isAbsolute } from 'node:path';
import { requestSuiFromFaucetV2 } from '@mysten/sui/faucet';
import { invoke } from './bridge.mjs';

// One retained synthetic fixture on the existing local ledger; never a public-chain deployment.
const rpcEndpoint = 'http://127.0.0.1:9000';
const network = 'localnet';
const path = process.env.OASIS_SUI_TEST_DEPLOYMENT;
if (!path || !isAbsolute(path)) throw new Error('An absolute retained fixture path is required.');
const request = { rpcEndpoint, network };
const { chainIdentifier: chainId } = await invoke({ ...request, operation: 'probe' });
let fixture;
try { fixture = JSON.parse(await readFile(path, 'utf8')); }
catch (error) { if (error.code !== 'ENOENT') throw error; }
if (fixture) {
  if (fixture.chainId !== chainId) throw new Error('Retained fixture belongs to another ledger; do not reset or overwrite it.');
  if (fixture.state !== 'ready') {
    // Explicit reconciliation reads the existing capability and receipt; it never publishes again.
    if (process.env.OASIS_SUI_RECONCILE_PUBLICATION !== '1')
      throw new Error('Prior publication outcome requires reconciliation; automatic republishing is forbidden.');
    const recovered = await invoke({ ...request, chainId, operation: 'publicationResume', privateKey: fixture.privateKey });
    fixture = { ...fixture, ...recovered, state: 'ready' };
    await writeFile(path, JSON.stringify(fixture), { mode: 0o600 });
  }
  await invoke({ ...request, chainId, operation: 'storageProbe', ...fixture });
  await invoke({ ...request, chainId, operation: 'tokenProbe', ...fixture });
  console.log('Reused and verified retained Sui fixture.');
} else {
  const modules = [];
  for (const name of ['nft', 'storage', 'token']) {
    const compiled = new URL(`../contracts/build/oasis/bytecode_modules/${name}.mv`, import.meta.url);
    const source = new URL(`../contracts/sources/${name}.move`, import.meta.url);
    if ((await stat(compiled)).mtimeMs < (await stat(source)).mtimeMs)
      throw new Error('Compiled Move modules are stale; build the existing package first.');
    modules.push((await readFile(compiled)).toString('base64'));
  }
  const wallet = await invoke({ operation: 'generateKey' });
  const recipient = await invoke({ operation: 'generateKey' });
  // Exclusive creation prevents overwriting credentials or a prior uncertain publication.
  fixture = { state: 'publication-pending', chainId, privateKey: wallet.privateKey, address: wallet.address };
  await writeFile(path, JSON.stringify(fixture), { flag: 'wx', mode: 0o600 });
  await writeFile(path + '.token-wallet.json', JSON.stringify({ chainId, wallet: recipient }), { flag: 'wx', mode: 0o600 });
  await requestSuiFromFaucetV2({ host: 'http://127.0.0.1:9123', recipient: wallet.address });
  await requestSuiFromFaucetV2({ host: 'http://127.0.0.1:9123', recipient: recipient.address });
  const published = await invoke({ ...request, chainId, operation: 'publish', privateKey: wallet.privateKey,
    modules, dependencies: ['0x1', '0x2'] });
  fixture = { ...fixture, ...published, state: 'ready' };
  await writeFile(path, JSON.stringify(fixture), { mode: 0o600 });
  console.log('Published and retained one local Sui fixture; no credentials printed.');
}
