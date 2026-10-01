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
        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            // First load the holon to return it, then delete
            var loadResult = await LoadHolonAsync(providerKey, false, true, 0, false, false, 0);
            if (loadResult.IsError || loadResult.Result == null)
            {
                var response = new OASISResult<IHolon>();
                OASISErrorHandling.HandleError(ref response, $"Holon with provider key {providerKey} not found");
                return response;
            }

            // Delete using the holon's ID
            return await DeleteHolonAsync(loadResult.Result.Id);
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
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

                // Query Sui for holons with parent matching the ID
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "sui_queryObjects",
                    @params = new object[]
                    {
                        new { StructType = "Holon" },
                        new { DataType = "MoveObject", ParentId = id.ToString() }
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

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
        {
            return ExportAllAsync(version).Result;
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
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Sui provider: {activateResult.Message}");
                        return response;
                    }
                }

                if (holons == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Holons cannot be null");
                    return response;
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
                        OASISErrorHandling.HandleError(ref response, $"Failed to save holon {holon.Id}: {saveResult.Message}");
                        return response;
                    }
                }

                response.Result = savedHolons;
                response.IsError = false;
                response.Message = $"Saved {savedHolons.Count} holons to Sui blockchain";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in SaveHolonsAsync: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarDetailByUsernameAsync(avatarUsername, version).Result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            // Import holons by saving them in batch
            var saveResult = await SaveHolonsAsync(holons, true, true, 0, 0, true, false);
            var response = new OASISResult<bool>();
            if (!saveResult.IsError && saveResult.Result != null)
            {
                response.Result = true;
                response.IsError = false;
                response.Message = $"Imported {saveResult.Result.Count()} holons to Sui blockchain";
            }
            else
            {
                OASISErrorHandling.HandleError(ref response, saveResult.Message ?? "Failed to import holons to Sui");
            }
            return response;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            // First load the avatar to get its ID
            var avatarResult = await LoadAvatarByUsernameAsync(avatarUsername, version);
            if (avatarResult.IsError || avatarResult.Result == null)
            {
                var response = new OASISResult<IEnumerable<IHolon>>();
                OASISErrorHandling.HandleError(ref response, $"Avatar with username {avatarUsername} not found");
                return response;
            }

            // Then export all data using the avatar ID
            return await ExportAllDataForAvatarByIdAsync(avatarResult.Result.Id, version);
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            // First load the avatar to get its ID
            var avatarResult = await LoadAvatarByEmailAsync(avatarEmail, version);
            if (avatarResult.IsError || avatarResult.Result == null)
            {
                var response = new OASISResult<IEnumerable<IHolon>>();
                OASISErrorHandling.HandleError(ref response, $"Avatar with email {avatarEmail} not found");
                return response;
            }

            // Then export all data using the avatar ID
            return await ExportAllDataForAvatarByIdAsync(avatarResult.Result.Id, version);
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool continueOnError = true, int maxChildren = 50, bool recurseChildren = true, bool loadDetail = true, int maxDepth = 0)
        {
            return LoadHolonAsync(providerKey, loadChildren, continueOnError, maxChildren, recurseChildren, loadDetail, maxDepth).Result;
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

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
        {
            return SaveAvatarAsync(avatar).Result;
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        {
            return DeleteAvatarAsync(providerKey, softDelete).Result;
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
        {
            return ImportAsync(holons).Result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatar)
        {
            return SaveAvatarDetailAsync(avatar).Result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            return LoadAllAvatarsAsync(version).Result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            // First load the avatar to get its ID
            var avatarResult = await LoadAvatarByEmailAsync(avatarEmail);
            if (avatarResult.IsError || avatarResult.Result == null)
            {
                var response = new OASISResult<bool>();
                OASISErrorHandling.HandleError(ref response, $"Avatar with email {avatarEmail} not found");
                return response;
            }

            // Then delete using the avatar ID
            return await DeleteAvatarAsync(avatarResult.Result.Id, softDelete);
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        {
            return DeleteAvatarAsync(id, softDelete).Result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
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

                // Query Sui for avatar by email
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "sui_queryObjects",
                    @params = new object[]
                    {
                        new { StructType = "Avatar" },
                        new { DataType = "MoveObject", Email = avatarEmail }
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
                                response.Message = "Avatar loaded from Sui by email successfully";
                            }
                            else
                            {
                                OASISErrorHandling.HandleError(ref response, "Failed to load avatar from Sui by email");
                            }
                        }
                        else
                        {
                            OASISErrorHandling.HandleError(ref response, "Avatar not found by email on Sui blockchain");
                        }
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Avatar not found by email on Sui blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar from Sui by email: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadAvatarByEmailAsync: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaData, string value, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            // Convert single metadata key-value pair to dictionary and delegate to the dictionary version
            var metaKeyValuePairs = new Dictionary<string, string> { { metaData, value } };
            return await LoadHolonsByMetaDataAsync(metaKeyValuePairs, MetaKeyValuePairMatchMode.All, holonType, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
        {
            return ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            return LoadAvatarDetailAsync(id, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
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

                // Load avatar from Sui blockchain by provider key (object ID)
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "sui_getObject",
                    @params = new object[] { providerKey, new { showType = true, showOwner = true, showPreviousTransaction = true, showDisplay = true, showContent = true, showBcs = false, showStorageRebate = false } }
                };

                var jsonContent = JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var rpcResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (rpcResponse.TryGetProperty("result", out var result) && result.TryGetProperty("data", out var data))
                    {
                        var avatarJson = data.TryGetProperty("content", out var contentElement) && contentElement.TryGetProperty("fields", out var fields) 
                            ? fields.GetRawText() 
                            : data.GetRawText();
                        
                        var avatar = ParseSuiToAvatar(avatarJson);
                        
                        if (avatar != null)
                        {
                            response.Result = avatar;
                            response.IsError = false;
                            response.Message = "Avatar loaded successfully from Sui blockchain by provider key";
                        }
                        else
                        {
                            OASISErrorHandling.HandleError(ref response, "Failed to parse avatar from Sui response");
                        }
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Avatar not found on Sui blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar from Sui: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadAvatarByProviderKeyAsync: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
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
                if (_httpClient == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Sui HTTP client is not initialized");
                    return response;
                }

                if (avatar == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Avatar cannot be null");
                    return response;
                }

                // Get wallet for the avatar
                var walletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(avatar.Id, Core.Enums.ProviderType.SuiOASIS);
                if (walletResult.IsError || walletResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Could not retrieve wallet address for avatar");
                    return response;
                }

                // Serialize avatar to JSON
                string avatarInfo = JsonSerializer.Serialize(avatar);
                string avatarId = avatar.Id.ToString();

                // Use Sui Move call to store avatar data
                // Check if smart contract is configured
                if (string.IsNullOrEmpty(_contractAddress))
                {
                    // No contract configured - use Sui object storage via Move call
                    // Create a Sui object with avatar data
                    var moveCallRequest = new
                    {
                        jsonrpc = "2.0",
                        id = 1,
                        method = "sui_moveCall",
                        @params = new object[]
                        {
                            walletResult.Result.WalletAddress, // sender
                            "0x2", // package (Sui system package)
                            "object", // module
                            "create", // function
                            new object[] { }, // typeArguments
                            new object[] // arguments
                            {
                                avatarId,
                                avatarInfo
                            },
                            "10000000" // gasBudget (10M MIST = 0.01 SUI)
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
                            // Store transaction hash in provider unique storage key
                            if (avatar.ProviderUniqueStorageKey == null)
                                avatar.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
                            avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.SuiOASIS] = result.GetString() ?? string.Empty;

                            response.Result = avatar;
                            response.IsError = false;
                            response.IsSaved = true;
                            response.Message = $"Avatar saved successfully to Sui: {result.GetString()}";
                        }
                        else
                        {
                            OASISErrorHandling.HandleError(ref response, "Failed to save avatar to Sui - no transaction hash returned");
                        }
                    }
                    else
                    {
                        var errorContent = await httpResponse.Content.ReadAsStringAsync();
                        OASISErrorHandling.HandleError(ref response, $"Failed to save avatar to Sui: {httpResponse.StatusCode} - {errorContent}");
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
                            walletResult.Result.WalletAddress, // sender
                            _contractAddress, // package (contract address)
                            "oasis", // module
                            "create_avatar", // function
                            new object[] { }, // typeArguments
                            new object[] // arguments
                            {
                                avatarId,
                                avatarInfo
                            },
                            "10000000" // gasBudget (10M MIST = 0.01 SUI - reasonable default for Sui transactions)
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
                            if (avatar.ProviderUniqueStorageKey == null)
                                avatar.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
                            avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.SuiOASIS] = result.GetString() ?? string.Empty;

                            response.Result = avatar;
                            response.IsError = false;
                            response.IsSaved = true;
                            response.Message = $"Avatar saved successfully to Sui contract: {result.GetString()}";
                        }
                        else
                        {
                            OASISErrorHandling.HandleError(ref response, "Failed to save avatar to Sui contract - no transaction hash returned");
                        }
                    }
                    else
                    {
                        var errorContent = await httpResponse.Content.ReadAsStringAsync();
                        OASISErrorHandling.HandleError(ref response, $"Failed to save avatar to Sui contract: {httpResponse.StatusCode} - {errorContent}");
                    }
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in SaveAvatarAsync: {ex.Message}", ex);
            }
            return response;
        }

    }
}
