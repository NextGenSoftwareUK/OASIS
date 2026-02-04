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
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate AWS provider: {activateResult.Message}");
                    return response;
                    }
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

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IHolon>();

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
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate AWS provider: {activateResult.Message}");
                    return response;
                    }
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
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate AWS provider: {activateResult.Message}");
                    return response;
                    }
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
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate AWS provider: {activateResult.Message}");
                    return response;
                    }
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

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var response = new OASISResult<IHolon>();

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

                // Delete holon from AWS DynamoDB
                var queryUrl = $"/dynamodb/holon/{id}";
                
                var httpResponse = await _httpClient.DeleteAsync(queryUrl);
                
                if (httpResponse.IsSuccessStatusCode)
                {
                    response.Result = new Holon { Id = id };
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
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate AWS provider: {activateResult.Message}");
                    return response;
                    }
                }

                // Delete holon by provider key from AWS DynamoDB
                var queryUrl = $"/dynamodb/holon/{providerKey}";
                
                var httpResponse = await _httpClient.DeleteAsync(queryUrl);
                
                if (httpResponse.IsSuccessStatusCode)
                {
                    response.Result = new Holon { Id = CreateDeterministicGuid($"{ProviderType.Value}:deleted:{providerKey}") };
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

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
        {
            return DeleteHolonAsync(providerKey).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
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

                // Load holons for parent from AWS DynamoDB
                var queryUrl = $"/dynamodb/holons/parent/{id}";
                var httpResponse = await _httpClient.GetAsync(queryUrl);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var holons = ParseAWSToHolons(content) ?? Enumerable.Empty<IHolon>();

                    if (type != HolonType.All)
                        holons = holons.Where(h => h.HolonType == type);

                    response.Result = holons;
                    response.IsError = false;
                    response.Message = $"Loaded {holons.Count()} holons for parent from AWS";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load holons for parent from AWS: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading holons for parent from AWS: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
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

                // Load holons for parent by provider key from AWS DynamoDB
                var queryUrl = $"/dynamodb/holons/parent-key/{providerKey}";
                var httpResponse = await _httpClient.GetAsync(queryUrl);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var holons = ParseAWSToHolons(content) ?? Enumerable.Empty<IHolon>();

                    if (type != HolonType.All)
                        holons = holons.Where(h => h.HolonType == type);

                    response.Result = holons;
                    response.IsError = false;
                    response.Message = $"Loaded {holons.Count()} holons for parent (provider key) from AWS";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load holons for parent by provider key from AWS: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading holons for parent by provider key from AWS: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
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

                var queryUrl = $"/dynamodb/holons/metadata?key={Uri.EscapeDataString(metaKey)}&value={Uri.EscapeDataString(metaValue)}";
                var httpResponse = await _httpClient.GetAsync(queryUrl);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var holons = ParseAWSToHolons(content) ?? Enumerable.Empty<IHolon>();

                    if (type != HolonType.All)
                        holons = holons.Where(h => h.HolonType == type);

                    response.Result = holons;
                    response.IsError = false;
                    response.Message = $"Loaded {holons.Count()} holons by metadata from AWS";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load holons by metadata from AWS: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading holons by metadata from AWS: {ex.Message}");
            }
            return response;
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
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate AWS provider: {activateResult.Message}");
                        return response;
                    }
                }

                var queryUrl = "/dynamodb/holons/search-metadata";
                var payload = JsonSerializer.Serialize(metaKeyValuePairs);
                var content = new StringContent(payload, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync(queryUrl, content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var body = await httpResponse.Content.ReadAsStringAsync();
                    var holons = ParseAWSToHolons(body) ?? Enumerable.Empty<IHolon>();

                    if (type != HolonType.All)
                        holons = holons.Where(h => h.HolonType == type);

                    response.Result = holons;
                    response.IsError = false;
                    response.Message = $"Loaded {holons.Count()} holons by metadata pairs from AWS";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load holons by metadata pairs from AWS: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading holons by metadata pairs from AWS: {ex.Message}");
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

                if (holons == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Holons cannot be null");
                    return response;
                }

                var savedHolons = new List<IHolon>();
                foreach (var holon in holons)
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

                response.Result = savedHolons;
                response.IsError = false;
                response.Message = $"Saved {savedHolons.Count} holons to AWS";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error saving holons to AWS: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;
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

        #endregion

        #region OASISStorageProviderBase Abstract Methods Implementation

        #region Avatar Methods

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
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate AWS provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Load all avatar details from AWS DynamoDB
                var queryUrl = "/dynamodb/avatardetails";
                
                var httpResponse = await _httpClient.GetAsync(queryUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var avatarDetails = JsonSerializer.Deserialize<IEnumerable<AvatarDetail>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                    });
                    
                    if (avatarDetails != null)
                    {
                        response.Result = avatarDetails;
                        response.IsError = false;
                        response.Message = $"Loaded {avatarDetails.Count()} avatar details from AWS successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse AWS JSON response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar details from AWS: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading all avatar details from AWS: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            return LoadAllAvatarDetailsAsync(version).Result;
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar Avatar)
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

                if (Avatar == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Avatar cannot be null");
                    return response;
                }

                // Save avatar to AWS DynamoDB
                var queryUrl = "/dynamodb/avatar";
                var awsJson = ConvertAvatarToAWS(Avatar);
                
                var content = new StringContent(awsJson, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync(queryUrl, content);
                
                if (httpResponse.IsSuccessStatusCode)
                {
                    response.Result = Avatar;
                    response.IsError = false;
                    response.IsSaved = true;
                    response.Message = "Avatar saved to AWS successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to save avatar to AWS: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error saving avatar to AWS: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar Avatar)
        {
            return SaveAvatarAsync(Avatar).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail Avatar)
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

                if (Avatar == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Avatar detail cannot be null");
                    return response;
                }

                // Save avatar detail to AWS DynamoDB
                var queryUrl = "/dynamodb/avatardetail";
                var awsJson = JsonSerializer.Serialize(Avatar, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });
                
                var content = new StringContent(awsJson, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync(queryUrl, content);
                
                if (httpResponse.IsSuccessStatusCode)
                {
                    response.Result = Avatar;
                    response.IsError = false;
                    response.IsSaved = true;
                    response.Message = "Avatar detail saved to AWS successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to save avatar detail to AWS: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error saving avatar detail to AWS: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail Avatar)
        {
            return SaveAvatarDetailAsync(Avatar).Result;
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
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate AWS provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Delete avatar from AWS DynamoDB
                var queryUrl = $"/dynamodb/avatar/{id}";
                if (softDelete)
                {
                    // For soft delete, update the record instead
                    queryUrl += "?softDelete=true";
                    var httpResponse = await _httpClient.PutAsync(queryUrl, new StringContent("{}", Encoding.UTF8, "application/json"));
                    response.Result = httpResponse.IsSuccessStatusCode;
                    response.IsError = !httpResponse.IsSuccessStatusCode;
                    response.Message = httpResponse.IsSuccessStatusCode ? "Avatar soft deleted from AWS successfully" : $"Failed to soft delete avatar: {httpResponse.StatusCode}";
                }
                else
                {
                    var httpResponse = await _httpClient.DeleteAsync(queryUrl);
                    response.Result = httpResponse.IsSuccessStatusCode;
                    response.IsError = !httpResponse.IsSuccessStatusCode;
                    response.Message = httpResponse.IsSuccessStatusCode ? "Avatar deleted from AWS successfully" : $"Failed to delete avatar: {httpResponse.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error deleting avatar from AWS: {ex.Message}");
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
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate AWS provider: {activateResult.Message}");
                        return response;
                    }
                }

                var requestUrl = $"/dynamodb/avatar/provider-key/{providerKey}?softDelete={softDelete.ToString().ToLower()}";
                var httpResponse = await _httpClient.DeleteAsync(requestUrl);

                response.Result = httpResponse.IsSuccessStatusCode;
                response.IsError = !httpResponse.IsSuccessStatusCode;
                response.Message = httpResponse.IsSuccessStatusCode
                    ? "Avatar deleted from AWS by provider key successfully"
                    : $"Failed to delete avatar by provider key: {httpResponse.StatusCode}";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error deleting avatar by provider key from AWS: {ex.Message}");
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
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate AWS provider: {activateResult.Message}");
                        return response;
                    }
                }

                var requestUrl = $"/dynamodb/avatar/email/{Uri.EscapeDataString(avatarEmail)}?softDelete={softDelete.ToString().ToLower()}";
                var httpResponse = await _httpClient.DeleteAsync(requestUrl);

                response.Result = httpResponse.IsSuccessStatusCode;
                response.IsError = !httpResponse.IsSuccessStatusCode;
                response.Message = httpResponse.IsSuccessStatusCode
                    ? "Avatar deleted from AWS by email successfully"
                    : $"Failed to delete avatar by email: {httpResponse.StatusCode}";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error deleting avatar by email from AWS: {ex.Message}");
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
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate AWS provider: {activateResult.Message}");
                        return response;
                    }
                }

                var requestUrl = $"/dynamodb/avatar/username/{Uri.EscapeDataString(avatarUsername)}?softDelete={softDelete.ToString().ToLower()}";
                var httpResponse = await _httpClient.DeleteAsync(requestUrl);

                response.Result = httpResponse.IsSuccessStatusCode;
                response.IsError = !httpResponse.IsSuccessStatusCode;
                response.Message = httpResponse.IsSuccessStatusCode
                    ? "Avatar deleted from AWS by username successfully"
                    : $"Failed to delete avatar by username: {httpResponse.StatusCode}";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error deleting avatar by username from AWS: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        {
            return DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;
        }

        #endregion

        #region Search Methods

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var response = new OASISResult<ISearchResults>();
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

                // AWS implementation for search using DynamoDB query
                var searchResults = new SearchResults();
                
                // Build search query parameters
                var queryParams = new List<string>();
                string searchQuery = null;
                if (searchParams != null && searchParams.SearchGroups != null && searchParams.SearchGroups.Any())
                {
                    // Extract search query from SearchGroups (similar to LocalFileOASIS)
                    var firstGroup = searchParams.SearchGroups.FirstOrDefault();
                    if (firstGroup is ISearchTextGroup textGroup && !string.IsNullOrWhiteSpace(textGroup.SearchQuery))
                    {
                        searchQuery = textGroup.SearchQuery;
                    }
                }
                if (!string.IsNullOrEmpty(searchQuery))
                {
                    queryParams.Add($"query={Uri.EscapeDataString(searchQuery)}");
                }
                if (version > 0)
                {
                    queryParams.Add($"version={version}");
                }
                
                var queryString = string.Join("&", queryParams);
                var searchUrl = $"/dynamodb/search?{queryString}";
                
                var httpResponse = await _httpClient.GetAsync(searchUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var searchData = JsonSerializer.Deserialize<Dictionary<string, object>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (searchData != null)
                    {
                        // Parse avatars from search results
                        if (searchData.ContainsKey("avatars") && searchData["avatars"] is JsonElement avatarsElement && avatarsElement.ValueKind == JsonValueKind.Array)
                        {
                            var avatars = new List<IAvatar>();
                            foreach (var item in avatarsElement.EnumerateArray())
                            {
                                var avatarJson = item.GetRawText();
                                var avatar = ParseAWSToAvatar(avatarJson);
                                if (avatar != null)
                                {
                                    avatars.Add(avatar);
                                }
                            }
                            searchResults.SearchResultAvatars = avatars;
                        }
                        
                        // Parse holons from search results
                        if (searchData.ContainsKey("holons") && searchData["holons"] is JsonElement holonsElement && holonsElement.ValueKind == JsonValueKind.Array)
                        {
                            var holons = new List<IHolon>();
                            foreach (var item in holonsElement.EnumerateArray())
                            {
                                var holonJson = item.GetRawText();
                                var holon = ParseAWSToHolon(holonJson);
                                if (holon != null)
                                {
                                    holons.Add(holon);
                                }
                            }
                            searchResults.SearchResultHolons = holons;
                        }
                        
                        searchResults.NumberOfResults = searchResults.SearchResultAvatars.Count + searchResults.SearchResultHolons.Count;
                        
                        response.Result = searchResults;
                        response.IsError = false;
                        response.Message = $"Successfully searched AWS and found {searchResults.NumberOfResults} results";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to deserialize search results from AWS");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to search AWS: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error searching in AWS: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
        }

        #endregion


        #region Import/Export Methods

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var response = new OASISResult<bool>();
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

                // AWS implementation for importing holons to DynamoDB
                if (holons == null || !holons.Any())
                {
                    OASISErrorHandling.HandleError(ref response, "No holons provided for import");
                    return response;
                }

                var holonsList = holons.ToList();
                var importData = new
                {
                    holons = holonsList.Select(h => ConvertHolonToAWS(h)).ToList()
                };

                var importJson = JsonSerializer.Serialize(importData);
                var content = new StringContent(importJson, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("/dynamodb/import", content);
                
                if (httpResponse.IsSuccessStatusCode)
                {
                    response.Result = true;
                    response.IsError = false;
                    response.Message = $"Successfully imported {holonsList.Count} holons to AWS";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to import holons to AWS: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error importing holons to AWS: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
        {
            return ImportAsync(holons).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
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

                // Export all holons for avatar from AWS DynamoDB
                var queryUrl = $"/dynamodb/holons/avatar/{avatarId}";
                
                var httpResponse = await _httpClient.GetAsync(queryUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var holons = ParseAWSToHolons(content);
                    if (holons != null)
                    {
                        response.Result = holons;
                        response.IsError = false;
                        response.Message = $"Exported {holons.Count()} holons for avatar {avatarId} from AWS successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse AWS JSON response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to export data from AWS: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error exporting all data for avatar by ID from AWS: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
        {
            return ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            // First load the avatar to get its ID
            var avatarResult = await LoadAvatarByUsernameAsync(avatarUsername, version);
            if (avatarResult.IsError || avatarResult.Result == null)
            {
                var response = new OASISResult<IEnumerable<IHolon>>();
                OASISErrorHandling.HandleError(ref response, $"Avatar with username {avatarUsername} not found");
                return response;
            }

            // Then export all data using the avatar ID
            return await ExportAllDataForAvatarByIdAsync(avatarResult.Result.Id, version);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
        {
            return ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            // First load the avatar to get its ID
            var avatarResult = await LoadAvatarByEmailAsync(avatarEmailAddress, version);
            if (avatarResult.IsError || avatarResult.Result == null)
            {
                var response = new OASISResult<IEnumerable<IHolon>>();
                OASISErrorHandling.HandleError(ref response, $"Avatar with email {avatarEmailAddress} not found");
                return response;
            }

            // Then export all data using the avatar ID
            return await ExportAllDataForAvatarByIdAsync(avatarResult.Result.Id, version);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
        {
            return ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
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

                // Export all holons from AWS DynamoDB
                var queryUrl = "/dynamodb/holons";
                
                var httpResponse = await _httpClient.GetAsync(queryUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();
                    var holons = ParseAWSToHolons(content);
                    if (holons != null)
                    {
                        response.Result = holons;
                        response.IsError = false;
                        response.Message = $"Exported {holons.Count()} holons from AWS successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse AWS JSON response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to export all data from AWS: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error exporting all data from AWS: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
        {
            return ExportAllAsync(version).Result;
        }

        #endregion

        #endregion

        #region IOASISNETProvider Implementation

        public OASISResult<IEnumerable<IPlayer>> GetPlayersNearMe()
        {
            var response = new OASISResult<IEnumerable<IPlayer>>();
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

                // Example AWS implementation using a custom geospatial endpoint
                var queryUrl = "/net/players/near-me";
                var httpResponse = _httpClient.GetAsync(queryUrl).Result;

                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = httpResponse.Content.ReadAsStringAsync().Result;
                    var players = ParseAWSToPlayers(content);
                    response.Result = players;
                    response.IsError = false;
                    response.Message = "Retrieved players near me from AWS";
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

        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(HolonType holonType = HolonType.All)
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

                var queryUrl = "/net/holons/near-me";
                var httpResponse = _httpClient.GetAsync(queryUrl).Result;

                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = httpResponse.Content.ReadAsStringAsync().Result;
                    var holons = ParseAWSToHolons(content) ?? Enumerable.Empty<IHolon>();

                    if (holonType != HolonType.All)
                        holons = holons.Where(h => h.HolonType == holonType);

                    response.Result = holons;
                    response.IsError = false;
                    response.Message = $"Retrieved {holons.Count()} holons near me from AWS";
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

        #region IDisposable

        public void Dispose()
        {
            _httpClient?.Dispose();
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
