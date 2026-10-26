import test from 'node:test';
import assert from 'node:assert/strict';
import {createServer} from 'node:http';
import {once} from 'node:events';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';
import {createHash} from 'node:crypto';
import {KeyPair,KeyPairSigner,createTransaction,actions,baseEncode,SignedTransaction} from 'near-api-js';
import {preparationProtocol,nativeActionIntent,prepareNearAction,verifyNearSignedOriginal,verifyNearPreparation,recoverNearPreparation,exactNearSubmission,submitNearPreparation,nearActionIntent} from './native-preparation.mjs';

import {submitNativeAction} from './native-mutation.mjs';
import {submitCheckedRecord} from './checked-write.mjs';

const key=KeyPair.fromRandom('ed25519'),signer=KeyPairSigner.fromSecretKey(key.toString());
const block=new Uint8Array(32).fill(7),checkpoint=baseEncode(block),chain='controlled-network';
const request=()=>({chainId:chain,sender:'sender.near',recipient:'contract.near',kind:'functionCall',depositUnits:'0',gasUnits:'100000000000000',
  methodName:'put',argsBase64:Buffer.from('{"precision":9223372036854775807,"unicode":"Café"}').toString('base64')});
const provider=()=>({getNetworkId:async()=>chain,viewBlock:async query=>({header:{hash:checkpoint,height:query.finality?12:10}})});
test('shipping SDK entry rejects unowned submission before endpoint/RPC work',()=>{
  const env={...process.env};delete env.OASIS_NATIVE_PREPARATION_PIPE;delete env.OASIS_NATIVE_PREPARATION_MODULE;
  const child=spawnSync(process.execPath,[fileURLToPath(new URL('./bridge.mjs',import.meta.url))],{
    input:JSON.stringify({operation:'submitNativePreparation',rpcEndpoint:'invalid-endpoint',networkId:chain}),env,encoding:'utf8',timeout:5000});
  assert.equal(child.status,1);const envelope=JSON.parse(child.stdout);
  assert.equal(envelope.ok,false);assert.match(envelope.error,/owned native preparation/);
});
async function signedOriginal(customActions=[actions.functionCall('put',Buffer.from(request().argsBase64,'base64'),100000000000000n,0n)],signingKey=key){
  const {signedTransaction:signed}=await KeyPairSigner.fromSecretKey(signingKey.toString()).signTransaction(createTransaction('sender.near',signingKey.getPublicKey(),'contract.near',9007199254740993n,customActions,block));
  const unsigned=Buffer.from(signed.transaction.encode());
  return {protocol:preparationProtocol,chainId:chain,sender:'sender.near',transactionHash:baseEncode(createHash('sha256').update(unsigned).digest()),
    signedTransactionBase64:Buffer.from(signed.encode()).toString('base64'),signDocBase64:unsigned.toString('base64'),signature:null};
}

