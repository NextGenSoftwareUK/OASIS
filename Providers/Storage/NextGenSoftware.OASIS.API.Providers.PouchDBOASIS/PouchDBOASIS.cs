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

namespace NextGenSoftware.OASIS.API.Providers.PouchDBOASIS
{
    /// <summary>
    /// OASIS provider for PouchDB / CouchDB — an open-source NoSQL JSON database reachable via the
    /// CouchDB HTTP REST API (PouchDB server and CouchDB are wire-compatible).
    ///
    /// Databases:
    ///   oasis_avatars
    ///   oasis_avatar_details
    ///   oasis_holons
    ///
    /// Documents are stored as CouchDB docs:  { "_id": guid, "_rev": ..., ...data }
    /// Upsert requires _rev from the current revision to avoid 409 conflicts.
    ///
    /// Mango queries ($eq filters on username/email) are used for lookups.
    ///
    /// Constructor parameters:
    ///   serverUrl — CouchDB/PouchDB server base URL, e.g. "http://localhost:5984"
    ///   username  — optional basic-auth username
    ///   password  — optional basic-auth password
    /// </summary>
    public class PouchDBOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISDBStorageProvider
    {
        private readonly HttpClient _http;
        private readonly string _base;

        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private const string DbAvatars = "oasis_avatars";
        private const string DbDetails = "oasis_avatar_details";
        private const string DbHolons = "oasis_holons";

        public PouchDBOASIS(string serverUrl, string? username = null, string? password = null)
        {
            _base = serverUrl.TrimEnd('/');
            _http = new HttpClient();
            if (!string.IsNullOrEmpty(username) && password != null)
            {
                var creds = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
                _http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", creds);
            }
            ProviderName = "PouchDBOASIS";
            ProviderDescription = "PouchDB/CouchDB provider (CouchDB HTTP REST API — compatible with PouchDB Server)";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.PouchDBOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageLocalAndNetwork);
        }

        private static string Ser(object obj) => JsonSerializer.Serialize(obj, _jsonOpts);
        private StringContent Json(object obj) => new StringContent(Ser(obj), Encoding.UTF8, "application/json");

        private async Task EnsureDbAsync(string db)
        {
            var resp = await _http.PutAsync($"{_base}/{db}", null);
            // 201 = created, 412 = already exists — both OK
        }

        private async Task CreateIndexAsync(string db, string field)
        {
            var body = new { index = new { fields = new[] { field } } };
            await _http.PostAsync($"{_base}/{db}/_index", Json(body));
        }

        /// <summary>Gets the current _rev of a doc, or null if it doesn't exist.</summary>
        private async Task<string?> GetRevAsync(string db, string id)
        {
            var resp = await _http.GetAsync($"{_base}/{db}/{Uri.EscapeDataString(id)}");
            if (!resp.IsSuccessStatusCode) return null;
            var json = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("_rev", out var rev) ? rev.GetString() : null;
        }

        private async Task<string?> GetDocJsonAsync(string db, string id)
        {
            var resp = await _http.GetAsync($"{_base}/{db}/{Uri.EscapeDataString(id)}");
            if (!resp.IsSuccessStatusCode) return null;
            return await resp.Content.ReadAsStringAsync();
        }

        private async Task UpsertDocAsync(string db, string id, object data)
        {
            // Merge _id and optional _rev into the document
            var rawJson = Ser(data);
            var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(rawJson, _jsonOpts)!;
            dict["_id"] = JsonDocument.Parse($"\"{id}\"").RootElement;
            var rev = await GetRevAsync(db, id);
            if (rev != null) dict["_rev"] = JsonDocument.Parse($"\"{rev}\"").RootElement;
            var content = new StringContent(Ser(dict), Encoding.UTF8, "application/json");
            await _http.PutAsync($"{_base}/{db}/{Uri.EscapeDataString(id)}", content);
        }

        private async Task<T?> GetDocAsync<T>(string db, string id)
        {
            var json = await GetDocJsonAsync(db, id);
            if (json == null) return default;
            return JsonSerializer.Deserialize<T>(json, _jsonOpts);
        }

