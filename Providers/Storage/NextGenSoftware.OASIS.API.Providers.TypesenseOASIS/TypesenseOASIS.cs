using System;
using System.Collections.Generic;
using System.Net.Http;
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

namespace NextGenSoftware.OASIS.API.Providers.TypesenseOASIS
{
    /// <summary>
    /// OASIS provider for Typesense — a fast, typo-tolerant search engine with a Typesense REST API.
    ///
    /// Collections: oasis_avatars, oasis_avatar_details, oasis_holons
    /// Upsert via POST /collections/{name}/documents?action=upsert
    /// Primary key field: "id" (string GUID)
    ///
    /// Constructor parameters:
    ///   host   — Typesense host URL, e.g. "http://localhost:8108"
    ///   apiKey — Typesense API key (X-TYPESENSE-API-KEY header)
    /// </summary>
    public class TypesenseOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISDBStorageProvider
    {
        private readonly HttpClient _http;
        private readonly string _host;

        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private const string ColAvatars = "oasis_avatars";
        private const string ColDetails = "oasis_avatar_details";
        private const string ColHolons = "oasis_holons";

        public TypesenseOASIS(string host, string apiKey)
        {
            _host = host.TrimEnd('/');
            _http = new HttpClient();
            _http.DefaultRequestHeaders.Add("X-TYPESENSE-API-KEY", apiKey);
            ProviderName = "TypesenseOASIS";
            ProviderDescription = "Typesense provider (fast fuzzy-search engine via REST API)";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.TypesenseOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageLocalAndNetwork);
        }

        private static string Ser(object obj) => JsonSerializer.Serialize(obj, _jsonOpts);
        private StringContent Json(object obj) => new StringContent(Ser(obj), Encoding.UTF8, "application/json");

        private async Task EnsureCollectionAsync(string name)
        {
            // Check if collection exists
            var resp = await _http.GetAsync($"{_host}/collections/{name}");
            if (resp.IsSuccessStatusCode) return;

            var schema = new
            {
                name,
                fields = new object[]
                {
                    new { name = "id", type = "string" },
                    new { name = ".*", type = "auto" }
                }
            };
            await _http.PostAsync($"{_host}/collections", Json(schema));
        }

