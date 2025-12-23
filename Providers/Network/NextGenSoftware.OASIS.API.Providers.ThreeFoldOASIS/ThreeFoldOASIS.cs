//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using NextGenSoftware.OASIS.API.Core;
//using NextGenSoftware.OASIS.API.Core.Interfaces;
//using NextGenSoftware.OASIS.API.Core.Enums;
//using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
//using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
//using NextGenSoftware.OASIS.API.Core.Objects.Search;
//using NextGenSoftware.OASIS.Common;
//using NextGenSoftware.Utilities;
//using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
//using NextGenSoftware.OASIS.API.Core.Holons;
//using NextGenSoftware.OASIS.API.Core.Helpers;
//using System.Net.Http;
//using System.Text;
//using System.Text.Json;
//using System.Linq;
//using NextGenSoftware.OASIS.API.Core.Objects.NFT;
//using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
//using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
//using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
//using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
//using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
//using System.Threading;
//using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
//using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
//using NextGenSoftware.OASIS.API.Core.Managers;
//using NextGenSoftware.OASIS.API.Core.Helpers;

//namespace NextGenSoftware.OASIS.API.Providers.ThreeFoldOASIS
//{
//    public class ThreeFoldOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider, IOASISBlockchainStorageProvider, IOASISSmartContractProvider, IOASISNFTProvider, IOASISSuperStar
//    {
//        private readonly HttpClient _httpClient;
//        private readonly string _apiBaseUrl;
//        private readonly string _apiKey;
//        private WalletManager _walletManager;
//        private KeyManager _keyManager;

//        public string HostUri { get; set; }

//        private WalletManager WalletManager
//        {
//            get
//            {
//                if (_walletManager == null)
//                    _walletManager = WalletManager.Instance;
//                return _walletManager;
//            }
//            set => _walletManager = value;
//        }

//        private KeyManager KeyManager
//        {
//            get
//            {
//                if (_keyManager == null)
//                    _keyManager = new KeyManager(this, OASISDNA);
//                return _keyManager;
//            }
//            set => _keyManager = value;
//        }

//        public ThreeFoldOASIS(string hostURI, string apiKey = "")
//        {
//            this.ProviderName = "ThreeFoldOASIS";
//            this.ProviderDescription = "ThreeFold Provider";
//            this.ProviderType = new EnumValue<ProviderType>(API.Core.Enums.ProviderType.ThreeFoldOASIS);
//            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
//            this.HostUri = hostURI;
            
//            _apiBaseUrl = hostURI ?? "https://grid.tf/api/v1";
//            _apiKey = apiKey;
//            _httpClient = new HttpClient();
            
//            if (!string.IsNullOrEmpty(_apiKey))
//            {
//                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
//            }
//        }

//        #region IOASISStorageProvider Implementation

//        public override async Task<OASISResult<bool>> ActivateProviderAsync()
//        {
//            var response = new OASISResult<bool>();
//            try
//            {
//                // Initialize ThreeFold connection
//                response.Result = true;
//                response.Message = "ThreeFold provider activated successfully";
//            }
//            catch (Exception ex)
//            {
//                response.Exception = ex;
//                OASISErrorHandling.HandleError(ref response, $"Error activating ThreeFold provider: {ex.Message}");
//            }
//            return response;
//        }

//        public override OASISResult<bool> ActivateProvider()
//        {
//            return ActivateProviderAsync().Result;
//        }

//        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
//        {
//            var response = new OASISResult<bool>();
//            try
//            {
//                // Cleanup ThreeFold connection
//                response.Result = true;
//                response.Message = "ThreeFold provider deactivated successfully";
//            }
//            catch (Exception ex)
//            {
//                response.Exception = ex;
//                OASISErrorHandling.HandleError(ref response, $"Error deactivating ThreeFold provider: {ex.Message}");
//            }
//            return response;
//        }

//        public override OASISResult<bool> DeActivateProvider()
//        {
//            return DeActivateProviderAsync().Result;
//        }

//        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
//        {
//            var response = new OASISResult<IAvatar>();
//            try
//            {
//                // Load avatar from ThreeFold network
//                // This would query ThreeFold network for avatar data
//                var avatar = new Avatar
//                {
//                    Id = id,
//                    Username = $"threefold_user_{id}",
//                    Email = $"user_{id}@threefold.example",
//                    CreatedDate = DateTime.UtcNow,
//                    ModifiedDate = DateTime.UtcNow
//                };
                
//                response.Result = avatar;
//                response.Message = "Avatar loaded from ThreeFold successfully";
//            }
//            catch (Exception ex)
//            {
//                response.Exception = ex;
//                OASISErrorHandling.HandleError(ref response, $"Error loading avatar from ThreeFold: {ex.Message}");
//            }
//            return response;
//        }

//        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0)
//        {
//            return LoadAvatarAsync(id, version).Result;
//        }

//        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
//        {
//            var response = new OASISResult<IAvatar>();
//            try
//            {
//                // Load avatar by provider key from ThreeFold network
//                OASISErrorHandling.HandleError(ref response, "ThreeFold avatar loading by provider key not yet implemented");
//            }
//            catch (Exception ex)
//            {
//                response.Exception = ex;
//                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by provider key from ThreeFold: {ex.Message}");
//            }
//            return response;
//        }

//        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
//        {
//            return LoadAvatarByProviderKeyAsync(providerKey, version).Result;
//        }

//        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
//        {
//            var response = new OASISResult<IAvatar>();
//            try
//            {
//                // Load avatar by email from ThreeFold network
//                OASISErrorHandling.HandleError(ref response, "ThreeFold avatar loading by email not yet implemented");
//            }
//            catch (Exception ex)
//            {
//                response.Exception = ex;
//                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by email from ThreeFold: {ex.Message}");
//            }
//            return response;
//        }

//        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
//        {
//            return LoadAvatarByEmailAsync(avatarEmail, version).Result;
//        }

//        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
//        {
//            var response = new OASISResult<IAvatar>();
//            try
//            {
//                // Load avatar by username from ThreeFold network
//                OASISErrorHandling.HandleError(ref response, "ThreeFold avatar loading by username not yet implemented");
//            }
//            catch (Exception ex)
//            {
//                response.Exception = ex;
//                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by username from ThreeFold: {ex.Message}");
//            }
//            return response;
//        }

//        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
//        {
//            return LoadAvatarByUsernameAsync(avatarUsername, version).Result;
//        }

//        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
//        {
//            var response = new OASISResult<IAvatarDetail>();
//            try
//            {
//                // Load avatar detail from ThreeFold network
//                OASISErrorHandling.HandleError(ref response, "ThreeFold avatar detail loading not yet implemented");
//            }
//            catch (Exception ex)
//            {
//                response.Exception = ex;
//                OASISErrorHandling.HandleError(ref response, $"Error loading avatar detail from ThreeFold: {ex.Message}");
//            }
//            return response;
//        }

//        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
//        {
//            return null;
//        }

//        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
//        {
//            return null;
//        }

//        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
//        {
//            return null;
//        }

//        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
//        {
//            return null;
//        }

//        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
//        {
//            return null;
//        }

//        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
//        {
//            return null;
//        }

//        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
//        {
//            return null;
//        }

//        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
//        {
//            return null;
//        }

//        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
//        {
//            return null;
//        }

//        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
//        {
//            return null;
//        }

//        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
//        {
//            return null;
//        }

//        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
//        {
//            return null;
//        }

//        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail)
//        {
//            return null;
//        }

//        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
//        {
//            return null;
//        }

//        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
//        {
//            return null;
//        }

//        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
//        {
//            return null;
//        }

//        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
//        {
//            return null;
//        }

//        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
//        {
//            return null;
//        }

//        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
//        {
//            return null;
//        }

//        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
//        {
//            return null;
//        }

//        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
//        {
//            return null;
//        }

//        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
//        {
//            return null;
//        }

//        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
//        {
//            return null;
//        }

//        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
//        {
//            return null;
//        }

//        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
//        {
//            return null;
//        }

//        //public override Task<OASISResult<IHolon>> LoadHolonByCustomKeyAsync(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
//        //{
//        //    throw new NotImplementedException();
//        //}

//        //public override OASISResult<IHolon> LoadHolonByCustomKey(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
//        //{
//        //    throw new NotImplementedException();
//        //}

//        //public override Task<OASISResult<IHolon>> LoadHolonByMetaDataAsync(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
//        //{
//        //    throw new NotImplementedException();
//        //}

//        //public override OASISResult<IHolon> LoadHolonByMetaData(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
//        //{
//        //    throw new NotImplementedException();
//        //}

