using System;
using System.Numerics;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS;

public partial class NEAROASIS
{
    public void Dispose() { }

    public OASISResult<IWeb3NFTTransactionResponse> LockNFT(ILockWeb3NFTRequest request) =>
        LockNFTAsync(request).GetAwaiter().GetResult();

    public Task<OASISResult<IWeb3NFTTransactionResponse>> LockNFTAsync(ILockWeb3NFTRequest request) =>
        SetAvatarNftLockAsync(request?.NFTTokenAddress, true,request?.LockedByAvatarId??Guid.Empty);

    public OASISResult<IWeb3NFTTransactionResponse> UnlockNFT(IUnlockWeb3NFTRequest request) =>
        UnlockNFTAsync(request).GetAwaiter().GetResult();

    public Task<OASISResult<IWeb3NFTTransactionResponse>> UnlockNFTAsync(IUnlockWeb3NFTRequest request) =>
        SetAvatarNftLockAsync(request?.NFTTokenAddress, false,request?.UnlockedByAvatarId??Guid.Empty);

    private async Task<OASISResult<IWeb3NFTTransactionResponse>> SetAvatarNftLockAsync(string tokenId,bool locked,Guid actor)
    {
        if(actor==Guid.Empty)return await SetNftLockAsync(tokenId,locked);
        var result=new OASISResult<IWeb3NFTTransactionResponse>();
        try {
            if(string.IsNullOrWhiteSpace(tokenId))throw new ArgumentException("NFT token id is required.");
            var signer=await ResolveLockSignerAsync(actor,null,null);
            return await SetNftLockAsync(tokenId,locked,signer.Account,signer.Key);
        }catch(Exception failure){OASISErrorHandling.HandleError(ref result,failure.Message,failure);return result;}
    }

    private async Task<OASISResult<IWeb3NFTTransactionResponse>> SetNftLockAsync(string tokenId, bool locked,
        string signerAccountId = null, string signerPrivateKey = null)
    {
        var result = new OASISResult<IWeb3NFTTransactionResponse>();
        string acknowledged=null;
        try
        {
            if (string.IsNullOrWhiteSpace(tokenId)) throw new ArgumentException("NFT token id is required.");
            if (!await EnsureSdkActiveAsync(result)) return result;
            var tx = await _sdk.CallAsync("nft_set_locked", new { token_id = tokenId, locked },
                signerAccountId: signerAccountId, signerPrivateKey: signerPrivateKey);
            acknowledged=tx;
            var loaded = await LoadOnChainNFTDataAsync(tokenId);
            if (loaded.IsError) throw loaded.Exception ?? new InvalidOperationException(loaded.Message);
            if(loaded.Result?.MetaData==null||!loaded.Result.MetaData.TryGetValue("near:locked",out var observed)
                ||!string.Equals(observed?.ToString(),locked.ToString(),StringComparison.Ordinal))
                throw new System.IO.InvalidDataException("Native NFT lock state after acknowledgement differs from the requested state.");
            result.Result = new Web3NFTTransactionResponse { TransactionResult = tx, Web3NFT = loaded.Result };
            result.Message = $"NFT {(locked ? "locked" : "unlocked")} by the deployed NEAR contract.";
        }
        catch (Exception ex) {
            if(!string.IsNullOrWhiteSpace(acknowledged))ex=new NativeTransactionFollowupException(Core.Enums.ProviderType.NEAROASIS,acknowledged,ex);
            OASISErrorHandling.HandleError(ref result, $"Error changing NEAR NFT lock: {ex.Message}", ex);
        }
        return result;
    }

    public async Task<OASISResult<BridgeTransactionResponse>> WithdrawNFTAsync(string nftTokenAddress,
        string tokenId, string senderAccountAddress, string senderPrivateKey)
    {
        var locked = await SetNftLockAsync(tokenId, true, senderAccountAddress, senderPrivateKey);
        return ToBridgeResult(locked.Result?.TransactionResult, locked.IsError, locked.Message,locked.Exception);
    }

