using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
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

namespace NextGenSoftware.OASIS.API.Providers.CouchDBOASIS
{
    /// <summary>
    /// OASIS provider for Apache CouchDB — document database via CouchDB HTTP REST API.
    ///
    /// Databases: oasis_avatars, oasis_avatar_details, oasis_holons
    /// CouchDB uses _id and _rev for documents; upsert requires reading _rev first.
    /// GET /{db}/{id}     → fetch document (includes _rev)
    /// PUT /{db}/{id}     → create/update (requires _rev for update)
    /// DELETE /{db}/{id}?rev={rev} → soft-delete by updating IsDeleted flag
    /// POST /{db}/_find   → Mango query for searching
    /// GET /{db}/_all_docs?include_docs=true → list all
    /// </summary>
    public class CouchDBOASIS : OASISStorageProviderBase, IOASISDBStorageProvider
    {
        /// <summary>
        /// When true this provider stores a new record per save and links to the previous
        /// version (blockchain-style) instead of updating in place.
        /// </summary>
        public bool IsVersionControlEnabled { get; set; }

        private readonly HttpClient _http;
        private readonly string _baseUrl;

        private const string DbAvatars = "oasis_avatars";
        private const string DbAvatarDetails = "oasis_avatar_details";
        private const string DbHolons = "oasis_holons";

        public CouchDBOASIS(string baseUrl = "http://localhost:5984", string username = null, string password = null)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.CouchDBOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageLocalAndNetwork);