//        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
//        {
//            return null;
//        }

//        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
//        {
//            return null;
//        }

//        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
//        {
//            return null;
//        }

//        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool loadChildrenFromProvider = false, bool continueOnError = true, int version = 0)
//        {
//            return null;
//        }

//        //public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentByCustomKeyAsync(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
//        //{
//        //    throw new NotImplementedException();
//        //}

//        //public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParentByCustomKey(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
//        //{
//        //    throw new NotImplementedException();
//        //}

//        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
//        {
//            var result = new OASISResult<IEnumerable<IHolon>>();
//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "ThreeFold provider is not activated");
//                    return result;
//                }

//                var searchParams = new Dictionary<string, string>
//                {
//                    { "metaKey", metaKey },
//                    { "metaValue", metaValue },
//                    { "type", type.ToString() },
//                    { "version", version.ToString() }
//                };

//                var queryString = string.Join("&", searchParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
//                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/holons/search?{queryString}");

//                if (response.IsSuccessStatusCode)
//                {
//                    var content = await response.Content.ReadAsStringAsync();
//                    var holons = JsonSerializer.Deserialize<List<Holon>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
//                    if (holons != null)
//                    {
//                        result.Result = holons.Cast<IHolon>();
//                        result.IsError = false;
//                        result.Message = $"Successfully loaded {holons.Count} holons by metadata from ThreeFold Grid";
//                    }
//                    else
//                    {
//                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize holons from ThreeFold Grid API");
//                    }
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata from ThreeFold Grid: {ex.Message}", ex);
//            }
//            return result;
//        }

//        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
//        {
//            return LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
//        }

//        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
//        {
//            var result = new OASISResult<IEnumerable<IHolon>>();
//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "ThreeFold provider is not activated");
//                    return result;
//                }

//                var searchRequest = new
//                {
//                    metaKeyValuePairs = metaKeyValuePairs,
//                    matchMode = metaKeyValuePairMatchMode.ToString(),
//                    type = type.ToString(),
//                    version = version
//                };

//                var jsonContent = JsonSerializer.Serialize(searchRequest);
//                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
//                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/holons/search-multiple", content);

//                if (response.IsSuccessStatusCode)
//                {
//                    var responseContent = await response.Content.ReadAsStringAsync();
//                    var holons = JsonSerializer.Deserialize<List<Holon>>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
//                    if (holons != null)
//                    {
//                        result.Result = holons.Cast<IHolon>();
//                        result.IsError = false;
//                        result.Message = $"Successfully loaded {holons.Count} holons by multiple metadata from ThreeFold Grid";
//                    }
//                    else
//                    {
//                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize holons from ThreeFold Grid API");
//                    }
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error loading holons by multiple metadata from ThreeFold Grid: {ex.Message}", ex);
//            }
//            return result;
//        }

//        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
//        {
//            return LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
//        }

//        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
//        {
//            return null;
//        }

//        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
//        {
//            return null;
//        }

//        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
//        {
//            var result = new OASISResult<IHolon>();
//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "ThreeFold provider is not activated");
//                    return result;
//                }

//                if (holon == null)
//                {
//                    OASISErrorHandling.HandleError(ref result, "Holon cannot be null");
//                    return result;
//                }

//                var jsonContent = JsonSerializer.Serialize(holon);
//                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
//                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/holons", content);

//                if (response.IsSuccessStatusCode)
//                {
//                    var responseContent = await response.Content.ReadAsStringAsync();
//                    var savedHolon = JsonSerializer.Deserialize<Holon>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
//                    if (savedHolon != null)
//                    {
//                        result.Result = savedHolon;
//                        result.IsError = false;
//                        result.Message = "Holon saved successfully to ThreeFold Grid";
//                    }
//                    else
//                    {
//                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize saved holon from ThreeFold Grid API");
//                    }
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error saving holon to ThreeFold Grid: {ex.Message}", ex);
//            }
//            return result;
//        }

//        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
//        {
//            return SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;
//        }

//        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
//        {
//            var result = new OASISResult<IEnumerable<IHolon>>();
//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "ThreeFold provider is not activated");
//                    return result;
//                }

//                if (holons == null || !holons.Any())
//                {
//                    OASISErrorHandling.HandleError(ref result, "Holons collection cannot be null or empty");
//                    return result;
//                }

//                var jsonContent = JsonSerializer.Serialize(holons);
//                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
//                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/holons/batch", content);

//                if (response.IsSuccessStatusCode)
//                {
//                    var responseContent = await response.Content.ReadAsStringAsync();
//                    var savedHolons = JsonSerializer.Deserialize<List<Holon>>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
//                    if (savedHolons != null)
//                    {
//                        result.Result = savedHolons.Cast<IHolon>();
//                        result.IsError = false;
//                        result.Message = $"Successfully saved {savedHolons.Count} holons to ThreeFold Grid";
//                    }
//                    else
//                    {
//                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize saved holons from ThreeFold Grid API");
//                    }
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error saving holons to ThreeFold Grid: {ex.Message}", ex);
//            }
//            return result;
//        }

//        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
//        {
//            return SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;
//        }

//        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
//        {
//            var result = new OASISResult<IHolon>();
//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "ThreeFold provider is not activated");
//                    return result;
//                }

//                var response = await _httpClient.DeleteAsync($"{_apiBaseUrl}/holons/{id}");

//                if (response.IsSuccessStatusCode)
//                {
//                    result.Result = null; // Holon deleted
//                    result.IsError = false;
//                    result.Message = "Holon deleted successfully from ThreeFold Grid";
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error deleting holon from ThreeFold Grid: {ex.Message}", ex);
//            }
//            return result;
//        }

//        public override OASISResult<IHolon> DeleteHolon(Guid id)
//        {
//            return DeleteHolonAsync(id).Result;
//        }

//        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
//        {
//            var result = new OASISResult<IHolon>();
//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "ThreeFold provider is not activated");
//                    return result;
//                }

//                var response = await _httpClient.DeleteAsync($"{_apiBaseUrl}/holons/by-provider-key/{Uri.EscapeDataString(providerKey)}");

//                if (response.IsSuccessStatusCode)
//                {
//                    result.Result = null; // Holon deleted
//                    result.IsError = false;
//                    result.Message = "Holon deleted successfully from ThreeFold Grid by provider key";
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error deleting holon from ThreeFold Grid by provider key: {ex.Message}", ex);
//            }
//            return result;
//        }

//        public override OASISResult<IHolon> DeleteHolon(string providerKey)
//        {
//            return DeleteHolonAsync(providerKey).Result;
//        }

//        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
//        {
//            var result = new OASISResult<ISearchResults>();
//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "ThreeFold provider is not activated");
//                    return result;
//                }

//                if (searchParams == null)
//                {
//                    OASISErrorHandling.HandleError(ref result, "SearchParams cannot be null");
//                    return result;
//                }

//                string searchText = null;
//                HolonType holonType = HolonType.All;
//                Dictionary<string, string> metaData = null;

//                if (searchParams.SearchGroups != null && searchParams.SearchGroups.Any())
//                {
//                    var firstGroup = searchParams.SearchGroups.First();
//                    holonType = firstGroup.HolonType;

//                    if (firstGroup is ISearchTextGroup textGroup)
//                        searchText = textGroup.SearchQuery;

//                    if (firstGroup.HolonSearchParams != null && firstGroup.HolonSearchParams.MetaData)
//                    {
//                        metaData = new Dictionary<string, string>();
//                    }
//                }

//                var searchRequest = new
//                {
//                    searchText = searchText,
//                    holonType = holonType.ToString(),
//                    metaData = metaData,
//                    version = version
//                };

//                var jsonContent = JsonSerializer.Serialize(searchRequest);
//                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
//                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/search", content);

//                if (response.IsSuccessStatusCode)
//                {
//                    var responseContent = await response.Content.ReadAsStringAsync();
//                    var searchResults = JsonSerializer.Deserialize<SearchResults>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
//                    if (searchResults != null)
//                    {
//                        result.Result = searchResults;
//                        result.IsError = false;
//                        result.Message = $"Search completed successfully on ThreeFold Grid";
//                    }
//                    else
//                    {
//                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize search results from ThreeFold Grid API");
//                    }
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error searching on ThreeFold Grid: {ex.Message}", ex);
//            }
//            return result;
//        }

//        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
//        {
//            return SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
//        }

//        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
//        {
//            var result = new OASISResult<bool>();
//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "ThreeFold provider is not activated");
//                    return result;
//                }

