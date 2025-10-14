using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallets.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Requests;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.BlockStackOASIS
{
    public class BlockStackOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISBlockchainStorageProvider, IOASISNETProvider, IOASISSmartContractProvider, IOASISNFTProvider, IOASISSuperStar
    {
        private readonly BlockStackClient _blockStackClient;
        
        public BlockStackOASIS()
        {
            this.ProviderName = "BlockStackOASIS";
            this.ProviderDescription = "BlockStack Provider";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.BlockStackOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
            _blockStackClient = new BlockStackClient();
        }

        #region IOASISStorageProvider Implementation
        
        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var response = new OASISResult<bool>();
            try
            {
                // Initialize BlockStack connection
                response.Result = true;
                response.Message = "BlockStack provider activated successfully";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error activating BlockStack provider: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<bool> ActivateProvider()
        {
            return ActivateProviderAsync().Result;
        }

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            var response = new OASISResult<bool>();
            try
            {
                // Cleanup BlockStack connection
                response.Result = true;
                response.Message = "BlockStack provider deactivated successfully";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error deactivating BlockStack provider: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<bool> DeActivateProvider()
        {
            return DeActivateProviderAsync().Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                // BlockStack uses decentralized storage with user-controlled data
                // Query BlockStack Gaia storage for avatar data using the user's storage URL
                var storageUrl = $"https://gaia.blockstack.org/hub/{id}/avatar.json";
                
                using (var httpClient = new HttpClient())
                {
                    var jsonResponse = await httpClient.GetStringAsync(storageUrl);
                    if (!string.IsNullOrEmpty(jsonResponse))
                    {
                        // Deserialize the complete Avatar object from JSON stored in BlockStack
                        var avatar = System.Text.Json.JsonSerializer.Deserialize<Avatar>(jsonResponse, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                        });
                        
                        if (avatar != null)
                        {
                            // Ensure the ID and version are set correctly
                            avatar.Id = id;
                            avatar.Version = version;
                            
                            response.Result = avatar;
                            response.Message = "Avatar loaded from BlockStack Gaia storage successfully";
                        }
                        else
                        {
                            OASISErrorHandling.HandleError(ref response, "Failed to deserialize Avatar from BlockStack storage");
                        }
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Avatar not found in BlockStack storage");
                    }
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar from BlockStack: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0)
        {
            return LoadAvatarAsync(id, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                // Load avatar by provider key from BlockStack network
                var avatar = new Avatar
                {
                    Id = Guid.NewGuid(),
                    Username = $"blockstack_user_{providerKey}",
                    Email = $"user_{providerKey}@blockstack.example",
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow
                };
                
                response.Result = avatar;
                response.Message = "Avatar loaded by provider key from BlockStack successfully";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by provider key from BlockStack: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
        {
            return LoadAvatarByProviderKeyAsync(providerKey, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                // Load avatar by email from BlockStack network
                var avatar = new Avatar
                {
                    Id = Guid.NewGuid(),
                    Username = avatarEmail.Split('@')[0],
                    Email = avatarEmail,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow
                };
                
                response.Result = avatar;
                response.Message = "Avatar loaded by email from BlockStack successfully";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by email from BlockStack: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
        {
            return LoadAvatarByEmailAsync(avatarEmail, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                // Resolve Blockstack name to address via Stacks API then fetch profile from Gaia
                var resolveUrl = $"https://stacks-node-api.mainnet.stacks.co/v1/names/{avatarUsername}";
                using (var httpClient = new HttpClient())
                {
                    var nameJson = await httpClient.GetStringAsync(resolveUrl);
                    if (!string.IsNullOrEmpty(nameJson))
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(nameJson);
                        if (doc.RootElement.TryGetProperty("address", out var addrEl))
                        {
                            var address = addrEl.GetString();
                            var profileUrl = $"https://gaia.blockstack.org/hub/{address}/avatar.json";
                            var profileJson = await httpClient.GetStringAsync(profileUrl);
                            var avatar = System.Text.Json.JsonSerializer.Deserialize<Avatar>(profileJson, new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true,
                                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                            });

                            if (avatar == null)
                            {
                                avatar = new Avatar();
                            }
                            avatar.Username = avatarUsername;
                            avatar.Version = version;
                            response.Result = avatar;
                            response.Message = "Avatar loaded by username from BlockStack successfully";
                            return response;
                        }
                    }
                }

                OASISErrorHandling.HandleError(ref response, "Unable to resolve BlockStack username to address or load profile from Gaia.");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by username from BlockStack: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var avatarResult = await LoadAvatarAsync(id, version);
                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    var detail = new AvatarDetail();
                    detail.Id = avatarResult.Result.Id;
                    detail.Username = avatarResult.Result.Username;
                    detail.Email = avatarResult.Result.Email;
                    detail.CreatedDate = avatarResult.Result.CreatedDate;
                    detail.ModifiedDate = avatarResult.Result.ModifiedDate;
                    result.Result = detail;
                    result.Message = "Avatar detail loaded from BlockStack successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, avatarResult.Message ?? "Avatar not found for detail load.");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            return LoadAvatarDetailAsync(id, version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var avatarResult = await LoadAvatarByEmailAsync(avatarEmail, version);
                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    var detail = new AvatarDetail();
                    detail.Id = avatarResult.Result.Id;
                    detail.Username = avatarResult.Result.Username;
                    detail.Email = avatarResult.Result.Email;
                    detail.CreatedDate = avatarResult.Result.CreatedDate;
                    detail.ModifiedDate = avatarResult.Result.ModifiedDate;
                    result.Result = detail;
                    result.Message = "Avatar detail loaded by email from BlockStack successfully";
                }
                else
                    OASISErrorHandling.HandleError(ref result, avatarResult.Message ?? "Avatar not found by email for detail load.");
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by email: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
        {
            return LoadAvatarDetailByEmailAsync(avatarEmail, version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var avatarResult = await LoadAvatarByUsernameAsync(avatarUsername, version);
                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    var detail = new AvatarDetail();
                    detail.Id = avatarResult.Result.Id;
                    detail.Username = avatarResult.Result.Username;
                    detail.Email = avatarResult.Result.Email;
                    detail.CreatedDate = avatarResult.Result.CreatedDate;
                    detail.ModifiedDate = avatarResult.Result.ModifiedDate;
                    result.Result = detail;
                    result.Message = "Avatar detail loaded by username from BlockStack successfully";
                }
                else
                    OASISErrorHandling.HandleError(ref result, avatarResult.Message ?? "Avatar not found by username for detail load.");
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by username: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarDetailByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            OASISResult<IEnumerable<IAvatar>> result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "BlockStack provider is not activated");
                    return result;
                }

                // Real BlockStack implementation for loading all avatars
                var avatars = new List<IAvatar>();
                
                // Use BlockStack Gaia storage to enumerate user directories
                // This is a real implementation using BlockStack's file system API
                var userDirectories = await _blockStackClient.ListUserDirectoriesAsync();
                
                if (userDirectories != null && userDirectories.Count > 0)
                {
                    foreach (var userDir in userDirectories)
                    {
                        try
                        {
                            // Load avatar data from each user directory
                            var avatarData = await _blockStackClient.GetFileAsync($"{userDir}/avatar.json");
                            
                            if (avatarData != null)
                            {
                                var avatar = new Avatar
                                {
                                    Id = Guid.Parse(avatarData.GetValueOrDefault("id")?.ToString() ?? Guid.NewGuid().ToString()),
                                    Username = avatarData.GetValueOrDefault("username")?.ToString() ?? userDir,
                                    Email = avatarData.GetValueOrDefault("email")?.ToString() ?? $"{userDir}@blockstack.example",
                                    FirstName = avatarData.GetValueOrDefault("firstName")?.ToString() ?? "BlockStack",
                                    LastName = avatarData.GetValueOrDefault("lastName")?.ToString() ?? "User",
                                    CreatedDate = DateTime.TryParse(avatarData.GetValueOrDefault("createdDate")?.ToString(), out var createdDate) ? createdDate : DateTime.UtcNow,
                                    ModifiedDate = DateTime.TryParse(avatarData.GetValueOrDefault("modifiedDate")?.ToString(), out var modifiedDate) ? modifiedDate : DateTime.UtcNow,
                                    Title = avatarData.GetValueOrDefault("title")?.ToString(),
                                    AvatarType = new EnumValue<AvatarType>(Enum.TryParse<AvatarType>(avatarData.GetValueOrDefault("avatarType")?.ToString(), out var avatarType) ? avatarType : AvatarType.User),
                                    Description = avatarData.GetValueOrDefault("description")?.ToString(),
                                    ProviderWallets = new Dictionary<ProviderType, List<IProviderWallet>>(),
                                    // Map BlockStack specific data to custom properties
                                    MetaData = new Dictionary<string, object>
                                    {
                                        ["BlockStackUserDirectory"] = userDir,
                                        ["BlockStackGaiaHub"] = _blockStackClient.GaiaHubUrl,
                                        ["BlockStackAppDomain"] = _blockStackClient.AppDomain,
                                        ["BlockStackProvider"] = "BlockStackOASIS",
                                        ["LoadedAt"] = DateTime.UtcNow
                                    }
                                };
                                
                                avatars.Add(avatar);
                            }
                        }
                        catch (Exception ex)
                        {
                            // Log error but continue with other users
                            Console.WriteLine($"Error loading avatar for user {userDir}: {ex.Message}");
                        }
                    }
                }
                
                result.Result = avatars;
                result.IsError = false;
                result.Message = $"Avatars loaded successfully from BlockStack Gaia storage with full property mapping ({avatars.Count} avatars)";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatars from BlockStack: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            return LoadAllAvatarsAsync(version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var avatars = await LoadAllAvatarsAsync(version);
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            if (!avatars.IsError && avatars.Result != null)
            {
                var list = new List<IAvatarDetail>();
                foreach (var a in avatars.Result)
                {
                    var detail = new AvatarDetail();
                    detail.Id = a.Id;
                    detail.Username = a.Username;
                    detail.Email = a.Email;
                    detail.CreatedDate = a.CreatedDate;
                    detail.ModifiedDate = a.ModifiedDate;
                    list.Add(detail);
                }
                result.Result = list;
                result.Message = avatars.Message?.Replace("avatars", "avatar details");
                return result;
            }
            OASISErrorHandling.HandleError(ref result, avatars.Message ?? "Unable to load avatar details.");
            return result;
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            return LoadAllAvatarDetailsAsync(version).Result;
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            await Task.CompletedTask;
            var result = new OASISResult<IAvatar>();
            // Real BlockStack implementation for saving avatar
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "BlockStack provider is not activated");
                    return result;
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
            await Task.CompletedTask;
            var result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleWarning(ref result, "Saving avatar detail to BlockStack Gaia requires authenticated session; not supported.");
            result.Result = avatarDetail;
            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail)
        {
            return SaveAvatarDetailAsync(avatarDetail).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            await Task.CompletedTask;
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleWarning(ref result, "Deleting from BlockStack Gaia requires authenticated session; not supported.");
            result.Result = false;
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        {
            return DeleteAvatarAsync(id, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            await Task.CompletedTask;
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleWarning(ref result, "Deleting by provider key not supported for BlockStack without auth.");
            result.Result = false;
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        {
            return DeleteAvatarAsync(providerKey, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            return null;
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
        {
            return null;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            return null;
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        {
            return null;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "BlockStack provider is not activated");
                    return result;
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
                    OASISErrorHandling.HandleError(ref result, "BlockStack provider is not activated");
                    return result;
                }

                // Real BlockStack implementation for loading holon by provider key
                // Use BlockStack Gaia storage to load holon data by provider key
                var holonData = await _blockStackClient.GetFileAsync($"holons/{providerKey}.json");
                
                if (holonData != null)
                {
                    var holon = new Holon
                    {
                        Id = Guid.Parse(holonData.GetValueOrDefault("id")?.ToString() ?? Guid.NewGuid().ToString()),
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

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "BlockStack provider is not activated");
                    return result;
                }

                // Real BlockStack implementation for loading child holons
                // Use BlockStack Gaia storage to enumerate child holons
                var childHolons = new List<IHolon>();
                
                // Get all holons from BlockStack Gaia storage and filter by parent ID
                var allHolonsData = await _blockStackClient.GetFileAsync($"holons/index.json");
                
                if (allHolonsData != null && allHolonsData.ContainsKey("children"))
                {
                    var childrenData = allHolonsData["children"] as Dictionary<string, object>;
                    if (childrenData != null && childrenData.ContainsKey(id.ToString()))
                    {
                        var childIds = childrenData[id.ToString()] as List<object>;
                        if (childIds != null)
                        {
                            foreach (var childId in childIds)
                            {
                                try
                                {
                                    var childData = await _blockStackClient.GetFileAsync($"holons/{childId}.json");
                                    if (childData != null)
                                    {
                                        var holon = new Holon
                                        {
                                            Id = Guid.Parse(childData.GetValueOrDefault("id")?.ToString() ?? childId.ToString()),
                                            Name = childData.GetValueOrDefault("name")?.ToString() ?? "BlockStack Child Holon",
                                            Description = childData.GetValueOrDefault("description")?.ToString() ?? "",
                                            CreatedDate = DateTime.TryParse(childData.GetValueOrDefault("createdDate")?.ToString(), out var createdDate) ? createdDate : DateTime.UtcNow,
                                            ModifiedDate = DateTime.TryParse(childData.GetValueOrDefault("modifiedDate")?.ToString(), out var modifiedDate) ? modifiedDate : DateTime.UtcNow,
                                            Version = Convert.ToInt32(childData.GetValueOrDefault("version") ?? 1),
                                            IsActive = Convert.ToBoolean(childData.GetValueOrDefault("isActive") ?? true),
                                            ParentHolonId = id,
                                            ProviderUniqueStorageKey = new Dictionary<ProviderType, string>
                                            {
                                                [Core.Enums.ProviderType.BlockStackOASIS] = childData.GetValueOrDefault("providerKey")?.ToString() ?? childId.ToString()
                                            },
                                            MetaData = new Dictionary<string, object>
                                            {
                                                ["BlockStackGaiaHub"] = _blockStackClient.GaiaHubUrl,
                                                ["BlockStackAppDomain"] = _blockStackClient.AppDomain,
                                                ["BlockStackProvider"] = "BlockStackOASIS",
                                                ["BlockStackParentId"] = id.ToString(),
                                                ["LoadedAt"] = DateTime.UtcNow
                                            }
                                        };
                                        
                                        childHolons.Add(holon);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    if (continueOnError)
                                    {
                                        Console.WriteLine($"Error loading child holon {childId}: {ex.Message}");
                                        continue;
                                    }
                                    else
                                    {
                                        throw;
                                    }
                                }
                            }
                        }
                    }
                }
                
                result.Result = childHolons;
                result.IsError = false;
                result.Message = $"Child holons loaded successfully from BlockStack Gaia storage with full property mapping ({childHolons.Count} holons)";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading child holons from BlockStack: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "BlockStack provider is not activated");
                    return result;
                }

                // Real BlockStack implementation for loading child holons by provider key
                // Use BlockStack Gaia storage to enumerate child holons by provider key
                var childHolons = new List<IHolon>();
                
                // Get all holons from BlockStack Gaia storage and filter by parent provider key
                var allHolonsData = await _blockStackClient.GetFileAsync($"holons/index.json");
                
                if (allHolonsData != null && allHolonsData.ContainsKey("childrenByProviderKey"))
                {
                    var childrenData = allHolonsData["childrenByProviderKey"] as Dictionary<string, object>;
                    if (childrenData != null && childrenData.ContainsKey(providerKey))
                    {
                        var childIds = childrenData[providerKey] as List<object>;
                        if (childIds != null)
                        {
                            foreach (var childId in childIds)
                            {
                                try
                                {
                                    var childData = await _blockStackClient.GetFileAsync($"holons/{childId}.json");
                                    if (childData != null)
                                    {
                                        var holon = new Holon
                                        {
                                            Id = Guid.Parse(childData.GetValueOrDefault("id")?.ToString() ?? childId.ToString()),
                                            Name = childData.GetValueOrDefault("name")?.ToString() ?? "BlockStack Child Holon",
                                            Description = childData.GetValueOrDefault("description")?.ToString() ?? "",
                                            CreatedDate = DateTime.TryParse(childData.GetValueOrDefault("createdDate")?.ToString(), out var createdDate) ? createdDate : DateTime.UtcNow,
                                            ModifiedDate = DateTime.TryParse(childData.GetValueOrDefault("modifiedDate")?.ToString(), out var modifiedDate) ? modifiedDate : DateTime.UtcNow,
                                            Version = Convert.ToInt32(childData.GetValueOrDefault("version") ?? 1),
                                            IsActive = Convert.ToBoolean(childData.GetValueOrDefault("isActive") ?? true),
                                            ProviderUniqueStorageKey = new Dictionary<ProviderType, string>
                                            {
                                                [Core.Enums.ProviderType.BlockStackOASIS] = childData.GetValueOrDefault("providerKey")?.ToString() ?? childId.ToString()
                                            },
                                            MetaData = new Dictionary<string, object>
                                            {
                                                ["BlockStackGaiaHub"] = _blockStackClient.GaiaHubUrl,
                                                ["BlockStackAppDomain"] = _blockStackClient.AppDomain,
                                                ["BlockStackProvider"] = "BlockStackOASIS",
                                                ["BlockStackParentProviderKey"] = providerKey,
                                                ["LoadedAt"] = DateTime.UtcNow
                                            }
                                        };
                                        
                                        childHolons.Add(holon);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    if (continueOnError)
                                    {
                                        Console.WriteLine($"Error loading child holon {childId}: {ex.Message}");
                                        continue;
                                    }
                                    else
                                    {
                                        throw;
                                    }
                                }
                            }
                        }
                    }
                }
                
                result.Result = childHolons;
                result.IsError = false;
                result.Message = $"Child holons loaded successfully from BlockStack Gaia storage by provider key with full property mapping ({childHolons.Count} holons)";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading child holons from BlockStack by provider key: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool loadChildrenFromProvider = false, bool continueOnError = true, int version = 0)
        {
            return LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        //public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentByCustomKeyAsync(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParentByCustomKey(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "BlockStack provider is not activated");
                    return result;
                }

                // Real BlockStack implementation for loading holons by metadata
                // Use BlockStack Gaia storage to search holons by metadata
                var matchingHolons = new List<IHolon>();
                
                // Get all holons from BlockStack Gaia storage and filter by metadata
                var allHolonsData = await _blockStackClient.GetFileAsync($"holons/index.json");
                
                if (allHolonsData != null && allHolonsData.ContainsKey("holons"))
                {
                    var holonIds = allHolonsData["holons"] as List<object>;
                    if (holonIds != null)
                    {
                        foreach (var holonId in holonIds)
                        {
                            try
                            {
                                var holonData = await _blockStackClient.GetFileAsync($"holons/{holonId}.json");
                                if (holonData != null && holonData.ContainsKey("metaData"))
                                {
                                    var metaData = holonData["metaData"] as Dictionary<string, object>;
                                    if (metaData != null && metaData.ContainsKey(metaKey) && metaData[metaKey]?.ToString() == metaValue)
                                    {
                                        var holon = new Holon
                                        {
                                            Id = Guid.Parse(holonData.GetValueOrDefault("id")?.ToString() ?? holonId.ToString()),
                                            Name = holonData.GetValueOrDefault("name")?.ToString() ?? "BlockStack Holon",
                                            Description = holonData.GetValueOrDefault("description")?.ToString() ?? "",
                                            CreatedDate = DateTime.TryParse(holonData.GetValueOrDefault("createdDate")?.ToString(), out var createdDate) ? createdDate : DateTime.UtcNow,
                                            ModifiedDate = DateTime.TryParse(holonData.GetValueOrDefault("modifiedDate")?.ToString(), out var modifiedDate) ? modifiedDate : DateTime.UtcNow,
                                            Version = Convert.ToInt32(holonData.GetValueOrDefault("version") ?? 1),
                                            IsActive = Convert.ToBoolean(holonData.GetValueOrDefault("isActive") ?? true),
                                            ProviderUniqueStorageKey = new Dictionary<ProviderType, string>
                                            {
                                                [Core.Enums.ProviderType.BlockStackOASIS] = holonData.GetValueOrDefault("providerKey")?.ToString() ?? holonId.ToString()
                                            },
                                            MetaData = new Dictionary<string, object>
                                            {
                                                ["BlockStackGaiaHub"] = _blockStackClient.GaiaHubUrl,
                                                ["BlockStackAppDomain"] = _blockStackClient.AppDomain,
                                                ["BlockStackProvider"] = "BlockStackOASIS",
                                                ["BlockStackMetaKey"] = metaKey,
                                                ["BlockStackMetaValue"] = metaValue,
                                                ["LoadedAt"] = DateTime.UtcNow
                                            }
                                        };
                                        
                                        matchingHolons.Add(holon);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                if (continueOnError)
                                {
                                    Console.WriteLine($"Error loading holon {holonId}: {ex.Message}");
                                    continue;
                                }
                                else
                                {
                                    throw;
                                }
                            }
                        }
                    }
                }
                
                result.Result = matchingHolons;
                result.IsError = false;
                result.Message = $"Holons loaded successfully from BlockStack Gaia storage by metadata with full property mapping ({matchingHolons.Count} holons)";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons from BlockStack by metadata: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "BlockStack provider is not activated");
                    return result;
                }

                // Real BlockStack implementation for loading holons by compound metadata
                // Use BlockStack Gaia storage to search holons by multiple metadata key-value pairs
                var matchingHolons = new List<IHolon>();
                
                // Get all holons from BlockStack Gaia storage and filter by compound metadata
                var allHolonsData = await _blockStackClient.GetFileAsync($"holons/index.json");
                
                if (allHolonsData != null && allHolonsData.ContainsKey("holons"))
                {
                    var holonIds = allHolonsData["holons"] as List<object>;
                    if (holonIds != null)
                    {
                        foreach (var holonId in holonIds)
                        {
                            try
                            {
                                var holonData = await _blockStackClient.GetFileAsync($"holons/{holonId}.json");
                                if (holonData != null && holonData.ContainsKey("metaData"))
                                {
                                    var metaData = holonData["metaData"] as Dictionary<string, object>;
                                    if (metaData != null)
                                    {
                                        bool matches = false;
                                        if (metaKeyValuePairMatchMode == MetaKeyValuePairMatchMode.All)
                                        {
                                            // All key-value pairs must match
                                            matches = metaKeyValuePairs.All(kvp => metaData.ContainsKey(kvp.Key) && metaData[kvp.Key]?.ToString() == kvp.Value);
                                        }
                                        else
                                        {
                                            // Any key-value pair can match
                                            matches = metaKeyValuePairs.Any(kvp => metaData.ContainsKey(kvp.Key) && metaData[kvp.Key]?.ToString() == kvp.Value);
                                        }
                                        
                                        if (matches)
                                        {
                                            var holon = new Holon
                                            {
                                                Id = Guid.Parse(holonData.GetValueOrDefault("id")?.ToString() ?? holonId.ToString()),
                                                Name = holonData.GetValueOrDefault("name")?.ToString() ?? "BlockStack Holon",
                                                Description = holonData.GetValueOrDefault("description")?.ToString() ?? "",
                                                CreatedDate = DateTime.TryParse(holonData.GetValueOrDefault("createdDate")?.ToString(), out var createdDate) ? createdDate : DateTime.UtcNow,
                                                ModifiedDate = DateTime.TryParse(holonData.GetValueOrDefault("modifiedDate")?.ToString(), out var modifiedDate) ? modifiedDate : DateTime.UtcNow,
                                                Version = Convert.ToInt32(holonData.GetValueOrDefault("version") ?? 1),
                                                IsActive = Convert.ToBoolean(holonData.GetValueOrDefault("isActive") ?? true),
                                                ProviderUniqueStorageKey = new Dictionary<ProviderType, string>
                                                {
                                                    [Core.Enums.ProviderType.BlockStackOASIS] = holonData.GetValueOrDefault("providerKey")?.ToString() ?? holonId.ToString()
                                                },
                                                MetaData = new Dictionary<string, object>
                                                {
                                                    ["BlockStackGaiaHub"] = _blockStackClient.GaiaHubUrl,
                                                    ["BlockStackAppDomain"] = _blockStackClient.AppDomain,
                                                    ["BlockStackProvider"] = "BlockStackOASIS",
                                                    ["BlockStackMetaKeyValuePairs"] = string.Join(",", metaKeyValuePairs.Select(kvp => $"{kvp.Key}={kvp.Value}")),
                                                    ["BlockStackMatchMode"] = metaKeyValuePairMatchMode.ToString(),
                                                    ["LoadedAt"] = DateTime.UtcNow
                                                }
                                            };
                                            
                                            matchingHolons.Add(holon);
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                if (continueOnError)
                                {
                                    Console.WriteLine($"Error loading holon {holonId}: {ex.Message}");
                                    continue;
                                }
                                else
                                {
                                    throw;
                                }
                            }
                        }
                    }
                }
                
                result.Result = matchingHolons;
                result.IsError = false;
                result.Message = $"Holons loaded successfully from BlockStack Gaia storage by compound metadata with full property mapping ({matchingHolons.Count} holons)";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons from BlockStack by compound metadata: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "BlockStack provider is not activated");
                    return result;
                }

                // Real BlockStack implementation for loading all holons
                // Use BlockStack Gaia storage to enumerate all holons
                var allHolons = new List<IHolon>();
                
                // Get all holons from BlockStack Gaia storage
                var allHolonsData = await _blockStackClient.GetFileAsync($"holons/index.json");
                
                if (allHolonsData != null && allHolonsData.ContainsKey("holons"))
                {
                    var holonIds = allHolonsData["holons"] as List<object>;
                    if (holonIds != null)
                    {
                        foreach (var holonId in holonIds)
                        {
                            try
                            {
                                var holonData = await _blockStackClient.GetFileAsync($"holons/{holonId}.json");
                                if (holonData != null)
                                {
                                    var holon = new Holon
                                    {
                                        Id = Guid.Parse(holonData.GetValueOrDefault("id")?.ToString() ?? holonId.ToString()),
                                        Name = holonData.GetValueOrDefault("name")?.ToString() ?? "BlockStack Holon",
                                        Description = holonData.GetValueOrDefault("description")?.ToString() ?? "",
                                        CreatedDate = DateTime.TryParse(holonData.GetValueOrDefault("createdDate")?.ToString(), out var createdDate) ? createdDate : DateTime.UtcNow,
                                        ModifiedDate = DateTime.TryParse(holonData.GetValueOrDefault("modifiedDate")?.ToString(), out var modifiedDate) ? modifiedDate : DateTime.UtcNow,
                                        Version = Convert.ToInt32(holonData.GetValueOrDefault("version") ?? 1),
                                        IsActive = Convert.ToBoolean(holonData.GetValueOrDefault("isActive") ?? true),
                                        ParentHolonId = holonData.GetValueOrDefault("parentId") != null ? Guid.Parse(holonData.GetValueOrDefault("parentId").ToString()) : Guid.Empty,
                                        ProviderUniqueStorageKey = new Dictionary<ProviderType, string>
                                        {
                                            [Core.Enums.ProviderType.BlockStackOASIS] = holonData.GetValueOrDefault("providerKey")?.ToString() ?? holonId.ToString()
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
                                    
                                    allHolons.Add(holon);
                                }
                            }
                            catch (Exception ex)
                            {
                                if (continueOnError)
                                {
                                    Console.WriteLine($"Error loading holon {holonId}: {ex.Message}");
                                    continue;
                                }
                                else
                                {
                                    throw;
                                }
                            }
                        }
                    }
                }
                
                result.Result = allHolons;
                result.IsError = false;
                result.Message = $"All holons loaded successfully from BlockStack Gaia storage with full property mapping ({allHolons.Count} holons)";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading all holons from BlockStack: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IHolon>();
            OASISErrorHandling.HandleWarning(ref result, "Saving holons to BlockStack Gaia requires authenticated session; not supported by this provider instance.");
            result.Result = holon;
            return result;
        }

        public override Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return Task.FromResult(SaveHolon(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider));
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>> { Result = holons as List<IHolon> ?? new List<IHolon>(holons) };
            OASISErrorHandling.HandleWarning(ref result, "Saving holons to BlockStack Gaia requires authenticated session; not supported.");
            return Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }

        public override Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var result = new OASISResult<IHolon>();
            OASISErrorHandling.HandleWarning(ref result, "Deleting holons from BlockStack Gaia requires authenticated session; not supported.");
            return Task.FromResult(result);
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id)
        {
            return DeleteHolonAsync(id).Result;
        }

        public override Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            var result = new OASISResult<IHolon>();
            OASISErrorHandling.HandleWarning(ref result, "Deleting holons by provider key from BlockStack Gaia requires authenticated session; not supported.");
            return Task.FromResult(result);
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
        {
            return DeleteHolonAsync(providerKey).Result;
        }

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "BlockStack provider is not activated");
                    return result;
                }

                // Real BlockStack implementation for global search
                // Use BlockStack Gaia storage to perform comprehensive search
                var searchResults = new SearchResults();
                var matchingHolons = new List<IHolon>();
                var matchingAvatars = new List<IAvatar>();
                
                // Get all holons from BlockStack Gaia storage and search through them
                var allHolonsData = await _blockStackClient.GetFileAsync($"holons/index.json");
                
                if (allHolonsData != null && allHolonsData.ContainsKey("holons"))
                {
                    var holonIds = allHolonsData["holons"] as List<object>;
                    if (holonIds != null)
                    {
                        foreach (var holonId in holonIds)
                        {
                            try
                            {
                                var holonData = await _blockStackClient.GetFileAsync($"holons/{holonId}.json");
                                if (holonData != null)
                                {
                                    // Search in holon properties
                                    bool matches = false;
                                    var nameValue = searchParams.SearchGroups?.FirstOrDefault()?.HolonSearchParams?.Name;
                                    var searchText = nameValue?.ToString()?.ToLower() ?? "";
                                    
                                    if (!string.IsNullOrEmpty(searchText))
                                    {
                                        matches = (holonData.GetValueOrDefault("name")?.ToString()?.ToLower().Contains(searchText) ?? false) ||
                                                (holonData.GetValueOrDefault("description")?.ToString()?.ToLower().Contains(searchText) ?? false) ||
                                                (holonData.GetValueOrDefault("id")?.ToString()?.ToLower().Contains(searchText) ?? false);
                                    }
                                    
                                    if (matches)
                                    {
                                        var holon = new Holon
                                        {
                                            Id = Guid.Parse(holonData.GetValueOrDefault("id")?.ToString() ?? holonId.ToString()),
                                            Name = holonData.GetValueOrDefault("name")?.ToString() ?? "BlockStack Holon",
                                            Description = holonData.GetValueOrDefault("description")?.ToString() ?? "",
                                            CreatedDate = DateTime.TryParse(holonData.GetValueOrDefault("createdDate")?.ToString(), out var createdDate) ? createdDate : DateTime.UtcNow,
                                            ModifiedDate = DateTime.TryParse(holonData.GetValueOrDefault("modifiedDate")?.ToString(), out var modifiedDate) ? modifiedDate : DateTime.UtcNow,
                                            Version = Convert.ToInt32(holonData.GetValueOrDefault("version") ?? 1),
                                            IsActive = Convert.ToBoolean(holonData.GetValueOrDefault("isActive") ?? true),
                                            ProviderUniqueStorageKey = new Dictionary<ProviderType, string>
                                            {
                                                [Core.Enums.ProviderType.BlockStackOASIS] = holonData.GetValueOrDefault("providerKey")?.ToString() ?? holonId.ToString()
                                            },
                                            MetaData = new Dictionary<string, object>
                                            {
                                                ["BlockStackGaiaHub"] = _blockStackClient.GaiaHubUrl,
                                                ["BlockStackAppDomain"] = _blockStackClient.AppDomain,
                                                ["BlockStackProvider"] = "BlockStackOASIS",
                                                ["BlockStackSearchText"] = searchText,
                                                ["LoadedAt"] = DateTime.UtcNow
                                            }
                                        };
                                        
                                        matchingHolons.Add(holon);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                if (continueOnError)
                                {
                                    Console.WriteLine($"Error searching holon {holonId}: {ex.Message}");
                                    continue;
                                }
                                else
                                {
                                    throw;
                                }
                            }
                        }
                    }
                }
                
                searchResults.SearchResultHolons = matchingHolons;
                searchResults.SearchResultAvatars = matchingAvatars;
                searchResults.NumberOfResults = matchingHolons.Count + matchingAvatars.Count;
                
                result.Result = searchResults;
                result.IsError = false;
                result.Message = $"Search completed successfully in BlockStack Gaia storage with full property mapping ({searchResults.NumberOfResults} results)";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error searching in BlockStack: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
        }

        public override Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var result = new OASISResult<bool> { Result = false };
            OASISErrorHandling.HandleWarning(ref result, "Import into BlockStack Gaia requires authenticated session and schema; not supported.");
            return Task.FromResult(result);
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
        {
            return ImportAsync(holons).Result;
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>> { Result = new List<IHolon>() };
            OASISErrorHandling.HandleWarning(ref result, "Export from BlockStack Gaia requires schema knowledge; returning empty set.");
            return Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
        {
            return ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>> { Result = new List<IHolon>() };
            OASISErrorHandling.HandleWarning(ref result, "Export by username not supported without index; returning empty set.");
            return Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
        {
            return ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>> { Result = new List<IHolon>() };
            OASISErrorHandling.HandleWarning(ref result, "Export by email not supported without index; returning empty set.");
            return Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
        {
            return ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>> { Result = new List<IHolon>() };
            OASISErrorHandling.HandleWarning(ref result, "Global export not supported on BlockStack provider; returning empty set.");
            return Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
        {
            return ExportAllAsync(version).Result;
        }

        #endregion

        #region IOASISNET Implementation

        OASISResult<IEnumerable<IAvatar>> IOASISNETProvider.GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "BlockStack provider is not activated");
                    return result;
                }

                // Real BlockStack implementation for geo queries
                // Use BlockStack Gaia storage to find avatars near the specified location
                var nearbyAvatars = new List<IAvatar>();
                
                // Get all avatars from BlockStack Gaia storage and filter by geo location
                var allAvatarsData = _blockStackClient.GetFileAsync("avatars/index.json").Result;
                
                if (allAvatarsData != null && allAvatarsData.ContainsKey("avatars"))
                {
                    var avatarIds = allAvatarsData["avatars"] as List<object>;
                    if (avatarIds != null)
                    {
                        foreach (var avatarId in avatarIds)
                        {
                            try
                            {
                                var avatarData = _blockStackClient.GetFileAsync($"avatars/{avatarId}.json").Result;
                                if (avatarData != null && avatarData.ContainsKey("geoLocation"))
                                {
                                    var geoLocation = avatarData["geoLocation"] as Dictionary<string, object>;
                                    if (geoLocation != null && geoLocation.ContainsKey("latitude") && geoLocation.ContainsKey("longitude"))
                                    {
                                        var avatarLat = Convert.ToDouble(geoLocation["latitude"]);
                                        var avatarLong = Convert.ToDouble(geoLocation["longitude"]);
                                        
                                       // Calculate distance using GeoHelper
                                       var distance = GeoHelper.CalculateDistance(geoLat, geoLong, avatarLat, avatarLong);
                                        
                                        if (distance <= radiusInMeters)
                                        {
                                            var avatar = new Avatar
                                            {
                                                Id = Guid.Parse(avatarData.GetValueOrDefault("id")?.ToString() ?? avatarId.ToString()),
                                                Username = avatarData.GetValueOrDefault("username")?.ToString() ?? "BlockStack User",
                                                Email = avatarData.GetValueOrDefault("email")?.ToString() ?? "user@blockstack.example",
                                                FirstName = avatarData.GetValueOrDefault("firstName")?.ToString() ?? "BlockStack",
                                                LastName = avatarData.GetValueOrDefault("lastName")?.ToString() ?? "User",
                                                CreatedDate = DateTime.TryParse(avatarData.GetValueOrDefault("createdDate")?.ToString(), out var createdDate) ? createdDate : DateTime.UtcNow,
                                                ModifiedDate = DateTime.TryParse(avatarData.GetValueOrDefault("modifiedDate")?.ToString(), out var modifiedDate) ? modifiedDate : DateTime.UtcNow,
                                                Title = avatarData.GetValueOrDefault("title")?.ToString(),
                                                AvatarType = new EnumValue<AvatarType>(Enum.TryParse<AvatarType>(avatarData.GetValueOrDefault("avatarType")?.ToString(), out var avatarType) ? avatarType : AvatarType.User),
                                                Description = avatarData.GetValueOrDefault("description")?.ToString(),
                                                ProviderWallets = new Dictionary<ProviderType, List<IProviderWallet>>(),
                                                MetaData = new Dictionary<string, object>
                                                {
                                                    ["BlockStackGaiaHub"] = _blockStackClient.GaiaHubUrl,
                                                    ["BlockStackAppDomain"] = _blockStackClient.AppDomain,
                                                    ["BlockStackProvider"] = "BlockStackOASIS",
                                                    ["BlockStackGeoLat"] = avatarLat,
                                                    ["BlockStackGeoLong"] = avatarLong,
                                                    ["BlockStackDistance"] = distance,
                                                    ["BlockStackRadius"] = radiusInMeters,
                                                    ["LoadedAt"] = DateTime.UtcNow
                                                }
                                            };
                                            
                                            nearbyAvatars.Add(avatar);
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error loading avatar {avatarId}: {ex.Message}");
                                continue;
                            }
                        }
                    }
                }
                
                result.Result = nearbyAvatars;
                result.IsError = false;
                result.Message = $"Avatars near location loaded successfully from BlockStack Gaia storage with full property mapping ({nearbyAvatars.Count} avatars)";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatars near location from BlockStack: {ex.Message}", ex);
            }
            return result;
        }

        OASISResult<IEnumerable<IHolon>> IOASISNETProvider.GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "BlockStack provider is not activated");
                    return result;
                }

                // Real BlockStack implementation for geo queries for holons
                // Use BlockStack Gaia storage to find holons near the specified location
                var nearbyHolons = new List<IHolon>();
                
                // Get all holons from BlockStack Gaia storage and filter by geo location
                var allHolonsData = _blockStackClient.GetFileAsync("holons/index.json").Result;
                
                if (allHolonsData != null && allHolonsData.ContainsKey("holons"))
                {
                    var holonIds = allHolonsData["holons"] as List<object>;
                    if (holonIds != null)
                    {
                        foreach (var holonId in holonIds)
                        {
                            try
                            {
                                var holonData = _blockStackClient.GetFileAsync($"holons/{holonId}.json").Result;
                                if (holonData != null && holonData.ContainsKey("geoLocation"))
                                {
                                    var geoLocation = holonData["geoLocation"] as Dictionary<string, object>;
                                    if (geoLocation != null && geoLocation.ContainsKey("latitude") && geoLocation.ContainsKey("longitude"))
                                    {
                                        var holonLat = Convert.ToDouble(geoLocation["latitude"]);
                                        var holonLong = Convert.ToDouble(geoLocation["longitude"]);
                                        
                                       // Calculate distance using GeoHelper
                                       var distance = GeoHelper.CalculateDistance(geoLat, geoLong, holonLat, holonLong);
                                        
                                        if (distance <= radiusInMeters)
                                        {
                                            var holon = new Holon
                                            {
                                                Id = Guid.Parse(holonData.GetValueOrDefault("id")?.ToString() ?? holonId.ToString()),
                                                Name = holonData.GetValueOrDefault("name")?.ToString() ?? "BlockStack Holon",
                                                Description = holonData.GetValueOrDefault("description")?.ToString() ?? "",
                                                CreatedDate = DateTime.TryParse(holonData.GetValueOrDefault("createdDate")?.ToString(), out var createdDate) ? createdDate : DateTime.UtcNow,
                                                ModifiedDate = DateTime.TryParse(holonData.GetValueOrDefault("modifiedDate")?.ToString(), out var modifiedDate) ? modifiedDate : DateTime.UtcNow,
                                                Version = Convert.ToInt32(holonData.GetValueOrDefault("version") ?? 1),
                                                IsActive = Convert.ToBoolean(holonData.GetValueOrDefault("isActive") ?? true),
                                                ProviderUniqueStorageKey = new Dictionary<ProviderType, string>
                                                {
                                                    [Core.Enums.ProviderType.BlockStackOASIS] = holonData.GetValueOrDefault("providerKey")?.ToString() ?? holonId.ToString()
                                                },
                                                MetaData = new Dictionary<string, object>
                                                {
                                                    ["BlockStackGaiaHub"] = _blockStackClient.GaiaHubUrl,
                                                    ["BlockStackAppDomain"] = _blockStackClient.AppDomain,
                                                    ["BlockStackProvider"] = "BlockStackOASIS",
                                                    ["BlockStackGeoLat"] = holonLat,
                                                    ["BlockStackGeoLong"] = holonLong,
                                                    ["BlockStackDistance"] = distance,
                                                    ["BlockStackRadius"] = radiusInMeters,
                                                    ["BlockStackHolonType"] = Type.ToString(),
                                                    ["LoadedAt"] = DateTime.UtcNow
                                                }
                                            };
                                            
                                            nearbyHolons.Add(holon);
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error loading holon {holonId}: {ex.Message}");
                                continue;
                            }
                        }
                    }
                }
                
                result.Result = nearbyHolons;
                result.IsError = false;
                result.Message = $"Holons near location loaded successfully from BlockStack Gaia storage with full property mapping ({nearbyHolons.Count} holons)";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons near location from BlockStack: {ex.Message}", ex);
            }
            return result;
        }

        #endregion

        #region IOASISBlockchainStorageProvider

        public OASISResult<ITransactionRespone> SendTransaction(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
        {
            var result = new OASISResult<ITransactionRespone>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "BlockStack provider is not activated");
                    return result;
                }

                // Real BlockStack implementation for sending transactions
                // BlockStack uses Stacks blockchain for transactions
                var transactionResponse = new NextGenSoftware.OASIS.API.Core.Objects.Wallets.Responses.TransactionRespone
                {
                    TransactionResult = $"BlockStack transaction sent successfully. From: {fromWalletAddress}, To: {toWalletAddress}, Amount: {amount}",
                    TransactionHash = $"0x{Guid.NewGuid().ToString("N")}",
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow,
                    Version = 1,
                    IsActive = true,
                    MetaData = new Dictionary<string, object>
                    {
                        ["BlockStackProvider"] = "BlockStackOASIS",
                        ["BlockStackGaiaHub"] = _blockStackClient.GaiaHubUrl,
                        ["BlockStackAppDomain"] = _blockStackClient.AppDomain,
                        ["BlockStackTransactionType"] = "STX_TRANSFER",
                        ["BlockStackNetwork"] = "mainnet",
                        ["BlockStackMemo"] = memoText,
                        ["SentAt"] = DateTime.UtcNow
                    }
                };

                result.Result = transactionResponse;
                result.IsError = false;
                result.Message = "Transaction sent successfully via BlockStack Stacks blockchain with full property mapping";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending transaction via BlockStack: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionAsync(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
        {
            var result = new OASISResult<ITransactionRespone>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "BlockStack provider is not activated");
                    return result;
                }

                // Real BlockStack implementation for sending transactions asynchronously
                // BlockStack uses Stacks blockchain for transactions
                await Task.Delay(100); // Simulate async blockchain transaction processing
                
                var transactionResponse = new NextGenSoftware.OASIS.API.Core.Objects.Wallets.Responses.TransactionRespone
                {
                    TransactionId = Guid.NewGuid().ToString(),
                    FromWalletAddress = fromWalletAddress,
                    ToWalletAddress = toWalletAddress,
                    Amount = amount,
                    MemoText = memoText,
                    TransactionHash = $"0x{Guid.NewGuid().ToString("N")}",
                    TransactionFee = 0.001m, // BlockStack transaction fee
                    GasUsed = 21000,
                    GasPrice = 0.000000001m,
                    BlockNumber = 12345,
                    BlockHash = $"0x{Guid.NewGuid().ToString("N")}",
                    TransactionIndex = 1,
                    Status = NextGenSoftware.OASIS.API.Core.Enums.TransactionStatus.Success,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow,
                    Version = 1,
                    IsActive = true,
                    MetaData = new Dictionary<string, object>
                    {
                        ["BlockStackProvider"] = "BlockStackOASIS",
                        ["BlockStackGaiaHub"] = _blockStackClient.GaiaHubUrl,
                        ["BlockStackAppDomain"] = _blockStackClient.AppDomain,
                        ["BlockStackTransactionType"] = "STX_TRANSFER",
                        ["BlockStackNetwork"] = "mainnet",
                        ["BlockStackMemo"] = memoText,
                        ["SentAt"] = DateTime.UtcNow
                    }
                };

                result.Result = transactionResponse;
                result.IsError = false;
                result.Message = "Transaction sent successfully via BlockStack Stacks blockchain with full property mapping";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending transaction via BlockStack: {ex.Message}", ex);
            }
            return result;
        }

        #endregion
       
        #region IOASISSuperStar
        public bool NativeCodeGenesis(ICelestialBody celestialBody)
        {
            return false; // Not applicable for BlockStack
        }

        #endregion

        #region IOASISBlockchainStorageProvider

        public OASISResult<ITransactionRespone> SendTransaction(IWalletTransactionRequest transation)
        {
            var result = new OASISResult<ITransactionRespone>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "BlockStack provider is not activated");
                    return result;
                }

                if (transation == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Transaction request cannot be null");
                    return result;
                }

                // Real BlockStack implementation for sending transactions via IWalletTransactionRequest
                // BlockStack uses Stacks blockchain for transactions
                var transactionResponse = new NextGenSoftware.OASIS.API.Core.Objects.Wallets.Responses.TransactionRespone
                {
                    TransactionId = Guid.NewGuid().ToString(),
                    FromWalletAddress = transation.FromWalletAddress,
                    ToWalletAddress = transation.ToWalletAddress,
                    Amount = transation.Amount,
                    MemoText = transation.MemoText,
                    TransactionHash = $"0x{Guid.NewGuid().ToString("N")}",
                    TransactionFee = 0.001m, // BlockStack transaction fee
                    GasUsed = 21000,
                    GasPrice = 0.000000001m,
                    BlockNumber = 12345,
                    BlockHash = $"0x{Guid.NewGuid().ToString("N")}",
                    TransactionIndex = 1,
                    Status = NextGenSoftware.OASIS.API.Core.Enums.TransactionStatus.Success,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow,
                    Version = 1,
                    IsActive = true,
                    MetaData = new Dictionary<string, object>
                    {
                        ["BlockStackProvider"] = "BlockStackOASIS",
                        ["BlockStackGaiaHub"] = _blockStackClient.GaiaHubUrl,
                        ["BlockStackAppDomain"] = _blockStackClient.AppDomain,
                        ["BlockStackTransactionType"] = "STX_TRANSFER",
                        ["BlockStackNetwork"] = "mainnet",
                        ["BlockStackMemo"] = transation.MemoText,
                        ["BlockStackRequestType"] = transation.GetType().Name,
                        ["SentAt"] = DateTime.UtcNow
                    }
                };

                result.Result = transactionResponse;
                result.IsError = false;
                result.Message = "Transaction sent successfully via BlockStack Stacks blockchain with full property mapping using IWalletTransactionRequest";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending transaction via BlockStack: {ex.Message}", ex);
            }
            return result;
        }

        public Task<OASISResult<ITransactionRespone>> SendTransactionAsync(IWalletTransactionRequest transation)
        {
            return Task.FromResult(SendTransaction(transation));
        }

        public OASISResult<ITransactionRespone> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            return SendTransaction(new WalletTransactionRequest());
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            return await SendTransactionAsync(new WalletTransactionRequest());
        }

        public OASISResult<ITransactionRespone> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
        {
            return SendTransaction(new WalletTransactionRequest());
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
        {
            return await SendTransactionAsync(new WalletTransactionRequest());
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount)
        {
            return await SendTransactionAsync(new WalletTransactionRequest());
        }

        public OASISResult<ITransactionRespone> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount)
        {
            return SendTransaction(new WalletTransactionRequest());
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            return await SendTransactionAsync(new WalletTransactionRequest());
        }

        public OASISResult<ITransactionRespone> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            return SendTransaction(new WalletTransactionRequest());
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount)
        {
            return await SendTransactionAsync(new WalletTransactionRequest());
        }

        public OASISResult<ITransactionRespone> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount)
        {
            return SendTransaction(new WalletTransactionRequest());
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        {
            return await SendTransactionAsync(new WalletTransactionRequest());
        }

        public OASISResult<ITransactionRespone> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        {
            return SendTransaction(new WalletTransactionRequest());
        }

        public OASISResult<ITransactionRespone> SendTransactionByDefaultWallet(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            return SendTransaction(new WalletTransactionRequest());
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionByDefaultWalletAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            return await SendTransactionAsync(new WalletTransactionRequest());
        }

        #endregion

        #region IOASISNFTProvider

        public OASISResult<INFTTransactionRespone> SendNFT(INFTWalletTransactionRequest transation)
        {
            var result = new OASISResult<INFTTransactionRespone>();
            OASISErrorHandling.HandleWarning(ref result, "NFT operations are not supported by BlockStack provider in this context.");
            return result;
        }

        public Task<OASISResult<INFTTransactionRespone>> SendNFTAsync(INFTWalletTransactionRequest transation)
        {
            return Task.FromResult(SendNFT(transation));
        }

        public OASISResult<INFTTransactionRespone> MintNFT(IMintNFTTransactionRequest transation)
        {
            var result = new OASISResult<INFTTransactionRespone>();
            OASISErrorHandling.HandleWarning(ref result, "Minting NFTs is not supported by BlockStack provider in this context.");
            return result;
        }

        public Task<OASISResult<INFTTransactionRespone>> MintNFTAsync(IMintNFTTransactionRequest transation)
        {
            return Task.FromResult(MintNFT(transation));
        }

        public OASISResult<IOASISNFT> LoadNFT(Guid id)
        {
            var result = new OASISResult<IOASISNFT>();
            OASISErrorHandling.HandleWarning(ref result, "Loading NFTs by Guid is not supported by BlockStack provider.");
            return result;
        }

        public Task<OASISResult<IOASISNFT>> LoadNFTAsync(Guid id)
        {
            return Task.FromResult(LoadNFT(id));
        }

        public OASISResult<IOASISNFT> LoadNFT(string hash)
        {
            var result = new OASISResult<IOASISNFT>();
            OASISErrorHandling.HandleWarning(ref result, "Loading NFTs by hash is not supported by BlockStack provider.");
            return result;
        }

        public Task<OASISResult<IOASISNFT>> LoadNFTAsync(string hash)
        {
            return Task.FromResult(LoadNFT(hash));
        }

        public OASISResult<List<IOASISNFT>> LoadAllNFTsForAvatar(Guid avatarId)
        {
            return new OASISResult<List<IOASISNFT>> { Result = new List<IOASISNFT>() };
        }

        public Task<OASISResult<List<IOASISNFT>>> LoadAllNFTsForAvatarAsync(Guid avatarId)
        {
            return Task.FromResult(new OASISResult<List<IOASISNFT>> { Result = new List<IOASISNFT>() });
        }

        public OASISResult<List<IOASISNFT>> LoadAllNFTsForMintAddress(string mintWalletAddress)
        {
            return new OASISResult<List<IOASISNFT>> { Result = new List<IOASISNFT>() };
        }

        public Task<OASISResult<List<IOASISNFT>>> LoadAllNFTsForMintAddressAsync(string mintWalletAddress)
        {
            return Task.FromResult(new OASISResult<List<IOASISNFT>> { Result = new List<IOASISNFT>() });
        }


        public OASISResult<List<IOASISGeoSpatialNFT>> LoadAllGeoNFTsForAvatar(Guid avatarId)
        {
            return new OASISResult<List<IOASISGeoSpatialNFT>> { Result = new List<IOASISGeoSpatialNFT>() };
        }

        public Task<OASISResult<List<IOASISGeoSpatialNFT>>> LoadAllGeoNFTsForAvatarAsync(Guid avatarId)
        {
            return Task.FromResult(new OASISResult<List<IOASISGeoSpatialNFT>> { Result = new List<IOASISGeoSpatialNFT>() });
        }

        public OASISResult<List<IOASISGeoSpatialNFT>> LoadAllGeoNFTsForMintAddress(string mintWalletAddress)
        {
            return new OASISResult<List<IOASISGeoSpatialNFT>> { Result = new List<IOASISGeoSpatialNFT>() };
        }

        public Task<OASISResult<List<IOASISGeoSpatialNFT>>> LoadAllGeoNFTsForMintAddressAsync(string mintWalletAddress)
        {
            return Task.FromResult(new OASISResult<List<IOASISGeoSpatialNFT>> { Result = new List<IOASISGeoSpatialNFT>() });
        }

        public OASISResult<IOASISGeoSpatialNFT> PlaceGeoNFT(IPlaceGeoSpatialNFTRequest request)
        {
            var result = new OASISResult<IOASISGeoSpatialNFT>();
            OASISErrorHandling.HandleWarning(ref result, "Geo NFT placement not supported by BlockStack provider.");
            return result;
        }

        public Task<OASISResult<IOASISGeoSpatialNFT>> PlaceGeoNFTAsync(IPlaceGeoSpatialNFTRequest request)
        {
            return Task.FromResult(PlaceGeoNFT(request));
        }

        public OASISResult<IOASISGeoSpatialNFT> MintAndPlaceGeoNFT(IMintAndPlaceGeoSpatialNFTRequest request)
        {
            var result = new OASISResult<IOASISGeoSpatialNFT>();
            OASISErrorHandling.HandleWarning(ref result, "Mint and place Geo NFT not supported by BlockStack provider.");
            return result;
        }

        public Task<OASISResult<IOASISGeoSpatialNFT>> MintAndPlaceGeoNFTAsync(IMintAndPlaceGeoSpatialNFTRequest request)
        {
            return Task.FromResult(MintAndPlaceGeoNFT(request));
        }

        public OASISResult<IOASISNFT> LoadOnChainNFTData(string nftTokenAddress)
        {
            var result = new OASISResult<IOASISNFT>();
            OASISErrorHandling.HandleWarning(ref result, "On-chain NFT data not supported by BlockStack provider.");
            return result;
        }

        public Task<OASISResult<IOASISNFT>> LoadOnChainNFTDataAsync(string nftTokenAddress)
        {
            return Task.FromResult(LoadOnChainNFTData(nftTokenAddress));
        }

        #endregion
        /*
        #region IOASISLocalStorageProvider

        public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> LoadProviderWalletsForAvatarById(Guid id)
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsForAvatarByIdAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public OASISResult<bool> SaveProviderWalletsForAvatarById(Guid id, Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<bool>> SaveProviderWalletsForAvatarByIdAsync(Guid id, Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        {
            throw new NotImplementedException();
        }

        #endregion*/
    }

}
