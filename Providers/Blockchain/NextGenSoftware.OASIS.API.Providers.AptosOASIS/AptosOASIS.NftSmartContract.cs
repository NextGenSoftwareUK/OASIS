using System;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using System.Net.Http;
using System.Text.Json;

namespace NextGenSoftware.OASIS.API.Providers.AptosOASIS;

public partial class AptosOASIS
{
    private const string NftRecordType = "nft";

    private static OASISResult<IWeb3NFTTransactionResponse> NftError(string message, Exception ex = null) =>
        new() { IsError = true, Message = message, Exception = ex };

    private async Task<(Web3NFT Nft, string Hash)> SaveNftAsync(Web3NFT nft)
    {
        var hash = await UpsertRecordWithHashAsync(NftRecordType, nft.NFTTokenAddress, nft);
        return (nft, hash);
    }

    public OASISResult<IWeb3NFTTransactionResponse> MintNFT(IMintWeb3NFTRequest request) => MintNFTAsync(request).Result;
    public async Task<OASISResult<IWeb3NFTTransactionResponse>> MintNFTAsync(IMintWeb3NFTRequest request)
    {
        try
        {
            if (!_isActivated) return NftError("Aptos provider is not activated.");
            var id = Guid.NewGuid();
            var nft = new Web3NFT
            {
                Id = id, NFTTokenAddress = id.ToString("N"), Title = request.Title, Description = request.Description,
                Symbol = request.Symbol, JSONMetaData = request.JSONMetaData, JSONMetaDataURL = request.JSONMetaDataURL,
                ImageUrl = request.ImageUrl, ThumbnailUrl = request.ThumbnailUrl, MetaData = request.MetaData,
                Tags = request.Tags, MintedByAvatarId = request.MintedByAvatarId, MintedOn = DateTime.UtcNow,
                SendToAddressAfterMinting = request.SendToAddressAfterMinting,
                NFTMintedUsingWalletAddress = _account.Address.ToString(), OASISMintWalletAddress = _account.Address.ToString(),
                OnChainProvider = new(Core.Enums.ProviderType.AptosOASIS), StoreNFTMetaDataOnChain = true
            };
            var saved = await SaveNftAsync(nft);
            nft.MintTransactionHash = saved.Hash;
            return new(new Web3NFTTransactionResponse { Web3NFT = nft, TransactionResult = saved.Hash }) { Message = "OASIS NFT minted into the Aptos Move contract by a signed SDK transaction." };
        }
        catch (Exception ex) { return NftError($"Error minting Aptos NFT: {ex.Message}", ex); }
    }

    public OASISResult<IWeb3NFT> LoadOnChainNFTData(string nftTokenAddress) => LoadOnChainNFTDataAsync(nftTokenAddress).Result;
    public async Task<OASISResult<IWeb3NFT>> LoadOnChainNFTDataAsync(string nftTokenAddress)
    {
        try
        {
            var nft = await GetRecordAsync<Web3NFT>(NftRecordType, nftTokenAddress);
            return new(nft) { Message = nft == null ? $"Aptos NFT '{nftTokenAddress}' was not found." : "Aptos NFT loaded from Move storage." };
        }
        catch (Exception ex) { return new() { IsError = true, Message = ex.Message, Exception = ex }; }
    }

    public OASISResult<IWeb3NFTTransactionResponse> SendNFT(ISendWeb3NFTRequest request) => SendNFTAsync(request).Result;
    public async Task<OASISResult<IWeb3NFTTransactionResponse>> SendNFTAsync(ISendWeb3NFTRequest request)
    {
        try
        {
            var key = string.IsNullOrWhiteSpace(request.TokenId) ? request.TokenAddress : request.TokenId;
            var loaded = await LoadOnChainNFTDataAsync(key);
            if (loaded.Result is not Web3NFT nft) return NftError(loaded.Message);
            nft.SendToAddressAfterMinting = request.ToWalletAddress;
            var saved = await SaveNftAsync(nft); nft.SendNFTTransactionHash = saved.Hash;
            return new(new Web3NFTTransactionResponse { Web3NFT = nft, TransactionResult = saved.Hash, SendNFTTransactionResult = saved.Hash }) { Message = "Aptos NFT ownership updated by a signed SDK transaction." };
        }
        catch (Exception ex) { return NftError($"Error sending Aptos NFT: {ex.Message}", ex); }
    }

    public OASISResult<IWeb3NFTTransactionResponse> BurnNFT(IBurnWeb3NFTRequest request) => BurnNFTAsync(request).Result;
    public async Task<OASISResult<IWeb3NFTTransactionResponse>> BurnNFTAsync(IBurnWeb3NFTRequest request)
    {
        try
        {
            var key = string.IsNullOrWhiteSpace(request.NFTTokenAddress) ? request.Web3NFTId.ToString("N") : request.NFTTokenAddress;
            var loaded = await LoadOnChainNFTDataAsync(key);
            if (loaded.Result == null) return NftError(loaded.Message);
            var hash = await DeleteRecordWithHashAsync(NftRecordType, key);
            return new(new Web3NFTTransactionResponse { Web3NFT = loaded.Result, TransactionResult = hash }) { Message = "Aptos NFT burned by a signed SDK transaction." };
        }
        catch (Exception ex) { return NftError($"Error burning Aptos NFT: {ex.Message}", ex); }
    }

