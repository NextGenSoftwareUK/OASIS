using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.PrivyOASIS
{
    /// <summary>
    /// OASIS provider for Privy — embedded wallet SDK with email/social login -> Web3 wallet.
    /// Uses the Privy REST API v1 (https://auth.privy.io/api/v1/).
    /// </summary>
    public class PrivyOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider
    {
        private readonly HttpClient _http;
        private readonly string _appId;
        private bool _isActivated;

        public PrivyOASIS(string appId, string appSecret)
        {
            _appId = appId;
            var creds = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{appId}:{appSecret}"));
            _http = new HttpClient { BaseAddress = new Uri("https://auth.privy.io") };
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", creds);
            _http.DefaultRequestHeaders.Add("privy-app-id", appId);
            _http.DefaultRequestHeaders.Add("Accept", "application/json");

            ProviderName = "PrivyOASIS";
            ProviderDescription = "Privy embedded wallet provider — email/social login to Web3 wallet";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.PrivyOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network));
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                var response = await _http.GetAsync($"/api/v1/apps/{_appId}");
                if (response.IsSuccessStatusCode) { _isActivated = true; result.Result = true; result.Message = "PrivyOASIS activated."; }
                else OASISErrorHandling.HandleError(ref result, $"PrivyOASIS: API check failed ({response.StatusCode}).");
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;
        public override async Task<OASISResult<bool>> DeActivateProviderAsync() { _isActivated = false; return await Task.FromResult(new OASISResult<bool>(true)); }
        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // ─── Avatars ──────────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var response = await _http.GetAsync($"/api/v1/users/{providerKey}");
                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadFromJsonAsync<JsonElement>();
                    var avatar = new Avatar();
                    avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.PrivyOASIS] = providerKey;
                    avatar.Username = body.TryGetProperty("id", out var id) ? id.GetString() ?? providerKey : providerKey;
                    // Extract email from linked_accounts if present
                    if (body.TryGetProperty("linked_accounts", out var accounts))
                        foreach (var acc in accounts.EnumerateArray())
                            if (acc.TryGetProperty("type", out var t) && t.GetString() == "email" && acc.TryGetProperty("address", out var addr))
                            { avatar.Email = addr.GetString(); break; }
                    if (body.TryGetProperty("created_at", out var ca)) avatar.MetaData["privy_created_at"] = ca.GetInt64().ToString();
                    result.Result = avatar;
                }
                else OASISErrorHandling.HandleError(ref result, $"PrivyOASIS: User lookup for '{providerKey}' failed ({response.StatusCode}).");
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0) => LoadAvatarByProviderKeyAsync(providerKey, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
            => await LoadAvatarByProviderKeyAsync(avatarUsername, version);
        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0) => LoadAvatarByUsernameAsync(avatarUsername, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        { var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, "PrivyOASIS: Use LoadAvatarByProviderKey(privyUserId)."); return await Task.FromResult(r); }
        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) => LoadAvatarAsync(id, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        { var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, "PrivyOASIS: Use LoadAvatarByProviderKey(privyUserId)."); return await Task.FromResult(r); }
        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0) => LoadAvatarByEmailAsync(avatarEmail, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        { var r = new OASISResult<IEnumerable<IAvatar>>(); OASISErrorHandling.HandleError(ref r, "PrivyOASIS: LoadAllAvatars not supported."); return await Task.FromResult(r); }
        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) => LoadAllAvatarsAsync(version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "PrivyOASIS: Not supported."); return await Task.FromResult(r); }
        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => LoadAvatarDetailAsync(id, version).Result;
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "PrivyOASIS: Not supported."); return await Task.FromResult(r); }
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0) => LoadAvatarDetailByEmailAsync(avatarEmail, version).Result;
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "PrivyOASIS: Not supported."); return await Task.FromResult(r); }
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0) => LoadAvatarDetailByUsernameAsync(avatarUsername, version).Result;
        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        { var r = new OASISResult<IEnumerable<IAvatarDetail>>(); OASISErrorHandling.HandleError(ref r, "PrivyOASIS: Not supported."); return await Task.FromResult(r); }
        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0) => LoadAllAvatarDetailsAsync(version).Result;

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        { var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, "PrivyOASIS: Avatar creation is managed by Privy's auth flows."); return await Task.FromResult(r); }
        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) => SaveAvatarAsync(avatar).Result;
        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatar)
        { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "PrivyOASIS: Not supported."); return await Task.FromResult(r); }
        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatar) => SaveAvatarDetailAsync(avatar).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "PrivyOASIS: Not supported."); return await Task.FromResult(r); }
        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) => DeleteAvatarAsync(id, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "PrivyOASIS: Not supported."); return await Task.FromResult(r); }
        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true) => DeleteAvatarAsync(providerKey, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "PrivyOASIS: Not supported."); return await Task.FromResult(r); }
        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true) => DeleteAvatarByEmailAsync(avatarEmail, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "PrivyOASIS: Not supported."); return await Task.FromResult(r); }
        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true) => DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;

        // ─── Holons (app-scoped storage) ──────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var payload = new { id = holon.Id.ToString(), data = JsonSerializer.Serialize(holon) };
                var response = await _http.PostAsJsonAsync($"/api/v1/apps/{_appId}/storage", payload);
                if (response.IsSuccessStatusCode)
                {
                    holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.PrivyOASIS] = holon.Id.ToString();
                    result.Result = holon;
                }
                else OASISErrorHandling.HandleError(ref result, $"PrivyOASIS: SaveHolon failed ({response.StatusCode}).");
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var saved = new List<IHolon>();
            foreach (var h in holons)
            {
                var r = await SaveHolonAsync(h, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
                if (r.Result != null) saved.Add(r.Result);
            }
            result.Result = saved;
            return result;
        }
        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var response = await _http.GetAsync($"/api/v1/apps/{_appId}/storage/{providerKey}");
                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadFromJsonAsync<JsonElement>();
                    var holon = new Holon();
                    holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.PrivyOASIS] = providerKey;
                    if (body.TryGetProperty("data", out var d))
                    {
                        var inner = d.GetString();
                        if (inner != null) holon.MetaData["privy_data"] = inner;
                    }
                    result.Result = holon;
                }
                else OASISErrorHandling.HandleError(ref result, $"PrivyOASIS: LoadHolon '{providerKey}' failed ({response.StatusCode}).");
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => await LoadHolonAsync(id.ToString(), loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);
        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var response = await _http.GetAsync($"/api/v1/apps/{_appId}/storage");
                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadFromJsonAsync<JsonElement>();
                    var holons = new List<IHolon>();
                    var arr = body.ValueKind == JsonValueKind.Array ? body : (body.TryGetProperty("data", out var d) ? d : default);
                    if (arr.ValueKind == JsonValueKind.Array)
                        foreach (var item in arr.EnumerateArray())
                        {
                            var holon = new Holon();
                            if (item.TryGetProperty("id", out var id)) holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.PrivyOASIS] = id.GetString() ?? "";
                            holons.Add(holon);
                        }
                    result.Result = holons;
                }
                else OASISErrorHandling.HandleError(ref result, $"PrivyOASIS: LoadAllHolons failed ({response.StatusCode}).");
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "PrivyOASIS: LoadHolonsForParent not supported."); return await Task.FromResult(r); }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "PrivyOASIS: LoadHolonsForParent not supported."); return await Task.FromResult(r); }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var response = await _http.DeleteAsync($"/api/v1/apps/{_appId}/storage/{id}");
                if (response.IsSuccessStatusCode) { result.Result = new Holon { Id = id }; result.Message = "Deleted."; }
                else OASISErrorHandling.HandleError(ref result, $"PrivyOASIS: DeleteHolon {id} failed ({response.StatusCode}).");
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<IHolon> DeleteHolon(Guid id) => DeleteHolonAsync(id).Result;

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var response = await _http.DeleteAsync($"/api/v1/apps/{_appId}/storage/{providerKey}");
                if (response.IsSuccessStatusCode)
                {
                    var h = new Holon(); h.ProviderUniqueStorageKey[Core.Enums.ProviderType.PrivyOASIS] = providerKey;
                    result.Result = h; result.Message = "Deleted.";
                }
                else OASISErrorHandling.HandleError(ref result, $"PrivyOASIS: DeleteHolon '{providerKey}' failed ({response.StatusCode}).");
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<IHolon> DeleteHolon(string providerKey) => DeleteHolonAsync(providerKey).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "PrivyOASIS: MetaData search not supported."); return await Task.FromResult(r); }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "PrivyOASIS: MetaData search not supported."); return await Task.FromResult(r); }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            try
            {
                var query = searchParams.SearchGroups?.OfType<SearchTextGroup>().FirstOrDefault()?.SearchQuery ?? "";
                var allResult = await LoadAllHolonsAsync();
                if (allResult.IsError) { OASISErrorHandling.HandleError(ref result, allResult.Message); return result; }
                var sr = new SearchResults();
                sr.SearchResultHolons = (allResult.Result ?? Enumerable.Empty<IHolon>())
                    .Where(h => h.Name != null && h.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                result.Result = sr;
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
            => SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;

        // ─── Import / Export ──────────────────────────────────────────────────────
        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "PrivyOASIS: Import not supported."); return await Task.FromResult(r); }
        public override OASISResult<bool> Import(IEnumerable<IHolon> holons) => ImportAsync(holons).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "PrivyOASIS: Export not supported."); return await Task.FromResult(r); }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0) => ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "PrivyOASIS: Export not supported."); return await Task.FromResult(r); }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0) => ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "PrivyOASIS: Export not supported."); return await Task.FromResult(r); }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0) => ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "PrivyOASIS: Export not supported."); return await Task.FromResult(r); }
        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0) => ExportAllAsync(version).Result;

        // ─── IOASISNETProvider ────────────────────────────────────────────────────
        public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        { var r = new OASISResult<IEnumerable<IAvatar>>(); OASISErrorHandling.HandleError(ref r, "PrivyOASIS: Geolocation not supported."); return r; }
        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
        { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "PrivyOASIS: Geolocation not supported."); return r; }
    }
}
