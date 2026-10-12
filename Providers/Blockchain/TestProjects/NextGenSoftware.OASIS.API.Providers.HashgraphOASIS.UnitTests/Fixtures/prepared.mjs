// Controlled process lifecycle only, no consensus client or SDK submission.
let input='';for await(const chunk of process.stdin)input+=chunk;
const {request}=JSON.parse(input);
const {pathToFileURL}=await import('node:url');
if(request.mode==='no-ack')process.stdout.write(JSON.stringify({transactionId:request.transactionId,status:'SUCCESS'}));
else try{
 const {preparationClient}=await import(pathToFileURL(process.env.OASIS_NATIVE_PREPARATION_MODULE).href);
 if(request.approvalTransactionId && request.mode!=='skip-approval')
  await preparationClient(process.env.OASIS_NATIVE_PREPARATION_PIPE,5000)({transactionHash:request.approvalTransactionId,signedTransactionBase64:'AQID'});
 if(request.mode==='approval-only-error'){
  process.stderr.write(JSON.stringify({error:'controlled child exit after approval acknowledgement'}));process.exit(1);
 }
 await preparationClient(process.env.OASIS_NATIVE_PREPARATION_PIPE,5000)({transactionHash:request.mode==='foreign-preparation'?'0.0.1001@1700000001.000000001':request.transactionId,signedTransactionBase64:'AQID'});
 if(request.mode==='hold')setInterval(()=>{},1000);
 else if(request.mode==='generic-error'){process.stderr.write(JSON.stringify({error:'controlled failure'}));process.exitCode=1;}
 else if(request.mode==='bad-utf8')process.stdout.write(Buffer.from([255]));
 else if(request.mode==='bad-json')process.stdout.write('invalid JSON');
 else process.stdout.write(JSON.stringify({transactionId:request.mode==='wrong'?'0.0.1001@1700000001.000000001':request.transactionId,status:'SUCCESS',
  ...(request.approvalTransactionId?{approvalTransactionId:request.mode==='skip-approval'||request.mode==='hide-approval'?null:request.approvalTransactionId}:{})}));
}catch{process.stderr.write(JSON.stringify({error:'controlled rejection'}));process.exitCode=1;}
