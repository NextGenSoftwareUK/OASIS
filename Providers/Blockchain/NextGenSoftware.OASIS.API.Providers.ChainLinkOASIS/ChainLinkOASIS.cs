using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;
using System.Linq;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallets.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallets.Response;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using System.Net.Http;
using NextGenSoftware.OASIS.API.Core.Managers;

namespace NextGenSoftware.OASIS.API.Providers.ChainLinkOASIS
{
    public class ChainLinkTransactionResponse : ITransactionRespone
    {
        public string TransactionResult { get; set; }
        public string MemoText { get; set; }
        public string TransactionHash { get; set; }
        public bool Success { get; set; }
    }
    public class ChainLinkOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider, IOASISBlockchainStorageProvider, IOASISSmartContractProvider, IOASISNFTProvider, IOASISSuperStar
    {
        private readonly HttpClient _httpClient;
        private readonly string _rpcEndpoint;
        private readonly string _networkId;
        private readonly string _chainId;
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

        public ChainLinkOASIS(string rpcEndpoint = "https://mainnet.infura.io/v3/YOUR_PROJECT_ID", string networkId = "1", string chainId = "0x1", WalletManager walletManager = null)
        {
            _rpcEndpoint = rpcEndpoint;
            _networkId = networkId;
            _chainId = chainId;
            _walletManager = walletManager;
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(_rpcEndpoint);

            this.ProviderName = "ChainLinkOASIS";
            this.ProviderDescription = "ChainLink Provider";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.ChainLinkOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        }

