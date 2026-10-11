import fs from 'node:fs';
import path from 'node:path';
import {spawnSync} from 'node:child_process';

const root=path.resolve(process.argv[2]);
const manifest=JSON.parse(fs.readFileSync(path.join(root,'package.json'),'utf8'));
const external=new Set();
for(const entry of fs.readdirSync(root).filter(name=>name.endsWith('.mjs'))){
 const file=path.join(root,entry);
 const checked=spawnSync(process.execPath,['--check',file],{encoding:'utf8'});
 if(checked.status!==0)throw new Error(`Invalid bridge module ${entry}: ${checked.stderr}`);
 const source=fs.readFileSync(file,'utf8');
 const imports=[...source.matchAll(/\bfrom\s+["']([^"']+)["']|\bimport\s*\(\s*["']([^"']+)["']\s*\)/g)]
  .map(match=>match[1]??match[2]);
 for(const name of imports){
  if(name.startsWith('.')){
   if(!fs.statSync(path.resolve(root,name)).isFile())throw new Error(`Missing local bridge module ${name}`);
  }else{
   if(!external.has(name)){
    // Resolve using the same ESM import conditions as the production bridge,
    // not CommonJS require conditions. Do not execute the bridge entry point.
    const imported=spawnSync(process.execPath,['--input-type=module','-e',`await import(${JSON.stringify(name)});`],
     {cwd:root,encoding:'utf8',timeout:20000});
    if(imported.status!==0)throw new Error(`SDK import ${name} failed: ${imported.stderr || imported.error?.message}`);
   }
   external.add(name);
  }
 }
}
for(const dependency of Object.keys(manifest.dependencies??{}))
 if(![...external].some(name=>name===dependency||name.startsWith(`${dependency}/`)))
  throw new Error(`Declared SDK ${dependency} has no verified bridge import`);
console.log(`${manifest.name}: bridge syntax and import resolution verified`);
