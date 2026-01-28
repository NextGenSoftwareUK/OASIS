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
    public class PLANOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider, IOASISSuperStar
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl;
        private readonly string _apiKey;
        private WalletManager _walletManager;

        public WalletManager WalletManager
        {
            get
            {
                if (_walletManager == null)
                    _walletManager = WalletManager.Instance;
                return _walletManager;
            }
            set => _walletManager = value;
        }

        public PLANOASIS(string apiBaseUrl = "https://api.plan-systems.org/v1", string apiKey = "")
        {
            this.ProviderName = "PLANOASIS";
            this.ProviderDescription = "PLAN Provider";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.PLANOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
            
            _apiBaseUrl = apiBaseUrl;
            _apiKey = apiKey;
            _httpClient = new HttpClient();
            
            if (!string.IsNullOrEmpty(_apiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
            }
        }

        #region IOASISStorageProvider Implementation

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
                // Load avatar by provider key from PLAN network
                OASISErrorHandling.HandleError(ref response, "PLAN avatar loading by provider key not yet implemented");
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
                // Load avatar by email from PLAN network
                OASISErrorHandling.HandleError(ref response, "PLAN avatar loading by email not yet implemented");
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
                // Load avatar by username from PLAN network
                OASISErrorHandling.HandleError(ref response, "PLAN avatar loading by username not yet implemented");
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
            return null;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            return null;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        {
            return null;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
        {
            return null;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        {
            return null;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
        {
            return null;
        }

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            return null;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            return null;
        }

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            return null;
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

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate PLAN provider: {activateResult.Message}");
                        return result;
                    }
                }

                var searchParams = new Dictionary<string, string>
                {
                    { "metaKey", metaKey },
                    { "metaValue", metaValue },
                    { "type", type.ToString() },
                    { "version", version.ToString() }
                };

                var queryString = string.Join("&", searchParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/holons/search?{queryString}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var holons = JsonSerializer.Deserialize<List<Holon>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (holons != null)
                    {
                        result.Result = holons.Cast<IHolon>();
                        result.IsError = false;
                        result.Message = $"Successfully loaded {holons.Count} holons by metadata from PLAN";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize holons from PLAN API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"PLAN API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata from PLAN: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate PLAN provider: {activateResult.Message}");
                        return result;
                    }
                }

                var searchRequest = new
                {
                    metaKeyValuePairs = metaKeyValuePairs,
                    matchMode = metaKeyValuePairMatchMode.ToString(),
                    type = type.ToString(),
                    version = version
                };

                var jsonContent = JsonSerializer.Serialize(searchRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/holons/search-multiple", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var holons = JsonSerializer.Deserialize<List<Holon>>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (holons != null)
                    {
                        result.Result = holons.Cast<IHolon>();
                        result.IsError = false;
                        result.Message = $"Successfully loaded {holons.Count} holons by multiple metadata from PLAN";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize holons from PLAN API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"PLAN API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons by multiple metadata from PLAN: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return null;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return null;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate PLAN provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (holon == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Holon cannot be null");
                    return result;
                }

                var jsonContent = JsonSerializer.Serialize(holon);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/holons", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var savedHolon = JsonSerializer.Deserialize<Holon>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (savedHolon != null)
                    {
                        result.Result = savedHolon;
                        result.IsError = false;
                        result.Message = "Holon saved successfully to PLAN";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize saved holon from PLAN API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"PLAN API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving holon to PLAN: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate PLAN provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (holons == null || !holons.Any())
                {
                    OASISErrorHandling.HandleError(ref result, "Holons collection cannot be null or empty");
                    return result;
                }

                var jsonContent = JsonSerializer.Serialize(holons);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/holons/batch", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var savedHolons = JsonSerializer.Deserialize<List<Holon>>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (savedHolons != null)
                    {
                        result.Result = savedHolons.Cast<IHolon>();
                        result.IsError = false;
                        result.Message = $"Successfully saved {savedHolons.Count} holons to PLAN";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize saved holons from PLAN API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"PLAN API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving holons to PLAN: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate PLAN provider: {activateResult.Message}");
                        return result;
                    }
                }

                var response = await _httpClient.DeleteAsync($"{_apiBaseUrl}/holons/{id}");

                if (response.IsSuccessStatusCode)
                {
                    result.Result = null; // Holon deleted
                    result.IsError = false;
                    result.Message = "Holon deleted successfully from PLAN";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"PLAN API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting holon from PLAN: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id)
        {
            return DeleteHolonAsync(id).Result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate PLAN provider: {activateResult.Message}");
                        return result;
                    }
                }

                var response = await _httpClient.DeleteAsync($"{_apiBaseUrl}/holons/by-provider-key/{Uri.EscapeDataString(providerKey)}");

                if (response.IsSuccessStatusCode)
                {
                    result.Result = null; // Holon deleted
                    result.IsError = false;
                    result.Message = "Holon deleted successfully from PLAN by provider key";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"PLAN API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting holon from PLAN by provider key: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
        {
            return DeleteHolonAsync(providerKey).Result;
        }

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate PLAN provider: {activateResult.Message}");
                        return result;
                    }
                }

                var searchUrl = $"{_apiBaseUrl}/search";
                
                // Extract search query and holon type from SearchGroups if available
                string searchQuery = "";
                string holonTypeStr = "All";
                
                if (searchParams?.SearchGroups != null && searchParams.SearchGroups.Count > 0)
                {
                    var firstGroup = searchParams.SearchGroups[0];
                    holonTypeStr = firstGroup.HolonType.ToString();
                    
                    // Try to get SearchQuery from SearchTextGroup if available
                    if (firstGroup is SearchTextGroup textGroup)
                    {
                        searchQuery = textGroup.SearchQuery ?? "";
                    }
                }
                
                var searchRequest = new
                {
                    query = searchQuery,
                    holonType = holonTypeStr,
                    version = version
                };

                var content = new StringContent(JsonSerializer.Serialize(searchRequest), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(searchUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var searchData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);

                    var searchResults = new SearchResults
                    {
                        SearchResultHolons = new List<IHolon>(),
                        SearchResultAvatars = new List<IAvatar>(),
                        NumberOfResults = 0,
                        NumberOfDuplicates = 0
                    };

                    if (searchData?.ContainsKey("results") == true && searchData["results"] is JsonElement resultsArray)
                    {
                        foreach (var item in resultsArray.EnumerateArray())
                        {
                            var holon = new Holon
                            {
                                Id = Guid.Parse(item.GetProperty("id").GetString() ?? Guid.Empty.ToString()),
                                Name = item.GetProperty("name").GetString() ?? "",
                                Description = item.TryGetProperty("description", out var desc) ? desc.GetString() : ""
                            };
                            searchResults.SearchResultHolons.Add(holon);
                        }
                        searchResults.NumberOfResults = searchResults.SearchResultHolons.Count;
                    }

                    result.Result = searchResults;
                    result.IsError = false;
                    result.Message = $"Search completed successfully, found {searchResults.NumberOfResults} results";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"PLAN search failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error searching PLAN: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
        }

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate PLAN provider: {activateResult.Message}");
                        return result;
                    }
                }

                var importUrl = $"{_apiBaseUrl}/import";
                var importedCount = 0;

                foreach (var holon in holons)
                {
                    var holonData = new
                    {
                        id = holon.Id.ToString(),
                        name = holon.Name,
                        description = holon.Description,
                        holonType = holon.HolonType.ToString(),
                        createdDate = holon.CreatedDate,
                        modifiedDate = holon.ModifiedDate,
                        version = holon.Version
                    };

                    var content = new StringContent(JsonSerializer.Serialize(holonData), Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync(importUrl, content);

                    if (response.IsSuccessStatusCode)
                    {
                        importedCount++;
                    }
                    else if (!result.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to import holon {holon.Id}: {response.StatusCode}");
                        return result;
                    }
                }

                result.Result = true;
                result.IsError = false;
                result.Message = $"Successfully imported {importedCount} holons to PLAN";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error importing holons to PLAN: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
        {
            return ImportAsync(holons).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate PLAN provider: {activateResult.Message}");
                        return result;
                    }
                }

                var exportUrl = $"{_apiBaseUrl}/export/avatar/{avatarId}?version={version}";
                var response = await _httpClient.GetAsync(exportUrl);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var exportData = JsonSerializer.Deserialize<Dictionary<string, object>>(content);

                    var holons = new List<IHolon>();
                    if (exportData?.ContainsKey("holons") == true && exportData["holons"] is JsonElement holonsArray)
                    {
                        foreach (var item in holonsArray.EnumerateArray())
                        {
                            var holon = new Holon
                            {
                                Id = Guid.Parse(item.GetProperty("id").GetString() ?? Guid.Empty.ToString()),
                                Name = item.GetProperty("name").GetString() ?? "",
                                Description = item.TryGetProperty("description", out var desc) ? desc.GetString() : "",
                                Version = item.TryGetProperty("version", out var ver) ? ver.GetInt32() : version
                            };
                            holons.Add(holon);
                        }
                    }

                    result.Result = holons;
                    result.IsError = false;
                    result.Message = $"Successfully exported {holons.Count} holons for avatar {avatarId} from PLAN";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"PLAN export failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data from PLAN: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
        {
            return ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate PLAN provider: {activateResult.Message}");
                        return result;
                    }
                }

                var exportUrl = $"{_apiBaseUrl}/export/avatar/username/{avatarUsername}?version={version}";
                var response = await _httpClient.GetAsync(exportUrl);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var exportData = JsonSerializer.Deserialize<Dictionary<string, object>>(content);

                    var holons = new List<IHolon>();
                    if (exportData?.ContainsKey("holons") == true && exportData["holons"] is JsonElement holonsArray)
                    {
                        foreach (var item in holonsArray.EnumerateArray())
                        {
                            var holon = new Holon
                            {
                                Id = Guid.Parse(item.GetProperty("id").GetString() ?? Guid.Empty.ToString()),
                                Name = item.GetProperty("name").GetString() ?? "",
                                Description = item.TryGetProperty("description", out var desc) ? desc.GetString() : "",
                                Version = item.TryGetProperty("version", out var ver) ? ver.GetInt32() : version
                            };
                            holons.Add(holon);
                        }
                    }

                    result.Result = holons;
                    result.IsError = false;
                    result.Message = $"Successfully exported {holons.Count} holons for avatar {avatarUsername} from PLAN";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"PLAN export failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data by username from PLAN: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
        {
            return ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate PLAN provider: {activateResult.Message}");
                        return result;
                    }
                }

                var exportUrl = $"{_apiBaseUrl}/export/avatar/email/{Uri.EscapeDataString(avatarEmailAddress)}?version={version}";
                var response = await _httpClient.GetAsync(exportUrl);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var exportData = JsonSerializer.Deserialize<Dictionary<string, object>>(content);

                    var holons = new List<IHolon>();
                    if (exportData?.ContainsKey("holons") == true && exportData["holons"] is JsonElement holonsArray)
                    {
                        foreach (var item in holonsArray.EnumerateArray())
                        {
                            var holon = new Holon
                            {
                                Id = Guid.Parse(item.GetProperty("id").GetString() ?? Guid.Empty.ToString()),
                                Name = item.GetProperty("name").GetString() ?? "",
                                Description = item.TryGetProperty("description", out var desc) ? desc.GetString() : "",
                                Version = item.TryGetProperty("version", out var ver) ? ver.GetInt32() : version
                            };
                            holons.Add(holon);
                        }
                    }

                    result.Result = holons;
                    result.IsError = false;
                    result.Message = $"Successfully exported {holons.Count} holons for avatar {avatarEmailAddress} from PLAN";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"PLAN export failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data by email from PLAN: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
        {
            return ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate PLAN provider: {activateResult.Message}");
                        return result;
                    }
                }

                var exportUrl = $"{_apiBaseUrl}/export/all?version={version}";
                var response = await _httpClient.GetAsync(exportUrl);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var exportData = JsonSerializer.Deserialize<Dictionary<string, object>>(content);

                    var holons = new List<IHolon>();
                    if (exportData?.ContainsKey("holons") == true && exportData["holons"] is JsonElement holonsArray)
                    {
                        foreach (var item in holonsArray.EnumerateArray())
                        {
                            var holon = new Holon
                            {
                                Id = Guid.Parse(item.GetProperty("id").GetString() ?? Guid.Empty.ToString()),
                                Name = item.GetProperty("name").GetString() ?? "",
                                Description = item.TryGetProperty("description", out var desc) ? desc.GetString() : "",
                                Version = item.TryGetProperty("version", out var ver) ? ver.GetInt32() : version
                            };
                            holons.Add(holon);
                        }
                    }

                    result.Result = holons;
                    result.IsError = false;
                    result.Message = $"Successfully exported {holons.Count} holons from PLAN";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"PLAN export failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting all data from PLAN: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
        {
            return ExportAllAsync(version).Result;
        }

        #endregion

        #region IOASISNET Implementation

        public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate PLAN provider: {activateResult.Message}");
                        return result;
                    }
                }

                var playersUrl = $"{_apiBaseUrl}/players/nearby?lat={geoLat}&lng={geoLong}&radius={radiusInMeters}";
                var response = _httpClient.GetAsync(playersUrl).Result;

                if (response.IsSuccessStatusCode)
                {
                    var content = response.Content.ReadAsStringAsync().Result;
                    var playersData = JsonSerializer.Deserialize<Dictionary<string, object>>(content);

                    var avatars = new List<IAvatar>();
                    if (playersData?.ContainsKey("players") == true && playersData["players"] is JsonElement playersArray)
                    {
                        foreach (var item in playersArray.EnumerateArray())
                        {
                            var avatar = new Avatar
                            {
                                Id = Guid.Parse(item.GetProperty("id").GetString() ?? Guid.Empty.ToString()),
                                Username = item.GetProperty("username").GetString() ?? "",
                                Email = item.TryGetProperty("email", out var email) ? email.GetString() : ""
                            };
                            avatars.Add(avatar);
                        }
                    }

                    result.Result = avatars;
                    result.IsError = false;
                    result.Message = $"Found {avatars.Count} avatars nearby from PLAN";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"PLAN GetAvatarsNearMe failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting avatars near me from PLAN: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate PLAN provider: {activateResult.Message}");
                        return result;
                    }
                }

                var holonsUrl = $"{_apiBaseUrl}/holons/nearby?type={Type}";
                var response = _httpClient.GetAsync(holonsUrl).Result;

                if (response.IsSuccessStatusCode)
                {
                    var content = response.Content.ReadAsStringAsync().Result;
                    var holonsData = JsonSerializer.Deserialize<Dictionary<string, object>>(content);

                    var holons = new List<IHolon>();
                    if (holonsData?.ContainsKey("holons") == true && holonsData["holons"] is JsonElement holonsArray)
                    {
                        foreach (var item in holonsArray.EnumerateArray())
                        {
                            var holon = new Holon
                            {
                                Id = Guid.Parse(item.GetProperty("id").GetString() ?? Guid.Empty.ToString()),
                                Name = item.GetProperty("name").GetString() ?? "",
                                Description = item.TryGetProperty("description", out var desc) ? desc.GetString() : "",
                                HolonType = Type
                            };
                            holons.Add(holon);
                        }
                    }

                    result.Result = holons;
                    result.IsError = false;
                    result.Message = $"Found {holons.Count} holons nearby from PLAN";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"PLAN GetHolonsNearMe failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting holons near me from PLAN: {ex.Message}", ex);
            }
            return result;
        }

        #endregion

        #region IOASISSuperStar
        public bool NativeCodeGenesis(ICelestialBody celestialBody, string outputFolder, string nativeSource)
        {
            try
            {
                if (string.IsNullOrEmpty(outputFolder))
                    return false;

                string planFolder = Path.Combine(outputFolder, "PLAN");
                if (!Directory.Exists(planFolder))
                    Directory.CreateDirectory(planFolder);

                if (!string.IsNullOrEmpty(nativeSource))
                {
                    File.WriteAllText(Path.Combine(planFolder, "plan.json"), nativeSource);
                    return true;
                }

                if (celestialBody == null)
                    return true;

                var sb = new StringBuilder();
                sb.AppendLine("{");
                sb.AppendLine($"  \"name\": \"{celestialBody.Name ?? "OAPP"}\",");
                sb.AppendLine($"  \"description\": \"{celestialBody.Description ?? ""}\",");
                sb.AppendLine("  \"holons\": [");

                var zomes = celestialBody.CelestialBodyCore?.Zomes;
                bool firstHolon = true;
                if (zomes != null)
                {
                    foreach (var zome in zomes)
                    {
                        if (zome?.Children == null) continue;

                        foreach (var holon in zome.Children)
                        {
                            if (holon == null || string.IsNullOrWhiteSpace(holon.Name)) continue;

                            if (!firstHolon) sb.AppendLine(",");
                            firstHolon = false;

                            sb.AppendLine("    {");
                            sb.AppendLine($"      \"id\": \"{holon.Id}\",");
                            sb.AppendLine($"      \"name\": \"{holon.Name}\",");
                            sb.AppendLine($"      \"description\": \"{holon.Description ?? ""}\"");
                            sb.AppendLine();
                            sb.Append("    }");
                        }
                    }
                }

                sb.AppendLine();
                sb.AppendLine("  ]");
                sb.AppendLine("}");

                File.WriteAllText(Path.Combine(planFolder, "plan.json"), sb.ToString());
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        #endregion

        #region IOASISBlockchainStorageProvider

        public async Task<OASISResult<string>> SendTransactionAsync(IWalletTransactionRequest transaction)
        {
            var result = new OASISResult<string>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate PLAN provider: {activateResult.Message}");
                        return result;
                    }
                }

                var transactionUrl = $"{_apiBaseUrl}/transactions/send";
                var transactionData = new
                {
                    fromAddress = transaction.FromWalletAddress,
                    toAddress = transaction.ToWalletAddress,
                    amount = transaction.Amount,
                    token = "PLAN", // Default token for PLAN
                    memo = transaction.MemoText ?? ""
                };

                var content = new StringContent(JsonSerializer.Serialize(transactionData), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(transactionUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var txData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
                    
                    result.Result = txData?.ContainsKey("transactionHash") == true ? txData["transactionHash"].ToString() : "";
                    result.IsError = false;
                    result.Message = "Transaction sent successfully to PLAN network";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"PLAN transaction failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending transaction to PLAN: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<string> SendTransaction(IWalletTransactionRequest transaction)
        {
            return SendTransactionAsync(transaction).Result;
        }

        public async Task<OASISResult<string>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            var result = new OASISResult<string>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate PLAN provider: {activateResult.Message}");
                        return result;
                    }
                }

                var fromAvatar = await LoadAvatarAsync(fromAvatarId);
                var toAvatar = await LoadAvatarAsync(toAvatarId);

                if (fromAvatar.IsError || fromAvatar.Result == null || toAvatar.IsError || toAvatar.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to load avatar(s) for transaction");
                    return result;
                }

                var walletResult = await WalletManager.Instance.LoadProviderWalletsForAvatarByIdAsync(fromAvatarId, providerTypeToLoadFrom: ProviderType.Value);
                if (walletResult.IsError || walletResult.Result == null || !walletResult.Result.ContainsKey(ProviderType.Value) || walletResult.Result[ProviderType.Value] == null || !walletResult.Result[ProviderType.Value].Any())
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to get wallet for sender avatar");
                    return result;
                }

                var fromWallet = walletResult.Result[ProviderType.Value].FirstOrDefault();
                var toWallet = toAvatar.Result.ProviderWallets?.ContainsKey(ProviderType.Value) == true && toAvatar.Result.ProviderWallets[ProviderType.Value]?.Any() == true 
                    ? toAvatar.Result.ProviderWallets[ProviderType.Value].FirstOrDefault() 
                    : null;

                var transactionRequest = new WalletTransactionRequest
                {
                    FromWalletAddress = fromWallet?.WalletAddress ?? "",
                    ToWalletAddress = toWallet?.WalletAddress ?? "",
                    Amount = amount
                };

                return await SendTransactionAsync(transactionRequest);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending transaction by ID to PLAN: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<string> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount).Result;
        }

        public async Task<OASISResult<string>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
        {
            var result = new OASISResult<string>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate PLAN provider: {activateResult.Message}");
                        return result;
                    }
                }

                var fromAvatar = await LoadAvatarAsync(fromAvatarId);
                var toAvatar = await LoadAvatarAsync(toAvatarId);

                if (fromAvatar.IsError || fromAvatar.Result == null || toAvatar.IsError || toAvatar.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to load avatar(s) for transaction");
                    return result;
                }

                var walletResult = await WalletManager.Instance.LoadProviderWalletsForAvatarByIdAsync(fromAvatarId, providerTypeToLoadFrom: ProviderType.Value);
                if (walletResult.IsError || walletResult.Result == null || !walletResult.Result.ContainsKey(ProviderType.Value) || walletResult.Result[ProviderType.Value] == null || !walletResult.Result[ProviderType.Value].Any())
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to get wallet for sender avatar");
                    return result;
                }

                var fromWallet = walletResult.Result[ProviderType.Value].FirstOrDefault();
                var toWallet = toAvatar.Result.ProviderWallets?.ContainsKey(ProviderType.Value) == true && toAvatar.Result.ProviderWallets[ProviderType.Value]?.Any() == true 
                    ? toAvatar.Result.ProviderWallets[ProviderType.Value].FirstOrDefault() 
                    : null;

                var transactionRequest = new WalletTransactionRequest
                {
                    FromWalletAddress = fromWallet?.WalletAddress ?? "",
                    ToWalletAddress = toWallet?.WalletAddress ?? "",
                    Amount = amount,
                    MemoText = token?.ToString() ?? ""
                };

                return await SendTransactionAsync(transactionRequest);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending transaction by ID with token to PLAN: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<string> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
        {
            return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount, token).Result;
        }

        public async Task<OASISResult<string>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount)
        {
            var result = new OASISResult<string>();
            try
            {
                var fromAvatar = await LoadAvatarByUsernameAsync(fromAvatarUsername);
                var toAvatar = await LoadAvatarByUsernameAsync(toAvatarUsername);

                if (fromAvatar.IsError || fromAvatar.Result == null || toAvatar.IsError || toAvatar.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to load avatar(s) by username for transaction");
                    return result;
                }

                return await SendTransactionByIdAsync(fromAvatar.Result.Id, toAvatar.Result.Id, amount);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending transaction by username to PLAN: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<string> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount)
        {
            return SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount).Result;
        }

        public async Task<OASISResult<string>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            var result = new OASISResult<string>();
            try
            {
                var fromAvatar = await LoadAvatarByUsernameAsync(fromAvatarUsername);
                var toAvatar = await LoadAvatarByUsernameAsync(toAvatarUsername);

                if (fromAvatar.IsError || fromAvatar.Result == null || toAvatar.IsError || toAvatar.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to load avatar(s) by username for transaction");
                    return result;
                }

                return await SendTransactionByIdAsync(fromAvatar.Result.Id, toAvatar.Result.Id, amount, token);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending transaction by username with token to PLAN: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<string> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            return SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount, token).Result;
        }

        public async Task<OASISResult<string>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount)
        {
            var result = new OASISResult<string>();
            try
            {
                var fromAvatar = await LoadAvatarByEmailAsync(fromAvatarEmail);
                var toAvatar = await LoadAvatarByEmailAsync(toAvatarEmail);

                if (fromAvatar.IsError || fromAvatar.Result == null || toAvatar.IsError || toAvatar.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to load avatar(s) by email for transaction");
                    return result;
                }

                return await SendTransactionByIdAsync(fromAvatar.Result.Id, toAvatar.Result.Id, amount);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending transaction by email to PLAN: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<string> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount)
        {
            return SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount).Result;
        }

        public async Task<OASISResult<string>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        {
            var result = new OASISResult<string>();
            try
            {
                var fromAvatar = await LoadAvatarByEmailAsync(fromAvatarEmail);
                var toAvatar = await LoadAvatarByEmailAsync(toAvatarEmail);

                if (fromAvatar.IsError || fromAvatar.Result == null || toAvatar.IsError || toAvatar.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to load avatar(s) by email for transaction");
                    return result;
                }

                return await SendTransactionByIdAsync(fromAvatar.Result.Id, toAvatar.Result.Id, amount, token);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending transaction by email with token to PLAN: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<string> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        {
            return SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount, token).Result;
        }

        public async Task<OASISResult<string>> SendTransactionByDefaultWalletAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            return await SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount);
        }

        public OASISResult<string> SendTransactionByDefaultWallet(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            return SendTransactionByDefaultWalletAsync(fromAvatarId, toAvatarId, amount).Result;
        }

        #endregion

        #region IOASISNFTProvider

        public async Task<OASISResult<bool>> SendNFTAsync(IWalletTransactionRequest transaction)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate PLAN provider: {activateResult.Message}");
                        return result;
                    }
                }

                var nftUrl = $"{_apiBaseUrl}/nft/transfer";
                var nftData = new
                {
                    fromAddress = transaction.FromWalletAddress ?? "",
                    toAddress = transaction.ToWalletAddress ?? "",
                    nftTokenId = "",
                    nftContractAddress = ""
                };

                var content = new StringContent(JsonSerializer.Serialize(nftData), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(nftUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    result.Result = true;
                    result.IsError = false;
                    result.Message = "NFT sent successfully to PLAN network";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"PLAN NFT transfer failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending NFT to PLAN: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<bool> SendNFT(IWalletTransactionRequest transaction)
        {
            return SendNFTAsync(transaction).Result;
        }

        #endregion

        #region IOASISLocalStorageProvider

        public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> LoadProviderWalletsForAvatarById(Guid id)
        {
            return LoadProviderWalletsForAvatarByIdAsync(id).Result;
        }

        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsForAvatarByIdAsync(Guid id)
        {
            var result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate PLAN provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load avatar to get provider wallets
                var avatarResult = await LoadAvatarAsync(id);
                if (avatarResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar: {avatarResult.Message}");
                    return result;
                }

                var providerWallets = new Dictionary<ProviderType, List<IProviderWallet>>();
                if (avatarResult.Result?.ProviderWallets != null)
                {
                    foreach (var group in avatarResult.Result.ProviderWallets.GroupBy(w => w.Key))
                    {
                        providerWallets[group.Key] = group.SelectMany(g => g.Value).ToList();
                    }
                }

                result.Result = providerWallets;
                result.IsError = false;
                result.Message = $"Successfully loaded {providerWallets.Count} provider wallet types for avatar {id} from PLAN";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading provider wallets for avatar from PLAN: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<bool> SaveProviderWalletsForAvatarById(Guid id, Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        {
            return SaveProviderWalletsForAvatarByIdAsync(id, providerWallets).Result;
        }

        public async Task<OASISResult<bool>> SaveProviderWalletsForAvatarByIdAsync(Guid id, Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate PLAN provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load avatar and update provider wallets
                var avatarResult = await LoadAvatarAsync(id);
                if (avatarResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar: {avatarResult.Message}");
                    return result;
                }

                var avatar = avatarResult.Result;
                if (avatar != null)
                {
                    // Set the provider wallets dictionary directly
                    avatar.ProviderWallets = providerWallets;

                    // Save updated avatar
                    var saveResult = await SaveAvatarAsync(avatar);
                    if (saveResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Error saving avatar: {saveResult.Message}");
                        return result;
                    }

                    // Count total wallets
                    var allWallets = new List<IProviderWallet>();
                    foreach (var kvp in providerWallets)
                    {
                        allWallets.AddRange(kvp.Value);
                    }

                    result.Result = true;
                    result.IsError = false;
                    result.Message = $"Successfully saved {allWallets.Count} provider wallets for avatar {id} to PLAN";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving provider wallets for avatar to PLAN: {ex.Message}", ex);
            }
            return result;
        }

        #endregion

        #region Serialization Methods

        /// <summary>
        /// Parse PLAN response to Avatar object
        /// </summary>
        private Avatar ParsePLANToAvatar(string planJson)
        {
            try
            {
                // Deserialize the complete Avatar object from PLAN JSON
                var avatar = System.Text.Json.JsonSerializer.Deserialize<Avatar>(planJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
                
                return avatar;
            }
            catch (Exception)
            {
                // If JSON deserialization fails, try to extract basic info
                return CreateAvatarFromPLAN(planJson);
            }
        }

        /// <summary>
        /// Create Avatar from PLAN response when JSON deserialization fails
        /// </summary>
        private Avatar CreateAvatarFromPLAN(string planJson)
        {
            try
            {
                // Extract basic information from PLAN JSON response
                var avatar = new Avatar
                {
                    Id = Guid.NewGuid(),
                    Username = ExtractPLANProperty(planJson, "id") ?? "plan_user",
                    Email = ExtractPLANProperty(planJson, "email") ?? "user@plan.example",
                    FirstName = ExtractPLANProperty(planJson, "first_name"),
                    LastName = ExtractPLANProperty(planJson, "last_name"),
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow
                };
                
                return avatar;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Extract property value from PLAN JSON response
        /// </summary>
        private string ExtractPLANProperty(string planJson, string propertyName)
        {
            try
            {
                // Simple regex-based extraction for PLAN properties
                var pattern = $"\"{propertyName}\"\\s*:\\s*\"([^\"]+)\"";
                var match = System.Text.RegularExpressions.Regex.Match(planJson, pattern);
                return match.Success ? match.Groups[1].Value : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Convert Avatar to PLAN format
        /// </summary>
        private string ConvertAvatarToPLAN(IAvatar avatar)
        {
            try
            {
                // Serialize Avatar to JSON with PLAN structure
                var planData = new
                {
                    id = avatar.Username,
                    email = avatar.Email,
                    first_name = avatar.FirstName,
                    last_name = avatar.LastName,
                    created = avatar.CreatedDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    modified = avatar.ModifiedDate.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                return System.Text.Json.JsonSerializer.Serialize(planData, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
            }
            catch (Exception)
            {
                // Fallback to basic JSON serialization
                return System.Text.Json.JsonSerializer.Serialize(avatar, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
            }
        }

        /// <summary>
        /// Convert Holon to PLAN format
        /// </summary>
        private string ConvertHolonToPLAN(IHolon holon)
        {
            try
            {
                // Serialize Holon to JSON with PLAN structure
                var planData = new
                {
                    id = holon.Id.ToString(),
                    type = holon.HolonType.ToString(),
                    name = holon.Name,
                    description = holon.Description,
                    created = holon.CreatedDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    modified = holon.ModifiedDate.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                return System.Text.Json.JsonSerializer.Serialize(planData, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
            }
            catch (Exception)
            {
                // Fallback to basic JSON serialization
                return System.Text.Json.JsonSerializer.Serialize(holon, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
            }
        }

        #endregion
    }
}
