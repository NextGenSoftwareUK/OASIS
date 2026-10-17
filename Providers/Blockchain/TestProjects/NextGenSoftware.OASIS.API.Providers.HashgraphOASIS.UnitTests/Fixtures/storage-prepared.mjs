// Real retained SDK signatures and parent persistence; controlled submission, no consensus client.
import {pathToFileURL} from 'node:url';
import {createRequire} from 'node:module';
import {appendFile} from 'node:fs/promises';
const bridge=process.env.OASIS_HEDERA_STORAGE_TEST_REAL_BRIDGE;
const {execute,restoreKey,persistSignedSubmission}=await import(pathToFileURL(bridge).href);
const {ContractExecuteTransaction,TransactionId,AccountId,Hbar}=createRequire(bridge)('@hiero-ledger/sdk');
let input='';for await(const chunk of process.stdin)input+=chunk;
const {action,request}=JSON.parse(input);
try {
 let result;
 if(action==='restoreKey'||action==='verifyNativePreparation')result=await execute(action,request);
 else if(action==='storageGet')result=null;
 else if(action==='storageList')result=[];
 else if(action==='storageProbe')result={verified:true};
 else if(action==='storagePut'||action==='storageDelete'){
  if(request.value?.includes('owned secret'))throw new Error('Plaintext private payload.');
  const tx=new ContractExecuteTransaction().setContractId(request.contractId).setGas(request.gas)
    .setFunctionParameters(Buffer.from(request.value??request.key,'utf8'))
    .setMaxTransactionFee(Hbar.fromTinybars(request.maxFeeTinybars))
    .setTransactionId(TransactionId.fromString(request.transactionId))
    .setNodeAccountIds([AccountId.fromString('0.0.3')]).freeze();
  const signed=await tx.sign(restoreKey(request));
  if(!process.env.OASIS_NATIVE_PREPARATION_PIPE)throw new Error('Missing preparation owner.');
  const {preparationClient}=await import(pathToFileURL(process.env.OASIS_NATIVE_PREPARATION_MODULE).href);
  await persistSignedSubmission(signed,request,preparationClient(process.env.OASIS_NATIVE_PREPARATION_PIPE));
  await appendFile(process.env.OASIS_HEDERA_STORAGE_TEST_SUBMISSIONS,JSON.stringify({transactionId:request.transactionId})+'\n');
  result={transactionId:request.transactionId,status:'SUCCESS'};
 }else throw new Error('Unexpected controlled action.');
 process.stdout.write(JSON.stringify(result));
}catch {process.stderr.write(JSON.stringify({error:'Controlled storage operation rejected'}));process.exitCode=1;}
