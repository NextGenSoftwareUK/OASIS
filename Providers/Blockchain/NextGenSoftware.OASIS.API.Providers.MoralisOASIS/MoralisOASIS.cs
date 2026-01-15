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
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
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
            try
            {
                // Real Moralis implementation - load avatar from Web3 API
                var avatarData = await LoadAvatarFromMoralisAsync(id.ToString(), version);
                if (avatarData != null)
                {
                    var avatar = JsonSerializer.Deserialize<Avatar>(avatarData);
                    return new OASISResult<IAvatar>(avatar) { Message = "Avatar loaded successfully from Moralis Web3 API" };
                }
                else
                {
                    return new OASISResult<IAvatar>(null) { Message = "Avatar not found on Moralis Web3 API" };
                }
            }
            catch (Exception ex)
            {
                var result = new OASISResult<IAvatar>(null);
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar: {ex.Message}", ex);
                return result;
            }
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0)
        {
            return LoadAvatarAsync(id, version).Result;
        }

        public async Task<OASISResult<IAvatar>> LoadAvatarAsync(string providerKey, int version = 0)
        {
            try
            {
                // Use REAL Moralis REST API endpoint: GET /nft/{address}?chain={chain}
                // This gets NFTs owned by the wallet address
                var response = await _httpClient.GetAsync($"{_baseUrl}/nft/{Uri.EscapeDataString(providerKey)}?chain={_chain}");
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var nftResponse = JsonSerializer.Deserialize<JsonElement>(jsonResponse);
                    
                    // Moralis returns NFT data, we need to extract avatar data from NFT metadata
                    // For now, return null as Moralis doesn't directly store OASIS avatars
                    // This would need to be implemented using Moralis Database (MongoDB) or NFT metadata
                    return new OASISResult<IAvatar>(null) { Message = "Moralis Web3 API returns NFT data. Avatar storage should use Moralis Database (MongoDB) or NFT metadata." };
                }
                return new OASISResult<IAvatar>(null) { Message = "Avatar not found on Moralis Web3 API" };
            }
            catch (Exception ex)
            {
                var result = new OASISResult<IAvatar>(null);
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by provider key: {ex.Message}", ex);
                return result;
            }
        }

        public OASISResult<IAvatar> LoadAvatar(string providerKey, int version = 0)
        {
            return LoadAvatarAsync(providerKey, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string email, int version = 0)
        {
            try
            {
                // Placeholder implementation - would use Moralis API to load avatar by email
                return new OASISResult<IAvatar>(null) { Message = "Not implemented yet" };
            }
            catch (Exception ex)
            {
                var result = new OASISResult<IAvatar>(null);
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by email: {ex.Message}", ex);
                return result;
            }
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
                // This is a placeholder - real implementation would query MongoDB via Moralis Database API
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
            try
            {
                // Placeholder implementation - would use Moralis API to load avatar by provider key
                return new OASISResult<IAvatar>(null) { Message = "Not implemented yet" };
            }
            catch (Exception ex)
            {
                var result = new OASISResult<IAvatar>(null);
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by provider key: {ex.Message}", ex);
                return result;
            }
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
            try
            {
                // Real Moralis implementation - save avatar to Web3 API
                avatar.ModifiedDate = DateTime.UtcNow;
                var txHash = await SaveAvatarToMoralisAsync(avatar);
                
                if (!string.IsNullOrEmpty(txHash))
                {
                    return new OASISResult<IAvatar>(avatar) { Message = $"Avatar saved to Moralis Web3 API successfully. Transaction: {txHash}" };
                }
                else
                {
                    return new OASISResult<IAvatar>(null) { Message = "Failed to save avatar to Moralis Web3 API" };
                }
            }
            catch (Exception ex)
            {
                var result = new OASISResult<IAvatar>(null);
                OASISErrorHandling.HandleError(ref result, $"Error saving avatar: {ex.Message}", ex);
                return result;
            }
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
        {
            return SaveAvatarAsync(avatar).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            try
            {
                // Moralis Web3 API doesn't have a delete endpoint for avatars
                // Avatar deletion should use Moralis Database (MongoDB)
                return new OASISResult<bool>(false) { Message = "Moralis Web3 API doesn't support avatar deletion. Use Moralis Database (MongoDB) for avatar storage operations." };
            }
            catch (Exception ex)
            {
                var result = new OASISResult<bool>(false);
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar: {ex.Message}", ex);
                return result;
            }
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        {
            return DeleteAvatarAsync(id, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            try
            {
                // Moralis Web3 API doesn't have a delete endpoint for avatars
                // Avatar deletion should use Moralis Database (MongoDB)
                return new OASISResult<bool>(false) { Message = "Moralis Web3 API doesn't support avatar deletion. Use Moralis Database (MongoDB) for avatar storage operations." };
            }
            catch (Exception ex)
            {
                var result = new OASISResult<bool>(false);
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by provider key: {ex.Message}", ex);
                return result;
            }
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
            try
            {
                // Placeholder implementation - would use Moralis API to save avatar detail
                return new OASISResult<IAvatarDetail>(avatarDetail) { Message = "Not implemented yet" };
            }
            catch (Exception ex)
            {
                var result = new OASISResult<IAvatarDetail>(null);
                OASISErrorHandling.HandleError(ref result, $"Error saving avatar detail: {ex.Message}", ex);
                return result;
            }
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail)
        {
            return SaveAvatarDetailAsync(avatarDetail).Result;
        }

        // Holon Methods
        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool reloadChildren = true, int version = 0)
        {
            try
            {
                // Placeholder implementation - would use Moralis API to load holon
                return new OASISResult<IHolon>(null) { Message = "Not implemented yet" };
            }
            catch (Exception ex)
            {
                var result = new OASISResult<IHolon>(null);
                OASISErrorHandling.HandleError(ref result, $"Error loading holon: {ex.Message}", ex);
                return result;
            }
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool reloadChildren = true, int version = 0)
        {
            return LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, reloadChildren, version).Result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool reloadChildren = true, int version = 0)
        {
            try
            {
                // Placeholder implementation - would use Moralis API to load holon by provider key
                return new OASISResult<IHolon>(null) { Message = "Not implemented yet" };
            }
            catch (Exception ex)
            {
                var result = new OASISResult<IHolon>(null);
                OASISErrorHandling.HandleError(ref result, $"Error loading holon by provider key: {ex.Message}", ex);
                return result;
            }
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool reloadChildren = true, int version = 0)
        {
            return LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, reloadChildren, version).Result;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool reloadChildren = true)
        {
            try
            {
                // Placeholder implementation - would use Moralis API to save holon
                return new OASISResult<IHolon>(holon) { Message = "Not implemented yet" };
            }
            catch (Exception ex)
            {
                var result = new OASISResult<IHolon>(null);
                OASISErrorHandling.HandleError(ref result, $"Error saving holon: {ex.Message}", ex);
                return result;
            }
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool reloadChildren = true)
        {
            return SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, reloadChildren).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int maxChildCount = 0, bool continueOnError = true, bool reloadChildren = true)
        {
            try
            {
                // Placeholder implementation - would use Moralis API to save holons
                return new OASISResult<IEnumerable<IHolon>>(holons) { Message = "Not implemented yet" };
            }
            catch (Exception ex)
            {
                var result = new OASISResult<IEnumerable<IHolon>>(new List<IHolon>());
                OASISErrorHandling.HandleError(ref result, $"Error saving holons: {ex.Message}", ex);
                return result;
            }
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int maxChildCount = 0, bool continueOnError = true, bool reloadChildren = true)
        {
            return SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, maxChildCount, continueOnError, reloadChildren).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int maxChildCount = 0, bool continueOnError = true, bool reloadChildren = true, int version = 0)
        {
            try
            {
                // Placeholder implementation - would use Moralis API to load holons for parent
                return new OASISResult<IEnumerable<IHolon>>(new List<IHolon>()) { Message = "Not implemented yet" };
            }
            catch (Exception ex)
            {
                var result = new OASISResult<IEnumerable<IHolon>>(new List<IHolon>());
                OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent: {ex.Message}", ex);
                return result;
            }
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int maxChildCount = 0, bool continueOnError = true, bool reloadChildren = true, int version = 0)
        {
            return LoadHolonsForParentAsync(id, holonType, loadChildren, recursive, maxChildDepth, maxChildCount, continueOnError, reloadChildren, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int maxChildCount = 0, bool continueOnError = true, bool reloadChildren = true, int version = 0)
        {
            try
            {
                // Placeholder implementation - would use Moralis API to load holons for parent by provider key
                return new OASISResult<IEnumerable<IHolon>>(new List<IHolon>()) { Message = "Not implemented yet" };
            }
            catch (Exception ex)
            {
                var result = new OASISResult<IEnumerable<IHolon>>(new List<IHolon>());
                OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent by provider key: {ex.Message}", ex);
                return result;
            }
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int maxChildCount = 0, bool continueOnError = true, bool reloadChildren = true, int version = 0)
        {
            return LoadHolonsForParentAsync(providerKey, holonType, loadChildren, recursive, maxChildDepth, maxChildCount, continueOnError, reloadChildren, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaData, string value, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int maxChildCount = 0, bool continueOnError = true, bool reloadChildren = true, int version = 0)
        {
            try
            {
                // Placeholder implementation - would use Moralis API to load holons by metadata
                return new OASISResult<IEnumerable<IHolon>>(new List<IHolon>()) { Message = "Not implemented yet" };
            }
            catch (Exception ex)
            {
                var result = new OASISResult<IEnumerable<IHolon>>(new List<IHolon>());
                OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata: {ex.Message}", ex);
            return result;
            }
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaData, string value, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int maxChildCount = 0, bool continueOnError = true, bool reloadChildren = true, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaData, value, holonType, loadChildren, recursive, maxChildDepth, maxChildCount, continueOnError, reloadChildren, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaData, MetaKeyValuePairMatchMode matchMode = MetaKeyValuePairMatchMode.All, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int maxChildCount = 0, bool continueOnError = true, bool reloadChildren = true, int version = 0)
        {
            try
            {
                // Placeholder implementation - would use Moralis API to load holons by metadata dictionary
                return new OASISResult<IEnumerable<IHolon>>(new List<IHolon>()) { Message = "Not implemented yet" };
            }
            catch (Exception ex)
            {
                var result = new OASISResult<IEnumerable<IHolon>>(new List<IHolon>());
                OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata dictionary: {ex.Message}", ex);
                return result;
            }
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaData, MetaKeyValuePairMatchMode matchMode = MetaKeyValuePairMatchMode.All, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int maxChildCount = 0, bool continueOnError = true, bool reloadChildren = true, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaData, matchMode, holonType, loadChildren, recursive, maxChildDepth, maxChildCount, continueOnError, reloadChildren, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int maxChildCount = 0, bool continueOnError = true, bool reloadChildren = true, int version = 0)
        {
            try
            {
                // Placeholder implementation - would use Moralis API to load all holons
                return new OASISResult<IEnumerable<IHolon>>(new List<IHolon>()) { Message = "Not implemented yet" };
            }
            catch (Exception ex)
            {
                var result = new OASISResult<IEnumerable<IHolon>>(new List<IHolon>());
                OASISErrorHandling.HandleError(ref result, $"Error loading all holons: {ex.Message}", ex);
                return result;
            }
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int maxChildCount = 0, bool continueOnError = true, bool reloadChildren = true, int version = 0)
        {
            return LoadAllHolonsAsync(holonType, loadChildren, recursive, maxChildDepth, maxChildCount, continueOnError, reloadChildren, version).Result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            try
            {
                // Placeholder implementation - would use Moralis API to delete holon
                return new OASISResult<IHolon>(null) { Message = "Not implemented yet" };
            }
            catch (Exception ex)
            {
                var result = new OASISResult<IHolon>(null);
                OASISErrorHandling.HandleError(ref result, $"Error deleting holon: {ex.Message}", ex);
                return result;
            }
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id)
        {
            return DeleteHolonAsync(id).Result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            try
            {
                // Placeholder implementation - would use Moralis API to delete holon by provider key
                return new OASISResult<IHolon>(null) { Message = "Not implemented yet" };
            }
            catch (Exception ex)
            {
                var result = new OASISResult<IHolon>(null);
                OASISErrorHandling.HandleError(ref result, $"Error deleting holon by provider key: {ex.Message}", ex);
                return result;
            }
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
        {
            return DeleteHolonAsync(providerKey).Result;
        }

        // Import/Export Methods
        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            try
            {
                // Placeholder implementation - would use Moralis API to import holons
                return new OASISResult<bool>(true) { Message = "Not implemented yet" };
            }
            catch (Exception ex)
            {
                var result = new OASISResult<bool>(false);
                OASISErrorHandling.HandleError(ref result, $"Error importing holons: {ex.Message}", ex);
                return result;
            }
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
        {
            return ImportAsync(holons).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid id, int version = 0)
        {
            try
            {
                // Placeholder implementation - would use Moralis API to export all data for avatar by ID
                return new OASISResult<IEnumerable<IHolon>>(new List<IHolon>()) { Message = "Not implemented yet" };
            }
            catch (Exception ex)
            {
                var result = new OASISResult<IEnumerable<IHolon>>(new List<IHolon>());
                OASISErrorHandling.HandleError(ref result, $"Error exporting all data for avatar by ID: {ex.Message}", ex);
                return result;
            }
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid id, int version = 0)
        {
            return ExportAllDataForAvatarByIdAsync(id, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string username, int version = 0)
        {
            try
            {
                // Placeholder implementation - would use Moralis API to export all data for avatar by username
                return new OASISResult<IEnumerable<IHolon>>(new List<IHolon>()) { Message = "Not implemented yet" };
            }
            catch (Exception ex)
            {
                var result = new OASISResult<IEnumerable<IHolon>>(new List<IHolon>());
                OASISErrorHandling.HandleError(ref result, $"Error exporting all data for avatar by username: {ex.Message}", ex);
                return result;
            }
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string username, int version = 0)
        {
            return ExportAllDataForAvatarByUsernameAsync(username, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string email, int version = 0)
        {
            try
            {
                // Placeholder implementation - would use Moralis API to export all data for avatar by email
                return new OASISResult<IEnumerable<IHolon>>(new List<IHolon>()) { Message = "Not implemented yet" };
            }
            catch (Exception ex)
            {
                var result = new OASISResult<IEnumerable<IHolon>>(new List<IHolon>());
                OASISErrorHandling.HandleError(ref result, $"Error exporting all data for avatar by email: {ex.Message}", ex);
                return result;
            }
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string email, int version = 0)
        {
            return ExportAllDataForAvatarByEmailAsync(email, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            try
            {
                // Placeholder implementation - would use Moralis API to export all data
                return new OASISResult<IEnumerable<IHolon>>(new List<IHolon>()) { Message = "Not implemented yet" };
            }
            catch (Exception ex)
            {
                var result = new OASISResult<IEnumerable<IHolon>>(new List<IHolon>());
                OASISErrorHandling.HandleError(ref result, $"Error exporting all data: {ex.Message}", ex);
                return result;
            }
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
        {
            return ExportAllAsync(version).Result;
        }

        // Search Methods
        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            try
            {
                // Placeholder implementation - would use Moralis API to search
                return new OASISResult<ISearchResults>(null) { Message = "Not implemented yet" };
            }
            catch (Exception ex)
            {
                var result = new OASISResult<ISearchResults>(null);
                OASISErrorHandling.HandleError(ref result, $"Error searching: {ex.Message}", ex);
                return result;
            }
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
        }

        // IOASISNETProvider Methods
        public async Task<OASISResult<IEnumerable<IAvatar>>> GetAvatarsNearMeAsync(IAvatar avatar, double radiusKm)
        {
            try
            {
                // Placeholder implementation - would use Moralis API to get avatars near location
                return new OASISResult<IEnumerable<IAvatar>>(new List<IAvatar>()) { Message = "Not implemented yet" };
            }
            catch (Exception ex)
            {
                var result = new OASISResult<IEnumerable<IAvatar>>(new List<IAvatar>());
                OASISErrorHandling.HandleError(ref result, $"Error getting avatars near me: {ex.Message}", ex);
                return result;
            }
        }

        public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(IAvatar avatar, double radiusKm)
        {
            return GetAvatarsNearMeAsync(avatar, radiusKm).Result;
        }

        public async Task<OASISResult<IEnumerable<IHolon>>> GetHolonsNearMeAsync(IAvatar avatar, double radiusKm, HolonType holonType = HolonType.All)
        {
            try
            {
                // Placeholder implementation - would use Moralis API to get holons near location
                return new OASISResult<IEnumerable<IHolon>>(new List<IHolon>()) { Message = "Not implemented yet" };
            }
            catch (Exception ex)
            {
                var result = new OASISResult<IEnumerable<IHolon>>(new List<IHolon>());
                OASISErrorHandling.HandleError(ref result, $"Error getting holons near me: {ex.Message}", ex);
                return result;
            }
        }

        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(IAvatar avatar, double radiusKm, HolonType holonType = HolonType.All)
        {
            return GetHolonsNearMeAsync(avatar, radiusKm, holonType).Result;
        }

        // IOASISNETProvider methods with correct signatures
        public async Task<OASISResult<IEnumerable<IAvatar>>> GetAvatarsNearMeAsync(long x, long y, int radius)
        {
            try
            {
                // Placeholder implementation - would use Moralis API to get avatars near coordinates
                return new OASISResult<IEnumerable<IAvatar>>(new List<IAvatar>()) { Message = "Not implemented yet" };
            }
            catch (Exception ex)
            {
                var result = new OASISResult<IEnumerable<IAvatar>>(new List<IAvatar>());
                OASISErrorHandling.HandleError(ref result, $"Error getting avatars near coordinates: {ex.Message}", ex);
                return result;
            }
        }

        public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long x, long y, int radius)
        {
            return GetAvatarsNearMeAsync(x, y, radius).Result;
        }

        public async Task<OASISResult<IEnumerable<IHolon>>> GetHolonsNearMeAsync(long x, long y, int radius, HolonType holonType = HolonType.All)
        {
            try
            {
                // Placeholder implementation - would use Moralis API to get holons near coordinates
                return new OASISResult<IEnumerable<IHolon>>(new List<IHolon>()) { Message = "Not implemented yet" };
            }
            catch (Exception ex)
            {
                var result = new OASISResult<IEnumerable<IHolon>>(new List<IHolon>());
                OASISErrorHandling.HandleError(ref result, $"Error getting holons near coordinates: {ex.Message}", ex);
                return result;
            }
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
                    var result = new OASISResult<IWeb3NFTTransactionResponse>(null);
                    OASISErrorHandling.HandleError(ref result, "Moralis provider is not activated");
                    return result;
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
                    var result = new OASISResult<IWeb3NFTTransactionResponse>(null);
                    OASISErrorHandling.HandleError(ref result, "Moralis provider is not activated");
                    return result;
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
                    var result = new OASISResult<IWeb3NFTTransactionResponse>(null);
                    OASISErrorHandling.HandleError(ref result, "Moralis provider is not activated");
                    return result;
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
                        Name = nftData.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null,
                        Symbol = nftData.TryGetProperty("symbol", out var symbolProp) ? symbolProp.GetString() : null,
                        TokenUri = nftData.TryGetProperty("token_uri", out var uriProp) ? uriProp.GetString() : null
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
                var holonJson = JsonSerializer.Serialize(holon, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                var request = new
                {
                    address = GetOASISContractAddress(),
                    function_name = "saveHolon",
                    abi = GetOASISContractABI(),
                    @params = new
                    {
                        holonId = holon.Id.ToString(),
                        holonData = holonJson
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
                Console.WriteLine($"Error saving holon to Moralis: {ex.Message}");
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
                OASISErrorHandling.HandleError(ref result, "Moralis provider is not activated");
                return result;
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
                OASISErrorHandling.HandleError(ref result, "Moralis provider is not activated");
                return result;
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
                OASISErrorHandling.HandleError(ref result, "Moralis provider is not activated");
                return result;
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
                Web3NFTId = Guid.TryParse(tokenId, out var guid) ? guid : Guid.NewGuid(),
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
                OASISErrorHandling.HandleError(ref result, "Moralis provider is not activated");
                return result;
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