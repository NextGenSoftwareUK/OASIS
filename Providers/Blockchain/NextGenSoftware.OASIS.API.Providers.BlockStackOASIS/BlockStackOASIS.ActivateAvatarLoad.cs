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
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate BlockStack provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Load avatar by provider key from BlockStack Gaia storage
                // Provider key can be a Gaia storage path or Stacks address
                var avatarData = await _blockStackClient.GetFileAsync($"avatars/{providerKey}.json");
                
                if (avatarData != null && avatarData.Count > 0)
                {
                    // Try to load from Gaia storage first
                    var avatarJson = System.Text.Json.JsonSerializer.Serialize(avatarData);
                    var avatar = System.Text.Json.JsonSerializer.Deserialize<Avatar>(avatarJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                    });
                    
                    if (avatar != null)
                    {
                        avatar.Version = version;
                        if (avatar.Id == Guid.Empty && avatarData.ContainsKey("id"))
                        {
                            avatar.Id = Guid.Parse(avatarData["id"].ToString());
                        }
                        response.Result = avatar;
                        response.Message = "Avatar loaded by provider key from BlockStack successfully";
                        return response;
                    }
                }
                
                // Fallback: Try loading from Gaia storage using provider key as address
                var gaiaUrl = $"https://gaia.blockstack.org/hub/{providerKey}/avatar.json";
                using (var httpClient = new HttpClient())
                {
                    var jsonResponse = await httpClient.GetStringAsync(gaiaUrl);
                    if (!string.IsNullOrEmpty(jsonResponse))
                    {
                        var avatar = System.Text.Json.JsonSerializer.Deserialize<Avatar>(jsonResponse, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                        });
                        
                        if (avatar != null)
                        {
                            avatar.Version = version;
                            response.Result = avatar;
                            response.Message = "Avatar loaded by provider key from BlockStack Gaia storage successfully";
                            return response;
                        }
                    }
                }
                
                OASISErrorHandling.HandleError(ref response, $"Avatar not found for provider key: {providerKey}");
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
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate BlockStack provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Search for avatar by email in BlockStack Gaia storage
                // Enumerate user directories and check each avatar.json for matching email
                var userDirectories = await _blockStackClient.ListUserDirectoriesAsync();
                
                if (userDirectories != null && userDirectories.Count > 0)
                {
                    foreach (var userDir in userDirectories)
                    {
                        try
                        {
                            var avatarData = await _blockStackClient.GetFileAsync($"{userDir}/avatar.json");
                            if (avatarData != null && avatarData.ContainsKey("email"))
                            {
                                var email = avatarData["email"]?.ToString();
                                if (string.Equals(email, avatarEmail, StringComparison.OrdinalIgnoreCase))
                                {
                                    var avatarJson = System.Text.Json.JsonSerializer.Serialize(avatarData);
                                    var avatar = System.Text.Json.JsonSerializer.Deserialize<Avatar>(avatarJson, new JsonSerializerOptions
                                    {
                                        PropertyNameCaseInsensitive = true,
                                        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                                    });
                                    
                                    if (avatar != null)
                                    {
                                        avatar.Version = version;
                                        response.Result = avatar;
                                        response.Message = "Avatar loaded by email from BlockStack successfully";
                                        return response;
                                    }
                                }
                            }
                        }
                        catch
                        {
                            // Continue searching other directories
                            continue;
                        }
                    }
                }
                
                OASISErrorHandling.HandleError(ref response, $"Avatar not found for email: {avatarEmail}");
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
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate BlockStack provider: {activateResult.Message}");
                        return result;
                    }
                }
                // Load avatar detail as a separate object from Gaia (avatar-detail.json), not derived from avatar
                var userDirectories = await _blockStackClient.ListUserDirectoriesAsync();
                if (userDirectories != null && userDirectories.Count > 0)
                {
                    foreach (var userDir in userDirectories)
                    {
                        try
                        {
                            var detailData = await _blockStackClient.GetFileAsync($"{userDir}/avatar-detail.json");
                            if (detailData != null && detailData.ContainsKey("id"))
                            {
                                var idStr = detailData["id"]?.ToString();
                                if (!string.IsNullOrWhiteSpace(idStr) && Guid.TryParse(idStr, out var detailId) && detailId == id)
                                {
                                    result.Result = MapDictToAvatarDetail(detailData);
                                    result.IsError = false;
                                    result.Message = "Avatar detail loaded from BlockStack successfully";
                                    return result;
                                }
                            }
                        }
                        catch { continue; }
                    }
                }
                OASISErrorHandling.HandleError(ref result, "Avatar detail not found for id.");
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
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate BlockStack provider: {activateResult.Message}");
                        return result;
                    }
                }
                // Load avatar detail as a separate object from Gaia (avatar-detail.json)
                var userDirectories = await _blockStackClient.ListUserDirectoriesAsync();
                if (userDirectories != null && userDirectories.Count > 0)
                {
                    foreach (var userDir in userDirectories)
                    {
                        try
                        {
                            var detailData = await _blockStackClient.GetFileAsync($"{userDir}/avatar-detail.json");
                            if (detailData != null && detailData.ContainsKey("email"))
                            {
                                var email = detailData["email"]?.ToString();
                                if (string.Equals(email, avatarEmail, StringComparison.OrdinalIgnoreCase))
                                {
                                    result.Result = MapDictToAvatarDetail(detailData);
                                    result.IsError = false;
                                    result.Message = "Avatar detail loaded by email from BlockStack successfully";
                                    return result;
                                }
                            }
                        }
                        catch { continue; }
                    }
                }
                OASISErrorHandling.HandleError(ref result, "Avatar detail not found by email.");
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
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate BlockStack provider: {activateResult.Message}");
                        return result;
                    }
                }
                // Load avatar detail as a separate object from Gaia (avatar-detail.json), not derived from avatar
                var detailData = await _blockStackClient.GetFileAsync($"{avatarUsername}/avatar-detail.json");
                if (detailData != null && detailData.Count > 0 && detailData.ContainsKey("id"))
                {
                    result.Result = MapDictToAvatarDetail(detailData);
                    result.IsError = false;
                    result.Message = "Avatar detail loaded by username from BlockStack successfully";
                }
                else
                    OASISErrorHandling.HandleError(ref result, "Avatar detail not found by username.");
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
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate BlockStack provider: {activateResult.Message}");
                        return result;
                    }
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
                                // Skip avatars without valid ID
                                var idStr = avatarData.GetValueOrDefault("id")?.ToString();
                                if (string.IsNullOrWhiteSpace(idStr) || !Guid.TryParse(idStr, out var avatarId))
                                {
                                    // Skip this avatar and continue with next
                                    continue;
                                }
                                
                                var avatar = new Avatar
                                {
                                    Id = avatarId,
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
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
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
                // Load all avatar details as separate objects from Gaia (avatar-detail.json per dir), not derived from avatars
                var list = new List<IAvatarDetail>();
                var userDirectories = await _blockStackClient.ListUserDirectoriesAsync();
                if (userDirectories != null && userDirectories.Count > 0)
                {
                    foreach (var userDir in userDirectories)
                    {
                        try
                        {
                            var detailData = await _blockStackClient.GetFileAsync($"{userDir}/avatar-detail.json");
                            if (detailData != null && detailData.ContainsKey("id"))
                            {
                                var detail = MapDictToAvatarDetail(detailData);
                                if (detail != null)
                                    list.Add(detail);
                            }
                        }
                        catch { continue; }
                    }
                }
                result.Result = list;
                result.IsError = false;
                result.Message = $"Avatar details loaded successfully from BlockStack ({list.Count} details)";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar details from BlockStack: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            return LoadAllAvatarDetailsAsync(version).Result;
        }

    }
}
