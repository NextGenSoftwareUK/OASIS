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
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
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

                // Query Sui for all holons
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "sui_queryObjects",
                    @params = new object[]
                    {
                        new { StructType = "Holon" },
                        new { DataType = "MoveObject" }
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
                                else if (!continueOnError)
                                {
                                    OASISErrorHandling.HandleError(ref response, $"Failed to load holon {objectId}: {holonResult.Message}");
                                    return response;
                                }
                            }
                        }

                        response.Result = holons;
                        response.IsError = false;
                        response.Message = $"Loaded {holons.Count} holons from Sui blockchain";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "No holons found on Sui blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load holons from Sui: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadAllHolonsAsync: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool continueOnError = true, int maxChildren = 50, bool recurseChildren = true, bool loadDetail = true, int maxDepth = 0)
        {
            var response = new OASISResult<IHolon>();
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
                var httpResponse = await _httpClient.PostAsync("", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var rpcResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (rpcResponse.TryGetProperty("result", out var result) && result.TryGetProperty("data", out var dataArray) && dataArray.GetArrayLength() > 0)
                    {
                        var firstObject = dataArray[0];
                        var objectId = firstObject.TryGetProperty("objectId", out var objId) ? objId.GetString() : null;
                        
                        if (!string.IsNullOrEmpty(objectId))
                        {
                            var loadResult = await LoadHolonAsync(objectId, loadChildren, continueOnError, maxChildren, recurseChildren, loadDetail, maxDepth);
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
                OASISErrorHandling.HandleError(ref response, $"Error in LoadHolonAsync: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var response = new OASISResult<IHolon>();
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
                if (_httpClient == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Sui HTTP client is not initialized");
                    return response;
                }

                if (holon == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Holon cannot be null");
                    return response;
                }

                // Get wallet for the holon (use avatar's wallet if holon has CreatedByAvatarId)
                Guid avatarId = holon.CreatedByAvatarId != Guid.Empty ? holon.CreatedByAvatarId : holon.Id;
                var walletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(avatarId, Core.Enums.ProviderType.SuiOASIS);
                if (walletResult.IsError || walletResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Could not retrieve wallet address for holon");
                    return response;
                }

                // Serialize holon to JSON
                string holonInfo = JsonSerializer.Serialize(holon);
                string holonId = holon.Id.ToString();

                // Use Sui Move call to store holon data
                if (string.IsNullOrEmpty(_contractAddress))
                {
                    // No contract configured - use Sui object storage
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
                                holonId,
                                holonInfo
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
                            if (holon.ProviderUniqueStorageKey == null)
                                holon.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
                            holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.SuiOASIS] = result.GetString() ?? string.Empty;

                            response.Result = holon;
                            response.IsError = false;
                            response.IsSaved = true;
                            response.Message = $"Holon saved successfully to Sui: {result.GetString()}";

                            // Handle children if requested
                            if (saveChildren && holon.Children != null && holon.Children.Any())
                            {
                                var childResults = new List<OASISResult<IHolon>>();
                                foreach (var child in holon.Children)
                                {
                                    var childResult = await SaveHolonAsync(child, saveChildren, recursive, maxChildDepth - 1, continueOnError, saveChildrenOnProvider);
                                    childResults.Add(childResult);
                                    
                                    if (!continueOnError && childResult.IsError)
                                    {
                                        OASISErrorHandling.HandleError(ref response, $"Failed to save child holon {child.Id}: {childResult.Message}");
                                        return response;
                                    }
                                }
                            }
                        }
                        else
                        {
                            OASISErrorHandling.HandleError(ref response, "Failed to save holon to Sui - no transaction hash returned");
                        }
                    }
                    else
                    {
                        var errorContent = await httpResponse.Content.ReadAsStringAsync();
                        OASISErrorHandling.HandleError(ref response, $"Failed to save holon to Sui: {httpResponse.StatusCode} - {errorContent}");
                    }
                }
                else
                {
                    // Use configured smart contract
                    var moveCallRequest = new
                    {
                        jsonrpc = "2.0",
                        id = 1,
                        method = "sui_moveCall",
                        @params = new object[]
                        {
                            walletResult.Result.WalletAddress,
                            _contractAddress,
                            "oasis",
                            "create_holon",
                            new object[] { },
                            new object[]
                            {
                                holonId,
                                holonInfo
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
                            if (holon.ProviderUniqueStorageKey == null)
                                holon.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
                            holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.SuiOASIS] = result.GetString() ?? string.Empty;

                            response.Result = holon;
                            response.IsError = false;
                            response.IsSaved = true;
                            response.Message = $"Holon saved successfully to Sui contract: {result.GetString()}";

                            // Handle children if requested
                            if (saveChildren && holon.Children != null && holon.Children.Any())
                            {
                                var childResults = new List<OASISResult<IHolon>>();
                                foreach (var child in holon.Children)
                                {
                                    var childResult = await SaveHolonAsync(child, saveChildren, recursive, maxChildDepth - 1, continueOnError, saveChildrenOnProvider);
                                    childResults.Add(childResult);
                                    
                                    if (!continueOnError && childResult.IsError)
                                    {
                                        OASISErrorHandling.HandleError(ref response, $"Failed to save child holon {child.Id}: {childResult.Message}");
                                        return response;
                                    }
                                }
                            }
                        }
                        else
                        {
                            OASISErrorHandling.HandleError(ref response, "Failed to save holon to Sui contract - no transaction hash returned");
                        }
                    }
                    else
                    {
                        var errorContent = await httpResponse.Content.ReadAsStringAsync();
                        OASISErrorHandling.HandleError(ref response, $"Failed to save holon to Sui contract: {httpResponse.StatusCode} - {errorContent}");
                    }
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in SaveHolonAsync: {ex.Message}", ex);
            }
            return response;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var response = new OASISResult<IAvatar>();
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

                // Query Sui for avatar by username
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "sui_queryObjects",
                    @params = new object[]
                    {
                        new { StructType = "Avatar" },
                        new { DataType = "MoveObject", Username = avatarUsername }
                    }
                };

                var jsonContent = JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var rpcResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (rpcResponse.TryGetProperty("result", out var result) && result.TryGetProperty("data", out var dataArray) && dataArray.GetArrayLength() > 0)
                    {
                        var firstObject = dataArray[0];
                        var objectId = firstObject.TryGetProperty("objectId", out var objId) ? objId.GetString() : null;
                        
                        if (!string.IsNullOrEmpty(objectId))
                        {
                            // Load avatar by provider key
                            var avatarResult = await LoadAvatarByProviderKeyAsync(objectId, version);
                            if (!avatarResult.IsError && avatarResult.Result != null)
                            {
                                response.Result = avatarResult.Result;
                                response.IsError = false;
                                response.Message = "Avatar loaded from Sui by username successfully";
                            }
                            else
                            {
                                OASISErrorHandling.HandleError(ref response, "Failed to load avatar from Sui by username");
                            }
                        }
                        else
                        {
                            OASISErrorHandling.HandleError(ref response, "Avatar not found by username on Sui blockchain");
                        }
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Avatar not found by username on Sui blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar from Sui by username: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadAvatarByUsernameAsync: {ex.Message}");
            }
            return response;
        }

    }
}
