import { readFileSync, writeFileSync, appendFileSync, existsSync } from 'node:fs';
import { join } from 'node:path';
// Controlled IPC acknowledgement schedule only. No SDK/native chain/signing or native persistence.
const input=JSON.parse(readFileSync(0,'utf8'));
let result;
if(input.operation==='probe')result={};
else if(input.operation==='get')result=null;
else if(input.operation==='put'){
  const root=process.env.OASIS_NEAR_PUBLICATION_FIXTURE;
  if(!root)throw new Error('Owned fixture directory required');
  appendFileSync(join(root,'writes.ndjson'),JSON.stringify({key:input.key})+'\n');
  writeFileSync(join(root,'ready.json'),JSON.stringify({key:input.key}),{flag:'wx'});
  const deadline=Date.now()+15000;
  while(!existsSync(join(root,'release'))){if(Date.now()>deadline)throw new Error('Controlled acknowledgement was not released');await new Promise(resolve=>setTimeout(resolve,10));}
  result={transactionHash:'controlled-record-acknowledgement'};
}else throw new Error('Unexpected controlled record operation');
process.stdout.write(JSON.stringify({ok:true,result}));
