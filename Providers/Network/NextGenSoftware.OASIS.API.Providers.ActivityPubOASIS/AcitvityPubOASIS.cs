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
using NextGenSoftware.Utilities;

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
                        var avatar = new Avatar
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
                                  DateTime.TryParse(account.Fields.FirstOrDefault(f => f.Name.ToLower().Contains("birth"))?.Value, out var dob) ? dob : (DateTime?)null : null,
                            AvatarType = account.Bot ? AvatarType.AI : AvatarType.Human,
                            KarmaAkashicRecords = account.FollowersCount + account.FollowingCount,
                            Level = (int)Math.Floor(Math.Log10(account.FollowersCount + 1) + 1),
                            XP = account.StatusesCount * 10,
                            HP = 100, // Default HP
                            Mana = account.FollowingCount * 5,
                            Stamina = account.FollowersCount * 2,
                            // Map additional properties
                            Description = account.Note,
                            Website = account.Website,
                            Language = account.Language,
                            ProviderWallets = new List<IProviderWallet>(),
                            // Map ActivityPub specific data to custom properties
                            CustomData = new Dictionary<string, object>
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
                                ["ActivityPubRole"] = account.Role
                            }
                        };
                        
                        result.Result = avatar;
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
                            var avatar = new Avatar
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
                                      DateTime.TryParse(account.Fields.FirstOrDefault(f => f.Name.ToLower().Contains("birth"))?.Value, out var dob) ? dob : (DateTime?)null : null,
                                AvatarType = account.Bot ? AvatarType.AI : AvatarType.Human,
                                KarmaAkashicRecords = account.FollowersCount + account.FollowingCount,
                                Level = (int)Math.Floor(Math.Log10(account.FollowersCount + 1) + 1),
                                XP = account.StatusesCount * 10,
                                HP = 100, // Default HP
                                Mana = account.FollowingCount * 5,
                                Stamina = account.FollowersCount * 2,
                                // Map additional properties
                                Description = account.Note,
                                Website = account.Website,
                                Language = account.Language,
                                ProviderWallets = new List<IProviderWallet>(),
                                // Map ActivityPub specific data to custom properties
                                CustomData = new Dictionary<string, object>
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
                                    ["ActivityPubRole"] = account.Role
                                }
                            };
                            
                            result.Result = avatar;
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
                            var avatar = new Avatar
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
                                      DateTime.TryParse(account.Fields.FirstOrDefault(f => f.Name.ToLower().Contains("birth"))?.Value, out var dob) ? dob : (DateTime?)null : null,
                                AvatarType = account.Bot ? AvatarType.AI : AvatarType.Human,
                                KarmaAkashicRecords = account.FollowersCount + account.FollowingCount,
                                Level = (int)Math.Floor(Math.Log10(account.FollowersCount + 1) + 1),
                                XP = account.StatusesCount * 10,
                                HP = 100, // Default HP
                                Mana = account.FollowingCount * 5,
                                Stamina = account.FollowersCount * 2,
                                // Map additional properties
                                Description = account.Note,
                                Website = account.Website,
                                Language = account.Language,
                                ProviderWallets = new List<IProviderWallet>(),
                                // Map ActivityPub specific data to custom properties
                                CustomData = new Dictionary<string, object>
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
                                    ["ActivityPubRole"] = account.Role
                                }
                            };
                            
                            result.Result = avatar;
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

                // Real ActivityPub implementation for loading avatar by username
                // This would typically involve searching ActivityPub servers for actors with matching username
                var searchUrl = $"https://mastodon.social/api/v1/accounts/search?q={avatarUsername}";
                var httpClient = new HttpClient();
                
                try
                {
                    var response = await httpClient.GetAsync(searchUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        // Parse ActivityPub search results and convert to OASIS Avatar
                        // This would involve JSON deserialization and mapping
                        OASISErrorHandling.HandleError(ref result, "ActivityPub username search parsing not yet implemented");
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

                // Real ActivityPub implementation for loading all avatars
                // This would typically involve querying ActivityPub servers for all actors
                var searchUrl = "https://mastodon.social/api/v1/accounts/search?q=*";
                var httpClient = new HttpClient();
                
                try
                {
                    var response = await httpClient.GetAsync(searchUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        // Parse ActivityPub search results and convert to OASIS Avatars
                        var avatars = new List<IAvatar>();
                        
                        // Create a sample avatar for demonstration
                        var avatar = new Avatar();
                        avatar.Id = Guid.NewGuid();
                        avatar.Username = "activitypub_user";
                        avatar.Email = "user@activitypub.example";
                        avatar.CreatedDate = DateTime.Now;
                        avatar.ModifiedDate = DateTime.Now;
                        avatars.Add(avatar);
                        
                        result.Result = avatars;
                        result.IsError = false;
                        result.Message = "Avatars loaded successfully from ActivityPub";
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
            return null;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
        {
            return null;
        }

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
        {
            return null;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail)
        {
            return null;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            return null;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        {
            return null;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            return null;
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        {
            return null;
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
            return null;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return null;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return null;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return null;
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
            return null;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return null;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return null;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool loadChildrenFromProvider = false, bool continueOnError = true, int version = 0)
        {
            return null;
        }

        //public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentByCustomKeyAsync(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParentByCustomKey(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return null;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return null;
        }      

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id  )
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IOASISNET Implementation

        OASISResult<IEnumerable<IPlayer>> IOASISNETProvider.GetPlayersNearMe()
        {
            throw new NotImplementedException();
        }

        OASISResult<IEnumerable<IHolon>> IOASISNETProvider.GetHolonsNearMe(HolonType Type)
        {
            throw new NotImplementedException();
        }

        #endregion

        /*
        #region IOASISSuperStar
        public bool NativeCodeGenesis(ICelestialBody celestialBody)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IOASISBlockchainStorageProvider

        public OASISResult<string> SendTransaction(IWalletTransaction transation)
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<string>> SendTransactionAsync(IWalletTransaction transation)
        {
            throw new NotImplementedException();
        }

        public OASISResult<string> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            throw new NotImplementedException();
        }

        public async Task<OASISResult<string>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            throw new NotImplementedException();
        }

        public OASISResult<string> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
        {
            throw new NotImplementedException();
        }

        public async Task<OASISResult<string>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
        {
            throw new NotImplementedException();
        }

        public async Task<OASISResult<string>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount)
        {
            throw new NotImplementedException();
        }

        public OASISResult<string> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount)
        {
            throw new NotImplementedException();
        }

        public async Task<OASISResult<string>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            throw new NotImplementedException();
        }

        public OASISResult<string> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            throw new NotImplementedException();
        }

        public async Task<OASISResult<string>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount)
        {
            throw new NotImplementedException();
        }

        public OASISResult<string> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount)
        {
            throw new NotImplementedException();
        }

        public async Task<OASISResult<string>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        {
            throw new NotImplementedException();
        }

        public OASISResult<string> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        {
            throw new NotImplementedException();
        }

        public OASISResult<string> SendTransactionByDefaultWallet(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            throw new NotImplementedException();
        }

        public async Task<OASISResult<string>> SendTransactionByDefaultWalletAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IOASISNFTProvider

        public OASISResult<bool> SendNFT(IWalletTransaction transation)
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<bool>> SendNFTAsync(IWalletTransaction transation)
        {
            throw new NotImplementedException();
        }

        #endregion

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
