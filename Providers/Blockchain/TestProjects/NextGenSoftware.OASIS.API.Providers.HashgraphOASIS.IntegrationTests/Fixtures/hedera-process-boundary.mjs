// Explicit transport fixture only; no SDK, network, signing or provider-success evidence.
let input='';
for await (const chunk of process.stdin) input+=chunk;
const envelope=JSON.parse(input);
if(envelope.action==='storageDelete') {
 const mode=envelope.request.fixtureStage;
 if(mode==='bad-json')process.stdout.write('invalid JSON');
 else process.stdout.write(JSON.stringify(mode==='missing'?{}:mode==='scalar'?[]:mode==='wrong-id'?{transactionId:'another-id',status:'SUCCESS'}
  :mode==='failed'?{transactionId:envelope.request.transactionId,status:'CONTRACT_REVERT_EXECUTED'}
  :mode==='bad-status'?{transactionId:envelope.request.transactionId,status:true}
  :{transactionId:envelope.request.transactionId,status:'SUCCESS'}));
} else if(envelope.action==='storagePut') {
  setInterval(()=>{},1000);
} else if(envelope.action==='tokenBurn') {
  process.stderr.write('invalid structured SDK error');process.exitCode=1;
} else if(envelope.action==='tokenMint') {
  process.stderr.write(JSON.stringify({error:'preflight validation error'}));process.exitCode=1;
} else {
  const stage=envelope.request.fixtureStage??'readback';
  const beforeSubmission=['construction','signing','validation'].includes(stage);
  process.stderr.write(JSON.stringify({transactionId:envelope.request.transactionId,
    mutationTransactionId:beforeSubmission?undefined:envelope.request.transactionId,
    approvalTransactionId:beforeSubmission?undefined:envelope.request.approvalTransactionId,
    stage,status:'CONTRACT_REVERT_EXECUTED'}));
  process.exitCode=1;
}
