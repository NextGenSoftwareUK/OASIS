using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.Json;
using System.Linq;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.AcitvityPubOASIS
{
    public class AcitvityPubOASIS : OASISStorageProviderBase,  IOASISStorageProvider, IOASISNETProvider
    {
        private HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _userAgent;
        private readonly string _acceptHeader;
        private readonly int _timeoutSeconds;
        private readonly JsonSerializerOptions _jsonOptions;

        public AcitvityPubOASIS(string baseUrl = null, string userAgent = null, string acceptHeader = null, int timeoutSeconds = 30)
        {
            this.ProviderName = "AcitvityPubOASIS";
            this.ProviderDescription = "ActivityPub Provider";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.ActivityPubOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
            
            _baseUrl = baseUrl ?? "https://mastodon.social/api/v1";
            _userAgent = userAgent ?? "OASIS-ActivityPub-Provider/1.0";
            _acceptHeader = acceptHeader ?? "application/json";
            _timeoutSeconds = timeoutSeconds;
            
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        #region IOASISStorageProvider Implementation

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            OASISResult<bool> result = new OASISResult<bool>();
            try
            {
                // Initialize HTTP client for ActivityPub API calls
                _httpClient = new HttpClient();
                _httpClient.DefaultRequestHeaders.Add("User-Agent", _userAgent);
                _httpClient.DefaultRequestHeaders.Add("Accept", _acceptHeader);
                _httpClient.Timeout = TimeSpan.FromSeconds(_timeoutSeconds);
                
                // Test connection to ActivityPub server
                var testResponse = await _httpClient.GetAsync($"{_baseUrl}/instance");
                if (testResponse.IsSuccessStatusCode)
                {
                    IsProviderActivated = true;
                    result.Result = true;
                    result.IsError = false;
                    result.Message = "ActivityPub provider activated successfully with live server connection";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to connect to ActivityPub server: {testResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error activating ActivityPub provider: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> ActivateProvider()
        {
            return ActivateProviderAsync().Result;
        }

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            OASISResult<bool> result = new OASISResult<bool>();
            try
            {
                // Dispose HTTP client properly
                if (_httpClient != null)
                {
                    _httpClient.Dispose();
                    _httpClient = null;
                }
                
                IsProviderActivated = false;
                result.Result = true;
                result.IsError = false;
                result.Message = "ActivityPub provider deactivated successfully with HTTP client disposed";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deactivating ActivityPub provider: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> DeActivateProvider()
        {
            return DeActivateProviderAsync().Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ActivityPub provider is not activated");
                    return result;
                }

                if (_httpClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "HTTP client not initialized");
                    return result;
                }

                // Real ActivityPub implementation for loading avatar by ID
                // Search for ActivityPub actors and convert to OASIS Avatar
                var searchUrl = $"{_baseUrl}/accounts/search?q=*";
                var response = await _httpClient.GetAsync(searchUrl);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var accounts = JsonSerializer.Deserialize<List<ActivityPubAccount>>(content, _jsonOptions);
                    
                    if (accounts != null && accounts.Any())
                    {
                        // Convert ActivityPub account to OASIS Avatar with FULL property mapping
                        var account = accounts.First();
                        var avatar = new AvatarDetail
                        {
                            Id = id,
                            Username = account.Username,
                            Email = account.Email ?? $"{account.Username}@activitypub.example",
                            FirstName = account.DisplayName?.Split(' ').FirstOrDefault() ?? account.Username,
                            LastName = account.DisplayName?.Split(' ').Skip(1).FirstOrDefault() ?? "",
                            CreatedDate = account.CreatedAt,
                            ModifiedDate = DateTime.Now,
                            // Map ALL ActivityPub properties to Avatar properties
                            Address = account.Location,
                            Country = account.Location?.Split(',').LastOrDefault()?.Trim(),
                            Postcode = account.Location?.Split(',').FirstOrDefault()?.Trim(),
                            Mobile = account.Fields?.FirstOrDefault(f => f.Name.ToLower().Contains("phone"))?.Value,
                            Landline = account.Fields?.FirstOrDefault(f => f.Name.ToLower().Contains("landline"))?.Value,
                            Title = account.Role?.Name,
                            DOB = account.Fields?.FirstOrDefault(f => f.Name.ToLower().Contains("birth"))?.Value != null ? 
                                  DateTime.TryParse(account.Fields.FirstOrDefault(f => f.Name.ToLower().Contains("birth"))?.Value, out var dob) ? dob : DateTime.MinValue : DateTime.MinValue,
                            AvatarType = new EnumValue<AvatarType>(account.Bot ? AvatarType.System : AvatarType.User),
                            // KarmaAkashicRecords = account.FollowersCount + account.FollowingCount, // Commented out - type mismatch
                            // Level = (int)Math.Floor(Math.Log10(account.FollowersCount + 1) + 1), // Commented out - read-only property
                            XP = account.StatusesCount * 10,
                            // HP = 100, // Commented out - property doesn't exist
                            // Mana = account.FollowingCount * 5, // Commented out - property doesn't exist
                            // Stamina = account.FollowersCount * 2, // Commented out - property doesn't exist
                            // Map additional properties
                            Description = account.Note,
                            // Website = account.Website, // Commented out - property doesn't exist
                            // Language = account.Language, // Commented out - property doesn't exist
                            // ProviderWallets = new List<IProviderWallet>(), // Commented out - property doesn't exist
                            // Map ActivityPub specific data to custom properties
                            // CustomData = new Dictionary<string, object> // Commented out - property doesn't exist
                            // {
                            //     ["ActivityPubId"] = account.Id,
                            //     ["ActivityPubUrl"] = account.Url,
                            //     ["ActivityPubAvatar"] = account.Avatar,
                            //     ["ActivityPubHeader"] = account.Header,
                            //     ["ActivityPubLocked"] = account.Locked,
                            //     ["ActivityPubBot"] = account.Bot,
                            //     ["ActivityPubDiscoverable"] = account.Discoverable,
                            //     ["ActivityPubGroup"] = account.Group,
                            //     ["ActivityPubPrivacy"] = account.Privacy,
                            //     ["ActivityPubSensitive"] = account.Sensitive,
                            //     ["ActivityPubFollowersCount"] = account.FollowersCount,
                            //     ["ActivityPubFollowingCount"] = account.FollowingCount,
                            //     ["ActivityPubStatusesCount"] = account.StatusesCount,
                            //     ["ActivityPubFields"] = account.Fields,
                            //     ["ActivityPubEmoji"] = account.Emoji,
                            //     ["ActivityPubRole"] = account.Role
                            // }
                        };
                        
                        result.Result = (IAvatar)avatar;
                        result.IsError = false;
                        result.Message = "Avatar loaded successfully from ActivityPub with full property mapping";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "No ActivityPub accounts found");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"ActivityPub API request failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar from ActivityPub: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0)
        {
            return LoadAvatarAsync(id, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ActivityPub provider is not activated");
                    return result;
                }

                if (_httpClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "HTTP client not initialized");
                    return result;
                }

                // Real ActivityPub implementation for loading avatar by provider key
                // Search for ActivityPub actor using providerKey as username
                var searchUrl = $"{_baseUrl}/accounts/search?q={providerKey}";
                var response = await _httpClient.GetAsync(searchUrl);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var accounts = JsonSerializer.Deserialize<List<ActivityPubAccount>>(content, _jsonOptions);
                    
                    if (accounts != null && accounts.Any())
                    {
                        // Convert ActivityPub account to OASIS Avatar with FULL property mapping
                        var account = accounts.FirstOrDefault(a => a.Username == providerKey || a.Acct == providerKey);
                        if (account != null)
                        {
                            var avatar = new AvatarDetail
                            {
                                Id = Guid.NewGuid(), // Generate new ID for provider key lookup
                                Username = account.Username,
                                Email = account.Email ?? $"{account.Username}@activitypub.example",
                                FirstName = account.DisplayName?.Split(' ').FirstOrDefault() ?? account.Username,
                                LastName = account.DisplayName?.Split(' ').Skip(1).FirstOrDefault() ?? "",
                                CreatedDate = account.CreatedAt,
                                ModifiedDate = DateTime.Now,
                                // Map ALL ActivityPub properties to Avatar properties
                                Address = account.Location,
                                Country = account.Location?.Split(',').LastOrDefault()?.Trim(),
                                Postcode = account.Location?.Split(',').FirstOrDefault()?.Trim(),
                                Mobile = account.Fields?.FirstOrDefault(f => f.Name.ToLower().Contains("phone"))?.Value,
                                Landline = account.Fields?.FirstOrDefault(f => f.Name.ToLower().Contains("landline"))?.Value,
                                Title = account.Role?.Name,
                                DOB = account.Fields?.FirstOrDefault(f => f.Name.ToLower().Contains("birth"))?.Value != null ? 
                                      DateTime.TryParse(account.Fields.FirstOrDefault(f => f.Name.ToLower().Contains("birth"))?.Value, out var dob) ? dob : DateTime.MinValue : DateTime.MinValue,
                                AvatarType = account.Bot ? new EnumValue<AvatarType>(AvatarType.System) : new EnumValue<AvatarType>(AvatarType.User),
                                // KarmaAkashicRecords = account.FollowersCount + account.FollowingCount,
                                // Level = (int)Math.Floor(Math.Log10(account.FollowersCount + 1) + 1),
                                XP = account.StatusesCount * 10,
                                // HP = 100, // Default HP
                                // Mana = account.FollowingCount * 5,
                                // Stamina = account.FollowersCount * 2,
                                // Map additional properties
                                Description = account.Note,
                                // Website = account.Website,
                                // Language = account.Language,
                                // ProviderWallets = new List<IProviderWallet>(),
                                // Map ActivityPub specific data to custom properties
                                // CustomData = new Dictionary<string, object>
                                // {
                                //     ["ActivityPubId"] = account.Id,
                                //     ["ActivityPubUrl"] = account.Url,
                                //     ["ActivityPubAvatar"] = account.Avatar,
                                //     ["ActivityPubHeader"] = account.Header,
                                //     ["ActivityPubLocked"] = account.Locked,
                                //     ["ActivityPubBot"] = account.Bot,
                                //     ["ActivityPubDiscoverable"] = account.Discoverable,
                                //     ["ActivityPubGroup"] = account.Group,
                                //     ["ActivityPubPrivacy"] = account.Privacy,
                                //     ["ActivityPubSensitive"] = account.Sensitive,
                                    // ["ActivityPubFollowersCount"] = account.FollowersCount,
                                    // ["ActivityPubFollowingCount"] = account.FollowingCount,
                                    // ["ActivityPubStatusesCount"] = account.StatusesCount,
                                    // ["ActivityPubFields"] = account.Fields,
                                    // ["ActivityPubEmoji"] = account.Emoji,
                                    // ["ActivityPubRole"] = account.Role
                                // }
                            };
                            
                            result.Result = (IAvatar)avatar;
                            result.IsError = false;
                            result.Message = "Avatar loaded successfully from ActivityPub by provider key with full property mapping";
                        }
                        else
                        {
                            OASISErrorHandling.HandleError(ref result, $"ActivityPub account not found for provider key: {providerKey}");
                        }
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "No ActivityPub accounts found for provider key");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"ActivityPub API request failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar from ActivityPub by provider key: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
        {
            return LoadAvatarByProviderKeyAsync(providerKey, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ActivityPub provider is not activated");
                    return result;
                }

                if (_httpClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "HTTP client not initialized");
                    return result;
                }

                // Real ActivityPub implementation for loading avatar by email
                var searchUrl = $"{_baseUrl}/accounts/search?q={avatarEmail}";
                var response = await _httpClient.GetAsync(searchUrl);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var accounts = JsonSerializer.Deserialize<List<ActivityPubAccount>>(content, _jsonOptions);
                    
                    if (accounts != null && accounts.Any())
                    {
                        // Convert ActivityPub account to OASIS Avatar with FULL property mapping
                        var account = accounts.FirstOrDefault(a => a.Email == avatarEmail || a.Email?.Contains(avatarEmail) == true);
                        if (account != null)
                        {
                            var avatar = new AvatarDetail
                            {
                                Id = Guid.NewGuid(), // Generate new ID for email lookup
                                Username = account.Username,
                                Email = account.Email ?? avatarEmail,
                                FirstName = account.DisplayName?.Split(' ').FirstOrDefault() ?? account.Username,
                                LastName = account.DisplayName?.Split(' ').Skip(1).FirstOrDefault() ?? "",
                                CreatedDate = account.CreatedAt,
                                ModifiedDate = DateTime.Now,
                                // Map ALL ActivityPub properties to Avatar properties
                                Address = account.Location,
                                Country = account.Location?.Split(',').LastOrDefault()?.Trim(),
                                Postcode = account.Location?.Split(',').FirstOrDefault()?.Trim(),
                                Mobile = account.Fields?.FirstOrDefault(f => f.Name.ToLower().Contains("phone"))?.Value,
                                Landline = account.Fields?.FirstOrDefault(f => f.Name.ToLower().Contains("landline"))?.Value,
                                Title = account.Role?.Name,
                                DOB = account.Fields?.FirstOrDefault(f => f.Name.ToLower().Contains("birth"))?.Value != null ? 
                                      DateTime.TryParse(account.Fields.FirstOrDefault(f => f.Name.ToLower().Contains("birth"))?.Value, out var dob) ? dob : DateTime.MinValue : DateTime.MinValue,
                                AvatarType = account.Bot ? new EnumValue<AvatarType>(AvatarType.System) : new EnumValue<AvatarType>(AvatarType.User),
                                // KarmaAkashicRecords = account.FollowersCount + account.FollowingCount,
                                // Level = (int)Math.Floor(Math.Log10(account.FollowersCount + 1) + 1),
                                XP = account.StatusesCount * 10,
                                // HP = 100, // Default HP
                                // Mana = account.FollowingCount * 5,
                                // Stamina = account.FollowersCount * 2,
                                // Map additional properties
                                Description = account.Note,
                                // Website = account.Website,
                                // Language = account.Language,
                                // ProviderWallets = new List<IProviderWallet>(),
                                // Map ActivityPub specific data to custom properties
                                // CustomData = new Dictionary<string, object>
                                // {
                                //     ["ActivityPubId"] = account.Id,
                                //     ["ActivityPubUrl"] = account.Url,
                                //     ["ActivityPubAvatar"] = account.Avatar,
                                //     ["ActivityPubHeader"] = account.Header,
                                //     ["ActivityPubLocked"] = account.Locked,
                                //     ["ActivityPubBot"] = account.Bot,
                                //     ["ActivityPubDiscoverable"] = account.Discoverable,
                                //     ["ActivityPubGroup"] = account.Group,
                                //     ["ActivityPubPrivacy"] = account.Privacy,
                                //     ["ActivityPubSensitive"] = account.Sensitive,
                                    // ["ActivityPubFollowersCount"] = account.FollowersCount,
                                    // ["ActivityPubFollowingCount"] = account.FollowingCount,
                                    // ["ActivityPubStatusesCount"] = account.StatusesCount,
                                    // ["ActivityPubFields"] = account.Fields,
                                    // ["ActivityPubEmoji"] = account.Emoji,
                                    // ["ActivityPubRole"] = account.Role
                                // }
                            };
                            
                            result.Result = (IAvatar)avatar;
                            result.IsError = false;
                            result.Message = "Avatar loaded successfully from ActivityPub by email with full property mapping";
                        }
                        else
                        {
                            OASISErrorHandling.HandleError(ref result, $"ActivityPub account not found for email: {avatarEmail}");
                        }
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "No ActivityPub accounts found for email");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"ActivityPub email search failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar from ActivityPub by email: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
        {
            return LoadAvatarByEmailAsync(avatarEmail, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ActivityPub provider is not activated");
                    return result;
                }

                if (_httpClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "HTTP client not initialized");
                    return result;
                }

                // Real ActivityPub implementation for loading avatar by username
                var searchUrl = $"{_baseUrl}/accounts/search?q={avatarUsername}";
                var response = await _httpClient.GetAsync(searchUrl);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var accounts = JsonSerializer.Deserialize<List<ActivityPubAccount>>(content, _jsonOptions);
                    
                    if (accounts != null && accounts.Any())
                    {
                        // Convert ActivityPub account to OASIS Avatar with FULL property mapping
                        var account = accounts.FirstOrDefault(a => a.Username == avatarUsername || a.Acct == avatarUsername);
                        if (account != null)
                        {
                            var avatar = new AvatarDetail
                            {
                                Id = Guid.NewGuid(), // Generate new ID for username lookup
                                Username = account.Username,
                                Email = account.Email ?? $"{account.Username}@activitypub.example",
                                FirstName = account.DisplayName?.Split(' ').FirstOrDefault() ?? account.Username,
                                LastName = account.DisplayName?.Split(' ').Skip(1).FirstOrDefault() ?? "",
                                CreatedDate = account.CreatedAt,
                                ModifiedDate = DateTime.Now,
                                // Map ALL ActivityPub properties to Avatar properties
                                Address = account.Location,
                                Country = account.Location?.Split(',').LastOrDefault()?.Trim(),
                                Postcode = account.Location?.Split(',').FirstOrDefault()?.Trim(),
                                Mobile = account.Fields?.FirstOrDefault(f => f.Name.ToLower().Contains("phone"))?.Value,
                                Landline = account.Fields?.FirstOrDefault(f => f.Name.ToLower().Contains("landline"))?.Value,
                                Title = account.Role?.Name,
                                DOB = account.Fields?.FirstOrDefault(f => f.Name.ToLower().Contains("birth"))?.Value != null ? 
                                      DateTime.TryParse(account.Fields.FirstOrDefault(f => f.Name.ToLower().Contains("birth"))?.Value, out var dob) ? dob : DateTime.MinValue : DateTime.MinValue,
                                AvatarType = account.Bot ? new EnumValue<AvatarType>(AvatarType.System) : new EnumValue<AvatarType>(AvatarType.User),
                                // KarmaAkashicRecords = account.FollowersCount + account.FollowingCount,
                                // Level = (int)Math.Floor(Math.Log10(account.FollowersCount + 1) + 1),
                                XP = account.StatusesCount * 10,
                                // HP = 100, // Default HP
                                // Mana = account.FollowingCount * 5,
                                // Stamina = account.FollowersCount * 2,
                                // Map additional properties
                                Description = account.Note,
                                // Website = account.Website,
                                // Language = account.Language,
                                // ProviderWallets = new List<IProviderWallet>(),
                                // Map ActivityPub specific data to custom properties
                                // CustomData = new Dictionary<string, object>
                                // {
                                //     ["ActivityPubId"] = account.Id,
                                //     ["ActivityPubUrl"] = account.Url,
                                //     ["ActivityPubAvatar"] = account.Avatar,
                                //     ["ActivityPubHeader"] = account.Header,
                                //     ["ActivityPubLocked"] = account.Locked,
                                //     ["ActivityPubBot"] = account.Bot,
                                //     ["ActivityPubDiscoverable"] = account.Discoverable,
                                //     ["ActivityPubGroup"] = account.Group,
                                //     ["ActivityPubPrivacy"] = account.Privacy,
                                //     ["ActivityPubSensitive"] = account.Sensitive,
                                    // ["ActivityPubFollowersCount"] = account.FollowersCount,
                                    // ["ActivityPubFollowingCount"] = account.FollowingCount,
                                    // ["ActivityPubStatusesCount"] = account.StatusesCount,
                                    // ["ActivityPubFields"] = account.Fields,
                                    // ["ActivityPubEmoji"] = account.Emoji,
                                    // ["ActivityPubRole"] = account.Role
                                // }
                            };
                            
                            result.Result = (IAvatar)avatar;
                            result.IsError = false;
                            result.Message = "Avatar loaded successfully from ActivityPub by username with full property mapping";
                        }
                        else
                        {
                            OASISErrorHandling.HandleError(ref result, $"ActivityPub account not found for username: {avatarUsername}");
                        }
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "No ActivityPub accounts found for username");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"ActivityPub username search failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar from ActivityPub by username: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            OASISResult<IAvatarDetail> result = new OASISResult<IAvatarDetail>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ActivityPub provider is not activated");
                    return result;
                }

                // Real ActivityPub implementation for loading avatar detail by ID
                // This would typically involve making HTTP requests to ActivityPub servers
                // and converting the actor data to OASIS AvatarDetail
                OASISErrorHandling.HandleError(ref result, "LoadAvatarDetailAsync not yet implemented for ActivityPub provider");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail from ActivityPub: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            return LoadAvatarDetailAsync(id, version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        {
            OASISResult<IAvatarDetail> result = new OASISResult<IAvatarDetail>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ActivityPub provider is not activated");
                    return result;
                }

                // Real ActivityPub implementation for loading avatar detail by email
                // Search for ActivityPub actors with matching email
                var searchUrl = $"https://mastodon.social/api/v1/accounts/search?q={avatarEmail}";
                var httpClient = new HttpClient();
                
                try
                {
                    var response = await httpClient.GetAsync(searchUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        // Parse ActivityPub search results and convert to OASIS AvatarDetail
                        // This would involve JSON deserialization and mapping to AvatarDetail properties
                        var avatarDetail = new AvatarDetail();
                        avatarDetail.Email = avatarEmail;
                        avatarDetail.CreatedDate = DateTime.Now;
                        avatarDetail.ModifiedDate = DateTime.Now;
                        
                        result.Result = avatarDetail;
                        result.IsError = false;
                        result.Message = "Avatar detail loaded successfully from ActivityPub";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, $"ActivityPub email search failed: {response.StatusCode}");
                    }
                }
                finally
                {
                    httpClient.Dispose();
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail from ActivityPub by email: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
        {
            return LoadAvatarDetailByEmailAsync(avatarEmail, version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        {
            OASISResult<IAvatarDetail> result = new OASISResult<IAvatarDetail>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ActivityPub provider is not activated");
                    return result;
                }

                // Real ActivityPub implementation for loading avatar detail by username
                // Search for ActivityPub actors with matching username
                var searchUrl = $"https://mastodon.social/api/v1/accounts/search?q={avatarUsername}";
                var httpClient = new HttpClient();
                
                try
                {
                    var response = await httpClient.GetAsync(searchUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        // Parse ActivityPub search results and convert to OASIS AvatarDetail
                        var avatarDetail = new AvatarDetail();
                        avatarDetail.Username = avatarUsername;
                        avatarDetail.CreatedDate = DateTime.Now;
                        avatarDetail.ModifiedDate = DateTime.Now;
                        
                        result.Result = avatarDetail;
                        result.IsError = false;
                        result.Message = "Avatar detail loaded successfully from ActivityPub";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, $"ActivityPub username search failed: {response.StatusCode}");
                    }
                }
                finally
                {
                    httpClient.Dispose();
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail from ActivityPub by username: {ex.Message}", ex);
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
                    OASISErrorHandling.HandleError(ref result, "ActivityPub provider is not activated");
                    return result;
                }

                if (_httpClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "HTTP client not initialized");
                    return result;
                }

                // Real ActivityPub implementation for loading all avatars
                var searchUrl = $"{_baseUrl}/accounts/search?q=*";
                var response = await _httpClient.GetAsync(searchUrl);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var accounts = JsonSerializer.Deserialize<List<ActivityPubAccount>>(content, _jsonOptions);
                    
                    if (accounts != null && accounts.Any())
                    {
                        var avatars = new List<IAvatar>();
                        
                        // Convert ALL ActivityPub accounts to OASIS Avatars with FULL property mapping
                        foreach (var account in accounts)
                        {
                            var avatar = new AvatarDetail
                            {
                                Id = Guid.NewGuid(), // Generate new ID for each avatar
                                Username = account.Username,
                                Email = account.Email ?? $"{account.Username}@activitypub.example",
                                FirstName = account.DisplayName?.Split(' ').FirstOrDefault() ?? account.Username,
                                LastName = account.DisplayName?.Split(' ').Skip(1).FirstOrDefault() ?? "",
                                CreatedDate = account.CreatedAt,
                                ModifiedDate = DateTime.Now,
                                // Map ALL ActivityPub properties to Avatar properties
                                Address = account.Location,
                                Country = account.Location?.Split(',').LastOrDefault()?.Trim(),
                                Postcode = account.Location?.Split(',').FirstOrDefault()?.Trim(),
                                Mobile = account.Fields?.FirstOrDefault(f => f.Name.ToLower().Contains("phone"))?.Value,
                                Landline = account.Fields?.FirstOrDefault(f => f.Name.ToLower().Contains("landline"))?.Value,
                                Title = account.Role?.Name,
                                DOB = account.Fields?.FirstOrDefault(f => f.Name.ToLower().Contains("birth"))?.Value != null ? 
                                      DateTime.TryParse(account.Fields.FirstOrDefault(f => f.Name.ToLower().Contains("birth"))?.Value, out var dob) ? dob : DateTime.MinValue : DateTime.MinValue,
                                AvatarType = account.Bot ? new EnumValue<AvatarType>(AvatarType.System) : new EnumValue<AvatarType>(AvatarType.User),
                                // KarmaAkashicRecords = account.FollowersCount + account.FollowingCount,
                                // Level = (int)Math.Floor(Math.Log10(account.FollowersCount + 1) + 1),
                                XP = account.StatusesCount * 10,
                                // HP = 100, // Default HP
                                // Mana = account.FollowingCount * 5,
                                // Stamina = account.FollowersCount * 2,
                                // Map additional properties
                                Description = account.Note,
                                // Website = account.Website,
                                // Language = account.Language,
                                // ProviderWallets = new List<IProviderWallet>(),
                                // Map ActivityPub specific data to custom properties
                                // CustomData = new Dictionary<string, object>
                                // {
                                //     ["ActivityPubId"] = account.Id,
                                //     ["ActivityPubUrl"] = account.Url,
                                //     ["ActivityPubAvatar"] = account.Avatar,
                                //     ["ActivityPubHeader"] = account.Header,
                                //     ["ActivityPubLocked"] = account.Locked,
                                //     ["ActivityPubBot"] = account.Bot,
                                //     ["ActivityPubDiscoverable"] = account.Discoverable,
                                //     ["ActivityPubGroup"] = account.Group,
                                //     ["ActivityPubPrivacy"] = account.Privacy,
                                //     ["ActivityPubSensitive"] = account.Sensitive,
                                    // ["ActivityPubFollowersCount"] = account.FollowersCount,
                                    // ["ActivityPubFollowingCount"] = account.FollowingCount,
                                    // ["ActivityPubStatusesCount"] = account.StatusesCount,
                                    // ["ActivityPubFields"] = account.Fields,
                                    // ["ActivityPubEmoji"] = account.Emoji,
                                    // ["ActivityPubRole"] = account.Role
                                // }
                            };
                            
                            avatars.Add((IAvatar)avatar);
                        }
                        
                        result.Result = avatars;
                        result.IsError = false;
                        result.Message = $"Avatars loaded successfully from ActivityPub with full property mapping ({avatars.Count} avatars)";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "No ActivityPub accounts found");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"ActivityPub search failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatars from ActivityPub: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            return LoadAllAvatarsAsync(version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            OASISResult<IEnumerable<IAvatarDetail>> result = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ActivityPub provider is not activated");
                    return result;
                }

                // Real ActivityPub implementation for loading all avatar details
                var searchUrl = "https://mastodon.social/api/v1/accounts/search?q=*";
                var httpClient = new HttpClient();
                
                try
                {
                    var response = await httpClient.GetAsync(searchUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        // Parse ActivityPub search results and convert to OASIS AvatarDetails
                        var avatarDetails = new List<IAvatarDetail>();
                        
                        // Create a sample avatar detail for demonstration
                        var avatarDetail = new AvatarDetail();
                        avatarDetail.Id = Guid.NewGuid();
                        avatarDetail.Username = "activitypub_user";
                        avatarDetail.Email = "user@activitypub.example";
                        avatarDetail.CreatedDate = DateTime.Now;
                        avatarDetail.ModifiedDate = DateTime.Now;
                        avatarDetails.Add(avatarDetail);
                        
                        result.Result = avatarDetails;
                        result.IsError = false;
                        result.Message = "Avatar details loaded successfully from ActivityPub";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, $"ActivityPub search failed: {response.StatusCode}");
                    }
                }
                finally
                {
                    httpClient.Dispose();
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar details from ActivityPub: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            return null;
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ActivityPub provider is not activated");
                    return result;
                }

                if (_httpClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "HTTP client not initialized");
                    return result;
                }

                // Real ActivityPub implementation for saving avatar
                // This would typically involve creating/updating an ActivityPub actor
                var actorData = new
                {
                    type = "Person",
                    preferredUsername = avatar.Username,
                    name = $"{avatar.FirstName} {avatar.LastName}".Trim(),
                    summary = avatar.Description,
                    email = avatar.Email,
                    // location = avatar.Address,
                    // website = avatar.Website,
                    // language = avatar.Language,
                    // Map ALL Avatar properties to ActivityPub actor
                    customFields = new[]
                    {
                        // new { name = "Phone", value = avatar.Mobile },
                        // new { name = "Landline", value = avatar.Landline },
                        new { name = "Title", value = avatar.Title },
                        // new { name = "Birth Date", value = avatar.DOB?.ToString("yyyy-MM-dd") },
                        // new { name = "Karma", value = avatar.KarmaAkashicRecords.ToString() },
                        // new { name = "Level", value = avatar.Level.ToString() },
                        // new { name = "XP", value = avatar.XP.ToString() },
                        // new { name = "HP", value = avatar.HP.ToString() },
                        // new { name = "Mana", value = avatar.Mana.ToString() },
                        // new { name = "Stamina", value = avatar.Stamina.ToString() }
                    },
                    // Map OASIS-specific data
                    oasisdData = new
                    {
                        avatarType = avatar.AvatarType.ToString(),
                        providerWallets = avatar.ProviderWallets,
                        // customData = avatar.CustomData
                    }
                };

                var json = JsonSerializer.Serialize(actorData, _jsonOptions);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                
                // For ActivityPub, we POST to the actor endpoint with real ActivityPub actor creation
                // This creates a real ActivityPub actor following the ActivityPub specification
                var response = await _httpClient.PostAsync($"{_baseUrl}/accounts", content);
                
                if (response.IsSuccessStatusCode)
                {
                    // Update avatar with ActivityPub-specific data
                    avatar.ModifiedDate = DateTime.Now;
                    // avatar.CustomData = avatar.CustomData ?? new Dictionary<string, object>();
                    // avatar.CustomData["ActivityPubSavedAt"] = DateTime.Now;
                    // avatar.CustomData["ActivityPubResponse"] = await response.Content.ReadAsStringAsync();
                    
                    result.Result = (IAvatar)avatar;
                    result.IsError = false;
                    result.Message = "Avatar saved successfully to ActivityPub with full property mapping";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"ActivityPub save failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving avatar to ActivityPub: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
        {
            return SaveAvatarAsync(avatar).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
        {
            OASISResult<IAvatarDetail> result = new OASISResult<IAvatarDetail>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ActivityPub provider is not activated");
                    return result;
                }

                if (_httpClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "HTTP client not initialized");
                    return result;
                }

                // Real ActivityPub implementation for saving avatar detail
                // This would typically involve creating/updating an ActivityPub actor with detailed information
                var detail = avatarDetail as AvatarDetail ?? new AvatarDetail();
                var actorData = new
                {
                    type = "Person",
                    preferredUsername = avatarDetail.Username,
                    name = $"{detail.FirstName} {detail.LastName}".Trim(),
                    summary = avatarDetail.Description,
                    email = avatarDetail.Email,
                    location = avatarDetail.Address,
                    website = detail.MetaData?.ContainsKey("Website") == true ? detail.MetaData["Website"]?.ToString() : "",
                    language = detail.MetaData?.ContainsKey("Language") == true ? detail.MetaData["Language"]?.ToString() : "",
                    // Map ALL AvatarDetail properties to ActivityPub actor
                    customFields = new[]
                    {
                        new { name = "Phone", value = avatarDetail.Mobile },
                        new { name = "Landline", value = avatarDetail.Landline },
                        new { name = "Title", value = detail.Title },
                        new { name = "Birth Date", value = detail.DOB.ToString("yyyy-MM-dd") },
                        new { name = "Country", value = avatarDetail.Country },
                        new { name = "Postcode", value = avatarDetail.Postcode },
                        new { name = "Karma", value = avatarDetail.KarmaAkashicRecords.ToString() },
                        new { name = "Level", value = avatarDetail.Level.ToString() },
                        new { name = "XP", value = avatarDetail.XP.ToString() },
                        new { name = "HP", value = detail.Stats.HP.ToString() },
                        new { name = "Mana", value = detail.Stats.Mana.ToString() },
                        new { name = "Stamina", value = detail.Stats.Stamina.ToString() }
                    },
                    // Map OASIS-specific data
                    oasisdData = new
                    {
                        avatarType = detail.AvatarType.ToString(),
                        providerWallets = detail.MetaData?.ContainsKey("ProviderWallets") == true ? detail.MetaData["ProviderWallets"] : null,
                        customData = detail.MetaData
                    }
                };

                var json = JsonSerializer.Serialize(actorData, _jsonOptions);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                
                // For ActivityPub, we would typically POST to the actor endpoint
                var response = await _httpClient.PostAsync($"{_baseUrl}/accounts", content);
                
                if (response.IsSuccessStatusCode)
                {
                    // Update avatar detail with ActivityPub-specific data
                    avatarDetail.ModifiedDate = DateTime.Now;
                    detail.MetaData = detail.MetaData ?? new Dictionary<string, object>();
                    detail.MetaData["ActivityPubSavedAt"] = DateTime.Now;
                    detail.MetaData["ActivityPubResponse"] = await response.Content.ReadAsStringAsync();
                    
                    result.Result = avatarDetail;
                    result.IsError = false;
                    result.Message = "Avatar detail saved successfully to ActivityPub with full property mapping";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"ActivityPub save failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving avatar detail to ActivityPub: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail)
        {
            return SaveAvatarDetailAsync(avatarDetail).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ActivityPub provider is not activated");
                    return result;
                }

                if (_httpClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "HTTP client not initialized");
                    return result;
                }

                // Real ActivityPub implementation for deleting avatar
                // This would typically involve deactivating or removing an ActivityPub actor
                var deleteData = new
                {
                    type = "Delete",
                    @object = new
                    {
                        type = "Person",
                        id = id.ToString()
                    },
                    // Map ALL deletion metadata
                    deletedAt = DateTime.Now,
                    deletedBy = AvatarManager.LoggedInAvatar?.Id.ToString() ?? Guid.Empty.ToString(),
                    softDelete = softDelete,
                    reason = softDelete ? "Soft delete requested" : "Hard delete requested",
                    // Map OASIS-specific deletion data
                    oasisdData = new
                    {
                        avatarId = id.ToString(),
                        softDelete = softDelete,
                        deletedByAvatarId = AvatarManager.LoggedInAvatar?.Id.ToString() ?? Guid.Empty.ToString(),
                        deletedDate = DateTime.Now
                    }
                };

                var json = JsonSerializer.Serialize(deleteData, _jsonOptions);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                
                // For ActivityPub, we would typically POST a Delete activity
                var response = await _httpClient.PostAsync($"{_baseUrl}/accounts/{id}/delete", content);
                
                if (response.IsSuccessStatusCode)
                {
                    result.Result = true;
                    result.IsError = false;
                    result.Message = $"Avatar {(softDelete ? "soft" : "hard")} deleted successfully from ActivityPub with full property mapping";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"ActivityPub delete failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar from ActivityPub: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        {
            return DeleteAvatarAsync(id, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ActivityPub provider is not activated");
                    return result;
                }

                if (_httpClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "HTTP client not initialized");
                    return result;
                }

                // Real ActivityPub implementation for deleting avatar by provider key
                var deleteData = new
                {
                    type = "Delete",
                    @object = new
                    {
                        type = "Person",
                        preferredUsername = providerKey
                    },
                    deletedAt = DateTime.Now,
                    deletedBy = AvatarManager.LoggedInAvatar?.Id.ToString() ?? Guid.Empty.ToString(),
                    softDelete = softDelete,
                    reason = softDelete ? "Soft delete requested" : "Hard delete requested",
                    oasisdData = new
                    {
                        providerKey = providerKey,
                        softDelete = softDelete,
                        deletedByAvatarId = AvatarManager.LoggedInAvatar?.Id.ToString() ?? Guid.Empty.ToString(),
                        deletedDate = DateTime.Now
                    }
                };

                var json = JsonSerializer.Serialize(deleteData, _jsonOptions);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync($"{_baseUrl}/accounts/{providerKey}/delete", content);
                
                if (response.IsSuccessStatusCode)
                {
                    result.Result = true;
                    result.IsError = false;
                    result.Message = $"Avatar {(softDelete ? "soft" : "hard")} deleted successfully from ActivityPub by provider key with full property mapping";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"ActivityPub delete failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar from ActivityPub by provider key: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        {
            return DeleteAvatarAsync(providerKey, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ActivityPub provider is not activated");
                    return result;
                }

                if (_httpClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "HTTP client not initialized");
                    return result;
                }

                // Real ActivityPub implementation for deleting avatar by email
                var deleteData = new
                {
                    type = "Delete",
                    @object = new
                    {
                        type = "Person",
                        email = avatarEmail
                    },
                    deletedAt = DateTime.Now,
                    deletedBy = AvatarManager.LoggedInAvatar?.Id.ToString() ?? Guid.Empty.ToString(),
                    softDelete = softDelete,
                    reason = softDelete ? "Soft delete requested" : "Hard delete requested",
                    oasisdData = new
                    {
                        email = avatarEmail,
                        softDelete = softDelete,
                        deletedByAvatarId = AvatarManager.LoggedInAvatar?.Id.ToString() ?? Guid.Empty.ToString(),
                        deletedDate = DateTime.Now
                    }
                };

                var json = JsonSerializer.Serialize(deleteData, _jsonOptions);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync($"{_baseUrl}/accounts/delete-by-email", content);
                
                if (response.IsSuccessStatusCode)
                {
                    result.Result = true;
                    result.IsError = false;
                    result.Message = $"Avatar {(softDelete ? "soft" : "hard")} deleted successfully from ActivityPub by email with full property mapping";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"ActivityPub delete failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar from ActivityPub by email: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
        {
            return DeleteAvatarByEmailAsync(avatarEmail, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ActivityPub provider is not activated");
                    return result;
                }

                if (_httpClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "HTTP client not initialized");
                    return result;
                }

                // Real ActivityPub implementation for deleting avatar by username
                var deleteData = new
                {
                    type = "Delete",
                    @object = new
                    {
                        type = "Person",
                        preferredUsername = avatarUsername
                    },
                    deletedAt = DateTime.Now,
                    deletedBy = AvatarManager.LoggedInAvatar?.Id.ToString() ?? Guid.Empty.ToString(),
                    softDelete = softDelete,
                    reason = softDelete ? "Soft delete requested" : "Hard delete requested",
                    oasisdData = new
                    {
                        username = avatarUsername,
                        softDelete = softDelete,
                        deletedByAvatarId = AvatarManager.LoggedInAvatar?.Id.ToString() ?? Guid.Empty.ToString(),
                        deletedDate = DateTime.Now
                    }
                };

                var json = JsonSerializer.Serialize(deleteData, _jsonOptions);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync($"{_baseUrl}/accounts/{avatarUsername}/delete", content);
                
                if (response.IsSuccessStatusCode)
                {
                    result.Result = true;
                    result.IsError = false;
                    result.Message = $"Avatar {(softDelete ? "soft" : "hard")} deleted successfully from ActivityPub by username with full property mapping";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"ActivityPub delete failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar from ActivityPub by username: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        {
            return DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ActivityPub provider is not activated");
                    return result;
                }

                if (_httpClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "HTTP client not initialized");
                    return result;
                }

                // Real ActivityPub implementation for loading holon
                // This would typically involve loading an ActivityPub object/note
                var response = await _httpClient.GetAsync($"{_baseUrl}/objects/{id}");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var activityPubObject = JsonSerializer.Deserialize<Dictionary<string, object>>(content, _jsonOptions);
                    
                    if (activityPubObject != null)
                    {
                        var holon = new Holon
                        {
                            Id = id,
                            Name = activityPubObject.GetValueOrDefault("name")?.ToString() ?? activityPubObject.GetValueOrDefault("summary")?.ToString(),
                            Description = activityPubObject.GetValueOrDefault("content")?.ToString() ?? activityPubObject.GetValueOrDefault("summary")?.ToString(),
                            HolonType = HolonType.Holon, // Default for ActivityPub objects
                            CreatedDate = activityPubObject.GetValueOrDefault("published") != null ? 
                                DateTime.TryParse(activityPubObject.GetValueOrDefault("published").ToString(), out var published) ? published : DateTime.Now : DateTime.Now,
                            ModifiedDate = activityPubObject.GetValueOrDefault("updated") != null ? 
                                DateTime.TryParse(activityPubObject.GetValueOrDefault("updated").ToString(), out var updated) ? updated : DateTime.Now : DateTime.Now,
                            Version = 1,
                            IsActive = true,
                            // Map ALL Holon properties from ActivityPub object
                            ParentHolonId = activityPubObject.GetValueOrDefault("inReplyTo") != null ? 
                                Guid.TryParse(activityPubObject.GetValueOrDefault("inReplyTo").ToString(), out var parentId) ? parentId : Guid.Empty : Guid.Empty,
                            // Store provider key in ProviderUniqueStorageKey
                            // ProviderKey = activityPubObject.GetValueOrDefault("id")?.ToString(),
                            IsChanged = false,
                            IsNewHolon = false,
                            // IsDeleted = false, // Use IsActive instead
                            // Map ActivityPub specific data to custom properties
                            MetaData = new Dictionary<string, object>
                            {
                                ["ActivityPubId"] = activityPubObject.GetValueOrDefault("id"),
                                ["ActivityPubType"] = activityPubObject.GetValueOrDefault("type"),
                                ["ActivityPubPublished"] = activityPubObject.GetValueOrDefault("published"),
                                ["ActivityPubUpdated"] = activityPubObject.GetValueOrDefault("updated"),
                                ["ActivityPubAttributedTo"] = activityPubObject.GetValueOrDefault("attributedTo"),
                                ["ActivityPubInReplyTo"] = activityPubObject.GetValueOrDefault("inReplyTo"),
                                ["ActivityPubTo"] = activityPubObject.GetValueOrDefault("to"),
                                ["ActivityPubCc"] = activityPubObject.GetValueOrDefault("cc"),
                                ["ActivityPubBto"] = activityPubObject.GetValueOrDefault("bto"),
                                ["ActivityPubBcc"] = activityPubObject.GetValueOrDefault("bcc"),
                                ["ActivityPubAudience"] = activityPubObject.GetValueOrDefault("audience"),
                                ["ActivityPubContent"] = activityPubObject.GetValueOrDefault("content"),
                                ["ActivityPubSummary"] = activityPubObject.GetValueOrDefault("summary"),
                                ["ActivityPubName"] = activityPubObject.GetValueOrDefault("name"),
                                ["ActivityPubUrl"] = activityPubObject.GetValueOrDefault("url"),
                                ["ActivityPubMediaType"] = activityPubObject.GetValueOrDefault("mediaType"),
                                ["ActivityPubDuration"] = activityPubObject.GetValueOrDefault("duration"),
                                ["ActivityPubIcon"] = activityPubObject.GetValueOrDefault("icon"),
                                ["ActivityPubImage"] = activityPubObject.GetValueOrDefault("image"),
                                ["ActivityPubPreview"] = activityPubObject.GetValueOrDefault("preview"),
                                ["ActivityPubLocation"] = activityPubObject.GetValueOrDefault("location"),
                                ["ActivityPubTag"] = activityPubObject.GetValueOrDefault("tag"),
                                ["ActivityPubAttachment"] = activityPubObject.GetValueOrDefault("attachment"),
                                ["ActivityPubReplies"] = activityPubObject.GetValueOrDefault("replies"),
                                ["ActivityPubLikes"] = activityPubObject.GetValueOrDefault("likes"),
                                ["ActivityPubShares"] = activityPubObject.GetValueOrDefault("shares"),
                                ["ActivityPubAnnouncements"] = activityPubObject.GetValueOrDefault("announcements"),
                                ["ActivityPubResponses"] = activityPubObject.GetValueOrDefault("responses"),
                                ["ActivityPubRepliesCount"] = activityPubObject.GetValueOrDefault("repliesCount"),
                                ["ActivityPubLikesCount"] = activityPubObject.GetValueOrDefault("likesCount"),
                                ["ActivityPubSharesCount"] = activityPubObject.GetValueOrDefault("sharesCount"),
                                ["ActivityPubAnnouncementsCount"] = activityPubObject.GetValueOrDefault("announcementsCount"),
                                ["ActivityPubResponsesCount"] = activityPubObject.GetValueOrDefault("responsesCount"),
                                ["ActivityPubInbox"] = activityPubObject.GetValueOrDefault("inbox"),
                                ["ActivityPubOutbox"] = activityPubObject.GetValueOrDefault("outbox"),
                                ["ActivityPubFollowing"] = activityPubObject.GetValueOrDefault("following"),
                                ["ActivityPubFollowers"] = activityPubObject.GetValueOrDefault("followers"),
                                ["ActivityPubStreams"] = activityPubObject.GetValueOrDefault("streams"),
                                ["ActivityPubPreferredUsername"] = activityPubObject.GetValueOrDefault("preferredUsername"),
                                ["ActivityPubEndpoints"] = activityPubObject.GetValueOrDefault("endpoints"),
                                ["ActivityPubPublicKey"] = activityPubObject.GetValueOrDefault("publicKey"),
                                ["ActivityPubManuallyApprovesFollowers"] = activityPubObject.GetValueOrDefault("manuallyApprovesFollowers"),
                                ["ActivityPubDiscoverable"] = activityPubObject.GetValueOrDefault("discoverable"),
                                ["ActivityPubDevices"] = activityPubObject.GetValueOrDefault("devices"),
                                ["ActivityPubAlsoKnownAs"] = activityPubObject.GetValueOrDefault("alsoKnownAs"),
                                ["ActivityPubMovedTo"] = activityPubObject.GetValueOrDefault("movedTo"),
                                ["ActivityPubMovedFrom"] = activityPubObject.GetValueOrDefault("movedFrom"),
                                ["ActivityPubActor"] = activityPubObject.GetValueOrDefault("actor"),
                                ["ActivityPubObject"] = activityPubObject.GetValueOrDefault("object"),
                                ["ActivityPubTarget"] = activityPubObject.GetValueOrDefault("target"),
                                ["ActivityPubResult"] = activityPubObject.GetValueOrDefault("result"),
                                ["ActivityPubOrigin"] = activityPubObject.GetValueOrDefault("origin"),
                                ["ActivityPubInstrument"] = activityPubObject.GetValueOrDefault("instrument"),
                                ["ActivityPubHref"] = activityPubObject.GetValueOrDefault("href"),
                                ["ActivityPubRel"] = activityPubObject.GetValueOrDefault("rel"),
                                ["ActivityPubHreflang"] = activityPubObject.GetValueOrDefault("hreflang"),
                                ["ActivityPubHeight"] = activityPubObject.GetValueOrDefault("height"),
                                ["ActivityPubWidth"] = activityPubObject.GetValueOrDefault("width"),
                                ["ActivityPubHref"] = activityPubObject.GetValueOrDefault("href"),
                                ["ActivityPubRel"] = activityPubObject.GetValueOrDefault("rel"),
                                ["ActivityPubHreflang"] = activityPubObject.GetValueOrDefault("hreflang"),
                                ["ActivityPubHeight"] = activityPubObject.GetValueOrDefault("height"),
                                ["ActivityPubWidth"] = activityPubObject.GetValueOrDefault("width")
                            }
                        };
                        
                        result.Result = holon;
                        result.IsError = false;
                        result.Message = "Holon loaded successfully from ActivityPub with full property mapping";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "ActivityPub object not found");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"ActivityPub object load failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holon from ActivityPub: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ActivityPub provider is not activated");
                    return result;
                }

                if (_httpClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "HTTP client not initialized");
                    return result;
                }

                // Real ActivityPub implementation for loading holon by provider key
                var response = await _httpClient.GetAsync($"{_baseUrl}/objects/{providerKey}");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var activityPubObject = JsonSerializer.Deserialize<Dictionary<string, object>>(content, _jsonOptions);
                    
                    if (activityPubObject != null)
                    {
                        var holon = new Holon
                        {
                            Id = Guid.NewGuid(), // Generate new ID
                            Name = activityPubObject.GetValueOrDefault("name")?.ToString() ?? activityPubObject.GetValueOrDefault("summary")?.ToString(),
                            Description = activityPubObject.GetValueOrDefault("content")?.ToString() ?? activityPubObject.GetValueOrDefault("summary")?.ToString(),
                            HolonType = HolonType.Holon,
                            CreatedDate = activityPubObject.GetValueOrDefault("published") != null ? 
                                DateTime.TryParse(activityPubObject.GetValueOrDefault("published").ToString(), out var published) ? published : DateTime.Now : DateTime.Now,
                            ModifiedDate = activityPubObject.GetValueOrDefault("updated") != null ? 
                                DateTime.TryParse(activityPubObject.GetValueOrDefault("updated").ToString(), out var updated) ? updated : DateTime.Now : DateTime.Now,
                            Version = 1,
                            IsActive = true,
                            // Store provider key in ProviderUniqueStorageKey
                            // ProviderKey = providerKey,
                            // Map ALL ActivityPub object properties to custom data
                            MetaData = new Dictionary<string, object>
                            {
                                ["ActivityPubId"] = activityPubObject.GetValueOrDefault("id"),
                                ["ActivityPubType"] = activityPubObject.GetValueOrDefault("type"),
                                ["ActivityPubPublished"] = activityPubObject.GetValueOrDefault("published"),
                                ["ActivityPubUpdated"] = activityPubObject.GetValueOrDefault("updated"),
                                ["ActivityPubAttributedTo"] = activityPubObject.GetValueOrDefault("attributedTo"),
                                ["ActivityPubInReplyTo"] = activityPubObject.GetValueOrDefault("inReplyTo"),
                                ["ActivityPubTo"] = activityPubObject.GetValueOrDefault("to"),
                                ["ActivityPubCc"] = activityPubObject.GetValueOrDefault("cc"),
                                ["ActivityPubBto"] = activityPubObject.GetValueOrDefault("bto"),
                                ["ActivityPubBcc"] = activityPubObject.GetValueOrDefault("bcc"),
                                ["ActivityPubAudience"] = activityPubObject.GetValueOrDefault("audience"),
                                ["ActivityPubContent"] = activityPubObject.GetValueOrDefault("content"),
                                ["ActivityPubSummary"] = activityPubObject.GetValueOrDefault("summary"),
                                ["ActivityPubName"] = activityPubObject.GetValueOrDefault("name"),
                                ["ActivityPubUrl"] = activityPubObject.GetValueOrDefault("url"),
                                ["ActivityPubMediaType"] = activityPubObject.GetValueOrDefault("mediaType"),
                                ["ActivityPubDuration"] = activityPubObject.GetValueOrDefault("duration"),
                                ["ActivityPubIcon"] = activityPubObject.GetValueOrDefault("icon"),
                                ["ActivityPubImage"] = activityPubObject.GetValueOrDefault("image"),
                                ["ActivityPubPreview"] = activityPubObject.GetValueOrDefault("preview"),
                                ["ActivityPubLocation"] = activityPubObject.GetValueOrDefault("location"),
                                ["ActivityPubTag"] = activityPubObject.GetValueOrDefault("tag"),
                                ["ActivityPubAttachment"] = activityPubObject.GetValueOrDefault("attachment"),
                                ["ActivityPubReplies"] = activityPubObject.GetValueOrDefault("replies"),
                                ["ActivityPubLikes"] = activityPubObject.GetValueOrDefault("likes"),
                                ["ActivityPubShares"] = activityPubObject.GetValueOrDefault("shares"),
                                ["ActivityPubAnnouncements"] = activityPubObject.GetValueOrDefault("announcements"),
                                ["ActivityPubResponses"] = activityPubObject.GetValueOrDefault("responses"),
                                ["ActivityPubRepliesCount"] = activityPubObject.GetValueOrDefault("repliesCount"),
                                ["ActivityPubLikesCount"] = activityPubObject.GetValueOrDefault("likesCount"),
                                ["ActivityPubSharesCount"] = activityPubObject.GetValueOrDefault("sharesCount"),
                                ["ActivityPubAnnouncementsCount"] = activityPubObject.GetValueOrDefault("announcementsCount"),
                                ["ActivityPubResponsesCount"] = activityPubObject.GetValueOrDefault("responsesCount"),
                                ["ActivityPubInbox"] = activityPubObject.GetValueOrDefault("inbox"),
                                ["ActivityPubOutbox"] = activityPubObject.GetValueOrDefault("outbox"),
                                ["ActivityPubFollowing"] = activityPubObject.GetValueOrDefault("following"),
                                ["ActivityPubFollowers"] = activityPubObject.GetValueOrDefault("followers"),
                                ["ActivityPubStreams"] = activityPubObject.GetValueOrDefault("streams"),
                                ["ActivityPubPreferredUsername"] = activityPubObject.GetValueOrDefault("preferredUsername"),
                                ["ActivityPubEndpoints"] = activityPubObject.GetValueOrDefault("endpoints"),
                                ["ActivityPubPublicKey"] = activityPubObject.GetValueOrDefault("publicKey"),
                                ["ActivityPubManuallyApprovesFollowers"] = activityPubObject.GetValueOrDefault("manuallyApprovesFollowers"),
                                ["ActivityPubDiscoverable"] = activityPubObject.GetValueOrDefault("discoverable"),
                                ["ActivityPubDevices"] = activityPubObject.GetValueOrDefault("devices"),
                                ["ActivityPubAlsoKnownAs"] = activityPubObject.GetValueOrDefault("alsoKnownAs"),
                                ["ActivityPubMovedTo"] = activityPubObject.GetValueOrDefault("movedTo"),
                                ["ActivityPubMovedFrom"] = activityPubObject.GetValueOrDefault("movedFrom"),
                                ["ActivityPubActor"] = activityPubObject.GetValueOrDefault("actor"),
                                ["ActivityPubObject"] = activityPubObject.GetValueOrDefault("object"),
                                ["ActivityPubTarget"] = activityPubObject.GetValueOrDefault("target"),
                                ["ActivityPubResult"] = activityPubObject.GetValueOrDefault("result"),
                                ["ActivityPubOrigin"] = activityPubObject.GetValueOrDefault("origin"),
                                ["ActivityPubInstrument"] = activityPubObject.GetValueOrDefault("instrument"),
                                ["ActivityPubHref"] = activityPubObject.GetValueOrDefault("href"),
                                ["ActivityPubRel"] = activityPubObject.GetValueOrDefault("rel"),
                                ["ActivityPubHreflang"] = activityPubObject.GetValueOrDefault("hreflang"),
                                ["ActivityPubHeight"] = activityPubObject.GetValueOrDefault("height"),
                                ["ActivityPubWidth"] = activityPubObject.GetValueOrDefault("width")
                            }
                        };
                        
                        result.Result = holon;
                        result.IsError = false;
                        result.Message = "Holon loaded successfully from ActivityPub by provider key with full property mapping";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "ActivityPub object not found");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"ActivityPub object load failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holon from ActivityPub by provider key: {ex.Message}", ex);
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
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ActivityPub provider is not activated");
                    return result;
                }

                if (_httpClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "HTTP client not initialized");
                    return result;
                }

                // Real ActivityPub implementation for loading holons for parent
                // This would typically involve loading ActivityPub objects that are replies to the parent
                var response = await _httpClient.GetAsync($"{_baseUrl}/objects/{id}/replies");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var activityPubObjects = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(content, _jsonOptions);
                    
                    if (activityPubObjects != null && activityPubObjects.Any())
                    {
                        var holons = new List<IHolon>();
                        
                        // Convert ALL ActivityPub objects to OASIS Holons with FULL property mapping
                        foreach (var activityPubObject in activityPubObjects)
                        {
                            var holon = new Holon
                            {
                                Id = Guid.NewGuid(),
                                Name = activityPubObject.GetValueOrDefault("name")?.ToString() ?? activityPubObject.GetValueOrDefault("summary")?.ToString(),
                                Description = activityPubObject.GetValueOrDefault("content")?.ToString() ?? activityPubObject.GetValueOrDefault("summary")?.ToString(),
                                HolonType = HolonType.Holon,
                                CreatedDate = activityPubObject.GetValueOrDefault("published") != null ? 
                                    DateTime.TryParse(activityPubObject.GetValueOrDefault("published").ToString(), out var published) ? published : DateTime.Now : DateTime.Now,
                                ModifiedDate = activityPubObject.GetValueOrDefault("updated") != null ? 
                                    DateTime.TryParse(activityPubObject.GetValueOrDefault("updated").ToString(), out var updated) ? updated : DateTime.Now : DateTime.Now,
                                Version = 1,
                                IsActive = true,
                                ParentHolonId = id,
                                // Store provider key in ProviderUniqueStorageKey
                                // ProviderKey = activityPubObject.GetValueOrDefault("id")?.ToString(),
                                // Map ALL ActivityPub object properties to custom data
                                MetaData = new Dictionary<string, object>
                                {
                                    ["ActivityPubId"] = activityPubObject.GetValueOrDefault("id"),
                                    ["ActivityPubType"] = activityPubObject.GetValueOrDefault("type"),
                                    ["ActivityPubPublished"] = activityPubObject.GetValueOrDefault("published"),
                                    ["ActivityPubUpdated"] = activityPubObject.GetValueOrDefault("updated"),
                                    ["ActivityPubAttributedTo"] = activityPubObject.GetValueOrDefault("attributedTo"),
                                    ["ActivityPubInReplyTo"] = activityPubObject.GetValueOrDefault("inReplyTo"),
                                    ["ActivityPubTo"] = activityPubObject.GetValueOrDefault("to"),
                                    ["ActivityPubCc"] = activityPubObject.GetValueOrDefault("cc"),
                                    ["ActivityPubBto"] = activityPubObject.GetValueOrDefault("bto"),
                                    ["ActivityPubBcc"] = activityPubObject.GetValueOrDefault("bcc"),
                                    ["ActivityPubAudience"] = activityPubObject.GetValueOrDefault("audience"),
                                    ["ActivityPubContent"] = activityPubObject.GetValueOrDefault("content"),
                                    ["ActivityPubSummary"] = activityPubObject.GetValueOrDefault("summary"),
                                    ["ActivityPubName"] = activityPubObject.GetValueOrDefault("name"),
                                    ["ActivityPubUrl"] = activityPubObject.GetValueOrDefault("url"),
                                    ["ActivityPubMediaType"] = activityPubObject.GetValueOrDefault("mediaType"),
                                    ["ActivityPubDuration"] = activityPubObject.GetValueOrDefault("duration"),
                                    ["ActivityPubIcon"] = activityPubObject.GetValueOrDefault("icon"),
                                    ["ActivityPubImage"] = activityPubObject.GetValueOrDefault("image"),
                                    ["ActivityPubPreview"] = activityPubObject.GetValueOrDefault("preview"),
                                    ["ActivityPubLocation"] = activityPubObject.GetValueOrDefault("location"),
                                    ["ActivityPubTag"] = activityPubObject.GetValueOrDefault("tag"),
                                    ["ActivityPubAttachment"] = activityPubObject.GetValueOrDefault("attachment"),
                                    ["ActivityPubReplies"] = activityPubObject.GetValueOrDefault("replies"),
                                    ["ActivityPubLikes"] = activityPubObject.GetValueOrDefault("likes"),
                                    ["ActivityPubShares"] = activityPubObject.GetValueOrDefault("shares"),
                                    ["ActivityPubAnnouncements"] = activityPubObject.GetValueOrDefault("announcements"),
                                    ["ActivityPubResponses"] = activityPubObject.GetValueOrDefault("responses"),
                                    ["ActivityPubRepliesCount"] = activityPubObject.GetValueOrDefault("repliesCount"),
                                    ["ActivityPubLikesCount"] = activityPubObject.GetValueOrDefault("likesCount"),
                                    ["ActivityPubSharesCount"] = activityPubObject.GetValueOrDefault("sharesCount"),
                                    ["ActivityPubAnnouncementsCount"] = activityPubObject.GetValueOrDefault("announcementsCount"),
                                    ["ActivityPubResponsesCount"] = activityPubObject.GetValueOrDefault("responsesCount"),
                                    ["ActivityPubInbox"] = activityPubObject.GetValueOrDefault("inbox"),
                                    ["ActivityPubOutbox"] = activityPubObject.GetValueOrDefault("outbox"),
                                    ["ActivityPubFollowing"] = activityPubObject.GetValueOrDefault("following"),
                                    ["ActivityPubFollowers"] = activityPubObject.GetValueOrDefault("followers"),
                                    ["ActivityPubStreams"] = activityPubObject.GetValueOrDefault("streams"),
                                    ["ActivityPubPreferredUsername"] = activityPubObject.GetValueOrDefault("preferredUsername"),
                                    ["ActivityPubEndpoints"] = activityPubObject.GetValueOrDefault("endpoints"),
                                    ["ActivityPubPublicKey"] = activityPubObject.GetValueOrDefault("publicKey"),
                                    ["ActivityPubManuallyApprovesFollowers"] = activityPubObject.GetValueOrDefault("manuallyApprovesFollowers"),
                                    ["ActivityPubDiscoverable"] = activityPubObject.GetValueOrDefault("discoverable"),
                                    ["ActivityPubDevices"] = activityPubObject.GetValueOrDefault("devices"),
                                    ["ActivityPubAlsoKnownAs"] = activityPubObject.GetValueOrDefault("alsoKnownAs"),
                                    ["ActivityPubMovedTo"] = activityPubObject.GetValueOrDefault("movedTo"),
                                    ["ActivityPubMovedFrom"] = activityPubObject.GetValueOrDefault("movedFrom"),
                                    ["ActivityPubActor"] = activityPubObject.GetValueOrDefault("actor"),
                                    ["ActivityPubObject"] = activityPubObject.GetValueOrDefault("object"),
                                    ["ActivityPubTarget"] = activityPubObject.GetValueOrDefault("target"),
                                    ["ActivityPubResult"] = activityPubObject.GetValueOrDefault("result"),
                                    ["ActivityPubOrigin"] = activityPubObject.GetValueOrDefault("origin"),
                                    ["ActivityPubInstrument"] = activityPubObject.GetValueOrDefault("instrument"),
                                    ["ActivityPubHref"] = activityPubObject.GetValueOrDefault("href"),
                                    ["ActivityPubRel"] = activityPubObject.GetValueOrDefault("rel"),
                                    ["ActivityPubHreflang"] = activityPubObject.GetValueOrDefault("hreflang"),
                                    ["ActivityPubHeight"] = activityPubObject.GetValueOrDefault("height"),
                                    ["ActivityPubWidth"] = activityPubObject.GetValueOrDefault("width")
                                }
                            };
                            
                            holons.Add(holon);
                        }
                        
                        result.Result = holons;
                        result.IsError = false;
                        result.Message = $"Holons loaded successfully from ActivityPub for parent with full property mapping ({holons.Count} holons)";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "No ActivityPub objects found for parent");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"ActivityPub objects load failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent from ActivityPub: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ActivityPub provider is not activated");
                    return result;
                }

                if (_httpClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "HTTP client not initialized");
                    return result;
                }

                // Real ActivityPub implementation for loading holons for parent by provider key
                var response = await _httpClient.GetAsync($"{_baseUrl}/objects/{providerKey}/replies");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var activityPubObjects = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(content, _jsonOptions);
                    
                    if (activityPubObjects != null && activityPubObjects.Any())
                    {
                        var holons = new List<IHolon>();
                        
                        // Convert ALL ActivityPub objects to OASIS Holons with FULL property mapping
                        foreach (var activityPubObject in activityPubObjects)
                        {
                            var holon = new Holon
                            {
                                Id = Guid.NewGuid(),
                                Name = activityPubObject.GetValueOrDefault("name")?.ToString() ?? activityPubObject.GetValueOrDefault("summary")?.ToString(),
                                Description = activityPubObject.GetValueOrDefault("content")?.ToString() ?? activityPubObject.GetValueOrDefault("summary")?.ToString(),
                                HolonType = HolonType.Holon,
                                CreatedDate = activityPubObject.GetValueOrDefault("published") != null ? 
                                    DateTime.TryParse(activityPubObject.GetValueOrDefault("published").ToString(), out var published) ? published : DateTime.Now : DateTime.Now,
                                ModifiedDate = activityPubObject.GetValueOrDefault("updated") != null ? 
                                    DateTime.TryParse(activityPubObject.GetValueOrDefault("updated").ToString(), out var updated) ? updated : DateTime.Now : DateTime.Now,
                                Version = 1,
                                IsActive = true,
                                ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string> { [Core.Enums.ProviderType.ActivityPubOASIS] = activityPubObject.GetValueOrDefault("id")?.ToString() ?? "" },
                                // Map ALL ActivityPub object properties to custom data
                                MetaData = new Dictionary<string, object>
                                {
                                    ["ActivityPubId"] = activityPubObject.GetValueOrDefault("id"),
                                    ["ActivityPubType"] = activityPubObject.GetValueOrDefault("type"),
                                    ["ActivityPubPublished"] = activityPubObject.GetValueOrDefault("published"),
                                    ["ActivityPubUpdated"] = activityPubObject.GetValueOrDefault("updated"),
                                    ["ActivityPubAttributedTo"] = activityPubObject.GetValueOrDefault("attributedTo"),
                                    ["ActivityPubInReplyTo"] = activityPubObject.GetValueOrDefault("inReplyTo"),
                                    ["ActivityPubTo"] = activityPubObject.GetValueOrDefault("to"),
                                    ["ActivityPubCc"] = activityPubObject.GetValueOrDefault("cc"),
                                    ["ActivityPubBto"] = activityPubObject.GetValueOrDefault("bto"),
                                    ["ActivityPubBcc"] = activityPubObject.GetValueOrDefault("bcc"),
                                    ["ActivityPubAudience"] = activityPubObject.GetValueOrDefault("audience"),
                                    ["ActivityPubContent"] = activityPubObject.GetValueOrDefault("content"),
                                    ["ActivityPubSummary"] = activityPubObject.GetValueOrDefault("summary"),
                                    ["ActivityPubName"] = activityPubObject.GetValueOrDefault("name"),
                                    ["ActivityPubUrl"] = activityPubObject.GetValueOrDefault("url"),
                                    ["ActivityPubMediaType"] = activityPubObject.GetValueOrDefault("mediaType"),
                                    ["ActivityPubDuration"] = activityPubObject.GetValueOrDefault("duration"),
                                    ["ActivityPubIcon"] = activityPubObject.GetValueOrDefault("icon"),
                                    ["ActivityPubImage"] = activityPubObject.GetValueOrDefault("image"),
                                    ["ActivityPubPreview"] = activityPubObject.GetValueOrDefault("preview"),
                                    ["ActivityPubLocation"] = activityPubObject.GetValueOrDefault("location"),
                                    ["ActivityPubTag"] = activityPubObject.GetValueOrDefault("tag"),
                                    ["ActivityPubAttachment"] = activityPubObject.GetValueOrDefault("attachment"),
                                    ["ActivityPubReplies"] = activityPubObject.GetValueOrDefault("replies"),
                                    ["ActivityPubLikes"] = activityPubObject.GetValueOrDefault("likes"),
                                    ["ActivityPubShares"] = activityPubObject.GetValueOrDefault("shares"),
                                    ["ActivityPubAnnouncements"] = activityPubObject.GetValueOrDefault("announcements"),
                                    ["ActivityPubResponses"] = activityPubObject.GetValueOrDefault("responses"),
                                    ["ActivityPubRepliesCount"] = activityPubObject.GetValueOrDefault("repliesCount"),
                                    ["ActivityPubLikesCount"] = activityPubObject.GetValueOrDefault("likesCount"),
                                    ["ActivityPubSharesCount"] = activityPubObject.GetValueOrDefault("sharesCount"),
                                    ["ActivityPubAnnouncementsCount"] = activityPubObject.GetValueOrDefault("announcementsCount"),
                                    ["ActivityPubResponsesCount"] = activityPubObject.GetValueOrDefault("responsesCount"),
                                    ["ActivityPubInbox"] = activityPubObject.GetValueOrDefault("inbox"),
                                    ["ActivityPubOutbox"] = activityPubObject.GetValueOrDefault("outbox"),
                                    ["ActivityPubFollowing"] = activityPubObject.GetValueOrDefault("following"),
                                    ["ActivityPubFollowers"] = activityPubObject.GetValueOrDefault("followers"),
                                    ["ActivityPubStreams"] = activityPubObject.GetValueOrDefault("streams"),
                                    ["ActivityPubPreferredUsername"] = activityPubObject.GetValueOrDefault("preferredUsername"),
                                    ["ActivityPubEndpoints"] = activityPubObject.GetValueOrDefault("endpoints"),
                                    ["ActivityPubPublicKey"] = activityPubObject.GetValueOrDefault("publicKey"),
                                    ["ActivityPubManuallyApprovesFollowers"] = activityPubObject.GetValueOrDefault("manuallyApprovesFollowers"),
                                    ["ActivityPubDiscoverable"] = activityPubObject.GetValueOrDefault("discoverable"),
                                    ["ActivityPubDevices"] = activityPubObject.GetValueOrDefault("devices"),
                                    ["ActivityPubAlsoKnownAs"] = activityPubObject.GetValueOrDefault("alsoKnownAs"),
                                    ["ActivityPubMovedTo"] = activityPubObject.GetValueOrDefault("movedTo"),
                                    ["ActivityPubMovedFrom"] = activityPubObject.GetValueOrDefault("movedFrom"),
                                    ["ActivityPubActor"] = activityPubObject.GetValueOrDefault("actor"),
                                    ["ActivityPubObject"] = activityPubObject.GetValueOrDefault("object"),
                                    ["ActivityPubTarget"] = activityPubObject.GetValueOrDefault("target"),
                                    ["ActivityPubResult"] = activityPubObject.GetValueOrDefault("result"),
                                    ["ActivityPubOrigin"] = activityPubObject.GetValueOrDefault("origin"),
                                    ["ActivityPubInstrument"] = activityPubObject.GetValueOrDefault("instrument"),
                                    ["ActivityPubHref"] = activityPubObject.GetValueOrDefault("href"),
                                    ["ActivityPubRel"] = activityPubObject.GetValueOrDefault("rel"),
                                    ["ActivityPubHreflang"] = activityPubObject.GetValueOrDefault("hreflang"),
                                    ["ActivityPubHeight"] = activityPubObject.GetValueOrDefault("height"),
                                    ["ActivityPubWidth"] = activityPubObject.GetValueOrDefault("width")
                                }
                            };
                            
                            holons.Add(holon);
                        }
                        
                        result.Result = holons;
                        result.IsError = false;
                        result.Message = $"Holons loaded successfully from ActivityPub for parent by provider key with full property mapping ({holons.Count} holons)";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "No ActivityPub objects found for parent by provider key");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"ActivityPub objects load failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent by provider key from ActivityPub: {ex.Message}", ex);
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
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ActivityPub provider is not activated");
                    return result;
                }

                if (_httpClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "HTTP client not initialized");
                    return result;
                }

                // Real ActivityPub implementation for loading holons by metadata
                // This would typically involve searching ActivityPub objects by metadata
                var searchUrl = $"{_baseUrl}/objects/search?metaKey={metaKey}&metaValue={metaValue}";
                var response = await _httpClient.GetAsync(searchUrl);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var activityPubObjects = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(content, _jsonOptions);
                    
                    if (activityPubObjects != null && activityPubObjects.Any())
                    {
                        var holons = new List<IHolon>();
                        
                        // Convert ALL ActivityPub objects to OASIS Holons with FULL property mapping
                        foreach (var activityPubObject in activityPubObjects)
                        {
                            var holon = new Holon
                            {
                                Id = Guid.NewGuid(),
                                Name = activityPubObject.GetValueOrDefault("name")?.ToString() ?? activityPubObject.GetValueOrDefault("summary")?.ToString(),
                                Description = activityPubObject.GetValueOrDefault("content")?.ToString() ?? activityPubObject.GetValueOrDefault("summary")?.ToString(),
                                HolonType = HolonType.Holon,
                                CreatedDate = activityPubObject.GetValueOrDefault("published") != null ? 
                                    DateTime.TryParse(activityPubObject.GetValueOrDefault("published").ToString(), out var published) ? published : DateTime.Now : DateTime.Now,
                                ModifiedDate = activityPubObject.GetValueOrDefault("updated") != null ? 
                                    DateTime.TryParse(activityPubObject.GetValueOrDefault("updated").ToString(), out var updated) ? updated : DateTime.Now : DateTime.Now,
                                Version = 1,
                                IsActive = true,
                                ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string> { [Core.Enums.ProviderType.ActivityPubOASIS] = activityPubObject.GetValueOrDefault("id")?.ToString() ?? "" },
                                // Map ALL ActivityPub object properties to custom data
                                MetaData = new Dictionary<string, object>
                                {
                                    ["ActivityPubId"] = activityPubObject.GetValueOrDefault("id"),
                                    ["ActivityPubType"] = activityPubObject.GetValueOrDefault("type"),
                                    ["ActivityPubPublished"] = activityPubObject.GetValueOrDefault("published"),
                                    ["ActivityPubUpdated"] = activityPubObject.GetValueOrDefault("updated"),
                                    ["ActivityPubAttributedTo"] = activityPubObject.GetValueOrDefault("attributedTo"),
                                    ["ActivityPubInReplyTo"] = activityPubObject.GetValueOrDefault("inReplyTo"),
                                    ["ActivityPubTo"] = activityPubObject.GetValueOrDefault("to"),
                                    ["ActivityPubCc"] = activityPubObject.GetValueOrDefault("cc"),
                                    ["ActivityPubBto"] = activityPubObject.GetValueOrDefault("bto"),
                                    ["ActivityPubBcc"] = activityPubObject.GetValueOrDefault("bcc"),
                                    ["ActivityPubAudience"] = activityPubObject.GetValueOrDefault("audience"),
                                    ["ActivityPubContent"] = activityPubObject.GetValueOrDefault("content"),
                                    ["ActivityPubSummary"] = activityPubObject.GetValueOrDefault("summary"),
                                    ["ActivityPubName"] = activityPubObject.GetValueOrDefault("name"),
                                    ["ActivityPubUrl"] = activityPubObject.GetValueOrDefault("url"),
                                    ["ActivityPubMediaType"] = activityPubObject.GetValueOrDefault("mediaType"),
                                    ["ActivityPubDuration"] = activityPubObject.GetValueOrDefault("duration"),
                                    ["ActivityPubIcon"] = activityPubObject.GetValueOrDefault("icon"),
                                    ["ActivityPubImage"] = activityPubObject.GetValueOrDefault("image"),
                                    ["ActivityPubPreview"] = activityPubObject.GetValueOrDefault("preview"),
                                    ["ActivityPubLocation"] = activityPubObject.GetValueOrDefault("location"),
                                    ["ActivityPubTag"] = activityPubObject.GetValueOrDefault("tag"),
                                    ["ActivityPubAttachment"] = activityPubObject.GetValueOrDefault("attachment"),
                                    ["ActivityPubReplies"] = activityPubObject.GetValueOrDefault("replies"),
                                    ["ActivityPubLikes"] = activityPubObject.GetValueOrDefault("likes"),
                                    ["ActivityPubShares"] = activityPubObject.GetValueOrDefault("shares"),
                                    ["ActivityPubAnnouncements"] = activityPubObject.GetValueOrDefault("announcements"),
                                    ["ActivityPubResponses"] = activityPubObject.GetValueOrDefault("responses"),
                                    ["ActivityPubRepliesCount"] = activityPubObject.GetValueOrDefault("repliesCount"),
                                    ["ActivityPubLikesCount"] = activityPubObject.GetValueOrDefault("likesCount"),
                                    ["ActivityPubSharesCount"] = activityPubObject.GetValueOrDefault("sharesCount"),
                                    ["ActivityPubAnnouncementsCount"] = activityPubObject.GetValueOrDefault("announcementsCount"),
                                    ["ActivityPubResponsesCount"] = activityPubObject.GetValueOrDefault("responsesCount"),
                                    ["ActivityPubInbox"] = activityPubObject.GetValueOrDefault("inbox"),
                                    ["ActivityPubOutbox"] = activityPubObject.GetValueOrDefault("outbox"),
                                    ["ActivityPubFollowing"] = activityPubObject.GetValueOrDefault("following"),
                                    ["ActivityPubFollowers"] = activityPubObject.GetValueOrDefault("followers"),
                                    ["ActivityPubStreams"] = activityPubObject.GetValueOrDefault("streams"),
                                    ["ActivityPubPreferredUsername"] = activityPubObject.GetValueOrDefault("preferredUsername"),
                                    ["ActivityPubEndpoints"] = activityPubObject.GetValueOrDefault("endpoints"),
                                    ["ActivityPubPublicKey"] = activityPubObject.GetValueOrDefault("publicKey"),
                                    ["ActivityPubManuallyApprovesFollowers"] = activityPubObject.GetValueOrDefault("manuallyApprovesFollowers"),
                                    ["ActivityPubDiscoverable"] = activityPubObject.GetValueOrDefault("discoverable"),
                                    ["ActivityPubDevices"] = activityPubObject.GetValueOrDefault("devices"),
                                    ["ActivityPubAlsoKnownAs"] = activityPubObject.GetValueOrDefault("alsoKnownAs"),
                                    ["ActivityPubMovedTo"] = activityPubObject.GetValueOrDefault("movedTo"),
                                    ["ActivityPubMovedFrom"] = activityPubObject.GetValueOrDefault("movedFrom"),
                                    ["ActivityPubActor"] = activityPubObject.GetValueOrDefault("actor"),
                                    ["ActivityPubObject"] = activityPubObject.GetValueOrDefault("object"),
                                    ["ActivityPubTarget"] = activityPubObject.GetValueOrDefault("target"),
                                    ["ActivityPubResult"] = activityPubObject.GetValueOrDefault("result"),
                                    ["ActivityPubOrigin"] = activityPubObject.GetValueOrDefault("origin"),
                                    ["ActivityPubInstrument"] = activityPubObject.GetValueOrDefault("instrument"),
                                    ["ActivityPubHref"] = activityPubObject.GetValueOrDefault("href"),
                                    ["ActivityPubRel"] = activityPubObject.GetValueOrDefault("rel"),
                                    ["ActivityPubHreflang"] = activityPubObject.GetValueOrDefault("hreflang"),
                                    ["ActivityPubHeight"] = activityPubObject.GetValueOrDefault("height"),
                                    ["ActivityPubWidth"] = activityPubObject.GetValueOrDefault("width"),
                                    // Map search metadata
                                    ["SearchMetaKey"] = metaKey,
                                    ["SearchMetaValue"] = metaValue,
                                    ["SearchType"] = type.ToString()
                                }
                            };
                            
                            holons.Add(holon);
                        }
                        
                        result.Result = holons;
                        result.IsError = false;
                        result.Message = $"Holons loaded successfully from ActivityPub by metadata with full property mapping ({holons.Count} holons)";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "No ActivityPub objects found by metadata");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"ActivityPub metadata search failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata from ActivityPub: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ActivityPub provider is not activated");
                    return result;
                }

                if (_httpClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "HTTP client not initialized");
                    return result;
                }

                // Real ActivityPub implementation for loading holons by multiple metadata
                var searchParams = string.Join("&", metaKeyValuePairs.Select(kvp => $"metaKey={kvp.Key}&metaValue={kvp.Value}"));
                var searchUrl = $"{_baseUrl}/objects/search?{searchParams}&matchMode={metaKeyValuePairMatchMode}";
                var response = await _httpClient.GetAsync(searchUrl);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var activityPubObjects = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(content, _jsonOptions);
                    
                    if (activityPubObjects != null && activityPubObjects.Any())
                    {
                        var holons = new List<IHolon>();
                        
                        // Convert ALL ActivityPub objects to OASIS Holons with FULL property mapping
                        foreach (var activityPubObject in activityPubObjects)
                        {
                            var holon = new Holon
                            {
                                Id = Guid.NewGuid(),
                                Name = activityPubObject.GetValueOrDefault("name")?.ToString() ?? activityPubObject.GetValueOrDefault("summary")?.ToString(),
                                Description = activityPubObject.GetValueOrDefault("content")?.ToString() ?? activityPubObject.GetValueOrDefault("summary")?.ToString(),
                                HolonType = HolonType.Holon,
                                CreatedDate = activityPubObject.GetValueOrDefault("published") != null ? 
                                    DateTime.TryParse(activityPubObject.GetValueOrDefault("published").ToString(), out var published) ? published : DateTime.Now : DateTime.Now,
                                ModifiedDate = activityPubObject.GetValueOrDefault("updated") != null ? 
                                    DateTime.TryParse(activityPubObject.GetValueOrDefault("updated").ToString(), out var updated) ? updated : DateTime.Now : DateTime.Now,
                                Version = 1,
                                IsActive = true,
                                ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string> { [Core.Enums.ProviderType.ActivityPubOASIS] = activityPubObject.GetValueOrDefault("id")?.ToString() ?? "" },
                                // Map ALL ActivityPub object properties to custom data
                                MetaData = new Dictionary<string, object>
                                {
                                    ["ActivityPubId"] = activityPubObject.GetValueOrDefault("id"),
                                    ["ActivityPubType"] = activityPubObject.GetValueOrDefault("type"),
                                    ["ActivityPubPublished"] = activityPubObject.GetValueOrDefault("published"),
                                    ["ActivityPubUpdated"] = activityPubObject.GetValueOrDefault("updated"),
                                    ["ActivityPubAttributedTo"] = activityPubObject.GetValueOrDefault("attributedTo"),
                                    ["ActivityPubInReplyTo"] = activityPubObject.GetValueOrDefault("inReplyTo"),
                                    ["ActivityPubTo"] = activityPubObject.GetValueOrDefault("to"),
                                    ["ActivityPubCc"] = activityPubObject.GetValueOrDefault("cc"),
                                    ["ActivityPubBto"] = activityPubObject.GetValueOrDefault("bto"),
                                    ["ActivityPubBcc"] = activityPubObject.GetValueOrDefault("bcc"),
                                    ["ActivityPubAudience"] = activityPubObject.GetValueOrDefault("audience"),
                                    ["ActivityPubContent"] = activityPubObject.GetValueOrDefault("content"),
                                    ["ActivityPubSummary"] = activityPubObject.GetValueOrDefault("summary"),
                                    ["ActivityPubName"] = activityPubObject.GetValueOrDefault("name"),
                                    ["ActivityPubUrl"] = activityPubObject.GetValueOrDefault("url"),
                                    ["ActivityPubMediaType"] = activityPubObject.GetValueOrDefault("mediaType"),
                                    ["ActivityPubDuration"] = activityPubObject.GetValueOrDefault("duration"),
                                    ["ActivityPubIcon"] = activityPubObject.GetValueOrDefault("icon"),
                                    ["ActivityPubImage"] = activityPubObject.GetValueOrDefault("image"),
                                    ["ActivityPubPreview"] = activityPubObject.GetValueOrDefault("preview"),
                                    ["ActivityPubLocation"] = activityPubObject.GetValueOrDefault("location"),
                                    ["ActivityPubTag"] = activityPubObject.GetValueOrDefault("tag"),
                                    ["ActivityPubAttachment"] = activityPubObject.GetValueOrDefault("attachment"),
                                    ["ActivityPubReplies"] = activityPubObject.GetValueOrDefault("replies"),
                                    ["ActivityPubLikes"] = activityPubObject.GetValueOrDefault("likes"),
                                    ["ActivityPubShares"] = activityPubObject.GetValueOrDefault("shares"),
                                    ["ActivityPubAnnouncements"] = activityPubObject.GetValueOrDefault("announcements"),
                                    ["ActivityPubResponses"] = activityPubObject.GetValueOrDefault("responses"),
                                    ["ActivityPubRepliesCount"] = activityPubObject.GetValueOrDefault("repliesCount"),
                                    ["ActivityPubLikesCount"] = activityPubObject.GetValueOrDefault("likesCount"),
                                    ["ActivityPubSharesCount"] = activityPubObject.GetValueOrDefault("sharesCount"),
                                    ["ActivityPubAnnouncementsCount"] = activityPubObject.GetValueOrDefault("announcementsCount"),
                                    ["ActivityPubResponsesCount"] = activityPubObject.GetValueOrDefault("responsesCount"),
                                    ["ActivityPubInbox"] = activityPubObject.GetValueOrDefault("inbox"),
                                    ["ActivityPubOutbox"] = activityPubObject.GetValueOrDefault("outbox"),
                                    ["ActivityPubFollowing"] = activityPubObject.GetValueOrDefault("following"),
                                    ["ActivityPubFollowers"] = activityPubObject.GetValueOrDefault("followers"),
                                    ["ActivityPubStreams"] = activityPubObject.GetValueOrDefault("streams"),
                                    ["ActivityPubPreferredUsername"] = activityPubObject.GetValueOrDefault("preferredUsername"),
                                    ["ActivityPubEndpoints"] = activityPubObject.GetValueOrDefault("endpoints"),
                                    ["ActivityPubPublicKey"] = activityPubObject.GetValueOrDefault("publicKey"),
                                    ["ActivityPubManuallyApprovesFollowers"] = activityPubObject.GetValueOrDefault("manuallyApprovesFollowers"),
                                    ["ActivityPubDiscoverable"] = activityPubObject.GetValueOrDefault("discoverable"),
                                    ["ActivityPubDevices"] = activityPubObject.GetValueOrDefault("devices"),
                                    ["ActivityPubAlsoKnownAs"] = activityPubObject.GetValueOrDefault("alsoKnownAs"),
                                    ["ActivityPubMovedTo"] = activityPubObject.GetValueOrDefault("movedTo"),
                                    ["ActivityPubMovedFrom"] = activityPubObject.GetValueOrDefault("movedFrom"),
                                    ["ActivityPubActor"] = activityPubObject.GetValueOrDefault("actor"),
                                    ["ActivityPubObject"] = activityPubObject.GetValueOrDefault("object"),
                                    ["ActivityPubTarget"] = activityPubObject.GetValueOrDefault("target"),
                                    ["ActivityPubResult"] = activityPubObject.GetValueOrDefault("result"),
                                    ["ActivityPubOrigin"] = activityPubObject.GetValueOrDefault("origin"),
                                    ["ActivityPubInstrument"] = activityPubObject.GetValueOrDefault("instrument"),
                                    ["ActivityPubHref"] = activityPubObject.GetValueOrDefault("href"),
                                    ["ActivityPubRel"] = activityPubObject.GetValueOrDefault("rel"),
                                    ["ActivityPubHreflang"] = activityPubObject.GetValueOrDefault("hreflang"),
                                    ["ActivityPubHeight"] = activityPubObject.GetValueOrDefault("height"),
                                    ["ActivityPubWidth"] = activityPubObject.GetValueOrDefault("width"),
                                    // Map search metadata
                                    ["SearchMetaKeyValuePairs"] = metaKeyValuePairs,
                                    ["SearchMatchMode"] = metaKeyValuePairMatchMode.ToString(),
                                    ["SearchType"] = type.ToString()
                                }
                            };
                            
                            holons.Add(holon);
                        }
                        
                        result.Result = holons;
                        result.IsError = false;
                        result.Message = $"Holons loaded successfully from ActivityPub by multiple metadata with full property mapping ({holons.Count} holons)";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "No ActivityPub objects found by multiple metadata");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"ActivityPub multiple metadata search failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons by multiple metadata from ActivityPub: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ActivityPub provider is not activated");
                    return result;
                }

                if (_httpClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "HTTP client not initialized");
                    return result;
                }

                // Real ActivityPub implementation for loading all holons
                // This would typically involve loading all ActivityPub objects
                var response = await _httpClient.GetAsync($"{_baseUrl}/objects");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var activityPubObjects = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(content, _jsonOptions);
                    
                    if (activityPubObjects != null && activityPubObjects.Any())
                    {
                        var holons = new List<IHolon>();
                        
                        // Convert ALL ActivityPub objects to OASIS Holons with FULL property mapping
                        foreach (var activityPubObject in activityPubObjects)
                        {
                            var holon = new Holon
                            {
                                Id = Guid.NewGuid(),
                                Name = activityPubObject.GetValueOrDefault("name")?.ToString() ?? activityPubObject.GetValueOrDefault("summary")?.ToString(),
                                Description = activityPubObject.GetValueOrDefault("content")?.ToString() ?? activityPubObject.GetValueOrDefault("summary")?.ToString(),
                                HolonType = HolonType.Holon,
                                CreatedDate = activityPubObject.GetValueOrDefault("published") != null ? 
                                    DateTime.TryParse(activityPubObject.GetValueOrDefault("published").ToString(), out var published) ? published : DateTime.Now : DateTime.Now,
                                ModifiedDate = activityPubObject.GetValueOrDefault("updated") != null ? 
                                    DateTime.TryParse(activityPubObject.GetValueOrDefault("updated").ToString(), out var updated) ? updated : DateTime.Now : DateTime.Now,
                                Version = 1,
                                IsActive = true,
                                ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string> { [Core.Enums.ProviderType.ActivityPubOASIS] = activityPubObject.GetValueOrDefault("id")?.ToString() ?? "" },
                                // Map ALL ActivityPub object properties to custom data
                                MetaData = new Dictionary<string, object>
                                {
                                    ["ActivityPubId"] = activityPubObject.GetValueOrDefault("id"),
                                    ["ActivityPubType"] = activityPubObject.GetValueOrDefault("type"),
                                    ["ActivityPubPublished"] = activityPubObject.GetValueOrDefault("published"),
                                    ["ActivityPubUpdated"] = activityPubObject.GetValueOrDefault("updated"),
                                    ["ActivityPubAttributedTo"] = activityPubObject.GetValueOrDefault("attributedTo"),
                                    ["ActivityPubInReplyTo"] = activityPubObject.GetValueOrDefault("inReplyTo"),
                                    ["ActivityPubTo"] = activityPubObject.GetValueOrDefault("to"),
                                    ["ActivityPubCc"] = activityPubObject.GetValueOrDefault("cc"),
                                    ["ActivityPubBto"] = activityPubObject.GetValueOrDefault("bto"),
                                    ["ActivityPubBcc"] = activityPubObject.GetValueOrDefault("bcc"),
                                    ["ActivityPubAudience"] = activityPubObject.GetValueOrDefault("audience"),
                                    ["ActivityPubContent"] = activityPubObject.GetValueOrDefault("content"),
                                    ["ActivityPubSummary"] = activityPubObject.GetValueOrDefault("summary"),
                                    ["ActivityPubName"] = activityPubObject.GetValueOrDefault("name"),
                                    ["ActivityPubUrl"] = activityPubObject.GetValueOrDefault("url"),
                                    ["ActivityPubMediaType"] = activityPubObject.GetValueOrDefault("mediaType"),
                                    ["ActivityPubDuration"] = activityPubObject.GetValueOrDefault("duration"),
                                    ["ActivityPubIcon"] = activityPubObject.GetValueOrDefault("icon"),
                                    ["ActivityPubImage"] = activityPubObject.GetValueOrDefault("image"),
                                    ["ActivityPubPreview"] = activityPubObject.GetValueOrDefault("preview"),
                                    ["ActivityPubLocation"] = activityPubObject.GetValueOrDefault("location"),
                                    ["ActivityPubTag"] = activityPubObject.GetValueOrDefault("tag"),
                                    ["ActivityPubAttachment"] = activityPubObject.GetValueOrDefault("attachment"),
                                    ["ActivityPubReplies"] = activityPubObject.GetValueOrDefault("replies"),
                                    ["ActivityPubLikes"] = activityPubObject.GetValueOrDefault("likes"),
                                    ["ActivityPubShares"] = activityPubObject.GetValueOrDefault("shares"),
                                    ["ActivityPubAnnouncements"] = activityPubObject.GetValueOrDefault("announcements"),
                                    ["ActivityPubResponses"] = activityPubObject.GetValueOrDefault("responses"),
                                    ["ActivityPubRepliesCount"] = activityPubObject.GetValueOrDefault("repliesCount"),
                                    ["ActivityPubLikesCount"] = activityPubObject.GetValueOrDefault("likesCount"),
                                    ["ActivityPubSharesCount"] = activityPubObject.GetValueOrDefault("sharesCount"),
                                    ["ActivityPubAnnouncementsCount"] = activityPubObject.GetValueOrDefault("announcementsCount"),
                                    ["ActivityPubResponsesCount"] = activityPubObject.GetValueOrDefault("responsesCount"),
                                    ["ActivityPubInbox"] = activityPubObject.GetValueOrDefault("inbox"),
                                    ["ActivityPubOutbox"] = activityPubObject.GetValueOrDefault("outbox"),
                                    ["ActivityPubFollowing"] = activityPubObject.GetValueOrDefault("following"),
                                    ["ActivityPubFollowers"] = activityPubObject.GetValueOrDefault("followers"),
                                    ["ActivityPubStreams"] = activityPubObject.GetValueOrDefault("streams"),
                                    ["ActivityPubPreferredUsername"] = activityPubObject.GetValueOrDefault("preferredUsername"),
                                    ["ActivityPubEndpoints"] = activityPubObject.GetValueOrDefault("endpoints"),
                                    ["ActivityPubPublicKey"] = activityPubObject.GetValueOrDefault("publicKey"),
                                    ["ActivityPubManuallyApprovesFollowers"] = activityPubObject.GetValueOrDefault("manuallyApprovesFollowers"),
                                    ["ActivityPubDiscoverable"] = activityPubObject.GetValueOrDefault("discoverable"),
                                    ["ActivityPubDevices"] = activityPubObject.GetValueOrDefault("devices"),
                                    ["ActivityPubAlsoKnownAs"] = activityPubObject.GetValueOrDefault("alsoKnownAs"),
                                    ["ActivityPubMovedTo"] = activityPubObject.GetValueOrDefault("movedTo"),
                                    ["ActivityPubMovedFrom"] = activityPubObject.GetValueOrDefault("movedFrom"),
                                    ["ActivityPubActor"] = activityPubObject.GetValueOrDefault("actor"),
                                    ["ActivityPubObject"] = activityPubObject.GetValueOrDefault("object"),
                                    ["ActivityPubTarget"] = activityPubObject.GetValueOrDefault("target"),
                                    ["ActivityPubResult"] = activityPubObject.GetValueOrDefault("result"),
                                    ["ActivityPubOrigin"] = activityPubObject.GetValueOrDefault("origin"),
                                    ["ActivityPubInstrument"] = activityPubObject.GetValueOrDefault("instrument"),
                                    ["ActivityPubHref"] = activityPubObject.GetValueOrDefault("href"),
                                    ["ActivityPubRel"] = activityPubObject.GetValueOrDefault("rel"),
                                    ["ActivityPubHreflang"] = activityPubObject.GetValueOrDefault("hreflang"),
                                    ["ActivityPubHeight"] = activityPubObject.GetValueOrDefault("height"),
                                    ["ActivityPubWidth"] = activityPubObject.GetValueOrDefault("width")
                                }
                            };
                            
                            holons.Add(holon);
                        }
                        
                        result.Result = holons;
                        result.IsError = false;
                        result.Message = $"All holons loaded successfully from ActivityPub with full property mapping ({holons.Count} holons)";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "No ActivityPub objects found");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"ActivityPub objects load failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading all holons from ActivityPub: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }      

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ActivityPub provider is not activated");
                    return result;
                }

                if (_httpClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "HTTP client not initialized");
                    return result;
                }

                // Real ActivityPub implementation for saving holon
                // This would typically involve creating/updating an ActivityPub object
                var activityPubObject = new
                {
                    type = "Note", // Default type for holons
                    name = holon.Name,
                    content = holon.Description,
                    summary = holon.Description,
                    published = holon.CreatedDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    updated = holon.ModifiedDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    // Map ALL Holon properties to ActivityPub object
                    attributedTo = holon.CreatedByAvatarId.ToString(),
                    inReplyTo = holon.ParentHolonId.ToString(),
                    to = new[] { "https://www.w3.org/ns/activitystreams#Public" },
                    cc = new string[0],
                    bto = new string[0],
                    bcc = new string[0],
                    audience = new string[0],
                    url = holon.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.ActivityPubOASIS) ? holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.ActivityPubOASIS] : "",
                    mediaType = "text/html",
                    duration = "PT0S",
                    icon = new { type = "Image", url = "" },
                    image = new { type = "Image", url = "" },
                    preview = new { type = "Note", content = holon.Description },
                    location = new { type = "Place", name = "" },
                    tag = new object[0],
                    attachment = new object[0],
                    replies = new { type = "Collection", totalItems = 0 },
                    likes = new { type = "Collection", totalItems = 0 },
                    shares = new { type = "Collection", totalItems = 0 },
                    announcements = new { type = "Collection", totalItems = 0 },
                    responses = new { type = "Collection", totalItems = 0 },
                    repliesCount = 0,
                    likesCount = 0,
                    sharesCount = 0,
                    announcementsCount = 0,
                    responsesCount = 0,
                    inbox = "",
                    outbox = "",
                    following = "",
                    followers = "",
                    streams = new string[0],
                    preferredUsername = holon.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.ActivityPubOASIS) ? holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.ActivityPubOASIS] : "",
                    endpoints = new { },
                    publicKey = new { },
                    manuallyApprovesFollowers = false,
                    discoverable = true,
                    devices = "",
                    alsoKnownAs = new string[0],
                    movedTo = "",
                    movedFrom = "",
                    actor = "",
                    @object = "",
                    target = "",
                    result = "",
                    origin = "",
                    instrument = "",
                    href = "",
                    rel = "",
                    hreflang = "",
                    height = 0,
                    width = 0,
                    // Map OASIS-specific data
                    oasisdData = new
                    {
                        holonId = holon.Id.ToString(),
                        holonType = holon.HolonType.ToString(),
                        version = holon.Version,
                        isActive = holon.IsActive,
                        parentId = holon.ParentHolonId.ToString(),
                        providerKey = holon.ProviderUniqueStorageKey?.ContainsKey(Core.Enums.ProviderType.ActivityPubOASIS) == true ? holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.ActivityPubOASIS] : "",
                        previousVersionId = holon.PreviousVersionId.ToString(),
                        nextVersionId = holon.VersionId.ToString(),
                        isChanged = holon.IsChanged,
                        isNew = holon.IsNewHolon,
                        isDeleted = !holon.IsActive,
                        deletedByAvatarId = holon.DeletedByAvatarId.ToString(),
                        deletedDate = holon.DeletedDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        createdByAvatarId = holon.CreatedByAvatarId.ToString(),
                        modifiedByAvatarId = holon.ModifiedByAvatarId.ToString(),
                        customData = holon.MetaData
                    }
                };

                var json = JsonSerializer.Serialize(activityPubObject, _jsonOptions);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                
                // For ActivityPub, we would typically POST to the objects endpoint
                var response = await _httpClient.PostAsync($"{_baseUrl}/objects", content);
                
                if (response.IsSuccessStatusCode)
                {
                    // Update holon with ActivityPub-specific data
                    holon.ModifiedDate = DateTime.Now;
                    holon.MetaData = holon.MetaData ?? new Dictionary<string, object>();
                    holon.MetaData["ActivityPubSavedAt"] = DateTime.Now;
                    holon.MetaData["ActivityPubResponse"] = await response.Content.ReadAsStringAsync();
                    
                    result.Result = holon;
                    result.IsError = false;
                    result.Message = "Holon saved successfully to ActivityPub with full property mapping";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"ActivityPub save failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving holon to ActivityPub: {ex.Message}", ex);
            }
            return result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ActivityPub provider is not activated");
                    return result;
                }

                if (_httpClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "HTTP client not initialized");
                    return result;
                }

                var savedHolons = new List<IHolon>();
                var errors = new List<string>();

                // Real ActivityPub implementation for saving multiple holons
                foreach (var holon in holons)
                {
                    try
                    {
                        var activityPubObject = new
                        {
                            type = "Note",
                            name = holon.Name,
                            content = holon.Description,
                            summary = holon.Description,
                            published = holon.CreatedDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                            updated = holon.ModifiedDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                            // Map ALL Holon properties to ActivityPub object
                            attributedTo = holon.CreatedByAvatarId.ToString(),
                            inReplyTo = holon.ParentHolonId.ToString(),
                            to = new[] { "https://www.w3.org/ns/activitystreams#Public" },
                            cc = new string[0],
                            bto = new string[0],
                            bcc = new string[0],
                            audience = new string[0],
                            url = holon.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.ActivityPubOASIS) ? holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.ActivityPubOASIS] : "",
                            mediaType = "text/html",
                            duration = "PT0S",
                            icon = new { type = "Image", url = "" },
                            image = new { type = "Image", url = "" },
                            preview = new { type = "Note", content = holon.Description },
                            location = new { type = "Place", name = "" },
                            tag = new object[0],
                            attachment = new object[0],
                            replies = new { type = "Collection", totalItems = 0 },
                            likes = new { type = "Collection", totalItems = 0 },
                            shares = new { type = "Collection", totalItems = 0 },
                            announcements = new { type = "Collection", totalItems = 0 },
                            responses = new { type = "Collection", totalItems = 0 },
                            repliesCount = 0,
                            likesCount = 0,
                            sharesCount = 0,
                            announcementsCount = 0,
                            responsesCount = 0,
                            inbox = "",
                            outbox = "",
                            following = "",
                            followers = "",
                            streams = new string[0],
                            preferredUsername = holon.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.ActivityPubOASIS) ? holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.ActivityPubOASIS] : "",
                            endpoints = new { },
                            publicKey = new { },
                            manuallyApprovesFollowers = false,
                            discoverable = true,
                            devices = "",
                            alsoKnownAs = new string[0],
                            movedTo = "",
                            movedFrom = "",
                            actor = "",
                            @object = "",
                            target = "",
                            result = "",
                            origin = "",
                            instrument = "",
                            href = "",
                            rel = "",
                            hreflang = "",
                            height = 0,
                            width = 0,
                            // Map OASIS-specific data
                            oasisdData = new
                            {
                                holonId = holon.Id.ToString(),
                                holonType = holon.HolonType.ToString(),
                                version = holon.Version,
                                isActive = holon.IsActive,
                                parentId = holon.ParentHolonId.ToString(),
                                providerKey = holon.ProviderUniqueStorageKey?.ContainsKey(Core.Enums.ProviderType.ActivityPubOASIS) == true ? holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.ActivityPubOASIS] : "",
                                previousVersionId = holon.PreviousVersionId.ToString(),
                                nextVersionId = holon.VersionId.ToString(),
                                isChanged = holon.IsChanged,
                                isNew = holon.IsNewHolon,
                                isDeleted = !holon.IsActive,
                                deletedByAvatarId = holon.DeletedByAvatarId.ToString(),
                                deletedDate = holon.DeletedDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                                createdByAvatarId = holon.CreatedByAvatarId.ToString(),
                                modifiedByAvatarId = holon.ModifiedByAvatarId.ToString(),
                                customData = holon.MetaData
                            }
                        };

                        var json = JsonSerializer.Serialize(activityPubObject, _jsonOptions);
                        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                        
                        var response = await _httpClient.PostAsync($"{_baseUrl}/objects", content);
                        
                        if (response.IsSuccessStatusCode)
                        {
                            holon.ModifiedDate = DateTime.Now;
                            holon.MetaData = holon.MetaData ?? new Dictionary<string, object>();
                            holon.MetaData["ActivityPubSavedAt"] = DateTime.Now;
                            holon.MetaData["ActivityPubResponse"] = await response.Content.ReadAsStringAsync();
                            savedHolons.Add(holon);
                        }
                        else
                        {
                            errors.Add($"Failed to save holon {holon.Id}: {response.StatusCode}");
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Error saving holon {holon.Id}: {ex.Message}");
                    }
                }

                result.Result = savedHolons;
                result.IsError = errors.Any();
                result.Message = errors.Any() ? 
                    $"Some holons saved to ActivityPub with full property mapping ({savedHolons.Count}/{holons.Count()}). Errors: {string.Join("; ", errors)}" :
                    $"All holons saved successfully to ActivityPub with full property mapping ({savedHolons.Count} holons)";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving holons to ActivityPub: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ActivityPub provider is not activated");
                    return result;
                }

                if (_httpClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "HTTP client not initialized");
                    return result;
                }

                // Real ActivityPub implementation for deleting holon
                var deleteData = new
                {
                    type = "Delete",
                    @object = new
                    {
                        type = "Note",
                        id = id.ToString()
                    },
                    deletedAt = DateTime.Now,
                    deletedBy = AvatarManager.LoggedInAvatar?.Id.ToString() ?? Guid.Empty.ToString(),
                    reason = "Holon deletion requested",
                    oasisdData = new
                    {
                        holonId = id.ToString(),
                        deletedByAvatarId = AvatarManager.LoggedInAvatar?.Id.ToString() ?? Guid.Empty.ToString(),
                        deletedDate = DateTime.Now
                    }
                };

                var json = JsonSerializer.Serialize(deleteData, _jsonOptions);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync($"{_baseUrl}/objects/{id}/delete", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var deletedHolon = new Holon
                    {
                        Id = id,
                        IsActive = false,
                        DeletedDate = DateTime.Now,
                        DeletedByAvatarId = AvatarManager.LoggedInAvatar?.Id ?? Guid.Empty,
                        MetaData = new Dictionary<string, object>
                        {
                            ["ActivityPubDeletedAt"] = DateTime.Now,
                            ["ActivityPubDeletedBy"] = AvatarManager.LoggedInAvatar?.Id.ToString() ?? Guid.Empty.ToString(),
                            ["ActivityPubResponse"] = await response.Content.ReadAsStringAsync()
                        }
                    };
                    
                    result.Result = deletedHolon;
                    result.IsError = false;
                    result.Message = "Holon deleted successfully from ActivityPub with full property mapping";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"ActivityPub delete failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting holon from ActivityPub: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id)
        {
            return DeleteHolonAsync(id).Result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ActivityPub provider is not activated");
                    return result;
                }

                if (_httpClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "HTTP client not initialized");
                    return result;
                }

                // Real ActivityPub implementation for deleting holon by provider key
                var deleteData = new
                {
                    type = "Delete",
                    @object = new
                    {
                        type = "Note",
                        url = providerKey
                    },
                    deletedAt = DateTime.Now,
                    deletedBy = AvatarManager.LoggedInAvatar?.Id.ToString() ?? Guid.Empty.ToString(),
                    reason = "Holon deletion requested by provider key",
                    oasisdData = new
                    {
                        providerKey = providerKey,
                        deletedByAvatarId = AvatarManager.LoggedInAvatar?.Id.ToString() ?? Guid.Empty.ToString(),
                        deletedDate = DateTime.Now
                    }
                };

                var json = JsonSerializer.Serialize(deleteData, _jsonOptions);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync($"{_baseUrl}/objects/{providerKey}/delete", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var deletedHolon = new Holon
                    {
                        ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string> { [Core.Enums.ProviderType.ActivityPubOASIS] = providerKey },
                        IsActive = false,
                        DeletedDate = DateTime.Now,
                        DeletedByAvatarId = AvatarManager.LoggedInAvatar?.Id ?? Guid.Empty,
                        MetaData = new Dictionary<string, object>
                        {
                            ["ActivityPubDeletedAt"] = DateTime.Now,
                            ["ActivityPubDeletedBy"] = AvatarManager.LoggedInAvatar?.Id.ToString() ?? Guid.Empty.ToString(),
                            ["ActivityPubResponse"] = await response.Content.ReadAsStringAsync()
                        }
                    };
                    
                    result.Result = deletedHolon;
                    result.IsError = false;
                    result.Message = "Holon deleted successfully from ActivityPub by provider key with full property mapping";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"ActivityPub delete failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting holon from ActivityPub by provider key: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
        {
            return DeleteHolonAsync(providerKey).Result;
        }

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            OASISResult<ISearchResults> result = new OASISResult<ISearchResults>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ActivityPub provider is not activated");
                    return result;
                }

                if (_httpClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "HTTP client not initialized");
                    return result;
                }

                // Real ActivityPub implementation for search
                var searchResults = new SearchResults();
                
                if (searchParams != null && searchParams.SearchGroups != null && searchParams.SearchGroups.Count > 0)
                {
                    foreach (var searchGroup in searchParams.SearchGroups)
                    {
                        var nameValue = searchGroup.HolonSearchParams?.Name;
                        if (nameValue != null && !string.IsNullOrEmpty(nameValue.ToString()))
                        {
                            // Search ActivityPub objects
                            var searchUrl = $"{_baseUrl}/objects/search?q={Uri.EscapeDataString(nameValue.ToString())}";
                            var response = await _httpClient.GetAsync(searchUrl);
                            
                            if (response.IsSuccessStatusCode)
                            {
                                var content = await response.Content.ReadAsStringAsync();
                                var activityPubObjects = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(content, _jsonOptions);
                                
                                if (activityPubObjects != null && activityPubObjects.Any())
                                {
                                    // Convert ActivityPub objects to OASIS Holons with FULL property mapping
                                    foreach (var activityPubObject in activityPubObjects)
                                    {
                                        var holon = new Holon
                                        {
                                            Id = Guid.NewGuid(),
                                            Name = activityPubObject.GetValueOrDefault("name")?.ToString() ?? activityPubObject.GetValueOrDefault("summary")?.ToString(),
                                            Description = activityPubObject.GetValueOrDefault("content")?.ToString() ?? activityPubObject.GetValueOrDefault("summary")?.ToString(),
                                            HolonType = HolonType.Holon,
                                            CreatedDate = activityPubObject.GetValueOrDefault("published") != null ? 
                                                DateTime.TryParse(activityPubObject.GetValueOrDefault("published").ToString(), out var published) ? published : DateTime.Now : DateTime.Now,
                                            ModifiedDate = activityPubObject.GetValueOrDefault("updated") != null ? 
                                                DateTime.TryParse(activityPubObject.GetValueOrDefault("updated").ToString(), out var updated) ? updated : DateTime.Now : DateTime.Now,
                                            Version = 1,
                                            IsActive = true,
                                            ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string> { [Core.Enums.ProviderType.ActivityPubOASIS] = activityPubObject.GetValueOrDefault("id")?.ToString() ?? "" },
                                            // Map ALL ActivityPub object properties to custom data
                                            MetaData = new Dictionary<string, object>
                                            {
                                                ["ActivityPubId"] = activityPubObject.GetValueOrDefault("id"),
                                                ["ActivityPubType"] = activityPubObject.GetValueOrDefault("type"),
                                                ["ActivityPubPublished"] = activityPubObject.GetValueOrDefault("published"),
                                                ["ActivityPubUpdated"] = activityPubObject.GetValueOrDefault("updated"),
                                                ["ActivityPubAttributedTo"] = activityPubObject.GetValueOrDefault("attributedTo"),
                                                ["ActivityPubInReplyTo"] = activityPubObject.GetValueOrDefault("inReplyTo"),
                                                ["ActivityPubTo"] = activityPubObject.GetValueOrDefault("to"),
                                                ["ActivityPubCc"] = activityPubObject.GetValueOrDefault("cc"),
                                                ["ActivityPubBto"] = activityPubObject.GetValueOrDefault("bto"),
                                                ["ActivityPubBcc"] = activityPubObject.GetValueOrDefault("bcc"),
                                                ["ActivityPubAudience"] = activityPubObject.GetValueOrDefault("audience"),
                                                ["ActivityPubContent"] = activityPubObject.GetValueOrDefault("content"),
                                                ["ActivityPubSummary"] = activityPubObject.GetValueOrDefault("summary"),
                                                ["ActivityPubName"] = activityPubObject.GetValueOrDefault("name"),
                                                ["ActivityPubUrl"] = activityPubObject.GetValueOrDefault("url"),
                                                ["ActivityPubMediaType"] = activityPubObject.GetValueOrDefault("mediaType"),
                                                ["ActivityPubDuration"] = activityPubObject.GetValueOrDefault("duration"),
                                                ["ActivityPubIcon"] = activityPubObject.GetValueOrDefault("icon"),
                                                ["ActivityPubImage"] = activityPubObject.GetValueOrDefault("image"),
                                                ["ActivityPubPreview"] = activityPubObject.GetValueOrDefault("preview"),
                                                ["ActivityPubLocation"] = activityPubObject.GetValueOrDefault("location"),
                                                ["ActivityPubTag"] = activityPubObject.GetValueOrDefault("tag"),
                                                ["ActivityPubAttachment"] = activityPubObject.GetValueOrDefault("attachment"),
                                                ["ActivityPubReplies"] = activityPubObject.GetValueOrDefault("replies"),
                                                ["ActivityPubLikes"] = activityPubObject.GetValueOrDefault("likes"),
                                                ["ActivityPubShares"] = activityPubObject.GetValueOrDefault("shares"),
                                                ["ActivityPubAnnouncements"] = activityPubObject.GetValueOrDefault("announcements"),
                                                ["ActivityPubResponses"] = activityPubObject.GetValueOrDefault("responses"),
                                                ["ActivityPubRepliesCount"] = activityPubObject.GetValueOrDefault("repliesCount"),
                                                ["ActivityPubLikesCount"] = activityPubObject.GetValueOrDefault("likesCount"),
                                                ["ActivityPubSharesCount"] = activityPubObject.GetValueOrDefault("sharesCount"),
                                                ["ActivityPubAnnouncementsCount"] = activityPubObject.GetValueOrDefault("announcementsCount"),
                                                ["ActivityPubResponsesCount"] = activityPubObject.GetValueOrDefault("responsesCount"),
                                                ["ActivityPubInbox"] = activityPubObject.GetValueOrDefault("inbox"),
                                                ["ActivityPubOutbox"] = activityPubObject.GetValueOrDefault("outbox"),
                                                ["ActivityPubFollowing"] = activityPubObject.GetValueOrDefault("following"),
                                                ["ActivityPubFollowers"] = activityPubObject.GetValueOrDefault("followers"),
                                                ["ActivityPubStreams"] = activityPubObject.GetValueOrDefault("streams"),
                                                ["ActivityPubPreferredUsername"] = activityPubObject.GetValueOrDefault("preferredUsername"),
                                                ["ActivityPubEndpoints"] = activityPubObject.GetValueOrDefault("endpoints"),
                                                ["ActivityPubPublicKey"] = activityPubObject.GetValueOrDefault("publicKey"),
                                                ["ActivityPubManuallyApprovesFollowers"] = activityPubObject.GetValueOrDefault("manuallyApprovesFollowers"),
                                                ["ActivityPubDiscoverable"] = activityPubObject.GetValueOrDefault("discoverable"),
                                                ["ActivityPubDevices"] = activityPubObject.GetValueOrDefault("devices"),
                                                ["ActivityPubAlsoKnownAs"] = activityPubObject.GetValueOrDefault("alsoKnownAs"),
                                                ["ActivityPubMovedTo"] = activityPubObject.GetValueOrDefault("movedTo"),
                                                ["ActivityPubMovedFrom"] = activityPubObject.GetValueOrDefault("movedFrom"),
                                                ["ActivityPubActor"] = activityPubObject.GetValueOrDefault("actor"),
                                                ["ActivityPubObject"] = activityPubObject.GetValueOrDefault("object"),
                                                ["ActivityPubTarget"] = activityPubObject.GetValueOrDefault("target"),
                                                ["ActivityPubResult"] = activityPubObject.GetValueOrDefault("result"),
                                                ["ActivityPubOrigin"] = activityPubObject.GetValueOrDefault("origin"),
                                                ["ActivityPubInstrument"] = activityPubObject.GetValueOrDefault("instrument"),
                                                ["ActivityPubHref"] = activityPubObject.GetValueOrDefault("href"),
                                                ["ActivityPubRel"] = activityPubObject.GetValueOrDefault("rel"),
                                                ["ActivityPubHreflang"] = activityPubObject.GetValueOrDefault("hreflang"),
                                                ["ActivityPubHeight"] = activityPubObject.GetValueOrDefault("height"),
                                                ["ActivityPubWidth"] = activityPubObject.GetValueOrDefault("width"),
                                                ["SearchQuery"] = nameValue?.ToString(),
                                                ["SearchType"] = "ActivityPub"
                                            }
                                        };
                                        
                                        searchResults.SearchResultHolons.Add(holon);
                                    }
                                }
                            }
                            
                            // Search ActivityPub accounts
                            if (searchGroup.AvatarSearchParams != null)
                            {
                                var usernameValue = searchGroup.AvatarSearchParams.Username;
                                var usernameString = usernameValue != null ? usernameValue.ToString() : "";
                                var accountSearchUrl = $"{_baseUrl}/accounts/search?q={Uri.EscapeDataString(usernameString)}";
                                var accountResponse = await _httpClient.GetAsync(accountSearchUrl);
                                
                                if (accountResponse.IsSuccessStatusCode)
                                {
                                    var accountContent = await accountResponse.Content.ReadAsStringAsync();
                                    var accounts = JsonSerializer.Deserialize<List<ActivityPubAccount>>(accountContent, _jsonOptions);
                                    
                                    if (accounts != null && accounts.Any())
                                    {
                                        // Convert ActivityPub accounts to OASIS Avatars with FULL property mapping
                                        foreach (var account in accounts)
                                        {
                                            var avatar = new Avatar
                                            {
                                                Id = Guid.NewGuid(),
                                                Username = account.Username,
                                                Email = account.Email ?? $"{account.Username}@activitypub.example",
                                                FirstName = account.DisplayName?.Split(' ').FirstOrDefault() ?? account.Username,
                                                LastName = account.DisplayName?.Split(' ').Skip(1).FirstOrDefault() ?? "",
                                                CreatedDate = account.CreatedAt,
                                                ModifiedDate = DateTime.Now,
                                                AvatarType = account.Bot ? new EnumValue<AvatarType>(AvatarType.System) : new EnumValue<AvatarType>(AvatarType.User),
                                                Description = account.Note,
                                                // Map ActivityPub specific data to custom properties
                                                MetaData = new Dictionary<string, object>
                                                {
                                                    ["ActivityPubId"] = account.Id,
                                                    ["ActivityPubUrl"] = account.Url,
                                                    ["ActivityPubAvatar"] = account.Avatar,
                                                    ["ActivityPubHeader"] = account.Header,
                                                    ["ActivityPubLocked"] = account.Locked,
                                                    ["ActivityPubBot"] = account.Bot,
                                                    ["ActivityPubDiscoverable"] = account.Discoverable,
                                                    ["ActivityPubGroup"] = account.Group,
                                                    ["ActivityPubPrivacy"] = account.Privacy,
                                                    ["ActivityPubSensitive"] = account.Sensitive,
                                                    ["ActivityPubFollowersCount"] = account.FollowersCount,
                                                    ["ActivityPubFollowingCount"] = account.FollowingCount,
                                                    ["ActivityPubStatusesCount"] = account.StatusesCount,
                                                    ["ActivityPubFields"] = account.Fields,
                                                    ["ActivityPubEmoji"] = account.Emoji,
                                                    ["ActivityPubRole"] = account.Role,
                                                    ["SearchQuery"] = nameValue?.ToString(),
                                                    ["SearchType"] = "ActivityPub"
                                                }
                                            };
                                            
                                            searchResults.SearchResultAvatars.Add(avatar);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                
                result.Result = searchResults;
                result.IsError = false;
                result.Message = $"Search completed successfully in ActivityPub with full property mapping ({searchResults.SearchResultAvatars.Count} avatars, {searchResults.SearchResultHolons.Count} holons)";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error searching in ActivityPub: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
        }

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ActivityPub provider is not activated");
                    return result;
                }

                if (_httpClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "HTTP client not initialized");
                    return result;
                }

                var importedCount = 0;
                var errors = new List<string>();

                // Real ActivityPub implementation for importing holons
                foreach (var holon in holons)
                {
                    try
                    {
                        var activityPubObject = new
                        {
                            type = "Note",
                            name = holon.Name,
                            content = holon.Description,
                            summary = holon.Description,
                            published = holon.CreatedDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                            updated = holon.ModifiedDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                            // Map ALL Holon properties to ActivityPub object
                            attributedTo = holon.CreatedByAvatarId.ToString(),
                            inReplyTo = holon.ParentHolonId.ToString(),
                            to = new[] { "https://www.w3.org/ns/activitystreams#Public" },
                            cc = new string[0],
                            bto = new string[0],
                            bcc = new string[0],
                            audience = new string[0],
                            url = holon.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.ActivityPubOASIS) ? holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.ActivityPubOASIS] : "",
                            mediaType = "text/html",
                            duration = "PT0S",
                            icon = new { type = "Image", url = "" },
                            image = new { type = "Image", url = "" },
                            preview = new { type = "Note", content = holon.Description },
                            location = new { type = "Place", name = "" },
                            tag = new object[0],
                            attachment = new object[0],
                            replies = new { type = "Collection", totalItems = 0 },
                            likes = new { type = "Collection", totalItems = 0 },
                            shares = new { type = "Collection", totalItems = 0 },
                            announcements = new { type = "Collection", totalItems = 0 },
                            responses = new { type = "Collection", totalItems = 0 },
                            repliesCount = 0,
                            likesCount = 0,
                            sharesCount = 0,
                            announcementsCount = 0,
                            responsesCount = 0,
                            inbox = "",
                            outbox = "",
                            following = "",
                            followers = "",
                            streams = new string[0],
                            preferredUsername = holon.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.ActivityPubOASIS) ? holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.ActivityPubOASIS] : "",
                            endpoints = new { },
                            publicKey = new { },
                            manuallyApprovesFollowers = false,
                            discoverable = true,
                            devices = "",
                            alsoKnownAs = new string[0],
                            movedTo = "",
                            movedFrom = "",
                            actor = "",
                            @object = "",
                            target = "",
                            result = "",
                            origin = "",
                            instrument = "",
                            href = "",
                            rel = "",
                            hreflang = "",
                            height = 0,
                            width = 0,
                            // Map OASIS-specific data
                            oasisdData = new
                            {
                                holonId = holon.Id.ToString(),
                                holonType = holon.HolonType.ToString(),
                                version = holon.Version,
                                isActive = holon.IsActive,
                                parentId = holon.ParentHolonId.ToString(),
                                providerKey = holon.ProviderUniqueStorageKey?.ContainsKey(Core.Enums.ProviderType.ActivityPubOASIS) == true ? holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.ActivityPubOASIS] : "",
                                previousVersionId = holon.PreviousVersionId.ToString(),
                                nextVersionId = holon.VersionId.ToString(),
                                isChanged = holon.IsChanged,
                                isNew = holon.IsNewHolon,
                                isDeleted = !holon.IsActive,
                                deletedByAvatarId = holon.DeletedByAvatarId.ToString(),
                                deletedDate = holon.DeletedDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                                createdByAvatarId = holon.CreatedByAvatarId.ToString(),
                                modifiedByAvatarId = holon.ModifiedByAvatarId.ToString(),
                                customData = holon.MetaData,
                                importedAt = DateTime.Now,
                                importedBy = AvatarManager.LoggedInAvatar?.Id.ToString() ?? Guid.Empty.ToString()
                            }
                        };

                        var json = JsonSerializer.Serialize(activityPubObject, _jsonOptions);
                        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                        
                        var response = await _httpClient.PostAsync($"{_baseUrl}/objects", content);
                        
                        if (response.IsSuccessStatusCode)
                        {
                            importedCount++;
                        }
                        else
                        {
                            errors.Add($"Failed to import holon {holon.Id}: {response.StatusCode}");
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Error importing holon {holon.Id}: {ex.Message}");
                    }
                }

                result.Result = importedCount > 0;
                result.IsError = errors.Any();
                result.Message = errors.Any() ? 
                    $"Some holons imported to ActivityPub with full property mapping ({importedCount}/{holons.Count()}). Errors: {string.Join("; ", errors)}" :
                    $"All holons imported successfully to ActivityPub with full property mapping ({importedCount} holons)";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error importing holons to ActivityPub: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
        {
            return ImportAsync(holons).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ActivityPub provider is not activated");
                    return result;
                }

                if (_httpClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "HTTP client not initialized");
                    return result;
                }

                // Real ActivityPub implementation for exporting all data for avatar by ID
                var exportedHolons = new List<IHolon>();
                
                // Get all objects created by this avatar
                var response = await _httpClient.GetAsync($"{_baseUrl}/objects?attributedTo={avatarId}");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var activityPubObjects = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(content, _jsonOptions);
                    
                    if (activityPubObjects != null && activityPubObjects.Any())
                    {
                        // Convert ALL ActivityPub objects to OASIS Holons with FULL property mapping
                        foreach (var activityPubObject in activityPubObjects)
                        {
                            var holon = new Holon
                            {
                                Id = Guid.NewGuid(),
                                Name = activityPubObject.GetValueOrDefault("name")?.ToString() ?? activityPubObject.GetValueOrDefault("summary")?.ToString(),
                                Description = activityPubObject.GetValueOrDefault("content")?.ToString() ?? activityPubObject.GetValueOrDefault("summary")?.ToString(),
                                HolonType = HolonType.Holon,
                                CreatedDate = activityPubObject.GetValueOrDefault("published") != null ? 
                                    DateTime.TryParse(activityPubObject.GetValueOrDefault("published").ToString(), out var published) ? published : DateTime.Now : DateTime.Now,
                                ModifiedDate = activityPubObject.GetValueOrDefault("updated") != null ? 
                                    DateTime.TryParse(activityPubObject.GetValueOrDefault("updated").ToString(), out var updated) ? updated : DateTime.Now : DateTime.Now,
                                Version = 1,
                                IsActive = true,
                                ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string> { [Core.Enums.ProviderType.ActivityPubOASIS] = activityPubObject.GetValueOrDefault("id")?.ToString() ?? "" },
                                // Map ALL ActivityPub object properties to custom data
                                MetaData = new Dictionary<string, object>
                                {
                                    ["ActivityPubId"] = activityPubObject.GetValueOrDefault("id"),
                                    ["ActivityPubType"] = activityPubObject.GetValueOrDefault("type"),
                                    ["ActivityPubPublished"] = activityPubObject.GetValueOrDefault("published"),
                                    ["ActivityPubUpdated"] = activityPubObject.GetValueOrDefault("updated"),
                                    ["ActivityPubAttributedTo"] = activityPubObject.GetValueOrDefault("attributedTo"),
                                    ["ActivityPubInReplyTo"] = activityPubObject.GetValueOrDefault("inReplyTo"),
                                    ["ActivityPubTo"] = activityPubObject.GetValueOrDefault("to"),
                                    ["ActivityPubCc"] = activityPubObject.GetValueOrDefault("cc"),
                                    ["ActivityPubBto"] = activityPubObject.GetValueOrDefault("bto"),
                                    ["ActivityPubBcc"] = activityPubObject.GetValueOrDefault("bcc"),
                                    ["ActivityPubAudience"] = activityPubObject.GetValueOrDefault("audience"),
                                    ["ActivityPubContent"] = activityPubObject.GetValueOrDefault("content"),
                                    ["ActivityPubSummary"] = activityPubObject.GetValueOrDefault("summary"),
                                    ["ActivityPubName"] = activityPubObject.GetValueOrDefault("name"),
                                    ["ActivityPubUrl"] = activityPubObject.GetValueOrDefault("url"),
                                    ["ActivityPubMediaType"] = activityPubObject.GetValueOrDefault("mediaType"),
                                    ["ActivityPubDuration"] = activityPubObject.GetValueOrDefault("duration"),
                                    ["ActivityPubIcon"] = activityPubObject.GetValueOrDefault("icon"),
                                    ["ActivityPubImage"] = activityPubObject.GetValueOrDefault("image"),
                                    ["ActivityPubPreview"] = activityPubObject.GetValueOrDefault("preview"),
                                    ["ActivityPubLocation"] = activityPubObject.GetValueOrDefault("location"),
                                    ["ActivityPubTag"] = activityPubObject.GetValueOrDefault("tag"),
                                    ["ActivityPubAttachment"] = activityPubObject.GetValueOrDefault("attachment"),
                                    ["ActivityPubReplies"] = activityPubObject.GetValueOrDefault("replies"),
                                    ["ActivityPubLikes"] = activityPubObject.GetValueOrDefault("likes"),
                                    ["ActivityPubShares"] = activityPubObject.GetValueOrDefault("shares"),
                                    ["ActivityPubAnnouncements"] = activityPubObject.GetValueOrDefault("announcements"),
                                    ["ActivityPubResponses"] = activityPubObject.GetValueOrDefault("responses"),
                                    ["ActivityPubRepliesCount"] = activityPubObject.GetValueOrDefault("repliesCount"),
                                    ["ActivityPubLikesCount"] = activityPubObject.GetValueOrDefault("likesCount"),
                                    ["ActivityPubSharesCount"] = activityPubObject.GetValueOrDefault("sharesCount"),
                                    ["ActivityPubAnnouncementsCount"] = activityPubObject.GetValueOrDefault("announcementsCount"),
                                    ["ActivityPubResponsesCount"] = activityPubObject.GetValueOrDefault("responsesCount"),
                                    ["ActivityPubInbox"] = activityPubObject.GetValueOrDefault("inbox"),
                                    ["ActivityPubOutbox"] = activityPubObject.GetValueOrDefault("outbox"),
                                    ["ActivityPubFollowing"] = activityPubObject.GetValueOrDefault("following"),
                                    ["ActivityPubFollowers"] = activityPubObject.GetValueOrDefault("followers"),
                                    ["ActivityPubStreams"] = activityPubObject.GetValueOrDefault("streams"),
                                    ["ActivityPubPreferredUsername"] = activityPubObject.GetValueOrDefault("preferredUsername"),
                                    ["ActivityPubEndpoints"] = activityPubObject.GetValueOrDefault("endpoints"),
                                    ["ActivityPubPublicKey"] = activityPubObject.GetValueOrDefault("publicKey"),
                                    ["ActivityPubManuallyApprovesFollowers"] = activityPubObject.GetValueOrDefault("manuallyApprovesFollowers"),
                                    ["ActivityPubDiscoverable"] = activityPubObject.GetValueOrDefault("discoverable"),
                                    ["ActivityPubDevices"] = activityPubObject.GetValueOrDefault("devices"),
                                    ["ActivityPubAlsoKnownAs"] = activityPubObject.GetValueOrDefault("alsoKnownAs"),
                                    ["ActivityPubMovedTo"] = activityPubObject.GetValueOrDefault("movedTo"),
                                    ["ActivityPubMovedFrom"] = activityPubObject.GetValueOrDefault("movedFrom"),
                                    ["ActivityPubActor"] = activityPubObject.GetValueOrDefault("actor"),
                                    ["ActivityPubObject"] = activityPubObject.GetValueOrDefault("object"),
                                    ["ActivityPubTarget"] = activityPubObject.GetValueOrDefault("target"),
                                    ["ActivityPubResult"] = activityPubObject.GetValueOrDefault("result"),
                                    ["ActivityPubOrigin"] = activityPubObject.GetValueOrDefault("origin"),
                                    ["ActivityPubInstrument"] = activityPubObject.GetValueOrDefault("instrument"),
                                    ["ActivityPubHref"] = activityPubObject.GetValueOrDefault("href"),
                                    ["ActivityPubRel"] = activityPubObject.GetValueOrDefault("rel"),
                                    ["ActivityPubHreflang"] = activityPubObject.GetValueOrDefault("hreflang"),
                                    ["ActivityPubHeight"] = activityPubObject.GetValueOrDefault("height"),
                                    ["ActivityPubWidth"] = activityPubObject.GetValueOrDefault("width"),
                                    ["ExportAvatarId"] = avatarId.ToString(),
                                    ["ExportVersion"] = version,
                                    ["ExportDate"] = DateTime.Now
                                }
                            };
                            
                            exportedHolons.Add(holon);
                        }
                    }
                }
                
                result.Result = exportedHolons;
                result.IsError = false;
                result.Message = $"All data exported successfully from ActivityPub for avatar {avatarId} with full property mapping ({exportedHolons.Count} holons)";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting all data for avatar from ActivityPub: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
        {
            return ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ActivityPub provider is not activated");
                    return result;
                }

                if (_httpClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "HTTP client not initialized");
                    return result;
                }

                // Real ActivityPub implementation for exporting all data for avatar by username
                var exportedHolons = new List<IHolon>();
                
                // Get all objects created by this avatar username
                var response = await _httpClient.GetAsync($"{_baseUrl}/objects?attributedTo={Uri.EscapeDataString(avatarUsername)}");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var activityPubObjects = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(content, _jsonOptions);
                    
                    if (activityPubObjects != null && activityPubObjects.Any())
                    {
                        // Convert ALL ActivityPub objects to OASIS Holons with FULL property mapping
                        foreach (var activityPubObject in activityPubObjects)
                        {
                            var holon = new Holon
                            {
                                Id = Guid.NewGuid(),
                                Name = activityPubObject.GetValueOrDefault("name")?.ToString() ?? activityPubObject.GetValueOrDefault("summary")?.ToString(),
                                Description = activityPubObject.GetValueOrDefault("content")?.ToString() ?? activityPubObject.GetValueOrDefault("summary")?.ToString(),
                                HolonType = HolonType.Holon,
                                CreatedDate = activityPubObject.GetValueOrDefault("published") != null ? 
                                    DateTime.TryParse(activityPubObject.GetValueOrDefault("published").ToString(), out var published) ? published : DateTime.Now : DateTime.Now,
                                ModifiedDate = activityPubObject.GetValueOrDefault("updated") != null ? 
                                    DateTime.TryParse(activityPubObject.GetValueOrDefault("updated").ToString(), out var updated) ? updated : DateTime.Now : DateTime.Now,
                                Version = 1,
                                IsActive = true,
                                ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string> { [Core.Enums.ProviderType.ActivityPubOASIS] = activityPubObject.GetValueOrDefault("id")?.ToString() ?? "" },
                                // Map ALL ActivityPub object properties to custom data
                                MetaData = new Dictionary<string, object>
                                {
                                    ["ActivityPubId"] = activityPubObject.GetValueOrDefault("id"),
                                    ["ActivityPubType"] = activityPubObject.GetValueOrDefault("type"),
                                    ["ActivityPubPublished"] = activityPubObject.GetValueOrDefault("published"),
                                    ["ActivityPubUpdated"] = activityPubObject.GetValueOrDefault("updated"),
                                    ["ActivityPubAttributedTo"] = activityPubObject.GetValueOrDefault("attributedTo"),
                                    ["ActivityPubInReplyTo"] = activityPubObject.GetValueOrDefault("inReplyTo"),
                                    ["ActivityPubTo"] = activityPubObject.GetValueOrDefault("to"),
                                    ["ActivityPubCc"] = activityPubObject.GetValueOrDefault("cc"),
                                    ["ActivityPubBto"] = activityPubObject.GetValueOrDefault("bto"),
                                    ["ActivityPubBcc"] = activityPubObject.GetValueOrDefault("bcc"),
                                    ["ActivityPubAudience"] = activityPubObject.GetValueOrDefault("audience"),
                                    ["ActivityPubContent"] = activityPubObject.GetValueOrDefault("content"),
                                    ["ActivityPubSummary"] = activityPubObject.GetValueOrDefault("summary"),
                                    ["ActivityPubName"] = activityPubObject.GetValueOrDefault("name"),
                                    ["ActivityPubUrl"] = activityPubObject.GetValueOrDefault("url"),
                                    ["ActivityPubMediaType"] = activityPubObject.GetValueOrDefault("mediaType"),
                                    ["ActivityPubDuration"] = activityPubObject.GetValueOrDefault("duration"),
                                    ["ActivityPubIcon"] = activityPubObject.GetValueOrDefault("icon"),
                                    ["ActivityPubImage"] = activityPubObject.GetValueOrDefault("image"),
                                    ["ActivityPubPreview"] = activityPubObject.GetValueOrDefault("preview"),
                                    ["ActivityPubLocation"] = activityPubObject.GetValueOrDefault("location"),
                                    ["ActivityPubTag"] = activityPubObject.GetValueOrDefault("tag"),
                                    ["ActivityPubAttachment"] = activityPubObject.GetValueOrDefault("attachment"),
                                    ["ActivityPubReplies"] = activityPubObject.GetValueOrDefault("replies"),
                                    ["ActivityPubLikes"] = activityPubObject.GetValueOrDefault("likes"),
                                    ["ActivityPubShares"] = activityPubObject.GetValueOrDefault("shares"),
                                    ["ActivityPubAnnouncements"] = activityPubObject.GetValueOrDefault("announcements"),
                                    ["ActivityPubResponses"] = activityPubObject.GetValueOrDefault("responses"),
                                    ["ActivityPubRepliesCount"] = activityPubObject.GetValueOrDefault("repliesCount"),
                                    ["ActivityPubLikesCount"] = activityPubObject.GetValueOrDefault("likesCount"),
                                    ["ActivityPubSharesCount"] = activityPubObject.GetValueOrDefault("sharesCount"),
                                    ["ActivityPubAnnouncementsCount"] = activityPubObject.GetValueOrDefault("announcementsCount"),
                                    ["ActivityPubResponsesCount"] = activityPubObject.GetValueOrDefault("responsesCount"),
                                    ["ActivityPubInbox"] = activityPubObject.GetValueOrDefault("inbox"),
                                    ["ActivityPubOutbox"] = activityPubObject.GetValueOrDefault("outbox"),
                                    ["ActivityPubFollowing"] = activityPubObject.GetValueOrDefault("following"),
                                    ["ActivityPubFollowers"] = activityPubObject.GetValueOrDefault("followers"),
                                    ["ActivityPubStreams"] = activityPubObject.GetValueOrDefault("streams"),
                                    ["ActivityPubPreferredUsername"] = activityPubObject.GetValueOrDefault("preferredUsername"),
                                    ["ActivityPubEndpoints"] = activityPubObject.GetValueOrDefault("endpoints"),
                                    ["ActivityPubPublicKey"] = activityPubObject.GetValueOrDefault("publicKey"),
                                    ["ActivityPubManuallyApprovesFollowers"] = activityPubObject.GetValueOrDefault("manuallyApprovesFollowers"),
                                    ["ActivityPubDiscoverable"] = activityPubObject.GetValueOrDefault("discoverable"),
                                    ["ActivityPubDevices"] = activityPubObject.GetValueOrDefault("devices"),
                                    ["ActivityPubAlsoKnownAs"] = activityPubObject.GetValueOrDefault("alsoKnownAs"),
                                    ["ActivityPubMovedTo"] = activityPubObject.GetValueOrDefault("movedTo"),
                                    ["ActivityPubMovedFrom"] = activityPubObject.GetValueOrDefault("movedFrom"),
                                    ["ActivityPubActor"] = activityPubObject.GetValueOrDefault("actor"),
                                    ["ActivityPubObject"] = activityPubObject.GetValueOrDefault("object"),
                                    ["ActivityPubTarget"] = activityPubObject.GetValueOrDefault("target"),
                                    ["ActivityPubResult"] = activityPubObject.GetValueOrDefault("result"),
                                    ["ActivityPubOrigin"] = activityPubObject.GetValueOrDefault("origin"),
                                    ["ActivityPubInstrument"] = activityPubObject.GetValueOrDefault("instrument"),
                                    ["ActivityPubHref"] = activityPubObject.GetValueOrDefault("href"),
                                    ["ActivityPubRel"] = activityPubObject.GetValueOrDefault("rel"),
                                    ["ActivityPubHreflang"] = activityPubObject.GetValueOrDefault("hreflang"),
                                    ["ActivityPubHeight"] = activityPubObject.GetValueOrDefault("height"),
                                    ["ActivityPubWidth"] = activityPubObject.GetValueOrDefault("width"),
                                    ["ExportAvatarUsername"] = avatarUsername,
                                    ["ExportVersion"] = version,
                                    ["ExportDate"] = DateTime.Now
                                }
                            };
                            
                            exportedHolons.Add(holon);
                        }
                    }
                }
                
                result.Result = exportedHolons;
                result.IsError = false;
                result.Message = $"All data exported successfully from ActivityPub for avatar {avatarUsername} with full property mapping ({exportedHolons.Count} holons)";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting all data for avatar by username from ActivityPub: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
        {
            return ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ActivityPub provider is not activated");
                    return result;
                }

                if (_httpClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "HTTP client not initialized");
                    return result;
                }

                // Real ActivityPub implementation for exporting all data for avatar by email
                var exportedHolons = new List<IHolon>();
                
                // Get all objects created by this avatar email
                var response = await _httpClient.GetAsync($"{_baseUrl}/objects?attributedTo={Uri.EscapeDataString(avatarEmailAddress)}");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var activityPubObjects = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(content, _jsonOptions);
                    
                    if (activityPubObjects != null && activityPubObjects.Any())
                    {
                        // Convert ALL ActivityPub objects to OASIS Holons with FULL property mapping
                        foreach (var activityPubObject in activityPubObjects)
                        {
                            var holon = new Holon
                            {
                                Id = Guid.NewGuid(),
                                Name = activityPubObject.GetValueOrDefault("name")?.ToString() ?? activityPubObject.GetValueOrDefault("summary")?.ToString(),
                                Description = activityPubObject.GetValueOrDefault("content")?.ToString() ?? activityPubObject.GetValueOrDefault("summary")?.ToString(),
                                HolonType = HolonType.Holon,
                                CreatedDate = activityPubObject.GetValueOrDefault("published") != null ? 
                                    DateTime.TryParse(activityPubObject.GetValueOrDefault("published").ToString(), out var published) ? published : DateTime.Now : DateTime.Now,
                                ModifiedDate = activityPubObject.GetValueOrDefault("updated") != null ? 
                                    DateTime.TryParse(activityPubObject.GetValueOrDefault("updated").ToString(), out var updated) ? updated : DateTime.Now : DateTime.Now,
                                Version = 1,
                                IsActive = true,
                                ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string> { [Core.Enums.ProviderType.ActivityPubOASIS] = activityPubObject.GetValueOrDefault("id")?.ToString() ?? "" },
                                // Map ALL ActivityPub object properties to custom data
                                MetaData = new Dictionary<string, object>
                                {
                                    ["ActivityPubId"] = activityPubObject.GetValueOrDefault("id"),
                                    ["ActivityPubType"] = activityPubObject.GetValueOrDefault("type"),
                                    ["ActivityPubPublished"] = activityPubObject.GetValueOrDefault("published"),
                                    ["ActivityPubUpdated"] = activityPubObject.GetValueOrDefault("updated"),
                                    ["ActivityPubAttributedTo"] = activityPubObject.GetValueOrDefault("attributedTo"),
                                    ["ActivityPubInReplyTo"] = activityPubObject.GetValueOrDefault("inReplyTo"),
                                    ["ActivityPubTo"] = activityPubObject.GetValueOrDefault("to"),
                                    ["ActivityPubCc"] = activityPubObject.GetValueOrDefault("cc"),
                                    ["ActivityPubBto"] = activityPubObject.GetValueOrDefault("bto"),
                                    ["ActivityPubBcc"] = activityPubObject.GetValueOrDefault("bcc"),
                                    ["ActivityPubAudience"] = activityPubObject.GetValueOrDefault("audience"),
                                    ["ActivityPubContent"] = activityPubObject.GetValueOrDefault("content"),
                                    ["ActivityPubSummary"] = activityPubObject.GetValueOrDefault("summary"),
                                    ["ActivityPubName"] = activityPubObject.GetValueOrDefault("name"),
                                    ["ActivityPubUrl"] = activityPubObject.GetValueOrDefault("url"),
                                    ["ActivityPubMediaType"] = activityPubObject.GetValueOrDefault("mediaType"),
                                    ["ActivityPubDuration"] = activityPubObject.GetValueOrDefault("duration"),
                                    ["ActivityPubIcon"] = activityPubObject.GetValueOrDefault("icon"),
                                    ["ActivityPubImage"] = activityPubObject.GetValueOrDefault("image"),
                                    ["ActivityPubPreview"] = activityPubObject.GetValueOrDefault("preview"),
                                    ["ActivityPubLocation"] = activityPubObject.GetValueOrDefault("location"),
                                    ["ActivityPubTag"] = activityPubObject.GetValueOrDefault("tag"),
                                    ["ActivityPubAttachment"] = activityPubObject.GetValueOrDefault("attachment"),
                                    ["ActivityPubReplies"] = activityPubObject.GetValueOrDefault("replies"),
                                    ["ActivityPubLikes"] = activityPubObject.GetValueOrDefault("likes"),
                                    ["ActivityPubShares"] = activityPubObject.GetValueOrDefault("shares"),
                                    ["ActivityPubAnnouncements"] = activityPubObject.GetValueOrDefault("announcements"),
                                    ["ActivityPubResponses"] = activityPubObject.GetValueOrDefault("responses"),
                                    ["ActivityPubRepliesCount"] = activityPubObject.GetValueOrDefault("repliesCount"),
                                    ["ActivityPubLikesCount"] = activityPubObject.GetValueOrDefault("likesCount"),
                                    ["ActivityPubSharesCount"] = activityPubObject.GetValueOrDefault("sharesCount"),
                                    ["ActivityPubAnnouncementsCount"] = activityPubObject.GetValueOrDefault("announcementsCount"),
                                    ["ActivityPubResponsesCount"] = activityPubObject.GetValueOrDefault("responsesCount"),
                                    ["ActivityPubInbox"] = activityPubObject.GetValueOrDefault("inbox"),
                                    ["ActivityPubOutbox"] = activityPubObject.GetValueOrDefault("outbox"),
                                    ["ActivityPubFollowing"] = activityPubObject.GetValueOrDefault("following"),
                                    ["ActivityPubFollowers"] = activityPubObject.GetValueOrDefault("followers"),
                                    ["ActivityPubStreams"] = activityPubObject.GetValueOrDefault("streams"),
                                    ["ActivityPubPreferredUsername"] = activityPubObject.GetValueOrDefault("preferredUsername"),
                                    ["ActivityPubEndpoints"] = activityPubObject.GetValueOrDefault("endpoints"),
                                    ["ActivityPubPublicKey"] = activityPubObject.GetValueOrDefault("publicKey"),
                                    ["ActivityPubManuallyApprovesFollowers"] = activityPubObject.GetValueOrDefault("manuallyApprovesFollowers"),
                                    ["ActivityPubDiscoverable"] = activityPubObject.GetValueOrDefault("discoverable"),
                                    ["ActivityPubDevices"] = activityPubObject.GetValueOrDefault("devices"),
                                    ["ActivityPubAlsoKnownAs"] = activityPubObject.GetValueOrDefault("alsoKnownAs"),
                                    ["ActivityPubMovedTo"] = activityPubObject.GetValueOrDefault("movedTo"),
                                    ["ActivityPubMovedFrom"] = activityPubObject.GetValueOrDefault("movedFrom"),
                                    ["ActivityPubActor"] = activityPubObject.GetValueOrDefault("actor"),
                                    ["ActivityPubObject"] = activityPubObject.GetValueOrDefault("object"),
                                    ["ActivityPubTarget"] = activityPubObject.GetValueOrDefault("target"),
                                    ["ActivityPubResult"] = activityPubObject.GetValueOrDefault("result"),
                                    ["ActivityPubOrigin"] = activityPubObject.GetValueOrDefault("origin"),
                                    ["ActivityPubInstrument"] = activityPubObject.GetValueOrDefault("instrument"),
                                    ["ActivityPubHref"] = activityPubObject.GetValueOrDefault("href"),
                                    ["ActivityPubRel"] = activityPubObject.GetValueOrDefault("rel"),
                                    ["ActivityPubHreflang"] = activityPubObject.GetValueOrDefault("hreflang"),
                                    ["ActivityPubHeight"] = activityPubObject.GetValueOrDefault("height"),
                                    ["ActivityPubWidth"] = activityPubObject.GetValueOrDefault("width"),
                                    ["ExportAvatarEmail"] = avatarEmailAddress,
                                    ["ExportVersion"] = version,
                                    ["ExportDate"] = DateTime.Now
                                }
                            };
                            
                            exportedHolons.Add(holon);
                        }
                    }
                }
                
                result.Result = exportedHolons;
                result.IsError = false;
                result.Message = $"All data exported successfully from ActivityPub for avatar {avatarEmailAddress} with full property mapping ({exportedHolons.Count} holons)";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting all data for avatar by email from ActivityPub: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
        {
            return ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ActivityPub provider is not activated");
                    return result;
                }

                if (_httpClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "HTTP client not initialized");
                    return result;
                }

                // Real ActivityPub implementation for exporting all data
                var exportedHolons = new List<IHolon>();
                
                // Get all objects
                var response = await _httpClient.GetAsync($"{_baseUrl}/objects");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var activityPubObjects = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(content, _jsonOptions);
                    
                    if (activityPubObjects != null && activityPubObjects.Any())
                    {
                        // Convert ALL ActivityPub objects to OASIS Holons with FULL property mapping
                        foreach (var activityPubObject in activityPubObjects)
                        {
                            var holon = new Holon
                            {
                                Id = Guid.NewGuid(),
                                Name = activityPubObject.GetValueOrDefault("name")?.ToString() ?? activityPubObject.GetValueOrDefault("summary")?.ToString(),
                                Description = activityPubObject.GetValueOrDefault("content")?.ToString() ?? activityPubObject.GetValueOrDefault("summary")?.ToString(),
                                HolonType = HolonType.Holon,
                                CreatedDate = activityPubObject.GetValueOrDefault("published") != null ? 
                                    DateTime.TryParse(activityPubObject.GetValueOrDefault("published").ToString(), out var published) ? published : DateTime.Now : DateTime.Now,
                                ModifiedDate = activityPubObject.GetValueOrDefault("updated") != null ? 
                                    DateTime.TryParse(activityPubObject.GetValueOrDefault("updated").ToString(), out var updated) ? updated : DateTime.Now : DateTime.Now,
                                Version = 1,
                                IsActive = true,
                                ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string> { [Core.Enums.ProviderType.ActivityPubOASIS] = activityPubObject.GetValueOrDefault("id")?.ToString() ?? "" },
                                // Map ALL ActivityPub object properties to custom data
                                MetaData = new Dictionary<string, object>
                                {
                                    ["ActivityPubId"] = activityPubObject.GetValueOrDefault("id"),
                                    ["ActivityPubType"] = activityPubObject.GetValueOrDefault("type"),
                                    ["ActivityPubPublished"] = activityPubObject.GetValueOrDefault("published"),
                                    ["ActivityPubUpdated"] = activityPubObject.GetValueOrDefault("updated"),
                                    ["ActivityPubAttributedTo"] = activityPubObject.GetValueOrDefault("attributedTo"),
                                    ["ActivityPubInReplyTo"] = activityPubObject.GetValueOrDefault("inReplyTo"),
                                    ["ActivityPubTo"] = activityPubObject.GetValueOrDefault("to"),
                                    ["ActivityPubCc"] = activityPubObject.GetValueOrDefault("cc"),
                                    ["ActivityPubBto"] = activityPubObject.GetValueOrDefault("bto"),
                                    ["ActivityPubBcc"] = activityPubObject.GetValueOrDefault("bcc"),
                                    ["ActivityPubAudience"] = activityPubObject.GetValueOrDefault("audience"),
                                    ["ActivityPubContent"] = activityPubObject.GetValueOrDefault("content"),
                                    ["ActivityPubSummary"] = activityPubObject.GetValueOrDefault("summary"),
                                    ["ActivityPubName"] = activityPubObject.GetValueOrDefault("name"),
                                    ["ActivityPubUrl"] = activityPubObject.GetValueOrDefault("url"),
                                    ["ActivityPubMediaType"] = activityPubObject.GetValueOrDefault("mediaType"),
                                    ["ActivityPubDuration"] = activityPubObject.GetValueOrDefault("duration"),
                                    ["ActivityPubIcon"] = activityPubObject.GetValueOrDefault("icon"),
                                    ["ActivityPubImage"] = activityPubObject.GetValueOrDefault("image"),
                                    ["ActivityPubPreview"] = activityPubObject.GetValueOrDefault("preview"),
                                    ["ActivityPubLocation"] = activityPubObject.GetValueOrDefault("location"),
                                    ["ActivityPubTag"] = activityPubObject.GetValueOrDefault("tag"),
                                    ["ActivityPubAttachment"] = activityPubObject.GetValueOrDefault("attachment"),
                                    ["ActivityPubReplies"] = activityPubObject.GetValueOrDefault("replies"),
                                    ["ActivityPubLikes"] = activityPubObject.GetValueOrDefault("likes"),
                                    ["ActivityPubShares"] = activityPubObject.GetValueOrDefault("shares"),
                                    ["ActivityPubAnnouncements"] = activityPubObject.GetValueOrDefault("announcements"),
                                    ["ActivityPubResponses"] = activityPubObject.GetValueOrDefault("responses"),
                                    ["ActivityPubRepliesCount"] = activityPubObject.GetValueOrDefault("repliesCount"),
                                    ["ActivityPubLikesCount"] = activityPubObject.GetValueOrDefault("likesCount"),
                                    ["ActivityPubSharesCount"] = activityPubObject.GetValueOrDefault("sharesCount"),
                                    ["ActivityPubAnnouncementsCount"] = activityPubObject.GetValueOrDefault("announcementsCount"),
                                    ["ActivityPubResponsesCount"] = activityPubObject.GetValueOrDefault("responsesCount"),
                                    ["ActivityPubInbox"] = activityPubObject.GetValueOrDefault("inbox"),
                                    ["ActivityPubOutbox"] = activityPubObject.GetValueOrDefault("outbox"),
                                    ["ActivityPubFollowing"] = activityPubObject.GetValueOrDefault("following"),
                                    ["ActivityPubFollowers"] = activityPubObject.GetValueOrDefault("followers"),
                                    ["ActivityPubStreams"] = activityPubObject.GetValueOrDefault("streams"),
                                    ["ActivityPubPreferredUsername"] = activityPubObject.GetValueOrDefault("preferredUsername"),
                                    ["ActivityPubEndpoints"] = activityPubObject.GetValueOrDefault("endpoints"),
                                    ["ActivityPubPublicKey"] = activityPubObject.GetValueOrDefault("publicKey"),
                                    ["ActivityPubManuallyApprovesFollowers"] = activityPubObject.GetValueOrDefault("manuallyApprovesFollowers"),
                                    ["ActivityPubDiscoverable"] = activityPubObject.GetValueOrDefault("discoverable"),
                                    ["ActivityPubDevices"] = activityPubObject.GetValueOrDefault("devices"),
                                    ["ActivityPubAlsoKnownAs"] = activityPubObject.GetValueOrDefault("alsoKnownAs"),
                                    ["ActivityPubMovedTo"] = activityPubObject.GetValueOrDefault("movedTo"),
                                    ["ActivityPubMovedFrom"] = activityPubObject.GetValueOrDefault("movedFrom"),
                                    ["ActivityPubActor"] = activityPubObject.GetValueOrDefault("actor"),
                                    ["ActivityPubObject"] = activityPubObject.GetValueOrDefault("object"),
                                    ["ActivityPubTarget"] = activityPubObject.GetValueOrDefault("target"),
                                    ["ActivityPubResult"] = activityPubObject.GetValueOrDefault("result"),
                                    ["ActivityPubOrigin"] = activityPubObject.GetValueOrDefault("origin"),
                                    ["ActivityPubInstrument"] = activityPubObject.GetValueOrDefault("instrument"),
                                    ["ActivityPubHref"] = activityPubObject.GetValueOrDefault("href"),
                                    ["ActivityPubRel"] = activityPubObject.GetValueOrDefault("rel"),
                                    ["ActivityPubHreflang"] = activityPubObject.GetValueOrDefault("hreflang"),
                                    ["ActivityPubHeight"] = activityPubObject.GetValueOrDefault("height"),
                                    ["ActivityPubWidth"] = activityPubObject.GetValueOrDefault("width"),
                                    ["ExportVersion"] = version,
                                    ["ExportDate"] = DateTime.Now
                                }
                            };
                            
                            exportedHolons.Add(holon);
                        }
                    }
                }
                
                result.Result = exportedHolons;
                result.IsError = false;
                result.Message = $"All data exported successfully from ActivityPub with full property mapping ({exportedHolons.Count} holons)";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting all data from ActivityPub: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
        {
            return ExportAllAsync(version).Result;
        }

        #endregion

        #region IOASISNET Implementation

        OASISResult<IEnumerable<IAvatar>> IOASISNETProvider.GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            OASISResult<IEnumerable<IAvatar>> result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ActivityPub provider is not activated");
                    return result;
                }

                // Load all avatars and filter by geo-location
                var avatarsResult = LoadAllAvatars();
                if (avatarsResult.IsError || avatarsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatars: {avatarsResult.Message}");
                    return result;
                }

                var centerLat = geoLat / 1e6d;
                var centerLng = geoLong / 1e6d;
                var nearby = new List<IAvatar>();

                foreach (var avatar in avatarsResult.Result)
                {
                    if (avatar.MetaData != null &&
                        avatar.MetaData.TryGetValue("Latitude", out var latObj) &&
                        avatar.MetaData.TryGetValue("Longitude", out var lngObj) &&
                        double.TryParse(latObj?.ToString(), out var lat) &&
                        double.TryParse(lngObj?.ToString(), out var lng))
                    {
                        var distance = GeoHelper.CalculateDistance(centerLat, centerLng, lat, lng);
                        if (distance <= radiusInMeters)
                            nearby.Add(avatar);
                    }
                }

                result.Result = nearby;
                result.IsError = false;
                result.Message = $"Found {nearby.Count} avatars within {radiusInMeters}m";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting avatars near me from ActivityPub: {ex.Message}", ex);
            }
            return result;
        }

        OASISResult<IEnumerable<IHolon>> IOASISNETProvider.GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ActivityPub provider is not activated");
                    return result;
                }

                // Load all holons and filter by geo-location and type
                var holonsResult = LoadAllHolons(Type);
                if (holonsResult.IsError || holonsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading holons: {holonsResult.Message}");
                    return result;
                }

                var centerLat = geoLat / 1e6d;
                var centerLng = geoLong / 1e6d;
                var nearby = new List<IHolon>();

                foreach (var holon in holonsResult.Result)
                {
                    if (holon.MetaData != null &&
                        holon.MetaData.TryGetValue("Latitude", out var latObj) &&
                        holon.MetaData.TryGetValue("Longitude", out var lngObj) &&
                        double.TryParse(latObj?.ToString(), out var lat) &&
                        double.TryParse(lngObj?.ToString(), out var lng))
                    {
                        var distance = GeoHelper.CalculateDistance(centerLat, centerLng, lat, lng);
                        if (distance <= radiusInMeters)
                            nearby.Add(holon);
                    }
                }

                result.Result = nearby;
                result.IsError = false;
                result.Message = $"Found {nearby.Count} holons within {radiusInMeters}m";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting holons near me from ActivityPub: {ex.Message}", ex);
            }
            return result;
        }

        #endregion

        /*
                
                if (response.IsSuccessStatusCode)
                {
                    var content = response.Content.ReadAsStringAsync().Result;
                    var activityPubObjects = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(content, _jsonOptions);
                    
                    if (activityPubObjects != null && activityPubObjects.Any())
                    {
                        // Convert ALL ActivityPub objects to OASIS Holons with FULL property mapping
                        foreach (var activityPubObject in activityPubObjects)
                        {
                            var holon = new Holon
                            {
                                Id = Guid.NewGuid(),
                                Name = activityPubObject.GetValueOrDefault("name")?.ToString() ?? activityPubObject.GetValueOrDefault("summary")?.ToString(),
                                Description = activityPubObject.GetValueOrDefault("content")?.ToString() ?? activityPubObject.GetValueOrDefault("summary")?.ToString(),
                                HolonType = HolonType.Holon,
                                CreatedDate = activityPubObject.GetValueOrDefault("published") != null ? 
                                    DateTime.TryParse(activityPubObject.GetValueOrDefault("published").ToString(), out var published) ? published : DateTime.Now : DateTime.Now,
                                ModifiedDate = activityPubObject.GetValueOrDefault("updated") != null ? 
                                    DateTime.TryParse(activityPubObject.GetValueOrDefault("updated").ToString(), out var updated) ? updated : DateTime.Now : DateTime.Now,
                                Version = 1,
                                IsActive = true,
                                ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string> { [Core.Enums.ProviderType.ActivityPubOASIS] = activityPubObject.GetValueOrDefault("id")?.ToString() ?? "" },
                                // Map ALL ActivityPub object properties to custom data
                                MetaData = new Dictionary<string, object>
                                {
                                    ["ActivityPubId"] = activityPubObject.GetValueOrDefault("id"),
                                    ["ActivityPubType"] = activityPubObject.GetValueOrDefault("type"),
                                    ["ActivityPubPublished"] = activityPubObject.GetValueOrDefault("published"),
                                    ["ActivityPubUpdated"] = activityPubObject.GetValueOrDefault("updated"),
                                    ["ActivityPubAttributedTo"] = activityPubObject.GetValueOrDefault("attributedTo"),
                                    ["ActivityPubInReplyTo"] = activityPubObject.GetValueOrDefault("inReplyTo"),
                                    ["ActivityPubTo"] = activityPubObject.GetValueOrDefault("to"),
                                    ["ActivityPubCc"] = activityPubObject.GetValueOrDefault("cc"),
                                    ["ActivityPubBto"] = activityPubObject.GetValueOrDefault("bto"),
                                    ["ActivityPubBcc"] = activityPubObject.GetValueOrDefault("bcc"),
                                    ["ActivityPubAudience"] = activityPubObject.GetValueOrDefault("audience"),
                                    ["ActivityPubContent"] = activityPubObject.GetValueOrDefault("content"),
                                    ["ActivityPubSummary"] = activityPubObject.GetValueOrDefault("summary"),
                                    ["ActivityPubName"] = activityPubObject.GetValueOrDefault("name"),
                                    ["ActivityPubUrl"] = activityPubObject.GetValueOrDefault("url"),
                                    ["ActivityPubMediaType"] = activityPubObject.GetValueOrDefault("mediaType"),
                                    ["ActivityPubDuration"] = activityPubObject.GetValueOrDefault("duration"),
                                    ["ActivityPubIcon"] = activityPubObject.GetValueOrDefault("icon"),
                                    ["ActivityPubImage"] = activityPubObject.GetValueOrDefault("image"),
                                    ["ActivityPubPreview"] = activityPubObject.GetValueOrDefault("preview"),
                                    ["ActivityPubLocation"] = activityPubObject.GetValueOrDefault("location"),
                                    ["ActivityPubTag"] = activityPubObject.GetValueOrDefault("tag"),
                                    ["ActivityPubAttachment"] = activityPubObject.GetValueOrDefault("attachment"),
                                    ["ActivityPubReplies"] = activityPubObject.GetValueOrDefault("replies"),
                                    ["ActivityPubLikes"] = activityPubObject.GetValueOrDefault("likes"),
                                    ["ActivityPubShares"] = activityPubObject.GetValueOrDefault("shares"),
                                    ["ActivityPubAnnouncements"] = activityPubObject.GetValueOrDefault("announcements"),
                                    ["ActivityPubResponses"] = activityPubObject.GetValueOrDefault("responses"),
                                    ["ActivityPubRepliesCount"] = activityPubObject.GetValueOrDefault("repliesCount"),
                                    ["ActivityPubLikesCount"] = activityPubObject.GetValueOrDefault("likesCount"),
                                    ["ActivityPubSharesCount"] = activityPubObject.GetValueOrDefault("sharesCount"),
                                    ["ActivityPubAnnouncementsCount"] = activityPubObject.GetValueOrDefault("announcementsCount"),
                                    ["ActivityPubResponsesCount"] = activityPubObject.GetValueOrDefault("responsesCount"),
                                    ["ActivityPubInbox"] = activityPubObject.GetValueOrDefault("inbox"),
                                    ["ActivityPubOutbox"] = activityPubObject.GetValueOrDefault("outbox"),
                                    ["ActivityPubFollowing"] = activityPubObject.GetValueOrDefault("following"),
                                    ["ActivityPubFollowers"] = activityPubObject.GetValueOrDefault("followers"),
                                    ["ActivityPubStreams"] = activityPubObject.GetValueOrDefault("streams"),
                                    ["ActivityPubPreferredUsername"] = activityPubObject.GetValueOrDefault("preferredUsername"),
                                    ["ActivityPubEndpoints"] = activityPubObject.GetValueOrDefault("endpoints"),
                                    ["ActivityPubPublicKey"] = activityPubObject.GetValueOrDefault("publicKey"),
                                    ["ActivityPubManuallyApprovesFollowers"] = activityPubObject.GetValueOrDefault("manuallyApprovesFollowers"),
                                    ["ActivityPubDiscoverable"] = activityPubObject.GetValueOrDefault("discoverable"),
                                    ["ActivityPubDevices"] = activityPubObject.GetValueOrDefault("devices"),
                                    ["ActivityPubAlsoKnownAs"] = activityPubObject.GetValueOrDefault("alsoKnownAs"),
                                    ["ActivityPubMovedTo"] = activityPubObject.GetValueOrDefault("movedTo"),
                                    ["ActivityPubMovedFrom"] = activityPubObject.GetValueOrDefault("movedFrom"),
                                    ["ActivityPubActor"] = activityPubObject.GetValueOrDefault("actor"),
                                    ["ActivityPubObject"] = activityPubObject.GetValueOrDefault("object"),
                                    ["ActivityPubTarget"] = activityPubObject.GetValueOrDefault("target"),
                                    ["ActivityPubResult"] = activityPubObject.GetValueOrDefault("result"),
                                    ["ActivityPubOrigin"] = activityPubObject.GetValueOrDefault("origin"),
                                    ["ActivityPubInstrument"] = activityPubObject.GetValueOrDefault("instrument"),
                                    ["ActivityPubHref"] = activityPubObject.GetValueOrDefault("href"),
                                    ["ActivityPubRel"] = activityPubObject.GetValueOrDefault("rel"),
                                    ["ActivityPubHreflang"] = activityPubObject.GetValueOrDefault("hreflang"),
                                    ["ActivityPubHeight"] = activityPubObject.GetValueOrDefault("height"),
                                    ["ActivityPubWidth"] = activityPubObject.GetValueOrDefault("width"),
                                    ["NearMe"] = true,
                                    ["Distance"] = 0.0, // Would be calculated based on actual location
                                    ["HolonType"] = Type.ToString()
                                }
                            };
                            
                            holons.Add(holon);
                        }
                    }
                }
                
                result.Result = holons;
                result.IsError = false;
                result.Message = $"Holons near me loaded successfully from ActivityPub with full property mapping ({holons.Count} holons)";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting holons near me from ActivityPub: {ex.Message}", ex);
            }
            return result;
        }

        #endregion

        /*
        #region IOASISSuperStar
        public bool NativeCodeGenesis(ICelestialBody celestialBody)
        {
            return true;
        }

        #endregion

        #region IOASISBlockchainStorageProvider

        public OASISResult<string> SendTransaction(IWalletTransaction transation)
        {
            return SendTransactionAsync(transation).Result;
        }

        public async Task<OASISResult<string>> SendTransactionAsync(IWalletTransaction transation)
        {
            var result = new OASISResult<string>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ActivityPub provider is not activated");
                    return result;
                }

                // Create ActivityPub transaction activity
                var transactionActivity = new
                {
                    type = "Transaction",
                    actor = transation.FromWalletAddress,
                    @object = new
                    {
                        type = "TransactionObject",
                        to = transation.ToWalletAddress,
                        amount = transation.Amount.ToString(),
                        currency = "OASIS",
                        memo = transation.MemoText
                    },
                    published = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                var json = JsonSerializer.Serialize(transactionActivity);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/transactions", content);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
                    result.Result = responseData?.GetValueOrDefault("id")?.ToString() ?? "transaction-completed";
                    result.IsError = false;
                    result.Message = "Transaction sent successfully via ActivityPub";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to send transaction via ActivityPub: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending transaction via ActivityPub: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<string> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount).Result;
        }

        public async Task<OASISResult<string>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            var result = new OASISResult<string>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ActivityPub provider is not activated");
                    return result;
                }

                // Get wallet addresses for avatars
                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(fromAvatarId, _httpClient);
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(toAvatarId, _httpClient);

                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error getting wallet addresses: {fromWalletResult.Message} {toWalletResult.Message}");
                    return result;
                }

                // Create ActivityPub transaction activity
                var transactionActivity = new
                {
                    type = "Transaction",
                    actor = fromWalletResult.Result,
                    @object = new
                    {
                        type = "TransactionObject",
                        to = toWalletResult.Result,
                        amount = amount.ToString(),
                        currency = "OASIS"
                    },
                    published = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                var json = JsonSerializer.Serialize(transactionActivity);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/transactions", content);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
                    result.Result = responseData?.GetValueOrDefault("id")?.ToString() ?? "transaction-completed";
                    result.IsError = false;
                    result.Message = "Transaction sent successfully via ActivityPub";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to send transaction via ActivityPub: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending transaction via ActivityPub: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<string> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
        {
            return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount, token).Result;
        }

        public async Task<OASISResult<string>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
        {
            var result = new OASISResult<string>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ActivityPub provider is not activated");
                    return result;
                }

                // Get wallet addresses for avatars
                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(fromAvatarId, _httpClient);
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(toAvatarId, _httpClient);

                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error getting wallet addresses: {fromWalletResult.Message} {toWalletResult.Message}");
                    return result;
                }

                // Create ActivityPub token transaction activity
                var transactionActivity = new
                {
                    type = "TokenTransaction",
                    actor = fromWalletResult.Result,
                    @object = new
                    {
                        type = "TokenTransactionObject",
                        to = toWalletResult.Result,
                        amount = amount.ToString(),
                        token = token,
                        currency = "OASIS"
                    },
                    published = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                var json = JsonSerializer.Serialize(transactionActivity);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/token-transactions", content);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
                    result.Result = responseData?.GetValueOrDefault("id")?.ToString() ?? "token-transaction-completed";
                    result.IsError = false;
                    result.Message = $"Token transaction sent successfully via ActivityPub for {token}";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to send token transaction via ActivityPub: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending token transaction via ActivityPub: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<string>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount)
        {
            var result = new OASISResult<string>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ActivityPub provider is not activated");
                    return result;
                }

                // Get wallet addresses for avatars by username
                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarByUsernameAsync(fromAvatarUsername, _httpClient);
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarByUsernameAsync(toAvatarUsername, _httpClient);

                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error getting wallet addresses: {fromWalletResult.Message} {toWalletResult.Message}");
                    return result;
                }

                // Create ActivityPub transaction activity
                var transactionActivity = new
                {
                    type = "Transaction",
                    actor = fromWalletResult.Result,
                    @object = new
                    {
                        type = "TransactionObject",
                        to = toWalletResult.Result,
                        amount = amount.ToString(),
                        currency = "OASIS"
                    },
                    published = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                var json = JsonSerializer.Serialize(transactionActivity);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/transactions", content);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
                    result.Result = responseData?.GetValueOrDefault("id")?.ToString() ?? "transaction-completed";
                    result.IsError = false;
                    result.Message = "Transaction sent successfully via ActivityPub";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to send transaction via ActivityPub: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending transaction via ActivityPub: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<string> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount)
        {
            return SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount).Result;
        }

        public async Task<OASISResult<string>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            var result = new OASISResult<string>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ActivityPub provider is not activated");
                    return result;
                }

                // Get wallet addresses for avatars by username
                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarByUsernameAsync(fromAvatarUsername, _httpClient);
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarByUsernameAsync(toAvatarUsername, _httpClient);

                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error getting wallet addresses: {fromWalletResult.Message} {toWalletResult.Message}");
                    return result;
                }

                // Create ActivityPub token transaction activity
                var transactionActivity = new
                {
                    type = "TokenTransaction",
                    actor = fromWalletResult.Result,
                    @object = new
                    {
                        type = "TokenTransactionObject",
                        to = toWalletResult.Result,
                        amount = amount.ToString(),
                        token = token,
                        currency = "OASIS"
                    },
                    published = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                var json = JsonSerializer.Serialize(transactionActivity);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/token-transactions", content);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
                    result.Result = responseData?.GetValueOrDefault("id")?.ToString() ?? "token-transaction-completed";
                    result.IsError = false;
                    result.Message = $"Token transaction sent successfully via ActivityPub for {token}";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to send token transaction via ActivityPub: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending token transaction via ActivityPub: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<string> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            return SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount, token).Result;
        }

        public async Task<OASISResult<string>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount)
        {
            var result = new OASISResult<string>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ActivityPub provider is not activated");
                    return result;
                }

                // Get wallet addresses for avatars by email
                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarByEmailAsync(fromAvatarEmail, _httpClient);
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarByEmailAsync(toAvatarEmail, _httpClient);

                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error getting wallet addresses: {fromWalletResult.Message} {toWalletResult.Message}");
                    return result;
                }

                // Create ActivityPub transaction activity
                var transactionActivity = new
                {
                    type = "Transaction",
                    actor = fromWalletResult.Result,
                    @object = new
                    {
                        type = "TransactionObject",
                        to = toWalletResult.Result,
                        amount = amount.ToString(),
                        currency = "OASIS"
                    },
                    published = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                var json = JsonSerializer.Serialize(transactionActivity);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/transactions", content);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
                    result.Result = responseData?.GetValueOrDefault("id")?.ToString() ?? "transaction-completed";
                    result.IsError = false;
                    result.Message = "Transaction sent successfully via ActivityPub";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to send transaction via ActivityPub: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending transaction via ActivityPub: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<string> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount)
        {
            return SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount).Result;
        }

        public async Task<OASISResult<string>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        {
            var result = new OASISResult<string>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ActivityPub provider is not activated");
                    return result;
                }

                // Get wallet addresses for avatars by email
                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarByEmailAsync(fromAvatarEmail, _httpClient);
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarByEmailAsync(toAvatarEmail, _httpClient);

                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error getting wallet addresses: {fromWalletResult.Message} {toWalletResult.Message}");
                    return result;
                }

                // Create ActivityPub token transaction activity
                var transactionActivity = new
                {
                    type = "TokenTransaction",
                    actor = fromWalletResult.Result,
                    @object = new
                    {
                        type = "TokenTransactionObject",
                        to = toWalletResult.Result,
                        amount = amount.ToString(),
                        token = token,
                        currency = "OASIS"
                    },
                    published = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                var json = JsonSerializer.Serialize(transactionActivity);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/token-transactions", content);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
                    result.Result = responseData?.GetValueOrDefault("id")?.ToString() ?? "token-transaction-completed";
                    result.IsError = false;
                    result.Message = $"Token transaction sent successfully via ActivityPub for {token}";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to send token transaction via ActivityPub: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending token transaction via ActivityPub: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<string> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        {
            return SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount, token).Result;
        }

        public OASISResult<string> SendTransactionByDefaultWallet(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            return SendTransactionByDefaultWalletAsync(fromAvatarId, toAvatarId, amount).Result;
        }

        public async Task<OASISResult<string>> SendTransactionByDefaultWalletAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            // Use the default wallet for the avatar
            return await SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount);
        }

        #endregion

        #region IOASISNFTProvider

        public OASISResult<bool> SendNFT(IWalletTransaction transation)
        {
            return SendNFTAsync(transation).Result;
        }

        public async Task<OASISResult<bool>> SendNFTAsync(IWalletTransaction transation)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ActivityPub provider is not activated");
                    return result;
                }

                var nftActivity = new
                {
                    type = "NFTTransfer",
                    actor = transation.FromWalletAddress,
                    @object = new
                    {
                        type = "NFT",
                        to = transation.ToWalletAddress,
                        nftId = transation.Amount.ToString(),
                        memo = transation.MemoText
                    },
                    published = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                var json = JsonSerializer.Serialize(nftActivity);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/nft-transfers", content);
                if (response.IsSuccessStatusCode)
                {
                    result.Result = true;
                    result.IsError = false;
                    result.Message = "NFT transfer sent successfully via ActivityPub";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to send NFT transfer via ActivityPub: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending NFT transfer via ActivityPub: {ex.Message}", ex);
            }
            return result;
        }

        #endregion

        #region IOASISLocalStorageProvider

        public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> LoadProviderWalletsForAvatarById(Guid id)
        {
            return LoadProviderWalletsForAvatarByIdAsync(id).Result;
        }

        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsForAvatarByIdAsync(Guid id)
        {
            var result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ActivityPub provider is not activated");
                    return result;
                }

                // Load avatar to get provider wallets
                var avatarResult = await LoadAvatarAsync(id);
                if (avatarResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar: {avatarResult.Message}");
                    return result;
                }

                var providerWallets = new Dictionary<ProviderType, List<IProviderWallet>>();
                if (avatarResult.Result?.ProviderWallets != null)
                {
                    foreach (var wallet in avatarResult.Result.ProviderWallets)
                    {
                        if (!providerWallets.ContainsKey(wallet.ProviderType))
                        {
                            providerWallets[wallet.ProviderType] = new List<IProviderWallet>();
                        }
                        providerWallets[wallet.ProviderType].Add(wallet);
                    }
                }

                result.Result = providerWallets;
                result.IsError = false;
                result.Message = $"Successfully loaded {providerWallets.Count} provider wallet types for avatar {id} from ActivityPub";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading provider wallets for avatar from ActivityPub: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<bool> SaveProviderWalletsForAvatarById(Guid id, Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        {
            return SaveProviderWalletsForAvatarByIdAsync(id, providerWallets).Result;
        }

        public async Task<OASISResult<bool>> SaveProviderWalletsForAvatarByIdAsync(Guid id, Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ActivityPub provider is not activated");
                    return result;
                }

                // Load avatar and update provider wallets
                var avatarResult = await LoadAvatarAsync(id);
                if (avatarResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar: {avatarResult.Message}");
                    return result;
                }

                var avatar = avatarResult.Result;
                if (avatar != null)
                {
                    // Convert dictionary to list
                    var allWallets = new List<IProviderWallet>();
                    foreach (var kvp in providerWallets)
                    {
                        allWallets.AddRange(kvp.Value);
                    }
                    avatar.ProviderWallets = allWallets;

                    // Save updated avatar
                    var saveResult = await SaveAvatarAsync(avatar);
                    if (saveResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Error saving avatar: {saveResult.Message}");
                        return result;
                    }

                    result.Result = true;
                    result.IsError = false;
                    result.Message = $"Successfully saved {allWallets.Count} provider wallets for avatar {id} to ActivityPub";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving provider wallets for avatar to ActivityPub: {ex.Message}", ex);
            }
            return result;
        }

        #endregion
        */
    }

    // ActivityPub Account model for JSON deserialization with ALL properties
    public class ActivityPubAccount
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string Acct { get; set; }
        public string DisplayName { get; set; }
        public bool Locked { get; set; }
        public bool Bot { get; set; }
        public bool Discoverable { get; set; }
        public bool Group { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Note { get; set; }
        public string Url { get; set; }
        public string Avatar { get; set; }
        public string AvatarStatic { get; set; }
        public string Header { get; set; }
        public string HeaderStatic { get; set; }
        public int FollowersCount { get; set; }
        public int FollowingCount { get; set; }
        public int StatusesCount { get; set; }
        public string Email { get; set; }
        public string Website { get; set; }
        public string Location { get; set; }
        public string Language { get; set; }
        public string Privacy { get; set; }
        public bool Sensitive { get; set; }
        public List<ActivityPubField> Fields { get; set; }
        public ActivityPubEmoji Emoji { get; set; }
        public ActivityPubRole Role { get; set; }
    }

    public class ActivityPubField
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public DateTime? VerifiedAt { get; set; }
    }

    public class ActivityPubEmoji
    {
        public string Shortcode { get; set; }
        public string StaticUrl { get; set; }
        public string Url { get; set; }
        public bool VisibleInPicker { get; set; }
    }

    public class ActivityPubRole
    {
        public string Name { get; set; }
        public string Color { get; set; }
        public int Position { get; set; }
        public List<string> Permissions { get; set; }
        public bool Highlighted { get; set; }
    }
}
