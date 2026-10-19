import {test} from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import {createRequire} from 'node:module';
const require=createRequire(process.env.OASIS_NEAR_TEST_MODULE_MANIFEST??import.meta.url);
const ts=require('typescript');
const source=readFileSync(new URL('./contract/src/checked-record.ts',import.meta.url),'utf8');
const compiled=ts.transpileModule(source,{compilerOptions:{target:ts.ScriptTarget.ES2022,module:ts.ModuleKind.ES2022}}).outputText;
const {writeCheckedRecord}=await import('data:text/javascript;base64,'+Buffer.from(compiled).toString('base64'));
const key='avatar:11111111-1111-1111-1111-111111111111';
const archive=`history:${key}:0000000001`;
function map(entries=[]){const values=new Map(entries),writes=[];return {values,writes,get:k=>values.get(k)??null,set(k,v){writes.push([k,v]);values.set(k,v);}};}
test('create requires explicit absence and writes once',()=>{const m=map();writeCheckedRecord(m,{key,expected_value:null,value:'encrypted-new'});assert.deepEqual(m.writes,[[key,'encrypted-new']]);});
test('replacement archives and updates within the one checked invocation',()=>{const m=map([[key,'old']]);writeCheckedRecord(m,{key,expected_value:'old',value:'new',archive_key:archive,archive_value:'encrypted-history'});assert.deepEqual(m.writes,[[archive,'encrypted-history'],[key,'new']]);});
for(const [name,input,entries] of [
 ['create collision',{key,expected_value:null,value:'new'},[[key,'winner']]],
 ['stale replacement',{key,expected_value:'stale',value:'new'},[[key,'winner']]],
 ['archive collision',{key,expected_value:'old',value:'new',archive_key:archive,archive_value:'history'},[[key,'old'],[archive,'existing']]],
 ['cross-record archive',{key,expected_value:'old',value:'new',archive_key:'history:other:0000000001',archive_value:'history'},[[key,'old']]],
 ['missing expected',{key,value:'new'},[]],
 ['archive on create',{key,expected_value:null,value:'new',archive_key:archive,archive_value:'history'},[]],
 ['zero archive version',{key,expected_value:'old',value:'new',archive_key:`history:${key}:0000000000`,archive_value:'history'},[[key,'old']]],
 ['missing archive ciphertext',{key,expected_value:'old',value:'new',archive_key:archive},[[key,'old']]],
 ['noncanonical key',{key:'other',expected_value:null,value:'new'},[]],
 ['empty GUID key',{key:'holon:00000000-0000-0000-0000-000000000000',expected_value:null,value:'new'},[]],
 ['archive version overflow',{key,expected_value:'old',value:'new',archive_key:`history:${key}:2147483648`,archive_value:'history'},[[key,'old']]],
])test(name+' refuses before either mutation',()=>{const m=map(entries);assert.throws(()=>writeCheckedRecord(m,input));assert.deepEqual(m.writes,[]);assert.deepEqual([...m.values],entries);});
