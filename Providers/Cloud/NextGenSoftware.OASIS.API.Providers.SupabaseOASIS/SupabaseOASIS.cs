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

namespace NextGenSoftware.OASIS.API.Providers.SupabaseOASIS
{
    /// <summary>
    /// OASIS provider for Supabase (PostgreSQL via PostgREST REST API).
    ///
    /// Uses Supabase's auto-generated REST API — no Supabase SDK required.
    /// Create the three required tables in Supabase SQL editor before use:
    ///
    ///   CREATE TABLE oasis_avatars (
    ///       id UUID PRIMARY KEY, username TEXT, email TEXT,
    ///       is_deleted BOOLEAN DEFAULT FALSE, data_json TEXT
    ///   );
    ///   CREATE TABLE oasis_avatar_details (
    ///       id UUID PRIMARY KEY, username TEXT, email TEXT, data_json TEXT
    ///   );
    ///   CREATE TABLE oasis_holons (
    ///       id UUID PRIMARY KEY, parent_holon_id UUID, holon_type INT DEFAULT 0,
    ///       is_deleted BOOLEAN DEFAULT FALSE, data_json TEXT
    ///   );
    ///
    /// Constructor parameters:
    ///   supabaseUrl  — e.g. "https://xyzxyz.supabase.co"
    ///   apiKey       — anon key or service_role key from Supabase → Settings → API
    /// </summary>
    public class SupabaseOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISDBStorageProvider
    {
        private readonly HttpClient _http;
        private readonly string _baseUrl;

        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public SupabaseOASIS(string supabaseUrl, string apiKey)
        {
            _baseUrl = supabaseUrl.TrimEnd('/') + "/rest/v1";
            _http = new HttpClient();
            _http.DefaultRequestHeaders.Add("apikey", apiKey);
            _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            _http.DefaultRequestHeaders.Add("Prefer", "return=representation");
            ProviderName = "SupabaseOASIS";
            ProviderDescription = "Supabase provider (PostgREST REST API over PostgreSQL)";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.SupabaseOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageLocalAndNetwork);
        }