        private async Task<T?> GetDocAsync<T>(string collection, string id)
        {
            var resp = await _http.GetAsync($"{_host}/collections/{collection}/documents/{id}");
            if (!resp.IsSuccessStatusCode) return default;
            var json = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json, _jsonOpts);
        }

        private async Task UpsertDocAsync(string collection, object doc)
        {
            var content = new StringContent(Ser(doc), Encoding.UTF8, "application/json");
            await _http.PostAsync($"{_host}/collections/{collection}/documents?action=upsert", content);
        }

        private async Task<List<T>> SearchAllAsync<T>(string collection)
        {
            var resp = await _http.GetAsync($"{_host}/collections/{collection}/documents/search?q=*&query_by=id&per_page=250");
            if (!resp.IsSuccessStatusCode) return new List<T>();
            var json = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("hits", out var hits)) return new List<T>();
            var list = new List<T>();
            foreach (var hit in hits.EnumerateArray())
            {
                if (!hit.TryGetProperty("document", out var d)) continue;
                var item = JsonSerializer.Deserialize<T>(d.GetRawText(), _jsonOpts);
                if (item != null) list.Add(item);
            }
            return list;
        }

        private async Task<List<T>> SearchByFieldAsync<T>(string collection, string field, string value)
        {
            var resp = await _http.GetAsync($"{_host}/collections/{collection}/documents/search?q={Uri.EscapeDataString(value)}&query_by={field}&filter_by={field}:={Uri.EscapeDataString(value)}&per_page=1");
            if (!resp.IsSuccessStatusCode) return new List<T>();
            var json = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("hits", out var hits)) return new List<T>();
            var list = new List<T>();
            foreach (var hit in hits.EnumerateArray())
            {
                if (!hit.TryGetProperty("document", out var d)) continue;
                var item = JsonSerializer.Deserialize<T>(d.GetRawText(), _jsonOpts);
                if (item != null) list.Add(item);
            }
            return list;
        }

        private async Task DeleteDocAsync(string collection, string id)
        {
            await _http.DeleteAsync($"{_host}/collections/{collection}/documents/{id}");
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                await EnsureCollectionAsync(ColAvatars);
                await EnsureCollectionAsync(ColDetails);
                await EnsureCollectionAsync(ColHolons);
                IsProviderActivated = true; result.Result = true; result.IsError = false; result.Message = "TypesenseOASIS activated.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TypesenseOASIS: Error activating — {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;
        public override async Task<OASISResult<bool>> DeActivateProviderAsync() { IsProviderActivated = false; return await Task.FromResult(new OASISResult<bool> { Result = true, IsError = false, Message = "TypesenseOASIS deactivated." }); }
        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // ─── Avatar ───────────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (avatar.Id == Guid.Empty) avatar.Id = Guid.NewGuid();
                if (avatar.ProviderUniqueStorageKey == null) avatar.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.TypesenseOASIS] = avatar.Id.ToString();
                var doc = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(Ser(avatar), _jsonOpts)!;
                doc["id"] = JsonDocument.Parse($"\"{avatar.Id}\"").RootElement;
                await UpsertDocAsync(ColAvatars, doc);
                result.Result = avatar; result.IsError = false; result.Message = $"TypesenseOASIS: Avatar '{avatar.Username}' saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TypesenseOASIS: Error saving avatar: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) => SaveAvatarAsync(avatar).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var a = await GetDocAsync<Avatar>(ColAvatars, id.ToString());
                if (a == null || a.IsDeleted) { OASISErrorHandling.HandleError(ref result, $"TypesenseOASIS: No avatar for ID '{id}'."); return result; }
                result.Result = a; result.IsError = false; result.Message = "TypesenseOASIS: Avatar loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TypesenseOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) => LoadAvatarAsync(id, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var hits = await SearchByFieldAsync<Avatar>(ColAvatars, "username", username);
                var a = hits.Count > 0 ? hits[0] : null;
                if (a == null || a.IsDeleted) { OASISErrorHandling.HandleError(ref result, $"TypesenseOASIS: No avatar for username '{username}'."); return result; }
                result.Result = a; result.IsError = false; result.Message = "TypesenseOASIS: Avatar loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TypesenseOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string username, int version = 0) => LoadAvatarByUsernameAsync(username, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var hits = await SearchByFieldAsync<Avatar>(ColAvatars, "email", avatarEmail);
                var a = hits.Count > 0 ? hits[0] : null;
                if (a == null || a.IsDeleted) { OASISErrorHandling.HandleError(ref result, $"TypesenseOASIS: No avatar for email '{avatarEmail}'."); return result; }
                result.Result = a; result.IsError = false; result.Message = "TypesenseOASIS: Avatar loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TypesenseOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0) => LoadAvatarByEmailAsync(avatarEmail, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            if (Guid.TryParse(providerKey, out Guid id)) return await LoadAvatarAsync(id, version);
            var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, $"TypesenseOASIS: Invalid GUID '{providerKey}'."); return r;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0) => LoadAvatarByProviderKeyAsync(providerKey, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                var all = await SearchAllAsync<Avatar>(ColAvatars);
                var list = new List<IAvatar>(); foreach (var a in all) { if (!a.IsDeleted) list.Add(a); }
                result.Result = list; result.IsError = false; result.Message = $"TypesenseOASIS: Loaded {list.Count} avatar(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TypesenseOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) => LoadAllAvatarsAsync(version).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (softDelete) { var l = await LoadAvatarAsync(id); if (!l.IsError && l.Result != null) { var a = (Avatar)l.Result; a.DeletedDate = DateTime.UtcNow; await SaveAvatarAsync(a); } }
                else { await DeleteDocAsync(ColAvatars, id.ToString()); }
                result.Result = true; result.IsError = false; result.Message = $"TypesenseOASIS: Avatar '{id}' deleted.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TypesenseOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) => DeleteAvatarAsync(id, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string u, bool softDelete = true) { var a = await LoadAvatarByUsernameAsync(u); if (a.IsError || a.Result == null) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, $"TypesenseOASIS: Avatar '{u}' not found."); return r; } return await DeleteAvatarAsync(a.Result.Id, softDelete); }
        public override OASISResult<bool> DeleteAvatarByUsername(string u, bool softDelete = true) => DeleteAvatarByUsernameAsync(u, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string e, bool softDelete = true) { var a = await LoadAvatarByEmailAsync(e); if (a.IsError || a.Result == null) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "TypesenseOASIS: Avatar not found."); return r; } return await DeleteAvatarAsync(a.Result.Id, softDelete); }
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
                await UpsertDocAsync(ColDetails, doc);
                result.Result = d; result.IsError = false; result.Message = "TypesenseOASIS: AvatarDetail saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TypesenseOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail d) => SaveAvatarDetailAsync(d).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try { var d = await GetDocAsync<AvatarDetail>(ColDetails, id.ToString()); if (d == null) { OASISErrorHandling.HandleError(ref result, $"TypesenseOASIS: No detail for ID '{id}'."); return result; } result.Result = d; result.IsError = false; result.Message = "TypesenseOASIS: AvatarDetail loaded."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TypesenseOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => LoadAvatarDetailAsync(id, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string u, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try { var hits = await SearchByFieldAsync<AvatarDetail>(ColDetails, "username", u); var d = hits.Count > 0 ? hits[0] : null; if (d == null) { OASISErrorHandling.HandleError(ref result, $"TypesenseOASIS: No detail for username '{u}'."); return result; } result.Result = d; result.IsError = false; result.Message = "TypesenseOASIS: AvatarDetail loaded."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TypesenseOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string u, int version = 0) => LoadAvatarDetailByUsernameAsync(u, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string e, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try { var hits = await SearchByFieldAsync<AvatarDetail>(ColDetails, "email", e); var d = hits.Count > 0 ? hits[0] : null; if (d == null) { OASISErrorHandling.HandleError(ref result, $"TypesenseOASIS: No detail for email '{e}'."); return result; } result.Result = d; result.IsError = false; result.Message = "TypesenseOASIS: AvatarDetail loaded."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TypesenseOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string e, int version = 0) => LoadAvatarDetailByEmailAsync(e, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            try { var all = await SearchAllAsync<AvatarDetail>(ColDetails); result.Result = new List<IAvatarDetail>(all); result.IsError = false; result.Message = $"TypesenseOASIS: Loaded {all.Count} detail(s)."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TypesenseOASIS: {ex.Message}"); }
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
                await UpsertDocAsync(ColHolons, doc);
                result.Result = holon; result.IsError = false; result.Message = $"TypesenseOASIS: Holon '{holon.Name}' saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TypesenseOASIS: Error saving holon: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, int version = 0) => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>(); var saved = new List<IHolon>();
            foreach (var h in holons) { var r = await SaveHolonAsync(h, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, version); if (!r.IsError && r.Result != null) saved.Add(r.Result); }
            result.Result = saved; result.IsError = false; result.Message = $"TypesenseOASIS: Saved {saved.Count} holons."; return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, int version = 0) => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, version).Result;

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var h = await GetDocAsync<Holon>(ColHolons, id.ToString());
                if (h == null || h.IsDeleted) { OASISErrorHandling.HandleError(ref result, $"TypesenseOASIS: No holon for ID '{id}'."); return result; }
                result.Result = h; result.IsError = false; result.Message = "TypesenseOASIS: Holon loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TypesenseOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IHolon>> LoadHolonByProviderKeyAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) { if (Guid.TryParse(providerKey, out Guid id)) return await LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version); var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, $"TypesenseOASIS: Invalid GUID '{providerKey}'."); return r; }
        public override OASISResult<IHolon> LoadHolonByProviderKey(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonByProviderKeyAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var all = await SearchAllAsync<Holon>(ColHolons); var list = new List<IHolon>();
                foreach (var h in all) { if (h.IsDeleted) continue; if (holonType != HolonType.All && h.HolonType != holonType) continue; list.Add(h); }
                result.Result = list; result.IsError = false; result.Message = $"TypesenseOASIS: Loaded {list.Count} holon(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TypesenseOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false) => LoadAllHolonsAsync(holonType, loadChildren, recursive, maxChildDepth, version, continueOnError, loadChildrenFromProvider).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var all = await SearchAllAsync<Holon>(ColHolons); var list = new List<IHolon>();
                foreach (var h in all) { if (h.IsDeleted || h.ParentHolonId != id) continue; if (holonType != HolonType.All && h.HolonType != holonType) continue; list.Add(h); }
                result.Result = list; result.IsError = false; result.Message = $"TypesenseOASIS: Loaded {list.Count} child holon(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TypesenseOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false) => LoadHolonsForParentAsync(id, holonType, loadChildren, recursive, maxChildDepth, version, continueOnError, loadChildrenFromProvider).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false) { if (Guid.TryParse(providerKey, out Guid id)) return await LoadHolonsForParentAsync(id, holonType, loadChildren, recursive, maxChildDepth, version, continueOnError, loadChildrenFromProvider); var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, $"TypesenseOASIS: Invalid GUID '{providerKey}'."); return r; }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false) => LoadHolonsForParentAsync(providerKey, holonType, loadChildren, recursive, maxChildDepth, version, continueOnError, loadChildrenFromProvider).Result;

        public override async Task<OASISResult<bool>> DeleteHolonAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (softDelete) { var l = await LoadHolonAsync(id); if (!l.IsError && l.Result != null) { var h = (Holon)l.Result; h.DeletedDate = DateTime.UtcNow; await SaveHolonAsync(h); } }
                else { await DeleteDocAsync(ColHolons, id.ToString()); }
                result.Result = true; result.IsError = false; result.Message = $"TypesenseOASIS: Holon '{id}' deleted.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TypesenseOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> DeleteHolon(Guid id, bool softDelete = true) => DeleteHolonAsync(id, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteHolonAsync(string pk, bool softDelete = true) { if (Guid.TryParse(pk, out Guid id)) return await DeleteHolonAsync(id, softDelete); var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, $"TypesenseOASIS: Invalid GUID '{pk}'."); return r; }
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
        public override Task<OASISResult<IAvatar>> SearchAvatarsAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => throw new NotImplementedException("TypesenseOASIS: Use SearchAsync.");
        public override OASISResult<IAvatar> SearchAvatars(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => throw new NotImplementedException();
        public override Task<OASISResult<IHolon>> SearchHolonsAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => throw new NotImplementedException("TypesenseOASIS: Use SearchAsync.");
        public override OASISResult<IHolon> SearchHolons(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => throw new NotImplementedException();

        public override string GetProviderVersion() => "1.0.0";
        public override Task<string> GetProviderVersionAsync() => Task.FromResult("1.0.0");
    }
}
