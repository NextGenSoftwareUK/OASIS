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

namespace NextGenSoftware.OASIS.API.Providers.UpstashOASIS
{
    /// <summary>
    /// OASIS provider for Upstash — serverless Redis-compatible key-value store, accessed via Upstash REST API.
    ///
    /// Key patterns:
    ///   avatar:{guid}              → JSON blob of Avatar
    ///   avatar:username:{username} → guid
    ///   avatar:email:{email}       → guid
    ///   avatars:all                → Redis Set of guids
    ///
    ///   detail:{guid}              → JSON blob of AvatarDetail
    ///   detail:username:{username} → guid
    ///   detail:email:{email}       → guid
    ///   details:all                → Redis Set of guids
    ///
    ///   holon:{guid}               → JSON blob of Holon
    ///   holons:all                 → Redis Set of guids
    ///   holons:parent:{parentGuid} → Redis Set of child guids
    ///
    /// Constructor parameters:
    ///   restUrl — Upstash Redis REST URL, e.g. "https://abc.upstash.io"
    ///   token   — Upstash REST token
    /// </summary>
    public class UpstashOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISDBStorageProvider
    {
        private readonly string _restUrl;
        private readonly HttpClient _http;

        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public UpstashOASIS(string restUrl, string token)
        {
            _restUrl = restUrl.TrimEnd('/');
            _http = new HttpClient();
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            ProviderName = "UpstashOASIS";
            ProviderDescription = "Upstash serverless Redis provider (REST API — edge-compatible key-value store for OASIS holons and avatars)";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.UpstashOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageCloud);
        }

        private static string Ser(object o) => JsonSerializer.Serialize(o, _jsonOpts);
        private static T? Des<T>(string? json) => json == null ? default : JsonSerializer.Deserialize<T>(json, _jsonOpts);

        // ─── Upstash REST helpers ─────────────────────────────────────────────────

