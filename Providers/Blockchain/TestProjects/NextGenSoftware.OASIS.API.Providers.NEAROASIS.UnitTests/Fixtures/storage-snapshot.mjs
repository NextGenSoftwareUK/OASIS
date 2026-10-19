import {readFileSync} from 'node:fs';
// Controlled IPC response only; no SDK, chain or storage.
const input=JSON.parse(readFileSync(0,'utf8'));
if(input.operation==='probe')process.stdout.write(JSON.stringify({ok:true,result:{}}));
else if(input.operation==='get'){
  const result=JSON.parse(process.env.OASIS_NEAR_SNAPSHOT_FIXTURE_RESPONSE);
  process.stdout.write(JSON.stringify({ok:true,result}));
}else if(input.operation==='putRecordChecked'){
  // Controlled envelope only, never dispatches an SDK/native write.
  process.stdout.write(process.env.OASIS_NEAR_CHECKED_FIXTURE_ENVELOPE);
}else throw new Error('Unexpected controlled storage operation');
