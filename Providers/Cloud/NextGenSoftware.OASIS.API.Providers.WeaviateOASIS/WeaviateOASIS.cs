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

namespace NextGenSoftware.OASIS.API.Providers.WeaviateOASIS
{
    /// <summary>
    /// OASIS provider for Weaviate — AI-native vector database, accessed via Weaviate REST + GraphQL APIs.
    ///
    /// Classes created on ActivateProvider:
    ///   OasisAvatar, OasisAvatarDetail, OasisHolon
    ///
    /// Each object stores indexed properties (username, email, isDeleted, parentHolonId, holonType)
    /// plus a dataJson string holding the full serialised OASIS object.
    ///
    /// Constructor parameters:
    ///   host   — Weaviate host, e.g. "http://localhost:8080" or "https://xyz.weaviate.network"
    ///   apiKey — optional Weaviate API key (required for Weaviate Cloud)
    /// </summary>
    public class WeaviateOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISDBStorageProvider
    {
        private readonly string _host;
        private readonly HttpClient _http;

        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public WeaviateOASIS(string host, string? apiKey = null)
        {
            _host = host.TrimEnd('/');
            _http = new HttpClient();
            if (!string.IsNullOrEmpty(apiKey))
                _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            ProviderName = "WeaviateOASIS";
            ProviderDescription = "Weaviate AI-native vector database provider (REST/GraphQL — semantic search over OASIS holons)";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.WeaviateOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageCloud);
        }

        private static string Ser(object o) => JsonSerializer.Serialize(o, _jsonOpts);
        private static T? Des<T>(string? json) => json == null ? default : JsonSerializer.Deserialize<T>(json, _jsonOpts);

        // ─── REST helpers ─────────────────────────────────────────────────────────