test('actual SDK Borsh/signature binds large nonce and exact UTF8 JSON bytes',async()=>{
  const original=await signedOriginal();
  const verified=verifyNearSignedOriginal(original,chain,request());
  assert.equal(verified.nonce,'9007199254740993');assert.equal(verified.intent.argsBase64,request().argsBase64);
  assert.equal((await verifyNearPreparation(provider(),original,chain)).checkpointHeight,10);
});
test('account grammar rejects reserved/ambiguous accounts before preparation',()=>{
  for(const recipient of ['system','a','bo__wen','.near','near.','a..near','WAT','a'.repeat(65)])
    assert.throws(()=>nativeActionIntent({kind:'transfer',recipient,depositUnits:'1'}));
  for(const recipient of ['ok','bro.a','max_99.near','0x'+'1'.repeat(40),'0s'+'1'.repeat(40),'a'.repeat(64)])
    assert.equal(nativeActionIntent({kind:'transfer',recipient,depositUnits:'1'}).recipient,recipient);
});
for(const algorithm of ['ed25519','secp256k1','ml-dsa-65'])test('actual SDK signature verification for '+algorithm,async()=>{
  const signingKey=KeyPair.fromRandom(algorithm);
  const original=await signedOriginal([actions.transfer(1n)],signingKey);
  assert.equal(verifyNearSignedOriginal(original,chain).intent.kind,'transfer');
  const decoded=SignedTransaction.decode(Buffer.from(original.signedTransactionBase64,'base64'));
  const tag=Object.keys(decoded.signature)[0];decoded.signature[tag].data[0]^=1;
  assert.throws(()=>verifyNearSignedOriginal({...original,signedTransactionBase64:Buffer.from(decoded.encode()).toString('base64')},chain));
});
test('one prepare freezes admitted intent before awaits and signs exactly once',async()=>{
  const input=request();let signs=0;
  const reads=provider();reads.getNetworkId=async()=>{input.methodName='different';input.argsBase64=Buffer.from('{}').toString('base64');return chain;};
  const account={accountId:'sender.near',provider:reads,createSignedTransaction:async options=>{
    signs++;return (await signer.signTransaction(createTransaction('sender.near',key.getPublicKey(),options.receiverId,1n,options.actions,block))).signedTransaction;
  }};
  const original=await prepareNearAction(account,input);
  assert.equal(signs,1);assert.equal(original.signature,null);assert.equal(original.intent.methodName,'put');assert.equal(original.intent.argsBase64,request().argsBase64);
});
test('read-only recovery reconstructs supported action from common signed fields only',async()=>{
  const original=await signedOriginal();delete original.protocol;
  const recovered=await recoverNearPreparation(original,chain);
  assert.equal(recovered.recoveredFromSignedOriginal,true);assert.equal(recovered.intent.argsBase64,request().argsBase64);
  assert.equal(recovered.signedTransactionBase64,original.signedTransactionBase64);
  await assert.rejects(recoverNearPreparation({...original,chainId:'other'},chain));
});
for(const field of ['protocol','chainId','sender','transactionHash','signature','signDocBase64','signedTransactionBase64']){
  test('reject changed original '+field,async()=>{
    const original=await signedOriginal();original[field]=field.endsWith('Base64')?Buffer.from('changed').toString('base64'):'changed';
    assert.throws(()=>verifyNearSignedOriginal(original,chain));
  });
}
test('reject trailing Borsh bytes, extra action, mismatched admitted calldata and unsafe scalar shapes',async()=>{
  const original=await signedOriginal();
  assert.throws(()=>verifyNearSignedOriginal({...original,signedTransactionBase64:Buffer.concat([Buffer.from(original.signedTransactionBase64,'base64'),Buffer.from([0])]).toString('base64')},chain));
  const multiple=await signedOriginal([actions.transfer(1n),actions.transfer(2n)]);
  assert.throws(()=>verifyNearSignedOriginal(multiple,chain),/Exactly one/);
  assert.throws(()=>verifyNearSignedOriginal(original,chain,{...request(),methodName:'delete_record'}),/admitted intent/);
  assert.throws(()=>verifyNearSignedOriginal({...original,intent:{...request(),kind:'transfer'}},chain),/Declared/);
  for(const depositUnits of ['01','-1',(1n<<128n).toString(),1])assert.throws(()=>verifyNearSignedOriginal(original,chain,{...request(),depositUnits}));
});
test('shared transport envelope and base64 limits reject before RPC',async()=>{
  const original=await signedOriginal();
  assert.throws(()=>verifyNearSignedOriginal({...original,unexpected:'x'.repeat(1048576)},chain),/envelope budget/);
  assert.throws(()=>verifyNearSignedOriginal({...original,signedTransactionBase64:'AAAA'.repeat(262145)},chain),/envelope budget/);
});
test('network/checkpoint disagreement rejects before persistence or submission',async()=>{
  const original=await signedOriginal();
  for(const mode of ['network','hash','canonical','unfinalized','unsafe-height']){
    const reads=provider();
    if(mode==='network')reads.getNetworkId=async()=> 'other-network';
    else reads.viewBlock=async query=>({header:{hash:mode==='hash'||mode==='canonical'&&typeof query.blockId==='number'?'other-hash':checkpoint,
      height:mode==='unsafe-height'?Number.MAX_SAFE_INTEGER+1:query.finality?(mode==='unfinalized'?9:12):10}});
    let persisted=0,submitted=0;
    await assert.rejects(submitNearPreparation(reads,original,chain,async()=>persisted++,async()=>submitted++),/network|checkpoint/i);
    assert.equal(persisted,0);assert.equal(submitted,0);
  }
});
test('submission waits for persistence callback and retains identity on unknown result',async()=>{
  const original=await signedOriginal(),order=[];
  const result=await submitNearPreparation(provider(),original,chain,async value=>{
    order.push('persist');assert.equal(value.signedTransactionBase64,original.signedTransactionBase64);
    original.transactionHash='caller-mutated-after-capture';
  },async()=>{order.push('submit');return {transaction_outcome:{id:(await signedOriginal()).transactionHash}};});
  assert.deepEqual(order,['persist','submit']);assert.equal(result.persisted,true);
  const prior=await signedOriginal();let submitted=0;
  await assert.rejects(submitNearPreparation(provider(),prior,chain,async()=>{throw new Error('disk failure');},async()=>submitted++),/disk failure/);
  assert.equal(submitted,0);
  for(const outcome of ['transport','wrong-id'])await assert.rejects(submitNearPreparation(provider(),prior,chain,async()=>{},async()=>{
    if(outcome==='transport')throw new Error('lost response');return {transaction_outcome:{id:'wrong'}};
  }),error=>error.originalTransactionId===prior.transactionHash&&error.reconciliationRequired===true);
});
test('one exact original capability makes only one SDK HTTP attempt, including 503 and repeat calls',async()=>{
  const original=await signedOriginal();let calls=0;
  const server=createServer(async(req,res)=>{
    calls++;let raw='';for await(const chunk of req)raw+=chunk;
    const body=JSON.parse(raw);assert.equal(body.method,'send_tx');
    assert.equal(body.params.signed_tx_base64,original.signedTransactionBase64);assert.equal(body.params.wait_until,'FINAL');
    res.writeHead(503);res.end('controlled unavailable');
  });
  server.listen(0,'127.0.0.1');await once(server,'listening');
  try {
    const submit=exactNearSubmission('http://127.0.0.1:'+server.address().port,original.signedTransactionBase64);
    await assert.rejects(submit());assert.equal(calls,1);
    await assert.rejects(submit(),/consumed/);assert.equal(calls,1);
  } finally {server.closeAllConnections();await new Promise(resolve=>server.close(resolve));}
});

