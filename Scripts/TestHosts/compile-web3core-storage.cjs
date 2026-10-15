const fs=require('fs');
const path=require('path');
const root=path.resolve(__dirname,'../..');
const runtime=path.join(root,'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS');
const solc=require(path.join(runtime,'node_modules/solc'));
const contractRoot=path.join(runtime,'Contracts');
const entry='Web3CoreOASIS.sol';
const output=JSON.parse(solc.compile(JSON.stringify({language:'Solidity',
  sources:{[entry]:{content:fs.readFileSync(path.join(contractRoot,entry),'utf8')}},
  settings:{evmVersion:'paris',optimizer:{enabled:true,runs:200},outputSelection:{'*':{'*':['abi','evm.bytecode.object']}}}}),
  {import:file=>{
    const resolved=path.resolve(contractRoot,file);
    if(!resolved.startsWith(contractRoot+path.sep)) return {error:'Import escapes contract source directory'};
    return fs.existsSync(resolved) ? {contents:fs.readFileSync(resolved,'utf8')}:{error:'Missing retained contract import: '+file};
  }}));
const errors=(output.errors||[]).filter(item=>item.severity==='error');
if(errors.length) throw new Error(errors.map(item=>item.formattedMessage).join('\n'));
const contract=output.contracts[entry].Web3CoreOASIS;
const helper=fs.readFileSync(path.join(runtime,'src/Web3CoreOASISBaseProviderHelper.Part1.cs'),'utf8');
const embedded=helper.match(/public const string Abi = @"([\s\S]*?)";/);
const canonical=value=>Array.isArray(value) ? value.map(canonical):value && typeof value==='object'
  ? Object.fromEntries(Object.keys(value).sort().map(key=>[key,canonical(value[key])])):value;
if((!embedded || JSON.stringify(canonical(JSON.parse(embedded[1].replace(/""/g,'"'))))!==JSON.stringify(canonical(contract.abi))) && !process.argv.includes('--emit-candidate'))
  throw new Error('Embedded Web3Core ABI differs from retained source compilation; regenerate/review before verification.');
for(const name of ['getAvatarsPage','getAvatarDetailsPage','getHolonsPage'])
  if(!contract.abi.some(item=>item.type==='function' && item.name===name)) throw new Error('Missing page ABI: '+name);
const folder=path.join(root,'TestResults/Web3CoreCurrent');fs.mkdirSync(folder,{recursive:true});
fs.writeFileSync(path.join(folder,'web3-storage.json'),JSON.stringify({compiler:solc.version(),abi:contract.abi,bytecode:contract.evm.bytecode.object}));
console.log(JSON.stringify({compiler:solc.version(),pageFunctions:3,bytecodeBytes:contract.evm.bytecode.object.length/2}));
