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
        public async Task<OASISResult<IWeb4GeoNFTCollection>> AddWeb4GeoNFTToCollectionAsync(Guid collectionId, Guid OASISGeoNFTId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4GeoNFTCollection> result = new();
            string errorMessage = "Error occured in AddOASISGeoNFTToCollectionAsync in NFTManager. Reason:";

            try
            {
                OASISResult<Web4GeoNFTCollection> holonResult = await Data.LoadHolonAsync<Web4GeoNFTCollection>(collectionId, providerType: providerType);

                if (holonResult != null && holonResult.Result != null && !holonResult.IsError)
                {
                    if (!holonResult.Result.Web4GeoNFTIds.Contains(OASISGeoNFTId.ToString()))
                    {
                        holonResult.Result.Web4GeoNFTIds.Add(OASISGeoNFTId.ToString());

                        OASISResult<Web4GeoNFTCollection> saveResult = await Data.SaveHolonAsync<Web4GeoNFTCollection>(holonResult.Result);

                        if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
                        {
                            result.Result = saveResult.Result;
                            result.Message = "OASIS GeoNFT Added To Collection Successfully.";
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured adding OASIS GeoNFT to collection. Reason: {saveResult?.Message}");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured adding OASIS GeoNFT to collection. Reason: GeoNFT Already Added!");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading OASIS GeoNFT Collection. Reason: {holonResult?.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IWeb4GeoNFTCollection>> AddWeb4GeoNFTToCollectionAsync(Guid collectionId, IWeb4GeoSpatialNFT OASISGeoNFT, ProviderType providerType = ProviderType.Default)
        {
            return await AddWeb4GeoNFTToCollectionAsync(collectionId, OASISGeoNFT.Id, providerType);
        }

        public async Task<OASISResult<IWeb4GeoNFTCollection>> RemoveWeb4GeoNFTFromCollectionAsync(Guid collectionId, Guid OASISGeoNFTId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4GeoNFTCollection> result = new();
            string errorMessage = "Error occured in RemoveGeoNFTFromCollectionAsync in NFTManager. Reason:";

            try
            {
                OASISResult<Web4GeoNFTCollection> holonResult = await Data.LoadHolonAsync<Web4GeoNFTCollection>(collectionId, providerType: providerType);

                if (holonResult != null && holonResult.Result != null && !holonResult.IsError)
                {
                    if (holonResult.Result.Web4GeoNFTIds.Contains(OASISGeoNFTId.ToString()))
                    {
                        holonResult.Result.Web4GeoNFTIds.Remove(OASISGeoNFTId.ToString());

                        OASISResult<Web4GeoNFTCollection> saveResult = await Data.SaveHolonAsync<Web4GeoNFTCollection>(holonResult.Result);

                        if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
                        {
                            result.Result = saveResult.Result;
                            result.Message = "OASIS GeoNFT Removed From Collection Successfully.";
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured removing OASIS GeoNFT from collection. Reason: {saveResult?.Message}");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured removing OASIS GeoNFT from collection. Reason: GeoNFT Not Found!");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading OASIS GeoNFT Collection. Reason: {holonResult?.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IWeb4GeoNFTCollection>> RemoveWeb4GeoNFTFromCollectionAsync(Guid collectionId, IWeb4GeoSpatialNFT OASISGeoNFT, ProviderType providerType = ProviderType.Default)
        {
            return await RemoveWeb4GeoNFTFromCollectionAsync(collectionId, OASISGeoNFT.Id, providerType);
        }

        public async Task<OASISResult<bool>> DeleteWeb4NFTCollectionAsync(Guid avatarId, Guid id, bool softDelete = true, bool deleteChildWeb4NFTs = false, bool deleteChildWeb3NFTs = false, bool burnChildWebNFTs = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new();
            string errorMessage = "Error occured in DeleteWeb4NFTCollectionAsync in NFTManager. Reason:";

            try
            {
                if (deleteChildWeb4NFTs)
                {
                    OASISResult<IWeb4NFTCollection> loadCollectionResult = await LoadWeb4NFTCollectionAsync(id, loadChildNFTs: false, providerType: providerType);

                    if (loadCollectionResult != null && loadCollectionResult.Result != null && !loadCollectionResult.IsError)
                    {
                        foreach (string web4NFTId in loadCollectionResult.Result.Web4NFTIds)
                        {
                            OASISResult<bool> deleteWeb4NFTResult = await DeleteWeb4NFTAsync(avatarId, new Guid(web4NFTId), softDelete, deleteChildWeb3NFTs, burnChildWebNFTs);

                            if (!(deleteWeb4NFTResult != null && !deleteWeb4NFTResult.IsError && deleteWeb4NFTResult.Result != null))
                                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured deleting Web4 NFT with id {web4NFTId}. Reason: {deleteWeb4NFTResult?.Message}");
                        }
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the collection. Reason: {loadCollectionResult?.Message}");
                }

                OASISResult<IHolon> deleteCollectionResult = await Data.DeleteHolonAsync(id, avatarId, softDelete, providerType: providerType);

                if (deleteCollectionResult != null && !deleteCollectionResult.IsError && deleteCollectionResult.Result != null)
                {
                    result.Result = true;
                    result.IsError = false;

                    if (result.IsWarning)
                        result.Message = $"Web4 OASIS NFT Collection Successfull Deleted but one or more errors occured deleting it's child Web4 NFT's: \n\n{OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";
                    else
                        result.Message = $"Web4 OASIS NFT Collection Successfull Deleted";
                }
                else
                {
                    result.Result = false;
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured deleting collection. Reason: {deleteCollectionResult?.Message}");
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<bool>> DeleteWeb4GeoNFTCollectionAsync(Guid avatarId, Guid id, bool softDelete = true, bool deleteChildWeb4GeoNFTs = false, bool deleteChildWeb3NFTs = false, bool burnChildWebNFTs = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new();
            string errorMessage = "Error occured in DeleteWeb4GeoNFTCollectionAsync in NFTManager. Reason:";

            try
            {
                if (deleteChildWeb4GeoNFTs)
                {
                    OASISResult<IWeb4GeoNFTCollection> loadCollectionResult = await LoadWeb4GeoNFTCollectionAsync(id, loadChildGeoNFTs: false, providerType: providerType);

                    if (loadCollectionResult != null && loadCollectionResult.Result != null && !loadCollectionResult.IsError)
                    {
                        foreach (string web4NFTId in loadCollectionResult.Result.Web4GeoNFTIds)
                        {
                            OASISResult<bool> deleteWeb4NFTResult = await DeleteWeb4GeoNFTAsync(avatarId, new Guid(web4NFTId), softDelete, deleteChildWeb3NFTs, burnChildWebNFTs);

                            if (!(deleteWeb4NFTResult != null && !deleteWeb4NFTResult.IsError && deleteWeb4NFTResult.Result != null))
                                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured deleting Web4 Geo-NFT with id {web4NFTId}. Reason: {deleteWeb4NFTResult?.Message}");
                        }
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading the collection. Reason: {loadCollectionResult?.Message}");
                }

                OASISResult<IHolon> deleteCollectionResult = await Data.DeleteHolonAsync(id, avatarId, softDelete, providerType: providerType);

                if (deleteCollectionResult != null && !deleteCollectionResult.IsError && deleteCollectionResult.Result != null)
                {
                    result.Result = true;
                    result.IsError = false;

                    if (result.IsWarning)
                        result.Message = $"Web4 OASIS Geo-NFT Collection Successfull Deleted but one or more errors occured deleting it's child Web4 Geo-NFT's: \n\n{OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";
                    else
                        result.Message = $"Web4 OASIS Geo-NFT Collection Successfull Deleted";
                }
                else
                {
                    result.Result = false;
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured deleting geo-nft collection. Reason: {deleteCollectionResult?.Message}");
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IList<IWeb4NFT>>> LoadChildWeb4NFTsForNFTCollectionAsync(List<string> Web4NFTIds, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IList<IWeb4NFT>> result = new OASISResult<IList<IWeb4NFT>>();

            if (Web4NFTIds != null && Web4NFTIds.Count > 0)
            {
                result.Result = new List<IWeb4NFT>();

                foreach (string nftId in Web4NFTIds)
                {
                    OASISResult<IWeb4NFT> nftRes = await LoadWeb4NftAsync(Guid.Parse(nftId), providerType: providerType);

                    if (nftRes != null && !nftRes.IsError && nftRes.Result != null)
                        result.Result.Add(nftRes.Result);
                    else
                        OASISErrorHandling.HandleError(ref result, $"Error occured in LoadChildWeb4NFTsForNFTCollection loading child nft for id {nftId}. Reason: {nftRes.Message}");
                }
            }

            if (result.ErrorCount > 0)
                result.Message = $"Error(s) occured in LoadChildWeb4NFTsForNFTCollection loading child nfts. Reason(s): {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";

            return result;
        }

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

    }
}