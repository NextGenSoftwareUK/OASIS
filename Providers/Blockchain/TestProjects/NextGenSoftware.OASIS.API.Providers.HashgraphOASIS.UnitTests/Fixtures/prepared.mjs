// Controlled process lifecycle only, no consensus client or SDK submission.
let input='';for await(const chunk of process.stdin)input+=chunk;
const {request}=JSON.parse(input);
const {pathToFileURL}=await import('node:url');
if(request.mode==='no-ack')process.stdout.write(JSON.stringify({transactionId:request.transactionId,status:'SUCCESS'}));
else try{
 const {preparationClient}=await import(pathToFileURL(process.env.OASIS_NATIVE_PREPARATION_MODULE).href);
 await preparationClient(process.env.OASIS_NATIVE_PREPARATION_PIPE,5000)({transactionHash:request.transactionId,signedTransactionBase64:'AQID'});
 if(request.mode==='hold')setInterval(()=>{},1000);
 else if(request.mode==='bad-utf8')process.stdout.write(Buffer.from([255]));
 else if(request.mode==='bad-json')process.stdout.write('invalid JSON');
 else process.stdout.write(JSON.stringify({transactionId:request.mode==='wrong'?'0.0.1001@1700000001.000000001':request.transactionId,status:'SUCCESS'}));
}catch{process.stderr.write(JSON.stringify({error:'controlled rejection'}));process.exitCode=1;}
