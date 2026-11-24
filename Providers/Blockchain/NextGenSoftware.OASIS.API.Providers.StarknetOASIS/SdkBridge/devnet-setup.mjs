// CI fixture for the Starknet native storage suite (not shipped): against a disposable starknet-devnet, declare and deploy
// the OasisKv contract with predeployed account 0, then fund and deploy a fresh storage account of the same OpenZeppelin
// class. Prints KEY=value lines for $GITHUB_ENV. Usage: node devnet-setup.mjs <rpcUrl> <devnetBaseUrl> <sierra.json> <casm.json>
import { readFileSync } from 'node:fs';
import { Account, RpcProvider, ec, num } from 'starknet';
import { accountAddress, keysFromPrivateKey } from './account.mjs';

const [rpcUrl, devnet, sierraPath, casmPath] = process.argv.slice(2);
for (const url of [rpcUrl, devnet]) if (!['127.0.0.1', 'localhost'].includes(new URL(url).hostname)) throw new Error('Devnet fixture must be loopback.');
console.log = (...args) => console.error(...args); // SDK chatter must not reach the env output

const provider = new RpcProvider({ nodeUrl: rpcUrl });
const [predeployed] = await (await fetch(`${devnet}/predeployed_accounts`)).json();
const deployer = new Account({ provider, address: predeployed.address, signer: predeployed.private_key });
const { deploy } = await deployer.declareAndDeploy({
  contract: JSON.parse(readFileSync(sierraPath, 'utf8')), casm: JSON.parse(readFileSync(casmPath, 'utf8'))
});
await provider.waitForTransaction(deploy.transaction_hash);

const classHash = num.toHex(await provider.getClassHashAt(predeployed.address));
const keys = keysFromPrivateKey(num.toHex(ec.starkCurve.utils.randomPrivateKey()));
const address = accountAddress(keys.publicKey, classHash);
const minted = await fetch(`${devnet}/mint`, {
  method: 'POST', headers: { 'content-type': 'application/json' },
  body: `{"address":"${address}","amount":1000000000000000000000,"unit":"FRI"}`
});
if (!minted.ok) throw new Error(`Mint failed: ${minted.status} ${await minted.text()}`);
const storage = new Account({ provider, address, signer: keys.privateKey });
const { transaction_hash } = await storage.deployAccount({ classHash, constructorCalldata: [keys.publicKey], addressSalt: keys.publicKey, contractAddress: address });
const receipt = await provider.waitForTransaction(transaction_hash);
if ((receipt.execution_status ?? receipt.value?.execution_status) !== 'SUCCEEDED') throw new Error('Storage account deployment failed.');

process.stdout.write([
  `OASIS_STARKNET_CHAIN_ID=${(await provider.getChainId()).toLowerCase()}`,
  `OASIS_STARKNET_CONTRACT=${num.toHex(deploy.contract_address)}`,
  `OASIS_STARKNET_ACCOUNT_CLASS=${classHash}`,
  `OASIS_STARKNET_STORAGE_KEY=${keys.privateKey}`,
  `OASIS_STARKNET_STORAGE_ADDRESS=${address}`, ''
].join('\n'));
