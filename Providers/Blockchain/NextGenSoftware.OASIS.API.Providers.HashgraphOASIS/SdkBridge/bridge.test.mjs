import test from 'node:test';
import assert from 'node:assert/strict';
import { execute, tokenUnits, nftTransaction, singleSubmission, nativeFailureStatus, safeSdkFailureCode } from './bridge.mjs';
import {MaxQueryPaymentExceeded} from '@hiero-ledger/sdk';

test('query payment cap diagnostic accepts only the official SDK exception without forwarding secret text',()=>{
 const error=new MaxQueryPaymentExceeded(Hbar.fromTinybars('135'),Hbar.fromTinybars('123'));
 error.message='private key input: never-log-this';
 assert.equal(safeSdkFailureCode(error),'QUERY_PAYMENT_CAP_EXCEEDED');
 assert.equal(safeSdkFailureCode({name:'MaxQueryPaymentExceededError',message:error.message}),undefined);
 assert.equal(safeSdkFailureCode(new Error(error.message)),undefined);
});
import { AccountId, Transaction, TransactionId, TokenMintTransaction, TokenBurnTransaction, TransferTransaction, Hbar, Client, PrivateKey } from '@hiero-ledger/sdk';

function roundTrip(transaction) {
  transaction.setTransactionId(TransactionId.fromString('0.0.1001@1700000000.000000001')).setNodeAccountIds([AccountId.fromString('0.0.3')]).freeze();
  return Transaction.fromBytes(transaction.toBytes());
}

test('direct transfer bridge cannot generate a replacement identity when original ID is absent', async () => {
  const key = PrivateKey.generateED25519();
  for (const transactionId of [undefined, null, '', ' ']) {
    await assert.rejects(execute('transfer', {
      sender: '0.0.1001', recipient: '0.0.1002', tinybars: '1', keyType: 'der',
      privateKey: key.toStringDer(), transactionId, network: 'invalid-network-must-not-be-opened'
    }), /explicit original transaction ID/);
  }
});

test('direct NFT mint rejects malformed Unicode and oversized serial batches before network use',()=>{
 for(const metadata of [['\ud800'],['\udfff'],Array(11).fill('oak')])
  assert.throws(()=>nftTransaction('nftMint',{tokenId:'0.0.2001',metadata}));
 const decoded=roundTrip(nftTransaction('nftMint',{tokenId:'0.0.2001',metadata:Array(10).fill('Oak 🌳')}));
 assert.equal(decoded.metadata.length,10);
 assert.equal(Buffer.from(decoded.metadata[0]).toString('utf8'),'Oak 🌳');
});

test('native transfer freezes with exactly one payer-ID assignment through actual client construction',()=>{
 const client=Client.forNetwork({'127.0.0.1:50211':'0.0.3'});
 try {
  const sender=AccountId.fromString('0.0.1001'), id=TransactionId.fromString('0.0.1001@1700000000.000000001');
  client.setOperator(sender,PrivateKey.generateECDSA());
  const tx=new TransferTransaction().setTransactionMemo('OASIS test').addHbarTransfer(sender,Hbar.fromTinybars('-1')).addHbarTransfer('0.0.1002',Hbar.fromTinybars('1'));
  singleSubmission(tx,id).freezeWith(client);
  assert.equal(tx.transactionId.toString(),id.toString());assert.equal(tx.maxAttempts,1);assert.equal(tx.regenerateTransactionId,false);
 } finally {client.close();}
});

test('native failure status preserves structured and SDK attempt-limit codes without arbitrary exception text',()=>{
 assert.equal(nativeFailureStatus({status:{toString:()=> 'INVALID_SIGNATURE'}}),'INVALID_SIGNATURE');
 assert.equal(nativeFailureStatus(new Error('max attempts of 1 was reached for request with last error being: BUSY')),'BUSY');
 assert.equal(nativeFailureStatus(new Error('private key input: secret')),undefined);
 assert.equal(nativeFailureStatus(new Error('max attempts of 1 was reached for request with last error being: SECRET extra text')),undefined);
});

test('signed transfer and HTS mutations use one attempt without regenerating transaction identity', () => {
  for (const transaction of [new TransferTransaction(), new TokenMintTransaction(), new TokenBurnTransaction()]) {
    const id = TransactionId.fromString('0.0.1001@1700000000.000000001');
    assert.equal(singleSubmission(transaction, id), transaction);
    assert.equal(transaction.maxAttempts, 1);
    assert.equal(transaction.regenerateTransactionId, false);
    assert.equal(transaction.transactionId.toString(), id.toString());
  }
});

test('NFT mint encodes metadata, not a fungible amount', () => {
  const transaction = roundTrip(nftTransaction('nftMint', { tokenId: '0.0.1234', metadata: ['ipfs://first', 'ipfs://second'] }));
  assert.ok(transaction instanceof TokenMintTransaction);
  assert.equal(transaction.amount.toString(), '0');
  assert.deepEqual(transaction.metadata.map(value => Buffer.from(value).toString('utf8')), ['ipfs://first', 'ipfs://second']);
});

