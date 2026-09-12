using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
// using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request; // Removed - use Requests (plural) instead
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.MoralisOASIS
{
    public partial class MoralisOASIS
    {
        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string email, bool softDelete = true)
        {
            try
            {
                // First find avatar by email, then delete it
                var avatarResult = await LoadAvatarByEmailAsync(email);
                if (avatarResult.IsError || avatarResult.Result == null)
                {
                    return new OASISResult<bool>(false) { Message = "Avatar not found" };
                }
                return await DeleteAvatarAsync(avatarResult.Result.Id, softDelete);
            }
            catch (Exception ex)
            {
                var result = new OASISResult<bool>(false);
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by email: {ex.Message}", ex);
                return result;
            }
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string email, bool softDelete = true)
        {
            return DeleteAvatarByEmailAsync(email, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string username, bool softDelete = true)
        {
            try
            {
                // First find avatar by username, then delete it
                var avatarResult = await LoadAvatarByUsernameAsync(username);
                if (avatarResult.IsError || avatarResult.Result == null)
                {
                    return new OASISResult<bool>(false) { Message = "Avatar not found" };
                }
                return await DeleteAvatarAsync(avatarResult.Result.Id, softDelete);
            }
            catch (Exception ex)
            {
                var result = new OASISResult<bool>(false);
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by username: {ex.Message}", ex);
                return result;
            }
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string username, bool softDelete = true)
        {
            return DeleteAvatarByUsernameAsync(username, softDelete).Result;
        }

        // AvatarDetail Methods
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            try
            {
                // Load avatar detail from Moralis API
                var avatarResult = await LoadAvatarAsync(id, version);
                if (avatarResult.IsError || avatarResult.Result == null)
                {
                    return new OASISResult<IAvatarDetail>(null) { Message = "Avatar not found" };
                }
                
                // Convert Avatar to AvatarDetail
                var avatarDetail = new AvatarDetail
                {
                    Id = avatarResult.Result.Id,
                    Username = avatarResult.Result.Username,
                    Email = avatarResult.Result.Email,
                    FirstName = avatarResult.Result.FirstName,
                    LastName = avatarResult.Result.LastName,
                    Version = version
                };
                
                return new OASISResult<IAvatarDetail>(avatarDetail) { Message = "Avatar detail loaded successfully from Moralis Web3 API" };
            }
            catch (Exception ex)
            {
                var result = new OASISResult<IAvatarDetail>(null);
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail: {ex.Message}", ex);
                return result;
            }
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            return LoadAvatarDetailAsync(id, version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string email, int version = 0)
        {
            try
            {
                // Load avatar by email, then convert to avatar detail
                var avatarResult = await LoadAvatarByEmailAsync(email, version);
                if (avatarResult.IsError || avatarResult.Result == null)
                {
                    return new OASISResult<IAvatarDetail>(null) { Message = "Avatar not found" };
                }
                
                var avatarDetail = new AvatarDetail
                {
                    Id = avatarResult.Result.Id,
                    Username = avatarResult.Result.Username,
                    Email = avatarResult.Result.Email,
                    FirstName = avatarResult.Result.FirstName,
                    LastName = avatarResult.Result.LastName,
                    Version = version
                };
                
                return new OASISResult<IAvatarDetail>(avatarDetail) { Message = "Avatar detail loaded successfully from Moralis Web3 API" };
            }
            catch (Exception ex)
            {
                var result = new OASISResult<IAvatarDetail>(null);
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by email: {ex.Message}", ex);
                return result;
            }
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string email, int version = 0)
        {
            return LoadAvatarDetailByEmailAsync(email, version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string username, int version = 0)
        {
            try
            {
                // Load avatar by username, then convert to avatar detail
                var avatarResult = await LoadAvatarByUsernameAsync(username, version);
                if (avatarResult.IsError || avatarResult.Result == null)
                {
                    return new OASISResult<IAvatarDetail>(null) { Message = "Avatar not found" };
                }
                
                var avatarDetail = new AvatarDetail
                {
                    Id = avatarResult.Result.Id,
                    Username = avatarResult.Result.Username,
                    Email = avatarResult.Result.Email,
                    FirstName = avatarResult.Result.FirstName,
                    LastName = avatarResult.Result.LastName,
                    Version = version
                };
                
                return new OASISResult<IAvatarDetail>(avatarDetail) { Message = "Avatar detail loaded successfully from Moralis Web3 API" };
            }
            catch (Exception ex)
            {
                var result = new OASISResult<IAvatarDetail>(null);
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by username: {ex.Message}", ex);
                return result;
            }
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string username, int version = 0)
        {
            return LoadAvatarDetailByUsernameAsync(username, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            try
            {
                // Load avatar details as separate entities (do not build from Avatar)
                var avatarsResult = await LoadAllAvatarsAsync(version);
                if (avatarsResult.IsError || avatarsResult.Result == null)
                {
                    return new OASISResult<IEnumerable<IAvatarDetail>>(new List<IAvatarDetail>()) { Message = "No avatars found" };
                }
                var avatarDetails = new List<IAvatarDetail>();
                foreach (var avatar in avatarsResult.Result)
                {
                    var detailResult = await LoadAvatarDetailAsync(avatar.Id, version);
                    if (!detailResult.IsError && detailResult.Result != null)
                        avatarDetails.Add(detailResult.Result);
                }
                return new OASISResult<IEnumerable<IAvatarDetail>>(avatarDetails) { Message = $"Loaded {avatarDetails.Count} avatar details from Moralis Web3 API" };
            }
            catch (Exception ex)
            {
                var result = new OASISResult<IEnumerable<IAvatarDetail>>(new List<IAvatarDetail>());
                OASISErrorHandling.HandleError(ref result, $"Error loading all avatar details: {ex.Message}", ex);
                return result;
            }
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            return LoadAllAvatarDetailsAsync(version).Result;
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Moralis provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (avatarDetail == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar detail cannot be null");
                    return result;
                }

                // Real Moralis implementation - save avatar detail to IPFS
                var avatarDetailJson = JsonSerializer.Serialize(avatarDetail, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                var avatarDetailBytes = Encoding.UTF8.GetBytes(avatarDetailJson);
                var base64Content = Convert.ToBase64String(avatarDetailBytes);
                
                var requestBody = new
                {
                    path = $"avatar_detail_{avatarDetail.Id}.json",
                    content = base64Content
                };

                var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_baseUrl}/ipfs/uploadFolder", jsonContent);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var ipfsResult = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    if (ipfsResult.TryGetProperty("path", out var path))
                    {
                        var ipfsPath = path.GetString();
                        
                        // Store IPFS path in avatar detail if it has ProviderUniqueStorageKey
                        if (avatarDetail is IHolonBase holonBase)
                        {
                            if (holonBase.ProviderUniqueStorageKey == null)
                                holonBase.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
                            holonBase.ProviderUniqueStorageKey[Core.Enums.ProviderType.MoralisOASIS] = ipfsPath;
                        }

                        result.Result = avatarDetail;
                        result.IsError = false;
                        result.IsSaved = true;
                        result.Message = $"Avatar detail saved to Moralis IPFS successfully. Path: {ipfsPath}";
                        return result;
                    }
                }

                OASISErrorHandling.HandleError(ref result, "Failed to save avatar detail to Moralis IPFS");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving avatar detail: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail)
        {
            return SaveAvatarDetailAsync(avatarDetail).Result;
        }

        // Holon Methods
        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool reloadChildren = true, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Moralis provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Real Moralis implementation - load holon from IPFS
                // First try to load from IPFS using the holon ID
                var ipfsPath = $"holon_{id}.json";
                var resolveRequest = new
                {
                    path = ipfsPath
                };

                var jsonContent = new StringContent(JsonSerializer.Serialize(resolveRequest), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_baseUrl}/ipfs/resolve", jsonContent);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var ipfsResult = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    if (ipfsResult.TryGetProperty("content", out var content))
                    {
                        var base64Content = content.GetString();
                        var holonBytes = Convert.FromBase64String(base64Content);
                        var holonJson = Encoding.UTF8.GetString(holonBytes);
                        var holon = JsonSerializer.Deserialize<Holon>(holonJson);
                        
                        result.Result = holon;
                        result.IsError = false;
                        result.IsLoaded = true;
                        result.Message = "Holon loaded successfully from Moralis IPFS";

                        // Load children if requested
                        if (loadChildren && holon.Children != null && holon.Children.Any() && maxChildDepth > 0)
                        {
                            var childResults = new List<IHolon>();
                            foreach (var childId in holon.Children.Select(c => c.Id))
                            {
                                var childResult = await LoadHolonAsync(childId, loadChildren, recursive, maxChildDepth - 1, continueOnError, reloadChildren, version);
                                if (!childResult.IsError && childResult.Result != null)
                                {
                                    childResults.Add(childResult.Result);
                                }
                                else if (!continueOnError)
                                {
                                    OASISErrorHandling.HandleError(ref result, $"Failed to load child holon {childId}: {childResult.Message}");
                                    return result;
                                }
                            }
                            holon.Children = childResults;
                        }

                        return result;
                    }
                }

                // Fallback: Try loading from contract if available
                var holonData = await LoadHolonFromMoralisAsync(id.ToString(), version);
                if (holonData != null)
                {
                    var holon = JsonSerializer.Deserialize<Holon>(holonData);
                    result.Result = holon;
                    result.IsError = false;
                    result.IsLoaded = true;
                    result.Message = "Holon loaded successfully from Moralis contract";
                    return result;
                }

                OASISErrorHandling.HandleError(ref result, "Holon not found in Moralis IPFS or contract");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holon: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool reloadChildren = true, int version = 0)
        {
            return LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, reloadChildren, version).Result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool reloadChildren = true, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Moralis provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Real Moralis implementation - load holon by provider key (IPFS path) from IPFS
                if (providerKey.StartsWith("ipfs://"))
                {
                    var ipfsHash = providerKey.Replace("ipfs://", "");
                    var resolveRequest = new
                    {
                        path = ipfsHash
                    };

                    var jsonContent = new StringContent(JsonSerializer.Serialize(resolveRequest), Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync($"{_baseUrl}/ipfs/resolve", jsonContent);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        var ipfsResult = JsonSerializer.Deserialize<JsonElement>(responseContent);
                        
                        if (ipfsResult.TryGetProperty("content", out var content))
                        {
                            var base64Content = content.GetString();
                            var holonBytes = Convert.FromBase64String(base64Content);
                            var holonJson = Encoding.UTF8.GetString(holonBytes);
                            var holon = JsonSerializer.Deserialize<Holon>(holonJson);
                            
                            result.Result = holon;
                            result.IsError = false;
                            result.IsLoaded = true;
                            result.Message = "Holon loaded successfully from Moralis IPFS by provider key";

                            // Load children if requested
                            if (loadChildren && holon.Children != null && holon.Children.Any() && maxChildDepth > 0)
                            {
                                var childResults = new List<IHolon>();
                                foreach (var childId in holon.Children.Select(c => c.Id))
                                {
                                    var childResult = await LoadHolonAsync(childId, loadChildren, recursive, maxChildDepth - 1, continueOnError, reloadChildren, version);
                                    if (!childResult.IsError && childResult.Result != null)
                                    {
                                        childResults.Add(childResult.Result);
                                    }
                                    else if (!continueOnError)
                                    {
                                        OASISErrorHandling.HandleError(ref result, $"Failed to load child holon {childId}: {childResult.Message}");
                                        return result;
                                    }
                                }
                                holon.Children = childResults;
                            }

                            return result;
                        }
                    }
                }
                else
                {
                    // Try loading from contract if provider key is a transaction hash or contract reference
                    if (!string.IsNullOrEmpty(GetOASISContractAddress()))
                    {
                        var contractRequest = new
                        {
                            address = GetOASISContractAddress(),
                            function_name = "getHolonByProviderKey",
                            abi = GetOASISContractABI(),
                            @params = new { providerKey = providerKey, version = version }
                        };

                        var contractResponse = await _httpClient.PostAsync($"{_baseUrl}/{Uri.EscapeDataString(GetOASISContractAddress())}/function",
                            new StringContent(JsonSerializer.Serialize(contractRequest), Encoding.UTF8, "application/json"));

                        if (contractResponse.IsSuccessStatusCode)
                        {
                            var contractContent = await contractResponse.Content.ReadAsStringAsync();
                            var contractResult = JsonSerializer.Deserialize<MoralisApiResult>(contractContent);
                            if (!string.IsNullOrEmpty(contractResult?.result))
                            {
                                var holon = JsonSerializer.Deserialize<Holon>(contractResult.result);
                                result.Result = holon;
                                result.IsError = false;
                                result.IsLoaded = true;
                                result.Message = "Holon loaded successfully from Moralis contract by provider key";
                                return result;
                            }
                        }
                    }
                }

                OASISErrorHandling.HandleError(ref result, "Holon not found by provider key in Moralis IPFS or contract");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holon by provider key: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool reloadChildren = true, int version = 0)
        {
            return LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, reloadChildren, version).Result;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool reloadChildren = true)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Moralis provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (holon == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Holon cannot be null");
                    return result;
                }

                // Real Moralis implementation - save holon to Web3 API using IPFS
                holon.ModifiedDate = DateTime.UtcNow;
                var ipfsPath = await SaveHolonToMoralisAsync(holon);
                
                if (!string.IsNullOrEmpty(ipfsPath))
                {
                    // Store IPFS path in provider unique storage key
                    if (holon.ProviderUniqueStorageKey == null)
                        holon.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
                    holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.MoralisOASIS] = ipfsPath;

                    result.Result = holon;
                    result.IsError = false;
                    result.IsSaved = true;
                    result.Message = $"Holon saved to Moralis IPFS successfully. Path: {ipfsPath}";

                    // Handle children if requested
                    if (saveChildren && holon.Children != null && holon.Children.Any())
                    {
                        var childResults = new List<OASISResult<IHolon>>();
                        foreach (var child in holon.Children)
                        {
                            var childResult = await SaveHolonAsync(child, saveChildren, recursive, maxChildDepth - 1, continueOnError, reloadChildren);
                            childResults.Add(childResult);
                            
                            if (!continueOnError && childResult.IsError)
                            {
                                OASISErrorHandling.HandleError(ref result, $"Failed to save child holon {child.Id}: {childResult.Message}");
                                return result;
                            }
                        }
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to save holon to Moralis IPFS");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving holon: {ex.Message}", ex);
            }
            return result;
        }

    }
}
