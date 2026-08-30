using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
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

namespace NextGenSoftware.OASIS.API.Providers.MinIOOASIS
{
    /// <summary>
    /// OASIS provider for MinIO — an S3-compatible high-performance object storage system.
    ///
    /// Each avatar/holon is stored as a JSON object.  Index objects provide username → ID and
    /// email → ID lookups without listing all objects.
    ///
    /// Key prefixes:
    ///   oasis-avatars/{guid}.json
    ///   oasis-avatar-details/{guid}.json
    ///   oasis-holons/{guid}.json
    ///   oasis-indexes/avatar-by-username/{username}.json  → { "id": "..." }
    ///   oasis-indexes/avatar-by-email/{email}.json        → { "id": "..." }
    ///
    /// Constructor parameters:
    ///   endpoint        — MinIO server endpoint, e.g. "http://localhost:9000"
    ///   accessKey       — MinIO access key
    ///   secretKey       — MinIO secret key
    ///   bucketName      — bucket name (default: "oasis")
    ///   useSSL          — whether to use HTTPS (default: false)
    /// </summary>
    public class MinIOOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISDBStorageProvider
    {
        private AmazonS3Client _s3 = null!;
        private readonly string _endpoint;
        private readonly string _accessKey;
        private readonly string _secretKey;
        private readonly string _bucket;
        private readonly bool _useSSL;

        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public MinIOOASIS(string endpoint, string accessKey, string secretKey, string bucketName = "oasis", bool useSSL = false)
        {
            _endpoint = endpoint;
            _accessKey = accessKey;
            _secretKey = secretKey;
            _bucket = bucketName;
            _useSSL = useSSL;
            ProviderName = "MinIOOASIS";
            ProviderDescription = "MinIO provider (S3-compatible object storage via AWSSDK.S3)";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.MinIOOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageLocalAndNetwork);
        }

        private static string Ser(object obj) => JsonSerializer.Serialize(obj, _jsonOpts);
        private static T? Des<T>(string json) => JsonSerializer.Deserialize<T>(json, _jsonOpts);

        private async Task<string?> GetObjectAsync(string key)
        {
            try
            {
                var req = new GetObjectRequest { BucketName = _bucket, Key = key };
                using var resp = await _s3.GetObjectAsync(req);
                using var reader = new StreamReader(resp.ResponseStream);
                return await reader.ReadToEndAsync();
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound) { return null; }
        }

        private async Task PutObjectAsync(string key, string json)
        {
            var req = new PutObjectRequest
            {
                BucketName = _bucket,
                Key = key,
                ContentType = "application/json",
                ContentBody = json
            };
            await _s3.PutObjectAsync(req);
        }

        private async Task DeleteObjectAsync(string key)
        {
            await _s3.DeleteObjectAsync(new DeleteObjectRequest { BucketName = _bucket, Key = key });
        }

