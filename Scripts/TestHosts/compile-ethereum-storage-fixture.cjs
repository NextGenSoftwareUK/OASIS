// Compile with existing pinned compiler only; generates one fixed disposable artifact.
const fs = require('node:fs'), path = require('node:path');
const {createRequire} = require('node:module');
const root = path.resolve(__dirname, '../..');
const existing = createRequire(path.join(root, 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS/package.json'));
const solc = existing('solc');
if (!solc.version().startsWith('0.8.26+')) throw new Error('Expected retained solc 0.8.26; no download attempted.');
const source = fs.readFileSync(path.join(root, 'Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests/RejectSecondHolon.sol'), 'utf8');
const output = JSON.parse(solc.compile(JSON.stringify({language:'Solidity',sources:{'Fixture.sol':{content:source}},
  settings:{evmVersion:'paris',outputSelection:{'*':{'*':['abi','evm.bytecode.object']}}}})));
const errors = (output.errors || []).filter(item => item.severity === 'error');
if (errors.length) throw new Error(errors.map(item => item.formattedMessage).join('\n'));
const contract = output.contracts['Fixture.sol'].RejectSecondHolon;
const folder = path.join(root, 'TestResults/EthereumCurrent');
fs.mkdirSync(folder,{recursive:true});
fs.writeFileSync(path.join(folder,'reject-second-holon.json'),JSON.stringify({abi:contract.abi,bytecode:'0x'+contract.evm.bytecode.object}));
console.log('Compiled retained solc fixture; no network or dependency install.');
