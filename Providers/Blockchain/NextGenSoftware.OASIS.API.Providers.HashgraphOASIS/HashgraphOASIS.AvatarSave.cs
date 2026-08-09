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
        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();
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

                if (avatar == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar cannot be null");
                    return result;
                }

                // Get wallet for the avatar
                var walletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(avatar.Id, Core.Enums.ProviderType.HashgraphOASIS);
                if (walletResult.IsError || walletResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Could not retrieve wallet address for avatar");
                    return result;
                }

                // Serialize avatar to JSON
                string avatarInfo = JsonSerializer.Serialize(avatar);
                int avatarEntityId = HashUtility.GetNumericHash(avatar.Id.ToString());
                string avatarId = avatar.Id.ToString();

                // Use Hedera File Service to store avatar data via HTTP API
                if (!string.IsNullOrEmpty(_contractAddress))
                {
                    // Smart contract storage - use Hedera Smart Contract Service via REST API
                    var contractData = new
                    {
                        contractId = _contractAddress,
                        functionParameters = new
                        {
                            functionName = "createAvatar",
                            parameters = new[]
                            {
                                new { type = "string", value = avatarId },
                                new { type = "string", value = avatarInfo }
                            }
                        }
                    };

                    var contractJson = JsonSerializer.Serialize(contractData);
                    var contractContent = new StringContent(contractJson, Encoding.UTF8, "application/json");
                    var contractResponse = await _httpClient.PostAsync("/api/v1/contracts/call", contractContent);

                    if (contractResponse.IsSuccessStatusCode)
                    {
                        var contractResponseContent = await contractResponse.Content.ReadAsStringAsync();
                        var contractResult = JsonSerializer.Deserialize<JsonElement>(contractResponseContent);
                        
                        if (contractResult.TryGetProperty("transactionId", out var txId))
                        {
                            avatar.ProviderUniqueStorageKey[ProviderType.Value] = txId.GetString();
                            result.Result = avatar;
                            result.IsError = false;
                            result.IsSaved = true;
                            result.Message = "Avatar saved to Hashgraph smart contract successfully";
                            return result;
                        }
                    }
                    
                    OASISErrorHandling.HandleError(ref result, $"Failed to save avatar to Hashgraph smart contract: {await contractResponse.Content.ReadAsStringAsync()}");
                    return result;
                }
                else
                {
                    // Use Hedera File Service via REST API
                    var fileData = new
                    {
                        contents = Encoding.UTF8.GetBytes(avatarInfo),
                        fileMemo = $"OASIS Avatar: {avatarId}"
                    };

                    var fileJson = JsonSerializer.Serialize(new
                    {
                        contents = Convert.ToBase64String(fileData.contents),
                        fileMemo = fileData.fileMemo
                    });
                    var fileContent = new StringContent(fileJson, Encoding.UTF8, "application/json");
                    var fileResponse = await _httpClient.PostAsync("/api/v1/files", fileContent);

                    if (fileResponse.IsSuccessStatusCode)
                    {
                        var fileResponseContent = await fileResponse.Content.ReadAsStringAsync();
                        var fileResult = JsonSerializer.Deserialize<JsonElement>(fileResponseContent);
                        
                        if (fileResult.TryGetProperty("fileId", out var fileId))
                        {
                            avatar.ProviderUniqueStorageKey[ProviderType.Value] = fileId.GetString();
                            result.Result = avatar;
                            result.IsError = false;
                            result.IsSaved = true;
                            result.Message = "Avatar saved to Hedera File Service successfully";
                            return result;
                        }
                    }
                    
                    OASISErrorHandling.HandleError(ref result, $"Failed to save avatar to Hedera File Service: {await fileResponse.Content.ReadAsStringAsync()}");
                    return result;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving avatar to Hashgraph: {ex.Message}", ex);
            }
            return result;
        }

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

    }
}
