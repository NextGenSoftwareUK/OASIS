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

        public override async Task<OASISResult<bool>> DeleteHolonAsync(Guid id, bool softDelete = true)
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

        public override OASISResult<bool> DeleteHolon(Guid id, bool softDelete = true)
            => DeleteHolonAsync(id, softDelete).GetAwaiter().GetResult();

        // ─── Search ─────────────────────────────────────────────────────────────

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
            => new OASISResult<ISearchResults> { Message = "Search not yet implemented for CouchDBOASIS.", IsWarning = true };

        public override Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
            => Task.FromResult(Search(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version));

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
    }
}
