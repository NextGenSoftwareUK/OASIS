using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>Invocation-owned copy of the public mint contract, captured before any await.
/// Does not validate, authorize, publish, sign or change defaults. Callers must not mutate during capture.</summary>
public static class NftMintIntent
{
    public static MintWeb3NFTRequest Capture(IMintWeb3NFTRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new MintWeb3NFTRequest
        {
            MetaData = request.MetaData == null ? null : new Dictionary<string,string>(request.MetaData, request.MetaData.Comparer),
            Tags = request.Tags == null ? null : new List<string>(request.Tags),
            Symbol = request.Symbol, MintedByAvatarId = request.MintedByAvatarId,
            Title = request.Title, Description = request.Description, MemoText = request.MemoText, Amount = request.Amount,
            JSONMetaDataURL = request.JSONMetaDataURL, JSONMetaData = request.JSONMetaData,
            Image = request.Image == null ? null : (byte[])request.Image.Clone(), ImageUrl = request.ImageUrl,
            Thumbnail = request.Thumbnail == null ? null : (byte[])request.Thumbnail.Clone(), ThumbnailUrl = request.ThumbnailUrl,
            IsForSale = request.IsForSale, SaleStartDate = request.SaleStartDate, SaleEndDate = request.SaleEndDate,
            SendToAddressAfterMinting = request.SendToAddressAfterMinting,
            SendToAvatarAfterMintingId = request.SendToAvatarAfterMintingId,
            SendToAvatarAfterMintingUsername = request.SendToAvatarAfterMintingUsername,
            SendToAvatarAfterMintingEmail = request.SendToAvatarAfterMintingEmail,
            CollectionPublicKey = request.CollectionPublicKey, NumberToMint = request.NumberToMint,
            StoreNFTMetaDataOnChain = request.StoreNFTMetaDataOnChain, OffChainProvider = request.OffChainProvider,
            OnChainProvider = request.OnChainProvider, NFTStandardType = request.NFTStandardType, NFTOffChainMetaType = request.NFTOffChainMetaType,
            Price = request.Price, Discount = request.Discount, RoyaltyPercentage = request.RoyaltyPercentage,
            NFTMetaDataMergeStrategy = request.NFTMetaDataMergeStrategy, NFTTagsMergeStrategy = request.NFTTagsMergeStrategy,
            WaitTillNFTMinted = request.WaitTillNFTMinted, WaitForNFTToMintInSeconds = request.WaitForNFTToMintInSeconds,
            AttemptToMintEveryXSeconds = request.AttemptToMintEveryXSeconds,
            WaitTillNFTVerified = request.WaitTillNFTVerified, WaitForNFTToVerifyInSeconds = request.WaitForNFTToVerifyInSeconds,
            AttemptToVerifyEveryXSeconds = request.AttemptToVerifyEveryXSeconds,
            WaitTillNFTSent = request.WaitTillNFTSent, WaitForNFTToSendInSeconds = request.WaitForNFTToSendInSeconds,
            AttemptToSendEveryXSeconds = request.AttemptToSendEveryXSeconds, FreezeMetadata = request.FreezeMetadata
        };
    }
}
