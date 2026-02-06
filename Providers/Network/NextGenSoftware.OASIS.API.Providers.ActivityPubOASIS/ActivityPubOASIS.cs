using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS
{
    /// <summary>
    /// ActivityPub Provider for OASIS
    /// Implements the ActivityPub protocol for federated social networks (Mastodon, Pleroma, etc.)
    /// </summary>
    public class ActivityPubOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider
    {
        private readonly HttpClient _httpClient;
        private readonly string _instanceUrl;
        private readonly string _accessToken;
        private bool _isActivated;

        /// <summary>
        /// Initializes a new instance of the ActivityPubOASIS provider
        /// </summary>
        /// <param name="instanceUrl">URL of the ActivityPub instance (e.g., https://mastodon.social)</param>
        /// <param name="accessToken">OAuth access token for API authentication</param>
        public ActivityPubOASIS(string instanceUrl = "https://mastodon.social", string accessToken = "")
        {
            this.ProviderName = "ActivityPubOASIS";
            this.ProviderDescription = "ActivityPub Provider - Federated social network protocol";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.ActivityPubOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);

            _instanceUrl = instanceUrl ?? throw new ArgumentNullException(nameof(instanceUrl));
            _accessToken = accessToken;
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_instanceUrl)
            };

            if (!string.IsNullOrEmpty(_accessToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
            }
        }

        #region IOASISStorageProvider Implementation

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var response = new OASISResult<bool>();

            try
            {
                if (_isActivated)
                {
                    response.Result = true;
                    response.Message = "ActivityPub provider is already activated";
                    return response;
                }

                // Test connection to ActivityPub instance
                var testResponse = await _httpClient.GetAsync("/api/v1/instance");
                if (testResponse.IsSuccessStatusCode)
                {
                    _isActivated = true;
                    response.Result = true;
                    response.Message = "ActivityPub provider activated successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to connect to ActivityPub instance: {testResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error activating ActivityPub provider: {ex.Message}");
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
                _isActivated = false;
                _httpClient?.Dispose();
                response.Result = true;
                response.Message = "ActivityPub provider deactivated successfully";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error deactivating ActivityPub provider: {ex.Message}");
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
                if (!_isActivated)
                {
                    var activateResult = ActivateProviderAsync().GetAwaiter().GetResult();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate ActivityPub provider: {activateResult.Message}");
                        return response;
                    }
                }

                // ActivityPub doesn't have direct avatar concept, but we can get account info
                // This would need to be mapped from ActivityPub account to OASIS avatar
                // Load avatar from ActivityPub instance
                var apiUrl = $"{_instanceUrl}/api/v1/accounts/{id}";
                
                var httpResponse = await _httpClient.GetAsync(apiUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    // Parse ActivityPub JSON and create Avatar object
                    // Parse ActivityPub JSON and create Avatar object
                    var avatar = ParseActivityPubToAvatar(content);
                    if (avatar != null)
                    {
                        response.Result = (IAvatar)avatar;
                        response.IsError = false;
                        response.Message = "Avatar loaded from ActivityPub successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse ActivityPub JSON response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar from ActivityPub instance: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar from ActivityPub: {ex.Message}");
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
                if (!_isActivated)
                {
                    var activateResult = ActivateProviderAsync().GetAwaiter().GetResult();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate ActivityPub provider: {activateResult.Message}");
                        return response;
                    }
                }

                // ActivityPub account lookup by username/domain
                // Load avatar by provider key from ActivityPub instance
                var apiUrl = $"{_instanceUrl}/api/v1/accounts/lookup?acct={providerKey}";
                
                var httpResponse = await _httpClient.GetAsync(apiUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    // Parse ActivityPub JSON and create Avatar object
                    // Parse ActivityPub JSON and create Avatar object
                    var avatar = ParseActivityPubToAvatar(content);
                    if (avatar != null)
                    {
                        response.Result = (IAvatar)avatar;
                        response.IsError = false;
                        response.Message = "Avatar loaded from ActivityPub successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse ActivityPub JSON response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar by provider key from ActivityPub instance: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by provider key from ActivityPub: {ex.Message}");
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
                if (!_isActivated)
                {
                    var activateResult = ActivateProviderAsync().GetAwaiter().GetResult();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate ActivityPub provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Load avatar by email from ActivityPub instance
                var apiUrl = $"{_instanceUrl}/api/v1/accounts/search?q={avatarEmail}";
                
                var httpResponse = await _httpClient.GetAsync(apiUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    // Parse ActivityPub JSON and create Avatar object
                    // Parse ActivityPub JSON and create Avatar object
                    var avatar = ParseActivityPubToAvatar(content);
                    if (avatar != null)
                    {
                        response.Result = (IAvatar)avatar;
                        response.IsError = false;
                        response.Message = "Avatar loaded from ActivityPub successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse ActivityPub JSON response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar by email from ActivityPub instance: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by email from ActivityPub: {ex.Message}");
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
                if (!_isActivated)
                {
                    var activateResult = ActivateProviderAsync().GetAwaiter().GetResult();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate ActivityPub provider: {activateResult.Message}");
                        return response;
                    }
                }

                // ActivityPub account lookup by username
                // Load avatar by username from ActivityPub instance
                var apiUrl = $"{_instanceUrl}/api/v1/accounts/lookup?acct={avatarUsername}";
                
                var httpResponse = await _httpClient.GetAsync(apiUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    // Parse ActivityPub JSON and create Avatar object
                    // Parse ActivityPub JSON and create Avatar object
                    var avatar = ParseActivityPubToAvatar(content);
                    if (avatar != null)
                    {
                        response.Result = (IAvatar)avatar;
                        response.IsError = false;
                        response.Message = "Avatar loaded from ActivityPub successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse ActivityPub JSON response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar by username from ActivityPub instance: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by username from ActivityPub: {ex.Message}");
            }

            return response;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var response = new OASISResult<IAvatarDetail>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = ActivateProviderAsync().GetAwaiter().GetResult();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate ActivityPub provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Load avatar detail from ActivityPub instance
                var apiUrl = $"{_instanceUrl}/api/v1/accounts/{id}";
                
                var httpResponse = await _httpClient.GetAsync(apiUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    // Parse ActivityPub JSON and create AvatarDetail object
                    // Parse ActivityPub JSON and create Avatar object
                    var avatar = ParseActivityPubToAvatar(content);
                    if (avatar != null)
                    {
                        // Convert Avatar to AvatarDetail
                        var avatarDetail = new AvatarDetail
                        {
                            Id = avatar.Id,
                            Username = avatar.Username,
                            Email = avatar.Email,
                            FirstName = avatar.Username, // Use username as first name for ActivityPub
                            LastName = "", // ActivityPub doesn't have separate last name
                            CreatedDate = avatar.CreatedDate,
                            ModifiedDate = avatar.ModifiedDate,
                            AvatarType = avatar.AvatarType,
                            Description = avatar.Username, // Use username as description
                            MetaData = avatar.MetaData
                        };
                        
                        response.Result = avatarDetail;
                        response.IsError = false;
                        response.Message = "Avatar loaded from ActivityPub successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse ActivityPub JSON response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar detail from ActivityPub instance: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar detail from ActivityPub: {ex.Message}");
            }

            return response;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            return LoadAvatarDetailAsync(id, version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        {
            var response = new OASISResult<IAvatarDetail>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = ActivateProviderAsync().GetAwaiter().GetResult();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate ActivityPub provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Load avatar detail by email from ActivityPub instance
                var apiUrl = $"{_instanceUrl}/api/v1/accounts/search?q={avatarEmail}";
                
                var httpResponse = await _httpClient.GetAsync(apiUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    // Parse ActivityPub JSON and create AvatarDetail object
                    // Parse ActivityPub JSON and create Avatar object
                    var avatar = ParseActivityPubToAvatar(content);
                    if (avatar != null)
                    {
                        // Convert Avatar to AvatarDetail
                        var avatarDetail = new AvatarDetail
                        {
                            Id = avatar.Id,
                            Username = avatar.Username,
                            Email = avatar.Email,
                            FirstName = avatar.Username, // Use username as first name for ActivityPub
                            LastName = "", // ActivityPub doesn't have separate last name
                            CreatedDate = avatar.CreatedDate,
                            ModifiedDate = avatar.ModifiedDate,
                            AvatarType = avatar.AvatarType,
                            Description = avatar.Username, // Use username as description
                            MetaData = avatar.MetaData
                        };
                        
                        response.Result = avatarDetail;
                        response.IsError = false;
                        response.Message = "Avatar loaded from ActivityPub successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse ActivityPub JSON response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar detail by email from ActivityPub instance: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar detail by email from ActivityPub: {ex.Message}");
            }

            return response;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
        {
            return LoadAvatarDetailByEmailAsync(avatarEmail, version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        {
            var response = new OASISResult<IAvatarDetail>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = ActivateProviderAsync().GetAwaiter().GetResult();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate ActivityPub provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Load avatar detail by username from ActivityPub instance
                var apiUrl = $"{_instanceUrl}/api/v1/accounts/lookup?acct={avatarUsername}";
                
                var httpResponse = await _httpClient.GetAsync(apiUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    // Parse ActivityPub JSON and create AvatarDetail object
                    // Parse ActivityPub JSON and create Avatar object
                    var avatar = ParseActivityPubToAvatar(content);
                    if (avatar != null)
                    {
                        // Convert Avatar to AvatarDetail
                        var avatarDetail = new AvatarDetail
                        {
                            Id = avatar.Id,
                            Username = avatar.Username,
                            Email = avatar.Email,
                            FirstName = avatar.Username, // Use username as first name for ActivityPub
                            LastName = "", // ActivityPub doesn't have separate last name
                            CreatedDate = avatar.CreatedDate,
                            ModifiedDate = avatar.ModifiedDate,
                            AvatarType = avatar.AvatarType,
                            Description = avatar.Username, // Use username as description
                            MetaData = avatar.MetaData
                        };
                        
                        response.Result = avatarDetail;
                        response.IsError = false;
                        response.Message = "Avatar loaded from ActivityPub successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse ActivityPub JSON response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar detail by username from ActivityPub instance: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar detail by username from ActivityPub: {ex.Message}");
            }

            return response;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarDetailByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var response = new OASISResult<IEnumerable<IAvatar>>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = ActivateProviderAsync().GetAwaiter().GetResult();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate ActivityPub provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Load all avatars from ActivityPub instance
                var apiUrl = $"{_instanceUrl}/api/v1/accounts";
                
                var httpResponse = await _httpClient.GetAsync(apiUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    // Parse ActivityPub JSON and create Avatar collection
                    // Parse ActivityPub JSON and create Avatar object
                    var avatar = ParseActivityPubToAvatar(content);
                    if (avatar != null)
                    {
                        // Convert single IAvatar to IEnumerable<IAvatar>
                        response.Result = new List<IAvatar> { avatar };
                        response.Message = "Avatar loaded from ActivityPub successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse ActivityPub JSON response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load all avatars from ActivityPub instance: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading all avatars from ActivityPub: {ex.Message}");
            }

            return response;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            return LoadAllAvatarsAsync(version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var response = new OASISResult<IEnumerable<IAvatarDetail>>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = ActivateProviderAsync().GetAwaiter().GetResult();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate ActivityPub provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Load all avatar details from ActivityPub instance
                var apiUrl = $"{_instanceUrl}/api/v1/accounts";
                
                var httpResponse = await _httpClient.GetAsync(apiUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    // Parse ActivityPub JSON and create AvatarDetail collection
                    // Parse ActivityPub JSON and create Avatar object
                    var avatar = ParseActivityPubToAvatar(content);
                    if (avatar != null)
                    {
                        response.Result = new List<IAvatarDetail> { (IAvatarDetail)avatar };
                        response.IsError = false;
                        response.Message = "Avatar loaded from ActivityPub successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse ActivityPub JSON response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load all avatar details from ActivityPub instance: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading all avatar details from ActivityPub: {ex.Message}");
            }

            return response;
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            return LoadAllAvatarDetailsAsync(version).Result;
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var response = new OASISResult<IAvatar>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = ActivateProviderAsync().GetAwaiter().GetResult();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate ActivityPub provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Save avatar to ActivityPub instance
                var apiUrl = $"{_instanceUrl}/api/v1/accounts";
                var activityPubJson = ConvertAvatarToActivityPub(avatar);
                
                var content = new StringContent(activityPubJson, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync(apiUrl, content);
                
                if (httpResponse.IsSuccessStatusCode)
                {
                    response.Result = avatar;
                    response.Message = "Avatar saved to ActivityPub instance successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to save avatar to ActivityPub instance: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error saving avatar to ActivityPub: {ex.Message}");
            }

            return response;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
        {
            return SaveAvatarAsync(avatar).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
        {
            var response = new OASISResult<IAvatarDetail>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = ActivateProviderAsync().GetAwaiter().GetResult();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate ActivityPub provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Save avatar detail to ActivityPub instance
                var apiUrl = $"{_instanceUrl}/api/v1/accounts";
                var activityPubJson = ConvertAvatarDetailToActivityPub(avatarDetail);
                
                var content = new StringContent(activityPubJson, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync(apiUrl, content);
                
                if (httpResponse.IsSuccessStatusCode)
                {
                    response.Result = avatarDetail;
                    response.Message = "Avatar detail saved to ActivityPub instance successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to save avatar detail to ActivityPub instance: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error saving avatar detail to ActivityPub: {ex.Message}");
            }

            return response;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail)
        {
            return SaveAvatarDetailAsync(avatarDetail).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var response = new OASISResult<bool>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = ActivateProviderAsync().GetAwaiter().GetResult();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate ActivityPub provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Delete avatar from ActivityPub instance
                var apiUrl = $"{_instanceUrl}/api/v1/accounts/{id}";
                
                var httpResponse = await _httpClient.DeleteAsync(apiUrl);
                
                if (httpResponse.IsSuccessStatusCode)
                {
                    response.Result = true;
                    response.Message = "Avatar deleted from ActivityPub instance successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to delete avatar from ActivityPub instance: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error deleting avatar from ActivityPub: {ex.Message}");
            }

            return response;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        {
            return DeleteAvatarAsync(id, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            var response = new OASISResult<bool>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = ActivateProviderAsync().GetAwaiter().GetResult();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate ActivityPub provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Delete avatar by provider key from ActivityPub instance
                var apiUrl = $"{_instanceUrl}/api/v1/accounts/{providerKey}";
                
                var httpResponse = await _httpClient.DeleteAsync(apiUrl);
                
                if (httpResponse.IsSuccessStatusCode)
                {
                    response.Result = true;
                    response.Message = "Avatar deleted from ActivityPub instance successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to delete avatar by provider key from ActivityPub instance: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error deleting avatar by provider key from ActivityPub: {ex.Message}");
            }

            return response;
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        {
            return DeleteAvatarAsync(providerKey, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            var response = new OASISResult<bool>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = ActivateProviderAsync().GetAwaiter().GetResult();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate ActivityPub provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Delete avatar by email from ActivityPub instance
                var apiUrl = $"{_instanceUrl}/api/v1/accounts/search?q={avatarEmail}";
                
                var httpResponse = await _httpClient.DeleteAsync(apiUrl);
                
                if (httpResponse.IsSuccessStatusCode)
                {
                    response.Result = true;
                    response.Message = "Avatar deleted from ActivityPub instance successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to delete avatar by email from ActivityPub instance: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error deleting avatar by email from ActivityPub: {ex.Message}");
            }

            return response;
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
        {
            return DeleteAvatarByEmailAsync(avatarEmail, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            var response = new OASISResult<bool>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = ActivateProviderAsync().GetAwaiter().GetResult();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate ActivityPub provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Delete avatar by username from ActivityPub instance
                var apiUrl = $"{_instanceUrl}/api/v1/accounts/lookup?acct={avatarUsername}";
                
                var httpResponse = await _httpClient.DeleteAsync(apiUrl);
                
                if (httpResponse.IsSuccessStatusCode)
                {
                    response.Result = true;
                    response.Message = "Avatar deleted from ActivityPub instance successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to delete avatar by username from ActivityPub instance: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error deleting avatar by username from ActivityPub: {ex.Message}");
            }

            return response;
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        {
            return DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;
        }

        // Additional methods would be implemented here following the same pattern...
        // For brevity, I'll implement the key methods and mark others as "not yet implemented"

        #endregion

        #region IOASISStorageProvider Holon Methods

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IHolon>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = ActivateProviderAsync().GetAwaiter().GetResult();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate ActivityPub provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Load holon from ActivityPub instance
                var apiUrl = $"{_instanceUrl}/api/v1/statuses/{id}";
                
                var httpResponse = await _httpClient.GetAsync(apiUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    // Parse ActivityPub JSON and create Holon object
                    var holon = ParseActivityPubToHolon(content);
                    if (holon != null)
                    {
                        response.Result = holon;
                        response.Message = "Holon loaded from ActivityPub successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse ActivityPub JSON response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load holon from ActivityPub instance: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading holon from ActivityPub: {ex.Message}");
            }

            return response;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IHolon>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = ActivateProviderAsync().GetAwaiter().GetResult();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate ActivityPub provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Load holon by provider key from ActivityPub instance
                var apiUrl = $"{_instanceUrl}/api/v1/statuses/{providerKey}";
                
                var httpResponse = await _httpClient.GetAsync(apiUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    // Parse ActivityPub JSON and create Holon object
                    var holon = ParseActivityPubToHolon(content);
                    if (holon != null)
                    {
                        response.Result = holon;
                        response.Message = "Holon loaded from ActivityPub successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse ActivityPub JSON response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load holon by provider key from ActivityPub instance: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading holon by provider key from ActivityPub: {ex.Message}");
            }

            return response;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = ActivateProviderAsync().GetAwaiter().GetResult();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate ActivityPub provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Load all holons from ActivityPub instance
                var apiUrl = $"{_instanceUrl}/api/v1/statuses";
                
                var httpResponse = await _httpClient.GetAsync(apiUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    // Parse ActivityPub JSON and create Holon collection
                    var holons = ParseActivityPubToHolons(content);
                    if (holons != null)
                    {
                        response.Result = holons;
                        response.Message = "Holons loaded from ActivityPub successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse ActivityPub JSON response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load all holons from ActivityPub instance: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading all holons from ActivityPub: {ex.Message}");
            }

            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var response = new OASISResult<IHolon>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = ActivateProviderAsync().GetAwaiter().GetResult();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate ActivityPub provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Save holon to ActivityPub instance
                var apiUrl = $"{_instanceUrl}/api/v1/statuses";
                var activityPubJson = ConvertHolonToActivityPub(holon);
                
                var content = new StringContent(activityPubJson, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync(apiUrl, content);
                
                if (httpResponse.IsSuccessStatusCode)
                {
                    response.Result = holon;
                    response.Message = "Holon saved to ActivityPub instance successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to save holon to ActivityPub instance: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error saving holon to ActivityPub: {ex.Message}");
            }

            return response;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonAsync(holon).Result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var response = new OASISResult<IHolon>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = ActivateProviderAsync().GetAwaiter().GetResult();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate ActivityPub provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Delete holon from ActivityPub instance
                var apiUrl = $"{_instanceUrl}/api/v1/statuses/{id}";
                
                var httpResponse = await _httpClient.DeleteAsync(apiUrl);
                
                if (httpResponse.IsSuccessStatusCode)
                {
                    response.Result = null; // Return null since holon is deleted
                    response.Message = "Holon deleted from ActivityPub instance successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to delete holon from ActivityPub instance: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error deleting holon from ActivityPub: {ex.Message}");
            }

            return response;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id)
        {
            return DeleteHolonAsync(id).Result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            var response = new OASISResult<IHolon>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = ActivateProviderAsync().GetAwaiter().GetResult();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate ActivityPub provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Delete holon by provider key from ActivityPub instance
                var apiUrl = $"{_instanceUrl}/api/v1/statuses/{providerKey}";
                
                var httpResponse = await _httpClient.DeleteAsync(apiUrl);
                
                if (httpResponse.IsSuccessStatusCode)
                {
                    response.Result = null; // Return null since holon is deleted
                    response.Message = "Holon deleted from ActivityPub instance successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to delete holon by provider key from ActivityPub instance: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error deleting holon by provider key from ActivityPub: {ex.Message}");
            }

            return response;
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
        {
            return DeleteHolonAsync(providerKey).Result;
        }

        #endregion

        #region IOASISNET Implementation

        OASISResult<IEnumerable<IAvatar>> IOASISNETProvider.GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            var response = new OASISResult<IEnumerable<IAvatar>>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = ActivateProviderAsync().GetAwaiter().GetResult();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate ActivityPub provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Load all avatars and filter by geo-location
                var avatarsResult = LoadAllAvatars();
                if (avatarsResult.IsError || avatarsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref response, $"Error loading avatars: {avatarsResult.Message}");
                    return response;
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

                response.Result = nearby;
                response.IsError = false;
                response.Message = $"Found {nearby.Count} avatars within {radiusInMeters}m";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"Error getting avatars near me from ActivityPub: {ex.Message}", ex);
            }

            return response;
        }

        OASISResult<IEnumerable<IHolon>> IOASISNETProvider.GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = ActivateProviderAsync().GetAwaiter().GetResult();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate ActivityPub provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Load all holons and filter by geo-location and type
                var holonsResult = LoadAllHolons(Type);
                if (holonsResult.IsError || holonsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref response, $"Error loading holons: {holonsResult.Message}");
                    return response;
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

                response.Result = nearby;
                response.IsError = false;
                response.Message = $"Found {nearby.Count} holons within {radiusInMeters}m";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"Error getting holons near me from ActivityPub: {ex.Message}", ex);
            }

            return response;
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Convert OASIS Avatar to ActivityPub JSON format
        /// </summary>
        private string ConvertAvatarToActivityPub(IAvatar avatar)
        {
            try
            {
                // Serialize the complete Avatar object to preserve all properties
                return JsonSerializer.Serialize(avatar, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });
            }
            catch (Exception)
            {
                // Fallback to basic JSON structure if serialization fails
                return $@"{{
                    ""@context"": ""https://www.w3.org/ns/activitystreams"",
                    ""type"": ""Person"",
                    ""id"": ""{avatar.Id}"",
                    ""name"": ""{avatar.Username}"",
                    ""email"": ""{avatar.Email}"",
                    ""published"": ""{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}""
                }}";
            }
        }

        /// <summary>
        /// Parse ActivityPub JSON content and convert to OASIS Avatar
        /// </summary>
        private IAvatar ParseActivityPubToAvatar(string activityPubJson)
        {
            try
            {
                // Deserialize the complete Avatar object to preserve all properties
                var avatar = JsonSerializer.Deserialize<Avatar>(activityPubJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });
                
                return avatar;
            }
            catch (Exception)
            {
                // Return null if parsing fails
                return null;
            }
        }

        /// <summary>
        /// Convert OASIS AvatarDetail to ActivityPub JSON format
        /// </summary>
        private string ConvertAvatarDetailToActivityPub(IAvatarDetail avatarDetail)
        {
            try
            {
                // Serialize the complete AvatarDetail object to preserve all properties
                return JsonSerializer.Serialize(avatarDetail, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });
            }
            catch (Exception)
            {
                // Fallback to basic JSON structure if serialization fails
                return $@"{{
                    ""@context"": ""https://www.w3.org/ns/activitystreams"",
                    ""type"": ""Person"",
                    ""id"": ""{avatarDetail.Id}"",
                    ""name"": ""{avatarDetail.Username}"",
                    ""email"": ""{avatarDetail.Email}"",
                    ""published"": ""{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}""
                }}";
            }
        }

        /// <summary>
        /// Convert OASIS Holon to ActivityPub JSON format
        /// </summary>
        private string ConvertHolonToActivityPub(IHolon holon)
        {
            try
            {
                // Serialize the complete Holon object to preserve all properties
                return JsonSerializer.Serialize(holon, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });
            }
            catch (Exception)
            {
                // Fallback to basic JSON structure if serialization fails
                return $@"{{
                    ""@context"": ""https://www.w3.org/ns/activitystreams"",
                    ""type"": ""Note"",
                    ""id"": ""{holon.Id}"",
                    ""content"": ""{holon.Name}"",
                    ""published"": ""{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}""
                }}";
            }
        }

        /// <summary>
        /// Parse ActivityPub JSON content and convert to OASIS Holon
        /// </summary>
        private IHolon ParseActivityPubToHolon(string activityPubJson)
        {
            try
            {
                // Deserialize the complete Holon object to preserve all properties
                var holon = JsonSerializer.Deserialize<Holon>(activityPubJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });
                
                return holon;
            }
            catch (Exception)
            {
                // Return null if parsing fails
                return null;
            }
        }

        /// <summary>
        /// Parse ActivityPub JSON content and convert to OASIS Holon collection
        /// </summary>
        private IEnumerable<IHolon> ParseActivityPubToHolons(string activityPubJson)
        {
            try
            {
                // Deserialize the complete Holon collection to preserve all properties
                var holons = JsonSerializer.Deserialize<IEnumerable<Holon>>(activityPubJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });
                
                return holons;
            }
            catch (Exception)
            {
                // Return null if parsing fails
                return null;
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _httpClient?.Dispose();
        }

        #endregion

        #region Missing Abstract Methods - Stub Implementations

        // Holon methods
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = ActivateProviderAsync().GetAwaiter().GetResult();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate ActivityPub provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Query ActivityPub for holons by parent ID
                var queryUrl = $"/api/v1/accounts/{id}/statuses";
                
                var httpResponse = await _httpClient.GetAsync(queryUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var statuses = JsonSerializer.Deserialize<JsonElement[]>(content);
                    
                    var holons = new List<IHolon>();
                    foreach (var status in statuses)
                    {
                        var holon = ParseActivityPubToHolon(status);
                        if (holon != null)
                        {
                            holons.Add(holon);
                        }
                    }
                    
                    response.Result = holons;
                    response.IsError = false;
                    response.Message = "Holons loaded from ActivityPub successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load holons from ActivityPub: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading holons from ActivityPub: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            // First load the parent holon to get its ID
            var parentResult = await LoadHolonAsync(providerKey, false, false, 0, continueOnError, loadChildrenFromProvider, version);
            if (parentResult.IsError || parentResult.Result == null)
            {
                return new OASISResult<IEnumerable<IHolon>>
                {
                    IsError = true,
                    Message = $"Failed to load parent holon by provider key: {parentResult.Message}"
                };
            }

            // Then load children using the parent ID
            return await LoadHolonsForParentAsync(parentResult.Result.Id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            // Convert single key-value pair to dictionary and use the main method
            var metaKeyValuePairs = new Dictionary<string, string> { { metaKey, metaValue } };
            return await LoadHolonsByMetaDataAsync(metaKeyValuePairs, MetaKeyValuePairMatchMode.All, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = ActivateProviderAsync().GetAwaiter().GetResult();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate ActivityPub provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Use ActivityPub search API to find holons by metadata
                // Build search query from metadata pairs
                var searchQuery = string.Join(" ", metaKeyValuePairs.Values);
                var apiUrl = $"/api/v2/search?q={Uri.EscapeDataString(searchQuery)}&type=statuses";
                
                var httpResponse = await _httpClient.GetAsync(apiUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var searchResult = JsonSerializer.Deserialize<JsonElement>(content);
                    
                    var holons = new List<IHolon>();
                    if (searchResult.TryGetProperty("statuses", out var statuses))
                    {
                        foreach (var status in statuses.EnumerateArray())
                        {
                            var holon = ParseActivityPubToHolon(status);
                            if (holon != null)
                            {
                                // Filter by metadata match mode
                                bool matches = metaKeyValuePairMatchMode == MetaKeyValuePairMatchMode.All
                                    ? metaKeyValuePairs.All(kvp => holon.MetaData.ContainsKey(kvp.Key) && holon.MetaData[kvp.Key]?.ToString() == kvp.Value)
                                    : metaKeyValuePairs.Any(kvp => holon.MetaData.ContainsKey(kvp.Key) && holon.MetaData[kvp.Key]?.ToString()?.Contains(kvp.Value) == true);
                                
                                if (matches && (type == HolonType.All || holon.HolonType == type))
                                {
                                    holons.Add(holon);
                                }
                            }
                        }
                    }
                    
                    response.Result = holons;
                    response.IsError = false;
                    response.Message = $"Successfully loaded {holons.Count} holons by metadata from ActivityPub";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to search holons in ActivityPub: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading holons by metadata from ActivityPub: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            var savedHolons = new List<IHolon>();
            
            try
            {
                if (!_isActivated)
                {
                    var activateResult = ActivateProviderAsync().GetAwaiter().GetResult();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate ActivityPub provider: {activateResult.Message}");
                        return response;
                    }
                }

                if (holons == null || !holons.Any())
                {
                    response.Result = savedHolons;
                    response.IsError = false;
                    response.Message = "No holons to save";
                    return response;
                }

                // Save each holon using ActivityPub API
                foreach (var holon in holons)
                {
                    try
                    {
                        var saveResult = await SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
                        if (!saveResult.IsError && saveResult.Result != null)
                        {
                            savedHolons.Add(saveResult.Result);
                        }
                        else if (!continueOnError)
                        {
                            OASISErrorHandling.HandleError(ref response, $"Failed to save holon {holon.Id}: {saveResult.Message}");
                            return response;
                        }
                    }
                    catch (Exception ex)
                    {
                        if (!continueOnError)
                        {
                            OASISErrorHandling.HandleError(ref response, $"Error saving holon {holon.Id}: {ex.Message}", ex);
                            return response;
                        }
                    }
                }
                
                response.Result = savedHolons;
                response.IsError = false;
                response.Message = $"Successfully saved {savedHolons.Count} holons to ActivityPub";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error saving holons to ActivityPub: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }

        // Search methods
        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var response = new OASISResult<ISearchResults>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = ActivateProviderAsync().GetAwaiter().GetResult();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate ActivityPub provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Extract search query from searchParams
                string searchQuery = "";
                if (searchParams is ISearchTextGroup textGroup)
                {
                    searchQuery = textGroup.SearchQuery ?? "";
                }

                if (string.IsNullOrWhiteSpace(searchQuery))
                {
                    OASISErrorHandling.HandleError(ref response, "Search query cannot be empty");
                    return response;
                }

                // Use ActivityPub search API
                var apiUrl = $"/api/v2/search?q={Uri.EscapeDataString(searchQuery)}&type=statuses";
                var httpResponse = await _httpClient.GetAsync(apiUrl);
                
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var searchResult = JsonSerializer.Deserialize<JsonElement>(content);
                    
                    var searchResults = new SearchResults();
                    
                    if (searchResult.TryGetProperty("statuses", out var statuses))
                    {
                        var holons = new List<IHolon>();
                        foreach (var status in statuses.EnumerateArray())
                        {
                            var holon = ParseActivityPubToHolon(status);
                            if (holon != null)
                            {
                                holons.Add(holon);
                            }
                        }
                        searchResults.SearchResultHolons = holons;
                    }
                    
                    response.Result = searchResults;
                    response.IsError = false;
                    response.Message = "Search completed successfully in ActivityPub";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to search in ActivityPub: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error searching in ActivityPub: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
        }

        // Import/Export methods
        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            // Use SaveHolonsAsync to import holons
            var saveResult = await SaveHolonsAsync(holons, true, true, 0, 0, true, false);
            
            var result = new OASISResult<bool>();
            if (!saveResult.IsError && saveResult.Result != null)
            {
                result.Result = true;
                result.IsError = false;
                result.Message = $"Successfully imported {saveResult.Result.Count()} holons to ActivityPub";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to import holons: {saveResult.Message}");
            }
            return result;
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
        {
            return ImportAsync(holons).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            // Load all holons for the avatar (which are statuses in ActivityPub)
            return await LoadHolonsForParentAsync(avatarId, HolonType.All, true, true, 0, 0, true, false, version);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
        {
            return ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string username, int version = 0)
        {
            // First load the avatar by username to get its ID
            var avatarResult = await LoadAvatarByUsernameAsync(username, version);
            if (avatarResult.IsError || avatarResult.Result == null)
            {
                return new OASISResult<IEnumerable<IHolon>>
                {
                    IsError = true,
                    Message = $"Failed to load avatar by username: {avatarResult.Message}"
                };
            }

            // Then export all data for that avatar
            return await ExportAllDataForAvatarByIdAsync(avatarResult.Result.Id, version);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string username, int version = 0)
        {
            return ExportAllDataForAvatarByUsernameAsync(username, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string email, int version = 0)
        {
            // First load the avatar by email to get its ID
            var avatarResult = await LoadAvatarByEmailAsync(email, version);
            if (avatarResult.IsError || avatarResult.Result == null)
            {
                return new OASISResult<IEnumerable<IHolon>>
                {
                    IsError = true,
                    Message = $"Failed to load avatar by email: {avatarResult.Message}"
                };
            }

            // Then export all data for that avatar
            return await ExportAllDataForAvatarByIdAsync(avatarResult.Result.Id, version);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string email, int version = 0)
        {
            return ExportAllDataForAvatarByEmailAsync(email, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            // Load all holons (statuses) from ActivityPub
            return await LoadAllHolonsAsync(HolonType.All, true, true, 0, 0, true, false, version);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
        {
            return ExportAllAsync(version).Result;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Parse ActivityPub status to Holon object
        /// </summary>
        private Holon ParseActivityPubToHolon(JsonElement status)
        {
            try
            {
                var statusId = status.TryGetProperty("id", out var id) ? id.GetString() : "";
                var holon = new Holon
                {
                    Id = CreateDeterministicGuid($"{ProviderType.Value}:holon:{statusId ?? "unknown"}"),
                    Name = status.TryGetProperty("content", out var content) ? content.GetString() : "ActivityPub Status",
                    Description = status.TryGetProperty("spoiler_text", out var spoiler) ? spoiler.GetString() : "",
                    HolonType = HolonType.Holon,
                    CreatedDate = status.TryGetProperty("created_at", out var createdAt) ? DateTime.Parse(createdAt.GetString()) : DateTime.UtcNow,
                    ModifiedDate = status.TryGetProperty("updated_at", out var updatedAt) ? DateTime.Parse(updatedAt.GetString()) : DateTime.UtcNow,
                    Version = 1,
                    IsActive = true
                };

                // Add metadata from ActivityPub status
                if (status.TryGetProperty("id", out var idProp))
                {
                        holon.ProviderMetaData[Core.Enums.ProviderType.ActivityPubOASIS] = new Dictionary<string, string> { ["activitypub_id"] = idProp.GetString() };
                }
                if (status.TryGetProperty("url", out var url))
                {
                        holon.ProviderMetaData[Core.Enums.ProviderType.ActivityPubOASIS] = new Dictionary<string, string> { ["activitypub_url"] = url.GetString() };
                }
                if (status.TryGetProperty("visibility", out var visibility))
                {
                        holon.ProviderMetaData[Core.Enums.ProviderType.ActivityPubOASIS] = new Dictionary<string, string> { ["activitypub_visibility"] = visibility.GetString() };
                }

                return holon;
            }
            catch (Exception)
            {
                return null;
            }
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
}