test('ordinary shared native policy delegates once to owned capability and never signs on failure',async()=>{
  let nativeSigns=0,ownedCalls=0;
  const account={signAndSendTransaction:async()=>{nativeSigns++;throw new Error('Direct signing forbidden in owned scope');}};
  const action=actions.transfer(7n);
  const expected={kind:'transfer',recipient:'receiver.near',depositUnits:'7'};
  assert.deepEqual(nearActionIntent('receiver.near',action),expected);
  const receipt={transaction_outcome:{id:'owned-original'}};
  assert.equal(await submitNativeAction(account,'receiver.near',action,async(a,r,signedAction)=>{
    ownedCalls++;assert.equal(a,account);assert.deepEqual(nearActionIntent(r,signedAction),expected);return receipt;
  }),receipt);
  await assert.rejects(submitNativeAction(account,'receiver.near',action,async()=>{ownedCalls++;throw new Error('No ACK');}),/No ACK/);
  assert.equal(ownedCalls,2);assert.equal(nativeSigns,0);
});
test('checked record keeps protocol preflight and passes exact action through same owned capability',async()=>{
  let submits=0,signs=0;
  const account={signAndSendTransaction:async()=>{signs++;throw new Error('Unowned signing');}};
  const input={networkId:chain,contractId:'contract.near',args:{key:'holon:fixture',expected_value:null,value:'ciphertext'}};
  const node={getNetworkId:async()=>chain,callFunction:async()=>1};
  const createAction=args=>actions.functionCall('put_record_checked',args,100000000000000n,0n);
  const receipt={transaction_outcome:{id:'retained-original'},final_execution_status:'FINAL',status:{SuccessValue:''}};
  const result=await submitCheckedRecord(node,()=>account,input,createAction,async(a,r,action)=>{
    submits++;assert.equal(a,account);const intent=nearActionIntent(r,action);
    assert.equal(intent.methodName,'put_record_checked');assert.equal(intent.argsBase64,Buffer.from(JSON.stringify(input.args)).toString('base64'));return receipt;
  });
  assert.equal(result.transactionHash,'retained-original');assert.equal(submits,1);assert.equal(signs,0);
  node.callFunction=async()=>2;
  await assert.rejects(submitCheckedRecord(node,()=>account,input,createAction,async()=>{submits++;}),/protocol1/);
  assert.equal(submits,1);assert.equal(signs,0);
});
