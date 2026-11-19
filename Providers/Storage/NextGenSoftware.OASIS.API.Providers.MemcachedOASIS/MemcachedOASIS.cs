using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Enyim.Caching;
using Enyim.Caching.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
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

namespace NextGenSoftware.OASIS.API.Providers.MemcachedOASIS
{
    /// <summary>
    /// OASIS provider for Memcached — distributed memory object caching via EnyimMemcachedCore.
    ///
    /// Key patterns:
    ///   avatar:{guid}, avatar-detail:{guid}, holon:{guid}
    ///   avatar-by-username:{u}, avatar-by-email:{e}
    ///   detail-by-username:{u}, detail-by-email:{e}
    ///   avatars:index — JSON array of GUID strings
    ///   holons:index  — JSON array of GUID strings
    /// </summary>
    public partial class MemcachedOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISDBStorageProvider
    {
        /// <summary>
        /// When true this provider stores a new record per save and links to the previous
        /// version (blockchain-style) instead of updating in place.
        /// </summary>
        public bool IsVersionControlEnabled { get; set; }

        private readonly string _host;
        private readonly int _port;
        private MemcachedClient _client = null!;

        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private static string Ser(object obj) => JsonSerializer.Serialize(obj, _jsonOpts);
        private static T? Des<T>(string? s) => s == null ? default : JsonSerializer.Deserialize<T>(s, _jsonOpts);

        // Memcached keys must not contain spaces or control chars; sanitize index key separators
        private static string SafeKey(string k) => k.Replace(" ", "_").Replace("\n", "").Replace("\r", "");

