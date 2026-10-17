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
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using System.Text.Json.Serialization;

namespace NextGenSoftware.OASIS.API.Providers.AWSOASIS
{
    public partial class AWSOASIS
    {
        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }



        OASISResult<IEnumerable<IAvatar>> IOASISNETProvider.GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            var response = new OASISResult<IEnumerable<IAvatar>>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = ActivateProviderAsync().Result;
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate AWS provider: {activateResult.Message}");
                    return response;
                    }
                }

                // Get players near me from AWS
                var queryUrl = "/dynamodb/players/nearby";
                
                var httpResponse = _httpClient.GetAsync(queryUrl).Result;
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = httpResponse.Content.ReadAsStringAsync().Result;
                    // Parse AWS JSON and create Avatar collection
                    var avatars = ParseAWSToAvatars(content);
                    if (avatars != null)
                    {
                        response.Result = avatars;
                        response.Message = "Players loaded from AWS successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse AWS JSON response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to get players near me from AWS: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error getting players near me from AWS: {ex.Message}");
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
                    var activateResult = ActivateProviderAsync().Result;
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate AWS provider: {activateResult.Message}");
                    return response;
                    }
                }

                // Get holons near me from AWS
                var queryUrl = $"/dynamodb/holons?type={Type}";
                
                var httpResponse = _httpClient.GetAsync(queryUrl).Result;
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = httpResponse.Content.ReadAsStringAsync().Result;
                    // Parse AWS JSON and create Holon collection
                    var holons = ParseAWSToHolons(content);
                    if (holons != null)
                    {
                        response.Result = holons;
                        response.Message = "Holons loaded from AWS successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse AWS JSON response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to get holons near me from AWS: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error getting holons near me from AWS: {ex.Message}");
            }

            return response;
        }



        /// <summary>
        /// Parse AWS JSON content and convert to OASIS Avatar
        /// </summary>
        private IAvatar ParseAWSToAvatar(string awsJson)
        {
            try
            {
                // Deserialize the complete Avatar object to preserve all properties
                var avatar = JsonSerializer.Deserialize<Avatar>(awsJson, new JsonSerializerOptions
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
        /// Convert OASIS Avatar to AWS JSON format
        /// </summary>
        private string ConvertAvatarToAWS(IAvatar avatar)
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
                    ""id"": ""{avatar.Id}"",
                    ""username"": ""{avatar.Username}"",
                    ""email"": ""{avatar.Email}"",
                    ""created"": ""{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}""
                }}";
            }
        }

        /// <summary>
        /// Convert OASIS Holon to AWS JSON format
        /// </summary>
        private string ConvertHolonToAWS(IHolon holon)
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
                    ""id"": ""{holon.Id}"",
                    ""name"": ""{holon.Name}"",
                    ""description"": ""{holon.Description}"",
                    ""created"": ""{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}""
                }}";
            }
        }

        /// <summary>
        /// Parse AWS JSON content and convert to OASIS Holon
        /// </summary>
        private IHolon ParseAWSToHolon(string awsJson)
        {
            try
            {
                // Deserialize the complete Holon object to preserve all properties
                var holon = JsonSerializer.Deserialize<Holon>(awsJson, new JsonSerializerOptions
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
        /// Parse AWS JSON content and convert to OASIS Holon collection
        /// </summary>
        private IEnumerable<IHolon> ParseAWSToHolons(string awsJson)
        {
            try
            {
                // Deserialize the complete Holon collection to preserve all properties
                var holons = JsonSerializer.Deserialize<IEnumerable<Holon>>(awsJson, new JsonSerializerOptions
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

        /// <summary>
        /// Parse AWS JSON content and convert to OASIS Player collection
        /// </summary>
        private IEnumerable<IPlayer> ParseAWSToPlayers(string awsJson)
        {
            try
            {
                // For now, return empty collection as Player interface needs to be defined
                return new List<IPlayer>();
            }
            catch (Exception)
            {
                return new List<IPlayer>();
            }
        }

        /// <summary>
        /// Parse AWS JSON content and convert to OASIS Avatar collection
        /// </summary>
        private IEnumerable<IAvatar> ParseAWSToAvatars(string awsJson)
        {
            try
            {
                // Deserialize the complete Avatar collection to preserve all properties
                var avatars = JsonSerializer.Deserialize<IEnumerable<Avatar>>(awsJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });
                
                return avatars;
            }
            catch (Exception)
            {
                // Return empty collection if parsing fails
                return new List<IAvatar>();
            }
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
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate AWS provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Load all avatars from AWS DynamoDB
                var queryUrl = "/dynamodb/avatars";
                
                var httpResponse = await _httpClient.GetAsync(queryUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var avatars = ParseAWSToAvatars(content);
                    if (avatars != null)
                    {
                        response.Result = avatars;
                        response.IsError = false;
                        response.Message = $"Loaded {avatars.Count()} avatars from AWS successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse AWS JSON response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatars from AWS: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading all avatars from AWS: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            return LoadAllAvatarsAsync(version).Result;
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
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate AWS provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Load avatar from AWS DynamoDB by provider key
                var queryUrl = $"/dynamodb/avatar/providerkey/{Uri.EscapeDataString(providerKey)}";
                
                var httpResponse = await _httpClient.GetAsync(queryUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var avatar = ParseAWSToAvatar(content);
                    if (avatar != null)
                    {
                        response.Result = avatar;
                        response.IsError = false;
                        response.Message = "Avatar loaded from AWS by provider key successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse AWS JSON response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar from AWS: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by provider key from AWS: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
        {
            return LoadAvatarByProviderKeyAsync(providerKey, version).Result;
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
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate AWS provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Load avatar from AWS DynamoDB by username
                var queryUrl = $"/dynamodb/avatar/username/{Uri.EscapeDataString(avatarUsername)}";
                
                var httpResponse = await _httpClient.GetAsync(queryUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var avatar = ParseAWSToAvatar(content);
                    if (avatar != null)
                    {
                        response.Result = avatar;
                        response.IsError = false;
                        response.Message = "Avatar loaded from AWS by username successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse AWS JSON response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar from AWS: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by username from AWS: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarByUsernameAsync(avatarUsername, version).Result;
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
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate AWS provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Load avatar from AWS DynamoDB by email
                var queryUrl = $"/dynamodb/avatar/email/{Uri.EscapeDataString(avatarEmail)}";
                
                var httpResponse = await _httpClient.GetAsync(queryUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var avatar = ParseAWSToAvatar(content);
                    if (avatar != null)
                    {
                        response.Result = avatar;
                        response.IsError = false;
                        response.Message = "Avatar loaded from AWS by email successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse AWS JSON response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar from AWS: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by email from AWS: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
        {
            return LoadAvatarByEmailAsync(avatarEmail, version).Result;
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
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate AWS provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Load avatar detail from AWS DynamoDB
                var queryUrl = $"/dynamodb/avatardetail/{id}";
                
                var httpResponse = await _httpClient.GetAsync(queryUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var avatarDetail = JsonSerializer.Deserialize<AvatarDetail>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                    });
                    
                    if (avatarDetail != null)
                    {
                        response.Result = avatarDetail;
                        response.IsError = false;
                        response.Message = "Avatar detail loaded from AWS successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse AWS JSON response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar detail from AWS: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar detail from AWS: {ex.Message}");
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
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate AWS provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Load avatar detail from AWS DynamoDB by email
                var queryUrl = $"/dynamodb/avatardetail/email/{Uri.EscapeDataString(avatarEmail)}";
                
                var httpResponse = await _httpClient.GetAsync(queryUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var avatarDetail = JsonSerializer.Deserialize<AvatarDetail>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                    });
                    
                    if (avatarDetail != null)
                    {
                        response.Result = avatarDetail;
                        response.IsError = false;
                        response.Message = "Avatar detail loaded from AWS by email successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse AWS JSON response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar detail from AWS: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar detail by email from AWS: {ex.Message}");
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
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate AWS provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Load avatar detail from AWS DynamoDB by username
                var queryUrl = $"/dynamodb/avatardetail/username/{Uri.EscapeDataString(avatarUsername)}";
                
                var httpResponse = await _httpClient.GetAsync(queryUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var avatarDetail = JsonSerializer.Deserialize<AvatarDetail>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                    });
                    
                    if (avatarDetail != null)
                    {
                        response.Result = avatarDetail;
                        response.IsError = false;
                        response.Message = "Avatar detail loaded from AWS by username successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse AWS JSON response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar detail from AWS: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar detail by username from AWS: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarDetailByUsernameAsync(avatarUsername, version).Result;
        }

    }
}
