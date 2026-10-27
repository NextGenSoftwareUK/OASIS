'use strict';
// Azurite exposes an official IPC "shutdown" message that closes and flushes its stores.
const {fork}=require('node:child_process');
const path=require('node:path');
const [runtime,data]=process.argv.slice(2);
if(!runtime||!data)throw new Error('Explicit retained runtime and data path required.');
const child=fork(runtime,['--blobHost','127.0.0.1','--blobPort','10000','--location',data,'--disableTelemetry','--silent'],{
  cwd:path.dirname(data),stdio:['ignore','inherit','inherit','ipc']
});
let requested=false;
function shutdown(){
  if(requested)return;
  requested=true;
  child.send('shutdown',error=>{
    if(error){console.error('Azurite shutdown IPC failed. Preserve the process and store.');process.exitCode=1;return;}
    child.disconnect();
  });
}
let input='';
process.stdin.setEncoding('utf8');
process.stdin.on('data',chunk=>{
  input+=chunk;
  for(let end;(end=input.indexOf('\n'))>=0;){
    const command=input.slice(0,end).trim();input=input.slice(end+1);
    if(command==='shutdown')shutdown();
    else{console.error('Invalid retained-host command.');process.exitCode=1;shutdown();}
  }
});
process.stdin.once('end',shutdown);
process.once('SIGINT',shutdown);process.once('SIGTERM',shutdown);
child.once('error',()=>{console.error('Retained Azurite launch failed.');process.exitCode=1;process.stdin.destroy();});
child.once('exit',(code,signal)=>{process.exitCode=code??1;if(signal)process.exitCode=1;process.stdin.destroy();});
