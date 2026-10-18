export async function persistSignedSubmission(signed,request,persist){
  if(signed.transactionId.toString()!==request.transactionId)throw new Error('Prepared Hedera original identity mismatch.');
  if(!persist)return;
  await persist(Object.freeze({transactionHash:request.transactionId,chainId:request.network,sender:request.sender,signedTransactionBase64:Buffer.from(signed.toBytes()).toString('base64')}));
}
import {Transaction,PublicKey,AccountId} from '@hiero-ledger/sdk';
import {proto} from '@hiero-ledger/proto';

// Reviewed network-fee-only operations. Principal, token/custom fees, batches and
// multiple logical/chunk transactions are not inferred from a parent fee ceiling.
const boundedTypes=new Set(['contractCall','contractCreateInstance','fileCreate','fileAppend',
  'tokenCreation','tokenDeletion','tokenUpdateNfts','tokenMint','tokenBurn','tokenAssociate','cryptoApproveAllowance']);
function signedNetworkFeeMaximum(bytes){
  const list=proto.TransactionList.decode(bytes).transactionList;
  if(!list.length||list.length>1024)return undefined;
  let originalId,maximum;
  for(const entry of list){
    if(!entry.signedTransactionBytes?.length)return undefined;
    const body=proto.TransactionBody.decode(proto.SignedTransaction.decode(entry.signedTransactionBytes).bodyBytes);
    if(!boundedTypes.has(body.data)||body.maxCustomFees?.length||body.batchKey)return undefined;
    const id=Buffer.from(proto.TransactionID.encode(body.transactionID).finish());
    if(originalId&&!id.equals(originalId))return undefined;
    originalId=id;
    const fee=body.transactionFee.toString();
    if(!/^(0|[1-9][0-9]*)$/.test(fee))throw new Error('Canonical original Hedera transaction fee required.');
    if(maximum!==undefined&&maximum!==fee)throw new Error('Original Hedera node fee declarations disagree.');
    maximum=fee;
  }
  return maximum;
}

export async function verifyPreparedIdentity(evidence,expectedPublicKey){
  if(typeof evidence.signedTransactionBase64!=='string'||!evidence.signedTransactionBase64.length||evidence.signedTransactionBase64.length>1048576)
    throw new Error('Bounded original Hedera signed bytes required.');
  const bytes=Buffer.from(evidence.signedTransactionBase64,'base64');
  if(bytes.toString('base64')!==evidence.signedTransactionBase64)throw new Error('Canonical Hedera signed bytes required.');
  const transaction=Transaction.fromBytes(bytes);
  if(transaction.transactionId.toString()!==evidence.transactionHash
      ||transaction.transactionId.accountId.toString()!==AccountId.fromString(evidence.sender).toString())
    throw new Error('Original Hedera signed identity or payer mismatch.');
  if(!PublicKey.fromString(expectedPublicKey).verifyTransaction(transaction))throw new Error('Original Hedera transaction signature mismatch.');
  const hashes=[...(await transaction.getTransactionHashPerNode()).values()].map(hash=>Buffer.from(hash).toString('hex'));
  if(hashes.length===0||hashes.length>1024||hashes.some(hash=>!/^[0-9a-f]{96}$/.test(hash))||new Set(hashes).size!==hashes.length)
    throw new Error('Bounded original Hedera signed transaction hashes required.');
  const maximumFeeTinybars=signedNetworkFeeMaximum(bytes);
  return {verified:true,transactionHash:evidence.transactionHash,sender:evidence.sender,signedTransactionHashes:hashes,
    ...(maximumFeeTinybars===undefined?{}:{maximumFeeTinybars})};
}

