using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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

namespace NextGenSoftware.OASIS.API.Providers.AppwriteOASIS
{
    /// <summary>
    /// OASIS provider for Appwrite via the Databases REST API.
    ///
    /// Appwrite is a self-hostable open-source Firebase/Supabase alternative.
    /// Uses Appwrite's Databases API — no Appwrite SDK required.
    ///
    /// Pre-requisites — create in Appwrite console:
    ///   1. A Database (note its ID → databaseId)
    ///   2. Three Collections in that database:
    ///        oasis_avatars      — attributes: oasis_id(string), username(string), email(string), is_deleted(boolean), data_json(string/64KB+)
    ///        oasis_avatar_details — attributes: oasis_id(string), username(string), email(string), data_json(string/64KB+)
    ///        oasis_holons       — attributes: oasis_id(string), parent_holon_id(string), holon_type(integer), is_deleted(boolean), data_json(string/64KB+)
    ///   3. Add indexes on oasis_id (unique), username, email, parent_holon_id, holon_type as needed.
    ///   4. An API key with databases.read and databases.write scopes.
    ///
    /// Constructor parameters:
    ///   appwriteUrl — e.g. "https://cloud.appwrite.io/v1" or your self-hosted URL
    ///   projectId   — Appwrite Project ID
    ///   apiKey      — Appwrite API key with database scopes
    ///   databaseId  — Database ID containing the three OASIS collections
    /// </summary>
    public class AppwriteOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISDBStorageProvider
    {
        private readonly HttpClient _http;
        private readonly string _baseUrl;
        private readonly string _databaseId;

        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public AppwriteOASIS(string appwriteUrl, string projectId, string apiKey, string databaseId)
        {
            _baseUrl = appwriteUrl.TrimEnd('/');
            _databaseId = databaseId;
            _http = new HttpClient();
            _http.DefaultRequestHeaders.Add("X-Appwrite-Project", projectId);
            _http.DefaultRequestHeaders.Add("X-Appwrite-Key", apiKey);
            _http.DefaultRequestHeaders.Add("X-Appwrite-Response-Format", "1.0.0");
            ProviderName = "AppwriteOASIS";
            ProviderDescription = "Appwrite provider (Databases REST API)";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.AppwriteOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageLocalAndNetwork);
        }

