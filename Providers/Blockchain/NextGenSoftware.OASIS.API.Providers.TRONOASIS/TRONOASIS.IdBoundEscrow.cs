using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS;

/// <summary>Explicit per-deposit contract selection; does not select or upgrade an aggregate ABI.</summary>
public sealed record TronIdBoundEscrowConfiguration(string ContractAddress, string RuntimeSha256)
{
    internal void Validate()
    {
        var address=NBitcoin.DataEncoders.Encoders.Base58Check.DecodeData(ContractAddress ?? "");
        if(address.Length!=21 || address[0]!=0x41
            || !System.Text.RegularExpressions.Regex.IsMatch(RuntimeSha256 ?? "",@"\A[0-9a-fA-F]{64}\z"))
            throw new ArgumentException("A current TRON contract and reviewed runtime SHA256 are required for per-deposit custody.");
    }
}

public sealed class TronIdBoundEscrowResponse : ITransactionResponse
{
    public string TransactionResult { get; set; }
    public Guid LockId { get; }
    public IReadOnlyList<string> NativeTransactions { get; }
    internal TronIdBoundEscrowResponse(TronNativeTransferClient.IdBoundEscrowResult native)
    {
        TransactionResult=native.TransactionId;LockId=native.LockId;
        NativeTransactions=Array.AsReadOnly(native.ApprovalTransactionId==null
            ? new[]{native.TransactionId}:new[]{native.ApprovalTransactionId,native.TransactionId});
    }
}

public partial class TRONOASIS
{
    private async Task<OASISResult<ITransactionResponse>> ExecuteIdBoundTokenEscrowAsync(Guid id, string token,
        string requestedEscrow, Guid avatar, string sender, string key, decimal amount, bool unlock)
    {
        var result=new OASISResult<ITransactionResponse>();
        try
        {
            // Scalar intent captured by the public method before the first await.
            if(id==Guid.Empty || avatar==Guid.Empty || (!unlock && amount<=0)
                || string.IsNullOrWhiteSpace(token) || requestedEscrow!=_idBoundEscrow.ContractAddress)
                throw new ArgumentException("Explicit deposit/owner identity, token, positive lock amount and matching configured per-deposit escrow are required.");
            if(unlock)
            {
                var wallet=await WalletManager.GetAvatarDefaultWalletByIdAsync(avatar,Core.Enums.ProviderType.TRONOASIS,showPrivateKeys:true);
                if(wallet.IsError)
                    return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<Core.Interfaces.IProviderWallet,ITransactionResponse>(wallet);
                if(wallet.Result==null || wallet.Result.AvatarId!=avatar || string.IsNullOrWhiteSpace(wallet.Result.PrivateKey))
                    throw new InvalidOperationException("Could not resolve the requesting avatar's depositor signing wallet.");
                sender=wallet.Result.WalletAddress;key=wallet.Result.PrivateKey;
            }
            else if(string.IsNullOrWhiteSpace(sender) || string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Explicit depositor address and private signing key are required.");
            var native=await ExecuteNativeRuntimeAsync(client=>client.ExecuteIdBoundEscrowAsync(sender,
                _idBoundEscrow.ContractAddress,token,id,amount,key,_idBoundEscrow.RuntimeSha256,unlock));
            result.Result=new TronIdBoundEscrowResponse(native);
            result.IsSaved=true;result.SavedCount=1;
            result.Message=unlock?"Owned per-deposit escrow release confirmed.":"Explicit amount and owned deposit confirmed.";
        }
        catch(Exception error)
        {
            OASISErrorHandling.HandleError(ref result,"TRON per-deposit escrow failed: "+error.Message,error);
            if(error is ArgumentException)result.ErrorCode="HYPERDRIVE_PAYLOAD_INVALID";
            if(error is StorageLifecycleConflictException)result.ErrorCode=StorageLifecycleConflictException.ErrorCode;
        }
        return result;
    }
}
