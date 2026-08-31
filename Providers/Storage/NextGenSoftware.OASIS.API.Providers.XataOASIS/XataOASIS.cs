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

namespace NextGenSoftware.OASIS.API.Providers.XataOASIS
{
    /// <summary>
    /// OASIS provider for Xata — serverless database platform via Xata REST API.
    ///
    /// Tables: avatars, avatar_details, holons  (inside the "oasis" database)
    /// Base URL: https://{workspace}.{region}.xata.sh/db/oasis:main
    ///
    /// GET    /tables/{table}/data/{id}          → fetch record
    /// POST   /tables/{table}/data               → create record (returns generated id)
    /// PUT    /tables/{table}/data/{id}           → upsert (create or replace)
    /// DELETE /tables/{table}/data/{id}           → hard delete
    /// POST   /tables/{table}/query               → filter/search
    /// POST   /tables/{table}/summarize           → aggregations (not used here)
    ///
    /// Auth: Authorization: Bearer {apiKey}
    /// </summary>
    public class XataOASIS : OASISStorageProviderBase, IOASISDBStorageProvider
    {
        private readonly HttpClient _http;
        private readonly string _baseUrl;

        private const string TableAvatars = "avatars";
        private const string TableAvatarDetails = "avatar_details";
        private const string TableHolons = "holons";

        public XataOASIS(string workspaceUrl, string apiKey)
        {
            // workspaceUrl example: https://myworkspace-abc123.us-east-1.xata.sh/db/oasis:main
            _baseUrl = workspaceUrl.TrimEnd('/');
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.XataOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageLocalAndNetwork);

