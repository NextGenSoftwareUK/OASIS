import test from 'node:test';
import assert from 'node:assert/strict';
import grpc from '@grpc/grpc-js';
import {proto} from '@hiero-ledger/proto';
import {PrivateKey,Transaction,TransactionId} from '@hiero-ledger/sdk';
import {Interface,getBytes,keccak256} from 'ethers';
import {execute} from './bridge.mjs';
import {idBoundEscrowAbi} from './id-bound-escrow.mjs';
import {verifyPreparedQueryIdentity} from './paid-query-preparation.mjs';

// Real SDK transport/signatures/preparation against controlled protobuf consensus responses.
// No native HTS execution, funded network, testnet fee or hosted billing claim.
for(const mode of ['lock','existing-allowance','release','unassociated','readback','query-rejected','mutation-rejected','approval-unknown','wrong-runtime'])
test(`id-bound escrow full SDK loopback flow: ${mode}`,{timeout:15000},async t=>{
 const key=PrivateKey.generateED25519(),owner='0x'+'12'.repeat(20),spender='0x'+'34'.repeat(20);
 const tokenAddress='0x0000000000000000000000000000000000000bb9';
 const accountID={shardNum:0,realmNum:0,accountNum:1001},contractAccountID={shardNum:0,realmNum:0,accountNum:2001};
 const contractID={shardNum:0,realmNum:0,contractNum:2001},tokenID={shardNum:0,realmNum:0,tokenNum:3001};
 const runtime=Uint8Array.from([0x60,0x01,0x60,0x00]);
 const release=mode==='release';let deposited=release,released=false,approvalConfirmed=false;
 let balance=release?877n:1000n,custody=release?123n:0n,allowance=mode==='existing-allowance'?123n:0n;
 let answers=0,submissions=0;const payments=new Map(),mutations=new Map(),submittedIds=[];
 const request={network:'local',sender:'0.0.1001',senderEvmAddress:owner,privateKey:key.toStringDer(),publicKey:key.publicKey.toStringDer(),keyType:'der',
  contractId:'0.0.2001',tokenId:'0.0.3001',runtimeCodeHash:keccak256(runtime),gas:400000,maxFeeTinybars:'100000000',
  escrowProtocol:'id-bound-v1',depositId:'0x'+'56'.repeat(32),amount:release?'0':'1.23',
  transactionId:TransactionId.generate('0.0.1001').toString(),approvalTransactionId:TransactionId.generate('0.0.1001').toString(),
  queryMaxPaymentTinybars:'100',queryMaxTransactionFeeTinybars:'100000000',queryPersistenceTimeoutMs:2000};
 assert.notEqual(request.transactionId,request.approvalTransactionId);
 const originalDeposit=request.depositId,originalMutation=request.transactionId;
 const tokenAbi=new Interface(['function balanceOf(address) view returns(uint256)','function totalSupply() view returns(uint256)',
  'function allowance(address,address) view returns(uint256)']);
 const server=new grpc.Server();
 function rpc(service,name,Request,Response,handler){
  server.addService({[name]:{path:`/proto.${service}/${name}`,requestStream:false,responseStream:false,
   requestSerialize:value=>Buffer.from(Request.encode(value).finish()),requestDeserialize:bytes=>Request.decode(bytes),
   responseSerialize:value=>Buffer.from(Response.encode(value).finish()),responseDeserialize:bytes=>Response.decode(bytes)}},
   {[name]:(call,callback)=>{try{callback(null,handler(call.request));}catch(error){callback(error);}}});
 }
 function queryHeader(query){
  const header=query.header;
  if(header.responseType===proto.ResponseType.ANSWER_ONLY){
   const payment=header.payment,signed=proto.SignedTransaction.decode(payment.signedTransactionBytes),body=proto.TransactionBody.decode(signed.bodyBytes);
   const id=TransactionId.fromBytes(proto.TransactionID.encode(body.transactionID).finish()).toString();
   const admitted=payments.get(id);assert.ok(admitted,'Actual selected payment must be persisted before transport');
   const retained=proto.TransactionList.decode(Buffer.from(admitted.signedTransactionBase64,'base64')).transactionList[0];
   assert.deepEqual(Buffer.from(retained.signedTransactionBytes),Buffer.from(payment.signedTransactionBytes));answers++;
  }
  return {nodeTransactionPrecheckCode:proto.ResponseCodeEnum.OK,responseType:header.responseType,cost:10};
 }
 rpc('CryptoService','getAccountInfo',proto.Query,proto.Response,query=>{
  const q=query.cryptoGetInfo,header=queryHeader(q);
  const contract=q.accountID.alias?.length&&Buffer.from(q.accountID.alias).toString('hex')===spender.slice(2);
  return {cryptoGetInfo:{header,accountInfo:{accountID:contract?contractAccountID:accountID,
   key:{ed25519:key.publicKey.toBytesRaw()},expirationTime:{seconds:2000000000,nanos:0},autoRenewPeriod:{seconds:7776000}}}};
 });
 rpc('SmartContractService','getContractInfo',proto.Query,proto.Response,query=>({contractGetInfo:{header:queryHeader(query.contractGetInfo),
  contractInfo:{contractID,accountID:contractAccountID,contractAccountID:spender.slice(2),expirationTime:{seconds:2000000000,nanos:0},autoRenewPeriod:{seconds:7776000}}}}));
 rpc('SmartContractService','ContractGetBytecode',proto.Query,proto.Response,query=>({contractGetBytecodeResponse:{header:queryHeader(query.contractGetBytecode),bytecode:mode==='wrong-runtime'?Uint8Array.from([1,2,3]):runtime}}));
 rpc('TokenService','getTokenInfo',proto.Query,proto.Response,query=>({tokenGetInfo:{header:queryHeader(query.tokenGetInfo),
  tokenInfo:{tokenId:tokenID,decimals:2,totalSupply:1000,name:'fixture',symbol:'FIX'}}}));
 rpc('SmartContractService','contractCallLocalMethod',proto.Query,proto.Response,query=>{
  const q=query.contractCallLocal,header=queryHeader(q),escrow=Number(q.contractID.contractNum)===2001;
  const abi=escrow?idBoundEscrowAbi:tokenAbi,call=abi.parseTransaction({data:q.functionParameters});let values;
  if(call.name==='escrowProtocolVersion')values=[1n];
  else if(call.name==='isTokenAssociated')values=[mode!=='unassociated'];
  else if(call.name==='deposits'){
   assert.equal(call.args[0],originalDeposit);
   values=deposited?[owner,tokenAddress,mode==='readback'?124n:123n,released]:['0x'+'0'.repeat(40),'0x'+'0'.repeat(40),0n,false];
  }else if(call.name==='balanceOf')values=[call.args[0].toLowerCase()===owner?balance:custody];
  else if(call.name==='totalSupply')values=[1000n];
  else {assert.equal(call.name,'allowance');values=[allowance];}
  return {contractCallLocal:{header,functionResult:{contractID:q.contractID,contractCallResult:getBytes(abi.encodeFunctionResult(call.name,values)),gasUsed:1,bloom:new Uint8Array()}}};
 });
 function submission(transaction,approval){
  const encoded=Buffer.from(proto.TransactionList.encode({transactionList:[transaction]}).finish());
  const native=Transaction.fromBytes(encoded),id=native.transactionId.toString();
  assert.ok(mutations.has(id),'Original mutation must persist before SDK submission');
  assert.equal(key.publicKey.verifyTransaction(native),true);
  assert.equal(native.maxTransactionFee.toTinybars().toString(),'100000000');
  assert.deepEqual(Buffer.from(native.toBytes()),Buffer.from(Transaction.fromBytes(Buffer.from(mutations.get(id).signedTransactionBase64,'base64')).toBytes()));
  if(approval){
   assert.equal(id,request.approvalTransactionId);assert.equal(native.tokenApprovals[0].amount.toString(),'123');allowance=123n;
   // Mutate original caller request after the first awaited stage, not executor's private scalar snapshot.
   request.amount='99';request.depositId='0x'+'ab'.repeat(32);
  }else{
   assert.equal(id,originalMutation);const call=idBoundEscrowAbi.parseTransaction({data:native.functionParameters});
   assert.equal(call.args[0],originalDeposit);assert.equal(call.name,release?'unlock':'lock');
   if(!release){assert.equal(call.args[2],123n);assert.ok(mode==='existing-allowance'||approvalConfirmed);}
   deposited=true;released=release;balance+=release?123n:-123n;custody+=release?-123n:123n;
  }
  submittedIds.push(id);submissions++;
  if(approval&&mode==='approval-unknown')throw Object.assign(new Error('controlled lost approval acknowledgement'),{code:grpc.status.UNAVAILABLE});
  return {nodeTransactionPrecheckCode:proto.ResponseCodeEnum.OK,cost:0};
 }
 rpc('CryptoService','approveAllowances',proto.Transaction,proto.TransactionResponse,tx=>submission(tx,true));
 rpc('SmartContractService','contractCallMethod',proto.Transaction,proto.TransactionResponse,tx=>submission(tx,false));
 rpc('CryptoService','getTransactionReceipts',proto.Query,proto.Response,query=>{
  const q=query.transactionGetReceipt,id=TransactionId.fromBytes(proto.TransactionID.encode(q.transactionID).finish()).toString();
  assert.ok(submittedIds.includes(id));if(id===request.approvalTransactionId)approvalConfirmed=true;
  return {transactionGetReceipt:{header:{nodeTransactionPrecheckCode:proto.ResponseCodeEnum.OK},receipt:{status:proto.ResponseCodeEnum.SUCCESS}}};
 });
 const port=await new Promise((resolve,reject)=>server.bindAsync('127.0.0.1:0',grpc.ServerCredentials.createInsecure(),(error,port)=>error?reject(error):resolve(port)));
 request.nodes={['127.0.0.1:'+port]:'0.0.3'};
 try{
  const operation=execute(release?'tokenEscrowUnlock':'tokenEscrowLock',request,
   async evidence=>{
    if(mode==='mutation-rejected'&&evidence.transactionHash===originalMutation)throw new Error('controlled mutation persistence rejection');
    assert.ok(!mutations.has(evidence.transactionHash));mutations.set(evidence.transactionHash,evidence);
   },
   async evidence=>{
    if(mode==='query-rejected')throw new Error('controlled paid-query persistence rejection');
    await verifyPreparedQueryIdentity(evidence,key.publicKey.toStringDer(),['0.0.3'],'100','100000000');
    assert.equal(evidence.queryPaymentTinybars,'11');
    assert.ok(!payments.has(evidence.transactionHash));payments.set(evidence.transactionHash,evidence);
   });
  if(mode==='unassociated'||mode==='query-rejected'||mode==='wrong-runtime'){
   await assert.rejects(operation,error=>{assert.equal(error.stage,'validation');assert.match(error.cause?.message??'',mode==='unassociated'?/Associate this HTS token/:mode==='wrong-runtime'?/runtime code/:/controlled paid-query persistence rejection/);return true;});assert.equal(submissions,0);
  }else if(mode==='mutation-rejected'||mode==='approval-unknown'){
   await assert.rejects(operation,error=>{
    assert.equal(error.stage,mode==='mutation-rejected'?'mutation':'approval');
    assert.equal(error.transactionId,mode==='mutation-rejected'?originalMutation:request.approvalTransactionId);
    assert.equal(error.approvalTransactionId,mode==='mutation-rejected'?request.approvalTransactionId:null);
    if(mode==='mutation-rejected')assert.match(error.cause?.message??'',/mutation persistence rejection/);
    return true;
   });assert.equal(submissions,1);
  }else if(mode==='readback'){
   await assert.rejects(operation,error=>{assert.equal(error.stage,'readback');assert.equal(error.mutationTransactionId,originalMutation);assert.equal(error.approvalTransactionId,request.approvalTransactionId);return true;});
   assert.equal(submissions,2);
  }else{
   const result=await operation;assert.equal(result.status,'SUCCESS');assert.equal(result.transactionId,originalMutation);
   assert.equal(result.depositId,originalDeposit);assert.equal(result.amountUnits,'123');
   assert.equal(submissions,mode==='lock'?2:1);assert.equal(balance,release?1000n:877n);
  }
  assert.equal(answers,payments.size);assert.equal(submissions,mutations.size);
  assert.ok(payments.size<=24,'This bounded scenario must not release an unbounded query sequence');
  t.diagnostic(`selected payments=${payments.size}, submitted mutations=${submissions}; fixture debit11tinybars/query, NOT observed native fees`);
 }finally{await new Promise(resolve=>server.tryShutdown(resolve));}
});
