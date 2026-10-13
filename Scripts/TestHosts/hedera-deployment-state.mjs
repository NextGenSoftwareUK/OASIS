import fs from 'node:fs';

// Same-directory replacement protects process-crash recovery, not a claim of power-loss durability.
export function saveDeploymentState(statePath,state){
 if(fs.existsSync(statePath)&&fs.lstatSync(statePath).isSymbolicLink())
  throw new Error('Refusing linked deployment evidence.');
 const stagedPath=statePath+'.next';
 const descriptor=fs.openSync(stagedPath,'wx',0o600);
 try{
  fs.writeFileSync(descriptor,JSON.stringify(state,null,2));
  fs.fsyncSync(descriptor);
 }finally{fs.closeSync(descriptor);}
 // A failed rename retains staging evidence; never silently discard/retry it.
 fs.renameSync(stagedPath,statePath);
}