        public MemcachedOASIS(string host = "localhost", int port = 11211)
        {
            _host = host;
            _port = port;
            ProviderName = "MemcachedOASIS";
            ProviderDescription = "Memcached provider (distributed memory caching via EnyimMemcachedCore)";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.MemcachedOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageLocalAndNetwork);
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                IsProviderActivated = false;
                _client?.Dispose();
                _client = null;
                var config = new MemcachedClientConfiguration(Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance, new MemcachedClientOptions
                {
                    Servers = new List<Server> { new Server { Address = _host, Port = _port } }
                });
                _client = new MemcachedClient(Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance, config);
                await GetStoredJsonAsync($"oasis:activation:{Guid.NewGuid():N}");
                IsProviderActivated = true;
                result.Result = true; result.IsError = false; result.Message = "MemcachedOASIS activated.";
            }
            catch (Exception ex)
            {
                _client?.Dispose();
                _client = null;
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"MemcachedOASIS: Error activating — {ex.Message}");
            }
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            _client?.Dispose();
            _client = null;
            IsProviderActivated = false;
            return await Task.FromResult(new OASISResult<bool> { Result = true, IsError = false, Message = "MemcachedOASIS deactivated." });
        }

        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        private async Task<string?> GetStoredJsonAsync(string key)
        {
            var result = await _client.GetAsync<string>(SafeKey(key));
            if (!result.Success)
            {
                // Native binary protocol status 1 means KEY_ENOENT. Connection,
                // timeout and server errors must not masquerade as an absent key.
                if (result.StatusCode == 1 && result.Exception == null) return null;
                throw new System.IO.IOException("Memcached read failed: " + result.Message, result.Exception);
            }
            return result.Value ?? throw new System.IO.InvalidDataException("Successful Memcached string read omitted its value.");
        }

        private async Task<T?> GetAsync<T>(string key) where T : class
        {
            var value = await GetStoredJsonAsync(key);
            return value == null ? null : Des<T>(value);
        }

        private Task SetAsync(string key, object value) => SetStoredJsonAsync(key, Ser(value));

        private async Task SetStoredJsonAsync(string key, string json)
        {
            if (!await _client.SetAsync(SafeKey(key), json, 0))
                throw new System.IO.IOException("Memcached did not acknowledge the cache write.");
        }
        private async Task DeleteAsync(string key)
        {
            if (!await _client.RemoveAsync(SafeKey(key)))
                throw new System.IO.IOException("Memcached did not acknowledge the cache deletion.");
        }

        private async Task<List<string>> GetIndexAsync(string indexKey)
        {
            var raw = await GetStoredJsonAsync(indexKey);
            return raw == null ? new List<string>() : JsonSerializer.Deserialize<List<string>>(raw) ?? new List<string>();
        }

        private async Task AddToIndexAsync(string indexKey, string value)
        {
            var idx = await GetIndexAsync(indexKey);
            if (!idx.Contains(value)) { idx.Add(value); await SetAsync(indexKey, idx); }
        }

        private async Task RemoveFromIndexAsync(string indexKey, string value)
        {
            var idx = await GetIndexAsync(indexKey);
            if (idx.Remove(value)) await SetAsync(indexKey, idx);
        }

        // ─── Avatar ───────────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (avatar.Id == Guid.Empty) avatar.Id = Guid.NewGuid();
                if (avatar.ProviderUniqueStorageKey == null) avatar.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.MemcachedOASIS] = avatar.Id.ToString();
                await SetAsync($"avatar:{avatar.Id}", avatar);
                if (!string.IsNullOrEmpty(avatar.Username)) await SetAsync($"avatar-by-username:{avatar.Username.ToLowerInvariant()}", avatar.Id.ToString());
                if (!string.IsNullOrEmpty(avatar.Email)) await SetAsync($"avatar-by-email:{avatar.Email.ToLowerInvariant()}", avatar.Id.ToString());
                await AddToIndexAsync("avatars:index", avatar.Id.ToString());
                result.Result = avatar; result.IsError = false; result.IsSaved = true; result.SavedCount = 1; result.Message = $"MemcachedOASIS: Avatar '{avatar.Username}' saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"MemcachedOASIS: Error saving avatar: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) => SaveAvatarAsync(avatar).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var a = await GetAsync<Avatar>($"avatar:{id}");
                if (a == null || a.IsDeleted) { OASISErrorHandling.HandleError(ref result, $"MemcachedOASIS: No avatar for ID '{id}'."); result.ErrorCode = OASISResultErrorCodes.RecordNotFound; return result; }
                result.Result = a; result.IsError = false; result.IsLoaded = true; result.LoadedCount = 1; result.Message = "MemcachedOASIS: Avatar loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"MemcachedOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) => LoadAvatarAsync(id, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var idStr = await GetAsync<string>($"avatar-by-username:{username.ToLowerInvariant()}");
                if (idStr == null || !Guid.TryParse(idStr, out Guid id)) { OASISErrorHandling.HandleError(ref result, $"MemcachedOASIS: No avatar for username '{username}'."); return result; }
                return await LoadAvatarAsync(id, version);
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"MemcachedOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string username, int version = 0) => LoadAvatarByUsernameAsync(username, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var idStr = await GetAsync<string>($"avatar-by-email:{avatarEmail.ToLowerInvariant()}");
                if (idStr == null || !Guid.TryParse(idStr, out Guid id)) { OASISErrorHandling.HandleError(ref result, $"MemcachedOASIS: No avatar for email '{avatarEmail}'."); return result; }
                return await LoadAvatarAsync(id, version);
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"MemcachedOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0) => LoadAvatarByEmailAsync(avatarEmail, version).Result;
        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string pk, int version = 0) { if (Guid.TryParse(pk, out Guid id)) return await LoadAvatarAsync(id, version); var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, $"MemcachedOASIS: Invalid GUID '{pk}'."); return r; }
        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string pk, int version = 0) => LoadAvatarByProviderKeyAsync(pk, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                var ids = await GetIndexAsync("avatars:index");
                var list = new List<IAvatar>();
                foreach (var idText in ids)
                {
                    if (!Guid.TryParse(idText, out var id))
                        throw new System.IO.InvalidDataException("Memcached index contains an invalid record ID.");
                    var row = await LoadAvatarAsync(id, version);
                    if (row.ErrorCode == OASISResultErrorCodes.RecordNotFound) continue;
                    if (row.IsError || row.Result == null)
                        throw new System.IO.IOException(row.Message, row.Exception);
                    list.Add(row.Result);
                }
                result.Result = list; result.IsError = false; result.IsLoaded = true; result.LoadedCount = list.Count; result.Message = $"MemcachedOASIS: Loaded {list.Count} avatar(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"MemcachedOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) => LoadAllAvatarsAsync(version).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                var key = $"avatar:{id}";
                var avatar = await GetAsync<Avatar>(key);
                if (avatar == null || (softDelete && avatar.IsDeleted))
                {
                    result.IsError = true;
                    result.ErrorCode = OASISResultErrorCodes.RecordNotFound;
                    result.Message = $"MemcachedOASIS: No avatar to delete for ID '{id}'.";
                    return result;
                }
                if (softDelete)
                {
                    avatar.DeletedDate = DateTime.UtcNow;
                    await SetAsync(key, avatar);
                }
                else
                {
                    var detailKey = $"avatar-detail:{id}";
                    if (await GetStoredJsonAsync(detailKey) != null) await DeleteAsync(detailKey);
                    await DeleteAsync(key);
                    await RemoveFromIndexAsync("avatars:index", id.ToString("D"));
                }
                result.Result = true;
                result.IsDeleted = true;
                result.DeletedCount = 1;
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"MemcachedOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) => DeleteAvatarAsync(id, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string u, bool softDelete = true) { var a = await LoadAvatarByUsernameAsync(u); if (a.IsError || a.Result == null) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, $"MemcachedOASIS: Avatar '{u}' not found."); return r; } return await DeleteAvatarAsync(a.Result.Id, softDelete); }
        public override OASISResult<bool> DeleteAvatarByUsername(string u, bool softDelete = true) => DeleteAvatarByUsernameAsync(u, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string e, bool softDelete = true) { var a = await LoadAvatarByEmailAsync(e); if (a.IsError || a.Result == null) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, $"MemcachedOASIS: Avatar not found."); return r; } return await DeleteAvatarAsync(a.Result.Id, softDelete); }
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
                if (d.Id == Guid.Empty) d.Id = Guid.NewGuid();
                await SetStoredJsonAsync($"avatar-detail:{d.Id}", StoredRecordJson.Serialize(d));
                if (!string.IsNullOrEmpty(d.Username)) await SetAsync($"detail-by-username:{d.Username.ToLowerInvariant()}", d.Id.ToString());
                if (!string.IsNullOrEmpty(d.Email)) await SetAsync($"detail-by-email:{d.Email.ToLowerInvariant()}", d.Id.ToString());
                result.Result = d; result.IsError = false; result.IsSaved = true; result.SavedCount = 1; result.Message = "MemcachedOASIS: AvatarDetail saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"MemcachedOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail d) => SaveAvatarDetailAsync(d).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var json = await GetStoredJsonAsync($"avatar-detail:{id}");
                var d = json == null ? null : StoredRecordJson.DeserializeStored<AvatarDetail>(json);
                if (d == null) { OASISErrorHandling.HandleError(ref result, $"MemcachedOASIS: No detail for ID '{id}'."); result.ErrorCode = OASISResultErrorCodes.RecordNotFound; return result; }
                result.Result = d; result.IsError = false; result.IsLoaded = true; result.LoadedCount = 1; result.Message = "MemcachedOASIS: AvatarDetail loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"MemcachedOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => LoadAvatarDetailAsync(id, version).Result;
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string u, int version = 0) { var result = new OASISResult<IAvatarDetail>(); try { var idStr = await GetAsync<string>($"detail-by-username:{u.ToLowerInvariant()}"); if (idStr == null || !Guid.TryParse(idStr, out Guid id)) { OASISErrorHandling.HandleError(ref result, $"MemcachedOASIS: No detail for username '{u}'."); return result; } return await LoadAvatarDetailAsync(id, version); } catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"MemcachedOASIS: {ex.Message}"); } return result; }
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string u, int version = 0) => LoadAvatarDetailByUsernameAsync(u, version).Result;
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string e, int version = 0) { var result = new OASISResult<IAvatarDetail>(); try { var idStr = await GetAsync<string>($"detail-by-email:{e.ToLowerInvariant()}"); if (idStr == null || !Guid.TryParse(idStr, out Guid id)) { OASISErrorHandling.HandleError(ref result, $"MemcachedOASIS: No detail for email '{e}'."); return result; } return await LoadAvatarDetailAsync(id, version); } catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"MemcachedOASIS: {ex.Message}"); } return result; }
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string e, int version = 0) => LoadAvatarDetailByEmailAsync(e, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                var avatarResult = await LoadAllAvatarsAsync(version);
                var list = new List<IAvatarDetail>();
                if (!avatarResult.IsError && avatarResult.Result != null) foreach (var a in avatarResult.Result) { var dr = await LoadAvatarDetailAsync(a.Id, version); if (!dr.IsError && dr.Result != null) list.Add(dr.Result); }
                result.Result = list; result.IsError = false; result.IsLoaded = true; result.LoadedCount = list.Count; result.Message = $"MemcachedOASIS: Loaded {list.Count} detail(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"MemcachedOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0) => LoadAllAvatarDetailsAsync(version).Result;

        // ─── Holons ───────────────────────────────────────────────────────────────

        public override Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonGraphAsync(holon, saveChildren, recursive, maxChildDepth, 0, continueOnError);

        private async Task<OASISResult<IHolon>> SaveHolonGraphAsync(IHolon holon, bool saveChildren, bool recursive, int maxChildDepth, int currentChildDepth, bool continueOnError)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var plan = HolonGraphWritePlan.CreateForSave(new[] { holon }, saveChildren, recursive, maxChildDepth, currentChildDepth);
                foreach (var node in plan.Nodes)
                {
                    try
                    {
                        var snapshot = node.Snapshot;
                        var keys = snapshot.ProviderUniqueStorageKey ??= new Dictionary<ProviderType, string>();
                        keys[Core.Enums.ProviderType.MemcachedOASIS] = snapshot.Id.ToString("D");
                        await SetAsync($"holon:{snapshot.Id}", snapshot);
                        await AddToIndexAsync("holons:index", snapshot.Id.ToString("D"));

                        node.Source.Id = snapshot.Id;
                        node.Source.ParentHolonId = snapshot.ParentHolonId;
                        node.Source.ProviderUniqueStorageKey = keys;
                        result.SavedCount++;
                        if (ReferenceEquals(node.Source, holon)) result.Result = holon;
                    }
                    catch (Exception error)
                    {
                        result.Exception ??= error;
                        result.ErrorCount++;
                        if (!continueOnError) break;
                    }
                }
                result.IsSaved = result.SavedCount > 0;
                if (result.ErrorCount > 0)
                    OASISErrorHandling.HandleError(ref result, "Memcached did not acknowledge every graph record and its index.", result.Exception, incrementErrorCount: false);
            }
            catch (Exception error)
            {
                OASISErrorHandling.HandleError(ref result, error.Message, error);
            }
            return result;
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
                if (write.IsError)
                {
                    errors.Add(write.Message);
                    if (write.Result != null) saved.Add(write.Result);
                    result.Exception ??= write.Exception;
                    if (!continueOnError) break;
                }
                else if (write.IsSaved && write.Result != null)
                {
                    saved.Add(write.Result);
                }
            }
            result.Result = saved;
            result.IsSaved = result.SavedCount > 0;
            result.IsError = errors.Count > 0;
            result.ErrorCount = errors.Count;
            result.Message = result.IsError ? string.Join("; ", errors) : $"MemcachedOASIS: Saved {result.SavedCount} holons.";
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var h = await GetAsync<Holon>($"holon:{id}");
                if (h == null || h.IsDeleted) { OASISErrorHandling.HandleError(ref result, $"MemcachedOASIS: No holon for ID '{id}'."); result.ErrorCode = OASISResultErrorCodes.RecordNotFound; return result; }
                result.Result = h; result.IsError = false; result.IsLoaded = true; result.LoadedCount = 1; result.Message = "MemcachedOASIS: Holon loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"MemcachedOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        public async Task<OASISResult<IHolon>> LoadHolonByProviderKeyAsync(string pk, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) { if (Guid.TryParse(pk, out Guid id)) return await LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version); var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, $"MemcachedOASIS: Invalid GUID '{pk}'."); return r; }
        public OASISResult<IHolon> LoadHolonByProviderKey(string pk, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonByProviderKeyAsync(pk, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var ids = await GetIndexAsync("holons:index");
                var list = new List<IHolon>();
                foreach (var idText in ids)
                {
                    if (!Guid.TryParse(idText, out var id))
                        throw new System.IO.InvalidDataException("Memcached index contains an invalid record ID.");
                    var row = await LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);
                    if (row.ErrorCode == OASISResultErrorCodes.RecordNotFound) continue;
                    if (row.IsError || row.Result == null)
                        throw new System.IO.IOException(row.Message, row.Exception);
                    if (holonType != HolonType.All && row.Result.HolonType != holonType) continue;
                    list.Add(row.Result);
                }
                result.Result = list; result.IsError = false; result.IsLoaded = true; result.LoadedCount = list.Count; result.Message = $"MemcachedOASIS: Loaded {list.Count} holon(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"MemcachedOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadAllHolonsAsync(holonType, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        { var result = new OASISResult<IEnumerable<IHolon>>(); try { var all = await LoadAllHolonsAsync(holonType, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version); if (all.IsError) return all; var list = new List<IHolon>(); if (all.Result != null) foreach (var h in all.Result) { if (h.ParentHolonId == id) list.Add(h); } result.Result = list; result.IsError = false; result.IsLoaded = true; result.LoadedCount = list.Count; result.Message = $"MemcachedOASIS: Loaded {list.Count} child holon(s)."; } catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"MemcachedOASIS: {ex.Message}"); } return result; }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsForParentAsync(id, holonType, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string pk, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) { if (Guid.TryParse(pk, out Guid id)) return await LoadHolonsForParentAsync(id, holonType, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version); var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, $"MemcachedOASIS: Invalid GUID '{pk}'."); return r; }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string pk, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsForParentAsync(pk, holonType, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public async Task<OASISResult<bool>> DeleteHolonSoftAsync(Guid id, bool softDelete = true)
        {
            var deleted = await DeleteHolonRecordAsync(id, softDelete);
            return new OASISResult<bool>
            {
                Result = deleted.IsDeleted,
                IsDeleted = deleted.IsDeleted,
                DeletedCount = deleted.DeletedCount,
                IsError = deleted.IsError,
                ErrorCode = deleted.ErrorCode,
                Exception = deleted.Exception,
                Message = deleted.Message
            };
        }

        private async Task<OASISResult<IHolon>> DeleteHolonRecordAsync(Guid id, bool softDelete)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var key = $"holon:{id}";
                var holon = await GetAsync<Holon>(key);
                if (holon == null || (softDelete && holon.IsDeleted))
                {
                    result.IsError = true;
                    result.ErrorCode = OASISResultErrorCodes.RecordNotFound;
                    result.Message = $"MemcachedOASIS: No holon to delete for ID '{id}'.";
                    return result;
                }
                if (softDelete)
                {
                    holon.DeletedDate = DateTime.UtcNow;
                    await SetAsync(key, holon);
                }
                else
                {
                    await DeleteAsync(key);
                    await RemoveFromIndexAsync("holons:index", id.ToString("D"));
                }
                result.Result = holon;
                result.IsDeleted = true;
                result.DeletedCount = 1;
            }
            catch (Exception error)
            {
                OASISErrorHandling.HandleError(ref result, $"MemcachedOASIS: {error.Message}", error);
            }
            return result;
        }

        public OASISResult<bool> DeleteHolonSoft(Guid id, bool softDelete = true) => DeleteHolonSoftAsync(id, softDelete).Result;
        public async Task<OASISResult<bool>> DeleteHolonSoftAsync(string pk, bool softDelete = true) { if (Guid.TryParse(pk, out Guid id)) return await DeleteHolonSoftAsync(id, softDelete); var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, $"MemcachedOASIS: Invalid GUID '{pk}'."); return r; }
        public OASISResult<bool> DeleteHolonSoft(string pk, bool softDelete = true) => DeleteHolonSoftAsync(pk, softDelete).Result;


        public string GetProviderVersion() => "1.0.0";
        public Task<string> GetProviderVersionAsync() => Task.FromResult("1.0.0");

        // ─── Remaining IOASISStorageProvider surface ─────────────────────────────



        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            if (Guid.TryParse(providerKey, out var id))
                return await LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);

            var all = await LoadAllHolonsAsync();
            if (all.IsError || all.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, all.Message);
                return result;
            }

            foreach (var holon in all.Result)
            {
                if (holon.ProviderUniqueStorageKey != null
                    && holon.ProviderUniqueStorageKey.TryGetValue(ProviderType.Value, out var key)
                    && key == providerKey)
                {
                    result.Result = holon;
                    return result;
                }
            }

            OASISErrorHandling.HandleError(ref result, $"Holon with provider key '{providerKey}' not found.");
            return result;
        }



        public override OASISResult<IHolon> DeleteHolon(Guid id)
            => DeleteHolonAsync(id).GetAwaiter().GetResult();

        public override Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
            => DeleteHolonRecordAsync(id, softDelete: true);

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
            => DeleteHolonAsync(providerKey).GetAwaiter().GetResult();

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            var result = new OASISResult<IHolon>();
            var holon = await LoadHolonAsync(providerKey);
            if (holon.IsError || holon.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, holon.Message);
                return result;
            }
            return await DeleteHolonAsync(holon.Result.Id);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var all = await LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
            if (all.IsError || all.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, all.Message);
                return result;
            }

            var matches = new List<IHolon>();
            foreach (var holon in all.Result)
            {
                if (holon.MetaData != null
                    && holon.MetaData.TryGetValue(metaKey, out var value)
                    && value?.ToString() == metaValue)
                    matches.Add(holon);
            }

            result.Result = matches;
            result.IsLoaded = true;
            result.LoadedCount = matches.Count;
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var all = await LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
            if (all.IsError || all.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, all.Message);
                return result;
            }

            if (metaKeyValuePairs == null || metaKeyValuePairs.Count == 0)
            {
                result.Result = new List<IHolon>(all.Result);
                return result;
            }

            var matches = new List<IHolon>();
            foreach (var holon in all.Result)
            {
                if (holon.MetaData == null) continue;

                var matched = 0;
                foreach (var pair in metaKeyValuePairs)
                {
                    if (holon.MetaData.TryGetValue(pair.Key, out var value) && value?.ToString() == pair.Value)
                        matched++;
                }

                var isMatch = metaKeyValuePairMatchMode == MetaKeyValuePairMatchMode.All
                    ? matched == metaKeyValuePairs.Count
                    : matched > 0;

                if (isMatch) matches.Add(holon);
            }

            result.Result = matches;
            result.IsLoaded = true;
            result.LoadedCount = matches.Count;
            return result;
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
            => ImportAsync(holons).GetAwaiter().GetResult();

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var result = new OASISResult<bool>();
            var saved = await SaveHolonsAsync(holons);
            result.Result = saved.IsSaved && !saved.IsError;
            result.IsError = saved.IsError;
            result.Exception = saved.Exception;
            result.Message = saved.Message;
            result.IsSaved = saved.IsSaved;
            result.SavedCount = saved.SavedCount;
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
            => ExportAllAsync(version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
            => await LoadAllHolonsAsync(HolonType.All, true, true, 0, 0, true, false, version);

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
            => ExportAllDataForAvatarByIdAsync(avatarId, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var all = await LoadAllHolonsAsync(HolonType.All, true, true, 0, 0, true, false, version);
            if (all.IsError || all.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, all.Message);
                return result;
            }

            var owned = new List<IHolon>();
            foreach (var holon in all.Result)
            {
                if (holon.CreatedByAvatarId == avatarId)
                    owned.Add(holon);
            }

            result.Result = owned;
            result.IsLoaded = true;
            result.LoadedCount = owned.Count;
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
            => ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var avatar = await LoadAvatarByUsernameAsync(avatarUsername, version);
            if (avatar.IsError || avatar.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, avatar.Message);
                return result;
            }
            return await ExportAllDataForAvatarByIdAsync(avatar.Result.Id, version);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
            => ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var avatar = await LoadAvatarByEmailAsync(avatarEmailAddress, version);
            if (avatar.IsError || avatar.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, avatar.Message);
                return result;
            }
            return await ExportAllDataForAvatarByIdAsync(avatar.Result.Id, version);
        }

        // ─── Search ──────────────────────────────────────────────────────────────

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            var searchResults = new SearchResults();

            try
            {
                var groups = searchParams?.SearchGroups ?? new List<ISearchGroupBase>();
                var wantAvatars = groups.Count == 0 || groups.Exists(g => g.SearchAvatars);
                var wantHolons = groups.Count == 0 || groups.Exists(g => g.SearchHolons);

                var matchedAvatars = new Dictionary<Guid, IAvatar>();
                var matchedHolons = new Dictionary<Guid, IHolon>();

                // ── Avatars ──────────────────────────────────────────────────
                if (wantAvatars)
                {
                    var avatars = await LoadAllAvatarsAsync(version);
                    if (avatars.IsError && !continueOnError)
                    {
                        OASISErrorHandling.HandleError(ref result, avatars.Message);
                        return result;
                    }

                    foreach (var avatar in avatars.Result ?? new List<IAvatar>())
                    {
                        if (avatar == null) continue;
                        if (searchParams != null && searchParams.SearchOnlyForCurrentAvatar
                            && searchParams.AvatarId != Guid.Empty && avatar.Id != searchParams.AvatarId)
                            continue;

                        if (groups.Count == 0 || AvatarMatchesAnyGroup(avatar, groups))
                            matchedAvatars[avatar.Id] = avatar;
                    }
                }

                // ── Holons ───────────────────────────────────────────────────
                if (wantHolons)
                {
                    var holons = await LoadAllHolonsAsync(HolonType.All, loadChildren, recursive, maxChildDepth, 0, continueOnError, false, version);
                    if (holons.IsError && !continueOnError)
                    {
                        OASISErrorHandling.HandleError(ref result, holons.Message);
                        return result;
                    }

                    foreach (var holon in holons.Result ?? new List<IHolon>())
                    {
                        if (holon == null) continue;

                        if (searchParams != null)
                        {
                            if (searchParams.SearchOnlyForCurrentAvatar && searchParams.AvatarId != Guid.Empty
                                && holon.CreatedByAvatarId != searchParams.AvatarId)
                                continue;

                            if (searchParams.ParentId != Guid.Empty && holon.ParentHolonId != searchParams.ParentId)
                                continue;

                            if (!MetaDataMatches(holon, searchParams.FilterByMetaData, searchParams.MetaKeyValuePairMatchMode))
                                continue;
                        }

                        if (groups.Count == 0 || HolonMatchesAnyGroup(holon, groups))
                            matchedHolons[holon.Id] = holon;
                    }
                }

                searchResults.SearchResultAvatars = new List<IAvatar>(matchedAvatars.Values);
                searchResults.SearchResultHolons = new List<IHolon>(matchedHolons.Values);
                searchResults.NumberOfResults = searchResults.SearchResultAvatars.Count + searchResults.SearchResultHolons.Count;

                result.Result = searchResults;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"MemcachedOASIS: SearchAsync failed: {ex.Message}");
            }

            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
            => SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).GetAwaiter().GetResult();

        private static bool Contains(string source, string query)
            => !string.IsNullOrEmpty(source) && source.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;

        private static bool MetaDataMatches(IHolon holon, Dictionary<string, string> filter, MetaKeyValuePairMatchMode mode)
        {
            if (filter == null || filter.Count == 0) return true;
            if (holon.MetaData == null) return false;

            var matched = 0;
            foreach (var pair in filter)
            {
                if (holon.MetaData.TryGetValue(pair.Key, out var value) && value?.ToString() == pair.Value)
                    matched++;
            }

            return mode == MetaKeyValuePairMatchMode.All ? matched == filter.Count : matched > 0;
        }

        private static bool AvatarMatchesAnyGroup(IAvatar avatar, List<ISearchGroupBase> groups)
        {
            foreach (var group in groups)
            {
                if (!group.SearchAvatars) continue;

                var text = group as ISearchTextGroup;
                var query = text?.SearchQuery;
                if (string.IsNullOrWhiteSpace(query)) return true;

                var p = group.AvatarSearchParams;

                // No field flags set - match the natural identity fields.
                if (p == null)
                {
                    if (Contains(avatar.Username, query) || Contains(avatar.Email, query)
                        || Contains(avatar.FirstName, query) || Contains(avatar.LastName, query))
                        return true;
                    continue;
                }

                if (p.Username && Contains(avatar.Username, query)) return true;
                if (p.Email && Contains(avatar.Email, query)) return true;
                if (p.FirstName && Contains(avatar.FirstName, query)) return true;
                if (p.LastName && Contains(avatar.LastName, query)) return true;
                if (p.Title && Contains(avatar.Title, query)) return true;
                if (p.AvatarId && Contains(avatar.Id.ToString(), query)) return true;
                if (text != null && text.SearchIds && Contains(avatar.Id.ToString(), query)) return true;

                if (text != null && text.SearchProviderKeys && avatar.ProviderUniqueStorageKey != null)
                {
                    foreach (var key in avatar.ProviderUniqueStorageKey.Values)
                        if (Contains(key, query)) return true;
                }

                // Flags present but none of them matched a searchable field - fall
                // back to identity fields so a query is never silently dropped.
                if (!p.Username && !p.Email && !p.FirstName && !p.LastName && !p.Title && !p.AvatarId)
                {
                    if (Contains(avatar.Username, query) || Contains(avatar.Email, query))
                        return true;
                }
            }

            return false;
        }

        private static bool HolonMatchesAnyGroup(IHolon holon, List<ISearchGroupBase> groups)
        {
            foreach (var group in groups)
            {
                if (!group.SearchHolons) continue;

                if (group.HolonType != HolonType.All && holon.HolonType != group.HolonType)
                    continue;

                var text = group as ISearchTextGroup;
                var query = text?.SearchQuery;
                if (string.IsNullOrWhiteSpace(query)) return true;

                var p = group.HolonSearchParams;

                if (p == null)
                {
                    if (Contains(holon.Name, query) || Contains(holon.Description, query))
                        return true;
                    continue;
                }

                if (p.Name && Contains(holon.Name, query)) return true;
                if (p.Description && Contains(holon.Description, query)) return true;
                if (text != null && text.SearchIds && Contains(holon.Id.ToString(), query)) return true;

                if (p.MetaData && holon.MetaData != null)
                {
                    foreach (var kvp in holon.MetaData)
                        if (Contains(kvp.Key, query) || Contains(kvp.Value?.ToString(), query)) return true;
                }

                if ((p.ProviderUniqueStorageKey || (text != null && text.SearchProviderKeys))
                    && holon.ProviderUniqueStorageKey != null)
                {
                    foreach (var key in holon.ProviderUniqueStorageKey.Values)
                        if (Contains(key, query)) return true;
                }

                if (!p.Name && !p.Description && !p.MetaData && !p.ProviderUniqueStorageKey)
                {
                    if (Contains(holon.Name, query) || Contains(holon.Description, query))
                        return true;
                }
            }

            return false;
        }

    }
}
