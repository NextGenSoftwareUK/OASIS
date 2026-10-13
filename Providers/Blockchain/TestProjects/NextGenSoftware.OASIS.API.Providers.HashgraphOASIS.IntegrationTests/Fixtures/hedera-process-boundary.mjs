// Explicit transport fixture only; no SDK, network, signing or provider-success evidence.
let input='';
for await (const chunk of process.stdin) input+=chunk;
const envelope=JSON.parse(input);
if(envelope.action==='nftTransfer'&&envelope.request.privateKey==='test-only-delivery-key') {
 if(envelope.request.sender!=='0.0.1001'||envelope.request.recipient!=='0.0.1002'
  ||envelope.request.tokenId!=='0.0.1234'||envelope.request.serials[0]!=='7')throw new Error('Delivery fixture selectors differ');
 process.stdout.write(JSON.stringify({transactionId:envelope.request.transactionId,status:'SUCCESS'}));
} else if(envelope.action==='storageDelete') {
 const mode=envelope.request.fixtureStage;
 if(mode==='invalid-utf8')process.stdout.write(Buffer.from([0xff]));
 else if(mode==='bad-json')process.stdout.write('invalid JSON');
 else process.stdout.write(JSON.stringify(mode==='missing'?{}:mode==='scalar'?[]:mode==='wrong-id'?{transactionId:'another-id',status:'SUCCESS'}
  :mode==='failed'?{transactionId:envelope.request.transactionId,status:'CONTRACT_REVERT_EXECUTED'}
  :mode==='bad-status'?{transactionId:envelope.request.transactionId,status:true}
  :mode==='foreign-approval'?{transactionId:envelope.request.transactionId,status:'SUCCESS',approvalTransactionId:'untrusted-other-id'}
  :mode==='invalid-approval'?{transactionId:envelope.request.transactionId,status:'SUCCESS',approvalTransactionId:42}
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
  process.stderr.write(JSON.stringify({transactionId:stage==='null-primary'?null:stage==='wrong-primary'?'untrusted-other-id':envelope.request.transactionId,
    mutationTransactionId:beforeSubmission?undefined:stage==='wrong-mutation'?'untrusted-other-id':envelope.request.transactionId,
    approvalTransactionId:beforeSubmission?undefined:stage==='wrong-approval'?'untrusted-other-id':envelope.request.approvalTransactionId,
    stage,status:'CONTRACT_REVERT_EXECUTED'}));
  process.exitCode=1;
}