            _http = new HttpClient();
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                var creds = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", creds);
            }
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                await EnsureDatabasesAsync();
                IsProviderActivated = true;
                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"CouchDBOASIS: ActivateProviderAsync failed: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<bool> ActivateProvider()
            => ActivateProviderAsync().GetAwaiter().GetResult();

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            IsProviderActivated = false;
            result.Result = true;
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> DeActivateProvider()
            => DeActivateProviderAsync().GetAwaiter().GetResult();

        // ─── Avatars ────────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var doc = await GetDocAsync<Avatar>(DbAvatars, id.ToString());
                if (doc == null)
                    OASISErrorHandling.HandleError(ref result, $"CouchDBOASIS: Avatar {id} not found.");
                else if (doc.IsDeleted)
                    OASISErrorHandling.HandleError(ref result, $"CouchDBOASIS: Avatar {id} has been deleted.");
                else
                    result.Result = doc;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"CouchDBOASIS: LoadAvatarAsync failed: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0)
            => LoadAvatarAsync(id, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var doc = await FindOneAsync<Avatar>(DbAvatars, "Username", username);
                if (doc == null || doc.IsDeleted)
                    OASISErrorHandling.HandleError(ref result, $"CouchDBOASIS: Avatar with username '{username}' not found.");
                else
                    result.Result = doc;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"CouchDBOASIS: LoadAvatarByUsernameAsync failed: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string username, int version = 0)
            => LoadAvatarByUsernameAsync(username, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string email, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var doc = await FindOneAsync<Avatar>(DbAvatars, "Email", email);
                if (doc == null || doc.IsDeleted)
                    OASISErrorHandling.HandleError(ref result, $"CouchDBOASIS: Avatar with email '{email}' not found.");
                else
                    result.Result = doc;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"CouchDBOASIS: LoadAvatarByEmailAsync failed: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string email, int version = 0)
            => LoadAvatarByEmailAsync(email, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                var docs = await GetAllDocsAsync<Avatar>(DbAvatars);
                result.Result = docs.FindAll(d => !d.IsDeleted);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"CouchDBOASIS: LoadAllAvatarsAsync failed: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
            => LoadAllAvatarsAsync(version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (avatar.Id == Guid.Empty)
                    avatar.Id = Guid.NewGuid();
                await UpsertDocAsync(DbAvatars, avatar.Id.ToString(), avatar);
                result.Result = avatar;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"CouchDBOASIS: SaveAvatarAsync failed: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
            => SaveAvatarAsync(avatar).GetAwaiter().GetResult();

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (softDelete)
                {
                    var avatar = await GetDocAsync<Avatar>(DbAvatars, id.ToString());
                    if (avatar != null)
                    {
                        avatar.DeletedDate = DateTime.UtcNow;
                        await UpsertDocAsync(DbAvatars, id.ToString(), avatar);
                    }
                }
                else
                {
                    await HardDeleteDocAsync(DbAvatars, id.ToString());
                }
                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"CouchDBOASIS: DeleteAvatarAsync failed: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
            => DeleteAvatarAsync(id, softDelete).GetAwaiter().GetResult();

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string username, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                var avatar = await FindOneAsync<Avatar>(DbAvatars, "Username", username);
                if (avatar == null)
                    OASISErrorHandling.HandleError(ref result, $"CouchDBOASIS: Avatar '{username}' not found.");
                else
                    return await DeleteAvatarAsync(avatar.Id, softDelete);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"CouchDBOASIS: DeleteAvatarByUsernameAsync failed: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string username, bool softDelete = true)
            => DeleteAvatarByUsernameAsync(username, softDelete).GetAwaiter().GetResult();

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string email, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                var avatar = await FindOneAsync<Avatar>(DbAvatars, "Email", email);
                if (avatar == null)
                    OASISErrorHandling.HandleError(ref result, $"CouchDBOASIS: Avatar with email '{email}' not found.");
                else
                    return await DeleteAvatarAsync(avatar.Id, softDelete);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"CouchDBOASIS: DeleteAvatarByEmailAsync failed: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string email, bool softDelete = true)
            => DeleteAvatarByEmailAsync(email, softDelete).GetAwaiter().GetResult();

        // ─── AvatarDetails ──────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var doc = await GetDocAsync<AvatarDetail>(DbAvatarDetails, id.ToString());
                if (doc == null)
                    OASISErrorHandling.HandleError(ref result, $"CouchDBOASIS: AvatarDetail {id} not found.");
                else
                    result.Result = doc;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"CouchDBOASIS: LoadAvatarDetailAsync failed: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
            => LoadAvatarDetailAsync(id, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var doc = await FindOneAsync<AvatarDetail>(DbAvatarDetails, "Username", username);
                if (doc == null)
                    OASISErrorHandling.HandleError(ref result, $"CouchDBOASIS: AvatarDetail '{username}' not found.");
                else
                    result.Result = doc;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"CouchDBOASIS: LoadAvatarDetailByUsernameAsync failed: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string username, int version = 0)
            => LoadAvatarDetailByUsernameAsync(username, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string email, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var doc = await FindOneAsync<AvatarDetail>(DbAvatarDetails, "Email", email);
                if (doc == null)
                    OASISErrorHandling.HandleError(ref result, $"CouchDBOASIS: AvatarDetail '{email}' not found.");
                else
                    result.Result = doc;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"CouchDBOASIS: LoadAvatarDetailByEmailAsync failed: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string email, int version = 0)
            => LoadAvatarDetailByEmailAsync(email, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                var docs = await GetAllDocsAsync<AvatarDetail>(DbAvatarDetails);
                result.Result = docs;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"CouchDBOASIS: LoadAllAvatarDetailsAsync failed: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
            => LoadAllAvatarDetailsAsync(version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail detail)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                if (detail.Id == Guid.Empty)
                    detail.Id = Guid.NewGuid();
                await UpsertDocAsync(DbAvatarDetails, detail.Id.ToString(), detail);
                result.Result = detail;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"CouchDBOASIS: SaveAvatarDetailAsync failed: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail detail)
            => SaveAvatarDetailAsync(detail).GetAwaiter().GetResult();

        // ─── Holons ─────────────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var doc = await GetDocAsync<Holon>(DbHolons, id.ToString());
                if (doc == null)
                    OASISErrorHandling.HandleError(ref result, $"CouchDBOASIS: Holon {id} not found.");
                else if (doc.IsDeleted)
                    OASISErrorHandling.HandleError(ref result, $"CouchDBOASIS: Holon {id} has been deleted.");
                else
                    result.Result = doc;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"CouchDBOASIS: LoadHolonAsync failed: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var all = await FindAllAsync<Holon>(DbHolons, "ParentHolonId", id.ToString());
                result.Result = all.FindAll(h => !h.IsDeleted);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"CouchDBOASIS: LoadHolonsForParentAsync failed: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsForParentAsync(id, holonType, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var docs = await GetAllDocsAsync<Holon>(DbHolons);
                result.Result = docs.FindAll(h => !h.IsDeleted);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"CouchDBOASIS: LoadAllHolonsAsync failed: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadAllHolonsAsync(holonType, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (holon.Id == Guid.Empty)
                    holon.Id = Guid.NewGuid();
                await UpsertDocAsync(DbHolons, holon.Id.ToString(), holon);
                result.Result = holon;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"CouchDBOASIS: SaveHolonAsync failed: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var saved = new List<IHolon>();
            try
            {
                foreach (var h in holons)
                {
                    var r = await SaveHolonAsync(h, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
                    if (!r.IsError) saved.Add(r.Result);
                }
                result.Result = saved;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"CouchDBOASIS: SaveHolonsAsync failed: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).GetAwaiter().GetResult();

        public async Task<OASISResult<bool>> DeleteHolonSoftAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (softDelete)
                {
                    var holon = await GetDocAsync<Holon>(DbHolons, id.ToString());
                    if (holon != null)
                    {
                        holon.DeletedDate = DateTime.UtcNow;
                        await UpsertDocAsync(DbHolons, id.ToString(), holon);
                    }
                }
                else
                {
                    await HardDeleteDocAsync(DbHolons, id.ToString());
                }
                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"CouchDBOASIS: DeleteHolonAsync failed: {ex.Message}");
            }
            return result;
        }

        public OASISResult<bool> DeleteHolonSoft(Guid id, bool softDelete = true)
            => DeleteHolonSoftAsync(id, softDelete).GetAwaiter().GetResult();

        // ─── Search ─────────────────────────────────────────────────────────────



        // ─── Internal HTTP helpers ───────────────────────────────────────────────

        private async Task EnsureDatabasesAsync()
        {
            foreach (var db in new[] { DbAvatars, DbAvatarDetails, DbHolons })
            {
                var resp = await _http.PutAsync($"{_baseUrl}/{db}", null);
                // 201 = created, 412 = already exists — both are fine
                if (resp.StatusCode != System.Net.HttpStatusCode.Created &&
                    resp.StatusCode != System.Net.HttpStatusCode.PreconditionFailed)
                    resp.EnsureSuccessStatusCode();

                // Create Mango indexes for common search fields
                await EnsureIndexAsync(db, "Username");
                await EnsureIndexAsync(db, "Email");
                await EnsureIndexAsync(db, "ParentHolonId");
            }
        }

        private async Task EnsureIndexAsync(string db, string field)
        {
            var body = JsonSerializer.Serialize(new
            {
                index = new { fields = new[] { field } },
                name = $"idx_{field.ToLower()}",
                type = "json"
            });
            await _http.PostAsync($"{_baseUrl}/{db}/_index",
                new StringContent(body, Encoding.UTF8, "application/json"));
        }

        private async Task<T> GetDocAsync<T>(string db, string id) where T : class
        {
            var resp = await _http.GetAsync($"{_baseUrl}/{db}/{Uri.EscapeDataString(id)}");
            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            resp.EnsureSuccessStatusCode();
            var json = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        private async Task UpsertDocAsync<T>(string db, string id, T obj)
        {
            // Fetch current _rev (required for update)
            string rev = null;
            var existing = await _http.GetAsync($"{_baseUrl}/{db}/{Uri.EscapeDataString(id)}");
            if (existing.IsSuccessStatusCode)
            {
                var existingJson = await existing.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(existingJson);
                if (doc.RootElement.TryGetProperty("_rev", out var revProp))
                    rev = revProp.GetString();
            }

            // Serialize obj and inject _id and _rev
            var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
                JsonSerializer.Serialize(obj));
            dict["_id"] = JsonDocument.Parse($"\"{id}\"").RootElement;
            if (rev != null)
                dict["_rev"] = JsonDocument.Parse($"\"{rev}\"").RootElement;

            var body = JsonSerializer.Serialize(dict);
            var resp = await _http.PutAsync(
                $"{_baseUrl}/{db}/{Uri.EscapeDataString(id)}",
                new StringContent(body, Encoding.UTF8, "application/json"));
            resp.EnsureSuccessStatusCode();
        }

        private async Task HardDeleteDocAsync(string db, string id)
        {
            var existing = await _http.GetAsync($"{_baseUrl}/{db}/{Uri.EscapeDataString(id)}");
            if (!existing.IsSuccessStatusCode) return;
            var json = await existing.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("_rev", out var revProp)) return;
            var rev = revProp.GetString();
            var resp = await _http.DeleteAsync($"{_baseUrl}/{db}/{Uri.EscapeDataString(id)}?rev={rev}");
            resp.EnsureSuccessStatusCode();
        }

        private async Task<T> FindOneAsync<T>(string db, string field, string value) where T : class
        {
            var results = await FindAllAsync<T>(db, field, value);
            return results.Count > 0 ? results[0] : null;
        }

        private async Task<List<T>> FindAllAsync<T>(string db, string field, string value) where T : class
        {
            var body = JsonSerializer.Serialize(new
            {
                selector = new Dictionary<string, object> { { field, value } },
                limit = 1000
            });
            var resp = await _http.PostAsync(
                $"{_baseUrl}/{db}/_find",
                new StringContent(body, Encoding.UTF8, "application/json"));
            resp.EnsureSuccessStatusCode();
            var json = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var docs = doc.RootElement.GetProperty("docs");
            var list = new List<T>();
            foreach (var el in docs.EnumerateArray())
            {
                var item = JsonSerializer.Deserialize<T>(el.GetRawText(),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (item != null) list.Add(item);
            }
            return list;
        }

        private async Task<List<T>> GetAllDocsAsync<T>(string db) where T : class
        {
            var resp = await _http.GetAsync($"{_baseUrl}/{db}/_all_docs?include_docs=true");
            resp.EnsureSuccessStatusCode();
            var json = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var rows = doc.RootElement.GetProperty("rows");
            var list = new List<T>();
            foreach (var row in rows.EnumerateArray())
            {
                if (!row.TryGetProperty("doc", out var docEl)) continue;
                if (docEl.TryGetProperty("_id", out var idEl) && idEl.GetString()?.StartsWith("_design") == true) continue;
                var item = JsonSerializer.Deserialize<T>(docEl.GetRawText(),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (item != null) list.Add(item);
            }
            return list;
        }

        // ─── Remaining IOASISStorageProvider surface ─────────────────────────────

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
            => LoadAvatarByProviderKeyAsync(providerKey, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            if (Guid.TryParse(providerKey, out var id))
                return await LoadAvatarAsync(id, version);

            var all = await LoadAllAvatarsAsync(version);
            if (all.IsError || all.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, all.Message);
                return result;
            }

            foreach (var avatar in all.Result)
            {
                if (avatar.ProviderUniqueStorageKey != null
                    && avatar.ProviderUniqueStorageKey.TryGetValue(ProviderType.Value, out var key)
                    && key == providerKey)
                {
                    result.Result = avatar;
                    return result;
                }
            }

            OASISErrorHandling.HandleError(ref result, $"Avatar with provider key '{providerKey}' not found.");
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
            => DeleteAvatarAsync(providerKey, softDelete).GetAwaiter().GetResult();

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            var avatar = await LoadAvatarByProviderKeyAsync(providerKey);
            if (avatar.IsError || avatar.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, avatar.Message);
                return result;
            }
            return await DeleteAvatarAsync(avatar.Result.Id, softDelete);
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            if (Guid.TryParse(providerKey, out var id))
                return await LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);

            var all = await LoadAllHolonsAsync();
            if (all.IsError || all.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, all.Message);
                return result;
            }

            foreach (var holon in all.Result)
            {
                if (holon.ProviderUniqueStorageKey != null
                    && holon.ProviderUniqueStorageKey.TryGetValue(ProviderType.Value, out var key)
                    && key == providerKey)
                {
                    result.Result = holon;
                    return result;
                }
            }

            OASISErrorHandling.HandleError(ref result, $"Holon with provider key '{providerKey}' not found.");
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            if (Guid.TryParse(providerKey, out var id))
                return await LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);

            var parent = await LoadHolonAsync(providerKey);
            if (parent.IsError || parent.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, parent.Message);
                return result;
            }
            return await LoadHolonsForParentAsync(parent.Result.Id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id)
            => DeleteHolonAsync(id).GetAwaiter().GetResult();

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var result = new OASISResult<IHolon>();
            var loaded = await LoadHolonAsync(id);
            if (loaded.IsError || loaded.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, loaded.Message);
                return result;
            }

            var deleted = await DeleteHolonSoftAsync(id, true);
            if (deleted.IsError)
                OASISErrorHandling.HandleError(ref result, deleted.Message);
            else
                result.Result = loaded.Result;

            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
            => DeleteHolonAsync(providerKey).GetAwaiter().GetResult();

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            var result = new OASISResult<IHolon>();
            var holon = await LoadHolonAsync(providerKey);
            if (holon.IsError || holon.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, holon.Message);
                return result;
            }
            return await DeleteHolonAsync(holon.Result.Id);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var all = await LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
            if (all.IsError || all.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, all.Message);
                return result;
            }

            var matches = new List<IHolon>();
            foreach (var holon in all.Result)
            {
                if (holon.MetaData != null
                    && holon.MetaData.TryGetValue(metaKey, out var value)
                    && value?.ToString() == metaValue)
                    matches.Add(holon);
            }

            result.Result = matches;
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var all = await LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
            if (all.IsError || all.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, all.Message);
                return result;
            }

            if (metaKeyValuePairs == null || metaKeyValuePairs.Count == 0)
            {
                result.Result = new List<IHolon>(all.Result);
                return result;
            }

            var matches = new List<IHolon>();
            foreach (var holon in all.Result)
            {
                if (holon.MetaData == null) continue;

                var matched = 0;
                foreach (var pair in metaKeyValuePairs)
                {
                    if (holon.MetaData.TryGetValue(pair.Key, out var value) && value?.ToString() == pair.Value)
                        matched++;
                }

                var isMatch = metaKeyValuePairMatchMode == MetaKeyValuePairMatchMode.All
                    ? matched == metaKeyValuePairs.Count
                    : matched > 0;

                if (isMatch) matches.Add(holon);
            }

            result.Result = matches;
            return result;
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
            => ImportAsync(holons).GetAwaiter().GetResult();

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var result = new OASISResult<bool>();
            var saved = await SaveHolonsAsync(holons);
            if (saved.IsError)
                OASISErrorHandling.HandleError(ref result, saved.Message);
            else
                result.Result = true;
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
            => ExportAllAsync(version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
            => await LoadAllHolonsAsync(HolonType.All, true, true, 0, 0, true, false, version);

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
            => ExportAllDataForAvatarByIdAsync(avatarId, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var all = await LoadAllHolonsAsync(HolonType.All, true, true, 0, 0, true, false, version);
            if (all.IsError || all.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, all.Message);
                return result;
            }

            var owned = new List<IHolon>();
            foreach (var holon in all.Result)
            {
                if (holon.CreatedByAvatarId == avatarId)
                    owned.Add(holon);
            }

            result.Result = owned;
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
            => ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var avatar = await LoadAvatarByUsernameAsync(avatarUsername, version);
            if (avatar.IsError || avatar.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, avatar.Message);
                return result;
            }
            return await ExportAllDataForAvatarByIdAsync(avatar.Result.Id, version);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
            => ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var avatar = await LoadAvatarByEmailAsync(avatarEmailAddress, version);
            if (avatar.IsError || avatar.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, avatar.Message);
                return result;
            }
            return await ExportAllDataForAvatarByIdAsync(avatar.Result.Id, version);
        }

        // ─── Search ──────────────────────────────────────────────────────────────

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            var searchResults = new SearchResults();

            try
            {
                var groups = searchParams?.SearchGroups ?? new List<ISearchGroupBase>();
                var wantAvatars = groups.Count == 0 || groups.Exists(g => g.SearchAvatars);
                var wantHolons = groups.Count == 0 || groups.Exists(g => g.SearchHolons);

                var matchedAvatars = new Dictionary<Guid, IAvatar>();
                var matchedHolons = new Dictionary<Guid, IHolon>();

                // ── Avatars ──────────────────────────────────────────────────
                if (wantAvatars)
                {
                    var avatars = await LoadAllAvatarsAsync(version);
                    if (avatars.IsError && !continueOnError)
                    {
                        OASISErrorHandling.HandleError(ref result, avatars.Message);
                        return result;
                    }

                    foreach (var avatar in avatars.Result ?? new List<IAvatar>())
                    {
                        if (avatar == null) continue;
                        if (searchParams != null && searchParams.SearchOnlyForCurrentAvatar
                            && searchParams.AvatarId != Guid.Empty && avatar.Id != searchParams.AvatarId)
                            continue;

                        if (groups.Count == 0 || AvatarMatchesAnyGroup(avatar, groups))
                            matchedAvatars[avatar.Id] = avatar;
                    }
                }

                // ── Holons ───────────────────────────────────────────────────
                if (wantHolons)
                {
                    var holons = await LoadAllHolonsAsync(HolonType.All, loadChildren, recursive, maxChildDepth, 0, continueOnError, false, version);
                    if (holons.IsError && !continueOnError)
                    {
                        OASISErrorHandling.HandleError(ref result, holons.Message);
                        return result;
                    }

                    foreach (var holon in holons.Result ?? new List<IHolon>())
                    {
                        if (holon == null) continue;

                        if (searchParams != null)
                        {
                            if (searchParams.SearchOnlyForCurrentAvatar && searchParams.AvatarId != Guid.Empty
                                && holon.CreatedByAvatarId != searchParams.AvatarId)
                                continue;

                            if (searchParams.ParentId != Guid.Empty && holon.ParentHolonId != searchParams.ParentId)
                                continue;

                            if (!MetaDataMatches(holon, searchParams.FilterByMetaData, searchParams.MetaKeyValuePairMatchMode))
                                continue;
                        }

                        if (groups.Count == 0 || HolonMatchesAnyGroup(holon, groups))
                            matchedHolons[holon.Id] = holon;
                    }
                }

                searchResults.SearchResultAvatars = new List<IAvatar>(matchedAvatars.Values);
                searchResults.SearchResultHolons = new List<IHolon>(matchedHolons.Values);
                searchResults.NumberOfResults = searchResults.SearchResultAvatars.Count + searchResults.SearchResultHolons.Count;

                result.Result = searchResults;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"CouchDBOASIS: SearchAsync failed: {ex.Message}");
            }

            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
            => SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).GetAwaiter().GetResult();

        private static bool Contains(string source, string query)
            => !string.IsNullOrEmpty(source) && source.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;

        private static bool MetaDataMatches(IHolon holon, Dictionary<string, string> filter, MetaKeyValuePairMatchMode mode)
        {
            if (filter == null || filter.Count == 0) return true;
            if (holon.MetaData == null) return false;

            var matched = 0;
            foreach (var pair in filter)
            {
                if (holon.MetaData.TryGetValue(pair.Key, out var value) && value?.ToString() == pair.Value)
                    matched++;
            }

            return mode == MetaKeyValuePairMatchMode.All ? matched == filter.Count : matched > 0;
        }

        private static bool AvatarMatchesAnyGroup(IAvatar avatar, List<ISearchGroupBase> groups)
        {
            foreach (var group in groups)
            {
                if (!group.SearchAvatars) continue;

                var text = group as ISearchTextGroup;
                var query = text?.SearchQuery;
                if (string.IsNullOrWhiteSpace(query)) return true;

                var p = group.AvatarSearchParams;

                // No field flags set - match the natural identity fields.
                if (p == null)
                {
                    if (Contains(avatar.Username, query) || Contains(avatar.Email, query)
                        || Contains(avatar.FirstName, query) || Contains(avatar.LastName, query))
                        return true;
                    continue;
                }

                if (p.Username && Contains(avatar.Username, query)) return true;
                if (p.Email && Contains(avatar.Email, query)) return true;
                if (p.FirstName && Contains(avatar.FirstName, query)) return true;
                if (p.LastName && Contains(avatar.LastName, query)) return true;
                if (p.Title && Contains(avatar.Title, query)) return true;
                if (p.AvatarId && Contains(avatar.Id.ToString(), query)) return true;
                if (text != null && text.SearchIds && Contains(avatar.Id.ToString(), query)) return true;

                if (text != null && text.SearchProviderKeys && avatar.ProviderUniqueStorageKey != null)
                {
                    foreach (var key in avatar.ProviderUniqueStorageKey.Values)
                        if (Contains(key, query)) return true;
                }

                // Flags present but none of them matched a searchable field - fall
                // back to identity fields so a query is never silently dropped.
                if (!p.Username && !p.Email && !p.FirstName && !p.LastName && !p.Title && !p.AvatarId)
                {
                    if (Contains(avatar.Username, query) || Contains(avatar.Email, query))
                        return true;
                }
            }

            return false;
        }

        private static bool HolonMatchesAnyGroup(IHolon holon, List<ISearchGroupBase> groups)
        {
            foreach (var group in groups)
            {
                if (!group.SearchHolons) continue;

                if (group.HolonType != HolonType.All && holon.HolonType != group.HolonType)
                    continue;

                var text = group as ISearchTextGroup;
                var query = text?.SearchQuery;
                if (string.IsNullOrWhiteSpace(query)) return true;

                var p = group.HolonSearchParams;

                if (p == null)
                {
                    if (Contains(holon.Name, query) || Contains(holon.Description, query))
                        return true;
                    continue;
                }

                if (p.Name && Contains(holon.Name, query)) return true;
                if (p.Description && Contains(holon.Description, query)) return true;
                if (text != null && text.SearchIds && Contains(holon.Id.ToString(), query)) return true;

                if (p.MetaData && holon.MetaData != null)
                {
                    foreach (var kvp in holon.MetaData)
                        if (Contains(kvp.Key, query) || Contains(kvp.Value?.ToString(), query)) return true;
                }

                if ((p.ProviderUniqueStorageKey || (text != null && text.SearchProviderKeys))
                    && holon.ProviderUniqueStorageKey != null)
                {
                    foreach (var key in holon.ProviderUniqueStorageKey.Values)
                        if (Contains(key, query)) return true;
                }

                if (!p.Name && !p.Description && !p.MetaData && !p.ProviderUniqueStorageKey)
                {
                    if (Contains(holon.Name, query) || Contains(holon.Description, query))
                        return true;
                }
            }

            return false;
        }

    }
}
