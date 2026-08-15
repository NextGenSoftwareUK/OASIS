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
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using System.Text.Json.Serialization;
using static NextGenSoftware.Utilities.KeyHelper;

namespace NextGenSoftware.OASIS.API.Providers.SuiOASIS
{
    public partial class SuiOASIS
    {
        public void Dispose()
        {
            _httpClient?.Dispose();
        }


        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            // First load the avatar to get its ID
            var avatarResult = await LoadAvatarByUsernameAsync(avatarUsername, 0);
            if (avatarResult.IsError || avatarResult.Result == null)
            {
                var response = new OASISResult<bool>();
                OASISErrorHandling.HandleError(ref response, $"Avatar with username {avatarUsername} not found");
                return response;
            }

            // Then delete using the avatar ID
            return await DeleteAvatarAsync(avatarResult.Result.Id, softDelete);
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
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Sui provider: {activateResult.Message}");
                        return response;
                    }
                }

                if (avatar == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Avatar detail cannot be null");
                    return response;
                }

                // Load the avatar first to get wallet
                var avatarResult = await LoadAvatarAsync(avatar.Id, 0);
                if (avatarResult.IsError || avatarResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref response, $"Avatar with ID {avatar.Id} not found");
                    return response;
                }

                // Get wallet for the avatar
                var walletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(avatar.Id, Core.Enums.ProviderType.SuiOASIS);
                if (walletResult.IsError || walletResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Could not retrieve wallet address for avatar");
                    return response;
                }

                // Serialize avatar detail to JSON
                string avatarDetailInfo = JsonSerializer.Serialize(avatar);
                string avatarDetailId = avatar.Id.ToString();

                // Use Sui Move call to store avatar detail data
                var moveCallRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "sui_moveCall",
                    @params = new object[]
                    {
                        walletResult.Result.WalletAddress,
                        "0x2",
                        "object",
                        "create",
                        new object[] { },
                        new object[]
                        {
                            $"avatar_detail_{avatarDetailId}",
                            avatarDetailInfo
                        },
                        Guid.NewGuid().ToString()
                    }
                };

                var jsonContent = JsonSerializer.Serialize(moveCallRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var rpcResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (rpcResponse.TryGetProperty("result", out var result))
                    {
                        response.Result = avatar;
                        response.IsError = false;
                        response.IsSaved = true;
                        response.Message = $"Avatar detail saved successfully to Sui: {result.GetString()}";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to save avatar detail to Sui - no transaction hash returned");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to save avatar detail to Sui: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in SaveAvatarDetailAsync: {ex.Message}");
            }
            return response;
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
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Sui provider: {activateResult.Message}");
                        return response;
                    }
                }

                if (searchParams == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Search parameters cannot be null");
                    return response;
                }

                var searchResults = new SearchResults();
                var matchingHolons = new List<IHolon>();
                var matchingAvatars = new List<IAvatar>();

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

                if (!string.IsNullOrEmpty(searchQuery))
                {
                    // Query Sui for objects matching search query
                    var rpcRequest = new
                    {
                        jsonrpc = "2.0",
                        id = 1,
                        method = "sui_queryObjects",
                        @params = new object[]
                        {
                            new { StructType = "Object" },
                            new { DataType = "MoveObject", Query = searchQuery }
                        }
                    };

                    var jsonContent = JsonSerializer.Serialize(rpcRequest);
                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                    var httpResponse = await _httpClient.PostAsync("", content);

                    if (httpResponse.IsSuccessStatusCode)
                    {
                        var responseContent = await httpResponse.Content.ReadAsStringAsync();
                        var rpcResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                        if (rpcResponse.TryGetProperty("result", out var result) && result.TryGetProperty("data", out var dataArray))
                        {
                            foreach (var item in dataArray.EnumerateArray())
                            {
                                var objectId = item.TryGetProperty("objectId", out var objId) ? objId.GetString() : null;
                                var objectType = item.TryGetProperty("data", out var objData) && objData.TryGetProperty("type", out var type) ? type.GetString() : null;

                                if (!string.IsNullOrEmpty(objectId))
                                {
                                    // Try to load as holon or avatar based on type
                                    if (objectType?.Contains("Holon") == true || objectType?.Contains("holon") == true)
                                    {
                                        var holonResult = await LoadHolonAsync(objectId, loadChildren, continueOnError, maxChildDepth > 0 ? maxChildDepth - 1 : 0, recursive, true, maxChildDepth);
                                        if (!holonResult.IsError && holonResult.Result != null)
                                        {
                                            matchingHolons.Add(holonResult.Result);
                                        }
                                    }
                                    else if (objectType?.Contains("Avatar") == true || objectType?.Contains("avatar") == true)
                                    {
                                        var avatarResult = await LoadAvatarByProviderKeyAsync(objectId, version);
                                        if (!avatarResult.IsError && avatarResult.Result != null)
                                        {
                                            matchingAvatars.Add(avatarResult.Result);
                                        }
                                    }
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

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var response = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Sui provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Load avatar details as separate entities (do not build from Avatar)
                var allAvatarsResult = await LoadAllAvatarsAsync(version);
                if (!allAvatarsResult.IsError && allAvatarsResult.Result != null)
                {
                    var avatarDetails = new List<IAvatarDetail>();
                    foreach (var avatar in allAvatarsResult.Result)
                    {
                        var detailResult = await LoadAvatarDetailAsync(avatar.Id, version);
                        if (!detailResult.IsError && detailResult.Result != null)
                            avatarDetails.Add(detailResult.Result);
                    }
                    response.Result = avatarDetails;
                    response.IsError = false;
                    response.Message = $"Loaded {avatarDetails.Count} avatar details from Sui successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, allAvatarsResult.Message ?? "Failed to load avatars for avatar details");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadAllAvatarDetailsAsync: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool continueOnError = true, int maxChildren = 50, bool recurseChildren = true, bool loadDetail = true, int maxDepth = 0)
        {
            // Load holon by ID - first need to find the Sui object ID from the GUID
            // For now, delegate to LoadHolonAsync with provider key lookup
            // In a real implementation, you'd maintain a mapping of GUID to Sui object IDs
            var response = new OASISResult<IHolon>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = ActivateProviderAsync().Result;
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Sui provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Query Sui for holon by ID using sui_queryObjects
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "sui_queryObjects",
                    @params = new object[]
                    {
                        new { StructType = "Holon" },
                        new { DataType = "MoveObject", ObjectId = id.ToString() }
                    }
                };

                var jsonContent = JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = _httpClient.PostAsync("", content).Result;

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = httpResponse.Content.ReadAsStringAsync().Result;
                    var rpcResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (rpcResponse.TryGetProperty("result", out var result) && result.TryGetProperty("data", out var dataArray) && dataArray.GetArrayLength() > 0)
                    {
                        var firstObject = dataArray[0];
                        var objectId = firstObject.TryGetProperty("objectId", out var objId) ? objId.GetString() : null;
                        
                        if (!string.IsNullOrEmpty(objectId))
                        {
                            var loadResult = LoadHolonAsync(objectId, loadChildren, continueOnError, maxChildren, recurseChildren, loadDetail, maxDepth).Result;
                            response.Result = loadResult.Result;
                            response.IsError = loadResult.IsError;
                            response.Message = loadResult.Message;
                        }
                        else
                        {
                            OASISErrorHandling.HandleError(ref response, "Holon not found on Sui blockchain");
                        }
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Holon not found on Sui blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to query holon from Sui: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadHolon: {ex.Message}");
            }
            return response;
        }

        // Add more missing methods
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Sui provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Query Sui for holons with parent matching providerKey
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "sui_queryObjects",
                    @params = new object[]
                    {
                        new { StructType = "Holon" },
                        new { DataType = "MoveObject", ParentId = providerKey }
                    }
                };

                var jsonContent = JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var rpcResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (rpcResponse.TryGetProperty("result", out var result) && result.TryGetProperty("data", out var dataArray))
                    {
                        var holons = new List<IHolon>();
                        foreach (var item in dataArray.EnumerateArray())
                        {
                            var objectId = item.TryGetProperty("objectId", out var objId) ? objId.GetString() : null;
                            if (!string.IsNullOrEmpty(objectId))
                            {
                                var holonResult = await LoadHolonAsync(objectId, loadChildren, continueOnError, maxChildDepth > 0 ? maxChildDepth - 1 : 0, recursive, true, maxChildDepth);
                                if (!holonResult.IsError && holonResult.Result != null)
                                {
                                    if (type == HolonType.All || holonResult.Result.HolonType == type)
                                    {
                                        holons.Add(holonResult.Result);
                                    }
                                }
                            }
                        }

                        response.Result = holons;
                        response.IsError = false;
                        response.Message = $"Loaded {holons.Count} holons for parent from Sui blockchain";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "No holons found for parent on Sui blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load holons for parent from Sui: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadHolonsForParentAsync: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id)
        {
            return DeleteHolonAsync(id).Result;
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
        {
            // First load the holon to get its ID, then delete
            var loadResult = LoadHolonAsync(providerKey, false, true, 0, false, false, 0).Result;
            if (loadResult.IsError || loadResult.Result == null)
            {
                var response = new OASISResult<IHolon>();
                OASISErrorHandling.HandleError(ref response, $"Holon with provider key {providerKey} not found");
                return response;
            }

            // Delete using the holon's ID
            return DeleteHolon(loadResult.Result.Id);
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        {
            return DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var response = new OASISResult<IAvatarDetail>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Sui provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Load avatar first, then create avatar detail from it
                var avatarResult = await LoadAvatarAsync(id, version);
                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    var avatarDetail = new AvatarDetail
                    {
                        Id = avatarResult.Result.Id,
                        Username = avatarResult.Result.Username,
                        Email = avatarResult.Result.Email,
                        CreatedDate = avatarResult.Result.CreatedDate,
                        ModifiedDate = avatarResult.Result.ModifiedDate
                    };
                    response.Result = avatarDetail;
                    response.IsError = false;
                    response.Message = "Avatar detail loaded from Sui successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, avatarResult.Message ?? "Avatar not found for detail load");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadAvatarDetailAsync: {ex.Message}");
            }
            return response;
        }

    }
}
