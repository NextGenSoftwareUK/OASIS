import fs from 'node:fs';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import {createRequire} from 'node:module';

const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'../..');
const retained=createRequire(path.join(root,'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS/package.json'));
const {keccak256}=retained('ethers');

// Public mirror observations only. No signing key, SDK paid query, state update or deployment.
export function verifyRuntime(compiled,actualHex,owner){
 if(!/^0x[0-9a-f]{40}$/i.test(owner??'')||typeof actualHex!=='string'||!/^0x(?:[0-9a-f]{2})+$/i.test(actualHex))
  throw new Error('Exact public owner and nonempty canonical runtime bytecode required.');
 if(!compiled||!/^([0-9a-f]{2})+$/i.test(compiled.object??'')||!compiled.immutableReferences)
  throw new Error('Nonempty retained compiled runtime required.');
 const expected=Buffer.from(compiled.object,'hex');
 const variables=Object.values(compiled.immutableReferences);
 if(variables.length!==1||!Array.isArray(variables[0])||variables[0].length<1||variables[0].length>1024)
  throw new Error('Exactly one bounded immutable storage owner required.');
 const references=[...variables[0]].sort((left,right)=>left.start-right.start);let end=0;
 for(const ref of references){
  if(!Number.isSafeInteger(ref.start)||ref.start<end||ref.length!==32||ref.start+32>expected.length)
   throw new Error('Invalid or overlapping immutable owner location.');
  Buffer.from(owner.slice(2).padStart(64,'0'),'hex').copy(expected,ref.start);end=ref.start+32;
 }
 const actual=Buffer.from(actualHex.slice(2),'hex');
 if(!actual.equals(expected))throw new Error('Indexed runtime does not match retained source and exact owner.');
 return keccak256(actual);
}

async function mirror(route){
 const response=await fetch('https://testnet.mirrornode.hedera.com/api/v1/'+route,{redirect:'error',signal:AbortSignal.timeout(20000)});
 if(!response.ok)throw new Error('Read-only mirror inspection failed: HTTP '+response.status);
 const chunks=[];let length=0;
 for await(const chunk of response.body){
  length+=chunk.length;if(length>262144)throw new Error('Mirror inspection response exceeds bound.');
  chunks.push(chunk);
 }
 return JSON.parse(Buffer.concat(chunks).toString('utf8'));
}

export async function inspect(){
 const state=JSON.parse(fs.readFileSync(path.join(root,'TestResults/HederaRuntime/deployment-store.json'),'utf8'));
 if(state.sender!=='0.0.10993779'||state.contractId!=='0.0.11017885'||state.verified!==true||state.pending)
  throw new Error('Retained verified deployment identity required; preserve pending intent.');
 const source=fs.readFileSync(path.join(root,'Providers/Shared/Contracts/OASISKeyValueStore.sol'),'utf8');
 if(keccak256(Buffer.from(source))!==state.sourceHash)throw new Error('Retained source fingerprint differs.');
 const solc=retained('solc');
 if(solc.version()!=='0.8.26+commit.8a97fa7a.Emscripten.clang'||state.compiler!==solc.version())throw new Error('Existing pinned compiler required.');
 const result=JSON.parse(solc.compile(JSON.stringify({language:'Solidity',sources:{'Store.sol':{content:source}},settings:{evmVersion:'paris',outputSelection:{'*':{'*':['evm.deployedBytecode.object','evm.deployedBytecode.immutableReferences']}}}})));
 if((result.errors??[]).some(error=>error.severity==='error'))throw new Error('Retained source compilation failed.');
 const [account,contract]=await Promise.all([mirror('accounts/'+state.sender),mirror('contracts/'+state.contractId)]);
 if(account.account!==state.sender||account.deleted!==false||!Number.isSafeInteger(account.balance?.balance)||account.balance.balance<0
    ||contract.contract_id!==state.contractId||contract.deleted!==false||contract.file_id!==state.fileId)
  throw new Error('Indexed active account/contract/file identity mismatch.');
 const hash=verifyRuntime(result.contracts['Store.sol'].OASISKeyValueStore.evm.deployedBytecode,contract.runtime_bytecode,account.evm_address);
 if(hash!==state.runtimeCodeHash)throw new Error('Retained runtime hash differs from indexed exact code.');
 return {network:'testnet',accountId:account.account,contractId:contract.contract_id,fileId:contract.file_id,runtimeCodeHash:hash,
  sourceHash:state.sourceHash,owner:account.evm_address,balanceTinybars:String(account.balance.balance),balanceTimestamp:account.balance.timestamp,
  mirrorOnly:true,consensusVerified:false,mutationAuthorized:false,submittedTransactions:0};
}

if(process.argv[1]&&path.resolve(process.argv[1])===fileURLToPath(import.meta.url)){
 try{console.log(JSON.stringify(await inspect()));}catch(error){console.error(error.message);process.exitCode=1;}
}
