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

namespace NextGenSoftware.OASIS.API.Providers.QdrantOASIS
{
    /// <summary>
    /// OASIS provider for Qdrant — high-performance vector database accessed via Qdrant REST API.
    ///
    /// Qdrant stores OASIS data as points in three collections:
    ///   oasis_avatars, oasis_avatar_details, oasis_holons
    ///
    /// Each point carries:
    ///   - id: UUID (OASIS object ID)
    ///   - payload: { username, email, is_deleted, data_json, holon_type, parent_holon_id }
    ///   - vector: zero-vector placeholder (real embeddings can be injected externally)
    ///
    /// Constructor parameters:
    ///   host    — Qdrant host URL (e.g. "http://localhost:6333" or "https://xyz.cloud.qdrant.io:6333")
    ///   apiKey  — optional Qdrant API key (required for Qdrant Cloud)
    ///   vectorSize — dimension of placeholder vectors (default 128)
    /// </summary>
    public class QdrantOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISDBStorageProvider
    {
        private readonly string _host;
        private readonly HttpClient _http;
        private readonly int _vectorSize;

        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public QdrantOASIS(string host, string? apiKey = null, int vectorSize = 128)
        {
            _host = host.TrimEnd('/');
            _vectorSize = vectorSize;
            _http = new HttpClient();
            if (!string.IsNullOrEmpty(apiKey))
                _http.DefaultRequestHeaders.Add("api-key", apiKey);
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            ProviderName = "QdrantOASIS";
            ProviderDescription = "Qdrant vector database provider (REST API — semantic search over OASIS holons)";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.QdrantOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageCloud);
        }

        private static string Ser(object o) => JsonSerializer.Serialize(o, _jsonOpts);
        private static T? Des<T>(string? json) => json == null ? default : JsonSerializer.Deserialize<T>(json, _jsonOpts);

        // ─── Qdrant REST helpers ──────────────────────────────────────────────────

