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
        public OASISResult<IOASISNFTProvider> GetNFTProvider(ProviderType providerType)
        {
            OASISResult<IOASISNFTProvider> result = new OASISResult<IOASISNFTProvider>();
            IOASISProvider OASISProvider = ProviderManager.Instance.GetProvider(providerType);

            if (OASISProvider != null)
            {
                if (!OASISProvider.IsProviderActivated)
                {
                    OASISResult<bool> activateProviderResult = OASISProvider.ActivateProvider();

                    if (activateProviderResult.IsError)
                        OASISErrorHandling.HandleError(ref result, $"Error occured in GetNFTProvider. Error occured activating provider. Reason: {activateProviderResult.Message}");
                }
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in GetNFTProvider. The {Enum.GetName(typeof(ProviderType), providerType)} provider was not found.");

            if (!result.IsError)
            {
                result.Result = OASISProvider as IOASISNFTProvider;

                if (result.Result == null)
                    OASISErrorHandling.HandleError(ref result, $"Error occured in GetNFTProvider. The {Enum.GetName(typeof(ProviderType), providerType)} provider is not a valid OASISNFTProvider.");
            }

            return result;
        }


        public async Task<OASISResult<IWeb4NFT>> MintOnChainCollectionNFTAsync(IMintOnChainCollectionNFTRequest request, ProviderType providerType = ProviderType.SolanaOASIS, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            OASISResult<IWeb4NFT> result = new OASISResult<IWeb4NFT>();
            string errorMessage = "Error occured in MintOnChainCollectionNFTAsync in NFTManager. Reason:";

            if (request == null)
            {
                result.IsError = true;
                result.Message = $"{errorMessage} The request is required. Please provide a valid IMintOnChainCollectionNFTRequest.";
                return result;
            }

            if (request.NumberToMint == 0)
                request.NumberToMint = 1;

            if (request.OnChainProvider == null)
                request.OnChainProvider = new EnumValue<ProviderType>(providerType);

            result = await MintNftAsync(request, false, responseFormatType);

            if (result == null || result.IsError || result.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling MintNftAsync. Reason: {result?.Message}");
                return result;
            }

            string collectionMintAddress = result.Result.NewlyMintedWeb3NFTs?.Count > 0
                ? result.Result.NewlyMintedWeb3NFTs[0].NFTTokenAddress
                : null;

            if (string.IsNullOrWhiteSpace(collectionMintAddress))
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} MintNftAsync succeeded but no NFTTokenAddress was returned on the minted Web3 NFT — cannot call SetCollectionSizeAsync.");
                return result;
            }

            if (request.InitialSize > 0)
            {
                bool attemptingToSetSize = true;
                DateTime startTime = DateTime.Now;
                string setSizeErrorMessage = string.Empty;

                do
                {
                    try
                    {
                        OASISResult<string> setSizeResult = await SetCollectionSizeAsync(collectionMintAddress, request.InitialSize, providerType);

                        if (setSizeResult != null && !setSizeResult.IsError)
                            break;

                        setSizeErrorMessage = $"{errorMessage} SetCollectionSizeAsync failed. Reason: {setSizeResult?.Message}";
                    }
                    catch (Exception e)
                    {
                        setSizeErrorMessage = $"{errorMessage} Unknown error occured calling SetCollectionSizeAsync. Reason: {e.Message}";
                    }

                    if (!string.IsNullOrEmpty(setSizeErrorMessage))
                    {
                        OASISErrorHandling.HandleWarning(ref result, setSizeErrorMessage, onlyLogToInnerMessages: true);

                        if (!request.WaitTillCollectionSizeSet)
                            break;

                        setSizeErrorMessage = "";
                    }

                    Thread.Sleep(request.AttemptToSetCollectionSizeEveryXSeconds * 1000);

                    if (startTime.AddSeconds(request.WaitForCollectionSizeToBeSetInSeconds).Ticks < DateTime.Now.Ticks)
                    {
                        setSizeErrorMessage = $"Timeout expired, WaitForCollectionSizeToBeSetInSeconds ({request.WaitForCollectionSizeToBeSetInSeconds}) exceeded. NFT was minted successfully but collection size could not be set. Try calling SetCollectionSize separately.";
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} {setSizeErrorMessage}", onlyLogToInnerMessages: true);
                        break;
                    }

                } while (attemptingToSetSize);
            }

            return result;
        }

        public async Task<OASISResult<string>> SetCollectionSizeAsync(string collectionMintAddress, ulong size, ProviderType providerType = ProviderType.SolanaOASIS)
        {
            OASISResult<string> result = new OASISResult<string>();

            if (string.IsNullOrWhiteSpace(collectionMintAddress))
            {
                result.IsError = true;
                result.Message = "collectionMintAddress is required.";
                return result;
            }

            OASISResult<IOASISNFTProvider> nftProviderResult = GetNFTProvider(providerType);

            if (nftProviderResult == null || nftProviderResult.IsError || nftProviderResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in SetCollectionSizeAsync in NFTManager. Error occured calling GetNFTProvider. Reason: {nftProviderResult?.Message}");
                return result;
            }

            return await nftProviderResult.Result.SetCollectionSizeAsync(collectionMintAddress, size);
        }

        public async Task<OASISResult<IWeb4NFTCollection>> CreateWeb4NFTCollectionAsync(ICreateWeb4NFTCollectionRequest createOASISNFTCollectionRequest, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4NFTCollection> result = new OASISResult<IWeb4NFTCollection>();
            if (createOASISNFTCollectionRequest == null)
            {
                result.IsError = true;
                result.Message = "The request is required. Please provide a valid ICreateWeb4NFTCollectionRequest.";
                return result;
            }
            string errorMessage = "Error occured in CreateNFTCollectionAsync in NFTManager. Reason:";

            Web4NFTCollection OASISNFTCollection = new Web4NFTCollection()
            {
                Name = createOASISNFTCollectionRequest.Title,
                Description = createOASISNFTCollectionRequest.Description,
                CreatedDate = DateTime.Now,
                CreatedByAvatarId = createOASISNFTCollectionRequest.CreatedBy,
                Image = createOASISNFTCollectionRequest.Image,
                ImageUrl = createOASISNFTCollectionRequest.ImageUrl,
                Thumbnail = createOASISNFTCollectionRequest.Thumbnail,
                ThumbnailUrl = createOASISNFTCollectionRequest.ThumbnailUrl,
                MetaData = createOASISNFTCollectionRequest.MetaData,
                Web4NFTs = createOASISNFTCollectionRequest.Web4NFTs,
                Web4NFTIds = createOASISNFTCollectionRequest.Web4NFTIds,
                Tags = createOASISNFTCollectionRequest.Tags
            };

            if (createOASISNFTCollectionRequest.Web4NFTIds == null)
                createOASISNFTCollectionRequest.Web4NFTIds = new List<string>();

            if (createOASISNFTCollectionRequest.Web4NFTs != null)
            {
                foreach (IWeb4NFT oasisNft in createOASISNFTCollectionRequest.Web4NFTs)
                {
                    if (!OASISNFTCollection.Web4NFTIds.Contains(oasisNft.Id.ToString()))
                        OASISNFTCollection.Web4NFTIds.Add(oasisNft.Id.ToString());
                }
            }

            //TODO: Not sure if we should store the entire NFTs in the collection or just their IDs?
            List<IWeb4NFT> nfts = OASISNFTCollection.Web4NFTs;
            OASISNFTCollection.Web4NFTs = null;

            OASISResult<Web4NFTCollection> saveResult = await OASISNFTCollection.SaveAsync<Web4NFTCollection>();

            //Dictionary<string, object> metaData = new Dictionary<string, object>()
            //{
            //    { "OASISNFTCOLLECTION.ID", Guid.NewGuid() },
            //    { "OASISNFTCOLLECTION.Title", createOASISNFTCollectionRequest.Title },
            //    { "OASISNFTCOLLECTION.Description", createOASISNFTCollectionRequest.Description  },
            //    { "OASISNFTCOLLECTION.CreatedDate", OASISNFTCollection.CreatedDate  },
            //    { "OASISNFTCOLLECTION.CreatedBy", createOASISNFTCollectionRequest.CreatedBy  },
            //    { "OASISNFTCOLLECTION.ImageUrl", createOASISNFTCollectionRequest.ImageUrl  },
            //    { "OASISNFTCOLLECTION.Image", createOASISNFTCollectionRequest.Image  },
            //    { "OASISNFTCOLLECTION.ThumbnailUrl", createOASISNFTCollectionRequest.ThumbnailUrl  },
            //    { "OASISNFTCOLLECTION.Thumbnail", createOASISNFTCollectionRequest.Thumbnail  },
            //    { "OASISNFTCOLLECTION.Web4NFTIds", createOASISNFTCollectionRequest.Web4NFTIds  },
            //    { "OASISNFTCOLLECTION.Tags", createOASISNFTCollectionRequest.Tags },
            //    { "OASISNFTCOLLECTION.MetaData", createOASISNFTCollectionRequest.MetaData }
            //};

            //OASISResult<IHolon> saveResult = await Data.SaveHolonAsync(new Holon()
            //{
            //    Id = Guid.Parse(metaData["OASISNFTCOLLECTION.ID"].ToString()),
            //    Name = $"OASIS NFT Collection with title {createOASISNFTCollectionRequest.Title}",
            //    Description = createOASISNFTCollectionRequest.Description,
            //    HolonType = HolonType.Web4NFTCollection,
            //    MetaData = metaData
            //}, providerType : providerType);

            if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
            {
                OASISNFTCollection.Web4NFTs = nfts;
                result.Result = OASISNFTCollection;
                result.Message = "OASIS NFT Collection created successfully.";
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving OASIS NFT Collection holon. Reason: {saveResult.Message}");

            return result;
        }

        public async Task<OASISResult<IWeb4GeoNFTCollection>> CreateWeb4GeoNFTCollectionAsyc(ICreateWeb4GeoNFTCollectionRequest createWeb4OASISGeoNFTCollectionRequest, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4GeoNFTCollection> result = new OASISResult<IWeb4GeoNFTCollection>();
            if (createWeb4OASISGeoNFTCollectionRequest == null)
            {
                result.IsError = true;
                result.Message = "The request is required. Please provide a valid ICreateWeb4GeoNFTCollectionRequest.";
                return result;
            }
            string errorMessage = "Error occured in CreateGeoNFTCollectionAsyc in NFTManager. Reason:";

            Web4GeoNFTCollection Web4OASISGeoNFTCollection = new Web4GeoNFTCollection()
            {
                Name = createWeb4OASISGeoNFTCollectionRequest.Title,
                Description = createWeb4OASISGeoNFTCollectionRequest.Description,
                CreatedDate = DateTime.Now,
                CreatedByAvatarId = createWeb4OASISGeoNFTCollectionRequest.CreatedBy,
                Image = createWeb4OASISGeoNFTCollectionRequest.Image,
                ImageUrl = createWeb4OASISGeoNFTCollectionRequest.ImageUrl,
                Thumbnail = createWeb4OASISGeoNFTCollectionRequest.Thumbnail,
                ThumbnailUrl = createWeb4OASISGeoNFTCollectionRequest.ThumbnailUrl,
                MetaData = createWeb4OASISGeoNFTCollectionRequest.MetaData,
                Web4GeoNFTs = createWeb4OASISGeoNFTCollectionRequest.Web4GeoNFTs,
                Web4GeoNFTIds = createWeb4OASISGeoNFTCollectionRequest.Web4GeoNFTIds,
                Tags = createWeb4OASISGeoNFTCollectionRequest.Tags
            };

            if (createWeb4OASISGeoNFTCollectionRequest.Web4GeoNFTIds == null)
                createWeb4OASISGeoNFTCollectionRequest.Web4GeoNFTIds = new List<string>();

            if (createWeb4OASISGeoNFTCollectionRequest.Web4GeoNFTIds != null)
            {
                foreach (IWeb4GeoSpatialNFT geoNFT in createWeb4OASISGeoNFTCollectionRequest.Web4GeoNFTs)
                {
                    if (!Web4OASISGeoNFTCollection.Web4GeoNFTIds.Contains(geoNFT.Id.ToString()))
                        Web4OASISGeoNFTCollection.Web4GeoNFTIds.Add(geoNFT.Id.ToString());
                }
            }

            //TODO: Not sure if we should store the entire NFTs in the collection or just their IDs?
            List<IWeb4GeoSpatialNFT> nfts = Web4OASISGeoNFTCollection.Web4GeoNFTs;
            Web4OASISGeoNFTCollection.Web4GeoNFTs = null;
            OASISResult<Web4GeoNFTCollection> saveResult = await Web4OASISGeoNFTCollection.SaveAsync<Web4GeoNFTCollection>();

            //Dictionary<string, object> metaData = new Dictionary<string, object>()
            //{
            //    { "OASISGEONFTCOLLECTION.ID", Guid.NewGuid() },
            //    { "OASISGEONFTCOLLECTION.Title", createWeb4OASISGeoNFTCollectionRequest.Title },
            //    { "OASISGEONFTCOLLECTION.Description", createWeb4OASISGeoNFTCollectionRequest.Description  },
            //    { "OASISGEONFTCOLLECTION.CreatedDate", Web4OASISGeoNFTCollection.CreatedDate  },
            //    { "OASISGEONFTCOLLECTION.CreatedBy", createWeb4OASISGeoNFTCollectionRequest.CreatedBy  },
            //    { "OASISGEONFTCOLLECTION.ImageUrl", createWeb4OASISGeoNFTCollectionRequest.ImageUrl  },
            //    { "OASISGEONFTCOLLECTION.Image", createWeb4OASISGeoNFTCollectionRequest.Image  },
            //    { "OASISGEONFTCOLLECTION.ThumbnailUrl", createWeb4OASISGeoNFTCollectionRequest.ThumbnailUrl  },
            //    { "OASISGEONFTCOLLECTION.Thumbnail", createWeb4OASISGeoNFTCollectionRequest.Thumbnail  },
            //    { "OASISGEONFTCOLLECTION.Web4GeoNFTIds", createWeb4OASISGeoNFTCollectionRequest.Web4GeoNFTIds  },
            //    { "OASISGEONFTCOLLECTION.Tags", createWeb4OASISGeoNFTCollectionRequest.Tags },
            //    { "OASISGEONFTCOLLECTION.MetaData", createWeb4OASISGeoNFTCollectionRequest.MetaData }
            //};

            //OASISResult<IHolon> saveResult = await Data.SaveHolonAsync(new Holon()
            //{
            //    Id = Guid.Parse(metaData["OASISGEONFTCOLLECTION.ID"].ToString()),
            //    Name = $"OASIS GeoNFT Collection with title {createWeb4OASISGeoNFTCollectionRequest.Title}",
            //    Description = createWeb4OASISGeoNFTCollectionRequest.Description,
            //    HolonType = HolonType.Web4NFTCollection,
            //    MetaData = metaData
            //}, providerType: providerType);

            if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
            {
                Web4OASISGeoNFTCollection.Web4GeoNFTs = nfts;
                result.Result = Web4OASISGeoNFTCollection;
                result.Message = "OASIS GeoNFT Collection created successfully.";
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving OASIS GeoNFT Collection holon. Reason: {saveResult.Message}");


            return result;
        }


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

    }
}