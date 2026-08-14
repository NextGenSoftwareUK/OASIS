using Ipfs;
using Newtonsoft.Json;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers.Base;
using NextGenSoftware.OASIS.API.Providers.IPFSOASIS;
using NextGenSoftware.OASIS.API.Providers.PinataOASIS;
using NextGenSoftware.Logging;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    public partial class NFTManager
    {
        public async Task<OASISResult<IList<IWeb4GeoSpatialNFT>>> LoadChildWeb4GeoNFTsForNFTCollectionAsync(List<string> Web4GeoNFTIds, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IList<IWeb4GeoSpatialNFT>> result = new OASISResult<IList<IWeb4GeoSpatialNFT>>();

            if (Web4GeoNFTIds != null && Web4GeoNFTIds.Count > 0)
            {
                result.Result = new List<IWeb4GeoSpatialNFT>();

                foreach (string nftId in Web4GeoNFTIds)
                {
                    OASISResult<IWeb4GeoSpatialNFT> nftRes = await LoadWeb4GeoNftAsync(Guid.Parse(nftId), providerType: providerType);

                    if (nftRes != null && !nftRes.IsError && nftRes.Result != null)
                        result.Result.Add(nftRes.Result);
                    else
                        OASISErrorHandling.HandleError(ref result, $"Error occured in LoadChildWeb4GeoNFTsForNFTCollection loading child nft for id {nftId}. Reason: {nftRes.Message}");
                }
            }

            if (result.ErrorCount > 0)
                result.Message = $"Error(s) occured in LoadChildWeb4GeoNFTsForNFTCollection loading child nfts. Reason(s): {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";

            return result;
        }

        public async Task<OASISResult<IWeb4NFTCollection>> LoadWeb4NFTCollectionAsync(Guid id, bool loadChildNFTs = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4NFTCollection> result = new();
            string errorMessage = "Error occured in LoadNFTCollectionAsync in NFTManager. Reason:";

            try
            {
                OASISResult<Web4NFTCollection> holonRes = await Data.LoadHolonAsync<Web4NFTCollection>(id, providerType: providerType);

                if (holonRes != null && !holonRes.IsError && holonRes.Result != null)
                {
                    if (loadChildNFTs && holonRes.Result.Web4NFTIds != null && holonRes.Result.Web4NFTIds.Count > 0)
                    {
                        OASISResult<IList<IWeb4NFT>> childrenResult = await LoadChildWeb4NFTsForNFTCollectionAsync(holonRes.Result.Web4NFTIds, providerType);

                        if (childrenResult != null && childrenResult.Result != null && !childrenResult.IsError)
                            holonRes.Result.Web4NFTs = childrenResult.Result.ToList();
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading child nfts, reason: {childrenResult.Message}");
                    }

                    result.Result = holonRes.Result;
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading collection. Reason: {holonRes?.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IWeb4GeoNFTCollection>> LoadWeb4GeoNFTCollectionAsync(Guid id, bool loadChildGeoNFTs = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4GeoNFTCollection> result = new();
            string errorMessage = "Error occured in LoadGeoNFTCollectionAsync in NFTManager. Reason:";

            try
            {
                OASISResult<Web4GeoNFTCollection> holonRes = await Data.LoadHolonAsync<Web4GeoNFTCollection>(id, providerType: providerType);

                if (holonRes != null && !holonRes.IsError && holonRes.Result != null)
                {
                    if (loadChildGeoNFTs && holonRes.Result.Web4GeoNFTIds != null && holonRes.Result.Web4GeoNFTIds.Count > 0)
                    {
                        OASISResult<IList<IWeb4GeoSpatialNFT>> childrenResult = await LoadChildWeb4GeoNFTsForNFTCollectionAsync(holonRes.Result.Web4GeoNFTIds, providerType);

                        if (childrenResult != null && childrenResult.Result != null && !childrenResult.IsError)
                            holonRes.Result.Web4GeoNFTs = childrenResult.Result.ToList();
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading child nfts, reason: {childrenResult.Message}");
                    }

                    result.Result = holonRes.Result;
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading collection. Reason: {holonRes?.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IWeb4NFTCollection>>> LoadAllWeb4NFTCollectionsAsync(bool loadChildNFTs = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb4NFTCollection>> result = new();
            string errorMessage = "Error occured in LoadAllWeb4NFTCollectionsAsync in NFTManager. Reason:";

            try
            {
                OASISResult<IEnumerable<Web4NFTCollection>> holonRes = await Data.LoadAllHolonsAsync<Web4NFTCollection>(HolonType.Web4NFTCollection, providerType: providerType);

                if (holonRes != null && !holonRes.IsError && holonRes.Result != null)
                {
                    if (loadChildNFTs)
                    {
                        foreach (IWeb4NFTCollection collection in holonRes.Result)
                        {
                            OASISResult<IList<IWeb4NFT>> childrenResult = await LoadChildWeb4NFTsForNFTCollectionAsync(collection.Web4NFTIds, providerType);

                            if (childrenResult != null && childrenResult.Result != null && !childrenResult.IsError)
                                collection.Web4NFTs = childrenResult.Result.ToList();
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading child nfts, reason: {childrenResult.Message}");
                        }
                    }
                    result.Result = holonRes.Result;
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading collections. Reason: {holonRes?.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IWeb4NFTCollection>>> LoadWeb4NFTCollectionsForAvatarAsync(Guid avatarId, bool loadChildNFTs = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb4NFTCollection>> result = new();
            string errorMessage = "Error occured in LoadWeb4NFTCollectionsForAvatarAsync in NFTManager. Reason:";

            try
            {
                OASISResult<IEnumerable<Web4NFTCollection>> holonRes = await Data.LoadHolonsForParentAsync<Web4NFTCollection>(avatarId, HolonType.Web4NFTCollection, providerType: providerType);

                if (holonRes != null && !holonRes.IsError && holonRes.Result != null)
                {
                    if (loadChildNFTs)
                    {
                        foreach (IWeb4NFTCollection collection in holonRes.Result)
                        {
                            OASISResult<IList<IWeb4NFT>> childrenResult = await LoadChildWeb4NFTsForNFTCollectionAsync(collection.Web4NFTIds, providerType);

                            if (childrenResult != null && childrenResult.Result != null && !childrenResult.IsError)
                                collection.Web4NFTs = childrenResult.Result.ToList();
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading child nfts, reason: {childrenResult.Message}");
                        }
                    }
                    result.Result = holonRes.Result;
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading collections. Reason: {holonRes?.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IWeb4GeoNFTCollection>>> LoadAllWeb4GeoNFTCollectionsAsync(bool loadChildNFTs = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb4GeoNFTCollection>> result = new();
            string errorMessage = "Error occured in LoadAllWeb4GeoNFTCollectionsAsync in NFTManager. Reason:";

            try
            {
                OASISResult<IEnumerable<Web4GeoNFTCollection>> holonRes = await Data.LoadAllHolonsAsync<Web4GeoNFTCollection>(HolonType.Web4GeoNFTCollection, providerType: providerType);

                if (holonRes != null && !holonRes.IsError && holonRes.Result != null)
                {
                    if (loadChildNFTs)
                    {
                        foreach (IWeb4GeoNFTCollection collection in holonRes.Result)
                        {
                            OASISResult<IList<IWeb4GeoSpatialNFT>> childrenResult = await LoadChildWeb4GeoNFTsForNFTCollectionAsync(collection.Web4GeoNFTIds, providerType);

                            if (childrenResult != null && childrenResult.Result != null && !childrenResult.IsError)
                                collection.Web4GeoNFTs = childrenResult.Result.ToList();
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading child nfts, reason: {childrenResult.Message}");
                        }
                    }
                    result.Result = holonRes.Result;
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading collections. Reason: {holonRes?.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IWeb4GeoNFTCollection>>> LoadWeb4GeoNFTCollectionsForAvatarAsync(Guid avatarId, bool loadChildNFTs = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb4GeoNFTCollection>> result = new();
            string errorMessage = "Error occured in LoadWeb4GeoNFTCollectionsForAvatarAsync in NFTManager. Reason:";

            try
            {
                OASISResult<IEnumerable<Web4GeoNFTCollection>> holonRes = await Data.LoadHolonsForParentAsync<Web4GeoNFTCollection>(avatarId, HolonType.Web4GeoNFTCollection, providerType: providerType);

                if (holonRes != null && !holonRes.IsError && holonRes.Result != null)
                {
                    if (loadChildNFTs)
                    {
                        foreach (IWeb4GeoNFTCollection collection in holonRes.Result)
                        {
                            OASISResult<IList<IWeb4GeoSpatialNFT>> childrenResult = await LoadChildWeb4GeoNFTsForNFTCollectionAsync(collection.Web4GeoNFTIds, providerType);

                            if (childrenResult != null && childrenResult.Result != null && !childrenResult.IsError)
                                collection.Web4GeoNFTs = childrenResult.Result.ToList();
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading child nfts, reason: {childrenResult.Message}");
                        }
                    }
                    result.Result = holonRes.Result;
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading collections. Reason: {holonRes?.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }


        private IMintWeb4NFTRequest CloneWeb4NFTRequest(IMintWeb4NFTRequest request)
        {
            return new MintWeb4NFTRequest()
            {
                CollectionPublicKey = request.CollectionPublicKey,
                AttemptToMintEveryXSeconds = request.AttemptToMintEveryXSeconds,
                AttemptToSendEveryXSeconds = request.AttemptToSendEveryXSeconds,
                Description = request.Description,
                Discount = request.Discount,
                Image = request.Image,
                ImageUrl = request.ImageUrl,
                IsForSale = request.IsForSale,
                JSONMetaData = request.JSONMetaData,
                MintedByAvatarId = request.MintedByAvatarId,
                Title = request.Title,
                MemoText = request.MemoText,
                Price = request.Price,
                RoyaltyPercentage = request.RoyaltyPercentage,
                NumberToMint = request.NumberToMint,
                SaleStartDate = request.SaleStartDate,
                SaleEndDate = request.SaleEndDate,
                OnChainProvider = request.OnChainProvider,
                OffChainProvider = request.OffChainProvider,
                StoreNFTMetaDataOnChain = request.StoreNFTMetaDataOnChain,
                NFTOffChainMetaType = request.NFTOffChainMetaType,
                NFTStandardType = request.NFTStandardType,
                Thumbnail = request.Thumbnail,
                ThumbnailUrl = request.ThumbnailUrl,
                JSONMetaDataURL = request.JSONMetaDataURL,
                Tags = request.Tags != null ? new List<string>(request.Tags) : null,
                MetaData = request.MetaData != null ? new Dictionary<string, string>(request.MetaData) : null,
                Symbol = request.Symbol,
                SendToAddressAfterMinting = request.SendToAddressAfterMinting,
                SendToAvatarAfterMintingId = request.SendToAvatarAfterMintingId,
                SendToAvatarAfterMintingUsername = request.SendToAvatarAfterMintingUsername,
                SendToAvatarAfterMintingEmail = request.SendToAvatarAfterMintingEmail,
                WaitForNFTToMintInSeconds = request.WaitForNFTToMintInSeconds,
                WaitTillNFTMinted = request.WaitTillNFTMinted,
                WaitForNFTToSendInSeconds = request.WaitForNFTToSendInSeconds,
                WaitTillNFTSent = request.WaitTillNFTSent,
                // DISABLED: see SolanaService.cs — RevokeTokenAuthorities is a no-op on Metaplex NFTs.
                // RevokeTokenAuthorities = request.RevokeTokenAuthorities,
                FreezeMetadata = request.FreezeMetadata,
                Web3NFTs = request.Web3NFTs != null ? request.Web3NFTs : new List<IMintWeb3NFTRequest>()
            };
        }

        private async Task<OASISResult<IWeb4NFT>> MintWeb3NFTsAsync(OASISResult<IWeb4NFT> result, IMintWeb4NFTRequest request, IMintWeb3NFTRequest web3Request = null, IWeb4NFT existingWeb4NFT = null, bool isGeoNFT = false, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText, bool isLastWeb3NFT = false)
        {
            IMintWeb4NFTRequest originalWeb4Request = CloneWeb4NFTRequest(request);

            //Web3 Request overrides web4 (optional).
            if (web3Request != null)
            {
                if (!string.IsNullOrEmpty(web3Request.CollectionPublicKey))
                    request.CollectionPublicKey = web3Request.CollectionPublicKey;

                if (!string.IsNullOrEmpty(web3Request.Title))
                    request.Title = web3Request.Title;

                if (!string.IsNullOrEmpty(web3Request.Description))
                    request.Description = web3Request.Description;

                if (web3Request.Image != null)
                    request.Image = web3Request.Image;

                if (!string.IsNullOrEmpty(web3Request.ImageUrl))
                    request.ImageUrl = web3Request.ImageUrl;

                if (web3Request.Thumbnail != null)
                    request.Thumbnail = web3Request.Thumbnail;

                if (!string.IsNullOrEmpty(web3Request.ThumbnailUrl))
                    request.ThumbnailUrl = web3Request.ThumbnailUrl;

                if (web3Request.Discount.HasValue)
                    request.Discount = web3Request.Discount.Value;

                if (web3Request.Price.HasValue)
                    request.Price = web3Request.Price.Value;

                if (web3Request.RoyaltyPercentage.HasValue)
                    request.RoyaltyPercentage = web3Request.RoyaltyPercentage.Value;

                if (web3Request.IsForSale.HasValue)
                    request.IsForSale = web3Request.IsForSale.Value;

                if (web3Request.SaleStartDate.HasValue)
                    request.SaleStartDate = web3Request.SaleStartDate.Value;

                if (web3Request.SaleEndDate.HasValue)
                    request.SaleEndDate = web3Request.SaleEndDate.Value;

                if (!string.IsNullOrEmpty(web3Request.Symbol))
                    request.Symbol = web3Request.Symbol;

                if (!string.IsNullOrEmpty(web3Request.JSONMetaData))
                    request.JSONMetaData = web3Request.JSONMetaData;

                if (!string.IsNullOrEmpty(web3Request.JSONMetaDataURL))
                    request.JSONMetaDataURL = web3Request.JSONMetaDataURL;

                if (web3Request.NumberToMint.HasValue)
                    request.NumberToMint = web3Request.NumberToMint.Value;

                if (web3Request.NFTOffChainMetaType.HasValue)
                    request.NFTOffChainMetaType = new EnumValue<NFTOffChainMetaType>(web3Request.NFTOffChainMetaType.Value);

                if (web3Request.NFTStandardType.HasValue)
                    request.NFTStandardType = new EnumValue<NFTStandardType>(web3Request.NFTStandardType.Value);

                if (web3Request.OnChainProvider.HasValue)
                    request.OnChainProvider = new EnumValue<ProviderType>(web3Request.OnChainProvider.Value);

                if (web3Request.OffChainProvider.HasValue)
                    request.OffChainProvider = new EnumValue<ProviderType>(web3Request.OffChainProvider.Value);

                if (web3Request.StoreNFTMetaDataOnChain.HasValue)
                    request.StoreNFTMetaDataOnChain = web3Request.StoreNFTMetaDataOnChain.Value;

                if (!string.IsNullOrEmpty(web3Request.SendToAddressAfterMinting))
                    request.SendToAddressAfterMinting = web3Request.SendToAddressAfterMinting;

                if (web3Request.SendToAvatarAfterMintingId != Guid.Empty)
                    request.SendToAvatarAfterMintingId = web3Request.SendToAvatarAfterMintingId;

                if (!string.IsNullOrEmpty(web3Request.SendToAvatarAfterMintingUsername))
                    request.SendToAvatarAfterMintingUsername = web3Request.SendToAvatarAfterMintingUsername;

                if (!string.IsNullOrEmpty(web3Request.SendToAvatarAfterMintingEmail))
                    request.SendToAvatarAfterMintingEmail = web3Request.SendToAvatarAfterMintingEmail;

                if (web3Request.AttemptToMintEveryXSeconds.HasValue)
                    request.AttemptToMintEveryXSeconds = web3Request.AttemptToMintEveryXSeconds.Value;

                if (web3Request.WaitForNFTToMintInSeconds.HasValue)
                    request.AttemptToSendEveryXSeconds = web3Request.AttemptToSendEveryXSeconds.Value;

                if (web3Request.WaitTillNFTMinted.HasValue)
                    request.WaitForNFTToMintInSeconds = web3Request.WaitForNFTToMintInSeconds.Value;

                if (web3Request.WaitTillNFTSent.HasValue)
                    request.WaitForNFTToSendInSeconds = web3Request.WaitForNFTToSendInSeconds.Value;

                if (web3Request.NFTTagsMergeStrategy == NFTTagsMergeStrategy.Replace)
                    request.Tags.Clear();

                if (web3Request.Tags != null)
                {
                    if (request.Tags == null)
                        request.Tags = new List<string>();

                    foreach (string tag in web3Request.Tags)
                    {
                        if (request.Tags.Contains(tag))
                            continue;

                        request.Tags.Add(tag);
                    }
                }

                //Add web3 metadata to web4 (if any keys already exist then web3 overrides web4).
                if (web3Request.NFTMetaDataMergeStrategy == NFTMetaDataMergeStrategy.Replace)
                    request.MetaData.Clear();

                if (web3Request.MetaData != null)
                {
                    if (request.MetaData == null)
                        request.MetaData = new Dictionary<string, string>();

                    foreach (string key in web3Request.MetaData.Keys)
                    {
                        if (request.MetaData.ContainsKey(key) && web3Request.NFTMetaDataMergeStrategy == NFTMetaDataMergeStrategy.Merge)
                            continue;

                        request.MetaData[key] = web3Request.MetaData[key];
                    }
                }
            }

            OASISResult<bool> validateResult = await ValidateNFTRequest(request);

            if (validateResult != null && validateResult.Result && !validateResult.IsError)
            {
                if (request.OffChainProvider == null)
                    request.OffChainProvider = new EnumValue<ProviderType>(ProviderType.MongoDBOASIS);

                if (web3Request == null)
                    web3Request = new MintWeb3NFTRequest();

                OASISResult<IOASISNFTProvider> nftProviderResult = GetNFTProvider(request.OnChainProvider.Value);

                if (nftProviderResult != null && nftProviderResult.Result != null && !nftProviderResult.IsError)
                {
                    string geoNFTMemoText = "";

                    if (isGeoNFT)
                        geoNFTMemoText = "Geo";

                    request.MemoText = $"{request.OnChainProvider.Name} {geoNFTMemoText}NFT minted on The OASIS with title '{request.Title}' by avatar with id {request.MintedByAvatarId} for the price of {request.Price}. {request.MemoText}";

                    EnumValue<ProviderType> NFTMetaDataProviderType;
                    //request.OffChainProvider = new EnumValue<ProviderType>(ProviderType.None); //TODO: Not sure why it was defaulting to None?! lol

                    if (request.StoreNFTMetaDataOnChain)
                        NFTMetaDataProviderType = request.OnChainProvider;
                    else
                        NFTMetaDataProviderType = request.OffChainProvider;

                    if (string.IsNullOrEmpty(request.Symbol))
                    {
                        if (isGeoNFT)
                            request.Symbol = "GEONFT";
                        else
                            request.Symbol = "OASISNFT";
                    }

                    //Sync web3Request with web4.
                    web3Request.CollectionPublicKey = request.CollectionPublicKey;
                    web3Request.AttemptToMintEveryXSeconds = request.AttemptToMintEveryXSeconds;
                    web3Request.AttemptToSendEveryXSeconds = request.AttemptToSendEveryXSeconds;
                    web3Request.Description = request.Description;
                    web3Request.Discount = request.Discount;
                    web3Request.JSONMetaDataURL = request.JSONMetaDataURL;
                    web3Request.JSONMetaData = request.JSONMetaData;
                    web3Request.MetaData = request.MetaData;
                    web3Request.SaleStartDate = request.SaleStartDate;
                    web3Request.SaleEndDate = request.SaleEndDate;
                    web3Request.Image = request.Image;
                    web3Request.ImageUrl = request.ImageUrl;
                    web3Request.IsForSale = request.IsForSale;
                    web3Request.MemoText = request.MemoText;
                    web3Request.MintedByAvatarId = request.MintedByAvatarId;
                    web3Request.NFTOffChainMetaType = request.NFTOffChainMetaType.Value;
                    web3Request.NFTStandardType = request.NFTStandardType.Value;
                    web3Request.OffChainProvider = request.OffChainProvider.Value;
                    web3Request.OnChainProvider = request.OnChainProvider.Value;
                    web3Request.Price = request.Price;
                    web3Request.RoyaltyPercentage = request.RoyaltyPercentage;
                    web3Request.SendToAddressAfterMinting = request.SendToAddressAfterMinting;
                    web3Request.SendToAvatarAfterMintingEmail = request.SendToAvatarAfterMintingEmail;
                    web3Request.SendToAvatarAfterMintingId = request.SendToAvatarAfterMintingId;
                    web3Request.SendToAvatarAfterMintingUsername = request.SendToAvatarAfterMintingUsername;
                    web3Request.StoreNFTMetaDataOnChain = request.StoreNFTMetaDataOnChain;
                    web3Request.Symbol = request.Symbol;
                    web3Request.Tags = request.Tags;
                    web3Request.Thumbnail = request.Thumbnail;
                    web3Request.ThumbnailUrl = request.ThumbnailUrl;
                    web3Request.Title = request.Title;
                    web3Request.WaitForNFTToMintInSeconds = request.WaitForNFTToMintInSeconds;
                    web3Request.WaitForNFTToSendInSeconds = request.WaitForNFTToSendInSeconds;
                    web3Request.WaitTillNFTMinted = request.WaitTillNFTMinted;
                    web3Request.WaitTillNFTSent = request.WaitTillNFTSent;
                    // DISABLED: see SolanaService.cs — RevokeTokenAuthorities is a no-op on Metaplex NFTs.
                    // web3Request.RevokeTokenAuthorities = request.RevokeTokenAuthorities;
                    web3Request.FreezeMetadata = request.FreezeMetadata;

                    result = await MintNFTInternalAsync(result, originalWeb4Request, web3Request, request, NFTMetaDataProviderType, nftProviderResult, existingWeb4NFT, isGeoNFT, responseFormatType, isLastWeb3NFT);
                }
                else
                {
                    OASISErrorHandling.HandleWarning(ref result, $"Error occured minting web3 NFT in MintWeb3NFTsAsync. Error occured calling GetNFTProvider. Reason: {nftProviderResult.Message}");
                    //result.Result = null;
                    //result.Message = nftProviderResult.Message;
                    //result.IsError = true;
                }
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured validating the NFT Request. Reason: {validateResult.Message}");

            return result;
        }

    }
}
