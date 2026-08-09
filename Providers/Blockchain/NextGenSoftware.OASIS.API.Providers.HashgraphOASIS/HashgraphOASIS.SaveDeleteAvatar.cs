using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.Core.Objects;
using System.IO;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS
{
    public partial class HashgraphOASIS
    {
        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
        {
            return SaveAvatarAsync(avatar).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Hashgraph provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (avatarDetail == null)
                {
                    OASISErrorHandling.HandleError(ref result, "AvatarDetail cannot be null");
                    return result;
                }

                // Store AvatarDetail as a Hedera file (memo contains AvatarDetail ID)
                var json = JsonSerializer.Serialize(avatarDetail);
                var fileJson = JsonSerializer.Serialize(new
                {
                    contents = Convert.ToBase64String(Encoding.UTF8.GetBytes(json)),
                    fileMemo = $"OASIS AvatarDetail: {avatarDetail.Id}"
                });
                var fileContent = new StringContent(fileJson, Encoding.UTF8, "application/json");
                var fileResponse = await _httpClient.PostAsync("/api/v1/files", fileContent);

                if (fileResponse.IsSuccessStatusCode)
                {
                    var fileResponseContent = await fileResponse.Content.ReadAsStringAsync();
                    var fileResult = JsonSerializer.Deserialize<JsonElement>(fileResponseContent);
                    if (fileResult.TryGetProperty("fileId", out var fileId))
                    {
                        if (avatarDetail.ProviderUniqueStorageKey != null)
                            avatarDetail.ProviderUniqueStorageKey[ProviderType.Value] = fileId.GetString();

                        result.Result = avatarDetail;
                        result.IsError = false;
                        result.IsSaved = true;
                        result.Message = "AvatarDetail saved to Hedera File Service successfully";
                        return result;
                    }
                }

                OASISErrorHandling.HandleError(ref result, $"Failed to save avatar detail to Hedera File Service: {await fileResponse.Content.ReadAsStringAsync()}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving avatar detail to Hashgraph: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail)
        {
            return SaveAvatarDetailAsync(avatarDetail).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                var loadResult = await LoadAvatarAsync(id);
                if (loadResult.IsError || loadResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, loadResult.Message ?? $"Avatar {id} not found.");
                    return result;
                }

                if (softDelete)
                {
                    loadResult.Result.DeletedDate = DateTime.UtcNow;
                    var saveResult = await SaveAvatarAsync(loadResult.Result);
                    result.Result = !saveResult.IsError;
                    result.IsError = saveResult.IsError;
                    result.Message = saveResult.IsError ? saveResult.Message : "Avatar soft deleted successfully (tombstoned via update).";
                    return result;
                }

                // Permanent deletes require Hedera SDK and appropriate permissions; represent as soft delete if not supported.
                loadResult.Result.DeletedDate = DateTime.UtcNow;
                var saveFallback = await SaveAvatarAsync(loadResult.Result);
                result.Result = !saveFallback.IsError;
                result.IsError = saveFallback.IsError;
                result.Message = saveFallback.IsError ? saveFallback.Message : "Avatar marked deleted (permanent delete requires Hedera SDK).";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        {
            return DeleteAvatarAsync(id, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                var loadResult = await LoadAvatarByProviderKeyAsync(providerKey);
                if (loadResult.IsError || loadResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, loadResult.Message ?? $"Avatar not found for providerKey {providerKey}.");
                    return result;
                }

                loadResult.Result.DeletedDate = DateTime.UtcNow;
                var saveResult = await SaveAvatarAsync(loadResult.Result);
                result.Result = !saveResult.IsError;
                result.IsError = saveResult.IsError;
                result.Message = saveResult.IsError ? saveResult.Message : "Avatar deleted successfully (tombstoned via update).";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by provider key: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        {
            return DeleteAvatarAsync(providerKey, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                var loadResult = await LoadAvatarByEmailAsync(avatarEmail);
                if (loadResult.IsError || loadResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, loadResult.Message ?? $"Avatar not found for email {avatarEmail}.");
                    return result;
                }

                loadResult.Result.DeletedDate = DateTime.UtcNow;
                var saveResult = await SaveAvatarAsync(loadResult.Result);
                result.Result = !saveResult.IsError;
                result.IsError = saveResult.IsError;
                result.Message = saveResult.IsError ? saveResult.Message : "Avatar deleted successfully (tombstoned via update).";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by email: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
        {
            return DeleteAvatarByEmailAsync(avatarEmail, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                var loadResult = await LoadAvatarByUsernameAsync(avatarUsername);
                if (loadResult.IsError || loadResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, loadResult.Message ?? $"Avatar not found for username {avatarUsername}.");
                    return result;
                }

                loadResult.Result.DeletedDate = DateTime.UtcNow;
                var saveResult = await SaveAvatarAsync(loadResult.Result);
                result.Result = !saveResult.IsError;
                result.IsError = saveResult.IsError;
                result.Message = saveResult.IsError ? saveResult.Message : "Avatar deleted successfully (tombstoned via update).";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by username: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        {
            return DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Hashgraph provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load holon from Hedera File Service or Smart Contract
                if (!string.IsNullOrEmpty(_contractAddress))
                {
                    // Query smart contract for holon data
                    var contractQuery = new
                    {
                        contractId = _contractAddress,
                        functionName = "getHolon",
                        parameters = new[] { id.ToString() }
                    };

                    var jsonContent = JsonSerializer.Serialize(contractQuery);
                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                    var contractResponse = await _httpClient.PostAsync("/api/v1/contracts/call", content);

                    if (contractResponse.IsSuccessStatusCode)
                    {
                        var responseContent = await contractResponse.Content.ReadAsStringAsync();
                        var contractResult = JsonSerializer.Deserialize<JsonElement>(responseContent);
                        
                        if (contractResult.TryGetProperty("result", out var resultProp) && resultProp.ValueKind == JsonValueKind.String)
                        {
                            var holonJson = resultProp.GetString();
                            var holon = JsonSerializer.Deserialize<Holon>(holonJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            if (holon != null)
                            {
                                result.Result = holon;
                                result.IsError = false;
                                result.Message = "Holon loaded from Hashgraph smart contract successfully";
                                return result;
                            }
                        }
                    }
                    
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holon from Hashgraph smart contract: {contractResponse.StatusCode}");
                }
                else
                {
                    // Query Hedera File Service for holon data
                    // Use deterministic file ID based on holon ID
                    var fileId = $"0.0.{HashUtility.GetNumericHash(id.ToString())}";
                    var fileResponse = await _httpClient.GetAsync($"/api/v1/files/{fileId}");

                    if (fileResponse.IsSuccessStatusCode)
                    {
                        var fileContent = await fileResponse.Content.ReadAsStringAsync();
                        var fileResult = JsonSerializer.Deserialize<JsonElement>(fileContent);
                        
                        if (fileResult.TryGetProperty("contents", out var contentsProp))
                        {
                            var contentsBase64 = contentsProp.GetString();
                            var contentsBytes = Convert.FromBase64String(contentsBase64);
                            var holonJson = Encoding.UTF8.GetString(contentsBytes);
                            var holon = JsonSerializer.Deserialize<Holon>(holonJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            
                            if (holon != null)
                            {
                                result.Result = holon;
                                result.IsError = false;
                                result.Message = "Holon loaded from Hedera File Service successfully";
                                return result;
                            }
                        }
                    }
                    
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holon from Hedera File Service: {fileResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holon from Hashgraph: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Hashgraph provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load holon from Hedera File Service or Smart Contract by provider key
                if (!string.IsNullOrEmpty(_contractAddress))
                {
                    // Query smart contract for holon data by provider key
                    var contractQuery = new
                    {
                        contractId = _contractAddress,
                        functionName = "getHolonByProviderKey",
                        parameters = new[] { providerKey }
                    };

                    var jsonContent = JsonSerializer.Serialize(contractQuery);
                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                    var contractResponse = await _httpClient.PostAsync("/api/v1/contracts/call", content);

                    if (contractResponse.IsSuccessStatusCode)
                    {
                        var responseContent = await contractResponse.Content.ReadAsStringAsync();
                        var contractResult = JsonSerializer.Deserialize<JsonElement>(responseContent);
                        
                        if (contractResult.TryGetProperty("result", out var resultProp) && resultProp.ValueKind == JsonValueKind.String)
                        {
                            var holonJson = resultProp.GetString();
                            var holon = JsonSerializer.Deserialize<Holon>(holonJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            if (holon != null)
                            {
                                result.Result = holon;
                                result.IsError = false;
                                result.Message = "Holon loaded from Hashgraph smart contract successfully";
                                return result;
                            }
                        }
                    }
                    
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holon from Hashgraph smart contract: {contractResponse.StatusCode}");
                }
                else
                {
                    // Query Hedera File Service for holon data by provider key
                    // Search for files with matching memo
                    var fileResponse = await _httpClient.GetAsync($"/api/v1/files?memo={Uri.EscapeDataString($"OASIS Holon: {providerKey}")}");

                    if (fileResponse.IsSuccessStatusCode)
                    {
                        var fileContent = await fileResponse.Content.ReadAsStringAsync();
                        var fileResult = JsonSerializer.Deserialize<JsonElement>(fileContent);
                        
                        if (fileResult.TryGetProperty("files", out var filesProp) && filesProp.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var file in filesProp.EnumerateArray())
                            {
                                if (file.TryGetProperty("file_id", out var fileIdProp))
                                {
                                    var fileId = fileIdProp.GetString();
                                    var fileContentsResponse = await _httpClient.GetAsync($"/api/v1/files/{fileId}/contents");
                                    
                                    if (fileContentsResponse.IsSuccessStatusCode)
                                    {
                                        var contentsBase64 = await fileContentsResponse.Content.ReadAsStringAsync();
                                        var contentsBytes = Convert.FromBase64String(contentsBase64);
                                        var holonJson = Encoding.UTF8.GetString(contentsBytes);
                                        var holon = JsonSerializer.Deserialize<Holon>(holonJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                                        
                                        if (holon != null && holon.ProviderUniqueStorageKey != null && holon.ProviderUniqueStorageKey.ContainsValue(providerKey))
                                        {
                                            result.Result = holon;
                                            result.IsError = false;
                                            result.Message = "Holon loaded from Hedera File Service successfully";
                                            return result;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holon from Hedera File Service: {fileResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holon from Hashgraph: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        //public override Task<OASISResult<IHolon>> LoadHolonByCustomKeyAsync(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override OASISResult<IHolon> LoadHolonByCustomKey(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override Task<OASISResult<IHolon>> LoadHolonByMetaDataAsync(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override OASISResult<IHolon> LoadHolonByMetaData(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

    }
}
