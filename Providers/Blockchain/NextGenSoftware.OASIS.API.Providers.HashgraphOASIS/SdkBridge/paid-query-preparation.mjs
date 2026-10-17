import {AccountInfoQuery,ContractInfoQuery,ContractByteCodeQuery,ContractCallQuery,FileContentsQuery,TokenInfoQuery,TokenNftInfoQuery,
  AccountId,PublicKey,Transaction,TransferTransaction} from '@hiero-ledger/sdk';
import {proto} from '@hiero-ledger/proto';

// Pinned SDK2.86.2 extension seam. No prototype patching: each explicit query subclass
// owns its selected request, after node selection and before the SDK's gRPC deadline race.
// Do not move persistence into _execute(): that race can return while a delayed hook
// subsequently sends the payment. Candidate signing is not a transport boundary either.
function preparedQuery(Base,field) {
  return class extends Base {
    #context;
    #issued=false;
    constructor(context) {
      super();
      if(!context||typeof context.persist!=='function'||typeof context.network!=='string'||!context.network.trim()
          ||!Array.isArray(context.nodes)||context.nodes.length===0||context.nodes.length>1024
          ||!Number.isInteger(context.persistenceTimeoutMs)||context.persistenceTimeoutMs<1||context.persistenceTimeoutMs>60000
          ||!/^[1-9][0-9]*$/.test(context.maxPaymentTinybars??'')||BigInt(context.maxPaymentTinybars)>9223372036854775807n
          ||!/^[1-9][0-9]*$/.test(context.maxTransactionFeeTinybars??'')||BigInt(context.maxTransactionFeeTinybars)>9223372036854775807n)
        throw new Error('Explicit bounded paid-query admission context is required.');
      const payer=AccountId.fromString(context.payer).toString();
      const nodes=context.nodes.map(value=>AccountId.fromString(value).toString());
      if(new Set(nodes).size!==nodes.length||nodes.includes(payer))throw new Error('Distinct authorized query nodes are required.');
      this.#context=Object.freeze({...context,payer,nodes:Object.freeze(nodes),publicKey:PublicKey.fromString(context.publicKey)});
      this.setMaxAttempts(1); // An unknown paid query is not permission to create another payment.
    }
    setMaxAttempts(value) {
      if(value!==1)throw new Error('Prepared paid queries require exactly one transport attempt.');
      return super.setMaxAttempts(value);
    }
    async _executeRequestWithGrpcDeadline(channel,request) {
      if(this.#issued)throw new Error('This prepared paid query was already issued; original outcome requires reconciliation.');
      const context=this.#context;
      const bytes=proto.Query.encode(request).finish();
      if(bytes.length>1048576)throw new Error('Bounded selected paid-query request is required.');
      request=proto.Query.decode(bytes); // Own immutable-by-isolation request across awaited persistence.
      const header=request[field]?.header;
      if(header?.responseType!==proto.ResponseType.ANSWER_ONLY||!header.payment?.signedTransactionBytes?.length)
        throw new Error('Selected paid ANSWER_ONLY request with signed original payment is required.');
      const payment=Transaction.fromBytes(proto.Transaction.encode(header.payment).finish());
      if(!(payment instanceof TransferTransaction)||payment.transactionId.accountId.toString()!==context.payer
          ||payment.nodeAccountIds.length!==1||!context.nodes.includes(payment.nodeAccountIds[0].toString())
          ||!context.publicKey.verifyTransaction(payment))
        throw new Error('Selected query payment does not bind admitted payer, node and signature.');
      const signed=proto.SignedTransaction.decode(header.payment.signedTransactionBytes);
      const body=proto.TransactionBody.decode(signed.bodyBytes);
      if(BigInt(body.transactionFee.toString())<0n||BigInt(body.transactionFee.toString())>BigInt(context.maxTransactionFeeTinybars))
        throw new Error('Selected query payment transaction fee ceiling exceeds admission.');
      const rows=body.cryptoTransfer?.transfers?.accountAmounts;
      const node=payment.nodeAccountIds[0].toString();
      if(!rows||rows.length!==2||body.cryptoTransfer.tokenTransfers?.length)
        throw new Error('Query payment must contain only payer-to-node HBAR transfers.');
      const accounts=rows.map(row=>AccountId.fromBytes(proto.AccountID.encode(row.accountID).finish()).toString());
      const units=rows.map(row=>BigInt(row.amount.toString()));
      const payerIndex=accounts.indexOf(context.payer),nodeIndex=accounts.indexOf(node);
      if(payerIndex<0||nodeIndex<0||payerIndex===nodeIndex||units[nodeIndex]<0n
          ||units[payerIndex]!==-units[nodeIndex]||units[nodeIndex]>BigInt(context.maxPaymentTinybars))
        throw new Error('Selected query payment exceeds admitted node/payment authority.');
      const evidence=Object.freeze({purpose:'HederaQueryPayment',chainId:context.network,sender:context.payer,
        transactionHash:payment.transactionId.toString(),signedTransactionBase64:Buffer.from(payment.toBytes()).toString('base64'),
        queryNodeAccountId:node,queryPaymentTinybars:units[nodeIndex].toString()});
      this.#issued=true;
      let timer;
      try {
        await Promise.race([Promise.resolve().then(()=>context.persist(evidence)),new Promise((_,reject)=>{
          timer=setTimeout(()=>reject(new Error('Paid-query admission timed out; request was not released.')),context.persistenceTimeoutMs);
        })]);
      }finally{clearTimeout(timer);}
      return super._executeRequestWithGrpcDeadline(channel,request);
    }
  };
}

export const PreparedAccountInfoQuery=preparedQuery(AccountInfoQuery,'cryptoGetInfo');
export const PreparedContractInfoQuery=preparedQuery(ContractInfoQuery,'contractGetInfo');
export const PreparedContractByteCodeQuery=preparedQuery(ContractByteCodeQuery,'contractGetBytecode');
export const PreparedContractCallQuery=preparedQuery(ContractCallQuery,'contractCallLocal');
export const PreparedFileContentsQuery=preparedQuery(FileContentsQuery,'fileGetContents');
export const PreparedTokenInfoQuery=preparedQuery(TokenInfoQuery,'tokenGetInfo');
export const PreparedTokenNftInfoQuery=preparedQuery(TokenNftInfoQuery,'tokenGetNftInfo');
