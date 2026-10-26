import { test } from 'node:test';
import assert from 'node:assert/strict';
import { TypeRegistry } from '@polkadot/types/create';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import { registerPsp34Types } from './psp34-contract-codec.mjs';
import { readPsp34Collection } from './psp34-contract-read.mjs';

const request = { contractAddress: '0x' + '11'.repeat(32), origin: '0x' + '22'.repeat(32), contractCodeHash: '0x' + '33'.repeat(32) };
function fixture({ missing=false, wrongCode=false, invalidUtf8=false, flags=0, dispatchFailure=false }={}) {
  const registry=new TypeRegistry();registerPsp34Types(registry);
  const counts={heads:0,snapshots:0,calls:0};
  const finalized={toHex:()=> '0x'+'44'.repeat(32)};
  const at={query:{contracts:{contractInfoOf:async()=>({isNone:missing,
    unwrap:()=>({codeHash:{toHex:()=>wrongCode?request.origin:request.contractCodeHash}})})}},
    call:{contractsApi:{call:async(origin,address,value,gas,deposit,input)=>{
      counts.calls++;assert.equal(origin,request.origin);assert.equal(address,request.contractAddress);
      assert.equal(value,0);assert.equal(gas,null);assert.equal(deposit,null);
      if(dispatchFailure)return {result:{isErr:true,asErr:{toString:()=> 'NativeDispatchFailure'}}};
      let type,result;
      if(input==='0xffa27a5f'){type='OasisPsp34Id';result={Bytes:'0x010203'};}
      else {
        assert.equal(input.slice(0,10),'0xf19d48d1');
        const args=registry.createType('(OasisPsp34Id,Bytes)',Buffer.from(input.slice(10),'hex'));
        assert.equal(args[0].value.toHex(),'0x010203');
        const key=Buffer.from(args[1].toU8a(true)).toString('utf8');
        const values={name:'Living tree',symbol:'TREE',description:'',image:null};
        assert.equal(Object.hasOwn(values,key),true);type='Option<Bytes>';
        const bytes=invalidUtf8?Buffer.from([255]):Buffer.from(values[key]??'');
        // An empty raw hex string is interpreted as None by the SDK Option
        // constructor. Encode Some(Bytes) explicitly so empty metadata stays distinct.
        result=registry.createType('Option<Bytes>',values[key]===null?new Uint8Array([0]):
          Buffer.concat([Buffer.from([1]),Buffer.from(registry.createType('Bytes','0x'+bytes.toString('hex')).toU8a())]));
      }
      const encoded=registry.createType(`Result<${type},OasisPsp34LangError>`,{Ok:result});
      return {result:{isErr:false,asOk:{flags:{get:()=>registry.createType('u32',flags)},data:registry.createType('Bytes',encoded.toHex())}}};
    }}}};
  return {counts,api:{registry,createType:(...args)=>registry.createType(...args),
    rpc:{chain:{getFinalizedHead:async()=>{counts.heads++;return finalized;}}},
    at:async hash=>{assert.equal(hash,finalized);counts.snapshots++;return at;}}};
}
test('collection identity and metadata share one finalized code-pinned snapshot',async()=>{
  const f=fixture();const result=await readPsp34Collection(f.api,request);
  assert.deepEqual(result.attributes,{name:'Living tree',symbol:'TREE',description:'',image:null});
  assert.equal(result.contractAddress,request.contractAddress);assert.equal(result.codeHash,request.contractCodeHash);
  assert.deepEqual(f.counts,{heads:1,snapshots:1,calls:5});
});
test('absent or wrong-code collections never become empty successful NFT records',async()=>{
  for(const options of [{missing:true},{wrongCode:true}]){
    const f=fixture(options);await assert.rejects(readPsp34Collection(f.api,request),/does not exist|differs/);
    assert.equal(f.counts.calls,0);
  }
});
test('invalid identities fail before finalized-head or native access',async()=>{
  const f=fixture();await assert.rejects(readPsp34Collection(f.api,{...request,origin:'invented-address'}));
  assert.deepEqual(f.counts,{heads:0,snapshots:0,calls:0});
});
test('invalid UTF8 metadata fails without replacement-character corruption',async()=>{
  await assert.rejects(readPsp34Collection(fixture({invalidUtf8:true}).api,request),/encoded data|encoding/i);
});
test('native dispatch errors and revert flags cannot produce collection metadata',async()=>{
  await assert.rejects(readPsp34Collection(fixture({dispatchFailure:true}).api,request),/NativeDispatchFailure/);
  await assert.rejects(readPsp34Collection(fixture({flags:1}).api,request),/reverted/);
});
test('shipping bridge admits the real operation but rejects invalid pins before RPC initialization',()=>{
  const child=spawnSync(process.execPath,[fileURLToPath(new URL('./bridge.mjs',import.meta.url))],{
    input:JSON.stringify({operation:'psp34ReadCollection',endpoint:'http://127.0.0.1:1',genesisHash:request.contractCodeHash,
      contractAddress:request.contractAddress,origin:request.origin}),encoding:'utf8',timeout:5000
  });
  assert.equal(child.status,1);const reply=JSON.parse(child.stdout);
  assert.equal(reply.ok,false);assert.match(reply.error,/request validation.*canonical contract\/origin\/code/);
  assert.doesNotMatch(reply.error,/Unknown Polkadot SDK operation|runtime metadata initialization/);
});
