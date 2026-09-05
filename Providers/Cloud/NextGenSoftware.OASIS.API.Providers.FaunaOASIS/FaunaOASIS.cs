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

namespace NextGenSoftware.OASIS.API.Providers.FaunaOASIS
{
    /// <summary>
    /// OASIS provider for Fauna — serverless document + relational database accessed
    /// via Fauna FQL v10 HTTP API (no SDK required; uses System.Net.Http).
    ///
    /// API endpoint: POST https://db.fauna.com/query/1
    /// Auth:  Authorization: Bearer {secretKey}
    /// Body:  { "query": "..." }   (FQL v10 expression string)
    ///
    /// Collections: oasis_avatars, oasis_avatar_details, oasis_holons
    /// Each document stores OASIS data as JSON encoded into a 'data_json' string field
    /// alongside indexed lookup fields (username, email, etc.).
    ///
    /// Constructor parameters:
    ///   secretKey — Fauna database secret key
    ///   endpoint  — Fauna API endpoint (default "https://db.fauna.com")
    /// </summary>
    public class FaunaOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISDBStorageProvider
    {
        private readonly string _secretKey;
        private readonly string _endpoint;
        private readonly HttpClient _http;

        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public FaunaOASIS(string secretKey, string endpoint = "https://db.fauna.com")
        {
            _secretKey = secretKey;
            _endpoint = endpoint.TrimEnd('/');
            _http = new HttpClient();
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", secretKey);
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            ProviderName = "FaunaOASIS";
            ProviderDescription = "Fauna provider (serverless document+relational database via FQL v10 HTTP API)";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.FaunaOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageLocalAndNetwork);
        }

        private static string Ser(object o) => JsonSerializer.Serialize(o, _jsonOpts);
        private static T? Des<T>(string? json) => json == null ? default : JsonSerializer.Deserialize<T>(json, _jsonOpts);

        // ─── FQL helper ───────────────────────────────────────────────────────────

