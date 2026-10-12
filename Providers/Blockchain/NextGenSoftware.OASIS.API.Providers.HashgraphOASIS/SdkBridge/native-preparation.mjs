export async function persistSignedSubmission(signed,request,persist){
  if(!persist)return;
  if(signed.transactionId.toString()!==request.transactionId)throw new Error('Prepared Hedera original identity mismatch.');
  await persist(Object.freeze({transactionHash:request.transactionId,chainId:request.network,sender:request.sender,signedTransactionBase64:Buffer.from(signed.toBytes()).toString('base64')}));
}
