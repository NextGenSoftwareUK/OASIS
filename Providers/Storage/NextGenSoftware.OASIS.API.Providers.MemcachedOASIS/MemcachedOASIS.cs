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
using NextGenSoftware.OASIS.Common;
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
    public class MemcachedOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISDBStorageProvider
    {
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
                var config = new MemcachedClientConfiguration(NullLoggerFactory.Instance, new MemcachedClientOptions
                {
                    Servers = new List<Server> { new Server { Address = _host, Port = _port } }
                });
                _client = new MemcachedClient(NullLoggerFactory.Instance, config);
                IsProviderActivated = true;
                result.Result = true; result.IsError = false; result.Message = "MemcachedOASIS activated.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"MemcachedOASIS: Error activating — {ex.Message}"); }
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            _client?.Dispose();
            IsProviderActivated = false;
            return await Task.FromResult(new OASISResult<bool> { Result = true, IsError = false, Message = "MemcachedOASIS deactivated." });
        }

        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        private async Task<T?> GetAsync<T>(string key) where T : class
        {
            var val = await _client.GetValueAsync<string>(SafeKey(key));
            return val == null ? null : Des<T>(val);
        }

        private async Task SetAsync(string key, object value) => await _client.SetAsync(SafeKey(key), Ser(value), expiration: 0);
        private async Task DeleteAsync(string key) => await _client.RemoveAsync(SafeKey(key));

        private async Task<List<string>> GetIndexAsync(string indexKey)
        {
            var raw = await _client.GetValueAsync<string>(SafeKey(indexKey));
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
                result.Result = avatar; result.IsError = false; result.Message = $"MemcachedOASIS: Avatar '{avatar.Username}' saved.";
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
                if (a == null || a.IsDeleted) { OASISErrorHandling.HandleError(ref result, $"MemcachedOASIS: No avatar for ID '{id}'."); return result; }
                result.Result = a; result.IsError = false; result.Message = "MemcachedOASIS: Avatar loaded.";
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
                var idStr = await _client.GetValueAsync<string>(SafeKey($"avatar-by-username:{username.ToLowerInvariant()}"));
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
                var idStr = await _client.GetValueAsync<string>(SafeKey($"avatar-by-email:{avatarEmail.ToLowerInvariant()}"));
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
                foreach (var idStr in ids) { if (!Guid.TryParse(idStr, out Guid id)) continue; var r = await LoadAvatarAsync(id, version); if (!r.IsError && r.Result != null) list.Add(r.Result); }
                result.Result = list; result.IsError = false; result.Message = $"MemcachedOASIS: Loaded {list.Count} avatar(s).";
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
                if (softDelete) { var load = await LoadAvatarAsync(id); if (!load.IsError && load.Result != null) { var a = (Avatar)load.Result; a.DeletedDate = DateTime.UtcNow; await SaveAvatarAsync(a); } }
                else { await DeleteAsync($"avatar:{id}"); await RemoveFromIndexAsync("avatars:index", id.ToString()); }
                result.Result = true; result.IsError = false; result.Message = $"MemcachedOASIS: Avatar '{id}' deleted.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"MemcachedOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) => DeleteAvatarAsync(id, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string u, bool softDelete = true) { var a = await LoadAvatarByUsernameAsync(u); if (a.IsError || a.Result == null) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, $"MemcachedOASIS: Avatar '{u}' not found."); return r; } return await DeleteAvatarAsync(a.Result.Id, softDelete); }
        public override OASISResult<bool> DeleteAvatarByUsername(string u, bool softDelete = true) => DeleteAvatarByUsernameAsync(u, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string e, bool softDelete = true) { var a = await LoadAvatarByEmailAsync(e); if (a.IsError || a.Result == null) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, $"MemcachedOASIS: Avatar not found."); return r; } return await DeleteAvatarAsync(a.Result.Id, softDelete); }
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
                await SetAsync($"avatar-detail:{d.Id}", d);
                if (!string.IsNullOrEmpty(d.Username)) await SetAsync($"detail-by-username:{d.Username.ToLowerInvariant()}", d.Id.ToString());
                if (!string.IsNullOrEmpty(d.Email)) await SetAsync($"detail-by-email:{d.Email.ToLowerInvariant()}", d.Id.ToString());
                result.Result = d; result.IsError = false; result.Message = "MemcachedOASIS: AvatarDetail saved.";
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
                var d = await GetAsync<AvatarDetail>($"avatar-detail:{id}");
                if (d == null) { OASISErrorHandling.HandleError(ref result, $"MemcachedOASIS: No detail for ID '{id}'."); return result; }
                result.Result = d; result.IsError = false; result.Message = "MemcachedOASIS: AvatarDetail loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"MemcachedOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => LoadAvatarDetailAsync(id, version).Result;
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string u, int version = 0) { var result = new OASISResult<IAvatarDetail>(); try { var idStr = await _client.GetValueAsync<string>(SafeKey($"detail-by-username:{u.ToLowerInvariant()}")); if (idStr == null || !Guid.TryParse(idStr, out Guid id)) { OASISErrorHandling.HandleError(ref result, $"MemcachedOASIS: No detail for username '{u}'."); return result; } return await LoadAvatarDetailAsync(id, version); } catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"MemcachedOASIS: {ex.Message}"); } return result; }
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string u, int version = 0) => LoadAvatarDetailByUsernameAsync(u, version).Result;
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string e, int version = 0) { var result = new OASISResult<IAvatarDetail>(); try { var idStr = await _client.GetValueAsync<string>(SafeKey($"detail-by-email:{e.ToLowerInvariant()}")); if (idStr == null || !Guid.TryParse(idStr, out Guid id)) { OASISErrorHandling.HandleError(ref result, $"MemcachedOASIS: No detail for email '{e}'."); return result; } return await LoadAvatarDetailAsync(id, version); } catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"MemcachedOASIS: {ex.Message}"); } return result; }
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string e, int version = 0) => LoadAvatarDetailByEmailAsync(e, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                var avatarResult = await LoadAllAvatarsAsync(version);
                var list = new List<IAvatarDetail>();
                if (!avatarResult.IsError && avatarResult.Result != null) foreach (var a in avatarResult.Result) { var dr = await LoadAvatarDetailAsync(a.Id, version); if (!dr.IsError && dr.Result != null) list.Add(dr.Result); }
                result.Result = list; result.IsError = false; result.Message = $"MemcachedOASIS: Loaded {list.Count} detail(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"MemcachedOASIS: {ex.Message}"); }
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
                await SetAsync($"holon:{holon.Id}", holon);
                await AddToIndexAsync("holons:index", holon.Id.ToString());
                result.Result = holon; result.IsError = false; result.Message = $"MemcachedOASIS: Holon '{holon.Name}' saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"MemcachedOASIS: Error saving holon: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, int version = 0) => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, int version = 0)
        { var result = new OASISResult<IEnumerable<IHolon>>(); var saved = new List<IHolon>(); foreach (var h in holons) { var r = await SaveHolonAsync(h, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, version); if (!r.IsError && r.Result != null) saved.Add(r.Result); } result.Result = saved; result.IsError = false; result.Message = $"MemcachedOASIS: Saved {saved.Count} holons."; return result; }
        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, int version = 0) => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, version).Result;

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var h = await GetAsync<Holon>($"holon:{id}");
                if (h == null || h.IsDeleted) { OASISErrorHandling.HandleError(ref result, $"MemcachedOASIS: No holon for ID '{id}'."); return result; }
                result.Result = h; result.IsError = false; result.Message = "MemcachedOASIS: Holon loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"MemcachedOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        public override async Task<OASISResult<IHolon>> LoadHolonByProviderKeyAsync(string pk, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) { if (Guid.TryParse(pk, out Guid id)) return await LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version); var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, $"MemcachedOASIS: Invalid GUID '{pk}'."); return r; }
        public override OASISResult<IHolon> LoadHolonByProviderKey(string pk, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonByProviderKeyAsync(pk, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var ids = await GetIndexAsync("holons:index");
                var list = new List<IHolon>();
                foreach (var idStr in ids) { if (!Guid.TryParse(idStr, out Guid id)) continue; var r = await LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version); if (r.IsError || r.Result == null || r.Result.IsDeleted) continue; if (holonType != HolonType.All && r.Result.HolonType != holonType) continue; list.Add(r.Result); }
                result.Result = list; result.IsError = false; result.Message = $"MemcachedOASIS: Loaded {list.Count} holon(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"MemcachedOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false) => LoadAllHolonsAsync(holonType, loadChildren, recursive, maxChildDepth, version, continueOnError, loadChildrenFromProvider).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false)
        { var result = new OASISResult<IEnumerable<IHolon>>(); try { var all = await LoadAllHolonsAsync(holonType, loadChildren, recursive, maxChildDepth, version, continueOnError, loadChildrenFromProvider); var list = new List<IHolon>(); if (!all.IsError && all.Result != null) foreach (var h in all.Result) { if (h.ParentHolonId == id) list.Add(h); } result.Result = list; result.IsError = false; result.Message = $"MemcachedOASIS: Loaded {list.Count} child holon(s)."; } catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"MemcachedOASIS: {ex.Message}"); } return result; }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false) => LoadHolonsForParentAsync(id, holonType, loadChildren, recursive, maxChildDepth, version, continueOnError, loadChildrenFromProvider).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string pk, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false) { if (Guid.TryParse(pk, out Guid id)) return await LoadHolonsForParentAsync(id, holonType, loadChildren, recursive, maxChildDepth, version, continueOnError, loadChildrenFromProvider); var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, $"MemcachedOASIS: Invalid GUID '{pk}'."); return r; }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string pk, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false) => LoadHolonsForParentAsync(pk, holonType, loadChildren, recursive, maxChildDepth, version, continueOnError, loadChildrenFromProvider).Result;

        public override async Task<OASISResult<bool>> DeleteHolonAsync(Guid id, bool softDelete = true)
        { var result = new OASISResult<bool>(); try { if (softDelete) { var load = await LoadHolonAsync(id); if (!load.IsError && load.Result != null) { var h = (Holon)load.Result; h.DeletedDate = DateTime.UtcNow; await SaveHolonAsync(h); } } else { await DeleteAsync($"holon:{id}"); await RemoveFromIndexAsync("holons:index", id.ToString()); } result.Result = true; result.IsError = false; result.Message = $"MemcachedOASIS: Holon '{id}' deleted."; } catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"MemcachedOASIS: {ex.Message}"); } return result; }
        public override OASISResult<bool> DeleteHolon(Guid id, bool softDelete = true) => DeleteHolonAsync(id, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteHolonAsync(string pk, bool softDelete = true) { if (Guid.TryParse(pk, out Guid id)) return await DeleteHolonAsync(id, softDelete); var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, $"MemcachedOASIS: Invalid GUID '{pk}'."); return r; }
        public override OASISResult<bool> DeleteHolon(string pk, bool softDelete = true) => DeleteHolonAsync(pk, softDelete).Result;

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        { var result = new OASISResult<ISearchResults>(); var sr = new SearchResults(); var avatarResult = await LoadAllAvatarsAsync(); if (!avatarResult.IsError && avatarResult.Result != null) sr.Avatars = new List<IAvatar>(avatarResult.Result); var holonResult = await LoadAllHolonsAsync(); if (!holonResult.IsError && holonResult.Result != null) sr.Holons = new List<IHolon>(holonResult.Result); result.Result = sr; result.IsError = false; return result; }
        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
        public override Task<OASISResult<IAvatar>> SearchAvatarsAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => throw new NotImplementedException("MemcachedOASIS: Use SearchAsync.");
        public override OASISResult<IAvatar> SearchAvatars(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => throw new NotImplementedException();
        public override Task<OASISResult<IHolon>> SearchHolonsAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => throw new NotImplementedException("MemcachedOASIS: Use SearchAsync.");
        public override OASISResult<IHolon> SearchHolons(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => throw new NotImplementedException();

        public override string GetProviderVersion() => "1.0.0";
        public override Task<string> GetProviderVersionAsync() => Task.FromResult("1.0.0");
    }
}