        // ─── Activation ───────────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                var response = await _http.GetAsync($"{_baseUrl}/databases/{_databaseId}");
                if (!response.IsSuccessStatusCode) throw new Exception($"HTTP {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
                result.Result = true; result.IsError = false; result.Message = "AppwriteOASIS activated — connected to Appwrite Databases API.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"AppwriteOASIS: Error activating provider — {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;
        public override async Task<OASISResult<bool>> DeActivateProviderAsync() => await Task.FromResult(new OASISResult<bool> { Result = true, IsError = false, Message = "AppwriteOASIS deactivated." });
        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // ─── Helpers ─────────────────────────────────────────────────────────────

        private static string Ser(object obj) => JsonSerializer.Serialize(obj, _jsonOpts);
        private static T? Des<T>(string? json) => json == null ? default : JsonSerializer.Deserialize<T>(json, _jsonOpts);
        private StringContent JsonBody(object obj) => new StringContent(Ser(obj), Encoding.UTF8, "application/json");

        private string CollUrl(string collection) => $"{_baseUrl}/databases/{_databaseId}/collections/{collection}/documents";

        // Appwrite document list response
        private class AppwriteList { [JsonPropertyName("documents")] public List<AppwriteDoc>? Documents { get; set; } [JsonPropertyName("total")] public int Total { get; set; } }
        private class AppwriteDoc
        {
            [JsonPropertyName("$id")] public string Id { get; set; } = "";
            [JsonPropertyName("oasis_id")] public string? OasisId { get; set; }
            [JsonPropertyName("data_json")] public string? DataJson { get; set; }
            [JsonPropertyName("is_deleted")] public bool IsDeleted { get; set; }
        }

        private async Task<AppwriteDoc?> GetFirstAsync(string collection, string[] queries)
        {
            var qParams = string.Join("&", queries.Select(q => $"queries[]={Uri.EscapeDataString(q)}"));
            var response = await _http.GetAsync($"{CollUrl(collection)}?limit=1&{qParams}");
            if (!response.IsSuccessStatusCode) return null;
            var list = Des<AppwriteList>(await response.Content.ReadAsStringAsync());
            return list?.Documents?.FirstOrDefault();
        }

        private async Task<List<AppwriteDoc>> GetAllAsync(string collection, string[]? queries = null)
        {
            var all = new List<AppwriteDoc>();
            int offset = 0;
            while (true)
            {
                var qParams = queries != null ? "&" + string.Join("&", queries.Select(q => $"queries[]={Uri.EscapeDataString(q)}")) : "";
                var response = await _http.GetAsync($"{CollUrl(collection)}?limit=100&offset={offset}{qParams}");
                if (!response.IsSuccessStatusCode) break;
                var list = Des<AppwriteList>(await response.Content.ReadAsStringAsync());
                if (list?.Documents == null || list.Documents.Count == 0) break;
                all.AddRange(list.Documents);
                if (all.Count >= list.Total) break;
                offset += list.Documents.Count;
            }
            return all;
        }

        // Appwrite query helpers — Appwrite Query syntax: Query.equal("field", ["value"])
        private static string Q(string field, string value) => $"equal(\"{field}\", [\"{value}\"])";
        private static string QBool(string field, bool value) => $"equal(\"{field}\", [{(value ? "true" : "false")})";

        private async Task<string> UpsertDocAsync(string collection, string oasisId, object data)
        {
            // Check for existing document by oasis_id
            var existing = await GetFirstAsync(collection, new[] { Q("oasis_id", oasisId) });
            HttpResponseMessage response;
            if (existing != null)
            {
                response = await _http.PatchAsync($"{CollUrl(collection)}/{existing.Id}", JsonBody(data));
            }
            else
            {
                // Use oasisId as document ID (Appwrite allows custom IDs)
                response = await _http.PostAsync(CollUrl(collection), JsonBody(new Dictionary<string, object> { ["documentId"] = oasisId }.Concat(((JsonElement)JsonSerializer.SerializeToElement(data, _jsonOpts)).EnumerateObject().Select(p => new KeyValuePair<string, object>(p.Name, p.Value))).ToDictionary(k => k.Key, v => v.Value)));
            }
            if (!response.IsSuccessStatusCode) throw new Exception($"Appwrite upsert failed: {await response.Content.ReadAsStringAsync()}");
            var doc = Des<AppwriteDoc>(await response.Content.ReadAsStringAsync());
            return doc?.Id ?? oasisId;
        }

        // ─── Avatar saving ────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (avatar.Id == Guid.Empty) avatar.Id = Guid.NewGuid();
                if (avatar.ProviderUniqueStorageKey == null) avatar.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.AppwriteOASIS] = avatar.Id.ToString();

                var existing = await GetFirstAsync("oasis_avatars", new[] { Q("oasis_id", avatar.Id.ToString()) });
                var doc = new { oasis_id = avatar.Id.ToString(), username = avatar.Username ?? "", email = avatar.Email ?? "", is_deleted = avatar.IsDeleted, data_json = Ser(avatar) };
                HttpResponseMessage response;
                if (existing != null)
                    response = await _http.PatchAsync($"{CollUrl("oasis_avatars")}/{existing.Id}", JsonBody(doc));
                else
                    response = await _http.PostAsync(CollUrl("oasis_avatars"), JsonBody(new { documentId = avatar.Id.ToString(), data = doc }));
                if (!response.IsSuccessStatusCode) throw new Exception(await response.Content.ReadAsStringAsync());
                result.Result = avatar; result.IsError = false; result.Message = $"AppwriteOASIS: Avatar '{avatar.Username}' saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"AppwriteOASIS: Error saving avatar: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) => SaveAvatarAsync(avatar).Result;

        // ─── Avatar loading ───────────────────────────────────────────────────────