        private async Task<JsonElement> FqlAsync(string fql)
        {
            var body = new { query = fql };
            var content = new StringContent(Ser(body), Encoding.UTF8, "application/json");
            var response = await _http.PostAsync($"{_endpoint}/query/1", content);
            var raw = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode) throw new Exception($"Fauna HTTP {(int)response.StatusCode}: {raw}");
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            if (root.TryGetProperty("error", out var err)) throw new Exception($"Fauna FQL error: {err.GetRawText()}");
            return root.TryGetProperty("data", out var data) ? data.Clone() : root.Clone();
        }

        // Escape a string for inline FQL embedding
        private static string FqlStr(string? s)
        {
            if (s == null) return "null";
            return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r") + "\"";
        }

        // Parse results from a Fauna Set page
        private static List<Dictionary<string, JsonElement>> ParsePage(JsonElement root)
        {
            var results = new List<Dictionary<string, JsonElement>>();
            JsonElement items = root;
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("data", out var d)) items = d;
            if (items.ValueKind == JsonValueKind.Array)
                foreach (var item in items.EnumerateArray())
                {
                    var dict = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
                    foreach (var p in item.EnumerateObject()) dict[p.Name] = p.Value.Clone();
                    results.Add(dict);
                }
            else if (items.ValueKind == JsonValueKind.Object)
            {
                var dict = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
                foreach (var p in items.EnumerateObject()) dict[p.Name] = p.Value.Clone();
                results.Add(dict);
            }
            return results;
        }

        private string? GetStr(Dictionary<string, JsonElement> d, string key) => d.TryGetValue(key, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
        private bool GetBool(Dictionary<string, JsonElement> d, string key) => d.TryGetValue(key, out var v) && v.ValueKind == JsonValueKind.True;
        private int GetInt(Dictionary<string, JsonElement> d, string key) => d.TryGetValue(key, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : 0;

        // ─── Activation — ensure collections exist ────────────────────────────────

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                // Create collections idempotently using FQL v10
                await FqlAsync("if (!Collection.byName('oasis_avatars').exists()) { Collection.create({ name: 'oasis_avatars' }) } else { null }");
                await FqlAsync("if (!Collection.byName('oasis_avatar_details').exists()) { Collection.create({ name: 'oasis_avatar_details' }) } else { null }");
                await FqlAsync("if (!Collection.byName('oasis_holons').exists()) { Collection.create({ name: 'oasis_holons' }) } else { null }");
                result.Result = true; result.IsError = false; result.Message = "FaunaOASIS activated — collections created.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"FaunaOASIS: Error activating — {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;
        public override async Task<OASISResult<bool>> DeActivateProviderAsync() => await Task.FromResult(new OASISResult<bool> { Result = true, IsError = false, Message = "FaunaOASIS deactivated." });
        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // ─── Avatar saving ────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (avatar.Id == Guid.Empty) avatar.Id = Guid.NewGuid();
                if (avatar.ProviderUniqueStorageKey == null) avatar.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.FaunaOASIS] = avatar.Id.ToString();
                var dataJson = Ser(avatar).Replace("\"", "\\\"");
                var fql = $@"
let existing = oasis_avatars.where(.id_field == {FqlStr(avatar.Id.ToString())}).first()
if (existing != null) {{
  existing.update({{ id_field: {FqlStr(avatar.Id.ToString())}, username: {FqlStr(avatar.Username)}, email: {FqlStr(avatar.Email)}, is_deleted: {(avatar.IsDeleted ? "true" : "false")}, data_json: {FqlStr(Ser(avatar))} }})
}} else {{
  oasis_avatars.create({{ id_field: {FqlStr(avatar.Id.ToString())}, username: {FqlStr(avatar.Username)}, email: {FqlStr(avatar.Email)}, is_deleted: false, data_json: {FqlStr(Ser(avatar))} }})
}}";
                await FqlAsync(fql);
                result.Result = avatar; result.IsError = false; result.Message = $"FaunaOASIS: Avatar '{avatar.Username}' saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"FaunaOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) => SaveAvatarAsync(avatar).Result;

        // ─── Avatar loading ───────────────────────────────────────────────────────

        private async Task<Avatar?> QueryAvatarAsync(string fql)
        {
            var data = await FqlAsync(fql);
            var rows = ParsePage(data);
            if (rows.Count == 0 || GetBool(rows[0], "is_deleted")) return null;
            return Des<Avatar>(GetStr(rows[0], "data_json"));
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var avatar = await QueryAvatarAsync($"oasis_avatars.where(.id_field == {FqlStr(id.ToString())}).first()");
                if (avatar == null) { OASISErrorHandling.HandleError(ref result, $"FaunaOASIS: No avatar for ID '{id}'."); return result; }
                result.Result = avatar; result.IsError = false; result.Message = "FaunaOASIS: Avatar loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"FaunaOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) => LoadAvatarAsync(id, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var avatar = await QueryAvatarAsync($"oasis_avatars.where(.username == {FqlStr(username)} && .is_deleted == false).first()");
                if (avatar == null) { OASISErrorHandling.HandleError(ref result, $"FaunaOASIS: No avatar for username '{username}'."); return result; }
                result.Result = avatar; result.IsError = false; result.Message = "FaunaOASIS: Avatar loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"FaunaOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string username, int version = 0) => LoadAvatarByUsernameAsync(username, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string email, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var avatar = await QueryAvatarAsync($"oasis_avatars.where(.email == {FqlStr(email)} && .is_deleted == false).first()");
                if (avatar == null) { OASISErrorHandling.HandleError(ref result, $"FaunaOASIS: No avatar for email '{email}'."); return result; }
                result.Result = avatar; result.IsError = false; result.Message = "FaunaOASIS: Avatar loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"FaunaOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string email, int version = 0) => LoadAvatarByEmailAsync(email, version).Result;
        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string pk, int version = 0) { if (Guid.TryParse(pk, out Guid id)) return await LoadAvatarAsync(id, version); var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, $"FaunaOASIS: Invalid GUID '{pk}'."); return r; }
        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string pk, int version = 0) => LoadAvatarByProviderKeyAsync(pk, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                var data = await FqlAsync("oasis_avatars.where(.is_deleted == false).toArray()");
                var rows = ParsePage(data);
                var avatars = rows.Select(r => Des<Avatar>(GetStr(r, "data_json"))).Where(a => a != null).Cast<IAvatar>().ToList();
                result.Result = avatars; result.IsError = false; result.Message = $"FaunaOASIS: Loaded {avatars.Count} avatar(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"FaunaOASIS: {ex.Message}"); }
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
                    var loaded = await LoadAvatarAsync(id); if (loaded.IsError || loaded.Result == null) { OASISErrorHandling.HandleError(ref result, $"FaunaOASIS: Avatar '{id}' not found."); return result; }
                    var av = (Avatar)loaded.Result; av.DeletedDate = DateTime.UtcNow;
                    await FqlAsync($"oasis_avatars.where(.id_field == {FqlStr(id.ToString())}).first()?.update({{ is_deleted: true, data_json: {FqlStr(Ser(av))} }})");
                }
                else { await FqlAsync($"oasis_avatars.where(.id_field == {FqlStr(id.ToString())}).first()?.delete()"); }
                result.Result = true; result.IsError = false; result.Message = $"FaunaOASIS: Avatar '{id}' deleted.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"FaunaOASIS: {ex.Message}"); }
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
                var fql = $@"
