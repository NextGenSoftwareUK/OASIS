using NextGenSoftware.OASIS.API.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Holons;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Linq;
using System.IO;

namespace NextGenSoftware.OASIS.API.Providers.BlockStackOASIS
{
    public partial class PLANOASIS
    {
        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var response = new OASISResult<bool>();
            try
            {
                // Initialize PLAN connection
                response.Result = true;
                response.Message = "PLAN provider activated successfully";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error activating PLAN provider: {ex.Message}");
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
                // Cleanup PLAN connection
                response.Result = true;
                response.Message = "PLAN provider deactivated successfully";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error deactivating PLAN provider: {ex.Message}");
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
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate PLAN provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Load avatar from PLAN network using HTTP client
                var planUrl = $"https://api.plan.network/avatars/{id}";
                var httpClient = new System.Net.Http.HttpClient();
                var httpResponse = await httpClient.GetAsync(planUrl);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var avatarData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(content);
                    
                    // Create avatar from PLAN data
                    var avatar = new NextGenSoftware.OASIS.API.Core.Holons.Avatar
                    {
                        Id = id,
                        Username = avatarData?.ContainsKey("username") == true ? avatarData["username"].ToString() : "",
                        Email = avatarData?.ContainsKey("email") == true ? avatarData["email"].ToString() : "",
                        FirstName = avatarData?.ContainsKey("firstName") == true ? avatarData["firstName"].ToString() : "",
                        LastName = avatarData?.ContainsKey("lastName") == true ? avatarData["lastName"].ToString() : "",
                        CreatedDate = DateTime.UtcNow,
                        ModifiedDate = DateTime.UtcNow,
                        Version = version
                    };

                    response.Result = avatar;
                    response.IsError = false;
                    response.Message = "Avatar loaded successfully from PLAN network";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar from PLAN network: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar from PLAN: {ex.Message}");
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
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate PLAN provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Load avatar by provider key from PLAN network API
                var planUrl = $"{_apiBaseUrl}/avatars/provider-key/{Uri.EscapeDataString(providerKey)}?version={version}";
                var httpResponse = await _httpClient.GetAsync(planUrl);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var avatarData = JsonSerializer.Deserialize<Dictionary<string, object>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (avatarData != null)
                    {
                        var avatar = new Avatar
                        {
                            Id = avatarData.ContainsKey("id") && Guid.TryParse(avatarData["id"].ToString(), out var id) ? id : Guid.NewGuid(),
                            Username = avatarData.ContainsKey("username") ? avatarData["username"].ToString() : $"plan_user_{providerKey}",
                            Email = avatarData.ContainsKey("email") ? avatarData["email"].ToString() : $"user_{providerKey}@plan.example",
                            FirstName = avatarData.ContainsKey("firstName") ? avatarData["firstName"].ToString() : "",
                            LastName = avatarData.ContainsKey("lastName") ? avatarData["lastName"].ToString() : "",
                            CreatedDate = avatarData.ContainsKey("createdDate") && DateTime.TryParse(avatarData["createdDate"].ToString(), out var created) ? created : DateTime.UtcNow,
                            ModifiedDate = DateTime.UtcNow,
                            Version = version
                        };

                        response.Result = avatar;
                        response.IsError = false;
                        response.Message = "Avatar loaded successfully from PLAN network by provider key";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to deserialize avatar data from PLAN network API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar from PLAN network: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by provider key from PLAN: {ex.Message}");
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
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate PLAN provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Load avatar by email from PLAN network API
                var planUrl = $"{_apiBaseUrl}/avatars/email/{Uri.EscapeDataString(avatarEmail)}?version={version}";
                var httpResponse = await _httpClient.GetAsync(planUrl);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
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
                        response.Message = "Avatar loaded successfully from PLAN network by email";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to deserialize avatar data from PLAN network API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar from PLAN network: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by email from PLAN: {ex.Message}");
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
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate PLAN provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Load avatar by username from PLAN network API
                var planUrl = $"{_apiBaseUrl}/avatars/username/{Uri.EscapeDataString(avatarUsername)}?version={version}";
                var httpResponse = await _httpClient.GetAsync(planUrl);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var avatarData = JsonSerializer.Deserialize<Dictionary<string, object>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (avatarData != null)
                    {
                        var avatar = new Avatar
                        {
                            Id = avatarData.ContainsKey("id") && Guid.TryParse(avatarData["id"].ToString(), out var id) ? id : Guid.NewGuid(),
                            Username = avatarUsername,
                            Email = avatarData.ContainsKey("email") ? avatarData["email"].ToString() : $"{avatarUsername}@plan.example",
                            FirstName = avatarData.ContainsKey("firstName") ? avatarData["firstName"].ToString() : "",
                            LastName = avatarData.ContainsKey("lastName") ? avatarData["lastName"].ToString() : "",
                            CreatedDate = avatarData.ContainsKey("createdDate") && DateTime.TryParse(avatarData["createdDate"].ToString(), out var created) ? created : DateTime.UtcNow,
                            ModifiedDate = DateTime.UtcNow,
                            Version = version
                        };

                        response.Result = avatar;
                        response.IsError = false;
                        response.Message = "Avatar loaded successfully from PLAN network by username";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to deserialize avatar data from PLAN network API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar from PLAN network: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by username from PLAN: {ex.Message}");
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
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate PLAN provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Load avatar detail from PLAN network API
                var planUrl = $"{_apiBaseUrl}/avatar-details/{id}?version={version}";
                var httpResponse = await _httpClient.GetAsync(planUrl);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
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
                        response.Message = "Avatar detail loaded successfully from PLAN network";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to deserialize avatar detail data from PLAN network API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar detail from PLAN network: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar detail from PLAN: {ex.Message}");
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
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate PLAN provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Load avatar detail by email from PLAN network API
                var planUrl = $"{_apiBaseUrl}/avatar-details/email/{Uri.EscapeDataString(avatarEmail)}?version={version}";
                var httpResponse = await _httpClient.GetAsync(planUrl);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
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
                        response.Message = "Avatar detail loaded successfully from PLAN network by email";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to deserialize avatar detail data from PLAN network API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar detail from PLAN network: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar detail by email from PLAN: {ex.Message}");
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
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate PLAN provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Load avatar detail by username from PLAN network API
                var planUrl = $"{_apiBaseUrl}/avatar-details/username/{Uri.EscapeDataString(avatarUsername)}?version={version}";
                var httpResponse = await _httpClient.GetAsync(planUrl);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var avatarDetailData = JsonSerializer.Deserialize<Dictionary<string, object>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (avatarDetailData != null)
                    {
                        var avatarDetail = new AvatarDetail
                        {
                            Id = avatarDetailData.ContainsKey("id") && Guid.TryParse(avatarDetailData["id"].ToString(), out var id) ? id : Guid.NewGuid(),
                            Username = avatarUsername,
                            Email = avatarDetailData.ContainsKey("email") ? avatarDetailData["email"].ToString() : $"{avatarUsername}@plan.example",
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
                        response.Message = "Avatar detail loaded successfully from PLAN network by username";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to deserialize avatar detail data from PLAN network API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar detail from PLAN network: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar detail by username from PLAN: {ex.Message}");
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
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate PLAN provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Load all avatars from PLAN network API
                var planUrl = $"{_apiBaseUrl}/avatars?version={version}";
                var httpResponse = await _httpClient.GetAsync(planUrl);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
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
                        response.Message = $"Successfully loaded {avatars.Count} avatars from PLAN network";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to deserialize avatars data from PLAN network API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatars from PLAN network: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading all avatars from PLAN: {ex.Message}");
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
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate PLAN provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Load all avatar details from PLAN network API
                var planUrl = $"{_apiBaseUrl}/avatar-details?version={version}";
                var httpResponse = await _httpClient.GetAsync(planUrl);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var avatarDetailsList = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (avatarDetailsList != null)
                    {
                        var avatarDetails = new List<IAvatarDetail>();
                        foreach (var avatarDetailData in avatarDetailsList)
                        {
                            var avatarDetail = new AvatarDetail
                            {
                                Id = avatarDetailData.ContainsKey("id") && Guid.TryParse(avatarDetailData["id"].ToString(), out var id) ? id : Guid.NewGuid(),
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
                            avatarDetails.Add(avatarDetail);
                        }

                        response.Result = avatarDetails;
                        response.IsError = false;
                        response.Message = $"Successfully loaded {avatarDetails.Count} avatar details from PLAN network";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to deserialize avatar details data from PLAN network API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar details from PLAN network: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading all avatar details from PLAN: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            return LoadAllAvatarDetailsAsync(version).Result;
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

    }
}
