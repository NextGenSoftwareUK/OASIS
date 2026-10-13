import fs from 'node:fs';import path from 'node:path';import {createRequire} from 'node:module';import {fileURLToPath} from 'node:url';
import {execute} from '../../Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.HashgraphOASIS/SdkBridge/bridge.mjs';
import {deploymentTransaction} from '../../Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.HashgraphOASIS/SdkBridge/deployment.mjs';
import {estimatedTinybars,validateDeploymentBudget} from './hedera-deployment-fee.mjs';
import {saveDeploymentState} from './hedera-deployment-state.mjs';
import {Client,TransactionId,ContractByteCodeQuery,AccountId,AccountInfoQuery,Hbar} from './hedera-deployment-sdk.mjs';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'../..');
const existing=createRequire(path.join(root,'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS/package.json'));
const solc=existing('solc');
const {keccak256}=existing('ethers');
let input='';for await(const chunk of process.stdin){input+=chunk;if(Buffer.byteLength(input)>16384)throw new Error('Deployment input too large.');}
const request=JSON.parse(input);input='';
if(request.network!=='testnet'||!/^0\.0\.[1-9][0-9]*$/.test(request.sender??''))throw new Error('This runner is restricted to an explicit funded testnet account.');
if(!/^0x[0-9a-f]{40}$/i.test(request.senderEvmAddress??''))throw new Error('Verified sender EVM address is required.');
if(!Number.isSafeInteger(request.gas)||request.gas<=0||typeof request.maxFeeTinybars!=='string'
 ||!/^[1-9][0-9]*$/.test(request.maxFeeTinybars)||BigInt(request.maxFeeTinybars)>9223372036854775807n)
 throw new Error('Explicit valid deployment gas and fee ceilings are required before any mutation.');
