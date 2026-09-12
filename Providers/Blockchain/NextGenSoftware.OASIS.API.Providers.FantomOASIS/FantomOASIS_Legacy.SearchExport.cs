using System;
using Nethereum.Hex.HexConvertors.Extensions;
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
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Holons;
using System.Text.Json.Serialization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

namespace NextGenSoftware.OASIS.API.Providers.FantomOASIS
{
    public partial class FantomOASIS_Legacy
    {
        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false)
        {
            return SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }

                if (_contract == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Smart contract not initialized");
                    return response;
                }

                if (holons == null || !holons.Any())
                {
                    OASISErrorHandling.HandleError(ref response, "Holons collection cannot be null or empty");
                    return response;
                }

                // Real Fantom implementation: Save multiple holons
                var savedHolons = new List<IHolon>();
                var errors = new List<string>();

                foreach (var holon in holons)
                {
                    try
                    {
                        var saveResult = await SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider);
                        if (!saveResult.IsError && saveResult.Result != null)
                        {
                            savedHolons.Add(saveResult.Result);
                        }
                        else if (!continueOnError)
                        {
                            OASISErrorHandling.HandleError(ref response, $"Error saving holon {holon.Id}: {saveResult.Message}");
                            return response;
                        }
                        else
                        {
                            errors.Add($"Holon {holon.Id}: {saveResult.Message}");
                        }
                    }
                    catch (Exception ex)
                    {
                        if (!continueOnError)
                        {
                            OASISErrorHandling.HandleError(ref response, $"Error saving holon {holon.Id}: {ex.Message}", ex);
                            return response;
                        }
                        errors.Add($"Holon {holon.Id}: {ex.Message}");
                    }
                }

                response.Result = savedHolons;
                response.IsError = errors.Any();
                if (errors.Any())
                {
                    response.Message = $"Saved {savedHolons.Count} holons with {errors.Count} errors: {string.Join("; ", errors)}";
                }
                else
                {
                    response.Message = $"Saved {savedHolons.Count} holons to Fantom successfully";
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in SaveHolonsAsync: {ex.Message}", ex);
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
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "DeleteHolon is not supported by Fantom provider");
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
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "DeleteHolonAsync is not supported by Fantom provider");
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
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "DeleteHolon is not supported by Fantom provider");
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
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "DeleteHolonAsync is not supported by Fantom provider");
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
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
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
                    // Search through avatars using getUserAvatars
                    try
                    {
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
                    }
                    catch { }

                    // Search through holons using getUserHolons
                    try
                    {
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
                                    (holonData.Description != null && holonData.Description.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)) ||
                                    (holonData.Metadata != null && holonData.Metadata.Contains(searchQuery, StringComparison.OrdinalIgnoreCase))
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

                                    if (!string.IsNullOrEmpty(holonData.ParentId) && Guid.TryParse(holonData.ParentId, out var parentId))
                                    {
                                        holon.ParentHolonId = parentId;
                                    }

                                    matchingHolons.Add(holon);
                                }
                            }
                            catch { continue; }
                        }
                    }
                    catch { }
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

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int maxChildDepth = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }

                if (_contract == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Smart contract not initialized");
                    return response;
                }

                // Real Fantom implementation: Export all holons for the current user
                var allHolonsResult = await LoadAllHolonsAsync(HolonType.All, true, true, maxChildDepth, 0, true, false, 0);
                if (allHolonsResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref response, $"Error loading holons for export: {allHolonsResult.Message}");
                    return response;
                }

                response.Result = allHolonsResult.Result;
                response.IsError = false;
                response.Message = $"Exported {allHolonsResult.Result?.Count() ?? 0} holons from Fantom";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in ExportAllAsync: {ex.Message}", ex);
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid id, int maxChildDepth = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "ExportAllDataForAvatarById is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in ExportAllDataForAvatarById: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid id, int maxChildDepth = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "ExportAllDataForAvatarByIdAsync is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in ExportAllDataForAvatarByIdAsync: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string username, int maxChildDepth = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Fantom provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "ExportAllDataForAvatarByUsername is not supported by Fantom provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in ExportAllDataForAvatarByUsername: {ex.Message}");
            }
            return response;
        }

    }
}
