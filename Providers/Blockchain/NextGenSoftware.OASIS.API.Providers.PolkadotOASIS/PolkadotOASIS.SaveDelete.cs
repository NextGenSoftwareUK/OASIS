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
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using System.Text.Json.Serialization;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS
{
    public partial class PolkadotOASIS
    {
        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Polkadot provider: {activateResult.Message}");
                        return response;
                    }
                }

                if (holons == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Holons cannot be null");
                    return response;
                }

                var savedHolons = new List<IHolon>();
                var errors = new List<string>();

                foreach (var holon in holons)
                {
                    var saveResult = await SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
                    
                    if (saveResult.IsError)
                    {
                        errors.Add($"Failed to save holon {holon.Id}: {saveResult.Message}");
                        if (!continueOnError)
                        {
                            OASISErrorHandling.HandleError(ref response, string.Join("; ", errors));
                            return response;
                        }
                    }
                    else if (saveResult.Result != null)
                    {
                        savedHolons.Add(saveResult.Result);
                    }
                }

                response.Result = savedHolons;
                response.IsError = errors.Any();
                response.Message = errors.Any() ? string.Join("; ", errors) : $"Successfully saved {savedHolons.Count} holons to Polkadot";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in SaveHolonsAsync: {ex.Message}", ex);
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id)
        {
            return DeleteHolonAsync(id).Result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var response = new OASISResult<IHolon>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Polkadot provider: {activateResult.Message}");
                        return response;
                    }
                }

                // First load the holon to return it
                var loadResult = await LoadHolonAsync(id, false, true, 0, false, false, 0);
                if (loadResult.IsError || loadResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref response, $"Holon with ID {id} not found");
                    return response;
                }

                // Check if smart contract is configured
                if (string.IsNullOrEmpty(_contractAddress))
                {
                    // No contract configured - delegate to ProviderManager as fallback
                    var deleteResult = await HolonManager.Instance.DeleteHolonAsync(id, Guid.Empty, true, NextGenSoftware.OASIS.API.Core.Enums.ProviderType.Default);
                    if (!deleteResult.IsError)
                    {
                        response.Result = loadResult.Result;
                        response.IsError = false;
                        response.Message = "Holon deleted successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, deleteResult.Message);
                    }
                    return response;
                }

                // Delete holon from Polkadot blockchain using smart contract call
                var deleteData = JsonSerializer.Serialize(new { id = id.ToString() });
                var signedTx = await CreatePolkadotTransaction("delete_holon", deleteData);

                var submitRequest = new
                {
                    id = 1,
                    jsonrpc = "2.0",
                    method = "author_submitExtrinsic",
                    @params = new[] { signedTx }
                };

                var jsonContent = JsonSerializer.Serialize(submitRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var rpcResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (rpcResponse.TryGetProperty("result", out var result))
                    {
                        response.Result = loadResult.Result;
                        response.IsError = false;
                        response.Message = $"Holon deleted successfully from Polkadot: {result.GetString()}";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to delete holon from Polkadot - no transaction hash returned");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to submit delete transaction to Polkadot: {httpResponse.StatusCode}");
                }
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
            return DeleteHolonAsync(providerKey).Result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            // First load the holon to get its ID
            var loadResult = await LoadHolonAsync(providerKey, false, true, 0, false, false, 0);
            if (loadResult.IsError || loadResult.Result == null)
            {
                var response = new OASISResult<IHolon>();
                OASISErrorHandling.HandleError(ref response, $"Holon with provider key {providerKey} not found");
                return response;
            }

            // Then delete using the ID
            return await DeleteHolonAsync(loadResult.Result.Id);
        }

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
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Polkadot provider: {activateResult.Message}");
                        return response;
                    }
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

                // Real Polkadot implementation - search through holons and avatars
                var searchResults = new SearchResults();
                var matchingHolons = new List<IHolon>();
                var matchingAvatars = new List<IAvatar>();

                if (!string.IsNullOrEmpty(searchQuery))
                {
                    // Query Polkadot blockchain using smart contract call for search
                    var rpcRequest = new
                    {
                        jsonrpc = "2.0",
                        id = 1,
                        method = "state_call",
                        @params = new[]
                        {
                            "Oasis_search",
                            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{{\"query\":\"{searchQuery}\"}}")),
                            null
                        }
                    };

                    var jsonContent = JsonSerializer.Serialize(rpcRequest);
                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                    var httpResponse = await _httpClient.PostAsync("", content);

                    if (httpResponse.IsSuccessStatusCode)
                    {
                        var responseContent = await httpResponse.Content.ReadAsStringAsync();
                        var rpcResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                        if (rpcResponse.TryGetProperty("result", out var result))
                        {
                            var searchData = JsonSerializer.Deserialize<JsonElement>(result.GetString());
                            
                            // Parse holons from search results
                            if (searchData.TryGetProperty("holons", out var holonsArray) && holonsArray.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var holonElement in holonsArray.EnumerateArray())
                                {
                                    var holon = JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
                                    if (holon != null) matchingHolons.Add(holon);
                                }
                            }
                            
                            // Parse avatars from search results
                            if (searchData.TryGetProperty("avatars", out var avatarsArray) && avatarsArray.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var avatarElement in avatarsArray.EnumerateArray())
                                {
                                    var avatar = JsonSerializer.Deserialize<Avatar>(avatarElement.GetRawText());
                                    if (avatar != null) matchingAvatars.Add(avatar);
                                }
                            }
                        }
                    }
                    else
                    {
                        // Fallback: Load all and filter
                        var allHolonsResult = await LoadAllHolonsAsync(HolonType.All, loadChildren, recursive, maxChildDepth, 0, continueOnError, false, version);
                        if (!allHolonsResult.IsError && allHolonsResult.Result != null)
                        {
                            foreach (var holon in allHolonsResult.Result)
                            {
                                if (holon.Name?.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) == true ||
                                    holon.Description?.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) == true)
                                {
                                    matchingHolons.Add(holon);
                                }
                            }
                        }

                        var allAvatarsResult = await LoadAllAvatarsAsync(version);
                        if (!allAvatarsResult.IsError && allAvatarsResult.Result != null)
                        {
                            foreach (var avatar in allAvatarsResult.Result)
                            {
                                if (avatar.Username?.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) == true ||
                                    avatar.Email?.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) == true ||
                                    $"{avatar.FirstName} {avatar.LastName}".Trim().Contains(searchQuery, StringComparison.OrdinalIgnoreCase))
                                {
                                    matchingAvatars.Add(avatar);
                                }
                            }
                        }
                    }
                }

                searchResults.SearchResultHolons = matchingHolons;
                searchResults.SearchResultAvatars = matchingAvatars;
                searchResults.NumberOfResults = matchingHolons.Count + matchingAvatars.Count;

                response.Result = searchResults;
                response.IsError = false;
                response.Message = $"Search completed: Found {matchingHolons.Count} holons and {matchingAvatars.Count} avatars";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in SearchAsync: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid id, int maxChildDepth = 0)
        {
            return ExportAllDataForAvatarByIdAsync(id, maxChildDepth).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid id, int maxChildDepth = 0)
        {
            // Export all holons for avatar - load holons for parent using avatar ID
            return await LoadHolonsForParentAsync(id, HolonType.All, true, true, maxChildDepth, 0, true, false, 0);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string username, int maxChildDepth = 0)
        {
            return ExportAllDataForAvatarByUsernameAsync(username, maxChildDepth).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string username, int maxChildDepth = 0)
        {
            // First load the avatar to get its ID
            var avatarResult = await LoadAvatarByUsernameAsync(username, 0);
            if (avatarResult.IsError || avatarResult.Result == null)
            {
                var response = new OASISResult<IEnumerable<IHolon>>();
                OASISErrorHandling.HandleError(ref response, $"Avatar with username {username} not found");
                return response;
            }

            // Then export all data using the avatar ID
            return await ExportAllDataForAvatarByIdAsync(avatarResult.Result.Id, maxChildDepth);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string email, int maxChildDepth = 0)
        {
            return ExportAllDataForAvatarByEmailAsync(email, maxChildDepth).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string email, int maxChildDepth = 0)
        {
            // First load the avatar to get its ID
            var avatarResult = await LoadAvatarByEmailAsync(email, 0);
            if (avatarResult.IsError || avatarResult.Result == null)
            {
                var response = new OASISResult<IEnumerable<IHolon>>();
                OASISErrorHandling.HandleError(ref response, $"Avatar with email {email} not found");
                return response;
            }

            // Then export all data using the avatar ID
            return await ExportAllDataForAvatarByIdAsync(avatarResult.Result.Id, maxChildDepth);
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
        {
            return ImportAsync(holons).Result;
        }

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var response = new OASISResult<bool>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Polkadot provider: {activateResult.Message}");
                        return response;
                    }
                }

                if (holons == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Holons cannot be null");
                    return response;
                }

                // Import holons using SaveHolonsAsync
                var saveResult = await SaveHolonsAsync(holons, true, true, 0, 0, true, false);
                response.Result = !saveResult.IsError;
                response.IsError = saveResult.IsError;
                response.Message = saveResult.Message;
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in ImportAsync: {ex.Message}");
            }
            return response;
        }


        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatar)
        {
            var response = new OASISResult<IAvatarDetail>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Polkadot provider: {activateResult.Message}");
                        return response;
                    }
                }

                if (avatar == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Avatar detail cannot be null");
                    return response;
                }

                // Get wallet for the avatar
                var walletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(avatar.Id, Core.Enums.ProviderType.PolkadotOASIS);
                if (walletResult.IsError || walletResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Could not retrieve wallet address for avatar");
                    return response;
                }

                // Save avatar detail to Polkadot smart contract
                var avatarDetailJson = ConvertAvatarDetailToPolkadot(avatar);
                var txHash = await CreatePolkadotTransaction("Oasis_saveAvatarDetail", avatarDetailJson);
                
                if (!string.IsNullOrEmpty(txHash))
                {
                    response.Result = avatar;
                    response.IsError = false;
                    response.IsSaved = true;
                    response.Message = $"Avatar detail saved successfully to Polkadot: {txHash}";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, "Failed to save avatar detail to Polkadot");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in SaveAvatarDetailAsync: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            // First load the avatar to get its ID
            var avatarResult = await LoadAvatarByProviderKeyAsync(providerKey);
            if (avatarResult.IsError || avatarResult.Result == null)
            {
                var response = new OASISResult<bool>();
                OASISErrorHandling.HandleError(ref response, $"Avatar with provider key {providerKey} not found");
                return response;
            }

            // Then delete using the avatar ID
            return await DeleteAvatarAsync(avatarResult.Result.Id, softDelete);
        }

    }
}