    public async Task<OASISResult<BridgeTransactionResponse>> DepositNFTAsync(string nftTokenAddress,
        string tokenId, string receiverAccountAddress, string sourceTransactionHash = null)
    {
        if (!string.IsNullOrWhiteSpace(sourceTransactionHash))
        {
            var sourceStatus = await GetTransactionStatusAsync(sourceTransactionHash);
            if (sourceStatus.IsError || sourceStatus.Result != BridgeTransactionStatus.Completed)
                return ToBridgeResult(null, true, sourceStatus.IsError
                    ? sourceStatus.Message
                    : $"Source NEAR transaction {sourceTransactionHash} is not final.",sourceStatus.Exception);
        }
        var unlocked = await SetNftLockAsync(tokenId, false);
        if (unlocked.IsError) return ToBridgeResult(null, true, unlocked.Message,unlocked.Exception);
        var sent = await SendNFTAsync(new Core.Objects.NFT.Requests.SendWeb3NFTRequest
        {
            TokenId = tokenId,
            ToWalletAddress = receiverAccountAddress
        });
        if(sent.IsError){
            var failure=new NativeTransactionFollowupException(Core.Enums.ProviderType.NEAROASIS,unlocked.Result.TransactionResult,
                sent.Exception??new InvalidOperationException(sent.Message));
            return ToBridgeResult(unlocked.Result.TransactionResult,true,sent.Message,failure);
        }
        return ToBridgeResult(sent.Result?.TransactionResult, false, sent.Message);
    }

    private static OASISResult<BridgeTransactionResponse> ToBridgeResult(string transactionId,bool error,string message,Exception failure=null)
    {
        transactionId??=failure switch{
            NativeTransactionFollowupException followup=>followup.OriginalTransactionId,
            NearMutationUncertainException uncertain=>uncertain.OriginalTransactionId,
            NearMutationRejectedException rejected=>rejected.OriginalTransactionId,
            _=>null
        };
        var status=!error?BridgeTransactionStatus.Completed:failure is NearMutationRejectedException?BridgeTransactionStatus.Failed:BridgeTransactionStatus.Pending;
        return new(new BridgeTransactionResponse(transactionId??string.Empty,null,!error,error?message:null,status)){
            IsError=error,Message=message,Exception=failure
        };
    }

    public OASISResult<ITransactionResponse> SendToken(ISendWeb3TokenRequest request) =>
        SendTokenAsync(request).GetAwaiter().GetResult();

    public async Task<OASISResult<ITransactionResponse>> SendTokenAsync(ISendWeb3TokenRequest request)
    {
        var result = new OASISResult<ITransactionResponse>();
        string acknowledged=null;
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.ToWalletAddress) || request.Amount <= 0)
                throw new ArgumentException("Destination account and a positive amount are required.");
            // Freeze all used caller fields before activation or any SDK await.
            var destination=request.ToWalletAddress;var source=request.FromWalletAddress;
            var tokenAddress=request.FromTokenAddress;var ownerKey=request.OwnerPrivateKey;var amount=request.Amount;
            var publicKey=request.OwnerPublicKey;var seed=request.OwnerSeedPhrase;
            var native=string.IsNullOrWhiteSpace(tokenAddress)||string.Equals(tokenAddress,"NEAR",StringComparison.OrdinalIgnoreCase);
            var units=native?NearNativeAmount.ToYocto(amount):ToTokenUnits(amount);
            var knownKey=await ResolveNearExplicitKeyAsync(ownerKey,seed);
            var owner=string.IsNullOrWhiteSpace(source)?knownKey?.ImplicitAccountId??_accountId:source;
            var signer=await ResolveNearSignerAsync(owner,ownerKey,publicKey,knownKey);
            if (!await EnsureSdkActiveAsync(result)) return result;
            string tx;
            if (native)
            {
                tx = await _sdk.TransferAsync(destination, units,signer.Account,signer.Key);
            }
            else
            {
                tx = await _sdk.CallAsync("token_transfer", new
                {
                    symbol = tokenAddress,
                    from_id = signer.Account,
                    receiver_id = destination,
                    amount = units
                },signerAccountId:signer.Account,signerPrivateKey:signer.Key);
            }
            acknowledged=tx;
            await RecordWalletTransactionAsync(tx,
                signer.Account,destination,amount,
                native ? "Native NEAR transfer" : $"NEAR token transfer {tokenAddress}");
            result.Result = new TransactionResponse { TransactionResult = tx };
            result.Message = "Token transferred through the official NEAR SDK.";
        }
        catch (Exception ex) {
            if(!string.IsNullOrWhiteSpace(acknowledged))ex=new NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.NativeTransactionFollowupException(Core.Enums.ProviderType.NEAROASIS,acknowledged,ex);
            OASISErrorHandling.HandleError(ref result, $"Error sending NEAR token: {ex.Message}", ex);
        }
        return result;
    }
}
