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
        public async Task<OASISResult<IWeb4NFTCollection>> UpdateWeb4NFTCollectionAsync(IUpdateWeb4NFTCollectionRequest request, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4NFTCollection> result = new();
            string errorMessage = "Error occured in UpdateNFCollectionAsync in NFTManager. Reason:";

            try
            {
                if (request == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Request is null");
                    return result;
                }

                OASISResult<Web4NFTCollection> holonResult = await Data.LoadHolonAsync<Web4NFTCollection>(request.Id, providerType: providerType);

                if (holonResult != null && holonResult.Result != null && !holonResult.IsError)
                {
                    holonResult.Result.Name = !string.IsNullOrEmpty(request.Title) ? request.Title : holonResult.Result.Name;
                    holonResult.Result.Description = !string.IsNullOrEmpty(request.Description) ? request.Description : holonResult.Result.Description;
                    holonResult.Result.ModifiedByAvatarId = request.ModifiedBy != Guid.Empty ? request.ModifiedBy : holonResult.Result.ModifiedByAvatarId;
                    holonResult.Result.ModifiedDate = DateTime.Now;
                    holonResult.Result.ImageUrl = !string.IsNullOrEmpty(request.ImageUrl) ? request.ImageUrl : holonResult.Result.ImageUrl;
                    holonResult.Result.Image = request.Image != null ? request.Image : holonResult.Result.Image;
                    holonResult.Result.ThumbnailUrl = !string.IsNullOrEmpty(request.ThumbnailUrl) ? request.ThumbnailUrl : holonResult.Result.ThumbnailUrl;
                    holonResult.Result.Thumbnail = request.Thumbnail != null ? request.Thumbnail : holonResult.Result.Thumbnail;
                    
                    
                    
                    //holonResult.Result.MetaData = request.MetaData != null ? request.MetaData : holonResult.Result.MetaData;
                    //holonResult.Result.Web4NFTIds = request.Web4NFTIds ?? holonResult.Result.Web4NFTIds;
                    holonResult.Result.Tags = request.Tags ?? holonResult.Result.Tags;

                    OASISResult<Web4NFTCollection> saveResult = await Data.SaveHolonAsync<Web4NFTCollection>(holonResult.Result);

                    if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
                    {
                        result.Result = saveResult.Result;
                        result.Message = "OASIS NFT Collection Updated Successfully.";
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving OASIS NFT Collection holon. Reason: {saveResult?.Message}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading OASIS NFT Collection holon. Reason: {holonResult?.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IWeb4GeoNFTCollection>> UpdateWeb4GeoNFTCollectionAsync(IUpdateWeb4GeoNFTCollectionRequest request, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4GeoNFTCollection> result = new();
            string errorMessage = "Error occured in UpdateGeoNFTCollectionAsync in NFTManager. Reason:";

            try
            {
                if (request == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Request is null");
                    return result;
                }

                OASISResult<Web4GeoNFTCollection> holonResult = await Data.LoadHolonAsync<Web4GeoNFTCollection>(request.Id, providerType: providerType);

                if (holonResult != null && holonResult.Result != null && !holonResult.IsError)
                {
                    //Dictionary<string, object> metaData = new Dictionary<string, object>()
                    //{
                    //    { "OASISGEONFTCOLLECTION.ID", request.Id == Guid.Empty ? holonResult.Result.Id : request.Id },
                    //    //{ "OASISGEONFTCOLLECTION.Title", !string.IsNullOrEmpty(request.Title) ? request.Title : holonResult.Result.MetaData != null && holonResult.Result.MetaData.ContainsKey("Title") ? holonResult.Result.MetaData["Title"] : "" },
                    //    { "OASISGEONFTCOLLECTION.Title", !string.IsNullOrEmpty(request.Title) ? request.Title : holonResult.Result.Name },
                    //    { "OASISGEONFTCOLLECTION.Description",  !string.IsNullOrEmpty(request.Description) ? request.Description : holonResult.Result.Description },
                    //    { "OASISGEONFTCOLLECTION.ModifiedBy", request.ModifiedBy != Guid.Empty ? request.ModifiedBy : holonResult.Result.ModifiedByAvatarId },
                    //    { "OASISGEONFTCOLLECTION.ModifiedOn", DateTime.Now },
                    //    { "OASISGEONFTCOLLECTION.ImageUrl", !string.IsNullOrEmpty(request.ImageUrl) ? request.ImageUrl : holonResult.Result.ImageUrl },
                    //    { "OASISGEONFTCOLLECTION.Image", request.Image },
                    //    { "OASISGEONFTCOLLECTION.ThumbnailUrl", request.ThumbnailUrl },
                    //    { "OASISGEONFTCOLLECTION.Thumbnail", request.Thumbnail },
                    //    { "OASISGEONFTCOLLECTION.Web4GeoNFTIds", request.Web4GeoNFTIds ?? new List<string>() },
                    //    { "OASISGEONFTCOLLECTION.Tags", request.Tags },
                    //    { "OASISGEONFTCOLLECTION.MetaData", request.MetaData }
                    //};

                    //holonResult.Result.MetaData = metaData;

                    holonResult.Result.Name = !string.IsNullOrEmpty(request.Title) ? request.Title : holonResult.Result.Name;
                    holonResult.Result.Description = !string.IsNullOrEmpty(request.Description) ? request.Description : holonResult.Result.Description;
                    holonResult.Result.ModifiedByAvatarId = request.ModifiedBy != Guid.Empty ? request.ModifiedBy : holonResult.Result.ModifiedByAvatarId;
                    holonResult.Result.ModifiedDate = DateTime.Now;
                    holonResult.Result.ImageUrl = !string.IsNullOrEmpty(request.ImageUrl) ? request.ImageUrl : holonResult.Result.ImageUrl;
                    holonResult.Result.Image = request.Image != null ? request.Image : holonResult.Result.Image;
                    holonResult.Result.ThumbnailUrl = !string.IsNullOrEmpty(request.ThumbnailUrl) ? request.ThumbnailUrl : holonResult.Result.ThumbnailUrl;
                    holonResult.Result.Thumbnail = request.Thumbnail != null ? request.Thumbnail : holonResult.Result.Thumbnail;
                    
                    if (request.MetaData != null)
                    {
                        if (holonResult.Result.MetaData == null)
                            holonResult.Result.MetaData = new Dictionary<string, string>();

                        foreach (var kvp in request.MetaData)
                        {
                            holonResult.Result.MetaData[kvp.Key] = kvp.Value;
                        }
                    }

                    //holonResult.Result.MetaData = request.MetaData != null ? request.MetaData : holonResult.Result.MetaData;
                    // holonResult.Result.Web4GeoNFTIds = request.Web4GeoNFTIds ?? holonResult.Result.Web4GeoNFTIds;
                    holonResult.Result.Tags = request.Tags ?? holonResult.Result.Tags;

                    OASISResult<Web4GeoNFTCollection> saveResult = await Data.SaveHolonAsync<Web4GeoNFTCollection>(holonResult.Result);

                    if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
                    {
                        //IWeb4OASISGeoNFTCollection coll = new Web4OASISGeoNFTCollection()
                        //{
                        //    Id = holonResult.Result.Id,
                        //    Name = request.Title,
                        //    Description = request.Description,
                        //    CreatedDate = holonResult.Result.CreatedDate,
                        //    CreatedByAvatarId = holonResult.Result.CreatedByAvatarId,
                        //    ModifiedDate = holonResult.Result.ModifiedDate,
                        //    ModifiedByAvatarId = holonResult.Result.ModifiedByAvatarId,
                        //    Image = request.Image,
                        //    ImageUrl = request.ImageUrl,
                        //    Thumbnail = request.Thumbnail,
                        //    ThumbnailUrl = request.ThumbnailUrl,
                        //    MetaData = request.MetaData,
                        //    Web4GeoNFTIds = request.Web4GeoNFTIds ?? new List<string>(),
                        //    Web4GeoNFTs = request.Web4GeoNFTs,
                        //    Tags = request.Tags
                        //};

                        //result.Result = coll;

                        result.Result = saveResult.Result;
                        result.Message = "OASIS GeoNFT Collection Updated Successfully.";
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving OASIS GeoNFT Collection holon. Reason: {saveResult?.Message}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading OASIS GeoNFT Collection holon. Reason: {holonResult?.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IWeb4NFTCollection>> AddWeb4NFTToCollectionAsync(Guid collectionId, Guid OASISNFTId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4NFTCollection> result = new();
            string errorMessage = "Error occured in AddNFTToCollectionAsync in NFTManager. Reason:";

            try
            {
                OASISResult<Web4NFTCollection> holonResult = await Data.LoadHolonAsync<Web4NFTCollection>(collectionId, providerType: providerType);

                if (holonResult != null && holonResult.Result != null && !holonResult.IsError)
                {
                    if (!holonResult.Result.Web4NFTIds.Contains(OASISNFTId.ToString()))
                    {
                        holonResult.Result.Web4NFTIds.Add(OASISNFTId.ToString());

                        OASISResult<Web4NFTCollection> saveResult = await Data.SaveHolonAsync<Web4NFTCollection>(holonResult.Result);

                        if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
                        {
                            result.Result = saveResult.Result;
                            result.Message = "OASIS NFT Added To Collection Successfully.";
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured adding OASIS NFT to collection. Reason: {saveResult?.Message}");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured adding OASIS NFT to collection. Reason: NFT already added!");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading OASIS NFT Collection. Reason: {holonResult?.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }
        public async Task<OASISResult<IWeb4NFTCollection>> AddWeb4NFTToCollectionAsync(Guid collectionId, IWeb4NFT OASISNFT, ProviderType providerType = ProviderType.Default)
        {
            return await AddWeb4NFTToCollectionAsync(collectionId, OASISNFT.Id, providerType);
        }

        public async Task<OASISResult<IWeb4NFTCollection>> RemoveWeb4NFTFromCollectionAsync(Guid collectionId, Guid OASISNFTId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4NFTCollection> result = new();
            string errorMessage = "Error occured in RemoveNFTFromCollectionAsync in NFTManager. Reason:";

            try
            {
                OASISResult<Web4NFTCollection> holonResult = await Data.LoadHolonAsync<Web4NFTCollection>(collectionId, providerType: providerType);

                if (holonResult != null && holonResult.Result != null && !holonResult.IsError)
                {
                    if (holonResult.Result.Web4NFTIds.Contains(OASISNFTId.ToString()))
                    {
                        holonResult.Result.Web4NFTIds.Remove(OASISNFTId.ToString());

                        OASISResult<Web4NFTCollection> saveResult = await Data.SaveHolonAsync<Web4NFTCollection>(holonResult.Result);

                        if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
                        {
                            result.Result = saveResult.Result;
                            result.Message = "OASIS NFT Removed From Collection Successfully.";
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured removing OASIS NFT from collection. Reason: {saveResult?.Message}");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured removing OASIS NFT from collection. Reason: NFT Not Found!");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading OASIS NFT Collection. Reason: {holonResult?.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IWeb4NFTCollection>> RemoveWeb4NFTFromCollectionAsync(Guid collectionId, IWeb4NFT OASISNFT, ProviderType providerType = ProviderType.Default)
        {
            return await RemoveWeb4NFTFromCollectionAsync(collectionId, OASISNFT.Id, providerType);
        }

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

    }
}