let existing = oasis_avatar_details.where(.id_field == {FqlStr(d.Id.ToString())}).first()
if (existing != null) {{
  existing.update({{ username: {FqlStr(d.Username)}, email: {FqlStr(d.Email)}, data_json: {FqlStr(Ser(d))} }})
}} else {{
  oasis_avatar_details.create({{ id_field: {FqlStr(d.Id.ToString())}, username: {FqlStr(d.Username)}, email: {FqlStr(d.Email)}, data_json: {FqlStr(Ser(d))} }})
}}";
                await FqlAsync(fql);
                result.Result = d; result.IsError = false; result.Message = "FaunaOASIS: AvatarDetail saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"FaunaOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail d) => SaveAvatarDetailAsync(d).Result;

        private async Task<AvatarDetail?> QueryDetailAsync(string fql) { var data = await FqlAsync(fql); var rows = ParsePage(data); return rows.Count == 0 ? null : Des<AvatarDetail>(GetStr(rows[0], "data_json")); }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try { var d = await QueryDetailAsync($"oasis_avatar_details.where(.id_field == {FqlStr(id.ToString())}).first()"); if (d == null) { OASISErrorHandling.HandleError(ref result, $"FaunaOASIS: No detail for ID '{id}'."); return result; } result.Result = d; result.IsError = false; result.Message = "FaunaOASIS: AvatarDetail loaded."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"FaunaOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => LoadAvatarDetailAsync(id, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string u, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try { var d = await QueryDetailAsync($"oasis_avatar_details.where(.username == {FqlStr(u)}).first()"); if (d == null) { OASISErrorHandling.HandleError(ref result, $"FaunaOASIS: No detail for username '{u}'."); return result; } result.Result = d; result.IsError = false; result.Message = "FaunaOASIS: AvatarDetail loaded."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"FaunaOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string u, int version = 0) => LoadAvatarDetailByUsernameAsync(u, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string e, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try { var d = await QueryDetailAsync($"oasis_avatar_details.where(.email == {FqlStr(e)}).first()"); if (d == null) { OASISErrorHandling.HandleError(ref result, $"FaunaOASIS: No detail for email '{e}'."); return result; } result.Result = d; result.IsError = false; result.Message = "FaunaOASIS: AvatarDetail loaded."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"FaunaOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string e, int version = 0) => LoadAvatarDetailByEmailAsync(e, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            try { var data = await FqlAsync("oasis_avatar_details.all().toArray()"); var rows = ParsePage(data); var details = rows.Select(r => Des<AvatarDetail>(GetStr(r, "data_json"))).Where(d => d != null).Cast<IAvatarDetail>().ToList(); result.Result = details; result.IsError = false; result.Message = $"FaunaOASIS: Loaded {details.Count} detail(s)."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"FaunaOASIS: {ex.Message}"); }
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
                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.FaunaOASIS] = holon.Id.ToString();
                var fql = $@"
let existing = oasis_holons.where(.id_field == {FqlStr(holon.Id.ToString())}).first()
if (existing != null) {{
  existing.update({{ parent_holon_id: {FqlStr(holon.ParentHolonId.ToString())}, holon_type: {(int)holon.HolonType}, is_deleted: {(holon.IsDeleted ? "true" : "false")}, data_json: {FqlStr(Ser(holon))} }})
}} else {{
  oasis_holons.create({{ id_field: {FqlStr(holon.Id.ToString())}, parent_holon_id: {FqlStr(holon.ParentHolonId.ToString())}, holon_type: {(int)holon.HolonType}, is_deleted: false, data_json: {FqlStr(Ser(holon))} }})
}}";
                await FqlAsync(fql);
                result.Result = holon; result.IsError = false; result.Message = $"FaunaOASIS: Holon '{holon.Name}' saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"FaunaOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var saved = new List<IHolon>(); var errors = new List<string>();
            foreach (var h in holons) { var r = await SaveHolonAsync(h, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider); if (r.IsError) errors.Add(r.Message ?? ""); else if (r.Result != null) saved.Add(r.Result); }
            result.Result = saved; result.IsError = errors.Count > 0; result.Message = errors.Count > 0 ? string.Join("; ", errors) : $"FaunaOASIS: {saved.Count} holon(s) saved.";
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;

        // ─── Holon loading ────────────────────────────────────────────────────────

        private async Task<Holon?> QueryHolonAsync(string fql) { var data = await FqlAsync(fql); var rows = ParsePage(data); if (rows.Count == 0 || GetBool(rows[0], "is_deleted")) return null; return Des<Holon>(GetStr(rows[0], "data_json")); }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try { var h = await QueryHolonAsync($"oasis_holons.where(.id_field == {FqlStr(id.ToString())}).first()"); if (h == null) { OASISErrorHandling.HandleError(ref result, $"FaunaOASIS: No holon for ID '{id}'."); return result; } result.Result = h; result.IsError = false; result.Message = "FaunaOASIS: Holon loaded."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"FaunaOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string pk, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) { if (Guid.TryParse(pk, out Guid id)) return await LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version); var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, $"FaunaOASIS: Invalid GUID '{pk}'."); return r; }
        public override OASISResult<IHolon> LoadHolon(string pk, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonAsync(pk, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var fql = type == HolonType.All ? "oasis_holons.where(.is_deleted == false).toArray()" : $"oasis_holons.where(.is_deleted == false && .holon_type == {(int)type}).toArray()";
                var data = await FqlAsync(fql);
                var rows = ParsePage(data);
                var holons = rows.Select(r => Des<Holon>(GetStr(r, "data_json"))).Where(h => h != null).Cast<IHolon>().ToList();
                result.Result = holons; result.IsError = false; result.Message = $"FaunaOASIS: Loaded {holons.Count} holon(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"FaunaOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var fql = type == HolonType.All ? $"oasis_holons.where(.is_deleted == false && .parent_holon_id == {FqlStr(id.ToString())}).toArray()" : $"oasis_holons.where(.is_deleted == false && .parent_holon_id == {FqlStr(id.ToString())} && .holon_type == {(int)type}).toArray()";
                var data = await FqlAsync(fql);
                var rows = ParsePage(data);
                var holons = rows.Select(r => Des<Holon>(GetStr(r, "data_json"))).Where(h => h != null).Cast<IHolon>().ToList();
                result.Result = holons; result.IsError = false; result.Message = $"FaunaOASIS: Loaded {holons.Count} holon(s) for parent '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"FaunaOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string pk, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) { if (Guid.TryParse(pk, out Guid id)) return await LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version); var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, $"FaunaOASIS: Invalid GUID '{pk}'."); return r; }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string pk, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsForParentAsync(pk, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        // ─── Holon deletion ───────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var loaded = await LoadHolonAsync(id); if (loaded.IsError || loaded.Result == null) { OASISErrorHandling.HandleError(ref result, $"FaunaOASIS: Holon '{id}' not found."); return result; }
                var holon = (Holon)loaded.Result; holon.DeletedDate = DateTime.UtcNow;
                await FqlAsync($"oasis_holons.where(.id_field == {FqlStr(id.ToString())}).first()?.update({{ is_deleted: true, data_json: {FqlStr(Ser(holon))} }})");
                result.Result = holon; result.IsError = false; result.Message = $"FaunaOASIS: Holon '{id}' soft-deleted.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"FaunaOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id) => DeleteHolonAsync(id).Result;
        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string pk) { if (Guid.TryParse(pk, out Guid id)) return await DeleteHolonAsync(id); var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, $"FaunaOASIS: Invalid GUID '{pk}'."); return r; }
        public override OASISResult<IHolon> DeleteHolon(string pk) => DeleteHolonAsync(pk).Result;

        // ─── Search + Metadata ────────────────────────────────────────────────────

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            try { string? q = searchParams.SearchGroups?.OfType<SearchTextGroup>().FirstOrDefault()?.SearchQuery?.ToLower(); var all = await LoadAllHolonsAsync(); var holons = all.Result?.ToList() ?? new List<IHolon>(); if (!string.IsNullOrEmpty(q)) holons = holons.Where(h => h.Name?.ToLower().Contains(q) == true || h.Description?.ToLower().Contains(q) == true).ToList(); result.Result = new SearchResults { SearchResultHolons = holons, NumberOfResults = holons.Count }; result.IsError = false; result.Message = $"FaunaOASIS: Found {holons.Count} result(s)."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        { var all = await LoadAllHolonsAsync(type); var holons = all.Result?.Where(h => h.MetaData != null && h.MetaData.TryGetValue(metaKey, out var v) && v?.ToString() == metaValue).ToList() ?? new List<IHolon>(); return new OASISResult<IEnumerable<IHolon>> { Result = holons, IsError = false, Message = $"FaunaOASIS: {holons.Count} holon(s)." }; }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKvp, MetaKeyValuePairMatchMode mode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        { var all = await LoadAllHolonsAsync(type); bool IsMatch(IHolon h) { if (h.MetaData == null) return false; var checks = metaKvp.Select(kvp => h.MetaData.TryGetValue(kvp.Key, out var v) && v?.ToString() == kvp.Value); return mode == MetaKeyValuePairMatchMode.Any ? checks.Any(c => c) : checks.All(c => c); } var holons = all.Result?.Where(IsMatch).ToList() ?? new List<IHolon>(); return new OASISResult<IEnumerable<IHolon>> { Result = holons, IsError = false, Message = $"FaunaOASIS: {holons.Count} holon(s)." }; }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKvp, MetaKeyValuePairMatchMode mode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsByMetaDataAsync(metaKvp, mode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        // ─── Import / Export ──────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons) { var s = await SaveHolonsAsync(holons); return new OASISResult<bool> { Result = !s.IsError, IsError = s.IsError, Message = s.Message }; }
        public override OASISResult<bool> Import(IEnumerable<IHolon> holons) => ImportAsync(holons).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0) => await LoadAllHolonsAsync();
        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0) => ExportAllAsync(version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0) { var all = await LoadAllHolonsAsync(); var h = all.Result?.Where(x => x.CreatedByAvatarId == avatarId).ToList() ?? new List<IHolon>(); return new OASISResult<IEnumerable<IHolon>> { Result = h, IsError = false, Message = $"FaunaOASIS: {h.Count} holon(s)." }; }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0) => ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string u, int version = 0) { var a = await LoadAvatarByUsernameAsync(u); if (a.IsError || a.Result == null) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "Not found."); return r; } return await ExportAllDataForAvatarByIdAsync(a.Result.Id, version); }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string u, int version = 0) => ExportAllDataForAvatarByUsernameAsync(u, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string e, int version = 0) { var a = await LoadAvatarByEmailAsync(e); if (a.IsError || a.Result == null) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "Not found."); return r; } return await ExportAllDataForAvatarByIdAsync(a.Result.Id, version); }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string e, int version = 0) => ExportAllDataForAvatarByEmailAsync(e, version).Result;

        public bool IsVersionControlEnabled { get; set; } = false;
    }
}
