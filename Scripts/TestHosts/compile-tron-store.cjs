// Cached compiler, one fixed generated output; no downloads or runtime installation.
const fs = require('node:fs');
const path = require('node:path');
const { createRequire } = require('node:module');
const root = path.resolve(__dirname, '../..');
const runtime = createRequire(path.join(root, 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS/package.json'));
const solc = runtime('solc');
const source = fs.readFileSync(path.join(root, 'Providers/Shared/Contracts/OASISKeyValueStore.sol'), 'utf8');
const nft = fs.readFileSync(path.join(root, 'Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.TRONOASIS.IntegrationTests/Contracts/LocalNFT.sol'), 'utf8');
const compiled = JSON.parse(solc.compile(JSON.stringify({language:'Solidity', sources:{'store.sol':{content:source},'nft.sol':{content:nft}},
 settings:{evmVersion:'paris', optimizer:{enabled:true,runs:200}, outputSelection:{'*':{'*':['evm.bytecode.object']}}}})));
const errors = (compiled.errors || []).filter(x => x.severity === 'error');
if (errors.length) throw new Error(errors.map(x => x.formattedMessage).join('\n'));
const output = path.join(root, 'TestResults/TronRuntime/store-bytecode.txt');
fs.mkdirSync(path.dirname(output), {recursive:true});
fs.writeFileSync(output, compiled.contracts['store.sol'].OASISKeyValueStore.evm.bytecode.object);
fs.writeFileSync(path.join(root, 'TestResults/TronRuntime/nft-bytecode.txt'), compiled.contracts['nft.sol'].LocalNFT.evm.bytecode.object);
console.log(JSON.stringify({compiler:solc.version(), output, scope:'creation bytecode only; no deployment'}));