        // ─── Activation ───────────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                var response = await _http.GetAsync($"{_baseUrl}/oasis_avatars?limit=1");
                if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NotFound)
                    throw new Exception($"HTTP {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
                result.Result = true; result.IsError = false;
                result.Message = "SupabaseOASIS activated — connected to Supabase PostgREST.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"SupabaseOASIS: Error activating provider — {ex.Message}");
            }
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
            => await Task.FromResult(new OASISResult<bool> { Result = true, IsError = false, Message = "SupabaseOASIS deactivated." });

        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // ─── Helpers ─────────────────────────────────────────────────────────────

        private static string Serialize(object obj) => JsonSerializer.Serialize(obj, _jsonOpts);
        private static T? Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, _jsonOpts);
        private static List<T> DeserializeList<T>(string json) => JsonSerializer.Deserialize<List<T>>(json, _jsonOpts) ?? new List<T>();

        private StringContent JsonContent(object obj) => new StringContent(Serialize(obj), Encoding.UTF8, "application/json");

        // ─── Row model ────────────────────────────────────────────────────────────

        private record AvatarRow(string id, string username, string email, bool is_deleted, string data_json);
        private record AvatarDetailRow(string id, string username, string email, string data_json);
        private record HolonRow(string id, string? parent_holon_id, int holon_type, bool is_deleted, string data_json);

        // ─── Avatar saving ────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (avatar.Id == Guid.Empty) avatar.Id = Guid.NewGuid();
                if (avatar.ProviderUniqueStorageKey == null)
                    avatar.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.SupabaseOASIS] = avatar.Id.ToString();

                var row = new { id = avatar.Id, username = avatar.Username ?? "", email = avatar.Email ?? "", is_deleted = avatar.IsDeleted, data_json = Serialize(avatar) };
                var content = JsonContent(row);

                // Upsert: POST with Prefer: resolution=merge-duplicates
                var req = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/oasis_avatars") { Content = content };
                req.Headers.Add("Prefer", "resolution=merge-duplicates");
                var response = await _http.SendAsync(req);
                if (!response.IsSuccessStatusCode)
                    throw new Exception($"HTTP {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");

                result.Result = avatar; result.IsError = false; result.Message = $"SupabaseOASIS: Avatar '{avatar.Username}' saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"SupabaseOASIS: Error saving avatar '{avatar.Username}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) => SaveAvatarAsync(avatar).Result;

        // ─── Avatar loading ───────────────────────────────────────────────────────

        private async Task<Avatar?> GetAvatarByFilterAsync(string filter)
        {
            var response = await _http.GetAsync($"{_baseUrl}/oasis_avatars?{filter}&is_deleted=eq.false&limit=1");
            if (!response.IsSuccessStatusCode) return null;
            var json = await response.Content.ReadAsStringAsync();
            var rows = DeserializeList<AvatarRow>(json);
            if (rows.Count == 0) return null;
            return Deserialize<Avatar>(rows[0].data_json);
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var avatar = await GetAvatarByFilterAsync($"id=eq.{id}");
                if (avatar == null) { OASISErrorHandling.HandleError(ref result, $"SupabaseOASIS: No avatar found with ID '{id}'."); return result; }
                result.Result = avatar; result.IsError = false; result.Message = $"SupabaseOASIS: Avatar loaded for ID '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"SupabaseOASIS: Error loading avatar by ID '{id}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) => LoadAvatarAsync(id, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var avatar = await GetAvatarByFilterAsync($"username=eq.{Uri.EscapeDataString(username)}");
                if (avatar == null) { OASISErrorHandling.HandleError(ref result, $"SupabaseOASIS: No avatar found with username '{username}'."); return result; }
                result.Result = avatar; result.IsError = false; result.Message = $"SupabaseOASIS: Avatar loaded for username '{username}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"SupabaseOASIS: Error loading avatar by username '{username}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string username, int version = 0) => LoadAvatarByUsernameAsync(username, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var avatar = await GetAvatarByFilterAsync($"email=eq.{Uri.EscapeDataString(avatarEmail)}");
                if (avatar == null) { OASISErrorHandling.HandleError(ref result, $"SupabaseOASIS: No avatar found with email '{avatarEmail}'."); return result; }
                result.Result = avatar; result.IsError = false; result.Message = $"SupabaseOASIS: Avatar loaded for email '{avatarEmail}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"SupabaseOASIS: Error loading avatar by email '{avatarEmail}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0) => LoadAvatarByEmailAsync(avatarEmail, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            if (Guid.TryParse(providerKey, out Guid id)) return await LoadAvatarAsync(id, version);
            var result = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref result, $"SupabaseOASIS: providerKey '{providerKey}' is not a valid GUID.");
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0) => LoadAvatarByProviderKeyAsync(providerKey, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                var response = await _http.GetAsync($"{_baseUrl}/oasis_avatars?is_deleted=eq.false");
                var json = await response.Content.ReadAsStringAsync();
                var rows = DeserializeList<AvatarRow>(json);
                var avatars = rows.Select(r => Deserialize<Avatar>(r.data_json)).Where(a => a != null).Cast<IAvatar>().ToList();
                result.Result = avatars; result.IsError = false; result.Message = $"SupabaseOASIS: Loaded {avatars.Count} avatar(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"SupabaseOASIS: Error loading all avatars: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) => LoadAllAvatarsAsync(version).Result;

        // ─── Avatar deletion ──────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                HttpResponseMessage response;
                if (softDelete)
                    response = await _http.PatchAsync($"{_baseUrl}/oasis_avatars?id=eq.{id}", JsonContent(new { is_deleted = true }));
                else
                    response = await _http.DeleteAsync($"{_baseUrl}/oasis_avatars?id=eq.{id}");
                result.Result = response.IsSuccessStatusCode; result.IsError = !result.Result;
                result.Message = result.Result ? $"SupabaseOASIS: Avatar '{id}' {(softDelete ? "soft" : "hard")}-deleted." : $"SupabaseOASIS: Failed to delete avatar '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"SupabaseOASIS: Error deleting avatar '{id}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) => DeleteAvatarAsync(id, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string username, bool softDelete = true)
        {
            var loaded = await LoadAvatarByUsernameAsync(username);
            if (loaded.IsError || loaded.Result == null) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, $"SupabaseOASIS: Avatar '{username}' not found."); return r; }
            return await DeleteAvatarAsync(loaded.Result.Id, softDelete);
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string username, bool softDelete = true) => DeleteAvatarByUsernameAsync(username, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string email, bool softDelete = true)
        {
            var loaded = await LoadAvatarByEmailAsync(email);
            if (loaded.IsError || loaded.Result == null) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, $"SupabaseOASIS: Avatar with email '{email}' not found."); return r; }
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
                var row = new { id = avatarDetail.Id, username = avatarDetail.Username ?? "", email = avatarDetail.Email ?? "", data_json = Serialize(avatarDetail) };
                var req = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/oasis_avatar_details") { Content = JsonContent(row) };
                req.Headers.Add("Prefer", "resolution=merge-duplicates");
                var response = await _http.SendAsync(req);
                if (!response.IsSuccessStatusCode) throw new Exception($"HTTP {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
                result.Result = avatarDetail; result.IsError = false; result.Message = "SupabaseOASIS: AvatarDetail saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"SupabaseOASIS: Error saving avatar detail: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail) => SaveAvatarDetailAsync(avatarDetail).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var response = await _http.GetAsync($"{_baseUrl}/oasis_avatar_details?id=eq.{id}&limit=1");
                var json = await response.Content.ReadAsStringAsync();
                var rows = DeserializeList<AvatarDetailRow>(json);
                if (rows.Count == 0) { OASISErrorHandling.HandleError(ref result, $"SupabaseOASIS: No avatar detail found for ID '{id}'."); return result; }
                result.Result = Deserialize<AvatarDetail>(rows[0].data_json); result.IsError = false;
                result.Message = $"SupabaseOASIS: AvatarDetail loaded for ID '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"SupabaseOASIS: Error loading avatar detail for '{id}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => LoadAvatarDetailAsync(id, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var response = await _http.GetAsync($"{_baseUrl}/oasis_avatar_details?username=eq.{Uri.EscapeDataString(username)}&limit=1");
                var rows = DeserializeList<AvatarDetailRow>(await response.Content.ReadAsStringAsync());
                if (rows.Count == 0) { OASISErrorHandling.HandleError(ref result, $"SupabaseOASIS: No avatar detail found for username '{username}'."); return result; }
                result.Result = Deserialize<AvatarDetail>(rows[0].data_json); result.IsError = false;
                result.Message = $"SupabaseOASIS: AvatarDetail loaded for username '{username}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"SupabaseOASIS: Error loading avatar detail by username '{username}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string username, int version = 0) => LoadAvatarDetailByUsernameAsync(username, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string email, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var response = await _http.GetAsync($"{_baseUrl}/oasis_avatar_details?email=eq.{Uri.EscapeDataString(email)}&limit=1");
                var rows = DeserializeList<AvatarDetailRow>(await response.Content.ReadAsStringAsync());
                if (rows.Count == 0) { OASISErrorHandling.HandleError(ref result, $"SupabaseOASIS: No avatar detail found for email '{email}'."); return result; }
                result.Result = Deserialize<AvatarDetail>(rows[0].data_json); result.IsError = false;
                result.Message = $"SupabaseOASIS: AvatarDetail loaded for email '{email}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"SupabaseOASIS: Error loading avatar detail by email '{email}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string email, int version = 0) => LoadAvatarDetailByEmailAsync(email, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                var response = await _http.GetAsync($"{_baseUrl}/oasis_avatar_details");
                var rows = DeserializeList<AvatarDetailRow>(await response.Content.ReadAsStringAsync());
                var details = rows.Select(r => Deserialize<AvatarDetail>(r.data_json)).Where(d => d != null).Cast<IAvatarDetail>().ToList();
                result.Result = details; result.IsError = false; result.Message = $"SupabaseOASIS: Loaded {details.Count} avatar detail(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"SupabaseOASIS: Error loading all avatar details: {ex.Message}"); }
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
                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.SupabaseOASIS] = holon.Id.ToString();

                var row = new
                {
                    id = holon.Id,
                    parent_holon_id = holon.ParentHolonId == Guid.Empty ? (Guid?)null : holon.ParentHolonId,
                    holon_type = (int)holon.HolonType,
                    is_deleted = holon.IsDeleted,
                    data_json = Serialize(holon)
                };
                var req = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/oasis_holons") { Content = JsonContent(row) };
                req.Headers.Add("Prefer", "resolution=merge-duplicates");
                var response = await _http.SendAsync(req);
                if (!response.IsSuccessStatusCode) throw new Exception($"HTTP {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
                result.Result = holon; result.IsError = false; result.Message = $"SupabaseOASIS: Holon '{holon.Name}' saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"SupabaseOASIS: Error saving holon '{holon.Name}': {ex.Message}"); }
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
            result.Message = errors.Count > 0 ? string.Join("; ", errors) : $"SupabaseOASIS: {saved.Count} holon(s) saved.";
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;

        // ─── Holon loading ────────────────────────────────────────────────────────

        private async Task<List<Holon>> GetHolonsByFilterAsync(string filter)
        {
            var response = await _http.GetAsync($"{_baseUrl}/oasis_holons?{filter}");
            var json = await response.Content.ReadAsStringAsync();
            var rows = DeserializeList<HolonRow>(json);
            return rows.Select(r => Deserialize<Holon>(r.data_json)).Where(h => h != null).Select(h => h!).ToList();
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var holons = await GetHolonsByFilterAsync($"id=eq.{id}&is_deleted=eq.false&limit=1");
                if (holons.Count == 0) { OASISErrorHandling.HandleError(ref result, $"SupabaseOASIS: No holon found with ID '{id}'."); return result; }
                result.Result = holons[0]; result.IsError = false; result.Message = $"SupabaseOASIS: Holon loaded for ID '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"SupabaseOASIS: Error loading holon '{id}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            if (Guid.TryParse(providerKey, out Guid id)) return await LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);
            var result = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref result, $"SupabaseOASIS: providerKey '{providerKey}' is not a valid GUID.");
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                string filter = type == HolonType.All ? "is_deleted=eq.false" : $"is_deleted=eq.false&holon_type=eq.{(int)type}";
                var holons = (await GetHolonsByFilterAsync(filter)).Cast<IHolon>().ToList();
                result.Result = holons; result.IsError = false; result.Message = $"SupabaseOASIS: Loaded {holons.Count} holon(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"SupabaseOASIS: Error loading all holons: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                string filter = type == HolonType.All
                    ? $"parent_holon_id=eq.{id}&is_deleted=eq.false"
                    : $"parent_holon_id=eq.{id}&is_deleted=eq.false&holon_type=eq.{(int)type}";
                var holons = (await GetHolonsByFilterAsync(filter)).Cast<IHolon>().ToList();
                result.Result = holons; result.IsError = false; result.Message = $"SupabaseOASIS: Loaded {holons.Count} holon(s) for parent '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"SupabaseOASIS: Error loading holons for parent '{id}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            if (Guid.TryParse(providerKey, out Guid id)) return await LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, $"SupabaseOASIS: providerKey '{providerKey}' is not a valid GUID.");
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
                if (loaded.IsError || loaded.Result == null) { OASISErrorHandling.HandleError(ref result, $"SupabaseOASIS: No holon found with ID '{id}'."); return result; }
                await _http.PatchAsync($"{_baseUrl}/oasis_holons?id=eq.{id}", JsonContent(new { is_deleted = true }));
                result.Result = loaded.Result; result.IsError = false; result.Message = $"SupabaseOASIS: Holon '{id}' soft-deleted.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id) => DeleteHolonAsync(id).Result;

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            if (Guid.TryParse(providerKey, out Guid id)) return await DeleteHolonAsync(id);
            var result = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref result, $"SupabaseOASIS: providerKey '{providerKey}' is not a valid GUID.");
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
                var all = await GetHolonsByFilterAsync("is_deleted=eq.false");
                var holons = string.IsNullOrEmpty(query) ? all.Cast<IHolon>().ToList()
                    : all.Where(h => h.Name?.ToLower().Contains(query) == true || h.Description?.ToLower().Contains(query) == true).Cast<IHolon>().ToList();
                result.Result = new SearchResults { SearchResultHolons = holons, NumberOfResults = holons.Count };
                result.IsError = false; result.Message = $"SupabaseOASIS: Found {holons.Count} holon(s).";
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
                var all = await GetHolonsByFilterAsync("is_deleted=eq.false");
                var holons = all.Where(h => h.MetaData != null && h.MetaData.TryGetValue(metaKey, out var v) && v?.ToString() == metaValue && (type == HolonType.All || h.HolonType == type)).Cast<IHolon>().ToList();
                result.Result = holons; result.IsError = false;
                result.Message = $"SupabaseOASIS: Loaded {holons.Count} holon(s) where {metaKey}={metaValue}.";
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
                var all = await GetHolonsByFilterAsync("is_deleted=eq.false");
                bool IsMatch(Holon h)
                {
                    if (h.MetaData == null) return false;
                    var checks = metaKeyValuePairs.Select(kvp => h.MetaData.TryGetValue(kvp.Key, out var v) && v?.ToString() == kvp.Value);
                    return metaKeyValuePairMatchMode == MetaKeyValuePairMatchMode.Any ? checks.Any(c => c) : checks.All(c => c);
                }
                var holons = all.Where(h => IsMatch(h) && (type == HolonType.All || h.HolonType == type)).Cast<IHolon>().ToList();
                result.Result = holons; result.IsError = false;
                result.Message = $"SupabaseOASIS: Loaded {holons.Count} holon(s) matching metadata filter.";
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
                var all = await GetHolonsByFilterAsync("is_deleted=eq.false");
                var holons = all.Where(h => h.CreatedByAvatarId == avatarId).Cast<IHolon>().ToList();
                result.Result = holons; result.IsError = false; result.Message = $"SupabaseOASIS: Exported {holons.Count} holon(s) for avatar '{avatarId}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0) => ExportAllDataForAvatarByIdAsync(avatarId, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var a = await LoadAvatarByUsernameAsync(avatarUsername);
            if (a.IsError || a.Result == null) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, $"SupabaseOASIS: Avatar '{avatarUsername}' not found."); return r; }
            return await ExportAllDataForAvatarByIdAsync(a.Result.Id, version);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0) => ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            var a = await LoadAvatarByEmailAsync(avatarEmailAddress);
            if (a.IsError || a.Result == null) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, $"SupabaseOASIS: Avatar with email '{avatarEmailAddress}' not found."); return r; }
            return await ExportAllDataForAvatarByIdAsync(a.Result.Id, version);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0) => ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;

        public bool IsVersionControlEnabled { get; set; } = false;
    }
}
