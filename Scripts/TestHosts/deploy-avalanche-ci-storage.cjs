const fs = require('node:fs');
const path = require('node:path');
const { createRequire } = require('node:module');
const { randomBytes } = require('node:crypto');

const fixture = process.env.OASIS_AVALANCHE_CI_FIXTURE;
const root = path.resolve(__dirname, '../..');
const runtime = path.join(root, 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS');
const load = createRequire(path.join(runtime, 'package.json'));
const solc = load('solc');
const { ethers } = load('ethers');

async function main() {
  if (process.env.GITHUB_ACTIONS !== 'true' || path.dirname(fixture || '') !== process.env.RUNNER_TEMP
      || !path.basename(fixture).startsWith('oasis-avalanche.')) {
    throw new Error('Requires the owned AvalancheGo CI fixture.');
  }

  const provider = new ethers.JsonRpcProvider('http://127.0.0.1:19650/ext/bc/C/rpc');
  try {
    if ((await provider.getNetwork()).chainId !== 43112n) throw new Error('Expected AvalancheGo local C-Chain 43112.');
    // Public EWOQ test key from AvalancheGo local genesis. It is used only after checking the loopback chain.
    const funder = new ethers.Wallet('0x56289e99c94b6912bfc12adc093c9b51124f0dc54ac7a766b2bc5ccf558d8027', provider);
    const owner = ethers.Wallet.createRandom().connect(provider);
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
    const settings = {
      OASIS_AVALANCHE_CI_KEY: owner.privateKey,
      OASIS_WEB3CORE_STORAGE_ENCRYPTION_KEY: randomBytes(32).toString('base64')
    };
    for (const mode of ['SYNC', 'ASYNC']) {
      const contract = await factory.deploy();
      await contract.waitForDeployment();
      if (await contract.owner() !== owner.address) throw new Error('Unexpected Avalanche contract owner.');
      if (await contract.getAvatarsCount() !== 0n || await contract.getAvatarDetailsCount() !== 0n
          || await contract.getHolonsCount() !== 0n) throw new Error('Expected a fresh Avalanche storage contract.');
      settings['OASIS_AVALANCHE_CI_CONTRACT_' + mode] = contract.target;
    }
    fs.writeFileSync(path.join(fixture, 'settings.json'), JSON.stringify(settings), { mode: 0o600, flag: 'wx' });
  } finally {
    provider.destroy();
  }
}

main().catch(error => {
  console.error(error.message);
  process.exitCode = 1;
});
