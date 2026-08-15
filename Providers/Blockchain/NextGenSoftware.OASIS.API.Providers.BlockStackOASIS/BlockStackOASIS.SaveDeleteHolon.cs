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

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
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

                // Real BlockStack implementation for loading holon by ID
                // Use BlockStack Gaia storage to load holon data
                var holonData = await _blockStackClient.GetFileAsync($"holons/{id}.json");
                
                if (holonData != null)
                {
                    var holon = new Holon
                    {
                        Id = Guid.Parse(holonData.GetValueOrDefault("id")?.ToString() ?? id.ToString()),
                        Name = holonData.GetValueOrDefault("name")?.ToString() ?? "BlockStack Holon",
                        Description = holonData.GetValueOrDefault("description")?.ToString() ?? "",
                        CreatedDate = DateTime.TryParse(holonData.GetValueOrDefault("createdDate")?.ToString(), out var createdDate) ? createdDate : DateTime.UtcNow,
                        ModifiedDate = DateTime.TryParse(holonData.GetValueOrDefault("modifiedDate")?.ToString(), out var modifiedDate) ? modifiedDate : DateTime.UtcNow,
                        Version = Convert.ToInt32(holonData.GetValueOrDefault("version") ?? 1),
                        IsActive = Convert.ToBoolean(holonData.GetValueOrDefault("isActive") ?? true),
                        ParentHolonId = holonData.GetValueOrDefault("parentId") != null ? Guid.Parse(holonData.GetValueOrDefault("parentId").ToString()) : Guid.Empty,
                        ProviderUniqueStorageKey = new Dictionary<ProviderType, string>
                        {
                            [Core.Enums.ProviderType.BlockStackOASIS] = holonData.GetValueOrDefault("providerKey")?.ToString() ?? id.ToString()
                        },
                        VersionId = holonData.GetValueOrDefault("nextVersionId") != null ? Guid.Parse(holonData.GetValueOrDefault("nextVersionId").ToString()) : Guid.Empty,
                        IsNewHolon = Convert.ToBoolean(holonData.GetValueOrDefault("isNew") ?? false),
                        DeletedByAvatarId = holonData.GetValueOrDefault("deletedByAvatarId") != null ? Guid.Parse(holonData.GetValueOrDefault("deletedByAvatarId").ToString()) : Guid.Empty,
                        DeletedDate = holonData.GetValueOrDefault("deletedDate") != null ? DateTime.Parse(holonData.GetValueOrDefault("deletedDate").ToString()) : DateTime.MinValue,
                        CreatedByAvatarId = holonData.GetValueOrDefault("createdByAvatarId") != null ? Guid.Parse(holonData.GetValueOrDefault("createdByAvatarId").ToString()) : Guid.Empty,
                        ModifiedByAvatarId = holonData.GetValueOrDefault("modifiedByAvatarId") != null ? Guid.Parse(holonData.GetValueOrDefault("modifiedByAvatarId").ToString()) : Guid.Empty,
                        MetaData = new Dictionary<string, object>
                        {
                            ["BlockStackGaiaHub"] = _blockStackClient.GaiaHubUrl,
                            ["BlockStackAppDomain"] = _blockStackClient.AppDomain,
                            ["BlockStackProvider"] = "BlockStackOASIS",
                            ["LoadedAt"] = DateTime.UtcNow
                        }
                    };
                    
                    result.Result = holon;
                    result.IsError = false;
                    result.Message = "Holon loaded successfully from BlockStack Gaia storage with full property mapping";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Holon not found in BlockStack Gaia storage");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holon from BlockStack: {ex.Message}", ex);
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
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate BlockStack provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Real BlockStack implementation for loading holon by provider key
                // Use BlockStack Gaia storage to load holon data by provider key
                var holonData = await _blockStackClient.GetFileAsync($"holons/{providerKey}.json");
                
                if (holonData != null)
                {
                    // Require valid ID for holon
                    var idStr = holonData.GetValueOrDefault("id")?.ToString();
                    if (string.IsNullOrWhiteSpace(idStr) || !Guid.TryParse(idStr, out var holonId))
                    {
                        OASISErrorHandling.HandleError(ref result, $"Invalid or missing holon ID in provider key: {providerKey}");
                        return result;
                    }
                    
                    var holon = new Holon
                    {
                        Id = holonId,
                        Name = holonData.GetValueOrDefault("name")?.ToString() ?? "BlockStack Holon",
                        Description = holonData.GetValueOrDefault("description")?.ToString() ?? "",
                        CreatedDate = DateTime.TryParse(holonData.GetValueOrDefault("createdDate")?.ToString(), out var createdDate) ? createdDate : DateTime.UtcNow,
                        ModifiedDate = DateTime.TryParse(holonData.GetValueOrDefault("modifiedDate")?.ToString(), out var modifiedDate) ? modifiedDate : DateTime.UtcNow,
                        Version = Convert.ToInt32(holonData.GetValueOrDefault("version") ?? 1),
                        IsActive = Convert.ToBoolean(holonData.GetValueOrDefault("isActive") ?? true),
                        ParentHolonId = holonData.GetValueOrDefault("parentId") != null ? Guid.Parse(holonData.GetValueOrDefault("parentId").ToString()) : Guid.Empty,
                        ProviderUniqueStorageKey = new Dictionary<ProviderType, string>
                        {
                            [Core.Enums.ProviderType.BlockStackOASIS] = providerKey
                        },
                        VersionId = holonData.GetValueOrDefault("nextVersionId") != null ? Guid.Parse(holonData.GetValueOrDefault("nextVersionId").ToString()) : Guid.Empty,
                        IsNewHolon = Convert.ToBoolean(holonData.GetValueOrDefault("isNew") ?? false),
                        DeletedByAvatarId = holonData.GetValueOrDefault("deletedByAvatarId") != null ? Guid.Parse(holonData.GetValueOrDefault("deletedByAvatarId").ToString()) : Guid.Empty,
                        DeletedDate = holonData.GetValueOrDefault("deletedDate") != null ? DateTime.Parse(holonData.GetValueOrDefault("deletedDate").ToString()) : DateTime.MinValue,
                        CreatedByAvatarId = holonData.GetValueOrDefault("createdByAvatarId") != null ? Guid.Parse(holonData.GetValueOrDefault("createdByAvatarId").ToString()) : Guid.Empty,
                        ModifiedByAvatarId = holonData.GetValueOrDefault("modifiedByAvatarId") != null ? Guid.Parse(holonData.GetValueOrDefault("modifiedByAvatarId").ToString()) : Guid.Empty,
                        MetaData = new Dictionary<string, object>
                        {
                            ["BlockStackGaiaHub"] = _blockStackClient.GaiaHubUrl,
                            ["BlockStackAppDomain"] = _blockStackClient.AppDomain,
                            ["BlockStackProvider"] = "BlockStackOASIS",
                            ["BlockStackProviderKey"] = providerKey,
                            ["LoadedAt"] = DateTime.UtcNow
                        }
                    };
                    
                    result.Result = holon;
                    result.IsError = false;
                    result.Message = "Holon loaded successfully from BlockStack Gaia storage by provider key with full property mapping";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Holon not found in BlockStack Gaia storage by provider key");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holon from BlockStack by provider key: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public async Task<OASISResult<IHolon>> LoadHolonByCustomKeyAsync(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
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

                if (string.IsNullOrWhiteSpace(customKey))
                {
                    OASISErrorHandling.HandleError(ref result, "Custom key cannot be null or empty");
                    return result;
                }

                // Load holon by custom key from BlockStack Gaia storage
                var filePath = $"holons/{customKey}.json";
                var holonData = await _blockStackClient.GetFileAsync(filePath);
                
                if (holonData != null && holonData.Count > 0)
                {
                    var holonJson = System.Text.Json.JsonSerializer.Serialize(holonData);
                    var holon = System.Text.Json.JsonSerializer.Deserialize<Holon>(holonJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    if (holon != null)
                    {
                        // Load children if requested
                        if (loadChildren && (recursive || maxChildDepth > 0))
                        {
                            var childrenResult = await LoadHolonsForParentAsync(holon.Id, HolonType.All, loadChildren, recursive, maxChildDepth, 0, continueOnError, loadChildrenFromProvider, version);
                            if (!childrenResult.IsError && childrenResult.Result != null)
                            {
                                holon.Children = childrenResult.Result.ToList();
                            }
                        }
                        
                        result.Result = holon;
                        result.IsError = false;
                        result.Message = "Holon loaded successfully from BlockStack by custom key";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize holon from BlockStack storage");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Holon not found in BlockStack storage");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holon by custom key from BlockStack: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IHolon> LoadHolonByCustomKey(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonByCustomKeyAsync(customKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public async Task<OASISResult<IHolon>> LoadHolonByMetaDataAsync(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
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

                if (string.IsNullOrWhiteSpace(metaKey) || string.IsNullOrWhiteSpace(metaValue))
                {
                    OASISErrorHandling.HandleError(ref result, "Metadata key and value are required");
                    return result;
                }

                // Load holons by metadata and return first match
                var holonsResult = await LoadHolonsByMetaDataAsync(metaKey, metaValue, HolonType.All, loadChildren, recursive, maxChildDepth, 0, continueOnError, loadChildrenFromProvider, version);
                
                if (!holonsResult.IsError && holonsResult.Result != null && holonsResult.Result.Any())
                {
                    result.Result = holonsResult.Result.First();
                    result.IsError = false;
                    result.Message = "Holon loaded successfully from BlockStack by metadata";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "No holon found matching the metadata criteria");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holon by metadata from BlockStack: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IHolon> LoadHolonByMetaData(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonByMetaDataAsync(metaKey, metaValue, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

    }
}
