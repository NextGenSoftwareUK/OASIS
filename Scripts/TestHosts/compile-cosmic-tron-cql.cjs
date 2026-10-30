// Reuse the pinned compiler; generate bytecode only, never deploy or download.
const fs = require('node:fs');
const path = require('node:path');
const {createRequire} = require('node:module');
const root = path.resolve(__dirname, '../..');
const load = createRequire(path.join(root, 'LocalState/COSMIC/EvmRuntime/package.json'));
const solc = load('solc');
if (!solc.version().startsWith('0.8.26+')) throw new Error('Pinned cached solc0.8.26 required.');
const source = fs.readFileSync(path.join(root, 'Providers/Shared/Contracts/OASISKeyValueStore.sol'), 'utf8');
const compiled = JSON.parse(solc.compile(JSON.stringify({language:'Solidity', sources:{'store.sol':{content:source}},
  settings:{evmVersion:'paris',optimizer:{enabled:true,runs:200},outputSelection:{'*':{'*':['evm.bytecode.object']}}}})));
const errors = (compiled.errors || []).filter(item => item.severity === 'error');
if (errors.length) throw new Error(errors.map(item => item.formattedMessage).join('\n'));
const output = path.join(root, 'TestResults/TronCqlCurrent/declared-store-bytecode.txt');
fs.mkdirSync(path.dirname(output), {recursive:true});
fs.writeFileSync(output, compiled.contracts['store.sol'].OASISKeyValueStore.evm.bytecode.object);
console.log('Compiled shipping storage contract; no deployment or network operation.');