test('NFT burn preserves serials above JavaScript safe integer range in SDK wire bytes', () => {
  const transaction = roundTrip(nftTransaction('nftBurn', { tokenId: '0.0.1234', serials: ['9007199254740993', '9223372036854775807'] }));
  assert.ok(transaction instanceof TokenBurnTransaction);
  assert.equal(transaction.amount.toString(), '0');
  assert.deepEqual(transaction.serials.map(value => value.toString()), ['9007199254740993', '9223372036854775807']);
});

test('NFT transfer is a real SDK transfer rather than an HBAR payment', () => {
  const transaction = roundTrip(nftTransaction('nftTransfer', {
    tokenId: '0.0.1234', serials: ['9007199254740993'], sender: '0.0.1001', recipient: '0.0.1002'
  }));
  assert.ok(transaction instanceof TransferTransaction);
  assert.equal(transaction.hbarTransfers.size, 0);
  assert.equal(transaction.tokenTransfers.size, 0);
  assert.equal(transaction.nftTransfers.size, 1);
  const [transfer] = transaction.nftTransfers.get('0.0.1234');
  assert.equal(transfer.serial.toString(), '9007199254740993');
  assert.equal(transfer.sender.toString(), '0.0.1001');
  assert.equal(transfer.recipient.toString(), '0.0.1002');
});

for (const serials of [[], ['0'], ['-1'], ['1.5'], [1], ['9223372036854775808'], ['1', '1']]) {
  test(`invalid NFT serial payload ${JSON.stringify(serials)} is rejected without network use`, async () => {
    await assert.rejects(execute('nftBurn', { tokenId: '0.0.1234', serials }), /serial/);
  });
}

test('NFT metadata limit is measured in UTF-8 bytes without truncation', async () => {
  await assert.rejects(execute('nftMint', { tokenId: '0.0.1234', metadata: ['é'.repeat(51)] }), /100 UTF-8 bytes/);
  assert.equal(nftTransaction('nftMint', { tokenId: '0.0.1234', metadata: ['é'.repeat(50)] }).metadata[0].length, 100);
});

test('invalid receipt ID is rejected before opening a consensus client', async () => {
  await assert.rejects(execute('receipt', { network: 'testnet', transactionId: 'not-a-transaction-id' }));
});

test('mint rejects an invalid HTS asset ID before opening a consensus client', async () => {
  await assert.rejects(execute('tokenMint', { network: 'testnet', tokenId: 'not-an-hts-id' }));
});

test('burn rejects an invalid HTS asset ID before opening a consensus client', async () => {
  await assert.rejects(execute('tokenBurn', { network: 'testnet', tokenId: 'not-an-hts-id' }));
});

for (const keyType of ['ed25519', 'ecdsa']) {
  test(`official SDK ${keyType} key generation and DER recovery`, async () => {
    const generated = await execute('generateKey', { keyType });
    const recovered = await execute('restoreKey', { privateKey: generated.privateKey, keyType: 'der' });
    assert.deepEqual(recovered, generated);
    assert.equal('accountId' in generated, false);
  });
}
test('raw key without explicit algorithm is rejected', async () => {
  await assert.rejects(execute('restoreKey', { privateKey: '1'.repeat(64), keyType: 'der' }));
});
test('fractional tinybars are rejected before network use', async () => {
  await assert.rejects(execute('transfer', { tinybars: '1.1' }), /exact int64/);
});
test('tinybar overflow is rejected before network use', async () => {
  await assert.rejects(execute('transfer', { tinybars: '9223372036854775808' }), /exact int64/);
});
test('token scaling uses ledger decimals without Number rounding', () => {
  assert.equal(tokenUnits('0.12345678', 8), 12345678n);
  assert.equal(tokenUnits('9007199254740993', 0), 9007199254740993n);
  assert.throws(() => tokenUnits('1.001', 2), /precision/);
  assert.throws(() => tokenUnits('9223372036854775808', 0), /int64/);
});
test('mismatched public key is rejected before opening a network client', async () => {
  const first = await execute('generateKey', { keyType: 'ed25519' });
  const second = await execute('generateKey', { keyType: 'ed25519' });
  await assert.rejects(execute('transfer', {
    sender: '0.0.1001', recipient: '0.0.1002', tinybars: '1', keyType: 'der',
    privateKey: first.privateKey, publicKey: second.publicKey
  }), /public key/);
});

for (const action of ['tokenTransfer', 'tokenMint', 'tokenBurn', 'nftTransfer', 'nftMint', 'nftBurn']) {
  test(`${action} rejects mismatched public key before network access`, async () => {
    const signer = PrivateKey.generateED25519();
    const other = PrivateKey.generateED25519();
    await assert.rejects(execute(action, {
      sender: '0.0.1001', recipient: '0.0.1002', tokenId: '0.0.1234', amount: '1',
      serials: ['1'], metadata: ['owned preflight fixture'], keyType: 'der',
      privateKey: signer.toStringDer(), publicKey: other.publicKey.toStringDer(),
      network: 'invalid-network-must-not-be-opened'
    }), /Signing key does not match the supplied public key/);
  });
}
test('transaction payer mismatch is rejected before opening a network client', async () => {
  const key = await execute('generateKey', { keyType: 'ed25519' });
  await assert.rejects(execute('transfer', {
    sender: '0.0.1001', recipient: '0.0.1002', tinybars: '1', keyType: 'der',
    privateKey: key.privateKey, transactionId: '0.0.1003@1700000000.000000001'
  }), /payer/);
});
