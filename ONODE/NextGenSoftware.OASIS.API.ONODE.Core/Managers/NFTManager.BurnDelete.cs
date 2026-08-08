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
        public async Task<OASISResult<IWeb3NFTTransactionResponse>> BurnWeb3NFTAsync(IBurnWeb3NFTRequest request)
        {
            OASISResult<IWeb3NFTTransactionResponse> result = new();
            if (request == null)
            {
                result.IsError = true;
                result.Message = "The request is required. Please provide a valid IBurnWeb3NFTRequest.";
                return result;
            }
            string errorMessage = "Error occured in BurnWeb3NFTAsync in NFTManager. Reason:";

            try
            {
                if (string.IsNullOrEmpty(request.NFTTokenAddress) && request.Web3NFTId == Guid.Empty)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Both the NFTTokenAddress and Web3NFTId are missing, you need to specify at least one of these!");
                    return result;
                }

                OASISResult<IWeb3NFT> web3NFTResult = await LoadWeb3NftAsync(request.Web3NFTId);

                if (web3NFTResult != null && web3NFTResult.Result != null && !web3NFTResult.IsError)
                {
                    request.NFTTokenAddress = web3NFTResult.Result.NFTTokenAddress;

                    OASISResult<IOASISNFTProvider> providerResult = GetNFTProvider(web3NFTResult.Result.OnChainProvider.Value);

                    if (providerResult != null && providerResult.Result != null && !providerResult.IsError)
                    {
                        bool burnt = false;
                        DateTime startTime = DateTime.Now;

                        do
                        {
                            try
                            {
                                OASISResult<IWeb3NFTTransactionResponse> burnResult = await providerResult.Result.BurnNFTAsync(request);
                                string burnErrorMessage = "";

                                if (burnResult != null && burnResult.Result != null && !burnResult.IsError)
                                {
                                    burnt = true;
                                    result.Result = burnResult.Result;
                                    result.Message = "Web3 NFT Burnt Successfully!";
                                }
                                else
                                    burnErrorMessage = $"{errorMessage} Error occured calling BurnNFTAsync on provider {web3NFTResult.Result.OnChainProvider.Name}. Reason: {providerResult.Message}.";

                                if (!burnt && !request.WaitTillNFTBurnt)
                                {
                                    OASISErrorHandling.HandleError(ref result, $"{burnErrorMessage} WaitTillNFTBurnt is false so aborting!");
                                    break;
                                }

                                Thread.Sleep(request.AttemptToBurnEveryXSeconds * 1000);

                                if (startTime.AddSeconds(request.WaitForNFTToBurnInSeconds).Ticks < DateTime.Now.Ticks)
                                {
                                    OASISErrorHandling.HandleError(ref result, $"{burnErrorMessage}Timeout expired, WaitForNFTToBurnInSeconds ({request.WaitForNFTToBurnInSeconds}) exceeded, try increasing and trying again!");
                                    break;
                                }
                            }
                            catch (Exception e)
                            {
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured calling BurnNFTAsync on provider {web3NFTResult.Result.OnChainProvider.Name} : {e.Message}", e);
                            }
                        }
                        while (!burnt);
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured calling LoadWeb3NftAsync. Reason: {web3NFTResult.Message}");
                    return result;
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

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

    }
}