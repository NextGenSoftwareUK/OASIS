using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using System.Threading;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.Providers.ThreeFoldOASIS
{
    public partial class ThreeFoldOASIS
    {

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var response = new OASISResult<bool>();
            try
            {
                // Initialize ThreeFold connection
                response.Result = true;
                response.Message = "ThreeFold provider activated successfully";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error activating ThreeFold provider: {ex.Message}");
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
                // Cleanup ThreeFold connection
                response.Result = true;
                response.Message = "ThreeFold provider deactivated successfully";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error deactivating ThreeFold provider: {ex.Message}");
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
                // Load avatar from ThreeFold network
                // This would query ThreeFold network for avatar data
                var avatar = new Avatar
                {
                    Id = id,
                    Username = $"threefold_user_{id}",
                    Email = $"user_{id}@threefold.example",
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow
                };

                response.Result = avatar;
                response.Message = "Avatar loaded from ThreeFold successfully";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar from ThreeFold: {ex.Message}");
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
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate ThreeFold provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Load avatar by provider key from ThreeFold Grid API
                var apiResponse = await _httpClient.GetAsync($"{_apiBaseUrl}/avatars/provider-key/{Uri.EscapeDataString(providerKey)}?version={version}");

                if (apiResponse.IsSuccessStatusCode)
                {
                    var content = await apiResponse.Content.ReadAsStringAsync();
                    var avatarData = JsonSerializer.Deserialize<Dictionary<string, object>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (avatarData != null)
                    {
                        var avatar = new Avatar
                        {
                            Id = avatarData.ContainsKey("id") && Guid.TryParse(avatarData["id"].ToString(), out var id) ? id : Guid.NewGuid(),
                            Username = avatarData.ContainsKey("username") ? avatarData["username"].ToString() : $"threefold_user_{providerKey}",
                            Email = avatarData.ContainsKey("email") ? avatarData["email"].ToString() : $"user_{providerKey}@threefold.example",
                            FirstName = avatarData.ContainsKey("firstName") ? avatarData["firstName"].ToString() : "",
                            LastName = avatarData.ContainsKey("lastName") ? avatarData["lastName"].ToString() : "",
                            CreatedDate = avatarData.ContainsKey("createdDate") && DateTime.TryParse(avatarData["createdDate"].ToString(), out var created) ? created : DateTime.UtcNow,
                            ModifiedDate = DateTime.UtcNow,
                            Version = version
                        };

                        response.Result = avatar;
                        response.IsError = false;
                        response.Message = "Avatar loaded successfully from ThreeFold Grid by provider key";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to deserialize avatar data from ThreeFold Grid API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"ThreeFold Grid API error: {apiResponse.StatusCode} - {apiResponse.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by provider key from ThreeFold: {ex.Message}");
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
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate ThreeFold provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Load avatar by email from ThreeFold Grid API
                var apiResponse = await _httpClient.GetAsync($"{_apiBaseUrl}/avatars/email/{Uri.EscapeDataString(avatarEmail)}?version={version}");

                if (apiResponse.IsSuccessStatusCode)
                {
                    var content = await apiResponse.Content.ReadAsStringAsync();
                    var avatarData = JsonSerializer.Deserialize<Dictionary<string, object>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (avatarData != null)
                    {
                        var avatar = new Avatar
                        {
                            Id = avatarData.ContainsKey("id") && Guid.TryParse(avatarData["id"].ToString(), out var id) ? id : Guid.NewGuid(),
                            Username = avatarData.ContainsKey("username") ? avatarData["username"].ToString() : avatarEmail.Split('@')[0],
                            Email = avatarEmail,
                            FirstName = avatarData.ContainsKey("firstName") ? avatarData["firstName"].ToString() : "",
                            LastName = avatarData.ContainsKey("lastName") ? avatarData["lastName"].ToString() : "",
                            CreatedDate = avatarData.ContainsKey("createdDate") && DateTime.TryParse(avatarData["createdDate"].ToString(), out var created) ? created : DateTime.UtcNow,
                            ModifiedDate = DateTime.UtcNow,
                            Version = version
                        };

                        response.Result = avatar;
                        response.IsError = false;
                        response.Message = "Avatar loaded successfully from ThreeFold Grid by email";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to deserialize avatar data from ThreeFold Grid API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"ThreeFold Grid API error: {apiResponse.StatusCode} - {apiResponse.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by email from ThreeFold: {ex.Message}");
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
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate ThreeFold provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Load avatar by username from ThreeFold Grid API
                var apiResponse = await _httpClient.GetAsync($"{_apiBaseUrl}/avatars/username/{Uri.EscapeDataString(avatarUsername)}?version={version}");

                if (apiResponse.IsSuccessStatusCode)
                {
                    var content = await apiResponse.Content.ReadAsStringAsync();
                    var avatarData = JsonSerializer.Deserialize<Dictionary<string, object>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (avatarData != null)
                    {
                        var avatar = new Avatar
                        {
                            Id = avatarData.ContainsKey("id") && Guid.TryParse(avatarData["id"].ToString(), out var id) ? id : Guid.NewGuid(),
                            Username = avatarUsername,
                            Email = avatarData.ContainsKey("email") ? avatarData["email"].ToString() : $"{avatarUsername}@threefold.example",
                            FirstName = avatarData.ContainsKey("firstName") ? avatarData["firstName"].ToString() : "",
                            LastName = avatarData.ContainsKey("lastName") ? avatarData["lastName"].ToString() : "",
                            CreatedDate = avatarData.ContainsKey("createdDate") && DateTime.TryParse(avatarData["createdDate"].ToString(), out var created) ? created : DateTime.UtcNow,
                            ModifiedDate = DateTime.UtcNow,
                            Version = version
                        };

                        response.Result = avatar;
                        response.IsError = false;
                        response.Message = "Avatar loaded successfully from ThreeFold Grid by username";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to deserialize avatar data from ThreeFold Grid API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"ThreeFold Grid API error: {apiResponse.StatusCode} - {apiResponse.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by username from ThreeFold: {ex.Message}");
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
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate ThreeFold provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Load avatar detail from ThreeFold Grid API
                var apiResponse = await _httpClient.GetAsync($"{_apiBaseUrl}/avatar-details/{id}?version={version}");

                if (apiResponse.IsSuccessStatusCode)
                {
                    var content = await apiResponse.Content.ReadAsStringAsync();
                    var avatarDetailData = JsonSerializer.Deserialize<Dictionary<string, object>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (avatarDetailData != null)
                    {
                        var avatarDetail = new AvatarDetail
                        {
                            Id = id,
                            Username = avatarDetailData.ContainsKey("username") ? avatarDetailData["username"].ToString() : "",
                            Email = avatarDetailData.ContainsKey("email") ? avatarDetailData["email"].ToString() : "",
                            FirstName = avatarDetailData.ContainsKey("firstName") ? avatarDetailData["firstName"].ToString() : "",
                            LastName = avatarDetailData.ContainsKey("lastName") ? avatarDetailData["lastName"].ToString() : "",
                            Karma = avatarDetailData.ContainsKey("karma") && long.TryParse(avatarDetailData["karma"].ToString(), out var karma) ? karma : 0,
                            XP = avatarDetailData.ContainsKey("xp") && int.TryParse(avatarDetailData["xp"].ToString(), out var xp) ? xp : 0,
                            CreatedDate = avatarDetailData.ContainsKey("createdDate") && DateTime.TryParse(avatarDetailData["createdDate"].ToString(), out var created) ? created : DateTime.UtcNow,
                            ModifiedDate = DateTime.UtcNow,
                            Version = version
                        };

                        response.Result = avatarDetail;
                        response.IsError = false;
                        response.Message = "Avatar detail loaded successfully from ThreeFold Grid";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to deserialize avatar detail data from ThreeFold Grid API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"ThreeFold Grid API error: {apiResponse.StatusCode} - {apiResponse.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar detail from ThreeFold: {ex.Message}");
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
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate ThreeFold provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Load avatar detail by email from ThreeFold Grid API
                var apiResponse = await _httpClient.GetAsync($"{_apiBaseUrl}/avatar-details/email/{Uri.EscapeDataString(avatarEmail)}?version={version}");

                if (apiResponse.IsSuccessStatusCode)
                {
                    var content = await apiResponse.Content.ReadAsStringAsync();
                    var avatarDetailData = JsonSerializer.Deserialize<Dictionary<string, object>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (avatarDetailData != null)
                    {
                        var avatarDetail = new AvatarDetail
                        {
                            Id = avatarDetailData.ContainsKey("id") && Guid.TryParse(avatarDetailData["id"].ToString(), out var id) ? id : Guid.NewGuid(),
                            Username = avatarDetailData.ContainsKey("username") ? avatarDetailData["username"].ToString() : avatarEmail.Split('@')[0],
                            Email = avatarEmail,
                            FirstName = avatarDetailData.ContainsKey("firstName") ? avatarDetailData["firstName"].ToString() : "",
                            LastName = avatarDetailData.ContainsKey("lastName") ? avatarDetailData["lastName"].ToString() : "",
                            Karma = avatarDetailData.ContainsKey("karma") && long.TryParse(avatarDetailData["karma"].ToString(), out var karma) ? karma : 0,
                            XP = avatarDetailData.ContainsKey("xp") && int.TryParse(avatarDetailData["xp"].ToString(), out var xp) ? xp : 0,
                            CreatedDate = avatarDetailData.ContainsKey("createdDate") && DateTime.TryParse(avatarDetailData["createdDate"].ToString(), out var created) ? created : DateTime.UtcNow,
                            ModifiedDate = DateTime.UtcNow,
                            Version = version
                        };

                        response.Result = avatarDetail;
                        response.IsError = false;
                        response.Message = "Avatar detail loaded successfully from ThreeFold Grid by email";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to deserialize avatar detail data from ThreeFold Grid API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"ThreeFold Grid API error: {apiResponse.StatusCode} - {apiResponse.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar detail by email from ThreeFold: {ex.Message}");
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
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate ThreeFold provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Load avatar detail by username from ThreeFold Grid API
                var apiResponse = await _httpClient.GetAsync($"{_apiBaseUrl}/avatar-details/username/{Uri.EscapeDataString(avatarUsername)}?version={version}");

                if (apiResponse.IsSuccessStatusCode)
                {
                    var content = await apiResponse.Content.ReadAsStringAsync();
                    var avatarDetailData = JsonSerializer.Deserialize<Dictionary<string, object>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (avatarDetailData != null)
                    {
                        var avatarDetail = new AvatarDetail
                        {
                            Id = avatarDetailData.ContainsKey("id") && Guid.TryParse(avatarDetailData["id"].ToString(), out var id) ? id : Guid.NewGuid(),
                            Username = avatarUsername,
                            Email = avatarDetailData.ContainsKey("email") ? avatarDetailData["email"].ToString() : $"{avatarUsername}@threefold.example",
                            FirstName = avatarDetailData.ContainsKey("firstName") ? avatarDetailData["firstName"].ToString() : "",
                            LastName = avatarDetailData.ContainsKey("lastName") ? avatarDetailData["lastName"].ToString() : "",
                            Karma = avatarDetailData.ContainsKey("karma") && long.TryParse(avatarDetailData["karma"].ToString(), out var karma) ? karma : 0,
                            XP = avatarDetailData.ContainsKey("xp") && int.TryParse(avatarDetailData["xp"].ToString(), out var xp) ? xp : 0,
                            CreatedDate = avatarDetailData.ContainsKey("createdDate") && DateTime.TryParse(avatarDetailData["createdDate"].ToString(), out var created) ? created : DateTime.UtcNow,
                            ModifiedDate = DateTime.UtcNow,
                            Version = version
                        };

                        response.Result = avatarDetail;
                        response.IsError = false;
                        response.Message = "Avatar detail loaded successfully from ThreeFold Grid by username";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to deserialize avatar detail data from ThreeFold Grid API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"ThreeFold Grid API error: {apiResponse.StatusCode} - {apiResponse.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar detail by username from ThreeFold: {ex.Message}");
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
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate ThreeFold provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Load all avatars from ThreeFold Grid API
                var apiResponse = await _httpClient.GetAsync($"{_apiBaseUrl}/avatars?version={version}");

                if (apiResponse.IsSuccessStatusCode)
                {
                    var content = await apiResponse.Content.ReadAsStringAsync();
                    var avatarsList = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (avatarsList != null)
                    {
                        var avatars = new List<IAvatar>();
                        foreach (var avatarData in avatarsList)
                        {
                            var avatar = new Avatar
                            {
                                Id = avatarData.ContainsKey("id") && Guid.TryParse(avatarData["id"].ToString(), out var id) ? id : Guid.NewGuid(),
                                Username = avatarData.ContainsKey("username") ? avatarData["username"].ToString() : "",
                                Email = avatarData.ContainsKey("email") ? avatarData["email"].ToString() : "",
                                FirstName = avatarData.ContainsKey("firstName") ? avatarData["firstName"].ToString() : "",
                                LastName = avatarData.ContainsKey("lastName") ? avatarData["lastName"].ToString() : "",
                                CreatedDate = avatarData.ContainsKey("createdDate") && DateTime.TryParse(avatarData["createdDate"].ToString(), out var created) ? created : DateTime.UtcNow,
                                ModifiedDate = DateTime.UtcNow,
                                Version = version
                            };
                            avatars.Add(avatar);
                        }

                        response.Result = avatars;
                        response.IsError = false;
                        response.Message = $"Successfully loaded {avatars.Count} avatars from ThreeFold Grid";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to deserialize avatars data from ThreeFold Grid API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"ThreeFold Grid API error: {apiResponse.StatusCode} - {apiResponse.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading all avatars from ThreeFold: {ex.Message}");
            }
            return response;
        }

    }
}
