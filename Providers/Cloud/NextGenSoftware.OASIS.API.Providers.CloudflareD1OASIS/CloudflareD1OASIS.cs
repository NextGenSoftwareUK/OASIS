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

namespace NextGenSoftware.OASIS.API.Providers.CloudflareD1OASIS
{
    /// <summary>
    /// OASIS provider for Cloudflare D1 — serverless SQLite-compatible database accessed
    /// via the Cloudflare REST API (no SDK required; uses System.Net.Http).
    ///
    /// API endpoint: POST https://api.cloudflare.com/client/v4/accounts/{accountId}/d1/database/{databaseId}/query
    /// Body:  { "sql": "...", "params": [...] }
    /// Auth:  Authorization: Bearer {apiToken}
    ///
    /// Tables (SQLite-compatible):
    ///   oasis_avatars        (id TEXT PRIMARY KEY, username TEXT, email TEXT, is_deleted INTEGER DEFAULT 0, data_json TEXT)
    ///   oasis_avatar_details (id TEXT PRIMARY KEY, username TEXT, email TEXT, data_json TEXT)
    ///   oasis_holons         (id TEXT PRIMARY KEY, parent_holon_id TEXT, holon_type INTEGER, is_deleted INTEGER DEFAULT 0, data_json TEXT)
    ///
    /// Constructor parameters:
    ///   accountId  — Cloudflare account ID
    ///   databaseId — D1 database ID
    ///   apiToken   — Cloudflare API token with D1 edit permissions
    /// </summary>
    public class CloudflareD1OASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISDBStorageProvider
    {
        private readonly string _accountId;
        private readonly string _databaseId;
        private readonly string _apiToken;
        private readonly HttpClient _http;
        private readonly string _baseUrl;

        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public CloudflareD1OASIS(string accountId, string databaseId, string apiToken)
        {
            _accountId = accountId;
            _databaseId = databaseId;
            _apiToken = apiToken;
            _baseUrl = $"https://api.cloudflare.com/client/v4/accounts/{accountId}/d1/database/{databaseId}/query";
            _http = new HttpClient();
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);
            ProviderName = "CloudflareD1OASIS";
            ProviderDescription = "Cloudflare D1 provider (serverless SQLite-compatible REST API)";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.CloudflareD1OASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageLocalAndNetwork);
        }

        private static string Ser(object o) => JsonSerializer.Serialize(o, _jsonOpts);
        private static T? Des<T>(string? json) => json == null ? default : JsonSerializer.Deserialize<T>(json, _jsonOpts);

        // ─── D1 query helper ──────────────────────────────────────────────────────

        private async Task<List<Dictionary<string, JsonElement>>> QueryAsync(string sql, object?[]? parameters = null)
        {
            var body = new { sql, @params = parameters ?? Array.Empty<object?>() };
            var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            var response = await _http.PostAsync(_baseUrl, content);
            var raw = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode) throw new Exception($"D1 HTTP {(int)response.StatusCode}: {raw}");
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            if (!root.GetProperty("success").GetBoolean()) throw new Exception($"D1 error: {raw}");
            var results = new List<Dictionary<string, JsonElement>>();
            if (root.TryGetProperty("result", out var resultArr) && resultArr.ValueKind == JsonValueKind.Array)
            {
                foreach (var resultSet in resultArr.EnumerateArray())
                {
                    if (resultSet.TryGetProperty("results", out var rows) && rows.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var row in rows.EnumerateArray())
                        {
                            var dict = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
                            foreach (var prop in row.EnumerateObject()) dict[prop.Name] = prop.Value.Clone();
                            results.Add(dict);
                        }
                    }
                }
            }
            return results;
        }

        private string? GetStr(Dictionary<string, JsonElement> row, string key) => row.TryGetValue(key, out var v) && v.ValueKind != JsonValueKind.Null ? v.GetString() : null;
        private bool GetBool(Dictionary<string, JsonElement> row, string key) => row.TryGetValue(key, out var v) && v.ValueKind == JsonValueKind.Number && v.GetInt32() != 0;
        private int GetInt(Dictionary<string, JsonElement> row, string key) => row.TryGetValue(key, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : 0;

        // ─── Activation ───────────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                await QueryAsync("CREATE TABLE IF NOT EXISTS oasis_avatars (id TEXT NOT NULL PRIMARY KEY, username TEXT, email TEXT, is_deleted INTEGER DEFAULT 0, data_json TEXT)");
                await QueryAsync("CREATE INDEX IF NOT EXISTS idx_avatars_username ON oasis_avatars(username)");
                await QueryAsync("CREATE INDEX IF NOT EXISTS idx_avatars_email ON oasis_avatars(email)");
                await QueryAsync("CREATE TABLE IF NOT EXISTS oasis_avatar_details (id TEXT NOT NULL PRIMARY KEY, username TEXT, email TEXT, data_json TEXT)");
                await QueryAsync("CREATE TABLE IF NOT EXISTS oasis_holons (id TEXT NOT NULL PRIMARY KEY, parent_holon_id TEXT, holon_type INTEGER, is_deleted INTEGER DEFAULT 0, data_json TEXT)");
                await QueryAsync("CREATE INDEX IF NOT EXISTS idx_holons_parent ON oasis_holons(parent_holon_id)");
                result.Result = true; result.IsError = false; result.Message = "CloudflareD1OASIS activated — schema created.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CloudflareD1OASIS: Error activating — {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;
        public override async Task<OASISResult<bool>> DeActivateProviderAsync() => await Task.FromResult(new OASISResult<bool> { Result = true, IsError = false, Message = "CloudflareD1OASIS deactivated." });
        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // ─── Avatar saving ────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (avatar.Id == Guid.Empty) avatar.Id = Guid.NewGuid();
                if (avatar.ProviderUniqueStorageKey == null) avatar.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.CloudflareD1OASIS] = avatar.Id.ToString();
                await QueryAsync(
                    "INSERT INTO oasis_avatars (id, username, email, is_deleted, data_json) VALUES (?, ?, ?, ?, ?) ON CONFLICT(id) DO UPDATE SET username=excluded.username, email=excluded.email, is_deleted=excluded.is_deleted, data_json=excluded.data_json",
                    new object?[] { avatar.Id.ToString(), avatar.Username, avatar.Email, avatar.IsDeleted ? 1 : 0, Ser(avatar) });
                result.Result = avatar; result.IsError = false; result.Message = $"CloudflareD1OASIS: Avatar '{avatar.Username}' saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CloudflareD1OASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) => SaveAvatarAsync(avatar).Result;

        // ─── Avatar loading ───────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var rows = await QueryAsync("SELECT data_json, is_deleted FROM oasis_avatars WHERE id = ?", new object?[] { id.ToString() });
                if (rows.Count == 0 || GetBool(rows[0], "is_deleted")) { OASISErrorHandling.HandleError(ref result, $"CloudflareD1OASIS: No avatar for ID '{id}'."); return result; }
                var avatar = Des<Avatar>(GetStr(rows[0], "data_json")); if (avatar == null) { OASISErrorHandling.HandleError(ref result, "CloudflareD1OASIS: Deserialise failed."); return result; }
                result.Result = avatar; result.IsError = false; result.Message = "CloudflareD1OASIS: Avatar loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CloudflareD1OASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) => LoadAvatarAsync(id, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var rows = await QueryAsync("SELECT data_json FROM oasis_avatars WHERE username = ? AND is_deleted = 0 LIMIT 1", new object?[] { username });
                if (rows.Count == 0) { OASISErrorHandling.HandleError(ref result, $"CloudflareD1OASIS: No avatar for username '{username}'."); return result; }
                var avatar = Des<Avatar>(GetStr(rows[0], "data_json")); if (avatar == null) { OASISErrorHandling.HandleError(ref result, "CloudflareD1OASIS: Deserialise failed."); return result; }
                result.Result = avatar; result.IsError = false; result.Message = "CloudflareD1OASIS: Avatar loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CloudflareD1OASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string username, int version = 0) => LoadAvatarByUsernameAsync(username, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string email, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var rows = await QueryAsync("SELECT data_json FROM oasis_avatars WHERE email = ? AND is_deleted = 0 LIMIT 1", new object?[] { email });
                if (rows.Count == 0) { OASISErrorHandling.HandleError(ref result, $"CloudflareD1OASIS: No avatar for email '{email}'."); return result; }
                var avatar = Des<Avatar>(GetStr(rows[0], "data_json")); if (avatar == null) { OASISErrorHandling.HandleError(ref result, "CloudflareD1OASIS: Deserialise failed."); return result; }
                result.Result = avatar; result.IsError = false; result.Message = "CloudflareD1OASIS: Avatar loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CloudflareD1OASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string email, int version = 0) => LoadAvatarByEmailAsync(email, version).Result;
        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string pk, int version = 0) { if (Guid.TryParse(pk, out Guid id)) return await LoadAvatarAsync(id, version); var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, $"CloudflareD1OASIS: Invalid GUID '{pk}'."); return r; }
        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string pk, int version = 0) => LoadAvatarByProviderKeyAsync(pk, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                var rows = await QueryAsync("SELECT data_json FROM oasis_avatars WHERE is_deleted = 0");
                var avatars = rows.Select(r => Des<Avatar>(GetStr(r, "data_json"))).Where(a => a != null).Cast<IAvatar>().ToList();
                result.Result = avatars; result.IsError = false; result.Message = $"CloudflareD1OASIS: Loaded {avatars.Count} avatar(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CloudflareD1OASIS: {ex.Message}"); }
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
                    var loaded = await LoadAvatarAsync(id); if (loaded.IsError || loaded.Result == null) { OASISErrorHandling.HandleError(ref result, $"CloudflareD1OASIS: Avatar '{id}' not found."); return result; }
                    var av = (Avatar)loaded.Result; av.DeletedDate = DateTime.UtcNow;
                    await QueryAsync("UPDATE oasis_avatars SET is_deleted = 1, data_json = ? WHERE id = ?", new object?[] { Ser(av), id.ToString() });
                }
                else { await QueryAsync("DELETE FROM oasis_avatars WHERE id = ?", new object?[] { id.ToString() }); }
                result.Result = true; result.IsError = false; result.Message = $"CloudflareD1OASIS: Avatar '{id}' deleted.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CloudflareD1OASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) => DeleteAvatarAsync(id, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string u, bool softDelete = true) { var a = await LoadAvatarByUsernameAsync(u); if (a.IsError || a.Result == null) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "Not found."); return r; } return await DeleteAvatarAsync(a.Result.Id, softDelete); }
        public override OASISResult<bool> DeleteAvatarByUsername(string u, bool softDelete = true) => DeleteAvatarByUsernameAsync(u, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string e, bool softDelete = true) { var a = await LoadAvatarByEmailAsync(e); if (a.IsError || a.Result == null) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "Not found."); return r; } return await DeleteAvatarAsync(a.Result.Id, softDelete); }
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
                await QueryAsync("INSERT INTO oasis_avatar_details (id, username, email, data_json) VALUES (?, ?, ?, ?) ON CONFLICT(id) DO UPDATE SET username=excluded.username, email=excluded.email, data_json=excluded.data_json", new object?[] { d.Id.ToString(), d.Username, d.Email, Ser(d) });
                result.Result = d; result.IsError = false; result.Message = "CloudflareD1OASIS: AvatarDetail saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CloudflareD1OASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail d) => SaveAvatarDetailAsync(d).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var rows = await QueryAsync("SELECT data_json FROM oasis_avatar_details WHERE id = ?", new object?[] { id.ToString() });
                if (rows.Count == 0) { OASISErrorHandling.HandleError(ref result, $"CloudflareD1OASIS: No detail for ID '{id}'."); return result; }
                var detail = Des<AvatarDetail>(GetStr(rows[0], "data_json")); if (detail == null) { OASISErrorHandling.HandleError(ref result, "CloudflareD1OASIS: Deserialise failed."); return result; }
                result.Result = detail; result.IsError = false; result.Message = "CloudflareD1OASIS: AvatarDetail loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CloudflareD1OASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => LoadAvatarDetailAsync(id, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string u, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try { var rows = await QueryAsync("SELECT data_json FROM oasis_avatar_details WHERE username = ? LIMIT 1", new object?[] { u }); if (rows.Count == 0) { OASISErrorHandling.HandleError(ref result, $"CloudflareD1OASIS: No detail for username '{u}'."); return result; } var d = Des<AvatarDetail>(GetStr(rows[0], "data_json")); if (d == null) { OASISErrorHandling.HandleError(ref result, "Deserialise failed."); return result; } result.Result = d; result.IsError = false; result.Message = "CloudflareD1OASIS: AvatarDetail loaded."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CloudflareD1OASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string u, int version = 0) => LoadAvatarDetailByUsernameAsync(u, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string e, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try { var rows = await QueryAsync("SELECT data_json FROM oasis_avatar_details WHERE email = ? LIMIT 1", new object?[] { e }); if (rows.Count == 0) { OASISErrorHandling.HandleError(ref result, $"CloudflareD1OASIS: No detail for email '{e}'."); return result; } var d = Des<AvatarDetail>(GetStr(rows[0], "data_json")); if (d == null) { OASISErrorHandling.HandleError(ref result, "Deserialise failed."); return result; } result.Result = d; result.IsError = false; result.Message = "CloudflareD1OASIS: AvatarDetail loaded."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CloudflareD1OASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string e, int version = 0) => LoadAvatarDetailByEmailAsync(e, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            try { var rows = await QueryAsync("SELECT data_json FROM oasis_avatar_details"); var details = rows.Select(r => Des<AvatarDetail>(GetStr(r, "data_json"))).Where(d => d != null).Cast<IAvatarDetail>().ToList(); result.Result = details; result.IsError = false; result.Message = $"CloudflareD1OASIS: Loaded {details.Count} detail(s)."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CloudflareD1OASIS: {ex.Message}"); }
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
                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.CloudflareD1OASIS] = holon.Id.ToString();
                await QueryAsync(
                    "INSERT INTO oasis_holons (id, parent_holon_id, holon_type, is_deleted, data_json) VALUES (?, ?, ?, ?, ?) ON CONFLICT(id) DO UPDATE SET parent_holon_id=excluded.parent_holon_id, holon_type=excluded.holon_type, is_deleted=excluded.is_deleted, data_json=excluded.data_json",
                    new object?[] { holon.Id.ToString(), holon.ParentHolonId.ToString(), (int)holon.HolonType, holon.IsDeleted ? 1 : 0, Ser(holon) });
                result.Result = holon; result.IsError = false; result.Message = $"CloudflareD1OASIS: Holon '{holon.Name}' saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CloudflareD1OASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var saved = new List<IHolon>(); var errors = new List<string>();
            foreach (var h in holons) { var r = await SaveHolonAsync(h, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider); if (r.IsError) errors.Add(r.Message ?? ""); else if (r.Result != null) saved.Add(r.Result); }
            result.Result = saved; result.IsError = errors.Count > 0; result.Message = errors.Count > 0 ? string.Join("; ", errors) : $"CloudflareD1OASIS: {saved.Count} holon(s) saved.";
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;

        // ─── Holon loading ────────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var rows = await QueryAsync("SELECT data_json, is_deleted FROM oasis_holons WHERE id = ?", new object?[] { id.ToString() });
                if (rows.Count == 0 || GetBool(rows[0], "is_deleted")) { OASISErrorHandling.HandleError(ref result, $"CloudflareD1OASIS: No holon for ID '{id}'."); return result; }
                var holon = Des<Holon>(GetStr(rows[0], "data_json")); if (holon == null) { OASISErrorHandling.HandleError(ref result, "CloudflareD1OASIS: Deserialise failed."); return result; }
                result.Result = holon; result.IsError = false; result.Message = "CloudflareD1OASIS: Holon loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CloudflareD1OASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string pk, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) { if (Guid.TryParse(pk, out Guid id)) return await LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version); var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, $"CloudflareD1OASIS: Invalid GUID '{pk}'."); return r; }
        public override OASISResult<IHolon> LoadHolon(string pk, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonAsync(pk, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var sql = type == HolonType.All ? "SELECT data_json, holon_type FROM oasis_holons WHERE is_deleted = 0" : "SELECT data_json, holon_type FROM oasis_holons WHERE is_deleted = 0 AND holon_type = ?";
                var rows = type == HolonType.All ? await QueryAsync(sql) : await QueryAsync(sql, new object?[] { (int)type });
                var holons = rows.Select(r => Des<Holon>(GetStr(r, "data_json"))).Where(h => h != null).Cast<IHolon>().ToList();
                result.Result = holons; result.IsError = false; result.Message = $"CloudflareD1OASIS: Loaded {holons.Count} holon(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CloudflareD1OASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var sql = type == HolonType.All ? "SELECT data_json FROM oasis_holons WHERE parent_holon_id = ? AND is_deleted = 0" : "SELECT data_json FROM oasis_holons WHERE parent_holon_id = ? AND is_deleted = 0 AND holon_type = ?";
                var rows = type == HolonType.All ? await QueryAsync(sql, new object?[] { id.ToString() }) : await QueryAsync(sql, new object?[] { id.ToString(), (int)type });
                var holons = rows.Select(r => Des<Holon>(GetStr(r, "data_json"))).Where(h => h != null).Cast<IHolon>().ToList();
                result.Result = holons; result.IsError = false; result.Message = $"CloudflareD1OASIS: Loaded {holons.Count} holon(s) for parent '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CloudflareD1OASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string pk, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) { if (Guid.TryParse(pk, out Guid id)) return await LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version); var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, $"CloudflareD1OASIS: Invalid GUID '{pk}'."); return r; }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string pk, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsForParentAsync(pk, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        // ─── Holon deletion ───────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var loaded = await LoadHolonAsync(id); if (loaded.IsError || loaded.Result == null) { OASISErrorHandling.HandleError(ref result, $"CloudflareD1OASIS: Holon '{id}' not found."); return result; }
                var holon = (Holon)loaded.Result; holon.DeletedDate = DateTime.UtcNow;
                await QueryAsync("UPDATE oasis_holons SET is_deleted = 1, data_json = ? WHERE id = ?", new object?[] { Ser(holon), id.ToString() });
                result.Result = holon; result.IsError = false; result.Message = $"CloudflareD1OASIS: Holon '{id}' soft-deleted.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CloudflareD1OASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id) => DeleteHolonAsync(id).Result;
        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string pk) { if (Guid.TryParse(pk, out Guid id)) return await DeleteHolonAsync(id); var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, $"CloudflareD1OASIS: Invalid GUID '{pk}'."); return r; }
        public override OASISResult<IHolon> DeleteHolon(string pk) => DeleteHolonAsync(pk).Result;

        // ─── Search + Metadata ────────────────────────────────────────────────────

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            try { string? q = searchParams.SearchGroups?.OfType<SearchTextGroup>().FirstOrDefault()?.SearchQuery?.ToLower(); var all = await LoadAllHolonsAsync(); var holons = all.Result?.ToList() ?? new List<IHolon>(); if (!string.IsNullOrEmpty(q)) holons = holons.Where(h => h.Name?.ToLower().Contains(q) == true || h.Description?.ToLower().Contains(q) == true).ToList(); result.Result = new SearchResults { SearchResultHolons = holons, NumberOfResults = holons.Count }; result.IsError = false; result.Message = $"CloudflareD1OASIS: Found {holons.Count} result(s)."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        { var all = await LoadAllHolonsAsync(type); var holons = all.Result?.Where(h => h.MetaData != null && h.MetaData.TryGetValue(metaKey, out var v) && v?.ToString() == metaValue).ToList() ?? new List<IHolon>(); return new OASISResult<IEnumerable<IHolon>> { Result = holons, IsError = false, Message = $"CloudflareD1OASIS: {holons.Count} holon(s)." }; }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKvp, MetaKeyValuePairMatchMode mode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        { var all = await LoadAllHolonsAsync(type); bool IsMatch(IHolon h) { if (h.MetaData == null) return false; var checks = metaKvp.Select(kvp => h.MetaData.TryGetValue(kvp.Key, out var v) && v?.ToString() == kvp.Value); return mode == MetaKeyValuePairMatchMode.Any ? checks.Any(c => c) : checks.All(c => c); } var holons = all.Result?.Where(IsMatch).ToList() ?? new List<IHolon>(); return new OASISResult<IEnumerable<IHolon>> { Result = holons, IsError = false, Message = $"CloudflareD1OASIS: {holons.Count} holon(s)." }; }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKvp, MetaKeyValuePairMatchMode mode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsByMetaDataAsync(metaKvp, mode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        // ─── Import / Export ──────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons) { var s = await SaveHolonsAsync(holons); return new OASISResult<bool> { Result = !s.IsError, IsError = s.IsError, Message = s.Message }; }
        public override OASISResult<bool> Import(IEnumerable<IHolon> holons) => ImportAsync(holons).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0) => await LoadAllHolonsAsync();
        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0) => ExportAllAsync(version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0) { var all = await LoadAllHolonsAsync(); var h = all.Result?.Where(x => x.CreatedByAvatarId == avatarId).ToList() ?? new List<IHolon>(); return new OASISResult<IEnumerable<IHolon>> { Result = h, IsError = false, Message = $"CloudflareD1OASIS: {h.Count} holon(s)." }; }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0) => ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string u, int version = 0) { var a = await LoadAvatarByUsernameAsync(u); if (a.IsError || a.Result == null) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "Not found."); return r; } return await ExportAllDataForAvatarByIdAsync(a.Result.Id, version); }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string u, int version = 0) => ExportAllDataForAvatarByUsernameAsync(u, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string e, int version = 0) { var a = await LoadAvatarByEmailAsync(e); if (a.IsError || a.Result == null) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "Not found."); return r; } return await ExportAllDataForAvatarByIdAsync(a.Result.Id, version); }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string e, int version = 0) => ExportAllDataForAvatarByEmailAsync(e, version).Result;

        public bool IsVersionControlEnabled { get; set; } = false;
    }
}
