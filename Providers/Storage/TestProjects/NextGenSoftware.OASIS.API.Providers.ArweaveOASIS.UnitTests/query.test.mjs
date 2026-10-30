import test from 'node:test';
import assert from 'node:assert/strict';
import { queryTransactionIds } from '../../NextGenSoftware.OASIS.API.Providers.ArweaveOASIS/SdkBridge/query.mjs';
const id = n => n.toString(16).padStart(43, '0');
const edge = n => ({cursor:'cursor-'+n,node:{id:id(n)}});
const page = (edges, next=false) => ({status:200,data:{data:{transactions:{edges,pageInfo:{hasNextPage:next}}}}});
const api = (...responses) => ({post:async () => {const value=responses.shift();if(value instanceof Error)throw value;return value;}});

test('complete multiple pages preserve tags and advance cursor', async () => {
  const requests=[];const pages=[page([edge(1)],true),page([edge(2)])];
  const result=await queryTransactionIds({post:async (path,body)=>{requests.push({path,body});return pages.shift();}}, {'OASIS-Id':'captured'});
  assert.deepEqual(result,[id(1),id(2)]);
  assert.equal(requests[0].path,'graphql');
  assert.deepEqual(requests[0].body.variables,{tags:[{name:'OASIS-Id',values:['captured']}],after:null});
  assert.equal(requests[1].body.variables.after,'cursor-1');
});
test('complete empty result is valid',async()=>assert.deepEqual(await queryTransactionIds(api(page([]))),[]));
for(const [name,response] of [
 ['http error',{status:503}],['graphql error',{status:200,data:{errors:[{message:'failure'}]}}],
 ['partial data plus errors',{...page([edge(1)]),data:{...page([edge(1)]).data,errors:[{}]}}],
 ['missing data',{status:200,data:{}}],['missing pageInfo',{status:200,data:{data:{transactions:{edges:[]}}}}],
 ['malformed edges',{status:200,data:{data:{transactions:{edges:null,pageInfo:{hasNextPage:false}}}}}],
 ['invalid identity',page([{node:{id:'bad'}}])],['duplicate identity',page([edge(1),edge(1)])],
 ['empty continuation',page([],true)],['missing cursor',page([{node:{id:id(1)}}],true)],
 ['oversized page',page(Array.from({length:101},(_,i)=>edge(i)))],
 ['nonboolean continuation',{status:200,data:{data:{transactions:{edges:[],pageInfo:{hasNextPage:'false'}}}}}]
]) test(name+' rejects',async()=>assert.rejects(()=>queryTransactionIds(api(response))));
test('repeated cursor rejects',async()=>assert.rejects(()=>queryTransactionIds(api(page([edge(1)],true),page([{...edge(2),cursor:'cursor-1'}],true)))));
test('later transport failure returns no partial result',async()=>assert.rejects(()=>queryTransactionIds(api(page([edge(1)],true),new Error('offline')))));
test('duplicate across pages rejects',async()=>assert.rejects(()=>queryTransactionIds(api(page([edge(1)],true),page([edge(1)])))));
test('bound exhaustion rejects without 101st request',async()=>{
 let calls=0; await assert.rejects(()=>queryTransactionIds({post:async()=>page([edge(++calls)],true)}));assert.equal(calls,100);
});
test('invalid tag rejects before dispatch',async()=>{
 let calls=0;await assert.rejects(()=>queryTransactionIds({post:async()=>{calls++;}}, {'bad':null}));assert.equal(calls,0);
});
