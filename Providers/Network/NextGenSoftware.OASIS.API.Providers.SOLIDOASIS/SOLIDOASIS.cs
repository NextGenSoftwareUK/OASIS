using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.SOLIDOASIS
{
    /// <summary>
    /// SOLID (Social Linked Data) Provider for OASIS.
    /// Implements Tim Berners-Lee's decentralized web standard where users store data in personal "pods"
    /// via the Linked Data Platform (LDP) HTTP protocol using RDF/Turtle format.
    /// </summary>
    public partial class SOLIDOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider, IOASISSuperStar, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _podServerUrl;
        private bool _isActivated;
        private bool _disposed;

        public SOLIDOASIS(string podServerUrl = "https://solidcommunity.net", string authToken = "")
            :this(podServerUrl,authToken,null,new HttpClientHandler()) { }

        public SOLIDOASIS(string podServerUrl,string authToken,byte[] storageKey)
            :this(podServerUrl,authToken,storageKey,new HttpClientHandler()) { }

        public SOLIDOASIS(string podServerUrl,string authToken,byte[] storageKey,HttpMessageHandler handler)
        {
            this.ProviderName = "SOLIDOASIS";
            this.ProviderDescription = "SOLID (Social Linked Data) Provider — decentralized personal data storage in LDP pods";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.SOLIDOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);

            _podServerUrl = (podServerUrl ?? throw new ArgumentNullException(nameof(podServerUrl))).TrimEnd('/');
            _privateRecords=storageKey==null?null:new SolidPrivateRecordCodec(new Uri(_podServerUrl),storageKey);
            _httpClient = new HttpClient(handler??throw new ArgumentNullException(nameof(handler))) { Timeout = TimeSpan.FromSeconds(30) };
            _httpClient.DefaultRequestHeaders.Add("Accept", "text/turtle, application/ld+json;q=0.9, */*;q=0.8");

            if (!string.IsNullOrEmpty(authToken))
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);
        }

        #region IOASISStorageProvider

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                if (_isActivated)
                {
                    result.Result = true;
                    result.Message = "SOLID provider already activated";
                    return result;
                }
                var response = await _httpClient.GetAsync($"{_podServerUrl}/");
                if (response.IsSuccessStatusCode)
                {
                    _isActivated = true;
                    IsProviderActivated = true;
                    result.Result = true;
                    result.Message = "SOLID provider activated successfully";
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Failed to connect to SOLID pod server: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error activating SOLID provider: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                _isActivated = false;
                IsProviderActivated = false;
                result.Result = true;
                result.Message = "SOLID provider deactivated";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deactivating SOLID provider: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
            => await LoadPrivateRecordAsync<IAvatar>(id,version);

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) => LoadAvatarAsync(id, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
            => await LoadPrivateRecordByKeyAsync<IAvatar>(providerKey,version);

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0) => LoadAvatarByProviderKeyAsync(providerKey, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
            => await FindPrivateRecordAsync<IAvatar>(avatarEmail,record=>record.Email,version);

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0) => LoadAvatarByEmailAsync(avatarEmail, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
            => await FindPrivateRecordAsync<IAvatar>(avatarUsername,record=>record.Username,version);

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0) => LoadAvatarByUsernameAsync(avatarUsername, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
            => await LoadPrivateRecordAsync<IAvatarDetail>(id,version);

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => LoadAvatarDetailAsync(id, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
            => await FindPrivateRecordAsync<IAvatarDetail>(avatarEmail,record=>record.Email,version);

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0) => LoadAvatarDetailByEmailAsync(avatarEmail, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
            => await FindPrivateRecordAsync<IAvatarDetail>(avatarUsername,record=>record.Username,version);

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0) => LoadAvatarDetailByUsernameAsync(avatarUsername, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
            => await LoadPrivateRecordsAsync<IAvatar>(version);

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) => LoadAllAvatarsAsync(version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
            => await LoadPrivateRecordsAsync<IAvatarDetail>(version);

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0) => LoadAllAvatarDetailsAsync(version).Result;

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
            => await SavePrivateRecordAsync(avatar);

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) => SaveAvatarAsync(avatar).Result;

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
            => await SavePrivateRecordAsync(avatarDetail);

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail) => SaveAvatarDetailAsync(avatarDetail).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!_isActivated) { OASISErrorHandling.HandleError(ref result, "Provider not activated"); return result; }
                if (softDelete)
                {
                    var loadResult = await LoadAvatarAsync(id);
                    if (!loadResult.IsError && loadResult.Result != null)
                    {
                        loadResult.Result.DeletedDate = DateTime.UtcNow;
                        await SaveAvatarAsync(loadResult.Result);
                        result.Result = true;
                        result.Message = "Avatar soft-deleted in SOLID pod";
                        return result;
                    }
                }
                var response = await _httpClient.DeleteAsync($"{_podServerUrl}/oasis/avatars/{id}.ttl");
                result.Result = response.IsSuccessStatusCode;
                result.IsError = !result.Result;
                result.Message = result.Result ? "Avatar deleted from SOLID pod" : $"SOLID LDP error: {response.StatusCode}";
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error deleting avatar: {ex.Message}", ex); }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) => DeleteAvatarAsync(id, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!_isActivated) { OASISErrorHandling.HandleError(ref result, "Provider not activated"); return result; }
                var response = await _httpClient.DeleteAsync($"{_podServerUrl}/oasis/avatars/by-key/{Uri.EscapeDataString(providerKey)}.ttl");
                result.Result = response.IsSuccessStatusCode;
                result.IsError = !result.Result;
                result.Message = result.Result ? "Avatar deleted from SOLID pod" : $"SOLID LDP error: {response.StatusCode}";
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by provider key: {ex.Message}", ex); }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true) => DeleteAvatarAsync(providerKey, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string username, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!_isActivated) { OASISErrorHandling.HandleError(ref result, "Provider not activated"); return result; }
                var response = await _httpClient.DeleteAsync($"{_podServerUrl}/oasis/avatars/by-username/{Uri.EscapeDataString(username)}.ttl");
                result.Result = response.IsSuccessStatusCode;
                result.IsError = !result.Result;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by username: {ex.Message}", ex); }
            return result;
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string username, bool softDelete = true) => DeleteAvatarByUsernameAsync(username, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string email, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!_isActivated) { OASISErrorHandling.HandleError(ref result, "Provider not activated"); return result; }
                var response = await _httpClient.DeleteAsync($"{_podServerUrl}/oasis/avatars/by-email/{Uri.EscapeDataString(email)}.ttl");
                result.Result = response.IsSuccessStatusCode;
                result.IsError = !result.Result;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by email: {ex.Message}", ex); }
            return result;
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string email, bool softDelete = true) => DeleteAvatarByEmailAsync(email, softDelete).Result;

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => await LoadPrivateRecordAsync<IHolon>(id,version);

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => await LoadPrivateRecordByKeyAsync<IHolon>(providerKey,version);

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => await QueryPrivateHolonsAsync(type,record=>record.ParentHolonId==id,loadChildren,recursive,maxChildDepth,curentChildDepth,loadChildrenFromProvider,version);

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => await QueryPrivateChildrenByKeyAsync(providerKey,type,loadChildren,recursive,maxChildDepth,curentChildDepth,loadChildrenFromProvider,version);

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => await QueryPrivateMetadataAsync(metaKey,metaValue,type,loadChildren,recursive,maxChildDepth,curentChildDepth,loadChildrenFromProvider,version);

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => await QueryPrivateMetadataAsync(metaKeyValuePairs,metaKeyValuePairMatchMode,type,loadChildren,recursive,maxChildDepth,curentChildDepth,loadChildrenFromProvider,version);

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => await QueryPrivateHolonsAsync(type,null,loadChildren,recursive,maxChildDepth,curentChildDepth,loadChildrenFromProvider,version);

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => await SavePrivateRecordAsync(holon);

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var saved = new List<IHolon>();
            try
            {
                if (!_isActivated) { OASISErrorHandling.HandleError(ref result, "Provider not activated"); return result; }
                if (holons == null || !holons.Any()) { OASISErrorHandling.HandleError(ref result, "Holons cannot be null or empty"); return result; }
                foreach (var holon in holons)
                {
                    var saveResult = await SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
                    if (!saveResult.IsError && saveResult.Result != null)
                        saved.Add(saveResult.Result);
                    else if (!continueOnError)
                    {
                        OASISErrorHandling.HandleError(ref result, saveResult.Message);
                        return result;
                    }
                }
                result.Result = saved;
                result.Message = $"Saved {saved.Count} holons to SOLID pod";
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error saving holons: {ex.Message}", ex); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (!_isActivated) { OASISErrorHandling.HandleError(ref result, "Provider not activated"); return result; }
                var response = await _httpClient.DeleteAsync($"{_podServerUrl}/oasis/holons/{id}.ttl");
                result.IsError = !response.IsSuccessStatusCode;
                result.Message = response.IsSuccessStatusCode ? "Holon deleted from SOLID pod" : $"SOLID LDP error: {response.StatusCode}";
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error deleting holon: {ex.Message}", ex); }
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id) => DeleteHolonAsync(id).Result;

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (!_isActivated) { OASISErrorHandling.HandleError(ref result, "Provider not activated"); return result; }
                var response = await _httpClient.DeleteAsync($"{_podServerUrl}/oasis/holons/by-key/{Uri.EscapeDataString(providerKey)}.ttl");
                result.IsError = !response.IsSuccessStatusCode;
                result.Message = response.IsSuccessStatusCode ? "Holon deleted from SOLID pod" : $"SOLID LDP error: {response.StatusCode}";
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error deleting holon by provider key: {ex.Message}", ex); }
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey) => DeleteHolonAsync(providerKey).Result;

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            try
            {
                if (!_isActivated) { OASISErrorHandling.HandleError(ref result, "Provider not activated"); return result; }
                if (searchParams == null) { OASISErrorHandling.HandleError(ref result, "SearchParams cannot be null"); return result; }
                var body = JsonSerializer.Serialize(new { searchParams, version });
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_podServerUrl}/oasis/search", content);
                if (response.IsSuccessStatusCode)
                {
                    result.Result = ParseSearchResults(await response.Content.ReadAsStringAsync());
                    result.IsError = result.Result == null;
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"SOLID LDP error: {response.StatusCode}");
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error searching SOLID pod: {ex.Message}", ex); }
            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
            => SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!_isActivated) { OASISErrorHandling.HandleError(ref result, "Provider not activated"); return result; }
                if (holons == null || !holons.Any()) { OASISErrorHandling.HandleError(ref result, "Holons cannot be null or empty"); return result; }
                var turtle = ConvertHolonsToRDF(holons);
                var content = new StringContent(turtle, Encoding.UTF8, "text/turtle");
                var response = await _httpClient.PostAsync($"{_podServerUrl}/oasis/import", content);
                result.Result = response.IsSuccessStatusCode;
                result.IsError = !result.Result;
                result.Message = result.Result ? $"Imported {holons.Count()} holons to SOLID pod" : $"SOLID LDP error: {response.StatusCode}";
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error importing holons: {ex.Message}", ex); }
            return result;
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons) => ImportAsync(holons).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated) { OASISErrorHandling.HandleError(ref result, "Provider not activated"); return result; }
                var response = await _httpClient.GetAsync($"{_podServerUrl}/oasis/export/avatar/{avatarId}?version={version}");
                if (response.IsSuccessStatusCode)
                {
                    result.Result = ParseRDFToHolons(await response.Content.ReadAsStringAsync());
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"SOLID LDP error: {response.StatusCode}");
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data: {ex.Message}", ex); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0) => ExportAllDataForAvatarByIdAsync(avatarId, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated) { OASISErrorHandling.HandleError(ref result, "Provider not activated"); return result; }
                var response = await _httpClient.GetAsync($"{_podServerUrl}/oasis/export/avatar/username/{Uri.EscapeDataString(avatarUsername)}?version={version}");
                if (response.IsSuccessStatusCode)
                {
                    result.Result = ParseRDFToHolons(await response.Content.ReadAsStringAsync());
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"SOLID LDP error: {response.StatusCode}");
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data by username: {ex.Message}", ex); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0) => ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated) { OASISErrorHandling.HandleError(ref result, "Provider not activated"); return result; }
                var response = await _httpClient.GetAsync($"{_podServerUrl}/oasis/export/avatar/email/{Uri.EscapeDataString(avatarEmailAddress)}?version={version}");
                if (response.IsSuccessStatusCode)
                {
                    result.Result = ParseRDFToHolons(await response.Content.ReadAsStringAsync());
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"SOLID LDP error: {response.StatusCode}");
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data by email: {ex.Message}", ex); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0) => ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated) { OASISErrorHandling.HandleError(ref result, "Provider not activated"); return result; }
                var response = await _httpClient.GetAsync($"{_podServerUrl}/oasis/export/all?version={version}");
                if (response.IsSuccessStatusCode)
                {
                    result.Result = ParseRDFToHolons(await response.Content.ReadAsStringAsync());
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"SOLID LDP error: {response.StatusCode}");
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error exporting all data: {ex.Message}", ex); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0) => ExportAllAsync(version).Result;

        #endregion

        #region IOASISNETProvider

        OASISResult<IEnumerable<IAvatar>> IOASISNETProvider.GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                if (!_isActivated) { OASISErrorHandling.HandleError(ref result, "Provider not activated"); return result; }
                var avatarsResult = LoadAllAvatars();
                if (avatarsResult.IsError || avatarsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatars: {avatarsResult.Message}");
                    return result;
                }
                double centerLat = geoLat / 1e6d;
                double centerLng = geoLong / 1e6d;
                var nearby = new List<IAvatar>();
                foreach (var avatar in avatarsResult.Result)
                {
                    if (avatar.MetaData != null
                        && avatar.MetaData.TryGetValue("Latitude", out var latObj)
                        && avatar.MetaData.TryGetValue("Longitude", out var lngObj)
                        && double.TryParse(latObj?.ToString(), out var lat)
                        && double.TryParse(lngObj?.ToString(), out var lng)
                        && HaversineDistanceMeters(centerLat, centerLng, lat, lng) <= radiusInMeters)
                    {
                        nearby.Add(avatar);
                    }
                }
                result.Result = nearby;
                result.Message = $"Found {nearby.Count} avatars within {radiusInMeters}m";
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error getting avatars near me: {ex.Message}", ex); }
            return result;
        }

        OASISResult<IEnumerable<IHolon>> IOASISNETProvider.GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType type)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated) { OASISErrorHandling.HandleError(ref result, "Provider not activated"); return result; }
                var holonsResult = LoadAllHolons(type);
                if (holonsResult.IsError || holonsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading holons: {holonsResult.Message}");
                    return result;
                }
                double centerLat = geoLat / 1e6d;
                double centerLng = geoLong / 1e6d;
                var nearby = new List<IHolon>();
                foreach (var holon in holonsResult.Result)
                {
                    if (holon.MetaData != null
                        && holon.MetaData.TryGetValue("Latitude", out var latObj)
                        && holon.MetaData.TryGetValue("Longitude", out var lngObj)
                        && double.TryParse(latObj?.ToString(), out var lat)
                        && double.TryParse(lngObj?.ToString(), out var lng)
                        && HaversineDistanceMeters(centerLat, centerLng, lat, lng) <= radiusInMeters)
                    {
                        nearby.Add(holon);
                    }
                }
                result.Result = nearby;
                result.Message = $"Found {nearby.Count} holons within {radiusInMeters}m";
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error getting holons near me: {ex.Message}", ex); }
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

                string folder = System.IO.Path.Combine(outputFolder, "SOLID");
                if (!System.IO.Directory.Exists(folder))
                    System.IO.Directory.CreateDirectory(folder);

                if (!string.IsNullOrEmpty(nativeSource))
                {
                    System.IO.File.WriteAllText(System.IO.Path.Combine(folder, "app.ttl"), nativeSource);
                    return true;
                }

                if (celestialBody == null)
                    return true;

                var sb = new StringBuilder();
                sb.AppendLine("@prefix rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#> .");
                sb.AppendLine("@prefix rdfs: <http://www.w3.org/2000/01/rdf-schema#> .");
                sb.AppendLine("@prefix schema: <https://schema.org/> .");
                sb.AppendLine("@prefix oasis: <https://oasisomniverse.one/ns/> .");
                sb.AppendLine();
                sb.AppendLine($"<{_podServerUrl}/oasis/apps/{celestialBody.Id}>");
                sb.AppendLine("    rdf:type schema:SoftwareApplication ;");
                sb.AppendLine($"    rdfs:label \"{EscapeTtlString(celestialBody.Name ?? "OAPP")}\" ;");
                if (!string.IsNullOrWhiteSpace(celestialBody.Description))
                    sb.AppendLine($"    schema:description \"{EscapeTtlString(celestialBody.Description)}\" ;");
                sb.AppendLine($"    oasis:id \"{celestialBody.Id}\" ;");

                var zomes = celestialBody.CelestialBodyCore?.Zomes;
                if (zomes != null)
                {
                    foreach (var zome in zomes)
                    {
                        if (zome?.Children == null) continue;
                        foreach (var holon in zome.Children)
                        {
                            if (holon == null || string.IsNullOrWhiteSpace(holon.Name)) continue;
                            sb.AppendLine($"    oasis:hasComponent <{_podServerUrl}/oasis/apps/{celestialBody.Id}/components/{holon.Id}> ;");
                        }
                    }
                }

                sb.AppendLine("    .");
                sb.AppendLine();

                if (zomes != null)
                {
                    foreach (var zome in zomes)
                    {
                        if (zome?.Children == null) continue;
                        foreach (var holon in zome.Children)
                        {
                            if (holon == null || string.IsNullOrWhiteSpace(holon.Name)) continue;
                            sb.AppendLine($"<{_podServerUrl}/oasis/apps/{celestialBody.Id}/components/{holon.Id}>");
                            sb.AppendLine("    rdf:type oasis:Component ;");
                            sb.AppendLine($"    rdfs:label \"{EscapeTtlString(holon.Name)}\" ;");
                            sb.AppendLine($"    oasis:id \"{holon.Id}\" ;");
                            if (!string.IsNullOrWhiteSpace(holon.Description))
                                sb.AppendLine($"    schema:description \"{EscapeTtlString(holon.Description)}\" ;");
                            sb.AppendLine("    .");
                        }
                    }
                }

                System.IO.File.WriteAllText(System.IO.Path.Combine(folder, "app.ttl"), sb.ToString());
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region RDF helpers

        private IAvatar ParseRDFToAvatar(string turtle)
            => AvatarFromRdf(SolidRdfRecord.Read(turtle).Single());

        private static IAvatar AvatarFromRdf(SolidRdfRecord record)=>new Avatar{
            Id=record.Id,Username=record.Get("http://xmlns.com/foaf/0.1/nick")??record.Get("https://schema.org/name"),
            Email=record.Get("https://schema.org/email"),
            FirstName=record.Get("http://xmlns.com/foaf/0.1/firstName")??record.Get("https://schema.org/givenName"),
            LastName=record.Get("http://xmlns.com/foaf/0.1/familyName")??record.Get("https://schema.org/familyName")};

        private IEnumerable<IAvatar> ParseRDFToAvatars(string turtle)
            => SolidRdfRecord.Read(turtle).Select(AvatarFromRdf).ToArray();

        private IAvatarDetail ParseRDFToAvatarDetail(string turtle)
            => new AvatarDetail{Id=SolidRdfRecord.Read(turtle).Single().Id};

        private IEnumerable<IAvatarDetail> ParseRDFToAvatarDetails(string turtle)
            => SolidRdfRecord.Read(turtle).Select(record=>(IAvatarDetail)new AvatarDetail{Id=record.Id}).ToArray();

        private IHolon ParseRDFToHolon(string turtle)
            => HolonFromRdf(SolidRdfRecord.Read(turtle).Single());

        private static IHolon HolonFromRdf(SolidRdfRecord record)=>new Holon{Id=record.Id,
            Name=record.Get("http://www.w3.org/2000/01/rdf-schema#label")??record.Get("https://schema.org/name"),
            Description=record.Get("https://schema.org/description")};

        private IEnumerable<IHolon> ParseRDFToHolons(string turtle)
            => SolidRdfRecord.Read(turtle).Select(HolonFromRdf).ToArray();

        private ISearchResults ParseSearchResults(string response)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(response)) return null;
                return new SearchResults { SearchResultHolons = ParseRDFToHolons(response).Cast<IHolon>().ToList() };
            }
            catch { return null; }
        }

        private string ConvertAvatarToRDF(IAvatar avatar)
        {
            var sb = new StringBuilder();
            sb.AppendLine("@prefix rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#> .");
            sb.AppendLine("@prefix rdfs: <http://www.w3.org/2000/01/rdf-schema#> .");
            sb.AppendLine("@prefix foaf: <http://xmlns.com/foaf/0.1/> .");
            sb.AppendLine("@prefix schema: <https://schema.org/> .");
            sb.AppendLine("@prefix oasis: <https://oasisomniverse.one/ns/> .");
            sb.AppendLine();
            sb.AppendLine($"<{_podServerUrl}/oasis/avatars/{avatar.Id}>");
            sb.AppendLine("    rdf:type foaf:Person ;");
            sb.AppendLine($"    oasis:id \"{avatar.Id}\" ;");
            if (!string.IsNullOrEmpty(avatar.Username)) sb.AppendLine($"    foaf:nick \"{EscapeTtlString(avatar.Username)}\" ;");
            if (!string.IsNullOrEmpty(avatar.Email)) sb.AppendLine($"    schema:email \"{EscapeTtlString(avatar.Email)}\" ;");
            if (!string.IsNullOrEmpty(avatar.FirstName)) sb.AppendLine($"    foaf:firstName \"{EscapeTtlString(avatar.FirstName)}\" ;");
            if (!string.IsNullOrEmpty(avatar.LastName)) sb.AppendLine($"    foaf:familyName \"{EscapeTtlString(avatar.LastName)}\" ;");
            sb.AppendLine("    .");
            return sb.ToString();
        }

        private string ConvertAvatarDetailToRDF(IAvatarDetail detail)
        {
            var sb = new StringBuilder();
            sb.AppendLine("@prefix rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#> .");
            sb.AppendLine("@prefix oasis: <https://oasisomniverse.one/ns/> .");
            sb.AppendLine();
            sb.AppendLine($"<{_podServerUrl}/oasis/avatar-details/{detail.Id}>");
            sb.AppendLine("    rdf:type oasis:AvatarDetail ;");
            sb.AppendLine($"    oasis:id \"{detail.Id}\" ;");
            sb.AppendLine("    .");
            return sb.ToString();
        }

        private string ConvertHolonToRDF(IHolon holon)
        {
            var sb = new StringBuilder();
            sb.AppendLine("@prefix rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#> .");
            sb.AppendLine("@prefix rdfs: <http://www.w3.org/2000/01/rdf-schema#> .");
            sb.AppendLine("@prefix schema: <https://schema.org/> .");
            sb.AppendLine("@prefix oasis: <https://oasisomniverse.one/ns/> .");
            sb.AppendLine();
            sb.AppendLine($"<{_podServerUrl}/oasis/holons/{holon.Id}>");
            sb.AppendLine("    rdf:type oasis:Holon ;");
            sb.AppendLine($"    oasis:id \"{holon.Id}\" ;");
            if (!string.IsNullOrEmpty(holon.Name)) sb.AppendLine($"    rdfs:label \"{EscapeTtlString(holon.Name)}\" ;");
            if (!string.IsNullOrEmpty(holon.Description)) sb.AppendLine($"    schema:description \"{EscapeTtlString(holon.Description)}\" ;");
            sb.AppendLine("    .");
            return sb.ToString();
        }

        private string ConvertHolonsToRDF(IEnumerable<IHolon> holons)
        {
            var sb = new StringBuilder();
            sb.AppendLine("@prefix rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#> .");
            sb.AppendLine("@prefix rdfs: <http://www.w3.org/2000/01/rdf-schema#> .");
            sb.AppendLine("@prefix schema: <https://schema.org/> .");
            sb.AppendLine("@prefix oasis: <https://oasisomniverse.one/ns/> .");
            sb.AppendLine();
            foreach (var holon in holons)
            {
                sb.AppendLine($"<{_podServerUrl}/oasis/holons/{holon.Id}>");
                sb.AppendLine("    rdf:type oasis:Holon ;");
                sb.AppendLine($"    oasis:id \"{holon.Id}\" ;");
                if (!string.IsNullOrEmpty(holon.Name)) sb.AppendLine($"    rdfs:label \"{EscapeTtlString(holon.Name)}\" ;");
                if (!string.IsNullOrEmpty(holon.Description)) sb.AppendLine($"    schema:description \"{EscapeTtlString(holon.Description)}\" ;");
                sb.AppendLine("    .");
                sb.AppendLine();
            }
            return sb.ToString();
        }

        private static string EscapeTtlString(string value)
            => value?.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r") ?? "";

        private static double HaversineDistanceMeters(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371000;
            double dLat = ToRad(lat2 - lat1);
            double dLon = ToRad(lon2 - lon1);
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                     + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2))
                     * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }

        private static double ToRad(double deg) => deg * Math.PI / 180.0;

        #endregion

        public void Dispose()
        {
            if (!_disposed)
            {
                _httpClient?.Dispose();
                _privateRecords?.Dispose();
                _disposed = true;
            }
        }
    }
}
