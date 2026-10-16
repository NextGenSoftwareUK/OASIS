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
for (const name of ['LegacyStorageProtocolFixture','UnsupportedStorageProtocolFixture','OwnedErc20Fixture']) {
  const fixture = output.contracts['Fixture.sol'][name];
  fs.writeFileSync(path.join(folder,name+'.json'),JSON.stringify({abi:fixture.abi,bytecode:'0x'+fixture.evm.bytecode.object}));
}
const contractRoot = path.join(root,'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.EthereumOASIS/Contracts');
const files = ['NextGenSoftwareOASIS.sol','Core/Entities/Avatar.sol','Core/Entities/AvatarDetail.sol','Core/Entities/Holon.sol'];
const sources = Object.fromEntries(files.map(file => [file,{content:fs.readFileSync(path.join(contractRoot,file),'utf8')}]));
const nativeOutput = JSON.parse(solc.compile(JSON.stringify({language:'Solidity',sources,settings:{evmVersion:'paris',
  optimizer:{enabled:true,runs:200},outputSelection:{'*':{'*':['abi','evm.bytecode.object']}}}})));
const nativeErrors=(nativeOutput.errors||[]).filter(item=>item.severity==='error');
if(nativeErrors.length)throw new Error(nativeErrors.map(item=>item.formattedMessage).join('\n'));
const native=nativeOutput.contracts['NextGenSoftwareOASIS.sol'].NextGenSoftwareOASIS;
fs.writeFileSync(path.join(folder,'default-storage.json'),JSON.stringify({compiler:solc.version(),evmVersion:'paris',
  optimizerRuns:200,abi:native.abi,bytecode:native.evm.bytecode.object}));
const deployedSource=fs.readFileSync(path.join(root,'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.EthereumOASIS/ContractDefinition/NextGenSoftwareOASISDeploymentBase.cs'),'utf8');
const embedded=deployedSource.match(/public static string BYTECODE = "([0-9a-fA-F]+)"/);
if((!embedded || embedded[1]!==native.evm.bytecode.object) && !process.argv.includes('--emit-candidate'))
  throw new Error('Embedded Ethereum deployment bytecode differs from pinned source compilation. Regenerate/review it before tests or deployment.');
console.log('Compiled retained solc fixture; no network or dependency install.');