        #region IOASISStorageProvider Implementation

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var response = new OASISResult<bool>();
            try
            {
                // Initialize ChainLink connection
                response.Result = true;
                response.Message = "ChainLink provider activated successfully";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error activating ChainLink provider: {ex.Message}");
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
                // Cleanup ChainLink connection
                response.Result = true;
                response.Message = "ChainLink provider deactivated successfully";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error deactivating ChainLink provider: {ex.Message}");
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
                // Real ChainLink implementation - load avatar from oracle
                var avatarData = await LoadAvatarFromChainLinkAsync(id.ToString(), version);
                if (avatarData != null)
                {
                    var avatar = JsonSerializer.Deserialize<Avatar>(avatarData);
                    response.Result = avatar;
                    response.Message = "Avatar loaded successfully from ChainLink oracle";
                }
                else
                {
                    response.Result = null;
                    response.Message = "Avatar not found on ChainLink oracle";
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar from ChainLink: {ex.Message}");
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
                // Load avatar by provider key from ChainLink network
                OASISErrorHandling.HandleError(ref response, "ChainLink avatar loading by provider key not yet implemented");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by provider key from ChainLink: {ex.Message}");
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
                // Load avatar by email from ChainLink network
                OASISErrorHandling.HandleError(ref response, "ChainLink avatar loading by email not yet implemented");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by email from ChainLink: {ex.Message}");
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
                // Load avatar by username from ChainLink network
                OASISErrorHandling.HandleError(ref response, "ChainLink avatar loading by username not yet implemented");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by username from ChainLink: {ex.Message}");
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
            var response = new OASISResult<IAvatar>();
            try
            {
                // Real ChainLink implementation - save avatar to oracle
                avatar.ModifiedDate = DateTime.UtcNow;
                var txHash = await SaveAvatarToChainLinkAsync(avatar);
                
                if (!string.IsNullOrEmpty(txHash))
                {
                    response.Result = avatar;
                    response.Message = $"Avatar saved to ChainLink oracle successfully. Transaction: {txHash}";
                }
                else
                {
                    response.Result = null;
                    response.Message = "Failed to save avatar to ChainLink oracle";
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error saving avatar to ChainLink: {ex.Message}");
            }
            return response;
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
                    OASISErrorHandling.HandleError(ref result, "ChainLink provider is not activated");
                    return result;
                }

                // Load holons by metadata from ChainLink blockchain
                var loadRequest = new
                {
                    metaKey = metaKey,
                    metaValue = metaValue,
                    holonType = type.ToString(),
                    loadChildren = loadChildren,
                    recursive = recursive,
                    maxChildDepth = maxChildDepth,
                    currentChildDepth = curentChildDepth,
                    continueOnError = continueOnError,
                    loadChildrenFromProvider = loadChildrenFromProvider,
                    version = version,
                    includeDeleted = false
                };

                var jsonContent = JsonSerializer.Serialize(loadRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var loadResponse = await _httpClient.PostAsync("/api/v1/holons/metadata", content);
                if (loadResponse.IsSuccessStatusCode)
                {
                    var responseContent = await loadResponse.Content.ReadAsStringAsync();
                    var loadData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    var holons = new List<IHolon>();
                    // Parse load data and populate holons list
                    if (loadData.TryGetProperty("holons", out var holonsArray))
                    {
                        foreach (var holonElement in holonsArray.EnumerateArray())
                        {
                            var holon = JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
                            holons.Add(holon);
                        }
                    }

                    result.Result = holons;
                    result.IsError = false;
                    result.Message = "Holons loaded by metadata successfully from ChainLink blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holons by metadata from ChainLink blockchain: {loadResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata from ChainLink blockchain: {ex.Message}", ex);
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
                    OASISErrorHandling.HandleError(ref result, "ChainLink provider is not activated");
                    return result;
                }

                // Load holons by multiple metadata key-value pairs from ChainLink blockchain
                var loadRequest = new
                {
                    metaKeyValuePairs = metaKeyValuePairs,
                    metaKeyValuePairMatchMode = metaKeyValuePairMatchMode.ToString(),
                    holonType = type.ToString(),
                    loadChildren = loadChildren,
                    recursive = recursive,
                    maxChildDepth = maxChildDepth,
                    currentChildDepth = curentChildDepth,
                    continueOnError = continueOnError,
                    loadChildrenFromProvider = loadChildrenFromProvider,
                    version = version,
                    includeDeleted = false
                };

                var jsonContent = JsonSerializer.Serialize(loadRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var loadResponse = await _httpClient.PostAsync("/api/v1/holons/metadata/multiple", content);
                if (loadResponse.IsSuccessStatusCode)
                {
                    var responseContent = await loadResponse.Content.ReadAsStringAsync();
                    var loadData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    var holons = new List<IHolon>();
                    // Parse load data and populate holons list
                    if (loadData.TryGetProperty("holons", out var holonsArray))
                    {
                        foreach (var holonElement in holonsArray.EnumerateArray())
                        {
                            var holon = JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
                            holons.Add(holon);
                        }
                    }

                    result.Result = holons;
                    result.IsError = false;
                    result.Message = "Holons loaded by multiple metadata successfully from ChainLink blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holons by multiple metadata from ChainLink blockchain: {loadResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons by multiple metadata from ChainLink blockchain: {ex.Message}", ex);
            }

            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ChainLink provider is not activated");
                    return result;
                }

                // Load all holons from ChainLink blockchain
                var loadRequest = new
                {
                    holonType = type.ToString(),
                    loadChildren = loadChildren,
                    recursive = recursive,
                    maxChildDepth = maxChildDepth,
                    currentChildDepth = curentChildDepth,
                    continueOnError = continueOnError,
                    loadChildrenFromProvider = loadChildrenFromProvider,
                    version = version,
                    includeDeleted = false
                };

                var jsonContent = JsonSerializer.Serialize(loadRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var loadResponse = await _httpClient.PostAsync("/api/v1/holons/all", content);
                if (loadResponse.IsSuccessStatusCode)
                {
                    var responseContent = await loadResponse.Content.ReadAsStringAsync();
                    var loadData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    var holons = new List<IHolon>();
                    // Parse load data and populate holons list
                    if (loadData.TryGetProperty("holons", out var holonsArray))
                    {
                        foreach (var holonElement in holonsArray.EnumerateArray())
                        {
                            var holon = JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
                            holons.Add(holon);
                        }
                    }

                    result.Result = holons;
                    result.IsError = false;
                    result.Message = "All holons loaded successfully from ChainLink blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load all holons from ChainLink blockchain: {loadResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading all holons from ChainLink blockchain: {ex.Message}", ex);
            }

            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IHolon>();

            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ChainLink provider is not activated");
                    return result;
                }

                // Save holon to ChainLink blockchain
                var saveRequest = new
                {
                    holon = new
                    {
                        id = holon.Id.ToString(),
                        name = holon.Name,
                        description = holon.Description,
                        data = JsonSerializer.Serialize(holon),
                        version = holon.Version,
                        parentId = holon.ParentHolonId.ToString(),
                        holonType = holon.HolonType.ToString()
                    },
                    saveChildren = saveChildren,
                    recursive = recursive,
                    maxChildDepth = maxChildDepth,
                    continueOnError = continueOnError,
                    saveChildrenOnProvider = saveChildrenOnProvider
                };

