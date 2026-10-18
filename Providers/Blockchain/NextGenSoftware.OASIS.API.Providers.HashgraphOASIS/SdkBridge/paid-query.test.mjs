import test from 'node:test';
import assert from 'node:assert/strict';
import {Client,AccountId,AccountInfoQuery,PrivateKey,TransactionId,Transaction,TransferTransaction,Hbar} from '@hiero-ledger/sdk';
import {proto} from '@hiero-ledger/proto';
import grpc from '@grpc/grpc-js';
import {execute} from './bridge.mjs';
import {PreparedAccountInfoQuery,PreparedContractInfoQuery,PreparedContractByteCodeQuery,PreparedContractCallQuery,
  PreparedFileContentsQuery,PreparedTokenInfoQuery,PreparedTokenNftInfoQuery,verifyPreparedQueryIdentity} from './paid-query-preparation.mjs';

// Baseline SDK probes. Candidate construction/signing is NOT the durable admission boundary.
for(const algorithm of ['ED25519','ECDSA']) {
  test(`official ${algorithm} query payment is observable through the supported operator signer before release`,async()=>{
    const key=algorithm==='ED25519'?PrivateKey.generateED25519():PrivateKey.generateECDSA();
    const payer=AccountId.fromString('0.0.1001'),node=AccountId.fromString('0.0.3');
    const client=Client.forNetwork({'127.0.0.1:50211':node});
    let retained;
    try{
      client.setOperatorWith(payer,key.publicKey,async bodyBytes=>{
        const signature=key.sign(bodyBytes);
        // Public SDK parser/signature APIs reconstruct exactly what the SDK will release.
        const original=Transaction.fromBytes(proto.Transaction.encode({bodyBytes}).finish());
        original.addSignature(key.publicKey,signature);
        retained=original;
        return signature;
      });
      const query=new AccountInfoQuery().setAccountId(payer);
      const id=TransactionId.fromString('0.0.1001@1700000000.000000001');
      const payment=await query._makePaymentTransaction(id,node,client._operator,Hbar.fromTinybars('123'));
      assert.ok(retained,'Signer must have observed original bytes before payment is released');
      const released=Transaction.fromBytes(proto.Transaction.encode(payment).finish());
      assert.equal(retained.transactionId.toString(),id.toString());
      assert.equal(released.transactionId.toString(),id.toString());
      assert.equal(key.publicKey.verifyTransaction(retained),true);
      assert.equal(key.publicKey.verifyTransaction(released),true);
      assert.deepEqual(await retained.getTransactionHash(),await released.getTransactionHash());
      const signed=proto.SignedTransaction.decode(payment.signedTransactionBytes);
      const body=proto.TransactionBody.decode(signed.bodyBytes);
      assert.deepEqual(body.cryptoTransfer.transfers.accountAmounts.map(row=>row.amount.toString()),['-123','123']);
      // This is a signed fee CEILING, not an observed charge and not the 123-tinybar query payment.
      assert.equal(body.transactionFee.toString(),'100000000');
    }finally{client.close();}
  });
}

for(const algorithm of ['ED25519','ECDSA'])test(`prepared ${algorithm} query constructs explicit signed fee ceiling without SDK one-HBAR default`,async()=>{
 const key=algorithm==='ED25519'?PrivateKey.generateED25519():PrivateKey.generateECDSA();
 const client=Client.forNetwork({'127.0.0.1:50211':'0.0.3'});client.setOperator('0.0.1001',key);
 let admissions=0;
 try{
  const query=new PreparedAccountInfoQuery({network:'offline-test',payer:'0.0.1001',publicKey:key.publicKey.toStringDer(),nodes:['0.0.3'],
   maxPaymentTinybars:'123',maxTransactionFeeTinybars:'1000000',persistenceTimeoutMs:1000,persist:async()=>{admissions++;}})
   .setAccountId('0.0.1001').setNodeAccountIds([AccountId.fromString('0.0.3')]).setQueryPayment(Hbar.fromTinybars('123'));
  await query._beforeExecute(client);const request=await query._makeRequestAsync();
  const payment=request.cryptoGetInfo.header.payment;
  const original=Transaction.fromBytes(proto.Transaction.encode(payment).finish());
  assert.equal(key.publicKey.verifyTransaction(original),true);
  assert.equal(original.transactionId.accountId.toString(),'0.0.1001');
  const body=proto.TransactionBody.decode(proto.SignedTransaction.decode(payment.signedTransactionBytes).bodyBytes);
  assert.equal(body.transactionFee.toString(),'1000000');assert.equal(admissions,0);
  assert.equal(body.cryptoTransfer.transfers.accountAmounts.length,2);
  assert.deepEqual(Object.fromEntries(body.cryptoTransfer.transfers.accountAmounts.map(row=>[
    AccountId.fromBytes(proto.AccountID.encode(row.accountID).finish()).toString(),row.amount.toString()])),{'0.0.1001':'-123','0.0.3':'123'});
 }finally{client.close();}
});

