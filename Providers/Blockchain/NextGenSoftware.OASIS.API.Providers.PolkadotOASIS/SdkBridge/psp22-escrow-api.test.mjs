import test from 'node:test';
import assert from 'node:assert/strict';
import { TypeRegistry } from '@polkadot/types/create';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import { validateTokenEscrowApiRequest, prepareTokenEscrowApiMutation } from './psp22-escrow-api.mjs';
import { readPsp22Precision } from './psp22-token-precision.mjs';
const account='0x'+'11'.repeat(32), token='0x'+'22'.repeat(32), escrow='0x'+'33'.repeat(32), hash='0x'+'44'.repeat(32);
const request=()=>({action:'lock',depositId:hash,tokenAddress:token,contractAddress:escrow,contractCodeHash:hash,
  sender:account,recipient:escrow,amount:'1.234567',privateKey:'0x'+'02'.repeat(32)});
function fixture({decimals=6,missing=false,flags=0,dispatch=false}={}) {
  const registry=new TypeRegistry();registry.register({OasisPsp22MetadataLangError:{_enum:{CouldNotReadInput:1}}});
  const counts={heads:0,snapshots:0,queries:0}; const final={toHex:()=>hash};
  const api={registry,rpc:{chain:{getFinalizedHead:async()=>{counts.heads++;return final;}}},
    at:async selected=>{counts.snapshots++;assert.equal(selected,final);return {
      query:{contracts:{contractInfoOf:async address=>{assert.equal(address,token);return {isNone:missing,
        unwrap:()=>({codeHash:{toHex:()=>hash}})};}}},
      call:{contractsApi:{call:async(origin,address,value,gas,deposit,data)=>{
        counts.queries++;assert.equal(origin,account);assert.equal(address,token);assert.equal(data,'0x7271b782');
        assert.equal(value,0);assert.equal(gas,null);assert.equal(deposit,null);
        if(dispatch)return {result:{isErr:true,asErr:{toString:()=> 'RejectedMetadata'}}};
        const encoded=registry.createType('Result<u8,OasisPsp22MetadataLangError>',{Ok:decimals});
        return {result:{isErr:false,asOk:{flags:{get:()=>registry.createType('u32',flags)},data:registry.createType('Bytes',encoded.toHex())}}};
      }}}};}};
  return {api,counts};
}
test('decimal API accepts exact positive shape and rejects conflicting raw units before RPC',async()=>{
  await validateTokenEscrowApiRequest(request());
  const api=new Proxy({}, {get(){throw new Error('Unexpected RPC');}});
  for(const extra of [{amount:'0'},{amount:1},{amount:'1e3'},{amount:'01.2'},{amount:'-1'},
    {amountUnits:'1'},{action:'unlock'},{to:account},{depositId:'guid'}])
    await assert.rejects(prepareTokenEscrowApiMutation(api,{...request(),...extra}),error=>!error.message.includes('Unexpected RPC'));
});
test('token precision comes from one finalized token view, not native currency metadata',async()=>{
  for(const decimals of [0,6,12,38]) {
    const f=fixture({decimals});const result=await readPsp22Precision(f.api,request());
    assert.deepEqual(result,{decimals,codeHash:hash,blockHash:hash});
    assert.deepEqual(f.counts,{heads:1,snapshots:1,queries:1});
  }
});
test('unknown, reverted, failed or excessive metadata cannot become an inferred token precision',async()=>{
  for(const options of [{missing:true},{flags:1},{dispatch:true},{decimals:39}]) {
    const f=fixture(options);await assert.rejects(readPsp22Precision(f.api,request()));
  }
});
test('nonrepresentable decimal quantities reject before escrow or signing access',async()=>{
  for(const amount of ['0.0000001','340282366920938463463374607431768211455']) {
    const f=fixture({decimals:6});await assert.rejects(prepareTokenEscrowApiMutation(f.api,{...request(),amount}),/not exact|fit positive u128/);
    assert.equal(f.counts.queries,1);
  }
});
test('shipping token submission requires durable owner and rejects malformed preparation before network access',()=>{
  const bridge=fileURLToPath(new URL('./bridge.mjs',import.meta.url));
  for(const payload of [{operation:'submitTokenEscrowPreparation'},
    {operation:'prepareTokenEscrowMutation',...request(),amountUnits:'discarded'}]) {
    const env={...process.env};delete env.OASIS_NATIVE_PREPARATION_PIPE;delete env.OASIS_NATIVE_PREPARATION_MODULE;
    const child=spawnSync(process.execPath,[bridge],{input:JSON.stringify(payload),encoding:'utf8',env,timeout:10000});
    assert.equal(child.status,1);const result=JSON.parse(child.stdout);assert.match(result.error,/request validation/);
    assert.match(result.error,/Owned persistence|cannot replace/);
  }
});
