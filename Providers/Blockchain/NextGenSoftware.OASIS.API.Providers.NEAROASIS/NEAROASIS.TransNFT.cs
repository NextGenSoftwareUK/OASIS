using System;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS;

public partial class NEAROASIS
{
    public OASISResult<ITransactionResponse> SendTransaction(string fromWalletAddress, string toWalletAddress,
        decimal amount, string memoText) =>
        SendTransactionAsync(fromWalletAddress, toWalletAddress, amount, memoText).GetAwaiter().GetResult();

    public async Task<OASISResult<ITransactionResponse>> SendTransactionAsync(string fromWalletAddress,
        string toWalletAddress, decimal amount, string memoText)
    {
        var result = new OASISResult<ITransactionResponse>();
        try
        {
            if (amount <= 0 || string.IsNullOrWhiteSpace(toWalletAddress))
                throw new ArgumentException("Destination account and a positive amount are required.");
            if (!await EnsureSdkActiveAsync(result)) return result;
            var yocto = new System.Numerics.BigInteger(decimal.Round(
                amount * 1_000_000_000_000_000_000_000_000m, 0, MidpointRounding.AwayFromZero)).ToString();
            var tx = await _sdk.TransferAsync(toWalletAddress, yocto,
                string.IsNullOrWhiteSpace(fromWalletAddress) ? null : fromWalletAddress);
            await RecordWalletTransactionAsync(tx,
                string.IsNullOrWhiteSpace(fromWalletAddress) ? _accountId : fromWalletAddress,
                toWalletAddress, amount, string.IsNullOrWhiteSpace(memoText) ? "Native NEAR transfer" : memoText);
            result.Result = new TransactionResponse { TransactionResult = tx };
            result.Message = "Native NEAR transferred through near-api-js.";
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error sending NEAR transaction: {ex.Message}", ex); }
        return result;
    }

    public Task<OASISResult<IWeb3NFT>> LoadNFTAsync(string nftTokenAddress) =>
        LoadOnChainNFTDataAsync(nftTokenAddress);

    public OASISResult<IWeb3NFT> LoadNFT(string nftTokenAddress) =>
        LoadNFTAsync(nftTokenAddress).GetAwaiter().GetResult();

    public OASISResult<IWeb3NFTTransactionResponse> BurnNFT(IBurnWeb3NFTRequest request) =>
        BurnNFTAsync(request).GetAwaiter().GetResult();

    public async Task<OASISResult<IWeb3NFTTransactionResponse>> BurnNFTAsync(IBurnWeb3NFTRequest request)
    {
        var result = new OASISResult<IWeb3NFTTransactionResponse>();
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.NFTTokenAddress))
                throw new ArgumentException("NFT token id is required.");
            if (!await EnsureSdkActiveAsync(result)) return result;
            var existing = await LoadOnChainNFTDataAsync(request.NFTTokenAddress);
            if (existing.IsError) throw existing.Exception ?? new InvalidOperationException(existing.Message);
            var tx = await _sdk.CallAsync("nft_burn", new { token_id = request.NFTTokenAddress });
            result.Result = new Web3NFTTransactionResponse { TransactionResult = tx, Web3NFT = existing.Result };
            result.Message = "NFT burned by the deployed NEAR contract.";
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error burning NEAR NFT: {ex.Message}", ex); }
        return result;
    }

    public OASISResult<IWeb3NFT> LoadOnChainNFTData(string nftTokenAddress) =>
        LoadOnChainNFTDataAsync(nftTokenAddress).GetAwaiter().GetResult();

    public async Task<OASISResult<IWeb3NFT>> LoadOnChainNFTDataAsync(string nftTokenAddress)
    {
        var result = new OASISResult<IWeb3NFT>();
        try
        {
            if (string.IsNullOrWhiteSpace(nftTokenAddress)) throw new ArgumentException("NFT token id is required.");
            if (!await EnsureSdkActiveAsync(result)) return result;
            var value = await _sdk.ViewAsync("nft_token", new { token_id = nftTokenAddress });
            if (value.ValueKind == JsonValueKind.Null)
            {
                result.Message = $"NFT {nftTokenAddress} was not found on NEAR.";
                return result;
            }
            var nft = OasisJson.Deserialize<Web3NFT>(value.GetProperty("metadata_json").GetString());
            if (nft == null) throw new InvalidOperationException("The NEAR NFT metadata could not be deserialized.");
            nft.NFTTokenAddress = nftTokenAddress;
            nft.SendToAddressAfterMinting = value.GetProperty("owner_id").GetString();
            nft.MetaData ??= new();
            nft.MetaData["near:locked"] = value.GetProperty("locked").GetBoolean().ToString();
            result.Result = nft;
            result.Message = "NFT loaded from the deployed NEAR contract.";
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error loading NEAR NFT: {ex.Message}", ex); }
        return result;
    }
}