test('rejecting the supported operator signer prevents release of a paid query payment',async()=>{
  const key=PrivateKey.generateED25519();
  const payer=AccountId.fromString('0.0.1001'),node=AccountId.fromString('0.0.3');
  const client=Client.forNetwork({'127.0.0.1:50211':node});
  let called=0;
  try{
    client.setOperatorWith(payer,key.publicKey,async()=>{called++;throw new Error('controlled durable-admission rejection');});
    await assert.rejects(new AccountInfoQuery()._makePaymentTransaction(
      TransactionId.fromString('0.0.1001@1700000000.000000001'),node,client._operator,Hbar.fromTinybars('123')),
      /controlled durable-admission rejection/);
    assert.equal(called,1);
  }finally{client.close();}
});

test('operator signing is NOT a broadcast boundary: SDK also signs unused per-node payment candidates',async()=>{
  const key=PrivateKey.generateED25519();
  const payer=AccountId.fromString('0.0.1001');
  const nodes=[AccountId.fromString('0.0.3'),AccountId.fromString('0.0.4')];
  const client=Client.forNetwork({'127.0.0.1:50211':nodes[0],'127.0.0.1:50212':nodes[1]});
  const observed=[];
  try{
    client.setOperatorWith(payer,key.publicKey,async body=>{
      observed.push(proto.TransactionBody.decode(body));return key.sign(body);
    });
    const query=new AccountInfoQuery().setAccountId(payer).setNodeAccountIds(nodes).setQueryPayment(Hbar.fromTinybars('123'));
    await query._beforeExecute(client); // Real SDK, explicit cost: no network query.
    assert.equal(observed.length,2,'SDK signs a candidate for each configured node before request selection');
    assert.notDeepEqual(observed[0].nodeAccountID,observed[1].nodeAccountID);
    const request=await query._makeRequestAsync();
    assert.equal(observed.length,3,'SDK creates another signed payment for the actual request');
    const payment=proto.SignedTransaction.decode(request.cryptoGetInfo.header.payment.signedTransactionBytes);
    const actual=proto.TransactionBody.decode(payment.bodyBytes);
    assert.deepEqual(actual,observed[2]);
    assert.notDeepEqual(actual.transactionID,observed[0].transactionID);
    // No execute()/transport call occurred. Signing alone cannot certify a charge, submission,
    // or even that a candidate will ever be sent. Recovery must not wait on unused candidates.
  }finally{client.close();}
});

