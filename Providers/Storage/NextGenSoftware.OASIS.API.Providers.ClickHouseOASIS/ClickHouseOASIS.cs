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

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, int version = 0)
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

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, int version = 0) => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>(); var saved = new List<IHolon>();
            foreach (var h in holons) { var r = await SaveHolonAsync(h, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, version); if (!r.IsError && r.Result != null) saved.Add(r.Result); }
            result.Result = saved; result.IsError = false; result.Message = $"ClickHouseOASIS: Saved {saved.Count} holons."; return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, int version = 0) => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, version).Result;

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

        public override async Task<OASISResult<IHolon>> LoadHolonByProviderKeyAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) { if (Guid.TryParse(providerKey, out Guid id)) return await LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version); var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, $"ClickHouseOASIS: Invalid GUID '{providerKey}'."); return r; }
        public override OASISResult<IHolon> LoadHolonByProviderKey(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonByProviderKeyAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false)
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

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false) => LoadAllHolonsAsync(holonType, loadChildren, recursive, maxChildDepth, version, continueOnError, loadChildrenFromProvider).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false)
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

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false) => LoadHolonsForParentAsync(id, holonType, loadChildren, recursive, maxChildDepth, version, continueOnError, loadChildrenFromProvider).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false) { if (Guid.TryParse(providerKey, out Guid id)) return await LoadHolonsForParentAsync(id, holonType, loadChildren, recursive, maxChildDepth, version, continueOnError, loadChildrenFromProvider); var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, $"ClickHouseOASIS: Invalid GUID '{providerKey}'."); return r; }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false) => LoadHolonsForParentAsync(providerKey, holonType, loadChildren, recursive, maxChildDepth, version, continueOnError, loadChildrenFromProvider).Result;

        public override async Task<OASISResult<bool>> DeleteHolonAsync(Guid id, bool softDelete = true)
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

        public override OASISResult<bool> DeleteHolon(Guid id, bool softDelete = true) => DeleteHolonAsync(id, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteHolonAsync(string pk, bool softDelete = true) { if (Guid.TryParse(pk, out Guid id)) return await DeleteHolonAsync(id, softDelete); var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, $"ClickHouseOASIS: Invalid GUID '{pk}'."); return r; }
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
        public override Task<OASISResult<IAvatar>> SearchAvatarsAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => throw new NotImplementedException("ClickHouseOASIS: Use SearchAsync.");
        public override OASISResult<IAvatar> SearchAvatars(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => throw new NotImplementedException();
        public override Task<OASISResult<IHolon>> SearchHolonsAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => throw new NotImplementedException("ClickHouseOASIS: Use SearchAsync.");
        public override OASISResult<IHolon> SearchHolons(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => throw new NotImplementedException();

        public override string GetProviderVersion() => "1.0.0";
        public override Task<string> GetProviderVersionAsync() => Task.FromResult("1.0.0");

        private static string Esc(string? s) => (s ?? "").Replace("'", "\\'").Replace("\\", "\\\\");
    }
}
