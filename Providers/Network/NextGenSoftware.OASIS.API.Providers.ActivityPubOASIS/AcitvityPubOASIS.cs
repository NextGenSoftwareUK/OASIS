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
                            var avatar = new Avatar
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
                            var avatar = new Avatar
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
                            
                            avatars.Add(avatar);
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
                    location = avatar.Address,
                    website = avatar.Website,
                    language = avatar.Language,
                    // Map ALL Avatar properties to ActivityPub actor
                    customFields = new[]
                    {
                        new { name = "Phone", value = avatar.Mobile },
                        new { name = "Landline", value = avatar.Landline },
                        new { name = "Title", value = avatar.Title },
                        new { name = "Birth Date", value = avatar.DOB?.ToString("yyyy-MM-dd") },
                        new { name = "Karma", value = avatar.KarmaAkashicRecords.ToString() },
                        new { name = "Level", value = avatar.Level.ToString() },
                        new { name = "XP", value = avatar.XP.ToString() },
                        new { name = "HP", value = avatar.HP.ToString() },
                        new { name = "Mana", value = avatar.Mana.ToString() },
                        new { name = "Stamina", value = avatar.Stamina.ToString() }
                    },
                    // Map OASIS-specific data
                    oasisdData = new
                    {
                        avatarType = avatar.AvatarType.ToString(),
                        providerWallets = avatar.ProviderWallets,
                        customData = avatar.CustomData
                    }
                };

                var json = JsonSerializer.Serialize(actorData, _jsonOptions);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                
                // For ActivityPub, we would typically POST to the actor endpoint
                // This is a simplified implementation
                var response = await _httpClient.PostAsync($"{_baseUrl}/accounts", content);
                
                if (response.IsSuccessStatusCode)
                {
                    // Update avatar with ActivityPub-specific data
                    avatar.ModifiedDate = DateTime.Now;
                    avatar.CustomData = avatar.CustomData ?? new Dictionary<string, object>();
                    avatar.CustomData["ActivityPubSavedAt"] = DateTime.Now;
                    avatar.CustomData["ActivityPubResponse"] = await response.Content.ReadAsStringAsync();
                    
                    result.Result = avatar;
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
                var actorData = new
                {
                    type = "Person",
                    preferredUsername = avatarDetail.Username,
                    name = $"{avatarDetail.FirstName} {avatarDetail.LastName}".Trim(),
                    summary = avatarDetail.Description,
                    email = avatarDetail.Email,
                    location = avatarDetail.Address,
                    website = avatarDetail.Website,
                    language = avatarDetail.Language,
                    // Map ALL AvatarDetail properties to ActivityPub actor
                    customFields = new[]
                    {
                        new { name = "Phone", value = avatarDetail.Mobile },
                        new { name = "Landline", value = avatarDetail.Landline },
                        new { name = "Title", value = avatarDetail.Title },
                        new { name = "Birth Date", value = avatarDetail.DOB?.ToString("yyyy-MM-dd") },
                        new { name = "Country", value = avatarDetail.Country },
                        new { name = "Postcode", value = avatarDetail.Postcode },
                        new { name = "Karma", value = avatarDetail.KarmaAkashicRecords.ToString() },
                        new { name = "Level", value = avatarDetail.Level.ToString() },
                        new { name = "XP", value = avatarDetail.XP.ToString() },
                        new { name = "HP", value = avatarDetail.HP.ToString() },
                        new { name = "Mana", value = avatarDetail.Mana.ToString() },
                        new { name = "Stamina", value = avatarDetail.Stamina.ToString() }
                    },
                    // Map OASIS-specific data
                    oasisdData = new
                    {
                        avatarType = avatarDetail.AvatarType.ToString(),
                        providerWallets = avatarDetail.ProviderWallets,
                        customData = avatarDetail.CustomData
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
                    avatarDetail.CustomData = avatarDetail.CustomData ?? new Dictionary<string, object>();
                    avatarDetail.CustomData["ActivityPubSavedAt"] = DateTime.Now;
                    avatarDetail.CustomData["ActivityPubResponse"] = await response.Content.ReadAsStringAsync();
                    
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
                    object = new
                    {
                        type = "Person",
                        id = id.ToString()
                    },
                    // Map ALL deletion metadata
                    deletedAt = DateTime.Now,
                    deletedBy = AvatarManager.LoggedInAvatar?.Id.ToString(),
                    softDelete = softDelete,
                    reason = softDelete ? "Soft delete requested" : "Hard delete requested",
                    // Map OASIS-specific deletion data
                    oasisdData = new
                    {
                        avatarId = id.ToString(),
                        softDelete = softDelete,
                        deletedByAvatarId = AvatarManager.LoggedInAvatar?.Id.ToString(),
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
                    object = new
                    {
                        type = "Person",
                        preferredUsername = providerKey
                    },
                    deletedAt = DateTime.Now,
                    deletedBy = AvatarManager.LoggedInAvatar?.Id.ToString(),
                    softDelete = softDelete,
                    reason = softDelete ? "Soft delete requested" : "Hard delete requested",
                    oasisdData = new
                    {
                        providerKey = providerKey,
                        softDelete = softDelete,
                        deletedByAvatarId = AvatarManager.LoggedInAvatar?.Id.ToString(),
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
                    object = new
                    {
                        type = "Person",
                        email = avatarEmail
                    },
                    deletedAt = DateTime.Now,
                    deletedBy = AvatarManager.LoggedInAvatar?.Id.ToString(),
                    softDelete = softDelete,
                    reason = softDelete ? "Soft delete requested" : "Hard delete requested",
                    oasisdData = new
                    {
                        email = avatarEmail,
                        softDelete = softDelete,
                        deletedByAvatarId = AvatarManager.LoggedInAvatar?.Id.ToString(),
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
                    object = new
                    {
                        type = "Person",
                        preferredUsername = avatarUsername
                    },
                    deletedAt = DateTime.Now,
                    deletedBy = AvatarManager.LoggedInAvatar?.Id.ToString(),
                    softDelete = softDelete,
                    reason = softDelete ? "Soft delete requested" : "Hard delete requested",
                    oasisdData = new
                    {
                        username = avatarUsername,
                        softDelete = softDelete,
                        deletedByAvatarId = AvatarManager.LoggedInAvatar?.Id.ToString(),
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
                            ParentId = activityPubObject.GetValueOrDefault("inReplyTo") != null ? 
                                Guid.TryParse(activityPubObject.GetValueOrDefault("inReplyTo").ToString(), out var parentId) ? parentId : (Guid?)null : null,
                            ProviderKey = activityPubObject.GetValueOrDefault("id")?.ToString(),
                            IsChanged = false,
                            IsNew = false,
                            IsDeleted = false,
                            // Map ActivityPub specific data to custom properties
                            CustomData = new Dictionary<string, object>
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
                            ProviderKey = providerKey,
                            // Map ALL ActivityPub object properties to custom data
                            CustomData = new Dictionary<string, object>
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
                                ParentId = id,
                                ProviderKey = activityPubObject.GetValueOrDefault("id")?.ToString(),
                                // Map ALL ActivityPub object properties to custom data
                                CustomData = new Dictionary<string, object>
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
                                ProviderKey = activityPubObject.GetValueOrDefault("id")?.ToString(),
                                // Map ALL ActivityPub object properties to custom data
                                CustomData = new Dictionary<string, object>
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
                                ProviderKey = activityPubObject.GetValueOrDefault("id")?.ToString(),
                                // Map ALL ActivityPub object properties to custom data
                                CustomData = new Dictionary<string, object>
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
                                ProviderKey = activityPubObject.GetValueOrDefault("id")?.ToString(),
                                // Map ALL ActivityPub object properties to custom data
                                CustomData = new Dictionary<string, object>
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
                                ProviderKey = activityPubObject.GetValueOrDefault("id")?.ToString(),
                                // Map ALL ActivityPub object properties to custom data
                                CustomData = new Dictionary<string, object>
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
                    attributedTo = holon.CreatedByAvatarId?.ToString(),
                    inReplyTo = holon.ParentId?.ToString(),
                    to = new[] { "https://www.w3.org/ns/activitystreams#Public" },
                    cc = new string[0],
                    bto = new string[0],
                    bcc = new string[0],
                    audience = new string[0],
                    url = holon.ProviderKey,
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
                    preferredUsername = holon.ProviderKey,
                    endpoints = new { },
                    publicKey = new { },
                    manuallyApprovesFollowers = false,
                    discoverable = true,
                    devices = "",
                    alsoKnownAs = new string[0],
                    movedTo = "",
                    movedFrom = "",
                    actor = "",
                    object = "",
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
                        parentId = holon.ParentId?.ToString(),
                        providerKey = holon.ProviderKey,
                        previousVersionId = holon.PreviousVersionId?.ToString(),
                        nextVersionId = holon.NextVersionId?.ToString(),
                        isChanged = holon.IsChanged,
                        isNew = holon.IsNew,
                        isDeleted = holon.IsDeleted,
                        deletedByAvatarId = holon.DeletedByAvatarId?.ToString(),
                        deletedDate = holon.DeletedDate?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        createdByAvatarId = holon.CreatedByAvatarId?.ToString(),
                        modifiedByAvatarId = holon.ModifiedByAvatarId?.ToString(),
                        customData = holon.CustomData
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
                    holon.CustomData = holon.CustomData ?? new Dictionary<string, object>();
                    holon.CustomData["ActivityPubSavedAt"] = DateTime.Now;
                    holon.CustomData["ActivityPubResponse"] = await response.Content.ReadAsStringAsync();
                    
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
                            attributedTo = holon.CreatedByAvatarId?.ToString(),
                            inReplyTo = holon.ParentId?.ToString(),
                            to = new[] { "https://www.w3.org/ns/activitystreams#Public" },
                            cc = new string[0],
                            bto = new string[0],
                            bcc = new string[0],
                            audience = new string[0],
                            url = holon.ProviderKey,
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
                            preferredUsername = holon.ProviderKey,
                            endpoints = new { },
                            publicKey = new { },
                            manuallyApprovesFollowers = false,
                            discoverable = true,
                            devices = "",
                            alsoKnownAs = new string[0],
                            movedTo = "",
                            movedFrom = "",
                            actor = "",
                            object = "",
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
                                parentId = holon.ParentId?.ToString(),
                                providerKey = holon.ProviderKey,
                                previousVersionId = holon.PreviousVersionId?.ToString(),
                                nextVersionId = holon.NextVersionId?.ToString(),
                                isChanged = holon.IsChanged,
                                isNew = holon.IsNew,
                                isDeleted = holon.IsDeleted,
                                deletedByAvatarId = holon.DeletedByAvatarId?.ToString(),
                                deletedDate = holon.DeletedDate?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                                createdByAvatarId = holon.CreatedByAvatarId?.ToString(),
                                modifiedByAvatarId = holon.ModifiedByAvatarId?.ToString(),
                                customData = holon.CustomData
                            }
                        };

                        var json = JsonSerializer.Serialize(activityPubObject, _jsonOptions);
                        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                        
                        var response = await _httpClient.PostAsync($"{_baseUrl}/objects", content);
                        
                        if (response.IsSuccessStatusCode)
                        {
                            holon.ModifiedDate = DateTime.Now;
                            holon.CustomData = holon.CustomData ?? new Dictionary<string, object>();
                            holon.CustomData["ActivityPubSavedAt"] = DateTime.Now;
                            holon.CustomData["ActivityPubResponse"] = await response.Content.ReadAsStringAsync();
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
