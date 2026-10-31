import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {JsonRpcProvider} from '../../../Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.NEAROASIS/SdkBridge/node_modules/near-api-js/lib/index.js';
import {collectEntries} from '../../../Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.NEAROASIS/SdkBridge/entries-page.mjs';
const provider=new JsonRpcProvider({url:'http://127.0.0.1:3030'});
assert.equal((await provider.viewNodeStatus()).chain_id,'oasis-near-local');
const snapshot=await provider.viewBlock({finality:'final'});
const blockId=snapshot.header.hash;
const state=await provider.query({request_type:'view_state',account_id:'test.near',prefix_base64:'',block_id:blockId});
const expected=state.values.flatMap(({key,value})=>{
  const decoded=Buffer.from(key,'base64').toString('utf8');
  if(!decoded.startsWith('recordsm'))return [];
  const row=JSON.parse(Buffer.from(value,'base64').toString('utf8'));
  assert.equal(typeof row[0],'string');assert.equal(Number.isSafeInteger(row[1]),true);
  // UnorderedMap stores its serialized string value inside the [value,index] tuple.
  const payload=JSON.parse(row[0]);assert.equal(typeof payload,'string');
  return [[decoded.slice('recordsm'.length),payload]];
});
// Pin even initial snapshot selection so this independent raw-state comparison has one state.
const fixed={viewBlock:async()=>snapshot,callFunction:request=>provider.callFunction(request)};
const sorted=rows=>rows.toSorted((a,b)=>a[0].localeCompare(b[0]));
const actual=await collectEntries(fixed,'test.near','');
const digest=rows=>createHash('sha256').update(JSON.stringify(sorted(rows))).digest('hex');
assert.equal(digest(actual),digest(expected),'Paged results differ from independently decoded raw map values');assert.ok(actual.length>32,'Retained data must exercise multiple pages');
const prefix=actual[0][0].slice(0,Math.min(actual[0][0].length,12));
const filtered=await collectEntries(fixed,'test.near',prefix);
assert.equal(digest(filtered),digest(expected.filter(([key])=>key.startsWith(prefix))));
assert.deepEqual(await collectEntries(fixed,'test.near','known-absent-pagination-fixture:'),[]);
console.log(JSON.stringify({blockId,records:actual.length,filtered:filtered.length,sha256:createHash('sha256').update(JSON.stringify(sorted(actual))).digest('hex'),state:'complete-raw-state-comparison-passed'}));