//                if (holons == null || !holons.Any())
//                {
//                    OASISErrorHandling.HandleError(ref result, "Holons collection cannot be null or empty");
//                    return result;
//                }

//                var jsonContent = JsonSerializer.Serialize(holons);
//                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
//                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/import", content);

//                if (response.IsSuccessStatusCode)
//                {
//                    result.Result = true;
//                    result.IsError = false;
//                    result.Message = $"Successfully imported {holons.Count()} holons to ThreeFold Grid";
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error importing holons to ThreeFold Grid: {ex.Message}", ex);
//            }
//            return result;
//        }

//        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
//        {
//            return ImportAsync(holons).Result;
//        }

//        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
//        {
//            var result = new OASISResult<IEnumerable<IHolon>>();
//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "ThreeFold provider is not activated");
//                    return result;
//                }

//                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/export/avatar/{avatarId}?version={version}");

//                if (response.IsSuccessStatusCode)
//                {
//                    var content = await response.Content.ReadAsStringAsync();
//                    var holons = JsonSerializer.Deserialize<List<Holon>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
//                    if (holons != null)
//                    {
//                        result.Result = holons.Cast<IHolon>();
//                        result.IsError = false;
//                        result.Message = $"Successfully exported {holons.Count} holons for avatar {avatarId} from ThreeFold Grid";
//                    }
//                    else
//                    {
//                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize exported holons from ThreeFold Grid API");
//                    }
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data from ThreeFold Grid: {ex.Message}", ex);
//            }
//            return result;
//        }

//        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
//        {
//            return ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
//        }

//        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
//        {
//            var result = new OASISResult<IEnumerable<IHolon>>();
//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "ThreeFold provider is not activated");
//                    return result;
//                }

//                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/export/avatar/username/{Uri.EscapeDataString(avatarUsername)}?version={version}");

//                if (response.IsSuccessStatusCode)
//                {
//                    var content = await response.Content.ReadAsStringAsync();
//                    var holons = JsonSerializer.Deserialize<List<Holon>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
//                    if (holons != null)
//                    {
//                        result.Result = holons.Cast<IHolon>();
//                        result.IsError = false;
//                        result.Message = $"Successfully exported {holons.Count} holons for avatar {avatarUsername} from ThreeFold Grid";
//                    }
//                    else
//                    {
//                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize exported holons from ThreeFold Grid API");
//                    }
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data by username from ThreeFold Grid: {ex.Message}", ex);
//            }
//            return result;
//        }

//        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
//        {
//            return ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
//        }

//        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
//        {
//            var result = new OASISResult<IEnumerable<IHolon>>();
//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "ThreeFold provider is not activated");
//                    return result;
//                }

//                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/export/avatar/email/{Uri.EscapeDataString(avatarEmailAddress)}?version={version}");

//                if (response.IsSuccessStatusCode)
//                {
//                    var content = await response.Content.ReadAsStringAsync();
//                    var holons = JsonSerializer.Deserialize<List<Holon>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
//                    if (holons != null)
//                    {
//                        result.Result = holons.Cast<IHolon>();
//                        result.IsError = false;
//                        result.Message = $"Successfully exported {holons.Count} holons for avatar {avatarEmailAddress} from ThreeFold Grid";
//                    }
//                    else
//                    {
//                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize exported holons from ThreeFold Grid API");
//                    }
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data by email from ThreeFold Grid: {ex.Message}", ex);
//            }
//            return result;
//        }

//        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
//        {
//            return ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;
//        }

//        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
//        {
//            var result = new OASISResult<IEnumerable<IHolon>>();
//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "ThreeFold provider is not activated");
//                    return result;
//                }

//                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/export/all?version={version}");

//                if (response.IsSuccessStatusCode)
//                {
//                    var content = await response.Content.ReadAsStringAsync();
//                    var holons = JsonSerializer.Deserialize<List<Holon>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
//                    if (holons != null)
//                    {
//                        result.Result = holons.Cast<IHolon>();
//                        result.IsError = false;
//                        result.Message = $"Successfully exported {holons.Count} holons from ThreeFold Grid";
//                    }
//                    else
//                    {
//                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize exported holons from ThreeFold Grid API");
//                    }
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error exporting all data from ThreeFold Grid: {ex.Message}", ex);
//            }
//            return result;
//        }

//        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
//        {
//            return ExportAllAsync(version).Result;
//        }

//        #endregion

//        #region IOASISNET Implementation

//        OASISResult<IEnumerable<IAvatar>> IOASISNETProvider.GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
//        {
//            var result = new OASISResult<IEnumerable<IAvatar>>();
//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "ThreeFold provider is not activated");
//                    return result;
//                }

//                var avatarsResult = LoadAllAvatars();
//                if (avatarsResult.IsError || avatarsResult.Result == null)
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Error loading avatars: {avatarsResult.Message}");
//                    return result;
//                }

//                var centerLat = geoLat / 1e6d;
//                var centerLng = geoLong / 1e6d;
//                var nearby = new List<IAvatar>();

//                foreach (var avatar in avatarsResult.Result)
//                {
//                    if (avatar.MetaData != null &&
//                        avatar.MetaData.TryGetValue("Latitude", out var latObj) &&
//                        avatar.MetaData.TryGetValue("Longitude", out var lngObj) &&
//                        double.TryParse(latObj?.ToString(), out var lat) &&
//                        double.TryParse(lngObj?.ToString(), out var lng))
//                    {
//                        var distance = GeoHelper.CalculateDistance(centerLat, centerLng, lat, lng);
//                        if (distance <= radiusInMeters)
//                            nearby.Add(avatar);
//                    }
//                }

//                result.Result = nearby;
//                result.IsError = false;
//                result.Message = $"Found {nearby.Count} avatars within {radiusInMeters}m";
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error getting avatars near me from ThreeFold: {ex.Message}", ex);
//            }
//            return result;
//        }

//        OASISResult<IEnumerable<IHolon>> IOASISNETProvider.GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
//        {
//            var result = new OASISResult<IEnumerable<IHolon>>();
//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "ThreeFold provider is not activated");
//                    return result;
//                }

//                var holonsResult = LoadAllHolons(Type);
//                if (holonsResult.IsError || holonsResult.Result == null)
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Error loading holons: {holonsResult.Message}");
//                    return result;
//                }

//                var centerLat = geoLat / 1e6d;
//                var centerLng = geoLong / 1e6d;
//                var nearby = new List<IHolon>();

//                foreach (var holon in holonsResult.Result)
//                {
//                    if (holon.MetaData != null &&
//                        holon.MetaData.TryGetValue("Latitude", out var latObj) &&
//                        holon.MetaData.TryGetValue("Longitude", out var lngObj) &&
//                        double.TryParse(latObj?.ToString(), out var lat) &&
//                        double.TryParse(lngObj?.ToString(), out var lng))
//                    {
//                        var distance = GeoHelper.CalculateDistance(centerLat, centerLng, lat, lng);
//                        if (distance <= radiusInMeters)
//                            nearby.Add(holon);
//                    }
//                }

//                result.Result = nearby;
//                result.IsError = false;
//                result.Message = $"Found {nearby.Count} holons within {radiusInMeters}m";
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error getting holons near me from ThreeFold: {ex.Message}", ex);
//            }
//            return result;
//        }

//        #endregion

//        #region IOASISSuperStar
//        public bool NativeCodeGenesis(ICelestialBody celestialBody, string outputFolder, string nativeSource)
//        {
//            // ThreeFold currently does not generate native code from STAR metadata.
//            return true;
//        }

//        #endregion

//        #region IOASISBlockchainStorageProvider

//        public async Task<OASISResult<ITransactionResponse>> SendTokenAsync(ISendWeb3TokenRequest transation)
//        {
//            var result = new OASISResult<ITransactionResponse>();
//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "ThreeFold provider is not activated");
//                    return result;
//                }

//                if (transation == null)
//                {
//                    OASISErrorHandling.HandleError(ref result, "Transaction request cannot be null");
//                    return result;
//                }

//                var transactionRequest = new
//                {
//                    fromAddress = transation.FromWalletAddress,
//                    toAddress = transation.ToWalletAddress,
//                    amount = transation.Amount,
//                    memo = transation.MemoText
//                };

//                var jsonContent = JsonSerializer.Serialize(transactionRequest);
//                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
//                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/transactions/send", content);

