using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nethereum.Web3;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

public partial class Web3CoreOASISBaseProvider : IOASISIdBoundTokenEscrowProvider
{
    public string TokenEscrowContractAddress { get; set; }

    private Task<OASISResult<ITransactionResponse>> ExecuteTokenEscrowAsync(Guid id,string tokenAddress,string requestedEscrow,Guid avatarId,string key,string sender,decimal amount,bool unlock)
        =>NextGenSoftware.OASIS.Providers.Shared.Evm.EvmTokenEscrow.ExecuteAsync(new()
        {
            EscrowAddress=TokenEscrowContractAddress,StorageAddress=_contractAddress,Host=_hostURI,ChainId=_expectedChainId??0,
            IsActivated=IsProviderActivated,TokenAbi=Erc20MutationAbi,Activate=ActivateProviderAsync,Keys=ResolveErc20MintSigningKeys,
            Client=account=>new Web3(account,_hostURI),
            Submit=async (web3,account,function,target,operation,arguments,submitted)=>
            {
                var signer=_web3CoreOASIS.ForContractTransaction(account.PrivateKey,target,"[]");
                var receipt=await signer.SendContractCallAsync(function.GetData(arguments));
                submitted(receipt.TransactionHash);
                return receipt;
            },
            Response=(hash,lockId,hashes)=>new Web3EscrowTransactionResponse(hash,lockId,hashes),
            Reconcile=(hash,stage,hashes,cause)=>new Web3EscrowReconciliationException(hash,stage,hashes,cause)
        },id,tokenAddress,requestedEscrow,avatarId,key,sender,amount,unlock);
}

public sealed class Web3EscrowTransactionResponse : ITransactionResponse
{
    public string TransactionResult { get; set; }
    public Guid LockId { get; }
    public IReadOnlyList<string> NativeTransactions { get; }
    public Web3EscrowTransactionResponse(string hash,Guid id,IEnumerable<string> hashes)
    {TransactionResult=hash;LockId=id;NativeTransactions=Array.AsReadOnly(hashes.ToArray());}
}

public sealed class Web3EscrowReconciliationException : Exception,INativeMutationReconciliationRequired
{
    public string OriginalTransactionId { get; }
    public string UncertainStage { get; }
    public IReadOnlyList<string> ConfirmedTransactions { get; }
    public Web3EscrowReconciliationException(string hash,string stage,IEnumerable<string> confirmed,Exception cause)
        :base("Escrow approval/deposit/release state must be reconciled before replay.",cause)
    {OriginalTransactionId=hash;UncertainStage=stage;ConfirmedTransactions=Array.AsReadOnly(confirmed.ToArray());}
}
