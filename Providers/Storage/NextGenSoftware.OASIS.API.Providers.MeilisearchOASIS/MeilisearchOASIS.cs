using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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

namespace NextGenSoftware.OASIS.API.Providers.MeilisearchOASIS
{
    /// <summary>
    /// OASIS provider for Meilisearch — a fast, typo-tolerant, RESTful search engine.
    ///
    /// Indexes: oasis_avatars, oasis_avatar_details, oasis_holons
    /// All documents stored with full JSON body; primary key = "id" (string form of GUID).
    ///
    /// Constructor parameters:
    ///   host   — Meilisearch host URL, e.g. "http://localhost:7700"
    ///   apiKey — optional master/API key
    /// </summary>
    public class MeilisearchOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISDBStorageProvider
    {
        private readonly HttpClient _http;
        private readonly string _host;

        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private const string IdxAvatars = "oasis_avatars";
        private const string IdxDetails = "oasis_avatar_details";
        private const string IdxHolons = "oasis_holons";

        public MeilisearchOASIS(string host, string? apiKey = null)
        {
            _host = host.TrimEnd('/');
            _http = new HttpClient();
            if (!string.IsNullOrEmpty(apiKey))
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            ProviderName = "MeilisearchOASIS";
            ProviderDescription = "Meilisearch provider (fast typo-tolerant full-text search via REST API)";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.MeilisearchOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageLocalAndNetwork);
        }

        private static string Ser(object obj) => JsonSerializer.Serialize(obj, _jsonOpts);
        private StringContent Json(object obj) => new StringContent(Ser(obj), Encoding.UTF8, "application/json");

        private async Task EnsureIndexAsync(string index)
        {
            var body = new { uid = index, primaryKey = "id" };
            var resp = await _http.PostAsync($"{_host}/indexes", Json(body));
            // 201 = created, 409 = already exists — both acceptable
        }

