// Explicit transport fixture only; no SDK, network, signing or provider-success evidence.
let input='';
for await (const chunk of process.stdin) input+=chunk;
const envelope=JSON.parse(input);
if(envelope.action==='hold') {
  setInterval(()=>{},1000);
} else {
  process.stderr.write(JSON.stringify({transactionId:envelope.request.transactionId,
    mutationTransactionId:envelope.request.transactionId,approvalTransactionId:envelope.request.approvalTransactionId,
    stage:'readback',status:'CONTRACT_REVERT_EXECUTED'}));
  process.exitCode=1;
}
