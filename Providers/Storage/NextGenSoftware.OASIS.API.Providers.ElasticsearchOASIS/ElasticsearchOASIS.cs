using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.ElasticsearchOASIS
{
    /// <summary>
    /// OASIS provider for Elasticsearch (and compatible OpenSearch) — accessed via Elasticsearch REST API.
    ///
    /// Indices:
    ///   oasis-avatars, oasis-avatar-details, oasis-holons
    ///
    /// Each document uses the OASIS object UUID as its _id.
    /// Indexed fields: username, email, isDeleted, parentHolonId, holonType.
    /// The full serialised object is stored in the dataJson field.
    ///
    /// Constructor parameters:
    ///   host      — Elasticsearch host, e.g. "http://localhost:9200" or "https://xyz.es.io"
    ///   username  — HTTP Basic auth username (optional)
    ///   password  — HTTP Basic auth password (optional)
    ///   apiKey    — Elasticsearch API key (optional; takes priority over Basic auth)
    /// </summary>
    public partial class ElasticsearchOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISDBStorageProvider
    {
        private readonly string _host;
        private readonly HttpClient _http;

        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public ElasticsearchOASIS(string host, string? username = null, string? password = null, string? apiKey = null)
            : this(new HttpClient(), host, username, password, apiKey) { }

        /// <summary>Use a dedicated caller-owned client, including its TLS trust/transport policy.</summary>
        public ElasticsearchOASIS(HttpClient client, string host, string? username = null, string? password = null, string? apiKey = null)
        {
            _host = host.TrimEnd('/');
            _http = client ?? throw new ArgumentNullException(nameof(client));
            if (!string.IsNullOrEmpty(apiKey))
                _http.DefaultRequestHeaders.Add("Authorization", $"ApiKey {apiKey}");
            else if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}")));
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            ProviderName = "ElasticsearchOASIS";
            ProviderDescription = "Elasticsearch / OpenSearch provider (REST API — full-text search and analytics over OASIS holons)";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.ElasticsearchOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageLocal);
        }

        private static string Ser(object o) => JsonSerializer.Serialize(o, _jsonOpts);
        private static T? Des<T>(string? json) => json == null ? default
            : typeof(T) == typeof(AvatarDetail) ? StoredRecordJson.DeserializeStored<T>(json)
            : typeof(T) == typeof(Holon) ? (T)(object)StoredRecordJson.DeserializeStoredHolon(json)
            : JsonSerializer.Deserialize<T>(json, _jsonOpts);

        // ─── REST helpers ─────────────────────────────────────────────────────────

        private async Task<JsonElement> EsGetAsync(string path)
        {
            var resp = await _http.GetAsync($"{_host}{path}");
            var raw = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode && resp.StatusCode != HttpStatusCode.NotFound)
                throw new Exception($"ES GET {path} → {(int)resp.StatusCode}: {raw}");
            using var doc = JsonDocument.Parse(raw);
            return doc.RootElement.Clone();
        }

        private async Task<JsonElement> EsPostAsync(string path, object body)
        {
            var content = new StringContent(Ser(body), Encoding.UTF8, "application/json");
            var resp = await _http.PostAsync($"{_host}{path}", content);
            var raw = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) throw new Exception($"ES POST {path} → {(int)resp.StatusCode}: {raw}");
            using var doc = JsonDocument.Parse(raw);
            return doc.RootElement.Clone();
        }

        private async Task EsPutAsync(string path, object body)
        {
            var content = new StringContent(Ser(body), Encoding.UTF8, "application/json");
            var resp = await _http.PutAsync($"{_host}{path}", content);
            var raw = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) throw new Exception($"ES PUT {path} → {(int)resp.StatusCode}: {raw}");
        }

        private async Task DeleteDocumentAsync(string index, string id, bool allowMissing = false)
        {
            using var response = await _http.DeleteAsync($"{_host}/{index}/_doc/{id}?refresh=wait_for");
            if (!allowMissing || response.StatusCode != HttpStatusCode.NotFound)
                response.EnsureSuccessStatusCode();
            using var receipt = await ReadNativeResponseAsync(response, 64 * 1024, default);
            var ack = receipt.RootElement;
            if (MatchesDocument(ack, index, id))
            {
                var outcome = ack.GetProperty("result").GetString();
                if (allowMissing && response.StatusCode == HttpStatusCode.NotFound && outcome == "not_found")
                    return;
                if (response.IsSuccessStatusCode && outcome == "deleted" &&
                    ack.GetProperty("_shards").GetProperty("failed").GetInt32() == 0 &&
                    ack.GetProperty("_shards").GetProperty("successful").GetInt32() > 0)
                    return;
            }
            throw new System.IO.InvalidDataException("Elasticsearch did not acknowledge the requested document deletion.");
        }

        private static bool MatchesDocument(JsonElement record, string index, string id)
            => record.TryGetProperty("_index", out var actualIndex) && actualIndex.GetString() == index &&
               record.TryGetProperty("_id", out var actualId) && actualId.GetString() == id;

        private async Task UpsertDocAsync(string index, string id, Dictionary<string, object?> doc)
        {
            using var request = new HttpRequestMessage(HttpMethod.Put, $"{_host}/{index}/_doc/{id}?refresh=wait_for")
            {
                Content = new StringContent(Ser(doc), Encoding.UTF8, "application/json")
            };
            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            using var acknowledgement = await ReadNativeResponseAsync(response, 64 * 1024, default);
            var ack = acknowledgement.RootElement;
            if (!MatchesDocument(ack, index, id) ||
                ack.GetProperty("_version").GetInt64() < 1 || ack.GetProperty("_seq_no").GetInt64() < 0 ||
                ack.GetProperty("_primary_term").GetInt64() < 1 ||
                ack.GetProperty("result").GetString() is not ("created" or "updated") ||
                ack.GetProperty("_shards").GetProperty("failed").GetInt32() != 0 ||
                ack.GetProperty("_shards").GetProperty("successful").GetInt32() < 1)
                throw new System.IO.InvalidDataException("Native write did not acknowledge the staged identity/revision.");
        }

        private async Task<JsonElement?> GetDocAsync(string index, string id)
        {
            var result = await EsGetAsync($"/{index}/_doc/{id}");
            if (!MatchesDocument(result, index, id) || !result.TryGetProperty("found", out var found) ||
                found.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                throw new System.IO.InvalidDataException("Elasticsearch did not acknowledge the requested document read.");
            return found.GetBoolean() ? result : null;
        }

        private string? GetSourceStr(JsonElement doc, string key)
        {
            if (doc.TryGetProperty("_source", out var src) && src.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String)
                return v.GetString();
            return null;
        }

        private bool GetSourceBool(JsonElement doc, string key)
        {
            if (doc.TryGetProperty("_source", out var src) && src.TryGetProperty(key, out var v))
                return v.ValueKind == JsonValueKind.True;
            return false;
        }

        private async Task<List<JsonElement>> SearchAsync(string index, object query, int size = 10000)
        {
            var result = await EsPostAsync($"/{index}/_search", new { size, query });
            var hits = new List<JsonElement>();
            if (result.TryGetProperty("hits", out var outer) && outer.TryGetProperty("hits", out var arr) && arr.ValueKind == JsonValueKind.Array)
                foreach (var h in arr.EnumerateArray()) hits.Add(h.Clone());
            return hits;
        }

        private async Task EnsureIndexAsync(string index, object mappings)
        {
            var check = await EsGetAsync($"/{index}");
            if (check.TryGetProperty(index, out _)) return;
            await EsPutAsync($"/{index}", new { mappings = new { properties = mappings } });
        }

        // ─── Activation ───────────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            IsProviderActivated = false;
            try
            {
                var strKw = new { type = "keyword" };
                var boolProp = new { type = "boolean" };
                var intProp = new { type = "integer" };

                await EnsureIndexAsync("oasis-avatars", new { username = strKw, email = strKw, isDeleted = boolProp, dataJson = new { type = "text", index = false } });
                await EnsureIndexAsync("oasis-avatar-details", new { username = strKw, email = strKw, dataJson = new { type = "text", index = false } });
                await EnsureIndexAsync("oasis-holons", new { parentHolonId = strKw, holonType = intProp, isDeleted = boolProp, dataJson = new { type = "text", index = false } });
                IsProviderActivated = true; result.Result = true; result.IsError = false; result.Message = "ElasticsearchOASIS activated — indices ready.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"ElasticsearchOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;
        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            IsProviderActivated = false;
            return await Task.FromResult(new OASISResult<bool> { Result = true, Message = "ElasticsearchOASIS deactivated." });
        }
        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // ─── Avatar saving ────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var id = avatar.Id == Guid.Empty ? Guid.NewGuid() : avatar.Id;
                var key = id.ToString("D");
                var keys = avatar.ProviderUniqueStorageKey == null
                    ? new Dictionary<Core.Enums.ProviderType, string>()
                    : new Dictionary<Core.Enums.ProviderType, string>(avatar.ProviderUniqueStorageKey);
                keys[Core.Enums.ProviderType.ElasticsearchOASIS] = key;
                var payload = JsonSerializer.SerializeToNode(avatar, avatar.GetType(), _jsonOpts)!.AsObject();
                payload["id"] = id;
                payload["providerUniqueStorageKey"] = JsonSerializer.SerializeToNode(keys, _jsonOpts);
                await UpsertDocAsync("oasis-avatars", key, new Dictionary<string, object?>
                {
                    ["username"] = avatar.Username, ["email"] = avatar.Email,
                    ["isDeleted"] = avatar.IsDeleted, ["dataJson"] = payload.ToJsonString(_jsonOpts)
                });
                avatar.Id = id;
                avatar.ProviderUniqueStorageKey ??= new Dictionary<Core.Enums.ProviderType, string>();
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.ElasticsearchOASIS] = key;
                result.IsSaved = true;
                result.SavedCount = 1;
                result.Result = avatar; result.IsError = false; result.Message = $"ElasticsearchOASIS: Avatar '{avatar.Username}' saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"ElasticsearchOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) => SaveAvatarAsync(avatar).Result;

        // ─── Avatar loading ───────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var doc = await GetDocAsync("oasis-avatars", id.ToString());
                if (doc == null || GetSourceBool(doc.Value, "isDeleted")) { OASISErrorHandling.HandleError(ref result, $"ElasticsearchOASIS: No avatar for ID '{id}'."); result.ErrorCode = OASISResultErrorCodes.RecordNotFound; return result; }
                var avatar = Des<Avatar>(GetSourceStr(doc.Value, "dataJson")); if (avatar == null) { OASISErrorHandling.HandleError(ref result, "ElasticsearchOASIS: Deserialise failed."); return result; }
                result.Result = avatar; result.IsLoaded = true; result.LoadedCount = 1; result.Message = "ElasticsearchOASIS: Avatar loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"ElasticsearchOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) => LoadAvatarAsync(id, version).Result;

        private async Task<Avatar?> SearchAvatarAsync(string field, string value)
        {
            var hits = await SearchAsync("oasis-avatars", new { term = new Dictionary<string, object> { [field] = value } });
            var hit = hits.FirstOrDefault(h => !GetSourceBool(h, "isDeleted"));
            if (hit.ValueKind == JsonValueKind.Undefined) return null;
            return Des<Avatar>(GetSourceStr(hit, "dataJson"));
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try { var a = await SearchAvatarAsync("username", username); if (a == null) { OASISErrorHandling.HandleError(ref result, $"ElasticsearchOASIS: No avatar for username '{username}'."); return result; } result.Result = a; result.IsLoaded = true; result.LoadedCount = 1; result.Message = "ElasticsearchOASIS: Avatar loaded."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"ElasticsearchOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string username, int version = 0) => LoadAvatarByUsernameAsync(username, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string email, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try { var a = await SearchAvatarAsync("email", email); if (a == null) { OASISErrorHandling.HandleError(ref result, $"ElasticsearchOASIS: No avatar for email '{email}'."); return result; } result.Result = a; result.IsLoaded = true; result.LoadedCount = 1; result.Message = "ElasticsearchOASIS: Avatar loaded."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"ElasticsearchOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string email, int version = 0) => LoadAvatarByEmailAsync(email, version).Result;
        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string pk, int version = 0) { if (Guid.TryParse(pk, out Guid id)) return await LoadAvatarAsync(id, version); var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, $"ElasticsearchOASIS: Invalid GUID '{pk}'."); return r; }
        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string pk, int version = 0) => LoadAvatarByProviderKeyAsync(pk, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                var hits = await SearchAsync("oasis-avatars", new { match_all = new { } });
                var avatars = hits.Where(h => !GetSourceBool(h, "isDeleted")).Select(h => Des<Avatar>(GetSourceStr(h, "dataJson"))).Where(a => a != null).Cast<IAvatar>().ToList();
                result.Result = avatars; result.IsError = false; result.Message = $"ElasticsearchOASIS: Loaded {avatars.Count} avatar(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"ElasticsearchOASIS: {ex.Message}"); }
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
                    var loaded = await LoadAvatarAsync(id); if (loaded.IsError || loaded.Result == null) { OASISErrorHandling.HandleError(ref result, $"ElasticsearchOASIS: Avatar '{id}' not found."); return result; }
                    var av = (Avatar)loaded.Result; av.DeletedDate = DateTime.UtcNow;
                    await UpsertDocAsync("oasis-avatars", id.ToString(), new Dictionary<string, object?> { ["username"] = av.Username, ["email"] = av.Email, ["isDeleted"] = true, ["dataJson"] = Ser(av) });
                }
                else
                {
                    var key = id.ToString("D");
                    await DeleteDocumentAsync("oasis-avatar-details", key, allowMissing: true);
                    await DeleteDocumentAsync("oasis-avatars", key);
                }
                result.Result = true; result.IsDeleted = true; result.DeletedCount = 1;
                result.Message = $"ElasticsearchOASIS: Avatar '{id}' deleted.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"ElasticsearchOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) => DeleteAvatarAsync(id, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string u, bool softDelete = true) { var a = await LoadAvatarByUsernameAsync(u); if (a.IsError || a.Result == null) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "Not found."); return r; } return await DeleteAvatarAsync(a.Result.Id, softDelete); }
        public override OASISResult<bool> DeleteAvatarByUsername(string u, bool softDelete = true) => DeleteAvatarByUsernameAsync(u, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string e, bool softDelete = true) { var a = await LoadAvatarByEmailAsync(e); if (a.IsError || a.Result == null) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "Not found."); return r; } return await DeleteAvatarAsync(a.Result.Id, softDelete); }
        public override OASISResult<bool> DeleteAvatarByEmail(string e, bool softDelete = true) => DeleteAvatarByEmailAsync(e, softDelete).Result;
        public override Task<OASISResult<bool>> DeleteAvatarAsync(string pk, bool softDelete = true)
            => GuidProviderKeyDeletion.DeleteAvatarAsync(pk, softDelete, DeleteAvatarAsync);
        public override OASISResult<bool> DeleteAvatar(string pk, bool softDelete = true) => DeleteAvatarAsync(pk, softDelete).Result;

        // ─── AvatarDetail ─────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail d)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var id = d.Id == Guid.Empty ? Guid.NewGuid() : d.Id;
                var payload = JsonNode.Parse(StoredRecordJson.Serialize(d))!.AsObject();
                payload["Id"] = id;
                await UpsertDocAsync("oasis-avatar-details", id.ToString("D"), new Dictionary<string, object?>
                {
                    ["username"] = d.Username, ["email"] = d.Email, ["dataJson"] = payload.ToJsonString()
                });
                d.Id = id;
                result.IsSaved = true;
                result.SavedCount = 1;
                result.Result = d; result.IsError = false; result.Message = "ElasticsearchOASIS: AvatarDetail saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"ElasticsearchOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail d) => SaveAvatarDetailAsync(d).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var doc = await GetDocAsync("oasis-avatar-details", id.ToString());
                if (doc == null) { OASISErrorHandling.HandleError(ref result, $"ElasticsearchOASIS: No detail for ID '{id}'."); result.ErrorCode = OASISResultErrorCodes.RecordNotFound; return result; }
                var d = Des<AvatarDetail>(GetSourceStr(doc.Value, "dataJson")); if (d == null) { OASISErrorHandling.HandleError(ref result, "ElasticsearchOASIS: Deserialise failed."); return result; }
                result.Result = d; result.IsLoaded = true; result.LoadedCount = 1; result.Message = "ElasticsearchOASIS: AvatarDetail loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"ElasticsearchOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => LoadAvatarDetailAsync(id, version).Result;

        private async Task<AvatarDetail?> SearchDetailAsync(string field, string value)
        {
            var hits = await SearchAsync("oasis-avatar-details", new { term = new Dictionary<string, object> { [field] = value } });
            var hit = hits.FirstOrDefault();
            return hit.ValueKind == JsonValueKind.Undefined ? null : Des<AvatarDetail>(GetSourceStr(hit, "dataJson"));
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string u, int version = 0) { var result = new OASISResult<IAvatarDetail>(); try { var d = await SearchDetailAsync("username", u); if (d == null) { OASISErrorHandling.HandleError(ref result, $"ElasticsearchOASIS: No detail for username '{u}'."); return result; } result.Result = d; result.IsLoaded = true; result.LoadedCount = 1; result.Message = "ElasticsearchOASIS: AvatarDetail loaded."; } catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"ElasticsearchOASIS: {ex.Message}"); } return result; }
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string u, int version = 0) => LoadAvatarDetailByUsernameAsync(u, version).Result;
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string e, int version = 0) { var result = new OASISResult<IAvatarDetail>(); try { var d = await SearchDetailAsync("email", e); if (d == null) { OASISErrorHandling.HandleError(ref result, $"ElasticsearchOASIS: No detail for email '{e}'."); return result; } result.Result = d; result.IsLoaded = true; result.LoadedCount = 1; result.Message = "ElasticsearchOASIS: AvatarDetail loaded."; } catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"ElasticsearchOASIS: {ex.Message}"); } return result; }
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string e, int version = 0) => LoadAvatarDetailByEmailAsync(e, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            try { var hits = await SearchAsync("oasis-avatar-details", new { match_all = new { } }); var details = hits.Select(h => Des<AvatarDetail>(GetSourceStr(h, "dataJson"))).Where(d => d != null).Cast<IAvatarDetail>().ToList(); result.Result = details; result.IsError = false; result.Message = $"ElasticsearchOASIS: Loaded {details.Count} detail(s)."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"ElasticsearchOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0) => LoadAllAvatarDetailsAsync(version).Result;

        // ─── Holon saving ─────────────────────────────────────────────────────────

        public override Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveGraphAsync(holon, saveChildren, recursive, maxChildDepth, 0, continueOnError);

        private async Task<OASISResult<IHolon>> SaveGraphAsync(IHolon holon, bool saveChildren, bool recursive,
            int maxChildDepth, int currentChildDepth, bool continueOnError)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var plan = HolonGraphWritePlan.CreateForSave(new[] { holon }, saveChildren, recursive, maxChildDepth, currentChildDepth);
                var failed = new HashSet<Guid>();
                foreach (var node in plan.Nodes)
                {
                    var snapshot = node.Snapshot;
                    if (failed.Contains(snapshot.ParentHolonId))
                    {
                        failed.Add(snapshot.Id);
                        continue;
                    }

                    try
                    {
                        await SavePlannedHolonAsync(node);
                        if (ReferenceEquals(node.Source, holon))
                            result.Result = holon;
                        result.SavedCount++;
                    }
                    catch (Exception ex)
                    {
                        failed.Add(snapshot.Id);
                        OASISErrorHandling.HandleError(ref result, $"Elasticsearch did not save holon '{snapshot.Id}'.", ex);
                        if (!continueOnError)
                            break;
                    }
                }
                result.IsSaved = result.SavedCount == plan.Nodes.Count;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"ElasticsearchOASIS: {ex.Message}", ex);
            }
            return result;
        }

        private async Task SavePlannedHolonAsync(PlannedHolonWrite node)
        {
            var snapshot = node.Snapshot;
            var key = snapshot.Id.ToString("D");
            var keys = snapshot.ProviderUniqueStorageKey ?? new Dictionary<Core.Enums.ProviderType, string>();
            keys[Core.Enums.ProviderType.ElasticsearchOASIS] = key;
            var payload = JsonNode.Parse(node.CapturedIntentJson)!.AsObject();
            payload.Remove("$type");
            payload["Id"] = snapshot.Id;
            payload["ParentHolonId"] = snapshot.ParentHolonId;
            payload["ProviderUniqueStorageKey"] = JsonSerializer.SerializeToNode(keys);
            await UpsertDocAsync("oasis-holons", key, new Dictionary<string, object?>
            {
                ["parentHolonId"] = snapshot.ParentHolonId.ToString("D"),
                ["holonType"] = (int)snapshot.HolonType,
                ["isDeleted"] = snapshot.IsDeleted,
                ["dataJson"] = payload.ToJsonString()
            });
            node.Source.Id = snapshot.Id;
            node.Source.ParentHolonId = snapshot.ParentHolonId;
            node.Source.ProviderUniqueStorageKey = keys;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var saved = new List<IHolon>(); var errors = new List<string>();
            foreach (var h in holons)
            {
                var write = await SaveGraphAsync(h, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError);
                result.SavedCount += write.SavedCount;
                if (write.IsError)
                {
                    errors.Add(write.Message);
                    if (!continueOnError) break;
                }
                else if (write.IsSaved && write.Result != null) saved.Add(write.Result);
            }
            result.Result = saved;
            result.IsError = errors.Count > 0;
            result.ErrorCount = errors.Count;
            result.IsSaved = saved.Count > 0;
            result.Message = string.Join("; ", errors);
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;

        // ─── Holon loading ────────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var doc = await GetDocAsync("oasis-holons", id.ToString());
                if (doc == null || GetSourceBool(doc.Value, "isDeleted")) { OASISErrorHandling.HandleError(ref result, $"ElasticsearchOASIS: No holon for ID '{id}'."); result.ErrorCode = OASISResultErrorCodes.RecordNotFound; return result; }
                var holon = Des<Holon>(GetSourceStr(doc.Value, "dataJson")); if (holon == null) { OASISErrorHandling.HandleError(ref result, "ElasticsearchOASIS: Deserialise failed."); return result; }
                result.Result = holon; result.IsLoaded = true; result.LoadedCount = 1; result.Message = "ElasticsearchOASIS: Holon loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"ElasticsearchOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string pk, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) { if (Guid.TryParse(pk, out Guid id)) return await LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version); var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, $"ElasticsearchOASIS: Invalid GUID '{pk}'."); return r; }
        public override OASISResult<IHolon> LoadHolon(string pk, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonAsync(pk, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var query = type == HolonType.All ? (object)new { match_all = new { } } : new { term = new Dictionary<string, object> { ["holonType"] = (int)type } };
                var hits = await SearchAsync("oasis-holons", query);
                var holons = hits.Where(h => !GetSourceBool(h, "isDeleted")).Select(h => Des<Holon>(GetSourceStr(h, "dataJson"))).Where(h => h != null).Cast<IHolon>().ToList();
                result.Result = holons; result.IsLoaded = true; result.LoadedCount = holons.Count; result.Message = $"ElasticsearchOASIS: Loaded {holons.Count} holon(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"ElasticsearchOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var idStr = id.ToString();
                var mustFilters = new List<object> { new { term = new Dictionary<string, object> { ["parentHolonId"] = idStr } } };
                if (type != HolonType.All) mustFilters.Add(new { term = new Dictionary<string, object> { ["holonType"] = (int)type } });
                var hits = await SearchAsync("oasis-holons", new { @bool = new { must = mustFilters } });
                var holons = hits.Where(h => !GetSourceBool(h, "isDeleted")).Select(h => Des<Holon>(GetSourceStr(h, "dataJson"))).Where(h => h != null).Cast<IHolon>().ToList();
                result.Result = holons; result.IsLoaded = true; result.LoadedCount = holons.Count; result.Message = $"ElasticsearchOASIS: Loaded {holons.Count} holon(s) for parent '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"ElasticsearchOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string pk, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) { if (Guid.TryParse(pk, out Guid id)) return await LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version); var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, $"ElasticsearchOASIS: Invalid GUID '{pk}'."); return r; }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string pk, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsForParentAsync(pk, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        // ─── Holon deletion ───────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var loaded = await LoadHolonAsync(id); if (loaded.IsError || loaded.Result == null) { OASISErrorHandling.HandleError(ref result, $"ElasticsearchOASIS: Holon '{id}' not found."); return result; }
                var holon = (Holon)loaded.Result; holon.DeletedDate = DateTime.UtcNow;
                await UpsertDocAsync("oasis-holons", id.ToString(), new Dictionary<string, object?> { ["parentHolonId"] = holon.ParentHolonId.ToString(), ["holonType"] = (int)holon.HolonType, ["isDeleted"] = true, ["dataJson"] = Ser(holon) });
                result.Result = holon; result.IsDeleted = true; result.DeletedCount = 1; result.Message = $"ElasticsearchOASIS: Holon '{id}' soft-deleted.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"ElasticsearchOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id) => DeleteHolonAsync(id).Result;
        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string pk) { if (Guid.TryParse(pk, out Guid id)) return await DeleteHolonAsync(id); var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, $"ElasticsearchOASIS: Invalid GUID '{pk}'."); return r; }
        public override OASISResult<IHolon> DeleteHolon(string pk) => DeleteHolonAsync(pk).Result;

        // ─── Search ───────────────────────────────────────────────────────────────

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            try
            {
                string? q = searchParams.SearchGroups?.OfType<SearchTextGroup>().FirstOrDefault()?.SearchQuery;
                object query = string.IsNullOrEmpty(q) ? new { match_all = new { } } : (object)new { multi_match = new { query = q, fields = new[] { "dataJson" } } };
                var hits = await SearchAsync("oasis-holons", query);
                var holons = hits.Where(h => !GetSourceBool(h, "isDeleted")).Select(h => Des<Holon>(GetSourceStr(h, "dataJson"))).Where(h => h != null).Cast<IHolon>().ToList();
                result.Result = new SearchResults { SearchResultHolons = holons, NumberOfResults = holons.Count }; result.IsError = false; result.Message = $"ElasticsearchOASIS: Found {holons.Count} result(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;

        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => FilterHolonsAsync(type, version,
                h => h.MetaData != null && h.MetaData.TryGetValue(metaKey, out var value) && value?.ToString() == metaValue);
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKvp, MetaKeyValuePairMatchMode mode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => FilterHolonsAsync(type, version, h =>
            {
                if (metaKvp == null || metaKvp.Count == 0)
                    return true;
                if (h.MetaData == null)
                    return false;
                var checks = metaKvp.Select(pair => h.MetaData.TryGetValue(pair.Key, out var value) && value?.ToString() == pair.Value);
                return mode == MetaKeyValuePairMatchMode.Any ? checks.Any(match => match) : checks.All(match => match);
            });
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKvp, MetaKeyValuePairMatchMode mode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsByMetaDataAsync(metaKvp, mode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        // ─── Import / Export ──────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons) { var s = await SaveHolonsAsync(holons); return new OASISResult<bool> { Result = !s.IsError && s.IsSaved, IsError = s.IsError, IsSaved = s.IsSaved, SavedCount = s.SavedCount, Message = s.Message, Exception = s.Exception }; }
        public override OASISResult<bool> Import(IEnumerable<IHolon> holons) => ImportAsync(holons).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0) => await LoadAllHolonsAsync();
        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0) => ExportAllAsync(version).Result;
        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
            => FilterHolonsAsync(HolonType.All, version, h => h.CreatedByAvatarId == avatarId);

        private async Task<OASISResult<IEnumerable<IHolon>>> FilterHolonsAsync(HolonType type, int version, Func<IHolon, bool> predicate)
        {
            var all = await LoadAllHolonsAsync(type, loadChildren: false, version: version);
            if (all.IsError)
                return all;
            var records = all.Result.Where(predicate).ToList();
            return new OASISResult<IEnumerable<IHolon>>(records) { IsLoaded = true, LoadedCount = records.Count };
        }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0) => ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string u, int version = 0) { var a = await LoadAvatarByUsernameAsync(u); if (a.IsError || a.Result == null) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "Not found."); return r; } return await ExportAllDataForAvatarByIdAsync(a.Result.Id, version); }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string u, int version = 0) => ExportAllDataForAvatarByUsernameAsync(u, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string e, int version = 0) { var a = await LoadAvatarByEmailAsync(e); if (a.IsError || a.Result == null) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "Not found."); return r; } return await ExportAllDataForAvatarByIdAsync(a.Result.Id, version); }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string e, int version = 0) => ExportAllDataForAvatarByEmailAsync(e, version).Result;

        public bool IsVersionControlEnabled { get; set; } = false;
    }
}