        private async Task<T?> GetDocAsync<T>(string index, string id)
        {
            var resp = await _http.GetAsync($"{_host}/indexes/{index}/documents/{id}");
            if (!resp.IsSuccessStatusCode) return default;
            var json = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json, _jsonOpts);
        }

        private async Task UpsertDocAsync(string index, object doc)
        {
            var content = new StringContent($"[{Ser(doc)}]", Encoding.UTF8, "application/json");
            await _http.PutAsync($"{_host}/indexes/{index}/documents", content);
        }

        private async Task<List<T>> GetAllDocsAsync<T>(string index)
        {
            var resp = await _http.GetAsync($"{_host}/indexes/{index}/documents?limit=1000");
            if (!resp.IsSuccessStatusCode) return new List<T>();
            var json = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("results", out var results)) return new List<T>();
            return JsonSerializer.Deserialize<List<T>>(results.GetRawText(), _jsonOpts) ?? new List<T>();
        }

        private async Task DeleteDocAsync(string index, string id)
        {
            await _http.DeleteAsync($"{_host}/indexes/{index}/documents/{id}");
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                await EnsureIndexAsync(IdxAvatars);
                await EnsureIndexAsync(IdxDetails);
                await EnsureIndexAsync(IdxHolons);
                IsProviderActivated = true; result.Result = true; result.IsError = false; result.Message = "MeilisearchOASIS activated.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"MeilisearchOASIS: Error activating — {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;
        public override async Task<OASISResult<bool>> DeActivateProviderAsync() { IsProviderActivated = false; return await Task.FromResult(new OASISResult<bool> { Result = true, IsError = false, Message = "MeilisearchOASIS deactivated." }); }
        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // ─── Avatar ───────────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (avatar.Id == Guid.Empty) avatar.Id = Guid.NewGuid();
                if (avatar.ProviderUniqueStorageKey == null) avatar.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.MeilisearchOASIS] = avatar.Id.ToString();
                var doc = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(Ser(avatar), _jsonOpts)!;
                doc["id"] = JsonDocument.Parse($"\"{avatar.Id}\"").RootElement;
                await UpsertDocAsync(IdxAvatars, doc);
                result.Result = avatar; result.IsError = false; result.Message = $"MeilisearchOASIS: Avatar '{avatar.Username}' saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"MeilisearchOASIS: Error saving avatar: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) => SaveAvatarAsync(avatar).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var a = await GetDocAsync<Avatar>(IdxAvatars, id.ToString());
                if (a == null || a.IsDeleted) { OASISErrorHandling.HandleError(ref result, $"MeilisearchOASIS: No avatar for ID '{id}'."); return result; }
                result.Result = a; result.IsError = false; result.Message = "MeilisearchOASIS: Avatar loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"MeilisearchOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) => LoadAvatarAsync(id, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var resp = await _http.GetAsync($"{_host}/indexes/{IdxAvatars}/search?q={Uri.EscapeDataString(username)}&filter=username%20%3D%20%22{Uri.EscapeDataString(username)}%22&limit=1");
                if (!resp.IsSuccessStatusCode) { OASISErrorHandling.HandleError(ref result, $"MeilisearchOASIS: Search failed."); return result; }
                var json = await resp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("hits", out var hits) || hits.GetArrayLength() == 0) { OASISErrorHandling.HandleError(ref result, $"MeilisearchOASIS: No avatar for username '{username}'."); return result; }
                var a = JsonSerializer.Deserialize<Avatar>(hits[0].GetRawText(), _jsonOpts);
                if (a == null || a.IsDeleted) { OASISErrorHandling.HandleError(ref result, $"MeilisearchOASIS: No avatar for username '{username}'."); return result; }
                result.Result = a; result.IsError = false; result.Message = "MeilisearchOASIS: Avatar loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"MeilisearchOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string username, int version = 0) => LoadAvatarByUsernameAsync(username, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var resp = await _http.GetAsync($"{_host}/indexes/{IdxAvatars}/search?q={Uri.EscapeDataString(avatarEmail)}&filter=email%20%3D%20%22{Uri.EscapeDataString(avatarEmail)}%22&limit=1");
                if (!resp.IsSuccessStatusCode) { OASISErrorHandling.HandleError(ref result, $"MeilisearchOASIS: Search failed."); return result; }
                var json = await resp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("hits", out var hits) || hits.GetArrayLength() == 0) { OASISErrorHandling.HandleError(ref result, $"MeilisearchOASIS: No avatar for email '{avatarEmail}'."); return result; }
                var a = JsonSerializer.Deserialize<Avatar>(hits[0].GetRawText(), _jsonOpts);
                if (a == null || a.IsDeleted) { OASISErrorHandling.HandleError(ref result, $"MeilisearchOASIS: No avatar for email '{avatarEmail}'."); return result; }
                result.Result = a; result.IsError = false; result.Message = "MeilisearchOASIS: Avatar loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"MeilisearchOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0) => LoadAvatarByEmailAsync(avatarEmail, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            if (Guid.TryParse(providerKey, out Guid id)) return await LoadAvatarAsync(id, version);
            var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, $"MeilisearchOASIS: Invalid GUID '{providerKey}'."); return r;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0) => LoadAvatarByProviderKeyAsync(providerKey, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                var all = await GetAllDocsAsync<Avatar>(IdxAvatars);
                var list = new List<IAvatar>();
                foreach (var a in all) { if (!a.IsDeleted) list.Add(a); }
                result.Result = list; result.IsError = false; result.Message = $"MeilisearchOASIS: Loaded {list.Count} avatar(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"MeilisearchOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) => LoadAllAvatarsAsync(version).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (softDelete)
                {
                    var loadResult = await LoadAvatarAsync(id);
                    if (!loadResult.IsError && loadResult.Result != null)
                    {
                        var avatar = (Avatar)loadResult.Result; avatar.DeletedDate = DateTime.UtcNow;
                        await SaveAvatarAsync(avatar);
                    }
                }
                else { await DeleteDocAsync(IdxAvatars, id.ToString()); }
                result.Result = true; result.IsError = false; result.Message = $"MeilisearchOASIS: Avatar '{id}' deleted.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"MeilisearchOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) => DeleteAvatarAsync(id, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string u, bool softDelete = true) { var a = await LoadAvatarByUsernameAsync(u); if (a.IsError || a.Result == null) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, $"MeilisearchOASIS: Avatar '{u}' not found."); return r; } return await DeleteAvatarAsync(a.Result.Id, softDelete); }
        public override OASISResult<bool> DeleteAvatarByUsername(string u, bool softDelete = true) => DeleteAvatarByUsernameAsync(u, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string e, bool softDelete = true) { var a = await LoadAvatarByEmailAsync(e); if (a.IsError || a.Result == null) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, $"MeilisearchOASIS: Avatar not found."); return r; } return await DeleteAvatarAsync(a.Result.Id, softDelete); }
        public override OASISResult<bool> DeleteAvatarByEmail(string e, bool softDelete = true) => DeleteAvatarByEmailAsync(e, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string pk, bool softDelete = true) { if (Guid.TryParse(pk, out Guid id)) return await DeleteAvatarAsync(id, softDelete); return await DeleteAvatarByUsernameAsync(pk, softDelete); }
        public override OASISResult<bool> DeleteAvatar(string pk, bool softDelete = true) => DeleteAvatarAsync(pk, softDelete).Result;

        // ─── AvatarDetail ─────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail d)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                if (d.Id == Guid.Empty) d.Id = Guid.NewGuid();
                var doc = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(Ser(d), _jsonOpts)!;
                doc["id"] = JsonDocument.Parse($"\"{d.Id}\"").RootElement;
                await UpsertDocAsync(IdxDetails, doc);
                result.Result = d; result.IsError = false; result.Message = "MeilisearchOASIS: AvatarDetail saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"MeilisearchOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail d) => SaveAvatarDetailAsync(d).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try { var d = await GetDocAsync<AvatarDetail>(IdxDetails, id.ToString()); if (d == null) { OASISErrorHandling.HandleError(ref result, $"MeilisearchOASIS: No detail for ID '{id}'."); return result; } result.Result = d; result.IsError = false; result.Message = "MeilisearchOASIS: AvatarDetail loaded."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"MeilisearchOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => LoadAvatarDetailAsync(id, version).Result;
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string u, int version = 0) { var result = new OASISResult<IAvatarDetail>(); try { var resp = await _http.GetAsync($"{_host}/indexes/{IdxDetails}/search?q={Uri.EscapeDataString(u)}&filter=username%20%3D%20%22{Uri.EscapeDataString(u)}%22&limit=1"); var json = await resp.Content.ReadAsStringAsync(); using var doc = JsonDocument.Parse(json); if (!doc.RootElement.TryGetProperty("hits", out var hits) || hits.GetArrayLength() == 0) { OASISErrorHandling.HandleError(ref result, $"MeilisearchOASIS: No detail for username '{u}'."); return result; } var d = JsonSerializer.Deserialize<AvatarDetail>(hits[0].GetRawText(), _jsonOpts); if (d == null) { OASISErrorHandling.HandleError(ref result, $"MeilisearchOASIS: No detail for username '{u}'."); return result; } result.Result = d; result.IsError = false; result.Message = "MeilisearchOASIS: AvatarDetail loaded."; } catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"MeilisearchOASIS: {ex.Message}"); } return result; }
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string u, int version = 0) => LoadAvatarDetailByUsernameAsync(u, version).Result;
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string e, int version = 0) { var result = new OASISResult<IAvatarDetail>(); try { var resp = await _http.GetAsync($"{_host}/indexes/{IdxDetails}/search?q={Uri.EscapeDataString(e)}&filter=email%20%3D%20%22{Uri.EscapeDataString(e)}%22&limit=1"); var json = await resp.Content.ReadAsStringAsync(); using var doc = JsonDocument.Parse(json); if (!doc.RootElement.TryGetProperty("hits", out var hits) || hits.GetArrayLength() == 0) { OASISErrorHandling.HandleError(ref result, $"MeilisearchOASIS: No detail for email '{e}'."); return result; } var d = JsonSerializer.Deserialize<AvatarDetail>(hits[0].GetRawText(), _jsonOpts); if (d == null) { OASISErrorHandling.HandleError(ref result, $"MeilisearchOASIS: No detail for email '{e}'."); return result; } result.Result = d; result.IsError = false; result.Message = "MeilisearchOASIS: AvatarDetail loaded."; } catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"MeilisearchOASIS: {ex.Message}"); } return result; }
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string e, int version = 0) => LoadAvatarDetailByEmailAsync(e, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            try { var all = await GetAllDocsAsync<AvatarDetail>(IdxDetails); result.Result = new List<IAvatarDetail>(all); result.IsError = false; result.Message = $"MeilisearchOASIS: Loaded {all.Count} detail(s)."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"MeilisearchOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0) => LoadAllAvatarDetailsAsync(version).Result;

        // ─── Holons ───────────────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (holon.Id == Guid.Empty) holon.Id = Guid.NewGuid();
                var doc = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(Ser(holon), _jsonOpts)!;
                doc["id"] = JsonDocument.Parse($"\"{holon.Id}\"").RootElement;
                await UpsertDocAsync(IdxHolons, doc);
                result.Result = holon; result.IsError = false; result.Message = $"MeilisearchOASIS: Holon '{holon.Name}' saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"MeilisearchOASIS: Error saving holon: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, int version = 0) => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>(); var saved = new List<IHolon>();
            foreach (var h in holons) { var r = await SaveHolonAsync(h, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, version); if (!r.IsError && r.Result != null) saved.Add(r.Result); }
            result.Result = saved; result.IsError = false; result.Message = $"MeilisearchOASIS: Saved {saved.Count} holons."; return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, int version = 0) => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, version).Result;

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var h = await GetDocAsync<Holon>(IdxHolons, id.ToString());
                if (h == null || h.IsDeleted) { OASISErrorHandling.HandleError(ref result, $"MeilisearchOASIS: No holon for ID '{id}'."); return result; }
                result.Result = h; result.IsError = false; result.Message = "MeilisearchOASIS: Holon loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"MeilisearchOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IHolon>> LoadHolonByProviderKeyAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) { if (Guid.TryParse(providerKey, out Guid id)) return await LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version); var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, $"MeilisearchOASIS: Invalid GUID '{providerKey}'."); return r; }
        public override OASISResult<IHolon> LoadHolonByProviderKey(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonByProviderKeyAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var all = await GetAllDocsAsync<Holon>(IdxHolons);
                var list = new List<IHolon>(); foreach (var h in all) { if (h.IsDeleted) continue; if (holonType != HolonType.All && h.HolonType != holonType) continue; list.Add(h); }
                result.Result = list; result.IsError = false; result.Message = $"MeilisearchOASIS: Loaded {list.Count} holon(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"MeilisearchOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false) => LoadAllHolonsAsync(holonType, loadChildren, recursive, maxChildDepth, version, continueOnError, loadChildrenFromProvider).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var all = await GetAllDocsAsync<Holon>(IdxHolons);
                var list = new List<IHolon>();
                foreach (var h in all) { if (h.IsDeleted || h.ParentHolonId != id) continue; if (holonType != HolonType.All && h.HolonType != holonType) continue; list.Add(h); }
                result.Result = list; result.IsError = false; result.Message = $"MeilisearchOASIS: Loaded {list.Count} child holon(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"MeilisearchOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false) => LoadHolonsForParentAsync(id, holonType, loadChildren, recursive, maxChildDepth, version, continueOnError, loadChildrenFromProvider).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false) { if (Guid.TryParse(providerKey, out Guid id)) return await LoadHolonsForParentAsync(id, holonType, loadChildren, recursive, maxChildDepth, version, continueOnError, loadChildrenFromProvider); var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, $"MeilisearchOASIS: Invalid GUID '{providerKey}'."); return r; }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false) => LoadHolonsForParentAsync(providerKey, holonType, loadChildren, recursive, maxChildDepth, version, continueOnError, loadChildrenFromProvider).Result;

        public override async Task<OASISResult<bool>> DeleteHolonAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (softDelete) { var load = await LoadHolonAsync(id); if (!load.IsError && load.Result != null) { var h = (Holon)load.Result; h.DeletedDate = DateTime.UtcNow; await SaveHolonAsync(h); } }
                else { await DeleteDocAsync(IdxHolons, id.ToString()); }
                result.Result = true; result.IsError = false; result.Message = $"MeilisearchOASIS: Holon '{id}' deleted.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"MeilisearchOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> DeleteHolon(Guid id, bool softDelete = true) => DeleteHolonAsync(id, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteHolonAsync(string pk, bool softDelete = true) { if (Guid.TryParse(pk, out Guid id)) return await DeleteHolonAsync(id, softDelete); var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, $"MeilisearchOASIS: Invalid GUID '{pk}'."); return r; }
        public override OASISResult<bool> DeleteHolon(string pk, bool softDelete = true) => DeleteHolonAsync(pk, softDelete).Result;

        // ─── Search ───────────────────────────────────────────────────────────────

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>(); var sr = new SearchResults();
            var avatarResult = await LoadAllAvatarsAsync(); if (!avatarResult.IsError && avatarResult.Result != null) sr.Avatars = new List<IAvatar>(avatarResult.Result);
            var holonResult = await LoadAllHolonsAsync(); if (!holonResult.IsError && holonResult.Result != null) sr.Holons = new List<IHolon>(holonResult.Result);
            result.Result = sr; result.IsError = false; return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
        public override Task<OASISResult<IAvatar>> SearchAvatarsAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => throw new NotImplementedException("MeilisearchOASIS: Use SearchAsync.");
        public override OASISResult<IAvatar> SearchAvatars(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => throw new NotImplementedException();
        public override Task<OASISResult<IHolon>> SearchHolonsAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => throw new NotImplementedException("MeilisearchOASIS: Use SearchAsync.");
        public override OASISResult<IHolon> SearchHolons(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => throw new NotImplementedException();

        public override string GetProviderVersion() => "1.0.0";
        public override Task<string> GetProviderVersionAsync() => Task.FromResult("1.0.0");
    }
}