        private async Task<JsonElement> WeaviateGetAsync(string path)
        {
            var resp = await _http.GetAsync($"{_host}{path}");
            var raw = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode && resp.StatusCode != System.Net.HttpStatusCode.NotFound)
                throw new Exception($"Weaviate GET {path} → {(int)resp.StatusCode}: {raw}");
            using var doc = JsonDocument.Parse(raw);
            return doc.RootElement.Clone();
        }

        private async Task<JsonElement> WeaviatePostAsync(string path, object body)
        {
            var content = new StringContent(Ser(body), Encoding.UTF8, "application/json");
            var resp = await _http.PostAsync($"{_host}{path}", content);
            var raw = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) throw new Exception($"Weaviate POST {path} → {(int)resp.StatusCode}: {raw}");
            using var doc = JsonDocument.Parse(raw);
            return doc.RootElement.Clone();
        }

        private async Task WeaviatePutAsync(string path, object body)
        {
            var content = new StringContent(Ser(body), Encoding.UTF8, "application/json");
            var resp = await _http.PutAsync($"{_host}{path}", content);
            var raw = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) throw new Exception($"Weaviate PUT {path} → {(int)resp.StatusCode}: {raw}");
        }

        private async Task WeaviateDeleteAsync(string path)
        {
            var resp = await _http.DeleteAsync($"{_host}{path}");
            if (!resp.IsSuccessStatusCode && resp.StatusCode != System.Net.HttpStatusCode.NotFound)
                throw new Exception($"Weaviate DELETE {path} → {(int)resp.StatusCode}");
        }

        // ─── Schema helpers ───────────────────────────────────────────────────────

        private async Task EnsureClassAsync(string className, object[] properties)
        {
            var schema = await WeaviateGetAsync($"/v1/schema/{className}");
            if (schema.TryGetProperty("class", out _)) return; // already exists
            await WeaviatePostAsync("/v1/schema", new
            {
                @class = className,
                vectorizer = "none",
                properties
            });
        }

        // ─── Object CRUD ──────────────────────────────────────────────────────────

        private async Task UpsertObjectAsync(string className, string id, Dictionary<string, object?> props)
        {
            // Weaviate uses deterministic UUIDs for upsert; PUT /v1/objects/{class}/{id} creates or replaces
            await WeaviatePutAsync($"/v1/objects/{className}/{id}", new { @class = className, id, properties = props });
        }

        private async Task<JsonElement?> GetObjectAsync(string className, string id)
        {
            var result = await WeaviateGetAsync($"/v1/objects/{className}/{id}?include=vector");
            if (result.TryGetProperty("error", out _)) return null;
            if (!result.TryGetProperty("id", out _)) return null;
            return result;
        }

        private string? GetProp(JsonElement obj, string key)
        {
            if (obj.TryGetProperty("properties", out var props) && props.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String)
                return v.GetString();
            return null;
        }

        private bool GetPropBool(JsonElement obj, string key)
        {
            if (obj.TryGetProperty("properties", out var props) && props.TryGetProperty(key, out var v))
                return v.ValueKind == JsonValueKind.True;
            return false;
        }

        private async Task<List<JsonElement>> GraphQlGetAsync(string className, string[] fields, string? whereClause = null)
        {
            var where = whereClause != null ? $", where: {{ {whereClause} }}" : "";
            var query = $"{{ Get {{ {className}(limit: 10000{where}) {{ {string.Join(" ", fields)} _additional {{ id }} }} }} }}";
            var result = await WeaviatePostAsync("/v1/graphql", new { query });
            var objects = new List<JsonElement>();
            if (result.TryGetProperty("data", out var data) && data.TryGetProperty("Get", out var get) && get.TryGetProperty(className, out var arr) && arr.ValueKind == JsonValueKind.Array)
                foreach (var o in arr.EnumerateArray()) objects.Add(o.Clone());
            return objects;
        }

        private string? GetGraphQlId(JsonElement o) { if (o.TryGetProperty("_additional", out var a) && a.TryGetProperty("id", out var id)) return id.GetString(); return null; }
        private string? GetGraphQlStr(JsonElement o, string key) { if (o.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String) return v.GetString(); return null; }
        private bool GetGraphQlBool(JsonElement o, string key) { if (o.TryGetProperty(key, out var v)) return v.ValueKind == JsonValueKind.True; return false; }

        // ─── Activation ───────────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                var strProp = new Func<string, string, object>((name, desc) => new { name, dataType = new[] { "text" }, description = desc });
                var boolProp = new Func<string, string, object>((name, desc) => new { name, dataType = new[] { "boolean" }, description = desc });
                var intProp = new Func<string, string, object>((name, desc) => new { name, dataType = new[] { "int" }, description = desc });

                await EnsureClassAsync("OasisAvatar", new object[]
                {
                    strProp("username", "Avatar username"), strProp("email", "Avatar email"),
                    boolProp("isDeleted", "Soft-delete flag"), strProp("dataJson", "Full serialised avatar")
                });
                await EnsureClassAsync("OasisAvatarDetail", new object[]
                {
                    strProp("username", "Avatar username"), strProp("email", "Avatar email"), strProp("dataJson", "Full serialised detail")
                });
                await EnsureClassAsync("OasisHolon", new object[]
                {
                    strProp("parentHolonId", "Parent holon UUID"), intProp("holonType", "HolonType enum value"),
                    boolProp("isDeleted", "Soft-delete flag"), strProp("dataJson", "Full serialised holon")
                });
                result.Result = true; result.IsError = false; result.Message = "WeaviateOASIS activated — schema ready.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"WeaviateOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;
        public override async Task<OASISResult<bool>> DeActivateProviderAsync() => await Task.FromResult(new OASISResult<bool> { Result = true, IsError = false, Message = "WeaviateOASIS deactivated." });
        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // ─── Avatar saving ────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (avatar.Id == Guid.Empty) avatar.Id = Guid.NewGuid();
                if (avatar.ProviderUniqueStorageKey == null) avatar.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.WeaviateOASIS] = avatar.Id.ToString();
                await UpsertObjectAsync("OasisAvatar", avatar.Id.ToString(), new Dictionary<string, object?>
                {
                    ["username"] = avatar.Username, ["email"] = avatar.Email, ["isDeleted"] = avatar.IsDeleted, ["dataJson"] = Ser(avatar)
                });
                result.Result = avatar; result.IsError = false; result.Message = $"WeaviateOASIS: Avatar '{avatar.Username}' saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"WeaviateOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) => SaveAvatarAsync(avatar).Result;

        // ─── Avatar loading ───────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var obj = await GetObjectAsync("OasisAvatar", id.ToString());
                if (obj == null || GetPropBool(obj.Value, "isDeleted")) { OASISErrorHandling.HandleError(ref result, $"WeaviateOASIS: No avatar for ID '{id}'."); return result; }
                var avatar = Des<Avatar>(GetProp(obj.Value, "dataJson")); if (avatar == null) { OASISErrorHandling.HandleError(ref result, "WeaviateOASIS: Deserialise failed."); return result; }
                result.Result = avatar; result.IsError = false; result.Message = "WeaviateOASIS: Avatar loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"WeaviateOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) => LoadAvatarAsync(id, version).Result;

        private async Task<Avatar?> ScanAvatarsAsync(Func<JsonElement, bool> pred)
        {
            var objs = await GraphQlGetAsync("OasisAvatar", new[] { "username", "email", "isDeleted", "dataJson" });
            var match = objs.FirstOrDefault(o => !GetGraphQlBool(o, "isDeleted") && pred(o));
            if (match.ValueKind == JsonValueKind.Undefined) return null;
            return Des<Avatar>(GetGraphQlStr(match, "dataJson"));
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try { var a = await ScanAvatarsAsync(o => GetGraphQlStr(o, "username") == username); if (a == null) { OASISErrorHandling.HandleError(ref result, $"WeaviateOASIS: No avatar for username '{username}'."); return result; } result.Result = a; result.IsError = false; result.Message = "WeaviateOASIS: Avatar loaded."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"WeaviateOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string username, int version = 0) => LoadAvatarByUsernameAsync(username, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string email, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try { var a = await ScanAvatarsAsync(o => GetGraphQlStr(o, "email") == email); if (a == null) { OASISErrorHandling.HandleError(ref result, $"WeaviateOASIS: No avatar for email '{email}'."); return result; } result.Result = a; result.IsError = false; result.Message = "WeaviateOASIS: Avatar loaded."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"WeaviateOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string email, int version = 0) => LoadAvatarByEmailAsync(email, version).Result;
        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string pk, int version = 0) { if (Guid.TryParse(pk, out Guid id)) return await LoadAvatarAsync(id, version); var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, $"WeaviateOASIS: Invalid GUID '{pk}'."); return r; }
        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string pk, int version = 0) => LoadAvatarByProviderKeyAsync(pk, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                var objs = await GraphQlGetAsync("OasisAvatar", new[] { "isDeleted", "dataJson" });
                var avatars = objs.Where(o => !GetGraphQlBool(o, "isDeleted")).Select(o => Des<Avatar>(GetGraphQlStr(o, "dataJson"))).Where(a => a != null).Cast<IAvatar>().ToList();
                result.Result = avatars; result.IsError = false; result.Message = $"WeaviateOASIS: Loaded {avatars.Count} avatar(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"WeaviateOASIS: {ex.Message}"); }
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
                    var loaded = await LoadAvatarAsync(id); if (loaded.IsError || loaded.Result == null) { OASISErrorHandling.HandleError(ref result, $"WeaviateOASIS: Avatar '{id}' not found."); return result; }
                    var av = (Avatar)loaded.Result; av.DeletedDate = DateTime.UtcNow;
                    await UpsertObjectAsync("OasisAvatar", id.ToString(), new Dictionary<string, object?> { ["username"] = av.Username, ["email"] = av.Email, ["isDeleted"] = true, ["dataJson"] = Ser(av) });
                }
                else { await WeaviateDeleteAsync($"/v1/objects/OasisAvatar/{id}"); }
                result.Result = true; result.IsError = false; result.Message = $"WeaviateOASIS: Avatar '{id}' deleted.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"WeaviateOASIS: {ex.Message}"); }
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
                await UpsertObjectAsync("OasisAvatarDetail", d.Id.ToString(), new Dictionary<string, object?> { ["username"] = d.Username, ["email"] = d.Email, ["dataJson"] = Ser(d) });
                result.Result = d; result.IsError = false; result.Message = "WeaviateOASIS: AvatarDetail saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"WeaviateOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail d) => SaveAvatarDetailAsync(d).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var obj = await GetObjectAsync("OasisAvatarDetail", id.ToString());
                if (obj == null) { OASISErrorHandling.HandleError(ref result, $"WeaviateOASIS: No detail for ID '{id}'."); return result; }
                var d = Des<AvatarDetail>(GetProp(obj.Value, "dataJson")); if (d == null) { OASISErrorHandling.HandleError(ref result, "WeaviateOASIS: Deserialise failed."); return result; }
                result.Result = d; result.IsError = false; result.Message = "WeaviateOASIS: AvatarDetail loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"WeaviateOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => LoadAvatarDetailAsync(id, version).Result;

        private async Task<AvatarDetail?> ScanDetailsAsync(Func<JsonElement, bool> pred) { var objs = await GraphQlGetAsync("OasisAvatarDetail", new[] { "username", "email", "dataJson" }); var m = objs.FirstOrDefault(pred); return m.ValueKind == JsonValueKind.Undefined ? null : Des<AvatarDetail>(GetGraphQlStr(m, "dataJson")); }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string u, int version = 0) { var result = new OASISResult<IAvatarDetail>(); try { var d = await ScanDetailsAsync(o => GetGraphQlStr(o, "username") == u); if (d == null) { OASISErrorHandling.HandleError(ref result, $"WeaviateOASIS: No detail for username '{u}'."); return result; } result.Result = d; result.IsError = false; result.Message = "WeaviateOASIS: AvatarDetail loaded."; } catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"WeaviateOASIS: {ex.Message}"); } return result; }
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string u, int version = 0) => LoadAvatarDetailByUsernameAsync(u, version).Result;
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string e, int version = 0) { var result = new OASISResult<IAvatarDetail>(); try { var d = await ScanDetailsAsync(o => GetGraphQlStr(o, "email") == e); if (d == null) { OASISErrorHandling.HandleError(ref result, $"WeaviateOASIS: No detail for email '{e}'."); return result; } result.Result = d; result.IsError = false; result.Message = "WeaviateOASIS: AvatarDetail loaded."; } catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"WeaviateOASIS: {ex.Message}"); } return result; }
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string e, int version = 0) => LoadAvatarDetailByEmailAsync(e, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            try { var objs = await GraphQlGetAsync("OasisAvatarDetail", new[] { "dataJson" }); var details = objs.Select(o => Des<AvatarDetail>(GetGraphQlStr(o, "dataJson"))).Where(d => d != null).Cast<IAvatarDetail>().ToList(); result.Result = details; result.IsError = false; result.Message = $"WeaviateOASIS: Loaded {details.Count} detail(s)."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"WeaviateOASIS: {ex.Message}"); }
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
                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.WeaviateOASIS] = holon.Id.ToString();
                await UpsertObjectAsync("OasisHolon", holon.Id.ToString(), new Dictionary<string, object?>
                {
                    ["parentHolonId"] = holon.ParentHolonId.ToString(), ["holonType"] = (int)holon.HolonType,
                    ["isDeleted"] = holon.IsDeleted, ["dataJson"] = Ser(holon)
                });
                result.Result = holon; result.IsError = false; result.Message = $"WeaviateOASIS: Holon '{holon.Name}' saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"WeaviateOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var saved = new List<IHolon>(); var errors = new List<string>();
            foreach (var h in holons) { var r = await SaveHolonAsync(h, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider); if (r.IsError) errors.Add(r.Message ?? ""); else if (r.Result != null) saved.Add(r.Result); }
            result.Result = saved; result.IsError = errors.Count > 0; result.Message = errors.Count > 0 ? string.Join("; ", errors) : $"WeaviateOASIS: {saved.Count} holon(s) saved.";
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;

        // ─── Holon loading ────────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var obj = await GetObjectAsync("OasisHolon", id.ToString());
                if (obj == null || GetPropBool(obj.Value, "isDeleted")) { OASISErrorHandling.HandleError(ref result, $"WeaviateOASIS: No holon for ID '{id}'."); return result; }
                var holon = Des<Holon>(GetProp(obj.Value, "dataJson")); if (holon == null) { OASISErrorHandling.HandleError(ref result, "WeaviateOASIS: Deserialise failed."); return result; }
                result.Result = holon; result.IsError = false; result.Message = "WeaviateOASIS: Holon loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"WeaviateOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string pk, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) { if (Guid.TryParse(pk, out Guid id)) return await LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version); var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, $"WeaviateOASIS: Invalid GUID '{pk}'."); return r; }
        public override OASISResult<IHolon> LoadHolon(string pk, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonAsync(pk, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var objs = await GraphQlGetAsync("OasisHolon", new[] { "isDeleted", "holonType", "dataJson" });
                var holons = objs.Where(o => !GetGraphQlBool(o, "isDeleted") && (type == HolonType.All || GetGraphQlInt(o, "holonType") == (int)type)).Select(o => Des<Holon>(GetGraphQlStr(o, "dataJson"))).Where(h => h != null).Cast<IHolon>().ToList();
                result.Result = holons; result.IsError = false; result.Message = $"WeaviateOASIS: Loaded {holons.Count} holon(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"WeaviateOASIS: {ex.Message}"); }
            return result;
        }

        private int GetGraphQlInt(JsonElement o, string key) { if (o.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.Number) return v.GetInt32(); return 0; }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var idStr = id.ToString();
                var objs = await GraphQlGetAsync("OasisHolon", new[] { "parentHolonId", "isDeleted", "holonType", "dataJson" });
                var holons = objs.Where(o => !GetGraphQlBool(o, "isDeleted") && GetGraphQlStr(o, "parentHolonId") == idStr && (type == HolonType.All || GetGraphQlInt(o, "holonType") == (int)type)).Select(o => Des<Holon>(GetGraphQlStr(o, "dataJson"))).Where(h => h != null).Cast<IHolon>().ToList();
                result.Result = holons; result.IsError = false; result.Message = $"WeaviateOASIS: Loaded {holons.Count} holon(s) for parent '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"WeaviateOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string pk, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) { if (Guid.TryParse(pk, out Guid id)) return await LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version); var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, $"WeaviateOASIS: Invalid GUID '{pk}'."); return r; }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string pk, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsForParentAsync(pk, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        // ─── Holon deletion ───────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var loaded = await LoadHolonAsync(id); if (loaded.IsError || loaded.Result == null) { OASISErrorHandling.HandleError(ref result, $"WeaviateOASIS: Holon '{id}' not found."); return result; }
                var holon = (Holon)loaded.Result; holon.DeletedDate = DateTime.UtcNow;
                await UpsertObjectAsync("OasisHolon", id.ToString(), new Dictionary<string, object?> { ["parentHolonId"] = holon.ParentHolonId.ToString(), ["holonType"] = (int)holon.HolonType, ["isDeleted"] = true, ["dataJson"] = Ser(holon) });
                result.Result = holon; result.IsError = false; result.Message = $"WeaviateOASIS: Holon '{id}' soft-deleted.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"WeaviateOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id) => DeleteHolonAsync(id).Result;
        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string pk) { if (Guid.TryParse(pk, out Guid id)) return await DeleteHolonAsync(id); var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, $"WeaviateOASIS: Invalid GUID '{pk}'."); return r; }
        public override OASISResult<IHolon> DeleteHolon(string pk) => DeleteHolonAsync(pk).Result;

        // ─── Search ───────────────────────────────────────────────────────────────

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            try { string? q = searchParams.SearchGroups?.OfType<SearchTextGroup>().FirstOrDefault()?.SearchQuery?.ToLower(); var all = await LoadAllHolonsAsync(); var holons = all.Result?.ToList() ?? new List<IHolon>(); if (!string.IsNullOrEmpty(q)) holons = holons.Where(h => h.Name?.ToLower().Contains(q) == true || h.Description?.ToLower().Contains(q) == true).ToList(); result.Result = new SearchResults { SearchResultHolons = holons, NumberOfResults = holons.Count }; result.IsError = false; result.Message = $"WeaviateOASIS: Found {holons.Count} result(s)."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        { var all = await LoadAllHolonsAsync(type); var holons = all.Result?.Where(h => h.MetaData != null && h.MetaData.TryGetValue(metaKey, out var v) && v?.ToString() == metaValue).ToList() ?? new List<IHolon>(); return new OASISResult<IEnumerable<IHolon>> { Result = holons, IsError = false, Message = $"WeaviateOASIS: {holons.Count} holon(s)." }; }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKvp, MetaKeyValuePairMatchMode mode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        { var all = await LoadAllHolonsAsync(type); bool IsMatch(IHolon h) { if (h.MetaData == null) return false; var checks = metaKvp.Select(kvp => h.MetaData.TryGetValue(kvp.Key, out var v) && v?.ToString() == kvp.Value); return mode == MetaKeyValuePairMatchMode.Any ? checks.Any(c => c) : checks.All(c => c); } var holons = all.Result?.Where(IsMatch).ToList() ?? new List<IHolon>(); return new OASISResult<IEnumerable<IHolon>> { Result = holons, IsError = false, Message = $"WeaviateOASIS: {holons.Count} holon(s)." }; }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKvp, MetaKeyValuePairMatchMode mode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsByMetaDataAsync(metaKvp, mode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        // ─── Import / Export ──────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons) { var s = await SaveHolonsAsync(holons); return new OASISResult<bool> { Result = !s.IsError, IsError = s.IsError, Message = s.Message }; }
        public override OASISResult<bool> Import(IEnumerable<IHolon> holons) => ImportAsync(holons).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0) => await LoadAllHolonsAsync();
        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0) => ExportAllAsync(version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0) { var all = await LoadAllHolonsAsync(); var h = all.Result?.Where(x => x.CreatedByAvatarId == avatarId).ToList() ?? new List<IHolon>(); return new OASISResult<IEnumerable<IHolon>> { Result = h, IsError = false, Message = $"WeaviateOASIS: {h.Count} holon(s)." }; }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0) => ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string u, int version = 0) { var a = await LoadAvatarByUsernameAsync(u); if (a.IsError || a.Result == null) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "Not found."); return r; } return await ExportAllDataForAvatarByIdAsync(a.Result.Id, version); }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string u, int version = 0) => ExportAllDataForAvatarByUsernameAsync(u, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string e, int version = 0) { var a = await LoadAvatarByEmailAsync(e); if (a.IsError || a.Result == null) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "Not found."); return r; } return await ExportAllDataForAvatarByIdAsync(a.Result.Id, version); }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string e, int version = 0) => ExportAllDataForAvatarByEmailAsync(e, version).Result;

        public bool IsVersionControlEnabled { get; set; } = false;
    }
}
