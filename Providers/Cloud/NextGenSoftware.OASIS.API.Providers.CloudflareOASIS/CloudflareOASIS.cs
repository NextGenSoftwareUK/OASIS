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

namespace NextGenSoftware.OASIS.API.Providers.CloudflareOASIS
{
    /// <summary>
    /// OASIS provider for Cloudflare Workers KV (key-value store via Cloudflare REST API).
    ///
    /// No Cloudflare SDK required — uses System.Net.Http.
    ///
    /// Key naming conventions:
    ///   avatar:{id}         — Avatar JSON blob
    ///   avatar_detail:{id}  — AvatarDetail JSON blob
    ///   holon:{id}          — Holon JSON blob
    ///   index:avatar:username:{username} → avatar id
    ///   index:avatar:email:{email}       → avatar id
    ///   index:holon:parent:{parentId}:{holonId} → "1" (presence index for child holons)
    ///   index:holon:type:{holonType}:{holonId}  → "1"
    ///
    /// Constructor parameters:
    ///   accountId       — Cloudflare Account ID (from dash.cloudflare.com → right sidebar)
    ///   namespaceId     — KV Namespace ID (Workers &amp; Pages → KV → your namespace → ID)
    ///   apiToken        — Cloudflare API token with KV read/write permission
    /// </summary>
    public class CloudflareOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISDBStorageProvider
    {
        private readonly HttpClient _http;
        private readonly string _baseUrl;

        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public CloudflareOASIS(string accountId, string namespaceId, string apiToken)
        {
            _baseUrl = $"https://api.cloudflare.com/client/v4/accounts/{accountId}/storage/kv/namespaces/{namespaceId}";
            _http = new HttpClient();
            _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiToken}");
            ProviderName = "CloudflareOASIS";
            ProviderDescription = "Cloudflare Workers KV provider (Cloudflare REST API)";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.CloudflareOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageLocalAndNetwork);
        }

        // ─── Activation ───────────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                var response = await _http.GetAsync($"{_baseUrl}/keys?limit=1");
                if (!response.IsSuccessStatusCode)
                    throw new Exception($"HTTP {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
                result.Result = true; result.IsError = false;
                result.Message = "CloudflareOASIS activated — connected to Cloudflare KV.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CloudflareOASIS: Error activating provider — {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
            => await Task.FromResult(new OASISResult<bool> { Result = true, IsError = false, Message = "CloudflareOASIS deactivated." });

        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // ─── KV helpers ───────────────────────────────────────────────────────────

        private static string Serialize(object obj) => JsonSerializer.Serialize(obj, _jsonOpts);
        private static T? Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, _jsonOpts);

        private async Task KvPutAsync(string key, string value)
        {
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(value), "value");
            content.Add(new StringContent(""), "metadata");
            var response = await _http.PutAsync($"{_baseUrl}/values/{Uri.EscapeDataString(key)}", new StringContent(value, Encoding.UTF8, "application/json"));
            if (!response.IsSuccessStatusCode)
                throw new Exception($"KV PUT '{key}': HTTP {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        }

        private async Task<string?> KvGetAsync(string key)
        {
            var response = await _http.GetAsync($"{_baseUrl}/values/{Uri.EscapeDataString(key)}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
            if (!response.IsSuccessStatusCode) throw new Exception($"KV GET '{key}': HTTP {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            return await response.Content.ReadAsStringAsync();
        }

        private async Task KvDeleteAsync(string key)
        {
            var response = await _http.DeleteAsync($"{_baseUrl}/values/{Uri.EscapeDataString(key)}");
            if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NotFound)
                throw new Exception($"KV DELETE '{key}': HTTP {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        }

        // List all keys with optional prefix. Returns list of key names.
        private async Task<List<string>> KvListKeysAsync(string prefix = "")
        {
            var keys = new List<string>();
            string? cursor = null;
            while (true)
            {
                string url = $"{_baseUrl}/keys?limit=1000{(string.IsNullOrEmpty(prefix) ? "" : $"&prefix={Uri.EscapeDataString(prefix)}")}";
                if (cursor != null) url += $"&cursor={Uri.EscapeDataString(cursor)}";
                var response = await _http.GetAsync(url);
                if (!response.IsSuccessStatusCode) break;
                var json = await response.Content.ReadAsStringAsync();
                var envelope = Deserialize<KvListResponse>(json);
                if (envelope?.Result == null) break;
                keys.AddRange(envelope.Result.Select(r => r.Name));
                if (envelope.ResultInfo?.Cursor == null || !envelope.Result.Any()) break;
                cursor = envelope.ResultInfo.Cursor;
            }
            return keys;
        }

        // JSON envelope returned by Cloudflare KV list endpoint
        private class KvListResponse
        {
            [JsonPropertyName("result")] public List<KvKey>? Result { get; set; }
            [JsonPropertyName("result_info")] public KvResultInfo? ResultInfo { get; set; }
        }
        private class KvKey { [JsonPropertyName("name")] public string Name { get; set; } = ""; }
        private class KvResultInfo { [JsonPropertyName("cursor")] public string? Cursor { get; set; } }

        // ─── Avatar saving ────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (avatar.Id == Guid.Empty) avatar.Id = Guid.NewGuid();
                if (avatar.ProviderUniqueStorageKey == null)
                    avatar.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.CloudflareOASIS] = avatar.Id.ToString();

                await KvPutAsync($"avatar:{avatar.Id}", Serialize(avatar));
                if (!string.IsNullOrEmpty(avatar.Username))
                    await KvPutAsync($"index:avatar:username:{avatar.Username.ToLower()}", avatar.Id.ToString());
                if (!string.IsNullOrEmpty(avatar.Email))
                    await KvPutAsync($"index:avatar:email:{avatar.Email.ToLower()}", avatar.Id.ToString());

                result.Result = avatar; result.IsError = false; result.Message = $"CloudflareOASIS: Avatar '{avatar.Username}' saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CloudflareOASIS: Error saving avatar '{avatar.Username}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) => SaveAvatarAsync(avatar).Result;

        // ─── Avatar loading ───────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var json = await KvGetAsync($"avatar:{id}");
                if (json == null) { OASISErrorHandling.HandleError(ref result, $"CloudflareOASIS: No avatar found with ID '{id}'."); return result; }
                var avatar = Deserialize<Avatar>(json);
                if (avatar == null || avatar.IsDeleted) { OASISErrorHandling.HandleError(ref result, $"CloudflareOASIS: Avatar '{id}' not found or deleted."); return result; }
                result.Result = avatar; result.IsError = false; result.Message = $"CloudflareOASIS: Avatar loaded for ID '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CloudflareOASIS: Error loading avatar '{id}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) => LoadAvatarAsync(id, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var idStr = await KvGetAsync($"index:avatar:username:{username.ToLower()}");
                if (idStr == null || !Guid.TryParse(idStr, out var id)) { OASISErrorHandling.HandleError(ref result, $"CloudflareOASIS: No avatar found with username '{username}'."); return result; }
                return await LoadAvatarAsync(id, version);
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CloudflareOASIS: Error loading avatar by username '{username}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string username, int version = 0) => LoadAvatarByUsernameAsync(username, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var idStr = await KvGetAsync($"index:avatar:email:{avatarEmail.ToLower()}");
                if (idStr == null || !Guid.TryParse(idStr, out var id)) { OASISErrorHandling.HandleError(ref result, $"CloudflareOASIS: No avatar found with email '{avatarEmail}'."); return result; }
                return await LoadAvatarAsync(id, version);
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CloudflareOASIS: Error loading avatar by email '{avatarEmail}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0) => LoadAvatarByEmailAsync(avatarEmail, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            if (Guid.TryParse(providerKey, out Guid id)) return await LoadAvatarAsync(id, version);
            var result = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref result, $"CloudflareOASIS: providerKey '{providerKey}' is not a valid GUID.");
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0) => LoadAvatarByProviderKeyAsync(providerKey, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                var keys = await KvListKeysAsync("avatar:");
                var avatars = new List<IAvatar>();
                foreach (var key in keys.Where(k => !k.StartsWith("avatar_detail:")))
                {
                    var json = await KvGetAsync(key);
                    if (json == null) continue;
                    var avatar = Deserialize<Avatar>(json);
                    if (avatar != null && !avatar.IsDeleted) avatars.Add(avatar);
                }
                result.Result = avatars; result.IsError = false; result.Message = $"CloudflareOASIS: Loaded {avatars.Count} avatar(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CloudflareOASIS: Error loading all avatars: {ex.Message}"); }
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
                    if (loaded.IsError || loaded.Result == null) { OASISErrorHandling.HandleError(ref result, $"CloudflareOASIS: Avatar '{id}' not found."); return result; }
                    if (loaded.Result is Avatar av) { av.DeletedDate = DateTime.UtcNow; await KvPutAsync($"avatar:{id}", Serialize(av)); }
                }
                else
                {
                    await KvDeleteAsync($"avatar:{id}");
                }
                result.Result = true; result.IsError = false; result.Message = $"CloudflareOASIS: Avatar '{id}' {(softDelete ? "soft" : "hard")}-deleted.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CloudflareOASIS: Error deleting avatar '{id}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) => DeleteAvatarAsync(id, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string username, bool softDelete = true)
        {
            var loaded = await LoadAvatarByUsernameAsync(username);
            if (loaded.IsError || loaded.Result == null) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, $"CloudflareOASIS: Avatar '{username}' not found."); return r; }
            return await DeleteAvatarAsync(loaded.Result.Id, softDelete);
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string username, bool softDelete = true) => DeleteAvatarByUsernameAsync(username, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string email, bool softDelete = true)
        {
            var loaded = await LoadAvatarByEmailAsync(email);
            if (loaded.IsError || loaded.Result == null) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, $"CloudflareOASIS: Avatar with email '{email}' not found."); return r; }
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
                await KvPutAsync($"avatar_detail:{avatarDetail.Id}", Serialize(avatarDetail));
                result.Result = avatarDetail; result.IsError = false; result.Message = "CloudflareOASIS: AvatarDetail saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CloudflareOASIS: Error saving avatar detail: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail) => SaveAvatarDetailAsync(avatarDetail).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var json = await KvGetAsync($"avatar_detail:{id}");
                if (json == null) { OASISErrorHandling.HandleError(ref result, $"CloudflareOASIS: No avatar detail found for ID '{id}'."); return result; }
                result.Result = Deserialize<AvatarDetail>(json); result.IsError = false;
                result.Message = $"CloudflareOASIS: AvatarDetail loaded for ID '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CloudflareOASIS: Error loading avatar detail '{id}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => LoadAvatarDetailAsync(id, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string username, int version = 0)
        {
            var a = await LoadAvatarByUsernameAsync(username);
            if (a.IsError || a.Result == null) { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, $"CloudflareOASIS: Avatar '{username}' not found."); return r; }
            return await LoadAvatarDetailAsync(a.Result.Id, version);
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string username, int version = 0) => LoadAvatarDetailByUsernameAsync(username, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string email, int version = 0)
        {
            var a = await LoadAvatarByEmailAsync(email);
            if (a.IsError || a.Result == null) { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, $"CloudflareOASIS: Avatar with email '{email}' not found."); return r; }
            return await LoadAvatarDetailAsync(a.Result.Id, version);
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string email, int version = 0) => LoadAvatarDetailByEmailAsync(email, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                var keys = await KvListKeysAsync("avatar_detail:");
                var details = new List<IAvatarDetail>();
                foreach (var key in keys)
                {
                    var json = await KvGetAsync(key);
                    if (json == null) continue;
                    var d = Deserialize<AvatarDetail>(json);
                    if (d != null) details.Add(d);
                }
                result.Result = details; result.IsError = false; result.Message = $"CloudflareOASIS: Loaded {details.Count} avatar detail(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CloudflareOASIS: Error loading all avatar details: {ex.Message}"); }
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
                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.CloudflareOASIS] = holon.Id.ToString();

                await KvPutAsync($"holon:{holon.Id}", Serialize(holon));

                if (holon.ParentHolonId != Guid.Empty)
                    await KvPutAsync($"index:holon:parent:{holon.ParentHolonId}:{holon.Id}", "1");
                if (holon.HolonType != HolonType.All)
                    await KvPutAsync($"index:holon:type:{(int)holon.HolonType}:{holon.Id}", "1");

                result.Result = holon; result.IsError = false; result.Message = $"CloudflareOASIS: Holon '{holon.Name}' saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CloudflareOASIS: Error saving holon '{holon.Name}': {ex.Message}"); }
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
            result.Message = errors.Count > 0 ? string.Join("; ", errors) : $"CloudflareOASIS: {saved.Count} holon(s) saved.";
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
                var json = await KvGetAsync($"holon:{id}");
                if (json == null) { OASISErrorHandling.HandleError(ref result, $"CloudflareOASIS: No holon found with ID '{id}'."); return result; }
                var holon = Deserialize<Holon>(json);
                if (holon == null || holon.IsDeleted) { OASISErrorHandling.HandleError(ref result, $"CloudflareOASIS: Holon '{id}' not found or deleted."); return result; }
                result.Result = holon; result.IsError = false; result.Message = $"CloudflareOASIS: Holon loaded for ID '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CloudflareOASIS: Error loading holon '{id}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            if (Guid.TryParse(providerKey, out Guid id)) return await LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);
            var result = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref result, $"CloudflareOASIS: providerKey '{providerKey}' is not a valid GUID.");
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                IEnumerable<string> keys;
                if (type == HolonType.All)
                {
                    var allKeys = await KvListKeysAsync("holon:");
                    keys = allKeys.Where(k => !k.StartsWith("index:"));
                }
                else
                {
                    var indexKeys = await KvListKeysAsync($"index:holon:type:{(int)type}:");
                    keys = indexKeys.Select(ik => $"holon:{ik.Split(':').Last()}");
                }

                var holons = new List<IHolon>();
                foreach (var key in keys)
                {
                    var json = await KvGetAsync(key);
                    if (json == null) continue;
                    var h = Deserialize<Holon>(json);
                    if (h != null && !h.IsDeleted) holons.Add(h);
                }
                result.Result = holons; result.IsError = false; result.Message = $"CloudflareOASIS: Loaded {holons.Count} holon(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CloudflareOASIS: Error loading all holons: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var indexKeys = await KvListKeysAsync($"index:holon:parent:{id}:");
                var holons = new List<IHolon>();
                foreach (var ik in indexKeys)
                {
                    var holonId = ik.Split(':').Last();
                    var json = await KvGetAsync($"holon:{holonId}");
                    if (json == null) continue;
                    var h = Deserialize<Holon>(json);
                    if (h != null && !h.IsDeleted && (type == HolonType.All || h.HolonType == type)) holons.Add(h);
                }
                result.Result = holons; result.IsError = false; result.Message = $"CloudflareOASIS: Loaded {holons.Count} holon(s) for parent '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CloudflareOASIS: Error loading holons for parent '{id}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            if (Guid.TryParse(providerKey, out Guid id)) return await LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, $"CloudflareOASIS: providerKey '{providerKey}' is not a valid GUID.");
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
                var loaded = await LoadHolonAsync(id);
                if (loaded.IsError || loaded.Result == null) { OASISErrorHandling.HandleError(ref result, $"CloudflareOASIS: No holon found with ID '{id}'."); return result; }
                if (loaded.Result is Holon h2) { h2.DeletedDate = DateTime.UtcNow; await KvPutAsync($"holon:{id}", Serialize(h2)); }
                result.Result = loaded.Result; result.IsError = false; result.Message = $"CloudflareOASIS: Holon '{id}' soft-deleted.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id) => DeleteHolonAsync(id).Result;

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            if (Guid.TryParse(providerKey, out Guid id)) return await DeleteHolonAsync(id);
            var result = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref result, $"CloudflareOASIS: providerKey '{providerKey}' is not a valid GUID.");
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
                var all = await LoadAllHolonsAsync();
                var holons = all.Result?.ToList() ?? new List<IHolon>();
                if (!string.IsNullOrEmpty(query))
                    holons = holons.Where(h => h.Name?.ToLower().Contains(query) == true || h.Description?.ToLower().Contains(query) == true).ToList();
                result.Result = new SearchResults { SearchResultHolons = holons, NumberOfResults = holons.Count };
                result.IsError = false; result.Message = $"CloudflareOASIS: Found {holons.Count} holon(s).";
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
                var all = await LoadAllHolonsAsync(type);
                var holons = all.Result?.Where(h => h.MetaData != null && h.MetaData.TryGetValue(metaKey, out var v) && v?.ToString() == metaValue).ToList() ?? new List<IHolon>();
                result.Result = holons; result.IsError = false;
                result.Message = $"CloudflareOASIS: Loaded {holons.Count} holon(s) where {metaKey}={metaValue}.";
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
                var all = await LoadAllHolonsAsync(type);
                bool IsMatch(IHolon h)
                {
                    if (h.MetaData == null) return false;
                    var checks = metaKeyValuePairs.Select(kvp => h.MetaData.TryGetValue(kvp.Key, out var v) && v?.ToString() == kvp.Value);
                    return metaKeyValuePairMatchMode == MetaKeyValuePairMatchMode.Any ? checks.Any(c => c) : checks.All(c => c);
                }
                var holons = all.Result?.Where(IsMatch).ToList() ?? new List<IHolon>();
                result.Result = holons; result.IsError = false;
                result.Message = $"CloudflareOASIS: Loaded {holons.Count} holon(s) matching metadata filter.";
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

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0) => await LoadAllHolonsAsync();
        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0) => ExportAllAsync(version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var all = await LoadAllHolonsAsync();
                var holons = all.Result?.Where(h => h.CreatedByAvatarId == avatarId).ToList() ?? new List<IHolon>();
                result.Result = holons; result.IsError = false; result.Message = $"CloudflareOASIS: Exported {holons.Count} holon(s) for avatar '{avatarId}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0) => ExportAllDataForAvatarByIdAsync(avatarId, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var a = await LoadAvatarByUsernameAsync(avatarUsername);
            if (a.IsError || a.Result == null) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, $"CloudflareOASIS: Avatar '{avatarUsername}' not found."); return r; }
            return await ExportAllDataForAvatarByIdAsync(a.Result.Id, version);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0) => ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            var a = await LoadAvatarByEmailAsync(avatarEmailAddress);
            if (a.IsError || a.Result == null) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, $"CloudflareOASIS: Avatar with email '{avatarEmailAddress}' not found."); return r; }
            return await ExportAllDataForAvatarByIdAsync(a.Result.Id, version);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0) => ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;

        public bool IsVersionControlEnabled { get; set; } = false;
    }
}