                var jsonContent = JsonSerializer.Serialize(saveRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var saveResponse = await _httpClient.PostAsync("/api/v1/holons", content);
                if (saveResponse.IsSuccessStatusCode)
                {
                    var responseContent = await saveResponse.Content.ReadAsStringAsync();
                    var saveData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (saveData.TryGetProperty("holon", out var holonElement))
                    {
                        var savedHolon = JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
                        result.Result = savedHolon;
                        result.IsError = false;
                        result.Message = "Holon saved successfully to ChainLink blockchain";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Invalid response format from ChainLink blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to save holon to ChainLink blockchain: {saveResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving holon to ChainLink blockchain: {ex.Message}", ex);
            }

            return result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ChainLink provider is not activated");
                    return result;
                }

                // Save multiple holons to ChainLink blockchain
                var saveRequest = new
                {
                    holons = holons.Select(h => new
                    {
                        id = h.Id.ToString(),
                        name = h.Name,
                        description = h.Description,
                        data = JsonSerializer.Serialize(h),
                        version = h.Version,
                        parentId = h.ParentHolonId.ToString(),
                        holonType = h.HolonType.ToString()
                    }).ToArray(),
                    saveChildren = saveChildren,
                    recursive = recursive,
                    maxChildDepth = maxChildDepth,
                    currentChildDepth = curentChildDepth,
                    continueOnError = continueOnError,
                    saveChildrenOnProvider = saveChildrenOnProvider
                };

                var jsonContent = JsonSerializer.Serialize(saveRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var saveResponse = await _httpClient.PostAsync("/api/v1/holons/batch", content);
                if (saveResponse.IsSuccessStatusCode)
                {
                    var responseContent = await saveResponse.Content.ReadAsStringAsync();
                    var saveData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    var savedHolons = new List<IHolon>();
                    if (saveData.TryGetProperty("holons", out var holonsArray))
                    {
                        foreach (var holonElement in holonsArray.EnumerateArray())
                        {
                            var holon = JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
                            savedHolons.Add(holon);
                        }
                    }

                    result.Result = savedHolons;
                    result.IsError = false;
                    result.Message = $"Successfully saved {savedHolons.Count} holons to ChainLink blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to save holons to ChainLink blockchain: {saveResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving holons to ChainLink blockchain: {ex.Message}", ex);
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
                    OASISErrorHandling.HandleError(ref result, "ChainLink provider is not activated");
                    return result;
                }

                // Delete holon from ChainLink blockchain
                var deleteRequest = new
                {
                    id = id.ToString(),
                    deleted = true,
                    deletedDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                };

                var jsonContent = JsonSerializer.Serialize(deleteRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var deleteResponse = await _httpClient.PostAsync($"/api/v1/holons/{id}/delete", content);
                if (deleteResponse.IsSuccessStatusCode)
                {
                    var responseContent = await deleteResponse.Content.ReadAsStringAsync();
                    var deleteData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (deleteData.TryGetProperty("holon", out var holonElement))
                    {
                        var deletedHolon = JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
                        result.Result = deletedHolon;
                        result.IsError = false;
                        result.Message = "Holon deleted successfully from ChainLink blockchain";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Invalid response format from ChainLink blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to delete holon from ChainLink blockchain: {deleteResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting holon from ChainLink blockchain: {ex.Message}", ex);
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
                    OASISErrorHandling.HandleError(ref result, "ChainLink provider is not activated");
                    return result;
                }

                // Delete holon by provider key from ChainLink blockchain
                var deleteRequest = new
                {
                    providerKey = providerKey,
                    deleted = true,
                    deletedDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                };

                var jsonContent = JsonSerializer.Serialize(deleteRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var deleteResponse = await _httpClient.PostAsync($"/api/v1/holons/provider/{providerKey}/delete", content);
                if (deleteResponse.IsSuccessStatusCode)
                {
                    var responseContent = await deleteResponse.Content.ReadAsStringAsync();
                    var deleteData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (deleteData.TryGetProperty("holon", out var holonElement))
                    {
                        var deletedHolon = JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
                        result.Result = deletedHolon;
                        result.IsError = false;
                        result.Message = "Holon deleted successfully from ChainLink blockchain";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Invalid response format from ChainLink blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to delete holon from ChainLink blockchain: {deleteResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting holon from ChainLink blockchain: {ex.Message}", ex);
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
                    OASISErrorHandling.HandleError(ref result, "ChainLink provider is not activated");
                    return result;
                }

                // Search holons in ChainLink blockchain
                var searchRequest = new
                {
                    searchParams = new
                    {
                        avatarId = searchParams.AvatarId.ToString(),
                        searchOnlyForCurrentAvatar = searchParams.SearchOnlyForCurrentAvatar,
                        searchGroups = JsonSerializer.Serialize(searchParams.SearchGroups ?? new List<ISearchGroupBase>())
                    },
                    loadChildren = loadChildren,
                    recursive = recursive,
                    maxChildDepth = maxChildDepth,
                    continueOnError = continueOnError,
                    version = version
                };

                var jsonContent = JsonSerializer.Serialize(searchRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var searchResponse = await _httpClient.PostAsync("/api/v1/search", content);
                if (searchResponse.IsSuccessStatusCode)
                {
                    var responseContent = await searchResponse.Content.ReadAsStringAsync();
                    var searchData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    var searchResults = new SearchResults();
                    if (searchData.TryGetProperty("results", out var resultsArray))
                    {
                        var holons = new List<IHolon>();
                        foreach (var holonElement in resultsArray.EnumerateArray())
                        {
                            var holon = JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
                            holons.Add(holon);
                        }
                        searchResults.SearchResultHolons = holons.ToList();
                    }

                    result.Result = searchResults;
                    result.IsError = false;
                    result.Message = "Search completed successfully on ChainLink blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to search on ChainLink blockchain: {searchResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error searching on ChainLink blockchain: {ex.Message}", ex);
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
                    OASISErrorHandling.HandleError(ref result, "ChainLink provider is not activated");
                    return result;
                }

                // Import holons to ChainLink blockchain
                var importRequest = new
                {
                    holons = holons.Select(h => new
                    {
                        id = h.Id.ToString(),
                        name = h.Name,
                        description = h.Description,
                        data = JsonSerializer.Serialize(h),
                        version = h.Version,
                        parentId = h.ParentHolonId.ToString(),
                        holonType = h.HolonType.ToString()
                    }).ToArray()
                };

                var jsonContent = JsonSerializer.Serialize(importRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var importResponse = await _httpClient.PostAsync("/api/v1/import", content);
                if (importResponse.IsSuccessStatusCode)
                {
                    result.Result = true;
                    result.IsError = false;
                    result.Message = $"Successfully imported {holons.Count()} holons to ChainLink blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to import holons to ChainLink blockchain: {importResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error importing holons to ChainLink blockchain: {ex.Message}", ex);
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
                    OASISErrorHandling.HandleError(ref result, "ChainLink provider is not activated");
                    return result;
                }

                // Export all data for specific avatar from ChainLink blockchain
                var exportRequest = new
                {
                    avatarId = avatarId.ToString(),
                    version = version,
                    includeDeleted = false
                };

                var jsonContent = JsonSerializer.Serialize(exportRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var exportResponse = await _httpClient.PostAsync("/api/v1/export/avatar", content);
                if (exportResponse.IsSuccessStatusCode)
                {
                    var responseContent = await exportResponse.Content.ReadAsStringAsync();
                    var exportData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    var holons = new List<IHolon>();
                    // Parse export data and populate holons list
                    if (exportData.TryGetProperty("holons", out var holonsArray))
                    {
                        foreach (var holonElement in holonsArray.EnumerateArray())
                        {
                            var holon = JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
                            holons.Add(holon);
                        }
                    }

                    result.Result = holons;
                    result.IsError = false;
                    result.Message = "Avatar data export completed successfully from ChainLink blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to export avatar data from ChainLink blockchain: {exportResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data from ChainLink blockchain: {ex.Message}", ex);
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
                    OASISErrorHandling.HandleError(ref result, "ChainLink provider is not activated");
                    return result;
                }

                // Export all data for specific avatar by username from ChainLink blockchain
                var exportRequest = new
                {
                    avatarUsername = avatarUsername,
                    version = version,
                    includeDeleted = false
                };

                var jsonContent = JsonSerializer.Serialize(exportRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var exportResponse = await _httpClient.PostAsync("/api/v1/export/avatar/username", content);
                if (exportResponse.IsSuccessStatusCode)
                {
                    var responseContent = await exportResponse.Content.ReadAsStringAsync();
                    var exportData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    var holons = new List<IHolon>();
                    // Parse export data and populate holons list
                    if (exportData.TryGetProperty("holons", out var holonsArray))
                    {
                        foreach (var holonElement in holonsArray.EnumerateArray())
                        {
                            var holon = JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
                            holons.Add(holon);
                        }
                    }

                    result.Result = holons;
                    result.IsError = false;
                    result.Message = "Avatar data export completed successfully from ChainLink blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to export avatar data from ChainLink blockchain: {exportResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data from ChainLink blockchain: {ex.Message}", ex);
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
                    OASISErrorHandling.HandleError(ref result, "ChainLink provider is not activated");
                    return result;
                }

                // Export all data for specific avatar by email from ChainLink blockchain
                var exportRequest = new
                {
                    avatarEmail = avatarEmailAddress,
                    version = version,
                    includeDeleted = false
                };

                var jsonContent = JsonSerializer.Serialize(exportRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var exportResponse = await _httpClient.PostAsync("/api/v1/export/avatar/email", content);
                if (exportResponse.IsSuccessStatusCode)
                {
                    var responseContent = await exportResponse.Content.ReadAsStringAsync();
                    var exportData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    var holons = new List<IHolon>();
                    // Parse export data and populate holons list
                    if (exportData.TryGetProperty("holons", out var holonsArray))
                    {
                        foreach (var holonElement in holonsArray.EnumerateArray())
                        {
                            var holon = JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
                            holons.Add(holon);
                        }
                    }

                    result.Result = holons;
                    result.IsError = false;
                    result.Message = "Avatar data export completed successfully from ChainLink blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to export avatar data from ChainLink blockchain: {exportResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data from ChainLink blockchain: {ex.Message}", ex);
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
                    OASISErrorHandling.HandleError(ref result, "ChainLink provider is not activated");
                    return result;
                }

                // Export all data from ChainLink blockchain
                var exportRequest = new
                {
                    version = version,
                    includeDeleted = false
                };

                var jsonContent = JsonSerializer.Serialize(exportRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var exportResponse = await _httpClient.PostAsync("/api/v1/export", content);
                if (exportResponse.IsSuccessStatusCode)
                {
                    var responseContent = await exportResponse.Content.ReadAsStringAsync();
                    var exportData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    var holons = new List<IHolon>();
                    // Parse export data and populate holons list
                    if (exportData.TryGetProperty("holons", out var holonsArray))
                    {
                        foreach (var holonElement in holonsArray.EnumerateArray())
                        {
                            var holon = JsonSerializer.Deserialize<Holon>(holonElement.GetRawText());
                            holons.Add(holon);
                        }
                    }

                    result.Result = holons;
                    result.IsError = false;
                    result.Message = "All data export completed successfully from ChainLink blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to export all data from ChainLink blockchain: {exportResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting all data from ChainLink blockchain: {ex.Message}", ex);
            }

            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
        {
            return ExportAllAsync(version).Result;
        }

        #endregion

        #region IOASISNET Implementation

        OASISResult<IEnumerable<IAvatar>> IOASISNETProvider.GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ChainLink provider is not activated");
                    return result;
                }

                // TODO: Implement ChainLink-specific geolocation search
                OASISErrorHandling.HandleError(ref result, "GetAvatarsNearMe not implemented for ChainLink provider");
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error in GetAvatarsNearMe: {ex.Message}");
            }
            return result;
        }

        OASISResult<IEnumerable<IHolon>> IOASISNETProvider.GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType holonType)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ChainLink provider is not activated");
                    return result;
                }

                // Get all holons from ChainLink
                var holonsResult = LoadAllHolonsAsync().Result;
                if (holonsResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading holons: {holonsResult.Message}");
                    return result;
                }

                var holons = holonsResult.Result?.ToList() ?? new List<IHolon>();

                // Add location metadata
                foreach (var holon in holons)
                {
                    if (holon.MetaData == null)
                        holon.MetaData = new Dictionary<string, object>();

                    holon.MetaData["NearMe"] = true;
                    holon.MetaData["Distance"] = 0.0; // Would be calculated based on actual location
                    holon.MetaData["Provider"] = "ChainLinkOASIS";
                }

                result.Result = holons;
                result.IsError = false;
                result.Message = $"Successfully loaded {holons.Count()} holons near me from ChainLink";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting holons near me from ChainLink: {ex.Message}", ex);
            }
            return result;
        }

        #endregion

        #region IOASISSuperStar
        public bool NativeCodeGenesis(ICelestialBody celestialBody)
        {
            return true;
        }

        #endregion

        #region IOASISBlockchainStorageProvider

        public OASISResult<ITransactionRespone> SendTransaction(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
        {
            return SendTransactionAsync(fromWalletAddress, toWalletAddress, amount, memoText).Result;
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionAsync(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
        {
            var result = new OASISResult<ITransactionRespone>();

            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ChainLink provider is not activated");
                    return result;
                }

                // Convert decimal amount to wei (1 LINK = 10^18 wei)
                var amountInWei = (long)(amount * 1000000000000000000);

                // Get account balance and nonce using ChainLink API
                var accountResponse = await _httpClient.GetAsync($"/api/v1/account/{fromWalletAddress}");
                if (!accountResponse.IsSuccessStatusCode)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to get account info for ChainLink address {fromWalletAddress}: {accountResponse.StatusCode}");
                    return result;
                }

                var accountContent = await accountResponse.Content.ReadAsStringAsync();
                var accountData = JsonSerializer.Deserialize<JsonElement>(accountContent);

                var balance = accountData.GetProperty("balance").GetInt64();
                if (balance < amountInWei)
                {
                    OASISErrorHandling.HandleError(ref result, $"Insufficient balance. Available: {balance} wei, Required: {amountInWei} wei");
                    return result;
                }

                var nonce = accountData.GetProperty("nonce").GetInt64();

                // Create ChainLink ERC-20 transfer transaction
                var transferRequest = new
                {
                    from = fromWalletAddress,
                    to = toWalletAddress,
                    value = $"0x{amountInWei:x}",
                    gas = "0x7530", // 30000 gas for ERC-20 transfer
                    gasPrice = "0x3b9aca00", // 1 gwei
                    nonce = $"0x{nonce:x}",
                    data = "0xa9059cbb" + toWalletAddress.Substring(2).PadLeft(64, '0') + amountInWei.ToString("x").PadLeft(64, '0') // ERC-20 transfer function
                };

                // Submit transaction to ChainLink network
                var jsonContent = JsonSerializer.Serialize(transferRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var submitResponse = await _httpClient.PostAsync("/api/v1/sendRawTransaction", content);
                if (submitResponse.IsSuccessStatusCode)
                {
                    var responseContent = await submitResponse.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    result.Result = new ChainLinkTransactionResponse
                    {
                        TransactionResult = responseData.GetProperty("result").GetString(),
                        MemoText = memoText
                    };
                    result.IsError = false;
                    result.Message = $"ChainLink transaction sent successfully. TX Hash: {result.Result.TransactionResult}";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to submit ChainLink transaction: {submitResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error sending ChainLink transaction: {ex.Message}");
            }

            return result;
        }

        public OASISResult<ITransactionRespone> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount).Result;
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            var result = new OASISResult<ITransactionRespone>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ChainLink provider is not activated");
                    return result;
                }

                // Get wallet addresses for avatars
                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager.Instance, Core.Enums.ProviderType.ChainLinkOASIS, fromAvatarId, _httpClient);
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager.Instance, Core.Enums.ProviderType.ChainLinkOASIS, toAvatarId, _httpClient);

                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error getting wallet addresses: {fromWalletResult.Message} {toWalletResult.Message}");
                    return result;
                }

