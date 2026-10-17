import test from 'node:test';
import assert from 'node:assert/strict';
import {Client,AccountId,AccountInfoQuery,PrivateKey,TransactionId,Transaction,Hbar} from '@hiero-ledger/sdk';
import {proto} from '@hiero-ledger/proto';
import {PreparedAccountInfoQuery,PreparedContractInfoQuery,PreparedContractByteCodeQuery,PreparedContractCallQuery,
  PreparedFileContentsQuery,PreparedTokenInfoQuery,PreparedTokenNftInfoQuery} from './paid-query-preparation.mjs';

// Offline contract probes against the retained pinned SDK. The protected payment-builder
// call is test instrumentation only, NOT a proposed production interception API.
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
      const attempt=query._executeRequestWithGrpcDeadline(channel,request);
      // Attach rejection handling immediately: timeout mode intentionally races admission.
      const stopped=assert.rejects(attempt);
      await Promise.resolve();await Promise.resolve();
      assert.equal(transports,0);
      if(mode==='accepted'){
        assert.equal(observed.queryPaymentTinybars,'123');assert.equal(observed.queryNodeAccountId,'0.0.3');
        assert.equal(observed.purpose,'HederaQueryPayment');
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
