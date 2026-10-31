import {AccountId,FileId,TransactionId,Hbar,FileCreateTransaction,FileAppendTransaction,ContractCreateTransaction,TokenCreateTransaction,TokenType,TokenSupplyType,TokenId,TokenDeleteTransaction,CustomRoyaltyFee,TokenUpdateNftsTransaction} from '@hiero-ledger/sdk';
import {Long} from '@hiero-ledger/sdk';

export function deploymentTransaction(action,request,publicKey) {
  if(!['bytecodeFileCreate','bytecodeFileAppend','contractCreate','nftCollectionCreate','tokenCollectionDelete','nftMetadataUpdate'].includes(action)) throw new Error('Unknown deployment mutation.');
  if(!/^[0-9]+\.[0-9]+\.[1-9][0-9]*$/.test(request.sender??'')) throw new Error('Deployment requires a nonzero numeric payer.');
  const sender=AccountId.fromString(request.sender),id=TransactionId.fromString(request.transactionId);
  if(id.accountId.toString()!==sender.toString()) throw new Error('Deployment payer identity mismatch.');
  if(typeof request.maxFeeTinybars!=='string'||!/^[1-9][0-9]*$/.test(request.maxFeeTinybars)||BigInt(request.maxFeeTinybars)>9223372036854775807n)
    throw new Error('Deployment requires an explicit positive int64 fee ceiling.');
  let transaction;
  if(action==='nftMetadataUpdate') {
    if(!/^[0-9]+\.[0-9]+\.[1-9][0-9]*$/.test(request.tokenId??''))throw new Error('Explicit numeric NFT collection ID required.');
    if(!Array.isArray(request.serials)||!request.serials.length||request.serials.length>10
      ||new Set(request.serials).size!==request.serials.length
      ||request.serials.some(value=>typeof value!=='string'||!/^[1-9][0-9]*$/.test(value)||BigInt(value)>9223372036854775807n))
      throw new Error('One to ten unique positive int64 NFT serial strings required.');
    if(typeof request.metadata!=='string'||/[\uD800-\uDFFF]/u.test(request.metadata)||Buffer.byteLength(request.metadata,'utf8')>100)
      throw new Error('Explicit NFT metadata of at most 100 UTF-8 bytes required.');
    transaction=new TokenUpdateNftsTransaction().setTokenId(request.tokenId).setSerialNumbers(request.serials.map(value=>Long.fromString(value))).setMetadata(Buffer.from(request.metadata,'utf8'));
  } else if(action==='tokenCollectionDelete') {
    if(!/^[0-9]+\.[0-9]+\.[1-9][0-9]*$/.test(request.tokenId??''))
      throw new Error('Collection deletion requires an explicit nonzero numeric token ID.');
    transaction=new TokenDeleteTransaction().setTokenId(TokenId.fromString(request.tokenId));
  } else if(action==='nftCollectionCreate') {
    for(const field of ['name','symbol'])
      if(typeof request[field]!=='string'||!request[field].trim()||/[\uD800-\uDFFF]/u.test(request[field])||Buffer.byteLength(request[field],'utf8')>100)
        throw new Error('NFT collection name and symbol require 1 to 100 UTF-8 bytes.');
    if(typeof request.maxSupply!=='string'||!/^[1-9][0-9]*$/.test(request.maxSupply)||BigInt(request.maxSupply)>9223372036854775807n)
      throw new Error('NFT collection requires an explicit positive int64 maximum supply.');
    const royalty=request.royaltyPercentage??0;
    if(!Number.isInteger(royalty)||royalty<0||royalty>100)
      throw new Error('NFT collection royalty percentage must be an integer from 0 to 100.');
    transaction=new TokenCreateTransaction().setTokenName(request.name).setTokenSymbol(request.symbol)
      .setTokenType(TokenType.NonFungibleUnique).setDecimals(0).setInitialSupply(0)
      .setSupplyType(TokenSupplyType.Finite).setMaxSupply(request.maxSupply)
      .setTreasuryAccountId(sender).setAutoRenewAccountId(sender).setAdminKey(publicKey).setSupplyKey(publicKey).setMetadataKey(publicKey);
    if(royalty>0)transaction.setCustomFees([new CustomRoyaltyFee().setNumerator(royalty).setDenominator(100).setFeeCollectorAccountId(sender)]);
  } else if(action==='contractCreate') {
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

export async function executeDeployment(action,request,{createClient,restoreKey,persistPreparedSubmission}) {
  if(persistPreparedSubmission!==undefined&&typeof persistPreparedSubmission!=='function')throw new TypeError('Prepared deployment persistence must be an awaited callback.');
  const key=restoreKey(request);
  if(request.publicKey&&request.publicKey.toLowerCase()!==key.publicKey.toStringDer().toLowerCase()) throw new Error('Deployment signer mismatch.');
  const transaction=deploymentTransaction(action,request,key.publicKey);
  const client=await createClient(request);
  let stage='construction';
  try {
    client.setOperator(request.sender,key);transaction.freezeWith(client);
    stage='signing';const signed=await transaction.sign(key);
    if(persistPreparedSubmission){
      stage='preparation';
      await persistPreparedSubmission(Object.freeze({transactionHash:request.transactionId,chainId:request.network,sender:request.sender,
        signedTransactionBase64:Buffer.from(signed.toBytes()).toString('base64')}));
    }
    stage='submission';const submitted=await signed.execute(client);
    stage='receipt';const receipt=await submitted.getReceipt(client);
    if(receipt.status.toString()!=='SUCCESS') throw new Error('Deployment consensus failure.');
    return {transactionId:submitted.transactionId.toString(),status:'SUCCESS',fileId:receipt.fileId?.toString(),contractId:receipt.contractId?.toString(),tokenId:receipt.tokenId?.toString()};
  } catch(cause) {
    if(stage==='preparation')throw cause;
    const error=new Error('Deployment success was not established; reconcile the original transaction before retrying.',{cause});
    error.transactionId=request.transactionId;error.stage=stage;error.status=cause.status?.toString();throw error;
  } finally {client.close();}
}
