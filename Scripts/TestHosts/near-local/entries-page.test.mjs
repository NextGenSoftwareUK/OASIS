import {test} from 'node:test';
import assert from 'node:assert/strict';
import {collectEntries} from '../../../Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.NEAROASIS/SdkBridge/entries-page.mjs';

function provider(pages){let calls=0;return {get calls(){return calls;},async viewBlock(query){assert.deepEqual(query,{finality:'final'});return {header:{hash:'fixed-final-hash'}};},async callFunction(request){assert.equal(request.method,'entries_page');assert.deepEqual(request.blockQuery,{blockId:'fixed-final-hash'});assert.equal(request.args.offset,calls*16);return pages[calls++];}};}
test('pins every page and continues across pages with no prefix matches',async()=>{
  const p=provider([{total:33,next_offset:16,rows:[['a:1','first']]},{total:33,next_offset:32,rows:[]},{total:33,next_offset:null,rows:[['a:33','last']]}]);
  assert.deepEqual(await collectEntries(p,'contract','a:'),[['a:1','first'],['a:33','last']]);assert.equal(p.calls,3);
});
test('empty collection is complete',async()=>assert.deepEqual(await collectEntries(provider([{total:0,next_offset:null,rows:[]}]),'contract'),[]));
for(const [name,bad] of [
  ['changed collection length',{total:18,next_offset:null,rows:[]}],
  ['skipped continuation',{total:17,next_offset:99,rows:[]}],
  ['duplicate key',{total:17,next_offset:null,rows:[['a:1','second']]}],
  ['outside prefix',{total:17,next_offset:null,rows:[['b:2','second']]}],
  ['malformed value',{total:17,next_offset:null,rows:[['a:2',3]]}],
])test(name,async()=>{await assert.rejects(collectEntries(provider([{total:17,next_offset:16,rows:[['a:1','first']]},bad]),'contract','a:'));});
test('snapshot failure propagates without latest-state retry',async()=>{const p=provider([]);p.callFunction=async()=>{throw new Error('snapshot pruned');};await assert.rejects(collectEntries(p,'contract'),/snapshot pruned/);});
