using System;
using System.Collections.Generic;
using System.Linq;
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

namespace NextGenSoftware.OASIS.API.Providers.OrbitDBOASIS
{
    /// <summary>
    /// OASIS provider for OrbitDB via orbit-db-http-api (Node.js bridge server).
    ///
    /// OrbitDB is a serverless, distributed, peer-to-peer database built on IPFS.
    /// This provider connects to an orbit-db-http-api REST server running locally or remotely.
    ///
    /// Setup (one-time):
    ///   npm install -g orbit-db-http-api
    ///   orbit-db-http-api --port 3000
    ///
    /// Three docstore databases are used:
    ///   oasis.avatars, oasis.avatar_details, oasis.holons
    ///
    /// Constructor parameters:
    ///   serverUrl — orbit-db-http-api base URL, e.g. "http://localhost:3000"
    ///   orbitDir  — optional OrbitDB directory (default: "./orbitdb") passed on open
    /// </summary>
    public class OrbitDBOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISDBStorageProvider
    {
        private readonly string _serverUrl;
        private readonly HttpClient _http;

        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public OrbitDBOASIS(string serverUrl)
        {
            _serverUrl = serverUrl.TrimEnd('/');
            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            ProviderName = "OrbitDBOASIS";
            ProviderDescription = "OrbitDB provider (decentralised IPFS-backed docstore via orbit-db-http-api)";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.OrbitDBOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageLocalAndNetwork);
        }

        // ─── HTTP helpers ─────────────────────────────────────────────────────────

        private StringContent Json(object obj) => new StringContent(JsonSerializer.Serialize(obj, _jsonOpts), Encoding.UTF8, "application/json");
        private static T? Des<T>(string? json) => json == null ? default : JsonSerializer.Deserialize<T>(json, _jsonOpts);

        // orbit-db-http-api endpoints:
        //   POST /db/:name/create      — open/create a database (type=docstore)
        //   POST /db/:name/put         — upsert a document (body: {_id, ...fields})
        //   GET  /db/:name/get/:key    — get a document by _id
        //   GET  /db/:name/all         — get all documents
        //   DELETE /db/:name/del/:key  — delete a document

        private string DbName(string name) => Uri.EscapeDataString(name);

        private async Task<bool> OpenDatabaseAsync(string dbName)
        {
            var resp = await _http.PostAsync($"{_serverUrl}/db/{DbName(dbName)}", Json(new { type = "docstore" }));
            return resp.IsSuccessStatusCode;
        }

        private async Task<string?> PutDocAsync(string dbName, object doc)
        {
            var resp = await _http.PostAsync($"{_serverUrl}/db/{DbName(dbName)}/put", Json(doc));
            if (!resp.IsSuccessStatusCode) return null;
            return await resp.Content.ReadAsStringAsync();
        }

