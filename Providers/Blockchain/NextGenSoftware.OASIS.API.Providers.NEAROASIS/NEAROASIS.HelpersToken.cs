using System;
using System.Numerics;
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
        SetNftLockAsync(request?.NFTTokenAddress, true);

    public OASISResult<IWeb3NFTTransactionResponse> UnlockNFT(IUnlockWeb3NFTRequest request) =>
        UnlockNFTAsync(request).GetAwaiter().GetResult();

    public Task<OASISResult<IWeb3NFTTransactionResponse>> UnlockNFTAsync(IUnlockWeb3NFTRequest request) =>
        SetNftLockAsync(request?.NFTTokenAddress, false);

    private async Task<OASISResult<IWeb3NFTTransactionResponse>> SetNftLockAsync(string tokenId, bool locked,
        string signerAccountId = null, string signerPrivateKey = null)
    {
        var result = new OASISResult<IWeb3NFTTransactionResponse>();
        try
        {
            if (string.IsNullOrWhiteSpace(tokenId)) throw new ArgumentException("NFT token id is required.");
            if (!await EnsureSdkActiveAsync(result)) return result;
            var tx = await _sdk.CallAsync("nft_set_locked", new { token_id = tokenId, locked },
                signerAccountId: signerAccountId, signerPrivateKey: signerPrivateKey);
            var loaded = await LoadOnChainNFTDataAsync(tokenId);
            if (loaded.IsError) throw loaded.Exception ?? new InvalidOperationException(loaded.Message);
            loaded.Result.MetaData ??= new();
            loaded.Result.MetaData["near:locked"] = locked.ToString();
            result.Result = new Web3NFTTransactionResponse { TransactionResult = tx, Web3NFT = loaded.Result };
            result.Message = $"NFT {(locked ? "locked" : "unlocked")} by the deployed NEAR contract.";
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error changing NEAR NFT lock: {ex.Message}", ex); }
        return result;
    }

    public async Task<OASISResult<BridgeTransactionResponse>> WithdrawNFTAsync(string nftTokenAddress,
        string tokenId, string senderAccountAddress, string senderPrivateKey)
    {
        var locked = await SetNftLockAsync(tokenId, true, senderAccountAddress, senderPrivateKey);
        return ToBridgeResult(locked.Result?.TransactionResult, locked.IsError, locked.Message);
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
                    : $"Source NEAR transaction {sourceTransactionHash} is not final.");
        }
        var unlocked = await SetNftLockAsync(tokenId, false);
        if (unlocked.IsError) return ToBridgeResult(null, true, unlocked.Message);
        var sent = await SendNFTAsync(new Core.Objects.NFT.Requests.SendWeb3NFTRequest
        {
            TokenId = tokenId,
            ToWalletAddress = receiverAccountAddress
        });
        return ToBridgeResult(sent.Result?.TransactionResult, sent.IsError, sent.Message);
    }

    private static OASISResult<BridgeTransactionResponse> ToBridgeResult(string transactionId, bool error, string message) =>
        new(new BridgeTransactionResponse(transactionId ?? string.Empty, null, !error, error ? message : null,
            error ? BridgeTransactionStatus.NotFound : BridgeTransactionStatus.Completed))
        {
            IsError = error,
            Message = message
        };

    public OASISResult<ITransactionResponse> SendToken(ISendWeb3TokenRequest request) =>
        SendTokenAsync(request).GetAwaiter().GetResult();

    public async Task<OASISResult<ITransactionResponse>> SendTokenAsync(ISendWeb3TokenRequest request)
    {
        var result = new OASISResult<ITransactionResponse>();
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.ToWalletAddress) || request.Amount <= 0)
                throw new ArgumentException("Destination account and a positive amount are required.");
            if (!await EnsureSdkActiveAsync(result)) return result;
            string tx;
            if (string.IsNullOrWhiteSpace(request.FromTokenAddress) ||
                string.Equals(request.FromTokenAddress, "NEAR", StringComparison.OrdinalIgnoreCase))
            {
                var yocto = new BigInteger(decimal.Round(request.Amount * 1_000_000_000_000_000_000_000_000m,
                    0, MidpointRounding.AwayFromZero)).ToString();
                tx = await _sdk.TransferAsync(request.ToWalletAddress, yocto,
                    string.IsNullOrWhiteSpace(request.FromWalletAddress) ? null : request.FromWalletAddress,
                    string.IsNullOrWhiteSpace(request.OwnerPrivateKey) ? null : request.OwnerPrivateKey);
            }
            else
            {
                tx = await _sdk.CallAsync("token_transfer", new
                {
                    symbol = request.FromTokenAddress,
                    from_id = string.IsNullOrWhiteSpace(request.FromWalletAddress) ? _accountId : request.FromWalletAddress,
                    receiver_id = request.ToWalletAddress,
                    amount = ToTokenUnits(request.Amount)
                });
            }
            await RecordWalletTransactionAsync(tx,
                string.IsNullOrWhiteSpace(request.FromWalletAddress) ? _accountId : request.FromWalletAddress,
                request.ToWalletAddress, request.Amount,
                string.IsNullOrWhiteSpace(request.FromTokenAddress) ? "Native NEAR transfer" : $"NEAR token transfer {request.FromTokenAddress}");
            result.Result = new TransactionResponse { TransactionResult = tx };
            result.Message = "Token transferred through the official NEAR SDK.";
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error sending NEAR token: {ex.Message}", ex); }
        return result;
    }
}
