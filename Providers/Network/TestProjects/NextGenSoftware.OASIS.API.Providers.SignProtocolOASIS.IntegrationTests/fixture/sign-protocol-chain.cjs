// Runs a disposable local Hardhat chain (31337) with the Sign Protocol SP contract deployed behind an ERC1967 proxy, as
// Sign deploys it. Compiles the SP sources checked out at the pinned commit with solc 0.8.26 (the repository's
// foundry.toml settings), funds one fresh wallet per storage mode, writes settings.env and stays up until SIGTERM.
// Usage: node sign-protocol-chain.cjs <fixture-dir> <sign-protocol-evm checkout> <port>
const fs = require('node:fs');
const path = require('node:path');

const here = __dirname;
process.env.HARDHAT_CONFIG = path.join(here, 'hardhat.config.cjs');
process.env.HARDHAT_NETWORK = 'hardhat';
const hardhat = require('hardhat');
const solc = require('solc');
const { ethers } = require('ethers');

const [fixture, checkout, portText] = process.argv.slice(2);
if (!fixture || !checkout || !portText) throw new Error('usage: sign-protocol-chain.cjs <fixture-dir> <checkout> <port>');

function compile() {
  const modules = path.join(here, 'node_modules');
  const entries = ['src/core/SP.sol', '@openzeppelin/contracts/proxy/ERC1967/ERC1967Proxy.sol'];
  const read = name => fs.readFileSync(name.startsWith('@openzeppelin/') ? path.join(modules, name) : path.join(checkout, name), 'utf8');
  const input = {
    language: 'Solidity',
    sources: Object.fromEntries(entries.map(name => [name, { content: read(name) }])),
    // foundry.toml at the pinned commit: solc 0.8.26, optimizer on, 50 runs, bytecode_hash none.
    settings: {
      optimizer: { enabled: true, runs: 50 }, metadata: { bytecodeHash: 'none' },
      outputSelection: { '*': { '*': ['abi', 'evm.bytecode.object'] } }
    }
  };
  const output = JSON.parse(solc.compile(JSON.stringify(input), {
    import: name => { try { return { contents: read(name) }; } catch (error) { return { error: error.message }; } }
  }));
  const errors = (output.errors || []).filter(error => error.severity === 'error');
  if (errors.length) throw new Error(errors.map(error => error.formattedMessage).join('\n'));
  if (!solc.version().startsWith('0.8.26+')) throw new Error('Unexpected solc ' + solc.version());
  return {
    sp: output.contracts['src/core/SP.sol'].SP,
    proxy: output.contracts['@openzeppelin/contracts/proxy/ERC1967/ERC1967Proxy.sol'].ERC1967Proxy
  };
}

async function main() {
  const artifacts = compile();
  const server = await hardhat.run('node:create-server', { hostname: '127.0.0.1', port: Number(portText), provider: hardhat.network.provider });
  await server.listen();
  const rpc = 'http://127.0.0.1:' + portText;
  const provider = new ethers.JsonRpcProvider(rpc, 31337, { staticNetwork: true });
  const deployer = await provider.getSigner(0);

  const implementation = await new ethers.ContractFactory(artifacts.sp.abi, artifacts.sp.evm.bytecode.object, deployer).deploy();
  await implementation.waitForDeployment();
  const init = implementation.interface.encodeFunctionData('initialize', [1, 1]);
  const proxy = await new ethers.ContractFactory(artifacts.proxy.abi, artifacts.proxy.evm.bytecode.object, deployer)
    .deploy(await implementation.getAddress(), init);
  await proxy.waitForDeployment();
  const sp = new ethers.Contract(await proxy.getAddress(), artifacts.sp.abi, provider);
  if (await sp.version() !== '1.1.4') throw new Error('Unexpected Sign Protocol version.');
  if (await sp.owner() !== await deployer.getAddress()) throw new Error('Sign Protocol proxy was not initialised.');

  const wallets = { ASYNC: ethers.Wallet.createRandom(), SYNC: ethers.Wallet.createRandom() };
  for (const wallet of Object.values(wallets))
    await (await deployer.sendTransaction({ to: wallet.address, value: ethers.parseEther('1000') })).wait();

  const settings = [
    `OASIS_SIGN_RPC=${rpc}`,
    'OASIS_SIGN_CHAIN_ID=31337',
    `OASIS_SIGN_SP_ADDRESS=${sp.target}`,
    `OASIS_SIGN_ASYNC_KEY=${wallets.ASYNC.privateKey}`,
    `OASIS_SIGN_SYNC_KEY=${wallets.SYNC.privateKey}`
  ].join('\n') + '\n';
  fs.writeFileSync(path.join(fixture, 'settings.env'), settings, { mode: 0o600 });
  console.log(`ready: Sign Protocol ${await sp.version()} at ${sp.target} on ${rpc}`);

  const stop = async () => { provider.destroy(); await server.close(); process.exit(0); };
  process.once('SIGTERM', stop);
  process.once('SIGINT', stop);
}

main().catch(error => { console.error(error.stack || error.message); process.exit(1); });
