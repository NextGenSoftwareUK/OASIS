using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Web;
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

namespace NextGenSoftware.OASIS.API.Providers.CloudinaryOASIS
{
    /// <summary>
    /// OASIS provider for Cloudinary — a cloud media/asset management platform.
    ///
    /// Avatar and holon JSON documents are uploaded as raw files to Cloudinary using the
    /// "raw" resource type.  Public IDs follow the pattern:
    ///   oasis/avatars/{guid}
    ///   oasis/avatar-details/{guid}
    ///   oasis/holons/{guid}
    ///
    /// Username/email lookup index objects are stored as:
    ///   oasis/indexes/avatar-by-username/{username}
    ///   oasis/indexes/avatar-by-email/{email}
    ///
    /// Authentication uses HTTP Basic auth (api_key:api_secret) for the Admin API, and
    /// signed uploads for the Upload API.
    ///
    /// Constructor parameters:
    ///   cloudName — Cloudinary cloud name
    ///   apiKey    — Cloudinary API key
    ///   apiSecret — Cloudinary API secret
    /// </summary>
    public class CloudinaryOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISDBStorageProvider
    {
        private readonly string _cloudName;
        private readonly string _apiKey;
        private readonly string _apiSecret;
        private readonly HttpClient _http;

        private string UploadUrl => $"https://api.cloudinary.com/v1_1/{_cloudName}/raw/upload";
        private string AdminUrl(string path) => $"https://api.cloudinary.com/v1_1/{_cloudName}/{path}";

        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public CloudinaryOASIS(string cloudName, string apiKey, string apiSecret)
        {
            _cloudName = cloudName;
            _apiKey = apiKey;
            _apiSecret = apiSecret;
            _http = new HttpClient();
            var creds = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{apiKey}:{apiSecret}"));
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", creds);
            ProviderName = "CloudinaryOASIS";
            ProviderDescription = "Cloudinary provider (cloud asset management via Cloudinary REST API)";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.CloudinaryOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageLocalAndNetwork);
        }

        private static string Ser(object obj) => JsonSerializer.Serialize(obj, _jsonOpts);
        private static T? Des<T>(string json) => JsonSerializer.Deserialize<T>(json, _jsonOpts);

