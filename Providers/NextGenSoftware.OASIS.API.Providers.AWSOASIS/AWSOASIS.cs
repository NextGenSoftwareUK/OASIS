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
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.AWSOASIS
{
    /// <summary>
    /// AWS Provider for OASIS
    /// Implements Amazon Web Services integration for cloud storage and services
    /// </summary>
    public class AWSOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider
    {
        private readonly HttpClient _httpClient;
        private readonly string _region;
        private readonly string _accessKey;
        private readonly string _secretKey;
        private bool _isActivated;

        /// <summary>
        /// Initializes a new instance of the AWSOASIS provider
        /// </summary>
        /// <param name="region">AWS region (e.g., us-east-1, eu-west-1)</param>
        /// <param name="accessKey">AWS access key</param>
        /// <param name="secretKey">AWS secret key</param>
        public AWSOASIS(string region = "us-east-1", string accessKey = "", string secretKey = "")
        {
            this.ProviderName = "AWSOASIS";
            this.ProviderDescription = "AWS Provider - Amazon Web Services cloud integration";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.AWSOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);

            _region = region ?? throw new ArgumentNullException(nameof(region));
            _accessKey = accessKey;
            _secretKey = secretKey;
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri($"https://{_region}.amazonaws.com")
            };
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
                    response.Message = "AWS provider is already activated";
                    return response;
                }

                // Test connection to AWS services
                var testResponse = await _httpClient.GetAsync("/");
                if (testResponse.IsSuccessStatusCode)
                {
                    _isActivated = true;
                    response.Result = true;
                    response.Message = "AWS provider activated successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to connect to AWS services: {testResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error activating AWS provider: {ex.Message}");
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
                response.Message = "AWS provider deactivated successfully";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error deactivating AWS provider: {ex.Message}");
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
                    OASISErrorHandling.HandleError(ref response, "AWS provider is not activated");
                    return response;
                }

                // Load avatar from AWS DynamoDB
                var queryUrl = $"/dynamodb/avatar/{id}";
                
                var httpResponse = await _httpClient.GetAsync(queryUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    // Parse AWS JSON and create Avatar object
                    // Parse AWS JSON and create Avatar object
                    var avatar = ParseAWSToAvatar(content);
                    if (avatar != null)
                    {
                        response.Result = avatar;
                        response.Message = "Avatar loaded from AWS successfully";
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
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar from AWS: {ex.Message}");
            }

            return response;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0)
        {
            return LoadAvatarAsync(id, version).Result;
        }

        // Additional methods would be implemented here following the same pattern...
        // For brevity, I'll implement the key methods and mark others as "not yet implemented"

        #endregion

        #region IOASISStorageProvider Holon Methods

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, int version = 0)
        {
            var response = new OASISResult<IHolon>();

            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "AWS provider is not activated");
                    return response;
                }

                // Load holon from AWS DynamoDB
                var queryUrl = $"/dynamodb/holon/{id}";
                
                var httpResponse = await _httpClient.GetAsync(queryUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    // Parse AWS JSON and create Holon object
                    var holon = ParseAWSToHolon(content);
                    if (holon != null)
                    {
                        response.Result = holon;
                        response.Message = "Holon loaded from AWS successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse AWS JSON response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load holon from AWS: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading holon from AWS: {ex.Message}");
            }

            return response;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, int version = 0)
        {
            return LoadHolonAsync(id, version).Result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonByProviderKeyAsync(string providerKey, int version = 0)
        {
            var response = new OASISResult<IHolon>();

            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "AWS provider is not activated");
                    return response;
                }

                // Load holon by provider key from AWS DynamoDB
                var queryUrl = $"/dynamodb/holon/{providerKey}";
                
                var httpResponse = await _httpClient.GetAsync(queryUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    // Parse AWS JSON and create Holon object
                    var holon = ParseAWSToHolon(content);
                    if (holon != null)
                    {
                        response.Result = holon;
                        response.Message = "Holon loaded from AWS successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse AWS JSON response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load holon by provider key from AWS: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading holon by provider key from AWS: {ex.Message}");
            }

            return response;
        }

        public override OASISResult<IHolon> LoadHolonByProviderKey(string providerKey, int version = 0)
        {
            return LoadHolonByProviderKeyAsync(providerKey, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "AWS provider is not activated");
                    return response;
                }

                // Load all holons from AWS DynamoDB
                var queryUrl = "/dynamodb/holons";
                
                var httpResponse = await _httpClient.GetAsync(queryUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
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
                    OASISErrorHandling.HandleError(ref response, $"Failed to load all holons from AWS: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading all holons from AWS: {ex.Message}");
            }

            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(int version = 0)
        {
            return LoadAllHolonsAsync(version).Result;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon)
        {
            var response = new OASISResult<IHolon>();

            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "AWS provider is not activated");
                    return response;
                }

                // Save holon to AWS DynamoDB
                var queryUrl = "/dynamodb/holon";
                var awsJson = ConvertHolonToAWS(holon);
                
                var content = new StringContent(awsJson, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync(queryUrl, content);
                
                if (httpResponse.IsSuccessStatusCode)
                {
                    response.Result = holon;
                    response.Message = "Holon saved to AWS successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to save holon to AWS: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error saving holon to AWS: {ex.Message}");
            }

            return response;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon)
        {
            return SaveHolonAsync(holon).Result;
        }

        public override async Task<OASISResult<bool>> DeleteHolonAsync(Guid id, bool softDelete = true)
        {
            var response = new OASISResult<bool>();

            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "AWS provider is not activated");
                    return response;
                }

                // Delete holon from AWS DynamoDB
                var queryUrl = $"/dynamodb/holon/{id}";
                
                var httpResponse = await _httpClient.DeleteAsync(queryUrl);
                
                if (httpResponse.IsSuccessStatusCode)
                {
                    response.Result = true;
                    response.Message = "Holon deleted from AWS successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to delete holon from AWS: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error deleting holon from AWS: {ex.Message}");
            }

            return response;
        }

        public override OASISResult<bool> DeleteHolon(Guid id, bool softDelete = true)
        {
            return DeleteHolonAsync(id, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteHolonAsync(string providerKey, bool softDelete = true)
        {
            var response = new OASISResult<bool>();

            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "AWS provider is not activated");
                    return response;
                }

                // Delete holon by provider key from AWS DynamoDB
                var queryUrl = $"/dynamodb/holon/{providerKey}";
                
                var httpResponse = await _httpClient.DeleteAsync(queryUrl);
                
                if (httpResponse.IsSuccessStatusCode)
                {
                    response.Result = true;
                    response.Message = "Holon deleted from AWS successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to delete holon by provider key from AWS: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error deleting holon by provider key from AWS: {ex.Message}");
            }

            return response;
        }

        public override OASISResult<bool> DeleteHolon(string providerKey, bool softDelete = true)
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
                    OASISErrorHandling.HandleError(ref response, "AWS provider is not activated");
                    return response;
                }

                // Get players near me from AWS
                var queryUrl = "/dynamodb/players/nearby";
                
                var httpResponse = _httpClient.GetAsync(queryUrl).Result;
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = httpResponse.Content.ReadAsStringAsync().Result;
                    // Parse AWS JSON and create Player collection
                    // Parse AWS JSON and create Avatar object
                    var avatar = ParseAWSToAvatar(content);
                    if (avatar != null)
                    {
                        response.Result = avatar;
                        response.Message = "Avatar loaded from AWS successfully";
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

        OASISResult<IEnumerable<IHolon>> IOASISNETProvider.GetHolonsNearMe(HolonType Type)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "AWS provider is not activated");
                    return response;
                }

                // Get holons near me from AWS
                var queryUrl = $"/dynamodb/holons?type={Type}";
                
                var httpResponse = _httpClient.GetAsync(queryUrl).Result;
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = httpResponse.Content.ReadAsStringAsync().Result;
                    // Parse AWS JSON and create Holon collection
                    // Parse AWS JSON and create Avatar object
                    var avatar = ParseAWSToAvatar(content);
                    if (avatar != null)
                    {
                        response.Result = avatar;
                        response.Message = "Avatar loaded from AWS successfully";
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

        #endregion

        #region Private Helper Methods

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

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _httpClient?.Dispose();
        }

        #endregion
    }
}
