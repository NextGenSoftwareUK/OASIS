import fs from 'node:fs';
const chunks=[];
for await(const chunk of process.stdin)chunks.push(chunk);
const request=JSON.parse(Buffer.concat(chunks).toString('utf8'));
if(request.operation!=='transactionStatus')throw new Error('Observation fixture must never receive a mutation.');
if(process.env.OASIS_NEAR_STATUS_TEST_PID){
  fs.writeFileSync(process.env.OASIS_NEAR_STATUS_TEST_PID,String(process.pid));
  setInterval(()=>{},1000);
}else{
  console.log(JSON.stringify({ok:false,error:'UNKNOWN_TRANSACTION: controlled unavailable observation'}));
  process.exitCode=1;
}