        private async Task<JsonElement> QdrantGetAsync(string path)
        {
            var resp = await _http.GetAsync($"{_host}{path}");
            var raw = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode && resp.StatusCode != System.Net.HttpStatusCode.NotFound)
                throw new Exception($"Qdrant GET {path} returned {(int)resp.StatusCode}: {raw}");
            using var doc = JsonDocument.Parse(raw);
            return doc.RootElement.Clone();
        }

        private async Task<JsonElement> QdrantPostAsync(string path, object body)
        {
            var content = new StringContent(Ser(body), Encoding.UTF8, "application/json");
            var resp = await _http.PostAsync($"{_host}{path}", content);
            var raw = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) throw new Exception($"Qdrant POST {path} returned {(int)resp.StatusCode}: {raw}");
            using var doc = JsonDocument.Parse(raw);
            return doc.RootElement.Clone();
        }

        private async Task QdrantPutAsync(string path, object body)
        {
            var content = new StringContent(Ser(body), Encoding.UTF8, "application/json");
            var resp = await _http.PutAsync($"{_host}{path}", content);
            var raw = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) throw new Exception($"Qdrant PUT {path} returned {(int)resp.StatusCode}: {raw}");
        }

        private async Task QdrantDeleteAsync(string path)
        {
            var resp = await _http.DeleteAsync($"{_host}{path}");
            if (!resp.IsSuccessStatusCode && resp.StatusCode != System.Net.HttpStatusCode.NotFound)
                throw new Exception($"Qdrant DELETE {path} returned {(int)resp.StatusCode}");
        }

        private float[] ZeroVector() => new float[_vectorSize];

        private async Task EnsureCollectionAsync(string name)
        {
            var info = await QdrantGetAsync($"/collections/{name}");
            if (info.TryGetProperty("status", out var st) && st.GetString() == "ok") return;
            await QdrantPutAsync($"/collections/{name}", new
            {
                vectors = new { size = _vectorSize, distance = "Cosine" }
            });
        }

        private async Task UpsertPointAsync(string collection, string id, Dictionary<string, object?> payload)
        {
            await QdrantPostAsync($"/collections/{collection}/points?wait=true", new
            {
                points = new[] { new { id, vector = ZeroVector(), payload } }
            });
        }

        private async Task<JsonElement?> GetPointAsync(string collection, string id)
        {
            var result = await QdrantGetAsync($"/collections/{collection}/points/{id}");
            if (result.TryGetProperty("status", out var st) && st.GetString() == "ok"
                && result.TryGetProperty("result", out var r) && r.ValueKind != JsonValueKind.Null)
                return r;
            return null;
        }

        private async Task<List<JsonElement>> ScrollAsync(string collection, Dictionary<string, object?>? filter = null, int limit = 10000)
        {
            var body = new Dictionary<string, object?> { ["limit"] = limit, ["with_payload"] = true, ["with_vector"] = false };
            if (filter != null) body["filter"] = new { must = filter.Select(kvp => new { key = kvp.Key, match = new { value = kvp.Value } }).ToArray() };
            var result = await QdrantPostAsync($"/collections/{collection}/points/scroll", body);
            var points = new List<JsonElement>();
            if (result.TryGetProperty("result", out var r) && r.TryGetProperty("points", out var arr) && arr.ValueKind == JsonValueKind.Array)
                foreach (var p in arr.EnumerateArray()) points.Add(p.Clone());
            return points;
        }

        private string? GetPayloadStr(JsonElement point, string key)
        {
            if (point.TryGetProperty("payload", out var pl) && pl.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String)
                return v.GetString();
            return null;
        }

        private bool GetPayloadBool(JsonElement point, string key)
        {
            if (point.TryGetProperty("payload", out var pl) && pl.TryGetProperty(key, out var v))
                return v.ValueKind == JsonValueKind.True;
            return false;
        }

        // ─── Activation ───────────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                await EnsureCollectionAsync("oasis_avatars");
                await EnsureCollectionAsync("oasis_avatar_details");
                await EnsureCollectionAsync("oasis_holons");
                result.Result = true; result.IsError = false; result.Message = "QdrantOASIS activated — collections ready.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"QdrantOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;
        public override async Task<OASISResult<bool>> DeActivateProviderAsync() => await Task.FromResult(new OASISResult<bool> { Result = true, IsError = false, Message = "QdrantOASIS deactivated." });
        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // ─── Avatar saving ────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (avatar.Id == Guid.Empty) avatar.Id = Guid.NewGuid();
                if (avatar.ProviderUniqueStorageKey == null) avatar.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.QdrantOASIS] = avatar.Id.ToString();
                await UpsertPointAsync("oasis_avatars", avatar.Id.ToString(), new Dictionary<string, object?>
                {
                    ["username"] = avatar.Username,
                    ["email"] = avatar.Email,
                    ["is_deleted"] = avatar.IsDeleted,
                    ["data_json"] = Ser(avatar)
                });
                result.Result = avatar; result.IsError = false; result.Message = $"QdrantOASIS: Avatar '{avatar.Username}' saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"QdrantOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) => SaveAvatarAsync(avatar).Result;

        // ─── Avatar loading ───────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var point = await GetPointAsync("oasis_avatars", id.ToString());
                if (point == null || GetPayloadBool(point.Value, "is_deleted")) { OASISErrorHandling.HandleError(ref result, $"QdrantOASIS: No avatar for ID '{id}'."); return result; }
                var avatar = Des<Avatar>(GetPayloadStr(point.Value, "data_json")); if (avatar == null) { OASISErrorHandling.HandleError(ref result, "QdrantOASIS: Deserialise failed."); return result; }
                result.Result = avatar; result.IsError = false; result.Message = "QdrantOASIS: Avatar loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"QdrantOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) => LoadAvatarAsync(id, version).Result;

        private async Task<Avatar?> ScanAvatarsAsync(Func<JsonElement, bool> predicate)
        {
            var points = await ScrollAsync("oasis_avatars");
            var p = points.FirstOrDefault(pt => !GetPayloadBool(pt, "is_deleted") && predicate(pt));
            if (p.ValueKind == JsonValueKind.Undefined) return null;
            return Des<Avatar>(GetPayloadStr(p, "data_json"));
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try { var a = await ScanAvatarsAsync(p => GetPayloadStr(p, "username") == username); if (a == null) { OASISErrorHandling.HandleError(ref result, $"QdrantOASIS: No avatar for username '{username}'."); return result; } result.Result = a; result.IsError = false; result.Message = "QdrantOASIS: Avatar loaded."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"QdrantOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string username, int version = 0) => LoadAvatarByUsernameAsync(username, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string email, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try { var a = await ScanAvatarsAsync(p => GetPayloadStr(p, "email") == email); if (a == null) { OASISErrorHandling.HandleError(ref result, $"QdrantOASIS: No avatar for email '{email}'."); return result; } result.Result = a; result.IsError = false; result.Message = "QdrantOASIS: Avatar loaded."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"QdrantOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string email, int version = 0) => LoadAvatarByEmailAsync(email, version).Result;
        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string pk, int version = 0) { if (Guid.TryParse(pk, out Guid id)) return await LoadAvatarAsync(id, version); var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, $"QdrantOASIS: Invalid GUID '{pk}'."); return r; }
        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string pk, int version = 0) => LoadAvatarByProviderKeyAsync(pk, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                var points = await ScrollAsync("oasis_avatars");
                var avatars = points.Where(p => !GetPayloadBool(p, "is_deleted")).Select(p => Des<Avatar>(GetPayloadStr(p, "data_json"))).Where(a => a != null).Cast<IAvatar>().ToList();
                result.Result = avatars; result.IsError = false; result.Message = $"QdrantOASIS: Loaded {avatars.Count} avatar(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"QdrantOASIS: {ex.Message}"); }
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
                    var loaded = await LoadAvatarAsync(id); if (loaded.IsError || loaded.Result == null) { OASISErrorHandling.HandleError(ref result, $"QdrantOASIS: Avatar '{id}' not found."); return result; }
                    var av = (Avatar)loaded.Result; av.DeletedDate = DateTime.UtcNow;
                    await UpsertPointAsync("oasis_avatars", id.ToString(), new Dictionary<string, object?> { ["username"] = av.Username, ["email"] = av.Email, ["is_deleted"] = true, ["data_json"] = Ser(av) });
                }
                else { await QdrantPostAsync($"/collections/oasis_avatars/points/delete?wait=true", new { points = new[] { id.ToString() } }); }
                result.Result = true; result.IsError = false; result.Message = $"QdrantOASIS: Avatar '{id}' deleted.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"QdrantOASIS: {ex.Message}"); }
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
                await UpsertPointAsync("oasis_avatar_details", d.Id.ToString(), new Dictionary<string, object?> { ["username"] = d.Username, ["email"] = d.Email, ["data_json"] = Ser(d) });
                result.Result = d; result.IsError = false; result.Message = "QdrantOASIS: AvatarDetail saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"QdrantOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail d) => SaveAvatarDetailAsync(d).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var point = await GetPointAsync("oasis_avatar_details", id.ToString());
                if (point == null) { OASISErrorHandling.HandleError(ref result, $"QdrantOASIS: No detail for ID '{id}'."); return result; }
                var d = Des<AvatarDetail>(GetPayloadStr(point.Value, "data_json")); if (d == null) { OASISErrorHandling.HandleError(ref result, "QdrantOASIS: Deserialise failed."); return result; }
                result.Result = d; result.IsError = false; result.Message = "QdrantOASIS: AvatarDetail loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"QdrantOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => LoadAvatarDetailAsync(id, version).Result;

        private async Task<AvatarDetail?> ScanDetailsAsync(Func<JsonElement, bool> pred) { var pts = await ScrollAsync("oasis_avatar_details"); var p = pts.FirstOrDefault(pred); return p.ValueKind == JsonValueKind.Undefined ? null : Des<AvatarDetail>(GetPayloadStr(p, "data_json")); }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string u, int version = 0) { var result = new OASISResult<IAvatarDetail>(); try { var d = await ScanDetailsAsync(p => GetPayloadStr(p, "username") == u); if (d == null) { OASISErrorHandling.HandleError(ref result, $"QdrantOASIS: No detail for username '{u}'."); return result; } result.Result = d; result.IsError = false; result.Message = "QdrantOASIS: AvatarDetail loaded."; } catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"QdrantOASIS: {ex.Message}"); } return result; }
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string u, int version = 0) => LoadAvatarDetailByUsernameAsync(u, version).Result;
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string e, int version = 0) { var result = new OASISResult<IAvatarDetail>(); try { var d = await ScanDetailsAsync(p => GetPayloadStr(p, "email") == e); if (d == null) { OASISErrorHandling.HandleError(ref result, $"QdrantOASIS: No detail for email '{e}'."); return result; } result.Result = d; result.IsError = false; result.Message = "QdrantOASIS: AvatarDetail loaded."; } catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"QdrantOASIS: {ex.Message}"); } return result; }
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string e, int version = 0) => LoadAvatarDetailByEmailAsync(e, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            try { var pts = await ScrollAsync("oasis_avatar_details"); var details = pts.Select(p => Des<AvatarDetail>(GetPayloadStr(p, "data_json"))).Where(d => d != null).Cast<IAvatarDetail>().ToList(); result.Result = details; result.IsError = false; result.Message = $"QdrantOASIS: Loaded {details.Count} detail(s)."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"QdrantOASIS: {ex.Message}"); }
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
                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.QdrantOASIS] = holon.Id.ToString();
                await UpsertPointAsync("oasis_holons", holon.Id.ToString(), new Dictionary<string, object?>
                {
                    ["parent_holon_id"] = holon.ParentHolonId.ToString(),
                    ["holon_type"] = (int)holon.HolonType,
                    ["is_deleted"] = holon.IsDeleted,
                    ["data_json"] = Ser(holon)
                });
                result.Result = holon; result.IsError = false; result.Message = $"QdrantOASIS: Holon '{holon.Name}' saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"QdrantOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var saved = new List<IHolon>(); var errors = new List<string>();
            foreach (var h in holons) { var r = await SaveHolonAsync(h, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider); if (r.IsError) errors.Add(r.Message ?? ""); else if (r.Result != null) saved.Add(r.Result); }
            result.Result = saved; result.IsError = errors.Count > 0; result.Message = errors.Count > 0 ? string.Join("; ", errors) : $"QdrantOASIS: {saved.Count} holon(s) saved.";
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;

        // ─── Holon loading ────────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var point = await GetPointAsync("oasis_holons", id.ToString());
                if (point == null || GetPayloadBool(point.Value, "is_deleted")) { OASISErrorHandling.HandleError(ref result, $"QdrantOASIS: No holon for ID '{id}'."); return result; }
                var holon = Des<Holon>(GetPayloadStr(point.Value, "data_json")); if (holon == null) { OASISErrorHandling.HandleError(ref result, "QdrantOASIS: Deserialise failed."); return result; }
                result.Result = holon; result.IsError = false; result.Message = "QdrantOASIS: Holon loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"QdrantOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string pk, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) { if (Guid.TryParse(pk, out Guid id)) return await LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version); var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, $"QdrantOASIS: Invalid GUID '{pk}'."); return r; }
        public override OASISResult<IHolon> LoadHolon(string pk, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonAsync(pk, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var points = await ScrollAsync("oasis_holons");
                var holons = points.Where(p => !GetPayloadBool(p, "is_deleted") && (type == HolonType.All || GetHolonType(p) == (int)type)).Select(p => Des<Holon>(GetPayloadStr(p, "data_json"))).Where(h => h != null).Cast<IHolon>().ToList();
                result.Result = holons; result.IsError = false; result.Message = $"QdrantOASIS: Loaded {holons.Count} holon(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"QdrantOASIS: {ex.Message}"); }
            return result;
        }

        private int GetHolonType(JsonElement p) { if (p.TryGetProperty("payload", out var pl) && pl.TryGetProperty("holon_type", out var v) && v.ValueKind == JsonValueKind.Number) return v.GetInt32(); return 0; }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var idStr = id.ToString();
                var points = await ScrollAsync("oasis_holons");
                var holons = points.Where(p => !GetPayloadBool(p, "is_deleted") && GetPayloadStr(p, "parent_holon_id") == idStr && (type == HolonType.All || GetHolonType(p) == (int)type)).Select(p => Des<Holon>(GetPayloadStr(p, "data_json"))).Where(h => h != null).Cast<IHolon>().ToList();
                result.Result = holons; result.IsError = false; result.Message = $"QdrantOASIS: Loaded {holons.Count} holon(s) for parent '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"QdrantOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string pk, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) { if (Guid.TryParse(pk, out Guid id)) return await LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version); var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, $"QdrantOASIS: Invalid GUID '{pk}'."); return r; }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string pk, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsForParentAsync(pk, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        // ─── Holon deletion ───────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var loaded = await LoadHolonAsync(id); if (loaded.IsError || loaded.Result == null) { OASISErrorHandling.HandleError(ref result, $"QdrantOASIS: Holon '{id}' not found."); return result; }
                var holon = (Holon)loaded.Result; holon.DeletedDate = DateTime.UtcNow;
                await UpsertPointAsync("oasis_holons", id.ToString(), new Dictionary<string, object?> { ["parent_holon_id"] = holon.ParentHolonId.ToString(), ["holon_type"] = (int)holon.HolonType, ["is_deleted"] = true, ["data_json"] = Ser(holon) });
                result.Result = holon; result.IsError = false; result.Message = $"QdrantOASIS: Holon '{id}' soft-deleted.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"QdrantOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id) => DeleteHolonAsync(id).Result;
        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string pk) { if (Guid.TryParse(pk, out Guid id)) return await DeleteHolonAsync(id); var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, $"QdrantOASIS: Invalid GUID '{pk}'."); return r; }
        public override OASISResult<IHolon> DeleteHolon(string pk) => DeleteHolonAsync(pk).Result;

        // ─── Search ───────────────────────────────────────────────────────────────

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            try { string? q = searchParams.SearchGroups?.OfType<SearchTextGroup>().FirstOrDefault()?.SearchQuery?.ToLower(); var all = await LoadAllHolonsAsync(); var holons = all.Result?.ToList() ?? new List<IHolon>(); if (!string.IsNullOrEmpty(q)) holons = holons.Where(h => h.Name?.ToLower().Contains(q) == true || h.Description?.ToLower().Contains(q) == true).ToList(); result.Result = new SearchResults { SearchResultHolons = holons, NumberOfResults = holons.Count }; result.IsError = false; result.Message = $"QdrantOASIS: Found {holons.Count} result(s)."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        { var all = await LoadAllHolonsAsync(type); var holons = all.Result?.Where(h => h.MetaData != null && h.MetaData.TryGetValue(metaKey, out var v) && v?.ToString() == metaValue).ToList() ?? new List<IHolon>(); return new OASISResult<IEnumerable<IHolon>> { Result = holons, IsError = false, Message = $"QdrantOASIS: {holons.Count} holon(s)." }; }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKvp, MetaKeyValuePairMatchMode mode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        { var all = await LoadAllHolonsAsync(type); bool IsMatch(IHolon h) { if (h.MetaData == null) return false; var checks = metaKvp.Select(kvp => h.MetaData.TryGetValue(kvp.Key, out var v) && v?.ToString() == kvp.Value); return mode == MetaKeyValuePairMatchMode.Any ? checks.Any(c => c) : checks.All(c => c); } var holons = all.Result?.Where(IsMatch).ToList() ?? new List<IHolon>(); return new OASISResult<IEnumerable<IHolon>> { Result = holons, IsError = false, Message = $"QdrantOASIS: {holons.Count} holon(s)." }; }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKvp, MetaKeyValuePairMatchMode mode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsByMetaDataAsync(metaKvp, mode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        // ─── Import / Export ──────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons) { var s = await SaveHolonsAsync(holons); return new OASISResult<bool> { Result = !s.IsError, IsError = s.IsError, Message = s.Message }; }
        public override OASISResult<bool> Import(IEnumerable<IHolon> holons) => ImportAsync(holons).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0) => await LoadAllHolonsAsync();
        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0) => ExportAllAsync(version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0) { var all = await LoadAllHolonsAsync(); var h = all.Result?.Where(x => x.CreatedByAvatarId == avatarId).ToList() ?? new List<IHolon>(); return new OASISResult<IEnumerable<IHolon>> { Result = h, IsError = false, Message = $"QdrantOASIS: {h.Count} holon(s)." }; }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0) => ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string u, int version = 0) { var a = await LoadAvatarByUsernameAsync(u); if (a.IsError || a.Result == null) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "Not found."); return r; } return await ExportAllDataForAvatarByIdAsync(a.Result.Id, version); }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string u, int version = 0) => ExportAllDataForAvatarByUsernameAsync(u, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string e, int version = 0) { var a = await LoadAvatarByEmailAsync(e); if (a.IsError || a.Result == null) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "Not found."); return r; } return await ExportAllDataForAvatarByIdAsync(a.Result.Id, version); }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string e, int version = 0) => ExportAllDataForAvatarByEmailAsync(e, version).Result;

        public bool IsVersionControlEnabled { get; set; } = false;
    }
}
