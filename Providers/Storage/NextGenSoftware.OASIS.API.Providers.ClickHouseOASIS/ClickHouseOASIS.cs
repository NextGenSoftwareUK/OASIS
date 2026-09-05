using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using ClickHouse.Client.ADO;
using ClickHouse.Client.Copy;
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
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NextGenSoftware.OASIS.API.Providers.ClickHouseOASIS
{
    /// <summary>
    /// OASIS provider for ClickHouse — a columnar OLAP database optimised for analytical workloads.
    ///
    /// Tables use ReplacingMergeTree so that re-inserting a row with the same primary key eventually
    /// deduplicates (upsert semantics) after background merges.  A FINAL qualifier on SELECT forces
    /// immediate deduplication at query time.
    ///
    /// Schema:
    ///   oasis.avatars         (id UUID, username String, email String, is_deleted UInt8, data_json String)
    ///   oasis.avatar_details  (id UUID, username String, email String, data_json String)
    ///   oasis.holons          (id UUID, parent_holon_id UUID, holon_type String, is_deleted UInt8, data_json String)
    ///
    /// Constructor parameters:
    ///   connectionString — ClickHouse ADO.NET connection string, e.g.
    ///                      "Host=localhost;Port=8123;Database=oasis;Username=default;Password="
    /// </summary>
    public class ClickHouseOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISDBStorageProvider
    {
        /// <summary>
        /// When true this provider stores a new record per save and links to the previous
        /// version (blockchain-style) instead of updating in place.
        /// </summary>
        public bool IsVersionControlEnabled { get; set; }

        private readonly string _connectionString;

        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public ClickHouseOASIS(string connectionString)
        {
            _connectionString = connectionString;
            ProviderName = "ClickHouseOASIS";
            ProviderDescription = "ClickHouse provider (columnar OLAP database via ClickHouse.Client ADO.NET driver)";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.ClickHouseOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageLocalAndNetwork);
        }

        private ClickHouseConnection OpenConnection()
        {
            var conn = new ClickHouseConnection(_connectionString);
            conn.Open();
            return conn;
        }

        private static string Ser(object obj) => JsonSerializer.Serialize(obj, _jsonOpts);
        private static T? Des<T>(string json) => JsonSerializer.Deserialize<T>(json, _jsonOpts);

        private async Task ExecuteAsync(string sql)
        {
            using var conn = OpenConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            await cmd.ExecuteNonQueryAsync();
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                await ExecuteAsync("CREATE DATABASE IF NOT EXISTS oasis");
                await ExecuteAsync(@"CREATE TABLE IF NOT EXISTS oasis.avatars (
                    id UUID,
                    username String,
                    email String,
                    is_deleted UInt8,
                    data_json String,
                    _version UInt64 DEFAULT toUnixTimestamp64Milli(now64())
                ) ENGINE = ReplacingMergeTree(_version)
                ORDER BY id");
                await ExecuteAsync(@"CREATE TABLE IF NOT EXISTS oasis.avatar_details (
                    id UUID,
                    username String,
                    email String,
                    data_json String,
                    _version UInt64 DEFAULT toUnixTimestamp64Milli(now64())
                ) ENGINE = ReplacingMergeTree(_version)
                ORDER BY id");
                await ExecuteAsync(@"CREATE TABLE IF NOT EXISTS oasis.holons (
                    id UUID,
                    parent_holon_id UUID,
                    holon_type String,
                    is_deleted UInt8,
                    data_json String,
                    _version UInt64 DEFAULT toUnixTimestamp64Milli(now64())
                ) ENGINE = ReplacingMergeTree(_version)
                ORDER BY id");
                IsProviderActivated = true; result.Result = true; result.IsError = false; result.Message = "ClickHouseOASIS activated.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"ClickHouseOASIS: Error activating — {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;
        public override async Task<OASISResult<bool>> DeActivateProviderAsync() { IsProviderActivated = false; return await Task.FromResult(new OASISResult<bool> { Result = true, IsError = false, Message = "ClickHouseOASIS deactivated." }); }
        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // ─── Avatar ───────────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (avatar.Id == Guid.Empty) avatar.Id = Guid.NewGuid();
                if (avatar.ProviderUniqueStorageKey == null) avatar.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.ClickHouseOASIS] = avatar.Id.ToString();
                var json = Ser(avatar);
                var isDeleted = avatar.DeletedDate != DateTime.MinValue ? 1 : 0;
                await ExecuteAsync($"INSERT INTO oasis.avatars (id, username, email, is_deleted, data_json) VALUES ('{avatar.Id}', '{Esc(avatar.Username)}', '{Esc(avatar.Email)}', {isDeleted}, '{Esc(json)}')");
                result.Result = avatar; result.IsError = false; result.Message = $"ClickHouseOASIS: Avatar '{avatar.Username}' saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"ClickHouseOASIS: Error saving avatar: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) => SaveAvatarAsync(avatar).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                using var conn = OpenConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = $"SELECT data_json FROM oasis.avatars FINAL WHERE id = '{id}' AND is_deleted = 0 LIMIT 1";
                using var reader = await cmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync()) { OASISErrorHandling.HandleError(ref result, $"ClickHouseOASIS: No avatar for ID '{id}'."); return result; }
                var a = Des<Avatar>(reader.GetString(0));
                if (a == null) { OASISErrorHandling.HandleError(ref result, $"ClickHouseOASIS: No avatar for ID '{id}'."); return result; }
                result.Result = a; result.IsError = false; result.Message = "ClickHouseOASIS: Avatar loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"ClickHouseOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) => LoadAvatarAsync(id, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                using var conn = OpenConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = $"SELECT data_json FROM oasis.avatars FINAL WHERE username = '{Esc(username)}' AND is_deleted = 0 LIMIT 1";
                using var reader = await cmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync()) { OASISErrorHandling.HandleError(ref result, $"ClickHouseOASIS: No avatar for username '{username}'."); return result; }
                var a = Des<Avatar>(reader.GetString(0));
                if (a == null) { OASISErrorHandling.HandleError(ref result, $"ClickHouseOASIS: No avatar for username '{username}'."); return result; }
                result.Result = a; result.IsError = false; result.Message = "ClickHouseOASIS: Avatar loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"ClickHouseOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string username, int version = 0) => LoadAvatarByUsernameAsync(username, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                using var conn = OpenConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = $"SELECT data_json FROM oasis.avatars FINAL WHERE email = '{Esc(avatarEmail)}' AND is_deleted = 0 LIMIT 1";
                using var reader = await cmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync()) { OASISErrorHandling.HandleError(ref result, $"ClickHouseOASIS: No avatar for email '{avatarEmail}'."); return result; }
                var a = Des<Avatar>(reader.GetString(0));
                if (a == null) { OASISErrorHandling.HandleError(ref result, $"ClickHouseOASIS: No avatar for email '{avatarEmail}'."); return result; }
                result.Result = a; result.IsError = false; result.Message = "ClickHouseOASIS: Avatar loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"ClickHouseOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0) => LoadAvatarByEmailAsync(avatarEmail, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            if (Guid.TryParse(providerKey, out Guid id)) return await LoadAvatarAsync(id, version);
            var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, $"ClickHouseOASIS: Invalid GUID '{providerKey}'."); return r;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0) => LoadAvatarByProviderKeyAsync(providerKey, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                var list = new List<IAvatar>();
                using var conn = OpenConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT data_json FROM oasis.avatars FINAL WHERE is_deleted = 0";
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync()) { var a = Des<Avatar>(reader.GetString(0)); if (a != null) list.Add(a); }
                result.Result = list; result.IsError = false; result.Message = $"ClickHouseOASIS: Loaded {list.Count} avatar(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"ClickHouseOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) => LoadAllAvatarsAsync(version).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (softDelete) { var l = await LoadAvatarAsync(id); if (!l.IsError && l.Result != null) { var a = (Avatar)l.Result; a.DeletedDate = DateTime.UtcNow; await SaveAvatarAsync(a); } }
                else { await ExecuteAsync($"ALTER TABLE oasis.avatars DELETE WHERE id = '{id}'"); }
                result.Result = true; result.IsError = false; result.Message = $"ClickHouseOASIS: Avatar '{id}' deleted.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"ClickHouseOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) => DeleteAvatarAsync(id, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string u, bool softDelete = true) { var a = await LoadAvatarByUsernameAsync(u); if (a.IsError || a.Result == null) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, $"ClickHouseOASIS: Avatar '{u}' not found."); return r; } return await DeleteAvatarAsync(a.Result.Id, softDelete); }
        public override OASISResult<bool> DeleteAvatarByUsername(string u, bool softDelete = true) => DeleteAvatarByUsernameAsync(u, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string e, bool softDelete = true) { var a = await LoadAvatarByEmailAsync(e); if (a.IsError || a.Result == null) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "ClickHouseOASIS: Avatar not found."); return r; } return await DeleteAvatarAsync(a.Result.Id, softDelete); }
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
                var json = Ser(d);
                await ExecuteAsync($"INSERT INTO oasis.avatar_details (id, username, email, data_json) VALUES ('{d.Id}', '{Esc(d.Username)}', '{Esc(d.Email)}', '{Esc(json)}')");
                result.Result = d; result.IsError = false; result.Message = "ClickHouseOASIS: AvatarDetail saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"ClickHouseOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail d) => SaveAvatarDetailAsync(d).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                using var conn = OpenConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = $"SELECT data_json FROM oasis.avatar_details FINAL WHERE id = '{id}' LIMIT 1";
                using var reader = await cmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync()) { OASISErrorHandling.HandleError(ref result, $"ClickHouseOASIS: No detail for ID '{id}'."); return result; }
                var detail = Des<AvatarDetail>(reader.GetString(0));
                if (detail == null) { OASISErrorHandling.HandleError(ref result, $"ClickHouseOASIS: No detail for ID '{id}'."); return result; }
                result.Result = detail; result.IsError = false; result.Message = "ClickHouseOASIS: AvatarDetail loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"ClickHouseOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => LoadAvatarDetailAsync(id, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string u, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                using var conn = OpenConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = $"SELECT data_json FROM oasis.avatar_details FINAL WHERE username = '{Esc(u)}' LIMIT 1";
                using var reader = await cmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync()) { OASISErrorHandling.HandleError(ref result, $"ClickHouseOASIS: No detail for username '{u}'."); return result; }
                var d = Des<AvatarDetail>(reader.GetString(0));
                if (d == null) { OASISErrorHandling.HandleError(ref result, $"ClickHouseOASIS: No detail for username '{u}'."); return result; }
                result.Result = d; result.IsError = false; result.Message = "ClickHouseOASIS: AvatarDetail loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"ClickHouseOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string u, int version = 0) => LoadAvatarDetailByUsernameAsync(u, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string e, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                using var conn = OpenConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = $"SELECT data_json FROM oasis.avatar_details FINAL WHERE email = '{Esc(e)}' LIMIT 1";
                using var reader = await cmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync()) { OASISErrorHandling.HandleError(ref result, $"ClickHouseOASIS: No detail for email '{e}'."); return result; }
                var d = Des<AvatarDetail>(reader.GetString(0));
                if (d == null) { OASISErrorHandling.HandleError(ref result, $"ClickHouseOASIS: No detail for email '{e}'."); return result; }
                result.Result = d; result.IsError = false; result.Message = "ClickHouseOASIS: AvatarDetail loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"ClickHouseOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string e, int version = 0) => LoadAvatarDetailByEmailAsync(e, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                var list = new List<IAvatarDetail>();
                using var conn = OpenConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT data_json FROM oasis.avatar_details FINAL";
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync()) { var d = Des<AvatarDetail>(reader.GetString(0)); if (d != null) list.Add(d); }
                result.Result = list; result.IsError = false; result.Message = $"ClickHouseOASIS: Loaded {list.Count} detail(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"ClickHouseOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0) => LoadAllAvatarDetailsAsync(version).Result;

        // ─── Holons ───────────────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (holon.Id == Guid.Empty) holon.Id = Guid.NewGuid();
                var json = Ser(holon);
                var isDeleted = holon.DeletedDate != DateTime.MinValue ? 1 : 0;
                var parentId = holon.ParentHolonId == Guid.Empty ? Guid.Empty.ToString() : holon.ParentHolonId.ToString();
                await ExecuteAsync($"INSERT INTO oasis.holons (id, parent_holon_id, holon_type, is_deleted, data_json) VALUES ('{holon.Id}', '{parentId}', '{Esc(holon.HolonType.ToString())}', {isDeleted}, '{Esc(json)}')");
                result.Result = holon; result.IsError = false; result.Message = $"ClickHouseOASIS: Holon '{holon.Name}' saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"ClickHouseOASIS: Error saving holon: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>(); var saved = new List<IHolon>();
            foreach (var h in holons) { var r = await SaveHolonAsync(h, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider); if (!r.IsError && r.Result != null) saved.Add(r.Result); }
            result.Result = saved; result.IsError = false; result.Message = $"ClickHouseOASIS: Saved {saved.Count} holons."; return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                using var conn = OpenConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = $"SELECT data_json FROM oasis.holons FINAL WHERE id = '{id}' AND is_deleted = 0 LIMIT 1";
                using var reader = await cmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync()) { OASISErrorHandling.HandleError(ref result, $"ClickHouseOASIS: No holon for ID '{id}'."); return result; }
                var h = Des<Holon>(reader.GetString(0));
                if (h == null) { OASISErrorHandling.HandleError(ref result, $"ClickHouseOASIS: No holon for ID '{id}'."); return result; }
                result.Result = h; result.IsError = false; result.Message = "ClickHouseOASIS: Holon loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"ClickHouseOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public async Task<OASISResult<IHolon>> LoadHolonByProviderKeyAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) { if (Guid.TryParse(providerKey, out Guid id)) return await LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version); var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, $"ClickHouseOASIS: Invalid GUID '{providerKey}'."); return r; }
        public OASISResult<IHolon> LoadHolonByProviderKey(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonByProviderKeyAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var list = new List<IHolon>();
                using var conn = OpenConnection();
                using var cmd = conn.CreateCommand();
                var filter = holonType != HolonType.All ? $" AND holon_type = '{Esc(holonType.ToString())}'" : "";
                cmd.CommandText = $"SELECT data_json FROM oasis.holons FINAL WHERE is_deleted = 0{filter}";
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync()) { var h = Des<Holon>(reader.GetString(0)); if (h != null) list.Add(h); }
                result.Result = list; result.IsError = false; result.Message = $"ClickHouseOASIS: Loaded {list.Count} holon(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"ClickHouseOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadAllHolonsAsync(holonType, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var list = new List<IHolon>();
                using var conn = OpenConnection();
                using var cmd = conn.CreateCommand();
                var filter = holonType != HolonType.All ? $" AND holon_type = '{Esc(holonType.ToString())}'" : "";
                cmd.CommandText = $"SELECT data_json FROM oasis.holons FINAL WHERE parent_holon_id = '{id}' AND is_deleted = 0{filter}";
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync()) { var h = Des<Holon>(reader.GetString(0)); if (h != null) list.Add(h); }
                result.Result = list; result.IsError = false; result.Message = $"ClickHouseOASIS: Loaded {list.Count} child holon(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"ClickHouseOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsForParentAsync(id, holonType, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) { if (Guid.TryParse(providerKey, out Guid id)) return await LoadHolonsForParentAsync(id, holonType, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version); var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, $"ClickHouseOASIS: Invalid GUID '{providerKey}'."); return r; }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsForParentAsync(providerKey, holonType, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public async Task<OASISResult<bool>> DeleteHolonSoftAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (softDelete) { var l = await LoadHolonAsync(id); if (!l.IsError && l.Result != null) { var h = (Holon)l.Result; h.DeletedDate = DateTime.UtcNow; await SaveHolonAsync(h); } }
                else { await ExecuteAsync($"ALTER TABLE oasis.holons DELETE WHERE id = '{id}'"); }
                result.Result = true; result.IsError = false; result.Message = $"ClickHouseOASIS: Holon '{id}' deleted.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"ClickHouseOASIS: {ex.Message}"); }
            return result;
        }

        public OASISResult<bool> DeleteHolonSoft(Guid id, bool softDelete = true) => DeleteHolonSoftAsync(id, softDelete).Result;
        public async Task<OASISResult<bool>> DeleteHolonSoftAsync(string pk, bool softDelete = true) { if (Guid.TryParse(pk, out Guid id)) return await DeleteHolonSoftAsync(id, softDelete); var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, $"ClickHouseOASIS: Invalid GUID '{pk}'."); return r; }
        public OASISResult<bool> DeleteHolonSoft(string pk, bool softDelete = true) => DeleteHolonSoftAsync(pk, softDelete).Result;

        // ─── Search ───────────────────────────────────────────────────────────────



        public string GetProviderVersion() => "1.0.0";
        public Task<string> GetProviderVersionAsync() => Task.FromResult("1.0.0");

        private static string Esc(string? s) => (s ?? "").Replace("'", "\\'").Replace("\\", "\\\\");

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

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var result = new OASISResult<IHolon>();
            var loaded = await LoadHolonAsync(id);
            if (loaded.IsError || loaded.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, loaded.Message);
                return result;
            }

            var deleted = await DeleteHolonSoftAsync(id, true);
            if (deleted.IsError)
                OASISErrorHandling.HandleError(ref result, deleted.Message);
            else
                result.Result = loaded.Result;

            return result;
        }

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
            return result;
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
            => ImportAsync(holons).GetAwaiter().GetResult();

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var result = new OASISResult<bool>();
            var saved = await SaveHolonsAsync(holons);
            if (saved.IsError)
                OASISErrorHandling.HandleError(ref result, saved.Message);
            else
                result.Result = true;
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
                OASISErrorHandling.HandleError(ref result, $"ClickHouseOASIS: SearchAsync failed: {ex.Message}");
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