//                if (response.IsSuccessStatusCode)
//                {
//                    var responseContent = await response.Content.ReadAsStringAsync();
//                    var transactionResponse = JsonSerializer.Deserialize<TransactionResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
//                    if (transactionResponse != null)
//                    {
//                        result.Result = transactionResponse;
//                        result.IsError = false;
//                        result.Message = "Transaction sent successfully to ThreeFold Grid";
//                    }
//                    else
//                    {
//                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize transaction response from ThreeFold Grid API");
//                    }
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error sending transaction to ThreeFold Grid: {ex.Message}", ex);
//            }
//            return result;
//        }

//        public OASISResult<ITransactionResponse> SendToken(ISendWeb3TokenRequest transation)
//        {
//            return SendTokenAsync(transation).Result;
//        }

//        public async Task<OASISResult<ITransactionResponse>> SendTokenByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
//        {
//            var result = new OASISResult<ITransactionResponse>();
//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "ThreeFold provider is not activated");
//                    return result;
//                }

//                var transactionRequest = new
//                {
//                    fromAvatarId = fromAvatarId,
//                    toAvatarId = toAvatarId,
//                    amount = amount
//                };

//                var jsonContent = JsonSerializer.Serialize(transactionRequest);
//                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
//                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/transactions/send-by-id", content);

//                if (response.IsSuccessStatusCode)
//                {
//                    var responseContent = await response.Content.ReadAsStringAsync();
//                    var transactionResponse = JsonSerializer.Deserialize<TransactionResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
//                    if (transactionResponse != null)
//                    {
//                        result.Result = transactionResponse;
//                        result.IsError = false;
//                        result.Message = "Transaction sent successfully by avatar ID to ThreeFold Grid";
//                    }
//                    else
//                    {
//                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize transaction response from ThreeFold Grid API");
//                    }
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error sending transaction by ID to ThreeFold Grid: {ex.Message}", ex);
//            }
//            return result;
//        }

//        public OASISResult<ITransactionResponse> SendTokenById(Guid fromAvatarId, Guid toAvatarId, decimal amount)
//        {
//            return SendTokenByIdAsync(fromAvatarId, toAvatarId, amount).Result;
//        }

//        public async Task<OASISResult<ITransactionResponse>> SendTokenByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
//        {
//            var result = new OASISResult<ITransactionResponse>();
//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "ThreeFold provider is not activated");
//                    return result;
//                }

//                var transactionRequest = new
//                {
//                    fromAvatarId = fromAvatarId,
//                    toAvatarId = toAvatarId,
//                    amount = amount,
//                    token = token
//                };

//                var jsonContent = JsonSerializer.Serialize(transactionRequest);
//                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
//                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/transactions/send-by-id-token", content);

//                if (response.IsSuccessStatusCode)
//                {
//                    var responseContent = await response.Content.ReadAsStringAsync();
//                    var transactionResponse = JsonSerializer.Deserialize<TransactionResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
//                    if (transactionResponse != null)
//                    {
//                        result.Result = transactionResponse;
//                        result.IsError = false;
//                        result.Message = "Transaction sent successfully by avatar ID with token to ThreeFold Grid";
//                    }
//                    else
//                    {
//                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize transaction response from ThreeFold Grid API");
//                    }
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error sending transaction by ID with token to ThreeFold Grid: {ex.Message}", ex);
//            }
//            return result;
//        }

//        public OASISResult<ITransactionResponse> SendTokenById(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
//        {
//            return SendTokenByIdAsync(fromAvatarId, toAvatarId, amount, token).Result;
//        }

//        public async Task<OASISResult<ITransactionResponse>> SendTokenByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount)
//        {
//            var result = new OASISResult<ITransactionResponse>();
//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "ThreeFold provider is not activated");
//                    return result;
//                }

//                var transactionRequest = new
//                {
//                    fromAvatarUsername = fromAvatarUsername,
//                    toAvatarUsername = toAvatarUsername,
//                    amount = amount
//                };

//                var jsonContent = JsonSerializer.Serialize(transactionRequest);
//                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
//                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/transactions/send-by-username", content);

//                if (response.IsSuccessStatusCode)
//                {
//                    var responseContent = await response.Content.ReadAsStringAsync();
//                    var transactionResponse = JsonSerializer.Deserialize<TransactionResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
//                    if (transactionResponse != null)
//                    {
//                        result.Result = transactionResponse;
//                        result.IsError = false;
//                        result.Message = "Transaction sent successfully by username to ThreeFold Grid";
//                    }
//                    else
//                    {
//                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize transaction response from ThreeFold Grid API");
//                    }
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error sending transaction by username to ThreeFold Grid: {ex.Message}", ex);
//            }
//            return result;
//        }

//        public OASISResult<ITransactionResponse> SendTokenByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount)
//        {
//            return SendTokenByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount).Result;
//        }

//        public async Task<OASISResult<ITransactionResponse>> SendTokenByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
//        {
//            var result = new OASISResult<ITransactionResponse>();
//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "ThreeFold provider is not activated");
//                    return result;
//                }

//                var transactionRequest = new
//                {
//                    fromAvatarUsername = fromAvatarUsername,
//                    toAvatarUsername = toAvatarUsername,
//                    amount = amount,
//                    token = token
//                };

//                var jsonContent = JsonSerializer.Serialize(transactionRequest);
//                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
//                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/transactions/send-by-username-token", content);

//                if (response.IsSuccessStatusCode)
//                {
//                    var responseContent = await response.Content.ReadAsStringAsync();
//                    var transactionResponse = JsonSerializer.Deserialize<TransactionResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
//                    if (transactionResponse != null)
//                    {
//                        result.Result = transactionResponse;
//                        result.IsError = false;
//                        result.Message = "Transaction sent successfully by username with token to ThreeFold Grid";
//                    }
//                    else
//                    {
//                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize transaction response from ThreeFold Grid API");
//                    }
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error sending transaction by username with token to ThreeFold Grid: {ex.Message}", ex);
//            }
//            return result;
//        }

//        public OASISResult<ITransactionResponse> SendTokenByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
//        {
//            return SendTokenByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount, token).Result;
//        }

//        public async Task<OASISResult<ITransactionResponse>> SendTokenByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount)
//        {
//            var result = new OASISResult<ITransactionResponse>();
//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "ThreeFold provider is not activated");
//                    return result;
//                }

//                var transactionRequest = new
//                {
//                    fromAvatarEmail = fromAvatarEmail,
//                    toAvatarEmail = toAvatarEmail,
//                    amount = amount
//                };

//                var jsonContent = JsonSerializer.Serialize(transactionRequest);
//                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
//                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/transactions/send-by-email", content);

//                if (response.IsSuccessStatusCode)
//                {
//                    var responseContent = await response.Content.ReadAsStringAsync();
//                    var transactionResponse = JsonSerializer.Deserialize<TransactionResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
//                    if (transactionResponse != null)
//                    {
//                        result.Result = transactionResponse;
//                        result.IsError = false;
//                        result.Message = "Transaction sent successfully by email to ThreeFold Grid";
//                    }
//                    else
//                    {
//                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize transaction response from ThreeFold Grid API");
//                    }
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error sending transaction by email to ThreeFold Grid: {ex.Message}", ex);
//            }
//            return result;
//        }

//        public OASISResult<ITransactionResponse> SendTokenByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount)
//        {
//            return SendTokenByEmailAsync(fromAvatarEmail, toAvatarEmail, amount).Result;
//        }

//        public async Task<OASISResult<ITransactionResponse>> SendTokenByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
//        {
//            var result = new OASISResult<ITransactionResponse>();
//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "ThreeFold provider is not activated");
//                    return result;
//                }

//                var transactionRequest = new
//                {
//                    fromAvatarEmail = fromAvatarEmail,
//                    toAvatarEmail = toAvatarEmail,
//                    amount = amount,
//                    token = token
//                };

//                var jsonContent = JsonSerializer.Serialize(transactionRequest);
//                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
//                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/transactions/send-by-email-token", content);

//                if (response.IsSuccessStatusCode)
//                {
//                    var responseContent = await response.Content.ReadAsStringAsync();
//                    var transactionResponse = JsonSerializer.Deserialize<TransactionResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
//                    if (transactionResponse != null)
//                    {
//                        result.Result = transactionResponse;
//                        result.IsError = false;
//                        result.Message = "Transaction sent successfully by email with token to ThreeFold Grid";
//                    }
//                    else
//                    {
//                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize transaction response from ThreeFold Grid API");
//                    }
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error sending transaction by email with token to ThreeFold Grid: {ex.Message}", ex);
//            }
//            return result;
//        }

