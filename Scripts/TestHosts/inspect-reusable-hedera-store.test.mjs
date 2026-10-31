import test from 'node:test';
import assert from 'node:assert/strict';
import {verifyRuntime} from './inspect-reusable-hedera-store.mjs';
const owner='0x'+'ab'.repeat(20);
const compiled={object:'00'.repeat(40),immutableReferences:{owner:[{start:4,length:32}]}};
const actual=Buffer.alloc(40);Buffer.from(owner.slice(2).padStart(64,'0'),'hex').copy(actual,4);
test('binds exact immutable owner without changing compiler artifact',()=>{
 const before=JSON.stringify(compiled);assert.match(verifyRuntime(compiled,'0x'+actual.toString('hex'),owner),/^0x[0-9a-f]{64}$/);
 assert.equal(JSON.stringify(compiled),before);
});
test('one immutable owner may occur at multiple nonoverlapping runtime locations',()=>{
 const repeated={object:'00'.repeat(80),immutableReferences:{owner:[{start:44,length:32},{start:4,length:32}]}};
 const bytes=Buffer.alloc(80);for(const start of [4,44])Buffer.from(owner.slice(2).padStart(64,'0'),'hex').copy(bytes,start);
 assert.match(verifyRuntime(repeated,'0x'+bytes.toString('hex'),owner),/^0x[0-9a-f]{64}$/);
});
for(const mode of ['owner','code','empty','location','ambiguous','second-variable'])test('rejects unverified runtime: '+mode,()=>{
 const candidate=structuredClone(compiled);let code='0x'+actual.toString('hex'),address=owner;
 if(mode==='owner')address='0x'+'cd'.repeat(20);
 if(mode==='code')code='0xff'+code.slice(4);
 if(mode==='empty')code='0x';
 if(mode==='location')candidate.immutableReferences.owner[0].start=100;
 if(mode==='ambiguous')candidate.immutableReferences.owner.push({start:4,length:32});
 if(mode==='second-variable')candidate.immutableReferences.other=[{start:4,length:32}];
 assert.throws(()=>verifyRuntime(candidate,code,address));
});
