const fs = require('node:fs');
const path = require('node:path');
const { createRequire } = require('node:module');
const { randomBytes } = require('node:crypto');
const { spawn } = require('node:child_process');

const root = path.resolve(__dirname, '../..');
const runtime = path.join(root, 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS');
const load = createRequire(path.join(runtime, 'package.json'));
process.env.HARDHAT_CONFIG = path.join(runtime, 'hardhat.config.cjs');
process.env.HARDHAT_NETWORK = 'hardhat';
process.env.LOCAL_CHAIN_ID = '1337';
const hardhat = load('hardhat');
const solc = load('solc');
const { ethers } = load('ethers');

async function main() {
  const project = process.argv[2];
  if (!project) throw new Error('Supply the built EVM conformance project.');
  const server = await hardhat.run('node:create-server', {
    hostname: '127.0.0.1', port: 0, provider: hardhat.network.provider
  });
  const { port } = await server.listen();
  const rpc = 'http://127.0.0.1:' + port;
  const provider = new ethers.JsonRpcProvider(rpc);
  try {
    const owner = ethers.Wallet.createRandom().connect(provider);
    const funder = await provider.getSigner(0);
    await (await funder.sendTransaction({ to: owner.address, value: ethers.parseEther('100') })).wait();
    const contracts = path.join(runtime, 'Contracts');
    const entry = 'Web3CoreOASIS.sol';
    const source = fs.readFileSync(path.join(contracts, entry), 'utf8');
    const compiled = JSON.parse(solc.compile(JSON.stringify({
      language: 'Solidity', sources: { [entry]: { content: source } },
      settings: { evmVersion: 'paris', optimizer: { enabled: true, runs: 200 }, outputSelection: { '*': { '*': ['abi', 'evm.bytecode.object'] } } }
    }), { import: name => ({ contents: fs.readFileSync(path.join(contracts, name), 'utf8') }) }));
    const errors = (compiled.errors || []).filter(error => error.severity === 'error');
    if (errors.length) throw new Error(errors.map(error => error.formattedMessage).join('\n'));
    const artifact = compiled.contracts[entry].Web3CoreOASIS;
    const factory = new ethers.ContractFactory(artifact.abi, artifact.evm.bytecode.object, owner);
    const contractsByCase = {};
    for (const name of ['EthereumOASIS', 'ArbitrumOASIS', 'PolygonOASIS']) {
      for (const mode of ['SYNC', 'ASYNC']) {
        const contract = await factory.deploy();
        await contract.waitForDeployment();
        if (await contract.owner() !== owner.address) throw new Error('Unexpected storage contract owner.');
        contractsByCase['OASIS_EVM_CONFORMANCE_CONTRACT_' + name + '_' + mode] = contract.target;
      }
    }

    await new Promise((resolve, reject) => {
      const test = spawn('dotnet', [
        'test', project, '--no-build', '/p:IncludeCoreStorageConformance=true',
        '--filter', 'FullyQualifiedName~EvmStorageConformanceTests'
      ], {
        cwd: root, stdio: 'inherit',
        env: {
          ...process.env,
          OASIS_EVM_CONFORMANCE_RPC: rpc,
          OASIS_EVM_CONFORMANCE_CHAIN_ID: '1337',
          OASIS_EVM_CONFORMANCE_PRIVATE_KEY: owner.privateKey,
          ...contractsByCase,
          OASIS_WEB3CORE_STORAGE_ENCRYPTION_KEY: randomBytes(32).toString('base64')
        }
      });
      test.once('error', reject);
      test.once('exit', code => code === 0 ? resolve() : reject(new Error('EVM conformance exited with ' + code)));
    });
  } finally {
    provider.destroy();
    await server.close();
  }
}

main().catch(error => {
  console.error(error.message);
  process.exitCode = 1;
});