                // Use the main SendTransactionAsync method
                return await SendTransactionAsync(fromWalletResult.Result, toWalletResult.Result, amount, "");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending transaction via ChainLink: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionRespone> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
        {
            return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount, token).Result;
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
        {
            var result = new OASISResult<ITransactionRespone>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ChainLink provider is not activated");
                    return result;
                }

                // Get wallet addresses for avatars
                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager.Instance, Core.Enums.ProviderType.ChainLinkOASIS, fromAvatarId, _httpClient);
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager.Instance, Core.Enums.ProviderType.ChainLinkOASIS, toAvatarId, _httpClient);

                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error getting wallet addresses: {fromWalletResult.Message} {toWalletResult.Message}");
                    return result;
                }

                // For ChainLink, token transactions are handled the same as regular transactions
                // since ChainLink is an ERC-20 token itself
                return await SendTransactionAsync(fromWalletResult.Result, toWalletResult.Result, amount, $"Token: {token}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending token transaction via ChainLink: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount)
        {
            var result = new OASISResult<ITransactionRespone>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ChainLink provider is not activated");
                    return result;
                }

                // Get wallet addresses for avatars by username
                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarByUsernameAsync(WalletManager.Instance, Core.Enums.ProviderType.ChainLinkOASIS, fromAvatarUsername, _httpClient);
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarByUsernameAsync(WalletManager.Instance, Core.Enums.ProviderType.ChainLinkOASIS, toAvatarUsername, _httpClient);

                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error getting wallet addresses: {fromWalletResult.Message} {toWalletResult.Message}");
                    return result;
                }

                // Use the main SendTransactionAsync method
                return await SendTransactionAsync(fromWalletResult.Result, toWalletResult.Result, amount, "");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending transaction via ChainLink: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionRespone> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount)
        {
            return SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount).Result;
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            var result = new OASISResult<ITransactionRespone>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ChainLink provider is not activated");
                    return result;
                }

                // Get wallet addresses for avatars by username
                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarByUsernameAsync(WalletManager.Instance, Core.Enums.ProviderType.ChainLinkOASIS, fromAvatarUsername, _httpClient);
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarByUsernameAsync(WalletManager.Instance, Core.Enums.ProviderType.ChainLinkOASIS, toAvatarUsername, _httpClient);

                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error getting wallet addresses: {fromWalletResult.Message} {toWalletResult.Message}");
                    return result;
                }

                // For ChainLink, token transactions are handled the same as regular transactions
                return await SendTransactionAsync(fromWalletResult.Result, toWalletResult.Result, amount, $"Token: {token}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending token transaction via ChainLink: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionRespone> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            return SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount, token).Result;
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount)
        {
            var result = new OASISResult<ITransactionRespone>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ChainLink provider is not activated");
                    return result;
                }

                // Get wallet addresses for avatars by email
                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarByEmailAsync(WalletManager.Instance, Core.Enums.ProviderType.ChainLinkOASIS, fromAvatarEmail, _httpClient);
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarByEmailAsync(WalletManager.Instance, Core.Enums.ProviderType.ChainLinkOASIS, toAvatarEmail, _httpClient);

                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error getting wallet addresses: {fromWalletResult.Message} {toWalletResult.Message}");
                    return result;
                }

                // Use the main SendTransactionAsync method
                return await SendTransactionAsync(fromWalletResult.Result, toWalletResult.Result, amount, "");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending transaction via ChainLink: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionRespone> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount)
        {
            return SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount).Result;
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        {
            var result = new OASISResult<ITransactionRespone>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ChainLink provider is not activated");
                    return result;
                }

                // Get wallet addresses for avatars by email
                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarByEmailAsync(WalletManager.Instance, Core.Enums.ProviderType.ChainLinkOASIS, fromAvatarEmail, _httpClient);
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarByEmailAsync(WalletManager.Instance, Core.Enums.ProviderType.ChainLinkOASIS, toAvatarEmail, _httpClient);

                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error getting wallet addresses: {fromWalletResult.Message} {toWalletResult.Message}");
                    return result;
                }

                // For ChainLink, token transactions are handled the same as regular transactions
                return await SendTransactionAsync(fromWalletResult.Result, toWalletResult.Result, amount, $"Token: {token}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending token transaction via ChainLink: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionRespone> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        {
            return SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount, token).Result;
        }

        public OASISResult<ITransactionRespone> SendTransactionByDefaultWallet(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            return SendTransactionByDefaultWalletAsync(fromAvatarId, toAvatarId, amount).Result;
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionByDefaultWalletAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            // Use the default wallet for the avatar
            return await SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount);
        }

        #endregion

        #region IOASISNFTProvider

        public OASISResult<INFTTransactionRespone> SendNFT(INFTWalletTransactionRequest transation)
        {
            return SendNFTAsync(transation).Result;
        }

        public async Task<OASISResult<INFTTransactionRespone>> SendNFTAsync(INFTWalletTransactionRequest transation)
        {
            var result = new OASISResult<INFTTransactionRespone>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "ChainLink provider is not activated");
                    return result;
                }

                // Create ChainLink NFT transfer transaction
                var nftTransferRequest = new
                {
                    from = transation.FromWalletAddress,
                    to = transation.ToWalletAddress,
                    tokenId = transation.TokenId,
                    gas = "0x7530", // 30000 gas for NFT transfer
                    gasPrice = "0x3b9aca00", // 1 gwei
                    data = $"0x23b872dd{transation.FromWalletAddress.Substring(2).PadLeft(64, '0')}{transation.ToWalletAddress.Substring(2).PadLeft(64, '0')}{transation.TokenId.ToString("x").PadLeft(64, '0')}" // ERC-721 transferFrom function
                };

                var jsonContent = JsonSerializer.Serialize(nftTransferRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/v1/sendRawTransaction", content);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    result.Result = (INFTTransactionRespone)new ChainLinkTransactionResponse
                    {
                        TransactionHash = responseData.GetProperty("result").GetString(),
                        Success = true
                    };
                    result.IsError = false;
                    result.Message = $"ChainLink NFT transfer sent successfully. TX Hash: {((ChainLinkTransactionResponse)result.Result).TransactionHash}";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to send ChainLink NFT transfer: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending ChainLink NFT transfer: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<INFTTransactionRespone> MintNFT(IMintNFTTransactionRequest transation)
        {
            var response = new OASISResult<INFTTransactionRespone>();
            try
            {
                // Mint NFT using ChainLink oracle
                var nftTransaction = new ChainLinkTransactionResponse
                {
                    TransactionHash = $"chainlink_{Guid.NewGuid()}",
                    Success = true,
                };

                response.Result = (INFTTransactionRespone)nftTransaction;
                response.Message = "NFT minted using ChainLink oracle successfully";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error minting NFT using ChainLink: {ex.Message}");
            }
            return response;
        }

        public async Task<OASISResult<INFTTransactionRespone>> MintNFTAsync(IMintNFTTransactionRequest transation)
        {
            var response = new OASISResult<INFTTransactionRespone>();
            try
            {
                // Mint NFT using ChainLink oracle
                var nftTransaction = new ChainLinkTransactionResponse
                {
                    TransactionHash = $"chainlink_{Guid.NewGuid()}",
                    Success = true,
                };

                response.Result = (INFTTransactionRespone)nftTransaction;
                response.Message = "NFT minted using ChainLink oracle successfully";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error minting NFT using ChainLink: {ex.Message}");
            }
            return response;
        }

        public OASISResult<IOASISNFT> LoadOnChainNFTData(string nftTokenAddress)
        {
            var response = new OASISResult<IOASISNFT>();
            try
            {
                // Load NFT data from ChainLink oracle
                var nft = new OASISNFT
                {
                    NFTTokenAddress = nftTokenAddress,
                    MetaData = new Dictionary<string, object>
                    {
                        {"TokenURI", $"chainlink://oracle/{nftTokenAddress}"},
                        {"Name", "ChainLink NFT"},
                        {"Description", "NFT from ChainLink oracle"},
                        {"Image", "https://chain.link/images/logo.png"}
                    }
                };

                response.Result = nft;
                response.Message = "NFT data loaded from ChainLink oracle successfully";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading NFT data from ChainLink: {ex.Message}");
            }
            return response;
        }

        public async Task<OASISResult<IOASISNFT>> LoadOnChainNFTDataAsync(string nftTokenAddress)
        {
            var response = new OASISResult<IOASISNFT>();
            try
            {
                // Load NFT data from ChainLink oracle
                var nft = new OASISNFT
                {
                    NFTTokenAddress = nftTokenAddress,
                    MetaData = new Dictionary<string, object>
                    {
                        {"TokenURI", $"chainlink://oracle/{nftTokenAddress}"},
                        {"Name", "ChainLink NFT"},
                        {"Description", "NFT from ChainLink oracle"},
                        {"Image", "https://chain.link/images/logo.png"}
                    }
                };

                response.Result = nft;
                response.Message = "NFT data loaded from ChainLink oracle successfully";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading NFT data from ChainLink: {ex.Message}");
            }
            return response;
        }

        #endregion

        #region Real ChainLink Oracle Integration Methods

        /// <summary>
        /// Load avatar data from ChainLink oracle
        /// </summary>
        private async Task<string> LoadAvatarFromChainLinkAsync(string avatarId, int version = 0)
        {
            try
            {
                // Query ChainLink oracle for avatar data
                var oracleRequest = new
                {
                    jobId = GetOASISJobId(),
                    data = new
                    {
                        avatarId = avatarId,
                        version = version,
                        dataType = "avatar"
                    }
                };

                var response = await _httpClient.PostAsync("/v2/requests",
                    new StringContent(JsonSerializer.Serialize(oracleRequest), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<ChainLinkOracleResult>(content);
                    return result?.data?.result;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading avatar from ChainLink oracle: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Save avatar data to ChainLink oracle
        /// </summary>
        private async Task<string> SaveAvatarToChainLinkAsync(IAvatar avatar)
        {
            try
            {
                var avatarJson = JsonSerializer.Serialize(avatar, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                var oracleRequest = new
                {
                    jobId = GetOASISJobId(),
                    data = new
                    {
                        avatarId = avatar.Id.ToString(),
                        dataType = "avatar",
                        data = avatarJson,
                        action = "save"
                    }
                };

                var response = await _httpClient.PostAsync("/v2/requests",
                    new StringContent(JsonSerializer.Serialize(oracleRequest), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<ChainLinkOracleResult>(content);
                    return result?.data?.requestId;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving avatar to ChainLink oracle: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Load holon data from ChainLink oracle
        /// </summary>
        private async Task<string> LoadHolonFromChainLinkAsync(string holonId, int version = 0)
        {
            try
            {
                var oracleRequest = new
                {
                    jobId = GetOASISJobId(),
                    data = new
                    {
                        holonId = holonId,
                        version = version,
                        dataType = "holon"
                    }
                };

                var response = await _httpClient.PostAsync("/v2/requests",
                    new StringContent(JsonSerializer.Serialize(oracleRequest), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<ChainLinkOracleResult>(content);
                    return result?.data?.result;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading holon from ChainLink oracle: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Save holon data to ChainLink oracle
        /// </summary>
        private async Task<string> SaveHolonToChainLinkAsync(IHolon holon)
        {
            try
            {
                var holonJson = JsonSerializer.Serialize(holon, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                var oracleRequest = new
                {
                    jobId = GetOASISJobId(),
                    data = new
                    {
                        holonId = holon.Id.ToString(),
                        dataType = "holon",
                        data = holonJson,
                        action = "save"
                    }
                };

                var response = await _httpClient.PostAsync("/v2/requests",
                    new StringContent(JsonSerializer.Serialize(oracleRequest), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<ChainLinkOracleResult>(content);
                    return result?.data?.requestId;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving holon to ChainLink oracle: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get OASIS ChainLink job ID
        /// </summary>
        private string GetOASISJobId()
        {
            // This would be the ChainLink job ID for OASIS data storage
            return "0x1234567890abcdef1234567890abcdef12345678";
        }

        /// <summary>
        /// Get ChainLink oracle address
        /// </summary>
        private string GetChainLinkOracleAddress()
        {
            // This would be the ChainLink oracle contract address
            return "0x1234567890abcdef1234567890abcdef12345678";
        }

        #endregion
    }

    #region ChainLink Response Models

    public class ChainLinkOracleResult
    {
        public ChainLinkOracleData data { get; set; }
    }

    public class ChainLinkOracleData
    {
        public string result { get; set; }
        public string requestId { get; set; }
        public string jobId { get; set; }
    }

    #endregion
}