for(const mode of ['accepted','rejected','timeout','wrong-key','payment-limit','fee-limit','foreign-node']) {
  test(`selected paid query awaits bounded request admission before SDK transport: ${mode}`,async()=>{
    const key=PrivateKey.generateED25519(),other=PrivateKey.generateED25519();
    const payer=AccountId.fromString('0.0.1001'),node=AccountId.fromString('0.0.3');
    const client=Client.forNetwork({'127.0.0.1:50211':node});
    client.setOperator(payer,key);
    let release;const gate=new Promise(resolve=>{release=resolve;});
    let observed;let transports=0;
    const context={network:'offline-test',payer:payer.toString(),publicKey:(mode==='wrong-key'?other:key).publicKey.toStringDer(),
      nodes:[mode==='foreign-node'?'0.0.4':node.toString()],maxPaymentTinybars:mode==='payment-limit'?'122':'123',
      maxTransactionFeeTinybars:mode==='fee-limit'?'99999999':'100000000',persistenceTimeoutMs:mode==='timeout'?1:1000,
      persist:async evidence=>{observed=evidence;if(mode==='rejected')throw new Error('controlled persistence rejection');await gate;}};
    const query=new PreparedAccountInfoQuery(context).setAccountId(payer).setNodeAccountIds([node]).setQueryPayment(Hbar.fromTinybars('123'));
    const channel={crypto:{getAccountInfo:async()=>{transports++;throw new Error('controlled transport stop');}}};
    try {
      await query._beforeExecute(client);
      assert.equal(observed,undefined,'Unused candidate preparation must not invoke admission');
      const request=await query._makeRequestAsync();
      if(mode==='fee-limit'){
        const id=Transaction.fromBytes(proto.Transaction.encode(request.cryptoGetInfo.header.payment).finish()).transactionId;
        const over=new TransferTransaction().setTransactionId(id).setNodeAccountIds([node]).setMaxTransactionFee(Hbar.fromTinybars('100000000'))
          .addHbarTransfer(payer,Hbar.fromTinybars('-123')).addHbarTransfer(node,Hbar.fromTinybars('123')).freeze();
        await over.sign(key);
        request.cryptoGetInfo.header.payment=proto.TransactionList.decode(over.toBytes()).transactionList[0];
      }
      const attempt=query._executeRequestWithGrpcDeadline(channel,request);
      // Attach rejection handling immediately: timeout mode intentionally races admission.
      const stopped=assert.rejects(attempt);
      await Promise.resolve();await Promise.resolve();
      assert.equal(transports,0);
      if(mode==='accepted'){
        assert.equal(observed.queryPaymentTinybars,'123');assert.equal(observed.queryNodeAccountId,'0.0.3');
        assert.equal(observed.purpose,'HederaQueryPayment');
        const verified=await verifyPreparedQueryIdentity(observed,key.publicKey.toStringDer(),['0.0.3'],'123','100000000');
        assert.equal(verified.verified,true);assert.equal(verified.queryPaymentTinybars,'123');
        assert.equal(verified.maximumFeeTinybars,'100000123');
        assert.equal((await verifyPreparedQueryIdentity({...observed,maximumFeeTinybars:'0'},key.publicKey.toStringDer(),['0.0.3'],'123','100000000')).maximumFeeTinybars,'100000123');
        for(const cap of [123,'0','01','9223372036854775808','1'.repeat(100)]){
          await assert.rejects(verifyPreparedQueryIdentity(observed,key.publicKey.toStringDer(),['0.0.3'],cap,'100000000'));
          assert.throws(()=>new PreparedAccountInfoQuery({...context,maxPaymentTinybars:cap}));
        }
        for(const replacement of [{purpose:'UserTransfer'},{queryPaymentTinybars:'124'},{queryNodeAccountId:'0.0.4'},{transactionHash:'0.0.1001@1700000000.000000001'}])
          await assert.rejects(verifyPreparedQueryIdentity({...observed,...replacement},key.publicKey.toStringDer(),['0.0.3'],'123','100000000'));
        assert.equal(key.publicKey.verifyTransaction(Transaction.fromBytes(Buffer.from(observed.signedTransactionBase64,'base64'))),true);
        release();
      }
      await stopped;
      if(mode==='timeout'){release();await Promise.resolve();await Promise.resolve();}
      assert.equal(transports,mode==='accepted'?1:0,'Rejected/timed-out admission must never release a late payment');
    }finally{release();client.close();}
  });
}

