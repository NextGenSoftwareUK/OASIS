using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS;

public partial class NEAROASIS
{
    public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long x, long y, int radius)
    {
        var result = LoadAllAvatarsAsync().GetAwaiter().GetResult();
        if (result.IsError) return result;
        var latitude = x / 1_000_000d;
        var longitude = y / 1_000_000d;
        result.Result = result.Result.Where(avatar => avatar.MetaData != null &&
            avatar.MetaData.TryGetValue("Latitude", out var lat) && avatar.MetaData.TryGetValue("Longitude", out var lon) &&
            GeoHelper.CalculateDistance(latitude, longitude, Convert.ToDouble(lat), Convert.ToDouble(lon)) <= radius).ToList();
        return result;
    }

    public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long x, long y, int radius, HolonType holonType)
    {
        var result = LoadAllHolonsAsync(holonType).GetAwaiter().GetResult();
        if (result.IsError) return result;
        var latitude = x / 1_000_000d;
        var longitude = y / 1_000_000d;
        result.Result = result.Result.Where(holon => holon.MetaData != null &&
            holon.MetaData.TryGetValue("Latitude", out var lat) && holon.MetaData.TryGetValue("Longitude", out var lon) &&
            GeoHelper.CalculateDistance(latitude, longitude, Convert.ToDouble(lat), Convert.ToDouble(lon)) <= radius).ToList();
        return result;
    }

    public OASISResult<IWeb3NFTTransactionResponse> SendNFT(ISendWeb3NFTRequest request) =>
        SendNFTAsync(request).GetAwaiter().GetResult();

    public async Task<OASISResult<IWeb3NFTTransactionResponse>> SendNFTAsync(ISendWeb3NFTRequest request)
    {
        var result = new OASISResult<IWeb3NFTTransactionResponse>();
        try
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var tokenId = request.TokenId ?? request.FromNFTTokenAddress ?? request.TokenAddress;
            if (string.IsNullOrWhiteSpace(tokenId) || string.IsNullOrWhiteSpace(request.ToWalletAddress))
                throw new ArgumentException("NEAR NFT token id and destination account are required.");
            if (!await EnsureSdkActiveAsync(result)) return result;
            var before = await LoadOnChainNFTDataAsync(tokenId);
            if (before.IsError || before.Result == null)
                throw before.Exception ?? new InvalidOperationException(before.Message);
            var tx = await _sdk.CallAsync("nft_transfer", new { token_id = tokenId, receiver_id = request.ToWalletAddress });
            await RecordWalletTransactionAsync(tx, before.Result.SendToAddressAfterMinting, request.ToWalletAddress,
                1m, $"NEAR NFT transfer {tokenId}", Core.Enums.TransactionCategory.NFTs);
            var loaded = await LoadOnChainNFTDataAsync(tokenId);
            if (loaded.IsError) throw loaded.Exception ?? new InvalidOperationException(loaded.Message);
            if (loaded.Result != null)
            {
                loaded.Result.SendToAddressAfterMinting = request.ToWalletAddress;
                loaded.Result.SendNFTTransactionHash = tx;
            }
            result.Result = new Web3NFTTransactionResponse
            {
                TransactionResult = tx,
                SendNFTTransactionResult = tx,
                Web3NFT = loaded.Result
            };
            result.Message = "NFT transferred by the deployed NEAR contract.";
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error sending NEAR NFT: {ex.Message}", ex); }
        return result;
    }

    public OASISResult<IWeb3NFTTransactionResponse> MintNFT(IMintWeb3NFTRequest request) =>
        MintNFTAsync(request).GetAwaiter().GetResult();

    public async Task<OASISResult<IWeb3NFTTransactionResponse>> MintNFTAsync(IMintWeb3NFTRequest request)
    {
        var result = new OASISResult<IWeb3NFTTransactionResponse>();
        try
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (!await EnsureSdkActiveAsync(result)) return result;
            var tokenId = Guid.NewGuid().ToString("N");
            var ownerId = string.IsNullOrWhiteSpace(request.SendToAddressAfterMinting) ? _accountId : request.SendToAddressAfterMinting;
            if (string.IsNullOrWhiteSpace(ownerId)) throw new InvalidOperationException("A NEAR owner account is required to mint an NFT.");
            var nft = new Web3NFT
            {
                Id = Guid.NewGuid(),
                NFTTokenAddress = tokenId,
                NFTMintedUsingWalletAddress = _accountId,
                SendToAddressAfterMinting = ownerId,
                Title = request.Title,
                Description = request.Description,
                Symbol = request.Symbol,
                JSONMetaData = request.JSONMetaData,
                JSONMetaDataURL = request.JSONMetaDataURL,
                ImageUrl = request.ImageUrl,
                ThumbnailUrl = request.ThumbnailUrl,
                MetaData = request.MetaData == null ? new Dictionary<string, string>() : new Dictionary<string, string>(request.MetaData),
                Tags = request.Tags?.ToList() ?? new List<string>(),
                MintedByAvatarId = request.MintedByAvatarId,
                MintedOn = DateTime.UtcNow,
                OnChainProvider = new EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.NEAROASIS)
            };
            var tx = await _sdk.CallAsync("nft_mint", new
            {
                token_id = tokenId,
                owner_id = ownerId,
                metadata_json = OasisJson.Serialize(nft)
            });
            nft.MintTransactionHash = tx;
            result.Result = new Web3NFTTransactionResponse { TransactionResult = tx, Web3NFT = nft };
            result.Message = "NFT minted by the deployed NEAR contract.";
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error minting NEAR NFT: {ex.Message}", ex); }
        return result;
    }

    private static bool MetaMatches(IHolon holon, string key, string value) =>
        holon.MetaData != null && holon.MetaData.TryGetValue(key, out var stored) &&
        string.Equals(stored?.ToString(), value, StringComparison.Ordinal);

    public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string key, string value,
        HolonType holonType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0,
        int version = 0, bool continueOnError = true, bool loadChildrenRecursiveDepth = true,
        int loadChildrenRecursiveDepthInt = 0)
    {
        var all = await LoadAllHolonsAsync(holonType, loadChildren, recursive, maxChildDepth, version,
            continueOnError, loadChildrenRecursiveDepth, loadChildrenRecursiveDepthInt);
        if (!all.IsError) all.Result = all.Result.Where(holon => MetaMatches(holon, key, value)).ToList();
        return all;
    }

    public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string key, string value,
        HolonType holonType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0,
        int version = 0, bool continueOnError = true, bool loadChildrenRecursiveDepth = true,
        int loadChildrenRecursiveDepthInt = 0) =>
        LoadHolonsByMetaDataAsync(key, value, holonType, loadChildren, recursive, maxChildDepth, version,
            continueOnError, loadChildrenRecursiveDepth, loadChildrenRecursiveDepthInt).GetAwaiter().GetResult();

    public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(
        Dictionary<string, string> metaData, MetaKeyValuePairMatchMode matchMode, HolonType holonType,
        bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0,
        bool continueOnError = true, bool loadChildrenRecursiveDepth = true, int loadChildrenRecursiveDepthInt = 0)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        if (metaData == null || metaData.Count == 0)
        {
            OASISErrorHandling.HandleError(ref result, "At least one metadata key/value pair is required.");
            return result;
        }
        var all = await LoadAllHolonsAsync(holonType, loadChildren, recursive, maxChildDepth, version,
            continueOnError, loadChildrenRecursiveDepth, loadChildrenRecursiveDepthInt);
        if (all.IsError) return all;
        bool Matches(IHolon holon) => matchMode == MetaKeyValuePairMatchMode.Any
            ? metaData.Any(pair => MetaMatches(holon, pair.Key, pair.Value))
            : metaData.All(pair => MetaMatches(holon, pair.Key, pair.Value));
        all.Result = all.Result.Where(Matches).ToList();
        return all;
    }

    public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaData,
        MetaKeyValuePairMatchMode matchMode, HolonType holonType, bool loadChildren = true,
        bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true,
        bool loadChildrenRecursiveDepth = true, int loadChildrenRecursiveDepthInt = 0) =>
        LoadHolonsByMetaDataAsync(metaData, matchMode, holonType, loadChildren, recursive, maxChildDepth,
            version, continueOnError, loadChildrenRecursiveDepth, loadChildrenRecursiveDepthInt).GetAwaiter().GetResult();
}
