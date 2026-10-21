// Real retained SDK signatures and parent persistence; controlled submission, no consensus client.
import {pathToFileURL} from 'node:url';
import {createRequire} from 'node:module';
import {appendFile,readFile} from 'node:fs/promises';
const bridge=process.env.OASIS_HEDERA_STORAGE_TEST_REAL_BRIDGE;
const {execute,restoreKey,persistSignedSubmission,storageReadCall}=await import(pathToFileURL(bridge).href);
const {PreparedContractCallQuery}=await import(new URL('paid-query-preparation.mjs',pathToFileURL(bridge)));
const packageFile=createRequire(bridge).resolve('@hiero-ledger/sdk/package.json');
const sdkMetadata=JSON.parse(await readFile(packageFile,'utf8'));
const {ContractExecuteTransaction,TransactionId,AccountId,Hbar,Client}=await import(new URL(sdkMetadata.exports['.'].import,pathToFileURL(packageFile)));
let input='';for await(const chunk of process.stdin)input+=chunk;
const {action,request}=JSON.parse(input);
try {
 let sequence;
 if(process.env.OASIS_NATIVE_QUERY_PREPARATION_PIPE){
  const {preparationSequenceClient}=await import(pathToFileURL(process.env.OASIS_NATIVE_QUERY_PREPARATION_MODULE).href);
  sequence=preparationSequenceClient(process.env.OASIS_NATIVE_QUERY_PREPARATION_PIPE,request.queryMaximumRequests);
 }
 async function paidRead(){
  if(!sequence)return;
  const key=restoreKey(request),client=Client.forNetwork({'127.0.0.1:50211':'0.0.3'});
  try{
   client.setOperator(request.sender,key);
   const query=new PreparedContractCallQuery({network:request.network,payer:request.sender,publicKey:key.publicKey.toStringDer(),nodes:['0.0.3'],
    maxPaymentTinybars:request.queryMaxPaymentTinybars,maxTransactionFeeTinybars:request.queryMaxTransactionFeeTinybars,
    persistenceTimeoutMs:request.queryPersistenceTimeoutMs,persist:sequence.persist})
    .setContractId(request.contractId).setGas(request.gas).setFunctionParameters(storageReadCall('get',[request.key??'controlled-probe']))
    .setNodeAccountIds([AccountId.fromString('0.0.3')]).setQueryPayment(Hbar.fromTinybars('123'));
   await query._beforeExecute(client);const frame=await query._makeRequestAsync();
   try{await query._executeRequestWithGrpcDeadline({smartContract:{contractCallLocalMethod:async()=>{throw new Error('controlled query transport stop');}}},frame);}
   catch(error){if(error.message!=='controlled query transport stop')throw error;}
  }finally{client.close();}
 }
 let result;
 if(action==='restoreKey'||action==='verifyNativePreparation'||action==='verifyNativeQueryPreparation')result=await execute(action,request);
 else if(action==='storageGet'){
  if(process.env.OASIS_HEDERA_STORAGE_TEST_HOLD_QUERY_FILE){
   await appendFile(process.env.OASIS_HEDERA_STORAGE_TEST_HOLD_QUERY_FILE,'owned query started\n');
   await new Promise(()=>{setInterval(()=>{},1000);});
  }
  await paidRead();result=null;
 }
 else if(action==='storageList'){await paidRead();result=[];}
 else if(action==='storageProbe'){await paidRead();result={verified:true};}
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
  await appendFile(process.env.OASIS_HEDERA_STORAGE_TEST_SUBMISSIONS,JSON.stringify({transactionId:request.transactionId,action,key:request.key,value:request.value})+'\n');
  result={transactionId:request.transactionId,status:'SUCCESS'};
 }else throw new Error('Unexpected controlled action.');
 if(sequence)await sequence.complete();
 process.stdout.write(JSON.stringify(result));
}catch {process.stderr.write(JSON.stringify({error:'Controlled storage operation rejected'}));process.exitCode=1;}