        private async Task<JsonElement> ExecAsync(params object[] command)
        {
            var content = new StringContent(Ser(command), Encoding.UTF8, "application/json");
            var resp = await _http.PostAsync($"{_restUrl}/", content);
            var raw = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) throw new Exception($"Upstash → {(int)resp.StatusCode}: {raw}");
            using var doc = JsonDocument.Parse(raw);
            return doc.RootElement.Clone();
        }

        private async Task<string?> GetAsync(string key)
        {
            var result = await ExecAsync("GET", key);
            if (result.TryGetProperty("result", out var r) && r.ValueKind == JsonValueKind.String) return r.GetString();
            return null;
        }

        private async Task SetAsync(string key, string value) => await ExecAsync("SET", key, value);
        private async Task DelAsync(params string[] keys) => await ExecAsync(new object[] { "DEL" }.Concat(keys.Cast<object>()).ToArray());
        private async Task SAddAsync(string key, string member) => await ExecAsync("SADD", key, member);
        private async Task SRemAsync(string key, string member) => await ExecAsync("SREM", key, member);
        private async Task<List<string>> SMembersAsync(string key)
        {
            var result = await ExecAsync("SMEMBERS", key);
            var members = new List<string>();
            if (result.TryGetProperty("result", out var arr) && arr.ValueKind == JsonValueKind.Array)
                foreach (var m in arr.EnumerateArray()) if (m.ValueKind == JsonValueKind.String) members.Add(m.GetString()!);
            return members;
        }

        // ─── Activation ───────────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                // Ping to verify connectivity
                await ExecAsync("PING");
                result.Result = true; result.IsError = false; result.Message = "UpstashOASIS activated — Redis connection verified.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"UpstashOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;
        public override async Task<OASISResult<bool>> DeActivateProviderAsync() => await Task.FromResult(new OASISResult<bool> { Result = true, IsError = false, Message = "UpstashOASIS deactivated." });
        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // ─── Avatar saving ────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (avatar.Id == Guid.Empty) avatar.Id = Guid.NewGuid();
                if (avatar.ProviderUniqueStorageKey == null) avatar.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.UpstashOASIS] = avatar.Id.ToString();
                var id = avatar.Id.ToString();
                await SetAsync($"avatar:{id}", Ser(avatar));
                if (!string.IsNullOrEmpty(avatar.Username)) await SetAsync($"avatar:username:{avatar.Username}", id);
                if (!string.IsNullOrEmpty(avatar.Email)) await SetAsync($"avatar:email:{avatar.Email}", id);
                await SAddAsync("avatars:all", id);
                result.Result = avatar; result.IsError = false; result.Message = $"UpstashOASIS: Avatar '{avatar.Username}' saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"UpstashOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) => SaveAvatarAsync(avatar).Result;

        // ─── Avatar loading ───────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var json = await GetAsync($"avatar:{id}"); if (json == null) { OASISErrorHandling.HandleError(ref result, $"UpstashOASIS: No avatar for ID '{id}'."); return result; }
                var avatar = Des<Avatar>(json); if (avatar == null) { OASISErrorHandling.HandleError(ref result, "UpstashOASIS: Deserialise failed."); return result; }
                if (avatar.IsDeleted) { OASISErrorHandling.HandleError(ref result, $"UpstashOASIS: Avatar '{id}' is deleted."); return result; }
                result.Result = avatar; result.IsError = false; result.Message = "UpstashOASIS: Avatar loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"UpstashOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) => LoadAvatarAsync(id, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var id = await GetAsync($"avatar:username:{username}"); if (id == null || !Guid.TryParse(id, out var guid)) { OASISErrorHandling.HandleError(ref result, $"UpstashOASIS: No avatar for username '{username}'."); return result; }
                return await LoadAvatarAsync(guid, version);
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"UpstashOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string username, int version = 0) => LoadAvatarByUsernameAsync(username, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string email, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var id = await GetAsync($"avatar:email:{email}"); if (id == null || !Guid.TryParse(id, out var guid)) { OASISErrorHandling.HandleError(ref result, $"UpstashOASIS: No avatar for email '{email}'."); return result; }
                return await LoadAvatarAsync(guid, version);
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"UpstashOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string email, int version = 0) => LoadAvatarByEmailAsync(email, version).Result;
        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string pk, int version = 0) { if (Guid.TryParse(pk, out Guid id)) return await LoadAvatarAsync(id, version); var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, $"UpstashOASIS: Invalid GUID '{pk}'."); return r; }
        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string pk, int version = 0) => LoadAvatarByProviderKeyAsync(pk, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                var ids = await SMembersAsync("avatars:all");
                var avatars = new List<IAvatar>();
                foreach (var id in ids)
                {
                    var json = await GetAsync($"avatar:{id}"); if (json == null) continue;
                    var a = Des<Avatar>(json); if (a == null || a.IsDeleted) continue; avatars.Add(a);
                }
                result.Result = avatars; result.IsError = false; result.Message = $"UpstashOASIS: Loaded {avatars.Count} avatar(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"UpstashOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) => LoadAllAvatarsAsync(version).Result;

        // ─── Avatar deletion ──────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                var loaded = await LoadAvatarAsync(id); if (loaded.IsError || loaded.Result == null) { OASISErrorHandling.HandleError(ref result, $"UpstashOASIS: Avatar '{id}' not found."); return result; }
                var av = (Avatar)loaded.Result; av.DeletedDate = DateTime.UtcNow;
                if (softDelete) { await SetAsync($"avatar:{id}", Ser(av)); }
                else
                {
                    if (!string.IsNullOrEmpty(av.Username)) await DelAsync($"avatar:username:{av.Username}");
                    if (!string.IsNullOrEmpty(av.Email)) await DelAsync($"avatar:email:{av.Email}");
                    await DelAsync($"avatar:{id}");
                    await SRemAsync("avatars:all", id.ToString());
                }
                result.Result = true; result.IsError = false; result.Message = $"UpstashOASIS: Avatar '{id}' deleted.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"UpstashOASIS: {ex.Message}"); }
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
                var id = d.Id.ToString();
                await SetAsync($"detail:{id}", Ser(d));
                if (!string.IsNullOrEmpty(d.Username)) await SetAsync($"detail:username:{d.Username}", id);
                if (!string.IsNullOrEmpty(d.Email)) await SetAsync($"detail:email:{d.Email}", id);
                await SAddAsync("details:all", id);
                result.Result = d; result.IsError = false; result.Message = "UpstashOASIS: AvatarDetail saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"UpstashOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail d) => SaveAvatarDetailAsync(d).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0) { var result = new OASISResult<IAvatarDetail>(); try { var json = await GetAsync($"detail:{id}"); if (json == null) { OASISErrorHandling.HandleError(ref result, $"UpstashOASIS: No detail for ID '{id}'."); return result; } var d = Des<AvatarDetail>(json); if (d == null) { OASISErrorHandling.HandleError(ref result, "UpstashOASIS: Deserialise failed."); return result; } result.Result = d; result.IsError = false; result.Message = "UpstashOASIS: AvatarDetail loaded."; } catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"UpstashOASIS: {ex.Message}"); } return result; }
        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => LoadAvatarDetailAsync(id, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string u, int version = 0) { var result = new OASISResult<IAvatarDetail>(); try { var id = await GetAsync($"detail:username:{u}"); if (id == null || !Guid.TryParse(id, out var guid)) { OASISErrorHandling.HandleError(ref result, $"UpstashOASIS: No detail for username '{u}'."); return result; } return await LoadAvatarDetailAsync(guid, version); } catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"UpstashOASIS: {ex.Message}"); } return result; }
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string u, int version = 0) => LoadAvatarDetailByUsernameAsync(u, version).Result;
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string e, int version = 0) { var result = new OASISResult<IAvatarDetail>(); try { var id = await GetAsync($"detail:email:{e}"); if (id == null || !Guid.TryParse(id, out var guid)) { OASISErrorHandling.HandleError(ref result, $"UpstashOASIS: No detail for email '{e}'."); return result; } return await LoadAvatarDetailAsync(guid, version); } catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"UpstashOASIS: {ex.Message}"); } return result; }
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string e, int version = 0) => LoadAvatarDetailByEmailAsync(e, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            try { var ids = await SMembersAsync("details:all"); var details = new List<IAvatarDetail>(); foreach (var id in ids) { var json = await GetAsync($"detail:{id}"); if (json == null) continue; var d = Des<AvatarDetail>(json); if (d != null) details.Add(d); } result.Result = details; result.IsError = false; result.Message = $"UpstashOASIS: Loaded {details.Count} detail(s)."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"UpstashOASIS: {ex.Message}"); }
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
                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.UpstashOASIS] = holon.Id.ToString();
                var id = holon.Id.ToString();
                await SetAsync($"holon:{id}", Ser(holon));
                await SAddAsync("holons:all", id);
                if (holon.ParentHolonId != Guid.Empty) await SAddAsync($"holons:parent:{holon.ParentHolonId}", id);
                result.Result = holon; result.IsError = false; result.Message = $"UpstashOASIS: Holon '{holon.Name}' saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"UpstashOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var saved = new List<IHolon>(); var errors = new List<string>();
            foreach (var h in holons) { var r = await SaveHolonAsync(h, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider); if (r.IsError) errors.Add(r.Message ?? ""); else if (r.Result != null) saved.Add(r.Result); }
            result.Result = saved; result.IsError = errors.Count > 0; result.Message = errors.Count > 0 ? string.Join("; ", errors) : $"UpstashOASIS: {saved.Count} holon(s) saved.";
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;

        // ─── Holon loading ────────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var json = await GetAsync($"holon:{id}"); if (json == null) { OASISErrorHandling.HandleError(ref result, $"UpstashOASIS: No holon for ID '{id}'."); return result; }
                var holon = Des<Holon>(json); if (holon == null) { OASISErrorHandling.HandleError(ref result, "UpstashOASIS: Deserialise failed."); return result; }
                if (holon.IsDeleted) { OASISErrorHandling.HandleError(ref result, $"UpstashOASIS: Holon '{id}' is deleted."); return result; }
                result.Result = holon; result.IsError = false; result.Message = "UpstashOASIS: Holon loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"UpstashOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string pk, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) { if (Guid.TryParse(pk, out Guid id)) return await LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version); var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, $"UpstashOASIS: Invalid GUID '{pk}'."); return r; }
        public override OASISResult<IHolon> LoadHolon(string pk, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonAsync(pk, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var ids = await SMembersAsync("holons:all");
                var holons = new List<IHolon>();
                foreach (var id in ids)
                {
                    var json = await GetAsync($"holon:{id}"); if (json == null) continue;
                    var h = Des<Holon>(json); if (h == null || h.IsDeleted) continue;
                    if (type != HolonType.All && h.HolonType != type) continue;
                    holons.Add(h);
                }
                result.Result = holons; result.IsError = false; result.Message = $"UpstashOASIS: Loaded {holons.Count} holon(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"UpstashOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var ids = await SMembersAsync($"holons:parent:{id}");
                var holons = new List<IHolon>();
                foreach (var childId in ids)
                {
                    var json = await GetAsync($"holon:{childId}"); if (json == null) continue;
                    var h = Des<Holon>(json); if (h == null || h.IsDeleted) continue;
                    if (type != HolonType.All && h.HolonType != type) continue;
                    holons.Add(h);
                }
                result.Result = holons; result.IsError = false; result.Message = $"UpstashOASIS: Loaded {holons.Count} holon(s) for parent '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"UpstashOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string pk, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) { if (Guid.TryParse(pk, out Guid id)) return await LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version); var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, $"UpstashOASIS: Invalid GUID '{pk}'."); return r; }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string pk, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsForParentAsync(pk, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        // ─── Holon deletion ───────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var loaded = await LoadHolonAsync(id); if (loaded.IsError || loaded.Result == null) { OASISErrorHandling.HandleError(ref result, $"UpstashOASIS: Holon '{id}' not found."); return result; }
                var holon = (Holon)loaded.Result; holon.DeletedDate = DateTime.UtcNow;
                await SetAsync($"holon:{id}", Ser(holon));
                result.Result = holon; result.IsError = false; result.Message = $"UpstashOASIS: Holon '{id}' soft-deleted.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"UpstashOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id) => DeleteHolonAsync(id).Result;
        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string pk) { if (Guid.TryParse(pk, out Guid id)) return await DeleteHolonAsync(id); var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, $"UpstashOASIS: Invalid GUID '{pk}'."); return r; }
        public override OASISResult<IHolon> DeleteHolon(string pk) => DeleteHolonAsync(pk).Result;

        // ─── Search ───────────────────────────────────────────────────────────────

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            try { string? q = searchParams.SearchGroups?.OfType<SearchTextGroup>().FirstOrDefault()?.SearchQuery?.ToLower(); var all = await LoadAllHolonsAsync(); var holons = all.Result?.ToList() ?? new List<IHolon>(); if (!string.IsNullOrEmpty(q)) holons = holons.Where(h => h.Name?.ToLower().Contains(q) == true || h.Description?.ToLower().Contains(q) == true).ToList(); result.Result = new SearchResults { SearchResultHolons = holons, NumberOfResults = holons.Count }; result.IsError = false; result.Message = $"UpstashOASIS: Found {holons.Count} result(s)."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        { var all = await LoadAllHolonsAsync(type); var holons = all.Result?.Where(h => h.MetaData != null && h.MetaData.TryGetValue(metaKey, out var v) && v?.ToString() == metaValue).ToList() ?? new List<IHolon>(); return new OASISResult<IEnumerable<IHolon>> { Result = holons, IsError = false, Message = $"UpstashOASIS: {holons.Count} holon(s)." }; }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKvp, MetaKeyValuePairMatchMode mode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        { var all = await LoadAllHolonsAsync(type); bool IsMatch(IHolon h) { if (h.MetaData == null) return false; var checks = metaKvp.Select(kvp => h.MetaData.TryGetValue(kvp.Key, out var v) && v?.ToString() == kvp.Value); return mode == MetaKeyValuePairMatchMode.Any ? checks.Any(c => c) : checks.All(c => c); } var holons = all.Result?.Where(IsMatch).ToList() ?? new List<IHolon>(); return new OASISResult<IEnumerable<IHolon>> { Result = holons, IsError = false, Message = $"UpstashOASIS: {holons.Count} holon(s)." }; }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKvp, MetaKeyValuePairMatchMode mode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsByMetaDataAsync(metaKvp, mode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        // ─── Import / Export ──────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons) { var s = await SaveHolonsAsync(holons); return new OASISResult<bool> { Result = !s.IsError, IsError = s.IsError, Message = s.Message }; }
        public override OASISResult<bool> Import(IEnumerable<IHolon> holons) => ImportAsync(holons).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0) => await LoadAllHolonsAsync();
        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0) => ExportAllAsync(version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0) { var all = await LoadAllHolonsAsync(); var h = all.Result?.Where(x => x.CreatedByAvatarId == avatarId).ToList() ?? new List<IHolon>(); return new OASISResult<IEnumerable<IHolon>> { Result = h, IsError = false, Message = $"UpstashOASIS: {h.Count} holon(s)." }; }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0) => ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string u, int version = 0) { var a = await LoadAvatarByUsernameAsync(u); if (a.IsError || a.Result == null) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "Not found."); return r; } return await ExportAllDataForAvatarByIdAsync(a.Result.Id, version); }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string u, int version = 0) => ExportAllDataForAvatarByUsernameAsync(u, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string e, int version = 0) { var a = await LoadAvatarByEmailAsync(e); if (a.IsError || a.Result == null) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "Not found."); return r; } return await ExportAllDataForAvatarByIdAsync(a.Result.Id, version); }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string e, int version = 0) => ExportAllDataForAvatarByEmailAsync(e, version).Result;

        public bool IsVersionControlEnabled { get; set; } = false;
    }
}
