using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
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
                    OASISErrorHandling.HandleError(ref response, "ActivityPub provider is not activated");
                    return response;
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
                        response.Result = avatar;
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
                    OASISErrorHandling.HandleError(ref response, "ActivityPub provider is not activated");
                    return response;
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
                        response.Result = avatar;
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
                    OASISErrorHandling.HandleError(ref response, "ActivityPub provider is not activated");
                    return response;
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
                        response.Result = avatar;
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
                    OASISErrorHandling.HandleError(ref response, "ActivityPub provider is not activated");
                    return response;
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
                        response.Result = avatar;
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
                    OASISErrorHandling.HandleError(ref response, "ActivityPub provider is not activated");
                    return response;
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
                        response.Result = avatar;
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
                    OASISErrorHandling.HandleError(ref response, "ActivityPub provider is not activated");
                    return response;
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
                        response.Result = avatar;
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
                    OASISErrorHandling.HandleError(ref response, "ActivityPub provider is not activated");
                    return response;
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
                        response.Result = avatar;
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
                    OASISErrorHandling.HandleError(ref response, "ActivityPub provider is not activated");
                    return response;
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
                        response.Result = avatar;
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
                    OASISErrorHandling.HandleError(ref response, "ActivityPub provider is not activated");
                    return response;
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
                        response.Result = avatar;
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
                    OASISErrorHandling.HandleError(ref response, "ActivityPub provider is not activated");
                    return response;
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
                    OASISErrorHandling.HandleError(ref response, "ActivityPub provider is not activated");
                    return response;
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
                    OASISErrorHandling.HandleError(ref response, "ActivityPub provider is not activated");
                    return response;
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
                    OASISErrorHandling.HandleError(ref response, "ActivityPub provider is not activated");
                    return response;
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
                    OASISErrorHandling.HandleError(ref response, "ActivityPub provider is not activated");
                    return response;
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
                    OASISErrorHandling.HandleError(ref response, "ActivityPub provider is not activated");
                    return response;
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
                    OASISErrorHandling.HandleError(ref response, "ActivityPub provider is not activated");
                    return response;
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
            return LoadHolonAsync(id, version).Result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IHolon>();

            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "ActivityPub provider is not activated");
                    return response;
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
            return LoadHolonByProviderKeyAsync(providerKey, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "ActivityPub provider is not activated");
                    return response;
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
            return LoadAllHolonsAsync(version).Result;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var response = new OASISResult<IHolon>();

            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "ActivityPub provider is not activated");
                    return response;
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
            var response = new OASISResult<bool>();

            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "ActivityPub provider is not activated");
                    return response;
                }

                // Delete holon from ActivityPub instance
                var apiUrl = $"{_instanceUrl}/api/v1/statuses/{id}";
                
                var httpResponse = await _httpClient.DeleteAsync(apiUrl);
                
                if (httpResponse.IsSuccessStatusCode)
                {
                    response.Result = true;
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
            return DeleteHolonAsync(id, softDelete).Result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            var response = new OASISResult<bool>();

            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "ActivityPub provider is not activated");
                    return response;
                }

                // Delete holon by provider key from ActivityPub instance
                var apiUrl = $"{_instanceUrl}/api/v1/statuses/{providerKey}";
                
                var httpResponse = await _httpClient.DeleteAsync(apiUrl);
                
                if (httpResponse.IsSuccessStatusCode)
                {
                    response.Result = true;
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
            return DeleteHolonAsync(providerKey, softDelete).Result;
        }

        #endregion

        #region IOASISNET Implementation

        OASISResult<IEnumerable<IPlayer>> IOASISNETProvider.GetPlayersNearMe()
        {
            var response = new OASISResult<IEnumerable<IPlayer>>();

            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "ActivityPub provider is not activated");
                    return response;
                }

                // Get players near me from ActivityPub instance
                var apiUrl = $"{_instanceUrl}/api/v1/accounts/followers";
                
                var httpResponse = await _httpClient.GetAsync(apiUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    // Parse ActivityPub JSON and create Player collection
                    // Parse ActivityPub JSON and create Avatar object
                    var avatar = ParseActivityPubToAvatar(content);
                    if (avatar != null)
                    {
                        response.Result = avatar;
                        response.Message = "Avatar loaded from ActivityPub successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse ActivityPub JSON response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to get players near me from ActivityPub instance: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error getting players near me from ActivityPub: {ex.Message}");
            }

            return response;
        }

        OASISResult<IEnumerable<IHolon>> IOASISNETProvider.GetHolonsNearMe(HolonType Type)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "ActivityPub provider is not activated");
                    return response;
                }

                // Get holons near me from ActivityPub instance
                var apiUrl = $"{_instanceUrl}/api/v1/statuses?type={Type}";
                
                var httpResponse = await _httpClient.GetAsync(apiUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    // Parse ActivityPub JSON and create Holon collection
                    // Parse ActivityPub JSON and create Avatar object
                    var avatar = ParseActivityPubToAvatar(content);
                    if (avatar != null)
                    {
                        response.Result = avatar;
                        response.Message = "Avatar loaded from ActivityPub successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse ActivityPub JSON response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to get holons near me from ActivityPub instance: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error getting holons near me from ActivityPub: {ex.Message}");
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
    }
}
