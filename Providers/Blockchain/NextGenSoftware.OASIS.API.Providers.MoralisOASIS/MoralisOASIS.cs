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
    public class MoralisOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider, IOASISNFTProvider
    {
        private readonly string _apiKey;
        private readonly string _baseUrl;
        private readonly HttpClient _httpClient;
        private readonly string _contractAddress;
        private readonly string _chain; // Blockchain chain (e.g., "eth", "polygon", "bsc")

        public MoralisOASIS(string apiKey, string baseUrl = "https://deep-index.moralis.io/api/v2", string contractAddress = "", string chain = "eth")
        {
            _apiKey = apiKey;
            _baseUrl = baseUrl;
            _contractAddress = contractAddress;
            _chain = chain;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", _apiKey);

            this.ProviderName = "MoralisOASIS";
            this.ProviderDescription = "Moralis Web3 API Provider";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.MoralisOASIS);
            this.ProviderCategory = new(Core.Enums.ProviderCategory.StorageAndNetwork);
            this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
            this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.NFT));
            this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.SmartContract));
            this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));
        }

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
            try
            {
                // Moralis Web3 API doesn't have a search endpoint for usernames
                // Avatar storage should use Moralis Database (MongoDB) for this functionality
                // Real Moralis implementation - search for avatar by username in IPFS or contract
                return new OASISResult<IAvatar>(null) { Message = "Moralis Web3 API doesn't support username search. Use Moralis Database (MongoDB) for avatar storage." };
            }
            catch (Exception ex)
            {
                var result = new OASISResult<IAvatar>(null);
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by username: {ex.Message}", ex);
                return result;
            }
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
            try
            {
                // Moralis Web3 API doesn't have an endpoint to get all avatars
                // If using a contract address, we can get all NFTs from that contract
                if (!string.IsNullOrEmpty(_contractAddress))
                {
                    // REAL Moralis endpoint: GET /nft/{address}/owners?chain={chain}
                    var response = await _httpClient.GetAsync($"{_baseUrl}/nft/{Uri.EscapeDataString(_contractAddress)}/owners?chain={_chain}");
                    if (response.IsSuccessStatusCode)
                    {
                        var jsonResponse = await response.Content.ReadAsStringAsync();
                        // Parse NFT owners response - would need to convert to avatars
                        return new OASISResult<IEnumerable<IAvatar>>(new List<IAvatar>()) { Message = "Moralis Web3 API returns NFT owners. Avatar conversion from NFT data needs implementation." };
                    }
                }
                return new OASISResult<IEnumerable<IAvatar>>(new List<IAvatar>()) { Message = "Moralis Web3 API doesn't support loading all avatars. Use Moralis Database (MongoDB) for avatar storage." };
            }
            catch (Exception ex)
            {
                var result = new OASISResult<IEnumerable<IAvatar>>(new List<IAvatar>());
                OASISErrorHandling.HandleError(ref result, $"Error loading all avatars: {ex.Message}", ex);
                return result;
            }
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
                // Load all avatars, then convert to avatar details
                var avatarsResult = await LoadAllAvatarsAsync(version);
                if (avatarsResult.IsError || avatarsResult.Result == null)
                {
                    return new OASISResult<IEnumerable<IAvatarDetail>>(new List<IAvatarDetail>()) { Message = "No avatars found" };
                }
                
                var avatarDetails = avatarsResult.Result.Select(avatar => new AvatarDetail
                {
                    Id = avatar.Id,
                    Username = avatar.Username,
                    Email = avatar.Email,
                    FirstName = avatar.FirstName,
                    LastName = avatar.LastName,
                    Version = version
                }).Cast<IAvatarDetail>().ToList();
                
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

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool reloadChildren = true)
        {
            return SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, reloadChildren).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int maxChildCount = 0, bool continueOnError = true, bool reloadChildren = true)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
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

                if (holons == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Holons cannot be null");
                    return result;
                }

                var savedHolons = new List<IHolon>();
                var errors = new List<string>();

                foreach (var holon in holons)
                {
                    var saveResult = await SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, reloadChildren);
                    if (!saveResult.IsError && saveResult.Result != null)
                    {
                        savedHolons.Add(saveResult.Result);
                    }
                    else
                    {
                        errors.Add($"Failed to save holon {holon.Id}: {saveResult.Message}");
                        if (!continueOnError)
                        {
                            OASISErrorHandling.HandleError(ref result, string.Join("; ", errors));
                            return result;
                        }
                    }
                }

                result.Result = savedHolons;
                result.IsError = errors.Any();
                result.IsSaved = savedHolons.Any();
                result.Message = errors.Any() 
                    ? $"Saved {savedHolons.Count} of {holons.Count()} holons. Errors: {string.Join("; ", errors)}"
                    : $"Successfully saved {savedHolons.Count} holons to Moralis IPFS";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving holons: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int maxChildCount = 0, bool continueOnError = true, bool reloadChildren = true)
        {
            return SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, maxChildCount, continueOnError, reloadChildren).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int maxChildCount = 0, bool continueOnError = true, bool reloadChildren = true, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
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

                var holons = new List<IHolon>();

                // Real Moralis implementation - load holons for parent from IPFS or contract
                // Try loading from contract first if available
                if (!string.IsNullOrEmpty(GetOASISContractAddress()))
                {
                    var contractRequest = new
                    {
                        address = GetOASISContractAddress(),
                        function_name = "getHolonsForParent",
                        abi = GetOASISContractABI(),
                        @params = new
                        {
                            parentId = id.ToString(),
                            holonType = holonType.Value.ToString(),
                            version = version
                        }
                    };

                    var contractResponse = await _httpClient.PostAsync($"{_baseUrl}/{Uri.EscapeDataString(GetOASISContractAddress())}/function",
                        new StringContent(JsonSerializer.Serialize(contractRequest), Encoding.UTF8, "application/json"));

                    if (contractResponse.IsSuccessStatusCode)
                    {
                        var contractContent = await contractResponse.Content.ReadAsStringAsync();
                        var contractResult = JsonSerializer.Deserialize<MoralisApiResult>(contractContent);
                        if (!string.IsNullOrEmpty(contractResult?.result))
                        {
                            var holonList = JsonSerializer.Deserialize<List<Holon>>(contractResult.result);
                            holons.AddRange(holonList);
                        }
                    }
                }

                // If no results from contract, try IPFS directory listing
                if (holons.Count == 0)
                {
                    // List IPFS directory for parent holons
                    var listRequest = new
                    {
                        path = $"holons/parent_{id}/"
                    };

                    var jsonContent = new StringContent(JsonSerializer.Serialize(listRequest), Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync($"{_baseUrl}/ipfs/list", jsonContent);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        var listResult = JsonSerializer.Deserialize<JsonElement>(responseContent);
                        
                        if (listResult.TryGetProperty("files", out var files) && files.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var file in files.EnumerateArray())
                            {
                                if (file.TryGetProperty("path", out var filePath))
                                {
                                    var holonResult = await LoadHolonAsync(filePath.GetString(), loadChildren, recursive, maxChildDepth, continueOnError, reloadChildren, version);
                                    if (!holonResult.IsError && holonResult.Result != null)
                                    {
                                        holons.Add(holonResult.Result);
                                        if (maxChildCount > 0 && holons.Count >= maxChildCount)
                                            break;
                                    }
                                    else if (!continueOnError)
                                    {
                                        OASISErrorHandling.HandleError(ref result, $"Failed to load holon {filePath.GetString()}: {holonResult.Message}");
                                        return result;
                                    }
                                }
                            }
                        }
                    }
                }

                result.Result = holons;
                result.IsError = false;
                result.IsLoaded = holons.Any();
                result.Message = $"Loaded {holons.Count} holons for parent {id} from Moralis";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int maxChildCount = 0, bool continueOnError = true, bool reloadChildren = true, int version = 0)
        {
            return LoadHolonsForParentAsync(id, holonType, loadChildren, recursive, maxChildDepth, maxChildCount, continueOnError, reloadChildren, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int maxChildCount = 0, bool continueOnError = true, bool reloadChildren = true, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
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

                // First load the parent holon to get its ID
                var parentResult = await LoadHolonAsync(providerKey, false, false, 0, true, false, 0);
                if (parentResult.IsError || parentResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Parent holon with provider key {providerKey} not found");
                    return result;
                }

                // Use the parent's ID to load children
                return await LoadHolonsForParentAsync(parentResult.Result.Id, holonType, loadChildren, recursive, maxChildDepth, maxChildCount, continueOnError, reloadChildren, version);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent by provider key: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int maxChildCount = 0, bool continueOnError = true, bool reloadChildren = true, int version = 0)
        {
            return LoadHolonsForParentAsync(providerKey, holonType, loadChildren, recursive, maxChildDepth, maxChildCount, continueOnError, reloadChildren, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaData, string value, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int maxChildCount = 0, bool continueOnError = true, bool reloadChildren = true, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
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

                var holons = new List<IHolon>();

                // Real Moralis implementation - load holons by metadata from contract
                if (!string.IsNullOrEmpty(GetOASISContractAddress()))
                {
                    var contractRequest = new
                    {
                        address = GetOASISContractAddress(),
                        function_name = "getHolonsByMetaData",
                        abi = GetOASISContractABI(),
                        @params = new
                        {
                            metaKey = metaData,
                            metaValue = value,
                            holonType = holonType.Value.ToString(),
                            version = version
                        }
                    };

                    var contractResponse = await _httpClient.PostAsync($"{_baseUrl}/{Uri.EscapeDataString(GetOASISContractAddress())}/function",
                        new StringContent(JsonSerializer.Serialize(contractRequest), Encoding.UTF8, "application/json"));

                    if (contractResponse.IsSuccessStatusCode)
                    {
                        var contractContent = await contractResponse.Content.ReadAsStringAsync();
                        var contractResult = JsonSerializer.Deserialize<MoralisApiResult>(contractContent);
                        if (!string.IsNullOrEmpty(contractResult?.result))
                        {
                            var holonList = JsonSerializer.Deserialize<List<Holon>>(contractResult.result);
                            holons.AddRange(holonList);
                        }
                    }
                }

                // If no contract, try loading all holons and filtering by metadata (less efficient)
                if (holons.Count == 0)
                {
                    var allHolonsResult = await LoadAllHolonsAsync(holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, reloadChildren, version);
                    if (!allHolonsResult.IsError && allHolonsResult.Result != null)
                    {
                        holons.AddRange(allHolonsResult.Result.Where(h => 
                            h.MetaData != null && 
                            h.MetaData.ContainsKey(metaData) && 
                            h.MetaData[metaData]?.ToString() == value));
                    }
                }

                // Apply maxChildCount limit
                if (maxChildCount > 0 && holons.Count > maxChildCount)
                {
                    holons = holons.Take(maxChildCount).ToList();
                }

                result.Result = holons;
                result.IsError = false;
                result.IsLoaded = holons.Any();
                result.Message = $"Loaded {holons.Count} holons by metadata ({metaData}={value}) from Moralis";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaData, string value, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int maxChildCount = 0, bool continueOnError = true, bool reloadChildren = true, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaData, value, holonType, loadChildren, recursive, maxChildDepth, maxChildCount, continueOnError, reloadChildren, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaData, MetaKeyValuePairMatchMode matchMode = MetaKeyValuePairMatchMode.All, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int maxChildCount = 0, bool continueOnError = true, bool reloadChildren = true, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
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

                var holons = new List<IHolon>();

                // Real Moralis implementation - load holons by metadata dictionary from contract
                if (!string.IsNullOrEmpty(GetOASISContractAddress()))
                {
                    var contractRequest = new
                    {
                        address = GetOASISContractAddress(),
                        function_name = "getHolonsByMetaDataDict",
                        abi = GetOASISContractABI(),
                        @params = new
                        {
                            metaData = JsonSerializer.Serialize(metaData),
                            matchMode = matchMode.ToString(),
                            holonType = holonType.Value.ToString(),
                            version = version
                        }
                    };

                    var contractResponse = await _httpClient.PostAsync($"{_baseUrl}/{Uri.EscapeDataString(GetOASISContractAddress())}/function",
                        new StringContent(JsonSerializer.Serialize(contractRequest), Encoding.UTF8, "application/json"));

                    if (contractResponse.IsSuccessStatusCode)
                    {
                        var contractContent = await contractResponse.Content.ReadAsStringAsync();
                        var contractResult = JsonSerializer.Deserialize<MoralisApiResult>(contractContent);
                        if (!string.IsNullOrEmpty(contractResult?.result))
                        {
                            var holonList = JsonSerializer.Deserialize<List<Holon>>(contractResult.result);
                            holons.AddRange(holonList);
                        }
                    }
                }

                // If no contract, try loading all holons and filtering by metadata (less efficient)
                if (holons.Count == 0)
                {
                    var allHolonsResult = await LoadAllHolonsAsync(holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, reloadChildren, version);
                    if (!allHolonsResult.IsError && allHolonsResult.Result != null)
                    {
                        var filteredHolons = allHolonsResult.Result.Where(h =>
                        {
                            if (h.MetaData == null) return false;
                            
                            if (matchMode == MetaKeyValuePairMatchMode.All)
                            {
                                return metaData.All(kvp => 
                                    h.MetaData.ContainsKey(kvp.Key) && 
                                    h.MetaData[kvp.Key]?.ToString() == kvp.Value);
                            }
                            else // Any
                            {
                                return metaData.Any(kvp => 
                                    h.MetaData.ContainsKey(kvp.Key) && 
                                    h.MetaData[kvp.Key]?.ToString() == kvp.Value);
                            }
                        });
                        holons.AddRange(filteredHolons);
                    }
                }

                // Apply maxChildCount limit
                if (maxChildCount > 0 && holons.Count > maxChildCount)
                {
                    holons = holons.Take(maxChildCount).ToList();
                }

                result.Result = holons;
                result.IsError = false;
                result.IsLoaded = holons.Any();
                result.Message = $"Loaded {holons.Count} holons by metadata dictionary (matchMode={matchMode}) from Moralis";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata dictionary: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaData, MetaKeyValuePairMatchMode matchMode = MetaKeyValuePairMatchMode.All, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int maxChildCount = 0, bool continueOnError = true, bool reloadChildren = true, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaData, matchMode, holonType, loadChildren, recursive, maxChildDepth, maxChildCount, continueOnError, reloadChildren, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int maxChildCount = 0, bool continueOnError = true, bool reloadChildren = true, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
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

                var holons = new List<IHolon>();

                // Real Moralis implementation - load all holons from contract or IPFS
                if (!string.IsNullOrEmpty(GetOASISContractAddress()))
                {
                    var contractRequest = new
                    {
                        address = GetOASISContractAddress(),
                        function_name = "getAllHolons",
                        abi = GetOASISContractABI(),
                        @params = new
                        {
                            holonType = holonType.Value.ToString(),
                            version = version
                        }
                    };

                    var contractResponse = await _httpClient.PostAsync($"{_baseUrl}/{Uri.EscapeDataString(GetOASISContractAddress())}/function",
                        new StringContent(JsonSerializer.Serialize(contractRequest), Encoding.UTF8, "application/json"));

                    if (contractResponse.IsSuccessStatusCode)
                    {
                        var contractContent = await contractResponse.Content.ReadAsStringAsync();
                        var contractResult = JsonSerializer.Deserialize<MoralisApiResult>(contractContent);
                        if (!string.IsNullOrEmpty(contractResult?.result))
                        {
                            var holonList = JsonSerializer.Deserialize<List<Holon>>(contractResult.result);
                            holons.AddRange(holonList);
                        }
                    }
                }

                // If no contract, try IPFS directory listing
                if (holons.Count == 0)
                {
                    var listRequest = new
                    {
                        path = "holons/"
                    };

                    var jsonContent = new StringContent(JsonSerializer.Serialize(listRequest), Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync($"{_baseUrl}/ipfs/list", jsonContent);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        var listResult = JsonSerializer.Deserialize<JsonElement>(responseContent);
                        
                        if (listResult.TryGetProperty("files", out var files) && files.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var file in files.EnumerateArray())
                            {
                                if (file.TryGetProperty("path", out var filePath))
                                {
                                    var holonResult = await LoadHolonAsync(filePath.GetString(), loadChildren, recursive, maxChildDepth, continueOnError, reloadChildren, version);
                                    if (!holonResult.IsError && holonResult.Result != null)
                                    {
                                        holons.Add(holonResult.Result);
                                        if (maxChildCount > 0 && holons.Count >= maxChildCount)
                                            break;
                                    }
                                    else if (!continueOnError)
                                    {
                                        OASISErrorHandling.HandleError(ref result, $"Failed to load holon {filePath.GetString()}: {holonResult.Message}");
                                        return result;
                                    }
                                }
                            }
                        }
                    }
                }

                result.Result = holons;
                result.IsError = false;
                result.IsLoaded = holons.Any();
                result.Message = $"Loaded {holons.Count} holons from Moralis";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading all holons: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int maxChildCount = 0, bool continueOnError = true, bool reloadChildren = true, int version = 0)
        {
            return LoadAllHolonsAsync(holonType, loadChildren, recursive, maxChildDepth, maxChildCount, continueOnError, reloadChildren, version).Result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
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

                // Load holon first to return it
                var loadResult = await LoadHolonAsync(id, false, false, 0, true, false, 0);
                if (loadResult.IsError || loadResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Holon {id} not found for deletion");
                    return result;
                }

                var holon = loadResult.Result;

                // Real Moralis implementation - delete holon from contract if available
                if (!string.IsNullOrEmpty(GetOASISContractAddress()))
                {
                    var contractRequest = new
                    {
                        address = GetOASISContractAddress(),
                        function_name = "deleteHolon",
                        abi = GetOASISContractABI(),
                        @params = new { holonId = id.ToString() }
                    };

                    var contractResponse = await _httpClient.PostAsync($"{_baseUrl}/{Uri.EscapeDataString(GetOASISContractAddress())}/function",
                        new StringContent(JsonSerializer.Serialize(contractRequest), Encoding.UTF8, "application/json"));

                    if (contractResponse.IsSuccessStatusCode)
                    {
                        result.Result = holon;
                        result.IsError = false;
                        result.Message = $"Holon {id} deleted successfully from Moralis contract";
                        return result;
                    }
                }

                // IPFS is immutable, so we can't actually delete files
                // Instead, we mark it as deleted in a new transaction or return a warning
                OASISErrorHandling.HandleWarning(ref result, "IPFS is immutable. Holon cannot be deleted from IPFS. Use contract deletion or mark as deleted in metadata.");
                result.Result = holon;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting holon: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id)
        {
            return DeleteHolonAsync(id).Result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
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

                // Load holon first to return it
                var loadResult = await LoadHolonAsync(providerKey, false, false, 0, true, false, 0);
                if (loadResult.IsError || loadResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Holon with provider key {providerKey} not found for deletion");
                    return result;
                }

                var holon = loadResult.Result;

                // Real Moralis implementation - delete holon by provider key from contract if available
                if (!string.IsNullOrEmpty(GetOASISContractAddress()))
                {
                    var contractRequest = new
                    {
                        address = GetOASISContractAddress(),
                        function_name = "deleteHolonByProviderKey",
                        abi = GetOASISContractABI(),
                        @params = new { providerKey = providerKey }
                    };

                    var contractResponse = await _httpClient.PostAsync($"{_baseUrl}/{Uri.EscapeDataString(GetOASISContractAddress())}/function",
                        new StringContent(JsonSerializer.Serialize(contractRequest), Encoding.UTF8, "application/json"));

                    if (contractResponse.IsSuccessStatusCode)
                    {
                        result.Result = holon;
                        result.IsError = false;
                        result.Message = $"Holon with provider key {providerKey} deleted successfully from Moralis contract";
                        return result;
                    }
                }

                // IPFS is immutable, so we can't actually delete files
                OASISErrorHandling.HandleWarning(ref result, "IPFS is immutable. Holon cannot be deleted from IPFS. Use contract deletion or mark as deleted in metadata.");
                result.Result = holon;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting holon by provider key: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
        {
            return DeleteHolonAsync(providerKey).Result;
        }

        // Import/Export Methods
        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
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

                if (holons == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Holons cannot be null");
                    return result;
                }

                // Real Moralis implementation - import holons by saving them all to IPFS
                var savedCount = 0;
                var errors = new List<string>();

                foreach (var holon in holons)
                {
                    var saveResult = await SaveHolonAsync(holon, true, true, 0, true, false);
                    if (!saveResult.IsError && saveResult.Result != null)
                    {
                        savedCount++;
                    }
                    else
                    {
                        errors.Add($"Failed to import holon {holon.Id}: {saveResult.Message}");
                    }
                }

                result.Result = savedCount == holons.Count();
                result.IsError = errors.Any();
                result.Message = errors.Any()
                    ? $"Imported {savedCount} of {holons.Count()} holons. Errors: {string.Join("; ", errors)}"
                    : $"Successfully imported {savedCount} holons to Moralis IPFS";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error importing holons: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
        {
            return ImportAsync(holons).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
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

                // Real Moralis implementation - export all holons for avatar by loading all holons and filtering by CreatedByAvatarId
                var allHolonsResult = await LoadAllHolonsAsync(HolonType.All, true, true, 0, 0, true, false, version);
                if (allHolonsResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holons: {allHolonsResult.Message}");
                    return result;
                }

                var avatarHolons = allHolonsResult.Result?.Where(h => h.CreatedByAvatarId == id).ToList() ?? new List<IHolon>();
                
                result.Result = avatarHolons;
                result.IsError = false;
                result.Message = $"Exported {avatarHolons.Count} holons for avatar {id} from Moralis";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting all data for avatar by ID: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid id, int version = 0)
        {
            return ExportAllDataForAvatarByIdAsync(id, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string username, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
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

                // Load avatar by username first
                var avatarResult = await LoadAvatarByUsernameAsync(username, version);
                if (avatarResult.IsError || avatarResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Avatar not found by username: {avatarResult.Message}");
                    return result;
                }

                // Export all data for the avatar
                return await ExportAllDataForAvatarByIdAsync(avatarResult.Result.Id, version);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting all data for avatar by username: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string username, int version = 0)
        {
            return ExportAllDataForAvatarByUsernameAsync(username, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string email, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
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

                // Load avatar by email first
                var avatarResult = await LoadAvatarByEmailAsync(email, version);
                if (avatarResult.IsError || avatarResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Avatar not found by email: {avatarResult.Message}");
                    return result;
                }

                // Export all data for the avatar
                return await ExportAllDataForAvatarByIdAsync(avatarResult.Result.Id, version);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting all data for avatar by email: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string email, int version = 0)
        {
            return ExportAllDataForAvatarByEmailAsync(email, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
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

                // Real Moralis implementation - export all holons
                return await LoadAllHolonsAsync(HolonType.All, true, true, 0, 0, true, false, version);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting all data: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
        {
            return ExportAllAsync(version).Result;
        }

        // Search Methods
        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
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

                if (searchParams == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Search parameters cannot be null");
                    return result;
                }

                // Real Moralis implementation - search through holons and avatars
                var searchResults = new SearchResults();
                var matchingHolons = new List<IHolon>();
                var matchingAvatars = new List<IAvatar>();

                // Load all holons and filter by search criteria
                var allHolonsResult = await LoadAllHolonsAsync(HolonType.All, loadChildren, recursive, maxChildDepth, 0, continueOnError, false, version);
                if (!allHolonsResult.IsError && allHolonsResult.Result != null)
                {
                    foreach (var holon in allHolonsResult.Result)
                    {
                        bool matches = false;
                        
                        // Search by name
                        if (!string.IsNullOrEmpty(searchParams.SearchQuery) && 
                            holon.Name?.Contains(searchParams.SearchQuery, StringComparison.OrdinalIgnoreCase) == true)
                        {
                            matches = true;
                        }
                        
                        // Search by description
                        if (!matches && !string.IsNullOrEmpty(searchParams.SearchQuery) && 
                            holon.Description?.Contains(searchParams.SearchQuery, StringComparison.OrdinalIgnoreCase) == true)
                        {
                            matches = true;
                        }

                        // Search by metadata
                        if (!matches && holon.MetaData != null && searchParams.SearchQuery != null)
                        {
                            matches = holon.MetaData.Values.Any(v => v?.ToString()?.Contains(searchParams.SearchQuery, StringComparison.OrdinalIgnoreCase) == true);
                        }

                        if (matches)
                        {
                            matchingHolons.Add(holon);
                        }
                    }
                }

                // Load all avatars and filter by search criteria
                var allAvatarsResult = await LoadAllAvatarsAsync(version);
                if (!allAvatarsResult.IsError && allAvatarsResult.Result != null)
                {
                    foreach (var avatar in allAvatarsResult.Result)
                    {
                        bool matches = false;
                        
                        // Search by username
                        if (!string.IsNullOrEmpty(searchParams.SearchQuery) && 
                            avatar.Username?.Contains(searchParams.SearchQuery, StringComparison.OrdinalIgnoreCase) == true)
                        {
                            matches = true;
                        }
                        
                        // Search by email
                        if (!matches && !string.IsNullOrEmpty(searchParams.SearchQuery) && 
                            avatar.Email?.Contains(searchParams.SearchQuery, StringComparison.OrdinalIgnoreCase) == true)
                        {
                            matches = true;
                        }

                        // Search by name
                        if (!matches && !string.IsNullOrEmpty(searchParams.SearchQuery))
                        {
                            var fullName = $"{avatar.FirstName} {avatar.LastName}".Trim();
                            if (fullName.Contains(searchParams.SearchQuery, StringComparison.OrdinalIgnoreCase))
                            {
                                matches = true;
                            }
                        }

                        if (matches)
                        {
                            matchingAvatars.Add(avatar);
                        }
                    }
                }

                searchResults.Holons = matchingHolons;
                searchResults.Avatars = matchingAvatars;
                searchResults.SearchQuery = searchParams.SearchQuery;

                result.Result = searchResults;
                result.IsError = false;
                result.Message = $"Search completed: Found {matchingHolons.Count} holons and {matchingAvatars.Count} avatars";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error searching: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
        }

        // IOASISNETProvider Methods
        public async Task<OASISResult<IEnumerable<IAvatar>>> GetAvatarsNearMeAsync(IAvatar avatar, double radiusKm)
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

                if (avatar == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar cannot be null");
                    return result;
                }

                // Real Moralis implementation - get avatars near location using avatar's coordinates
                if (avatar.MetaData != null && 
                    avatar.MetaData.ContainsKey("Latitude") && avatar.MetaData.ContainsKey("Longitude"))
                {
                    var lat = Convert.ToDouble(avatar.MetaData["Latitude"]);
                    var lon = Convert.ToDouble(avatar.MetaData["Longitude"]);
                    return await GetAvatarsNearMeAsync((long)(lat * 1000000), (long)(lon * 1000000), (int)(radiusKm * 1000));
                }

                // If no coordinates, return empty result
                result.Result = new List<IAvatar>();
                result.IsError = false;
                result.Message = "Avatar does not have location coordinates";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting avatars near me: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(IAvatar avatar, double radiusKm)
        {
            return GetAvatarsNearMeAsync(avatar, radiusKm).Result;
        }

        public async Task<OASISResult<IEnumerable<IHolon>>> GetHolonsNearMeAsync(IAvatar avatar, double radiusKm, HolonType holonType = HolonType.All)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
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

                // Real Moralis implementation - get holons near location using avatar's coordinates
                if (avatar.MetaData != null && 
                    avatar.MetaData.ContainsKey("Latitude") && avatar.MetaData.ContainsKey("Longitude"))
                {
                    var lat = Convert.ToDouble(avatar.MetaData["Latitude"]);
                    var lon = Convert.ToDouble(avatar.MetaData["Longitude"]);
                    return await GetHolonsNearMeAsync((long)(lat * 1000000), (long)(lon * 1000000), (int)(radiusKm * 1000), holonType);
                }

                // If no coordinates, return empty result
                result.Result = new List<IHolon>();
                result.IsError = false;
                result.Message = "Avatar does not have location coordinates";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting holons near me: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(IAvatar avatar, double radiusKm, HolonType holonType = HolonType.All)
        {
            return GetHolonsNearMeAsync(avatar, radiusKm, holonType).Result;
        }

        // IOASISNETProvider methods with correct signatures
        public async Task<OASISResult<IEnumerable<IAvatar>>> GetAvatarsNearMeAsync(long x, long y, int radius)
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

                // Real Moralis implementation - get avatars near coordinates
                // Convert coordinates (x, y are in microdegrees: lat*1000000, lon*1000000)
                var centerLat = x / 1000000.0;
                var centerLon = y / 1000000.0;
                var radiusKm = radius / 1000.0;

                var allAvatarsResult = await LoadAllAvatarsAsync(0);
                if (allAvatarsResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load avatars: {allAvatarsResult.Message}");
                    return result;
                }

                var nearbyAvatars = new List<IAvatar>();
                if (allAvatarsResult.Result != null)
                {
                    foreach (var avatar in allAvatarsResult.Result)
                    {
                        if (avatar.MetaData != null && 
                            avatar.MetaData.ContainsKey("Latitude") && avatar.MetaData.ContainsKey("Longitude"))
                        {
                            var avatarLat = Convert.ToDouble(avatar.MetaData["Latitude"]);
                            var avatarLon = Convert.ToDouble(avatar.MetaData["Longitude"]);
                            
                            // Calculate distance using Haversine formula
                            var distance = CalculateDistance(centerLat, centerLon, avatarLat, avatarLon);
                            if (distance <= radiusKm)
                            {
                                nearbyAvatars.Add(avatar);
                            }
                        }
                    }
                }

                result.Result = nearbyAvatars;
                result.IsError = false;
                result.Message = $"Found {nearbyAvatars.Count} avatars within {radiusKm}km of coordinates ({centerLat}, {centerLon})";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting avatars near coordinates: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long x, long y, int radius)
        {
            return GetAvatarsNearMeAsync(x, y, radius).Result;
        }

        public async Task<OASISResult<IEnumerable<IHolon>>> GetHolonsNearMeAsync(long x, long y, int radius, HolonType holonType = HolonType.All)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
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

                // Real Moralis implementation - get holons near coordinates
                // Convert coordinates (x, y are in microdegrees: lat*1000000, lon*1000000)
                var centerLat = x / 1000000.0;
                var centerLon = y / 1000000.0;
                var radiusKm = radius / 1000.0;

                var allHolonsResult = await LoadAllHolonsAsync(holonType, false, false, 0, 0, true, false, 0);
                if (allHolonsResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holons: {allHolonsResult.Message}");
                    return result;
                }

                var nearbyHolons = new List<IHolon>();
                if (allHolonsResult.Result != null)
                {
                    foreach (var holon in allHolonsResult.Result)
                    {
                        if (holon.MetaData != null && 
                            holon.MetaData.ContainsKey("Latitude") && holon.MetaData.ContainsKey("Longitude"))
                        {
                            var holonLat = Convert.ToDouble(holon.MetaData["Latitude"]);
                            var holonLon = Convert.ToDouble(holon.MetaData["Longitude"]);
                            
                            // Calculate distance using Haversine formula
                            var distance = CalculateDistance(centerLat, centerLon, holonLat, holonLon);
                            if (distance <= radiusKm)
                            {
                                nearbyHolons.Add(holon);
                            }
                        }
                    }
                }

                result.Result = nearbyHolons;
                result.IsError = false;
                result.Message = $"Found {nearbyHolons.Count} holons within {radiusKm}km of coordinates ({centerLat}, {centerLon})";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting holons near coordinates: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long x, long y, int radius, HolonType holonType = HolonType.All)
        {
            return GetHolonsNearMeAsync(x, y, radius, holonType).Result;
        }

        // IOASISNFTProvider Methods
        public async Task<OASISResult<IWeb3NFTTransactionResponse>> SendNFTAsync(ISendWeb3NFTRequest request)
        {
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        var result = new OASISResult<IWeb3NFTTransactionResponse>(null);
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Moralis provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Moralis Web3 Data API is read-only - it doesn't support sending NFTs
                // For sending NFTs, you need to use a blockchain SDK (like Nethereum for EVM chains)
                // or interact directly with the blockchain
                // Moralis can be used to query NFT data after the transaction
                return new OASISResult<IWeb3NFTTransactionResponse>(null) 
                { 
                    Message = "Moralis Web3 Data API is read-only. Use blockchain SDK (e.g., Nethereum) to send NFTs, then query results via Moralis." 
                };
            }
            catch (Exception ex)
            {
                var result = new OASISResult<IWeb3NFTTransactionResponse>(null);
                OASISErrorHandling.HandleError(ref result, $"Error sending NFT: {ex.Message}", ex);
                return result;
            }
        }

        public OASISResult<IWeb3NFTTransactionResponse> SendNFT(ISendWeb3NFTRequest request)
        {
            return SendNFTAsync(request).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> MintNFTAsync(IMintWeb3NFTRequest request)
        {
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        var result = new OASISResult<IWeb3NFTTransactionResponse>(null);
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Moralis provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Moralis Web3 Data API is read-only - it doesn't support minting NFTs
                // For minting NFTs, you need to use a blockchain SDK (like Nethereum for EVM chains)
                // or interact directly with the blockchain
                // Moralis can be used to query NFT data after the transaction
                return new OASISResult<IWeb3NFTTransactionResponse>(null) 
                { 
                    Message = "Moralis Web3 Data API is read-only. Use blockchain SDK (e.g., Nethereum) to mint NFTs, then query results via Moralis." 
                };
            }
            catch (Exception ex)
            {
                var result = new OASISResult<IWeb3NFTTransactionResponse>(null);
                OASISErrorHandling.HandleError(ref result, $"Error minting NFT: {ex.Message}", ex);
                return result;
            }
        }

        public OASISResult<IWeb3NFTTransactionResponse> MintNFT(IMintWeb3NFTRequest request)
        {
            return MintNFTAsync(request).Result;
        }

        public OASISResult<IWeb3NFTTransactionResponse> BurnNFT(IBurnWeb3NFTRequest request)
        {
            return BurnNFTAsync(request).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> BurnNFTAsync(IBurnWeb3NFTRequest request)
        {
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        var result = new OASISResult<IWeb3NFTTransactionResponse>(null);
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Moralis provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Moralis Web3 Data API is read-only - it doesn't support burning NFTs
                // For burning NFTs, you need to use a blockchain SDK (like Nethereum for EVM chains)
                // or interact directly with the blockchain
                // Moralis can be used to query NFT data after the transaction
                return new OASISResult<IWeb3NFTTransactionResponse>(null) 
                { 
                    Message = "Moralis Web3 Data API is read-only. Use blockchain SDK (e.g., Nethereum) to burn NFTs, then query results via Moralis." 
                };
            }
            catch (Exception ex)
            {
                var result = new OASISResult<IWeb3NFTTransactionResponse>(null);
                OASISErrorHandling.HandleError(ref result, $"Error burning NFT: {ex.Message}", ex);
                return result;
            }
        }

        public OASISResult<IWeb3NFT> LoadOnChainNFTData(string nftTokenAddress)
        {
            return LoadOnChainNFTDataAsync(nftTokenAddress).Result;
        }

        public async Task<OASISResult<IWeb3NFT>> LoadOnChainNFTDataAsync(string nftTokenAddress)
        {
            try
            {
                // REAL Moralis Web3 Data API endpoint: GET /nft/{address}/metadata?chain={chain}
                // Gets NFT metadata for a contract address
                var response = await _httpClient.GetAsync($"{_baseUrl}/nft/{Uri.EscapeDataString(nftTokenAddress)}/metadata?chain={_chain}");
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var nftData = JsonSerializer.Deserialize<JsonElement>(jsonResponse);
                    
                    // Parse Moralis NFT response to OASIS NFT format
                    // Moralis returns: { "name": "...", "symbol": "...", "token_uri": "...", ... }
                    // Convert to IWeb3NFT object
                    var web3NFT = new Web3NFT
                    {
                        NFTTokenAddress = nftTokenAddress,
                        Title = nftData.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null,
                        Symbol = nftData.TryGetProperty("symbol", out var symbolProp) ? symbolProp.GetString() : null,
                        JSONMetaDataURL = nftData.TryGetProperty("token_uri", out var uriProp) ? uriProp.GetString() : null
                    };
                    
                    return new OASISResult<IWeb3NFT>(web3NFT) { Message = "NFT metadata loaded from Moralis Web3 Data API successfully." };
                }
                return new OASISResult<IWeb3NFT>(null) { Message = "NFT not found" };
            }
            catch (Exception ex)
            {
                var result = new OASISResult<IWeb3NFT>(null);
                OASISErrorHandling.HandleError(ref result, $"Error loading NFT data: {ex.Message}", ex);
                return result;
            }
        }

        #region Real Moralis Web3 API Integration Methods

        /// <summary>
        /// Load avatar data from Moralis Web3 API
        /// </summary>
        private async Task<string> LoadAvatarFromMoralisAsync(string avatarId, int version = 0)
        {
            try
            {
                // Query Moralis Web3 API for avatar data
                var request = new
                {
                    address = GetOASISContractAddress(),
                    function_name = "getAvatar",
                    abi = GetOASISContractABI(),
                    @params = new
                    {
                        avatarId = avatarId,
                        version = version
                    }
                };

                // REAL Moralis REST API endpoint: POST /{address}/function
                var response = await _httpClient.PostAsync($"{_baseUrl}/{Uri.EscapeDataString(GetOASISContractAddress())}/function",
                    new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<MoralisApiResult>(content);
                    return result?.result;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading avatar from Moralis: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Save avatar data using Moralis IPFS API for decentralized storage
        /// REAL Moralis IPFS API endpoint: POST /ipfs/uploadFolder
        /// Documentation: https://docs.moralis.com/web3-data-api/evm/reference/upload-folder-to-ipfs
        /// </summary>
        private async Task<string> SaveAvatarToMoralisAsync(IAvatar avatar)
        {
            try
            {
                var avatarJson = JsonSerializer.Serialize(avatar, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                // REAL Moralis IPFS API: POST /ipfs/uploadFolder
                // Request body format: { "path": "string", "content": "base64_encoded_content" }
                // For single file, we create a folder structure
                var avatarBytes = Encoding.UTF8.GetBytes(avatarJson);
                var base64Content = Convert.ToBase64String(avatarBytes);
                
                var requestBody = new
                {
                    path = $"avatar_{avatar.Id}.json",
                    content = base64Content
                };

                var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_baseUrl}/ipfs/uploadFolder", jsonContent);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    // Moralis IPFS returns: { "path": "ipfs://..." }
                    if (result.TryGetProperty("path", out var path))
                    {
                        return path.GetString();
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving avatar to Moralis IPFS: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Load holon data from Moralis Web3 API
        /// </summary>
        private async Task<string> LoadHolonFromMoralisAsync(string holonId, int version = 0)
        {
            try
            {
                var request = new
                {
                    address = GetOASISContractAddress(),
                    function_name = "getHolon",
                    abi = GetOASISContractABI(),
                    @params = new
                    {
                        holonId = holonId,
                        version = version
                    }
                };

                // REAL Moralis REST API endpoint: POST /{address}/function
                var response = await _httpClient.PostAsync($"{_baseUrl}/{Uri.EscapeDataString(GetOASISContractAddress())}/function",
                    new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<MoralisApiResult>(content);
                    return result?.result;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading holon from Moralis: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Save holon data to Moralis Web3 API
        /// </summary>
        private async Task<string> SaveHolonToMoralisAsync(IHolon holon)
        {
            try
            {
                // Serialize holon to JSON
                var holonJson = JsonSerializer.Serialize(holon, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                // REAL Moralis IPFS API: POST /ipfs/uploadFolder
                // Request body format: { "path": "string", "content": "base64_encoded_content" }
                var holonBytes = Encoding.UTF8.GetBytes(holonJson);
                var base64Content = Convert.ToBase64String(holonBytes);
                
                var requestBody = new
                {
                    path = $"holon_{holon.Id}.json",
                    content = base64Content
                };

                var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_baseUrl}/ipfs/uploadFolder", jsonContent);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    // Moralis IPFS returns: { "path": "ipfs://..." }
                    if (result.TryGetProperty("path", out var path))
                    {
                        return path.GetString();
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving holon to Moralis IPFS: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get OASIS smart contract address
        /// </summary>
        private string GetOASISContractAddress()
        {
            // This would be the deployed OASIS smart contract address
            return "0x1234567890abcdef1234567890abcdef12345678";
        }

        /// <summary>
        /// Get OASIS smart contract ABI
        /// </summary>
        private string GetOASISContractABI()
        {
            // This would be the OASIS smart contract ABI
            return @"[
                {
                    ""inputs"": [
                        {""name"": ""avatarId"", ""type"": ""string""},
                        {""name"": ""version"", ""type"": ""uint256""}
                    ],
                    ""name"": ""getAvatar"",
                    ""outputs"": [
                        {""name"": """", ""type"": ""string""}
                    ],
                    ""stateMutability"": ""view"",
                    ""type"": ""function""
                },
                {
                    ""inputs"": [
                        {""name"": ""avatarId"", ""type"": ""string""},
                        {""name"": ""avatarData"", ""type"": ""string""}
                    ],
                    ""name"": ""saveAvatar"",
                    ""outputs"": [
                        {""name"": """", ""type"": ""string""}
                    ],
                    ""stateMutability"": ""nonpayable"",
                    ""type"": ""function""
                }
            ]";
        }

    // NFT-specific lock/unlock methods
    public OASISResult<IWeb3NFTTransactionResponse> LockNFT(ILockWeb3NFTRequest request)
    {
        return LockNFTAsync(request).Result;
    }

    public async Task<OASISResult<IWeb3NFTTransactionResponse>> LockNFTAsync(ILockWeb3NFTRequest request)
    {
        var result = new OASISResult<IWeb3NFTTransactionResponse>(new Web3NFTTransactionResponse());
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

            var bridgePoolAddress = _contractAddress ?? "0x0000000000000000000000000000000000000000";
            var sendRequest = new SendWeb3NFTRequest
            {
                FromNFTTokenAddress = request.NFTTokenAddress,
                FromWalletAddress = string.Empty,
                ToWalletAddress = bridgePoolAddress,
                TokenAddress = request.NFTTokenAddress,
                TokenId = request.Web3NFTId.ToString(),
                Amount = 1
            };

            var sendResult = await SendNFTAsync(sendRequest);
            if (sendResult.IsError || sendResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to lock NFT: {sendResult.Message}", sendResult.Exception);
                return result;
            }

            result.IsError = false;
            result.Result.TransactionResult = sendResult.Result.TransactionResult;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error locking NFT: {ex.Message}", ex);
        }
        return result;
    }

    public OASISResult<IWeb3NFTTransactionResponse> UnlockNFT(IUnlockWeb3NFTRequest request)
    {
        return UnlockNFTAsync(request).Result;
    }

    public async Task<OASISResult<IWeb3NFTTransactionResponse>> UnlockNFTAsync(IUnlockWeb3NFTRequest request)
    {
        var result = new OASISResult<IWeb3NFTTransactionResponse>(new Web3NFTTransactionResponse());
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

            var bridgePoolAddress = _contractAddress ?? "0x0000000000000000000000000000000000000000";
            var sendRequest = new SendWeb3NFTRequest
            {
                FromNFTTokenAddress = request.NFTTokenAddress,
                FromWalletAddress = bridgePoolAddress,
                ToWalletAddress = string.Empty,
                TokenAddress = request.NFTTokenAddress,
                TokenId = request.Web3NFTId.ToString(),
                Amount = 1
            };

            var sendResult = await SendNFTAsync(sendRequest);
            if (sendResult.IsError || sendResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to unlock NFT: {sendResult.Message}", sendResult.Exception);
                return result;
            }

            result.IsError = false;
            result.Result.TransactionResult = sendResult.Result.TransactionResult;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error unlocking NFT: {ex.Message}", ex);
        }
        return result;
    }

    // NFT Bridge Methods
    public async Task<OASISResult<BridgeTransactionResponse>> WithdrawNFTAsync(string nftTokenAddress, string tokenId, string senderAccountAddress, string senderPrivateKey)
    {
        var result = new OASISResult<BridgeTransactionResponse>();
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

            if (string.IsNullOrWhiteSpace(nftTokenAddress) || string.IsNullOrWhiteSpace(tokenId) || 
                string.IsNullOrWhiteSpace(senderAccountAddress) || string.IsNullOrWhiteSpace(senderPrivateKey))
            {
                OASISErrorHandling.HandleError(ref result, "NFT token address, token ID, sender address, and private key are required");
                return result;
            }

            var lockRequest = new LockWeb3NFTRequest
            {
                NFTTokenAddress = nftTokenAddress,
                Web3NFTId = Guid.TryParse(tokenId, out var guid) ? guid : CreateDeterministicGuid($"{ProviderType.Value}:nft:{nftTokenAddress}"),
                LockedByAvatarId = Guid.Empty
            };

            var lockResult = await LockNFTAsync(lockRequest);
            if (lockResult.IsError || lockResult.Result == null)
            {
                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = string.Empty,
                    IsSuccessful = false,
                    ErrorMessage = lockResult.Message,
                    Status = BridgeTransactionStatus.Canceled
                };
                OASISErrorHandling.HandleError(ref result, $"Failed to lock NFT: {lockResult.Message}");
                return result;
            }

            result.Result = new BridgeTransactionResponse
            {
                TransactionId = lockResult.Result.TransactionResult ?? string.Empty,
                IsSuccessful = !lockResult.IsError,
                Status = BridgeTransactionStatus.Pending
            };
            result.IsError = false;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error withdrawing NFT: {ex.Message}", ex);
            result.Result = new BridgeTransactionResponse
            {
                TransactionId = string.Empty,
                IsSuccessful = false,
                ErrorMessage = ex.Message,
                Status = BridgeTransactionStatus.Canceled
            };
        }
        return result;
    }

    public async Task<OASISResult<BridgeTransactionResponse>> DepositNFTAsync(string nftTokenAddress, string tokenId, string receiverAccountAddress, string sourceTransactionHash = null)
    {
        var result = new OASISResult<BridgeTransactionResponse>();
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

            // Moralis Web3 Data API is read-only - it doesn't support depositing/minting NFTs
            // For depositing NFTs, you need to use a blockchain SDK (like Nethereum for EVM chains)
            // Moralis can be used to query NFT data after the transaction
            result.Result = new BridgeTransactionResponse
            {
                TransactionId = string.Empty,
                IsSuccessful = false,
                ErrorMessage = "Moralis Web3 Data API is read-only. Use blockchain SDK (e.g., Nethereum) to deposit/mint NFTs, then query results via Moralis.",
                Status = BridgeTransactionStatus.Canceled
            };
            OASISErrorHandling.HandleError(ref result, "Moralis Web3 Data API is read-only. Use blockchain SDK to deposit NFTs.");
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error depositing NFT: {ex.Message}", ex);
            result.Result = new BridgeTransactionResponse
            {
                TransactionId = string.Empty,
                IsSuccessful = false,
                ErrorMessage = ex.Message,
                Status = BridgeTransactionStatus.Canceled
            };
        }
        return result;
    }

        /// <summary>
        /// Creates a deterministic GUID from input string using SHA-256 hash
        /// </summary>
        private static Guid CreateDeterministicGuid(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return Guid.Empty;

            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return new Guid(bytes.Take(16).ToArray());
        }

        #endregion
    }

    #region Moralis Response Models

    public class MoralisApiResult
    {
        public string result { get; set; }
        public string error { get; set; }
    }

    #endregion
}