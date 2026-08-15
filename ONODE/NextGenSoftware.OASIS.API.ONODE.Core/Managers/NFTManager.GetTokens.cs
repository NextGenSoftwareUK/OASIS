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
        public async Task<OASISResult<bool>> DeleteWeb3NFTAsync(Guid avatarId, Guid id, bool softDelete = true, bool burnWeb3NFT = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new();
            string errorMessage = "Error occured in DeleteWeb3NFTAsync in NFTManager. Reason:";

            try
            {
                if (burnWeb3NFT)
                {
                    OASISResult<IWeb3NFTTransactionResponse> burnResult = await BurnWeb3NFTAsync(new BurnWeb3NFTRequest()
                    {
                        Web3NFTId = id,
                        BurntByAvatarId = avatarId,
                        OwnerPrivateKey = "",
                        OwnerPublicKey = "",
                        OwnerSeedPhrase = ""
                    });

                    if (!(burnResult != null && burnResult.Result != null && !burnResult.IsError))
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured burning Web3 NFT with id {id}. Reason: {burnResult?.Message}");
                }

                OASISResult<IHolon> deleteWeb4NFTResult = await Data.DeleteHolonAsync(id, avatarId, softDelete, providerType: providerType);

                if (deleteWeb4NFTResult != null && !deleteWeb4NFTResult.IsError && deleteWeb4NFTResult.Result != null)
                {
                    result.Result = true;

                    if (result.IsWarning)
                        result.Message = $"Web3 NFT deleted successfully but there was an issue burning the web3 NFT:\n\n{OASISResultHelper.BuildInnerMessageError(result.InnerMessages)} ";
                    else
                        result.Message = "Web3 NFT deleted successfully"; ;
                }
                else
                {
                    result.Result = false;
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured deleting Web3 NFT. Reason: {deleteWeb4NFTResult?.Message}");
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<bool>> DeleteWeb4NFTAsync(Guid avatarId, Guid id, bool softDelete = true, bool deleteChildWeb3NFTs = true, bool burnChildWeb3NFTs = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new();
            string errorMessage = "Error occured in DeleteWeb4NFTAsync in NFTManager. Reason:";

            try
            {
                if (deleteChildWeb3NFTs)
                {
                    OASISResult<IEnumerable<IWeb3NFT>> web3NFTs = await LoadAllWeb3NFTsAsync(id, providerType);

                    if (web3NFTs != null && web3NFTs.Result != null && !web3NFTs.IsError)
                    {
                        foreach (IWeb3NFT web3NFT in web3NFTs.Result)
                        {
                            OASISResult<bool> deleteWeb3NFTResult = await DeleteWeb3NFTAsync(avatarId, web3NFT.Id, softDelete, burnChildWeb3NFTs, providerType);

                            if (!(deleteWeb3NFTResult != null && !deleteWeb3NFTResult.IsError && deleteWeb3NFTResult.Result != null))
                                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured deleting Web3 NFT with id {web3NFT.Id} and title '{web3NFT.Title}'. Reason: {deleteWeb3NFTResult?.Message}");
                        }
                    }
                }

                OASISResult<IHolon> deleteWeb4NFTResult = await Data.DeleteHolonAsync(id, avatarId, softDelete, providerType: providerType);

                if (deleteWeb4NFTResult != null && !deleteWeb4NFTResult.IsError && deleteWeb4NFTResult.Result != null)
                {
                    result.Result = true;
                    result.IsError = false;

                    if (result.IsWarning)
                        result.Message = $"Web4 NFT deleted successfully but there were issues deleting one or more of the child web3 NFTs:\n\n{OASISResultHelper.BuildInnerMessageError(result.InnerMessages)} ";
                    else
                        result.Message = "Web4 NFT deleted successfully";
                }
                else
                {
                    result.Result = false;
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured deleting Web4 NFT. Reason: {deleteWeb4NFTResult?.Message}");
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<bool>> DeleteWeb4GeoNFTAsync(Guid avatarId, Guid id, bool softDelete = true, bool deleteChildWeb3NFTs = true, bool burnChildWeb3NFTs = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new();
            string errorMessage = "Error occured in DeleteWeb4GeoNFTAsync in NFTManager. Reason:";

            try
            {
                if (deleteChildWeb3NFTs)
                {
                    OASISResult<IEnumerable<IWeb3NFT>> web3NFTs = await LoadAllWeb3NFTsAsync(id, providerType);

                    if (web3NFTs != null && web3NFTs.Result != null && !web3NFTs.IsError)
                    {
                        foreach (IWeb3NFT web3NFT in web3NFTs.Result)
                        {
                            OASISResult<bool> deleteWeb3NFTResult = await DeleteWeb3NFTAsync(avatarId, id, softDelete, burnChildWeb3NFTs, providerType);

                            if (!(deleteWeb3NFTResult != null && !deleteWeb3NFTResult.IsError && deleteWeb3NFTResult.Result != null))
                                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} Error occured deleting Web3 NFT with id {web3NFT.Id} and title '{web3NFT.Title}'. Reason: {deleteWeb3NFTResult?.Message}");
                        }
                    }
                }

                OASISResult<IHolon> deleteWeb4NFTResult = await Data.DeleteHolonAsync(id, avatarId, softDelete, providerType: providerType);

                if (deleteWeb4NFTResult != null && !deleteWeb4NFTResult.IsError && deleteWeb4NFTResult.Result != null)
                {
                    result.Result = true;
                    result.IsError = false;

                    if (result.IsWarning)
                        result.Message = $"Web4 Geo-NFT deleted successfully but there were issues deleting one or more of the child web3 NFTs:\n\n{OASISResultHelper.BuildInnerMessageError(result.InnerMessages)} ";
                    else
                        result.Message = "Web4 Geo-NFT deleted successfully";
                }
                else
                {
                    result.Result = false;
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured deleting Web4 Geo-NFT. Reason: {deleteWeb4NFTResult?.Message}");
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IWeb3NFT>>> SearchWeb3NFTsAsync(string searchTerm, Guid avatarId, Guid parentWeb4NFTId = default, Dictionary<string, string> filterByMetaData = null, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode = MetaKeyValuePairMatchMode.All, bool searchOnlyForCurrentAvatar = true, ProviderType providerType = ProviderType.Default)
        {
            string errorMessage = "Error occured in SearchWeb3NFTsAsync in NFTManager. Reason:";
            OASISResult<IEnumerable<IWeb3NFT>> result = new OASISResult<IEnumerable<IWeb3NFT>>();
            result = DecodeNFTMetaData(await Data.SearchHolonsAsync(searchTerm, avatarId, parentWeb4NFTId, filterByMetaData, metaKeyValuePairMatchMode, searchOnlyForCurrentAvatar, HolonType.Web3NFT, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            return result;
        }

        public OASISResult<IEnumerable<IWeb3NFT>> SearchWeb3NFTs(string searchTerm, Guid avatarId, Guid parentWeb4NFTId = default, Dictionary<string, string> filterByMetaData = null, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode = MetaKeyValuePairMatchMode.All, bool searchOnlyForCurrentAvatar = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb3NFT>> result = new OASISResult<IEnumerable<IWeb3NFT>>();
            string errorMessage = "Error occured in SearchWeb3NFTs in NFTManager. Reason:";
            result = DecodeNFTMetaData(Data.SearchHolons(searchTerm, avatarId, parentWeb4NFTId, filterByMetaData, metaKeyValuePairMatchMode, searchOnlyForCurrentAvatar, HolonType.Web3NFT, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            return result;
        }

        public async Task<OASISResult<IEnumerable<IWeb4NFT>>> SearchWeb4NFTsAsync(string searchTerm, Guid avatarId, Dictionary<string, string> filterByMetaData = null, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode = MetaKeyValuePairMatchMode.All, bool searchOnlyForCurrentAvatar = true, ProviderType providerType = ProviderType.Default)
        {
            string errorMessage = "Error occured in SearchNFTsAsync in NFTManager. Reason:";
            OASISResult<IEnumerable<IWeb4NFT>> result = new OASISResult<IEnumerable<IWeb4NFT>>();
            result = DecodeNFTMetaData(await Data.SearchHolonsAsync(searchTerm, avatarId, default, filterByMetaData, metaKeyValuePairMatchMode, searchOnlyForCurrentAvatar, HolonType.Web4NFT, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            return result;
        }

        public OASISResult<IEnumerable<IWeb4NFT>> SearchWeb4NFTs(string searchTerm, Guid avatarId, Dictionary<string, string> filterByMetaData = null, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode = MetaKeyValuePairMatchMode.All, bool searchOnlyForCurrentAvatar = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb4NFT>> result = new OASISResult<IEnumerable<IWeb4NFT>>();
            string errorMessage = "Error occured in SearchNFTs in NFTManager. Reason:";
            result = DecodeNFTMetaData(Data.SearchHolons(searchTerm, avatarId, default, filterByMetaData, metaKeyValuePairMatchMode, searchOnlyForCurrentAvatar, HolonType.Web4NFT, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            return result;
        }

        public async Task<OASISResult<IEnumerable<IWeb4GeoSpatialNFT>>> SearchWeb4GeoNFTsAsync(string searchTerm, Guid avatarId, Dictionary<string, string> filterByMetaData = null, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode = MetaKeyValuePairMatchMode.All, bool searchOnlyForCurrentAvatar = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb4GeoSpatialNFT>> result = new OASISResult<IEnumerable<IWeb4GeoSpatialNFT>>();
            string errorMessage = "Error occured in SearchGeoNFTsAsync in NFTManager. Reason:";
            result = DecodeGeoNFTMetaData(await Data.SearchHolonsAsync(searchTerm, avatarId, default, filterByMetaData, metaKeyValuePairMatchMode, searchOnlyForCurrentAvatar, HolonType.Web4GeoNFT, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            return result;
        }
        public async Task<OASISResult<IEnumerable<IWeb4GeoSpatialNFT>>> SearchWeb4GeoNFTs(string searchTerm, Guid avatarId, Dictionary<string, string> filterByMetaData = null, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode = MetaKeyValuePairMatchMode.All, bool searchOnlyForCurrentAvatar = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb4GeoSpatialNFT>> result = new OASISResult<IEnumerable<IWeb4GeoSpatialNFT>>();
            string errorMessage = "Error occured in SearchGeoNFT in NFTManager. Reason:";
            result = DecodeGeoNFTMetaData(Data.SearchHolons(searchTerm, avatarId, default, filterByMetaData, metaKeyValuePairMatchMode, searchOnlyForCurrentAvatar, HolonType.Web4GeoNFT, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            return result;
        }

        public async Task<OASISResult<IEnumerable<IWeb4NFTCollection>>> SearchWeb4NFTCollectionsAsync(string searchTerm, Guid avatarId, Dictionary<string, string> filterByMetaData = null, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode = MetaKeyValuePairMatchMode.All, bool searchOnlyForCurrentAvatar = true, ProviderType providerType = ProviderType.Default)
        {
            string errorMessage = "Error occured in SearchNFTCollectionsAsync in NFTManager. Reason:";
            OASISResult<IEnumerable<IWeb4NFTCollection>> result = new OASISResult<IEnumerable<IWeb4NFTCollection>>();
            OASISResult<IEnumerable<Web4NFTCollection>> collectionResults = await Data.SearchHolonsAsync<Web4NFTCollection>(searchTerm, avatarId, default, filterByMetaData, metaKeyValuePairMatchMode, searchOnlyForCurrentAvatar, HolonType.Web4NFTCollection, true, true, 0, true, false, HolonType.All, 0, providerType);
            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(collectionResults, result);
            result.Result = collectionResults.Result;
            return result;
        }

        public OASISResult<IEnumerable<IWeb4NFTCollection>> SearchWeb4NFTCollections(string searchTerm, Guid avatarId, Dictionary<string, string> filterByMetaData = null, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode = MetaKeyValuePairMatchMode.All, bool searchOnlyForCurrentAvatar = true, ProviderType providerType = ProviderType.Default)
        {
            string errorMessage = "Error occured in SearchNFTCollections in NFTManager. Reason:";
            OASISResult<IEnumerable<IWeb4NFTCollection>> result = new OASISResult<IEnumerable<IWeb4NFTCollection>>();
            OASISResult<IEnumerable<Web4NFTCollection>> collectionResults = Data.SearchHolons<Web4NFTCollection>(searchTerm, avatarId, default, filterByMetaData, metaKeyValuePairMatchMode, searchOnlyForCurrentAvatar, HolonType.Web4NFTCollection, true, true, 0, true, false, HolonType.All, 0, providerType);
            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(collectionResults, result);
            result.Result = collectionResults.Result;
            return result;
        }

        public async Task<OASISResult<IEnumerable<IWeb4GeoNFTCollection>>> SearchWeb4GeoNFTCollectionsAsync(string searchTerm, Guid avatarId, Dictionary<string, string> filterByMetaData = null, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode = MetaKeyValuePairMatchMode.All, bool searchOnlyForCurrentAvatar = true, ProviderType providerType = ProviderType.Default)
        {
            string errorMessage = "Error occured in SearchGeoNFTCollectionsAsync in NFTManager. Reason:";
            OASISResult<IEnumerable<IWeb4GeoNFTCollection>> result = new OASISResult<IEnumerable<IWeb4GeoNFTCollection>>();
            OASISResult<IEnumerable<Web4GeoNFTCollection>> collectionResults = await Data.SearchHolonsAsync<Web4GeoNFTCollection>(searchTerm, avatarId, default, filterByMetaData, metaKeyValuePairMatchMode, searchOnlyForCurrentAvatar, HolonType.Web4GeoNFTCollection, true, true, 0, true, false, HolonType.All, 0, providerType);
            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(collectionResults, result);
            result.Result = collectionResults.Result;
            return result;
        }

        public OASISResult<IEnumerable<IWeb4GeoNFTCollection>> SearchWeb4GeoNFTCollections(string searchTerm, Guid avatarId, Dictionary<string, string> filterByMetaData = null, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode = MetaKeyValuePairMatchMode.All, bool searchOnlyForCurrentAvatar = true, ProviderType providerType = ProviderType.Default)
        {
            string errorMessage = "Error occured in SearchGeoNFTCollections in NFTManager. Reason:";
            OASISResult<IEnumerable<IWeb4GeoNFTCollection>> result = new OASISResult<IEnumerable<IWeb4GeoNFTCollection>>();
            OASISResult<IEnumerable<Web4GeoNFTCollection>> collectionResults = Data.SearchHolons<Web4GeoNFTCollection>(searchTerm, avatarId, default, filterByMetaData, metaKeyValuePairMatchMode, searchOnlyForCurrentAvatar, HolonType.Web4GeoNFTCollection, true, true, 0, true, false, HolonType.All, 0, providerType);
            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(collectionResults, result);
            result.Result = collectionResults.Result;
            return result;
        }

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

            if (request.InitialSize >= 0)
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
                        if (!request.WaitTillCollectionSizeSet)
                        {
                            OASISErrorHandling.HandleWarning(ref result, setSizeErrorMessage, onlyLogToInnerMessages: true);
                            break;
                        }
                    }

                    Thread.Sleep(request.AttemptToSetCollectionSizeEveryXSeconds * 1000);

                    if (startTime.AddSeconds(request.WaitForCollectionSizeToBeSetInSeconds).Ticks < DateTime.Now.Ticks)
                    {
                        string timeoutMsg = $"Timeout expired, WaitForCollectionSizeToBeSetInSeconds ({request.WaitForCollectionSizeToBeSetInSeconds}) exceeded. NFT was minted successfully but collection size could not be set. Try calling SetCollectionSize separately.";
                        if (!string.IsNullOrEmpty(setSizeErrorMessage))
                            timeoutMsg += $" Last error: {setSizeErrorMessage}";
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} {timeoutMsg}", onlyLogToInnerMessages: true);
                        break;
                    }

                    setSizeErrorMessage = "";

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


    }
}
