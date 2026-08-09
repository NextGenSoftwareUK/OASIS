using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using Nethereum.Signer;
using Nethereum.Hex.HexConvertors.Extensions;
using System.IO;
using static NextGenSoftware.Utilities.KeyHelper;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS
{
    public partial class TRONOASIS
    {
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                // Load holons for parent from TRON blockchain using smart contract
                var contractAddress = GetOASISContractAddress();
                var functionName = "getHolonsForParent";
                var parameters = new object[] { id.ToString(), type.ToString() };

                // Call TRON smart contract to load holons for parent
                var contractResult = await CallContractAsync(contractAddress, functionName, parameters);
                if (!contractResult.IsError && !string.IsNullOrWhiteSpace(contractResult.Result))
                {
                    var holons = ParseTRONToHolons(contractResult.Result);
                    result.Result = holons;
                    result.IsError = false;
                    result.Message = "Holons loaded successfully from TRON for parent";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holons from TRON for parent: {contractResult.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons from TRON for parent: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                // Load holons for parent by provider key from TRON blockchain using smart contract
                var contractAddress = GetOASISContractAddress();
                var functionName = "getHolonsForParentByProviderKey";
                var parameters = new object[] { providerKey, type.ToString() };

                // Call TRON smart contract to load holons for parent by provider key
                var contractResult = await CallContractAsync(contractAddress, functionName, parameters);
                if (!contractResult.IsError && !string.IsNullOrWhiteSpace(contractResult.Result))
                {
                    var holons = ParseTRONToHolons(contractResult.Result);
                    result.Result = holons;
                    result.IsError = false;
                    result.Message = "Holons loaded successfully from TRON for parent by provider key";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holons from TRON for parent by provider key: {contractResult.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons from TRON for parent by provider key: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool loadChildrenFromProvider = false, bool continueOnError = true, int version = 0)
        {
            return LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentByCustomKeyAsync(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (string.IsNullOrWhiteSpace(customKey))
                {
                    OASISErrorHandling.HandleError(ref result, "Custom key cannot be null or empty");
                    return result;
                }

                // First load the parent holon by custom key
                var parentResult = await LoadHolonByCustomKeyAsync(customKey, false, false, 0, continueOnError, loadChildrenFromProvider, version);
                
                if (parentResult.IsError || parentResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Parent holon not found: {parentResult.Message}");
                    return result;
                }

                // Then load children for the parent
                var childrenResult = await LoadHolonsForParentAsync(parentResult.Result.Id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
                
                result.Result = childrenResult.Result;
                result.IsError = childrenResult.IsError;
                result.Message = childrenResult.Message;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent by custom key from TRON: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IEnumerable<IHolon>> LoadHolonsForParentByCustomKey(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsForParentByCustomKeyAsync(customKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                // Query TRON smart contract for holons matching metadata
                var contractAddress = GetOASISContractAddress();
                var functionName = "getHolonsByMetadata";
                var parameters = new object[] { metaKey, metaValue, (int)type };

                var contractResult = await CallContractAsync(contractAddress, functionName, parameters);
                if (!contractResult.IsError && !string.IsNullOrWhiteSpace(contractResult.Result))
                {
                    var holons = ParseTRONToHolons(contractResult.Result);
                    if (holons != null)
                    {
                        result.Result = holons.Where(h => type == HolonType.All || h.HolonType == type).ToList();
                        result.IsError = false;
                        result.Message = $"Loaded {result.Result.Count()} holons by metadata from TRON";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "No holons found with matching metadata in TRON blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holons by metadata from TRON: {contractResult.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata from TRON: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                // Serialize metadata dictionary to JSON for query
                var metadataJson = JsonSerializer.Serialize(metaKeyValuePairs);
                
                // Query TRON smart contract for holons matching multiple metadata pairs
                var contractAddress = GetOASISContractAddress();
                var functionName = "getHolonsByMetadataMulti";
                var parameters = new object[] { metadataJson, metaKeyValuePairMatchMode.ToString(), (int)type };

                var contractResult = await CallContractAsync(contractAddress, functionName, parameters);
                if (!contractResult.IsError && !string.IsNullOrWhiteSpace(contractResult.Result))
                {
                    var holons = ParseTRONToHolons(contractResult.Result);
                    if (holons != null)
                    {
                        result.Result = holons.Where(h => type == HolonType.All || h.HolonType == type).ToList();
                        result.IsError = false;
                        result.Message = $"Loaded {result.Result.Count()} holons by metadata from TRON";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "No holons found with matching metadata in TRON blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holons by metadata from TRON: {contractResult.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata from TRON: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                // Query TRON smart contract for all holons
                var contractAddress = GetOASISContractAddress();
                var functionName = "getAllHolons";
                var parameters = new object[] { (int)type };

                var contractResult = await CallContractAsync(contractAddress, functionName, parameters);
                if (!contractResult.IsError && !string.IsNullOrWhiteSpace(contractResult.Result))
                {
                    var holons = ParseTRONToHolons(contractResult.Result);
                    if (holons != null)
                    {
                        result.Result = holons.Where(h => type == HolonType.All || h.HolonType == type).ToList();
                        result.IsError = false;
                        result.Message = $"Loaded {result.Result.Count()} holons from TRON blockchain";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "No holons found in TRON blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holons from TRON: {contractResult.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading all holons from TRON: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (holon == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Holon cannot be null");
                    return result;
                }

                // Get wallet for the holon
                var walletResult = await GetWalletAddressForAvatar(holon.CreatedByAvatarId != Guid.Empty ? holon.CreatedByAvatarId : holon.Id);
                if (walletResult.IsError || string.IsNullOrWhiteSpace(walletResult.Result))
                {
                    OASISErrorHandling.HandleError(ref result, "Could not retrieve wallet address for holon");
                    return result;
                }

                // Save holon to TRON smart contract
                var contractAddress = GetOASISContractAddress();
                var functionName = "saveHolon";
                var holonJson = JsonSerializer.Serialize(holon);
                var parameters = new object[]
                {
                    holon.Id.ToString(),
                    holon.Name ?? "",
                    holon.Description ?? "",
                    (int)holon.HolonType,
                    holon.ParentHolonId.ToString(),
                    holonJson
                };

                var contractResult = await CallContractAsync(contractAddress, functionName, parameters, walletResult.Result);
                if (!contractResult.IsError)
                {
                    result.Result = holon;
                    result.IsError = false;
                    result.IsSaved = true;
                    result.Message = "Holon saved successfully to TRON blockchain";

                    // Handle children if requested
                    if (saveChildren && holon.Children != null && holon.Children.Any())
                    {
                        foreach (var child in holon.Children)
                        {
                            var childResult = await SaveHolonAsync(child, saveChildren, recursive, maxChildDepth - 1, continueOnError, saveChildrenOnProvider);
                            if (!continueOnError && childResult.IsError)
                            {
                                OASISErrorHandling.HandleError(ref result, $"Failed to save child holon {child.Id}: {childResult.Message}");
                                return result;
                            }
                        }
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to save holon to TRON: {contractResult.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving holon to TRON: {ex.Message}", ex);
            }
            return result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (holons == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Holons cannot be null");
                    return result;
                }

                var savedHolons = new List<IHolon>();
                foreach (var holon in holons)
                {
                    var saveResult = await SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
                    if (!saveResult.IsError && saveResult.Result != null)
                    {
                        savedHolons.Add(saveResult.Result);
                    }
                    else if (!continueOnError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to save holon {holon.Id}: {saveResult.Message}");
                        return result;
                    }
                }

                result.Result = savedHolons;
                result.IsError = false;
                result.Message = $"Saved {savedHolons.Count} holons to TRON blockchain";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving holons to TRON: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                // First load the holon to return it
                var loadResult = await LoadHolonAsync(id);
                if (loadResult.IsError || loadResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Holon with ID {id} not found");
                    return result;
                }

                // Get wallet for the holon
                var walletResult = await GetWalletAddressForAvatar(loadResult.Result.CreatedByAvatarId != Guid.Empty ? loadResult.Result.CreatedByAvatarId : id);
                if (walletResult.IsError || string.IsNullOrWhiteSpace(walletResult.Result))
                {
                    OASISErrorHandling.HandleError(ref result, "Could not retrieve wallet address for holon deletion");
                    return result;
                }

                // Delete holon from TRON smart contract
                var contractAddress = GetOASISContractAddress();
                var functionName = "deleteHolon";
                var parameters = new object[] { id.ToString() };

                var contractResult = await CallContractAsync(contractAddress, functionName, parameters, walletResult.Result);
                if (!contractResult.IsError)
                {
                    result.Result = loadResult.Result;
                    result.IsError = false;
                    result.Message = "Holon deleted successfully from TRON blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to delete holon from TRON: {contractResult.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting holon from TRON: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id)
        {
            return DeleteHolonAsync(id).Result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            // First load the holon to get its ID, then delete
            var loadResult = await LoadHolonAsync(providerKey);
            if (loadResult.IsError || loadResult.Result == null)
            {
                var result = new OASISResult<IHolon>();
                OASISErrorHandling.HandleError(ref result, $"Holon with provider key {providerKey} not found");
                return result;
            }

            // Delete using the holon's ID
            return await DeleteHolonAsync(loadResult.Result.Id);
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
        {
            return DeleteHolonAsync(providerKey).Result;
        }

    }
}