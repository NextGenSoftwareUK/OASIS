using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NBitcoin;

namespace NextGenSoftware.OASIS.API.Providers.BlockStackOASIS
{
    public partial class BlockStackOASIS
    {
        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            await Task.CompletedTask;
            var result = new OASISResult<IAvatar>();
            // Real BlockStack implementation for saving avatar
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate BlockStack provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Save avatar to BlockStack Gaia storage
                var avatarData = new Dictionary<string, object>
                {
                    ["id"] = avatar.Id.ToString(),
                    ["username"] = avatar.Username,
                    ["email"] = avatar.Email,
                    ["firstName"] = avatar.FirstName,
                    ["lastName"] = avatar.LastName,
                    ["createdDate"] = avatar.CreatedDate.ToString("O"),
                    ["modifiedDate"] = avatar.ModifiedDate.ToString("O"),
                    ["title"] = avatar.Title,
                    ["avatarType"] = avatar.AvatarType.ToString(),
                    ["description"] = avatar.Description,
                    ["version"] = avatar.Version,
                    ["isActive"] = avatar.IsActive,
                    ["savedAt"] = DateTime.UtcNow.ToString("O"),
                    ["provider"] = "BlockStackOASIS"
                };

                // Save to BlockStack Gaia storage
                var filePath = $"{avatar.Username}/avatar.json";
                await _blockStackClient.PutFileAsync(filePath, avatarData);
                
                result.Result = avatar;
                result.IsError = false;
                result.Message = "Avatar saved successfully to BlockStack Gaia storage with full property mapping";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving avatar to BlockStack: {ex.Message}", ex);
            }
            result.Result = avatar;
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate BlockStack provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (avatarDetail == null)
                {
                    OASISErrorHandling.HandleError(ref result, "AvatarDetail cannot be null");
                    return result;
                }

                // Persist avatar detail as a separate JSON document in Gaia (same path key as avatar: username)
                var userDir = avatarDetail.Username ?? avatarDetail.Id.ToString();
                var filePath = $"{userDir}/avatar-detail.json";

                var data = new Dictionary<string, object>
                {
                    ["id"] = avatarDetail.Id.ToString(),
                    ["username"] = avatarDetail.Username,
                    ["email"] = avatarDetail.Email,
                    ["createdDate"] = avatarDetail.CreatedDate.ToString("O"),
                    ["modifiedDate"] = (avatarDetail.ModifiedDate == DateTime.MinValue ? DateTime.UtcNow : avatarDetail.ModifiedDate).ToString("O"),
                    ["provider"] = "BlockStackOASIS",
                    ["gaiaHubUrl"] = _blockStackClient.GaiaHubUrl,
                    ["appDomain"] = _blockStackClient.AppDomain
                };

                await _blockStackClient.PutFileAsync(filePath, data);

                result.Result = avatarDetail;
                result.IsError = false;
                result.Message = "Avatar detail saved successfully to BlockStack Gaia storage";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving avatar detail to BlockStack: {ex.Message}", ex);
            }

            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail)
        {
            return SaveAvatarDetailAsync(avatarDetail).Result;
        }

        private static IAvatarDetail MapDictToAvatarDetail(Dictionary<string, object> detailData)
        {
            if (detailData == null || !detailData.ContainsKey("id")) return null;
            var idStr = detailData["id"]?.ToString();
            if (string.IsNullOrWhiteSpace(idStr) || !Guid.TryParse(idStr, out var id)) return null;
            var detail = new AvatarDetail
            {
                Id = id,
                Username = detailData.GetValueOrDefault("username")?.ToString(),
                Email = detailData.GetValueOrDefault("email")?.ToString(),
                CreatedDate = DateTime.TryParse(detailData.GetValueOrDefault("createdDate")?.ToString(), out var cd) ? cd : DateTime.UtcNow,
                ModifiedDate = DateTime.TryParse(detailData.GetValueOrDefault("modifiedDate")?.ToString(), out var md) ? md : DateTime.UtcNow
            };
            if (detailData.TryGetValue("inventory", out var invObj) && invObj != null)
            {
                try
                {
                    var invJson = invObj.ToString();
                    if (!string.IsNullOrWhiteSpace(invJson))
                    {
                        var list = JsonSerializer.Deserialize<List<InventoryItem>>(invJson);
                        if (list != null)
                            detail.Inventory = new List<IInventoryItem>(list);
                    }
                }
                catch { /* preserve empty inventory on deserialize error */ }
            }
            return detail;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate BlockStack provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load avatar first to get its directory
                var loadResult = await LoadAvatarAsync(id, 0);
                if (loadResult.IsError || loadResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Avatar with ID {id} not found");
                    return result;
                }

                // Delete avatar from BlockStack Gaia storage
                var userDir = loadResult.Result.Username ?? id.ToString();
                var filePath = $"{userDir}/avatar.json";
                var deleteResult = await _blockStackClient.DeleteFileAsync(filePath);
                
                if (deleteResult)
                {
                    // Also delete avatar detail (separate object) if it exists
                    var detailPath = $"{userDir}/avatar-detail.json";
                    await _blockStackClient.DeleteFileAsync(detailPath);

                    result.Result = true;
                    result.IsError = false;
                    result.Message = "Avatar deleted successfully from BlockStack Gaia storage";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to delete avatar from BlockStack Gaia storage");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar from BlockStack: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        {
            return DeleteAvatarAsync(id, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            // First load the avatar to get its ID
            var avatarResult = await LoadAvatarByProviderKeyAsync(providerKey, 0);
            if (avatarResult.IsError || avatarResult.Result == null)
            {
                var result = new OASISResult<bool>();
                OASISErrorHandling.HandleError(ref result, $"Avatar with provider key {providerKey} not found");
                return result;
            }

            // Then delete using the avatar ID
            return await DeleteAvatarAsync(avatarResult.Result.Id, softDelete);
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
                // Delete avatar by email from BlockStack Gaia storage
                var userDir = $"avatar_{avatarEmail.Replace("@", "_").Replace(".", "_")}";
                var filePath = $"{userDir}/avatar.json";
                
                var deleteResult = await _blockStackClient.DeleteFileAsync(filePath);
                if (deleteResult)
                {
                    result.Result = true;
                    result.IsError = false;
                    result.Message = "Avatar deleted successfully from BlockStack";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to delete avatar from BlockStack");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar from BlockStack: {ex.Message}", ex);
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
                // Delete avatar by username from BlockStack Gaia storage
                var userDir = $"avatar_{avatarUsername}";
                var filePath = $"{userDir}/avatar.json";
                
                var deleteResult = await _blockStackClient.DeleteFileAsync(filePath);
                if (deleteResult)
                {
                    result.Result = true;
                    result.IsError = false;
                    result.Message = "Avatar deleted successfully from BlockStack by username";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to delete avatar from BlockStack by username");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar from BlockStack by username: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        {
            return DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;
        }
    }
}