const source=fs.readFileSync(path.join(root,'Providers/Shared/Contracts/OASISKeyValueStore.sol'),'utf8');
const output=JSON.parse(solc.compile(JSON.stringify({language:'Solidity',sources:{'Store.sol':{content:source}},settings:{evmVersion:'paris',outputSelection:{'*':{'*':['evm.bytecode.object','evm.deployedBytecode.object','evm.deployedBytecode.immutableReferences']}}}})));
if((output.errors??[]).some(e=>e.severity==='error'))throw new Error('Existing compiler rejected storage contract.');
const compiled=output.contracts['Store.sol'].OASISKeyValueStore.evm;
const bytecode=compiled.bytecode.object,sourceHash=keccak256(Buffer.from(source));
const statePath=path.join(root,'TestResults/HederaRuntime/deployment-store.json');
fs.mkdirSync(path.dirname(statePath),{recursive:true});
// Exclusive lease prevents overlapping runners sharing this fixed reusable deployment.
const leasePath=statePath+'.lock',lease=fs.openSync(leasePath,'wx');
try {
 if(fs.existsSync(statePath+'.next'))throw new Error('Retained staged deployment evidence requires reconciliation before any query or mutation.');
 if(fs.existsSync(statePath)&&fs.lstatSync(statePath).isSymbolicLink())throw new Error('Refusing linked deployment evidence.');
 let state=fs.existsSync(statePath)?JSON.parse(fs.readFileSync(statePath,'utf8')):{sender:request.sender,sourceHash,compiler:solc.version(),uploaded:0};
 if(state.sender!==request.sender||state.sourceHash!==sourceHash||state.compiler!==solc.version())throw new Error('Retained deployment identity/source/compiler differs; preserve and reconcile it.');
 if(state.pending)throw new Error('Unresolved deployment transaction '+state.pending.transactionId+'; reconcile before another mutation.');
 function save(){saveDeploymentState(statePath,state);}
 async function mutate(action,fields={}){
  const transactionId=TransactionId.generate(request.sender).toString();
  state.pending={action,transactionId,...fields};save();
  const result=await execute(action,{...request,...fields,transactionId});
  if(result.status!=='SUCCESS'||result.transactionId!==transactionId)throw new Error('Deployment receipt identity mismatch.');
  state.transactions??=[];state.transactions.push({action,transactionId});return result;
 }
 if(!state.contractId){
  if(!state.fileId){const first=bytecode.slice(0,1024);const created=await mutate('bytecodeFileCreate',{contents:first});
   if(!created.fileId)throw new Error('No created bytecode file ID.');state.fileId=created.fileId;state.uploaded=first.length;delete state.pending;save();}
  while(state.uploaded<bytecode.length){const chunk=bytecode.slice(state.uploaded,state.uploaded+1024);
   await mutate('bytecodeFileAppend',{fileId:state.fileId,contents:chunk});state.uploaded+=chunk.length;delete state.pending;save();}
  const estimateClient=Client.forTestnet();
  try {
   await estimateClient.updateNetwork();
   const key=(await import('../../Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.HashgraphOASIS/SdkBridge/bridge.mjs')).restoreKey(request);
   const tx=deploymentTransaction('contractCreate',{...request,fileId:state.fileId,transactionId:TransactionId.generate(request.sender).toString()},key.publicKey);
   const fee=await tx.estimateFee().execute(estimateClient);
   async function mirror(route){const response=await fetch('https://testnet.mirrornode.hedera.com/api/v1/'+route,{redirect:'error',signal:AbortSignal.timeout(20000)});
    if(!response.ok)throw new Error('Deployment budget query failed: HTTP '+response.status);return response.json();}
   const rates=await mirror('network/exchangerate'),account=await mirror('accounts/'+request.sender);
   if(account.account!==request.sender||account.deleted!==false||!Number.isSafeInteger(account.balance?.balance)||account.balance.balance<0)
    throw new Error('Deployment funding response has no exact active account balance.');
   if(!Number.isSafeInteger(rates.current_rate?.expiration_time)||rates.current_rate.expiration_time<=Math.floor(Date.now()/1000))
    throw new Error('Network fee exchange rate has expired.');
   const estimate=estimatedTinybars(fee.total.toString(),rates.current_rate);
   validateDeploymentBudget(estimate,BigInt(request.maxFeeTinybars),BigInt(account.balance.balance));
   state.feeEstimate={tinycents:fee.total.toString(),tinybars:estimate.toString(),ceiling:request.maxFeeTinybars};save();
  } finally {estimateClient.close();}
  const created=await mutate('contractCreate',{fileId:state.fileId,gas:request.gas});
  if(!created.contractId)throw new Error('No created storage contract ID.');state.contractId=created.contractId;delete state.pending;save();
 }
 const client=Client.forTestnet();
 try {
  await client.updateNetwork();
  const key=(await import('../../Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.HashgraphOASIS/SdkBridge/bridge.mjs')).restoreKey(request);
  client.setOperator(request.sender,key);const payment=Hbar.fromTinybars(request.maxFeeTinybars);
  const alias=AccountId.fromEvmAddress(0,0,request.senderEvmAddress);
  const account=await new AccountInfoQuery().setAccountId(alias).setMaxQueryPayment(payment).execute(client);
  if(account.isDeleted||account.accountId.toString()!==request.sender)throw new Error('Sender EVM identity does not match consensus account.');
  const actual=Buffer.from(await new ContractByteCodeQuery().setContractId(state.contractId).setMaxQueryPayment(payment).execute(client));
  const expected=Buffer.from(compiled.deployedBytecode.object,'hex');
  for(const refs of Object.values(compiled.deployedBytecode.immutableReferences))for(const ref of refs){
   if(ref.length!==32)throw new Error('Unexpected immutable owner encoding.');
   Buffer.from(request.senderEvmAddress.slice(2).padStart(64,'0'),'hex').copy(expected,ref.start);
  }
  if(!actual.equals(expected))throw new Error('Deployed runtime differs from compiled storage code and expected owner.');
  state.runtimeCodeHash=keccak256(actual);state.verified=true;save();
  console.log(JSON.stringify({contractId:state.contractId,runtimeCodeHash:state.runtimeCodeHash,reused:state.transactions?.length??0,statePath}));
 } finally {client.close();}
} finally {fs.closeSync(lease);fs.unlinkSync(leasePath);request.privateKey=null;}
