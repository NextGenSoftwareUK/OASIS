import fs from 'node:fs';
import path from 'node:path';
import {createRequire} from 'node:module';
import {spawnSync} from 'node:child_process';
import {pathToFileURL} from 'node:url';

const root=path.resolve(process.argv[2]);
const manifest=JSON.parse(fs.readFileSync(path.join(root,'package.json'),'utf8'));
const require=createRequire(path.join(root,'package.json'));
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
   const resolved=require.resolve(name);
   await import(name.startsWith('node:') ? name : pathToFileURL(resolved).href);
   external.add(name);
  }
 }
}
for(const dependency of Object.keys(manifest.dependencies??{}))
 if(![...external].some(name=>name===dependency||name.startsWith(`${dependency}/`)))
  throw new Error(`Declared SDK ${dependency} has no verified bridge import`);
console.log(`${manifest.name}: bridge syntax and import resolution verified`);