        private string Sign(Dictionary<string, string> parameters)
        {
            var sorted = new SortedDictionary<string, string>(parameters);
            var sb = new StringBuilder();
            foreach (var kvp in sorted) { if (sb.Length > 0) sb.Append('&'); sb.Append($"{kvp.Key}={kvp.Value}"); }
            sb.Append(_apiSecret);
            using var sha = SHA1.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        private async Task UploadRawAsync(string publicId, string json)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            var signParams = new Dictionary<string, string>
            {
                { "public_id", publicId },
                { "resource_type", "raw" },
                { "timestamp", timestamp }
            };
            var signature = Sign(signParams);

            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(_apiKey), "api_key");
            form.Add(new StringContent(timestamp), "timestamp");
            form.Add(new StringContent(signature), "signature");
            form.Add(new StringContent(publicId), "public_id");
            form.Add(new StringContent("raw"), "resource_type");
            form.Add(new StringContent(json, Encoding.UTF8, "application/json"), "file", "data.json");
            await _http.PostAsync(UploadUrl, form);
        }

        private async Task<string?> DownloadRawAsync(string publicId)
        {
            try
            {
                // Cloudinary delivers raw assets via the delivery URL
                var url = $"https://res.cloudinary.com/{_cloudName}/raw/upload/{publicId}";
                var resp = await _http.GetAsync(url);
                if (!resp.IsSuccessStatusCode) return null;
                return await resp.Content.ReadAsStringAsync();
            }
            catch { return null; }
        }

        private async Task DeleteRawAsync(string publicId)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            var signParams = new Dictionary<string, string> { { "public_id", publicId }, { "resource_type", "raw" }, { "timestamp", timestamp } };
            var signature = Sign(signParams);
            var url = AdminUrl($"resources/raw/upload?public_ids[]={HttpUtility.UrlEncode(publicId)}");
            await _http.DeleteAsync(url);
        }

        private async Task<List<string>> ListPublicIdsAsync(string folder)
        {
            var list = new List<string>();
            string? nextCursor = null;
            do
            {
                var url = AdminUrl($"resources/raw/upload?prefix={HttpUtility.UrlEncode(folder)}&max_results=500{(nextCursor != null ? $"&next_cursor={nextCursor}" : "")}");
                var resp = await _http.GetAsync(url);
                if (!resp.IsSuccessStatusCode) break;
                var json = await resp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("resources", out var resources))
                    foreach (var r in resources.EnumerateArray())
                        if (r.TryGetProperty("public_id", out var pid)) list.Add(pid.GetString()!);
                nextCursor = doc.RootElement.TryGetProperty("next_cursor", out var nc) ? nc.GetString() : null;
            } while (nextCursor != null);
            return list;
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                // Verify credentials by pinging the usage endpoint
                var resp = await _http.GetAsync(AdminUrl("usage"));
                if (!resp.IsSuccessStatusCode) throw new Exception($"Cloudinary credentials invalid (HTTP {(int)resp.StatusCode}).");
                IsProviderActivated = true; result.Result = true; result.IsError = false; result.Message = "CloudinaryOASIS activated.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CloudinaryOASIS: Error activating — {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;
        public override async Task<OASISResult<bool>> DeActivateProviderAsync() { IsProviderActivated = false; return await Task.FromResult(new OASISResult<bool> { Result = true, IsError = false, Message = "CloudinaryOASIS deactivated." }); }
        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // ─── Avatar ───────────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (avatar.Id == Guid.Empty) avatar.Id = Guid.NewGuid();
                if (avatar.ProviderUniqueStorageKey == null) avatar.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.CloudinaryOASIS] = avatar.Id.ToString();
                await UploadRawAsync($"oasis/avatars/{avatar.Id}", Ser(avatar));
                if (!string.IsNullOrEmpty(avatar.Username)) await UploadRawAsync($"oasis/indexes/avatar-by-username/{avatar.Username}", $"{{\"id\":\"{avatar.Id}\"}}");
                if (!string.IsNullOrEmpty(avatar.Email)) await UploadRawAsync($"oasis/indexes/avatar-by-email/{avatar.Email}", $"{{\"id\":\"{avatar.Id}\"}}");
                result.Result = avatar; result.IsError = false; result.Message = $"CloudinaryOASIS: Avatar '{avatar.Username}' saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CloudinaryOASIS: Error saving avatar: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) => SaveAvatarAsync(avatar).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var json = await DownloadRawAsync($"oasis/avatars/{id}");
                if (json == null) { OASISErrorHandling.HandleError(ref result, $"CloudinaryOASIS: No avatar for ID '{id}'."); return result; }
                var a = Des<Avatar>(json);
                if (a == null || a.IsDeleted) { OASISErrorHandling.HandleError(ref result, $"CloudinaryOASIS: No avatar for ID '{id}'."); return result; }
                result.Result = a; result.IsError = false; result.Message = "CloudinaryOASIS: Avatar loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CloudinaryOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) => LoadAvatarAsync(id, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var json = await DownloadRawAsync($"oasis/indexes/avatar-by-username/{username}");
                if (json == null) { OASISErrorHandling.HandleError(ref result, $"CloudinaryOASIS: No avatar for username '{username}'."); return result; }
                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("id", out var idEl) || !Guid.TryParse(idEl.GetString(), out Guid id)) { OASISErrorHandling.HandleError(ref result, $"CloudinaryOASIS: No avatar for username '{username}'."); return result; }
                return await LoadAvatarAsync(id, version);
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CloudinaryOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string username, int version = 0) => LoadAvatarByUsernameAsync(username, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var json = await DownloadRawAsync($"oasis/indexes/avatar-by-email/{avatarEmail}");
                if (json == null) { OASISErrorHandling.HandleError(ref result, $"CloudinaryOASIS: No avatar for email '{avatarEmail}'."); return result; }
                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("id", out var idEl) || !Guid.TryParse(idEl.GetString(), out Guid id)) { OASISErrorHandling.HandleError(ref result, $"CloudinaryOASIS: No avatar for email '{avatarEmail}'."); return result; }
                return await LoadAvatarAsync(id, version);
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CloudinaryOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0) => LoadAvatarByEmailAsync(avatarEmail, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            if (Guid.TryParse(providerKey, out Guid id)) return await LoadAvatarAsync(id, version);
            var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, $"CloudinaryOASIS: Invalid GUID '{providerKey}'."); return r;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0) => LoadAvatarByProviderKeyAsync(providerKey, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                var pids = await ListPublicIdsAsync("oasis/avatars/");
                var list = new List<IAvatar>();
                foreach (var pid in pids) { var json = await DownloadRawAsync(pid); if (json == null) continue; var a = Des<Avatar>(json); if (a != null && !a.IsDeleted) list.Add(a); }
                result.Result = list; result.IsError = false; result.Message = $"CloudinaryOASIS: Loaded {list.Count} avatar(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CloudinaryOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) => LoadAllAvatarsAsync(version).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (softDelete) { var l = await LoadAvatarAsync(id); if (!l.IsError && l.Result != null) { var a = (Avatar)l.Result; a.DeletedDate = DateTime.UtcNow; await SaveAvatarAsync(a); } }
                else { await DeleteRawAsync($"oasis/avatars/{id}"); }
                result.Result = true; result.IsError = false; result.Message = $"CloudinaryOASIS: Avatar '{id}' deleted.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CloudinaryOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) => DeleteAvatarAsync(id, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string u, bool softDelete = true) { var a = await LoadAvatarByUsernameAsync(u); if (a.IsError || a.Result == null) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, $"CloudinaryOASIS: Avatar '{u}' not found."); return r; } return await DeleteAvatarAsync(a.Result.Id, softDelete); }
        public override OASISResult<bool> DeleteAvatarByUsername(string u, bool softDelete = true) => DeleteAvatarByUsernameAsync(u, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string e, bool softDelete = true) { var a = await LoadAvatarByEmailAsync(e); if (a.IsError || a.Result == null) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "CloudinaryOASIS: Avatar not found."); return r; } return await DeleteAvatarAsync(a.Result.Id, softDelete); }
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
                await UploadRawAsync($"oasis/avatar-details/{d.Id}", Ser(d));
                if (!string.IsNullOrEmpty(d.Username)) await UploadRawAsync($"oasis/indexes/detail-by-username/{d.Username}", $"{{\"id\":\"{d.Id}\"}}");
                if (!string.IsNullOrEmpty(d.Email)) await UploadRawAsync($"oasis/indexes/detail-by-email/{d.Email}", $"{{\"id\":\"{d.Id}\"}}");
                result.Result = d; result.IsError = false; result.Message = "CloudinaryOASIS: AvatarDetail saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CloudinaryOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail d) => SaveAvatarDetailAsync(d).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var json = await DownloadRawAsync($"oasis/avatar-details/{id}");
                if (json == null) { OASISErrorHandling.HandleError(ref result, $"CloudinaryOASIS: No detail for ID '{id}'."); return result; }
                var d = Des<AvatarDetail>(json);
                if (d == null) { OASISErrorHandling.HandleError(ref result, $"CloudinaryOASIS: No detail for ID '{id}'."); return result; }
                result.Result = d; result.IsError = false; result.Message = "CloudinaryOASIS: AvatarDetail loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CloudinaryOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => LoadAvatarDetailAsync(id, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string u, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var json = await DownloadRawAsync($"oasis/indexes/detail-by-username/{u}");
                if (json == null) { OASISErrorHandling.HandleError(ref result, $"CloudinaryOASIS: No detail for username '{u}'."); return result; }
                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("id", out var idEl) || !Guid.TryParse(idEl.GetString(), out Guid id)) { OASISErrorHandling.HandleError(ref result, $"CloudinaryOASIS: No detail for username '{u}'."); return result; }
                return await LoadAvatarDetailAsync(id, version);
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CloudinaryOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string u, int version = 0) => LoadAvatarDetailByUsernameAsync(u, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string e, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var json = await DownloadRawAsync($"oasis/indexes/detail-by-email/{e}");
                if (json == null) { OASISErrorHandling.HandleError(ref result, $"CloudinaryOASIS: No detail for email '{e}'."); return result; }
                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("id", out var idEl) || !Guid.TryParse(idEl.GetString(), out Guid id)) { OASISErrorHandling.HandleError(ref result, $"CloudinaryOASIS: No detail for email '{e}'."); return result; }
                return await LoadAvatarDetailAsync(id, version);
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CloudinaryOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string e, int version = 0) => LoadAvatarDetailByEmailAsync(e, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                var pids = await ListPublicIdsAsync("oasis/avatar-details/");
                var list = new List<IAvatarDetail>();
                foreach (var pid in pids) { var json = await DownloadRawAsync(pid); if (json == null) continue; var d = Des<AvatarDetail>(json); if (d != null) list.Add(d); }
                result.Result = list; result.IsError = false; result.Message = $"CloudinaryOASIS: Loaded {list.Count} detail(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CloudinaryOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0) => LoadAllAvatarDetailsAsync(version).Result;

        // ─── Holons ───────────────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (holon.Id == Guid.Empty) holon.Id = Guid.NewGuid();
                await UploadRawAsync($"oasis/holons/{holon.Id}", Ser(holon));
                result.Result = holon; result.IsError = false; result.Message = $"CloudinaryOASIS: Holon '{holon.Name}' saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CloudinaryOASIS: Error saving holon: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, int version = 0) => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>(); var saved = new List<IHolon>();
            foreach (var h in holons) { var r = await SaveHolonAsync(h, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, version); if (!r.IsError && r.Result != null) saved.Add(r.Result); }
            result.Result = saved; result.IsError = false; result.Message = $"CloudinaryOASIS: Saved {saved.Count} holons."; return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, int version = 0) => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, version).Result;

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var json = await DownloadRawAsync($"oasis/holons/{id}");
                if (json == null) { OASISErrorHandling.HandleError(ref result, $"CloudinaryOASIS: No holon for ID '{id}'."); return result; }
                var h = Des<Holon>(json);
                if (h == null || h.IsDeleted) { OASISErrorHandling.HandleError(ref result, $"CloudinaryOASIS: No holon for ID '{id}'."); return result; }
                result.Result = h; result.IsError = false; result.Message = "CloudinaryOASIS: Holon loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CloudinaryOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IHolon>> LoadHolonByProviderKeyAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) { if (Guid.TryParse(providerKey, out Guid id)) return await LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version); var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, $"CloudinaryOASIS: Invalid GUID '{providerKey}'."); return r; }
        public override OASISResult<IHolon> LoadHolonByProviderKey(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonByProviderKeyAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var pids = await ListPublicIdsAsync("oasis/holons/"); var list = new List<IHolon>();
                foreach (var pid in pids) { var json = await DownloadRawAsync(pid); if (json == null) continue; var h = Des<Holon>(json); if (h == null || h.IsDeleted) continue; if (holonType != HolonType.All && h.HolonType != holonType) continue; list.Add(h); }
                result.Result = list; result.IsError = false; result.Message = $"CloudinaryOASIS: Loaded {list.Count} holon(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CloudinaryOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false) => LoadAllHolonsAsync(holonType, loadChildren, recursive, maxChildDepth, version, continueOnError, loadChildrenFromProvider).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false)
        {
            var allResult = await LoadAllHolonsAsync(holonType, loadChildren, recursive, maxChildDepth, version, continueOnError, loadChildrenFromProvider);
            if (allResult.IsError) return allResult;
            var filtered = new List<IHolon>(); foreach (var h in allResult.Result!) { if (h.ParentHolonId == id) filtered.Add(h); }
            return new OASISResult<IEnumerable<IHolon>> { Result = filtered, IsError = false, Message = $"CloudinaryOASIS: Loaded {filtered.Count} child holon(s)." };
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false) => LoadHolonsForParentAsync(id, holonType, loadChildren, recursive, maxChildDepth, version, continueOnError, loadChildrenFromProvider).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false) { if (Guid.TryParse(providerKey, out Guid id)) return await LoadHolonsForParentAsync(id, holonType, loadChildren, recursive, maxChildDepth, version, continueOnError, loadChildrenFromProvider); var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, $"CloudinaryOASIS: Invalid GUID '{providerKey}'."); return r; }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false) => LoadHolonsForParentAsync(providerKey, holonType, loadChildren, recursive, maxChildDepth, version, continueOnError, loadChildrenFromProvider).Result;

        public override async Task<OASISResult<bool>> DeleteHolonAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (softDelete) { var l = await LoadHolonAsync(id); if (!l.IsError && l.Result != null) { var h = (Holon)l.Result; h.DeletedDate = DateTime.UtcNow; await SaveHolonAsync(h); } }
                else { await DeleteRawAsync($"oasis/holons/{id}"); }
                result.Result = true; result.IsError = false; result.Message = $"CloudinaryOASIS: Holon '{id}' deleted.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CloudinaryOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> DeleteHolon(Guid id, bool softDelete = true) => DeleteHolonAsync(id, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteHolonAsync(string pk, bool softDelete = true) { if (Guid.TryParse(pk, out Guid id)) return await DeleteHolonAsync(id, softDelete); var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, $"CloudinaryOASIS: Invalid GUID '{pk}'."); return r; }
        public override OASISResult<bool> DeleteHolon(string pk, bool softDelete = true) => DeleteHolonAsync(pk, softDelete).Result;

        // ─── Search ───────────────────────────────────────────────────────────────

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>(); var sr = new SearchResults();
            var avatarResult = await LoadAllAvatarsAsync(); if (!avatarResult.IsError && avatarResult.Result != null) sr.Avatars = new List<IAvatar>(avatarResult.Result);
            var holonResult = await LoadAllHolonsAsync(); if (!holonResult.IsError && holonResult.Result != null) sr.Holons = new List<IHolon>(holonResult.Result);
            result.Result = sr; result.IsError = false; return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
        public override Task<OASISResult<IAvatar>> SearchAvatarsAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => throw new NotImplementedException("CloudinaryOASIS: Use SearchAsync.");
        public override OASISResult<IAvatar> SearchAvatars(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => throw new NotImplementedException();
        public override Task<OASISResult<IHolon>> SearchHolonsAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => throw new NotImplementedException("CloudinaryOASIS: Use SearchAsync.");
        public override OASISResult<IHolon> SearchHolons(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => throw new NotImplementedException();

        public override string GetProviderVersion() => "1.0.0";
        public override Task<string> GetProviderVersionAsync() => Task.FromResult("1.0.0");
    }
}