//        public OASISResult<ITransactionResponse> SendTokenByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
//        {
//            return SendTokenByEmailAsync(fromAvatarEmail, toAvatarEmail, amount, token).Result;
//        }

//        // IOASISBlockchainStorageProvider interface methods
//        public OASISResult<ITransactionResponse> SendToken(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
//        {
//            return SendTokenAsync(fromWalletAddress, toWalletAddress, amount, memoText).Result;
//        }

//        public async Task<OASISResult<ITransactionResponse>> SendTokenAsync(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
//        {
//            var result = new OASISResult<ITransactionResponse>();
//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "ThreeFold provider is not activated");
//                    return result;
//                }

//                // ThreeFold transaction implementation
//                var transactionRequest = new
//                {
//                    from = fromWalletAddress,
//                    to = toWalletAddress,
//                    amount = amount,
//                    memo = memoText
//                };

//                var jsonContent = JsonSerializer.Serialize(transactionRequest);
//                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
//                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/transactions", content);

//                if (response.IsSuccessStatusCode)
//                {
//                    var responseContent = await response.Content.ReadAsStringAsync();
//                    var transactionResponse = JsonSerializer.Deserialize<TransactionResponse>(responseContent);
//                    result.Result = transactionResponse;
//                    result.IsError = false;
//                    result.Message = "Transaction sent successfully";
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Transaction failed: {response.StatusCode}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error sending transaction: {ex.Message}", ex);
//            }
//            return result;
//        }

//        public async Task<OASISResult<ITransactionResponse>> SendTokenByDefaultWalletAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
//        {
//            var result = new OASISResult<ITransactionResponse>();
//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "ThreeFold provider is not activated");
//                    return result;
//                }

//                var transactionRequest = new
//                {
//                    fromAvatarId = fromAvatarId,
//                    toAvatarId = toAvatarId,
//                    amount = amount
//                };

//                var jsonContent = JsonSerializer.Serialize(transactionRequest);
//                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
//                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/transactions/send-by-default-wallet", content);

//                if (response.IsSuccessStatusCode)
//                {
//                    var responseContent = await response.Content.ReadAsStringAsync();
//                    var transactionResponse = JsonSerializer.Deserialize<TransactionResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
//                    if (transactionResponse != null)
//                    {
//                        result.Result = transactionResponse;
//                        result.IsError = false;
//                        result.Message = "Transaction sent successfully by default wallet to ThreeFold Grid";
//                    }
//                    else
//                    {
//                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize transaction response from ThreeFold Grid API");
//                    }
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error sending transaction by default wallet to ThreeFold Grid: {ex.Message}", ex);
//            }
//            return result;
//        }

//        public OASISResult<ITransactionResponse> SendTokenByDefaultWallet(Guid fromAvatarId, Guid toAvatarId, decimal amount)
//        {
//            return SendTokenByDefaultWalletAsync(fromAvatarId, toAvatarId, amount).Result;
//        }

//        #endregion

//        #region IOASISNFTProvider

//        public async Task<OASISResult<IWeb3NFTTransactionResponse>> SendNFTAsync(ISendWeb3NFTRequest transation)
//        {
//            var result = new OASISResult<IWeb3NFTTransactionResponse>();
//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "ThreeFold provider is not activated");
//                    return result;
//                }

//                if (transation == null)
//                {
//                    OASISErrorHandling.HandleError(ref result, "NFT transaction request cannot be null");
//                    return result;
//                }

//                var nftRequest = new
//                {
//                    fromAddress = transation.FromWalletAddress,
//                    toAddress = transation.ToWalletAddress,
//                    nftId = transation.TokenId,
//                    tokenId = transation.TokenId,
//                    contractAddress = transation.TokenAddress
//                };

//                var jsonContent = JsonSerializer.Serialize(nftRequest);
//                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
//                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/nft/send", content);

//                if (response.IsSuccessStatusCode)
//                {
//                    var responseContent = await response.Content.ReadAsStringAsync();
//                    var nftResponse = JsonSerializer.Deserialize<IWeb3NFTTransactionResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
//                    if (nftResponse != null)
//                    {
//                        result.Result = nftResponse;
//                        result.IsError = false;
//                        result.Message = "NFT sent successfully to ThreeFold Grid";
//                    }
//                    else
//                    {
//                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize NFT transaction response from ThreeFold Grid API");
//                    }
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error sending NFT to ThreeFold Grid: {ex.Message}", ex);
//            }
//            return result;
//        }

//        public OASISResult<IWeb3NFTTransactionResponse> SendNFT(ISendWeb3NFTRequest transation)
//        {
//            return SendNFTAsync(transation).Result;
//        }

//        public async Task<OASISResult<IWeb3NFTTransactionResponse>> MintNFTAsync(IMintWeb3NFTRequest transation)
//        {
//            var result = new OASISResult<IWeb3NFTTransactionResponse>();
//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "ThreeFold provider is not activated");
//                    return result;
//                }

//                if (transation == null)
//                {
//                    OASISErrorHandling.HandleError(ref result, "Mint NFT transaction request cannot be null");
//                    return result;
//                }

//                var mintRequest = new
//                {
//                    toAddress = transation.SendToAddressAfterMinting,
//                    tokenId = transation.Title,
//                    contractAddress = transation.OnChainProvider.ToString(),
//                    metadata = transation.MetaData,
//                    name = transation.Title,
//                    description = transation.Description,
//                    imageUrl = transation.ImageUrl
//                };

//                var jsonContent = JsonSerializer.Serialize(mintRequest);
//                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
//                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/nft/mint", content);

//                if (response.IsSuccessStatusCode)
//                {
//                    var responseContent = await response.Content.ReadAsStringAsync();
//                    var nftResponse = JsonSerializer.Deserialize<IWeb3NFTTransactionResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
//                    if (nftResponse != null)
//                    {
//                        result.Result = nftResponse;
//                        result.IsError = false;
//                        result.Message = "NFT minted successfully on ThreeFold Grid";
//                    }
//                    else
//                    {
//                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize NFT mint response from ThreeFold Grid API");
//                    }
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error minting NFT on ThreeFold Grid: {ex.Message}", ex);
//            }
//            return result;
//        }

//        public OASISResult<IWeb3NFTTransactionResponse> MintNFT(IMintWeb3NFTRequest transation)
//        {
//            return MintNFTAsync(transation).Result;
//        }


//        public async Task<OASISResult<IWeb3NFT>> LoadOnChainNFTDataAsync(string nftTokenAddress)
//        {
//            var result = new OASISResult<IWeb3NFT>();
//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "ThreeFold provider is not activated");
//                    return result;
//                }

//                if (string.IsNullOrEmpty(nftTokenAddress))
//                {
//                    OASISErrorHandling.HandleError(ref result, "NFT token address cannot be null or empty");
//                    return result;
//                }

//                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/nft/on-chain/{Uri.EscapeDataString(nftTokenAddress)}");

//                if (response.IsSuccessStatusCode)
//                {
//                    var content = await response.Content.ReadAsStringAsync();
//                    var nft = JsonSerializer.Deserialize<Web3NFT>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
//                    if (nft != null)
//                    {
//                        result.Result = nft;
//                        result.IsError = false;
//                        result.Message = "NFT data loaded successfully from ThreeFold Grid";
//                    }
//                    else
//                    {
//                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize NFT data from ThreeFold Grid API");
//                    }
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error loading NFT data from ThreeFold Grid: {ex.Message}", ex);
//            }
//            return result;
//        }

//        public OASISResult<IWeb3NFT> LoadOnChainNFTData(string nftTokenAddress)
//        {
//            return LoadOnChainNFTDataAsync(nftTokenAddress).Result;
//        }

//        public async Task<OASISResult<IWeb3NFTTransactionResponse>> BurnNFTAsync(IBurnWeb3NFTRequest request)
//        {
//            var result = new OASISResult<IWeb3NFTTransactionResponse>();
//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "ThreeFold provider is not activated");
//                    return result;
//                }

//                var burnRequest = new
//                {
//                    nftTokenAddress = request.NFTTokenAddress,
//                    burntByAvatarId = request.BurntByAvatarId,
//                    web3NFTId = request.Web3NFTId
//                };

//                var jsonContent = JsonSerializer.Serialize(burnRequest);
//                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
//                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/nft/burn", content);

