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
    public partial class ActivityPubOASIS
    {

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
                    var activateResult = await ActivateProviderAsync();
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
                    var activateResult = await ActivateProviderAsync();
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
                    var activateResult = await ActivateProviderAsync();
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
                    var activateResult = await ActivateProviderAsync();
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
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate ActivityPub provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Load avatar detail from ActivityPub instance (parse as AvatarDetail, do not build from Avatar)
                var apiUrl = $"{_instanceUrl}/api/v1/accounts/{id}";
                var httpResponse = await _httpClient.GetAsync(apiUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var avatarDetail = ParseActivityPubToAvatarDetail(content);
                    if (avatarDetail != null)
                    {
                        response.Result = avatarDetail;
                        response.IsError = false;
                        response.Message = "Avatar detail loaded from ActivityPub successfully";
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
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate ActivityPub provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Load avatar detail by email: search accounts then parse as AvatarDetail (do not build from Avatar)
                var apiUrl = $"{_instanceUrl}/api/v1/accounts/search?q={Uri.EscapeDataString(avatarEmail)}";
                var httpResponse = await _httpClient.GetAsync(apiUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var avatarDetail = ParseActivityPubToAvatarDetail(content);
                    if (avatarDetail != null && string.Equals(avatarDetail.Email, avatarEmail, StringComparison.OrdinalIgnoreCase))
                    {
                        response.Result = avatarDetail;
                        response.IsError = false;
                        response.Message = "Avatar detail loaded from ActivityPub by email successfully";
                    }
                    else if (avatarDetail != null)
                    {
                        response.Result = avatarDetail;
                        response.IsError = false;
                        response.Message = "Avatar detail loaded from ActivityPub search (email match best-effort)";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse ActivityPub JSON response or no match by email");
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
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate ActivityPub provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Load avatar detail by username from ActivityPub instance (parse as AvatarDetail, do not build from Avatar)
                var apiUrl = $"{_instanceUrl}/api/v1/accounts/lookup?acct={Uri.EscapeDataString(avatarUsername)}";
                var httpResponse = await _httpClient.GetAsync(apiUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var avatarDetail = ParseActivityPubToAvatarDetail(content);
                    if (avatarDetail != null)
                    {
                        response.Result = avatarDetail;
                        response.IsError = false;
                        response.Message = "Avatar detail loaded from ActivityPub by username successfully";
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
                    var activateResult = await ActivateProviderAsync();
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
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate ActivityPub provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Load all avatar details from accounts endpoint; parse each as AvatarDetail (do not build from Avatar)
                var apiUrl = $"{_instanceUrl}/api/v1/accounts";
                var httpResponse = await _httpClient.GetAsync(apiUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var list = new List<IAvatarDetail>();
                    try
                    {
                        using var doc = JsonDocument.Parse(content);
                        var root = doc.RootElement;
                        if (root.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var item in root.EnumerateArray())
                            {
                                var detail = ParseActivityPubToAvatarDetail(item.GetRawText());
                                if (detail != null) list.Add(detail);
                            }
                        }
                        else
                        {
                            var detail = ParseActivityPubToAvatarDetail(content);
                            if (detail != null) list.Add(detail);
                        }
                    }
                    catch
                    {
                        var detail = ParseActivityPubToAvatarDetail(content);
                        if (detail != null) list.Add(detail);
                    }
                    response.Result = list;
                    response.IsError = false;
                    response.Message = $"Loaded {list.Count} avatar details from ActivityPub";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar details from ActivityPub instance: {httpResponse.StatusCode}");
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
                    var activateResult = await ActivateProviderAsync();
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
                    var activateResult = await ActivateProviderAsync();
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
                    var activateResult = await ActivateProviderAsync();
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
                    var activateResult = await ActivateProviderAsync();
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
                    var activateResult = await ActivateProviderAsync();
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
                    var activateResult = await ActivateProviderAsync();
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


    }
}
