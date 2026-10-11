import {AccountId,FileId,TransactionId,Hbar,FileCreateTransaction,FileAppendTransaction,ContractCreateTransaction} from '@hiero-ledger/sdk';

export function deploymentTransaction(action,request,publicKey) {
  if(!['bytecodeFileCreate','bytecodeFileAppend','contractCreate'].includes(action)) throw new Error('Unknown deployment mutation.');
  if(!/^[0-9]+\.[0-9]+\.[1-9][0-9]*$/.test(request.sender??'')) throw new Error('Deployment requires a nonzero numeric payer.');
  const sender=AccountId.fromString(request.sender),id=TransactionId.fromString(request.transactionId);
  if(id.accountId.toString()!==sender.toString()) throw new Error('Deployment payer identity mismatch.');
  if(typeof request.maxFeeTinybars!=='string'||!/^[1-9][0-9]*$/.test(request.maxFeeTinybars)||BigInt(request.maxFeeTinybars)>9223372036854775807n)
    throw new Error('Deployment requires an explicit positive int64 fee ceiling.');
  let transaction;
  if(action==='contractCreate') {
    if(!Number.isSafeInteger(request.gas)||request.gas<=0) throw new Error('Deployment requires an explicit positive gas limit.');
    FileId.fromString(request.fileId);
    transaction=new ContractCreateTransaction().setBytecodeFileId(request.fileId).setGas(request.gas).setAdminKey(publicKey);
  } else {
    // HTS contract bytecode files store hexadecimal ASCII, not decoded binary.
    if(typeof request.contents!=='string'||!/^([0-9a-f]{2})+$/i.test(request.contents)||request.contents.length>1024)
      throw new Error('Bytecode file chunks require even hexadecimal ASCII of at most 1024 bytes.');
    transaction=action==='bytecodeFileCreate'
      ?new FileCreateTransaction().setKeys([publicKey]).setContents(request.contents)
      :new FileAppendTransaction().setFileId(FileId.fromString(request.fileId)).setContents(request.contents).setMaxChunks(1);
  }
  return transaction.setTransactionId(id).setMaxTransactionFee(Hbar.fromTinybars(request.maxFeeTinybars))
    .setMaxAttempts(1).setRegenerateTransactionId(false);
}

export async function executeDeployment(action,request,{createClient,restoreKey}) {
  const key=restoreKey(request);
  if(request.publicKey&&request.publicKey.toLowerCase()!==key.publicKey.toStringDer().toLowerCase()) throw new Error('Deployment signer mismatch.');
  const transaction=deploymentTransaction(action,request,key.publicKey);
  const client=await createClient(request);
  let stage='construction';
  try {
    client.setOperator(request.sender,key);transaction.freezeWith(client);
    stage='signing';const signed=await transaction.sign(key);
    stage='submission';const submitted=await signed.execute(client);
    stage='receipt';const receipt=await submitted.getReceipt(client);
    if(receipt.status.toString()!=='SUCCESS') throw new Error('Deployment consensus failure.');
    return {transactionId:submitted.transactionId.toString(),status:'SUCCESS',fileId:receipt.fileId?.toString(),contractId:receipt.contractId?.toString()};
  } catch(cause) {
    const error=new Error('Deployment success was not established; reconcile the original transaction before retrying.');
    error.transactionId=request.transactionId;error.stage=stage;error.status=cause.status?.toString();throw error;
  } finally {client.close();}
}
