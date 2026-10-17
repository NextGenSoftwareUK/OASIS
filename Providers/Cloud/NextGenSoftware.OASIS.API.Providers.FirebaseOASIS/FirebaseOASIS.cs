using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
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
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.FirebaseOASIS
{
    /// <summary>
    /// OASIS provider for Google Firebase Realtime Database.
    ///
    /// Uses the Firebase REST API (no Firebase SDK — pure System.Net.Http).
    /// Data layout under the database root:
    ///   /oasis_avatars/{guid}        — Avatar JSON
    ///   /oasis_avatar_details/{guid} — AvatarDetail JSON
    ///   /oasis_holons/{guid}         — Holon JSON
    ///
    /// Authentication: pass a Firebase ID token (from Firebase Auth) or a server secret.
    /// For service-account access use the database secret (Firebase console → Project settings
    /// → Service accounts → Database secrets) as the auth token.
    ///
    /// Constructor parameters:
    ///   databaseUrl  — e.g. "https://my-project-default-rtdb.firebaseio.com"
    ///   authToken    — Firebase ID token or database secret (optional for public databases)
    /// </summary>
    public class FirebaseOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISDBStorageProvider
    {
        private readonly HttpClient _http;
        private readonly string _baseUrl;
        private readonly string? _authToken;

        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public FirebaseOASIS(string databaseUrl, string? authToken = null)
        {
            _baseUrl = databaseUrl.TrimEnd('/');
            _authToken = authToken;
            _http = new HttpClient();
            ProviderName = "FirebaseOASIS";
            ProviderDescription = "Google Firebase Realtime Database provider (REST API, JSON documents)";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.FirebaseOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageLocalAndNetwork);
        }

        // ─── Activation ───────────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                // Ping the database root to verify connectivity and credentials
                var response = await _http.GetAsync(NodeUrl("/.info/connected"));
                response.EnsureSuccessStatusCode();
                result.Result = true;
                result.IsError = false;
                result.Message = "FirebaseOASIS activated — connected to Firebase Realtime Database.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"FirebaseOASIS: Error activating provider — {ex.Message}");
            }
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
            => await Task.FromResult(new OASISResult<bool> { Result = true, IsError = false, Message = "FirebaseOASIS deactivated." });

        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // ─── URL helpers ──────────────────────────────────────────────────────────

        private string NodeUrl(string path)
        {
            string url = $"{_baseUrl}{path}.json";
            if (!string.IsNullOrEmpty(_authToken))
                url += $"?auth={Uri.EscapeDataString(_authToken)}";
            return url;
        }

        private string QueryUrl(string collection, string field, string value)
        {
            string url = $"{_baseUrl}/{collection}.json?orderBy=\"{field}\"&equalTo=\"{Uri.EscapeDataString(value)}\"";
            if (!string.IsNullOrEmpty(_authToken))
                url += $"&auth={Uri.EscapeDataString(_authToken)}";
            return url;
        }

        private static string Serialize(object obj) => JsonSerializer.Serialize(obj, _jsonOpts);

        private static T? Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, _jsonOpts);

        // ─── Low-level CRUD ───────────────────────────────────────────────────────

        private async Task PutAsync(string path, object data)
        {
            var content = new StringContent(Serialize(data), Encoding.UTF8, "application/json");
            var response = await _http.PutAsync(NodeUrl(path), content);
            response.EnsureSuccessStatusCode();
        }

        private async Task<T?> GetAsync<T>(string path)
        {
            var response = await _http.GetAsync(NodeUrl(path));
            if (!response.IsSuccessStatusCode) return default;
            var json = await response.Content.ReadAsStringAsync();
            if (json == "null" || string.IsNullOrWhiteSpace(json)) return default;
            return Deserialize<T>(json);
        }

        private async Task<List<T>> QueryByFieldAsync<T>(string collection, string field, string value)
        {
            var url = QueryUrl(collection, field, value);
            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode) return new List<T>();
            var json = await response.Content.ReadAsStringAsync();
            if (json == "null" || string.IsNullOrWhiteSpace(json)) return new List<T>();
            var node = JsonNode.Parse(json);
            if (node is not JsonObject obj) return new List<T>();
            return obj.Select(kvp => Deserialize<T>(kvp.Value!.ToJsonString())).Where(v => v != null).Select(v => v!).ToList();
        }

        private async Task<List<T>> GetAllAsync<T>(string collection)
        {
            var response = await _http.GetAsync(NodeUrl($"/{collection}"));
            if (!response.IsSuccessStatusCode) return new List<T>();
            var json = await response.Content.ReadAsStringAsync();
            if (json == "null" || string.IsNullOrWhiteSpace(json)) return new List<T>();
            var node = JsonNode.Parse(json);
            if (node is not JsonObject obj) return new List<T>();
            return obj.Select(kvp => Deserialize<T>(kvp.Value!.ToJsonString())).Where(v => v != null).Select(v => v!).ToList();
        }

        private async Task<bool> PatchAsync(string path, object patch)
        {
            var content = new StringContent(Serialize(patch), Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(new HttpMethod("PATCH"), NodeUrl(path)) { Content = content };
            var response = await _http.SendAsync(request);
            return response.IsSuccessStatusCode;
        }

        private async Task<bool> DeleteNodeAsync(string path)
        {
            var response = await _http.DeleteAsync(NodeUrl(path));
            return response.IsSuccessStatusCode;
        }

        // ─── Avatar saving ────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (avatar.Id == Guid.Empty) avatar.Id = Guid.NewGuid();
                if (avatar.ProviderUniqueStorageKey == null)
                    avatar.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.FirebaseOASIS] = avatar.Id.ToString();
                await PutAsync($"/oasis_avatars/{avatar.Id}", avatar);
                result.Result = avatar; result.IsError = false; result.Message = $"FirebaseOASIS: Avatar '{avatar.Username}' saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"FirebaseOASIS: Error saving avatar '{avatar.Username}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) => SaveAvatarAsync(avatar).Result;

        // ─── Avatar loading ───────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var avatar = await GetAsync<Avatar>($"/oasis_avatars/{id}");
                if (avatar == null || avatar.IsDeleted) { OASISErrorHandling.HandleError(ref result, $"FirebaseOASIS: No avatar found with ID '{id}'."); return result; }
                result.Result = avatar; result.IsError = false; result.Message = $"FirebaseOASIS: Avatar loaded for ID '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"FirebaseOASIS: Error loading avatar by ID '{id}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) => LoadAvatarAsync(id, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var avatars = await QueryByFieldAsync<Avatar>("oasis_avatars", "username", username);
                var avatar = avatars.FirstOrDefault(a => !a.IsDeleted);
                if (avatar == null) { OASISErrorHandling.HandleError(ref result, $"FirebaseOASIS: No avatar found with username '{username}'."); return result; }
                result.Result = avatar; result.IsError = false; result.Message = $"FirebaseOASIS: Avatar loaded for username '{username}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"FirebaseOASIS: Error loading avatar by username '{username}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string username, int version = 0) => LoadAvatarByUsernameAsync(username, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var avatars = await QueryByFieldAsync<Avatar>("oasis_avatars", "email", avatarEmail);
                var avatar = avatars.FirstOrDefault(a => !a.IsDeleted);
                if (avatar == null) { OASISErrorHandling.HandleError(ref result, $"FirebaseOASIS: No avatar found with email '{avatarEmail}'."); return result; }
                result.Result = avatar; result.IsError = false; result.Message = $"FirebaseOASIS: Avatar loaded for email '{avatarEmail}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"FirebaseOASIS: Error loading avatar by email '{avatarEmail}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0) => LoadAvatarByEmailAsync(avatarEmail, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            if (Guid.TryParse(providerKey, out Guid id)) return await LoadAvatarAsync(id, version);
            var result = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref result, $"FirebaseOASIS: providerKey '{providerKey}' is not a valid GUID.");
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0) => LoadAvatarByProviderKeyAsync(providerKey, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                var all = await GetAllAsync<Avatar>("oasis_avatars");
                var avatars = all.Where(a => !a.IsDeleted).Cast<IAvatar>().ToList();
                result.Result = avatars; result.IsError = false; result.Message = $"FirebaseOASIS: Loaded {avatars.Count} avatar(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"FirebaseOASIS: Error loading all avatars: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) => LoadAllAvatarsAsync(version).Result;

        // ─── Avatar deletion ──────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                bool ok;
                if (softDelete)
                    ok = await PatchAsync($"/oasis_avatars/{id}", new { isDeleted = true });
                else
                    ok = await DeleteNodeAsync($"/oasis_avatars/{id}");
                result.Result = ok; result.IsError = !ok;
                result.Message = ok ? $"FirebaseOASIS: Avatar '{id}' {(softDelete ? "soft" : "hard")}-deleted." : $"FirebaseOASIS: Failed to delete avatar '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"FirebaseOASIS: Error deleting avatar '{id}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) => DeleteAvatarAsync(id, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string username, bool softDelete = true)
        {
            var loaded = await LoadAvatarByUsernameAsync(username);
            if (loaded.IsError || loaded.Result == null)
            {
                var r = new OASISResult<bool>();
                OASISErrorHandling.HandleError(ref r, $"FirebaseOASIS: Avatar '{username}' not found.");
                return r;
            }
            return await DeleteAvatarAsync(loaded.Result.Id, softDelete);
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string username, bool softDelete = true) => DeleteAvatarByUsernameAsync(username, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string email, bool softDelete = true)
        {
            var loaded = await LoadAvatarByEmailAsync(email);
            if (loaded.IsError || loaded.Result == null)
            {
                var r = new OASISResult<bool>();
                OASISErrorHandling.HandleError(ref r, $"FirebaseOASIS: Avatar with email '{email}' not found.");
                return r;
            }
            return await DeleteAvatarAsync(loaded.Result.Id, softDelete);
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
                await PutAsync($"/oasis_avatar_details/{avatarDetail.Id}", avatarDetail);
                result.Result = avatarDetail; result.IsError = false; result.Message = "FirebaseOASIS: AvatarDetail saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"FirebaseOASIS: Error saving avatar detail: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail) => SaveAvatarDetailAsync(avatarDetail).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var detail = await GetAsync<AvatarDetail>($"/oasis_avatar_details/{id}");
                if (detail == null) { OASISErrorHandling.HandleError(ref result, $"FirebaseOASIS: No avatar detail found for ID '{id}'."); return result; }
                result.Result = detail; result.IsError = false; result.Message = $"FirebaseOASIS: AvatarDetail loaded for ID '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"FirebaseOASIS: Error loading avatar detail for '{id}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => LoadAvatarDetailAsync(id, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var details = await QueryByFieldAsync<AvatarDetail>("oasis_avatar_details", "username", username);
                var detail = details.FirstOrDefault();
                if (detail == null) { OASISErrorHandling.HandleError(ref result, $"FirebaseOASIS: No avatar detail found for username '{username}'."); return result; }
                result.Result = detail; result.IsError = false; result.Message = $"FirebaseOASIS: AvatarDetail loaded for username '{username}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"FirebaseOASIS: Error loading avatar detail by username '{username}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string username, int version = 0) => LoadAvatarDetailByUsernameAsync(username, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string email, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var details = await QueryByFieldAsync<AvatarDetail>("oasis_avatar_details", "email", email);
                var detail = details.FirstOrDefault();
                if (detail == null) { OASISErrorHandling.HandleError(ref result, $"FirebaseOASIS: No avatar detail found for email '{email}'."); return result; }
                result.Result = detail; result.IsError = false; result.Message = $"FirebaseOASIS: AvatarDetail loaded for email '{email}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"FirebaseOASIS: Error loading avatar detail by email '{email}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string email, int version = 0) => LoadAvatarDetailByEmailAsync(email, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                var all = await GetAllAsync<AvatarDetail>("oasis_avatar_details");
                result.Result = all.Cast<IAvatarDetail>().ToList(); result.IsError = false;
                result.Message = $"FirebaseOASIS: Loaded {all.Count} avatar detail(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"FirebaseOASIS: Error loading all avatar details: {ex.Message}"); }
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
                if (holon.ProviderUniqueStorageKey == null)
                    holon.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.FirebaseOASIS] = holon.Id.ToString();
                await PutAsync($"/oasis_holons/{holon.Id}", holon);
                result.Result = holon; result.IsError = false; result.Message = $"FirebaseOASIS: Holon '{holon.Name}' saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"FirebaseOASIS: Error saving holon '{holon.Name}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var saved = new List<IHolon>(); var errors = new List<string>();
            foreach (var holon in holons)
            {
                var r = await SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
                if (r.IsError) errors.Add(r.Message ?? ""); else if (r.Result != null) saved.Add(r.Result);
            }
            result.Result = saved; result.IsError = errors.Count > 0;
            result.Message = errors.Count > 0 ? string.Join("; ", errors) : $"FirebaseOASIS: {saved.Count} holon(s) saved.";
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;

        // ─── Holon loading ────────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var holon = await GetAsync<Holon>($"/oasis_holons/{id}");
                if (holon == null || holon.IsDeleted) { OASISErrorHandling.HandleError(ref result, $"FirebaseOASIS: No holon found with ID '{id}'."); return result; }
                result.Result = holon; result.IsError = false; result.Message = $"FirebaseOASIS: Holon loaded for ID '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"FirebaseOASIS: Error loading holon '{id}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            if (Guid.TryParse(providerKey, out Guid id)) return await LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);
            var result = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref result, $"FirebaseOASIS: providerKey '{providerKey}' is not a valid GUID.");
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var all = await GetAllAsync<Holon>("oasis_holons");
                var holons = (type == HolonType.All ? all.Where(h => !h.IsDeleted) : all.Where(h => !h.IsDeleted && h.HolonType == type)).Cast<IHolon>().ToList();
                result.Result = holons; result.IsError = false; result.Message = $"FirebaseOASIS: Loaded {holons.Count} holon(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"FirebaseOASIS: Error loading all holons: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var all = await QueryByFieldAsync<Holon>("oasis_holons", "parentHolonId", id.ToString());
                var holons = (type == HolonType.All ? all.Where(h => !h.IsDeleted) : all.Where(h => !h.IsDeleted && h.HolonType == type)).Cast<IHolon>().ToList();
                result.Result = holons; result.IsError = false; result.Message = $"FirebaseOASIS: Loaded {holons.Count} holon(s) for parent '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"FirebaseOASIS: Error loading holons for parent '{id}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            if (Guid.TryParse(providerKey, out Guid id)) return await LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, $"FirebaseOASIS: providerKey '{providerKey}' is not a valid GUID.");
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        // ─── Holon deletion ───────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var holon = await GetAsync<Holon>($"/oasis_holons/{id}");
                if (holon == null) { OASISErrorHandling.HandleError(ref result, $"FirebaseOASIS: No holon found with ID '{id}'."); return result; }
                await PatchAsync($"/oasis_holons/{id}", new { isDeleted = true });
                result.Result = holon; result.IsError = false; result.Message = $"FirebaseOASIS: Holon '{id}' soft-deleted.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id) => DeleteHolonAsync(id).Result;

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            if (Guid.TryParse(providerKey, out Guid id)) return await DeleteHolonAsync(id);
            var result = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref result, $"FirebaseOASIS: providerKey '{providerKey}' is not a valid GUID.");
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey) => DeleteHolonAsync(providerKey).Result;

        // ─── Search ───────────────────────────────────────────────────────────────

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            try
            {
                string? query = searchParams.SearchGroups?.OfType<SearchTextGroup>().FirstOrDefault()?.SearchQuery?.ToLower();
                var all = await GetAllAsync<Holon>("oasis_holons");
                var holons = all.Where(h => !h.IsDeleted && (string.IsNullOrEmpty(query) ||
                    (h.Name?.ToLower().Contains(query) == true) ||
                    (h.Description?.ToLower().Contains(query) == true))).Cast<IHolon>().ToList();
                result.Result = new SearchResults { SearchResultHolons = holons, NumberOfResults = holons.Count };
                result.IsError = false; result.Message = $"FirebaseOASIS: Found {holons.Count} holon(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
            => SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;

        // ─── Metadata queries ─────────────────────────────────────────────────────

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var all = await GetAllAsync<Holon>("oasis_holons");
                var holons = all.Where(h => !h.IsDeleted &&
                    h.MetaData != null &&
                    h.MetaData.TryGetValue(metaKey, out var val) && val?.ToString() == metaValue &&
                    (type == HolonType.All || h.HolonType == type)).Cast<IHolon>().ToList();
                result.Result = holons; result.IsError = false;
                result.Message = $"FirebaseOASIS: Loaded {holons.Count} holon(s) where {metaKey}={metaValue}.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var all = await GetAllAsync<Holon>("oasis_holons");
                bool IsMatch(Holon h)
                {
                    if (h.MetaData == null) return false;
                    var checks = metaKeyValuePairs.Select(kvp => h.MetaData.TryGetValue(kvp.Key, out var v) && v?.ToString() == kvp.Value);
                    return metaKeyValuePairMatchMode == MetaKeyValuePairMatchMode.Any ? checks.Any(c => c) : checks.All(c => c);
                }
                var holons = all.Where(h => !h.IsDeleted && IsMatch(h) && (type == HolonType.All || h.HolonType == type)).Cast<IHolon>().ToList();
                result.Result = holons; result.IsError = false;
                result.Message = $"FirebaseOASIS: Loaded {holons.Count} holon(s) matching metadata filter.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        // ─── Import / Export ──────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var saved = await SaveHolonsAsync(holons);
            return new OASISResult<bool> { Result = !saved.IsError, IsError = saved.IsError, Message = saved.Message };
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons) => ImportAsync(holons).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
            => await LoadAllHolonsAsync();

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0) => ExportAllAsync(version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var all = await GetAllAsync<Holon>("oasis_holons");
                var holons = all.Where(h => !h.IsDeleted && h.CreatedByAvatarId == avatarId).Cast<IHolon>().ToList();
                result.Result = holons; result.IsError = false; result.Message = $"FirebaseOASIS: Exported {holons.Count} holon(s) for avatar '{avatarId}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0) => ExportAllDataForAvatarByIdAsync(avatarId, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var avatarResult = await LoadAvatarByUsernameAsync(avatarUsername);
            if (avatarResult.IsError || avatarResult.Result == null)
            {
                var r = new OASISResult<IEnumerable<IHolon>>();
                OASISErrorHandling.HandleError(ref r, $"FirebaseOASIS: Avatar '{avatarUsername}' not found.");
                return r;
            }
            return await ExportAllDataForAvatarByIdAsync(avatarResult.Result.Id, version);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0) => ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            var avatarResult = await LoadAvatarByEmailAsync(avatarEmailAddress);
            if (avatarResult.IsError || avatarResult.Result == null)
            {
                var r = new OASISResult<IEnumerable<IHolon>>();
                OASISErrorHandling.HandleError(ref r, $"FirebaseOASIS: Avatar with email '{avatarEmailAddress}' not found.");
                return r;
            }
            return await ExportAllDataForAvatarByIdAsync(avatarResult.Result.Id, version);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0) => ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;

        public bool IsVersionControlEnabled { get; set; } = false;
    }
}