            _http = new HttpClient();
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                // Verify connectivity by querying the avatars table
                var resp = await _http.PostAsync($"{_baseUrl}/tables/{TableAvatars}/query",
                    JsonContent(new { page = new { size = 1 } }));
                resp.EnsureSuccessStatusCode();
                IsProviderActivated = true;
                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"XataOASIS: ActivateProviderAsync failed: {ex.Message}");
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
                var doc = await GetRecordAsync<Avatar>(TableAvatars, id.ToString());
                if (doc == null)
                    OASISErrorHandling.HandleError(ref result, $"XataOASIS: Avatar {id} not found.");
                else if (doc.IsDeleted)
                    OASISErrorHandling.HandleError(ref result, $"XataOASIS: Avatar {id} has been deleted.");
                else
                    result.Result = doc;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"XataOASIS: LoadAvatarAsync failed: {ex.Message}");
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
                var doc = await QueryOneAsync<Avatar>(TableAvatars, "Username", username);
                if (doc == null || doc.IsDeleted)
                    OASISErrorHandling.HandleError(ref result, $"XataOASIS: Avatar '{username}' not found.");
                else
                    result.Result = doc;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"XataOASIS: LoadAvatarByUsernameAsync failed: {ex.Message}");
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
                var doc = await QueryOneAsync<Avatar>(TableAvatars, "Email", email);
                if (doc == null || doc.IsDeleted)
                    OASISErrorHandling.HandleError(ref result, $"XataOASIS: Avatar with email '{email}' not found.");
                else
                    result.Result = doc;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"XataOASIS: LoadAvatarByEmailAsync failed: {ex.Message}");
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
                var docs = await QueryAllAsync<Avatar>(TableAvatars);
                result.Result = docs.FindAll(a => !a.IsDeleted);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"XataOASIS: LoadAllAvatarsAsync failed: {ex.Message}");
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
                await UpsertRecordAsync(TableAvatars, avatar.Id.ToString(), avatar);
                result.Result = avatar;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"XataOASIS: SaveAvatarAsync failed: {ex.Message}");
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
                    var avatar = await GetRecordAsync<Avatar>(TableAvatars, id.ToString());
                    if (avatar != null)
                    {
                        avatar.DeletedDate = DateTime.UtcNow;
                        await UpsertRecordAsync(TableAvatars, id.ToString(), avatar);
                    }
                }
                else
                {
                    await DeleteRecordAsync(TableAvatars, id.ToString());
                }
                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"XataOASIS: DeleteAvatarAsync failed: {ex.Message}");
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
                var avatar = await QueryOneAsync<Avatar>(TableAvatars, "Username", username);
                if (avatar == null)
                    OASISErrorHandling.HandleError(ref result, $"XataOASIS: Avatar '{username}' not found.");
                else
                    return await DeleteAvatarAsync(avatar.Id, softDelete);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"XataOASIS: DeleteAvatarByUsernameAsync failed: {ex.Message}");
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
                var avatar = await QueryOneAsync<Avatar>(TableAvatars, "Email", email);
                if (avatar == null)
                    OASISErrorHandling.HandleError(ref result, $"XataOASIS: Avatar with email '{email}' not found.");
                else
                    return await DeleteAvatarAsync(avatar.Id, softDelete);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"XataOASIS: DeleteAvatarByEmailAsync failed: {ex.Message}");
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
                var doc = await GetRecordAsync<AvatarDetail>(TableAvatarDetails, id.ToString());
                if (doc == null)
                    OASISErrorHandling.HandleError(ref result, $"XataOASIS: AvatarDetail {id} not found.");
                else
                    result.Result = doc;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"XataOASIS: LoadAvatarDetailAsync failed: {ex.Message}");
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
                var doc = await QueryOneAsync<AvatarDetail>(TableAvatarDetails, "Username", username);
                if (doc == null)
                    OASISErrorHandling.HandleError(ref result, $"XataOASIS: AvatarDetail '{username}' not found.");
                else
                    result.Result = doc;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"XataOASIS: LoadAvatarDetailByUsernameAsync failed: {ex.Message}");
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
                var doc = await QueryOneAsync<AvatarDetail>(TableAvatarDetails, "Email", email);
                if (doc == null)
                    OASISErrorHandling.HandleError(ref result, $"XataOASIS: AvatarDetail '{email}' not found.");
                else
                    result.Result = doc;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"XataOASIS: LoadAvatarDetailByEmailAsync failed: {ex.Message}");
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
                var docs = await QueryAllAsync<AvatarDetail>(TableAvatarDetails);
                result.Result = docs;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"XataOASIS: LoadAllAvatarDetailsAsync failed: {ex.Message}");
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
                await UpsertRecordAsync(TableAvatarDetails, detail.Id.ToString(), detail);
                result.Result = detail;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"XataOASIS: SaveAvatarDetailAsync failed: {ex.Message}");
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
                var doc = await GetRecordAsync<Holon>(TableHolons, id.ToString());
                if (doc == null)
                    OASISErrorHandling.HandleError(ref result, $"XataOASIS: Holon {id} not found.");
                else if (doc.IsDeleted)
                    OASISErrorHandling.HandleError(ref result, $"XataOASIS: Holon {id} has been deleted.");
                else
                    result.Result = doc;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"XataOASIS: LoadHolonAsync failed: {ex.Message}");
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
                var all = await QueryAllAsync<Holon>(TableHolons, "ParentHolonId", id.ToString());
                result.Result = all.FindAll(h => !h.IsDeleted);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"XataOASIS: LoadHolonsForParentAsync failed: {ex.Message}");
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
                var docs = await QueryAllAsync<Holon>(TableHolons);
                result.Result = docs.FindAll(h => !h.IsDeleted);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"XataOASIS: LoadAllHolonsAsync failed: {ex.Message}");
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
                await UpsertRecordAsync(TableHolons, holon.Id.ToString(), holon);
                result.Result = holon;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"XataOASIS: SaveHolonAsync failed: {ex.Message}");
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
                OASISErrorHandling.HandleError(ref result, $"XataOASIS: SaveHolonsAsync failed: {ex.Message}");
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
                    var holon = await GetRecordAsync<Holon>(TableHolons, id.ToString());
                    if (holon != null)
                    {
                        holon.DeletedDate = DateTime.UtcNow;
                        await UpsertRecordAsync(TableHolons, id.ToString(), holon);
                    }
                }
                else
                {
                    await DeleteRecordAsync(TableHolons, id.ToString());
                }
                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"XataOASIS: DeleteHolonAsync failed: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<bool> DeleteHolon(Guid id, bool softDelete = true)
            => DeleteHolonAsync(id, softDelete).GetAwaiter().GetResult();

        // ─── Search ─────────────────────────────────────────────────────────────

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
            => new OASISResult<ISearchResults> { Message = "Search not yet implemented for XataOASIS.", IsWarning = true };

        public override Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
            => Task.FromResult(Search(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version));

        // ─── Internal HTTP helpers ───────────────────────────────────────────────

        private StringContent JsonContent(object obj)
            => new StringContent(JsonSerializer.Serialize(obj), Encoding.UTF8, "application/json");

        private async Task<T> GetRecordAsync<T>(string table, string id) where T : class
        {
            var resp = await _http.GetAsync($"{_baseUrl}/tables/{table}/data/{Uri.EscapeDataString(id)}");
            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            resp.EnsureSuccessStatusCode();
            var json = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        private async Task UpsertRecordAsync<T>(string table, string id, T obj)
        {
            var body = JsonContent(obj);
            var resp = await _http.PutAsync(
                $"{_baseUrl}/tables/{table}/data/{Uri.EscapeDataString(id)}",
                body);
            resp.EnsureSuccessStatusCode();
        }

        private async Task DeleteRecordAsync(string table, string id)
        {
            var resp = await _http.DeleteAsync($"{_baseUrl}/tables/{table}/data/{Uri.EscapeDataString(id)}");
            if (resp.StatusCode != System.Net.HttpStatusCode.NotFound)
                resp.EnsureSuccessStatusCode();
        }

        private async Task<T> QueryOneAsync<T>(string table, string field, string value) where T : class
        {
            var results = await QueryAllAsync<T>(table, field, value);
            return results.Count > 0 ? results[0] : null;
        }

        private async Task<List<T>> QueryAllAsync<T>(string table, string field = null, string value = null) where T : class
        {
            object filter = field != null
                ? (object)new { filter = new Dictionary<string, object> { { field, value } }, page = new { size = 1000 } }
                : new { page = new { size = 1000 } };

            var resp = await _http.PostAsync($"{_baseUrl}/tables/{table}/query", JsonContent(filter));
            resp.EnsureSuccessStatusCode();
            var json = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var records = doc.RootElement.GetProperty("records");
            var list = new List<T>();
            foreach (var el in records.EnumerateArray())
            {
                var item = JsonSerializer.Deserialize<T>(el.GetRawText(),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (item != null) list.Add(item);
            }
            return list;
        }
    }
}