        private async Task<JsonElement?> GetDocAsync(string dbName, string key)
        {
            var resp = await _http.GetAsync($"{_serverUrl}/db/{DbName(dbName)}/get/{Uri.EscapeDataString(key)}");
            if (!resp.IsSuccessStatusCode) return null;
            var body = await resp.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(body)) return null;
            var arr = JsonSerializer.Deserialize<JsonElement[]>(body, _jsonOpts);
            return arr?.Length > 0 ? arr[0] : null;
        }

        private async Task<List<JsonElement>> GetAllDocsAsync(string dbName)
        {
            var resp = await _http.GetAsync($"{_serverUrl}/db/{DbName(dbName)}/all");
            if (!resp.IsSuccessStatusCode) return new List<JsonElement>();
            var body = await resp.Content.ReadAsStringAsync();
            var arr = JsonSerializer.Deserialize<JsonElement[]>(body, _jsonOpts);
            return arr?.ToList() ?? new List<JsonElement>();
        }

        private async Task DeleteDocAsync(string dbName, string key)
            => await _http.DeleteAsync($"{_serverUrl}/db/{DbName(dbName)}/del/{Uri.EscapeDataString(key)}");

        // ─── Activation ───────────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                await OpenDatabaseAsync("oasis.avatars");
                await OpenDatabaseAsync("oasis.avatar_details");
                await OpenDatabaseAsync("oasis.holons");
                result.Result = true; result.IsError = false;
                result.Message = "OrbitDBOASIS activated — 3 docstores opened (oasis.avatars, oasis.avatar_details, oasis.holons).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"OrbitDBOASIS: Error activating provider — {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;
        public override async Task<OASISResult<bool>> DeActivateProviderAsync() => await Task.FromResult(new OASISResult<bool> { Result = true, IsError = false, Message = "OrbitDBOASIS deactivated." });
        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // ─── Avatar saving ────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (avatar.Id == Guid.Empty) avatar.Id = Guid.NewGuid();
                if (avatar.ProviderUniqueStorageKey == null) avatar.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.OrbitDBOASIS] = avatar.Id.ToString();
                var doc = new { _id = avatar.Id.ToString(), username = avatar.Username, email = avatar.Email, is_deleted = avatar.IsDeleted, data_json = JsonSerializer.Serialize(avatar, _jsonOpts) };
                await PutDocAsync("oasis.avatars", doc);
                result.Result = avatar; result.IsError = false; result.Message = $"OrbitDBOASIS: Avatar '{avatar.Username}' saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"OrbitDBOASIS: Error saving avatar: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) => SaveAvatarAsync(avatar).Result;

        // ─── Avatar loading ───────────────────────────────────────────────────────

        private Avatar? ParseAvatar(JsonElement? el)
        {
            if (el == null) return null;
            if (el.Value.TryGetProperty("data_json", out var dj)) return Des<Avatar>(dj.GetString());
            return null;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var doc = await GetDocAsync("oasis.avatars", id.ToString());
                var avatar = ParseAvatar(doc);
                if (avatar == null || avatar.IsDeleted) { OASISErrorHandling.HandleError(ref result, $"OrbitDBOASIS: No avatar found for ID '{id}'."); return result; }
                result.Result = avatar; result.IsError = false; result.Message = $"OrbitDBOASIS: Avatar loaded for ID '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"OrbitDBOASIS: Error loading avatar '{id}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) => LoadAvatarAsync(id, version).Result;

        private async Task<Avatar?> FindAvatarByFieldAsync(string field, string value)
        {
            var all = await GetAllDocsAsync("oasis.avatars");
            return all.Select(el => { try { if (el.TryGetProperty(field, out var f) && f.GetString() == value && el.TryGetProperty("is_deleted", out var d) && !d.GetBoolean()) return ParseAvatar(el); } catch { } return null; }).FirstOrDefault(a => a != null);
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var avatar = await FindAvatarByFieldAsync("username", username);
                if (avatar == null) { OASISErrorHandling.HandleError(ref result, $"OrbitDBOASIS: No avatar found for username '{username}'."); return result; }
                result.Result = avatar; result.IsError = false; result.Message = $"OrbitDBOASIS: Avatar loaded for username '{username}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"OrbitDBOASIS: Error loading avatar by username '{username}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string username, int version = 0) => LoadAvatarByUsernameAsync(username, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var avatar = await FindAvatarByFieldAsync("email", avatarEmail);
                if (avatar == null) { OASISErrorHandling.HandleError(ref result, $"OrbitDBOASIS: No avatar found for email '{avatarEmail}'."); return result; }
                result.Result = avatar; result.IsError = false; result.Message = $"OrbitDBOASIS: Avatar loaded for email '{avatarEmail}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"OrbitDBOASIS: Error loading avatar by email '{avatarEmail}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0) => LoadAvatarByEmailAsync(avatarEmail, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            if (Guid.TryParse(providerKey, out Guid id)) return await LoadAvatarAsync(id, version);
            var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, $"OrbitDBOASIS: providerKey '{providerKey}' is not a valid GUID."); return r;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0) => LoadAvatarByProviderKeyAsync(providerKey, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                var all = await GetAllDocsAsync("oasis.avatars");
                var avatars = all.Select(el => { try { if (el.TryGetProperty("is_deleted", out var d) && d.GetBoolean()) return null; return ParseAvatar(el); } catch { return null; } }).Where(a => a != null).Cast<IAvatar>().ToList();
                result.Result = avatars; result.IsError = false; result.Message = $"OrbitDBOASIS: Loaded {avatars.Count} avatar(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"OrbitDBOASIS: Error loading all avatars: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) => LoadAllAvatarsAsync(version).Result;

        // ─── Avatar deletion ──────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (softDelete)
                {
                    var loaded = await LoadAvatarAsync(id);
                    if (loaded.IsError || loaded.Result == null) { OASISErrorHandling.HandleError(ref result, $"OrbitDBOASIS: Avatar '{id}' not found."); return result; }
                    var av = (Avatar)loaded.Result;
                    av.DeletedDate = DateTime.UtcNow;
                    await SaveAvatarAsync(av);
                }
                else { await DeleteDocAsync("oasis.avatars", id.ToString()); }
                result.Result = true; result.IsError = false; result.Message = $"OrbitDBOASIS: Avatar '{id}' {(softDelete ? "soft" : "hard")}-deleted.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"OrbitDBOASIS: Error deleting avatar '{id}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) => DeleteAvatarAsync(id, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string username, bool softDelete = true)
        {
            var a = await LoadAvatarByUsernameAsync(username);
            if (a.IsError || a.Result == null) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, $"OrbitDBOASIS: Avatar '{username}' not found."); return r; }
            return await DeleteAvatarAsync(a.Result.Id, softDelete);
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string username, bool softDelete = true) => DeleteAvatarByUsernameAsync(username, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string email, bool softDelete = true)
        {
            var a = await LoadAvatarByEmailAsync(email);
            if (a.IsError || a.Result == null) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, $"OrbitDBOASIS: Avatar with email '{email}' not found."); return r; }
            return await DeleteAvatarAsync(a.Result.Id, softDelete);
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string email, bool softDelete = true) => DeleteAvatarByEmailAsync(email, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            if (Guid.TryParse(providerKey, out Guid id)) return await DeleteAvatarAsync(id, softDelete);
            return await DeleteAvatarByUsernameAsync(providerKey, softDelete);
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true) => DeleteAvatarAsync(providerKey, softDelete).Result;

        // ─── AvatarDetail ─────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                if (avatarDetail.Id == Guid.Empty) avatarDetail.Id = Guid.NewGuid();
                var doc = new { _id = avatarDetail.Id.ToString(), username = avatarDetail.Username, email = avatarDetail.Email, data_json = JsonSerializer.Serialize(avatarDetail, _jsonOpts) };
                await PutDocAsync("oasis.avatar_details", doc);
                result.Result = avatarDetail; result.IsError = false; result.Message = "OrbitDBOASIS: AvatarDetail saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"OrbitDBOASIS: Error saving avatar detail: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail) => SaveAvatarDetailAsync(avatarDetail).Result;

        private AvatarDetail? ParseDetail(JsonElement? el)
        {
            if (el == null) return null;
            if (el.Value.TryGetProperty("data_json", out var dj)) return Des<AvatarDetail>(dj.GetString());
            return null;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var doc = await GetDocAsync("oasis.avatar_details", id.ToString());
                var d = ParseDetail(doc);
                if (d == null) { OASISErrorHandling.HandleError(ref result, $"OrbitDBOASIS: No avatar detail found for ID '{id}'."); return result; }
                result.Result = d; result.IsError = false; result.Message = $"OrbitDBOASIS: AvatarDetail loaded for ID '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"OrbitDBOASIS: Error loading avatar detail '{id}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => LoadAvatarDetailAsync(id, version).Result;

        private async Task<AvatarDetail?> FindDetailByFieldAsync(string field, string value)
        {
            var all = await GetAllDocsAsync("oasis.avatar_details");
            return all.Select(el => { try { if (el.TryGetProperty(field, out var f) && f.GetString() == value) return ParseDetail(el); } catch { } return null; }).FirstOrDefault(d => d != null);
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var d = await FindDetailByFieldAsync("username", username);
                if (d == null) { OASISErrorHandling.HandleError(ref result, $"OrbitDBOASIS: No avatar detail found for username '{username}'."); return result; }
                result.Result = d; result.IsError = false; result.Message = $"OrbitDBOASIS: AvatarDetail loaded for username '{username}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"OrbitDBOASIS: Error loading avatar detail by username '{username}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string username, int version = 0) => LoadAvatarDetailByUsernameAsync(username, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string email, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var d = await FindDetailByFieldAsync("email", email);
                if (d == null) { OASISErrorHandling.HandleError(ref result, $"OrbitDBOASIS: No avatar detail found for email '{email}'."); return result; }
                result.Result = d; result.IsError = false; result.Message = $"OrbitDBOASIS: AvatarDetail loaded for email '{email}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"OrbitDBOASIS: Error loading avatar detail by email '{email}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string email, int version = 0) => LoadAvatarDetailByEmailAsync(email, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                var all = await GetAllDocsAsync("oasis.avatar_details");
                var details = all.Select(el => { try { return ParseDetail(el); } catch { return null; } }).Where(d => d != null).Cast<IAvatarDetail>().ToList();
                result.Result = details; result.IsError = false; result.Message = $"OrbitDBOASIS: Loaded {details.Count} avatar detail(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"OrbitDBOASIS: Error loading all avatar details: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0) => LoadAllAvatarDetailsAsync(version).Result;

        // ─── Holon saving ─────────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (holon.Id == Guid.Empty) holon.Id = Guid.NewGuid();
                if (holon.ProviderUniqueStorageKey == null) holon.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.OrbitDBOASIS] = holon.Id.ToString();
                var doc = new { _id = holon.Id.ToString(), parent_holon_id = holon.ParentHolonId.ToString(), holon_type = (int)holon.HolonType, is_deleted = holon.IsDeleted, data_json = JsonSerializer.Serialize(holon, _jsonOpts) };
                await PutDocAsync("oasis.holons", doc);
                result.Result = holon; result.IsError = false; result.Message = $"OrbitDBOASIS: Holon '{holon.Name}' saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"OrbitDBOASIS: Error saving holon '{holon.Name}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var saved = new List<IHolon>(); var errors = new List<string>();
            foreach (var holon in holons) { var r = await SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider); if (r.IsError) errors.Add(r.Message ?? ""); else if (r.Result != null) saved.Add(r.Result); }
            result.Result = saved; result.IsError = errors.Count > 0; result.Message = errors.Count > 0 ? string.Join("; ", errors) : $"OrbitDBOASIS: {saved.Count} holon(s) saved.";
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;

        // ─── Holon loading ────────────────────────────────────────────────────────

        private Holon? ParseHolon(JsonElement el) { try { if (el.TryGetProperty("data_json", out var dj)) return Des<Holon>(dj.GetString()); } catch { } return null; }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var doc = await GetDocAsync("oasis.holons", id.ToString());
                if (doc == null) { OASISErrorHandling.HandleError(ref result, $"OrbitDBOASIS: No holon found for ID '{id}'."); return result; }
                var holon = ParseHolon(doc.Value);
                if (holon == null || holon.IsDeleted) { OASISErrorHandling.HandleError(ref result, $"OrbitDBOASIS: Holon '{id}' not found or deleted."); return result; }
                result.Result = holon; result.IsError = false; result.Message = $"OrbitDBOASIS: Holon loaded for ID '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"OrbitDBOASIS: Error loading holon '{id}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            if (Guid.TryParse(providerKey, out Guid id)) return await LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);
            var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, $"OrbitDBOASIS: providerKey '{providerKey}' is not a valid GUID."); return r;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        private async Task<List<IHolon>> GetFilteredHolonsAsync(Func<JsonElement, bool>? filter = null)
        {
            var all = await GetAllDocsAsync("oasis.holons");
            return all.Where(el => { try { if (el.TryGetProperty("is_deleted", out var d) && d.GetBoolean()) return false; return filter == null || filter(el); } catch { return false; } }).Select(el => ParseHolon(el) as IHolon).Where(h => h != null).Cast<IHolon>().ToList();
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var holons = await GetFilteredHolonsAsync(type == HolonType.All ? null : (Func<JsonElement, bool>)(el => el.TryGetProperty("holon_type", out var t) && t.GetInt32() == (int)type));
                result.Result = holons; result.IsError = false; result.Message = $"OrbitDBOASIS: Loaded {holons.Count} holon(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"OrbitDBOASIS: Error loading all holons: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var parentStr = id.ToString();
                bool Filter(JsonElement el) { if (!el.TryGetProperty("parent_holon_id", out var p) || p.GetString() != parentStr) return false; if (type != HolonType.All && el.TryGetProperty("holon_type", out var t) && t.GetInt32() != (int)type) return false; return true; }
                var holons = await GetFilteredHolonsAsync(Filter);
                result.Result = holons; result.IsError = false; result.Message = $"OrbitDBOASIS: Loaded {holons.Count} holon(s) for parent '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"OrbitDBOASIS: Error loading holons for parent '{id}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            if (Guid.TryParse(providerKey, out Guid id)) return await LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
            var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, $"OrbitDBOASIS: providerKey '{providerKey}' is not a valid GUID."); return r;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        // ─── Holon deletion ───────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var loaded = await LoadHolonAsync(id);
                if (loaded.IsError || loaded.Result == null) { OASISErrorHandling.HandleError(ref result, $"OrbitDBOASIS: No holon found with ID '{id}'."); return result; }
                var holon = (Holon)loaded.Result;
                holon.DeletedDate = DateTime.UtcNow;
                await SaveHolonAsync(holon);
                result.Result = holon; result.IsError = false; result.Message = $"OrbitDBOASIS: Holon '{id}' soft-deleted.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id) => DeleteHolonAsync(id).Result;

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            if (Guid.TryParse(providerKey, out Guid id)) return await DeleteHolonAsync(id);
            var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, $"OrbitDBOASIS: providerKey '{providerKey}' is not a valid GUID."); return r;
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey) => DeleteHolonAsync(providerKey).Result;

        // ─── Search + Metadata ────────────────────────────────────────────────────

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            try
            {
                string? q = searchParams.SearchGroups?.OfType<SearchTextGroup>().FirstOrDefault()?.SearchQuery?.ToLower();
                var all = await LoadAllHolonsAsync();
                var holons = all.Result?.ToList() ?? new List<IHolon>();
                if (!string.IsNullOrEmpty(q)) holons = holons.Where(h => h.Name?.ToLower().Contains(q) == true || h.Description?.ToLower().Contains(q) == true).ToList();
                result.Result = new SearchResults { SearchResultHolons = holons, NumberOfResults = holons.Count };
                result.IsError = false; result.Message = $"OrbitDBOASIS: Found {holons.Count} holon(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
            => SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var all = await LoadAllHolonsAsync(type);
            var holons = all.Result?.Where(h => h.MetaData != null && h.MetaData.TryGetValue(metaKey, out var v) && v?.ToString() == metaValue).ToList() ?? new List<IHolon>();
            return new OASISResult<IEnumerable<IHolon>> { Result = holons, IsError = false, Message = $"OrbitDBOASIS: Loaded {holons.Count} holon(s) where {metaKey}={metaValue}." };
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var all = await LoadAllHolonsAsync(type);
            bool IsMatch(IHolon h) { if (h.MetaData == null) return false; var checks = metaKeyValuePairs.Select(kvp => h.MetaData.TryGetValue(kvp.Key, out var v) && v?.ToString() == kvp.Value); return metaKeyValuePairMatchMode == MetaKeyValuePairMatchMode.Any ? checks.Any(c => c) : checks.All(c => c); }
            var holons = all.Result?.Where(IsMatch).ToList() ?? new List<IHolon>();
            return new OASISResult<IEnumerable<IHolon>> { Result = holons, IsError = false, Message = $"OrbitDBOASIS: Loaded {holons.Count} holon(s) matching metadata filter." };
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        // ─── Import / Export ──────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons) { var s = await SaveHolonsAsync(holons); return new OASISResult<bool> { Result = !s.IsError, IsError = s.IsError, Message = s.Message }; }
        public override OASISResult<bool> Import(IEnumerable<IHolon> holons) => ImportAsync(holons).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0) => await LoadAllHolonsAsync();
        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0) => ExportAllAsync(version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            var all = await LoadAllHolonsAsync();
            var holons = all.Result?.Where(h => h.CreatedByAvatarId == avatarId).ToList() ?? new List<IHolon>();
            return new OASISResult<IEnumerable<IHolon>> { Result = holons, IsError = false, Message = $"OrbitDBOASIS: Exported {holons.Count} holon(s) for avatar '{avatarId}'." };
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0) => ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string u, int version = 0) { var a = await LoadAvatarByUsernameAsync(u); if (a.IsError || a.Result == null) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, $"OrbitDBOASIS: Avatar '{u}' not found."); return r; } return await ExportAllDataForAvatarByIdAsync(a.Result.Id, version); }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string u, int version = 0) => ExportAllDataForAvatarByUsernameAsync(u, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string e, int version = 0) { var a = await LoadAvatarByEmailAsync(e); if (a.IsError || a.Result == null) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, $"OrbitDBOASIS: Avatar with email '{e}' not found."); return r; } return await ExportAllDataForAvatarByIdAsync(a.Result.Id, version); }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string e, int version = 0) => ExportAllDataForAvatarByEmailAsync(e, version).Result;

        public bool IsVersionControlEnabled { get; set; } = false;
    }
}
