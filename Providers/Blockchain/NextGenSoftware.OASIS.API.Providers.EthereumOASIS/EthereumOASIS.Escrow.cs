using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS;

public partial class EthereumOASIS : IOASISIdBoundTokenEscrowProvider
{
    public string TokenEscrowContractAddress { get; set; }
    private Task<OASISResult<ITransactionResponse>> ExecuteTokenEscrowAsync(Guid id,string tokenAddress,string requestedEscrow,Guid avatarId,string key,string sender,decimal amount,bool unlock)
        =>NextGenSoftware.OASIS.Providers.Shared.Evm.EvmTokenEscrow.ExecuteAsync(new()
        {
            EscrowAddress=TokenEscrowContractAddress,StorageAddress=ContractAddress,Host=HostURI,ChainId=ChainId,
            IsActivated=IsProviderActivated,TokenAbi=TokenAbi,Activate=ActivateProviderAsync,Keys=ResolveTokenSigningKeys,
            Client=account=>CreateWeb3WithAccount(account,HostURI),
            Submit=async (web3,account,function,target,operation,arguments,submitted)=>
            {
                var hash=await function.SendTransactionAsync(account.Address,new HexBigInteger(600000),null,arguments);
                submitted(hash);
                return await PollEscrowReceiptAsync(web3,hash,operation);
            },
            Response=(hash,lockId,hashes)=>new EthereumEscrowTransactionResponse(hash,lockId,hashes),
            Reconcile=(hash,stage,hashes,cause)=>new EthereumEscrowReconciliationException(hash,stage,hashes,cause)
        },id,tokenAddress,requestedEscrow,avatarId,key,sender,amount,unlock);

    protected virtual Task<TransactionReceipt> PollEscrowReceiptAsync(Web3 web3,string hash,string stage)
        =>PollTokenReceiptAsync(web3,hash);
}

[FunctionOutput]
public sealed class EthereumEscrowDeposit : IFunctionOutputDTO
{
    [Parameter("address", "owner", 1)] public string Owner { get; set; }
    [Parameter("address", "token", 2)] public string Token { get; set; }
    [Parameter("uint256", "amount", 3)] public BigInteger Amount { get; set; }
    [Parameter("bool", "released", 4)] public bool Released { get; set; }
}

public sealed class EthereumEscrowTransactionResponse : ITransactionResponse
{
    public string TransactionResult { get; set; }
    public Guid LockId { get; }
    public IReadOnlyList<string> NativeTransactions { get; }
    public EthereumEscrowTransactionResponse(string hash, Guid id, IEnumerable<string> hashes)
    { TransactionResult = hash; LockId = id; NativeTransactions = Array.AsReadOnly(hashes.ToArray()); }
}

public sealed class EthereumEscrowReconciliationException : Exception, INativeMutationReconciliationRequired
{
    public string OriginalTransactionId { get; }
    public string UncertainStage { get; }
    public IReadOnlyList<string> ConfirmedTransactions { get; }
    public EthereumEscrowReconciliationException(string hash, string stage, IEnumerable<string> confirmed, Exception cause)
        : base("Escrow approval/deposit/release state must be reconciled before replay.", cause)
    { OriginalTransactionId = hash; UncertainStage = stage; ConfirmedTransactions = Array.AsReadOnly(confirmed.ToArray()); }
}
