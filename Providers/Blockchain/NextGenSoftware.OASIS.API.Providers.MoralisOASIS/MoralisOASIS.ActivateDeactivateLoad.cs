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

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            try
            {
                // Test API connection
                var response = await _httpClient.GetAsync($"{_baseUrl}/info");
                if (response.IsSuccessStatusCode)
                {
                    IsProviderActivated = true;
                    return new OASISResult<bool>(true);
                }
                else
                {
                    return new OASISResult<bool>(false) { Message = "Failed to connect to Moralis API" };
                }
            }
            catch (Exception ex)
            {
                var result = new OASISResult<bool>(false);
                OASISErrorHandling.HandleError(ref result, $"Error activating Moralis provider: {ex.Message}", ex);
                return result;
            }
        }

        public override OASISResult<bool> ActivateProvider()
        {
            return ActivateProviderAsync().Result;
        }

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            try
            {
                IsProviderActivated = false;
                _httpClient?.Dispose();
                return new OASISResult<bool>(true);
            }
            catch (Exception ex)
            {
                var result = new OASISResult<bool>(false);
                OASISErrorHandling.HandleError(ref result, $"Error deactivating Moralis provider: {ex.Message}", ex);
                return result;
            }
        }

        public override OASISResult<bool> DeActivateProvider()
        {
            return DeActivateProviderAsync().Result;
        }

        // Avatar Methods
        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
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

                // Real Moralis implementation - load avatar from Web3 API
                var avatarData = await LoadAvatarFromMoralisAsync(id.ToString(), version);
                if (avatarData != null)
                {
                    var avatar = JsonSerializer.Deserialize<Avatar>(avatarData);
                    result.Result = avatar;
                    result.IsError = false;
                    result.Message = "Avatar loaded successfully from Moralis Web3 API";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found on Moralis Web3 API");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0)
        {
            return LoadAvatarAsync(id, version).Result;
        }

        public async Task<OASISResult<IAvatar>> LoadAvatarAsync(string providerKey, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
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

                // Real Moralis implementation - use LoadAvatarByProviderKeyAsync which handles IPFS paths
                return await LoadAvatarByProviderKeyAsync(providerKey, version);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by provider key: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IAvatar> LoadAvatar(string providerKey, int version = 0)
        {
            return LoadAvatarAsync(providerKey, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string email, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
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

                // Real Moralis implementation - load avatar by email from IPFS
                // Search for avatar file by email in IPFS
                var searchRequest = new
                {
                    path = $"avatar_*_{email}.json"
                };

                var jsonContent = new StringContent(JsonSerializer.Serialize(searchRequest), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_baseUrl}/ipfs/resolve", jsonContent);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var ipfsResult = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    if (ipfsResult.TryGetProperty("content", out var content))
                    {
                        var base64Content = content.GetString();
                        var avatarBytes = Convert.FromBase64String(base64Content);
                        var avatarJson = Encoding.UTF8.GetString(avatarBytes);
                        var avatar = JsonSerializer.Deserialize<Avatar>(avatarJson);
                        
                        result.Result = avatar;
                        result.IsError = false;
                        result.Message = "Avatar loaded successfully from Moralis IPFS by email";
                        return result;
                    }
                }

                // Fallback: Try loading from contract if available
                if (!string.IsNullOrEmpty(GetOASISContractAddress()))
                {
                    var contractRequest = new
                    {
                        address = GetOASISContractAddress(),
                        function_name = "getAvatarByEmail",
                        abi = GetOASISContractABI(),
                        @params = new { email = email, version = version }
                    };

                    var contractResponse = await _httpClient.PostAsync($"{_baseUrl}/{Uri.EscapeDataString(GetOASISContractAddress())}/function",
                        new StringContent(JsonSerializer.Serialize(contractRequest), Encoding.UTF8, "application/json"));

                    if (contractResponse.IsSuccessStatusCode)
                    {
                        var contractContent = await contractResponse.Content.ReadAsStringAsync();
                        var contractResult = JsonSerializer.Deserialize<MoralisApiResult>(contractContent);
                        if (!string.IsNullOrEmpty(contractResult?.result))
                        {
                            var avatar = JsonSerializer.Deserialize<Avatar>(contractResult.result);
                            result.Result = avatar;
                            result.IsError = false;
                            result.Message = "Avatar loaded successfully from Moralis contract by email";
                            return result;
                        }
                    }
                }

                OASISErrorHandling.HandleError(ref result, "Avatar not found by email in Moralis IPFS or contract");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by email: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string email, int version = 0)
        {
            return LoadAvatarByEmailAsync(email, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string username, int version = 0)
        {
            // Try using the username as a provider key (IPFS path or contract reference)
            var result = await LoadAvatarByProviderKeyAsync(username, version);
            if (!result.IsError && result.Result != null)
                return result;

            // Fall back: search contract for avatar with matching username field
            var fallback = new OASISResult<IAvatar>();
            try
            {
                if (!string.IsNullOrEmpty(GetOASISContractAddress()))
                {
                    var contractRequest = new
                    {
                        address = GetOASISContractAddress(),
                        function_name = "getAvatarByUsername",
                        abi = GetOASISContractABI(),
                        @params = new { username, version }
                    };

                    var contractResponse = await _httpClient.PostAsync(
                        $"{_baseUrl}/{Uri.EscapeDataString(GetOASISContractAddress())}/function",
                        new StringContent(JsonSerializer.Serialize(contractRequest), Encoding.UTF8, "application/json"));

                    if (contractResponse.IsSuccessStatusCode)
                    {
                        var contractContent = await contractResponse.Content.ReadAsStringAsync();
                        var contractResult = JsonSerializer.Deserialize<MoralisApiResult>(contractContent);
                        if (!string.IsNullOrEmpty(contractResult?.result))
                        {
                            var avatar = JsonSerializer.Deserialize<Avatar>(contractResult.result);
                            fallback.Result = avatar;
                            fallback.IsError = false;
                            fallback.Message = "Avatar loaded by username from Moralis contract";
                            return fallback;
                        }
                    }
                }

                OASISErrorHandling.HandleError(ref fallback, $"Avatar with username '{username}' not found in Moralis IPFS or contract");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref fallback, $"Error loading avatar by username: {ex.Message}", ex);
            }
            return fallback;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string username, int version = 0)
        {
            return LoadAvatarByUsernameAsync(username, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
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

                // Real Moralis implementation - load avatar by provider key (IPFS path) from IPFS
                // Provider key is the IPFS path stored when saving
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
                            var avatarBytes = Convert.FromBase64String(base64Content);
                            var avatarJson = Encoding.UTF8.GetString(avatarBytes);
                            var avatar = JsonSerializer.Deserialize<Avatar>(avatarJson);
                            
                            result.Result = avatar;
                            result.IsError = false;
                            result.Message = "Avatar loaded successfully from Moralis IPFS by provider key";
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
                            function_name = "getAvatarByProviderKey",
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
                                var avatar = JsonSerializer.Deserialize<Avatar>(contractResult.result);
                                result.Result = avatar;
                                result.IsError = false;
                                result.Message = "Avatar loaded successfully from Moralis contract by provider key";
                                return result;
                            }
                        }
                    }
                }

                OASISErrorHandling.HandleError(ref result, "Avatar not found by provider key in Moralis IPFS or contract");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by provider key: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
        {
            return LoadAvatarByProviderKeyAsync(providerKey, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
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

                if (string.IsNullOrEmpty(_contractAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "No contract address configured. Set a contract address to enumerate all avatars via Moralis NFT owner API.");
                    return result;
                }

                // GET /nft/{address}/owners?chain={chain} — returns all NFT owners for the OASIS contract
                var response = await _httpClient.GetAsync($"{_baseUrl}/nft/{Uri.EscapeDataString(_contractAddress)}/owners?chain={_chain}");
                if (!response.IsSuccessStatusCode)
                {
                    OASISErrorHandling.HandleError(ref result, $"Moralis NFT owner query failed: {response.StatusCode}");
                    return result;
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var parsed = JsonSerializer.Deserialize<JsonElement>(jsonResponse);

                var avatars = new List<IAvatar>();
                if (parsed.TryGetProperty("result", out var nftArray))
                {
                    foreach (var nft in nftArray.EnumerateArray())
                    {
                        // token_uri holds the IPFS path where avatar JSON was stored during SaveAvatarAsync
                        var tokenUri = nft.TryGetProperty("token_uri", out var tu) ? tu.GetString() : null;
                        if (!string.IsNullOrEmpty(tokenUri))
                        {
                            var avatarResult = await LoadAvatarByProviderKeyAsync(tokenUri, version);
                            if (!avatarResult.IsError && avatarResult.Result != null)
                                avatars.Add(avatarResult.Result);
                        }
                    }
                }

                result.Result = avatars;
                result.IsError = false;
                result.Message = $"Loaded {avatars.Count} avatars from Moralis NFT contract";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading all avatars: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            return LoadAllAvatarsAsync(version).Result;
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();
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

                if (avatar == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar cannot be null");
                    return result;
                }

                // Real Moralis implementation - save avatar to Web3 API
                avatar.ModifiedDate = DateTime.UtcNow;
                var txHash = await SaveAvatarToMoralisAsync(avatar);
                
                if (!string.IsNullOrEmpty(txHash))
                {
                    result.Result = avatar;
                    result.IsError = false;
                    result.IsSaved = true;
                    result.Message = $"Avatar saved to Moralis Web3 API successfully. Transaction: {txHash}";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to save avatar to Moralis Web3 API");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving avatar: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
        {
            return SaveAvatarAsync(avatar).Result;
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Moralis provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Real Moralis implementation - delete avatar from contract if available
                if (!string.IsNullOrEmpty(GetOASISContractAddress()))
                {
                    var contractRequest = new
                    {
                        address = GetOASISContractAddress(),
                        function_name = "deleteAvatar",
                        abi = GetOASISContractABI(),
                        @params = new { avatarId = id.ToString(), softDelete = softDelete }
                    };

                    var contractResponse = await _httpClient.PostAsync($"{_baseUrl}/{Uri.EscapeDataString(GetOASISContractAddress())}/function",
                        new StringContent(JsonSerializer.Serialize(contractRequest), Encoding.UTF8, "application/json"));

                    if (contractResponse.IsSuccessStatusCode)
                    {
                        result.Result = true;
                        result.IsError = false;
                        result.Message = $"Avatar {id} deleted successfully from Moralis contract";
                        return result;
                    }
                }

                // IPFS is immutable, so we can't actually delete files
                OASISErrorHandling.HandleWarning(ref result, "IPFS is immutable. Avatar cannot be deleted from IPFS. Use contract deletion or mark as deleted in metadata.");
                result.Result = false;
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
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Moralis provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Real Moralis implementation - delete avatar by provider key from contract if available
                if (!string.IsNullOrEmpty(GetOASISContractAddress()))
                {
                    var contractRequest = new
                    {
                        address = GetOASISContractAddress(),
                        function_name = "deleteAvatarByProviderKey",
                        abi = GetOASISContractABI(),
                        @params = new { providerKey = providerKey, softDelete = softDelete }
                    };

                    var contractResponse = await _httpClient.PostAsync($"{_baseUrl}/{Uri.EscapeDataString(GetOASISContractAddress())}/function",
                        new StringContent(JsonSerializer.Serialize(contractRequest), Encoding.UTF8, "application/json"));

                    if (contractResponse.IsSuccessStatusCode)
                    {
                        result.Result = true;
                        result.IsError = false;
                        result.Message = $"Avatar with provider key {providerKey} deleted successfully from Moralis contract";
                        return result;
                    }
                }

                // IPFS is immutable, so we can't actually delete files
                OASISErrorHandling.HandleWarning(ref result, "IPFS is immutable. Avatar cannot be deleted from IPFS. Use contract deletion or mark as deleted in metadata.");
                result.Result = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        {
            return DeleteAvatarAsync(providerKey, softDelete).Result;
        }

    }
}
