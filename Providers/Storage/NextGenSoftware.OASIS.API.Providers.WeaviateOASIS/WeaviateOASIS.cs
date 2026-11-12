using System;
using System.Collections.Generic;
using System.Linq;
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
    public partial class WeaviateOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISDBStorageProvider
    {
        private readonly string _host;
        private readonly HttpClient _http;
        private readonly Weaviate.Client.WeaviateClient? _queryClient;

        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public WeaviateOASIS(string host, string? apiKey = null)
            : this(new HttpClient(), host, apiKey) { }

        /// <summary>Explicit caller-owned REST/gRPC clients; no endpoint guessing or GraphQL fallback.</summary>
        public WeaviateOASIS(HttpClient client, string host, string? apiKey = null,
            Weaviate.Client.WeaviateClient? queryClient = null)
        {
            _queryClient = queryClient;
            _host = host.TrimEnd('/');
            _http = client ?? throw new ArgumentNullException(nameof(client));
            if (!string.IsNullOrEmpty(apiKey))
                _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            ProviderName = "WeaviateOASIS";
            ProviderDescription = "Weaviate AI-native vector database provider (REST payload storage; explicit gRPC client for bounded CQL)";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.WeaviateOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageLocalAndNetwork);
        }

        private static string Ser(object o) => JsonSerializer.Serialize(o, _jsonOpts);
        private static T? Des<T>(string? json) => json == null ? default
            : typeof(T) == typeof(AvatarDetail) ? StoredRecordJson.DeserializeStored<T>(json)
            : typeof(T) == typeof(Holon) ? (T)(object)StoredRecordJson.DeserializeStoredHolon(json)
            : JsonSerializer.Deserialize<T>(json, _jsonOpts);

        // ─── REST helpers ─────────────────────────────────────────────────────────

        private async Task<JsonElement> WeaviateGetAsync(string path)
        {
            using var resp = await _http.GetAsync($"{_host}{path}");
            var raw = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode && resp.StatusCode != System.Net.HttpStatusCode.NotFound)
                throw new Exception($"Weaviate GET {path} → {(int)resp.StatusCode}: {raw}");
            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                using var notFound = JsonDocument.Parse("{}");
                return notFound.RootElement.Clone();
            }
            if (string.IsNullOrWhiteSpace(raw))
                throw new Exception($"Weaviate GET {path} returned an empty successful response.");
            using var doc = JsonDocument.Parse(raw);
            return doc.RootElement.Clone();
        }

        private async Task<JsonElement> WeaviatePostAsync(string path, object body)
        {
            using var content = new StringContent(Ser(body), Encoding.UTF8, "application/json");
            using var resp = await _http.PostAsync($"{_host}{path}", content);
            var raw = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) throw new Exception($"Weaviate POST {path} → {(int)resp.StatusCode}: {raw}");
            using var doc = JsonDocument.Parse(raw);
            return doc.RootElement.Clone();
        }

        private async Task WeaviatePutAsync(string path, object body)
        {
            using var content = new StringContent(Ser(body), Encoding.UTF8, "application/json");
            using var resp = await _http.PutAsync($"{_host}{path}", content);
            var raw = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) throw new Exception($"Weaviate PUT {path} → {(int)resp.StatusCode}: {raw}");
        }

        private async Task WeaviateDeleteAsync(string path, bool allowMissing = false)
        {
            using var resp = await _http.DeleteAsync($"{_host}{path}");
            if (!resp.IsSuccessStatusCode && !(allowMissing && resp.StatusCode == System.Net.HttpStatusCode.NotFound))
                throw new Exception($"Weaviate DELETE {path} → {(int)resp.StatusCode}");
        }

        // ─── Schema helpers ───────────────────────────────────────────────────────

        private static void ValidateClassSchema(JsonElement schema, string name, object[] properties)
        {
            if (schema.GetProperty("class").GetString() != name || schema.GetProperty("vectorizer").GetString() != "none")
                throw new InvalidOperationException("Native class/vectorizer differs from configured payload storage.");
            var actual = schema.GetProperty("properties").EnumerateArray().ToDictionary(p => p.GetProperty("name").GetString()!);
            var expected = JsonSerializer.SerializeToElement(properties, _jsonOpts);
            foreach (var property in expected.EnumerateArray())
            {
                var key = property.GetProperty("name").GetString()!;
                if (!actual.TryGetValue(key, out var found) ||
                    !property.GetProperty("dataType").EnumerateArray().Select(t => t.GetString())
                        .SequenceEqual(found.GetProperty("dataType").EnumerateArray().Select(t => t.GetString())))
                    throw new InvalidOperationException("Native class required property types differ from configured storage.");
            }
        }

        private async Task EnsureClassAsync(string className, object[] properties)
        {
            using var response = await _http.GetAsync($"{_host}/v1/schema/{className}", HttpCompletionOption.ResponseHeadersRead);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, $"{_host}/v1/schema") {
                    Content = new StringContent(Ser(new { @class = className, vectorizer = "none", properties }), Encoding.UTF8, "application/json") };
                using var created = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                created.EnsureSuccessStatusCode();
                using var acknowledgement = await ReadNativeResponseAsync(created, 64 * 1024, default);
                ValidateClassSchema(acknowledgement.RootElement, className, properties);
                return;
            }
            response.EnsureSuccessStatusCode();
            using var schema = await ReadNativeResponseAsync(response, 64 * 1024, default);
            ValidateClassSchema(schema.RootElement, className, properties);
        }

        // Native single-object batch upsert removes the GET/POST-or-PUT race.
        private async Task UpsertObjectAsync(string className, string id, Dictionary<string, object?> props)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{_host}/v1/batch/objects?consistency_level=ALL") {
                Content = new StringContent(Ser(new { objects = new[] { new { @class = className, id, properties = props } } }), Encoding.UTF8, "application/json") };
            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            using var acknowledgement = await ReadNativeResponseAsync(response, 4 * 1024 * 1024, default);
            var results = acknowledgement.RootElement;
            if (results.ValueKind != JsonValueKind.Array || results.GetArrayLength() != 1)
                throw new InvalidOperationException("One native object acknowledgement required.");
            var item = results[0];
            var result = item.GetProperty("result");
            if (item.GetProperty("class").GetString() != className || item.GetProperty("id").GetString() != id ||
                result.GetProperty("status").GetString() != "SUCCESS" ||
                result.TryGetProperty("errors", out var errors) && errors.ValueKind != JsonValueKind.Null)
                throw new InvalidOperationException("Native object was not acknowledged successfully for the requested class/ID.");
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

        private async Task<List<JsonElement>> GraphQlGetAsync(string className, string[] fields)
        {
            const int pageSize = 100;
            var objects = new List<JsonElement>();
            var cursors = new HashSet<Guid>();
            var selection = string.Join(" ", fields);
            var after = "";

            while (true)
            {
                var query = $"{{ Get {{ {className}(limit: {pageSize}{after}) {{ {selection} _additional {{ id }} }} }} }}";
                var result = await WeaviatePostAsync("/v1/graphql", new { query });
                if (result.TryGetProperty("errors", out var errors) &&
                    (errors.ValueKind != JsonValueKind.Array || errors.GetArrayLength() > 0))
                    throw new InvalidOperationException("Weaviate rejected the GraphQL collection query.");

                if (!result.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object ||
                    !data.TryGetProperty("Get", out var get) || get.ValueKind != JsonValueKind.Object ||
                    !get.TryGetProperty(className, out var rows) || rows.ValueKind != JsonValueKind.Array)
                    throw new JsonException("Weaviate returned an invalid GraphQL collection response.");

                if (rows.GetArrayLength() == 0)
                    return objects;

                foreach (var row in rows.EnumerateArray())
                    objects.Add(row.Clone());

                if (!Guid.TryParse(GetGraphQlId(objects[objects.Count - 1]), out var cursor) || !cursors.Add(cursor))
                    throw new JsonException("Weaviate returned an invalid or repeated GraphQL pagination cursor.");

                after = $", after: \"{cursor:D}\"";
            }
        }

        private static void EnsureCurrentVersion(int version)
        {
            if (version != 0)
                throw new NotSupportedException("Weaviate does not store historical versions.");
        }

        private string? GetGraphQlId(JsonElement o) { if (o.TryGetProperty("_additional", out var a) && a.TryGetProperty("id", out var id)) return id.GetString(); return null; }
        private string? GetGraphQlStr(JsonElement o, string key) { if (o.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String) return v.GetString(); return null; }
        private bool GetGraphQlBool(JsonElement o, string key) { if (o.TryGetProperty(key, out var v)) return v.ValueKind == JsonValueKind.True; return false; }

        // ─── Activation ───────────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            IsProviderActivated = false;
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
                IsProviderActivated = true;
                result.Result = true; result.IsError = false; result.Message = "WeaviateOASIS activated — schema ready.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"WeaviateOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;
        public override Task<OASISResult<bool>> DeActivateProviderAsync() { IsProviderActivated = false; return Task.FromResult(new OASISResult<bool> { Result = true }); }
        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // ─── Avatar saving ────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                ArgumentNullException.ThrowIfNull(avatar);
                var id = avatar.Id == Guid.Empty ? Guid.NewGuid() : avatar.Id;
                var keys = avatar.ProviderUniqueStorageKey == null
                    ? new Dictionary<Core.Enums.ProviderType, string>()
                    : new Dictionary<Core.Enums.ProviderType, string>(avatar.ProviderUniqueStorageKey);
                keys[Core.Enums.ProviderType.WeaviateOASIS] = id.ToString("D");
                var payload = JsonSerializer.SerializeToNode(avatar, avatar.GetType(), _jsonOpts)!.AsObject();
                payload["id"] = id;
                payload["providerUniqueStorageKey"] = JsonSerializer.SerializeToNode(keys, _jsonOpts);
                await UpsertObjectAsync("OasisAvatar", id.ToString("D"), new Dictionary<string, object?>
                {
                    ["username"] = avatar.Username,
                    ["email"] = avatar.Email,
                    ["isDeleted"] = avatar.IsDeleted,
                    ["dataJson"] = payload.ToJsonString(_jsonOpts)
                });
                avatar.Id = id;
                avatar.ProviderUniqueStorageKey ??= new Dictionary<Core.Enums.ProviderType, string>();
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.WeaviateOASIS] = id.ToString("D");
                result.Result = avatar;
                result.IsSaved = true;
                result.SavedCount = 1;
                result.Message = $"WeaviateOASIS: Avatar '{avatar.Username}' saved.";
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
                EnsureCurrentVersion(version);
                var obj = await GetObjectAsync("OasisAvatar", id.ToString());
                if (obj == null || GetPropBool(obj.Value, "isDeleted")) { OASISErrorHandling.HandleError(ref result, $"WeaviateOASIS: No avatar for ID '{id}'."); result.ErrorCode = OASISResultErrorCodes.RecordNotFound; return result; }
                var avatar = Des<Avatar>(GetProp(obj.Value, "dataJson")); if (avatar == null) { OASISErrorHandling.HandleError(ref result, "WeaviateOASIS: Deserialise failed."); return result; }
                result.Result = avatar; result.IsLoaded = true; result.LoadedCount = 1; result.Message = "WeaviateOASIS: Avatar loaded.";
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
            try { EnsureCurrentVersion(version); var a = await ScanAvatarsAsync(o => GetGraphQlStr(o, "username") == username); if (a == null) { OASISErrorHandling.HandleError(ref result, $"WeaviateOASIS: No avatar for username '{username}'."); result.ErrorCode = OASISResultErrorCodes.RecordNotFound; return result; } result.Result = a; result.IsLoaded = true; result.LoadedCount = 1; result.Message = "WeaviateOASIS: Avatar loaded."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"WeaviateOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string username, int version = 0) => LoadAvatarByUsernameAsync(username, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string email, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try { EnsureCurrentVersion(version); var a = await ScanAvatarsAsync(o => GetGraphQlStr(o, "email") == email); if (a == null) { OASISErrorHandling.HandleError(ref result, $"WeaviateOASIS: No avatar for email '{email}'."); result.ErrorCode = OASISResultErrorCodes.RecordNotFound; return result; } result.Result = a; result.IsLoaded = true; result.LoadedCount = 1; result.Message = "WeaviateOASIS: Avatar loaded."; }
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
                EnsureCurrentVersion(version);
                var objs = await GraphQlGetAsync("OasisAvatar", new[] { "isDeleted", "dataJson" });
                var avatars = objs.Where(o => !GetGraphQlBool(o, "isDeleted")).Select(o => Des<Avatar>(GetGraphQlStr(o, "dataJson"))).Where(a => a != null).Cast<IAvatar>().ToList();
                result.Result = avatars; result.IsLoaded = true; result.LoadedCount = avatars.Count; result.Message = $"WeaviateOASIS: Loaded {avatars.Count} avatar(s).";
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
                else
                {
                    if (await GetObjectAsync("OasisAvatar", id.ToString("D")) == null)
                    {
                        OASISErrorHandling.HandleError(ref result, $"WeaviateOASIS: Avatar '{id}' not found.");
                        result.ErrorCode = OASISResultErrorCodes.RecordNotFound;
                        return result;
                    }

                    // Keep the avatar available for retry until its optional detail is gone.
                    await WeaviateDeleteAsync($"/v1/objects/OasisAvatarDetail/{id:D}", allowMissing: true);
                    await WeaviateDeleteAsync($"/v1/objects/OasisAvatar/{id:D}");
                }
                result.Result = true; result.IsDeleted = true; result.DeletedCount = 1; result.Message = $"WeaviateOASIS: Avatar '{id}' deleted.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"WeaviateOASIS: {ex.Message}"); }
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
                ArgumentNullException.ThrowIfNull(d);
                var id = d.Id == Guid.Empty ? Guid.NewGuid() : d.Id;
                var payload = JsonSerializer.SerializeToNode(d, d.GetType(), _jsonOpts)!.AsObject();
                payload["id"] = id;
                await UpsertObjectAsync("OasisAvatarDetail", id.ToString("D"), new Dictionary<string, object?>
                {
                    ["username"] = d.Username,
                    ["email"] = d.Email,
                    ["dataJson"] = payload.ToJsonString(_jsonOpts)
                });
                d.Id = id;
                result.Result = d;
                result.IsSaved = true;
                result.SavedCount = 1;
                result.Message = "WeaviateOASIS: AvatarDetail saved.";
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
                EnsureCurrentVersion(version);
                var obj = await GetObjectAsync("OasisAvatarDetail", id.ToString());
                if (obj == null) { OASISErrorHandling.HandleError(ref result, $"WeaviateOASIS: No detail for ID '{id}'."); result.ErrorCode = OASISResultErrorCodes.RecordNotFound; return result; }
                var d = Des<AvatarDetail>(GetProp(obj.Value, "dataJson")); if (d == null) { OASISErrorHandling.HandleError(ref result, "WeaviateOASIS: Deserialise failed."); return result; }
                result.Result = d; result.IsLoaded = true; result.LoadedCount = 1; result.Message = "WeaviateOASIS: AvatarDetail loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"WeaviateOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => LoadAvatarDetailAsync(id, version).Result;

        private async Task<AvatarDetail?> ScanDetailsAsync(Func<JsonElement, bool> pred) { var objs = await GraphQlGetAsync("OasisAvatarDetail", new[] { "username", "email", "dataJson" }); var m = objs.FirstOrDefault(pred); return m.ValueKind == JsonValueKind.Undefined ? null : Des<AvatarDetail>(GetGraphQlStr(m, "dataJson")); }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string u, int version = 0) { var result = new OASISResult<IAvatarDetail>(); try { EnsureCurrentVersion(version); var d = await ScanDetailsAsync(o => GetGraphQlStr(o, "username") == u); if (d == null) { OASISErrorHandling.HandleError(ref result, $"WeaviateOASIS: No detail for username '{u}'."); result.ErrorCode = OASISResultErrorCodes.RecordNotFound; return result; } result.Result = d; result.IsLoaded = true; result.LoadedCount = 1; result.Message = "WeaviateOASIS: AvatarDetail loaded."; } catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"WeaviateOASIS: {ex.Message}"); } return result; }
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string u, int version = 0) => LoadAvatarDetailByUsernameAsync(u, version).Result;
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string e, int version = 0) { var result = new OASISResult<IAvatarDetail>(); try { EnsureCurrentVersion(version); var d = await ScanDetailsAsync(o => GetGraphQlStr(o, "email") == e); if (d == null) { OASISErrorHandling.HandleError(ref result, $"WeaviateOASIS: No detail for email '{e}'."); result.ErrorCode = OASISResultErrorCodes.RecordNotFound; return result; } result.Result = d; result.IsLoaded = true; result.LoadedCount = 1; result.Message = "WeaviateOASIS: AvatarDetail loaded."; } catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"WeaviateOASIS: {ex.Message}"); } return result; }
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string e, int version = 0) => LoadAvatarDetailByEmailAsync(e, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            try { EnsureCurrentVersion(version); var objs = await GraphQlGetAsync("OasisAvatarDetail", new[] { "dataJson" }); var details = objs.Select(o => Des<AvatarDetail>(GetGraphQlStr(o, "dataJson"))).Where(d => d != null).Cast<IAvatarDetail>().ToList(); result.Result = details; result.IsLoaded = true; result.LoadedCount = details.Count; result.Message = $"WeaviateOASIS: Loaded {details.Count} detail(s)."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"WeaviateOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0) => LoadAllAvatarDetailsAsync(version).Result;

        // ─── Holon saving ─────────────────────────────────────────────────────────

        public override Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonGraphAsync(holon, saveChildren, recursive, maxChildDepth, 0, continueOnError);

        private async Task<OASISResult<IHolon>> SaveHolonGraphAsync(IHolon holon, bool saveChildren, bool recursive,
            int maxChildDepth, int currentChildDepth, bool continueOnError)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (IsVersionControlEnabled)
                    throw new NotSupportedException("Weaviate historical writes are not implemented.");

                var plan = HolonGraphWritePlan.CreateForSave(new[] { holon }, saveChildren, recursive, maxChildDepth, currentChildDepth);
                var failed = new HashSet<Guid>();
                foreach (var node in plan.Nodes)
                {
                    if (failed.Contains(node.Snapshot.ParentHolonId))
                    {
                        failed.Add(node.Snapshot.Id);
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
                        failed.Add(node.Snapshot.Id);
                        OASISErrorHandling.HandleError(ref result, $"Weaviate did not save holon '{node.Snapshot.Id}'.", ex);
                        if (!continueOnError)
                            break;
                    }
                }

                result.IsSaved = result.SavedCount == plan.Nodes.Count;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"WeaviateOASIS: {ex.Message}", ex);
            }
            return result;
        }

        private async Task SavePlannedHolonAsync(PlannedHolonWrite node)
        {
            var snapshot = node.Snapshot;
            var id = snapshot.Id.ToString("D");
            var keys = snapshot.ProviderUniqueStorageKey ?? new Dictionary<Core.Enums.ProviderType, string>();
            keys[Core.Enums.ProviderType.WeaviateOASIS] = id;
            var captured = JsonNode.Parse(node.CapturedIntentJson)!.AsObject();
            var document = new JsonObject();
            foreach (var property in captured)
            {
                if (property.Key != "$type")
                    document.Add(JsonNamingPolicy.CamelCase.ConvertName(property.Key), property.Value?.DeepClone());
            }
            document["id"] = id;
            document["parentHolonId"] = snapshot.ParentHolonId;
            document["providerUniqueStorageKey"] = JsonSerializer.SerializeToNode(keys, _jsonOpts);
            await PersistHolonAsync(snapshot.Id, snapshot.ParentHolonId, (int)snapshot.HolonType, snapshot.IsDeleted, document.ToJsonString(_jsonOpts));

            node.Source.Id = snapshot.Id;
            node.Source.ParentHolonId = snapshot.ParentHolonId;
            node.Source.ProviderUniqueStorageKey = keys;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var saved = new List<IHolon>();
            var errors = new List<string>();
            foreach (var holon in holons)
            {
                var write = await SaveHolonGraphAsync(holon, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError);
                result.SavedCount += write.SavedCount;
                if (write.Result != null) saved.Add(write.Result);
                if (write.IsError)
                {
                    errors.Add(write.Message);
                    result.Exception ??= write.Exception;
                    if (!continueOnError) break;
                }
            }
            result.Result = saved;
            result.IsError = errors.Count > 0;
            result.ErrorCount = errors.Count;
            result.IsSaved = result.SavedCount > 0;
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
                EnsureCurrentVersion(version);
                var obj = await GetObjectAsync("OasisHolon", id.ToString());
                if (obj == null || GetPropBool(obj.Value, "isDeleted")) { OASISErrorHandling.HandleError(ref result, $"WeaviateOASIS: No holon for ID '{id}'."); result.ErrorCode = OASISResultErrorCodes.RecordNotFound; return result; }
                var holon = Des<Holon>(GetProp(obj.Value, "dataJson")); if (holon == null) { OASISErrorHandling.HandleError(ref result, "WeaviateOASIS: Deserialise failed."); return result; }
                result.Result = holon; result.IsLoaded = true; result.LoadedCount = 1; result.Message = "WeaviateOASIS: Holon loaded.";
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
                EnsureCurrentVersion(version);
                var objs = await GraphQlGetAsync("OasisHolon", new[] { "isDeleted", "holonType", "dataJson" });
                var holons = objs.Where(o => !GetGraphQlBool(o, "isDeleted") && (type == HolonType.All || GetGraphQlInt(o, "holonType") == (int)type)).Select(o => Des<Holon>(GetGraphQlStr(o, "dataJson"))).Where(h => h != null).Cast<IHolon>().ToList();
                result.Result = holons; result.IsLoaded = true; result.LoadedCount = holons.Count; result.Message = $"WeaviateOASIS: Loaded {holons.Count} holon(s).";
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
                EnsureCurrentVersion(version);
                var idStr = id.ToString();
                var objs = await GraphQlGetAsync("OasisHolon", new[] { "parentHolonId", "isDeleted", "holonType", "dataJson" });
                var holons = objs.Where(o => !GetGraphQlBool(o, "isDeleted") && GetGraphQlStr(o, "parentHolonId") == idStr && (type == HolonType.All || GetGraphQlInt(o, "holonType") == (int)type)).Select(o => Des<Holon>(GetGraphQlStr(o, "dataJson"))).Where(h => h != null).Cast<IHolon>().ToList();
                result.Result = holons; result.IsLoaded = true; result.LoadedCount = holons.Count; result.Message = $"WeaviateOASIS: Loaded {holons.Count} holon(s) for parent '{id}'.";
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
                var loaded = await LoadHolonAsync(id, loadChildren: false); if (loaded.IsError || loaded.Result == null) return loaded;
                var holon = (Holon)loaded.Result; holon.DeletedDate = DateTime.UtcNow;
                await UpsertObjectAsync("OasisHolon", id.ToString(), new Dictionary<string, object?> { ["parentHolonId"] = holon.ParentHolonId.ToString(), ["holonType"] = (int)holon.HolonType, ["isDeleted"] = true, ["dataJson"] = Ser(holon) });
                result.Result = holon; result.IsDeleted = true; result.DeletedCount = 1; result.Message = $"WeaviateOASIS: Holon '{id}' soft-deleted.";
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
            try { string? q = searchParams.SearchGroups?.OfType<SearchTextGroup>().FirstOrDefault()?.SearchQuery?.ToLower(); var all = await LoadAllHolonsAsync(version: version); if (all.IsError) throw new InvalidOperationException(all.Message, all.Exception); var holons = all.Result?.ToList() ?? new List<IHolon>(); if (!string.IsNullOrEmpty(q)) holons = holons.Where(h => h.Name?.ToLower().Contains(q) == true || h.Description?.ToLower().Contains(q) == true).ToList(); result.Result = new SearchResults { SearchResultHolons = holons, NumberOfResults = holons.Count }; result.IsError = false; result.Message = $"WeaviateOASIS: Found {holons.Count} result(s)."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var all = await LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth,
                curentChildDepth, continueOnError, loadChildrenFromProvider, version);
            if (all.IsError)
                return all;

            var matching = all.Result.Where(holon => holon.MetaData != null &&
                holon.MetaData.TryGetValue(metaKey, out var value) && value?.ToString() == metaValue).ToList();
            return new OASISResult<IEnumerable<IHolon>>
            {
                Result = matching,
                IsLoaded = true,
                LoadedCount = matching.Count
            };
        }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKvp, MetaKeyValuePairMatchMode mode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        { var all = await LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version); if (all.IsError) return all; bool IsMatch(IHolon h) { if (h.MetaData == null) return false; var checks = metaKvp.Select(kvp => h.MetaData.TryGetValue(kvp.Key, out var v) && v?.ToString() == kvp.Value); return mode == MetaKeyValuePairMatchMode.Any ? checks.Any(c => c) : checks.All(c => c); } var holons = all.Result?.Where(IsMatch).ToList() ?? new List<IHolon>(); return new OASISResult<IEnumerable<IHolon>> { Result = holons, IsLoaded = true, LoadedCount = holons.Count }; }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKvp, MetaKeyValuePairMatchMode mode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsByMetaDataAsync(metaKvp, mode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        // ─── Import / Export ──────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var saved = await SaveHolonsAsync(holons);
            return new OASISResult<bool>
            {
                Result = !saved.IsError && saved.IsSaved,
                IsError = saved.IsError,
                IsSaved = saved.IsSaved,
                SavedCount = saved.SavedCount,
                ErrorCount = saved.ErrorCount,
                Message = saved.Message,
                Exception = saved.Exception
            };
        }
        public override OASISResult<bool> Import(IEnumerable<IHolon> holons) => ImportAsync(holons).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0) => await LoadAllHolonsAsync(version: version);
        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0) => ExportAllAsync(version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0) { var all = await LoadAllHolonsAsync(version: version); if (all.IsError) return all; var h = all.Result?.Where(x => x.CreatedByAvatarId == avatarId).ToList() ?? new List<IHolon>(); return new OASISResult<IEnumerable<IHolon>> { Result = h, IsLoaded = true, LoadedCount = h.Count }; }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0) => ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string u, int version = 0) { var a = await LoadAvatarByUsernameAsync(u); if (a.IsError || a.Result == null) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "Not found."); return r; } return await ExportAllDataForAvatarByIdAsync(a.Result.Id, version); }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string u, int version = 0) => ExportAllDataForAvatarByUsernameAsync(u, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string e, int version = 0) { var a = await LoadAvatarByEmailAsync(e); if (a.IsError || a.Result == null) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "Not found."); return r; } return await ExportAllDataForAvatarByIdAsync(a.Result.Id, version); }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string e, int version = 0) => ExportAllDataForAvatarByEmailAsync(e, version).Result;

        public bool IsVersionControlEnabled { get; set; } = false;
    }
}
