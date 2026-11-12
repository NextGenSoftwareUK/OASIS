// Reuse the single pinned COSMIC compiler/runtime, never install dependencies.
const fs = require('node:fs');
const path = require('node:path');
const {createRequire} = require('node:module');
const root = path.resolve(__dirname, '../..');
const load = createRequire(path.join(root, 'LocalState/COSMIC/EvmRuntime/package.json'));
const solc = load('solc');
if (!solc.version().startsWith('0.8.26+')) throw new Error('Pinned solc0.8.26 required.');
const provider = path.join(root, 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS');
const contracts = path.join(provider, 'Contracts');
const source = 'Web3CoreOASIS.sol';
const output = JSON.parse(solc.compile(JSON.stringify({language:'Solidity',
    sources:{[source]:{content:fs.readFileSync(path.join(contracts, source), 'utf8')}},
    settings:{evmVersion:'paris',optimizer:{enabled:true,runs:200},outputSelection:{'*':{'*':['abi','evm.bytecode.object']}}}}),
    {import:name => {
        const target = path.resolve(contracts, name);
        if (!target.startsWith(contracts + path.sep)) return {error:'Import escapes contract directory.'};
        return {contents:fs.readFileSync(target, 'utf8')};
    }}));
const errors = (output.errors || []).filter(item => item.severity === 'error');
if (errors.length) throw new Error(errors.map(item => item.formattedMessage).join('\n'));
const contract = output.contracts[source].Web3CoreOASIS;
const helper = fs.readFileSync(path.join(provider, 'src/Web3CoreOASISBaseProviderHelper.Part1.cs'), 'utf8');
const embedded = helper.match(/public const string Abi = @"([\s\S]*?)";/);
const canonical = value => Array.isArray(value) ? value.map(canonical) : value && typeof value === 'object'
    ? Object.fromEntries(Object.keys(value).sort().map(key => [key, canonical(value[key])])) : value;
if (!embedded || JSON.stringify(canonical(JSON.parse(embedded[1].replace(/""/g, '"')))) !== JSON.stringify(canonical(contract.abi)))
    throw new Error('Shipping Web3Core ABI differs from compiled source.');
const folder = path.join(root, 'TestResults/EthereumCqlCurrent');
fs.mkdirSync(folder, {recursive:true});
fs.writeFileSync(path.join(folder, 'web3-storage.json'), JSON.stringify({compiler:solc.version(),
    abi:contract.abi,bytecode:'0x' + contract.evm.bytecode.object}));
console.log('Compiled shipping Web3Core contract with pinned local solc; ABI matches.');
