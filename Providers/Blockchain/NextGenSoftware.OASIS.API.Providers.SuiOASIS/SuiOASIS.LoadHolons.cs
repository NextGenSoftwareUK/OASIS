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
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
        {
            return LoadAvatarDetailByEmailAsync(avatarEmail, version).Result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool continueOnError = true, int maxChildren = 50, bool recurseChildren = true, bool loadDetail = true, int maxDepth = 0)
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

                // Load holon from Sui blockchain by provider key (object ID)
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
                        var holonJson = data.TryGetProperty("content", out var contentElement) && contentElement.TryGetProperty("fields", out var fields) 
                            ? fields.GetRawText() 
                            : data.GetRawText();
                        
                        var holons = ParseSuiToHolons(holonJson);
                        var holon = holons?.FirstOrDefault();
                        
                        if (holon != null)
                        {
                            response.Result = holon;
                            response.IsError = false;
                            response.Message = "Holon loaded successfully from Sui blockchain by provider key";
                        }
                        else
                        {
                            OASISErrorHandling.HandleError(ref response, "Failed to parse holon from Sui response");
                        }
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Holon not found on Sui blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load holon from Sui: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadHolonAsync: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        {
            // First load the avatar by email, then create avatar detail
            var avatarResult = await LoadAvatarByEmailAsync(avatarEmail, version);
            if (!avatarResult.IsError && avatarResult.Result != null)
            {
                var response = new OASISResult<IAvatarDetail>();
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
                response.Message = "Avatar detail loaded from Sui by email successfully";
                return response;
            }
            else
            {
                var response = new OASISResult<IAvatarDetail>();
                OASISErrorHandling.HandleError(ref response, avatarResult.Message ?? "Avatar not found by email for detail load");
                return response;
            }
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmail, int version = 0)
        {
            return ExportAllDataForAvatarByEmailAsync(avatarEmail, version).Result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
        {
            return DeleteAvatarByEmailAsync(avatarEmail, softDelete).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        {
            // First load the avatar by username, then create avatar detail
            var avatarResult = await LoadAvatarByUsernameAsync(avatarUsername, version);
            if (!avatarResult.IsError && avatarResult.Result != null)
            {
                var response = new OASISResult<IAvatarDetail>();
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
                response.Message = "Avatar detail loaded from Sui by username successfully";
                return response;
            }
            else
            {
                var response = new OASISResult<IAvatarDetail>();
                OASISErrorHandling.HandleError(ref response, avatarResult.Message ?? "Avatar not found by username for detail load");
                return response;
            }
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var response = new OASISResult<bool>();
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

                // Load the avatar first to get its Sui object ID
                var avatarResult = await LoadAvatarAsync(id, 0);
                if (avatarResult.IsError || avatarResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref response, $"Avatar with ID {id} not found");
                    return response;
                }

                // Get the Sui object ID from provider key
                var providerKey = avatarResult.Result.ProviderUniqueStorageKey?.TryGetValue(Core.Enums.ProviderType.SuiOASIS, out var suiKey) == true ? suiKey : null;
                if (string.IsNullOrEmpty(providerKey))
                {
                    OASISErrorHandling.HandleError(ref response, "Avatar does not have a Sui provider key (object ID)");
                    return response;
                }

                // Get wallet for the avatar
                var walletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(id, Core.Enums.ProviderType.SuiOASIS);
                if (walletResult.IsError || walletResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Could not retrieve wallet address for avatar");
                    return response;
                }

                if (softDelete)
                {
                    // For soft delete, set DeletedDate (IsDeleted is derived from it)
                    avatarResult.Result.DeletedDate = DateTime.UtcNow;
                    var saveResult = await SaveAvatarAsync(avatarResult.Result);
                    response.Result = !saveResult.IsError;
                    response.IsError = saveResult.IsError;
                    response.Message = saveResult.Message;
                }
                else
                {
                    // For hard delete, transfer object to burn address
                    var moveCallRequest = new
                    {
                        jsonrpc = "2.0",
                        id = 1,
                        method = "sui_transferObject",
                        @params = new object[]
                        {
                            walletResult.Result.WalletAddress,
                            providerKey,
                            "0x" + new string('0', 64), // Burn address
                            Guid.NewGuid().ToString()
                        }
                    };

                    var jsonContent = JsonSerializer.Serialize(moveCallRequest);
                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                    var httpResponse = await _httpClient.PostAsync("", content);

                    response.Result = httpResponse.IsSuccessStatusCode;
                    response.IsError = !httpResponse.IsSuccessStatusCode;
                    response.Message = httpResponse.IsSuccessStatusCode ? "Avatar deleted successfully from Sui blockchain" : $"Failed to delete avatar: {httpResponse.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in DeleteAvatarAsync: {ex.Message}");
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
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Sui provider: {activateResult.Message}");
                        return response;
                    }
                }

                // First load the holon to return it
                var loadResult = LoadHolon(id, false, true, 0, false, false, 0);
                if (loadResult.IsError || loadResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref response, $"Holon with ID {id} not found");
                    return response;
                }

                // Get the Sui object ID from provider key
                var providerKey = loadResult.Result.ProviderUniqueStorageKey?.TryGetValue(Core.Enums.ProviderType.SuiOASIS, out var suiKey) == true ? suiKey : null;
                if (string.IsNullOrEmpty(providerKey))
                {
                    OASISErrorHandling.HandleError(ref response, "Holon does not have a Sui provider key (object ID)");
                    return response;
                }

                // Delete holon from Sui using sui_deleteObject or transfer to a burn address
                // Sui doesn't have direct delete - we transfer to a burn address or mark as deleted
                var walletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(loadResult.Result.CreatedByAvatarId != Guid.Empty ? loadResult.Result.CreatedByAvatarId : id, Core.Enums.ProviderType.SuiOASIS);
                if (walletResult.IsError || walletResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Could not retrieve wallet address for holon deletion");
                    return response;
                }

                // Transfer object to burn address (0x000...000) to effectively delete it
                var moveCallRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "sui_transferObject",
                    @params = new object[]
                    {
                        walletResult.Result.WalletAddress,
                        providerKey,
                        "0x" + new string('0', 64), // Burn address
                        Guid.NewGuid().ToString()
                    }
                };

                var jsonContent = JsonSerializer.Serialize(moveCallRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    response.Result = loadResult.Result;
                    response.IsError = false;
                    response.Message = "Holon deleted successfully from Sui blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to delete holon from Sui: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in DeleteHolonAsync: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
        {
            return LoadAvatarByEmailAsync(avatarEmail, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
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

                // Query Sui for holons matching metadata
                // Build query filter from metadata
                var metadataJson = JsonSerializer.Serialize(metaKeyValuePairs);
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "sui_queryObjects",
                    @params = new object[]
                    {
                        new { StructType = "Holon" },
                        new { DataType = "MoveObject", Metadata = metadataJson, MatchMode = metaKeyValuePairMatchMode.ToString() }
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
                        response.Message = $"Loaded {holons.Count} holons by metadata from Sui blockchain";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "No holons found with matching metadata on Sui blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load holons by metadata from Sui: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadHolonsByMetaDataAsync: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaData, string value, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaData, value, holonType, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            return LoadAllAvatarDetailsAsync(version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            // Export all holons - delegate to LoadAllHolonsAsync
            return await LoadAllHolonsAsync(HolonType.All, true, true, 0, 0, true, false, version);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
        {
            return LoadAvatarByProviderKeyAsync(providerKey, version).Result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid id, int version = 0)
        {
            return ExportAllDataForAvatarByIdAsync(id, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var response = new OASISResult<IEnumerable<IAvatar>>();
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

                // Query Sui for all avatars
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "sui_queryObjects",
                    @params = new object[]
                    {
                        new { StructType = "Avatar" },
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
                        var avatars = new List<IAvatar>();
                        foreach (var item in dataArray.EnumerateArray())
                        {
                            var objectId = item.TryGetProperty("objectId", out var objId) ? objId.GetString() : null;
                            if (!string.IsNullOrEmpty(objectId))
                            {
                                // Try to load avatar by provider key
                                var avatarResult = await LoadAvatarByProviderKeyAsync(objectId, version);
                                if (!avatarResult.IsError && avatarResult.Result != null)
                                {
                                    avatars.Add(avatarResult.Result);
                                }
                            }
                        }

                        response.Result = avatars;
                        response.IsError = false;
                        response.Message = $"Loaded {avatars.Count} avatars from Sui blockchain";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "No avatars found on Sui blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatars from Sui: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadAllAvatarsAsync: {ex.Message}");
            }
            return response;
        }

    }
}