        private async Task<List<string>> ListKeysAsync(string prefix)
        {
            var keys = new List<string>();
            string? continuationToken = null;
            do
            {
                var req = new ListObjectsV2Request { BucketName = _bucket, Prefix = prefix, ContinuationToken = continuationToken };
                var resp = await _s3.ListObjectsV2Async(req);
                foreach (var obj in resp.S3Objects) keys.Add(obj.Key);
                continuationToken = resp.IsTruncated ? resp.NextContinuationToken : null;
            } while (continuationToken != null);
            return keys;
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                var config = new AmazonS3Config
                {
                    ServiceURL = _endpoint,
                    ForcePathStyle = true,
                    UseHttp = !_useSSL
                };
                _s3 = new AmazonS3Client(_accessKey, _secretKey, config);
                try { await _s3.HeadBucketAsync(new HeadBucketRequest { BucketName = _bucket }); }
                catch { await _s3.PutBucketAsync(new PutBucketRequest { BucketName = _bucket, UseClientRegion = true }); }
                IsProviderActivated = true; result.Result = true; result.IsError = false; result.Message = "MinIOOASIS activated.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"MinIOOASIS: Error activating — {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;
        public override async Task<OASISResult<bool>> DeActivateProviderAsync() { _s3?.Dispose(); IsProviderActivated = false; return await Task.FromResult(new OASISResult<bool> { Result = true, IsError = false, Message = "MinIOOASIS deactivated." }); }
        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // ─── Avatar ───────────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (avatar.Id == Guid.Empty) avatar.Id = Guid.NewGuid();
                if (avatar.ProviderUniqueStorageKey == null) avatar.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.MinIOOASIS] = avatar.Id.ToString();
                await PutObjectAsync($"oasis-avatars/{avatar.Id}.json", Ser(avatar));
                if (!string.IsNullOrEmpty(avatar.Username)) await PutObjectAsync($"oasis-indexes/avatar-by-username/{avatar.Username}.json", $"{{\"id\":\"{avatar.Id}\"}}");
                if (!string.IsNullOrEmpty(avatar.Email)) await PutObjectAsync($"oasis-indexes/avatar-by-email/{avatar.Email}.json", $"{{\"id\":\"{avatar.Id}\"}}");
                result.Result = avatar; result.IsError = false; result.Message = $"MinIOOASIS: Avatar '{avatar.Username}' saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"MinIOOASIS: Error saving avatar: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) => SaveAvatarAsync(avatar).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var json = await GetObjectAsync($"oasis-avatars/{id}.json");
                if (json == null) { OASISErrorHandling.HandleError(ref result, $"MinIOOASIS: No avatar for ID '{id}'."); return result; }
                var a = Des<Avatar>(json);
                if (a == null || a.IsDeleted) { OASISErrorHandling.HandleError(ref result, $"MinIOOASIS: No avatar for ID '{id}'."); return result; }
                result.Result = a; result.IsError = false; result.Message = "MinIOOASIS: Avatar loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"MinIOOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) => LoadAvatarAsync(id, version).Result;

        private async Task<Guid?> LookupIdByUsernameAsync(string username)
        {
            var json = await GetObjectAsync($"oasis-indexes/avatar-by-username/{username}.json");
            if (json == null) return null;
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("id", out var idEl) && Guid.TryParse(idEl.GetString(), out Guid id)) return id;
            return null;
        }

        private async Task<Guid?> LookupIdByEmailAsync(string email)
        {
            var json = await GetObjectAsync($"oasis-indexes/avatar-by-email/{email}.json");
            if (json == null) return null;
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("id", out var idEl) && Guid.TryParse(idEl.GetString(), out Guid id)) return id;
            return null;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var id = await LookupIdByUsernameAsync(username);
                if (!id.HasValue) { OASISErrorHandling.HandleError(ref result, $"MinIOOASIS: No avatar for username '{username}'."); return result; }
                return await LoadAvatarAsync(id.Value, version);
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"MinIOOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string username, int version = 0) => LoadAvatarByUsernameAsync(username, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var id = await LookupIdByEmailAsync(avatarEmail);
                if (!id.HasValue) { OASISErrorHandling.HandleError(ref result, $"MinIOOASIS: No avatar for email '{avatarEmail}'."); return result; }
                return await LoadAvatarAsync(id.Value, version);
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"MinIOOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0) => LoadAvatarByEmailAsync(avatarEmail, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            if (Guid.TryParse(providerKey, out Guid id)) return await LoadAvatarAsync(id, version);
            var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, $"MinIOOASIS: Invalid GUID '{providerKey}'."); return r;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0) => LoadAvatarByProviderKeyAsync(providerKey, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                var keys = await ListKeysAsync("oasis-avatars/");
                var list = new List<IAvatar>();
                foreach (var key in keys)
                {
                    if (!key.EndsWith(".json")) continue;
                    var json = await GetObjectAsync(key);
                    if (json == null) continue;
                    var a = Des<Avatar>(json);
                    if (a != null && !a.IsDeleted) list.Add(a);
                }
                result.Result = list; result.IsError = false; result.Message = $"MinIOOASIS: Loaded {list.Count} avatar(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"MinIOOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) => LoadAllAvatarsAsync(version).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (softDelete) { var l = await LoadAvatarAsync(id); if (!l.IsError && l.Result != null) { var a = (Avatar)l.Result; a.DeletedDate = DateTime.UtcNow; await SaveAvatarAsync(a); } }
                else { await DeleteObjectAsync($"oasis-avatars/{id}.json"); }
                result.Result = true; result.IsError = false; result.Message = $"MinIOOASIS: Avatar '{id}' deleted.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"MinIOOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) => DeleteAvatarAsync(id, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string u, bool softDelete = true) { var a = await LoadAvatarByUsernameAsync(u); if (a.IsError || a.Result == null) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, $"MinIOOASIS: Avatar '{u}' not found."); return r; } return await DeleteAvatarAsync(a.Result.Id, softDelete); }
        public override OASISResult<bool> DeleteAvatarByUsername(string u, bool softDelete = true) => DeleteAvatarByUsernameAsync(u, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string e, bool softDelete = true) { var a = await LoadAvatarByEmailAsync(e); if (a.IsError || a.Result == null) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "MinIOOASIS: Avatar not found."); return r; } return await DeleteAvatarAsync(a.Result.Id, softDelete); }
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
                await PutObjectAsync($"oasis-avatar-details/{d.Id}.json", Ser(d));
                if (!string.IsNullOrEmpty(d.Username)) await PutObjectAsync($"oasis-indexes/detail-by-username/{d.Username}.json", $"{{\"id\":\"{d.Id}\"}}");
                if (!string.IsNullOrEmpty(d.Email)) await PutObjectAsync($"oasis-indexes/detail-by-email/{d.Email}.json", $"{{\"id\":\"{d.Id}\"}}");
                result.Result = d; result.IsError = false; result.Message = "MinIOOASIS: AvatarDetail saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"MinIOOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail d) => SaveAvatarDetailAsync(d).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var json = await GetObjectAsync($"oasis-avatar-details/{id}.json");
                if (json == null) { OASISErrorHandling.HandleError(ref result, $"MinIOOASIS: No detail for ID '{id}'."); return result; }
                var d = Des<AvatarDetail>(json);
                if (d == null) { OASISErrorHandling.HandleError(ref result, $"MinIOOASIS: No detail for ID '{id}'."); return result; }
                result.Result = d; result.IsError = false; result.Message = "MinIOOASIS: AvatarDetail loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"MinIOOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => LoadAvatarDetailAsync(id, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string u, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var json = await GetObjectAsync($"oasis-indexes/detail-by-username/{u}.json");
                if (json == null) { OASISErrorHandling.HandleError(ref result, $"MinIOOASIS: No detail for username '{u}'."); return result; }
                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("id", out var idEl) || !Guid.TryParse(idEl.GetString(), out Guid id)) { OASISErrorHandling.HandleError(ref result, $"MinIOOASIS: No detail for username '{u}'."); return result; }
                return await LoadAvatarDetailAsync(id, version);
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"MinIOOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string u, int version = 0) => LoadAvatarDetailByUsernameAsync(u, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string e, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var json = await GetObjectAsync($"oasis-indexes/detail-by-email/{e}.json");
                if (json == null) { OASISErrorHandling.HandleError(ref result, $"MinIOOASIS: No detail for email '{e}'."); return result; }
                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("id", out var idEl) || !Guid.TryParse(idEl.GetString(), out Guid id)) { OASISErrorHandling.HandleError(ref result, $"MinIOOASIS: No detail for email '{e}'."); return result; }
                return await LoadAvatarDetailAsync(id, version);
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"MinIOOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string e, int version = 0) => LoadAvatarDetailByEmailAsync(e, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                var keys = await ListKeysAsync("oasis-avatar-details/");
                var list = new List<IAvatarDetail>();
                foreach (var key in keys) { if (!key.EndsWith(".json")) continue; var json = await GetObjectAsync(key); if (json == null) continue; var d = Des<AvatarDetail>(json); if (d != null) list.Add(d); }
                result.Result = list; result.IsError = false; result.Message = $"MinIOOASIS: Loaded {list.Count} detail(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"MinIOOASIS: {ex.Message}"); }
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
                await PutObjectAsync($"oasis-holons/{holon.Id}.json", Ser(holon));
                result.Result = holon; result.IsError = false; result.Message = $"MinIOOASIS: Holon '{holon.Name}' saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"MinIOOASIS: Error saving holon: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, int version = 0) => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>(); var saved = new List<IHolon>();
            foreach (var h in holons) { var r = await SaveHolonAsync(h, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, version); if (!r.IsError && r.Result != null) saved.Add(r.Result); }
            result.Result = saved; result.IsError = false; result.Message = $"MinIOOASIS: Saved {saved.Count} holons."; return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, int version = 0) => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, version).Result;

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var json = await GetObjectAsync($"oasis-holons/{id}.json");
                if (json == null) { OASISErrorHandling.HandleError(ref result, $"MinIOOASIS: No holon for ID '{id}'."); return result; }
                var h = Des<Holon>(json);
                if (h == null || h.IsDeleted) { OASISErrorHandling.HandleError(ref result, $"MinIOOASIS: No holon for ID '{id}'."); return result; }
                result.Result = h; result.IsError = false; result.Message = "MinIOOASIS: Holon loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"MinIOOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IHolon>> LoadHolonByProviderKeyAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) { if (Guid.TryParse(providerKey, out Guid id)) return await LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version); var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, $"MinIOOASIS: Invalid GUID '{providerKey}'."); return r; }
        public override OASISResult<IHolon> LoadHolonByProviderKey(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonByProviderKeyAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var keys = await ListKeysAsync("oasis-holons/"); var list = new List<IHolon>();
                foreach (var key in keys)
                {
                    if (!key.EndsWith(".json")) continue;
                    var json = await GetObjectAsync(key); if (json == null) continue;
                    var h = Des<Holon>(json); if (h == null || h.IsDeleted) continue;
                    if (holonType != HolonType.All && h.HolonType != holonType) continue;
                    list.Add(h);
                }
                result.Result = list; result.IsError = false; result.Message = $"MinIOOASIS: Loaded {list.Count} holon(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"MinIOOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false) => LoadAllHolonsAsync(holonType, loadChildren, recursive, maxChildDepth, version, continueOnError, loadChildrenFromProvider).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false)
        {
            var allResult = await LoadAllHolonsAsync(holonType, loadChildren, recursive, maxChildDepth, version, continueOnError, loadChildrenFromProvider);
            if (allResult.IsError) return allResult;
            var filtered = new List<IHolon>(); foreach (var h in allResult.Result!) { if (h.ParentHolonId == id) filtered.Add(h); }
            return new OASISResult<IEnumerable<IHolon>> { Result = filtered, IsError = false, Message = $"MinIOOASIS: Loaded {filtered.Count} child holon(s)." };
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false) => LoadHolonsForParentAsync(id, holonType, loadChildren, recursive, maxChildDepth, version, continueOnError, loadChildrenFromProvider).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false) { if (Guid.TryParse(providerKey, out Guid id)) return await LoadHolonsForParentAsync(id, holonType, loadChildren, recursive, maxChildDepth, version, continueOnError, loadChildrenFromProvider); var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, $"MinIOOASIS: Invalid GUID '{providerKey}'."); return r; }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false) => LoadHolonsForParentAsync(providerKey, holonType, loadChildren, recursive, maxChildDepth, version, continueOnError, loadChildrenFromProvider).Result;

        public override async Task<OASISResult<bool>> DeleteHolonAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (softDelete) { var l = await LoadHolonAsync(id); if (!l.IsError && l.Result != null) { var h = (Holon)l.Result; h.DeletedDate = DateTime.UtcNow; await SaveHolonAsync(h); } }
                else { await DeleteObjectAsync($"oasis-holons/{id}.json"); }
                result.Result = true; result.IsError = false; result.Message = $"MinIOOASIS: Holon '{id}' deleted.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"MinIOOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> DeleteHolon(Guid id, bool softDelete = true) => DeleteHolonAsync(id, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteHolonAsync(string pk, bool softDelete = true) { if (Guid.TryParse(pk, out Guid id)) return await DeleteHolonAsync(id, softDelete); var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, $"MinIOOASIS: Invalid GUID '{pk}'."); return r; }
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
        public override Task<OASISResult<IAvatar>> SearchAvatarsAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => throw new NotImplementedException("MinIOOASIS: Use SearchAsync.");
        public override OASISResult<IAvatar> SearchAvatars(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => throw new NotImplementedException();
        public override Task<OASISResult<IHolon>> SearchHolonsAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => throw new NotImplementedException("MinIOOASIS: Use SearchAsync.");
        public override OASISResult<IHolon> SearchHolons(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => throw new NotImplementedException();

        public override string GetProviderVersion() => "1.0.0";
        public override Task<string> GetProviderVersionAsync() => Task.FromResult("1.0.0");
    }
}