for(const [name,Query,configure,service,method] of [
  ['account',PreparedAccountInfoQuery,q=>q.setAccountId('0.0.1001'),'crypto','getAccountInfo'],
  ['contract-info',PreparedContractInfoQuery,q=>q.setContractId('0.0.2001'),'smartContract','getContractInfo'],
  ['contract-bytecode',PreparedContractByteCodeQuery,q=>q.setContractId('0.0.2001'),'smartContract','contractGetBytecode'],
  ['contract-call',PreparedContractCallQuery,q=>q.setContractId('0.0.2001').setGas(100000),'smartContract','contractCallLocalMethod'],
  ['file',PreparedFileContentsQuery,q=>q.setFileId('0.0.2001'),'file','getFileContent'],
  ['token',PreparedTokenInfoQuery,q=>q.setTokenId('0.0.2001'),'token','getTokenInfo'],
  ['nft',PreparedTokenNftInfoQuery,q=>q.setNftId('0.0.2001/1'),'token','getTokenNftInfo']
]) {
  test(`prepared ${name} query delegates its actual SDK transport after exactly one selected-payment admission`,async()=>{
    const key=PrivateKey.generateECDSA(),payer=AccountId.fromString('0.0.1001'),node=AccountId.fromString('0.0.3');
    const client=Client.forNetwork({'127.0.0.1:50211':node});client.setOperator(payer,key);
    let admissions=0,transports=0;
    try{
      const query=configure(new Query({network:'offline-test',payer:payer.toString(),publicKey:key.publicKey.toStringDer(),nodes:[node.toString()],
        maxPaymentTinybars:'123',maxTransactionFeeTinybars:'100000000',persistenceTimeoutMs:1000,
        persist:async()=>{admissions++;}})).setNodeAccountIds([node]).setQueryPayment(Hbar.fromTinybars('123'));
      await query._beforeExecute(client);assert.equal(admissions,0);
      const request=await query._makeRequestAsync();
      await assert.rejects(query._executeRequestWithGrpcDeadline({[service]:{[method]:async()=>{
        assert.equal(admissions,1);transports++;throw new Error('controlled transport stop');}}},request));
      assert.equal(transports,1);assert.equal(admissions,1);
      assert.throws(()=>query.setMaxAttempts(2),/exactly one/);
      await assert.rejects(query._executeRequestWithGrpcDeadline({},request),/already issued/);
      assert.equal(admissions,1);assert.equal(transports,1);
    }finally{client.close();}
  });
}

for(const cap of ['123','135'])test(`actual file-read bridge enforces explicit ${cap} cap against SDK-buffered payment over loopback gRPC`,{timeout:10000},async()=>{
  const server=new grpc.Server();let admitted=0,answers=0;
  server.addService({getFileContent:{path:'/proto.FileService/getFileContent',requestStream:false,responseStream:false,
    requestSerialize:value=>Buffer.from(proto.Query.encode(value).finish()),requestDeserialize:bytes=>proto.Query.decode(bytes),
    responseSerialize:value=>Buffer.from(proto.Response.encode(value).finish()),responseDeserialize:bytes=>proto.Response.decode(bytes)}},
    {getFileContent:(call,callback)=>{
      const query=call.request.fileGetContents,header=query.header;
      if(header.responseType===proto.ResponseType.ANSWER_ONLY){assert.equal(admitted,1);answers++;}
      callback(null,{fileGetContents:{header:{nodeTransactionPrecheckCode:proto.ResponseCodeEnum.OK,responseType:header.responseType,cost:123},
        fileContents:{fileID:query.fileID,contents:Buffer.from('controlled bridge read')}}});
    }});
  const port=await new Promise((resolve,reject)=>server.bindAsync('127.0.0.1:0',grpc.ServerCredentials.createInsecure(),(error,port)=>error?reject(error):resolve(port)));
  const key=PrivateKey.generateED25519();
  try{
    const operation=execute('fileRead',{network:'local',nodes:{['127.0.0.1:'+port]:'0.0.3'},sender:'0.0.1001',privateKey:key.toStringDer(),keyType:'der',
      fileId:'0.0.2001',maxFeeTinybars:'1',queryMaxPaymentTinybars:cap,queryMaxTransactionFeeTinybars:'100000000',queryPersistenceTimeoutMs:3000},
      undefined,async evidence=>{assert.equal(evidence.queryPaymentTinybars,'135');admitted++;}).catch(error=>{throw error.cause??error;});
    if(cap==='123'){await assert.rejects(operation,error=>error.name==='MaxQueryPaymentExceededError'&&error.queryCost.toTinybars().toString()==='135');assert.equal(admitted,0);assert.equal(answers,0);}
    else{const response=await operation;assert.equal(response.contents,'controlled bridge read');assert.equal(answers,1);assert.equal(admitted,1);}
  }finally{server.forceShutdown();}
});
