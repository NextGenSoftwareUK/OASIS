using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using System.Text.Json.Serialization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.Hex.HexConvertors.Extensions;
using System.Numerics;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

namespace NextGenSoftware.OASIS.API.Providers.OptimismOASIS
{
    public partial class OptimismOASIS_Legacy
    {
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Optimism provider is not activated");
                    return response;
                }

                if (_contract == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Smart contract not initialized");
                    return response;
                }

                if (metaKeyValuePairs == null || !metaKeyValuePairs.Any())
                {
                    OASISErrorHandling.HandleError(ref response, "Metadata key-value pairs cannot be null or empty");
                    return response;
                }

                // Real Optimism implementation: Load all holons and filter by metadata
                var getUserHolonsFunction = _contract.GetFunction("getUserHolons");
                var holonIds = await getUserHolonsFunction.CallAsync<List<string>>(_account.Address);

                var matchingHolons = new List<IHolon>();

                foreach (var holonId in holonIds)
                {
                    try
                    {
                        var getHolonFunction = _contract.GetFunction("getHolon");
                        var holonData = await getHolonFunction.CallDeserializingToObjectAsync<GetHolonOutputDTO>(holonId);

                        if (holonData != null)
                        {
                            // Parse metadata
                            Dictionary<string, object> metadata = null;
                            if (!string.IsNullOrEmpty(holonData.Metadata))
                            {
                                try
                                {
                                    metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(holonData.Metadata);
                                }
                                catch { }
                            }

                            // Check if holon matches metadata criteria
                            bool matches = false;
                            if (metadata != null)
                            {
                                if (metaKeyValuePairMatchMode == MetaKeyValuePairMatchMode.All)
                                {
                                    // All key-value pairs must match
                                    matches = metaKeyValuePairs.All(kvp =>
                                        metadata.ContainsKey(kvp.Key) &&
                                        metadata[kvp.Key]?.ToString() == kvp.Value);
                                }
                                else
                                {
                                    // At least one key-value pair must match
                                    matches = metaKeyValuePairs.Any(kvp =>
                                        metadata.ContainsKey(kvp.Key) &&
                                        metadata[kvp.Key]?.ToString() == kvp.Value);
                                }
                            }

                            if (matches)
                            {
                                var holon = new Holon
                                {
                                    Id = Guid.Parse(holonId),
                                    Name = holonData.Name,
                                    Description = holonData.Description,
                                    HolonType = Enum.Parse<HolonType>(holonData.HolonType),
                                    MetaData = metadata
                                };

                                // Parse parent ID if available
                                if (!string.IsNullOrEmpty(holonData.ParentId) && Guid.TryParse(holonData.ParentId, out var parentId))
                                {
                                    holon.ParentHolonId = parentId;
                                }

                                // Filter by type if specified
                                if (type == HolonType.All || holon.HolonType == type)
                                {
                                    // Load children if requested
                                    if (loadChildren && (maxChildDepth == 0 || curentChildDepth < maxChildDepth))
                                    {
                                        var childrenResult = await LoadHolonsForParentAsync(holon.Id, type, loadChildren, recursive, maxChildDepth, curentChildDepth + 1, continueOnError, loadChildrenFromProvider, version);
                                        if (!childrenResult.IsError && childrenResult.Result != null)
                                        {
                                            holon.Children = childrenResult.Result.ToList();
                                        }
                                    }

                                    matchingHolons.Add(holon);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (!continueOnError)
                        {
                            OASISErrorHandling.HandleError(ref response, $"Error loading holon {holonId}: {ex.Message}", ex);
                            return response;
                        }
                    }
                }

                response.Result = matchingHolons;
                response.IsError = false;
                response.Message = $"Loaded {matchingHolons.Count} holons matching metadata from Optimism";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadHolonsByMetaDataAsync: {ex.Message}", ex);
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Optimism provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadAllHolons is not supported by Optimism provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadAllHolons: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Optimism provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadAllHolonsAsync is not supported by Optimism provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadAllHolonsAsync: {ex.Message}");
            }
            return response;
        }

        // Save/Delete Holon methods
        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false)
        {
            return SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider).Result;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false)
        {
            var response = new OASISResult<IHolon>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Optimism provider is not activated");
                    return response;
                }

                if (_contract == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Smart contract not initialized");
                    return response;
                }

                if (holon == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Holon cannot be null");
                    return response;
                }

                // Real Optimism implementation: Save holon to smart contract
                var holonId = holon.Id.ToString();
                var holonData = new
                {
                    holonId = holonId,
                    name = holon.Name ?? "",
                    description = holon.Description ?? "",
                    holonType = holon.HolonType.ToString(),
                    metadata = JsonSerializer.Serialize(holon.MetaData ?? new Dictionary<string, object>()),
                    parentId = holon.ParentHolonId != Guid.Empty ? holon.ParentHolonId.ToString() : ""
                };

                // Check if holon exists
                var getHolonFunction = _contract.GetFunction("getHolon");
                bool holonExists = false;
                try
                {
                    await getHolonFunction.CallDeserializingToObjectAsync<GetHolonOutputDTO>(holonId);
                    holonExists = true;
                }
                catch { }

                Nethereum.Contracts.Function function;
                if (holonExists)
                {
                    // Update existing holon
                    function = _contract.GetFunction("updateHolon");
                }
                else
                {
                    // Create new holon
                    function = _contract.GetFunction("createHolon");
                }

                var gasEstimate = await function.EstimateGasAsync(
                    holonData.holonId,
                    holonData.name,
                    holonData.description,
                    holonData.holonType,
                    holonData.metadata,
                    holonData.parentId
                );

                var transactionReceipt = await function.SendTransactionAndWaitForReceiptAsync(
                    _account.Address,
                    gasEstimate,
                    null,
                    null,
                    holonData.holonId,
                    holonData.name,
                    holonData.description,
                    holonData.holonType,
                    holonData.metadata,
                    holonData.parentId
                );

                if (transactionReceipt.Status.Value == 1)
                {
                    // Save children if requested
                    if (saveChildren && holon.Children != null && holon.Children.Any() && (maxChildDepth == 0 || maxChildDepth > 0))
                    {
                        var childrenResult = await SaveHolonsAsync(holon.Children, saveChildren, recursive, maxChildDepth, 0, continueOnError, loadChildrenFromProvider);
                        if (childrenResult.IsError && !continueOnError)
                        {
                            OASISErrorHandling.HandleError(ref response, $"Error saving holon children: {childrenResult.Message}");
                            return response;
                        }
                    }

                    response.Result = holon;
                    response.IsError = false;
                    response.Message = $"Holon saved to Optimism successfully. Transaction hash: {transactionReceipt.TransactionHash}";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, "Transaction failed on Optimism");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error saving holon to Optimism: {ex.Message}", ex);
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Optimism provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "SaveHolons is not supported by Optimism provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in SaveHolons: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Optimism provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "SaveHolonsAsync is not supported by Optimism provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in SaveHolonsAsync: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id)
        {
            var response = new OASISResult<IHolon>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Optimism provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "DeleteHolon is not supported by Optimism provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in DeleteHolon: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var response = new OASISResult<IHolon>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Optimism provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "DeleteHolonAsync is not supported by Optimism provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in DeleteHolonAsync: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
        {
            var response = new OASISResult<IHolon>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Optimism provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "DeleteHolon is not supported by Optimism provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in DeleteHolon: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            var response = new OASISResult<IHolon>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Optimism provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "DeleteHolonAsync is not supported by Optimism provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in DeleteHolonAsync: {ex.Message}");
            }
            return response;
        }

        // Search methods
        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
        }

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var response = new OASISResult<ISearchResults>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Optimism provider is not activated");
                    return response;
                }

                if (_contract == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Smart contract not initialized");
                    return response;
                }

                if (searchParams == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Search parameters cannot be null");
                    return response;
                }

                // Extract search query from SearchGroups
                string searchQuery = null;
                if (searchParams.SearchGroups != null && searchParams.SearchGroups.Any())
                {
                    var firstGroup = searchParams.SearchGroups.FirstOrDefault();
                    if (firstGroup is ISearchTextGroup textGroup && !string.IsNullOrWhiteSpace(textGroup.SearchQuery))
                    {
                        searchQuery = textGroup.SearchQuery;
                    }
                }

                var searchResults = new SearchResults();
                var matchingHolons = new List<IHolon>();
                var matchingAvatars = new List<IAvatar>();

                if (!string.IsNullOrEmpty(searchQuery))
                {
                    // Search through avatars
                    var getUserAvatarsFunction = _contract.GetFunction("getUserAvatars");
                    var avatarIds = await getUserAvatarsFunction.CallAsync<List<string>>(_account.Address);

                    foreach (var avatarId in avatarIds)
                    {
                        try
                        {
                            var getAvatarFunction = _contract.GetFunction("getAvatar");
                            var avatarData = await getAvatarFunction.CallDeserializingToObjectAsync<GetAvatarOutputDTO>(avatarId);

                            if (avatarData != null && (
                                (avatarData.Username != null && avatarData.Username.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)) ||
                                (avatarData.Email != null && avatarData.Email.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)) ||
                                (avatarData.FirstName != null && avatarData.FirstName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)) ||
                                (avatarData.LastName != null && avatarData.LastName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase))
                            ))
                            {
                                var avatar = new Avatar
                                {
                                    Id = Guid.Parse(avatarId),
                                    Username = avatarData.Username,
                                    Email = avatarData.Email,
                                    FirstName = avatarData.FirstName,
                                    LastName = avatarData.LastName,
                                    AvatarType = new EnumValue<AvatarType>(Enum.Parse<AvatarType>(avatarData.AvatarType))
                                };

                                if (!string.IsNullOrEmpty(avatarData.Metadata))
                                {
                                    try
                                    {
                                        avatar.MetaData = JsonSerializer.Deserialize<Dictionary<string, object>>(avatarData.Metadata);
                                    }
                                    catch { }
                                }

                                matchingAvatars.Add(avatar);
                            }
                        }
                        catch { continue; }
                    }

                    // Search through holons
                    var getUserHolonsFunction = _contract.GetFunction("getUserHolons");
                    var holonIds = await getUserHolonsFunction.CallAsync<List<string>>(_account.Address);

                    foreach (var holonId in holonIds)
                    {
                        try
                        {
                            var getHolonFunction = _contract.GetFunction("getHolon");
                            var holonData = await getHolonFunction.CallDeserializingToObjectAsync<GetHolonOutputDTO>(holonId);

                            if (holonData != null && (
                                (holonData.Name != null && holonData.Name.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)) ||
                                (holonData.Description != null && holonData.Description.Contains(searchQuery, StringComparison.OrdinalIgnoreCase))
                            ))
                            {
                                var holon = new Holon
                                {
                                    Id = Guid.Parse(holonId),
                                    Name = holonData.Name,
                                    Description = holonData.Description,
                                    HolonType = Enum.Parse<HolonType>(holonData.HolonType)
                                };

                                if (!string.IsNullOrEmpty(holonData.Metadata))
                                {
                                    try
                                    {
                                        holon.MetaData = JsonSerializer.Deserialize<Dictionary<string, object>>(holonData.Metadata);
                                    }
                                    catch { }
                                }

                                matchingHolons.Add(holon);
                            }
                        }
                        catch { continue; }
                    }
                }

                searchResults.SearchResultAvatars = matchingAvatars;
                searchResults.SearchResultHolons = matchingHolons;
                response.Result = searchResults;
                response.IsError = false;
                response.Message = $"Search completed: found {matchingAvatars.Count} avatars and {matchingHolons.Count} holons";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in SearchAsync: {ex.Message}", ex);
            }
            return response;
        }

        // Export methods
        public override OASISResult<IEnumerable<IHolon>> ExportAll(int maxChildDepth = 0)
        {
            return ExportAllAsync(maxChildDepth).Result;
        }

    }
}
