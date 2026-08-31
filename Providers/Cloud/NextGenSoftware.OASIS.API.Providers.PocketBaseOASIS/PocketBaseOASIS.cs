using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
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

namespace NextGenSoftware.OASIS.API.Providers.PocketBaseOASIS
{
    /// <summary>
    /// OASIS provider for PocketBase via its REST API.
    ///
    /// Requires PocketBase running (self-hosted single binary or PocketBase Cloud).
    /// Create three collections in the PocketBase Admin UI before use:
    ///   oasis_avatars      — fields: oasis_id (text, unique), username (text), email (text), is_deleted (bool), data_json (text)
    ///   oasis_avatar_details — fields: oasis_id (text, unique), username (text), email (text), data_json (text)
    ///   oasis_holons       — fields: oasis_id (text, unique), parent_holon_id (text), holon_type (number), is_deleted (bool), data_json (text)
    ///
    /// Constructor parameters:
    ///   baseUrl   — e.g. "https://myapp.pockethost.io" or "http://localhost:8090"
    ///   adminEmail, adminPassword — superuser credentials for the Admin API
    /// </summary>
    public class PocketBaseOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISDBStorageProvider
    {
        private readonly HttpClient _http;
        private readonly string _baseUrl;
        private readonly string _adminEmail;
        private readonly string _adminPassword;
        private string? _adminToken;

        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public PocketBaseOASIS(string baseUrl, string adminEmail, string adminPassword)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _adminEmail = adminEmail;
            _adminPassword = adminPassword;
            _http = new HttpClient();
            ProviderName = "PocketBaseOASIS";
            ProviderDescription = "PocketBase provider (self-hosted open-source backend REST API)";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.PocketBaseOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageLocalAndNetwork);
        }

        // ─── Auth ─────────────────────────────────────────────────────────────────

        private async Task EnsureAuthAsync()
        {
            if (_adminToken != null) return;
            var payload = new { identity = _adminEmail, password = _adminPassword };
            var response = await _http.PostAsync($"{_baseUrl}/api/admins/auth-with-password",
                new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));
            if (!response.IsSuccessStatusCode) throw new Exception($"PocketBase admin auth failed: {await response.Content.ReadAsStringAsync()}");
            var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            _adminToken = doc.RootElement.GetProperty("token").GetString();
            _http.DefaultRequestHeaders.Remove("Authorization");
            _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {_adminToken}");
        }

        // ─── Activation ───────────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                await EnsureAuthAsync();
                result.Result = true; result.IsError = false;
                result.Message = "PocketBaseOASIS activated — authenticated with PocketBase admin API.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PocketBaseOASIS: Error activating provider — {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;
        public override async Task<OASISResult<bool>> DeActivateProviderAsync() => await Task.FromResult(new OASISResult<bool> { Result = true, IsError = false, Message = "PocketBaseOASIS deactivated." });
        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // ─── Helpers ─────────────────────────────────────────────────────────────

        private static string Serialize(object obj) => JsonSerializer.Serialize(obj, _jsonOpts);
        private static T? Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, _jsonOpts);

        private StringContent JsonContent(object obj) => new StringContent(Serialize(obj), Encoding.UTF8, "application/json");

        // PocketBase list response envelope
        private class PbList<T> { [JsonPropertyName("items")] public List<T>? Items { get; set; } }
        private class PbRecord { [JsonPropertyName("id")] public string Id { get; set; } = ""; [JsonPropertyName("data_json")] public string? DataJson { get; set; } [JsonPropertyName("is_deleted")] public bool IsDeleted { get; set; } }

        private async Task<PbRecord?> GetFirstAsync(string collection, string filter)
        {
            await EnsureAuthAsync();
            var url = $"{_baseUrl}/api/collections/{collection}/records?filter={Uri.EscapeDataString(filter)}&perPage=1";
            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;
            var list = Deserialize<PbList<PbRecord>>(await response.Content.ReadAsStringAsync());
            return list?.Items?.FirstOrDefault();
        }

        private async Task<List<PbRecord>> GetAllAsync(string collection, string? filter = null)
        {
            await EnsureAuthAsync();
            var url = $"{_baseUrl}/api/collections/{collection}/records?perPage=500{(filter != null ? $"&filter={Uri.EscapeDataString(filter)}" : "")}";
            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode) return new List<PbRecord>();
            var list = Deserialize<PbList<PbRecord>>(await response.Content.ReadAsStringAsync());
            return list?.Items ?? new List<PbRecord>();
        }

        private async Task UpsertAsync(string collection, string oasisId, object row)
        {
            await EnsureAuthAsync();
            // Check if record exists by oasis_id field
            var existing = await GetFirstAsync(collection, $"oasis_id='{oasisId}'");
            HttpResponseMessage response;
            if (existing != null)
                response = await _http.PatchAsync($"{_baseUrl}/api/collections/{collection}/records/{existing.Id}", JsonContent(row));
            else
                response = await _http.PostAsync($"{_baseUrl}/api/collections/{collection}/records", JsonContent(row));
            if (!response.IsSuccessStatusCode) throw new Exception($"PocketBase upsert failed: {await response.Content.ReadAsStringAsync()}");
        }

        // ─── Avatar saving ────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (avatar.Id == Guid.Empty) avatar.Id = Guid.NewGuid();
                if (avatar.ProviderUniqueStorageKey == null) avatar.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.PocketBaseOASIS] = avatar.Id.ToString();
                var row = new { oasis_id = avatar.Id.ToString(), username = avatar.Username ?? "", email = avatar.Email ?? "", is_deleted = avatar.IsDeleted, data_json = Serialize(avatar) };
                await UpsertAsync("oasis_avatars", avatar.Id.ToString(), row);
                result.Result = avatar; result.IsError = false; result.Message = $"PocketBaseOASIS: Avatar '{avatar.Username}' saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PocketBaseOASIS: Error saving avatar: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) => SaveAvatarAsync(avatar).Result;

        // ─── Avatar loading ───────────────────────────────────────────────────────

        private Avatar? ParseAvatar(PbRecord? rec) => rec == null || rec.IsDeleted ? null : (rec.DataJson != null ? Deserialize<Avatar>(rec.DataJson) : null);

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var rec = await GetFirstAsync("oasis_avatars", $"oasis_id='{id}'&&is_deleted=false");
                var avatar = ParseAvatar(rec);
                if (avatar == null) { OASISErrorHandling.HandleError(ref result, $"PocketBaseOASIS: No avatar found for ID '{id}'."); return result; }
                result.Result = avatar; result.IsError = false; result.Message = $"PocketBaseOASIS: Avatar loaded for ID '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PocketBaseOASIS: Error loading avatar '{id}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) => LoadAvatarAsync(id, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var rec = await GetFirstAsync("oasis_avatars", $"username='{username}'&&is_deleted=false");
                var avatar = ParseAvatar(rec);
                if (avatar == null) { OASISErrorHandling.HandleError(ref result, $"PocketBaseOASIS: No avatar found for username '{username}'."); return result; }
                result.Result = avatar; result.IsError = false; result.Message = $"PocketBaseOASIS: Avatar loaded for username '{username}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PocketBaseOASIS: Error loading avatar by username '{username}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string username, int version = 0) => LoadAvatarByUsernameAsync(username, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var rec = await GetFirstAsync("oasis_avatars", $"email='{avatarEmail}'&&is_deleted=false");
                var avatar = ParseAvatar(rec);
                if (avatar == null) { OASISErrorHandling.HandleError(ref result, $"PocketBaseOASIS: No avatar found for email '{avatarEmail}'."); return result; }
                result.Result = avatar; result.IsError = false; result.Message = $"PocketBaseOASIS: Avatar loaded for email '{avatarEmail}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PocketBaseOASIS: Error loading avatar by email '{avatarEmail}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0) => LoadAvatarByEmailAsync(avatarEmail, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            if (Guid.TryParse(providerKey, out Guid id)) return await LoadAvatarAsync(id, version);
            var result = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref result, $"PocketBaseOASIS: providerKey '{providerKey}' is not a valid GUID."); return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0) => LoadAvatarByProviderKeyAsync(providerKey, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                var recs = await GetAllAsync("oasis_avatars", "is_deleted=false");
                var avatars = recs.Select(r => r.DataJson != null ? Deserialize<Avatar>(r.DataJson) : null).Where(a => a != null).Cast<IAvatar>().ToList();
                result.Result = avatars; result.IsError = false; result.Message = $"PocketBaseOASIS: Loaded {avatars.Count} avatar(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PocketBaseOASIS: Error loading all avatars: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) => LoadAllAvatarsAsync(version).Result;

        // ─── Avatar deletion ──────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                await EnsureAuthAsync();
                var rec = await GetFirstAsync("oasis_avatars", $"oasis_id='{id}'");
                if (rec == null) { OASISErrorHandling.HandleError(ref result, $"PocketBaseOASIS: Avatar '{id}' not found."); return result; }
                HttpResponseMessage response;
                if (softDelete)
                    response = await _http.PatchAsync($"{_baseUrl}/api/collections/oasis_avatars/records/{rec.Id}", JsonContent(new { is_deleted = true }));
                else
                    response = await _http.DeleteAsync($"{_baseUrl}/api/collections/oasis_avatars/records/{rec.Id}");
                result.Result = response.IsSuccessStatusCode; result.IsError = !result.Result;
                result.Message = result.Result ? $"PocketBaseOASIS: Avatar '{id}' {(softDelete ? "soft" : "hard")}-deleted." : "PocketBaseOASIS: Delete failed.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PocketBaseOASIS: Error deleting avatar '{id}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) => DeleteAvatarAsync(id, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string username, bool softDelete = true)
        {
            var a = await LoadAvatarByUsernameAsync(username);
            if (a.IsError || a.Result == null) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, $"PocketBaseOASIS: Avatar '{username}' not found."); return r; }
            return await DeleteAvatarAsync(a.Result.Id, softDelete);
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string username, bool softDelete = true) => DeleteAvatarByUsernameAsync(username, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string email, bool softDelete = true)
        {
            var a = await LoadAvatarByEmailAsync(email);
            if (a.IsError || a.Result == null) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, $"PocketBaseOASIS: Avatar with email '{email}' not found."); return r; }
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
                var row = new { oasis_id = avatarDetail.Id.ToString(), username = avatarDetail.Username ?? "", email = avatarDetail.Email ?? "", data_json = Serialize(avatarDetail) };
                await UpsertAsync("oasis_avatar_details", avatarDetail.Id.ToString(), row);
                result.Result = avatarDetail; result.IsError = false; result.Message = "PocketBaseOASIS: AvatarDetail saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PocketBaseOASIS: Error saving avatar detail: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail) => SaveAvatarDetailAsync(avatarDetail).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var rec = await GetFirstAsync("oasis_avatar_details", $"oasis_id='{id}'");
                if (rec?.DataJson == null) { OASISErrorHandling.HandleError(ref result, $"PocketBaseOASIS: No avatar detail found for ID '{id}'."); return result; }
                result.Result = Deserialize<AvatarDetail>(rec.DataJson); result.IsError = false; result.Message = $"PocketBaseOASIS: AvatarDetail loaded for ID '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PocketBaseOASIS: Error loading avatar detail '{id}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => LoadAvatarDetailAsync(id, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var rec = await GetFirstAsync("oasis_avatar_details", $"username='{username}'");
                if (rec?.DataJson == null) { OASISErrorHandling.HandleError(ref result, $"PocketBaseOASIS: No avatar detail found for username '{username}'."); return result; }
                result.Result = Deserialize<AvatarDetail>(rec.DataJson); result.IsError = false; result.Message = $"PocketBaseOASIS: AvatarDetail loaded for username '{username}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PocketBaseOASIS: Error loading avatar detail by username '{username}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string username, int version = 0) => LoadAvatarDetailByUsernameAsync(username, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string email, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var rec = await GetFirstAsync("oasis_avatar_details", $"email='{email}'");
                if (rec?.DataJson == null) { OASISErrorHandling.HandleError(ref result, $"PocketBaseOASIS: No avatar detail found for email '{email}'."); return result; }
                result.Result = Deserialize<AvatarDetail>(rec.DataJson); result.IsError = false; result.Message = $"PocketBaseOASIS: AvatarDetail loaded for email '{email}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PocketBaseOASIS: Error loading avatar detail by email '{email}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string email, int version = 0) => LoadAvatarDetailByEmailAsync(email, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                var recs = await GetAllAsync("oasis_avatar_details");
                var details = recs.Where(r => r.DataJson != null).Select(r => Deserialize<AvatarDetail>(r.DataJson!)).Where(d => d != null).Cast<IAvatarDetail>().ToList();
                result.Result = details; result.IsError = false; result.Message = $"PocketBaseOASIS: Loaded {details.Count} avatar detail(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PocketBaseOASIS: Error loading all avatar details: {ex.Message}"); }
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
                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.PocketBaseOASIS] = holon.Id.ToString();
                var row = new { oasis_id = holon.Id.ToString(), parent_holon_id = holon.ParentHolonId == Guid.Empty ? "" : holon.ParentHolonId.ToString(), holon_type = (int)holon.HolonType, is_deleted = holon.IsDeleted, data_json = Serialize(holon) };
                await UpsertAsync("oasis_holons", holon.Id.ToString(), row);
                result.Result = holon; result.IsError = false; result.Message = $"PocketBaseOASIS: Holon '{holon.Name}' saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PocketBaseOASIS: Error saving holon '{holon.Name}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var saved = new List<IHolon>(); var errors = new List<string>();
            foreach (var holon in holons) { var r = await SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider); if (r.IsError) errors.Add(r.Message ?? ""); else if (r.Result != null) saved.Add(r.Result); }
            result.Result = saved; result.IsError = errors.Count > 0; result.Message = errors.Count > 0 ? string.Join("; ", errors) : $"PocketBaseOASIS: {saved.Count} holon(s) saved.";
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;

        // ─── Holon loading ────────────────────────────────────────────────────────

        private Holon? ParseHolon(PbRecord? rec) => rec == null || rec.IsDeleted ? null : (rec.DataJson != null ? Deserialize<Holon>(rec.DataJson) : null);

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var rec = await GetFirstAsync("oasis_holons", $"oasis_id='{id}'&&is_deleted=false");
                var holon = ParseHolon(rec);
                if (holon == null) { OASISErrorHandling.HandleError(ref result, $"PocketBaseOASIS: No holon found for ID '{id}'."); return result; }
                result.Result = holon; result.IsError = false; result.Message = $"PocketBaseOASIS: Holon loaded for ID '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PocketBaseOASIS: Error loading holon '{id}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            if (Guid.TryParse(providerKey, out Guid id)) return await LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);
            var result = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref result, $"PocketBaseOASIS: providerKey '{providerKey}' is not a valid GUID."); return result;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                string? filter = type == HolonType.All ? "is_deleted=false" : $"is_deleted=false&&holon_type={((int)type)}";
                var recs = await GetAllAsync("oasis_holons", filter);
                var holons = recs.Where(r => r.DataJson != null).Select(r => Deserialize<Holon>(r.DataJson!)).Where(h => h != null).Cast<IHolon>().ToList();
                result.Result = holons; result.IsError = false; result.Message = $"PocketBaseOASIS: Loaded {holons.Count} holon(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PocketBaseOASIS: Error loading all holons: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                string filter = type == HolonType.All ? $"parent_holon_id='{id}'&&is_deleted=false" : $"parent_holon_id='{id}'&&is_deleted=false&&holon_type={(int)type}";
                var recs = await GetAllAsync("oasis_holons", filter);
                var holons = recs.Where(r => r.DataJson != null).Select(r => Deserialize<Holon>(r.DataJson!)).Where(h => h != null).Cast<IHolon>().ToList();
                result.Result = holons; result.IsError = false; result.Message = $"PocketBaseOASIS: Loaded {holons.Count} holon(s) for parent '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PocketBaseOASIS: Error loading holons for parent '{id}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            if (Guid.TryParse(providerKey, out Guid id)) return await LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
            var result = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref result, $"PocketBaseOASIS: providerKey '{providerKey}' is not a valid GUID."); return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        // ─── Holon deletion ───────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                await EnsureAuthAsync();
                var rec = await GetFirstAsync("oasis_holons", $"oasis_id='{id}'");
                if (rec == null) { OASISErrorHandling.HandleError(ref result, $"PocketBaseOASIS: No holon found with ID '{id}'."); return result; }
                var loaded = ParseHolon(rec);
                await _http.PatchAsync($"{_baseUrl}/api/collections/oasis_holons/records/{rec.Id}", JsonContent(new { is_deleted = true }));
                result.Result = loaded; result.IsError = false; result.Message = $"PocketBaseOASIS: Holon '{id}' soft-deleted.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id) => DeleteHolonAsync(id).Result;

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            if (Guid.TryParse(providerKey, out Guid id)) return await DeleteHolonAsync(id);
            var result = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref result, $"PocketBaseOASIS: providerKey '{providerKey}' is not a valid GUID."); return result;
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
                if (!string.IsNullOrEmpty(query)) holons = holons.Where(h => h.Name?.ToLower().Contains(query) == true || h.Description?.ToLower().Contains(query) == true).ToList();
                result.Result = new SearchResults { SearchResultHolons = holons, NumberOfResults = holons.Count };
                result.IsError = false; result.Message = $"PocketBaseOASIS: Found {holons.Count} holon(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
            => SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;

        // ─── Metadata ─────────────────────────────────────────────────────────────

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var all = await LoadAllHolonsAsync(type);
            var holons = all.Result?.Where(h => h.MetaData != null && h.MetaData.TryGetValue(metaKey, out var v) && v?.ToString() == metaValue).ToList() ?? new List<IHolon>();
            return new OASISResult<IEnumerable<IHolon>> { Result = holons, IsError = false, Message = $"PocketBaseOASIS: Loaded {holons.Count} holon(s) where {metaKey}={metaValue}." };
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var all = await LoadAllHolonsAsync(type);
            bool IsMatch(IHolon h) { if (h.MetaData == null) return false; var checks = metaKeyValuePairs.Select(kvp => h.MetaData.TryGetValue(kvp.Key, out var v) && v?.ToString() == kvp.Value); return metaKeyValuePairMatchMode == MetaKeyValuePairMatchMode.Any ? checks.Any(c => c) : checks.All(c => c); }
            var holons = all.Result?.Where(IsMatch).ToList() ?? new List<IHolon>();
            return new OASISResult<IEnumerable<IHolon>> { Result = holons, IsError = false, Message = $"PocketBaseOASIS: Loaded {holons.Count} holon(s) matching metadata filter." };
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
            return new OASISResult<IEnumerable<IHolon>> { Result = holons, IsError = false, Message = $"PocketBaseOASIS: Exported {holons.Count} holon(s) for avatar '{avatarId}'." };
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0) => ExportAllDataForAvatarByIdAsync(avatarId, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0) { var a = await LoadAvatarByUsernameAsync(avatarUsername); if (a.IsError || a.Result == null) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, $"PocketBaseOASIS: Avatar '{avatarUsername}' not found."); return r; } return await ExportAllDataForAvatarByIdAsync(a.Result.Id, version); }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0) => ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0) { var a = await LoadAvatarByEmailAsync(avatarEmailAddress); if (a.IsError || a.Result == null) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, $"PocketBaseOASIS: Avatar with email '{avatarEmailAddress}' not found."); return r; } return await ExportAllDataForAvatarByIdAsync(a.Result.Id, version); }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0) => ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;

        public bool IsVersionControlEnabled { get; set; } = false;
    }
}