//                if (response.IsSuccessStatusCode)
//                {
//                    var responseContent = await response.Content.ReadAsStringAsync();
//                    var nftResponse = JsonSerializer.Deserialize<Web3NFTTransactionResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
//                    if (nftResponse != null)
//                    {
//                        result.Result = nftResponse;
//                        result.IsError = false;
//                        result.Message = "NFT burned successfully on ThreeFold Grid";
//                    }
//                    else
//                    {
//                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize NFT burn response from ThreeFold Grid API");
//                    }
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error burning NFT on ThreeFold Grid: {ex.Message}", ex);
//            }
//            return result;
//        }

//        public OASISResult<IWeb3NFTTransactionResponse> BurnNFT(IBurnWeb3NFTRequest request)
//        {
//            return BurnNFTAsync(request).Result;
//        }

//        public async Task<OASISResult<ITransactionResponse>> MintTokenAsync(IMintWeb3TokenRequest request)
//        {
//            var result = new OASISResult<ITransactionResponse>();
//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "ThreeFold provider is not activated");
//                    return result;
//                }

//                var mintRequest = new
//                {
//                    tokenAddress = request.TokenAddress,
//                    mintedByAvatarId = request.MintedByAvatarId,
//                    amount = request.MetaData?.ContainsKey("Amount") == true ? request.MetaData["Amount"] : 1m,
//                    symbol = request.Symbol ?? "TFT"
//                };

//                var jsonContent = JsonSerializer.Serialize(mintRequest);
//                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
//                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/tokens/mint", content);

//                if (response.IsSuccessStatusCode)
//                {
//                    var responseContent = await response.Content.ReadAsStringAsync();
//                    var txResponse = JsonSerializer.Deserialize<TransactionResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
//                    if (txResponse != null)
//                    {
//                        result.Result = txResponse;
//                        result.IsError = false;
//                        result.Message = "Token minted successfully on ThreeFold Grid";
//                    }
//                    else
//                    {
//                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize token mint response from ThreeFold Grid API");
//                    }
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error minting token on ThreeFold Grid: {ex.Message}", ex);
//            }
//            return result;
//        }

//        public OASISResult<ITransactionResponse> MintToken(IMintWeb3TokenRequest request)
//        {
//            return MintTokenAsync(request).Result;
//        }

//        public async Task<OASISResult<ITransactionResponse>> BurnTokenAsync(IBurnWeb3TokenRequest request)
//        {
//            var result = new OASISResult<ITransactionResponse>();
//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "ThreeFold provider is not activated");
//                    return result;
//                }

//                var burnRequest = new
//                {
//                    tokenAddress = request.TokenAddress,
//                    burntByAvatarId = request.BurntByAvatarId,
//                    amount = request.MetaData?.ContainsKey("Amount") == true ? request.MetaData["Amount"] : 1m
//                };

//                var jsonContent = JsonSerializer.Serialize(burnRequest);
//                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
//                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/tokens/burn", content);

//                if (response.IsSuccessStatusCode)
//                {
//                    var responseContent = await response.Content.ReadAsStringAsync();
//                    var txResponse = JsonSerializer.Deserialize<TransactionResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
//                    if (txResponse != null)
//                    {
//                        result.Result = txResponse;
//                        result.IsError = false;
//                        result.Message = "Token burned successfully on ThreeFold Grid";
//                    }
//                    else
//                    {
//                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize token burn response from ThreeFold Grid API");
//                    }
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error burning token on ThreeFold Grid: {ex.Message}", ex);
//            }
//            return result;
//        }

//        public OASISResult<ITransactionResponse> BurnToken(IBurnWeb3TokenRequest request)
//        {
//            return BurnTokenAsync(request).Result;
//        }

//        public async Task<OASISResult<ITransactionResponse>> LockTokenAsync(ILockWeb3TokenRequest request)
//        {
//            var result = new OASISResult<ITransactionResponse>();
//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "ThreeFold provider is not activated");
//                    return result;
//                }

//                var lockRequest = new
//                {
//                    tokenAddress = request.TokenAddress,
//                    fromWalletAddress = request.FromWalletAddress,
//                    amount = request.Amount
//                };

//                var jsonContent = JsonSerializer.Serialize(lockRequest);
//                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
//                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/tokens/lock", content);

//                if (response.IsSuccessStatusCode)
//                {
//                    var responseContent = await response.Content.ReadAsStringAsync();
//                    var txResponse = JsonSerializer.Deserialize<TransactionResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
//                    if (txResponse != null)
//                    {
//                        result.Result = txResponse;
//                        result.IsError = false;
//                        result.Message = "Token locked successfully on ThreeFold Grid";
//                    }
//                    else
//                    {
//                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize token lock response from ThreeFold Grid API");
//                    }
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error locking token on ThreeFold Grid: {ex.Message}", ex);
//            }
//            return result;
//        }

//        public OASISResult<ITransactionResponse> LockToken(ILockWeb3TokenRequest request)
//        {
//            return LockTokenAsync(request).Result;
//        }

//        public async Task<OASISResult<ITransactionResponse>> UnlockTokenAsync(IUnlockWeb3TokenRequest request)
//        {
//            var result = new OASISResult<ITransactionResponse>();
//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "ThreeFold provider is not activated");
//                    return result;
//                }

//                var unlockRequest = new
//                {
//                    tokenAddress = request.TokenAddress,
//                    toWalletAddress = request.ToWalletAddress,
//                    amount = request.Amount
//                };

//                var jsonContent = JsonSerializer.Serialize(unlockRequest);
//                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
//                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/tokens/unlock", content);

//                if (response.IsSuccessStatusCode)
//                {
//                    var responseContent = await response.Content.ReadAsStringAsync();
//                    var txResponse = JsonSerializer.Deserialize<TransactionResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
//                    if (txResponse != null)
//                    {
//                        result.Result = txResponse;
//                        result.IsError = false;
//                        result.Message = "Token unlocked successfully on ThreeFold Grid";
//                    }
//                    else
//                    {
//                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize token unlock response from ThreeFold Grid API");
//                    }
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error unlocking token on ThreeFold Grid: {ex.Message}", ex);
//            }
//            return result;
//        }

//        public OASISResult<ITransactionResponse> UnlockToken(IUnlockWeb3TokenRequest request)
//        {
//            return UnlockTokenAsync(request).Result;
//        }

//        public async Task<OASISResult<double>> GetBalanceAsync(IGetWeb3WalletBalanceRequest request)
//        {
//            var result = new OASISResult<double>();
//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "ThreeFold provider is not activated");
//                    return result;
//                }

//                var walletAddress = request.WalletAddress ?? request.AccountAddress;
//                if (string.IsNullOrEmpty(walletAddress))
//                {
//                    OASISErrorHandling.HandleError(ref result, "Wallet address is required");
//                    return result;
//                }

//                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/wallets/{Uri.EscapeDataString(walletAddress)}/balance");

//                if (response.IsSuccessStatusCode)
//                {
//                    var responseContent = await response.Content.ReadAsStringAsync();
//                    var balanceData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
//                    if (balanceData.TryGetProperty("balance", out var balance))
//                    {
//                        if (double.TryParse(balance.GetString() ?? "0", out var balanceAmount))
//                        {
//                            result.Result = balanceAmount;
//                            result.IsError = false;
//                            result.Message = "Balance retrieved successfully from ThreeFold Grid";
//                        }
//                        else
//                        {
//                            OASISErrorHandling.HandleError(ref result, "Failed to parse balance from ThreeFold Grid response");
//                        }
//                    }
//                    else
//                    {
//                        result.Result = 0.0;
//                        result.IsError = false;
//                        result.Message = "Account found but no balance information available";
//                    }
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error getting balance from ThreeFold Grid: {ex.Message}", ex);
//            }
//            return result;
//        }

//        public OASISResult<double> GetBalance(IGetWeb3WalletBalanceRequest request)
//        {
//            return GetBalanceAsync(request).Result;
//        }

//        public async Task<OASISResult<IList<IWalletTransaction>>> GetTransactionsAsync(IGetWeb3TransactionsRequest request)
//        {
//            var result = new OASISResult<IList<IWalletTransaction>>();
//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "ThreeFold provider is not activated");
//                    return result;
//                }

//                var walletAddress = request.WalletAddress ?? request.AccountAddress;
//                if (string.IsNullOrEmpty(walletAddress))
//                {
//                    OASISErrorHandling.HandleError(ref result, "Wallet address is required");
//                    return result;
//                }

//                var limit = request.Limit ?? 100;
//                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/wallets/{Uri.EscapeDataString(walletAddress)}/transactions?limit={limit}");