    private async Task<OASISResult<IWeb3NFTTransactionResponse>> SetNftLockAsync(string key, Guid avatarId, bool locked)
    {
        var loaded = await LoadOnChainNFTDataAsync(key);
        if (loaded.Result is not Web3NFT nft) return NftError(loaded.Message);
        nft.MetaData ??= new(); nft.MetaData["aptos:locked"] = locked.ToString(); nft.MetaData["aptos:lock-avatar"] = avatarId.ToString();
        var saved = await SaveNftAsync(nft);
        return new(new Web3NFTTransactionResponse { Web3NFT = nft, TransactionResult = saved.Hash }) { Message = locked ? "Aptos NFT locked." : "Aptos NFT unlocked." };
    }
    public OASISResult<IWeb3NFTTransactionResponse> LockNFT(ILockWeb3NFTRequest request) => LockNFTAsync(request).Result;
    public Task<OASISResult<IWeb3NFTTransactionResponse>> LockNFTAsync(ILockWeb3NFTRequest request) => SetNftLockAsync(string.IsNullOrWhiteSpace(request.NFTTokenAddress) ? request.Web3NFTId.ToString("N") : request.NFTTokenAddress, request.LockedByAvatarId, true);
    public OASISResult<IWeb3NFTTransactionResponse> UnlockNFT(IUnlockWeb3NFTRequest request) => UnlockNFTAsync(request).Result;
    public Task<OASISResult<IWeb3NFTTransactionResponse>> UnlockNFTAsync(IUnlockWeb3NFTRequest request) => SetNftLockAsync(string.IsNullOrWhiteSpace(request.NFTTokenAddress) ? request.Web3NFTId.ToString("N") : request.NFTTokenAddress, request.UnlockedByAvatarId, false);

    public async Task<OASISResult<BridgeTransactionResponse>> WithdrawNFTAsync(string nftTokenAddress, string tokenId, string senderAccountAddress, string senderPrivateKey)
    { var r = await SetNftLockAsync(string.IsNullOrWhiteSpace(tokenId) ? nftTokenAddress : tokenId, Guid.Empty, true); return BridgeResult(r.Result?.TransactionResult, r.IsError, r.Message); }
    public async Task<OASISResult<BridgeTransactionResponse>> DepositNFTAsync(string nftTokenAddress, string tokenId, string receiverAccountAddress, string sourceTransactionHash = null)
    { var r = await SetNftLockAsync(string.IsNullOrWhiteSpace(tokenId) ? nftTokenAddress : tokenId, Guid.Empty, false); return BridgeResult(r.Result?.TransactionResult, r.IsError, r.Message); }

    public async Task<OASISResult<BridgeTransactionResponse>> WithdrawAsync(decimal amount, string senderAccountAddress, string senderPrivateKey)
    {
        if (!string.Equals(senderAccountAddress?.TrimStart('0', 'x'), _account.Address.ToString().TrimStart('0', 'x'), StringComparison.OrdinalIgnoreCase))
            return BridgeResult(null, true, "Withdrawal sender must match the configured Aptos signing account.");
        var sent = await SendTokenAsync(new SendWeb3TokenRequest { FromWalletAddress = senderAccountAddress, ToWalletAddress = _contractAddress, Amount = amount });
        return BridgeResult(sent.Result?.TransactionResult, sent.IsError, sent.Message);
    }

    public async Task<OASISResult<BridgeTransactionResponse>> DepositAsync(decimal amount, string receiverAccountAddress)
    {
        var sent = await SendTokenAsync(new SendWeb3TokenRequest { FromWalletAddress = _account.Address.ToString(), ToWalletAddress = receiverAccountAddress, Amount = amount });
        return BridgeResult(sent.Result?.TransactionResult, sent.IsError, sent.Message);
    }

    public async Task<OASISResult<BridgeTransactionStatus>> GetTransactionStatusAsync(string transactionHash, CancellationToken token = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync($"/v1/transactions/by_hash/{transactionHash}", token);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return new(BridgeTransactionStatus.NotFound);
            response.EnsureSuccessStatusCode();
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(token));
            var success = json.RootElement.TryGetProperty("success", out var value) && value.GetBoolean();
            return new(success ? BridgeTransactionStatus.Completed : BridgeTransactionStatus.Pending);
        }
        catch (Exception ex) { return new() { IsError = true, Message = ex.Message, Exception = ex }; }
    }

    public OASISResult<string> SendSmartContractFunction(string contractAddress, string functionName, params object[] parameters) => SendSmartContractFunctionAsync(contractAddress, functionName, parameters).Result;
    public async Task<OASISResult<string>> SendSmartContractFunctionAsync(string contractAddress, string functionName, params object[] parameters)
    {
        try
        {
            var id = functionName.Contains("::") ? functionName : $"{contractAddress}::{functionName}";
            var hash = await ExecuteEntryFunctionAsync(id, Array.Empty<object>(), parameters);
            return new(hash) { Message = "Aptos entry function executed by a signed SDK transaction." };
        }
        catch (Exception ex) { return new() { IsError = true, Message = ex.Message, Exception = ex }; }
    }

    private static OASISResult<BridgeTransactionResponse> BridgeResult(string hash, bool error, string message) => new(new BridgeTransactionResponse
    { TransactionId = hash ?? string.Empty, IsSuccessful = !error, ErrorMessage = error ? message : null, Status = error ? BridgeTransactionStatus.NotFound : BridgeTransactionStatus.Completed }) { IsError = error, Message = message };
}
