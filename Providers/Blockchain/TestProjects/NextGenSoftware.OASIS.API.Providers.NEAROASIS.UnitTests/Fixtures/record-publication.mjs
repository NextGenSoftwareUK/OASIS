import { readFileSync, writeFileSync, appendFileSync, existsSync, renameSync } from 'node:fs';
import { join } from 'node:path';
// Controlled IPC acknowledgement schedule only. No SDK/native chain/signing or native persistence.
const input=JSON.parse(readFileSync(0,'utf8'));
let result;
if(input.operation==='probe')result={};
else if(input.operation==='get')result=null;
else if(input.operation==='putRecordChecked'){
  const root=process.env.OASIS_NEAR_PUBLICATION_FIXTURE;
  if(!root)throw new Error('Owned fixture directory required');
  if(input.args.expected_value!==null||Object.hasOwn(input.args,'archive_key'))throw new Error('Expected exactly one create-only conditional invocation');
  appendFileSync(join(root,'writes.ndjson'),JSON.stringify({key:input.args.key})+'\n');
  // Publish readiness only after the writer closes its handle. File existence while
  // writeFileSync is still open is not a safe read barrier on Windows.
  writeFileSync(join(root,'ready.pending'),JSON.stringify({key:input.args.key}),{flag:'wx'});
  renameSync(join(root,'ready.pending'),join(root,'ready.json'));
  const deadline=Date.now()+15000;
  while(!existsSync(join(root,'release'))){if(Date.now()>deadline)throw new Error('Controlled acknowledgement was not released');await new Promise(resolve=>setTimeout(resolve,10));}
  result={transactionHash:'controlled-record-acknowledgement',finalExecutionStatus:'FINAL',nativeStatus:{SuccessValue:''}};
}else throw new Error('Unexpected controlled record operation');
process.stdout.write(JSON.stringify({ok:true,result}));
