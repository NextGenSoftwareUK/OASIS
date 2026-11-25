// Owner key + Web3CoreOASIS deployment for the disposable Lotus FEVM devnet fixture.
//   node deploy-lotus-storage.cjs key <fixture>          -> prints the fresh owner 0x address
//   node deploy-lotus-storage.cjs deploy <fixture> <rpc> -> deploys SYNC/ASYNC contracts, writes settings.env
const fs = require('node:fs');
const path = require('node:path');
const { createRequire } = require('node:module');
const { randomBytes } = require('node:crypto');

const root = path.resolve(__dirname, '../../../../..');
const runtime = path.join(root, 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS');
const load = createRequire(path.join(runtime, 'package.json'));
const solc = load('solc');
const { ethers } = load('ethers');
const LOTUS_DEVNET_CHAIN_ID = 31415926n;

async function main() {
  const [command, fixture, rpc] = process.argv.slice(2);
  if (!fixture || !fs.statSync(fixture).isDirectory()) throw new Error('Supply the fixture directory.');
  const keyPath = path.join(fixture, 'owner.key');
  if (command === 'key') {
    const owner = ethers.Wallet.createRandom();
    fs.writeFileSync(keyPath, owner.privateKey, { mode: 0o600, flag: 'wx' });
    process.stdout.write(owner.address);
    return;
  }
  if (command !== 'deploy' || !rpc) throw new Error('Usage: key <fixture> | deploy <fixture> <rpc>');
  const endpoint = new URL(rpc);
  if (endpoint.protocol !== 'http:' || !['127.0.0.1', 'localhost'].includes(endpoint.hostname))
    throw new Error('The Lotus devnet fixture only deploys to a loopback RPC.');

  const provider = new ethers.JsonRpcProvider(rpc, undefined, { polling: true, pollingInterval: 1000 });
  try {
    if ((await provider.getNetwork()).chainId !== LOTUS_DEVNET_CHAIN_ID) throw new Error('Expected the Lotus devnet chain 31415926.');
    const owner = new ethers.Wallet(fs.readFileSync(keyPath, 'utf8'), provider);
    for (let attempt = 0; (await provider.getBalance(owner.address)) === 0n; attempt++) {
      if (attempt === 90) throw new Error('The devnet owner was never funded.');
      await new Promise(resolve => setTimeout(resolve, 2000));
    }
    const contracts = path.join(runtime, 'Contracts');
    const entry = 'Web3CoreOASIS.sol';
    const compiled = JSON.parse(solc.compile(JSON.stringify({
      language: 'Solidity', sources: { [entry]: { content: fs.readFileSync(path.join(contracts, entry), 'utf8') } },
      settings: { evmVersion: 'paris', optimizer: { enabled: true, runs: 200 }, outputSelection: { '*': { '*': ['abi', 'evm.bytecode.object'] } } }
    }), { import: name => ({ contents: fs.readFileSync(path.join(contracts, name), 'utf8') }) }));
    const errors = (compiled.errors || []).filter(error => error.severity === 'error');
    if (errors.length) throw new Error(errors.map(error => error.formattedMessage).join('\n'));
    const artifact = compiled.contracts[entry].Web3CoreOASIS;
    const factory = new ethers.ContractFactory(artifact.abi, artifact.evm.bytecode.object, owner);
    const settings = {
      OASIS_FILECOIN_FEVM_RPC: rpc,
      OASIS_FILECOIN_FEVM_CHAIN_ID: LOTUS_DEVNET_CHAIN_ID.toString(),
      OASIS_FILECOIN_FEVM_PRIVATE_KEY: owner.privateKey,
      OASIS_WEB3CORE_STORAGE_ENCRYPTION_KEY: randomBytes(32).toString('base64')
    };
    for (const mode of ['SYNC', 'ASYNC']) {
      const contract = await factory.deploy();
      await contract.waitForDeployment();
      if (await contract.owner() !== owner.address) throw new Error('Unexpected storage contract owner.');
      if (await contract.getHolonsCount() !== 0n) throw new Error('Expected a fresh storage contract.');
      settings['OASIS_FILECOIN_FEVM_CONTRACT_' + mode] = contract.target;
    }
    fs.writeFileSync(path.join(fixture, 'settings.env'),
      Object.entries(settings).map(([key, value]) => `export ${key}=${value}\n`).join(''), { mode: 0o600, flag: 'wx' });
  } finally {
    provider.destroy();
  }
}

main().catch(error => {
  console.error(error.message);
  process.exitCode = 1;
});
