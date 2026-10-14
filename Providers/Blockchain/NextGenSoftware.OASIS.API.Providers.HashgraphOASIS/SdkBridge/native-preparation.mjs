export async function persistSignedSubmission(signed,request,persist){
  if(signed.transactionId.toString()!==request.transactionId)throw new Error('Prepared Hedera original identity mismatch.');
  if(!persist)return;
  await persist(Object.freeze({transactionHash:request.transactionId,chainId:request.network,sender:request.sender,signedTransactionBase64:Buffer.from(signed.toBytes()).toString('base64')}));
}
import {Transaction,PublicKey,AccountId} from '@hiero-ledger/sdk';

export function verifyPreparedIdentity(evidence,expectedPublicKey){
  if(typeof evidence.signedTransactionBase64!=='string'||!evidence.signedTransactionBase64.length||evidence.signedTransactionBase64.length>1048576)
    throw new Error('Bounded original Hedera signed bytes required.');
  const bytes=Buffer.from(evidence.signedTransactionBase64,'base64');
  if(bytes.toString('base64')!==evidence.signedTransactionBase64)throw new Error('Canonical Hedera signed bytes required.');
  const transaction=Transaction.fromBytes(bytes);
  if(transaction.transactionId.toString()!==evidence.transactionHash
      ||transaction.transactionId.accountId.toString()!==AccountId.fromString(evidence.sender).toString())
    throw new Error('Original Hedera signed identity or payer mismatch.');
  if(!PublicKey.fromString(expectedPublicKey).verifyTransaction(transaction))throw new Error('Original Hedera transaction signature mismatch.');
  return {verified:true,transactionHash:evidence.transactionHash,sender:evidence.sender};
}

