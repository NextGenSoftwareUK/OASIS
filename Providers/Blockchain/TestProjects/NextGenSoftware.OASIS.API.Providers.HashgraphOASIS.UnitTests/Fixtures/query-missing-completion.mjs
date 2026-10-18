// Deliberately obsolete/invalid child fixture: reports success without closing its owned query stream.
let input='';for await(const chunk of process.stdin)input+=chunk;
const {action,request}=JSON.parse(input);
process.stdout.write(JSON.stringify(action==='transfer'?{transactionId:request.transactionId,status:'SUCCESS'}:
  {fileId:request.fileId,contents:'controlled fixture'}));