//                if (response.IsSuccessStatusCode)
//                {
//                    var responseContent = await response.Content.ReadAsStringAsync();
//                    var transactions = JsonSerializer.Deserialize<List<WalletTransaction>>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
//                    if (transactions != null)
//                    {
//                        result.Result = transactions.Cast<IWalletTransaction>().ToList();
//                        result.IsError = false;
//                        result.Message = $"Retrieved {transactions.Count} transactions from ThreeFold Grid";
//                    }
//                    else
//                    {
//                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize transactions from ThreeFold Grid API");
//                    }
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error getting transactions from ThreeFold Grid: {ex.Message}", ex);
//            }
//            return result;
//        }

//        public OASISResult<IList<IWalletTransaction>> GetTransactions(IGetWeb3TransactionsRequest request)
//        {
//            return GetTransactionsAsync(request).Result;
//        }

//        public async Task<OASISResult<IKeyPairAndWallet>> GenerateKeyPairAsync(IGetWeb3WalletBalanceRequest request)
//        {
//            var result = new OASISResult<IKeyPairAndWallet>();
//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "ThreeFold provider is not activated");
//                    return result;
//                }

//                // Generate ThreeFold key pair using KeyManager
//                var keyPairResult = KeyManager.GenerateKeyPairWithWalletAddress(Core.Enums.ProviderType.ThreeFoldOASIS);
//                if (keyPairResult.IsError || keyPairResult.Result == null)
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Failed to generate key pair: {keyPairResult.Message}");
//                    return result;
//                }

//                result.Result = keyPairResult.Result;
//                result.IsError = false;
//                result.Message = "Key pair generated successfully for ThreeFold";
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error generating key pair for ThreeFold: {ex.Message}", ex);
//            }
//            return result;
//        }

//        public OASISResult<IKeyPairAndWallet> GenerateKeyPair(IGetWeb3WalletBalanceRequest request)
//        {
//            return GenerateKeyPairAsync(request).Result;
//        }

//        public async Task<OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>> CreateAccountAsync(CancellationToken token = default)
//        {
//            var result = new OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>();
//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "ThreeFold provider is not activated");
//                    return result;
//                }

//                // Generate key pair and seed phrase using KeyManager
//                var keyPairResult = KeyManager.GenerateKeyPairWithWalletAddress(Core.Enums.ProviderType.ThreeFoldOASIS);
//                if (keyPairResult.IsError || keyPairResult.Result == null)
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Failed to create account: {keyPairResult.Message}");
//                    return result;
//                }

//                // Generate seed phrase for ThreeFold
//                var seedPhrase = KeyHelper.GenerateMnemonic();

//                result.Result = (keyPairResult.Result.PublicKey, keyPairResult.Result.PrivateKey, seedPhrase);
//                result.IsError = false;
//                result.Message = "Account created successfully on ThreeFold Grid";
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error creating account on ThreeFold Grid: {ex.Message}", ex);
//            }
//            return result;
//        }

//        public async Task<OASISResult<(string PublicKey, string PrivateKey)>> RestoreAccountAsync(string seedPhrase, CancellationToken token = default)
//        {
//            var result = new OASISResult<(string PublicKey, string PrivateKey)>();
//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "ThreeFold provider is not activated");
//                    return result;
//                }

//                if (string.IsNullOrWhiteSpace(seedPhrase))
//                {
//                    OASISErrorHandling.HandleError(ref result, "Seed phrase is required");
//                    return result;
//                }

//                // Restore key pair from seed phrase for ThreeFold
//                var keyManager = KeyManager;
//                var keyPairResult = keyManager.GenerateKeyPairWithWalletAddress(Core.Enums.ProviderType.ThreeFoldOASIS);

//                if (keyPairResult.IsError || keyPairResult.Result == null)
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Failed to restore account: {keyPairResult.Message}");
//                    return result;
//                }

//                // Note: In production, derive keys deterministically from seedPhrase using BIP39/BIP44
//                result.Result = (keyPairResult.Result.PublicKey, keyPairResult.Result.PrivateKey);
//                result.IsError = false;
//                result.Message = "Account restored successfully from seed phrase";
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error restoring account from seed phrase: {ex.Message}", ex);
//            }
//            return result;
//        }

//        public async Task<OASISResult<BridgeTransactionResponse>> WithdrawAsync(decimal amount, string senderAccountAddress, string senderPrivateKey)
//        {
//            var result = new OASISResult<BridgeTransactionResponse>();
//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "ThreeFold provider is not activated");
//                    return result;
//                }

//                var withdrawRequest = new
//                {
//                    amount = amount,
//                    senderAccountAddress = senderAccountAddress,
//                    senderPrivateKey = senderPrivateKey
//                };

//                var jsonContent = JsonSerializer.Serialize(withdrawRequest);
//                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
//                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/bridge/withdraw", content);

//                if (response.IsSuccessStatusCode)
//                {
//                    var responseContent = await response.Content.ReadAsStringAsync();
//                    var bridgeResponse = JsonSerializer.Deserialize<BridgeTransactionResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
//                    if (bridgeResponse != null)
//                    {
//                        result.Result = bridgeResponse;
//                        result.IsError = false;
//                        result.Message = "Withdrawal transaction initiated successfully";
//                    }
//                    else
//                    {
//                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize bridge withdrawal response from ThreeFold Grid API");
//                    }
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error withdrawing from ThreeFold Grid: {ex.Message}", ex);
//            }
//            return result;
//        }

//        public async Task<OASISResult<BridgeTransactionResponse>> DepositAsync(decimal amount, string receiverAccountAddress)
//        {
//            var result = new OASISResult<BridgeTransactionResponse>();
//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "ThreeFold provider is not activated");
//                    return result;
//                }

//                var depositRequest = new
//                {
//                    amount = amount,
//                    receiverAccountAddress = receiverAccountAddress
//                };

//                var jsonContent = JsonSerializer.Serialize(depositRequest);
//                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
//                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/bridge/deposit", content);

//                if (response.IsSuccessStatusCode)
//                {
//                    var responseContent = await response.Content.ReadAsStringAsync();
//                    var bridgeResponse = JsonSerializer.Deserialize<BridgeTransactionResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
//                    if (bridgeResponse != null)
//                    {
//                        result.Result = bridgeResponse;
//                        result.IsError = false;
//                        result.Message = "Deposit transaction initiated successfully";
//                    }
//                    else
//                    {
//                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize bridge deposit response from ThreeFold Grid API");
//                    }
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error depositing to ThreeFold Grid: {ex.Message}", ex);
//            }
//            return result;
//        }

//        public async Task<OASISResult<BridgeTransactionStatus>> GetTransactionStatusAsync(string transactionHash, CancellationToken token = default)
//        {
//            var result = new OASISResult<BridgeTransactionStatus>();
//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "ThreeFold provider is not activated");
//                    return result;
//                }

//                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/bridge/transactions/{Uri.EscapeDataString(transactionHash)}/status", token);

//                if (response.IsSuccessStatusCode)
//                {
//                    var responseContent = await response.Content.ReadAsStringAsync();
//                    var statusData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
//                    if (statusData.TryGetProperty("status", out var status))
//                    {
//                        var statusStr = status.GetString() ?? "NotFound";
//                        if (Enum.TryParse<BridgeTransactionStatus>(statusStr, out var statusEnum))
//                        {
//                            result.Result = statusEnum;
//                            result.IsError = false;
//                            result.Message = "Transaction status retrieved successfully";
//                        }
//                        else
//                        {
//                            result.Result = BridgeTransactionStatus.NotFound;
//                            result.IsError = false;
//                            result.Message = "Unknown transaction status";
//                        }
//                    }
//                    else
//                    {
//                        result.Result = BridgeTransactionStatus.NotFound;
//                        result.IsError = false;
//                        result.Message = "Transaction not found";
//                    }
//                }
//                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
//                {
//                    result.Result = BridgeTransactionStatus.NotFound;
//                    result.IsError = false;
//                    result.Message = "Transaction not found";
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error getting transaction status from ThreeFold Grid: {ex.Message}", ex);
//            }
//            return result;
//        }

//        public async Task<OASISResult<IWeb3NFTTransactionResponse>> LockNFTAsync(ILockWeb3NFTRequest request)
//        {
//            var result = new OASISResult<IWeb3NFTTransactionResponse>();
//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "ThreeFold provider is not activated");
//                    return result;
//                }

//                var lockRequest = new
//                {
//                    nftTokenAddress = request.NFTTokenAddress,
//                    web3NFTId = request.Web3NFTId,
//                    lockedByAvatarId = request.LockedByAvatarId
//                };