        private Avatar? ParseAvatar(AppwriteDoc? doc) => doc == null || doc.IsDeleted ? null : Des<Avatar>(doc.DataJson);

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var doc = await GetFirstAsync("oasis_avatars", new[] { Q("oasis_id", id.ToString()), QBool("is_deleted", false) });
                var avatar = ParseAvatar(doc);
                if (avatar == null) { OASISErrorHandling.HandleError(ref result, $"AppwriteOASIS: No avatar found for ID '{id}'."); return result; }
                result.Result = avatar; result.IsError = false; result.Message = $"AppwriteOASIS: Avatar loaded for ID '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"AppwriteOASIS: Error loading avatar '{id}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) => LoadAvatarAsync(id, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var doc = await GetFirstAsync("oasis_avatars", new[] { Q("username", username), QBool("is_deleted", false) });
                var avatar = ParseAvatar(doc);
                if (avatar == null) { OASISErrorHandling.HandleError(ref result, $"AppwriteOASIS: No avatar found for username '{username}'."); return result; }
                result.Result = avatar; result.IsError = false; result.Message = $"AppwriteOASIS: Avatar loaded for username '{username}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"AppwriteOASIS: Error loading avatar by username '{username}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string username, int version = 0) => LoadAvatarByUsernameAsync(username, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var doc = await GetFirstAsync("oasis_avatars", new[] { Q("email", avatarEmail), QBool("is_deleted", false) });
                var avatar = ParseAvatar(doc);
                if (avatar == null) { OASISErrorHandling.HandleError(ref result, $"AppwriteOASIS: No avatar found for email '{avatarEmail}'."); return result; }
                result.Result = avatar; result.IsError = false; result.Message = $"AppwriteOASIS: Avatar loaded for email '{avatarEmail}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"AppwriteOASIS: Error loading avatar by email '{avatarEmail}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0) => LoadAvatarByEmailAsync(avatarEmail, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            if (Guid.TryParse(providerKey, out Guid id)) return await LoadAvatarAsync(id, version);
            var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, $"AppwriteOASIS: providerKey '{providerKey}' is not a valid GUID."); return r;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0) => LoadAvatarByProviderKeyAsync(providerKey, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                var docs = await GetAllAsync("oasis_avatars", new[] { QBool("is_deleted", false) });
                var avatars = docs.Select(ParseAvatar).Where(a => a != null).Cast<IAvatar>().ToList();
                result.Result = avatars; result.IsError = false; result.Message = $"AppwriteOASIS: Loaded {avatars.Count} avatar(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"AppwriteOASIS: Error loading all avatars: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) => LoadAllAvatarsAsync(version).Result;

        // ─── Avatar deletion ──────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                var doc = await GetFirstAsync("oasis_avatars", new[] { Q("oasis_id", id.ToString()) });
                if (doc == null) { OASISErrorHandling.HandleError(ref result, $"AppwriteOASIS: Avatar '{id}' not found."); return result; }
                HttpResponseMessage response;
                if (softDelete)
                    response = await _http.PatchAsync($"{CollUrl("oasis_avatars")}/{doc.Id}", JsonBody(new { is_deleted = true }));
                else
                    response = await _http.DeleteAsync($"{CollUrl("oasis_avatars")}/{doc.Id}");
                result.Result = response.IsSuccessStatusCode; result.IsError = !result.Result;
                result.Message = result.Result ? $"AppwriteOASIS: Avatar '{id}' {(softDelete ? "soft" : "hard")}-deleted." : "AppwriteOASIS: Delete failed.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"AppwriteOASIS: Error deleting avatar '{id}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) => DeleteAvatarAsync(id, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string username, bool softDelete = true)
        {
            var a = await LoadAvatarByUsernameAsync(username);
            if (a.IsError || a.Result == null) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, $"AppwriteOASIS: Avatar '{username}' not found."); return r; }
            return await DeleteAvatarAsync(a.Result.Id, softDelete);
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string username, bool softDelete = true) => DeleteAvatarByUsernameAsync(username, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string email, bool softDelete = true)
        {
            var a = await LoadAvatarByEmailAsync(email);
            if (a.IsError || a.Result == null) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, $"AppwriteOASIS: Avatar with email '{email}' not found."); return r; }
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
                var existing = await GetFirstAsync("oasis_avatar_details", new[] { Q("oasis_id", avatarDetail.Id.ToString()) });
                var doc = new { oasis_id = avatarDetail.Id.ToString(), username = avatarDetail.Username ?? "", email = avatarDetail.Email ?? "", data_json = Ser(avatarDetail) };
                HttpResponseMessage response;
                if (existing != null)
                    response = await _http.PatchAsync($"{CollUrl("oasis_avatar_details")}/{existing.Id}", JsonBody(doc));
                else
                    response = await _http.PostAsync(CollUrl("oasis_avatar_details"), JsonBody(new { documentId = avatarDetail.Id.ToString(), data = doc }));
                if (!response.IsSuccessStatusCode) throw new Exception(await response.Content.ReadAsStringAsync());
                result.Result = avatarDetail; result.IsError = false; result.Message = "AppwriteOASIS: AvatarDetail saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"AppwriteOASIS: Error saving avatar detail: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail) => SaveAvatarDetailAsync(avatarDetail).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var doc = await GetFirstAsync("oasis_avatar_details", new[] { Q("oasis_id", id.ToString()) });
                if (doc?.DataJson == null) { OASISErrorHandling.HandleError(ref result, $"AppwriteOASIS: No avatar detail found for ID '{id}'."); return result; }
                result.Result = Des<AvatarDetail>(doc.DataJson); result.IsError = false; result.Message = $"AppwriteOASIS: AvatarDetail loaded for ID '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"AppwriteOASIS: Error loading avatar detail '{id}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => LoadAvatarDetailAsync(id, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var doc = await GetFirstAsync("oasis_avatar_details", new[] { Q("username", username) });
                if (doc?.DataJson == null) { OASISErrorHandling.HandleError(ref result, $"AppwriteOASIS: No avatar detail found for username '{username}'."); return result; }
                result.Result = Des<AvatarDetail>(doc.DataJson); result.IsError = false; result.Message = $"AppwriteOASIS: AvatarDetail loaded for username '{username}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"AppwriteOASIS: Error loading avatar detail by username '{username}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string username, int version = 0) => LoadAvatarDetailByUsernameAsync(username, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string email, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var doc = await GetFirstAsync("oasis_avatar_details", new[] { Q("email", email) });
                if (doc?.DataJson == null) { OASISErrorHandling.HandleError(ref result, $"AppwriteOASIS: No avatar detail found for email '{email}'."); return result; }
                result.Result = Des<AvatarDetail>(doc.DataJson); result.IsError = false; result.Message = $"AppwriteOASIS: AvatarDetail loaded for email '{email}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"AppwriteOASIS: Error loading avatar detail by email '{email}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string email, int version = 0) => LoadAvatarDetailByEmailAsync(email, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                var docs = await GetAllAsync("oasis_avatar_details");
                var details = docs.Where(d => d.DataJson != null).Select(d => Des<AvatarDetail>(d.DataJson!)).Where(d => d != null).Cast<IAvatarDetail>().ToList();
                result.Result = details; result.IsError = false; result.Message = $"AppwriteOASIS: Loaded {details.Count} avatar detail(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"AppwriteOASIS: Error loading all avatar details: {ex.Message}"); }
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
                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.AppwriteOASIS] = holon.Id.ToString();

                var existing = await GetFirstAsync("oasis_holons", new[] { Q("oasis_id", holon.Id.ToString()) });
                var doc = new { oasis_id = holon.Id.ToString(), parent_holon_id = holon.ParentHolonId == Guid.Empty ? "" : holon.ParentHolonId.ToString(), holon_type = (int)holon.HolonType, is_deleted = holon.IsDeleted, data_json = Ser(holon) };
                HttpResponseMessage response;
                if (existing != null)
                    response = await _http.PatchAsync($"{CollUrl("oasis_holons")}/{existing.Id}", JsonBody(doc));
                else
                    response = await _http.PostAsync(CollUrl("oasis_holons"), JsonBody(new { documentId = holon.Id.ToString(), data = doc }));
                if (!response.IsSuccessStatusCode) throw new Exception(await response.Content.ReadAsStringAsync());
                result.Result = holon; result.IsError = false; result.Message = $"AppwriteOASIS: Holon '{holon.Name}' saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"AppwriteOASIS: Error saving holon '{holon.Name}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var saved = new List<IHolon>(); var errors = new List<string>();
            foreach (var holon in holons) { var r = await SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider); if (r.IsError) errors.Add(r.Message ?? ""); else if (r.Result != null) saved.Add(r.Result); }
            result.Result = saved; result.IsError = errors.Count > 0; result.Message = errors.Count > 0 ? string.Join("; ", errors) : $"AppwriteOASIS: {saved.Count} holon(s) saved.";
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;

        // ─── Holon loading ────────────────────────────────────────────────────────

        private Holon? ParseHolon(AppwriteDoc? doc) => doc == null || doc.IsDeleted ? null : Des<Holon>(doc.DataJson);

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var doc = await GetFirstAsync("oasis_holons", new[] { Q("oasis_id", id.ToString()), QBool("is_deleted", false) });
                var holon = ParseHolon(doc);
                if (holon == null) { OASISErrorHandling.HandleError(ref result, $"AppwriteOASIS: No holon found for ID '{id}'."); return result; }
                result.Result = holon; result.IsError = false; result.Message = $"AppwriteOASIS: Holon loaded for ID '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"AppwriteOASIS: Error loading holon '{id}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            if (Guid.TryParse(providerKey, out Guid id)) return await LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);
            var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, $"AppwriteOASIS: providerKey '{providerKey}' is not a valid GUID."); return r;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var queries = type == HolonType.All ? new[] { QBool("is_deleted", false) } : new[] { QBool("is_deleted", false), Q("holon_type", ((int)type).ToString()) };
                var docs = await GetAllAsync("oasis_holons", queries);
                var holons = docs.Select(ParseHolon).Where(h => h != null).Cast<IHolon>().ToList();
                result.Result = holons; result.IsError = false; result.Message = $"AppwriteOASIS: Loaded {holons.Count} holon(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"AppwriteOASIS: Error loading all holons: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var queries = type == HolonType.All ? new[] { Q("parent_holon_id", id.ToString()), QBool("is_deleted", false) } : new[] { Q("parent_holon_id", id.ToString()), QBool("is_deleted", false), Q("holon_type", ((int)type).ToString()) };
                var docs = await GetAllAsync("oasis_holons", queries);
                var holons = docs.Select(ParseHolon).Where(h => h != null).Cast<IHolon>().ToList();
                result.Result = holons; result.IsError = false; result.Message = $"AppwriteOASIS: Loaded {holons.Count} holon(s) for parent '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"AppwriteOASIS: Error loading holons for parent '{id}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            if (Guid.TryParse(providerKey, out Guid id)) return await LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
            var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, $"AppwriteOASIS: providerKey '{providerKey}' is not a valid GUID."); return r;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        // ─── Holon deletion ───────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var doc = await GetFirstAsync("oasis_holons", new[] { Q("oasis_id", id.ToString()) });
                if (doc == null) { OASISErrorHandling.HandleError(ref result, $"AppwriteOASIS: No holon found with ID '{id}'."); return result; }
                var holon = ParseHolon(doc) ?? Des<Holon>(doc.DataJson);
                await _http.PatchAsync($"{CollUrl("oasis_holons")}/{doc.Id}", JsonBody(new { is_deleted = true }));
                result.Result = holon; result.IsError = false; result.Message = $"AppwriteOASIS: Holon '{id}' soft-deleted.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id) => DeleteHolonAsync(id).Result;

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            if (Guid.TryParse(providerKey, out Guid id)) return await DeleteHolonAsync(id);
            var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, $"AppwriteOASIS: providerKey '{providerKey}' is not a valid GUID."); return r;
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
                result.IsError = false; result.Message = $"AppwriteOASIS: Found {holons.Count} holon(s).";
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
            return new OASISResult<IEnumerable<IHolon>> { Result = holons, IsError = false, Message = $"AppwriteOASIS: Loaded {holons.Count} holon(s) where {metaKey}={metaValue}." };
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var all = await LoadAllHolonsAsync(type);
            bool IsMatch(IHolon h) { if (h.MetaData == null) return false; var checks = metaKeyValuePairs.Select(kvp => h.MetaData.TryGetValue(kvp.Key, out var v) && v?.ToString() == kvp.Value); return metaKeyValuePairMatchMode == MetaKeyValuePairMatchMode.Any ? checks.Any(c => c) : checks.All(c => c); }
            var holons = all.Result?.Where(IsMatch).ToList() ?? new List<IHolon>();
            return new OASISResult<IEnumerable<IHolon>> { Result = holons, IsError = false, Message = $"AppwriteOASIS: Loaded {holons.Count} holon(s) matching metadata filter." };
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
            return new OASISResult<IEnumerable<IHolon>> { Result = holons, IsError = false, Message = $"AppwriteOASIS: Exported {holons.Count} holon(s) for avatar '{avatarId}'." };
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0) => ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string u, int version = 0) { var a = await LoadAvatarByUsernameAsync(u); if (a.IsError || a.Result == null) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, $"AppwriteOASIS: Avatar '{u}' not found."); return r; } return await ExportAllDataForAvatarByIdAsync(a.Result.Id, version); }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string u, int version = 0) => ExportAllDataForAvatarByUsernameAsync(u, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string e, int version = 0) { var a = await LoadAvatarByEmailAsync(e); if (a.IsError || a.Result == null) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, $"AppwriteOASIS: Avatar with email '{e}' not found."); return r; } return await ExportAllDataForAvatarByIdAsync(a.Result.Id, version); }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string e, int version = 0) => ExportAllDataForAvatarByEmailAsync(e, version).Result;

        public bool IsVersionControlEnabled { get; set; } = false;
    }
}