        private async Task<List<T>> MangoQueryAsync<T>(string db, string field, string value)
        {
            var body = new { selector = new Dictionary<string, object> { { field, new { _eq = value } } }, limit = 1 };
            // Mango syntax uses $eq
            var mangoBody = $"{{\"selector\":{{\"{field}\":{{\"$eq\":\"{value}\"}}}},\"limit\":1}}";
            var resp = await _http.PostAsync($"{_base}/{db}/_find", new StringContent(mangoBody, Encoding.UTF8, "application/json"));
            if (!resp.IsSuccessStatusCode) return new List<T>();
            var json = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("docs", out var docs)) return new List<T>();
            var list = new List<T>();
            foreach (var d in docs.EnumerateArray()) { var item = JsonSerializer.Deserialize<T>(d.GetRawText(), _jsonOpts); if (item != null) list.Add(item); }
            return list;
        }

        private async Task<List<T>> GetAllDocsAsync<T>(string db)
        {
            var resp = await _http.GetAsync($"{_base}/{db}/_all_docs?include_docs=true");
            if (!resp.IsSuccessStatusCode) return new List<T>();
            var json = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("rows", out var rows)) return new List<T>();
            var list = new List<T>();
            foreach (var row in rows.EnumerateArray())
            {
                if (!row.TryGetProperty("doc", out var d)) continue;
                var idProp = d.TryGetProperty("_id", out var idEl) ? idEl.GetString() : null;
                if (idProp != null && idProp.StartsWith("_design/")) continue; // skip design docs
                var item = JsonSerializer.Deserialize<T>(d.GetRawText(), _jsonOpts);
                if (item != null) list.Add(item);
            }
            return list;
        }

        private async Task SoftDeleteAsync(string db, string id)
        {
            var rev = await GetRevAsync(db, id);
            if (rev == null) return;
            var deleteBody = $"{{\"_id\":\"{id}\",\"_rev\":\"{rev}\",\"_deleted\":true}}";
            await _http.PutAsync($"{_base}/{db}/{Uri.EscapeDataString(id)}", new StringContent(deleteBody, Encoding.UTF8, "application/json"));
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                await EnsureDbAsync(DbAvatars);
                await EnsureDbAsync(DbDetails);
                await EnsureDbAsync(DbHolons);
                await CreateIndexAsync(DbAvatars, "username");
                await CreateIndexAsync(DbAvatars, "email");
                await CreateIndexAsync(DbDetails, "username");
                await CreateIndexAsync(DbDetails, "email");
                await CreateIndexAsync(DbHolons, "parentHolonId");
                IsProviderActivated = true; result.Result = true; result.IsError = false; result.Message = "PouchDBOASIS activated.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PouchDBOASIS: Error activating — {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;
        public override async Task<OASISResult<bool>> DeActivateProviderAsync() { IsProviderActivated = false; return await Task.FromResult(new OASISResult<bool> { Result = true, IsError = false, Message = "PouchDBOASIS deactivated." }); }
        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // ─── Avatar ───────────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (avatar.Id == Guid.Empty) avatar.Id = Guid.NewGuid();
                if (avatar.ProviderUniqueStorageKey == null) avatar.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.PouchDBOASIS] = avatar.Id.ToString();
                await UpsertDocAsync(DbAvatars, avatar.Id.ToString(), avatar);
                result.Result = avatar; result.IsError = false; result.Message = $"PouchDBOASIS: Avatar '{avatar.Username}' saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PouchDBOASIS: Error saving avatar: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) => SaveAvatarAsync(avatar).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var a = await GetDocAsync<Avatar>(DbAvatars, id.ToString());
                if (a == null || a.IsDeleted) { OASISErrorHandling.HandleError(ref result, $"PouchDBOASIS: No avatar for ID '{id}'."); return result; }
                result.Result = a; result.IsError = false; result.Message = "PouchDBOASIS: Avatar loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PouchDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) => LoadAvatarAsync(id, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var hits = await MangoQueryAsync<Avatar>(DbAvatars, "username", username);
                var a = hits.Count > 0 ? hits[0] : null;
                if (a == null || a.IsDeleted) { OASISErrorHandling.HandleError(ref result, $"PouchDBOASIS: No avatar for username '{username}'."); return result; }
                result.Result = a; result.IsError = false; result.Message = "PouchDBOASIS: Avatar loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PouchDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string username, int version = 0) => LoadAvatarByUsernameAsync(username, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var hits = await MangoQueryAsync<Avatar>(DbAvatars, "email", avatarEmail);
                var a = hits.Count > 0 ? hits[0] : null;
                if (a == null || a.IsDeleted) { OASISErrorHandling.HandleError(ref result, $"PouchDBOASIS: No avatar for email '{avatarEmail}'."); return result; }
                result.Result = a; result.IsError = false; result.Message = "PouchDBOASIS: Avatar loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PouchDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0) => LoadAvatarByEmailAsync(avatarEmail, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            if (Guid.TryParse(providerKey, out Guid id)) return await LoadAvatarAsync(id, version);
            var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, $"PouchDBOASIS: Invalid GUID '{providerKey}'."); return r;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0) => LoadAvatarByProviderKeyAsync(providerKey, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                var all = await GetAllDocsAsync<Avatar>(DbAvatars);
                var list = new List<IAvatar>(); foreach (var a in all) { if (!a.IsDeleted) list.Add(a); }
                result.Result = list; result.IsError = false; result.Message = $"PouchDBOASIS: Loaded {list.Count} avatar(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PouchDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) => LoadAllAvatarsAsync(version).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (softDelete) { var l = await LoadAvatarAsync(id); if (!l.IsError && l.Result != null) { var a = (Avatar)l.Result; a.DeletedDate = DateTime.UtcNow; await SaveAvatarAsync(a); } }
                else { await SoftDeleteAsync(DbAvatars, id.ToString()); }
                result.Result = true; result.IsError = false; result.Message = $"PouchDBOASIS: Avatar '{id}' deleted.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PouchDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) => DeleteAvatarAsync(id, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string u, bool softDelete = true) { var a = await LoadAvatarByUsernameAsync(u); if (a.IsError || a.Result == null) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, $"PouchDBOASIS: Avatar '{u}' not found."); return r; } return await DeleteAvatarAsync(a.Result.Id, softDelete); }
        public override OASISResult<bool> DeleteAvatarByUsername(string u, bool softDelete = true) => DeleteAvatarByUsernameAsync(u, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string e, bool softDelete = true) { var a = await LoadAvatarByEmailAsync(e); if (a.IsError || a.Result == null) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "PouchDBOASIS: Avatar not found."); return r; } return await DeleteAvatarAsync(a.Result.Id, softDelete); }
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
                await UpsertDocAsync(DbDetails, d.Id.ToString(), d);
                result.Result = d; result.IsError = false; result.Message = "PouchDBOASIS: AvatarDetail saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PouchDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail d) => SaveAvatarDetailAsync(d).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try { var d = await GetDocAsync<AvatarDetail>(DbDetails, id.ToString()); if (d == null) { OASISErrorHandling.HandleError(ref result, $"PouchDBOASIS: No detail for ID '{id}'."); return result; } result.Result = d; result.IsError = false; result.Message = "PouchDBOASIS: AvatarDetail loaded."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PouchDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => LoadAvatarDetailAsync(id, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string u, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try { var hits = await MangoQueryAsync<AvatarDetail>(DbDetails, "username", u); var d = hits.Count > 0 ? hits[0] : null; if (d == null) { OASISErrorHandling.HandleError(ref result, $"PouchDBOASIS: No detail for username '{u}'."); return result; } result.Result = d; result.IsError = false; result.Message = "PouchDBOASIS: AvatarDetail loaded."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PouchDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string u, int version = 0) => LoadAvatarDetailByUsernameAsync(u, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string e, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try { var hits = await MangoQueryAsync<AvatarDetail>(DbDetails, "email", e); var d = hits.Count > 0 ? hits[0] : null; if (d == null) { OASISErrorHandling.HandleError(ref result, $"PouchDBOASIS: No detail for email '{e}'."); return result; } result.Result = d; result.IsError = false; result.Message = "PouchDBOASIS: AvatarDetail loaded."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PouchDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string e, int version = 0) => LoadAvatarDetailByEmailAsync(e, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            try { var all = await GetAllDocsAsync<AvatarDetail>(DbDetails); result.Result = new List<IAvatarDetail>(all); result.IsError = false; result.Message = $"PouchDBOASIS: Loaded {all.Count} detail(s)."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PouchDBOASIS: {ex.Message}"); }
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
                await UpsertDocAsync(DbHolons, holon.Id.ToString(), holon);
                result.Result = holon; result.IsError = false; result.Message = $"PouchDBOASIS: Holon '{holon.Name}' saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PouchDBOASIS: Error saving holon: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, int version = 0) => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>(); var saved = new List<IHolon>();
            foreach (var h in holons) { var r = await SaveHolonAsync(h, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, version); if (!r.IsError && r.Result != null) saved.Add(r.Result); }
            result.Result = saved; result.IsError = false; result.Message = $"PouchDBOASIS: Saved {saved.Count} holons."; return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, int version = 0) => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, version).Result;

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var h = await GetDocAsync<Holon>(DbHolons, id.ToString());
                if (h == null || h.IsDeleted) { OASISErrorHandling.HandleError(ref result, $"PouchDBOASIS: No holon for ID '{id}'."); return result; }
                result.Result = h; result.IsError = false; result.Message = "PouchDBOASIS: Holon loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PouchDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IHolon>> LoadHolonByProviderKeyAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) { if (Guid.TryParse(providerKey, out Guid id)) return await LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version); var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, $"PouchDBOASIS: Invalid GUID '{providerKey}'."); return r; }
        public override OASISResult<IHolon> LoadHolonByProviderKey(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonByProviderKeyAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var all = await GetAllDocsAsync<Holon>(DbHolons); var list = new List<IHolon>();
                foreach (var h in all) { if (h.IsDeleted) continue; if (holonType != HolonType.All && h.HolonType != holonType) continue; list.Add(h); }
                result.Result = list; result.IsError = false; result.Message = $"PouchDBOASIS: Loaded {list.Count} holon(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PouchDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false) => LoadAllHolonsAsync(holonType, loadChildren, recursive, maxChildDepth, version, continueOnError, loadChildrenFromProvider).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var all = await GetAllDocsAsync<Holon>(DbHolons); var list = new List<IHolon>();
                foreach (var h in all) { if (h.IsDeleted || h.ParentHolonId != id) continue; if (holonType != HolonType.All && h.HolonType != holonType) continue; list.Add(h); }
                result.Result = list; result.IsError = false; result.Message = $"PouchDBOASIS: Loaded {list.Count} child holon(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PouchDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false) => LoadHolonsForParentAsync(id, holonType, loadChildren, recursive, maxChildDepth, version, continueOnError, loadChildrenFromProvider).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false) { if (Guid.TryParse(providerKey, out Guid id)) return await LoadHolonsForParentAsync(id, holonType, loadChildren, recursive, maxChildDepth, version, continueOnError, loadChildrenFromProvider); var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, $"PouchDBOASIS: Invalid GUID '{providerKey}'."); return r; }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false) => LoadHolonsForParentAsync(providerKey, holonType, loadChildren, recursive, maxChildDepth, version, continueOnError, loadChildrenFromProvider).Result;

        public override async Task<OASISResult<bool>> DeleteHolonAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (softDelete) { var l = await LoadHolonAsync(id); if (!l.IsError && l.Result != null) { var h = (Holon)l.Result; h.DeletedDate = DateTime.UtcNow; await SaveHolonAsync(h); } }
                else { await SoftDeleteAsync(DbHolons, id.ToString()); }
                result.Result = true; result.IsError = false; result.Message = $"PouchDBOASIS: Holon '{id}' deleted.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PouchDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> DeleteHolon(Guid id, bool softDelete = true) => DeleteHolonAsync(id, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteHolonAsync(string pk, bool softDelete = true) { if (Guid.TryParse(pk, out Guid id)) return await DeleteHolonAsync(id, softDelete); var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, $"PouchDBOASIS: Invalid GUID '{pk}'."); return r; }
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
        public override Task<OASISResult<IAvatar>> SearchAvatarsAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => throw new NotImplementedException("PouchDBOASIS: Use SearchAsync.");
        public override OASISResult<IAvatar> SearchAvatars(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => throw new NotImplementedException();
        public override Task<OASISResult<IHolon>> SearchHolonsAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => throw new NotImplementedException("PouchDBOASIS: Use SearchAsync.");
        public override OASISResult<IHolon> SearchHolons(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => throw new NotImplementedException();

        public override string GetProviderVersion() => "1.0.0";
        public override Task<string> GetProviderVersionAsync() => Task.FromResult("1.0.0");
    }
}