//                var jsonContent = JsonSerializer.Serialize(lockRequest);
//                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
//                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/nft/lock", content);

//                if (response.IsSuccessStatusCode)
//                {
//                    var responseContent = await response.Content.ReadAsStringAsync();
//                    var nftResponse = JsonSerializer.Deserialize<Web3NFTTransactionResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
//                    if (nftResponse != null)
//                    {
//                        result.Result = nftResponse;
//                        result.IsError = false;
//                        result.Message = "NFT locked successfully on ThreeFold Grid";
//                    }
//                    else
//                    {
//                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize NFT lock response from ThreeFold Grid API");
//                    }
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error locking NFT on ThreeFold Grid: {ex.Message}", ex);
//            }
//            return result;
//        }

//        public OASISResult<IWeb3NFTTransactionResponse> LockNFT(ILockWeb3NFTRequest request)
//        {
//            return LockNFTAsync(request).Result;
//        }

//        public async Task<OASISResult<IWeb3NFTTransactionResponse>> UnlockNFTAsync(IUnlockWeb3NFTRequest request)
//        {
//            var result = new OASISResult<IWeb3NFTTransactionResponse>();
//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "ThreeFold provider is not activated");
//                    return result;
//                }

//                var unlockRequest = new
//                {
//                    nftTokenAddress = request.NFTTokenAddress,
//                    web3NFTId = request.Web3NFTId,
//                    unlockedByAvatarId = request.UnlockedByAvatarId
//                };

//                var jsonContent = JsonSerializer.Serialize(unlockRequest);
//                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
//                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/nft/unlock", content);

//                if (response.IsSuccessStatusCode)
//                {
//                    var responseContent = await response.Content.ReadAsStringAsync();
//                    var nftResponse = JsonSerializer.Deserialize<Web3NFTTransactionResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
//                    if (nftResponse != null)
//                    {
//                        result.Result = nftResponse;
//                        result.IsError = false;
//                        result.Message = "NFT unlocked successfully on ThreeFold Grid";
//                    }
//                    else
//                    {
//                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize NFT unlock response from ThreeFold Grid API");
//                    }
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error unlocking NFT on ThreeFold Grid: {ex.Message}", ex);
//            }
//            return result;
//        }

//        public OASISResult<IWeb3NFTTransactionResponse> UnlockNFT(IUnlockWeb3NFTRequest request)
//        {
//            return UnlockNFTAsync(request).Result;
//        }

//        public async Task<OASISResult<BridgeTransactionResponse>> WithdrawNFTAsync(string nftTokenAddress, string tokenId, string senderAccountAddress, string senderPrivateKey)
//        {
//            var result = new OASISResult<BridgeTransactionResponse>();
//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "ThreeFold provider is not activated");
//                    return result;
//                }

//                var withdrawRequest = new
//                {
//                    nftTokenAddress = nftTokenAddress,
//                    tokenId = tokenId,
//                    senderAccountAddress = senderAccountAddress,
//                    senderPrivateKey = senderPrivateKey
//                };

//                var jsonContent = JsonSerializer.Serialize(withdrawRequest);
//                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
//                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/bridge/nft/withdraw", content);

//                if (response.IsSuccessStatusCode)
//                {
//                    var responseContent = await response.Content.ReadAsStringAsync();
//                    var bridgeResponse = JsonSerializer.Deserialize<BridgeTransactionResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
//                    if (bridgeResponse != null)
//                    {
//                        result.Result = bridgeResponse;
//                        result.IsError = false;
//                        result.Message = "NFT withdrawal transaction initiated successfully";
//                    }
//                    else
//                    {
//                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize bridge NFT withdrawal response from ThreeFold Grid API");
//                    }
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error withdrawing NFT from ThreeFold Grid: {ex.Message}", ex);
//            }
//            return result;
//        }

//        public async Task<OASISResult<BridgeTransactionResponse>> DepositNFTAsync(string nftTokenAddress, string tokenId, string receiverAccountAddress, string sourceTransactionHash = null)
//        {
//            var result = new OASISResult<BridgeTransactionResponse>();
//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "ThreeFold provider is not activated");
//                    return result;
//                }

//                var depositRequest = new
//                {
//                    nftTokenAddress = nftTokenAddress,
//                    tokenId = tokenId,
//                    receiverAccountAddress = receiverAccountAddress,
//                    sourceTransactionHash = sourceTransactionHash
//                };

//                var jsonContent = JsonSerializer.Serialize(depositRequest);
//                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
//                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/bridge/nft/deposit", content);

//                if (response.IsSuccessStatusCode)
//                {
//                    var responseContent = await response.Content.ReadAsStringAsync();
//                    var bridgeResponse = JsonSerializer.Deserialize<BridgeTransactionResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
//                    if (bridgeResponse != null)
//                    {
//                        result.Result = bridgeResponse;
//                        result.IsError = false;
//                        result.Message = "NFT deposit transaction initiated successfully";
//                    }
//                    else
//                    {
//                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize bridge NFT deposit response from ThreeFold Grid API");
//                    }
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"ThreeFold Grid API error: {response.StatusCode} - {response.ReasonPhrase}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error depositing NFT to ThreeFold Grid: {ex.Message}", ex);
//            }
//            return result;
//        }

//        #endregion

//        /*
//        #region IOASISLocalStorageProvider

//        public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> LoadProviderWalletsForAvatarById(Guid id)
//        {
//            var response = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
//            try
//            {
//                // Load provider wallets from ThreeFold grid
//                var wallets = new Dictionary<ProviderType, List<IProviderWallet>>();
                
//                // Add ThreeFold wallet
//                var threeFoldWallet = new ProviderWallet
//                {
//                    Id = Guid.NewGuid(),
//                    ProviderType = ProviderType.ThreeFoldOASIS,
//                    Address = $"threefold://{id}",
//                    PrivateKey = "encrypted_private_key",
//                    PublicKey = "public_key"
//                };
                
//                wallets[ProviderType.ThreeFoldOASIS] = new List<IProviderWallet> { threeFoldWallet };
                
//                response.Result = wallets;
//                response.Message = "Provider wallets loaded from ThreeFold grid successfully";
//            }
//            catch (Exception ex)
//            {
//                response.Exception = ex;
//                OASISErrorHandling.HandleError(ref response, $"Error loading provider wallets from ThreeFold: {ex.Message}");
//            }
//            return response;
//        }

//        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsForAvatarByIdAsync(Guid id)
//        {
//            var response = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
//            try
//            {
//                // Load provider wallets from ThreeFold grid
//                var wallets = new Dictionary<ProviderType, List<IProviderWallet>>();
                
//                // Add ThreeFold wallet
//                var threeFoldWallet = new ProviderWallet
//                {
//                    Id = Guid.NewGuid(),
//                    ProviderType = ProviderType.ThreeFoldOASIS,
//                    Address = $"threefold://{id}",
//                    PrivateKey = "encrypted_private_key",
//                    PublicKey = "public_key"
//                };
                
//                wallets[ProviderType.ThreeFoldOASIS] = new List<IProviderWallet> { threeFoldWallet };
                
//                response.Result = wallets;
//                response.Message = "Provider wallets loaded from ThreeFold grid successfully";
//            }
//            catch (Exception ex)
//            {
//                response.Exception = ex;
//                OASISErrorHandling.HandleError(ref response, $"Error loading provider wallets from ThreeFold: {ex.Message}");
//            }
//            return response;
//        }

//        public OASISResult<bool> SaveProviderWalletsForAvatarById(Guid id, Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
//        {
//            var response = new OASISResult<bool>();
//            try
//            {
//                // Save provider wallets to ThreeFold grid
//                response.Result = true;
//                response.Message = "Provider wallets saved to ThreeFold grid successfully";
//            }
//            catch (Exception ex)
//            {
//                response.Exception = ex;
//                OASISErrorHandling.HandleError(ref response, $"Error saving provider wallets to ThreeFold: {ex.Message}");
//            }
//            return response;
//        }

//        public async Task<OASISResult<bool>> SaveProviderWalletsForAvatarByIdAsync(Guid id, Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
//        {
//            var response = new OASISResult<bool>();
//            try
//            {
//                // Save provider wallets to ThreeFold grid
//                response.Result = true;
//                response.Message = "Provider wallets saved to ThreeFold grid successfully";
//            }
//            catch (Exception ex)
//            {
//                response.Exception = ex;
//                OASISErrorHandling.HandleError(ref response, $"Error saving provider wallets to ThreeFold: {ex.Message}");
//            }
//            return response;
//        }

//        #endregion*/
//    }
//}
